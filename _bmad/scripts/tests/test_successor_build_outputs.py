"""Successor build facts use evaluated, contained output paths."""

import importlib.util
import base64
import json
import os
import shutil
import subprocess
from pathlib import Path
from types import SimpleNamespace

import pytest


SCRIPT = Path(__file__).resolve().parents[1] / "generate_story_record.py"


def load_generator():
    specification = importlib.util.spec_from_file_location("successor_build_record", SCRIPT)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


def load_evaluator():
    specification = importlib.util.spec_from_file_location(
        "native_successor_solution_evaluator", SCRIPT.with_name("verify_conformance_tiering.py"))
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


class EvaluationError(Exception):
    """Disposable equivalent of the shared evaluator's bounded failure."""


def restore_assets(root, project, *, version=4):
    """Minimal native package-free assets graph, including the restore owner."""
    return json.dumps({"version": version, "targets": {"net10.0": {}}, "libraries": {},
                       "projectFileDependencyGroups": {"net10.0": []},
                       "project": {"restore": {"projectPath": str(root / project),
                                                "projectUniqueName": str(root / project)},
                                   "frameworks": {"net10.0": {"framework": "net10.0"}}}}).encode()


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
    metadata = {"Properties": {"AssemblyName": "Renamed", "TargetPath": str(binary),
                               "ProjectAssetsFile": str(tmp_path / "fixture/App/obj/project.assets.json")}}

    def evaluate(repository, relative, configuration, *, resolved=False, solution=None, building_solution=False):
        calls.append((repository, relative, configuration, resolved, solution, building_solution))
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
    assert fixture.calls == [(fixture.root, fixture.project, "Release", False,
                              fixture.solution if target == "solution" else None, target == "solution")]


@pytest.mark.parametrize("kind", ["build", "restore"])
def test_solution_windows_separators_are_normalized(build_fixture, kind):
    fixture = build_fixture
    fixture.blobs[fixture.solution] = b'<Solution><Project Path="App\\App.csproj" /></Solution>'
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir()
    assets.write_bytes(restore_assets(fixture.root, fixture.project))
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
    assets.write_bytes(restore_assets(fixture.root, fixture.project))
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

    def evaluate(_repository, project, _configuration, *, resolved=False, solution=None, building_solution=False):
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


@pytest.mark.parametrize("kind", ["build", "restore"])
@pytest.mark.parametrize("directive", [
    '<BuildType Solution="Release|*" Project="Debug" />',
    '<Platform Solution="*|Any CPU" Project="x64" />',
    '<Build Solution="*|*" Project="false" />',
])
@pytest.mark.parametrize("scope", ["solution", "folder", "project"])
def test_solution_configuration_and_participation_directives_block_before_command(
    build_fixture, monkeypatch, kind, directive, scope,
):
    fixture = build_fixture
    project = '<Project Path="App/App.csproj">' + (directive if scope == "project" else "") + '</Project>'
    if scope == "folder":
        body = '<Folder Name="/Apps/">' + directive + project + '</Folder>'
    else:
        body = (directive if scope == "solution" else "") + project
    fixture.blobs[fixture.solution] = ('<Solution>' + body + '</Solution>').encode()
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    launches = []
    monkeypatch.setattr(fixture.module.subprocess, "run", lambda *_args, **_kwargs: launches.append(1))
    record, category, _ = fixture.module.v2_successor_scenario_from_results(
        fixture.root, {"id": "AC-11.2-03", "command": f"dotnet {kind} {fixture.solution} -c Release"},
        {"kind": kind, "project": fixture.solution}, fixture.candidate, 0, [])
    assert category == "failed" and "SCENARIO_COMMAND_UNSUPPORTED" in record["blockers"]
    assert launches == [] and fixture.calls == []


