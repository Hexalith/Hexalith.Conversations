# /// script
# requires-python = ">=3.11"
# dependencies = ["pytest>=8.0"]
# ///
"""Hermetic tests for the submodule promotion completion checker."""

import hashlib
import json
import os
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
AUTHORIZED_BOUNDARY_EXCEPTIONS = {
    "_bmad-output/planning-artifacts/architecture.md",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md",
    "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
    # Undeclared-but-clean root gitlink bumps disclosed by the 2026-07-27 code review
    # (Boundary Confirmation correction): the checker's own UNDECLARED_GITLINK_CHANGE
    # warning is the live authority for these; this allowlist only keeps the story's
    # own File List check from flagging what has already been disclosed and reviewed.
    "references/Hexalith.Builds",
    "references/Hexalith.EventStore",
    "references/Hexalith.Memories",
    "references/Hexalith.Tenants",
}
EXPECTED_STORY_FILES = {
    "_bmad/scripts/verify_submodule_promotion.py",
    "_bmad/scripts/tests/test_verify_submodule_promotion.py",
    ".agents/skills/bmad-create-story/SKILL.md",
    ".agents/skills/bmad-create-story/template.md",
    ".agents/skills/bmad-code-review/steps/step-04-present.md",
    ".agents/skills/bmad-dev-auto/spec-template.md",
    ".agents/skills/bmad-dev-auto/step-02-plan.md",
    ".agents/skills/bmad-dev-auto/step-04-review.md",
    ".agents/skills/bmad-dev-story/SKILL.md",
    ".agents/skills/bmad-dev-story/checklist.md",
    ".agents/skills/bmad-quick-dev/spec-template.md",
    ".agents/skills/bmad-quick-dev/step-02-plan.md",
    ".agents/skills/bmad-quick-dev/step-05-present.md",
    ".agents/skills/bmad-quick-dev/step-oneshot.md",
    ".claude/skills/bmad-create-story/SKILL.md",
    ".claude/skills/bmad-create-story/template.md",
    ".claude/skills/bmad-code-review/steps/step-04-present.md",
    ".claude/skills/bmad-dev-auto/spec-template.md",
    ".claude/skills/bmad-dev-auto/step-02-plan.md",
    ".claude/skills/bmad-dev-auto/step-04-review.md",
    ".claude/skills/bmad-dev-story/SKILL.md",
    ".claude/skills/bmad-dev-story/checklist.md",
    ".claude/skills/bmad-quick-dev/spec-template.md",
    ".claude/skills/bmad-quick-dev/step-02-plan.md",
    ".claude/skills/bmad-quick-dev/step-05-present.md",
    ".claude/skills/bmad-quick-dev/step-oneshot.md",
    "docs/runbooks/submodule-promotion-completion-gate.md",
    "_bmad-output/implementation-artifacts/6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md",
    "_bmad-output/implementation-artifacts/epic-6-context.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
    "_bmad-output/planning-artifacts/architecture.md",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-26.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27.md",
    "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
    "references/Hexalith.Builds",
    "references/Hexalith.EventStore",
    "references/Hexalith.Memories",
    "references/Hexalith.Tenants",
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


def story_file_list() -> list[str]:
    content = STORY_FILE.read_text(encoding="utf-8")
    section = content.split("### File List", 1)[1].split("### Boundary Confirmation", 1)[0]
    return [line.split("`", 2)[1] for line in section.splitlines() if line.startswith("- `")]


def boundary_violations(paths: list[str]) -> list[str]:
    forbidden_prefixes = ("references/", "src/", "tests/", "docs/release-evidence/")
    forbidden_exact = {
        "Hexalith.Conversations.slnx",
        "_bmad-output/planning-artifacts/architecture.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md",
        "docs/release-evidence/success-metric-report-and-attestation-v1.json",
        "docs/release-evidence/success-metric-report-and-attestation-v1.md",
    }
    return [
        path
        for path in paths
        if (path.startswith(forbidden_prefixes) or path in forbidden_exact)
        and path not in AUTHORIZED_BOUNDARY_EXCEPTIONS
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
    assert warning_codes(result) == {"UNRELATED_SUBMODULE_DIRTY"}


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
        ("--submodule", "references/Example", "--submodule", "references/Example"),
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

    assert result.returncode == 0
    assert warning_codes(result) == {"BASELINE_NOT_PROVIDED"}


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
    assert warning_codes(result) == {"UNRELATED_SUBMODULE_INSPECTION_FAILED"}


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


def test_completion_workflows_gate_before_success_status_writes() -> None:
    contracts = {
        "bmad-code-review/steps/step-04-present.md": (
            "#### Promotion completion gate",
            "If `promotion_gate_failed` is not true",
            "set `{new_status}` = `done`",
        ),
        "bmad-quick-dev/step-05-present.md": (
            "### Prepare Committed Candidate",
            "### Promotion Completion Gate",
            "### Mark Spec Done and Synchronize",
            "### Commit Completion Record and Open",
        ),
        "bmad-quick-dev/step-oneshot.md": (
            "### Capture Baseline and Promotion Scope",
            "status: 'in-review'",
            "### Commit Candidate",
            "### Promotion Completion Gate",
            "### Complete Trace and Commit Completion Record",
        ),
        "bmad-dev-auto/step-04-review.md": (
            "commit every file in the reviewed diff",
            "### Promotion Completion Gate",
            "Capture `final_revision`",
            "frontmatter `status: done`",
        ),
        "bmad-dev-story/SKILL.md": (
            "verify_submodule_promotion.py",
            "Update the story Status to: \"review\"",
        ),
    }
    for relative_path, markers in contracts.items():
        agent_content = (WORKSPACE / ".agents/skills" / relative_path).read_text(encoding="utf-8")
        claude_content = (WORKSPACE / ".claude/skills" / relative_path).read_text(encoding="utf-8")
        assert agent_content == claude_content
        assert ordering_violations(agent_content, markers) == []


def test_workflow_contract_check_catches_removed_gate() -> None:
    content = (WORKSPACE / ".agents/skills/bmad-dev-auto/step-04-review.md").read_text(
        encoding="utf-8"
    )
    mutated = content.replace("### Promotion Completion Gate", "### Removed Gate", 1)

    violations = ordering_violations(
        mutated,
        (
            "commit every file in the reviewed diff",
            "### Promotion Completion Gate",
            "Capture `final_revision`",
            "frontmatter `status: done`",
        ),
    )

    assert violations == ["missing marker: ### Promotion Completion Gate"]


def test_story_boundary_excludes_product_submodule_and_frozen_authority_changes() -> None:
    assert boundary_violations(story_file_list()) == []


def test_story_file_list_is_complete_and_unique() -> None:
    paths = story_file_list()

    assert len(paths) == len(set(paths))
    assert set(paths) == EXPECTED_STORY_FILES


def test_story_boundary_check_catches_reference_mutation() -> None:
    # Hexalith.Parties is a real root-declared submodule but is not one of the
    # four gitlinks the 2026-07-27 code review disclosed and authorized above,
    # so it must still be caught as an unauthorized boundary mutation.
    mutated = [*story_file_list(), "references/Hexalith.Parties"]

    assert boundary_violations(mutated) == ["references/Hexalith.Parties"]


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


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
