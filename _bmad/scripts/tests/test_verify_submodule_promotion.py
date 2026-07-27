# /// script
# requires-python = ">=3.11"
# dependencies = ["pytest>=8.0"]
# ///
"""Hermetic tests for the submodule promotion completion checker."""

import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path

import pytest


SCRIPT = Path(__file__).resolve().parents[1] / "verify_submodule_promotion.py"
WORKSPACE = SCRIPT.parents[2]
STORY_FILE = WORKSPACE / "_bmad-output/implementation-artifacts/6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md"
SIGNED_RUNBOOK = WORKSPACE / "docs/release-evidence/promote-adopt-runbook.md"
OPERATIONAL_RUNBOOK = WORKSPACE / "docs/runbooks/submodule-promotion-completion-gate.md"
SIGNED_RUNBOOK_SHA256 = "2ae308e82f159b3f152077d6946ff220108266806668c6dfb0921f3df0920ce1"
# Non-`references/` boundary exceptions carried by Jerome's explicit expanded-scope
# authorization (see the story's Dev Notes -> Testing requirements addendum). This
# set is deliberately NOT allowed to contain `references/` paths: submodule scope is
# authorized through the story's own `submodule_promotions` declaration instead, so
# an undeclared gitlink in the File List still fails the boundary check.
AUTHORIZED_BOUNDARY_EXCEPTIONS = {
    "_bmad-output/planning-artifacts/architecture.md",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md",
    "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
}
GIT_ENV = {
    **os.environ,
    "GIT_AUTHOR_NAME": "Fixture Author",
    "GIT_AUTHOR_EMAIL": "fixture-author@example.invalid",
    "GIT_COMMITTER_NAME": "Fixture Committer",
    "GIT_COMMITTER_EMAIL": "fixture-committer@example.invalid",
    "GIT_CONFIG_GLOBAL": os.devnull,
    "GIT_CONFIG_NOSYSTEM": "1",
    "GIT_TERMINAL_PROMPT": "0",
}


def run_git(repository: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            "git",
            "-c",
            "init.defaultBranch=main",
            "-c",
            "commit.gpgsign=false",
            "-c",
            "protocol.file.allow=always",
            "-C",
            str(repository),
            *arguments,
        ],
        check=True,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=20,
    )


def commit_all(repository: Path, message: str = "fixture") -> str:
    run_git(repository, "add", "--all")
    run_git(repository, "commit", "-m", message)
    return run_git(repository, "rev-parse", "HEAD").stdout.strip()


def commit_index(repository: Path, message: str = "fixture") -> str:
    run_git(repository, "commit", "-m", message)
    return run_git(repository, "rev-parse", "HEAD").stdout.strip()


def create_source(path: Path, content: str = "captured\n") -> Path:
    path.mkdir()
    run_git(path, "init")
    (path / "tracked.txt").write_text(content, encoding="utf-8")
    commit_all(path)
    return path


def create_umbrella(tmp_path: Path, submodule_path: str = "references/Example") -> tuple[Path, str]:
    source = tmp_path / "source"
    create_source(source)
    origin = tmp_path / "origin.git"
    origin.mkdir()
    run_git(origin, "init", "--bare")
    run_git(source, "remote", "add", "origin", str(origin))
    run_git(source, "push", "--set-upstream", "origin", "main")

    umbrella = tmp_path / "umbrella"
    umbrella.mkdir()
    run_git(umbrella, "init")
    run_git(
        umbrella,
        "submodule",
        "add",
        str(origin),
        submodule_path,
    )
    candidate = commit_all(umbrella)
    return umbrella, candidate


@pytest.fixture
def clean_umbrella(tmp_path: Path) -> tuple[Path, str]:
    return create_umbrella(tmp_path)


def run_checker(
    repository: Path,
    *arguments: str,
    environment: dict[str, str] | None = None,
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(repository),
            "--format",
            "json",
            *arguments,
        ],
        check=False,
        capture_output=True,
        env=environment or GIT_ENV,
        text=True,
        timeout=20,
    )


def payload(result: subprocess.CompletedProcess[str]) -> dict:
    return json.loads(result.stdout)


def blocker_codes(result: subprocess.CompletedProcess[str]) -> set[str]:
    return {item["code"] for item in payload(result)["blockers"]}


def warning_codes(result: subprocess.CompletedProcess[str]) -> set[str]:
    return {item["code"] for item in payload(result)["warnings"]}


def ordering_violations(content: str, markers: tuple[str, ...]) -> list[str]:
    violations: list[str] = []
    previous = -1
    for marker in markers:
        position = content.find(marker)
        if position < 0:
            violations.append(f"missing marker: {marker}")
        elif position <= previous:
            violations.append(f"out-of-order marker: {marker}")
        previous = max(previous, position)
    return violations


def story_frontmatter() -> str:
    return STORY_FILE.read_text(encoding="utf-8").split("---", 2)[1]


def story_baseline_commit() -> str:
    match = re.search(r"^baseline_commit:\s*'([0-9a-f]{40})'", story_frontmatter(), re.MULTILINE)
    assert match is not None, "story frontmatter must carry a 40-character baseline_commit"
    return match.group(1)


