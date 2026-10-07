#!/usr/bin/env python3
"""Freeze, propose, approve, generate, and verify the Story 9.1 conformance tiering disposition.

The exact AC-9.1-01 command derives the closed schema, authoritative JSON, and
deterministic Markdown for every frozen pre-split conformance assertion. It needs
the retained isolated pre-split capture under `artifacts/v9/9.1/pre-split/` and
a Quality-owner decision in `docs/release-evidence/conformance-oracle-tiering-approvals-v2.json`
whose digest equals the derived per-row membership. Other modes:

* `--capture-pre-split --freeze-commit SHA` builds and runs the exact current CI
  conformance lane in an isolated clone and retains discovery, TRX, logs,
  assemblies, and a receipt. Nothing in the working repository is written
  outside the capture directory.
* `--propose-approvals` writes the digest-bound proposed membership for review.
* `--record-approval` records the owner's decision only for the exact proposed digest.
* `--verify` re-derives every fact read-only and reports the first failing blocker
  category; it uses the embedded pre-split facts when the retained capture is absent.

Exit codes: 0 PASS, 1 FAIL, 2 BLOCKED. Failures are printed as `FAIL: CODE: detail`.
The C# source model below is deliberately small: it tokenizes the C# subset that
the conformance project and the module sources use, splits type and member
declarations, and derives one transitive source closure per test method. Any
construct it does not understand raises `SourceModelError`, so an unsupported
discovery fails instead of silently dropping an assertion.

Closure rules (frozen by the disposition's `strengthDefinition`):

* A test method always closes over its own declaration, every constructor and
  field of its declaring class, and every property that carries an initializer,
  because xUnit executes them for each test.
* A declaring-class member is added when any closed declaration names it.
* Another top-level conformance-project type is added whole when any closed
  declaration names it, or names an extension method that it declares. A whole
  type closes over the types it names and its base types.
* A module type binds when a closed declaration names it through a visible
  namespace, a using alias, a qualified name, or one of its extension methods.

The canonical strength material is the triple of bound first-party module
assemblies, the behavior identity (SHA-256 of the canonical closure text), and
the negative-case count (closed assertion sites that assert a rejection,
absence, falsity, or exclusion).
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field
from datetime import datetime, timezone
from importlib import util as importlib_util
from pathlib import Path, PurePosixPath
from typing import Any, Iterable, NamedTuple

import jsonschema


class SourceModelError(Exception):
    """An unsupported or malformed source construct; the caller maps it to a blocker."""


class Token(NamedTuple):
    kind: str  # id, num, str, op
    text: str
    line: int
    inner: tuple[str, ...]  # identifiers inside interpolation holes


IDENT = re.compile(r"[^\W\d]\w*")
NUMBER = re.compile(
    r"(?:0[xXbB][0-9a-fA-F_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?|\.\d[\d_]*(?:[eE][+-]?\d+)?)[A-Za-z]*"
)
LITERAL_PREFIX = re.compile(r"\$+@?|@\$+|@")
OPERATORS = (
    "??=", "...", "=>", "==", "!=", "<=", ">=", "&&", "||", "??", "?.", "::", "++", "--",
    "..", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "->",
)
MODIFIERS = frozenset({
    "public", "private", "protected", "internal", "static", "readonly", "sealed", "abstract",
    "virtual", "override", "async", "partial", "const", "extern", "unsafe", "volatile", "new",
    "required", "file", "ref", "fixed", "implicit", "explicit",
})
TYPE_KEYWORDS = frozenset({"class", "struct", "interface", "enum", "record", "delegate"})
IMPLICIT_USINGS = (
    "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Net.Http",
    "System.Threading", "System.Threading.Tasks",
)
TEST_ATTRIBUTES = frozenset({"Fact", "Theory"})

# Assertion sites: Shouldly instance assertions, the static `Should` entry
# points, and xUnit's static `Assert` class.
NEGATIVE_EXACT = frozenset({
    "ShouldThrow", "ShouldThrowAsync", "ShouldBeFalse", "ShouldBeNull", "ShouldBeEmpty",
    "ShouldBeNullOrEmpty", "ShouldBeNullOrWhiteSpace",
    "Should.Throw", "Should.ThrowAsync",
    "Assert.Throws", "Assert.ThrowsAsync", "Assert.ThrowsAny", "Assert.ThrowsAnyAsync",
    "Assert.False", "Assert.Null", "Assert.Empty", "Assert.DoesNotContain", "Assert.DoesNotMatch",
    "Assert.NotEqual", "Assert.NotSame", "Assert.NotStrictEqual", "Assert.NotInRange",
})
POSITIVE_SHOULD_NOT = frozenset({
    "ShouldNotBeNull", "ShouldNotBeEmpty", "ShouldNotBeNullOrEmpty", "ShouldNotBeNullOrWhiteSpace",
    "ShouldNotThrow", "ShouldNotThrowAsync",
})


def sha256_bytes(content: bytes) -> str:
    """Return the lowercase SHA-256 digest of exact bytes."""
    return hashlib.sha256(content).hexdigest()


def canonical_json(value: object) -> bytes:
    """Canonical JSON: sorted keys, no insignificant whitespace, UTF-8."""
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")


# --------------------------------------------------------------------------- tokens


def tokenize(text: str, path: str, *, strict: bool) -> list[Token]:
    """Tokenize C# source; comments and whitespace are dropped."""
    tokens: list[Token] = []
    index, length, line = 0, len(text), 1
    line_start = True

    def fail(detail: str) -> SourceModelError:
        return SourceModelError(f"{path}:{line}: {detail}")

    while index < length:
        character = text[index]
        if character == "\n":
            line += 1
            index += 1
            line_start = True
            continue
        if character in " \t\r\f\v﻿":
            index += 1
            continue
        if text.startswith("//", index):
            end = text.find("\n", index)
            index = length if end < 0 else end
            continue
        if text.startswith("/*", index):
            end = text.find("*/", index + 2)
            if end < 0:
                raise fail("unterminated block comment")
            line += text.count("\n", index, end)
            index = end + 2
            continue
        if character == "#" and line_start:
            if strict:
                raise fail("preprocessor directives are unsupported in conformance sources")
            end = text.find("\n", index)
            index = length if end < 0 else end
            continue
        line_start = False
        if character in "\"$@'":
            literal = _scan_literal(text, index, path, line, strict=strict)
            if literal is not None:
                end, inner = literal
                raw = text[index:end]
                tokens.append(Token("str", raw.replace("\r\n", "\n"), line, tuple(inner)))
                line += raw.count("\n")
                index = end
                continue
        if character == "@" and index + 1 < length:
            match = IDENT.match(text, index + 1)
            if match is not None:
                tokens.append(Token("id", match.group(), line, ()))
                index = match.end()
                continue
        match = IDENT.match(text, index)
        if match is not None:
            tokens.append(Token("id", match.group(), line, ()))
            index = match.end()
            continue
        if character.isdigit() or (character == "." and index + 1 < length and text[index + 1].isdigit()):
            match = NUMBER.match(text, index)
            if match is None:  # pragma: no cover - NUMBER always matches a leading digit
                raise fail("malformed numeric literal")
            tokens.append(Token("num", match.group(), line, ()))
            index = match.end()
            continue
        operator = next((candidate for candidate in OPERATORS if text.startswith(candidate, index)), character)
        tokens.append(Token("op", operator, line, ()))
        index += len(operator)
    return tokens


def _scan_char(text: str, index: int, path: str, line: int) -> int:
    position = index + 1
    if position < len(text) and text[position] == "\\":
        position += 2
        while position < len(text) and text[position] != "'" and position - index < 12:
            position += 1
    else:
        position += 1
    if position >= len(text) or text[position] != "'":
        raise SourceModelError(f"{path}:{line}: malformed character literal")
    return position + 1


def _scan_literal(text: str, index: int, path: str, line: int, *, strict: bool) -> tuple[int, list[str]] | None:
    """Scan one string or character literal; None when `index` starts neither."""
    length = len(text)
    match = LITERAL_PREFIX.match(text, index)
    prefix = match.group() if match else ""
    start = index + len(prefix)
    if start >= length:
        return None
    if text[start] == "'" and not prefix:
        return _scan_char(text, index, path, line), []
    if text[start] != '"':
        return None
    dollars = prefix.count("$")
    verbatim = "@" in prefix
    if text.startswith('"""', start):
        if strict:
            raise SourceModelError(f"{path}:{line}: raw string literals are unsupported in conformance sources")
        quotes = 0
        while start + quotes < length and text[start + quotes] == '"':
            quotes += 1
        end = text.find('"' * quotes, start + quotes)
        if end < 0:
            raise SourceModelError(f"{path}:{line}: unterminated raw string literal")
        return end + quotes, []
    if dollars > 1:
        raise SourceModelError(f"{path}:{line}: multi-dollar interpolation requires a raw string")
    position = start + 1
    inner: list[str] = []
    while position < length:
        character = text[position]
        if verbatim:
            if character == '"':
                if text.startswith('""', position):
                    position += 2
                    continue
                return position + 1, inner
        else:
            if character == "\\":
                position += 2
                continue
            if character == '"':
                return position + 1, inner
            if character == "\n":
                raise SourceModelError(f"{path}:{line}: newline in a regular string literal")
        if dollars:
            if character == "{":
                if text.startswith("{{", position):
                    position += 2
                    continue
                hole_end = _scan_hole(text, position, path, line, strict=strict)
                for token in tokenize(text[position + 1:hole_end - 1], path, strict=strict):
                    if token.kind == "id":
                        inner.append(token.text)
                    inner.extend(token.inner)
                position = hole_end
                continue
            if character == "}":
                if text.startswith("}}", position):
                    position += 2
                    continue
                raise SourceModelError(f"{path}:{line}: unbalanced interpolation brace")
        position += 1
    raise SourceModelError(f"{path}:{line}: unterminated string literal")


def _scan_hole(text: str, index: int, path: str, line: int, *, strict: bool) -> int:
    depth = 0
    position = index
    while position < len(text):
        character = text[position]
        if character in "\"$@'" and position != index:
            literal = _scan_literal(text, position, path, line, strict=strict)
            if literal is not None:
                position = literal[0]
                continue
        if character == "{":
            depth += 1
        elif character == "}":
            depth -= 1
            if depth == 0:
                return position + 1
        position += 1
    raise SourceModelError(f"{path}:{line}: unterminated interpolation hole")


def normalize(tokens: Iterable[Token]) -> str:
    """Location-independent canonical text of a token span."""
    return " ".join(token.text for token in tokens)


# --------------------------------------------------------------------------- structure


@dataclass
class Member:
    name: str
    kind: str  # method, ctor, field, property, other
    start: int
    end: int
    attributes: tuple[str, ...]
    line: int
    modifiers: frozenset[str]
    is_extension: bool = False
    has_initializer: bool = False


@dataclass
class TypeDecl:
    name: str
    full_name: str
    kind: str
    start: int
    end: int
    modifiers: frozenset[str]
    bases: tuple[str, ...]
    attributes: tuple[str, ...]
    line: int
    file: "SourceFile"
    outer: "TypeDecl | None" = None
    members: list[Member] = field(default_factory=list)
    nested: list["TypeDecl"] = field(default_factory=list)


@dataclass
class SourceFile:
    path: str
    sha256: str
    tokens: list[Token]
    namespace: str
    usings: tuple[str, ...]
    aliases: dict[str, str]
    types: list[TypeDecl]

    def all_types(self) -> list[TypeDecl]:
        found: list[TypeDecl] = []

        def walk(declarations: list[TypeDecl]) -> None:
            for declaration in declarations:
                found.append(declaration)
                walk(declaration.nested)

        walk(self.types)
        return found


def _is_op(token: Token, text: str) -> bool:
    return token.kind == "op" and token.text == text


def _match(tokens: list[Token], index: int, opening: str, closing: str, end: int, path: str) -> int:
    depth = 0
    position = index
    while position < end:
        token = tokens[position]
        if token.kind == "op":
            if token.text == opening:
                depth += 1
            elif token.text == closing:
                depth -= 1
                if depth == 0:
                    return position
        position += 1
    raise SourceModelError(f"{path}:{tokens[index].line}: unbalanced '{opening}'")


def _split_items(tokens: list[Token], start: int, end: int, path: str) -> list[tuple[int, int]]:
    """Split a declaration region into top-level items at `;` or a closing block."""
    items: list[tuple[int, int]] = []
    position = start
    while position < end:
        item_start = position
        depth = 0
        assigned = False
        terminated = False
        while position < end:
            token = tokens[position]
            text = token.text if token.kind == "op" else None
            if text in ("(", "["):
                depth += 1
            elif text in (")", "]"):
                depth -= 1
            elif text == "{":
                if depth == 0 and not assigned:
                    close = _match(tokens, position, "{", "}", end, path)
                    position = close + 1
                    if position < end and _is_op(tokens[position], "="):
                        assigned = True
                        continue
                    if position < end and _is_op(tokens[position], ";"):
                        position += 1
                    terminated = True
                    break
                depth += 1
            elif text == "}":
                depth -= 1
            elif depth == 0 and text in ("=", "=>"):
                assigned = True
            elif depth == 0 and text == ";":
                position += 1
                terminated = True
                break
            position += 1
        if not terminated:
            raise SourceModelError(f"{path}:{tokens[item_start].line}: unterminated declaration")
        items.append((item_start, position))
    return items


def _attributes(tokens: list[Token], position: int, end: int, path: str) -> tuple[int, list[str]]:
    names: list[str] = []
    while position < end and _is_op(tokens[position], "["):
        close = _match(tokens, position, "[", "]", end, path)
        segment: list[Token] = []
        depth = 0
        for token in tokens[position + 1:close] + [Token("op", ",", 0, ())]:
            if token.kind == "op" and token.text in ("(", "["):
                depth += 1
            elif token.kind == "op" and token.text in (")", "]"):
                depth -= 1
            if depth == 0 and _is_op(token, ","):
                identifiers = []
                for item in segment:
                    if _is_op(item, "("):
                        break
                    if item.kind == "id":
                        identifiers.append(item.text)
                if identifiers:
                    name = identifiers[-1]
                    names.append(name[:-len("Attribute")] if name.endswith("Attribute") and name != "Attribute" else name)
                segment = []
                continue
            if not (_is_op(token, ":") and depth == 0):
                segment.append(token)
            else:
                segment = []  # attribute target specifier, such as `return:`
        position = close + 1
    return position, names


def _block_start(tokens: list[Token], start: int, end: int) -> int | None:
    depth = 0
    for position in range(start, end):
        token = tokens[position]
        if token.kind != "op":
            continue
        if token.text in ("(", "["):
            depth += 1
        elif token.text in (")", "]"):
            depth -= 1
        elif depth == 0 and token.text in ("=", "=>"):
            return None
        elif depth == 0 and token.text == "{":
            return position
    return None


def _parse_items(source: SourceFile, start: int, end: int, namespace: str,
                 outer: TypeDecl | None) -> tuple[list[TypeDecl], list[Member]]:
    tokens = source.tokens
    types: list[TypeDecl] = []
    members: list[Member] = []
    for item_start, item_end in _split_items(tokens, start, end, source.path):
        position, attributes = _attributes(tokens, item_start, item_end, source.path)
        modifiers: set[str] = set()
        while position < item_end and tokens[position].kind == "id" and tokens[position].text in MODIFIERS:
            if tokens[position].text == "new" and position + 1 < item_end and _is_op(tokens[position + 1], "("):
                break
            modifiers.add(tokens[position].text)
            position += 1
        if position >= item_end:
            continue
        head = tokens[position]
        if head.kind == "id" and head.text in TYPE_KEYWORDS and not (
                head.text == "record" and position + 1 < item_end and _is_op(tokens[position + 1], "(")):
            types.append(_parse_type(source, position, item_start, item_end, frozenset(modifiers),
                                     tuple(attributes), namespace, outer))
            continue
        if head.kind == "op" and head.text == ";":
            continue
        members.append(_parse_member(source, position, item_start, item_end, frozenset(modifiers),
                                     tuple(attributes), outer))
    return types, members


def _parse_type(source: SourceFile, position: int, item_start: int, item_end: int,
                modifiers: frozenset[str], attributes: tuple[str, ...], namespace: str,
                outer: TypeDecl | None) -> TypeDecl:
    tokens = source.tokens
    kind = tokens[position].text
    position += 1
    if kind == "record" and position < item_end and tokens[position].kind == "id" and tokens[position].text in ("class", "struct"):
        kind = f"record {tokens[position].text}"
        position += 1
    if kind == "delegate":
        parenthesis = next((index for index in range(position, item_end) if _is_op(tokens[index], "(")), None)
        if parenthesis is None:
            raise SourceModelError(f"{source.path}:{tokens[position].line}: malformed delegate")
        name_index = parenthesis - 1
        while name_index > position and tokens[name_index].kind != "id":
            name_index -= 1
        name = tokens[name_index].text
    else:
        if position >= item_end or tokens[position].kind != "id":
            raise SourceModelError(f"{source.path}:{tokens[item_start].line}: malformed type declaration")
        name = tokens[position].text
    block = _block_start(tokens, position, item_end) if kind not in ("delegate",) else None
    header_end = block if block is not None else item_end
    bases: list[str] = []
    depth = 0
    in_bases = False
    for index in range(position + 1, header_end):
        token = tokens[index]
        if token.kind == "op" and token.text in ("(", "<", "["):
            depth += 1
        elif token.kind == "op" and token.text in (")", ">", "]"):
            depth -= 1
        elif depth == 0 and _is_op(token, ":") and not in_bases:
            in_bases = True
            continue
        if in_bases and token.kind == "id":
            if token.text == "where" and depth == 0:
                break
            bases.append(token.text)
    full_name = (outer.full_name + "+" + name) if outer is not None else (f"{namespace}.{name}" if namespace else name)
    declaration = TypeDecl(name=name, full_name=full_name, kind=kind, start=item_start, end=item_end,
                           modifiers=modifiers, bases=tuple(bases), attributes=attributes,
                           line=tokens[item_start].line, file=source, outer=outer)
    if block is not None and kind != "enum":
        close = _match(tokens, block, "{", "}", item_end, source.path)
        nested, members = _parse_items(source, block + 1, close, namespace, declaration)
        declaration.nested = nested
        declaration.members = members
    return declaration


