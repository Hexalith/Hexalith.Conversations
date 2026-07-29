# /// script
# requires-python = ">=3.11"
# dependencies = ["pytest>=8.0"]
# ///
"""Hermetic tests for the story final-record generator."""

import ast
import hashlib
import json
import os
import re
import subprocess
import sys
from importlib import util as importlib_util
from pathlib import Path

import pytest


SCRIPT = Path(__file__).resolve().parents[1] / "generate_story_record.py"
WORKSPACE = SCRIPT.parents[2]
STORY_FILE = (
    WORKSPACE
    / "_bmad-output/implementation-artifacts"
    / "6-8-generate-the-final-story-record-mechanically-from-measured-state.md"
)
RUNBOOK = WORKSPACE / "docs/runbooks/story-final-record-generation.md"
TRX_NAMESPACE = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"

# Every completion surface AC5 names, in both published skill trees. AC6 requires
# that the invocation cannot be silently removed from any of them.
GENERATOR_WORKFLOWS = (
    "bmad-dev-story/SKILL.md",
    "bmad-quick-dev/step-05-present.md",
    "bmad-quick-dev/step-oneshot.md",
    "bmad-code-review/steps/step-04-present.md",
)
GIT_ENVIRONMENT_OVERRIDES = {
    "GIT_DIR",
    "GIT_WORK_TREE",
    "GIT_INDEX_FILE",
    "GIT_OBJECT_DIRECTORY",
    "GIT_COMMON_DIR",
    "GIT_ALTERNATE_OBJECT_DIRECTORIES",
    "GIT_NAMESPACE",
}


def fixture_git_environment() -> dict[str, str]:
    environment = dict(os.environ)
    for name in GIT_ENVIRONMENT_OVERRIDES:
        environment.pop(name, None)
    environment.update(
        {
            "GIT_AUTHOR_NAME": "Fixture Author",
            "GIT_AUTHOR_EMAIL": "fixture-author@example.invalid",
            "GIT_COMMITTER_NAME": "Fixture Committer",
            "GIT_COMMITTER_EMAIL": "fixture-committer@example.invalid",
            "GIT_CONFIG_GLOBAL": os.devnull,
            "GIT_CONFIG_NOSYSTEM": "1",
            "GIT_TERMINAL_PROMPT": "0",
        }
    )
    return environment


GIT_ENV = fixture_git_environment()


def load_generator():
    spec = importlib_util.spec_from_file_location("generate_story_record", SCRIPT)
    module = importlib_util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


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
        timeout=30,
    )


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_trx(
    path: Path,
    passed: int = 3,
    failed: int = 0,
    skipped: int = 0,
    project: str = "Fixture",
) -> None:
    """Write a TRX whose summary agrees with the results it contains."""
    results = [
        f'<UnitTestResult testName="{project}.P{index}" outcome="Passed" />'
        for index in range(passed)
    ]
    results += [
        f'<UnitTestResult testName="{project}.F{index}" outcome="Failed" />'
        for index in range(failed)
    ]
    results += [
        f'<UnitTestResult testName="{project}.S{index}" outcome="NotExecuted" />'
        for index in range(skipped)
    ]
    executed = passed + failed
    total = executed + skipped
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        '<?xml version="1.0" encoding="utf-8"?>\n'
        f'<TestRun id="fixture" name="fixture" xmlns="{TRX_NAMESPACE}">\n'
        "  <Results>\n    " + "\n    ".join(results) + "\n  </Results>\n"
        "  <TestDefinitions>\n"
        f'    <UnitTest name="fixture"><TestMethod codeBase="/fixture/{project}.dll" /></UnitTest>\n'
        "  </TestDefinitions>\n"
        '  <ResultSummary outcome="Completed">\n'
        f'    <Counters total="{total}" executed="{executed}" passed="{passed}" '
        f'failed="{failed}" error="0" timeout="0" aborted="0" inconclusive="0" '
        f'notRunnable="0" notExecuted="{skipped}" disconnected="0" />\n'
        "  </ResultSummary>\n"
        "</TestRun>\n",
        encoding="utf-8",
    )


STORY_TEMPLATE = """---
story_key: 'fixture-story'
status: 'in-progress'
baseline_commit: '{baseline}'
---

# Fixture Story

## Dev Agent Record

### File List

### Boundary Confirmation

Fixture.
"""


def build_umbrella(tmp_path: Path) -> dict[str, object]:
    """A root-only umbrella with one declared submodule, a story record and a TRX."""
    source = tmp_path / "source"
    source.mkdir()
    run_git(source, "init")
    (source / "tracked.txt").write_text("captured\n", encoding="utf-8")
    run_git(source, "add", "--all")
    run_git(source, "commit", "-m", "source")

    origin = tmp_path / "origin.git"
    origin.mkdir()
    run_git(origin, "init", "--bare")
    run_git(source, "remote", "add", "origin", str(origin))
    run_git(source, "push", "--set-upstream", "origin", "main")

    umbrella = tmp_path / "umbrella"
    umbrella.mkdir()
    run_git(umbrella, "init")
    (umbrella / "seed.txt").write_text("seed\n", encoding="utf-8")
    (umbrella / "_bmad-output/implementation-artifacts").mkdir(parents=True)
    (umbrella / "tests/Fixture").mkdir(parents=True)
    (umbrella / "tests/Fixture/Fixture.csproj").write_text(
        "<Project />\n", encoding="utf-8"
    )
    (umbrella / "Fixture.slnx").write_text(
        '<Solution><Folder Name="/tests/"><Project Path="tests/Fixture/Fixture.csproj" />'
        "</Folder></Solution>\n",
        encoding="utf-8",
    )
    run_git(umbrella, "add", "--all")
    run_git(umbrella, "commit", "-m", "seed")
    run_git(umbrella, "submodule", "add", str(source), "references/Example")
    run_git(umbrella, "commit", "-m", "add submodule")
    baseline = run_git(umbrella, "rev-parse", "HEAD").stdout.strip()

    story = "_bmad-output/implementation-artifacts/fixture-story.md"
    (umbrella / story).write_text(
        STORY_TEMPLATE.format(baseline=baseline), encoding="utf-8"
    )
    (umbrella / "changed.txt").write_text("changed\n", encoding="utf-8")
    run_git(umbrella, "add", story, "changed.txt")
    run_git(umbrella, "commit", "-m", "story and change")
    candidate = run_git(umbrella, "rev-parse", "HEAD").stdout.strip()

    artifact = "results/fixture.trx"
    write_trx(umbrella / artifact)
    return {
        "repository": umbrella,
        "baseline": baseline,
        "candidate": candidate,
        "story": story,
        "artifact": artifact,
    }


