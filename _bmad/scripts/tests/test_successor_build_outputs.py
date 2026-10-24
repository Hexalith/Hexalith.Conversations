"""Successor build facts use evaluated, contained output paths."""

import importlib.util
import os
from pathlib import Path
from types import SimpleNamespace

import pytest


SCRIPT = Path(__file__).resolve().parents[1] / "generate_story_record.py"


def load_generator():
    specification = importlib.util.spec_from_file_location("successor_build_record", SCRIPT)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


class EvaluationError(Exception):
    """Disposable equivalent of the shared evaluator's bounded failure."""


@pytest.fixture
def build_fixture(tmp_path, monkeypatch):
    module = load_generator()
    candidate = "a" * 40
    project = "fixture/App/App.csproj"
    solution = "fixture/Fixture.slnx"
    output = "custom-output/release/net9.0/Renamed.dll"
    blobs = {project: b"<Project />", solution: b'<Solution><Project Path="App/App.csproj" /></Solution>'}
    for relative, content in blobs.items():
        path = tmp_path / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)
    binary = tmp_path / output
    binary.parent.mkdir(parents=True)
    binary.write_bytes(b"1.0.0+" + candidate.encode())
    calls = []
    metadata = {"Properties": {"AssemblyName": "Renamed", "TargetPath": str(binary)}}

    def evaluate(repository, relative, configuration, *, resolved=False):
        calls.append((repository, relative, configuration, resolved))
        return metadata

    evaluator = SimpleNamespace(msbuild=evaluate, VerificationError=EvaluationError)
    monkeypatch.setattr(module, "v2_9_2_verifier", lambda: evaluator)
    monkeypatch.setattr(module, "v2_committed_blob", lambda _repository, _candidate, relative: blobs.get(relative))

    def run(kind="build", target=solution, gitlinks=()):
        scenario = {"id": "AC-11.2-03", "command": f"dotnet {kind} {target} -c Release"}
        return module.v2_successor_scenario_from_results(
            tmp_path, scenario, {"kind": kind, "project": target}, candidate, 0, gitlinks,
            execute_commands=False)

    return SimpleNamespace(module=module, root=tmp_path, candidate=candidate, project=project,
                           solution=solution, output=output, blobs=blobs, metadata=metadata,
                           evaluator=evaluator, calls=calls, run=run)


@pytest.mark.parametrize("target", ["project", "solution"])
def test_build_uses_evaluated_assembly_framework_and_output_directory(build_fixture, target):
    fixture = build_fixture
    record, category, findings = fixture.run(target=getattr(fixture, target))
    assert category == "passed" and not findings
    assert record["outputFiles"] == [{"path": fixture.output,
                                      "sha256": fixture.module.v2_sha256((fixture.root / fixture.output).read_bytes())}]
    assert fixture.calls == [(fixture.root, fixture.project, "Release", False)]


@pytest.mark.parametrize("kind", ["build", "restore"])
def test_solution_windows_separators_are_normalized(build_fixture, kind):
    fixture = build_fixture
    fixture.blobs[fixture.solution] = b'<Solution><Project Path="App\\App.csproj" /></Solution>'
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir()
    assets.write_bytes(b'{"version":3}')
    record, category, findings = fixture.run(kind=kind)
    assert category == "passed" and not findings
    assert record["assertionLedger"][0]["subject"] == f"{kind}::{fixture.project}"


@pytest.mark.parametrize("metadata", [None, {}, {"Properties": []},
    {"Properties": {}}, {"Properties": {"TargetPath": ""}},
    {"Properties": {"TargetPath": []}}, {"Properties": {"TargetPath": "one.dll;two.dll"}}])
def test_build_rejects_missing_or_ambiguous_output_metadata(build_fixture, metadata):
    fixture = build_fixture
    fixture.evaluator.msbuild = lambda *_args, **_kwargs: metadata
    record, category, _ = fixture.run()
    assert category == "failed" and "TEST_RESULTS_MISSING" in record["blockers"]


@pytest.mark.parametrize("fault", ["foreign-stamp", "outside", "gitlink", "symlink", "unavailable"])
def test_build_rejects_untrusted_evaluated_output(build_fixture, fault, tmp_path):
    fixture = build_fixture
    binary = fixture.root / fixture.output
    gitlinks = []
    if fault == "foreign-stamp":
        binary.write_bytes(b"1.0.0+" + b"b" * 40)
    elif fault == "outside":
        outside = tmp_path.parent / f"{tmp_path.name}-outside.dll"
        outside.write_bytes(binary.read_bytes())
        fixture.metadata["Properties"]["TargetPath"] = str(outside)
    elif fault == "gitlink":
        gitlinks = ["custom-output"]
    elif fault == "symlink":
        alias = fixture.root / "alias.dll"
        alias.symlink_to(binary)
        fixture.metadata["Properties"]["TargetPath"] = str(alias)
    else:
        def unavailable(*_args, **_kwargs):
            raise EvaluationError("SDK unavailable")
        fixture.evaluator.msbuild = unavailable
    record, category, _ = fixture.run(gitlinks=gitlinks)
    assert category == "failed"
    expected = {"foreign-stamp": "TEST_RESULTS_STALE", "outside": "OUTPUT_PATH_INVALID",
                "gitlink": "OUTPUT_PATH_INVALID", "symlink": "TEST_RESULTS_MISSING",
                "unavailable": "TEST_RESULTS_FAILED"}[fault]
    assert expected in record["blockers"]