def _parse_member(source: SourceFile, position: int, item_start: int, item_end: int,
                  modifiers: frozenset[str], attributes: tuple[str, ...],
                  outer: TypeDecl | None) -> Member:
    tokens = source.tokens
    path = source.path
    line = tokens[item_start].line
    depth = 0
    first_assign = first_arrow = first_block = None
    method_paren = None
    index = position
    while index < item_end:
        token = tokens[index]
        if token.kind == "op":
            if token.text == "(" and depth == 0 and method_paren is None and first_assign is None \
                    and first_arrow is None and first_block is None:
                name_index = index - 1
                if name_index >= position and _is_op(tokens[name_index], ">"):
                    angle = 0
                    while name_index >= position:
                        if _is_op(tokens[name_index], ">"):
                            angle += 1
                        elif _is_op(tokens[name_index], "<"):
                            angle -= 1
                            if angle == 0:
                                name_index -= 1
                                break
                        name_index -= 1
                if name_index >= position and (
                        (tokens[name_index].kind == "id" and tokens[name_index].text not in MODIFIERS)
                        or (name_index - 1 >= position and tokens[name_index - 1].kind == "id"
                            and tokens[name_index - 1].text == "operator")):
                    method_paren = index
                close = _match(tokens, index, "(", ")", item_end, path)
                index = close + 1
                continue
            if token.text in ("(", "["):
                depth += 1
            elif token.text in (")", "]"):
                depth -= 1
            elif depth == 0 and token.text == "=" and first_assign is None:
                first_assign = index
            elif depth == 0 and token.text == "=>" and first_arrow is None:
                first_arrow = index
            elif depth == 0 and token.text == "{" and first_block is None and first_assign is None \
                    and first_arrow is None:
                first_block = index
                close = _match(tokens, index, "{", "}", item_end, path)
                index = close + 1
                continue
        index += 1
    if method_paren is not None:
        name_token = tokens[method_paren - 1]
        if _is_op(name_token, ">"):
            angle = 0
            name_index = method_paren - 1
            while name_index >= position:
                if _is_op(tokens[name_index], ">"):
                    angle += 1
                elif _is_op(tokens[name_index], "<"):
                    angle -= 1
                    if angle == 0:
                        break
                name_index -= 1
            name_token = tokens[name_index - 1]
        name = name_token.text
        if name_token.kind == "op" or (method_paren - 2 >= position and tokens[method_paren - 2].text == "operator"):
            name = "operator" + name_token.text
        kind = "method"
        if outer is not None and name == outer.name and (method_paren - 1 == position or tokens[method_paren - 2].text in MODIFIERS or _is_op(tokens[method_paren - 2], "~")):
            kind = "ctor"
        if name == "this":
            kind = "property"
        is_extension = (method_paren + 1 < item_end and tokens[method_paren + 1].kind == "id"
                        and tokens[method_paren + 1].text == "this")
        return Member(name=name, kind=kind, start=item_start, end=item_end, attributes=attributes,
                      line=line, modifiers=modifiers, is_extension=is_extension)
    if first_arrow is not None and (first_assign is None or first_arrow < first_assign):
        name_index = first_arrow - 1
        if tokens[name_index].kind != "id":
            raise SourceModelError(f"{path}:{line}: malformed expression-bodied member")
        return Member(name=tokens[name_index].text, kind="property", start=item_start, end=item_end,
                      attributes=attributes, line=line, modifiers=modifiers)
    if first_block is not None:
        name_index = first_block - 1
        if tokens[name_index].kind != "id":
            raise SourceModelError(f"{path}:{line}: malformed property declaration")
        return Member(name=tokens[name_index].text, kind="property", start=item_start, end=item_end,
                      attributes=attributes, line=line, modifiers=modifiers,
                      has_initializer=first_assign is not None)
    stop = first_assign if first_assign is not None else item_end - 1
    name_index = stop - 1
    while name_index >= position and tokens[name_index].kind != "id":
        name_index -= 1
    if name_index < position:
        raise SourceModelError(f"{path}:{line}: malformed field declaration")
    return Member(name=tokens[name_index].text, kind="field", start=item_start, end=item_end,
                  attributes=attributes, line=line, modifiers=modifiers,
                  has_initializer=first_assign is not None)


def parse_source(path: str, content: bytes, *, strict: bool) -> SourceFile:
    """Parse one C# compilation unit into namespace, usings, and declarations."""
    try:
        text = content.decode("utf-8-sig")
    except UnicodeDecodeError as error:
        raise SourceModelError(f"{path}: source is not UTF-8") from error
    tokens = tokenize(text, path, strict=strict)
    source = SourceFile(path=path, sha256=sha256_bytes(content), tokens=tokens, namespace="",
                        usings=(), aliases={}, types=[])
    usings: list[str] = []
    position = 0
    length = len(tokens)
    namespace = ""
    body_start, body_end = None, length
    while position < length:
        token = tokens[position]
        if token.kind == "id" and token.text == "global" and position + 1 < length and tokens[position + 1].text == "using":
            if strict:
                raise SourceModelError(f"{path}:{token.line}: global using directives are unsupported")
            position += 1
            continue
        if token.kind == "id" and token.text == "using" and not (position + 1 < length and _is_op(tokens[position + 1], "(")):
            end = next((index for index in range(position, length) if _is_op(tokens[index], ";")), None)
            if end is None:
                raise SourceModelError(f"{path}:{token.line}: unterminated using directive")
            parts = tokens[position + 1:end]
            if parts and parts[0].text == "static":
                if strict:
                    raise SourceModelError(f"{path}:{token.line}: using static directives are unsupported")
            elif len(parts) >= 3 and parts[0].kind == "id" and _is_op(parts[1], "="):
                source.aliases[parts[0].text] = "".join(part.text for part in parts[2:])
            else:
                usings.append("".join(part.text for part in parts))
            position = end + 1
            continue
        if token.kind == "id" and token.text == "namespace":
            end = position + 1
            while end < length and (tokens[end].kind == "id" or _is_op(tokens[end], ".")):
                end += 1
            namespace = "".join(part.text for part in tokens[position + 1:end])
            if end < length and _is_op(tokens[end], ";"):
                body_start, body_end = end + 1, length
            elif end < length and _is_op(tokens[end], "{"):
                close = _match(tokens, end, "{", "}", length, path)
                inner_using = end + 1
                while inner_using < close and tokens[inner_using].text == "using":
                    stop = next(index for index in range(inner_using, close) if _is_op(tokens[index], ";"))
                    usings.append("".join(part.text for part in tokens[inner_using + 1:stop]))
                    inner_using = stop + 1
                body_start, body_end = inner_using, close
            else:
                raise SourceModelError(f"{path}:{token.line}: malformed namespace declaration")
            break
        if _is_op(token, "[") and position + 1 < length and tokens[position + 1].text in ("assembly", "module"):
            position = _match(tokens, position, "[", "]", length, path) + 1
            continue
        body_start = position
        break
    source.namespace = namespace
    source.usings = tuple(usings)
    if body_start is not None:
        source.types, stray = _parse_items(source, body_start, body_end, namespace, None)
        if stray and strict:
            raise SourceModelError(f"{path}:{tokens[stray[0].start].line}: top-level members are unsupported")
    return source


# --------------------------------------------------------------------------- catalogs


@dataclass
class ModuleCatalog:
    """Types and extension methods declared by the first-party module assemblies."""

    types: dict[str, dict[str, str]]  # namespace -> type name -> assembly
    extensions: dict[str, dict[str, set[tuple[str, str]]]]  # namespace -> method -> {(assembly, type)}
    namespaces: dict[str, str]  # namespace -> assembly

    @classmethod
    def build(cls, sources: dict[str, Iterable[SourceFile]]) -> "ModuleCatalog":
        types: dict[str, dict[str, str]] = {}
        extensions: dict[str, dict[str, set[tuple[str, str]]]] = {}
        namespaces: dict[str, str] = {}
        for assembly, files in sorted(sources.items()):
            for source in files:
                if source.namespace:
                    previous = namespaces.setdefault(source.namespace, assembly)
                    if previous != assembly:
                        raise SourceModelError(f"namespace {source.namespace} spans {previous} and {assembly}")
                for declaration in source.types:
                    types.setdefault(source.namespace, {})[declaration.name] = assembly
                for declaration in source.all_types():
                    for member in declaration.members:
                        if member.kind == "method" and member.is_extension:
                            extensions.setdefault(source.namespace, {}).setdefault(member.name, set()).add(
                                (assembly, declaration.full_name))
        return cls(types=types, extensions=extensions, namespaces=namespaces)


@dataclass
class TestCase:
    identity: str
    method: Member
    declaring_type: TypeDecl
    kind: str  # fact or theory


@dataclass
class Closure:
    members: list[tuple[TypeDecl, Member]]
    types: list[TypeDecl]
    bound: dict[str, set[str]]  # assembly -> type full names
    files: list[str]
    assertion_sites: list[str]


class ProjectModel:
    """The parsed conformance project and its closure derivation."""

    def __init__(self, files: list[SourceFile], catalog: ModuleCatalog, global_usings: tuple[str, ...]) -> None:
        self.files = sorted(files, key=lambda item: item.path)
        self.catalog = catalog
        self.global_usings = tuple(IMPLICIT_USINGS) + tuple(global_usings)
        self.types: dict[str, list[TypeDecl]] = {}
        self.extension_types: dict[str, list[TypeDecl]] = {}
        for source in self.files:
            # Only top-level types resolve by simple name across declarations; a nested
            # type is visible by simple name only inside its own declaring type.
            for declaration in source.types:
                self.types.setdefault(declaration.name, []).append(declaration)
                if "static" in declaration.modifiers:
                    for member in declaration.members:
                        if member.kind == "method" and member.is_extension:
                            self.extension_types.setdefault(member.name, []).append(declaration)

    def tests(self) -> list[TestCase]:
        cases: list[TestCase] = []
        seen: set[str] = set()
        for source in self.files:
            for declaration in source.all_types():
                for member in declaration.members:
                    attributes = TEST_ATTRIBUTES.intersection(member.attributes)
                    if not attributes:
                        continue
                    if member.kind != "method" or len(attributes) != 1:
                        raise SourceModelError(f"{source.path}:{member.line}: unsupported test declaration {member.name}")
                    if declaration.outer is not None or "abstract" in declaration.modifiers or declaration.kind != "class":
                        raise SourceModelError(f"{source.path}:{member.line}: tests must be declared by a top-level concrete class")
                    identity = f"{declaration.full_name}.{member.name}"
                    if identity in seen:
                        raise SourceModelError(f"{source.path}:{member.line}: overloaded test method {identity}")
                    seen.add(identity)
                    cases.append(TestCase(identity=identity, method=member, declaring_type=declaration,
                                          kind="theory" if "Theory" in attributes else "fact"))
        for source in self.files:
            for declaration in source.all_types():
                for base in declaration.bases:
                    for candidate in self.types.get(base, []):
                        if any(TEST_ATTRIBUTES.intersection(member.attributes) for member in candidate.members):
                            raise SourceModelError(f"{source.path}: inherited test classes are unsupported ({declaration.full_name})")
        return sorted(cases, key=lambda case: case.identity)

    def _identifiers(self, source: SourceFile, start: int, end: int) -> list[tuple[str, str]]:
        """Identifier references as (text, context): plain, member, or qualified chain."""
        found: list[tuple[str, str]] = []
        tokens = source.tokens
        for index in range(start, end):
            token = tokens[index]
            if token.kind == "id":
                previous = tokens[index - 1] if index > start else None
                context = "member" if previous is not None and previous.kind == "op" and previous.text in (".", "?.") else "plain"
                found.append((token.text, context))
            elif token.kind == "str":
                found.extend((identifier, "plain") for identifier in token.inner)
        return found

    def _bind(self, source: SourceFile, start: int, end: int, bound: dict[str, set[str]]) -> None:
        tokens = source.tokens
        visible = set(self.global_usings) | set(source.usings)
        parts = source.namespace.split(".") if source.namespace else []
        visible.update(".".join(parts[:count]) for count in range(1, len(parts) + 1))
        catalog = self.catalog
        index = start
        while index < end:
            token = tokens[index]
            identifiers: list[tuple[str, bool]] = []
            if token.kind == "str":
                identifiers = [(identifier, False) for identifier in token.inner]
            elif token.kind == "id":
                previous = tokens[index - 1] if index > start else None
                after_member = previous is not None and previous.kind == "op" and previous.text in (".", "?.")
                if not after_member:
                    chain = [token.text]
                    cursor = index + 1
                    while cursor + 1 < end and _is_op(tokens[cursor], ".") and tokens[cursor + 1].kind == "id":
                        chain.append(tokens[cursor + 1].text)
                        cursor += 2
                    if chain[0] == "global" and cursor + 1 < end and _is_op(tokens[cursor], "::"):
                        chain = []
                    # A qualified chain may be absolute, alias-rooted, or relative to an enclosing namespace.
                    roots = [""] + [".".join(parts[:count]) + "." for count in range(1, len(parts) + 1)]
                    for count in range(1, len(chain)):
                        relative = ".".join(chain[:count])
                        if chain[0] in source.aliases:
                            candidates = [".".join([source.aliases[chain[0]], *chain[1:count]])]
                        else:
                            candidates = [prefix + relative for prefix in roots]
                        for namespace in candidates:
                            assembly = catalog.types.get(namespace, {}).get(chain[count])
                            if assembly is not None:
                                bound.setdefault(assembly, set()).add(f"{namespace}.{chain[count]}")
                    if token.text in source.aliases:
                        target = source.aliases[token.text]
                        namespace, _, name = target.rpartition(".")
                        assembly = catalog.types.get(namespace, {}).get(name)
                        if assembly is not None:
                            bound.setdefault(assembly, set()).add(target)
                identifiers = [(token.text, after_member)]
                if index > start and _is_op(tokens[index - 1], "::") and index + 1 < end:
                    identifiers = []
            for identifier, after_member in identifiers:
                for namespace in visible:
                    if not after_member:
                        assembly = catalog.types.get(namespace, {}).get(identifier)
                        if assembly is not None:
                            bound.setdefault(assembly, set()).add(f"{namespace}.{identifier}")
                    if after_member:
                        for assembly, declaring in catalog.extensions.get(namespace, {}).get(identifier, ()):
                            bound.setdefault(assembly, set()).add(declaring)
            index += 1

    def closure(self, case: TestCase) -> Closure:
        owner = case.declaring_type
        index: dict[str, list[Member]] = {}
        for member in owner.members:
            index.setdefault(member.name, []).append(member)
        nested_names = {nested.name: nested for nested in owner.nested}
        selected: list[Member] = [case.method]
        selected_ids = {id(case.method)}
        for member in owner.members:
            if member.kind in ("ctor", "field") or (member.kind == "property" and member.has_initializer):
                if id(member) not in selected_ids:
                    selected.append(member)
                    selected_ids.add(id(member))
        whole: list[TypeDecl] = []
        whole_ids: set[int] = set()

        def include_type(declaration: TypeDecl) -> None:
            top = declaration
            while top.outer is not None:
                top = top.outer
            target = declaration if top is owner else top
            if target is owner or id(target) in whole_ids:
                return
            whole_ids.add(id(target))
            whole.append(target)

        def visit_identifiers(source: SourceFile, start: int, end: int, *, from_owner: bool) -> None:
            for identifier, context in self._identifiers(source, start, end):
                if from_owner and identifier in index:
                    for member in index[identifier]:
                        if id(member) not in selected_ids:
                            selected.append(member)
                            selected_ids.add(id(member))
                if from_owner and identifier in nested_names:
                    include_type(nested_names[identifier])
                if context == "plain":
                    for declaration in self.types.get(identifier, []):
                        include_type(declaration)
                if context == "member":
                    for declaration in self.extension_types.get(identifier, []):
                        include_type(declaration)

        for base in owner.bases:
            for declaration in self.types.get(base, []):
                include_type(declaration)
        cursor = 0
        type_cursor = 0
        while cursor < len(selected) or type_cursor < len(whole):
            if cursor < len(selected):
                member = selected[cursor]
                cursor += 1
                visit_identifiers(owner.file, member.start, member.end, from_owner=True)
                continue
            declaration = whole[type_cursor]
            type_cursor += 1
            visit_identifiers(declaration.file, declaration.start, declaration.end, from_owner=False)
            for base in declaration.bases:
                for candidate in self.types.get(base, []):
                    include_type(candidate)
        bound: dict[str, set[str]] = {}
        for member in selected:
            self._bind(owner.file, member.start, member.end, bound)
        for declaration in whole:
            self._bind(declaration.file, declaration.start, declaration.end, bound)
        sites: list[str] = []
        for member in sorted(selected, key=lambda item: item.start):
            sites.extend(assertion_sites(owner.file.tokens, member.start, member.end))
        for declaration in sorted(whole, key=lambda item: (item.file.path, item.start)):
            sites.extend(assertion_sites(declaration.file.tokens, declaration.start, declaration.end))
        files = sorted({owner.file.path, *(declaration.file.path for declaration in whole)})
        ordered_members = [(owner, member) for member in sorted(selected, key=lambda item: item.start)]
        ordered_types = sorted(whole, key=lambda item: (item.file.path, item.start))
        return Closure(members=ordered_members, types=ordered_types, bound=bound, files=files,
                       assertion_sites=sites)

    def strength(self, case: TestCase, closure: Closure, first_party: Iterable[str]) -> dict[str, object]:
        owner_tokens = case.declaring_type.file.tokens
        method_text = normalize(owner_tokens[case.method.start:case.method.end])
        members = sorted(normalize(owner_tokens[member.start:member.end])
                         for _, member in closure.members if member is not case.method)
        types = sorted(normalize(declaration.file.tokens[declaration.start:declaration.end])
                       for declaration in closure.types)
        behavior = sha256_bytes(canonical_json({"declaration": method_text, "members": members, "types": types}))
        allowed = set(first_party)
        assemblies = sorted(assembly for assembly in closure.bound if assembly in allowed)
        negatives = sum(1 for site in closure.assertion_sites if is_negative_site(site))
        return {"boundAssemblies": assemblies, "behaviorIdentity": behavior, "negativeCaseCount": negatives}


