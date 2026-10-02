#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# ///
"""Generate a story final record from measured repository state."""

import argparse
import hashlib
import json
import os
import re
import shlex
import shutil
import subprocess
import sys
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
        paths.append(promotion_path(value))
    return sorted(set(paths))


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
        {"test": element.get("testName") or "", "outcome": element.get("outcome") or ""}
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
    if not 0 <= reported["executed"] <= reported["total"]:
        disagreements.append(
            "executed {executed} is outside zero through total {total}".format(
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
        contract_path = f"_bmad-output/planning-artifacts/v9/story-contracts/{contract['storyId']}.json"
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
                "the candidate .gitmodules declares a path outside references/ or with no value",
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
    lines.extend(["", "## Fault injection", ""])
    faults = record["faultInjection"]["results"]
    if faults and record["storyId"] == "7.4":
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
        try:
            if original is None:
                target.unlink(missing_ok=True)
            else:
                temporary.write_bytes(original)
                os.replace(temporary, target)
        except OSError:
            restored = False
        finally:
            try:
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
    staged: list[tuple[Path, Path]] = []
    originals: dict[Path, bytes | None] = {}
    replaced: list[Path] = []
    try:
        for target, content in targets:
            target.parent.mkdir(parents=True, exist_ok=True)
            originals[target] = target.read_bytes() if target.exists() else None
            temporary = target.with_name(f".{target.name}.{os.getpid()}.tmp")
            with temporary.open("wb") as handle:
                handle.write(content)
                handle.flush()
                os.fsync(handle.fileno())
            staged.append((temporary, target))
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
            task_section = re.sub(rb"(?m)^- \[[ x]\] ", b"- [<task>] ", task_section)
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
    require_transition: bool = True,
) -> bool:
    """Accept one story row, its `last_updated` date, and the header comment date."""
    status = re.compile(
        rb"(?m)^[ \t]*" + re.escape(sprint_key.encode("ascii")) + rb":[ \t]*"
        rb"(?P<value>backlog|draft|ready-for-dev|in-progress|in-review|review|done)[ \t]*$"
    )
    update_date = re.compile(rb"(?m)^last_updated:[ \t]*(?P<value>\d{4}-\d{2}-\d{2})[ \t]*$")
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
    latest = without_status(v2_committed_blob(repository, head, V2_7_2_SPRINT_PATH))
    if original is None or latest is None or original[0] != latest[0]:
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
    sprint_key = candidates[0].stem.removeprefix("spec-")
    return story_id, spec_path, sprint_key, True


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
            or v2_schema_errors(validator, record)
            or v2_verify_pair(json_bytes, markdown_bytes)
        ):
            return None
    except (UnicodeDecodeError, ValueError, AttributeError, KeyError, TypeError):
        return None
    return v2_sha256(json_bytes)


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

    def read_output(path: str) -> tuple[bytes, int] | None:
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
        if committed is None and modified < candidate_ns:
            fail("TEST_RESULTS_STALE", f"the declared output predates the candidate: {path}")
        outputs.append((path, content, modified))
        bound_files.append({"path": path, "sha256": v2_sha256(content)})
        return content, modified

    kind = command["kind"]
    if kind in ("python", "build", "restore"):
        try:
            executed = subprocess.run(shlex.split(scenario["command"]), cwd=repository,
                                      stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                      timeout=600, check=False)
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
            if dotnet_source_revisions(binary) != [candidate] or binary_time > result[1]:
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
        committed = v2_committed_blob(repository, candidate, script)
        script_file = repository / script
        try:
            current = script_file.read_bytes() if script_file.is_file() and not script_file.is_symlink() else None
        except OSError:
            current = None
        if committed is None or current != committed:
            fail("SCENARIO_COMMAND_UNSUPPORTED", "the check script differs from the candidate")
        else:
            try:
                process = subprocess.run(shlex.split(scenario["command"]), cwd=repository,
                                         stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                         timeout=120, check=False)
                if process.returncode:
                    fail("TEST_RESULTS_FAILED", f"the authority check exited {process.returncode}")
                else:
                    subjects.append(f"python-check::{script}")
            except (OSError, subprocess.TimeoutExpired):
                fail("TEST_RESULTS_FAILED", "the authority check did not complete")
    elif kind == "python":
        script = command["script"]
        if v2_committed_blob(repository, candidate, script) is None:
            fail("SCENARIO_COMMAND_UNSUPPORTED", f"the Python script is not committed: {script}")
        if command["options"].get("--scenario", scenario_id) != scenario_id:
            fail("SCENARIO_RESULT_MISMATCH", "the Python command names another scenario")
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
                    if "candidate" in document and isinstance(document["candidate"], str) and document["candidate"] != candidate:
                        raise ValueError("the machine result names another candidate")
                    candidate_value = document.get("candidate")
                    if (isinstance(candidate_value, dict) and "commit" in candidate_value
                            and candidate_value["commit"] != candidate):
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
        if v2_committed_blob(repository, candidate, project) is None:
            fail("SCENARIO_COMMAND_UNSUPPORTED", f"the build target is not committed: {project}")
        elif kind == "build":
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
                if result is not None and dotnet_source_revisions(result[0]) != [candidate]:
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
                    if read_output(assets) is not None:
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
    if not findings:
        record.update({"exitCode": 0, "result": "PASS"})
    record["blockers"] = sorted({item["code"] for item in findings})
    return record, "passed" if not findings else "failed", findings


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
    try:
        schema = v2_parse_json(output_bytes["schema"])
        disposition = v2_parse_json(output_bytes["json"])
        jsonschema.Draft202012Validator.check_schema(schema)
        if v2_schema_errors(jsonschema.Draft202012Validator(schema), disposition):
            stop("UX_SCHEMA_INVALID", V2_8_1_DISPOSITION_PATHS[1])
    except (UnicodeDecodeError, ValueError, jsonschema.SchemaError):
        stop("UX_SCHEMA_INVALID", V2_8_1_DISPOSITION_PATHS[0])
    if disposition.get("renderedMarkdownSha256") != outputs["markdown"]["sha256"]:
        stop("UX_RENDER_DRIFT", V2_8_1_DISPOSITION_PATHS[2])
    if disposition.get("status") != "preserved-not-activated" or "not activated" not in disposition.get("preservationBanner", ""):
        stop("UX_ACTIVATION_UNAUTHORIZED", V2_8_1_DISPOSITION_PATHS[1])
    decisions = disposition.get("decisions", [])
    acceptance = disposition.get("acceptanceCriteria", [])
    expected_acceptance = [*(f"AC-SAFE-{i:03d}" for i in range(1, 9)),
                           *(f"AC-RESP-{i:03d}" for i in range(1, 16)),
                           "AC-A11Y-001", "AC-A11Y-002", "AC-LEAK-001", "AC-MOB-001", "AC-PERF-001"]
    if [row.get("id") for row in decisions] != [f"UX-DR{i}" for i in range(1, 53)]:
        stop("UX_DECISION_INVENTORY_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    if [row.get("id") for row in acceptance] != expected_acceptance:
        stop("UX_ACCEPTANCE_INVENTORY_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    if [(row.get("path"), row.get("sha256")) for row in disposition.get("sources", [])] != [
        (row["path"], row["sha256"]) for row in sources]:
        stop("UX_SOURCE_DRIFT", V2_8_1_DISPOSITION_PATHS[1])
    if any(row.get("status") != "preserved-not-activated" for row in decisions + acceptance):
        stop("UX_ACTIVATION_UNAUTHORIZED", V2_8_1_DISPOSITION_PATHS[1])
    if any(row.get("owner") != "Stories 8.1-8.2 preservation contract" for row in decisions + acceptance):
        stop("UX_CURRENT_STORY_INVALID", V2_8_1_DISPOSITION_PATHS[1])
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
    module_path = Path(__file__).with_name("generate_ux_preservation_disposition.py")
    module_spec = importlib_util.spec_from_file_location("story81_disposition_derivation", module_path)
    if module_spec is None or module_spec.loader is None:
        stop("UX_SCHEMA_INVALID", str(module_path))
    ux_module = importlib_util.module_from_spec(module_spec)
    module_spec.loader.exec_module(ux_module)
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


def v2_generate(options: dict[str, str]) -> bytes:
    """Derive, validate, and atomically write the v2 pair; return the JSON bytes."""
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
        repository, candidate, contract, validators["bundle"], findings
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
    successor_scenarios: list[tuple[dict[str, Any], dict[str, Any]]] = []
    self_scenarios: list[dict[str, Any]] = []
    for position, scenario in enumerate(contract["scenarios"]):
        try:
            tokens = shlex.split(scenario["command"])
        except ValueError:
            tokens = []
        ux_command = v2_ux_command(tokens, scenario["id"]) if story_id == "8.1" else None
        pytest_command = v2_pytest_command(tokens)
        generator_command = None if pytest_command else v2_generator_command(tokens)
        successor_command = (
            v2_successor_command(tokens, contract_path)
            if int(story_id.split(".")[0]) >= 8 and pytest_command is None
            and ux_command is None and generator_command is None else None
        )
        acceptance_command = (
            None if pytest_command or generator_command else v2_acceptance_command(tokens)
        )
        if ux_command is not None:
            ux_scenarios.append((scenario, ux_command))
        elif generator_command is not None:
            try:
                final_tokens = shlex.split(contract["scenarios"][-1]["command"])
            except ValueError:
                final_tokens = []
            final_route = v2_successor_command(final_tokens, contract_path)
            permitted_early = (int(story_id.split(".")[0]) >= 8
                               and final_route is not None
                               and final_route["kind"] == "python_check")
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
    for scenario, command in successor_scenarios:
        scenario_record, category, scenario_findings = v2_successor_scenario_from_results(
            repository, scenario, command, candidate, candidate_time * 1_000_000_000,
            gitlink_paths,
        )
        scenario_records[scenario["id"]] = scenario_record
        categories[scenario["id"]] = category
        findings.extend(scenario_findings)
        if "resultFile" in scenario_record:
            parsed_results += 1
    ledger_rows = sum(
        len(item.get("assertionLedger", [])) for item in scenario_records.values()
    )
    if (pytest_scenarios or acceptance_scenarios or ux_scenarios or successor_scenarios) and parsed_results == 0:
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
        expected_outputs = tuple(contract["finalRecord"]["paths"])
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


def v2_main(raw_arguments: Sequence[str]) -> int:
    """Run the v2 route: exit 0 PASS, 1 FAIL, 2 BLOCKED; stdout is always schema-valid."""
    story_id: str | None = None
    try:
        options = v2_parse_arguments(raw_arguments)
        if V2_VERIFY_OPTION in options:
            json_bytes = v2_verify_inserted(options)
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
    for index, token in enumerate(raw_arguments):
        option, separator, inline_value = token.partition("=")
        if not is_format_option(option):
            continue
        if separator:
            return inline_value if inline_value in ("markdown", "bundle") else "json"
        following = raw_arguments[index + 1] if index + 1 < len(raw_arguments) else None
        return following if following in ("markdown", "bundle") else "json"
    return "json"


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
