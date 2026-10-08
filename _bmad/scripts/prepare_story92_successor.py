#!/usr/bin/env python3
"""Prepare the authorized current-main successor without publishing acceptance.

The source repository must be an isolated, clean checkout of the exact authorized
commit. The authority/output repository may contain excluded concurrent work: only
the two hash-pinned scope documents are consumed from it. Build measurement and
proposal preparation are separate so two preparations reuse exact measured bytes.
No historical generator, approval, migration, or accepted record is rewritten.
"""

from __future__ import annotations

import argparse
from copy import deepcopy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
from typing import Any

import jsonschema


SCRIPT_ROOT = Path(__file__).resolve().parents[2]
SCHEMA_PATH = "_bmad/schemas/story-9.2-current-main-successor-proposal-v1.schema.json"
AUTHORIZATION_PATH = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-authorization-v1.json"
SCOPE_PATH = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-proposal-v1.json"
JSON_NAME = "conformance-oracle-tiering-migration-v4-proposal.json"
MARKDOWN_NAME = "conformance-oracle-tiering-migration-review-v4.md"
BUILD_NAME = "build-evidence.json"
VERSION = "hexalith.conversations.story-9.2-current-main-successor-proposal.v1"
BUILD_VERSION = "hexalith.conversations.story-9.2-successor-build-receipt.v1"
PINS = {
    "authorizationSha256": "e70ab5de8df530a29e575def28067cde2f0005de1420121182729a49f7c2b102",
    "scopeSha256": "e3835746809ae7a30184d25d1a53303ad821ebc8c2f5165ee8f63630a7a66c61",
    "scopeMaterialSha256": "852db3f13f442300f71142cdf094b2e4feb54cb77f90ef0a48f4905e4c51c4df",
    "candidate": "59e72b82cdc2ecc58971e34594c4e8b62896518b",
    "baseline": "51aa06b856bcb0aaf22013153cfe02a51b046156",
    "originalCandidate": "c44f6b7b116b17453aa213d39cd244a3e0cbe1dd",
    "originalPublication": "97d300d9bfda3ed771d35bea9f41a91356c907a9",
    "completedOriginalScopeSuccessor": {
        "candidate": "1275e5cfb2ec8a77095ec6f097dcdfb62ce43650",
        "publication": "7183038b367863ac769546206b951681042c11b3",
        "lifecycle": "19d8f234d04b3ebc7a1fe8d37445803cba0bd403",
    },
    "counts": {"committedChangedPaths": 97, "productionPaths": 20,
               "changedRootGitlinks": 9, "owningPromotionCommits": 10},
}


class PreparationError(Exception):
    """A specific blocker that never authorizes acceptance or writes a proposal."""

    def __init__(self, code: str, detail: str) -> None:
        super().__init__(detail)
        self.code = code


def require(condition: Any, code: str, detail: str) -> None:
    if not condition:
        raise PreparationError(code, detail)


def sha(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def canonical(value: Any) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False,
                      allow_nan=False).encode("utf-8")


def json_bytes(value: Any) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False, allow_nan=False) + "\n").encode("utf-8")


def parse(content: bytes) -> Any:
    def pairs(items: list[tuple[str, Any]]) -> dict[str, Any]:
        result = {}
        for key, value in items:
            require(key not in result, "SUCCESSOR_INPUT_INVALID", "Duplicate JSON key: " + key)
            result[key] = value
        return result

    try:
        return json.loads(content.decode("utf-8"), object_pairs_hook=pairs,
                          parse_constant=lambda value: (_ for _ in ()).throw(ValueError(value)))
    except (ValueError, UnicodeError) as error:
        raise PreparationError("SUCCESSOR_INPUT_INVALID", "Invalid strict UTF-8 JSON") from error


def contained(root: Path, relative: str) -> Path:
    require(isinstance(relative, str) and bool(relative) and not relative.startswith("/")
            and "\\" not in relative and all(part not in ("", ".", "..") for part in relative.split("/")),
            "SUCCESSOR_OUTPUT_INVALID", "Use a normalized contained relative path")
    target = root
    require(not root.is_symlink(), "SUCCESSOR_OUTPUT_INVALID", "Root aliases a symlink")
    for part in relative.split("/"):
        target /= part
        require(not target.is_symlink(), "SUCCESSOR_OUTPUT_INVALID", "A path component is a symlink: " + relative)
    require(target.resolve().is_relative_to(root.resolve()), "SUCCESSOR_OUTPUT_INVALID", "Path escapes its root")
    return target


def read(root: Path, relative: str) -> bytes:
    path = contained(root, relative)
    require(path.is_file(), "SUCCESSOR_INPUT_MISSING", "Missing regular input: " + relative)
    return path.read_bytes()


def git_environment() -> dict[str, str]:
    environment = dict(os.environ)
    for name in list(environment):
        if name in {"GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_OBJECT_DIRECTORY", "GIT_COMMON_DIR",
                    "GIT_ALTERNATE_OBJECT_DIRECTORIES", "GIT_NAMESPACE"} or name.startswith("GIT_CONFIG_"):
            environment.pop(name, None)
    environment.update(GIT_CONFIG_GLOBAL=os.devnull, GIT_CONFIG_NOSYSTEM="1", GIT_TERMINAL_PROMPT="0")
    return environment


