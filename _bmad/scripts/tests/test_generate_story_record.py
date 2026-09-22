# /// script
# requires-python = ">=3.11"
# dependencies = ["pytest>=8.0", "jsonschema>=4.0"]
# ///
"""Hermetic tests for the story final-record generator."""

import ast
import hashlib
import json
import os
import re
import subprocess
import sys
import unicodedata
from copy import deepcopy
from importlib import util as importlib_util
from pathlib import Path

import jsonschema
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

# V12 replaces the retired quick-dev/dev-auto route assumptions with these exact
# current logical routes. The story-record generator remains covered hermetically
# below; current-route lifecycle enforcement is owned by the V12 promotion and
# evidence gates rather than by the retired Story 6.8 integration assertions.
CURRENT_LIFECYCLE_WORKFLOWS = (
    "bmad-build/step-04-review.md",
    "bmad-build/step-05-present.md",
    "bmad-build/step-oneshot.md",
    "bmad-build-auto/step-04-review.md",
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
    code_base: Path | None = None,
) -> None:
    """Write a TRX whose summary agrees with the results it contains."""
    if code_base is None:
        repository = next(
            (parent for parent in (path.parent, *path.parents) if (parent / ".git").exists()),
            None,
        )
        if repository is not None:
            candidate = run_git(repository, "rev-parse", "HEAD").stdout.strip()
            code_base = (
                repository
                / "tests"
                / project
                / "bin"
                / "Release"
                / "net10.0"
                / f"{project}.dll"
            )
            code_base.parent.mkdir(parents=True, exist_ok=True)
            code_base.write_bytes(
                f"fixture managed assembly 1.0.0+{candidate}\0".encode("ascii")
            )
        else:
            code_base = Path(f"/fixture/{project}.dll")
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
        f'    <UnitTest name="fixture"><TestMethod codeBase="{code_base}" /></UnitTest>\n'
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
    (umbrella / ".gitignore").write_text("bin/\nobj/\nresults/\n", encoding="utf-8")
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
    assert document["build_manifest"]["candidate"] == umbrella["candidate"]
    assert [
        item["project"] for item in document["build_manifest"]["projects"]
    ] == ["Fixture"]
    assert re.fullmatch(
        r"[0-9a-f]{64}", document["build_manifest"]["projects"][0]["sha256"]
    )


def test_test_binary_must_embed_the_exact_candidate_revision(
    umbrella: dict[str, object],
) -> None:
    binary = (
        umbrella["repository"]
        / "tests/Fixture/bin/Release/net10.0/Fixture.dll"
    )
    binary.write_bytes(b"fixture managed assembly 1.0.0+" + b"0" * 40 + b"\0")
    code, document = measured(umbrella)
    assert code == 1
    assert "TEST_BUILD_NOT_BOUND" in codes(document)
    assert document["build_manifest"]["projects"][0]["source_revision"] is None


def test_test_result_must_not_predate_its_candidate_bound_binary(
    umbrella: dict[str, object],
) -> None:
    artifact = umbrella["repository"] / umbrella["artifact"]
    binary = (
        umbrella["repository"]
        / "tests/Fixture/bin/Release/net10.0/Fixture.dll"
    )
    future = artifact.stat().st_mtime_ns + 1_000_000_000
    os.utime(binary, ns=(future, future))
    code, document = measured(umbrella)
    assert code == 1
    assert "TEST_BUILD_NOT_BOUND" in codes(document)


def test_totals_are_summed_across_projects_not_transcribed(
    umbrella: dict[str, object],
) -> None:
    add_test_project(umbrella, "Second")
    write_trx(umbrella["repository"] / umbrella["artifact"])
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

    os.utime(artifact, ns=(stat.st_atime_ns, stat.st_mtime_ns))
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
    write_trx(repository / umbrella["artifact"])
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
    write_trx(repository / umbrella["artifact"])

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
    assert "### Test Build Manifest" in derived
    assert f"Candidate `{umbrella['candidate']}` was clean-rebuilt" in derived
    artifact_hash = sha256_file(umbrella["repository"] / umbrella["artifact"])
    assert f"`{artifact_hash}`" in derived
    assert f"`{artifact_hash[:16]}`" not in derived


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
# Current-route boundary and parity after V12 supersession
# --------------------------------------------------------------------------- #


def story_file_list() -> list[str]:
    content = STORY_FILE.read_text(encoding="utf-8")
    module = load_generator()
    paths, _ = module.declared_file_list(content)
    return paths


def test_current_route_inventory_matches_the_v12_gate_contract() -> None:
    """Retired route names must not creep back into a current-tree assertion."""
    sibling_path = (
        Path(__file__).resolve().parent / "test_verify_submodule_promotion.py"
    )
    spec = importlib_util.spec_from_file_location(
        "sibling_promotion_tests", sibling_path
    )
    sibling = importlib_util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(sibling)

    assert set(CURRENT_LIFECYCLE_WORKFLOWS) == set(sibling.WORKFLOW_GATE_CONTRACTS)
    assert not any("bmad-quick-dev" in path for path in CURRENT_LIFECYCLE_WORKFLOWS)
    assert not any("bmad-dev-auto" in path for path in CURRENT_LIFECYCLE_WORKFLOWS)


def test_v12_gate_span_does_not_absorb_story_record_enforcement() -> None:
    """Story-record text outside a lifecycle gate must not satisfy that gate."""
    sibling_path = (
        Path(__file__).resolve().parent / "test_verify_submodule_promotion.py"
    )
    spec = importlib_util.spec_from_file_location(
        "sibling_promotion_tests", sibling_path
    )
    sibling = importlib_util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(sibling)

    for relative_path in CURRENT_LIFECYCLE_WORKFLOWS:
        markers, _ = sibling.WORKFLOW_GATE_CONTRACTS[relative_path]
        content = (WORKSPACE / ".agents/skills" / relative_path).read_text(
            encoding="utf-8"
        )
        start, end = sibling.promotion_gate_span(content, markers)
        assert start >= 0, relative_path
        assert "generate_story_record.py" not in content[start:end], relative_path


def test_both_skill_trees_stay_byte_identical_for_every_changed_file() -> None:
    for relative_path in CURRENT_LIFECYCLE_WORKFLOWS:
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


# --------------------------------------------------------------------------- #
# 7.1-SCHEMAS checkpoint: v2_schema_contract (no generator import or call)
# --------------------------------------------------------------------------- #

OUTPUT_SCHEMA_INVALID = "OUTPUT_SCHEMA_INVALID"
FROZEN_STORY_CONTRACT_SCHEMA_DIGEST = (
    "33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0"
)
HOLD_PATH = WORKSPACE / "_bmad-output/planning-artifacts/implementation-hold-v1.json"
STORY_CONTRACT_SCHEMA = WORKSPACE / "_bmad/schemas/v9-story-contract-v1.schema.json"
ACCEPTANCE_RESULT_SCHEMA = WORKSPACE / "_bmad/schemas/v9-acceptance-result-v1.schema.json"
FROZEN_INVENTORY_SCHEMA = WORKSPACE / "_bmad/schemas/v9-frozen-inventory-v1.schema.json"
FINAL_RECORD_SCHEMA = WORKSPACE / "_bmad/schemas/story-final-record-v2.schema.json"
STORY_7_1_CONTRACT = (
    WORKSPACE / "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json"
)
NEW_SCHEMA_FIXTURES = (
    ACCEPTANCE_RESULT_SCHEMA,
    FROZEN_INVENTORY_SCHEMA,
    FINAL_RECORD_SCHEMA,
)
CANONICAL_SCHEMA_PATHS = (STORY_CONTRACT_SCHEMA, *NEW_SCHEMA_FIXTURES)
SCHEMA_IDENTITIES = {
    STORY_CONTRACT_SCHEMA: "hexalith.conversations.story-contract.v1",
    ACCEPTANCE_RESULT_SCHEMA: "hexalith.conversations.acceptance-result.v1",
    FROZEN_INVENTORY_SCHEMA: "hexalith.conversations.frozen-inventory.v1",
    FINAL_RECORD_SCHEMA: "hexalith.conversations.story-final-record.v2",
}
SCHEMA_AUTHORITY_BOUNDARY_ANNOTATIONS = {
    ACCEPTANCE_RESULT_SCHEMA: (
        "Authority boundary: this schema validates document structure only; "
        "producers and verifiers must derive command outcomes, candidate identity, "
        "digest bindings, and any assertion ledger from executed commands and "
        "committed evidence."
    ),
    FROZEN_INVENTORY_SCHEMA: (
        "Authority boundary: this schema validates document structure only; "
        "producers and verifiers must independently recompute the canonical NFC "
        "UTF-8 LF inventory digest from the ordered items."
    ),
    FINAL_RECORD_SCHEMA: (
        "Authority boundary: this schema validates document structure only; "
        "producers and verifiers must derive candidate, gitlink, scenario, output, "
        "and summary facts from committed evidence rather than caller-authored "
        "claims."
    ),
}
HOLD_PLANNING_CANDIDATE = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5"
HOLD_BUNDLE_DIGEST = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055"
HOLD_IR0_DIGEST = "862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad"
STORY_7_1_INVENTORY_ITEMS = (
    "V8-6.8-AC1",
    "V8-6.8-AC6-ANTI-VACUITY",
    "V8-6.8-PROHIBITIONS-SOURCE-BOUNDARY",
)
STORY_7_1_INVENTORY_DIGEST = (
    "5fb79e8d9251c3187f2a2de7d4ae3766ab962015e628d345f8033bf14ba8e36e"
)
ROOT_GITLINK_PATHS = (
    "references/Hexalith.AI.Tools",
    "references/Hexalith.Builds",
    "references/Hexalith.Commons",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)


