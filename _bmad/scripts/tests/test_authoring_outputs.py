"""A/B output aliases preserve source and historical bytes through the shared writer."""

from __future__ import annotations

import os
import json
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

import test_minimal_module_fixture as fixture_tests
import test_thin_authoring_guidance as guidance_tests

import authoring_outputs as writer
import generate_minimal_module_fixture_proof as fixture_generator
import generate_story_record as records
import generate_thin_authoring_guidance as guidance_generator
import verify_minimal_module_fixture as fixture_verifier
import verify_thin_authoring_guidance as guidance_verifier

guidance_repo = guidance_tests.repo
fixture_repo = fixture_tests.repo
HISTORY = "docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json"
CASES = ["guidance-json", "guidance-markdown", "fixture-json", "fixture-markdown", "guidance-receipt", "fixture-receipt"]


@pytest.fixture(params=CASES)
def output_case(request):
    name = request.param
    guidance = name.startswith("guidance")
    repo = request.getfixturevalue("guidance_repo" if guidance else "fixture_repo")
    outputs = ["out/generated.json", "out/generated.md"] if not name.endswith("receipt") else ["out/result.json"]
    args = ["--repository", str(repo)]
    if name == "guidance-receipt":
        module = guidance_verifier
        args += ["--evidence", guidance_tests.JSON_OUT, "--markdown", guidance_tests.MD_OUT, "--output", outputs[0]]
    elif name == "fixture-receipt":
        module = fixture_verifier
        args += ["--contract", fixture_tests.CONTRACT, "--fixture", fixture_tests.FIXTURE, "--inventory-only", "--output", outputs[0]]
    else:
        args += ["--output-json", outputs[0], "--output-markdown", outputs[1]]
        if guidance:
            module = guidance_generator
            args += ["--contract", guidance_tests.CONTRACT, "--schema", guidance_tests.SCHEMA,
                     "--approve-reviewer", guidance_tests.REVIEW["reviewer"],
                     "--decided-at-utc", guidance_tests.REVIEW["decidedAtUtc"]]
        else:
            module = fixture_generator
            args += ["--contract", fixture_tests.CONTRACT, "--fixture", fixture_tests.FIXTURE, "--test-result", fixture_tests.TRX]
    historical = repo / HISTORY
    historical.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(guidance_tests.ROOT / HISTORY, historical)
    source = repo / (guidance_tests.SOURCE if guidance else f"{fixture_tests.FIXTURE}/README.md")
    slot = 1 if name.endswith("markdown") else 0
    return repo, module, args, [repo / path for path in outputs], source, slot


def run_case(case):
    repo, module, args, *_ = case
    return subprocess.run([sys.executable, module.__file__, *args], cwd=repo,
                          capture_output=True, text=True, check=False)


@pytest.mark.parametrize("link_kind", ["symlink", "hardlink"])
@pytest.mark.parametrize("protected_kind", ["source", "historical"])
def test_all_ab_cli_writers_preserve_aliased_source_and_history(output_case, link_kind, protected_kind):
    repo, _, _, outputs, source, slot = output_case
    initial = run_case(output_case)
    assert initial.returncode == 0, initial.stderr
    expected = [path.read_bytes() for path in outputs]
    protected = source if protected_kind == "source" else repo / HISTORY
    original = protected.read_bytes()
    pristine = fixture_tests.snapshot(repo)
    target = outputs[slot]
    target.unlink()
    if link_kind == "symlink":
        target.symlink_to(protected)
    else:
        target.hardlink_to(protected)
    try:
        result = run_case(output_case)
        assert "Traceback" not in result.stderr
        assert protected.read_bytes() == original
        if link_kind == "symlink":
            assert result.returncode == 2 and "BLOCKED" in result.stderr
            assert target.is_symlink()
            assert all(path.read_bytes() == expected[index] for index, path in enumerate(outputs) if index != slot)
        else:
            assert result.returncode == 0, result.stderr
            assert [path.read_bytes() for path in outputs] == expected
            assert not target.samefile(protected)
    finally:
        target.unlink()
        target.write_bytes(expected[slot])
        assert protected.read_bytes() == original
        assert fixture_tests.snapshot(repo) == pristine, "FIXTURE_NOT_RESTORED"
        assert [path.read_bytes() for path in outputs] == expected
        assert run_case(output_case).returncode == 0


