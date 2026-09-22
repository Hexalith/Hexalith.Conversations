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
        raise GateError("GIT_COMMAND_FAILED", f"git command failed: {error}") from error

    if result.returncode not in allowed_returncodes:
        rendered = " ".join(arguments)
        stderr = decode(result.stderr).strip() or "no stderr"
        raise GateError(
            "GIT_COMMAND_FAILED",
            f"git {rendered} exited {result.returncode}: {stderr}",
        )
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
V2_BUNDLE_SCHEMA_VERSION = "hexalith.conversations.v9-authority-bundle.v1"

# Roots of trust: the tooling's own schema copies, never the evaluated
# repository's, so an evaluated candidate cannot redefine its own validator.
V2_SCHEMA_DIRECTORY = Path(__file__).resolve().parents[1] / "schemas"
V2_SCHEMA_FILES = {
    "record": "story-final-record-v2.schema.json",
    "failure": "story-record-generator-failure-v1.schema.json",
    "contract": "v9-story-contract-v1.schema.json",
    "bundle": "v9-authority-bundle-v1.schema.json",
}
V2_AUTHORITY_BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V2_GENERATOR_PATH = "_bmad/scripts/generate_story_record.py"
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
    "ARGUMENT_INVALID": "FAIL",
    "CALLER_AUTHORED_FACT": "FAIL",
    "INPUT_SCHEMA_INVALID": "FAIL",
    "RECORD_NOT_DERIVED": "FAIL",
    "ASSERTION_LEDGER_EMPTY": "FAIL",
    "AUTHORITY_BINDING_INVALID": "FAIL",
    "GITLINK_INVENTORY_DRIFT": "FAIL",
    "WORKTREE_NOT_CLEAN": "FAIL",
    "SCENARIO_COMMAND_UNSUPPORTED": "FAIL",
    "SCENARIO_RESULT_MISMATCH": "FAIL",
    "TEST_RESULTS_MISSING": "FAIL",
    "TEST_RESULTS_STALE": "FAIL",
    "TEST_RESULTS_FAILED": "FAIL",
    "TEST_SKIP_NOT_ALLOWED": "FAIL",
    "TEST_COUNT_INCONSISTENT": "FAIL",
    "OUTPUT_PATH_INVALID": "FAIL",
    "OUTPUT_SCHEMA_INVALID": "FAIL",
    "RECORD_CONTENT_DRIFT": "FAIL",
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
        if name not in V2_ACCEPTED_OPTIONS:
            findings.append(
                v2_finding(
                    "ARGUMENT_INVALID",
                    "argv",
                    "an unrecognized option was supplied; the v2 route accepts exactly "
                    + ", ".join(V2_ACCEPTED_OPTIONS),
                )
            )
            if takes_following:
                index += 1
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

    for required in ("--contract", "--output-json", "--output-markdown"):
        if required not in values and not any(
            item["subject"] == required for item in findings
        ):
            findings.append(
                v2_finding("ARGUMENT_INVALID", required, f"{required} is required")
            )
    output_format = values.get("--format", "bundle")
    if output_format not in V2_FORMATS:
        findings.append(
            v2_finding(
                "ARGUMENT_INVALID",
                "--format",
                "the v2 route supports only --format bundle",
            )
        )
    if findings:
        raise V2Stop(findings)
    values.setdefault("--format", "bundle")
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
    content: bytes | None, contract_path: str, validator: Any
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
    if not isinstance(contract, dict) or contract.get("schemaVersion") != (
        V2_CONTRACT_SCHEMA_VERSION
    ):
        raise V2Stop(
            [
                v2_finding(
                    "INPUT_SCHEMA_INVALID",
                    contract_path,
                    "the story contract does not carry the known schema identity "
                    f"{V2_CONTRACT_SCHEMA_VERSION}",
                )
            ]
        )
    errors = v2_schema_errors(validator, contract)
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
    repository: Path, candidate: str, findings: list[dict[str, str]]
) -> list[dict[str, str]]:
    """Raw root gitlinks, required to equal the ordinal root `.gitmodules` inventory."""
    try:
        declared = root_submodule_paths(repository, candidate)
    except GateError as error:
        if error.code != "INVALID_SCOPE":
            raise
        findings.append(
            v2_finding(
                "GITLINK_INVENTORY_DRIFT",
                ".gitmodules",
                "the candidate .gitmodules declares a path outside references/ or with no value",
            )
        )
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
            v2_finding("GITLINK_INVENTORY_DRIFT", "candidate.gitlinks", "; ".join(details))
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
        return v2_parse_arguments(tokens[2:])
    except V2Stop:
        return None


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
    lines = [
        f"# Story {record['storyId']} Final Record",
        "",
        "<!-- hexalith.conversations.story-final-record.v2 markdown projection -->",
        "",
        "Generated by `_bmad/scripts/generate_story_record.py` from the committed "
        "candidate and measured JUnit results. The JSON record is authoritative; "
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
                "| Assertion | Subject | State |",
                "| --- | --- | --- |",
            ]
        )
        for entry in scenario.get("assertionLedger", []):
            lines.append(
                f"| {code(entry['id'])} | {code(entry['subject'])} | {code(entry['state'])} |"
            )
    lines.extend(["", "## Fault injection", ""])
    faults = record["faultInjection"]["results"]
    if faults:
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


