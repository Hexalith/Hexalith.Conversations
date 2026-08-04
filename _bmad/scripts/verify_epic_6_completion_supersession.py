#!/usr/bin/env python3
"""Reconstruct immutable Epic 6 completion evidence and fail closed."""

from __future__ import annotations

import argparse
from datetime import date
import hashlib
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
GIT_TIMEOUT = 60
COMMAND_TIMEOUT = 900


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


def raw_diff(repository: Path, candidate: str, done: str) -> list[dict[str, str]]:
    fields = [field for field in git(repository, "diff", "--raw", "--no-abbrev", "-z", candidate, done, "--").stdout.split(b"\0") if field]
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


def skipped_count(output: str) -> int:
    values = [int(value) for value in re.findall(r"(?i)\bskipped\s*[:=]?\s*(\d+)\b", output)]
    return max(values, default=0)


def not_run_count(output: str) -> int:
    values = [
        int(value)
        for value in re.findall(r"(?i)\bnot[\s-]*run\s*[:=]?\s*(\d+)\b", output)
    ]
    return max(values, default=0)


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


def execute_story_checks(repository: Path, story: dict[str, Any], done_rows: Sequence[dict[str, str]]) -> dict[str, Any]:
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


def failure_document(error: SupersessionError) -> dict[str, Any]:
    return {
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
    lines.extend(
        [
            "## Boundary",
            "",
            "This evidence can support an independent acceptance-evidence supersession decision. It does not rewrite either completed story, lift the implementation hold, start a successor, or authorize release.",
            "",
        ]
    )
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
    parser.add_argument("--output-json")
    parser.add_argument("--output-md")
    return parser.parse_args(argv)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        document = reconstruct(
            Path(args.repository),
            execute_tests=args.execute_tests,
            planning_candidate=args.planning_candidate,
            authority_bundle=args.authority_bundle,
        )
        exit_code = 0
    except SupersessionError as error:
        document = failure_document(error)
        exit_code = 2 if error.state == "BLOCKED" else 1
    serialized = (json.dumps(document, indent=2, sort_keys=True) + "\n").encode()
    write_output(args.output_json, serialized)
    write_output(args.output_md, markdown(document).encode())
    if args.output_json is None:
        sys.stdout.buffer.write(serialized)
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
