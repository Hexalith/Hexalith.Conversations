#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# ///
"""Verify that scoped root submodule promotions are clean and committed."""

import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path, PurePosixPath
from typing import Any, NoReturn, Sequence


SCHEMA = "submodule-promotion-gate/v1"
GIT_TIMEOUT_SECONDS = 20

# Ambient Git configuration that would otherwise silently change the verdict.
# diff.ignoreSubmodules=all makes every gitlink change invisible to
# changed_gitlinks(); diff.renames=false decomposes a submodule rename into
# delete+add and produces a false PATH_NOT_ROOT_DECLARED on the vanished path.
GIT_CONFIG_OVERRIDES = (
    "core.quotepath=false",
    "diff.ignoreSubmodules=none",
    "diff.renames=true",
)

# Git environment variables that redirect a `git -C <submodule>` invocation back
# at the umbrella repository. Inheriting these from a hook or `rebase --exec`
# makes every submodule report the umbrella's HEAD, index and worktree.
GIT_ENVIRONMENT_OVERRIDES = (
    "GIT_DIR",
    "GIT_WORK_TREE",
    "GIT_INDEX_FILE",
    "GIT_OBJECT_DIRECTORY",
    "GIT_COMMON_DIR",
    "GIT_ALTERNATE_OBJECT_DIRECTORIES",
    "GIT_NAMESPACE",
)
BLOCKER_REMEDIATION = {
    "PATH_NOT_ROOT_DECLARED": "Declare only paths listed by the root .gitmodules file.",
    "SUBMODULE_NOT_INITIALIZED": "Initialize this root-declared submodule without using recursive submodule commands.",
    "SUBMODULE_DIRTY_TRACKED": "Commit or otherwise resolve the tracked submodule changes before completion.",
    "SUBMODULE_DIRTY_UNTRACKED": "Commit or remove the untracked submodule files before completion.",
    "SUBMODULE_HEAD_UNRESOLVED": "Check out a valid committed submodule revision before completion.",
    "REMOTE_COMMIT_UNAVAILABLE": "Push the submodule commit and refresh the local remote-tracking ref outside this checker.",
    "GITLINK_MISSING_IN_CANDIDATE": "Commit the root gitlink for this submodule in the candidate umbrella revision.",
    "GITLINK_MODE_NOT_160000": "Record this path as a Git submodule entry with mode 160000.",
    "GITLINK_COMMIT_MISMATCH": "Commit the root gitlink that exactly matches the checked-out submodule HEAD.",
    "UNCAPTURED_SUBMODULE_PROMOTION": (
        "Declare this path in submodule_promotions and commit the root gitlink, "
        "or restore the submodule checkout to the recorded commit."
    ),
}


class GateArgumentParser(argparse.ArgumentParser):
    """Report argument errors through the checker's stable error document."""

    def error(self, message: str) -> NoReturn:
        raise GateError("INVALID_SCOPE", message)


class GateError(Exception):
    """An invocation or repository error that prevents a trustworthy decision."""

    def __init__(self, code: str, message: str, path: str | None = None) -> None:
        super().__init__(message)
        self.code = code
        self.message = message
        self.path = path


def decode(value: bytes) -> str:
    return value.decode("utf-8", errors="surrogateescape")


def default_repository() -> Path:
    return Path(__file__).resolve().parents[2]


def diagnostic(
    code: str,
    message: str,
    path: str | None = None,
    remediation: str | None = None,
) -> dict[str, Any]:
    item: dict[str, Any] = {"code": code, "path": path, "message": message}
    if remediation is not None:
        item["remediation"] = remediation
    return item


def empty_document(repository: Path | None = None) -> dict[str, Any]:
    return {
        "schema": SCHEMA,
        "result": "error",
        "repository": str(repository.resolve()) if repository is not None else None,
        "baseline": None,
        "candidate": None,
        "declared": [],
        "changed_gitlinks": [],
        "evaluated": [],
        "blockers": [],
        "warnings": [],
    }


def git_environment() -> dict[str, str]:
    environment = dict(os.environ)
    for name in GIT_ENVIRONMENT_OVERRIDES:
        environment.pop(name, None)
    return environment


