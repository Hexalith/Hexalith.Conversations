# /// script
# requires-python = ">=3.11"
# dependencies = ["pytest>=8.0", "jsonschema>=4.0"]
# ///
"""Hermetic tests for the story final-record generator."""

import ast
import builtins
import hashlib
import io
import json
import os
import re
import shutil
import subprocess
import sys
import time
import unicodedata
from contextlib import redirect_stdout
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
    include_test_ids: bool = False,
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
    if include_test_ids:
        definitions = []
        for index, result in enumerate(results):
            test_id = f"{index:04d}"
            test_name = re.search(r'testName="([^"]+)"', result).group(1)
            results[index] = result.replace(' outcome=', f' testId="{test_id}" outcome=', 1)
            definitions.append(
                f'    <UnitTest name="{test_name}" id="{test_id}">'
                f'<TestMethod codeBase="{code_base}" /></UnitTest>'
            )
        definition_rows = "\n".join(definitions) + "\n"
    else:
        definition_rows = f'    <UnitTest name="fixture"><TestMethod codeBase="{code_base}" /></UnitTest>\n'
    executed = passed + failed
    total = executed + skipped
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        '<?xml version="1.0" encoding="utf-8"?>\n'
        f'<TestRun id="fixture" name="fixture" xmlns="{TRX_NAMESPACE}">\n'
        "  <Results>\n    " + "\n    ".join(results) + "\n  </Results>\n"
        "  <TestDefinitions>\n"
        f"{definition_rows}"
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
    write_trx(outside, code_base=Path("/fixture/Fixture.dll"))
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
        if start < 0:
            # The current-change policy retired this historical V12 gate.
            assert "current-change-validation.md" in content, relative_path
            assert markers[0] not in content, relative_path
            continue
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
                "assertionLedger": [
                    {"id": "AC-7.1-01#0001", "subject": "fixture::assertion", "state": "PASS"}
                ],
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


def v2_restore_schema_fixture(path: Path, original: bytes, metadata: os.stat_result) -> None:
    """Restore a tracked schema without making unrelated test results stale."""
    if path.read_bytes() != original:
        path.write_bytes(original)
    if path.stat().st_mtime_ns != metadata.st_mtime_ns:
        os.utime(path, ns=(metadata.st_atime_ns, metadata.st_mtime_ns))


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
    before = {fixture: (fixture.read_bytes(), fixture.stat()) for fixture in NEW_SCHEMA_FIXTURES}
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
        for fixture, (original, metadata) in before.items():
            v2_restore_schema_fixture(fixture, original, metadata)
    assert all(
        fixture.read_bytes() == original
        and fixture.stat().st_mtime_ns == metadata.st_mtime_ns
        for fixture, (original, metadata) in before.items()
    )


def test_v2_schema_contract_rejects_invalid_bindings() -> None:
    v2_schema_contract_hold_is_lifted()
    acceptance_schema = v2_schema_contract_load(ACCEPTANCE_RESULT_SCHEMA)
    inventory_schema = v2_schema_contract_load(FROZEN_INVENTORY_SCHEMA)
    record_schema = v2_schema_contract_load(FINAL_RECORD_SCHEMA)
    before = {fixture: (fixture.read_bytes(), fixture.stat()) for fixture in NEW_SCHEMA_FIXTURES}
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

        record = v2_schema_contract_final_record()
        record["scenarios"][0].pop("assertionLedger")
        assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID
    finally:
        for fixture, (original, metadata) in before.items():
            v2_restore_schema_fixture(fixture, original, metadata)
    assert all(
        fixture.read_bytes() == original
        and fixture.stat().st_mtime_ns == metadata.st_mtime_ns
        for fixture, (original, metadata) in before.items()
    )


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
        metadata = path.stat()
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
            v2_restore_schema_fixture(path, before, metadata)
        assert path.read_bytes() == before
        assert path.stat().st_mtime_ns == metadata.st_mtime_ns
    assert STORY_CONTRACT_SCHEMA.read_bytes() == story_contract_before
    assert sha256_file(STORY_CONTRACT_SCHEMA) == FROZEN_STORY_CONTRACT_SCHEMA_DIGEST


def test_v2_schema_contract_pass_requires_an_assertion_ledger() -> None:
    v2_schema_contract_hold_is_lifted()
    record_schema = v2_schema_contract_load(FINAL_RECORD_SCHEMA)
    record = v2_schema_contract_final_record()
    record["scenarios"][0]["assertionLedger"] = []
    assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID
    record["scenarios"][0]["assertionLedger"] = [
        {"id": "AC-7.1-01#1", "subject": "x", "state": "PASS"}
    ]
    assert v2_schema_contract_reject(record_schema, record) == OUTPUT_SCHEMA_INVALID


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

    # The digest only binds the Markdown to its own renderer; prove its content.
    markdown_lines = first_outputs[1].decode("utf-8").splitlines()
    for scenario in record["scenarios"]:
        assert any(
            line.startswith(
                f"| `{scenario['scenarioId']}` | `0` | `PASS` | `none` "
                f"| `{len(scenario['assertionLedger'])}` |"
            )
            for line in markdown_lines
        ), scenario["scenarioId"]
        for entry in scenario["assertionLedger"]:
            assert (
                f"| `{entry['id']}` | `{entry['subject']}` | `{entry['state']}` |"
                in markdown_lines
            ), entry["id"]
    assert "| `6` | `6` | `0` | `0` | `0` | `0` |" in markdown_lines


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
    before = v2_snapshot(repository)
    v2_assert_failure(
        v2_run(v2_arguments(repository, "--passed", "6", "--failed", "0")),
        {"CALLER_AUTHORED_FACT"},
    )
    assert v2_outputs(repository) == passing
    assert v2_snapshot(repository) == before


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
    before = v2_snapshot(repository)
    try:
        v2_write_result(repository, junit, v2_junit(selector, cases=[]))
        document = v2_assert_failure(
            v2_run(v2_arguments(repository)), {"ASSERTION_LEDGER_EMPTY"}
        )
        assert {item["subject"] for item in document["diagnostics"]} >= {"AC-7.1-03"}
        assert v2_outputs(repository) == (None, None)
    finally:
        v2_write_result(repository, junit, original)
    assert v2_snapshot(repository) == before
    v2_assert_pass(repository, v2_run(v2_arguments(repository)))


# ---- AC-7.1-05: v2_malformed_input_is_schema_valid_failure ------------------ #

V2_PAYLOAD_SENTINEL = "PAYLOAD-SENTINEL-4b1d"


def v2_contract_bytes(mutate) -> bytes:
    """A contract that still validates against its schema after `mutate`."""
    contract = v2_contract()
    mutate(contract)
    v2_schema_contract_validator(v2_schema_contract_load(STORY_CONTRACT_SCHEMA)).validate(
        contract
    )
    return (json.dumps(contract, indent=2) + "\n").encode("utf-8")


def v2_foreign_scenario_id(contract: dict) -> None:
    contract["scenarios"][4]["id"] = "AC-7.2-05"


def v2_swapped_output_paths(contract: dict) -> None:
    contract["finalRecord"]["paths"].reverse()


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
        v2_contract_bytes(v2_foreign_scenario_id),
        v2_contract_bytes(v2_swapped_output_paths),
    ),
    ids=(
        "malformed-json",
        "unknown-schema-identity",
        "closed-schema-violation",
        "duplicate-key",
        "foreign-scenario-id",
        "swapped-output-paths",
    ),
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
        document = v2_assert_failure(result, {"INPUT_SCHEMA_INVALID"})
        assert document["blockers"] == ["INPUT_SCHEMA_INVALID"]
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
    document = v2_assert_failure(result, {"ARGUMENT_INVALID"})
    assert document["blockers"] == ["ARGUMENT_INVALID"]
    assert V2_PAYLOAD_SENTINEL.encode("ascii") not in result.stdout + result.stderr
    assert V2_PAYLOAD_SENTINEL.lower().encode("ascii") not in result.stdout
    assert v2_snapshot(repository) == before


