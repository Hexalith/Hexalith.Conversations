"""Retained Story 9.2 rederivation must read evidence without executing commands."""

import ast
import inspect
import json
import os
import shlex
import subprocess
from copy import deepcopy
from pathlib import Path
from types import SimpleNamespace

import pytest

from test_generate_story_record import WORKSPACE, _story_9_2_module, _story_9_2_record, write_trx


def _retained_generator_fixture(root: Path, monkeypatch):
    """Supply synthetic authority facts while retaining real command routing and output readers."""
    generator, verifier = _story_9_2_module()
    fixture = _story_9_2_record(generator, verifier)
    candidate = fixture["candidate"]["commit"]
    blobs = {}
    for path in (generator.V2_9_2_CONTRACT_PATH, generator.V2_9_2_AMENDMENT_PATH,
                 "_bmad/scripts/tests/test_conformance_tiering.py"):
        blobs[path] = (WORKSPACE / path).read_bytes()
    amendment = json.loads(blobs[generator.V2_9_2_AMENDMENT_PATH])
    outputs = {}
    assemblies = {}
    for scenario in amendment["effectiveScenarios"][:-1]:
        command = generator.v2_9_2_command(shlex.split(scenario["command"]))
        if command is None:  # The pytest result is an unrelated fault-ledger adapter below.
            continue
        if command["kind"] == "build":
            project = command["project"]
            blobs[project] = b"<Project />\n"
            name = Path(project).stem
            assembly = (Path(project).parent / "bin/Release/net10.0" / f"{name}.dll").as_posix()
            assemblies[project] = assembly
            outputs[assembly] = b"synthetic parser fixture 1.0.0+" + candidate.encode()
        elif command["kind"] == "python":
            blobs[command["script"]] = b"# Synthetic retained verifier; never execute.\n"
            for path in command["outputs"]:
                outputs[path] = b'{"result":"PASS","exitCode":0,"blockers":[]}\n'
    for path, content in {**blobs, **outputs}.items():
        target = root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)
        os.utime(target, ns=(10_000_000_000, 10_000_000_000))
    for scenario in amendment["effectiveScenarios"][:-1]:
        command = generator.v2_9_2_command(shlex.split(scenario["command"]))
        if command is None or command["kind"] != "xunit":
            continue
        target = root / command["output"]
        write_trx(target, passed=1, code_base=root / command["assembly"])
        target.write_text(target.read_text().replace("Fixture.P0", command["selector"] or "Fixture.Sample"))
        os.utime(target, ns=(20_000_000_000, 20_000_000_000))

    class EvaluationError(Exception):
        pass

    def evaluate(_root, project, configuration, *, resolved=False):
        assert configuration == "Release" and resolved is False
        return {"Properties": {"TargetPath": str(root / assemblies[project])}}

    evaluator = SimpleNamespace(VerificationError=EvaluationError, msbuild=evaluate)
    monkeypatch.setattr(generator, "v2_9_2_verifier", lambda: evaluator)
    monkeypatch.setattr(generator, "validate_repository", lambda *_: root)
    monkeypatch.setattr(generator, "resolve_commit", lambda *_: candidate)
    monkeypatch.setattr(generator, "v2_retention_config", lambda *_: ("9.2", generator.V2_9_2_SPEC_PATH,
                                                                    "9-2-fixture", True))
    monkeypatch.setattr(generator, "v2_retained_candidate", lambda *_: candidate)
    monkeypatch.setattr(generator, "v2_committed_blob", lambda _repo, _rev, path: blobs.get(path))
    monkeypatch.setattr(generator, "v2_authority", lambda *_: fixture["authority"])
    monkeypatch.setattr(generator, "v2_gitlinks", lambda *_: fixture["candidate"]["gitlinks"])
    monkeypatch.setattr(generator, "worktree_path_status", lambda *_: {})
    monkeypatch.setattr(generator, "run_git", lambda *_: subprocess.CompletedProcess([], 0, b"0", b""))

    def fault_result(_repo, scenario, *_args):
        row = next(deepcopy(row) for row in fixture["scenarios"] if row["scenarioId"] == scenario["id"])
        row["resultFile"] = {"path": "artifacts/synthetic-fault-result.xml", "sha256": "c" * 64}
        return row, "passed", []

    monkeypatch.setattr(generator, "v2_scenario_from_results", fault_result)
    monkeypatch.setattr(generator, "v2_9_2_facts", lambda *_args, **_kwargs: deepcopy(fixture["conformanceExecution"]))
    monkeypatch.setattr(generator, "v2_9_2_faults", lambda *_: deepcopy(fixture["faultInjection"]["results"]))
    launches = []

    def reject_launch(command, **_kwargs):
        launches.append(command)
        raise AssertionError("retained verification attempted a scenario command launch")

    monkeypatch.setattr(generator.subprocess, "run", reject_launch)
    options = {"--repository": str(root), "--contract": generator.V2_9_2_CONTRACT_PATH, "--format": "bundle",
               "--output-json": fixture["outputs"]["json"]["path"],
               "--output-markdown": fixture["outputs"]["markdown"]["path"]}
    return generator, options, launches