@pytest.mark.parametrize("kind", ["build", "restore"])
@pytest.mark.parametrize("shape", ["custom-type", "implicit-type", "explicit-type"])
def test_solution_project_type_overrides_cannot_hide_nonparticipation(build_fixture, monkeypatch, kind, shape):
    fixture = build_fixture
    definition = ('<Configurations><ProjectType Name="NoBuild" '
                  'TypeId="{ACB9A212-8763-492D-AD65-37522E65B102}" Extension=".csproj" '
                  'IsBuildable="false" /></Configurations>')
    entry = '<Project Path="App/App.csproj"' + (' Type="NoBuild"' if shape != "implicit-type" else '') + ' />'
    fixture.blobs[fixture.solution] = ('<Solution>' + (definition if shape != "explicit-type" else '') + entry + '</Solution>').encode()
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    launches = []
    monkeypatch.setattr(fixture.module.subprocess, "run", lambda *_args, **_kwargs: launches.append(1))
    record, category, _ = fixture.module.v2_successor_scenario_from_results(
        fixture.root, {"id": "AC-11.2-03", "command": f"dotnet {kind} {fixture.solution} -c Release"},
        {"kind": kind, "project": fixture.solution}, fixture.candidate, 0, [])
    assert category == "failed" and "SCENARIO_COMMAND_UNSUPPORTED" in record["blockers"]
    assert launches == [] and fixture.calls == []


@pytest.mark.parametrize("relative", ["obj/project.assets.json", "custom-obj/project.assets.json",
                                      "../shared/project.assets.json", "..\\shared\\project.assets.json"])
def test_restore_uses_evaluated_assets_relative_to_project(build_fixture, relative):
    fixture = build_fixture
    assets = Path(os.path.abspath(fixture.root / "fixture/App" / relative.replace("\\", "/")))
    assets.parent.mkdir(parents=True, exist_ok=True)
    assets.write_bytes(restore_assets(fixture.root, fixture.project))
    os.utime(assets, ns=(1, 1))
    fixture.metadata["Properties"]["ProjectAssetsFile"] = relative
    record, category, findings = fixture.run(kind="restore")
    assert category == "passed" and not findings
    assert record["outputFiles"] == [{"path": assets.relative_to(fixture.root).as_posix(),
                                      "sha256": fixture.module.v2_sha256(assets.read_bytes())}]
    assert fixture.calls == [(fixture.root, fixture.project, "Release", False, fixture.solution, False)]


@pytest.mark.parametrize("metadata", [None, {}, {"Properties": []}, {"Properties": {}},
    {"Properties": {"ProjectAssetsFile": ""}}, {"Properties": {"ProjectAssetsFile": []}},
    {"Properties": {"ProjectAssetsFile": "one.json;two.json"}}])
def test_restore_metadata_failure_never_falls_back_to_default_assets(build_fixture, metadata):
    fixture = build_fixture
    default = fixture.root / "fixture/App/obj/project.assets.json"
    default.parent.mkdir()
    default.write_bytes(restore_assets(fixture.root, fixture.project))
    fixture.evaluator.msbuild = lambda *_args, **_kwargs: metadata
    record, category, _ = fixture.run(kind="restore")
    assert category == "failed" and "TEST_RESULTS_MISSING" in record["blockers"]
    assert record.get("outputFiles", []) == []


@pytest.mark.parametrize("fault", ["missing", "outside", "gitlink", "symlink", "loop", "unavailable",
                                  "physical-gitlink", "lexical-gitlink"])
def test_restore_rejects_unsafe_or_unavailable_evaluated_assets(build_fixture, fault):
    fixture = build_fixture
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir()
    assets.write_bytes(restore_assets(fixture.root, fixture.project))
    gitlinks = []
    if fault == "missing":
        fixture.metadata["Properties"]["ProjectAssetsFile"] = str(assets.with_name("missing.json"))
    elif fault == "outside":
        fixture.metadata["Properties"]["ProjectAssetsFile"] = "../../../outside.json"
    elif fault == "gitlink":
        gitlinks = ["fixture/App/obj"]
    elif fault in ("symlink", "loop"):
        alias = fixture.root / "alias.json"
        alias.symlink_to(alias if fault == "loop" else assets)
        fixture.metadata["Properties"]["ProjectAssetsFile"] = str(alias)
    elif fault in ("physical-gitlink", "lexical-gitlink"):
        blocked = fixture.root / "references/Blocked"
        blocked.mkdir(parents=True)
        gitlinks = ["references/Blocked"]
        if fault == "physical-gitlink":
            (blocked / "project.assets.json").write_bytes(restore_assets(fixture.root, fixture.project))
            alias = fixture.root / "alias"
            alias.symlink_to(blocked, target_is_directory=True)
            path = alias / "project.assets.json"
        else:
            alias = blocked / "alias"
            alias.symlink_to(assets.parent, target_is_directory=True)
            path = alias / "project.assets.json"
        fixture.metadata["Properties"]["ProjectAssetsFile"] = str(path)
    else:
        def unavailable(*_args, **_kwargs):
            raise EvaluationError("SDK unavailable")
        fixture.evaluator.msbuild = unavailable
    record, category, _ = fixture.run(kind="restore", gitlinks=gitlinks)
    expected = {"missing": "TEST_RESULTS_MISSING", "symlink": "TEST_RESULTS_MISSING",
                "unavailable": "TEST_RESULTS_FAILED"}.get(fault, "OUTPUT_PATH_INVALID")
    if fault == "loop":
        # Python versions differ on non-strict resolve of a symlink loop;
        # neither an unresolved boundary nor an unreadable output can pass.
        assert category == "failed" and {"OUTPUT_PATH_INVALID", "TEST_RESULTS_MISSING"}.intersection(record["blockers"])
        return
    assert category == "failed" and expected in record["blockers"]


