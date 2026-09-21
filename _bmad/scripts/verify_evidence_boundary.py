#!/usr/bin/env python3
"""Fail closed at planning/evidence lifecycle boundaries."""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
from typing import Any, Callable, Sequence


SCHEMA = "hexalith.conversations.evidence-boundary-result.v1"
GIT_TIMEOUT_SECONDS = 30
GATE_MARKER = "V12 lifecycle evidence gates"
PUBLICATION_SCOPE_PATH = "_bmad-output/planning-artifacts/v14-planning-publication-scope-v1.json"
V15_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json"
V15_PUBLISHER_PATH = "_bmad/scripts/publish_v15_planning_tooling_environment.py"
V16_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json"
V16_PUBLISHER_PATH = "_bmad/scripts/publish_v16_planning_tooling_lifecycle.py"
V15_BASELINE_COMMIT = "6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4"
V16_BASELINE_COMMIT = "08a4bdcc5a18067f8f93c777055d8097987a9da2"
V17_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json"
V17_RECORD_PATH = "_bmad-output/planning-artifacts/implementation-hold-v1.json"
V17_PUBLISHER_PATH = "_bmad/scripts/publish_implementation_hold_decision.py"
V17_BASELINE_COMMIT = "074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf"
V23_REQUEST_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-entry-candidate-v1.json"
V23_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-entry-authority-v1.json"
V23_PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_entry_authority.py"
V23_SCHEMA_PATH = "_bmad/schemas/v23-story-7.1-entry-authority-v1.schema.json"
V23_ARCHITECTURE_PATH = "_bmad-output/planning-artifacts/architecture.md"
V23_TOOLING_BASELINE = "e0b098fa1c056385e28ee8ac0efd0c55dfab324f"
V23_PROTECTED_MAIN = "dcba5d4b1314eb67a95fa560b7cc0f88a9ab2607"
V23_HISTORICAL_V22_CANDIDATE = "cf82f8008d02b07d48338a545909d97faa302362"
V23_PUBLISHER_SHA256 = "acb9695cb9c20ff42d4b5ad5e4756d82bc1d0edc0c61c4e064d702fb902e542a"
V23_WORKFLOW_SHA256 = "328fdd95cb6edd546c735a0da329cc3d1505097b05d3fb2a855692d4b18c3478"
V23_SCHEMA_SHA256 = "d11340d9b2665c5295a4408f9e6b26218001a61f118d6ec979d6e9ca4da3ea1b"
V23_RESULT_SCHEMA_VERSION = "hexalith.conversations.current-planning-authority-result.v1"
V23_TRUSTED_OWNER_IDENTITY = "Jerome Piquot <jpiquot@itaneo.com>"
V23_TRUSTED_SSH_PRINCIPAL = "jpiquot@itaneo.com"
V23_TRUSTED_SSH_FINGERPRINT = "SHA256:8XlNQvE3ucPf/e509wU4qtNgiyWA+TKmLei7F7+TCvk"
V23_BEGIN = b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN"
V23_END = b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END"
V23_TOOLING_PATHS = tuple(
    sorted(
        (
            V23_REQUEST_PATH,
            V23_SCHEMA_PATH,
            V23_PUBLISHER_PATH,
            "_bmad/scripts/resolve_current_planning_authority.py",
            "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py",
            "_bmad/scripts/tests/test_resolve_current_planning_authority.py",
            "_bmad/scripts/tests/test_verify_evidence_boundary.py",
            "_bmad/scripts/verify_evidence_boundary.py",
            ".github/workflows/planning-authority-preflight.yml",
        )
    )
)
V23_AUTHORITY_GATE_SUBJECTS = (
    "V22-HISTORICAL-CANDIDATE",
    "V22-PROTECTED-MAIN-DIAGNOSTIC",
    "STORY-6.2-PREDECESSOR",
    "7.1-SCHEMAS-CHECKPOINT",
    "IR-0-READINESS",
    "CURRENT-WORKFLOW-ROUTE",
    "PRODUCTION-OPERATIONAL-ENVELOPE",
    "FR-20-SM-C1",
    "SM-C2",
    "OQ-1",
    "OWNER-APPROVAL",
    "EPIC-7-SPRINT-INVENTORY",
)
V23_REQUEST_LEDGER = (
    ("V23.REQUEST.01", "V22-HISTORICAL-CANDIDATE", "PASS"),
    ("V23.REQUEST.02", "V22-PROTECTED-MAIN-DIAGNOSTIC", "PASS"),
    ("V23.REQUEST.03", "STORY-6.2-PREDECESSOR", "PASS"),
    ("V23.REQUEST.04", "7.1-SCHEMAS-CHECKPOINT", "PASS"),
    ("V23.REQUEST.05", "IR-0-READINESS", "PASS"),
    ("V23.REQUEST.06", "CURRENT-WORKFLOW-ROUTE", "PASS"),
    ("V23_OPERATIONAL_ENVELOPE_MISSING", "PRODUCTION-OPERATIONAL-ENVELOPE", "BLOCKED"),
    ("V23_PRESERVATION_GATE_PENDING", "FR-20-SM-C1", "BLOCKED"),
    ("V23_PERFORMANCE_GATE_FAILED", "SM-C2", "FAIL"),
    ("V23_LANDING_ZONE_GATE_BLOCKED", "OQ-1", "BLOCKED"),
    ("V23_OWNER_APPROVAL_MISSING", "OWNER-APPROVAL", "BLOCKED"),
    ("V23.REQUEST.12", "EPIC-7-SPRINT-INVENTORY", "PASS"),
)
V23_REQUEST_BLOCKERS = (
    "V23_OPERATIONAL_ENVELOPE_MISSING",
    "V23_PRESERVATION_GATE_PENDING",
    "V23_PERFORMANCE_GATE_FAILED",
    "V23_LANDING_ZONE_GATE_BLOCKED",
    "V23_OWNER_APPROVAL_MISSING",
)
GIT_EXECUTABLE = "/usr/bin/git"
TRUSTED_EXECUTABLE_PATH = "/usr/bin:/bin"
V15_C1_PATHS = tuple(
    sorted(
        (
            ".github/workflows/planning-authority-preflight.yml",
            "_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md",
            "_bmad/schemas/v15-planning-tooling-environment-authority-v1.schema.json",
            V15_PUBLISHER_PATH,
            "_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py",
            "_bmad/scripts/tests/test_verify_evidence_boundary.py",
            "_bmad/scripts/verify_evidence_boundary.py",
            "pyproject.toml",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs",
            "uv.lock",
        )
    )
)
V16_C1_PATHS = tuple(
    sorted(
        (
            ".github/workflows/planning-authority-preflight.yml",
            "_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md",
            "_bmad-output/implementation-artifacts/spec-v16-correct-planning-tooling-lifecycle-authority.md",
            "_bmad/schemas/v16-planning-tooling-lifecycle-authority-v1.schema.json",
            V15_PUBLISHER_PATH,
            V16_PUBLISHER_PATH,
            "_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py",
            "_bmad/scripts/tests/test_publish_v16_planning_tooling_lifecycle.py",
            "_bmad/scripts/tests/test_verify_evidence_boundary.py",
            "_bmad/scripts/verify_evidence_boundary.py",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingLifecycleAuthorityV16ValidationTest.cs",
        )
    )
)
V17_C1_PATHS = tuple(
    sorted(
        (
            "_bmad/schemas/implementation-hold-v1.schema.json",
            "_bmad/schemas/v17-implementation-hold-decision-authority-v1.schema.json",
            V17_PUBLISHER_PATH,
            "_bmad/scripts/tests/test_publish_implementation_hold_decision.py",
            "_bmad/scripts/tests/test_verify_evidence_boundary.py",
            "_bmad/scripts/verify_evidence_boundary.py",
        )
    )
)
ACTIVE_ROUTE_PATHS = (
    ".agents/skills/bmad-build/step-04-review.md",
    ".agents/skills/bmad-build/step-05-present.md",
    ".agents/skills/bmad-build/step-oneshot.md",
    ".agents/skills/bmad-build-auto/step-04-review.md",
    ".agents/skills/bmad-code-review/steps/step-04-present.md",
    ".claude/skills/bmad-build/step-04-review.md",
    ".claude/skills/bmad-build/step-05-present.md",
    ".claude/skills/bmad-build/step-oneshot.md",
    ".claude/skills/bmad-build-auto/step-04-review.md",
    ".claude/skills/bmad-code-review/steps/step-04-present.md",
)
LOGICAL_ROUTE_PATHS = tuple(path.split("/skills/", 1)[1] for path in ACTIVE_ROUTE_PATHS[:5])
CONTEXT_WORKFLOW_PATHS = (
    "bmad-build/compile-epic-context.md",
    "bmad-build/step-01-clarify-and-route.md",
    "bmad-build-auto/compile-epic-context.md",
    "bmad-build-auto/step-01-clarify-and-route.md",
)
LIFECYCLE_TOKENS = {
    "bmad-build/step-04-review.md": "Change `{spec_file}` status to `in-review`",
    "bmad-build/step-05-present.md": "Change `{spec_file}` status to `done`",
    "bmad-build/step-oneshot.md": "status: 'done'",
    "bmad-build-auto/step-04-review.md": "Change `{spec_file}` status to `in-review`",
    "bmad-code-review/steps/step-04-present.md": "set `new_status` = `done`",
}
APPLICABLE_PREFIXES = (
    ".agents/skills/",
    ".claude/skills/",
    ".github/workflows/",
    "_bmad-output/implementation-artifacts/epic-6-context.md",
    "_bmad-output/planning-artifacts/",
    "_bmad/schemas/",
    "_bmad/scripts/",
    "docs/release-evidence/",
    "docs/runbooks/",
    "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
)


