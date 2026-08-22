#!/usr/bin/env python3
"""Publish and validate the durable V16 planning-tooling lifecycle authority."""

from __future__ import annotations

import argparse
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
from typing import Any, Sequence


SCHEMA_VERSION = "hexalith.conversations.v16-planning-tooling-lifecycle-authority.v1"
AUTHORITY_ID = "V16-PLANNING-TOOLING-LIFECYCLE"
BASELINE_COMMIT = "08a4bdcc5a18067f8f93c777055d8097987a9da2"
AUTHORITY_PATH = "_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json"
AUTHORITY_SCHEMA_PATH = "_bmad/schemas/v16-planning-tooling-lifecycle-authority-v1.schema.json"
V15_BASELINE_COMMIT = "6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4"
V15_CANDIDATE_COMMIT = "4586df9d35e1d50df401cd98cf62e4435d89007d"
V15_PUBLICATION_COMMIT = BASELINE_COMMIT
V15_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json"
V15_AUTHORITY_SHA256 = "bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea"
V9_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V9_AUTHORITY_SHA256 = "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3"
V9_PLANNING_CANDIDATE = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5"
V9_BUNDLE_DIGEST = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055"
IR0_PATH = "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md"
IR0_SHA256 = "862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad"
PYPROJECT_SHA256 = "19a0826b998332b8fcd44fd094eab99a4a7c0c529401c9eb9c41c3e18348718d"
LOCK_SHA256 = "bc416ecf9ffca757073fb0e9e11530b39ae41ff4d736afc1ee5c926852343221"
RESULT_STATES = ("PASS", "FAIL", "BLOCKED", "not-applicable")
V15_C1_PATHS = tuple(
    sorted(
        (
            ".github/workflows/planning-authority-preflight.yml",
            "_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md",
            "_bmad/schemas/v15-planning-tooling-environment-authority-v1.schema.json",
            "_bmad/scripts/publish_v15_planning_tooling_environment.py",
            "_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py",
            "_bmad/scripts/tests/test_verify_evidence_boundary.py",
            "_bmad/scripts/verify_evidence_boundary.py",
            "pyproject.toml",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs",
            "uv.lock",
        )
    )
)
V15_COMBINED_PATHS = tuple(sorted((*V15_C1_PATHS, V15_AUTHORITY_PATH)))
C1_PATHS = tuple(
    sorted(
        (
            ".github/workflows/planning-authority-preflight.yml",
            "_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md",
            "_bmad-output/implementation-artifacts/spec-v16-correct-planning-tooling-lifecycle-authority.md",
            AUTHORITY_SCHEMA_PATH,
            "_bmad/scripts/publish_v15_planning_tooling_environment.py",
            "_bmad/scripts/publish_v16_planning_tooling_lifecycle.py",
            "_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py",
            "_bmad/scripts/tests/test_publish_v16_planning_tooling_lifecycle.py",
            "_bmad/scripts/tests/test_verify_evidence_boundary.py",
            "_bmad/scripts/verify_evidence_boundary.py",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingLifecycleAuthorityV16ValidationTest.cs",
        )
    )
)
COMBINED_PATHS = tuple(sorted((*C1_PATHS, AUTHORITY_PATH)))
IMMUTABLE_AUTHORITIES = (
    (V9_AUTHORITY_PATH, V9_AUTHORITY_SHA256),
    (
        "_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json",
        "c082cde6923e9831eea768be6c547ca1ab87ed91244185b505bdf3ae1c116dcc",
    ),
    (
        "_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json",
        "f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f",
    ),
    (
        "_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json",
        "e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f",
    ),
    (V15_AUTHORITY_PATH, V15_AUTHORITY_SHA256),
    (IR0_PATH, IR0_SHA256),
)
PACKAGE_NAMES = (
    "attrs",
    "colorama",
    "hexalith-conversations-planning",
    "iniconfig",
    "jsonschema",
    "jsonschema-specifications",
    "packaging",
    "pluggy",
    "pygments",
    "pytest",
    "referencing",
    "rpds-py",
    "typing-extensions",
)
PACKAGE_ROWS = (
    {
        "name": "jsonschema",
        "version": "4.26.0",
        "registry": "https://pypi.org/simple",
        "sdist": {
            "url": "https://files.pythonhosted.org/packages/b3/fc/e067678238fa451312d4c62bf6e6cf5ec56375422aee02f9cb5f909b3047/jsonschema-4.26.0.tar.gz",
            "sha256": "0c26707e2efad8aa1bfc5b7ce170f3fccc2e4918ff85989ba9ffa9facb2be326",
        },
        "wheels": [
            {
                "url": "https://files.pythonhosted.org/packages/69/90/f63fb5873511e014207a475e2bb4e8b2e570d655b00ac19a9a0ca0a385ee/jsonschema-4.26.0-py3-none-any.whl",
                "sha256": "d489f15263b8d200f8387e64b4c3a75f06629559fb73deb8fdfb525f2dab50ce",
            }
        ],
    },
    {
        "name": "pytest",
        "version": "9.1.1",
        "registry": "https://pypi.org/simple",
        "sdist": {
            "url": "https://files.pythonhosted.org/packages/e4/47/b9efed96c114afcfa3c9d3fe98a76a1d14c74a9e266d397cf6eb64be5e01/pytest-9.1.1.tar.gz",
            "sha256": "1088fbde8f2b49d95a549a195707afa7a76a3ce9bcadc26b6d71f0ffda5fe313",
        },
        "wheels": [
            {
                "url": "https://files.pythonhosted.org/packages/24/25/1de2678b631f5a49215c6c96fff41ba892b0a34df68d6d80292b1b48aa7f/pytest-9.1.1-py3-none-any.whl",
                "sha256": "37a86b45efb9a47a61a36449063e8e18d0cab3161329fc099eb21783169c4f0c",
            }
        ],
    },
)