@pytest.mark.parametrize("collides", [True, False])
def test_restore_projects_need_distinct_evaluated_assets(build_fixture, collides):
    fixture = build_fixture
    other_project = "fixture/Other/Other.csproj"
    fixture.blobs[other_project] = b"<Project />"
    (fixture.root / other_project).parent.mkdir()
    (fixture.root / other_project).write_bytes(fixture.blobs[other_project])
    fixture.blobs[fixture.solution] = b'<Solution><Project Path="App/App.csproj" /><Project Path="Other/Other.csproj" /></Solution>'
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    paths = [fixture.root / "fixture" / name / "obj/project.assets.json" for name in ("App", "Other")]
    for path, project in zip(paths, (fixture.project, other_project)):
        path.parent.mkdir()
        path.write_bytes(restore_assets(fixture.root, project))
    fixture.evaluator.msbuild = lambda _repo, project, _configuration, **_kwargs: {
        "Properties": {"ProjectAssetsFile": str(paths[0 if collides or project == fixture.project else 1])}}
    record, category, _ = fixture.run(kind="restore")
    assert (category == "passed") != collides
    if collides:
        assert "OUTPUT_PATH_INVALID" in record["blockers"]
    else:
        assert {row["path"] for row in record["outputFiles"]} == {
            path.relative_to(fixture.root).as_posix() for path in paths}


@pytest.mark.parametrize("kind", ["build", "restore"])
def test_distinct_output_paths_cannot_alias_one_file_through_hardlinks(build_fixture, kind):
    fixture = build_fixture
    other_project = "fixture/Other/Other.csproj"
    fixture.blobs[other_project] = b"<Project />"
    (fixture.root / other_project).parent.mkdir()
    (fixture.root / other_project).write_bytes(fixture.blobs[other_project])
    fixture.blobs[fixture.solution] = b'<Solution><Project Path="App/App.csproj" /><Project Path="Other/Other.csproj" /></Solution>'
    (fixture.root / fixture.solution).write_bytes(fixture.blobs[fixture.solution])
    source = fixture.root / fixture.output
    alias = source.with_name("Other.dll" if kind == "build" else "other.assets.json")
    os.link(source, alias)
    property_name = "TargetPath" if kind == "build" else "ProjectAssetsFile"
    fixture.evaluator.msbuild = lambda _repo, project, _configuration, **_kwargs: {
        "Properties": {property_name: str(source if project == fixture.project else alias)}}
    record, category, _ = fixture.run(kind=kind)
    assert category == "failed" and "OUTPUT_PATH_INVALID" in record["blockers"]


@pytest.mark.parametrize("version", [3, 4])
def test_restore_accepts_native_package_free_assets_versions(build_fixture, version):
    fixture = build_fixture
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir()
    assets.write_bytes(restore_assets(fixture.root, fixture.project, version=version))
    record, category, findings = fixture.run(kind="restore")
    assert category == "passed" and not findings
    assert record["outputFiles"][0]["sha256"] == fixture.module.v2_sha256(assets.read_bytes())


@pytest.mark.parametrize("fault", ["garbage", "truncated", "array", "duplicate-version",
    "unsupported-version", "bool-version", "targets", "empty-targets", "target-values",
    "libraries", "library-values", "dependency-groups", "dependency-group-values", "dependency-values",
    "project", "owner", "frameworks", "empty-frameworks",
    "framework-values", "missing-project-path", "relative-project-path", "foreign-project-path",
    "missing-project-unique-name", "relative-project-unique-name", "foreign-project-unique-name"])