def test_all_ab_cli_writers_reject_direct_input_targets_before_writing(output_case):
    repo, _, args, outputs, source, slot = output_case
    original = source.read_bytes()
    option = "--output" if len(outputs) == 1 else "--output-markdown" if slot else "--output-json"
    args[args.index(option) + 1] = str(source)
    result = run_case(output_case)
    assert result.returncode == 2 and "BLOCKED" in result.stderr and "Traceback" not in result.stderr
    assert source.read_bytes() == original
    assert not any(path.exists() for path in outputs)


@pytest.mark.parametrize("unsafe", ["escape", "absolute-escape", "symlink-parent", "external-parent", "duplicate", "git-admin", "readonly", "readonly-loop"])
def test_preflight_rejects_entire_set_without_writing(tmp_path: Path, unsafe: str):
    repo = tmp_path / "repository"
    repo.mkdir()
    protected = repo / "source.txt"
    protected.write_bytes(b"source\n")
    outside = tmp_path / "outside"
    outside.mkdir()
    paths = ["out/first.json", "out/second.md"]
    readonly = ("source.txt",)
    if unsafe == "escape":
        paths[1] = "../outside/escape.md"
    elif unsafe == "absolute-escape":
        paths[1] = str(outside / "escape.md")
    elif unsafe in ("symlink-parent", "external-parent"):
        (repo / "alias").symlink_to(repo if unsafe == "symlink-parent" else outside, target_is_directory=True)
        paths[1] = "alias/second.md"
    elif unsafe == "duplicate":
        paths[1] = "out/sub/../first.json"
    elif unsafe == "git-admin":
        paths[1] = ".git/config"
    elif unsafe == "readonly-loop":
        (repo / "loop").symlink_to("loop")
        readonly = ("loop",)
        paths[1] = "loop"
    else:
        paths[1] = "source.txt"
    with pytest.raises(writer.OutputError):
        writer.write_outputs(repo, [(path, b"generated\n") for path in paths], readonly)
    assert protected.read_bytes() == b"source\n"
    assert not (repo / "out").exists() and not list(outside.iterdir())


def test_tracked_source_and_history_are_immutable_but_identical_outputs_are_retained(tmp_path, monkeypatch):
    fixture_tests.git(tmp_path, "init", "--quiet")
    for relative in ["source.txt", HISTORY]:
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(b"original\n")
        fixture_tests.git(tmp_path, "add", "--", relative)
        with pytest.raises(writer.OutputError, match="tracked source or publication"):
            writer.write_outputs(tmp_path, [("out/fresh.json", b"fresh\n"), (relative, b"changed\n")])
        assert target.read_bytes() == b"original\n" and not (tmp_path / "out").exists()
        inode = target.stat().st_ino
        monkeypatch.setattr(records.os, "replace", lambda *_: pytest.fail("identical tracked bytes must not be rewritten"))
        writer.write_outputs(tmp_path, [(relative, b"original\n")])
        assert target.stat().st_ino == inode


@pytest.mark.parametrize("link_kind", ["symlink", "hardlink"])
@pytest.mark.parametrize("phase", ["tmp", "restore"])
def test_shared_temporary_aliases_preserve_source_and_the_existing_alias(tmp_path, link_kind, phase):
    protected = tmp_path / "protected.txt"
    protected.write_bytes(b"protected\n")
    target = tmp_path / "result.json"
    temporary = target.with_name(f".{target.name}.{os.getpid()}.{phase}")
    if link_kind == "symlink":
        temporary.symlink_to(protected)
    else:
        temporary.hardlink_to(protected)
    if phase == "tmp":
        with pytest.raises(OSError):
            records.v2_write_outputs([(target, b"generated\n")])
        assert not target.exists()
    else:
        target.write_bytes(b"new\n")
        assert records.v2_restore_outputs([target], {target: b"old\n"}) is False
        assert target.read_bytes() == b"new\n"
    assert protected.read_bytes() == b"protected\n"
    assert temporary.samefile(protected)