def git(root: Path, *arguments: str) -> bytes:
    try:
        result = subprocess.run(["git", "-C", str(root), *arguments], capture_output=True,
                                check=False, timeout=60, env=git_environment())
    except (OSError, subprocess.TimeoutExpired) as error:
        raise PreparationError("SUCCESSOR_GIT_UNAVAILABLE", "Cannot inspect local Git objects") from error
    require(result.returncode == 0, "SUCCESSOR_GIT_INVALID", "Git failed: " + " ".join(arguments))
    return result.stdout


def tree(root: Path, revision: str) -> dict[str, dict[str, Any]]:
    result = {}
    for item in git(root, "ls-tree", "-r", "-z", "--full-tree", revision).split(b"\0"):
        if item:
            metadata, path = item.split(b"\t", 1)
            mode, kind, identity = metadata.decode().split()
            result[path.decode()] = {"mode": mode, "type": kind, "object": identity}
    return result


def changes(root: Path, before: str, after: str) -> list[dict[str, Any]]:
    old, new = tree(root, before), tree(root, after)
    result = []
    for path in sorted(set(old) | set(new)):
        if old.get(path) != new.get(path):
            row = {"path": path, "status": "A" if path not in old else "D" if path not in new else "M"}
            for key, entries in (("before", old), ("after", new)):
                value = deepcopy(entries.get(path))
                if value and value["type"] == "blob":
                    value["sha256"] = sha(git(root, "cat-file", "blob", value["object"]))
                row[key] = value
            result.append(row)
    return result


def promotions(root: Path, before: str, after: str) -> list[dict[str, Any]]:
    result = []
    for revision in git(root, "rev-list", "--reverse", f"{before}..{after}").decode().splitlines():
        parents = git(root, "rev-list", "--parents", "-n", "1", revision).decode().split()[1:]
        require(len(parents) == 1, "SUCCESSOR_SCOPE_DRIFT", "Only the authorized single-parent history is allowed")
        rows = [row for row in changes(root, parents[0], revision)
                if any(row[key] and row[key]["mode"] == "160000" for key in ("before", "after"))]
        if rows:
            result.append({"commit": revision, "parent": parents[0], "gitlinks": rows})
    return result


def verify_tracked_bytes(root: Path, entries: dict[str, dict[str, Any]], code: str) -> None:
    """Compare every consumed tracked byte even when index flags hide its dirt."""
    rows = [(path, entry) for path, entry in sorted(entries.items()) if entry["type"] == "blob"]
    payload = "".join(entry["object"] + "\n" for _, entry in rows).encode()
    try:
        result = subprocess.run(["git", "-C", str(root), "cat-file", "--batch"], input=payload,
                                capture_output=True, check=False, timeout=60, env=git_environment())
    except (OSError, subprocess.TimeoutExpired) as error:
        raise PreparationError("SUCCESSOR_GIT_UNAVAILABLE", "Cannot compare candidate source bytes") from error
    require(result.returncode == 0, "SUCCESSOR_GIT_INVALID", "Cannot read candidate blobs")
    position = 0
    for path, entry in rows:
        end = result.stdout.index(b"\n", position)
        header = result.stdout[position:end].split()
        require(header[:2] == [entry["object"].encode(), b"blob"], "SUCCESSOR_GIT_INVALID", "Unexpected Git blob")
        size = int(header[2])
        committed = result.stdout[end + 1:end + 1 + size]
        position = end + size + 2
        parent = Path(path).parent.as_posix()
        target = (contained(root, parent) if parent != "." else root) / Path(path).name
        matches = (target.is_symlink() and os.fsencode(os.readlink(target)) == committed
                   if entry["mode"] == "120000" else
                   entry["mode"] in ("100644", "100755") and not target.is_symlink()
                   and target.is_file() and target.read_bytes() == committed
                   and bool(target.stat().st_mode & 0o111) == (entry["mode"] == "100755"))
        require(matches, code, "Tracked source bytes differ from Git: " + path)


def reject_ignored_inputs(root: Path, code: str) -> None:
    """Do not consume extra source/configuration hidden by local ignore rules."""
    for item in git(root, "ls-files", "--others", "--ignored", "--exclude-standard", "-z").split(b"\0"):
        if not item:
            continue
        path = item.decode()
        parts = Path(path).parts
        if parts[0] in ("artifacts", ".venv", "node_modules", ".vs") or path.startswith("_bmad/render/"):
            continue
        if any(part in ("bin", "obj") for part in parts):
            continue
        if "__pycache__" in parts and Path(path).suffix == ".pyc":
            continue
        require(False, code, "Ignored input is outside the approved tree and explicit generated-output allowances: " + path)