def _snapshots(root: Path):
    """Capture bytes and modification times, including both build outputs and all native result files."""
    return {path.relative_to(root).as_posix(): (path.read_bytes(), path.stat().st_mtime_ns)
            for path in root.rglob("*") if path.is_file()}


def test_retained_story92_generator_reads_real_build_python_and_trx_outputs_without_execution(
    tmp_path: Path, monkeypatch,
) -> None:
    generator, options, launches = _retained_generator_fixture(tmp_path, monkeypatch)
    before = _snapshots(tmp_path)
    result = json.loads(generator.v2_generate(options, verify_spec=generator.V2_9_2_SPEC_PATH))
    assert result["summary"] == {"required": 10, "passed": 10, "failed": 0, "blocked": 0,
                                 "skipped": 0, "notRun": 0}
    rows = {row["scenarioId"]: row for row in result["scenarios"]}
    assert rows["AC-9.2-01"]["assertionLedger"][0]["subject"].startswith("build::")
    assert rows["AC-9.2-03"]["assertionLedger"][0]["subject"].startswith("build::")
    assert rows["AC-9.2-08"]["assertionLedger"][0]["subject"].startswith("python-output::")
    for number in (1, 2, 3, 4, 5, 6, 7, 8):
        assert rows[f"AC-9.2-{number:02d}"]["outputFiles"]
    assert not launches
    assert _snapshots(tmp_path) == before


def test_retained_story92_boundary_regression_detects_removed_suppression(tmp_path: Path, monkeypatch) -> None:
    generator, options, launches = _retained_generator_fixture(tmp_path, monkeypatch)
    before = _snapshots(tmp_path)
    tree = ast.parse(inspect.getsource(generator.v2_generate))
    calls = [node for node in ast.walk(tree) if isinstance(node, ast.Call)
             and isinstance(node.func, ast.Name) and node.func.id == "v2_successor_scenario_from_results"]
    assert len(calls) == 1
    forwarded = [keyword for keyword in calls[0].keywords if keyword.arg is None]
    assert len(forwarded) == 1
    calls[0].keywords.remove(forwarded[0])
    namespace = dict(generator.__dict__)
    exec(compile(ast.fix_missing_locations(tree), "<missing-retained-suppression-mutant>", "exec"), namespace)
    with pytest.raises(AssertionError, match="attempted a scenario command launch"):
        namespace["v2_generate"](options, verify_spec=generator.V2_9_2_SPEC_PATH)
    assert launches == [shlex.split(json.loads((tmp_path / generator.V2_9_2_AMENDMENT_PATH).read_bytes())[
        "effectiveScenarios"][0]["command"])]
    assert _snapshots(tmp_path) == before
