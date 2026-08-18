#!/usr/bin/env python3
"""Reconstruct immutable Epic 6 completion evidence and fail closed."""

from __future__ import annotations

import argparse
import ctypes
from datetime import date
import errno
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
from typing import Any, Callable, Sequence

from jsonschema import Draft202012Validator


CONTRACT_PATH = "_bmad-output/planning-artifacts/epic-6-completion-supersession-contract-v1.json"
SCHEMA_PATH = "_bmad/schemas/epic-6-completion-supersession-v1.schema.json"
EVIDENCE_SCHEMA_VERSION = "epic-6-completion-supersession-evidence-v1"
CURRENT_PROOF_CONTRACT_PATH = (
    "_bmad-output/planning-artifacts/epic-6-completion-supersession-current-proof-contract-v1.json"
)
CURRENT_PROOF_SCHEMA_PATH = "_bmad/schemas/epic-6-completion-supersession-current-proof-v1.schema.json"
CURRENT_PROOF_EVIDENCE_SCHEMA_VERSION = "epic-6-completion-supersession-current-proof-evidence-v1"
CURRENT_PROOF_EVIDENCE_ID = "EPIC-6-COMPLETION-SUPERSESSION-CURRENT-PROOF-v1"
GIT_TIMEOUT = 60
COMMAND_TIMEOUT = 900
INOTIFY_MODIFY = 0x00000002


class SupersessionError(RuntimeError):
    """Stable reconstruction failure with an explicit evidence state."""

    def __init__(self, code: str, message: str, state: str = "FAIL", story_id: str | None = None) -> None:
        super().__init__(message)
        self.code = code
        self.message = message
        self.state = state
        self.story_id = story_id