def story_declared_promotions() -> set[str]:
    """Root paths the story itself declares as promotion-bearing scope."""
    block = story_frontmatter().split("submodule_promotions:", 1)
    if len(block) == 1:
        return set()
    declared: set[str] = set()
    for line in block[1].splitlines()[1:]:
        if line and not line[0].isspace():
            break
        match = re.match(r"\s*-\s*path:\s*['\"]?([^'\"\s]+)['\"]?", line)
        if match:
            declared.add(match.group(1))
    return declared


def story_file_list() -> list[str]:
    content = STORY_FILE.read_text(encoding="utf-8")
    section = content.split("### File List", 1)[1].split("### Boundary Confirmation", 1)[0]
    return [line.split("`", 2)[1] for line in section.splitlines() if line.startswith("- `")]


def workspace_changed_paths() -> set[str]:
    """Every path this story actually changed, straight from git.

    Diffing the story's own baseline against the working tree (not against HEAD)
    keeps the comparison honest while a review pass has uncommitted edits in
    flight, and makes the File List check impossible to satisfy by editing a
    hand-maintained constant.
    """
    result = subprocess.run(
        ["git", "-C", str(WORKSPACE), "diff", "--name-only", story_baseline_commit()],
        check=True,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=20,
    )
    return {line for line in result.stdout.splitlines() if line}


def boundary_violations(paths: list[str]) -> list[str]:
    forbidden_prefixes = ("references/", "src/", "tests/", "docs/release-evidence/")
    forbidden_exact = {
        "Hexalith.Conversations.slnx",
        "_bmad-output/planning-artifacts/architecture.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md",
        "docs/release-evidence/success-metric-report-and-attestation-v1.json",
        "docs/release-evidence/success-metric-report-and-attestation-v1.md",
    }
    # A `references/` path is in bounds only when the story declares it as
    # promotion scope, so an undeclared gitlink is still a boundary violation.
    authorized = AUTHORIZED_BOUNDARY_EXCEPTIONS | story_declared_promotions()
    return [
        path
        for path in paths
        if (path.startswith(forbidden_prefixes) or path in forbidden_exact)
        and path not in authorized
    ]


def test_clean_captured_submodule_passes(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
        "--require-remote",
        "references/Example",
    )

    document = payload(result)
    assert result.returncode == 0, result.stderr or document
    assert document["result"] == "pass"
    assert document["blockers"] == []
    assert document["evaluated"][0]["recorded_mode"] == "160000"
    assert document["evaluated"][0]["remote_available"] is True