def authority(authority_root: Path) -> tuple[dict[str, Any], dict[str, Any]]:
    authorization_bytes = read(authority_root, AUTHORIZATION_PATH)
    scope_bytes = read(authority_root, SCOPE_PATH)
    require(sha(authorization_bytes) == PINS["authorizationSha256"], "SUCCESSOR_AUTHORIZATION_INVALID",
            "The exact genuine scope authorization is missing or altered")
    require(sha(scope_bytes) == PINS["scopeSha256"], "SUCCESSOR_SCOPE_INVALID", "The reviewed scope bytes changed")
    authorization, scope = parse(authorization_bytes), parse(scope_bytes)
    require(set(authorization) == {"schemaVersion", "storyId", "status", "authorization", "proposal",
            "measuredCommittedCandidate", "originalBaseline", "workingTreeChangesIncluded", "qualityDecisionIncluded",
            "preserveOriginalBaselineFrozenContractsApprovalsAndAcceptedRecords"}
            and authorization["schemaVersion"] == "hexalith.conversations.story-9.2-current-main-scope-authorization.v1"
            and authorization["storyId"] == "9.2" and authorization["status"] == "authorized"
            and set(authorization["authorization"]) == {"actor", "authorizedOn", "statement", "evidence"}
            and authorization["authorization"]["actor"] == "user"
            and authorization["authorization"]["statement"] == "yes"
            and authorization["workingTreeChangesIncluded"] is False
            and authorization["qualityDecisionIncluded"] is False
            and authorization["preserveOriginalBaselineFrozenContractsApprovalsAndAcceptedRecords"] is True
            and authorization["measuredCommittedCandidate"] == PINS["candidate"]
            and authorization["originalBaseline"] == PINS["baseline"]
            and authorization["proposal"] == {"path": SCOPE_PATH, "sha256": PINS["scopeSha256"],
                                               "proposalSha256": PINS["scopeMaterialSha256"]},
            "SUCCESSOR_AUTHORIZATION_INVALID", "Scope authorization is not the closed preparation-only decision")
    material = {key: value for key, value in scope.items() if key != "proposalSha256"}
    require(sha(canonical(material)) == scope.get("proposalSha256") == PINS["scopeMaterialSha256"]
            and scope.get("status") == "prepared-unapproved"
            and scope.get("authorizationClaimed") is False and scope.get("approvalClaimed") is False
            and scope.get("originalBaseline") == PINS["baseline"]
            and scope.get("originalAcceptedSourceCandidate") == PINS["originalCandidate"]
            and scope.get("originalPublication") == PINS["originalPublication"]
            and scope.get("measuredCommittedCandidate") == PINS["candidate"]
            and scope.get("completedOriginalScopeSuccessor") == PINS["completedOriginalScopeSuccessor"],
            "SUCCESSOR_SCOPE_INVALID", "Reviewed scope identity or material digest changed")
    return authorization, scope