def v2_self_ledger(scenario_id: str) -> list[dict[str, str]]:
    """The generator's own evaluated assertions for the self-invocation scenario."""
    subjects = (
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
    return [
        {"id": f"{scenario_id}#{ordinal:04d}", "subject": subject, "state": "PASS"}
        for ordinal, subject in enumerate(subjects, start=1)
    ]


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

    contract = v2_validate_contract(
        v2_committed_blob(repository, candidate, contract_path),
        contract_path,
        validators["contract"],
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
    gitlinks = v2_gitlinks(repository, candidate, findings)
    gitlink_paths = [item["path"] for item in gitlinks]
    if not gitlink_paths:
        try:
            gitlink_paths = root_submodule_paths(repository, candidate)
        except GateError as error:
            if error.code != "INVALID_SCOPE":
                raise

    # Classify every scenario command before touching a result file.
    pytest_scenarios: list[tuple[dict[str, Any], dict[str, str | None]]] = []
    self_scenarios: list[dict[str, Any]] = []
    for position, scenario in enumerate(contract["scenarios"]):
        try:
            tokens = shlex.split(scenario["command"])
        except ValueError:
            tokens = []
        pytest_command = v2_pytest_command(tokens)
        generator_command = None if pytest_command else v2_generator_command(tokens)
        if pytest_command is not None:
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
        elif generator_command is not None:
            if position != len(contract["scenarios"]) - 1 or self_scenarios:
                findings.append(
                    v2_finding(
                        "SCENARIO_COMMAND_UNSUPPORTED",
                        scenario["id"],
                        "the generator self-invocation must be the single final scenario",
                    )
                )
                continue
            if (
                generator_command.get("--repository") != "."
                or generator_command.get("--contract") != contract_path
                or generator_command.get("--output-json") != output_json
                or generator_command.get("--output-markdown") != output_markdown
                or generator_command.get("--format") != options["--format"]
            ):
                findings.append(
                    v2_finding(
                        "SCENARIO_RESULT_MISMATCH",
                        scenario["id"],
                        "this invocation is not the contract's declared self-invocation",
                    )
                )
                continue
            self_scenarios.append(scenario)
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

    for label, path in (("--output-json", output_json), ("--output-markdown", output_markdown)):
        if v2_below(path, gitlink_paths) or path in junit_paths or path == contract_path:
            findings.append(
                v2_finding(
                    "OUTPUT_PATH_INVALID",
                    label,
                    "an output path may not lie below a gitlink or alias an input",
                )
            )

    allowed_dirt = {output_json, output_markdown, *junit_paths}
    dirt = sorted(set(worktree_path_status(repository)) - allowed_dirt)
    if dirt:
        findings.append(
            v2_finding(
                "WORKTREE_NOT_CLEAN",
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
    ledger_rows = sum(
        len(item.get("assertionLedger", [])) for item in scenario_records.values()
    )
    if pytest_scenarios and parsed_results == 0:
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
        "assertionLedger": v2_self_ledger(self_scenario["id"]),
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