def test_require_remote_without_declared_path_is_invalid(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella

    result = run_checker(
        repository,
        "--candidate",
        candidate,
        "--require-remote",
        "references/Example",
    )

    document = payload(result)
    assert result.returncode == 2
    assert document["result"] == "error"
    assert document["blockers"][0]["code"] == "INVALID_SCOPE"


def test_tracked_dirt_blocks(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella
    (repository / "references/Example/tracked.txt").write_text("dirty\n", encoding="utf-8")

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert blocker_codes(result) == {"SUBMODULE_DIRTY_TRACKED"}


def test_untracked_dirt_blocks(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella
    (repository / "references/Example/untracked.txt").write_text("dirty\n", encoding="utf-8")

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert blocker_codes(result) == {"SUBMODULE_DIRTY_UNTRACKED"}


def test_mismatched_candidate_gitlink_blocks(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella
    submodule = repository / "references/Example"
    (submodule / "tracked.txt").write_text("new commit\n", encoding="utf-8")
    commit_all(submodule, "new submodule commit")

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert blocker_codes(result) == {"GITLINK_COMMIT_MISMATCH"}


def test_remote_availability_failure_is_deterministic(clean_umbrella: tuple[Path, str]) -> None:
    repository, baseline = clean_umbrella
    submodule = repository / "references/Example"
    (submodule / "tracked.txt").write_text("local only\n", encoding="utf-8")
    commit_all(submodule, "local-only submodule commit")
    run_git(repository, "add", "references/Example")
    candidate = commit_index(repository, "capture local-only gitlink")

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
        "--require-remote",
        "references/Example",
    )

    assert result.returncode == 1
    assert blocker_codes(result) == {"REMOTE_COMMIT_UNAVAILABLE"}


def test_changed_but_undeclared_gitlink_is_evaluated_and_warns(
    clean_umbrella: tuple[Path, str],
) -> None:
    repository, baseline = clean_umbrella
    submodule = repository / "references/Example"
    (submodule / "tracked.txt").write_text("captured update\n", encoding="utf-8")
    commit_all(submodule, "captured update")
    run_git(repository, "add", "references/Example")
    candidate = commit_index(repository, "capture updated gitlink")

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
    )

    document = payload(result)
    assert result.returncode == 0, document
    assert warning_codes(result) == {"UNDECLARED_GITLINK_CHANGE"}
    assert document["changed_gitlinks"] == ["references/Example"]
    assert document["evaluated"][0]["path"] == "references/Example"


def test_changed_but_undeclared_mismatched_gitlink_still_blocks(
    clean_umbrella: tuple[Path, str],
) -> None:
    repository, baseline = clean_umbrella
    submodule = repository / "references/Example"
    (submodule / "tracked.txt").write_text("candidate update\n", encoding="utf-8")
    commit_all(submodule, "candidate submodule commit")
    run_git(repository, "add", "references/Example")
    candidate = commit_index(repository, "capture candidate gitlink")
    (submodule / "tracked.txt").write_text("uncaptured update\n", encoding="utf-8")
    commit_all(submodule, "uncaptured submodule commit")

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
    )

    assert result.returncode == 1
    assert blocker_codes(result) == {"GITLINK_COMMIT_MISMATCH"}
    assert "UNDECLARED_GITLINK_CHANGE" in warning_codes(result)


def test_unrelated_dirty_submodule_warns_without_blocking(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella
    (repository / "references/Example/tracked.txt").write_text("unrelated dirt\n", encoding="utf-8")

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
    )

    assert result.returncode == 0
    assert blocker_codes(result) == set()
    # Self-comparing baseline with no declaration evaluates nothing, so the
    # unevaluated-scope signal is expected alongside the unrelated-dirt warning.
    assert warning_codes(result) == {"UNRELATED_SUBMODULE_DIRTY", "SCOPE_NOT_EVALUATED"}


def test_uninitialized_submodule_does_not_fall_back_to_umbrella(
    clean_umbrella: tuple[Path, str],
) -> None:
    repository, candidate = clean_umbrella
    run_git(repository, "submodule", "deinit", "--force", "references/Example")

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert blocker_codes(result) == {"SUBMODULE_NOT_INITIALIZED"}


def test_candidate_with_no_gitlink_entry_blocks(clean_umbrella: tuple[Path, str]) -> None:
    repository, baseline = clean_umbrella
    run_git(repository, "rm", "--cached", "--force", "references/Example")
    candidate = commit_index(repository, "remove candidate gitlink")

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert "GITLINK_MISSING_IN_CANDIDATE" in blocker_codes(result)


def test_candidate_mode_is_parsed_as_a_field(clean_umbrella: tuple[Path, str]) -> None:
    repository, baseline = clean_umbrella
    marker = repository / "mode-160000-marker.txt"
    marker.write_text("the filename and content contain 160000\n", encoding="utf-8")
    blob = run_git(repository, "hash-object", "-w", str(marker)).stdout.strip()
    run_git(
        repository,
        "update-index",
        "--add",
        "--cacheinfo",
        "100644",
        blob,
        "references/Example",
    )
    candidate = commit_index(repository, "replace gitlink with blob mode")

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert "GITLINK_MODE_NOT_160000" in blocker_codes(result)
    assert payload(result)["evaluated"][0]["recorded_mode"] == "100644"


@pytest.mark.parametrize(
    ("option", "revision", "expected_code"),
    [
        ("--baseline", "missing-baseline", "BASELINE_UNRESOLVABLE"),
        ("--candidate", "missing-candidate", "CANDIDATE_UNRESOLVABLE"),
    ],
)
def test_unresolvable_history_is_an_error(
    clean_umbrella: tuple[Path, str],
    option: str,
    revision: str,
    expected_code: str,
) -> None:
    repository, candidate = clean_umbrella
    arguments = ["--baseline", candidate, "--candidate", candidate]
    arguments[arguments.index(option) + 1] = revision

    result = run_checker(repository, *arguments)

    assert result.returncode == 2
    assert blocker_codes(result) == {expected_code}


@pytest.mark.parametrize(
    "arguments",
    [
        ("--submodule", "/absolute/path"),
        ("--submodule", "../outside"),
        ("--submodule", "."),
        ("--submodule", ".."),
        ("--submodule", "references/Example/"),
        ("--submodule", "references/Example", "--submodule", "references/Example"),
        (
            "--submodule",
            "references/Example",
            "--require-remote",
            "references/Example",
            "--require-remote",
            "references/Example",
        ),
    ],
)
def test_invalid_scope_is_an_error(
    clean_umbrella: tuple[Path, str],
    arguments: tuple[str, ...],
) -> None:
    repository, candidate = clean_umbrella

    result = run_checker(repository, "--candidate", candidate, *arguments)

    assert result.returncode == 2
    assert blocker_codes(result) == {"INVALID_SCOPE"}


def test_path_not_declared_by_root_gitmodules_blocks(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Missing",
    )

    assert result.returncode == 1
    assert blocker_codes(result) == {"PATH_NOT_ROOT_DECLARED"}


def test_unresolved_submodule_head_blocks(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella
    submodule = repository / "references/Example"
    run_git(submodule, "symbolic-ref", "HEAD", "refs/heads/missing")

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert "SUBMODULE_HEAD_UNRESOLVED" in blocker_codes(result)


def create_nested_umbrella(tmp_path: Path) -> tuple[Path, str]:
    child = create_source(tmp_path / "child")
    parent = create_source(tmp_path / "parent", "parent\n")
    run_git(parent, "submodule", "add", str(child), "nested/Child")
    commit_all(parent, "add nested submodule")

    umbrella = tmp_path / "umbrella"
    umbrella.mkdir()
    run_git(umbrella, "init")
    run_git(umbrella, "submodule", "add", str(parent), "references/Example")
    candidate = commit_all(umbrella, "add root submodule")
    return umbrella, candidate


@pytest.mark.parametrize("initialize_nested", [False, True])
def test_nested_submodules_are_not_initialized_or_traversed(
    tmp_path: Path,
    initialize_nested: bool,
) -> None:
    repository, candidate = create_nested_umbrella(tmp_path)
    parent = repository / "references/Example"
    nested = parent / "nested/Child"
    if initialize_nested:
        run_git(parent, "submodule", "update", "--init", "nested/Child")
        (nested / "tracked.txt").write_text("nested dirt\n", encoding="utf-8")
    before_has_git = (nested / ".git").exists()

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 0, payload(result)
    assert (nested / ".git").exists() is before_has_git
    if initialize_nested:
        assert (nested / "tracked.txt").read_text(encoding="utf-8") == "nested dirt\n"
    else:
        assert list(nested.iterdir()) == []


def test_staged_nested_gitlink_change_counts_as_tracked_dirt(tmp_path: Path) -> None:
    repository, candidate = create_nested_umbrella(tmp_path)
    parent = repository / "references/Example"
    nested = parent / "nested/Child"
    run_git(parent, "submodule", "update", "--init", "nested/Child")
    (nested / "tracked.txt").write_text("new nested commit\n", encoding="utf-8")
    commit_all(nested, "advance nested submodule")
    run_git(parent, "add", "nested/Child")

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert "SUBMODULE_DIRTY_TRACKED" in blocker_codes(result)


def test_unicode_and_space_path_round_trips_through_null_delimited_git_output(
    tmp_path: Path,
) -> None:
    path = "references/Café Module"
    repository, candidate = create_umbrella(tmp_path, path)

    result = run_checker(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        path,
    )

    assert result.returncode == 0, payload(result)
    assert payload(result)["evaluated"][0]["path"] == path


def test_missing_baseline_warns_without_blocking_empty_scope(
    clean_umbrella: tuple[Path, str],
) -> None:
    repository, candidate = clean_umbrella

    result = run_checker(repository, "--candidate", candidate)

    document = payload(result)
    assert result.returncode == 0
    assert warning_codes(result) == {"BASELINE_NOT_PROVIDED", "SCOPE_NOT_EVALUATED"}
    # Prove detection was skipped rather than run-and-empty.
    assert document["baseline"] is None
    assert document["changed_gitlinks"] == []
    assert document["evaluated"] == []


def test_git_unavailable_is_a_stable_error(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella
    environment = {**GIT_ENV, "PATH": ""}

    result = run_checker(
        repository,
        "--candidate",
        candidate,
        environment=environment,
    )

    assert result.returncode == 2
    assert blocker_codes(result) == {"GIT_UNAVAILABLE"}


def test_renamed_declared_submodule_is_evaluated_only_at_its_new_path(
    tmp_path: Path,
) -> None:
    repository, baseline = create_umbrella(tmp_path, "references/Example")
    run_git(repository, "mv", "references/Example", "references/Renamed")
    candidate = commit_index(repository, "rename declared submodule")

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
        "--submodule",
        "references/Renamed",
    )

    document = payload(result)
    assert result.returncode == 0, document
    assert document["changed_gitlinks"] == ["references/Renamed"]
    assert blocker_codes(result) == set()
    assert document["evaluated"][0]["path"] == "references/Renamed"
    assert document["evaluated"][0]["clean"] is True


def test_baseline_not_ancestor_of_candidate_is_an_error(tmp_path: Path) -> None:
    repository, base = create_umbrella(tmp_path)
    run_git(repository, "checkout", "-q", "-b", "branch-a")
    (repository / "a.txt").write_text("a\n", encoding="utf-8")
    branch_a = commit_all(repository, "branch a")
    run_git(repository, "checkout", "-q", base)
    run_git(repository, "checkout", "-q", "-b", "branch-b")
    (repository / "b.txt").write_text("b\n", encoding="utf-8")
    branch_b = commit_all(repository, "branch b")

    result = run_checker(repository, "--baseline", branch_a, "--candidate", branch_b)

    assert result.returncode == 2
    assert blocker_codes(result) == {"BASELINE_NOT_ANCESTOR"}


def test_git_failure_inspecting_unrelated_submodule_warns_without_erroring(
    tmp_path: Path,
) -> None:
    repository, candidate = create_umbrella(tmp_path, "references/Example")
    submodule = repository / "references/Example"
    git_dir = Path(
        run_git(submodule, "rev-parse", "--git-dir").stdout.strip()
    )
    if not git_dir.is_absolute():
        git_dir = (submodule / git_dir).resolve()
    (git_dir / "index").write_bytes(os.urandom(200))

    result = run_checker(repository, "--baseline", candidate, "--candidate", candidate)

    document = payload(result)
    assert result.returncode == 0, document
    assert blocker_codes(result) == set()
    assert warning_codes(result) == {
        "UNRELATED_SUBMODULE_INSPECTION_FAILED",
        "SCOPE_NOT_EVALUATED",
    }


def test_format_equals_syntax_still_emits_json_on_argument_error() -> None:
    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--format=json", "--bogus-flag"],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=20,
    )

    assert result.returncode == 2
    document = json.loads(result.stdout)
    assert document["blockers"][0]["code"] == "INVALID_SCOPE"


def test_default_text_format_is_human_readable(clean_umbrella: tuple[Path, str]) -> None:
    repository, candidate = clean_umbrella

    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(repository),
            "--baseline",
            candidate,
            "--candidate",
            candidate,
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=20,
    )

    assert result.returncode == 0
    assert result.stdout.startswith("Submodule promotion gate: PASS\n")
    assert "BLOCKER" not in result.stdout
    with pytest.raises(json.JSONDecodeError):
        json.loads(result.stdout)