@pytest.fixture()
def umbrella(tmp_path: Path) -> dict[str, object]:
    return build_umbrella(tmp_path)


def invoke(fixture: dict[str, object], *extra: str) -> tuple[int, dict]:
    """Run the generator as a subprocess and parse its document."""
    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(fixture["repository"]),
            "--story",
            str(fixture["story"]),
            "--format",
            "json",
            *extra,
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=120,
    )
    return result.returncode, json.loads(result.stdout)


def codes(document: dict) -> set[str]:
    return {item["code"] for item in document["blockers"]}


def measured(fixture: dict[str, object], *extra: str) -> tuple[int, dict]:
    return invoke(fixture, "--test-results", f"Fixture={fixture['artifact']}", *extra)


def bundled(fixture: dict[str, object], *extra: str) -> tuple[int, dict]:
    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(fixture["repository"]),
            "--story",
            str(fixture["story"]),
            "--format",
            "bundle",
            "--test-results",
            f"Fixture={fixture['artifact']}",
            *extra,
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=120,
    )
    return result.returncode, json.loads(result.stdout)


def insert_generated_block(fixture: dict[str, object], block: str) -> None:
    story_file = fixture["repository"] / fixture["story"]
    content = story_file.read_text(encoding="utf-8")
    module = load_generator()
    anchor, start, end = module.record_anchor(content)
    assert anchor is not None
    story_file.write_text(content[:start] + block + content[end:], encoding="utf-8")


def set_file_list(fixture: dict[str, object], paths: list[str]) -> None:
    story_file = fixture["repository"] / fixture["story"]
    bullets = "\n".join(f"- `{path}` (modified)" for path in paths)
    content = story_file.read_text(encoding="utf-8").replace(
        "### File List\n\n### Boundary Confirmation",
        f"### File List\n\n{bullets}\n\n### Boundary Confirmation",
    )
    story_file.write_text(content, encoding="utf-8")


def add_test_project(fixture: dict[str, object], name: str) -> None:
    repository = fixture["repository"]
    project = repository / f"tests/{name}/{name}.csproj"
    project.parent.mkdir(parents=True)
    project.write_text("<Project />\n", encoding="utf-8")
    solution = repository / "Fixture.slnx"
    solution.write_text(
        solution.read_text(encoding="utf-8").replace(
            "</Folder>", f'<Project Path="tests/{name}/{name}.csproj" /></Folder>'
        ),
        encoding="utf-8",
    )
    run_git(repository, "add", "Fixture.slnx", str(project.relative_to(repository)))
    run_git(repository, "commit", "-m", f"add {name} test project")


# --------------------------------------------------------------------------- #
# Contract: exit codes, document shape, anti-vacuity
# --------------------------------------------------------------------------- #


def test_a_fully_derived_record_passes(umbrella: dict[str, object]) -> None:
    code, document = measured(umbrella)
    assert code == 0, document["blockers"]
    assert document["result"] == "pass"
    assert document["schema"] == "story-final-record-v1"
    assert document["derived"] == {
        "test_results": True,
        "candidate": True,
        "record_section": True,
    }
    assert document["test_results"]["totals"] == {
        "total": 3,
        "executed": 3,
        "passed": 3,
        "failed": 0,
        "skipped": 0,
    }


def test_totals_are_summed_across_projects_not_transcribed(
    umbrella: dict[str, object],
) -> None:
    add_test_project(umbrella, "Second")
    write_trx(umbrella["repository"] / "results/second.trx", passed=5, project="Second")
    code, document = invoke(
        umbrella,
        "--test-results",
        f"Fixture={umbrella['artifact']}",
        "--test-results",
        "Second=results/second.trx",
    )
    assert code == 0, document["blockers"]
    assert document["test_results"]["totals"] == {
        "total": 8,
        "executed": 8,
        "passed": 8,
        "failed": 0,
        "skipped": 0,
    }


def test_a_run_that_derives_no_test_artifact_cannot_report_a_pass(
    umbrella: dict[str, object],
) -> None:
    code, document = invoke(umbrella)
    assert code == 1
    assert document["result"] == "blocked"
    assert "RECORD_NOT_DERIVED" in codes(document)


def test_a_record_with_no_replaceable_section_blocks(
    umbrella: dict[str, object],
) -> None:
    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        "---\nstory_key: 'x'\n---\n\n# Nothing here\n", encoding="utf-8"
    )
    code, document = measured(umbrella)
    assert code == 1
    assert "RECORD_NOT_DERIVED" in codes(document)
    assert document["derived"]["record_section"] is False


def test_an_invocation_error_still_emits_a_parseable_document(
    umbrella: dict[str, object],
) -> None:
    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(umbrella["repository"]),
            "--format",
            "json",
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=60,
    )
    assert result.returncode == 2
    document = json.loads(result.stdout)
    assert document["result"] == "error"
    assert codes(document) == {"INVALID_SCOPE"}
    # Every top-level key is pre-seeded so a consumer never KeyErrors on failure.
    for key in ("file_list", "test_results", "promotions", "blockers", "warnings"):
        assert key in document