def test_v2_malformed_input_is_schema_valid_failure_for_missing_arguments(
    tmp_path: Path,
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    arguments = v2_arguments(repository, "--contract", V2_CONTRACT_PATH, replace=True)
    document = v2_assert_failure(v2_run(arguments), {"ARGUMENT_INVALID"})
    assert document["blockers"] == ["ARGUMENT_INVALID"]
    assert {"--output-json", "--output-markdown"} <= {
        item["subject"] for item in document["diagnostics"]
    }
    for arguments in (
        v2_arguments(tmp_path / "missing-repository"),
        v2_arguments(repository, "--contract", "../escape.json", replace=True),
    ):
        document = v2_assert_failure(v2_run(arguments), {"ARGUMENT_INVALID"})
        assert document["blockers"] == ["ARGUMENT_INVALID"]
    assert v2_snapshot(repository) == before


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
        document = v2_assert_failure(result, {"INPUT_SCHEMA_INVALID"})
        assert document["blockers"] == ["INPUT_SCHEMA_INVALID"]
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


def v2_self_repository_mismatch(scenarios: list[dict]) -> None:
    scenarios[5]["command"] = scenarios[5]["command"].replace(
        "--repository .", "--repository ../another-checkout"
    )


def v2_uncommitted_target(scenarios: list[dict]) -> None:
    scenarios[0]["command"] = scenarios[0]["command"].replace(
        V2_TARGET_PATH, "_bmad/scripts/tests/test_not_committed.py"
    )


def v2_shared_junit(scenarios: list[dict]) -> None:
    scenarios[1]["command"] = scenarios[1]["command"].replace(
        "AC-7.1-02.xml", "AC-7.1-01.xml"
    )


def v2_self_contract_mismatch(scenarios: list[dict]) -> None:
    scenarios[5]["command"] = scenarios[5]["command"].replace(
        V2_CONTRACT_PATH, "_bmad-output/planning-artifacts/v9/story-contracts/7.2.json"
    )


def v2_self_markdown_mismatch(scenarios: list[dict]) -> None:
    scenarios[5]["command"] = scenarios[5]["command"].replace(
        V2_OUTPUT_MARKDOWN, "docs/release-evidence/elsewhere.md"
    )


def v2_junit_below_gitlink(scenarios: list[dict]) -> None:
    scenarios[0]["command"] = scenarios[0]["command"].replace(
        "artifacts/v9/7.1/AC-7.1-01.xml", "references/Hexalith.Builds/AC-7.1-01.xml"
    )


def v2_fault_duplicate_testcase(repository: Path, fixture: dict) -> None:
    _, selector, _ = v2_pytest_scenarios()[0]
    name = f"test_{selector}_first"
    v2_fault_result(repository, fixture, 0, v2_junit(selector, [(name, None), (name, None)]))


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
    "self-invocation-repository-mismatch": (
        v2_fault_commit(v2_mutate_contract(v2_self_repository_mismatch)),
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
    "self-invocation-contract-mismatch": (
        v2_fault_commit(v2_mutate_contract(v2_self_contract_mismatch)),
        "SCENARIO_RESULT_MISMATCH",
    ),
    "self-invocation-markdown-mismatch": (
        v2_fault_commit(v2_mutate_contract(v2_self_markdown_mismatch)),
        "SCENARIO_RESULT_MISMATCH",
    ),
    "junit-below-gitlink": (
        v2_fault_commit(v2_mutate_contract(v2_junit_below_gitlink)),
        "SCENARIO_COMMAND_UNSUPPORTED",
    ),
    "duplicate-testcase": (v2_fault_duplicate_testcase, "SCENARIO_RESULT_MISMATCH"),
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


def test_v2_fault_injection_shared_junit_path_is_a_story_level_mismatch(
    tmp_path: Path,
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    v2_fault_commit(v2_mutate_contract(v2_shared_junit))(repository, fixture)
    document = v2_assert_failure(
        v2_run(v2_arguments(repository)), {"SCENARIO_RESULT_MISMATCH"}
    )
    # The shared file also fails AC-7.1-02's selector; require the story-level check.
    assert {
        "code": "SCENARIO_RESULT_MISMATCH",
        "subject": "7.1",
        "message": "two scenarios declare the same JUnit result path",
    } in document["diagnostics"]


@pytest.mark.parametrize(
    "output_json",
    (V2_CONTRACT_PATH, "references/Hexalith.Builds/story-7.1-final-record-v2.json"),
    ids=("aliases-the-contract", "below-a-gitlink"),
)
def test_v2_fault_injection_rejects_an_output_path_that_aliases_an_input_or_a_gitlink(
    tmp_path: Path, output_json: str
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]

    def redirect(contract: dict) -> None:
        contract["finalRecord"]["paths"][0] = output_json
        contract["scenarios"][5]["command"] = contract["scenarios"][5]["command"].replace(
            V2_OUTPUT_JSON, output_json
        )

    (repository / V2_CONTRACT_PATH).write_bytes(v2_contract_bytes(redirect))
    v2_git(repository, "add", V2_CONTRACT_PATH)
    v2_fault_commit(lambda repository: None)(repository, fixture)
    before = v2_snapshot(repository)
    arguments = v2_arguments(
        repository,
        "--contract",
        V2_CONTRACT_PATH,
        "--format",
        "bundle",
        "--output-json",
        output_json,
        "--output-markdown",
        V2_OUTPUT_MARKDOWN,
        replace=True,
    )
    document = v2_assert_failure(v2_run(arguments), {"OUTPUT_PATH_INVALID"})
    assert [
        item["subject"]
        for item in document["diagnostics"]
        if item["code"] == "OUTPUT_PATH_INVALID"
    ] == ["--output-json"]
    assert v2_snapshot(repository) == before


# ---- BLOCKED class, output containment, and output rollback ---------------- #
# These fall outside the AC-7.1-05 selector: its contract row covers only exit
# `1` input failures. The read-back drift tests belong to AC-7.1-02.


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


def test_v2_blocked_when_schemas_are_unavailable(
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


def test_v2_blocked_when_git_fails(
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


def test_v2_blocked_when_head_resolution_git_fails(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    module = load_generator()
    original = module.run_git

    def failing(repository_path: Path, *arguments: str, **options):
        if arguments[:3] == ("rev-parse", "--verify", "HEAD^{commit}"):
            raise module.GateError("GIT_COMMAND_FAILED", "forced fixture failure")
        return original(repository_path, *arguments, **options)

    monkeypatch.setattr(module, "run_git", failing)
    document = v2_in_process(module, repository, capsys, 2)
    assert document["blockers"] == ["GIT_COMMAND_FAILED"]
    assert "forced fixture failure" not in json.dumps(document)
    assert v2_snapshot(repository) == before


def test_v2_blocked_when_jsonschema_is_unavailable(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    module = load_generator()
    real_import = builtins.__import__

    def refuse(name: str, *arguments: object, **keywords: object) -> object:
        if name == "jsonschema":
            raise ImportError("no module named jsonschema")
        return real_import(name, *arguments, **keywords)

    monkeypatch.setattr(builtins, "__import__", refuse)
    exit_code = module.main(v2_arguments(repository))
    monkeypatch.undo()
    assert exit_code == 2
    captured = capsys.readouterr()
    assert captured.err == ""
    assert "Traceback" not in captured.out
    document = json.loads(captured.out)
    v2_failure_validator().validate(document)
    assert document["exitCode"] == 2
    assert document["result"] == "BLOCKED"
    assert document["blockers"] == ["SCHEMA_VALIDATOR_UNAVAILABLE"]
    assert v2_snapshot(repository) == before


def test_v2_output_rejects_a_symlinked_leaf(
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


def test_v2_output_rolls_back_when_the_second_replace_fails(
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
    assert "every replaced output was restored" in json.dumps(document)


def test_v2_output_rolls_back_when_the_second_replace_fails_without_seeded_outputs(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
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
    assert v2_outputs(repository) == (None, None)
    assert v2_snapshot(repository) == before
    assert "every replaced output was restored" in json.dumps(document)


def test_v2_output_reports_a_failed_restore(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
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
    monkeypatch.setattr(module, "v2_restore_outputs", lambda replaced, originals: False)
    document = v2_in_process(module, repository, capsys, 2)
    assert document["blockers"] == ["OUTPUT_WRITE_FAILED"]
    payload = json.dumps(document)
    assert "one or more replaced outputs could not be restored" in payload
    assert "every replaced output was restored" not in payload
    assert v2_outputs(repository)[0] is not None


def test_v2_deterministic_bundle_read_back_drift_rolls_back_without_seeded_outputs(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
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
    assert v2_outputs(repository) == (None, None)
    assert v2_snapshot(repository) == before
    assert "every replaced output was restored" in json.dumps(document)


def test_v2_deterministic_bundle_read_back_drift_rolls_back(
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


STORY_7_2_CONTRACT = WORKSPACE / "_bmad-output/planning-artifacts/v9/story-contracts/7.2.json"
STORY_7_2_SPEC = WORKSPACE / "_bmad-output/implementation-artifacts/spec-7-2-derive-test-path-candidate-submodule-and-gitlink-facts.md"
STORY_7_2_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/7.2.json"
STORY_7_2_SPEC_PATH = "_bmad-output/implementation-artifacts/spec-7-2-derive-test-path-candidate-submodule-and-gitlink-facts.md"
STORY_7_2_SPRINT_PATH = "_bmad-output/implementation-artifacts/sprint-status.yaml"
STORY_7_2_OUTPUT_JSON = "docs/release-evidence/story-7.2-final-record-v2.json"
STORY_7_2_OUTPUT_MARKDOWN = "docs/release-evidence/story-7.2-final-record-v2.md"
STORY_7_2_PROJECTS = tuple(sorted(path.stem for path in WORKSPACE.glob("tests/*/*.csproj")))
STORY_7_2_ACCEPTANCE_SCHEMA = WORKSPACE / "_bmad/schemas/v9-acceptance-result-v1.schema.json"


def v2_7_2_arguments(repository: Path) -> list[str]:
    return v2_arguments(repository, "--contract", STORY_7_2_CONTRACT_PATH,
                        "--output-json", STORY_7_2_OUTPUT_JSON,
                        "--output-markdown", STORY_7_2_OUTPUT_MARKDOWN, replace=True)


def v2_7_2_assert_pass(repository: Path, result: subprocess.CompletedProcess[bytes]) -> dict:
    assert result.returncode == 0, result.stdout.decode("utf-8", "replace")
    json_bytes = (repository / STORY_7_2_OUTPUT_JSON).read_bytes()
    markdown_bytes = (repository / STORY_7_2_OUTPUT_MARKDOWN).read_bytes()
    assert result.stdout == json_bytes
    record = json.loads(json_bytes)
    v2_schema_contract_validator(v2_schema_contract_load(FINAL_RECORD_SCHEMA)).validate(record)
    assert record["outputs"]["json"]["sha256"] == v2_json_digest(record)
    assert record["outputs"]["markdown"]["sha256"] == hashlib.sha256(markdown_bytes).hexdigest()
    assert load_generator().v2_verify_pair(json_bytes, markdown_bytes) == []
    return record


def v2_7_2_results(repository: Path) -> None:
    contract = json.loads(STORY_7_2_CONTRACT.read_bytes())
    for scenario in contract["scenarios"][:10]:
        selector = re.search(r" -k (\S+) ", scenario["command"]).group(1)
        junit = re.search(r"--junitxml=(\S+)$", scenario["command"]).group(1)
        v2_write_result(repository, junit, v2_junit(selector))


def v2_7_2_trx(repository: Path, name: str, *, passed: int = 2,
               failed: int = 0, skipped: int = 0) -> Path:
    path = repository / f"artifacts/v9/7.2/test-results/{name}.trx"
    write_trx(path, passed=passed, failed=failed, skipped=skipped, project=name,
              code_base=Path(f"/fixture/{name}.dll"), include_test_ids=True)
    # Anchor after every fixture write and commit, not only the spec, so slow
    # runners never read a freshly written result as stale.
    current = max((repository / STORY_7_2_SPEC_PATH).stat().st_mtime_ns,
                  (repository / STORY_7_2_CONTRACT_PATH).stat().st_mtime_ns,
                  time.time_ns())
    os.utime(path, ns=(current + 10_000_000_000, current + 10_000_000_000))
    return path


def build_v2_7_2_repository(tmp_path: Path) -> dict[str, object]:
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    baseline = fixture["candidate"]
    for relative, source in (
        (STORY_7_2_CONTRACT_PATH, STORY_7_2_CONTRACT),
        ("docs/release-evidence/story-7.1-final-record-v2.json",
         WORKSPACE / "docs/release-evidence/story-7.1-final-record-v2.json"),
        ("docs/release-evidence/story-7.1-final-record-v2.md",
         WORKSPACE / "docs/release-evidence/story-7.1-final-record-v2.md"),
    ):
        target = repository / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(source.read_bytes())
    spec = STORY_7_2_SPEC.read_text(encoding="utf-8")
    spec = re.sub(r"^baseline_commit:.*$", f"baseline_commit: '{baseline}'", spec,
                  count=1, flags=re.MULTILINE)
    spec = re.sub(r"^status:.*$", "status: 'in-progress'", spec,
                  count=1, flags=re.MULTILINE)
    spec_path = repository / STORY_7_2_SPEC_PATH
    spec_path.parent.mkdir(parents=True, exist_ok=True)
    spec_path.write_text(spec, encoding="utf-8")
    sprint = repository / STORY_7_2_SPRINT_PATH
    sprint.write_text(
        "last_updated: 2026-09-25\n"
        "development_status:\n"
        "  epic-7: in-progress\n"
        "  7-2-derive-test-path-candidate-submodule-and-gitlink-facts: in-progress\n",
        encoding="utf-8",
    )
    slnx = repository / "Hexalith.Conversations.slnx"
    slnx.write_text("<Solution><Folder Name=\"/tests/\">" + "".join(
        f'<Project Path="tests/{name}/{name}.csproj" />' for name in STORY_7_2_PROJECTS
    ) + "</Folder></Solution>\n", encoding="utf-8")
    for name in STORY_7_2_PROJECTS:
        path = repository / f"tests/{name}/{name}.csproj"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("<Project />\n", encoding="utf-8")
    (repository / "docs/decoy-160000.txt").write_text(
        "160000 is text, not a Git mode\n", encoding="utf-8")
    v2_git(repository, "add", "--all")
    v2_git(repository, "commit", "-m", "story 7.2 fixture candidate")
    fixture["candidate"] = v2_git(repository, "rev-parse", "HEAD").stdout.strip()
    fixture["baseline"] = baseline
    v2_7_2_results(repository)
    for name in STORY_7_2_PROJECTS:
        v2_7_2_trx(repository, name)
    return fixture


def v2_7_2_snapshot_and_fault(fixture: dict[str, object], mutate,
                              expected: str) -> None:
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    before = v2_snapshot(repository)
    outputs = tuple((repository / path).read_bytes() for path in
                    (STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN))
    try:
        restore = mutate(repository)
        document = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {expected})
        assert document["blockers"] == [expected]
        assert outputs == tuple((repository / path).read_bytes() for path in
                                (STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN))
    finally:
        restore()
    assert v2_snapshot(repository) == before
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    assert outputs == tuple((repository / path).read_bytes() for path in
                            (STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN))


def v2_7_2_replace(path: Path, content: bytes):
    original = path.read_bytes()
    modified = path.stat().st_mtime_ns
    path.write_bytes(content)
    os.utime(path, ns=(modified, modified))

    def restore() -> None:
        path.write_bytes(original)
        os.utime(path, ns=(modified, modified))

    return restore


def test_v2_derives_required_test_totals(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    measured = record["measurements"]
    assert [row["project"] for row in measured["testProjects"]] == list(STORY_7_2_PROJECTS)
    expected_total = 2 * len(STORY_7_2_PROJECTS)
    assert measured["testTotals"] == {"total": expected_total, "executed": expected_total,
                                      "passed": expected_total, "failed": 0, "skipped": 0}
    assert all(row["counts"]["total"] == 2 for row in measured["testProjects"])
    assert measured["baseline"] == fixture["baseline"]
    assert measured["changedPaths"] == sorted(measured["changedPaths"])
    assert measured["predecessorRecord"]["sha256"] == sha256_file(
        repository / "docs/release-evidence/story-7.1-final-record-v2.json")
    markdown = (repository / STORY_7_2_OUTPUT_MARKDOWN).read_text(encoding="utf-8")
    assert "## Story 7.2 measurements" in markdown
    for row in measured["testProjects"]:
        assert f"| `{row['project']}` | `{row['resultFile']['path']}` | `2` |" in markdown
    assert f"| `TOTAL` | `none` | `{expected_total}` |" in markdown
    assert "``" not in markdown
    for path in measured["changedPaths"]:
        assert f"- `{path}`" in markdown
    acceptance = {
        "schemaVersion": "hexalith.conversations.acceptance-result.v1",
        "storyId": "7.2", "scenarioId": "AC-7.2-01",
        "command": json.loads(STORY_7_2_CONTRACT.read_bytes())["scenarios"][0]["command"],
        "exitCode": 0, "result": "PASS", "blockers": [],
        "candidate": fixture["candidate"],
        "inputs": [row["resultFile"] for row in measured["testProjects"]],
        "outputs": [record["outputs"]["json"], record["outputs"]["markdown"]],
        "assertionLedger": [
            {"id": f"AC-7.2-01#{index:04d}", "subject": row["project"], "state": "PASS"}
            for index, row in enumerate(measured["testProjects"], 1)
        ],
    }
    v2_schema_contract_validator(v2_schema_contract_load(STORY_7_2_ACCEPTANCE_SCHEMA)).validate(acceptance)


def test_v2_blocks_missing_result(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    def mutate(_):
        original = path.read_bytes()
        modified = path.stat().st_mtime_ns
        path.unlink()
        def restore():
            path.write_bytes(original)
            os.utime(path, ns=(modified, modified))
        return restore
    v2_7_2_snapshot_and_fault(fixture, mutate, "TEST_RESULTS_MISSING")


def test_v2_blocks_stale_result(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    def mutate(_):
        original = path.stat().st_mtime_ns
        os.utime(path, ns=(1, 1))
        return lambda: os.utime(path, ns=(original, original))
    v2_7_2_snapshot_and_fault(fixture, mutate, "TEST_RESULTS_STALE")


def test_v2_blocks_result_older_than_changed_source(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    source = repository / STORY_7_2_CONTRACT_PATH
    result = repository / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    def mutate(_):
        original = source.stat().st_mtime_ns
        os.utime(source, ns=(result.stat().st_mtime_ns + 1_000_000_000, ) * 2)
        return lambda: os.utime(source, ns=(original, original))
    v2_7_2_snapshot_and_fault(fixture, mutate, "TEST_RESULTS_STALE")


def test_v2_blocks_failed_test(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    v2_7_2_snapshot_and_fault(fixture,
        lambda _: v2_7_2_replace(path, path.read_bytes().replace(b'outcome="Passed"', b'outcome="Failed"', 1)
                                 .replace(b'passed="2"', b'passed="1"').replace(b'failed="0"', b'failed="1"')),
        "TEST_FAILED")


def test_v2_blocks_executed_count_below_passed_results(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    v2_7_2_snapshot_and_fault(
        fixture,
        lambda _: v2_7_2_replace(path, path.read_bytes().replace(b'executed="2"', b'executed="0"', 1)),
        "TEST_FAILED",
    )


def test_v2_blocks_foreign_result_test_id(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    v2_7_2_snapshot_and_fault(
        fixture,
        lambda _: v2_7_2_replace(path, path.read_bytes().replace(b'testId="0000"', b'testId="foreign"', 1)),
        "TEST_RESULTS_MISSING",
    )


def test_v2_blocks_unapproved_skip(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    v2_7_2_snapshot_and_fault(fixture,
        lambda _: v2_7_2_replace(path, path.read_bytes().replace(b'outcome="Passed"', b'outcome="NotExecuted"', 1)
                                 .replace(b'passed="2"', b'passed="1"').replace(b'notExecuted="0"', b'notExecuted="1"')
                                 .replace(b'executed="2"', b'executed="1"')),
        "TEST_SKIPPED")


def test_v2_blocks_not_run(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    v2_7_2_snapshot_and_fault(fixture,
        lambda _: v2_7_2_replace(path, path.read_bytes().replace(
            b'<UnitTestResult testName="' + STORY_7_2_PROJECTS[0].encode(), b'<Removed testName="' + STORY_7_2_PROJECTS[0].encode())
            .replace(b'total="2"', b'total="0"').replace(b'executed="2"', b'executed="0"')
            .replace(b'passed="2"', b'passed="0"')),
        "TEST_NOT_RUN")


def test_v2_derives_singular_file_list_and_blocks_dirt(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    changed = subprocess.run(["git", "-C", str(repository), "diff", "--name-only",
                              "--no-renames", fixture["baseline"], fixture["candidate"]],
                             check=True, capture_output=True, text=True).stdout.splitlines()
    assert record["measurements"]["changedPaths"] == sorted(changed)
    dirt = repository / "unrelated-source.txt"
    def mutate(_):
        dirt.write_text("dirty\n", encoding="utf-8")
        return lambda: dirt.unlink()
    v2_7_2_snapshot_and_fault(fixture, mutate, "SOURCE_TREE_DIRTY")
    module = load_generator()
    original = module.committed_path_status
    before = v2_snapshot(repository)
    def divergent(repository_arg, baseline_arg, candidate_arg):
        return {**original(repository_arg, baseline_arg, candidate_arg),
                "invented-path.txt": "A"}
    module.committed_path_status = divergent
    try:
        with pytest.raises(module.V2Stop) as error:
            module.v2_story_7_2_measurements(repository, fixture["candidate"],
                record["candidate"]["gitlinks"],
                {STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN},
                module.v2_load_schemas()[1])
        assert {row["code"] for row in error.value.findings} == {"FILE_LIST_DRIFT"}
    finally:
        module.committed_path_status = original
    assert v2_snapshot(repository) == before


def test_v2_blocks_submodule_internal_path(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    module = load_generator()
    before = v2_snapshot(repository)
    original_status = module.committed_path_status
    original_git = module.run_git
    internal_path = ROOT_GITLINK_PATHS[0] + "/internal.txt"
    def derived_internal_path(repository_arg, baseline_arg, candidate_arg):
        paths = original_status(repository_arg, baseline_arg, candidate_arg)
        if baseline_arg == fixture["baseline"] and candidate_arg == fixture["candidate"]:
            return {**paths, internal_path: "A"}
        return paths
    def matching_raw_path(repository_arg, *arguments, **options):
        result = original_git(repository_arg, *arguments, **options)
        if (arguments[:3] == ("diff", "--name-only", "--no-renames")
                and arguments[4:6] == (fixture["baseline"], fixture["candidate"])):
            return subprocess.CompletedProcess(result.args, result.returncode,
                                               result.stdout + internal_path.encode() + b"\0",
                                               result.stderr)
        return result
    module.committed_path_status = derived_internal_path
    module.run_git = matching_raw_path
    try:
        output = io.StringIO()
        with redirect_stdout(output):
            exit_code = module.v2_main(v2_7_2_arguments(repository))
        failure = json.loads(output.getvalue())
        v2_failure_validator().validate(failure)
        assert exit_code == failure["exitCode"] == 1
        assert failure["result"] == "FAIL"
        assert failure["blockers"] == ["SUBMODULE_INTERNAL_PATH"]
    finally:
        module.committed_path_status = original_status
        module.run_git = original_git
    assert v2_snapshot(repository) == before


def test_v2_resolves_raw_gitlinks(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    assert len(record["candidate"]["gitlinks"]) == 10
    assert all(row["mode"] == "160000" for row in record["candidate"]["gitlinks"])
    module = load_generator()
    assert module.v2_raw_gitlinks(repository, fixture["candidate"]) == [
        (row["path"], row["commit"]) for row in record["candidate"]["gitlinks"]]
    assert "docs/decoy-160000.txt" in record["measurements"]["changedPaths"]
    before = v2_snapshot(repository)
    missing_path = ROOT_GITLINK_PATHS[0]
    v2_git(repository, "update-index", "--force-remove", missing_path)
    v2_git(repository, "commit", "-m", "remove gitlink")
    findings = []
    module.v2_gitlinks(repository, v2_git(repository, "rev-parse", "HEAD").stdout.strip(),
                       findings, "7.2")
    assert {row["code"] for row in findings} == {"GITLINK_SCOPE_MISMATCH"}
    v2_git(repository, "reset", "--hard", "-q", fixture["candidate"])
    assert v2_snapshot(repository) == before
    v2_git(repository, "update-index", "--add", "--cacheinfo",
           f"160000,{'a' * 40},references/Extra")
    v2_git(repository, "commit", "-m", "add extra gitlink")
    findings = []
    module.v2_gitlinks(repository, v2_git(repository, "rev-parse", "HEAD").stdout.strip(),
                       findings, "7.2")
    assert {row["code"] for row in findings} == {"GITLINK_SCOPE_MISMATCH"}
    v2_git(repository, "reset", "--hard", "-q", fixture["candidate"])
    assert v2_snapshot(repository) == before
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    record_only = v2_snapshot(repository)
    moved = ROOT_GITLINK_PATHS[0]
    v2_git(repository, "update-index", "--cacheinfo",
           f"160000,{'a' * 40},{moved}")
    v2_git(repository, "commit", "-m", "move gitlink")
    moved_failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)),
                                      {"GITLINK_DRIFT", "CANDIDATE_NOT_FINAL"})
    assert set(moved_failure["blockers"]) == {"GITLINK_DRIFT", "CANDIDATE_NOT_FINAL"}
    v2_git(repository, "reset", "--hard", "-q", record_only[0])
    assert v2_snapshot(repository) == record_only


def test_v2_excludes_changed_gitlink_from_root_paths(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    moved = ROOT_GITLINK_PATHS[0]
    v2_git(repository, "update-index", "--cacheinfo", f"160000,{'a' * 40},{moved}")
    v2_git(repository, "commit", "-m", "move fixture gitlink")
    v2_7_2_results(repository)
    for name in STORY_7_2_PROJECTS:
        v2_7_2_trx(repository, name)
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    assert moved not in record["measurements"]["changedPaths"]
    assert next(row for row in record["candidate"]["gitlinks"] if row["path"] == moved)["commit"] == "a" * 40


def test_v2_blocks_superseded_candidate(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    before = v2_snapshot(repository)
    target = repository / V2_TARGET_PATH
    target.write_text(target.read_text(encoding="utf-8") + "# superseding source\n",
                      encoding="utf-8")
    v2_git(repository, "add", V2_TARGET_PATH)
    v2_git(repository, "commit", "-m", "supersede candidate")
    superseded = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert superseded["blockers"] == ["CANDIDATE_NOT_FINAL"]
    v2_git(repository, "reset", "--hard", "-q", before[0])
    assert v2_snapshot(repository) == before
    spec_path = repository / STORY_7_2_SPEC_PATH
    original_time = spec_path.stat().st_mtime_ns
    spec_path.write_text(spec_path.read_text(encoding="utf-8").replace(
        fixture["baseline"], "0" * 40, 1), encoding="utf-8")
    v2_git(repository, "add", STORY_7_2_SPEC_PATH)
    v2_git(repository, "commit", "-m", "invalidate baseline")
    # Retract the committed pair so the bad baseline is evaluated as a new candidate.
    output_bytes = [(repository / path).read_bytes() for path in
                    (STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)]
    v2_git(repository, "rm", "-q", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "retract prior pair")
    for name in STORY_7_2_PROJECTS:
        v2_7_2_trx(repository, name)
    untrusted = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"BASELINE_NOT_TRUSTWORTHY"})
    assert untrusted["blockers"] == ["BASELINE_NOT_TRUSTWORTHY"]
    v2_git(repository, "reset", "--hard", "-q", before[0])
    os.utime(spec_path, ns=(original_time, original_time))
    for path, content in zip((STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN), output_bytes):
        (repository / path).write_bytes(content)
    v2_7_2_restore_result_mtimes(repository, before)
    assert v2_snapshot(repository) == before



def v2_7_2_outputs(repository: Path) -> tuple[bytes, bytes]:
    return tuple((repository / path).read_bytes() for path in
                 (STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN))


def v2_7_2_restore_result_mtimes(repository: Path, snapshot: tuple) -> None:
    for relative, entry in snapshot[1].items():
        if relative.startswith("artifacts/") and entry[0] == "file":
            os.utime(repository / relative, ns=(entry[2], entry[2]))


def test_v2_retains_candidate_across_record_only_commit(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    outputs = v2_7_2_outputs(repository)
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    assert record["candidate"]["commit"] == fixture["candidate"]
    assert v2_git(repository, "rev-parse", "HEAD").stdout.strip() != fixture["candidate"]
    assert v2_7_2_outputs(repository) == outputs


def test_v2_blocks_invalid_prior_pair(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    before = v2_snapshot(repository)
    outputs = v2_7_2_outputs(repository)
    json_path = repository / STORY_7_2_OUTPUT_JSON
    markdown_path = repository / STORY_7_2_OUTPUT_MARKDOWN
    v2_git(repository, "rm", "-q", STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "remove prior markdown")
    incomplete = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"RECORD_CONTENT_DRIFT"})
    assert incomplete["blockers"] == ["RECORD_CONTENT_DRIFT"]
    v2_git(repository, "reset", "--hard", "-q", before[0])
    assert v2_snapshot(repository) == before
    json_path.write_bytes(outputs[0].replace(b'"storyId": "7.2"', b'"storyId": "7.1"', 1))
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON)
    v2_git(repository, "commit", "-m", "corrupt prior record")
    tampered = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"RECORD_CONTENT_DRIFT"})
    assert tampered["blockers"] == ["RECORD_CONTENT_DRIFT"]
    v2_git(repository, "reset", "--hard", "-q", before[0])
    assert v2_7_2_outputs(repository) == outputs
    assert v2_snapshot(repository) == before


def test_v2_uncommitted_prior_pair_does_not_pin_candidate(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    source = repository / V2_TARGET_PATH
    source.write_text(source.read_text(encoding="utf-8") + "# new source\n", encoding="utf-8")
    v2_git(repository, "add", V2_TARGET_PATH)
    v2_git(repository, "commit", "-m", "source successor")
    v2_7_2_results(repository)
    for name in STORY_7_2_PROJECTS:
        v2_7_2_trx(repository, name)
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    assert record["candidate"]["commit"] == v2_git(repository, "rev-parse", "HEAD").stdout.strip()


def test_v2_retains_candidate_across_lifecycle_bookkeeping(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    original = v2_7_2_outputs(repository)
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    spec = repository / STORY_7_2_SPEC_PATH
    spec.write_text(spec.read_text(encoding="utf-8").replace("status: 'in-progress'", "status: 'in-review'", 1), encoding="utf-8")
    sprint = repository / STORY_7_2_SPRINT_PATH
    sprint.write_text(
        sprint.read_text(encoding="utf-8").replace(
            "7-2-derive-test-path-candidate-submodule-and-gitlink-facts: in-progress",
            "7-2-derive-test-path-candidate-submodule-and-gitlink-facts: in-review",
            1,
        ).replace("last_updated: 2026-09-25", "last_updated: 2026-09-27", 1),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_2_SPEC_PATH, STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "lifecycle bookkeeping")
    latest_result_ns = max((repository / f"artifacts/v9/7.2/test-results/{name}.trx").stat().st_mtime_ns
                           for name in STORY_7_2_PROJECTS)
    lifecycle_ns = latest_result_ns + 1_000_000_000
    os.utime(spec, ns=(lifecycle_ns, lifecycle_ns))
    os.utime(sprint, ns=(lifecycle_ns, lifecycle_ns))
    assert spec.stat().st_mtime_ns > latest_result_ns
    assert sprint.stat().st_mtime_ns > latest_result_ns
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    assert record["candidate"]["commit"] == fixture["candidate"]
    assert v2_7_2_outputs(repository) == original


def test_v2_retains_candidate_across_a_sprint_header_date_comment(tmp_path: Path) -> None:
    """A comment-only `# last_updated` date change remains lifecycle bookkeeping."""
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    sprint = repository / STORY_7_2_SPRINT_PATH
    sprint.write_text(
        "# last_updated: 2026-09-25\n" + sprint.read_text(encoding="utf-8"),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "record sprint header date")
    for name in STORY_7_2_PROJECTS:
        v2_7_2_trx(repository, name)
    candidate = v2_git(repository, "rev-parse", "HEAD").stdout.strip()
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    original = v2_7_2_outputs(repository)
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    sprint.write_text(
        sprint.read_text(encoding="utf-8").replace(
            "# last_updated: 2026-09-25", "# last_updated: 2026-09-26", 1
        ),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "refresh sprint header date")
    record = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    assert record["candidate"]["commit"] == candidate
    assert v2_7_2_outputs(repository) == original


def test_v2_blocks_unrelated_sprint_status_change_after_record(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    before = v2_snapshot(repository)
    original = v2_7_2_outputs(repository)
    sprint = repository / STORY_7_2_SPRINT_PATH
    sprint.write_text(sprint.read_text(encoding="utf-8").replace(
        "epic-7: in-progress", "epic-7: done", 1), encoding="utf-8")
    v2_git(repository, "add", STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "change unrelated sprint status")
    failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
    assert failure["diagnostics"][0]["message"].endswith(f": {STORY_7_2_SPRINT_PATH}")
    assert v2_7_2_outputs(repository) == original
    v2_git(repository, "reset", "--hard", "-q", before[0])
    assert v2_snapshot(repository) == before


def test_v2_blocks_non_status_spec_change_after_record(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    original = v2_7_2_outputs(repository)
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    spec = repository / STORY_7_2_SPEC_PATH
    spec.write_text(spec.read_text(encoding="utf-8") + "\nLater source edit.\n", encoding="utf-8")
    v2_git(repository, "add", STORY_7_2_SPEC_PATH)
    v2_git(repository, "commit", "-m", "change story source")
    failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
    assert failure["diagnostics"][0]["message"].endswith(f": {STORY_7_2_SPEC_PATH}")
    assert v2_7_2_outputs(repository) == original


@pytest.mark.parametrize("edit", ("followup-field", "final-record-region"))
def test_v2_story_7_2_retention_rejects_record_region_and_followup(
    tmp_path: Path, edit: str
) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    original = v2_7_2_outputs(repository)
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    spec = repository / STORY_7_2_SPEC_PATH
    if edit == "followup-field":
        spec.write_text(
            spec.read_text(encoding="utf-8").replace(
                "status: 'in-progress'\n",
                "status: 'in-progress'\nfollowup_review_recommended: false\n",
                1,
            ),
            encoding="utf-8",
        )
    else:
        spec.write_bytes(
            spec.read_bytes()
            + b"\n"
            + RECORD_BEGIN_LINE
            + b"\n"
            + original[1]
            + RECORD_END_LINE
            + b"\n"
        )
    v2_git(repository, "add", STORY_7_2_SPEC_PATH)
    v2_git(repository, "commit", "-m", "story 7.2 lifecycle edit that retention must reject")
    failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
    assert v2_7_2_outputs(repository) == original
    v2_git(repository, "reset", "--hard", "-q", "HEAD~1")
    inserted = spec.read_bytes() + b"\n" + RECORD_BEGIN_LINE + b"\n" + original[1] + RECORD_END_LINE + b"\n"
    spec.write_bytes(inserted)
    verify = [
        "--repository", str(repository),
        "--contract", STORY_7_2_CONTRACT_PATH,
        "--verify-inserted-record", STORY_7_2_SPEC_PATH,
    ]
    rejected = v2_assert_failure(v2_run(verify), {"RECORD_CONTENT_DRIFT"})
    assert rejected["blockers"] == ["RECORD_CONTENT_DRIFT"]
    assert v2_7_2_outputs(repository) == original


def test_v2_blocks_orphaned_record_candidate(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    v2_git(repository, "add", STORY_7_2_OUTPUT_JSON, STORY_7_2_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    tree = v2_git(repository, "rev-parse", "HEAD^{tree}").stdout.strip()
    rewritten = v2_git(repository, "commit-tree", tree, "-p", fixture["baseline"], "-m", "rewritten record").stdout.strip()
    v2_git(repository, "reset", "--hard", "-q", rewritten)
    failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]


def test_v2_blocks_resolvable_nonancestor_baseline(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    baseline_tree = v2_git(repository, "rev-parse", f"{fixture['baseline']}^{{tree}}").stdout.strip()
    sibling = v2_git(repository, "commit-tree", baseline_tree, "-p", fixture["baseline"],
                     "-m", "sibling baseline").stdout.strip()
    spec = repository / STORY_7_2_SPEC_PATH
    spec.write_text(spec.read_text(encoding="utf-8").replace(fixture["baseline"], sibling, 1), encoding="utf-8")
    v2_git(repository, "add", STORY_7_2_SPEC_PATH)
    v2_git(repository, "commit", "-m", "use nonancestor baseline")
    v2_7_2_results(repository)
    for name in STORY_7_2_PROJECTS:
        v2_7_2_trx(repository, name)
    failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"BASELINE_NOT_TRUSTWORTHY"})
    assert failure["blockers"] == ["BASELINE_NOT_TRUSTWORTHY"]


def test_v2_blocks_repeated_gitmodules_path(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    gitmodules = repository / ".gitmodules"
    gitmodules.write_text(gitmodules.read_text(encoding="utf-8")
                          + f'[submodule "duplicate"]\n\tpath = {ROOT_GITLINK_PATHS[0]}\n'
                          + "\turl = https://example.invalid/duplicate.git\n", encoding="utf-8")
    v2_git(repository, "add", ".gitmodules")
    v2_git(repository, "commit", "-m", "repeat a submodule path")
    module = load_generator()
    findings = []
    assert module.v2_gitlinks(repository, v2_git(repository, "rev-parse", "HEAD").stdout.strip(),
                              findings, "7.2") == []
    assert [row["code"] for row in findings] == ["GITLINK_SCOPE_MISMATCH"]
    failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"GITLINK_SCOPE_MISMATCH"})
    assert failure["blockers"] == ["GITLINK_SCOPE_MISMATCH"]
    v2_git(repository, "reset", "--hard", "-q", fixture["candidate"])
    assert v2_snapshot(repository) == before


def test_v2_blocks_foreign_assembly_result(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    own, other = STORY_7_2_PROJECTS[0], STORY_7_2_PROJECTS[1]
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{own}.trx"
    v2_7_2_snapshot_and_fault(fixture,
        lambda _: v2_7_2_replace(path, path.read_bytes().replace(
            f"/fixture/{own}.dll".encode(), f"/fixture/{other}.dll".encode())),
        "TEST_RESULTS_MISSING")


def test_v2_blocks_header_row_disagreement(tmp_path: Path) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    path = fixture["repository"] / f"artifacts/v9/7.2/test-results/{STORY_7_2_PROJECTS[0]}.trx"
    # The header still reports every test passed; only a result row disagrees.
    v2_7_2_snapshot_and_fault(fixture,
        lambda _: v2_7_2_replace(path, path.read_bytes().replace(
            b'outcome="Passed"', b'outcome="Failed"', 1)),
        "TEST_FAILED")


@pytest.mark.parametrize("fault", ["tampered-json", "missing-markdown"])
def test_v2_blocks_invalid_predecessor_record(tmp_path: Path, fault: str) -> None:
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    if fault == "tampered-json":
        target = repository / "docs/release-evidence/story-7.1-final-record-v2.json"
        predecessor = json.loads(target.read_bytes())
        predecessor["rollback"]["boundary"] += " tampered"
        target.write_text(json.dumps(predecessor, indent=2) + "\n", encoding="utf-8")
        v2_git(repository, "add", "docs/release-evidence/story-7.1-final-record-v2.json")
    else:
        v2_git(repository, "rm", "-q", "docs/release-evidence/story-7.1-final-record-v2.md")
    v2_git(repository, "commit", "-m", "invalidate predecessor")
    v2_7_2_results(repository)
    for name in STORY_7_2_PROJECTS:
        v2_7_2_trx(repository, name)
    failure = v2_assert_failure(v2_run(v2_7_2_arguments(repository)), {"AUTHORITY_BINDING_INVALID"})
    assert failure["blockers"] == ["AUTHORITY_BINDING_INVALID"]
    assert not (repository / STORY_7_2_OUTPUT_JSON).exists()
    v2_git(repository, "reset", "--hard", "-q", fixture["candidate"])
    v2_7_2_restore_result_mtimes(repository, before)
    assert v2_snapshot(repository) == before


def test_v2_schema_requires_measurements_only_for_story_7_2(tmp_path: Path) -> None:
    validator = v2_schema_contract_validator(v2_schema_contract_load(FINAL_RECORD_SCHEMA))
    story_7_1 = json.loads((WORKSPACE / "docs/release-evidence/story-7.1-final-record-v2.json").read_bytes())
    fixture = build_v2_7_2_repository(tmp_path)
    repository = fixture["repository"]
    story_7_2 = v2_7_2_assert_pass(repository, v2_run(v2_7_2_arguments(repository)))
    for record, should_accept in ((story_7_1, True), (story_7_2, False)):
        assert validator.is_valid(record)
        without = dict(record)
        without.pop("measurements", None)
        assert validator.is_valid(without) == should_accept
    story_7_1["measurements"] = story_7_2["measurements"]
    assert not validator.is_valid(story_7_1)



# --------------------------------------------------------------------------- #
# Story 7.3: generation gates every blocking completion transition
# --------------------------------------------------------------------------- #
#
# The fixtures copy the governed skill trees, the render configuration, the
# frozen Story 7.3 contract, and the committed Story 7.1/7.2 pairs into a
# hermetic repository, so the verifier and the generator run against real route
# bodies without ever mutating this checkout. Every fault restores the fixture
# byte-identically, including its acceptance-result evidence.

VERIFIER_SCRIPT = SCRIPT.parent / "verify_story_completion_workflows.py"
STORY_7_3_CONTRACT = WORKSPACE / "_bmad-output/planning-artifacts/v9/story-contracts/7.3.json"
STORY_7_3_CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/7.3.json"
STORY_7_3_SPEC_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-7-3-integrate-generation-into-every-blocking-completion-transition.md"
)
STORY_7_3_SPRINT_ROW = "7-3-integrate-generation-into-every-blocking-completion-transition"
STORY_7_3_OUTPUT_JSON = "docs/release-evidence/story-7.3-final-record-v2.json"
STORY_7_3_OUTPUT_MARKDOWN = "docs/release-evidence/story-7.3-final-record-v2.md"
STORY_7_3_RESULTS = "artifacts/v9/7.3"
STORY_7_3_SKILLS = ("bmad-build", "bmad-build-auto", "bmad-code-review")
STORY_7_3_COPIED_FILES = (
    STORY_7_3_CONTRACT_PATH,
    "docs/release-evidence/story-7.1-final-record-v2.json",
    "docs/release-evidence/story-7.1-final-record-v2.md",
    "docs/release-evidence/story-7.2-final-record-v2.json",
    "docs/release-evidence/story-7.2-final-record-v2.md",
    "_bmad/scripts/verify_story_completion_workflows.py",
    "_bmad/config.toml",
    "_bmad/config.user.toml",
    "_bmad/custom/config.toml",
    "_bmad/custom/bmad-build.toml",
    "_bmad/custom/bmad-build-auto.toml",
)
RECORD_BEGIN_LINE = b"<!-- STORY-FINAL-RECORD:BEGIN -->"
RECORD_END_LINE = b"<!-- STORY-FINAL-RECORD:END -->"


def load_completion_verifier():
    spec = importlib_util.spec_from_file_location(
        "verify_story_completion_workflows", VERIFIER_SCRIPT
    )
    module = importlib_util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


COMPLETION_VERIFIER = load_completion_verifier()
STORY_7_3_BODIES = COMPLETION_VERIFIER.BODY_PATHS
STORY_7_3_TWINS = COMPLETION_VERIFIER.TWIN_LABELS


def v2_7_3_route(label: str):
    logical = label.split("/skills/", 1)[1]
    return next(route for route in COMPLETION_VERIFIER.ROUTES if route.path == logical)


def v2_7_3_affected(body: str) -> list[str]:
    """A .claude body's render twin is rendered from it, so it carries the same fault."""
    twin = f"{COMPLETION_VERIFIER.TWIN_PREFIX}{body}"
    return [body, twin] if twin in STORY_7_3_TWINS else [body]


def v2_7_3_block_span(text: str) -> tuple[int, int]:
    start = text.index(COMPLETION_VERIFIER.BLOCK_BEGIN)
    end = text.index(COMPLETION_VERIFIER.BLOCK_END) + len(COMPLETION_VERIFIER.BLOCK_END)
    return start, end


def v2_7_3_spec(baseline: str) -> str:
    return (
        "---\n"
        "title: 'Fixture Story 7.3'\n"
        "status: 'in-progress'\n"
        f"baseline_commit: '{baseline}'\n"
        "---\n\n"
        "# Fixture Story 7.3\n\n"
        "## Verification\n\n"
        "Fixture verification notes.\n"
    )


def v2_7_3_verify(repository: Path, scenario_id: str, *overrides: str) -> subprocess.CompletedProcess[bytes]:
    arguments = overrides or (
        "--repository",
        str(repository),
        "--contract",
        STORY_7_3_CONTRACT_PATH,
        "--scenario",
        scenario_id,
        "--output",
        f"{STORY_7_3_RESULTS}/{scenario_id}.json",
    )
    return subprocess.run(
        [sys.executable, str(VERIFIER_SCRIPT), *arguments],
        check=False,
        capture_output=True,
        env=GIT_ENV,
        timeout=120,
    )


def v2_7_3_verify_in_process(
    module, repository: Path, scenario_id: str, capsys: pytest.CaptureFixture[str]
) -> tuple[int, dict]:
    exit_code = module.main(
        [
            "--repository",
            str(repository),
            "--contract",
            STORY_7_3_CONTRACT_PATH,
            "--scenario",
            scenario_id,
            "--output",
            f"{STORY_7_3_RESULTS}/{scenario_id}.json",
        ]
    )
    captured = capsys.readouterr()
    assert "Traceback" not in captured.out + captured.err
    return exit_code, json.loads(captured.out)


def v2_7_3_acceptance_validator() -> jsonschema.Draft202012Validator:
    return v2_schema_contract_validator(v2_schema_contract_load(ACCEPTANCE_RESULT_SCHEMA))


def v2_7_3_assert_verifier(
    result: subprocess.CompletedProcess[bytes], exit_code: int, blockers: list[str]
) -> dict:
    """The verifier's stdout is the acceptance result it wrote; it is schema-valid."""
    document = json.loads(result.stdout.decode("utf-8"))
    assert result.returncode == exit_code, (document, result.stderr)
    assert b"Traceback" not in result.stdout + result.stderr
    v2_7_3_acceptance_validator().validate(document)
    assert document["exitCode"] == exit_code
    assert document["result"] == {0: "PASS", 1: "FAIL", 2: "BLOCKED"}[exit_code]
    assert document["blockers"] == blockers
    return document


def v2_7_3_results(repository: Path) -> None:
    """Measured evidence for every scenario before the self-invocation."""
    contract = json.loads(STORY_7_3_CONTRACT.read_bytes())
    for scenario in contract["scenarios"]:
        command = scenario["command"]
        if " -m pytest " in command:
            selector = re.search(r" -k (\S+) ", command).group(1)
            junit = re.search(r"--junitxml=(\S+)$", command).group(1)
            v2_write_result(repository, junit, v2_junit(selector))
    for scenario_id in ("AC-7.3-01", "AC-7.3-02"):
        v2_7_3_assert_verifier(v2_7_3_verify(repository, scenario_id), 0, [])


def build_v2_7_3_repository(
    tmp_path: Path, *, results: bool = True, mutate=None
) -> dict[str, object]:
    """A hermetic committed Story 7.3 candidate carrying the real governed routes."""
    fixture = build_v2_repository(tmp_path)
    repository = fixture["repository"]
    baseline = fixture["candidate"]
    for relative in STORY_7_3_COPIED_FILES:
        source = WORKSPACE / relative
        if source.is_file():
            target = repository / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(source.read_bytes())
    for tree in COMPLETION_VERIFIER.SKILL_TREES:
        for skill in STORY_7_3_SKILLS:
            shutil.copytree(
                WORKSPACE / tree / skill,
                repository / tree / skill,
                ignore=shutil.ignore_patterns("__pycache__"),
            )
    spec_path = repository / STORY_7_3_SPEC_PATH
    spec_path.parent.mkdir(parents=True, exist_ok=True)
    spec_path.write_text(v2_7_3_spec(baseline), encoding="utf-8")
    (repository / STORY_7_2_SPRINT_PATH).write_text(
        "last_updated: 2026-09-29\n"
        "development_status:\n"
        "  epic-7: in-progress\n"
        f"  {STORY_7_3_SPRINT_ROW}: in-progress\n",
        encoding="utf-8",
    )
    if mutate is not None:
        mutate(repository)
    v2_git(repository, "add", "--all")
    v2_git(repository, "commit", "-m", "story 7.3 fixture candidate")
    fixture["candidate"] = v2_git(repository, "rev-parse", "HEAD").stdout.strip()
    fixture["baseline"] = baseline
    if results:
        v2_7_3_results(repository)
    return fixture


def v2_7_3_arguments(repository: Path) -> list[str]:
    return v2_arguments(
        repository,
        "--contract",
        STORY_7_3_CONTRACT_PATH,
        "--format",
        "bundle",
        "--output-json",
        STORY_7_3_OUTPUT_JSON,
        "--output-markdown",
        STORY_7_3_OUTPUT_MARKDOWN,
        replace=True,
    )


def v2_7_3_verify_arguments(repository: Path, spec: str = STORY_7_3_SPEC_PATH) -> list[str]:
    return [
        "--repository",
        str(repository),
        "--contract",
        STORY_7_3_CONTRACT_PATH,
        "--verify-inserted-record",
        spec,
    ]


def v2_7_3_outputs(repository: Path) -> tuple[bytes | None, bytes | None]:
    return tuple(
        (repository / path).read_bytes() if (repository / path).exists() else None
        for path in (STORY_7_3_OUTPUT_JSON, STORY_7_3_OUTPUT_MARKDOWN)
    )


def v2_7_3_assert_pass(repository: Path, result: subprocess.CompletedProcess[bytes]) -> dict:
    assert result.returncode == 0, result.stdout.decode("utf-8", "replace")
    assert result.stderr == b""
    json_bytes, markdown_bytes = v2_7_3_outputs(repository)
    assert result.stdout == json_bytes
    record = json.loads(json_bytes)
    v2_schema_contract_validator(v2_schema_contract_load(FINAL_RECORD_SCHEMA)).validate(record)
    assert record["outputs"]["json"]["sha256"] == v2_json_digest(record)
    assert record["renderedMarkdownSha256"] == hashlib.sha256(markdown_bytes).hexdigest()
    assert load_generator().v2_verify_pair(json_bytes, markdown_bytes) == []
    assert record["summary"] == json.loads(STORY_7_3_CONTRACT.read_bytes())["finalRecord"]["summary"]
    return record


def v2_7_3_lifecycle(repository: Path) -> tuple[bytes, bytes]:
    return (
        (repository / STORY_7_3_SPEC_PATH).read_bytes(),
        (repository / STORY_7_2_SPRINT_PATH).read_bytes(),
    )


def v2_7_3_replace(path: Path, content: bytes):
    """Replace one file and return a restore that puts back its bytes and mtime."""
    original = path.read_bytes()
    metadata = path.stat()
    path.write_bytes(content)

    def restore() -> None:
        path.write_bytes(original)
        os.utime(path, ns=(metadata.st_atime_ns, metadata.st_mtime_ns))

    return restore


def v2_7_3_isolated(repository: Path, fault, check) -> None:
    """Run one fault and restore the fixture, including result evidence, byte-identically."""
    before = v2_snapshot(repository)
    results = repository / STORY_7_3_RESULTS
    saved = {
        path.name: (path.read_bytes(), path.stat().st_mtime_ns)
        for path in (results.iterdir() if results.is_dir() else ())
        if path.is_file()
    }
    restore = fault(repository)
    try:
        check()
    finally:
        restore()
        if results.is_dir():
            for path in results.iterdir():
                if path.name not in saved:
                    path.unlink()
        for name, (content, mtime) in saved.items():
            (results / name).write_bytes(content)
            os.utime(results / name, ns=(mtime, mtime))
    assert v2_snapshot(repository) == before


def v2_7_3_remove_block(repository: Path, label: str):
    path = repository / label
    text = path.read_text(encoding="utf-8")
    start, end = v2_7_3_block_span(text)
    return v2_7_3_replace(path, (text[:start] + text[end + 1 :]).encode("utf-8"))


def v2_7_3_insert_record(repository: Path, markdown: bytes):
    spec = repository / STORY_7_3_SPEC_PATH
    content = spec.read_bytes()
    return v2_7_3_replace(
        spec, content + b"\n" + RECORD_BEGIN_LINE + b"\n" + markdown + RECORD_END_LINE + b"\n"
    )


def v2_7_3_passing_pair(fixture: dict[str, object]) -> tuple[dict, bytes, bytes]:
    """Generate the pair and commit it as the record-only successor."""
    repository = fixture["repository"]
    record = v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    json_bytes, markdown_bytes = v2_7_3_outputs(repository)
    v2_git(repository, "add", STORY_7_3_OUTPUT_JSON, STORY_7_3_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only successor")
    return record, json_bytes, markdown_bytes


# ---- AC-7.3-03: v2_workflow_verifies_inserted_digest ------------------------ #


def test_v2_workflow_verifies_inserted_digest_matching_bytes_pass_and_altered_bytes_fail(
    tmp_path: Path,
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    record, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    assert record["candidate"]["commit"] == fixture["candidate"]
    v2_7_3_insert_record(repository, markdown_bytes)
    spec = (repository / STORY_7_3_SPEC_PATH).read_bytes()
    start = spec.index(RECORD_BEGIN_LINE) + len(RECORD_BEGIN_LINE) + 1
    inserted = spec[start : spec.index(RECORD_END_LINE)]
    assert inserted == markdown_bytes
    assert hashlib.sha256(inserted).hexdigest() == record["renderedMarkdownSha256"]
    verified = v2_run(v2_7_3_verify_arguments(repository))
    assert verified.returncode == 0, verified.stdout
    assert verified.stdout == json_bytes
    assert verified.stderr == b""
    absolute = v2_run(v2_7_3_verify_arguments(repository, str(repository / STORY_7_3_SPEC_PATH)))
    assert absolute.returncode == 0, absolute.stdout

    position = start + markdown_bytes.index(b"| `7` |")
    altered = spec[:position] + b"| `8` |" + spec[position + len(b"| `7` |") :]

    def fault(repository_path: Path):
        return v2_7_3_replace(repository_path / STORY_7_3_SPEC_PATH, altered)

    def check() -> None:
        document = v2_assert_failure(
            v2_run(v2_7_3_verify_arguments(repository)), {"RECORD_CONTENT_DRIFT"}
        )
        assert document["blockers"] == ["RECORD_CONTENT_DRIFT"]
        assert document["storyId"] == "7.3"

    v2_7_3_isolated(repository, fault, check)
    assert v2_run(v2_7_3_verify_arguments(repository)).returncode == 0


@pytest.mark.parametrize(
    "fault",
    (
        "no-marker-pair",
        "duplicated-marker-pair",
        "truncated-region",
        "trailing-byte-in-region",
        "marker-not-on-its-own-line",
        "end-marker-without-trailing-lf",
        "uncommitted-pair",
        "edited-working-tree-json",
        "edited-working-tree-markdown",
        "inconsistent-committed-json",
        "foreign-story-committed-json",
    ),
)
def test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions(
    tmp_path: Path, fault: str
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    if fault == "uncommitted-pair":
        v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
        markdown_bytes = v2_7_3_outputs(repository)[1]
    else:
        _, _, markdown_bytes = v2_7_3_passing_pair(fixture)
    v2_7_3_insert_record(repository, markdown_bytes)
    spec_path = repository / STORY_7_3_SPEC_PATH
    spec = spec_path.read_bytes()

    def mutate(repository_path: Path):
        if fault == "no-marker-pair":
            return v2_7_3_replace(spec_path, spec.replace(RECORD_BEGIN_LINE, b"<!-- removed -->"))
        if fault == "duplicated-marker-pair":
            return v2_7_3_replace(spec_path, spec + RECORD_BEGIN_LINE + b"\n" + RECORD_END_LINE + b"\n")
        if fault == "truncated-region":
            return v2_7_3_replace(spec_path, spec.replace(markdown_bytes, markdown_bytes[:-40]))
        if fault == "trailing-byte-in-region":
            return v2_7_3_replace(spec_path, spec.replace(RECORD_END_LINE, b"\n" + RECORD_END_LINE))
        if fault == "marker-not-on-its-own-line":
            return v2_7_3_replace(spec_path, spec.replace(RECORD_BEGIN_LINE, b"x " + RECORD_BEGIN_LINE))
        if fault == "end-marker-without-trailing-lf":
            assert spec.endswith(RECORD_END_LINE + b"\n")
            return v2_7_3_replace(spec_path, spec[:-1])
        if fault == "uncommitted-pair":
            return lambda: None
        target = repository_path / STORY_7_3_OUTPUT_JSON
        if fault == "edited-working-tree-json":
            return v2_7_3_replace(target, target.read_bytes().replace(b'"7.3"', b'"7.3" ', 1))
        if fault == "edited-working-tree-markdown":
            target = repository_path / STORY_7_3_OUTPUT_MARKDOWN
            return v2_7_3_replace(target, target.read_bytes() + b"dirty\n")
        # A canonically rendered but internally inconsistent, or foreign, JSON record
        # committed with a matching working tree reaches the pair-validity check.
        record = json.loads(target.read_bytes())
        if fault == "inconsistent-committed-json":
            record["rollback"]["boundary"] += " changed"
        else:
            record["storyId"] = "7.2"
        head = v2_git(repository_path, "rev-parse", "HEAD").stdout.strip()
        target.write_bytes((json.dumps(record, indent=2, ensure_ascii=False) + "\n").encode("utf-8"))
        v2_git(repository_path, "add", STORY_7_3_OUTPUT_JSON)
        v2_git(repository_path, "commit", "-q", "-m", "commit a record that does not verify")
        return lambda: v2_git(repository_path, "reset", "-q", "--keep", head)

    def check() -> None:
        document = v2_assert_failure(
            v2_run(v2_7_3_verify_arguments(repository)), {"RECORD_CONTENT_DRIFT"}
        )
        assert document["blockers"] == ["RECORD_CONTENT_DRIFT"]

    v2_7_3_isolated(repository, mutate, check)


def test_v2_workflow_verifies_inserted_digest_and_reproduces_the_pair_after_lifecycle_commits(
    tmp_path: Path,
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    _, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    v2_7_3_insert_record(repository, markdown_bytes)
    assert v2_run(v2_7_3_verify_arguments(repository)).returncode == 0
    spec = repository / STORY_7_3_SPEC_PATH
    spec.write_text(
        spec.read_text(encoding="utf-8")
        .replace(
            "status: 'in-progress'",
            "status: 'done'\nfollowup_review_recommended: false",
            1,
        )
        .replace(
            "## Verification\n\nFixture verification notes.",
            "## Verification\n\nFixture verification notes.\n\n"
            "## Review Triage Log\n\n- `[false]` fixture review row.\n\n"
            "## Auto Run Result\n\n- Fixture lifecycle summary.",
            1,
        ),
        encoding="utf-8",
    )
    sprint = repository / STORY_7_2_SPRINT_PATH
    sprint.write_text(
        sprint.read_text(encoding="utf-8")
        .replace(f"{STORY_7_3_SPRINT_ROW}: in-progress", f"{STORY_7_3_SPRINT_ROW}: review", 1)
        .replace("last_updated: 2026-09-29", "last_updated: 2026-09-30", 1),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_3_SPEC_PATH, STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "lifecycle bookkeeping")
    assert v2_run(v2_7_3_verify_arguments(repository)).returncode == 0
    record = v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    assert record["candidate"]["commit"] == fixture["candidate"]
    assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes)
    lifecycle = v2_snapshot(repository)

    spec.write_text(spec.read_text(encoding="utf-8") + "\nLater source edit.\n", encoding="utf-8")
    v2_git(repository, "add", STORY_7_3_SPEC_PATH)
    v2_git(repository, "commit", "-m", "change story source")
    failure = v2_assert_failure(v2_run(v2_7_3_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
    assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes)
    v2_git(repository, "reset", "--hard", "-q", lifecycle[0])
    assert v2_snapshot(repository) == lifecycle


def test_v2_workflow_verifies_inserted_digest_rejects_committed_source_changes(
    tmp_path: Path,
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    _, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    v2_7_3_insert_record(repository, markdown_bytes)
    assert v2_run(v2_7_3_verify_arguments(repository)).returncode == 0
    head = v2_git(repository, "rev-parse", "HEAD").stdout.strip()
    source = repository / STORY_7_3_BODIES[0]
    original = source.read_bytes()

    def fault(repository_path: Path):
        source.write_bytes(original + b"\nforbidden source change\n")
        v2_git(repository_path, "add", STORY_7_3_BODIES[0])
        v2_git(repository_path, "commit", "-m", "test: commit forbidden source change")
        return lambda: v2_git(repository_path, "reset", "-q", "--keep", head)

    def check() -> None:
        for phase in ("source-commit", "subsequent-revert"):
            if phase == "subsequent-revert":
                source.write_bytes(original)
                v2_git(repository, "add", STORY_7_3_BODIES[0])
                v2_git(repository, "commit", "-m", "test: revert forbidden source change")
            document = v2_assert_failure(
                v2_run(v2_7_3_verify_arguments(repository)), {"CANDIDATE_NOT_FINAL"}
            )
            assert document["blockers"] == ["CANDIDATE_NOT_FINAL"], phase
            assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes), phase

    v2_7_3_isolated(repository, fault, check)


def test_v2_workflow_accepts_a_separate_blocker_rollback_to_the_candidate_status(
    tmp_path: Path,
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    _, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    spec = repository / STORY_7_3_SPEC_PATH
    sprint = repository / STORY_7_2_SPRINT_PATH
    spec.write_text(
        spec.read_text(encoding="utf-8").replace("status: 'in-progress'", "status: 'done'", 1),
        encoding="utf-8",
    )
    sprint.write_text(
        sprint.read_text(encoding="utf-8")
        .replace(f"{STORY_7_3_SPRINT_ROW}: in-progress", f"{STORY_7_3_SPRINT_ROW}: review", 1)
        .replace("last_updated: 2026-09-29", "last_updated: 2026-09-30", 1),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_3_SPEC_PATH, STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "enter review")
    spec.write_text(
        spec.read_text(encoding="utf-8").replace("status: 'done'", "status: 'in-progress'", 1),
        encoding="utf-8",
    )
    sprint.write_text(
        sprint.read_text(encoding="utf-8")
        .replace(f"{STORY_7_3_SPRINT_ROW}: review", f"{STORY_7_3_SPRINT_ROW}: in-progress", 1),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_3_SPEC_PATH, STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "roll back blocked review")

    record = v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    assert record["candidate"]["commit"] == fixture["candidate"]
    assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes)


@pytest.mark.parametrize("original_value", (None, "false", "true"))
def test_v2_workflow_accepts_followup_as_the_final_frontmatter_field(
    tmp_path: Path, original_value: str | None,
) -> None:
    def seed(repository: Path) -> None:
        if original_value is not None:
            spec = repository / STORY_7_3_SPEC_PATH
            spec.write_bytes(spec.read_bytes().replace(
                b"\n---\n", f"\nfollowup_review_recommended: {original_value}\n---\n".encode(), 1,
            ))

    fixture = build_v2_7_3_repository(tmp_path, results=False, mutate=seed)
    repository = fixture["repository"]
    original = (repository / STORY_7_3_SPEC_PATH).read_bytes()
    changed = original.replace(b"status: 'in-progress'", b"status: 'done'", 1)
    if original_value is None:
        changed = changed.replace(b"\n---\n", b"\nfollowup_review_recommended: true\n---\n", 1)
    else:
        replacement = "false" if original_value == "true" else "true"
        changed = changed.replace(
            f"followup_review_recommended: {original_value}\n---\n".encode(),
            f"followup_review_recommended: {replacement}\n---\n".encode(), 1,
        )

    assert load_generator().v2_working_spec_lifecycle_only_change(
        repository, fixture["candidate"], STORY_7_3_SPEC_PATH, changed, record_region=True,
    )


@pytest.mark.parametrize("phase", ("first-add", "replacement"))
def test_v2_retained_candidate_requires_pair_changes_to_be_record_only(
    tmp_path: Path, phase: str
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    if phase == "first-add":
        v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    else:
        v2_7_3_passing_pair(fixture)
        for scenario_id in ("AC-7.3-01", "AC-7.3-02"):
            v2_7_3_assert_verifier(v2_7_3_verify(repository, scenario_id), 0, [])
        v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    spec = repository / STORY_7_3_SPEC_PATH
    spec.write_text(
        spec.read_text(encoding="utf-8").replace("status: 'in-progress'", "status: 'done'", 1),
        encoding="utf-8",
    )
    v2_git(
        repository,
        "add",
        STORY_7_3_OUTPUT_JSON,
        STORY_7_3_OUTPUT_MARKDOWN,
        STORY_7_3_SPEC_PATH,
    )
    v2_git(repository, "commit", "-m", "mix record and lifecycle changes")

    failure = v2_assert_failure(v2_run(v2_7_3_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
    assert failure["diagnostics"][0]["message"].endswith(
        ": " + ", ".join(sorted((STORY_7_3_OUTPUT_JSON, STORY_7_3_OUTPUT_MARKDOWN)))
    )


def test_v2_retained_candidate_rejects_an_intermediate_source_commit_that_is_reverted(
    tmp_path: Path,
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    _, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    source = repository / STORY_7_3_BODIES[0]
    original = source.read_bytes()
    source.write_bytes(original + b"\nintermediate source change\n")
    v2_git(repository, "add", STORY_7_3_BODIES[0])
    v2_git(repository, "commit", "-m", "intermediate source change")
    source.write_bytes(original)
    v2_git(repository, "add", STORY_7_3_BODIES[0])
    v2_git(repository, "commit", "-m", "revert intermediate source change")

    failure = v2_assert_failure(v2_run(v2_7_3_arguments(repository)), {"CANDIDATE_NOT_FINAL"})
    assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
    # The diagnostic names the path that broke retention, not only the commit.
    assert failure["diagnostics"][0]["message"].endswith(f": {STORY_7_3_BODIES[0]}")
    assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes)


def test_v2_retained_candidate_rejects_a_lifecycle_commit_before_the_record_only_commit(
    tmp_path: Path,
) -> None:
    """Every commit after the candidate must carry the pair, so lifecycle cannot come first."""
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    json_bytes, markdown_bytes = v2_7_3_outputs(repository)
    spec = repository / STORY_7_3_SPEC_PATH
    sprint = repository / STORY_7_2_SPRINT_PATH
    spec.write_text(
        spec.read_text(encoding="utf-8").replace("status: 'in-progress'", "status: 'done'", 1),
        encoding="utf-8",
    )
    sprint.write_text(
        sprint.read_text(encoding="utf-8")
        .replace(f"{STORY_7_3_SPRINT_ROW}: in-progress", f"{STORY_7_3_SPRINT_ROW}: review", 1)
        .replace("last_updated: 2026-09-29", "last_updated: 2026-09-30", 1),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_3_SPEC_PATH, STORY_7_2_SPRINT_PATH)
    v2_git(repository, "commit", "-m", "lifecycle bookkeeping before the record")
    lifecycle = v2_git(repository, "rev-parse", "HEAD").stdout.strip()
    v2_git(repository, "add", STORY_7_3_OUTPUT_JSON, STORY_7_3_OUTPUT_MARKDOWN)
    v2_git(repository, "commit", "-m", "record-only commit after lifecycle")
    v2_7_3_insert_record(repository, markdown_bytes)
    before = v2_snapshot(repository)

    for arguments in (v2_7_3_arguments(repository), v2_7_3_verify_arguments(repository)):
        failure = v2_assert_failure(v2_run(arguments), {"CANDIDATE_NOT_FINAL"})
        assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
        # The lifecycle commit is otherwise valid; it fails only because it lacks the pair.
        assert lifecycle in failure["diagnostics"][0]["message"]
        assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes)
        assert v2_snapshot(repository) == before


@pytest.mark.parametrize(
    ("heading", "route_owned"),
    (("## Review Triage Log", True), ("## Auto Run Result", True), ("## Unowned Notes", False)),
)
def test_v2_retained_candidate_masks_a_route_section_that_trails_the_inserted_record(
    tmp_path: Path, heading: str, route_owned: bool
) -> None:
    """A route-owned section appended after the record at end of file is lifecycle state."""
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    _, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    v2_7_3_insert_record(repository, markdown_bytes)
    spec = repository / STORY_7_3_SPEC_PATH
    content = spec.read_bytes().replace(b"status: 'in-progress'", b"status: 'done'", 1)
    assert content.endswith(RECORD_END_LINE + b"\n")
    spec.write_bytes(content + b"\n" + heading.encode("utf-8") + b"\n\n- Fixture row.\n")

    verified = v2_run(v2_7_3_verify_arguments(repository))
    if route_owned:
        assert verified.returncode == 0, verified.stdout
        assert verified.stdout == json_bytes
    else:
        document = v2_assert_failure(verified, {"RECORD_CONTENT_DRIFT"})
        assert document["blockers"] == ["RECORD_CONTENT_DRIFT"]
    v2_git(repository, "add", STORY_7_3_SPEC_PATH)
    v2_git(repository, "commit", "-m", "lifecycle with a trailing section")

    result = v2_run(v2_7_3_arguments(repository))
    if route_owned:
        record = v2_7_3_assert_pass(repository, result)
        assert record["candidate"]["commit"] == fixture["candidate"]
    else:
        failure = v2_assert_failure(result, {"CANDIDATE_NOT_FINAL"})
        assert failure["blockers"] == ["CANDIDATE_NOT_FINAL"]
    assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes)


@pytest.mark.parametrize("fault", ("unrelated-dirt", "unrelated-spec-edit"))
def test_v2_verify_inserted_rejects_changes_outside_the_designated_record(
    tmp_path: Path, fault: str
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    _, _, markdown_bytes = v2_7_3_passing_pair(fixture)
    v2_7_3_insert_record(repository, markdown_bytes)
    if fault == "unrelated-dirt":
        extra = repository / "unrelated.txt"
        extra.write_text("dirty\n", encoding="utf-8")
        expected = "WORKTREE_NOT_CLEAN"
    else:
        spec = repository / STORY_7_3_SPEC_PATH
        spec.write_text(spec.read_text(encoding="utf-8") + "\nUnrelated spec edit.\n", encoding="utf-8")
        expected = "RECORD_CONTENT_DRIFT"

    document = v2_assert_failure(v2_run(v2_7_3_verify_arguments(repository)), {expected})
    assert document["blockers"] == [expected]


def test_v2_workflow_verifies_inserted_digest_in_a_preseeded_marker_pair(tmp_path: Path) -> None:
    def seed(repository: Path) -> None:
        spec = repository / STORY_7_3_SPEC_PATH
        spec.write_bytes(spec.read_bytes() + b"\n" + RECORD_BEGIN_LINE + b"\n" + RECORD_END_LINE + b"\n")

    fixture = build_v2_7_3_repository(tmp_path, mutate=seed)
    repository = fixture["repository"]
    _, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    spec = repository / STORY_7_3_SPEC_PATH
    spec.write_bytes(spec.read_bytes().replace(RECORD_END_LINE, markdown_bytes + RECORD_END_LINE, 1))
    assert v2_run(v2_7_3_verify_arguments(repository)).returncode == 0
    v2_git(repository, "add", STORY_7_3_SPEC_PATH)
    v2_git(repository, "commit", "-m", "insert record")
    record = v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    assert record["candidate"]["commit"] == fixture["candidate"]
    assert v2_7_3_outputs(repository) == (json_bytes, markdown_bytes)


def test_v2_story_7_3_accepts_results_rerun_after_the_record_only_commit(tmp_path: Path) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    _, json_bytes, markdown_bytes = v2_7_3_passing_pair(fixture)
    heads = [v2_git(repository, "rev-parse", "HEAD").stdout.strip()]
    v2_7_3_insert_record(repository, markdown_bytes)
    spec = repository / STORY_7_3_SPEC_PATH
    spec.write_text(
        spec.read_text(encoding="utf-8").replace("status: 'in-progress'", "status: 'done'", 1),
        encoding="utf-8",
    )
    v2_git(repository, "add", STORY_7_3_SPEC_PATH)
    v2_git(repository, "commit", "-m", "lifecycle bookkeeping")
    heads.append(v2_git(repository, "rev-parse", "HEAD").stdout.strip())
    original = json.loads(json_bytes)
    for head in heads:
        for path, content in zip((STORY_7_3_OUTPUT_JSON, STORY_7_3_OUTPUT_MARKDOWN),
                                 (json_bytes, markdown_bytes)):
            (repository / path).write_bytes(content)
        v2_git(repository, "checkout", "-q", "--detach", head)
        for scenario_id in ("AC-7.3-01", "AC-7.3-02"):
            document = v2_7_3_assert_verifier(v2_7_3_verify(repository, scenario_id), 0, [])
            assert document["candidate"] == head != fixture["candidate"]
        record = v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
        # The retained candidate and every derived fact survive; only the two rerun
        # results are rebound, because the record binds each result file's digest.
        assert record["candidate"] == original["candidate"]
        assert record["workflowIntegration"] == original["workflowIntegration"]
        assert record["scenarios"][2:] == original["scenarios"][2:]
        for scenario in record["scenarios"][:2]:
            assert scenario["result"] == "PASS"
            assert scenario["resultFile"]["sha256"] == sha256_file(
                repository / scenario["resultFile"]["path"]
            )
        regenerated = v2_7_3_outputs(repository)
        v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
        assert v2_7_3_outputs(repository) == regenerated


# ---- AC-7.3-04: v2_fault_removed_workflow_invocation ----------------------- #


@pytest.mark.parametrize("body", STORY_7_3_BODIES)
def test_v2_fault_removed_workflow_invocation(tmp_path: Path, body: str) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    route = v2_7_3_route(body)
    original = (repository / body).read_text(encoding="utf-8")

    def check() -> None:
        mutated = (repository / body).read_text(encoding="utf-8")
        assert COMPLETION_VERIFIER.BLOCK_BEGIN not in mutated
        # The transition itself is untouched; only its gate is gone.
        assert mutated.count(route.transition) == original.count(route.transition)
        document = v2_7_3_assert_verifier(
            v2_7_3_verify(repository, "AC-7.3-01"), 1, ["WORKFLOW_INTEGRATION_MISSING"]
        )
        failed = {row["subject"] for row in document["assertionLedger"] if row["state"] == "FAIL"}
        assert failed == {
            f"{surface}::{check_name}"
            for surface in v2_7_3_affected(body)
            for check_name in (
                "completion-gate-block-present",
                "generator-invocation-in-block",
                "block-in-gate-span-before-transition",
            )
        }

    v2_7_3_isolated(repository, lambda path: v2_7_3_remove_block(path, body), check)
    v2_7_3_assert_verifier(v2_7_3_verify(repository, "AC-7.3-01"), 0, [])


@pytest.mark.parametrize("body", STORY_7_3_BODIES)
def test_v2_fault_removed_workflow_invocation_inside_the_block(tmp_path: Path, body: str) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    text = (repository / body).read_text(encoding="utf-8")
    gutted = text.replace(COMPLETION_VERIFIER.GENERATOR_COMMAND, "the record is optional")
    assert gutted != text

    def check() -> None:
        document = v2_7_3_assert_verifier(
            v2_7_3_verify(repository, "AC-7.3-01"), 1, ["WORKFLOW_INTEGRATION_MISSING"]
        )
        assert {row["subject"] for row in document["assertionLedger"] if row["state"] == "FAIL"} == {
            f"{surface}::generator-invocation-in-block" for surface in v2_7_3_affected(body)
        }

    v2_7_3_isolated(
        repository, lambda path: v2_7_3_replace(path / body, gutted.encode("utf-8")), check
    )


@pytest.mark.parametrize("variant", ("end-removed", "stray-end", "end-before-begin"))
@pytest.mark.parametrize("body", STORY_7_3_BODIES)
def test_v2_fault_removed_workflow_invocation_marker_structure(
    tmp_path: Path, body: str, variant: str
) -> None:
    """An incomplete or out-of-order marker pair is no block at all, never a partial one."""
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    text = (repository / body).read_text(encoding="utf-8")
    begin = COMPLETION_VERIFIER.BLOCK_BEGIN + "\n"
    end = COMPLETION_VERIFIER.BLOCK_END + "\n"
    assert text.count(begin) == 1 and text.count(end) == 1
    if variant == "end-removed":
        mutated = text.replace(end, "", 1)
    elif variant == "stray-end":
        mutated = text.replace(end, end + end, 1)
    else:
        mutated = text.replace(end, "", 1).replace(begin, end + begin, 1)
    assert mutated != text
    affected = v2_7_3_affected(body)

    def check() -> None:
        presence = v2_7_3_assert_verifier(
            v2_7_3_verify(repository, "AC-7.3-01"), 1, ["WORKFLOW_INTEGRATION_MISSING"]
        )
        assert {row["subject"] for row in presence["assertionLedger"] if row["state"] == "FAIL"} == {
            f"{surface}::{check_name}"
            for surface in affected
            for check_name in (
                "completion-gate-block-present",
                "generator-invocation-in-block",
                "block-in-gate-span-before-transition",
            )
        }
        parity = v2_7_3_assert_verifier(
            v2_7_3_verify(repository, "AC-7.3-02"), 1, ["SURFACE_PARITY_DRIFT"]
        )
        assert {row["subject"] for row in parity["assertionLedger"] if row["state"] == "FAIL"} == {
            f"{surface}::{check_name}"
            for surface in affected
            for check_name in ("block-bytes-identical-across-surfaces", "block-states-gate-contract")
        }

    v2_7_3_isolated(
        repository, lambda path: v2_7_3_replace(path / body, mutated.encode("utf-8")), check
    )


@pytest.mark.parametrize("twin", STORY_7_3_TWINS)
def test_v2_fault_removed_workflow_invocation_in_a_render_twin(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str], twin: str
) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    module = load_completion_verifier()
    original = module.render_twins

    def without_block(repository_path: Path) -> dict[str, str]:
        twins = original(repository_path)
        start, end = v2_7_3_block_span(twins[twin])
        twins[twin] = twins[twin][:start] + twins[twin][end:]
        return twins

    def check() -> None:
        monkeypatch.setattr(module, "render_twins", without_block)
        exit_code, document = v2_7_3_verify_in_process(module, repository, "AC-7.3-01", capsys)
        monkeypatch.undo()
        assert exit_code == 1
        assert document["blockers"] == ["WORKFLOW_INTEGRATION_MISSING"]
        assert {row["subject"] for row in document["assertionLedger"] if row["state"] == "FAIL"} == {
            f"{twin}::completion-gate-block-present",
            f"{twin}::generator-invocation-in-block",
            f"{twin}::block-in-gate-span-before-transition",
        }

    v2_7_3_isolated(repository, lambda path: (lambda: None), check)


def test_v2_fault_removed_workflow_invocation_blocks_the_record(tmp_path: Path) -> None:
    body = ".claude/skills/bmad-build/step-05-present.md"

    def remove(repository: Path) -> None:
        text = (repository / body).read_text(encoding="utf-8")
        start, end = v2_7_3_block_span(text)
        (repository / body).write_text(text[:start] + text[end + 1 :], encoding="utf-8")

    fixture = build_v2_7_3_repository(tmp_path, results=False, mutate=remove)
    repository = fixture["repository"]
    v2_7_3_assert_verifier(v2_7_3_verify(repository, "AC-7.3-01"), 1, ["WORKFLOW_INTEGRATION_MISSING"])
    v2_7_3_assert_verifier(v2_7_3_verify(repository, "AC-7.3-02"), 1, ["SURFACE_PARITY_DRIFT"])
    lifecycle = v2_7_3_lifecycle(repository)
    before = v2_snapshot(repository)
    document = v2_assert_failure(
        v2_run(v2_7_3_arguments(repository)),
        {"WORKFLOW_INTEGRATION_MISSING", "SURFACE_PARITY_DRIFT", "TEST_RESULTS_FAILED"},
    )
    assert "TEST_RESULTS_MISSING" in document["blockers"]  # AC-7.3-03..06 were not run here
    assert v2_7_3_outputs(repository) == (None, None)
    assert v2_7_3_lifecycle(repository) == lifecycle
    assert v2_snapshot(repository) == before


# ---- AC-7.3-05: v2_fault_displaced_workflow_invocation --------------------- #


@pytest.mark.parametrize("body", STORY_7_3_BODIES)
def test_v2_fault_displaced_workflow_invocation(tmp_path: Path, body: str) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    route = v2_7_3_route(body)
    text = (repository / body).read_text(encoding="utf-8")
    start, end = v2_7_3_block_span(text)
    block = text[start:end]
    decoy = f"Decoy text: `{COMPLETION_VERIFIER.GENERATOR_COMMAND}`"
    displaced = text[:start] + decoy + text[end:] + "\n" + block + "\n"

    def check() -> None:
        mutated = (repository / body).read_text(encoding="utf-8")
        gate = mutated.index(route.gate)
        follower = mutated.index(route.follower, gate)
        # Whole-file and in-span vocabulary are both still present: they cannot pass.
        assert COMPLETION_VERIFIER.GENERATOR_COMMAND in mutated[gate:follower]
        assert mutated.index(COMPLETION_VERIFIER.BLOCK_BEGIN) > mutated.index(route.transition)
        document = v2_7_3_assert_verifier(
            v2_7_3_verify(repository, "AC-7.3-01"), 1, ["WORKFLOW_INTEGRATION_DISPLACED"]
        )
        assert {row["subject"] for row in document["assertionLedger"] if row["state"] == "FAIL"} == {
            f"{surface}::block-in-gate-span-before-transition" for surface in v2_7_3_affected(body)
        }

    v2_7_3_isolated(
        repository, lambda path: v2_7_3_replace(path / body, displaced.encode("utf-8")), check
    )
    v2_7_3_assert_verifier(v2_7_3_verify(repository, "AC-7.3-01"), 0, [])


@pytest.mark.parametrize("twin", STORY_7_3_TWINS)
def test_v2_fault_displaced_workflow_invocation_in_a_render_twin(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str], twin: str
) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    module = load_completion_verifier()
    original = module.render_twins

    def displaced(repository_path: Path) -> dict[str, str]:
        twins = original(repository_path)
        start, end = v2_7_3_block_span(twins[twin])
        block = twins[twin][start:end]
        twins[twin] = twins[twin][:start] + twins[twin][end:] + "\n" + block + "\n"
        return twins

    def check() -> None:
        monkeypatch.setattr(module, "render_twins", displaced)
        exit_code, document = v2_7_3_verify_in_process(module, repository, "AC-7.3-01", capsys)
        monkeypatch.undo()
        assert exit_code == 1
        assert document["blockers"] == ["WORKFLOW_INTEGRATION_DISPLACED"]

    v2_7_3_isolated(repository, lambda path: (lambda: None), check)


@pytest.mark.parametrize(
    "variant",
    (
        "before-the-gate-heading",
        "decoy-gate-heading",
        "gate-heading-removed",
        "transition-moved-into-the-span",
        "only-transition-moved-before-the-gate",
        "only-transition-moved-into-the-span",
        "second-block-after-the-transition",
    ),
)
def test_v2_fault_displaced_workflow_invocation_variants(tmp_path: Path, variant: str) -> None:
    body = ".agents/skills/bmad-code-review/steps/step-04-present.md"
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    route = v2_7_3_route(body)
    text = (repository / body).read_text(encoding="utf-8")
    start, end = v2_7_3_block_span(text)
    block = text[start:end]
    if variant == "before-the-gate-heading":
        without = text[:start] + text[end:]
        gate = without.index(route.gate)
        mutated = without[:gate] + block + "\n\n" + without[gate:]
    elif variant == "decoy-gate-heading":
        mutated = route.gate + "\n\n" + text
    elif variant == "gate-heading-removed":
        mutated = text.replace(route.gate, "#### Removed gate", 1)
    elif variant == "transition-moved-into-the-span":
        mutated = text[:end] + f"\n{route.transition}\n" + text[end:]
    elif variant.startswith("only-transition-moved"):
        # Relocate the single transition, so only the transition-before-span-end rule can fire.
        assert text.count(route.transition) == 1 and text.index(route.transition) > end
        without = text.replace(route.transition, "the review outcome", 1)
        anchor = without.index(route.gate) if variant.endswith("before-the-gate") else start
        mutated = without[:anchor] + route.transition + "\n\n" + without[anchor:]
    else:
        mutated = text + "\n" + block + "\n"

    def check() -> None:
        result = v2_7_3_verify(repository, "AC-7.3-01")
        document = v2_7_3_assert_verifier(result, 1, ["WORKFLOW_INTEGRATION_DISPLACED"])
        if variant.startswith("only-transition-moved"):
            assert b"a lifecycle transition precedes the end of the gate span" in result.stderr
            assert {
                row["subject"] for row in document["assertionLedger"] if row["state"] == "FAIL"
            } == {f"{body}::block-in-gate-span-before-transition"}

    v2_7_3_isolated(
        repository, lambda path: v2_7_3_replace(path / body, mutated.encode("utf-8")), check
    )


# ---- AC-7.3-06: v2_blocker_prevents_state_transition ----------------------- #


def test_v2_blocker_prevents_state_transition_when_generation_fails(tmp_path: Path) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    lifecycle = v2_7_3_lifecycle(repository)
    result = repository / STORY_7_3_RESULTS / "AC-7.3-02.json"
    failed = json.loads(result.read_bytes())
    failed.update(
        exitCode=1,
        result="FAIL",
        blockers=["SURFACE_PARITY_DRIFT"],
        assertionLedger=[dict(failed["assertionLedger"][0], state="FAIL")],
    )

    def fault(repository_path: Path):
        return v2_7_3_replace(result, (json.dumps(failed, indent=2) + "\n").encode("utf-8"))

    def check() -> None:
        document = v2_assert_failure(
            v2_run(v2_7_3_arguments(repository)), {"SURFACE_PARITY_DRIFT", "TEST_RESULTS_FAILED"}
        )
        assert document["blockers"] == ["SURFACE_PARITY_DRIFT", "TEST_RESULTS_FAILED"]
        assert all(item["subject"] == "AC-7.3-02" for item in document["diagnostics"])
        assert v2_7_3_outputs(repository) == (None, None)
        assert v2_7_3_lifecycle(repository) == lifecycle

    v2_7_3_isolated(repository, fault, check)
    v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))


@pytest.mark.parametrize("environment", ("schemas-unavailable", "git-fails"))
def test_v2_blocker_prevents_state_transition_when_generation_is_blocked(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    capsys: pytest.CaptureFixture[str],
    environment: str,
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    lifecycle = v2_7_3_lifecycle(repository)
    before = v2_snapshot(repository)
    module = load_generator()
    if environment == "schemas-unavailable":
        empty = tmp_path / "no-schemas"
        empty.mkdir()
        monkeypatch.setattr(module, "V2_SCHEMA_DIRECTORY", empty)
        expected = ["SCHEMA_UNAVAILABLE"]
    else:
        original = module.run_git

        def failing(repository_path: Path, *arguments: str, **options):
            if arguments[:2] == ("ls-tree", "-r"):
                raise module.GateError("GIT_COMMAND_FAILED", "forced fixture failure")
            return original(repository_path, *arguments, **options)

        monkeypatch.setattr(module, "run_git", failing)
        expected = ["GIT_COMMAND_FAILED"]
    assert module.main(v2_7_3_arguments(repository)) == 2
    captured = capsys.readouterr()
    document = json.loads(captured.out)
    v2_failure_validator().validate(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"] == expected
    assert v2_7_3_outputs(repository) == (None, None)
    assert v2_7_3_lifecycle(repository) == lifecycle
    assert v2_snapshot(repository) == before


@pytest.mark.parametrize("surface", (*STORY_7_3_BODIES, *STORY_7_3_TWINS))
def test_v2_blocker_prevents_state_transition_on_every_surface(surface: str) -> None:
    """Each installed surface and render twin HALTs in progress and claims no CI gate."""
    if surface.startswith(COMPLETION_VERIFIER.TWIN_PREFIX):
        text = load_completion_verifier().render_twins(WORKSPACE)[surface]
    else:
        text = (WORKSPACE / surface).read_text(encoding="utf-8")
    route = v2_7_3_route(surface)
    start, end = v2_7_3_block_span(text)
    block = text[start:end]
    assert COMPLETION_VERIFIER.placement_problem(text, route, start, end) is None
    branch = block.index("Blocker branch:")
    assert branch > block.index(COMPLETION_VERIFIER.GENERATOR_COMMAND)
    assert branch > block.index(COMPLETION_VERIFIER.VERIFY_COMMAND)
    blocker_branch = block[branch:]
    for clause in (
        "on any nonzero exit, summary mismatch, required commit that is not authorized, "
        "commit failure, or verification failure",
        "keep or return `{spec_file}` and the story's sprint-status row to `in-progress`",
        "never write `review` or `done`",
        "Report the exact command, its exit, and every stable blocker code, then HALT",
    ):
        assert clause in blocker_branch, (surface, clause)
    assert "no CI job or hook enforces it" in block
    assert COMPLETION_VERIFIER.CI_CLAIM.search(block) is None
    assert COMPLETION_VERIFIER.CI_CLAIM.search("this gate is enforced by CI") is not None


# ---- Story 7.3 verifier matrix and generator bindings ----------------------- #


def test_story_completion_verifier_passes_on_the_integrated_repository() -> None:
    module = load_completion_verifier()
    contract = json.loads(STORY_7_3_CONTRACT.read_bytes())
    candidate = run_git(WORKSPACE, "rev-parse", "HEAD").stdout.strip()
    expected_inputs = [
        {"path": path, "sha256": sha256_file(WORKSPACE / path)} for path in STORY_7_3_BODIES
    ]
    for scenario in contract["scenarios"][:2]:
        findings, rows, inputs = module.evaluate(WORKSPACE, candidate, scenario)
        assert findings == [], findings
        assert rows and all(state == "PASS" for _, state in rows)
        assert len(rows) == (3 if scenario["id"] == "AC-7.3-01" else 2) * 11
        assert inputs == expected_inputs


def test_story_completion_verifier_emits_schema_valid_acceptance_results(tmp_path: Path) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    for scenario_id in ("AC-7.3-01", "AC-7.3-02"):
        result = v2_7_3_verify(repository, scenario_id)
        document = v2_7_3_assert_verifier(result, 0, [])
        assert result.stderr == b""
        assert (repository / STORY_7_3_RESULTS / f"{scenario_id}.json").read_bytes() == result.stdout
        assert document["candidate"] == fixture["candidate"]
        assert document["command"] == next(
            item["command"] for item in json.loads(STORY_7_3_CONTRACT.read_bytes())["scenarios"]
            if item["id"] == scenario_id
        )
        assert [row["path"] for row in document["inputs"]] == sorted(STORY_7_3_BODIES)
        assert all(
            row["sha256"] == sha256_file(repository / row["path"]) for row in document["inputs"]
        )
        assert document["assertionLedger"]
        assert all(row["state"] == "PASS" for row in document["assertionLedger"])
    # The twins are rendered in memory only: no render snapshot is ever published.
    assert not (repository / "_bmad/render").exists()


def test_story_completion_verifier_preserves_parity_after_autocrlf_checkout(tmp_path: Path) -> None:
    def seed(repository: Path) -> None:
        (repository / ".gitattributes").write_bytes((WORKSPACE / ".gitattributes").read_bytes())
        (repository / "autocrlf-control.md").write_bytes(b"control\n")

    fixture = build_v2_7_3_repository(tmp_path, results=False, mutate=seed)
    repository = fixture["repository"]
    v2_git(repository, "config", "core.autocrlf", "true")
    for relative in ("autocrlf-control.md", *STORY_7_3_BODIES):
        (repository / relative).unlink()
    v2_git(repository, "checkout-index", "--force", "--", "autocrlf-control.md", *STORY_7_3_BODIES)
    assert (repository / "autocrlf-control.md").read_bytes() == b"control\r\n"
    for body in STORY_7_3_BODIES:
        content = (repository / body).read_bytes()
        assert b"\r" not in content, body
        assert sha256_file(repository / body) == hashlib.sha256(
            v2_git(repository, "show", f"HEAD:{body}").stdout.encode("utf-8")
        ).hexdigest()
    for scenario_id in ("AC-7.3-01", "AC-7.3-02"):
        document = v2_7_3_assert_verifier(v2_7_3_verify(repository, scenario_id), 0, [])
        assert all(row["state"] == "PASS" for row in document["assertionLedger"])
    assert not (repository / "_bmad/render").exists()


def test_story_completion_verifier_does_not_follow_the_predictable_temp_symlink(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch,
) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    module = load_completion_verifier()
    relative = f"{STORY_7_3_RESULTS}/AC-7.3-01.json"
    target = repository / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    outside = tmp_path / "outside.json"
    outside.write_bytes(b"outside\n")
    predictable = target.with_name(f".{target.name}.{os.getpid()}.tmp")
    predictable.symlink_to(outside)
    collision = target.with_name(f".{target.name}.collision.tmp")
    collision.symlink_to(outside)
    tokens = iter(("collision", "safe"))
    monkeypatch.setattr(module.secrets, "token_hex", lambda _: next(tokens))

    module.write_output(repository, relative, b"derived\n")

    assert target.read_bytes() == b"derived\n"
    assert outside.read_bytes() == b"outside\n"
    assert predictable.is_symlink()
    assert collision.is_symlink()


@pytest.mark.parametrize("collision_kind", ("regular-file", "symlink"))
def test_story_completion_verifier_preserves_collisions_when_temp_names_are_exhausted(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, collision_kind: str,
) -> None:
    repository = tmp_path / "repository"
    repository.mkdir()
    module = load_completion_verifier()
    relative = f"{STORY_7_3_RESULTS}/AC-7.3-01.json"
    target = repository / relative
    target.parent.mkdir(parents=True)
    target.write_bytes(b"prior result\n")
    outside = tmp_path / "outside.json"
    outside.write_bytes(b"outside\n")
    collision = target.with_name(f".{target.name}.collision.tmp")
    if collision_kind == "symlink":
        collision.symlink_to(outside)
    else:
        collision.write_bytes(b"occupied\n")
    monkeypatch.setattr(module.secrets, "token_hex", lambda _: "collision")

    with pytest.raises(module.Blocked) as stopped:
        module.write_output(repository, relative, b"derived\n")

    assert stopped.value.finding["code"] == "OUTPUT_WRITE_FAILED"
    assert target.read_bytes() == b"prior result\n"
    assert outside.read_bytes() == b"outside\n"
    assert collision.is_symlink() == (collision_kind == "symlink")
    assert collision.read_bytes() == (b"outside\n" if collision_kind == "symlink" else b"occupied\n")


# Every clause the one block must state, written out here rather than imported, so
# a clause the verifier stops requiring is caught instead of silently shared.
STORY_7_3_REQUIRED_BLOCK_CLAUSES = (
    "uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . "
    "--contract <contract> --format bundle --output-json <json> --output-markdown <md>",
    "`_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json`",
    "the spec cannot opt out",
    "`finalRecord.paths`",
    "Require exit `0`",
    "`finalRecord.summary`",
    "record-only commit",
    "`<!-- STORY-FINAL-RECORD:BEGIN -->`",
    "`<!-- STORY-FINAL-RECORD:END -->`",
    "uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . "
    "--contract <contract> --verify-inserted-record {spec_file}",
    "to `in-progress`",
    "never write `review` or `done`",
    "Report the exact command, its exit, and every stable blocker code",
    "then HALT",
    "no CI job or hook enforces it",
)
V2_7_3_DRIFTS = (
    "one-body-byte",
    "one-tree-only",
    "uniform-ci-claim",
    "uniform-render-token",
    *(f"uniform-clause-loss-{index:02d}" for index in range(len(STORY_7_3_REQUIRED_BLOCK_CLAUSES))),
)


def test_story_completion_verifier_requires_exactly_the_listed_block_clauses() -> None:
    required = {clause for _, clause in COMPLETION_VERIFIER.REQUIRED_CLAUSES}
    assert set(STORY_7_3_REQUIRED_BLOCK_CLAUSES) == required | {COMPLETION_VERIFIER.GENERATOR_COMMAND}


@pytest.mark.parametrize("drift", V2_7_3_DRIFTS)
def test_story_completion_verifier_reports_surface_parity_drift(tmp_path: Path, drift: str) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    first = STORY_7_3_BODIES[0]
    if drift in ("one-body-byte", "one-tree-only"):
        targets = [first if drift == "one-body-byte" else ".claude/skills/bmad-build/step-oneshot.md"]
        change = ("then HALT.", "then HALT!")
    elif drift == "uniform-ci-claim":
        targets = list(STORY_7_3_BODIES)
        change = ("no CI job or hook enforces it.", "no CI job or hook enforces it; CI enforces it.")
    elif drift == "uniform-render-token":
        # A real render resolves this token, so only the twins' bytes change.
        targets = list(STORY_7_3_BODIES)
        change = ("then HALT.", "then HALT {{.implementation_artifacts}}.")
    else:
        targets = list(STORY_7_3_BODIES)
        clause = STORY_7_3_REQUIRED_BLOCK_CLAUSES[int(drift.rsplit("-", 1)[1])]
        change = (clause, "removed clause")

    def in_block(text: str) -> str:
        start, end = v2_7_3_block_span(text)
        block = text[start:end]
        assert change[0] in block
        count = 1 if drift in ("one-body-byte", "one-tree-only") else -1
        return text[:start] + block.replace(*change, count) + text[end:]

    def fault(repository_path: Path):
        restores = [
            v2_7_3_replace(
                repository_path / target,
                in_block((repository_path / target).read_text(encoding="utf-8")).encode("utf-8"),
            )
            for target in targets
        ]
        return lambda: [restore() for restore in restores]

    def check() -> None:
        document = v2_7_3_assert_verifier(
            v2_7_3_verify(repository, "AC-7.3-02"), 1, ["SURFACE_PARITY_DRIFT"]
        )
        failed_rows = {
            row["subject"] for row in document["assertionLedger"] if row["state"] == "FAIL"
        }
        failed = {subject.split("::")[0] for subject in failed_rows}
        if drift == "one-body-byte":
            assert failed == {first}
        elif drift == "one-tree-only":
            # The twin renders from the .claude tree, so it drifts with its body.
            assert failed == {targets[0], f"render:{targets[0]}"}
        elif drift == "uniform-render-token":
            assert failed_rows == {
                f"{twin}::block-bytes-identical-across-surfaces" for twin in STORY_7_3_TWINS
            }
        elif drift == "uniform-ci-claim":
            assert failed == {*STORY_7_3_BODIES, *STORY_7_3_TWINS}
        else:
            assert failed_rows == {
                f"{surface}::block-states-gate-contract"
                for surface in (*STORY_7_3_BODIES, *STORY_7_3_TWINS)
            }
        if change[0] != STORY_7_3_REQUIRED_BLOCK_CLAUSES[0]:
            v2_7_3_assert_verifier(v2_7_3_verify(repository, "AC-7.3-01"), 0, [])

    v2_7_3_isolated(repository, fault, check)


def test_story_completion_verifier_reports_render_twin_drift(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    module = load_completion_verifier()
    original = module.render_twins
    twin = STORY_7_3_TWINS[0]

    def drifted(repository_path: Path) -> dict[str, str]:
        twins = original(repository_path)
        twins[twin] = twins[twin].replace("then HALT.", "then HALT!", 1)
        return twins

    def check() -> None:
        monkeypatch.setattr(module, "render_twins", drifted)
        exit_code, document = v2_7_3_verify_in_process(module, repository, "AC-7.3-02", capsys)
        monkeypatch.undo()
        assert exit_code == 1
        assert document["blockers"] == ["SURFACE_PARITY_DRIFT"]
        failed = {row["subject"] for row in document["assertionLedger"] if row["state"] == "FAIL"}
        assert failed == {f"{twin}::block-bytes-identical-across-surfaces"}

    v2_7_3_isolated(repository, lambda path: (lambda: None), check)


@pytest.mark.parametrize(
    "fault",
    (
        "contract-not-7.3",
        "unknown-scenario",
        "undeclared-output",
        "abbreviated-option",
        "unreadable-body",
        "render-configuration-missing",
        "output-escapes",
    ),
)
def test_story_completion_verifier_blocks_unreadable_inputs(tmp_path: Path, fault: str) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    base = [
        "--repository",
        str(repository),
        "--contract",
        STORY_7_3_CONTRACT_PATH,
        "--scenario",
        "AC-7.3-01",
        "--output",
        f"{STORY_7_3_RESULTS}/AC-7.3-01.json",
    ]
    arguments = list(base)
    mutate = lambda path: (lambda: None)  # noqa: E731
    if fault == "contract-not-7.3":
        arguments[3] = STORY_7_2_CONTRACT_PATH
        target = repository / STORY_7_2_CONTRACT_PATH
        target.parent.mkdir(parents=True, exist_ok=True)

        def mutate(path: Path):
            target.write_bytes(STORY_7_2_CONTRACT.read_bytes())
            return target.unlink

        expected = "CONTRACT_UNSUPPORTED"
    elif fault == "unknown-scenario":
        arguments[5] = "AC-7.3-03"
        expected = "ARGUMENT_INVALID"
    elif fault == "undeclared-output":
        arguments[7] = f"{STORY_7_3_RESULTS}/elsewhere.json"
        expected = "ARGUMENT_INVALID"
    elif fault == "abbreviated-option":
        arguments[6] = "--out"
        expected = "ARGUMENT_INVALID"
    elif fault == "unreadable-body":
        mutate = lambda path: v2_7_3_replace(path / STORY_7_3_BODIES[0], b"\xff\xfe not utf-8\n")  # noqa: E731
        expected = "SURFACE_UNREADABLE"
    elif fault == "render-configuration-missing":
        mutate = lambda path: v2_7_3_replace(path / "_bmad/config.toml", b"not = [valid toml\n")  # noqa: E731
        expected = "RENDER_UNAVAILABLE"
    else:
        outside = tmp_path / "outside-results"
        outside.mkdir()
        results = repository / STORY_7_3_RESULTS

        def mutate(path: Path):
            results.parent.mkdir(parents=True, exist_ok=True)
            results.symlink_to(outside, target_is_directory=True)
            return results.unlink

        expected = "OUTPUT_WRITE_FAILED"

    def check() -> None:
        result = v2_7_3_verify(repository, "AC-7.3-01", *arguments)
        assert result.returncode == 2
        assert b"Traceback" not in result.stdout + result.stderr
        document = json.loads(result.stdout)
        assert document["result"] == "BLOCKED"
        assert document["exitCode"] == 2
        assert document["blockers"] == [expected]
        written = repository / STORY_7_3_RESULTS / "AC-7.3-01.json"
        if expected == "OUTPUT_WRITE_FAILED":
            # The derived result is reported, but nothing is written outside the repository.
            v2_7_3_acceptance_validator().validate(document)
            assert document["inputs"] == [] and document["assertionLedger"] == []
            assert list(outside.iterdir()) == []
        elif expected in ("SURFACE_UNREADABLE", "RENDER_UNAVAILABLE"):
            # A contract-bound blocked result is evidence: no input, no ledger, no PASS.
            v2_7_3_acceptance_validator().validate(document)
            assert document["inputs"] == [] and document["assertionLedger"] == []
            assert written.read_bytes() == result.stdout
        else:
            assert document["schemaVersion"] != "hexalith.conversations.acceptance-result.v1"
            assert not written.exists()

    v2_7_3_isolated(repository, mutate, check)


def test_story_completion_verifier_inventory_matches_the_generator() -> None:
    module = load_generator()
    assert tuple(COMPLETION_VERIFIER.BODY_PATHS) == module.V2_7_3_WORKFLOW_BODIES
    assert len(STORY_7_3_BODIES) == 8 and len(STORY_7_3_TWINS) == 3
    assert (
        set(COMPLETION_VERIFIER.FAIL_CODES) | set(COMPLETION_VERIFIER.BLOCKED_CODES)
        == set(module.V2_PROPAGATED_ACCEPTANCE_CODES)
    )
    runbook = RUNBOOK.read_text(encoding="utf-8")
    for code in (*COMPLETION_VERIFIER.FAIL_CODES, *COMPLETION_VERIFIER.BLOCKED_CODES):
        assert f"`{code}`" in runbook, code


def test_v2_story_7_3_propagates_every_stable_verifier_code(tmp_path: Path) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    path = repository / STORY_7_3_RESULTS / "AC-7.3-01.json"
    document = json.loads(path.read_bytes())
    codes = sorted(COMPLETION_VERIFIER.FAIL_CODES | COMPLETION_VERIFIER.BLOCKED_CODES)
    path.write_text(
        json.dumps(dict(document, exitCode=1, result="FAIL", blockers=codes), indent=2) + "\n",
        encoding="utf-8",
    )

    failure = v2_assert_failure(
        v2_run(v2_7_3_arguments(repository)),
        {"TEST_RESULTS_FAILED", *codes},
        exit_code=2,
    )
    assert set(failure["blockers"]) == {"TEST_RESULTS_FAILED", *codes}


def test_v2_story_7_3_record_binds_contract_bodies_and_predecessors(tmp_path: Path) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    first = v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    outputs = v2_7_3_outputs(repository)
    second = v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))
    assert first == second and v2_7_3_outputs(repository) == outputs
    integration = first["workflowIntegration"]
    assert integration["contract"] == {
        "path": STORY_7_3_CONTRACT_PATH,
        "sha256": sha256_file(STORY_7_3_CONTRACT),
    }
    assert integration["workflowBodies"] == [
        {"path": path, "sha256": sha256_file(repository / path)} for path in STORY_7_3_BODIES
    ]
    for scenario_id in ("AC-7.3-01", "AC-7.3-02"):
        result = json.loads((repository / STORY_7_3_RESULTS / f"{scenario_id}.json").read_bytes())
        assert result["inputs"] == integration["workflowBodies"]
    assert integration["predecessorRecords"] == [
        {"storyId": story, "path": path, "sha256": sha256_file(WORKSPACE / path)}
        for story, path in (
            ("7.1", "docs/release-evidence/story-7.1-final-record-v2.json"),
            ("7.2", "docs/release-evidence/story-7.2-final-record-v2.json"),
        )
    ]
    assert [scenario["result"] for scenario in first["scenarios"]] == ["PASS"] * 7
    assert first["scenarios"][0]["resultFile"]["path"] == f"{STORY_7_3_RESULTS}/AC-7.3-01.json"
    assert len(first["scenarios"][-1]["assertionLedger"]) == 12
    assert [row["subject"] for row in first["scenarios"][-1]["assertionLedger"][-3:]] == [
        "generator::acceptance-results-bound-to-candidate",
        "generator::workflow-bodies-equal-acceptance-inputs",
        "generator::predecessor-records-7.1-7.2-verified",
    ]
    assert "measurements" not in first
    markdown = outputs[1].decode("utf-8")
    assert "## Story 7.3 workflow integration" in markdown
    assert "candidate, measured JUnit results, and acceptance results" in markdown
    assert f"- Story contract: `{integration['contract']['path']}`" in markdown
    assert f"- Story contract SHA-256: `{integration['contract']['sha256']}`" in markdown
    for body in integration["workflowBodies"]:
        assert f"| `{body['path']}` | `{body['sha256']}` |" in markdown
    for item in integration["predecessorRecords"]:
        assert f"| `{item['storyId']}` | `{item['path']}` | `{item['sha256']}` |" in markdown
    assert "## Story 7.2 measurements" not in markdown


V2_7_3_ACCEPTANCE_FAULTS = {
    "missing": ("TEST_RESULTS_MISSING",),
    "stale-mtime": ("TEST_RESULTS_STALE",),
    "other-candidate": ("TEST_RESULTS_STALE",),
    "tampered-input-digest": ("TEST_RESULTS_STALE",),
    "command-mismatch": ("SCENARIO_RESULT_MISMATCH",),
    "story-id-mismatch": ("SCENARIO_RESULT_MISMATCH",),
    "scenario-id-mismatch": ("SCENARIO_RESULT_MISMATCH",),
    "state-exit-disagree": ("SCENARIO_RESULT_MISMATCH",),
    "inputs-not-governed-set": ("SCENARIO_RESULT_MISMATCH",),
    "malformed-json": ("INPUT_SCHEMA_INVALID",),
    "schema-violation": ("INPUT_SCHEMA_INVALID",),
    "symlinked-result": ("INPUT_SCHEMA_INVALID",),
    "ledger-id-mismatch": ("SCENARIO_RESULT_MISMATCH",),
    "duplicate-ledger-subject": ("SCENARIO_RESULT_MISMATCH",),
    "passing-with-failed-row": ("TEST_COUNT_INCONSISTENT",),
    "passing-with-blocker": ("TEST_COUNT_INCONSISTENT",),
    "passing-without-ledger": ("ASSERTION_LEDGER_EMPTY",),
    "failed-with-verifier-blocker": ("TEST_RESULTS_FAILED", "WORKFLOW_INTEGRATION_DISPLACED"),
}


@pytest.mark.parametrize("fault", sorted(V2_7_3_ACCEPTANCE_FAULTS))
def test_v2_story_7_3_acceptance_result_faults_block_the_record(tmp_path: Path, fault: str) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    path = repository / STORY_7_3_RESULTS / "AC-7.3-01.json"
    document = json.loads(path.read_bytes())

    def rewritten(**changes) -> bytes:
        return (json.dumps(dict(document, **changes), indent=2) + "\n").encode("utf-8")

    def mutate(repository_path: Path):
        if fault == "missing":
            original = path.read_bytes()
            path.unlink()
            return lambda: path.write_bytes(original)
        if fault == "stale-mtime":
            mtime = path.stat().st_mtime_ns
            os.utime(path, ns=(1, 1))
            return lambda: os.utime(path, ns=(mtime, mtime))
        if fault == "symlinked-result":
            outside = tmp_path / "outside-AC-7.3-01.json"
            outside.write_bytes(path.read_bytes())
            path.unlink()
            path.symlink_to(outside)
            return path.unlink
        content = {
            "other-candidate": lambda: rewritten(candidate=fixture["baseline"]),
            "tampered-input-digest": lambda: rewritten(
                inputs=[dict(document["inputs"][0], sha256="0" * 64), *document["inputs"][1:]]
            ),
            "command-mismatch": lambda: rewritten(command=document["command"] + " --extra"),
            "story-id-mismatch": lambda: rewritten(storyId="7.1"),
            "scenario-id-mismatch": lambda: rewritten(scenarioId="AC-7.3-02"),
            "state-exit-disagree": lambda: rewritten(exitCode=1),
            "inputs-not-governed-set": lambda: rewritten(inputs=document["inputs"][1:]),
            "malformed-json": lambda: b'{"schemaVersion": ',
            "schema-violation": lambda: rewritten(undeclared=True),
            "ledger-id-mismatch": lambda: rewritten(
                assertionLedger=[
                    dict(document["assertionLedger"][0], id="AC-7.3-01#9999"),
                    *document["assertionLedger"][1:],
                ]
            ),
            "duplicate-ledger-subject": lambda: rewritten(
                assertionLedger=[
                    document["assertionLedger"][0],
                    dict(
                        document["assertionLedger"][1],
                        subject=document["assertionLedger"][0]["subject"],
                    ),
                    *document["assertionLedger"][2:],
                ]
            ),
            "passing-with-failed-row": lambda: rewritten(
                assertionLedger=[dict(document["assertionLedger"][0], state="FAIL")]
            ),
            "passing-with-blocker": lambda: rewritten(blockers=["WORKFLOW_INTEGRATION_MISSING"]),
            "passing-without-ledger": lambda: rewritten(assertionLedger=[]),
            "failed-with-verifier-blocker": lambda: rewritten(
                exitCode=1, result="FAIL", blockers=["WORKFLOW_INTEGRATION_DISPLACED"]
            ),
        }[fault]()
        return v2_7_3_replace(path, content)

    def check() -> None:
        expected = set(V2_7_3_ACCEPTANCE_FAULTS[fault])
        failure = v2_assert_failure(v2_run(v2_7_3_arguments(repository)), expected)
        assert set(failure["blockers"]) == expected
        assert v2_7_3_outputs(repository) == (None, None)

    v2_7_3_isolated(repository, mutate, check)
    v2_7_3_assert_pass(repository, v2_run(v2_7_3_arguments(repository)))


def test_v2_story_7_3_acceptance_command_requires_a_committed_script(tmp_path: Path) -> None:
    """A verifier script present in the working tree but absent from the candidate cannot pass."""
    script = "_bmad/scripts/verify_story_completion_workflows.py"

    def keep_script_uncommitted(repository: Path) -> None:
        exclude = repository / ".git/info/exclude"
        exclude.parent.mkdir(parents=True, exist_ok=True)
        existing = exclude.read_text(encoding="utf-8") if exclude.exists() else ""
        exclude.write_text(f"{existing}/{script}\n", encoding="utf-8")

    fixture = build_v2_7_3_repository(tmp_path, mutate=keep_script_uncommitted)
    repository = fixture["repository"]
    assert (repository / script).is_file()
    assert v2_git(repository, "ls-files", "--", script).stdout == ""
    for scenario_id in ("AC-7.3-01", "AC-7.3-02"):
        assert (repository / STORY_7_3_RESULTS / f"{scenario_id}.json").is_file()
    before = v2_snapshot(repository)

    failure = v2_assert_failure(
        v2_run(v2_7_3_arguments(repository)), {"SCENARIO_COMMAND_UNSUPPORTED"}
    )
    assert failure["blockers"] == ["SCENARIO_COMMAND_UNSUPPORTED"]
    assert {
        item["subject"]
        for item in failure["diagnostics"]
        if "must name a committed script" in item["message"]
    } == {"AC-7.3-01", "AC-7.3-02"}
    assert v2_7_3_outputs(repository) == (None, None)
    assert v2_snapshot(repository) == before


def test_v2_story_7_3_git_failure_reading_an_acceptance_input_is_blocked(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
) -> None:
    fixture = build_v2_7_3_repository(tmp_path)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    module = load_generator()
    original = module.run_git
    body = STORY_7_3_BODIES[0]

    def failing(repository_path: Path, *arguments: str, **options):
        if arguments[:2] == ("ls-tree", "-z") and body in arguments:
            raise module.GateError("GIT_COMMAND_FAILED", "forced fixture failure")
        return original(repository_path, *arguments, **options)

    monkeypatch.setattr(module, "run_git", failing)
    assert module.main(v2_7_3_arguments(repository)) == 2
    captured = capsys.readouterr()
    document = json.loads(captured.out)
    v2_failure_validator().validate(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"] == ["GIT_COMMAND_FAILED"]
    assert "forced fixture failure" not in json.dumps(document)
    assert v2_7_3_outputs(repository) == (None, None)
    assert v2_snapshot(repository) == before


@pytest.mark.parametrize(
    "fault",
    ("tampered-7.2-json", "missing-7.1-markdown", "mismatched-7.2-predecessor"),
)
def test_v2_story_7_3_invalid_predecessor_records_block_the_record(tmp_path: Path, fault: str) -> None:
    def mutate(repository: Path) -> None:
        if fault in ("tampered-7.2-json", "mismatched-7.2-predecessor"):
            target = repository / "docs/release-evidence/story-7.2-final-record-v2.json"
            record = json.loads(target.read_bytes())
            if fault == "tampered-7.2-json":
                record["rollback"]["boundary"] += " tampered"
                target.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
            else:
                record["measurements"]["predecessorRecord"]["sha256"] = "0" * 64
                _, json_bytes, markdown_bytes = load_generator().v2_finalize(record)
                target.write_bytes(json_bytes)
                (
                    repository / "docs/release-evidence/story-7.2-final-record-v2.md"
                ).write_bytes(markdown_bytes)
        else:
            (repository / "docs/release-evidence/story-7.1-final-record-v2.md").unlink()

    fixture = build_v2_7_3_repository(tmp_path, mutate=mutate)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    failure = v2_assert_failure(v2_run(v2_7_3_arguments(repository)), {"AUTHORITY_BINDING_INVALID"})
    assert failure["blockers"] == ["AUTHORITY_BINDING_INVALID"]
    assert v2_7_3_outputs(repository) == (None, None)
    assert v2_snapshot(repository) == before


def test_v2_story_7_3_verification_mode_accepts_only_its_own_options(tmp_path: Path) -> None:
    fixture = build_v2_7_3_repository(tmp_path, results=False)
    repository = fixture["repository"]
    before = v2_snapshot(repository)
    for arguments in (
        [*v2_7_3_verify_arguments(repository), "--output-json", STORY_7_3_OUTPUT_JSON],
        [*v2_7_3_verify_arguments(repository), "--format", "bundle"],
        [*v2_7_3_verify_arguments(repository), "--verify-inserted-record", STORY_7_3_SPEC_PATH],
        v2_7_3_verify_arguments(repository, "../outside.md"),
        v2_7_3_verify_arguments(repository, "missing-spec.md"),
        v2_7_3_verify_arguments(repository, STORY_7_3_BODIES[0]),
    ):
        document = v2_assert_failure(v2_run(arguments), {"ARGUMENT_INVALID"})
        assert document["blockers"] == ["ARGUMENT_INVALID"]
    unknown = v2_assert_failure(
        v2_run(["--unknown", "value", *v2_7_3_verify_arguments(repository)]),
        {"ARGUMENT_INVALID"},
    )
    diagnostic = unknown["diagnostics"][0]["message"]
    assert "--verify-inserted-record" in diagnostic
    assert "--output-json" not in diagnostic
    assert v2_snapshot(repository) == before


def test_v2_story_7_3_schema_requires_workflow_integration_only_for_story_7_3() -> None:
    validator = v2_schema_contract_validator(v2_schema_contract_load(FINAL_RECORD_SCHEMA))
    story_7_1 = json.loads((WORKSPACE / "docs/release-evidence/story-7.1-final-record-v2.json").read_bytes())
    story_7_2 = json.loads((WORKSPACE / "docs/release-evidence/story-7.2-final-record-v2.json").read_bytes())
    integration = {
        "contract": {"path": STORY_7_3_CONTRACT_PATH, "sha256": "a" * 64},
        "workflowBodies": [{"path": path, "sha256": "b" * 64} for path in STORY_7_3_BODIES],
        "predecessorRecords": [
            {"storyId": "7.1", "path": "docs/release-evidence/story-7.1-final-record-v2.json", "sha256": "c" * 64},
            {"storyId": "7.2", "path": "docs/release-evidence/story-7.2-final-record-v2.json", "sha256": "d" * 64},
        ],
    }
    assert validator.is_valid(story_7_1) and validator.is_valid(story_7_2)
    assert not validator.is_valid(dict(story_7_1, workflowIntegration=integration))
    assert not validator.is_valid(dict(story_7_2, workflowIntegration=integration))
    story_7_3 = dict(story_7_1, storyId="7.3", workflowIntegration=integration)
    assert validator.is_valid(story_7_3)
    assert not validator.is_valid({key: value for key, value in story_7_3.items() if key != "workflowIntegration"})
    assert not validator.is_valid(
        dict(story_7_3, workflowIntegration=dict(integration, workflowBodies=integration["workflowBodies"][1:]))
    )
    assert not validator.is_valid(dict(story_7_3, workflowIntegration=dict(integration, extra=True)))


def test_v2_story_7_3_changes_leave_story_7_1_and_7_2_pairs_byte_identical() -> None:
    """The committed predecessor pairs still re-render and re-verify byte for byte."""
    module = load_generator()
    for story in ("7.1", "7.2"):
        json_bytes = (WORKSPACE / f"docs/release-evidence/story-{story}-final-record-v2.json").read_bytes()
        markdown_bytes = (WORKSPACE / f"docs/release-evidence/story-{story}-final-record-v2.md").read_bytes()
        assert module.v2_verify_pair(json_bytes, markdown_bytes) == []
        record = json.loads(json_bytes)
        assert record["scenarios"][-1]["assertionLedger"] == module.v2_self_ledger(
            record["scenarios"][-1]["scenarioId"], story
        )


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