class LifecycleAuthorityError(RuntimeError):
    """A stable fail-closed V16 publication result."""

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
        raise LifecycleAuthorityError("LIFECYCLE_PATH_ESCAPE", repr(value), "BLOCKED")
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
        raise LifecycleAuthorityError("LIFECYCLE_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise LifecycleAuthorityError("LIFECYCLE_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def resolve_commit(root: Path, revision: str, code: str) -> str:
    """Resolve one exact commit object."""

    try:
        value = run_git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode("ascii").strip()
    except (LifecycleAuthorityError, UnicodeError) as error:
        detail = error.detail if isinstance(error, LifecycleAuthorityError) else str(error)
        raise LifecycleAuthorityError(code, detail, "BLOCKED") from error
    if re.fullmatch(r"[0-9a-f]{40}", value) is None:
        raise LifecycleAuthorityError(code, value, "BLOCKED")
    return value


def commit_parents(root: Path, commit: str, code: str) -> tuple[str, ...]:
    """Return every parent from the raw commit record."""

    try:
        record = run_git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").strip().split()
    except UnicodeError as error:
        raise LifecycleAuthorityError(code, str(error), "BLOCKED") from error
    if not record or record[0] != commit or any(re.fullmatch(r"[0-9a-f]{40}", item) is None for item in record):
        raise LifecycleAuthorityError(code, repr(record), "BLOCKED")
    return tuple(record[1:])


def require_single_parent(root: Path, commit: str, expected: str, code: str) -> None:
    """Require exactly one parent with the expected identity."""

    parents = commit_parents(root, commit, code)
    if len(parents) != 1 or parents[0] != expected:
        raise LifecycleAuthorityError(code, f"expected ({expected!r},); observed {parents!r}", "BLOCKED")


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require one commit to be reachable from another."""

    result = run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1))
    if result.returncode != 0:
        raise LifecycleAuthorityError(code, f"{ancestor} is not an ancestor of {descendant}", "BLOCKED")


def candidate_blob(root: Path, candidate: str, relative_path: str) -> bytes:
    """Read one exact committed blob."""

    safe_path(relative_path)
    try:
        return run_git(root, "show", f"{candidate}:{relative_path}").stdout
    except LifecycleAuthorityError as error:
        raise LifecycleAuthorityError("LIFECYCLE_CANDIDATE_PATH_MISSING", relative_path, "BLOCKED") from error


def changed_paths(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return an exact committed path set, excluding every worktree state."""

    content = run_git(root, "diff", "--name-only", "-z", baseline, candidate, "--").stdout
    try:
        return tuple(sorted(safe_path(item.decode("utf-8", errors="strict")) for item in content.split(b"\0") if item))
    except UnicodeError as error:
        raise LifecycleAuthorityError("LIFECYCLE_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error


def changed_gitlinks(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Derive changed gitlinks exclusively from raw Git mode 160000 records."""

    content = run_git(root, "diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--").stdout
    records = [item for item in content.split(b"\0") if item]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise LifecycleAuthorityError("LIFECYCLE_GITLINK_DIFF_MALFORMED", "incomplete raw record", "BLOCKED")
        try:
            metadata = records[index].decode("ascii", errors="strict")
            path = safe_path(records[index + 1].decode("utf-8", errors="strict"))
        except UnicodeError as error:
            raise LifecycleAuthorityError("LIFECYCLE_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error
        fields = metadata.split()
        if len(fields) >= 5 and (fields[0] == ":160000" or fields[1] == "160000"):
            paths.append(path)
    return tuple(sorted(set(paths)))


def tree_mode(root: Path, candidate: str, relative_path: str) -> str:
    """Read the raw Git mode for one committed path."""

    output = run_git(root, "ls-tree", candidate, "--", safe_path(relative_path)).stdout.decode("utf-8").rstrip("\n")
    match = re.fullmatch(r"([0-7]{6}) (?:blob|commit) [0-9a-f]{40}\t(.+)", output)
    if match is None or match.group(2) != relative_path:
        raise LifecycleAuthorityError("LIFECYCLE_MODE_UNAVAILABLE", relative_path, "BLOCKED")
    return match.group(1)


def parse_json(content: bytes, code: str) -> dict[str, Any]:
    """Parse a UTF-8 JSON object with one stable failure code."""

    try:
        document = json.loads(content.decode("utf-8", errors="strict"))
    except (UnicodeError, json.JSONDecodeError) as error:
        raise LifecycleAuthorityError(code, str(error)) from error
    if not isinstance(document, dict):
        raise LifecycleAuthorityError(code, "document must be an object")
    return document


def validate_v15_transaction(root: Path, candidate: str) -> tuple[dict[str, Any], list[dict[str, str]]]:
    """Validate immutable V15 C1/C2 topology, bytes, packages, and predecessors."""

    require_ancestor(root, V15_PUBLICATION_COMMIT, candidate, "LIFECYCLE_V15_NOT_ANCESTOR")
    require_single_parent(root, V15_CANDIDATE_COMMIT, V15_BASELINE_COMMIT, "LIFECYCLE_V15_C1_PARENT_MISMATCH")
    require_single_parent(root, V15_PUBLICATION_COMMIT, V15_CANDIDATE_COMMIT, "LIFECYCLE_V15_C2_PARENT_MISMATCH")
    if changed_paths(root, V15_BASELINE_COMMIT, V15_CANDIDATE_COMMIT) != V15_C1_PATHS:
        raise LifecycleAuthorityError("LIFECYCLE_V15_C1_SCOPE_DRIFT", repr(changed_paths(root, V15_BASELINE_COMMIT, V15_CANDIDATE_COMMIT)))
    if changed_paths(root, V15_CANDIDATE_COMMIT, V15_PUBLICATION_COMMIT) != (V15_AUTHORITY_PATH,):
        raise LifecycleAuthorityError("LIFECYCLE_V15_C2_SCOPE_DRIFT", repr(changed_paths(root, V15_CANDIDATE_COMMIT, V15_PUBLICATION_COMMIT)))
    if changed_paths(root, V15_BASELINE_COMMIT, V15_PUBLICATION_COMMIT) != V15_COMBINED_PATHS:
        raise LifecycleAuthorityError("LIFECYCLE_V15_SCOPE_DRIFT", repr(changed_paths(root, V15_BASELINE_COMMIT, V15_PUBLICATION_COMMIT)))
    if changed_gitlinks(root, V15_BASELINE_COMMIT, V15_PUBLICATION_COMMIT):
        raise LifecycleAuthorityError("LIFECYCLE_V15_GITLINK_DRIFT", "V15 contains a gitlink")
    published = candidate_blob(root, V15_PUBLICATION_COMMIT, V15_AUTHORITY_PATH)
    if sha256(published) != V15_AUTHORITY_SHA256 or candidate_blob(root, candidate, V15_AUTHORITY_PATH) != published:
        raise LifecycleAuthorityError("LIFECYCLE_V15_AUTHORITY_DRIFT", V15_AUTHORITY_PATH)
    document = parse_json(published, "LIFECYCLE_V15_AUTHORITY_INVALID")
    if (
        document.get("schemaVersion") != "hexalith.conversations.v15-planning-tooling-environment-authority.v1"
        or document.get("authorityId") != "V15-PLANNING-TOOLING-ENVIRONMENT"
        or document.get("baselineCommit") != V15_BASELINE_COMMIT
        or document.get("candidateCommit") != V15_CANDIDATE_COMMIT
        or document.get("publication")
        != {
            "c1Paths": list(V15_C1_PATHS),
            "c2Path": V15_AUTHORITY_PATH,
            "combinedPaths": list(V15_COMBINED_PATHS),
            "changedGitlinks": [],
        }
        or document.get("environment")
        != {"packageCount": len(PACKAGE_NAMES), "packageNames": list(PACKAGE_NAMES), "packages": list(PACKAGE_ROWS)}
    ):
        raise LifecycleAuthorityError("LIFECYCLE_V15_AUTHORITY_INVALID", "closed V15 identity or environment mismatch")
    if sha256(candidate_blob(root, candidate, "pyproject.toml")) != PYPROJECT_SHA256:
        raise LifecycleAuthorityError("LIFECYCLE_PACKAGE_MANIFEST_DRIFT", "pyproject.toml")
    if sha256(candidate_blob(root, candidate, "uv.lock")) != LOCK_SHA256:
        raise LifecycleAuthorityError("LIFECYCLE_PACKAGE_LOCK_DRIFT", "uv.lock")
    immutable: list[dict[str, str]] = []
    for path, digest in IMMUTABLE_AUTHORITIES:
        observed = sha256(candidate_blob(root, candidate, path))
        mode = tree_mode(root, candidate, path)
        if observed != digest or mode != "100644":
            raise LifecycleAuthorityError("LIFECYCLE_PREDECESSOR_DRIFT", f"{path}: {observed} {mode}")
        immutable.append({"path": path, "sha256": digest, "mode": mode})
    ir0 = candidate_blob(root, candidate, IR0_PATH).decode("utf-8", errors="strict")
    frontmatter = ir0.split("\n---\n", 1)[0]
    if not re.search(r"(?m)^result: READY$", frontmatter) or not re.search(r"(?m)^effective_hold: ACTIVE$", frontmatter):
        raise LifecycleAuthorityError("LIFECYCLE_IR0_DRIFT", "expected READY with hold ACTIVE")
    return document, immutable


def validate_candidate(root: Path, candidate: str) -> tuple[list[dict[str, str]], dict[str, Any], list[dict[str, str]]]:
    """Validate V16 C1 as the direct exact child of immutable V15 C2."""

    require_single_parent(root, candidate, BASELINE_COMMIT, "LIFECYCLE_C1_PARENT_MISMATCH")
    observed = changed_paths(root, BASELINE_COMMIT, candidate)
    if AUTHORITY_PATH in observed:
        raise LifecycleAuthorityError("LIFECYCLE_SELF_REFERENCE", AUTHORITY_PATH, "BLOCKED")
    if observed != C1_PATHS:
        missing = sorted(set(C1_PATHS) - set(observed))
        unexpected = sorted(set(observed) - set(C1_PATHS))
        raise LifecycleAuthorityError("LIFECYCLE_C1_SCOPE_DRIFT", f"missing={missing!r} unexpected={unexpected!r}")
    if changed_gitlinks(root, BASELINE_COMMIT, candidate):
        raise LifecycleAuthorityError("LIFECYCLE_GITLINK_DRIFT", "V16 C1 contains a gitlink")
    files: list[dict[str, str]] = []
    for path in C1_PATHS:
        mode = tree_mode(root, candidate, path)
        if mode != "100644":
            raise LifecycleAuthorityError("LIFECYCLE_MODE_DRIFT", f"{path}: {mode}")
        files.append({"path": path, "sha256": sha256(candidate_blob(root, candidate, path)), "mode": mode})
    v15, immutable = validate_v15_transaction(root, candidate)
    return files, v15, immutable


def render_authority(root: Path, candidate: str) -> dict[str, Any]:
    """Recompute the closed candidate-bound V16 authority."""

    files, v15, immutable = validate_candidate(root, candidate)
    ledger = [
        {"id": "V16-C1", "subject": "single-parent-exact-twelve-path-c1", "state": "PASS"},
        {"id": "V16-C2", "subject": "single-parent-authority-only-c2", "state": "PASS"},
        {"id": "V16-GITLINKS", "subject": "raw-mode-160000-changed-set", "state": "PASS", "paths": []},
        {"id": "V16-V15", "subject": "immutable-original-v15-transaction", "state": "PASS"},
        {"id": "V16-ENVIRONMENT", "subject": "exact-thirteen-package-environment", "state": "PASS"},
        {"id": "V16-PREDECESSORS", "subject": "immutable-v9-v15-and-ir0-identities", "state": "PASS"},
        {"id": "V16-LIFECYCLE", "subject": "active-hold-no-release-or-push-authority", "state": "PASS"},
    ]
    return {
        "schemaVersion": SCHEMA_VERSION,
        "authorityId": AUTHORITY_ID,
        "baselineCommit": BASELINE_COMMIT,
        "candidateCommit": candidate,
        "publication": {
            "c1Paths": list(C1_PATHS),
            "c2Path": AUTHORITY_PATH,
            "combinedPaths": list(COMBINED_PATHS),
            "changedGitlinks": [],
        },
        "candidateFiles": files,
        "v15Publication": {
            "baselineCommit": V15_BASELINE_COMMIT,
            "candidateCommit": V15_CANDIDATE_COMMIT,
            "publicationCommit": V15_PUBLICATION_COMMIT,
            "path": V15_AUTHORITY_PATH,
            "sha256": V15_AUTHORITY_SHA256,
            "c1Paths": list(V15_C1_PATHS),
            "combinedPaths": list(V15_COMBINED_PATHS),
        },
        "environment": v15["environment"],
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
            "effectiveHold": "ACTIVE",
            "preserved": True,
        },
        "authorityEffect": {
            "implementationHold": "ACTIVE",
            "ir0AuthorizationChanged": False,
            "successorActivated": False,
            "releaseAuthorized": False,
            "pushAuthorized": False,
        },
        "resultSemantics": {
            "states": list(RESULT_STATES),
            "ledgerRequired": True,
            "skipsAllowed": False,
        },
        "result": "PASS",
        "assertionLedger": ledger,
    }


def validate_schema(root: Path, candidate: str, document: dict[str, Any]) -> None:
    """Validate the authority against the closed schema committed in C1."""

    try:
        import jsonschema
    except ImportError as error:
        raise LifecycleAuthorityError("LIFECYCLE_SCHEMA_UNAVAILABLE", str(error), "BLOCKED") from error
    try:
        schema = json.loads(candidate_blob(root, candidate, AUTHORITY_SCHEMA_PATH).decode("utf-8", errors="strict"))
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except (UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise LifecycleAuthorityError("LIFECYCLE_AUTHORITY_SCHEMA_INVALID", str(error)) from error


def validate_installed_versions() -> None:
    """Require installed package metadata to match the V15/V16 authority."""

    for row in PACKAGE_ROWS:
        name = row["name"]
        try:
            observed = importlib.metadata.version(name)
        except importlib.metadata.PackageNotFoundError as error:
            raise LifecycleAuthorityError("LIFECYCLE_INSTALLED_METADATA_UNAVAILABLE", name, "BLOCKED") from error
        if observed != row["version"]:
            raise LifecycleAuthorityError("LIFECYCLE_INSTALLED_VERSION_MISMATCH", f"{name}: {observed}")


def locate_publication(root: Path, evaluated_candidate: str, publication_revision: str | None) -> str:
    """Locate the single V16 authority-addition commit in evaluated history."""

    if publication_revision:
        candidates = (resolve_commit(root, publication_revision, "LIFECYCLE_PUBLICATION_UNAVAILABLE"),)
    else:
        try:
            output = run_git(
                root,
                "log",
                "--format=%H",
                "--diff-filter=A",
                evaluated_candidate,
                "--",
                AUTHORITY_PATH,
            ).stdout.decode("ascii", errors="strict")
        except UnicodeError as error:
            raise LifecycleAuthorityError("LIFECYCLE_PUBLICATION_UNAVAILABLE", str(error), "BLOCKED") from error
        candidates = tuple(line for line in output.splitlines() if line)
    if len(candidates) != 1:
        raise LifecycleAuthorityError(
            "LIFECYCLE_PUBLICATION_UNAVAILABLE",
            f"expected one V16 publication; observed {candidates!r}",
            "BLOCKED",
        )
    return candidates[0]


def validate_publication(root: Path, candidate: str, publication: str, evaluated_candidate: str) -> None:
    """Validate V16 C2 and unchanged authority bytes at any descendant candidate."""

    require_single_parent(root, publication, candidate, "LIFECYCLE_C2_PARENT_MISMATCH")
    if changed_paths(root, candidate, publication) != (AUTHORITY_PATH,):
        raise LifecycleAuthorityError("LIFECYCLE_C2_SCOPE_DRIFT", repr(changed_paths(root, candidate, publication)))
    if changed_paths(root, BASELINE_COMMIT, publication) != COMBINED_PATHS:
        raise LifecycleAuthorityError("LIFECYCLE_SCOPE_DRIFT", repr(changed_paths(root, BASELINE_COMMIT, publication)))
    if changed_gitlinks(root, candidate, publication) or changed_gitlinks(root, BASELINE_COMMIT, publication):
        raise LifecycleAuthorityError("LIFECYCLE_GITLINK_DRIFT", "V16 C2 contains a gitlink")
    if tree_mode(root, publication, AUTHORITY_PATH) != "100644":
        raise LifecycleAuthorityError("LIFECYCLE_MODE_DRIFT", AUTHORITY_PATH)
    require_ancestor(root, publication, evaluated_candidate, "LIFECYCLE_PUBLICATION_NOT_ANCESTOR")
    if candidate_blob(root, publication, AUTHORITY_PATH) != candidate_blob(root, evaluated_candidate, AUTHORITY_PATH):
        raise LifecycleAuthorityError("LIFECYCLE_AUTHORITY_DESCENDANT_DRIFT", AUTHORITY_PATH)


def publish(
    root: Path,
    *,
    candidate_revision: str | None,
    check: bool,
    publication_revision: str | None = None,
    check_installed: bool = False,
) -> dict[str, Any]:
    """Publish C2 bytes or validate the immutable transaction at a descendant."""

    root = root.resolve()
    if check:
        evaluated_candidate = resolve_commit(root, candidate_revision or "HEAD", "LIFECYCLE_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated_candidate, publication_revision)
        existing_bytes = candidate_blob(root, publication, AUTHORITY_PATH)
        existing = parse_json(existing_bytes, "LIFECYCLE_AUTHORITY_INVALID")
        pinned_candidate = existing.get("candidateCommit")
        if not isinstance(pinned_candidate, str):
            raise LifecycleAuthorityError("LIFECYCLE_AUTHORITY_INVALID", "candidateCommit")
        candidate = resolve_commit(root, pinned_candidate, "LIFECYCLE_CANDIDATE_UNAVAILABLE")
    else:
        candidate = resolve_commit(root, candidate_revision or "HEAD", "LIFECYCLE_CANDIDATE_UNAVAILABLE")
        existing = None
    document = render_authority(root, candidate)
    validate_schema(root, candidate, document)
    if check_installed:
        validate_installed_versions()
    target = root / AUTHORITY_PATH
    if check:
        if existing != document or existing_bytes != json_bytes(document):
            raise LifecycleAuthorityError("LIFECYCLE_AUTHORITY_DRIFT", AUTHORITY_PATH)
        validate_publication(root, candidate, publication, evaluated_candidate)
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        temporary = target.with_suffix(target.suffix + ".tmp")
        try:
            temporary.write_bytes(json_bytes(document))
            os.replace(temporary, target)
        except OSError as error:
            temporary.unlink(missing_ok=True)
            raise LifecycleAuthorityError("LIFECYCLE_AUTHORITY_WRITE_FAILED", str(error), "BLOCKED") from error
    return document


def failure_document(root: Path, error: LifecycleAuthorityError) -> dict[str, Any]:
    """Return one parseable non-vacuous failure result."""

    return {
        "schemaVersion": "hexalith.conversations.v16-planning-tooling-lifecycle-result.v1",
        "result": error.state,
        "repository": str(root.resolve()),
        "assertionLedger": [
            {"id": error.code, "subject": "v16-planning-tooling-lifecycle", "state": error.state, "detail": error.detail}
        ],
        "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
    }


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the V16 publisher/checker."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    parser.add_argument("--candidate")
    parser.add_argument("--publication")
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--check-installed", action="store_true")
    args = parser.parse_args(arguments)
    root = Path(args.repository)
    try:
        document = publish(
            root,
            candidate_revision=args.candidate,
            check=args.check,
            publication_revision=args.publication,
            check_installed=args.check_installed,
        )
        print(
            f"V16_PLANNING_TOOLING_LIFECYCLE_OK CANDIDATE={document['candidateCommit']} "
            f"PACKAGES={document['environment']['packageCount']} PATHS={len(document['publication']['combinedPaths'])}"
        )
        return 0
    except LifecycleAuthorityError as error:
        sys.stdout.write(json.dumps(failure_document(root, error), indent=2, ensure_ascii=False) + "\n")
        return 2 if error.state == "BLOCKED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