class BoundaryError(RuntimeError):
    """A stable boundary result that must not become PASS."""

    def __init__(self, code: str, message: str, state: str = "FAIL", path: str | None = None) -> None:
        super().__init__(message)
        self.code = code
        self.message = message
        self.state = state
        self.path = path


def sha256(content: bytes) -> str:
    """Return a lowercase SHA-256 digest."""

    return hashlib.sha256(content).hexdigest()


def assertion(assertion_id: str, subject: str, state: str, **details: Any) -> dict[str, Any]:
    """Create one non-vacuous assertion-ledger row."""

    row = {"id": assertion_id, "subject": subject, "state": state}
    row.update(details)
    return row


def safe_relative_path(value: str) -> str:
    """Require one normalized, contained repository-relative path."""

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
        raise BoundaryError("EVIDENCE_PATH_ESCAPE", f"invalid repository-relative path: {value!r}", path=value)
    return value


def trusted_environment() -> dict[str, str]:
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
    }


def run_git(repository: Path, *arguments: str, allowed: tuple[int, ...] = (0,)) -> subprocess.CompletedProcess[bytes]:
    """Run one bounded non-interactive Git command."""

    try:
        result = subprocess.run(
            (GIT_EXECUTABLE, "--no-replace-objects", "-C", str(repository), *arguments),
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=GIT_TIMEOUT_SECONDS,
            env=trusted_environment(),
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise BoundaryError("EVIDENCE_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise BoundaryError("EVIDENCE_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def repository_root(repository: Path) -> Path:
    """Resolve and require the explicit repository root."""

    resolved = repository.resolve()
    result = run_git(resolved, "rev-parse", "--show-toplevel")
    observed = Path(result.stdout.decode().strip()).resolve()
    if observed != resolved:
        raise BoundaryError("EVIDENCE_ROOT_MISMATCH", f"expected {resolved}; observed {observed}", "BLOCKED")
    return resolved


def resolve_commit(repository: Path, revision: str, code: str) -> str:
    """Resolve an exact commit or return a stable BLOCKED result."""

    try:
        value = run_git(repository, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode().strip()
    except BoundaryError as error:
        raise BoundaryError(code, error.message, "BLOCKED") from error
    if re.fullmatch(r"[0-9a-f]{40}", value) is None:
        raise BoundaryError(code, value, "BLOCKED")
    return value


def commit_parents(repository: Path, commit: str, code: str) -> tuple[str, ...]:
    """Return every parent and preserve malformed or unavailable history as BLOCKED."""

    try:
        record = run_git(repository, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").strip().split()
    except UnicodeError as error:
        raise BoundaryError(code, str(error), "BLOCKED") from error
    if not record or record[0] != commit or any(re.fullmatch(r"[0-9a-f]{40}", value) is None for value in record):
        raise BoundaryError(code, repr(record), "BLOCKED")
    return tuple(record[1:])


def require_single_parent(repository: Path, commit: str, expected: str, code: str) -> None:
    """Require exactly one parent with the expected identity."""

    parents = commit_parents(repository, commit, code)
    if len(parents) != 1 or parents[0] != expected:
        raise BoundaryError(code, f"expected ({expected!r},); observed {parents!r}", "BLOCKED")


def nul_paths(content: bytes) -> set[str]:
    """Parse normalized NUL-separated paths."""

    try:
        return {safe_relative_path(value.decode("utf-8", errors="strict")) for value in content.split(b"\0") if value}
    except UnicodeError as error:
        raise BoundaryError("EVIDENCE_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error


def changed_paths(repository: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return the exact committed candidate path set without worktree contamination."""

    return tuple(sorted(nul_paths(run_git(repository, "diff", "--name-only", "-z", baseline, candidate, "--").stdout)))


def worktree_paths(repository: Path) -> tuple[str, ...]:
    """Report staged, unstaged, and untracked paths separately from committed scope."""

    paths = set[str]()
    paths.update(nul_paths(run_git(repository, "diff", "--name-only", "-z", "--").stdout))
    paths.update(nul_paths(run_git(repository, "diff", "--cached", "--name-only", "-z", "--").stdout))
    paths.update(nul_paths(run_git(repository, "ls-files", "--others", "--exclude-standard", "-z").stdout))
    return tuple(sorted(paths))


def candidate_has_path(repository: Path, candidate: str, relative_path: str) -> bool:
    """Return whether the candidate tree contains one exact path."""

    result = run_git(
        repository,
        "cat-file",
        "-e",
        f"{candidate}:{safe_relative_path(relative_path)}",
        allowed=(0, 1, 128),
    )
    return result.returncode == 0


def candidate_blob(repository: Path, candidate: str, relative_path: str) -> bytes:
    """Read one exact committed candidate blob without consulting the worktree."""

    return run_git(
        repository,
        "cat-file",
        "blob",
        f"{candidate}:{safe_relative_path(relative_path)}",
    ).stdout


def authority_route(repository: Path, candidate: str) -> str:
    """Choose authority exclusively from committed candidate-tree identity."""

    if candidate_has_path(repository, candidate, V23_AUTHORITY_PATH):
        return "v23-authority"
    if candidate_has_path(repository, candidate, V23_REQUEST_PATH):
        return "v23-request"
    if candidate_has_path(repository, candidate, V23_ARCHITECTURE_PATH) and v23_marker_complete(
        repository,
        candidate,
    ):
        raise BoundaryError(
            "EVIDENCE_V23_MARKER_WITHOUT_RECORD",
            "complete V23 marker exists without the V23 request or authority",
            "BLOCKED",
            V23_ARCHITECTURE_PATH,
        )
    if candidate_has_path(repository, candidate, V17_AUTHORITY_PATH):
        return "v17"
    if candidate_has_path(repository, candidate, V16_AUTHORITY_PATH):
        return "v16"
    if candidate_has_path(repository, candidate, V15_AUTHORITY_PATH):
        return "v15"
    return "legacy"


def is_applicable(paths: Sequence[str]) -> bool:
    """Classify planning/evidence authority changes without treating absence as PASS."""

    return any(path.startswith(APPLICABLE_PREFIXES) for path in paths)


def read_route(root: Path, path: str) -> bytes:
    """Read an active route from the current repository."""

    try:
        return (root / path).read_bytes()
    except OSError as error:
        raise BoundaryError("EVIDENCE_GATE_NOT_USED", f"{path}: {error}", path=path) from error


def validate_active_routes(root: Path, reader: Callable[[Path, str], bytes] = read_route) -> list[dict[str, Any]]:
    """Require exact mirrored route identity, parity, and pre-transition placement."""

    ledger: list[dict[str, Any]] = []
    if len(ACTIVE_ROUTE_PATHS) != 10 or len(set(ACTIVE_ROUTE_PATHS)) != 10:
        raise BoundaryError("EVIDENCE_ROUTE_INVENTORY_DRIFT", "active route inventory is not exactly ten paths")
    content_by_path: dict[str, bytes] = {}
    for path in ACTIVE_ROUTE_PATHS:
        content = reader(root, path)
        content_by_path[path] = content
        logical = path.split("/skills/", 1)[1]
        text = content.decode("utf-8")
        if text.count(GATE_MARKER) != 1:
            raise BoundaryError("EVIDENCE_GATE_NOT_USED", path, path=path)
        marker = text.index(GATE_MARKER)
        lifecycle = text.find(LIFECYCLE_TOKENS[logical])
        if lifecycle < 0 or marker > lifecycle:
            raise BoundaryError("EVIDENCE_GATE_DISPLACED", path, path=path)
        required = ("verify_submodule_promotion.py", "verify_evidence_boundary.py", "PASS", "FAIL", "BLOCKED", "not-applicable")
        if any(token not in text[marker:] for token in required):
            raise BoundaryError("EVIDENCE_GATE_DECOY", path, path=path)
        ledger.append(assertion(f"ROUTE-{len(ledger) + 1:02d}", logical, "PASS", path=path))
    for logical in LOGICAL_ROUTE_PATHS:
        agents_path = f".agents/skills/{logical}"
        claude_path = f".claude/skills/{logical}"
        if content_by_path[agents_path] != content_by_path[claude_path]:
            raise BoundaryError("EVIDENCE_WORKFLOW_PARITY_DRIFT", logical)
    return ledger


def validate_context(root: Path, content: bytes | None = None) -> dict[str, Any]:
    """Require generated Epic 6 context frontmatter and active parity identities."""

    path = root / "_bmad-output/implementation-artifacts/epic-6-context.md"
    try:
        raw = path.read_bytes() if content is None else content
        text = raw.decode("utf-8", errors="strict")
    except (OSError, UnicodeError) as error:
        raise BoundaryError("EVIDENCE_CONTEXT_INVALID", str(error), path=path.as_posix()) from error
    match = re.match(r"\A---\n(?P<body>.*?)\n---\n", text.replace("\r\n", "\n"), re.DOTALL)
    if match is None:
        raise BoundaryError("EVIDENCE_CONTEXT_INVALID", "Epic 6 context frontmatter is missing")
    pairs = re.findall(
        r"^(overlay_version|architecture_version):\s*'?([^'\n]+?)'?\s*$",
        match.group("body"),
        re.MULTILINE,
    )
    if len(pairs) != 2 or {name for name, _ in pairs} != {
        "overlay_version",
        "architecture_version",
    }:
        raise BoundaryError(
            "EVIDENCE_CONTEXT_INVALID",
            "Epic 6 context requires each governing identity exactly once",
        )
    values = dict(pairs)
    expected = {
        "overlay_version": "epic-6-authority-2026-08-01-v8",
        "architecture_version": "conversations-architecture-2026-08-01-v8",
    }
    if values != expected:
        raise BoundaryError("EVIDENCE_CONTEXT_INVALID", f"expected={expected!r} observed={values!r}")
    normalized = text.replace("\r\n", "\n")
    required = (
        "# Epic 6 Context:",
        "FR-16 is the only non-activation",
        "AUTHORITY CORRECTION ONLY — NOT READY",
        "Promotion Completion Invariant",
        "Final Record Invariant",
        "Conformance Oracle Tier Invariant",
        "PROJECTION_PROOF_SUPERSESSION_REQUIRED",
    )
    if any(token not in normalized for token in required):
        raise BoundaryError(
            "EVIDENCE_CONTEXT_INVALID",
            "Epic 6 V8 semantic context is incomplete",
        )
    for story in range(1, 13):
        if normalized.count(f"### 6.{story} ") != 1:
            raise BoundaryError(
                "EVIDENCE_CONTEXT_INVALID",
                f"Epic 6 V8 story heading 6.{story} must occur exactly once",
            )
    return assertion("CONTEXT-01", "epic-6-context-frontmatter", "PASS", sha256=sha256(raw))


def validate_context_workflows(
    root: Path, reader: Callable[[Path, str], bytes] | None = None
) -> list[dict[str, Any]]:
    """Require both workflow trees to preserve and validate identity frontmatter."""

    read = reader if reader is not None else (lambda base, relative: (base / relative).read_bytes())
    ledger: list[dict[str, Any]] = []
    for logical in CONTEXT_WORKFLOW_PATHS:
        try:
            agents = read(root, f".agents/skills/{logical}")
            claude = read(root, f".claude/skills/{logical}")
        except OSError as error:
            raise BoundaryError("EVIDENCE_CONTEXT_WORKFLOW_INVALID", str(error)) from error
        if agents != claude:
            raise BoundaryError("EVIDENCE_WORKFLOW_PARITY_DRIFT", logical)
        text = agents.decode("utf-8")
        required = ("overlay_version", "architecture_version", "frontmatter")
        if any(token not in text for token in required):
            raise BoundaryError("EVIDENCE_CONTEXT_WORKFLOW_INVALID", logical)
        if "step-01" in logical:
            required = (
                "heading-only context",
                "historical authority",
                "filesystem mtime alone",
                "`### 6.1 ` through `### 6.12 `",
            )
            if any(token not in text for token in required):
                raise BoundaryError("EVIDENCE_CONTEXT_WORKFLOW_INVALID", logical)
        if "compile-epic-context" in logical:
            required = (
                "Historical Epic 6 v8 exception",
                "`### 6.1 ` through `### 6.12 `",
                "write nothing",
            )
            if any(token not in text for token in required):
                raise BoundaryError("EVIDENCE_CONTEXT_WORKFLOW_INVALID", logical)
        ledger.append(assertion(f"CONTEXT-WORKFLOW-{len(ledger) + 1:02d}", logical, "PASS", sha256=sha256(agents)))
    return ledger


def validate_csharp_signature_guard(root: Path, content: bytes | None = None) -> dict[str, Any]:
    """Reject vacuous signature checks and current-tree historical fallbacks."""

    path = "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs"
    if content is None:
        try:
            content = (root / path).read_bytes()
        except OSError as error:
            raise BoundaryError("EVIDENCE_SIGNATURE_GUARD_INVALID", str(error), path=path) from error
    text = content.decode("utf-8")
    required = (
        "Trim().Length == 0",
        "current checkout bytes are not historical evidence",
        "TryReadRecordedGitlink(submodule, out string gitlink).ShouldBeTrue",
        "TryReadSubmoduleBlob(submodule, gitlink",
    )
    if "Trim().Length >= 0" in text or any(token not in text for token in required):
        raise BoundaryError("EVIDENCE_SIGNATURE_GUARD_INVALID", path, path=path)
    return assertion("SIGNATURE-01", "historical-platform-signature-guard", "PASS", sha256=sha256(content))


def validate_gitlinks(repository: Path, baseline: str, candidate: str) -> dict[str, Any]:
    """Derive changed gitlinks only from raw mode 160000 tree entries."""

    raw = run_git(
        repository,
        "diff",
        "--raw",
        "--no-abbrev",
        "--no-renames",
        "-z",
        baseline,
        candidate,
        "--",
    ).stdout
    records = [record for record in raw.split(b"\0") if record]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise BoundaryError("EVIDENCE_GITLINK_SET_DRIFT", "malformed raw diff", "BLOCKED")
        metadata = records[index].decode("ascii", errors="strict")
        path = safe_relative_path(records[index + 1].decode("utf-8", errors="strict"))
        fields = metadata.split()
        if len(fields) >= 5 and (fields[0] == ":160000" or fields[1] == "160000"):
            paths.append(path)
    if any(not path.startswith("references/") for path in paths):
        raise BoundaryError("EVIDENCE_GITLINK_SET_DRIFT", repr(paths))
    return assertion("GITLINK-01", "raw-mode-160000-changed-set", "PASS", paths=sorted(set(paths)))


def validate_publication_scope(
    root: Path,
    baseline: str,
    candidate: str,
    paths: Sequence[str],
    gitlink_row: dict[str, Any],
) -> dict[str, Any]:
    """Apply the candidate-bound V14 exact path and zero-gitlink contract."""

    if PUBLICATION_SCOPE_PATH not in paths:
        return assertion("SCOPE-01", "candidate-bound-publication-allowlist", "PASS", applied=False)
    try:
        content = run_git(root, "show", f"{candidate}:{PUBLICATION_SCOPE_PATH}").stdout
        document = json.loads(content.decode("utf-8"))
    except (BoundaryError, UnicodeError, json.JSONDecodeError) as error:
        raise BoundaryError("EVIDENCE_SCOPE_MANIFEST_INVALID", str(error), path=PUBLICATION_SCOPE_PATH) from error
    if not isinstance(document, dict):
        raise BoundaryError(
            "EVIDENCE_SCOPE_MANIFEST_INVALID",
            "publication scope manifest must be a JSON object",
            path=PUBLICATION_SCOPE_PATH,
        )
    expected_keys = {"schemaVersion", "baseline", "expectedChangedPaths", "requireNoGitlinkChanges"}
    if set(document) != expected_keys or document.get("schemaVersion") != "hexalith.conversations.v14-planning-publication-scope.v1":
        raise BoundaryError("EVIDENCE_SCOPE_MANIFEST_INVALID", "closed schema mismatch", path=PUBLICATION_SCOPE_PATH)
    if document.get("baseline") != baseline:
        raise BoundaryError(
            "EVIDENCE_SCOPE_BASELINE_MISMATCH",
            f"expected {document.get('baseline')}; observed {baseline}",
            path=PUBLICATION_SCOPE_PATH,
        )
    expected = document.get("expectedChangedPaths")
    if (
        not isinstance(expected, list)
        or not expected
        or not all(isinstance(value, str) for value in expected)
        or len(expected) != len(set(expected))
    ):
        raise BoundaryError("EVIDENCE_SCOPE_MANIFEST_INVALID", "expectedChangedPaths must be unique and nonempty")
    normalized = tuple(sorted(safe_relative_path(value) for value in expected))
    observed = tuple(sorted(paths))
    if observed != normalized:
        missing = sorted(set(normalized) - set(observed))
        unexpected = sorted(set(observed) - set(normalized))
        raise BoundaryError("EVIDENCE_PUBLICATION_SCOPE_DRIFT", f"missing={missing!r} unexpected={unexpected!r}")
    if document.get("requireNoGitlinkChanges") is not True:
        raise BoundaryError("EVIDENCE_SCOPE_MANIFEST_INVALID", "zero-gitlink requirement missing")
    if gitlink_row.get("paths"):
        raise BoundaryError("EVIDENCE_GITLINK_SET_DRIFT", repr(gitlink_row["paths"]))
    return assertion("SCOPE-01", "candidate-bound-publication-allowlist", "PASS", count=len(normalized))


def validate_authority_scope(
    root: Path,
    evaluated_candidate: str,
    *,
    version: str,
    authority_path: str,
    schema_version: str,
    baseline: str,
    expected_c1_paths: tuple[str, ...],
    extra_c2_paths: tuple[str, ...] = (),
) -> dict[str, Any]:
    """Validate an authority's original C1/C2 from committed objects at any descendant."""

    code_prefix = f"EVIDENCE_{version.upper()}"
    try:
        additions = run_git(
            root,
            "log",
            "--format=%H",
            "--diff-filter=A",
            evaluated_candidate,
            "--",
            authority_path,
        ).stdout.decode("ascii", errors="strict").splitlines()
    except UnicodeError as error:
        raise BoundaryError(f"{code_prefix}_PUBLICATION_UNAVAILABLE", str(error), "BLOCKED", authority_path) from error
    publications = tuple(value for value in additions if value)
    if len(publications) != 1:
        raise BoundaryError(
            f"{code_prefix}_PUBLICATION_UNAVAILABLE",
            f"expected one publication; observed {publications!r}",
            "BLOCKED",
            authority_path,
        )
    publication_commit = resolve_commit(root, publications[0], f"{code_prefix}_PUBLICATION_UNAVAILABLE")
    try:
        published_bytes = run_git(root, "show", f"{publication_commit}:{authority_path}").stdout
        candidate_bytes = run_git(root, "show", f"{evaluated_candidate}:{authority_path}").stdout
        document = json.loads(published_bytes.decode("utf-8", errors="strict"))
    except (BoundaryError, UnicodeError, json.JSONDecodeError) as error:
        state = error.state if isinstance(error, BoundaryError) else "FAIL"
        raise BoundaryError(f"{code_prefix}_AUTHORITY_INVALID", str(error), state, path=authority_path) from error
    if not isinstance(document, dict):
        raise BoundaryError(f"{code_prefix}_AUTHORITY_INVALID", "authority must be an object", path=authority_path)
    publication = document.get("publication")
    candidate_commit = document.get("candidateCommit")
    if (
        document.get("schemaVersion") != schema_version
        or document.get("baselineCommit") != baseline
        or not isinstance(candidate_commit, str)
        or not isinstance(publication, dict)
    ):
        raise BoundaryError(f"{code_prefix}_AUTHORITY_INVALID", "closed identity mismatch", path=authority_path)
    c1 = resolve_commit(root, candidate_commit, f"{code_prefix}_CANDIDATE_UNAVAILABLE")
    require_single_parent(root, c1, baseline, f"{code_prefix}_C1_PARENT_MISMATCH")
    require_single_parent(root, publication_commit, c1, f"{code_prefix}_C2_PARENT_MISMATCH")
    ancestry = run_git(root, "merge-base", "--is-ancestor", publication_commit, evaluated_candidate, allowed=(0, 1))
    if ancestry.returncode != 0:
        raise BoundaryError(
            f"{code_prefix}_PUBLICATION_NOT_ANCESTOR",
            f"{publication_commit} is not an ancestor of {evaluated_candidate}",
            "BLOCKED",
        )
    expected_c2 = tuple(sorted((authority_path, *extra_c2_paths)))
    normalized_combined = tuple(sorted((*expected_c1_paths, *expected_c2)))
    expected_publication = {
        "c1Paths": list(expected_c1_paths),
        "c2Paths": list(expected_c2),
        "combinedPaths": list(normalized_combined),
        "changedGitlinks": [],
    } if extra_c2_paths else {
        "c1Paths": list(expected_c1_paths),
        "c2Path": authority_path,
        "combinedPaths": list(normalized_combined),
        "changedGitlinks": [],
    }
    if publication != expected_publication:
        raise BoundaryError(f"{code_prefix}_AUTHORITY_INVALID", "closed publication contract mismatch")
    observed_c1 = changed_paths(root, baseline, c1)
    observed_c2 = changed_paths(root, c1, publication_commit)
    observed_combined = changed_paths(root, baseline, publication_commit)
    if observed_c1 != expected_c1_paths or observed_c2 != expected_c2 or observed_combined != normalized_combined:
        raise BoundaryError(
            f"{code_prefix}_SCOPE_DRIFT",
            f"c1={observed_c1!r} c2={observed_c2!r} combined={observed_combined!r}",
        )
    transaction_gitlinks = validate_gitlinks(root, baseline, publication_commit)
    if transaction_gitlinks.get("paths"):
        raise BoundaryError("EVIDENCE_GITLINK_SET_DRIFT", repr(transaction_gitlinks["paths"]))
    if candidate_bytes != published_bytes:
        raise BoundaryError(f"{code_prefix}_AUTHORITY_DESCENDANT_DRIFT", authority_path)
    return assertion(
        f"{version.upper()}-SCOPE-01",
        f"{version}-planning-tooling-boundary",
        "PASS",
        applied=True,
        publication=publication_commit,
        count=len(normalized_combined),
    )


def validate_v15_scope(root: Path, candidate: str) -> dict[str, Any]:
    """Validate immutable V15 transaction at V15-only candidates."""

    return validate_authority_scope(
        root,
        candidate,
        version="v15",
        authority_path=V15_AUTHORITY_PATH,
        schema_version="hexalith.conversations.v15-planning-tooling-environment-authority.v1",
        baseline=V15_BASELINE_COMMIT,
        expected_c1_paths=V15_C1_PATHS,
    )


def validate_v16_scope(root: Path, candidate: str) -> dict[str, Any]:
    """Validate immutable V16 transaction at V16 and later descendants."""

    return validate_authority_scope(
        root,
        candidate,
        version="v16",
        authority_path=V16_AUTHORITY_PATH,
        schema_version="hexalith.conversations.v16-planning-tooling-lifecycle-authority.v1",
        baseline=V16_BASELINE_COMMIT,
        expected_c1_paths=V16_C1_PATHS,
    )


def validate_v17_scope(root: Path, candidate: str) -> dict[str, Any]:
    """Validate the immutable V17 hold-decision transaction at V17 and later descendants."""

    return validate_authority_scope(
        root,
        candidate,
        version="v17",
        authority_path=V17_AUTHORITY_PATH,
        schema_version="hexalith.conversations.v17-implementation-hold-decision-authority.v1",
        baseline=V17_BASELINE_COMMIT,
        expected_c1_paths=V17_C1_PATHS,
        extra_c2_paths=(V17_RECORD_PATH,),
    )


def v23_marker_complete(root: Path, candidate: str) -> bool:
    """Require a single ordered terminal V23 marker in committed architecture bytes."""

    architecture = candidate_blob(root, candidate, V23_ARCHITECTURE_PATH)
    begins = [match.start() for match in re.finditer(re.escape(V23_BEGIN), architecture)]
    ends = [match.start() for match in re.finditer(re.escape(V23_END), architecture)]
    if not begins and not ends:
        return False
    if len(begins) != 1 or len(ends) != 1 or ends[0] < begins[0]:
        raise BoundaryError("EVIDENCE_V23_MARKER_INCOMPLETE", f"begin={begins!r}; end={ends!r}", "BLOCKED")
    try:
        close = architecture.index(b"-->", ends[0]) + 3
    except ValueError as error:
        raise BoundaryError("EVIDENCE_V23_MARKER_INCOMPLETE", "V23 END is not closed", "BLOCKED") from error
    if architecture[close:].strip():
        raise BoundaryError("EVIDENCE_V23_MARKER_NOT_TERMINAL", "content follows V23", "BLOCKED")
    return True


def v23_tree_record(root: Path, commit: str, relative_path: str) -> tuple[str, str, str]:
    """Read one exact raw tree record for the independent V23 trust host."""

    content = run_git(root, "ls-tree", "-z", commit, "--", safe_relative_path(relative_path)).stdout
    rows = [row for row in content.split(b"\0") if row]
    if len(rows) != 1:
        raise BoundaryError("EVIDENCE_V23_TREE_ENTRY_UNAVAILABLE", relative_path, "BLOCKED")
    try:
        header, raw_path = rows[0].split(b"\t", 1)
        mode, kind, object_id = header.decode("ascii", errors="strict").split(" ")
        observed = raw_path.decode("utf-8", errors="strict")
    except (UnicodeError, ValueError) as error:
        raise BoundaryError("EVIDENCE_V23_TREE_ENTRY_INVALID", relative_path, "BLOCKED") from error
    if observed != relative_path or re.fullmatch(r"[0-9a-f]{40}", object_id) is None:
        raise BoundaryError("EVIDENCE_V23_TREE_ENTRY_INVALID", relative_path, "BLOCKED")
    return mode, kind, object_id


def load_v23_publisher(root: Path, evaluated: str) -> tuple[Any, str]:
    """Independently authenticate V23 topology and publisher bytes before compilation."""

    try:
        publications = tuple(
            row
            for row in run_git(
                root,
                "log",
                "--format=%H",
                "--diff-filter=A",
                evaluated,
                "--",
                V23_REQUEST_PATH,
            ).stdout.decode("ascii", errors="strict").splitlines()
            if row
        )
    except UnicodeError as error:
        raise BoundaryError("EVIDENCE_V23_REQUEST_HISTORY_INVALID", str(error), "BLOCKED") from error
    if len(publications) != 1 or re.fullmatch(r"[0-9a-f]{40}", publications[0]) is None:
        raise BoundaryError(
            "EVIDENCE_V23_REQUEST_PUBLICATION_MISSING",
            f"expected one publication; observed={publications!r}",
            "BLOCKED",
        )
    publication = publications[0]
    require_single_parent(root, publication, V23_TOOLING_BASELINE, "EVIDENCE_V23_TOOLING_PARENT_MISMATCH")
    observed_paths = changed_paths(root, V23_TOOLING_BASELINE, publication)
    if observed_paths != V23_TOOLING_PATHS:
        missing = sorted(set(V23_TOOLING_PATHS) - set(observed_paths))
        unexpected = sorted(set(observed_paths) - set(V23_TOOLING_PATHS))
        raise BoundaryError("EVIDENCE_V23_TOOLING_SCOPE_DRIFT", f"missing={missing!r}; unexpected={unexpected!r}")
    for path in V23_TOOLING_PATHS:
        mode, kind, _object_id = v23_tree_record(root, publication, path)
        if (mode, kind) != ("100644", "blob"):
            raise BoundaryError("EVIDENCE_V23_TOOLING_MODE_DRIFT", f"{path}: {mode} {kind}")
    workflow_digest = sha256(candidate_blob(root, publication, ".github/workflows/planning-authority-preflight.yml"))
    if workflow_digest != V23_WORKFLOW_SHA256:
        raise BoundaryError(
            "EVIDENCE_V23_WORKFLOW_IDENTITY_MISMATCH",
            f"expected={V23_WORKFLOW_SHA256}; observed={workflow_digest}",
            "BLOCKED",
        )
    request_content = candidate_blob(root, publication, V23_REQUEST_PATH)
    if candidate_blob(root, evaluated, V23_REQUEST_PATH) != request_content:
        raise BoundaryError("EVIDENCE_V23_REQUEST_DESCENDANT_DRIFT", V23_REQUEST_PATH)
    schema_content = candidate_blob(root, publication, V23_SCHEMA_PATH)
    observed_schema_digest = sha256(schema_content)
    if observed_schema_digest != V23_SCHEMA_SHA256:
        raise BoundaryError(
            "EVIDENCE_V23_SCHEMA_IDENTITY_MISMATCH",
            f"expected={V23_SCHEMA_SHA256}; observed={observed_schema_digest}",
            "BLOCKED",
        )
    if candidate_blob(root, evaluated, V23_SCHEMA_PATH) != schema_content:
        raise BoundaryError("EVIDENCE_V23_SCHEMA_DESCENDANT_DRIFT", V23_SCHEMA_PATH, "BLOCKED")
    publisher_content = candidate_blob(root, publication, V23_PUBLISHER_PATH)
    observed_digest = sha256(publisher_content)
    if observed_digest != V23_PUBLISHER_SHA256:
        raise BoundaryError(
            "EVIDENCE_V23_PUBLISHER_IDENTITY_MISMATCH",
            f"expected={V23_PUBLISHER_SHA256}; observed={observed_digest}",
            "BLOCKED",
        )
    if candidate_blob(root, evaluated, V23_PUBLISHER_PATH) != publisher_content:
        raise BoundaryError("EVIDENCE_V23_PUBLISHER_DESCENDANT_DRIFT", V23_PUBLISHER_PATH, "BLOCKED")
    spec = importlib.util.spec_from_loader("evidence_trusted_v23_entry_authority", loader=None)
    if spec is None:
        raise BoundaryError("EVIDENCE_V23_PUBLISHER_LOAD_FAILED", V23_PUBLISHER_PATH, "BLOCKED")
    module = importlib.util.module_from_spec(spec)
    module.__file__ = f"{publication}:{V23_PUBLISHER_PATH}"
    try:
        exec(compile(publisher_content, module.__file__, "exec"), module.__dict__)
    except BaseException as error:
        raise BoundaryError("EVIDENCE_V23_PUBLISHER_LOAD_FAILED", str(error), "BLOCKED") from error
    if not callable(getattr(module, "validate_request", None)) or not callable(
        getattr(module, "resolve_published_authority", None)
    ):
        raise BoundaryError("EVIDENCE_V23_PUBLISHER_INTERFACE_INVALID", V23_PUBLISHER_PATH, "BLOCKED")
    return module, publication


def valid_v23_observed(
    observed: Any,
    result: Any,
    route: str,
    expected_candidate: str | None,
    expected_publication: str | None,
) -> bool:
    """Validate the closed request or progressive authority observation contract."""

    if not isinstance(observed, dict):
        return False
    if route == "request":
        expected = {
            "requestPublication": expected_publication,
            "protectedMain": V23_PROTECTED_MAIN,
            "historicalV22Candidate": V23_HISTORICAL_V22_CANDIDATE,
        }
        return result == "BLOCKED" and set(observed) == set(expected) and all(
            isinstance(observed[field], str)
            and re.fullmatch(r"[0-9a-f]{40}", observed[field]) is not None
            and (expected[field] is None or observed[field] == expected[field])
            for field in expected
        )
    if route != "authority":
        return False
    fields = (
        "candidateCommit",
        "candidateTree",
        "authorityPublication",
        "sourceCommit",
        "changedPaths",
        "ownerSignature",
    )
    keys = frozenset(observed)
    if result == "PASS":
        if keys != frozenset(fields):
            return False
    elif keys not in {frozenset(fields[:count]) for count in range(len(fields) + 1)}:
        return False
    if any(
        not isinstance(observed.get(field), str) or re.fullmatch(r"[0-9a-f]{40}", observed[field]) is None
        for field in fields[:4]
        if field in observed
    ):
        return False
    if (
        expected_candidate is not None
        and "candidateCommit" in observed
        and observed["candidateCommit"] != expected_candidate
    ):
        return False
    changed_paths = observed.get("changedPaths")
    if "changedPaths" in observed and (
        not isinstance(changed_paths, list)
        or any(not isinstance(path, str) or not path for path in changed_paths)
        or len(set(changed_paths)) != len(changed_paths)
    ):
        return False
    signature = observed.get("ownerSignature")
    if "ownerSignature" in observed and (
        not isinstance(signature, dict)
        or set(signature) != {"status", "principal", "fingerprint", "authorIdentity"}
        or signature
        != {
            "status": "G",
            "principal": V23_TRUSTED_SSH_PRINCIPAL,
            "fingerprint": V23_TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": V23_TRUSTED_OWNER_IDENTITY,
        }
    ):
        return False
    return not (
        result == "PASS"
        and (
            observed["candidateCommit"] != observed["authorityPublication"]
            or changed_paths != [V23_ARCHITECTURE_PATH, V23_AUTHORITY_PATH]
        )
    )


def v23_blockers_match_ledger(
    result: Any,
    ledger: list[dict[str, Any]],
    blockers: list[dict[str, Any]],
) -> bool:
    """Require every blocker to identify the same non-PASS assertion and detail."""

    if result == "PASS":
        return blockers == []
    if not blockers:
        return False
    for blocker in blockers:
        index = blocker.get("assertionIndex")
        if type(index) is not int or index < 0 or index >= len(ledger):
            return False
        assertion = ledger[index]
        if (
            assertion.get("id") != blocker.get("code")
            or assertion.get("state") not in {"FAIL", "BLOCKED"}
            or assertion.get("detail") != blocker.get("detail")
        ):
            return False
    return len({blocker["assertionIndex"] for blocker in blockers}) == len(blockers)


def valid_v23_ledger_inventory(
    route: str,
    result: str,
    ledger: list[dict[str, Any]],
    blockers: list[dict[str, Any]],
) -> bool:
    """Require the route-specific fixed ledger and blocker inventories."""

    if route == "request":
        return (
            result == "BLOCKED"
            and [(row.get("id"), row.get("subject"), row.get("state")) for row in ledger]
            == list(V23_REQUEST_LEDGER)
            and [row.get("code") for row in blockers] == list(V23_REQUEST_BLOCKERS)
            and [row.get("assertionIndex") for row in blockers] == [6, 7, 8, 9, 10]
        )
    if route != "authority":
        return False
    if result == "PASS":
        expected = [
            (f"V23.AUTHORITY.{index:02d}", subject)
            for index, subject in enumerate(V23_AUTHORITY_GATE_SUBJECTS, start=1)
        ] + [("V23.AUTHORITY.SIGNATURE", "trusted-owner-publication-signature")]
        return blockers == [] and [
            (row.get("id"), row.get("subject")) for row in ledger
        ] == expected and all(row.get("state") == "PASS" for row in ledger)
    return (
        len(ledger) == 1
        and len(blockers) == 1
        and ledger[0].get("state") == result
        and ledger[0].get("subject") == "v23-entry-authority"
        and ledger[0].get("id") == blockers[0].get("code")
        and ledger[0].get("detail") == blockers[0].get("detail")
        and blockers[0].get("assertionIndex") == 0
    )


def validate_v23_result(
    document: Any,
    expected_result: str,
    *,
    route: str = "authority",
    expected_candidate: str | None = None,
    expected_publication: str | None = None,
) -> dict[str, Any]:
    """Enforce the entire host-owned V23 result contract after trusted dispatch."""

    expected_keys = {
        "schemaVersion",
        "result",
        "exitCode",
        "effectiveHold",
        "implementationHold",
        "observed",
        "assertionLedger",
        "blockers",
        "ownerApprovalClaimed",
        "releaseAuthorized",
        "pushAuthorized",
        "executionAllowed",
        "storyExecution",
    }
    if not isinstance(document, dict) or set(document) != expected_keys:
        raise BoundaryError("EVIDENCE_V23_RESULT_INVALID", "result shape mismatch", "BLOCKED")
    result = document.get("result")
    exit_codes = {"PASS": 0, "FAIL": 1, "BLOCKED": 2}
    passed = result == "PASS"
    ledger = document.get("assertionLedger")
    blockers = document.get("blockers")
    if (
        document.get("schemaVersion") != V23_RESULT_SCHEMA_VERSION
        or result != expected_result
        or document.get("exitCode") != exit_codes.get(result)
        or document.get("effectiveHold") != ("EXECUTION_ALLOWED" if passed else "ACTIVE")
        or document.get("implementationHold") != ("EXECUTION_ALLOWED" if passed else "ACTIVE")
        or not valid_v23_observed(
            document.get("observed"),
            result,
            route,
            expected_candidate,
            expected_publication,
        )
        or not isinstance(ledger, list)
        or not ledger
        or any(
            not isinstance(row, dict)
            or set(row) != {"id", "subject", "state", "detail"}
            or row.get("state") not in {"PASS", "FAIL", "BLOCKED"}
            or not all(isinstance(row.get(key), str) and bool(row[key]) for key in ("id", "subject", "detail"))
            for row in ledger
        )
        or not isinstance(blockers, list)
        or any(
            not isinstance(row, dict)
            or set(row) != {"code", "detail", "assertionIndex"}
            or not all(isinstance(row.get(key), str) and bool(row[key]) for key in ("code", "detail"))
            or type(row.get("assertionIndex")) is not int
            for row in blockers
        )
        or document.get("ownerApprovalClaimed") is not passed
        or document.get("releaseAuthorized") is not False
        or document.get("pushAuthorized") is not False
        or document.get("executionAllowed") is not passed
        or document.get("storyExecution")
        != {"7.1": passed, "7.2": False, "7.3": False, "7.4": False}
        or (passed and (blockers or any(row["state"] != "PASS" for row in ledger)))
        or (result == "FAIL" and (not blockers or not any(row["state"] == "FAIL" for row in ledger)))
        or (result == "BLOCKED" and (not blockers or not any(row["state"] == "BLOCKED" for row in ledger)))
        or len({row["id"] for row in ledger}) != len(ledger)
        or len({row["code"] for row in blockers}) != len(blockers)
        or not v23_blockers_match_ledger(result, ledger, blockers)
        or not valid_v23_ledger_inventory(route, result, ledger, blockers)
    ):
        raise BoundaryError("EVIDENCE_V23_RESULT_INVALID", "closed result semantics mismatch", "BLOCKED")
    return document


def validate_v23_scope(
    root: Path,
    candidate: str,
    *,
    authority: bool,
    signature_verifier: Callable[[Path, str, str], dict[str, str]] | None = None,
) -> dict[str, Any]:
    """Validate a V23 request or authority only after independent executable authentication."""

    marker_complete = v23_marker_complete(root, candidate)
    if authority and not marker_complete:
        raise BoundaryError("EVIDENCE_V23_MARKER_INCOMPLETE", V23_ARCHITECTURE_PATH, "BLOCKED")
    if not authority and marker_complete:
        raise BoundaryError("EVIDENCE_V23_REQUEST_ROUTE_CONFLICT", "request route contains a complete authority marker", "BLOCKED")
    module, publication = load_v23_publisher(root, candidate)
    if not authority and candidate != publication:
        paths = changed_paths(root, publication, candidate)
        raise BoundaryError("EVIDENCE_V23_REQUEST_DESCENDANT_SCOPE", repr(paths), "BLOCKED")
    try:
        if authority:
            arguments: dict[str, Any] = {}
            if signature_verifier is not None:
                arguments["signature_verifier"] = signature_verifier
            result = module.resolve_published_authority(root, candidate, **arguments)
        else:
            request, observed_publication, _content = module.validate_request(root, candidate)
            if observed_publication != publication:
                raise BoundaryError(
                    "EVIDENCE_V23_REQUEST_PUBLICATION_DRIFT",
                    f"host={publication}; publisher={observed_publication}",
                    "BLOCKED",
                )
            result = module.request_check_result(request, observed_publication)
    except BoundaryError:
        raise
    except BaseException as error:
        raise BoundaryError("EVIDENCE_V23_PUBLISHER_EXECUTION_FAILED", str(error), "BLOCKED") from error
    expected_result = result.get("result") if authority and isinstance(result, dict) else "BLOCKED"
    validate_v23_result(
        result,
        expected_result,
        route="authority" if authority else "request",
        expected_candidate=candidate,
        expected_publication=candidate if authority else publication,
    )
    if authority and expected_result in ("FAIL", "BLOCKED"):
        blocker = result["blockers"][0]
        raise BoundaryError(blocker["code"], blocker["detail"], expected_result)
    return assertion(
        "V23-SCOPE-01",
        "v23-entry-authority-boundary",
        "PASS",
        route="authority" if authority else "request",
        publication=publication,
        publisherSha256=V23_PUBLISHER_SHA256,
    )


def child_failure(result: subprocess.CompletedProcess[str]) -> BoundaryError:
    """Preserve a structured child FAIL or BLOCKED result without state collapse."""

    for content in (result.stdout, result.stderr):
        try:
            document = json.loads(content)
        except (TypeError, json.JSONDecodeError):
            continue
        if not isinstance(document, dict) or document.get("result") not in ("FAIL", "BLOCKED"):
            continue
        blockers = document.get("blockers")
        blocker = blockers[0] if isinstance(blockers, list) and blockers and isinstance(blockers[0], dict) else {}
        code = blocker.get("code") if isinstance(blocker.get("code"), str) else "EVIDENCE_PUBLICATION_DRIFT"
        detail = blocker.get("detail") or blocker.get("message") or content.strip()
        return BoundaryError(code, str(detail), str(document["result"]))
    detail = (result.stderr or result.stdout).strip()
    return BoundaryError("EVIDENCE_PUBLICATION_DRIFT", detail)


def run_publication_check(root: Path, *, route: str = "legacy", candidate: str = "HEAD") -> dict[str, Any]:
    """Run the applicable deterministic publication checks without accepting skips."""

    current_commands = (
        (
            [sys.executable, str(root / "_bmad/scripts/publish_v13_current_proof_authority.py"), "--repository", str(root), "--check"],
            "V13_CURRENT_PROOF_AUTHORITY_OK",
        ),
        (
            [sys.executable, str(root / "_bmad/scripts/publish_v14_current_candidate_authority.py"), "--repository", str(root), "--check"],
            "V14_CURRENT_CANDIDATE_AUTHORITY_OK",
        ),
        (
            [
                sys.executable,
                str(root / V15_PUBLISHER_PATH),
                "--repository",
                str(root),
                "--candidate",
                candidate,
                "--check",
                "--check-installed",
            ],
            "V15_PLANNING_TOOLING_AUTHORITY_OK",
        ),
    )
    if route == "v16":
        current_commands = (
            *current_commands,
            (
                [
                    sys.executable,
                    str(root / V16_PUBLISHER_PATH),
                    "--repository",
                    str(root),
                    "--candidate",
                    candidate,
                    "--check",
                    "--check-installed",
                ],
                "V16_PLANNING_TOOLING_LIFECYCLE_OK",
            ),
        )
    if route == "v17":
        current_commands = (
            *current_commands,
            (
                [
                    sys.executable,
                    str(root / V16_PUBLISHER_PATH),
                    "--repository",
                    str(root),
                    "--candidate",
                    candidate,
                    "--check",
                    "--check-installed",
                ],
                "V16_PLANNING_TOOLING_LIFECYCLE_OK",
            ),
            (
                [
                    sys.executable,
                    str(root / V17_PUBLISHER_PATH),
                    "--repository",
                    str(root),
                    "--candidate",
                    candidate,
                    "--check",
                ],
                "HOLD_DECISION_OK",
            ),
        )
    commands = current_commands if route in ("v15", "v16", "v17") else (
        (
            [sys.executable, str(root / "_bmad/scripts/publish_v9_planning_authority.py"), "--repository", str(root), "--check"],
            "V14_PLANNING_AUTHORITY_OK",
        ),
    )
    outputs: list[str] = []
    for command, success_token in commands:
        try:
            result = subprocess.run(command, cwd=root, capture_output=True, text=True, timeout=120, check=False)
        except (OSError, subprocess.TimeoutExpired) as error:
            raise BoundaryError("EVIDENCE_PREFLIGHT_UNAVAILABLE", str(error), "BLOCKED") from error
        if result.returncode != 0:
            raise child_failure(result)
        output = result.stdout.strip()
        if success_token not in output:
            raise BoundaryError("SCOPE_NOT_EVALUATED", f"publication check emitted no {success_token} identity")
        outputs.append(output)
    return assertion("PUBLICATION-01", "deterministic-planning-publication", "PASS", output=" | ".join(outputs))


def verify(
    repository: Path,
    baseline_revision: str,
    candidate_revision: str,
    *,
    signature_verifier: Callable[[Path, str, str], dict[str, str]] | None = None,
) -> dict[str, Any]:
    """Evaluate the complete evidence boundary and return one closed result."""

    root = repository_root(repository)
    baseline = resolve_commit(root, baseline_revision, "BASELINE_UNAVAILABLE")
    candidate = resolve_commit(root, candidate_revision, "CANDIDATE_UNAVAILABLE")
    ancestry = run_git(root, "merge-base", "--is-ancestor", baseline, candidate, allowed=(0, 1))
    if ancestry.returncode != 0:
        raise BoundaryError("BASELINE_NOT_ANCESTOR", f"{baseline} is not an ancestor of {candidate}", "BLOCKED")
    paths = changed_paths(root, baseline, candidate)
    dirty_paths = worktree_paths(root)
    route = authority_route(root, candidate)
    applicable = is_applicable(paths) or route in ("v15", "v16", "v17", "v23-request", "v23-authority")
    gitlink_row = validate_gitlinks(root, baseline, candidate)
    ledger = [
        assertion("PATHS-01", "exact-changed-path-set", "PASS", paths=list(paths), count=len(paths)),
        assertion("WORKTREE-01", "reported-uncommitted-paths", "PASS", paths=list(dirty_paths), count=len(dirty_paths)),
        gitlink_row,
        validate_publication_scope(root, baseline, candidate, paths, gitlink_row),
    ]
    if route == "v23-authority":
        ledger.append(
            validate_v23_scope(
                root,
                candidate,
                authority=True,
                signature_verifier=signature_verifier,
            )
        )
    elif route == "v23-request":
        ledger.append(validate_v23_scope(root, candidate, authority=False))
    elif route == "v17":
        ledger.append(validate_v17_scope(root, candidate))
        ledger.append(validate_v16_scope(root, candidate))
    elif route == "v16":
        ledger.append(validate_v16_scope(root, candidate))
    elif route == "v15":
        ledger.append(validate_v15_scope(root, candidate))
    else:
        ledger.append(assertion("AUTHORITY-ROUTE-01", "candidate-tree-authority-route", "PASS", route=route))
    if not applicable:
        return {
            "schemaVersion": SCHEMA,
            "result": "not-applicable",
            "repository": str(root),
            "baseline": baseline,
            "candidate": candidate,
            "changedPaths": list(paths),
            "worktreePaths": list(dirty_paths),
            "assertionLedger": ledger,
            "blockers": [],
        }
    committed_reader = lambda _base, relative: candidate_blob(root, candidate, relative)
    ledger.extend(validate_active_routes(root, committed_reader))
    ledger.append(
        validate_context(
            root,
            candidate_blob(root, candidate, "_bmad-output/implementation-artifacts/epic-6-context.md"),
        )
    )
    ledger.extend(validate_context_workflows(root, committed_reader))
    ledger.append(
        validate_csharp_signature_guard(
            root,
            candidate_blob(
                root,
                candidate,
                "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
            ),
        )
    )
    if route not in ("v23-request", "v23-authority"):
        ledger.append(run_publication_check(root, route=route, candidate=candidate))
    if not ledger:
        raise BoundaryError("SCOPE_NOT_EVALUATED", "applicable scope produced an empty assertion ledger")
    return {
        "schemaVersion": SCHEMA,
        "result": "PASS",
        "repository": str(root),
        "baseline": baseline,
        "candidate": candidate,
        "changedPaths": list(paths),
        "worktreePaths": list(dirty_paths),
        "assertionLedger": ledger,
        "blockers": [],
    }


def failure_document(repository: Path, error: BoundaryError) -> dict[str, Any]:
    """Create one parseable fail-closed result."""

    blocker = {"code": error.code, "state": error.state, "message": error.message, "path": error.path}
    return {
        "schemaVersion": SCHEMA,
        "result": error.state,
        "repository": str(repository.resolve()),
        "baseline": None,
        "candidate": None,
        "changedPaths": [],
        "worktreePaths": [],
        "assertionLedger": [
            {"id": error.code, "subject": error.path or "evidence-boundary", "state": error.state, "message": error.message}
        ],
        "blockers": [blocker],
    }


def build_parser() -> argparse.ArgumentParser:
    """Build the stable command-line contract."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--baseline", required=True)
    parser.add_argument("--candidate", default="HEAD")
    parser.add_argument("--output")
    return parser


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the gate and preserve PASS/FAIL/BLOCKED/not-applicable semantics."""

    args = build_parser().parse_args(arguments)
    repository = Path(args.repository)
    try:
        document = verify(repository, args.baseline, args.candidate)
    except BoundaryError as error:
        document = failure_document(repository, error)
    except (UnicodeError, ValueError, TypeError) as error:
        document = failure_document(
            repository,
            BoundaryError("EVIDENCE_MALFORMED", str(error), "BLOCKED"),
        )
    content = json.dumps(document, indent=2, ensure_ascii=False) + "\n"
    if args.output:
        output = Path(args.output)
        if not output.is_absolute():
            output = repository.resolve() / output
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(content, encoding="utf-8")
    sys.stdout.write(content)
    return {"PASS": 0, "not-applicable": 0, "FAIL": 1, "BLOCKED": 2}[document["result"]]


if __name__ == "__main__":
    raise SystemExit(main())