def validate_source(root: Path, scope: dict[str, Any]) -> dict[str, Any]:
    require(git(root, "rev-parse", "--show-toplevel").decode().strip() == str(root),
            "SUCCESSOR_SOURCE_INVALID", "Source must be a repository root")
    require(git(root, "rev-parse", "HEAD^{commit}").decode().strip() == PINS["candidate"],
            "SUCCESSOR_CANDIDATE_INVALID", "Use the exact authorized isolated source commit")
    require(not git(root, "diff", "--cached", "HEAD", "--name-only", "--ignore-submodules=all")
            and not git(root, "ls-files", "--others", "--exclude-standard", "-z"),
            "SUCCESSOR_SOURCE_DIRTY", "Source inputs contain excluded working-tree edits")
    require(git(root, "merge-base", PINS["originalCandidate"], PINS["candidate"]).decode().strip()
            == PINS["originalCandidate"], "SUCCESSOR_SCOPE_DRIFT", "Original accepted candidate is not an ancestor")
    require(git(root, "merge-base", PINS["baseline"], PINS["candidate"]).decode().strip() == PINS["baseline"],
            "SUCCESSOR_SCOPE_DRIFT", "Original baseline is not an ancestor")
    measured_changes = changes(root, PINS["originalCandidate"], PINS["candidate"])
    measured_promotions = promotions(root, PINS["originalCandidate"], PINS["candidate"])
    counts = {"committedChangedPaths": len(measured_changes),
              "productionPaths": sum(row["path"].startswith("src/") for row in measured_changes),
              "changedRootGitlinks": sum(any(row[key] and row[key]["mode"] == "160000"
                                            for key in ("before", "after")) for row in measured_changes),
              "owningPromotionCommits": len(measured_promotions)}
    require(counts == scope.get("counts") == PINS["counts"]
            and measured_changes == scope["committedChangesSinceAcceptedCandidate"]
            and measured_promotions == scope["rootGitlinkPromotionHistorySinceAcceptedCandidate"],
            "SUCCESSOR_SCOPE_DRIFT", "Changed paths, gitlinks, or owning promotion history differ from the approved scope")
    candidate_tree = tree(root, PINS["candidate"])
    verify_tracked_bytes(root, candidate_tree, "SUCCESSOR_SOURCE_DIRTY")
    reject_ignored_inputs(root, "SUCCESSOR_SOURCE_DIRTY")
    gitlinks = [{"path": path, **entry} for path, entry in sorted(candidate_tree.items()) if entry["mode"] == "160000"]
    modules = git(root, "config", "--blob", f"{PINS['candidate']}:.gitmodules", "--get-regexp", r"^submodule\..*\.path$")
    declared = sorted(line.split(None, 1)[1] for line in modules.decode().splitlines())
    require(declared == [row["path"] for row in gitlinks] and len(declared) == len(set(declared))
            and all(path.startswith("references/") and path.count("/") == 1 for path in declared),
            "SUCCESSOR_ENVIRONMENT_DRIFT", "Root gitlink inventory differs from root .gitmodules")
    for binding in scope["preservedProtectedEvidence"]:
        content = read(root, binding["path"])
        require(binding.get("unchangedFromOriginalPublication") is True
                and sha(content) == binding["sha256"] and len(content) == binding["sizeBytes"]
                and content == git(root, "show", f"{PINS['originalPublication']}:{binding['path']}"),
                "SUCCESSOR_PROTECTED_DRIFT", "Protected original evidence changed: " + binding["path"])
    # Full history is retained, including changes whose final bytes were reverted.
    history = [{"commit": revision, "parents": git(root, "rev-list", "--parents", "-n", "1", revision).decode().split()[1:],
                "tree": git(root, "rev-parse", f"{revision}^{{tree}}").decode().strip()}
               for revision in git(root, "rev-list", "--reverse", f"{PINS['originalCandidate']}..{PINS['candidate']}").decode().splitlines()]
    source_files = [{"path": path, **entry, "sha256": sha(read(root, path))}
                    for path, entry in sorted(candidate_tree.items()) if path.startswith("src/") and entry["type"] == "blob"]
    dependencies = []
    for row in gitlinks:
        dependency = root / row["path"]
        require(not dependency.is_symlink(), "SUCCESSOR_ENVIRONMENT_DRIFT", "Copied dependency is a symlink: " + row["path"])
        if (dependency / ".git").exists():
            require(git(dependency, "rev-parse", "HEAD^{commit}").decode().strip() == row["object"]
                    and not git(dependency, "diff", "--cached", "HEAD", "--name-only", "--ignore-submodules=all")
                    and not git(dependency, "ls-files", "--others", "--exclude-standard", "-z"),
                    "SUCCESSOR_ENVIRONMENT_DRIFT", "Copied root dependency differs from its approved source: " + row["path"])
            verify_tracked_bytes(dependency, tree(dependency, row["object"]), "SUCCESSOR_ENVIRONMENT_DRIFT")
            reject_ignored_inputs(dependency, "SUCCESSOR_ENVIRONMENT_DRIFT")
            dependencies.append({"path": row["path"], "commit": row["object"],
                                 "tree": git(dependency, "rev-parse", "HEAD^{tree}").decode().strip()})
    return {"candidate": PINS["candidate"], "tree": git(root, "rev-parse", "HEAD^{tree}").decode().strip(),
            "sourceFiles": source_files, "sourceFilesSha256": sha(canonical(source_files)),
            "actualProductionSurface": verifier().TIERING.surface(verifier().TIERING.WorkTree(root)),
            "rootGitlinks": gitlinks, "copiedRootDependencies": dependencies,
            "history": history, "historySha256": sha(canonical(history)),
            "changes": measured_changes, "promotions": measured_promotions, "counts": counts}


_VERIFIER: Any = None


def verifier() -> Any:
    global _VERIFIER
    if _VERIFIER is None:
        specification = importlib.util.spec_from_file_location("story92_successor_verifier", Path(__file__).with_name("verify_conformance_tiering.py"))
        assert specification and specification.loader
        _VERIFIER = importlib.util.module_from_spec(specification)
        sys.modules[specification.name] = _VERIFIER
        specification.loader.exec_module(_VERIFIER)
    return _VERIFIER


def output_targets(authority_root: Path, source_root: Path, directory: str, names: list[str]) -> list[Path]:
    require(re.fullmatch(r"artifacts/v9/9\.2/[A-Za-z0-9][A-Za-z0-9._-]*-v[1-9][0-9]*", directory),
            "SUCCESSOR_OUTPUT_INVALID", "Use an explicit versioned artifacts/v9/9.2/<name>-vN directory")
    target_directory = contained(authority_root, directory)
    require(not target_directory.exists() or target_directory.is_dir(), "SUCCESSOR_OUTPUT_INVALID", "Output directory is not a directory")
    targets = [contained(authority_root, directory + "/" + name) for name in names]
    # All existing files outside this directory are protected, including ignored
    # retained results, binaries, executable inputs, configuration, and hardlinks.
    protected_inodes = set()
    for root in {authority_root, source_root, SCRIPT_ROOT}:
        for parent, directories, files in os.walk(root, followlinks=False):
            directories[:] = [name for name in directories if name != ".git" and not (Path(parent) / name).is_symlink()
                              and Path(parent) / name != target_directory]
            for name in files:
                path = Path(parent) / name
                if path.is_file() and not path.is_symlink():
                    info = path.stat()
                    protected_inodes.add((info.st_dev, info.st_ino))
    seen = set()
    for target in targets:
        require(not target.exists() or target.is_file(), "SUCCESSOR_OUTPUT_INVALID", "Output is not a regular file")
        if target.exists():
            info = target.stat()
            identity = (info.st_dev, info.st_ino)
            require(identity not in protected_inodes and identity not in seen and info.st_nlink == 1,
                    "SUCCESSOR_OUTPUT_INVALID", "Output aliases a protected input or another output")
            seen.add(identity)
    return targets


