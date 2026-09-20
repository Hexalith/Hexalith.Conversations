#!/usr/bin/env python3
"""Resolve the V22 current planning authority from committed raw Git objects."""

from __future__ import annotations

import argparse
import configparser
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
from typing import Any, NoReturn

from jsonschema import Draft202012Validator
from jsonschema.exceptions import SchemaError, ValidationError


RECOVERY_PATH = "_bmad-output/planning-artifacts/v22-current-authority-recovery-v1.json"
RECOVERY_SCHEMA_PATH = "_bmad/schemas/v22-current-authority-recovery-v1.schema.json"
ROUTE_PATH = "_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json"
ROUTE_SCHEMA_PATH = "_bmad/schemas/v22-workflow-route-inventory-v1.schema.json"
ARCHITECTURE_PATH = "_bmad-output/planning-artifacts/architecture.md"
WORKFLOW_PATH = ".github/workflows/planning-authority-preflight.yml"
V21_RECORD_PATH = "_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json"
V21_SCHEMA_PATH = "_bmad/schemas/v21-story-7.1-authority-correction-v1.schema.json"
V21_PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_successor_authorities.py"
V21_TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py"
GITMODULES_PATH = ".gitmodules"

EXPECTED_PARENT = "c610fbb8c5491fb0d7987c2f8294199887f4a44c"
EXPECTED_PARENT_TREE = "a760f6ae98c51d2b377afb230648e8a451ac1cba"
EXPECTED_ARCHITECTURE_BYTES = 215037
EXPECTED_ARCHITECTURE_SHA256 = "7bd3b7e54927565d9b32950b1b0554ceba28d5187370f067369d5891690094a3"
EXPECTED_V16_BYTES = 7022
EXPECTED_V16_SHA256 = "7d438ccb69391805973ff827f02457a4441b1e540f1fff0249e4eee0bb26b2ea"
EXPECTED_V21_SHA256 = "296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636"
EXPECTED_V21_TOOLING = "239758d396d28372687b73f5dc128405892cb520"
EXPECTED_ROUTE_SHA256 = "f0e1c4f3497d5195a71f050dd74db7d4fc88ca674c6bfc0777b92d64199ace57"
ACTIVE_COMMAND = (
    "uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py "
    "--repository . --candidate HEAD --check"
)

EXPECTED_PATHS = (
    WORKFLOW_PATH,
    ARCHITECTURE_PATH,
    RECOVERY_PATH,
    ROUTE_PATH,
    RECOVERY_SCHEMA_PATH,
    ROUTE_SCHEMA_PATH,
    "_bmad/scripts/resolve_current_planning_authority.py",
    "_bmad/scripts/tests/test_resolve_current_planning_authority.py",
)

EXPECTED_GITLINKS = (
    ("references/Hexalith.AI.Tools", "160000", "5f93d2ec8239494852c97032c819cb1689939e36"),
    ("references/Hexalith.Builds", "160000", "141260881c0336f6dc6e434b00134f61eda0f50f"),
    ("references/Hexalith.Commons", "160000", "9f4809d37095e64e3732abc3e766535f9e836563"),
    ("references/Hexalith.EventStore", "160000", "ba7ac196e60db8820525961791eccfacec24633f"),
    ("references/Hexalith.Folders", "160000", "040cd14465bdec1aa275e997f637fe79485b31a6"),
    ("references/Hexalith.FrontComposer", "160000", "b2bac5f193130ea08abc22fd78d6a936c2f419f4"),
    ("references/Hexalith.Memories", "160000", "5829db422522b79c0d164df85adde53a996ba114"),
    ("references/Hexalith.Parties", "160000", "14d249fde316b0002aec84351d7a7cdf953d1d30"),
    ("references/Hexalith.Projects", "160000", "fd19bf86698e41a0d2a7c9441a5f7810f31647f0"),
    ("references/Hexalith.Tenants", "160000", "c150d5b1af4f911ff3b2a7ab7901d94838be09d7"),
)

_COMMIT = re.compile(r"^[0-9a-f]{40}$")