def v2_schema_contract_hold_is_lifted() -> None:
    hold = json.loads(HOLD_PATH.read_text(encoding="utf-8"))
    if (
        hold.get("effectiveState") != "LIFTED"
        or hold.get("scope", {}).get("unlocks") != ["7.1-SCHEMAS"]
        or hold.get("scope", {}).get("planningCandidate") != HOLD_PLANNING_CANDIDATE
        or hold.get("scope", {}).get("bundleDigest") != HOLD_BUNDLE_DIGEST
        or hold.get("scope", {}).get("ir0Sha256") != HOLD_IR0_DIGEST
    ):
        raise AssertionError("BLOCKED: 7.1-SCHEMAS hold drift; treat hold as ACTIVE")


def v2_schema_contract_load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def v2_schema_contract_validator(schema: dict) -> jsonschema.Draft202012Validator:
    jsonschema.Draft202012Validator.check_schema(schema)
    return jsonschema.Draft202012Validator(schema)


def v2_schema_contract_reject(schema: dict, instance: object) -> str:
    with pytest.raises(jsonschema.ValidationError):
        v2_schema_contract_validator(schema).validate(instance)
    return OUTPUT_SCHEMA_INVALID


def v2_schema_contract_object_nodes(schema: object) -> list[dict]:
    nodes: list[dict] = []
    if isinstance(schema, dict):
        if schema.get("type") == "object":
            nodes.append(schema)
        for value in schema.values():
            nodes.extend(v2_schema_contract_object_nodes(value))
    elif isinstance(schema, list):
        for item in schema:
            nodes.extend(v2_schema_contract_object_nodes(item))
    return nodes


def v2_schema_contract_resolve(root: object, path: tuple) -> object:
    current = root
    for step in path:
        current = current[step]
    return current


def v2_schema_contract_delete(root: dict, path: tuple) -> dict:
    mutated = deepcopy(root)
    parent = v2_schema_contract_resolve(mutated, path[:-1])
    del parent[path[-1]]
    return mutated


def v2_schema_contract_inject(root: dict, path: tuple) -> dict:
    mutated = deepcopy(root)
    target = v2_schema_contract_resolve(mutated, path)
    target["undeclaredField"] = True
    return mutated


def v2_schema_contract_object_paths(value: object, path: tuple = ()) -> list[tuple]:
    found: list[tuple] = []
    if isinstance(value, dict):
        found.append(path)
        for key, child in value.items():
            found.extend(v2_schema_contract_object_paths(child, path + (key,)))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            found.extend(v2_schema_contract_object_paths(child, path + (index,)))
    return found


def v2_schema_contract_assert_closed(schema: dict) -> None:
    for node in v2_schema_contract_object_nodes(schema):
        if node.get("additionalProperties") is not False:
            raise jsonschema.ValidationError("object is not recursively closed")
        required = node.get("required")
        properties = node.get("properties")
        if isinstance(required, list) and isinstance(properties, dict):
            missing = [name for name in required if name not in properties]
            if missing:
                raise jsonschema.ValidationError(
                    f"required field missing from properties: {missing}"
                )


def v2_schema_contract_inventory_digest(items: list[str]) -> str:
    payload = "".join(
        f"{unicodedata.normalize('NFC', item)}\n" for item in items
    ).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def v2_schema_contract_gitlinks() -> list[dict[str, str]]:
    return [
        {
            "path": path,
            "commit": HOLD_PLANNING_CANDIDATE,
            "mode": "160000",
        }
        for path in ROOT_GITLINK_PATHS
    ]


def v2_schema_contract_acceptance_result(*, with_ledger: bool) -> dict:
    document = {
        "schemaVersion": "hexalith.conversations.acceptance-result.v1",
        "storyId": "7.1",
        "scenarioId": "AC-7.1-01",
        "command": (
            "python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py "
            "-k v2_schema_contract --junitxml=artifacts/v9/schema-slice/"
            "v2-schema-contract.xml"
        ),
        "exitCode": 0,
        "result": "PASS",
        "blockers": [],
        "candidate": HOLD_PLANNING_CANDIDATE,
        "inputs": [
            {
                "path": "_bmad/schemas/v9-story-contract-v1.schema.json",
                "sha256": FROZEN_STORY_CONTRACT_SCHEMA_DIGEST,
            }
        ],
        "outputs": [
            {
                "path": "artifacts/v9/schema-slice/v2-schema-contract.xml",
                "sha256": "a" * 64,
            }
        ],
    }
    if with_ledger:
        document["assertionLedger"] = [
            {
                "id": "SCHEMA-METASCHEMA",
                "subject": "draft-2020-12",
                "state": "PASS",
            }
        ]
    return document


def v2_schema_contract_frozen_inventory() -> dict:
    items = list(STORY_7_1_INVENTORY_ITEMS)
    return {
        "schemaVersion": "hexalith.conversations.frozen-inventory.v1",
        "inventoryId": "V9-7.1-ENTRY-v1",
        "digestAlgorithm": "sha256",
        "canonicalization": "nfc-utf8-lf-displayed-ids",
        "items": items,
        "sha256": v2_schema_contract_inventory_digest(items),
    }


def v2_schema_contract_final_record() -> dict:
    return {
        "schemaVersion": "hexalith.conversations.story-final-record.v2",
        "storyId": "7.1",
        "authority": {
            "epic": "epic-6-authority-2026-08-03-v10",
            "architecture": "conversations-architecture-2026-08-03-v10",
            "planningCandidate": HOLD_PLANNING_CANDIDATE,
            "bundleDigest": HOLD_BUNDLE_DIGEST,
        },
        "candidate": {
            "commit": HOLD_PLANNING_CANDIDATE,
            "gitlinks": v2_schema_contract_gitlinks(),
        },
        "predecessors": ["6.2"],
        "inventory": {
            "id": "V9-7.1-ENTRY-v1",
            "sha256": STORY_7_1_INVENTORY_DIGEST,
        },
        "scenarios": [
            {
                "scenarioId": "AC-7.1-01",
                "command": (
                    "python3 -m pytest -q "
                    "_bmad/scripts/tests/test_generate_story_record.py "
                    "-k v2_schema_contract"
                ),
                "exitCode": 0,
                "result": "PASS",
                "blockers": [],
            }
        ],
        "faultInjection": {
            "results": [
                {
                    "id": "MISSING_REQUIRED_FIELD",
                    "expectedBlocker": "OUTPUT_SCHEMA_INVALID",
                }
            ]
        },
        "outputs": {
            "json": {
                "path": "docs/release-evidence/story-7.1-final-record-v2.json",
                "sha256": "b" * 64,
            },
            "markdown": {
                "path": "docs/release-evidence/story-7.1-final-record-v2.md",
                "sha256": "c" * 64,
            },
        },
        "rollback": {
            "boundary": (
                "remove only new v2 schemas, generator-core changes, "
                "Story 7.1 tests/results, and Story 7.1 final-record outputs; "
                "preserve the v1 generator, Story 6.8 provenance, all completed "
                "records, and the v1-v8 prefix."
            )
        },
        "summary": {
            "required": 6,
            "passed": 6,
            "failed": 0,
            "blocked": 0,
            "skipped": 0,
            "notRun": 0,
        },
        "renderedMarkdownSha256": "d" * 64,
    }


def v2_schema_contract_valid_pairs() -> list[tuple[Path, dict]]:
    return [
        (STORY_CONTRACT_SCHEMA, v2_schema_contract_load(STORY_7_1_CONTRACT)),
        (ACCEPTANCE_RESULT_SCHEMA, v2_schema_contract_acceptance_result(with_ledger=False)),
        (ACCEPTANCE_RESULT_SCHEMA, v2_schema_contract_acceptance_result(with_ledger=True)),
        (FROZEN_INVENTORY_SCHEMA, v2_schema_contract_frozen_inventory()),
        (FINAL_RECORD_SCHEMA, v2_schema_contract_final_record()),
    ]