def test_restore_rejects_malformed_or_foreign_owned_assets(build_fixture, fault):
    fixture = build_fixture
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir()
    document = json.loads(restore_assets(fixture.root, fixture.project))
    if fault in ("garbage", "truncated", "array", "duplicate-version"):
        content = {"garbage": b"not NuGet assets", "truncated": b'{"version":',
                   "array": b"[]", "duplicate-version": b'{"version":3,"version":4}'}[fault]
    else:
        if fault == "unsupported-version":
            document["version"] = 99
        elif fault == "bool-version":
            document["version"] = True
        elif fault in ("targets", "libraries"):
            document[fault] = []
        elif fault == "dependency-groups":
            document["projectFileDependencyGroups"] = []
        elif fault in ("dependency-group-values", "dependency-values"):
            document["projectFileDependencyGroups"]["net10.0"] = (
                {} if fault == "dependency-group-values" else [True])
        elif fault == "library-values":
            document["libraries"]["Example/1.0.0"] = []
        elif fault == "empty-targets":
            document["targets"] = {}
        elif fault == "target-values":
            document["targets"]["net10.0"] = []
        elif fault == "project":
            document["project"] = []
        elif fault == "owner":
            document["project"]["restore"] = []
        elif fault in ("frameworks", "empty-frameworks", "framework-values"):
            document["project"]["frameworks"] = {
                "frameworks": [], "empty-frameworks": {}, "framework-values": {"net10.0": []}}[fault]
        else:
            prefix, _, name = fault.partition("-")
            key = "projectPath" if name == "project-path" else "projectUniqueName"
            if prefix == "missing":
                del document["project"]["restore"][key]
            else:
                document["project"]["restore"][key] = (fixture.project if prefix == "relative"
                    else str(fixture.root / "foreign/Other.csproj"))
        content = json.dumps(document).encode()
    assets.write_bytes(content)
    record, category, _ = fixture.run(kind="restore")
    assert category == "failed" and "TEST_RESULTS_FAILED" in record["blockers"]


@pytest.mark.parametrize("operation", ["project", "build", "restore"])
def test_shared_evaluator_preserves_solution_command_context(build_fixture, monkeypatch, operation):
    fixture = build_fixture
    verifier = load_evaluator()
    calls = []

    def evaluate(command, **_kwargs):
        calls.append(command)
        if any(argument.startswith("-target:_HexalithReadSolutionContext_") for argument in command):
            return SimpleNamespace(returncode=0, stdout="HEXALITH_SOLUTION_CONTEXT:" +
                base64.b64encode(b"<SolutionConfiguration />").decode() + "\n")
        return SimpleNamespace(returncode=0, stdout='{\n"Properties": {}}')

    monkeypatch.setattr(verifier.subprocess, "run", evaluate)
    verifier.msbuild(fixture.root, fixture.project, "Debug",
                     solution=fixture.solution if operation != "project" else None,
                     building_solution=operation == "build")
    context = {argument[3:].partition("=")[0]: argument[3:].partition("=")[2]
               for argument in calls[-1] if argument.startswith("-p:")}
    expected = {"Configuration": "Debug"}
    if operation != "project":
        path = fixture.root / fixture.solution
        expected.update(SolutionDir=str(path.parent) + os.sep, SolutionPath=str(path),
                        SolutionName=path.stem, SolutionFileName=path.name, SolutionExt=".slnx",
                        Platform="AnyCPU")
    if operation == "build":
        expected["BuildingSolutionFile"] = "true"
        expected["CurrentSolutionConfigurationContents"] = "<SolutionConfiguration />"
    assert context == expected