class ResolutionError(Exception):
    """Represent a stable FAIL or BLOCKED resolver result."""

    def __init__(self, code: str, detail: str, state: str = "FAIL") -> None:
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail
        self.state = state


def sha256(content: bytes) -> str:
    """Return the lowercase SHA-256 digest for exact bytes."""

    return hashlib.sha256(content).hexdigest()


def safe_path(value: str) -> str:
    """Require a normalized repository-relative POSIX path."""

    path = PurePosixPath(value)
    if not value or path.is_absolute() or ".." in path.parts or str(path) != value:
        raise ResolutionError("PATH_INVALID", repr(value), "BLOCKED")
    return value


def reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    """Reject duplicate JSON object properties instead of silently accepting the last one."""

    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON property {key!r}")
        result[key] = value
    return result


def load_json(content: bytes, code: str) -> dict[str, Any]:
    """Load one duplicate-safe UTF-8 JSON object."""

    try:
        document = json.loads(
            content.decode("utf-8", errors="strict"),
            object_pairs_hook=reject_duplicate_keys,
        )
    except (UnicodeDecodeError, json.JSONDecodeError, ValueError) as error:
        raise ResolutionError(code, str(error), "BLOCKED") from error
    if not isinstance(document, dict):
        raise ResolutionError(code, "top-level JSON value is not an object", "BLOCKED")
    return document


def trusted_environment() -> dict[str, str]:
    """Return a Git environment without user/system configuration or replacement objects."""

    return {
        "GIT_CONFIG_GLOBAL": os.devnull,
        "GIT_CONFIG_NOSYSTEM": "1",
        "GIT_CONFIG_SYSTEM": os.devnull,
        "GIT_NO_REPLACE_OBJECTS": "1",
        "HOME": "/tmp",
        "LANG": "C.UTF-8",
        "LC_ALL": "C.UTF-8",
        "PATH": os.environ.get("PATH", "/usr/local/bin:/usr/bin:/bin"),
    }


def run_git(
    repository: Path,
    *arguments: str,
    allowed: tuple[int, ...] = (0,),
) -> subprocess.CompletedProcess[bytes]:
    """Run Git with replacement objects and ambient configuration disabled."""

    try:
        completed = subprocess.run(
            ["git", "--no-replace-objects", "-C", str(repository), *arguments],
            check=False,
            capture_output=True,
            env=trusted_environment(),
        )
    except OSError as error:
        raise ResolutionError("GIT_UNAVAILABLE", str(error), "BLOCKED") from error
    if completed.returncode not in allowed:
        detail = completed.stderr.decode("utf-8", errors="replace").strip()
        raise ResolutionError("GIT_OBJECT_UNAVAILABLE", detail or "Git command failed", "BLOCKED")
    return completed