def test_a_gate_error_honours_the_requested_format_before_argparse_succeeds() -> None:
    module = load_generator()
    assert module.pre_parse_output_format(["--format", "markdown"]) == "markdown"
    assert module.pre_parse_output_format(["--format=markdown"]) == "markdown"
    assert module.pre_parse_output_format(["--f", "markdown"]) == "markdown"
    assert module.pre_parse_output_format(["--format", "bundle"]) == "bundle"
    assert module.pre_parse_output_format([]) == "json"


def test_one_bundle_is_inserted_and_verified_by_digest(
    umbrella: dict[str, object],
) -> None:
    code, bundle = bundled(umbrella)
    assert code == 0, bundle["document"]["blockers"]
    assert bundle["schema"] == "story-final-record-bundle-v1"
    assert (
        hashlib.sha256(bundle["markdown"].encode()).hexdigest()
        == bundle["markdown_sha256"]
    )
    insert_generated_block(umbrella, bundle["markdown"])

    verification = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(umbrella["repository"]),
            "--story",
            str(umbrella["story"]),
            "--verify-record-sha256",
            bundle["markdown_sha256"],
            "--format",
            "json",
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=60,
    )
    assert verification.returncode == 0, verification.stdout

    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "Fixture | PARSED", "Fixture | NOT_RUN"
        ),
        encoding="utf-8",
    )
    code, document = invoke(
        umbrella,
        "--verify-record-sha256",
        bundle["markdown_sha256"],
    )
    assert code == 1
    assert "RECORD_CONTENT_DRIFT" in codes(document)


def test_test_project_scope_is_exact_and_artifacts_bind_to_assemblies(
    umbrella: dict[str, object],
) -> None:
    code, document = invoke(
        umbrella,
        "--test-results",
        f"Fixture={umbrella['artifact']}",
        "--test-results",
        f"Fixture={umbrella['artifact']}",
    )
    assert code == 1
    assert "TEST_PROJECT_SCOPE_MISMATCH" in codes(document)

    write_trx(umbrella["repository"] / umbrella["artifact"], project="Foreign")
    code, document = measured(umbrella)
    assert code == 1
    assert "TEST_PROJECT_SCOPE_MISMATCH" in codes(document)


def test_zero_failed_and_unapproved_skipped_results_block(
    umbrella: dict[str, object],
) -> None:
    write_trx(umbrella["repository"] / umbrella["artifact"], passed=0)
    assert "TEST_RESULTS_EMPTY" in codes(measured(umbrella)[1])

    write_trx(umbrella["repository"] / umbrella["artifact"], passed=2, failed=1)
    assert "TEST_RESULTS_FAILED" in codes(measured(umbrella)[1])

    write_trx(umbrella["repository"] / umbrella["artifact"], passed=2, skipped=1)
    assert "TEST_SKIP_NOT_ALLOWED" in codes(measured(umbrella)[1])

    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "---\n\n# Fixture Story",
            "allowed_skipped_tests:\n"
            "  - test: 'Fixture.S0'\n"
            "    reason: 'requires the opt-in live service lane'\n"
            "---\n\n# Fixture Story",
        ),
        encoding="utf-8",
    )
    code, document = measured(umbrella)
    assert code == 0, document["blockers"]


def test_valid_error_counter_mapping_and_unknown_outcomes(
    umbrella: dict[str, object],
) -> None:
    artifact = umbrella["repository"] / umbrella["artifact"]
    write_trx(artifact, passed=2, failed=1)
    content = (
        artifact.read_text(encoding="utf-8")
        .replace('outcome="Failed"', 'outcome="Error"')
        .replace('failed="1" error="0"', 'failed="0" error="1"')
    )
    artifact.write_text(content, encoding="utf-8")
    code, document = measured(umbrella)
    assert code == 1
    assert "TEST_RESULTS_FAILED" in codes(document)
    assert "TEST_COUNT_INCONSISTENT" not in codes(document)

    artifact.write_text(
        content.replace('outcome="Passed"', 'outcome="Mystery"', 1), encoding="utf-8"
    )
    assert "TEST_COUNT_INCONSISTENT" in codes(measured(umbrella)[1])


# --------------------------------------------------------------------------- #
# AC8 fault injection: one mutation per guard, each restored byte-identically
# --------------------------------------------------------------------------- #


def test_injection_altered_parsed_count_trips_the_count_guard(
    umbrella: dict[str, object],
) -> None:
    artifact = umbrella["repository"] / umbrella["artifact"]
    before = sha256_file(artifact)
    original = artifact.read_bytes()

    artifact.write_text(
        artifact.read_text(encoding="utf-8").replace('passed="3"', 'passed="2"'),
        encoding="utf-8",
    )
    code, document = measured(umbrella)
    assert code == 1
    assert "TEST_COUNT_INCONSISTENT" in codes(document)

    artifact.write_bytes(original)
    assert sha256_file(artifact) == before


def test_injection_submodule_internal_path_trips_the_boundary_guard(
    umbrella: dict[str, object],
) -> None:
    story_file = umbrella["repository"] / umbrella["story"]
    before = sha256_file(story_file)
    original = story_file.read_bytes()

    set_file_list(umbrella, ["references/Example/src/Leaked.cs"])
    code, document = measured(umbrella)
    assert code == 1
    assert "SUBMODULE_INTERNAL_PATH" in codes(document)
    # The path is refused, never quietly carried into the derived list.
    assert not any(
        path.startswith("references/Example/")
        for path in document["file_list"]["derived"]
    )

    story_file.write_bytes(original)
    assert sha256_file(story_file) == before
    assert "SUBMODULE_INTERNAL_PATH" not in codes(measured(umbrella)[1])


