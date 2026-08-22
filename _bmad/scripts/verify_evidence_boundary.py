#!/usr/bin/env python3
"""Fail closed at planning/evidence lifecycle boundaries."""

from __future__ import annotations

import argparse
import hashlib
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
ACTIVE_ROUTE_PATHS = (
    ".agents/skills/bmad-build/step-04-review.md",
    ".agents/skills/bmad-build/step-05-present.md",
    ".agents/skills/bmad-build/step-oneshot.md",
    ".agents/skills/bmad-build-auto/step-04-review.md",
    ".agents/skills/bmad-dev-story/SKILL.md",
    ".agents/skills/bmad-code-review/steps/step-04-present.md",
    ".claude/skills/bmad-build/step-04-review.md",
    ".claude/skills/bmad-build/step-05-present.md",
    ".claude/skills/bmad-build/step-oneshot.md",
    ".claude/skills/bmad-build-auto/step-04-review.md",
    ".claude/skills/bmad-dev-story/SKILL.md",
    ".claude/skills/bmad-code-review/steps/step-04-present.md",
)
LOGICAL_ROUTE_PATHS = tuple(path.split("/skills/", 1)[1] for path in ACTIVE_ROUTE_PATHS[:6])
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
    "bmad-dev-story/SKILL.md": "<action>Update the story Status to: \"review\"</action>",
    "bmad-code-review/steps/step-04-present.md": "set `{new_status}` = `done`",
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