def write_outputs(targets: list[Path], contents: list[bytes], *, immutable: bool = False) -> None:
    require(len(targets) == len(contents), "SUCCESSOR_OUTPUT_INVALID", "Output count differs")
    for target, content in zip(targets, contents, strict=True):
        require(not target.exists() or (not immutable and target.read_bytes() == content),
                "SUCCESSOR_OUTPUT_COLLISION", "Existing output differs; use a new versioned directory: " + target.name)
    created = []
    try:
        for target, content in zip(targets, contents, strict=True):
            if target.exists():
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            with target.open("xb") as output:
                output.write(content)
            created.append(target)
    except OSError as error:
        for target in created:
            target.unlink()
        raise PreparationError("SUCCESSOR_OUTPUT_WRITE_FAILED", "Failed to write complete outputs") from error


def build_commands() -> list[tuple[str, list[str]]]:
    result = []
    for tier, project in verifier().PROJECTS.items():
        result.extend([(tier + "-restore", ["dotnet", "restore", project, "-p:Configuration=Release",
                                             "--source", str(Path.home() / ".nuget/packages"), "-p:NuGetAudit=false"]),
                       (tier + "-build", ["dotnet", "build", project, "--configuration", "Release", "--no-restore"])])
    return result


def run_build(root: Path, command: list[str]) -> tuple[int, bytes, bytes]:
    try:
        result = subprocess.run(command, cwd=root, capture_output=True, check=False, timeout=600)
        return result.returncode, result.stdout, result.stderr
    except FileNotFoundError:
        return 127, b"", b"dotnet executable unavailable\n"
    except subprocess.TimeoutExpired as error:
        return 124, error.stdout or b"", (error.stderr or b"") + b"\nbuild measurement timed out\n"


def measure_builds(source: Path, authority_root: Path, directory: str) -> dict[str, Any]:
    _, scope = authority(authority_root)
    snapshot = validate_source(source, scope)
    commands = build_commands()
    fallback = ("module-internal-fallback", commands[-1][1] + ["-m:1", "-p:NuGetAudit=false", "-p:MinVerVersionOverride=1.0.0"])
    all_commands = commands + [fallback]
    names = [BUILD_NAME] + [label + suffix for label, _ in all_commands for suffix in (".stdout.log", ".stderr.log")]
    targets = output_targets(authority_root, source, directory, names)
    require(all(not target.exists() for target in targets), "SUCCESSOR_OUTPUT_COLLISION", "Build evidence already exists; preserve it")
    observations, logs = [], []
    for label, command in all_commands:
        exit_code, stdout, stderr = run_build(source, command)
        logs.extend([stdout, stderr])
        observations.append({"id": label, "command": command, "exitCode": exit_code,
                             "state": "available" if exit_code == 0 else "blocked",
                             "stdout": {"path": directory + "/" + label + ".stdout.log", "sha256": sha(stdout)},
                             "stderr": {"path": directory + "/" + label + ".stderr.log", "sha256": sha(stderr)}})
    require(validate_source(source, scope) == snapshot, "SUCCESSOR_SOURCE_DIRTY", "Build changed source inputs")
    receipt = {"schemaVersion": BUILD_VERSION, "candidate": PINS["candidate"],
               "sourceSnapshotSha256": sha(canonical(snapshot)), "acceptanceClaimed": False,
               "observations": observations}
    write_outputs(targets, [json_bytes(receipt), *logs], immutable=True)
    return receipt


def build_evidence(authority_root: Path, directory: str, snapshot: dict[str, Any]) -> dict[str, Any]:
    path = directory + "/" + BUILD_NAME
    receipt = parse(read(authority_root, path))
    require(set(receipt) == {"schemaVersion", "candidate", "sourceSnapshotSha256", "acceptanceClaimed", "observations"}
            and receipt["schemaVersion"] == BUILD_VERSION and receipt["candidate"] == PINS["candidate"]
            and receipt["sourceSnapshotSha256"] == sha(canonical(snapshot)) and receipt["acceptanceClaimed"] is False,
            "SUCCESSOR_BUILD_EVIDENCE_INVALID", "Build receipt does not bind the approved source or claims acceptance")
    commands = build_commands()
    commands.append(("module-internal-fallback", commands[-1][1] + ["-m:1", "-p:NuGetAudit=false", "-p:MinVerVersionOverride=1.0.0"]))
    require(isinstance(receipt["observations"], list) and len(receipt["observations"]) == len(commands),
            "SUCCESSOR_BUILD_EVIDENCE_INVALID", "Build observations are missing or duplicated")
    for row, (label, command) in zip(receipt["observations"], commands, strict=True):
        require(set(row) == {"id", "command", "exitCode", "state", "stdout", "stderr"}
                and row["id"] == label and row["command"] == command and type(row["exitCode"]) is int
                and row["state"] == ("available" if row["exitCode"] == 0 else "blocked"),
                "SUCCESSOR_BUILD_EVIDENCE_INVALID", "Build status or exact command is inconsistent")
        for stream in ("stdout", "stderr"):
            binding = row[stream]
            require(set(binding) == {"path", "sha256"} and binding["path"] == directory + "/" + label + "." + stream + ".log"
                    and sha(read(authority_root, binding["path"])) == binding["sha256"],
                    "SUCCESSOR_BUILD_EVIDENCE_INVALID", "Exact build log bytes are missing or altered")
    return {"receipt": {"path": path, "sha256": sha(read(authority_root, path))}, **receipt}


