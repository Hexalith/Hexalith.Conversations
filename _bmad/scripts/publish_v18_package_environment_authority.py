#!/usr/bin/env python3
"""Publish and validate the additive V18 package-environment authority."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import tomllib
from typing import Any, Sequence
import xml.etree.ElementTree as ET


SCHEMA_VERSION = "hexalith.conversations.v18-package-environment-authority.v1"
AUTHORITY_ID = "V18-PACKAGE-ENVIRONMENT-AUTHORITY"
BASELINE_COMMIT = "b819a7c43a7024295abaabd418a74f5f64cb5af0"
AUTHORITY_PATH = "_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json"
AUTHORITY_SCHEMA_PATH = "_bmad/schemas/v18-package-environment-authority-v1.schema.json"
BUILDS_PATH = "references/Hexalith.Builds"
BUILDS_CATALOG_PATH = "Props/Directory.Packages.props"
DOTNET_SDK_VERSION = "10.0.401"
UV_VERSION = "0.12.13"
ASPIRE_VERSION = "13.5.3"
TOOLKIT_DAPR_VERSION = "13.5.1-beta.751"
MICROSOFT_TEST_SDK_VERSION = "18.10.0"
RESULT_STATES = ("PASS", "FAIL", "BLOCKED", "not-applicable")
C1_PATHS = tuple(
    sorted(
        (
            ".github/workflows/planning-authority-preflight.yml",
            "_bmad-output/implementation-artifacts/spec-update-all-packages.md",
            AUTHORITY_SCHEMA_PATH,
            "_bmad/scripts/publish_v18_package_environment_authority.py",
            "_bmad/scripts/tests/test_publish_v18_package_environment_authority.py",
            "global.json",
            "package-lock.json",
            BUILDS_PATH,
            "tests/Hexalith.Conversations.Conformance.Tests/PackageEnvironmentAuthorityV18ValidationTest.cs",
            "tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs",
            "uv.lock",
        )
    )
)
CANDIDATE_FILE_PATHS = tuple(path for path in C1_PATHS if path != BUILDS_PATH)
COMBINED_PATHS = tuple(sorted((*C1_PATHS, AUTHORITY_PATH)))
SOURCE_PATHS = tuple(
    sorted(
        (
            ".github/workflows/planning-authority-preflight.yml",
            "global.json",
            "package-lock.json",
            "package.json",
            "pyproject.toml",
            "uv.lock",
        )
    )
)
PYTHON_PACKAGES = (
    ("attrs", "26.1.0"),
    ("colorama", "0.4.6"),
    ("hexalith-conversations-planning", "0.0.0"),
    ("iniconfig", "2.3.0"),
    ("jsonschema", "4.26.0"),
    ("jsonschema-specifications", "2025.9.1"),
    ("packaging", "26.3"),
    ("pluggy", "1.6.0"),
    ("pygments", "2.21.0"),
    ("pytest", "9.1.1"),
    ("referencing", "0.37.0"),
    ("rpds-py", "2026.6.3"),
    ("typing-extensions", "4.16.0"),
)
IMMUTABLE_AUTHORITIES = (
    (
        "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json",
        "bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea",
    ),
    (
        "_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json",
        "5b71e6fbf8851f790af92f0a0d056d7f7e04b3b9749c3a2f3f4ef5f87312d45a",
    ),
    (
        "_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json",
        "1444f76dad9495d4c17354a9f2f5d3ce9f456cfd254c66d4a6e77e5abf446e50",
    ),
    (
        "_bmad-output/planning-artifacts/implementation-hold-v1.json",
        "2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12",
    ),
)


class PackageAuthorityError(RuntimeError):
    """A stable fail-closed V18 publication result."""

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
        raise PackageAuthorityError("PACKAGE_PATH_ESCAPE", repr(value), "BLOCKED")
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
        raise PackageAuthorityError("PACKAGE_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise PackageAuthorityError("PACKAGE_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def resolve_commit(root: Path, revision: str, code: str) -> str:
    """Resolve one exact commit object."""

    try:
        value = run_git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode("ascii").strip()
    except (PackageAuthorityError, UnicodeError) as error:
        detail = error.detail if isinstance(error, PackageAuthorityError) else str(error)
        raise PackageAuthorityError(code, detail, "BLOCKED") from error
    if re.fullmatch(r"[0-9a-f]{40}", value) is None:
        raise PackageAuthorityError(code, value, "BLOCKED")
    return value


def commit_parents(root: Path, commit: str, code: str) -> tuple[str, ...]:
    """Return every parent from the raw commit record."""

    try:
        record = run_git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").strip().split()
    except UnicodeError as error:
        raise PackageAuthorityError(code, str(error), "BLOCKED") from error
    if not record or record[0] != commit or any(re.fullmatch(r"[0-9a-f]{40}", item) is None for item in record):
        raise PackageAuthorityError(code, repr(record), "BLOCKED")
    return tuple(record[1:])


def require_single_parent(root: Path, commit: str, expected: str, code: str) -> None:
    """Require exactly one parent with the expected identity."""

    parents = commit_parents(root, commit, code)
    if len(parents) != 1 or parents[0] != expected:
        raise PackageAuthorityError(code, f"expected ({expected!r},); observed {parents!r}", "BLOCKED")


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require one commit to be reachable from another."""

    result = run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1))
    if result.returncode != 0:
        raise PackageAuthorityError(code, f"{ancestor} is not an ancestor of {descendant}", "BLOCKED")