def assertion_sites(tokens: list[Token], start: int, end: int) -> list[str]:
    """Return the assertion API name of every assertion site in a token span."""
    sites: list[str] = []
    for index in range(start, end):
        token = tokens[index]
        if token.kind != "id":
            continue
        previous = tokens[index - 1] if index > start else None
        following = tokens[index + 1] if index + 1 < end else None
        if token.text in ("Should", "Assert") and following is not None and _is_op(following, ".") \
                and index + 2 < end and tokens[index + 2].kind == "id" \
                and not (previous is not None and previous.kind == "op" and previous.text in (".", "?.")):
            sites.append(f"{token.text}.{tokens[index + 2].text}")
            continue
        if token.text.startswith("Should") and token.text != "Should" and previous is not None \
                and previous.kind == "op" and previous.text in (".", "?.") and following is not None \
                and following.kind == "op" and following.text in ("(", "<"):
            sites.append(token.text)
    return sites


def is_negative_site(site: str) -> bool:
    """Whether an assertion site asserts a rejection, absence, falsity, or exclusion."""
    if site in NEGATIVE_EXACT:
        return True
    return site.startswith("ShouldNot") and site not in POSITIVE_SHOULD_NOT


def strength_sha256(material: dict[str, object]) -> str:
    """Digest of the canonical strength triple."""
    return sha256_bytes(canonical_json(material))


# =========================================================================== disposition

SCHEMA_VERSION = "hexalith.conversations.conformance-oracle-tiering-disposition.v2"
APPROVALS_SCHEMA_VERSION = "hexalith.conversations.conformance-oracle-tiering-approvals.v2"
RECEIPT_SCHEMA_VERSION = "hexalith.conversations.conformance-pre-split-capture.v1"
STORY_ID = "9.1"
CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/9.1.json"
DECISION_PATH = "docs/release-evidence/conformance-oracle-tiering-decision-v2.json"
BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
PREDECESSOR_PATH = "docs/release-evidence/story-7.4-final-record-v2.json"
PREDECESSOR_MARKDOWN_PATH = "docs/release-evidence/story-7.4-final-record-v2.md"
APPROVALS_PATH = "docs/release-evidence/conformance-oracle-tiering-approvals-v2.json"
OUTPUT_PATHS = (
    "docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json",
    "docs/release-evidence/conformance-oracle-tiering-disposition-v2.json",
    "docs/release-evidence/conformance-oracle-tiering-disposition-v2.md",
)
BASELINE_PATH = "docs/release-evidence/release-baseline-v1.json"
MANIFEST_V2_PATH = "docs/release-evidence/preservation-traceability-manifest-v2.json"
MANIFEST_RC2_PATH = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json"
LEDGER_PATH = "docs/release-evidence/removed-test-justification-ledger-reconciliation-v1.json"
REGISTER_PATH = "docs/release-evidence/at-risk-test-register-v1.json"
CONTRACTS_BASELINE_PATH = "docs/release-evidence/public-contract-shape-baseline-v1.json"
CLIENT_BASELINE_PATH = "docs/release-evidence/client-public-api-baseline-v1.json"
CI_WORKFLOW_PATH = ".github/workflows/ci.yml"
PROJECT_DIR = "tests/Hexalith.Conversations.Conformance.Tests"
PROJECT_FILE = PROJECT_DIR + "/Hexalith.Conversations.Conformance.Tests.csproj"
TESTS_PROPS = "tests/Directory.Build.props"
TEST_NAMESPACE = "Hexalith.Conversations.Conformance.Tests"
TEST_ASSEMBLY = PROJECT_DIR + "/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll"
CAPTURE_DIR = "artifacts/v9/9.1/pre-split"
RECEIPT_PATH = CAPTURE_DIR + "/receipt.json"
PROPOSAL_REVIEW_PATH = "artifacts/v9/9.1/approval-proposal.md"
VALIDATION_ADDITION_FILES = (PROJECT_DIR + "/ConformanceOracleTieringValidationTest.cs",)
SERVER_ASSEMBLY = "Hexalith.Conversations.Server"
RECLASSIFIED_SUITES = (
    "TelemetryCardinalityConformanceSuiteTest",
    "TelemetryRedactionConformanceSuiteTest",
    "ConformanceStatusConformanceSuiteTest",
)
GENERATION_METHODS = (
    f"{TEST_NAMESPACE}.AtRiskTestRegisterGenerationTest.GenerateAndSaveAtRiskTestRegisterFile",
    f"{TEST_NAMESPACE}.OracleBlindSpotAnalysisArtifactGenerationTest.GenerateAndSaveArtifactFile",
    f"{TEST_NAMESPACE}.PublicContractShapeSnapshotGenerationTest.GenerateAndSaveContractShapeSnapshotFile",
    f"{TEST_NAMESPACE}.ReleaseConformanceArtifactGenerationTest.GenerateAndSaveFixtureArtifactFile",
)
TIERS = ("portable", "module-internal")
OWNER_ROLE = "Quality owner"
BINDING_RULE = "SC-9.1 is HEAD^{commit} at final-record generation"
INVENTORY = {"id": "V9-9.1-ENTRY-v1", "sha256": "31e18fed38706bbb44e5a8059ff3ea30f00400708902694af697954b889bcdf1"}
EXPECTED_AUTHORITY = {"epic": "epic-6-authority-2026-08-03-v10",
                      "architecture": "conversations-architecture-2026-08-03-v10",
                      "sectionSha256": "5b62af3b04bff71cdf05c71a88c71ccf1c084c25ca87a506747bb4b9e0cd5358"}
DECISION_SHA256 = "1fe609f0114d52622b01b5cbaa74df799c09690f33804921961cad7a725dbcd3"
PROPOSAL_FIELDS = ("id", "sourcePath", "sourceSha256", "preSplitResultIdentity", "strengthMaterial",
                   "strengthSha256", "serverBindings", "tier", "publicReplacement", "internalTypeAndReason",
                   "rationale")
SUITE_PROPOSAL_FIELDS = ("suite", "class", "sourcePath", "sourceSha256", "releaseGateBehavior",
                         "v1FloorTestIds", "v1FloorTestIdsSha256", "currentIdentities",
                         "currentIdentitiesSha256", "membershipChanged", "tier", "rowTiers",
                         "historicalApproval", "rationale", "manifestUpdate")
CATEGORY_ORDER = ("schema", "identity", "strength", "source", "tier", "approval", "denominator", "public", "v1",
                  "render")
FAIL_CODES = {
    "CONFORMANCE_ASSERTION_MISSING": "identity", "CONFORMANCE_ASSERTION_DUPLICATE": "identity",
    "CONFORMANCE_ASSERTION_RENAMED": "identity", "CONFORMANCE_ASSERTION_UNKNOWN": "identity",
    "CONFORMANCE_DISCOVERY_UNSUPPORTED": "identity", "CONFORMANCE_ASSERTION_SOURCE_DRIFT": "source",
    "ASSERTION_STRENGTH_WEAKENED": "strength", "TIER_UNASSIGNED": "tier", "TIER_REASON_MISSING": "tier",
    "TIER_APPROVAL_MISSING": "approval", "FR20_DENOMINATOR_DRIFT": "denominator",
    "PUBLIC_CONTRACT_WIDENED": "public", "V1_ARTIFACT_DRIFT": "v1",
    "TIERING_AUTHORITY_INVALID": "schema", "TIERING_PRE_SPLIT_RESULT_INVALID": "schema",
    "TIERING_SCHEMA_INVALID": "schema", "TIERING_RENDER_DRIFT": "render",
}
BLOCKED_CODES = frozenset({"TIERING_PRE_SPLIT_RESULT_MISSING", "TIERING_CAPTURE_FAILED",
                           "TIERING_ENVIRONMENT_UNAVAILABLE"})
GIT_TIMEOUT = 120


class TieringError(Exception):
    """A stable, content-safe tiering blocker."""

    def __init__(self, code: str, detail: str) -> None:
        super().__init__(detail)
        self.code = code


class Findings:
    """Ordered blocker findings; verification reports only the first failing category."""

    def __init__(self) -> None:
        self.items: list[tuple[str, str]] = []

    def add(self, code: str, detail: str) -> None:
        if (code, detail) not in self.items:
            self.items.append((code, detail))

    def first_category(self) -> list[tuple[str, str]]:
        if not self.items:
            return []
        rank = min(CATEGORY_ORDER.index(FAIL_CODES.get(code, "schema")) for code, _ in self.items)
        return [(code, detail) for code, detail in self.items
                if CATEGORY_ORDER.index(FAIL_CODES.get(code, "schema")) == rank]


def load_record_module() -> Any:
    """Load the final-record generator for its hardened Git, TRX, and pair helpers."""
    path = Path(__file__).with_name("generate_story_record.py")
    specification = importlib_util.spec_from_file_location("story_record_for_tiering", path)
    if specification is None or specification.loader is None:
        raise TieringError("TIERING_ENVIRONMENT_UNAVAILABLE", "the final-record helpers are unavailable")
    module = importlib_util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


RECORD = load_record_module()


def git(root: Path, *arguments: str) -> bytes:
    try:
        return RECORD.run_git(root, *arguments).stdout
    except RECORD.GateError as error:
        raise TieringError("TIERING_ENVIRONMENT_UNAVAILABLE", f"git {' '.join(arguments[:3])} failed: {error}") from None


def git_long(cwd: Path, *arguments: str, timeout: int = 900) -> bytes:
    """Run a long Git operation (clone, checkout) with the hardened environment."""
    try:
        return subprocess.run(["git", "-C", str(cwd), *arguments], check=True, capture_output=True,
                              timeout=timeout, env=RECORD.git_environment()).stdout
    except (OSError, subprocess.SubprocessError) as error:
        raise TieringError("TIERING_CAPTURE_FAILED", f"git {' '.join(arguments[:2])} failed: {error}") from None


def git_optional(root: Path, *arguments: str) -> bytes | None:
    try:
        return RECORD.run_git(root, *arguments).stdout
    except RECORD.GateError:
        return None


def read_file(root: Path, relative: str) -> bytes | None:
    path = root / relative
    try:
        if path.is_symlink() or not path.is_file() or not path.resolve(strict=True).is_relative_to(root):
            return None
        return path.read_bytes()
    except (OSError, RuntimeError):
        return None


def parse_json_bytes(content: bytes | None, code: str, subject: str) -> Any:
    if content is None:
        raise TieringError(code, f"{subject} is missing")
    try:
        return json.loads(content)
    except (UnicodeDecodeError, ValueError) as error:
        raise TieringError(code, f"{subject} is not valid JSON") from error


class GitTree:
    """Read-only view of one committed tree."""

    def __init__(self, root: Path, commit: str) -> None:
        self.root = root
        self.commit = commit
        listing = git(root, "ls-tree", "-r", "-z", "--full-tree", commit)
        self.entries: dict[str, str] = {}
        for record in listing.split(b"\0"):
            if not record:
                continue
            meta, _, path = record.partition(b"\t")
            _, kind, object_id = meta.decode().split(" ")
            if kind == "blob":
                self.entries[path.decode("utf-8")] = object_id
        self._cache: dict[str, bytes] = {}

    def files(self, prefix: str) -> list[str]:
        return sorted(path for path in self.entries if path.startswith(prefix))

    def read(self, path: str) -> bytes | None:
        if path not in self.entries:
            return None
        if path not in self._cache:
            self._cache[path] = git(self.root, "cat-file", "blob", self.entries[path])
        return self._cache[path]

    def preload(self, paths: Iterable[str]) -> None:
        wanted = [path for path in paths if path in self.entries and path not in self._cache]
        if not wanted:
            return
        payload = "".join(self.entries[path] + "\n" for path in wanted).encode()
        try:
            result = subprocess.run(["git", "-C", str(self.root), "cat-file", "--batch"], input=payload,
                                    capture_output=True, check=True, timeout=GIT_TIMEOUT,
                                    env=RECORD.git_environment())
        except (OSError, subprocess.SubprocessError) as error:
            raise TieringError("TIERING_ENVIRONMENT_UNAVAILABLE", f"git cat-file failed: {error}") from None
        output = result.stdout
        position = 0
        for path in wanted:
            header_end = output.index(b"\n", position)
            size = int(output[position:header_end].split(b" ")[2])
            start = header_end + 1
            self._cache[path] = output[start:start + size]
            position = start + size + 1


class WorkTree:
    """Read-only view of the working tree: tracked and untracked non-ignored files."""

    def __init__(self, root: Path) -> None:
        self.root = root
        listing = git(root, "ls-files", "-z", "--cached", "--others", "--exclude-standard")
        self.paths = sorted({item.decode("utf-8") for item in listing.split(b"\0") if item
                             and (root / item.decode("utf-8")).is_file()})

    def files(self, prefix: str) -> list[str]:
        return [path for path in self.paths if path.startswith(prefix)]

    def read(self, path: str) -> bytes | None:
        return read_file(self.root, path)

    def preload(self, paths: Iterable[str]) -> None:
        return None


def csharp_files(tree: GitTree | WorkTree, directory: str) -> list[str]:
    return [path for path in tree.files(directory + "/")
            if path.endswith(".cs") and "/bin/" not in path and "/obj/" not in path]


def project_references(content: bytes) -> list[str]:
    return sorted(PurePosixPath(match.replace("\\", "/")).stem for match in
                  re.findall(r'<ProjectReference\s+Include="([^"]+\.csproj)"', content.decode("utf-8")))


def using_items(content: bytes | None) -> list[str]:
    if content is None:
        return []
    return re.findall(r'<Using\s+Include="([A-Za-z0-9_.]+)"\s*/>', content.decode("utf-8"))


def module_assemblies(tree: GitTree | WorkTree) -> list[dict[str, Any]]:
    """The first-party module assemblies the conformance project compiles against."""
    project = tree.read(PROJECT_FILE)
    if project is None:
        raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", "the conformance project file is missing")
    direct = project_references(project)
    pending = list(direct)
    found: dict[str, dict[str, Any]] = {}
    while pending:
        name = pending.pop()
        if name in found:
            continue
        csproj = tree.read(f"src/{name}/{name}.csproj")
        if csproj is None:
            raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", f"referenced module project is missing: {name}")
        text = csproj.decode("utf-8")
        values = re.findall(r"<IsPackable>(true|false)</IsPackable>", text)
        if len(values) != 1:
            raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", f"module packability is not declared once: {name}")
        found[name] = {"name": name, "project": f"src/{name}/{name}.csproj", "packable": values[0] == "true",
                       "directReference": name in direct}
        pending.extend(reference for reference in project_references(csproj)
                       if reference.startswith("Hexalith.Conversations"))
    return [found[name] for name in sorted(found)]


@dataclass
class SourceView:
    model: ProjectModel
    files: dict[str, SourceFile]
    assemblies: list[dict[str, Any]]


def build_view(tree: GitTree | WorkTree) -> SourceView:
    assemblies = module_assemblies(tree)
    test_paths = csharp_files(tree, PROJECT_DIR)
    module_paths = {item["name"]: csharp_files(tree, f"src/{item['name']}") for item in assemblies}
    tree.preload([*test_paths, *(path for paths in module_paths.values() for path in paths)])
    try:
        files = [parse_source(path, tree.read(path) or b"", strict=True) for path in test_paths]
        modules = {name: [parse_source(path, tree.read(path) or b"", strict=False) for path in paths]
                   for name, paths in module_paths.items()}
        catalog = ModuleCatalog.build(modules)
        usings = tuple(sorted(set(using_items(tree.read(TESTS_PROPS)) + using_items(tree.read(PROJECT_FILE)))))
        model = ProjectModel(files, catalog, usings)
    except SourceModelError as error:
        raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", str(error)) from None
    return SourceView(model=model, files={item.path: item for item in files}, assemblies=assemblies)