def test_v2_schema_contract_hold_drift_is_blocked(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    drifted = json.loads(HOLD_PATH.read_text(encoding="utf-8"))
    drifted["scope"]["planningCandidate"] = "0" * 40
    fake = tmp_path / "implementation-hold-v1.json"
    fake.write_text(json.dumps(drifted), encoding="utf-8")
    monkeypatch.setattr(
        sys.modules[__name__],
        "HOLD_PATH",
        fake,
    )
    with pytest.raises(AssertionError, match="BLOCKED: 7.1-SCHEMAS hold drift"):
        v2_schema_contract_hold_is_lifted()
    real_hold = WORKSPACE / "_bmad-output/planning-artifacts/implementation-hold-v1.json"
    assert (
        json.loads(real_hold.read_text(encoding="utf-8"))["scope"]["planningCandidate"]
        == HOLD_PLANNING_CANDIDATE
    )


def test_v2_schema_contract_hold_metaschema_and_identities() -> None:
    v2_schema_contract_hold_is_lifted()
    assert sha256_file(STORY_CONTRACT_SCHEMA) == FROZEN_STORY_CONTRACT_SCHEMA_DIGEST
    for path in CANONICAL_SCHEMA_PATHS:
        schema = v2_schema_contract_load(path)
        v2_schema_contract_validator(schema)
        v2_schema_contract_assert_closed(schema)
        assert schema["properties"]["schemaVersion"]["const"] == SCHEMA_IDENTITIES[path]
        assert schema["$id"].startswith("https://hexalith.io/schemas/conversations/")
        assert schema["$defs"]["commit"]["pattern"] == "^[0-9a-f]{40}$"
        assert schema["$defs"]["digest"]["pattern"] == "^[0-9a-f]{64}$"
    empty_ledger = v2_schema_contract_acceptance_result(with_ledger=False)
    empty_ledger["assertionLedger"] = []
    v2_schema_contract_validator(
        v2_schema_contract_load(ACCEPTANCE_RESULT_SCHEMA)
    ).validate(empty_ledger)


def test_v2_schema_contract_authority_boundary_annotations() -> None:
    v2_schema_contract_hold_is_lifted()
    for path, annotation in SCHEMA_AUTHORITY_BOUNDARY_ANNOTATIONS.items():
        assert v2_schema_contract_load(path)["description"] == annotation


def test_v2_schema_contract_valid_in_memory_instances() -> None:
    v2_schema_contract_hold_is_lifted()
    inventory = v2_schema_contract_frozen_inventory()
    assert inventory["sha256"] == STORY_7_1_INVENTORY_DIGEST
    assert inventory["schemaVersion"] != "hexalith.conversations.v9-inventory.v1"
    for path, instance in v2_schema_contract_valid_pairs():
        v2_schema_contract_validator(v2_schema_contract_load(path)).validate(instance)


def test_v2_schema_contract_rejects_missing_and_extra_fields() -> None:
    v2_schema_contract_hold_is_lifted()
    nested_required = (
        (
            ACCEPTANCE_RESULT_SCHEMA,
            v2_schema_contract_acceptance_result(with_ledger=True),
            (
                ("inputs", 0, "path"),
                ("outputs", 0, "sha256"),
                ("assertionLedger", 0, "subject"),
            ),
        ),
        (
            FROZEN_INVENTORY_SCHEMA,
            v2_schema_contract_frozen_inventory(),
            (("items",), ("sha256",)),
        ),
        (
            FINAL_RECORD_SCHEMA,
            v2_schema_contract_final_record(),
            (
                ("authority", "bundleDigest"),
                ("candidate", "commit"),
                ("candidate", "gitlinks", 0, "mode"),
                ("inventory", "id"),
                ("scenarios", 0, "scenarioId"),
                ("faultInjection", "results"),
                ("outputs", "json", "path"),
                ("rollback", "boundary"),
                ("summary", "notRun"),
            ),
        ),
        (
            STORY_CONTRACT_SCHEMA,
            v2_schema_contract_load(STORY_7_1_CONTRACT),
            (("authority", "planningCandidate"), ("finalRecord", "summary", "passed")),
        ),
    )
    before = {fixture: fixture.read_bytes() for fixture in NEW_SCHEMA_FIXTURES}
    try:
        for path, instance in v2_schema_contract_valid_pairs():
            schema = v2_schema_contract_load(path)
            for field in schema["required"]:
                assert (
                    v2_schema_contract_reject(
                        schema, v2_schema_contract_delete(instance, (field,))
                    )
                    == OUTPUT_SCHEMA_INVALID
                )
            for object_path in v2_schema_contract_object_paths(instance):
                assert (
                    v2_schema_contract_reject(
                        schema, v2_schema_contract_inject(instance, object_path)
                    )
                    == OUTPUT_SCHEMA_INVALID
                )
        for path, instance, removals in nested_required:
            schema = v2_schema_contract_load(path)
            for removal in removals:
                assert (
                    v2_schema_contract_reject(
                        schema, v2_schema_contract_delete(instance, removal)
                    )
                    == OUTPUT_SCHEMA_INVALID
                )
    finally:
        for fixture, original in before.items():
            fixture.write_bytes(original)
    assert all(fixture.read_bytes() == original for fixture, original in before.items())


def test_v2_schema_contract_rejects_invalid_bindings() -> None:
    v2_schema_contract_hold_is_lifted()
    acceptance_schema = v2_schema_contract_load(ACCEPTANCE_RESULT_SCHEMA)
    inventory_schema = v2_schema_contract_load(FROZEN_INVENTORY_SCHEMA)
    record_schema = v2_schema_contract_load(FINAL_RECORD_SCHEMA)
    before = {fixture: fixture.read_bytes() for fixture in NEW_SCHEMA_FIXTURES}
    try:
        acceptance = v2_schema_contract_acceptance_result(with_ledger=True)
        acceptance["schemaVersion"] = "hexalith.conversations.v9-inventory.v1"
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        acceptance = v2_schema_contract_acceptance_result(with_ledger=False)
        acceptance["candidate"] = HOLD_PLANNING_CANDIDATE.upper()
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        acceptance = v2_schema_contract_acceptance_result(with_ledger=False)
        acceptance["candidate"] = "abc"
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        acceptance = v2_schema_contract_acceptance_result(with_ledger=False)
        acceptance["inputs"][0]["sha256"] = "A" * 64
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        acceptance = v2_schema_contract_acceptance_result(with_ledger=False)
        acceptance["outputs"][0]["path"] = "/tmp/escape.json"
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        acceptance = v2_schema_contract_acceptance_result(with_ledger=False)
        acceptance["outputs"][0]["path"] = "docs/../secrets.json"
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        acceptance = v2_schema_contract_acceptance_result(with_ledger=False)
        acceptance["outputs"][0]["path"] = "docs\\release-evidence\\escape.json"
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        acceptance = v2_schema_contract_acceptance_result(with_ledger=False)
        acceptance["blockers"] = ["OUTPUT_SCHEMA_INVALID", "OUTPUT_SCHEMA_INVALID"]
        assert v2_schema_contract_reject(acceptance_schema, acceptance) == (
            OUTPUT_SCHEMA_INVALID
        )

        inventory = v2_schema_contract_frozen_inventory()
        inventory["items"] = [inventory["items"][0], *inventory["items"]]
        assert v2_schema_contract_reject(inventory_schema, inventory) == (
            OUTPUT_SCHEMA_INVALID
        )

        inventory = v2_schema_contract_frozen_inventory()
        inventory["digestAlgorithm"] = "sha1"
        assert v2_schema_contract_reject(inventory_schema, inventory) == (
            OUTPUT_SCHEMA_INVALID
        )

        record = v2_schema_contract_final_record()
        record["candidate"]["gitlinks"][0], record["candidate"]["gitlinks"][1] = (
            record["candidate"]["gitlinks"][1],
            record["candidate"]["gitlinks"][0],
        )
        assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID

        record = v2_schema_contract_final_record()
        record["candidate"]["gitlinks"][0]["mode"] = "100644"
        assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID

        record = v2_schema_contract_final_record()
        record["faultInjection"]["results"] = [
            record["faultInjection"]["results"][0],
            record["faultInjection"]["results"][0],
        ]
        assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID

        record = v2_schema_contract_final_record()
        record["predecessors"] = ["6.2", "6.2"]
        assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID
    finally:
        for fixture, original in before.items():
            fixture.write_bytes(original)
    assert all(fixture.read_bytes() == original for fixture, original in before.items())


def test_v2_schema_contract_restores_permissive_and_inconsistent_fixtures() -> None:
    v2_schema_contract_hold_is_lifted()
    story_contract_before = STORY_CONTRACT_SCHEMA.read_bytes()
    mutations = (
        (
            ACCEPTANCE_RESULT_SCHEMA,
            lambda document: document.update({"additionalProperties": True}),
        ),
        (
            FROZEN_INVENTORY_SCHEMA,
            lambda document: document["properties"].pop("sha256"),
        ),
        (
            FINAL_RECORD_SCHEMA,
            lambda document: document["$defs"]["authority"]["properties"].pop(
                "bundleDigest"
            ),
        ),
    )
    for path, mutate in mutations:
        before = path.read_bytes()
        try:
            document = json.loads(before)
            mutate(document)
            path.write_bytes(
                json.dumps(document, indent=2, ensure_ascii=True).encode("utf-8")
                + b"\n"
            )
            try:
                v2_schema_contract_assert_closed(document)
                raise AssertionError("permissive or inconsistent schema was accepted")
            except jsonschema.ValidationError:
                assert OUTPUT_SCHEMA_INVALID == OUTPUT_SCHEMA_INVALID
        finally:
            path.write_bytes(before)
        assert path.read_bytes() == before
    assert STORY_CONTRACT_SCHEMA.read_bytes() == story_contract_before
    assert sha256_file(STORY_CONTRACT_SCHEMA) == FROZEN_STORY_CONTRACT_SCHEMA_DIGEST


# --------------------------------------------------------------------------- #
# Story 7.1 v2 generator route (`--contract`)
# --------------------------------------------------------------------------- #
#
# Roots of trust are pinned here, in the consuming test source: the schema
# files, the frozen gitlink order, and the digest rules are re-derived below
# rather than read back from the generator under test.

V2_FAILURE_SCHEMA = WORKSPACE / "_bmad/schemas/story-record-generator-failure-v1.schema.json"
V2_AUTHORITY_BUNDLE = WORKSPACE / "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V2_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json"
V2_BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V2_TARGET_PATH = "_bmad/scripts/tests/test_generate_story_record.py"
V2_TARGET_MODULE = "_bmad.scripts.tests.test_generate_story_record"
V2_OUTPUT_JSON = "docs/release-evidence/story-7.1-final-record-v2.json"
V2_OUTPUT_MARKDOWN = "docs/release-evidence/story-7.1-final-record-v2.md"
V2_FIXED_DATE = "2026-01-01T00:00:00+00:00"
V2_FIXED_EPOCH = 1767225600
V2_RESULT_MTIME_NS = (V2_FIXED_EPOCH + 86400) * 1_000_000_000
V2_ZERO = "0" * 64
V2_GIT_ENV = dict(
    GIT_ENV, GIT_AUTHOR_DATE=V2_FIXED_DATE, GIT_COMMITTER_DATE=V2_FIXED_DATE
)
# The real root declaration order, deliberately not ordinal, so the fixture
# proves the generator sorts raw gitlinks ordinally instead of echoing input order.
V2_DECLARATION_ORDER = (
    "references/Hexalith.AI.Tools",
    "references/Hexalith.EventStore",
    "references/Hexalith.Projects",
    "references/Hexalith.Folders",
    "references/Hexalith.Tenants",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Parties",
    "references/Hexalith.Memories",
    "references/Hexalith.Commons",
    "references/Hexalith.Builds",
)


def v2_git(repository: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            "git",
            "-c",
            "init.defaultBranch=main",
            "-c",
            "commit.gpgsign=false",
            "-C",
            str(repository),
            *arguments,
        ],
        check=True,
        capture_output=True,
        env=V2_GIT_ENV,
        text=True,
        timeout=30,
    )


