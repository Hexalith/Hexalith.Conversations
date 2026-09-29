#!/usr/bin/env python3
"""Inspect committed AD-4 inputs without authenticating or publishing acceptance."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys
from types import ModuleType
from typing import Any


HOST = Path(__file__).resolve().parents[2]
SCHEMA_PATH = "_bmad/schemas/story-7.1-ad4-readiness-result-v1.schema.json"
RECORD_SCHEMA_PATH = "_bmad/schemas/story-final-record-v2.schema.json"
SCHEMA_VERSION = "hexalith.conversations.story-7.1-ad4-readiness-result.v1"
COMMIT = re.compile(r"^[0-9a-f]{40}$")
# Exact existing bytes recorded by the approved proposal and its reused audit.
PRESERVED = {
    "7.1": (
        "0a4ede3074fca55853f2fd59f065677cacea3eb700491cbc87594e117557e2f9",
        "044ac80a72a2f58587dbd9a53463d75567a75ac99797028fa4cc9a413c3dab43",
    ),
    "7.2": (
        "15212a33103bf36ccefb3778853d3e4a8875d1c9e82a0c3d3104dae54e361ef5",
        "0821f9b934506a87104279e67f51441ae3e9bbbad7a71cf3f3c1ab0786770e81",
    ),
}


class InspectionError(Exception):
    """Carry one stable invalid-input or unavailable-evidence diagnostic."""

    def __init__(self, code: str, detail: str, state: str = "BLOCKED") -> None:
        super().__init__(detail)
        self.code, self.detail, self.state = code, detail, state


class JsonArgumentParser(argparse.ArgumentParser):
    """Keep every CLI outcome in the JSON result channel, including help."""

    def error(self, message: str) -> None:
        raise InspectionError("AD4_CLI_INVALID", message, "FAIL")


def digest(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def path_row(path: str) -> dict[str, Any]:
    return {"path": path, "status": "NOT_INSPECTED", "mode": None,
            "objectType": None, "objectId": None, "sha256": None}


def empty_result() -> dict[str, Any]:
    return {
        "schemaVersion": SCHEMA_VERSION, "stage": "READINESS",
        "result": "BLOCKED", "exitCode": 2, "diagnostic": "AD4_TERMINAL_UNSUPPORTED",
        "effectiveHold": "ACTIVE", "implementationHold": "ACTIVE",
        "ownerApprovalClaimed": False, "executionAllowed": False,
        "releaseAuthorized": False, "pushAuthorized": False,
        "terminalVerification": "UNSUPPORTED", "terminalPublication": "UNSUPPORTED",
        "acceptance": "UNESTABLISHED", "acceptedMainCommit": None,
        "candidate": None, "gitlinks": [], "gitmodules": path_row(".gitmodules"),
        "prerequisites": [], "preservedPairs": [
            {"storyId": story, "status": "NOT_INSPECTED",
             "json": path_row(f"docs/release-evidence/story-{story}-final-record-v2.json"),
             "markdown": path_row(f"docs/release-evidence/story-{story}-final-record-v2.md"),
             "expectedJsonSha256": hashes[0], "expectedMarkdownSha256": hashes[1]}
            for story, hashes in PRESERVED.items()
        ],
        "hostBindings": [], "blockers": [], "assertionLedger": [],
    }


def assertion(result: dict[str, Any], code: str, subject: str, state: str, detail: str) -> None:
    row = {"code": code, "subject": subject, "state": state,
           "detail": detail or "No additional diagnostic detail is available."}
    result["assertionLedger"].append(row)
    if state in ("FAIL", "BLOCKED"):
        result["blockers"].append(dict(row))


def finish(result: dict[str, Any]) -> dict[str, Any]:
    assertion(result, "AD4_TERMINAL_UNSUPPORTED", "terminal-acceptance", "BLOCKED",
              "Terminal tooling, trust adoption, authenticated entry/integration provenance, "
              "post-integration proof and final-record binding are not verified by stage 1.")
    first = next((row for row in result["blockers"] if row["state"] == "FAIL"),
                 result["blockers"][0])
    result.update(result=first["state"], exitCode=1 if first["state"] == "FAIL" else 2,
                  diagnostic=first["code"])
    return result


def load_host_module(result: dict[str, Any], filename: str, name: str) -> Any:
    # Execute precisely the checked/bound source snapshot, never cached bytecode.
    path = f"_bmad/scripts/{filename}"
    content = bind_host(result, path)
    module = ModuleType(name)
    module.__file__ = str(HOST / path)
    module.__package__ = ""
    code = compile(content, module.__file__, "exec", dont_inherit=True)
    sys.modules[name] = module
    exec(code, module.__dict__)
    return module


def bind_host(result: dict[str, Any], path: str) -> bytes:
    """Read one regular snapshot through directory descriptors without following links."""
    target = HOST.absolute() / path
    directory_fd = None
    leaf_fd = None
    try:
        if not path or Path(path).is_absolute() or any(part in (".", "..") for part in path.split("/")):
            raise InspectionError("AD4_HOST_UNAVAILABLE", f"invalid host path: {path}")
        directory_flags = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW | os.O_CLOEXEC
        directory_fd = os.open(target.anchor, directory_flags)
        for component in target.parts[1:-1]:
            next_fd = os.open(component, directory_flags, dir_fd=directory_fd)
            os.close(directory_fd)
            directory_fd = next_fd
        leaf_fd = os.open(target.name, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK | os.O_CLOEXEC,
                          dir_fd=directory_fd)
        mode = os.fstat(leaf_fd).st_mode
        if not stat.S_ISREG(mode):
            raise InspectionError("AD4_HOST_UNAVAILABLE", f"host file is not regular: {path}")
        with os.fdopen(leaf_fd, "rb") as stream:
            leaf_fd = None
            content = stream.read()
    except OSError as error:
        raise InspectionError("AD4_HOST_UNAVAILABLE", f"cannot safely read host file {path}: {error}") from error
    finally:
        if leaf_fd is not None:
            os.close(leaf_fd)
        if directory_fd is not None:
            os.close(directory_fd)
    result["hostBindings"].append({"path": path, "mode": f"{stat.S_IMODE(mode):04o}",
                                   "sha256": digest(content), "source": "HOST_WORKTREE_UNTRUSTED"})
    return content


def safe_git(entry: Any, root: Path, *arguments: str,
             allowed: tuple[int, ...] = (0,)) -> subprocess.CompletedProcess[bytes]:
    environment = entry.git_environment()
    environment.update(GIT_NO_LAZY_FETCH="1", GIT_OPTIONAL_LOCKS="0", GIT_GRAFT_FILE=os.devnull)
    try:
        completed = subprocess.run(
            (entry.GIT_EXECUTABLE, "--no-replace-objects", "-c", "protocol.allow=never",
             "-c", "core.fsmonitor=false", "-c", "core.hooksPath=/dev/null",
             "-C", str(root), *arguments),
            capture_output=True, check=False, timeout=30, env=environment,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise InspectionError("AD4_GIT_UNAVAILABLE", str(error)) from error
    if completed.returncode not in allowed:
        raise InspectionError("AD4_OBJECT_UNAVAILABLE",
                              completed.stderr.decode("utf-8", errors="replace").strip() or "Git failed")
    return completed


def checked_path(entry: Any, value: str) -> str:
    try:
        return entry.safe_path(value)
    except entry.EntryAuthorityError as error:
        raise InspectionError("AD4_PATH_INVALID", error.detail, "FAIL") from error


def strict_json(entry: Any, content: bytes) -> dict[str, Any]:
    # The reused parser rejects duplicate keys; also reject non-JSON numeric constants.
    def invalid_constant(value: str) -> None:
        raise ValueError(f"non-JSON numeric constant: {value}")

    try:
        value = json.loads(content.decode("utf-8"), object_pairs_hook=entry.reject_duplicate_keys,
                           parse_constant=invalid_constant)
        if not isinstance(value, dict):
            raise ValueError("top-level JSON must be an object")
        return value
    except (UnicodeError, ValueError, RecursionError) as error:
        raise InspectionError("AD4_JSON_INVALID", str(error), "FAIL") from error


def inspect_path(entry: Any, root: Path, candidate: str, row: dict[str, Any],
                 result: dict[str, Any], *, prerequisite: bool = False) -> bytes | None:
    path = checked_path(entry, row["path"])
    try:
        raw = entry.run_git(root, "ls-tree", "-z", candidate, "--", path).stdout
        if not raw:
            row["status"] = "ABSENT"
            assertion(result, "AD4_PREREQUISITE_ABSENT" if prerequisite else "AD4_EVIDENCE_ABSENT",
                      path, "BLOCKED", "Path is absent from the candidate tree.")
            return None
        mode, kind, object_id = entry.tree_record(root, candidate, path)
        row.update(mode=mode, objectType=kind, objectId=object_id)
        if mode != "100644" or kind != "blob":
            raise InspectionError("AD4_MODE_INVALID", "Expected a mode-100644 regular blob.", "FAIL")
        content = entry.run_git(root, "cat-file", "blob", object_id).stdout
        row.update(sha256=digest(content), status="PRESENT_UNVERIFIED")
        if prerequisite:
            if path != entry.RECOVERY_RUNBOOK_PATH:
                strict_json(entry, content)
            else:
                content.decode("utf-8", errors="strict")
            assertion(result, "AD4_PREREQUISITE_UNVERIFIED", path, "BLOCKED",
                      "Committed bytes are present; syntax and presence establish no gate or approval.")
        return content
    except InspectionError as error:
        row["status"] = "INVALID" if error.state == "FAIL" else "UNAVAILABLE"
        assertion(result, error.code, path, error.state, error.detail)
    except (entry.EntryAuthorityError, UnicodeError, ValueError) as error:
        row["status"] = "INVALID"
        assertion(result, "AD4_EVIDENCE_INVALID", path, "FAIL", str(error))
    return None


def inspect(result: dict[str, Any], repository: str, candidate: str) -> None:
    if COMMIT.fullmatch(candidate) is None:
        raise InspectionError("AD4_CANDIDATE_INVALID", "--candidate requires a full lowercase commit SHA.", "FAIL")
    from jsonschema import Draft202012Validator

    entry = load_host_module(result, "publish_story_7_1_entry_authority.py", "_ad4_host_entry")
    # A private module instance adapts only Git execution; historical files are untouched.
    entry.run_git = lambda root, *args, allowed=(0,): safe_git(entry, root, *args, allowed=allowed)
    generator = load_host_module(result, "generate_story_record.py", "_ad4_host_record")
    paths = sorted((entry.AUTHORITY_PATH, entry.OPERATIONAL_ENVELOPE_PATH,
                    entry.RECOVERY_RUNBOOK_PATH, *entry.GATE_EVIDENCE_PATHS.values(),
                    *entry.LANDING_ZONE_SUCCESSOR_PATHS.values(),
                    *entry.LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values()))
    result["prerequisites"] = [path_row(path) for path in paths]
    for path in ("_bmad/scripts/inspect_story_7_1_acceptance.py", SCHEMA_PATH):
        bind_host(result, path)
    schema = strict_json(entry, bind_host(result, RECORD_SCHEMA_PATH))
    Draft202012Validator.check_schema(schema)
    record_validator = Draft202012Validator(schema)

    root = Path(repository).resolve(strict=True)
    observed_root = Path(entry.run_git(root, "rev-parse", "--show-toplevel").stdout.decode().strip()).resolve()
    if root != observed_root:
        raise InspectionError("AD4_REPOSITORY_INVALID", "--repository must name the repository root.", "FAIL")
    if entry.run_git(root, "rev-parse", "--is-shallow-repository").stdout.strip() != b"false":
        raise InspectionError("AD4_HISTORY_UNAVAILABLE", "Shallow history cannot establish readiness inputs.")
    partial = entry.run_git(root, "config", "--get-regexp",
                           r"^(extensions\.partialclone|remote\..*\.(promisor|partialclonefilter))$", allowed=(0, 1))
    if partial.stdout.strip():
        raise InspectionError("AD4_HISTORY_UNAVAILABLE", "Partial-clone configuration is unsupported; no fetch attempted.")
    if entry.run_git(root, "cat-file", "-t", candidate).stdout.strip() != b"commit":
        raise InspectionError("AD4_CANDIDATE_INVALID", "Candidate object must itself be a commit.", "FAIL")
    result["candidate"] = {"commit": candidate, "tree": entry.commit_tree(root, candidate),
                           "parents": list(entry.commit_parents(root, candidate))}
    history = entry.run_git(root, "rev-list", "--objects", "--missing=print", candidate).stdout
    if any(line.startswith(b"?") for line in history.splitlines()):
        raise InspectionError("AD4_HISTORY_UNAVAILABLE", "Reachable history contains unavailable objects.")
    assertion(result, "AD4_CANDIDATE_OBSERVED", candidate, "OBSERVED",
              "Commit, tree and ordered parents read from complete root history; no integration provenance inferred.")
    modules = inspect_path(entry, root, candidate, result["gitmodules"], result)
    if modules is not None:
        try:
            result["gitlinks"] = entry.root_gitlinks(root, candidate)
            assertion(result, "AD4_GITLINKS_OBSERVED", ".gitmodules", "OBSERVED",
                      "Ordinal raw root gitlinks equal the ten declared paths; submodules were not traversed.")
        except entry.EntryAuthorityError as error:
            result["gitmodules"]["status"] = "INVALID"
            code = "AD4_PATH_INVALID" if error.code == "V23_PATH_ESCAPE" else "AD4_GITLINKS_INVALID"
            assertion(result, code, ".gitmodules", "FAIL", error.detail)
        except (UnicodeError, ValueError) as error:
            result["gitmodules"]["status"] = "INVALID"
            assertion(result, "AD4_GITLINKS_INVALID", ".gitmodules", "FAIL", str(error))
    for row in result["prerequisites"]:
        inspect_path(entry, root, candidate, row, result, prerequisite=True)

    for pair in result["preservedPairs"]:
        raw_json = inspect_path(entry, root, candidate, pair["json"], result)
        raw_markdown = inspect_path(entry, root, candidate, pair["markdown"], result)
        states = {pair["json"]["status"], pair["markdown"]["status"]}
        pair["status"] = next((state for state in ("INVALID", "UNAVAILABLE", "ABSENT") if state in states), "UNVERIFIED")
        if raw_json is None or raw_markdown is None:
            continue
        try:
            record = strict_json(entry, raw_json)
            errors = list(record_validator.iter_errors(record))
            if errors:
                raise InspectionError("AD4_RECORD_SCHEMA_INVALID", errors[0].message, "FAIL")
            if (record["storyId"] != pair["storyId"]
                    or any(record["outputs"][kind]["path"] != pair[kind]["path"] for kind in ("json", "markdown"))):
                raise InspectionError("AD4_RECORD_BINDING_INVALID", "Story or output paths differ from the preserved pair.", "FAIL")
            problems = generator.v2_verify_pair(raw_json, raw_markdown)
            if problems:
                raise InspectionError("AD4_RECORD_DIGEST_INVALID", "; ".join(problems), "FAIL")
            if (digest(raw_json) != pair["expectedJsonSha256"]
                    or digest(raw_markdown) != pair["expectedMarkdownSha256"]):
                raise InspectionError("AD4_RECORD_BYTES_CHANGED", "Exact preserved full-file SHA-256 values differ.", "FAIL")
            pair["status"] = "VERIFIED"
            assertion(result, "AD4_PRESERVED_PAIR_VERIFIED", pair["storyId"], "OBSERVED",
                      "Host schema, existing pair digests and preserved full-file hashes agree; no scenarios rerun.")
        except InspectionError as error:
            pair["status"] = "INVALID"
            assertion(result, error.code, pair["storyId"], error.state, error.detail)
        except UnicodeError as error:
            pair["status"] = "INVALID"
            assertion(result, "AD4_RECORD_CONTENT_INVALID", pair["storyId"], "FAIL", str(error))


def main(argv: list[str] | None = None) -> int:
    # Importing the read-only verifier must not create or refresh host bytecode files.
    sys.dont_write_bytecode = True
    result = empty_result()
    try:
        parser = JsonArgumentParser(add_help=False, allow_abbrev=False)
        parser.add_argument("--repository", default=".")
        parser.add_argument("--candidate", required=True)
        parser.add_argument("--check", action="store_true")
        args = parser.parse_args(argv)
        inspect(result, args.repository, args.candidate)
    except InspectionError as error:
        assertion(result, error.code, "invocation", error.state, error.detail)
    except (OSError, ImportError) as error:
        assertion(result, "AD4_ENVIRONMENT_UNAVAILABLE", "invocation", "BLOCKED", str(error))
    except Exception as error:
        # Unexpected host/schema/helper failures remain unavailable evidence, never success.
        assertion(result, "AD4_INSPECTION_UNAVAILABLE", "invocation", "BLOCKED", str(error))
    finish(result)
    print(json.dumps(result, ensure_ascii=True, indent=2, allow_nan=False))
    return result["exitCode"]


if __name__ == "__main__":
    raise SystemExit(main())
