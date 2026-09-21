#!/usr/bin/env python3
"""Build and verify the fail-closed V23 Story 7.1 entry-authority transition."""

from __future__ import annotations

import argparse
import configparser
import ctypes
from datetime import datetime, timezone
from decimal import Decimal
import errno
import fcntl
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path, PurePosixPath
import re
import secrets
import stat
import subprocess
import sys
import tempfile
import time
from typing import Any, Callable, NoReturn, Sequence
import unicodedata

from jsonschema import Draft202012Validator
from jsonschema.exceptions import SchemaError, ValidationError


SCHEMA_VERSION = "hexalith.conversations.story-7.1-entry-authority.v1"
RESULT_SCHEMA_VERSION = "hexalith.conversations.current-planning-authority-result.v1"
AUTHORITY_ID = "V23-STORY-7.1-ENTRY-AUTHORITY"
REQUEST_ID = "V23-STORY-7.1-ENTRY-REQUEST-v1"
CORRECTION_SCHEMA_VERSION = "hexalith.conversations.story-7.1-entry-tooling-correction.v1"
CORRECTION_ID = "V24-STORY-7.1-ENTRY-TOOLING-CORRECTION-v1"
SCHEMA_PATH = "_bmad/schemas/v23-story-7.1-entry-authority-v1.schema.json"
CORRECTION_SCHEMA_PATH = "_bmad/schemas/v24-story-7.1-entry-tooling-correction-v1.schema.json"
PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_entry_authority.py"
REQUEST_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-entry-candidate-v1.json"
CORRECTION_PATH = "_bmad-output/planning-artifacts/v24-story-7.1-entry-tooling-correction-v1.json"
AUTHORITY_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-entry-authority-v1.json"
ARCHITECTURE_PATH = "_bmad-output/planning-artifacts/architecture.md"
PUBLISHER_TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py"
RESOLVER_PATH = "_bmad/scripts/resolve_current_planning_authority.py"
RESOLVER_TEST_PATH = "_bmad/scripts/tests/test_resolve_current_planning_authority.py"
VERIFIER_PATH = "_bmad/scripts/verify_evidence_boundary.py"
VERIFIER_TEST_PATH = "_bmad/scripts/tests/test_verify_evidence_boundary.py"
WORKFLOW_PATH = ".github/workflows/planning-authority-preflight.yml"
GITMODULES_PATH = ".gitmodules"
OPERATIONAL_ENVELOPE_PATH = "_bmad-output/planning-artifacts/production-operational-envelope-v1.md"
PRESERVATION_MANIFEST_PATH = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json"
PERFORMANCE_BASELINE_PATH = "docs/release-evidence/sm-c2-hot-path-baseline-v1.json"
LANDING_ZONE_AUTHORITY_PATH = "_bmad-output/planning-artifacts/oq-1-landing-zone-approval-v1.json"
RECOVERY_RUNBOOK_PATH = "docs/runbooks/story-7.1-production-recovery.md"
PRESERVATION_EVIDENCE_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-preservation-gate-result-v1.json"
PERFORMANCE_EVIDENCE_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-performance-gate-result-v1.json"
LANDING_ZONE_EVIDENCE_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-landing-zone-gate-result-v1.json"
GATE_EVIDENCE_PATHS = {
    "preservation": PRESERVATION_EVIDENCE_PATH,
    "performance": PERFORMANCE_EVIDENCE_PATH,
    "landingZone": LANDING_ZONE_EVIDENCE_PATH,
}

PROTECTED_MAIN = "dcba5d4b1314eb67a95fa560b7cc0f88a9ab2607"
HISTORICAL_V22_CANDIDATE = "cf82f8008d02b07d48338a545909d97faa302362"
TOOLING_BASELINE = "e0b098fa1c056385e28ee8ac0efd0c55dfab324f"
V23_REQUEST_PUBLICATION = "5a7234b922371b5d0a12085a444d93783263f278"
V23_PUBLISHER_SHA256 = "9c1ea485a5906d0a69e4c99058494ffab86b2f96ed47a936ab95d0d4d8be7364"
TRUSTED_OWNER_IDENTITY = "Jerome Piquot <jpiquot@itaneo.com>"
TRUSTED_SSH_PRINCIPAL = "jpiquot@itaneo.com"
TRUSTED_SSH_PUBLIC_KEY = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIL0Kt34ByT8WvAx325SbxYRNLKBZ3ggbgWomqD1nCHq4"
TRUSTED_SSH_FINGERPRINT = "SHA256:8XlNQvE3ucPf/e509wU4qtNgiyWA+TKmLei7F7+TCvk"
ARCHITECTURE_VERSION = "conversations-architecture-2026-09-20-v23"
HISTORICAL_V22_RESOLVER_SHA256 = "cad217c642706cbf116f0ad5d05a0b5ef204e196f3d1dd2458668b3cb442f38e"
GIT_EXECUTABLE = "/usr/bin/git"
SSH_KEYGEN_EXECUTABLE = "/usr/bin/ssh-keygen"
TRUSTED_EXECUTABLE_PATH = "/usr/bin:/bin"
V22_ROUTE_SHA256 = "f0e1c4f3497d5195a71f050dd74db7d4fc88ca674c6bfc0777b92d64199ace57"
# Updated only after the protected workflow is final. The successor route accepts these
# exact committed bytes; token presence is deliberately not a trust decision.
V23_WORKFLOW_SHA256 = "328fdd95cb6edd546c735a0da329cc3d1505097b05d3fb2a855692d4b18c3478"
V23_SCHEMA_SHA256 = "d11340d9b2665c5295a4408f9e6b26218001a61f118d6ec979d6e9ca4da3ea1b"
V24_SCHEMA_SHA256 = "2cfb5fa98cc523375202deb6e00bd2024a44490a604fc0dbf9785fd13d9b195a"
V22_ACTIVE_COMMAND = (
    "uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py "
    "--repository . --candidate HEAD --check"
)

TOOLING_PATHS = tuple(
    sorted(
        (
            SCHEMA_PATH,
            PUBLISHER_PATH,
            REQUEST_PATH,
            PUBLISHER_TEST_PATH,
            RESOLVER_PATH,
            RESOLVER_TEST_PATH,
            VERIFIER_PATH,
            VERIFIER_TEST_PATH,
            WORKFLOW_PATH,
        )
    )
)
CORRECTION_MANIFEST_PATHS = tuple(
    sorted(
        (
            CORRECTION_SCHEMA_PATH,
            PUBLISHER_PATH,
            PUBLISHER_TEST_PATH,
            RESOLVER_PATH,
            RESOLVER_TEST_PATH,
            VERIFIER_PATH,
            VERIFIER_TEST_PATH,
        )
    )
)
CORRECTION_PATHS = tuple(sorted((*CORRECTION_MANIFEST_PATHS, CORRECTION_PATH)))
AUTHORITY_PATHS = (ARCHITECTURE_PATH, AUTHORITY_PATH)
SOURCE_PATHS = (
    ARCHITECTURE_PATH,
    "_bmad-output/planning-artifacts/v22-current-authority-recovery-v1.json",
    "_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json",
    "_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md",
    "_bmad-output/planning-artifacts/implementation-hold-v1.json",
    "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md",
    "_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json",
    "_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json",
    "_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
    LANDING_ZONE_AUTHORITY_PATH,
    PRESERVATION_MANIFEST_PATH,
    PERFORMANCE_BASELINE_PATH,
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
)
SPRINT_ROWS = (
    "epic-7",
    "7-1-define-the-final-record-schema-and-deterministic-generator-core",
    "7-2-derive-test-path-candidate-submodule-and-gitlink-facts",
    "7-3-integrate-generation-into-every-blocking-completion-transition",
    "7-4-verify-historical-mode-and-required-fault-injection-blockers",
)
STORY_6_2_ROW = "6-2-migrate-conversations-to-platform-owned-hosting"
_COMMIT = re.compile(r"^[0-9a-f]{40}$")
PRESERVATION_CATEGORIES = (
    "tenant-isolation",
    "idempotency",
    "contract-validation",
    "redaction-replay",
    "provider-portability",
    "projection-freshness",
    "governance-audit-pairing",
)
PERFORMANCE_HOT_PATHS = ("HP-APPEND", "HP-CREATE", "HP-LIST", "HP-OPEN")
PERFORMANCE_MIN_SAMPLE_COUNT = 30
PERFORMANCE_MAX_REGRESSION_PERCENT = 5.0
LANDING_ZONE_DECISION_IDS = ("FR-10", "FR-11", "FR-12", "FR-13", "FR-14", "FR-15")
LANDING_ZONE_APPROVING_AUTHORITY = "OQ1-OWNER-AUTHORITY-001"
LANDING_ZONE_APPROVAL_PATH = "_bmad-output/planning-artifacts/oq-1-owner-authority-v1.json"
LANDING_ZONE_GRANT_PATHS = {
    "OQ1-EVENTSTORE-GRANT-001": "_bmad-output/planning-artifacts/oq-1-eventstore-change-grant-v1.json",
    "OQ1-COMMONS-GRANT-001": "_bmad-output/planning-artifacts/oq-1-commons-change-grant-v1.json",
    "OQ1-CONVERSATIONS-CONSUMER-GRANT-001": (
        "_bmad-output/planning-artifacts/oq-1-conversations-consumer-grant-v1.json"
    ),
}
LANDING_ZONE_GRANT_REPOSITORIES = {
    "OQ1-EVENTSTORE-GRANT-001": {
        "name": "Hexalith.EventStore",
        "url": "https://github.com/Hexalith/Hexalith.EventStore.git",
        "gitlinkPath": "references/Hexalith.EventStore",
    },
    "OQ1-COMMONS-GRANT-001": {
        "name": "Hexalith.Commons",
        "url": "https://github.com/Hexalith/Hexalith.Commons.git",
        "gitlinkPath": "references/Hexalith.Commons",
    },
    "OQ1-CONVERSATIONS-CONSUMER-GRANT-001": {
        "name": "Hexalith.Conversations",
        "url": "https://github.com/Hexalith/Hexalith.Conversations.git",
    },
}
LANDING_ZONE_GRANT_REQUIREMENTS = {
    "OQ1-EVENTSTORE-GRANT-001": ("FR-10", "FR-13", "FR-15"),
    "OQ1-COMMONS-GRANT-001": ("FR-11", "FR-12", "FR-13", "FR-14", "FR-15"),
    "OQ1-CONVERSATIONS-CONSUMER-GRANT-001": ("FR-10", "FR-11", "FR-12", "FR-13", "FR-14", "FR-15"),
}
LANDING_ZONE_SUCCESSOR_GRANT_PATHS = {
    grant_id: path.replace("-v1.json", "-effective-successor-v1.json")
    for grant_id, path in LANDING_ZONE_GRANT_PATHS.items()
}
LANDING_ZONE_SUCCESSOR_PATHS = {
    decision_id: f"_bmad-output/planning-artifacts/oq-1-{decision_id.casefold()}-approved-successor-v1.json"
    for decision_id in LANDING_ZONE_DECISION_IDS
}
RECOVERY_PROCEDURE_IDS = ("DETECT", "CONTAIN", "RESTORE", "VERIFY", "ESCALATE")
RECOVERY_PROCEDURES = {
    "DETECT": (
        "Observe health and telemetry for Story 7.1 services.",
        "Record the affected scope and evidence identifiers.",
    ),
    "CONTAIN": (
        "Activate the Story 7.1 implementation hold with execution false.",
        "Quiesce affected writes while preserving unrelated tenant traffic.",
    ),
    "RESTORE": (
        "Select a verified checkpoint or immutable backup.",
        "Restore state and rebuild projections from the selected recovery source.",
    ),
    "VERIFY": (
        "Verify event stream integrity and projection consistency.",
        "Verify tenant isolation before reopening affected writes.",
    ),
    "ESCALATE": (
        "Notify the AD-5 operational owner and record the recovery decision and evidence.",
        "Retain the Story 7.1 hold pending explicit owner approval.",
    ),
}
PUBLICATION_LOCK_TIMEOUT_SECONDS = 2.0


class EntryAuthorityError(RuntimeError):
    """Represent a stable V23 FAIL or BLOCKED result."""

    def __init__(self, code: str, detail: str, state: str = "FAIL") -> None:
        if state not in ("FAIL", "BLOCKED"):
            raise ValueError(f"invalid failure state {state!r}")
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail
        self.state = state


def sha256(content: bytes) -> str:
    """Return the lowercase SHA-256 digest for exact bytes."""

    return hashlib.sha256(content).hexdigest()


def json_bytes(value: Any) -> bytes:
    """Render deterministic UTF-8/LF JSON bytes."""

    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def canonical_digest(value: Any) -> str:
    """Hash one canonical compact JSON value."""

    content = json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    return sha256(content + b"\n")


RECOVERY_SUMMARIES = {
    "backupProcedure": f"RESTORE.action::{RECOVERY_PROCEDURES['RESTORE'][0]}",
    "rebuildProcedure": f"RESTORE.outcome::{RECOVERY_PROCEDURES['RESTORE'][1]}",
    "disasterRecoveryProcedure": (
        "RECOVERY_PROCEDURES.sha256::"
        + canonical_digest(
            [
                {
                    "id": procedure_id,
                    "action": RECOVERY_PROCEDURES[procedure_id][0],
                    "outcome": RECOVERY_PROCEDURES[procedure_id][1],
                }
                for procedure_id in RECOVERY_PROCEDURE_IDS
            ]
        )
    ),
}


def nfc_list_digest(values: Sequence[str]) -> str:
    """Hash one exact NFC UTF-8 inventory with one LF-terminated identity per row."""

    normalized = [unicodedata.normalize("NFC", value) for value in values]
    return sha256(("\n".join(normalized) + "\n").encode("utf-8"))


def safe_path(value: str) -> str:
    """Require one normalized repository-relative POSIX path."""

    path = PurePosixPath(value)
    if (
        not value
        or value in (".", "..")
        or "\\" in value
        or any(ord(character) < 32 or ord(character) == 127 for character in value)
        or path.is_absolute()
        or path.as_posix() != value
        or any(part in ("", ".", "..") for part in path.parts)
    ):
        raise EntryAuthorityError("V23_PATH_ESCAPE", repr(value), "BLOCKED")
    return value


def git_environment() -> dict[str, str]:
    """Return an allowlisted Git environment without ambient redirects or configuration."""

    return {
        "GIT_CONFIG_GLOBAL": os.devnull,
        "GIT_CONFIG_NOSYSTEM": "1",
        "GIT_CONFIG_SYSTEM": os.devnull,
        "GIT_NO_REPLACE_OBJECTS": "1",
        "GIT_TERMINAL_PROMPT": "0",
        "LANG": "C.UTF-8",
        "LC_ALL": "C.UTF-8",
        "PATH": TRUSTED_EXECUTABLE_PATH,
        "TMPDIR": "/var/tmp",
    }