def run_git(
    repository: Path,
    *arguments: str,
    allowed_returncodes: tuple[int, ...] = (0,),
) -> subprocess.CompletedProcess[bytes]:
    command: list[str] = ["git"]
    for override in GIT_CONFIG_OVERRIDES:
        command.extend(("-c", override))
    command.extend(("-C", str(repository), *arguments))
    try:
        result = subprocess.run(
            command,
            check=False,
            capture_output=True,
            timeout=GIT_TIMEOUT_SECONDS,
            env=git_environment(),
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise GateError("GIT_COMMAND_FAILED", f"git command failed: {error}") from error

    if result.returncode not in allowed_returncodes:
        command = " ".join(arguments)
        stderr = decode(result.stderr).strip() or "no stderr"
        raise GateError(
            "GIT_COMMAND_FAILED",
            f"git {command} exited {result.returncode}: {stderr}",
        )
    return result


def validate_repository(repository: Path) -> Path:
    if shutil.which("git") is None:
        raise GateError("GIT_UNAVAILABLE", "git is not available on PATH")
    if not repository.is_dir():
        raise GateError("NOT_A_GIT_REPOSITORY", f"repository directory does not exist: {repository}")

    try:
        result = run_git(
            repository,
            "rev-parse",
            "--show-toplevel",
            allowed_returncodes=(0, 128),
        )
    except GateError as error:
        raise GateError("NOT_A_GIT_REPOSITORY", str(error)) from error
    if result.returncode != 0:
        # Exit 128 also covers safe.directory dubious-ownership refusal, which is
        # common in CI containers and WSL mounts; git's own stderr carries the
        # only actionable instruction, so never discard it.
        detail = decode(result.stderr).strip()
        suffix = f": {detail}" if detail else ""
        raise GateError("NOT_A_GIT_REPOSITORY", f"not a Git repository: {repository}{suffix}")

    root = Path(decode(result.stdout).strip()).resolve()
    if root != repository.resolve():
        raise GateError(
            "NOT_A_GIT_REPOSITORY",
            f"--repository must name the repository root: {root}",
        )
    return root


def safe_relative_path(value: str) -> str:
    path = PurePosixPath(value)
    # PurePosixPath(".").parts is (), so the per-part guard below never sees a
    # bare "." — it has to be rejected explicitly or the umbrella root itself
    # becomes scopeable as a submodule.
    if (
        not value
        or value in (".", "..")
        or "\\" in value
        or path.is_absolute()
        or path.as_posix() != value
        or any(part in ("", ".", "..") for part in path.parts)
        or any(ord(character) < 0x20 for character in value)
    ):
        raise GateError(
            "INVALID_SCOPE",
            f"submodule scope must be a normalized repository-relative path: {value!r}",
            value or None,
        )
    return value


def promotion_path(value: str) -> str:
    """Return a normalized path in the root references/ submodule namespace."""
    path = safe_relative_path(value)
    if not path.startswith("references/"):
        raise GateError(
            "INVALID_SCOPE",
            f"root submodule scope must be below references/: {value!r}",
            value,
        )
    return path


def validate_scope(declared: Sequence[str], require_remote: Sequence[str]) -> tuple[list[str], set[str]]:
    normalized_declared = [promotion_path(path) for path in declared]
    normalized_remote = [promotion_path(path) for path in require_remote]
    if len(set(normalized_declared)) != len(normalized_declared):
        raise GateError("INVALID_SCOPE", "each --submodule path must be declared exactly once")
    if len(set(normalized_remote)) != len(normalized_remote):
        raise GateError("INVALID_SCOPE", "each --require-remote path must be declared exactly once")
    remote_set = set(normalized_remote)
    missing = sorted(remote_set.difference(normalized_declared))
    if missing:
        raise GateError(
            "INVALID_SCOPE",
            f"--require-remote paths must also be declared by --submodule: {', '.join(missing)}",
            missing[0],
        )
    return normalized_declared, remote_set


def root_submodule_paths(repository: Path, candidate: str) -> list[str]:
    candidate_gitmodules = f"{candidate}:.gitmodules"
    exists = run_git(
        repository,
        "cat-file",
        "-e",
        candidate_gitmodules,
        allowed_returncodes=(0, 128),
    )
    if exists.returncode != 0:
        raise GateError(
            "MISSING_GITMODULES",
            f"candidate commit {candidate} has no root .gitmodules blob",
        )

    result = run_git(
        repository,
        "config",
        "--null",
        "--blob",
        candidate_gitmodules,
        "--get-regexp",
        r"^submodule\..*\.path$",
        allowed_returncodes=(0, 1),
    )
    if result.returncode == 1:
        return []

    paths: list[str] = []
    for entry in result.stdout.split(b"\0"):
        if not entry:
            continue
        text = decode(entry)
        if "\n" not in text:
            raise GateError(
                "INVALID_SCOPE",
                f"root .gitmodules declares {text!r} with no path value",
            )
        _, value = text.split("\n", 1)
        paths.append(promotion_path(value))
    if len(paths) != len(set(paths)):
        raise GateError("INVALID_SCOPE", "root .gitmodules declares a submodule path more than once")
    # A root declaration nested under another root declaration would make the
    # checker descend into a nested submodule, which AC 4a forbids outright.
    for path in paths:
        for other in paths:
            if path != other and path.startswith(f"{other}/"):
                raise GateError(
                    "INVALID_SCOPE",
                    f"root .gitmodules declares {path} inside {other}; "
                    "nested submodule paths must never be root-declared",
                    path,
                )
    return paths


def resolve_commit(repository: Path, revision: str, code: str) -> str:
    result = run_git(
        repository,
        "rev-parse",
        "--verify",
        f"{revision}^{{commit}}",
        allowed_returncodes=(0, 128),
    )
    if result.returncode != 0:
        raise GateError(code, f"revision does not resolve to a commit: {revision}")
    return decode(result.stdout).strip()


def is_ancestor(repository: Path, ancestor: str, descendant: str) -> bool:
    result = run_git(
        repository,
        "merge-base",
        "--is-ancestor",
        ancestor,
        descendant,
        allowed_returncodes=(0, 1),
    )
    return result.returncode == 0


def changed_gitlinks(repository: Path, baseline: str, candidate: str) -> list[str]:
    result = run_git(
        repository,
        "diff",
        "--raw",
        "--no-abbrev",
        "-z",
        baseline,
        candidate,
        "--",
    )
    tokens = result.stdout.split(b"\0")
    changed: list[str] = []
    index = 0
    while index < len(tokens):
        header_bytes = tokens[index]
        index += 1
        if not header_bytes:
            continue
        header = decode(header_bytes)
        if not header.startswith(":"):
            raise GateError("GIT_COMMAND_FAILED", "could not parse git diff --raw output")
        fields = header[1:].split()
        if len(fields) < 5 or index >= len(tokens):
            raise GateError("GIT_COMMAND_FAILED", "incomplete git diff --raw record")
        source_mode, destination_mode, _, _, status = fields[:5]
        # Only gitlink records are scope inputs. Validating every changed path
        # here would let an ordinary file whose name is merely unusual (a
        # backslash, a control character) abort the entire run with exit 2 --
        # blocking on state that AC 3d requires to be ignored.
        raw_source_path = decode(tokens[index])
        index += 1
        raw_destination_path = raw_source_path
        is_rename_or_copy = status[:1] in ("R", "C")
        if is_rename_or_copy:
            if index >= len(tokens):
                raise GateError("GIT_COMMAND_FAILED", "incomplete renamed git diff record")
            raw_destination_path = decode(tokens[index])
            index += 1
        if source_mode != "160000" and destination_mode != "160000":
            continue
        source_path = safe_relative_path(raw_source_path)
        destination_path = (
            safe_relative_path(raw_destination_path)
            if raw_destination_path != raw_source_path
            else source_path
        )
        paths: list[str] = []
        if is_rename_or_copy:
            # The source path was renamed away, not left behind as a gitlink to
            # evaluate; only the destination path reflects real candidate state.
            if destination_mode == "160000":
                paths.append(destination_path)
        else:
            if source_mode == "160000":
                paths.append(source_path)
            if destination_mode == "160000":
                paths.append(destination_path)
        for path in paths:
            if path not in changed:
                changed.append(path)
    return changed


def own_worktree(repository: Path, path: Path) -> bool:
    repository_root = repository.resolve()
    try:
        relative = path.relative_to(repository)
    except ValueError:
        return False

    # Reject the declared path and every parent component when any is a
    # symlink. Merely comparing resolved Git toplevels accepts a symlink to an
    # arbitrary external checkout as though it were the umbrella submodule.
    current = repository
    for part in relative.parts:
        current = current / part
        if current.is_symlink():
            return False
    try:
        resolved_path = path.resolve()
        resolved_path.relative_to(repository_root)
    except (OSError, ValueError):
        return False

    if not path.is_dir() or not (path / ".git").exists():
        return False
    result = run_git(path, "rev-parse", "--show-toplevel", allowed_returncodes=(0, 128))
    if result.returncode != 0:
        return False
    return Path(decode(result.stdout).strip()).resolve() == resolved_path


def submodule_head(path: Path) -> str | None:
    result = run_git(
        path,
        "rev-parse",
        "--verify",
        "HEAD^{commit}",
        allowed_returncodes=(0, 128),
    )
    if result.returncode != 0:
        return None
    return decode(result.stdout).strip()


def submodule_dirt(path: Path, head: str | None) -> tuple[bool, bool]:
    status = run_git(
        path,
        "status",
        "--porcelain=v1",
        "-z",
        "--untracked-files=all",
        "--ignore-submodules=all",
    )
    tracked = False
    untracked = False
    for entry in status.stdout.split(b"\0"):
        if not entry:
            continue
        if entry.startswith(b"??"):
            untracked = True
        else:
            tracked = True

    if head is not None:
        staged = run_git(
            path,
            "diff-index",
            "--cached",
            "--name-status",
            "-z",
            head,
            "--",
        )
        tracked = tracked or bool(staged.stdout)
    return tracked, untracked


def recorded_gitlink(repository: Path, candidate: str, path: str) -> tuple[str | None, str | None]:
    result = run_git(repository, "ls-tree", "-z", candidate, "--", path)
    records = [record for record in result.stdout.split(b"\0") if record]
    if not records:
        return None, None
    for record in records:
        metadata, separator, encoded_path = record.partition(b"\t")
        if not separator or decode(encoded_path) != path:
            continue
        fields = decode(metadata).split()
        if len(fields) != 3:
            raise GateError("GIT_COMMAND_FAILED", f"could not parse candidate tree entry for {path}", path)
        mode, _, object_id = fields
        return mode, object_id
    return None, None


def remote_contains(path: Path, head: str) -> bool:
    result = run_git(
        path,
        "for-each-ref",
        "--contains",
        head,
        "--format=%(refname)",
        "refs/remotes/",
    )
    return bool(result.stdout.strip())


def evaluate_path(
    repository: Path,
    candidate: str,
    path: str,
    root_paths: set[str],
    require_remote: bool,
    blockers: list[dict[str, Any]],
) -> dict[str, Any]:
    item: dict[str, Any] = {
        "path": path,
        "recorded_gitlink": None,
        "recorded_mode": None,
        "head": None,
        "initialized": False,
        "clean": False,
        "remote_available": None,
    }
    if path not in root_paths:
        blockers.append(
            diagnostic(
                "PATH_NOT_ROOT_DECLARED",
                f"{path} is not declared by the root .gitmodules file",
                path,
                BLOCKER_REMEDIATION["PATH_NOT_ROOT_DECLARED"],
            )
        )
        return item

    worktree = repository / path
    initialized = own_worktree(repository, worktree)
    item["initialized"] = initialized
    if not initialized:
        blockers.append(
            diagnostic(
                "SUBMODULE_NOT_INITIALIZED",
                f"{path} is not an initialized submodule worktree",
                path,
                BLOCKER_REMEDIATION["SUBMODULE_NOT_INITIALIZED"],
            )
        )
    else:
        head = submodule_head(worktree)
        item["head"] = head
        if head is None:
            blockers.append(
                diagnostic(
                    "SUBMODULE_HEAD_UNRESOLVED",
                    f"{path} HEAD does not resolve to a commit",
                    path,
                    BLOCKER_REMEDIATION["SUBMODULE_HEAD_UNRESOLVED"],
                )
            )
        tracked, untracked = submodule_dirt(worktree, head)
        item["clean"] = not tracked and not untracked
        if tracked:
            blockers.append(
                diagnostic(
                    "SUBMODULE_DIRTY_TRACKED",
                    f"{path} contains staged or unstaged tracked changes",
                    path,
                    BLOCKER_REMEDIATION["SUBMODULE_DIRTY_TRACKED"],
                )
            )
        if untracked:
            blockers.append(
                diagnostic(
                    "SUBMODULE_DIRTY_UNTRACKED",
                    f"{path} contains untracked files",
                    path,
                    BLOCKER_REMEDIATION["SUBMODULE_DIRTY_UNTRACKED"],
                )
            )
        if require_remote and head is not None:
            available = remote_contains(worktree, head)
            item["remote_available"] = available
            if not available:
                blockers.append(
                    diagnostic(
                        "REMOTE_COMMIT_UNAVAILABLE",
                        f"{path} HEAD is contained by no locally known remote-tracking ref",
                        path,
                        BLOCKER_REMEDIATION["REMOTE_COMMIT_UNAVAILABLE"],
                    )
                )

    mode, object_id = recorded_gitlink(repository, candidate, path)
    item["recorded_mode"] = mode
    item["recorded_gitlink"] = object_id
    if mode is None:
        blockers.append(
            diagnostic(
                "GITLINK_MISSING_IN_CANDIDATE",
                f"candidate commit records no entry for {path}",
                path,
                BLOCKER_REMEDIATION["GITLINK_MISSING_IN_CANDIDATE"],
            )
        )
    elif mode != "160000":
        blockers.append(
            diagnostic(
                "GITLINK_MODE_NOT_160000",
                f"candidate entry for {path} has mode {mode}, not 160000",
                path,
                BLOCKER_REMEDIATION["GITLINK_MODE_NOT_160000"],
            )
        )
    elif item["head"] is not None and object_id != item["head"]:
        blockers.append(
            diagnostic(
                "GITLINK_COMMIT_MISMATCH",
                f"candidate gitlink {object_id} does not match {path} HEAD {item['head']}",
                path,
                BLOCKER_REMEDIATION["GITLINK_COMMIT_MISMATCH"],
            )
        )
    return item


def checkout_is_ahead(worktree: Path, recorded: str, head: str) -> bool:
    """True when the submodule checkout is a strict descendant of the recorded gitlink.

    That combination is an uncaptured promotion: real submodule commits exist that
    the umbrella never recorded. A checkout that is merely behind or diverged is
    ordinary concurrent state and stays non-blocking under AC 3d.
    """
    if recorded == head:
        return False
    result = run_git(
        worktree,
        "merge-base",
        "--is-ancestor",
        recorded,
        head,
        allowed_returncodes=(0, 1, 128),
    )
    # Exit 128 means the recorded commit is not in this submodule's object
    # database, so the relationship is unknowable without fetching. Never fetch;
    # fall back to the non-blocking warning.
    return result.returncode == 0


def inspect_unrelated(
    repository: Path,
    candidate: str,
    path: str,
    warnings: list[dict[str, Any]],
    blockers: list[dict[str, Any]],
) -> None:
    worktree = repository / path
    if not own_worktree(repository, worktree):
        warnings.append(
            diagnostic(
                "UNRELATED_SUBMODULE_UNINSPECTABLE",
                f"unrelated root submodule {path} is not an inspectable initialized worktree",
                path,
            )
        )
        return
    head = submodule_head(worktree)
    tracked, untracked = submodule_dirt(worktree, head)
    if tracked or untracked:
        kinds = "tracked and untracked" if tracked and untracked else "tracked" if tracked else "untracked"
        warnings.append(
            diagnostic(
                "UNRELATED_SUBMODULE_DIRTY",
                f"unrelated root submodule {path} contains {kinds} changes",
                path,
            )
        )
    mode, object_id = recorded_gitlink(repository, candidate, path)
    if head is not None and (mode != "160000" or object_id != head):
        if (
            mode == "160000"
            and object_id is not None
            and checkout_is_ahead(worktree, object_id, head)
        ):
            blockers.append(
                diagnostic(
                    "UNCAPTURED_SUBMODULE_PROMOTION",
                    f"{path} checkout {head} is ahead of the candidate gitlink {object_id}; "
                    "submodule commits exist that the umbrella never captured",
                    path,
                    BLOCKER_REMEDIATION["UNCAPTURED_SUBMODULE_PROMOTION"],
                )
            )
        else:
            warnings.append(
                diagnostic(
                    "UNRELATED_GITLINK_DRIFT",
                    f"unrelated root submodule {path} HEAD differs from the candidate gitlink",
                    path,
                )
            )


def verify(args: argparse.Namespace) -> dict[str, Any]:
    # Path("") is Path("."), so an unset shell variable would silently gate
    # whatever directory the process happened to start in.
    if not args.repository.strip():
        raise GateError("INVALID_SCOPE", "--repository must not be empty")
    repository = validate_repository(Path(args.repository).expanduser().resolve())
    candidate = resolve_commit(repository, args.candidate, "CANDIDATE_UNRESOLVABLE")
    declared_paths, remote_paths = validate_scope(args.submodule, args.require_remote)
    root_paths = root_submodule_paths(repository, candidate)
    baseline = None
    warnings: list[dict[str, Any]] = []
    changed: list[str] = []
    if args.baseline is None:
        warnings.append(
            diagnostic(
                "BASELINE_NOT_PROVIDED",
                "no baseline was provided; changed-gitlink detection was skipped",
            )
        )
    else:
        baseline = resolve_commit(repository, args.baseline, "BASELINE_UNRESOLVABLE")
        if not is_ancestor(repository, baseline, candidate):
            raise GateError(
                "BASELINE_NOT_ANCESTOR",
                f"baseline {baseline} is not an ancestor of candidate {candidate}; "
                "changed-gitlink detection would be unreliable",
            )
        changed = changed_gitlinks(repository, baseline, candidate)

    affected = list(declared_paths)
    for path in changed:
        if path not in declared_paths:
            warnings.append(
                diagnostic(
                    "UNDECLARED_GITLINK_CHANGE",
                    f"gitlink changed between baseline and candidate without a declaration: {path}",
                    path,
                )
            )
        if path not in affected:
            affected.append(path)

    if not affected and (baseline is None or baseline == candidate):
        # Nothing was declared and no usable baseline was supplied, so no path
        # was evaluated at all. Without this signal an exit-0 "pass" is
        # indistinguishable from a fully verified promotion, and the callers'
        # "missing baseline is a blocker when activated" rule can never fire --
        # the missing baseline is exactly what keeps changed_gitlinks empty.
        warnings.append(
            diagnostic(
                "SCOPE_NOT_EVALUATED",
                "no declared scope and no usable baseline, so no submodule was evaluated; "
                "this run proves nothing about promotion completeness",
            )
        )

    blockers: list[dict[str, Any]] = []
    evaluated = [
        evaluate_path(
            repository,
            candidate,
            path,
            set(root_paths),
            path in remote_paths,
            blockers,
        )
        for path in affected
    ]
    for path in root_paths:
        if path not in affected:
            try:
                inspect_unrelated(repository, candidate, path, warnings, blockers)
            except GateError as error:
                warnings.append(
                    diagnostic(
                        "UNRELATED_SUBMODULE_INSPECTION_FAILED",
                        f"could not inspect unrelated root submodule {path}: {error.message}",
                        path,
                    )
                )

    return {
        "schema": SCHEMA,
        "result": "blocked" if blockers else "pass",
        "repository": str(repository),
        "baseline": baseline,
        "candidate": candidate,
        "declared": [
            {"path": path, "require_remote": path in remote_paths}
            for path in declared_paths
        ],
        "changed_gitlinks": changed,
        "evaluated": evaluated,
        "blockers": blockers,
        "warnings": warnings,
    }


def write_output(document: dict[str, Any], output_format: str) -> None:
    # decode() preserves undecodable bytes as surrogates, so a strict stdout
    # would raise UnicodeEncodeError here -- outside main()'s handlers, turning
    # a deliberate exit 2 into exit 1 with an empty, unparseable stdout.
    if getattr(sys.stdout, "errors", None) not in ("surrogateescape", "backslashreplace"):
        try:
            sys.stdout.reconfigure(errors="backslashreplace")
        except (AttributeError, ValueError):  # pragma: no cover - exotic stdout
            pass

    if output_format == "json":
        sys.stdout.write(json.dumps(document, indent=2, ensure_ascii=False) + "\n")
        return

    sys.stdout.write(f"Submodule promotion gate: {document['result'].upper()}\n")
    if document.get("repository"):
        sys.stdout.write(f"Repository: {document['repository']}\n")
    # State the scope explicitly: a pass that evaluated nothing must never look
    # the same as a pass that verified every declared promotion.
    declared_paths = [item["path"] for item in document["declared"]]
    sys.stdout.write(
        "Declared: {} | Changed gitlinks: {} | Evaluated: {}\n".format(
            ", ".join(declared_paths) or "none",
            ", ".join(document["changed_gitlinks"]) or "none",
            ", ".join(item["path"] for item in document["evaluated"]) or "none",
        )
    )
    for item in document["blockers"]:
        location = f" ({item['path']})" if item.get("path") else ""
        sys.stdout.write(f"BLOCKER [{item['code']}]{location}: {item['message']}\n")
        if item.get("remediation"):
            sys.stdout.write(f"  Remediation: {item['remediation']}\n")
    for item in document["warnings"]:
        location = f" ({item['path']})" if item.get("path") else ""
        sys.stdout.write(f"WARNING [{item['code']}]{location}: {item['message']}\n")


def build_parser() -> argparse.ArgumentParser:
    parser = GateArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=str(default_repository()))
    parser.add_argument("--baseline")
    parser.add_argument("--candidate", default="HEAD")
    parser.add_argument("--submodule", action="append", default=[])
    parser.add_argument("--require-remote", action="append", default=[])
    parser.add_argument("--format", choices=("text", "json"), default="text")
    return parser