def v2_gitlink_commit(path: str) -> str:
    return hashlib.sha1(f"fixture-gitlink:{path}".encode("utf-8")).hexdigest()


def v2_contract() -> dict:
    return json.loads(STORY_7_1_CONTRACT.read_text(encoding="utf-8"))


def v2_pytest_scenarios() -> list[tuple[str, str, str]]:
    """(scenario id, -k selector, JUnit path) read from the frozen contract."""
    scenarios = []
    for scenario in v2_contract()["scenarios"][:5]:
        command = scenario["command"]
        selector = re.search(r" -k (\S+) ", command).group(1)
        junit = re.search(r"--junitxml=(\S+)$", command).group(1)
        scenarios.append((scenario["id"], selector, junit))
    return scenarios


def v2_junit(
    selector: str,
    cases: list[tuple[str, str | None]] | None = None,
    *,
    classname: str = V2_TARGET_MODULE,
    counters: dict[str, int] | None = None,
) -> bytes:
    """A pytest-shaped JUnit document whose counters agree with its testcases."""
    if cases is None:
        cases = [(f"test_{selector}_first", None), (f"test_{selector}_second", None)]
    counted = {
        "tests": len(cases),
        "failures": sum(1 for _, child in cases if child == "failure"),
        "errors": sum(1 for _, child in cases if child == "error"),
        "skipped": sum(1 for _, child in cases if child == "skipped"),
    }
    counted.update(counters or {})
    body = []
    for name, child in cases:
        if child is None:
            body.append(f'<testcase classname="{classname}" name="{name}" time="0.001" />')
        else:
            body.append(
                f'<testcase classname="{classname}" name="{name}" time="0.001">'
                f'<{child} message="fixture" /></testcase>'
            )
    return (
        '<?xml version="1.0" encoding="utf-8"?><testsuites name="pytest tests">'
        f'<testsuite name="pytest" errors="{counted["errors"]}" '
        f'failures="{counted["failures"]}" skipped="{counted["skipped"]}" '
        f'tests="{counted["tests"]}" time="0.010" timestamp="2026-01-02T00:00:00+00:00" '
        'hostname="fixture">' + "".join(body) + "</testsuite></testsuites>"
    ).encode("utf-8")


def v2_write_result(repository: Path, junit: str, content: bytes) -> None:
    path = repository / junit
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(content)
    os.utime(path, ns=(V2_RESULT_MTIME_NS, V2_RESULT_MTIME_NS))


def build_v2_repository(tmp_path: Path, *, gitlinks: bool = True) -> dict[str, object]:
    """A hermetic committed candidate carrying the real Story 7.1 contract."""
    repository = tmp_path / "v2-candidate"
    repository.mkdir(parents=True)
    v2_git(repository, "init")
    (repository / ".gitignore").write_text("artifacts/\n", encoding="utf-8")
    for relative, source in (
        (V2_CONTRACT_PATH, STORY_7_1_CONTRACT),
        (V2_BUNDLE_PATH, V2_AUTHORITY_BUNDLE),
    ):
        target = repository / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(source.read_bytes())
    target = repository / V2_TARGET_PATH
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text("# fixture scenario target\n", encoding="utf-8")
    if gitlinks:
        (repository / ".gitmodules").write_text(
            "".join(
                f'[submodule "{path}"]\n\tpath = {path}\n'
                f"\turl = https://example.invalid/{path.rsplit('/', 1)[1]}.git\n"
                for path in V2_DECLARATION_ORDER
            ),
            encoding="utf-8",
        )
    v2_git(repository, "add", "--all")
    if gitlinks:
        for path in V2_DECLARATION_ORDER:
            # An uninitialized submodule checkout: the gitlink exists, its
            # directory is empty, and nothing is ever cloned or traversed.
            (repository / path).mkdir(parents=True)
            v2_git(
                repository,
                "update-index",
                "--add",
                "--cacheinfo",
                f"160000,{v2_gitlink_commit(path)},{path}",
            )
    v2_git(repository, "commit", "-m", "story 7.1 fixture candidate")
    candidate = v2_git(repository, "rev-parse", "HEAD").stdout.strip()
    results = {}
    for scenario_id, selector, junit in v2_pytest_scenarios():
        v2_write_result(repository, junit, v2_junit(selector))
        results[scenario_id] = junit
    return {"repository": repository, "candidate": candidate, "results": results}


def v2_arguments(repository: Path, *overrides: str, replace: bool = False) -> list[str]:
    if replace:
        return ["--repository", str(repository), *overrides]
    return [
        "--repository",
        str(repository),
        "--contract",
        V2_CONTRACT_PATH,
        "--format",
        "bundle",
        "--output-json",
        V2_OUTPUT_JSON,
        "--output-markdown",
        V2_OUTPUT_MARKDOWN,
        *overrides,
    ]


def v2_run(arguments: list[str]) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(
        [sys.executable, str(SCRIPT), *arguments],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        timeout=120,
    )


def v2_outputs(repository: Path) -> tuple[bytes | None, bytes | None]:
    return tuple(
        (repository / path).read_bytes() if (repository / path).exists() else None
        for path in (V2_OUTPUT_JSON, V2_OUTPUT_MARKDOWN)
    )


def v2_snapshot(repository: Path) -> tuple[str | None, dict[str, tuple]]:
    """HEAD plus every non-.git file's bytes, symlink target, and result-file mtime."""
    head = subprocess.run(
        ["git", "-C", str(repository), "rev-parse", "--verify", "-q", "HEAD"],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
    ).stdout.strip() or None
    files: dict[str, tuple] = {}
    for path in sorted(repository.rglob("*")):
        relative = path.relative_to(repository).as_posix()
        if relative == ".git" or relative.startswith(".git/"):
            continue
        if path.is_symlink():
            files[relative] = ("symlink", os.readlink(path))
        elif path.is_file():
            # Result-file mtimes are evidence (staleness); other mtimes are not.
            mtime = path.stat().st_mtime_ns if relative.startswith("artifacts/") else None
            files[relative] = ("file", path.read_bytes(), mtime)
    return head, files


def v2_failure_validator() -> jsonschema.Draft202012Validator:
    return v2_schema_contract_validator(v2_schema_contract_load(V2_FAILURE_SCHEMA))