def test_decode_uses_surrogateescape_not_lossy_replacement() -> None:
    raw = b"references/broken-\xffpath"
    from importlib import util as importlib_util

    spec = importlib_util.spec_from_file_location("verify_submodule_promotion", SCRIPT)
    module = importlib_util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)

    decoded = module.decode(raw)
    assert decoded.encode("utf-8", errors="surrogateescape") == raw


def test_safe_relative_path_rejects_embedded_control_characters() -> None:
    from importlib import util as importlib_util

    spec = importlib_util.spec_from_file_location("verify_submodule_promotion", SCRIPT)
    module = importlib_util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)

    with pytest.raises(module.GateError) as excinfo:
        module.safe_relative_path("references/bad\npath")
    assert excinfo.value.code == "INVALID_SCOPE"


# Per gated workflow: (ordering markers, prohibitive clauses that must survive
# inside the gate block). Ordering alone only proves a heading exists in the
# right place -- it passes just as happily when the gate body has been replaced
# with "the gate is optional", so the enforcement language is asserted too.
WORKFLOW_GATE_CONTRACTS = {
    "bmad-code-review/steps/step-04-present.md": (
        (
            "#### Promotion completion gate",
            "If `promotion_gate_failed` is not true",
            "set `{new_status}` = `done`",
        ),
        (
            "SCOPE_NOT_EVALUATED",
            "force `{new_status}` = `in-progress`",
            "never write or synchronize `done`",
        ),
    ),
    "bmad-quick-dev/step-05-present.md": (
        (
            "### Prepare Committed Candidate",
            "### Promotion Completion Gate",
            "### Mark Spec Done and Synchronize",
            "### Commit Completion Record and Open",
        ),
        (
            "SCOPE_NOT_EVALUATED",
            "Never write `done`",
            "HALT for remediation",
        ),
    ),
    "bmad-quick-dev/step-oneshot.md": (
        (
            "### Capture Baseline and Promotion Scope",
            "status: 'in-review'",
            "### Commit Candidate",
            "### Promotion Completion Gate",
            "### Complete Trace and Commit Completion Record",
        ),
        (
            "SCOPE_NOT_EVALUATED",
            "Never write `done`",
            "HALT for remediation",
        ),
    ),
    "bmad-dev-auto/step-04-review.md": (
        (
            "commit every file in the reviewed diff",
            "### Promotion Completion Gate",
            "Capture `final_revision`",
            "frontmatter `status: done`",
        ),
        (
            "SCOPE_NOT_EVALUATED",
            "HALT with status `blocked`",
            "Never capture a successful `final_revision`",
        ),
    ),
    "bmad-dev-story/SKILL.md": (
        (
            "verify_submodule_promotion.py",
            "Update the story Status to: \"review\"",
        ),
        (
            "SCOPE_NOT_EVALUATED",
            "HALT: \"Submodule promotion completion gate failed",
            "Set story frontmatter status and Status section to `in-progress`",
        ),
    ),
}