# --------------------------------------------------------------------------- pre-split capture


@dataclass
class PreSplit:
    receipt: dict[str, Any]
    receipt_sha256: str
    discovered: dict[str, str]  # method identity -> display name
    results: dict[str, list[dict[str, str]]]  # method identity -> TRX results
    exclusions: dict[str, list[str]]
    summary: dict[str, Any]


def ci_lane(workflow: bytes) -> dict[str, Any]:
    """Extract the exact current conformance build and run commands from ci.yml."""
    text = workflow.decode("utf-8")
    build = re.search(r"- name: Build conformance project\n\s+run: >-\n((?:\s+\S.*\n)+?)\n", text)
    run = re.search(r"- name: Run current conformance checks\n(?:.*\n)*?\s+run: \|\n((?:\s+.*\n)+?)\n\s+- name:", text)
    if build is None or run is None:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "ci.yml no longer declares the conformance lane")
    build_tokens = " ".join(line.strip() for line in build.group(1).splitlines()).split()
    run_text = run.group(1)
    classes = re.findall(r'-class- "([^"]+)"', run_text)
    methods = re.findall(r'-method- "([^"]+)"', run_text)
    if any("*" in item for item in classes + methods) or "-parallelMode none" not in run_text:
        raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", "the conformance lane uses unsupported filters")
    return {"buildCommand": build_tokens, "exclusions": {"classes": classes, "methods": methods}}


def run_logged(command: list[str], cwd: Path, log: Path, environment: dict[str, str] | None = None,
               timeout: int = 1800) -> tuple[int, bytes]:
    merged = dict(os.environ)
    if environment:
        merged.update(environment)
    try:
        result = subprocess.run(command, cwd=cwd, capture_output=True, timeout=timeout, env=merged, check=False)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise TieringError("TIERING_CAPTURE_FAILED", f"{command[0]} did not complete: {error}") from None
    log.write_bytes(result.stdout + result.stderr)
    return result.returncode, result.stdout


