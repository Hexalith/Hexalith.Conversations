#!/usr/bin/env python3
"""Publish and validate the V19/V20 Story 7.1 successor authorities."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
from typing import Any, Sequence
import unicodedata
import xml.etree.ElementTree as ET


RESULT_STATES = ("PASS", "FAIL", "BLOCKED", "not-applicable")
V19_SCHEMA_VERSION = "hexalith.conversations.story-7.1-checkpoint-completion-authority.v1"
V20_SCHEMA_VERSION = "hexalith.conversations.story-7.1-release-owner-authority.v1"
INVENTORY_SCHEMA_VERSION = "hexalith.conversations.story-7.1-input-inventory.v1"
V19_AUTHORITY_ID = "V19-STORY-7.1-CHECKPOINT-COMPLETION"
V20_AUTHORITY_ID = "V20-STORY-7.1-RELEASE-OWNER-AUTHORITY"
V19_PATH = "_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json"
V20_PATH = "_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json"
V19_SCHEMA_PATH = "_bmad/schemas/v19-story-7.1-checkpoint-completion-authority-v1.schema.json"
V20_SCHEMA_PATH = "_bmad/schemas/v20-story-7.1-release-owner-authority-v1.schema.json"
INVENTORY_PATH = "_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json"
INVENTORY_SCHEMA_PATH = "_bmad/schemas/v20-story-7.1-input-inventory-v1.schema.json"
PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_successor_authorities.py"
PUBLISHER_TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py"

PLANNING_CANDIDATE = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5"
V9_BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V9_BUNDLE_RAW_SHA256 = "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3"
V9_BUNDLE_DIGEST = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055"
V11_PATH = "_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json"
V11_RAW_SHA256 = "14e95c44149594b87e5337b45fd546fdd48d58407fa0b61f3d4b94cba59da82d"
V11_EPIC = "epic-6-authority-2026-08-04-v11"
V11_ARCHITECTURE = "conversations-architecture-2026-08-04-v11"
V18_PATH = "_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json"
V18_RAW_SHA256 = "24891d990b399fd9526f864c3e8be2b188db728a398f09d9129731d3169f1d94"
SEMANTIC_SOURCE_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-7-1-define-the-final-record-schema-and-deterministic-generator-core-2.md"
)
SEMANTIC_SOURCE_SHA256 = "90477eb2666c2dc693192770b801664fedab385638917e6202dd9a7e09d4166d"
CHECKPOINT_RESULT_PATH = "artifacts/v9/schema-slice/v2-schema-contract.xml"
CHECKPOINT_COMMAND = (
    "python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py "
    "-k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml"
)

CHECKPOINT_PATHS = (
    "_bmad/schemas/v9-acceptance-result-v1.schema.json",
    "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/scripts/tests/test_generate_story_record.py",
    CHECKPOINT_RESULT_PATH,
)
CHECKPOINT_PATH_DIGEST = "5137dfa15a6c28253898b7edac5969684abddc2c77b35c2f3299b07faa72cf9d"
ROOT_GITLINK_PATHS = (
    "references/Hexalith.AI.Tools",
    "references/Hexalith.Builds",
    "references/Hexalith.Commons",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)
ROOT_GITLINK_PATH_DIGEST = "d1e434be15361351b1aa12b0e22619060761bd2d4b6e1a56f4bcf1f6e8978c20"

HISTORICAL_COMMIT = "b819a7c43a7024295abaabd418a74f5f64cb5af0"
HISTORICAL_PARENT = "73bcee6f04479d4743d5a65ce929728e22687d7d"
HISTORICAL_TREE = "89baa263615d1e510d15410be7f47b09bff30aca"
HISTORICAL_PATHS = (
    "_bmad-output/implementation-artifacts/deferred-work.md",
    "_bmad-output/implementation-artifacts/epic-7-context.md",
    "_bmad-output/implementation-artifacts/spec-7-1-schemas.md",
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/schemas/v9-acceptance-result-v1.schema.json",
    "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
    "_bmad/scripts/tests/test_generate_story_record.py",
    "references/Hexalith.Builds",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)
HISTORICAL_PATH_DIGEST = "be68d84d11534579092215b1f8a758cefb4edfd56644b046cb6687468e5a8212"
HISTORICAL_MISSING_PATHS = (CHECKPOINT_RESULT_PATH,)
HISTORICAL_MISSING_DIGEST = "f0fbd24ece1085ca146f67193721007ab06309d2d025ee1c1bcde0e5046d279e"
HISTORICAL_UNEXPECTED_PATHS = (
    "_bmad-output/implementation-artifacts/deferred-work.md",
    "_bmad-output/implementation-artifacts/epic-7-context.md",
    "_bmad-output/implementation-artifacts/spec-7-1-schemas.md",
    "references/Hexalith.Builds",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)
HISTORICAL_UNEXPECTED_DIGEST = "be3fb907bd3288d0b5d7df553aafa24be141e6b3f7c5012c2dad36e5eaf4b4db"

PRESERVED_EVIDENCE = (
    {
        "kind": "authority",
        "identity": "V17-IMPLEMENTATION-HOLD-DECISION",
        "path": "_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json",
        "sha256": "1444f76dad9495d4c17354a9f2f5d3ce9f456cfd254c66d4a6e77e5abf446e50",
        "interpretation": "valid-point-in-time-authority-lifted-only-7.1-SCHEMAS",
    },
    {
        "kind": "authority",
        "identity": "V18-PACKAGE-ENVIRONMENT-AUTHORITY",
        "path": V18_PATH,
        "sha256": V18_RAW_SHA256,
        "interpretation": "valid-point-in-time-authority-full-story-hold-active-and-unresolved",
    },
    {
        "kind": "hold-record",
        "identity": "implementation-hold-v1",
        "path": "_bmad-output/planning-artifacts/implementation-hold-v1.json",
        "sha256": "2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12",
        "interpretation": "valid-scoped-decision-unlocks-only-7.1-SCHEMAS-and-is-not-global",
    },
    {
        "kind": "git-transaction",
        "identity": HISTORICAL_COMMIT,
        "parent": HISTORICAL_PARENT,
        "tree": HISTORICAL_TREE,
        "interpretation": "immutable-mixed-transaction-with-additive-nonconformance-finding",
    },
)

IMPLEMENTATION_PATHS = (
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/schemas/story-record-generator-failure-v1.schema.json",
    "_bmad/scripts/generate_story_record.py",
    "_bmad/scripts/tests/test_generate_story_record.py",
    "docs/runbooks/story-final-record-generation.md",
)
RESULT_PATHS = tuple(f"artifacts/v9/7.1/AC-7.1-0{index}.xml" for index in range(1, 6))
RECORD_OUTPUT_PATHS = (
    "docs/release-evidence/story-7.1-final-record-v2.json",
    "docs/release-evidence/story-7.1-final-record-v2.md",
)
AUTHORITY_PREFIX = (
    V9_BUNDLE_PATH,
    "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
    V11_PATH,
    V19_PATH,
    V20_PATH,
    INVENTORY_PATH,
    "_bmad/schemas/v9-story-contract-v1.schema.json",
    INVENTORY_SCHEMA_PATH,
)
SCENARIO_PATHS = {
    "AC-7.1-01": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-02": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-03": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-04": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-05": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/schemas/story-record-generator-failure-v1.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-06": (".gitmodules",)
    + AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/schemas/story-record-generator-failure-v1.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
        *RESULT_PATHS,
    ),
}
SCENARIO_IDENTITIES = {
    "AC-7.1-01": "V20-7.1-AC01-INPUT-PATHS-v1",
    "AC-7.1-02": "V20-7.1-AC02-INPUT-PATHS-v1",
    "AC-7.1-03": "V20-7.1-AC03-INPUT-PATHS-v1",
    "AC-7.1-04": "V20-7.1-AC04-INPUT-PATHS-v1",
    "AC-7.1-05": "V20-7.1-AC05-INPUT-PATHS-v1",
    "AC-7.1-06": "V20-7.1-AC06-AGGREGATE-INPUT-PATHS-v1",
}
SCENARIO_DIGESTS = {
    "AC-7.1-01": "78a2fc7f6a53273a4c85ecc2a476cfc88c7896a86f5a6a8fd69eacc44c2540c7",
    "AC-7.1-02": "4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908",
    "AC-7.1-03": "4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908",
    "AC-7.1-04": "4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908",
    "AC-7.1-05": "2ba87bf3475e3dc799fafbefb30a132c7950057abe98eb887a107f59f6016e26",
    "AC-7.1-06": "ab3e8bb068cf5fc8b1ade9225f004d6b8dcbbccc15eb59657b1af4bfcfbfb91c",
}
CURRENT_INPUTS = (
    (V9_BUNDLE_PATH, "V9 authority index", V9_BUNDLE_RAW_SHA256),
    (
        "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
        "Six-scenario Story 7.1 contract",
        "548294d8e9752ff3354897efbfc30a1920bf8cea6a3187ac719c0ca9df618d2e",
    ),
    (V11_PATH, "Exact checkpoint scope", V11_RAW_SHA256),
    (
        "_bmad/schemas/v9-story-contract-v1.schema.json",
        "Story-contract schema",
        "33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0",
    ),
    (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "Acceptance-result schema, pre-V19 observation",
        "3a0f417edaf9979d3d3ce3f7f06095c89a78cd42b1df20b1529e10f339c67cf9",
    ),
    (
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "Frozen-inventory schema, pre-V19 observation",
        "3741bc4455d34fb0a0b575321eb3d5e909320bb5e17f185ce5cac15d0f992970",
    ),
    (
        "_bmad/schemas/story-final-record-v2.schema.json",
        "Final-record schema, pre-V19 observation",
        "212bec3facdd2a68ace486be2919f8defdb01c4720afdb8b2fa77472fd45e5ec",
    ),
    (
        "_bmad/scripts/generate_story_record.py",
        "Legacy v1 generator baseline",
        "150a293948ef760173fb7cef7b386e6bd81bfcfdeb9862fa761e0d2286addd8c",
    ),
    (
        "_bmad/scripts/tests/test_generate_story_record.py",
        "Generator/schema tests, pre-V19 observation",
        "ef9c5c9d13d0d70f01b02f2f1093be7a11de5201cf4f1cd662cc809113f2155b",
    ),
    (
        "docs/runbooks/story-final-record-generation.md",
        "Generator runbook baseline",
        "3068c63dcc3f8cf517634c10dfba7eaf4dae7cc469a7143994bda448a2c213c1",
    ),
    (SEMANTIC_SOURCE_PATH, "Approved semantic source", SEMANTIC_SOURCE_SHA256),
)


class SuccessorAuthorityError(RuntimeError):
    """A stable fail-closed successor-authority result."""

    def __init__(self, code: str, detail: str, state: str = "FAIL") -> None:
        if state not in RESULT_STATES:
            raise ValueError(f"unknown result state: {state}")
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail
        self.state = state


def sha256(content: bytes) -> str:
    """Return the lowercase SHA-256 digest of exact bytes."""

    return hashlib.sha256(content).hexdigest()


def json_bytes(value: Any) -> bytes:
    """Serialize canonical repository JSON bytes."""

    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def inventory_digest(paths: Sequence[str]) -> str:
    """Hash an ordered NFC UTF-8 path list with one terminal LF."""

    payload = unicodedata.normalize("NFC", "\n".join(paths) + "\n").encode("utf-8")
    return sha256(payload)


def safe_path(value: str) -> str:
    """Require one normalized repository-relative POSIX path."""

    path = PurePosixPath(value)
    if (
        not value
        or value in (".", "..")
        or "\\" in value
        or path.is_absolute()
        or path.as_posix() != value
        or any(part in ("", ".", "..") for part in path.parts)
        or any(ord(character) < 0x20 for character in value)
    ):
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", repr(value), "BLOCKED")
    return value


def run_git(
    root: Path,
    *arguments: str,
    allowed: tuple[int, ...] = (0,),
) -> subprocess.CompletedProcess[bytes]:
    """Run bounded non-interactive Git and preserve unavailable history as BLOCKED."""

    try:
        result = subprocess.run(
            ("git", "-C", str(root), *arguments),
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=30,
            env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SuccessorAuthorityError("SUCCESSOR_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise SuccessorAuthorityError("SUCCESSOR_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def resolve_commit(root: Path, revision: str, code: str) -> str:
    """Resolve one exact commit object."""

    try:
        value = run_git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode("ascii").strip()
    except (SuccessorAuthorityError, UnicodeError) as error:
        detail = error.detail if isinstance(error, SuccessorAuthorityError) else str(error)
        raise SuccessorAuthorityError(code, detail, "BLOCKED") from error
    if re.fullmatch(r"[0-9a-f]{40}", value) is None:
        raise SuccessorAuthorityError(code, value, "BLOCKED")
    return value


def commit_parents(root: Path, commit: str, code: str) -> tuple[str, ...]:
    """Return all parents from the raw commit record."""

    try:
        record = run_git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").split()
    except UnicodeError as error:
        raise SuccessorAuthorityError(code, str(error), "BLOCKED") from error
    if not record or record[0] != commit or any(re.fullmatch(r"[0-9a-f]{40}", row) is None for row in record):
        raise SuccessorAuthorityError(code, repr(record), "BLOCKED")
    return tuple(record[1:])


def require_single_parent(root: Path, commit: str, expected: str, code: str) -> None:
    """Require exactly one parent equal to the expected commit."""

    parents = commit_parents(root, commit, code)
    if parents != (expected,):
        raise SuccessorAuthorityError(code, f"expected=({expected!r},) observed={parents!r}", "BLOCKED")


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require an immutable ancestor relation."""

    result = run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1))
    if result.returncode != 0:
        raise SuccessorAuthorityError(code, f"{ancestor} is not an ancestor of {descendant}", "BLOCKED")