def gate_contract_violations(
    content: str, markers: tuple[str, ...], clauses: tuple[str, ...]
) -> list[str]:
    """Ordering AND enforcement language. Ordering alone passes a gutted gate."""
    violations = ordering_violations(content, markers)
    violations.extend(f"missing enforcement clause: {clause}" for clause in clauses if clause not in content)
    return violations


def test_completion_workflows_gate_before_success_status_writes() -> None:
    for relative_path, (markers, clauses) in WORKFLOW_GATE_CONTRACTS.items():
        agent_content = (WORKSPACE / ".agents/skills" / relative_path).read_text(encoding="utf-8")
        claude_content = (WORKSPACE / ".claude/skills" / relative_path).read_text(encoding="utf-8")
        assert agent_content == claude_content, relative_path
        assert gate_contract_violations(agent_content, markers, clauses) == [], relative_path


@pytest.mark.parametrize("relative_path", sorted(WORKFLOW_GATE_CONTRACTS))
def test_workflow_contract_check_catches_removed_gate(relative_path: str) -> None:
    """Every gated workflow -- not just dev-auto -- must fail when its gate heading goes."""
    markers, clauses = WORKFLOW_GATE_CONTRACTS[relative_path]
    content = (WORKSPACE / ".agents/skills" / relative_path).read_text(encoding="utf-8")
    gate_marker = next(marker for marker in markers if "romotion" in marker)
    mutated = content.replace(gate_marker, "### Removed Gate", 1)

    assert f"missing marker: {gate_marker}" in gate_contract_violations(mutated, markers, clauses)


@pytest.mark.parametrize("relative_path", sorted(WORKFLOW_GATE_CONTRACTS))
def test_workflow_contract_check_catches_gutted_gate(relative_path: str) -> None:
    """A gate whose heading survives but whose enforcement language is gone must fail.

    This is the mutation the ordering-only contract could not see: replacing the
    body with "the gate is advisory" while keeping every heading in place.
    """
    markers, clauses = WORKFLOW_GATE_CONTRACTS[relative_path]
    content = (WORKSPACE / ".agents/skills" / relative_path).read_text(encoding="utf-8")

    for clause in clauses:
        gutted = content.replace(clause, "the gate is advisory")
        assert clause not in gutted, f"{relative_path}: {clause!r} was not fully removed"
        violations = gate_contract_violations(gutted, markers, clauses)
        assert f"missing enforcement clause: {clause}" in violations, (
            f"{relative_path}: removing {clause!r} was not detected"
        )