@pytest.mark.parametrize("variant", ["ordinary", "platform", "alias", "configuration-xml"])
def test_native_solution_context_selects_actual_build_and_restore_outputs(build_fixture, variant):
    fixture = build_fixture
    solution = fixture.solution
    if variant == "alias":
        (fixture.root / "alias").symlink_to("fixture", target_is_directory=True)
        solution = "alias/Fixture.slnx"
        fixture.blobs[solution] = fixture.blobs[fixture.solution]
    assert shutil.which("dotnet"), "Native solution-context coverage requires the .NET SDK"
    sdk = subprocess.run(["dotnet", "--version"], cwd=fixture.root, capture_output=True,
                         text=True, timeout=30, check=False)
    assert sdk.returncode == 0, sdk.stdout + sdk.stderr
    framework = "net" + sdk.stdout.strip().split(".")[0] + ".0"
    # Explicit imports let the context choose intermediate paths before SDK.props.
    project = (('''<Project><PropertyGroup>
<SolutionContext Condition="'$(SolutionName)' == 'Fixture' and '$(SolutionFileName)' == 'Fixture.slnx'
 and '$(SolutionExt)' == '.slnx'
 and '$(SolutionDir)' != '' and '$(SolutionPath)' == 'EXPECTED_SOLUTION_PATH'">true</SolutionContext>
<BaseIntermediateOutputPath Condition="'$(SolutionContext)' == 'true'">$(SolutionDir)solution-obj/</BaseIntermediateOutputPath>
</PropertyGroup><Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
<PropertyGroup><TargetFramework>net10.0</TargetFramework><NuGetAudit>false</NuGetAudit>
<DefaultItemExcludes>$(DefaultItemExcludes);obj/**;bin/**</DefaultItemExcludes>
<SourceRevisionId>''' + fixture.candidate + '''</SourceRevisionId>
<BaseOutputPath Condition="'$(BaseOutputPath)' == ''">bin/</BaseOutputPath>
<BaseOutputPath Condition="'$(SolutionContext)' == 'true' and '$(BuildingSolutionFile)' == 'true'">$(SolutionDir)solution-output/</BaseOutputPath>
PLATFORM_PROPERTIES
</PropertyGroup><Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
<Target Name="RecordRestoreContext" BeforeTargets="_GenerateRestoreGraphProjectEntry">
<WriteLinesToFile File="$(MSBuildProjectDirectory)/restore-context.txt" Lines="restore=$(BuildingSolutionFile)" Overwrite="true" />
</Target><Target Name="RecordBuildContext" BeforeTargets="PrepareForBuild">
<WriteLinesToFile File="$(MSBuildProjectDirectory)/build-context.txt" Lines="build=$(BuildingSolutionFile)" Overwrite="true" />
</Target></Project>''').replace("<TargetFramework>net10.0", "<TargetFramework>" + framework)
               .replace("EXPECTED_SOLUTION_PATH", str(fixture.root / solution))
               .replace("PLATFORM_PROPERTIES", {
                   "platform": "<Platforms>AnyCPU;x64</Platforms><Platform>x64</Platform>"
                               "<OutputPath>$(BaseOutputPath)$(Platform)/$(Configuration)/</OutputPath>",
                   "configuration-xml": "<OutputPath Condition=\"'$(CurrentSolutionConfigurationContents)' != ''\">"
                                        "$(BaseOutputPath)configuration-xml/$(Configuration)/</OutputPath>",
               }.get(variant, "")).encode())
    fixture.blobs[fixture.project] = project
    (fixture.root / fixture.project).write_bytes(project)
    (fixture.root / "NuGet.Config").write_text(
        '<configuration><packageSources><clear /></packageSources></configuration>')
    env = {**os.environ, "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1", "DOTNET_CLI_TELEMETRY_OPTOUT": "1"}

    def native(*arguments):
        result = subprocess.run(["dotnet", *arguments], cwd=fixture.root, env=env,
                                capture_output=True, text=True, timeout=90, check=False)
        assert result.returncode == 0, result.stdout + result.stderr

    # Retain real misleading standalone outputs before the actual solution run.
    native("restore", fixture.project)
    native("build", fixture.project, "-c", "Debug", "--no-restore")
    native("restore", solution)
    native("build", solution, "-c", "Debug", "--no-restore")
    assert (fixture.root / "fixture/App/restore-context.txt").read_text().strip() == "restore="
    assert (fixture.root / "fixture/App/build-context.txt").read_text().strip() == "build=true"
    evaluator = fixture.module.v2_9_2_verifier
    # The fixture replaces this loader; obtain the production evaluator directly.
    verifier = load_evaluator()
    fixture.module.v2_9_2_verifier = lambda: verifier

    def snapshot():
        return {path.relative_to(fixture.root).as_posix():
                (str(path.readlink()) if path.is_symlink() else path.read_bytes(),
                 path.stat(follow_symlinks=False).st_mtime_ns)
                for path in fixture.root.rglob("*") if path.is_file() or path.is_symlink()}

    before = snapshot()
    try:
        directory = "alias" if variant == "alias" else "fixture"
        platform = {"platform": "AnyCPU/", "configuration-xml": "configuration-xml/"}.get(variant, "")
        for kind, expected in (("build", f"{directory}/solution-output/{platform}Debug/{framework}/App.dll"),
                               ("restore", f"{directory}/solution-obj/project.assets.json")):
            scenario = {"id": "AC-11.2-03", "command": f"dotnet {kind} {solution}"}
            record, category, findings = fixture.module.v2_successor_scenario_from_results(
                fixture.root, scenario, {"kind": kind, "project": solution},
                fixture.candidate, 0, [], execute_commands=False)
            assert category == "passed" and not findings
            assert record["outputFiles"] == [{"path": expected,
                "sha256": fixture.module.v2_sha256((fixture.root / expected).read_bytes())}]
        assert (fixture.root / "fixture/App/obj/project.assets.json").is_file()
        standalone = "x64/" if variant == "platform" else ""
        assert (fixture.root / f"fixture/App/bin/{standalone}Debug/{framework}/App.dll").is_file()
        assert snapshot() == before
    finally:
        fixture.module.v2_9_2_verifier = evaluator