def is_format_option(token: str) -> bool:
    """argparse accepts any unambiguous prefix, so --forma and --f mean --format too."""
    return len(token) > 2 and token.startswith("--") and "--format".startswith(token)


def pre_parse_output_format(raw_arguments: Sequence[str]) -> str:
    """Best-effort output format, used only if a GateError occurs before argparse succeeds."""
    for index, token in enumerate(raw_arguments):
        option, separator, inline_value = token.partition("=")
        if not is_format_option(option):
            continue
        if separator:
            return "json" if inline_value == "json" else "text"
        following = raw_arguments[index + 1] if index + 1 < len(raw_arguments) else None
        return "json" if following == "json" else "text"
    return "text"


def main(arguments: Sequence[str] | None = None) -> int:
    raw_arguments = list(arguments) if arguments is not None else sys.argv[1:]
    output_format = pre_parse_output_format(raw_arguments)
    repository: Path | None = None
    try:
        args = build_parser().parse_args(raw_arguments)
        output_format = args.format
        repository = Path(args.repository).expanduser()
        document = verify(args)
    except GateError as error:
        document = empty_document(repository)
        document["blockers"].append(diagnostic(error.code, error.message, error.path))
        write_output(document, output_format)
        return 2
    except Exception as error:  # noqa: BLE001 - always emit a parseable error document
        document = empty_document(repository)
        document["blockers"].append(
            diagnostic("INTERNAL_ERROR", f"unexpected internal error: {error}")
        )
        write_output(document, output_format)
        return 2

    write_output(document, output_format)
    return 1 if document["blockers"] else 0


if __name__ == "__main__":
    sys.exit(main())