def test_both_skill_trees_stay_byte_identical_for_every_changed_file() -> None:
    """Parity must cover every skill file this story touched, not just the gated five."""
    skill_paths = [
        path
        for path in story_file_list()
        if path.startswith((".claude/skills/", ".agents/skills/"))
    ]
    assert skill_paths, "story must list the skill files it changed"

    for path in skill_paths:
        relative_path = path.split("/skills/", 1)[1]
        agent_file = WORKSPACE / ".agents/skills" / relative_path
        claude_file = WORKSPACE / ".claude/skills" / relative_path
        assert agent_file.read_bytes() == claude_file.read_bytes(), relative_path


def test_story_boundary_excludes_product_submodule_and_frozen_authority_changes() -> None:
    assert boundary_violations(story_file_list()) == []


def test_story_file_list_is_complete_and_unique() -> None:
    paths = story_file_list()

    assert len(paths) == len(set(paths))
    # Compared against git, never against a constant in this file: a hand-kept
    # expected set can only detect divergence between two hand-kept lists, so it
    # cannot catch a File List that omits a path the story really changed.
    assert set(paths) == workspace_changed_paths()


def test_story_boundary_check_catches_reference_mutation() -> None:
    # Hexalith.Parties is a real root-declared submodule that the story does not
    # declare in `submodule_promotions`, so it must still be caught.
    assert "references/Hexalith.Parties" not in story_declared_promotions()
    mutated = [*story_file_list(), "references/Hexalith.Parties"]

    assert boundary_violations(mutated) == ["references/Hexalith.Parties"]


def test_story_boundary_check_rejects_undeclared_gitlinks_in_the_file_list() -> None:
    """The boundary guard must fail when a gitlink is listed but not declared.

    This is the mutation that the previous allowlist made impossible: every
    gitlink the story shipped had been added to the exception set, so the
    boundary assertion compared [] against [] and could not fail.
    """
    declared = sorted(story_declared_promotions())
    assert declared, "story must declare the gitlinks it ships, or list none at all"

    listed_without_declaration = [
        path for path in story_file_list() if path.startswith("references/")
    ]
    assert sorted(listed_without_declaration) == declared

    for path in declared:
        assert path not in AUTHORIZED_BOUNDARY_EXCEPTIONS
        assert boundary_violations([path]) == []


def test_operational_runbook_preserves_signed_v1_and_exposes_the_completion_gate() -> None:
    assert hashlib.sha256(SIGNED_RUNBOOK.read_bytes()).hexdigest() == SIGNED_RUNBOOK_SHA256

    content = OPERATIONAL_RUNBOOK.read_text(encoding="utf-8")
    required_text = (
        "submodule_promotions",
        "python3 _bmad/scripts/verify_submodule_promotion.py",
        "--baseline <story-baseline-commit>",
        "--candidate <committed-umbrella-revision>",
        "Exit `0`",
        "Exit `1`",
        "Exit `2`",
        "in-progress",
        "blocked",
        "staged pointer bump",
        "prose completion note",
        "Never use recursive submodule commands",
        "8. [ ] Exact `submodule_promotions` scope recorded; remote requirements identified.",
        "9. [ ] Each affected submodule committed separately, clean, and available remotely where required.",
        "10. [ ] Root-only gitlinks committed in the umbrella repository and the mechanical completion gate passes.",
    )
    for expected in required_text:
        assert expected in content


def run_checker_text(
    repository: Path,
    *arguments: str,
    environment: dict[str, str] | None = None,
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(SCRIPT), "--repository", str(repository), *arguments],
        check=False,
        capture_output=True,
        env=environment or GIT_ENV,
        text=True,
        timeout=20,
    )


def load_checker_module():
    from importlib import util as importlib_util

    spec = importlib_util.spec_from_file_location("checker_under_test", SCRIPT)
    module = importlib_util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def advance_submodule(repository: Path, path: str = "references/Example") -> str:
    """Add a real commit inside the submodule without capturing it in the umbrella."""
    worktree = repository / path
    (worktree / "promoted.txt").write_text("promoted work\n", encoding="utf-8")
    return commit_all(worktree, "promote submodule work")


def test_zero_scope_run_reports_that_nothing_was_evaluated(
    clean_umbrella: tuple[Path, str],
) -> None:
    """A pass that verified nothing must never be silent about it."""
    repository, candidate = clean_umbrella

    result = run_checker(repository, "--candidate", candidate)

    document = payload(result)
    assert result.returncode == 0
    assert document["evaluated"] == []
    assert "SCOPE_NOT_EVALUATED" in warning_codes(result)
    assert "BASELINE_NOT_PROVIDED" in warning_codes(result)


def test_self_comparing_baseline_is_reported_as_unevaluated(
    clean_umbrella: tuple[Path, str],
) -> None:
    """--baseline X --candidate X cannot detect any change, so it proves nothing."""
    repository, candidate = clean_umbrella

    result = run_checker(repository, "--baseline", candidate, "--candidate", candidate)

    assert result.returncode == 0
    assert payload(result)["changed_gitlinks"] == []
    assert "SCOPE_NOT_EVALUATED" in warning_codes(result)


def test_declared_scope_suppresses_the_unevaluated_warning(
    clean_umbrella: tuple[Path, str],
) -> None:
    repository, candidate = clean_umbrella

    result = run_checker(
        repository, "--candidate", candidate, "--submodule", "references/Example"
    )

    assert result.returncode == 0
    assert "SCOPE_NOT_EVALUATED" not in warning_codes(result)


