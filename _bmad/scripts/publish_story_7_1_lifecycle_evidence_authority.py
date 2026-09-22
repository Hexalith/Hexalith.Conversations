#!/usr/bin/env python3
"""Publish and verify the non-executable V27 Story 7.1 lifecycle-evidence authority successor."""

from __future__ import annotations

import argparse
import configparser
import hashlib
import json
import os
import re
import subprocess
import sys
import unicodedata
import uuid
from pathlib import Path, PurePosixPath
from typing import Any, NoReturn, Sequence


SCHEMA_VERSION = "hexalith.conversations.story-7.1-lifecycle-evidence-authority.v1"
RESULT_SCHEMA_VERSION = "hexalith.conversations.current-planning-authority-result.v1"
SUCCESSOR_ID = "V27-STORY-7.1-LIFECYCLE-EVIDENCE-AUTHORITY-v1"

RECORD_PATH = "_bmad-output/planning-artifacts/v27-story-7.1-lifecycle-evidence-authority-v1.json"
SCHEMA_PATH = "_bmad/schemas/v27-story-7.1-lifecycle-evidence-authority-v1.schema.json"
PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_lifecycle_evidence_authority.py"
PUBLISHER_TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_lifecycle_evidence_authority.py"
RESOLVER_PATH = "_bmad/scripts/resolve_current_planning_authority.py"
RESOLVER_TEST_PATH = "_bmad/scripts/tests/test_resolve_current_planning_authority.py"
VERIFIER_PATH = "_bmad/scripts/verify_evidence_boundary.py"
VERIFIER_TEST_PATH = "_bmad/scripts/tests/test_verify_evidence_boundary.py"
WORKFLOW_PATH = ".github/workflows/planning-authority-preflight.yml"
GITMODULES_PATH = ".gitmodules"

BOOTSTRAP_PATHS = tuple(
    sorted(
        (
            WORKFLOW_PATH,
            SCHEMA_PATH,
            PUBLISHER_PATH,
            RESOLVER_PATH,
            PUBLISHER_TEST_PATH,
            RESOLVER_TEST_PATH,
            VERIFIER_TEST_PATH,
            VERIFIER_PATH,
        )
    )
)
BOOTSTRAP_IDENTITY_PATHS = (SCHEMA_PATH, PUBLISHER_PATH, PUBLISHER_TEST_PATH)
GITLINK_COUNT = 10
REQUIRED_MODE = "100644"
REPOSITORY_MARKER = "Hexalith.Conversations.slnx"

# Pinned root of trust for the closed V27 contracts. The bootstrap publication that carries this
# publisher is authorized externally, by protected-host ancestry, never by candidate content.
V27_SCHEMA_SHA256 = "d8f6920c00821cab1f0d1e434ad6c8faf965e9bd9f60f5655e7c734683700399"

GIT_EXECUTABLE = "/usr/bin/git"
TRUSTED_EXECUTABLE_PATH = "/usr/bin:/bin"
GIT_TIMEOUT_SECONDS = 30

_COMMIT = re.compile(r"^[0-9a-f]{40}$")

ASSERTION_SUBJECTS: tuple[tuple[str, str], ...] = (
    (
        "bootstrap-publication-discovery",
        "full history introduces exactly one V27 bootstrap publication",
    ),
    (
        "protected-host-authorization",
        "the bootstrap publication is contained in protected-host ancestry",
    ),
    (
        "bootstrap-exact-scope",
        "the bootstrap declares exactly eight mode-100644 paths under one parent",
    ),
    (
        "pinned-bootstrap-identities",
        "the pinned V27 schema digest matches the committed bootstrap blob",
    ),
    (
        "submodule-binding",
        "the .gitmodules inventory binds exactly ten raw mode-160000 root gitlinks",
    ),
    (
        "governed-no-touch-history",
        "no commit after the bootstrap touches a governed path, even when restored",
    ),
    (
        "record-only-publication",
        "the record is a direct single-path mode-100644 child of the bootstrap",
    ),
    (
        "deterministic-record-identity",
        "the committed record equals its deterministic canonical projection",
    ),
    (
        "sticky-record-history",
        "the record remains byte-identical through the evaluated candidate",
    ),
    (
        "non-executable-authority",
        "V27 grants no approval, execution, release, or push authority",
    ),
)
assert len(ASSERTION_SUBJECTS) == 10


class SuccessorError(RuntimeError):
    """Represent one stable fail-closed V27 diagnostic."""

    def __init__(self, code: str, detail: str, state: str = "BLOCKED", route: str | None = None) -> None:
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail
        self.state = state
        self.route = route or ("DRIFT" if state == "FAIL" else "BLOCKED")


def sha256_bytes(content: bytes) -> str:
    """Return one lowercase SHA-256 digest."""

    return hashlib.sha256(content).hexdigest()


def canonical_json(document: Any) -> bytes:
    """Render canonical LF-terminated JSON bytes."""

    return (json.dumps(document, ensure_ascii=False, indent=2) + "\n").encode("utf-8")


def safe_path(value: str) -> str:
    """Require one normalized repository-relative POSIX path."""

    path = PurePosixPath(value)
    if not value or path.is_absolute() or ".." in path.parts or "\\" in value or str(path) != value:
        raise SuccessorError("V27_PATH_INVALID", repr(value))
    return value


def trusted_environment() -> dict[str, str]:
    """Return a Git environment without ambient configuration or replacement objects."""

    return {
        "GIT_CONFIG_GLOBAL": os.devnull,
        "GIT_CONFIG_NOSYSTEM": "1",
        "GIT_CONFIG_SYSTEM": os.devnull,
        "GIT_NO_REPLACE_OBJECTS": "1",
        "GIT_TERMINAL_PROMPT": "0",
        "LANG": "C.UTF-8",
        "LC_ALL": "C.UTF-8",
        "PATH": TRUSTED_EXECUTABLE_PATH,
    }