def test_injection_repointed_candidate_trips_the_binding_guard(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    story_hash = sha256_file(repository / umbrella["story"])
    head = run_git(repository, "rev-parse", "HEAD").stdout.strip()

    # Move the declared gitlink after the candidate: the record would otherwise
    # bind to a superseded promotion.
    run_git(
        repository / "references/Example", "commit", "--allow-empty", "-m", "advance"
    )
    run_git(repository, "add", "references/Example")
    run_git(repository, "commit", "-m", "advance gitlink")

    code, document = measured(
        umbrella, "--candidate", head, "--submodule", "references/Example"
    )
    assert code == 1
    assert "CANDIDATE_NOT_FINAL" in codes(document)
    assert document["candidate_binding"]["gitlinks_moved_after_candidate"] == [
        "references/Example"
    ]

    run_git(repository, "reset", "--hard", head)
    assert run_git(repository, "rev-parse", "HEAD").stdout.strip() == head
    assert sha256_file(repository / umbrella["story"]) == story_hash


def test_injection_dropped_declared_gitlink_trips_the_promotion_gate(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    head = run_git(repository, "rev-parse", "HEAD").stdout.strip()
    story_hash = sha256_file(repository / umbrella["story"])

    run_git(repository, "rm", "--cached", "references/Example")
    run_git(repository, "commit", "-m", "drop the declared gitlink")

    code, document = measured(umbrella, "--submodule", "references/Example")
    assert code == 1
    assert "PROMOTION_GATE_NOT_PASS" in codes(document)
    # The embedded checker document is preserved verbatim, so its own stable
    # codes stay available to the caller.
    embedded = {item["code"] for item in document["promotion_gate"]["blockers"]}
    assert "GITLINK_MISSING_IN_CANDIDATE" in embedded

    run_git(repository, "reset", "--hard", head)
    assert run_git(repository, "rev-parse", "HEAD").stdout.strip() == head
    assert sha256_file(repository / umbrella["story"]) == story_hash


def test_injection_deleted_result_artifact_trips_the_not_run_guard(
    umbrella: dict[str, object],
) -> None:
    artifact = umbrella["repository"] / umbrella["artifact"]
    before = sha256_file(artifact)
    original = artifact.read_bytes()

    artifact.unlink()
    code, document = measured(umbrella)
    assert code == 1
    assert "TEST_RESULTS_MISSING" in codes(document)
    assert [item["state"] for item in document["test_results"]["projects"]] == [
        "NOT_RUN"
    ]

    artifact.write_bytes(original)
    assert sha256_file(artifact) == before


def test_injection_backdated_artifact_trips_the_staleness_guard(
    umbrella: dict[str, object],
) -> None:
    artifact = umbrella["repository"] / umbrella["artifact"]
    before = sha256_file(artifact)
    stat = artifact.stat()

    os.utime(artifact, (stat.st_atime - 86_400, stat.st_mtime - 86_400))
    code, document = measured(umbrella)
    assert code == 1
    assert "TEST_RESULTS_STALE" in codes(document)

    os.utime(artifact, (stat.st_atime, stat.st_mtime))
    assert sha256_file(artifact) == before
    assert measured(umbrella)[0] == 0


def test_staleness_exclusion_does_not_hide_a_genuinely_stale_artifact(
    umbrella: dict[str, object],
) -> None:
    """D3 excludes the generator's own write targets, and only those."""
    repository = umbrella["repository"]
    artifact = repository / umbrella["artifact"]
    stat = artifact.stat()

    # Touching only the excluded output targets must NOT report staleness...
    os.utime(repository / umbrella["story"], (stat.st_atime + 600, stat.st_mtime + 600))
    assert "TEST_RESULTS_STALE" not in codes(measured(umbrella)[1])

    # ...while touching any ordinary derived path still must.
    os.utime(repository / "changed.txt", (stat.st_atime + 600, stat.st_mtime + 600))
    assert "TEST_RESULTS_STALE" in codes(measured(umbrella)[1])


# --------------------------------------------------------------------------- #
# Decoys the sibling checker proved necessary
# --------------------------------------------------------------------------- #


def commit_decoy(fixture: dict[str, object], name: str, content: str) -> None:
    """Commit a decoy into the range so it is inside the record's derived scope."""
    (fixture["repository"] / name).write_text(content, encoding="utf-8")
    run_git(fixture["repository"], "add", "--", name)
    run_git(fixture["repository"], "commit", "-m", "decoy")
    write_trx(fixture["repository"] / fixture["artifact"])


def test_a_filename_containing_the_literal_digits_160000_is_not_a_gitlink(
    umbrella: dict[str, object],
) -> None:
    commit_decoy(umbrella, "blob-160000-not-a-gitlink.txt", "160000 160000 160000\n")
    code, document = measured(umbrella)
    assert code == 0, document["blockers"]
    assert "blob-160000-not-a-gitlink.txt" in document["file_list"]["derived"]
    assert [item["path"] for item in document["promotions"]] == []


def test_a_filename_containing_a_backslash_does_not_abort_the_run(
    umbrella: dict[str, object],
) -> None:
    commit_decoy(umbrella, "back\\slash.txt", "decoy\n")
    code, document = measured(umbrella)
    # An ordinary file with an unusual name is measured, never a reason to
    # abort: aborting would block on state the record must simply report.
    assert code == 0, document["blockers"]
    assert "back\\slash.txt" in document["file_list"]["derived"]


def test_worktree_dirt_outside_the_committed_range_blocks(
    umbrella: dict[str, object],
) -> None:
    """A record binds to a revision, so it cannot claim a path that revision lacks."""
    (umbrella["repository"] / "someone-elses-file.txt").write_text(
        "theirs\n", encoding="utf-8"
    )
    code, document = measured(umbrella)
    assert code == 1
    assert "someone-elses-file.txt" not in document["file_list"]["derived"]
    assert "WORKTREE_NOT_CLEAN" in codes(document)


def test_dirty_in_range_source_also_blocks(umbrella: dict[str, object]) -> None:
    (umbrella["repository"] / "changed.txt").write_text(
        "dirty after candidate\n", encoding="utf-8"
    )
    code, document = measured(umbrella)
    assert code == 1
    assert "WORKTREE_NOT_CLEAN" in codes(document)


def test_ordinary_post_candidate_commit_blocks_but_output_only_commit_is_allowed(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    candidate = run_git(repository, "rev-parse", "HEAD").stdout.strip()
    (repository / "ordinary.txt").write_text("source\n", encoding="utf-8")
    run_git(repository, "add", "ordinary.txt")
    run_git(repository, "commit", "-m", "ordinary source commit")
    code, document = measured(umbrella, "--candidate", candidate)
    assert code == 1
    assert (
        "ordinary.txt" in document["candidate_binding"]["changed_paths_after_candidate"]
    )
    assert "CANDIDATE_NOT_FINAL" in codes(document)


def test_output_only_post_candidate_commit_is_allowed(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    candidate = run_git(repository, "rev-parse", "HEAD").stdout.strip()
    story_file = repository / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8") + "\nOutput note.\n", encoding="utf-8"
    )
    run_git(repository, "add", str(umbrella["story"]))
    run_git(repository, "commit", "-m", "record output")
    code, document = measured(umbrella, "--candidate", candidate)
    assert code == 0, document["blockers"]
    assert document["candidate_binding"]["changed_paths_after_candidate"] == [
        umbrella["story"]
    ]


def test_nanosecond_staleness_detects_a_later_edit_in_the_same_second(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    artifact = repository / umbrella["artifact"]
    second = artifact.stat().st_mtime_ns // 1_000_000_000 + 10
    os.utime(artifact, ns=(second * 1_000_000_000 + 100, second * 1_000_000_000 + 100))
    source = repository / "changed.txt"
    os.utime(source, ns=(second * 1_000_000_000 + 200, second * 1_000_000_000 + 200))
    assert "TEST_RESULTS_STALE" in codes(measured(umbrella)[1])


def test_symlinked_result_artifact_cannot_escape_the_repository(
    umbrella: dict[str, object], tmp_path: Path
) -> None:
    outside = tmp_path / "outside.trx"
    write_trx(outside)
    artifact = umbrella["repository"] / umbrella["artifact"]
    artifact.unlink()
    artifact.symlink_to(outside)
    code, document = measured(umbrella)
    assert code == 2
    assert codes(document) == {"INVALID_SCOPE"}


def test_one_artifact_snapshot_supplies_both_counts_and_hash(
    umbrella: dict[str, object], monkeypatch: pytest.MonkeyPatch
) -> None:
    module = load_generator()
    artifact = umbrella["repository"] / umbrella["artifact"]
    snapshot = artifact.read_bytes()
    artifact.write_bytes(snapshot.replace(b'passed="3"', b'passed="2"'))
    monkeypatch.setattr(module, "read_file_snapshot", lambda _: (snapshot, 123456789))
    blockers: list[dict] = []
    result = module.derive_test_results(
        umbrella["repository"],
        [("Fixture", umbrella["artifact"])],
        {"Fixture": "tests/Fixture/Fixture.csproj"},
        {},
        blockers,
        [],
    )
    assert blockers == []
    assert result["projects"][0]["counts"]["passed"] == 3
    assert result["projects"][0]["sha256"] == hashlib.sha256(snapshot).hexdigest()


def test_removed_gitlink_is_structural_promotion_state_not_a_file(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    run_git(repository, "rm", "-f", "references/Example")
    (repository / ".gitmodules").write_text("", encoding="utf-8")
    run_git(repository, "add", ".gitmodules")
    run_git(repository, "commit", "-m", "remove gitlink declaration and entry")
    _, document = measured(umbrella)
    assert "references/Example" not in document["file_list"]["derived"]
    assert "references/Example" in {item["path"] for item in document["promotions"]}


def test_gitlink_detection_overrides_ambient_ignore_configuration(
    umbrella: dict[str, object], monkeypatch: pytest.MonkeyPatch
) -> None:
    repository = umbrella["repository"]
    run_git(repository, "config", "submodule.Example.ignore", "all")
    run_git(
        repository / "references/Example", "commit", "--allow-empty", "-m", "advance"
    )
    run_git(repository, "add", "references/Example")
    run_git(repository, "commit", "-m", "advance hidden gitlink")
    monkeypatch.setenv("GIT_CONFIG_COUNT", "1")
    monkeypatch.setenv("GIT_CONFIG_KEY_0", "submodule.Example.ignore")
    monkeypatch.setenv("GIT_CONFIG_VALUE_0", "all")
    module = load_generator()
    hardened = module.git_environment()
    assert "GIT_CONFIG_COUNT" not in hardened
    code, document = measured(umbrella, "--submodule", "references/Example")
    assert code == 0, document["blockers"]
    assert "references/Example" in {
        item["path"] for item in document["promotions"] if item["changed_in_range"]
    }


def test_worktree_column_rename_consumes_the_original_path_token(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    module = load_generator()
    outputs = iter((b" R new.txt\0old.txt\0", b"", b""))

    def fake_git(*_args, **_kwargs):
        return subprocess.CompletedProcess([], 0, next(outputs), b"")

    monkeypatch.setattr(module, "run_git", fake_git)
    assert module.worktree_path_status(Path("/fixture")) == {"new.txt": "R"}


def test_unmatched_generated_marker_fails_closed(umbrella: dict[str, object]) -> None:
    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "### File List", "<!-- STORY-FINAL-RECORD:BEGIN -->\n### File List"
        ),
        encoding="utf-8",
    )
    code, document = measured(umbrella)
    assert code == 1
    assert document["record"]["anchor"] is None
    assert "RECORD_NOT_DERIVED" in codes(document)


def test_markdown_rendering_escapes_legal_delimiters() -> None:
    module = load_generator()
    assert module.markdown_code("tick`file.txt") == "``tick`file.txt``"
    assert module.markdown_table_text("Pipe|Project") == r"Pipe\|Project"
    record = (
        "<!-- STORY-FINAL-RECORD:BEGIN -->\n\n### File List\n\n"
        "- ``tick`file.txt`` (new)\n\n<!-- STORY-FINAL-RECORD:END -->\n"
    )
    assert module.declared_file_list(record) == (["tick`file.txt"], 1)


# --------------------------------------------------------------------------- #
# File list, baseline and drift
# --------------------------------------------------------------------------- #


def test_a_disagreeing_declared_file_list_blocks_as_drift(
    umbrella: dict[str, object],
) -> None:
    set_file_list(umbrella, ["a-path-that-never-changed.txt"])
    code, document = measured(umbrella)
    assert code == 1
    assert "FILE_LIST_DRIFT" in codes(document)
    assert "a-path-that-never-changed.txt" in document["file_list"]["unexpected"]


def test_an_agreeing_declared_file_list_does_not_block(
    umbrella: dict[str, object],
) -> None:
    derived = measured(umbrella)[1]["file_list"]["derived"]
    set_file_list(umbrella, derived)
    code, document = measured(umbrella)
    assert code == 0, document["blockers"]
    assert document["file_list"]["missing"] == []
    assert document["file_list"]["unexpected"] == []


def test_a_second_file_list_is_a_conformance_failure(
    umbrella: dict[str, object],
) -> None:
    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "### Boundary Confirmation",
            "### File List\n\n- `extra.txt` (new)\n\n### Boundary Confirmation",
        ),
        encoding="utf-8",
    )
    code, document = measured(umbrella)
    assert code == 1
    assert "FILE_LIST_DRIFT" in codes(document)


def test_an_untrustworthy_baseline_blocks(umbrella: dict[str, object]) -> None:
    for baseline in ("NO_VCS", "0" * 40):
        code, document = measured(umbrella, "--baseline", baseline)
        assert code == 1, baseline
        assert "BASELINE_NOT_TRUSTWORTHY" in codes(document), baseline


def test_a_non_ancestor_baseline_blocks(umbrella: dict[str, object]) -> None:
    repository = umbrella["repository"]
    run_git(repository, "checkout", "-b", "sidebranch", umbrella["baseline"])
    (repository / "divergent.txt").write_text("divergent\n", encoding="utf-8")
    run_git(repository, "add", "divergent.txt")
    run_git(repository, "commit", "-m", "divergent")
    divergent = run_git(repository, "rev-parse", "HEAD").stdout.strip()
    run_git(repository, "checkout", "main")

    code, document = measured(umbrella, "--baseline", divergent)
    assert code == 1
    assert "BASELINE_NOT_TRUSTWORTHY" in codes(document)


def test_gitlinks_are_reported_as_promotions_and_never_as_file_list_paths(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    run_git(
        repository / "references/Example", "commit", "--allow-empty", "-m", "advance"
    )
    run_git(repository, "add", "references/Example")
    run_git(repository, "commit", "-m", "promote")

    code, document = measured(umbrella, "--submodule", "references/Example")
    assert "references/Example" not in document["file_list"]["derived"]
    promotion = next(
        item for item in document["promotions"] if item["path"] == "references/Example"
    )
    assert promotion["recorded_mode"] == "160000"
    assert promotion["recorded_gitlink"] != promotion["baseline_gitlink"]
    assert promotion["declared"] is True
    assert code == 0, document["blockers"]


# --------------------------------------------------------------------------- #
# Markdown renderer
# --------------------------------------------------------------------------- #


def render(fixture: dict[str, object], *extra: str) -> str:
    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(fixture["repository"]),
            "--story",
            str(fixture["story"]),
            "--format",
            "markdown",
            *extra,
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=120,
    )
    return result.stdout


def test_the_renderer_names_what_it_derived(umbrella: dict[str, object]) -> None:
    derived = render(umbrella, "--test-results", f"Fixture={umbrella['artifact']}")
    assert "story-final-record-v1" in derived
    assert "The JSON document is authoritative" in derived
    assert "test results **yes**" in derived
    assert "1 test artifact(s) parsed" in derived


def test_a_nothing_derived_run_renders_visibly_differently(
    umbrella: dict[str, object],
) -> None:
    """A vacuous run must never be byte-identical to a fully measured one."""
    derived = render(umbrella, "--test-results", f"Fixture={umbrella['artifact']}")
    vacuous = render(umbrella)
    assert derived != vacuous
    assert "test results **NO**" in vacuous
    assert "0 test artifact(s) parsed" in vacuous
    assert "**BLOCKER** `RECORD_NOT_DERIVED`" in vacuous


def test_the_rendered_block_is_delimited_so_a_rerun_replaces_its_own_output(
    umbrella: dict[str, object],
) -> None:
    module = load_generator()
    block = render(umbrella, "--test-results", f"Fixture={umbrella['artifact']}")
    assert block.startswith(module.RECORD_BEGIN_MARKER)
    assert block.rstrip().endswith(module.RECORD_END_MARKER)

    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "### File List\n\n### Boundary Confirmation",
            f"{block}\n### Boundary Confirmation",
        ),
        encoding="utf-8",
    )
    code, document = measured(umbrella)
    assert document["record"]["anchor"] == "generated-block"
    assert document["record"]["generated_block"] is True
    assert code == 0, document["blockers"]