def candidate_blob(root: Path, candidate: str, relative_path: str) -> bytes:
    """Read one exact committed blob."""

    safe_path(relative_path)
    try:
        return run_git(root, "show", f"{candidate}:{relative_path}").stdout
    except PackageAuthorityError as error:
        raise PackageAuthorityError("PACKAGE_CANDIDATE_PATH_MISSING", relative_path, "BLOCKED") from error


def changed_paths(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return the exact committed path set, excluding worktree state."""

    content = run_git(root, "diff", "--name-only", "-z", baseline, candidate, "--").stdout
    try:
        return tuple(sorted(safe_path(item.decode("utf-8", errors="strict")) for item in content.split(b"\0") if item))
    except UnicodeError as error:
        raise PackageAuthorityError("PACKAGE_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error


def raw_tree_record(root: Path, candidate: str, relative_path: str) -> tuple[str, str, str]:
    """Read one path's raw Git mode, object type, and object identifier."""

    try:
        output = run_git(root, "ls-tree", candidate, "--", safe_path(relative_path)).stdout.decode("utf-8").rstrip("\n")
    except UnicodeError as error:
        raise PackageAuthorityError("PACKAGE_MODE_UNAVAILABLE", str(error), "BLOCKED") from error
    match = re.fullmatch(r"([0-7]{6}) (blob|commit) ([0-9a-f]{40})\t(.+)", output)
    if match is None or match.group(4) != relative_path:
        raise PackageAuthorityError("PACKAGE_MODE_UNAVAILABLE", relative_path, "BLOCKED")
    return match.group(1), match.group(2), match.group(3)


def changed_gitlinks(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Derive changed gitlinks exclusively from raw mode-160000 records."""

    content = run_git(root, "diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--").stdout
    records = [item for item in content.split(b"\0") if item]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise PackageAuthorityError("PACKAGE_GITLINK_DIFF_MALFORMED", "incomplete raw record", "BLOCKED")
        try:
            metadata = records[index].decode("ascii", errors="strict")
            path = safe_path(records[index + 1].decode("utf-8", errors="strict"))
        except UnicodeError as error:
            raise PackageAuthorityError("PACKAGE_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error
        fields = metadata.split()
        if len(fields) >= 5 and (fields[0] == ":160000" or fields[1] == "160000"):
            paths.append(path)
    return tuple(sorted(set(paths)))


def parse_json(content: bytes, code: str) -> dict[str, Any]:
    """Parse a UTF-8 JSON object with one stable failure code."""

    try:
        document = json.loads(content.decode("utf-8", errors="strict"))
    except (UnicodeError, json.JSONDecodeError) as error:
        raise PackageAuthorityError(code, str(error)) from error
    if not isinstance(document, dict):
        raise PackageAuthorityError(code, "document must be an object")
    return document


def parse_toml(content: bytes, code: str) -> dict[str, Any]:
    """Parse a UTF-8 TOML object with one stable failure code."""

    try:
        document = tomllib.loads(content.decode("utf-8", errors="strict"))
    except (UnicodeError, tomllib.TOMLDecodeError) as error:
        raise PackageAuthorityError(code, str(error)) from error
    if not isinstance(document, dict):
        raise PackageAuthorityError(code, "document must be a table")
    return document


def validate_tooling_sources(root: Path, candidate: str) -> tuple[list[dict[str, str]], list[dict[str, str]]]:
    """Validate direct pins, exact lock identities, SDK, and preflight uv version."""

    package_bytes = candidate_blob(root, candidate, "package.json")
    pyproject_bytes = candidate_blob(root, candidate, "pyproject.toml")
    if package_bytes != candidate_blob(root, BASELINE_COMMIT, "package.json"):
        raise PackageAuthorityError("PACKAGE_NPM_MANIFEST_DRIFT", "package.json changed")
    if pyproject_bytes != candidate_blob(root, BASELINE_COMMIT, "pyproject.toml"):
        raise PackageAuthorityError("PACKAGE_PYTHON_MANIFEST_DRIFT", "pyproject.toml changed")

    package = parse_json(package_bytes, "PACKAGE_NPM_MANIFEST_INVALID")
    direct_npm = package.get("devDependencies")
    expected_npm = {"@commitlint/cli": "21.2.2", "@commitlint/config-conventional": "21.2.2"}
    if direct_npm != expected_npm:
        raise PackageAuthorityError("PACKAGE_NPM_DIRECT_PIN_DRIFT", repr(direct_npm))
    package_lock = parse_json(candidate_blob(root, candidate, "package-lock.json"), "PACKAGE_NPM_LOCK_INVALID")
    packages = package_lock.get("packages")
    if not isinstance(packages, dict) or not isinstance(packages.get(""), dict):
        raise PackageAuthorityError("PACKAGE_NPM_LOCK_INVALID", "packages root missing")
    if packages[""].get("devDependencies") != expected_npm:
        raise PackageAuthorityError("PACKAGE_NPM_LOCK_PARITY_DRIFT", "root direct dependencies")

    pyproject = parse_toml(pyproject_bytes, "PACKAGE_PYTHON_MANIFEST_INVALID")
    project = pyproject.get("project")
    expected_python_direct = ["jsonschema==4.26.0", "pytest==9.1.1"]
    if not isinstance(project, dict) or project.get("dependencies") != expected_python_direct:
        raise PackageAuthorityError("PACKAGE_PYTHON_DIRECT_PIN_DRIFT", repr(project))
    lock = parse_toml(candidate_blob(root, candidate, "uv.lock"), "PACKAGE_PYTHON_LOCK_INVALID")
    rows = lock.get("package")
    if not isinstance(rows, list) or not all(isinstance(row, dict) for row in rows):
        raise PackageAuthorityError("PACKAGE_PYTHON_LOCK_INVALID", "package rows missing")
    observed = tuple(sorted((row.get("name"), row.get("version", "0")) for row in rows))
    if observed != PYTHON_PACKAGES:
        raise PackageAuthorityError("PACKAGE_PYTHON_GRAPH_DRIFT", repr(observed))
    root_rows = [row for row in rows if row.get("name") == "hexalith-conversations-planning"]
    if len(root_rows) != 1:
        raise PackageAuthorityError("PACKAGE_PYTHON_LOCK_INVALID", "root package identity")
    metadata = root_rows[0].get("metadata")
    if not isinstance(metadata, dict) or metadata.get("requires-dist") != [
        {"name": "jsonschema", "specifier": "==4.26.0"},
        {"name": "pytest", "specifier": "==9.1.1"},
    ]:
        raise PackageAuthorityError("PACKAGE_PYTHON_LOCK_PARITY_DRIFT", repr(metadata))

    global_json = parse_json(candidate_blob(root, candidate, "global.json"), "PACKAGE_DOTNET_SDK_INVALID")
    if global_json != {"sdk": {"version": DOTNET_SDK_VERSION, "rollForward": "latestPatch"}}:
        raise PackageAuthorityError("PACKAGE_DOTNET_SDK_DRIFT", repr(global_json))
    workflow = candidate_blob(root, candidate, ".github/workflows/planning-authority-preflight.yml").decode(
        "utf-8", errors="strict"
    )
    if workflow.count(f"uv=={UV_VERSION}") != 1 or re.search(r"uv==(?!(?:0\.12\.13)\b)", workflow):
        raise PackageAuthorityError("PACKAGE_UV_CLIENT_DRIFT", "preflight uv pin")

    bindings = []
    for path in SOURCE_PATHS:
        mode, object_type, _ = raw_tree_record(root, candidate, path)
        if mode != "100644" or object_type != "blob":
            raise PackageAuthorityError("PACKAGE_SOURCE_MODE_DRIFT", f"{path}: {mode} {object_type}")
        bindings.append({"path": path, "sha256": sha256(candidate_blob(root, candidate, path)), "mode": mode})
    python_rows = [{"name": name, "version": version} for name, version in PYTHON_PACKAGES]
    return bindings, python_rows


def submodule_blob(root: Path, commit: str, relative_path: str) -> bytes:
    """Read an exact blob from the initialized Builds repository."""

    builds = (root / BUILDS_PATH).resolve()
    try:
        return run_git(builds, "show", f"{commit}:{safe_path(relative_path)}").stdout
    except PackageAuthorityError as error:
        raise PackageAuthorityError("PACKAGE_BUILDS_COMMIT_UNAVAILABLE", error.detail, "BLOCKED") from error


def validate_catalog(root: Path, candidate: str, builds_commit: str) -> dict[str, Any]:
    """Validate the exact selected Builds catalog families at the recorded gitlink."""

    content = submodule_blob(root, builds_commit, BUILDS_CATALOG_PATH)
    try:
        document = ET.fromstring(content.decode("utf-8-sig", errors="strict"))
    except (UnicodeError, ET.ParseError) as error:
        raise PackageAuthorityError("PACKAGE_BUILDS_CATALOG_INVALID", str(error)) from error
    rows: dict[str, str] = {}
    for item in document.iter("PackageVersion"):
        identity = item.attrib.get("Include")
        version = item.attrib.get("Version")
        if identity and version:
            if identity.lower() in (value.lower() for value in rows):
                raise PackageAuthorityError("PACKAGE_BUILDS_CATALOG_INVALID", f"duplicate {identity}")
            rows[identity] = version
    required = {
        "Aspire.Hosting": ASPIRE_VERSION,
        "Aspire.Hosting.Testing": ASPIRE_VERSION,
        "CommunityToolkit.Aspire.Hosting.Dapr": TOOLKIT_DAPR_VERSION,
        "Microsoft.Extensions.Http": "10.0.12",
        "Microsoft.NET.Test.Sdk": MICROSOFT_TEST_SDK_VERSION,
        "System.Text.Json": "10.0.12",
    }
    for package, version in required.items():
        if rows.get(package) != version:
            raise PackageAuthorityError("PACKAGE_BUILDS_VERSION_DRIFT", f"{package}: {rows.get(package)!r}")
    stale_ten = sorted(package for package, version in rows.items() if version == "10.0.11")
    if stale_ten:
        raise PackageAuthorityError("PACKAGE_MICROSOFT_FAMILY_DRIFT", repr(stale_ten))
    app_host = candidate_blob(
        root,
        candidate,
        "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj",
    )
    if f'Aspire.AppHost.Sdk/{ASPIRE_VERSION}'.encode() not in app_host:
        raise PackageAuthorityError("PACKAGE_ASPIRE_FAMILY_DRIFT", "AppHost SDK")
    return {
        "catalogPath": BUILDS_CATALOG_PATH,
        "catalogSha256": sha256(content),
        "aspireVersion": ASPIRE_VERSION,
        "communityToolkitAspireDaprVersion": TOOLKIT_DAPR_VERSION,
        "microsoftNetTestSdkVersion": MICROSOFT_TEST_SDK_VERSION,
    }


def validate_immutable_authorities(root: Path, candidate: str) -> list[dict[str, str]]:
    """Require V15, V16, V17, and the hold record to remain byte-identical."""

    rows = []
    for path, digest in IMMUTABLE_AUTHORITIES:
        mode, object_type, _ = raw_tree_record(root, candidate, path)
        observed = sha256(candidate_blob(root, candidate, path))
        if observed != digest or mode != "100644" or object_type != "blob":
            raise PackageAuthorityError("PACKAGE_PREDECESSOR_DRIFT", f"{path}: {observed} {mode}")
        rows.append({"path": path, "sha256": digest, "mode": mode})
    return rows


def validate_candidate(root: Path, candidate: str) -> tuple[list[dict[str, str]], dict[str, str], list[dict[str, str]], list[dict[str, str]], dict[str, Any]]:
    """Validate V18 C1 as an exact direct child with one Builds gitlink."""

    require_single_parent(root, candidate, BASELINE_COMMIT, "PACKAGE_C1_PARENT_MISMATCH")
    observed = changed_paths(root, BASELINE_COMMIT, candidate)
    if AUTHORITY_PATH in observed:
        raise PackageAuthorityError("PACKAGE_SELF_REFERENCE", AUTHORITY_PATH, "BLOCKED")
    if observed != C1_PATHS:
        missing = sorted(set(C1_PATHS) - set(observed))
        unexpected = sorted(set(observed) - set(C1_PATHS))
        raise PackageAuthorityError("PACKAGE_C1_SCOPE_DRIFT", f"missing={missing!r} unexpected={unexpected!r}")
    gitlinks = changed_gitlinks(root, BASELINE_COMMIT, candidate)
    if gitlinks != (BUILDS_PATH,):
        raise PackageAuthorityError("PACKAGE_GITLINK_SET_DRIFT", repr(gitlinks))

    files = []
    for path in CANDIDATE_FILE_PATHS:
        mode, object_type, _ = raw_tree_record(root, candidate, path)
        if mode != "100644" or object_type != "blob":
            raise PackageAuthorityError("PACKAGE_MODE_DRIFT", f"{path}: {mode} {object_type}")
        files.append({"path": path, "sha256": sha256(candidate_blob(root, candidate, path)), "mode": mode})
    mode, object_type, builds_commit = raw_tree_record(root, candidate, BUILDS_PATH)
    if mode != "160000" or object_type != "commit":
        raise PackageAuthorityError("PACKAGE_GITLINK_MODE_DRIFT", f"{mode} {object_type}")
    baseline_mode, baseline_type, baseline_builds_commit = raw_tree_record(root, BASELINE_COMMIT, BUILDS_PATH)
    if baseline_mode != "160000" or baseline_type != "commit" or baseline_builds_commit == builds_commit:
        raise PackageAuthorityError("PACKAGE_GITLINK_IDENTITY_DRIFT", builds_commit)
    source_bindings, python_rows = validate_tooling_sources(root, candidate)
    catalog = validate_catalog(root, candidate, builds_commit)
    immutable = validate_immutable_authorities(root, candidate)
    return files, {"path": BUILDS_PATH, "mode": mode, "baselineCommit": baseline_builds_commit, "candidateCommit": builds_commit}, source_bindings, python_rows, {**catalog, "immutable": immutable}


def render_authority(root: Path, candidate: str) -> dict[str, Any]:
    """Recompute the closed candidate-bound V18 authority."""

    files, gitlink, sources, python_rows, catalog_context = validate_candidate(root, candidate)
    immutable = catalog_context.pop("immutable")
    ledger = [
        {"id": "V18-C1", "subject": "single-parent-exact-eleven-path-c1", "state": "PASS"},
        {"id": "V18-C2", "subject": "single-parent-authority-only-c2", "state": "PASS"},
        {"id": "V18-GITLINK", "subject": "exact-builds-mode-160000-promotion", "state": "PASS", "paths": [BUILDS_PATH]},
        {"id": "V18-NPM", "subject": "unchanged-direct-pins-refreshed-lock", "state": "PASS"},
        {"id": "V18-PYTHON", "subject": "exact-thirteen-package-refreshed-lock", "state": "PASS"},
        {"id": "V18-TOOLCHAIN", "subject": "dotnet-uv-aspire-package-alignment", "state": "PASS"},
        {"id": "V18-PREDECESSORS", "subject": "immutable-v15-v16-v17-and-hold", "state": "PASS"},
        {"id": "V18-HOLD", "subject": "story-7-1-binding-remains-unresolved", "state": "PASS"},
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
            "changedGitlinks": [BUILDS_PATH],
        },
        "candidateFiles": files,
        "gitlinks": [gitlink],
        "sourceBindings": sources,
        "directPins": {
            "npm": {"@commitlint/cli": "21.2.2", "@commitlint/config-conventional": "21.2.2"},
            "python": ["jsonschema==4.26.0", "pytest==9.1.1"],
            "unchangedFromBaseline": True,
        },
        "pythonEnvironment": {"packageCount": len(python_rows), "packages": python_rows},
        "toolchain": {
            "dotnetSdk": DOTNET_SDK_VERSION,
            "uv": UV_VERSION,
            "aspire": ASPIRE_VERSION,
            "communityToolkitAspireDapr": TOOLKIT_DAPR_VERSION,
            "microsoftNetTestSdk": MICROSOFT_TEST_SDK_VERSION,
        },
        "buildsCatalog": catalog_context,
        "immutableAuthorities": immutable,
        "authorityEffect": {
            "implementationHold": "ACTIVE",
            "story71CandidateBindingResolved": False,
            "historicalEvidenceRewritten": False,
            "releaseAuthorized": False,
            "pushAuthorized": False,
        },
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True, "skipsAllowed": False},
        "result": "PASS",
        "assertionLedger": ledger,
    }


def validate_schema(root: Path, candidate: str, document: dict[str, Any]) -> None:
    """Validate the authority against the closed schema committed in C1."""

    try:
        import jsonschema
    except ImportError as error:
        raise PackageAuthorityError("PACKAGE_SCHEMA_UNAVAILABLE", str(error), "BLOCKED") from error
    try:
        schema = json.loads(candidate_blob(root, candidate, AUTHORITY_SCHEMA_PATH).decode("utf-8", errors="strict"))
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except (UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise PackageAuthorityError("PACKAGE_AUTHORITY_SCHEMA_INVALID", str(error)) from error


def locate_publication(root: Path, evaluated_candidate: str, publication_revision: str | None) -> str:
    """Locate the single V18 authority-addition commit in evaluated history."""

    if publication_revision:
        candidates = (resolve_commit(root, publication_revision, "PACKAGE_PUBLICATION_UNAVAILABLE"),)
    else:
        try:
            output = run_git(root, "log", "--format=%H", "--diff-filter=A", evaluated_candidate, "--", AUTHORITY_PATH).stdout.decode("ascii")
        except UnicodeError as error:
            raise PackageAuthorityError("PACKAGE_PUBLICATION_UNAVAILABLE", str(error), "BLOCKED") from error
        candidates = tuple(line for line in output.splitlines() if line)
    if len(candidates) != 1:
        raise PackageAuthorityError("PACKAGE_PUBLICATION_UNAVAILABLE", f"expected one V18 publication; observed {candidates!r}", "BLOCKED")
    return candidates[0]


def validate_publication(root: Path, candidate: str, publication: str, evaluated_candidate: str) -> None:
    """Validate V18 C2 and unchanged authority bytes at any descendant candidate."""

    require_single_parent(root, publication, candidate, "PACKAGE_C2_PARENT_MISMATCH")
    if changed_paths(root, candidate, publication) != (AUTHORITY_PATH,):
        raise PackageAuthorityError("PACKAGE_C2_SCOPE_DRIFT", repr(changed_paths(root, candidate, publication)))
    if changed_paths(root, BASELINE_COMMIT, publication) != COMBINED_PATHS:
        raise PackageAuthorityError("PACKAGE_SCOPE_DRIFT", repr(changed_paths(root, BASELINE_COMMIT, publication)))
    if changed_gitlinks(root, candidate, publication) or changed_gitlinks(root, BASELINE_COMMIT, publication) != (BUILDS_PATH,):
        raise PackageAuthorityError("PACKAGE_GITLINK_SET_DRIFT", "V18 transaction")
    mode, object_type, _ = raw_tree_record(root, publication, AUTHORITY_PATH)
    if mode != "100644" or object_type != "blob":
        raise PackageAuthorityError("PACKAGE_MODE_DRIFT", AUTHORITY_PATH)
    require_ancestor(root, publication, evaluated_candidate, "PACKAGE_PUBLICATION_NOT_ANCESTOR")
    if candidate_blob(root, publication, AUTHORITY_PATH) != candidate_blob(root, evaluated_candidate, AUTHORITY_PATH):
        raise PackageAuthorityError("PACKAGE_AUTHORITY_DESCENDANT_DRIFT", AUTHORITY_PATH)


def publish(
    root: Path,
    *,
    candidate_revision: str | None,
    check: bool,
    publication_revision: str | None = None,
) -> dict[str, Any]:
    """Publish C2 bytes or validate the immutable transaction at a descendant."""

    root = root.resolve()
    if check:
        evaluated_candidate = resolve_commit(root, candidate_revision or "HEAD", "PACKAGE_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated_candidate, publication_revision)
        existing_bytes = candidate_blob(root, publication, AUTHORITY_PATH)
        existing = parse_json(existing_bytes, "PACKAGE_AUTHORITY_INVALID")
        pinned_candidate = existing.get("candidateCommit")
        if not isinstance(pinned_candidate, str):
            raise PackageAuthorityError("PACKAGE_AUTHORITY_INVALID", "candidateCommit")
        candidate = resolve_commit(root, pinned_candidate, "PACKAGE_CANDIDATE_UNAVAILABLE")
    else:
        candidate = resolve_commit(root, candidate_revision or "HEAD", "PACKAGE_CANDIDATE_UNAVAILABLE")
        existing = None
    document = render_authority(root, candidate)
    validate_schema(root, candidate, document)
    target = root / AUTHORITY_PATH
    if check:
        if existing != document or existing_bytes != json_bytes(document):
            raise PackageAuthorityError("PACKAGE_AUTHORITY_DRIFT", AUTHORITY_PATH)
        validate_publication(root, candidate, publication, evaluated_candidate)
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        temporary = target.with_suffix(target.suffix + ".tmp")
        try:
            temporary.write_bytes(json_bytes(document))
            os.replace(temporary, target)
        except OSError as error:
            temporary.unlink(missing_ok=True)
            raise PackageAuthorityError("PACKAGE_AUTHORITY_WRITE_FAILED", str(error), "BLOCKED") from error
    return document


def failure_document(root: Path, error: PackageAuthorityError) -> dict[str, Any]:
    """Return one parseable non-vacuous failure result."""

    return {
        "schemaVersion": "hexalith.conversations.v18-package-environment-result.v1",
        "result": error.state,
        "repository": str(root.resolve()),
        "assertionLedger": [
            {"id": error.code, "subject": "v18-package-environment", "state": error.state, "detail": error.detail}
        ],
        "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
    }


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the V18 publisher/checker."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    parser.add_argument("--candidate")
    parser.add_argument("--publication")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args(arguments)
    root = Path(args.repository)
    try:
        document = publish(
            root,
            candidate_revision=args.candidate,
            check=args.check,
            publication_revision=args.publication,
        )
        print(
            f"V18_PACKAGE_ENVIRONMENT_AUTHORITY_OK CANDIDATE={document['candidateCommit']} "
            f"PACKAGES={document['pythonEnvironment']['packageCount']} PATHS={len(document['publication']['combinedPaths'])}"
        )
        return 0
    except PackageAuthorityError as error:
        sys.stdout.write(json.dumps(failure_document(root, error), indent=2, ensure_ascii=False) + "\n")
        return 2 if error.state == "BLOCKED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