def run_git(
    root: Path,
    *arguments: str,
    code: str = "V27_GIT_OBJECT_UNAVAILABLE",
    allowed: tuple[int, ...] = (0,),
) -> subprocess.CompletedProcess[bytes]:
    """Run the pinned Git executable with replacement objects and ambient config disabled."""

    try:
        completed = subprocess.run(
            [GIT_EXECUTABLE, "--no-replace-objects", "-C", str(root), *arguments],
            check=False,
            capture_output=True,
            env=trusted_environment(),
            timeout=GIT_TIMEOUT_SECONDS,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SuccessorError("V27_GIT_UNAVAILABLE", str(error)) from error
    if completed.returncode not in allowed:
        detail = completed.stderr.decode("utf-8", errors="replace").strip()
        raise SuccessorError(code, detail or f"git {' '.join(arguments)} failed")
    return completed


def git_text(root: Path, *arguments: str, code: str = "V27_GIT_OBJECT_UNAVAILABLE") -> str:
    """Run Git and decode one stripped textual result."""

    return run_git(root, *arguments, code=code).stdout.decode("utf-8", errors="strict").strip()


def resolve_root(start: Path) -> Path:
    """Resolve and confirm the owning repository root."""

    try:
        completed = subprocess.run(
            [GIT_EXECUTABLE, "--no-replace-objects", "-C", str(start), "rev-parse", "--show-toplevel"],
            check=False,
            capture_output=True,
            env=trusted_environment(),
            timeout=GIT_TIMEOUT_SECONDS,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SuccessorError("V27_REPOSITORY_UNAVAILABLE", str(error)) from error
    if completed.returncode != 0:
        raise SuccessorError("V27_REPOSITORY_UNAVAILABLE", completed.stderr.decode("utf-8", "replace").strip())
    root = Path(completed.stdout.decode("utf-8", errors="strict").strip()).resolve()
    if not (root / REPOSITORY_MARKER).is_file():
        raise SuccessorError("V27_REPOSITORY_ROOT_MISMATCH", str(root))
    return root


def resolve_commit(root: Path, revision: str, code: str = "V27_REVISION_UNAVAILABLE") -> str:
    """Resolve one revision to its canonical 40-hex commit identity."""

    commit = git_text(root, "rev-parse", "--verify", f"{revision}^{{commit}}", code=code)
    if not _COMMIT.fullmatch(commit):
        raise SuccessorError(code, repr(commit))
    return commit


def commit_tree(root: Path, commit: str) -> str:
    """Resolve one commit's exact tree identity."""

    tree = git_text(root, "rev-parse", "--verify", f"{commit}^{{tree}}", code="V27_TREE_UNAVAILABLE")
    if not _COMMIT.fullmatch(tree):
        raise SuccessorError("V27_TREE_UNAVAILABLE", repr(tree))
    return tree


def commit_parents(root: Path, commit: str) -> tuple[str, ...]:
    """Return one commit's exact ordinal parent list."""

    parts = git_text(root, "rev-list", "--parents", "-n", "1", commit, code="V27_LINEAGE_UNAVAILABLE").split()
    if not parts or parts[0] != commit or any(not _COMMIT.fullmatch(part) for part in parts):
        raise SuccessorError("V27_LINEAGE_UNAVAILABLE", repr(parts))
    return tuple(parts[1:])


def require_complete_history(root: Path) -> None:
    """Require complete history before any V27 fact is derived from it.

    Shallow, truncated, or otherwise partial history cannot prove a bootstrap parent diff, a
    unique publication, or the full-history no-touch boundary, so it is a stable `BLOCKED`
    outcome rather than a drift diagnosis or a silent pass.
    """

    observed = git_text(root, "rev-parse", "--is-shallow-repository", code="V27_HISTORY_UNAVAILABLE")
    if observed != "false":
        raise SuccessorError(
            "V27_HISTORY_UNAVAILABLE",
            f"repository is shallow or history availability is unknown: {observed!r}",
        )
    promisor = run_git(
        root,
        "config",
        "--get-regexp",
        r"^(extensions\.partialclone|remote\..*\.promisor)$",
        code="V27_HISTORY_UNAVAILABLE",
        allowed=(0, 1),
    )
    if promisor.returncode == 0 and promisor.stdout.strip():
        raise SuccessorError(
            "V27_HISTORY_UNAVAILABLE",
            "repository is a partial clone; object availability is unknown",
        )


def is_ancestor(root: Path, ancestor: str, descendant: str) -> bool:
    """Return whether one commit is contained by another commit's ancestry."""

    completed = run_git(
        root,
        "merge-base",
        "--is-ancestor",
        ancestor,
        descendant,
        code="V27_ANCESTRY_UNAVAILABLE",
        allowed=(0, 1),
    )
    return completed.returncode == 0


def tree_entry(root: Path, revision: str, relative_path: str, code: str) -> tuple[str, str, str]:
    """Return the unique raw mode, object kind, and object ID for one path."""

    content = run_git(root, "ls-tree", "-z", "--full-tree", revision, "--", safe_path(relative_path), code=code).stdout
    records = [row for row in content.split(b"\0") if row]
    if len(records) != 1:
        raise SuccessorError(code, f"{revision}:{relative_path}: rows={len(records)}")
    try:
        header, raw_path = records[0].split(b"\t", 1)
        mode, kind, object_id = header.decode("ascii", errors="strict").split(" ")
        observed_path = raw_path.decode("utf-8", errors="strict")
    except (UnicodeDecodeError, ValueError) as error:
        raise SuccessorError(code, f"malformed tree row for {revision}:{relative_path}") from error
    if observed_path != relative_path or not _COMMIT.fullmatch(object_id):
        raise SuccessorError(code, f"malformed tree row for {revision}:{relative_path}")
    return mode, kind, object_id


def path_exists(root: Path, revision: str, relative_path: str) -> bool:
    """Return whether one exact committed path exists without conflating Git failure."""

    completed = run_git(
        root,
        "cat-file",
        "-e",
        f"{revision}:{safe_path(relative_path)}",
        code="V27_HISTORY_UNAVAILABLE",
        allowed=(0, 1, 128),
    )
    return completed.returncode == 0


def blob_bytes(root: Path, revision: str, relative_path: str, code: str) -> bytes:
    """Read exact committed blob bytes after requiring a regular mode-100644 file."""

    mode, kind, _object_id = tree_entry(root, revision, relative_path, code)
    if mode != REQUIRED_MODE or kind != "blob":
        raise SuccessorError(code, f"{revision}:{relative_path}: {mode} {kind}")
    return run_git(root, "cat-file", "blob", f"{revision}:{safe_path(relative_path)}", code=code).stdout


def committed_binding(root: Path, revision: str, relative_path: str, code: str) -> dict[str, Any]:
    """Bind one committed regular blob to its raw object and byte identities."""

    mode, kind, object_id = tree_entry(root, revision, relative_path, code)
    if mode != REQUIRED_MODE or kind != "blob":
        raise SuccessorError(code, f"{revision}:{relative_path}: {mode} {kind}")
    content = run_git(root, "cat-file", "blob", f"{revision}:{safe_path(relative_path)}", code=code).stdout
    return {
        "path": relative_path,
        "mode": mode,
        "objectId": object_id,
        "sha256": sha256_bytes(content),
        "bytes": len(content),
    }


def changed_paths(root: Path, parent: str, commit: str, code: str = "V27_DIFF_UNAVAILABLE") -> tuple[str, ...]:
    """Return the ordinal exact no-rename changed-path set between two commits."""

    content = run_git(
        root,
        "diff-tree",
        "--no-commit-id",
        "--name-only",
        "--no-renames",
        "-r",
        "-z",
        parent,
        commit,
        "--",
        code=code,
    ).stdout
    try:
        paths = tuple(sorted(part.decode("utf-8", errors="strict") for part in content.split(b"\0") if part))
    except UnicodeDecodeError as error:
        raise SuccessorError(code, str(error)) from error
    if len(paths) != len(set(paths)):
        raise SuccessorError("V27_CHANGED_PATH_DUPLICATE", repr(paths))
    return paths


def changed_path_rows(root: Path, parent: str, commit: str) -> list[dict[str, Any]]:
    """Return the truthful immediate-parent diff as closed observation rows."""

    content = run_git(
        root,
        "diff-tree",
        "--no-commit-id",
        "--no-renames",
        "-r",
        "-z",
        "--raw",
        parent,
        commit,
        code="V27_DIFF_UNAVAILABLE",
    ).stdout
    fields = [part for part in content.split(b"\0") if part]
    if len(fields) % 2 != 0:
        raise SuccessorError("V27_DIFF_MALFORMED", f"unpaired raw diff fields: {len(fields)}")
    rows: list[dict[str, Any]] = []
    for index in range(0, len(fields), 2):
        try:
            metadata = fields[index].decode("ascii", errors="strict")
            relative_path = fields[index + 1].decode("utf-8", errors="strict")
        except UnicodeDecodeError as error:
            raise SuccessorError("V27_DIFF_MALFORMED", str(error)) from error
        if not metadata.startswith(":"):
            raise SuccessorError("V27_DIFF_MALFORMED", repr(metadata))
        parts = metadata[1:].split()
        if len(parts) != 5:
            raise SuccessorError("V27_DIFF_MALFORMED", repr(metadata))
        _source_mode, target_mode, _source_object, target_object, _status = parts
        if not re.fullmatch(r"[0-7]{6}", target_mode) or not _COMMIT.fullmatch(target_object):
            raise SuccessorError("V27_DIFF_MALFORMED", repr(metadata))
        digest: str | None = None
        if target_mode not in ("000000", "160000"):
            digest = sha256_bytes(
                run_git(root, "cat-file", "blob", target_object, code="V27_DIFF_UNAVAILABLE").stdout
            )
        rows.append(
            {
                "path": safe_path(relative_path),
                "mode": target_mode,
                "objectId": target_object,
                "sha256": digest,
            }
        )
    return sorted(rows, key=lambda row: row["path"])


def raw_root_gitlinks(root: Path, revision: str) -> list[dict[str, str]]:
    """Derive root gitlinks exclusively from raw tree mode-160000 entries."""

    content = run_git(root, "ls-tree", "-r", "-z", "--full-tree", revision, code="V27_GITLINK_INVENTORY_UNAVAILABLE").stdout
    links: list[dict[str, str]] = []
    for record in (row for row in content.split(b"\0") if row):
        try:
            header, raw_path = record.split(b"\t", 1)
            mode, kind, object_id = header.decode("ascii", errors="strict").split(" ")
            relative_path = raw_path.decode("utf-8", errors="strict")
        except (UnicodeDecodeError, ValueError) as error:
            raise SuccessorError("V27_GITLINK_INVENTORY_INVALID", repr(record)) from error
        if mode != "160000":
            continue
        if kind != "commit" or not _COMMIT.fullmatch(object_id):
            raise SuccessorError("V27_GITLINK_INVENTORY_INVALID", relative_path)
        links.append({"path": safe_path(relative_path), "mode": mode, "objectId": object_id})
    return sorted(links, key=lambda row: row["path"])


def gitmodule_paths(content: bytes) -> tuple[str, ...]:
    """Parse and ordinally normalize the root .gitmodules path inventory."""

    parser = configparser.ConfigParser(interpolation=None, strict=True)
    parser.optionxform = str
    try:
        parser.read_string(content.decode("utf-8", errors="strict"))
    except (UnicodeDecodeError, configparser.Error) as error:
        raise SuccessorError("V27_GITMODULES_MALFORMED", str(error)) from error
    paths: list[str] = []
    for section in parser.sections():
        if not section.startswith('submodule "') or not section.endswith('"'):
            raise SuccessorError("V27_GITMODULES_MALFORMED", f"unexpected section {section!r}")
        if not parser.has_option(section, "path"):
            raise SuccessorError("V27_GITMODULES_MALFORMED", f"missing path in {section!r}")
        paths.append(safe_path(parser.get(section, "path")))
    if len(paths) != len(set(paths)):
        raise SuccessorError("V27_GITMODULES_DUPLICATE_PATH", repr(paths))
    return tuple(sorted(paths))


def submodule_binding(root: Path, revision: str, code: str) -> dict[str, Any]:
    """Bind `.gitmodules` bytes to exactly ten raw root gitlinks."""

    content = blob_bytes(root, revision, GITMODULES_PATH, code)
    declared = gitmodule_paths(content)
    links = raw_root_gitlinks(root, revision)
    if len(links) != GITLINK_COUNT or len(declared) != GITLINK_COUNT:
        raise SuccessorError(code, f"declared={len(declared)}; raw={len(links)}; expected={GITLINK_COUNT}")
    if declared != tuple(row["path"] for row in links):
        raise SuccessorError(code, f"declared={declared!r}; raw={[row['path'] for row in links]!r}")
    return {
        "gitmodulesPath": GITMODULES_PATH,
        "gitmodulesSha256": sha256_bytes(content),
        "declaredPaths": list(declared),
        "rootGitlinks": links,
    }


def additions(root: Path, evaluated: str, relative_path: str) -> tuple[str, ...]:
    """Return every full-history commit that introduces one governed path."""

    content = run_git(
        root,
        "log",
        "--full-history",
        "--format=%H",
        "--diff-filter=A",
        evaluated,
        "--",
        safe_path(relative_path),
        code="V27_HISTORY_UNAVAILABLE",
    ).stdout
    try:
        rows = tuple(row for row in content.decode("ascii", errors="strict").splitlines() if row)
    except UnicodeDecodeError as error:
        raise SuccessorError("V27_HISTORY_UNAVAILABLE", str(error)) from error
    if any(not _COMMIT.fullmatch(row) for row in rows):
        raise SuccessorError("V27_HISTORY_INVALID", repr(rows))
    return rows


def touching_commits(root: Path, since: str, evaluated: str, paths: Sequence[str]) -> tuple[str, ...]:
    """Return every full-history commit after `since` that touches one governed path."""

    if since == evaluated:
        return ()
    content = run_git(
        root,
        "rev-list",
        "--full-history",
        evaluated,
        f"^{since}",
        "--",
        *(safe_path(path) for path in paths),
        code="V27_HISTORY_UNAVAILABLE",
    ).stdout
    try:
        rows = tuple(row for row in content.decode("ascii", errors="strict").splitlines() if row)
    except UnicodeDecodeError as error:
        raise SuccessorError("V27_HISTORY_UNAVAILABLE", str(error)) from error
    if any(not _COMMIT.fullmatch(row) for row in rows):
        raise SuccessorError("V27_HISTORY_INVALID", repr(rows))
    return rows


def discover_bootstrap(root: Path, evaluated: str) -> str:
    """Discover the unique V27 bootstrap publication from committed history alone."""

    discovered: set[str] = set()
    for relative_path in BOOTSTRAP_IDENTITY_PATHS:
        rows = additions(root, evaluated, relative_path)
        if not rows:
            raise SuccessorError("V27_BOOTSTRAP_PUBLICATION_MISSING", relative_path)
        if len(rows) != 1:
            raise SuccessorError("V27_DUPLICATE_BOOTSTRAP_PUBLICATION", f"{relative_path}: {rows!r}")
        discovered.add(rows[0])
    if len(discovered) != 1:
        raise SuccessorError("V27_BOOTSTRAP_PUBLICATION_SPLIT", repr(sorted(discovered)))
    return discovered.pop()


def require_protected_host(root: Path, bootstrap: str, trusted_host: str) -> None:
    """Require the externally recorded protected host to already contain the bootstrap."""

    if not is_ancestor(root, bootstrap, trusted_host):
        raise SuccessorError(
            "V27_BOOTSTRAP_NOT_PROTECTED",
            f"bootstrap={bootstrap} is not contained by protected host={trusted_host}",
        )


def validate_bootstrap(root: Path, bootstrap: str) -> tuple[list[dict[str, Any]], str, str, str]:
    """Validate the exact eight-path bootstrap publication and its pinned identities."""

    parents = commit_parents(root, bootstrap)
    if not parents:
        raise SuccessorError(
            "V27_HISTORY_UNAVAILABLE",
            f"{bootstrap} has no available parent; its ancestry is truncated",
        )
    if len(parents) != 1:
        raise SuccessorError("V27_BOOTSTRAP_PARENT_DRIFT", repr(parents))
    parent = parents[0]
    observed = changed_paths(root, parent, bootstrap)
    if observed != BOOTSTRAP_PATHS:
        missing = sorted(set(BOOTSTRAP_PATHS) - set(observed))
        unexpected = sorted(set(observed) - set(BOOTSTRAP_PATHS))
        raise SuccessorError(
            "V27_BOOTSTRAP_SCOPE_DRIFT",
            f"missing={missing!r}; unexpected={unexpected!r}",
        )
    if path_exists(root, bootstrap, RECORD_PATH):
        raise SuccessorError("V27_COMBINED_PUBLICATION_REJECTED", RECORD_PATH)
    bindings = [
        committed_binding(root, bootstrap, relative_path, "V27_BOOTSTRAP_ARTIFACT_UNAVAILABLE")
        for relative_path in BOOTSTRAP_PATHS
    ]
    schema_binding = next(row for row in bindings if row["path"] == SCHEMA_PATH)
    if schema_binding["sha256"] != V27_SCHEMA_SHA256:
        raise SuccessorError(
            "V27_SCHEMA_IDENTITY_MISMATCH",
            f"expected={V27_SCHEMA_SHA256}; observed={schema_binding['sha256']}",
        )
    return bindings, parent, commit_tree(root, bootstrap), commit_tree(root, parent)


def validate_record_publication(root: Path, bootstrap: str, publication: str) -> None:
    """Require a direct record-only child of the authorized bootstrap."""

    parents = commit_parents(root, publication)
    if parents != (bootstrap,):
        raise SuccessorError(
            "V27_RECORD_PARENT_DRIFT",
            f"expected={(bootstrap,)!r}; observed={parents!r}",
            "FAIL",
        )
    observed = changed_paths(root, bootstrap, publication)
    if observed != (RECORD_PATH,):
        raise SuccessorError("V27_RECORD_SCOPE_DRIFT", repr(observed), "FAIL")
    mode, kind, _object_id = tree_entry(root, publication, RECORD_PATH, "V27_RECORD_MODE_DRIFT")
    if mode != REQUIRED_MODE or kind != "blob":
        raise SuccessorError("V27_RECORD_MODE_DRIFT", f"{RECORD_PATH}: {mode} {kind}", "FAIL")


def manifest_digest(bindings: Sequence[dict[str, Any]]) -> str:
    """Digest the canonical ordered self-excluded manifest rows."""

    material = "".join(
        f"{unicodedata.normalize('NFC', row['path'])}\t{row['mode']}\t{row['objectId']}\t{row['sha256']}\t{row['bytes']}\n"
        for row in bindings
    ).encode("utf-8")
    return sha256_bytes(material)


def record_ledger() -> list[dict[str, str]]:
    """Return the fixed, nonempty evaluated assertion inventory for the record."""

    return [
        {"id": f"V27.SUCCESSOR.{index:02d}", "subject": subject, "state": "PASS", "detail": detail}
        for index, (subject, detail) in enumerate(ASSERTION_SUBJECTS, start=1)
    ]


def result_semantics() -> dict[str, Any]:
    """Return the closed machine result semantics."""

    return {
        "states": ["PASS", "FAIL", "BLOCKED"],
        "exitCodes": {"PASS": 0, "FAIL": 1, "BLOCKED": 2},
        "ledgerRequired": True,
    }


def build_document(
    bindings: list[dict[str, Any]],
    bootstrap: str,
    bootstrap_tree: str,
    bootstrap_parent: str,
    bootstrap_parent_tree: str,
    submodules: dict[str, Any],
) -> dict[str, Any]:
    """Build the deterministic record-only V27 publication document."""

    if [row["path"] for row in bindings] != list(BOOTSTRAP_PATHS):
        raise SuccessorError("V27_MANIFEST_SCOPE_DRIFT", repr([row["path"] for row in bindings]), "FAIL")
    if RECORD_PATH in BOOTSTRAP_PATHS:
        raise SuccessorError("V27_RECORD_SELF_INCLUSION", RECORD_PATH, "FAIL")
    return {
        "schemaVersion": SCHEMA_VERSION,
        "recordType": "LIFECYCLE_EVIDENCE_AUTHORITY_SUCCESSOR",
        "successorId": SUCCESSOR_ID,
        "authorization": {
            "model": "PROTECTED_HOST_ANCESTRY",
            "bootstrapCommit": bootstrap,
            "bootstrapTree": bootstrap_tree,
            "bootstrapParent": bootstrap_parent,
            "bootstrapParentTree": bootstrap_parent_tree,
            "externalExceptionRequired": True,
            "selfAuthorized": False,
        },
        "bootstrapTransaction": {
            "exactChangedPaths": list(BOOTSTRAP_PATHS),
            "requiredMode": REQUIRED_MODE,
            "manifestSha256": manifest_digest(bindings),
            "manifest": bindings,
        },
        "recordTransaction": {
            "parentCommit": bootstrap,
            "parentTree": bootstrap_tree,
            "exactChangedPaths": [RECORD_PATH],
            "requiredMode": REQUIRED_MODE,
        },
        "submoduleBinding": submodules,
        "resultSemantics": result_semantics(),
        "assertionLedger": record_ledger(),
        "blockers": [],
        "result": "PASS",
        "implementationHold": "ACTIVE",
        "executionAllowed": False,
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
    }


def validate_schema(document: dict[str, Any], schema_bytes: bytes) -> None:
    """Validate one document against the pinned closed Draft 2020-12 V27 schema."""

    observed = sha256_bytes(schema_bytes)
    if observed != V27_SCHEMA_SHA256:
        raise SuccessorError("V27_SCHEMA_IDENTITY_MISMATCH", f"expected={V27_SCHEMA_SHA256}; observed={observed}")
    try:
        import jsonschema  # Imported only after the schema identity is pinned.
    except ImportError as error:  # An unavailable dependency is unavailable tooling, never drift.
        raise SuccessorError("V27_TOOLING_UNAVAILABLE", str(error)) from error
    try:
        schema = json.loads(schema_bytes)
        jsonschema.Draft202012Validator.check_schema(schema)
    except (json.JSONDecodeError, UnicodeDecodeError, jsonschema.exceptions.SchemaError) as error:
        raise SuccessorError("V27_SCHEMA_INVALID", str(error)) from error
    try:
        jsonschema.Draft202012Validator(schema).validate(document)
    except jsonschema.exceptions.ValidationError as error:
        raise SuccessorError("V27_DOCUMENT_SCHEMA_INVALID", str(error), "FAIL") from error


def pinned_schema_bytes(root: Path, bootstrap: str | None = None) -> bytes:
    """Return the digest-pinned V27 schema bytes from committed objects or the working tree."""

    if bootstrap is not None:
        content = blob_bytes(root, bootstrap, SCHEMA_PATH, "V27_SCHEMA_UNAVAILABLE")
    else:
        try:
            content = (root / SCHEMA_PATH).read_bytes()
        except OSError as error:
            raise SuccessorError("V27_SCHEMA_UNAVAILABLE", f"{SCHEMA_PATH}: {error}") from error
    observed = sha256_bytes(content)
    if observed != V27_SCHEMA_SHA256:
        raise SuccessorError("V27_SCHEMA_IDENTITY_MISMATCH", f"expected={V27_SCHEMA_SHA256}; observed={observed}")
    return content


def self_validated(root: Path, envelope: dict[str, Any], bootstrap: str | None = None) -> dict[str, Any]:
    """Validate an outgoing envelope against the pinned schema before it leaves this module."""

    try:
        validate_schema(envelope, pinned_schema_bytes(root, bootstrap))
        return envelope
    except SuccessorError as error:
        declared = envelope.get("blockers") or [{"code": "V27_RESULT_ENVELOPE_INVALID", "detail": "none"}]
        detail = f"{error.code}: {error.detail}; declared={declared[0].get('code')}"
        return result_envelope(
            "BLOCKED",
            "BLOCKED",
            empty_observation(),
            [],
            [{"code": "V27_RESULT_ENVELOPE_INVALID", "detail": detail}],
        )


def committed_schema_source(root: Path) -> str | None:
    """Return the bootstrap whose committed schema blob should validate outgoing envelopes."""

    try:
        return discover_bootstrap(root, "HEAD")
    except SuccessorError:
        return None


def bootstrap_facts(
    root: Path,
    evaluated: str,
    trusted_host: str,
    ledger: list[dict[str, str]],
    bootstrap: str,
) -> dict[str, Any]:
    """Authenticate the externally authorized bootstrap before any record is consulted."""

    record_assertion(ledger, 1, f"bootstrap={bootstrap}")
    require_protected_host(root, bootstrap, trusted_host)
    record_assertion(ledger, 2, f"protectedHost={trusted_host}")
    bindings, parent, tree, parent_tree = validate_bootstrap(root, bootstrap)
    record_assertion(ledger, 3, f"parent={parent}; paths={len(BOOTSTRAP_PATHS)}")
    record_assertion(ledger, 4, f"schemaSha256={V27_SCHEMA_SHA256}")
    submodules = submodule_binding(root, bootstrap, "V27_SUBMODULE_BINDING_DRIFT")
    record_assertion(ledger, 5, f"gitlinks={GITLINK_COUNT}")
    return {
        "bootstrap": bootstrap,
        "bindings": bindings,
        "parent": parent,
        "tree": tree,
        "parentTree": parent_tree,
        "submodules": submodules,
    }


def require_no_governed_touch(root: Path, facts: dict[str, Any], evaluated: str) -> None:
    """Reject any post-bootstrap touch of a governed path, including restored drift."""

    bootstrap = facts["bootstrap"]
    if not is_ancestor(root, bootstrap, evaluated):
        raise SuccessorError("V27_BOOTSTRAP_NOT_ANCESTOR", f"{bootstrap} is not an ancestor of {evaluated}")
    governed = [
        *BOOTSTRAP_PATHS,
        GITMODULES_PATH,
        *(row["path"] for row in facts["submodules"]["rootGitlinks"]),
    ]
    touched = touching_commits(root, bootstrap, evaluated, governed)
    if touched:
        raise SuccessorError(
            "V27_GOVERNED_PATH_TOUCHED",
            f"commits={sorted(touched)!r}",
            "FAIL",
        )
    for relative_path in BOOTSTRAP_PATHS:
        if tree_entry(root, evaluated, relative_path, "V27_GOVERNED_ARTIFACT_MISSING") != tree_entry(
            root,
            bootstrap,
            relative_path,
            "V27_BOOTSTRAP_ARTIFACT_UNAVAILABLE",
        ):
            raise SuccessorError("V27_GOVERNED_ARTIFACT_DRIFT", relative_path, "FAIL")
    if submodule_binding(root, evaluated, "V27_SUBMODULE_BINDING_DRIFT") != facts["submodules"]:
        raise SuccessorError("V27_SUBMODULE_BINDING_DRIFT", "evaluated submodule binding differs", "FAIL")


def record_assertion(ledger: list[dict[str, str]], index: int, detail: str) -> None:
    """Append one evaluated PASS assertion using the fixed subject inventory."""

    subject, _detail = ASSERTION_SUBJECTS[index - 1]
    ledger.append(
        {
            "id": f"V27.SUCCESSOR.{index:02d}",
            "subject": subject,
            "state": "PASS",
            "detail": detail,
        }
    )


def require_single_parent(root: Path, evaluated: str) -> str:
    """Return the one immediate parent; truncated and merge candidates are never evaluated."""

    parents = commit_parents(root, evaluated)
    if not parents:
        raise SuccessorError(
            "V27_HISTORY_UNAVAILABLE",
            f"{evaluated} has no available parent; its ancestry is truncated",
        )
    if len(parents) != 1:
        raise SuccessorError(
            "V27_CANDIDATE_PARENT_DRIFT",
            f"{evaluated} has {len(parents)} parents; V27 evaluates only single-parent candidates",
        )
    return parents[0]


def observation(root: Path, evaluated: str) -> dict[str, Any]:
    """Build the truthful immediate-parent observation envelope."""

    parent = require_single_parent(root, evaluated)
    return {
        "candidateCommit": evaluated,
        "candidateTree": commit_tree(root, evaluated),
        "parentCommit": parent,
        "parentTree": commit_tree(root, parent),
        "changedPaths": changed_path_rows(root, parent, evaluated),
        "parentGitlinks": raw_root_gitlinks(root, parent),
        "candidateGitlinks": raw_root_gitlinks(root, evaluated),
    }


def empty_observation() -> dict[str, Any]:
    """Return the closed observation envelope used when facts cannot be derived."""

    return {
        "candidateCommit": None,
        "candidateTree": None,
        "parentCommit": None,
        "parentTree": None,
        "changedPaths": [],
        "parentGitlinks": [],
        "candidateGitlinks": [],
    }


def result_envelope(
    result: str,
    route: str,
    observed: dict[str, Any],
    ledger: list[dict[str, str]],
    blockers: list[dict[str, str]],
) -> dict[str, Any]:
    """Build one closed route-discriminated non-executable V27 result."""

    exit_codes = {"PASS": 0, "FAIL": 1, "BLOCKED": 2}
    route_state = {"C2": "PASS", "DESCENDANT": "PASS", "C1": "BLOCKED", "BLOCKED": "BLOCKED", "DRIFT": "FAIL"}[route]
    route_row = {
        "id": f"V27.ROUTE.{route}",
        "subject": "v27-lifecycle-evidence-authority-route",
        "state": route_state,
        "detail": blockers[0]["detail"] if blockers else f"route={route}",
    }
    return {
        "schemaVersion": RESULT_SCHEMA_VERSION,
        "result": result,
        "exitCode": exit_codes[result],
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": observed,
        "assertionLedger": [route_row, *ledger],
        "blockers": blockers,
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
    }


def verify_revision(root: Path, revision: str, trusted_host: str) -> dict[str, Any]:
    """Evaluate one candidate against the externally authorized V27 boundary."""

    ledger: list[dict[str, str]] = []
    evaluated: str | None = None
    bootstrap: str | None = None
    try:
        evaluated = resolve_commit(root, revision, "V27_EVALUATED_CANDIDATE_UNAVAILABLE")
        host = resolve_commit(root, trusted_host, "V27_PROTECTED_HOST_UNAVAILABLE")
        require_complete_history(root)
        require_single_parent(root, evaluated)
        bootstrap = discover_bootstrap(root, evaluated)
        facts = bootstrap_facts(root, evaluated, host, ledger, bootstrap)
        require_no_governed_touch(root, facts, evaluated)
        record_assertion(ledger, 6, f"governedPaths={len(BOOTSTRAP_PATHS) + 1 + GITLINK_COUNT}")

        publications = additions(root, evaluated, RECORD_PATH)
        if not publications:
            route = "C1" if evaluated == facts["bootstrap"] else "BLOCKED"
            raise SuccessorError("V27_C2_PUBLICATION_MISSING", evaluated, "BLOCKED", route)
        if len(publications) != 1:
            raise SuccessorError("V27_DUPLICATE_RECORD_PUBLICATION", repr(publications))
        publication = publications[0]
        validate_record_publication(root, facts["bootstrap"], publication)
        record_assertion(ledger, 7, f"publication={publication}")

        schema_bytes = blob_bytes(root, facts["bootstrap"], SCHEMA_PATH, "V27_SCHEMA_UNAVAILABLE")
        expected = build_document(
            facts["bindings"],
            facts["bootstrap"],
            facts["tree"],
            facts["parent"],
            facts["parentTree"],
            facts["submodules"],
        )
        record_bytes = blob_bytes(root, publication, RECORD_PATH, "V27_RECORD_UNAVAILABLE")
        try:
            observed_record = json.loads(record_bytes.decode("utf-8", errors="strict"))
        except (UnicodeDecodeError, json.JSONDecodeError) as error:
            raise SuccessorError("V27_RECORD_INVALID", str(error), "FAIL") from error
        validate_schema(observed_record, schema_bytes)
        if observed_record != expected or record_bytes != canonical_json(expected):
            raise SuccessorError(
                "V27_RECORD_IDENTITY_MISMATCH",
                "the committed record is not the deterministic authenticated projection",
                "FAIL",
            )
        if not observed_record["assertionLedger"]:
            raise SuccessorError("V27_EMPTY_ASSERTION_LEDGER", "zero evaluated assertions cannot pass", "FAIL")
        record_assertion(ledger, 8, f"recordSha256={sha256_bytes(record_bytes)}")

        if touching_commits(root, publication, evaluated, (RECORD_PATH,)):
            raise SuccessorError("V27_RECORD_HISTORY_TOUCHED", RECORD_PATH, "FAIL")
        if not path_exists(root, evaluated, RECORD_PATH):
            raise SuccessorError("V27_RECORD_REVERTED_OR_DELETED", RECORD_PATH, "FAIL")
        if blob_bytes(root, evaluated, RECORD_PATH, "V27_RECORD_UNAVAILABLE") != record_bytes:
            raise SuccessorError("V27_RECORD_DESCENDANT_DRIFT", RECORD_PATH, "FAIL")
        record_assertion(ledger, 9, f"evaluated={evaluated}")

        if any(
            expected[field] is not False
            for field in ("executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized")
        ) or expected["implementationHold"] != "ACTIVE":
            raise SuccessorError("V27_AUTHORITY_FLAG_DRIFT", "V27 cannot set an authority flag", "FAIL")
        record_assertion(ledger, 10, "hold=ACTIVE; executionAllowed=false")

        route = "C2" if evaluated == publication else "DESCENDANT"
        return self_validated(
            root,
            result_envelope("PASS", route, observation(root, evaluated), ledger, []),
            facts["bootstrap"],
        )
    except SuccessorError as error:
        route = error.route
        try:
            observed = observation(root, evaluated) if evaluated is not None else empty_observation()
        except SuccessorError:
            observed = empty_observation()
            route = "BLOCKED" if route == "C1" else route
        return self_validated(
            root,
            result_envelope(
                error.state,
                route,
                observed,
                ledger,
                [{"code": error.code, "detail": error.detail or error.code}],
            ),
            # Always validate against committed bytes: a dirtied working-tree schema must not be
            # able to turn a governed FAIL into anything else.
            bootstrap if bootstrap is not None else committed_schema_source(root),
        )


def open_record_directory(root: Path) -> int:
    """Open the record's parent directory beneath the root without following any alias."""

    directory = str(PurePosixPath(RECORD_PATH).parent)
    descriptor = os.open(str(root), os.O_RDONLY | os.O_DIRECTORY)
    try:
        for segment in directory.split("/"):
            following = os.open(segment, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=descriptor)
            os.close(descriptor)
            descriptor = following
    except OSError as error:
        os.close(descriptor)
        raise SuccessorError("V27_RECORD_PARENT_ALIAS", f"{directory}: {error}") from error
    return descriptor


def generate_document(root: Path) -> tuple[dict[str, Any], str]:
    """Generate the prospective deterministic record at the exact bootstrap publication."""

    head = resolve_commit(root, "HEAD", "V27_GENERATION_BASELINE_UNAVAILABLE")
    require_complete_history(root)
    bootstrap = discover_bootstrap(root, head)
    if head != bootstrap:
        raise SuccessorError("V27_GENERATION_BASELINE_DRIFT", f"expected={bootstrap}; observed={head}", "FAIL")
    bindings, parent, tree, parent_tree = validate_bootstrap(root, bootstrap)
    submodules = submodule_binding(root, bootstrap, "V27_SUBMODULE_BINDING_DRIFT")
    document = build_document(bindings, bootstrap, tree, parent, parent_tree, submodules)
    validate_schema(document, blob_bytes(root, bootstrap, SCHEMA_PATH, "V27_SCHEMA_UNAVAILABLE"))
    return document, head


def write_document(root: Path) -> dict[str, Any]:
    """Install the record atomically, never following an alias or replacing foreign bytes."""

    document, head = generate_document(root)
    content = canonical_json(document)
    name = PurePosixPath(RECORD_PATH).name
    descriptor = open_record_directory(root)
    temporary = f".{name}.tmp.{uuid.uuid4().hex}"
    installed = False
    try:
        try:
            os.lstat(name, dir_fd=descriptor)
        except FileNotFoundError:
            pass
        else:
            raise SuccessorError("V27_RECORD_ALREADY_EXISTS", RECORD_PATH)
        handle = os.open(
            temporary,
            os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW,
            0o644,
            dir_fd=descriptor,
        )
        with os.fdopen(handle, "wb") as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        if resolve_commit(root, "HEAD", "V27_GENERATION_BASELINE_UNAVAILABLE") != head:
            raise SuccessorError("V27_GENERATION_BASELINE_DRIFT", head, "FAIL")
        try:
            os.link(temporary, name, src_dir_fd=descriptor, dst_dir_fd=descriptor)
        except FileExistsError as error:
            raise SuccessorError("V27_RECORD_CONCURRENT_PUBLICATION", RECORD_PATH) from error
        installed = True
        os.fsync(descriptor)
        readback = os.open(name, os.O_RDONLY | os.O_NOFOLLOW, dir_fd=descriptor)
        with os.fdopen(readback, "rb") as stream:
            if stream.read() != content:
                raise SuccessorError("V27_PUBLICATION_FINAL_IDENTITY_DRIFT", RECORD_PATH, "FAIL")
        return document
    except BaseException:
        # Remove only bytes this call installed, and leave no quarantine dirt in a governed
        # directory. Foreign bytes are never touched.
        if installed:
            try:
                readback = os.open(name, os.O_RDONLY | os.O_NOFOLLOW, dir_fd=descriptor)
                with os.fdopen(readback, "rb") as stream:
                    ours = stream.read() == content
                if ours:
                    os.unlink(name, dir_fd=descriptor)
            except OSError:
                pass
        raise
    finally:
        try:
            os.unlink(temporary, dir_fd=descriptor)
        except OSError:
            pass
        os.close(descriptor)


def fail(error: SuccessorError, root: Path) -> NoReturn:
    """Print one deterministic, self-validated failure envelope with governed semantics."""

    envelope = self_validated(
        root,
        result_envelope(
            error.state,
            error.route,
            empty_observation(),
            [],
            [{"code": error.code, "detail": error.detail or error.code}],
        ),
        committed_schema_source(root),
    )
    print(json.dumps(envelope, indent=2, sort_keys=True))
    raise SystemExit(1 if envelope["result"] == "FAIL" else 2)


def parse_args(arguments: Sequence[str] | None = None) -> argparse.Namespace:
    """Parse the closed V27 command line."""

    parser = argparse.ArgumentParser(description=__doc__)
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--write", action="store_true", help="generate the prospective record at the bootstrap")
    action.add_argument("--verify", metavar="REVISION", help="verify one committed V27 candidate")
    parser.add_argument("--root", type=Path, default=Path.cwd(), help="repository working tree")
    parser.add_argument(
        "--trusted-host",
        metavar="REVISION",
        default="",
        help="externally recorded protected-host provenance required by --verify; never read by --write",
    )
    return parser.parse_args(arguments)


def main(arguments: Sequence[str] | None = None) -> int:
    """Run generation or verification with PASS/FAIL/BLOCKED exit semantics."""

    options = parse_args(arguments)
    # Even a repository-unavailable envelope is validated, so the declared root is the fallback
    # source for the pinned schema when the repository root itself cannot be resolved.
    root = options.root
    try:
        root = resolve_root(options.root)
        if options.write:
            if options.trusted_host:
                raise SuccessorError(
                    "V27_TRUSTED_HOST_NOT_APPLICABLE",
                    "--trusted-host is only read by --verify",
                )
            document = write_document(root)
            print(
                f"V27_STORY_7_1_LIFECYCLE_EVIDENCE_AUTHORITY_WRITTEN path={RECORD_PATH} "
                f"sha256={sha256_bytes(canonical_json(document))}"
            )
            return 0
        if not options.trusted_host:
            raise SuccessorError("V27_TRUSTED_HOST_REQUIRED", "--verify requires --trusted-host")
        document = verify_revision(root, options.verify, options.trusted_host)
    except SuccessorError as error:
        fail(error, root)
    except OSError as error:  # Publication and filesystem faults stay governed JSON, not tracebacks.
        fail(SuccessorError("V27_PUBLICATION_IO_FAILED", str(error)), root)
    print(json.dumps(document, indent=2, sort_keys=True))
    return int(document["exitCode"])


if __name__ == "__main__":
    sys.exit(main())