def candidate_blob(root: Path, candidate: str, relative_path: str, code: str) -> bytes:
    """Read one exact committed blob."""

    safe_path(relative_path)
    result = run_git(root, "show", f"{candidate}:{relative_path}", allowed=(0, 128))
    if result.returncode != 0:
        raise SuccessorAuthorityError(code, f"{relative_path}@{candidate}", "BLOCKED")
    return result.stdout


def raw_tree_record(root: Path, candidate: str, relative_path: str, code: str) -> tuple[str, str, str]:
    """Read one raw Git tree mode, type, and object identity."""

    try:
        output = run_git(root, "ls-tree", candidate, "--", safe_path(relative_path)).stdout.decode("utf-8").rstrip("\n")
    except UnicodeError as error:
        raise SuccessorAuthorityError(code, str(error), "BLOCKED") from error
    match = re.fullmatch(r"([0-7]{6}) (blob|commit) ([0-9a-f]{40})\t(.+)", output)
    if match is None or match.group(4) != relative_path:
        raise SuccessorAuthorityError(code, f"{relative_path}@{candidate}", "BLOCKED")
    return match.group(1), match.group(2), match.group(3)


def changed_paths(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return the ordered raw committed path list with rename detection disabled."""

    content = run_git(root, "diff", "--name-only", "--no-renames", "-z", baseline, candidate, "--").stdout
    try:
        return tuple(safe_path(item.decode("utf-8", errors="strict")) for item in content.split(b"\0") if item)
    except UnicodeError as error:
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error


def changed_gitlinks(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Derive changed gitlinks only from raw mode-160000 diff records."""

    content = run_git(root, "diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--").stdout
    records = [record for record in content.split(b"\0") if record]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise SuccessorAuthorityError("SUCCESSOR_GITLINK_DIFF_MALFORMED", "incomplete raw record", "BLOCKED")
        try:
            metadata = records[index].decode("ascii", errors="strict").split()
            path = safe_path(records[index + 1].decode("utf-8", errors="strict"))
        except UnicodeError as error:
            raise SuccessorAuthorityError("SUCCESSOR_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error
        if len(metadata) >= 2 and (metadata[0] == ":160000" or metadata[1] == "160000"):
            paths.append(path)
    return tuple(sorted(set(paths)))


def root_gitlinks(root: Path, candidate: str) -> list[dict[str, str]]:
    """Validate `.gitmodules` and derive the exact ten root mode-160000 entries."""

    try:
        modules = candidate_blob(root, candidate, ".gitmodules", "SUCCESSOR_GITMODULES_UNAVAILABLE").decode(
            "utf-8", errors="strict"
        )
        tree = run_git(root, "ls-tree", "-rz", candidate).stdout.decode("utf-8", errors="strict")
    except UnicodeError as error:
        raise SuccessorAuthorityError("SUCCESSOR_GITLINK_INVENTORY_MALFORMED", str(error), "BLOCKED") from error
    declared = tuple(sorted(re.findall(r"(?m)^\s*path\s*=\s*(\S+)\s*$", modules)))
    if declared != ROOT_GITLINK_PATHS or len(declared) != len(set(declared)):
        raise SuccessorAuthorityError("SUCCESSOR_GITMODULES_DRIFT", repr(declared))
    rows: list[dict[str, str]] = []
    for record in tree.split("\0"):
        match = re.fullmatch(r"160000 commit ([0-9a-f]{40})\t(.+)", record)
        if match:
            rows.append({"path": safe_path(match.group(2)), "commit": match.group(1), "mode": "160000"})
    rows.sort(key=lambda row: row["path"])
    if tuple(row["path"] for row in rows) != ROOT_GITLINK_PATHS:
        raise SuccessorAuthorityError("SUCCESSOR_GITLINK_INVENTORY_DRIFT", repr(rows))
    if inventory_digest(ROOT_GITLINK_PATHS) != ROOT_GITLINK_PATH_DIGEST:
        raise SuccessorAuthorityError("SUCCESSOR_GITLINK_DIGEST_DRIFT", ROOT_GITLINK_PATH_DIGEST)
    return rows


def parse_json(content: bytes, code: str) -> dict[str, Any]:
    """Parse one UTF-8 JSON object with a stable code."""

    try:
        document = json.loads(content.decode("utf-8", errors="strict"))
    except (UnicodeError, json.JSONDecodeError) as error:
        raise SuccessorAuthorityError(code, str(error)) from error
    if not isinstance(document, dict):
        raise SuccessorAuthorityError(code, "document must be an object")
    return document


def validate_json_schema(schema_bytes: bytes, document: dict[str, Any], code: str) -> None:
    """Validate one document against one Draft 2020-12 schema."""

    try:
        import jsonschema
    except ImportError as error:
        raise SuccessorAuthorityError("SUCCESSOR_SCHEMA_UNAVAILABLE", str(error), "BLOCKED") from error
    try:
        schema = json.loads(schema_bytes.decode("utf-8", errors="strict"))
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except (UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise SuccessorAuthorityError(code, str(error)) from error


def validate_v9_bundle(content: bytes) -> None:
    """Recompute the V9 raw and self-excluding internal bundle digests."""

    if sha256(content) != V9_BUNDLE_RAW_SHA256:
        raise SuccessorAuthorityError("V19_V9_BUNDLE_RAW_DRIFT", sha256(content))
    bundle = parse_json(content, "V19_V9_BUNDLE_INVALID")
    if bundle.get("planningCandidate") != PLANNING_CANDIDATE:
        raise SuccessorAuthorityError("V19_PLANNING_CANDIDATE_DRIFT", repr(bundle.get("planningCandidate")))
    rows = bundle.get("artifacts")
    if not isinstance(rows, list) or not rows:
        raise SuccessorAuthorityError("V19_V9_BUNDLE_INVALID", "artifact rows missing")
    identities: list[str] = []
    digest_rows: list[str] = []
    for row in rows:
        if not isinstance(row, dict):
            raise SuccessorAuthorityError("V19_V9_BUNDLE_INVALID", "artifact row must be an object")
        path = row.get("path")
        digest = row.get("sha256")
        if not isinstance(path, str) or not isinstance(digest, str) or re.fullmatch(r"[0-9a-f]{64}", digest) is None:
            raise SuccessorAuthorityError("V19_V9_BUNDLE_INVALID", repr(row))
        identities.append(safe_path(path))
        digest_rows.append(f"{digest}  {path}\n")
    if len(identities) != len(set(identities)) or identities != sorted(identities):
        raise SuccessorAuthorityError("V19_V9_BUNDLE_INVENTORY_DRIFT", repr(identities))
    observed = sha256("".join(digest_rows).encode("utf-8"))
    if bundle.get("bundleDigest") != observed or observed != V9_BUNDLE_DIGEST:
        raise SuccessorAuthorityError("V19_V9_BUNDLE_DIGEST_DRIFT", observed)


def validate_v11(content: bytes) -> dict[str, Any]:
    """Validate the immutable V11 checkpoint scope and command."""

    if sha256(content) != V11_RAW_SHA256:
        raise SuccessorAuthorityError("V19_V11_RAW_DRIFT", sha256(content))
    document = parse_json(content, "V19_V11_INVALID")
    authority = document.get("authority")
    acceptance = document.get("acceptance")
    if (
        document.get("writablePaths") != list(CHECKPOINT_PATHS)
        or not isinstance(authority, dict)
        or authority.get("epic") != V11_EPIC
        or authority.get("architecture") != V11_ARCHITECTURE
        or authority.get("planningCandidate") != PLANNING_CANDIDATE
        or not isinstance(acceptance, dict)
        or acceptance.get("command") != CHECKPOINT_COMMAND
        or acceptance.get("result") != "PASS"
    ):
        raise SuccessorAuthorityError("V19_V11_SCOPE_DRIFT", "checkpoint scope, authority, or command")
    return document


def validate_preserved_evidence(root: Path, candidate: str) -> None:
    """Recompute every immutable V17/V18/hold digest at the fresh candidate."""

    for row in PRESERVED_EVIDENCE[:3]:
        path = str(row["path"])
        mode, object_type, _ = raw_tree_record(root, candidate, path, "V19_PRESERVED_EVIDENCE_MISSING")
        content = candidate_blob(root, candidate, path, "V19_PRESERVED_EVIDENCE_MISSING")
        if mode != "100644" or object_type != "blob" or sha256(content) != row["sha256"]:
            raise SuccessorAuthorityError("V19_PRESERVED_EVIDENCE_DRIFT", f"{path}: {mode} {sha256(content)}")


def validate_historical_transaction(root: Path) -> dict[str, Any]:
    """Independently reproduce the immutable b819a7c NONCONFORMING finding."""

    commit = resolve_commit(root, HISTORICAL_COMMIT, "V19_HISTORICAL_HISTORY_UNAVAILABLE")
    parents = commit_parents(root, commit, "V19_HISTORICAL_HISTORY_UNAVAILABLE")
    try:
        tree = run_git(root, "show", "-s", "--format=%T", commit).stdout.decode("ascii").strip()
    except UnicodeError as error:
        raise SuccessorAuthorityError("V19_HISTORICAL_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    actual = changed_paths(root, HISTORICAL_PARENT, commit)
    missing = tuple(path for path in CHECKPOINT_PATHS if path not in actual)
    unexpected = tuple(path for path in actual if path not in CHECKPOINT_PATHS)
    historical_result = run_git(root, "cat-file", "-e", f"{commit}:{CHECKPOINT_RESULT_PATH}", allowed=(0, 128))
    if (
        parents != (HISTORICAL_PARENT,)
        or tree != HISTORICAL_TREE
        or actual != HISTORICAL_PATHS
        or missing != HISTORICAL_MISSING_PATHS
        or unexpected != HISTORICAL_UNEXPECTED_PATHS
        or inventory_digest(CHECKPOINT_PATHS) != CHECKPOINT_PATH_DIGEST
        or inventory_digest(actual) != HISTORICAL_PATH_DIGEST
        or inventory_digest(missing) != HISTORICAL_MISSING_DIGEST
        or inventory_digest(unexpected) != HISTORICAL_UNEXPECTED_DIGEST
        or historical_result.returncode == 0
    ):
        raise SuccessorAuthorityError("V19_HISTORICAL_EVIDENCE_DRIFT", "commit relation or path partitions")
    return {
        "commit": commit,
        "parent": HISTORICAL_PARENT,
        "tree": HISTORICAL_TREE,
        "expectedChangedPaths": list(CHECKPOINT_PATHS),
        "expectedChangedPathInventorySha256": CHECKPOINT_PATH_DIGEST,
        "actualChangedPaths": list(actual),
        "actualChangedPathInventorySha256": HISTORICAL_PATH_DIGEST,
        "missingPaths": list(missing),
        "missingPathInventorySha256": HISTORICAL_MISSING_DIGEST,
        "unexpectedPaths": list(unexpected),
        "unexpectedPathInventorySha256": HISTORICAL_UNEXPECTED_DIGEST,
        "machineResultCommitted": False,
        "result": "NONCONFORMING",
        "blockers": ["CHANGED_PATH_SET_MISMATCH"],
    }


def normalized_substantive_bytes(content: bytes) -> bytes:
    """Normalize only Unicode and line endings for substantive-change comparison."""

    try:
        text = content.decode("utf-8", errors="strict").replace("\r\n", "\n").replace("\r", "\n")
    except UnicodeError:
        return content
    return unicodedata.normalize("NFC", text).encode("utf-8")


def validate_substantive_checkpoint_changes(root: Path, baseline: str, candidate: str) -> None:
    """Reject touch-only and Unicode/line-ending-only checkpoint rewrites."""

    for path in CHECKPOINT_PATHS:
        candidate_content = candidate_blob(root, candidate, path, "V19_CHECKPOINT_PATH_MISSING")
        baseline_result = run_git(root, "show", f"{baseline}:{path}", allowed=(0, 128))
        if baseline_result.returncode == 0 and normalized_substantive_bytes(baseline_result.stdout) == normalized_substantive_bytes(
            candidate_content
        ):
            raise SuccessorAuthorityError("V19_NON_SUBSTANTIVE_CHANGE", path)


def checkpoint_bindings(root: Path, candidate: str) -> list[dict[str, str]]:
    """Bind all five changed checkpoint paths to committed regular blobs."""

    bindings: list[dict[str, str]] = []
    for path in CHECKPOINT_PATHS:
        mode, object_type, _ = raw_tree_record(root, candidate, path, "V19_CHECKPOINT_PATH_MISSING")
        if mode != "100644" or object_type != "blob":
            raise SuccessorAuthorityError("V19_CHECKPOINT_MODE_DRIFT", f"{path}: {mode} {object_type}")
        content = candidate_blob(root, candidate, path, "V19_CHECKPOINT_PATH_MISSING")
        bindings.append({"path": path, "mode": mode, "sha256": sha256(content)})
    return bindings


def parse_junit(content: bytes) -> tuple[dict[str, Any], list[dict[str, str]]]:
    """Parse committed JUnit counts and derive the ordered nonempty testcase ledger."""

    try:
        root = ET.fromstring(content.decode("utf-8-sig", errors="strict"))
    except (UnicodeError, ET.ParseError) as error:
        raise SuccessorAuthorityError("V19_RESULT_XML_INVALID", str(error)) from error
    suites = [root] if root.tag == "testsuite" else list(root.findall("testsuite"))
    if root.tag not in ("testsuite", "testsuites") or not suites:
        raise SuccessorAuthorityError("V19_RESULT_XML_INVALID", f"root={root.tag!r}")
    testcases = list(root.iter("testcase"))
    ledger: list[dict[str, str]] = []
    seen: set[str] = set()
    failures = errors = skipped = 0
    for ordinal, testcase in enumerate(testcases, start=1):
        classname = (testcase.get("classname") or "").strip()
        name = (testcase.get("name") or "").strip()
        identity = f"{classname}::{name}" if classname else name
        if not identity:
            raise SuccessorAuthorityError("V19_RESULT_LEDGER_INVALID", f"testcase {ordinal} has no identity")
        if identity in seen:
            raise SuccessorAuthorityError("V19_RESULT_LEDGER_DUPLICATE", identity)
        seen.add(identity)
        if testcase.find("failure") is not None:
            state = "FAIL"
            failures += 1
        elif testcase.find("error") is not None:
            state = "BLOCKED"
            errors += 1
        elif testcase.find("skipped") is not None:
            state = "not-applicable"
            skipped += 1
        else:
            state = "PASS"
        ledger.append({"id": f"JUNIT-{ordinal:04d}", "subject": identity, "state": state})
    if not ledger:
        raise SuccessorAuthorityError("V19_RESULT_LEDGER_EMPTY", CHECKPOINT_RESULT_PATH, "BLOCKED")
    declared_tests = sum(int(suite.get("tests", "0")) for suite in suites)
    declared_failures = sum(int(suite.get("failures", "0")) for suite in suites)
    declared_errors = sum(int(suite.get("errors", "0")) for suite in suites)
    declared_skipped = sum(int(suite.get("skipped", "0")) for suite in suites)
    observed = (len(testcases), failures, errors, skipped)
    declared = (declared_tests, declared_failures, declared_errors, declared_skipped)
    if declared != observed:
        raise SuccessorAuthorityError("V19_RESULT_COUNT_DRIFT", f"declared={declared!r} observed={observed!r}")
    if declared_tests <= 0 or any((declared_failures, declared_errors, declared_skipped)):
        raise SuccessorAuthorityError("V19_RESULT_NOT_PASS", repr(declared))
    return {
        "tests": declared_tests,
        "failures": declared_failures,
        "errors": declared_errors,
        "skipped": declared_skipped,
    }, ledger


def render_v19(root: Path, candidate_revision: str) -> dict[str, Any]:
    """Recompute the complete candidate-bound V19 authority."""

    root = root.resolve()
    candidate = resolve_commit(root, candidate_revision, "V19_CANDIDATE_UNAVAILABLE")
    parents = commit_parents(root, candidate, "V19_CANDIDATE_PARENT_UNAVAILABLE")
    if len(parents) != 1:
        raise SuccessorAuthorityError("V19_CANDIDATE_PARENT_MISMATCH", repr(parents), "BLOCKED")
    baseline = parents[0]
    observed_paths = changed_paths(root, baseline, candidate)
    observed_gitlinks = changed_gitlinks(root, baseline, candidate)
    if observed_gitlinks:
        raise SuccessorAuthorityError("V19_CHECKPOINT_GITLINK_DRIFT", repr(observed_gitlinks))
    if len(observed_paths) != len(set(observed_paths)) or set(observed_paths) != set(CHECKPOINT_PATHS):
        missing = [path for path in CHECKPOINT_PATHS if path not in observed_paths]
        unexpected = [path for path in observed_paths if path not in CHECKPOINT_PATHS]
        raise SuccessorAuthorityError(
            "V19_CHECKPOINT_PATH_SET_MISMATCH", f"missing={missing!r} unexpected={unexpected!r}"
        )
    validate_substantive_checkpoint_changes(root, baseline, candidate)
    v9_content = candidate_blob(root, candidate, V9_BUNDLE_PATH, "V19_V9_BUNDLE_MISSING")
    validate_v9_bundle(v9_content)
    v11_content = candidate_blob(root, candidate, V11_PATH, "V19_V11_MISSING")
    validate_v11(v11_content)
    validate_preserved_evidence(root, candidate)
    gitlinks = root_gitlinks(root, candidate)
    bindings = checkpoint_bindings(root, candidate)
    result_binding = bindings[-1]
    result_content = candidate_blob(root, candidate, CHECKPOINT_RESULT_PATH, "V19_RESULT_MISSING")
    counts, ledger = parse_junit(result_content)
    return {
        "schemaVersion": V19_SCHEMA_VERSION,
        "authorityId": V19_AUTHORITY_ID,
        "predecessor": {"authorityId": "V18-PACKAGE-ENVIRONMENT-AUTHORITY", "path": V18_PATH, "sha256": V18_RAW_SHA256},
        "preservedEvidence": list(PRESERVED_EVIDENCE),
        "planningAuthority": {
            "planningCandidate": PLANNING_CANDIDATE,
            "bundlePath": V9_BUNDLE_PATH,
            "bundleRawSha256": V9_BUNDLE_RAW_SHA256,
            "bundleDigest": V9_BUNDLE_DIGEST,
        },
        "v11Authority": {
            "epic": V11_EPIC,
            "architecture": V11_ARCHITECTURE,
            "sidecarPath": V11_PATH,
            "sidecarRawSha256": V11_RAW_SHA256,
        },
        "historicalTransaction": validate_historical_transaction(root),
        "freshCheckpoint": {
            "baselineCommit": baseline,
            "candidateCommit": candidate,
            "changedPaths": list(CHECKPOINT_PATHS),
            "changedPathInventorySha256": CHECKPOINT_PATH_DIGEST,
            "changedPathBindings": bindings,
            "changedGitlinkPaths": [],
            "rootGitlinks": gitlinks,
            "machineResult": {
                "path": CHECKPOINT_RESULT_PATH,
                "mode": result_binding["mode"],
                "sha256": result_binding["sha256"],
                "result": "PASS",
                **counts,
            },
            "assertionLedger": ledger,
        },
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True},
        "result": "PASS",
        "authorityEffect": {
            "checkpointComplete": True,
            "implementationHold": "ACTIVE",
            "fullStoryExecutionAllowed": False,
            "storyDoneAllowed": False,
            "successorUnlocked": False,
            "releaseAuthorized": False,
            "pushAuthorized": False,
        },
    }


def schema_at(root: Path, candidate: str, path: str, code: str) -> bytes:
    """Require a schema to be a committed regular blob."""

    mode, object_type, _ = raw_tree_record(root, candidate, path, code)
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError(code, f"{path}: {mode} {object_type}", "BLOCKED")
    return candidate_blob(root, candidate, path, code)


def locate_publication(root: Path, evaluated: str, path: str, requested: str | None, code: str) -> str:
    """Locate exactly one committed addition of an authority in evaluated history."""

    if requested:
        candidates = (resolve_commit(root, requested, code),)
    else:
        try:
            output = run_git(root, "log", "--format=%H", "--diff-filter=A", evaluated, "--", path).stdout.decode("ascii")
        except UnicodeError as error:
            raise SuccessorAuthorityError(code, str(error), "BLOCKED") from error
        candidates = tuple(line for line in output.splitlines() if line)
    if len(candidates) != 1:
        raise SuccessorAuthorityError(code, f"expected one publication; observed={candidates!r}", "BLOCKED")
    return candidates[0]


def validate_publication(
    root: Path,
    *,
    source: str,
    publication: str,
    evaluated: str,
    path: str,
    prefix: str,
) -> bytes:
    """Validate a direct-child exact-one-path publication and immutable descendant bytes."""

    require_single_parent(root, publication, source, f"{prefix}_PUBLICATION_PARENT_MISMATCH")
    paths = changed_paths(root, source, publication)
    if paths != (path,):
        raise SuccessorAuthorityError(f"{prefix}_PUBLICATION_SCOPE_DRIFT", repr(paths))
    if changed_gitlinks(root, source, publication):
        raise SuccessorAuthorityError(f"{prefix}_PUBLICATION_GITLINK_DRIFT", repr(changed_gitlinks(root, source, publication)))
    mode, object_type, _ = raw_tree_record(root, publication, path, f"{prefix}_PUBLICATION_PATH_MISSING")
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError(f"{prefix}_PUBLICATION_MODE_DRIFT", f"{mode} {object_type}")
    require_ancestor(root, publication, evaluated, f"{prefix}_PUBLICATION_NOT_ANCESTOR")
    published = candidate_blob(root, publication, path, f"{prefix}_PUBLICATION_PATH_MISSING")
    descendant = candidate_blob(root, evaluated, path, f"{prefix}_AUTHORITY_DESCENDANT_MISSING")
    if published != descendant:
        raise SuccessorAuthorityError(f"{prefix}_AUTHORITY_DESCENDANT_DRIFT", path)
    return published


def write_atomic(root: Path, relative_path: str, content: bytes, code: str) -> None:
    """Atomically replace one bounded repository file."""

    target = (root / safe_path(relative_path)).resolve()
    try:
        target.relative_to(root.resolve())
    except ValueError as error:
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", relative_path, "BLOCKED") from error
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_suffix(target.suffix + ".tmp")
    try:
        temporary.write_bytes(content)
        os.replace(temporary, target)
    except OSError as error:
        temporary.unlink(missing_ok=True)
        raise SuccessorAuthorityError(code, str(error), "BLOCKED") from error


def publish_v19(
    root: Path,
    *,
    candidate_revision: str | None,
    check: bool,
    publication_revision: str | None = None,
) -> dict[str, Any]:
    """Write V19 or validate its committed candidate-before-record transaction."""

    root = root.resolve()
    if check:
        evaluated = resolve_commit(root, candidate_revision or "HEAD", "V19_EVALUATED_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated, V19_PATH, publication_revision, "V19_PUBLICATION_MISSING")
        existing_bytes = candidate_blob(root, publication, V19_PATH, "V19_AUTHORITY_MISSING")
        existing = parse_json(existing_bytes, "V19_AUTHORITY_INVALID")
        fresh = existing.get("freshCheckpoint")
        if not isinstance(fresh, dict) or not isinstance(fresh.get("candidateCommit"), str):
            raise SuccessorAuthorityError("V19_AUTHORITY_INVALID", "freshCheckpoint.candidateCommit")
        candidate = resolve_commit(root, fresh["candidateCommit"], "V19_CANDIDATE_UNAVAILABLE")
    else:
        candidate = resolve_commit(root, candidate_revision or "HEAD", "V19_CANDIDATE_UNAVAILABLE")
        existing = None
        existing_bytes = b""
    document = render_v19(root, candidate)
    validate_json_schema(schema_at(root, candidate, V19_SCHEMA_PATH, "V19_SCHEMA_MISSING"), document, "V19_SCHEMA_INVALID")
    if check:
        if existing != document or existing_bytes != json_bytes(document):
            raise SuccessorAuthorityError("V19_AUTHORITY_DRIFT", V19_PATH)
        validate_publication(
            root,
            source=candidate,
            publication=publication,
            evaluated=evaluated,
            path=V19_PATH,
            prefix="V19",
        )
    else:
        write_atomic(root, V19_PATH, json_bytes(document), "V19_WRITE_FAILED")
    return document


def binding_role(path: str) -> str:
    """Return the frozen non-interchangeable role for a scenario input path."""

    roles = {
        ".gitmodules": "root-submodule-inventory",
        V9_BUNDLE_PATH: "planning-authority-bundle",
        "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json": "story-contract",
        V11_PATH: "checkpoint-scope-authority",
        V19_PATH: "checkpoint-completion-authority",
        V20_PATH: "release-owner-authority",
        INVENTORY_PATH: "frozen-input-inventory",
        "_bmad/schemas/v9-story-contract-v1.schema.json": "story-contract-schema",
        INVENTORY_SCHEMA_PATH: "frozen-input-inventory-schema",
        "_bmad/schemas/v9-acceptance-result-v1.schema.json": "acceptance-result-schema",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json": "frozen-inventory-schema",
        "_bmad/schemas/story-final-record-v2.schema.json": "story-final-record-schema",
        "_bmad/schemas/story-record-generator-failure-v1.schema.json": "generator-failure-schema",
        "_bmad/scripts/generate_story_record.py": "story-record-generator",
        "_bmad/scripts/tests/test_generate_story_record.py": "story-record-generator-tests",
    }
    if path in RESULT_PATHS:
        return "machine-result-snapshot"
    return roles[path]


def binding_mode(path: str) -> str:
    """Return the currently required committed mode for one scenario input."""

    return "100755" if path == "_bmad/scripts/generate_story_record.py" else "100644"


def expected_inventory_document() -> dict[str, Any]:
    """Return the exact closed V20 inventory required by proposal sections 5.4-5.6."""

    path_inventories = [
        {
            "role": "implementation-candidate-delta",
            "inventoryId": "V20-7.1-IMPLEMENTATION-PATHS-v1",
            "paths": list(IMPLEMENTATION_PATHS),
            "sha256": "4405332b49ec26ffff40c9b7424c858e7634e0d92313b2c0270011e29a132f4b",
        },
        {
            "role": "machine-results",
            "inventoryId": "V20-7.1-RESULT-PATHS-v1",
            "paths": list(RESULT_PATHS),
            "sha256": "f54595279fc201056604e56971f2e22f5483c505ddb1a2488b69e513f08b0fef",
        },
        {
            "role": "post-candidate-record-outputs",
            "inventoryId": "V20-7.1-RECORD-OUTPUT-PATHS-v1",
            "paths": list(RECORD_OUTPUT_PATHS),
            "sha256": "80b1c47320b8f4ebb98e0dfb40d6777cded1e4e08541c7734678991ab5598b64",
        },
    ]
    scenarios = []
    for scenario, paths in SCENARIO_PATHS.items():
        inputs = [
            {
                "path": path,
                "sha256": "recompute-from-committed-SC-7.1-or-machine-result-snapshot",
                "mode": binding_mode(path),
                "role": binding_role(path),
            }
            for path in paths
        ]
        scenarios.append(
            {
                "scenarioId": scenario,
                "inventoryId": SCENARIO_IDENTITIES[scenario],
                "paths": list(paths),
                "sha256": SCENARIO_DIGESTS[scenario],
                "orderedInputBindings": inputs,
            }
        )
    return {
        "schemaVersion": INVENTORY_SCHEMA_VERSION,
        "inventoryId": "V20-STORY-7.1-INPUT-INVENTORY-v1",
        "digestAlgorithm": "sha256",
        "canonicalization": {
            "rule": "nfc-utf8-lf-path-list",
            "itemEncoding": "one-repository-relative-path-per-line",
            "terminalLf": True,
            "orderSignificant": True,
        },
        "pathInventories": path_inventories,
        "authorityPrefix": {
            "inventoryId": "V20-7.1-AUTHORITY-PREFIX-v1",
            "paths": list(AUTHORITY_PREFIX),
            "sha256": inventory_digest(AUTHORITY_PREFIX),
        },
        "currentInputObservations": [
            {"path": path, "role": role, "rawSha256": digest} for path, role, digest in CURRENT_INPUTS
        ],
        "candidateDerivedBindings": {
            "checkpointCandidate": [
                {
                    "path": path,
                    "rule": "recompute-raw-mode-and-sha256-at-V19-checkpoint-candidate",
                }
                for path in CHECKPOINT_PATHS[:-1]
            ],
            "storyCandidate": [
                {"path": path, "rule": "recompute-raw-mode-and-sha256-at-SC-7.1"}
                for path in IMPLEMENTATION_PATHS
            ],
            "machineResults": [
                {"path": path, "rule": "generator-captured-actual-exit-and-ID-derived-XML-snapshot"}
                for path in RESULT_PATHS
            ],
        },
        "entryObligations": {
            "inventoryId": "V9-7.1-ENTRY-v1",
            "items": [
                "V8-6.8-AC1",
                "V8-6.8-AC6-ANTI-VACUITY",
                "V8-6.8-PROHIBITIONS-SOURCE-BOUNDARY",
            ],
            "sha256": "5fb79e8d9251c3187f2a2de7d4ae3766ab962015e628d345f8033bf14ba8e36e",
        },
        "scenarioInventories": scenarios,
        "scenarioResultBinding": {
            "requiredFields": ["path", "sha256", "mode", "role"],
            "orderedBy": "scenario-inventory-path-order",
            "declaredDigestTrusted": False,
            "candidateMoveInvalidatesResults": True,
        },
    }


def validate_inventory_document(document: dict[str, Any]) -> None:
    """Recompute every path digest and exact identity in the V20 inventory."""

    expected = expected_inventory_document()
    if document != expected:
        raise SuccessorAuthorityError("V20_INVENTORY_CONTENT_DRIFT", INVENTORY_PATH)
    inventories = [*document["pathInventories"], document["authorityPrefix"], *document["scenarioInventories"]]
    for row in inventories:
        paths = row["paths"]
        if len(paths) != len(set(paths)) or any(safe_path(path) != path for path in paths):
            raise SuccessorAuthorityError("V20_INVENTORY_PATH_SET_DRIFT", row["inventoryId"])
        observed = inventory_digest(paths)
        if observed != row["sha256"]:
            raise SuccessorAuthorityError("V20_INVENTORY_DIGEST_DRIFT", f"{row['inventoryId']}: {observed}")
    for scenario in document["scenarioInventories"]:
        bindings = scenario["orderedInputBindings"]
        if [row["path"] for row in bindings] != scenario["paths"]:
            raise SuccessorAuthorityError("V20_SCENARIO_BINDING_ORDER_DRIFT", scenario["scenarioId"])


def inventory_route(root: Path, *, check: bool) -> dict[str, Any]:
    """Write or validate the frozen V20 inventory without publishing an authority."""

    root = root.resolve()
    expected = expected_inventory_document()
    schema_path = root / INVENTORY_SCHEMA_PATH
    try:
        schema_content = schema_path.read_bytes()
    except OSError as error:
        raise SuccessorAuthorityError("V20_INVENTORY_SCHEMA_MISSING", str(error), "BLOCKED") from error
    validate_json_schema(schema_content, expected, "V20_INVENTORY_SCHEMA_INVALID")
    validate_inventory_document(expected)
    for path, _role, digest in CURRENT_INPUTS:
        try:
            content = (root / safe_path(path)).read_bytes()
        except OSError as error:
            raise SuccessorAuthorityError("V20_FIXED_INPUT_MISSING", f"{path}: {error}", "BLOCKED") from error
        if sha256(content) != digest:
            raise SuccessorAuthorityError("V20_FIXED_INPUT_DRIFT", f"{path}: {sha256(content)}")
    target = root / INVENTORY_PATH
    if check:
        try:
            existing_bytes = target.read_bytes()
        except OSError as error:
            raise SuccessorAuthorityError("V20_INVENTORY_MISSING", str(error), "BLOCKED") from error
        existing = parse_json(existing_bytes, "V20_INVENTORY_INVALID")
        validate_json_schema(schema_content, existing, "V20_INVENTORY_SCHEMA_INVALID")
        validate_inventory_document(existing)
        if existing_bytes != json_bytes(expected):
            raise SuccessorAuthorityError("V20_INVENTORY_BYTES_DRIFT", INVENTORY_PATH)
    else:
        write_atomic(root, INVENTORY_PATH, json_bytes(expected), "V20_INVENTORY_WRITE_FAILED")
    return expected


def validate_committed_inventory(root: Path, entry: str) -> tuple[bytes, bytes]:
    """Validate the exact committed inventory schema/data pair at entry."""

    schema_content = schema_at(root, entry, INVENTORY_SCHEMA_PATH, "V20_INVENTORY_SCHEMA_NOT_COMMITTED")
    inventory_content = candidate_blob(root, entry, INVENTORY_PATH, "V20_INVENTORY_NOT_COMMITTED")
    mode, object_type, _ = raw_tree_record(root, entry, INVENTORY_PATH, "V20_INVENTORY_NOT_COMMITTED")
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError("V20_INVENTORY_MODE_DRIFT", f"{mode} {object_type}")
    document = parse_json(inventory_content, "V20_INVENTORY_INVALID")
    validate_json_schema(schema_content, document, "V20_INVENTORY_SCHEMA_INVALID")
    validate_inventory_document(document)
    if inventory_content != json_bytes(expected_inventory_document()):
        raise SuccessorAuthorityError("V20_INVENTORY_BYTES_DRIFT", INVENTORY_PATH)
    return inventory_content, schema_content


def validate_owner_fields(identity: str, decided_at_utc: str, rationale: str) -> None:
    """Require a concrete independent human identity, UTC instant, and rationale."""

    if not identity.strip() or identity.strip().lower() == "release owner":
        raise SuccessorAuthorityError("V20_OWNER_IDENTITY_INVALID", repr(identity))
    if not rationale.strip() or V19_AUTHORITY_ID not in rationale or "V20-STORY-7.1-INPUT-INVENTORY-v1" not in rationale:
        raise SuccessorAuthorityError("V20_OWNER_RATIONALE_INVALID", "rationale must bind V19 and the frozen inventory")
    if re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", decided_at_utc) is None:
        raise SuccessorAuthorityError("V20_OWNER_DECISION_TIME_INVALID", repr(decided_at_utc))
    try:
        parsed = datetime.strptime(decided_at_utc, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
    except ValueError as error:
        raise SuccessorAuthorityError("V20_OWNER_DECISION_TIME_INVALID", str(error)) from error
    if parsed.year < 2026:
        raise SuccessorAuthorityError("V20_OWNER_DECISION_TIME_INVALID", decided_at_utc)


def check_v19_at(root: Path, evaluated: str) -> tuple[dict[str, Any], str, bytes]:
    """Validate the one committed current V19 reachable from an entry candidate."""

    publication = locate_publication(root, evaluated, V19_PATH, None, "V20_V19_PUBLICATION_MISSING")
    existing_bytes = candidate_blob(root, publication, V19_PATH, "V20_V19_AUTHORITY_MISSING")
    existing = parse_json(existing_bytes, "V20_V19_AUTHORITY_INVALID")
    fresh = existing.get("freshCheckpoint")
    if not isinstance(fresh, dict) or not isinstance(fresh.get("candidateCommit"), str):
        raise SuccessorAuthorityError("V20_V19_AUTHORITY_INVALID", "freshCheckpoint.candidateCommit")
    candidate = resolve_commit(root, fresh["candidateCommit"], "V20_V19_CANDIDATE_UNAVAILABLE")
    expected = render_v19(root, candidate)
    validate_json_schema(schema_at(root, candidate, V19_SCHEMA_PATH, "V20_V19_SCHEMA_MISSING"), expected, "V20_V19_SCHEMA_INVALID")
    if existing != expected or existing_bytes != json_bytes(expected):
        raise SuccessorAuthorityError("V20_V19_AUTHORITY_DRIFT", V19_PATH)
    validate_publication(
        root,
        source=candidate,
        publication=publication,
        evaluated=evaluated,
        path=V19_PATH,
        prefix="V20_V19",
    )
    return expected, publication, existing_bytes


def committed_binding(root: Path, entry: str, path: str, role: str) -> dict[str, str]:
    """Bind one exact committed regular input at the entry candidate."""

    mode, object_type, _ = raw_tree_record(root, entry, path, "V20_ENTRY_INPUT_MISSING")
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError("V20_ENTRY_INPUT_MODE_DRIFT", f"{path}: {mode} {object_type}")
    content = candidate_blob(root, entry, path, "V20_ENTRY_INPUT_MISSING")
    return {"path": path, "sha256": sha256(content), "mode": mode, "role": role}


def render_v20(
    root: Path,
    *,
    entry_revision: str,
    owner_identity: str,
    decided_at_utc: str,
    rationale: str,
) -> dict[str, Any]:
    """Recompute the release-owner-authored, entry-candidate-bound V20 authority."""

    root = root.resolve()
    entry = resolve_commit(root, entry_revision, "V20_ENTRY_CANDIDATE_UNAVAILABLE")
    validate_owner_fields(owner_identity, decided_at_utc, rationale)
    v19, v19_publication, v19_bytes = check_v19_at(root, entry)
    require_ancestor(root, v19_publication, entry, "V20_ENTRY_NOT_AFTER_V19")
    checkpoint = v19["freshCheckpoint"]["candidateCommit"]
    if len({PLANNING_CANDIDATE, checkpoint, entry}) != 3:
        raise SuccessorAuthorityError("V20_CANDIDATE_ROLE_CONFLATION", f"pc={PLANNING_CANDIDATE} checkpoint={checkpoint} entry={entry}")
    for binding in v19["freshCheckpoint"]["changedPathBindings"]:
        path = binding["path"]
        mode, object_type, _ = raw_tree_record(root, entry, path, "V20_CHECKPOINT_INPUT_MISSING")
        content = candidate_blob(root, entry, path, "V20_CHECKPOINT_INPUT_MISSING")
        if mode != binding["mode"] or object_type != "blob" or sha256(content) != binding["sha256"]:
            raise SuccessorAuthorityError("V20_CHECKPOINT_INPUT_DRIFT", path)
    checkpoint_owned = set(CHECKPOINT_PATHS[:-1])
    for path, _role, digest in CURRENT_INPUTS:
        if path in checkpoint_owned or path == SEMANTIC_SOURCE_PATH:
            continue
        mode, object_type, _ = raw_tree_record(root, entry, path, "V20_FIXED_ENTRY_INPUT_MISSING")
        content = candidate_blob(root, entry, path, "V20_FIXED_ENTRY_INPUT_MISSING")
        if mode != binding_mode(path) or object_type != "blob" or sha256(content) != digest:
            raise SuccessorAuthorityError("V20_FIXED_ENTRY_INPUT_DRIFT", path)
    inventory_content, inventory_schema_content = validate_committed_inventory(root, entry)
    semantic = candidate_blob(root, entry, SEMANTIC_SOURCE_PATH, "V20_SEMANTIC_SOURCE_NOT_COMMITTED")
    semantic_mode, semantic_type, _ = raw_tree_record(
        root, entry, SEMANTIC_SOURCE_PATH, "V20_SEMANTIC_SOURCE_NOT_COMMITTED"
    )
    if semantic_mode != "100644" or semantic_type != "blob" or sha256(semantic) != SEMANTIC_SOURCE_SHA256:
        raise SuccessorAuthorityError("V20_SEMANTIC_SOURCE_DRIFT", f"{semantic_mode} {sha256(semantic)}")
    entry_paths = (
        (V19_PATH, "checkpoint-completion-authority"),
        (V19_SCHEMA_PATH, "checkpoint-completion-authority-schema"),
        (V20_SCHEMA_PATH, "release-owner-authority-schema"),
        (INVENTORY_PATH, "frozen-input-inventory"),
        (INVENTORY_SCHEMA_PATH, "frozen-input-inventory-schema"),
        (PUBLISHER_PATH, "successor-authority-publisher"),
        (PUBLISHER_TEST_PATH, "successor-authority-tests"),
        (SEMANTIC_SOURCE_PATH, "approved-semantic-source"),
    )
    entry_bindings = [committed_binding(root, entry, path, role) for path, role in entry_paths]
    if entry_bindings[0]["sha256"] != sha256(v19_bytes):
        raise SuccessorAuthorityError("V20_V19_BINDING_DRIFT", entry_bindings[0]["sha256"])
    ledger = [
        {"id": "V20-C1", "subject": "committed-current-v19-pass", "state": "PASS"},
        {"id": "V20-C2", "subject": "independent-owner-fields-and-exact-7.1-scope", "state": "PASS"},
        {"id": "V20-C3", "subject": "planning-checkpoint-entry-and-eventual-story-roles", "state": "PASS"},
        {"id": "V20-C4", "subject": "committed-inventory-schema-data-and-semantic-source", "state": "PASS"},
        {"id": "V20-C5", "subject": "exact-story-and-scenario-inventory-identities", "state": "PASS"},
        {"id": "V20-C6", "subject": "narrow-no-done-no-7.2-no-release-no-push-effect", "state": "PASS"},
        {"id": "V20-C7", "subject": "fail-closed-effective-hold-composition", "state": "PASS"},
    ]
    return {
        "schemaVersion": V20_SCHEMA_VERSION,
        "authorityId": V20_AUTHORITY_ID,
        "predecessor": {"authorityId": V19_AUTHORITY_ID, "path": V19_PATH, "sha256": sha256(v19_bytes)},
        "ownerDecision": {
            "owner": "Release owner",
            "identity": owner_identity.strip(),
            "decidedAtUtc": decided_at_utc,
            "rationale": rationale.strip(),
            "decision": "LIFTED",
        },
        "scope": {
            "unlocks": ["7.1"],
            "global": False,
            "checkpointReauthorized": False,
            "storyDoneAllowedWithoutAcceptance": False,
            "successorUnlocked": False,
        },
        "candidateRoles": {
            "planningCandidate": PLANNING_CANDIDATE,
            "checkpointCandidate": checkpoint,
            "entryCandidate": entry,
            "eventualStoryCandidate": "SC-7.1",
            "rolesAreInterchangeable": False,
            "scKnownAtDecisionTime": False,
            "scMustDescendFromEntry": True,
            "scMustBindCurrentRawGitlinks": True,
        },
        "storyWorkBaseline": {
            "resolution": "unique-single-parent-authority-publication",
            "parent": entry,
            "exactChangedPaths": [V20_PATH],
            "resolvedAtSc": True,
        },
        "authorityBundle": {
            "path": V9_BUNDLE_PATH,
            "rawSha256": V9_BUNDLE_RAW_SHA256,
            "bundleDigest": V9_BUNDLE_DIGEST,
        },
        "v11Authority": v19["v11Authority"],
        "frozenInputs": {
            "inventoryPath": INVENTORY_PATH,
            "inventorySha256": sha256(inventory_content),
            "schemaPath": INVENTORY_SCHEMA_PATH,
            "schemaSha256": sha256(inventory_schema_content),
        },
        "semanticSource": {
            "path": SEMANTIC_SOURCE_PATH,
            "sha256": SEMANTIC_SOURCE_SHA256,
            "mode": semantic_mode,
            "preservedUnchanged": True,
        },
        "entryBindings": entry_bindings,
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True},
        "assertionLedger": ledger,
        "result": "PASS",
        "authorityEffect": {
            "implementationHold": "LIFTED",
            "fullStoryExecutionAllowed": True,
            "storyDoneAllowedWithoutAcceptance": False,
            "story71CandidateBindingResolved": True,
            "story72Unlocked": False,
            "releaseAuthorized": False,
            "pushAuthorized": False,
        },
    }


def publish_v20(
    root: Path,
    *,
    entry_revision: str | None,
    owner_identity: str | None,
    decided_at_utc: str | None,
    rationale: str | None,
    check: bool,
    publication_revision: str | None = None,
) -> dict[str, Any]:
    """Write V20 or validate its exact-one-path committed publication."""

    root = root.resolve()
    if check:
        evaluated = resolve_commit(root, entry_revision or "HEAD", "V20_EVALUATED_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated, V20_PATH, publication_revision, "V20_PUBLICATION_MISSING")
        existing_bytes = candidate_blob(root, publication, V20_PATH, "V20_AUTHORITY_MISSING")
        existing = parse_json(existing_bytes, "V20_AUTHORITY_INVALID")
        roles = existing.get("candidateRoles")
        owner = existing.get("ownerDecision")
        if not isinstance(roles, dict) or not isinstance(owner, dict):
            raise SuccessorAuthorityError("V20_AUTHORITY_INVALID", "candidateRoles or ownerDecision")
        entry_value = roles.get("entryCandidate")
        identity_value = owner.get("identity")
        decided_value = owner.get("decidedAtUtc")
        rationale_value = owner.get("rationale")
        if not all(isinstance(value, str) for value in (entry_value, identity_value, decided_value, rationale_value)):
            raise SuccessorAuthorityError("V20_AUTHORITY_INVALID", "owner or entry fields")
        entry = resolve_commit(root, entry_value, "V20_ENTRY_CANDIDATE_UNAVAILABLE")
        identity = identity_value
        decided = decided_value
        rationale_text = rationale_value
    else:
        if not all((entry_revision, owner_identity, decided_at_utc, rationale)):
            raise SuccessorAuthorityError(
                "V20_OWNER_INPUTS_REQUIRED",
                "--entry-candidate, --owner-identity, --decided-at-utc, and --rationale are required",
                "BLOCKED",
            )
        entry = resolve_commit(root, str(entry_revision), "V20_ENTRY_CANDIDATE_UNAVAILABLE")
        identity = str(owner_identity)
        decided = str(decided_at_utc)
        rationale_text = str(rationale)
        existing = None
        existing_bytes = b""
    document = render_v20(
        root,
        entry_revision=entry,
        owner_identity=identity,
        decided_at_utc=decided,
        rationale=rationale_text,
    )
    validate_json_schema(schema_at(root, entry, V20_SCHEMA_PATH, "V20_SCHEMA_NOT_COMMITTED"), document, "V20_SCHEMA_INVALID")
    if check:
        if existing != document or existing_bytes != json_bytes(document):
            raise SuccessorAuthorityError("V20_AUTHORITY_DRIFT", V20_PATH)
        validate_publication(
            root,
            source=entry,
            publication=publication,
            evaluated=evaluated,
            path=V20_PATH,
            prefix="V20",
        )
    else:
        write_atomic(root, V20_PATH, json_bytes(document), "V20_WRITE_FAILED")
    return document


def effective_hold(root: Path, *, evaluated_revision: str | None = None) -> dict[str, Any]:
    """Evaluate the effective Story 7.1 hold, defaulting every V20 fault to ACTIVE."""

    try:
        authority = publish_v20(
            root,
            entry_revision=evaluated_revision,
            owner_identity=None,
            decided_at_utc=None,
            rationale=None,
            check=True,
        )
    except SuccessorAuthorityError as error:
        return {
            "schemaVersion": "hexalith.conversations.story-7.1-effective-hold-result.v1",
            "result": error.state,
            "effectiveHold": "ACTIVE",
            "assertionLedger": [
                {"id": error.code, "subject": "Story 7.1 successor authority", "state": error.state, "detail": error.detail}
            ],
            "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
        }
    return {
        "schemaVersion": "hexalith.conversations.story-7.1-effective-hold-result.v1",
        "result": "PASS",
        "effectiveHold": authority["authorityEffect"]["implementationHold"],
        "assertionLedger": authority["assertionLedger"],
        "blockers": [],
    }


def failure_document(root: Path, route: str, error: SuccessorAuthorityError) -> dict[str, Any]:
    """Return a parseable nonvacuous failure result with stable state semantics."""

    result = {
        "schemaVersion": "hexalith.conversations.story-7.1-successor-authority-result.v1",
        "route": route,
        "result": error.state,
        "repository": str(root.resolve()),
        "resultSemantics": list(RESULT_STATES),
        "assertionLedger": [
            {"id": error.code, "subject": f"Story 7.1 {route}", "state": error.state, "detail": error.detail}
        ],
        "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
    }
    if route == "v20":
        result["effectiveHold"] = "ACTIVE"
    return result


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the inventory, V19, or V20 publication/check route."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    routes = parser.add_subparsers(dest="route", required=True)
    inventory_parser = routes.add_parser("inventory")
    inventory_parser.add_argument("--check", action="store_true")
    v19_parser = routes.add_parser("v19")
    v19_parser.add_argument("--candidate")
    v19_parser.add_argument("--publication")
    v19_parser.add_argument("--check", action="store_true")
    v20_parser = routes.add_parser("v20")
    v20_parser.add_argument("--entry-candidate")
    v20_parser.add_argument("--publication")
    v20_parser.add_argument("--owner-identity")
    v20_parser.add_argument("--decided-at-utc")
    v20_parser.add_argument("--rationale")
    v20_parser.add_argument("--effective-hold", action="store_true")
    v20_parser.add_argument("--check", action="store_true")
    args = parser.parse_args(arguments)
    root = Path(args.repository)
    try:
        if args.route == "inventory":
            document = inventory_route(root, check=args.check)
            print(f"V20_STORY_7_1_INVENTORY_OK INVENTORIES={len(document['scenarioInventories']) + 4}")
        elif args.route == "v19":
            document = publish_v19(
                root,
                candidate_revision=args.candidate,
                publication_revision=args.publication,
                check=args.check,
            )
            print(
                "V19_STORY_7_1_CHECKPOINT_AUTHORITY_OK "
                f"CANDIDATE={document['freshCheckpoint']['candidateCommit']} "
                f"TESTS={document['freshCheckpoint']['machineResult']['tests']}"
            )
        elif args.effective_hold:
            result = effective_hold(root, evaluated_revision=args.entry_candidate)
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return 0 if result["result"] == "PASS" else (2 if result["result"] == "BLOCKED" else 1)
        else:
            document = publish_v20(
                root,
                entry_revision=args.entry_candidate,
                publication_revision=args.publication,
                owner_identity=args.owner_identity,
                decided_at_utc=args.decided_at_utc,
                rationale=args.rationale,
                check=args.check,
            )
            print(
                "V20_STORY_7_1_RELEASE_OWNER_AUTHORITY_OK "
                f"ENTRY={document['candidateRoles']['entryCandidate']} EFFECTIVE_HOLD=LIFTED"
            )
        return 0
    except SuccessorAuthorityError as error:
        sys.stdout.write(json.dumps(failure_document(root, args.route, error), indent=2, ensure_ascii=False) + "\n")
        return 2 if error.state == "BLOCKED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
