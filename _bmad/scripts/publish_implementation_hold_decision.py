#!/usr/bin/env python3
"""Publish and validate the release-owner implementation-hold decision and V17 authority."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
from typing import Any, Sequence


RECORD_SCHEMA_VERSION = "hexalith.conversations.implementation-hold.v1"
AUTHORITY_SCHEMA_VERSION = "hexalith.conversations.v17-implementation-hold-decision-authority.v1"
AUTHORITY_ID = "V17-IMPLEMENTATION-HOLD-DECISION"
PREDECESSOR_AUTHORITY_ID = "V16-PLANNING-TOOLING-LIFECYCLE"
BASELINE_COMMIT = "074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf"
RECORD_PATH = "_bmad-output/planning-artifacts/implementation-hold-v1.json"
AUTHORITY_PATH = "_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json"
RECORD_SCHEMA_PATH = "_bmad/schemas/implementation-hold-v1.schema.json"
AUTHORITY_SCHEMA_PATH = "_bmad/schemas/v17-implementation-hold-decision-authority-v1.schema.json"
PUBLISHER_PATH = "_bmad/scripts/publish_implementation_hold_decision.py"
PUBLISHER_TEST_PATH = "_bmad/scripts/tests/test_publish_implementation_hold_decision.py"
V9_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V9_AUTHORITY_SHA256 = "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3"
V9_PLANNING_CANDIDATE = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5"
V9_BUNDLE_DIGEST = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055"
V13_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json"
V14_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json"
V16_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json"
V16_AUTHORITY_SHA256 = "5b71e6fbf8851f790af92f0a0d056d7f7e04b3b9749c3a2f3f4ef5f87312d45a"
SLICE_PATH = "_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json"
IR0_PATH = "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md"
IR0_SHA256 = "862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad"
UNLOCKED_SLICE = "7.1-SCHEMAS"
DECISION_ID = "IMPLEMENTATION-HOLD-LIFT-2026-09-08"
DECISION_DATE = "2026-09-08"
RESULT_STATES = ("PASS", "FAIL", "BLOCKED", "not-applicable")
NON_CLAIM_READINESS_RERUN = (
    "The readiness rerun required by AR-2 has not been performed; this decision does not satisfy or claim it."
)
DECISION_AUTHORITY = {
    "owner": "Release owner",
    "name": "Jerome - release-owner hold lift 2026-09-08",
    "mechanism": "Repository-recorded independent release-owner decision; no cryptographic-signature claim",
}
RATIONALE = (
    "The independent IR-0 assessment of 2026-08-22 returned READY against planning candidate "
    "1e9a61126d3b7a55b514b7c7c8942d5af03355e5 with the effective hold ACTIVE, and states that a separate "
    "release-owner decision is required to lift it. This record is that decision. It is candidate-bound and "
    "unlocks only the 7.1-SCHEMAS schema checkpoint."
)
NON_CLAIMS = (
    NON_CLAIM_READINESS_RERUN,
    "This decision does not mark Story 7.1 done, produce a story final record, or unlock any successor beyond 7.1-SCHEMAS.",
    "This decision does not authorize a release, a push, or any change to sprint-status row states.",
    "This decision does not re-authorize, reissue, or alter IR-0 or any V1-V16 authority.",
)
C1_PATHS = tuple(
    sorted(
        (
            RECORD_SCHEMA_PATH,
            AUTHORITY_SCHEMA_PATH,
            PUBLISHER_PATH,
            PUBLISHER_TEST_PATH,
            "_bmad/scripts/verify_evidence_boundary.py",
            "_bmad/scripts/tests/test_verify_evidence_boundary.py",
        )
    )
)
C2_PATHS = tuple(sorted((RECORD_PATH, AUTHORITY_PATH)))
COMBINED_PATHS = tuple(sorted((*C1_PATHS, *C2_PATHS)))
IMMUTABLE_AUTHORITIES = (
    (V9_AUTHORITY_PATH, V9_AUTHORITY_SHA256),
    (
        "_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json",
        "c082cde6923e9831eea768be6c547ca1ab87ed91244185b505bdf3ae1c116dcc",
    ),
    (V13_AUTHORITY_PATH, "f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f"),
    (V14_AUTHORITY_PATH, "e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f"),
    (
        "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json",
        "bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea",
    ),
    (V16_AUTHORITY_PATH, V16_AUTHORITY_SHA256),
    (IR0_PATH, IR0_SHA256),
)
NON_CLAIM_LITERAL = "create implementation-hold-v1.json"


class ImplementationHoldError(RuntimeError):
    """A stable fail-closed V17 publication result."""

    def __init__(self, code: str, detail: str, state: str = "FAIL") -> None:
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


def safe_path(value: str) -> str:
    """Require a normalized repository-relative UTF-8 POSIX path."""

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
        raise ImplementationHoldError("HOLD_PATH_ESCAPE", repr(value), "BLOCKED")
    return value


def run_git(root: Path, *arguments: str, allowed: tuple[int, ...] = (0,)) -> subprocess.CompletedProcess[bytes]:
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
        raise ImplementationHoldError("HOLD_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise ImplementationHoldError("HOLD_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def resolve_commit(root: Path, revision: str, code: str) -> str:
    """Resolve one exact commit object."""

    try:
        value = run_git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode("ascii").strip()
    except (ImplementationHoldError, UnicodeError) as error:
        detail = error.detail if isinstance(error, ImplementationHoldError) else str(error)
        raise ImplementationHoldError(code, detail, "BLOCKED") from error
    if re.fullmatch(r"[0-9a-f]{40}", value) is None:
        raise ImplementationHoldError(code, value, "BLOCKED")
    return value


def commit_parents(root: Path, commit: str, code: str) -> tuple[str, ...]:
    """Return every parent from the raw commit record."""

    try:
        record = run_git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").strip().split()
    except UnicodeError as error:
        raise ImplementationHoldError(code, str(error), "BLOCKED") from error
    if not record or record[0] != commit or any(re.fullmatch(r"[0-9a-f]{40}", item) is None for item in record):
        raise ImplementationHoldError(code, repr(record), "BLOCKED")
    return tuple(record[1:])


def require_single_parent(root: Path, commit: str, expected: str, code: str) -> None:
    """Require exactly one parent with the expected identity."""

    parents = commit_parents(root, commit, code)
    if len(parents) != 1 or parents[0] != expected:
        raise ImplementationHoldError(code, f"expected ({expected!r},); observed {parents!r}", "BLOCKED")


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require one commit to be reachable from another."""

    result = run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1))
    if result.returncode != 0:
        raise ImplementationHoldError(code, f"{ancestor} is not an ancestor of {descendant}", "BLOCKED")