def prepare(source: Path, authority_root: Path, directory: str) -> dict[str, Any]:
    authorization, scope = authority(authority_root)
    targets = output_targets(authority_root, source, directory, [JSON_NAME, MARKDOWN_NAME])
    snapshot = validate_source(source, scope)
    builds = build_evidence(authority_root, directory, snapshot)
    tiering = verifier()
    try:
        derived = tiering.derive_migration(source, current_tree=True)
        tiering.approved_migration(source, derived, current_tree=True)
    except tiering.VerificationError as error:
        raise PreparationError(error.code, str(error)) from error
    retained = tiering.document(source, tiering.MIGRATION)
    proposal = derived["proposal"]
    require(proposal["executionPolicy"]["frozenExecutedCaseFloor"] == 415
            and len(proposal["liveControls"]) == 3, "SUCCESSOR_INVENTORY_INVALID", "Frozen case floor or control inventory changed")
    required_rows = [[row["id"], row["rowSha256"]] for row in proposal["changedAssertions"]]
    old_rows = [[row["id"], row["rowSha256"]] for row in retained["proposal"]["changedAssertions"]]
    material = {"approvedScope": {"authorization": {"path": AUTHORIZATION_PATH, "sha256": PINS["authorizationSha256"]},
                                  "proposal": {"path": SCOPE_PATH, "sha256": PINS["scopeSha256"]},
                                  "proposalSha256": PINS["scopeMaterialSha256"], "decision": authorization},
                "originalEvidence": {"baseline": PINS["baseline"], "acceptedCandidate": PINS["originalCandidate"],
                                     "publication": PINS["originalPublication"],
                                     "completedOriginalScopeSuccessor": PINS["completedOriginalScopeSuccessor"],
                                     "protected": scope["preservedProtectedEvidence"]},
                "currentSourceSnapshot": snapshot, "proposedMigration": proposal,
                "proposedMigrationSha256": derived["proposalSha256"],
                "comparisonToOriginal": {"originalMigrationProposalSha256": retained["proposalSha256"],
                    "migrationProposalChanged": retained["proposalSha256"] != derived["proposalSha256"],
                    "changedRowsChanged": required_rows != old_rows,
                    "originalChangedRows": old_rows, "proposedChangedRows": required_rows,
                    "publicDriftChanged": proposal["publicSurface"]["driftSha256"] != retained["proposal"]["publicSurface"]["driftSha256"],
                    "originalPublicDriftSha256": retained["proposal"]["publicSurface"]["driftSha256"],
                    "proposedPublicDriftSha256": proposal["publicSurface"]["driftSha256"]}}
    material_digest = sha(canonical(material))
    result = {"schemaVersion": VERSION, "storyId": "9.2", "status": "quality-review-pending",
              "preparationOnly": True, "qualityApprovalClaimed": False, "acceptanceClaimed": False,
              "successorMaterial": material, "proposalSha256": material_digest,
              "requiredQualityBinding": {"role": "Quality owner", "proposalSha256": material_digest,
                                         "migrationProposalSha256": derived["proposalSha256"],
                                         "changedAssertionRows": required_rows,
                                         "publicDriftSha256": proposal["publicSurface"]["driftSha256"],
                                         "sourceSnapshotSha256": sha(canonical(snapshot)),
                                         "scopeProposalSha256": PINS["scopeMaterialSha256"]},
              "buildEvidence": builds,
              "acceptance": {"state": "blocked", "executionClaimed": False,
                             "blockers": ["SUCCESSOR_QUALITY_DECISION_REQUIRED", "FRESH_COMPLETE_ACCEPTANCE_REQUIRED"]
                             + (["INTERNAL_TIER_BUILD_FAILED"] if any(row["state"] == "blocked" and row["id"].startswith("module-internal") for row in builds["observations"]) else []),
                             "requiredFrozenCases": 415, "requiredLiveControls": 3}}
    validate_document(result)
    require(validate_source(source, scope) == snapshot, "SUCCESSOR_SOURCE_DIRTY", "Preparation changed source inputs")
    contents = [json_bytes(result), markdown(result, authority_root)]
    require(contents == [json_bytes(result), markdown(result, authority_root)], "SUCCESSOR_RENDER_DRIFT", "Rendering is not deterministic")
    write_outputs(targets, contents)
    return result