@pytest.mark.parametrize("kind", ["build", "restore"])
@pytest.mark.parametrize("project_path", ["../../../escape.csproj", "/outside.csproj", "App/../../references/Blocked/App.csproj"])
def test_solution_rejects_uncontained_or_gitlink_projects_before_evaluation(build_fixture, kind, project_path):
    fixture = build_fixture
    fixture.blobs[fixture.solution] = f'<Solution><Project Path="{project_path}" /></Solution>'.encode()
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    record, category, _ = fixture.run(kind=kind, gitlinks=["references/Blocked"])
    assert category == "failed" and "OUTPUT_PATH_INVALID" in record["blockers"]
    assert fixture.calls == []


def test_build_metadata_evaluation_failure_does_not_use_guessed_binary(build_fixture):
    fixture = build_fixture
    guessed = fixture.root / "fixture/App/bin/Release/net10.0/App.dll"
    guessed.parent.mkdir(parents=True)
    guessed.write_bytes(b"1.0.0+" + fixture.candidate.encode())
    fixture.evaluator.msbuild = lambda *_args, **_kwargs: {"Properties": {}}
    record, category, _ = fixture.run()
    assert category == "failed" and "TEST_RESULTS_MISSING" in record["blockers"]
    assert record.get("outputFiles", []) == []


@pytest.mark.parametrize("kind", ["build", "restore"])
def test_solution_cannot_alias_a_path_out_of_a_root_gitlink(build_fixture, kind):
    fixture = build_fixture
    blocked = fixture.root / "references/Blocked"
    blocked.mkdir(parents=True)
    (blocked / "alias").symlink_to(fixture.root / "fixture/App", target_is_directory=True)
    fixture.blobs[fixture.solution] = b'<Solution><Project Path="../references/Blocked/alias/App.csproj" /></Solution>'
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir()
    assets.write_bytes(b'{"version":3}')
    record, category, _ = fixture.run(kind=kind, gitlinks=["references/Blocked"])
    assert category == "failed" and "OUTPUT_PATH_INVALID" in record["blockers"]
    assert fixture.calls == []


def test_build_cannot_alias_an_output_into_a_root_gitlink(build_fixture):
    fixture = build_fixture
    blocked = fixture.root / "references/Blocked"
    blocked.mkdir(parents=True)
    (blocked / "Output.dll").write_bytes(b"1.0.0+" + fixture.candidate.encode())
    alias = fixture.root / "alias"
    alias.symlink_to(blocked, target_is_directory=True)
    fixture.metadata["Properties"]["TargetPath"] = str(alias / "Output.dll")
    record, category, _ = fixture.run(gitlinks=["references/Blocked"])
    assert category == "failed" and "OUTPUT_PATH_INVALID" in record["blockers"]


@pytest.mark.parametrize("relative", ["bin/Renamed.dll", "../shared/Renamed.dll", "..\\shared\\Renamed.dll"])
def test_relative_target_path_is_relative_to_owning_project(build_fixture, relative):
    fixture = build_fixture
    binary = Path(os.path.abspath(fixture.root / "fixture/App" / relative.replace("\\", "/")))
    binary.parent.mkdir(parents=True)
    binary.write_bytes(b"1.0.0+" + fixture.candidate.encode())
    fixture.metadata["Properties"]["TargetPath"] = relative
    record, category, findings = fixture.run()
    assert category == "passed" and not findings
    assert record["outputFiles"][0]["path"] == binary.relative_to(fixture.root).as_posix()


@pytest.mark.parametrize("collides", [True, False])
def test_solution_projects_need_distinct_evaluated_outputs(build_fixture, collides):
    fixture = build_fixture
    other_project = "fixture/Other/Other.csproj"
    fixture.blobs[other_project] = b"<Project />"
    (fixture.root / other_project).parent.mkdir()
    (fixture.root / other_project).write_bytes(fixture.blobs[other_project])
    fixture.blobs[fixture.solution] = b'<Solution><Project Path="App/App.csproj" /><Project Path="Other/Other.csproj" /></Solution>'
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    other_binary = fixture.root / "custom-output/Other.dll"
    other_binary.write_bytes(b"1.0.0+" + fixture.candidate.encode())

    def evaluate(_repository, project, _configuration, *, resolved=False):
        assert resolved is False
        path = fixture.root / fixture.output if collides or project == fixture.project else other_binary
        return {"Properties": {"TargetPath": str(path)}}

    fixture.evaluator.msbuild = evaluate
    record, category, _ = fixture.run()
    assert (category == "passed") != collides
    if collides:
        assert "OUTPUT_PATH_INVALID" in record["blockers"]
    else:
        assert {row["path"] for row in record["outputFiles"]} == {fixture.output, "custom-output/Other.dll"}


@pytest.mark.parametrize("kind", ["build", "restore"])
def test_solution_symlink_loop_is_a_path_blocker(build_fixture, kind):
    fixture = build_fixture
    (fixture.root / "fixture/loop").symlink_to("loop", target_is_directory=True)
    fixture.blobs[fixture.solution] = b'<Solution><Project Path="loop/App.csproj" /></Solution>'
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    record, category, _ = fixture.run(kind=kind)
    assert category == "failed" and "OUTPUT_PATH_INVALID" in record["blockers"]
    assert fixture.calls == []