def candidate_blob(root: Path, candidate: str, relative_path: str) -> bytes:
    """Read one exact committed blob."""

    safe_path(relative_path)
    try:
        return run_git(root, "show", f"{candidate}:{relative_path}").stdout
    except ImplementationHoldError as error:
        raise ImplementationHoldError("HOLD_CANDIDATE_PATH_MISSING", relative_path, "BLOCKED") from error


def changed_paths(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return an exact committed path set, excluding every worktree state."""

    content = run_git(root, "diff", "--name-only", "-z", baseline, candidate, "--").stdout
    try:
        return tuple(sorted(safe_path(item.decode("utf-8", errors="strict")) for item in content.split(b"\0") if item))
    except UnicodeError as error:
        raise ImplementationHoldError("HOLD_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error


def changed_gitlinks(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Derive changed gitlinks exclusively from raw Git mode 160000 records."""

    content = run_git(root, "diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--").stdout
    records = [item for item in content.split(b"\0") if item]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise ImplementationHoldError("HOLD_GITLINK_DIFF_MALFORMED", "incomplete raw record", "BLOCKED")
        try:
            metadata = records[index].decode("ascii", errors="strict")
            path = safe_path(records[index + 1].decode("utf-8", errors="strict"))
        except UnicodeError as error:
            raise ImplementationHoldError("HOLD_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error
        fields = metadata.split()
        if len(fields) >= 5 and (fields[0] == ":160000" or fields[1] == "160000"):
            paths.append(path)
    return tuple(sorted(set(paths)))


def tree_mode(root: Path, candidate: str, relative_path: str) -> str:
    """Read the raw Git mode for one committed path."""

    output = run_git(root, "ls-tree", candidate, "--", safe_path(relative_path)).stdout.decode("utf-8").rstrip("\n")
    match = re.fullmatch(r"([0-7]{6}) (?:blob|commit) [0-9a-f]{40}\t(.+)", output)
    if match is None or match.group(2) != relative_path:
        raise ImplementationHoldError("HOLD_MODE_UNAVAILABLE", relative_path, "BLOCKED")
    return match.group(1)


def parse_json(content: bytes, code: str) -> dict[str, Any]:
    """Parse a UTF-8 JSON object with one stable failure code."""

    try:
        document = json.loads(content.decode("utf-8", errors="strict"))
    except (UnicodeError, json.JSONDecodeError) as error:
        raise ImplementationHoldError(code, str(error)) from error
    if not isinstance(document, dict):
        raise ImplementationHoldError(code, "document must be an object")
    return document


def validate_predecessors(root: Path, candidate: str) -> list[dict[str, str]]:
    """Require every carried authority to keep its exact bytes and raw mode."""

    immutable: list[dict[str, str]] = []
    for path, digest in IMMUTABLE_AUTHORITIES:
        observed = sha256(candidate_blob(root, candidate, path))
        mode = tree_mode(root, candidate, path)
        if observed != digest or mode != "100644":
            raise ImplementationHoldError("HOLD_PREDECESSOR_DRIFT", f"{path}: {observed} {mode}")
        immutable.append({"path": path, "sha256": digest, "mode": mode})
    for path in (V13_AUTHORITY_PATH, V14_AUTHORITY_PATH):
        document = parse_json(candidate_blob(root, candidate, path), "HOLD_PREDECESSOR_DRIFT")
        prohibitions = document.get("prohibitions") or document.get("nonClaims")
        if not isinstance(prohibitions, list) or NON_CLAIM_LITERAL not in prohibitions:
            raise ImplementationHoldError("HOLD_PREDECESSOR_DRIFT", f"{path}: non-claim literal absent")
    return immutable


def validate_binding(root: Path, candidate: str) -> None:
    """Require the decision's bound candidate, bundle digest and IR-0 identity to hold."""

    bundle = parse_json(candidate_blob(root, candidate, V9_AUTHORITY_PATH), "HOLD_BINDING_STALE")
    if (
        bundle.get("planningCandidate") != V9_PLANNING_CANDIDATE
        or bundle.get("bundleDigest") != V9_BUNDLE_DIGEST
    ):
        raise ImplementationHoldError(
            "HOLD_BINDING_STALE",
            f"planningCandidate={bundle.get('planningCandidate')!r} bundleDigest={bundle.get('bundleDigest')!r}",
        )
    if sha256(candidate_blob(root, candidate, IR0_PATH)) != IR0_SHA256:
        raise ImplementationHoldError("HOLD_BINDING_STALE", IR0_PATH)
    ir0 = candidate_blob(root, candidate, IR0_PATH).decode("utf-8", errors="strict")
    frontmatter = ir0.split("\n---\n", 1)[0]
    if not re.search(r"(?m)^result: READY$", frontmatter) or not re.search(r"(?m)^effective_hold: ACTIVE$", frontmatter):
        raise ImplementationHoldError("HOLD_IR0_DRIFT", "expected READY with hold ACTIVE")
    slice_document = parse_json(candidate_blob(root, candidate, SLICE_PATH), "HOLD_SLICE_DRIFT")
    requirement = slice_document.get("holdRequirement")
    if (
        not isinstance(requirement, dict)
        or requirement.get("effectiveState") != "LIFTED"
        or requirement.get("recordPath") != RECORD_PATH
    ):
        raise ImplementationHoldError("HOLD_SLICE_DRIFT", f"{SLICE_PATH}: {requirement!r}")
    if slice_document.get("sliceId") != UNLOCKED_SLICE:
        raise ImplementationHoldError("HOLD_SLICE_DRIFT", f"{SLICE_PATH}: sliceId")


def validate_candidate(root: Path, candidate: str) -> list[dict[str, str]]:
    """Validate V17 C1 as the direct exact child of the baseline."""

    require_single_parent(root, candidate, BASELINE_COMMIT, "HOLD_C1_PARENT_MISMATCH")
    observed = changed_paths(root, BASELINE_COMMIT, candidate)
    for generated in C2_PATHS:
        if generated in observed:
            raise ImplementationHoldError("HOLD_SELF_REFERENCE", generated, "BLOCKED")
    if observed != C1_PATHS:
        missing = sorted(set(C1_PATHS) - set(observed))
        unexpected = sorted(set(observed) - set(C1_PATHS))
        raise ImplementationHoldError("HOLD_C1_SCOPE_DRIFT", f"missing={missing!r} unexpected={unexpected!r}")
    if changed_gitlinks(root, BASELINE_COMMIT, candidate):
        raise ImplementationHoldError("HOLD_GITLINK_DRIFT", "V17 C1 contains a gitlink")
    files: list[dict[str, str]] = []
    for path in C1_PATHS:
        mode = tree_mode(root, candidate, path)
        if mode != "100644":
            raise ImplementationHoldError("HOLD_MODE_DRIFT", f"{path}: {mode}")
        files.append({"path": path, "sha256": sha256(candidate_blob(root, candidate, path)), "mode": mode})
    return files


def render_record() -> dict[str, Any]:
    """Recompute the closed candidate-bound release-owner decision record."""

    return {
        "schemaVersion": RECORD_SCHEMA_VERSION,
        "decisionId": DECISION_ID,
        "decisionDate": DECISION_DATE,
        "decisionAuthority": dict(DECISION_AUTHORITY),
        "scope": {
            "unlocks": [UNLOCKED_SLICE],
            "global": False,
            "planningCandidate": V9_PLANNING_CANDIDATE,
            "bundleDigest": V9_BUNDLE_DIGEST,
            "ir0Sha256": IR0_SHA256,
            "staleOnDrift": True,
        },
        "expiry": {"kind": "candidate-bound", "calendarExpiry": None},
        "ir0Assessment": {
            "path": IR0_PATH,
            "sha256": IR0_SHA256,
            "result": "READY",
            "effectiveHoldAtAssessment": "ACTIVE",
        },
        "rationale": RATIONALE,
        "nonClaims": list(NON_CLAIMS),
        "effectiveState": "LIFTED",
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True, "skipsAllowed": False},
        "result": "PASS",
        "assertionLedger": [
            {"id": "HOLD-DECISION", "subject": "release-owner-decision-recorded", "state": "PASS"},
            {"id": "HOLD-BINDING", "subject": "candidate-bundle-and-ir0-binding", "state": "PASS"},
            {"id": "HOLD-SCOPE", "subject": "unlocks-7-1-schemas-only", "state": "PASS"},
            {"id": "HOLD-READINESS-RERUN", "subject": "readiness-rerun-declared-unmet", "state": "not-applicable"},
        ],
    }


def render_authority(root: Path, candidate: str, record: dict[str, Any]) -> dict[str, Any]:
    """Recompute the closed candidate-bound V17 authority."""

    files = validate_candidate(root, candidate)
    validate_binding(root, candidate)
    immutable = validate_predecessors(root, candidate)
    return {
        "schemaVersion": AUTHORITY_SCHEMA_VERSION,
        "authorityId": AUTHORITY_ID,
        "predecessorAuthorityId": PREDECESSOR_AUTHORITY_ID,
        "baselineCommit": BASELINE_COMMIT,
        "candidateCommit": candidate,
        "publication": {
            "c1Paths": list(C1_PATHS),
            "c2Paths": list(C2_PATHS),
            "combinedPaths": list(COMBINED_PATHS),
            "changedGitlinks": [],
        },
        "candidateFiles": files,
        "holdRecord": {
            "path": RECORD_PATH,
            "sha256": sha256(json_bytes(record)),
            "mode": "100644",
            "effectiveState": "LIFTED",
        },
        "predecessor": {
            "path": V9_AUTHORITY_PATH,
            "fileSha256": V9_AUTHORITY_SHA256,
            "planningCandidate": V9_PLANNING_CANDIDATE,
            "bundleDigest": V9_BUNDLE_DIGEST,
        },
        "immutableAuthorities": immutable,
        "ir0Assessment": {
            "path": IR0_PATH,
            "sha256": IR0_SHA256,
            "result": "READY",
            "effectiveHoldAtAssessment": "ACTIVE",
            "preserved": True,
        },
        "authorityEffect": {
            "implementationHold": "LIFTED",
            "ir0AuthorizationChanged": False,
            "successorActivated": True,
            "unlocks": [UNLOCKED_SLICE],
            "storyDoneAllowed": False,
            "readinessRerunSatisfied": False,
            "releaseAuthorized": False,
            "pushAuthorized": False,
        },
        "nonClaims": list(NON_CLAIMS),
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True, "skipsAllowed": False},
        "result": "PASS",
        "assertionLedger": [
            {"id": "V17-C1", "subject": "single-parent-exact-six-path-c1", "state": "PASS"},
            {"id": "V17-C2", "subject": "record-and-authority-only-c2", "state": "PASS"},
            {"id": "V17-GITLINKS", "subject": "raw-mode-160000-changed-set", "state": "PASS", "paths": []},
            {"id": "V17-PREDECESSORS", "subject": "immutable-v9-v16-and-ir0-identities", "state": "PASS"},
            {"id": "V17-BINDING", "subject": "candidate-bundle-and-ir0-binding", "state": "PASS"},
            {"id": "V17-SLICE", "subject": "slice-hold-requirement-satisfied", "state": "PASS"},
            {"id": "V17-LIFECYCLE", "subject": "lifted-hold-no-release-or-push-authority", "state": "PASS"},
        ],
    }