def resolve_commit(repository: Path, revision: str) -> str:
    """Resolve one revision to its canonical commit identity."""

    output = run_git(repository, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout
    commit = output.decode("ascii", errors="strict").strip()
    if not _COMMIT.fullmatch(commit):
        raise ResolutionError("CANDIDATE_MALFORMED", repr(commit), "BLOCKED")
    return commit


def commit_tree(repository: Path, commit: str) -> str:
    """Resolve a commit's tree identity."""

    output = run_git(repository, "rev-parse", "--verify", f"{commit}^{{tree}}").stdout
    tree = output.decode("ascii", errors="strict").strip()
    if not _COMMIT.fullmatch(tree):
        raise ResolutionError("TREE_MALFORMED", repr(tree), "BLOCKED")
    return tree


def candidate_blob(repository: Path, commit: str, relative_path: str) -> bytes:
    """Read exact committed blob bytes without consulting the worktree."""

    return run_git(repository, "cat-file", "blob", f"{commit}:{safe_path(relative_path)}").stdout


def changed_paths(repository: Path, parent: str, candidate: str) -> tuple[str, ...]:
    """Return the exact ordinal no-rename changed-path set."""

    content = run_git(
        repository,
        "diff-tree",
        "--no-commit-id",
        "--name-only",
        "--no-renames",
        "-r",
        "-z",
        parent,
        candidate,
        "--",
    ).stdout
    if not content:
        return ()
    if not content.endswith(b"\0"):
        raise ResolutionError("DIFF_OUTPUT_MALFORMED", "missing NUL terminator", "BLOCKED")
    try:
        paths = tuple(item.decode("utf-8", errors="strict") for item in content[:-1].split(b"\0"))
    except UnicodeDecodeError as error:
        raise ResolutionError("DIFF_OUTPUT_MALFORMED", str(error), "BLOCKED") from error
    return tuple(safe_path(path) for path in paths)


def tree_entry(repository: Path, commit: str, relative_path: str) -> tuple[str, str, str]:
    """Read one exact tree record as mode, object kind, and object ID."""

    content = run_git(repository, "ls-tree", "-z", commit, "--", safe_path(relative_path)).stdout
    records = [row for row in content.split(b"\0") if row]
    if len(records) != 1:
        raise ResolutionError("TREE_ENTRY_UNAVAILABLE", relative_path, "BLOCKED")
    try:
        header, observed = records[0].split(b"\t", 1)
        mode, kind, object_id = header.decode("ascii", errors="strict").split(" ")
        observed_path = observed.decode("utf-8", errors="strict")
    except (UnicodeDecodeError, ValueError) as error:
        raise ResolutionError("TREE_ENTRY_MALFORMED", relative_path, "BLOCKED") from error
    if observed_path != relative_path or not _COMMIT.fullmatch(object_id):
        raise ResolutionError("TREE_ENTRY_MALFORMED", relative_path, "BLOCKED")
    return mode, kind, object_id


def gitlinks(repository: Path, commit: str) -> tuple[tuple[str, str, str], ...]:
    """Derive all repository gitlinks from raw mode-160000 tree entries."""

    content = run_git(repository, "ls-tree", "-r", "-z", commit).stdout
    rows: list[tuple[str, str, str]] = []
    for record in (row for row in content.split(b"\0") if row):
        try:
            header, raw_path = record.split(b"\t", 1)
            mode, kind, object_id = header.decode("ascii", errors="strict").split(" ")
            relative_path = raw_path.decode("utf-8", errors="strict")
        except (UnicodeDecodeError, ValueError) as error:
            raise ResolutionError("TREE_OUTPUT_MALFORMED", str(error), "BLOCKED") from error
        if mode == "160000":
            if kind != "commit" or not _COMMIT.fullmatch(object_id):
                raise ResolutionError("GITLINK_MALFORMED", relative_path, "BLOCKED")
            rows.append((safe_path(relative_path), mode, object_id))
    return tuple(rows)


def gitmodule_paths(content: bytes) -> tuple[str, ...]:
    """Parse and ordinally normalize the root .gitmodules path inventory."""

    parser = configparser.ConfigParser(interpolation=None, strict=True)
    parser.optionxform = str
    try:
        parser.read_string(content.decode("utf-8", errors="strict"))
    except (UnicodeDecodeError, configparser.Error) as error:
        raise ResolutionError("GITMODULES_MALFORMED", str(error), "BLOCKED") from error
    paths: list[str] = []
    for section in parser.sections():
        if not section.startswith('submodule "') or not section.endswith('"'):
            raise ResolutionError("GITMODULES_MALFORMED", f"unexpected section {section!r}", "BLOCKED")
        if not parser.has_option(section, "path"):
            raise ResolutionError("GITMODULES_MALFORMED", f"missing path in {section!r}", "BLOCKED")
        paths.append(safe_path(parser.get(section, "path")))
    if len(paths) != len(set(paths)):
        raise ResolutionError("GITMODULES_DUPLICATE_PATH", repr(paths), "BLOCKED")
    return tuple(sorted(paths))


def v16_block(content: bytes) -> bytes:
    """Extract the one complete frozen V16 block without its following newline."""

    begin = b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V16:BEGIN"
    end = b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V16:END"
    if content.count(begin) != 1 or content.count(end) != 1:
        raise ResolutionError("V16_MARKER_CARDINALITY", "expected one complete V16 marker", "FAIL")
    start = content.index(begin)
    finish_start = content.index(end, start)
    try:
        finish = content.index(b"-->", finish_start) + 3
    except ValueError as error:
        raise ResolutionError("V16_MARKER_INCOMPLETE", "V16 END is not closed", "FAIL") from error
    return content[start:finish]


def expected_v22_suffix(record_digest: str) -> bytes:
    """Render the only V22 architecture suffix accepted by this resolver."""

    text = f"""
<!-- ARCHITECTURE-EXECUTION-OVERLAY-V22:BEGIN version=conversations-architecture-2026-09-20-v22 supersedes=conversations-architecture-2026-09-19-v16 v16-block-bytes=7022 v16-block-sha256={EXPECTED_V16_SHA256} recovery-record=v22-current-authority-recovery-v1.json recovery-record-sha256={record_digest} hold=ACTIVE -->

# Architecture Execution Authority Overlay V22 — Current-Authority Recovery

The V22 recovery record and committed resolver implement the pragmatic AR-15
route adopted by V16. V22 supersedes only V16's pending-recovery state; all
other V16 decisions and inherited invariants remain binding. A technical
`PASS` completes AR-15 only after repository-owner review, exact-SHA ordinary
CI, owner fast-forward, and a post-merge resolver `PASS`. Candidate-authored
evidence claims none of those owner actions.

The effective implementation hold remains `ACTIVE`. V22 authorizes no Story
7.1 implementation, product or dependency change, submodule or gitlink change,
sprint transition, release, push, or `EXECUTION_ALLOWED`. The next eligible
request after completed AR-15 is a separately owner-approved AD-4
`EXECUTION_ALLOWED` successor.

<!-- ARCHITECTURE-EXECUTION-OVERLAY-V22:END version=conversations-architecture-2026-09-20-v22 recovery-record=v22-current-authority-recovery-v1.json recovery-record-sha256={record_digest} hold=ACTIVE -->
"""
    return text.encode("utf-8")


def empty_observed() -> dict[str, Any]:
    """Return the closed observation envelope used even for early blockers."""

    return {
        "candidateCommit": None,
        "candidateTree": None,
        "parentCommit": None,
        "parentTree": None,
        "changedPaths": [],
        "parentGitlinks": [],
        "candidateGitlinks": [],
    }


def result_document(
    result: str,
    observed: dict[str, Any],
    ledger: list[dict[str, str]],
    blockers: list[dict[str, str]],
) -> dict[str, Any]:
    """Build one closed ACTIVE-hold result envelope."""

    exit_codes = {"PASS": 0, "FAIL": 1, "BLOCKED": 2}
    return {
        "schemaVersion": "hexalith.conversations.current-planning-authority-result.v1",
        "result": result,
        "exitCode": exit_codes[result],
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": observed,
        "assertionLedger": ledger,
        "blockers": blockers,
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
    }


def blocked_result(code: str, detail: str) -> dict[str, Any]:
    """Build a nonvacuous caller-safe BLOCKED result for unavailable evidence."""

    ledger = [{"id": code, "subject": "current-authority-resolution", "state": "BLOCKED", "detail": detail}]
    return result_document("BLOCKED", empty_observed(), ledger, [{"code": code, "detail": detail}])


def fail(
    ledger: list[dict[str, str]],
    code: str,
    subject: str,
    detail: str,
    state: str = "FAIL",
) -> NoReturn:
    """Record and raise one stable failing assertion."""

    ledger.append({"id": code, "subject": subject, "state": state, "detail": detail})
    raise ResolutionError(code, detail, state)


def passed(ledger: list[dict[str, str]], code: str, subject: str, detail: str) -> None:
    """Record one successful evaluated assertion."""

    ledger.append({"id": code, "subject": subject, "state": "PASS", "detail": detail})


def require(
    condition: bool,
    ledger: list[dict[str, str]],
    code: str,
    subject: str,
    detail: str,
    state: str = "FAIL",
) -> None:
    """Record PASS or raise a stable FAIL/BLOCKED assertion."""

    if not condition:
        fail(ledger, code, subject, detail, state)
    passed(ledger, code, subject, detail)


def validate_schema(schema: dict[str, Any], document: dict[str, Any], code: str) -> None:
    """Validate a document with one Draft 2020-12 schema."""

    try:
        Draft202012Validator.check_schema(schema)
        Draft202012Validator(schema).validate(document)
    except (SchemaError, ValidationError) as error:
        raise ResolutionError(code, error.message, "BLOCKED") from error


def policy_gitlinks(policy: dict[str, Any]) -> tuple[tuple[str, str, str], ...]:
    """Read closed gitlink tuples from a schema-validated policy."""

    return tuple(
        (row["path"], row["mode"], row["objectId"])
        for row in policy["rootGitlinks"]["entries"]
    )


def changed_manifest(repository: Path, candidate: str, paths: tuple[str, ...]) -> list[dict[str, str]]:
    """Build the raw-object manifest for all changed regular blobs."""

    manifest: list[dict[str, str]] = []
    for relative_path in paths:
        mode, kind, object_id = tree_entry(repository, candidate, relative_path)
        if mode != "100644" or kind != "blob":
            raise ResolutionError(
                "TRANSACTION_MODE_DRIFT",
                f"{relative_path}: observed {mode} {kind}",
                "FAIL",
            )
        content = candidate_blob(repository, candidate, relative_path)
        manifest.append(
            {
                "path": relative_path,
                "mode": mode,
                "objectId": object_id,
                "sha256": sha256(content),
            }
        )
    return manifest


def resolve_authority(repository: Path, revision: str) -> dict[str, Any]:
    """Recompute the V22 authority result from committed objects."""

    observed = empty_observed()
    ledger: list[dict[str, str]] = []
    result_schema: dict[str, Any] | None = None
    try:
        if run_git(repository, "rev-parse", "--is-shallow-repository").stdout.strip() != b"false":
            fail(
                ledger,
                "HISTORY_UNAVAILABLE",
                "complete-git-history",
                "repository is shallow or history availability is unknown",
                "BLOCKED",
            )
        candidate = resolve_commit(repository, revision)
        observed["candidateCommit"] = candidate
        candidate_tree = commit_tree(repository, candidate)
        observed["candidateTree"] = candidate_tree
        passed(ledger, "V22-ENVIRONMENT", "raw-git-object-access", "complete non-shallow history is available")

        parent_row = run_git(repository, "rev-list", "--parents", "-n", "1", candidate).stdout.decode(
            "ascii", errors="strict"
        ).split()
        if len(parent_row) != 2 or parent_row[0] != candidate or not _COMMIT.fullmatch(parent_row[1]):
            fail(
                ledger,
                "CANDIDATE_GRAPH_DRIFT",
                "single-direct-parent",
                f"observed parent row {parent_row!r}",
            )
        parent = parent_row[1]
        observed["parentCommit"] = parent
        parent_tree = commit_tree(repository, parent)
        observed["parentTree"] = parent_tree
        require(
            parent == EXPECTED_PARENT and parent_tree == EXPECTED_PARENT_TREE,
            ledger,
            "CANDIDATE_PARENT_DRIFT",
            "selected-parent-commit-and-tree",
            f"observed parent={parent} tree={parent_tree}",
        )

        recovery_bytes = candidate_blob(repository, candidate, RECOVERY_PATH)
        schema_bytes = candidate_blob(repository, candidate, RECOVERY_SCHEMA_PATH)
        policy = load_json(recovery_bytes, "RECOVERY_POLICY_MALFORMED")
        result_schema = load_json(schema_bytes, "RECOVERY_SCHEMA_MALFORMED")
        validate_schema(result_schema, policy, "RECOVERY_POLICY_SCHEMA_INVALID")
        require(
            policy["selectedParent"] == {"commit": EXPECTED_PARENT, "tree": EXPECTED_PARENT_TREE}
            and tuple(policy["transaction"]["exactChangedPaths"]) == EXPECTED_PATHS
            and policy["transaction"]["requiredMode"] == "100644",
            ledger,
            "RECOVERY_POLICY_DRIFT",
            "pinned-parent-and-transaction-policy",
            "policy matches resolver-pinned parent and eight-path transaction",
        )

        route_bytes = candidate_blob(repository, candidate, ROUTE_PATH)
        route_schema = load_json(candidate_blob(repository, candidate, ROUTE_SCHEMA_PATH), "ROUTE_SCHEMA_MALFORMED")
        route = load_json(route_bytes, "ROUTE_INVENTORY_MALFORMED")
        validate_schema(route_schema, route, "ROUTE_INVENTORY_SCHEMA_INVALID")
        require(
            sha256(route_bytes) == EXPECTED_ROUTE_SHA256
            and policy["routeInventory"]["sha256"] == EXPECTED_ROUTE_SHA256,
            ledger,
            "ROUTE_INVENTORY_DIGEST_DRIFT",
            "acyclic-route-inventory-binding",
            f"observed route digest {sha256(route_bytes)}",
        )

        paths = changed_paths(repository, parent, candidate)
        if paths != EXPECTED_PATHS:
            missing = sorted(set(EXPECTED_PATHS) - set(paths))
            unexpected = sorted(set(paths) - set(EXPECTED_PATHS))
            fail(
                ledger,
                "TRANSACTION_PATH_DRIFT",
                "exact-eight-path-transaction",
                f"missing={missing!r}; unexpected={unexpected!r}; observed={paths!r}",
            )
        observed["changedPaths"] = changed_manifest(repository, candidate, paths)
        passed(
            ledger,
            "V22-TRANSACTION",
            "exact-eight-mode-100644-paths",
            "candidate changes exactly the eight ordinal regular-file paths",
        )

        parent_links = gitlinks(repository, parent)
        candidate_links = gitlinks(repository, candidate)
        observed["parentGitlinks"] = [
            {"path": path, "mode": mode, "objectId": object_id}
            for path, mode, object_id in parent_links
        ]
        observed["candidateGitlinks"] = [
            {"path": path, "mode": mode, "objectId": object_id}
            for path, mode, object_id in candidate_links
        ]
        policy_links = policy_gitlinks(policy)
        expected_module_paths = tuple(path for path, _mode, _object_id in EXPECTED_GITLINKS)
        parent_modules = gitmodule_paths(candidate_blob(repository, parent, GITMODULES_PATH))
        candidate_modules = gitmodule_paths(candidate_blob(repository, candidate, GITMODULES_PATH))
        require(
            parent_links == candidate_links == policy_links == EXPECTED_GITLINKS
            and parent_modules == candidate_modules == expected_module_paths,
            ledger,
            "ROOT_GITLINK_DRIFT",
            "ordinal-root-gitlinks-and-gitmodules-paths",
            f"parent={parent_links!r}; candidate={candidate_links!r}; modules={candidate_modules!r}",
        )

        parent_architecture = candidate_blob(repository, parent, ARCHITECTURE_PATH)
        require(
            len(parent_architecture) == EXPECTED_ARCHITECTURE_BYTES
            and sha256(parent_architecture) == EXPECTED_ARCHITECTURE_SHA256,
            ledger,
            "ARCHITECTURE_PREFIX_DRIFT",
            "byte-exact-v1-through-v16-prefix",
            f"observed bytes={len(parent_architecture)} sha256={sha256(parent_architecture)}",
        )
        frozen_v16 = v16_block(parent_architecture)
        require(
            len(frozen_v16) == EXPECTED_V16_BYTES and sha256(frozen_v16) == EXPECTED_V16_SHA256,
            ledger,
            "V16_BINDING_DRIFT",
            "frozen-v16-block-binding",
            f"observed bytes={len(frozen_v16)} sha256={sha256(frozen_v16)}",
        )
        candidate_architecture = candidate_blob(repository, candidate, ARCHITECTURE_PATH)
        expected_architecture = parent_architecture + expected_v22_suffix(sha256(recovery_bytes))
        require(
            candidate_architecture == expected_architecture,
            ledger,
            "V22_MARKER_DRIFT",
            "single-complete-v22-tail-marker",
            "candidate architecture is the exact frozen prefix plus the bound V22 marker",
        )

        historical_paths = (V21_RECORD_PATH, V21_SCHEMA_PATH, V21_PUBLISHER_PATH, V21_TEST_PATH)
        historical_unchanged = all(
            candidate_blob(repository, candidate, path) == candidate_blob(repository, parent, path)
            for path in historical_paths
        )
        require(
            historical_unchanged
            and sha256(candidate_blob(repository, candidate, V21_RECORD_PATH)) == EXPECTED_V21_SHA256,
            ledger,
            "HISTORICAL_V21_DRIFT",
            "immutable-v21-record-schema-publisher-and-tests",
            "historical V21 bytes are parent-identical and the record digest is pinned",
        )

        workflow = candidate_blob(repository, candidate, WORKFLOW_PATH).decode("utf-8", errors="strict")
        forbidden = (
            "RULESET_TOKEN",
            "/rulesets?",
            " ci-trust ",
            " ci-trust\\",
            f"{V21_PUBLISHER_PATH}\" --repository",
            f"{V21_PUBLISHER_PATH} --repository",
        )
        route_valid = (
            workflow.count(ACTIVE_COMMAND) == 1
            and EXPECTED_V21_TOOLING in workflow
            and V21_TEST_PATH in workflow
            and all(token not in workflow for token in forbidden)
            and route["activeRoutes"][0]["command"] == ACTIVE_COMMAND
            and route["historicalRoutes"][0]["sourceCommit"] == EXPECTED_V21_TOOLING
            and route["historicalRoutes"][0]["invokedAsCurrentAuthority"] is False
        )
        require(
            route_valid,
            ledger,
            "WORKFLOW_ROUTE_DRIFT",
            "one-active-v22-route-and-frozen-v21-tests",
            "workflow invokes V22 once, runs V21 tests from the frozen commit, and invokes no retired mechanism",
        )

        require(
            policy_links == EXPECTED_GITLINKS
            and policy["architecture"]["v16BlockBytes"] == EXPECTED_V16_BYTES
            and policy["architecture"]["v16BlockSha256"] == EXPECTED_V16_SHA256
            and policy["historicalV21"]["toolingCommit"] == EXPECTED_V21_TOOLING,
            ledger,
            "RECOVERY_BINDINGS_DRIFT",
            "closed-raw-object-recovery-bindings",
            "policy binds V16, historical V21, routes, and all root gitlinks",
        )
        passed(
            ledger,
            "V22-HOLD",
            "active-hold-without-owner-or-execution-claim",
            "technical resolution preserves ACTIVE and requests only later AD-4 EXECUTION_ALLOWED",
        )

        document = result_document("PASS", observed, ledger, [])
        validate_schema(result_schema, document, "RESULT_SCHEMA_INVALID")
        return document
    except ResolutionError as error:
        if not ledger or ledger[-1].get("id") != error.code:
            ledger.append(
                {
                    "id": error.code,
                    "subject": "current-authority-resolution",
                    "state": error.state,
                    "detail": error.detail or error.code,
                }
            )
        document = result_document(
            error.state,
            observed,
            ledger,
            [{"code": error.code, "detail": error.detail or error.code}],
        )
        if result_schema is not None:
            try:
                validate_schema(result_schema, document, "RESULT_SCHEMA_INVALID")
            except ResolutionError as schema_error:
                return blocked_result(schema_error.code, schema_error.detail)
        return document
    except (UnicodeDecodeError, KeyError, TypeError, ValueError) as error:
        return blocked_result("EVIDENCE_MALFORMED", str(error))


def parse_args(arguments: list[str] | None = None) -> argparse.Namespace:
    """Parse the resolver command line."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--candidate", required=True)
    parser.add_argument("--check", action="store_true", required=True)
    return parser.parse_args(arguments)


def main(arguments: list[str] | None = None) -> int:
    """Run the resolver, emit one JSON document, and map PASS/FAIL/BLOCKED to 0/1/2."""

    try:
        args = parse_args(arguments)
        repository = args.repository.resolve(strict=True)
        if not repository.is_dir():
            document = blocked_result("REPOSITORY_UNAVAILABLE", str(repository))
        else:
            document = resolve_authority(repository, args.candidate)
    except (OSError, SystemExit) as error:
        if isinstance(error, SystemExit):
            raise
        document = blocked_result("REPOSITORY_UNAVAILABLE", str(error))
    print(json.dumps(document, indent=2, sort_keys=True))
    return int(document["exitCode"])


if __name__ == "__main__":
    sys.exit(main())
