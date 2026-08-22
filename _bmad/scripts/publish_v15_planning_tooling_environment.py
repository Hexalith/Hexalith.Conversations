#!/usr/bin/env python3
"""Publish and validate the additive V15 planning-tooling environment authority."""

from __future__ import annotations

import argparse
from copy import deepcopy
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import tomllib
from typing import Any, Sequence


SCHEMA_VERSION = "hexalith.conversations.v15-planning-tooling-environment-authority.v1"
AUTHORITY_ID = "V15-PLANNING-TOOLING-ENVIRONMENT"
BASELINE_COMMIT = "6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4"
AUTHORITY_PATH = "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json"
AUTHORITY_SCHEMA_PATH = "_bmad/schemas/v15-planning-tooling-environment-authority-v1.schema.json"
PUBLICATION_COMMIT = "08a4bdcc5a18067f8f93c777055d8097987a9da2"
PUBLICATION_SHA256 = "bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea"
PINNED_CANDIDATE_COMMIT = "4586df9d35e1d50df401cd98cf62e4435d89007d"
V9_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V9_AUTHORITY_SHA256 = "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3"
V9_PLANNING_CANDIDATE = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5"
V9_BUNDLE_DIGEST = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055"
IR0_PATH = "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md"
IR0_SHA256 = "862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad"
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
    (IR0_PATH, IR0_SHA256),
)
C1_PATHS = tuple(
    sorted(
        (
            ".github/workflows/planning-authority-preflight.yml",
            "_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md",
            AUTHORITY_SCHEMA_PATH,
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
COMBINED_PATHS = tuple(sorted((*C1_PATHS, AUTHORITY_PATH)))
EXPECTED_PACKAGE_NAMES = (
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
APPROVED_PACKAGES = {
    "jsonschema": {
        "version": "4.26.0",
        "sdist": {
            "url": "https://files.pythonhosted.org/packages/b3/fc/e067678238fa451312d4c62bf6e6cf5ec56375422aee02f9cb5f909b3047/jsonschema-4.26.0.tar.gz",
            "sha256": "0c26707e2efad8aa1bfc5b7ce170f3fccc2e4918ff85989ba9ffa9facb2be326",
        },
        "wheels": (
            {
                "url": "https://files.pythonhosted.org/packages/69/90/f63fb5873511e014207a475e2bb4e8b2e570d655b00ac19a9a0ca0a385ee/jsonschema-4.26.0-py3-none-any.whl",
                "sha256": "d489f15263b8d200f8387e64b4c3a75f06629559fb73deb8fdfb525f2dab50ce",
            },
        ),
    },
    "pytest": {
        "version": "9.1.1",
        "sdist": {
            "url": "https://files.pythonhosted.org/packages/e4/47/b9efed96c114afcfa3c9d3fe98a76a1d14c74a9e266d397cf6eb64be5e01/pytest-9.1.1.tar.gz",
            "sha256": "1088fbde8f2b49d95a549a195707afa7a76a3ce9bcadc26b6d71f0ffda5fe313",
        },
        "wheels": (
            {
                "url": "https://files.pythonhosted.org/packages/24/25/1de2678b631f5a49215c6c96fff41ba892b0a34df68d6d80292b1b48aa7f/pytest-9.1.1-py3-none-any.whl",
                "sha256": "37a86b45efb9a47a61a36449063e8e18d0cab3161329fc099eb21783169c4f0c",
            },
        ),
    },
}
RESULT_STATES = ("PASS", "FAIL", "BLOCKED", "not-applicable")


class ToolingAuthorityError(RuntimeError):
    """A stable fail-closed V15 publication result."""

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
    """Require a normalized repository-relative POSIX path."""

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
        raise ToolingAuthorityError("TOOLING_PATH_ESCAPE", repr(value), "BLOCKED")
    return value


def run_git(root: Path, *arguments: str, allowed: tuple[int, ...] = (0,)) -> subprocess.CompletedProcess[bytes]:
    """Run bounded, non-interactive Git and preserve unavailable history as BLOCKED."""

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
        raise ToolingAuthorityError("TOOLING_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise ToolingAuthorityError("TOOLING_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def resolve_commit(root: Path, revision: str, code: str) -> str:
    """Resolve one exact commit object."""

    try:
        commit = run_git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode().strip()
    except ToolingAuthorityError as error:
        raise ToolingAuthorityError(code, error.detail, "BLOCKED") from error
    if re.fullmatch(r"[0-9a-f]{40}", commit) is None:
        raise ToolingAuthorityError(code, commit, "BLOCKED")
    return commit


def commit_parents(root: Path, commit: str, code: str) -> tuple[str, ...]:
    """Return every parent and require an unambiguous commit record."""

    record = run_git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").strip().split()
    if not record or record[0] != commit or any(re.fullmatch(r"[0-9a-f]{40}", item) is None for item in record):
        raise ToolingAuthorityError(code, repr(record), "BLOCKED")
    return tuple(record[1:])


def require_single_parent(root: Path, commit: str, expected: str, code: str) -> None:
    """Require exactly one parent with the expected identity."""

    parents = commit_parents(root, commit, code)
    if len(parents) != 1:
        raise ToolingAuthorityError(code, f"expected one parent; observed {parents!r}", "BLOCKED")
    if parents[0] != expected:
        raise ToolingAuthorityError(code, f"expected {expected}; observed {parents[0]}", "BLOCKED")


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require one committed publication to remain reachable from the evaluated candidate."""

    result = run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1))
    if result.returncode != 0:
        raise ToolingAuthorityError(code, f"{ancestor} is not an ancestor of {descendant}", "BLOCKED")


def candidate_blob(root: Path, candidate: str, relative_path: str) -> bytes:
    """Read one exact candidate blob."""

    safe_path(relative_path)
    try:
        return run_git(root, "show", f"{candidate}:{relative_path}").stdout
    except ToolingAuthorityError as error:
        raise ToolingAuthorityError("TOOLING_CANDIDATE_PATH_MISSING", relative_path, "BLOCKED") from error


def tree_mode(root: Path, candidate: str, relative_path: str) -> str:
    """Read the raw Git tree mode for one candidate path."""

    output = run_git(root, "ls-tree", candidate, "--", safe_path(relative_path)).stdout.decode().rstrip("\n")
    match = re.fullmatch(r"([0-7]{6}) (?:blob|commit) [0-9a-f]{40}\t(.+)", output)
    if match is None or match.group(2) != relative_path:
        raise ToolingAuthorityError("TOOLING_MODE_UNAVAILABLE", relative_path, "BLOCKED")
    return match.group(1)


def changed_paths(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return the exact committed path set for a transaction edge."""

    content = run_git(root, "diff", "--name-only", "-z", baseline, candidate, "--").stdout
    try:
        return tuple(sorted(safe_path(item.decode("utf-8", errors="strict")) for item in content.split(b"\0") if item))
    except UnicodeError as error:
        raise ToolingAuthorityError("TOOLING_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error


def changed_gitlinks(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Derive changed gitlinks exclusively from raw mode 160000 records."""

    content = run_git(
        root,
        "diff",
        "--raw",
        "--no-abbrev",
        "--no-renames",
        "-z",
        baseline,
        candidate,
        "--",
    ).stdout
    records = [item for item in content.split(b"\0") if item]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise ToolingAuthorityError("TOOLING_GITLINK_DIFF_MALFORMED", "incomplete raw record", "BLOCKED")
        try:
            metadata = records[index].decode("ascii", errors="strict")
            path = safe_path(records[index + 1].decode("utf-8", errors="strict"))
        except UnicodeError as error:
            raise ToolingAuthorityError("TOOLING_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error
        fields = metadata.split()
        if len(fields) >= 5 and (fields[0] == ":160000" or fields[1] == "160000"):
            paths.append(path)
    return tuple(sorted(set(paths)))


def lock_document(content: bytes) -> dict[str, Any]:
    """Parse the UTF-8 lock document with a stable failure."""

    try:
        document = tomllib.loads(content.decode("utf-8", errors="strict"))
    except (UnicodeError, tomllib.TOMLDecodeError) as error:
        raise ToolingAuthorityError("TOOLING_LOCK_INVALID", str(error)) from error
    if not isinstance(document, dict):
        raise ToolingAuthorityError("TOOLING_LOCK_INVALID", "lock document must be a table")
    return document


def manifest_document(content: bytes) -> dict[str, Any]:
    """Parse the UTF-8 project manifest with a stable failure."""

    try:
        document = tomllib.loads(content.decode("utf-8", errors="strict"))
    except (UnicodeError, tomllib.TOMLDecodeError) as error:
        raise ToolingAuthorityError("TOOLING_MANIFEST_INVALID", str(error)) from error
    if not isinstance(document, dict):
        raise ToolingAuthorityError("TOOLING_MANIFEST_INVALID", "manifest document must be a table")
    return document


def package_rows(root: Path, candidate: str) -> list[dict[str, Any]]:
    """Validate manifest/lock parity, frozen graph identity, versions, URLs, and PyPI hashes."""

    manifest = manifest_document(candidate_blob(root, candidate, "pyproject.toml"))
    baseline_manifest = manifest_document(candidate_blob(root, BASELINE_COMMIT, "pyproject.toml"))
    expected_dependencies = [f"{name}=={APPROVED_PACKAGES[name]['version']}" for name in APPROVED_PACKAGES]
    project = manifest.get("project")
    baseline_project = baseline_manifest.get("project")
    if not isinstance(project, dict) or not isinstance(baseline_project, dict):
        raise ToolingAuthorityError("TOOLING_MANIFEST_INVALID", "project table missing")
    if project.get("dependencies") != expected_dependencies:
        raise ToolingAuthorityError("TOOLING_MANIFEST_VERSION_MISMATCH", repr(project.get("dependencies")))
    normalized_baseline = deepcopy(baseline_manifest)
    normalized_project = normalized_baseline.get("project")
    if not isinstance(normalized_project, dict):
        raise ToolingAuthorityError("TOOLING_MANIFEST_INVALID", "baseline project table missing")
    normalized_project["dependencies"] = expected_dependencies
    if manifest != normalized_baseline:
        raise ToolingAuthorityError("TOOLING_MANIFEST_SCOPE_DRIFT", "fields beyond the approved pins changed")

    lock = lock_document(candidate_blob(root, candidate, "uv.lock"))
    baseline_lock = lock_document(candidate_blob(root, BASELINE_COMMIT, "uv.lock"))
    packages = lock.get("package")
    baseline_packages = baseline_lock.get("package")
    if not isinstance(packages, list) or not isinstance(baseline_packages, list):
        raise ToolingAuthorityError("TOOLING_LOCK_INVALID", "package table missing")
    if not all(isinstance(record, dict) and isinstance(record.get("name"), str) for record in packages):
        raise ToolingAuthorityError("TOOLING_LOCK_INVALID", "every package row requires a string name")
    if not all(isinstance(record, dict) and isinstance(record.get("name"), str) for record in baseline_packages):
        raise ToolingAuthorityError("TOOLING_LOCK_INVALID", "every baseline package row requires a string name")
    names = tuple(sorted(record["name"] for record in packages))
    if names != EXPECTED_PACKAGE_NAMES or len(packages) != len(EXPECTED_PACKAGE_NAMES):
        raise ToolingAuthorityError("TOOLING_LOCK_GRAPH_DRIFT", repr(names))
    by_name = {record["name"]: record for record in packages}
    baseline_by_name = {record["name"]: record for record in baseline_packages}
    if len(by_name) != len(packages) or len(baseline_by_name) != len(baseline_packages):
        raise ToolingAuthorityError("TOOLING_LOCK_GRAPH_DRIFT", "duplicate package name")
    for name in set(by_name) - set(APPROVED_PACKAGES) - {"hexalith-conversations-planning"}:
        if by_name[name] != baseline_by_name.get(name):
            raise ToolingAuthorityError("TOOLING_LOCK_GRAPH_DRIFT", name)
    if "hexalith-conversations-planning" not in baseline_by_name:
        raise ToolingAuthorityError("TOOLING_LOCK_GRAPH_DRIFT", "baseline root package missing")
    root_record = deepcopy(baseline_by_name["hexalith-conversations-planning"])
    metadata = root_record.get("metadata")
    if not isinstance(metadata, dict):
        raise ToolingAuthorityError("TOOLING_LOCK_INVALID", "root package metadata missing")
    metadata["requires-dist"] = [
        {"name": name, "specifier": f"=={APPROVED_PACKAGES[name]['version']}"} for name in APPROVED_PACKAGES
    ]
    if by_name["hexalith-conversations-planning"] != root_record:
        raise ToolingAuthorityError("TOOLING_LOCK_GRAPH_DRIFT", "root dependency metadata")

    result: list[dict[str, Any]] = []
    for name, approved in APPROVED_PACKAGES.items():
        record = by_name.get(name)
        if not isinstance(record, dict) or record.get("version") != approved["version"]:
            raise ToolingAuthorityError("TOOLING_LOCK_VERSION_MISMATCH", name)
        if record.get("source") != {"registry": "https://pypi.org/simple"}:
            raise ToolingAuthorityError("TOOLING_LOCK_SOURCE_MISMATCH", name)
        observed_sdist = record.get("sdist")
        observed_wheels = record.get("wheels")
        expected_sdist = approved["sdist"]
        expected_wheels = approved["wheels"]
        if not isinstance(observed_sdist, dict) or {
            "url": observed_sdist.get("url"),
            "sha256": str(observed_sdist.get("hash", "")).removeprefix("sha256:"),
        } != expected_sdist:
            raise ToolingAuthorityError("TOOLING_LOCK_HASH_MISMATCH", f"{name}:sdist")
        if not isinstance(observed_wheels, list) or not all(isinstance(wheel, dict) for wheel in observed_wheels):
            raise ToolingAuthorityError("TOOLING_LOCK_INVALID", f"{name}:wheels")
        normalized_wheels = tuple(
            {"url": wheel.get("url"), "sha256": str(wheel.get("hash", "")).removeprefix("sha256:")}
            for wheel in observed_wheels
        )
        if normalized_wheels != expected_wheels:
            raise ToolingAuthorityError("TOOLING_LOCK_HASH_MISMATCH", f"{name}:wheels")
        if record.get("dependencies") != baseline_by_name[name].get("dependencies"):
            raise ToolingAuthorityError("TOOLING_LOCK_GRAPH_DRIFT", f"{name}:dependencies")
        result.append(
            {
                "name": name,
                "version": approved["version"],
                "registry": "https://pypi.org/simple",
                "sdist": expected_sdist,
                "wheels": list(expected_wheels),
            }
        )
    return result


def validate_predecessors(root: Path, candidate: str) -> list[dict[str, str]]:
    """Pin immutable V9-V14 and the committed READY IR-0 report."""

    rows: list[dict[str, str]] = []
    for path, expected_digest in IMMUTABLE_AUTHORITIES:
        observed = sha256(candidate_blob(root, candidate, path))
        if observed != expected_digest:
            code = "TOOLING_IR0_DRIFT" if path == IR0_PATH else "TOOLING_PREDECESSOR_DRIFT"
            raise ToolingAuthorityError(code, f"{path}: expected {expected_digest}; observed {observed}")
        rows.append({"path": path, "sha256": observed, "mode": tree_mode(root, candidate, path)})
    if any(row["mode"] != "100644" for row in rows):
        raise ToolingAuthorityError("TOOLING_MODE_DRIFT", "immutable authority mode")
    try:
        bundle = json.loads(candidate_blob(root, candidate, V9_AUTHORITY_PATH))
    except (UnicodeError, json.JSONDecodeError) as error:
        raise ToolingAuthorityError("TOOLING_PREDECESSOR_INVALID", str(error)) from error
    if bundle.get("planningCandidate") != V9_PLANNING_CANDIDATE or bundle.get("bundleDigest") != V9_BUNDLE_DIGEST:
        raise ToolingAuthorityError("TOOLING_PREDECESSOR_DRIFT", "V9 identity or digest")
    frontmatter = candidate_blob(root, candidate, IR0_PATH).decode("utf-8").split("\n---\n", 1)[0]
    if not re.search(r"(?m)^result: READY$", frontmatter) or not re.search(r"(?m)^effective_hold: ACTIVE$", frontmatter):
        raise ToolingAuthorityError("TOOLING_IR0_DRIFT", "expected READY with effective hold ACTIVE")
    return rows


def validate_candidate(root: Path, candidate: str) -> tuple[list[dict[str, Any]], list[dict[str, str]], list[dict[str, Any]]]:
    """Validate C1 as the direct, exact, zero-gitlink child of the frozen baseline."""

    require_single_parent(root, candidate, BASELINE_COMMIT, "TOOLING_CANDIDATE_PARENT_MISMATCH")
    observed_paths = changed_paths(root, BASELINE_COMMIT, candidate)
    if AUTHORITY_PATH in observed_paths:
        raise ToolingAuthorityError("TOOLING_SELF_REFERENCE", AUTHORITY_PATH, "BLOCKED")
    if observed_paths != C1_PATHS:
        missing = sorted(set(C1_PATHS) - set(observed_paths))
        unexpected = sorted(set(observed_paths) - set(C1_PATHS))
        raise ToolingAuthorityError("TOOLING_SCOPE_DRIFT", f"missing={missing!r} unexpected={unexpected!r}")
    gitlinks = changed_gitlinks(root, BASELINE_COMMIT, candidate)
    if gitlinks:
        raise ToolingAuthorityError("TOOLING_GITLINK_DRIFT", repr(gitlinks))
    files = []
    for path in C1_PATHS:
        mode = tree_mode(root, candidate, path)
        if mode != "100644":
            raise ToolingAuthorityError("TOOLING_MODE_DRIFT", f"{path}: {mode}")
        files.append({"path": path, "sha256": sha256(candidate_blob(root, candidate, path)), "mode": mode})
    packages = package_rows(root, candidate)
    immutable = validate_predecessors(root, candidate)
    return files, immutable, packages


def render_authority(root: Path, candidate: str) -> dict[str, Any]:
    """Recompute the closed, candidate-bound V15 authority."""

    files, immutable, packages = validate_candidate(root, candidate)
    ledger = [
        {"id": "V15-BASELINE", "subject": "baseline-and-candidate-parent", "state": "PASS"},
        {"id": "V15-SCOPE", "subject": "exact-eleven-path-c1-c2-boundary", "state": "PASS"},
        {"id": "V15-GITLINKS", "subject": "raw-mode-160000-changed-set", "state": "PASS", "paths": []},
        {"id": "V15-ENVIRONMENT", "subject": "manifest-lock-version-hash-parity", "state": "PASS"},
        {"id": "V15-PREDECESSOR", "subject": "immutable-v9-v14-authority", "state": "PASS"},
        {"id": "V15-IR0", "subject": "recorded-ready-result-with-active-hold", "state": "PASS"},
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
        "environment": {
            "packageCount": len(EXPECTED_PACKAGE_NAMES),
            "packageNames": list(EXPECTED_PACKAGE_NAMES),
            "packages": packages,
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
    """Validate the authority against the schema committed in C1."""

    try:
        import jsonschema
    except ImportError as error:
        raise ToolingAuthorityError("TOOLING_SCHEMA_UNAVAILABLE", str(error), "BLOCKED") from error
    try:
        schema = json.loads(candidate_blob(root, candidate, AUTHORITY_SCHEMA_PATH))
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except (UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise ToolingAuthorityError("TOOLING_AUTHORITY_SCHEMA_INVALID", str(error)) from error


def validate_installed_versions() -> None:
    """Require installed distribution metadata to match the approved lock."""

    for name, approved in APPROVED_PACKAGES.items():
        try:
            observed = importlib.metadata.version(name)
        except importlib.metadata.PackageNotFoundError as error:
            raise ToolingAuthorityError("TOOLING_INSTALLED_METADATA_UNAVAILABLE", name, "BLOCKED") from error
        if observed != approved["version"]:
            raise ToolingAuthorityError("TOOLING_INSTALLED_VERSION_MISMATCH", f"{name}: {observed}")


def locate_publication(root: Path, evaluated_candidate: str, publication_revision: str | None) -> str:
    """Locate the immutable V15 C2 from committed history, never from live bytes."""

    if publication_revision:
        publication = resolve_commit(root, publication_revision, "TOOLING_PUBLICATION_UNAVAILABLE")
        candidates = (publication,)
    else:
        output = run_git(
            root,
            "log",
            "--format=%H",
            "--diff-filter=A",
            evaluated_candidate,
            "--",
            AUTHORITY_PATH,
        ).stdout.decode("ascii", errors="strict")
        candidates = tuple(line for line in output.splitlines() if line)
    if len(candidates) != 1:
        raise ToolingAuthorityError(
            "TOOLING_PUBLICATION_UNAVAILABLE",
            f"expected one V15 publication; observed {candidates!r}",
            "BLOCKED",
        )
    publication = candidates[0]
    pinned_reachable = run_git(
        root,
        "merge-base",
        "--is-ancestor",
        PUBLICATION_COMMIT,
        evaluated_candidate,
        allowed=(0, 1),
    ).returncode == 0
    if pinned_reachable and publication != PUBLICATION_COMMIT:
        raise ToolingAuthorityError(
            "TOOLING_PUBLICATION_IDENTITY_MISMATCH",
            f"expected {PUBLICATION_COMMIT}; observed {publication}",
        )
    if pinned_reachable and sha256(candidate_blob(root, publication, AUTHORITY_PATH)) != PUBLICATION_SHA256:
        raise ToolingAuthorityError("TOOLING_AUTHORITY_DRIFT", AUTHORITY_PATH)
    return publication


def validate_publication(root: Path, candidate: str, publication: str, evaluated_candidate: str) -> str:
    """Require immutable C2 topology and unchanged authority bytes at a descendant."""

    require_single_parent(root, publication, candidate, "TOOLING_PUBLICATION_PARENT_MISMATCH")
    if changed_paths(root, candidate, publication) != (AUTHORITY_PATH,):
        raise ToolingAuthorityError("TOOLING_PUBLICATION_SCOPE_DRIFT", repr(changed_paths(root, candidate, publication)))
    if changed_paths(root, BASELINE_COMMIT, publication) != COMBINED_PATHS:
        raise ToolingAuthorityError("TOOLING_SCOPE_DRIFT", repr(changed_paths(root, BASELINE_COMMIT, publication)))
    if changed_gitlinks(root, candidate, publication) or changed_gitlinks(root, BASELINE_COMMIT, publication):
        raise ToolingAuthorityError("TOOLING_GITLINK_DRIFT", "C2 introduced a gitlink")
    if tree_mode(root, publication, AUTHORITY_PATH) != "100644":
        raise ToolingAuthorityError("TOOLING_MODE_DRIFT", AUTHORITY_PATH)
    require_ancestor(root, publication, evaluated_candidate, "TOOLING_PUBLICATION_NOT_ANCESTOR")
    published = candidate_blob(root, publication, AUTHORITY_PATH)
    current = candidate_blob(root, evaluated_candidate, AUTHORITY_PATH)
    if published != current:
        raise ToolingAuthorityError("TOOLING_AUTHORITY_DESCENDANT_DRIFT", AUTHORITY_PATH)
    return publication


def publish(
    root: Path,
    *,
    candidate_revision: str | None,
    check: bool,
    publication_revision: str | None = None,
    check_installed: bool = False,
) -> dict[str, Any]:
    """Publish C2 bytes or validate the committed two-commit transaction."""

    root = root.resolve()
    if check:
        evaluated_candidate = resolve_commit(root, candidate_revision or "HEAD", "TOOLING_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated_candidate, publication_revision)
        try:
            existing_bytes = candidate_blob(root, publication, AUTHORITY_PATH)
            existing = json.loads(existing_bytes.decode("utf-8", errors="strict"))
            pinned_candidate = existing["candidateCommit"]
        except (UnicodeError, json.JSONDecodeError, KeyError, TypeError) as error:
            raise ToolingAuthorityError("TOOLING_AUTHORITY_UNAVAILABLE", str(error), "BLOCKED") from error
        if not isinstance(pinned_candidate, str):
            raise ToolingAuthorityError("TOOLING_AUTHORITY_INVALID", "candidateCommit", "BLOCKED")
        candidate = resolve_commit(root, pinned_candidate, "TOOLING_CANDIDATE_UNAVAILABLE")
        if publication == PUBLICATION_COMMIT and candidate != PINNED_CANDIDATE_COMMIT:
            raise ToolingAuthorityError(
                "TOOLING_CANDIDATE_BINDING_MISMATCH",
                f"expected {PINNED_CANDIDATE_COMMIT}; observed {candidate}",
            )
    else:
        candidate = resolve_commit(root, candidate_revision or "HEAD", "TOOLING_CANDIDATE_UNAVAILABLE")
        existing = None
    document = render_authority(root, candidate)
    validate_schema(root, candidate, document)
    if check_installed:
        validate_installed_versions()
    target = root / AUTHORITY_PATH
    if check:
        if existing != document or existing_bytes != json_bytes(document):
            raise ToolingAuthorityError("TOOLING_AUTHORITY_DRIFT", AUTHORITY_PATH)
        validate_publication(root, candidate, publication, evaluated_candidate)
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        temporary = target.with_suffix(target.suffix + ".tmp")
        try:
            temporary.write_bytes(json_bytes(document))
            os.replace(temporary, target)
        except OSError as error:
            temporary.unlink(missing_ok=True)
            raise ToolingAuthorityError("TOOLING_AUTHORITY_WRITE_FAILED", str(error), "BLOCKED") from error
    return document


def failure_document(root: Path, error: ToolingAuthorityError) -> dict[str, Any]:
    """Return one parseable, non-vacuous failure result."""

    return {
        "schemaVersion": "hexalith.conversations.v15-planning-tooling-environment-result.v1",
        "result": error.state,
        "repository": str(root.resolve()),
        "assertionLedger": [
            {"id": error.code, "subject": "v15-planning-tooling-environment", "state": error.state, "detail": error.detail}
        ],
        "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
    }


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the V15 publisher/checker."""

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
            f"V15_PLANNING_TOOLING_AUTHORITY_OK CANDIDATE={document['candidateCommit']} "
            f"PACKAGES={len(document['environment']['packageNames'])} PATHS={len(document['publication']['combinedPaths'])}"
        )
        return 0
    except ToolingAuthorityError as error:
        sys.stdout.write(json.dumps(failure_document(root, error), indent=2, ensure_ascii=False) + "\n")
        return 2 if error.state == "BLOCKED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