def run_git(
    root: Path,
    *arguments: str,
    allowed: tuple[int, ...] = (0,),
) -> subprocess.CompletedProcess[bytes]:
    """Run one bounded Git command and preserve unavailable evidence as BLOCKED."""

    try:
        result = subprocess.run(
            (GIT_EXECUTABLE, "--no-replace-objects", "-C", str(root), *arguments),
            check=False,
            capture_output=True,
            timeout=30,
            env=git_environment(),
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise EntryAuthorityError("V23_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise EntryAuthorityError("V23_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def repository_root(repository: Path) -> Path:
    """Resolve and require the explicit repository root."""

    root = repository.resolve(strict=True)
    observed = Path(run_git(root, "rev-parse", "--show-toplevel").stdout.decode().strip()).resolve()
    if root != observed:
        raise EntryAuthorityError("V23_REPOSITORY_ROOT_MISMATCH", f"expected={root}; observed={observed}", "BLOCKED")
    shallow = run_git(root, "rev-parse", "--is-shallow-repository").stdout.strip()
    if shallow != b"false":
        raise EntryAuthorityError("V23_HISTORY_INCOMPLETE", repr(shallow), "BLOCKED")
    return root


def resolve_commit(root: Path, revision: str, code: str = "V23_COMMIT_UNAVAILABLE") -> str:
    """Resolve one revision to a full commit identity."""

    try:
        commit = run_git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode("ascii").strip()
    except (EntryAuthorityError, UnicodeError) as error:
        detail = error.detail if isinstance(error, EntryAuthorityError) else str(error)
        raise EntryAuthorityError(code, detail, "BLOCKED") from error
    if _COMMIT.fullmatch(commit) is None:
        raise EntryAuthorityError(code, repr(commit), "BLOCKED")
    return commit


def commit_tree(root: Path, commit: str) -> str:
    """Resolve one commit tree."""

    tree = run_git(root, "rev-parse", "--verify", f"{commit}^{{tree}}").stdout.decode("ascii").strip()
    if _COMMIT.fullmatch(tree) is None:
        raise EntryAuthorityError("V23_TREE_INVALID", repr(tree), "BLOCKED")
    return tree


def commit_parents(root: Path, commit: str) -> tuple[str, ...]:
    """Read all parents from the committed object graph."""

    try:
        row = run_git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").split()
    except UnicodeError as error:
        raise EntryAuthorityError("V23_GRAPH_INVALID", str(error), "BLOCKED") from error
    if not row or row[0] != commit or any(_COMMIT.fullmatch(value) is None for value in row):
        raise EntryAuthorityError("V23_GRAPH_INVALID", repr(row), "BLOCKED")
    return tuple(row[1:])


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require committed ancestry without using the worktree."""

    result = run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1))
    if result.returncode != 0:
        raise EntryAuthorityError(code, f"{ancestor} is not an ancestor of {descendant}", "BLOCKED")


def candidate_blob(root: Path, commit: str, relative_path: str, code: str = "V23_BLOB_UNAVAILABLE") -> bytes:
    """Read one exact committed blob."""

    result = run_git(root, "cat-file", "blob", f"{commit}:{safe_path(relative_path)}", allowed=(0, 128))
    if result.returncode != 0:
        raise EntryAuthorityError(code, f"{relative_path}@{commit}", "BLOCKED")
    return result.stdout


def tree_record(root: Path, commit: str, relative_path: str) -> tuple[str, str, str]:
    """Return one exact mode, type, and object identity from a tree."""

    content = run_git(root, "ls-tree", "-z", commit, "--", safe_path(relative_path)).stdout
    records = [row for row in content.split(b"\0") if row]
    if len(records) != 1:
        raise EntryAuthorityError("V23_TREE_ENTRY_UNAVAILABLE", f"{relative_path}@{commit}", "BLOCKED")
    try:
        header, observed = records[0].split(b"\t", 1)
        mode, kind, object_id = header.decode("ascii").split(" ")
        observed_path = observed.decode("utf-8", errors="strict")
    except (UnicodeError, ValueError) as error:
        raise EntryAuthorityError("V23_TREE_ENTRY_INVALID", relative_path, "BLOCKED") from error
    if observed_path != relative_path or _COMMIT.fullmatch(object_id) is None:
        raise EntryAuthorityError("V23_TREE_ENTRY_INVALID", relative_path, "BLOCKED")
    return mode, kind, object_id


def changed_paths(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return the exact ordinal changed path set without rename inference."""

    content = run_git(
        root,
        "diff-tree",
        "--no-commit-id",
        "--name-only",
        "--no-renames",
        "-r",
        "-z",
        baseline,
        candidate,
        "--",
    ).stdout
    if not content:
        return ()
    if not content.endswith(b"\0"):
        raise EntryAuthorityError("V23_DIFF_INVALID", "missing NUL terminator", "BLOCKED")
    try:
        return tuple(safe_path(row.decode("utf-8", errors="strict")) for row in content[:-1].split(b"\0"))
    except UnicodeError as error:
        raise EntryAuthorityError("V23_DIFF_INVALID", str(error), "BLOCKED") from error


def changed_gitlinks(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Derive changed gitlinks only from raw Git modes."""

    content = run_git(root, "diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--").stdout
    records = [row for row in content.split(b"\0") if row]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise EntryAuthorityError("V23_GITLINK_DIFF_INVALID", "incomplete raw record", "BLOCKED")
        fields = records[index].decode("ascii", errors="strict").split()
        path = safe_path(records[index + 1].decode("utf-8", errors="strict"))
        if len(fields) >= 5 and (fields[0] == ":160000" or fields[1] == "160000"):
            paths.append(path)
    return tuple(sorted(paths))


def gitmodule_paths(content: bytes) -> tuple[str, ...]:
    """Parse the exact root .gitmodules path inventory."""

    parser = configparser.ConfigParser(interpolation=None, strict=True)
    parser.optionxform = str
    try:
        parser.read_string(content.decode("utf-8", errors="strict"))
    except (UnicodeError, configparser.Error) as error:
        raise EntryAuthorityError("V23_GITMODULES_INVALID", str(error), "BLOCKED") from error
    paths: list[str] = []
    for section in parser.sections():
        if not section.startswith('submodule "') or not section.endswith('"') or not parser.has_option(section, "path"):
            raise EntryAuthorityError("V23_GITMODULES_INVALID", section, "BLOCKED")
        paths.append(safe_path(parser.get(section, "path")))
    if len(paths) != len(set(paths)):
        raise EntryAuthorityError("V23_GITMODULES_INVALID", "duplicate path", "BLOCKED")
    return tuple(sorted(paths))


def root_gitlinks(root: Path, commit: str) -> list[dict[str, str]]:
    """Derive the ordinal root gitlink inventory from raw mode-160000 rows."""

    content = run_git(root, "ls-tree", "-r", "-z", commit).stdout
    rows: list[dict[str, str]] = []
    for record in (row for row in content.split(b"\0") if row):
        header, raw_path = record.split(b"\t", 1)
        mode, kind, object_id = header.decode("ascii", errors="strict").split(" ")
        relative_path = safe_path(raw_path.decode("utf-8", errors="strict"))
        if mode == "160000":
            if kind != "commit" or _COMMIT.fullmatch(object_id) is None:
                raise EntryAuthorityError("V23_GITLINK_INVALID", relative_path, "BLOCKED")
            rows.append({"path": relative_path, "mode": mode, "objectId": object_id})
    rows.sort(key=lambda row: row["path"])
    modules = gitmodule_paths(candidate_blob(root, commit, GITMODULES_PATH))
    if tuple(row["path"] for row in rows) != modules or len(rows) != 10:
        raise EntryAuthorityError("V23_GITLINK_INVENTORY_DRIFT", f"gitlinks={rows!r}; modules={modules!r}")
    return rows


def reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    """Reject duplicate JSON properties."""

    value: dict[str, Any] = {}
    for key, item in pairs:
        if key in value:
            raise ValueError(f"duplicate JSON property {key!r}")
        value[key] = item
    return value


def load_json(content: bytes, code: str) -> dict[str, Any]:
    """Load one duplicate-safe UTF-8 JSON object."""

    try:
        value = json.loads(content.decode("utf-8", errors="strict"), object_pairs_hook=reject_duplicate_keys)
    except (UnicodeError, json.JSONDecodeError, ValueError) as error:
        raise EntryAuthorityError(code, str(error), "BLOCKED") from error
    if not isinstance(value, dict):
        raise EntryAuthorityError(code, "top-level JSON value is not an object", "BLOCKED")
    return value


def validate_request_controls(document: dict[str, Any]) -> None:
    """Independently close every request field that can grant or imply authority."""

    expected_keys = {
        "schemaVersion",
        "recordType",
        "authorityId",
        "requestId",
        "protectedMain",
        "historicalV22",
        "toolingTransaction",
        "sourceBindings",
        "rootGitlinks",
        "entryGates",
        "publicationContract",
        "resultSemantics",
        "assertionLedger",
        "blockers",
        "result",
        "implementationHold",
        "ownerApprovalClaimed",
        "releaseAuthorized",
        "pushAuthorized",
        "executionAllowed",
    }
    protected = document.get("protectedMain")
    historical = document.get("historicalV22")
    publication = document.get("publicationContract")
    if (
        set(document) != expected_keys
        or document.get("schemaVersion") != SCHEMA_VERSION
        or document.get("recordType") != "REQUEST"
        or document.get("authorityId") != AUTHORITY_ID
        or document.get("requestId") != REQUEST_ID
        or not isinstance(protected, dict)
        or set(protected) != {"commit", "tree", "parents", "diagnosticResult", "diagnosticBlocker"}
        or protected.get("diagnosticResult") != "FAIL"
        or protected.get("diagnosticBlocker") != "CANDIDATE_GRAPH_DRIFT"
        or not isinstance(historical, dict)
        or set(historical) != {"candidateCommit", "candidateTree", "result", "executionAllowed"}
        or historical.get("result") != "PASS"
        or historical.get("executionAllowed") is not False
        or not isinstance(publication, dict)
        or publication
        != {
            "authorityPath": AUTHORITY_PATH,
            "architecturePath": ARCHITECTURE_PATH,
            "exactChangedPaths": list(AUTHORITY_PATHS),
            "requiredCommitSignature": True,
            "requiredDecision": "EXECUTION_ALLOWED",
            "story7_2Locked": True,
        }
        or document.get("resultSemantics") != result_semantics()
        or document.get("result") != "BLOCKED"
        or document.get("implementationHold") != "ACTIVE"
        or document.get("ownerApprovalClaimed") is not False
        or document.get("releaseAuthorized") is not False
        or document.get("pushAuthorized") is not False
        or document.get("executionAllowed") is not False
    ):
        raise EntryAuthorityError(
            "V23_REQUEST_CONTROL_DRIFT",
            "request authority, decision, hold, release, push, execution, or story controls drifted",
            "BLOCKED",
        )


def validate_authority_controls(document: dict[str, Any]) -> None:
    """Independently close every signed authority field that can enable execution."""

    expected_keys = {
        "schemaVersion",
        "recordType",
        "authorityId",
        "request",
        "publication",
        "ownerDecision",
        "gateDispositions",
        "sourceBindings",
        "rootGitlinks",
        "architecture",
        "resultSemantics",
        "assertionLedger",
        "blockers",
        "result",
        "implementationHold",
        "ownerApprovalClaimed",
        "releaseAuthorized",
        "pushAuthorized",
        "executionAllowed",
        "storyExecution",
    }
    request = document.get("request")
    publication = document.get("publication")
    owner = document.get("ownerDecision")
    architecture = document.get("architecture")
    if (
        set(document) != expected_keys
        or document.get("schemaVersion") != SCHEMA_VERSION
        or document.get("recordType") != "AUTHORITY"
        or document.get("authorityId") != AUTHORITY_ID
        or not isinstance(request, dict)
        or set(request) != {"path", "publicationCommit", "sha256"}
        or request.get("path") != REQUEST_PATH
        or not isinstance(publication, dict)
        or set(publication)
        != {"sourceCommit", "sourceTree", "exactChangedPaths", "requiredMode", "changedGitlinks"}
        or publication.get("exactChangedPaths") != list(AUTHORITY_PATHS)
        or publication.get("requiredMode") != "100644"
        or publication.get("changedGitlinks") != []
        or not isinstance(owner, dict)
        or set(owner) != {"identity", "decidedAtUtc", "rationale", "decision"}
        or owner.get("decision") != "EXECUTION_ALLOWED"
        or not isinstance(architecture, dict)
        or set(architecture) != {"path", "prefixBytes", "prefixSha256", "version", "requestSha256"}
        or architecture.get("path") != ARCHITECTURE_PATH
        or architecture.get("version") != ARCHITECTURE_VERSION
        or document.get("resultSemantics") != result_semantics()
        or document.get("blockers") != []
        or document.get("result") != "PASS"
        or document.get("implementationHold") != "EXECUTION_ALLOWED"
        or document.get("ownerApprovalClaimed") is not True
        or document.get("releaseAuthorized") is not False
        or document.get("pushAuthorized") is not False
        or document.get("executionAllowed") is not True
        or document.get("storyExecution") != {"7.1": True, "7.2": False, "7.3": False, "7.4": False}
    ):
        raise EntryAuthorityError(
            "V23_AUTHORITY_CONTROL_DRIFT",
            "authority decision, hold, owner, release, push, execution, or story controls drifted",
            "BLOCKED",
        )


def validate_schema(root: Path, document: dict[str, Any], *, schema_commit: str | None = None) -> None:
    """Validate a V23 request, authority, or result against trusted schema bytes."""

    content = (
        candidate_blob(root, schema_commit, SCHEMA_PATH, "V23_SCHEMA_UNAVAILABLE")
        if schema_commit is not None
        else (root / SCHEMA_PATH).read_bytes()
    )
    observed_digest = sha256(content)
    if observed_digest != V23_SCHEMA_SHA256:
        raise EntryAuthorityError(
            "V23_SCHEMA_IDENTITY_MISMATCH",
            f"expected={V23_SCHEMA_SHA256}; observed={observed_digest}",
            "BLOCKED",
        )
    schema = load_json(content, "V23_SCHEMA_INVALID")
    try:
        Draft202012Validator.check_schema(schema)
        Draft202012Validator(schema, format_checker=Draft202012Validator.FORMAT_CHECKER).validate(document)
    except (SchemaError, ValidationError) as error:
        raise EntryAuthorityError("V23_DOCUMENT_SCHEMA_INVALID", error.message, "BLOCKED") from error


def correction_ledger() -> list[dict[str, str]]:
    """Return the fixed nonvacuous V24 correction assertion inventory."""

    return [
        {
            "id": "V24.CORRECTION.01",
            "subject": "immutable-v23-request-publication",
            "state": "PASS",
            "detail": "the original V23 request publication and publisher identity remain exact",
        },
        {
            "id": "V24.CORRECTION.02",
            "subject": "direct-parent-topology",
            "state": "PASS",
            "detail": "the correction publication has the immutable V23 request as its only parent",
        },
        {
            "id": "V24.CORRECTION.03",
            "subject": "exact-eight-path-scope",
            "state": "PASS",
            "detail": "the correction changes exactly the eight declared tooling paths",
        },
        {
            "id": "V24.CORRECTION.04",
            "subject": "mode-100644-tooling",
            "state": "PASS",
            "detail": "every correction path is one mode-100644 regular blob",
        },
        {
            "id": "V24.CORRECTION.05",
            "subject": "self-excluding-tooling-manifest",
            "state": "PASS",
            "detail": "the seven non-record blobs equal the declared self-excluding manifest",
        },
        {
            "id": "V24.CORRECTION.06",
            "subject": "raw-root-gitlink-equality",
            "state": "PASS",
            "detail": "the raw root gitlink inventory is unchanged from V23",
        },
        {
            "id": "V24.CORRECTION.07",
            "subject": "active-non-executable-hold",
            "state": "PASS",
            "detail": "the correction grants no approval, execution, release, or push authority",
        },
    ]


def validate_correction_controls(document: dict[str, Any]) -> None:
    """Close every V24 field independently of the public correction schema."""

    expected_keys = {
        "schemaVersion",
        "recordType",
        "correctionId",
        "predecessor",
        "toolingTransaction",
        "rootGitlinks",
        "resultSemantics",
        "assertionLedger",
        "blockers",
        "result",
        "implementationHold",
        "ownerApprovalClaimed",
        "releaseAuthorized",
        "pushAuthorized",
        "executionAllowed",
        "storyExecution",
    }
    predecessor = document.get("predecessor")
    transaction = document.get("toolingTransaction")
    if (
        set(document) != expected_keys
        or document.get("schemaVersion") != CORRECTION_SCHEMA_VERSION
        or document.get("recordType") != "TOOLING_CORRECTION"
        or document.get("correctionId") != CORRECTION_ID
        or not isinstance(predecessor, dict)
        or set(predecessor)
        != {"requestPath", "publicationCommit", "publicationTree", "requestSha256", "publisherSha256"}
        or predecessor.get("requestPath") != REQUEST_PATH
        or predecessor.get("publicationCommit") != V23_REQUEST_PUBLICATION
        or predecessor.get("publisherSha256") != V23_PUBLISHER_SHA256
        or not isinstance(transaction, dict)
        or set(transaction)
        != {
            "baselineCommit",
            "baselineTree",
            "exactChangedPaths",
            "requiredMode",
            "selfExcludedManifestSha256",
            "manifest",
        }
        or transaction.get("baselineCommit") != V23_REQUEST_PUBLICATION
        or tuple(transaction.get("exactChangedPaths", ())) != CORRECTION_PATHS
        or transaction.get("requiredMode") != "100644"
        or document.get("resultSemantics") != result_semantics()
        or document.get("assertionLedger") != correction_ledger()
        or document.get("blockers") != []
        or document.get("result") != "PASS"
        or document.get("implementationHold") != "ACTIVE"
        or document.get("ownerApprovalClaimed") is not False
        or document.get("releaseAuthorized") is not False
        or document.get("pushAuthorized") is not False
        or document.get("executionAllowed") is not False
        or document.get("storyExecution")
        != {"7.1": False, "7.2": False, "7.3": False, "7.4": False}
    ):
        raise EntryAuthorityError(
            "V24_CORRECTION_CONTROL_DRIFT",
            "correction identity, scope, hold, approval, release, push, or execution controls drifted",
            "BLOCKED",
        )


def validate_correction_schema(
    root: Path,
    document: dict[str, Any],
    *,
    schema_commit: str | None = None,
) -> None:
    """Validate a correction against independently pinned, safely read schema bytes."""

    try:
        content = (
            candidate_blob(root, schema_commit, CORRECTION_SCHEMA_PATH, "V24_SCHEMA_UNAVAILABLE")
            if schema_commit is not None
            else read_regular_worktree_file(root, CORRECTION_SCHEMA_PATH)[0]
        )
    except EntryAuthorityError as error:
        raise EntryAuthorityError("V24_SCHEMA_UNAVAILABLE", error.detail, "BLOCKED") from error
    observed_digest = sha256(content)
    if observed_digest != V24_SCHEMA_SHA256:
        raise EntryAuthorityError(
            "V24_SCHEMA_IDENTITY_MISMATCH",
            f"expected={V24_SCHEMA_SHA256}; observed={observed_digest}",
            "BLOCKED",
        )
    schema = load_json(content, "V24_SCHEMA_INVALID")
    try:
        Draft202012Validator.check_schema(schema)
        Draft202012Validator(schema, format_checker=Draft202012Validator.FORMAT_CHECKER).validate(document)
    except (SchemaError, ValidationError) as error:
        raise EntryAuthorityError("V24_DOCUMENT_SCHEMA_INVALID", error.message, "BLOCKED") from error


def binding(root: Path, commit: str, path: str) -> dict[str, str]:
    """Bind one regular committed blob by raw mode, object identity, and digest."""

    mode, kind, object_id = tree_record(root, commit, path)
    if (mode, kind) != ("100644", "blob"):
        raise EntryAuthorityError("V23_BINDING_MODE_DRIFT", f"{path}: {mode} {kind}")
    return {
        "path": path,
        "mode": mode,
        "objectId": object_id,
        "sha256": sha256(candidate_blob(root, commit, path)),
    }


def worktree_binding(root: Path, path: str) -> dict[str, str]:
    """Bind a prospective regular blob without inserting it into Git."""

    parent_descriptor = -1
    descriptor = -1
    try:
        parent_descriptor, name = open_parent_directory(root, path)
        descriptor = os.open(
            name,
            os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0) | getattr(os, "O_NONBLOCK", 0),
            dir_fd=parent_descriptor,
        )
        initial = os.fstat(descriptor)
        named = os.stat(name, dir_fd=parent_descriptor, follow_symlinks=False)
        if (
            not stat.S_ISREG(initial.st_mode)
            or not stat.S_ISREG(named.st_mode)
            or stat.S_IMODE(initial.st_mode) != 0o644
            or stat.S_IMODE(named.st_mode) != 0o644
            or initial.st_nlink != 1
            or named.st_nlink != 1
            or (initial.st_dev, initial.st_ino) != (named.st_dev, named.st_ino)
        ):
            raise OSError("expected a mode-100644 single-link regular file")
        with os.fdopen(descriptor, "rb", closefd=False) as stream:
            content = stream.read()
        final = os.fstat(descriptor)
        final_named = os.stat(name, dir_fd=parent_descriptor, follow_symlinks=False)
        if (
            (final.st_dev, final.st_ino) != (initial.st_dev, initial.st_ino)
            or (final_named.st_dev, final_named.st_ino) != (initial.st_dev, initial.st_ino)
            or final.st_size != initial.st_size
            or final.st_mtime_ns != initial.st_mtime_ns
            or final.st_ctime_ns != initial.st_ctime_ns
            or final.st_nlink != 1
            or final_named.st_nlink != 1
        ):
            raise OSError("mode, inode, link count, or bytes changed during read")
    except (EntryAuthorityError, OSError) as error:
        raise EntryAuthorityError("V23_TOOLING_INPUT_UNAVAILABLE", f"{path}: {error}", "BLOCKED") from error
    finally:
        close_errors: list[OSError] = []
        for open_descriptor in (descriptor, parent_descriptor):
            if open_descriptor >= 0:
                try:
                    os.close(open_descriptor)
                except OSError as error:
                    close_errors.append(error)
        if close_errors and sys.exception() is None:
            raise EntryAuthorityError(
                "V23_TOOLING_INPUT_UNAVAILABLE",
                f"{path}: descriptor close failed: {close_errors!r}",
                "BLOCKED",
            )
    try:
        hashed = subprocess.run(
            (GIT_EXECUTABLE, "--no-replace-objects", "-C", str(root), "hash-object", "--stdin"),
            input=content,
            check=True,
            capture_output=True,
            timeout=30,
            env=git_environment(),
        ).stdout.decode("ascii").strip()
    except (OSError, subprocess.SubprocessError, UnicodeError) as error:
        raise EntryAuthorityError("V23_TOOLING_INPUT_UNAVAILABLE", f"{path}: {error}", "BLOCKED") from error
    if _COMMIT.fullmatch(hashed) is None:
        raise EntryAuthorityError("V23_TOOLING_INPUT_UNAVAILABLE", f"{path}: {hashed!r}", "BLOCKED")
    return {"path": path, "mode": "100644", "objectId": hashed, "sha256": sha256(content)}


def result_semantics() -> dict[str, Any]:
    """Return the common closed result contract."""

    return {
        "states": ["PASS", "FAIL", "BLOCKED"],
        "exitCodes": {"PASS": 0, "FAIL": 1, "BLOCKED": 2},
        "ledgerRequired": True,
    }


def gate(gate_id: str, state: str, evidence: str, detail: str) -> dict[str, str]:
    """Build one closed entry-gate row."""

    return {"id": gate_id, "state": state, "evidence": evidence, "detail": detail}


def validate_workflow_route(root: Path, commit: str) -> dict[str, str]:
    """Mechanically validate the committed V22 route inventory and workflow binding."""

    route_path = "_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json"
    workflow_path = ".github/workflows/planning-authority-preflight.yml"
    route_content = candidate_blob(root, commit, route_path, "V23_WORKFLOW_ROUTE_MISSING")
    route = load_json(route_content, "V23_WORKFLOW_ROUTE_INVALID")
    expected_keys = {
        "schemaVersion",
        "inventoryId",
        "activeRoutes",
        "historicalRoutes",
        "forbiddenMechanisms",
    }
    active = route.get("activeRoutes")
    historical = route.get("historicalRoutes")
    if (
        set(route) != expected_keys
        or route.get("schemaVersion") != "hexalith.conversations.v22-workflow-route-inventory.v1"
        or route.get("inventoryId") != "V22-WORKFLOW-ROUTE-INVENTORY"
        or sha256(route_content) != V22_ROUTE_SHA256
        or not isinstance(active, list)
        or len(active) != 1
        or set(active[0]) != {"ordinal", "routeId", "status", "workflowPath", "resolverPath", "command"}
        or active[0]
        != {
            "ordinal": 1,
            "routeId": "current-planning-authority",
            "status": "ACTIVE",
            "workflowPath": workflow_path,
            "resolverPath": RESOLVER_PATH,
            "command": V22_ACTIVE_COMMAND,
        }
        or not isinstance(historical, list)
        or len(historical) != 1
        or historical[0].get("status") != "HISTORICAL_ONLY"
        or historical[0].get("invokedAsCurrentAuthority") is not False
    ):
        raise EntryAuthorityError("V23_WORKFLOW_ROUTE_INVALID", "closed route inventory or digest mismatch")
    workflow = candidate_blob(root, commit, workflow_path, "V23_WORKFLOW_ROUTE_MISSING").decode(
        "utf-8", errors="strict"
    )
    workflow_digest = sha256(workflow.encode("utf-8"))
    forbidden = ("RULESET_TOKEN", "/rulesets?", " ci-trust ", " ci-trust\\")
    historical_route = commit == PROTECTED_MAIN and workflow.count(V22_ACTIVE_COMMAND) == 1
    protected_route_tokens = (
        "\n  pull_request_target:\n    branches: [main]\n",
        "\n  pull_request:\n",
        "\n  protected-planning-authority:\n    name: protected-planning-authority-${{ github.event_name }}\n"
        "    if: github.event_name != 'pull_request'\n",
        "uses: actions/setup-python@a26af69be951a213d495a4c3e4e4022e16d87065",
        'python-version: "3.11.15"',
        "update-environment: false",
        "${{ steps.protected-python.outputs.python-path }}",
        "allow-unsafe-pr-checkout: true",
        "UV_PYTHON_DOWNLOADS=never",
        'trusted_git show "$TRUSTED_HOST_COMMIT:$path"',
        "          TRUSTED_EVENT_BASE: ${{ github.event.pull_request.base.sha || github.event.before }}",
        '          trusted_host_commit="$baseline"',
        '          test "$trusted_host_commit" = "$baseline"',
        "            pyproject.toml \\",
        "            uv.lock",
        "uses: astral-sh/setup-uv@c771a70e6277c0a99b617c7a806ffedaca235ff9",
        'checksum: "745765a3b6e360ad76743599ae5c42e9278c7edf8bbff9fc76d05bf2623a04dd"',
        "PROTECTED_UV: ${{ steps.protected-uv.outputs.uv-path }}",
        '"$PROTECTED_UV" sync',
        '--python "$PROTECTED_BASE_PYTHON"',
        '$RUNNER_TEMP/planning-authority-protected-host/.venv/bin/python',
        "- name: Verify synchronized protected Python identity",
        "import jsonschema",
        "sys._base_executable",
        "env -i PATH=/usr/bin:/bin",
        '/usr/bin/git -C "$GITHUB_WORKSPACE"',
        "-c core.hooksPath=/dev/null -c protocol.file.allow=never",
        'working-directory: ${{ runner.temp }}',
        'PYTHONSAFEPATH: "1"',
        '"$protected_python" -I -P',
        '"$RUNNER_TEMP/planning-authority-protected-host/.venv/bin/python" -I -P',
        "$RUNNER_TEMP/planning-authority-protected-host/resolve_current_planning_authority.py",
        "$RUNNER_TEMP/planning-authority-protected-host/verify_evidence_boundary.py",
        '--candidate "${{ steps.range.outputs.candidate }}"',
        "- name: Verify checked-out candidate identity",
        'test "$observed" = "$TRUSTED_EVENT_HEAD"',
        "    name: candidate-validation-${{ github.event_name }}\n",
        "\n  candidate-validation:\n",
        "    needs: protected-planning-authority\n",
        "      always() &&\n",
        "      (github.event_name == 'pull_request' ||\n",
        "      needs.protected-planning-authority.result == 'success'))\n",
    )
    protected_sync = workflow.find("- name: Synchronize protected planning verifier environment")
    protected_python = workflow.find("- name: Set up exact protected Python")
    protected_range = workflow.find("- name: Resolve immutable comparison range")
    protected_materialize = workflow.find("- name: Materialize protected planning-authority trust hosts")
    protected_identity = workflow.find("- name: Verify synchronized protected Python identity")
    resolver_step = workflow.find("- name: Resolve current planning authority through the protected host")
    evidence_step = workflow.find("- name: Verify lifecycle evidence and candidate-bound publication scope")
    candidate_job = workflow.find("\n  candidate-validation:\n")
    candidate_checkout = workflow.find("- name: Check out exact candidate with pinned root submodules")
    candidate_identity = workflow.find("- name: Verify checked-out candidate identity")
    candidate_dotnet = workflow.find("- name: Set up repository .NET SDK")
    candidate_sync = workflow.find("- name: Synchronize frozen candidate environment")
    protected_prefix = workflow[:candidate_job] if candidate_job >= 0 else workflow
    candidate_only_tokens = (
        "submodules: true",
        "global-json-file: global.json",
        "node-version-file: package.json",
        "npm ci --ignore-scripts",
        "_bmad/scripts/check_lifecycle_gate_preflight.py",
    )
    protected_precedes_candidate = (
        -1
        < protected_python
        < protected_range
        < protected_materialize
        < protected_sync
        < protected_identity
        < resolver_step
        < candidate_job
        and -1 < protected_sync < evidence_step < candidate_job
        and candidate_job < candidate_checkout < candidate_identity < candidate_dotnet < candidate_sync
        and all(workflow.find(token) > candidate_job for token in candidate_only_tokens)
        and "python3 -m pip" not in protected_prefix
        and "/usr/bin/python3" not in protected_prefix
        and " --no-sync python3" not in protected_prefix
        and protected_prefix.count("working-directory: ${{ runner.temp }}") == 7
        and protected_prefix.count("UV_PYTHON_DOWNLOADS=never") == 1
        and protected_prefix.count("env -i PATH=/usr/bin:/bin") == 7
    )
    successor_route = (
        commit != PROTECTED_MAIN
        and workflow_digest == V23_WORKFLOW_SHA256
        and workflow.count(protected_route_tokens[0]) == 1
        and workflow.count(protected_route_tokens[1]) == 1
        and all(token in workflow for token in protected_route_tokens[2:])
        and protected_precedes_candidate
    )
    candidate_host_invocation = re.search(
        r"python3\s+_bmad/scripts/(?:resolve_current_planning_authority|verify_evidence_boundary)\.py",
        workflow,
    )
    if (
        not (historical_route or successor_route)
        or (successor_route and candidate_host_invocation is not None)
        or any(token in workflow for token in forbidden)
    ):
        raise EntryAuthorityError(
            "V23_WORKFLOW_ROUTE_INVALID",
            f"active workflow does not match its closed identity; sha256={workflow_digest}",
        )
    return binding(root, commit, route_path)


def request_gates(root: Path, protected_main: str) -> list[dict[str, str]]:
    """Recompute the frozen request-time entry-gate snapshot."""

    sprint = candidate_blob(
        root,
        protected_main,
        "_bmad-output/implementation-artifacts/sprint-status.yaml",
    ).decode("utf-8", errors="strict")
    for key in SPRINT_ROWS:
        if len(re.findall(rf"^\s{{2}}{re.escape(key)}:\s+backlog\s*$", sprint, re.MULTILINE)) != 1:
            raise EntryAuthorityError("V23_SPRINT_INVENTORY_DRIFT", key)
    if len(re.findall(rf"^\s{{2}}{re.escape(STORY_6_2_ROW)}:\s+done\s*$", sprint, re.MULTILINE)) != 1:
        raise EntryAuthorityError("V23_STORY_6_2_PREDECESSOR_DRIFT", STORY_6_2_ROW)
    v19 = load_json(
        candidate_blob(
            root,
            protected_main,
            "_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json",
        ),
        "V23_V19_INVALID",
    )
    oq1 = load_json(
        candidate_blob(root, protected_main, "_bmad-output/planning-artifacts/oq-1-landing-zone-approval-v1.json"),
        "V23_OQ1_INVALID",
    )
    ir0 = candidate_blob(
        root,
        protected_main,
        "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md",
    ).decode("utf-8", errors="strict")
    prd = candidate_blob(
        root,
        protected_main,
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
    ).decode("utf-8", errors="strict")
    validate_workflow_route(root, protected_main)
    return [
        gate(
            "V22-HISTORICAL-CANDIDATE",
            "PASS",
            HISTORICAL_V22_CANDIDATE,
            "the immutable direct-child V22 candidate remains PASS with execution disabled",
        ),
        gate(
            "V22-PROTECTED-MAIN-DIAGNOSTIC",
            "PASS",
            protected_main,
            "the two-parent protected merge is recorded as FAIL/CANDIDATE_GRAPH_DRIFT rather than reinterpreted",
        ),
        gate(
            "STORY-6.2-PREDECESSOR",
            "PASS",
            "_bmad-output/implementation-artifacts/sprint-status.yaml",
            "the immutable Story 6.2 predecessor is recorded done",
        ),
        gate(
            "7.1-SCHEMAS-CHECKPOINT",
            "PASS" if v19.get("result") == "PASS" else "FAIL",
            "_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json",
            "the historical schema checkpoint is bound as point-in-time evidence only",
        ),
        gate(
            "IR-0-READINESS",
            "PASS" if re.search(r"^result:\s+READY\s*$", ir0, re.MULTILINE) else "FAIL",
            "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md",
            "the independent readiness result is bound without treating it as a hold lift",
        ),
        gate(
            "CURRENT-WORKFLOW-ROUTE",
            "PASS",
            "_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json",
            "the current marker resolver and route inventory are frozen inputs to V23",
        ),
        gate(
            "PRODUCTION-OPERATIONAL-ENVELOPE",
            "MISSING",
            OPERATIONAL_ENVELOPE_PATH,
            "AD-5 requires the production operational envelope before every hold lift",
        ),
        gate(
            "FR-20-SM-C1",
            "PENDING" if "Preservation gate | **PENDING." in prd else "FAIL",
            "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
            "the current preservation denominator has no passing successor disposition",
        ),
        gate(
            "SM-C2",
            "FAIL" if "Performance gate | **FAILED on current evidence." in prd else "BLOCKED",
            "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
            "the current universal hot-path performance gate is failed",
        ),
        gate(
            "OQ-1",
            "BLOCKED" if oq1.get("result") == "BLOCKED" else "FAIL",
            "_bmad-output/planning-artifacts/oq-1-landing-zone-approval-v1.json",
            "the current landing-zone acceptance remains evidence-blocked",
        ),
        gate(
            "OWNER-APPROVAL",
            "MISSING",
            "",
            "the technical request contains no copied or prefilled owner decision",
        ),
        gate(
            "EPIC-7-SPRINT-INVENTORY",
            "PASS",
            "_bmad-output/implementation-artifacts/sprint-status.yaml",
            "Epic 7 and Stories 7.1 through 7.4 each have exactly one backlog row",
        ),
    ]


def request_blockers(gates: Sequence[dict[str, str]]) -> list[dict[str, Any]]:
    """Map every nonpassing entry gate to an explicit stable blocker."""

    codes = {
        "PRODUCTION-OPERATIONAL-ENVELOPE": "V23_OPERATIONAL_ENVELOPE_MISSING",
        "FR-20-SM-C1": "V23_PRESERVATION_GATE_PENDING",
        "SM-C2": "V23_PERFORMANCE_GATE_FAILED",
        "OQ-1": "V23_LANDING_ZONE_GATE_BLOCKED",
        "OWNER-APPROVAL": "V23_OWNER_APPROVAL_MISSING",
    }
    return [
        {"code": codes[row["id"]], "detail": row["detail"], "assertionIndex": index}
        for index, row in enumerate(gates)
        if row["state"] != "PASS" and row["id"] in codes
    ]


def request_ledger(gates: Sequence[dict[str, str]]) -> list[dict[str, str]]:
    """Convert all request gates into a nonempty evaluated assertion ledger."""

    blocker_codes = {
        "PRODUCTION-OPERATIONAL-ENVELOPE": "V23_OPERATIONAL_ENVELOPE_MISSING",
        "FR-20-SM-C1": "V23_PRESERVATION_GATE_PENDING",
        "SM-C2": "V23_PERFORMANCE_GATE_FAILED",
        "OQ-1": "V23_LANDING_ZONE_GATE_BLOCKED",
        "OWNER-APPROVAL": "V23_OWNER_APPROVAL_MISSING",
    }
    return [
        {
            "id": blocker_codes.get(row["id"], f"V23.REQUEST.{index:02d}"),
            "subject": row["id"],
            "state": "PASS" if row["state"] == "PASS" else ("FAIL" if row["state"] == "FAIL" else "BLOCKED"),
            "detail": row["detail"],
        }
        for index, row in enumerate(gates, start=1)
    ]


def tooling_manifest_from_worktree(root: Path) -> list[dict[str, str]]:
    """Bind every prospective tooling blob except the self-containing request."""

    return [worktree_binding(root, path) for path in TOOLING_PATHS if path != REQUEST_PATH]


def tooling_manifest_from_commit(root: Path, commit: str) -> list[dict[str, str]]:
    """Bind every committed tooling blob except the self-containing request."""

    return [binding(root, commit, path) for path in TOOLING_PATHS if path != REQUEST_PATH]


def render_request(root: Path) -> dict[str, Any]:
    """Render the deterministic technical request from current worktree tooling and committed evidence."""

    protected = resolve_commit(root, PROTECTED_MAIN, "V23_PROTECTED_MAIN_UNAVAILABLE")
    historical = resolve_commit(root, HISTORICAL_V22_CANDIDATE, "V23_V22_CANDIDATE_UNAVAILABLE")
    baseline = resolve_commit(root, TOOLING_BASELINE, "V23_TOOLING_BASELINE_UNAVAILABLE")
    if protected != PROTECTED_MAIN or historical != HISTORICAL_V22_CANDIDATE or baseline != TOOLING_BASELINE:
        raise EntryAuthorityError("V23_PINNED_COMMIT_DRIFT", "one or more pinned commits did not resolve exactly")
    parents = commit_parents(root, protected)
    if len(parents) != 2:
        raise EntryAuthorityError("V23_PROTECTED_MAIN_GRAPH_DRIFT", repr(parents))
    manifest = tooling_manifest_from_worktree(root)
    gates = request_gates(root, protected)
    document: dict[str, Any] = {
        "schemaVersion": SCHEMA_VERSION,
        "recordType": "REQUEST",
        "authorityId": AUTHORITY_ID,
        "requestId": REQUEST_ID,
        "protectedMain": {
            "commit": protected,
            "tree": commit_tree(root, protected),
            "parents": list(parents),
            "diagnosticResult": "FAIL",
            "diagnosticBlocker": "CANDIDATE_GRAPH_DRIFT",
        },
        "historicalV22": {
            "candidateCommit": historical,
            "candidateTree": commit_tree(root, historical),
            "result": "PASS",
            "executionAllowed": False,
        },
        "toolingTransaction": {
            "baselineCommit": baseline,
            "baselineTree": commit_tree(root, baseline),
            "exactChangedPaths": list(TOOLING_PATHS),
            "requiredMode": "100644",
            "selfExcludedManifestSha256": canonical_digest(manifest),
            "manifest": manifest,
        },
        "sourceBindings": [binding(root, protected, path) for path in SOURCE_PATHS],
        "rootGitlinks": root_gitlinks(root, protected),
        "entryGates": gates,
        "publicationContract": {
            "authorityPath": AUTHORITY_PATH,
            "architecturePath": ARCHITECTURE_PATH,
            "exactChangedPaths": list(AUTHORITY_PATHS),
            "requiredCommitSignature": True,
            "requiredDecision": "EXECUTION_ALLOWED",
            "story7_2Locked": True,
        },
        "resultSemantics": result_semantics(),
        "assertionLedger": request_ledger(gates),
        "blockers": request_blockers(gates),
        "result": "BLOCKED",
        "implementationHold": "ACTIVE",
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
    }
    validate_request_controls(document)
    validate_schema(root, document)
    return document


def publication_candidates(root: Path, evaluated: str, path: str) -> tuple[str, ...]:
    """Locate all commits that add a declared immutable path."""

    try:
        rows = run_git(
            root,
            "log",
            "--full-history",
            "--format=%H",
            "--diff-filter=A",
            evaluated,
            "--",
            safe_path(path),
        ).stdout.decode("ascii", errors="strict").splitlines()
    except UnicodeError as error:
        raise EntryAuthorityError("V23_PUBLICATION_HISTORY_INVALID", str(error), "BLOCKED") from error
    commits = tuple(row for row in rows if row)
    if any(_COMMIT.fullmatch(row) is None for row in commits):
        raise EntryAuthorityError("V23_PUBLICATION_HISTORY_INVALID", repr(commits), "BLOCKED")
    return commits


def locate_publication(root: Path, evaluated: str, path: str, code: str) -> str:
    """Require exactly one immutable publication in evaluated history."""

    publications = publication_candidates(root, evaluated, path)
    if len(publications) != 1:
        raise EntryAuthorityError(code, f"expected one publication; observed={publications!r}", "BLOCKED")
    return publications[0]


def load_v22_resolver(root: Path, commit: str) -> Any:
    """Load only the separately pinned historical V22 resolver blob."""

    content = candidate_blob(root, commit, RESOLVER_PATH, "V23_V22_RESOLVER_UNAVAILABLE")
    if sha256(content) != HISTORICAL_V22_RESOLVER_SHA256:
        raise EntryAuthorityError(
            "V23_V22_RESOLVER_IDENTITY_MISMATCH",
            f"expected={HISTORICAL_V22_RESOLVER_SHA256}; observed={sha256(content)}",
            "BLOCKED",
        )
    spec = importlib.util.spec_from_loader("v23_historical_v22_resolver", loader=None)
    if spec is None:
        raise EntryAuthorityError("V23_V22_RESOLVER_UNAVAILABLE", f"{RESOLVER_PATH}@{commit}", "BLOCKED")
    module = importlib.util.module_from_spec(spec)
    module.__file__ = f"{commit}:{RESOLVER_PATH}"
    try:
        exec(compile(content, module.__file__, "exec"), module.__dict__)
    except (Exception, SystemExit) as error:
        raise EntryAuthorityError("V23_V22_RESOLVER_UNAVAILABLE", str(error), "BLOCKED") from error
    return module


def validate_v22_history(
    root: Path,
    _resolver_commit: str,
    v22_resolver: Callable[[Path, str], dict[str, Any]] | None = None,
) -> None:
    """Prove the immutable V22 PASS and current merge diagnostic remain exact."""

    callback = v22_resolver
    if callback is None:
        module = load_v22_resolver(root, HISTORICAL_V22_CANDIDATE)
        callback = getattr(module, "resolve_v22_authority", None)
        if callback is None:
            callback = getattr(module, "resolve_authority", None)
        if not callable(callback):
            raise EntryAuthorityError("V23_V22_RESOLVER_INTERFACE_INVALID", RESOLVER_PATH, "BLOCKED")
    try:
        historical = callback(root, HISTORICAL_V22_CANDIDATE)
        current = callback(root, PROTECTED_MAIN)
    except BaseException as error:
        raise EntryAuthorityError("V23_V22_RESOLVER_EXECUTION_FAILED", str(error), "BLOCKED") from error
    if not isinstance(historical, dict) or not isinstance(current, dict):
        raise EntryAuthorityError("V23_V22_RESOLVER_INTERFACE_INVALID", "resolver returned a non-object", "BLOCKED")
    historical_ok = (
        historical.get("result") == "PASS"
        and historical.get("executionAllowed") is False
        and bool(historical.get("assertionLedger"))
    )
    blockers = current.get("blockers")
    current_ok = (
        current.get("result") == "FAIL"
        and current.get("executionAllowed") is False
        and isinstance(blockers, list)
        and bool(blockers)
        and blockers[0].get("code") == "CANDIDATE_GRAPH_DRIFT"
        and bool(current.get("assertionLedger"))
    )
    if not historical_ok or not current_ok:
        raise EntryAuthorityError(
            "V23_V22_HISTORY_DRIFT",
            f"historical={historical.get('result')!r}; current={current.get('result')!r}; blockers={blockers!r}",
        )


def validate_request(
    root: Path,
    evaluated_revision: str,
    *,
    v22_resolver: Callable[[Path, str], dict[str, Any]] | None = None,
) -> tuple[dict[str, Any], str, bytes]:
    """Validate the committed request, its exact transaction, and every frozen source fact."""

    evaluated = resolve_commit(root, evaluated_revision, "V23_EVALUATED_CANDIDATE_UNAVAILABLE")
    publication = locate_publication(root, evaluated, REQUEST_PATH, "V23_REQUEST_PUBLICATION_MISSING")
    require_ancestor(root, publication, evaluated, "V23_REQUEST_NOT_ANCESTOR")
    content = candidate_blob(root, publication, REQUEST_PATH, "V23_REQUEST_MISSING")
    if candidate_blob(root, evaluated, REQUEST_PATH, "V23_REQUEST_MISSING") != content:
        raise EntryAuthorityError("V23_REQUEST_DESCENDANT_DRIFT", REQUEST_PATH)
    document = load_json(content, "V23_REQUEST_INVALID")
    validate_schema(root, document, schema_commit=publication)
    validate_request_controls(document)
    transaction = document["toolingTransaction"]
    baseline = resolve_commit(root, transaction["baselineCommit"], "V23_TOOLING_BASELINE_UNAVAILABLE")
    if baseline != TOOLING_BASELINE or transaction["baselineTree"] != commit_tree(root, baseline):
        raise EntryAuthorityError("V23_TOOLING_BASELINE_DRIFT", repr(transaction))
    parents = commit_parents(root, publication)
    if parents != (baseline,):
        raise EntryAuthorityError("V23_TOOLING_PARENT_DRIFT", f"expected={(baseline,)!r}; observed={parents!r}")
    observed_paths = changed_paths(root, baseline, publication)
    if observed_paths != TOOLING_PATHS or tuple(transaction["exactChangedPaths"]) != TOOLING_PATHS:
        missing = sorted(set(TOOLING_PATHS) - set(observed_paths))
        unexpected = sorted(set(observed_paths) - set(TOOLING_PATHS))
        raise EntryAuthorityError("V23_TOOLING_SCOPE_DRIFT", f"missing={missing!r}; unexpected={unexpected!r}")
    if changed_gitlinks(root, baseline, publication):
        raise EntryAuthorityError("V23_TOOLING_GITLINK_DRIFT", repr(changed_gitlinks(root, baseline, publication)))
    for path in TOOLING_PATHS:
        mode, kind, _object_id = tree_record(root, publication, path)
        if (mode, kind) != ("100644", "blob"):
            raise EntryAuthorityError("V23_TOOLING_MODE_DRIFT", f"{path}: {mode} {kind}")
    manifest = tooling_manifest_from_commit(root, publication)
    if transaction["manifest"] != manifest or transaction["selfExcludedManifestSha256"] != canonical_digest(manifest):
        raise EntryAuthorityError("V23_TOOLING_MANIFEST_DRIFT", "self-excluded tooling manifest mismatch")
    if tooling_manifest_from_commit(root, evaluated) != manifest:
        raise EntryAuthorityError(
            "V23_TOOLING_DESCENDANT_DRIFT",
            "request-pinned tooling changed after its publication",
        )
    validate_workflow_route(root, publication)
    protected = resolve_commit(root, document["protectedMain"]["commit"], "V23_PROTECTED_MAIN_UNAVAILABLE")
    if (
        protected != PROTECTED_MAIN
        or document["protectedMain"]["tree"] != commit_tree(root, protected)
        or tuple(document["protectedMain"]["parents"]) != commit_parents(root, protected)
    ):
        raise EntryAuthorityError("V23_PROTECTED_MAIN_DRIFT", repr(document["protectedMain"]))
    historical = resolve_commit(
        root,
        document["historicalV22"]["candidateCommit"],
        "V23_V22_CANDIDATE_UNAVAILABLE",
    )
    if historical != HISTORICAL_V22_CANDIDATE or document["historicalV22"]["candidateTree"] != commit_tree(root, historical):
        raise EntryAuthorityError("V23_V22_BINDING_DRIFT", repr(document["historicalV22"]))
    source_bindings = [binding(root, protected, path) for path in SOURCE_PATHS]
    if document["sourceBindings"] != source_bindings:
        raise EntryAuthorityError("V23_SOURCE_BINDING_DRIFT", "protected-main source manifest mismatch")
    protected_links = root_gitlinks(root, protected)
    if document["rootGitlinks"] != protected_links or root_gitlinks(root, publication) != protected_links:
        raise EntryAuthorityError("V23_ROOT_GITLINK_DRIFT", "protected, request, and declared gitlinks differ")
    gates = request_gates(root, protected)
    if document["entryGates"] != gates:
        raise EntryAuthorityError("V23_REQUEST_GATE_DRIFT", "request gate snapshot mismatch")
    if document["blockers"] != request_blockers(gates) or document["assertionLedger"] != request_ledger(gates):
        raise EntryAuthorityError("V23_REQUEST_RESULT_DRIFT", "blocker or assertion ledger mismatch")
    validate_v22_history(root, publication, v22_resolver)
    return document, publication, content


def request_check_result(document: dict[str, Any], publication: str) -> dict[str, Any]:
    """Return a closed BLOCKED result proving the technical request did not grant execution."""

    return {
        "schemaVersion": RESULT_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {
            "requestPublication": publication,
            "protectedMain": document["protectedMain"]["commit"],
            "historicalV22Candidate": document["historicalV22"]["candidateCommit"],
        },
        "assertionLedger": document["assertionLedger"],
        "blockers": document["blockers"],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }


def correction_worktree_binding(root: Path, path: str) -> dict[str, str]:
    """Bind one prospective V24 input and preserve a V24-specific stable blocker."""

    try:
        return worktree_binding(root, path)
    except EntryAuthorityError as error:
        raise EntryAuthorityError("V24_TOOLING_INPUT_UNAVAILABLE", f"{path}: {error.detail}", "BLOCKED") from error


def correction_manifest_from_worktree(root: Path) -> list[dict[str, str]]:
    """Bind the seven V24 blobs that do not contain the correction record."""

    return [correction_worktree_binding(root, path) for path in CORRECTION_MANIFEST_PATHS]


def correction_manifest_from_commit(root: Path, commit: str) -> list[dict[str, str]]:
    """Bind the seven committed V24 blobs that do not contain the correction record."""

    try:
        return [binding(root, commit, path) for path in CORRECTION_MANIFEST_PATHS]
    except EntryAuthorityError as error:
        raise EntryAuthorityError("V24_TOOLING_MANIFEST_DRIFT", error.detail, error.state) from error


def require_v24_blob_mode(root: Path, commit: str, path: str) -> None:
    """Require one evaluated V24-bound path to remain a non-executable regular blob."""

    try:
        mode, kind, _object_id = tree_record(root, commit, path)
    except EntryAuthorityError as error:
        raise EntryAuthorityError("V24_TOOLING_MODE_DRIFT", f"{path}: {error.detail}", "BLOCKED") from error
    if (mode, kind) != ("100644", "blob"):
        raise EntryAuthorityError("V24_TOOLING_MODE_DRIFT", f"{path}: {mode} {kind}", "BLOCKED")


def render_correction(root: Path) -> dict[str, Any]:
    """Render the deterministic V24 correction after authenticating immutable V23."""

    baseline = resolve_commit(root, V23_REQUEST_PUBLICATION, "V24_V23_PUBLICATION_UNAVAILABLE")
    if baseline != V23_REQUEST_PUBLICATION:
        raise EntryAuthorityError("V24_V23_PUBLICATION_DRIFT", repr(baseline), "BLOCKED")
    head = resolve_commit(root, "HEAD", "V24_GENERATION_BASELINE_UNAVAILABLE")
    if head != baseline:
        raise EntryAuthorityError(
            "V24_GENERATION_BASELINE_DRIFT",
            f"expected HEAD={baseline}; observed={head}",
            "BLOCKED",
        )
    request, request_publication, request_content = validate_request(root, baseline)
    if request_publication != baseline:
        raise EntryAuthorityError(
            "V24_V23_PUBLICATION_DRIFT",
            f"expected={baseline}; observed={request_publication}",
            "BLOCKED",
        )
    historical_publisher = candidate_blob(root, baseline, PUBLISHER_PATH, "V24_V23_PUBLISHER_UNAVAILABLE")
    if sha256(historical_publisher) != V23_PUBLISHER_SHA256:
        raise EntryAuthorityError(
            "V24_V23_PUBLISHER_IDENTITY_MISMATCH",
            f"expected={V23_PUBLISHER_SHA256}; observed={sha256(historical_publisher)}",
            "BLOCKED",
        )
    manifest = correction_manifest_from_worktree(root)
    document: dict[str, Any] = {
        "schemaVersion": CORRECTION_SCHEMA_VERSION,
        "recordType": "TOOLING_CORRECTION",
        "correctionId": CORRECTION_ID,
        "predecessor": {
            "requestPath": REQUEST_PATH,
            "publicationCommit": baseline,
            "publicationTree": commit_tree(root, baseline),
            "requestSha256": sha256(request_content),
            "publisherSha256": V23_PUBLISHER_SHA256,
        },
        "toolingTransaction": {
            "baselineCommit": baseline,
            "baselineTree": commit_tree(root, baseline),
            "exactChangedPaths": list(CORRECTION_PATHS),
            "requiredMode": "100644",
            "selfExcludedManifestSha256": canonical_digest(manifest),
            "manifest": manifest,
        },
        "rootGitlinks": root_gitlinks(root, baseline),
        "resultSemantics": result_semantics(),
        "assertionLedger": correction_ledger(),
        "blockers": [],
        "result": "PASS",
        "implementationHold": "ACTIVE",
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }
    # Keep the independently reconstructed V23 request live in this path. The value is
    # intentionally unused beyond authentication because V24 must not reinterpret it.
    if request.get("result") != "BLOCKED" or request.get("executionAllowed") is not False:
        raise EntryAuthorityError("V24_V23_REQUEST_RESULT_DRIFT", "immutable V23 request is not BLOCKED/false")
    validate_correction_controls(document)
    validate_correction_schema(root, document)
    return document


def locate_correction_publication(root: Path, evaluated: str) -> str:
    """Locate the single immutable V24 correction publication."""

    return locate_publication(
        root,
        evaluated,
        CORRECTION_PATH,
        "V24_CORRECTION_PUBLICATION_MISSING",
    )


def validate_correction(
    root: Path,
    evaluated_revision: str,
) -> tuple[dict[str, Any], str, bytes]:
    """Validate V23 first, then the exact V24 correction from raw committed objects."""

    evaluated = resolve_commit(root, evaluated_revision, "V24_EVALUATED_CANDIDATE_UNAVAILABLE")
    publication = locate_correction_publication(root, evaluated)
    require_ancestor(root, publication, evaluated, "V24_CORRECTION_NOT_ANCESTOR")
    parents = commit_parents(root, publication)
    if parents != (V23_REQUEST_PUBLICATION,):
        raise EntryAuthorityError(
            "V24_TOOLING_PARENT_DRIFT",
            f"expected={(V23_REQUEST_PUBLICATION,)!r}; observed={parents!r}",
            "BLOCKED",
        )
    observed_paths = changed_paths(root, V23_REQUEST_PUBLICATION, publication)
    if observed_paths != CORRECTION_PATHS:
        missing = sorted(set(CORRECTION_PATHS) - set(observed_paths))
        unexpected = sorted(set(observed_paths) - set(CORRECTION_PATHS))
        raise EntryAuthorityError(
            "V24_TOOLING_SCOPE_DRIFT",
            f"missing={missing!r}; unexpected={unexpected!r}",
            "BLOCKED",
        )
    changed_links = changed_gitlinks(root, V23_REQUEST_PUBLICATION, publication)
    if changed_links:
        raise EntryAuthorityError("V24_TOOLING_GITLINK_DRIFT", repr(changed_links), "BLOCKED")
    for path in CORRECTION_PATHS:
        require_v24_blob_mode(root, publication, path)
    require_v24_blob_mode(root, evaluated, CORRECTION_PATH)
    require_v24_blob_mode(root, evaluated, REQUEST_PATH)
    content = candidate_blob(root, publication, CORRECTION_PATH, "V24_CORRECTION_MISSING")
    if candidate_blob(root, evaluated, CORRECTION_PATH, "V24_CORRECTION_MISSING") != content:
        raise EntryAuthorityError("V24_CORRECTION_DESCENDANT_DRIFT", CORRECTION_PATH, "BLOCKED")
    document = load_json(content, "V24_CORRECTION_INVALID")
    validate_correction_schema(root, document, schema_commit=publication)
    validate_correction_controls(document)
    transaction = document["toolingTransaction"]
    if (
        transaction["baselineTree"] != commit_tree(root, V23_REQUEST_PUBLICATION)
        or document["predecessor"]["publicationTree"] != commit_tree(root, V23_REQUEST_PUBLICATION)
    ):
        raise EntryAuthorityError("V24_V23_PUBLICATION_DRIFT", "V23 tree identity drifted", "BLOCKED")
    request, request_publication, request_content = validate_request(root, V23_REQUEST_PUBLICATION)
    historical_publisher = candidate_blob(
        root,
        V23_REQUEST_PUBLICATION,
        PUBLISHER_PATH,
        "V24_V23_PUBLISHER_UNAVAILABLE",
    )
    predecessor = document["predecessor"]
    if (
        request_publication != V23_REQUEST_PUBLICATION
        or predecessor["requestSha256"] != sha256(request_content)
        or predecessor["publisherSha256"] != sha256(historical_publisher)
        or sha256(historical_publisher) != V23_PUBLISHER_SHA256
        or request.get("result") != "BLOCKED"
        or request.get("executionAllowed") is not False
        or candidate_blob(root, evaluated, REQUEST_PATH, "V24_V23_REQUEST_UNAVAILABLE") != request_content
    ):
        raise EntryAuthorityError(
            "V24_V23_PUBLICATION_DRIFT",
            "immutable V23 request, publisher, or non-executable result drifted",
            "BLOCKED",
        )
    manifest = correction_manifest_from_commit(root, publication)
    if (
        transaction["manifest"] != manifest
        or transaction["selfExcludedManifestSha256"] != canonical_digest(manifest)
    ):
        raise EntryAuthorityError("V24_TOOLING_MANIFEST_DRIFT", "self-excluded manifest mismatch", "BLOCKED")
    if correction_manifest_from_commit(root, evaluated) != manifest:
        raise EntryAuthorityError(
            "V24_TOOLING_DESCENDANT_DRIFT",
            "correction-pinned tooling changed after publication",
            "BLOCKED",
        )
    baseline_links = root_gitlinks(root, V23_REQUEST_PUBLICATION)
    if (
        document["rootGitlinks"] != baseline_links
        or root_gitlinks(root, publication) != baseline_links
        or root_gitlinks(root, evaluated) != baseline_links
    ):
        raise EntryAuthorityError("V24_ROOT_GITLINK_DRIFT", "V23, V24, and evaluated gitlinks differ", "BLOCKED")
    return document, publication, content


def validate_current_request(
    root: Path,
    evaluated_revision: str,
    *,
    v22_resolver: Callable[[Path, str], dict[str, Any]] | None = None,
) -> tuple[dict[str, Any], str, bytes, str]:
    """Keep V23 as request identity and V24 as the later evidence-scope anchor."""

    _correction, correction_publication, _content = validate_correction(root, evaluated_revision)
    request, request_publication, request_content = validate_request(
        root,
        V23_REQUEST_PUBLICATION,
        v22_resolver=v22_resolver,
    )
    if request_publication != V23_REQUEST_PUBLICATION:
        raise EntryAuthorityError(
            "V24_V23_PUBLICATION_DRIFT",
            f"expected={V23_REQUEST_PUBLICATION}; observed={request_publication}",
            "BLOCKED",
        )
    return request, request_publication, request_content, correction_publication


def validate_owner_fields(identity: str, decided_at_utc: str, rationale: str) -> None:
    """Require explicit non-placeholder trusted owner fields."""

    if identity.strip() != TRUSTED_OWNER_IDENTITY:
        raise EntryAuthorityError("V23_OWNER_IDENTITY_INVALID", repr(identity))
    rationale_symbols = {character.casefold() for character in rationale if character.isalnum()}
    if len(rationale.strip()) < 20 or not substantive(rationale) or len(rationale_symbols) < 4:
        raise EntryAuthorityError("V23_OWNER_RATIONALE_INVALID", "rationale is non-substantive")
    try:
        decided = datetime.fromisoformat(decided_at_utc.replace("Z", "+00:00"))
    except ValueError as error:
        raise EntryAuthorityError("V23_OWNER_TIME_INVALID", decided_at_utc) from error
    if decided.tzinfo is None or decided.utcoffset() != timezone.utc.utcoffset(decided):
        raise EntryAuthorityError("V23_OWNER_TIME_INVALID", decided_at_utc)


def utc_instant(value: Any, code: str) -> datetime:
    """Parse one strict second-precision UTC instant."""

    if not isinstance(value, str) or re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", value) is None:
        raise EntryAuthorityError(code, repr(value))
    try:
        return datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
    except ValueError as error:
        raise EntryAuthorityError(code, str(error)) from error


def trusted_validation_time(clock: Callable[[], datetime] | None = None) -> datetime:
    """Return an injectable, timezone-aware UTC validation observation."""

    observed = (clock or (lambda: datetime.now(timezone.utc)))()
    if not isinstance(observed, datetime) or observed.tzinfo is None:
        raise EntryAuthorityError("V23_VALIDATION_TIME_INVALID", repr(observed), "BLOCKED")
    return observed.astimezone(timezone.utc)


def nonempty_string(value: Any) -> bool:
    """Return whether a JSON value is a concrete non-placeholder string."""

    return isinstance(value, str) and bool(value.strip()) and value.strip().casefold() not in {
        "tbd",
        "todo",
        "unknown",
        "n/a",
    }


def identifier(value: Any) -> bool:
    """Return whether a JSON value is a stable machine identifier."""

    return isinstance(value, str) and re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._:/-]{2,127}", value) is not None


def substantive(value: Any) -> bool:
    """Return whether a disposition is actionable rather than a token or placeholder."""

    return nonempty_string(value) and len(value.strip()) >= 12 and len(value.split()) >= 2


def validate_recovery_runbook(content: bytes, expected_owner: str, recovery: dict[str, Any]) -> dict[str, Any]:
    """Validate the one canonical Story 7.1 recovery identity and procedure inventory."""

    document = load_json(content, "V23_OPERATIONAL_ENVELOPE_INVALID")
    procedures = document.get("procedures")
    if (
        set(document) != {
            "schemaVersion",
            "runbookId",
            "ownerIdentity",
            "rpoMinutes",
            "rtoMinutes",
            "procedures",
        }
        or document.get("schemaVersion") != "hexalith.conversations.story-7.1-production-recovery.v1"
        or document.get("runbookId") != "STORY-7.1-PRODUCTION-RECOVERY"
        or document.get("ownerIdentity") != expected_owner
        or document.get("rpoMinutes") != recovery.get("rpoMinutes")
        or document.get("rtoMinutes") != recovery.get("rtoMinutes")
        or not isinstance(procedures, list)
        or len(procedures) != len(RECOVERY_PROCEDURE_IDS)
    ):
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "canonical recovery runbook identity drift")
    observed_ids: list[str] = []
    for row in procedures:
        procedure_id = row.get("id") if isinstance(row, dict) else None
        if (
            not isinstance(row, dict)
            or set(row) != {"id", "responsibleOwner", "action", "outcome"}
            or procedure_id not in RECOVERY_PROCEDURE_IDS
            or procedure_id in observed_ids
            or row.get("responsibleOwner") != expected_owner
            or (row.get("action"), row.get("outcome")) != RECOVERY_PROCEDURES.get(procedure_id)
        ):
            raise EntryAuthorityError(
                "V23_OPERATIONAL_ENVELOPE_INVALID",
                "canonical recovery procedure inventory drift",
            )
        observed_ids.append(procedure_id)
    if tuple(observed_ids) != RECOVERY_PROCEDURE_IDS:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "recovery procedure order drift")
    return document


def validate_operational_envelope(
    content: bytes,
    expected_owner: str = TRUSTED_OWNER_IDENTITY,
    *,
    root: Path | None = None,
    source: str | None = None,
    decision_time: datetime | None = None,
    validation_time: datetime | None = None,
) -> dict[str, Any]:
    """Validate the closed AD-5 ownership, recovery, scaling, and waiver contract."""

    document = load_json(content, "V23_OPERATIONAL_ENVELOPE_INVALID")
    if set(document) != {
        "schemaVersion",
        "owner",
        "evaluatedAtUtc",
        "environments",
        "recovery",
        "scaling",
        "waiver",
    } or document.get("schemaVersion") != "hexalith.conversations.production-operational-envelope.v1":
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "closed envelope identity mismatch")
    owner = document.get("owner")
    if (
        not isinstance(owner, dict)
        or set(owner) != {"identity", "role", "authorityId"}
        or owner.get("identity") != expected_owner
        or owner.get("role") != "AD-5 operational owner"
        or not identifier(owner.get("authorityId"))
    ):
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "AD-5 owner is absent or mismatched")
    evaluated = utc_instant(document.get("evaluatedAtUtc"), "V23_OPERATIONAL_ENVELOPE_INVALID")
    environments = document.get("environments")
    required_capabilities = {"state", "pubsub", "secrets", "identity", "health", "telemetry"}
    required_environments = {"local", "ci", "staging", "production"}
    if not isinstance(environments, list) or len(environments) != len(required_environments):
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "exact environment inventory is required")
    observed_environments: set[str] = set()
    for row in environments:
        if (
            not isinstance(row, dict)
            or set(row) != {"environmentId", "providerId", "responsibleOwner", "dependencies"}
            or row.get("environmentId") not in required_environments
            or row["environmentId"] in observed_environments
            or not identifier(row.get("providerId"))
            or row.get("responsibleOwner") != expected_owner
            or not isinstance(row.get("dependencies"), list)
            or len(row["dependencies"]) != len(required_capabilities)
        ):
            raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "environment identity is incomplete")
        observed_environments.add(row["environmentId"])
        observed_capabilities: set[str] = set()
        for dependency in row["dependencies"]:
            if (
                not isinstance(dependency, dict)
                or set(dependency) != {"capability", "providerId", "responsibleOwner", "responsibility"}
                or dependency.get("capability") not in required_capabilities
                or dependency["capability"] in observed_capabilities
                or not identifier(dependency.get("providerId"))
                or dependency.get("responsibleOwner") != expected_owner
                or not substantive(dependency.get("responsibility"))
            ):
                raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "dependency responsibility is incomplete")
            observed_capabilities.add(dependency["capability"])
        if observed_capabilities != required_capabilities:
            raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "dependency inventory is incomplete")
    if observed_environments != required_environments:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "environment inventory is incomplete")
    recovery = document.get("recovery")
    if not isinstance(recovery, dict) or set(recovery) != {
        "rpoMinutes",
        "rtoMinutes",
        "backupProcedure",
        "rebuildProcedure",
        "disasterRecoveryProcedure",
        "responsibleOwner",
        "runbook",
    }:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "closed recovery disposition is required")
    if (
        not isinstance(recovery.get("rpoMinutes"), int)
        or isinstance(recovery.get("rpoMinutes"), bool)
        or recovery["rpoMinutes"] < 0
        or not isinstance(recovery.get("rtoMinutes"), int)
        or isinstance(recovery.get("rtoMinutes"), bool)
        or recovery["rtoMinutes"] <= 0
        or recovery.get("responsibleOwner") != expected_owner
        or any(recovery.get(key) != value for key, value in RECOVERY_SUMMARIES.items())
    ):
        raise EntryAuthorityError(
            "V23_OPERATIONAL_ENVELOPE_INVALID",
            "recovery summaries do not reference the canonical procedure inventory",
        )
    runbook = recovery.get("runbook")
    if (
        not isinstance(runbook, dict)
        or set(runbook) != {"path", "sha256"}
        or not isinstance(runbook.get("path"), str)
        or not isinstance(runbook.get("sha256"), str)
        or re.fullmatch(r"[0-9a-f]{64}", runbook["sha256"]) is None
    ):
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "runbook binding is incomplete")
    runbook_path = safe_path(runbook["path"])
    if runbook_path != RECOVERY_RUNBOOK_PATH:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "recovery runbook path is not canonical")
    if root is not None and source is not None:
        runbook_content = candidate_blob(root, source, runbook_path)
        if sha256(runbook_content) != runbook["sha256"]:
            raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "runbook digest does not bind committed bytes")
        validate_recovery_runbook(runbook_content, expected_owner, recovery)
    scaling = document.get("scaling")
    if (
        not isinstance(scaling, dict)
        or set(scaling) != {
            "minimumReplicas",
            "maximumReplicas",
            "strategy",
            "metric",
            "scaleOutThreshold",
            "scaleInThreshold",
            "responsibleOwner",
        }
        or not isinstance(scaling.get("minimumReplicas"), int)
        or isinstance(scaling.get("minimumReplicas"), bool)
        or scaling["minimumReplicas"] < 1
        or not isinstance(scaling.get("maximumReplicas"), int)
        or isinstance(scaling.get("maximumReplicas"), bool)
        or scaling["maximumReplicas"] < scaling["minimumReplicas"]
        or scaling.get("strategy") not in {"HORIZONTAL", "FIXED"}
        or not identifier(scaling.get("metric"))
        or not isinstance(scaling.get("scaleOutThreshold"), (int, float))
        or isinstance(scaling.get("scaleOutThreshold"), bool)
        or not math.isfinite(scaling["scaleOutThreshold"])
        or not isinstance(scaling.get("scaleInThreshold"), (int, float))
        or isinstance(scaling.get("scaleInThreshold"), bool)
        or not math.isfinite(scaling["scaleInThreshold"])
        or (
            scaling.get("strategy") == "HORIZONTAL"
            and (
                scaling["maximumReplicas"] <= scaling["minimumReplicas"]
                or scaling["scaleInThreshold"] >= scaling["scaleOutThreshold"]
            )
        )
        or scaling.get("responsibleOwner") != expected_owner
        or (
            scaling.get("strategy") == "FIXED"
            and (
                scaling["minimumReplicas"] != 1
                or scaling["maximumReplicas"] != 1
                or scaling["scaleOutThreshold"] != 0
                or scaling["scaleInThreshold"] != 0
            )
        )
    ):
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "closed scaling disposition is required")
    waiver = document.get("waiver")
    if not isinstance(waiver, dict) or set(waiver) != {"state", "expiresAtUtc", "rationale", "ownerIdentity"}:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "closed waiver state is required")
    if waiver.get("state") == "NONE":
        if any(waiver.get(key) is not None for key in ("expiresAtUtc", "rationale", "ownerIdentity")):
            raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "NONE waiver cannot carry expiry or rationale")
    elif waiver.get("state") == "ACTIVE":
        expiry = utc_instant(waiver.get("expiresAtUtc"), "V23_OPERATIONAL_ENVELOPE_INVALID")
        expiry_boundary = decision_time or evaluated
        if (
            expiry <= expiry_boundary
            or (validation_time is not None and expiry <= validation_time)
            or waiver.get("ownerIdentity") != expected_owner
            or not substantive(waiver.get("rationale"))
        ):
            raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "waiver is expired or lacks rationale")
    else:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "waiver state must be NONE or ACTIVE")
    if decision_time is not None and evaluated > decision_time:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "envelope evaluation is later than owner decision")
    if validation_time is not None and evaluated > validation_time:
        raise EntryAuthorityError("V23_OPERATIONAL_ENVELOPE_INVALID", "envelope evaluation is later than validation")
    return document


def disposition_gates(evidence_paths: dict[str, str], owner_identity: str) -> list[dict[str, str]]:
    """Build the all-PASS decision-time gate inventory from explicit committed evidence paths."""

    return [
        gate("V22-HISTORICAL-CANDIDATE", "PASS", HISTORICAL_V22_CANDIDATE, "historical V22 remains PASS/false"),
        gate("V22-PROTECTED-MAIN-DIAGNOSTIC", "PASS", PROTECTED_MAIN, "protected merge diagnostic remains exact"),
        gate("STORY-6.2-PREDECESSOR", "PASS", evidence_paths["story62"], "Story 6.2 predecessor remains accepted"),
        gate("7.1-SCHEMAS-CHECKPOINT", "PASS", evidence_paths["checkpoint"], "schema checkpoint is current and compatible"),
        gate("IR-0-READINESS", "PASS", evidence_paths["ir0"], "independent readiness is current and compatible"),
        gate("CURRENT-WORKFLOW-ROUTE", "PASS", evidence_paths["workflow"], "current route and conformance gate pass"),
        gate("PRODUCTION-OPERATIONAL-ENVELOPE", "PASS", evidence_paths["operational"], "AD-5 production envelope passes"),
        gate("FR-20-SM-C1", "PASS", evidence_paths["preservation"], "preservation gate has a valid current disposition"),
        gate("SM-C2", "PASS", evidence_paths["performance"], "performance gate has a valid current disposition"),
        gate("OQ-1", "PASS", evidence_paths["landingZone"], "landing-zone gate has a valid current disposition"),
        gate("OWNER-APPROVAL", "PASS", owner_identity, "trusted owner explicitly approves EXECUTION_ALLOWED"),
        gate("EPIC-7-SPRINT-INVENTORY", "PASS", evidence_paths["sprint"], "Epic 7 lifecycle inventory remains unchanged"),
    ]


def successor_repository_binding(
    root: Path,
    request_publication: str,
    grant_id: str,
    predecessor_repository: Any,
) -> dict[str, str]:
    """Refresh only the accepted snapshot while preserving the closed repository identity."""

    expected_identity = LANDING_ZONE_GRANT_REPOSITORIES[grant_id]
    if not isinstance(predecessor_repository, dict):
        raise EntryAuthorityError("V23_LANDING_ZONE_EVIDENCE_INVALID", f"{grant_id} repository identity drift")
    predecessor_identity = dict(predecessor_repository)
    predecessor_identity.pop("acceptedSourceSnapshot", None)
    if predecessor_identity != expected_identity:
        raise EntryAuthorityError("V23_LANDING_ZONE_EVIDENCE_INVALID", f"{grant_id} repository identity drift")
    gitlink_path = expected_identity.get("gitlinkPath")
    if gitlink_path is None:
        accepted_snapshot = PROTECTED_MAIN
    else:
        matching = [row for row in root_gitlinks(root, request_publication) if row["path"] == gitlink_path]
        if len(matching) != 1 or matching[0]["mode"] != "160000":
            raise EntryAuthorityError(
                "V23_LANDING_ZONE_EVIDENCE_INVALID",
                f"{grant_id} request gitlink snapshot is unavailable",
            )
        accepted_snapshot = matching[0]["objectId"]
    return {**expected_identity, "acceptedSourceSnapshot": accepted_snapshot}


def validate_landing_zone_successor(
    root: Path,
    source: str,
    evaluated: str,
    predecessor: dict[str, Any],
    predecessor_content: bytes,
    decision_id: str,
    disposition_time: datetime,
    validation_time: datetime | None,
) -> tuple[dict[str, Any], dict[str, str]]:
    """Validate one new approved decision without relabelling its blocked predecessor."""

    predecessor_rows = predecessor.get("decisions")
    if predecessor.get("result") != "BLOCKED" or not isinstance(predecessor_rows, list):
        raise EntryAuthorityError("V23_LANDING_ZONE_EVIDENCE_INVALID", "canonical predecessor is not BLOCKED")
    matching = [row for row in predecessor_rows if isinstance(row, dict) and row.get("requirementId") == decision_id]
    if len(matching) != 1 or matching[0].get("status") != "grant-issued-evidence-blocked":
        raise EntryAuthorityError(
            "V23_LANDING_ZONE_EVIDENCE_INVALID",
            f"{decision_id} blocked predecessor identity drift",
        )
    predecessor_row = matching[0]
    grant_ids = predecessor_row.get("owningRepositoryApproval")
    if (
        not isinstance(grant_ids, list)
        or not grant_ids
        or len(set(grant_ids)) != len(grant_ids)
        or any(grant_id not in LANDING_ZONE_GRANT_PATHS for grant_id in grant_ids)
    ):
        raise EntryAuthorityError("V23_LANDING_ZONE_EVIDENCE_INVALID", f"{decision_id} grant inventory drift")
    effective_grants: list[dict[str, Any]] = []
    for grant_id in grant_ids:
        predecessor_path = LANDING_ZONE_GRANT_PATHS[grant_id]
        predecessor_grant_content = candidate_blob(root, evaluated, predecessor_path)
        predecessor_grant = load_json(predecessor_grant_content, "V23_LANDING_ZONE_EVIDENCE_INVALID")
        if (
            predecessor_grant.get("grantId") != grant_id
            or predecessor_grant.get("status") != "issued-pending-evidence-closure"
            or predecessor_grant.get("effective") is not False
            or predecessor_grant.get("effectiveAtUtc") is not None
            or predecessor_grant.get("grantor", {}).get("authorityId") != LANDING_ZONE_APPROVING_AUTHORITY
            or tuple(predecessor_grant.get("requirements", ())) != LANDING_ZONE_GRANT_REQUIREMENTS[grant_id]
        ):
            raise EntryAuthorityError(
                "V23_LANDING_ZONE_EVIDENCE_INVALID",
                f"{decision_id} predecessor grant identity drift",
            )
        successor_grant_path = LANDING_ZONE_SUCCESSOR_GRANT_PATHS[grant_id]
        successor_grant_content = candidate_blob(
            root,
            source,
            successor_grant_path,
            "V23_LANDING_ZONE_EVIDENCE_MISSING",
        )
        successor_grant = load_json(successor_grant_content, "V23_LANDING_ZONE_EVIDENCE_INVALID")
        effective_time = utc_instant(
            successor_grant.get("effectiveAtUtc"),
            "V23_LANDING_ZONE_EVIDENCE_INVALID",
        )
        refreshed_repository = successor_repository_binding(
            root,
            evaluated,
            grant_id,
            predecessor_grant.get("repository"),
        )
        expected_grant = {
            "schemaVersion": "hexalith.conversations.oq-1-effective-successor-grant.v1",
            "grantId": f"{grant_id}-EFFECTIVE-SUCCESSOR-v1",
            "predecessor": {
                "path": predecessor_path,
                "grantId": grant_id,
                "status": "issued-pending-evidence-closure",
                "effective": False,
                "sha256": sha256(predecessor_grant_content),
            },
            "grantor": {
                "authorityId": LANDING_ZONE_APPROVING_AUTHORITY,
                "binding": binding(root, evaluated, LANDING_ZONE_APPROVAL_PATH),
            },
            "repository": refreshed_repository,
            "requirements": list(LANDING_ZONE_GRANT_REQUIREMENTS[grant_id]),
            "effective": True,
            "effectiveAtUtc": successor_grant.get("effectiveAtUtc"),
            "decision": "EXECUTION_ALLOWED",
        }
        if (
            successor_grant != expected_grant
            or effective_time > disposition_time
            or (validation_time is not None and effective_time > validation_time)
        ):
            raise EntryAuthorityError(
                "V23_LANDING_ZONE_EVIDENCE_INVALID",
                f"{decision_id} effective successor grant is missing, blocked, or temporally invalid",
            )
        effective_grants.append(
            {
                "authorityId": successor_grant["grantId"],
                "predecessorAuthorityId": grant_id,
                "binding": binding(root, source, successor_grant_path),
            }
        )
    successor_path = LANDING_ZONE_SUCCESSOR_PATHS[decision_id]
    successor_content = candidate_blob(root, source, successor_path, "V23_LANDING_ZONE_EVIDENCE_MISSING")
    successor = load_json(successor_content, "V23_LANDING_ZONE_EVIDENCE_INVALID")
    expected = {
        "schemaVersion": "hexalith.conversations.oq-1-approved-successor-decision.v1",
        "decisionId": f"OQ-1-{decision_id}-APPROVED-SUCCESSOR-v1",
        "requirementId": decision_id,
        "state": "APPROVED",
        "supersedes": {
            "path": LANDING_ZONE_AUTHORITY_PATH,
            "recordResult": "BLOCKED",
            "recordSha256": sha256(predecessor_content),
            "decisionObjectSha256": canonical_digest(predecessor_row),
        },
        "approvingAuthority": {
            "authorityId": LANDING_ZONE_APPROVING_AUTHORITY,
            "binding": binding(root, evaluated, LANDING_ZONE_APPROVAL_PATH),
        },
        "grantAuthorities": effective_grants,
    }
    if successor != expected:
        raise EntryAuthorityError(
            "V23_LANDING_ZONE_EVIDENCE_INVALID",
            f"{decision_id} approved successor object is missing, invented, or drifted",
        )
    return successor, binding(root, source, successor_path)


def validate_pass_evidence(
    root: Path,
    source: str,
    path: str,
    gate_id: str,
    evaluated: str,
    *,
    validation_time: datetime | None = None,
) -> tuple[dict[str, str], datetime]:
    """Validate one gate-specific, candidate-bound, provenance-complete PASS record."""

    code = f"V23_{gate_id}_EVIDENCE_INVALID"
    content = candidate_blob(root, source, path, f"V23_{gate_id}_EVIDENCE_MISSING")
    document = load_json(content, code)
    expected_keys = {
        "schemaVersion",
        "gateId",
        "gateAuthority",
        "evaluatedCommit",
        "evaluatedTree",
        "disposition",
        "sourceBindings",
        "command",
        "measurements",
        "assertionLedger",
        "result",
    }
    authorities = {"PRESERVATION": "FR-20/SM-C1", "PERFORMANCE": "SM-C2", "LANDING_ZONE": "OQ-1"}
    if (
        set(document) != expected_keys
        or document.get("schemaVersion") != "hexalith.conversations.story-7.1-entry-gate-result.v2"
        or document.get("gateId") != gate_id
        or document.get("gateAuthority") != authorities.get(gate_id)
        or document.get("evaluatedCommit") != evaluated
        or document.get("evaluatedTree") != commit_tree(root, evaluated)
        or document.get("result") != "PASS"
    ):
        raise EntryAuthorityError(code, "closed gate identity, authority, or candidate binding mismatch")
    disposition = document.get("disposition")
    if (
        not isinstance(disposition, dict)
        or set(disposition) != {"identity", "decidedAtUtc", "decision"}
        or disposition.get("identity") != TRUSTED_OWNER_IDENTITY
        or disposition.get("decision") != "PASS"
    ):
        raise EntryAuthorityError(code, "trusted disposition identity is missing")
    disposition_time = utc_instant(disposition.get("decidedAtUtc"), code)
    if validation_time is not None and disposition_time > validation_time:
        raise EntryAuthorityError(code, "gate disposition is later than validation")
    canonical_paths: dict[str, tuple[str, ...]] = {
        "PRESERVATION": (PRESERVATION_MANIFEST_PATH,),
        "PERFORMANCE": (PERFORMANCE_BASELINE_PATH,),
        "LANDING_ZONE": (
            LANDING_ZONE_AUTHORITY_PATH,
            LANDING_ZONE_APPROVAL_PATH,
            *LANDING_ZONE_GRANT_PATHS.values(),
            *LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values(),
            *LANDING_ZONE_SUCCESSOR_PATHS.values(),
        ),
    }
    expected_source_paths = canonical_paths.get(gate_id)
    if expected_source_paths is None:
        raise EntryAuthorityError(code, f"unsupported gate {gate_id!r}")
    declared_sources = document.get("sourceBindings")
    successor_paths = set(LANDING_ZONE_SUCCESSOR_PATHS.values()) | set(
        LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values()
    )
    observed_sources = [
        binding(root, source if source_path in successor_paths else evaluated, source_path)
        for source_path in expected_source_paths
    ]
    if declared_sources != observed_sources:
        raise EntryAuthorityError(code, "source inventory, digest, or object identity drift")
    command = document.get("command")
    if (
        not isinstance(command, dict)
        or set(command) != {"argv", "exitCode", "outputSha256", "outputUtf8"}
        or not isinstance(command.get("argv"), list)
        or not command["argv"]
        or any(not nonempty_string(value) for value in command["argv"])
        or command.get("exitCode") != 0
        or not isinstance(command.get("outputSha256"), str)
        or re.fullmatch(r"[0-9a-f]{64}", command["outputSha256"]) is None
        or not isinstance(command.get("outputUtf8"), str)
        or not command["outputUtf8"].endswith("\n")
        or sha256(command["outputUtf8"].encode("utf-8")) != command["outputSha256"]
    ):
        raise EntryAuthorityError(code, "command output bytes are absent, drifted, or nonpassing")
    output = load_json(command["outputUtf8"].encode("utf-8"), code)
    measurements = document.get("measurements")
    if not isinstance(measurements, dict):
        raise EntryAuthorityError(code, "measurements must be an object")
    required_ledger_rows: list[tuple[str, str]] = []
    if gate_id == "PRESERVATION":
        manifest = load_json(candidate_blob(root, evaluated, PRESERVATION_MANIFEST_PATH), code)
        denominator = manifest.get("testDenominator", {}).get("currentCandidate", {})
        expected_ids = denominator.get("testIds")
        expected_digest = nfc_list_digest(expected_ids) if isinstance(expected_ids, list) and expected_ids else None
        expected_categories = [
            {
                "category": row.get("category"),
                "requirementIds": row.get("requirementIds"),
                "testIds": row.get("testIds"),
            }
            for row in manifest.get("categoryMappings", [])
            if isinstance(row, dict)
        ]
        expected_output = {
            "gateId": gate_id,
            "testResults": [{"id": test_id, "state": "PASS"} for test_id in expected_ids or []],
            "categoryMappings": expected_categories,
        }
        expected_measurements = {
            "denominatorCount": len(expected_ids or []),
            "denominatorSha256": expected_digest,
            "evaluatedCount": len(expected_ids or []),
            "mismatchCount": 0,
        }
        if (
            not isinstance(expected_ids, list)
            or not expected_ids
            or len(set(expected_ids)) != len(expected_ids)
            or any(
                not isinstance(test_id, str)
                or not test_id
                or unicodedata.normalize("NFC", test_id) != test_id
                for test_id in expected_ids
            )
            or denominator.get("testIdsSha256") != expected_digest
            or tuple(row.get("category") for row in expected_categories) != PRESERVATION_CATEGORIES
            or output != expected_output
            or any(type(measurements.get(key)) is not int for key in ("denominatorCount", "evaluatedCount", "mismatchCount"))
            or measurements != expected_measurements
        ):
            raise EntryAuthorityError(code, "frozen preservation denominator was not enumerated and proven exactly")
        required_ledger_rows.extend(
            (f"PRESERVATION.TEST.{sha256(test_id.encode())}", observed_sources[0]["sha256"])
            for test_id in expected_ids
        )
        required_ledger_rows.extend(
            (f"PRESERVATION.CATEGORY.{row['category']}", observed_sources[0]["sha256"])
            for row in expected_categories
        )
    elif gate_id == "PERFORMANCE":
        baseline_content = candidate_blob(root, evaluated, PERFORMANCE_BASELINE_PATH)
        baseline = load_json(baseline_content, code)
        try:
            exact_baseline = json.loads(
                baseline_content.decode("utf-8", errors="strict"),
                object_pairs_hook=reject_duplicate_keys,
                parse_float=Decimal,
            )
            exact_output = json.loads(
                command["outputUtf8"],
                object_pairs_hook=reject_duplicate_keys,
                parse_float=Decimal,
            )
        except (UnicodeError, json.JSONDecodeError, ValueError) as error:
            raise EntryAuthorityError(code, f"performance decimal input is invalid: {error}") from error
        baseline_rows = baseline.get("rows")
        output_rows = output.get("hotPaths") if isinstance(output, dict) else None
        exact_baseline_rows = exact_baseline.get("rows") if isinstance(exact_baseline, dict) else None
        exact_output_rows = exact_output.get("hotPaths") if isinstance(exact_output, dict) else None
        if (
            set(output) != {"gateId", "hotPaths"}
            or output.get("gateId") != gate_id
            or not isinstance(baseline_rows, list)
            or not isinstance(output_rows, list)
            or not isinstance(exact_baseline_rows, list)
            or not isinstance(exact_output_rows, list)
        ):
            raise EntryAuthorityError(code, "performance output shape is invalid")
        baseline_by_id = {row.get("hotPathId"): row for row in baseline_rows if isinstance(row, dict)}
        exact_baseline_by_id = {
            row.get("hotPathId"): row for row in exact_baseline_rows if isinstance(row, dict)
        }
        if tuple(sorted(baseline_by_id)) != PERFORMANCE_HOT_PATHS or len(output_rows) != len(PERFORMANCE_HOT_PATHS):
            raise EntryAuthorityError(code, "frozen hot-path inventory is incomplete")
        summaries: list[dict[str, Any]] = []
        for hot_path_id, row, exact_row in zip(
            PERFORMANCE_HOT_PATHS,
            output_rows,
            exact_output_rows,
            strict=True,
        ):
            baseline_samples = baseline_by_id[hot_path_id].get("rawMicrosecondsPerOperation")
            exact_baseline_samples = exact_baseline_by_id.get(hot_path_id, {}).get(
                "rawMicrosecondsPerOperation"
            )
            if (
                not isinstance(row, dict)
                or set(row) != {"hotPathId", "baselineSamples", "candidateSamples"}
                or row.get("hotPathId") != hot_path_id
                or row.get("baselineSamples") != baseline_samples
                or not isinstance(exact_row, dict)
                or exact_row.get("hotPathId") != hot_path_id
                or exact_row.get("baselineSamples") != exact_baseline_samples
            ):
                raise EntryAuthorityError(code, f"{hot_path_id} sample identity drift")
            candidate_samples = row.get("candidateSamples")
            exact_candidate_samples = exact_row.get("candidateSamples")
            for samples in (baseline_samples, candidate_samples):
                if (
                    not isinstance(samples, list)
                    or len(samples) < PERFORMANCE_MIN_SAMPLE_COUNT
                    or any(
                        not isinstance(value, (int, float))
                        or isinstance(value, bool)
                        or not math.isfinite(value)
                        or value < 0
                        for value in samples
                    )
                ):
                    raise EntryAuthorityError(code, f"{hot_path_id} samples are incomplete or non-finite")
            for samples in (exact_baseline_samples, exact_candidate_samples):
                if (
                    not isinstance(samples, list)
                    or len(samples) < PERFORMANCE_MIN_SAMPLE_COUNT
                    or any(
                        isinstance(value, bool)
                        or not isinstance(value, (int, Decimal))
                        or not Decimal(value).is_finite()
                        or Decimal(value) < 0
                        for value in samples
                    )
                ):
                    raise EntryAuthorityError(code, f"{hot_path_id} exact decimal samples are invalid")
            baseline_p95 = sorted(float(value) for value in baseline_samples)[math.ceil(0.95 * len(baseline_samples)) - 1]
            candidate_p95 = sorted(float(value) for value in candidate_samples)[math.ceil(0.95 * len(candidate_samples)) - 1]
            exact_baseline_p95 = sorted(Decimal(value) for value in exact_baseline_samples)[
                math.ceil(0.95 * len(exact_baseline_samples)) - 1
            ]
            exact_candidate_p95 = sorted(Decimal(value) for value in exact_candidate_samples)[
                math.ceil(0.95 * len(exact_candidate_samples)) - 1
            ]
            if exact_baseline_p95 <= 0:
                raise EntryAuthorityError(code, f"{hot_path_id} baseline P95 is nonpositive")
            exact_regression = (
                (exact_candidate_p95 - exact_baseline_p95) * Decimal(100) / exact_baseline_p95
            )
            if exact_regression > Decimal(PERFORMANCE_MAX_REGRESSION_PERCENT):
                raise EntryAuthorityError(code, f"{hot_path_id} regression {exact_regression}% exceeds 5%")
            regression = float(exact_regression)
            summaries.append(
                {
                    "hotPathId": hot_path_id,
                    "sampleCount": len(candidate_samples),
                    "baselineP95Microseconds": baseline_p95,
                    "candidateP95Microseconds": candidate_p95,
                    "observedRegressionPercent": regression,
                }
            )
            required_ledger_rows.extend(
                (
                    (f"PERFORMANCE.{hot_path_id}.BASELINE", observed_sources[0]["sha256"]),
                    (f"PERFORMANCE.{hot_path_id}.CANDIDATE", observed_sources[0]["sha256"]),
                )
            )
        expected_measurements = {
            "minimumSampleCount": PERFORMANCE_MIN_SAMPLE_COUNT,
            "maximumRegressionPercent": PERFORMANCE_MAX_REGRESSION_PERCENT,
            "hotPaths": summaries,
        }
        declared_summaries = measurements.get("hotPaths")
        numeric_summary_valid = (
            type(measurements.get("minimumSampleCount")) is int
            and isinstance(measurements.get("maximumRegressionPercent"), (int, float))
            and not isinstance(measurements.get("maximumRegressionPercent"), bool)
            and math.isfinite(measurements["maximumRegressionPercent"])
            and isinstance(declared_summaries, list)
            and all(
            isinstance(row, dict)
            and type(row.get("sampleCount")) is int
            and all(
                isinstance(row.get(key), (int, float))
                and not isinstance(row.get(key), bool)
                and math.isfinite(row[key])
                for key in (
                    "baselineP95Microseconds",
                    "candidateP95Microseconds",
                    "observedRegressionPercent",
                )
            )
            for row in declared_summaries
            )
        )
        if not numeric_summary_valid or measurements != expected_measurements:
            raise EntryAuthorityError(code, "performance summaries were not recomputed from raw samples")
    elif gate_id == "LANDING_ZONE":
        predecessor_content = candidate_blob(root, evaluated, LANDING_ZONE_AUTHORITY_PATH)
        authority = load_json(predecessor_content, code)
        if authority.get("result") != "BLOCKED":
            raise EntryAuthorityError(code, "canonical OQ-1 predecessor must remain BLOCKED")
        expected_decisions: list[dict[str, Any]] = []
        for decision_id in LANDING_ZONE_DECISION_IDS:
            successor, successor_binding = validate_landing_zone_successor(
                root,
                source,
                evaluated,
                authority,
                predecessor_content,
                decision_id,
                disposition_time,
                validation_time,
            )
            expected_decisions.append(
                {
                    "decisionId": successor["decisionId"],
                    "requirementId": decision_id,
                    "approvingAuthority": successor["approvingAuthority"]["authorityId"],
                    "predecessorResult": successor["supersedes"]["recordResult"],
                    "predecessorDecisionObjectSha256": successor["supersedes"]["decisionObjectSha256"],
                    "decisionObjectSha256": successor_binding["sha256"],
                    "state": "APPROVED",
                }
            )
            required_ledger_rows.append(
                (f"LANDING_ZONE.DECISION.{decision_id}", successor_binding["sha256"])
            )
        expected_output = {
            "gateId": gate_id,
            "predecessorResult": "BLOCKED",
            "decisions": expected_decisions,
            "unresolvedDecisionIds": [],
        }
        expected_measurements = {
            "decisionsRequired": len(LANDING_ZONE_DECISION_IDS),
            "decisionsApproved": len(LANDING_ZONE_DECISION_IDS),
            "unresolvedCount": 0,
        }
        if (
            output != expected_output
            or any(type(measurements.get(key)) is not int for key in ("decisionsRequired", "decisionsApproved", "unresolvedCount"))
            or measurements != expected_measurements
        ):
            raise EntryAuthorityError(code, "landing-zone decision identities, authorities, or hashes are incomplete")
    ledger = document.get("assertionLedger")
    expected_ledger = [
        {
            "id": f"{gate_id}.SOURCE.{sha256(row['path'].encode())}",
            "subject": authorities[gate_id],
            "state": "PASS",
            "sourceSha256": row["sha256"],
            "outputSha256": command["outputSha256"],
        }
        for row in observed_sources
    ] + [
        {
            "id": ledger_id,
            "subject": authorities[gate_id],
            "state": "PASS",
            "sourceSha256": source_sha256,
            "outputSha256": command["outputSha256"],
        }
        for ledger_id, source_sha256 in required_ledger_rows
    ]
    if (
        ledger != expected_ledger
        or len({row["id"] for row in expected_ledger}) != len(expected_ledger)
    ):
        raise EntryAuthorityError(code, "gate assertion ledger does not equal the canonical source and measurement inventory")
    return binding(root, source, path), disposition_time


def expected_v23_suffix(authority_digest: str, request_digest: str) -> bytes:
    """Render the only accepted append-only V23 architecture marker."""

    return f"""
<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN version={ARCHITECTURE_VERSION} supersedes=conversations-architecture-2026-09-20-v22 authority-record=v23-story-7.1-entry-authority-v1.json authority-record-sha256={authority_digest} request-record=v23-story-7.1-entry-candidate-v1.json request-record-sha256={request_digest} hold=EXECUTION_ALLOWED -->

# Architecture Execution Authority Overlay V23 — Story 7.1 Entry

The separately authenticated V23 decision satisfies the closed AD-4 entry
transition for Story 7.1 only. It binds the exact V22 history, protected merge,
request, current gate evidence, root gitlinks, and two-path authority
publication. Story 7.2 remains locked; release and push remain unauthorized.

Any missing or drifted request, gate, signature, marker, path, mode, history, or
gitlink fails closed to `ACTIVE`. Historical V22 remains immutable and retains
its original `executionAllowed: false` result.

<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END version={ARCHITECTURE_VERSION} authority-record=v23-story-7.1-entry-authority-v1.json authority-record-sha256={authority_digest} request-record=v23-story-7.1-entry-candidate-v1.json request-record-sha256={request_digest} hold=EXECUTION_ALLOWED -->
""".encode("utf-8")


def render_authority(
    root: Path,
    source_revision: str,
    *,
    owner_identity: str,
    decided_at_utc: str,
    rationale: str,
    preservation_evidence: str,
    performance_evidence: str,
    landing_zone_evidence: str,
    clock: Callable[[], datetime] | None = None,
) -> tuple[dict[str, Any], bytes]:
    """Render an owner-authored authority and its exact append-only marker bytes."""

    validate_owner_fields(owner_identity, decided_at_utc, rationale)
    owner_decision_time = utc_instant(decided_at_utc, "V23_OWNER_TIME_INVALID")
    validation_time = trusted_validation_time(clock)
    if owner_decision_time > validation_time:
        raise EntryAuthorityError("V23_OWNER_TIME_INVALID", "owner decision is later than validation")
    source = resolve_commit(root, source_revision, "V23_AUTHORITY_SOURCE_UNAVAILABLE")
    request, request_publication, request_content, evidence_scope_anchor = validate_current_request(root, source)
    require_ancestor(root, evidence_scope_anchor, source, "V23_GATE_EVIDENCE_GRAPH_DRIFT")
    if run_git(root, "cat-file", "-e", f"{source}:{AUTHORITY_PATH}", allowed=(0, 1, 128)).returncode == 0:
        raise EntryAuthorityError("V23_AUTHORITY_ALREADY_COMMITTED", f"{AUTHORITY_PATH}@{source}")
    operational = candidate_blob(root, source, OPERATIONAL_ENVELOPE_PATH, "V23_OPERATIONAL_ENVELOPE_MISSING")
    validate_operational_envelope(
        operational,
        owner_identity.strip(),
        root=root,
        source=source,
        decision_time=owner_decision_time,
        validation_time=validation_time,
    )
    supplied_evidence_paths = {
        "preservation": safe_path(preservation_evidence),
        "performance": safe_path(performance_evidence),
        "landingZone": safe_path(landing_zone_evidence),
    }
    if supplied_evidence_paths != GATE_EVIDENCE_PATHS:
        raise EntryAuthorityError(
            "V23_GATE_EVIDENCE_PATH_INVALID",
            f"expected={GATE_EVIDENCE_PATHS!r}; observed={supplied_evidence_paths!r}",
        )
    evidence_paths = {
        "story62": "_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md",
        "checkpoint": "_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json",
        "ir0": "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md",
        "workflow": "_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json",
        "operational": OPERATIONAL_ENVELOPE_PATH,
        **GATE_EVIDENCE_PATHS,
        "sprint": "_bmad-output/implementation-artifacts/sprint-status.yaml",
    }
    evidence_scope = tuple(
        sorted(
            (
                OPERATIONAL_ENVELOPE_PATH,
                RECOVERY_RUNBOOK_PATH,
                evidence_paths["preservation"],
                evidence_paths["performance"],
                evidence_paths["landingZone"],
                *LANDING_ZONE_SUCCESSOR_PATHS.values(),
                *LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values(),
            )
        )
    )
    observed_evidence_scope = changed_paths(root, evidence_scope_anchor, source)
    if observed_evidence_scope != evidence_scope:
        missing = sorted(set(evidence_scope) - set(observed_evidence_scope))
        unexpected = sorted(set(observed_evidence_scope) - set(evidence_scope))
        raise EntryAuthorityError(
            "V23_GATE_EVIDENCE_SCOPE_DRIFT",
            f"missing={missing!r}; unexpected={unexpected!r}",
        )
    if changed_gitlinks(root, evidence_scope_anchor, source):
        raise EntryAuthorityError(
            "V23_GATE_EVIDENCE_GITLINK_DRIFT",
            repr(changed_gitlinks(root, evidence_scope_anchor, source)),
        )
    for gate_id, path in (
        ("PRESERVATION", evidence_paths["preservation"]),
        ("PERFORMANCE", evidence_paths["performance"]),
        ("LANDING_ZONE", evidence_paths["landingZone"]),
    ):
        _evidence_binding, disposition_time = validate_pass_evidence(
            root,
            source,
            path,
            gate_id,
            evidence_scope_anchor,
            validation_time=validation_time,
        )
        if disposition_time > owner_decision_time:
            raise EntryAuthorityError(
                f"V23_{gate_id}_EVIDENCE_INVALID",
                "gate disposition is later than owner decision",
            )
    prefix = candidate_blob(root, source, ARCHITECTURE_PATH, "V23_ARCHITECTURE_UNAVAILABLE")
    gates = disposition_gates(evidence_paths, owner_identity)
    source_bindings = [binding(root, source, path) for path in SOURCE_PATHS]
    if source_bindings != request["sourceBindings"]:
        raise EntryAuthorityError(
            "V23_SOURCE_DESCENDANT_DRIFT",
            "request-bound historical sources changed before the owner decision",
        )
    for evidence_path in evidence_scope:
        row = binding(root, source, evidence_path)
        if row not in source_bindings:
            source_bindings.append(row)
    document: dict[str, Any] = {
        "schemaVersion": SCHEMA_VERSION,
        "recordType": "AUTHORITY",
        "authorityId": AUTHORITY_ID,
        "request": {"path": REQUEST_PATH, "publicationCommit": request_publication, "sha256": sha256(request_content)},
        "publication": {
            "sourceCommit": source,
            "sourceTree": commit_tree(root, source),
            "exactChangedPaths": list(AUTHORITY_PATHS),
            "requiredMode": "100644",
            "changedGitlinks": [],
        },
        "ownerDecision": {
            "identity": owner_identity.strip(),
            "decidedAtUtc": decided_at_utc,
            "rationale": rationale.strip(),
            "decision": "EXECUTION_ALLOWED",
        },
        "gateDispositions": gates,
        "sourceBindings": source_bindings,
        "rootGitlinks": root_gitlinks(root, source),
        "architecture": {
            "path": ARCHITECTURE_PATH,
            "prefixBytes": len(prefix),
            "prefixSha256": sha256(prefix),
            "version": ARCHITECTURE_VERSION,
            "requestSha256": sha256(request_content),
        },
        "resultSemantics": result_semantics(),
        "assertionLedger": [
            {
                "id": f"V23.AUTHORITY.{index:02d}",
                "subject": row["id"],
                "state": "PASS",
                "detail": row["detail"],
            }
            for index, row in enumerate(gates, start=1)
        ],
        "blockers": [],
        "result": "PASS",
        "implementationHold": "EXECUTION_ALLOWED",
        "ownerApprovalClaimed": True,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": True,
        "storyExecution": {"7.1": True, "7.2": False, "7.3": False, "7.4": False},
    }
    validate_authority_controls(document)
    validate_schema(root, document, schema_commit=source)
    marker = expected_v23_suffix(sha256(json_bytes(document)), sha256(request_content))
    return document, prefix + marker


def verify_publication_signature(
    root: Path,
    publication: str,
    owner_identity: str,
    *,
    trusted_owner_identity: str = TRUSTED_OWNER_IDENTITY,
    trusted_principal: str = TRUSTED_SSH_PRINCIPAL,
    trusted_public_key: str = TRUSTED_SSH_PUBLIC_KEY,
    trusted_fingerprint: str = TRUSTED_SSH_FINGERPRINT,
    ssh_keygen_path: str | None = None,
    revoked_public_keys: Sequence[str] = (),
    allowed_signer_principal: str | None = None,
) -> dict[str, str]:
    """Verify one commit with only explicit source-pinned SSH trust material."""

    if owner_identity.strip() != trusted_owner_identity:
        raise EntryAuthorityError("V23_OWNER_IDENTITY_INVALID", repr(owner_identity))
    try:
        author = run_git(root, "show", "-s", "--format=%an <%ae>", publication).stdout.decode("utf-8").strip()
    except UnicodeError as error:
        raise EntryAuthorityError("V23_PUBLICATION_AUTHOR_INVALID", str(error), "BLOCKED") from error
    if author != trusted_owner_identity:
        raise EntryAuthorityError("V23_PUBLICATION_AUTHOR_INVALID", f"expected={trusted_owner_identity!r}; observed={author!r}")
    executable = ssh_keygen_path or SSH_KEYGEN_EXECUTABLE
    executable_path = Path(executable)
    try:
        resolved_executable = executable_path.resolve(strict=True)
    except OSError as error:
        raise EntryAuthorityError("V23_TRUST_TOOL_UNAVAILABLE", str(error), "BLOCKED") from error
    if not resolved_executable.is_file() or not os.access(resolved_executable, os.X_OK):
        raise EntryAuthorityError("V23_TRUST_TOOL_UNAVAILABLE", str(resolved_executable), "BLOCKED")
    try:
        with tempfile.TemporaryDirectory(prefix="v23-owner-trust-") as temporary_name:
            directory = Path(temporary_name)
            allowed_signers = directory / "allowed_signers"
            revoked_signers = directory / "revoked_signers"
            allowed_signers.write_text(
                f"{allowed_signer_principal or trusted_principal} {trusted_public_key}\n",
                encoding="utf-8",
            )
            revoked_signers.write_text("".join(f"{key}\n" for key in revoked_public_keys), encoding="utf-8")
            common = (
                "-c",
                "gpg.format=ssh",
                "-c",
                f"gpg.ssh.allowedSignersFile={allowed_signers}",
                "-c",
                f"gpg.ssh.revocationFile={revoked_signers}",
                "-c",
                f"gpg.ssh.program={resolved_executable}",
                "-c",
                "gpg.minTrustLevel=undefined",
            )
            verification = run_git(root, *common, "verify-commit", "--raw", publication, allowed=tuple(range(256)))
            if verification.returncode != 0:
                detail = verification.stderr.decode("utf-8", errors="replace").strip() or "signature verification failed"
                raise EntryAuthorityError("V23_PUBLICATION_SIGNATURE_INVALID", detail)
            signature = run_git(
                root,
                *common,
                "show",
                "-s",
                "--format=%G?%x00%GS%x00%GF",
                publication,
            ).stdout.decode("utf-8", errors="strict").rstrip("\n")
    except OSError as error:
        raise EntryAuthorityError("V23_TRUST_ANCHOR_UNAVAILABLE", str(error), "BLOCKED") from error
    parts = signature.split("\0")
    if len(parts) != 3:
        raise EntryAuthorityError("V23_PUBLICATION_SIGNER_INVALID", repr(signature))
    status, principal, fingerprint = parts
    if status != "G" or principal != trusted_principal or fingerprint != trusted_fingerprint:
        raise EntryAuthorityError(
            "V23_PUBLICATION_SIGNER_INVALID",
            f"status={status!r}; principal={principal!r}; fingerprint={fingerprint!r}",
        )
    return {"status": status, "principal": principal, "fingerprint": fingerprint, "authorIdentity": author}


def result_document(
    result: str,
    observed: dict[str, Any],
    ledger: list[dict[str, str]],
    blockers: list[dict[str, Any]],
) -> dict[str, Any]:
    """Build one closed current-authority result."""

    passed = result == "PASS"
    return {
        "schemaVersion": RESULT_SCHEMA_VERSION,
        "result": result,
        "exitCode": {"PASS": 0, "FAIL": 1, "BLOCKED": 2}[result],
        "effectiveHold": "EXECUTION_ALLOWED" if passed else "ACTIVE",
        "implementationHold": "EXECUTION_ALLOWED" if passed else "ACTIVE",
        "observed": observed,
        "assertionLedger": ledger,
        "blockers": blockers,
        "ownerApprovalClaimed": passed,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": passed,
        "storyExecution": {"7.1": passed, "7.2": False, "7.3": False, "7.4": False},
    }


def fail_result(code: str, detail: str, state: str = "BLOCKED") -> dict[str, Any]:
    """Build a nonvacuous fail-closed result for early failures."""

    ledger = [{"id": code, "subject": "v23-entry-authority", "state": state, "detail": detail}]
    return result_document(state, {}, ledger, [{"code": code, "detail": detail, "assertionIndex": 0}])


def resolve_published_authority(
    root: Path,
    revision: str,
    *,
    v22_resolver: Callable[[Path, str], dict[str, Any]] | None = None,
    signature_verifier: Callable[[Path, str, str], dict[str, str]] = verify_publication_signature,
    clock: Callable[[], datetime] | None = None,
) -> dict[str, Any]:
    """Validate the separately published V23 authority from raw committed objects."""

    ledger: list[dict[str, str]] = []
    observed: dict[str, Any] = {}
    publication: str | None = None
    try:
        validation_time = trusted_validation_time(clock)
        candidate = resolve_commit(root, revision, "V23_CANDIDATE_UNAVAILABLE")
        observed["candidateCommit"] = candidate
        observed["candidateTree"] = commit_tree(root, candidate)
        publication = locate_publication(root, candidate, AUTHORITY_PATH, "V23_AUTHORITY_PUBLICATION_MISSING")
        observed["authorityPublication"] = publication
        if candidate != publication:
            raise EntryAuthorityError("V23_DESCENDANT_REQUIRES_SUCCESSOR_STATE", f"publication={publication}; candidate={candidate}")
        parents = commit_parents(root, publication)
        if len(parents) != 1:
            raise EntryAuthorityError("V23_AUTHORITY_GRAPH_DRIFT", repr(parents), "BLOCKED")
        source = parents[0]
        observed["sourceCommit"] = source
        paths = changed_paths(root, source, publication)
        observed["changedPaths"] = list(paths)
        if paths != AUTHORITY_PATHS:
            missing = sorted(set(AUTHORITY_PATHS) - set(paths))
            unexpected = sorted(set(paths) - set(AUTHORITY_PATHS))
            raise EntryAuthorityError("V23_AUTHORITY_SCOPE_DRIFT", f"missing={missing!r}; unexpected={unexpected!r}")
        if changed_gitlinks(root, source, publication):
            raise EntryAuthorityError("V23_AUTHORITY_GITLINK_DRIFT", repr(changed_gitlinks(root, source, publication)))
        for path in AUTHORITY_PATHS:
            mode, kind, _object_id = tree_record(root, publication, path)
            if (mode, kind) != ("100644", "blob"):
                raise EntryAuthorityError("V23_AUTHORITY_MODE_DRIFT", f"{path}: {mode} {kind}")
        content = candidate_blob(root, publication, AUTHORITY_PATH, "V23_AUTHORITY_MISSING")
        authority = load_json(content, "V23_AUTHORITY_INVALID")
        validate_schema(root, authority, schema_commit=publication)
        validate_authority_controls(authority)
        declared_publication = authority["publication"]
        if (
            declared_publication["sourceCommit"] != source
            or declared_publication["sourceTree"] != commit_tree(root, source)
            or tuple(declared_publication["exactChangedPaths"]) != AUTHORITY_PATHS
        ):
            raise EntryAuthorityError("V23_AUTHORITY_PUBLICATION_DRIFT", repr(declared_publication))
        request, request_publication, request_content, evidence_scope_anchor = validate_current_request(
            root,
            source,
            v22_resolver=v22_resolver,
        )
        if authority["request"] != {
            "path": REQUEST_PATH,
            "publicationCommit": request_publication,
            "sha256": sha256(request_content),
        }:
            raise EntryAuthorityError("V23_AUTHORITY_REQUEST_DRIFT", repr(authority["request"]))
        if authority["rootGitlinks"] != root_gitlinks(root, source) or root_gitlinks(root, publication) != authority["rootGitlinks"]:
            raise EntryAuthorityError("V23_ROOT_GITLINK_DRIFT", "source, publication, and authority gitlinks differ")
        gate_rows = authority["gateDispositions"]
        expected_gates = disposition_gates(
            {
                "story62": "_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md",
                "checkpoint": "_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json",
                "ir0": "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md",
                "workflow": "_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json",
                "operational": OPERATIONAL_ENVELOPE_PATH,
                "preservation": next(row["evidence"] for row in gate_rows if row["id"] == "FR-20-SM-C1"),
                "performance": next(row["evidence"] for row in gate_rows if row["id"] == "SM-C2"),
                "landingZone": next(row["evidence"] for row in gate_rows if row["id"] == "OQ-1"),
                "sprint": "_bmad-output/implementation-artifacts/sprint-status.yaml",
            },
            authority["ownerDecision"]["identity"],
        )
        if gate_rows != expected_gates:
            raise EntryAuthorityError("V23_GATE_DISPOSITION_DRIFT", repr(gate_rows))
        evidence_scope = tuple(
            sorted(
                (
                    OPERATIONAL_ENVELOPE_PATH,
                    RECOVERY_RUNBOOK_PATH,
                    next(row["evidence"] for row in gate_rows if row["id"] == "FR-20-SM-C1"),
                    next(row["evidence"] for row in gate_rows if row["id"] == "SM-C2"),
                    next(row["evidence"] for row in gate_rows if row["id"] == "OQ-1"),
                    *LANDING_ZONE_SUCCESSOR_PATHS.values(),
                    *LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values(),
                )
            )
        )
        observed_evidence_scope = changed_paths(root, evidence_scope_anchor, source)
        if observed_evidence_scope != evidence_scope:
            missing = sorted(set(evidence_scope) - set(observed_evidence_scope))
            unexpected = sorted(set(observed_evidence_scope) - set(evidence_scope))
            raise EntryAuthorityError(
                "V23_GATE_EVIDENCE_SCOPE_DRIFT",
                f"missing={missing!r}; unexpected={unexpected!r}",
            )
        if changed_gitlinks(root, evidence_scope_anchor, source):
            raise EntryAuthorityError(
                "V23_GATE_EVIDENCE_GITLINK_DRIFT",
                repr(changed_gitlinks(root, evidence_scope_anchor, source)),
            )
        owner = authority["ownerDecision"]
        validate_owner_fields(owner["identity"], owner["decidedAtUtc"], owner["rationale"])
        owner_decision_time = utc_instant(owner["decidedAtUtc"], "V23_OWNER_TIME_INVALID")
        if owner_decision_time > validation_time:
            raise EntryAuthorityError("V23_OWNER_TIME_INVALID", "owner decision is later than validation")
        require_ancestor(root, evidence_scope_anchor, source, "V23_GATE_EVIDENCE_GRAPH_DRIFT")
        validate_operational_envelope(
            candidate_blob(root, source, OPERATIONAL_ENVELOPE_PATH),
            owner["identity"],
            root=root,
            source=source,
            decision_time=owner_decision_time,
            validation_time=validation_time,
        )
        validate_workflow_route(root, source)
        evidence_gate_ids = {
            "FR-20-SM-C1": "PRESERVATION",
            "SM-C2": "PERFORMANCE",
            "OQ-1": "LANDING_ZONE",
        }
        for gate_id, evidence_gate_id in evidence_gate_ids.items():
            evidence_path = next(row["evidence"] for row in gate_rows if row["id"] == gate_id)
            _evidence_binding, disposition_time = validate_pass_evidence(
                root,
                source,
                evidence_path,
                evidence_gate_id,
                evidence_scope_anchor,
                validation_time=validation_time,
            )
            if disposition_time > owner_decision_time:
                raise EntryAuthorityError(
                    f"V23_{evidence_gate_id}_EVIDENCE_INVALID",
                    "gate disposition is later than owner decision",
                )
        fixed_source_bindings = [binding(root, source, path) for path in SOURCE_PATHS]
        if fixed_source_bindings != request["sourceBindings"]:
            raise EntryAuthorityError(
                "V23_SOURCE_DESCENDANT_DRIFT",
                "request-bound historical sources changed before the owner decision",
            )
        expected_source_bindings = list(fixed_source_bindings)
        for evidence_path in evidence_scope:
            row = binding(root, source, evidence_path)
            if row not in expected_source_bindings:
                expected_source_bindings.append(row)
        if authority["sourceBindings"] != expected_source_bindings:
            raise EntryAuthorityError(
                "V23_AUTHORITY_BINDING_INVENTORY_DRIFT",
                "authority source-binding identities do not equal the frozen input inventory",
            )
        signature = signature_verifier(root, publication, owner["identity"])
        observed["ownerSignature"] = signature
        prefix = candidate_blob(root, source, ARCHITECTURE_PATH)
        architecture = authority["architecture"]
        if (
            architecture["prefixBytes"] != len(prefix)
            or architecture["prefixSha256"] != sha256(prefix)
            or architecture["requestSha256"] != sha256(request_content)
        ):
            raise EntryAuthorityError("V23_ARCHITECTURE_BINDING_DRIFT", repr(architecture))
        expected_architecture = prefix + expected_v23_suffix(sha256(content), sha256(request_content))
        if candidate_blob(root, publication, ARCHITECTURE_PATH) != expected_architecture:
            raise EntryAuthorityError("V23_MARKER_DRIFT", "architecture is not the exact prefix plus V23 marker")
        expected_ledger = [
            {
                "id": f"V23.AUTHORITY.{index:02d}",
                "subject": row["id"],
                "state": "PASS",
                "detail": row["detail"],
            }
            for index, row in enumerate(gate_rows, start=1)
        ]
        if authority["assertionLedger"] != expected_ledger or authority["blockers"]:
            raise EntryAuthorityError("V23_AUTHORITY_RESULT_DRIFT", "authority ledger or blocker mismatch")
        ledger.extend(expected_ledger)
        ledger.append(
            {
                "id": "V23.AUTHORITY.SIGNATURE",
                "subject": "trusted-owner-publication-signature",
                "state": "PASS",
                "detail": "the two-path authority publication is signed by the pinned owner key",
            }
        )
        document = result_document("PASS", observed, ledger, [])
        validate_schema(root, document, schema_commit=publication)
        return document
    except EntryAuthorityError as error:
        ledger = [
            {
                "id": error.code,
                "subject": "v23-entry-authority",
                "state": error.state,
                "detail": error.detail,
            }
        ]
        document = result_document(
            error.state,
            observed,
            ledger,
            [{"code": error.code, "detail": error.detail, "assertionIndex": 0}],
        )
        if publication is not None:
            try:
                validate_schema(root, document, schema_commit=publication)
            except EntryAuthorityError:
                # Preserve the originating blocker. Schema availability is evidence, not authority
                # to replace the stable failure that already forced execution false.
                pass
        return document
    except Exception as error:
        return fail_result("V23_EVIDENCE_MALFORMED", str(error), "BLOCKED")


def open_parent_directory(root: Path, relative_path: str) -> tuple[int, str]:
    """Open a contained parent directory without following any path-component symlink."""

    parts = PurePosixPath(safe_path(relative_path)).parts
    flags = os.O_RDONLY | os.O_DIRECTORY | getattr(os, "O_NOFOLLOW", 0)
    try:
        descriptor = os.open(root.resolve(strict=True), flags)
        for part in parts[:-1]:
            child = os.open(part, flags, dir_fd=descriptor)
            os.close(descriptor)
            descriptor = child
        return descriptor, parts[-1]
    except OSError as error:
        try:
            os.close(descriptor)
        except (OSError, UnboundLocalError):
            pass
        raise EntryAuthorityError("V23_WRITE_PATH_INVALID", f"{relative_path}: {error}", "BLOCKED") from error


def acquire_bounded_lock(
    descriptor: int,
    *,
    code: str = "V23_PUBLICATION_LOCK_TIMEOUT",
    timeout_seconds: float = PUBLICATION_LOCK_TIMEOUT_SECONDS,
) -> None:
    """Acquire one advisory exclusive lock without allowing an unbounded publisher hang."""

    deadline = time.monotonic() + timeout_seconds
    while True:
        try:
            fcntl.flock(descriptor, fcntl.LOCK_EX | fcntl.LOCK_NB)
            return
        except BlockingIOError as error:
            if time.monotonic() >= deadline:
                raise EntryAuthorityError(code, "bounded publication lock acquisition timed out", "BLOCKED") from error
            time.sleep(0.01)


def rename_no_replace(parent_descriptor: int, source: str, destination: str) -> None:
    """Atomically rename one sibling path without replacing a concurrent destination."""

    renameat2 = getattr(ctypes.CDLL(None, use_errno=True), "renameat2", None)
    if renameat2 is None:
        raise EntryAuthorityError(
            "V23_NO_REPLACE_UNAVAILABLE",
            "renameat2(RENAME_NOREPLACE) is unavailable",
            "BLOCKED",
        )
    renameat2.argtypes = (
        ctypes.c_int,
        ctypes.c_char_p,
        ctypes.c_int,
        ctypes.c_char_p,
        ctypes.c_uint,
    )
    renameat2.restype = ctypes.c_int
    result = renameat2(
        parent_descriptor,
        os.fsencode(source),
        parent_descriptor,
        os.fsencode(destination),
        1,
    )
    if result != 0:
        observed_errno = ctypes.get_errno()
        if observed_errno == errno.EEXIST:
            raise FileExistsError(observed_errno, os.strerror(observed_errno), destination)
        raise OSError(observed_errno, os.strerror(observed_errno), source)


def read_regular_worktree_file(root: Path, relative_path: str) -> tuple[bytes, tuple[int, int]]:
    """Read one contained regular worktree file through no-follow descriptors."""

    parent_descriptor, name = open_parent_directory(root, relative_path)
    descriptor = -1
    try:
        descriptor = os.open(
            name,
            os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0) | getattr(os, "O_NONBLOCK", 0),
            dir_fd=parent_descriptor,
        )
        metadata = os.fstat(descriptor)
        if (
            not stat.S_ISREG(metadata.st_mode)
            or stat.S_IMODE(metadata.st_mode) != 0o644
            or metadata.st_nlink != 1
        ):
            raise EntryAuthorityError(
                "V23_WRITE_PATH_INVALID",
                f"{relative_path}: expected a mode-100644 single-link regular file",
                "BLOCKED",
            )
        with os.fdopen(descriptor, "rb", closefd=False) as stream:
            content = stream.read()
        final_metadata = os.fstat(descriptor)
        named_metadata = os.stat(name, dir_fd=parent_descriptor, follow_symlinks=False)
        if (
            (final_metadata.st_dev, final_metadata.st_ino) != (metadata.st_dev, metadata.st_ino)
            or (named_metadata.st_dev, named_metadata.st_ino) != (metadata.st_dev, metadata.st_ino)
            or not stat.S_ISREG(final_metadata.st_mode)
            or not stat.S_ISREG(named_metadata.st_mode)
            or stat.S_IMODE(final_metadata.st_mode) != 0o644
            or stat.S_IMODE(named_metadata.st_mode) != 0o644
            or final_metadata.st_nlink != 1
            or named_metadata.st_nlink != 1
            or final_metadata.st_size != metadata.st_size
            or final_metadata.st_mtime_ns != metadata.st_mtime_ns
            or final_metadata.st_ctime_ns != metadata.st_ctime_ns
        ):
            raise EntryAuthorityError(
                "V23_WRITE_PATH_INVALID",
                f"{relative_path}: mode, inode, link count, or bytes changed during read",
                "BLOCKED",
            )
        return content, (final_metadata.st_dev, final_metadata.st_ino)
    except OSError as error:
        raise EntryAuthorityError("V23_WRITE_PATH_INVALID", f"{relative_path}: {error}", "BLOCKED") from error
    finally:
        close_errors: list[OSError] = []
        for open_descriptor in (descriptor, parent_descriptor):
            if open_descriptor >= 0:
                try:
                    os.close(open_descriptor)
                except OSError as error:
                    close_errors.append(error)
        if close_errors:
            detail = f"{relative_path}: descriptor close failed: {close_errors!r}"
            active_error = sys.exception()
            if isinstance(active_error, EntryAuthorityError):
                active_error.detail = f"{active_error.detail}; {detail}"
                active_error.args = (f"{active_error.code}: {active_error.detail}",)
            elif active_error is None:
                raise EntryAuthorityError("V23_WRITE_PATH_INVALID", detail, "BLOCKED")
            else:
                raise EntryAuthorityError("V23_WRITE_PATH_INVALID", f"{active_error}; {detail}", "BLOCKED") from active_error


def revalidate_owned_file(
    root: Path,
    relative_path: str,
    expected_content: bytes,
    expected_identity: tuple[int, int] | None,
) -> tuple[int, int]:
    """Require a named regular file to retain its final bytes and optional inode identity."""

    content, identity = read_regular_worktree_file(root, relative_path)
    if content != expected_content or (expected_identity is not None and identity != expected_identity):
        raise EntryAuthorityError(
            "V23_PUBLICATION_FINAL_IDENTITY_DRIFT",
            f"{relative_path}: final inode or bytes changed",
            "BLOCKED",
        )
    return identity


def remove_owned_file(
    root: Path,
    relative_path: str,
    expected_content: bytes,
    expected_identity: tuple[int, int],
) -> str:
    """Quarantine one created path without truncating or unlinking its inode."""

    parent_descriptor, name = open_parent_directory(root, relative_path)
    descriptor = -1
    quarantine = f".{name}.rollback.{os.getpid()}.{secrets.token_hex(8)}"
    quarantine_path = str(PurePosixPath(relative_path).parent / quarantine)
    renamed = False
    try:
        rename_no_replace(parent_descriptor, name, quarantine)
        renamed = True
        descriptor = os.open(quarantine, os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0), dir_fd=parent_descriptor)
        metadata = os.fstat(descriptor)
        with os.fdopen(descriptor, "rb", closefd=False) as stream:
            observed = stream.read()
        if (
            not stat.S_ISREG(metadata.st_mode)
            or metadata.st_nlink != 1
            or (metadata.st_dev, metadata.st_ino) != expected_identity
            or observed != expected_content
        ):
            raise EntryAuthorityError(
                "V23_ROLLBACK_OWNERSHIP_RACE",
                f"{relative_path}: created inode or bytes no longer owned; preserved={quarantine_path}",
                "BLOCKED",
            )
        os.fsync(parent_descriptor)
        return quarantine_path
    except FileNotFoundError as error:
        preserved = quarantine_path if renamed else relative_path
        raise EntryAuthorityError(
            "V23_ROLLBACK_OWNERSHIP_RACE",
            f"{relative_path}: missing; preserved={preserved}",
            "BLOCKED",
        ) from error
    except OSError as error:
        preserved = quarantine_path if renamed else relative_path
        raise EntryAuthorityError(
            "V23_ROLLBACK_FAILED",
            f"{relative_path}: {error}; preserved={preserved}",
            "BLOCKED",
        ) from error
    finally:
        close_errors: list[OSError] = []
        for open_descriptor in (descriptor, parent_descriptor):
            if open_descriptor >= 0:
                try:
                    os.close(open_descriptor)
                except OSError as error:
                    close_errors.append(error)
        if close_errors:
            close_detail = f"descriptor close failed: {close_errors!r}; preserved={quarantine_path if renamed else relative_path}"
            active_error = sys.exception()
            if isinstance(active_error, EntryAuthorityError):
                active_error.detail = f"{active_error.detail}; {close_detail}"
                active_error.args = (f"{active_error.code}: {active_error.detail}",)
            elif active_error is None:
                raise EntryAuthorityError("V23_ROLLBACK_FAILED", close_detail, "BLOCKED")


def quarantine_unverified_created_file(root: Path, relative_path: str) -> str:
    """Move a just-created path aside when its initial metadata cannot be trusted."""

    parent_descriptor, name = open_parent_directory(root, relative_path)
    quarantine = f".{name}.rollback.{os.getpid()}.{secrets.token_hex(8)}"
    quarantine_path = str(PurePosixPath(relative_path).parent / quarantine)
    renamed = False
    try:
        rename_no_replace(parent_descriptor, name, quarantine)
        renamed = True
        os.fsync(parent_descriptor)
        return quarantine_path
    except OSError as error:
        preserved = quarantine_path if renamed else relative_path
        raise EntryAuthorityError(
            "V23_ROLLBACK_FAILED",
            f"{relative_path}: {error}; preserved={preserved}",
            "BLOCKED",
        ) from error
    finally:
        os.close(parent_descriptor)


def quarantine_failed_creation(
    root: Path,
    relative_path: str,
    content: bytes,
    created: bool,
    identity: tuple[int, int] | None,
    error: EntryAuthorityError,
) -> None:
    """Quarantine a path created by this invocation while preserving the originating blocker."""

    if not created:
        return
    try:
        quarantine = (
            remove_owned_file(root, relative_path, content, identity)
            if identity is not None
            else quarantine_unverified_created_file(root, relative_path)
        )
    except EntryAuthorityError as rollback_error:
        error.detail = f"{error.detail}; rollbackError={rollback_error}"
    else:
        error.detail = f"{error.detail}; rollbackQuarantine={quarantine}"
    error.args = (f"{error.code}: {error.detail}",)


def atomic_write(
    root: Path,
    relative_path: str,
    content: bytes,
    *,
    no_clobber: bool,
) -> tuple[bool, tuple[int, int] | None]:
    """Write one contained regular file and return its created inode identity."""

    parent_descriptor, name = open_parent_directory(root, relative_path)
    descriptor = -1
    try:
        if no_clobber:
            try:
                descriptor = os.open(
                    name,
                    os.O_RDWR | os.O_CREAT | os.O_EXCL | getattr(os, "O_NOFOLLOW", 0),
                    0o644,
                    dir_fd=parent_descriptor,
                )
            except FileExistsError as error:
                existing, existing_identity = read_regular_worktree_file(root, relative_path)
                if existing == content:
                    return False, existing_identity
                raise EntryAuthorityError("V23_WRITE_WOULD_CLOBBER", relative_path, "BLOCKED") from error
            identity: tuple[int, int] | None = None
            written = 0
            try:
                metadata = os.fstat(descriptor)
                identity = (metadata.st_dev, metadata.st_ino)
                if (
                    not stat.S_ISREG(metadata.st_mode)
                    or stat.S_IMODE(metadata.st_mode) != 0o644
                    or metadata.st_nlink != 1
                ):
                    raise EntryAuthorityError(
                        "V23_WRITE_PATH_INVALID",
                        f"{relative_path}: created path is not a mode-100644 single-link regular file",
                        "BLOCKED",
                    )
                while written < len(content):
                    count = os.write(descriptor, memoryview(content)[written:])
                    if count <= 0:
                        raise OSError("short write made no progress")
                    written += count
                os.fsync(descriptor)
                os.lseek(descriptor, 0, os.SEEK_SET)
                if os.read(descriptor, len(content) + 1) != content:
                    raise OSError("post-write byte identity mismatch")
                os.fsync(parent_descriptor)
                closing_descriptor = descriptor
                descriptor = -1
                os.close(closing_descriptor)
                closing_parent = parent_descriptor
                parent_descriptor = -1
                os.close(closing_parent)
            except BaseException as write_error:
                if descriptor >= 0:
                    try:
                        os.close(descriptor)
                    except OSError:
                        pass
                descriptor = -1
                try:
                    quarantine = (
                        remove_owned_file(root, relative_path, content[:written], identity)
                        if identity is not None
                        else quarantine_unverified_created_file(root, relative_path)
                    )
                except EntryAuthorityError as rollback_error:
                    if isinstance(write_error, EntryAuthorityError):
                        write_error.detail = f"{write_error.detail}; rollbackError={rollback_error}"
                        write_error.args = (f"{write_error.code}: {write_error.detail}",)
                        raise write_error
                    raise EntryAuthorityError(
                        "V23_WRITE_FAILED",
                        f"{relative_path}: {write_error}; rollbackError={rollback_error}",
                        "BLOCKED",
                    ) from write_error
                if isinstance(write_error, EntryAuthorityError):
                    write_error.detail = f"{write_error.detail}; rollbackQuarantine={quarantine}"
                    write_error.args = (f"{write_error.code}: {write_error.detail}",)
                    raise write_error
                raise EntryAuthorityError(
                    "V23_WRITE_FAILED",
                    f"{relative_path}: {write_error}; rollbackQuarantine={quarantine}",
                    "BLOCKED",
                ) from write_error
            return True, identity
        temporary_name = f".{name}.{os.getpid()}.{secrets.token_hex(8)}.tmp"
        descriptor = os.open(
            temporary_name,
            os.O_RDWR | os.O_CREAT | os.O_EXCL | getattr(os, "O_NOFOLLOW", 0),
            0o644,
            dir_fd=parent_descriptor,
        )
        written = 0
        while written < len(content):
            count = os.write(descriptor, memoryview(content)[written:])
            if count <= 0:
                raise OSError("short write made no progress")
            written += count
        os.fsync(descriptor)
        os.lseek(descriptor, 0, os.SEEK_SET)
        if os.read(descriptor, len(content) + 1) != content:
            raise OSError("post-write byte identity mismatch")
        os.close(descriptor)
        descriptor = -1
        os.replace(temporary_name, name, src_dir_fd=parent_descriptor, dst_dir_fd=parent_descriptor)
        os.fsync(parent_descriptor)
        return True, None
    except EntryAuthorityError:
        raise
    except OSError as error:
        raise EntryAuthorityError("V23_WRITE_FAILED", f"{relative_path}: {error}", "BLOCKED") from error
    finally:
        if descriptor >= 0:
            try:
                os.close(descriptor)
            except OSError:
                pass
        if parent_descriptor >= 0:
            try:
                os.close(parent_descriptor)
            except OSError:
                pass


def append_suffix_exact(
    root: Path,
    relative_path: str,
    prefix: bytes,
    suffix: bytes,
) -> tuple[bool, tuple[int, int]]:
    """Append one exact suffix and report whether this invocation installed it."""

    if not suffix:
        raise EntryAuthorityError("V23_ARCHITECTURE_APPEND_INVALID", "empty marker suffix", "BLOCKED")
    parent_descriptor, name = open_parent_directory(root, relative_path)
    descriptor = -1
    identity: tuple[int, int] | None = None
    written = 0
    try:
        descriptor = os.open(
            name,
            os.O_RDWR | os.O_APPEND | getattr(os, "O_NOFOLLOW", 0),
            dir_fd=parent_descriptor,
        )
        opened = os.fstat(descriptor)
        identity = (opened.st_dev, opened.st_ino)
        if (
            not stat.S_ISREG(opened.st_mode)
            or stat.S_IMODE(opened.st_mode) != 0o644
            or opened.st_nlink != 1
        ):
            raise EntryAuthorityError(
                "V23_ARCHITECTURE_APPEND_FAILED",
                "architecture is not a mode-100644 single-link regular file",
                "BLOCKED",
            )
        with os.fdopen(descriptor, "r+b", buffering=0) as stream:
            descriptor = -1
            acquire_bounded_lock(stream.fileno())
            stream.seek(0)
            observed = stream.read()
            if observed == prefix + suffix:
                named = os.stat(name, dir_fd=parent_descriptor, follow_symlinks=False)
                if (
                    not stat.S_ISREG(named.st_mode)
                    or stat.S_IMODE(named.st_mode) != 0o644
                    or named.st_nlink != 1
                    or (named.st_dev, named.st_ino) != identity
                ):
                    raise EntryAuthorityError(
                        "V23_ARCHITECTURE_FINAL_IDENTITY_DRIFT",
                        "byte-identical retry no longer names the opened architecture inode",
                        "BLOCKED",
                    )
                return False, identity
            if observed != prefix:
                raise EntryAuthorityError(
                    "V23_ARCHITECTURE_WORKTREE_DRIFT",
                    "architecture is neither the source prefix nor the exact published marker pair",
                    "BLOCKED",
                )
            while written < len(suffix):
                count = os.write(stream.fileno(), memoryview(suffix)[written:])
                if count <= 0:
                    raise EntryAuthorityError(
                        "V23_ARCHITECTURE_APPEND_FAILED",
                        f"expected={len(suffix)}; written={written}",
                        "BLOCKED",
                    )
                written += count
            os.fsync(stream.fileno())
            stream.seek(0)
            final_content = stream.read()
            final_metadata = os.fstat(stream.fileno())
            named_metadata = os.stat(name, dir_fd=parent_descriptor, follow_symlinks=False)
            if (
                (final_metadata.st_dev, final_metadata.st_ino) != identity
                or (named_metadata.st_dev, named_metadata.st_ino) != identity
                or not stat.S_ISREG(named_metadata.st_mode)
                or stat.S_IMODE(final_metadata.st_mode) != 0o644
                or stat.S_IMODE(named_metadata.st_mode) != 0o644
                or final_metadata.st_nlink != 1
                or named_metadata.st_nlink != 1
                or final_content != prefix + suffix
            ):
                raise EntryAuthorityError(
                    "V23_ARCHITECTURE_FINAL_IDENTITY_DRIFT",
                    "architecture inode or final bytes changed during publication",
                    "BLOCKED",
                )
            return True, identity
    except EntryAuthorityError as error:
        error.append_identity = identity
        error.append_may_have_written = written > 0
        raise
    except OSError as error:
        wrapped = EntryAuthorityError("V23_ARCHITECTURE_APPEND_FAILED", str(error), "BLOCKED")
        wrapped.append_identity = identity
        wrapped.append_may_have_written = written > 0
        raise wrapped from error
    finally:
        if descriptor >= 0:
            os.close(descriptor)
        os.close(parent_descriptor)


def rollback_architecture_append(
    root: Path,
    relative_path: str,
    prefix: bytes,
    expected_identity: tuple[int, int],
) -> str:
    """Quarantine the appended inode and restore the visible source prefix on a new inode."""

    parent_descriptor, name = open_parent_directory(root, relative_path)
    descriptor = -1
    quarantine = f".{name}.rollback.{os.getpid()}.{secrets.token_hex(8)}"
    quarantine_path = str(PurePosixPath(relative_path).parent / quarantine)
    quarantined = False
    try:
        rename_no_replace(parent_descriptor, name, quarantine)
        quarantined = True
        descriptor = os.open(quarantine, os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0), dir_fd=parent_descriptor)
        metadata = os.fstat(descriptor)
        if (
            not stat.S_ISREG(metadata.st_mode)
            or metadata.st_nlink != 1
            or (metadata.st_dev, metadata.st_ino) != expected_identity
        ):
            try:
                rename_no_replace(parent_descriptor, quarantine, name)
                os.fsync(parent_descriptor)
                preserved = relative_path
            except OSError:
                preserved = quarantine_path
            raise EntryAuthorityError(
                "V23_ARCHITECTURE_ROLLBACK_OWNERSHIP_RACE",
                f"architecture pathname no longer names the appended inode; preserved={preserved}",
                "BLOCKED",
            )
        os.close(descriptor)
        descriptor = -1
        atomic_write(root, relative_path, prefix, no_clobber=True)
        os.fsync(parent_descriptor)
        return quarantine_path
    except EntryAuthorityError as error:
        if quarantined and "preserved=" not in error.detail:
            error.detail = f"{error.detail}; preserved={quarantine_path}"
            error.args = (f"{error.code}: {error.detail}",)
        raise
    except OSError as error:
        raise EntryAuthorityError(
            "V23_ARCHITECTURE_ROLLBACK_FAILED",
            f"{relative_path}: {error}; preserved={quarantine_path}",
            "BLOCKED",
        ) from error
    finally:
        if descriptor >= 0:
            os.close(descriptor)
        os.close(parent_descriptor)


def publication_observation(
    parent_descriptor: int,
    name: str,
    descriptor: int,
    relative_path: str,
) -> tuple[bytes, tuple[int, int, int, int, int, int, int]]:
    """Read one locked named descriptor and return bytes plus stable ownership metadata."""

    before = os.fstat(descriptor)
    named_before = os.stat(name, dir_fd=parent_descriptor, follow_symlinks=False)
    os.lseek(descriptor, 0, os.SEEK_SET)
    chunks: list[bytes] = []
    remaining = before.st_size
    while remaining:
        chunk = os.read(descriptor, min(1024 * 1024, remaining))
        if not chunk:
            break
        chunks.append(chunk)
        remaining -= len(chunk)
    after = os.fstat(descriptor)
    named_after = os.stat(name, dir_fd=parent_descriptor, follow_symlinks=False)
    before_identity = (
        before.st_dev,
        before.st_ino,
        before.st_nlink,
        before.st_mode,
        before.st_size,
        before.st_mtime_ns,
        before.st_ctime_ns,
    )
    after_identity = (
        after.st_dev,
        after.st_ino,
        after.st_nlink,
        after.st_mode,
        after.st_size,
        after.st_mtime_ns,
        after.st_ctime_ns,
    )
    if (
        before_identity != after_identity
        or remaining != 0
        or not stat.S_ISREG(after.st_mode)
        or not stat.S_ISREG(named_before.st_mode)
        or not stat.S_ISREG(named_after.st_mode)
        or stat.S_IMODE(after.st_mode) != 0o644
        or stat.S_IMODE(named_before.st_mode) != 0o644
        or stat.S_IMODE(named_after.st_mode) != 0o644
        or after.st_nlink != 1
        or named_before.st_nlink != 1
        or named_after.st_nlink != 1
        or (named_before.st_dev, named_before.st_ino) != (after.st_dev, after.st_ino)
        or (named_after.st_dev, named_after.st_ino) != (after.st_dev, after.st_ino)
    ):
        raise EntryAuthorityError(
            "V23_PUBLICATION_FINAL_IDENTITY_DRIFT",
            f"{relative_path}: named descriptor metadata changed",
            "BLOCKED",
        )
    return b"".join(chunks), after_identity


def validate_correction_publication_snapshot(
    root: Path,
    manifest: list[dict[str, str]],
    correction_content: bytes,
    correction_identity: tuple[int, int],
) -> None:
    """Retain one common stable snapshot of all V24 inputs and the visible correction record."""

    manifest_by_path = {row["path"]: row for row in manifest}
    paths = (*CORRECTION_MANIFEST_PATHS, CORRECTION_PATH)
    opened: list[tuple[int, str, int, str]] = []
    active_path = CORRECTION_PATH
    try:
        for active_path in paths:
            parent_descriptor, name = open_parent_directory(root, active_path)
            try:
                descriptor = os.open(
                    name,
                    os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0) | getattr(os, "O_NONBLOCK", 0),
                    dir_fd=parent_descriptor,
                )
            except BaseException:
                os.close(parent_descriptor)
                raise
            opened.append((parent_descriptor, name, descriptor, active_path))
        for _parent_descriptor, _name, descriptor, active_path in opened:
            acquire_bounded_lock(
                descriptor,
                code=(
                    "V24_PUBLICATION_FINAL_IDENTITY_DRIFT"
                    if active_path == CORRECTION_PATH
                    else "V24_TOOLING_INPUT_DRIFT"
                ),
            )
        first: dict[str, tuple[bytes, tuple[int, int, int, int, int, int, int]]] = {}
        second: dict[str, tuple[bytes, tuple[int, int, int, int, int, int, int]]] = {}
        for parent_descriptor, name, descriptor, active_path in opened:
            first[active_path] = publication_observation(
                parent_descriptor,
                name,
                descriptor,
                active_path,
            )
        for parent_descriptor, name, descriptor, active_path in reversed(opened):
            second[active_path] = publication_observation(
                parent_descriptor,
                name,
                descriptor,
                active_path,
            )
        for active_path in paths:
            if first[active_path] != second[active_path]:
                raise EntryAuthorityError(
                    "V24_PUBLICATION_FINAL_IDENTITY_DRIFT"
                    if active_path == CORRECTION_PATH
                    else "V24_TOOLING_INPUT_DRIFT",
                    f"{active_path}: bytes, inode, link count, or metadata changed across the retained snapshot",
                    "BLOCKED",
                )
            content, metadata = second[active_path]
            if active_path == CORRECTION_PATH:
                if content != correction_content or metadata[:2] != correction_identity:
                    raise EntryAuthorityError(
                        "V24_PUBLICATION_FINAL_IDENTITY_DRIFT",
                        f"{active_path}: final inode or bytes changed",
                        "BLOCKED",
                    )
            elif sha256(content) != manifest_by_path[active_path]["sha256"]:
                raise EntryAuthorityError(
                    "V24_TOOLING_INPUT_DRIFT",
                    f"{active_path}: final bytes differ from the rendered manifest",
                    "BLOCKED",
                )
    except EntryAuthorityError as error:
        if error.code.startswith("V24_"):
            raise
        raise EntryAuthorityError(
            "V24_PUBLICATION_FINAL_IDENTITY_DRIFT"
            if active_path == CORRECTION_PATH
            else "V24_TOOLING_INPUT_DRIFT",
            f"{active_path}: {error.detail}",
            "BLOCKED",
        ) from error
    except OSError as error:
        raise EntryAuthorityError(
            "V24_PUBLICATION_FINAL_IDENTITY_DRIFT"
            if active_path == CORRECTION_PATH
            else "V24_TOOLING_INPUT_DRIFT",
            f"{active_path}: {error}",
            "BLOCKED",
        ) from error
    finally:
        close_errors: list[tuple[str, OSError]] = []
        for parent_descriptor, _name, descriptor, _relative_path in reversed(opened):
            try:
                os.close(descriptor)
            except OSError as error:
                close_errors.append((_relative_path, error))
            try:
                os.close(parent_descriptor)
            except OSError as error:
                close_errors.append((_relative_path, error))
        if close_errors:
            close_paths = {path for path, _error in close_errors}
            code = (
                "V24_PUBLICATION_FINAL_IDENTITY_DRIFT"
                if CORRECTION_PATH in close_paths
                else "V24_TOOLING_INPUT_DRIFT"
            )
            detail = f"descriptor close failed: {close_errors!r}"
            active_error = sys.exception()
            if isinstance(active_error, EntryAuthorityError):
                active_error.detail = f"{active_error.detail}; {detail}"
                active_error.args = (f"{active_error.code}: {active_error.detail}",)
            elif active_error is None:
                raise EntryAuthorityError(code, detail, "BLOCKED")
            else:
                raise EntryAuthorityError(code, f"{active_error}; {detail}", "BLOCKED") from active_error


def validate_publication_pair(
    root: Path,
    authority_content: bytes,
    authority_identity: tuple[int, int],
    architecture_content: bytes,
    architecture_identity: tuple[int, int],
) -> None:
    """Linearize success with bounded locks and authority-A1/architecture-B/authority-A2 reads."""

    opened: list[tuple[int, str, int, str]] = []
    try:
        for relative_path in (AUTHORITY_PATH, ARCHITECTURE_PATH):
            parent_descriptor, name = open_parent_directory(root, relative_path)
            try:
                descriptor = os.open(
                    name,
                    os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0),
                    dir_fd=parent_descriptor,
                )
            except BaseException:
                os.close(parent_descriptor)
                raise
            opened.append((parent_descriptor, name, descriptor, relative_path))
        for _parent_descriptor, _name, descriptor, _relative_path in opened:
            acquire_bounded_lock(descriptor)
        authority_parent, authority_name, authority_descriptor, _ = opened[0]
        architecture_parent, architecture_name, architecture_descriptor, _ = opened[1]
        authority_a1 = publication_observation(
            authority_parent,
            authority_name,
            authority_descriptor,
            AUTHORITY_PATH,
        )
        architecture_b = publication_observation(
            architecture_parent,
            architecture_name,
            architecture_descriptor,
            ARCHITECTURE_PATH,
        )
        authority_a2 = publication_observation(
            authority_parent,
            authority_name,
            authority_descriptor,
            AUTHORITY_PATH,
        )
        if (
            authority_a1 != authority_a2
            or authority_a2[0] != authority_content
            or authority_a2[1][:2] != authority_identity
            or architecture_b[0] != architecture_content
            or architecture_b[1][:2] != architecture_identity
        ):
            raise EntryAuthorityError(
                "V23_PUBLICATION_FINAL_IDENTITY_DRIFT",
                "authority A1/A2 or architecture B bytes, inode, link count, or metadata changed",
                "BLOCKED",
            )
    except EntryAuthorityError:
        raise
    except OSError as error:
        raise EntryAuthorityError("V23_PUBLICATION_FINAL_IDENTITY_DRIFT", str(error), "BLOCKED") from error
    finally:
        for parent_descriptor, _name, descriptor, _relative_path in reversed(opened):
            try:
                os.close(descriptor)
            except OSError:
                pass
            try:
                os.close(parent_descriptor)
            except OSError:
                pass


def publish_authority_pair(root: Path, document: dict[str, Any], architecture: bytes) -> None:
    """Install the future authority and marker as a recoverable two-path transaction."""

    authority_content = json_bytes(document)
    original_architecture, original_architecture_identity = read_regular_worktree_file(root, ARCHITECTURE_PATH)
    publication = document.get("publication")
    source = publication.get("sourceCommit") if isinstance(publication, dict) else None
    if not isinstance(source, str):
        raise EntryAuthorityError("V23_ARCHITECTURE_WORKTREE_DRIFT", "authority source commit is unavailable", "BLOCKED")
    committed_architecture = candidate_blob(root, source, ARCHITECTURE_PATH, "V23_ARCHITECTURE_UNAVAILABLE")
    if original_architecture not in (committed_architecture, architecture):
        raise EntryAuthorityError(
            "V23_ARCHITECTURE_WORKTREE_DRIFT",
            "worktree architecture is neither the committed source nor the exact published pair",
            "BLOCKED",
        )
    if not architecture.startswith(committed_architecture):
        raise EntryAuthorityError("V23_ARCHITECTURE_WORKTREE_DRIFT", "rendered marker does not preserve the source", "BLOCKED")
    marker = architecture[len(committed_architecture) :]
    lock_descriptor, _lock_name = open_parent_directory(root, AUTHORITY_PATH)
    try:
        acquire_bounded_lock(lock_descriptor)
    except BaseException:
        os.close(lock_descriptor)
        raise
    authority_created = False
    authority_identity: tuple[int, int] | None = None
    marker_created = False
    architecture_identity = original_architecture_identity
    try:
        try:
            authority_created, authority_identity = atomic_write(
                root,
                AUTHORITY_PATH,
                authority_content,
                no_clobber=True,
            )
            marker_created, architecture_identity = append_suffix_exact(
                root,
                ARCHITECTURE_PATH,
                committed_architecture,
                marker,
            )
            architecture_parent, _architecture_name = open_parent_directory(root, ARCHITECTURE_PATH)
            try:
                os.fsync(lock_descriptor)
                os.fsync(architecture_parent)
            finally:
                try:
                    os.close(architecture_parent)
                except OSError:
                    pass
            if authority_identity is None:
                raise EntryAuthorityError(
                    "V23_PUBLICATION_FINAL_IDENTITY_DRIFT",
                    "authority inode identity is unavailable",
                    "BLOCKED",
                )
            # This is the success linearization point. No semantic operation follows it.
            validate_publication_pair(
                root,
                authority_content,
                authority_identity,
                architecture,
                architecture_identity,
            )
            return
        except BaseException as original_error:
            rollback_diagnostics: list[str] = []
            appended_by_this_call = marker_created or bool(
                getattr(original_error, "append_may_have_written", False)
            )
            append_identity = getattr(original_error, "append_identity", None)
            if appended_by_this_call:
                try:
                    quarantine = rollback_architecture_append(
                        root,
                        ARCHITECTURE_PATH,
                        committed_architecture,
                        append_identity or architecture_identity,
                    )
                    rollback_diagnostics.append(f"architectureQuarantine={quarantine}")
                except BaseException as error:
                    rollback_diagnostics.append(f"architectureRollbackError={error}")
            if authority_created and authority_identity is not None:
                try:
                    quarantine = remove_owned_file(
                        root,
                        AUTHORITY_PATH,
                        authority_content,
                        authority_identity,
                    )
                    rollback_diagnostics.append(f"authorityQuarantine={quarantine}")
                except BaseException as error:
                    rollback_diagnostics.append(f"authorityRollbackError={error}")
            if rollback_diagnostics and isinstance(original_error, EntryAuthorityError):
                original_error.detail = f"{original_error.detail}; {'; '.join(rollback_diagnostics)}"
                original_error.args = (f"{original_error.code}: {original_error.detail}",)
            raise original_error
    finally:
        try:
            os.close(lock_descriptor)
        except OSError:
            pass


def build_parser() -> argparse.ArgumentParser:
    """Build the closed CLI."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--candidate", default="HEAD")
    actions = parser.add_mutually_exclusive_group(required=True)
    actions.add_argument("--write-request", action="store_true")
    actions.add_argument("--verify-request", action="store_true")
    actions.add_argument("--write-correction", action="store_true")
    actions.add_argument("--verify-correction", action="store_true")
    actions.add_argument("--publish-authority", action="store_true")
    actions.add_argument("--check", action="store_true")
    parser.add_argument("--owner-identity")
    parser.add_argument("--decided-at-utc")
    parser.add_argument("--rationale")
    parser.add_argument("--preservation-evidence")
    parser.add_argument("--performance-evidence")
    parser.add_argument("--landing-zone-evidence")
    return parser


def required(value: str | None, option: str) -> str:
    """Require one future publication option."""

    if value is None:
        raise EntryAuthorityError("V23_ARGUMENT_MISSING", option, "BLOCKED")
    return value


def main(arguments: Sequence[str] | None = None) -> int:
    """Run request generation/checking or the separately authorized publication route."""

    args = build_parser().parse_args(arguments)
    try:
        root = repository_root(args.repository)
        if args.write_request:
            document = render_request(root)
            content = json_bytes(document)
            created, identity = atomic_write(root, REQUEST_PATH, content, no_clobber=True)
            try:
                if identity is None:
                    raise EntryAuthorityError(
                        "V23_PUBLICATION_FINAL_IDENTITY_DRIFT",
                        "request inode identity is unavailable",
                        "BLOCKED",
                    )
                revalidate_owned_file(root, REQUEST_PATH, content, identity)
            except Exception as error:
                mapped = (
                    error
                    if isinstance(error, EntryAuthorityError)
                    else EntryAuthorityError(
                        "V23_PUBLICATION_FINAL_IDENTITY_DRIFT",
                        str(error),
                        "BLOCKED",
                    )
                )
                quarantine_failed_creation(root, REQUEST_PATH, content, created, identity, mapped)
                if mapped is not error:
                    raise mapped from error
                raise
            print(f"V23_STORY_7_1_ENTRY_REQUEST_WRITTEN path={REQUEST_PATH} sha256={sha256(content)}")
            return 0
        if args.verify_request:
            request, publication, _content = validate_request(root, args.candidate)
            result = request_check_result(request, publication)
            validate_schema(root, result, schema_commit=publication)
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return int(result["exitCode"])
        if args.write_correction:
            document = render_correction(root)
            content = json_bytes(document)
            created, identity = atomic_write(root, CORRECTION_PATH, content, no_clobber=True)
            try:
                if identity is None:
                    raise EntryAuthorityError(
                        "V24_PUBLICATION_FINAL_IDENTITY_DRIFT",
                        "correction inode identity is unavailable",
                        "BLOCKED",
                    )
                validate_correction_publication_snapshot(
                    root,
                    document["toolingTransaction"]["manifest"],
                    content,
                    identity,
                )
            except Exception as error:
                if not isinstance(error, EntryAuthorityError) or error.code == "V23_PUBLICATION_FINAL_IDENTITY_DRIFT":
                    mapped = EntryAuthorityError(
                        "V24_PUBLICATION_FINAL_IDENTITY_DRIFT",
                        error.detail if isinstance(error, EntryAuthorityError) else str(error),
                        "BLOCKED",
                    )
                else:
                    mapped = error
                quarantine_failed_creation(root, CORRECTION_PATH, content, created, identity, mapped)
                if mapped is not error:
                    raise mapped from error
                raise
            print(f"V24_STORY_7_1_TOOLING_CORRECTION_WRITTEN path={CORRECTION_PATH} sha256={sha256(content)}")
            return 0
        if args.verify_correction:
            correction, publication, content = validate_correction(root, args.candidate)
            print(
                "V24_STORY_7_1_TOOLING_CORRECTION_OK "
                f"publication={publication} sha256={sha256(content)} assertions={len(correction['assertionLedger'])}"
            )
            return 0
        if args.publish_authority:
            document, architecture = render_authority(
                root,
                args.candidate,
                owner_identity=required(args.owner_identity, "--owner-identity"),
                decided_at_utc=required(args.decided_at_utc, "--decided-at-utc"),
                rationale=required(args.rationale, "--rationale"),
                preservation_evidence=required(args.preservation_evidence, "--preservation-evidence"),
                performance_evidence=required(args.performance_evidence, "--performance-evidence"),
                landing_zone_evidence=required(args.landing_zone_evidence, "--landing-zone-evidence"),
            )
            publish_authority_pair(root, document, architecture)
            print("V23_STORY_7_1_ENTRY_AUTHORITY_WRITTEN awaiting-signed-commit-validation")
            return 0
        document = resolve_published_authority(root, args.candidate)
    except EntryAuthorityError as error:
        document = fail_result(error.code, error.detail, error.state)
    except Exception as error:
        document = fail_result("V23_ENVIRONMENT_UNAVAILABLE", str(error), "BLOCKED")
    print(json.dumps(document, indent=2, ensure_ascii=False))
    return int(document["exitCode"])


if __name__ == "__main__":
    raise SystemExit(main())
