#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# ///
"""Generate a story final record from measured repository state."""

import argparse
import base64
import hashlib
import json
import os
import re
import shlex
import shutil
import subprocess
import sys
import unicodedata
import xml.etree.ElementTree as ElementTree
from importlib import util as importlib_util
from pathlib import Path, PurePosixPath
from typing import Any, NoReturn, Sequence


SCHEMA = "story-final-record-v1"
BUNDLE_SCHEMA = "story-final-record-bundle-v1"
GIT_TIMEOUT_SECONDS = 20
PROMOTION_CHECKER = "verify_submodule_promotion.py"

# The rendered block is delimited so a second run replaces its own previous
# output instead of appending a second, contradicting record. `### File List`
# and `## Verification` are the story and spec anchors used before a record has
# ever been generated.
RECORD_BEGIN_MARKER = "<!-- STORY-FINAL-RECORD:BEGIN -->"
RECORD_END_MARKER = "<!-- STORY-FINAL-RECORD:END -->"
STORY_ANCHOR = "### File List"
STORY_ANCHOR_END = "### Boundary Confirmation"
SPEC_ANCHOR = "## Verification"

# Ambient Git configuration that would otherwise silently change the verdict,
# carried unchanged from the promotion checker so both halves of the completion
# gate observe the same repository.
GIT_CONFIG_OVERRIDES = (
    "core.quotepath=false",
    "diff.ignoreSubmodules=none",
    "diff.renames=true",
)

# Git environment variables that redirect a `git -C <path>` invocation back at
# some other repository. Inheriting these from a hook or `rebase --exec` makes
# every measurement describe the wrong tree.
GIT_ENVIRONMENT_OVERRIDES = (
    "GIT_DIR",
    "GIT_WORK_TREE",
    "GIT_INDEX_FILE",
    "GIT_OBJECT_DIRECTORY",
    "GIT_COMMON_DIR",
    "GIT_ALTERNATE_OBJECT_DIRECTORIES",
    "GIT_NAMESPACE",
)

# Source: sprint-change-proposal-2026-07-28.md:423-427. The frozen Epic 6
# overlay names blocking conditions only and enumerates no code strings, so
# these are attributed to the proposal rather than to the overlay.
BLOCKER_REMEDIATION = {
    "TEST_RESULTS_MISSING": (
        "Run the declared test project and pass its machine-readable result artifact; "
        "never carry a count forward from an earlier pass."
    ),
    "TEST_RESULTS_STALE": (
        "Re-run the declared test project after the last file change, so every count "
        "describes the tree the record binds to."
    ),
    "TEST_COUNT_INCONSISTENT": (
        "Re-run the test project and pass the artifact it emitted; the artifact's own "
        "summary disagrees with the results it contains."
    ),
    "TEST_PROJECT_SCOPE_MISMATCH": (
        "Declare exactly one artifact for every root-owned test project in the root solution; "
        "do not omit, duplicate, relabel, or import a foreign project."
    ),
    "TEST_RESULTS_EMPTY": (
        "Run the declared test project without an empty filter; a zero-test artifact measures "
        "no executable behavior."
    ),
    "TEST_RESULTS_FAILED": (
        "Fix every failing test and emit a new artifact; failed tests cannot complete a story."
    ),
    "TEST_SKIP_NOT_ALLOWED": (
        "Either run the skipped test or declare its exact identity and reason in the story's "
        "versioned allowed_skipped_tests policy."
    ),
    "TEST_BUILD_NOT_BOUND": (
        "Clean-rebuild every root-owned test project from the committed candidate, run the "
        "tests against those binaries, and retain the candidate-bound binary manifest emitted "
        "by this generator."
    ),
    "FILE_LIST_DRIFT": (
        "Replace the record's File List with the derived list emitted by this generator; "
        "never hand-edit either side into agreement."
    ),
    "SUBMODULE_INTERNAL_PATH": (
        "Remove the submodule-internal path from this record; it belongs to that "
        "repository's own record, and the gitlink belongs in the promotions section."
    ),
    "CANDIDATE_NOT_FINAL": (
        "Re-run against the committed head, or restore the gitlink that moved after the "
        "candidate, so the record binds to the revision that is actually final."
    ),
    "PROMOTION_GATE_NOT_PASS": (
        "Remediate the embedded promotion checker's own blockers without initializing, "
        "updating, fetching, or silently expanding submodule scope."
    ),
    "BASELINE_NOT_TRUSTWORTHY": (
        "Record a resolvable `baseline_commit` that is an ancestor of the candidate; "
        "a missing or `NO_VCS` baseline cannot bound a derived file list."
    ),
    "RECORD_NOT_DERIVED": (
        "Supply the inputs the record is derived from: a readable story record with a "
        "replaceable section, a resolvable candidate, and at least one parsed test-result artifact."
    ),
    "RECORD_CONTENT_DRIFT": (
        "Insert the Markdown from the passing bundle verbatim, then verify its bundle digest "
        "before changing completion state."
    ),
    "WORKTREE_NOT_CLEAN": (
        "Commit or remove every source-tree change outside the story, sprint-status file, and "
        "declared TRX artifacts before deriving the final record."
    ),
}

# TRX outcome vocabulary, mapped onto the five record fields. `notExecuted` is
# the attribute that carries skipped tests; there is no `skipped` attribute.
TRX_PASSED_OUTCOMES = ("Passed",)
TRX_FAILED_OUTCOMES = ("Failed", "Error", "Timeout", "Aborted")
TRX_SKIPPED_OUTCOMES = (
    "NotExecuted",
    "NotRunnable",
    "Inconclusive",
    "Disconnected",
    "Warning",
    "Pending",
    "InProgress",
)


class GateArgumentParser(argparse.ArgumentParser):
    """Report argument errors through the generator's stable error document."""

    def error(self, message: str) -> NoReturn:
        raise GateError("INVALID_SCOPE", message)


class GateError(Exception):
    """An invocation or repository error that prevents a trustworthy record."""

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


def blocker(code: str, message: str, path: str | None = None) -> dict[str, Any]:
    return diagnostic(code, message, path, BLOCKER_REMEDIATION[code])


def empty_document(repository: Path | None = None) -> dict[str, Any]:
    """Pre-seed every top-level key so consumers never KeyError on a total failure."""
    return {
        "schema": SCHEMA,
        "result": "error",
        "mode": "live",
        "repository": str(repository.resolve()) if repository is not None else None,
        "story": None,
        "baseline": None,
        "candidate": None,
        "derived": {
            "test_results": False,
            "candidate": False,
            "record_section": False,
        },
        "record": {"anchor": None, "declared_file_list": [], "generated_block": False},
        "test_results": {
            "required_projects": {},
            "allowed_skipped_tests": {},
            "projects": [],
            "totals": None,
        },
        "build_manifest": {"candidate": None, "projects": []},
        "file_list": {
            "derived": [],
            "declared": [],
            "missing": [],
            "unexpected": [],
            "entries": [],
        },
        "newest_derived_input": None,
        "promotions": [],
        "candidate_binding": None,
        "promotion_gate": None,
        "classification": None,
        "boundary": None,
        "blockers": [],
        "warnings": [],
    }


def git_environment() -> dict[str, str]:
    environment = dict(os.environ)
    for name in GIT_ENVIRONMENT_OVERRIDES:
        environment.pop(name, None)
    for name in list(environment):
        if name in {
            "GIT_CONFIG_COUNT",
            "GIT_CONFIG_GLOBAL",
            "GIT_CONFIG_SYSTEM",
            "GIT_CONFIG_NOSYSTEM",
        } or name.startswith(("GIT_CONFIG_KEY_", "GIT_CONFIG_VALUE_")):
            environment.pop(name, None)
    environment["GIT_CONFIG_GLOBAL"] = os.devnull
    environment["GIT_CONFIG_NOSYSTEM"] = "1"
    environment["GIT_OPTIONAL_LOCKS"] = "0"
    return environment


# Historical verification reuses legacy readers that turn a Git failure into a
# finding. Each active observer list records every Git execution failure raised
# below, so that caller can still report the failure as BLOCKED.
GIT_FAILURE_OBSERVERS: list[list[GateError]] = []


def git_failure(message: str) -> GateError:
    error = GateError("GIT_COMMAND_FAILED", message)
    for observer in GIT_FAILURE_OBSERVERS:
        observer.append(error)
    return error


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
        # stdout and stderr are read concurrently; reading them sequentially from
        # live pipes deadlocks on any command with substantial output.
        result = subprocess.run(
            command,
            check=False,
            capture_output=True,
            timeout=GIT_TIMEOUT_SECONDS,
            env=git_environment(),
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise git_failure(f"git command failed: {error}") from error

    if result.returncode not in allowed_returncodes:
        rendered = " ".join(arguments)
        stderr = decode(result.stderr).strip() or "no stderr"
        raise git_failure(f"git {rendered} exited {result.returncode}: {stderr}")
    return result


def validate_repository(repository: Path) -> Path:
    if shutil.which("git") is None:
        raise GateError("GIT_UNAVAILABLE", "git is not available on PATH")
    if not repository.is_dir():
        raise GateError(
            "NOT_A_GIT_REPOSITORY", f"repository directory does not exist: {repository}"
        )

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
        detail = decode(result.stderr).strip()
        suffix = f": {detail}" if detail else ""
        raise GateError(
            "NOT_A_GIT_REPOSITORY", f"not a Git repository: {repository}{suffix}"
        )

    root = Path(decode(result.stdout).strip()).resolve()
    if root != repository.resolve():
        raise GateError(
            "NOT_A_GIT_REPOSITORY",
            f"--repository must name the repository root: {root}",
        )
    return root


def safe_relative_path(value: str) -> str:
    """Reject anything that is not a normalized, repository-relative POSIX path."""
    path = PurePosixPath(value)
    # PurePosixPath(".").parts is (), so the per-part guard below never sees a
    # bare "." — it has to be rejected explicitly.
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
            f"path must be a normalized repository-relative path: {value!r}",
            value or None,
        )
    return value


def promotion_path(value: str) -> str:
    path = safe_relative_path(value)
    if not path.startswith("references/"):
        raise GateError(
            "INVALID_SCOPE",
            f"root submodule scope must be below references/: {value!r}",
            value,
        )
    return path


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def contained_file(repository: Path, relative: str, description: str) -> Path:
    """Resolve one repository-relative file without allowing a symlink escape."""
    lexical = repository / relative
    try:
        resolved = lexical.resolve(strict=True)
        resolved.relative_to(repository)
    except (OSError, ValueError) as error:
        raise GateError(
            "INVALID_SCOPE",
            f"{description} must resolve to a file inside the repository: {relative}",
            relative,
        ) from error
    if not resolved.is_file():
        raise GateError(
            "INVALID_SCOPE", f"{description} is not a file: {relative}", relative
        )
    return resolved


def read_file_snapshot(path: Path) -> tuple[bytes, int]:
    """Read bytes, digest input, and nanosecond mtime from one stable open file."""
    with path.open("rb") as handle:
        before = os.fstat(handle.fileno())
        content = handle.read()
        after = os.fstat(handle.fileno())

    def identity(value: os.stat_result) -> tuple[int, int, int, int]:
        return value.st_dev, value.st_ino, value.st_size, value.st_mtime_ns

    if identity(before) != identity(after):
        raise OSError(f"file changed while it was being read: {path}")
    current = path.stat()
    if (current.st_dev, current.st_ino) != (after.st_dev, after.st_ino):
        raise OSError(f"file was replaced while it was being read: {path}")
    return content, after.st_mtime_ns


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


def try_resolve_commit(repository: Path, revision: str) -> str | None:
    try:
        return resolve_commit(repository, revision, "CANDIDATE_UNRESOLVABLE")
    except GateError:
        return None


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


def root_submodule_paths(repository: Path, candidate: str) -> list[str]:
    """Root-declared submodule paths, read from the candidate's own .gitmodules blob."""
    candidate_gitmodules = f"{candidate}:.gitmodules"
    exists = run_git(
        repository,
        "cat-file",
        "-e",
        candidate_gitmodules,
        allowed_returncodes=(0, 128),
    )
    if exists.returncode != 0:
        if tree_entry(repository, candidate, ".gitmodules")[0] is not None:
            raise GateError("GIT_COMMAND_FAILED", "Git could not probe the committed .gitmodules path")
        return []

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
        path = promotion_path(value)
        if path in paths:
            raise GateError("INVALID_SCOPE", f"root .gitmodules declares duplicate path {path}")
        paths.append(path)
    return sorted(paths)


def raw_diff_records(
    repository: Path, *revisions: str
) -> list[tuple[str, str, str, str, str, str]]:
    """Parse `git diff --raw -z` into (src_mode, dst_mode, src_sha, dst_sha, status, path).

    The mode is read from its own column. `160000` can legitimately appear inside
    a blob hash or a filename, so a substring test over the raw record is wrong.
    """
    result = run_git(
        repository,
        "diff",
        "--ignore-submodules=none",
        "--raw",
        "--no-abbrev",
        "-z",
        *revisions,
        "--",
    )
    tokens = result.stdout.split(b"\0")
    records: list[tuple[str, str, str, str, str, str]] = []
    index = 0
    while index < len(tokens):
        header_bytes = tokens[index]
        index += 1
        if not header_bytes:
            continue
        header = decode(header_bytes)
        if not header.startswith(":"):
            raise GateError(
                "GIT_COMMAND_FAILED", "could not parse git diff --raw output"
            )
        fields = header[1:].split()
        if len(fields) < 5 or index >= len(tokens):
            raise GateError("GIT_COMMAND_FAILED", "incomplete git diff --raw record")
        source_mode, destination_mode, source_sha, destination_sha, status = fields[:5]
        raw_path = decode(tokens[index])
        index += 1
        # A second path token appears only for a rename or a copy, and only the
        # destination reflects real candidate state.
        if status[:1] in ("R", "C"):
            if index >= len(tokens):
                raise GateError(
                    "GIT_COMMAND_FAILED", "incomplete renamed git diff record"
                )
            raw_path = decode(tokens[index])
            index += 1
        records.append(
            (
                source_mode,
                destination_mode,
                source_sha,
                destination_sha,
                status,
                raw_path,
            )
        )
    return records


def changed_gitlinks(repository: Path, baseline: str, candidate: str) -> list[str]:
    changed: list[str] = []
    for source_mode, destination_mode, _, _, _, raw_path in raw_diff_records(
        repository, baseline, candidate
    ):
        if source_mode != "160000" and destination_mode != "160000":
            continue
        path = safe_relative_path(raw_path)
        if path not in changed:
            changed.append(path)
    return sorted(changed)


def tree_entry(
    repository: Path, revision: str, path: str
) -> tuple[str | None, str | None]:
    """Return (mode, object id) for one path, read from the tree entry's own columns.

    `git ls-tree <rev> -- <missing>` exits 0 with empty output, so absence is
    detected by empty output and never by exit status.
    """
    result = run_git(repository, "ls-tree", "-z", revision, "--", path)
    for record in result.stdout.split(b"\0"):
        if not record:
            continue
        metadata, separator, encoded_path = record.partition(b"\t")
        if not separator or decode(encoded_path) != path:
            continue
        fields = decode(metadata).split()
        if len(fields) != 3:
            raise GateError(
                "GIT_COMMAND_FAILED", f"could not parse tree entry for {path}", path
            )
        mode, _, object_id = fields
        return mode, object_id
    return None, None


def committed_path_status(
    repository: Path, baseline: str, candidate: str
) -> dict[str, str]:
    """path -> status for the committed range, with renames decomposed to delete+add."""
    result = run_git(
        repository,
        "diff",
        "--name-status",
        "--no-renames",
        "-z",
        baseline,
        candidate,
        "--",
    )
    tokens = [token for token in result.stdout.split(b"\0") if token]
    observed: dict[str, str] = {}
    index = 0
    while index + 1 < len(tokens):
        status = decode(tokens[index])[:1]
        path = decode(tokens[index + 1])
        index += 2
        observed[path] = status
    return observed


def worktree_path_status(repository: Path) -> dict[str, str]:
    """Tracked working-tree delta plus untracked non-ignored files.

    Detection is a two-command split on purpose: `status` with
    `--ignore-submodules=all` never traverses a submodule, and `diff-index
    --cached` recovers the staged changes `status` reports only in its own
    format.
    """
    observed: dict[str, str] = {}
    status = run_git(
        repository,
        "status",
        "--porcelain=v1",
        "-z",
        "--untracked-files=all",
        "--ignore-submodules=all",
    )
    tokens = status.stdout.split(b"\0")
    index = 0
    while index < len(tokens):
        entry = tokens[index]
        index += 1
        if not entry:
            continue
        text = decode(entry)
        if len(text) < 4:
            continue
        codes, path = text[:2], text[3:]
        if "R" in codes or "C" in codes:
            # Rename records carry the original path as the following NUL field.
            index += 1
        if codes == "??":
            observed.setdefault(path, "?")
            continue
        letters = [code for code in codes if code not in (" ", "?")]
        observed.setdefault(path, letters[0] if letters else "M")

    staged = run_git(
        repository,
        "diff-index",
        "--cached",
        "--name-status",
        "--no-renames",
        "-z",
        "HEAD",
        "--",
        allowed_returncodes=(0, 128),
    )
    if staged.returncode == 0:
        tokens = [token for token in staged.stdout.split(b"\0") if token]
        index = 0
        while index + 1 < len(tokens):
            observed.setdefault(decode(tokens[index + 1]), decode(tokens[index])[:1])
            index += 2

    untracked = run_git(repository, "ls-files", "--others", "--exclude-standard", "-z")
    for token in untracked.stdout.split(b"\0"):
        if token:
            observed.setdefault(decode(token), "?")
    return observed


def parse_frontmatter(content: str) -> str:
    if not content.startswith("---\n"):
        return ""
    end = content.find("\n---", 4)
    return content[4:end] if end >= 0 else ""


def parse_yaml_scalar(value: str) -> str:
    value = value.strip()
    if value.startswith("'") and value.endswith("'") and len(value) >= 2:
        return value[1:-1].replace("''", "'")
    if value.startswith('"') and value.endswith('"') and len(value) >= 2:
        parsed = json.loads(value)
        return str(parsed)
    return value.split(" #", 1)[0].strip()


def frontmatter_scalar(frontmatter: str, key: str) -> str | None:
    match = re.search(rf"^{re.escape(key)}:\s*(.+)$", frontmatter, re.MULTILINE)
    if match is None:
        return None
    return parse_yaml_scalar(match.group(1))


def frontmatter_allowed_skips(frontmatter: str) -> dict[str, str]:
    """Parse the narrow versioned skip-policy shape used by story/spec records."""
    lines = frontmatter.splitlines()
    start = next(
        (
            index
            for index, line in enumerate(lines)
            if re.fullmatch(r"allowed_skipped_tests:\s*", line)
        ),
        None,
    )
    if start is None:
        return {}
    entries: dict[str, str] = {}
    current_test: str | None = None
    current_reason: str | None = None

    def finish() -> None:
        nonlocal current_test, current_reason
        if current_test is None:
            return
        if not current_reason:
            raise GateError(
                "INVALID_SCOPE",
                f"allowed skipped test {current_test!r} has no non-empty reason",
            )
        if current_test in entries:
            raise GateError(
                "INVALID_SCOPE", f"allowed skipped test is duplicated: {current_test}"
            )
        entries[current_test] = current_reason
        current_test = None
        current_reason = None

    for line in lines[start + 1 :]:
        if line and not line.startswith((" ", "\t")):
            break
        match_test = re.fullmatch(r"\s+-\s+test:\s*(.+)", line)
        if match_test:
            finish()
            current_test = parse_yaml_scalar(match_test.group(1))
            continue
        match_reason = re.fullmatch(r"\s+reason:\s*(.+)", line)
        if match_reason and current_test is not None:
            current_reason = parse_yaml_scalar(match_reason.group(1))
            continue
        if line.strip():
            raise GateError(
                "INVALID_SCOPE", f"malformed allowed_skipped_tests entry: {line!r}"
            )
    finish()
    return entries


def root_test_projects(
    repository: Path, submodule_paths: Sequence[str]
) -> dict[str, str]:
    """Return project-name -> path for root-owned tests declared by the root solution."""
    solutions = sorted(repository.glob("*.slnx"))
    if len(solutions) != 1:
        raise GateError(
            "INVALID_SCOPE",
            f"expected exactly one root .slnx test-scope authority, found {len(solutions)}",
        )
    solution_relative = solutions[0].relative_to(repository).as_posix()
    solution = contained_file(repository, solution_relative, "root solution")
    try:
        root = ElementTree.fromstring(solution.read_bytes())
    except (ElementTree.ParseError, OSError) as error:
        raise GateError(
            "INVALID_SCOPE", f"root solution is unreadable: {error}"
        ) from error

    prefixes = tuple(f"{path}/" for path in submodule_paths)
    projects: dict[str, str] = {}
    for element in root.findall(".//{*}Project"):
        raw_path = element.get("Path")
        if not raw_path:
            continue
        path = safe_relative_path(raw_path)
        if (
            path.startswith(prefixes)
            or not path.startswith("tests/")
            or not path.endswith(".csproj")
        ):
            continue
        contained_file(repository, path, "root test project")
        name = PurePosixPath(path).stem
        if name in projects:
            raise GateError(
                "INVALID_SCOPE", f"root solution repeats test project name: {name}"
            )
        projects[name] = path
    if not projects:
        raise GateError(
            "INVALID_SCOPE", "root solution declares no root-owned test projects"
        )
    return dict(sorted(projects.items()))


def record_anchor(content: str) -> tuple[str | None, int, int]:
    """Locate the region the rendered block replaces.

    Returns (anchor name, start, end). The marker pair wins so a second run
    replaces its own output; otherwise the story or spec heading is used.
    """
    begin_count = content.count(RECORD_BEGIN_MARKER)
    end_count = content.count(RECORD_END_MARKER)
    if begin_count or end_count:
        if begin_count != 1 or end_count != 1:
            return None, -1, -1
        start = content.find(RECORD_BEGIN_MARKER)
        end = content.find(RECORD_END_MARKER)
        if end <= start:
            return None, -1, -1
        return "generated-block", start, end + len(RECORD_END_MARKER)
    start = content.find(f"{STORY_ANCHOR}\n")
    if start >= 0:
        end = content.find(f"{STORY_ANCHOR_END}\n", start)
        return "story-file-list", start, end if end >= 0 else len(content)
    start = content.find(f"{SPEC_ANCHOR}\n")
    if start >= 0:
        return "spec-verification", start, len(content)
    return None, -1, -1


def generated_block(content: str) -> str | None:
    anchor, start, end = record_anchor(content)
    if anchor != "generated-block":
        return None
    if end < len(content) and content[end] == "\n":
        end += 1
    return content[start:end]


def declared_file_list(content: str) -> tuple[list[str], int]:
    """Paths the record already claims, plus how many distinct lists carry them.

    Enter on the File List heading and exit on the next heading, so the
    promotions and test-result sections that follow -- which also carry
    backticked text -- can never be read as file paths. Both the generated
    bullet form and the fenced-block form older records use are recognised, so a
    pre-generator record can still be verified against measured state.
    """
    anchor, start, end = record_anchor(content)
    if anchor is None:
        return [], 0
    lines = content[start:end].splitlines()
    heading = next(
        (index for index, line in enumerate(lines) if line.strip() == STORY_ANCHOR),
        None,
    )
    if heading is None:
        return [], 0

    paths: list[str] = []
    lists = 0
    in_fence = False
    fence_had_paths = False
    bullets_seen = False
    for line in lines[heading + 1 :]:
        if line.startswith("```"):
            if in_fence:
                lists += 1 if fence_had_paths else 0
            in_fence = not in_fence
            fence_had_paths = False
            continue
        if in_fence:
            token = line.strip()
            # Fenced content is literal, so any whitespace-free token that looks
            # like a path is one.
            if (
                token
                and re.fullmatch(r"[^\s`]+", token)
                and ("/" in token or "." in token)
            ):
                paths.append(token)
                fence_had_paths = True
            continue
        if re.match(r"^#{1,3}\s+", line):
            break
        match = re.match(r"^-\s+(`+)( ?)(.*?)( ?)\1(?:\s|$)", line)
        if match:
            paths.append(match.group(3))
            bullets_seen = True
    if bullets_seen:
        lists += 1
    return sorted(set(paths)), lists


def count_file_list_headings(content: str) -> int:
    return len(re.findall(rf"^{re.escape(STORY_ANCHOR)}\s*$", content, re.MULTILINE))


def parse_trx(content: bytes) -> dict[str, Any]:
    """Parse one TRX artifact into reported and recomputed counts.

    TRX carries the namespace http://microsoft.com/schemas/VisualStudio/TeamTest/2010,
    so a literal /TestRun/... path matches nothing; every lookup uses {*}.
    """
    root = ElementTree.fromstring(content)
    counters = root.find("./{*}ResultSummary/{*}Counters")
    if counters is None:
        raise ValueError("TRX has no /TestRun/ResultSummary/Counters element")

    def counter(name: str, *, required: bool = True) -> int:
        raw = counters.get(name)
        if raw is None:
            if not required:
                return 0
            raise ValueError(f"TRX Counters element has no {name} attribute")
        return int(raw)

    failed = sum(
        counter(name, required=False)
        for name in ("failed", "error", "timeout", "aborted")
    )
    skipped = sum(
        counter(name, required=False)
        for name in (
            "notExecuted",
            "notRunnable",
            "inconclusive",
            "disconnected",
            "warning",
            "pending",
            "inProgress",
        )
    )
    reported = {
        "total": counter("total"),
        "executed": counter("executed"),
        "passed": counter("passed"),
        "failed": failed,
        "skipped": skipped,
    }

    # Only direct children of <Results>: a data-driven test nests its cases in
    # <InnerResults>, which the summary counts once.
    results = root.findall("./{*}Results/{*}UnitTestResult")
    result_items = [
        {"test": element.get("testName") or "", "outcome": element.get("outcome") or "",
         "output": "\n".join(node.text or "" for node in element.findall("./{*}Output/{*}StdOut")
                              + element.findall("./{*}Output/{*}TextMessages/{*}Message"))}
        for element in results
    ]
    outcomes = [item["outcome"] for item in result_items]
    recomputed = {
        "total": len(results),
        "passed": sum(1 for outcome in outcomes if outcome in TRX_PASSED_OUTCOMES),
        "failed": sum(1 for outcome in outcomes if outcome in TRX_FAILED_OUTCOMES),
        "skipped": sum(1 for outcome in outcomes if outcome in TRX_SKIPPED_OUTCOMES),
    }
    known_outcomes = set(
        TRX_PASSED_OUTCOMES + TRX_FAILED_OUTCOMES + TRX_SKIPPED_OUTCOMES
    )
    unknown_outcomes = sorted(set(outcomes) - known_outcomes)
    code_bases = sorted(
        {
            code_base
            for element in root.findall(
                "./{*}TestDefinitions/{*}UnitTest/{*}TestMethod"
            )
            if (code_base := element.get("codeBase"))
        }
    )
    assemblies = sorted(
        {PurePosixPath(re.split(r"[\\/]", code_base)[-1]).stem for code_base in code_bases}
    )
    return {
        "reported": reported,
        "recomputed": recomputed,
        "results": result_items,
        "unknown_outcomes": unknown_outcomes,
        "assemblies": assemblies,
        "code_bases": code_bases,
    }


def count_disagreements(parsed: dict[str, Any]) -> list[str]:
    reported = parsed["reported"]
    recomputed = parsed["recomputed"]
    disagreements = [
        f"{field}: summary {reported[field]} vs recorded results {recomputed[field]}"
        for field in ("total", "passed", "failed", "skipped")
        if reported[field] != recomputed[field]
    ]
    if parsed["unknown_outcomes"]:
        disagreements.append(
            "unknown UnitTestResult outcome(s): "
            + ", ".join(parsed["unknown_outcomes"])
        )
    if (
        reported["total"]
        != reported["passed"] + reported["failed"] + reported["skipped"]
    ):
        disagreements.append(
            "total {total} is not passed {passed} plus failed {failed} plus skipped {skipped}".format(
                **reported
            )
        )
    if reported["executed"] != reported["passed"] + reported["failed"]:
        disagreements.append(
            "executed {executed} is not passed {passed} plus failed {failed}".format(
                **reported
            )
        )
    return disagreements


def parse_test_declaration(value: str) -> tuple[str, str]:
    name, separator, artifact = value.partition("=")
    if not separator or not name.strip() or not artifact.strip():
        raise GateError(
            "INVALID_SCOPE",
            f"--test-results must be NAME=PATH: {value!r}",
        )
    return name.strip(), safe_relative_path(artifact.strip())


def load_promotion_checker() -> Any:
    script = Path(__file__).resolve().parent / PROMOTION_CHECKER
    if not script.is_file():
        raise GateError(
            "PROMOTION_CHECKER_UNAVAILABLE",
            f"the promotion completion checker is missing: {script}",
        )
    spec = importlib_util.spec_from_file_location("verify_submodule_promotion", script)
    if spec is None or spec.loader is None:
        raise GateError(
            "PROMOTION_CHECKER_UNAVAILABLE",
            f"the promotion completion checker could not be loaded: {script}",
        )
    module = importlib_util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def run_promotion_gate(
    repository: Path,
    baseline: str | None,
    candidate: str,
    declared: Sequence[str],
    require_remote: Sequence[str],
) -> dict[str, Any]:
    """Run the Story 6.7 checker in-process and return its document verbatim.

    Neither main() nor verify() calls sys.exit(), and both exit 1 and exit 2 emit
    a valid document, so callers must branch on the document's own `result`.
    """
    module = load_promotion_checker()
    arguments = [
        "--repository",
        str(repository),
        "--candidate",
        candidate,
        "--format",
        "json",
    ]
    if baseline is not None:
        arguments.extend(("--baseline", baseline))
    for path in declared:
        arguments.extend(("--submodule", path))
    for path in require_remote:
        arguments.extend(("--require-remote", path))
    try:
        return module.verify(module.build_parser().parse_args(arguments))
    except module.GateError as error:  # the checker's own error document shape
        document = module.empty_document(repository)
        document["blockers"].append(
            module.diagnostic(error.code, error.message, error.path)
        )
        return document


def derive_test_results(
    repository: Path,
    declarations: Sequence[tuple[str, str]],
    expected_projects: dict[str, str],
    allowed_skips: dict[str, str],
    blockers: list[dict[str, Any]],
    warnings: list[dict[str, Any]],
) -> dict[str, Any]:
    projects: list[dict[str, Any]] = []
    names = [name for name, _ in declarations]
    artifacts = [artifact for _, artifact in declarations]
    duplicate_names = sorted({name for name in names if names.count(name) > 1})
    duplicate_artifacts = sorted(
        {path for path in artifacts if artifacts.count(path) > 1}
    )
    declaration_by_name: dict[str, str] = {}
    for name, artifact in declarations:
        declaration_by_name.setdefault(name, artifact)
    missing = sorted(set(expected_projects) - set(declaration_by_name))
    unexpected = sorted(set(declaration_by_name) - set(expected_projects))
    if missing or unexpected or duplicate_names or duplicate_artifacts:
        details = []
        if missing:
            details.append("missing " + ", ".join(missing))
        if unexpected:
            details.append("unexpected " + ", ".join(unexpected))
        if duplicate_names:
            details.append("duplicate project name(s) " + ", ".join(duplicate_names))
        if duplicate_artifacts:
            details.append("reused artifact(s) " + ", ".join(duplicate_artifacts))
        blockers.append(
            blocker(
                "TEST_PROJECT_SCOPE_MISMATCH",
                "test-result declarations do not exactly match the root solution — "
                + "; ".join(details),
            )
        )

    observed_skips: set[str] = set()
    for name in expected_projects:
        artifact = declaration_by_name.get(name)
        item: dict[str, Any] = {
            "project": name,
            "artifact": artifact,
            "state": "NOT_RUN",
            "sha256": None,
            "modified": None,
            "counts": None,
        }
        if artifact is None:
            blockers.append(
                blocker(
                    "TEST_RESULTS_MISSING",
                    f"required test project {name} has no declared result artifact",
                    expected_projects[name],
                )
            )
            projects.append(item)
            continue
        lexical = repository / artifact
        if not lexical.is_file():
            blockers.append(
                blocker(
                    "TEST_RESULTS_MISSING",
                    f"declared test project {name} has no result artifact at {artifact}",
                    artifact,
                )
            )
            projects.append(item)
            continue
        try:
            absolute = contained_file(repository, artifact, "test-result artifact")
            content, modified_ns = read_file_snapshot(absolute)
            parsed = parse_trx(content)
        except (ElementTree.ParseError, ValueError, OSError) as error:
            # An artifact that yields no counters is not a measured result, so it
            # is reported exactly as an unrun project rather than carried forward.
            blockers.append(
                blocker(
                    "TEST_RESULTS_MISSING",
                    f"declared test project {name} has an unparseable result artifact "
                    f"at {artifact}: {error}",
                    artifact,
                )
            )
            projects.append(item)
            continue

        item["state"] = "PARSED"
        item["sha256"] = hashlib.sha256(content).hexdigest()
        item["modified"] = modified_ns
        item["counts"] = parsed["reported"]
        item["recomputed"] = parsed["recomputed"]
        item["failed_tests"] = sorted(
            result["test"]
            for result in parsed["results"]
            if result["outcome"] in TRX_FAILED_OUTCOMES
        )
        item["skipped_tests"] = sorted(
            result["test"]
            for result in parsed["results"]
            if result["outcome"] in TRX_SKIPPED_OUTCOMES
        )
        item["code_bases"] = parsed["code_bases"]
        observed_skips.update(item["skipped_tests"])
        disagreements = count_disagreements(parsed)
        if disagreements:
            blockers.append(
                blocker(
                    "TEST_COUNT_INCONSISTENT",
                    f"{name} result artifact disagrees with itself — "
                    + "; ".join(disagreements),
                    artifact,
                )
            )
        if parsed["reported"]["total"] == 0:
            blockers.append(
                blocker(
                    "TEST_RESULTS_EMPTY",
                    f"{name} result artifact contains zero tests",
                    artifact,
                )
            )
        if parsed["assemblies"] != [name]:
            blockers.append(
                blocker(
                    "TEST_PROJECT_SCOPE_MISMATCH",
                    f"{artifact} identifies test assembly {parsed['assemblies']!r}, expected {name!r}",
                    artifact,
                )
            )
        if item["failed_tests"]:
            blockers.append(
                blocker(
                    "TEST_RESULTS_FAILED",
                    f"{name} has {len(item['failed_tests'])} failed test(s): "
                    + ", ".join(item["failed_tests"][:5]),
                    artifact,
                )
            )
        unapproved_skips = sorted(set(item["skipped_tests"]) - set(allowed_skips))
        if unapproved_skips:
            blockers.append(
                blocker(
                    "TEST_SKIP_NOT_ALLOWED",
                    f"{name} has {len(unapproved_skips)} unapproved skipped test(s): "
                    + ", ".join(unapproved_skips[:5]),
                    artifact,
                )
            )
        projects.append(item)

    for test, reason in sorted(allowed_skips.items()):
        if test not in observed_skips:
            warnings.append(
                diagnostic(
                    "UNUSED_TEST_SKIP_ALLOWANCE",
                    f"versioned skip allowance was not exercised: {test} — {reason}",
                )
            )

    parsed_projects = [item for item in projects if item["state"] == "PARSED"]
    totals: dict[str, int] | None = None
    if parsed_projects:
        # Computed by summation. A caller-supplied total is never accepted.
        totals = {
            field: sum(int(item["counts"][field]) for item in parsed_projects)
            for field in ("total", "executed", "passed", "failed", "skipped")
        }
    return {
        "required_projects": expected_projects,
        "allowed_skipped_tests": allowed_skips,
        "projects": projects,
        "totals": totals,
    }


def dotnet_source_revisions(content: bytes) -> list[str]:
    """Return SourceRevisionId values embedded in managed informational versions."""
    return sorted(
        {
            match.decode("ascii")
            for match in re.findall(
                rb"[0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?\+([0-9a-f]{40})(?![0-9a-f])",
                content,
            )
        }
    )


def test_binary_path(repository: Path, code_base: str) -> tuple[str, Path]:
    """Resolve a TRX codeBase to one repository-contained test binary."""
    lexical = Path(code_base)
    absolute = lexical if lexical.is_absolute() else repository / lexical
    try:
        resolved = absolute.resolve(strict=True)
        relative = resolved.relative_to(repository).as_posix()
    except (OSError, ValueError) as error:
        raise ValueError(
            f"TRX codeBase must resolve to a test binary inside the repository: {code_base}"
        ) from error
    if not resolved.is_file():
        raise ValueError(f"TRX codeBase is not a file: {code_base}")
    return relative, resolved


def derive_test_build_manifest(
    repository: Path,
    candidate: str,
    test_results: dict[str, Any],
    blockers: list[dict[str, Any]],
) -> dict[str, Any]:
    """Bind every parsed TRX to a candidate-stamped test assembly and its full digest."""
    manifest: dict[str, Any] = {"candidate": candidate, "projects": []}
    seen_binaries: set[str] = set()
    for item in test_results["projects"]:
        if item["state"] != "PARSED":
            continue
        project = item["project"]
        code_bases = item.get("code_bases", [])
        if len(code_bases) != 1:
            blockers.append(
                blocker(
                    "TEST_BUILD_NOT_BOUND",
                    f"{project} identifies {len(code_bases)} distinct test binaries; exactly one is required",
                    item.get("artifact"),
                )
            )
            continue
        try:
            relative, binary = test_binary_path(repository, code_bases[0])
            content, modified_ns = read_file_snapshot(binary)
        except (OSError, ValueError) as error:
            blockers.append(
                blocker(
                    "TEST_BUILD_NOT_BOUND",
                    f"{project} has no verifiable repository-contained test binary: {error}",
                    item.get("artifact"),
                )
            )
            continue
        if relative in seen_binaries:
            blockers.append(
                blocker(
                    "TEST_BUILD_NOT_BOUND",
                    f"test binary is reused by more than one declared project: {relative}",
                    relative,
                )
            )
        seen_binaries.add(relative)

        revisions = dotnet_source_revisions(content)
        if revisions != [candidate]:
            observed = ", ".join(revisions) if revisions else "none"
            blockers.append(
                blocker(
                    "TEST_BUILD_NOT_BOUND",
                    f"{project} test binary records SourceRevisionId {observed}; expected candidate {candidate}",
                    relative,
                )
            )
        artifact_modified = item.get("modified")
        if artifact_modified is not None and modified_ns > artifact_modified:
            blockers.append(
                blocker(
                    "TEST_BUILD_NOT_BOUND",
                    f"{project} test result predates the binary it claims to execute",
                    relative,
                )
            )
        manifest["projects"].append(
            {
                "project": project,
                "binary": relative,
                "source_revision": candidate if revisions == [candidate] else None,
                "sha256": hashlib.sha256(content).hexdigest(),
                "modified": modified_ns,
            }
        )
    return manifest


def evaluate_staleness(
    repository: Path,
    test_results: dict[str, Any],
    derived_paths: Sequence[str],
    output_targets: set[str],
    blockers: list[dict[str, Any]],
) -> dict[str, Any] | None:
    """Block when an artifact predates the newest file the record binds to.

    Two exclusions, both narrow. The generator's own write targets (D3): AC5
    requires writing the record into a file that is itself in the derived list,
    so an unmodified rule would report every correct re-run as stale. And the
    declared artifacts themselves: a suite takes time to run, so the project
    finishing first is always older than the project finishing last, and
    comparing artifacts against each other measures nothing. Every other derived
    path is still compared, so a genuinely stale artifact still blocks.
    """
    excluded = set(output_targets) | {
        item["artifact"] for item in test_results["projects"] if item["artifact"]
    }
    newest_path: str | None = None
    newest_mtime: int | None = None
    for path in derived_paths:
        if path in excluded:
            continue
        absolute = repository / path
        if not absolute.is_file():
            continue
        modified = absolute.stat().st_mtime_ns
        if newest_mtime is None or modified > newest_mtime:
            newest_mtime, newest_path = modified, path
    if newest_mtime is None:
        return None

    for item in test_results["projects"]:
        if item["state"] != "PARSED" or item["modified"] is None:
            continue
        if item["modified"] < newest_mtime:
            blockers.append(
                blocker(
                    "TEST_RESULTS_STALE",
                    f"{item['project']} result artifact predates {newest_path}; "
                    "its counts describe an earlier tree",
                    item["artifact"],
                )
            )
    return {"path": newest_path, "modified": newest_mtime}


STATUS_ANNOTATION = {
    "A": "new",
    "M": "modified",
    "D": "deleted",
    "T": "type changed",
    "?": "new",
}


def derive_file_list(
    repository: Path,
    baseline: str | None,
    candidate: str,
    root_paths: Sequence[str],
    gitlink_paths: Sequence[str],
    output_targets: set[str],
    evidence_paths: set[str],
    blockers: list[dict[str, Any]],
    warnings: list[dict[str, Any]],
) -> list[dict[str, str]]:
    committed: dict[str, str] = (
        committed_path_status(repository, baseline, candidate)
        if baseline is not None
        else {}
    )
    observed: dict[str, str] = dict(committed)
    allowed_dirt = set(output_targets) | set(evidence_paths)
    for path in sorted(worktree_path_status(repository)):
        if path in allowed_dirt:
            continue
        blockers.append(
            blocker(
                "WORKTREE_NOT_CLEAN",
                f"{path} is dirty outside the explicit record outputs and test artifacts",
                path,
            )
        )

    # Self-accounting: the generator writes into the tree it measures, so its own
    # output targets belong in the list even before the write happens.
    for path in output_targets:
        observed.setdefault(path, "M")

    submodule_prefixes = tuple(f"{path}/" for path in root_paths)
    root_set = set(root_paths) | set(gitlink_paths)
    entries: list[dict[str, str]] = []
    for path in sorted(observed):
        if path in root_set:
            # A gitlink is promotion state, never a file-list entry.
            continue
        if path.startswith(submodule_prefixes):
            blockers.append(
                blocker(
                    "SUBMODULE_INTERNAL_PATH",
                    f"{path} is inside a root-declared submodule; it belongs to that "
                    "repository's own record",
                    path,
                )
            )
            continue
        status = observed[path]
        entries.append(
            {
                "path": path,
                "status": status,
                "annotation": STATUS_ANNOTATION.get(status, "changed"),
            }
        )
    return entries


def derive_promotions(
    repository: Path,
    baseline: str | None,
    candidate: str,
    declared: Sequence[str],
) -> tuple[list[dict[str, Any]], list[str]]:
    changed = (
        changed_gitlinks(repository, baseline, candidate)
        if baseline is not None
        else []
    )
    affected = sorted(set(changed) | set(declared))
    promotions: list[dict[str, Any]] = []
    for path in affected:
        candidate_mode, candidate_object = tree_entry(repository, candidate, path)
        baseline_mode, baseline_object = (
            tree_entry(repository, baseline, path)
            if baseline is not None
            else (None, None)
        )
        promotions.append(
            {
                "path": path,
                "declared": path in declared,
                "changed_in_range": path in changed,
                "baseline_mode": baseline_mode,
                "baseline_gitlink": baseline_object,
                "recorded_mode": candidate_mode,
                "recorded_gitlink": candidate_object,
            }
        )
    return promotions, changed


def evaluate_candidate_binding(
    repository: Path,
    candidate: str,
    output_targets: set[str],
    blockers: list[dict[str, Any]],
) -> dict[str, Any]:
    head = try_resolve_commit(repository, "HEAD")
    binding: dict[str, Any] = {
        "candidate": candidate,
        "head": head,
        "candidate_is_ancestor_of_head": None,
        "changed_paths_after_candidate": [],
        "gitlinks_moved_after_candidate": [],
    }
    if head is None:
        blockers.append(
            blocker(
                "CANDIDATE_NOT_FINAL",
                "the committed head does not resolve, so the candidate cannot be proven final",
            )
        )
        return binding

    ancestor = candidate == head or is_ancestor(repository, candidate, head)
    binding["candidate_is_ancestor_of_head"] = ancestor
    if not ancestor:
        blockers.append(
            blocker(
                "CANDIDATE_NOT_FINAL",
                f"candidate {candidate} is not an ancestor of the committed head {head}",
            )
        )
        return binding

    moved = changed_gitlinks(repository, candidate, head) if candidate != head else []
    changed_after = (
        sorted(committed_path_status(repository, candidate, head))
        if candidate != head
        else []
    )
    binding["changed_paths_after_candidate"] = changed_after
    for path in changed_after:
        if path not in output_targets and path not in moved:
            blockers.append(
                blocker(
                    "CANDIDATE_NOT_FINAL",
                    f"{path} changed between candidate {candidate} and head {head}; only final "
                    "record outputs may change after the candidate",
                    path,
                )
            )

    binding["gitlinks_moved_after_candidate"] = moved
    for path in moved:
        blockers.append(
            blocker(
                "CANDIDATE_NOT_FINAL",
                f"gitlink {path} moved between candidate {candidate} and head {head}; "
                "no gitlink movement is a final-record output",
                path,
            )
        )
    return binding


def markdown_table_rows(block: str, heading: str) -> list[list[str]]:
    """Parse one rendered Markdown table, honoring backslash-escaped separators."""
    lines = block.splitlines()
    try:
        index = lines.index(heading) + 1
    except ValueError:
        return []
    while index < len(lines) and not lines[index].strip():
        index += 1
    rows: list[list[str]] = []
    while index < len(lines) and lines[index].lstrip().startswith("|"):
        line = lines[index].strip()
        cells: list[str] = []
        current: list[str] = []
        escaped = False
        for character in line[1:-1]:
            if escaped:
                current.append(character)
                escaped = False
            elif character == "\\":
                escaped = True
            elif character == "|":
                cells.append("".join(current).strip())
                current = []
            else:
                current.append(character)
        cells.append("".join(current).strip())
        rows.append(cells)
        index += 1
    if len(rows) < 2:
        return []
    return rows[2:]


def markdown_value(cell: str) -> str:
    value = cell.strip()
    if value.startswith("**") and value.endswith("**"):
        value = value[2:-2]
    match = re.fullmatch(r"(`+)( ?)(.*?)( ?)\1", value)
    if match:
        value = match.group(3)
    return value.strip()


def parse_recorded_test_results(block: str) -> dict[str, Any]:
    rows = markdown_table_rows(block, "### Test Results")
    if not rows:
        raise ValueError("generated record has no parseable Test Results table")
    projects: list[dict[str, Any]] = []
    total_row: list[str] | None = None
    for row in rows:
        if len(row) not in (7, 8):
            raise ValueError(
                f"Test Results row has {len(row)} columns, expected 7 or 8"
            )
        values = [markdown_value(cell) for cell in row]
        if values[0].startswith("Total (computed)"):
            total_row = values
            continue
        if values[1] != "PARSED":
            raise ValueError(f"test project {values[0]} is not PARSED")
        if len(values) == 8:
            total, executed, passed, failed, skipped, digest = values[2:]
        else:
            total, passed, failed, skipped, digest = values[2:]
            executed = str(int(total) - int(skipped))
        if not re.fullmatch(r"(?:[0-9a-f]{16}|[0-9a-f]{64})", digest):
            raise ValueError(
                f"test project {values[0]} has no full artifact SHA-256 or legacy 16-digit prefix"
            )
        counts = {
            "total": int(total),
            "executed": int(executed),
            "passed": int(passed),
            "failed": int(failed),
            "skipped": int(skipped),
        }
        if counts["total"] <= 0:
            raise ValueError(f"test project {values[0]} records zero tests")
        if counts["total"] != counts["passed"] + counts["failed"] + counts["skipped"]:
            raise ValueError(
                f"test project {values[0]} has inconsistent recorded counts"
            )
        projects.append(
            {
                "project": values[0],
                "state": "PARSED",
                "artifact": None,
                "sha256": digest,
                "modified": None,
                "counts": counts,
            }
        )
    if total_row is None or not projects:
        raise ValueError("generated record has no computed total row")
    if len(total_row) == 8:
        recorded_totals = [int(markdown_value(value)) for value in total_row[2:7]]
        fields = ("total", "executed", "passed", "failed", "skipped")
    else:
        recorded_totals = [int(markdown_value(value)) for value in total_row[2:6]]
        fields = ("total", "passed", "failed", "skipped")
    totals = {
        field: sum(int(project["counts"][field]) for project in projects)
        for field in ("total", "executed", "passed", "failed", "skipped")
    }
    if recorded_totals != [totals[field] for field in fields]:
        raise ValueError("computed total row disagrees with its project rows")
    parsed_label = total_row[1]
    if parsed_label != f"{len(projects)} parsed":
        raise ValueError("computed total row carries the wrong parsed-project count")
    return {
        "required_projects": {},
        "allowed_skipped_tests": {},
        "projects": projects,
        "totals": totals,
    }


def parse_recorded_build_manifest(
    block: str, candidate: str | None
) -> dict[str, Any] | None:
    """Parse a persisted candidate-bound test build manifest when one is present."""
    if "### Test Build Manifest" not in block:
        return None
    identity = re.search(
        r"^Candidate `([0-9a-f]{40})` was clean-rebuilt; every test binary below embeds that SourceRevisionId\.$",
        block,
        re.MULTILINE,
    )
    if identity is None:
        raise ValueError("Test Build Manifest has no candidate identity")
    recorded_candidate = identity.group(1)
    if candidate is None or recorded_candidate != candidate:
        raise ValueError("Test Build Manifest candidate disagrees with the record candidate")
    projects: list[dict[str, Any]] = []
    seen_projects: set[str] = set()
    seen_binaries: set[str] = set()
    for row in markdown_table_rows(block, "#### Candidate-Bound Test Binaries"):
        if len(row) != 4:
            raise ValueError(
                f"Test Build Manifest row has {len(row)} columns, expected 4"
            )
        project, binary, source_revision, digest = [
            markdown_value(cell) for cell in row
        ]
        binary = safe_relative_path(binary)
        if not project or project in seen_projects:
            raise ValueError(f"Test Build Manifest repeats or omits project {project!r}")
        if binary in seen_binaries:
            raise ValueError(f"Test Build Manifest reuses test binary {binary}")
        if source_revision != recorded_candidate:
            raise ValueError(
                f"Test Build Manifest project {project} has a foreign SourceRevisionId"
            )
        if not re.fullmatch(r"[0-9a-f]{64}", digest):
            raise ValueError(
                f"Test Build Manifest project {project} has no full binary SHA-256"
            )
        seen_projects.add(project)
        seen_binaries.add(binary)
        projects.append(
            {
                "project": project,
                "binary": binary,
                "source_revision": source_revision,
                "sha256": digest,
                "modified": None,
            }
        )
    if not projects:
        raise ValueError("Test Build Manifest contains no project rows")
    return {"candidate": recorded_candidate, "projects": projects}


def parse_recorded_promotions(block: str) -> list[dict[str, Any]]:
    rows = markdown_table_rows(block, "### Gitlink Promotions")
    promotions: list[dict[str, Any]] = []
    for row in rows:
        if len(row) != 5:
            raise ValueError(
                f"Gitlink Promotions row has {len(row)} columns, expected 5"
            )
        path, declared, mode, recorded, baseline = [
            markdown_value(cell) for cell in row
        ]
        promotions.append(
            {
                "path": safe_relative_path(path),
                "declared": declared == "yes",
                "recorded_mode": None if mode == "—" else mode,
                "recorded_gitlink": None if recorded == "—" else recorded,
                "baseline_gitlink": None if baseline == "—" else baseline,
            }
        )
    return promotions


def parse_recorded_skip_policy(block: str) -> tuple[dict[str, str], set[str]]:
    policy: dict[str, str] = {}
    observed: set[str] = set()
    for row in markdown_table_rows(block, "### Allowed Skipped Tests"):
        if len(row) != 3:
            raise ValueError(
                f"Allowed Skipped Tests row has {len(row)} columns, expected 3"
            )
        test, reason, state = [markdown_value(cell) for cell in row]
        if not test or not reason or state not in ("yes", "no"):
            raise ValueError("Allowed Skipped Tests carries an invalid row")
        if test in policy:
            raise ValueError(f"Allowed Skipped Tests repeats {test}")
        policy[test] = reason
        if state == "yes":
            observed.add(test)
    return policy, observed


def verify_live(repository: Path, args: argparse.Namespace) -> dict[str, Any]:
    document = empty_document(repository)
    blockers: list[dict[str, Any]] = document["blockers"]
    warnings: list[dict[str, Any]] = document["warnings"]

    if not args.story or not args.story.strip():
        raise GateError(
            "INVALID_SCOPE", "--story is required and must name the record to derive"
        )
    story = safe_relative_path(args.story.strip())
    document["story"] = story
    story_file = repository / story
    if not story_file.is_file():
        raise GateError("INVALID_SCOPE", f"story record does not exist: {story}", story)
    story_file = contained_file(repository, story, "story record")
    try:
        content = story_file.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        raise GateError(
            "INVALID_SCOPE", f"story record is unreadable: {error}", story
        ) from error

    anchor, _, _ = record_anchor(content)
    document["record"]["anchor"] = anchor
    document["record"]["generated_block"] = anchor == "generated-block"
    document["derived"]["record_section"] = anchor is not None
    if anchor is None:
        blockers.append(
            blocker(
                "RECORD_NOT_DERIVED",
                f"{story} exposes no section this generator can replace: expected the "
                f"{RECORD_BEGIN_MARKER} marker, a `{STORY_ANCHOR}` heading, or a `{SPEC_ANCHOR}` heading",
                story,
            )
        )

    candidate = resolve_commit(repository, args.candidate, "CANDIDATE_UNRESOLVABLE")
    document["candidate"] = candidate
    document["derived"]["candidate"] = True

    frontmatter = parse_frontmatter(content)
    baseline_input = (
        args.baseline
        or frontmatter_scalar(frontmatter, "baseline_commit")
        or (frontmatter_scalar(frontmatter, "baseline_revision"))
    )
    baseline: str | None = None
    if not baseline_input or baseline_input == "NO_VCS":
        blockers.append(
            blocker(
                "BASELINE_NOT_TRUSTWORTHY",
                "no trustworthy baseline was supplied or recorded, so the committed "
                "file-list range cannot be derived",
            )
        )
    else:
        baseline = try_resolve_commit(repository, baseline_input)
        if baseline is None:
            blockers.append(
                blocker(
                    "BASELINE_NOT_TRUSTWORTHY",
                    f"baseline does not resolve to a commit: {baseline_input}",
                )
            )
        elif not is_ancestor(repository, baseline, candidate):
            blockers.append(
                blocker(
                    "BASELINE_NOT_TRUSTWORTHY",
                    f"baseline {baseline} is not an ancestor of candidate {candidate}",
                )
            )
            baseline = None
    document["baseline"] = baseline

    declared_paths = [promotion_path(path) for path in args.submodule]
    remote_paths = [promotion_path(path) for path in args.require_remote]
    root_paths = sorted(
        set(root_submodule_paths(repository, candidate))
        | (
            set(root_submodule_paths(repository, baseline))
            if baseline is not None
            else set()
        )
    )
    structural_gitlinks = (
        changed_gitlinks(repository, baseline, candidate) if baseline else []
    )

    declarations = [parse_test_declaration(value) for value in args.test_results]
    evidence_paths = {artifact for _, artifact in declarations}
    expected_projects = root_test_projects(repository, root_paths)
    allowed_skips = frontmatter_allowed_skips(frontmatter)

    output_targets = {story}
    sprint_status = f"{PurePosixPath(story).parent}/sprint-status.yaml"
    if (repository / sprint_status).is_file():
        output_targets.add(sprint_status)

    entries = derive_file_list(
        repository,
        baseline,
        candidate,
        root_paths,
        structural_gitlinks,
        output_targets,
        evidence_paths,
        blockers,
        warnings,
    )
    derived_paths = [entry["path"] for entry in entries]
    declared_list, declared_lists = declared_file_list(content)
    # The umbrella's own delta can never surface a path inside an initialized
    # submodule, so the record itself is the surface this guard defends: a
    # submodule-internal path gets into a File List by being written there.
    submodule_prefixes = tuple(f"{path}/" for path in root_paths)
    for path in declared_list:
        if path.startswith(submodule_prefixes):
            blockers.append(
                blocker(
                    "SUBMODULE_INTERNAL_PATH",
                    f"{path} is inside a root-declared submodule; it belongs to that "
                    "repository's own record",
                    path,
                )
            )
    missing = sorted(set(derived_paths) - set(declared_list))
    unexpected = sorted(set(declared_list) - set(derived_paths))
    document["file_list"] = {
        "derived": derived_paths,
        "declared": declared_list,
        "missing": missing,
        "unexpected": unexpected,
        "entries": entries,
    }
    document["record"]["declared_file_list"] = declared_list

    # A record that already claims to be generated must agree exactly. A record
    # with no list yet is the state this generator exists to fill, not drift.
    if (document["record"]["generated_block"] or declared_list) and (
        missing or unexpected
    ):
        detail = []
        if missing:
            detail.append(f"missing {len(missing)}: {', '.join(missing[:5])}")
        if unexpected:
            detail.append(f"unexpected {len(unexpected)}: {', '.join(unexpected[:5])}")
        blockers.append(
            blocker(
                "FILE_LIST_DRIFT",
                f"the record's File List disagrees with the derived set — {'; '.join(detail)}",
                story,
            )
        )
    headings = count_file_list_headings(content)
    if headings > 1 or declared_lists > 1:
        blockers.append(
            blocker(
                "FILE_LIST_DRIFT",
                f"{story} carries {headings} `{STORY_ANCHOR}` heading(s) over {declared_lists} "
                "path list(s); a record has exactly one derived File List",
                story,
            )
        )

    test_results = derive_test_results(
        repository,
        declarations,
        expected_projects,
        allowed_skips,
        blockers,
        warnings,
    )
    document["test_results"] = test_results
    document["derived"]["test_results"] = bool(test_results["projects"]) and all(
        item["state"] == "PARSED" and item["counts"] and item["counts"]["total"] > 0
        for item in test_results["projects"]
    )
    document["build_manifest"] = derive_test_build_manifest(
        repository, candidate, test_results, blockers
    )
    document["newest_derived_input"] = evaluate_staleness(
        repository, test_results, derived_paths, output_targets, blockers
    )

    promotions, changed = derive_promotions(
        repository, baseline, candidate, declared_paths
    )
    document["promotions"] = promotions
    document["candidate_binding"] = evaluate_candidate_binding(
        repository, candidate, output_targets, blockers
    )

    gate = run_promotion_gate(
        repository, baseline, candidate, declared_paths, remote_paths
    )
    document["promotion_gate"] = gate
    # Branch on the document's own result: exit 1 and exit 2 both emit valid
    # JSON, and error codes land inside blockers[] outside the frozen table.
    if gate.get("result") != "pass":
        gate_codes = (
            ", ".join(item.get("code", "?") for item in gate.get("blockers", []))
            or "none"
        )
        blockers.append(
            blocker(
                "PROMOTION_GATE_NOT_PASS",
                f"the embedded promotion completion gate reported {gate.get('result')!r} "
                f"with blockers: {gate_codes}",
            )
        )

    if not all(document["derived"].values()):
        undelivered = sorted(
            name for name, value in document["derived"].items() if not value
        )
        if not any(item["code"] == "RECORD_NOT_DERIVED" for item in blockers):
            blockers.append(
                blocker(
                    "RECORD_NOT_DERIVED",
                    "the record derived nothing for: " + ", ".join(undelivered),
                )
            )

    document["result"] = "blocked" if blockers else "pass"
    return document


def verify_historical(repository: Path, args: argparse.Namespace) -> dict[str, Any]:
    """Verify an already-closed record read-only. This function performs no writes."""
    document = empty_document(repository)
    document["mode"] = "historical"
    blockers: list[dict[str, Any]] = document["blockers"]
    warnings: list[dict[str, Any]] = document["warnings"]

    if not args.story or not args.story.strip():
        raise GateError(
            "INVALID_SCOPE", "--story is required and must name the record to verify"
        )
    story = safe_relative_path(args.story.strip())
    document["story"] = story
    story_file = repository / story
    if not story_file.is_file():
        raise GateError("INVALID_SCOPE", f"story record does not exist: {story}", story)
    story_file = contained_file(repository, story, "story record")
    try:
        content = story_file.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        raise GateError(
            "INVALID_SCOPE", f"story record is unreadable: {error}", story
        ) from error

    block = generated_block(content)
    marker_present = (
        RECORD_BEGIN_MARKER in content
        or RECORD_END_MARKER in content
        or "**Final record** —" in content
    )
    record_header = (
        re.search(
            rf"^\*\*Final record\*\* — `{re.escape(SCHEMA)}`, result \*\*(?:PASS|BLOCKED)\*\*,",
            block,
            re.MULTILINE,
        )
        if block
        else None
    )
    generated = record_header is not None
    classification = (
        "generated"
        if generated
        else ("malformed-generated" if marker_present else "pre-generator")
    )
    document["classification"] = classification
    document["boundary"] = (
        "Committed bytes, path modes, and cross-record claims are verified. A former "
        "uncommitted working tree is not reconstructed and is not claimed."
    )

    # D4: a record carrying no story-final-record-v1 block predates this
    # generator, so its AC2/AC3-shaped findings are reported without blocking and
    # without rewriting the closed record.
    def finding(code: str, message: str, path: str | None = None) -> None:
        if classification != "pre-generator":
            blockers.append(blocker(code, message, path))
        else:
            warnings.append(diagnostic(code, message, path))

    anchor, _, _ = record_anchor(content)
    document["record"]["anchor"] = anchor
    document["record"]["generated_block"] = anchor == "generated-block"
    document["derived"]["record_section"] = anchor is not None
    if anchor is None:
        blockers.append(
            blocker(
                "RECORD_NOT_DERIVED",
                f"{story} exposes no derivable record section",
                story,
            )
        )
        document["result"] = "blocked"
        return document
    if classification == "malformed-generated":
        blockers.append(
            blocker(
                "RECORD_NOT_DERIVED",
                f"{story} carries generated-record markers without a structurally valid {SCHEMA} block",
                story,
            )
        )
    elif record_header is not None and "result **PASS**" not in record_header.group(0):
        blockers.append(
            blocker(
                "RECORD_CONTENT_DRIFT",
                f"{story} is a generated record whose own recorded result is not PASS",
                story,
            )
        )

    frontmatter = parse_frontmatter(content)
    baseline_input = frontmatter_scalar(
        frontmatter, "baseline_commit"
    ) or frontmatter_scalar(frontmatter, "baseline_revision")
    file_list_commit = frontmatter_scalar(frontmatter, "file_list_commit")

    baseline = (
        try_resolve_commit(repository, baseline_input) if baseline_input else None
    )
    document["baseline"] = baseline
    if baseline is None:
        finding(
            "BASELINE_NOT_TRUSTWORTHY",
            f"{story} records no resolvable baseline, so its File List cannot be re-derived",
            story,
        )

    candidate = (
        try_resolve_commit(repository, file_list_commit) if file_list_commit else None
    )
    document["candidate"] = candidate
    document["derived"]["candidate"] = candidate is not None
    if candidate is None:
        finding(
            "CANDIDATE_NOT_FINAL",
            f"{story} records no resolvable `file_list_commit`, so its recorded paths "
            "cannot be compared against any single revision",
            story,
        )

    declared_list, declared_lists = declared_file_list(content)
    document["record"]["declared_file_list"] = declared_list
    document["record"]["declared_list_count"] = declared_lists
    root_paths = sorted(
        set(root_submodule_paths(repository, candidate or "HEAD"))
        | (
            set(root_submodule_paths(repository, baseline))
            if baseline is not None
            else set()
        )
    )
    submodule_prefixes = tuple(f"{path}/" for path in root_paths)
    for path in declared_list:
        if path.startswith(submodule_prefixes):
            finding(
                "SUBMODULE_INTERNAL_PATH",
                f"{path} is inside a root-declared submodule and belongs to that "
                "repository's own record",
                path,
            )
    headings = count_file_list_headings(content)
    if headings > 1 or declared_lists > 1:
        finding(
            "FILE_LIST_DRIFT",
            f"{story} carries {headings} `{STORY_ANCHOR}` heading(s) over {declared_lists} "
            "path list(s); a record has exactly one File List",
            story,
        )

    derived_paths: list[str] = []
    comparable = bool(
        baseline is not None
        and candidate is not None
        and is_ancestor(repository, baseline, candidate)
    )
    if baseline is not None and candidate is not None and not comparable:
        finding(
            "BASELINE_NOT_TRUSTWORTHY",
            f"baseline {baseline} is not an ancestor of file_list_commit {candidate}",
            story,
        )
    if comparable:
        assert baseline is not None and candidate is not None
        changed_structural = changed_gitlinks(repository, baseline, candidate)
        root_set = set(root_paths) | set(changed_structural)
        derived_paths = sorted(
            path
            for path in committed_path_status(repository, baseline, candidate)
            if path not in root_set
        )
        # Gitlinks the record declares are promotion state, not file-list drift:
        # comparing them as paths would report every correct promotion as an error.
        missing = sorted(set(derived_paths) - set(declared_list))
        unexpected = sorted(set(declared_list) - set(derived_paths) - root_set)
        document["file_list"] = {
            "derived": derived_paths,
            "declared": declared_list,
            "missing": missing,
            "unexpected": unexpected,
            "entries": [],
        }
        if missing or unexpected:
            finding(
                "FILE_LIST_DRIFT",
                f"the recorded File List does not equal the committed range "
                f"{baseline[:7]}..{candidate[:7]} — missing {len(missing)}, unexpected {len(unexpected)}",
                story,
            )
    else:
        document["file_list"] = {
            "derived": [],
            "declared": declared_list,
            "missing": [],
            "unexpected": [],
            "entries": [],
        }

    if generated and block is not None:
        identity = re.search(
            r"^Baseline `([^`]+)` → candidate `([^`]+)`\.$",
            block,
            re.MULTILINE,
        )
        if identity is None or identity.groups() != (baseline, candidate):
            finding(
                "RECORD_CONTENT_DRIFT",
                "the generated block's baseline/candidate line disagrees with frontmatter",
                story,
            )
        try:
            test_results = parse_recorded_test_results(block)
            skip_policy, observed_skips = parse_recorded_skip_policy(block)
            test_results["allowed_skipped_tests"] = skip_policy
            document["test_results"] = test_results
            totals = test_results["totals"]
            document["derived"]["test_results"] = bool(totals and totals["total"] > 0)
            if totals and totals["failed"]:
                finding(
                    "TEST_RESULTS_FAILED",
                    f"generated record carries {totals['failed']} failed test(s)",
                    story,
                )
            if totals and totals["skipped"] != len(observed_skips):
                finding(
                    "TEST_SKIP_NOT_ALLOWED",
                    f"generated record carries {totals['skipped']} skipped test(s) but "
                    f"documents {len(observed_skips)} observed allowance(s)",
                    story,
                )
        except (GateError, ValueError) as error:
            finding(
                "TEST_COUNT_INCONSISTENT",
                f"generated record's Test Results section is invalid: {error}",
                story,
            )

        try:
            build_manifest = parse_recorded_build_manifest(block, candidate)
            if build_manifest is not None:
                recorded_projects = {
                    item["project"] for item in document["test_results"]["projects"]
                }
                manifest_projects = {
                    item["project"] for item in build_manifest["projects"]
                }
                if manifest_projects != recorded_projects:
                    raise ValueError(
                        "Test Build Manifest project set disagrees with Test Results"
                    )
                document["build_manifest"] = build_manifest
        except (GateError, ValueError) as error:
            finding(
                "TEST_BUILD_NOT_BOUND",
                f"generated record's Test Build Manifest is invalid: {error}",
                story,
            )

        try:
            if baseline is None or candidate is None:
                raise ValueError(
                    "Gitlink Promotions cannot be verified without both revisions"
                )
            promotions = parse_recorded_promotions(block)
            promotion_paths = [item["path"] for item in promotions]
            if len(promotion_paths) != len(set(promotion_paths)):
                raise ValueError("Gitlink Promotions repeats a path")
            changed_paths = set(changed_structural) if comparable else set()
            if not changed_paths.issubset(promotion_paths):
                raise ValueError(
                    "Gitlink Promotions omits changed path(s): "
                    + ", ".join(sorted(changed_paths - set(promotion_paths)))
                )
            for item in promotions:
                path = item["path"]
                baseline_mode, baseline_object = tree_entry(repository, baseline, path)
                candidate_mode, candidate_object = tree_entry(
                    repository, candidate, path
                )
                if (
                    item["recorded_mode"] != candidate_mode
                    or item["recorded_gitlink"] != candidate_object
                    or item["baseline_gitlink"] != baseline_object
                    or (baseline_mode != "160000" and candidate_mode != "160000")
                ):
                    raise ValueError(
                        f"Gitlink Promotions disagrees with tree entries for {path}"
                    )
                item["baseline_mode"] = baseline_mode
                item["changed_in_range"] = path in changed_paths
            document["promotions"] = promotions
        except (GateError, ValueError) as error:
            finding(
                "RECORD_CONTENT_DRIFT",
                f"generated record's Gitlink Promotions section is invalid: {error}",
                story,
            )

    if classification == "pre-generator":
        try:
            legacy_promotions = parse_recorded_promotions(content)
            for item in legacy_promotions:
                path = item["path"]
                baseline_mode, _ = (
                    tree_entry(repository, baseline, path)
                    if baseline is not None
                    else (None, None)
                )
                candidate_mode, _ = (
                    tree_entry(repository, candidate, path)
                    if candidate is not None
                    else (None, None)
                )
                item["baseline_mode"] = baseline_mode
                item["changed_in_range"] = bool(
                    comparable
                    and (baseline_mode == "160000" or candidate_mode == "160000")
                )
            document["promotions"] = legacy_promotions
        except (GateError, ValueError) as error:
            warnings.append(
                diagnostic(
                    "RECORD_CONTENT_DRIFT",
                    f"pre-generator Gitlink Promotions section could not be parsed: {error}",
                    story,
                )
            )

    if classification == "pre-generator":
        warnings.append(
            diagnostic(
                "RECORD_NOT_DERIVED",
                f"{story} carries no `{SCHEMA}` block, so its counts and File List were "
                "authored before this generator existed; reported without blocking per the "
                "approved historical disposition",
                story,
            )
        )
    # The promotion checker inspects live submodule worktrees, which say nothing
    # about a closed record; running it here would claim to reconstruct a former
    # working tree, which AC7 forbids.
    document["promotion_gate"] = None
    document["result"] = "blocked" if blockers else "pass"
    return document


def verify_inserted_record(
    repository: Path, args: argparse.Namespace
) -> dict[str, Any]:
    """Verify that the story carries the exact Markdown emitted by one passing bundle."""
    document = empty_document(repository)
    document["mode"] = "inserted-verification"
    expected = (args.verify_record_sha256 or "").lower()
    if not re.fullmatch(r"[0-9a-f]{64}", expected):
        raise GateError(
            "INVALID_SCOPE", "--verify-record-sha256 must be 64 hexadecimal characters"
        )
    if not args.story or not args.story.strip():
        raise GateError(
            "INVALID_SCOPE", "--story is required for inserted-record verification"
        )
    story = safe_relative_path(args.story.strip())
    document["story"] = story
    story_file = contained_file(repository, story, "story record")
    try:
        content = story_file.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        raise GateError(
            "INVALID_SCOPE", f"story record is unreadable: {error}", story
        ) from error
    block = generated_block(content)
    document["record"]["anchor"] = "generated-block" if block is not None else None
    document["record"]["generated_block"] = block is not None
    document["derived"]["record_section"] = block is not None
    actual = (
        hashlib.sha256(block.encode("utf-8")).hexdigest() if block is not None else None
    )
    document["record"]["sha256"] = actual
    document["record"]["expected_sha256"] = expected
    if actual != expected:
        document["blockers"].append(
            blocker(
                "RECORD_CONTENT_DRIFT",
                f"inserted record digest is {actual or 'missing'}, expected {expected}",
                story,
            )
        )
    document["result"] = "blocked" if document["blockers"] else "pass"
    return document


def verify(args: argparse.Namespace) -> dict[str, Any]:
    # Path("") is Path("."), so an unset shell variable would silently measure
    # whatever directory the process happened to start in.
    if not args.repository.strip():
        raise GateError("INVALID_SCOPE", "--repository must not be empty")
    repository = validate_repository(Path(args.repository).expanduser().resolve())
    if args.verify_record_sha256:
        if args.historical:
            raise GateError(
                "INVALID_SCOPE", "--historical and --verify-record-sha256 are exclusive"
            )
        return verify_inserted_record(repository, args)
    if args.historical:
        return verify_historical(repository, args)
    return verify_live(repository, args)


def markdown_code(value: Any) -> str:
    text = str(value)
    longest = max((len(run) for run in re.findall(r"`+", text)), default=0)
    delimiter = "`" * (longest + 1)
    padding = " " if text.startswith("`") or text.endswith("`") else ""
    return f"{delimiter}{padding}{text}{padding}{delimiter}"


def markdown_table_text(value: Any) -> str:
    return str(value).replace("\\", "\\\\").replace("|", "\\|").replace("\n", "<br>")


def markdown_table_code(value: Any) -> str:
    return markdown_code(markdown_table_text(value))


def render_counts_table(document: dict[str, Any]) -> list[str]:
    lines = [
        "| Test project | State | Total | Executed | Passed | Failed | Skipped | Artifact SHA-256 |",
        "| --- | --- | --- | --- | --- | --- | --- | --- |",
    ]
    for item in document["test_results"]["projects"]:
        counts = item["counts"] or {}
        digest = item["sha256"]
        lines.append(
            "| {project} | {state} | {total} | {executed} | {passed} | {failed} | {skipped} | {digest} |".format(
                project=markdown_table_text(item["project"]),
                state=item["state"],
                total=counts.get("total", "—"),
                executed=counts.get("executed", "—"),
                passed=counts.get("passed", "—"),
                failed=counts.get("failed", "—"),
                skipped=counts.get("skipped", "—"),
                digest=f"`{digest}`" if digest else "—",
            )
        )
    totals = document["test_results"]["totals"]
    if totals is None:
        lines.append("| **Total** | **NOT_RUN** | — | — | — | — | — | — |")
    else:
        lines.append(
            "| **Total (computed)** | **{count} parsed** | **{total}** | **{executed}** | "
            "**{passed}** | **{failed}** | **{skipped}** | — |".format(
                count=sum(
                    1
                    for item in document["test_results"]["projects"]
                    if item["state"] == "PARSED"
                ),
                **totals,
            )
        )
    return lines


def render_markdown(document: dict[str, Any]) -> str:
    lines: list[str] = [RECORD_BEGIN_MARKER, ""]
    derived = document["derived"]
    lines.append(
        f"**Final record** — `{document['schema']}`, result **{document['result'].upper()}**, "
        f"mode `{document.get('mode', 'live')}`. The JSON document is authoritative; this "
        "Markdown is rendered from it."
    )
    lines.append("")
    # Name what was derived. A run that derived nothing must never render the
    # same block as a fully measured one.
    lines.append(
        "Derived: test results **{tests}**, candidate **{candidate}**, record section **{record}** "
        "· {parsed} test artifact(s) parsed · {files} file-list path(s) · {promotions} gitlink "
        "promotion(s) evaluated.".format(
            tests="yes" if derived["test_results"] else "NO",
            candidate="yes" if derived["candidate"] else "NO",
            record="yes" if derived["record_section"] else "NO",
            parsed=sum(
                1
                for item in document["test_results"]["projects"]
                if item["state"] == "PARSED"
            ),
            files=len(document["file_list"]["derived"]),
            promotions=len(document["promotions"]),
        )
    )
    lines.extend(
        [
            "",
            f"Baseline `{document['baseline']}` → candidate `{document['candidate']}`.",
            "",
        ]
    )

    lines.extend([STORY_ANCHOR, ""])
    entries = document["file_list"].get("entries") or [
        {"path": path, "annotation": "recorded"}
        for path in document["file_list"]["derived"]
    ]
    if entries:
        for entry in entries:
            lines.append(f"- {markdown_code(entry['path'])} ({entry['annotation']})")
    else:
        lines.append("_No path was derived. This record measured nothing._")
    lines.append("")

    lines.extend(["### Gitlink Promotions", ""])
    if document["promotions"]:
        lines.append(
            "| Path | Declared | Recorded mode | Recorded commit | Baseline commit |"
        )
        lines.append("| --- | --- | --- | --- | --- |")
        for item in document["promotions"]:
            lines.append(
                "| {path} | {declared} | {mode} | {recorded} | {baseline} |".format(
                    path=markdown_table_code(item["path"]),
                    declared="yes" if item["declared"] else "no",
                    mode=markdown_table_code(item["recorded_mode"] or "—"),
                    recorded=markdown_table_code(item["recorded_gitlink"] or "—"),
                    baseline=markdown_table_code(item["baseline_gitlink"] or "—"),
                )
            )
    else:
        lines.append(
            "_None. No root gitlink changed between the baseline and the candidate._"
        )
    lines.append("")

    lines.extend(["### Test Results", ""])
    lines.extend(render_counts_table(document))
    totals = document["test_results"]["totals"]
    not_run = [
        item["project"]
        for item in document["test_results"]["projects"]
        if item["state"] != "PARSED"
    ]
    # A red or partially unrun suite must be legible in the rendered block, not
    # only in a column a reader can skim past.
    if not_run:
        lines.extend(
            [
                "",
                f"**Not run: {', '.join(not_run)}.** No artifact was parsed for these projects.",
            ]
        )
    if totals and (totals["failed"] or totals["skipped"]):
        lines.extend(
            [
                "",
                f"**This suite is not fully green: {totals['failed']} failed, "
                f"{totals['skipped']} skipped.**",
            ]
        )
    lines.append("")

    lines.extend(["### Test Build Manifest", ""])
    build_manifest = document.get("build_manifest") or {"candidate": None, "projects": []}
    if build_manifest["projects"]:
        lines.append(
            f"Candidate `{build_manifest['candidate']}` was clean-rebuilt; every test binary below embeds that SourceRevisionId."
        )
        lines.extend(["", "#### Candidate-Bound Test Binaries", ""])
        lines.append("| Test project | Binary | Source revision | Binary SHA-256 |")
        lines.append("| --- | --- | --- | --- |")
        for item in build_manifest["projects"]:
            lines.append(
                "| {project} | {binary} | {revision} | {digest} |".format(
                    project=markdown_table_text(item["project"]),
                    binary=markdown_table_code(item["binary"]),
                    revision=markdown_table_code(item["source_revision"] or "unbound"),
                    digest=markdown_table_code(item["sha256"]),
                )
            )
    else:
        lines.append(
            "_No candidate-bound test binary was derived. A passing completion record requires one for every parsed test project._"
        )
    lines.append("")

    allowed_skips = document["test_results"].get("allowed_skipped_tests", {})
    if allowed_skips:
        observed_skips = {
            test
            for item in document["test_results"]["projects"]
            for test in item.get("skipped_tests", [])
        }
        lines.extend(["### Allowed Skipped Tests", ""])
        lines.append("| Test identity | Reason | Observed |")
        lines.append("| --- | --- | --- |")
        for test, reason in sorted(allowed_skips.items()):
            lines.append(
                "| {test} | {reason} | {observed} |".format(
                    test=markdown_table_text(test),
                    reason=markdown_table_text(reason),
                    observed="yes" if test in observed_skips else "no",
                )
            )
        lines.append("")

    lines.extend(["### Candidate Binding", ""])
    binding = document.get("candidate_binding")
    if binding is None:
        lines.append("_Not evaluated in this mode._")
    else:
        moved = binding["gitlinks_moved_after_candidate"]
        lines.append(
            "- Candidate `{candidate}` · committed head `{head}` · ancestor of head: "
            "**{ancestor}**".format(
                candidate=binding["candidate"],
                head=binding["head"] or "unresolved",
                ancestor={True: "yes", False: "NO", None: "unknown"}[
                    binding["candidate_is_ancestor_of_head"]
                ],
            )
        )
        lines.append(
            "- Gitlinks moved after the candidate: "
            + (", ".join(markdown_code(path) for path in moved) if moved else "none")
        )
        changed_after = binding.get("changed_paths_after_candidate", [])
        lines.append(
            "- Paths changed after the candidate: "
            + (
                ", ".join(markdown_code(path) for path in changed_after)
                if changed_after
                else "none"
            )
        )
    lines.append("")

    lines.extend(["### Promotion Completion Gate", ""])
    gate = document.get("promotion_gate")
    if gate is None:
        lines.append(
            "_Not run: this mode verifies a closed record and does not reconstruct or claim "
            "a former uncommitted working tree._"
        )
    else:
        gate_declared = (
            ", ".join(item["path"] for item in gate.get("declared", [])) or "none"
        )
        lines.append(
            "- Result **{result}** · declared: {declared} · changed gitlinks: {changed} · "
            "evaluated: {evaluated}".format(
                result=str(gate.get("result")).upper(),
                declared=gate_declared,
                changed=", ".join(gate.get("changed_gitlinks", [])) or "none",
                evaluated=", ".join(item["path"] for item in gate.get("evaluated", []))
                or "none",
            )
        )
        for item in gate.get("blockers", []):
            lines.append(f"- BLOCKER `{item['code']}`: {item['message']}")
        for item in gate.get("warnings", []):
            lines.append(f"- WARNING `{item['code']}`: {item['message']}")
    lines.append("")

    if document["blockers"] or document["warnings"]:
        lines.extend(["### Record Diagnostics", ""])
        for item in document["blockers"]:
            location = f" (`{item['path']}`)" if item.get("path") else ""
            lines.append(f"- **BLOCKER** `{item['code']}`{location}: {item['message']}")
            if item.get("remediation"):
                lines.append(f"  - Remediation: {item['remediation']}")
        for item in document["warnings"]:
            location = f" (`{item['path']}`)" if item.get("path") else ""
            lines.append(f"- WARNING `{item['code']}`{location}: {item['message']}")
        lines.append("")

    lines.append(RECORD_END_MARKER)
    return "\n".join(lines) + "\n"


def write_output(document: dict[str, Any], output_format: str) -> None:
    # decode() preserves undecodable bytes as surrogates, so a strict stdout
    # would raise UnicodeEncodeError here -- outside main()'s handlers, turning a
    # deliberate exit 2 into exit 1 with an empty, unparseable stdout.
    if getattr(sys.stdout, "errors", None) not in (
        "surrogateescape",
        "backslashreplace",
    ):
        try:
            sys.stdout.reconfigure(errors="backslashreplace")
        except (AttributeError, ValueError):  # pragma: no cover - exotic stdout
            pass

    if output_format == "json":
        sys.stdout.write(json.dumps(document, indent=2, ensure_ascii=False) + "\n")
        return
    if output_format == "bundle":
        markdown = render_markdown(document)
        bundle = {
            "schema": BUNDLE_SCHEMA,
            "document": document,
            "markdown": markdown,
            "markdown_sha256": hashlib.sha256(markdown.encode("utf-8")).hexdigest(),
        }
        sys.stdout.write(json.dumps(bundle, indent=2, ensure_ascii=False) + "\n")
        return
    sys.stdout.write(render_markdown(document))


# --------------------------------------------------------------------------- #
# Story final record v2: the isolated `--contract` route
# --------------------------------------------------------------------------- #
#
# The v2 route is additive. `main()` dispatches to it only when the exact
# `--contract` option is present, before the legacy parser runs, so every v1
# invocation, document, and exit code is unchanged. The v2 route reuses the
# hardened v1 Git, containment, snapshot, `.gitmodules`, raw-tree, and Markdown
# escaping helpers above, and accepts no caller-authored completion fact: every
# count, path, commit, gitlink, digest, exit, and verdict is derived from Git
# objects of the committed candidate and from measured JUnit result files.

V2_RECORD_SCHEMA_VERSION = "hexalith.conversations.story-final-record.v2"
V2_FAILURE_SCHEMA_VERSION = "hexalith.conversations.story-record-generator-failure.v1"
V2_CONTRACT_SCHEMA_VERSION = "hexalith.conversations.story-contract.v1"
V2_V14_CONTRACT_SCHEMA_VERSION = "hexalith.conversations.v14-story-contract.v1"
V2_BUNDLE_SCHEMA_VERSION = "hexalith.conversations.v9-authority-bundle.v1"

# Roots of trust: the tooling's own schema copies, never the evaluated
# repository's, so an evaluated candidate cannot redefine its own validator.
V2_SCHEMA_DIRECTORY = Path(__file__).resolve().parents[1] / "schemas"
V2_SCHEMA_FILES = {
    "record": "story-final-record-v2.schema.json",
    "failure": "story-record-generator-failure-v1.schema.json",
    "contract": "v9-story-contract-v1.schema.json",
    "contract_v14": "v14-story-contract-v1.schema.json",
    "bundle": "v9-authority-bundle-v1.schema.json",
}
V2_AUTHORITY_BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V2_GENERATOR_PATH = "_bmad/scripts/generate_story_record.py"
V2_7_2_SPEC_PATH = "_bmad-output/implementation-artifacts/spec-7-2-derive-test-path-candidate-submodule-and-gitlink-facts.md"
V2_7_2_SPRINT_PATH = "_bmad-output/implementation-artifacts/sprint-status.yaml"
V2_7_2_LIFECYCLE_PATHS = {V2_7_2_SPEC_PATH, V2_7_2_SPRINT_PATH}
V2_7_1_RECORD_PATH = "docs/release-evidence/story-7.1-final-record-v2.json"
V2_7_1_MARKDOWN_PATH = "docs/release-evidence/story-7.1-final-record-v2.md"
V2_7_2_RECORD_PATH = "docs/release-evidence/story-7.2-final-record-v2.json"
V2_7_2_MARKDOWN_PATH = "docs/release-evidence/story-7.2-final-record-v2.md"
V2_7_3_SPEC_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-7-3-integrate-generation-into-every-blocking-completion-transition.md"
)
V2_7_4_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/7.4.json"
V2_7_4_SPEC_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-7-4-verify-historical-mode-and-required-fault-injection-blockers.md"
)
V2_8_1_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/8.1.json"
V2_8_1_SPEC_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-8-1-generate-the-versioned-ux-disposition-contract.md"
)
V2_8_1_DISPOSITION_PATHS = (
    "docs/release-evidence/ux-preservation-disposition-v1.schema.json",
    "docs/release-evidence/ux-preservation-disposition-v1.json",
    "docs/release-evidence/ux-preservation-disposition-v1.md",
)
V2_8_1_SOURCES = (
    "_bmad-output/planning-artifacts/ux-design-specification.md",
    "_bmad-output/planning-artifacts/ux-requirement-map.md",
)
V2_8_1_PREDECESSOR = (
    "7.4", "docs/release-evidence/story-7.4-final-record-v2.json",
    "docs/release-evidence/story-7.4-final-record-v2.md",
)
V2_8_1_TEST_ASSEMBLY = (
    "tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/"
    "Hexalith.Conversations.Conformance.Tests.dll"
)
V2_8_1_ALLOWED_PATHS = frozenset({
    V2_8_1_SPEC_PATH,
    "_bmad-output/implementation-artifacts/epic-8-context.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/scripts/generate_story_record.py",
    "_bmad/scripts/generate_ux_preservation_disposition.py",
    "_bmad/scripts/tests/test_generate_story_record.py",
    "_bmad/scripts/tests/test_generate_ux_preservation_disposition.py",
    "docs/runbooks/story-final-record-generation.md",
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs",
    *V2_8_1_DISPOSITION_PATHS,
    "docs/release-evidence/story-8.1-final-record-v2.json",
    "docs/release-evidence/story-8.1-final-record-v2.md",
})
V2_8_2_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/8.2.json"
V2_8_2_SPEC_PATH = "_bmad-output/implementation-artifacts/spec-8-2-enforce-the-52-decision-28-acceptance-zero-gap-validator.md"
V2_8_2_PREDECESSOR = ("8.1", "docs/release-evidence/story-8.1-final-record-v2.json",
                      "docs/release-evidence/story-8.1-final-record-v2.md")
V2_8_2_ALLOWED_PATHS = (V2_8_1_ALLOWED_PATHS - {
    V2_8_1_SPEC_PATH, *V2_8_1_DISPOSITION_PATHS, V2_8_2_PREDECESSOR[1], V2_8_2_PREDECESSOR[2],
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs",
}) | {V2_8_2_SPEC_PATH, ".github/workflows/ci.yml", "docs/release-evidence/story-8.2-final-record-v2.json",
      "docs/release-evidence/story-8.2-final-record-v2.md"}
V2_8_2_FAULT_PROPERTY = "story82ObservedFault"
V2_8_2_REQUIRED_FAULTS = {
    "decision-missing": "UX_DECISION_MISSING", "decision-duplicate": "UX_DECISION_DUPLICATE",
    "decision-unknown": "UX_DECISION_UNKNOWN", "acceptance-missing": "UX_ACCEPTANCE_MISSING",
    "acceptance-duplicate": "UX_ACCEPTANCE_DUPLICATE", "acceptance-unknown": "UX_ACCEPTANCE_UNKNOWN",
    "owner-missing": "UX_OWNER_MISSING", "hash-missing": "UX_HASH_MISSING",
    "source-changed": "UX_SOURCE_DRIFT", "render-changed": "UX_RENDER_DRIFT",
    "json-order": "UX_ORDER_DRIFT", "markdown-order": "UX_ORDER_DRIFT",
    "row-activated": "UX_ACTIVATION_UNAUTHORIZED", "historical-owner": "UX_CURRENT_STORY_INVALID",
    "nonexistent-owner": "UX_CURRENT_STORY_INVALID",
}
V2_8_2_FAULT_LANES = {
    "AC-8.2-02": ["decision-missing"], "AC-8.2-03": ["decision-duplicate"],
    "AC-8.2-04": ["decision-unknown"],
    "AC-8.2-05": ["acceptance-missing", "acceptance-duplicate", "acceptance-unknown"],
    "AC-8.2-06": ["owner-missing", "hash-missing"], "AC-8.2-07": ["source-changed"],
    "AC-8.2-08": ["render-changed", "json-order", "markdown-order"],
    "AC-8.2-09": ["row-activated", "historical-owner", "nonexistent-owner"],
    "AC-8.2-10": list(V2_8_2_REQUIRED_FAULTS),
}
V2_9_1_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/9.1.json"
V2_9_1_GENERATOR_PATH = "_bmad/scripts/generate_conformance_tiering.py"
V2_9_1_DECISION_PATH = "docs/release-evidence/conformance-oracle-tiering-decision-v2.json"
V2_9_1_APPROVALS_PATH = "docs/release-evidence/conformance-oracle-tiering-approvals-v2.json"
V2_9_1_OUTPUT_PATHS = (
    "docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json",
    "docs/release-evidence/conformance-oracle-tiering-disposition-v2.json",
    "docs/release-evidence/conformance-oracle-tiering-disposition-v2.md",
)
V2_9_1_RECEIPT_PATH = "artifacts/v9/9.1/pre-split/receipt.json"
V2_9_1_FAULT_RESULT_PATH = "artifacts/v9/9.1/faults.xml"
V2_9_1_FAULT_COMMAND = (
    "python3 -m pytest -q _bmad/scripts/tests/test_conformance_tiering.py -k tiering_faults "
    "--junitxml=artifacts/v9/9.1/faults.xml"
)
V2_9_1_FAULT_PROPERTY = "story91ObservedFault"
V2_9_1_FAULT_CLASSNAME = "_bmad.scripts.tests.test_conformance_tiering"
V2_9_1_REQUIRED_FAULTS = {
    "assertion-missing": "CONFORMANCE_ASSERTION_MISSING",
    "assertion-duplicate": "CONFORMANCE_ASSERTION_DUPLICATE",
    "assertion-renamed": "CONFORMANCE_ASSERTION_RENAMED",
    "strength-weakened": "ASSERTION_STRENGTH_WEAKENED",
    "tier-unassigned": "TIER_UNASSIGNED",
    "tier-reason-missing": "TIER_REASON_MISSING",
    "approval-missing": "TIER_APPROVAL_MISSING",
    "denominator-drift": "FR20_DENOMINATOR_DRIFT",
    "public-widened": "PUBLIC_CONTRACT_WIDENED",
    "v1-mutated": "V1_ARTIFACT_DRIFT",
}
# The exact byte streams the Story 9.1 fault fixtures hash before, during, and after each mutation;
# `_bmad/scripts/tests/test_conformance_tiering.py` uses the same ordered set.
V2_9_1_FAULT_FIXTURE_PATHS = tuple(sorted((
    *V2_9_1_OUTPUT_PATHS,
    V2_9_1_APPROVALS_PATH,
    "tests/Hexalith.Conversations.Conformance.Tests/TenantIsolationConformanceSuiteTest.cs",
    "src/Hexalith.Conversations.Contracts/Conformance/TieringFaultProbeWidening.cs",
    "docs/release-evidence/release-baseline-v1.md",
)))
V2_9_1_BINDING_RULE = "SC-9.1 is HEAD^{commit} at final-record generation"
V2_9_2_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/9.2.json"
V2_9_2_AMENDMENT_PATH = "_bmad-output/planning-artifacts/v9/story-9.2-execution-amendment-v1.json"
V2_9_2_AMENDMENT_SHA256 = "921bf66838c6121ffd57421804192c108b02b5d0c71bd4063e3cb5664d399951"
V2_9_2_SPEC_PATH = "_bmad-output/implementation-artifacts/spec-9-2-make-the-portable-tier-structural-and-prove-complete-monotonic-tier-execution.md"
V2_9_2_BASELINE = "51aa06b856bcb0aaf22013153cfe02a51b046156"
V2_9_2_ENVIRONMENT_PATH = "_bmad-output/planning-artifacts/v9/story-9.2-candidate-environment-amendment-v1.json"
V2_9_2_ENVIRONMENT_SHA256 = "ae7175a058a5a2a9006d715a96481f25b9a748e1c918a32d111ab0b92c15607a"
V2_9_2_ENVIRONMENT_PROPOSAL_SHA256 = "a5c12ba32a82338ce8bcb791f8b64355e5a820d7433145e0648d107441bc3d20"
V2_9_2_ENVIRONMENT_CANDIDATE = "d54cba773290b555e1939e9d943ae84548e5eba3"
V2_9_2_VERIFIER_PATH = "_bmad/scripts/verify_conformance_tiering.py"
V2_9_2_RESULT_PATH = "artifacts/v9/9.2/AC-9.2-08.json"
V2_9_2_FAULT_RESULT_PATH = "artifacts/v9/9.2/AC-9.2-09.xml"
V2_9_2_PREDECESSOR = ("9.1", "docs/release-evidence/story-9.1-final-record-v2.json",
                       "docs/release-evidence/story-9.1-final-record-v2.md")
V2_9_2_BINDING_RULE = "SC-9.2 is HEAD^{commit} at first final-record generation; retained evidence stays stamped by SC-9.2"
V2_HISTORY_FIXTURE_PATH = "_bmad/scripts/fixtures/story-7.4-history-v1.json"
V2_HISTORY_OUTPUT_PATH = "artifacts/v9/7.4/AC-7.4-01.json"
V2_HISTORY_ANCHORS = (
    ("6.1", "spec-6-1-rebaseline-architecture-and-planning-authority.md", "16e3d3db4530719aa06129ba06b34bd78f7995eb"),
    ("6.2", "6-2-migrate-conversations-to-platform-owned-hosting.md", "e480c3f3176cdc3d911baf91eb3e7a8cd38874aa"),
    ("6.7", "6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md", "29def441408becfbbbdc5c59b9af14a7717cb21f"),
)
V2_HISTORY_LIMITS = (
    "A former uncommitted working tree is not reconstructed and is not claimed.",
    "Original TRX, test binaries, and raw promotion results were uncommitted; archived declarations are recorded-only.",
    "Story 6.1 has no recorded candidate; its closure commit is not a reconstructed candidate.",
    "Pre-generator findings retain their approved warning disposition.",
    "Only root Git objects are read; submodule contents, former runtime state, and CI enforcement are not verified.",
)
V2_REQUIRED_FAULTS = {
    "COUNT": "TEST_COUNT_INCONSISTENT",
    "SUBMODULE_PATH": "SUBMODULE_INTERNAL_PATH",
    "CANDIDATE": "CANDIDATE_NOT_FINAL",
    "GITLINK": "GITLINK_SCOPE_MISMATCH",
    "RESULT_MISSING": "TEST_RESULTS_MISSING",
    "RESULT_STALE": "TEST_RESULTS_STALE",
    "RESULT_FAILED": "TEST_FAILED",
    "RESULT_SKIPPED": "TEST_SKIPPED",
    "RESULT_NOT_RUN": "TEST_NOT_RUN",
    "LEDGER_EMPTY": "ASSERTION_LEDGER_EMPTY",
    "WORKFLOW_REMOVED": "WORKFLOW_INTEGRATION_MISSING",
    "WORKFLOW_DISPLACED": "WORKFLOW_INTEGRATION_DISPLACED",
    "MARKDOWN_DIGEST": "RECORD_CONTENT_DRIFT",
}
V2_FAULT_PROPERTY = "hexalith.fault-injection-result.v1"
# The governed Story 7.3 completion-route bodies, in ordinal order. The owner
# rebound the frozen inventory one-for-one to these routes in both skill trees;
# `verify_story_completion_workflows.py` proves them and this generator binds
# their candidate digests. Roots of trust live in the tooling, not in a result.
V2_7_3_WORKFLOW_BODIES = tuple(
    sorted(
        f"{tree}/{route}"
        for tree in (".agents/skills", ".claude/skills")
        for route in (
            "bmad-build/step-05-present.md",
            "bmad-build/step-oneshot.md",
            "bmad-build-auto/step-04-review.md",
            "bmad-code-review/steps/step-04-present.md",
        )
    )
)
V2_7_3_PREDECESSOR_RECORDS = (
    ("7.1", V2_7_1_RECORD_PATH, V2_7_1_MARKDOWN_PATH),
    ("7.2", V2_7_2_RECORD_PATH, V2_7_2_MARKDOWN_PATH),
)
# A committed pair pins its candidate for these contracts. Later commits may
# change only lifecycle bookkeeping: the story spec's frontmatter `status`, its
# sprint-status row, the `last_updated` date, the `# last_updated` header date,
# and, for Story 7.3, the spec's inserted final-record region.
V2_RETAINED_CANDIDATES = {
    V2_7_4_CONTRACT_PATH: (
        "7.4", V2_7_4_SPEC_PATH,
        "7-4-verify-historical-mode-and-required-fault-injection-blockers", True,
    ),
    "_bmad-output/planning-artifacts/v9/story-contracts/7.2.json": (
        "7.2",
        V2_7_2_SPEC_PATH,
        "7-2-derive-test-path-candidate-submodule-and-gitlink-facts",
        False,
    ),
    "_bmad-output/planning-artifacts/v9/story-contracts/7.3.json": (
        "7.3",
        V2_7_3_SPEC_PATH,
        "7-3-integrate-generation-into-every-blocking-completion-transition",
        True,
    ),
}
V2_RETAINED_OUTPUTS = {
    "7.4": (
        "docs/release-evidence/story-7.4-final-record-v2.json",
        "docs/release-evidence/story-7.4-final-record-v2.md",
    ),
    "7.2": (V2_7_2_RECORD_PATH, V2_7_2_MARKDOWN_PATH),
    "7.3": (
        "docs/release-evidence/story-7.3-final-record-v2.json",
        "docs/release-evidence/story-7.3-final-record-v2.md",
    ),
}
V2_ACCEPTANCE_SCHEMA_FILE = "v9-acceptance-result-v1.schema.json"
V2_ACCEPTANCE_SCHEMA_VERSION = "hexalith.conversations.acceptance-result.v1"
V2_ACCEPTANCE_OPTIONS = ("--repository", "--contract", "--scenario", "--output")
# Every stable verifier code is carried into a failure document when an
# acceptance result reports it, so the completion surface can report the exact
# cause rather than only the generator's aggregate TEST_RESULTS_FAILED code.
V2_PROPAGATED_ACCEPTANCE_CODES = (
    "ARGUMENT_INVALID",
    "CANDIDATE_UNRESOLVABLE",
    "CONTRACT_UNSUPPORTED",
    "GIT_UNAVAILABLE",
    "INTERNAL_ERROR",
    "OUTPUT_WRITE_FAILED",
    "RENDER_UNAVAILABLE",
    "SCHEMA_UNAVAILABLE",
    "SCHEMA_VALIDATOR_UNAVAILABLE",
    "SURFACE_PARITY_DRIFT",
    "SURFACE_UNREADABLE",
    "WORKFLOW_INTEGRATION_DISPLACED",
    "WORKFLOW_INTEGRATION_MISSING",
)
V2_VERIFY_OPTION = "--verify-inserted-record"
V2_ZERO_DIGEST = "0" * 64
V2_MESSAGE_LIMIT = 1900
V2_PATH_REPORT_LIMIT = 20
V2_LEDGER_LIMIT = 9999

V2_ACCEPTED_OPTIONS = (
    "--repository",
    "--contract",
    "--format",
    "--output-json",
    "--output-markdown",
)
V2_FORMATS = ("bundle",)

# Options that would let a caller author a completion fact. They are refused by
# name, before any derivation, whatever value accompanies them.
V2_CALLER_FACT_OPTIONS = frozenset(
    {
        "--assertion",
        "--assertions",
        "--baseline",
        "--blocked",
        "--bundle-digest",
        "--candidate",
        "--changed-path",
        "--changed-paths",
        "--commit",
        "--commits",
        "--count",
        "--counts",
        "--digest",
        "--exit-code",
        "--exit-codes",
        "--failed",
        "--file-list",
        "--gitlink",
        "--gitlinks",
        "--inventory",
        "--junit",
        "--ledger",
        "--not-run",
        "--notrun",
        "--passed",
        "--path",
        "--paths",
        "--planning-candidate",
        "--require-remote",
        "--required",
        "--result",
        "--results",
        "--scenario",
        "--scenario-result",
        "--sha256",
        "--skipped",
        "--status",
        "--story",
        "--submodule",
        "--summary",
        "--test-results",
        "--total",
        "--totals",
        "--verdict",
    }
)

# Every v2 code, its exit class, and the runbook remediation summary. `BLOCKED`
# codes describe an environment that cannot support a trustworthy record;
# everything else is a proven `FAIL`.
V2_CODES = {
    "EVIDENCE_PATH_ESCAPE": "FAIL",
    "EVIDENCE_FILE_MISSING": "FAIL",
    "EVIDENCE_HASH_MISMATCH": "FAIL",
    "EVIDENCE_OUTPUT_FORBIDDEN": "FAIL",
    "SIGNABLE_PAYLOAD_MISMATCH": "FAIL",
    "CHANGED_PATH_SET_MISMATCH": "FAIL",
    "EVIDENCE_BOUNDARY_GITLINK": "FAIL",
    "UNAVAILABLE_HISTORY_PASSED": "FAIL",
    "ASSERTION_LEDGER_EMPTY": "FAIL",
    "ROOT_OF_TRUST_NOT_PINNED": "FAIL",
    "SIGNED_EVIDENCE_ALLOWLISTED": "FAIL",
    "FAULT_NOT_DETECTED": "FAIL",
    "FIXTURE_NOT_RESTORED": "FAIL",
    "TEST_SUPPORT_BOUNDARY_INVALID": "FAIL",
    "PORTABLE_TIER_NONPORTABLE_REFERENCE": "FAIL",
    "RESOLVED_COMPILE_SURFACE_INVALID": "FAIL",
    "ASSERTION_INVENTORY_DRIFT": "FAIL",
    "TIER_PROJECT_MISSING": "FAIL",
    "TIER_NOT_DECLARED": "FAIL",
    "TIER_EXECUTION_INCOMPLETE": "FAIL",
    "TIER_EXECUTION_FAILED": "FAIL",
    "EXECUTED_COUNT_REGRESSION": "FAIL",
    "TIERING_INPUT_INVALID": "FAIL",
    "TIERING_INPUT_MISSING": "FAIL",
    "CONFORMANCE_ASSERTION_MISSING": "FAIL",
    "CONFORMANCE_ASSERTION_DUPLICATE": "FAIL",
    "CONFORMANCE_ASSERTION_RENAMED": "FAIL",
    "CONFORMANCE_ASSERTION_UNKNOWN": "FAIL",
    "CONFORMANCE_ASSERTION_SOURCE_DRIFT": "FAIL",
    "CONFORMANCE_DISCOVERY_UNSUPPORTED": "FAIL",
    "ASSERTION_STRENGTH_WEAKENED": "FAIL",
    "TIER_UNASSIGNED": "FAIL",
    "TIER_REASON_MISSING": "FAIL",
    "TIER_APPROVAL_MISSING": "FAIL",
    "FR20_DENOMINATOR_DRIFT": "FAIL",
    "PUBLIC_CONTRACT_WIDENED": "FAIL",
    "V1_ARTIFACT_DRIFT": "FAIL",
    "TIERING_AUTHORITY_INVALID": "FAIL",
    "TIERING_PRE_SPLIT_RESULT_INVALID": "FAIL",
    "TIERING_SCHEMA_INVALID": "FAIL",
    "TIERING_RENDER_DRIFT": "FAIL",
    "TIERING_PRE_SPLIT_RESULT_MISSING": "BLOCKED",
    "TIERING_CAPTURE_FAILED": "BLOCKED",
    "TIERING_ENVIRONMENT_UNAVAILABLE": "BLOCKED",
    "UX_DECISION_MISSING": "FAIL",
    "UX_DECISION_DUPLICATE": "FAIL",
    "UX_DECISION_UNKNOWN": "FAIL",
    "UX_ACCEPTANCE_MISSING": "FAIL",
    "UX_ACCEPTANCE_DUPLICATE": "FAIL",
    "UX_ACCEPTANCE_UNKNOWN": "FAIL",
    "UX_OWNER_MISSING": "FAIL",
    "UX_HASH_MISSING": "FAIL",
    "UX_ORDER_DRIFT": "FAIL",
    "UX_SOURCE_UNBOUND": "FAIL",
    "UX_SOURCE_DRIFT": "FAIL",
    "UX_DECISION_INVENTORY_DRIFT": "FAIL",
    "UX_ACCEPTANCE_INVENTORY_DRIFT": "FAIL",
    "UX_ACTIVATION_UNAUTHORIZED": "FAIL",
    "UX_CURRENT_STORY_INVALID": "FAIL",
    "UX_PRODUCTION_CHANGE_FORBIDDEN": "FAIL",
    "UX_SCHEMA_INVALID": "FAIL",
    "UX_RENDER_DRIFT": "FAIL",
    "HISTORICAL_BLOB_UNRESOLVED": "FAIL",
    "HISTORICAL_RECORD_DRIFT": "FAIL",
    "FAULT_NOT_DETECTED": "FAIL",
    "FIXTURE_NOT_RESTORED": "FAIL",
    "ARGUMENT_INVALID": "FAIL",
    "CALLER_AUTHORED_FACT": "FAIL",
    "INPUT_SCHEMA_INVALID": "FAIL",
    "RECORD_NOT_DERIVED": "FAIL",
    "ASSERTION_LEDGER_EMPTY": "FAIL",
    "AUTHORITY_BINDING_INVALID": "FAIL",
    "GITLINK_INVENTORY_DRIFT": "FAIL",
    "GITLINK_SCOPE_MISMATCH": "FAIL",
    "GITLINK_DRIFT": "FAIL",
    "WORKTREE_NOT_CLEAN": "FAIL",
    "SOURCE_TREE_DIRTY": "FAIL",
    "FILE_LIST_DRIFT": "FAIL",
    "SUBMODULE_INTERNAL_PATH": "FAIL",
    "BASELINE_NOT_TRUSTWORTHY": "FAIL",
    "CANDIDATE_NOT_FINAL": "FAIL",
    "SCENARIO_COMMAND_UNSUPPORTED": "FAIL",
    "SCENARIO_RESULT_MISMATCH": "FAIL",
    "TEST_RESULTS_MISSING": "FAIL",
    "TEST_RESULTS_STALE": "FAIL",
    "TEST_FAILED": "FAIL",
    "TEST_SKIPPED": "FAIL",
    "TEST_NOT_RUN": "FAIL",
    "TEST_RESULTS_FAILED": "FAIL",
    "TEST_SKIP_NOT_ALLOWED": "FAIL",
    "TEST_COUNT_INCONSISTENT": "FAIL",
    "OUTPUT_PATH_INVALID": "FAIL",
    "OUTPUT_SCHEMA_INVALID": "FAIL",
    "RECORD_CONTENT_DRIFT": "FAIL",
    "WORKFLOW_INTEGRATION_MISSING": "FAIL",
    "WORKFLOW_INTEGRATION_DISPLACED": "FAIL",
    "SURFACE_PARITY_DRIFT": "FAIL",
    "CONTRACT_UNSUPPORTED": "BLOCKED",
    "CANDIDATE_UNRESOLVABLE": "BLOCKED",
    "SURFACE_UNREADABLE": "BLOCKED",
    "RENDER_UNAVAILABLE": "BLOCKED",
    "GIT_UNAVAILABLE": "BLOCKED",
    "GIT_COMMAND_FAILED": "BLOCKED",
    "SCHEMA_UNAVAILABLE": "BLOCKED",
    "SCHEMA_VALIDATOR_UNAVAILABLE": "BLOCKED",
    "OUTPUT_WRITE_FAILED": "BLOCKED",
    "INTERNAL_ERROR": "BLOCKED",
}

V2_JUNIT_SUITE_CHILDREN = ("testcase", "properties", "system-out", "system-err")
V2_JUNIT_NON_PASS_CHILDREN = ("failure", "error", "skipped")
V2_SIMPLE_SELECTOR = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")


class V2Stop(Exception):
    """Stop the v2 route and emit a schema-valid failure document."""

    def __init__(self, findings: list[dict[str, str]], story_id: str | None = None) -> None:
        super().__init__("v2 generation stopped")
        self.findings = findings
        self.story_id = story_id


def v2_clean_text(value: str, limit: int = V2_MESSAGE_LIMIT) -> str:
    """Generator-authored text only: no control characters, no lone surrogates."""
    text = value.encode("utf-8", errors="backslashreplace").decode("utf-8")
    text = "".join("?" if ord(character) < 0x20 else character for character in text)
    if len(text) > limit:
        text = text[: limit - 3] + "..."
    return text or "?"


def v2_finding(code: str, subject: str, message: str) -> dict[str, str]:
    if code not in V2_CODES:  # pragma: no cover - programming error guard
        raise ValueError(f"undocumented v2 code: {code}")
    return {
        "code": code,
        "subject": v2_clean_text(subject, 400),
        "message": v2_clean_text(message),
    }


def v2_path_summary(paths: Sequence[str]) -> str:
    shown = [v2_clean_text(path, 200) for path in list(paths)[:V2_PATH_REPORT_LIMIT]]
    remainder = len(paths) - len(shown)
    suffix = f" (and {remainder} more)" if remainder > 0 else ""
    return ", ".join(shown) + suffix


def v2_sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def v2_parse_arguments(raw_arguments: Sequence[str]) -> dict[str, str]:
    """Parse the exact v2 option set without argparse prefix matching.

    Caller-fact options are reported by their canonical name; no other caller
    value is ever echoed into the failure document.
    """
    findings: list[dict[str, str]] = []
    values: dict[str, str] = {}
    tokens = list(raw_arguments)
    verify_mode = any(token.partition("=")[0] == V2_VERIFY_OPTION for token in tokens)
    historical_mode = any(token.partition("=")[0] == "--historical" for token in tokens)
    accepted_options = (
        ("--repository", "--contract", V2_VERIFY_OPTION)
        if verify_mode
        else (("--repository", "--contract", "--historical", "--format", "--output-json")
              if historical_mode else V2_ACCEPTED_OPTIONS)
    )
    index = 0
    while index < len(tokens):
        token = tokens[index]
        index += 1
        if not token.startswith("--") or token == "--":
            findings.append(
                v2_finding(
                    "ARGUMENT_INVALID",
                    "argv",
                    "positional arguments and short options are not accepted by the v2 route",
                )
            )
            continue
        name, separator, inline_value = token.partition("=")
        takes_following = (
            not separator
            and index < len(tokens)
            and not tokens[index].startswith("--")
        )
        if name in V2_CALLER_FACT_OPTIONS:
            findings.append(
                v2_finding(
                    "CALLER_AUTHORED_FACT",
                    name,
                    f"{name} would supply a caller-authored completion fact; the v2 route "
                    "derives every count, path, commit, gitlink, exit, and verdict itself",
                )
            )
            if takes_following:
                index += 1
            continue
        if name not in accepted_options:
            findings.append(
                v2_finding(
                    "ARGUMENT_INVALID",
                    "argv",
                    "an unrecognized option was supplied; the v2 route accepts exactly "
                    + ", ".join(accepted_options),
                )
            )
            if takes_following:
                index += 1
            continue
        if name == "--historical":
            if separator or name in values:
                findings.append(v2_finding("ARGUMENT_INVALID", name,
                                           "--historical is a flag and may appear only once"))
            values[name] = "true"
            continue
        if separator:
            value = inline_value
        elif takes_following:
            value = tokens[index]
            index += 1
        else:
            findings.append(v2_finding("ARGUMENT_INVALID", name, f"{name} requires a value"))
            continue
        if name in values:
            findings.append(
                v2_finding("ARGUMENT_INVALID", name, f"{name} may appear only once")
            )
            continue
        if not value:
            findings.append(
                v2_finding("ARGUMENT_INVALID", name, f"{name} requires a non-empty value")
            )
            continue
        values[name] = value

    if verify_mode:
        # Verification mode reads a committed pair; it derives and writes nothing.
        for name in ("--format", "--output-json", "--output-markdown"):
            if name in values:
                findings.append(
                    v2_finding(
                        "ARGUMENT_INVALID",
                        name,
                        f"{V2_VERIFY_OPTION} accepts exactly --repository, --contract, and "
                        f"{V2_VERIFY_OPTION}; the pair paths come from the contract",
                    )
                )
        if "--contract" not in values and not any(
            item["subject"] == "--contract" for item in findings
        ):
            findings.append(v2_finding("ARGUMENT_INVALID", "--contract", "--contract is required"))
        if findings:
            raise V2Stop(findings)
        return values

    for required in (("--contract", "--output-json") if historical_mode
                     else ("--contract", "--output-json", "--output-markdown")):
        if required not in values and not any(
            item["subject"] == required for item in findings
        ):
            findings.append(
                v2_finding("ARGUMENT_INVALID", required, f"{required} is required")
            )
    output_format = values.get("--format", "json" if historical_mode else "bundle")
    if output_format not in (("json",) if historical_mode else V2_FORMATS):
        findings.append(
            v2_finding(
                "ARGUMENT_INVALID",
                "--format",
                "historical mode requires --format json" if historical_mode
                else "the v2 route supports only --format bundle",
            )
        )
    if findings:
        raise V2Stop(findings)
    values.setdefault("--format", "json" if historical_mode else "bundle")
    return values


def v2_load_schemas() -> tuple[Any, dict[str, Any]]:
    try:
        import jsonschema  # noqa: PLC0415 - optional for the v1 route only
    except ImportError as error:
        raise V2Stop(
            [
                v2_finding(
                    "SCHEMA_VALIDATOR_UNAVAILABLE",
                    "jsonschema",
                    "the jsonschema package is unavailable; run through the pinned "
                    "`uv run --frozen --no-sync` environment",
                )
            ]
        ) from error
    validators: dict[str, Any] = {}
    for role, name in V2_SCHEMA_FILES.items():
        path = V2_SCHEMA_DIRECTORY / name
        try:
            schema = json.loads(path.read_text(encoding="utf-8"))
            jsonschema.Draft202012Validator.check_schema(schema)
        except (OSError, ValueError, jsonschema.SchemaError) as error:
            raise V2Stop(
                [
                    v2_finding(
                        "SCHEMA_UNAVAILABLE",
                        f"_bmad/schemas/{name}",
                        "a tooling schema is missing, unreadable, or not a valid "
                        "draft 2020-12 schema",
                    )
                ]
            ) from error
        validators[role] = jsonschema.Draft202012Validator(schema)
    return jsonschema, validators


def v2_schema_errors(validator: Any, instance: Any) -> list[str]:
    """Stable, payload-free error locations for one schema validation."""
    locations = []
    for error in sorted(
        validator.iter_errors(instance), key=lambda item: list(map(str, item.absolute_path))
    ):
        location = "/".join(str(part) for part in error.absolute_path) or "(root)"
        locations.append(f"{location}: {error.validator}")
    return locations


def v2_reject_constant(value: str) -> NoReturn:
    raise ValueError("non-finite JSON number")


def v2_reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    document: dict[str, Any] = {}
    for key, value in pairs:
        if key in document:
            raise ValueError("duplicate JSON object key")
        document[key] = value
    return document


def v2_parse_json(content: bytes) -> Any:
    return json.loads(
        content.decode("utf-8"),
        object_pairs_hook=v2_reject_duplicate_keys,
        parse_constant=v2_reject_constant,
    )


def v2_committed_blob(repository: Path, candidate: str, path: str) -> bytes | None:
    """Read one regular-file blob from the candidate tree; None when absent."""
    mode, object_id = tree_entry(repository, candidate, path)
    if mode not in ("100644", "100755") or object_id is None:
        return None
    return run_git(repository, "cat-file", "blob", object_id).stdout


def v2_raw_gitlinks(repository: Path, candidate: str) -> list[tuple[str, str]]:
    """Every mode-160000 entry of the candidate tree, read from its own mode column.

    `ls-tree -r` recurses into trees only; it never enters a submodule.
    """
    result = run_git(repository, "ls-tree", "-r", "-z", "--full-tree", candidate)
    gitlinks: list[tuple[str, str]] = []
    for record in result.stdout.split(b"\0"):
        if not record:
            continue
        metadata, separator, encoded_path = record.partition(b"\t")
        fields = decode(metadata).split()
        if not separator or len(fields) != 3:
            raise GateError("GIT_COMMAND_FAILED", "could not parse git ls-tree output")
        mode, _, object_id = fields
        if mode == "160000":
            gitlinks.append((decode(encoded_path), object_id))
    return sorted(gitlinks)


def v2_below(path: str, roots: Sequence[str]) -> bool:
    return any(path == root or path.startswith(root + "/") for root in roots)


def v2_validate_contract(
    content: bytes | None, contract_path: str, validator: Any, v14_validator: Any = None
) -> dict[str, Any]:
    if content is None:
        raise V2Stop(
            [
                v2_finding(
                    "ARGUMENT_INVALID",
                    "--contract",
                    "--contract does not name a regular file committed at the candidate",
                )
            ]
        )
    try:
        contract = v2_parse_json(content)
    except (UnicodeDecodeError, ValueError):
        raise V2Stop(
            [
                v2_finding(
                    "INPUT_SCHEMA_INVALID",
                    contract_path,
                    "the story contract is not well-formed UTF-8 JSON without duplicate keys",
                )
            ]
        ) from None
    schema_version = contract.get("schemaVersion") if isinstance(contract, dict) else None
    if schema_version not in (V2_CONTRACT_SCHEMA_VERSION, V2_V14_CONTRACT_SCHEMA_VERSION):
        raise V2Stop(
            [
                v2_finding(
                    "INPUT_SCHEMA_INVALID",
                    contract_path,
                    "the story contract does not carry a known schema identity",
                )
            ]
        )
    selected_validator = v14_validator if schema_version == V2_V14_CONTRACT_SCHEMA_VERSION else validator
    if selected_validator is None:
        raise V2Stop([v2_finding("SCHEMA_UNAVAILABLE", contract_path,
                                 "the V14 contract schema validator is unavailable")])
    errors = v2_schema_errors(selected_validator, contract)
    if errors:
        raise V2Stop(
            [
                v2_finding(
                    "INPUT_SCHEMA_INVALID",
                    contract_path,
                    "the story contract violates its schema at " + "; ".join(errors[:10]),
                )
            ]
        )

    story_id = contract["storyId"]
    findings: list[dict[str, str]] = []
    identifiers = [scenario["id"] for scenario in contract["scenarios"]]
    if len(identifiers) != len(set(identifiers)):
        findings.append(
            v2_finding("INPUT_SCHEMA_INVALID", contract_path, "scenario identifiers repeat")
        )
    foreign = [item for item in identifiers if not item.startswith(f"AC-{story_id}-")]
    if foreign:
        findings.append(
            v2_finding(
                "INPUT_SCHEMA_INVALID",
                contract_path,
                "scenario identifiers do not belong to story "
                f"{story_id}: {v2_path_summary(foreign)}",
            )
        )
    expected_summary = {
        "required": len(identifiers),
        "passed": len(identifiers),
        "failed": 0,
        "blocked": 0,
        "skipped": 0,
        "notRun": 0,
    }
    if contract["finalRecord"]["summary"] != expected_summary:
        findings.append(
            v2_finding(
                "INPUT_SCHEMA_INVALID",
                contract_path,
                "finalRecord.summary must require and pass every declared scenario",
            )
        )
    json_path, markdown_path = contract["finalRecord"]["paths"]
    try:
        safe_relative_path(json_path)
        safe_relative_path(markdown_path)
        valid_paths = json_path.endswith(".json") and markdown_path.endswith(".md")
    except GateError:
        valid_paths = False
    if not valid_paths:
        findings.append(
            v2_finding(
                "INPUT_SCHEMA_INVALID",
                contract_path,
                "finalRecord.paths must be normalized repository-relative [JSON, Markdown] paths",
            )
        )
    if findings:
        raise V2Stop(findings, story_id)
    return contract


def v2_authority(
    repository: Path,
    candidate: str,
    contract_path: str,
    contract: dict[str, Any],
    validator: Any,
    findings: list[dict[str, str]],
) -> dict[str, Any] | None:
    """Bind the contract's planning candidate to the recomputed V9 bundle digest."""
    content = v2_committed_blob(repository, candidate, V2_AUTHORITY_BUNDLE_PATH)
    if content is None:
        findings.append(
            v2_finding(
                "AUTHORITY_BINDING_INVALID",
                V2_AUTHORITY_BUNDLE_PATH,
                "the V9 authority bundle is not committed at the candidate",
            )
        )
        return None
    try:
        bundle = v2_parse_json(content)
    except (UnicodeDecodeError, ValueError):
        bundle = None
    if (
        not isinstance(bundle, dict)
        or bundle.get("schemaVersion") != V2_BUNDLE_SCHEMA_VERSION
        or v2_schema_errors(validator, bundle)
    ):
        findings.append(
            v2_finding(
                "AUTHORITY_BINDING_INVALID",
                V2_AUTHORITY_BUNDLE_PATH,
                "the V9 authority bundle is malformed or violates its schema",
            )
        )
        return None
    rows = bundle["artifacts"]
    paths = [row["path"] for row in rows]
    recomputed = v2_sha256(
        "".join(f"{row['sha256']}  {row['path']}\n" for row in rows).encode("utf-8")
    )
    authority = contract["authority"]
    problems = []
    if paths != sorted(set(paths)):
        problems.append("artifact rows are not unique and ordinally sorted")
    if recomputed != bundle["bundleDigest"]:
        problems.append("the declared bundle digest differs from the recomputed row digest")
    if bundle["planningCandidate"] != authority["planningCandidate"]:
        problems.append("the bundle planning candidate differs from the contract's")
    if int(contract["storyId"].split(".")[0]) >= 8:
        contract_bytes = v2_committed_blob(repository, candidate, contract_path)
        contract_rows = [row for row in rows if row["path"] == contract_path]
        if (contract_bytes is None or len(contract_rows) != 1
                or contract_rows[0]["sha256"] != v2_sha256(contract_bytes)):
            problems.append("the committed contract bytes differ from the authority bundle row")
    if problems:
        findings.append(
            v2_finding(
                "AUTHORITY_BINDING_INVALID", V2_AUTHORITY_BUNDLE_PATH, "; ".join(problems)
            )
        )
        return None
    return {
        "epic": authority["epic"],
        "architecture": authority["architecture"],
        "planningCandidate": authority["planningCandidate"],
        "bundleDigest": recomputed,
    }


def v2_gitlinks(
    repository: Path, candidate: str, findings: list[dict[str, str]], story_id: str = "7.1"
) -> list[dict[str, str]]:
    """Raw root gitlinks, required to equal the ordinal root `.gitmodules` inventory."""
    try:
        declared = root_submodule_paths(repository, candidate)
    except GateError as error:
        if error.code != "INVALID_SCOPE":
            raise
        findings.append(
            v2_finding(
                "GITLINK_SCOPE_MISMATCH" if story_id == "7.2" else "GITLINK_INVENTORY_DRIFT",
                ".gitmodules",
                f"the candidate .gitmodules has invalid root declarations: {error.message}",
            )
        )
        return []
    if story_id == "7.2" and v2_committed_blob(repository, candidate, ".gitmodules") is not None:
        raw_declarations = run_git(
            repository, "config", "--null", "--blob", f"{candidate}:.gitmodules",
            "--get-regexp", r"^submodule\..*\.path$", allowed_returncodes=(0, 1)
        )
        if raw_declarations.returncode == 0:
            values = [decode(item).split("\n", 1)[-1]
                      for item in raw_declarations.stdout.split(b"\0") if item]
            if len(values) != len(set(values)):
                findings.append(v2_finding("GITLINK_SCOPE_MISMATCH", ".gitmodules",
                                           "root .gitmodules repeats a submodule path"))
                return []
    raw = v2_raw_gitlinks(repository, candidate)
    raw_paths = [path for path, _ in raw]
    if not raw_paths and not declared:
        findings.append(
            v2_finding(
                "RECORD_NOT_DERIVED",
                "candidate.gitlinks",
                "the candidate tree yields no mode-160000 root gitlink path to bind",
            )
        )
        return []
    missing = sorted(set(declared) - set(raw_paths))
    extra = sorted(set(raw_paths) - set(declared))
    if missing or extra or len(raw_paths) != len(set(raw_paths)):
        details = []
        if missing:
            details.append(f"declared without a raw gitlink: {v2_path_summary(missing)}")
        if extra:
            details.append(f"raw gitlink not declared: {v2_path_summary(extra)}")
        if not details:
            details.append("a raw gitlink path repeats")
        findings.append(
            v2_finding(
                "GITLINK_SCOPE_MISMATCH" if story_id == "7.2" else "GITLINK_INVENTORY_DRIFT",
                "candidate.gitlinks", "; ".join(details)
            )
        )
        return []
    return [{"path": path, "commit": commit, "mode": "160000"} for path, commit in raw]


def v2_pytest_command(tokens: list[str]) -> dict[str, str | None] | None:
    """Recognize `python3 -m pytest -q TARGET [-k SELECTOR] --junitxml=PATH`."""
    if tokens[:3] != ["python3", "-m", "pytest"]:
        return None
    target: str | None = None
    selector: str | None = None
    junit: str | None = None
    index = 3
    while index < len(tokens):
        token = tokens[index]
        index += 1
        if token == "-q":
            continue
        if token == "-k":
            if selector is not None or index >= len(tokens):
                return None
            selector = tokens[index]
            index += 1
            continue
        if token.startswith("--junitxml="):
            if junit is not None:
                return None
            junit = token.partition("=")[2]
            continue
        if token.startswith("-") or target is not None or not token.endswith(".py"):
            return None
        target = token
    if target is None or not junit:
        return None
    return {"target": target, "selector": selector, "junit": junit}


def v2_generator_command(tokens: list[str]) -> dict[str, str] | None:
    """Recognize the self-invocation `python3 _bmad/scripts/generate_story_record.py ...`."""
    if tokens[:2] != ["python3", V2_GENERATOR_PATH]:
        return None
    try:
        options = v2_parse_arguments(tokens[2:])
        return None if "--historical" in options else options
    except V2Stop:
        return None


def v2_acceptance_command(tokens: list[str]) -> dict[str, str] | None:
    """Recognize `python3 SCRIPT --repository R --contract C --scenario ID --output PATH`.

    The command's declared output is one acceptance-result v1 document. Each
    option appears exactly once, in any order; nothing else is accepted.
    """
    if tokens[:2] == ["python3", V2_GENERATOR_PATH] and "--historical" in tokens:
        try:
            options = v2_parse_arguments(tokens[2:])
        except V2Stop:
            return None
        if options.get("--contract") != V2_7_4_CONTRACT_PATH:
            return None
        return {"script": V2_GENERATOR_PATH, "repository": options.get("--repository", "."),
                "contract": options["--contract"], "scenario": "AC-7.4-01",
                "output": options["--output-json"]}
    if (
        len(tokens) != 2 + 2 * len(V2_ACCEPTANCE_OPTIONS)
        or tokens[0] != "python3"
        or not tokens[1].endswith(".py")
        or tokens[1] == V2_GENERATOR_PATH
    ):
        return None
    values: dict[str, str] = {}
    for name, value in zip(tokens[2::2], tokens[3::2]):
        if name not in V2_ACCEPTANCE_OPTIONS or name in values:
            return None
        if not value or value.startswith("-"):
            return None
        values[name] = value
    return {"script": tokens[1], **{name[2:]: value for name, value in values.items()}}


def v2_load_acceptance_validator() -> Any:
    """The tooling's own acceptance-result schema, loaded only for contracts that need it."""
    import jsonschema  # noqa: PLC0415 - v2_load_schemas already proved it importable

    path = V2_SCHEMA_DIRECTORY / V2_ACCEPTANCE_SCHEMA_FILE
    try:
        schema = json.loads(path.read_text(encoding="utf-8"))
        jsonschema.Draft202012Validator.check_schema(schema)
    except (OSError, ValueError, jsonschema.SchemaError) as error:
        raise V2Stop(
            [
                v2_finding(
                    "SCHEMA_UNAVAILABLE",
                    f"_bmad/schemas/{V2_ACCEPTANCE_SCHEMA_FILE}",
                    "a tooling schema is missing, unreadable, or not a valid draft 2020-12 schema",
                )
            ]
        ) from error
    return jsonschema.Draft202012Validator(schema)


def v2_scenario_from_acceptance(
    repository: Path,
    scenario: dict[str, Any],
    command: dict[str, str],
    story_id: str,
    candidate: str,
    candidate_time_ns: int,
    validator: Any,
    stamped: frozenset[str] = frozenset(),
) -> tuple[dict[str, Any], str, list[dict[str, str]], list[dict[str, str]]]:
    """Derive one scenario from its acceptance-result v1 document.

    Returns (scenario record, summary category, findings, bound inputs). The
    result must carry this story, scenario, and exact declared command, a state
    consistent with the contract's `resultSemantics`, and inputs whose digests
    equal the candidate's committed blobs; it must not predate the candidate
    commit. It must name the derived candidate, or one of the `stamped` commits
    on the already-verified lifecycle-only path from a retained candidate to
    `HEAD`, where a rerun after the record-only commit stamps its result.
    """
    scenario_id = scenario["id"]
    output = command["output"]
    semantics = scenario["resultSemantics"]
    findings: list[dict[str, str]] = []
    record: dict[str, Any] = {
        "scenarioId": scenario_id,
        "command": scenario["command"],
        "exitCode": 2,
        "result": "FAIL",
        "blockers": [],
    }

    def stop(
        code: str, message: str, category: str
    ) -> tuple[dict[str, Any], str, list[dict[str, str]], list[dict[str, str]]]:
        findings.append(v2_finding(code, scenario_id, message))
        record["blockers"] = sorted({item["code"] for item in findings})
        return record, category, findings, []

    lexical = repository / output
    try:
        resolved = lexical.resolve(strict=True)
    except FileNotFoundError:
        return stop(
            "TEST_RESULTS_MISSING",
            f"the declared result file {output} does not exist; the scenario was not run",
            "notRun",
        )
    except OSError:
        return stop(
            "INPUT_SCHEMA_INVALID", f"the result file {output} cannot be resolved", "failed"
        )
    try:
        resolved.relative_to(repository)
    except ValueError:
        return stop(
            "INPUT_SCHEMA_INVALID",
            f"the result file {output} resolves outside the repository",
            "failed",
        )
    if lexical.is_symlink() or not resolved.is_file():
        return stop(
            "INPUT_SCHEMA_INVALID", f"the result file {output} is not a regular file", "failed"
        )
    try:
        content, mtime_ns = read_file_snapshot(resolved)
        document = v2_parse_json(content)
    except OSError:
        return stop(
            "INPUT_SCHEMA_INVALID", f"the result file {output} could not be read stably", "failed"
        )
    except (UnicodeDecodeError, ValueError):
        return stop(
            "INPUT_SCHEMA_INVALID",
            f"the result file {output} is not well-formed UTF-8 JSON without duplicate keys",
            "failed",
        )
    if (
        not isinstance(document, dict)
        or document.get("schemaVersion") != V2_ACCEPTANCE_SCHEMA_VERSION
        or v2_schema_errors(validator, document)
    ):
        return stop(
            "INPUT_SCHEMA_INVALID",
            f"the result file {output} is not a schema-valid acceptance-result v1 document",
            "failed",
        )

    record["resultFile"] = {"path": output, "sha256": v2_sha256(content)}
    record["exitCode"] = document["exitCode"]
    rows = document.get("assertionLedger", [])
    if len(rows) > V2_LEDGER_LIMIT or any(
        re.search(r"[\x00-\x1f]", row["subject"]) for row in rows
    ):
        return stop(
            "INPUT_SCHEMA_INVALID",
            f"the result file {output} has more than {V2_LEDGER_LIMIT} ledger rows or a "
            "control character in a ledger subject",
            "failed",
        )
    expected_row_ids = [f"{scenario_id}#{ordinal:04d}" for ordinal in range(1, len(rows) + 1)]
    if [row["id"] for row in rows] != expected_row_ids:
        findings.append(
            v2_finding(
                "SCENARIO_RESULT_MISMATCH",
                scenario_id,
                f"the assertion ledger IDs in {output} are not the exact ordered "
                f"{scenario_id}#<four-digit ordinal> sequence",
            )
        )
    ledger = [
        {"id": f"{scenario_id}#{ordinal:04d}", "subject": row["subject"], "state": row["state"]}
        for ordinal, row in enumerate(rows, start=1)
    ]
    if ledger:
        record["assertionLedger"] = ledger

    category = "passed"
    if (
        document["storyId"] != story_id
        or document["scenarioId"] != scenario_id
        or document["command"] != scenario["command"]
    ):
        findings.append(
            v2_finding(
                "SCENARIO_RESULT_MISMATCH",
                scenario_id,
                f"the result file {output} does not carry this story, scenario, and exact "
                "declared command",
            )
        )
        category = "failed"
    exit_classes = {
        "PASS": semantics["passExitCodes"],
        "FAIL": semantics["failExitCodes"],
        "BLOCKED": semantics["blockedExitCodes"],
    }
    state = document["result"]
    consistent = (
        semantics["notApplicableAllowed"]
        if state == "not-applicable"
        else document["exitCode"] in exit_classes[state]
    )
    if not consistent:
        findings.append(
            v2_finding(
                "SCENARIO_RESULT_MISMATCH",
                scenario_id,
                f"the result state and exit code in {output} disagree with the contract's "
                "resultSemantics",
            )
        )
        category = "failed"
    subjects = [row["subject"] for row in rows]
    if len(subjects) != len(set(subjects)):
        findings.append(
            v2_finding(
                "SCENARIO_RESULT_MISMATCH", scenario_id, f"a ledger subject in {output} repeats"
            )
        )
        category = "failed"
    if state != "PASS" or state != semantics["expected"]:
        propagated_codes = V2_PROPAGATED_ACCEPTANCE_CODES + (
            ("HISTORICAL_BLOB_UNRESOLVED", "HISTORICAL_RECORD_DRIFT", "GIT_COMMAND_FAILED")
            if story_id == "7.4" else ()
        )
        for code in propagated_codes:
            if code in document["blockers"]:
                findings.append(
                    v2_finding(code, scenario_id, f"the acceptance result {output} reports {code}")
                )
        findings.append(
            v2_finding(
                "TEST_RESULTS_FAILED",
                scenario_id,
                f"the acceptance result {output} reports {state} with exit "
                f"{document['exitCode']} and blockers "
                + (", ".join(v2_clean_text(code, 60) for code in document["blockers"]) or "none"),
            )
        )
        category = "failed"
    elif document["blockers"] or any(row["state"] != "PASS" for row in rows):
        findings.append(
            v2_finding(
                "TEST_COUNT_INCONSISTENT",
                scenario_id,
                f"the passing result {output} carries a blocker or a non-passing ledger row",
            )
        )
        category = "failed"
    if not rows:
        findings.append(
            v2_finding(
                "ASSERTION_LEDGER_EMPTY",
                scenario_id,
                f"the result file {output} records no assertion",
            )
        )
        category = "failed"

    stale_inputs = []
    for row in document["inputs"]:
        try:
            path = safe_relative_path(row["path"])
            blob = v2_committed_blob(repository, candidate, path)
        except GateError as error:
            if error.code in ("GIT_UNAVAILABLE", "GIT_COMMAND_FAILED"):
                return stop(
                    error.code,
                    "git failed while reading an acceptance input",
                    "blocked",
                )
            blob = None
        if blob is None or v2_sha256(blob) != row["sha256"]:
            stale_inputs.append(row["path"])
    known_candidate = document["candidate"] == candidate or document["candidate"] in stamped
    if not known_candidate or mtime_ns < candidate_time_ns or stale_inputs:
        detail = (
            f"inputs differ from the candidate: {v2_path_summary(stale_inputs)}"
            if stale_inputs
            else "it was produced for another candidate or predates the candidate commit"
        )
        findings.append(
            v2_finding(
                "TEST_RESULTS_STALE", scenario_id, f"the result file {output} is stale: {detail}"
            )
        )
        category = "failed"

    passing = (
        not findings
        and document["exitCode"] in semantics["passExitCodes"]
        and state == "PASS"
    )
    if passing:
        record["result"] = "PASS"
    record["blockers"] = sorted({item["code"] for item in findings})
    return record, category, findings, list(document["inputs"])


def v2_parse_junit(content: bytes) -> dict[str, Any]:
    """Parse the single direct pytest suite of one JUnit XML file.

    Raises ValueError for anything that is not exactly one direct `testsuite`
    under a `testsuites` root.
    """
    if b"<!DOCTYPE" in content or b"<!ENTITY" in content:
        raise ValueError("JUnit XML must not declare a document type or entities")
    root = ElementTree.fromstring(content)
    if root.tag != "testsuites":
        raise ValueError("JUnit XML root must be testsuites")
    suites = list(root)
    if len(suites) != 1 or suites[0].tag != "testsuite":
        raise ValueError("JUnit XML must contain exactly one direct testsuite")
    suite = suites[0]
    unexpected = [child.tag for child in suite if child.tag not in V2_JUNIT_SUITE_CHILDREN]
    if unexpected:
        raise ValueError("the testsuite contains a nested suite or unknown element")
    reported = {}
    for name in ("tests", "failures", "errors", "skipped"):
        raw = suite.get(name)
        if raw is None or re.fullmatch(r"[0-9]+", raw) is None:
            raise ValueError(f"testsuite has no integer {name} attribute")
        reported[name] = int(raw)
    cases = []
    for element in suite.findall("testcase"):
        classname = element.get("classname") or ""
        name = element.get("name") or ""
        if not classname or not name:
            raise ValueError("a testcase has no classname or name")
        children = {child.tag for child in element}
        cases.append(
            {
                "classname": classname,
                "name": name,
                "failure": "failure" in children,
                "error": "error" in children,
                "skipped": "skipped" in children,
                "properties": [(prop.get("name") or "", prop.get("value") or "")
                               for group in element.findall("properties")
                               for prop in group.findall("property")],
            }
        )
    return {"reported": reported, "cases": cases}


def v2_scenario_from_results(
    repository: Path,
    scenario: dict[str, Any],
    command: dict[str, str | None],
    candidate_time_ns: int,
    *, snapshot: tuple[bytes, int] | None = None,
) -> tuple[dict[str, Any], str, list[dict[str, str]]]:
    """Derive one pytest scenario from its measured JUnit file.

    Returns (scenario record, summary category, findings). The category is one
    of passed, failed, skipped, notRun.
    """
    scenario_id = scenario["id"]
    junit = str(command["junit"])
    findings: list[dict[str, str]] = []
    record: dict[str, Any] = {
        "scenarioId": scenario_id,
        "command": scenario["command"],
        "exitCode": 0,
        "result": "FAIL",
        "blockers": [],
    }

    def fail(code: str, message: str, category: str) -> tuple[dict[str, Any], str, list]:
        findings.append(v2_finding(code, scenario_id, message))
        record["blockers"] = sorted({item["code"] for item in findings})
        return record, category, findings

    if snapshot is not None:
        content, mtime_ns = snapshot
    else:
        lexical = repository / junit
        try:
            resolved = lexical.resolve(strict=True)
        except FileNotFoundError:
            record["exitCode"] = 5
            return fail(
                "TEST_RESULTS_MISSING",
                f"the declared result file {junit} does not exist; the scenario was not run",
                "notRun",
            )
        except OSError:
            return fail(
                "INPUT_SCHEMA_INVALID", f"the result file {junit} cannot be resolved", "failed"
            )
        try:
            resolved.relative_to(repository)
        except ValueError:
            return fail(
                "INPUT_SCHEMA_INVALID",
                f"the result file {junit} resolves outside the repository",
                "failed",
            )
        if not resolved.is_file():
            return fail("INPUT_SCHEMA_INVALID", f"the result file {junit} is not a file", "failed")
        try:
            content, mtime_ns = read_file_snapshot(resolved)
        except OSError:
            return fail(
                "INPUT_SCHEMA_INVALID", f"the result file {junit} could not be read stably", "failed"
            )
    try:
        parsed = v2_parse_junit(content)
    except (ValueError, ElementTree.ParseError):
        return fail(
            "INPUT_SCHEMA_INVALID",
            f"the result file {junit} is not a single-suite pytest JUnit XML document",
            "failed",
        )

    cases = parsed["cases"]
    reported = parsed["reported"]
    counted = {
        "tests": len(cases),
        "failures": sum(1 for case in cases if case["failure"]),
        "errors": sum(1 for case in cases if case["error"]),
        "skipped": sum(1 for case in cases if case["skipped"]),
    }
    record["resultFile"] = {"path": junit, "sha256": v2_sha256(content)}
    if not cases:
        record["exitCode"] = 5
    elif counted["failures"] or counted["errors"]:
        record["exitCode"] = 1
    if len(cases) > V2_LEDGER_LIMIT:
        return fail(
            "INPUT_SCHEMA_INVALID",
            f"the result file {junit} exceeds {V2_LEDGER_LIMIT} testcases",
            "failed",
        )
    ledger = []
    for ordinal, case in enumerate(cases, start=1):
        passing = not (case["failure"] or case["error"] or case["skipped"])
        ledger.append(
            {
                "id": f"{scenario_id}#{ordinal:04d}",
                "subject": f"{case['classname']}::{case['name']}",
                "state": "PASS" if passing else "FAIL",
            }
        )
    if ledger:
        record["assertionLedger"] = ledger

    category = "passed"
    if counted != reported:
        findings.append(
            v2_finding(
                "TEST_COUNT_INCONSISTENT",
                scenario_id,
                f"the testsuite counters in {junit} disagree with the testcases it contains",
            )
        )
        category = "failed"
    if not cases:
        findings.append(
            v2_finding(
                "ASSERTION_LEDGER_EMPTY",
                scenario_id,
                f"the result file {junit} records no executed testcase",
            )
        )
        category = "failed"
    if counted["failures"] or counted["errors"]:
        findings.append(
            v2_finding(
                "TEST_RESULTS_FAILED",
                scenario_id,
                f"{counted['failures'] + counted['errors']} testcase(s) in {junit} failed or errored",
            )
        )
        category = "failed"
    if counted["skipped"]:
        findings.append(
            v2_finding(
                "TEST_SKIP_NOT_ALLOWED",
                scenario_id,
                f"{counted['skipped']} testcase(s) in {junit} were skipped; v2 allows no skip",
            )
        )
        if category == "passed":
            category = "skipped"

    module = str(command["target"])[: -len(".py")].replace("/", ".")
    selector = command["selector"]
    foreign = [
        case
        for case in cases
        if not (case["classname"] == module or case["classname"].startswith(module + "."))
        or (
            selector is not None
            and V2_SIMPLE_SELECTOR.fullmatch(selector) is not None
            and selector not in case["name"]
        )
    ]
    subjects = [entry["subject"] for entry in ledger]
    if foreign or len(subjects) != len(set(subjects)):
        findings.append(
            v2_finding(
                "SCENARIO_RESULT_MISMATCH",
                scenario_id,
                f"the testcases in {junit} do not all belong to the scenario's target "
                "and selector, or a testcase identity repeats",
            )
        )
        category = "failed"
    if mtime_ns < candidate_time_ns:
        findings.append(
            v2_finding(
                "TEST_RESULTS_STALE",
                scenario_id,
                f"the result file {junit} predates the candidate commit",
            )
        )
        category = "failed"

    passing = (
        not findings
        and record["exitCode"] in scenario["resultSemantics"]["passExitCodes"]
    )
    if passing:
        record["result"] = "PASS"
    else:
        if not findings:
            findings.append(
                v2_finding(
                    "TEST_RESULTS_FAILED",
                    scenario_id,
                    "the derived exit code is not a declared passing exit code",
                )
            )
        category = "failed" if category == "passed" else category
    record["blockers"] = sorted({item["code"] for item in findings})
    return record, category, findings


def v2_render_json(record: dict[str, Any]) -> bytes:
    return (json.dumps(record, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def v2_zero_digests(record: dict[str, Any]) -> dict[str, Any]:
    draft = json.loads(json.dumps(record))
    draft["outputs"]["json"]["sha256"] = V2_ZERO_DIGEST
    draft["outputs"]["markdown"]["sha256"] = V2_ZERO_DIGEST
    draft["renderedMarkdownSha256"] = V2_ZERO_DIGEST
    return draft


def v2_render_markdown(record: dict[str, Any], json_digest: str) -> str:
    """Deterministic UTF-8/LF projection of a digest-free record draft."""
    code = markdown_table_code
    sources = (
        "candidate, measured JUnit results, and acceptance results"
        if "workflowIntegration" in record or "historicalVerification" in record
        else ("candidate and measured scenario results"
              if int(record["storyId"].split(".")[0]) >= 8
              else "candidate and measured JUnit results")
    )
    lines = [
        f"# Story {record['storyId']} Final Record",
        "",
        "<!-- hexalith.conversations.story-final-record.v2 markdown projection -->",
        "",
        "Generated by `_bmad/scripts/generate_story_record.py` from the committed "
        f"{sources}. The JSON record is authoritative; "
        "this rendering is bound to it by digest.",
        "",
        f"- Schema: {markdown_code(record['schemaVersion'])}",
        "- Result: `PASS`",
        f"- Story: {markdown_code(record['storyId'])}",
        f"- Candidate: {markdown_code(record['candidate']['commit'])}",
        "- JSON content SHA-256 (all three digest fields zeroed): "
        f"{markdown_code(json_digest)}",
        "",
        "## Authority",
        "",
        "| Field | Value |",
        "| --- | --- |",
        f"| Epic | {code(record['authority']['epic'])} |",
        f"| Architecture | {code(record['authority']['architecture'])} |",
        f"| Planning candidate | {code(record['authority']['planningCandidate'])} |",
        f"| Bundle digest | {code(record['authority']['bundleDigest'])} |",
        "",
        "## Root gitlinks",
        "",
        "| Path | Mode | Commit |",
        "| --- | --- | --- |",
    ]
    for gitlink in record["candidate"]["gitlinks"]:
        lines.append(
            f"| {code(gitlink['path'])} | {code(gitlink['mode'])} | {code(gitlink['commit'])} |"
        )
    lines.extend(
        [
            "",
            "## Inventory",
            "",
            "| Inventory | SHA-256 |",
            "| --- | --- |",
            f"| {code(record['inventory']['id'])} | {code(record['inventory']['sha256'])} |",
            "",
            "## Predecessors",
            "",
        ]
    )
    lines.extend(f"- {markdown_code(item)}" for item in record["predecessors"])
    lines.extend(
        [
            "",
            "## Scenarios",
            "",
            "| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |",
            "| --- | --- | --- | --- | --- | --- | --- |",
        ]
    )
    for scenario in record["scenarios"]:
        result_file = scenario.get("resultFile")
        blockers = ", ".join(scenario["blockers"]) or "none"
        lines.append(
            f"| {code(scenario['scenarioId'])} | {code(scenario['exitCode'])} "
            f"| {code(scenario['result'])} | {code(blockers)} "
            f"| {code(len(scenario.get('assertionLedger', [])))} "
            f"| {code(result_file['path']) if result_file else 'none'} "
            f"| {code(result_file['sha256']) if result_file else 'none'} |"
        )
    for scenario in record["scenarios"]:
        lines.extend(
            [
                "",
                f"### {markdown_code(scenario['scenarioId'])}",
                "",
                f"Command: {markdown_code(scenario['command'])}",
                "",
            ]
        )
        if scenario.get("outputFiles"):
            lines.extend(["| Bound output | SHA-256 |", "| --- | --- |"])
            lines.extend(f"| {code(row['path'])} | {code(row['sha256'])} |"
                         for row in scenario["outputFiles"])
            lines.append("")
        lines.extend(["| Assertion | Subject | State |", "| --- | --- | --- |"])
        for entry in scenario.get("assertionLedger", []):
            lines.append(
                f"| {code(entry['id'])} | {code(entry['subject'])} | {code(entry['state'])} |"
            )
    if "measurements" in record:
        measured = record["measurements"]
        lines.extend([
            "", "## Story 7.2 measurements", "",
            f"- Baseline: {code(measured['baseline'])}",
            f"- Predecessor record: {code(measured['predecessorRecord']['path'])}",
            f"- Predecessor SHA-256: {code(measured['predecessorRecord']['sha256'])}",
            "", "### Root test projects", "",
            "| Project | Result file | Total | Executed | Passed | Failed | Skipped |",
            "| --- | --- | --- | --- | --- | --- | --- |",
        ])
        for project in measured["testProjects"]:
            counts = project["counts"]
            lines.append("| " + " | ".join(code(value) for value in (
                project["project"], project["resultFile"]["path"],
                counts["total"], counts["executed"], counts["passed"],
                counts["failed"], counts["skipped"])) + " |")
        totals = measured["testTotals"]
        lines.append("| " + " | ".join(code(value) for value in (
            "TOTAL", "none", totals["total"], totals["executed"], totals["passed"],
            totals["failed"], totals["skipped"])) + " |")
        lines.extend(["", "### Exact changed paths", ""])
        lines.extend(f"- {code(path)}" for path in measured["changedPaths"])
    if "workflowIntegration" in record:
        integration = record["workflowIntegration"]
        lines.extend([
            "", "## Story 7.3 workflow integration", "",
            f"- Story contract: {code(integration['contract']['path'])}",
            f"- Story contract SHA-256: {code(integration['contract']['sha256'])}",
            "", "### Governed workflow bodies", "",
            "| Path | SHA-256 |",
            "| --- | --- |",
        ])
        lines.extend(
            f"| {code(body['path'])} | {code(body['sha256'])} |"
            for body in integration["workflowBodies"]
        )
        lines.extend([
            "", "### Predecessor records", "",
            "| Story | Record | SHA-256 |",
            "| --- | --- | --- |",
        ])
        lines.extend(
            f"| {code(item['storyId'])} | {code(item['path'])} | {code(item['sha256'])} |"
            for item in integration["predecessorRecords"]
        )
    if "historicalVerification" in record:
        history = record["historicalVerification"]
        lines.extend(["", "## Story 7.4 historical verification", "",
                      f"- Contract SHA-256: {code(history['contract']['sha256'])}",
                      f"- Historical fixture: {code(history['fixture']['path'])}",
                      f"- Historical fixture SHA-256: {code(history['fixture']['sha256'])}", "",
                      "| Predecessor | Record | SHA-256 |", "| --- | --- | --- |"])
        lines.extend(f"| {code(row['storyId'])} | {code(row['path'])} | {code(row['sha256'])} |"
                     for row in history["predecessorRecords"])
        lines.extend(["", "| Closed story | Classification | Closure | Recorded candidate | Bound blobs "
                      "| Retained warnings |", "| --- | --- | --- | --- | --- | --- |"])
        for row in history["records"]:
            recorded = row["recordedCandidate"]
            lines.append(f"| {code(row['storyId'])} | {code(row['classification'])} "
                         f"| {code(row['closure']['commit'])} "
                         f"| {code(recorded['commit']) if recorded else 'none recorded'} "
                         f"| {code(len(row['boundBlobs']))} "
                         f"| {code(', '.join(row['warnings'])) if row['warnings'] else 'none'} |")
        lines.append("")
        lines.extend(f"- {limit}" for limit in history["limits"])
    if "uxDisposition" in record:
        ux = record["uxDisposition"]
        lines.extend(["", "## Story 8.1 UX disposition", "",
                      f"- Contract: {code(ux['contract']['path'])}",
                      f"- Contract SHA-256: {code(ux['contract']['sha256'])}",
                      f"- Story 7.4 record SHA-256: {code(ux['predecessorRecord']['sha256'])}",
                      f"- Build SourceRevisionId: {code(ux['sourceRevisionId'])}",
                      f"- Test assembly SHA-256: {code(ux['testAssembly']['sha256'])}",
                      "", "| Bound input/output | Path | SHA-256 |", "| --- | --- | --- |"])
        lines.extend(f"| Source | {code(row['path'])} | {code(row['sha256'])} |" for row in ux["sources"])
        lines.extend(f"| {code(role)} | {code(row['path'])} | {code(row['sha256'])} |"
                     for role, row in ux["outputs"].items())
    if "uxValidation" in record:
        ux = record["uxValidation"]
        lines.extend(["", "## Story 8.2 zero-gap UX validation", "",
                      f"- Candidate: {code(ux['sourceRevisionId'])}",
                      f"- Story 8.1 candidate: {code(ux['predecessorCandidate'])}",
                      f"- Story 8.1 record SHA-256: {code(ux['predecessorRecord']['sha256'])}",
                      f"- Inventory SHA-256: {code(ux['inventorySha256'])}",
                      "- Preserved decisions: 52; acceptance criteria: 28; no activation authorized.",
                      "", "| Binding | Path | SHA-256 |", "| --- | --- | --- |"])
        lines.extend(f"| Source | {code(row['path'])} | {code(row['sha256'])} |" for row in ux["sources"])
        lines.extend(f"| {code(role)} | {code(row['path'])} | {code(row['sha256'])} |"
                     for role, row in ux["outputs"].items())
    if "conformanceTiering" in record:
        tiering = record["conformanceTiering"]
        pre_split = tiering["preSplitResult"]
        lines.extend(["", "## Story 9.1 conformance tiering", "",
                      f"- Candidate: {code(tiering['sourceRevisionId'])}",
                      f"- Candidate rule: {markdown_table_text(tiering['bindingRule'])}",
                      f"- Story 7.4 record SHA-256: {code(tiering['predecessorRecord']['sha256'])}",
                      f"- Story 7.4 candidate: {code(tiering['predecessorCandidate'])}",
                      f"- Inventory SHA-256: {code(tiering['inventorySha256'])}",
                      f"- Pre-split freeze commit: {code(pre_split['sourceCommit'])}",
                      f"- Pre-split methods / executed cases: {code(pre_split['discoveredMethods'])} / "
                      f"{code(pre_split['executedTestCases'])}",
                      f"- Assertions: {code(tiering['assertionCount'])}; validation additions: "
                      f"{code(tiering['validationAdditionCount'])}; portable: {code(tiering['tierCounts']['portable'])}; "
                      f"module-internal: {code(tiering['tierCounts']['moduleInternal'])}",
                      f"- Assertion inventory SHA-256: {code(tiering['assertionInventorySha256'])}",
                      f"- Strength inventory SHA-256: {code(tiering['strengthInventorySha256'])}",
                      f"- Approval: {code(tiering['approvals']['approvalId'])} by "
                      f"{markdown_table_text(tiering['approvals']['approver'])}; membership "
                      f"{code(tiering['approvals']['membershipSha256'])}",
                      "", "| Binding | Path | SHA-256 |", "| --- | --- | --- |",
                      f"| Contract | {code(tiering['contract']['path'])} | {code(tiering['contract']['sha256'])} |",
                      f"| Decision | {code(tiering['decision']['path'])} | {code(tiering['decision']['sha256'])} |",
                      f"| Pre-split result | {code(pre_split['path'])} | {code(pre_split['sha256'])} |",
                      f"| Pre-split receipt | {code(pre_split['receipt']['path'])} | {code(pre_split['receipt']['sha256'])} |",
                      f"| Approvals | {code(tiering['approvals']['path'])} | {code(tiering['approvals']['sha256'])} |",
                      f"| Fault evidence | {code(tiering['faultEvidence']['path'])} | {code(tiering['faultEvidence']['sha256'])} |",
                      f"| Test assembly | {code(tiering['testAssembly']['path'])} | {code(tiering['testAssembly']['sha256'])} |"])
        lines.extend(f"| {code(role)} | {code(row['path'])} | {code(row['sha256'])} |"
                     for role, row in tiering["disposition"].items())
    if "conformanceExecution" in record:
        tiering = record["conformanceExecution"]
        execution = tiering["execution"]
        lines.extend(["", "## Story 9.2 conformance execution", "",
                      f"- Candidate: {code(tiering['sourceRevisionId'])}",
                      f"- Story 9.1 record SHA-256: {code(tiering['predecessorRecord']['sha256'])}",
                      f"- Frozen definitions / active methods / historical exclusions: {tiering['frozenDefinitions']} / "
                      f"{execution['activeFrozenMethods']} / {tiering['historicalExclusions']}",
                      f"- Preserved cases before / after: {execution['beforeExecutedCases']} / {execution['afterExecutedCases']}; "
                      f"live controls: {execution['controlsExecuted']}; historical controls: {tiering['historicalValidationControls']}",
                      f"- Migration proposal SHA-256: {code(tiering['proposalSha256'])}",
                      f"- Quality approval SHA-256: {code(tiering['approval']['sha256'])}",
                      f"- Candidate fault-source SHA-256: {code(tiering['faultSourceInputsSha256'])}",
                      "", "| Tier | Frozen methods | Frozen cases | Controls | Passed | Project SHA-256 | Assembly SHA-256 | Result SHA-256 |",
                      "| --- | --- | --- | --- | --- | --- | --- | --- |"])
        for name, tier in execution["tiers"].items():
            lines.append("| " + " | ".join(code(value) for value in (
                name, tier['activeFrozenMethods'], tier['activeFrozenCases'], tier['controlCases'],
                tier['counts']['passed'], tier['project']['sha256'], tier['assembly']['sha256'], tier['result']['sha256'])) + " |")
        lines.extend(["", "| Inventory | SHA-256 |", "| --- | --- |"])
        lines.extend(f"| {code(name)} | {code(digest)} |" for name, digest in tiering['inventories'].items())
        lines.extend(["", "Fault fixtures use explicitly synthetic approval, execution, and assembly bytes. "
                      "Their measured blockers and restoration bind candidate source inputs; passing acceptance comes from the two tier results above."])
        environment = tiering["candidateEnvironment"]
        if environment.get("route") in ("story-9.2-successor-v2", "story-9.2-successor-v3",
                                        "story-9.2-successor-v4"):
            version = environment["route"].rsplit("-", 1)[-1]
            lines.extend(["", f"### Authorized candidate environment {version}", "",
                          f"- Approved source: {code(environment['approvedSourceCommit'])}",
                          f"- Scope authorization SHA-256: {code(environment['scopeAuthorization']['sha256'])}",
                          f"- Scope proposal SHA-256: {code(environment['scopeProposal']['proposalSha256'])}",
                          f"- Quality approval SHA-256: {code(environment['qualityApproval']['sha256'])}",
                          "", "| Root gitlink | Commit |", "| --- | --- |"])
            lines.extend("| " + " | ".join(code(value) for value in (row["path"], row["object"])) + " |"
                         for row in environment["rootGitlinks"])
        else:
            lines.extend(["", "### Authorized candidate environment", "",
                          f"- Amendment SHA-256: {code(environment['amendment']['sha256'])}",
                          f"- Authorized environment proposal SHA-256: {code(environment['proposalSha256'])}",
                          f"- Original measured environment candidate: {code(environment['measuredCandidateCommit'])}",
                          "", "| Root gitlink | Before commit | After commit | Before / after mode |",
                          "| --- | --- | --- | --- |"])
            lines.extend("| " + " | ".join(code(value) for value in (
                row["path"], row["beforeCommit"], row["afterCommit"], row["beforeMode"] + " / " + row["afterMode"])) + " |"
                         for row in environment["gitlinks"])
            lines.extend(["", "| Owning promotion commit | Parent | Changed gitlinks |", "| --- | --- | --- |"])
            lines.extend("| " + " | ".join(code(value) for value in (
                row["commit"], row["parent"], ", ".join(link["path"] for link in row["changedGitlinks"]))) + " |"
                         for row in environment["promotionCommits"])
    if "testSupportExecution" in record or "evidenceBoundaryExecution" in record:
        support = record.get("testSupportExecution", record.get("evidenceBoundaryExecution"))
        lines.extend(["", f"## Story {record['storyId']} TestSupport execution", "",
                      f"- Candidate: {code(support['sourceRevisionId'])}",
                      f"- Candidate rule: {markdown_table_text(support['bindingRule'])}",
                      f"- Frozen inventory SHA-256: {code(support['inventorySha256'])}",
                      f"- Contract SHA-256: {code(support['contract']['sha256'])}",
                      "", "| Predecessor | Record SHA-256 |", "| --- | --- |"])
        lines.extend(f"| {code(row['storyId'])} | {code(row['sha256'])} |"
                     for row in support["predecessorRecords"])
        lines.extend(["", "| Project | Project SHA-256 | Assembly | Assembly SHA-256 |",
                      "| --- | --- | --- | --- |"])
        lines.extend(f"| {code(row['project']['path'])} | {code(row['project']['sha256'])} "
                     f"| {code(row['assembly']['path'])} | {code(row['assembly']['sha256'])} |"
                     for row in support["projects"])
        lines.extend(["", "| Result | SHA-256 |", "| --- | --- |"])
        lines.extend(f"| {code(row['path'])} | {code(row['sha256'])} |"
                     for row in support["results"])
        if "rootConstants" in support:
            roots = support["rootConstants"]
            lines.extend(["", f"- Consuming source: {code(roots['sourcePath'])}",
                          "- Supersession allowlist: empty", "",
                          "| Literal root | Value |", "| --- | --- |"])
            lines.extend(f"| {code(name)} | {code(value)} |"
                         for name, value in sorted(roots["constants"].items()))
    if "evidenceAdoptionExecution" in record:
        amendment = record["evidenceAdoptionExecution"]["approvedGitlinkAmendment"]
        lines.extend(["", "## Approved root-gitlink amendment", "",
                      f"- Owner decision: {code(amendment['decisionDate'])}",
                      f"- Approved revision: {code(amendment['approvalCommit'])}",
                      "- The twelve original baseline roots remain immutable provenance; three roots are unchanged.",
                      "", "| Root submodule | Original commit | Approved commit | Original / approved mode |",
                      "| --- | --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (
            row["path"], row["sourceObject"], row["destinationObject"], row["sourceMode"] + " / " + row["destinationMode"])) + " |"
                     for row in amendment["transitions"])
    if "thinGuidanceExecution" in record:
        guidance = record["thinGuidanceExecution"]
        lines.extend(["", "## Thin-module authoring guidance", "",
                      f"- Guidance source: {code(guidance['guidance']['sourcePath'])} ({code(guidance['guidance']['sourceSha256'])})",
                      f"- Bundle JSON: {code(guidance['guidance']['jsonPath'])} ({code(guidance['guidance']['jsonSha256'])})",
                      f"- Bundle Markdown: {code(guidance['guidance']['markdownPath'])} ({code(guidance['guidance']['markdownSha256'])})",
                      f"- Signable payload: {code(guidance['guidance']['signablePayloadSha256'])}",
                      f"- Reviewer: {code(guidance['reviewDecision']['reviewer'])} ({code(guidance['reviewDecision']['decision'])} at {code(guidance['reviewDecision']['decidedAtUtc'])})",
                      f"- AC-11.1-01 substitution: {code(guidance['authoritySubstitution']['decision'])}", "",
                      "| Predecessor | Binding | Path | SHA-256 |", "| --- | --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (row["storyId"], row["binding"], row["path"], row["sha256"])) + " |"
                     for row in guidance["authoritySubstitution"]["predecessors"])
        lines.extend(["", "| Receipt | Path | SHA-256 |", "| --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (row["scenarioId"], row["path"], row["sha256"])) + " |"
                     for row in guidance["receipts"])
    if "fixtureProofExecution" in record:
        fixture = record["fixtureProofExecution"]
        lines.extend(["", "## Minimal-module fixture proof", "",
                      f"- Story 11.1 record: {code(fixture['predecessor']['recordPath'])} ({code(fixture['predecessor']['recordSha256'])})",
                      f"- Guidance bundle: {code(fixture['predecessor']['guidanceJsonSha256'])}; signable payload {code(fixture['predecessor']['signablePayloadSha256'])}",
                      f"- Fixture inventory {code(fixture['fixture']['inventoryId'])}: {code(fixture['fixture']['inventorySha256'])} ({fixture['fixture']['fileCount']} paths under {code(fixture['fixture']['root'])})",
                      f"- Fixture sources: {code(fixture['fixture']['sourcesSha256'])}",
                      f"- Proof JSON: {code(fixture['proof']['jsonPath'])} ({code(fixture['proof']['jsonSha256'])})",
                      f"- Proof Markdown: {code(fixture['proof']['markdownPath'])} ({code(fixture['proof']['markdownSha256'])})",
                      f"- Proof schema: {code(fixture['proof']['schemaPath'])} ({code(fixture['proof']['schemaSha256'])})",
                      f"- Proof signable payload: {code(fixture['proof']['signablePayloadSha256'])}",
                      f"- Resolved public APIs: {code(fixture['proof']['resolvedPublicApisSha256'])}",
                      f"- Measured test result: {code(fixture['proof']['resultSha256'])}", "",
                      "| Lock file | SHA-256 |", "| --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (row["path"], row["sha256"])) + " |"
                     for row in fixture["fixture"]["lockFiles"])
        lines.extend(["", "| Receipt | Path | SHA-256 |", "| --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (row["scenarioId"], row["path"], row["sha256"])) + " |"
                     for row in fixture["receipts"])
    if "sm2Execution" in record:
        sm2 = record['sm2Execution']
        metrics = sm2['metrics']
        lines.extend(['', '## Measured SM-2 and OQ-2', '',
                      f"- Evidence JSON: {code(sm2['evidence']['jsonPath'])} ({code(sm2['evidence']['jsonSha256'])})",
                      f"- Evidence Markdown: {code(sm2['evidence']['markdownPath'])} ({code(sm2['evidence']['markdownSha256'])})",
                      f"- Signable payload: {code(sm2['evidence']['signablePayloadSha256'])}",
                      f"- Measured: {metrics['fileCount']} files; {metrics['physicalLoc']} physical LOC; {metrics['nonblankLoc']} nonblank LOC.",
                      f"- Reduction against 58 estimated files / 1,460 estimated LOC: {metrics['fileReductionPercentage']}% files; {metrics['locReductionPercentage']}% indicative LOC.",
                      f"- OQ-2: {code(metrics['oq2Decision'])}, qualified against the accepted estimate. Measurement confidence high; comparison confidence low.",
                      '- Historical LOC uses estimated nonblank logical skeleton lines; its comparison to measured physical LOC is supporting evidence only.',
                      '', '| Checkpoint | Original candidate |', '| --- | --- |'])
        lines.extend('| ' + code(key) + ' | ' + code(value) + ' |' for key, value in sm2['checkpoints'].items())
        lines.extend(['', '| Receipt | Path | SHA-256 |', '| --- | --- | --- |'])
        lines.extend('| ' + ' | '.join(code(row[key]) for key in ('scenarioId', 'path', 'sha256')) + ' |'
                     for row in sm2['receipts'])
    lines.extend(["", "## Fault injection", ""])
    faults = record["faultInjection"]["results"]
    if "thinGuidanceExecution" in record or "fixtureProofExecution" in record:
        faults = record.get("thinGuidanceExecution", record.get("fixtureProofExecution"))["mutationLedger"]
        lines.extend([f"{len(faults)} observed mutation results are bound to the retained receipts.", "",
                      "| Fault | Expected blocker | Detected | Restored |", "| --- | --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (
            row["id"], row["expectedBlocker"], row["detected"], row["restored"])) + " |" for row in faults)
    elif 'sm2Execution' in record:
        faults = record['sm2Execution']['mutationLedger']
        lines.extend([f'{len(faults)} measured faults detected with byte-identical restoration.', '',
                      '| Fault | Expected blocker | Observed blockers | Before SHA-256 | After SHA-256 | Restored |',
                      '| --- | --- | --- | --- | --- | --- |'])
        lines.extend('| ' + ' | '.join(code(value) for value in (row['id'], row['expectedBlocker'],
                     ', '.join(row['observedBlockers']), row['beforeSha256'], row['afterSha256'], row['restored'])) + ' |'
                     for row in faults)
    elif record["storyId"] in ("10.3", "10.4") and ("evidenceWorkflowExecution" in record or "evidenceAdoptionExecution" in record):
        facts = record.get("evidenceWorkflowExecution", record.get("evidenceAdoptionExecution"))
        faults = facts["mutationLedger"]
        lines.extend([f"{len(faults)} observed mutation results are bound to the retained receipts.", "",
                      "| Fault | Expected blocker | Observed blockers | Before SHA-256 | After SHA-256 | Restored |",
                      "| --- | --- | --- | --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (
            row["id"], row["expectedBlocker"], ", ".join(row["observedBlockers"]),
            row["beforeSha256"], row["afterSha256"], row["restored"])) + " |" for row in faults)
    elif faults and record["storyId"] == "10.2":
        lines.extend(["| Fault | Expected blocker | Observed blockers | Before SHA-256 | After SHA-256 | Detected | Restored |",
                      "| --- | --- | --- | --- | --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (
            row["id"], row["expectedBlocker"], ", ".join(row["observedBlockers"]),
            row["beforeSha256"], row["afterSha256"], row["detected"], row["restored"])) + " |" for row in faults)
    elif faults and record["storyId"] in ("7.4", "8.2", "9.1", "9.2"):
        lines.extend(["| Fault | Expected blocker | Observed exit | Observed blockers | Before SHA-256 | After SHA-256 |",
                      "| --- | --- | --- | --- | --- | --- |"])
        lines.extend("| " + " | ".join(code(value) for value in (
            row["id"], row["expectedBlocker"], row["observedExitCode"],
            ", ".join(row["observedBlockers"]), row["beforeSha256"], row["afterSha256"])) + " |"
                     for row in faults)
    elif faults:
        lines.extend(["| Fault | Expected blocker |", "| --- | --- |"])
        lines.extend(
            f"| {code(item['id'])} | {code(item['expectedBlocker'])} |" for item in faults
        )
    else:
        lines.append("No fault-injection result is bound to this record.")
    lines.extend(
        [
            "",
            "## Outputs",
            "",
            "| Output | Path |",
            "| --- | --- |",
            f"| JSON | {code(record['outputs']['json']['path'])} |",
            f"| Markdown | {code(record['outputs']['markdown']['path'])} |",
            "",
            "## Rollback boundary",
            "",
            markdown_table_text(record["rollback"]["boundary"]),
            "",
            "## Summary",
            "",
            "| Required | Passed | Failed | Blocked | Skipped | Not run |",
            "| --- | --- | --- | --- | --- | --- |",
            "| "
            + " | ".join(
                code(record["summary"][key])
                for key in ("required", "passed", "failed", "blocked", "skipped", "notRun")
            )
            + " |",
        ]
    )
    return "\n".join(lines) + "\n"


def v2_finalize(record: dict[str, Any]) -> tuple[dict[str, Any], bytes, bytes]:
    """Cross-bind the JSON and Markdown digests.

    The JSON content digest is the SHA-256 of the canonical JSON rendering with
    `outputs.json.sha256`, `outputs.markdown.sha256`, and
    `renderedMarkdownSha256` all zeroed; it is written into
    `outputs.json.sha256` and into the Markdown. Both Markdown digest fields
    carry the SHA-256 of the exact Markdown bytes.
    """
    draft = v2_zero_digests(record)
    json_digest = v2_sha256(v2_render_json(draft))
    markdown = v2_render_markdown(draft, json_digest).encode("utf-8")
    markdown_digest = v2_sha256(markdown)
    final = json.loads(json.dumps(draft))
    final["outputs"]["json"]["sha256"] = json_digest
    final["outputs"]["markdown"]["sha256"] = markdown_digest
    final["renderedMarkdownSha256"] = markdown_digest
    return final, v2_render_json(final), markdown


def v2_verify_pair(json_bytes: bytes, markdown_bytes: bytes) -> list[str]:
    """Independently re-derive every digest binding of one JSON/Markdown pair."""
    problems: list[str] = []
    try:
        record = v2_parse_json(json_bytes)
        draft = v2_zero_digests(record)
    except (UnicodeDecodeError, ValueError, KeyError, TypeError):
        return ["the JSON record is not parseable"]
    if v2_render_json(record) != json_bytes:
        problems.append("the JSON bytes are not the canonical rendering")
    json_digest = v2_sha256(v2_render_json(draft))
    if record["outputs"]["json"]["sha256"] != json_digest:
        problems.append("outputs.json.sha256 is not the self-excluding JSON digest")
    markdown_digest = v2_sha256(markdown_bytes)
    if (
        record["outputs"]["markdown"]["sha256"] != markdown_digest
        or record["renderedMarkdownSha256"] != markdown_digest
    ):
        problems.append("the Markdown digests do not match the Markdown bytes")
    if v2_render_markdown(draft, json_digest).encode("utf-8") != markdown_bytes:
        problems.append("the Markdown is not the projection of the JSON record")
    if b"\r" in json_bytes or b"\r" in markdown_bytes:
        problems.append("the outputs are not LF-only")
    if record.get("storyId") == "10.1":
        support = record.get("testSupportExecution")
        candidate_data = record.get("candidate")
        if (not isinstance(support, dict) or not isinstance(candidate_data, dict)
                or support.get("sourceRevisionId") != candidate_data.get("commit")):
            problems.append("Story 10.1 source revision differs from its candidate")
        elif not v2_10_1_exact_paths(support):
            problems.append("Story 10.1 artifact paths are not exact and distinct")
        elif record.get("predecessors") != [row["storyId"] for row in support["predecessorRecords"]]:
            problems.append("Story 10.1 predecessor list differs from the bound records")
        inventory = record.get("inventory")
        if not isinstance(support, dict) or not isinstance(inventory, dict) or inventory.get("sha256") != support.get("inventorySha256"):
            problems.append("Story 10.1 inventory digest differs from the TestSupport binding")
        scenarios = record.get("scenarios")
        if not isinstance(scenarios, list) or not all(isinstance(row, dict) for row in scenarios):
            problems.append("Story 10.1 scenarios are malformed")
        else:
            if any(row.get("result") == "PASS" and row.get("exitCode") != 0 for row in scenarios):
                problems.append("Story 10.1 PASS scenario has a nonzero exit")
            measured_paths = [(row.get("resultFile") or {}).get("path") for row in scenarios
                              if row.get("scenarioId") in {f"AC-10.1-0{number}" for number in range(2, 7)}]
            if (not isinstance(support, dict) or not isinstance(support.get("results"), list)
                    or measured_paths != [row.get("path") for row in support["results"]]):
                problems.append("Story 10.1 scenario result paths differ from the TestSupport binding")
            try:
                by_id = {row["scenarioId"]: row for row in scenarios}
                projects = support["projects"]
                results = support["results"]
                if (len(by_id) != len(scenarios)
                        or set(by_id) != {f"AC-10.1-0{number}" for number in range(1, 8)}
                        or by_id["AC-10.1-01"]["resultFile"] != projects[0]["assembly"]
                        or by_id["AC-10.1-01"]["outputFiles"] != [projects[0]["assembly"]]
                        or any(by_id[f"AC-10.1-0{number}"]["resultFile"] != results[number - 2]
                               or by_id[f"AC-10.1-0{number}"]["outputFiles"]
                               != [results[number - 2], projects[1]["assembly"]]
                               for number in range(2, 7))):
                    problems.append("Story 10.1 scenario result or assembly digests differ from the TestSupport binding")
            except (KeyError, TypeError, IndexError):
                problems.append("Story 10.1 scenario result or assembly digests are malformed")
    if record.get("storyId") == "10.2":
        problems.extend(v2_10_2_pair_errors(record))
    if record.get("storyId") == "10.3":
        problems.extend(v2_10_3_pair_errors(record))
    if record.get("storyId") == "10.4":
        problems.extend(v2_10_4_pair_errors(record))
    if record.get("storyId") == "11.1":
        problems.extend(v2_11_1_pair_errors(record))
    if record.get("storyId") == "11.2":
        problems.extend(v2_11_2_pair_errors(record))
    if record.get("storyId") == "11.3":
        problems.extend(v2_11_3_pair_errors(record))
    return problems


def v2_10_1_exact_paths(support: dict[str, Any]) -> bool:
    """Check the fixed Story 10.1 artifact order independently of schema validation."""
    names = ("Hexalith.Conversations.TestSupport", "Hexalith.Conversations.TestSupport.Tests")
    try:
        projects = support["projects"]
        results = support["results"]
        expected_projects = [
            (f"tests/{name}/{name}.csproj", f"tests/{name}/bin/Release/net10.0/{name}.dll")
            for name in names
        ]
        expected_results = [f"artifacts/v9/10.1/AC-10.1-0{number}.trx" for number in range(2, 7)]
        expected_predecessors = [
            (story, f"docs/release-evidence/story-{story}-final-record-v2.json")
            for story in ("7.4", "9.2")
        ]
        return (len(projects) == 2 and len(results) == 5
                and [(row["project"]["path"], row["assembly"]["path"]) for row in projects] == expected_projects
                and [row["path"] for row in results] == expected_results
                and support["contract"]["path"] == "_bmad-output/planning-artifacts/v9/story-contracts/10.1.json"
                and [(row["storyId"], row["path"]) for row in support["predecessorRecords"]] == expected_predecessors)
    except (KeyError, TypeError, ValueError):
        return False


def v2_10_2_pair_errors(record: dict[str, Any]) -> list[str]:
    """Check every cross-binding independently of JSON/Markdown digest agreement."""
    problems = []
    try:
        support = record["evidenceBoundaryExecution"]
        names = ("Hexalith.Conversations.TestSupport", "Hexalith.Conversations.TestSupport.Tests")
        expected_projects = [(f"tests/{name}/{name}.csproj", f"tests/{name}/bin/Release/net10.0/{name}.dll") for name in names]
        expected_results = [f"artifacts/v9/10.2/AC-10.2-0{number}.trx" for number in range(1, 9)]
        if (support["sourceRevisionId"] != record["candidate"]["commit"]
                or support["bindingRule"] != "SC-10.2 is HEAD^{commit} at first final-record generation"
                or support["contract"]["path"] != "_bmad-output/planning-artifacts/v9/story-contracts/10.2.json"
                or support["inventorySha256"] != V2_10_2_INVENTORY
                or record["inventory"] != {"id": "V9-10.2-ENTRY-v1", "sha256": V2_10_2_INVENTORY}
                or record["predecessors"] != ["10.1"]
                or [(row["storyId"], row["path"]) for row in support["predecessorRecords"]]
                != [("10.1", "docs/release-evidence/story-10.1-final-record-v2.json")]
                or [(row["project"]["path"], row["assembly"]["path"]) for row in support["projects"]] != expected_projects
                or [row["path"] for row in support["results"]] != expected_results):
            problems.append("Story 10.2 candidate, inventory, predecessor, or artifact paths differ from their bindings")
        roots = support["rootConstants"]
        if (set(roots) != {"sourcePath", "constants", "allowlist"}
                or roots["sourcePath"] != V2_10_2_ROOT_SOURCE or roots["allowlist"] != []
                or set(roots["constants"]) != {"Story101RecordSha256", "Story101CandidateCommit"}
                or roots["constants"]["Story101RecordSha256"] != support["predecessorRecords"][0]["sha256"]):
            problems.append("Story 10.2 root constants differ from the predecessor binding")
        problems.extend(row["message"] for row in v2_10_2_mutation_errors(support["mutationLedger"]))
        if record["faultInjection"]["results"] != support["mutationLedger"]:
            problems.append("Story 10.2 fault table differs from its mutation ledger")
        scenarios = record["scenarios"]
        by_id = {row["scenarioId"]: row for row in scenarios}
        if (len(by_id) != len(scenarios) or set(by_id) != {f"AC-10.2-0{number}" for number in range(1, 10)}
                or any(by_id[f"AC-10.2-0{number}"]["resultFile"] != support["results"][number - 1]
                       or by_id[f"AC-10.2-0{number}"]["outputFiles"] != [support["results"][number - 1], support["projects"][1]["assembly"]]
                       for number in range(1, 9))
                or not v2_10_1_predecessor_passes(record)
                or record["summary"]["required"] != 9):
            problems.append("Story 10.2 scenarios or receipt/assembly digests differ from the execution binding")
    except (KeyError, TypeError, IndexError, AttributeError):
        problems.append("Story 10.2 execution bindings are malformed")
    return problems


def v2_output_target(repository: Path, relative: str) -> Path:
    """Lexically and physically contain one output path; never follow a symlinked leaf."""
    target = repository / relative
    parent = target.parent
    existing = parent
    while not existing.exists():
        existing = existing.parent
    existing.resolve(strict=True).relative_to(repository)
    if target.is_symlink():
        raise ValueError("output leaf is a symlink")
    if target.exists() and not target.is_file():
        raise ValueError("output leaf is not a regular file")
    return target


class V2OutputDrift(Exception):
    """The installed output bytes differ from the generated bytes."""


def v2_restore_claim(error: BaseException) -> str:
    """Honest restore clause for a write-path diagnostic."""
    if getattr(error, "restored", True):
        return "every replaced output was restored"
    return "one or more replaced outputs could not be restored"


def v2_restore_outputs(replaced: list[Path], originals: dict[Path, bytes | None]) -> bool:
    """Restore every replaced target. Return True only when every restore succeeded.

    Restore errors are not raised, so the caller's write error stays primary.
    Callers must not claim a restore that did not complete.
    """
    restored = True
    for target in reversed(replaced):
        original = originals[target]
        temporary = target.with_name(f".{target.name}.{os.getpid()}.restore")
        created = False
        try:
            if original is None:
                target.unlink(missing_ok=True)
            else:
                with temporary.open("xb") as handle:
                    created = True
                    handle.write(original)
                os.replace(temporary, target)
        except OSError:
            restored = False
        finally:
            try:
                if created:
                    temporary.unlink(missing_ok=True)
            except OSError:
                pass
    return restored


def v2_write_outputs(targets: list[tuple[Path, bytes]]) -> None:
    """Replace every output atomically and read each back.

    Any failure after the first replacement, including a read-back that differs
    from the generated bytes, restores every replaced target to its captured
    original (or removes it when it did not exist) and re-raises the original
    error: `OSError` for an I/O fault, `V2OutputDrift` for differing bytes.
    The raised error's `restored` attribute is True only when every restore
    succeeded; callers must not claim a restore that did not complete.
    """
    destinations = [Path(os.path.abspath(target)) for target, _ in targets]
    reserved = {target.with_name(f".{target.name}.{os.getpid()}.{phase}")
                for target in destinations for phase in ("tmp", "restore")}
    if len(set(destinations)) != len(destinations) or set(destinations) & reserved:
        raise ValueError("output destinations collide with each other or transaction temporaries")
    staged: list[tuple[Path, Path]] = []
    originals: dict[Path, bytes | None] = {}
    replaced: list[Path] = []
    try:
        for target, content in targets:
            target.parent.mkdir(parents=True, exist_ok=True)
            originals[target] = target.read_bytes() if target.exists() else None
            temporary = target.with_name(f".{target.name}.{os.getpid()}.tmp")
            with temporary.open("xb") as handle:
                staged.append((temporary, target))
                handle.write(content)
                handle.flush()
                os.fsync(handle.fileno())
        try:
            for temporary, target in staged:
                os.replace(temporary, target)
                replaced.append(target)
            for target, content in targets:
                if target.read_bytes() != content:
                    raise V2OutputDrift(str(target))
        except (OSError, V2OutputDrift) as error:
            setattr(error, "restored", v2_restore_outputs(replaced, originals))
            raise
    finally:
        for temporary, _ in staged:
            try:
                temporary.unlink(missing_ok=True)
            except OSError:
                pass


def v2_self_ledger(scenario_id: str, story_id: str | None = None) -> list[dict[str, str]]:
    """The generator's own evaluated assertions for the self-invocation scenario."""
    subjects: tuple[str, ...] = (
        "generator::contract-schema-and-identity",
        "generator::authority-bundle-digest-recomputed",
        "generator::raw-gitlinks-equal-root-gitmodules",
        "generator::committed-candidate-worktree-clean",
        "generator::predecessor-scenarios-pass-with-ledgers",
        "generator::declared-output-paths",
        "generator::record-schema-valid",
        "generator::deterministic-rendering",
        "generator::json-markdown-digest-cross-binding",
    )
    if story_id == "7.3":
        subjects += (
            "generator::acceptance-results-bound-to-candidate",
            "generator::workflow-bodies-equal-acceptance-inputs",
            "generator::predecessor-records-7.1-7.2-verified",
        )
    if story_id == "7.4":
        subjects += (
            "generator::historical-closure-facts-equal-acceptance-result",
            "generator::predecessor-records-7.1-7.3-and-chain-verified",
            "generator::all-thirteen-required-fault-blockers-observed",
            "generator::all-thirteen-fixtures-restored-byte-identically",
        )
    if story_id == "8.1":
        subjects += (
            "generator::ux-disposition-schema-sources-and-output-digests",
            "generator::story-7.4-predecessor-pair-verified",
            "generator::five-exact-xunit-selectors-passed",
            "generator::candidate-build-and-production-scope-bound",
        )
    if story_id == "9.1":
        subjects += (
            "generator::tiering-disposition-reproduced-and-verified-read-only",
            "generator::story-7.4-predecessor-pair-verified",
            "generator::retained-pre-split-capture-receipt-bound",
            "generator::assertion-and-strength-inventories-bound",
            "generator::quality-owner-approval-digest-bound",
            "generator::all-ten-tiering-fault-categories-measured",
            "generator::all-ten-fixtures-restored-byte-identically",
            "generator::seven-exact-xunit-selectors-passed",
            "generator::candidate-stamped-nonempty-results",
        )
    if story_id == "9.2":
        subjects += (
            "generator::hash-bound-execution-amendment-preserves-frozen-contract",
            "generator::story-9.1-accepted-pair-and-disposition-compatible",
            "generator::both-projects-assemblies-and-results-bound",
            "generator::evaluated-portable-surface-has-no-nonportable-reference",
            "generator::before-after-identities-and-strengths-bound",
            "generator::genuine-quality-approval-binds-every-successor-and-public-drift",
            "generator::415-frozen-cases-pass-and-controls-count-separately",
            "generator::all-structural-execution-faults-measured-and-restored",
            "generator::fault-receipts-bind-candidate-source-bytes",
            "generator::protected-v1-bytes-and-fr20-membership-retained",
        )
    if story_id == "8.2":
        subjects += (
            "generator::ordered-unique-52-decisions-and-28-acceptance-ids",
            "generator::story-8.1-pair-candidate-and-disposition-compatible",
            "generator::source-output-and-inventory-digests-bound",
            "generator::all-thirteen-fault-categories-measured",
            "generator::all-fifteen-fixtures-restored-with-pass-and-matching-hashes",
            "generator::candidate-stamped-nonempty-results",
        )
    return [
        {"id": f"{scenario_id}#{ordinal:04d}", "subject": subject, "state": "PASS"}
        for ordinal, subject in enumerate(subjects, start=1)
    ]


def v2_record_region(content: bytes) -> tuple[int, int] | None:
    """Byte span between the single final-record BEGIN line and END line.

    The span starts after the BEGIN line's LF and ends where the END line
    starts. None when either marker is absent or repeated, when a marker is not
    on its own LF-terminated line, or when the markers are out of order.
    """
    begin_marker = RECORD_BEGIN_MARKER.encode("ascii")
    end_marker = RECORD_END_MARKER.encode("ascii")
    if content.count(begin_marker) != 1 or content.count(end_marker) != 1:
        return None
    begin = content.find(begin_marker)
    end = content.find(end_marker)
    start = begin + len(begin_marker) + 1
    if (
        (begin > 0 and content[begin - 1 : begin] != b"\n")
        or content[start - 1 : start] != b"\n"
        or end < start
        or (end > start and content[end - 1 : end] != b"\n")
        or content[end + len(end_marker) : end + len(end_marker) + 1] != b"\n"
    ):
        return None
    return start, end


def v2_spec_lifecycle_only_change(
    repository: Path, candidate: str, head: str, spec_path: str, record_region: bool = False,
    updated_content: bytes | None = None, task_checkboxes: bool = False,
) -> bool:
    """Accept only completion-route-owned changes in a committed story spec.

    With `record_region`, the spec's inserted final-record region may also
    change: its content between an existing marker pair, or one marker pair
    appended after a blank line at the end of a spec that carried none. Story
    7.3 completion routes may additionally set `followup_review_recommended`
    and write the `Review Triage Log` and `Auto Run Result` sections they own.
    """
    status = re.compile(
        rb"(?m)^status: (?P<quote>['\"]?)(?P<value>draft|ready-for-dev|"
        rb"in-progress|in-review|done)(?P=quote)$"
    )

    followup = re.compile(rb"(?m)^followup_review_recommended:[ \t]*(?:true|false)[ \t]*\n")
    route_sections = (b"## Review Triage Log", b"## Auto Run Result")

    def without_route_sections(content: bytes) -> bytes | None:
        for heading in route_sections:
            pattern = re.compile(rb"(?m)^" + re.escape(heading) + rb"[ \t]*$")
            matches = list(pattern.finditer(content))
            if len(matches) > 1:
                return None
            if not matches:
                continue
            match = matches[0]
            boundaries = [
                position
                for position in (
                    content.find(b"\n## ", match.end()),
                    content.find(b"\n" + RECORD_BEGIN_MARKER.encode("ascii"), match.end()),
                )
                if position >= 0
            ]
            start = match.start()
            if boundaries:
                end = min(boundaries) + 1
            else:
                end = len(content)
            if not boundaries and start > 0 and content[start - 1 : start] == b"\n":
                start -= 1
            content = content[:start] + content[end:]
        return content

    def without_status(content: bytes | None) -> bytes | None:
        if content is None or not content.startswith(b"---\n"):
            return None
        end = content.find(b"\n---\n", 4)
        if end < 0:
            return None
        frontmatter = content[4:end]
        matches = list(status.finditer(frontmatter))
        if len(matches) != 1:
            return None
        match = matches[0]
        masked = (
            content[:4 + match.start("value")]
            + b"<lifecycle>"
            + content[4 + match.end("value"):]
        )
        if record_region:
            frontmatter_end = masked.find(b"\n---\n", 4)
            followups = list(followup.finditer(masked, 4, frontmatter_end + 1))
            if len(followups) > 1:
                return None
            if followups:
                owned = followups[0]
                masked = masked[:owned.start()] + masked[owned.end():]
            masked = without_route_sections(masked)
        if task_checkboxes and masked is not None:
            start = masked.find(b"## Tasks & Acceptance\n")
            end = masked.find(b"## Implementation Notes\n", start + 1) if start >= 0 else -1
            if start < 0 or end < 0:
                return None
            task_section = masked[start:end]
            task_section = re.sub(
                rb"(?m)^([ \t]*- \[)[ xX](\] )",
                rb"\1<task>\2",
                task_section,
            )
            masked = masked[:start] + task_section + masked[end:]
        return masked

    def without_record(content: bytes) -> tuple[bytes, bool] | None:
        markers = (RECORD_BEGIN_MARKER.encode("ascii"), RECORD_END_MARKER.encode("ascii"))
        if not any(marker in content for marker in markers):
            return content, False
        region = v2_record_region(content)
        if region is None:
            return None
        return content[: region[0]] + b"<record>\n" + content[region[1]:], True

    original = without_status(v2_committed_blob(repository, candidate, spec_path))
    updated = without_status(
        updated_content
        if updated_content is not None
        else v2_committed_blob(repository, head, spec_path)
    )
    if not record_region or original is None or updated is None:
        return original is not None and original == updated
    masked_original = without_record(original)
    masked_updated = without_record(updated)
    if masked_original is None or masked_updated is None:
        return False
    if masked_original[1] or not masked_updated[1]:
        return masked_original[0] == masked_updated[0]
    appended = (
        b"\n" + RECORD_BEGIN_MARKER.encode("ascii") + b"\n<record>\n"
        + RECORD_END_MARKER.encode("ascii") + b"\n"
    )
    return masked_original[0] + appended == masked_updated[0]


def v2_working_spec_lifecycle_only_change(
    repository: Path, head: str, spec_path: str, working: bytes, *, record_region: bool,
    task_checkboxes: bool = False,
) -> bool:
    """Apply the committed lifecycle mask for one retained story to its working spec."""
    return v2_spec_lifecycle_only_change(
        repository, head, head, spec_path, record_region=record_region,
        updated_content=working, task_checkboxes=task_checkboxes,
    )


def v2_sprint_status_only_change(
    repository: Path, candidate: str, head: str, sprint_key: str,
    require_transition: bool = True, *, updated_content: bytes | None = None,
) -> bool:
    """Accept one story row, its `last_updated` date, and the header comment date."""
    status = re.compile(
        rb"(?m)^[ \t]*" + re.escape(sprint_key.encode("utf-8")) + rb":[ \t]*"
        rb"(?P<value>backlog|draft|ready-for-dev|in-progress|in-review|review|done)"
        rb"(?:[ \t]+#[^\r\n]*)?[ \t]*$"
    )
    update_date = re.compile(
        rb"(?m)^last_updated:[ \t]*(?P<value>\d{4}-\d{2}-\d{2}|\d{2}-\d{2}-\d{4} \d{2}:\d{2})[ \t]*$"
    )
    header_date = re.compile(rb"(?m)^# last_updated:[ \t]*(?P<value>\d{4}-\d{2}-\d{2})[ \t]*$")

    def without_status(content: bytes | None) -> tuple[bytes, bytes, bytes, bytes, tuple[bytes, ...]] | None:
        if content is None:
            return None
        matches = list(status.finditer(content))
        if len(matches) != 1:
            return None
        match = matches[0]
        story_value = match.group("value")
        dates = list(update_date.finditer(content))
        if len(dates) != 1:
            return None
        date = dates[0]
        headers = list(header_date.finditer(content))
        if len(headers) > 1:
            return None
        spans = [
            (match.start("value"), match.end("value"), b"<lifecycle>"),
            (date.start("value"), date.end("value"), b"<updated>"),
        ]
        header_value = b""
        if headers:
            header = headers[0]
            header_value = header.group("value")
            spans.append((header.start("value"), header.end("value"), b"<updated>"))
        retro_values: list[bytes] = []
        if sprint_key.startswith("8-1-"):
            for number in (30, 31, 32):
                start = re.search(rb'(?m)^  - id: "epic-7-retro-item-' + str(number).encode("ascii") + rb'-[^\n]+$', content)
                if start is None:
                    return None
                next_item = content.find(b"\n  - id:", start.end())
                block_end = next_item if next_item >= 0 else len(content)
                block = content[start.end():block_end]
                matches = list(re.finditer(rb"(?m)^    status: (?P<value>open|done)$", block))
                if len(matches) != 1:
                    return None
                match = matches[0]
                retro_values.append(match.group("value"))
                spans.append((start.end() + match.start("value"), start.end() + match.end("value"), b"<retro>"))
        masked = content
        for start, end, token in sorted(spans, reverse=True):
            masked = masked[:start] + token + masked[end:]
        return masked, story_value, date.group("value"), header_value, tuple(retro_values)

    original = without_status(v2_committed_blob(repository, candidate, V2_7_2_SPRINT_PATH))
    latest = without_status(updated_content if updated_content is not None
        else v2_committed_blob(repository, head, V2_7_2_SPRINT_PATH))
    if original is None or latest is None or original[0] != latest[0]:
        return False
    if original[1] == b"done" and latest[1] != b"done":
        return False
    if any(before == b"done" and after != b"done" for before, after in zip(original[4], latest[4])):
        return False
    retro_changed = original[4] != latest[4]
    status_changed = original[1] != latest[1]
    header_date_only = (
        not status_changed
        and original[2] == latest[2]
        and original[3] != b""
        and original[3] != latest[3]
    )
    return not require_transition or status_changed or header_date_only or retro_changed


def v2_working_sprint_status_only_change(repository: Path, head: str, sprint_key: str, working: bytes) -> bool:
    """Apply the same single-story/date lifecycle mask to a working sprint file."""
    return v2_sprint_status_only_change(repository, head, head, sprint_key,
        require_transition=False, updated_content=working)


def v2_story_7_2_status_only_change(repository: Path, candidate: str, head: str) -> bool:
    """Accept only a frontmatter status change in the committed Story 7.2 spec."""
    return v2_spec_lifecycle_only_change(repository, candidate, head, V2_7_2_SPEC_PATH)


def v2_story_7_2_sprint_status_only_change(repository: Path, candidate: str, head: str) -> bool:
    """Accept only Story 7.2's status and the sprint update date changing."""
    return v2_sprint_status_only_change(
        repository, candidate, head, "7-2-derive-test-path-candidate-submodule-and-gitlink-facts"
    )


def v2_retention_config(repository: Path, contract_path: str) -> tuple[str, str, str, bool] | None:
    """Resolve a successor story's lifecycle paths from its contract identity."""
    historical = V2_RETAINED_CANDIDATES.get(contract_path)
    if historical is not None:
        return historical
    match = re.fullmatch(r"_bmad-output/planning-artifacts/v9/story-contracts/(\d+)\.(\d+)\.json", contract_path)
    if match is None or int(match.group(1)) < 8:
        return None
    story_id = f"{match.group(1)}.{match.group(2)}"
    prefix = f"spec-{match.group(1)}-{match.group(2)}-"
    candidates = sorted((repository / "_bmad-output/implementation-artifacts").glob(prefix + "*.md"))
    if len(candidates) != 1:
        raise V2Stop([v2_finding("BASELINE_NOT_TRUSTWORTHY", contract_path,
                                 "the successor contract must resolve to one story spec")], story_id)
    spec_path = candidates[0].relative_to(repository).as_posix()
    try:
        lines = (repository / V2_7_2_SPRINT_PATH).read_text(encoding="utf-8").splitlines()
        sections = [index for index, line in enumerate(lines)
                    if re.fullmatch(r"development_status:[ \t]*(?:#.*)?", line)]
        if len(sections) != 1:
            raise ValueError("the sprint tracker must contain one development_status mapping")
        sprint_keys = []
        seen_keys = set()
        for line in lines[sections[0] + 1:]:
            if not line.strip() or line.lstrip().startswith("#"):
                continue
            if not line.startswith((" ", "\t")):
                break
            # The tracked sprint format uses two-space, unquoted mapping keys.
            # Reject unsupported rows rather than hiding another story identity.
            row = re.fullmatch(r"  ([\w-]+):[ \t]+(.+)", line)
            raw_status = row.group(2).lstrip() if row is not None else ""
            if row is None or re.fullmatch(r"[A-Za-z][A-Za-z0-9_-]*(?:[ \t]+#.*)?[ \t]*", raw_status) is None:
                raise ValueError("the sprint tracker must use flat two-space unquoted scalar rows")
            key = row.group(1)
            if key in seen_keys:
                raise ValueError(f"the sprint tracker repeats a development_status key: {key}")
            seen_keys.add(key)
            identity = re.fullmatch(r"(\d+)-(\d+)-.+", key)
            if identity is not None and tuple(map(int, identity.groups())) == tuple(map(int, match.groups())):
                sprint_keys.append(key)
        if len(sprint_keys) != 1:
            raise ValueError("the successor contract must resolve to one tracked story row")
    except (OSError, UnicodeError, ValueError) as error:
        raise V2Stop([v2_finding("BASELINE_NOT_TRUSTWORTHY", V2_7_2_SPRINT_PATH,
                                 str(error))], story_id) from None
    return story_id, spec_path, sprint_keys[0], True


def v2_designated_successor_outputs(story_id: str) -> tuple[str, str]:
    """Derive the retained pair independently of the contract being verified."""
    prefix = f"docs/release-evidence/story-{story_id}-final-record-v2"
    return prefix + ".json", prefix + ".md"


def v2_retained_candidate(repository: Path, head: str, json_path: str, markdown_path: str,
                          validator: Any, story_id: str, spec_path: str, sprint_key: str,
                          record_region: bool) -> str:
    """Retain a verified source candidate across verified successor commits."""
    json_bytes = v2_committed_blob(repository, head, json_path)
    markdown_bytes = v2_committed_blob(repository, head, markdown_path)
    if json_bytes is None and markdown_bytes is None:
        return head
    if json_bytes is None or markdown_bytes is None:
        raise V2Stop([v2_finding("RECORD_CONTENT_DRIFT", "outputs",
                                 f"a prior Story {story_id} output pair is incomplete")], story_id)
    try:
        existing = v2_parse_json(json_bytes)
        if (not isinstance(existing, dict) or existing.get("storyId") != story_id
                or v2_schema_errors(validator, existing)
                or v2_verify_pair(json_bytes, markdown_bytes)):
            raise ValueError(f"prior Story {story_id} pair does not verify")
        old = existing["candidate"]["commit"]
        candidate = try_resolve_commit(repository, old)
    except (OSError, ValueError, TypeError, KeyError, AttributeError):
        raise V2Stop([v2_finding("RECORD_CONTENT_DRIFT", "outputs",
                                 f"the prior Story {story_id} output pair is invalid")],
                     story_id) from None
    if candidate is None or not is_ancestor(repository, candidate, head):
        raise V2Stop([v2_finding("CANDIDATE_NOT_FINAL", "candidate.commit",
                                 "the verified prior candidate is no longer an ancestor of HEAD")],
                     story_id)
    revisions = decode(
        run_git(repository, "rev-list", "--reverse", "--topo-order", f"{candidate}..{head}").stdout
    ).split()
    pair_paths = {json_path, markdown_path}
    lifecycle_paths = {spec_path, V2_7_2_SPRINT_PATH}
    for revision in revisions:
        parent = decode(run_git(repository, "rev-parse", f"{revision}^1").stdout).strip()
        changed = set(committed_path_status(repository, candidate, revision))
        delta = set(committed_path_status(repository, parent, revision))
        moved = changed_gitlinks(repository, candidate, revision)
        pair_delta_invalid = bool(delta & pair_paths) and delta != pair_paths
        successor = int(story_id.split(".")[0]) >= 8
        invalid_spec_change = (spec_path in changed and not v2_spec_lifecycle_only_change(
            repository, candidate, revision, spec_path, record_region,
            task_checkboxes=successor))
        invalid_sprint_change = (
            V2_7_2_SPRINT_PATH in changed
            and not v2_sprint_status_only_change(
                repository, candidate, revision, sprint_key, require_transition=False
            )
        )
        invalid_spec_delta = spec_path in delta and not v2_spec_lifecycle_only_change(
            repository, parent, revision, spec_path, record_region,
            task_checkboxes=successor,
        )
        invalid_sprint_delta = V2_7_2_SPRINT_PATH in delta and not v2_sprint_status_only_change(
            repository, parent, revision, sprint_key
        )
        revision_json = v2_committed_blob(repository, revision, json_path)
        revision_markdown = v2_committed_blob(repository, revision, markdown_path)
        pair_invalid = revision_json is None or revision_markdown is None
        if not pair_invalid:
            try:
                revision_record = v2_parse_json(revision_json)
                pair_invalid = (
                    not isinstance(revision_record, dict)
                    or revision_record.get("storyId") != story_id
                    or revision_record.get("candidate", {}).get("commit") != candidate
                    or v2_schema_errors(validator, revision_record)
                    or bool(v2_verify_pair(revision_json, revision_markdown))
                )
            except (UnicodeDecodeError, ValueError, TypeError, KeyError, AttributeError):
                pair_invalid = True
        if (
            moved
            or pair_invalid
            or pair_delta_invalid
            or invalid_spec_change
            or invalid_sprint_change
            or invalid_spec_delta
            or invalid_sprint_delta
            or changed - {json_path, markdown_path} - {spec_path, V2_7_2_SPRINT_PATH}
            or delta - pair_paths - lifecycle_paths
        ):
            # Name every path that broke retention so the operator need not diff the commit.
            offending = sorted(
                ((changed | delta) - pair_paths - lifecycle_paths)
                | ({spec_path} if invalid_spec_change or invalid_spec_delta else set())
                | (
                    {V2_7_2_SPRINT_PATH}
                    if invalid_sprint_change or invalid_sprint_delta
                    else set()
                )
                | (pair_paths if pair_invalid or pair_delta_invalid else set())
            )
            findings = [
                v2_finding(
                    "CANDIDATE_NOT_FINAL",
                    "candidate.commit",
                    f"commit {revision} after the verified candidate changes source, "
                    "gitlinks, or non-lifecycle state"
                    + (f": {v2_path_summary(offending)}" if offending else ""),
                )
            ]
            if moved:
                findings.append(v2_finding("GITLINK_DRIFT", "candidate.gitlinks",
                                           f"root gitlinks moved: {v2_path_summary(moved)}"))
            raise V2Stop(findings, story_id)
    return candidate


def v2_story_7_2_result_ids_match(content: bytes, project: str) -> bool:
    """Require every TRX result ID to resolve to this project's test definitions."""
    root = ElementTree.fromstring(content)
    expected_ids = {
        definition.get("id")
        for definition in root.findall("./{*}TestDefinitions/{*}UnitTest")
        for method in definition.findall("./{*}TestMethod")
        if definition.get("id")
        and (code_base := method.get("codeBase"))
        and PurePosixPath(re.split(r"[\\/]", code_base)[-1]).stem == project
    }
    results = root.findall("./{*}Results/{*}UnitTestResult")
    return bool(expected_ids) and bool(results) and all(
        result.get("testId") in expected_ids for result in results
    )


def v2_story_7_2_measurements(
    repository: Path,
    candidate: str,
    gitlinks: list[dict[str, str]],
    output_paths: set[str],
    validators: dict[str, Any],
) -> dict[str, Any]:
    """Bind Story 7.2 to root Git objects and current root-owned TRX results."""
    findings: list[dict[str, str]] = []
    spec_bytes = v2_committed_blob(repository, candidate, V2_7_2_SPEC_PATH)
    if spec_bytes is None:
        raise V2Stop([v2_finding("BASELINE_NOT_TRUSTWORTHY", V2_7_2_SPEC_PATH,
                                 "the committed Story 7.2 spec is absent")], "7.2")
    try:
        spec_text = spec_bytes.decode("utf-8")
        frontmatter = parse_frontmatter(spec_text)
        baseline_value = frontmatter_scalar(frontmatter, "baseline_commit")
    except (UnicodeDecodeError, GateError):
        baseline_value = None
        frontmatter = ""
    baseline = try_resolve_commit(repository, baseline_value) if baseline_value and baseline_value != "NO_VCS" else None
    if baseline is None or not is_ancestor(repository, baseline, candidate):
        findings.append(v2_finding("BASELINE_NOT_TRUSTWORTHY", V2_7_2_SPEC_PATH,
                                   "baseline_commit must resolve to an ancestor of the candidate"))

    root_paths = [item["path"] for item in gitlinks]
    changed_paths: list[str] = []
    if baseline is not None:
        try:
            changed = committed_path_status(repository, baseline, candidate)
            changed_paths = sorted(safe_relative_path(path) for path in changed)
        except GateError:
            findings.append(v2_finding("FILE_LIST_DRIFT", "measurements.changedPaths",
                                       "the baseline-to-candidate path set cannot be normalized"))
        internal = [path for path in changed_paths if any(path.startswith(root + "/") for root in root_paths)]
        if internal:
            findings.append(v2_finding("SUBMODULE_INTERNAL_PATH", "measurements.changedPaths",
                                       f"paths below root gitlinks: {v2_path_summary(internal)}"))
        # A gitlink is recorded in candidate.gitlinks, never as a root-owned file.
        changed_paths = [path for path in changed_paths if path not in root_paths]
        if not changed_paths:
            findings.append(v2_finding("FILE_LIST_DRIFT", "measurements.changedPaths",
                                       "the baseline-to-candidate root-owned path set is empty"))
        # The two independently encoded Git diff forms must describe one exact set.
        names = run_git(repository, "diff", "--name-only", "--no-renames", "-z",
                        baseline, candidate, "--").stdout
        raw_paths = sorted({safe_relative_path(decode(path)) for path in names.split(b"\0")
                            if path and decode(path) not in root_paths})
        if raw_paths != changed_paths:
            findings.append(v2_finding("FILE_LIST_DRIFT", "measurements.changedPaths",
                                       "raw and name-status baseline-to-candidate path sets differ"))

    # A candidate is the current committed HEAD. Every later source or gitlink
    # movement invalidates it, even when the caller retained old result files.
    head = try_resolve_commit(repository, "HEAD")
    later_changes = set(committed_path_status(repository, candidate, head)) if head != candidate else set()
    invalid_spec_change = (V2_7_2_SPEC_PATH in later_changes and head is not None
                           and not v2_story_7_2_status_only_change(repository, candidate, head))
    invalid_sprint_change = (V2_7_2_SPRINT_PATH in later_changes and head is not None
                             and not v2_story_7_2_sprint_status_only_change(repository, candidate, head))
    verified_lifecycle_changes = later_changes & V2_7_2_LIFECYCLE_PATHS
    if invalid_spec_change:
        verified_lifecycle_changes.discard(V2_7_2_SPEC_PATH)
    if invalid_sprint_change:
        verified_lifecycle_changes.discard(V2_7_2_SPRINT_PATH)
    if head != candidate and (invalid_spec_change or invalid_sprint_change or later_changes
                              - output_paths - V2_7_2_LIFECYCLE_PATHS):
        findings.append(v2_finding("CANDIDATE_NOT_FINAL", "candidate.commit",
                                   "the candidate was superseded by another committed HEAD"))
        if head is not None and changed_gitlinks(repository, candidate, head):
            findings.append(v2_finding("GITLINK_DRIFT", "candidate.gitlinks",
                                       "a root gitlink moved after the candidate"))

    solution_tree = run_git(repository, "ls-tree", "-r", "--name-only", "-z", candidate).stdout
    root_solutions = sorted(path for path in map(decode, solution_tree.split(b"\0"))
                            if path.endswith(".slnx") and "/" not in path)
    if len(root_solutions) != 1:
        findings.append(v2_finding("TEST_NOT_RUN", "root solution",
                                   "the candidate must contain exactly one root .slnx"))
        projects: dict[str, str] = {}
    else:
        solution_blob = v2_committed_blob(repository, candidate, root_solutions[0])
        try:
            solution = ElementTree.fromstring(solution_blob or b"")
            paths = [safe_relative_path(element.attrib["Path"])
                     for element in solution.findall(".//{*}Project") if "Path" in element.attrib]
            test_paths = [path for path in paths if path.startswith("tests/") and path.endswith(".csproj")
                          and not v2_below(path, root_paths)]
            projects = {PurePosixPath(path).stem: path for path in test_paths}
            if len(projects) != len(test_paths) or not projects:
                raise ValueError("duplicate or empty root test project inventory")
            for path in projects.values():
                if v2_committed_blob(repository, candidate, path) is None:
                    raise ValueError(f"test project is not committed: {path}")
        except (ElementTree.ParseError, GateError, ValueError) as error:
            findings.append(v2_finding("TEST_NOT_RUN", root_solutions[0], str(error)))
            projects = {}

    try:
        if re.search(r"^allowed_skipped_tests:\s*\[\]\s*$", frontmatter, re.MULTILINE):
            allowed_skips: dict[str, str] = {}
        else:
            allowed_skips = frontmatter_allowed_skips(frontmatter)
    except GateError:
        findings.append(v2_finding("TEST_SKIPPED", V2_7_2_SPEC_PATH,
                                   "the committed skip policy is malformed"))
        allowed_skips = {}

    newest_input_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    for path in changed_paths:
        # Only verified lifecycle changes after the candidate can have newer
        # working-tree mtimes without making the candidate's results stale.
        if path in output_paths or path in verified_lifecycle_changes:
            continue
        source = repository / path
        if source.is_file() and not source.is_symlink():
            newest_input_ns = max(newest_input_ns, source.stat().st_mtime_ns)
    measured: list[dict[str, Any]] = []
    for name, project_path in sorted(projects.items()):
        result_path = f"artifacts/v9/7.2/test-results/{name}.trx"
        result_file = repository / result_path
        if not result_file.is_file() or result_file.is_symlink():
            findings.append(v2_finding("TEST_RESULTS_MISSING", result_path,
                                       f"no current TRX result for {name}"))
            continue
        try:
            content, mtime_ns = read_file_snapshot(result_file)
            parsed = parse_trx(content)
        except (ElementTree.ParseError, ValueError, OSError):
            findings.append(v2_finding("TEST_RESULTS_MISSING", result_path,
                                       "TRX result is unreadable or has no valid counters"))
            continue
        if mtime_ns < newest_input_ns:
            findings.append(v2_finding("TEST_RESULTS_STALE", result_path,
                                       "TRX result predates the newest bound source input"))
        counts = parsed["reported"]
        if parsed["assemblies"] != [name]:
            findings.append(v2_finding("TEST_RESULTS_MISSING", result_path,
                                       "TRX result does not name the declared project assembly"))
        if count_disagreements(parsed):
            findings.append(v2_finding("TEST_FAILED", result_path,
                                       "TRX counters disagree with recorded results"))
        if counts["total"] == 0 or not parsed["results"]:
            findings.append(v2_finding("TEST_NOT_RUN", result_path,
                                       "the declared test project ran zero tests"))
        if counts["executed"] < parsed["recomputed"]["passed"] + parsed["recomputed"]["failed"]:
            findings.append(v2_finding("TEST_FAILED", result_path,
                                       "TRX executed count is lower than passed and failed result rows"))
        if parsed["results"] and not v2_story_7_2_result_ids_match(content, name):
            findings.append(v2_finding("TEST_RESULTS_MISSING", result_path,
                                       "TRX result IDs do not match this project's test definitions"))
        if counts["failed"]:
            findings.append(v2_finding("TEST_FAILED", result_path,
                                       f"{counts['failed']} test(s) failed"))
        unapproved = [row["test"] for row in parsed["results"]
                      if row["outcome"] in TRX_SKIPPED_OUTCOMES and row["test"] not in allowed_skips]
        if unapproved:
            findings.append(v2_finding("TEST_SKIPPED", result_path,
                                       f"unapproved skipped tests: {v2_path_summary(unapproved)}"))
        measured.append({"project": name, "projectPath": project_path,
                         "resultFile": {"path": result_path, "sha256": v2_sha256(content)},
                         "counts": counts})
    totals = {key: sum(row["counts"][key] for row in measured)
              for key in ("total", "executed", "passed", "failed", "skipped")}
    if not measured or totals["total"] == 0:
        findings.append(v2_finding("TEST_NOT_RUN", "measurements.testTotals",
                                   "no nonempty root test result was measured"))

    predecessor_bytes = v2_committed_blob(repository, candidate, V2_7_1_RECORD_PATH)
    predecessor_markdown = v2_committed_blob(repository, candidate, V2_7_1_MARKDOWN_PATH)
    if predecessor_bytes is None or predecessor_markdown is None:
        findings.append(v2_finding("AUTHORITY_BINDING_INVALID", "predecessor 7.1",
                                   "the committed Story 7.1 record pair is missing"))
        predecessor_digest = V2_ZERO_DIGEST
    else:
        predecessor_digest = v2_sha256(predecessor_bytes)
        try:
            predecessor = v2_parse_json(predecessor_bytes)
            if predecessor.get("storyId") != "7.1" or v2_schema_errors(validators["record"], predecessor) or v2_verify_pair(predecessor_bytes, predecessor_markdown):
                raise ValueError("predecessor pair does not verify")
        except (ValueError, AttributeError):
            findings.append(v2_finding("AUTHORITY_BINDING_INVALID", "predecessor 7.1",
                                       "the committed Story 7.1 record digest or projection is invalid"))
    if findings:
        raise V2Stop(findings, "7.2")
    return {"baseline": baseline, "changedPaths": changed_paths,
            "testProjects": measured, "testTotals": totals,
            "predecessorRecord": {"storyId": "7.1", "path": V2_7_1_RECORD_PATH,
                                  "sha256": predecessor_digest}}


def v2_verified_predecessor(
    repository: Path, candidate: str, story_id: str, json_path: str, markdown_path: str,
    validator: Any,
) -> str | None:
    """SHA-256 of a committed predecessor JSON record whose pair verifies, else None."""
    json_bytes = v2_committed_blob(repository, candidate, json_path)
    markdown_bytes = v2_committed_blob(repository, candidate, markdown_path)
    if json_bytes is None or markdown_bytes is None:
        return None
    try:
        record = v2_parse_json(json_bytes)
        if (
            not isinstance(record, dict)
            or record.get("storyId") != story_id
            or record.get("outputs", {}).get("json", {}).get("path") != json_path
            or record.get("outputs", {}).get("markdown", {}).get("path") != markdown_path
            or v2_schema_errors(validator, record)
            or v2_verify_pair(json_bytes, markdown_bytes)
            or (story_id == "10.2" and v2_10_2_repository_pair_errors(repository, record, validator))
            or (story_id == "10.3" and v2_10_3_source_errors(repository, record, validator, candidate))
        ):
            return None
    except (UnicodeDecodeError, ValueError, AttributeError, KeyError, TypeError):
        return None
    return v2_sha256(json_bytes)


def v2_10_1_artifact_bytes(repository: Path, relative: str) -> bytes:
    """Read a regular candidate artifact without following any linked path component."""
    if any(parent.is_symlink() for parent in (repository, *repository.parents)):
        raise ValueError(f"linked artifact path: {relative}")
    path = repository
    for component in PurePosixPath(relative).parts:
        if component in ("", ".", ".."):
            raise ValueError(f"invalid artifact path: {relative}")
        path = path / component
        if path.is_symlink():
            raise ValueError(f"linked artifact path: {relative}")
    if not path.is_file():
        raise ValueError(f"missing regular artifact: {relative}")
    path.resolve(strict=True).relative_to(repository.resolve(strict=True))
    return read_file_snapshot(path)[0]


def v2_10_1_project_boundary(repository: Path, relative: str, helper: bool) -> None:
    """Check Debug and Release MSBuild properties and every declared dependency item."""
    for configuration in ("Debug", "Release"):
        command = ["dotnet", "msbuild", relative, f"-p:Configuration={configuration}",
                   "-getProperty:IsPackable,IsPublishable",
                   "-getItem:ProjectReference,PackageReference,Reference,FrameworkReference", "-nologo"]
        try:
            result = subprocess.run(command, cwd=repository, capture_output=True, text=True,
                                    timeout=30, check=False)
        except (OSError, subprocess.TimeoutExpired) as error:
            raise ValueError(f"MSBuild evaluation unavailable: {error}") from error
        if result.returncode:
            raise ValueError(f"MSBuild evaluation failed: {result.stderr.strip()}")
        try:
            evaluated = json.loads(result.stdout)
            properties = evaluated["Properties"]
            items = evaluated["Items"]
            if properties["IsPackable"].lower() != "false" or properties["IsPublishable"].lower() != "false":
                raise ValueError("a TestSupport project is packable or publishable")
            identities = {key: [row["Identity"] for row in items[key]]
                          for key in ("ProjectReference", "PackageReference", "Reference", "FrameworkReference")}
            if identities["Reference"]:
                raise ValueError("an assembly reference is declared")
            if items["FrameworkReference"] != [] and any(
                    row["Identity"] != "Microsoft.NETCore.App" or row.get("IsImplicitlyDefined", "").lower() != "true"
                    for row in items["FrameworkReference"]):
                raise ValueError("an additional framework dependency is declared")
            if helper:
                if identities["ProjectReference"] or identities["PackageReference"]:
                    raise ValueError("the neutral helper has a package or project dependency")
            else:
                expected_project = "../Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj"
                projects = [value.replace("\\", "/") for value in identities["ProjectReference"]]
                expected_packages = {"Microsoft.NET.Test.Sdk", "Shouldly", "xunit.v3",
                                     "xunit.runner.visualstudio", "coverlet.collector"}
                if projects != [expected_project] or set(identities["PackageReference"]) != expected_packages \
                        or len(identities["PackageReference"]) != len(expected_packages):
                    raise ValueError("the test project's declared dependencies changed")
        except (KeyError, TypeError, AttributeError, json.JSONDecodeError) as error:
            raise ValueError(f"malformed {configuration} MSBuild evaluation: {error}") from error


def v2_10_1_predecessor_passes(record: dict[str, Any]) -> bool:
    """Require nonempty, count-consistent passing predecessor scenarios."""
    summary = record["summary"]
    scenarios = record["scenarios"]
    count = len(scenarios)
    return (count > 0 and summary["required"] == summary["passed"] == count
            and not any(summary[key] for key in ("failed", "blocked", "skipped", "notRun"))
            and all(row["result"] == "PASS" and row["exitCode"] == 0 and not row.get("blockers")
                    and row.get("assertionLedger")
                    and all(assertion.get("state") == "PASS" for assertion in row["assertionLedger"])
                    for row in scenarios))


def v2_10_1_facts(
    repository: Path, candidate: str, contract_path: str,
    contract: dict[str, Any], authority: dict[str, Any], scenario_records: dict[str, dict[str, Any]],
    validator: Any,
) -> dict[str, Any]:
    """Bind the two neutral projects, five measured results, and hard entry records."""
    findings: list[dict[str, str]] = []
    predecessors = []
    for story_id in ("7.4", "9.2"):
        json_path = f"docs/release-evidence/story-{story_id}-final-record-v2.json"
        markdown_path = f"docs/release-evidence/story-{story_id}-final-record-v2.md"
        digest = v2_verified_predecessor(repository, candidate, story_id, json_path,
                                         markdown_path, validator)
        if digest is None:
            findings.append(v2_finding("AUTHORITY_BINDING_INVALID", json_path,
                                       "the committed predecessor pair is missing or invalid"))
            continue
        previous = v2_parse_json(v2_committed_blob(repository, candidate, json_path) or b"")
        authority_keys = ("epic", "architecture", "planningCandidate", "bundleDigest")
        compatible_authority = all(previous["authority"].get(key) == authority.get(key)
                                   for key in authority_keys)
        predecessor_candidate = previous["candidate"]["commit"]
        if (not v2_10_1_predecessor_passes(previous)
                or not compatible_authority
                or try_resolve_commit(repository, predecessor_candidate) != predecessor_candidate):
            findings.append(v2_finding("AUTHORITY_BINDING_INVALID", json_path,
                                       "the predecessor is not passing, has mismatched authority, or has an unresolvable candidate"))
        predecessors.append({"storyId": story_id, "path": json_path, "sha256": digest})

    inventory_digest = v2_sha256(b"V8-6.10-AC1\nV8-6.10-AC2\n")
    if (contract["inventory"] != {"id": "V9-10.1-ENTRY-v1", "sha256": inventory_digest}
            or contract["predecessors"] != ["7.4", "9.2"]):
        findings.append(v2_finding("ASSERTION_INVENTORY_DRIFT", contract_path,
                                   "the frozen entry inventory or predecessor order changed"))

    projects = []
    for name in ("Hexalith.Conversations.TestSupport", "Hexalith.Conversations.TestSupport.Tests"):
        path = f"tests/{name}/{name}.csproj"
        assembly_path = f"tests/{name}/bin/Release/net10.0/{name}.dll"
        project = v2_committed_blob(repository, candidate, path)
        if project is None:
            findings.append(v2_finding("TEST_RESULTS_MISSING", path,
                                       "a committed TestSupport project is missing"))
            continue
        try:
            if v2_10_1_artifact_bytes(repository, path) != project:
                raise ValueError("the evaluated project differs from the committed candidate")
            v2_10_1_project_boundary(repository, path, name == "Hexalith.Conversations.TestSupport")
            binary = v2_10_1_artifact_bytes(repository, assembly_path)
            if dotnet_source_revisions(binary) != [candidate]:
                raise ValueError("the assembly is not stamped by SC-10.1")
        except (ValueError, OSError) as error:
            findings.append(v2_finding("TEST_SUPPORT_BOUNDARY_INVALID", path, str(error)))
            continue
        projects.append({
            "project": {"path": path, "sha256": v2_sha256(project)},
            "assembly": {"path": assembly_path, "sha256": v2_sha256(binary)},
        })

    results = []
    for number in range(2, 7):
        scenario_id = f"AC-10.1-0{number}"
        row = scenario_records.get(scenario_id, {})
        expected = f"artifacts/v9/10.1/{scenario_id}.trx"
        result = row.get("resultFile")
        if row.get("result") != "PASS" or not isinstance(result, dict) or result.get("path") != expected:
            findings.append(v2_finding("TEST_RESULTS_MISSING", scenario_id,
                                       "the exact passing TRX result was not measured"))
            continue
        try:
            if v2_sha256(v2_10_1_artifact_bytes(repository, expected)) != result["sha256"]:
                raise ValueError("result digest differs from the measured bytes")
        except (KeyError, ValueError, OSError) as error:
            findings.append(v2_finding("TEST_RESULTS_MISSING", scenario_id, str(error)))
            continue
        results.append(result)
    if findings:
        raise V2Stop(findings, "10.1")
    contract_blob = v2_committed_blob(repository, candidate, contract_path) or b""
    return {
        "bindingRule": "SC-10.1 is HEAD^{commit} at first final-record generation",
        "sourceRevisionId": candidate,
        "contract": {"path": contract_path, "sha256": v2_sha256(contract_blob)},
        "predecessorRecords": predecessors,
        "inventorySha256": inventory_digest,
        "projects": projects,
        "results": results,
    }


V2_10_2_ROOT_SOURCE = "tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs"
V2_10_2_INVENTORY = v2_sha256(b"V8-6.10-AC3\nV8-6.10-AC4\nV8-6.10-AC5\nV8-HR-BOUND-CONTENT-DRIFT\n")
V2_10_2_MUTATIONS = (
    ("AC-10.2-01-HASH", "EVIDENCE_HASH_MISMATCH"),
    ("AC-10.2-01-PATH", "EVIDENCE_PATH_ESCAPE"),
    ("AC-10.2-01-MISSING", "EVIDENCE_FILE_MISSING"),
    ("AC-10.2-02-OUTPUT", "EVIDENCE_OUTPUT_FORBIDDEN"),
    ("AC-10.2-03-SIGNABLE", "SIGNABLE_PAYLOAD_MISMATCH"),
    ("AC-10.2-04-SUBSET", "CHANGED_PATH_SET_MISMATCH"),
    ("AC-10.2-04-SUPERSET", "CHANGED_PATH_SET_MISMATCH"),
    ("AC-10.2-05-GITLINK", "EVIDENCE_BOUNDARY_GITLINK"),
    ("AC-10.2-06-HISTORY", "UNAVAILABLE_HISTORY_PASSED"),
    ("AC-10.2-06-EMPTY", "ASSERTION_LEDGER_EMPTY"),
    ("AC-10.2-07-ROOT", "ROOT_OF_TRUST_NOT_PINNED"),
    ("AC-10.2-07-ALLOWLIST", "SIGNED_EVIDENCE_ALLOWLISTED"),
)


V2_CSHARP_LINE_TERMINATORS = "\r\n\u0085\u2028\u2029"


def v2_csharp_whitespace(character: str) -> bool:
    return character in "\t\v\f\ufeff\u001a" or unicodedata.category(character) == "Zs"


def v2_csharp_line_end(source: str, index: int) -> int:
    while index < len(source) and source[index] not in V2_CSHARP_LINE_TERMINATORS:
        index += 1
    return index


def v2_csharp_unconditional_tokens(source: str) -> list[str]:
    """Lex declarations, excluding comments, string contents, and every conditional region."""
    tokens, index, depth = [], 0, 0
    while index < len(source):
        current = source[index]
        if v2_csharp_whitespace(current) or current in V2_CSHARP_LINE_TERMINATORS:
            index += 1
            continue
        line_start = max(source.rfind(terminator, 0, index) for terminator in V2_CSHARP_LINE_TERMINATORS) + 1
        if current == "#" and all(v2_csharp_whitespace(character) for character in source[line_start:index]):
            end = v2_csharp_line_end(source, index)
            name_start = index + 1
            while name_start < end and v2_csharp_whitespace(source[name_start]):
                name_start += 1
            name_end = name_start
            while name_end < end and (source[name_end].isalnum() or source[name_end] == "_"):
                name_end += 1
            directive = source[name_start:name_end]
            if directive in {"if", "elif", "else", "endif"}:
                tokens.append("<conditional>")
            if directive == "if":
                depth += 1
            elif directive == "endif" and depth:
                depth -= 1
            index = end
            continue
        # Disabled text is opaque; quotes and comment markers cannot hide nested directives.
        if depth:
            index = v2_csharp_line_end(source, index)
            continue
        if source.startswith("//", index):
            index = v2_csharp_line_end(source, index + 2)
            continue
        if source.startswith("/*", index):
            end = source.find("*/", index + 2)
            index = len(source) if end < 0 else end + 2
            continue
        start, quote = index, index
        while quote < len(source) and source[quote] in "$@":
            quote += 1
        if quote < len(source) and source[quote] == '"':
            index, literal = v2_csharp_read_string(source, start, quote)
            if not depth:
                tokens.append(source[start:index] if literal else "<string>")
            continue
        if current == "'":
            index += 1
            while index < len(source):
                character = source[index]
                index += 1
                if character == "\\":
                    index = min(len(source), index + 1)
                elif character == "'":
                    break
            if not depth:
                tokens.append("<character>")
            continue
        index += 1
        if current.isalpha() or current == "_":
            while index < len(source) and (source[index].isalnum() or source[index] == "_"):
                index += 1
        if not depth:
            tokens.append(source[start:index])
    return tokens


def v2_csharp_read_string(source: str, start: int, quote: int) -> tuple[int, bool]:
    """Treat interpolated expressions and their nested literals as opaque string contents."""
    verbatim, dollars = "@" in source[start:quote], source[start:quote].count("$")
    quotes = 0
    while quote + quotes < len(source) and source[quote + quotes] == '"':
        quotes += 1
    raw = not verbatim and quotes >= 3
    literal = start == quote and not raw
    index = quote + (quotes if raw else 1)
    while index < len(source):
        if dollars and source[index] == "{":
            braces = 0
            while index + braces < len(source) and source[index + braces] == "{":
                braces += 1
            if raw and braces >= dollars or not raw and braces % 2:
                index = v2_csharp_skip_interpolation(source, index + braces, dollars if raw else 1)
            else:
                index += braces
            continue
        if source[index] == '"':
            if raw:
                count = 0
                while index + count < len(source) and source[index + count] == '"':
                    count += 1
                if count >= quotes:
                    return index + quotes, literal
                index += count
                continue
            if verbatim and source.startswith('""', index):
                index += 2
                continue
            return index + 1, literal
        if not raw and not verbatim and source[index] == "\\":
            literal, index = False, min(len(source), index + 2)
        else:
            index += 1
    return index, False


def v2_csharp_skip_interpolation(source: str, index: int, closing_braces: int) -> int:
    depth, expression_groups, formatting = 0, 0, False
    while index < len(source):
        # Format text is opaque: apostrophes, quotes, and comment markers cannot start expressions.
        if formatting:
            if source[index] == "}":
                count = 0
                while index + count < len(source) and source[index + count] == "}":
                    count += 1
                if count >= closing_braces:
                    return index + closing_braces
            index += 1
            continue
        if source[index] in "([":
            expression_groups += 1
            index += 1
            continue
        if source[index] in ")]":
            expression_groups = max(0, expression_groups - 1)
            index += 1
            continue
        if source[index] == ":" and not depth and not expression_groups:
            if source.startswith("::", index):
                index += 2
                continue
            formatting = True
            index += 1
            continue
        if source[index] == "{":
            depth += 1
            index += 1
            continue
        if source[index] == "}":
            count = 0
            while index + count < len(source) and source[index + count] == "}":
                count += 1
            if not depth and count >= closing_braces:
                return index + closing_braces
            if depth:
                depth -= 1
            index += 1
            continue
        if source.startswith("//", index) or source.startswith("/*", index):
            line = source.startswith("//", index)
            end = v2_csharp_line_end(source, index + 2) if line else source.find("*/", index + 2)
            index = len(source) if end < 0 else end + (0 if line else 2)
            continue
        quote = index
        while quote < len(source) and source[quote] in "$@":
            quote += 1
        if quote < len(source) and source[quote] == '"':
            index, _ = v2_csharp_read_string(source, index, quote)
            continue
        if source[index] == "'":
            index += 1
            while index < len(source):
                character = source[index]
                index += 1
                if character == "\\":
                    index = min(len(source), index + 1)
                elif character == "'":
                    break
            continue
        index += 1
    return index


def v2_csharp_has_sequence(tokens: list[str], declaration: list[str]) -> bool:
    return any(tokens[index:index + len(declaration)] == declaration
               for index in range(len(tokens) - len(declaration) + 1))


def v2_10_2_root_errors(roots: Any, constants: dict[str, str] | None, source_bytes: bytes | None) -> list[dict[str, str]]:
    """Check measured roots against independently verified facts and active literal declarations."""
    try:
        if (not isinstance(roots, dict) or set(roots) != {"sourcePath", "constants", "allowlist"}
                or roots.get("sourcePath") != V2_10_2_ROOT_SOURCE
                or constants is None or roots.get("constants") != constants or source_bytes is None):
            raise ValueError("root output differs from the independently verified predecessor")
        tokens = v2_csharp_unconditional_tokens(source_bytes.decode("utf-8-sig"))
        for name, value in constants.items():
            if not v2_csharp_has_sequence(tokens, ["const", "string", name, "=", f'"{value}"', ";"]):
                raise ValueError(f"{name} is not an unconditional literal declaration in the committed consuming source")
        if roots["allowlist"] != [] or not v2_csharp_has_sequence(
                tokens, ["string", "[", "]", "SupersessionAllowlist", "=", "[", "]", ";"]):
            return [v2_finding("SIGNED_EVIDENCE_ALLOWLISTED", V2_10_2_ROOT_SOURCE, "the real allowlist must start empty")]
    except (ValueError, UnicodeDecodeError) as error:
        return [v2_finding("ROOT_OF_TRUST_NOT_PINNED", V2_10_2_ROOT_SOURCE, str(error))]
    return []


def v2_10_2_repository_pair_errors(repository: Path, record: dict[str, Any], validator: Any) -> list[str]:
    """Reverify a consumed pair against its committed predecessor, source, and measured commands."""
    try:
        candidate = record["candidate"]["commit"]
        if try_resolve_commit(repository, candidate) != candidate:
            return ["Story 10.2 candidate history is unavailable"]
        support = record["evidenceBoundaryExecution"]
        predecessor_path = "docs/release-evidence/story-10.1-final-record-v2.json"
        digest = v2_verified_predecessor(repository, candidate, "10.1", predecessor_path,
                                         "docs/release-evidence/story-10.1-final-record-v2.md", validator)
        if digest is None or digest != support["predecessorRecords"][0]["sha256"]:
            return ["Story 10.2 committed predecessor pair differs from its digest binding"]
        previous = v2_parse_json(v2_committed_blob(repository, candidate, predecessor_path) or b"")
        if (not v2_10_1_predecessor_passes(previous)
                or try_resolve_commit(repository, previous["candidate"]["commit"]) != previous["candidate"]["commit"]
                or any(previous["authority"].get(key) != record["authority"].get(key)
                       for key in ("epic", "architecture", "planningCandidate", "bundleDigest"))):
            return ["Story 10.2 committed predecessor is nonpassing, incompatible, or unresolvable"]
        constants = {"Story101RecordSha256": digest, "Story101CandidateCommit": previous["candidate"]["commit"]}
        problems = [row["message"] for row in v2_10_2_root_errors(support["rootConstants"], constants,
                     v2_committed_blob(repository, candidate, V2_10_2_ROOT_SOURCE))]
        for row in support["projects"]:
            project = row["project"]
            project_bytes = v2_committed_blob(repository, candidate, project["path"])
            if project_bytes is None or v2_sha256(project_bytes) != project["sha256"]:
                problems.append("Story 10.2 project digest differs from its committed candidate blob")
        gitlinks = [{"path": path, "mode": "160000", "commit": commit}
                    for path, commit in v2_raw_gitlinks(repository, candidate)]
        if record["candidate"]["gitlinks"] != gitlinks:
            problems.append("Story 10.2 gitlink inventory differs from its committed candidate tree")
        contract_bytes = v2_committed_blob(repository, candidate, support["contract"]["path"])
        if contract_bytes is None or v2_sha256(contract_bytes) != support["contract"]["sha256"]:
            problems.append("Story 10.2 committed contract differs from its hash binding")
        else:
            contract = v2_parse_json(contract_bytes)
            expected = [(row["id"], row["command"]) for row in contract["scenarios"]]
            measured = [(row["scenarioId"], row["command"]) for row in record["scenarios"]]
            if contract["storyId"] != "10.2" or measured != expected:
                problems.append("Story 10.2 measured scenario commands differ from the committed contract")
        return problems
    except (ValueError, KeyError, TypeError, IndexError, AttributeError):
        return ["Story 10.2 committed consumption bindings are malformed"]


def v2_10_2_marked_output(content: bytes, marker: str) -> Any:
    """Read one marked JSON line from the sole passing TRX row, never a side file."""
    parsed = parse_trx(content)
    if (count_disagreements(parsed) or parsed["reported"]["executed"] != 1
            or parsed["reported"]["passed"] != 1 or len(parsed["results"]) != 1
            or parsed["results"][0]["outcome"] not in TRX_PASSED_OUTCOMES):
        raise ValueError("the marker must belong to exactly one passing result")
    lines = [line[len(marker):] for line in parsed["results"][0].get("output", "").splitlines()
             if line.startswith(marker)]
    if len(lines) != 1:
        raise ValueError(f"exactly one {marker} line is required")
    return v2_parse_json(lines[0].encode("utf-8"))


def v2_10_2_mutation_errors(rows: Any) -> list[dict[str, str]]:
    """Require the contracted twelve observations, detections, and byte restorations."""
    findings = []
    keys = {"id", "expectedBlocker", "observedBlockers", "beforeSha256", "afterSha256", "detected", "restored"}
    if (not isinstance(rows, list) or len(rows) != len(V2_10_2_MUTATIONS)
            or not all(isinstance(row, dict) for row in rows)):
        return [v2_finding("FAULT_NOT_DETECTED", "mutation ledger", "exactly twelve measured mutation rows are required")]
    for row, (identity, expected) in zip(rows, V2_10_2_MUTATIONS):
        observed = row.get("observedBlockers")
        if (set(row) != keys or row.get("id") != identity or row.get("expectedBlocker") != expected
                or row.get("detected") is not True or not isinstance(observed, list)
                or not all(isinstance(code, str) and re.fullmatch(r"[A-Z][A-Z0-9_]*", code) for code in observed)
                or observed != sorted(set(observed)) or expected not in observed):
            findings.append(v2_finding("FAULT_NOT_DETECTED", identity, "the exact mutation and expected blocker were not observed"))
        before, after = row.get("beforeSha256"), row.get("afterSha256")
        if (row.get("restored") is not True or not isinstance(before, str)
                or re.fullmatch(r"[0-9a-f]{64}", before) is None or before != after):
            findings.append(v2_finding("FIXTURE_NOT_RESTORED", identity, "the before and restored byte hashes differ or are absent"))
    return findings


def v2_10_2_facts(
    repository: Path, candidate: str, contract_path: str, contract: dict[str, Any],
    authority: dict[str, Any], scenario_records: dict[str, dict[str, Any]], validator: Any,
) -> dict[str, Any]:
    """Recompute candidate, predecessor, projects, eight receipts, roots, and fault observations."""
    findings = []
    json_path = "docs/release-evidence/story-10.1-final-record-v2.json"
    digest = v2_verified_predecessor(repository, candidate, "10.1", json_path,
                                     "docs/release-evidence/story-10.1-final-record-v2.md", validator)
    previous = None
    if digest is None:
        findings.append(v2_finding("AUTHORITY_BINDING_INVALID", json_path, "the committed Story 10.1 pair does not verify"))
    else:
        previous = v2_parse_json(v2_committed_blob(repository, candidate, json_path) or b"")
        if (not v2_10_1_predecessor_passes(previous)
                or any(previous["authority"].get(key) != authority.get(key)
                       for key in ("epic", "architecture", "planningCandidate", "bundleDigest"))
                or try_resolve_commit(repository, previous["candidate"]["commit"]) != previous["candidate"]["commit"]):
            findings.append(v2_finding("AUTHORITY_BINDING_INVALID", json_path, "the predecessor is nonpassing, incompatible, or unresolvable"))
    if (contract.get("inventory") != {"id": "V9-10.2-ENTRY-v1", "sha256": V2_10_2_INVENTORY}
            or contract.get("predecessors") != ["10.1"]):
        findings.append(v2_finding("ASSERTION_INVENTORY_DRIFT", contract_path, "the frozen inventory or predecessor changed"))

    projects = []
    helper_mtime_ns = 0
    for name in ("Hexalith.Conversations.TestSupport", "Hexalith.Conversations.TestSupport.Tests"):
        path = f"tests/{name}/{name}.csproj"
        assembly_path = f"tests/{name}/bin/Release/net10.0/{name}.dll"
        project = v2_committed_blob(repository, candidate, path)
        try:
            if project is None or v2_10_1_artifact_bytes(repository, path) != project:
                raise ValueError("the evaluated project is missing or differs from the candidate")
            v2_10_1_project_boundary(repository, path, name == "Hexalith.Conversations.TestSupport")
            binary = v2_10_1_artifact_bytes(repository, assembly_path)
            if dotnet_source_revisions(binary) != [candidate]:
                raise ValueError("the assembly is not stamped by SC-10.2")
            if name == "Hexalith.Conversations.TestSupport":
                runtime_path = "tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll"
                runtime = v2_10_1_artifact_bytes(repository, runtime_path)
                if runtime != binary or dotnet_source_revisions(runtime) != [candidate]:
                    raise ValueError("the runtime helper copy differs from the bound assembly or candidate stamp")
                helper_mtime_ns = max((repository / assembly_path).stat().st_mtime_ns,
                                      (repository / runtime_path).stat().st_mtime_ns)
        except (ValueError, OSError) as error:
            findings.append(v2_finding("TEST_SUPPORT_BOUNDARY_INVALID", path, str(error)))
            continue
        projects.append({"project": {"path": path, "sha256": v2_sha256(project)},
                         "assembly": {"path": assembly_path, "sha256": v2_sha256(binary)}})
    results, receipts = [], {}
    for number in range(1, 9):
        identity = f"AC-10.2-0{number}"
        expected = f"artifacts/v9/10.2/{identity}.trx"
        row = scenario_records.get(identity, {})
        result = row.get("resultFile")
        try:
            if row.get("result") != "PASS" or not isinstance(result, dict) or result.get("path") != expected:
                raise ValueError("the exact passing TRX receipt was not measured")
            content = v2_10_1_artifact_bytes(repository, expected)
            if (repository / expected).stat().st_mtime_ns < helper_mtime_ns:
                findings.append(v2_finding("TEST_RESULTS_STALE", identity,
                                           "the acceptance receipt predates a bound or runtime helper assembly"))
                continue
            if v2_sha256(content) != result.get("sha256"):
                raise ValueError("the measured TRX digest differs from current bytes")
            parsed = parse_trx(content)
            if (count_disagreements(parsed) or parsed["reported"]["total"] != 1
                    or parsed["reported"]["executed"] != 1 or parsed["reported"]["passed"] != 1):
                raise ValueError("each acceptance receipt must contain exactly one passing row")
        except (ValueError, OSError, ElementTree.ParseError) as error:
            findings.append(v2_finding("TEST_RESULTS_MISSING", identity, str(error)))
            continue
        results.append(result)
        receipts[identity] = content

    roots, mutations = None, None
    for identity, marker in (("AC-10.2-07", "STORY102_ROOTS:"), ("AC-10.2-08", "STORY102_MUTATIONS:")):
        try:
            marked = v2_10_2_marked_output(receipts[identity], marker)
            if identity.endswith("07"):
                roots = marked
            else:
                mutations = marked
        except (KeyError, ValueError, ElementTree.ParseError) as error:
            findings.append(v2_finding("TEST_RESULTS_MISSING", identity, str(error)))
    if roots is not None:
        expected_constants = None if previous is None else {
            "Story101RecordSha256": digest, "Story101CandidateCommit": previous["candidate"]["commit"]}
        findings.extend(v2_10_2_root_errors(roots, expected_constants,
                        v2_committed_blob(repository, candidate, V2_10_2_ROOT_SOURCE)))
    if mutations is not None:
        findings.extend(v2_10_2_mutation_errors(mutations))
    if findings:
        raise V2Stop(findings, "10.2")
    return {
        "bindingRule": "SC-10.2 is HEAD^{commit} at first final-record generation",
        "sourceRevisionId": candidate,
        "contract": {"path": contract_path, "sha256": v2_sha256(v2_committed_blob(repository, candidate, contract_path) or b"")},
        "predecessorRecords": [{"storyId": "10.1", "path": json_path, "sha256": digest}],
        "inventorySha256": V2_10_2_INVENTORY, "projects": projects, "results": results,
        "rootConstants": roots, "mutationLedger": mutations,
    }


def v2_story_7_3_workflow_integration(
    repository: Path,
    candidate: str,
    contract_path: str,
    acceptance_inputs: dict[str, list[dict[str, str]]],
    validators: dict[str, Any],
) -> dict[str, Any]:
    """Bind Story 7.3 to its contract, governed workflow bodies, and predecessor records.

    Each governed body digest is re-derived from the candidate's committed blob
    and must equal the inputs of every workflow acceptance result exactly.
    """
    findings: list[dict[str, str]] = []
    bodies: list[dict[str, str]] = []
    for path in V2_7_3_WORKFLOW_BODIES:
        blob = v2_committed_blob(repository, candidate, path)
        if blob is None:
            findings.append(
                v2_finding(
                    "WORKFLOW_INTEGRATION_MISSING",
                    path,
                    "a governed completion-route body is not committed at the candidate",
                )
            )
            continue
        bodies.append({"path": path, "sha256": v2_sha256(blob)})
    if not acceptance_inputs:
        findings.append(
            v2_finding(
                "SCENARIO_COMMAND_UNSUPPORTED",
                "7.3",
                "the contract declares no acceptance-result scenario that binds the "
                "workflow bodies",
            )
        )
    for scenario_id, inputs in sorted(acceptance_inputs.items()):
        if [row["path"] for row in inputs] != list(V2_7_3_WORKFLOW_BODIES):
            findings.append(
                v2_finding(
                    "SCENARIO_RESULT_MISMATCH",
                    scenario_id,
                    "the acceptance result's inputs are not exactly the governed workflow bodies "
                    "in ordinal order",
                )
            )
        elif len(bodies) == len(V2_7_3_WORKFLOW_BODIES) and inputs != bodies:
            findings.append(
                v2_finding(
                    "TEST_RESULTS_STALE",
                    scenario_id,
                    "the acceptance result's body digests differ from the candidate's bodies",
                )
            )
    contract_blob = v2_committed_blob(repository, candidate, contract_path) or b""
    predecessors = []
    predecessor_digests: dict[str, str] = {}
    for story_id, json_path, markdown_path in V2_7_3_PREDECESSOR_RECORDS:
        digest = v2_verified_predecessor(
            repository, candidate, story_id, json_path, markdown_path, validators["record"]
        )
        if digest is None:
            findings.append(
                v2_finding(
                    "AUTHORITY_BINDING_INVALID",
                    f"predecessor {story_id}",
                    f"the committed Story {story_id} record pair is missing or does not verify",
                )
            )
            continue
        if story_id == "7.2":
            try:
                story_7_2 = v2_parse_json(
                    v2_committed_blob(repository, candidate, json_path) or b""
                )
                expected_link = {
                    "storyId": "7.1",
                    "path": V2_7_1_RECORD_PATH,
                    "sha256": predecessor_digests["7.1"],
                }
                if story_7_2["measurements"]["predecessorRecord"] != expected_link:
                    raise ValueError("Story 7.2 does not bind the verified Story 7.1 record")
            except (UnicodeDecodeError, ValueError, TypeError, KeyError, AttributeError):
                findings.append(
                    v2_finding(
                        "AUTHORITY_BINDING_INVALID",
                        "predecessor 7.2",
                        "the committed Story 7.2 record does not bind the verified Story 7.1 "
                        "record digest",
                    )
                )
                continue
        predecessor_digests[story_id] = digest
        predecessors.append({"storyId": story_id, "path": json_path, "sha256": digest})
    if findings:
        raise V2Stop(findings, "7.3")
    return {
        "contract": {"path": contract_path, "sha256": v2_sha256(contract_blob)},
        "workflowBodies": bodies,
        "predecessorRecords": predecessors,
    }


def v2_history_require_object(repository: Path, object_id: str, subject: str) -> None:
    """Distinguish Git's explicit missing-object result from execution/environment errors."""
    result = run_git(repository, "cat-file", "-e", object_id, allowed_returncodes=(0, 1, 128))
    if result.returncode == 1 and not result.stderr.strip():
        raise V2Stop([v2_finding("HISTORICAL_BLOB_UNRESOLVED", subject,
                                "a recorded historical Git object is missing")], "7.4")
    if result.returncode != 0:
        raise GateError("GIT_COMMAND_FAILED", "Git could not inspect a historical object")


def v2_history_require_trees(repository: Path, tree: str) -> None:
    """On a failed tree read, locate a missing root/nested tree without opening gitlinks."""
    v2_history_require_object(repository, tree, tree)
    entries = run_git(repository, "ls-tree", "-z", tree).stdout.split(b"\0")
    for entry in entries:
        if not entry:
            continue
        metadata, _, _ = entry.partition(b"\t")
        fields = metadata.split()
        if len(fields) != 3:
            raise GateError("GIT_COMMAND_FAILED", "Git returned an invalid historical tree entry")
        if fields[1] == b"tree":
            v2_history_require_trees(repository, decode(fields[2]))


def v2_history_committed_blob(repository: Path, revision: str, path: str) -> bytes | None:
    """Read a historical blob; reclassify only an explicitly absent object as FAIL."""
    try:
        return v2_committed_blob(repository, revision, path)
    except GateError as error:
        if error.code == "GIT_COMMAND_FAILED":
            _, object_id = tree_entry(repository, revision, path)
            if object_id is not None:
                v2_history_require_object(repository, object_id, path)
        raise


def v2_history_revision(repository: Path, revision: str) -> dict[str, Any]:
    """Read a root commit's tree and gitlinks without opening submodule objects."""
    # The legacy try-resolver swallows all Git errors. Historical verification
    # must preserve execution/environment failures as BLOCKED instead.
    v2_history_require_object(repository, revision, revision)
    try:
        commit = resolve_commit(repository, revision, "HISTORICAL_BLOB_UNRESOLVED")
    except GateError as error:
        if error.code == "HISTORICAL_BLOB_UNRESOLVED":
            object_type = decode(run_git(repository, "cat-file", "-t", revision).stdout).strip()
            if object_type != "commit":
                raise V2Stop([v2_finding("HISTORICAL_BLOB_UNRESOLVED", revision,
                                        "a historical root revision is not a commit")], "7.4") from None
            raise GateError("GIT_COMMAND_FAILED", "Git could not resolve an existing historical commit") from error
        raise
    if commit != revision:
        raise V2Stop([v2_finding("HISTORICAL_BLOB_UNRESOLVED", revision,
                                "a recorded historical root commit cannot be resolved")], "7.4")
    commit_bytes = run_git(repository, "cat-file", "commit", commit).stdout
    header = re.match(rb"tree ([0-9a-f]{40})\n", commit_bytes)
    if header is None:
        raise GateError("GIT_COMMAND_FAILED", "a historical commit has no valid tree header")
    tree = decode(header.group(1))
    try:
        links = v2_raw_gitlinks(repository, commit)
    except GateError as error:
        if error.code == "GIT_COMMAND_FAILED":
            v2_history_require_trees(repository, tree)
        raise
    # The legacy historical verifier reads each recorded root .gitmodules blob.
    # Detect an absent object before that legacy reader would classify it as a
    # generic Git failure. Absence of the path remains the verifier's concern.
    v2_history_committed_blob(repository, commit, ".gitmodules")
    return {
        "commit": commit,
        "tree": tree,
        "gitlinks": [{"path": path, "mode": "160000", "commit": oid}
                     for path, oid in links],
    }


def v2_history_blob(repository: Path, revision: str, path: str,
                    roots: Sequence[str] | None = None) -> dict[str, str]:
    """Bind an ordinary committed blob, including its path mode and object ID."""
    path = safe_relative_path(path)
    if v2_below(path, roots if roots is not None else root_submodule_paths(repository, revision)):
        raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", path,
                                "historical blob bindings must remain outside submodules")], "7.4")
    mode, oid = tree_entry(repository, revision, path)
    content = v2_history_committed_blob(repository, revision, path)
    if mode not in ("100644", "100755") or oid is None or content is None:
        raise V2Stop([v2_finding("HISTORICAL_BLOB_UNRESOLVED", path,
                                "a historical path does not resolve to an ordinary committed blob")], "7.4")
    return {"revision": revision, "path": path, "mode": mode, "blob": oid,
            "sha256": v2_sha256(content)}


def v2_history_record(repository: Path, story_id: str, path: str, closure: str,
                      document: dict[str, Any]) -> dict[str, Any]:
    """Derive the fixture facts from closed bytes and root objects, never transient results."""
    closure_revision = v2_history_revision(repository, closure)
    baseline = document["baseline"]
    candidate = document["candidate"]
    revisions = list(dict.fromkeys(value for value in (candidate or closure, baseline, closure)
                                   if value is not None))
    roots = set().union(*(root_submodule_paths(repository, revision) for revision in revisions))
    blobs = []
    for listed in sorted(set(document["record"]["declared_file_list"])):
        if listed in roots or v2_below(listed, sorted(roots)):
            continue  # pre-generator warnings are preserved; no submodule traversal
        revision = next((rev for rev in revisions if tree_entry(repository, rev, listed)[0]), None)
        if revision is None:
            raise V2Stop([v2_finding("HISTORICAL_BLOB_UNRESOLVED", listed,
                                    "a recorded root path resolves at none of the recorded revisions")], "7.4")
        blobs.append(v2_history_blob(repository, revision, listed, sorted(roots)))
    content = (v2_history_committed_blob(repository, closure, path) or b"").decode("utf-8")
    archived = []
    # These are declarations archived in committed Markdown, not original TRX,
    # binaries, or live promotion-checker results. Preserve the literal sections.
    for heading in ("Test Results", "Test Build Manifest", "Promotion Completion Gate"):
        match = re.search(rf"^### {heading}\n(.*?)(?=^### |^<!-- STORY-FINAL-RECORD:END -->|\Z)",
                          content, re.MULTILINE | re.DOTALL)
        if match:
            archived.append({"kind": heading, "status": "recorded-only",
                             "declaration": match.group(1).strip()})
    if not archived:
        for heading in ("Verification", "Completion Notes List"):
            match = re.search(rf"^#{{2,3}} {heading}\n(.*?)(?=^#{{2,3}} |\Z)",
                              content, re.MULTILINE | re.DOTALL)
            if match and match.group(1).strip():
                archived.append({"kind": heading, "status": "recorded-only",
                                 "declaration": match.group(1).strip()})
    identities = []
    def evidence_identity(value: Any, pointer: str) -> None:
        if isinstance(value, dict):
            for key, child in sorted(value.items()):
                location = pointer + "/" + key.replace("~", "~0").replace("/", "~1")
                if (isinstance(child, (str, int, bool))
                        and re.search(r"(?:run|result|gate|candidate|baseline|commit|revision)", key, re.IGNORECASE)):
                    identities.append({"subject": location, "value": str(child)})
                else:
                    evidence_identity(child, location)
        elif isinstance(value, list):
            for ordinal, child in enumerate(value):
                evidence_identity(child, pointer + f"/{ordinal}")
    for blob in blobs:
        if blob["path"].startswith("docs/release-evidence/") and blob["path"].endswith(".json"):
            evidence = v2_parse_json(v2_history_committed_blob(repository, blob["revision"], blob["path"]))
            evidence_identity(evidence, blob["path"] + "#")
    return {
        "storyId": story_id, "classification": document["classification"],
        "closure": closure_revision,
        "baseline": v2_history_revision(repository, baseline) if baseline else None,
        "recordedCandidate": v2_history_revision(repository, candidate) if candidate else None,
        "record": v2_history_blob(repository, closure, path),
        "boundBlobs": blobs,
        "archivedDeclarations": archived,
        "evidenceIdentities": identities,
        "warnings": sorted({row["code"] for row in document["warnings"]}),
    }


def v2_history_resolved_path(repository: Path, relative: str, roots: Sequence[str],
                             *, output: bool = False) -> Path:
    """Reject a lexical or resolved historical path that crosses a root gitlink."""
    code = "OUTPUT_PATH_INVALID" if output else "HISTORICAL_RECORD_DRIFT"
    try:
        relative = safe_relative_path(relative)
        resolved = (repository / relative).resolve(strict=False)
        physical = resolved.relative_to(repository).as_posix()
    except OSError:
        raise V2Stop([v2_finding("SURFACE_UNREADABLE", relative,
                                "the historical path could not be resolved")], "7.4") from None
    except (GateError, ValueError, RuntimeError):
        raise V2Stop([v2_finding(code, relative,
                                "the historical path must remain inside the root repository")], "7.4") from None
    if relative in roots or physical in roots or v2_below(relative, roots) or v2_below(physical, roots):
        raise V2Stop([v2_finding(code, relative,
                                "the historical path resolves into a root gitlink")], "7.4")
    return resolved


def v2_history_installed_snapshot(repository: Path, path: str, roots: Sequence[str]
                                   ) -> tuple[bytes, int] | None:
    """Read closed bytes stably, preserving environmental read errors as BLOCKED."""
    resolved = v2_history_resolved_path(repository, path, roots)
    if (repository / path).is_symlink():
        return None
    try:
        return read_file_snapshot(resolved)
    except FileNotFoundError:
        return None
    except OSError:
        raise V2Stop([v2_finding("SURFACE_UNREADABLE", path,
                                "the installed closed record could not be read stably")], "7.4") from None


def v2_history_facts(repository: Path, candidate: str, validators: dict[str, Any]
                     ) -> tuple[dict[str, Any], list[dict[str, str]], list[dict[str, str]]]:
    """Verify frozen closures, recorded roots, modes and blob bindings read-only."""
    fixture_bytes = v2_committed_blob(repository, candidate, V2_HISTORY_FIXTURE_PATH)
    try:
        fixture = v2_parse_json(fixture_bytes or b"")
        valid = (isinstance(fixture, dict)
                 and set(fixture) == {"schemaVersion", "records", "limits"}
                 and fixture["schemaVersion"] == "hexalith.conversations.story-history-fixture.v1"
                 and fixture["limits"] == list(V2_HISTORY_LIMITS)
                 and len(fixture["records"]) == len(V2_HISTORY_ANCHORS))
        historical_validator = validators["record"].evolve(schema={
            "$defs": validators["record"].schema["$defs"], "$ref": "#/$defs/historicalRecord"})
        valid = valid and all(not v2_schema_errors(historical_validator, row)
                              for row in fixture["records"])
    except (UnicodeError, ValueError, KeyError, TypeError):
        valid = False
    if not valid:
        raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", V2_HISTORY_FIXTURE_PATH,
                                "the committed historical fixture is missing or malformed")], "7.4")
    inputs = [{"path": V2_HISTORY_FIXTURE_PATH, "sha256": v2_sha256(fixture_bytes)}]
    roots = [path for path, _ in v2_raw_gitlinks(repository, candidate)]
    records = []
    subjects = ["history::" + limit for limit in V2_HISTORY_LIMITS]
    for pinned, (story_id, name, closure) in zip(fixture["records"], V2_HISTORY_ANCHORS):
        path = "_bmad-output/implementation-artifacts/" + name
        if (pinned["storyId"] != story_id or pinned["closure"]["commit"] != closure
                or pinned["record"]["path"] != path):
            raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", path,
                                    "historical records must bind the three frozen closure anchors in order")], "7.4")
        v2_history_revision(repository, closure)
        for revision in (pinned["baseline"], pinned["recordedCandidate"]):
            if revision is not None:
                v2_history_revision(repository, revision["commit"])
        closed = v2_history_committed_blob(repository, closure, path)
        current = v2_committed_blob(repository, candidate, path)
        installed = v2_history_installed_snapshot(repository, path, roots)
        if closed is None:
            raise V2Stop([v2_finding("HISTORICAL_BLOB_UNRESOLVED", path,
                                    "the closure commit does not contain its closed record")], "7.4")
        if (current != closed or installed is None or installed[0] != closed
                or tree_entry(repository, candidate, path) != tree_entry(repository, closure, path)):
            raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", path,
                                    "current closed-record bytes differ from the closure commit")], "7.4")
        # Equality above makes the existing read-only verifier's worktree read
        # identical to the closure blob. Its pre-generator disposition is retained.
        v2_history_resolved_path(repository, path, roots)
        # The legacy verifier converts Git failures into findings; recover them
        # so a timeout stays BLOCKED instead of reading as historical drift.
        failures: list[GateError] = []
        GIT_FAILURE_OBSERVERS.append(failures)
        try:
            document = verify_historical(repository, argparse.Namespace(story=path))
        finally:
            GIT_FAILURE_OBSERVERS.remove(failures)
        if failures:
            raise GateError("GIT_COMMAND_FAILED", "Git failed while the legacy verifier read a closed record")
        if v2_history_installed_snapshot(repository, path, roots) != installed:
            raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", path,
                                    "closed-record bytes changed while the legacy verifier reopened them")], "7.4")
        if document["blockers"]:
            raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", path,
                                    "the closed record's committed claims do not verify")], "7.4")
        measured = v2_history_record(repository, story_id, path, closure, document)
        if measured != pinned:
            raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", path,
                                    "historical committed facts differ from their pinned fixture bindings")], "7.4")
        records.append(measured)
        inputs.append({"path": path, "sha256": v2_sha256(closed)})
        subjects.extend([
            f"history::{story_id}::closure-record-bytes-modes-and-digest",
            f"history::{story_id}::root-commits-trees-and-gitlinks",
            f"history::{story_id}::bound-blobs-and-commit-bound-evidence" if measured["evidenceIdentities"]
            else f"history::{story_id}::bound-blobs-no-commit-bound-evidence",
            f"history::{story_id}::archived-declarations-recorded-only",
            f"history::{story_id}::disposition::{measured['classification']}",
        ])
    return ({"fixture": inputs[0], "records": records, "limits": list(V2_HISTORY_LIMITS)},
            inputs, [{"id": f"AC-7.4-01#{ordinal:04d}", "subject": subject, "state": "PASS"}
                     for ordinal, subject in enumerate(subjects, 1)])


def v2_historical(options: dict[str, str]) -> tuple[bytes, int]:
    """Execute the frozen Story 7.4 historical command and emit acceptance-result v1."""
    repository = validate_repository(Path(options.get("--repository") or default_repository()))
    candidate = resolve_commit(repository, "HEAD", "CANDIDATE_UNRESOLVABLE")
    _, validators = v2_load_schemas()
    if options["--contract"] != V2_7_4_CONTRACT_PATH:
        raise V2Stop([v2_finding("ARGUMENT_INVALID", "--contract",
                                "contract historical mode is supported only for Story 7.4")])
    contract = v2_validate_contract(v2_committed_blob(repository, candidate, V2_7_4_CONTRACT_PATH),
                                    V2_7_4_CONTRACT_PATH, validators["contract"])
    scenario = contract["scenarios"][0]
    command = v2_acceptance_command(shlex.split(scenario["command"]))
    if (contract["storyId"] != "7.4" or command is None
            or command["output"] != V2_HISTORY_OUTPUT_PATH
            or options["--output-json"] != V2_HISTORY_OUTPUT_PATH):
        raise V2Stop([v2_finding("CALLER_AUTHORED_FACT", "--output-json",
                                "the historical output must equal the frozen scenario output")], "7.4")
    roots = [path for path, _ in v2_raw_gitlinks(repository, candidate)]
    v2_history_resolved_path(repository, command["output"], roots, output=True)
    try:
        target = v2_output_target(repository, command["output"])
    except (OSError, ValueError):
        raise V2Stop([v2_finding("OUTPUT_PATH_INVALID", "--output-json",
                                "the historical receipt output is not a safe regular root path")], "7.4") from None
    acceptance_validator = v2_load_acceptance_validator()
    document = {"schemaVersion": V2_ACCEPTANCE_SCHEMA_VERSION, "storyId": "7.4",
                "scenarioId": scenario["id"], "command": scenario["command"],
                "candidate": candidate, "inputs": [], "outputs": [],
                "exitCode": 0, "result": "PASS", "blockers": [], "assertionLedger": []}
    try:
        _, inputs, ledger = v2_history_facts(repository, candidate, validators)
        document["inputs"] = inputs
        document["assertionLedger"] = ledger
    except V2Stop as stop:
        failure = v2_failure_document(stop.findings, "7.4")
        document.update(exitCode=failure["exitCode"], result=failure["result"],
                        blockers=failure["blockers"])
    except GateError as error:
        code = error.code if error.code in ("GIT_UNAVAILABLE", "GIT_COMMAND_FAILED") else "INTERNAL_ERROR"
        document.update(exitCode=2, result="BLOCKED", blockers=[code])
    except OSError:
        document.update(exitCode=2, result="BLOCKED", blockers=["SURFACE_UNREADABLE"])
    acceptance_validator.validate(document)
    content = v2_render_json(document)
    try:
        v2_history_resolved_path(repository, command["output"], roots, output=True)
        target = v2_output_target(repository, command["output"])
        v2_write_outputs([(target, content)])
    except (OSError, ValueError, V2OutputDrift):
        raise V2Stop([v2_finding("OUTPUT_WRITE_FAILED", "--output-json",
                                "the historical acceptance output could not be installed")], "7.4") from None
    return content, document["exitCode"]


def v2_story_7_4_verification(repository: Path, candidate: str, contract_path: str,
                             scenarios: dict[str, dict[str, Any]], validators: dict[str, Any]
                             ) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    """Bind historical measurements, the predecessor chain, and all observed faults."""
    history, inputs, ledger = v2_history_facts(repository, candidate, validators)
    def bound_result(scenario_id: str) -> bytes:
        binding = scenarios[scenario_id]["resultFile"]
        try:
            content, _ = read_file_snapshot(contained_file(repository, binding["path"], "result"))
        except (GateError, OSError):
            content = b""
        if v2_sha256(content) != binding["sha256"]:
            raise V2Stop([v2_finding("TEST_RESULTS_STALE", scenario_id,
                                    "the result bytes changed after their scenario digest was measured")], "7.4")
        return content
    historical = v2_parse_json(bound_result("AC-7.4-01"))
    if historical["inputs"] != inputs or historical.get("assertionLedger") != ledger:
        raise V2Stop([v2_finding("HISTORICAL_RECORD_DRIFT", "AC-7.4-01",
                                "the historical acceptance facts do not equal the remeasured closure facts")], "7.4")
    predecessors = []
    for story_id, json_path, markdown_path in (*V2_7_3_PREDECESSOR_RECORDS,
                                               ("7.3", *V2_RETAINED_OUTPUTS["7.3"])):
        digest = v2_verified_predecessor(repository, candidate, story_id, json_path,
                                         markdown_path, validators["record"])
        if digest is None:
            raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", story_id,
                                    "the committed predecessor pair does not verify")], "7.4")
        predecessor = v2_parse_json(v2_committed_blob(repository, candidate, json_path))
        if ((story_id == "7.2" and predecessor["measurements"]["predecessorRecord"] != predecessors[0])
                or (story_id == "7.3" and predecessor["workflowIntegration"]["predecessorRecords"] != predecessors)):
            raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", story_id,
                                    "the predecessor does not bind the verified predecessor chain")], "7.4")
        predecessors.append({"storyId": story_id, "path": json_path, "sha256": digest})
    observed_validator = validators["record"].evolve(schema={
        "$defs": validators["record"].schema["$defs"], "$ref": "#/$defs/observedFault"})
    lanes = []
    for scenario_id in ("AC-7.4-03", "AC-7.4-04"):
        content = bound_result(scenario_id)
        parsed = v2_parse_junit(content)
        rows = []
        for case in parsed["cases"]:
            metadata = [value for name, value in case["properties"] if name == V2_FAULT_PROPERTY]
            if len(metadata) != 1:
                raise V2Stop([v2_finding("FAULT_NOT_DETECTED", scenario_id,
                                        "every required fault testcase must carry exactly one observed result")], "7.4")
            try:
                row = v2_parse_json(metadata[0].encode("utf-8"))
                valid = not v2_schema_errors(observed_validator, row)
            except (ValueError, UnicodeError):
                valid = False
            if not valid:
                raise V2Stop([v2_finding("FAULT_NOT_DETECTED", scenario_id,
                                        "an observed fault result is malformed")], "7.4")
            rows.append(row)
            if not case["name"].endswith(f"[{row['id']}]"):
                raise V2Stop([v2_finding("FAULT_NOT_DETECTED", scenario_id,
                                        "a fault result does not identify its executed testcase parameter")], "7.4")
        if (len(rows) != len(V2_REQUIRED_FAULTS)
                or {row["id"] for row in rows} != set(V2_REQUIRED_FAULTS)):
            raise V2Stop([v2_finding("FAULT_NOT_DETECTED", scenario_id,
                                    "the observed fault set is incomplete, duplicated, or unknown")], "7.4")
        for row in rows:
            required = V2_REQUIRED_FAULTS[row["id"]]
            if (row["expectedBlocker"] != required or required not in row["observedBlockers"]
                    or row["observedExitCode"] != 1):
                raise V2Stop([v2_finding("FAULT_NOT_DETECTED", row["id"],
                                        "the mutation did not observe its exact required blocker with exit 1")], "7.4")
            if row["beforeSha256"] != row["afterSha256"]:
                raise V2Stop([v2_finding("FIXTURE_NOT_RESTORED", row["id"],
                                        "the before and after fixture hashes differ")], "7.4")
        lanes.append(sorted(rows, key=lambda row: list(V2_REQUIRED_FAULTS).index(row["id"])))
    if lanes[0] != lanes[1]:
        raise V2Stop([v2_finding("FIXTURE_NOT_RESTORED", "AC-7.4-04",
                                "the restoration lane does not reproduce the mutation lane measurements")], "7.4")
    return ({"contract": {"path": contract_path, "sha256": v2_sha256(
                v2_committed_blob(repository, candidate, contract_path))},
             **history, "predecessorRecords": predecessors}, lanes[0])


def v2_ux_command(tokens: list[str], scenario_id: str) -> dict[str, str] | None:
    """Recognize Story 8.1's exact frozen Python and xUnit commands."""
    if scenario_id == "AC-8.1-01":
        expected = ["python3", "_bmad/scripts/generate_ux_preservation_disposition.py",
                    "--repository", ".", "--contract", V2_8_1_CONTRACT_PATH,
                    "--output-schema", V2_8_1_DISPOSITION_PATHS[0],
                    "--output-json", V2_8_1_DISPOSITION_PATHS[1],
                    "--output-markdown", V2_8_1_DISPOSITION_PATHS[2]]
        return {"kind": "bundle", "output": V2_8_1_DISPOSITION_PATHS[1]} if tokens == expected else None
    methods = {
        "AC-8.1-02": "SourcesShouldBindCanonicalPathsVersionsAndHashes",
        "AC-8.1-03": "DecisionsShouldProjectTheFrozenInventory",
        "AC-8.1-04": "AcceptanceCriteriaShouldProjectTheFrozenInventory",
        "AC-8.1-05": "DispositionsShouldRemainPreservedAndHistorical",
        "AC-8.1-06": "CandidateShouldContainNoProductionUiChange",
    }
    if scenario_id not in methods:
        return None
    method = "Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest." + methods[scenario_id]
    result = f"artifacts/v9/8.1/{scenario_id}.trx"
    expected = ["dotnet", V2_8_1_TEST_ASSEMBLY, "-automated", "sync", "-failSkips",
                "-method", method, "-trx", result]
    return {"kind": "trx", "output": result, "method": method} if tokens == expected else None


def v2_successor_command(tokens: list[str], contract_path: str) -> dict[str, Any] | None:
    """Classify the command forms declared by successor contracts 8 through 16."""
    if len(tokens) < 3:
        return None
    try:
        if tokens[0] == "dotnet" and tokens[1].endswith(".dll"):
            assembly = safe_relative_path(tokens[1])
            if tokens[2:5] != ["-automated", "sync", "-failSkips"]:
                return None
            selector = None
            selector_kind = None
            result = None
            index = 5
            while index < len(tokens):
                flag = tokens[index]
                if flag in ("-showLiveOutput", "-noColor"):
                    index += 1
                    continue
                if flag not in ("-method", "-class", "-parallel", "-maxThreads", "-reporter", "-trx") or index + 1 >= len(tokens):
                    return None
                value = tokens[index + 1]
                if flag in ("-method", "-class"):
                    if selector is not None or not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_.]*", value):
                        return None
                    selector, selector_kind = value, flag
                elif flag == "-trx":
                    if result is not None:
                        return None
                    result = safe_relative_path(value)
                index += 2
            return ({"kind": "xunit", "assembly": assembly, "output": result,
                     "selector": selector, "selectorKind": selector_kind}
                    if result is not None and result.endswith(".trx") else None)
        if tokens[:2] in (["dotnet", "build"], ["dotnet", "restore"]):
            project = safe_relative_path(tokens[2])
            if not project.endswith((".csproj", ".slnx")):
                return None
            allowed = {"--configuration", "-c", "--no-restore", "--locked-mode", "--no-cache", "--force-evaluate"}
            index = 3
            while index < len(tokens):
                flag = tokens[index]
                if flag not in allowed:
                    return None
                if flag in ("--configuration", "-c") and index + 1 >= len(tokens):
                    return None
                index += 1 if flag.startswith("--no-") or flag in ("--locked-mode", "--force-evaluate") else 2
            return {"kind": tokens[1], "project": project}
        if tokens[0] == "python3" and tokens[1].startswith("_bmad/scripts/") and tokens[1].endswith(".py"):
            script = safe_relative_path(tokens[1])
            options: dict[str, str] = {}
            flags = {"--inventory-only", "--require-approval", "--require-review-decision",
                     "--require-oq2-decision", "--verify-workflows", "--no-current-head",
                     "--rerun"}
            index = 2
            while index < len(tokens):
                flag = tokens[index]
                if not flag.startswith("--") or flag in options:
                    return None
                if flag in flags or (flag == "--check" and script == "_bmad/scripts/publish_v9_planning_authority.py"):
                    options[flag] = "true"
                    index += 1
                elif index + 1 < len(tokens) and not tokens[index + 1].startswith("--"):
                    options[flag] = tokens[index + 1]
                    index += 2
                else:
                    return None
            if options.get("--repository") != "." or options.get("--contract", contract_path) != contract_path:
                return None
            outputs = [safe_relative_path(options[flag]) for flag in
                       ("--output", "--output-schema", "--output-json", "--output-markdown")
                       if flag in options]
            if not outputs and script == "_bmad/scripts/publish_v9_planning_authority.py" and options == {"--repository": ".", "--check": "true"}:
                return {"kind": "python_check", "script": script, "outputs": [], "options": options}
            if not outputs or len(outputs) != len(set(outputs)):
                return None
            return {"kind": "python", "script": script, "outputs": outputs,
                    "options": options}
    except GateError:
        return None
    return None


def v2_successor_scenario_from_results(
    repository: Path, scenario: dict[str, Any], command: dict[str, Any],
    candidate: str, candidate_ns: int, gitlink_paths: Sequence[str],
    stamped: frozenset[str] = frozenset(),
    allowed_dirt: set[str] | None = None,
    execute_commands: bool = True,
) -> tuple[dict[str, Any], str, list[dict[str, str]]]:
    """Derive successor scenario facts from current outputs and candidate-stamped builds."""
    scenario_id = scenario["id"]
    record: dict[str, Any] = {"scenarioId": scenario_id, "command": scenario["command"],
                              "exitCode": 1, "result": "FAIL", "blockers": []}
    findings: list[dict[str, str]] = []
    subjects: list[str] = []
    outputs: list[tuple[str, bytes, int]] = []
    bound_files: list[dict[str, str]] = []

    def fail(code: str, detail: str) -> None:
        findings.append(v2_finding(code, scenario_id, detail))

    def stamped_binary(content: bytes) -> bool:
        revisions = dotnet_source_revisions(content)
        return len(revisions) == 1 and revisions[0] in stamped | {candidate}

    def read_output(path: str, *, require_current_mtime: bool = True) -> tuple[bytes, int] | None:
        if v2_below(path, gitlink_paths):
            fail("OUTPUT_PATH_INVALID", f"the output lies below a root gitlink: {path}")
            return None
        target = repository / path
        if not target.is_file() or target.is_symlink() or not target.resolve().is_relative_to(repository):
            fail("TEST_RESULTS_MISSING", f"the declared output is absent: {path}")
            return None
        try:
            content, modified = read_file_snapshot(target)
        except OSError:
            fail("TEST_RESULTS_MISSING", f"the declared output is unreadable: {path}")
            return None
        if not content:
            fail("ASSERTION_LEDGER_EMPTY", f"the declared output is empty: {path}")
        committed = v2_committed_blob(repository, candidate, path)
        if committed is not None and committed != content:
            fail("TEST_RESULTS_STALE", f"the output differs from the candidate: {path}")
        if committed is None and require_current_mtime and modified < candidate_ns:
            fail("TEST_RESULTS_STALE", f"the declared output predates the candidate: {path}")
        outputs.append((path, content, modified))
        bound_files.append({"path": path, "sha256": v2_sha256(content)})
        return content, modified

    kind = command["kind"]
    if kind in ("python", "python_check"):
        script = command["script"]
        committed = v2_committed_blob(repository, candidate, script)
        script_file = repository / script
        try:
            current = script_file.read_bytes() if script_file.is_file() and not script_file.is_symlink() else None
        except OSError:
            current = None
        if committed is None or current != committed:
            fail("SCENARIO_COMMAND_UNSUPPORTED", "the script differs from the candidate")
        if kind == "python" and command["options"].get("--scenario", scenario_id) != scenario_id:
            fail("SCENARIO_RESULT_MISMATCH", "the Python command names another scenario")
    elif kind in ("build", "restore"):
        project = command["project"]
        committed = v2_committed_blob(repository, candidate, project)
        project_file = repository / project
        try:
            current = project_file.read_bytes() if project_file.is_file() and not project_file.is_symlink() else None
        except OSError:
            current = None
        if committed is None or current != committed:
            fail("SCENARIO_COMMAND_UNSUPPORTED", f"the build target differs from the candidate: {project}")
    if execute_commands and kind in ("python", "python_check", "build", "restore") and not findings:
        try:
            executed = subprocess.run(shlex.split(scenario["command"]), cwd=repository,
                                      stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                      timeout=120 if kind == "python_check" else 600, check=False)
            if executed.returncode != 0:
                fail("TEST_RESULTS_FAILED", f"the exact command exited {executed.returncode}")
        except (OSError, ValueError, subprocess.TimeoutExpired):
            fail("TEST_RESULTS_FAILED", "the exact command did not complete")
    if kind == "xunit":
        result = read_output(command["output"])
        assembly_path = command["assembly"]
        assembly = repository / assembly_path
        if not assembly.is_file() or assembly.is_symlink():
            fail("TEST_RESULTS_MISSING", f"the test assembly is absent: {assembly_path}")
        elif result is not None:
            binary, binary_time = read_file_snapshot(assembly)
            bound_files.append({"path": assembly_path, "sha256": v2_sha256(binary)})
            if not stamped_binary(binary) or binary_time > result[1]:
                fail("TEST_RESULTS_STALE", "the test result is not bound to a candidate-stamped assembly")
            try:
                parsed = parse_trx(result[0])
                code_bases = [test_binary_path(repository, item)[0] for item in parsed["code_bases"]]
                if code_bases != [assembly_path] or count_disagreements(parsed):
                    raise ValueError("TRX assembly or counters mismatch")
                rows = parsed["results"]
                if (not rows or parsed["reported"]["executed"] == 0
                        or parsed["reported"]["failed"] or parsed["reported"]["skipped"]):
                    raise ValueError("TRX has no passing execution or contains failures/skips")
                if parsed["reported"]["passed"] != len(rows):
                    raise ValueError("TRX pass count differs from result rows")
                selector = command["selector"]
                if selector is not None and any(
                    row["test"] != selector if command["selectorKind"] == "-method"
                    else not row["test"].startswith(selector + ".") for row in rows
                ):
                    raise ValueError("TRX contains a foreign selected test")
                subjects.extend(row["test"] for row in rows)
            except (ValueError, ElementTree.ParseError):
                fail("TEST_FAILED", "the xUnit TRX does not prove the declared passing selector")
    elif kind == "python_check":
        script = command["script"]
        if not findings:
            subjects.append(f"python-check::{script}")
    elif kind == "python":
        script = command["script"]
        json_documents = []
        for path in command["outputs"]:
            result = read_output(path)
            if result is None:
                continue
            content, _ = result
            if path.endswith(".json"):
                try:
                    document = v2_parse_json(content)
                    if not isinstance(document, dict):
                        raise ValueError("JSON output is not an object")
                    json_documents.append(document)
                    state = document.get("result", document.get("status"))
                    if state is not None and str(state).upper() not in ("PASS", "PASSED", "SUCCESS"):
                        raise ValueError("the machine result is non-passing")
                    is_schema = (path == command["options"].get("--output-schema")
                                 or path.endswith(".schema.json"))
                    if state is None and "exitCode" not in document and not is_schema:
                        raise ValueError("the Python output has no explicit passing verdict")
                    if document.get("exitCode", 0) != 0:
                        raise ValueError("the machine result carries a nonzero exit code")
                    if document.get("blockers"):
                        raise ValueError("the machine result carries blockers")
                    summary = document.get("summary")
                    if summary is not None:
                        if not isinstance(summary, dict):
                            raise ValueError("the machine result summary is malformed")
                        counts = ("required", "passed", "failed", "blocked", "skipped", "notRun")
                        if any(name in summary and (not isinstance(summary[name], int)
                                or isinstance(summary[name], bool) or summary[name] < 0)
                               for name in counts):
                            raise ValueError("the machine result summary has invalid counts")
                        if (any(summary.get(name, 0) != 0 for name in ("failed", "blocked", "skipped", "notRun"))
                                or ("required" in summary and summary.get("passed") != summary["required"])
                                or ("passed" in summary and summary["passed"] == 0)):
                            raise ValueError("the machine result summary is non-passing")
                    if "candidate" in document:
                        candidate_value = document["candidate"]
                        if isinstance(candidate_value, dict):
                            candidate_value = candidate_value.get("commit")
                        if not isinstance(candidate_value, str) or candidate_value not in stamped | {candidate}:
                            raise ValueError("the machine result names another candidate")
                except (UnicodeDecodeError, ValueError):
                    fail("TEST_RESULTS_FAILED", f"the Python output is malformed or non-passing: {path}")
            subjects.append(f"python-output::{path}")
        for document in json_documents:
            rendered_digest = document.get("renderedMarkdownSha256")
            if rendered_digest is not None:
                markdown_rows = [content for path, content, _ in outputs if path.endswith(".md")]
                if len(markdown_rows) != 1 or v2_sha256(markdown_rows[0]) != rendered_digest:
                    fail("RECORD_CONTENT_DRIFT", "the Python output Markdown digest differs")
    elif kind in ("build", "restore"):
        project = command["project"]
        if kind == "build":
            tokens = shlex.split(scenario["command"])
            configuration = "Debug"
            for index, token in enumerate(tokens[:-1]):
                if token in ("--configuration", "-c"):
                    configuration = tokens[index + 1]
            projects = [project]
            if project.endswith(".slnx"):
                try:
                    solution = ElementTree.fromstring(v2_committed_blob(repository, candidate, project))
                    projects = [str(PurePosixPath(project).parent / item.attrib["Path"])
                                for item in solution.findall(".//{*}Project")]
                except (ElementTree.ParseError, KeyError, TypeError):
                    projects = []
            if not projects:
                fail("ASSERTION_LEDGER_EMPTY", "the solution has no build projects")
            for item in projects:
                name = PurePosixPath(item).stem
                binary_path = str(PurePosixPath(item).parent / f"bin/{configuration}/net10.0" / f"{name}.dll")
                result = read_output(binary_path)
                if result is not None and not stamped_binary(result[0]):
                    fail("TEST_RESULTS_STALE", "the built assembly lacks the candidate SourceRevisionId")
                subjects.append(f"build::{item}")
        else:
            if not project.endswith(".slnx"):
                fail("SCENARIO_COMMAND_UNSUPPORTED", "the restore target is not a solution")
            else:
                try:
                    solution = ElementTree.fromstring(v2_committed_blob(repository, candidate, project))
                    projects = [item.attrib["Path"] for item in solution.findall(".//{*}Project")]
                except (ElementTree.ParseError, KeyError, TypeError):
                    projects = []
                if not projects:
                    fail("ASSERTION_LEDGER_EMPTY", "the solution has no restore projects")
                for item in projects:
                    assets = str(PurePosixPath(project).parent / PurePosixPath(item).parent / "obj/project.assets.json")
                    if read_output(assets, require_current_mtime=False) is not None:
                        subjects.append(f"restore::{item}")
    if outputs:
        path, content, _ = outputs[0]
        record["resultFile"] = {"path": path, "sha256": v2_sha256(content)}
    if bound_files:
        record["outputFiles"] = bound_files
    if subjects:
        record["assertionLedger"] = [
            {"id": f"{scenario_id}#{ordinal:04d}", "subject": subject,
             "state": "FAIL" if findings else "PASS"}
            for ordinal, subject in enumerate(subjects, 1)
        ]
    else:
        fail("ASSERTION_LEDGER_EMPTY", "the successor command yielded no assertion")
    if allowed_dirt is not None:
        dirt = sorted(set(worktree_path_status(repository)) - allowed_dirt)
        if dirt:
            fail("WORKTREE_NOT_CLEAN", f"the command changed undeclared paths: {v2_path_summary(dirt)}")
    if not findings:
        record.update({"exitCode": 0, "result": "PASS"})
    record["blockers"] = sorted({item["code"] for item in findings})
    return record, "passed" if not findings else "failed", findings


def v2_early_generator_permitted(
    story_id: str, position: int, scenarios: Sequence[dict[str, Any]], contract_path: str,
) -> bool:
    """Permit the self-invocation immediately before a declared read-only check."""
    if int(story_id.split(".")[0]) < 8 or position != len(scenarios) - 2:
        return False
    try:
        final_tokens = shlex.split(scenarios[-1]["command"])
    except ValueError:
        return False
    final_route = v2_successor_command(final_tokens, contract_path)
    return final_route is not None and final_route["kind"] == "python_check"


def v2_ux_parity_code(output_bytes: dict[str, bytes], canonical: tuple[bytes, bytes, bytes]) -> str | None:
    """Return the owning blocker for a disposition bundle that differs from derivation."""
    if output_bytes["schema"] != canonical[0]:
        return "UX_SCHEMA_INVALID"
    try:
        disposition = v2_parse_json(output_bytes["json"])
        expected = v2_parse_json(canonical[1])
    except (UnicodeDecodeError, ValueError):
        return "UX_SCHEMA_INVALID"
    if not isinstance(disposition, dict):
        return "UX_SCHEMA_INVALID"
    if disposition.get("authority") != expected["authority"] or disposition.get("candidate") != expected["candidate"]:
        return "AUTHORITY_BINDING_INVALID"
    if disposition.get("sources") != expected["sources"]:
        return "UX_SOURCE_DRIFT"
    if disposition.get("decisions") != expected["decisions"]:
        return "UX_DECISION_INVENTORY_DRIFT"
    if disposition.get("acceptanceCriteria") != expected["acceptanceCriteria"]:
        return "UX_ACCEPTANCE_INVENTORY_DRIFT"
    if output_bytes["markdown"] != canonical[2]:
        return "UX_RENDER_DRIFT"
    if output_bytes["json"] != canonical[1]:
        return "UX_SCHEMA_INVALID"
    return None


def v2_ux_facts(repository: Path, candidate: str, contract_path: str,
                contract: dict[str, Any], record_validator: Any) -> dict[str, Any]:
    """Independently bind the committed disposition, sources, predecessor, and build."""
    import jsonschema  # noqa: PLC0415 - v2_load_schemas already checked availability

    def stop(code: str, subject: str) -> NoReturn:
        raise V2Stop([v2_finding(code, subject, "Story 8.1 disposition binding failed")], "8.1")

    sources = []
    for path in V2_8_1_SOURCES:
        committed = v2_committed_blob(repository, candidate, path)
        installed = repository / path
        if committed is None or not installed.is_file() or installed.is_symlink():
            stop("UX_SOURCE_UNBOUND", path)
        if installed.read_bytes() != committed:
            stop("UX_SOURCE_DRIFT", path)
        sources.append({"path": path, "sha256": v2_sha256(committed)})
    predecessor_id, predecessor_path, predecessor_markdown = V2_8_1_PREDECESSOR
    predecessor_sha = v2_verified_predecessor(repository, candidate, predecessor_id,
                                              predecessor_path, predecessor_markdown, record_validator)
    if predecessor_sha is None:
        stop("AUTHORITY_BINDING_INVALID", predecessor_path)
    predecessor = v2_parse_json(v2_committed_blob(repository, candidate, predecessor_path))
    for source in sources:
        original = v2_committed_blob(repository, predecessor["candidate"]["commit"], source["path"])
        if original is None:
            stop("UX_SOURCE_UNBOUND", source["path"])
        if v2_sha256(original) != source["sha256"]:
            stop("UX_SOURCE_DRIFT", source["path"])
    spec = v2_committed_blob(repository, candidate, V2_8_1_SPEC_PATH)
    if spec is None:
        stop("BASELINE_NOT_TRUSTWORTHY", V2_8_1_SPEC_PATH)
    try:
        baseline_value = frontmatter_scalar(parse_frontmatter(spec.decode("utf-8")), "baseline_commit")
    except (UnicodeDecodeError, GateError):
        baseline_value = None
    baseline = try_resolve_commit(repository, baseline_value) if baseline_value else None
    if baseline is None or not is_ancestor(repository, baseline, candidate):
        stop("BASELINE_NOT_TRUSTWORTHY", V2_8_1_SPEC_PATH)
    if any(path not in V2_8_1_ALLOWED_PATHS for path in committed_path_status(repository, baseline, candidate)):
        stop("UX_PRODUCTION_CHANGE_FORBIDDEN", "candidate")
    outputs = {}
    output_bytes = {}
    for role, path in zip(("schema", "json", "markdown"), V2_8_1_DISPOSITION_PATHS):
        committed = v2_committed_blob(repository, candidate, path)
        installed = repository / path
        if committed is None or not installed.is_file() or installed.is_symlink():
            stop("UX_SCHEMA_INVALID", path)
        if installed.read_bytes() != committed:
            stop("UX_RENDER_DRIFT", path)
        output_bytes[role] = committed
        outputs[role] = {"path": path, "sha256": v2_sha256(committed)}
    module_path = Path(__file__).with_name("generate_ux_preservation_disposition.py")
    module_spec = importlib_util.spec_from_file_location("story81_disposition_derivation", module_path)
    if module_spec is None or module_spec.loader is None:
        stop("UX_SCHEMA_INVALID", str(module_path))
    ux_module = importlib_util.module_from_spec(module_spec)
    module_spec.loader.exec_module(ux_module)
    try:
        schema = v2_parse_json(output_bytes["schema"])
        disposition = v2_parse_json(output_bytes["json"])
        jsonschema.Draft202012Validator.check_schema(schema)
    except (UnicodeDecodeError, ValueError, jsonschema.SchemaError):
        stop("UX_SCHEMA_INVALID", V2_8_1_DISPOSITION_PATHS[0])
    if not isinstance(disposition, dict):
        stop("UX_SCHEMA_INVALID", V2_8_1_DISPOSITION_PATHS[1])
    if (disposition.get("status") != "preserved-not-activated"
            or disposition.get("preservationBanner") != ux_module.BANNER):
        stop("UX_ACTIVATION_UNAUTHORIZED", V2_8_1_DISPOSITION_PATHS[1])
    decisions = disposition.get("decisions")
    acceptance = disposition.get("acceptanceCriteria")
    if not isinstance(decisions, list) or any(not isinstance(row, dict) for row in decisions):
        stop("UX_DECISION_INVENTORY_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    if not isinstance(acceptance, list) or any(not isinstance(row, dict) for row in acceptance):
        stop("UX_ACCEPTANCE_INVENTORY_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    expected_acceptance = [*(f"AC-SAFE-{i:03d}" for i in range(1, 9)),
                           *(f"AC-RESP-{i:03d}" for i in range(1, 16)),
                           "AC-A11Y-001", "AC-A11Y-002", "AC-LEAK-001", "AC-MOB-001", "AC-PERF-001"]
    if [row.get("id") for row in decisions] != [f"UX-DR{i}" for i in range(1, 53)]:
        stop("UX_DECISION_INVENTORY_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    if [row.get("id") for row in acceptance] != expected_acceptance:
        stop("UX_ACCEPTANCE_INVENTORY_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    if any(row.get("status") != "preserved-not-activated" for row in decisions + acceptance):
        stop("UX_ACTIVATION_UNAUTHORIZED", V2_8_1_DISPOSITION_PATHS[1])
    if any(row.get("owner") != "Stories 8.1-8.2 preservation contract" for row in decisions + acceptance):
        stop("UX_CURRENT_STORY_INVALID", V2_8_1_DISPOSITION_PATHS[1])
    if any(isinstance(mapping, dict)
           and (mapping.get("current") is True
                or mapping.get("classification", "historical-provenance") != "historical-provenance")
           for row in decisions + acceptance
           for mapping in (row.get("historicalMappings")
                           if isinstance(row.get("historicalMappings"), list) else [])):
        stop("UX_CURRENT_STORY_INVALID", V2_8_1_DISPOSITION_PATHS[1])
    provenance = disposition.get("historicalProvenance")
    if isinstance(provenance, dict) and (provenance.get("currentImplementationOwner") is True
                                         or provenance.get("classification", "non-current") != "non-current"):
        stop("UX_CURRENT_STORY_INVALID", V2_8_1_DISPOSITION_PATHS[1])
    if v2_schema_errors(jsonschema.Draft202012Validator(schema), disposition):
        stop("UX_SCHEMA_INVALID", V2_8_1_DISPOSITION_PATHS[1])
    if disposition.get("renderedMarkdownSha256") != outputs["markdown"]["sha256"]:
        stop("UX_RENDER_DRIFT", V2_8_1_DISPOSITION_PATHS[2])
    if [(row.get("path"), row.get("sha256")) for row in disposition.get("sources", [])] != [
        (row["path"], row["sha256"]) for row in sources]:
        stop("UX_SOURCE_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    if (disposition.get("candidate", {}).get("predecessorSha256") != predecessor_sha
            or disposition.get("candidate", {}).get("bindingRule") != "SC-8.1 is HEAD^{commit} at final-record generation"
            or disposition.get("authority", {}).get("inventorySha256") != contract["inventory"]["sha256"]
            or disposition.get("authority", {}).get("contractSha256") != v2_sha256(v2_committed_blob(repository, candidate, contract_path))):
        stop("AUTHORITY_BINDING_INVALID", V2_8_1_DISPOSITION_PATHS[1])
    for path in (contract_path, V2_AUTHORITY_BUNDLE_PATH, predecessor_path, predecessor_markdown):
        committed = v2_committed_blob(repository, candidate, path)
        installed = repository / path
        if committed is None or not installed.is_file() or installed.is_symlink() or installed.read_bytes() != committed:
            stop("AUTHORITY_BINDING_INVALID", path)
    try:
        canonical = ux_module.generate(repository, contract_path)
    except ux_module.DispositionError as error:
        stop(error.code, contract_path)
    parity_code = v2_ux_parity_code(output_bytes, canonical)
    if parity_code is not None:
        stop(parity_code, V2_8_1_DISPOSITION_PATHS[1])
    assembly = repository / V2_8_1_TEST_ASSEMBLY
    if not assembly.is_file() or assembly.is_symlink():
        stop("TEST_RESULTS_MISSING", V2_8_1_TEST_ASSEMBLY)
    binary, mtime_ns = read_file_snapshot(assembly)
    candidate_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    if mtime_ns < candidate_ns:
        stop("TEST_RESULTS_STALE", V2_8_1_TEST_ASSEMBLY)
    if dotnet_source_revisions(binary) != [candidate]:
        stop("TEST_RESULTS_STALE", V2_8_1_TEST_ASSEMBLY)
    return {"contract": {"path": contract_path, "sha256": v2_sha256(v2_committed_blob(repository, candidate, contract_path))},
            "predecessorRecord": {"storyId": predecessor_id, "path": predecessor_path, "sha256": predecessor_sha},
            "sources": sources, "outputs": outputs,
            "testAssembly": {"path": V2_8_1_TEST_ASSEMBLY, "sha256": v2_sha256(binary)},
            "sourceRevisionId": candidate,
            "testAssemblyMtimeNs": mtime_ns}


def v2_ux_pinned_trx_digests(repository: Path, head: str, candidate: str,
                             record_path: str) -> dict[str, str] | None:
    """Read exact-method result digests only from an already verified committed pair."""
    if candidate == head:
        return None
    content = v2_committed_blob(repository, head, record_path)
    try:
        record = v2_parse_json(content)
        if record["candidate"]["commit"] != candidate:
            raise ValueError("the retained candidate differs")
        scenarios = record["scenarios"]
        digests = {}
        for ordinal in range(2, 7):
            scenario_id = f"AC-8.1-0{ordinal}"
            rows = [row for row in scenarios if row["scenarioId"] == scenario_id]
            expected_path = f"artifacts/v9/8.1/{scenario_id}.trx"
            if (len(rows) != 1 or rows[0].get("resultFile", {}).get("path") != expected_path
                    or not re.fullmatch(r"[0-9a-f]{64}", rows[0]["resultFile"].get("sha256", ""))):
                raise ValueError("a pinned TRX digest is missing")
            digests[scenario_id] = rows[0]["resultFile"]["sha256"]
        return digests
    except (UnicodeDecodeError, ValueError, KeyError, TypeError, AttributeError):
        raise V2Stop([v2_finding("RECORD_CONTENT_DRIFT", record_path,
                                 "the committed Story 8.1 pair lacks a pinned TRX digest")], "8.1") from None


def v2_ux_scenario_from_results(repository: Path, scenario: dict[str, Any], command: dict[str, str],
                                candidate_ns: int, assembly_ns: int,
                                facts: dict[str, Any], pinned_trx_sha256: str | None = None
                                ) -> tuple[dict[str, Any], str, list[dict[str, str]]]:
    """Measure one exact-method TRX or the committed generator bundle."""
    scenario_id = scenario["id"]
    if command["kind"] == "bundle":
        findings: list[dict[str, str]] = []
        record: dict[str, Any] = {"scenarioId": scenario_id, "command": scenario["command"],
                                  "exitCode": 1, "result": "FAIL", "blockers": []}
        try:
            run = subprocess.run(shlex.split(scenario["command"]), cwd=repository,
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                 timeout=120, check=False)
        except (OSError, subprocess.TimeoutExpired):
            findings.append(v2_finding("TEST_RESULTS_FAILED", scenario_id,
                                       "the exact disposition command did not complete"))
        else:
            if run.returncode != 0:
                findings.append(v2_finding("TEST_RESULTS_FAILED", scenario_id,
                                           f"the exact disposition command exited {run.returncode}"))
            for role, path in zip(("schema", "json", "markdown"), V2_8_1_DISPOSITION_PATHS):
                target = repository / path
                if not target.is_file() or target.is_symlink() or v2_sha256(target.read_bytes()) != facts["outputs"][role]["sha256"]:
                    findings.append(v2_finding("UX_RENDER_DRIFT", path,
                                               "the exact command did not reproduce the committed output"))
        if findings:
            record["blockers"] = sorted({item["code"] for item in findings})
            return record, "failed", findings
        subjects = ("schema::closed-valid", "sources::path-version-hash", "inventory::52-28",
                    "status::preserved", "provenance::non-current", "markdown::digest", "predecessor::7.4")
        return ({"scenarioId": scenario_id, "command": scenario["command"], "exitCode": 0,
                 "result": "PASS", "blockers": [], "resultFile": facts["outputs"]["json"],
                 "assertionLedger": [{"id": f"{scenario_id}#{i:04d}", "subject": item, "state": "PASS"}
                                      for i, item in enumerate(subjects, 1)]}, "passed", [])
    findings: list[dict[str, str]] = []
    record: dict[str, Any] = {"scenarioId": scenario_id, "command": scenario["command"],
                              "exitCode": 1, "result": "FAIL", "blockers": []}
    if pinned_trx_sha256 is None:
        try:
            executed = subprocess.run(shlex.split(scenario["command"]), cwd=repository,
                                      stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                      timeout=120, check=False)
            if executed.returncode != 0:
                findings.append(v2_finding("TEST_RESULTS_FAILED", scenario_id,
                                           f"the exact selector exited {executed.returncode}"))
        except (OSError, ValueError, subprocess.TimeoutExpired):
            findings.append(v2_finding("TEST_RESULTS_FAILED", scenario_id,
                                       "the exact selector did not complete"))
        if findings:
            record["blockers"] = ["TEST_RESULTS_FAILED"]
            return record, "failed", findings
    result = repository / command["output"]
    if not result.is_file() or result.is_symlink():
        findings.append(v2_finding("TEST_RESULTS_MISSING", scenario_id, "the exact-method TRX is absent"))
        record["blockers"] = ["TEST_RESULTS_MISSING"]
        return record, "notRun", findings
    try:
        content, mtime_ns = read_file_snapshot(result)
        parsed = parse_trx(content)
    except (OSError, ValueError, ElementTree.ParseError):
        findings.append(v2_finding("INPUT_SCHEMA_INVALID", scenario_id, "the exact-method TRX is invalid"))
        record["blockers"] = ["INPUT_SCHEMA_INVALID"]
        return record, "failed", findings
    record["resultFile"] = {"path": command["output"], "sha256": v2_sha256(content)}
    if pinned_trx_sha256 is not None and record["resultFile"]["sha256"] != pinned_trx_sha256:
        findings.append(v2_finding("TEST_RESULTS_STALE", scenario_id,
                                   "the TRX differs from the verified committed Story 8.1 pair"))
    rows = parsed["results"]
    binary_paths = []
    for code_base in parsed["code_bases"]:
        try:
            binary_paths.append(test_binary_path(repository, code_base)[0])
        except ValueError:
            binary_paths.append("invalid")
    if (len(rows) != 1 or rows[0]["test"] != command["method"]
            or parsed["assemblies"] != ["Hexalith.Conversations.Conformance.Tests"]
            or binary_paths != [V2_8_1_TEST_ASSEMBLY]
            or parsed["reported"] != {"total": 1, "executed": 1, "passed": 1, "failed": 0, "skipped": 0}
            or parsed["recomputed"] != {"total": 1, "passed": 1, "failed": 0, "skipped": 0}
            or count_disagreements(parsed)):
        findings.append(v2_finding("TEST_FAILED", scenario_id, "the exact-method TRX is empty, skipped, failed, or mismatched"))
    if mtime_ns < max(candidate_ns, assembly_ns):
        findings.append(v2_finding("TEST_RESULTS_STALE", scenario_id, "the TRX predates the candidate or test build"))
    if rows:
        record["assertionLedger"] = [{"id": f"{scenario_id}#0001", "subject": rows[0]["test"],
                                      "state": "FAIL" if findings else "PASS"}]
    if not findings:
        record.update({"exitCode": 0, "result": "PASS"})
    record["blockers"] = sorted({item["code"] for item in findings})
    return record, "failed" if findings else "passed", findings


def v2_8_2_facts(repository: Path, candidate: str, contract: dict[str, Any], validator: Any) -> dict[str, Any]:
    """Bind Story 8.2 to committed preservation inputs and a compatible accepted predecessor."""
    def stop(code: str, subject: str) -> NoReturn:
        raise V2Stop([v2_finding(code, subject, "Story 8.2 zero-gap binding failed")], "8.2")

    def committed_input(path: str, code: str) -> bytes:
        blob = v2_committed_blob(repository, candidate, path)
        installed = repository / path
        if blob is None or not installed.is_file() or installed.is_symlink() or installed.read_bytes() != blob:
            stop(code, path)
        return blob

    predecessor_id, predecessor_path, predecessor_markdown = V2_8_2_PREDECESSOR
    predecessor_digest = v2_verified_predecessor(repository, candidate, predecessor_id,
                                                 predecessor_path, predecessor_markdown, validator)
    if predecessor_digest is None:
        stop("AUTHORITY_BINDING_INVALID", predecessor_path)
    predecessor = v2_parse_json(committed_input(predecessor_path, "AUTHORITY_BINDING_INVALID"))
    committed_input(predecessor_markdown, "AUTHORITY_BINDING_INVALID")
    predecessor_candidate = predecessor["candidate"]["commit"]
    if not is_ancestor(repository, predecessor_candidate, candidate) or predecessor_candidate == candidate:
        stop("AUTHORITY_BINDING_INVALID", "predecessor candidate")
    if predecessor["summary"] != {"required": 7, "passed": 7, "failed": 0, "blocked": 0, "skipped": 0, "notRun": 0}:
        stop("AUTHORITY_BINDING_INVALID", "predecessor summary")
    spec = v2_committed_blob(repository, candidate, V2_8_2_SPEC_PATH)
    if spec is None:
        stop("BASELINE_NOT_TRUSTWORTHY", V2_8_2_SPEC_PATH)
    frontmatter = {}
    scope_start_value = None
    try:
        frontmatter = parse_frontmatter(spec.decode())
        baseline = frontmatter_scalar(frontmatter, "baseline_commit")
        baseline = try_resolve_commit(repository, baseline) if baseline else None
        scope_start_value = frontmatter_scalar(frontmatter, "implementation_start_commit")
        scope_start = try_resolve_commit(repository, scope_start_value) if scope_start_value else None
    except (UnicodeError, GateError):
        baseline = None
        scope_start = None
    additions = decode(run_git(repository, "log", "--no-renames", "--diff-filter=A", "--format=%H",
                               candidate, "--", V2_8_2_SPEC_PATH).stdout).split()
    if len(additions) != 1:
        stop("BASELINE_NOT_TRUSTWORTHY", "original spec-add commit")
    original_spec = v2_committed_blob(repository, additions[0], V2_8_2_SPEC_PATH)
    try:
        original_baseline = frontmatter_scalar(parse_frontmatter(original_spec.decode()), "baseline_commit")
    except (AttributeError, UnicodeError, GateError):
        original_baseline = None
    if not scope_start_value:
        scope_start = additions[0]
    if (baseline is None or scope_start is None or scope_start != additions[0]
            or frontmatter_scalar(frontmatter, "baseline_commit") != original_baseline
            or not is_ancestor(repository, baseline, scope_start)
            or not is_ancestor(repository, scope_start, candidate)):
        stop("BASELINE_NOT_TRUSTWORTHY", V2_8_2_SPEC_PATH)
    if any(path not in V2_8_2_ALLOWED_PATHS for path in committed_path_status(repository, scope_start, candidate)):
        stop("UX_PRODUCTION_CHANGE_FORBIDDEN", "candidate")
    if v2_raw_gitlinks(repository, candidate) != v2_raw_gitlinks(repository, scope_start):
        stop("UX_PRODUCTION_CHANGE_FORBIDDEN", "candidate gitlinks")
    committed_input(V2_8_2_CONTRACT_PATH, "AUTHORITY_BINDING_INVALID")
    committed_input(V2_8_1_CONTRACT_PATH, "AUTHORITY_BINDING_INVALID")
    committed_input(V2_AUTHORITY_BUNDLE_PATH, "AUTHORITY_BINDING_INVALID")
    for path in ("_bmad/scripts/generate_ux_preservation_disposition.py",
                 "_bmad/scripts/tests/test_generate_ux_preservation_disposition.py",
                 "tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs"):
        committed_input(path, "TEST_RESULTS_STALE")
    sources = [{"path": path, "sha256": v2_sha256(committed_input(path, "UX_SOURCE_DRIFT"))}
               for path in V2_8_1_SOURCES]
    outputs = {role: {"path": path, "sha256": v2_sha256(committed_input(path, "UX_RENDER_DRIFT"))}
               for role, path in zip(("schema", "json", "markdown"), V2_8_1_DISPOSITION_PATHS)}
    prior_binding = predecessor.get("uxDisposition", {})
    if prior_binding.get("sources") != sources or prior_binding.get("outputs") != outputs:
        stop("AUTHORITY_BINDING_INVALID", "predecessor disposition")
    for row in [*sources, *outputs.values()]:
        blob = v2_committed_blob(repository, predecessor_candidate, row["path"])
        if blob is None or v2_sha256(blob) != row["sha256"]:
            stop("AUTHORITY_BINDING_INVALID", row["path"])
    module_spec = importlib_util.spec_from_file_location("story82_ux_verifier", repository / "_bmad/scripts/generate_ux_preservation_disposition.py")
    ux_module = importlib_util.module_from_spec(module_spec)
    module_spec.loader.exec_module(ux_module)
    try:
        disposition = ux_module.verify(repository)
    except ux_module.DispositionError as error:
        stop(error.code, "disposition")
    inventories = {"decisions": [row["id"] for row in disposition["decisions"]],
                   "acceptanceCriteria": [row["id"] for row in disposition["acceptanceCriteria"]]}
    inventory_digest = v2_sha256(v2_render_json(inventories))
    binary, modified = v2_8_2_evidence(repository, V2_8_1_TEST_ASSEMBLY)
    candidate_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    if modified < candidate_ns or dotnet_source_revisions(binary) != [candidate]:
        stop("TEST_RESULTS_STALE", V2_8_1_TEST_ASSEMBLY)
    return {"bindingRule": "SC-8.2 is HEAD^{commit} at final-record generation",
            "implementationStartCommit": scope_start,
            "contract": {"path": V2_8_2_CONTRACT_PATH, "sha256": v2_sha256(v2_committed_blob(repository, candidate, V2_8_2_CONTRACT_PATH))},
            "predecessorRecord": {"storyId": "8.1", "path": predecessor_path, "sha256": predecessor_digest},
            "predecessorCandidate": predecessor_candidate, "sources": sources, "outputs": outputs,
            "inventorySha256": inventory_digest, "decisionCount": 52, "acceptanceCount": 28,
            "testAssembly": {"path": V2_8_1_TEST_ASSEMBLY, "sha256": v2_sha256(binary)},
            "sourceRevisionId": candidate}


def v2_8_2_fixture_digest(repository: Path, candidate: str) -> str:
    """Derive the ordered fixture-byte hash from the candidate's committed inputs."""
    paths = sorted([*V2_8_1_SOURCES, V2_8_1_CONTRACT_PATH, V2_AUTHORITY_BUNDLE_PATH,
                    V2_8_1_PREDECESSOR[1], V2_8_1_PREDECESSOR[2], *V2_8_1_DISPOSITION_PATHS])
    streams = []
    for path in paths:
        blob = v2_committed_blob(repository, candidate, path)
        if blob is None:
            raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", path, "a committed fixture input is missing")], "8.2")
        streams.append(path.encode() + b"\0" + blob + b"\0")
    return v2_sha256(b"".join(streams))


def v2_8_2_evidence(repository: Path, relative: str) -> tuple[bytes, int]:
    """Read a contained regular receipt without following any symlink."""
    try:
        lexical = repository / safe_relative_path(relative)
        resolved = lexical.resolve(strict=True)
        if (resolved != lexical or not resolved.is_relative_to(repository) or not resolved.is_file()
                or any(parent.is_symlink() for parent in [lexical, *lexical.parents] if parent.is_relative_to(repository))):
            raise ValueError("receipt must be a contained regular file")
        return read_file_snapshot(resolved)
    except (FileNotFoundError, OSError):
        raise V2Stop([v2_finding("TEST_RESULTS_MISSING", relative, "receipt is missing or unreadable")], "8.2") from None
    except (GateError, ValueError, RuntimeError):
        raise V2Stop([v2_finding("INPUT_SCHEMA_INVALID", relative, "receipt path is outside the repository or names a symlink/non-file")], "8.2") from None


def v2_8_2_faults(repository: Path, candidate: str, scenarios: dict[str, dict[str, Any]], validator: Any) -> list[dict[str, Any]]:
    """Consume measured, candidate-stamped exact faults and their restoration rerun."""
    observed_validator = validator.evolve(schema={"$defs": validator.schema["$defs"], "$ref": "#/$defs/uxObservedFault"})
    candidate_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    fixture_digest = v2_8_2_fixture_digest(repository, candidate)
    lanes = {}
    for scenario_id, expected in V2_8_2_FAULT_LANES.items():
        binding = scenarios[scenario_id]["resultFile"]
        content, modified = v2_8_2_evidence(repository, binding["path"])
        if v2_sha256(content) != binding["sha256"] or modified < candidate_ns:
            raise V2Stop([v2_finding("TEST_RESULTS_STALE", scenario_id, "JUnit bytes changed after measurement")], "8.2")
        try:
            parsed = v2_parse_junit(content)
        except (ValueError, ElementTree.ParseError):
            raise V2Stop([v2_finding("INPUT_SCHEMA_INVALID", scenario_id, "receipt is malformed JUnit XML")], "8.2") from None
        if (parsed["reported"] != {"tests": len(parsed["cases"]), "failures": 0, "errors": 0, "skipped": 0}
                or not parsed["cases"] or any(case["failure"] or case["error"] or case["skipped"] for case in parsed["cases"])):
            raise V2Stop([v2_finding("TEST_RESULTS_FAILED", scenario_id, "fault evidence is empty, failed, skipped, or has inconsistent counters")], "8.2")
        rows = []
        for case in parsed["cases"]:
            metadata = [value for name, value in case["properties"] if name == V2_8_2_FAULT_PROPERTY]
            row = None
            try:
                row = v2_parse_json(metadata[0].encode()) if len(metadata) == 1 else None
                valid = isinstance(row, dict) and not v2_schema_errors(observed_validator, row)
            except (ValueError, UnicodeError):
                valid = False
            if not valid:
                if (isinstance(row, dict) and "restoredExitCode" in row and "restoredBlockers" in row
                        and (row["restoredExitCode"] != 0 or row["restoredBlockers"] != [])):
                    raise V2Stop([v2_finding("FIXTURE_NOT_RESTORED", scenario_id, "restored PASS differs")], "8.2")
                raise V2Stop([v2_finding("FAULT_NOT_DETECTED", scenario_id, "exactly one measured fault property is required per executed case")], "8.2")
            if (case["classname"] != "_bmad.scripts.tests.test_generate_ux_preservation_disposition"
                    or case["name"] != v2_8_2_case_name(scenario_id, row["id"])):
                raise V2Stop([v2_finding("FAULT_NOT_DETECTED", scenario_id, "fault metadata does not identify its exact executed testcase")], "8.2")
            if row["candidateCommit"] != candidate:
                raise V2Stop([v2_finding("TEST_RESULTS_STALE", scenario_id, "fault evidence names another candidate")], "8.2")
            if (row["expectedBlocker"] != V2_8_2_REQUIRED_FAULTS.get(row["id"])
                    or row["observedBlockers"] != [row["expectedBlocker"]] or row["observedExitCode"] != 1
                    or row["baselineExitCode"] != 0 or row["baselineBlockers"]):
                raise V2Stop([v2_finding("FAULT_NOT_DETECTED", row["id"], "the mutation did not measure baseline PASS and its sole exact blocker with exit 1")], "8.2")
            if (row["beforeSha256"] != fixture_digest or row["afterSha256"] != fixture_digest or row["beforeSha256"] == row["mutatedSha256"]
                    or row["restoredExitCode"] != 0 or row["restoredBlockers"]):
                raise V2Stop([v2_finding("FIXTURE_NOT_RESTORED", row["id"], "fixture restoration or restored PASS differs")], "8.2")
            rows.append(row)
        if [row["id"] for row in rows] != expected:
            raise V2Stop([v2_finding("FAULT_NOT_DETECTED", scenario_id, "the exact ordered fault set is missing, duplicated, unknown, or reordered")], "8.2")
        lanes[scenario_id] = rows
    mutations = [row for scenario_id, rows in lanes.items() if scenario_id != "AC-8.2-10" for row in rows]
    if mutations != lanes["AC-8.2-10"]:
        raise V2Stop([v2_finding("FIXTURE_NOT_RESTORED", "AC-8.2-10", "restoration matrix differs from measured mutation lanes")], "8.2")
    return mutations


def v2_8_2_case_name(scenario_id: str, identifier: str) -> str:
    """Return the frozen selector's sole test name or exact parameter identity."""
    selectors = {"AC-8.2-02": "missing_decision", "AC-8.2-03": "duplicate_decision",
                 "AC-8.2-04": "unknown_decision", "AC-8.2-05": "acceptance_identity_faults",
                 "AC-8.2-06": "ownership_and_hash_faults", "AC-8.2-07": "source_drift",
                 "AC-8.2-08": "rendering_and_order_drift", "AC-8.2-09": "activation_and_story_binding_faults",
                 "AC-8.2-10": "fixtures_restore_byte_identically"}
    suffix = "" if scenario_id in ("AC-8.2-02", "AC-8.2-03", "AC-8.2-04", "AC-8.2-07") else f"[{identifier}]"
    return f"test_{selectors[scenario_id]}{suffix}"


def v2_8_2_positive(repository: Path, candidate: str, scenario: dict[str, Any]) -> None:
    """Require the six complete preservation facts in current candidate-stamped TRX."""
    command = v2_successor_command(shlex.split(scenario["command"]), V2_8_2_CONTRACT_PATH)
    binding = scenario["resultFile"]
    content, _ = v2_8_2_evidence(repository, binding["path"])
    if binding["path"] != command["output"] or v2_sha256(content) != binding["sha256"]:
        raise V2Stop([v2_finding("TEST_RESULTS_STALE", "AC-8.2-01", "the positive result differs from its measured binding")], "8.2")
    scenario_contract = {"id": "AC-8.2-01", "command": scenario["command"]}
    candidate_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    _, _, findings = v2_successor_scenario_from_results(repository, scenario_contract, command, candidate, candidate_ns, [])
    if findings:
        raise V2Stop(findings, "8.2")
    prefix = "Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest."
    expected = {prefix + name for name in (
        "PreservedBundleShouldPassZeroGapVerification", "SourcesShouldBindCanonicalPathsVersionsAndHashes",
        "DecisionsShouldProjectTheFrozenInventory", "AcceptanceCriteriaShouldProjectTheFrozenInventory",
        "DispositionsShouldRemainPreservedAndHistorical", "CandidateShouldContainNoProductionUiChange")}
    rows = parse_trx(content)["results"]
    if len(rows) != len(expected) or {row["test"] for row in rows} != expected:
        raise V2Stop([v2_finding("TEST_NOT_RUN", "AC-8.2-01", "the TRX must execute all six preservation facts exactly once")], "8.2")


def v2_tiering_command(tokens: list[str], scenario_id: str) -> dict[str, str] | None:
    """Recognize Story 9.1's exact frozen disposition-generator command."""
    expected = ["python3", V2_9_1_GENERATOR_PATH, "--repository", ".", "--contract", V2_9_1_CONTRACT_PATH,
                "--decision", V2_9_1_DECISION_PATH, "--output-schema", V2_9_1_OUTPUT_PATHS[0],
                "--output-json", V2_9_1_OUTPUT_PATHS[1], "--output-markdown", V2_9_1_OUTPUT_PATHS[2]]
    if scenario_id == "AC-9.1-01" and tokens == expected:
        return {"kind": "tiering-bundle", "output": V2_9_1_OUTPUT_PATHS[1]}
    return None


def v2_9_1_tiering_module() -> Any:
    """Load the tooling's own tiering generator; the evaluated candidate cannot redefine it."""
    path = Path(__file__).with_name("generate_conformance_tiering.py")
    specification = importlib_util.spec_from_file_location("story91_conformance_tiering", path)
    if specification is None or specification.loader is None:
        raise V2Stop([v2_finding("TIERING_ENVIRONMENT_UNAVAILABLE", str(path),
                                 "the conformance tiering generator is unavailable")], "9.1")
    module = importlib_util.module_from_spec(specification)
    sys.modules[specification.name] = module  # dataclasses resolve postponed annotations here
    specification.loader.exec_module(module)
    return module


def v2_9_1_scenario_from_results(repository: Path, candidate: str, scenario: dict[str, Any]
                                 ) -> tuple[dict[str, Any], str, list[dict[str, str]]]:
    """Execute the exact AC-9.1-01 command and require it to reproduce the committed bundle."""
    scenario_id = scenario["id"]
    record: dict[str, Any] = {"scenarioId": scenario_id, "command": scenario["command"],
                              "exitCode": 1, "result": "FAIL", "blockers": []}
    findings: list[dict[str, str]] = []
    try:
        run = subprocess.run(shlex.split(scenario["command"]), cwd=repository, stdout=subprocess.PIPE,
                             stderr=subprocess.PIPE, timeout=600, check=False)
    except (OSError, ValueError, subprocess.TimeoutExpired):
        findings.append(v2_finding("TEST_RESULTS_FAILED", scenario_id, "the exact disposition command did not complete"))
    else:
        if run.returncode != 0:
            reported = re.findall(r"^(?:FAIL|BLOCKED): ([A-Z][A-Z0-9_]*):", decode(run.stderr), re.M)
            findings.extend(v2_finding(code, scenario_id, "the exact disposition command reported this blocker")
                            for code in dict.fromkeys(reported) if code in V2_CODES)
            findings.append(v2_finding("TEST_RESULTS_FAILED", scenario_id,
                                       f"the exact disposition command exited {run.returncode}"))
        for path in V2_9_1_OUTPUT_PATHS:
            committed = v2_committed_blob(repository, candidate, path)
            installed = repository / path
            if (committed is None or not installed.is_file() or installed.is_symlink()
                    or installed.read_bytes() != committed):
                findings.append(v2_finding("TIERING_RENDER_DRIFT", path,
                                           "the exact command did not reproduce the committed output"))
    if findings:
        record["blockers"] = sorted({item["code"] for item in findings})
        return record, "failed", findings
    subjects = ("schema::closed-v2", "inventory::pre-split-identities-once", "strength::canonical-triple-digests",
                "tiers::exact-public-replacement-or-internal-type", "approvals::quality-owner-digest",
                "denominator::fr20-membership-unchanged", "public::freeze-surface-unchanged",
                "supersession::v1-protected-and-linked", "markdown::digest", "predecessor::7.4")
    record.update({"exitCode": 0, "result": "PASS",
                   "resultFile": {"path": V2_9_1_OUTPUT_PATHS[1],
                                  "sha256": v2_sha256(v2_committed_blob(repository, candidate, V2_9_1_OUTPUT_PATHS[1]))},
                   "outputFiles": [{"path": path, "sha256": v2_sha256(v2_committed_blob(repository, candidate, path))}
                                   for path in V2_9_1_OUTPUT_PATHS],
                   "assertionLedger": [{"id": f"{scenario_id}#{ordinal:04d}", "subject": subject, "state": "PASS"}
                                       for ordinal, subject in enumerate(subjects, 1)]})
    return record, "passed", []


def v2_9_1_receipt(repository: Path, relative: str) -> tuple[bytes, int]:
    """Read one contained regular retained receipt without following a symlink."""
    try:
        lexical = repository / safe_relative_path(relative)
        resolved = lexical.resolve(strict=True)
        if (resolved != lexical or not resolved.is_relative_to(repository) or not resolved.is_file()
                or any(parent.is_symlink() for parent in [lexical, *lexical.parents]
                       if parent.is_relative_to(repository))):
            raise ValueError("receipt must be a contained regular file")
        return read_file_snapshot(resolved)
    except (FileNotFoundError, OSError):
        raise V2Stop([v2_finding("TEST_RESULTS_MISSING", relative, "the retained receipt is missing or unreadable")],
                     "9.1") from None
    except (GateError, ValueError, RuntimeError):
        raise V2Stop([v2_finding("INPUT_SCHEMA_INVALID", relative,
                                 "the receipt path is outside the repository or names a symlink/non-file")], "9.1") from None


def v2_9_1_facts(repository: Path, candidate: str, contract: dict[str, Any], validator: Any) -> dict[str, Any]:
    """Bind Story 9.1 to its verified disposition, retained pre-split result, approvals, and build."""
    def stop(code: str, subject: str, message: str = "Story 9.1 tiering binding failed") -> NoReturn:
        raise V2Stop([v2_finding(code, subject, message)], "9.1")

    def committed_input(path: str, code: str) -> bytes:
        blob = v2_committed_blob(repository, candidate, path)
        installed = repository / path
        if blob is None or not installed.is_file() or installed.is_symlink() or installed.read_bytes() != blob:
            stop(code, path)
        return blob

    for path in (V2_9_1_CONTRACT_PATH, V2_AUTHORITY_BUNDLE_PATH, V2_9_1_DECISION_PATH, V2_8_1_PREDECESSOR[1],
                 V2_8_1_PREDECESSOR[2]):
        committed_input(path, "AUTHORITY_BINDING_INVALID")
    for path in (V2_9_1_GENERATOR_PATH, "_bmad/scripts/tests/test_conformance_tiering.py",
                 "tests/Hexalith.Conversations.Conformance.Tests/ConformanceOracleTieringValidationTest.cs",
                 "tests/Hexalith.Conversations.Conformance.Tests/ConformanceTieringIlReferenceReader.cs"):
        committed_input(path, "TEST_RESULTS_STALE")
    outputs = {role: {"path": path, "sha256": v2_sha256(committed_input(path, "TIERING_RENDER_DRIFT"))}
               for role, path in zip(("schema", "json", "markdown"), V2_9_1_OUTPUT_PATHS)}
    approvals_bytes = committed_input(V2_9_1_APPROVALS_PATH, "TIER_APPROVAL_MISSING")
    predecessor_id, predecessor_path, predecessor_markdown = V2_8_1_PREDECESSOR
    predecessor_digest = v2_verified_predecessor(repository, candidate, predecessor_id, predecessor_path,
                                                 predecessor_markdown, validator)
    if predecessor_digest is None:
        stop("AUTHORITY_BINDING_INVALID", predecessor_path)
    predecessor = v2_parse_json(v2_committed_blob(repository, candidate, predecessor_path))
    predecessor_candidate = predecessor["candidate"]["commit"]
    if (predecessor["summary"] != {"required": 6, "passed": 6, "failed": 0, "blocked": 0, "skipped": 0, "notRun": 0}
            or try_resolve_commit(repository, predecessor_candidate) is None):
        stop("AUTHORITY_BINDING_INVALID", "Story 7.4 predecessor")
    tiering = v2_9_1_tiering_module()
    receipt_bytes, _ = v2_9_1_receipt(repository, V2_9_1_RECEIPT_PATH)
    try:
        document = tiering.verify(repository)
        pre_split = tiering.load_pre_split(repository)
    except tiering.MultiError as error:
        raise V2Stop([v2_finding(code if code in V2_CODES else "TEST_RESULTS_FAILED", "disposition", detail)
                      for code, detail in error.items], "9.1") from None
    except tiering.TieringError as error:
        code = error.code if error.code in V2_CODES else "TEST_RESULTS_FAILED"
        raise V2Stop([v2_finding(code, "disposition", str(error))], "9.1") from None
    result = document["preSplitResult"]
    if (pre_split.receipt_sha256 != v2_sha256(receipt_bytes)
            or result["receipt"]["sha256"] != v2_sha256(receipt_bytes)):
        stop("TEST_RESULTS_STALE", V2_9_1_RECEIPT_PATH, "the retained pre-split receipt differs from the disposition")
    freeze = result["sourceCommit"]
    if freeze == candidate or not is_ancestor(repository, freeze, candidate):
        stop("BASELINE_NOT_TRUSTWORTHY", "freeze commit", "the pre-split freeze commit is not a strict ancestor")
    approvals = v2_parse_json(approvals_bytes)
    decision = approvals.get("decision") if isinstance(approvals, dict) else None
    binding = document.get("approvals")
    if (not isinstance(decision, dict) or decision.get("state") != "approved" or not isinstance(binding, dict)
            or binding.get("sha256") != v2_sha256(approvals_bytes)
            or decision.get("approvedMembershipSha256") != binding.get("membershipSha256")):
        stop("TIER_APPROVAL_MISSING", V2_9_1_APPROVALS_PATH)
    rows = [*document["assertions"], *document["validationAdditions"]]
    binary, modified = v2_9_1_receipt(repository, V2_8_1_TEST_ASSEMBLY)
    candidate_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    if modified < candidate_ns or dotnet_source_revisions(binary) != [candidate]:
        stop("TEST_RESULTS_STALE", V2_8_1_TEST_ASSEMBLY, "the test assembly is not stamped by the candidate")
    return {
        "bindingRule": V2_9_1_BINDING_RULE,
        "contract": {"path": V2_9_1_CONTRACT_PATH,
                     "sha256": v2_sha256(v2_committed_blob(repository, candidate, V2_9_1_CONTRACT_PATH))},
        "predecessorRecord": {"storyId": "7.4", "path": predecessor_path, "sha256": predecessor_digest},
        "predecessorCandidate": predecessor_candidate,
        "decision": {"path": V2_9_1_DECISION_PATH,
                     "sha256": v2_sha256(v2_committed_blob(repository, candidate, V2_9_1_DECISION_PATH))},
        "disposition": outputs,
        "preSplitResult": {"path": result["result"]["path"], "sha256": result["result"]["sha256"],
                           "receipt": {"path": V2_9_1_RECEIPT_PATH, "sha256": v2_sha256(receipt_bytes)},
                           "sourceCommit": freeze, "discoveredMethods": result["counts"]["discoveredMethods"],
                           "executedTestCases": result["counts"]["executedTestCases"]},
        "assertionInventorySha256": v2_sha256(v2_render_json([row["id"] for row in rows])),
        "strengthInventorySha256": v2_sha256(v2_render_json([[row["id"], row["strengthSha256"]] for row in rows])),
        "assertionCount": len(document["assertions"]),
        "validationAdditionCount": len(document["validationAdditions"]),
        "tierCounts": {"portable": sum(1 for row in rows if row["tier"] == "portable"),
                       "moduleInternal": sum(1 for row in rows if row["tier"] == "module-internal")},
        "approvals": {"path": V2_9_1_APPROVALS_PATH, "sha256": v2_sha256(approvals_bytes),
                      "approvalId": binding["approvalId"], "approver": binding["approver"],
                      "membershipSha256": binding["membershipSha256"]},
        "inventorySha256": contract["inventory"]["sha256"],
        "faultEvidence": {"path": V2_9_1_FAULT_RESULT_PATH,
                          "sha256": v2_sha256(v2_9_1_receipt(repository, V2_9_1_FAULT_RESULT_PATH)[0])},
        "testAssembly": {"path": V2_8_1_TEST_ASSEMBLY, "sha256": v2_sha256(binary)},
        "sourceRevisionId": candidate,
    }


def v2_9_1_fixture_digest(repository: Path, candidate: str) -> str:
    """Derive the fault fixtures' ordered byte-stream hash from the candidate's committed blobs."""
    streams = []
    for path in V2_9_1_FAULT_FIXTURE_PATHS:
        blob = v2_committed_blob(repository, candidate, path)
        streams.append(path.encode() + b"\0" + (blob if blob is not None else b"\0absent") + b"\0")
    return v2_sha256(b"".join(streams))


def v2_9_1_faults(repository: Path, candidate: str, validator: Any) -> list[dict[str, Any]]:
    """Consume the measured, candidate-bound, byte-restored Story 9.1 fault receipts."""
    def stop(code: str, subject: str, message: str) -> NoReturn:
        raise V2Stop([v2_finding(code, subject, message)], "9.1")

    content, modified = v2_9_1_receipt(repository, V2_9_1_FAULT_RESULT_PATH)
    candidate_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    if modified < candidate_ns:
        stop("TEST_RESULTS_STALE", V2_9_1_FAULT_RESULT_PATH, "the fault receipt predates the candidate")
    try:
        parsed = v2_parse_junit(content)
    except (ValueError, ElementTree.ParseError):
        stop("INPUT_SCHEMA_INVALID", V2_9_1_FAULT_RESULT_PATH, "the fault receipt is malformed JUnit XML")
    if (parsed["reported"] != {"tests": len(parsed["cases"]), "failures": 0, "errors": 0, "skipped": 0}
            or not parsed["cases"] or any(case["failure"] or case["error"] or case["skipped"] for case in parsed["cases"])):
        stop("TEST_RESULTS_FAILED", V2_9_1_FAULT_RESULT_PATH, "fault evidence is empty, failed, skipped, or inconsistent")
    observed_validator = validator.evolve(schema={"$defs": validator.schema["$defs"], "$ref": "#/$defs/tieringObservedFault"})
    fixture = v2_9_1_fixture_digest(repository, candidate)
    rows = []
    for case in parsed["cases"]:
        metadata = [value for name, value in case["properties"] if name == V2_9_1_FAULT_PROPERTY]
        try:
            row = v2_parse_json(metadata[0].encode()) if len(metadata) == 1 else None
            valid = isinstance(row, dict) and not v2_schema_errors(observed_validator, row)
        except (ValueError, UnicodeError):
            row, valid = None, False
        if not valid:
            if (isinstance(row, dict) and "restoredExitCode" in row
                    and (row.get("restoredExitCode") != 0 or row.get("restoredBlockers") != [])):
                stop("FIXTURE_NOT_RESTORED", V2_9_1_FAULT_RESULT_PATH, "a restored verification did not pass")
            stop("FAULT_NOT_DETECTED", V2_9_1_FAULT_RESULT_PATH, "every executed case needs exactly one valid measured fault")
        if case["classname"] != V2_9_1_FAULT_CLASSNAME or case["name"] != f"test_tiering_faults[{row['id']}]":
            stop("FAULT_NOT_DETECTED", row["id"], "the fault metadata does not identify its executed testcase")
        if row["candidateCommit"] != candidate:
            stop("TEST_RESULTS_STALE", row["id"], "the fault evidence names another candidate")
        if (row["expectedBlocker"] != V2_9_1_REQUIRED_FAULTS.get(row["id"])
                or row["observedBlockers"] != [row["expectedBlocker"]] or row["observedExitCode"] != 1
                or row["baselineExitCode"] != 0 or row["baselineBlockers"]):
            stop("FAULT_NOT_DETECTED", row["id"], "the mutation did not measure baseline PASS and its sole blocker")
        if (row["beforeSha256"] != fixture or row["afterSha256"] != fixture or row["mutatedSha256"] == fixture
                or row["restoredExitCode"] != 0 or row["restoredBlockers"]):
            stop("FIXTURE_NOT_RESTORED", row["id"], "the fixture bytes or restored PASS differ from the candidate")
        rows.append(row)
    if [row["id"] for row in rows] != list(V2_9_1_REQUIRED_FAULTS):
        stop("FAULT_NOT_DETECTED", V2_9_1_FAULT_RESULT_PATH, "the ten ordered fault categories are not all measured once")
    return rows


def v2_9_2_verifier() -> Any:
    """Load the installed verifier; candidate-owned input cannot redefine this API."""
    specification = importlib_util.spec_from_file_location("story92_record_verifier", Path(__file__).with_name("verify_conformance_tiering.py"))
    if specification is None or specification.loader is None:
        raise V2Stop([v2_finding("SCHEMA_UNAVAILABLE", V2_9_2_VERIFIER_PATH, "the structural verifier is unavailable")], "9.2")
    module = importlib_util.module_from_spec(specification)
    sys.modules[specification.name] = module
    specification.loader.exec_module(module)
    return module


def v2_9_2_receipt(repository: Path, relative: str) -> tuple[bytes, int]:
    """Read one regular, contained Story 9.2 evidence file without writing it."""
    lexical = repository / relative
    try:
        resolved = lexical.resolve(strict=True)
        if lexical.is_symlink() or not resolved.is_file() or not resolved.is_relative_to(repository):
            raise ValueError("not a contained regular file")
        return read_file_snapshot(resolved)
    except FileNotFoundError:
        raise V2Stop([v2_finding("TEST_RESULTS_MISSING", relative, "required Story 9.2 evidence is absent")], "9.2") from None
    except (OSError, ValueError):
        raise V2Stop([v2_finding("INPUT_SCHEMA_INVALID", relative, "evidence is not a readable contained regular file")], "9.2") from None


def v2_9_2_amendment(repository: Path, candidate: str, contract: dict[str, Any]) -> dict[str, Any]:
    """Apply only the reviewed, hash-pinned Story 9.2 command amendment; retain frozen bytes."""
    content, _ = v2_9_2_receipt(repository, V2_9_2_AMENDMENT_PATH)
    if (v2_sha256(content) != V2_9_2_AMENDMENT_SHA256
            or content != v2_committed_blob(repository, candidate, V2_9_2_AMENDMENT_PATH)):
        raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", V2_9_2_AMENDMENT_PATH,
                                 "the committed execution amendment differs from the reviewed digest")], "9.2")
    amendment = v2_parse_json(content)
    frozen = v2_committed_blob(repository, candidate, V2_9_2_CONTRACT_PATH)
    scenarios = amendment["effectiveScenarios"]
    if (amendment["storyId"] != "9.2" or amendment["frozenContract"] != {
            "path": V2_9_2_CONTRACT_PATH, "sha256": v2_sha256(frozen)}
            or [row["id"] for row in scenarios] != [row["id"] for row in contract["scenarios"]]
            or any(row["frozenCommand"] != original["command"] or row["resultSemantics"] != original["resultSemantics"]
                   for row, original in zip(scenarios, contract["scenarios"]))):
        raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", V2_9_2_AMENDMENT_PATH,
                                 "effective scenarios must bind every frozen scenario and its semantics")], "9.2")
    effective = {**contract, "scenarios": [{**original, "command": row["command"]}
                                            for original, row in zip(contract["scenarios"], scenarios)]}
    return effective


def v2_9_2_pair_history(repository: Path, revision: str, parent: str, delta: dict[str, str], validator: Any) -> bool:
    """Recognize only independently verified Story 9.2 pair-only publication/retraction."""
    paths = {"docs/release-evidence/story-9.2-final-record-v2.json", "docs/release-evidence/story-9.2-final-record-v2.md"}
    if not paths.intersection(delta):
        return False
    def stop() -> NoReturn:
        raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", "candidate scope",
                                 "Story 9.2 pair history must be a verified pair-only publication or retraction")], "9.2")
    if set(delta) != paths or set(delta.values()) not in ({"A"}, {"M"}, {"D"}):
        stop()
    json_path, markdown_path = sorted(paths)
    retained = parent if set(delta.values()) == {"D"} else revision
    content = v2_committed_blob(repository, retained, json_path)
    markdown = v2_committed_blob(repository, retained, markdown_path)
    try:
        record = v2_parse_json(content) if content is not None else None
        if (not isinstance(record, dict) or markdown is None or record.get("storyId") != "9.2"
                or v2_schema_errors(validator, record) or v2_verify_pair(content, markdown)
                or record["outputs"]["json"]["path"] != json_path or record["outputs"]["markdown"]["path"] != markdown_path
                or record["conformanceExecution"]["baselineCommit"] != V2_9_2_BASELINE
                or record["conformanceExecution"]["sourceRevisionId"] != record["candidate"]["commit"]):
            stop()
        original = record["candidate"]["commit"]
        if (not is_ancestor(repository, original, parent)
                or record["candidate"]["gitlinks"] != [{"path": path, "commit": commit, "mode": "160000"}
                                                        for path, commit in v2_raw_gitlinks(repository, original)]):
            stop()
        if set(delta.values()) == {"M"} and v2_retained_candidate(
                repository, parent, json_path, markdown_path, validator, "9.2", V2_9_2_SPEC_PATH,
                "9-2-make-the-portable-tier-structural-and-prove-complete-monotonic-tier-execution", True) != original:
            stop()
        if v2_retained_candidate(repository, retained, json_path, markdown_path, validator,
                                 "9.2", V2_9_2_SPEC_PATH, "9-2-make-the-portable-tier-structural-and-prove-complete-monotonic-tier-execution", True) != original:
            stop()
    except (V2Stop, ValueError, TypeError, KeyError, UnicodeError, GateError):
        stop()
    return True


def v2_9_2_environment(repository: Path, candidate: str, validator: Any) -> tuple[dict[str, Any], set[str]]:
    """Measure only the authorized environment and every owning promotion from root Git.

    The returned touched paths include intermediate commits, so reverting an
    extra promotion or protected-path change cannot conceal it in the net diff.
    No submodule is initialized, updated, or read.
    """
    def stop(message: str) -> NoReturn:
        raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", V2_9_2_ENVIRONMENT_PATH, message)], "9.2")

    try:
        content, _ = v2_9_2_receipt(repository, V2_9_2_ENVIRONMENT_PATH)
        amendment = v2_parse_json(content)
    except (V2Stop, ValueError, UnicodeError):
        stop("the authorized candidate-environment amendment is missing or malformed")
    if (v2_sha256(content) != V2_9_2_ENVIRONMENT_SHA256
            or v2_committed_blob(repository, candidate, V2_9_2_ENVIRONMENT_PATH) != content):
        stop("the committed candidate-environment amendment differs from the authorized digest")
    if (set(amendment) != {"schemaVersion", "storyId", "status", "authorization", "proposal"}
            or amendment["schemaVersion"] != "hexalith.conversations.story-9.2-candidate-environment-amendment.v1"
            or amendment["storyId"] != "9.2" or amendment["status"] != "authorized"
            or set(amendment["authorization"]) != {"actor", "authorizedOn", "statement", "evidence"}
            or amendment["authorization"]["actor"] != "user"
            or amendment["authorization"]["statement"] != "I authorize and approve"):
        stop("the closed amendment must record the actual Story 9.2 authorization")
    proposal = amendment["proposal"]
    material = {key: value for key, value in proposal.items() if key != "proposalSha256"}
    canonical = json.dumps(material, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    if (proposal["schemaVersion"] != "hexalith.conversations.story-9.2-candidate-environment-amendment-proposal.v1"
            or proposal["storyId"] != "9.2" or proposal["baselineCommit"] != V2_9_2_BASELINE
            or proposal["measuredCandidateCommit"] != V2_9_2_ENVIRONMENT_CANDIDATE
            or proposal["proposalSha256"] != V2_9_2_ENVIRONMENT_PROPOSAL_SHA256
            or v2_sha256(canonical) != V2_9_2_ENVIRONMENT_PROPOSAL_SHA256):
        stop("the embedded proposal must reproduce the authorized digest and original baseline")
    measured_candidate = proposal["measuredCandidateCommit"]
    if (try_resolve_commit(repository, measured_candidate) != measured_candidate
            or not is_ancestor(repository, V2_9_2_BASELINE, measured_candidate)
            or not is_ancestor(repository, measured_candidate, candidate)):
        stop("the measured environment must descend from the original baseline and precede SC-9.2")

    def gitlink_rows(records: Sequence[tuple[str, str, str, str, str, str]]) -> list[dict[str, str]]:
        return sorted(({"path": path, "beforeCommit": before, "afterCommit": after,
                        "beforeMode": before_mode, "afterMode": after_mode, "status": status}
                       for before_mode, after_mode, before, after, status, path in records
                       if "160000" in (before_mode, after_mode)), key=lambda row: row["path"])

    accepted = proposal["acceptedEnvironmentGitlinks"]
    roots = root_submodule_paths(repository, V2_9_2_BASELINE)
    if (roots != root_submodule_paths(repository, measured_candidate)
            or roots != root_submodule_paths(repository, candidate)
            or any(row["path"] not in roots or row["beforeMode"] != "160000"
                   or row["afterMode"] != "160000" or row["status"] != "M" for row in accepted)):
        stop("the amendment accepts only the exact existing mode-160000 root gitlinks")
    for revision in (measured_candidate, candidate):
        if (gitlink_rows(raw_diff_records(repository, V2_9_2_BASELINE, revision)) != accepted
                or [path for path, _ in v2_raw_gitlinks(repository, revision)] != roots):
            stop("the measured before/after root gitlinks differ from the authorized seven-row environment")

    observed_promotions = []
    touched: set[str] = set()
    revisions = decode(run_git(repository, "rev-list", "--reverse", f"{V2_9_2_BASELINE}..{candidate}").stdout).split()
    for revision in revisions:
        parents = decode(run_git(repository, "rev-list", "--parents", "-n", "1", revision).stdout).split()
        for parent in parents[1:]:
            records = raw_diff_records(repository, parent, revision)
            delta = committed_path_status(repository, parent, revision)
            pair_history = v2_9_2_pair_history(repository, revision, parent, delta, validator)
            if pair_history and len(parents) != 2:
                stop("Story 9.2 pair history requires an exact single parent")
            if not pair_history:
                touched.update(delta)
            changed = gitlink_rows(records)
            if changed:
                if len(parents) != 2:
                    stop("an environment promotion must have its exact single owning parent")
                observed_promotions.append({"commit": revision, "parent": parent, "changedGitlinks": changed})
    if observed_promotions != proposal["promotionCommits"]:
        stop("the owning promotion commits, parents, or diffs differ; additional promotions require new authorization")
    facts = {"amendment": {"path": V2_9_2_ENVIRONMENT_PATH, "sha256": v2_sha256(content)},
             "proposalSha256": proposal["proposalSha256"], "measuredCandidateCommit": measured_candidate,
             "gitlinks": gitlink_rows(raw_diff_records(repository, V2_9_2_BASELINE, candidate)),
             "promotionCommits": observed_promotions}
    environment_validator = validator.evolve(schema={"$defs": validator.schema["$defs"], "$ref": "#/$defs/candidateEnvironment"})
    if v2_schema_errors(environment_validator, facts):
        stop("the measured candidate-environment binding is not a closed valid record")
    return facts, touched


def v2_9_2_environment_successor_v2(repository: Path, candidate: str, module: Any) -> dict[str, Any]:
    """Bind the exact ec5c9be/v5 authority without changing the original v1 route."""
    try:
        binding = module.SUCCESSOR_V2.validate(repository, candidate)
    except module.SUCCESSOR_V2.AcceptanceError as error:
        raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", error.code, str(error))], "9.2") from error
    return {
        "route": "story-9.2-successor-v2",
        "approvedSourceCommit": binding["approvedSource"],
        "scopeAuthorization": {"path": module.SUCCESSOR_V2.PREPARER.AUTH,
                               "sha256": module.SUCCESSOR_V2.PREPARER.AUTH_SHA},
        "scopeProposal": {"path": module.SUCCESSOR_V2.PREPARER.SCOPE,
                          "sha256": module.SUCCESSOR_V2.PREPARER.SCOPE_SHA,
                          "proposalSha256": binding["scopeProposalSha256"]},
        "qualityApproval": binding["approval"],
        "rootGitlinks": module.SUCCESSOR_V2.root_gitlinks(repository, candidate),
    }


def v2_9_2_environment_successor_v3(repository: Path, candidate: str, module: Any) -> dict[str, Any]:
    """Bind the exact fbe2f50 scope and later pinned v6 Quality decision."""
    try:
        binding = module.SUCCESSOR_V3.validate(repository, candidate)
        approval = module.SUCCESSOR_V3.approved_quality(repository, candidate)
    except module.SUCCESSOR_V3.AcceptanceError as error:
        raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", error.code, str(error))], "9.2") from error
    return {
        "route": "story-9.2-successor-v3",
        "approvedSourceCommit": binding["approvedSource"],
        "scopeAuthorization": binding["scopeAuthorization"],
        "scopeProposal": binding["scopeProposal"],
        "qualityApproval": approval,
        "rootGitlinks": binding["rootGitlinks"],
    }


def v2_9_2_environment_successor_v4(repository: Path, candidate: str, module: Any, *,
                                    require_head: bool = True) -> dict[str, Any]:
    """Bind the exact v4 scope and v7 Quality decision."""
    try:
        binding = module.SUCCESSOR_V4.authority(repository, candidate, require_head=require_head)
        approval = module.SUCCESSOR_V4.approved_quality(repository, candidate)
    except module.SUCCESSOR_V4.AcceptanceError as error:
        raise V2Stop([v2_finding("AUTHORITY_BINDING_INVALID", error.code, str(error))], "9.2") from error
    return {
        "route": "story-9.2-successor-v4",
        "approvedSourceCommit": binding["approvedSource"],
        "scopeAuthorization": binding["scopeAuthorization"],
        "scopeProposal": binding["scopeProposal"],
        "qualityApproval": approval,
        "rootGitlinks": binding["rootGitlinks"],
    }


def v2_9_2_command(tokens: list[str]) -> dict[str, Any] | None:
    """Normalize only this amendment's xUnit v4 options for the existing successor reader."""
    if tokens[:1] != ["dotnet"] or len(tokens) < 2 or not tokens[1].endswith(".dll"):
        return v2_successor_command(tokens, V2_9_2_CONTRACT_PATH)
    normalized = []
    index = 0
    while index < len(tokens):
        token = tokens[index]
        if token in ("-parallelMode", "-class-", "-method-"):
            if index + 1 >= len(tokens) or (token == "-parallelMode" and tokens[index + 1] != "none"):
                return None
            index += 2
            continue
        normalized.append("-trx" if token == "-result-trx" else token)
        index += 1
    return v2_successor_command(normalized, V2_9_2_CONTRACT_PATH)


def v2_9_2_source_digest(repository: Path, candidate: str, module: Any) -> str:
    """Recompute the fault source binding from committed inputs, independently of fixture receipts."""
    rows = []
    for path in module.fault_source_paths(repository):
        blob = v2_committed_blob(repository, candidate, path)
        if blob is None or v2_9_2_receipt(repository, path)[0] != blob:
            raise V2Stop([v2_finding("TEST_RESULTS_STALE", path, "a fixture source/configuration input differs from the candidate")], "9.2")
        rows.append({"path": path, "sha256": v2_sha256(blob)})
    return v2_sha256(module.TIERING.canonical_json(rows))


def v2_9_2_faults(repository: Path, candidate: str, validator: Any, module: Any | None = None) -> list[dict[str, Any]]:
    """Require every measured fault exactly once, with candidate source bindings and byte restoration."""
    module = module or v2_9_2_verifier()
    def stop(code: str, subject: str, message: str) -> NoReturn:
        raise V2Stop([v2_finding(code, subject, message)], "9.2")

    content, modified = v2_9_2_receipt(repository, V2_9_2_FAULT_RESULT_PATH)
    candidate_ns = int(decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()) * 1_000_000_000
    if modified < candidate_ns:
        stop("TEST_RESULTS_STALE", V2_9_2_FAULT_RESULT_PATH, "fault evidence predates the candidate")
    try:
        parsed = v2_parse_junit(content)
    except (ValueError, ElementTree.ParseError):
        stop("INPUT_SCHEMA_INVALID", V2_9_2_FAULT_RESULT_PATH, "the fault receipt is malformed JUnit XML")
    if (parsed["reported"] != {"tests": len(parsed["cases"]), "failures": 0, "errors": 0, "skipped": 0}
            or not parsed["cases"] or any(case["failure"] or case["error"] or case["skipped"] for case in parsed["cases"])):
        stop("TEST_RESULTS_FAILED", V2_9_2_FAULT_RESULT_PATH, "fault cases are empty, failed, skipped, or inconsistent")
    observed_validator = validator.evolve(schema={"$defs": validator.schema["$defs"], "$ref": "#/$defs/structuralObservedFault"})
    source_digest = v2_9_2_source_digest(repository, candidate, module)
    rows = []
    for case in parsed["cases"]:
        metadata = [value for name, value in case["properties"] if name == module.FAULT_PROPERTY]
        try:
            row = v2_parse_json(metadata[0].encode()) if len(metadata) == 1 else None
            valid = isinstance(row, dict) and not v2_schema_errors(observed_validator, row)
        except (ValueError, UnicodeError):
            row, valid = None, False
        if isinstance(row, dict) and (row.get("restoredExitCode") != 0 or row.get("restoredBlockers") != []):
            stop("FIXTURE_NOT_RESTORED", V2_9_2_FAULT_RESULT_PATH, "restored verification did not pass")
        if not valid:
            stop("FAULT_NOT_DETECTED", V2_9_2_FAULT_RESULT_PATH, "each executed fault needs exactly one valid measured property")
        if (case["classname"] != V2_9_1_FAULT_CLASSNAME
                or case["name"] != f"test_structural_and_execution_faults[{row['id']}]"):
            stop("FAULT_NOT_DETECTED", row["id"], "the fault metadata does not identify its executed testcase")
        if row["candidateCommit"] != candidate or row["sourceInputsSha256"] != source_digest:
            stop("TEST_RESULTS_STALE", row["id"], "fault evidence names another candidate or different source bytes")
        if (row["expectedBlocker"] != module.FAULTS.get(row["id"]) or row["observedBlockers"] != [row["expectedBlocker"]]
                or row["observedExitCode"] != 1 or row["baselineExitCode"] != 0 or row["baselineBlockers"]):
            stop("FAULT_NOT_DETECTED", row["id"], "the mutation did not measure baseline PASS and its sole blocker")
        if row["beforeSha256"] != row["afterSha256"] or row["mutatedSha256"] == row["beforeSha256"]:
            stop("FIXTURE_NOT_RESTORED", row["id"], "the mutation made no change or restored different bytes")
        rows.append(row)
    if [row["id"] for row in rows] != list(module.FAULTS):
        stop("FAULT_NOT_DETECTED", V2_9_2_FAULT_RESULT_PATH, "the exact ordered structural/execution fault set is not measured once")
    return rows


def v2_9_2_facts(repository: Path, candidate: str, contract: dict[str, Any], validator: Any, *,
                 retained_record: bool = False) -> dict[str, Any]:
    """Re-derive full tier execution, migration, approval, retention, and predecessor facts."""
    def stop(code: str, subject: str, message: str) -> NoReturn:
        raise V2Stop([v2_finding(code if code in V2_CODES else "TEST_RESULTS_FAILED", subject, message)], "9.2")
    module = v2_9_2_verifier()
    v4_scope = getattr(module, "SUCCESSOR_V4", None)
    successor_v4 = (v4_scope is not None
                    and v2_committed_blob(repository, candidate, v4_scope.AUTH) is not None)
    if successor_v4:
        try:
            v4_scope.authority(repository, candidate, require_head=not retained_record)
            v4_scope.approved_quality(repository, candidate)
        except v4_scope.AcceptanceError as error:
            stop("AUTHORITY_BINDING_INVALID", error.code, str(error))
    v3_scope = getattr(module, "SUCCESSOR_V3", None)
    successor_v3 = (not successor_v4 and v3_scope is not None
                    and v2_committed_blob(repository, candidate, v3_scope.AUTH) is not None)
    if successor_v3:
        try:
            v3_scope.validate(repository, candidate)
        except v3_scope.AcceptanceError as error:
            stop("AUTHORITY_BINDING_INVALID", error.code, str(error))
        if v3_scope.QUALITY_APPROVAL_SHA is None:
            stop("TIER_APPROVAL_MISSING", v3_scope.QUALITY_APPROVAL,
                 "v3 source scope is authorized, but the changed source binding has no exact Quality decision")
    successor_authority = getattr(module, "SUCCESSOR_V2", None)
    successor_v2 = (successor_authority is not None
                    and v2_committed_blob(repository, candidate, successor_authority.APPROVAL) is not None)
    source_digest = v2_9_2_source_digest(repository, candidate, module)
    v2_9_2_amendment(repository, candidate, contract)
    predecessor_id, predecessor_path, predecessor_markdown = V2_9_2_PREDECESSOR
    predecessor_digest = v2_verified_predecessor(repository, candidate, predecessor_id, predecessor_path, predecessor_markdown, validator)
    if predecessor_digest is None:
        stop("AUTHORITY_BINDING_INVALID", predecessor_path, "Story 9.1's committed accepted pair does not verify")
    predecessor = v2_parse_json(v2_committed_blob(repository, candidate, predecessor_path))
    prior_candidate = predecessor["candidate"]["commit"]
    if prior_candidate == candidate or not is_ancestor(repository, prior_candidate, candidate):
        stop("AUTHORITY_BINDING_INVALID", predecessor_path, "Story 9.1 must be a strict candidate ancestor")
    spec = v2_committed_blob(repository, candidate, V2_9_2_SPEC_PATH)
    baseline = frontmatter_scalar(parse_frontmatter(spec.decode("utf-8")), "baseline_commit") if spec else None
    if baseline != V2_9_2_BASELINE or not is_ancestor(repository, baseline, candidate):
        stop("BASELINE_NOT_TRUSTWORTHY", V2_9_2_SPEC_PATH, "the original Story 9.2 baseline must be preserved")
    if successor_v4:
        environment = v2_9_2_environment_successor_v4(repository, candidate, module,
                                                      require_head=not retained_record)
        touched = set()
    elif successor_v3:
        environment = v2_9_2_environment_successor_v3(repository, candidate, module)
        touched = set()
    elif successor_v2:
        environment = v2_9_2_environment_successor_v2(repository, candidate, module)
        touched = set()
    else:
        environment, touched = v2_9_2_environment(repository, candidate, validator)
    allowed = {V2_9_2_SPEC_PATH, V2_7_2_SPRINT_PATH, V2_9_2_AMENDMENT_PATH, V2_9_2_ENVIRONMENT_PATH, V2_9_2_VERIFIER_PATH,
               V2_GENERATOR_PATH, "_bmad/scripts/tests/test_conformance_tiering.py", "_bmad/scripts/tests/test_generate_story_record.py",
               "_bmad/schemas/story-final-record-v2.schema.json", "docs/runbooks/story-final-record-generation.md",
               ".github/workflows/ci.yml", "Hexalith.Conversations.slnx", ".gitattributes", ".gitignore",
               module.MIGRATION, module.APPROVAL, module.SNAPSHOT, module.document(repository, module.AMENDMENT)["preSplitMachineResult"]["path"],
               "docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md",
               module.PROJECTS["module-internal"],
               "tests/Hexalith.Conversations.Conformance.Tests/ConformanceTierAssemblyInventory.cs",
               *(module.AUTHORIZED_SUCCESSOR_FILES)}
    if not successor_v2 and not successor_v3 and not successor_v4:
        allowed.update(row["path"] for row in environment["gitlinks"])
    if not successor_v2 and not successor_v3 and not successor_v4:
        changed = set(committed_path_status(repository, baseline, candidate)) | touched
        forbidden = sorted(path for path in changed if path not in allowed and not path.startswith((
            "tests/Hexalith.Conversations.Conformance.Portable.Tests/", "tests/Hexalith.Conversations.Conformance.Tests/Story92/")))
        if forbidden:
            stop("AUTHORITY_BINDING_INVALID", "candidate scope", "Story 9.2 changes protected paths: " + v2_path_summary(forbidden))
    # The v2 route instead measures both closed, user-authorized successor scopes
    # from the accepted original candidate through ec5c9be and constrains every
    # later commit to the additive evidence/tooling path set. The frozen baseline
    # and protected artifacts are still checked below.
    # Both acceptance results must come from the original story candidate. Synthetic
    # fixture PASS never supplies these assemblies or the acceptance result.
    for project in module.PROJECTS.values():
        assembly = str(PurePosixPath(project).parent / "bin/Release/net10.0" / (PurePosixPath(project).stem + ".dll"))
        content, _ = v2_9_2_receipt(repository, assembly)
        if dotnet_source_revisions(content) != [candidate]:
            stop("TEST_RESULTS_STALE", assembly, "retained tier assembly must be stamped by SC-9.2")
    report = (module.verify_authorized_successor_v4_scope(repository, candidate,
              portable_result="artifacts/v9/9.2/portable.trx", internal_result="artifacts/v9/9.2/internal.trx",
              require_head=not retained_record)
              if successor_v4 else module.verify_authorized_successor_v3_scope(repository, candidate,
              portable_result="artifacts/v9/9.2/portable.trx", internal_result="artifacts/v9/9.2/internal.trx")
              if successor_v3 else module.verify_approved_successor_v2(repository, candidate,
              portable_result="artifacts/v9/9.2/portable.trx", internal_result="artifacts/v9/9.2/internal.trx")
              if successor_v2 else module.verify(repository, portable_result="artifacts/v9/9.2/portable.trx",
                                                  internal_result="artifacts/v9/9.2/internal.trx"))
    if report["result"] != "PASS":
        blocker = report["blockers"][0]
        stop(blocker["code"], "tier verification", blocker["message"])
    receipt, _ = v2_9_2_receipt(repository, V2_9_2_RESULT_PATH)
    if v2_parse_json(receipt) != report:
        stop("TEST_RESULTS_STALE", V2_9_2_RESULT_PATH, "AC-9.2-08 differs from fresh read-only verification")
    if successor_v4:
        proposal = module.derive_migration(repository, current_tree=True)["proposal"]
        approval = module.document(repository, module.SUCCESSOR_V4.QUALITY_APPROVAL)
        migration_path, approval_path = module.SUCCESSOR_V4.QUALITY_PROPOSAL, module.SUCCESSOR_V4.QUALITY_APPROVAL
    elif successor_v3:
        proposal = module.document(repository, module.SUCCESSOR_V2.PROPOSAL)["successorMaterial"]["proposedMigration"]
        approval = module.document(repository, module.SUCCESSOR_V3.QUALITY_APPROVAL)
        migration_path, approval_path = module.SUCCESSOR_V3.QUALITY_PROPOSAL, module.SUCCESSOR_V3.QUALITY_APPROVAL
    elif successor_v2:
        material = module.document(repository, module.SUCCESSOR_V2.PROPOSAL)["successorMaterial"]
        proposal = material["proposedMigration"]
        approval = module.document(repository, module.SUCCESSOR_V2.APPROVAL)
        migration_path, approval_path = module.SUCCESSOR_V2.PROPOSAL, module.SUCCESSOR_V2.APPROVAL
    else:
        proposal = module.document(repository, module.MIGRATION)["proposal"]
        approval = module.document(repository, module.APPROVAL)
        migration_path, approval_path = module.MIGRATION, module.APPROVAL
    for path in (migration_path, approval_path):
        if v2_committed_blob(repository, candidate, path) != v2_9_2_receipt(repository, path)[0]:
            stop("TIER_APPROVAL_MISSING", path, "migration and genuine Quality decision must be committed at SC-9.2")
    if approval["approver"] == "SYNTHETIC-FIXTURE" or approval["approvalId"] == "SYNTHETIC-FIXTURE-NOT-AN-APPROVAL":
        stop("TIER_APPROVAL_MISSING", approval_path, "disposable fixture approval cannot authorize acceptance")
    try:
        frozen, amendment = module.inputs(repository, current_tree=successor_v2 or successor_v3 or successor_v4)
    except module.VerificationError as error:
        stop(error.code if error.code in V2_CODES else "TEST_RESULTS_FAILED",
             "tier inputs", str(error))
    protected = frozen["supersedes"]["v1Artifacts"] + frozen["supersedes"]["tieringLineage"] + [frozen["publicContract"]["reviewedClientBaseline"]]
    return {"bindingRule": V2_9_2_BINDING_RULE, "sourceRevisionId": candidate, "baselineCommit": baseline,
            "contract": module.bound(repository, V2_9_2_CONTRACT_PATH), "executionAmendment": module.bound(repository, V2_9_2_AMENDMENT_PATH),
            "candidateEnvironment": environment,
            "predecessorRecord": {"storyId": "9.1", "path": predecessor_path, "sha256": predecessor_digest},
            "predecessorCandidate": prior_candidate, "beforeDisposition": module.bound(repository, module.DISPOSITION),
            "preSplitResult": module.bound(repository, amendment["preSplitMachineResult"]["path"]),
            "migration": report["migration"], "proposalSha256": report["proposalSha256"], "approval": report["approval"],
            "inventories": report["inventories"], "inventorySha256": contract["inventory"]["sha256"],
            "declarations": {"solution": report["declarations"]["solution"], "workflow": report["declarations"]["workflow"],
                             "completionInventorySha256": v2_sha256(module.TIERING.canonical_json(report["declarations"]["completionInventory"]))},
            "portableSurfaceSha256": v2_sha256(module.TIERING.canonical_json(proposal["portableSurface"])),
            "fr20MembershipSha256": v2_sha256(module.TIERING.canonical_json(proposal["fr20Membership"])),
            "denominatorSuitesSha256": v2_sha256(module.TIERING.canonical_json(proposal["denominatorSuites"])),
            "publicDriftSha256": proposal["publicSurface"]["driftSha256"], "changedAssertionRows": len(proposal["changedAssertions"]),
            "frozenDefinitions": len(frozen["assertions"]), "historicalExclusions": amendment["executionPolicy"]["historicalFrozenExclusions"],
            "historicalValidationControls": len(frozen["validationAdditions"]), "execution": report["execution"],
            "protectedArtifacts": [{"path": row["path"], "sha256": row["sha256"]} for row in protected],
            "faultEvidence": {"path": V2_9_2_FAULT_RESULT_PATH, "sha256": v2_sha256(v2_9_2_receipt(repository, V2_9_2_FAULT_RESULT_PATH)[0])},
            "faultSourceInputsSha256": source_digest}


def v2_generate(options: dict[str, str], *, verify_spec: str | None = None) -> bytes:
    """Derive, validate, and atomically write the v2 pair; return the JSON bytes."""
    if options.get("--contract") == V2_11_3_CONTRACT:
        try:
            return v2_11_3_generate(options)
        except ValueError as error:
            raise V2Stop([v2_finding('RECORD_NOT_DERIVED', '11.3', str(error))], '11.3') from None
    if options.get("--contract") == V2_11_2_CONTRACT:
        try:
            return v2_11_2_generate(options)
        except ValueError as error:
            raise V2Stop([v2_finding('RECORD_NOT_DERIVED', '11.2', str(error))], '11.2') from None
    if options.get("--contract") == V2_11_1_CONTRACT:
        try:
            return v2_11_1_generate(options)
        except ValueError as error:
            raise V2Stop([v2_finding('RECORD_NOT_DERIVED', '11.1', str(error))], '11.1') from None
    if options.get("--contract") == V2_10_4_CONTRACT:
        try:
            return v2_10_4_generate(options, verify_spec=verify_spec)
        except ValueError as error:
            raise V2Stop([v2_finding('RECORD_NOT_DERIVED', '10.4', str(error))], '10.4') from None
    if options.get("--contract") == V2_10_3_CONTRACT:
        return v2_10_3_generate(options, verify_spec=verify_spec)
    try:
        repository = validate_repository(
            Path(options.get("--repository") or default_repository()).expanduser()
        )
    except GateError as error:
        if error.code == "GIT_UNAVAILABLE":
            raise V2Stop([v2_finding("GIT_UNAVAILABLE", "git", "git is not available on PATH")])
        raise V2Stop(
            [
                v2_finding(
                    "ARGUMENT_INVALID",
                    "--repository",
                    "--repository must name the root of an existing Git repository",
                )
            ]
        ) from None

    try:
        contract_path = safe_relative_path(options["--contract"])
        output_json = safe_relative_path(options["--output-json"])
        output_markdown = safe_relative_path(options["--output-markdown"])
    except GateError:
        raise V2Stop(
            [
                v2_finding(
                    "ARGUMENT_INVALID",
                    "argv",
                    "--contract, --output-json, and --output-markdown must be normalized "
                    "repository-relative paths",
                )
            ]
        ) from None

    _, validators = v2_load_schemas()

    try:
        candidate = resolve_commit(repository, "HEAD", "CANDIDATE_UNRESOLVABLE")
    except GateError as error:
        if error.code in ("GIT_UNAVAILABLE", "GIT_COMMAND_FAILED"):
            raise
        raise V2Stop(
            [
                v2_finding(
                    "RECORD_NOT_DERIVED",
                    "candidate",
                    "HEAD does not resolve to a committed candidate; nothing can be derived",
                ),
                v2_finding(
                    "ASSERTION_LEDGER_EMPTY",
                    "candidate",
                    "no assertion can execute without a committed candidate",
                ),
            ]
        ) from None

    head = candidate
    retained = v2_retention_config(repository, contract_path)
    if retained is not None:
        candidate = v2_retained_candidate(repository, candidate, output_json, output_markdown,
                                          validators["record"], *retained)
    # Retention has proved every commit after a retained candidate lifecycle-only,
    # so a result rerun on one of those commits still describes the candidate.
    stamped = frozenset(
        decode(run_git(repository, "rev-list", "--ancestry-path", f"{candidate}..{head}").stdout)
        .split()
    ) if candidate != head else frozenset()

    contract = v2_validate_contract(
        v2_committed_blob(repository, candidate, contract_path),
        contract_path,
        validators["contract"],
        validators["contract_v14"],
    )
    story_id = contract["storyId"]
    frozen_contract = contract
    if story_id == "9.2":
        contract = v2_9_2_amendment(repository, candidate, contract)
    findings: list[dict[str, str]] = []

    declared_json, declared_markdown = contract["finalRecord"]["paths"]
    if (output_json, output_markdown) != (declared_json, declared_markdown):
        findings.append(
            v2_finding(
                "CALLER_AUTHORED_FACT",
                "--output-json/--output-markdown",
                "the output paths differ from the contract's finalRecord.paths; output "
                "paths are contract facts, not caller choices",
            )
        )

    authority = v2_authority(
        repository, candidate, contract_path, contract, validators["bundle"], findings
    )
    gitlinks = v2_gitlinks(repository, candidate, findings, story_id)
    gitlink_paths = [item["path"] for item in gitlinks]
    if not gitlink_paths:
        try:
            gitlink_paths = root_submodule_paths(repository, candidate)
        except GateError as error:
            if error.code != "INVALID_SCOPE":
                raise

    # Classify every scenario command before touching a result file.
    pytest_scenarios: list[tuple[dict[str, Any], dict[str, str | None]]] = []
    acceptance_scenarios: list[tuple[dict[str, Any], dict[str, str]]] = []
    ux_scenarios: list[tuple[dict[str, Any], dict[str, str]]] = []
    tiering_scenarios: list[dict[str, Any]] = []
    successor_scenarios: list[tuple[dict[str, Any], dict[str, Any]]] = []
    self_scenarios: list[dict[str, Any]] = []
    for position, scenario in enumerate(contract["scenarios"]):
        try:
            tokens = shlex.split(scenario["command"])
        except ValueError:
            tokens = []
        ux_command = v2_ux_command(tokens, scenario["id"]) if story_id == "8.1" else None
        tiering_command = v2_tiering_command(tokens, scenario["id"]) if story_id == "9.1" else None
        pytest_command = v2_pytest_command(tokens)
        generator_command = None if pytest_command else v2_generator_command(tokens)
        successor_command = (
            (v2_9_2_command(tokens) if story_id == "9.2" else v2_successor_command(tokens, contract_path))
            if int(story_id.split(".")[0]) >= 8 and pytest_command is None
            and ux_command is None and tiering_command is None and generator_command is None else None
        )
        acceptance_command = (
            None if pytest_command or generator_command else v2_acceptance_command(tokens)
        )
        if ux_command is not None:
            ux_scenarios.append((scenario, ux_command))
        elif tiering_command is not None:
            tiering_scenarios.append(scenario)
        elif generator_command is not None:
            permitted_early = v2_early_generator_permitted(
                story_id, position, contract["scenarios"], contract_path,
            )
            if self_scenarios or (position != len(contract["scenarios"]) - 1 and not permitted_early):
                findings.append(v2_finding("SCENARIO_COMMAND_UNSUPPORTED", scenario["id"],
                                           "the generator invocation must be single and final unless a declared read-only authority check follows"))
                continue
            if (
                generator_command.get("--repository") != "."
                or generator_command.get("--contract") != contract_path
                or generator_command.get("--output-json") != output_json
                or generator_command.get("--output-markdown") != output_markdown
                or generator_command.get("--format") != options["--format"]
            ):
                findings.append(v2_finding("SCENARIO_RESULT_MISMATCH", scenario["id"],
                                           "this invocation is not the contract's declared self-invocation"))
                continue
            self_scenarios.append(scenario)
        elif successor_command is not None:
            successor_scenarios.append((scenario, successor_command))
        elif acceptance_command is not None:
            try:
                safe_relative_path(acceptance_command["script"])
                safe_relative_path(acceptance_command["output"])
                valid = (
                    acceptance_command["repository"] == "."
                    and acceptance_command["contract"] == contract_path
                    and acceptance_command["scenario"] == scenario["id"]
                    and acceptance_command["output"].endswith(".json")
                    and not v2_below(acceptance_command["output"], gitlink_paths)
                )
            except GateError:
                valid = False
            if not valid or v2_committed_blob(
                repository, candidate, acceptance_command["script"]
            ) is None:
                findings.append(
                    v2_finding(
                        "SCENARIO_COMMAND_UNSUPPORTED",
                        scenario["id"],
                        "the acceptance-result command must name a committed script, this "
                        "repository, this contract and scenario, and a repository-relative "
                        ".json output outside every gitlink",
                    )
                )
                continue
            acceptance_scenarios.append((scenario, acceptance_command))
        elif pytest_command is not None:
            try:
                safe_relative_path(str(pytest_command["junit"]))
                safe_relative_path(str(pytest_command["target"]))
                valid = not v2_below(str(pytest_command["junit"]), gitlink_paths)
            except GateError:
                valid = False
            if not valid or v2_committed_blob(
                repository, candidate, str(pytest_command["target"])
            ) is None:
                findings.append(
                    v2_finding(
                        "SCENARIO_COMMAND_UNSUPPORTED",
                        scenario["id"],
                        "the pytest target must be committed at the candidate and the "
                        "JUnit path must be repository-relative outside every gitlink",
                    )
                )
                continue
            pytest_scenarios.append((scenario, pytest_command))
        else:
            findings.append(
                v2_finding(
                    "SCENARIO_COMMAND_UNSUPPORTED",
                    scenario["id"],
                    "the scenario command is neither a supported pytest JUnit command nor "
                    "the generator self-invocation",
                )
            )
    if not self_scenarios and not any(
        item["code"] == "SCENARIO_COMMAND_UNSUPPORTED" for item in findings
    ):
        findings.append(
            v2_finding(
                "SCENARIO_COMMAND_UNSUPPORTED",
                story_id,
                "the contract declares no generator self-invocation scenario",
            )
        )
    junit_paths = [str(command["junit"]) for _, command in pytest_scenarios]
    if len(junit_paths) != len(set(junit_paths)):
        findings.append(
            v2_finding(
                "SCENARIO_RESULT_MISMATCH",
                story_id,
                "two scenarios declare the same JUnit result path",
            )
        )
    acceptance_paths = [command["output"] for _, command in acceptance_scenarios]
    ux_result_paths = [command["output"] for _, command in ux_scenarios if command["kind"] == "trx"]
    successor_result_paths = [
        path for _, command in successor_scenarios
        for path in ([command["output"]] if command["kind"] == "xunit" else command.get("outputs", []))
    ]
    result_paths = [*junit_paths, *acceptance_paths, *ux_result_paths, *successor_result_paths]
    if len(result_paths) != len(set(result_paths)):
        findings.append(
            v2_finding(
                "SCENARIO_RESULT_MISMATCH",
                story_id,
                "two scenarios declare the same result path",
            )
        )

    for label, path in (("--output-json", output_json), ("--output-markdown", output_markdown)):
        if v2_below(path, gitlink_paths) or path in result_paths or path == contract_path:
            findings.append(
                v2_finding(
                    "OUTPUT_PATH_INVALID",
                    label,
                    "an output path may not lie below a gitlink or alias an input",
                )
            )

    allowed_dirt = {output_json, output_markdown, *result_paths}
    if verify_spec is not None:
        designated_spec = {"8.2": V2_8_2_SPEC_PATH, "9.2": V2_9_2_SPEC_PATH,
                           "10.1": retained[1] if retained is not None else None,
                           "10.2": retained[1] if retained is not None else None}.get(story_id)
        if verify_spec != designated_spec:
            raise V2Stop([v2_finding("ARGUMENT_INVALID", "verification", "read-only rederivation requires this story's designated spec")], story_id)
        allowed_dirt.add(verify_spec)
    dirt = sorted(set(worktree_path_status(repository)) - allowed_dirt)
    if dirt:
        findings.append(
            v2_finding(
                "SOURCE_TREE_DIRTY" if story_id == "7.2" else "WORKTREE_NOT_CLEAN",
                "worktree",
                "the working tree differs from the committed candidate outside the "
                f"declared outputs and results: {v2_path_summary(dirt)}",
            )
        )

    candidate_time = int(
        decode(run_git(repository, "show", "-s", "--format=%ct", candidate).stdout).strip()
    )
    if story_id == "8.2":
        for path in result_paths:
            v2_8_2_evidence(repository, path)
    scenario_records: dict[str, dict[str, Any]] = {}
    categories: dict[str, str] = {}
    parsed_results = 0
    for scenario, command in pytest_scenarios:
        scenario_record, category, scenario_findings = v2_scenario_from_results(
            repository, scenario, command, candidate_time * 1_000_000_000
        )
        scenario_records[scenario["id"]] = scenario_record
        categories[scenario["id"]] = category
        findings.extend(scenario_findings)
        if "resultFile" in scenario_record:
            parsed_results += 1
    acceptance_inputs: dict[str, list[dict[str, str]]] = {}
    if acceptance_scenarios:
        acceptance_validator = v2_load_acceptance_validator()
        for scenario, command in acceptance_scenarios:
            scenario_record, category, scenario_findings, inputs = v2_scenario_from_acceptance(
                repository,
                scenario,
                command,
                story_id,
                candidate,
                candidate_time * 1_000_000_000,
                acceptance_validator,
                stamped,
            )
            scenario_records[scenario["id"]] = scenario_record
            categories[scenario["id"]] = category
            findings.extend(scenario_findings)
            acceptance_inputs[scenario["id"]] = inputs
            if "resultFile" in scenario_record:
                parsed_results += 1
    ux_facts = None
    if story_id == "8.1" and not findings:
        if [scenario["id"] for scenario, _ in ux_scenarios] != [f"AC-8.1-0{i}" for i in range(1, 7)]:
            findings.append(v2_finding("SCENARIO_COMMAND_UNSUPPORTED", story_id,
                                       "Story 8.1 requires its six exact ordered commands"))
        else:
            ux_facts = v2_ux_facts(repository, candidate, contract_path, contract, validators["record"])
            assembly_ns = ux_facts.pop("testAssemblyMtimeNs")
            pinned_ux_trx = v2_ux_pinned_trx_digests(repository, head, candidate, output_json)
            for scenario, command in ux_scenarios:
                scenario_record, category, scenario_findings = v2_ux_scenario_from_results(
                    repository, scenario, command, candidate_time * 1_000_000_000, assembly_ns,
                    ux_facts, pinned_ux_trx.get(scenario["id"]) if pinned_ux_trx else None,
                )
                scenario_records[scenario["id"]] = scenario_record
                categories[scenario["id"]] = category
                findings.extend(scenario_findings)
                if "resultFile" in scenario_record:
                    parsed_results += 1
    if story_id == "9.1" and not findings:
        if [scenario["id"] for scenario in tiering_scenarios] != ["AC-9.1-01"]:
            findings.append(v2_finding("SCENARIO_COMMAND_UNSUPPORTED", story_id,
                                       "Story 9.1 requires its exact AC-9.1-01 disposition command"))
        for scenario in tiering_scenarios:
            scenario_record, category, scenario_findings = v2_9_1_scenario_from_results(repository, candidate, scenario)
            scenario_records[scenario["id"]] = scenario_record
            categories[scenario["id"]] = category
            findings.extend(scenario_findings)
            if "resultFile" in scenario_record:
                parsed_results += 1
    for scenario, command in (successor_scenarios if not findings else ()):
        if findings:
            break
        scenario_record, category, scenario_findings = v2_successor_scenario_from_results(
            repository, scenario, command, candidate, candidate_time * 1_000_000_000,
            gitlink_paths, stamped, allowed_dirt,
            **({"execute_commands": False} if story_id in ("9.2", "10.1", "10.2") and verify_spec is not None else {}),
        )
        scenario_records[scenario["id"]] = scenario_record
        categories[scenario["id"]] = category
        findings.extend(scenario_findings)
        if "resultFile" in scenario_record:
            parsed_results += 1
    ledger_rows = sum(
        len(item.get("assertionLedger", [])) for item in scenario_records.values()
    )
    if (pytest_scenarios or acceptance_scenarios or ux_scenarios or tiering_scenarios or successor_scenarios) \
            and parsed_results == 0:
        findings.append(
            v2_finding(
                "RECORD_NOT_DERIVED",
                "scenarios",
                "no declared JUnit result file was parsed; a run that derived nothing "
                "proves nothing",
            )
        )
    if ledger_rows == 0:
        findings.append(
            v2_finding(
                "ASSERTION_LEDGER_EMPTY",
                "scenarios",
                "no executed assertion was derived from any scenario result",
            )
        )
        if not any(item["code"] == "RECORD_NOT_DERIVED" for item in findings):
            findings.append(
                v2_finding(
                    "RECORD_NOT_DERIVED",
                    "scenarios",
                    "the scenario results yield no derived assertion",
                )
            )

    if findings or authority is None or not self_scenarios:
        raise V2Stop(findings or [
            v2_finding("RECORD_NOT_DERIVED", story_id, "the record could not be derived")
        ], story_id)

    self_scenario = self_scenarios[0]
    scenario_records[self_scenario["id"]] = {
        "scenarioId": self_scenario["id"],
        "command": self_scenario["command"],
        "exitCode": 0,
        "result": "PASS",
        "blockers": [],
        "assertionLedger": v2_self_ledger(self_scenario["id"], story_id),
    }
    categories[self_scenario["id"]] = "passed"
    ordered = [scenario_records[scenario["id"]] for scenario in contract["scenarios"]]
    summary = {
        "required": len(ordered),
        "passed": sum(1 for value in categories.values() if value == "passed"),
        "failed": sum(1 for value in categories.values() if value == "failed"),
        "blocked": 0,
        "skipped": sum(1 for value in categories.values() if value == "skipped"),
        "notRun": sum(1 for value in categories.values() if value == "notRun"),
    }
    record = {
        "schemaVersion": V2_RECORD_SCHEMA_VERSION,
        "storyId": story_id,
        "authority": authority,
        "candidate": {"commit": candidate, "gitlinks": gitlinks},
        "predecessors": list(contract["predecessors"]),
        "inventory": {
            "id": contract["inventory"]["id"],
            "sha256": contract["inventory"]["sha256"],
        },
        "scenarios": ordered,
        "faultInjection": {"results": []},
        "outputs": {
            "json": {"path": output_json, "sha256": V2_ZERO_DIGEST},
            "markdown": {"path": output_markdown, "sha256": V2_ZERO_DIGEST},
        },
        "rollback": {"boundary": contract["rollback"]["boundary"]},
        "summary": summary,
        "renderedMarkdownSha256": V2_ZERO_DIGEST,
    }
    if story_id == "7.2":
        record["measurements"] = v2_story_7_2_measurements(
            repository, candidate, gitlinks, {output_json, output_markdown}, validators
        )
    if story_id == "7.3":
        record["workflowIntegration"] = v2_story_7_3_workflow_integration(
            repository, candidate, contract_path, acceptance_inputs, validators
        )
    if story_id == "7.4":
        record["historicalVerification"], record["faultInjection"]["results"] = (
            v2_story_7_4_verification(repository, candidate, contract_path, scenario_records, validators)
        )
    if story_id == "8.1":
        record["uxDisposition"] = ux_facts
    if story_id == "9.1":
        record["conformanceTiering"] = v2_9_1_facts(repository, candidate, contract, validators["record"])
        record["faultInjection"]["results"] = v2_9_1_faults(repository, candidate, validators["record"])
    if story_id == "9.2":
        record["conformanceExecution"] = v2_9_2_facts(
            repository, candidate, frozen_contract, validators["record"],
            **({"retained_record": True} if verify_spec is not None and candidate != head else {}))
        record["faultInjection"]["results"] = v2_9_2_faults(repository, candidate, validators["record"])
    if story_id == "10.1":
        record["testSupportExecution"] = v2_10_1_facts(
            repository, candidate, contract_path, contract, authority, scenario_records, validators["record"])
    if story_id == "10.2":
        record["evidenceBoundaryExecution"] = v2_10_2_facts(
            repository, candidate, contract_path, contract, authority, scenario_records, validators["record"])
        record["faultInjection"]["results"] = record["evidenceBoundaryExecution"]["mutationLedger"]
    if story_id == "8.2":
        v2_8_2_positive(repository, candidate, scenario_records["AC-8.2-01"])
        record["uxValidation"] = v2_8_2_facts(repository, candidate, contract, validators["record"])
        record["faultInjection"]["results"] = v2_8_2_faults(repository, candidate, scenario_records, validators["record"])
    if summary != contract["finalRecord"]["summary"]:
        raise V2Stop(
            [
                v2_finding(
                    "RECORD_NOT_DERIVED",
                    "summary",
                    "the derived summary does not equal the contract's required summary",
                )
            ],
            story_id,
        )

    final, json_bytes, markdown_bytes = v2_finalize(record)
    _, json_again, markdown_again = v2_finalize(record)
    if (json_bytes, markdown_bytes) != (json_again, markdown_again):
        raise V2Stop(
            [
                v2_finding(
                    "RECORD_CONTENT_DRIFT",
                    "record",
                    "two renderings of identical derived inputs differ",
                )
            ],
            story_id,
        )
    schema_errors = v2_schema_errors(validators["record"], final)
    if schema_errors:
        raise V2Stop(
            [
                v2_finding(
                    "OUTPUT_SCHEMA_INVALID",
                    "record",
                    "the derived record violates the final-record schema at "
                    + "; ".join(schema_errors[:10]),
                )
            ],
            story_id,
        )
    problems = v2_verify_pair(json_bytes, markdown_bytes)
    if problems:
        raise V2Stop(
            [v2_finding("RECORD_CONTENT_DRIFT", "record", "; ".join(problems))], story_id
        )

    if verify_spec is not None:
        return json_bytes

    try:
        targets = [
            (v2_output_target(repository, output_json), json_bytes),
            (v2_output_target(repository, output_markdown), markdown_bytes),
        ]
    except (OSError, ValueError):
        raise V2Stop(
            [
                v2_finding(
                    "OUTPUT_PATH_INVALID",
                    "outputs",
                    "an output path escapes the repository or names a symlink or non-file",
                )
            ],
            story_id,
        ) from None
    try:
        v2_write_outputs(targets)
    except OSError as error:
        raise V2Stop(
            [
                v2_finding(
                    "OUTPUT_WRITE_FAILED",
                    "outputs",
                    "the outputs could not be written; " + v2_restore_claim(error),
                )
            ],
            story_id,
        ) from None
    except V2OutputDrift as error:
        raise V2Stop(
            [
                v2_finding(
                    "RECORD_CONTENT_DRIFT",
                    "outputs",
                    "the installed output bytes differ from the generated bytes; "
                    + v2_restore_claim(error),
                )
            ],
            story_id,
        ) from None
    return json_bytes


def v2_verify_inserted(options: dict[str, str]) -> bytes:
    """Prove a spec's inserted record region is the committed pair's Markdown, byte for byte.

    The pair is read from `HEAD`, where the record-only commit placed it, and
    from the working tree, which must match it. The JSON record must verify
    against the final-record schema and its own digest bindings. The spec
    region between the single `STORY-FINAL-RECORD` marker pair must equal the
    Markdown bytes and hash to `renderedMarkdownSha256`. Nothing is written.
    """
    try:
        repository = validate_repository(
            Path(options.get("--repository") or default_repository()).expanduser()
        )
    except GateError as error:
        if error.code == "GIT_UNAVAILABLE":
            raise V2Stop([v2_finding("GIT_UNAVAILABLE", "git", "git is not available on PATH")])
        raise V2Stop(
            [
                v2_finding(
                    "ARGUMENT_INVALID",
                    "--repository",
                    "--repository must name the root of an existing Git repository",
                )
            ]
        ) from None
    try:
        contract_path = safe_relative_path(options["--contract"])
    except GateError:
        raise V2Stop(
            [v2_finding("ARGUMENT_INVALID", "--contract", "--contract must be a normalized "
                        "repository-relative path")]
        ) from None
    _, validators = v2_load_schemas()
    try:
        head = resolve_commit(repository, "HEAD", "CANDIDATE_UNRESOLVABLE")
    except GateError as error:
        if error.code in ("GIT_UNAVAILABLE", "GIT_COMMAND_FAILED"):
            raise
        raise V2Stop(
            [v2_finding("RECORD_CONTENT_DRIFT", "HEAD", "no committed record pair can exist "
                        "without a committed HEAD")]
        ) from None
    head_contract_blob = v2_committed_blob(repository, head, contract_path)
    contract = v2_validate_contract(head_contract_blob, contract_path, validators["contract"],
                                    validators["contract_v14"])
    story_id = contract["storyId"]
    retained = v2_retention_config(repository, contract_path)
    expected_outputs = V2_RETAINED_OUTPUTS.get(story_id)
    if expected_outputs is None and retained is not None and int(story_id.split(".")[0]) >= 8:
        expected_outputs = v2_designated_successor_outputs(story_id)
    if retained is None or retained[0] != story_id or expected_outputs is None:
        raise V2Stop(
            [
                v2_finding(
                    "ARGUMENT_INVALID",
                    "--contract",
                    f"{V2_VERIFY_OPTION} requires a retained-candidate story contract",
                )
            ],
            story_id,
        )
    json_path, markdown_path = contract["finalRecord"]["paths"]
    if (json_path, markdown_path) != expected_outputs:
        raise V2Stop(
            [
                v2_finding(
                    "INPUT_SCHEMA_INVALID",
                    "--contract",
                    "the current contract's finalRecord.paths do not match the designated "
                    "retained record pair",
                )
            ],
            story_id,
        )
    candidate = v2_retained_candidate(
        repository,
        head,
        json_path,
        markdown_path,
        validators["record"],
        *retained,
    )
    candidate_contract_blob = v2_committed_blob(repository, candidate, contract_path)
    if candidate_contract_blob != head_contract_blob:
        raise V2Stop(
            [
                v2_finding(
                    "RECORD_CONTENT_DRIFT",
                    "--contract",
                    "the current contract differs from the retained candidate's contract",
                )
            ],
            story_id,
        )
    v2_validate_contract(candidate_contract_blob, contract_path, validators["contract"],
                         validators["contract_v14"])

    spec_argument = Path(options[V2_VERIFY_OPTION])
    lexical = spec_argument if spec_argument.is_absolute() else repository / spec_argument
    try:
        resolved = lexical.resolve(strict=True)
        spec_relative = resolved.relative_to(repository).as_posix()
        if lexical.is_symlink() or not resolved.is_file():
            raise ValueError("not a regular file")
        if spec_relative != retained[1]:
            raise ValueError("not the contract's designated story spec")
        spec_bytes, _ = read_file_snapshot(resolved)
    except (OSError, ValueError):
        raise V2Stop(
            [
                v2_finding(
                    "ARGUMENT_INVALID",
                    V2_VERIFY_OPTION,
                    f"{V2_VERIFY_OPTION} must name a regular spec file inside the repository",
                )
            ],
            story_id,
        ) from None

    def drift(subject: str, message: str) -> NoReturn:
        raise V2Stop([v2_finding("RECORD_CONTENT_DRIFT", subject, message)], story_id)

    json_bytes = v2_committed_blob(repository, head, json_path)
    markdown_bytes = v2_committed_blob(repository, head, markdown_path)
    if json_bytes is None or markdown_bytes is None:
        drift("outputs", "the contract's record pair is not committed at HEAD; commit it first")
    for path, committed in ((json_path, json_bytes), (markdown_path, markdown_bytes)):
        installed = repository / path
        if installed.is_symlink() or not installed.is_file() or installed.read_bytes() != committed:
            drift(path, "the working-tree record output differs from the committed pair")
    dirt = set(worktree_path_status(repository))
    outside_spec = dirt - {spec_relative}
    if outside_spec:
        raise V2Stop(
            [v2_finding("WORKTREE_NOT_CLEAN", "working-tree", "unrelated working-tree "
                        f"changes are present: {v2_path_summary(outside_spec)}")],
            story_id,
        )
    if not v2_working_spec_lifecycle_only_change(
        repository, head, spec_relative, spec_bytes, record_region=retained[3],
        task_checkboxes=int(story_id.split(".")[0]) >= 8,
    ):
        drift(V2_VERIFY_OPTION, "the working spec changes content outside lifecycle-owned "
              "fields, sections, or the final-record region")
    try:
        record = v2_parse_json(json_bytes)
        valid = (
            isinstance(record, dict)
            and record.get("storyId") == story_id
            and not v2_schema_errors(validators["record"], record)
            and record["outputs"]["json"]["path"] == json_path
            and record["outputs"]["markdown"]["path"] == markdown_path
            and not v2_verify_pair(json_bytes, markdown_bytes)
        )
    except (UnicodeDecodeError, ValueError, KeyError, TypeError):
        valid = False
    if not valid:
        drift("outputs", "the committed record pair does not verify against its schema and digests")
    region = v2_record_region(spec_bytes)
    if region is None:
        drift(
            V2_VERIFY_OPTION,
            "the spec does not carry exactly one well-formed STORY-FINAL-RECORD marker pair",
        )
    inserted = spec_bytes[region[0] : region[1]]
    if inserted != markdown_bytes or v2_sha256(inserted) != record["renderedMarkdownSha256"]:
        drift(
            V2_VERIFY_OPTION,
            "the inserted record region differs from the committed Markdown and its "
            "renderedMarkdownSha256",
        )
    if story_id in ("8.2", "9.2", "10.1", "10.2"):
        rederived = v2_generate({"--repository": str(repository), "--contract": contract_path,
                                "--format": "bundle", "--output-json": json_path,
                                "--output-markdown": markdown_path}, verify_spec=spec_relative)
        if rederived != json_bytes:
            drift("record", "the inserted record differs from the full contract and receipt rederivation")
    return json_bytes


def v2_failure_document(
    findings: list[dict[str, str]], story_id: str | None
) -> dict[str, Any]:
    codes: list[str] = []
    diagnostics: list[dict[str, str]] = []
    for item in findings:
        if item["code"] not in codes:
            codes.append(item["code"])
        if item not in diagnostics:
            diagnostics.append(item)
    blocked = any(V2_CODES[code] == "BLOCKED" for code in codes)
    document: dict[str, Any] = {
        "schemaVersion": V2_FAILURE_SCHEMA_VERSION,
        "result": "BLOCKED" if blocked else "FAIL",
        "exitCode": 2 if blocked else 1,
    }
    if story_id is not None:
        document["storyId"] = story_id
    document["blockers"] = codes
    document["diagnostics"] = diagnostics
    return document


def v2_write_stdout(content: bytes) -> None:
    buffer = getattr(sys.stdout, "buffer", None)
    if buffer is None:  # pragma: no cover - exotic stdout
        sys.stdout.write(content.decode("utf-8"))
        sys.stdout.flush()
        return
    sys.stdout.flush()
    buffer.write(content)
    buffer.flush()


# Story 10.3 is an additive current-policy binding. The original V9 authority
# bundle is retained and hashed as provenance, never rewritten to enroll it.
V2_10_3_CONTRACT = '_bmad-output/planning-artifacts/story-10.3-current-contract-v1.json'
V2_10_3_BINDING = '_bmad-output/planning-artifacts/story-10.3-current-binding-v1.json'
V2_10_3_SPEC = '_bmad-output/implementation-artifacts/spec-10-3-provide-the-evidence-boundary-verifier-and-integrate-every-workflow-surface.md'


def v2_10_3_current_module() -> Any:
    """Load this tool's sibling route even when the generator was imported by path."""
    directory = str(Path(__file__).resolve().parent)
    if directory not in sys.path:
        sys.path.insert(0,directory)
    import evidence_boundary_current
    return evidence_boundary_current


def v2_10_3_pair_errors(record: dict[str, Any]) -> list[str]:
    """Check the closed cross-bindings independently of a pair's digest fields."""
    try:
        facts = record['evidenceWorkflowExecution']
        scenarios = record['scenarios']
        by_id = {row['scenarioId']: row for row in scenarios}
        expected = [f'AC-10.3-0{number}' for number in range(1, 9)]
        if (facts['sourceRevisionId'] != record['candidate']['commit']
                or record['predecessors'] != ['10.2']
                or [row['scenarioId'] for row in scenarios] != expected
                or any(row['result'] != 'PASS' or row['exitCode'] != 0 or row['blockers']
                       or not row.get('assertionLedger') for row in scenarios)
                or record['summary'] != dict(required=8, passed=8, failed=0, blocked=0, skipped=0, notRun=0)
                or record['inventory'] != {key:facts['workflowInventory'][key] for key in ('id','sha256')}
                or facts['binding']['path'] != V2_10_3_BINDING
                or facts['contract']['path'] != V2_10_3_CONTRACT
                or facts['exemptions'] != []):
            return ['Story 10.3 candidate, scenarios, inventory, or effective bindings differ']
        receipts = facts['receipts']
        if len(receipts) != 7 or any(by_id[identity]['resultFile'] != receipt
               or by_id[identity]['outputFiles'] != [receipt]
               for identity, receipt in zip(expected, receipts)):
            return ['Story 10.3 receipt paths or digests differ from its scenarios']
        if any(row['beforeSha256'] != row['afterSha256'] or row['expectedBlocker'] not in row['observedBlockers']
               or row['result'] != 'PASS' or not row['restored'] for row in facts['mutationLedger']):
            return ['Story 10.3 fault observations or restoration do not pass']
        if len({row['path'] for row in facts['routeFacts']}) != 10:
            return ['Story 10.3 current route identity count differs']
        for inventory in (facts['workflowInventory'], facts['guidanceInventory']):
            if v2_sha256(json.dumps(inventory['rows'], sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()) != inventory['sha256']:
                return ['Story 10.3 inventory row digest differs']
    except (KeyError, TypeError, ValueError):
        return ['Story 10.3 execution bindings are malformed']
    return []


def v2_10_3_route_facts(repository: Path, candidate: str, paths: Sequence[str]) -> list[dict[str, Any]]:
    """Measure byte offsets and digests of the exact committed gate spans."""
    current = v2_10_3_current_module()
    facts = []
    for path in paths:
        content = v2_committed_blob(repository,candidate,path)
        if content is None:
            raise ValueError('a governed route is unavailable')
        start = content.index(current.BEGIN.encode())
        end = content.index(current.END.encode()) + len(current.END.encode())
        facts.append(dict(path=path,sha256=v2_sha256(content),gateStart=start,gateEnd=end))
    return facts


def v2_10_3_source_errors(repository: Path, record: dict[str, Any], validator: Any, consumer_candidate: str | None = None) -> list[str]:
    """Recompute committed sources, provenance, predecessor pair and every receipt."""
    problems = v2_10_3_pair_errors(record)
    if problems:
        return problems
    facts = record['evidenceWorkflowExecution']
    candidate = record['candidate']['commit']
    try:
        bound_files = [facts['binding'], facts['contract'], *facts['provenance'], *facts['sourceFiles']]
        for row in bound_files:
            blob = v2_committed_blob(repository, candidate, row['path'])
            if blob is None or v2_sha256(blob) != row['sha256']:
                problems.append('Story 10.3 committed source digest differs: ' + row['path'])
        binding = v2_parse_json(v2_committed_blob(repository, candidate, V2_10_3_BINDING))
        if facts['provenance'] != binding['provenance']:
            problems.append('Story 10.3 original provenance differs from the approved binding')
        if facts['routeFacts'] != v2_10_3_route_facts(repository,candidate,[row['path'] for row in binding['workflowInventory']['rows']]):
            problems.append('Story 10.3 route insertion facts differ from committed byte spans')
        if [row['path'] for row in facts['sourceFiles']] != binding['sourcePaths']:
            problems.append('Story 10.3 exact source-file inventory differs')
        current = v2_10_3_current_module()
        if not current.source_binding_valid(repository, binding):
            problems.append('Story 10.3 source inventory or post-entry scope differs from the approved entry')
        provenance = current.historical_provenance_result(repository, binding['provenance'])
        if provenance['result'] != 'PASS':
            problems.append('Story 10.3 provenance differs from approved baseline blobs: ' + ', '.join(provenance['blockers']))
        raw = current.bridge(repository,dict(baseline=binding['baseline'],candidate=candidate,
            expectedPaths=binding['expectedChangedPaths']))
        if raw['result'] != 'PASS':
            problems.append('Story 10.3 full raw source boundary does not pass: '+', '.join(raw['blockers']))
        elif not current.withdrawal_valid(binding, raw['diff']):
            problems.append('Story 10.3 prior publication withdrawal is not nine exact deletions')
        if record['candidate']['gitlinks'] != [dict(path=path,mode='160000',commit=commit) for path,commit in v2_raw_gitlinks(repository,candidate)]:
            problems.append('Story 10.3 raw candidate gitlinks differ')
        if (binding['workflowInventory'] != facts['workflowInventory']
                or binding['guidanceInventory'] != facts['guidanceInventory']
                or binding['resolvedCustomization'] != facts['resolvedCustomization']
                or binding['retiredRoutes'] != facts['retiredRoutes']
                or binding['baseline'] != facts['baseline']):
            problems.append('Story 10.3 current binding facts differ')
        previous = facts['predecessorPair']['json']
        if v2_verified_predecessor(repository, candidate, '10.2', previous['path'],
                facts['predecessorPair']['markdown']['path'], validator) != previous['sha256']:
            problems.append('Story 10.3 verified predecessor pair differs')
        contract = v2_parse_json(v2_committed_blob(repository, candidate, V2_10_3_CONTRACT))
        if [row['command'] for row in record['scenarios']] != [row['command'] for row in contract['scenarios']]:
            problems.append('Story 10.3 effective scenario commands differ')
        consumer_candidate = consumer_candidate or resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')
        def committed_receipt(path: str) -> bytes:
            content = v2_committed_blob(repository, consumer_candidate, path)
            if content is None:
                raise ValueError('a committed receipt is unavailable')
            return content
        scenarios, receipts, mutations = v2_10_3_receipt_facts(repository, contract, candidate, read_receipt=committed_receipt)
        if scenarios != record['scenarios'] or receipts != facts['receipts'] or mutations != facts['mutationLedger']:
            problems.append('Story 10.3 scenario or mutation ledgers differ from committed receipt derivation')
        for receipt in facts['receipts']:
            committed = v2_committed_blob(repository, consumer_candidate, receipt['path'])
            if committed is None or v2_sha256(committed) != receipt['sha256']:
                problems.append('Story 10.3 retained receipt digest differs: ' + receipt['path'])
    except V2Stop as error:
        problems.append('Story 10.3 committed receipt derivation failed: ' + '; '.join(item['message'] for item in error.findings))
    except (KeyError, TypeError, ValueError, OSError, UnicodeError):
        problems.append('Story 10.3 committed consumption bindings are unavailable')
    return problems


def v2_10_3_receipt_facts(repository: Path, contract: dict[str, Any], candidate: str,
                         observed: dict[str, Any] | None = None, read_receipt: Any = None) -> tuple[list, list, list]:
    """Derive ledgers and required faults from live or committed receipts using shared parsers."""
    current = v2_10_3_current_module()
    scenarios, receipts, mutations = [], [], []
    candidate_time_ns = int(decode(run_git(repository, 'show', '-s', '--format=%ct', candidate).stdout).strip()) * 1_000_000_000
    for scenario in contract['scenarios'][:-1]:
        identity = scenario['id']
        if identity in ('AC-10.3-01','AC-10.3-03'):
            tokens = shlex.split(scenario['command'])
            receipt_path = tokens[tokens.index('--output')+1]
            content = read_receipt(receipt_path) if read_receipt else v2_10_1_artifact_bytes(repository, receipt_path)
            measured = v2_parse_json(content)
            try:
                import jsonschema
                jsonschema.validate(measured, json.loads((repository / current.SCHEMA).read_bytes()))
            except (ValueError, jsonschema.ValidationError):
                raise V2Stop([v2_finding('SCENARIO_RESULT_MISMATCH', identity, 'the verifier receipt violates its closed schema')], '10.3') from None
            if (observed is not None and measured != observed) or measured['candidate'] != candidate or measured['result'] != 'PASS':
                raise V2Stop([v2_finding('SCENARIO_RESULT_MISMATCH', identity, 'the verifier receipt differs from candidate re-evaluation')], '10.3')
            receipt = dict(path=receipt_path, sha256=v2_sha256(content))
            row = dict(scenarioId=identity, command=scenario['command'], exitCode=0, result='PASS', blockers=[],
                assertionLedger=[dict(id=f'{identity}#{n+1:04d}',subject=entry['id']+'::'+entry['subject'],state=entry['state'])
                    for n,entry in enumerate(measured['assertionLedger'])], resultFile=receipt, outputFiles=[receipt])
        else:
            command = v2_pytest_command(shlex.split(scenario['command']))
            if command is None:
                raise V2Stop([v2_finding('SCENARIO_COMMAND_UNSUPPORTED', identity, 'the effective pytest selector is unsupported')], '10.3')
            content = read_receipt(str(command['junit'])) if read_receipt else v2_10_1_artifact_bytes(repository, str(command['junit']))
            row, category, findings = v2_scenario_from_results(repository, scenario, command, candidate_time_ns,
                snapshot=(content, candidate_time_ns) if read_receipt else None)
            if findings or category != 'passed':
                raise V2Stop(findings or [v2_finding('TEST_RESULTS_FAILED', identity, 'the required selector did not pass')], '10.3')
            receipt = row['resultFile']
            row['outputFiles'] = [receipt]
            cases = v2_parse_junit(content)['cases']
            for case in cases:
                properties = dict(case['properties'])
                if len(properties) != len(case['properties']):
                    raise V2Stop([v2_finding('TEST_RESULTS_STALE', identity, 'testcase properties must be unique')], '10.3')
                if properties.get('story103_source_revision') != candidate:
                    raise V2Stop([v2_finding('TEST_RESULTS_STALE', identity, 'a testcase does not bind this committed candidate')], '10.3')
                if 'story103_expected_code' in properties:
                    code = properties['story103_expected_code']
                    blockers = json.loads(properties['story103_observed_blockers'])
                    before, after = properties['story103_before_sha256'], properties['story103_after_sha256']
                    if code not in blockers or before != after:
                        raise V2Stop([v2_finding('RECORD_NOT_DERIVED', identity, 'fault observation or restoration failed')], '10.3')
                    mutations.append(dict(id=case['name'], expectedBlocker=code, observedBlockers=blockers,
                        beforeSha256=before, afterSha256=after, restored=True, result='PASS'))
        scenarios.append(row); receipts.append(receipt)
    required_faults = {'EVIDENCE_BOUNDARY_GITLINK','EVIDENCE_HASH_MISMATCH','EVIDENCE_PATH_ESCAPE','EVIDENCE_FILE_MISSING','EVIDENCE_OUTPUT_FORBIDDEN',
        'SIGNABLE_PAYLOAD_MISMATCH','CHANGED_PATH_SET_MISMATCH','ROOT_OF_TRUST_NOT_PINNED','SIGNED_EVIDENCE_ALLOWLISTED',
        'REVISION_INVALID','EVIDENCE_READER_NOT_SHARED','EVIDENCE_READER_STRENGTH_REDUCED','EVIDENCE_GATE_NOT_USED','EVIDENCE_GATE_DISPLACED','EVIDENCE_ALIAS_ROUTE_BROKEN','EVIDENCE_GUIDANCE_NOT_BOUND'}
    if not required_faults.issubset({row['expectedBlocker'] for row in mutations}):
        raise V2Stop([v2_finding('RECORD_NOT_DERIVED', 'faults', 'required observed faults are missing')], '10.3')
    scenarios.append(dict(scenarioId='AC-10.3-08', command=contract['scenarios'][-1]['command'], exitCode=0, result='PASS', blockers=[],
        assertionLedger=[dict(id=f'AC-10.3-08#{n+1:04d}', subject=subject, state='PASS') for n,subject in enumerate((
            'effective-contract-and-original-provenance', 'shared-verifier-and-current-route-checks', 'verified-10.2-predecessor-pair',
            'seven-candidate-bound-receipts', 'closed-schema-and-deterministic-pair'))]))
    return scenarios, receipts, mutations


def v2_10_3_generate(options: dict[str, str], *, candidate: str | None = None,
                     verify_spec: str | None = None, consumer_candidate: str | None = None) -> bytes:
    """Generate from the approved current binding, shared verifier and measured receipts."""
    current = v2_10_3_current_module()
    repository = validate_repository(Path(options.get('--repository') or default_repository()))
    _, validators = v2_load_schemas()
    candidate = candidate or resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')
    contract_bytes = v2_committed_blob(repository, candidate, V2_10_3_CONTRACT)
    contract = v2_validate_contract(contract_bytes, V2_10_3_CONTRACT, validators['contract'], validators['contract_v14'])
    if contract['storyId'] != '10.3' or [row['id'] for row in contract['scenarios']] != [f'AC-10.3-0{n}' for n in range(1,9)]:
        raise V2Stop([v2_finding('INPUT_SCHEMA_INVALID', '10.3', 'all eight ordered AC identities are required')], '10.3')
    paths = contract['finalRecord']['paths']
    if [options['--output-json'], options['--output-markdown']] != paths:
        raise V2Stop([v2_finding('OUTPUT_PATH_INVALID', '10.3', 'output pair differs from the effective contract')], '10.3')
    binding_bytes = v2_committed_blob(repository, candidate, V2_10_3_BINDING)
    if binding_bytes is None or (repository / V2_10_3_BINDING).read_bytes() != binding_bytes:
        raise V2Stop([v2_finding('AUTHORITY_BINDING_INVALID', V2_10_3_BINDING, 'the current binding must equal its committed source')], '10.3')
    binding = v2_parse_json(binding_bytes)
    if not current.source_binding_valid(repository, binding):
        raise V2Stop([v2_finding('AUTHORITY_BINDING_INVALID', V2_10_3_BINDING,
            'the source inventory and post-entry scope must match the approved entry')], '10.3')
    previous = v2_verified_predecessor(repository, candidate, '10.2', binding['predecessor']['path'],
        'docs/release-evidence/story-10.2-final-record-v2.md', validators['record'])
    if previous != binding['predecessor']['sha256']:
        raise V2Stop([v2_finding('AUTHORITY_BINDING_INVALID', '10.2', 'the committed predecessor pair does not verify')], '10.3')
    observed = current.verify(repository, binding['baseline'], candidate,
        authority_bundle=binding['authorityBundle']['path'], verify_workflows=True)
    if observed['result'] != 'PASS':
        raise V2Stop([v2_finding('RECORD_NOT_DERIVED', 'scoped-verifier',
            'the current verifier did not pass: ' + ', '.join(observed['blockers']))], '10.3')
    # Compare every reviewed source to its candidate blob; lifecycle insertion is
    # checked separately. Unrelated staged user changes are outside this scope.
    source_files = []
    consumer_head = (consumer_candidate or resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')) if verify_spec is not None else None
    for path in binding['sourcePaths']:
        blob = v2_committed_blob(repository, candidate, path)
        if blob is None:
            raise V2Stop([v2_finding('RECORD_NOT_DERIVED', path, 'a scoped source is not committed')], '10.3')
        source_files.append(dict(path=path, sha256=v2_sha256(blob)))
        installed = repository / path
        valid_spec = path == V2_10_3_SPEC and installed.is_file() and not installed.is_symlink() and v2_working_spec_lifecycle_only_change(
            repository, candidate, path, installed.read_bytes(), record_region=True, task_checkboxes=True)
        valid_sprint = (consumer_head is not None and path == '_bmad-output/implementation-artifacts/sprint-status.yaml'
            and installed.is_file() and not installed.is_symlink()
            and installed.read_bytes() == v2_committed_blob(repository, consumer_head, path)
            and v2_sprint_status_only_change(repository, candidate, consumer_head,
                '10-3-provide-the-evidence-boundary-verifier-and-integrate-every-workflow-surface'))
        if (installed.is_symlink() or not installed.is_file()
                or (path == V2_10_3_SPEC and not valid_spec)
                or (path != V2_10_3_SPEC and installed.read_bytes() != blob and not valid_sprint)):
            raise V2Stop([v2_finding('WORKTREE_NOT_CLEAN', path, 'a scoped source differs from the candidate')], '10.3')
    scenarios, receipts, mutations = v2_10_3_receipt_facts(repository, contract, candidate, observed)
    route_facts = v2_10_3_route_facts(repository,candidate,current.WORKFLOWS)
    bundle = v2_parse_json(v2_committed_blob(repository, candidate, binding['authorityBundle']['path']))
    bundle_digest = v2_sha256(''.join(f'{row["sha256"]}  {row["path"]}\n' for row in bundle['artifacts']).encode())
    if bundle_digest != bundle['bundleDigest']:
        raise V2Stop([v2_finding('AUTHORITY_BINDING_INVALID', 'provenance', 'the original bundle row digest differs')], '10.3')
    facts = dict(bindingRule='SC-10.3 is the committed candidate evaluated under the approved current binding',
        sourceRevisionId=candidate, baseline=binding['baseline'], binding=dict(path=V2_10_3_BINDING,sha256=v2_sha256(binding_bytes)),
        contract=dict(path=V2_10_3_CONTRACT,sha256=v2_sha256(contract_bytes)),
        predecessorPair=dict(json=binding['predecessor'] | {}, markdown=dict(path='docs/release-evidence/story-10.2-final-record-v2.md',
            sha256=v2_sha256(v2_committed_blob(repository,candidate,'docs/release-evidence/story-10.2-final-record-v2.md')))),
        provenance=binding['provenance'], workflowInventory=binding['workflowInventory'], guidanceInventory=binding['guidanceInventory'],
        resolvedCustomization=binding['resolvedCustomization'], retiredRoutes=binding['retiredRoutes'], exemptions=[],
        routeFacts=route_facts, receipts=receipts, mutationLedger=mutations, sourceFiles=source_files)
    facts['predecessorPair']['json'] = {key:facts['predecessorPair']['json'][key] for key in ('path','sha256')}
    record = dict(schemaVersion=V2_RECORD_SCHEMA_VERSION, storyId='10.3',
        authority=dict(epic=contract['authority']['epic'],architecture=contract['authority']['architecture'],
            planningCandidate=contract['authority']['planningCandidate'],bundleDigest=bundle_digest),
        candidate=dict(commit=candidate,gitlinks=[dict(path=path,mode='160000',commit=commit) for path,commit in v2_raw_gitlinks(repository,candidate)]),
        predecessors=['10.2'], inventory=contract['inventory'], scenarios=scenarios, faultInjection=dict(results=[]),
        outputs=dict(json=dict(path=paths[0],sha256=V2_ZERO_DIGEST),markdown=dict(path=paths[1],sha256=V2_ZERO_DIGEST)),
        rollback=contract['rollback'],summary=dict(required=8,passed=8,failed=0,blocked=0,skipped=0,notRun=0),
        renderedMarkdownSha256=V2_ZERO_DIGEST,evidenceWorkflowExecution=facts)
    final, json_bytes, markdown_bytes = v2_finalize(record)
    problems = v2_schema_errors(validators['record'], final) + v2_verify_pair(json_bytes,markdown_bytes)
    if problems:
        raise V2Stop([v2_finding('OUTPUT_SCHEMA_INVALID','10.3','; '.join(problems))], '10.3')
    if verify_spec is not None:
        return json_bytes
    v2_write_outputs([(v2_output_target(repository,paths[0]),json_bytes),(v2_output_target(repository,paths[1]),markdown_bytes)])
    return json_bytes


def v2_10_3_descendant_errors(repository: Path, record: dict[str, Any], head: str) -> list[str]:
    """Allow one exact pair/receipt publication followed only by checked lifecycle edits."""
    current = v2_10_3_current_module()
    candidate = record['candidate']['commit']
    ancestry = run_git(repository, 'merge-base', '--is-ancestor', candidate, head, allowed_returncodes=(0,1))
    if ancestry.returncode:
        return ['the evaluated source candidate is not an ancestor of the gate candidate']
    json_path, markdown_path = (record['outputs'][kind]['path'] for kind in ('json','markdown'))
    receipts = record['evidenceWorkflowExecution']['receipts']
    receipt_paths = {row['path'] for row in receipts}
    publication_paths = {json_path,markdown_path} | receipt_paths
    commits = decode(run_git(repository,'rev-list','--reverse',f'{candidate}..{head}').stdout).split()
    parent, publications = candidate, 0
    for commit in commits:
        parents = decode(run_git(repository,'rev-list','--parents','-n','1',commit).stdout).split()[1:]
        if parents != [parent]:
            return ['a post-source commit is not the direct single-parent continuation']
        facts = current.bridge(repository,dict(baseline=parent,candidate=commit))
        if facts['result'] != 'PASS':
            return ['post-source history or raw gitlinks do not pass: '+', '.join(facts['blockers'])]
        changed = {entry['path'] for entry in facts['diff']} | {entry['sourcePath'] for entry in facts['diff'] if entry['sourcePath']}
        if changed == publication_paths and publications == 0:
            for row in receipts:
                blob = v2_committed_blob(repository,commit,row['path'])
                if blob is None or v2_sha256(blob) != row['sha256']:
                    return ['a published receipt differs from its bound digest']
            json_bytes, markdown_bytes = (v2_committed_blob(repository,commit,path) for path in (json_path,markdown_path))
            if json_bytes is None or markdown_bytes is None or v2_verify_pair(json_bytes,markdown_bytes):
                return ['the published final-record pair does not verify']
            if v2_parse_json(json_bytes) != record:
                return ['the published final record differs from the gate record']
            publications += 1
        elif publications == 1 and changed and changed.issubset({V2_10_3_SPEC,'_bmad-output/implementation-artifacts/sprint-status.yaml'}):
            if V2_10_3_SPEC in changed and not v2_spec_lifecycle_only_change(repository,parent,commit,V2_10_3_SPEC,
                    record_region=True,task_checkboxes=True):
                return ['post-source spec changes exceed lifecycle ownership']
            if '_bmad-output/implementation-artifacts/sprint-status.yaml' in changed and not v2_sprint_status_only_change(
                    repository,parent,commit,'10-3-provide-the-evidence-boundary-verifier-and-integrate-every-workflow-surface'):
                return ['post-source tracking changes exceed lifecycle ownership']
        else:
            return ['post-source source drift or output publication drift is not allowed']
        parent = commit
    return [] if publications == 1 else ['the candidate does not include exactly one bound pair/receipt publication']


def v2_10_3_verify_inserted(options: dict[str,str]) -> bytes:
    """Retain the evaluated candidate across pair publication and lifecycle insertion."""
    repository = validate_repository(Path(options.get('--repository') or default_repository()))
    head = resolve_commit(repository,'HEAD','CANDIDATE_UNRESOLVABLE')
    _, validators = v2_load_schemas()
    json_path, markdown_path = ('docs/release-evidence/story-10.3-final-record-v2.json','docs/release-evidence/story-10.3-final-record-v2.md')
    json_bytes = v2_committed_blob(repository,head,json_path)
    markdown_bytes = v2_committed_blob(repository,head,markdown_path)
    if json_bytes is None or markdown_bytes is None:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT','outputs','the Story 10.3 pair must be committed first')], '10.3')
    record = v2_parse_json(json_bytes)
    problems = v2_schema_errors(validators['record'],record) + v2_verify_pair(json_bytes,markdown_bytes)
    problems += v2_10_3_source_errors(repository,record,validators['record'],head)
    if problems:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT','10.3','; '.join(problems))], '10.3')
    candidate = record['candidate']['commit']
    descendants = v2_10_3_descendant_errors(repository,record,head)
    if descendants:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT','candidate','; '.join(descendants))], '10.3')
    spec_path = safe_relative_path(options[V2_VERIFY_OPTION])
    if spec_path != V2_10_3_SPEC:
        raise V2Stop([v2_finding('ARGUMENT_INVALID','spec','the designated Story 10.3 spec is required')], '10.3')
    spec = (repository/spec_path).read_bytes()
    if not v2_working_spec_lifecycle_only_change(repository, head, spec_path, spec,
            record_region=True, task_checkboxes=True):
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT', 'spec',
            'the working spec changes content outside lifecycle-owned fields, task checkboxes, or the final-record region')], '10.3')
    region = v2_record_region(spec)
    if region is None or spec[region[0]:region[1]] != markdown_bytes:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT','spec','the inserted record differs from its committed Markdown')], '10.3')
    for path,content in ((json_path,json_bytes),(markdown_path,markdown_bytes)):
        if (repository/path).read_bytes() != content:
            raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT',path,'the installed pair differs from committed bytes')], '10.3')
    derived = v2_10_3_generate({'--repository':str(repository),'--contract':V2_10_3_CONTRACT,
        '--format':'bundle','--output-json':json_path,'--output-markdown':markdown_path},candidate=candidate,verify_spec=spec_path,consumer_candidate=head)
    if derived != json_bytes:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT','10.3','candidate rederivation differs from the pair')], '10.3')
    return json_bytes


# Story 10.4 uses the approved current policy without amending historical contracts.
V2_10_4_CONTRACT = '_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json'
V2_10_4_BINDING = '_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json'
V2_10_4_SPEC = '_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md'
V2_10_4_SPRINT_KEY = '10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection'


def v2_10_4_working_source_matches(repository, candidate, path, working, consumer_candidate=None):
    """Allow exact sources or checked lifecycle bytes committed by the record consumer."""
    original = v2_committed_blob(repository, candidate, path)
    if original is not None and working == original:
        return True
    if path == V2_10_4_SPEC:
        return v2_working_spec_lifecycle_only_change(repository, candidate, path, working,
            record_region=True, task_checkboxes=True)
    return (path == V2_7_2_SPRINT_PATH
        and v2_working_sprint_status_only_change(repository, candidate, V2_10_4_SPRINT_KEY, working)
        and (consumer_candidate is None or v2_working_sprint_status_only_change(
            repository, consumer_candidate, V2_10_4_SPRINT_KEY, working)))


def v2_10_4_module():
    """Load the scoped sibling route for command-line and path-import consumers."""
    directory = str(Path(__file__).resolve().parent)
    if directory not in sys.path: sys.path.insert(0, directory)
    import evidence_boundary_adoption
    return evidence_boundary_adoption


def v2_10_4_pair_errors(record):
    """Reject a pair whose nine identities, receipts, ledgers or immutable facts disagree."""
    try:
        facts=record['evidenceAdoptionExecution'];scenarios=record['scenarios']
        expected=[f'AC-10.4-0{n}' for n in range(1,10)]
        if ([row['scenarioId'] for row in scenarios]!=expected or record['predecessors']!=['10.3']
            or facts['sourceRevisionId']!=record['candidate']['commit']
            or record['summary']!=dict(required=9,passed=9,failed=0,blocked=0,skipped=0,notRun=0)
            or any(row['result']!='PASS' or row['exitCode']!=0 or row['blockers'] or not row['assertionLedger'] for row in scenarios)
            or facts['binding']['path']!=V2_10_4_BINDING or facts['contract']['path']!=V2_10_4_CONTRACT
            or facts['exemptions']!=[] or len(facts['readerBeforeAfter'])!=27):
            return ['Story 10.4 candidate, summary, scenarios or closed bindings differ']
        measured=[row for row in scenarios if row['scenarioId']!='AC-10.4-08']
        if len(facts['receipts'])!=8 or any(row['resultFile']!=receipt or row['outputFiles']!=[receipt]
                for row,receipt in zip(measured,facts['receipts'])):
            return ['Story 10.4 eight measured receipts differ']
        expected_faults=v2_10_4_module().FAULT_EXPECTATIONS
        if ({row['id'] for row in facts['mutationLedger']}!=set(expected_faults)
            or len(facts['mutationLedger'])!=len(expected_faults)
            or any(row['expectedBlocker']!=expected_faults[row['id']]['code']
                or row['observedBlockers']!=expected_faults[row['id']]['blockers']
                or row['beforeSha256']!=row['afterSha256'] or not row['restored'] or row['result']!='PASS'
                for row in facts['mutationLedger'])):
            return ['Story 10.4 measured faults or restoration differ']
        if [row['path'] for row in facts['readerBeforeAfter']]!=facts['readerInventory']['paths']:
            return ['Story 10.4 reader identities differ']
        adoption=v2_10_4_module()
        if (facts['approvedGitlinkAmendment']!=adoption.APPROVED_GITLINK_AMENDMENT
                or adoption.candidate_gitlinks(facts['rootGitlinks'])!=record['candidate']['gitlinks']):
            return ['Story 10.4 approved gitlink amendment or candidate roots differ']
    except (KeyError,TypeError,ValueError):return ['Story 10.4 execution facts are malformed']
    return []


# These independent identities/codes describe the executed contract, not receipt declarations.
V2_10_4_FAULT_GROUPS = {
    'integrity': {'HASH':'EVIDENCE_HASH_MISMATCH','PATH_ESCAPE':'EVIDENCE_PATH_ESCAPE',
        'GENERATED_OUTPUT':'EVIDENCE_OUTPUT_FORBIDDEN','SUBSET':'CHANGED_PATH_SET_MISMATCH',
        'SIGNED_ALLOWLIST':'SIGNED_EVIDENCE_ALLOWLISTED','SIGNABLE':'SIGNABLE_PAYLOAD_MISMATCH',
        'ROOT':'ROOT_OF_TRUST_NOT_PINNED','GIT_UNAVAILABLE':'GIT_UNAVAILABLE',
        'HISTORY_UNAVAILABLE':'REVISION_INVALID','GITLINK':'EVIDENCE_BOUNDARY_GITLINK'},
    'chain': {**dict.fromkeys(('CHAIN_CHANGED','CHAIN_MISSING','CHAIN_REORDERED','CHAIN_DUPLICATE_ROW'),'EVIDENCE_CHAIN_TABLE_INVALID'),
        **dict.fromkeys(('CHAIN_NESTED','CHAIN_DUPLICATE','CHAIN_UNTERMINATED'),'EVIDENCE_CHAIN_MARKER_INVALID')},
    'workflow': {'WORKFLOW_REMOVED':'EVIDENCE_GATE_NOT_USED','MARKER_MALFORMED':'EVIDENCE_GATE_NOT_USED','SPAN_DISPLACED':'EVIDENCE_GATE_DISPLACED'},
    'guidance': {'GUIDANCE_REMOVED':'EVIDENCE_GUIDANCE_NOT_USED',
        **dict.fromkeys(('GUIDANCE_EQUALITY','GUIDANCE_DECLARED_HASH','GUIDANCE_REDIRECT'),'EVIDENCE_GUIDANCE_DRIFT')},
    'readers': {'READER_STRENGTH':'ASSERTION_STRENGTH_WEAKENED','READER_HELPER':'EVIDENCE_HELPER_NOT_USED',
        'EXEMPTION':'DAY_ONE_EXEMPTION_FORBIDDEN','READER_INVENTORY':'EVIDENCE_READER_INVENTORY_DRIFT'},
}
V2_10_4_FAULT_CODES = {fault:code for group in V2_10_4_FAULT_GROUPS.values() for fault,code in group.items()}
V2_10_4_FAULT_CODES['PROMOTION_SPAN_DISPLACED']='PROMOTION_GATE_SPAN_WEAKENED'
V2_10_4_PYTEST_CASES = {
    'AC-10.4-06': {('test_complete_fault_matrix_'+group+'['+fault+'-'+code+']'):fault
        for group,rows in V2_10_4_FAULT_GROUPS.items() for fault,code in rows.items()},
    'AC-10.4-07': {'test_evidence_gate_span_coupling[exact-span]':None,
        'test_evidence_gate_span_coupling[displaced-span]':'PROMOTION_SPAN_DISPLACED'},
}
V2_10_4_PYTEST_TARGETS = {'AC-10.4-06':'_bmad/scripts/tests/test_verify_evidence_boundary.py',
    'AC-10.4-07':'_bmad/scripts/tests/test_verify_submodule_promotion.py'}
# Only these accepted bytes predate observed result/exit properties. New or
# altered receipts must satisfy the current contract, even at this candidate.
V2_10_4_LEGACY_FAULT_RECEIPT = (
    '7e5a4b765a4437f361fac7e7bb4f13e5b7cf3ec8',
    'artifacts/v9/10.4/AC-10.4-06.xml',
    '94faa9000e8fdcbfc4a803d53777d061ff827837874bbda14557c388d1d3fb4a',
)
# AC-09 composed two guidance readers before their IDs had separate namespaces.
# Preserve its exact accepted envelope; current envelopes must use unique IDs.
V2_10_4_LEGACY_SCOPE_ENVELOPE = (
    '7e5a4b765a4437f361fac7e7bb4f13e5b7cf3ec8',
    'f6e7937ec00fef6eaebb355a68877dd264f5dfae7ca39acac5f9711fe66d395d',
)


def v2_10_4_assertion_rows(rows, *, passing=False):
    """Reject unevaluated, malformed or duplicate assertion identities."""
    if (not isinstance(rows,list) or not rows or any(not isinstance(row,dict)
            or set(row)!={'id','subject','state'}
            or any(not isinstance(row[key],str) or not row[key].strip() for key in ('id','subject'))
            or row['state'] not in ('PASS','FAIL','SKIPPED') for row in rows)):
        raise ValueError('assertion ledger violates its closed schema')
    identities=[row['id'] for row in rows]
    if len(set(identities))!=len(identities) or not any(row['state'] in ('PASS','FAIL') for row in rows):
        raise ValueError('assertion identities must be unique and evaluated (closed schema)')
    if passing and any(row['state']!='PASS' for row in rows):raise ValueError('passing receipts require passing assertions')
    return rows


def v2_10_4_helper_result(repository,observed,candidate):
    import jsonschema
    adoption=v2_10_4_module()
    try:jsonschema.validate(observed,json.loads((repository/adoption.current.SCHEMA).read_bytes()))
    except jsonschema.ValidationError as error:raise ValueError('invalid helper envelope: '+error.message) from None
    if observed['candidate']!=candidate or observed['result']!='PASS' or observed['blockers'] or observed['warnings']:
        raise ValueError('a passing candidate-bound helper result is required')
    if (candidate,v2_sha256(adoption.current.canonical(observed)))==V2_10_4_LEGACY_SCOPE_ENVELOPE:
        return observed['assertionLedger']
    return v2_10_4_assertion_rows(observed['assertionLedger'],passing=True)


def v2_10_4_scope_receipt(repository,observed,candidate):
    """Recompute source boundary and guidance facts without publication or output ownership."""
    adoption=v2_10_4_module();current=adoption.current
    v2_10_4_helper_result(repository,observed,candidate)
    binding=v2_parse_json(v2_committed_blob(repository,candidate,V2_10_4_BINDING))
    manifest=binding['provenance']+binding['workflowInventory']['rows']+binding['guidanceInventory']['rows']
    manifest += [dict(path=adoption.SNAPSHOT,sha256=adoption.SNAPSHOT_SHA256,role='entry-snapshot')]
    payload=''.join(f'{row["path"]}\t{row["sha256"]}\t{row.get("role","provenance")}\n' for row in sorted(manifest,key=lambda row:row['path']))
    raw=current.bridge(repository,dict(baseline=adoption.BASELINE,candidate=candidate,
        expectedPaths=binding['expectedChangedPaths'],roots=adoption.ROOTS,manifest=manifest,
        signablePayloadHash=current.digest(payload.encode()),story104GitlinkAmendment=adoption.APPROVED_GITLINK_AMENDMENT,
        blobPaths=[row['path'] for row in manifest]))
    if raw['result']!='PASS' or raw['blockers']:raise ValueError('candidate boundary facts do not pass')
    changed=sorted({row['path'] for row in raw['diff']}|{row['sourcePath'] for row in raw['diff'] if row['sourcePath']})
    blobs={row['path']:row for row in raw['blobs']}
    if (changed!=binding['expectedChangedPaths'] or observed['changedPaths']!=changed or observed['manifest']!=manifest
            or any(row['path'] not in blobs or blobs[row['path']]['code'] or blobs[row['path']]['sha256']!=row['sha256'] for row in manifest)):
        raise ValueError('AC-09 boundary or candidate manifest facts differ')
    expected=current.empty_result(candidate)
    adoption.guidance_checks(repository,expected,binding)
    current.workflow_checks(repository,expected,binding)
    if expected['blockers']:raise ValueError('candidate workflow or guidance facts do not pass')
    required=[dict(row,id='boundary:'+row['id']) for row in raw['assertionLedger']]+expected['assertionLedger']
    if (candidate,v2_sha256(current.canonical(observed)))==V2_10_4_LEGACY_SCOPE_ENVELOPE:
        required=[dict(row,id=row['id'].replace('guidance:adoption-resolved:','guidance:resolved:',1)) for row in required]
    required += [dict(id='candidate:blob:'+path,subject=path,state='PASS') for path in binding['sourcePaths']]
    actual={(row['id'],row['subject'],row['state']) for row in observed['assertionLedger']}
    if any((row['id'],row['subject'],row['state']) not in actual for row in required):
        raise ValueError('AC-09 measured boundary/workflow/guidance assertions differ')


def v2_10_4_receipt_facts(repository,contract,candidate,read_receipt=None):
    """Read eight measured receipts, then derive AC-08 from their verified facts."""
    adoption = v2_10_4_module()
    read=read_receipt or (lambda path:v2_10_1_artifact_bytes(repository,path))
    scenarios=[];receipts=[];mutations=[]
    for scenario in contract['scenarios']:
        identity=scenario['id']
        if identity=='AC-10.4-08':continue
        tokens=shlex.split(scenario['command']);ledger=[]
        if identity=='AC-10.4-09':
            path=tokens[tokens.index('--output')+1];content=read(path);observed=v2_parse_json(content)
            v2_10_4_scope_receipt(repository,observed,candidate)
            ledger=[dict(id=f'{identity}#{n+1:04d}',subject=row['id']+'::'+row['subject'],state=row['state'])
                for n,row in enumerate(observed['assertionLedger'])]
        elif '-trx' in tokens:
            path=tokens[tokens.index('-trx')+1];content=read(path)
            try:parsed=parse_trx(content)
            except ElementTree.ParseError as error:raise ValueError(identity+' malformed TRX XML') from error
            method=tokens[tokens.index('-method')+1]
            if count_disagreements(parsed) or parsed['reported']!=dict(total=1,executed=1,passed=1,failed=0,skipped=0):
                raise ValueError(identity+' must measure exactly its named passing xUnit test')
            testcase=parsed['results'][0]
            root=ElementTree.fromstring(content)
            definitions=root.findall('./{*}TestDefinitions/{*}UnitTest')
            result=root.find('./{*}Results/{*}UnitTestResult')
            if len(definitions)!=1:raise ValueError(identity+' requires exactly one native test definition')
            definition=definitions[0];methods=definition.findall('./{*}TestMethod');executions=definition.findall('./{*}Execution')
            assembly=PurePosixPath(tokens[1]).name
            if (not result.get('testId') or result.get('testId')!=definition.get('id')
                or not result.get('executionId') or len(executions)!=1 or result.get('executionId')!=executions[0].get('id')
                or definition.get('name')!=method or len(methods)!=1
                or methods[0].get('className')!=method.rsplit('.',1)[0] or methods[0].get('name')!=method.rsplit('.',1)[1]
                or re.split(r'[\\/]',methods[0].get('codeBase',''))[-1]!=assembly
                or re.split(r'[\\/]',definition.get('storage',''))[-1]!=assembly):
                raise ValueError(identity+' native assembly or result/definition linkage differs')
            stamps=re.findall(r'(?m)^story104_source_revision=([^\r\n]+)$',testcase['output'])
            revisions=[match.group(1) if (match:=re.fullmatch(r'(?:(?:[0-9]+\.){2}[0-9]+(?:\.[0-9]+)?(?:-[0-9A-Za-z.-]+)?\+)?([0-9a-f]{40})',stamp)) else None for stamp in stamps]
            if testcase['test']!=method or revisions!=[candidate]:
                raise ValueError(identity+' receipt does not bind its exact test and source assembly revision')
            ledger=[dict(id=identity+'#0001',subject=testcase['test'],state='PASS')]
            # Check() writes the actual shared-helper result into the test receipt.
            lines=testcase['output'].splitlines()
            envelopes=[]
            for line in lines:
                if '{' not in line:continue
                try:envelope=v2_parse_json(line[line.index('{'):].encode())
                except ValueError:
                    if 'hexalith.conversations.evidence-boundary-result.v1' in line:
                        raise ValueError(identity+' malformed helper result envelope') from None
                    continue
                if isinstance(envelope,dict) and envelope.get('schemaVersion')=='hexalith.conversations.evidence-boundary-result.v1':
                    envelopes.append(envelope)
            if len(envelopes)!=1:raise ValueError(identity+' requires exactly one measured helper result envelope')
            observed=envelopes[0]
            v2_10_4_helper_result(repository,observed,candidate)
            ledger += [dict(id=f'{identity}#{n+2:04d}',subject=row['id']+'::'+row['subject'],state=row['state'])
                for n,row in enumerate(observed['assertionLedger'])]
        else:
            command=v2_pytest_command(tokens)
            if command is None:raise ValueError(identity+' unsupported command')
            path=str(command['junit']);content=read(path)
            try:parsed=v2_parse_junit(content)
            except ElementTree.ParseError as error:raise ValueError(identity+' malformed JUnit XML') from error
            if not parsed['cases'] or any(case['failure'] or case['error'] or case['skipped'] for case in parsed['cases']):
                raise ValueError(identity+' has failed, skipped or empty tests')
            reported=parsed['reported']
            if reported['tests']!=len(parsed['cases']) or any(reported[key] for key in ('failures','errors','skipped')):
                raise ValueError(identity+' testcase summary differs')
            expected_cases=V2_10_4_PYTEST_CASES[identity]
            if command['target']!=V2_10_4_PYTEST_TARGETS[identity]:raise ValueError(identity+' unrelated pytest target')
            require_fault_state=identity=='AC-10.4-06' and (candidate,path,v2_sha256(content))!=V2_10_4_LEGACY_FAULT_RECEIPT
            expected_class=command['target'][:-3].replace('/','.')
            names=[case['name'] for case in parsed['cases']]
            if len(names)!=len(set(names)) or set(names)!=set(expected_cases):raise ValueError(identity+' testcase identities differ')
            for n,case in enumerate(parsed['cases']):
                props=dict(case['properties']);fault=expected_cases[case['name']]
                if case['classname']!=expected_class or len(props)!=len(case['properties']) or props.get('story104_source_revision')!=candidate:
                    raise ValueError(identity+' exact testcase identities differ')
                ledger.append(dict(id=f'{identity}#{n+1:04d}',subject=case['name'],state='PASS'))
                if fault is not None:
                    code=V2_10_4_FAULT_CODES[fault]
                    if props.get('story104_fault_id')!=fault or props.get('story104_expected_code')!=code:
                        raise ValueError(identity+' independent fault/code mapping differs')
                    blockers=json.loads(props['story104_observed_blockers'])
                    before,after=props['story104_before_sha256'],props['story104_after_sha256']
                    execution=json.loads(props['story104_assertion_ledger'])
                    v2_10_4_assertion_rows(execution)
                    if require_fault_state:
                        expected=('BLOCKED','2') if fault in ('GIT_UNAVAILABLE','HISTORY_UNAVAILABLE') else ('FAIL','1')
                        if (props.get('story104_observed_result'),props.get('story104_observed_exit'))!=expected:
                            raise ValueError(identity+' fault result/exit differs: '+fault)
                    if (not isinstance(blockers,list) or any(not isinstance(item,str) for item in blockers)
                            or blockers!=adoption.FAULT_EXPECTATIONS[fault]['blockers'] or set(blockers)-(adoption.FAIL_CODES|adoption.current.BLOCKED_CODES)
                            or blockers!=sorted(set(blockers)) or before!=after or re.fullmatch(r'[0-9a-f]{64}',before) is None):
                        raise ValueError(identity+' fault observation or restoration differs')
                    if require_fault_state:
                        envelope=adoption.current.empty_result(candidate)
                        envelope.update(assertionLedger=execution,blockers=blockers)
                        if adoption.finish(envelope)['result']!=props['story104_observed_result']:
                            raise ValueError(identity+' fault ledger/result differs: '+fault)
                    mutations.append(dict(id=fault,expectedBlocker=code,observedBlockers=blockers,
                        beforeSha256=before,afterSha256=after,restored=True,result='PASS'))
                else:
                    allowed={'story104_source_revision','story104_before_sha256','story104_after_sha256','story104_assertion_ledger'}
                    if any(key.startswith('story104_') and key not in allowed for key in props):raise ValueError(identity+' positive span case carries unrelated fault facts')
                    v2_10_4_assertion_rows(json.loads(props['story104_assertion_ledger']),passing=True)
                    if props['story104_before_sha256']!=props['story104_after_sha256'] or re.fullmatch(r'[0-9a-f]{64}',props['story104_before_sha256']) is None:
                        raise ValueError(identity+' positive span restoration differs')
        receipt=dict(path=path,sha256=v2_sha256(content));receipts.append(receipt)
        scenarios.append(dict(scenarioId=identity,command=scenario['command'],exitCode=0,result='PASS',blockers=[],
            assertionLedger=ledger,resultFile=receipt,outputFiles=[receipt]))
    required=set(V2_10_4_FAULT_CODES)
    if {row['id'] for row in mutations}!=required or len(mutations)!=len(required):
        raise ValueError('the exact named 29-fault matrix was not measured')
    scenario=contract['scenarios'][7]
    scenarios.insert(7,dict(scenarioId='AC-10.4-08',command=scenario['command'],exitCode=0,result='PASS',blockers=[],
        assertionLedger=[dict(id=f'AC-10.4-08#{n+1:04d}',subject=subject,state='PASS') for n,subject in enumerate((
            'eight-measured-candidate-bound-receipts-including-AC-09','verified-10.3-predecessor',
            '27-exact-reader-substitutions-and-zero-exemptions','29-real-faults-and-byte-identical-restoration',
            'ten-workflow-spans-and-resolved-guidance','nine-approved-raw-gitlink-transitions-and-three-unchanged-roots',
            'closed-schema-and-deterministic-pair'))]))
    return scenarios,receipts,mutations




def v2_10_4_source_errors(repository,record,consumer_candidate=None):
    """Recompute sources, predecessor, exact Git boundary and committed receipt derivations."""
    adoption = v2_10_4_module()
    problems=v2_10_4_pair_errors(record)
    if problems:return problems
    try:
        facts=record['evidenceAdoptionExecution'];candidate=record['candidate']['commit']
        for row in facts['receipts']:
            if v2_sha256(v2_10_1_artifact_bytes(repository,row['path']))!=row['sha256']:
                problems.append('installed receipt differs: '+row['path'])
        if problems:return problems
        for row in [facts['binding'],facts['contract'],*facts['sourceFiles'],*facts['provenance']]:
            blob=v2_committed_blob(repository,candidate,row['path'])
            if blob is None or v2_sha256(blob)!=row['sha256']:problems.append('committed source differs: '+row['path'])
        binding=v2_parse_json(v2_committed_blob(repository,candidate,V2_10_4_BINDING))
        contract=v2_parse_json(v2_committed_blob(repository,candidate,V2_10_4_CONTRACT))
        if binding.get('entrySnapshot')!=dict(path=adoption.SNAPSHOT,sha256=adoption.SNAPSHOT_SHA256):
            return problems+['entry snapshot declaration differs from independent trust root']
        if [row['path'] for row in facts['sourceFiles']]!=binding['sourcePaths']:problems.append('source membership differs')
        if (binding['sourcePaths']!=list(adoption.APPROVED_SOURCE_PATHS) or binding['expectedChangedPaths']!=sorted(
                set(adoption.APPROVED_SOURCE_PATHS)|{row['path'] for row in adoption.APPROVED_GITLINK_AMENDMENT['transitions']})):
            problems.append('independent exact source/transition boundary differs')
        for key in ('baseline','provenance','workflowInventory','guidanceInventory','resolvedCustomization','retiredRoutes','exemptions','readerInventory','rootGitlinks','entrySnapshot','approvedAssertionCorrections','approvedGitlinkAmendment'):
            if facts[key]!=binding[key]:problems.append('effective binding differs: '+key)
        raw=adoption.current.bridge(repository,dict(baseline=adoption.BASELINE,candidate=candidate,expectedPaths=binding['expectedChangedPaths'],includeRootGitlinks=True,
            story104GitlinkAmendment=adoption.APPROVED_GITLINK_AMENDMENT,
            blobPaths=[adoption.SNAPSHOT,*adoption.PROVENANCE]))
        if raw['result']!='PASS' or raw['rootGitlinks']!=adoption.candidate_gitlinks(facts['rootGitlinks']):problems.append('raw boundary or twelve roots differ')
        blobs={row['path']:row for row in raw['blobs']}
        if blobs[adoption.SNAPSHOT]['sha256']!=adoption.SNAPSHOT_SHA256:problems.append('entry trust root differs')
        snapshot=v2_parse_json(base64.b64decode(blobs[adoption.SNAPSHOT]['content']))
        reader_facts=v2_10_4_reader_facts(snapshot)
        if reader_facts!=facts['readerBeforeAfter']:problems.append('reader identity/strength material differs')
        for row in reader_facts:
            blob=v2_committed_blob(repository,candidate,row['path'])
            if blob is None or v2_sha256(blob)!=row['afterSha256']:problems.append('migrated reader differs: '+row['path'])
        if facts['routeFacts']!=v2_10_3_route_facts(repository,candidate,adoption.WORKFLOWS):problems.append('workflow byte spans differ')
        _,validators=v2_load_schemas()
        if v2_verified_predecessor(repository,candidate,'10.3',adoption.PROVENANCE[5],adoption.PROVENANCE[6],validators['record'])!=adoption.PREDECESSOR_SHA256:
            problems.append('verified 10.3 predecessor differs')
        consumer_candidate=consumer_candidate or resolve_commit(repository,'HEAD','CANDIDATE_UNRESOLVABLE')
        def committed(path):
            content=v2_committed_blob(repository,consumer_candidate,path)
            if content is None:raise ValueError('committed receipt unavailable')
            return content
        scenarios,receipts,mutations=v2_10_4_receipt_facts(repository,contract,candidate,committed)
        if (scenarios,receipts,mutations)!=(record['scenarios'],facts['receipts'],facts['mutationLedger']):
            problems.append('committed receipts do not derive the recorded facts')
    except (ValueError,KeyError,TypeError,OSError,V2Stop) as error:problems.append('source consumption unavailable: '+str(error))
    return problems


def v2_10_4_reader_facts(snapshot):
    """Full entry bytes and enumerated substitutions bind identity and strength, not assertion counts."""
    adoption = v2_10_4_module()
    rows=[]
    for entry in snapshot['readers']:
        original=base64.b64decode(entry['entryBase64']);strength=original.decode().replace('\r\n','\n')
        # Preserve every original assertion in this strength material. The sole owner-approved
        # count correction is bound separately and must never be normalized away as plumbing.
        for operation in entry['substitutions']:
            strength=strength.replace(operation['before'],'<plumbing:'+operation['kind']+'>')
        rows.append(dict(path=entry['path'],beforeSha256=entry['entrySha256'],afterSha256=entry['afterSha256'],
            substitutionSha256=v2_sha256(adoption.current.canonical(entry['substitutions'])),identityStrengthSha256=v2_sha256(strength.encode())))
    return rows


def v2_10_4_publication_paths(record):
    return [record['outputs'][kind]['path'] for kind in ('json','markdown')]+[row['path'] for row in record['evidenceAdoptionExecution']['receipts']]


def v2_10_4_descendant_errors(repository,record,head):
    """Require one atomic receipt/pair publication followed only by lifecycle insertion."""
    adoption = v2_10_4_module()
    candidate=record['candidate']['commit'];parent=candidate;publications=0
    if run_git(repository,'merge-base','--is-ancestor',candidate,head,allowed_returncodes=(0,1)).returncode:
        return ['source candidate is not an ancestor']
    for commit in decode(run_git(repository,'rev-list','--reverse',f'{candidate}..{head}').stdout).split():
        if decode(run_git(repository,'rev-list','--parents','-n','1',commit).stdout).split()[1:]!=[parent]:return ['history is not a direct single-parent continuation']
        facts=adoption.current.bridge(repository,dict(baseline=parent,candidate=commit))
        changed={row['path'] for row in facts['diff']}|{row['sourcePath'] for row in facts['diff'] if row['sourcePath']}
        if facts['result']!='PASS':return ['post-source history or gitlinks do not pass']
        if changed==set(v2_10_4_publication_paths(record)) and publications==0:
            for row in record['evidenceAdoptionExecution']['receipts']:
                blob=v2_committed_blob(repository,commit,row['path'])
                if blob is None or v2_sha256(blob)!=row['sha256']:return ['published receipt differs']
            j,m=(v2_committed_blob(repository,commit,record['outputs'][kind]['path']) for kind in ('json','markdown'))
            if j is None or m is None or v2_verify_pair(j,m) or v2_parse_json(j)!=record:return ['published pair differs']
            publications+=1
        elif publications==1 and changed and changed.issubset({V2_10_4_SPEC,'_bmad-output/implementation-artifacts/sprint-status.yaml'}):
            if V2_10_4_SPEC in changed and not v2_spec_lifecycle_only_change(repository,parent,commit,V2_10_4_SPEC,record_region=True,task_checkboxes=True):return ['spec source drift after acceptance']
            if '_bmad-output/implementation-artifacts/sprint-status.yaml' in changed and not v2_sprint_status_only_change(repository,parent,commit,
                    V2_10_4_SPRINT_KEY):return ['tracking source drift after acceptance']
        else:return ['publication boundary or later source changed']
        parent=commit
    return [] if publications==1 else ['exactly one atomic publication is required']


def v2_10_4_generate(options,*,candidate=None,verify_spec=None,consumer_candidate=None):
    """Generate from committed sources, eight receipts and verified predecessor; derive AC-08 last."""
    adoption = v2_10_4_module()
    repository=validate_repository(Path(options.get('--repository') or default_repository()))
    candidate=candidate or resolve_commit(repository,'HEAD','CANDIDATE_UNRESOLVABLE')
    _,validators=v2_load_schemas()
    binding_bytes=v2_committed_blob(repository,candidate,V2_10_4_BINDING);contract_bytes=v2_committed_blob(repository,candidate,V2_10_4_CONTRACT)
    binding=v2_parse_json(binding_bytes);contract=v2_validate_contract(contract_bytes,V2_10_4_CONTRACT,validators['contract'],validators['contract_v14'])
    if [row['id'] for row in contract['scenarios']]!=[f'AC-10.4-0{n}' for n in range(1,10)]:raise ValueError('all nine AC identities are required')
    if [options['--output-json'],options['--output-markdown']]!=contract['finalRecord']['paths']:raise ValueError('output pair differs from contract')
    source_files=[]
    for path in binding['sourcePaths']:
        blob=v2_committed_blob(repository,candidate,path)
        if blob is None:raise ValueError('source unavailable: '+path)
        source_files.append(dict(path=path,sha256=v2_sha256(blob)))
        working=v2_10_1_artifact_bytes(repository,path)
        if not v2_10_4_working_source_matches(repository,candidate,path,working,consumer_candidate) or (
                path==V2_7_2_SPRINT_PATH and working!=blob and verify_spec is None):
            raise ValueError('working source differs: '+path)
    allowed_dirt=set(contract['finalRecord']['paths'])
    for scenario in contract['scenarios']:
        if scenario['id']=='AC-10.4-08':continue
        tokens=shlex.split(scenario['command'])
        output=tokens[tokens.index('-trx')+1] if '-trx' in tokens else (tokens[tokens.index('--output')+1] if '--output' in tokens else v2_pytest_command(tokens)['junit'])
        allowed_dirt.add(str(output))
    # Source bytes were checked above, including the strictly masked lifecycle files.
    allowed_dirt.update(binding['sourcePaths'])
    dirt=set(worktree_path_status(repository))-allowed_dirt
    if dirt:raise V2Stop([v2_finding('WORKTREE_NOT_CLEAN','working-tree',
        'working tree differs outside explicit source/receipt/pair scope: '+v2_path_summary(dirt))],'10.4')
    predecessor=v2_verified_predecessor(repository,candidate,'10.3',adoption.PROVENANCE[5],adoption.PROVENANCE[6],validators['record'])
    if predecessor!=adoption.PREDECESSOR_SHA256:raise ValueError('predecessor does not verify')
    observed=adoption.verify(repository,adoption.BASELINE,candidate,authority_bundle=adoption.PROVENANCE[4],verify_workflows=True)
    if observed['result']!='PASS':
        raise V2Stop([v2_finding('GIT_COMMAND_FAILED' if observed['result']=='BLOCKED' else 'RECORD_NOT_DERIVED',
            'scoped-verifier','scoped verifier does not pass: '+', '.join(observed['blockers']))],'10.4')
    scenarios,receipts,mutations=v2_10_4_receipt_facts(repository,contract,candidate,
        (lambda path:v2_committed_blob(repository,consumer_candidate,path)) if consumer_candidate else None)
    facts={key:binding[key] for key in ('baseline','provenance','workflowInventory','guidanceInventory','resolvedCustomization','retiredRoutes','exemptions','readerInventory','rootGitlinks','entrySnapshot','approvedAssertionCorrections','approvedGitlinkAmendment')}
    facts.update(bindingRule='SC-10.4 is the committed candidate evaluated under the approved current binding',sourceRevisionId=candidate,
        binding=dict(path=V2_10_4_BINDING,sha256=v2_sha256(binding_bytes)),contract=dict(path=V2_10_4_CONTRACT,sha256=v2_sha256(contract_bytes)),
        predecessorPair=dict(json=dict(path=adoption.PROVENANCE[5],sha256=predecessor),markdown=dict(path=adoption.PROVENANCE[6],sha256=v2_sha256(v2_committed_blob(repository,candidate,adoption.PROVENANCE[6])))),
        sourceFiles=source_files,routeFacts=v2_10_3_route_facts(repository,candidate,adoption.WORKFLOWS),
        readerBeforeAfter=v2_10_4_reader_facts(adoption.snapshot(repository)),receipts=receipts,mutationLedger=mutations)
    bundle=v2_parse_json(v2_committed_blob(repository,candidate,adoption.PROVENANCE[4]))
    bundle_digest=v2_sha256(''.join(f'{row["sha256"]}  {row["path"]}\n' for row in bundle['artifacts']).encode())
    if bundle_digest!=bundle['bundleDigest']:raise ValueError('original authority row digest differs')
    paths=contract['finalRecord']['paths']
    observed_roots=[dict(path=path,mode='160000',commit=commit) for path,commit in v2_raw_gitlinks(repository,candidate)]
    record=dict(schemaVersion=V2_RECORD_SCHEMA_VERSION,storyId='10.4',authority={key:contract['authority'][key] for key in ('epic','architecture','planningCandidate')},
        candidate=dict(commit=candidate,gitlinks=observed_roots),predecessors=['10.3'],inventory=contract['inventory'],scenarios=scenarios,
        faultInjection=dict(results=[]),outputs=dict(json=dict(path=paths[0],sha256=V2_ZERO_DIGEST),markdown=dict(path=paths[1],sha256=V2_ZERO_DIGEST)),
        rollback=contract['rollback'],summary=dict(required=9,passed=9,failed=0,blocked=0,skipped=0,notRun=0),renderedMarkdownSha256=V2_ZERO_DIGEST,evidenceAdoptionExecution=facts)
    record['authority']['bundleDigest']=bundle_digest
    final,j,m=v2_finalize(record)
    problems=v2_schema_errors(validators['record'],final)+v2_verify_pair(j,m)
    if problems:raise ValueError('; '.join(problems))
    if verify_spec is not None:return j
    v2_write_outputs([(v2_output_target(repository,paths[0]),j),(v2_output_target(repository,paths[1]),m)])
    return j


def v2_10_4_verify_inserted_impl(options):
    repository=validate_repository(Path(options.get('--repository') or default_repository()));head=resolve_commit(repository,'HEAD','CANDIDATE_UNRESOLVABLE')
    paths=('docs/release-evidence/story-10.4-final-record-v2.json','docs/release-evidence/story-10.4-final-record-v2.md')
    j,m=(v2_committed_blob(repository,head,path) for path in paths)
    if j is None or m is None:raise ValueError('pair must be committed before insertion verification')
    record=v2_parse_json(j);_,validators=v2_load_schemas()
    problems=v2_schema_errors(validators['record'],record)+v2_verify_pair(j,m)+v2_10_4_source_errors(repository,record,head)+v2_10_4_descendant_errors(repository,record,head)
    if problems:raise ValueError('; '.join(problems))
    if options[V2_VERIFY_OPTION]!=V2_10_4_SPEC:raise ValueError('designated 10.4 spec required')
    spec=(repository/V2_10_4_SPEC).read_bytes();region=v2_record_region(spec)
    if region is None or spec[region[0]:region[1]]!=m:raise ValueError('verbatim inserted region differs')
    if not v2_working_spec_lifecycle_only_change(repository,head,V2_10_4_SPEC,spec,record_region=True,task_checkboxes=True):raise ValueError('working spec changes source')
    if not v2_working_sprint_status_only_change(repository,head,V2_10_4_SPRINT_KEY,
            v2_10_1_artifact_bytes(repository,V2_7_2_SPRINT_PATH)):raise ValueError('working sprint changes source')
    for path,content in zip(paths,(j,m)):
        if (repository/path).read_bytes()!=content:raise ValueError('installed pair differs')
    derived=v2_10_4_generate({'--repository':str(repository),'--contract':V2_10_4_CONTRACT,'--format':'bundle',
        '--output-json':paths[0],'--output-markdown':paths[1]},candidate=record['candidate']['commit'],verify_spec=V2_10_4_SPEC,consumer_candidate=head)
    if derived!=j:raise ValueError('candidate rederivation differs')
    return j

def v2_10_4_verify_inserted(options):
    """Report scoped rederivation defects with the stable insertion blocker."""
    try:
        return v2_10_4_verify_inserted_impl(options)
    except ValueError as error:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT', '10.4', str(error))], '10.4') from None


# Story 11.1 (Checkpoint A) closed route: thin-module authoring guidance.
V2_11_1_CONTRACT = '_bmad-output/planning-artifacts/v9/story-contracts/11.1.json'
V2_11_1_RECEIPT_DIR = 'artifacts/v9/11.1/'
V2_11_1_SCENARIOS = tuple(f'AC-11.1-0{n}' for n in range(1, 8))
V2_11_1_TRX_METHODS = {
    'AC-11.1-03': 'GuidanceShouldDefineOneNonShippingTestAppHost',
    'AC-11.1-04': 'GuidanceShouldProhibitPlatformCapabilityOwnership',
}


def v2_11_1_module():
    """Load the guidance generator, which owns the bundle format and the fault expectations."""
    directory = str(Path(__file__).resolve().parent)
    if directory not in sys.path: sys.path.insert(0, directory)
    import generate_thin_authoring_guidance
    import verify_thin_authoring_guidance
    return generate_thin_authoring_guidance, verify_thin_authoring_guidance


def v2_11_1_pair_errors(record):
    """Independent cross-binding checks for one Story 11.1 record."""
    try:
        gen, _ = v2_11_1_module()
        facts = record['thinGuidanceExecution']
        scenarios = record['scenarios']
        if (record['summary'] != dict(required=7, passed=7, failed=0, blocked=0, skipped=0, notRun=0)
                or [row['scenarioId'] for row in scenarios] != list(V2_11_1_SCENARIOS)
                or any(row['result'] != 'PASS' or row['exitCode'] != 0 or row['blockers'] for row in scenarios)):
            return ['Story 11.1 summary or scenarios are not the closed 7/7/0/0/0/0 set']
        if facts['sourceRevisionId'] != record['candidate']['commit'] or record['predecessors'] != ['10.4', '6.2', '7.4']:
            return ['Story 11.1 candidate or predecessor list differs']
        review = facts['reviewDecision']
        if (review['decision'] != 'approved' or not review['reviewer'].strip()
                or review['boundSignablePayloadSha256'] != facts['guidance']['signablePayloadSha256']):
            return ['Story 11.1 reviewer decision is not an approved decision bound to the signable payload']
        if [row['id'] for row in facts['mutationLedger']] != list(gen.FAULT_EXPECTATIONS) or any(
                row['expectedBlocker'] != gen.FAULT_EXPECTATIONS[row['id']] or not row['detected'] or not row['restored']
                for row in facts['mutationLedger']):
            return ['Story 11.1 mutation ledger differs from the closed fault matrix']
        if [row['storyId'] for row in facts['authoritySubstitution']['predecessors']] != ['6.2', '7.4', '10.4']:
            return ['Story 11.1 authority substitution predecessors differ']
    except (KeyError, TypeError, ValueError, AttributeError):
        return ['Story 11.1 execution facts are malformed']
    return []


def v2_11_1_read_receipt(repository, path, candidate_time, read_receipt):
    if read_receipt is not None:
        content = read_receipt(path)
        if content is None: raise ValueError('receipt missing: ' + path)
        return content
    target = repository / path
    if target.is_symlink() or not target.is_file(): raise ValueError('receipt missing: ' + path)
    if int(target.stat().st_mtime) < candidate_time: raise ValueError('receipt is older than the candidate: ' + path)
    return v2_10_1_artifact_bytes(repository, path)


def v2_11_1_receipts(repository, contract, candidate, candidate_time, read_receipt=None):
    """Parse the four measured receipts; return scenario rows, receipt digests and the mutation ledger."""
    gen, verifier = v2_11_1_module()
    by_id = {row['id']: row for row in contract['scenarios']}
    receipts, rows, ledger = [], {}, []
    for identity in ('AC-11.1-03', 'AC-11.1-04'):
        tokens = shlex.split(by_id[identity]['command'])
        path = tokens[tokens.index('-trx') + 1]
        if path != f'{V2_11_1_RECEIPT_DIR}{identity}.trx': raise ValueError(identity + ' receipt path differs')
        content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
        try:
            parsed = parse_trx(content)
        except (ElementTree.ParseError, ValueError):
            raise ValueError(identity + ' TRX is not parseable') from None
        if (len(parsed['results']) != 1 or parsed['recomputed'] != dict(total=1, passed=1, failed=0, skipped=0)
                or parsed['reported']['total'] != 1 or parsed['unknown_outcomes']
                or not parsed['results'][0]['test'].endswith(V2_11_1_TRX_METHODS[identity])):
            raise ValueError(identity + ' TRX is not one passing, non-skipped target test')
        digest = v2_sha256(content)
        receipts.append(dict(scenarioId=identity, path=path, sha256=digest))
        rows[identity] = dict(resultFile=dict(path=path, sha256=digest), subject=V2_11_1_TRX_METHODS[identity])
    tokens = shlex.split(by_id['AC-11.1-05']['command'])
    path = tokens[tokens.index('--output') + 1]
    if path != f'{V2_11_1_RECEIPT_DIR}AC-11.1-05.json': raise ValueError('AC-11.1-05 receipt path differs')
    content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
    report = v2_parse_json(content)
    if report != dict(schemaVersion=verifier.RESULT_SCHEMA, result='PASS', blockers=[]):
        raise ValueError('AC-11.1-05 receipt is not a PASS with no blockers')
    digest = v2_sha256(content)
    receipts.append(dict(scenarioId='AC-11.1-05', path=path, sha256=digest))
    rows['AC-11.1-05'] = dict(resultFile=dict(path=path, sha256=digest), subject='verifier PASS with review decision required')
    command = v2_pytest_command(shlex.split(by_id['AC-11.1-06']['command']))
    path = str(command['junit'])
    if path != f'{V2_11_1_RECEIPT_DIR}AC-11.1-06.xml': raise ValueError('AC-11.1-06 receipt path differs')
    content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
    try:
        suite = ElementTree.fromstring(content)
    except ElementTree.ParseError:
        raise ValueError('AC-11.1-06 JUnit is not parseable XML') from None
    cases = suite.findall('.//testcase')
    if not cases or any(case.findall('failure') or case.findall('error') or case.findall('skipped') for case in cases):
        raise ValueError('AC-11.1-06 JUnit has a failed, errored or skipped case')
    found = {}
    for case in cases:
        name = case.get('name', '')
        if '[' in name and name.endswith(']'): found[name.split('[', 1)[1][:-1]] = name
    if set(found) != set(gen.FAULT_EXPECTATIONS) or len(found) != len([c for c in cases if '[' in c.get('name', '')]):
        raise ValueError('AC-11.1-06 JUnit does not cover exactly the closed fault matrix')
    digest = v2_sha256(content)
    receipts.append(dict(scenarioId='AC-11.1-06', path=path, sha256=digest))
    rows['AC-11.1-06'] = dict(resultFile=dict(path=path, sha256=digest), subject='checkpoint A fault matrix, every mutation detected and restored')
    for fault, code in gen.FAULT_EXPECTATIONS.items():
        ledger.append(dict(id=fault, expectedBlocker=code, detected=True, restored=True))
    return rows, receipts, ledger


def v2_11_1_generate(options, *, candidate=None, read_receipt=None, verify_only=False):
    """Derive the Story 11.1 record from the committed bundle, predecessors and four receipts."""
    gen, verifier = v2_11_1_module()
    repository = validate_repository(Path(options.get('--repository') or default_repository()))
    head = resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')
    supplied = candidate is not None
    candidate = candidate or head
    _, validators = v2_load_schemas()
    contract_bytes = v2_committed_blob(repository, candidate, V2_11_1_CONTRACT)
    contract = v2_validate_contract(contract_bytes, V2_11_1_CONTRACT, validators['contract'], validators['contract_v14'])
    if [row['id'] for row in contract['scenarios']] != list(V2_11_1_SCENARIOS): raise ValueError('all seven AC identities are required')
    paths = contract['finalRecord']['paths']
    if [options['--output-json'], options['--output-markdown']] != paths: raise ValueError('output pair differs from contract')
    findings = []
    authority = v2_authority(repository, candidate, V2_11_1_CONTRACT, contract, validators['bundle'], findings)
    if findings or authority is None: raise V2Stop(findings or [v2_finding('AUTHORITY_BINDING_INVALID', V2_11_1_CONTRACT, 'authority does not bind')], '11.1')
    receipt_paths = {f'{V2_11_1_RECEIPT_DIR}AC-11.1-0{n}.{ext}' for n, ext in ((3, 'trx'), (4, 'trx'), (5, 'json'), (6, 'xml'))}
    dirt = set(worktree_path_status(repository)) - set(paths) - receipt_paths
    if dirt: raise V2Stop([v2_finding('WORKTREE_NOT_CLEAN', 'working-tree', 'working tree differs outside the record pair and receipts: ' + v2_path_summary(sorted(dirt)))], '11.1')
    candidate_time = int(decode(run_git(repository, 'show', '-s', '--format=%ct', candidate).stdout).strip())

    # AC-11.1-01 owner-approved substitution: committed HEAD candidate, recomputed predecessor digests.
    if supplied and candidate != head: raise ValueError('the candidate is not the committed HEAD')
    # AC-11.1-02: the committed bundle is byte-identical to a fresh deterministic regeneration, and verifies.
    bundle_json = v2_committed_blob(repository, candidate, 'docs/release-evidence/thin-authoring-guidance-v2.json')
    bundle_md = v2_committed_blob(repository, candidate, 'docs/release-evidence/thin-authoring-guidance-v2.md')
    schema_bytes = v2_committed_blob(repository, candidate, 'docs/release-evidence/thin-authoring-guidance-v2.schema.json')
    source_bytes = v2_committed_blob(repository, candidate, gen.SOURCE_PATH)
    if None in (bundle_json, bundle_md, schema_bytes, source_bytes): raise ValueError('the guidance source or bundle is not committed at the candidate')
    bundle = v2_parse_json(bundle_json)
    signable = gen.build_signable(repository, source_bytes, contract_bytes)
    regenerated = gen.finalize(signable, bundle['reviewDecision'])
    if regenerated != (bundle_json.decode('utf-8'), bundle_md.decode('utf-8')):
        raise ValueError('the committed bundle is not the deterministic regeneration of its source')
    blockers = verifier.verify(repository, 'docs/release-evidence/thin-authoring-guidance-v2.json', 'docs/release-evidence/thin-authoring-guidance-v2.md', True)
    if blockers: raise ValueError('the guidance verifier does not pass: ' + ', '.join(item['code'] for item in blockers))
    review = bundle['reviewDecision']
    substitution = bundle['validation']['authoritySubstitution']
    for row in substitution['predecessors']:
        blob = v2_committed_blob(repository, candidate, row['path'])
        if blob is None or v2_sha256(blob) != row['sha256']: raise ValueError('predecessor digest differs: ' + row['storyId'])
        if row['binding'] == 'final-record-v2':
            if v2_verified_predecessor(repository, candidate, row['storyId'], row['path'], row['path'][:-4] + 'md', validators['record']) != row['sha256']:
                raise ValueError('predecessor record does not verify: ' + row['storyId'])

    measured, receipts, ledger = v2_11_1_receipts(repository, contract, candidate, candidate_time, read_receipt)
    by_id = {row['id']: row for row in contract['scenarios']}
    def scenario(identity, subjects, **extra):
        return dict(scenarioId=identity, command=by_id[identity]['command'], exitCode=0, result='PASS', blockers=[],
                    assertionLedger=[dict(id=f'{identity}#{n + 1:04d}', subject=subject, state='PASS') for n, subject in enumerate(subjects)], **extra)
    guidance_files = [dict(path='docs/release-evidence/thin-authoring-guidance-v2.json', sha256=v2_sha256(bundle_json)),
                      dict(path='docs/release-evidence/thin-authoring-guidance-v2.md', sha256=v2_sha256(bundle_md))]
    scenarios = [
        scenario('AC-11.1-01', ['candidate is the committed HEAD', 'Story 7.4 and 10.4 final-record v2 digests recomputed', 'Story 6.2 bound through its existing release evidence',
                                'verify_story_candidate.py substitution recorded in the bundle validation']),
        scenario('AC-11.1-02', ['committed JSON and Markdown equal a fresh deterministic regeneration', 'module and platform ownership inventories are exact'],
                 outputFiles=guidance_files),
    ]
    for identity in ('AC-11.1-03', 'AC-11.1-04', 'AC-11.1-05', 'AC-11.1-06'):
        scenarios.append(scenario(identity, [measured[identity]['subject']], resultFile=measured[identity]['resultFile']))
    scenarios.append(scenario('AC-11.1-07', ['AC-11.1-01 through AC-11.1-06 are current and compatible', 'record binds SC-11.1, predecessors, bundle and reviewer digests']))
    facts = dict(
        bindingRule='SC-11.1 is the committed candidate; the record binds the guidance source, bundle and reviewer decision',
        sourceRevisionId=candidate, contract=dict(path=V2_11_1_CONTRACT, sha256=v2_sha256(contract_bytes)),
        guidance=dict(sourcePath=gen.SOURCE_PATH, sourceSha256=v2_sha256(source_bytes),
                      jsonPath=guidance_files[0]['path'], jsonSha256=guidance_files[0]['sha256'],
                      markdownPath=guidance_files[1]['path'], markdownSha256=guidance_files[1]['sha256'],
                      schemaPath='docs/release-evidence/thin-authoring-guidance-v2.schema.json', schemaSha256=v2_sha256(schema_bytes),
                      signablePayloadSha256=bundle['digests']['signablePayloadSha256']),
        reviewDecision=review, authoritySubstitution=substitution, receipts=receipts, mutationLedger=ledger)
    observed_roots = [dict(path=path, mode='160000', commit=commit) for path, commit in v2_raw_gitlinks(repository, candidate)]
    record = dict(schemaVersion=V2_RECORD_SCHEMA_VERSION, storyId='11.1',
                  authority=authority, candidate=dict(commit=candidate, gitlinks=observed_roots), predecessors=list(contract['predecessors']),
                  inventory=contract['inventory'], scenarios=scenarios, faultInjection=dict(results=[]),
                  outputs=dict(json=dict(path=paths[0], sha256=V2_ZERO_DIGEST), markdown=dict(path=paths[1], sha256=V2_ZERO_DIGEST)),
                  rollback=contract['rollback'], summary=dict(required=7, passed=7, failed=0, blocked=0, skipped=0, notRun=0),
                  renderedMarkdownSha256=V2_ZERO_DIGEST, thinGuidanceExecution=facts)
    final, j, m = v2_finalize(record)
    problems = v2_schema_errors(validators['record'], final) + v2_verify_pair(j, m)
    if problems: raise ValueError('; '.join(problems))
    if verify_only: return j
    v2_write_outputs([(v2_output_target(repository, paths[0]), j), (v2_output_target(repository, paths[1]), m)])
    return j


V2_11_1_SPEC = '_bmad-output/implementation-artifacts/spec-11-1-correct-and-validate-platform-hosted-thin-module-authoring-guidance.md'
V2_11_1_SPRINT_KEY = '11-1-correct-and-validate-platform-hosted-thin-module-authoring-guidance'
V2_SPRINT_PATH = '_bmad-output/implementation-artifacts/sprint-status.yaml'


def v2_11_1_descendant_errors(repository, record, head, *, facts_key='thinGuidanceExecution', spec=None, sprint_key=None,
                              extra_publication=(), publication_check=None):
    """Require one atomic receipt/pair publication followed only by lifecycle edits."""
    spec = spec or V2_11_1_SPEC; sprint_key = sprint_key or V2_11_1_SPRINT_KEY
    candidate = record['candidate']['commit']; parent = candidate; publications = 0
    publication = {record['outputs'][kind]['path'] for kind in ('json', 'markdown')} | {
        row['path'] for row in record[facts_key]['receipts']} | set(extra_publication)
    if run_git(repository, 'rev-parse', '--verify', candidate).returncode:
        return ['source candidate is unresolvable']
    if run_git(repository, 'merge-base', '--is-ancestor', candidate, head, allowed_returncodes=(0, 1)).returncode:
        return ['source candidate is not an ancestor']
    for commit in decode(run_git(repository, 'rev-list', '--reverse', f'{candidate}..{head}').stdout).split():
        if decode(run_git(repository, 'rev-list', '--parents', '-n', '1', commit).stdout).split()[1:] != [parent]:
            return ['history is not a direct single-parent continuation']
        changed = set(decode(run_git(repository, 'diff-tree', '--no-commit-id', '--name-only', '-r', '--no-renames', parent, commit).stdout).split('\n')) - {''}
        if changed == publication and publications == 0:
            for row in record[facts_key]['receipts']:
                blob = v2_committed_blob(repository, commit, row['path'])
                if blob is None or v2_sha256(blob) != row['sha256']: return ['published receipt differs']
            if publication_check is not None:
                failure = publication_check(repository, commit, record)
                if failure: return [failure]
            j, m = (v2_committed_blob(repository, commit, record['outputs'][kind]['path']) for kind in ('json', 'markdown'))
            if j is None or m is None or v2_verify_pair(j, m) or v2_parse_json(j) != record: return ['published pair differs']
            publications += 1
        elif publications == 1 and changed and changed <= {spec, V2_SPRINT_PATH}:
            if spec in changed and not v2_spec_lifecycle_only_change(
                    repository, parent, commit, spec, record_region=True, task_checkboxes=True):
                return ['spec source drift after acceptance']
            if V2_SPRINT_PATH in changed and not v2_sprint_status_only_change(repository, parent, commit, sprint_key):
                return ['tracking source drift after acceptance']
        else:
            return ['publication boundary or later source changed']
        parent = commit
    return [] if publications == 1 else ['exactly one atomic publication is required']


def v2_11_1_verify_inserted(options, *, story='11.1', facts_key='thinGuidanceExecution', spec_path=None, sprint_key=None,
                            extra_publication=(), publication_check=None):
    """Verify the committed 11.1/11.2 pair, its single publication, and the verbatim inserted region."""
    spec_path = spec_path or V2_11_1_SPEC; sprint_key = sprint_key or V2_11_1_SPRINT_KEY
    try:
        repository = validate_repository(Path(options.get('--repository') or default_repository()))
        head = resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')
        paths = (f'docs/release-evidence/story-{story}-final-record-v2.json', f'docs/release-evidence/story-{story}-final-record-v2.md')
        j, m = (v2_committed_blob(repository, head, path) for path in paths)
        if j is None or m is None: raise ValueError('pair must be committed before insertion verification')
        record = v2_parse_json(j); _, validators = v2_load_schemas()
        problems = (v2_schema_errors(validators['record'], record) + v2_verify_pair(j, m)
                    + v2_11_1_descendant_errors(repository, record, head, facts_key=facts_key, spec=spec_path,
                                                sprint_key=sprint_key, extra_publication=extra_publication,
                                                publication_check=publication_check))
        if problems: raise ValueError('; '.join(problems))
        if options[V2_VERIFY_OPTION] != spec_path: raise ValueError(f'designated {story} spec required')
        spec = (repository / spec_path).read_bytes(); region = v2_record_region(spec)
        if region is None or spec[region[0]:region[1]] != m: raise ValueError('verbatim inserted region differs')
        if not v2_working_spec_lifecycle_only_change(repository, head, spec_path, spec, record_region=True, task_checkboxes=True):
            raise ValueError('working spec changes source')
        if not v2_working_sprint_status_only_change(repository, head, sprint_key, v2_10_1_artifact_bytes(repository, V2_SPRINT_PATH)):
            raise ValueError('working sprint changes source')
        for path, content in zip(paths, (j, m)):
            if (repository / path).read_bytes() != content: raise ValueError('installed pair differs')
        return j
    except ValueError as error:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT', story, str(error))], story) from None


# Story 11.2 (Checkpoint B) closed route: reproducible minimal-module fixture.
V2_11_2_CONTRACT = '_bmad-output/planning-artifacts/v9/story-contracts/11.2.json'
V2_11_2_RECEIPT_DIR = 'artifacts/v9/11.2/'
V2_11_2_SCENARIOS = tuple(f'AC-11.2-0{n}' for n in range(1, 9))
V2_11_2_RECEIPT_SCENARIOS = ('AC-11.2-01', 'AC-11.2-02', 'AC-11.2-03', 'AC-11.2-04', 'AC-11.2-05', 'AC-11.2-07')
V2_11_2_SPEC = '_bmad-output/implementation-artifacts/spec-11-2-build-the-reproducible-minimal-module-fixture-against-live-platform-apis.md'
V2_11_2_SPRINT_KEY = '11-2-build-the-reproducible-minimal-module-fixture-against-live-platform-apis'
V2_11_2_APPHOST_METHOD = 'MinimalFixtureShouldContainOneNonShippingAppHostAndNoReusableAspireLibrary'


def v2_11_2_module():
    """Load the fixture verifier and proof generator, which own the inventory, proof format and fault expectations."""
    directory = str(Path(__file__).resolve().parent)
    if directory not in sys.path: sys.path.insert(0, directory)
    import generate_minimal_module_fixture_proof
    import verify_minimal_module_fixture
    return verify_minimal_module_fixture, generate_minimal_module_fixture_proof


def v2_11_2_pair_errors(record):
    """Independent cross-binding checks for one Story 11.2 record."""
    try:
        verifier, _ = v2_11_2_module()
        facts = record['fixtureProofExecution']
        scenarios = record['scenarios']
        if (record['summary'] != dict(required=8, passed=8, failed=0, blocked=0, skipped=0, notRun=0)
                or [row['scenarioId'] for row in scenarios] != list(V2_11_2_SCENARIOS)
                or any(row['result'] != 'PASS' or row['exitCode'] != 0 or row['blockers'] for row in scenarios)):
            return ['Story 11.2 summary or scenarios are not the closed 8/8/0/0/0/0 set']
        if facts['sourceRevisionId'] != record['candidate']['commit'] or record['predecessors'] != ['11.1']:
            return ['Story 11.2 candidate or predecessor list differs']
        if (facts['fixture']['inventorySha256'] != verifier.INVENTORY_SHA256 or facts['fixture']['root'] != verifier.FIXTURE_ROOT
                or facts['predecessor']['storyId'] != '11.1'):
            return ['Story 11.2 fixture or predecessor binding differs']
        if [row['id'] for row in facts['mutationLedger']] != list(verifier.FAULT_EXPECTATIONS) or any(
                row['expectedBlocker'] != verifier.FAULT_EXPECTATIONS[row['id']] or not row['detected'] or not row['restored']
                for row in facts['mutationLedger']):
            return ['Story 11.2 mutation ledger differs from the closed fault matrix']
        if sorted(row['scenarioId'] for row in facts['receipts']) != sorted(V2_11_2_RECEIPT_SCENARIOS):
            return ['Story 11.2 receipts differ from the measured scenarios']
        by_id = {row['scenarioId']: row for row in scenarios}
        for row in facts['receipts']:
            if by_id[row['scenarioId']].get('resultFile') != dict(path=row['path'], sha256=row['sha256']):
                return ['Story 11.2 scenario result digests differ from the receipts']
        proof = facts['proof']
        if by_id['AC-11.2-06'].get('outputFiles') != [dict(path=proof['jsonPath'], sha256=proof['jsonSha256']),
                                                      dict(path=proof['markdownPath'], sha256=proof['markdownSha256'])]:
            return ['Story 11.2 proof pair differs from the scenario outputs']
    except (KeyError, TypeError, ValueError, AttributeError):
        return ['Story 11.2 execution facts are malformed']
    return []


def v2_11_2_receipts(repository, contract, candidate_time, read_receipt=None):
    """Parse the six measured receipts; return scenario rows, receipt digests and the mutation ledger."""
    verifier, _ = v2_11_2_module()
    by_id = {row['id']: row for row in contract['scenarios']}
    receipts, rows, ledger = [], {}, []

    tokens = shlex.split(by_id['AC-11.2-01']['command'])
    path = tokens[tokens.index('--output') + 1]
    if path != f'{V2_11_2_RECEIPT_DIR}AC-11.2-01.json': raise ValueError('AC-11.2-01 receipt path differs')
    content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
    report = v2_parse_json(content)
    if (not isinstance(report, dict) or report.get('schemaVersion') != verifier.RESULT_SCHEMA or report.get('result') != 'PASS'
            or report.get('blockers') != [] or report.get('inventorySha256') != verifier.INVENTORY_SHA256
            or report.get('fixtureFileCount') != 13):
        raise ValueError('AC-11.2-01 receipt is not a PASS with the exact frozen inventory')
    digest = v2_sha256(content)
    receipts.append(dict(scenarioId='AC-11.2-01', path=path, sha256=digest))
    rows['AC-11.2-01'] = dict(resultFile=dict(path=path, sha256=digest), subject='exact frozen fixture inventory verified at the committed candidate')

    for scenario, path in verifier.COMMAND_RECEIPTS.items():
        content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
        measured = verifier.command_receipt(repository, scenario, content)
        digest = measured['sha256']
        receipts.append(dict(scenarioId=scenario, path=path, sha256=digest))
        subject = ('measured locked restore with resolved NuGet sources' if scenario == 'AC-11.2-02'
                   else 'measured warnings-as-errors Release build using public platform APIs')
        rows[scenario] = dict(resultFile=dict(path=path, sha256=digest), subject=subject,
                              exitCode=measured['exitCode'])

    tokens = shlex.split(by_id['AC-11.2-04']['command'])
    path = tokens[tokens.index('-trx') + 1]
    if path != verifier.TEST_RESULT_PATH: raise ValueError('AC-11.2-04 receipt path differs')
    content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
    try:
        test_blockers, test_facts = verifier.check_tests(content)
    except verifier.Blocked as error:
        raise ValueError('AC-11.2-04 TRX is not parseable') from error
    if test_blockers or test_facts['counts']['total'] < len(set(verifier.CAPABILITY_TESTS.values())):
        raise ValueError('AC-11.2-04 TRX is vacuous: ' + '; '.join(item['message'] for item in test_blockers))
    digest = v2_sha256(content)
    receipts.append(dict(scenarioId='AC-11.2-04', path=path, sha256=digest))
    rows['AC-11.2-04'] = dict(resultFile=dict(path=path, sha256=digest), subject='non-vacuous minimal-module tests with zero failed, skipped or not-run')

    tokens = shlex.split(by_id['AC-11.2-05']['command'])
    path = tokens[tokens.index('-trx') + 1]
    if path != f'{V2_11_2_RECEIPT_DIR}AC-11.2-05.trx': raise ValueError('AC-11.2-05 receipt path differs')
    content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
    try:
        parsed = parse_trx(content)
    except (ElementTree.ParseError, ValueError):
        raise ValueError('AC-11.2-05 TRX is not parseable') from None
    if (len(parsed['results']) != 1 or parsed['recomputed'] != dict(total=1, passed=1, failed=0, skipped=0)
            or parsed['reported']['total'] != 1 or parsed['unknown_outcomes']
            or not parsed['results'][0]['test'].endswith(V2_11_2_APPHOST_METHOD)):
        raise ValueError('AC-11.2-05 TRX is not one passing, non-skipped target test')
    digest = v2_sha256(content)
    receipts.append(dict(scenarioId='AC-11.2-05', path=path, sha256=digest))
    rows['AC-11.2-05'] = dict(resultFile=dict(path=path, sha256=digest), subject=V2_11_2_APPHOST_METHOD)

    command = v2_pytest_command(shlex.split(by_id['AC-11.2-07']['command']))
    path = str(command['junit'])
    if path != f'{V2_11_2_RECEIPT_DIR}AC-11.2-07.xml': raise ValueError('AC-11.2-07 receipt path differs')
    content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
    try:
        suite = ElementTree.fromstring(content)
    except ElementTree.ParseError:
        raise ValueError('AC-11.2-07 JUnit is not parseable XML') from None
    cases = suite.findall('.//testcase')
    if not cases or any(case.findall('failure') or case.findall('error') or case.findall('skipped') for case in cases):
        raise ValueError('AC-11.2-07 JUnit has a failed, errored or skipped case')
    found = {}
    for case in cases:
        name = case.get('name', '')
        if '[' in name and name.endswith(']'): found[name.split('[', 1)[1][:-1]] = name
    if set(found) != set(verifier.FAULT_EXPECTATIONS) or len(found) != len([c for c in cases if '[' in c.get('name', '')]):
        raise ValueError('AC-11.2-07 JUnit does not cover exactly the closed fault matrix')
    digest = v2_sha256(content)
    receipts.append(dict(scenarioId='AC-11.2-07', path=path, sha256=digest))
    rows['AC-11.2-07'] = dict(resultFile=dict(path=path, sha256=digest), subject='checkpoint B fault matrix, every mutation detected and restored')
    for fault, code in verifier.FAULT_EXPECTATIONS.items():
        ledger.append(dict(id=fault, expectedBlocker=code, detected=True, restored=True))
    return rows, receipts, ledger


def v2_11_2_generate(options, *, candidate=None, read_receipt=None, verify_only=False):
    """Derive the Story 11.2 record from the committed fixture, the 11.1 record, the proof pair and six receipts."""
    verifier, proof_module = v2_11_2_module()
    repository = validate_repository(Path(options.get('--repository') or default_repository()))
    head = resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')
    supplied = candidate is not None
    candidate = candidate or head
    _, validators = v2_load_schemas()
    contract_bytes = v2_committed_blob(repository, candidate, V2_11_2_CONTRACT)
    contract = v2_validate_contract(contract_bytes, V2_11_2_CONTRACT, validators['contract'], validators['contract_v14'])
    if [row['id'] for row in contract['scenarios']] != list(V2_11_2_SCENARIOS): raise ValueError('all eight AC identities are required')
    paths = contract['finalRecord']['paths']
    if [options['--output-json'], options['--output-markdown']] != paths: raise ValueError('output pair differs from contract')
    findings = []
    authority = v2_authority(repository, candidate, V2_11_2_CONTRACT, contract, validators['bundle'], findings)
    if findings or authority is None: raise V2Stop(findings or [v2_finding('AUTHORITY_BINDING_INVALID', V2_11_2_CONTRACT, 'authority does not bind')], '11.2')
    proof_paths = [verifier.PROOF_JSON_PATH, verifier.PROOF_MARKDOWN_PATH]
    receipt_paths = {f'{V2_11_2_RECEIPT_DIR}AC-11.2-01.json', f'{V2_11_2_RECEIPT_DIR}AC-11.2-05.trx',
                     f'{V2_11_2_RECEIPT_DIR}AC-11.2-07.xml', verifier.TEST_RESULT_PATH, *verifier.COMMAND_RECEIPTS.values()}
    dirt = set(worktree_path_status(repository)) - set(paths) - receipt_paths - set(proof_paths) - {V2_11_2_SPEC, V2_SPRINT_PATH}
    if dirt: raise V2Stop([v2_finding('WORKTREE_NOT_CLEAN', 'working-tree', 'working tree differs outside the record pair, proof pair, receipts and lifecycle files: ' + v2_path_summary(sorted(dirt)))], '11.2')
    candidate_time = int(decode(run_git(repository, 'show', '-s', '--format=%ct', candidate).stdout).strip())

    # AC-11.2-01 owner-approved candidate rule: committed HEAD, Story 11.1 digests recomputed.
    if supplied and candidate != head: raise ValueError('the candidate is not the committed HEAD')
    blockers, facts = verifier.verify(repository, verifier.FIXTURE_ROOT, False, verifier.TEST_RESULT_PATH,
                                       candidate_commit=candidate)
    if blockers: raise ValueError('the fixture does not verify: ' + ', '.join(item['code'] for item in blockers))
    predecessor = facts['predecessor']

    # AC-11.2-06: the proof pair is the deterministic, schema-valid projection of the current fixture and result.
    proof_json = v2_10_1_artifact_bytes(repository, verifier.PROOF_JSON_PATH)
    proof_markdown = v2_10_1_artifact_bytes(repository, verifier.PROOF_MARKDOWN_PATH)
    schema_bytes = v2_committed_blob(repository, candidate, verifier.PROOF_SCHEMA_PATH)
    if schema_bytes is None: raise ValueError('the proof schema is not committed at the candidate')
    proof_blockers = verifier.verify_proof(repository, verifier.FIXTURE_ROOT, verifier.PROOF_JSON_PATH,
                                           verifier.PROOF_MARKDOWN_PATH, verifier.TEST_RESULT_PATH, rederive=False,
                                           candidate_commit=candidate)
    if proof_blockers: raise ValueError('the fixture proof does not verify: ' + '; '.join(item['message'] for item in proof_blockers))
    proof = v2_parse_json(proof_json)

    measured, receipts, ledger = v2_11_2_receipts(repository, contract, candidate_time, read_receipt)
    if proof['tests']['resultFile']['sha256'] != measured['AC-11.2-04']['resultFile']['sha256']:
        raise ValueError('the proof does not bind the measured test result')
    for section, scenario_id in (('restore', 'AC-11.2-02'), ('build', 'AC-11.2-03')):
        if proof[section]['receipt'] != {**measured[scenario_id]['resultFile'], 'exitCode': measured[scenario_id]['exitCode']}:
            raise ValueError('the proof does not bind the measured ' + section + ' result')
    by_id = {row['id']: row for row in contract['scenarios']}
    def scenario(identity, subjects, *, exit_code=0, **extra):
        return dict(scenarioId=identity, command=by_id[identity]['command'], exitCode=exit_code, result='PASS', blockers=[],
                    assertionLedger=[dict(id=f'{identity}#{n + 1:04d}', subject=subject, state='PASS') for n, subject in enumerate(subjects)], **extra)
    proof_files = [dict(path=verifier.PROOF_JSON_PATH, sha256=v2_sha256(proof_json)),
                   dict(path=verifier.PROOF_MARKDOWN_PATH, sha256=v2_sha256(proof_markdown))]
    lock_files = [dict(path=row['path'], sha256=row['sha256']) for row in facts['inventory']['lockFiles']]
    scenarios = [
        scenario('AC-11.2-01', [measured['AC-11.2-01']['subject'], 'candidate is the committed HEAD', 'Story 11.1 final-record v2 and guidance digests recomputed'],
                 resultFile=measured['AC-11.2-01']['resultFile']),
        scenario('AC-11.2-02', [measured['AC-11.2-02']['subject']], exit_code=measured['AC-11.2-02']['exitCode'],
                 resultFile=measured['AC-11.2-02']['resultFile']),
        scenario('AC-11.2-03', [measured['AC-11.2-03']['subject']], exit_code=measured['AC-11.2-03']['exitCode'],
                 resultFile=measured['AC-11.2-03']['resultFile']),
        scenario('AC-11.2-04', [measured['AC-11.2-04']['subject']], resultFile=measured['AC-11.2-04']['resultFile']),
        scenario('AC-11.2-05', [measured['AC-11.2-05']['subject']], resultFile=measured['AC-11.2-05']['resultFile']),
        scenario('AC-11.2-06', ['proof JSON and Markdown are deterministic, schema-valid and bound by the signable-payload digest',
                                'proof binds commands, tool versions, public API hashes, AppHost facts, inventory and results'],
                 outputFiles=proof_files),
        scenario('AC-11.2-07', [measured['AC-11.2-07']['subject']], resultFile=measured['AC-11.2-07']['resultFile']),
        scenario('AC-11.2-08', ['AC-11.2-01 through AC-11.2-07 and Story 11.1 are current and compatible',
                                'record binds SC-11.2, Story 11.1, fixture, proof, receipts and mutation ledger']),
    ]
    fixture_record = dict(
        bindingRule='SC-11.2 is the committed candidate; the record binds the Story 11.1 record and guidance, the fixture inventory, the public-API graph, the measured results and the fixture proof',
        sourceRevisionId=candidate, contract=dict(path=V2_11_2_CONTRACT, sha256=v2_sha256(contract_bytes)),
        predecessor=dict(storyId='11.1', recordPath=predecessor['recordPath'], recordSha256=predecessor['recordSha256'],
                         guidanceJsonSha256=predecessor['bundleJsonSha256'], guidanceMarkdownSha256=predecessor['bundleMarkdownSha256'],
                         signablePayloadSha256=predecessor['signablePayloadSha256']),
        fixture=dict(inventoryId=verifier.INVENTORY_ID, inventorySha256=verifier.INVENTORY_SHA256, root=verifier.FIXTURE_ROOT,
                     fileCount=len(facts['inventory']['paths']), lockFiles=lock_files,
                     sourcesSha256=v2_sha256(proof_module.canonical(facts['inventory']['paths']))),
        proof=dict(jsonPath=proof_files[0]['path'], jsonSha256=proof_files[0]['sha256'],
                   markdownPath=proof_files[1]['path'], markdownSha256=proof_files[1]['sha256'],
                   schemaPath=verifier.PROOF_SCHEMA_PATH, schemaSha256=v2_sha256(schema_bytes),
                   signablePayloadSha256=proof['digests']['signablePayloadSha256'],
                   resolvedPublicApisSha256=v2_sha256(proof_module.canonical(proof['resolvedPublicApis'])),
                   resultSha256=proof['tests']['resultFile']['sha256']),
        receipts=receipts, mutationLedger=ledger)
    observed_roots = [dict(path=path, mode='160000', commit=commit) for path, commit in v2_raw_gitlinks(repository, candidate)]
    record = dict(schemaVersion=V2_RECORD_SCHEMA_VERSION, storyId='11.2',
                  authority=authority, candidate=dict(commit=candidate, gitlinks=observed_roots), predecessors=list(contract['predecessors']),
                  inventory=contract['inventory'], scenarios=scenarios, faultInjection=dict(results=[]),
                  outputs=dict(json=dict(path=paths[0], sha256=V2_ZERO_DIGEST), markdown=dict(path=paths[1], sha256=V2_ZERO_DIGEST)),
                  rollback=contract['rollback'], summary=dict(required=8, passed=8, failed=0, blocked=0, skipped=0, notRun=0),
                  renderedMarkdownSha256=V2_ZERO_DIGEST, fixtureProofExecution=fixture_record)
    final, j, m = v2_finalize(record)
    problems = v2_schema_errors(validators['record'], final) + v2_verify_pair(j, m)
    if problems: raise ValueError('; '.join(problems))
    if verify_only: return j
    v2_write_outputs([(v2_output_target(repository, paths[0]), j), (v2_output_target(repository, paths[1]), m)])
    return j


def v2_11_2_publication_check(repository, commit, record):
    """The publication commit must carry the proof pair exactly as the record binds it."""
    proof = record['fixtureProofExecution']['proof']
    for path, digest in ((proof['jsonPath'], proof['jsonSha256']), (proof['markdownPath'], proof['markdownSha256'])):
        blob = v2_committed_blob(repository, commit, path)
        if blob is None or v2_sha256(blob) != digest: return 'published proof differs'
    return None


def v2_11_2_verify_inserted(options):
    """Verify the committed 11.2 pair, its single publication, and the verbatim inserted region."""
    verifier, _ = v2_11_2_module()
    return v2_11_1_verify_inserted(
        options, story='11.2', facts_key='fixtureProofExecution', spec_path=V2_11_2_SPEC, sprint_key=V2_11_2_SPRINT_KEY,
        extra_publication=(verifier.PROOF_JSON_PATH, verifier.PROOF_MARKDOWN_PATH), publication_check=v2_11_2_publication_check)


# Story 11.3 (Checkpoint C): committed measurement and five non-vacuous measured receipts.
V2_11_3_CONTRACT = '_bmad-output/planning-artifacts/v9/story-contracts/11.3.json'
V2_11_3_SPEC = '_bmad-output/implementation-artifacts/spec-11-3-generate-authoritative-sm-2-v2-evidence-and-decide-oq-2.md'
V2_11_3_SPRINT_KEY = '11-3-generate-authoritative-sm-2-v2-evidence-and-decide-oq-2'
V2_11_3_SCENARIOS = tuple(f'AC-11.3-0{number}' for number in range(1, 8))
V2_11_3_RECEIPTS = tuple(f'artifacts/v9/11.3/AC-11.3-0{number}.{extension}'
                        for number, extension in ((2, 'trx'), (3, 'trx'), (4, 'json'), (5, 'json'), (6, 'xml')))
V2_11_3_SOURCES = (
    '_bmad/scripts/generate_sm2_evidence.py', '_bmad/scripts/verify_sm2_evidence.py',
    '_bmad/scripts/verify_story_candidate.py', '_bmad/scripts/generate_story_record.py',
    '_bmad/scripts/tests/test_sm2_evidence.py', '_bmad/scripts/tests/test_generate_story_record.py',
    '_bmad/schemas/story-final-record-v2.schema.json',
    'docs/release-evidence/minimal-module-authoring-cost-sm2-v2.schema.json',
    'tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs',
)


def v2_11_3_modules():
    directory = str(Path(__file__).resolve().parent)
    if directory not in sys.path: sys.path.insert(0, directory)
    import generate_sm2_evidence
    import verify_sm2_evidence
    import verify_story_candidate
    return generate_sm2_evidence, verify_sm2_evidence, verify_story_candidate


def v2_11_3_trx(content, method, candidate, evidence_sha256, metrics):
    """Require one native named test, linked definitions and assembly/evidence revision stamps."""
    try:
        parsed = parse_trx(content)
    except ElementTree.ParseError as error:
        raise ValueError('malformed Story 11.3 native TRX') from error
    if (count_disagreements(parsed) or parsed['reported'] != dict(total=1, executed=1, passed=1, failed=0, skipped=0)
            or len(parsed['results']) != 1 or parsed['unknown_outcomes']):
        raise ValueError('TRX must contain exactly one executed, passing, non-skipped target test')
    testcase = parsed['results'][0]
    root = ElementTree.fromstring(content)
    definitions = root.findall('./{*}TestDefinitions/{*}UnitTest')
    native = root.find('./{*}Results/{*}UnitTestResult')
    if native is None or len(definitions) != 1:
        raise ValueError('TRX requires a native result and exactly one linked definition')
    definition = definitions[0]
    methods = definition.findall('./{*}TestMethod'); executions = definition.findall('./{*}Execution')
    assembly = 'Hexalith.Conversations.Contracts.Tests.dll'
    if (testcase['test'] != method or not native.get('testId') or native.get('testId') != definition.get('id')
            or not native.get('executionId') or len(executions) != 1 or native.get('executionId') != executions[0].get('id')
            or definition.get('name') != method or len(methods) != 1
            or methods[0].get('className') != method.rsplit('.', 1)[0] or methods[0].get('name') != method.rsplit('.', 1)[1]
            or re.split(r'[\\/]', methods[0].get('codeBase', ''))[-1] != assembly
            or re.split(r'[\\/]', definition.get('storage', ''))[-1] != assembly):
        raise ValueError('TRX native assembly or result/definition linkage differs')
    output = testcase['output']
    stamps = re.findall(r'(?m)^story113_source_revision=([^\r\n]+)$', output)
    if (len(stamps) != 1 or not re.fullmatch(r'(?:[0-9A-Za-z.\-]+\+)?' + re.escape(candidate), stamps[0])
            or re.findall(r'(?m)^story113_candidate=([^\r\n]+)$', output) != [candidate]
            or re.findall(r'(?m)^story113_evidence_sha256=([^\r\n]+)$', output) != [evidence_sha256]):
        raise ValueError('TRX source assembly, candidate or measured evidence digest differs')
    if method.endswith('SmTwoV2ShouldCountEveryHandAuthoredFixtureFileIncludingAppHost'):
        totals = '/'.join(str(metrics[key]) for key in ('fileCount', 'physicalLoc', 'nonblankLoc'))
        if re.findall(r'(?m)^story113_measured_totals=([^\r\n]+)$', output) != [totals]:
            raise ValueError('TRX independently measured totals differ')
    return testcase['test']


def v2_11_3_mutations(content, candidate, evidence_sha256, *, repository):
    """Consume actual fault observations and restoration hashes, never infer them from case names."""
    _, verifier, _ = v2_11_3_modules()
    baseline = verifier.verify(repository, candidate=candidate)
    if baseline['result'] != 'PASS' or baseline['evidenceSha256'] != evidence_sha256:
        raise ValueError('fault receipt requires independently verified baseline PASS')
    baseline_hash = verifier.measurement_input_sha256(repository)
    try:
        parsed = v2_parse_junit(content)
    except ElementTree.ParseError as error:
        raise ValueError('malformed Story 11.3 fault JUnit') from error
    cases, reported = parsed['cases'], parsed['reported']
    if (not cases or reported['tests'] != len(cases) or any(reported[key] for key in ('failures', 'errors', 'skipped'))
            or any(case['failure'] or case['error'] or case['skipped'] for case in cases)):
        raise ValueError('fault receipt is empty, inconsistent, failed or skipped')
    observed = {}
    for case in cases:
        properties = dict(case['properties'])
        fault = properties.get('story113_fault_id')
        if (fault not in verifier.FAULT_EXPECTATIONS or fault in observed
                or case['name'] != f'test_checkpoint_c_fault_matrix[{fault}]'
                or case['classname'] != '_bmad.scripts.tests.test_sm2_evidence'
                or len(properties) != len(case['properties'])
                or properties.get('story113_source_revision') != candidate
                or properties.get('story113_evidence_sha256') != evidence_sha256
                or properties.get('story113_expected_code') != verifier.FAULT_EXPECTATIONS[fault]):
            raise ValueError('fault identity, source revision or evidence digest differs')
        blockers = json.loads(properties['story113_observed_blockers'])
        ledger = json.loads(properties['story113_assertion_ledger'])
        expected_state, expected_exit = ('BLOCKED', '2') if fault == 'history-unavailable' else ('FAIL', '1')
        before, after = properties['story113_before_sha256'], properties['story113_after_sha256']
        mutated = properties['story113_mutated_sha256']
        baseline_report = json.loads(properties['story113_baseline_report'])
        restored_report = json.loads(properties['story113_restored_report'])
        observed_report = json.loads(properties['story113_observed_report'])
        known_codes = set(verifier.FAULT_EXPECTATIONS.values())
        if (not isinstance(blockers, list) or blockers != sorted(set(blockers)) or not set(blockers) <= known_codes
                or verifier.FAULT_EXPECTATIONS[fault] not in blockers or not isinstance(ledger, list) or not ledger
                or any(set(row) != {'id', 'subject', 'state'} or not row['id'] or not row['subject']
                       or row['state'] not in ('PASS', 'FAIL', 'SKIPPED') for row in ledger)
                or len({row['id'] for row in ledger}) != len(ledger)
                or not any(row['state'] == ('SKIPPED' if expected_state == 'BLOCKED' else 'FAIL') for row in ledger)
                or properties.get('story113_observed_result') != expected_state
                or properties.get('story113_observed_exit') != expected_exit
                or before != baseline_hash or after != baseline_hash
                or re.fullmatch(r'[0-9a-f]{64}', mutated) is None or mutated in (baseline_hash, '0'*64)
                or properties.get('story113_baseline_exit') != '0' or properties.get('story113_restored_exit') != '0'
                or baseline_report != baseline or restored_report != baseline
                or not isinstance(observed_report, dict) or set(observed_report) != set(baseline)
                or observed_report.get('result') != expected_state or observed_report.get('blockers') != blockers
                or observed_report.get('assertionLedger') != ledger):
            raise ValueError('fault observation, exact blocker, ledger or byte-identical restoration differs')
        observed[fault] = dict(id=fault, expectedBlocker=verifier.FAULT_EXPECTATIONS[fault], observedBlockers=blockers,
                               beforeSha256=before, afterSha256=after, detected=True, restored=True, result='PASS')
    if set(observed) != set(verifier.FAULT_EXPECTATIONS):
        raise ValueError('the exact closed checkpoint C fault matrix was not measured')
    return [observed[fault] for fault in verifier.FAULT_EXPECTATIONS]


def v2_11_3_pair_errors(record):
    try:
        _, verifier, _ = v2_11_3_modules()
        facts, scenarios = record['sm2Execution'], record['scenarios']
        if (record['summary'] != dict(required=7, passed=7, failed=0, blocked=0, skipped=0, notRun=0)
                or [row['scenarioId'] for row in scenarios] != list(V2_11_3_SCENARIOS)
                or any(row['result'] != 'PASS' or row['exitCode'] != 0 or row['blockers'] or not row['assertionLedger']
                       or any(assertion['state'] != 'PASS' for assertion in row['assertionLedger']) for row in scenarios)
                or record['predecessors'] != ['11.2'] or facts['sourceRevisionId'] != record['candidate']['commit']
                or facts['checkpoints']['C'] != record['candidate']['commit']):
            return ['Story 11.3 requires the compatible closed 7/7/0/0/0/0 PASS set']
        if [row['path'] for row in facts['sourceFiles']] != list(V2_11_3_SOURCES):
            return ['Story 11.3 source membership differs']
        if [row['path'] for row in facts['receipts']] != list(V2_11_3_RECEIPTS):
            return ['Story 11.3 measured receipt membership differs']
        by_id = {row['scenarioId']: row for row in scenarios}
        for row in facts['receipts']:
            if by_id[row['scenarioId']].get('resultFile') != dict(path=row['path'], sha256=row['sha256']):
                return ['Story 11.3 receipt/scenario digest differs']
        evidence = facts['evidence']
        if by_id['AC-11.3-01'].get('outputFiles') != [dict(path=evidence[prefix+'Path'], sha256=evidence[prefix+'Sha256']) for prefix in ('json', 'markdown')]:
            return ['Story 11.3 evidence pair/scenario output differs']
        faults = facts['mutationLedger']
        if ([row['id'] for row in faults] != list(verifier.FAULT_EXPECTATIONS)
                or any(row['expectedBlocker'] != verifier.FAULT_EXPECTATIONS[row['id']]
                       or row['expectedBlocker'] not in row['observedBlockers'] or not row['detected'] or not row['restored']
                       or row['beforeSha256'] != row['afterSha256'] for row in faults)):
            return ['Story 11.3 fault observation/restoration ledger differs']
        metrics = facts['metrics']
        if (metrics['fileCount'] != 13 or metrics['measurementConfidence'] != 'high' or metrics['comparisonConfidence'] != 'low'
                or metrics['oq2Decision'] != 'accepted'):
            return ['Story 11.3 measured metric confidence or OQ-2 decision differs']
    except (KeyError, ValueError, TypeError, AttributeError):
        return ['Story 11.3 execution facts are malformed']
    return []


def v2_11_3_generate(options, *, candidate=None, read_receipt=None, verify_only=False):
    gen, verifier, chain = v2_11_3_modules()
    repository = validate_repository(Path(options.get('--repository') or default_repository()))
    candidate = candidate or resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')
    _, validators = v2_load_schemas()
    contract_bytes = v2_committed_blob(repository, candidate, V2_11_3_CONTRACT)
    contract = v2_validate_contract(contract_bytes, V2_11_3_CONTRACT, validators['contract'], validators['contract_v14'])
    paths = contract['finalRecord']['paths']
    if ([row['id'] for row in contract['scenarios']] != list(V2_11_3_SCENARIOS)
            or [options['--output-json'], options['--output-markdown']] != paths):
        raise ValueError('the exact seven contracted commands and output pair are required')
    findings = []
    authority = v2_authority(repository, candidate, V2_11_3_CONTRACT, contract, validators['bundle'], findings)
    if findings or authority is None: raise ValueError('Story 11.3 authority does not bind')
    allowed = set(paths) | set(V2_11_3_RECEIPTS) | {gen.JSON_PATH, gen.MARKDOWN_PATH, V2_11_3_SPEC, V2_SPRINT_PATH}
    dirt = set(worktree_path_status(repository)) - allowed
    if dirt: raise V2Stop([v2_finding('WORKTREE_NOT_CLEAN', 'working-tree',
                                    'working tree differs outside publication and lifecycle files: ' + v2_path_summary(sorted(dirt)))], '11.3')
    sources = []
    for path in V2_11_3_SOURCES:
        content = v2_committed_blob(repository, candidate, path)
        if content is None or content != v2_10_1_artifact_bytes(repository, path):
            raise ValueError('implementation must be committed before measurement: ' + path)
        sources.append(dict(path=path, sha256=v2_sha256(content)))
    binding = chain.verify(repository, candidate)
    verified = verifier.verify(repository, candidate=candidate)
    if binding['result'] == 'BLOCKED' or verified['result'] == 'BLOCKED':
        raise V2Stop([v2_finding('CANDIDATE_UNRESOLVABLE', '11.3',
                                'upstream verification is BLOCKED: ' + ', '.join(binding['blockers'] + verified['blockers']))], '11.3')
    if binding['result'] != 'PASS' or verified['result'] != 'PASS':
        upstream = binding['blockers'] + verified['blockers']
        raise V2Stop([v2_finding('RECORD_NOT_DERIVED', '11.3', 'upstream verifier FAIL: ' + code)
                      for code in upstream] or [v2_finding('RECORD_NOT_DERIVED', '11.3', 'upstream verifier reported FAIL without blockers')], '11.3')
    evidence_bytes = v2_10_1_artifact_bytes(repository, gen.JSON_PATH)
    evidence = v2_parse_json(evidence_bytes)
    if evidence['candidate']['commit'] != candidate: raise ValueError('SM-2 evidence is stale for this source candidate')
    replay = dict(_recorded_versions=evidence['toolVersions']) if verify_only else {}
    if gen.finalize(gen.build_signable(repository, candidate=candidate, **replay)) != (evidence_bytes, v2_10_1_artifact_bytes(repository, gen.MARKDOWN_PATH)):
        raise ValueError('SM-2 pair differs from deterministic regeneration')
    candidate_time = int(decode(run_git(repository, 'show', '-s', '--format=%ct', candidate).stdout).strip())
    measured, receipts, ledger = {}, [], []
    by_id = {row['id']: row for row in contract['scenarios']}
    for number, path in enumerate(V2_11_3_RECEIPTS, 2):
        identity = f'AC-11.3-0{number}'
        content = v2_11_1_read_receipt(repository, path, candidate_time, read_receipt)
        tokens = shlex.split(by_id[identity]['command'])
        if path not in tokens and not any(token == '--junitxml=' + path for token in tokens):
            raise ValueError('receipt path differs from exact contract command: ' + identity)
        if number in (2, 3):
            subject = v2_11_3_trx(content, tokens[tokens.index('-method')+1], candidate, v2_sha256(evidence_bytes), verified['metrics'])
            execution = [dict(id=identity+'#0001', subject=subject, state='PASS')]
        elif number in (4, 5):
            expected = verified if number == 4 else binding
            if v2_parse_json(content) != expected: raise ValueError(identity + ' receipt differs from independent current verification')
            execution = expected['assertionLedger']
        else:
            ledger = v2_11_3_mutations(content, candidate, v2_sha256(evidence_bytes), repository=repository)
            execution = [dict(id=identity+'#'+row['id'], subject=row['expectedBlocker'], state='PASS') for row in ledger]
        execution = [dict(id=f'{identity}#{index+1:04d}', subject=row['id']+'::'+row['subject'], state=row['state'])
                     for index, row in enumerate(execution)]
        receipt = dict(path=path, sha256=v2_sha256(content))
        receipts.append(dict(scenarioId=identity, **receipt))
        measured[identity] = dict(receipt=receipt, ledger=execution)
    evidence_files = [dict(path=path, sha256=v2_sha256(v2_10_1_artifact_bytes(repository, path))) for path in (gen.JSON_PATH, gen.MARKDOWN_PATH)]
    scenarios = []
    for identity in V2_11_3_SCENARIOS:
        execution = measured.get(identity)
        scenario = dict(scenarioId=identity, command=by_id[identity]['command'], exitCode=0, result='PASS', blockers=[],
                        assertionLedger=execution['ledger'] if execution else [dict(id=identity+'#0001',
                            subject='committed sources, exact measurement, compatible A-C chain and deterministic pair' if identity.endswith('01')
                            else 'five measured current receipts, exact fault ledger and closed final-record schema', state='PASS')])
        if execution: scenario['resultFile'] = execution['receipt']
        if identity.endswith('01'): scenario['outputFiles'] = evidence_files
        scenarios.append(scenario)
    facts = dict(bindingRule='SC-11.3 is the committed source candidate; original A/B publications, all authored bytes, immutable history, current receipts and the qualified OQ-2 decision are independently verified',
                 sourceRevisionId=candidate, contract=dict(path=V2_11_3_CONTRACT, sha256=v2_sha256(contract_bytes)),
                 checkpoints=binding['checkpoints'], sourceFiles=sources, provenance=binding['bindings'],
                 historicalBaseline=evidence['historicalBaseline']['history'], metrics=verified['metrics'],
                 evidence=dict(jsonPath=gen.JSON_PATH, jsonSha256=evidence_files[0]['sha256'], markdownPath=gen.MARKDOWN_PATH,
                               markdownSha256=evidence_files[1]['sha256'], schemaPath=gen.SCHEMA,
                               schemaSha256=v2_sha256(v2_10_1_artifact_bytes(repository, gen.SCHEMA)),
                               signablePayloadSha256=evidence['digests']['signablePayloadSha256']),
                 receipts=receipts, mutationLedger=ledger)
    record = dict(schemaVersion=V2_RECORD_SCHEMA_VERSION, storyId='11.3', authority=authority,
                  candidate=dict(commit=candidate, gitlinks=binding['gitlinks']), predecessors=['11.2'], inventory=contract['inventory'],
                  scenarios=scenarios, faultInjection=dict(results=[]), rollback=contract['rollback'],
                  outputs=dict(json=dict(path=paths[0], sha256=V2_ZERO_DIGEST), markdown=dict(path=paths[1], sha256=V2_ZERO_DIGEST)),
                  summary=dict(required=7, passed=7, failed=0, blocked=0, skipped=0, notRun=0),
                  renderedMarkdownSha256=V2_ZERO_DIGEST, sm2Execution=facts)
    final, j, m = v2_finalize(record)
    problems = v2_schema_errors(validators['record'], final) + v2_verify_pair(j, m)
    if problems: raise ValueError('; '.join(problems))
    if not verify_only:
        v2_write_outputs([(v2_output_target(repository, paths[0]), j), (v2_output_target(repository, paths[1]), m)])
    return j


def v2_11_3_publication_check(repository, commit, record):
    facts = record['sm2Execution']['evidence']
    for prefix in ('json', 'markdown'):
        content = v2_committed_blob(repository, commit, facts[prefix+'Path'])
        if content is None or v2_sha256(content) != facts[prefix+'Sha256']: return 'published SM-2 evidence differs'
    return None


def v2_11_3_verify_inserted(options):
    """Recompute original C sources/receipts before enforcing atomic publication and verbatim insertion."""
    gen, _, _ = v2_11_3_modules()
    repository = validate_repository(Path(options.get('--repository') or default_repository()))
    head = resolve_commit(repository, 'HEAD', 'CANDIDATE_UNRESOLVABLE')
    path = 'docs/release-evidence/story-11.3-final-record-v2.json'
    try:
        record_bytes = v2_committed_blob(repository, head, path)
        if record_bytes is None: raise ValueError('the Story 11.3 final record must be committed')
        record = v2_parse_json(record_bytes)
        derive = dict(options, **{'--output-json': path, '--output-markdown': 'docs/release-evidence/story-11.3-final-record-v2.md'})
        fresh = v2_11_3_generate(derive, candidate=record['candidate']['commit'], verify_only=True)
        if fresh != record_bytes: raise ValueError('record differs from independently recomputed source candidate and current receipts')
    except ValueError as error:
        raise V2Stop([v2_finding('RECORD_CONTENT_DRIFT', '11.3', str(error))], '11.3') from None
    return v2_11_1_verify_inserted(options, story='11.3', facts_key='sm2Execution', spec_path=V2_11_3_SPEC,
                                  sprint_key=V2_11_3_SPRINT_KEY, extra_publication=(gen.JSON_PATH, gen.MARKDOWN_PATH),
                                  publication_check=v2_11_3_publication_check)


def v2_main(raw_arguments: Sequence[str]) -> int:
    """Run the v2 route: exit 0 PASS, 1 FAIL, 2 BLOCKED; stdout is always schema-valid."""
    story_id: str | None = None
    try:
        options = v2_parse_arguments(raw_arguments)
        if V2_VERIFY_OPTION in options:
            if options.get("--contract") == V2_11_3_CONTRACT:
                json_bytes = v2_11_3_verify_inserted(options)
            elif options.get("--contract") == V2_11_2_CONTRACT:
                json_bytes = v2_11_2_verify_inserted(options)
            elif options.get("--contract") == V2_11_1_CONTRACT:
                json_bytes = v2_11_1_verify_inserted(options)
            elif options.get("--contract") == V2_10_4_CONTRACT:
                json_bytes = v2_10_4_verify_inserted(options)
            else:
                json_bytes = (v2_10_3_verify_inserted(options) if options.get("--contract") == V2_10_3_CONTRACT
                          else v2_verify_inserted(options))
        elif "--historical" in options:
            json_bytes, historical_exit = v2_historical(options)
            v2_write_stdout(json_bytes)
            return historical_exit
        else:
            json_bytes = v2_generate(options)
    except V2Stop as stop:
        findings, story_id = stop.findings, stop.story_id
    except GateError as error:
        code = error.code if error.code in ("GIT_UNAVAILABLE", "GIT_COMMAND_FAILED") else (
            "INTERNAL_ERROR"
        )
        findings = [
            v2_finding(code, "git", "a Git command failed while deriving the record")
        ]
    except Exception as error:  # noqa: BLE001 - always emit a schema-valid failure
        findings = [
            v2_finding(
                "INTERNAL_ERROR",
                "generator",
                f"unexpected internal error ({type(error).__name__})",
            )
        ]
    else:
        v2_write_stdout(json_bytes)
        return 0

    document = v2_failure_document(findings, story_id)
    v2_write_stdout(
        (json.dumps(document, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    )
    return document["exitCode"]


def is_v2_invocation(raw_arguments: Sequence[str]) -> bool:
    """The exact `--contract` option selects v2; argparse prefixes never do."""
    return any(
        token == "--contract" or token.startswith("--contract=") for token in raw_arguments
    )


def build_parser() -> argparse.ArgumentParser:
    parser = GateArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=str(default_repository()))
    parser.add_argument("--story")
    parser.add_argument("--baseline")
    parser.add_argument("--candidate", default="HEAD")
    parser.add_argument(
        "--test-results", action="append", default=[], metavar="NAME=PATH"
    )
    parser.add_argument("--submodule", action="append", default=[])
    parser.add_argument("--require-remote", action="append", default=[])
    parser.add_argument(
        "--format", choices=("json", "markdown", "bundle"), default="json"
    )
    parser.add_argument("--historical", action="store_true")
    parser.add_argument("--verify-record-sha256")
    return parser


def is_format_option(token: str) -> bool:
    """argparse accepts any unambiguous prefix, so --forma and --f mean --format too."""
    return len(token) > 2 and token.startswith("--") and "--format".startswith(token)


def pre_parse_output_format(raw_arguments: Sequence[str]) -> str:
    """Best-effort output format, used only if a GateError occurs before argparse succeeds."""
    output_format = "json"
    for index, token in enumerate(raw_arguments):
        if token == "--":
            break
        option, separator, inline_value = token.partition("=")
        if not is_format_option(option):
            continue
        if separator:
            value = inline_value
        else:
            value = raw_arguments[index + 1] if index + 1 < len(raw_arguments) else None
        output_format = value if value in ("markdown", "bundle") else "json"
    return output_format


def main(arguments: Sequence[str] | None = None) -> int:
    raw_arguments = list(arguments) if arguments is not None else sys.argv[1:]
    if is_v2_invocation(raw_arguments):
        return v2_main(raw_arguments)
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