def test_staging_failure_cleans_only_owned_temporaries(tmp_path, monkeypatch):
    first, second = tmp_path / "first.json", tmp_path / "second.md"
    protected = tmp_path / "protected.txt"
    protected.write_bytes(b"protected\n")
    existing = second.with_name(f".{second.name}.{os.getpid()}.tmp")
    existing.symlink_to(protected)

    def fail_sync(_):
        raise OSError("forced staging sync failure")

    monkeypatch.setattr(records.os, "fsync", fail_sync)
    with pytest.raises(writer.OutputError, match="forced staging sync failure"):
        writer.write_outputs(tmp_path, [(str(first), b"json\n"), (str(second), b"markdown\n")])
    assert not first.exists() and not second.exists()
    assert not first.with_name(f".{first.name}.{os.getpid()}.tmp").exists()
    assert existing.is_symlink() and protected.read_bytes() == b"protected\n"


@pytest.mark.parametrize("phase", ["tmp", "restore"])
@pytest.mark.parametrize("reverse", [False, True])
def test_output_names_cannot_collide_with_transaction_paths(tmp_path, phase, reverse):
    first = tmp_path / "first.json"
    collision = first.with_name(f".{first.name}.{os.getpid()}.{phase}")
    targets = [(first, b"first\n"), (collision, b"second\n")]
    if reverse:
        targets.reverse()
    with pytest.raises(writer.OutputError, match="transaction temporaries"):
        writer.write_outputs(tmp_path, [(str(path), data) for path, data in targets])
    with pytest.raises(ValueError, match="transaction temporaries"):
        records.v2_write_outputs(targets)
    assert not list(tmp_path.iterdir())


def test_separate_git_administration_is_protected(tmp_path):
    repo = tmp_path / "repository"
    repo.mkdir()
    state = repo / "git-state"
    fixture_tests.git(repo, "init", "--quiet", "--separate-git-dir", str(state))
    original = (state / "config").read_bytes()
    with pytest.raises(writer.OutputError, match="Git administration"):
        writer.write_outputs(repo, [("out/first.json", b"first\n"), ("git-state/config", b"clobbered\n")])
    assert (state / "config").read_bytes() == original and not (repo / "out").exists()


@pytest.mark.parametrize("name", ["project.assets.json", "packages.lock.json", ".nupkg.metadata"])
def test_restore_metadata_is_readonly_outside_the_fixture(tmp_path, name):
    target = tmp_path / "custom-packages" / name
    target.parent.mkdir()
    target.write_bytes(b"restore input\n")
    with pytest.raises(writer.OutputError, match="read-only input"):
        writer.write_outputs(tmp_path, [(str(target), b"generated\n")])
    assert target.read_bytes() == b"restore input\n"


@pytest.mark.parametrize("output_case", ["fixture-json", "fixture-markdown", "fixture-receipt"], indirect=True)
@pytest.mark.parametrize("kind", ["assembly", "assets", "lock", "metadata"])
def test_b_cli_writers_preserve_measured_generated_inputs(output_case, kind):
    repo, _, args, outputs, _, slot = output_case
    if kind == "assembly":
        bundle = json.loads((repo / fixture_tests.JSON_OUT).read_bytes())
        source = repo / bundle["build"]["assemblies"][0]["path"]
    elif kind in ("assets", "lock"):
        project = Path(fixture_verifier.EXPECTED_PROJECTS[0]).parent
        source = repo / fixture_tests.FIXTURE / project / ("obj/project.assets.json" if kind == "assets" else "packages.lock.json")
    else:
        source = next((repo / fixture_tests.FIXTURE / "obj/packages").rglob(".nupkg.metadata"))
    saved = source.read_bytes()
    option = "--output" if len(outputs) == 1 else "--output-markdown" if slot else "--output-json"
    args[args.index(option) + 1] = str(source)
    result = run_case(output_case)
    assert result.returncode == 2 and "BLOCKED" in result.stderr and "Traceback" not in result.stderr
    assert source.read_bytes() == saved and not any(path.exists() for path in outputs)