def validate_schema(root: Path, candidate: str, schema_path: str, document: dict[str, Any]) -> None:
    """Validate one document against its closed schema committed in C1."""

    try:
        import jsonschema
    except ImportError as error:
        raise ImplementationHoldError("HOLD_SCHEMA_UNAVAILABLE", str(error), "BLOCKED") from error
    try:
        schema = json.loads(candidate_blob(root, candidate, schema_path).decode("utf-8", errors="strict"))
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except (UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise ImplementationHoldError("HOLD_SCHEMA_INVALID", str(error)) from error


def locate_publication(root: Path, evaluated_candidate: str, publication_revision: str | None) -> str:
    """Locate the single V17 authority-addition commit in evaluated history."""

    if publication_revision:
        return resolve_commit(root, publication_revision, "HOLD_PUBLICATION_UNAVAILABLE")
    try:
        output = run_git(
            root, "log", "--format=%H", "--diff-filter=A", evaluated_candidate, "--", AUTHORITY_PATH
        ).stdout.decode("ascii", errors="strict")
    except UnicodeError as error:
        raise ImplementationHoldError("HOLD_PUBLICATION_UNAVAILABLE", str(error), "BLOCKED") from error
    candidates = tuple(line for line in output.splitlines() if line)
    if len(candidates) != 1:
        raise ImplementationHoldError(
            "HOLD_PUBLICATION_UNAVAILABLE", f"expected one V17 publication; observed {candidates!r}", "BLOCKED"
        )
    return candidates[0]


def validate_publication(root: Path, candidate: str, publication: str, evaluated_candidate: str) -> None:
    """Validate V17 C2 and unchanged published bytes at any descendant candidate."""

    require_single_parent(root, publication, candidate, "HOLD_C2_PARENT_MISMATCH")
    if changed_paths(root, candidate, publication) != C2_PATHS:
        raise ImplementationHoldError("HOLD_C2_SCOPE_DRIFT", repr(changed_paths(root, candidate, publication)))
    if changed_paths(root, BASELINE_COMMIT, publication) != COMBINED_PATHS:
        raise ImplementationHoldError("HOLD_SCOPE_DRIFT", repr(changed_paths(root, BASELINE_COMMIT, publication)))
    if changed_gitlinks(root, candidate, publication) or changed_gitlinks(root, BASELINE_COMMIT, publication):
        raise ImplementationHoldError("HOLD_GITLINK_DRIFT", "V17 C2 contains a gitlink")
    for path in C2_PATHS:
        if tree_mode(root, publication, path) != "100644":
            raise ImplementationHoldError("HOLD_MODE_DRIFT", path)
    require_ancestor(root, publication, evaluated_candidate, "HOLD_PUBLICATION_NOT_ANCESTOR")
    for path in C2_PATHS:
        if candidate_blob(root, publication, path) != candidate_blob(root, evaluated_candidate, path):
            raise ImplementationHoldError("HOLD_AUTHORITY_DESCENDANT_DRIFT", path)


def write_atomically(target: Path, content: bytes) -> None:
    """Replace one target file atomically."""

    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_suffix(target.suffix + ".tmp")
    try:
        temporary.write_bytes(content)
        os.replace(temporary, target)
    except OSError as error:
        temporary.unlink(missing_ok=True)
        raise ImplementationHoldError("HOLD_WRITE_FAILED", str(error), "BLOCKED") from error


def publish(
    root: Path, *, candidate_revision: str | None, check: bool, publication_revision: str | None = None
) -> dict[str, Any]:
    """Publish both C2 documents or validate the immutable transaction at a descendant."""

    root = root.resolve()
    if check:
        evaluated_candidate = resolve_commit(root, candidate_revision or "HEAD", "HOLD_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated_candidate, publication_revision)
        existing_authority_bytes = candidate_blob(root, publication, AUTHORITY_PATH)
        existing_record_bytes = candidate_blob(root, publication, RECORD_PATH)
        existing_authority = parse_json(existing_authority_bytes, "HOLD_AUTHORITY_INVALID")
        pinned_candidate = existing_authority.get("candidateCommit")
        if not isinstance(pinned_candidate, str):
            raise ImplementationHoldError("HOLD_AUTHORITY_INVALID", "candidateCommit")
        candidate = resolve_commit(root, pinned_candidate, "HOLD_CANDIDATE_UNAVAILABLE")
    else:
        candidate = resolve_commit(root, candidate_revision or "HEAD", "HOLD_CANDIDATE_UNAVAILABLE")
        existing_authority = None
        existing_authority_bytes = existing_record_bytes = None
    record = render_record()
    authority = render_authority(root, candidate, record)
    validate_schema(root, candidate, RECORD_SCHEMA_PATH, record)
    validate_schema(root, candidate, AUTHORITY_SCHEMA_PATH, authority)
    if check:
        if existing_record_bytes != json_bytes(record):
            raise ImplementationHoldError("HOLD_AUTHORITY_DRIFT", RECORD_PATH)
        if existing_authority != authority or existing_authority_bytes != json_bytes(authority):
            raise ImplementationHoldError("HOLD_AUTHORITY_DRIFT", AUTHORITY_PATH)
        validate_publication(root, candidate, publication, evaluated_candidate)
    else:
        write_atomically(root / RECORD_PATH, json_bytes(record))
        write_atomically(root / AUTHORITY_PATH, json_bytes(authority))
    return authority


def failure_document(root: Path, error: ImplementationHoldError) -> dict[str, Any]:
    """Return one parseable non-vacuous failure result."""

    return {
        "schemaVersion": "hexalith.conversations.v17-implementation-hold-decision-result.v1",
        "result": error.state,
        "repository": str(root.resolve()),
        "assertionLedger": [
            {"id": error.code, "subject": "v17-implementation-hold-decision", "state": error.state, "detail": error.detail}
        ],
        "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
    }


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the V17 publisher/checker."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    parser.add_argument("--candidate")
    parser.add_argument("--publication")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args(arguments)
    root = Path(args.repository)
    try:
        document = publish(
            root, candidate_revision=args.candidate, check=args.check, publication_revision=args.publication
        )
        print(
            f"HOLD_DECISION_OK CANDIDATE={document['candidateCommit']} "
            f"STATE={document['holdRecord']['effectiveState']} "
            f"UNLOCKS={','.join(document['authorityEffect']['unlocks'])} "
            f"PATHS={len(document['publication']['combinedPaths'])}"
        )
        return 0
    except ImplementationHoldError as error:
        sys.stdout.write(json.dumps(failure_document(root, error), indent=2, ensure_ascii=False) + "\n")
        return 2 if error.state == "BLOCKED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