def digest(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def safe_path(value: str) -> str:
    path = PurePosixPath(value)
    if (
        not value
        or value in (".", "..")
        or path.is_absolute()
        or path.as_posix() != value
        or "\\" in value
        or any(part in ("", ".", "..") for part in path.parts)
        or any(ord(character) < 0x20 for character in value)
    ):
        raise SupersessionError("E6_PATH_INVALID", repr(value), "BLOCKED")
    return value


def git(
    repository: Path,
    *arguments: str,
    allowed: tuple[int, ...] = (0,),
    timeout: int = GIT_TIMEOUT,
) -> subprocess.CompletedProcess[bytes]:
    try:
        result = subprocess.run(
            ("git", "-C", str(repository), *arguments),
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=timeout,
            env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SupersessionError("E6_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        message = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise SupersessionError("E6_HISTORY_UNAVAILABLE", message, "BLOCKED")
    return result


def resolve_root(repository: Path) -> Path:
    root = repository.resolve()
    observed = Path(git(root, "rev-parse", "--show-toplevel").stdout.decode().strip()).resolve()
    if observed != root:
        raise SupersessionError("E6_REPOSITORY_ROOT_MISMATCH", f"expected {root}; observed {observed}", "BLOCKED")
    return root


def resolve_commit(repository: Path, revision: str, story_id: str) -> str:
    result = git(repository, "rev-parse", "--verify", f"{revision}^{{commit}}")
    commit = result.stdout.decode().strip()
    if commit != revision or re.fullmatch(r"[0-9a-f]{40}", commit) is None:
        raise SupersessionError("E6_COMMIT_ID_MISMATCH", f"expected {revision}; observed {commit}", "BLOCKED", story_id)
    return commit


def load_contract(repository: Path) -> tuple[dict[str, Any], bytes, bytes]:
    try:
        contract_bytes = (repository / CONTRACT_PATH).read_bytes()
        schema_bytes = (repository / SCHEMA_PATH).read_bytes()
        contract = json.loads(contract_bytes)
        schema = json.loads(schema_bytes)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise SupersessionError("E6_CONTRACT_UNAVAILABLE", str(error), "BLOCKED") from error
    errors = sorted(Draft202012Validator(schema).iter_errors(contract), key=lambda item: list(item.absolute_path))
    if errors:
        detail = "; ".join(f"{'/'.join(map(str, error.absolute_path))}: {error.message}" for error in errors)
        raise SupersessionError("E6_CONTRACT_SCHEMA_INVALID", detail)
    story_ids = [story["storyId"] for story in contract["stories"]]
    if story_ids != ["6.7", "6.2"]:
        raise SupersessionError("E6_STORY_SET_DRIFT", repr(story_ids))
    if contract["rootGitlinkPaths"] != sorted(contract["rootGitlinkPaths"]):
        raise SupersessionError("E6_GITLINK_SET_DRIFT", "root gitlink paths are not canonically sorted")
    return contract, contract_bytes, schema_bytes


def raw_diff(repository: Path, candidate: str, done: str, *, paths: Sequence[str] = ()) -> list[dict[str, str]]:
    """Return exact raw-diff records between two commits.

    `paths` is an optional pathspec restriction appended after `--`; the
    default (no paths) reproduces the exact prior unrestricted invocation
    byte-for-byte, so every existing historical-mode call site is unchanged.
    Restricting to a pathspec (as the additive current-proof route does, to
    `references/`) also sidesteps Git's rename-pair records, which this raw
    parser -- unchanged from its historical two-field-per-record shape --
    does not need to represent because no gitlink entry is ever a rename.
    """
    fields = [
        field
        for field in git(repository, "diff", "--raw", "--no-abbrev", "-z", candidate, done, "--", *paths).stdout.split(b"\0")
        if field
    ]
    if len(fields) % 2:
        raise SupersessionError("E6_RAW_DIFF_MALFORMED", "raw diff has an incomplete record", "BLOCKED")
    records: list[dict[str, str]] = []
    for index in range(0, len(fields), 2):
        metadata = fields[index].decode("ascii", errors="strict").split()
        if len(metadata) != 5 or not metadata[0].startswith(":"):
            raise SupersessionError("E6_RAW_DIFF_MALFORMED", repr(metadata), "BLOCKED")
        records.append(
            {
                "path": safe_path(fields[index + 1].decode("utf-8", errors="strict")),
                "oldMode": metadata[0][1:],
                "newMode": metadata[1],
                "oldObject": metadata[2],
                "newObject": metadata[3],
                "status": metadata[4],
            }
        )
    return records


def root_gitlinks(repository: Path, commit: str) -> list[dict[str, str]]:
    entries = [entry for entry in git(repository, "ls-tree", "-r", "-z", commit, "--", "references").stdout.split(b"\0") if entry]
    rows: list[dict[str, str]] = []
    for entry in entries:
        metadata, raw_path = entry.split(b"\t", 1)
        mode, object_type, object_id = metadata.decode("ascii").split()
        if mode != "160000":
            continue
        path = safe_path(raw_path.decode("utf-8", errors="strict"))
        rows.append({"path": path, "mode": mode, "objectId": object_id, "objectType": object_type})
    return sorted(rows, key=lambda row: row["path"])


def parse_frontmatter(content: str) -> dict[str, Any]:
    match = re.match(r"\A---\n(?P<body>.*?)\n---\n", content.replace("\r\n", "\n"), re.DOTALL)
    if match is None:
        raise SupersessionError("E6_RECORD_FRONTMATTER_INVALID", "frontmatter missing")
    body = match.group("body")
    scalars = dict(re.findall(r"^(status|file_list_commit):\s*'([^']+)'\s*$", body, re.MULTILINE))
    promotion_block = body.split("submodule_promotions:", 1)
    paths: list[str] = []
    if len(promotion_block) == 2:
        for line in promotion_block[1].splitlines()[1:]:
            if line and not line[0].isspace():
                break
            path_match = re.match(r"\s*-\s*path:\s*'([^']+)'\s*$", line)
            if path_match:
                paths.append(safe_path(path_match.group(1)))
    return {"status": scalars.get("status"), "fileListCommit": scalars.get("file_list_commit"), "promotionPaths": paths}


def recorded_story(repository: Path, story: dict[str, Any]) -> dict[str, Any]:
    path = safe_path(story["recordPath"])
    result = git(repository, "show", f"{story['doneCommit']}:{path}")
    content = result.stdout
    frontmatter = parse_frontmatter(content.decode("utf-8", errors="strict"))
    if frontmatter["status"] != "done" or frontmatter["fileListCommit"] != story["candidateCommit"]:
        raise SupersessionError("E6_RECORD_IDENTITY_DRIFT", repr(frontmatter), story_id=story["storyId"])
    if frontmatter["promotionPaths"] != story["declaredPromotionPaths"]:
        raise SupersessionError(
            "E6_PROMOTION_DECLARATION_DRIFT",
            f"expected={story['declaredPromotionPaths']!r} observed={frontmatter['promotionPaths']!r}",
            story_id=story["storyId"],
        )
    return {**frontmatter, "path": path, "sha256": digest(content), "blobBytes": len(content)}


def verify_gitlink_objects(repository: Path, rows: Sequence[dict[str, str]], story_id: str, revision: str) -> None:
    for row in rows:
        submodule = repository / row["path"]
        if not submodule.is_dir():
            raise SupersessionError("E6_SUBMODULE_UNAVAILABLE", row["path"], "BLOCKED", story_id)
        result = git(submodule, "cat-file", "-e", f"{row['objectId']}^{{commit}}", allowed=(0, 1, 128))
        if result.returncode != 0:
            raise SupersessionError(
                "E6_GITLINK_OBJECT_UNAVAILABLE",
                f"{revision}:{row['path']}:{row['objectId']}",
                "BLOCKED",
                story_id,
            )


def clone_exact_tree(repository: Path, destination: Path, done: str, rows: Sequence[dict[str, str]]) -> Path:
    clone = destination / "repository"
    try:
        subprocess.run(
            (
                "git",
                "clone",
                "--quiet",
                "--no-checkout",
                "--local",
                "--no-hardlinks",
                str(repository),
                str(clone),
            ),
            check=True,
            timeout=COMMAND_TIMEOUT,
            env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
        )
        git(clone, "checkout", "--quiet", "--detach", done)
        for row in rows:
            source = repository / row["path"]
            target = clone / row["path"]
            target.parent.mkdir(parents=True, exist_ok=True)
            subprocess.run(
                (
                    "git",
                    "clone",
                    "--quiet",
                    "--no-checkout",
                    "--local",
                    "--no-hardlinks",
                    str(source),
                    str(target),
                ),
                check=True,
                timeout=COMMAND_TIMEOUT,
                env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
            )
            git(target, "checkout", "--quiet", "--detach", row["objectId"])
    except (OSError, subprocess.CalledProcessError, subprocess.TimeoutExpired) as error:
        raise SupersessionError("E6_EXACT_TREE_MATERIALIZATION_FAILED", str(error), "BLOCKED") from error
    return clone


# Runners report counts in both orders. pytest's normal summary is count-first ("1 skipped",
# "3 passed, 1 skipped"); label-first ("Skipped: 1") is the VSTest/dotnet shape. Matching only one
# form let a real skip read as zero and a narrowed lane read as complete verification.
SKIPPED_PATTERNS = (
    r"(?i)\bskipped\s*[:=]?\s*(\d+)\b",
    r"(?i)\b(\d+)\s+skipped\b",
)
NOT_RUN_PATTERNS = (
    r"(?i)\bnot[\s-]*run\s*[:=]?\s*(\d+)\b",
    r"(?i)\b(\d+)\s+not[\s-]*run\b",
)


def _max_reported_count(output: str, patterns: Sequence[str]) -> int:
    values = [int(value) for pattern in patterns for value in re.findall(pattern, output)]
    return max(values, default=0)


def skipped_count(output: str) -> int:
    return _max_reported_count(output, SKIPPED_PATTERNS)


def not_run_count(output: str) -> int:
    return _max_reported_count(output, NOT_RUN_PATTERNS)


def execute_command(root: Path, command: dict[str, Any]) -> dict[str, Any]:
    try:
        result = subprocess.run(
            command["argv"],
            cwd=root,
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=COMMAND_TIMEOUT,
            env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SupersessionError("E6_REBUILT_TEST_UNAVAILABLE", f"{command['id']}: {error}", "BLOCKED") from error
    output = result.stdout.decode("utf-8", errors="replace")
    skipped = skipped_count(output)
    not_run = not_run_count(output)
    state = "PASS" if result.returncode == 0 and skipped == 0 and not_run == 0 else "FAIL"
    return {
        "id": command["id"],
        "argv": command["argv"],
        "state": state,
        "exitCode": result.returncode,
        "skippedCount": skipped,
        "notRunCount": not_run,
        "outputSha256": digest(result.stdout),
        "outputTail": output[-4000:],
        "_output": output,
    }


def require_linux_inotify_watch(root: Path, command: dict[str, Any], story_id: str) -> None:
    """Fail closed when daprd cannot watch its exact-tree resource directory."""
    if not sys.platform.startswith("linux"):
        return

    try:
        libc = ctypes.CDLL(None, use_errno=True)
        descriptor = libc.inotify_init1(os.O_CLOEXEC)
    except (AttributeError, OSError) as error:
        raise SupersessionError(
            "E6_REBUILT_TEST_ENVIRONMENT_UNAVAILABLE",
            f"{command['id']}: Linux inotify probe unavailable: {error}; argv={command['argv']!r}",
            "BLOCKED",
            story_id,
        ) from error

    if descriptor < 0:
        error_number = ctypes.get_errno()
        raise SupersessionError(
            "E6_REBUILT_TEST_ENVIRONMENT_UNAVAILABLE",
            f"{command['id']}: inotify_init1 failed with errno {error_number} "
            f"({os.strerror(error_number)}); argv={command['argv']!r}",
            "BLOCKED",
            story_id,
        )

    try:
        watch = libc.inotify_add_watch(descriptor, os.fsencode(root), INOTIFY_MODIFY)
        if watch < 0:
            error_number = ctypes.get_errno()
            classification = "capacity exhausted" if error_number in (errno.ENOSPC, errno.EMFILE, errno.ENFILE) else "probe failed"
            raise SupersessionError(
                "E6_REBUILT_TEST_ENVIRONMENT_UNAVAILABLE",
                f"{command['id']}: required Linux inotify watch {classification}: errno {error_number} "
                f"({os.strerror(error_number)}); argv={command['argv']!r}",
                "BLOCKED",
                story_id,
            )
    finally:
        os.close(descriptor)


def execute_story_checks(repository: Path, story: dict[str, Any], done_rows: Sequence[dict[str, str]]) -> dict[str, Any]:
    if story["storyId"] == "6.2":
        require_linux_inotify_watch(repository, story["testCommands"][0], story["storyId"])
    with tempfile.TemporaryDirectory(prefix=f"epic-6-{story['storyId'].replace('.', '-')}-") as temporary:
        exact = clone_exact_tree(repository, Path(temporary), story["doneCommit"], done_rows)
        promotion_argv = [
            sys.executable,
            str(exact / "_bmad/scripts/verify_submodule_promotion.py"),
            "--repository",
            str(exact),
            "--baseline",
            story["candidateCommit"],
            "--candidate",
            story["doneCommit"],
        ]
        for path in story["declaredPromotionPaths"]:
            promotion_argv.extend(("--submodule", path))
        for path in story["remoteRequiredPaths"]:
            promotion_argv.extend(("--require-remote", path))
        promotion_argv.extend(("--format", "json"))
        promotion = execute_command(exact, {"id": f"STORY-{story['storyId']}-PROMOTION", "argv": promotion_argv})
        promotion_output = promotion.pop("_output")
        try:
            promotion_document = json.loads(promotion_output)
        except json.JSONDecodeError:
            promotion_document = {}
        if promotion["state"] != "PASS" or promotion_document.get("result") != "pass" or promotion_document.get("blockers"):
            promotion["state"] = "FAIL"
        promotion["result"] = promotion_document.get("result")
        promotion["blockerCodes"] = [item.get("code") for item in promotion_document.get("blockers", [])]
        tests = [execute_command(exact, command) for command in story["testCommands"]]
        for test in tests:
            test.pop("_output")
        return {"promotion": promotion, "tests": tests}


def bind_authority_bundle(repository: Path, planning_candidate: str, authority_bundle: str) -> dict[str, Any]:
    candidate = resolve_commit(repository, planning_candidate, "V12")
    path = safe_path(authority_bundle)
    try:
        content = (repository / path).read_bytes()
        document = json.loads(content)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise SupersessionError("E6_AUTHORITY_BUNDLE_UNAVAILABLE", str(error), "BLOCKED") from error
    observed = document.get("planningCandidate")
    if observed != candidate:
        raise SupersessionError("E6_AUTHORITY_BUNDLE_PC_MISMATCH", f"expected={candidate} observed={observed}")
    return {"candidateCommit": candidate, "path": path, "sha256": digest(content)}


CheckExecutor = Callable[[Path, dict[str, Any], Sequence[dict[str, str]]], dict[str, Any]]


def reconstruct(
    repository: Path,
    *,
    execute_tests: bool,
    planning_candidate: str | None = None,
    authority_bundle: str | None = None,
    check_executor: CheckExecutor = execute_story_checks,
) -> dict[str, Any]:
    root = resolve_root(repository)
    contract, contract_bytes, schema_bytes = load_contract(root)
    ledger: list[dict[str, Any]] = [
        {"id": "CONTRACT-01", "subject": CONTRACT_PATH, "state": "PASS", "sha256": digest(contract_bytes)},
        {"id": "SCHEMA-01", "subject": SCHEMA_PATH, "state": "PASS", "sha256": digest(schema_bytes)},
    ]
    authority = None
    if planning_candidate is not None or authority_bundle is not None:
        if planning_candidate is None or authority_bundle is None:
            raise SupersessionError("E6_AUTHORITY_BINDING_INCOMPLETE", "planning candidate and bundle are both required", "BLOCKED")
        authority = bind_authority_bundle(root, planning_candidate, authority_bundle)
        ledger.append({"id": "AUTHORITY-01", "subject": authority_bundle, "state": "PASS", "sha256": authority["sha256"]})
    stories: list[dict[str, Any]] = []
    expected_gitlink_paths = contract["rootGitlinkPaths"]
    for story in contract["stories"]:
        story_id = story["storyId"]
        candidate = resolve_commit(root, story["candidateCommit"], story_id)
        done = resolve_commit(root, story["doneCommit"], story_id)
        if git(root, "merge-base", "--is-ancestor", candidate, done, allowed=(0, 1)).returncode != 0:
            raise SupersessionError("E6_CANDIDATE_NOT_ANCESTOR", f"{candidate} !<= {done}", "BLOCKED", story_id)
        diff_rows = raw_diff(root, candidate, done)
        observed_paths = [row["path"] for row in diff_rows]
        if observed_paths != story["changedPaths"]:
            raise SupersessionError("E6_CHANGED_PATH_SET_DRIFT", f"expected={story['changedPaths']!r} observed={observed_paths!r}", story_id=story_id)
        candidate_rows = root_gitlinks(root, candidate)
        done_rows = root_gitlinks(root, done)
        for revision, rows in ((candidate, candidate_rows), (done, done_rows)):
            paths = [row["path"] for row in rows]
            if paths != expected_gitlink_paths or any(row["mode"] != "160000" for row in rows):
                raise SupersessionError("E6_GITLINK_SET_DRIFT", f"{revision}: {paths!r}", story_id=story_id)
            verify_gitlink_objects(root, rows, story_id, revision)
        record = recorded_story(root, story)
        checks = None
        if execute_tests:
            checks = check_executor(root, story, done_rows)
            if not checks.get("tests"):
                raise SupersessionError("E6_REBUILT_TESTS_SKIPPED", f"{story_id}: no rebuilt test result", "BLOCKED", story_id)
            if any(test.get("skippedCount", 0) != 0 for test in checks["tests"]):
                raise SupersessionError("E6_REBUILT_TEST_SKIPPED", story_id, story_id=story_id)
            if any(test.get("notRunCount", 0) != 0 for test in checks["tests"]):
                raise SupersessionError("E6_REBUILT_TEST_NOT_RUN", story_id, story_id=story_id)
            states = [checks["promotion"]["state"], *(test["state"] for test in checks["tests"])]
            if not states or any(state != "PASS" for state in states):
                detail = [
                    {
                        "id": test.get("id"),
                        "state": test.get("state"),
                        "exitCode": test.get("exitCode"),
                        "skippedCount": test.get("skippedCount"),
                        "notRunCount": test.get("notRunCount"),
                        "outputSha256": test.get("outputSha256"),
                        "outputTail": test.get("outputTail"),
                    }
                    for test in checks["tests"]
                    if test.get("state") != "PASS"
                ]
                raise SupersessionError(
                    "E6_REBUILT_CHECK_FAILED",
                    f"{story_id}: {json.dumps(detail, sort_keys=True)}",
                    story_id=story_id,
                )
        stories.append(
            {
                "storyId": story_id,
                "candidateCommit": candidate,
                "doneCommit": done,
                "record": record,
                "rawDiff": diff_rows,
                "candidateRootGitlinks": candidate_rows,
                "doneRootGitlinks": done_rows,
                "checks": checks,
            }
        )
        ledger.append({"id": f"STORY-{story_id}", "subject": f"{candidate}..{done}", "state": "PASS", "changedPathCount": len(diff_rows), "rootGitlinkCount": len(done_rows)})
    if not execute_tests:
        raise SupersessionError("E6_REBUILT_TESTS_SKIPPED", "exact done-tree tests and promotion checks were not executed", "BLOCKED")
    return {
        "schemaVersion": EVIDENCE_SCHEMA_VERSION,
        "evidenceId": "EPIC-6-COMPLETION-SUPERSESSION-v1",
        "issuedOn": date.today().isoformat(),
        "result": "PASS",
        "contract": {"path": CONTRACT_PATH, "sha256": digest(contract_bytes)},
        "authorityBundle": authority,
        "stories": stories,
        "assertionLedger": ledger,
        "decisionRequired": True,
        "implementationHold": "ACTIVE",
        "releaseAuthorized": False,
    }


# --- Additive V13 current-proof route -------------------------------------
#
# `current_proof()` answers a genuinely separate, present-state question:
# does current HEAD still hold, given the immutable Story 6.7/6.2 done
# commits? It never touches CONTRACT_PATH/SCHEMA_PATH, never calls
# `reconstruct()`, and never clones an exact historical tree -- it binds
# current HEAD directly, deriving gitlinks from raw mode 160000 only.


def promotion_module() -> Any:
    """Import verify_submodule_promotion.py once for raw-mode-160000 lookups.

    Reuses `recorded_gitlink()` instead of duplicating raw gitlink-mode
    derivation logic.
    """
    module_path = Path(__file__).resolve().parent / "verify_submodule_promotion.py"
    try:
        spec = importlib.util.spec_from_file_location("verify_submodule_promotion", module_path)
        if spec is None or spec.loader is None:
            raise SupersessionError("E6_CURRENT_PROOF_PROMOTION_MODULE_UNAVAILABLE", str(module_path), "BLOCKED")
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
    except SupersessionError:
        raise
    except Exception as error:
        raise SupersessionError(
            "E6_CURRENT_PROOF_PROMOTION_MODULE_UNAVAILABLE",
            f"{module_path}: {error}",
            "BLOCKED",
        ) from error
    return module


def load_current_proof_contract(repository: Path) -> tuple[dict[str, Any], bytes, bytes]:
    try:
        contract_bytes = (repository / CURRENT_PROOF_CONTRACT_PATH).read_bytes()
        schema_bytes = (repository / CURRENT_PROOF_SCHEMA_PATH).read_bytes()
        contract = json.loads(contract_bytes)
        schema = json.loads(schema_bytes)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise SupersessionError("E6_CURRENT_PROOF_CONTRACT_UNAVAILABLE", str(error), "BLOCKED") from error
    try:
        Draft202012Validator.check_schema(schema)
    except Exception as error:
        raise SupersessionError("E6_CURRENT_PROOF_SCHEMA_INVALID", str(error), "BLOCKED") from error
    errors = sorted(Draft202012Validator(schema).iter_errors(contract), key=lambda item: list(item.absolute_path))
    if errors:
        detail = "; ".join(f"{'/'.join(map(str, error.absolute_path))}: {error.message}" for error in errors)
        raise SupersessionError("E6_CURRENT_PROOF_CONTRACT_SCHEMA_INVALID", detail, "BLOCKED")
    if list(contract["storyDoneCommits"]) != ["6.7", "6.2"]:
        raise SupersessionError("E6_CURRENT_PROOF_STORY_SET_DRIFT", repr(list(contract["storyDoneCommits"])), "BLOCKED")
    if contract["rootGitlinks"] != sorted(contract["rootGitlinks"]):
        raise SupersessionError(
            "E6_CURRENT_PROOF_GITLINK_SET_DRIFT",
            "root gitlink paths are not canonically sorted",
            "BLOCKED",
        )
    return contract, contract_bytes, schema_bytes


def resolve_head(repository: Path) -> str:
    result = git(repository, "rev-parse", "--verify", "HEAD^{commit}")
    head = result.stdout.decode().strip()
    if re.fullmatch(r"[0-9a-f]{40}", head) is None:
        raise SupersessionError("E6_CURRENT_PROOF_HEAD_UNRESOLVED", repr(head), "BLOCKED")
    return head


def current_root_gitlinks(repository: Path, head: str, paths: Sequence[str]) -> list[dict[str, str]]:
    """Bind current HEAD's raw-mode-160000 gitlink SHA for every declared root path."""
    module = promotion_module()
    rows: list[dict[str, str]] = []
    for path in paths:
        safe_path(path)
        try:
            mode, object_id = module.recorded_gitlink(repository, head, path)
        except module.GateError as error:
            raise SupersessionError("E6_CURRENT_PROOF_GITLINK_UNAVAILABLE", str(error), "BLOCKED") from error
        if mode != "160000" or object_id is None:
            raise SupersessionError(
                "E6_CURRENT_PROOF_GITLINK_MODE_DRIFT",
                f"{path}: mode={mode!r} objectId={object_id!r}",
                "BLOCKED",
            )
        rows.append({"path": path, "mode": mode, "objectId": object_id})

    # Exact inventory equality, not "the declared subset all happen to be gitlinks". A root gitlink
    # that exists at HEAD but is missing from the contract would otherwise never be inspected.
    declared = sorted({safe_path(path) for path in paths})
    observed = declared_root_gitlink_inventory(repository, head)
    if declared != observed:
        raise SupersessionError(
            "E6_CURRENT_PROOF_GITLINK_INVENTORY_DRIFT",
            f"declared={declared!r} observed={observed!r}",
            "BLOCKED",
        )
    modules = gitmodules_paths(repository, head)
    if declared != modules:
        raise SupersessionError(
            "E6_CURRENT_PROOF_GITMODULES_DRIFT",
            f"declared={declared!r} gitmodules={modules!r}",
            "BLOCKED",
        )

    # A recorded object id is only evidence if the object is actually reachable in that submodule.
    for row in rows:
        submodule = repository / row["path"]
        probe = git(submodule, "cat-file", "-e", f"{row['objectId']}^{{commit}}", allowed=(0, 1, 128))
        if probe.returncode != 0:
            raise SupersessionError(
                "E6_CURRENT_PROOF_GITLINK_OBJECT_UNAVAILABLE",
                f"{row['path']}: {row['objectId']}",
                "BLOCKED",
            )
    return rows


def assert_inputs_come_from_the_resolved_commit(
    repository: Path, head: str, inputs: Sequence[tuple[str, bytes]]
) -> None:
    """Require every declared input byte to equal its blob at the resolved commit.

    Evidence labelled with a commit must execute only bytes from that commit. Loading the contract
    or schema from the mutable worktree lets uncommitted bytes be attributed to `head`.
    """
    for relative_path, observed in inputs:
        result = git(repository, "show", f"{head}:{safe_path(relative_path)}", allowed=(0, 128))
        if result.returncode != 0:
            raise SupersessionError(
                "E6_CURRENT_PROOF_INPUT_NOT_IN_COMMIT", f"{relative_path}@{head}", "BLOCKED"
            )
        if result.stdout != observed:
            raise SupersessionError(
                "E6_CURRENT_PROOF_INPUT_WORKTREE_DRIFT",
                f"{relative_path}: worktree bytes differ from {head}",
                "BLOCKED",
            )


def worktree_dirt(repository: Path) -> list[str]:
    """Return tracked paths whose worktree bytes differ from the index or HEAD."""
    result = git(repository, "status", "--porcelain=v1", "--untracked-files=no")
    return sorted(
        line[3:].strip()
        for line in result.stdout.decode("utf-8", errors="replace").splitlines()
        if line.strip()
    )


def endpoint_changed_paths(repository: Path, done: str, head: str, scope_prefix: str) -> list[str]:
    """Endpoint difference only: paths whose bytes differ between `done` and `head`."""
    diff_rows = raw_diff(repository, done, head, paths=(scope_prefix,))
    return sorted({row["path"] for row in diff_rows if row["path"].startswith(scope_prefix)})


def history_union_changed_paths(repository: Path, done: str, head: str, scope_prefix: str) -> list[str]:
    """Every path touched anywhere in `done..head`, derived independently of the endpoint diff.

    Deliberately a separate raw-Git code path rather than a second call to the same helper: an oracle
    that calls the implementation cannot detect a regression in it. History-union semantics also
    catch a path changed and later reverted, which an endpoint diff silently drops.
    """
    result = git(
        repository,
        "log",
        "--pretty=format:",
        "--name-only",
        "--no-renames",
        f"{done}..{head}",
        "--",
        scope_prefix,
    )
    paths = {
        safe_path(line)
        for line in result.stdout.decode("utf-8", errors="strict").splitlines()
        if line.strip()
    }
    return sorted(path for path in paths if path.startswith(scope_prefix))


def post_done_changed_paths(repository: Path, done: str, head: str, scope_prefix: str) -> dict[str, Any]:
    """Enumerate every path changed under `scope_prefix` since a story's done commit.

    Returns the authoritative history-union set together with the endpoint difference and the paths
    that differ between them, so a claim about "every path changed" is never silently narrowed to
    "paths that happen to differ at the endpoints".
    """
    endpoint = endpoint_changed_paths(repository, done, head, scope_prefix)
    union = history_union_changed_paths(repository, done, head, scope_prefix)
    unexpected = sorted(set(endpoint) - set(union))
    if unexpected:
        # An endpoint difference outside the history union means the range or the diff is not
        # describing the same commits; never report that as a complete enumeration.
        raise SupersessionError(
            "E6_CURRENT_PROOF_CHANGED_PATH_ORACLE_DRIFT",
            f"endpoint paths absent from history union: {unexpected!r}",
            "BLOCKED",
        )
    return {
        "paths": union,
        "endpointPaths": endpoint,
        "revertedWithinRange": sorted(set(union) - set(endpoint)),
    }


def declared_root_gitlink_inventory(repository: Path, head: str) -> list[str]:
    """Derive the complete root mode-160000 inventory from HEAD's tree, not from a mutable contract."""
    result = git(repository, "ls-tree", "-r", "--full-tree", "-z", head)
    inventory: list[str] = []
    for record in result.stdout.split(b"\0"):
        if not record:
            continue
        try:
            metadata, path = record.decode("utf-8", errors="strict").split("\t", 1)
        except ValueError as error:
            raise SupersessionError(
                "E6_CURRENT_PROOF_GITLINK_INVENTORY_MALFORMED", repr(record), "BLOCKED"
            ) from error
        if metadata.split()[0] == "160000":
            inventory.append(safe_path(path))
    return sorted(inventory)


def gitmodules_paths(repository: Path, head: str) -> list[str]:
    """Read the declared submodule paths from `.gitmodules` at the resolved commit."""
    result = git(repository, "show", f"{head}:.gitmodules", allowed=(0, 128))
    if result.returncode != 0:
        raise SupersessionError("E6_CURRENT_PROOF_GITMODULES_UNAVAILABLE", head, "BLOCKED")
    text = result.stdout.decode("utf-8", errors="strict")
    return sorted({safe_path(match) for match in re.findall(r"(?m)^\s*path\s*=\s*(\S+)\s*$", text)})


def require_nonempty_ledger(ledger: Sequence[dict[str, Any]], code: str) -> None:
    if not ledger:
        raise SupersessionError(code, "assertion ledger is empty", "BLOCKED")


CurrentProofTestExecutor = Callable[[Path, dict[str, Any]], dict[str, Any]]


def current_proof(
    repository: Path,
    *,
    execute_tests: bool,
    test_executor: CurrentProofTestExecutor = execute_command,
    allow_dirty_worktree: bool = False,
) -> dict[str, Any]:
    root = resolve_root(repository)
    contract, contract_bytes, schema_bytes = load_current_proof_contract(root)
    ledger: list[dict[str, Any]] = [
        {"id": "CURRENT-PROOF-CONTRACT-01", "subject": CURRENT_PROOF_CONTRACT_PATH, "state": "PASS", "sha256": digest(contract_bytes)},
        {"id": "CURRENT-PROOF-SCHEMA-01", "subject": CURRENT_PROOF_SCHEMA_PATH, "state": "PASS", "sha256": digest(schema_bytes)},
    ]
    head = resolve_head(root)
    ledger.append({"id": "CURRENT-PROOF-HEAD-01", "subject": head, "state": "PASS"})

    # The result is labelled with `head`, so its inputs must be `head`'s bytes and the tree the
    # declared commands run against must not carry uncommitted tracked changes.
    assert_inputs_come_from_the_resolved_commit(
        root,
        head,
        ((CURRENT_PROOF_CONTRACT_PATH, contract_bytes), (CURRENT_PROOF_SCHEMA_PATH, schema_bytes)),
    )
    ledger.append({"id": "CURRENT-PROOF-INPUT-BINDING-01", "subject": head, "state": "PASS"})

    dirt = worktree_dirt(root)
    if dirt and not allow_dirty_worktree:
        raise SupersessionError(
            "E6_CURRENT_PROOF_WORKTREE_DIRTY",
            f"{len(dirt)} tracked path(s) differ from HEAD: {dirt[:10]!r}",
            "BLOCKED",
        )
    ledger.append(
        {
            "id": "CURRENT-PROOF-WORKTREE-01",
            "subject": "tracked-worktree-clean",
            "state": "PASS" if not dirt else "not-applicable",
            "dirtyTrackedPathCount": len(dirt),
        }
    )

    scope_prefix = contract["postDoneChangedPaths"]["scopePrefix"]
    done_commits: dict[str, str] = {}
    changed_paths: dict[str, list[str]] = {}
    for story_id, declared_done in contract["storyDoneCommits"].items():
        done = resolve_commit(root, declared_done, story_id)
        if git(root, "merge-base", "--is-ancestor", done, head, allowed=(0, 1)).returncode != 0:
            raise SupersessionError(
                "E6_CURRENT_PROOF_HEAD_NOT_DESCENDANT",
                f"{story_id}: HEAD {head} does not descend from done {done}",
                "BLOCKED",
                story_id,
            )
        done_commits[story_id] = done
        ledger.append({"id": f"CURRENT-PROOF-DONE-{story_id}", "subject": f"{done}..{head}", "state": "PASS"})
        enumeration = post_done_changed_paths(root, done, head, scope_prefix)
        paths = enumeration["paths"]
        changed_paths[story_id] = paths
        ledger.append(
            {
                "id": f"CURRENT-PROOF-CHANGED-PATHS-{story_id}",
                "subject": f"{done}..{head}",
                "state": "PASS",
                "semantics": "history-union",
                "changedPathCount": len(paths),
                "endpointPathCount": len(enumeration["endpointPaths"]),
                "revertedWithinRangeCount": len(enumeration["revertedWithinRange"]),
            }
        )

    gitlink_rows = current_root_gitlinks(root, head, contract["rootGitlinks"])
    ledger.append(
        {
            "id": "CURRENT-PROOF-GITLINKS-01",
            "subject": scope_prefix,
            "state": "PASS",
            "rootGitlinkCount": len(gitlink_rows),
        }
    )

    if not execute_tests:
        raise SupersessionError(
            "E6_CURRENT_PROOF_TESTS_SKIPPED",
            "current-tree build/test surface was not executed",
            "BLOCKED",
        )

    tests = [test_executor(root, command) for command in contract["testCommands"]]
    if not tests:
        raise SupersessionError("E6_CURRENT_PROOF_TESTS_SKIPPED", "no current-tree test result", "BLOCKED")
    required_test_keys = ("id", "argv", "state", "exitCode", "skippedCount", "notRunCount", "outputSha256")
    for test in tests:
        missing = [key for key in required_test_keys if key not in test]
        if missing:
            raise SupersessionError(
                "E6_CURRENT_PROOF_TEST_MALFORMED",
                f"missing keys {missing!r} in {test.get('id')!r}",
                "BLOCKED",
            )
        test.pop("_output", None)
    if any(test["skippedCount"] != 0 for test in tests):
        raise SupersessionError(
            "E6_CURRENT_PROOF_TEST_SKIPPED",
            json.dumps([test.get("id") for test in tests]),
            "BLOCKED",
        )
    if any(test["notRunCount"] != 0 for test in tests):
        raise SupersessionError(
            "E6_CURRENT_PROOF_TEST_NOT_RUN",
            json.dumps([test.get("id") for test in tests]),
            "BLOCKED",
        )
    for test in tests:
        ledger.append(
            {
                "id": test["id"],
                "subject": " ".join(test["argv"]),
                "state": test["state"],
                "exitCode": test["exitCode"],
                "outputSha256": test["outputSha256"],
            }
        )
    if any(test["state"] != "PASS" for test in tests):
        detail = [
            {
                "id": test.get("id"),
                "state": test.get("state"),
                "exitCode": test.get("exitCode"),
                "outputSha256": test.get("outputSha256"),
                "outputTail": test.get("outputTail"),
            }
            for test in tests
            if test.get("state") != "PASS"
        ]
        raise SupersessionError("E6_CURRENT_PROOF_TEST_FAILED", json.dumps(detail, sort_keys=True))

    require_nonempty_ledger(ledger, "E6_CURRENT_PROOF_LEDGER_EMPTY")

    return {
        "schemaVersion": CURRENT_PROOF_EVIDENCE_SCHEMA_VERSION,
        "evidenceId": CURRENT_PROOF_EVIDENCE_ID,
        "issuedOn": date.today().isoformat(),
        "result": "PASS",
        "contract": {"path": CURRENT_PROOF_CONTRACT_PATH, "sha256": digest(contract_bytes)},
        "currentHeadCommit": head,
        "storyDoneCommits": done_commits,
        "rootGitlinks": gitlink_rows,
        "postDoneChangedPaths": changed_paths,
        "testResults": tests,
        "assertionLedger": ledger,
        "decisionRequired": True,
        "implementationHold": "ACTIVE",
        "releaseAuthorized": False,
        "supersedesHistoricalEvidence": False,
    }


def current_proof_failure_document(error: SupersessionError) -> dict[str, Any]:
    return {
        "schemaVersion": CURRENT_PROOF_EVIDENCE_SCHEMA_VERSION,
        "evidenceId": CURRENT_PROOF_EVIDENCE_ID,
        "issuedOn": date.today().isoformat(),
        "result": error.state,
        "currentHeadCommit": None,
        "storyDoneCommits": None,
        "rootGitlinks": [],
        "postDoneChangedPaths": {},
        "testResults": [],
        "assertionLedger": [
            {"id": error.code, "subject": error.story_id or "current-proof", "state": error.state, "message": error.message}
        ],
        "decisionRequired": True,
        "implementationHold": "ACTIVE",
        "releaseAuthorized": False,
        "supersedesHistoricalEvidence": False,
    }


def current_proof_markdown(document: dict[str, Any]) -> str:
    lines = [
        "# Epic 6 Completion Supersession Current-Proof Evidence v1",
        "",
        f"- Result: `{document['result']}`",
        f"- Issued on: `{document['issuedOn']}`",
        f"- Implementation hold: `{document['implementationHold']}`",
        "- Release authorized: `false`",
        "- Independent decision required: `true`",
        "- Supersedes historical evidence: `false`",
        "",
        "## Present-state binding",
        "",
    ]
    if document.get("currentHeadCommit"):
        lines.append(f"- Current HEAD: `{document['currentHeadCommit']}`")
        for story_id, done in (document.get("storyDoneCommits") or {}).items():
            changed = (document.get("postDoneChangedPaths") or {}).get(story_id, [])
            lines.append(f"- Story {story_id} done: `{done}` -- changed paths under `references/` since done: `{len(changed)}`")
        lines.append(f"- Root gitlinks bound: `{len(document.get('rootGitlinks') or [])}`")
        lines.append(f"- Test commands executed: `{len(document.get('testResults') or [])}`")
        lines.append("")
    lines.extend(["## Boundary", ""])
    if document["result"] == "PASS":
        lines.append(
            "This evidence answers only whether the present tree still holds. It never rewrites, narrows, or "
            "supersedes the V1-V12 historical evidence, decision, schema, or contract, and it does not by "
            "itself lift the implementation hold, authorize a successor, or authorize release. Only an "
            "ACCEPTED independent decision on this evidence may propose -- never silently apply -- the "
            "`epic-6-retro-item-24` sprint-status transition."
        )
    else:
        lines.append(
            "This evidence cannot support an ACCEPTED present-state decision while the current-proof route is "
            "not PASS. The blocker must be resolved and the current-proof route rerun; V1-V12 historical "
            "evidence and decisions remain untouched and the implementation hold stays active."
        )
    lines.append("")
    return "\n".join(lines)


def failure_document(error: SupersessionError, authority: dict[str, Any] | None = None) -> dict[str, Any]:
    document = {
        "schemaVersion": EVIDENCE_SCHEMA_VERSION,
        "evidenceId": "EPIC-6-COMPLETION-SUPERSESSION-v1",
        "issuedOn": date.today().isoformat(),
        "result": error.state,
        "stories": [],
        "assertionLedger": [
            {"id": error.code, "subject": error.story_id or "checkpoint", "state": error.state, "message": error.message}
        ],
        "decisionRequired": True,
        "implementationHold": "ACTIVE",
        "releaseAuthorized": False,
    }
    if authority is not None:
        document["authorityBundle"] = authority
    return document


def markdown(document: dict[str, Any]) -> str:
    lines = [
        "# Epic 6 Completion Supersession Evidence v1",
        "",
        f"- Result: `{document['result']}`",
        f"- Issued on: `{document['issuedOn']}`",
        f"- Implementation hold: `{document['implementationHold']}`",
        "- Release authorized: `false`",
        "- Independent decision required: `true`",
        "",
        "## Immutable reconstruction",
        "",
    ]
    authority = document.get("authorityBundle")
    if authority is not None:
        lines.extend(
            [
                f"- Planning candidate: `{authority['candidateCommit']}`",
                f"- Authority bundle: `{authority['path']}`",
                f"- Authority bundle SHA-256: `{authority['sha256']}`",
                "",
            ]
        )
    for story in document.get("stories", []):
        lines.extend(
            [
                f"### Story {story['storyId']}",
                "",
                f"- Candidate: `{story['candidateCommit']}`",
                f"- Actual done: `{story['doneCommit']}`",
                f"- Exact changed paths: `{len(story['rawDiff'])}`",
                f"- Candidate/done root gitlinks: `{len(story['candidateRootGitlinks'])}` / `{len(story['doneRootGitlinks'])}`",
                f"- Promotion result: `{story['checks']['promotion']['result']}`",
                f"- Rebuilt test commands: `{len(story['checks']['tests'])}` passed",
                "",
            ]
        )
    lines.extend(["## Boundary", ""])
    if document["result"] == "PASS":
        lines.append(
            "This evidence can support an independent acceptance-evidence supersession decision. It does not rewrite either completed story, lift the implementation hold, start a successor, or authorize release."
        )
    else:
        lines.append(
            "This evidence cannot support acceptance-evidence supersession while reconstruction is not PASS. The blocker must be resolved and the exact checkpoint rerun; completed stories remain immutable and the implementation hold stays active."
        )
    lines.append("")
    return "\n".join(lines)


def write_output(path: str | None, content: bytes) -> None:
    if path is None:
        return
    target = Path(path)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(content)


def parse_args(argv: Sequence[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--execute-tests", action="store_true")
    parser.add_argument("--planning-candidate")
    parser.add_argument("--authority-bundle")
    parser.add_argument("--current-proof", action="store_true")
    # Opting out must be an explicit, recorded choice: the default blocks so uncommitted bytes are
    # never attributed to the resolved commit.
    parser.add_argument("--allow-dirty-worktree", action="store_true")
    parser.add_argument("--output-json")
    parser.add_argument("--output-md")
    return parser.parse_args(argv)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv)
    if args.current_proof:
        # Additive V13 route: never shares state, output paths, or the
        # historical CLI contract with the `reconstruct()` branch below.
        try:
            document = current_proof(
                Path(args.repository),
                execute_tests=args.execute_tests,
                allow_dirty_worktree=args.allow_dirty_worktree,
            )
            exit_code = 0
        except SupersessionError as error:
            document = current_proof_failure_document(error)
            exit_code = 2 if error.state == "BLOCKED" else 1
        serialized = (json.dumps(document, indent=2, sort_keys=True) + "\n").encode()
        write_output(args.output_json, serialized)
        write_output(args.output_md, current_proof_markdown(document).encode())
        if args.output_json is None:
            sys.stdout.buffer.write(serialized)
        return exit_code
    try:
        document = reconstruct(
            Path(args.repository),
            execute_tests=args.execute_tests,
            planning_candidate=args.planning_candidate,
            authority_bundle=args.authority_bundle,
        )
        exit_code = 0
    except SupersessionError as error:
        authority = None
        if args.planning_candidate is not None and args.authority_bundle is not None:
            try:
                authority = bind_authority_bundle(
                    Path(args.repository),
                    args.planning_candidate,
                    args.authority_bundle,
                )
            except SupersessionError as authority_error:
                if error.code not in {
                    "E6_AUTHORITY_BUNDLE_UNAVAILABLE",
                    "E6_AUTHORITY_BUNDLE_PC_MISMATCH",
                    "E6_HISTORY_UNAVAILABLE",
                }:
                    error = authority_error
        document = failure_document(error, authority)
        exit_code = 2 if error.state == "BLOCKED" else 1
    serialized = (json.dumps(document, indent=2, sort_keys=True) + "\n").encode()
    write_output(args.output_json, serialized)
    write_output(args.output_md, markdown(document).encode())
    if args.output_json is None:
        sys.stdout.buffer.write(serialized)
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