def test_a_red_suite_is_legible_in_the_rendered_block(
    umbrella: dict[str, object],
) -> None:
    write_trx(
        umbrella["repository"] / umbrella["artifact"], passed=2, failed=1, skipped=1
    )
    rendered = render(umbrella, "--test-results", f"Fixture={umbrella['artifact']}")
    assert "**This suite is not fully green: 1 failed, 1 skipped.**" in rendered


# --------------------------------------------------------------------------- #
# AC7 historical mode
# --------------------------------------------------------------------------- #


def historical(story: Path) -> tuple[int, dict]:
    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(WORKSPACE),
            "--historical",
            "--story",
            str(story.relative_to(WORKSPACE)),
            "--format",
            "json",
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=120,
    )
    return result.returncode, json.loads(result.stdout)


def historical_fixture(fixture: dict[str, object]) -> tuple[int, dict]:
    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(fixture["repository"]),
            "--historical",
            "--story",
            str(fixture["story"]),
            "--format",
            "json",
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=120,
    )
    return result.returncode, json.loads(result.stdout)


def prepare_generated_record(fixture: dict[str, object]) -> dict:
    story_file = fixture["repository"] / fixture["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "baseline_commit:",
            f"file_list_commit: '{fixture['candidate']}'\nbaseline_commit:",
        ),
        encoding="utf-8",
    )
    code, bundle = bundled(fixture)
    assert code == 0, bundle["document"]["blockers"]
    insert_generated_block(fixture, bundle["markdown"])
    return bundle