def validate_document(document: dict[str, Any]) -> None:
    try:
        schema = parse(read(SCRIPT_ROOT, SCHEMA_PATH))
        jsonschema.Draft202012Validator(schema).validate(document)
    except jsonschema.ValidationError as error:
        raise PreparationError("SUCCESSOR_SCHEMA_INVALID", "Preparation document violates the closed schema: " + error.json_path) from error
    require(document["proposalSha256"] == sha(canonical(document["successorMaterial"])),
            "SUCCESSOR_SCHEMA_INVALID", "Successor material digest differs")
    material = document["successorMaterial"]
    migration = material["proposedMigration"]
    snapshot = material["currentSourceSnapshot"]
    scope = material["approvedScope"]
    require(material["proposedMigrationSha256"] == sha(canonical(migration))
            and snapshot["sourceFilesSha256"] == sha(canonical(snapshot["sourceFiles"]))
            and snapshot["historySha256"] == sha(canonical(snapshot["history"]))
            and snapshot["candidate"] == PINS["candidate"]
            and scope["proposalSha256"] == PINS["scopeMaterialSha256"],
            "SUCCESSOR_BINDING_INVALID", "Migration, source, history, or approved scope digest differs")
    expected_quality = {"role": "Quality owner", "proposalSha256": document["proposalSha256"],
                        "migrationProposalSha256": material["proposedMigrationSha256"],
                        "changedAssertionRows": [[row["id"], row["rowSha256"]] for row in migration["changedAssertions"]],
                        "publicDriftSha256": migration["publicSurface"]["driftSha256"],
                        "sourceSnapshotSha256": sha(canonical(snapshot)), "scopeProposalSha256": scope["proposalSha256"]}
    require(document["requiredQualityBinding"] == expected_quality
            and migration["publicSurface"]["driftSha256"] == sha(canonical(migration["publicSurface"]["drift"])),
            "SUCCESSOR_BINDING_INVALID", "Later Quality binding does not describe the exact proposed material")
    for row in migration["changedAssertions"]:
        row_material = {key: value for key, value in row.items() if key not in ("reason", "rowSha256")}
        require(row["rowSha256"] == sha(canonical(row_material)), "SUCCESSOR_BINDING_INVALID", "Changed row digest differs")
    builds = document["buildEvidence"]
    require(builds["candidate"] == snapshot["candidate"]
            and builds["sourceSnapshotSha256"] == sha(canonical(snapshot))
            and all(row["state"] == ("available" if row["exitCode"] == 0 else "blocked") for row in builds["observations"]),
            "SUCCESSOR_BUILD_EVIDENCE_INVALID", "Build observations do not bind source and measured exit states")


def compiler_diagnostics(document: dict[str, Any], authority_root: Path) -> list[str]:
    summaries = []
    for observation in document["buildEvidence"]["observations"]:
        if observation["state"] != "blocked":
            continue
        diagnostics = set()
        for stream in ("stdout", "stderr"):
            binding = observation[stream]
            content = read(authority_root, binding["path"])
            require(sha(content) == binding["sha256"], "SUCCESSOR_BUILD_EVIDENCE_INVALID", "Build log changed during rendering")
            diagnostics.update(line.strip() for line in content.decode("utf-8", errors="replace").splitlines()
                               if re.search(r"\berror CS\d+:", line))
        if not diagnostics:
            continue
        codes = sorted({match for line in diagnostics for match in re.findall(r"\berror (CS\d+):", line)})
        types = sorted({match for line in diagnostics for match in re.findall(
            r"The type or namespace name '([^']+)' could not be found", line)})
        summary = f"{observation['id']}: {len(diagnostics)} distinct compiler diagnostics ({', '.join(codes)})."
        if types:
            summary += " Missing types: " + ", ".join(f"`{name}`" for name in types) + "."
        summaries.append(summary)
    return summaries