def v2_assert_failure(
    result: subprocess.CompletedProcess[bytes],
    expected: set[str],
    *,
    exit_code: int = 1,
) -> dict:
    """A failure is schema-valid, payload-free, traceback-free, and never a record."""
    document = json.loads(result.stdout.decode("utf-8"))
    v2_failure_validator().validate(document)
    assert result.returncode == exit_code, document
    assert document["exitCode"] == exit_code
    assert document["result"] == ("FAIL" if exit_code == 1 else "BLOCKED")
    assert expected <= set(document["blockers"]), document
    assert document["blockers"] == list(
        dict.fromkeys(item["code"] for item in document["diagnostics"])
    )
    assert "schemaVersion" in document
    assert document["schemaVersion"] != "hexalith.conversations.story-final-record.v2"
    assert result.stderr == b""
    assert b"Traceback" not in result.stdout
    return document


def v2_json_digest(record: dict) -> str:
    """Pinned rule: SHA-256 of the canonical JSON with all three digest fields zeroed."""
    draft = deepcopy(record)
    draft["outputs"]["json"]["sha256"] = V2_ZERO
    draft["outputs"]["markdown"]["sha256"] = V2_ZERO
    draft["renderedMarkdownSha256"] = V2_ZERO
    rendered = json.dumps(draft, indent=2, ensure_ascii=False) + "\n"
    return hashlib.sha256(rendered.encode("utf-8")).hexdigest()


def v2_assert_pass(repository: Path, result: subprocess.CompletedProcess[bytes]) -> dict:
    assert result.returncode == 0, result.stdout.decode("utf-8", "replace")
    assert result.stderr == b""
    json_bytes, markdown_bytes = v2_outputs(repository)
    assert result.stdout == json_bytes
    record = json.loads(json_bytes.decode("utf-8"))
    v2_schema_contract_validator(v2_schema_contract_load(FINAL_RECORD_SCHEMA)).validate(
        record
    )
    json_digest = v2_json_digest(record)
    markdown_digest = hashlib.sha256(markdown_bytes).hexdigest()
    assert record["outputs"]["json"]["sha256"] == json_digest
    assert record["outputs"]["markdown"]["sha256"] == markdown_digest
    assert record["renderedMarkdownSha256"] == markdown_digest
    assert f"`{json_digest}`".encode("ascii") in markdown_bytes
    assert b"\r" not in json_bytes and b"\r" not in markdown_bytes
    assert json_bytes.endswith(b"\n") and markdown_bytes.endswith(b"\n")
    json_bytes.decode("utf-8")
    markdown_bytes.decode("utf-8")
    return record


# ---- AC-7.1-02: v2_deterministic_bundle ------------------------------------ #


def test_v2_deterministic_bundle_is_byte_identical_schema_valid_and_cross_bound(
    tmp_path: Path,
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    first = v2_run(v2_arguments(repository))
    record = v2_assert_pass(repository, first)
    first_outputs = v2_outputs(repository)
    second = v2_run(v2_arguments(repository))
    v2_assert_pass(repository, second)
    assert v2_outputs(repository) == first_outputs
    assert second.stdout == first.stdout

    contract = v2_contract()
    assert record["storyId"] == "7.1"
    assert record["candidate"]["commit"] == fixture["candidate"]
    assert [item["path"] for item in record["candidate"]["gitlinks"]] == list(
        ROOT_GITLINK_PATHS
    )
    assert record["candidate"]["gitlinks"] == [
        {"path": path, "commit": v2_gitlink_commit(path), "mode": "160000"}
        for path in ROOT_GITLINK_PATHS
    ]
    bundle = json.loads(V2_AUTHORITY_BUNDLE.read_text(encoding="utf-8"))
    recomputed_bundle = hashlib.sha256(
        "".join(f"{row['sha256']}  {row['path']}\n" for row in bundle["artifacts"]).encode(
            "utf-8"
        )
    ).hexdigest()
    assert record["authority"] == {
        "epic": contract["authority"]["epic"],
        "architecture": contract["authority"]["architecture"],
        "planningCandidate": contract["authority"]["planningCandidate"],
        "bundleDigest": recomputed_bundle,
    }
    assert recomputed_bundle == HOLD_BUNDLE_DIGEST
    assert record["inventory"] == contract["inventory"]
    assert record["inventory"]["sha256"] == v2_schema_contract_inventory_digest(
        list(STORY_7_1_INVENTORY_ITEMS)
    )
    assert record["predecessors"] == contract["predecessors"]
    assert record["rollback"] == contract["rollback"]
    assert record["summary"] == {
        "required": 6,
        "passed": 6,
        "failed": 0,
        "blocked": 0,
        "skipped": 0,
        "notRun": 0,
    }
    assert record["outputs"]["json"]["path"] == V2_OUTPUT_JSON
    assert record["outputs"]["markdown"]["path"] == V2_OUTPUT_MARKDOWN
    assert [item["scenarioId"] for item in record["scenarios"]] == [
        item["id"] for item in contract["scenarios"]
    ]
    for scenario, declared in zip(record["scenarios"], contract["scenarios"]):
        assert scenario["command"] == declared["command"]
        assert (scenario["exitCode"], scenario["result"], scenario["blockers"]) == (
            0,
            "PASS",
            [],
        )
        assert scenario["assertionLedger"]
        assert [entry["id"] for entry in scenario["assertionLedger"]] == [
            f"{declared['id']}#{ordinal:04d}"
            for ordinal in range(1, len(scenario["assertionLedger"]) + 1)
        ]
        assert {entry["state"] for entry in scenario["assertionLedger"]} == {"PASS"}
    for scenario_id, selector, junit in v2_pytest_scenarios():
        scenario = next(item for item in record["scenarios"] if item["scenarioId"] == scenario_id)
        assert scenario["resultFile"] == {
            "path": junit,
            "sha256": sha256_file(repository / junit),
        }
        assert [entry["subject"] for entry in scenario["assertionLedger"]] == [
            f"{V2_TARGET_MODULE}::test_{selector}_first",
            f"{V2_TARGET_MODULE}::test_{selector}_second",
        ]
    assert "resultFile" not in record["scenarios"][5]
    assert record["faultInjection"] == {"results": []}
    assert load_generator().v2_verify_pair(*first_outputs) == []


def test_v2_deterministic_bundle_is_identical_across_rebuilt_fixtures(
    tmp_path: Path,
) -> None:
    left = build_v2_repository(tmp_path / "left")
    right = build_v2_repository(tmp_path / "right")
    assert left["candidate"] == right["candidate"]
    for fixture in (left, right):
        v2_assert_pass(fixture["repository"], v2_run(v2_arguments(fixture["repository"])))
    assert v2_outputs(left["repository"]) == v2_outputs(right["repository"])


def test_v2_deterministic_bundle_rendering_drift_is_record_content_drift(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    sentinel = b"previous output\n"
    for path in (V2_OUTPUT_JSON, V2_OUTPUT_MARKDOWN):
        (repository / path).parent.mkdir(parents=True, exist_ok=True)
        (repository / path).write_bytes(sentinel)
    before = v2_snapshot(repository)
    module = load_generator()
    original = module.v2_render_markdown
    calls = iter(range(1_000))

    def drifting(record: dict, json_digest: str) -> str:
        return original(record, json_digest) + f"<!-- drift {next(calls)} -->\n"

    monkeypatch.setattr(module, "v2_render_markdown", drifting)
    assert module.main(v2_arguments(repository)) == 1
    document = json.loads(capsys.readouterr().out)
    v2_failure_validator().validate(document)
    assert "RECORD_CONTENT_DRIFT" in document["blockers"]
    assert v2_snapshot(repository) == before


def test_v2_deterministic_bundle_verifier_detects_tampered_pairs(tmp_path: Path) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))
    json_bytes, markdown_bytes = v2_outputs(repository)
    module = load_generator()
    assert module.v2_verify_pair(json_bytes, markdown_bytes) == []
    assert module.v2_verify_pair(json_bytes, markdown_bytes + b"edited\n")
    tampered = json.loads(json_bytes)
    tampered["summary"]["passed"] = 5
    assert module.v2_verify_pair(
        (json.dumps(tampered, indent=2, ensure_ascii=False) + "\n").encode("utf-8"),
        markdown_bytes,
    )
    assert module.v2_verify_pair(json_bytes.replace(b"\n", b"\r\n"), markdown_bytes)


# ---- AC-7.1-03: v2_rejects_caller_authored_facts ---------------------------- #

V2_CALLER_FACTS = (
    ("--passed", "6"),
    ("--summary=6/6/0/0/0/0",),
    ("--test-results", "AC-7.1-01=artifacts/v9/7.1/AC-7.1-01.xml"),
    ("--candidate", "HEAD"),
    ("--commit", "0" * 40),
    ("--changed-path", "docs/runbooks/story-final-record-generation.md"),
    ("--gitlink", "references/Hexalith.Builds=" + "0" * 40),
    ("--result", "PASS"),
    ("--verdict=PASS",),
)


@pytest.mark.parametrize("fact", V2_CALLER_FACTS, ids=lambda fact: fact[0].split("=")[0])
def test_v2_rejects_caller_authored_facts(tmp_path: Path, fact: tuple[str, ...]) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    document = v2_assert_failure(
        v2_run(v2_arguments(repository, *fact)), {"CALLER_AUTHORED_FACT"}
    )
    assert fact[0].split("=")[0] in {item["subject"] for item in document["diagnostics"]}
    assert v2_outputs(repository) == (None, None)
    assert v2_snapshot(repository) == before