def test_uncaptured_promotion_blocks_even_when_undeclared(
    clean_umbrella: tuple[Path, str],
) -> None:
    """The story's goal line: an uncaptured umbrella gitlink cannot reach done."""
    repository, candidate = clean_umbrella
    promoted = advance_submodule(repository)

    result = run_checker(repository, "--baseline", candidate, "--candidate", candidate)

    document = payload(result)
    assert result.returncode == 1
    assert document["result"] == "blocked"
    assert "UNCAPTURED_SUBMODULE_PROMOTION" in blocker_codes(result)
    blocker = next(
        item for item in document["blockers"] if item["code"] == "UNCAPTURED_SUBMODULE_PROMOTION"
    )
    assert blocker["path"] == "references/Example"
    assert promoted[:8] in blocker["message"]
    assert blocker["remediation"]


def test_checkout_behind_the_gitlink_still_only_warns(tmp_path: Path) -> None:
    """A checkout that is behind is ordinary concurrent state, not a promotion."""
    repository, _ = create_umbrella(tmp_path)
    worktree = repository / "references/Example"
    original = run_git(worktree, "rev-parse", "HEAD").stdout.strip()
    advance_submodule(repository)
    candidate = commit_all(repository, "capture promoted gitlink")
    run_git(worktree, "checkout", "--detach", original)

    result = run_checker(repository, "--baseline", candidate, "--candidate", candidate)

    assert result.returncode == 0
    assert "UNRELATED_GITLINK_DRIFT" in warning_codes(result)
    assert "UNCAPTURED_SUBMODULE_PROMOTION" not in blocker_codes(result)


def test_unrelated_gitlink_drift_warns_without_blocking(tmp_path: Path) -> None:
    """AC 3d's live acceptance case, previously with zero coverage."""
    repository, candidate = create_umbrella(tmp_path)
    worktree = repository / "references/Example"
    # An unrelated commit that shares no history with the recorded gitlink is
    # neither ahead nor behind, so it must stay a warning.
    run_git(worktree, "checkout", "--orphan", "sideline")
    (worktree / "sideline.txt").write_text("unrelated\n", encoding="utf-8")
    commit_all(worktree, "unrelated sideline")

    result = run_checker(repository, "--baseline", candidate, "--candidate", candidate)

    assert result.returncode == 0
    assert payload(result)["result"] == "pass"
    assert "UNRELATED_GITLINK_DRIFT" in warning_codes(result)


def test_ambient_ignore_submodules_config_cannot_hide_a_gitlink_change(
    tmp_path: Path,
) -> None:
    """diff.ignoreSubmodules=all must not silently disable changed-gitlink detection."""
    repository, baseline = create_umbrella(tmp_path)
    advance_submodule(repository)
    candidate = commit_all(repository, "capture promoted gitlink")

    hostile_config = tmp_path / "hostile.gitconfig"
    hostile_config.write_text(
        "[diff]\n\tignoreSubmodules = all\n\trenames = false\n", encoding="utf-8"
    )
    environment = {**GIT_ENV, "GIT_CONFIG_GLOBAL": str(hostile_config)}

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
        environment=environment,
    )

    assert payload(result)["changed_gitlinks"] == ["references/Example"]
    assert "UNDECLARED_GITLINK_CHANGE" in warning_codes(result)


def test_inherited_git_dir_does_not_redirect_submodule_inspection(
    clean_umbrella: tuple[Path, str],
) -> None:
    """GIT_DIR from a hook or rebase --exec must not make submodules read the umbrella."""
    repository, candidate = clean_umbrella
    environment = {
        **GIT_ENV,
        "GIT_DIR": str(repository / ".git"),
        "GIT_WORK_TREE": str(repository),
    }

    result = run_checker(
        repository,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
        environment=environment,
    )

    document = payload(result)
    assert result.returncode == 0, document
    assert document["blockers"] == []
    assert document["evaluated"][0]["clean"] is True


def test_unusual_unrelated_filename_does_not_abort_the_run(tmp_path: Path) -> None:
    """T-3 on the diff --raw side: only gitlink records are scope inputs."""
    repository, baseline = create_umbrella(tmp_path)
    noisy = repository / "mode-160000-marker\\name.txt"
    noisy.write_text("160000 is in this filename and content\n", encoding="utf-8")
    advance_submodule(repository)
    candidate = commit_all(repository, "unrelated noisy path plus a real promotion")

    result = run_checker(
        repository,
        "--baseline",
        baseline,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    document = payload(result)
    assert result.returncode == 0, document
    # The backslash path is not a gitlink, so it is never validated as scope.
    assert document["changed_gitlinks"] == ["references/Example"]
    assert "INVALID_SCOPE" not in blocker_codes(result)


def test_missing_repository_directory_is_not_a_git_repository(tmp_path: Path) -> None:
    result = run_checker(tmp_path / "does-not-exist")

    document = payload(result)
    assert result.returncode == 2
    assert document["result"] == "error"
    assert document["blockers"][0]["code"] == "NOT_A_GIT_REPOSITORY"


def test_plain_directory_is_not_a_git_repository(tmp_path: Path) -> None:
    plain = tmp_path / "plain"
    plain.mkdir()

    result = run_checker(plain)

    assert result.returncode == 2
    assert payload(result)["blockers"][0]["code"] == "NOT_A_GIT_REPOSITORY"


def test_repository_without_gitmodules_is_an_error(tmp_path: Path) -> None:
    bare_umbrella = tmp_path / "no-gitmodules"
    bare_umbrella.mkdir()
    run_git(bare_umbrella, "init")
    (bare_umbrella / "file.txt").write_text("content\n", encoding="utf-8")
    commit_all(bare_umbrella)

    result = run_checker(bare_umbrella)

    assert result.returncode == 2
    assert payload(result)["blockers"][0]["code"] == "MISSING_GITMODULES"


def test_empty_repository_argument_is_rejected(tmp_path: Path) -> None:
    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--repository", "", "--format", "json"],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=20,
        cwd=str(tmp_path),
    )

    assert result.returncode == 2
    assert json.loads(result.stdout)["blockers"][0]["code"] == "INVALID_SCOPE"