def markdown(document: dict[str, Any], authority_root: Path) -> bytes:
    material = document["successorMaterial"]
    comparison = material["comparisonToOriginal"]
    lines = ["# Story 9.2 current-main successor Quality review", "",
             "Preparation evidence only. A genuine Quality decision and fresh complete acceptance remain required.", "",
             f"Approved source: `{material['currentSourceSnapshot']['candidate']}`.",
             f"Approved scope material: `{material['approvedScope']['proposalSha256']}`.",
             f"Successor material to approve: `{document['proposalSha256']}`.",
             f"Proposed migration material: `{material['proposedMigrationSha256']}`.", "",
             "The historical generator, original baseline, frozen intent, original migration/Quality decision, accepted pairs,",
             "and completed original-scope successor are preserved. Excluded shared-tree edits are outside this proposal.", "",
             f"Measured scope: {material['currentSourceSnapshot']['counts']['committedChangedPaths']} changed paths, "
             f"{material['currentSourceSnapshot']['counts']['productionPaths']} production paths, "
             f"{material['currentSourceSnapshot']['counts']['changedRootGitlinks']} changed root gitlinks, "
             f"{material['currentSourceSnapshot']['counts']['owningPromotionCommits']} owning promotions.",
             "The full committed history and actual production source snapshot are bound separately from retained assertion semantics.",
             "The proposed migration's productionSurface is the retained historical freeze; currentSourceSnapshot.actualProductionSurface",
             "describes the approved current source. Source digests do not establish exported API additions.", "",
             f"Migration proposal changed: `{str(comparison['migrationProposalChanged']).lower()}`; "
             f"changed-row digests changed: `{str(comparison['changedRowsChanged']).lower()}`; "
             f"public-drift digest changed: `{str(comparison['publicDriftChanged']).lower()}`.", "",
             "Every retained row, before-strength binding, exact successor strength, tier, FR-20 member, and historical exclusion is retained.",
             "The frozen floor remains 415 cases across 401 active methods, with 452 frozen definitions and 51 exclusions.",
             "The execution policy requires the seven historical controls to remain compiled; the three live controls count separately from the floor.", "",
             "## Exact later Quality binding", "", "```json", json.dumps(document["requiredQualityBinding"], indent=2), "```", "",
             "The previous Quality approval applies to the original v3 material. Scope authorization supplies no new Quality decision.",
             "Review the actual source/environment snapshot, every changed row, and public drift before recording a genuine decision.", "",
             "## Measured build evidence", "", "| Observation | Exit | State | Exact command |", "| --- | --- | --- | --- |"]
    for row in document["buildEvidence"]["observations"]:
        lines.append(f"| {row['id']} | {row['exitCode']} | {row['state']} | `{' '.join(row['command'])}` |")
    diagnostics = compiler_diagnostics(document, authority_root)
    if diagnostics:
        lines += ["", "## Compiler blockers", "", *diagnostics]
    lines += ["", "Exact stdout/stderr paths and SHA-256 digests are in the pending JSON's buildEvidence.",
              "Failed builds remain blockers. No stale binary, synthetic decision, or invented tier execution supplies acceptance.",
              "Acceptance requires a compatible committed successor, fresh candidate-bound AC01–10, both complete passing tiers,",
              "all 415 frozen cases and three live controls, all exact/restored faults, deterministic final-pair generation, and insertion verification.", "",
              "## Preserved original evidence", "", "| Path | SHA-256 |", "| --- | --- |"]
    lines += [f"| `{row['path']}` | `{row['sha256']}` |" for row in material["originalEvidence"]["protected"]]
    original = material["originalEvidence"]
    lines += ["", f"Original accepted candidate `{original['acceptedCandidate']}`, publication `{original['publication']}`.",
              f"Completed original-scope successor: `{original['completedOriginalScopeSuccessor']['candidate']}`; "
              f"publication `{original['completedOriginalScopeSuccessor']['publication']}`; "
              f"lifecycle `{original['completedOriginalScopeSuccessor']['lifecycle']}`.",
              "Main's spec and sprint lifecycle remain in-progress.", ""]
    return "\n".join(lines).encode("utf-8")


class PreparationArgumentParser(argparse.ArgumentParser):
    """Report unsupported fact/approval options through the stable failure envelope."""

    def error(self, message: str) -> None:
        raise PreparationError("SUCCESSOR_ARGUMENT_INVALID", message)


def main(argv: list[str] | None = None) -> int:
    parser = PreparationArgumentParser(description=__doc__, allow_abbrev=False)
    parser.add_argument("--repository", required=True, help="Isolated clean checkout of the approved source commit")
    parser.add_argument("--authorization-root", required=True, help="Root containing the pinned genuine scope documents and outputs")
    parser.add_argument("--output-directory", required=True, help="Contained artifacts/v9/9.2/<name>-vN directory")
    parser.add_argument("--measure-builds", action="store_true", help="Measure and retain exact build receipts once")
    try:
        arguments = parser.parse_args(argv)
        source, authority_root = Path(arguments.repository).absolute(), Path(arguments.authorization_root).absolute()
        require(source != authority_root, "SUCCESSOR_SOURCE_INVALID", "Use a separate isolated source checkout")
        operation = measure_builds if arguments.measure_builds else prepare
        result = operation(source, authority_root, arguments.output_directory)
        print(json.dumps({"status": "builds-measured" if arguments.measure_builds else "prepared",
                          "acceptanceClaimed": False, "outputDirectory": arguments.output_directory,
                          "documentSha256": sha(json_bytes(result))}, indent=2))
        return 0
    except PreparationError as error:
        print(json.dumps({"status": "rejected", "acceptanceClaimed": False,
                          "blockers": [{"code": error.code, "message": str(error)}]}, indent=2))
        return 1
    except (OSError, KeyError, TypeError, ValueError) as error:
        print(json.dumps({"status": "rejected", "acceptanceClaimed": False,
                          "blockers": [{"code": "SUCCESSOR_INPUT_INVALID", "message": type(error).__name__}]}, indent=2))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