def test_v2_rejects_caller_authored_facts_in_output_paths(tmp_path: Path) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    arguments = v2_arguments(
        repository,
        "--contract",
        V2_CONTRACT_PATH,
        "--format",
        "bundle",
        "--output-json",
        "docs/release-evidence/caller-chosen.json",
        "--output-markdown",
        V2_OUTPUT_MARKDOWN,
        replace=True,
    )
    v2_assert_failure(v2_run(arguments), {"CALLER_AUTHORED_FACT"})
    assert not (repository / "docs/release-evidence/caller-chosen.json").exists()
    assert v2_snapshot(repository) == before


def test_v2_rejects_caller_authored_facts_before_any_passing_record(tmp_path: Path) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))
    passing = v2_outputs(repository)
    v2_assert_failure(
        v2_run(v2_arguments(repository, "--passed", "6", "--failed", "0")),
        {"CALLER_AUTHORED_FACT"},
    )
    assert v2_outputs(repository) == passing


# ---- AC-7.1-04: v2_rejects_empty_derivation --------------------------------- #


def test_v2_rejects_empty_derivation_without_any_parsed_result(tmp_path: Path) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    saved = {junit: (repository / junit).read_bytes() for junit in fixture["results"].values()}
    try:
        for junit in saved:
            (repository / junit).unlink()
        v2_assert_failure(
            v2_run(v2_arguments(repository)),
            {"RECORD_NOT_DERIVED", "ASSERTION_LEDGER_EMPTY", "TEST_RESULTS_MISSING"},
        )
        assert v2_outputs(repository) == (None, None)
    finally:
        for junit, content in saved.items():
            v2_write_result(repository, junit, content)
    assert v2_snapshot(repository) == before
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))


def test_v2_rejects_empty_derivation_without_any_executed_assertion(tmp_path: Path) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    saved = {junit: (repository / junit).read_bytes() for junit in fixture["results"].values()}
    try:
        for _, selector, junit in v2_pytest_scenarios():
            v2_write_result(repository, junit, v2_junit(selector, cases=[]))
        document = v2_assert_failure(
            v2_run(v2_arguments(repository)),
            {"RECORD_NOT_DERIVED", "ASSERTION_LEDGER_EMPTY"},
        )
        assert "TEST_RESULTS_MISSING" not in document["blockers"]
        assert v2_outputs(repository) == (None, None)
    finally:
        for junit, content in saved.items():
            v2_write_result(repository, junit, content)
    assert v2_snapshot(repository) == before
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))


def test_v2_rejects_empty_derivation_without_a_resolved_candidate(tmp_path: Path) -> None:
    repository = tmp_path / "unborn"
    repository.mkdir()
    v2_git(repository, "init")
    target = repository / V2_CONTRACT_PATH
    target.parent.mkdir(parents=True)
    target.write_bytes(STORY_7_1_CONTRACT.read_bytes())
    before = v2_snapshot(repository)
    v2_assert_failure(
        v2_run(v2_arguments(repository)),
        {"RECORD_NOT_DERIVED", "ASSERTION_LEDGER_EMPTY"},
    )
    assert v2_snapshot(repository) == before


def test_v2_rejects_empty_derivation_without_a_derived_gitlink_path(tmp_path: Path) -> None:
    fixture = build_v2_repository(tmp_path, gitlinks=False)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    v2_assert_failure(v2_run(v2_arguments(repository)), {"RECORD_NOT_DERIVED"})
    assert v2_snapshot(repository) == before


def test_v2_rejects_empty_derivation_for_one_empty_scenario(tmp_path: Path) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    _, selector, junit = v2_pytest_scenarios()[2]
    original = (repository / junit).read_bytes()
    try:
        v2_write_result(repository, junit, v2_junit(selector, cases=[]))
        document = v2_assert_failure(
            v2_run(v2_arguments(repository)), {"ASSERTION_LEDGER_EMPTY"}
        )
        assert {item["subject"] for item in document["diagnostics"]} >= {"AC-7.1-03"}
    finally:
        v2_write_result(repository, junit, original)
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))


# ---- AC-7.1-05: v2_malformed_input_is_schema_valid_failure ------------------ #

V2_PAYLOAD_SENTINEL = "PAYLOAD-SENTINEL-4b1d"


def v2_commit_contract(repository: Path, content: bytes) -> None:
    (repository / V2_CONTRACT_PATH).write_bytes(content)
    v2_git(repository, "add", V2_CONTRACT_PATH)
    v2_git(repository, "commit", "-m", "mutated contract")


@pytest.mark.parametrize(
    "content",
    (
        b'{"schemaVersion": "' + V2_PAYLOAD_SENTINEL.encode("ascii") + b'", ',
        json.dumps(
            dict(v2_contract(), schemaVersion="hexalith.conversations.story-contract.v9")
            | {"storyId": V2_PAYLOAD_SENTINEL}
        ).encode("utf-8"),
        json.dumps(v2_contract() | {"undeclared": V2_PAYLOAD_SENTINEL}).encode("utf-8"),
        b'{"storyId": "7.1", "storyId": "' + V2_PAYLOAD_SENTINEL.encode("ascii") + b'"}',
    ),
    ids=("malformed-json", "unknown-schema-identity", "closed-schema-violation", "duplicate-key"),
)
def test_v2_malformed_input_is_schema_valid_failure_for_contracts(
    tmp_path: Path, content: bytes
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    try:
        v2_commit_contract(repository, content)
        result = v2_run(v2_arguments(repository))
        v2_assert_failure(result, {"INPUT_SCHEMA_INVALID"})
        assert V2_PAYLOAD_SENTINEL.encode("ascii") not in result.stdout + result.stderr
        assert v2_outputs(repository) == (None, None)
    finally:
        v2_git(repository, "reset", "--hard", "-q", str(fixture["candidate"]))
    assert v2_snapshot(repository) == before
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))