@pytest.mark.parametrize("directory", ["comma,context", "percent%2Ccontext", "semi;context"])
@pytest.mark.parametrize("building", [False, True])
def test_native_solution_location_globals_preserve_literal_path_characters(tmp_path, directory, building):
    """The argv API still needs MSBuild's own property-value escaping."""
    root = tmp_path / directory
    root.mkdir()
    (root / "App.csproj").write_text('''<Project><PropertyGroup>
<TargetPath>$(SolutionDir)expected.dll</TargetPath>
<ProjectAssetsFile>$(SolutionPath)</ProjectAssetsFile>
<AssemblyName>$(SolutionName)</AssemblyName>
</PropertyGroup></Project>''')
    (root / "Fixture.slnx").write_text('<Solution><Project Path="App.csproj" /></Solution>')
    verifier = load_evaluator()
    properties = verifier.msbuild(root, "App.csproj", "Debug", solution="Fixture.slnx",
                                  building_solution=building)["Properties"]
    assert properties["TargetPath"] == str(root / "expected.dll")
    assert properties["ProjectAssetsFile"] == str(root / "Fixture.slnx")
    assert properties["AssemblyName"] == "Fixture"


def test_native_failed_restore_assets_cannot_supply_passing_retained_receipt(build_fixture):
    fixture = build_fixture
    sdk = subprocess.run(["dotnet", "--version"], cwd=fixture.root, capture_output=True,
                         text=True, timeout=30, check=False)
    assert sdk.returncode == 0, sdk.stdout + sdk.stderr
    framework = "net" + sdk.stdout.strip().split(".")[0] + ".0"
    project = f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>{framework}</TargetFramework><NuGetAudit>false</NuGetAudit>