def test_nested_root_declaration_is_rejected(tmp_path: Path) -> None:
    """A root .gitmodules entry inside another root entry would force traversal."""
    repository, _ = create_umbrella(tmp_path)
    gitmodules = repository / ".gitmodules"
    gitmodules.write_text(
        gitmodules.read_text(encoding="utf-8")
        + '[submodule "nested"]\n\tpath = references/Example/nested/Child\n\turl = ../origin.git\n',
        encoding="utf-8",
    )

    result = run_checker(repository)

    document = payload(result)
    assert result.returncode == 2
    assert document["blockers"][0]["code"] == "INVALID_SCOPE"
    assert "nested" in document["blockers"][0]["message"]


def test_git_command_failure_raises_a_stable_code_with_stderr(tmp_path: Path) -> None:
    module = load_checker_module()
    repository, _ = create_umbrella(tmp_path)

    with pytest.raises(module.GateError) as excinfo:
        module.run_git(repository, "definitely-not-a-git-subcommand")

    assert excinfo.value.code == "GIT_COMMAND_FAILED"
    # stderr must survive into the message, not be discarded.
    assert "definitely-not-a-git-subcommand" in excinfo.value.message


def test_unexpected_exception_emits_an_internal_error_document(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    """Any non-GateError must still produce a parseable document, never a traceback."""
    module = load_checker_module()
    repository, _ = create_umbrella(tmp_path)

    def explode(_args):
        raise RuntimeError("synthetic failure")

    monkeypatch.setattr(module, "verify", explode)
    exit_code = module.main(["--repository", str(repository), "--format", "json"])

    document = json.loads(capsys.readouterr().out)
    assert exit_code == 2
    assert document["result"] == "error"
    assert document["blockers"][0]["code"] == "INTERNAL_ERROR"
    assert "synthetic failure" in document["blockers"][0]["message"]


def test_text_output_renders_blockers_warnings_and_remediation(tmp_path: Path) -> None:
    """AC 3a requires actionable text; this is the only test that renders it."""
    repository, candidate = create_umbrella(tmp_path)
    (repository / "references/Example/tracked.txt").write_text("dirty\n", encoding="utf-8")
    (repository / "references/Example/stray.txt").write_text("stray\n", encoding="utf-8")

    result = run_checker_text(
        repository,
        "--baseline",
        candidate,
        "--candidate",
        candidate,
        "--submodule",
        "references/Example",
    )

    assert result.returncode == 1
    assert result.stdout.startswith("Submodule promotion gate: BLOCKED\n")
    assert "Declared: references/Example" in result.stdout
    assert "Evaluated: references/Example" in result.stdout
    assert "BLOCKER [SUBMODULE_DIRTY_TRACKED] (references/Example):" in result.stdout
    assert "BLOCKER [SUBMODULE_DIRTY_UNTRACKED] (references/Example):" in result.stdout
    assert "Remediation: Commit or otherwise resolve the tracked submodule changes" in result.stdout


def test_text_output_distinguishes_an_unevaluated_pass(clean_umbrella: tuple[Path, str]) -> None:
    """A vacuous pass must not read the same as a verified promotion."""
    repository, candidate = clean_umbrella

    vacuous = run_checker_text(repository, "--candidate", candidate)
    verified = run_checker_text(
        repository, "--candidate", candidate, "--submodule", "references/Example"
    )

    assert "Evaluated: none" in vacuous.stdout
    assert "WARNING [SCOPE_NOT_EVALUATED]" in vacuous.stdout
    assert "Evaluated: references/Example" in verified.stdout
    assert vacuous.stdout != verified.stdout


def test_surrogate_escaped_output_does_not_crash_strict_stdout(tmp_path: Path) -> None:
    """A non-UTF-8 byte in a diagnostic must not turn exit 2 into an empty exit 1."""
    module = load_checker_module()
    repository, _ = create_umbrella(tmp_path)
    environment = {**GIT_ENV, "PYTHONIOENCODING": "utf-8"}
    broken = b"references/broken-\xffpath".decode("utf-8", errors="surrogateescape")

    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(repository),
            "--submodule",
            broken,
            "--format",
            "json",
        ],
        check=False,
        capture_output=True,
        env=environment,
        timeout=20,
    )

    assert result.returncode == 1
    assert result.stdout, "an unwritable diagnostic must never produce empty stdout"
    assert b"Traceback" not in result.stderr
    document = json.loads(result.stdout.decode("utf-8", errors="surrogateescape"))
    assert document["blockers"][0]["code"] == "PATH_NOT_ROOT_DECLARED"
    assert module.SCHEMA == document["schema"]


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