@pytest.mark.parametrize(
    "arguments",
    (
        ("--format", "markdown"),
        ("--bogus-" + V2_PAYLOAD_SENTINEL.lower(), "x"),
        ("positional-" + V2_PAYLOAD_SENTINEL,),
        ("--contract", V2_CONTRACT_PATH),
        ("--output-json",),
        ("--contract=",),
    ),
    ids=(
        "unsupported-format",
        "unknown-option",
        "positional",
        "duplicate-option",
        "missing-value",
        "empty-value",
    ),
)
def test_v2_malformed_input_is_schema_valid_failure_for_arguments(
    tmp_path: Path, arguments: tuple[str, ...]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    result = v2_run(v2_arguments(repository, *arguments))
    v2_assert_failure(result, {"ARGUMENT_INVALID"})
    assert V2_PAYLOAD_SENTINEL.encode("ascii") not in result.stdout + result.stderr
    assert V2_PAYLOAD_SENTINEL.lower().encode("ascii") not in result.stdout
    assert v2_snapshot(repository) == before


def test_v2_malformed_input_is_schema_valid_failure_for_missing_arguments(
    tmp_path: Path,
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    arguments = v2_arguments(repository, "--contract", V2_CONTRACT_PATH, replace=True)
    document = v2_assert_failure(v2_run(arguments), {"ARGUMENT_INVALID"})
    assert {"--output-json", "--output-markdown"} <= {
        item["subject"] for item in document["diagnostics"]
    }
    v2_assert_failure(
        v2_run(v2_arguments(tmp_path / "missing-repository")), {"ARGUMENT_INVALID"}
    )
    v2_assert_failure(
        v2_run(v2_arguments(repository, "--contract", "../escape.json", replace=True)),
        {"ARGUMENT_INVALID"},
    )


@pytest.mark.parametrize(
    "content",
    (
        b"<testsuites><testsuite " + V2_PAYLOAD_SENTINEL.encode("ascii"),
        b'<?xml version="1.0"?><!DOCTYPE lol [<!ENTITY a "' + V2_PAYLOAD_SENTINEL.encode(
            "ascii"
        ) + b'">]><testsuites />',
        b'<testsuites><testsuite tests="0" failures="0" errors="0" skipped="0">'
        b'<testsuite tests="0" failures="0" errors="0" skipped="0" /></testsuite></testsuites>',
    ),
    ids=("malformed-xml", "doctype", "nested-suite"),
)
def test_v2_malformed_input_is_schema_valid_failure_for_results(
    tmp_path: Path, content: bytes
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    junit = fixture["results"]["AC-7.1-02"]
    original = (repository / junit).read_bytes()
    before = v2_snapshot(repository)
    try:
        v2_write_result(repository, junit, content)
        result = v2_run(v2_arguments(repository))
        v2_assert_failure(result, {"INPUT_SCHEMA_INVALID"})
        assert V2_PAYLOAD_SENTINEL.encode("ascii") not in result.stdout + result.stderr
    finally:
        v2_write_result(repository, junit, original)
    assert v2_snapshot(repository) == before
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))


def test_v2_malformed_input_is_schema_valid_failure_schema_is_closed() -> None:
    schema = v2_schema_contract_load(V2_FAILURE_SCHEMA)
    v2_schema_contract_validator(schema)
    v2_schema_contract_assert_closed(schema)
    assert schema["properties"]["schemaVersion"]["const"] == (
        "hexalith.conversations.story-record-generator-failure.v1"
    )
    assert schema["$id"].startswith("https://hexalith.io/schemas/conversations/")
    validator = v2_failure_validator()
    valid = {
        "schemaVersion": "hexalith.conversations.story-record-generator-failure.v1",
        "result": "FAIL",
        "exitCode": 1,
        "blockers": ["ARGUMENT_INVALID"],
        "diagnostics": [
            {"code": "ARGUMENT_INVALID", "subject": "argv", "message": "fixture"}
        ],
    }
    validator.validate(valid)
    for mutation in (
        {"exitCode": 2},
        {"result": "PASS", "exitCode": 0},
        {"blockers": []},
        {"diagnostics": []},
        {"record": {}},
        {"storyId": "6.8"},
        {"diagnostics": [{"code": "X", "subject": "argv", "message": "line\nbreak"}]},
    ):
        assert v2_schema_contract_reject(schema, valid | mutation) == OUTPUT_SCHEMA_INVALID
    record_schema = v2_schema_contract_load(FINAL_RECORD_SCHEMA)
    record = v2_schema_contract_final_record()
    record["scenarios"][0]["assertionLedger"] = []
    assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID
    record["scenarios"][0]["assertionLedger"] = [
        {"id": "AC-7.1-01#1", "subject": "x", "state": "PASS"}
    ]
    assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID


# ---- Fault injection and byte-identical restoration ------------------------- #


def v2_fault_result(repository: Path, fixture: dict, scenario: int, content: bytes) -> None:
    _, _, junit = v2_pytest_scenarios()[scenario]
    v2_write_result(repository, junit, content)


def v2_fault_stale(repository: Path, fixture: dict) -> None:
    junit = fixture["results"]["AC-7.1-04"]
    stale = (V2_FIXED_EPOCH - 1) * 1_000_000_000
    os.utime(repository / junit, ns=(stale, stale))


def v2_fault_dirt(repository: Path, fixture: dict) -> None:
    (repository / "unrelated-dirt.txt").write_text("dirt\n", encoding="utf-8")


def v2_fault_dirty_contract(repository: Path, fixture: dict) -> None:
    path = repository / V2_CONTRACT_PATH
    path.write_bytes(path.read_bytes() + b"\n")


def v2_fault_symlink(repository: Path, fixture: dict) -> None:
    junit = repository / fixture["results"]["AC-7.1-05"]
    outside = repository.parent / "outside.xml"
    outside.write_bytes(junit.read_bytes())
    junit.unlink()
    junit.symlink_to(outside)


def v2_fault_commit(mutate):
    def apply(repository: Path, fixture: dict) -> None:
        mutate(repository)  # stages exactly its own change; outputs stay untracked
        v2_git(repository, "commit", "-q", "-m", "fault")
        for junit in fixture["results"].values():
            os.utime(repository / junit, ns=(V2_RESULT_MTIME_NS, V2_RESULT_MTIME_NS))

    return apply


def v2_mutate_bundle_digest(repository: Path) -> None:
    path = repository / V2_BUNDLE_PATH
    bundle = json.loads(path.read_text(encoding="utf-8"))
    bundle["artifacts"][0]["sha256"] = "f" * 64
    path.write_text(json.dumps(bundle, indent=2) + "\n", encoding="utf-8")
    v2_git(repository, "add", V2_BUNDLE_PATH)


def v2_mutate_extra_gitlink(repository: Path) -> None:
    v2_git(
        repository,
        "update-index",
        "--add",
        "--cacheinfo",
        f"160000,{v2_gitlink_commit('extra')},references/Hexalith.Undeclared",
    )


def v2_mutate_gitlink_mode(repository: Path) -> None:
    path = "references/Hexalith.Builds"
    v2_git(repository, "rm", "-q", "--cached", path)
    (repository / path).rmdir()
    (repository / path).write_text("not a gitlink\n", encoding="utf-8")
    v2_git(repository, "add", path)


def v2_mutate_bundle(mutate):
    """Rewrite the committed V9 bundle with its digest recomputed from its own rows."""

    def apply(repository: Path) -> None:
        path = repository / V2_BUNDLE_PATH
        bundle = json.loads(path.read_text(encoding="utf-8"))
        mutate(bundle)
        bundle["bundleDigest"] = hashlib.sha256(
            "".join(f"{row['sha256']}  {row['path']}\n" for row in bundle["artifacts"]).encode(
                "utf-8"
            )
        ).hexdigest()
        path.write_text(json.dumps(bundle, indent=2) + "\n", encoding="utf-8")
        v2_git(repository, "add", V2_BUNDLE_PATH)

    return apply


def v2_swap_rows(bundle: dict) -> None:
    rows = bundle["artifacts"]
    rows[0], rows[1] = rows[1], rows[0]


def v2_duplicate_row(bundle: dict) -> None:
    bundle["artifacts"][1]["path"] = bundle["artifacts"][0]["path"]


def v2_remove_bundle(repository: Path) -> None:
    v2_git(repository, "rm", "-q", V2_BUNDLE_PATH)


def v2_mutate_contract(mutate):
    """Commit a still schema-valid contract whose scenario commands were edited."""

    def apply(repository: Path) -> None:
        path = repository / V2_CONTRACT_PATH
        contract = json.loads(path.read_text(encoding="utf-8"))
        mutate(contract["scenarios"])
        v2_schema_contract_validator(v2_schema_contract_load(STORY_CONTRACT_SCHEMA)).validate(
            contract
        )
        path.write_text(json.dumps(contract, indent=2) + "\n", encoding="utf-8")
        v2_git(repository, "add", V2_CONTRACT_PATH)

    return apply


def v2_self_not_last(scenarios: list[dict]) -> None:
    scenarios[4], scenarios[5] = scenarios[5], scenarios[4]


def v2_self_output_mismatch(scenarios: list[dict]) -> None:
    scenarios[5]["command"] = scenarios[5]["command"].replace(
        V2_OUTPUT_JSON, "docs/release-evidence/elsewhere.json"
    )


def v2_uncommitted_target(scenarios: list[dict]) -> None:
    scenarios[0]["command"] = scenarios[0]["command"].replace(
        V2_TARGET_PATH, "_bmad/scripts/tests/test_not_committed.py"
    )


def v2_shared_junit(scenarios: list[dict]) -> None:
    scenarios[1]["command"] = scenarios[1]["command"].replace(
        "AC-7.1-02.xml", "AC-7.1-01.xml"
    )


V2_FAULTS = {
    "failing-testcase": (
        lambda repository, fixture: v2_fault_result(
            repository,
            fixture,
            1,
            v2_junit(
                "v2_deterministic_bundle",
                [("test_v2_deterministic_bundle_a", None), ("test_v2_deterministic_bundle_b", "failure")],
            ),
        ),
        "TEST_RESULTS_FAILED",
    ),
    "erroring-testcase": (
        lambda repository, fixture: v2_fault_result(
            repository,
            fixture,
            1,
            v2_junit("v2_deterministic_bundle", [("test_v2_deterministic_bundle_a", "error")]),
        ),
        "TEST_RESULTS_FAILED",
    ),
    "skipped-testcase": (
        lambda repository, fixture: v2_fault_result(
            repository,
            fixture,
            0,
            v2_junit(
                "v2_schema_contract",
                [("test_v2_schema_contract_a", None), ("test_v2_schema_contract_b", "skipped")],
            ),
        ),
        "TEST_SKIP_NOT_ALLOWED",
    ),
    "counter-disagreement": (
        lambda repository, fixture: v2_fault_result(
            repository,
            fixture,
            3,
            v2_junit("v2_rejects_empty_derivation", counters={"tests": 7}),
        ),
        "TEST_COUNT_INCONSISTENT",
    ),
    "foreign-selector": (
        lambda repository, fixture: v2_fault_result(
            repository, fixture, 4, v2_junit("v2_schema_contract")
        ),
        "SCENARIO_RESULT_MISMATCH",
    ),
    "foreign-module": (
        lambda repository, fixture: v2_fault_result(
            repository,
            fixture,
            4,
            v2_junit("v2_malformed_input_is_schema_valid_failure", classname="tests.other"),
        ),
        "SCENARIO_RESULT_MISMATCH",
    ),
    "stale-result": (v2_fault_stale, "TEST_RESULTS_STALE"),
    "symlinked-result": (v2_fault_symlink, "INPUT_SCHEMA_INVALID"),
    "unrelated-dirt": (v2_fault_dirt, "WORKTREE_NOT_CLEAN"),
    "uncommitted-contract-edit": (v2_fault_dirty_contract, "WORKTREE_NOT_CLEAN"),
    "bundle-digest-drift": (v2_fault_commit(v2_mutate_bundle_digest), "AUTHORITY_BINDING_INVALID"),
    "undeclared-gitlink": (v2_fault_commit(v2_mutate_extra_gitlink), "GITLINK_INVENTORY_DRIFT"),
    "non-gitlink-mode": (v2_fault_commit(v2_mutate_gitlink_mode), "GITLINK_INVENTORY_DRIFT"),
    "bundle-planning-candidate-drift": (
        v2_fault_commit(
            v2_mutate_bundle(lambda bundle: bundle.update(planningCandidate="0" * 40))
        ),
        "AUTHORITY_BINDING_INVALID",
    ),
    "bundle-rows-unsorted": (
        v2_fault_commit(v2_mutate_bundle(v2_swap_rows)),
        "AUTHORITY_BINDING_INVALID",
    ),
    "bundle-rows-duplicated": (
        v2_fault_commit(v2_mutate_bundle(v2_duplicate_row)),
        "AUTHORITY_BINDING_INVALID",
    ),
    "bundle-missing": (v2_fault_commit(v2_remove_bundle), "AUTHORITY_BINDING_INVALID"),
    "self-invocation-not-last": (
        v2_fault_commit(v2_mutate_contract(v2_self_not_last)),
        "SCENARIO_COMMAND_UNSUPPORTED",
    ),
    "self-invocation-output-mismatch": (
        v2_fault_commit(v2_mutate_contract(v2_self_output_mismatch)),
        "SCENARIO_RESULT_MISMATCH",
    ),
    "uncommitted-pytest-target": (
        v2_fault_commit(v2_mutate_contract(v2_uncommitted_target)),
        "SCENARIO_COMMAND_UNSUPPORTED",
    ),
    "shared-junit-path": (
        v2_fault_commit(v2_mutate_contract(v2_shared_junit)),
        "SCENARIO_RESULT_MISMATCH",
    ),
}


@pytest.mark.parametrize("fault", sorted(V2_FAULTS))
def test_v2_fault_injection_blocks_and_restores_byte_identically(
    tmp_path: Path, fault: str
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))
    baseline_outputs = v2_outputs(repository)
    before = v2_snapshot(repository)
    mutate, expected = V2_FAULTS[fault]
    try:
        mutate(repository, fixture)
        v2_assert_failure(v2_run(v2_arguments(repository)), {expected})
        assert v2_outputs(repository) == baseline_outputs
    finally:
        v2_git(repository, "reset", "--hard", "-q", str(fixture["candidate"]))
        (repository / "unrelated-dirt.txt").unlink(missing_ok=True)
        (repository.parent / "outside.xml").unlink(missing_ok=True)
        for path in V2_DECLARATION_ORDER:
            (repository / path).mkdir(parents=True, exist_ok=True)
        for scenario_id, selector, junit in v2_pytest_scenarios():
            (repository / junit).unlink(missing_ok=True)
            v2_write_result(repository, junit, v2_junit(selector))
        for path, content in zip((V2_OUTPUT_JSON, V2_OUTPUT_MARKDOWN), baseline_outputs):
            (repository / path).write_bytes(content)
    assert v2_snapshot(repository) == before
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))
    assert v2_outputs(repository) == baseline_outputs