CLOSED_RECORDS = (
    "spec-6-1-rebaseline-architecture-and-planning-authority.md",
    "6-2-migrate-conversations-to-platform-owned-hosting.md",
    "6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md",
)


@pytest.mark.parametrize("name", CLOSED_RECORDS)
def test_historical_mode_verifies_closed_records_without_mutating_them(
    name: str,
) -> None:
    record = WORKSPACE / "_bmad-output/implementation-artifacts" / name
    before = sha256_file(record)
    code, document = historical(record)
    assert sha256_file(record) == before, (
        f"{name} was mutated by a read-only verification"
    )
    assert document["mode"] == "historical"
    expected_classification = (
        "generated"
        if name == "6-2-migrate-conversations-to-platform-owned-hosting.md"
        else "pre-generator"
    )
    assert document["classification"] == expected_classification
    # D4: historical verification is read-only. A sound generated record stays
    # green, while pre-generator findings are reported without rewriting history.
    assert code == 0, document["blockers"]
    assert document["result"] == "pass"
    assert (
        "former uncommitted working tree is not reconstructed" in document["boundary"]
    )
    assert document["promotion_gate"] is None


def test_historical_mode_reproduces_story_6_7s_recorded_file_list() -> None:
    record = WORKSPACE / "_bmad-output/implementation-artifacts" / CLOSED_RECORDS[2]
    _, document = historical(record)
    assert document["file_list"]["missing"] == []
    assert document["file_list"]["unexpected"] == []
    assert len(document["file_list"]["derived"]) == 37
    assert document["promotions"] == []