@pytest.mark.parametrize("output_case", ["guidance-json"], indirect=True)
def test_review_reuse_rejects_a_fifo_without_opening_it(output_case):
    repo, module, args, outputs, source, _ = output_case
    for option in ("--approve-reviewer", "--decided-at-utc"):
        index = args.index(option)
        del args[index:index + 2]
    outputs[0].parent.mkdir(parents=True)
    assert hasattr(os, "mkfifo"), "FIFO support is required by the POSIX tooling lane"
    os.mkfifo(outputs[0])
    saved = source.read_bytes()
    result = subprocess.run([sys.executable, module.__file__, *args], cwd=repo,
                            capture_output=True, text=True, check=False, timeout=10)
    assert result.returncode == 2 and "BLOCKED" in result.stderr and "Traceback" not in result.stderr
    assert source.read_bytes() == saved and not outputs[1].exists()


@pytest.mark.parametrize("output_case", ["fixture-receipt"], indirect=True)
@pytest.mark.parametrize("kind", ["test-result", "proof-json", "proof-markdown"])
def test_fixture_receipts_protect_exact_backslash_input_paths(output_case, kind):
    repo, _, args, _, _, _ = output_case
    if kind == "test-result":
        args.remove("--inventory-only")
        original = repo / fixture_tests.TRX
    else:
        original = repo / (fixture_tests.JSON_OUT if kind == "proof-json" else fixture_tests.MD_OUT)
        args += ["--proof-json", fixture_tests.JSON_OUT, "--proof-markdown", fixture_tests.MD_OUT]
    literal = r"custom\input.trx" if kind == "test-result" else r"custom\input.json" if kind == "proof-json" else r"custom\input.md"
    target = repo / literal
    target.parent.mkdir(parents=True, exist_ok=True)
    saved = original.read_bytes()
    target.write_bytes(saved)
    if kind == "test-result":
        args += ["--test-result", literal]
    else:
        args[args.index("--" + kind) + 1] = literal
    assert run_case(output_case).returncode == 0
    args[args.index("--output") + 1] = literal
    result = run_case(output_case)
    assert result.returncode == 2 and "BLOCKED" in result.stderr and "Traceback" not in result.stderr
    assert target.read_bytes() == saved


@pytest.mark.parametrize("seeded", [False, True])
@pytest.mark.parametrize("output_case", CASES[:4], indirect=True)
def test_ab_pair_failure_restores_every_output_and_reports_blocked(output_case, monkeypatch, capsys, seeded):
    repo, module, args, outputs, source, _ = output_case
    for path in outputs:
        path.parent.mkdir(parents=True, exist_ok=True)
        if seeded:
            path.write_bytes(b"previous\n")
    source_bytes = source.read_bytes()
    original_replace = records.os.replace

    def fail_second(staged, target):
        if str(staged).endswith(".tmp") and target == outputs[1]:
            raise OSError("forced second replacement failure")
        original_replace(staged, target)

    monkeypatch.setattr(records.os, "replace", fail_second)
    assert module.main(args) == 2
    captured = capsys.readouterr()
    assert "BLOCKED" in captured.err and "every replaced output was restored" in captured.err
    assert "Traceback" not in captured.err and source.read_bytes() == source_bytes
    assert [(path.read_bytes() if path.exists() else None) for path in outputs] == [b"previous\n" if seeded else None] * 2
    assert not list((repo / "out").glob(".*.tmp")) and not list((repo / "out").glob(".*.restore"))