# ---- BLOCKED class, output containment, and output rollback ---------------- #


def v2_in_process(
    module, repository: Path, capsys: pytest.CaptureFixture[str], exit_code: int
) -> dict:
    assert module.main(v2_arguments(repository)) == exit_code
    captured = capsys.readouterr()
    assert captured.err == ""
    assert "Traceback" not in captured.out
    document = json.loads(captured.out)
    v2_failure_validator().validate(document)
    assert document["exitCode"] == exit_code
    assert document["result"] == ("FAIL" if exit_code == 1 else "BLOCKED")
    return document


def v2_seed_outputs(repository: Path) -> tuple[bytes, bytes]:
    pair = (b"previous json\n", b"previous markdown\n")
    for path, content in zip((V2_OUTPUT_JSON, V2_OUTPUT_MARKDOWN), pair):
        (repository / path).parent.mkdir(parents=True, exist_ok=True)
        (repository / path).write_bytes(content)
    return pair


def test_v2_malformed_input_is_schema_valid_failure_when_schemas_are_unavailable(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    module = load_generator()
    empty = tmp_path / "no-schemas"
    empty.mkdir()
    monkeypatch.setattr(module, "V2_SCHEMA_DIRECTORY", empty)
    document = v2_in_process(module, repository, capsys, 2)
    assert document["blockers"] == ["SCHEMA_UNAVAILABLE"]
    assert v2_snapshot(repository) == before


def test_v2_malformed_input_is_schema_valid_failure_when_git_fails(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    module = load_generator()
    original = module.run_git

    def failing(repository_path: Path, *arguments: str, **options):
        if arguments[:2] == ("ls-tree", "-r"):
            raise module.GateError("GIT_COMMAND_FAILED", "forced fixture failure")
        return original(repository_path, *arguments, **options)

    monkeypatch.setattr(module, "run_git", failing)
    document = v2_in_process(module, repository, capsys, 2)
    assert document["blockers"] == ["GIT_COMMAND_FAILED"]
    assert "forced fixture failure" not in json.dumps(document)
    assert v2_snapshot(repository) == before


def test_v2_malformed_input_is_schema_valid_failure_for_a_symlinked_output_leaf(
    tmp_path: Path,
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    decoy = repository / "docs/release-evidence/decoy.json"
    decoy.parent.mkdir(parents=True)
    decoy.write_bytes(b"decoy\n")
    leaf = repository / V2_OUTPUT_JSON
    leaf.symlink_to("decoy.json")
    v2_git(repository, "add", "docs/release-evidence/decoy.json")
    v2_git(repository, "commit", "-q", "-m", "decoy")
    for junit in fixture["results"].values():
        os.utime(repository / junit, ns=(V2_RESULT_MTIME_NS, V2_RESULT_MTIME_NS))
    before = v2_snapshot(repository)
    v2_assert_failure(v2_run(v2_arguments(repository)), {"OUTPUT_PATH_INVALID"})
    assert leaf.is_symlink() and os.readlink(leaf) == "decoy.json"
    assert decoy.read_bytes() == b"decoy\n"
    assert not (repository / V2_OUTPUT_MARKDOWN).exists()
    assert v2_snapshot(repository) == before


def test_v2_malformed_input_is_schema_valid_failure_when_the_second_replace_fails(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    pair = v2_seed_outputs(repository)
    before = v2_snapshot(repository)
    module = load_generator()
    original = os.replace
    staged: list[str] = []

    def failing_second(source, target) -> None:
        if str(source).endswith(".tmp"):
            staged.append(str(target))
            if len(staged) == 2:
                raise OSError("forced second replace failure")
        original(source, target)

    monkeypatch.setattr(module.os, "replace", failing_second)
    document = v2_in_process(module, repository, capsys, 2)
    assert document["blockers"] == ["OUTPUT_WRITE_FAILED"]
    assert len(staged) == 2
    assert v2_outputs(repository) == pair
    assert v2_snapshot(repository) == before


def test_v2_malformed_input_is_schema_valid_failure_when_read_back_differs(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    pair = v2_seed_outputs(repository)
    before = v2_snapshot(repository)
    module = load_generator()
    original = os.replace

    def concurrent_writer(source, target) -> None:
        original(source, target)
        if str(source).endswith(".tmp") and str(target).endswith(".md"):
            with open(target, "ab") as handle:
                handle.write(b"concurrent edit\n")

    monkeypatch.setattr(module.os, "replace", concurrent_writer)
    document = v2_in_process(module, repository, capsys, 1)
    assert document["blockers"] == ["RECORD_CONTENT_DRIFT"]
    assert v2_outputs(repository) == pair
    assert v2_snapshot(repository) == before


# ---- v1 regression and documentation ---------------------------------------- #


def test_v2_route_leaves_the_v1_route_unchanged(umbrella: dict[str, object]) -> None:
    code, document = measured(umbrella)
    assert code == 0, document["blockers"]
    assert document["schema"] == "story-final-record-v1"
    assert "schemaVersion" not in document
    abbreviated = subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--repository",
            str(umbrella["repository"]),
            "--contr",
            V2_CONTRACT_PATH,
        ],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        text=True,
        timeout=120,
    )
    assert abbreviated.returncode == 2
    legacy = json.loads(abbreviated.stdout)
    assert legacy["schema"] == "story-final-record-v1"
    assert [item["code"] for item in legacy["blockers"]] == ["INVALID_SCOPE"]
    module = load_generator()
    assert module.is_v2_invocation(["--contract", "x"])
    assert module.is_v2_invocation(["--contract=x"])
    assert not module.is_v2_invocation(["--contr", "x", "--story", "--contract-x"])


def test_v2_every_code_is_documented_in_the_runbook() -> None:
    module = load_generator()
    runbook = RUNBOOK.read_text(encoding="utf-8")
    assert "## 9. Contract-bound v2 route" in runbook
    for code, exit_class in module.V2_CODES.items():
        assert exit_class in {"FAIL", "BLOCKED"}
        assert f"`{code}`" in runbook, code
    assert set(module.V2_SCHEMA_FILES.values()) == {
        FINAL_RECORD_SCHEMA.name,
        V2_FAILURE_SCHEMA.name,
        STORY_CONTRACT_SCHEMA.name,
        "v9-authority-bundle-v1.schema.json",
    }


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