def test_historical_mode_verifies_story_6_2s_generated_record() -> None:
    record = WORKSPACE / "_bmad-output/implementation-artifacts" / CLOSED_RECORDS[1]
    code, document = historical(record)
    assert document["classification"] == "generated"
    assert document["record"]["anchor"] == "generated-block"
    assert document["record"]["generated_block"] is True
    assert document["record"]["declared_list_count"] == 1
    assert document["file_list"]["derived"] == document["file_list"]["declared"]
    assert document["file_list"]["missing"] == []
    assert document["file_list"]["unexpected"] == []
    assert document["warnings"] == []
    assert document["blockers"] == []
    assert code == 0


def test_historical_mode_parses_generated_counts_and_rejects_tampering(
    umbrella: dict[str, object],
) -> None:
    prepare_generated_record(umbrella)
    code, document = historical_fixture(umbrella)
    assert code == 0, document["blockers"]
    assert document["test_results"]["totals"]["total"] == 3
    assert document["derived"]["test_results"] is True

    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "| Fixture | PARSED | 3 | 3 | 3 | 0 | 0 |",
            "| Fixture | PARSED | 4 | 3 | 3 | 0 | 0 |",
        ),
        encoding="utf-8",
    )
    code, document = historical_fixture(umbrella)
    assert code == 1
    assert "TEST_COUNT_INCONSISTENT" in codes(document)


def test_historical_mode_rejects_schema_demotion_and_promotion_tampering(
    umbrella: dict[str, object],
) -> None:
    bundle = prepare_generated_record(umbrella)
    story_file = umbrella["repository"] / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            "story-final-record-v1", "removed-schema", 1
        ),
        encoding="utf-8",
    )
    code, document = historical_fixture(umbrella)
    assert code == 1
    assert document["classification"] == "malformed-generated"

    insert_generated_block(umbrella, bundle["markdown"])
    content = story_file.read_text(encoding="utf-8")
    content = content.replace(
        "_None. No root gitlink changed between the baseline and the candidate._",
        "| Path | Declared | Recorded mode | Recorded commit | Baseline commit |\n"
        "| --- | --- | --- | --- | --- |\n"
        "| `references/Example` | yes | `160000` | `deadbeef` | `deadbeef` |",
    )
    story_file.write_text(content, encoding="utf-8")
    code, document = historical_fixture(umbrella)
    assert code == 1
    assert "RECORD_CONTENT_DRIFT" in codes(document)