def run_git(repository: Path, *arguments: str, allowed: tuple[int, ...] = (0,)) -> subprocess.CompletedProcess[bytes]:
    """Run one bounded non-interactive Git command."""

    try:
        result = subprocess.run(
            ("git", "-C", str(repository), *arguments),
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=GIT_TIMEOUT_SECONDS,
            env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
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


def nul_paths(content: bytes) -> set[str]:
    """Parse normalized NUL-separated paths."""

    return {safe_relative_path(value.decode("utf-8", errors="strict")) for value in content.split(b"\0") if value}


def changed_paths(repository: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return the exact committed plus staged, unstaged, and untracked path set."""

    paths = nul_paths(run_git(repository, "diff", "--name-only", "-z", baseline, candidate, "--").stdout)
    paths.update(nul_paths(run_git(repository, "diff", "--name-only", "-z", "--").stdout))
    paths.update(nul_paths(run_git(repository, "diff", "--cached", "--name-only", "-z", "--").stdout))
    paths.update(nul_paths(run_git(repository, "ls-files", "--others", "--exclude-standard", "-z").stdout))
    return tuple(sorted(paths))


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
    if len(ACTIVE_ROUTE_PATHS) != 12 or len(set(ACTIVE_ROUTE_PATHS)) != 12:
        raise BoundaryError("EVIDENCE_ROUTE_INVENTORY_DRIFT", "active route inventory is not exactly twelve paths")
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


def validate_context(root: Path) -> dict[str, Any]:
    """Require generated Epic 6 context frontmatter and active parity identities."""

    path = root / "_bmad-output/implementation-artifacts/epic-6-context.md"
    try:
        text = path.read_text(encoding="utf-8")
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
    return assertion("CONTEXT-01", "epic-6-context-frontmatter", "PASS", sha256=sha256(path.read_bytes()))


def validate_context_workflows(root: Path) -> list[dict[str, Any]]:
    """Require both workflow trees to preserve and validate identity frontmatter."""

    ledger: list[dict[str, Any]] = []
    for logical in CONTEXT_WORKFLOW_PATHS:
        agents_path = root / ".agents/skills" / logical
        claude_path = root / ".claude/skills" / logical
        try:
            agents = agents_path.read_bytes()
            claude = claude_path.read_bytes()
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


def validate_v15_scope(
    root: Path,
    baseline: str,
    candidate: str,
    paths: Sequence[str],
    gitlink_row: dict[str, Any],
) -> dict[str, Any]:
    """Apply the additive V15 two-commit exact boundary when its authority is present."""

    if V15_AUTHORITY_PATH not in paths:
        return assertion("V15-SCOPE-01", "v15-planning-tooling-boundary", "PASS", applied=False)
    try:
        content = run_git(root, "show", f"{candidate}:{V15_AUTHORITY_PATH}").stdout
        document = json.loads(content.decode("utf-8"))
    except (BoundaryError, UnicodeError, json.JSONDecodeError) as error:
        raise BoundaryError("EVIDENCE_V15_AUTHORITY_INVALID", str(error), path=V15_AUTHORITY_PATH) from error
    if not isinstance(document, dict):
        raise BoundaryError("EVIDENCE_V15_AUTHORITY_INVALID", "authority must be an object", path=V15_AUTHORITY_PATH)
    publication = document.get("publication")
    candidate_commit = document.get("candidateCommit")
    if (
        document.get("schemaVersion")
        != "hexalith.conversations.v15-planning-tooling-environment-authority.v1"
        or document.get("baselineCommit") != baseline
        or not isinstance(candidate_commit, str)
        or not isinstance(publication, dict)
    ):
        raise BoundaryError("EVIDENCE_V15_AUTHORITY_INVALID", "closed identity mismatch", path=V15_AUTHORITY_PATH)
    c1 = resolve_commit(root, candidate_commit, "EVIDENCE_V15_CANDIDATE_UNAVAILABLE")
    parent = resolve_commit(root, f"{candidate}^", "EVIDENCE_V15_PUBLICATION_PARENT_UNAVAILABLE")
    if parent != c1:
        raise BoundaryError("EVIDENCE_V15_PUBLICATION_PARENT_MISMATCH", f"expected {c1}; observed {parent}", "BLOCKED")
    c1_paths = publication.get("c1Paths")
    combined = publication.get("combinedPaths")
    if (
        not isinstance(c1_paths, list)
        or not c1_paths
        or not all(isinstance(path, str) for path in c1_paths)
        or len(c1_paths) != len(set(c1_paths))
        or not isinstance(combined, list)
        or not combined
        or not all(isinstance(path, str) for path in combined)
        or len(combined) != len(set(combined))
        or publication.get("c2Path") != V15_AUTHORITY_PATH
        or publication.get("changedGitlinks") != []
    ):
        raise BoundaryError("EVIDENCE_V15_AUTHORITY_INVALID", "closed publication contract mismatch")
    normalized_c1 = tuple(sorted(safe_relative_path(path) for path in c1_paths))
    normalized_combined = tuple(sorted(safe_relative_path(path) for path in combined))
    observed_c1 = changed_paths(root, baseline, c1)
    observed_c2 = changed_paths(root, c1, candidate)
    observed = tuple(sorted(paths))
    if observed_c1 != normalized_c1 or observed_c2 != (V15_AUTHORITY_PATH,) or observed != normalized_combined:
        missing = sorted(set(normalized_combined) - set(observed))
        unexpected = sorted(set(observed) - set(normalized_combined))
        raise BoundaryError(
            "EVIDENCE_V15_SCOPE_DRIFT",
            f"c1={observed_c1!r} c2={observed_c2!r} missing={missing!r} unexpected={unexpected!r}",
        )
    if gitlink_row.get("paths"):
        raise BoundaryError("EVIDENCE_GITLINK_SET_DRIFT", repr(gitlink_row["paths"]))
    return assertion("V15-SCOPE-01", "v15-planning-tooling-boundary", "PASS", applied=True, count=len(observed))


def run_publication_check(root: Path, *, v15: bool = False) -> dict[str, Any]:
    """Run the applicable deterministic publication checks without accepting skips."""

    commands = (
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
                "--check",
                "--check-installed",
            ],
            "V15_PLANNING_TOOLING_AUTHORITY_OK",
        ),
    ) if v15 else (
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
            detail = (result.stderr or result.stdout).strip()
            raise BoundaryError("EVIDENCE_PUBLICATION_DRIFT", detail)
        output = result.stdout.strip()
        if success_token not in output:
            raise BoundaryError("SCOPE_NOT_EVALUATED", f"publication check emitted no {success_token} identity")
        outputs.append(output)
    return assertion("PUBLICATION-01", "deterministic-planning-publication", "PASS", output=" | ".join(outputs))


def verify(repository: Path, baseline_revision: str, candidate_revision: str) -> dict[str, Any]:
    """Evaluate the complete evidence boundary and return one closed result."""

    root = repository_root(repository)
    baseline = resolve_commit(root, baseline_revision, "BASELINE_UNAVAILABLE")
    candidate = resolve_commit(root, candidate_revision, "CANDIDATE_UNAVAILABLE")
    ancestry = run_git(root, "merge-base", "--is-ancestor", baseline, candidate, allowed=(0, 1))
    if ancestry.returncode != 0:
        raise BoundaryError("BASELINE_NOT_ANCESTOR", f"{baseline} is not an ancestor of {candidate}", "BLOCKED")
    paths = changed_paths(root, baseline, candidate)
    applicable = is_applicable(paths)
    gitlink_row = validate_gitlinks(root, baseline, candidate)
    ledger = [
        assertion("PATHS-01", "exact-changed-path-set", "PASS", paths=list(paths), count=len(paths)),
        gitlink_row,
        validate_publication_scope(root, baseline, candidate, paths, gitlink_row),
        validate_v15_scope(root, baseline, candidate, paths, gitlink_row),
    ]
    if not applicable:
        return {
            "schemaVersion": SCHEMA,
            "result": "not-applicable",
            "repository": str(root),
            "baseline": baseline,
            "candidate": candidate,
            "changedPaths": list(paths),
            "assertionLedger": ledger,
            "blockers": [],
        }
    ledger.extend(validate_active_routes(root))
    ledger.append(validate_context(root))
    ledger.extend(validate_context_workflows(root))
    ledger.append(validate_csharp_signature_guard(root))
    ledger.append(run_publication_check(root, v15=V15_AUTHORITY_PATH in paths))
    if not ledger:
        raise BoundaryError("SCOPE_NOT_EVALUATED", "applicable scope produced an empty assertion ledger")
    return {
        "schemaVersion": SCHEMA,
        "result": "PASS",
        "repository": str(root),
        "baseline": baseline,
        "candidate": candidate,
        "changedPaths": list(paths),
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