</PropertyGroup><ItemGroup><PackageReference Include="Hexalith.Disposable.Unavailable.Probe" Version="1.0.0" />
</ItemGroup></Project>'''.encode()
    fixture.blobs[fixture.project] = project
    (fixture.root / fixture.project).write_bytes(project)
    (fixture.root / "NuGet.Config").write_text(
        '<configuration><packageSources><clear /></packageSources></configuration>')
    restored = subprocess.run(["dotnet", "restore", fixture.solution], cwd=fixture.root,
                              capture_output=True, text=True, timeout=90, check=False)
    assert restored.returncode != 0 and "NU1100" in restored.stdout + restored.stderr
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    content = json.loads(assets.read_bytes())
    assert any(row["level"] == "Error" and row["code"] == "NU1100" for row in content["logs"])
    verifier = load_evaluator()
    fixture.module.v2_9_2_verifier = lambda: verifier
    record, category, _ = fixture.run(kind="restore")
    assert category == "failed" and "TEST_RESULTS_FAILED" in record["blockers"]


@pytest.mark.parametrize("fault", ["nonzero", "missing", "ambiguous", "malformed", "wrong-root", "oversize"])
def test_solution_configuration_query_fails_closed(build_fixture, monkeypatch, fault):
    fixture = build_fixture
    verifier = load_evaluator()
    marker = "HEXALITH_SOLUTION_CONTEXT:"
    valid = marker + base64.b64encode(b"<SolutionConfiguration />").decode() + "\n"
    outputs = {"nonzero": valid, "missing": "", "ambiguous": valid + valid,
               "malformed": marker + "a===\n", "oversize": "x" * (4 * 1024 * 1024 + 1),
               "wrong-root": marker + base64.b64encode(b"<Wrong />").decode() + "\n"}
    calls = []

    def query(command, **_kwargs):
        calls.append(command)
        return SimpleNamespace(returncode=1 if fault == "nonzero" else 0, stdout=outputs[fault])

    monkeypatch.setattr(verifier.subprocess, "run", query)
    with pytest.raises(verifier.VerificationError) as raised:
        verifier.msbuild(fixture.root, fixture.project, "Debug", solution=fixture.solution,
                         building_solution=True)
    assert raised.value.code == "RESOLVED_COMPILE_SURFACE_INVALID"
    assert len(calls) == 1 and any(argument.startswith("-target:_HexalithReadSolutionContext_")
                                 for argument in calls[0])


@pytest.mark.parametrize("customization", ["Directory.Solution.props", "Directory.Solution.targets",
                                         "before.Fixture.slnx.targets", "after.Fixture.slnx.targets"])
@pytest.mark.parametrize("building", [False, True])
def test_custom_solution_imports_are_explicitly_unsupported(build_fixture, monkeypatch, customization, building):
    fixture = build_fixture
    verifier = load_evaluator()
    (fixture.root / customization).write_text('<Project InitialTargets="Unexpected" />')
    launches = []
    monkeypatch.setattr(verifier.subprocess, "run", lambda *_args, **_kwargs: launches.append(1))
    with pytest.raises(verifier.VerificationError) as raised:
        verifier.msbuild(fixture.root, fixture.project, "Debug", solution=fixture.solution,
                         building_solution=building)
    assert raised.value.code == "SCENARIO_COMMAND_UNSUPPORTED" and launches == []


@pytest.mark.parametrize("logs,passes", [
    ([], True), ([{"level": "Warning", "code": "NU1901"}], True),
    ([{"level": "Error", "code": "NU1100"}], False),
    ([{"level": "Warning"}, {"level": "Error"}], False),
    (None, False), ({}, False), ([None], False), ([{}], False),
    ([{"level": "error"}], False), ([{"level": 0}], False),
])
def test_retained_restore_assets_cannot_hide_failed_restore_diagnostics(build_fixture, logs, passes):
    fixture = build_fixture
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir(parents=True)
    document = json.loads(restore_assets(fixture.root, fixture.project))
    document["logs"] = logs
    assets.write_text(json.dumps(document))
    record, category, _ = fixture.run(kind="restore")
    assert (category == "passed") is passes
    assert (record["result"] == "PASS") is passes
    if not passes:
        assert "TEST_RESULTS_FAILED" in record["blockers"]


@pytest.mark.parametrize("fault", ["target-entry-list", "target-entry-null", "target-entry-string",
                                   "target-framework", "dependency-framework", "empty-framework"])
def test_restore_assets_reject_malformed_target_entries_and_contradictory_frameworks(build_fixture, fault):
    fixture = build_fixture
    document = json.loads(restore_assets(fixture.root, fixture.project))
    if fault.startswith("target-entry"):
        document["targets"]["net10.0"]["Example/1.0.0"] = {
            "target-entry-list": [], "target-entry-null": None, "target-entry-string": "package",
        }[fault]
    elif fault == "target-framework":
        document["targets"] = {"net9.0": {}}
    elif fault == "dependency-framework":
        document["projectFileDependencyGroups"] = {"net9.0": []}
    else:
        document["targets"] = {"": {}}
        document["projectFileDependencyGroups"] = {"": []}
        document["project"]["frameworks"] = {"": {}}
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir(parents=True)
    assets.write_text(json.dumps(document))
    record, category, _ = fixture.run(kind="restore")
    assert category == "failed" and "TEST_RESULTS_FAILED" in record["blockers"]


def test_restore_assets_allow_runtime_specific_target_keys(build_fixture):
    fixture = build_fixture
    document = json.loads(restore_assets(fixture.root, fixture.project))
    document["targets"]["net10.0/linux-x64"] = {"Example/1.0.0": {"type": "package"}}
    assets = fixture.root / "fixture/App/obj/project.assets.json"
    assets.parent.mkdir(parents=True)
    assets.write_text(json.dumps(document))
    record, category, findings = fixture.run(kind="restore")
    assert category == "passed" and not findings and record["result"] == "PASS"