def test_historical_mode_blocks_a_resolved_nonancestor_baseline(
    umbrella: dict[str, object],
) -> None:
    repository = umbrella["repository"]
    run_git(repository, "checkout", "-b", "side", umbrella["baseline"])
    (repository / "side.txt").write_text("side\n", encoding="utf-8")
    run_git(repository, "add", "side.txt")
    run_git(repository, "commit", "-m", "side")
    divergent = run_git(repository, "rev-parse", "HEAD").stdout.strip()
    run_git(repository, "checkout", "main")
    write_trx(repository / umbrella["artifact"])
    prepare_generated_record(umbrella)
    story_file = repository / umbrella["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8").replace(
            str(umbrella["baseline"]), divergent
        ),
        encoding="utf-8",
    )
    code, document = historical_fixture(umbrella)
    assert code == 1
    assert "BASELINE_NOT_TRUSTWORTHY" in codes(document)


def test_historical_mode_blocks_on_a_generated_record(tmp_path: Path) -> None:
    """The demotion to warnings applies only to pre-generator records."""
    fixture = build_umbrella(tmp_path)
    repository = fixture["repository"]
    story_file = repository / fixture["story"]
    story_file.write_text(
        story_file.read_text(encoding="utf-8")
        .replace(
            "baseline_commit:",
            f"file_list_commit: '{fixture['candidate']}'\nbaseline_commit:",
        )
        .replace(
            "### File List\n\n### Boundary Confirmation",
            "<!-- STORY-FINAL-RECORD:BEGIN -->\n\n"
            "story-final-record-v1\n\n"
            "### File List\n\n- `references/Example/leaked.cs` (new)\n\n"
            "<!-- STORY-FINAL-RECORD:END -->\n\n### Boundary Confirmation",
        ),
        encoding="utf-8",
    )
    result = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(repository),
            "--historical",
            "--story",
            str(fixture["story"]),
            "--format",
            "json",
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=60,
    )
    document = json.loads(result.stdout)
    assert document["classification"] == "malformed-generated"
    assert result.returncode == 1
    assert "SUBMODULE_INTERNAL_PATH" in codes(document)


# --------------------------------------------------------------------------- #
# AC6 non-deletability, and the parity this story's own record must satisfy
# --------------------------------------------------------------------------- #


def story_file_list() -> list[str]:
    content = STORY_FILE.read_text(encoding="utf-8")
    module = load_generator()
    paths, _ = module.declared_file_list(content)
    return paths


@pytest.mark.parametrize("relative_path", GENERATOR_WORKFLOWS)
def test_every_completion_surface_invokes_the_generator(relative_path: str) -> None:
    for tree in (".agents/skills", ".claude/skills"):
        content = (WORKSPACE / tree / relative_path).read_text(encoding="utf-8")
        assert completion_surface_violations(content) == [], f"{tree}/{relative_path}"


def completion_surface_violations(content: str) -> list[str]:
    required = (
        "generate_story_record.py",
        "--format bundle",
        "markdown_sha256",
        "--verify-record-sha256",
        "RECORD_NOT_DERIVED",
    )
    return [value for value in required if value not in content]


@pytest.mark.parametrize("relative_path", GENERATOR_WORKFLOWS)
def test_the_generator_invocation_cannot_be_silently_removed(
    relative_path: str,
) -> None:
    """The same executable contract checker must reject a displaced invocation."""
    for tree in (".agents/skills", ".claude/skills"):
        content = (WORKSPACE / tree / relative_path).read_text(encoding="utf-8")
        assert completion_surface_violations(content) == []
        gutted = content.replace("generate_story_record.py", "the record is optional")
        assert "generate_story_record.py" in completion_surface_violations(gutted)


def test_the_promotion_gate_span_still_excludes_the_final_record_section() -> None:
    """T9: inserting this story's section must not widen Story 6.7's gate span.

    Left unrepaired the positive promotion test keeps passing while its
    displacement guard weakens: a promotion clause moved into the final-record
    section would count as "inside the gate".
    """
    sibling_path = (
        Path(__file__).resolve().parent / "test_verify_submodule_promotion.py"
    )
    spec = importlib_util.spec_from_file_location(
        "sibling_promotion_tests", sibling_path
    )
    sibling = importlib_util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(sibling)

    for relative_path in GENERATOR_WORKFLOWS:
        markers, _ = sibling.WORKFLOW_GATE_CONTRACTS[relative_path]
        content = (WORKSPACE / ".agents/skills" / relative_path).read_text(
            encoding="utf-8"
        )
        start, end = sibling.promotion_gate_span(content, markers)
        assert start >= 0, relative_path
        assert "generate_story_record.py" not in content[start:end], relative_path


def test_both_skill_trees_stay_byte_identical_for_every_changed_file() -> None:
    for relative_path in GENERATOR_WORKFLOWS:
        agent_file = WORKSPACE / ".agents/skills" / relative_path
        claude_file = WORKSPACE / ".claude/skills" / relative_path
        assert agent_file.read_bytes() == claude_file.read_bytes(), relative_path


def test_every_emitted_code_is_documented_in_the_runbook() -> None:
    """A blocker or warning code that no runbook row explains is not shippable."""
    module = load_generator()
    runbook = RUNBOOK.read_text(encoding="utf-8")
    source = SCRIPT.read_text(encoding="utf-8")
    for code in module.BLOCKER_REMEDIATION:
        assert f"`{code}`" in runbook, code
    tree = ast.parse(source)
    emitted = {
        node.args[0].value
        for node in ast.walk(tree)
        if isinstance(node, ast.Call)
        and isinstance(node.func, ast.Name)
        and node.func.id in {"GateError", "diagnostic", "blocker"}
        and node.args
        and isinstance(node.args[0], ast.Constant)
        and isinstance(node.args[0].value, str)
        and re.fullmatch(r"[A-Z_]+", node.args[0].value)
    }
    for code in emitted:
        assert f"`{code}`" in runbook, code


def test_the_story_file_list_carries_no_submodule_internal_path() -> None:
    assert [path for path in story_file_list() if path.startswith("references/")] == []


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