def capture(root: Path, freeze_revision: str, workdir: Path | None) -> dict[str, Any]:
    """Freeze the pre-split execution in an isolated clone; write only artifacts/v9/9.1/pre-split."""
    freeze = git(root, "rev-parse", "--verify", f"{freeze_revision}^{{commit}}").decode().strip()
    base = workdir or Path(tempfile.mkdtemp(prefix="hexalith-9.1-pre-split-",
                                            dir="/var/tmp" if Path("/var/tmp").is_dir() else None))
    clone = base / "conversations"
    if clone.exists():
        raise TieringError("TIERING_CAPTURE_FAILED", f"the isolated clone path already exists: {clone}")
    base.mkdir(parents=True, exist_ok=True)
    git_long(base, "clone", "--quiet", "--no-checkout", str(root), str(clone))
    git_long(clone, "checkout", "--quiet", "--detach", freeze)
    names = git(clone, "config", "--file", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$").decode().splitlines()
    git(clone, "submodule", "init")
    initialized = []
    for line in names:
        key, path = line.split(" ", 1)
        name = key[len("submodule."):-len(".path")]
        source = root / ".git" / "modules" / path
        if not source.is_dir():
            raise TieringError("TIERING_CAPTURE_FAILED", f"the local submodule repository is unavailable: {path}")
        git(clone, "config", f"submodule.{name}.url", str(source))
        initialized.append(path)
    try:
        subprocess.run(["git", "-C", str(clone), "-c", "protocol.file.allow=always", "-c", "submodule.recurse=false",
                        "submodule", "update", "--init"], check=True, capture_output=True, timeout=900,
                       env=RECORD.git_environment())
    except (OSError, subprocess.SubprocessError) as error:
        raise TieringError("TIERING_CAPTURE_FAILED", f"root submodule initialization failed: {error}") from None
    if git(clone, "status", "--porcelain").strip():
        raise TieringError("TIERING_CAPTURE_FAILED", "the isolated clone is not clean after checkout")
    workflow = git(clone, "show", f"{freeze}:{CI_WORKFLOW_PATH}")
    lane = ci_lane(workflow)
    output = root / CAPTURE_DIR
    if output.exists():
        shutil.rmtree(output)
    (output / "assemblies").mkdir(parents=True)
    pins = ["-nr:false", "-p:UseSharedCompilation=false"]
    build_command = [*lane["buildCommand"], *pins]
    if build_command[:3] != ["dotnet", "build", PROJECT_FILE]:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the conformance build command changed shape")
    build_exit, _ = run_logged(build_command, clone, output / "build.log", {"CI": "true"})
    if build_exit != 0:
        raise TieringError("TIERING_CAPTURE_FAILED", f"the isolated Release build exited {build_exit}")
    assembly = clone / TEST_ASSEMBLY
    binary = assembly.read_bytes()
    if RECORD.dotnet_source_revisions(binary) != [freeze]:
        raise TieringError("TIERING_CAPTURE_FAILED", "the built assembly is not stamped with the freeze commit")
    discovery_command = ["dotnet", TEST_ASSEMBLY, "-list", "full/json", "-noLogo", "-noColor"]
    discovery_exit, discovery = run_logged(discovery_command, clone, output / "discovery.log")
    if discovery_exit != 0:
        raise TieringError("TIERING_CAPTURE_FAILED", f"test discovery exited {discovery_exit}")
    (output / "discovery.json").write_bytes(discovery)
    result_relative = "TestResults/conformance/conformance-results.trx"
    run_command = ["dotnet", TEST_ASSEMBLY]
    for item in lane["exclusions"]["classes"]:
        run_command.extend(["-class-", item])
    for item in lane["exclusions"]["methods"]:
        run_command.extend(["-method-", item])
    run_command.extend(["-parallelMode", "none", "-result-trx", result_relative, "-noLogo"])
    (clone / "TestResults/conformance").mkdir(parents=True, exist_ok=True)
    run_exit, _ = run_logged(run_command, clone, output / "run.log")
    trx = (clone / result_relative).read_bytes()
    (output / "conformance.trx").write_bytes(trx)
    writes = []
    for line in git(clone, "status", "--porcelain", "--untracked-files=all").decode().splitlines():
        path = line[3:]
        if path.startswith("TestResults/"):
            continue
        before = git_optional(clone, "show", f"{freeze}:{path}")
        after = read_file(clone, path)
        writes.append({"path": path, "beforeSha256": sha256_bytes(before) if before is not None else None,
                       "afterSha256": sha256_bytes(after) if after is not None else None})
    assemblies = []
    for item in module_assemblies(GitTree(root, freeze)):
        name = item["name"] + ".dll"
        content = (assembly.parent / name).read_bytes()
        (output / "assemblies" / name).write_bytes(content)
        assemblies.append({"name": item["name"], "sha256": sha256_bytes(content),
                           "retainedCopy": f"{CAPTURE_DIR}/assemblies/{name}"})
    (output / "assemblies" / assembly.name).write_bytes(binary)
    parsed = RECORD.parse_trx(trx)
    sdk = subprocess.run(["dotnet", "--version"], cwd=clone, capture_output=True, check=False).stdout.decode().strip()

    def bound(relative: str) -> dict[str, str]:
        return {"path": f"{CAPTURE_DIR}/{relative}", "sha256": sha256_bytes((output / relative).read_bytes())}

    receipt = {
        "schemaVersion": RECEIPT_SCHEMA_VERSION,
        "storyId": STORY_ID,
        "freezeCommit": freeze,
        "freezeTree": git(root, "rev-parse", f"{freeze}^{{tree}}").decode().strip(),
        "isolation": {"mode": "isolated-clone", "clonePath": str(clone), "rootSubmodules": sorted(initialized),
                      "nestedSubmodulesInitialized": False, "trackedWrites": writes,
                      "repositoryWritesOutsideCapture": False},
        "lane": {"workflow": {"path": CI_WORKFLOW_PATH, "sha256": sha256_bytes(workflow)},
                 "job": "ci / conformance", "environment": {"CI": "true"}, "environmentPins": pins,
                 "buildCommand": build_command, "runCommand": run_command,
                 "exclusions": lane["exclusions"]},
        "sdkVersion": sdk,
        "build": {"exitCode": build_exit, "log": bound("build.log")},
        "testAssembly": {"path": TEST_ASSEMBLY, "sha256": sha256_bytes(binary),
                         "sourceRevisionIds": [freeze],
                         "retainedCopy": f"{CAPTURE_DIR}/assemblies/{assembly.name}"},
        "moduleAssemblies": assemblies,
        "discovery": {"command": discovery_command, "exitCode": discovery_exit, "result": bound("discovery.json"),
                      "log": bound("discovery.log")},
        "run": {"command": run_command, "exitCode": run_exit, "result": bound("conformance.trx"),
                "log": bound("run.log"), "reported": parsed["reported"],
                "codeBase": str(assembly)},
        "capturedAtUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    }
    (output / "receipt.json").write_bytes((json.dumps(receipt, indent=2, ensure_ascii=False) + "\n").encode())
    return receipt


def load_pre_split(root: Path) -> PreSplit:
    """Load and verify the retained pre-split machine result."""
    receipt_bytes = read_file(root, RECEIPT_PATH)
    if receipt_bytes is None:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_MISSING", f"the retained pre-split receipt is missing: {RECEIPT_PATH}")
    receipt = parse_json_bytes(receipt_bytes, "TIERING_PRE_SPLIT_RESULT_INVALID", "the pre-split receipt")
    try:
        if receipt["schemaVersion"] != RECEIPT_SCHEMA_VERSION or receipt["storyId"] != STORY_ID:
            raise KeyError("schema")
        for binding in (receipt["discovery"]["result"], receipt["run"]["result"], receipt["build"]["log"],
                        receipt["run"]["log"]):
            content = read_file(root, binding["path"])
            if content is None or sha256_bytes(content) != binding["sha256"]:
                raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", f"retained evidence changed: {binding['path']}")
        for item in [receipt["testAssembly"], *receipt["moduleAssemblies"]]:
            content = read_file(root, item["retainedCopy"])
            if content is None or sha256_bytes(content) != item["sha256"]:
                raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", f"retained assembly changed: {item['retainedCopy']}")
        if receipt["build"]["exitCode"] != 0 or receipt["discovery"]["exitCode"] != 0:
            raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the pre-split build or discovery failed")
        if receipt["testAssembly"]["sourceRevisionIds"] != [receipt["freezeCommit"]]:
            raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the pre-split assembly is not freeze-stamped")
        discovery = json.loads(read_file(root, receipt["discovery"]["result"]["path"]))
        trx = read_file(root, receipt["run"]["result"]["path"])
        parsed = RECORD.parse_trx(trx)
    except TieringError:
        raise
    except (KeyError, TypeError, ValueError, AttributeError) as error:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", f"the pre-split receipt is malformed: {error}") from None
    if RECORD.count_disagreements(parsed) or parsed["code_bases"] != [receipt["run"]["codeBase"]]:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the pre-split TRX counters or code base disagree")
    if parsed["reported"]["total"] == 0 or parsed["reported"]["skipped"]:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the pre-split TRX is empty or skipped tests")
    discovered: dict[str, str] = {}
    for item in discovery:
        identity = f"{item['Class']}.{item['Method']}"
        if identity in discovered or item.get("Explicit") or "Skip" in item:
            raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", f"unsupported discovery entry: {identity}")
        discovered[identity] = item["DisplayName"]
    results: dict[str, list[dict[str, str]]] = {identity: [] for identity in discovered}
    for row in parsed["results"]:
        name = row["test"]
        owners = [identity for identity in discovered if name == identity or name.startswith(identity + "(")]
        if len(owners) != 1:
            raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", f"a pre-split result maps to {len(owners)} methods: {name}")
        results[owners[0]].append({"testName": name, "outcome": row["outcome"]})
    for rows in results.values():
        rows.sort(key=lambda row: row["testName"])
    reported = parsed["reported"]
    failed = sorted(row["testName"] for rows in results.values() for row in rows if row["outcome"] != "Passed")
    summary = {
        "total": reported["total"], "executed": reported["executed"], "passed": reported["passed"],
        "failed": reported["failed"], "skipped": reported["skipped"], "notRun": 0,
        "exitCode": receipt["run"]["exitCode"], "failedTests": failed,
    }
    return PreSplit(receipt=receipt, receipt_sha256=sha256_bytes(receipt_bytes), discovered=discovered,
                    results=results, exclusions=receipt["lane"]["exclusions"], summary=summary)


def pre_split_from_document(document: dict[str, Any]) -> PreSplit:
    """Rebuild the frozen machine facts from a committed disposition (read-only verification)."""
    try:
        result = document["preSplitResult"]
        discovered = {}
        results = {}
        for row in document["assertions"]:
            identity = row["preSplitResultIdentity"]
            discovered[row["id"]] = identity["displayName"]
            results[row["id"]] = [dict(item) for item in identity["results"]]
        return PreSplit(receipt=result["receiptFacts"], receipt_sha256=result["receipt"]["sha256"],
                        discovered=discovered, results=results, exclusions=result["lane"]["exclusions"],
                        summary=result["result"]["summary"])
    except (KeyError, TypeError) as error:
        raise TieringError("TIERING_SCHEMA_INVALID", f"the embedded pre-split result is malformed: {error}") from None


def lane_state(identity: str, exclusions: dict[str, list[str]]) -> str:
    owner = identity.rsplit(".", 1)[0]
    if owner in exclusions["classes"]:
        return "excluded-historical-class"
    if identity in exclusions["methods"]:
        return "excluded-historical-method"
    return "executed"


# --------------------------------------------------------------------------- derivation


@dataclass
class Derivation:
    document: dict[str, Any]
    findings: Findings
    membership: dict[str, Any]
    membership_sha256: str
    approvals_bytes: bytes | None


def contract_inputs(root: Path, contract_path: str, decision_path: str) -> dict[str, Any]:
    if contract_path != CONTRACT_PATH or decision_path != DECISION_PATH:
        raise TieringError("TIERING_AUTHORITY_INVALID", "the contract and decision paths must be canonical")
    contract_bytes = read_file(root, CONTRACT_PATH)
    contract = parse_json_bytes(contract_bytes, "TIERING_AUTHORITY_INVALID", "the Story 9.1 contract")
    bundle = parse_json_bytes(read_file(root, BUNDLE_PATH), "TIERING_AUTHORITY_INVALID", "the authority bundle")
    decision_bytes = read_file(root, DECISION_PATH)
    decision = parse_json_bytes(decision_bytes, "TIERING_AUTHORITY_INVALID", "the tiering decision")
    try:
        artifacts = bundle["artifacts"]
        bundle_digest = sha256_bytes("".join(f"{row['sha256']}  {row['path']}\n" for row in artifacts).encode())
        rows = [row for row in artifacts if row["path"] == CONTRACT_PATH]
        authority = contract["authority"]
        valid = (contract["schemaVersion"] == "hexalith.conversations.story-contract.v1"
                 and contract["storyId"] == STORY_ID and contract["predecessors"] == ["7.4"]
                 and authority["epic"] == EXPECTED_AUTHORITY["epic"]
                 and authority["architecture"] == EXPECTED_AUTHORITY["architecture"]
                 and authority["sectionSha256"] == EXPECTED_AUTHORITY["sectionSha256"]
                 and authority["planningCandidate"] == bundle["planningCandidate"]
                 and contract["inventory"] == INVENTORY
                 and bundle["bundleDigest"] == bundle_digest
                 and len(rows) == 1 and rows[0]["sha256"] == sha256_bytes(contract_bytes)
                 and [scenario["id"] for scenario in contract["scenarios"]] == [f"AC-9.1-0{index}" for index in range(1, 10)])
    except (KeyError, TypeError):
        valid = False
    if not valid:
        raise TieringError("TIERING_AUTHORITY_INVALID", "the Story 9.1 contract or authority bundle binding is invalid")
    if (sha256_bytes(decision_bytes) != DECISION_SHA256 or decision.get("status") != "approved"
            or decision.get("decision") != "tier-the-oracle" or decision.get("triageResults") is not None):
        raise TieringError("V1_ARTIFACT_DRIFT", "the approved tiering decision bytes changed")
    predecessor_bytes = read_file(root, PREDECESSOR_PATH)
    predecessor_markdown = read_file(root, PREDECESSOR_MARKDOWN_PATH)
    predecessor = parse_json_bytes(predecessor_bytes, "TIERING_AUTHORITY_INVALID", "the Story 7.4 record")
    try:
        candidate = predecessor["candidate"]["commit"]
        pair_valid = (predecessor["storyId"] == "7.4"
                      and predecessor["summary"] == {"required": 6, "passed": 6, "failed": 0, "blocked": 0,
                                                     "skipped": 0, "notRun": 0}
                      and all(row["result"] == "PASS" and row["exitCode"] == 0 for row in predecessor["scenarios"])
                      and not RECORD.v2_verify_pair(predecessor_bytes, predecessor_markdown or b"")
                      and git_optional(root, "cat-file", "-e", f"{candidate}^{{commit}}") is not None)
    except (KeyError, TypeError):
        pair_valid = False
    if not pair_valid:
        raise TieringError("TIERING_AUTHORITY_INVALID", "the Story 7.4 predecessor pair does not verify")
    return {"contract": contract, "contract_bytes": contract_bytes, "bundle": bundle, "bundle_digest": bundle_digest,
            "decision": decision, "decision_bytes": decision_bytes, "predecessor": predecessor,
            "predecessor_bytes": predecessor_bytes}


def proposal_digest(row: dict[str, Any], fields: tuple[str, ...]) -> str:
    return sha256_bytes(canonical_json({name: row[name] for name in fields if name in row}))


def tier_row(row: dict[str, Any]) -> None:
    """Apply the frozen mechanical tier proposal to one inventory row."""
    server = row["serverBindings"]
    surfaces = [assembly for assembly in row["strengthMaterial"]["boundAssemblies"] if assembly != SERVER_ASSEMBLY]
    if server:
        row["tier"] = "module-internal"
        row["internalTypeAndReason"] = {
            "types": list(server),
            "reason": ("The transitive source closure binds non-packable Hexalith.Conversations.Server "
                       "type(s) listed here. The shipped Contracts, Client, and Testing surfaces expose no "
                       "equivalent, so public re-expression would widen the public contract or weaken the "
                       "assertion; both are prohibited."),
        }
        row["rationale"] = ("Module-internal: keeps the assertion at full strength against the exact Server "
                            "types it binds, without widening a public contract.")
    else:
        row["tier"] = "portable"
        row["publicReplacement"] = {
            "expression": "unchanged-assertion",
            "surfaces": surfaces,
            "strengthSha256": row["strengthSha256"],
            "equalStrength": True,
        }
        row["rationale"] = ("Portable: the transitive source closure binds no non-packable module assembly, so "
                            "the unchanged assertion executes against shipped surfaces at equal strength.")


def derive(root: Path, contract_path: str, decision_path: str, *, pre_split: PreSplit | None = None,
           require_approval: bool = True) -> Derivation:
    """Derive the full disposition and every blocker finding from bound inputs."""
    findings = Findings()
    inputs = contract_inputs(root, contract_path, decision_path)
    if pre_split is None:
        pre_split = load_pre_split(root)
    freeze = pre_split.receipt["freezeCommit"]
    head = git_optional(root, "rev-parse", "--verify", "HEAD^{commit}")
    head = head.decode().strip() if head else None
    if git_optional(root, "cat-file", "-e", f"{freeze}^{{commit}}") is None or (
            head is not None and git_optional(root, "merge-base", "--is-ancestor", freeze, head) is None):
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the freeze commit is not an ancestor of HEAD")
    frozen_tree = GitTree(root, freeze)
    workflow = frozen_tree.read(CI_WORKFLOW_PATH)
    if workflow is None or sha256_bytes(workflow) != pre_split.receipt["lane"]["workflow"]["sha256"] \
            or ci_lane(workflow)["exclusions"] != pre_split.exclusions:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the bound CI lane differs from the freeze commit")
    frozen = build_view(frozen_tree)
    current = build_view(WorkTree(root))
    first_party = [item["name"] for item in frozen.assemblies]
    nonpackable = [item["name"] for item in frozen.assemblies if not item["packable"]]
    if nonpackable != [SERVER_ASSEMBLY]:
        raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", f"unexpected non-packable module set: {nonpackable}")
    frozen_tests = {case.identity: case for case in frozen.model.tests()}
    current_tests = {case.identity: case for case in current.model.tests()}
    if set(frozen_tests) != set(pre_split.discovered):
        missing = sorted(set(pre_split.discovered) - set(frozen_tests))
        extra = sorted(set(frozen_tests) - set(pre_split.discovered))
        raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED",
                           f"source and discovery identities differ: discovery-only {missing[:3]}, source-only {extra[:3]}")
    for identity, case in frozen_tests.items():
        state = lane_state(identity, pre_split.exclusions)
        rows = pre_split.results.get(identity, [])
        if (state == "executed") != bool(rows) or (case.kind == "fact" and len(rows) > 1) or \
                (rows and case.kind == "fact" and rows[0]["testName"] != identity):
            raise TieringError("CONFORMANCE_DISCOVERY_UNSUPPORTED", f"pre-split execution does not match the lane: {identity}")

    # Identity drift between the frozen inventory and the current working tree.
    additions = {identity: case for identity, case in current_tests.items()
                 if identity not in frozen_tests and case.declaring_type.file.path in VALIDATION_ADDITION_FILES}
    unknown = sorted(set(current_tests) - set(frozen_tests) - set(additions))
    missing = sorted(set(frozen_tests) - set(current_tests))
    if missing and unknown:
        findings.add("CONFORMANCE_ASSERTION_RENAMED", f"frozen {missing[0]} is absent while {unknown[0]} appeared")
    elif missing:
        findings.add("CONFORMANCE_ASSERTION_MISSING", f"frozen assertion is absent from source: {missing[0]}")
    elif unknown:
        findings.add("CONFORMANCE_ASSERTION_UNKNOWN", f"undeclared post-freeze assertion: {unknown[0]}")

    def material(view: SourceView, case: TestCase) -> tuple[dict[str, Any], Closure]:
        closure = view.model.closure(case)
        return view.model.strength(case, closure, first_party), closure

    rows: list[dict[str, Any]] = []
    for identity in sorted(frozen_tests):
        case = frozen_tests[identity]
        strength, closure = material(frozen, case)
        source = frozen.files[case.declaring_type.file.path]
        server_types = sorted(closure.bound.get(SERVER_ASSEMBLY, set()))
        results = pre_split.results.get(identity, [])
        row = {
            "id": identity,
            "kind": case.kind,
            "sourcePath": source.path,
            "sourceSha256": source.sha256,
            "sourceLine": case.method.line,
            "preSplitResultIdentity": {
                "discovered": True,
                "displayName": pre_split.discovered[identity],
                "lane": lane_state(identity, pre_split.exclusions),
                "results": results,
                "executed": len(results),
                "passed": sum(1 for item in results if item["outcome"] == "Passed"),
                "failed": sum(1 for item in results if item["outcome"] != "Passed"),
            },
            "strengthMaterial": strength,
            "strengthSha256": strength_sha256(strength),
            "assertionSiteCount": len(closure.assertion_sites),
            "closureFiles": [{"path": path, "sha256": frozen.files[path].sha256} for path in closure.files],
            "serverBindings": server_types,
        }
        tier_row(row)
        rows.append(row)
        if identity in current_tests:
            current_strength, _ = material(current, current_tests[identity])
            if current_strength != strength:
                findings.add("ASSERTION_STRENGTH_WEAKENED", f"current strength differs from the frozen material: {identity}")
            elif any(current.files.get(path) is None or current.files[path].sha256 != frozen.files[path].sha256
                     for path in closure.files):
                findings.add("CONFORMANCE_ASSERTION_SOURCE_DRIFT", f"a frozen closure source changed: {identity}")
    addition_rows: list[dict[str, Any]] = []
    for identity in sorted(additions):
        case = additions[identity]
        strength, closure = material(current, case)
        source = current.files[case.declaring_type.file.path]
        row = {
            "id": identity, "kind": case.kind, "sourcePath": source.path, "sourceSha256": source.sha256,
            "sourceLine": case.method.line,
            "preSplitResultIdentity": {"discovered": False, "displayName": identity,
                                       "lane": "post-freeze-validation-addition", "results": [],
                                       "executed": 0, "passed": 0, "failed": 0},
            "strengthMaterial": strength, "strengthSha256": strength_sha256(strength),
            "assertionSiteCount": len(closure.assertion_sites),
            "closureFiles": [{"path": path, "sha256": current.files[path].sha256} for path in closure.files],
            "serverBindings": sorted(closure.bound.get(SERVER_ASSEMBLY, set())),
        }
        tier_row(row)
        addition_rows.append(row)

    by_id = {row["id"]: row for row in rows}
    suites, membership_section = denominator(root, inputs, by_id, pre_split, findings)
    public = public_contract(root, frozen_tree, freeze, by_id, findings)
    supersedes = supersession(root, inputs, frozen_tree, findings)
    triage = triage_outcomes(root, rows)
    result_binding = {
        "sourceCommit": freeze,
        "sourceTree": pre_split.receipt["freezeTree"],
        "sourceFiles": [{"path": path, "sha256": frozen.files[path].sha256} for path in sorted(frozen.files)],
        "lane": {"workflow": pre_split.receipt["lane"]["workflow"], "job": pre_split.receipt["lane"]["job"],
                 "environment": pre_split.receipt["lane"]["environment"],
                 "environmentPins": pre_split.receipt["lane"]["environmentPins"],
                 "buildCommand": pre_split.receipt["lane"]["buildCommand"],
                 "runCommand": pre_split.receipt["lane"]["runCommand"],
                 "exclusions": pre_split.exclusions,
                 "historicalExclusionPolicy": ("Excluded classes and methods assert retired authority or mandatory "
                                               "final-record policy; their identities are frozen here without a "
                                               "current pass claim.")},
        "build": {"configuration": "Release", "exitCode": pre_split.receipt["build"]["exitCode"],
                  "testAssembly": {"path": pre_split.receipt["testAssembly"]["path"],
                                   "sha256": pre_split.receipt["testAssembly"]["sha256"],
                                   "sourceRevisionId": freeze},
                  "moduleAssemblies": [{"name": item["name"], "sha256": item["sha256"]}
                                       for item in pre_split.receipt["moduleAssemblies"]]},
        "discovery": {"path": pre_split.receipt["discovery"]["result"]["path"],
                      "sha256": pre_split.receipt["discovery"]["result"]["sha256"],
                      "testMethodCount": len(pre_split.discovered)},
        "result": {"path": pre_split.receipt["run"]["result"]["path"],
                   "sha256": pre_split.receipt["run"]["result"]["sha256"], "summary": pre_split.summary},
        "receipt": {"path": RECEIPT_PATH, "sha256": pre_split.receipt_sha256},
        "receiptFacts": {key: pre_split.receipt[key] for key in
                         ("freezeCommit", "freezeTree", "lane", "build", "testAssembly", "moduleAssemblies",
                          "discovery", "run", "isolation", "sdkVersion", "schemaVersion", "storyId",
                          "capturedAtUtc")},
        "counts": {
            "discoveredMethods": len(pre_split.discovered),
            "executedMethods": sum(1 for row in rows if row["preSplitResultIdentity"]["lane"] == "executed"),
            "excludedMethods": sum(1 for row in rows if row["preSplitResultIdentity"]["lane"] != "executed"),
            "theoryMethods": sum(1 for row in rows if row["kind"] == "theory"),
            "executedTestCases": sum(row["preSplitResultIdentity"]["executed"] for row in rows),
        },
        "identitySha256": sha256_bytes(canonical_json([row["id"] for row in rows])),
        "generationIsolation": {"methods": list(GENERATION_METHODS),
                                "mode": pre_split.receipt["isolation"]["mode"],
                                "trackedWritesObserved": pre_split.receipt["isolation"]["trackedWrites"],
                                "repositoryWritesOutsideCapture": False},
    }
    membership = {
        "storyId": STORY_ID,
        "sourceCommit": freeze,
        "preSplitResultSha256": result_binding["result"]["sha256"],
        "decisionSha256": sha256_bytes(inputs["decision_bytes"]),
        "contractSha256": sha256_bytes(inputs["contract_bytes"]),
        "assertions": [[row["id"], proposal_digest(row, PROPOSAL_FIELDS)] for row in rows],
        "validationAdditions": [[row["id"], proposal_digest(row, PROPOSAL_FIELDS)] for row in addition_rows],
        "denominatorSuites": [[suite["suite"], proposal_digest(suite, SUITE_PROPOSAL_FIELDS)] for suite in suites],
    }
    membership_sha = sha256_bytes(canonical_json(membership))
    approvals_bytes = read_file(root, APPROVALS_PATH)
    approval_section = approval_binding(approvals_bytes, membership, membership_sha, inputs, findings,
                                        require_approval=require_approval)
    owner = approval_section["ownerLabel"] if approval_section else None
    for row in [*rows, *addition_rows]:
        digest = proposal_digest(row, PROPOSAL_FIELDS)
        row["owner"] = owner
        row["approval"] = ({"approvalId": approval_section["approvalId"], "membershipSha256": membership_sha,
                            "rowSha256": digest} if approval_section else None)
    for suite in suites:
        suite["owner"] = owner
        suite["approval"] = ({"approvalId": approval_section["approvalId"], "membershipSha256": membership_sha,
                              "rowSha256": proposal_digest(suite, SUITE_PROPOSAL_FIELDS)} if approval_section else None)
    document = {
        "schemaVersion": SCHEMA_VERSION,
        "authority": {
            "epic": inputs["contract"]["authority"]["epic"],
            "architecture": inputs["contract"]["authority"]["architecture"],
            "planningCandidate": inputs["contract"]["authority"]["planningCandidate"],
            "bundleDigest": inputs["bundle_digest"],
            "sectionSha256": inputs["contract"]["authority"]["sectionSha256"],
            "inventoryId": INVENTORY["id"], "inventorySha256": INVENTORY["sha256"],
            "contractPath": CONTRACT_PATH, "contractSha256": sha256_bytes(inputs["contract_bytes"]),
        },
        "candidate": {"storyId": STORY_ID, "bindingRule": BINDING_RULE, "freezeCommit": freeze,
                      "predecessorPath": PREDECESSOR_PATH,
                      "predecessorSha256": sha256_bytes(inputs["predecessor_bytes"]),
                      "predecessorCandidate": inputs["predecessor"]["candidate"]["commit"]},
        "decision": {"path": DECISION_PATH, "sha256": sha256_bytes(inputs["decision_bytes"]),
                     "status": inputs["decision"]["status"], "decision": inputs["decision"]["decision"],
                     "approvedBy": inputs["decision"]["approvedBy"],
                     "decisionDate": inputs["decision"]["decisionDate"],
                     "triageResultsInDecision": None,
                     "triageSuppliedBy": "this disposition; the decision bytes are preserved unchanged"},
        "moduleAssemblies": frozen.assemblies,
        "strengthDefinition": {
            "material": ["boundAssemblies", "behaviorIdentity", "negativeCaseCount"],
            "digest": "SHA-256 of canonical JSON (sorted keys, compact separators, UTF-8) of the material",
            "behaviorIdentity": ("SHA-256 of canonical JSON {declaration, members, types}: the test declaration, "
                                 "the sorted declaring-class members it closes over, and the sorted whole "
                                 "conformance-project types it closes over, each as space-joined tokens without "
                                 "comments or whitespace"),
            "closure": ("declaration; every constructor, field, and initialized property of the declaring class; "
                        "named declaring-class members; named or extension-providing conformance-project types "
                        "(whole, transitively, with base types)"),
            "boundAssemblies": "first-party module assemblies whose types or extension methods the closure names",
            "negativeSites": sorted(NEGATIVE_EXACT) + ["ShouldNot* except " + ", ".join(sorted(POSITIVE_SHOULD_NOT))],
        },
        "preSplitResult": result_binding,
        "assertions": rows,
        "validationAdditions": addition_rows,
        "denominatorSuites": suites,
        "fr20Membership": membership_section,
        "publicContract": public,
        "triage": triage,
        "approvals": approval_section["binding"] if approval_section else None,
        "supersedes": supersedes,
        "renderedMarkdownSha256": "0" * 64,
    }
    return Derivation(document=document, findings=findings, membership=membership,
                      membership_sha256=membership_sha, approvals_bytes=approvals_bytes)


def denominator(root: Path, inputs: dict[str, Any], by_id: dict[str, dict[str, Any]], pre_split: PreSplit,
                findings: Findings) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    """Bind the immutable v1 floor, the approved accumulated FR-20 membership, and the three suites."""
    baseline_bytes = read_file(root, BASELINE_PATH)
    manifest_bytes = read_file(root, MANIFEST_RC2_PATH)
    baseline = parse_json_bytes(baseline_bytes, "FR20_DENOMINATOR_DRIFT", "the v1 release baseline")
    manifest = parse_json_bytes(manifest_bytes, "FR20_DENOMINATOR_DRIFT", "the accumulated preservation manifest")
    try:
        floor = manifest["testDenominator"]["originalV1Floor"]
        later = manifest["testDenominator"]["laterApprovedAdditions"]
        floor_ids = list(floor["testIds"])
        cumulative = list(later["cumulativeTestIds"])
        suite_rows = {row["class"]: row["releaseGateBehavior"] for row in baseline["conformanceOracle"]["suiteClasses"]}
        consistent = (len(floor_ids) == 214 == baseline["conformanceOracle"]["conformanceSuiteTestCount"]
                      and floor["suiteCount"] == 14 == baseline["conformanceOracle"]["suiteClassCount"]
                      and sorted(floor["suiteClasses"]) == sorted(suite_rows)
                      and sha256_bytes(("\n".join(floor_ids) + "\n").encode()) == floor["testIdsSha256"]
                      and len(cumulative) == later["approvedCumulativeCount"] == 384
                      and sha256_bytes(("\n".join(cumulative) + "\n").encode()) == later["cumulativeTestIdsSha256"]
                      and set(floor_ids) <= set(cumulative)
                      and sha256_bytes(baseline_bytes) == "a3f0b4a76aa99226dfb6a7d9a0c930f30705c4d4f8d8c32f97a5b3124a335932")
    except (KeyError, TypeError):
        consistent = False
    if not consistent:
        raise TieringError("FR20_DENOMINATOR_DRIFT", "the FR-20 v1 floor or accumulated membership is inconsistent")
    missing_ids = []
    for identifier in cumulative:
        method = identifier.split("(", 1)[0]
        row = by_id.get(method)
        if row is None:
            missing_ids.append(identifier)
            continue
        if "(" in identifier and row["preSplitResultIdentity"]["lane"] == "executed":
            display = identifier.replace('\\"', '"')
            if display not in {item["testName"] for item in row["preSplitResultIdentity"]["results"]}:
                missing_ids.append(identifier)
    if missing_ids:
        findings.add("FR20_DENOMINATOR_DRIFT", f"accumulated FR-20 identities lack a frozen row: {missing_ids[:3]}")

    def tier_counts(identifiers: list[str]) -> dict[str, int]:
        counts = {tier: 0 for tier in TIERS}
        for identifier in identifiers:
            row = by_id.get(identifier.split("(", 1)[0])
            if row is not None:
                counts[row["tier"]] += 1
        return counts

    decision = inputs["decision"]
    historical = {
        "source": DECISION_PATH, "sha256": sha256_bytes(inputs["decision_bytes"]),
        "approvedBy": decision["fr20Reclassification"]["namedOwnerApproval"],
        "approvedOn": decision["decisionDate"],
        "scope": ("suite-level FR-20 tier reclassification of the three manifested suites only; it approves no "
                  "individual assertion row"),
        "approvesRows": False,
    }
    if decision["fr20Reclassification"]["manifestedSuitesReclassified"] != list(RECLASSIFIED_SUITES):
        raise TieringError("FR20_DENOMINATOR_DRIFT", "the decision's reclassified suite set changed")
    suites = []
    for suite in RECLASSIFIED_SUITES:
        class_name = f"{TEST_NAMESPACE}.{suite}"
        floor_members = [identifier for identifier in floor_ids if identifier.startswith(class_name + ".")]
        current_members = sorted(identity for identity in by_id if identity.startswith(class_name + "."))
        source_rows = [by_id[identity] for identity in current_members]
        if not source_rows:
            raise TieringError("FR20_DENOMINATOR_DRIFT", f"a reclassified suite has no frozen rows: {suite}")
        tiers = sorted({row["tier"] for row in source_rows})
        changed = not set(floor_members) <= set(current_members)
        if changed:
            findings.add("FR20_DENOMINATOR_DRIFT", f"a v1 floor identity left suite {suite}")
        suites.append({
            "suite": suite,
            "class": class_name,
            "sourcePath": source_rows[0]["sourcePath"],
            "sourceSha256": source_rows[0]["sourceSha256"],
            "releaseGateBehavior": suite_rows[suite],
            "v1FloorTestIds": floor_members,
            "v1FloorTestIdsSha256": sha256_bytes(canonical_json(floor_members)),
            "currentIdentities": current_members,
            "currentIdentitiesSha256": sha256_bytes(canonical_json(current_members)),
            "membershipChanged": changed,
            "tier": tiers[0] if len(tiers) == 1 else "mixed",
            "rowTiers": tier_counts(current_members),
            "historicalApproval": historical,
            "rationale": ("The decision reclassified this manifested suite's tier; denominator membership, test "
                          "identities, and strength are unchanged, and every row carries its own digest approval."),
            "manifestUpdate": {"manifest": "conformance-oracle-tiering-disposition-v2", "version": 2,
                               "path": OUTPUT_PATHS[1],
                               "previousManifest": {"path": BASELINE_PATH, "sha256": sha256_bytes(baseline_bytes)},
                               "membershipChanged": changed, "testsRemoved": 0, "testsWeakened": 0},
        })
    section = {
        "v1Floor": {"baseline": {"path": BASELINE_PATH, "sha256": sha256_bytes(baseline_bytes)},
                    "source": {"path": MANIFEST_RC2_PATH, "sha256": sha256_bytes(manifest_bytes)},
                    "sourceCommit": floor["sourceCommit"], "suiteCount": 14, "testCount": len(floor_ids),
                    "testIdsSha256": floor["testIdsSha256"], "tierCounts": tier_counts(floor_ids)},
        "approvedCumulative": {"testCount": len(cumulative), "testIdsSha256": later["cumulativeTestIdsSha256"],
                               "approvalDecision": {"path": later["approvalDecisionPath"],
                                                    "sha256": later["approvalDecisionSha256"]},
                               "tierCounts": tier_counts(cumulative)},
        "missingIdentities": missing_ids,
        "rule": ("Every v1-floor and approved accumulated FR-20 identity maps to exactly one frozen row; tiering "
                 "never deletes, replaces, aggregates, waives, or substitutes denominator coverage."),
    }
    return suites, section


PUBLIC_PROJECTS = ("Hexalith.Conversations", "Hexalith.Conversations.Client",
                   "Hexalith.Conversations.Contracts", "Hexalith.Conversations.Testing")


def surface(tree: GitTree | WorkTree) -> list[dict[str, Any]]:
    projects = []
    for name in PUBLIC_PROJECTS:
        directory = f"src/{name}/"
        paths = [path for path in tree.files(directory) if "/bin/" not in path and "/obj/" not in path]
        tree.preload(paths)
        listing = "".join(f"{path}\0{sha256_bytes(tree.read(path) or b'')}\n" for path in paths)
        projects.append({"path": directory.rstrip("/"), "fileCount": len(paths),
                         "sha256": sha256_bytes(listing.encode())})
    return projects


def public_contract(root: Path, frozen_tree: GitTree, freeze: str, by_id: dict[str, dict[str, Any]],
                    findings: Findings) -> dict[str, Any]:
    frozen_projects = surface(frozen_tree)
    current_projects = surface(WorkTree(root))
    if current_projects != frozen_projects:
        changed = [item["path"] for item, now in zip(frozen_projects, current_projects) if item != now]
        findings.add("PUBLIC_CONTRACT_WIDENED", f"public module source changed after the freeze: {changed}")
    baseline = read_file(root, CONTRACTS_BASELINE_PATH)
    client = read_file(root, CLIENT_BASELINE_PATH)
    if baseline is None or sha256_bytes(baseline) != "ebfc2f67e90ecc8a7734719c6e2673b6e8392ab2cae9956a8e98b7bf769acfca":
        findings.add("V1_ARTIFACT_DRIFT", "the pre-PC public contract shape baseline changed")
    snapshot = by_id.get(f"{TEST_NAMESPACE}.PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting")
    drift_observed = bool(snapshot and snapshot["preSplitResultIdentity"]["failed"])
    return {
        "protectedBaseline": {"path": CONTRACTS_BASELINE_PATH,
                              "sha256": sha256_bytes(baseline) if baseline else None,
                              "role": "immutable pre-PC Contracts public shape (Story 1.1)"},
        "reviewedClientBaseline": {"path": CLIENT_BASELINE_PATH,
                                   "sha256": sha256_bytes(client) if client else None},
        "freezeSurface": {"commit": freeze, "projects": frozen_projects,
                          "sha256": sha256_bytes(canonical_json(frozen_projects))},
        "preExistingDrift": {
            "observedInPreSplitResult": drift_observed,
            "evidence": ("PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting "
                         "outcome in the frozen pre-split result"),
            "attribution": "post-PC public surface changes committed before the Story 9.1 freeze",
            "approvalClaimed": False,
        },
        "rule": ("Story 9.1 may not change any public module source. The candidate's public project sources must "
                 "equal the freeze surface; any difference is PUBLIC_CONTRACT_WIDENED."),
    }


def supersession(root: Path, inputs: dict[str, Any], frozen_tree: GitTree, findings: Findings) -> dict[str, Any]:
    manifest_bytes = read_file(root, MANIFEST_V2_PATH)
    manifest = parse_json_bytes(manifest_bytes, "V1_ARTIFACT_DRIFT", "the protected v1 inventory")
    protected = []
    try:
        bindings = list(manifest["immutableV1Bindings"])
        decision_binding = [row for row in manifest["sourceBindings"] if row["path"] == DECISION_PATH]
    except (KeyError, TypeError):
        raise TieringError("V1_ARTIFACT_DRIFT", "the protected v1 inventory is malformed") from None
    for binding in bindings:
        content = read_file(root, binding["path"])
        current = sha256_bytes(content) if content is not None else None
        if current != binding["sha256"]:
            findings.add("V1_ARTIFACT_DRIFT", f"protected v1 bytes changed: {binding['path']}")
        protected.append({"path": binding["path"], "sha256": binding["sha256"], "inventory": "immutableV1Bindings"})
    if len(decision_binding) != 1 or decision_binding[0]["sha256"] != sha256_bytes(inputs["decision_bytes"]):
        findings.add("V1_ARTIFACT_DRIFT", "the decision differs from its protected binding")
    lineage = []
    for path, role in ((REGISTER_PATH, "superseded-field-owner"), (LEDGER_PATH, "triage-input")):
        content = read_file(root, path)
        if content is None:
            findings.add("V1_ARTIFACT_DRIFT", f"a v1 tiering lineage artifact is missing: {path}")
            continue
        if frozen_tree.read(path) != content:
            findings.add("V1_ARTIFACT_DRIFT", f"a v1 tiering lineage artifact changed after the freeze: {path}")
        lineage.append({"path": path, "sha256": sha256_bytes(content), "role": role})
    return {
        "decision": {"path": DECISION_PATH, "sha256": sha256_bytes(inputs["decision_bytes"]),
                     "supersededField": "at-risk-test-register-v1.projectReferenceDisposition.targetEndState",
                     "relationship": "this v2 disposition supplies the decision's per-assertion triage outside its bytes"},
        "protectedInventory": {"path": MANIFEST_V2_PATH, "sha256": sha256_bytes(manifest_bytes),
                               "fields": ["immutableV1Bindings", "sourceBindings[tier-decision]"]},
        "v1Artifacts": protected,
        "tieringLineage": lineage,
        "v1MutationAllowed": False,
    }


def triage_outcomes(root: Path, rows: list[dict[str, Any]]) -> dict[str, Any]:
    ledger_bytes = read_file(root, LEDGER_PATH)
    ledger = parse_json_bytes(ledger_bytes, "V1_ARTIFACT_DRIFT", "the residual coupling inventory")
    try:
        inventory = ledger["projectReferenceDisposition"]["residualCouplingInventory"]
    except (KeyError, TypeError):
        raise TieringError("V1_ARTIFACT_DRIFT", "the residual coupling inventory is malformed") from None
    outcomes = []
    for item in inventory:
        closing = [row for row in rows if any(entry["path"] == item["file"] for entry in row["closureFiles"])]
        outcomes.append({"file": item["file"], "namespaces": item["namespaces"], "assertions": len(closing),
                         "tierCounts": {tier: sum(1 for row in closing if row["tier"] == tier) for tier in TIERS}})
    return {"source": {"path": LEDGER_PATH, "sha256": sha256_bytes(ledger_bytes)}, "fileCount": len(inventory),
            "outcomes": outcomes}


def approval_binding(approvals_bytes: bytes | None, membership: dict[str, Any], membership_sha: str,
                     inputs: dict[str, Any], findings: Findings, *, require_approval: bool) -> dict[str, Any] | None:
    """Validate the Quality-owner decision against the derived membership digest."""
    if approvals_bytes is None:
        if require_approval:
            findings.add("TIER_APPROVAL_MISSING", f"no approvals file exists at {APPROVALS_PATH}")
        return None
    try:
        approvals = json.loads(approvals_bytes)
        proposal = approvals["proposal"]
        decision = approvals.get("decision")
    except (ValueError, KeyError, TypeError):
        findings.add("TIER_APPROVAL_MISSING", "the approvals file is malformed")
        return None
    expected_rows = [[row_id, digest] for row_id, digest in membership["assertions"]]
    if (approvals.get("schemaVersion") != APPROVALS_SCHEMA_VERSION
            or proposal.get("membershipSha256") != membership_sha
            or [[row["id"], row["rowSha256"]] for row in proposal.get("assertions", [])] != expected_rows
            or [[row["id"], row["rowSha256"]] for row in proposal.get("validationAdditions", [])] != membership["validationAdditions"]
            or [[row["suite"], row["rowSha256"]] for row in proposal.get("denominatorSuites", [])] != membership["denominatorSuites"]):
        if require_approval:
            findings.add("TIER_APPROVAL_MISSING", "the proposed membership differs from the derived per-row digests")
        return None
    if not isinstance(decision, dict) or decision.get("state") != "approved":
        if require_approval:
            findings.add("TIER_APPROVAL_MISSING", "the Quality owner has not approved the proposed membership")
        return None
    required = ("approver", "role", "approvalId", "approvedOn", "approvedMembershipSha256", "evidence")
    if any(not isinstance(decision.get(name), str) or not decision[name].strip() for name in required) \
            or decision["approvedMembershipSha256"] != membership_sha or decision["role"] != OWNER_ROLE:
        findings.add("TIER_APPROVAL_MISSING", "the owner decision does not bind the derived membership digest")
        return None
    historical = approvals.get("historicalSuiteApproval", {})
    if historical.get("approvesRows") is not False or historical.get("decisionSha256") != sha256_bytes(inputs["decision_bytes"]):
        findings.add("TIER_APPROVAL_MISSING", "the historical suite approval is not kept distinct")
        return None
    label = f"{decision['approver']} ({decision['role']})"
    return {"approvalId": decision["approvalId"], "ownerLabel": label,
            "binding": {"path": APPROVALS_PATH, "sha256": sha256_bytes(approvals_bytes),
                        "approvalId": decision["approvalId"], "approver": decision["approver"],
                        "role": decision["role"], "approvedOn": decision["approvedOn"],
                        "membershipSha256": membership_sha, "historicalSuiteApproval": historical}}


def proposal_document(derivation: Derivation, inputs_decision_bytes: bytes) -> dict[str, Any]:
    document = derivation.document
    rows = document["assertions"]
    additions = document["validationAdditions"]
    return {
        "schemaVersion": APPROVALS_SCHEMA_VERSION,
        "storyId": STORY_ID,
        "status": "pending-quality-owner-approval",
        "requestedOwnerRole": OWNER_ROLE,
        "proposal": {
            "sourceCommit": derivation.membership["sourceCommit"],
            "preSplitResultSha256": derivation.membership["preSplitResultSha256"],
            "decisionSha256": derivation.membership["decisionSha256"],
            "contractSha256": derivation.membership["contractSha256"],
            "counts": {"assertions": len(rows), "validationAdditions": len(additions),
                       "portable": sum(1 for row in rows + additions if row["tier"] == "portable"),
                       "moduleInternal": sum(1 for row in rows + additions if row["tier"] == "module-internal")},
            "membershipSha256": derivation.membership_sha256,
            "assertions": [{"id": row["id"], "tier": row["tier"], "strengthSha256": row["strengthSha256"],
                            "rowSha256": digest} for row, (_, digest) in zip(rows, derivation.membership["assertions"])],
            "validationAdditions": [{"id": row["id"], "tier": row["tier"], "strengthSha256": row["strengthSha256"],
                                     "rowSha256": digest}
                                    for row, (_, digest) in zip(additions, derivation.membership["validationAdditions"])],
            "denominatorSuites": [{"suite": suite["suite"], "tier": suite["tier"], "rowSha256": digest}
                                  for suite, (_, digest) in zip(document["denominatorSuites"],
                                                                derivation.membership["denominatorSuites"])],
        },
        "historicalSuiteApproval": dict(document["denominatorSuites"][0]["historicalApproval"],
                                        decisionPath=DECISION_PATH,
                                        decisionSha256=sha256_bytes(inputs_decision_bytes),
                                        suites=list(RECLASSIFIED_SUITES)),
        "decision": None,
    }


# --------------------------------------------------------------------------- rendering


def schema() -> dict[str, Any]:
    """The closed v2 disposition schema."""
    string = {"type": "string", "minLength": 1}
    sha = {"type": "string", "pattern": "^[0-9a-f]{64}$"}
    commit = {"type": "string", "pattern": "^[0-9a-f]{40}$"}
    path = {"type": "string", "minLength": 1,
            "pattern": r"^(?!.*(?:^|/)\.{1,2}(?:/|$))(?!.*//)[A-Za-z0-9_.](?:[A-Za-z0-9_./-]*[A-Za-z0-9_])?$"}
    count = {"type": "integer", "minimum": 0}
    strings = {"type": "array", "items": string}
    file_binding = {"type": "object", "additionalProperties": False, "required": ["path", "sha256"],
                    "properties": {"path": path, "sha256": sha}}

    def closed(properties: dict[str, Any], required: list[str] | None = None) -> dict[str, Any]:
        return {"type": "object", "additionalProperties": False,
                "required": list(properties) if required is None else required, "properties": properties}

    result_row = closed({"testName": string, "outcome": string})
    identity = closed({"discovered": {"type": "boolean"}, "displayName": string,
                       "lane": {"enum": ["executed", "excluded-historical-class", "excluded-historical-method",
                                         "post-freeze-validation-addition"]},
                       "results": {"type": "array", "items": result_row}, "executed": count, "passed": count,
                       "failed": count})
    material = closed({"boundAssemblies": strings, "behaviorIdentity": sha, "negativeCaseCount": count})
    approval = {"anyOf": [{"type": "null"},
                          closed({"approvalId": string, "membershipSha256": sha, "rowSha256": sha})]}
    row = {
        "type": "object", "additionalProperties": False,
        "required": ["id", "kind", "sourcePath", "sourceSha256", "sourceLine", "preSplitResultIdentity",
                     "strengthMaterial", "strengthSha256", "assertionSiteCount", "closureFiles", "serverBindings",
                     "tier", "owner", "approval", "rationale"],
        "properties": {
            "id": string, "kind": {"enum": ["fact", "theory"]}, "sourcePath": path, "sourceSha256": sha,
            "sourceLine": {"type": "integer", "minimum": 1}, "preSplitResultIdentity": identity,
            "strengthMaterial": material, "strengthSha256": sha, "assertionSiteCount": count,
            "closureFiles": {"type": "array", "minItems": 1, "items": file_binding},
            "serverBindings": strings, "tier": {"enum": list(TIERS)},
            "publicReplacement": closed({"expression": {"const": "unchanged-assertion"}, "surfaces": strings,
                                         "strengthSha256": sha, "equalStrength": {"const": True}}),
            "internalTypeAndReason": closed({"types": {"type": "array", "minItems": 1, "items": string},
                                             "reason": string}),
            "owner": {"anyOf": [{"type": "null"}, string]}, "approval": approval, "rationale": string,
        },
        "oneOf": [{"required": ["publicReplacement"], "not": {"required": ["internalTypeAndReason"]}},
                  {"required": ["internalTypeAndReason"], "not": {"required": ["publicReplacement"]}}],
    }
    suite_row = closed({
        "suite": string, "class": string, "sourcePath": path, "sourceSha256": sha, "releaseGateBehavior": string,
        "v1FloorTestIds": strings, "v1FloorTestIdsSha256": sha, "currentIdentities": strings,
        "currentIdentitiesSha256": sha, "membershipChanged": {"const": False}, "tier": {"enum": [*TIERS, "mixed"]},
        "rowTiers": closed({tier: count for tier in TIERS}),
        "historicalApproval": closed({"source": path, "sha256": sha, "approvedBy": string, "approvedOn": string,
                                      "scope": string, "approvesRows": {"const": False}}),
        "rationale": string,
        "manifestUpdate": closed({"manifest": string, "version": {"const": 2}, "path": path,
                                  "previousManifest": file_binding, "membershipChanged": {"const": False},
                                  "testsRemoved": {"const": 0}, "testsWeakened": {"const": 0}}),
        "owner": {"anyOf": [{"type": "null"}, string]}, "approval": approval,
    })
    tier_counts = closed({tier: count for tier in TIERS})
    nullable_sha = {"anyOf": [{"type": "null"}, sha]}
    writes = {"type": "array", "items": closed({"path": path, "beforeSha256": nullable_sha, "afterSha256": nullable_sha})}
    environment = closed({"CI": {"const": "true"}})
    exclusions = closed({"classes": strings, "methods": strings})
    reported = closed({"total": count, "executed": count, "passed": count, "failed": count, "skipped": count})
    historical = closed({"source": path, "sha256": sha, "approvedBy": string, "approvedOn": string, "scope": string,
                         "approvesRows": {"const": False}, "decisionPath": path, "decisionSha256": sha,
                         "suites": strings})
    receipt_facts = closed({
        "schemaVersion": {"const": RECEIPT_SCHEMA_VERSION}, "storyId": {"const": STORY_ID},
        "freezeCommit": commit, "freezeTree": commit,
        "lane": closed({"workflow": file_binding, "job": string, "environment": environment,
                        "environmentPins": strings, "buildCommand": strings, "runCommand": strings,
                        "exclusions": exclusions}),
        "build": closed({"exitCode": {"const": 0}, "log": file_binding}),
        "testAssembly": closed({"path": path, "sha256": sha, "sourceRevisionIds": {"type": "array", "items": commit},
                                "retainedCopy": path}),
        "moduleAssemblies": {"type": "array", "items": closed({"name": string, "sha256": sha, "retainedCopy": path})},
        "discovery": closed({"command": strings, "exitCode": {"const": 0}, "result": file_binding, "log": file_binding}),
        "run": closed({"command": strings, "exitCode": {"type": "integer"}, "result": file_binding, "log": file_binding,
                       "reported": reported, "codeBase": string}),
        "isolation": closed({"mode": {"const": "isolated-clone"}, "clonePath": string, "rootSubmodules": strings,
                             "nestedSubmodulesInitialized": {"const": False}, "trackedWrites": writes,
                             "repositoryWritesOutsideCapture": {"const": False}}),
        "sdkVersion": string, "capturedAtUtc": string})
    return {
        "$schema": "https://json-schema.org/draft/2020-12/schema",
        "$id": "https://hexalith.io/schemas/conversations/conformance-oracle-tiering-disposition-v2.schema.json",
        "title": "Conversations Conformance Oracle Tiering Disposition V2",
        "type": "object", "additionalProperties": False,
        "required": ["schemaVersion", "authority", "candidate", "decision", "moduleAssemblies", "strengthDefinition",
                     "preSplitResult", "assertions", "validationAdditions", "denominatorSuites", "fr20Membership",
                     "publicContract", "triage", "approvals", "supersedes", "renderedMarkdownSha256"],
        "properties": {
            "schemaVersion": {"const": SCHEMA_VERSION},
            "authority": closed({"epic": string, "architecture": string, "planningCandidate": commit,
                                 "bundleDigest": sha, "sectionSha256": sha, "inventoryId": {"const": INVENTORY["id"]},
                                 "inventorySha256": {"const": INVENTORY["sha256"]}, "contractPath": {"const": CONTRACT_PATH},
                                 "contractSha256": sha}),
            "candidate": closed({"storyId": {"const": STORY_ID}, "bindingRule": {"const": BINDING_RULE},
                                 "freezeCommit": commit, "predecessorPath": {"const": PREDECESSOR_PATH},
                                 "predecessorSha256": sha, "predecessorCandidate": commit}),
            "decision": closed({"path": {"const": DECISION_PATH}, "sha256": {"const": DECISION_SHA256},
                                "status": {"const": "approved"}, "decision": {"const": "tier-the-oracle"},
                                "approvedBy": string, "decisionDate": string, "triageResultsInDecision": {"type": "null"},
                                "triageSuppliedBy": string}),
            "moduleAssemblies": {"type": "array", "minItems": 1, "items": closed({
                "name": string, "project": path, "packable": {"type": "boolean"}, "directReference": {"type": "boolean"}})},
            "strengthDefinition": closed({"material": {"const": ["boundAssemblies", "behaviorIdentity", "negativeCaseCount"]},
                                          "digest": string, "behaviorIdentity": string, "closure": string,
                                          "boundAssemblies": string, "negativeSites": strings}),
            "preSplitResult": closed({
                "sourceCommit": commit, "sourceTree": commit,
                "sourceFiles": {"type": "array", "minItems": 1, "items": file_binding},
                "lane": closed({"workflow": file_binding, "job": string, "environment": environment,
                                "environmentPins": strings, "buildCommand": strings, "runCommand": strings,
                                "exclusions": exclusions, "historicalExclusionPolicy": string}),
                "build": closed({"configuration": {"const": "Release"}, "exitCode": {"const": 0},
                                 "testAssembly": closed({"path": path, "sha256": sha, "sourceRevisionId": commit}),
                                 "moduleAssemblies": {"type": "array", "items": closed({"name": string, "sha256": sha})}}),
                "discovery": closed({"path": path, "sha256": sha, "testMethodCount": count}),
                "result": closed({"path": path, "sha256": sha, "summary": closed({
                    "total": count, "executed": count, "passed": count, "failed": count, "skipped": {"const": 0},
                    "notRun": {"const": 0}, "exitCode": {"type": "integer"}, "failedTests": strings})}),
                "receipt": file_binding,
                "receiptFacts": receipt_facts,
                "counts": closed({"discoveredMethods": count, "executedMethods": count, "excludedMethods": count,
                                  "theoryMethods": count, "executedTestCases": count}),
                "identitySha256": sha,
                "generationIsolation": closed({"methods": strings, "mode": {"const": "isolated-clone"},
                                               "trackedWritesObserved": writes,
                                               "repositoryWritesOutsideCapture": {"const": False}}),
            }),
            "assertions": {"type": "array", "minItems": 1, "items": row},
            "validationAdditions": {"type": "array", "items": row},
            "denominatorSuites": {"type": "array", "minItems": 3, "maxItems": 3, "items": suite_row},
            "fr20Membership": closed({
                "v1Floor": closed({"baseline": file_binding, "source": file_binding, "sourceCommit": commit,
                                   "suiteCount": {"const": 14}, "testCount": {"const": 214}, "testIdsSha256": sha,
                                   "tierCounts": tier_counts}),
                "approvedCumulative": closed({"testCount": {"const": 384}, "testIdsSha256": sha,
                                             "approvalDecision": file_binding, "tierCounts": tier_counts}),
                "missingIdentities": {"type": "array", "maxItems": 0}, "rule": string}),
            "publicContract": closed({
                "protectedBaseline": closed({"path": path, "sha256": sha, "role": string}),
                "reviewedClientBaseline": file_binding,
                "freezeSurface": closed({"commit": commit, "projects": {"type": "array", "items": closed({
                    "path": path, "fileCount": count, "sha256": sha})}, "sha256": sha}),
                "preExistingDrift": closed({"observedInPreSplitResult": {"type": "boolean"}, "evidence": string,
                                            "attribution": string, "approvalClaimed": {"const": False}}),
                "rule": string}),
            "triage": closed({"source": file_binding, "fileCount": count, "outcomes": {"type": "array", "items": closed({
                "file": path, "namespaces": strings, "assertions": count, "tierCounts": tier_counts})}}),
            "approvals": {"anyOf": [{"type": "null"}, closed({
                "path": {"const": APPROVALS_PATH}, "sha256": sha, "approvalId": string, "approver": string,
                "role": {"const": OWNER_ROLE}, "approvedOn": string, "membershipSha256": sha,
                "historicalSuiteApproval": historical})]},
            "supersedes": closed({
                "decision": closed({"path": {"const": DECISION_PATH}, "sha256": {"const": DECISION_SHA256},
                                    "supersededField": string, "relationship": string}),
                "protectedInventory": closed({"path": {"const": MANIFEST_V2_PATH}, "sha256": sha, "fields": strings}),
                "v1Artifacts": {"type": "array", "minItems": 1, "items": closed({"path": path, "sha256": sha,
                                                                                 "inventory": string})},
                "tieringLineage": {"type": "array", "items": closed({"path": path, "sha256": sha, "role": string})},
                "v1MutationAllowed": {"const": False}}),
            "renderedMarkdownSha256": sha,
        },
    }


def cell(value: Any) -> str:
    text = value if isinstance(value, str) else json.dumps(value, ensure_ascii=False, sort_keys=True)
    return text.replace("|", "\\|").replace("\n", " ")


def markdown(document: dict[str, Any]) -> bytes:
    """Deterministic reviewer projection of the authoritative JSON."""
    result = document["preSplitResult"]
    summary = result["result"]["summary"]
    rows = document["assertions"]
    additions = document["validationAdditions"]
    approvals = document["approvals"]
    lines = [
        "# Conformance Oracle Tiering Disposition V2", "",
        "The JSON disposition is authoritative; this rendering is bound to it by `renderedMarkdownSha256`.", "",
        f"- Schema: `{document['schemaVersion']}`",
        f"- Story: `{document['candidate']['storyId']}`; candidate rule: {document['candidate']['bindingRule']}",
        f"- Freeze commit: `{document['candidate']['freezeCommit']}`",
        f"- Contract: `{document['authority']['contractPath']}` (`{document['authority']['contractSha256']}`)",
        f"- Inventory: `{document['authority']['inventoryId']}` (`{document['authority']['inventorySha256']}`)",
        f"- Decision: `{document['decision']['path']}` (`{document['decision']['sha256']}`)",
        f"- Story 7.4 predecessor: `{document['candidate']['predecessorPath']}` (`{document['candidate']['predecessorSha256']}`)",
        "", "## Pre-split result", "",
        f"- Source commit/tree: `{result['sourceCommit']}` / `{result['sourceTree']}`",
        f"- Lane: `{result['lane']['job']}` from `{result['lane']['workflow']['path']}` (`{result['lane']['workflow']['sha256']}`)",
        f"- Test assembly: `{result['build']['testAssembly']['path']}` (`{result['build']['testAssembly']['sha256']}`)",
        f"- Discovery: `{result['discovery']['path']}` (`{result['discovery']['sha256']}`), {result['discovery']['testMethodCount']} methods",
        f"- Result: `{result['result']['path']}` (`{result['result']['sha256']}`): total {summary['total']}, "
        f"executed {summary['executed']}, passed {summary['passed']}, failed {summary['failed']}, skipped {summary['skipped']}",
        f"- Failed pre-split tests (recorded, not claimed as passing): {', '.join(f'`{item}`' for item in summary['failedTests']) or 'none'}",
        f"- Historical exclusions (frozen identities, no current pass claim): {len(result['lane']['exclusions']['classes'])} classes, "
        f"{len(result['lane']['exclusions']['methods'])} methods",
        f"- Counts: {cell(result['counts'])}",
        f"- Identity digest: `{result['identitySha256']}`",
        "", "## Tier summary", "",
        "| Group | Rows | Portable | Module-internal |", "| --- | --- | --- | --- |",
    ]
    for title, group in (("Pre-split assertions", rows), ("Post-freeze validation additions", additions)):
        lines.append(f"| {title} | {len(group)} | {sum(1 for row in group if row['tier'] == 'portable')} | "
                     f"{sum(1 for row in group if row['tier'] == 'module-internal')} |")
    lines.extend(["", "## Approvals", ""])
    if approvals is None:
        lines.append("No Quality-owner decision is bound.")
    else:
        lines.extend([f"- Decision: `{approvals['approvalId']}` by {approvals['approver']} ({approvals['role']}) on {approvals['approvedOn']}",
                      f"- Approvals file: `{approvals['path']}` (`{approvals['sha256']}`)",
                      f"- Membership digest: `{approvals['membershipSha256']}`",
                      f"- Historical suite approval (distinct, approves no row): {cell(approvals['historicalSuiteApproval'])}"])
    lines.extend(["", "## Denominator suites", "",
                  "| Suite | Tier | v1 floor | Current | Membership changed | Historical approval | Approval | Manifest update |",
                  "| --- | --- | --- | --- | --- | --- | --- | --- |"])
    for suite in document["denominatorSuites"]:
        lines.append(f"| `{suite['suite']}` | `{suite['tier']}` | {len(suite['v1FloorTestIds'])} | "
                     f"{len(suite['currentIdentities'])} | `{str(suite['membershipChanged']).lower()}` | "
                     f"{cell(suite['historicalApproval']['approvedBy'])} {suite['historicalApproval']['approvedOn']} | "
                     f"{cell(suite['approval'])} | {cell(suite['manifestUpdate'])} |")
    membership = document["fr20Membership"]
    lines.extend(["", "## FR-20 membership", "",
                  f"- v1 floor: {membership['v1Floor']['testCount']} tests / {membership['v1Floor']['suiteCount']} suites, "
                  f"`{membership['v1Floor']['testIdsSha256']}`; tiers {cell(membership['v1Floor']['tierCounts'])}",
                  f"- Approved accumulated: {membership['approvedCumulative']['testCount']} tests, "
                  f"`{membership['approvedCumulative']['testIdsSha256']}`; tiers {cell(membership['approvedCumulative']['tierCounts'])}",
                  f"- Missing identities: {len(membership['missingIdentities'])}",
                  "", "## Public contract", "",
                  f"- Protected baseline: `{document['publicContract']['protectedBaseline']['path']}` (`{document['publicContract']['protectedBaseline']['sha256']}`)",
                  f"- Freeze surface digest: `{document['publicContract']['freezeSurface']['sha256']}`",
                  f"- Pre-existing drift observed in pre-split result: `{str(document['publicContract']['preExistingDrift']['observedInPreSplitResult']).lower()}` (no approval claimed)",
                  "", "## Supersession", "",
                  f"- Decision `{document['supersedes']['decision']['path']}` (`{document['supersedes']['decision']['sha256']}`) is preserved byte-for-byte.",
                  f"- Protected inventory: `{document['supersedes']['protectedInventory']['path']}` (`{document['supersedes']['protectedInventory']['sha256']}`)",
                  "", "| Protected v1 artifact | SHA-256 |", "| --- | --- |"])
    for item in document["supersedes"]["v1Artifacts"] + document["supersedes"]["tieringLineage"]:
        lines.append(f"| `{item['path']}` | `{item['sha256']}` |")
    lines.extend(["", "## Triage of the residual coupling inventory", "", "| File | Assertions | Portable | Module-internal |",
                  "| --- | --- | --- | --- |"])
    for item in document["triage"]["outcomes"]:
        lines.append(f"| `{item['file']}` | {item['assertions']} | {item['tierCounts']['portable']} | {item['tierCounts']['module-internal']} |")
    for title, group in (("Assertions", rows), ("Validation additions", additions)):
        lines.extend(["", f"## {title}", "",
                      "| ID | Source | Pre-split identity | Strength | Server bindings | Tier | Replacement or internal type and reason | Owner | Approval | Rationale |",
                      "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"])
        for row in group:
            identity = row["preSplitResultIdentity"]
            disposition = row.get("publicReplacement") or row.get("internalTypeAndReason")
            lines.append(
                f"| `{row['id']}` | `{row['sourcePath']}:{row['sourceLine']}` (`{row['sourceSha256']}`) | "
                f"{identity['lane']}; executed {identity['executed']}, passed {identity['passed']}, failed {identity['failed']} | "
                f"`{row['strengthSha256']}` {cell(row['strengthMaterial'])} | {cell(row['serverBindings'])} | `{row['tier']}` | "
                f"{cell(disposition)} | {cell(row['owner'])} | {cell(row['approval'])} | {cell(row['rationale'])} |")
    lines.append("")
    return "\n".join(lines).encode("utf-8")


def render(document: dict[str, Any]) -> tuple[bytes, bytes, bytes]:
    document = json.loads(json.dumps(document))
    document["renderedMarkdownSha256"] = "0" * 64
    rendered = markdown(document)
    document["renderedMarkdownSha256"] = sha256_bytes(rendered)
    schema_document = schema()
    try:
        jsonschema.Draft202012Validator.check_schema(schema_document)
        jsonschema.validate(document, schema_document)
    except (jsonschema.ValidationError, jsonschema.SchemaError) as error:
        raise TieringError("TIERING_SCHEMA_INVALID", f"the disposition violates its schema at {error.json_path}") from None
    return ((json.dumps(schema_document, indent=2, ensure_ascii=False) + "\n").encode(),
            (json.dumps(document, indent=2, ensure_ascii=False) + "\n").encode(), rendered)


def write_files(root: Path, outputs: tuple[str, ...], contents: tuple[bytes, ...]) -> None:
    """Replace every output or restore the prior bytes."""
    staged: list[Path] = []
    originals: list[bytes | None] = []
    targets = [root / relative for relative in outputs]
    try:
        for target, content in zip(targets, contents):
            target.parent.mkdir(parents=True, exist_ok=True)
            if target.is_symlink() or not target.parent.resolve(strict=True).is_relative_to(root):
                raise OSError("unsafe output path")
            originals.append(target.read_bytes() if target.exists() else None)
            with tempfile.NamedTemporaryFile(dir=target.parent, delete=False) as handle:
                staged.append(Path(handle.name))
                handle.write(content)
            os.chmod(staged[-1], 0o644)
        for temporary, target in zip(staged, targets):
            os.replace(temporary, target)
    except OSError as error:
        for target, content in zip(targets, originals):
            try:
                if content is None:
                    target.unlink(missing_ok=True)
                else:
                    target.write_bytes(content)
            except OSError:
                pass
        raise TieringError("TIERING_RENDER_DRIFT", "the outputs could not be written coherently") from error
    finally:
        for temporary in staged:
            temporary.unlink(missing_ok=True)


# --------------------------------------------------------------------------- verification


def verify(root: Path, contract_path: str = CONTRACT_PATH, decision_path: str = DECISION_PATH) -> dict[str, Any]:
    """Read-only verification of the committed bundle; raises the first failing category."""
    installed = [read_file(root, path) for path in OUTPUT_PATHS]
    if any(content is None for content in installed):
        raise TieringError("TIERING_SCHEMA_INVALID", "the disposition bundle is incomplete")
    schema_bytes, json_bytes, markdown_bytes = installed
    try:
        document = json.loads(json_bytes)
        if not isinstance(document, dict) or not isinstance(document.get("assertions"), list) \
                or not isinstance(document.get("validationAdditions"), list) \
                or any(not isinstance(row, dict) for row in document["assertions"] + document["validationAdditions"]):
            raise ValueError("shape")
    except (ValueError, UnicodeDecodeError):
        raise TieringError("TIERING_SCHEMA_INVALID", "the disposition JSON is malformed") from None
    retained = read_file(root, RECEIPT_PATH) is not None
    pre_split = load_pre_split(root) if retained else pre_split_from_document(document)
    if retained and document.get("preSplitResult", {}).get("receipt", {}).get("sha256") != pre_split.receipt_sha256:
        raise TieringError("TIERING_PRE_SPLIT_RESULT_INVALID", "the retained pre-split receipt differs from the disposition")
    derivation = derive(root, contract_path, decision_path, pre_split=pre_split)
    expected = derivation.document
    findings = derivation.findings

    def rows_check(name: str, rows: list[dict[str, Any]], expected_rows: list[dict[str, Any]]) -> None:
        identifiers = [row.get("id") for row in rows]
        expected_ids = [row["id"] for row in expected_rows]
        duplicates = sorted({identifier for identifier in identifiers if identifiers.count(identifier) > 1})
        missing = sorted(set(expected_ids) - set(identifiers))
        unknown = sorted(set(identifiers) - set(expected_ids), key=str)
        if duplicates:
            findings.add("CONFORMANCE_ASSERTION_DUPLICATE", f"{name} identity appears more than once: {duplicates[0]}")
        if missing and unknown:
            findings.add("CONFORMANCE_ASSERTION_RENAMED", f"{name} identity {missing[0]} was replaced by {unknown[0]}")
        elif missing:
            findings.add("CONFORMANCE_ASSERTION_MISSING", f"{name} identity is missing: {missing[0]}")
        elif unknown:
            findings.add("CONFORMANCE_ASSERTION_UNKNOWN", f"{name} identity is unknown: {unknown[0]}")
        expected_by_id = {row["id"]: row for row in expected_rows}
        for row in rows:
            reference = expected_by_id.get(row.get("id"))
            if reference is None:
                continue
            if row.get("strengthMaterial") != reference["strengthMaterial"] or \
                    row.get("strengthSha256") != reference["strengthSha256"] or \
                    (isinstance(row.get("strengthMaterial"), dict)
                     and row.get("strengthSha256") != strength_sha256(row["strengthMaterial"])):
                findings.add("ASSERTION_STRENGTH_WEAKENED", f"recorded strength differs from derivation: {row['id']}")
            if row.get("sourceSha256") != reference["sourceSha256"] or row.get("closureFiles") != reference["closureFiles"]:
                findings.add("CONFORMANCE_ASSERTION_SOURCE_DRIFT", f"recorded source binding differs: {row['id']}")
            tier = row.get("tier")
            if tier not in TIERS:
                findings.add("TIER_UNASSIGNED", f"no valid tier is assigned: {row['id']}")
                continue
            if not isinstance(row.get("rationale"), str) or not row["rationale"].strip():
                findings.add("TIER_REASON_MISSING", f"the tier rationale is missing: {row['id']}")
            if tier == "module-internal":
                detail = row.get("internalTypeAndReason")
                if (not isinstance(detail, dict) or not isinstance(detail.get("reason"), str)
                        or not detail["reason"].strip() or not isinstance(detail.get("types"), list)
                        or not detail["types"] or any(item not in row.get("serverBindings", [])
                                                      for item in detail["types"])):
                    findings.add("TIER_REASON_MISSING", f"module-internal row lacks its exact internal type and reason: {row['id']}")
            else:
                detail = row.get("publicReplacement")
                if (not isinstance(detail, dict) or detail.get("strengthSha256") != row.get("strengthSha256")
                        or detail.get("equalStrength") is not True or row.get("serverBindings")):
                    findings.add("TIER_REASON_MISSING", f"portable row lacks an exact equal-strength public replacement: {row['id']}")
            if tier != reference["tier"]:
                findings.add("TIER_APPROVAL_MISSING", f"the row tier differs from the approved proposal: {row['id']}")
            if row.get("approval") != reference["approval"] or row.get("owner") != reference["owner"] \
                    or not row.get("owner") or not isinstance(row.get("approval"), dict):
                findings.add("TIER_APPROVAL_MISSING", f"the row lacks its digest-bound owner approval: {row['id']}")

    rows_check("assertion", document["assertions"], expected["assertions"])
    rows_check("validation addition", document["validationAdditions"], expected["validationAdditions"])
    suites = document.get("denominatorSuites")
    if not isinstance(suites, list) or any(not isinstance(suite, dict) for suite in suites):
        findings.add("FR20_DENOMINATOR_DRIFT", "the denominator suites are malformed")
    else:
        expected_suites = {suite["suite"]: suite for suite in expected["denominatorSuites"]}
        if [suite.get("suite") for suite in suites] != list(expected_suites):
            findings.add("FR20_DENOMINATOR_DRIFT", "the reclassified denominator suite set changed")
        for suite in suites:
            reference = expected_suites.get(suite.get("suite"))
            if reference is None:
                continue
            for name in ("v1FloorTestIds", "v1FloorTestIdsSha256", "currentIdentities", "currentIdentitiesSha256",
                         "membershipChanged", "manifestUpdate", "class"):
                if suite.get(name) != reference[name]:
                    findings.add("FR20_DENOMINATOR_DRIFT", f"suite {suite['suite']} {name} differs from the frozen membership")
            if suite.get("historicalApproval") != reference["historicalApproval"] \
                    or suite.get("approval") != reference["approval"] or not suite.get("owner") \
                    or suite.get("owner") != reference["owner"] or not isinstance(suite.get("rationale"), str):
                findings.add("TIER_APPROVAL_MISSING", f"suite {suite['suite']} lacks its distinct owner approval evidence")
    if document.get("fr20Membership") != expected["fr20Membership"]:
        findings.add("FR20_DENOMINATOR_DRIFT", "the recorded FR-20 membership differs from the frozen derivation")
    if document.get("publicContract") != expected["publicContract"]:
        findings.add("PUBLIC_CONTRACT_WIDENED", "the recorded public contract surface differs from the freeze")
    if document.get("supersedes") != expected["supersedes"]:
        findings.add("V1_ARTIFACT_DRIFT", "the recorded v1 supersession links differ from the protected inventory")
    if document.get("approvals") != expected["approvals"]:
        findings.add("TIER_APPROVAL_MISSING", "the recorded approval binding differs from the approvals file")
    first = findings.first_category()
    if first:
        raise MultiError(first)
    canonical = render(expected)
    if schema_bytes != canonical[0]:
        raise TieringError("TIERING_SCHEMA_INVALID", "the schema differs from the canonical closed schema")
    try:
        jsonschema.validate(document, json.loads(schema_bytes))
    except (jsonschema.ValidationError, jsonschema.SchemaError):
        raise TieringError("TIERING_SCHEMA_INVALID", "the disposition violates its closed schema") from None
    if markdown_bytes != canonical[2] or document.get("renderedMarkdownSha256") != sha256_bytes(markdown_bytes):
        raise TieringError("TIERING_RENDER_DRIFT", "the Markdown differs from its deterministic rendering")
    if json_bytes != canonical[1]:
        raise TieringError("TIERING_RENDER_DRIFT", "the disposition differs from its deterministic derivation")
    return document


class MultiError(Exception):
    """Several blockers of the same category."""

    def __init__(self, items: list[tuple[str, str]]) -> None:
        super().__init__("; ".join(code for code, _ in items))
        self.items = items


# --------------------------------------------------------------------------- CLI


def generate(root: Path, contract_path: str, decision_path: str) -> tuple[bytes, bytes, bytes]:
    derivation = derive(root, contract_path, decision_path)
    first = derivation.findings.first_category()
    if first:
        raise MultiError(first)
    return render(derivation.document)


def propose(root: Path, contract_path: str, decision_path: str) -> str:
    derivation = derive(root, contract_path, decision_path, require_approval=False)
    blocking = [(code, detail) for code, detail in derivation.findings.items if code != "TIER_APPROVAL_MISSING"]
    if blocking:
        raise MultiError(blocking)
    inputs = contract_inputs(root, contract_path, decision_path)
    proposal = proposal_document(derivation, inputs["decision_bytes"])
    document = json.loads(json.dumps(derivation.document))
    document["renderedMarkdownSha256"] = "0" * 64
    review = markdown(document)
    write_files(root, (APPROVALS_PATH, PROPOSAL_REVIEW_PATH),
                ((json.dumps(proposal, indent=2, ensure_ascii=False) + "\n").encode(), review))
    return derivation.membership_sha256


def record_approval(root: Path, contract_path: str, decision_path: str, arguments: argparse.Namespace) -> str:
    approvals = parse_json_bytes(read_file(root, APPROVALS_PATH), "TIER_APPROVAL_MISSING", "the approvals file")
    derivation = derive(root, contract_path, decision_path, require_approval=False)
    if approvals.get("proposal", {}).get("membershipSha256") != derivation.membership_sha256 \
            or arguments.approved_membership_sha256 != derivation.membership_sha256:
        raise TieringError("TIER_APPROVAL_MISSING", "the approved digest does not equal the current proposed membership")
    if approvals.get("decision") is not None:
        raise TieringError("TIER_APPROVAL_MISSING", "an owner decision is already recorded; propose again to replace it")
    values = {"approver": arguments.approver, "approvalId": arguments.approval_id,
              "approvedOn": arguments.approved_on, "evidence": arguments.approval_evidence}
    if any(not value or not value.strip() for value in values.values()) or \
            not re.fullmatch(r"\d{4}-\d{2}-\d{2}", arguments.approved_on or ""):
        raise TieringError("TIER_APPROVAL_MISSING", "approver, approval id, ISO date, and evidence are required")
    approvals["status"] = "approved"
    approvals["decision"] = {"state": "approved", "role": OWNER_ROLE,
                             "approvedMembershipSha256": derivation.membership_sha256, **values}
    write_files(root, (APPROVALS_PATH,), ((json.dumps(approvals, indent=2, ensure_ascii=False) + "\n").encode(),))
    return derivation.membership_sha256


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--contract", default=CONTRACT_PATH)
    parser.add_argument("--decision", default=DECISION_PATH)
    parser.add_argument("--output-schema")
    parser.add_argument("--output-json")
    parser.add_argument("--output-markdown")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--verify", action="store_true", help="verify the committed bundle without writing")
    mode.add_argument("--capture-pre-split", action="store_true", help="freeze the pre-split execution in isolation")
    mode.add_argument("--propose-approvals", action="store_true", help="write the digest-bound proposal for review")
    mode.add_argument("--record-approval", action="store_true", help="record the Quality owner's decision")
    parser.add_argument("--freeze-commit")
    parser.add_argument("--workdir")
    parser.add_argument("--approver")
    parser.add_argument("--approval-id")
    parser.add_argument("--approved-on")
    parser.add_argument("--approved-membership-sha256")
    parser.add_argument("--approval-evidence")
    arguments = parser.parse_args(argv)
    root = Path(arguments.repository).resolve()
    try:
        if not (root / ".git").exists():
            raise TieringError("TIERING_ENVIRONMENT_UNAVAILABLE", "the repository is not a Git checkout")
        if arguments.capture_pre_split:
            if not arguments.freeze_commit:
                raise TieringError("TIERING_CAPTURE_FAILED", "--freeze-commit is required")
            receipt = capture(root, arguments.freeze_commit, Path(arguments.workdir) if arguments.workdir else None)
            print(f"PASS: pre-split execution captured at {receipt['freezeCommit']}: "
                  f"{receipt['run']['reported']['total']} results, run exit {receipt['run']['exitCode']}")
            return 0
        if arguments.propose_approvals:
            digest = propose(root, arguments.contract, arguments.decision)
            print(f"PASS: proposed membership written to {APPROVALS_PATH}; membershipSha256 {digest}")
            return 0
        if arguments.record_approval:
            digest = record_approval(root, arguments.contract, arguments.decision, arguments)
            print(f"PASS: Quality-owner decision recorded for membershipSha256 {digest}")
            return 0
        outputs = (arguments.output_schema, arguments.output_json, arguments.output_markdown)
        if outputs != OUTPUT_PATHS:
            raise TieringError("TIERING_SCHEMA_INVALID", "output paths differ from the canonical bundle")
        if arguments.verify:
            document = verify(root, arguments.contract, arguments.decision)
            print(f"PASS: conformance tiering disposition verified; {len(document['assertions'])} assertions, "
                  f"{len(document['validationAdditions'])} validation additions")
            return 0
        generated = generate(root, arguments.contract, arguments.decision)
        write_files(root, OUTPUT_PATHS, generated)
        document = json.loads(generated[1])
        print(f"PASS: conformance tiering disposition generated; schema {SCHEMA_VERSION}; "
              f"{len(document['assertions'])} assertions; result {document['preSplitResult']['result']['sha256']}; "
              f"decision {document['decision']['sha256']}")
        return 0
    except MultiError as error:
        for code, detail in error.items:
            print(f"FAIL: {code}: {detail}", file=sys.stderr)
        return 1
    except TieringError as error:
        label = "BLOCKED" if error.code in BLOCKED_CODES else "FAIL"
        print(f"{label}: {error.code}: {error}", file=sys.stderr)
        return 2 if error.code in BLOCKED_CODES else 1


if __name__ == "__main__":
    sys.exit(main())
