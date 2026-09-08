"""Fault-injection tests for the lifecycle-gate upgrade-detection preflight."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/check_lifecycle_gate_preflight.py"
SPEC = importlib.util.spec_from_file_location("check_lifecycle_gate_preflight", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
preflight = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(preflight)

verifier = preflight.load_verifier()


def install_tree(destination: Path) -> None:
    """Copy the declared routes and context workflows, as an upgrade reinstalls them."""

    relatives = list(verifier.ACTIVE_ROUTE_PATHS) + [
        f"{tree}/{logical}"
        for logical in verifier.CONTEXT_WORKFLOW_PATHS
        for tree in (".agents/skills", ".claude/skills")
    ]
    for relative in relatives:
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative, target)


def codes(root: Path) -> list[str]:
    findings = (
        preflight.check_routes(root, verifier)
        + preflight.check_context_workflows(root, verifier)
        + preflight.check_orphan_gates(root, verifier)
    )
    for finding in findings:
        assert finding["detail"], finding
    return [finding["code"] for finding in findings]


def test_the_installed_repository_passes_and_names_every_declared_route() -> None:
    assert preflight.check_routes(ROOT, verifier) == []
    assert preflight.check_context_workflows(ROOT, verifier) == []
    assert preflight.check_orphan_gates(ROOT, verifier) == []
    assert preflight.inventory_error(verifier) is None
    assert len(verifier.ACTIVE_ROUTE_PATHS) == 10


def test_a_simulated_upgrade_that_strips_a_gate_fails_and_names_the_path(tmp_path: Path) -> None:
    """The exact `1c36c45` fault: a reinstall that drops the gate section."""

    install_tree(tmp_path)
    victim = verifier.ACTIVE_ROUTE_PATHS[0]
    target = tmp_path / victim
    text = target.read_text(encoding="utf-8")
    gate = text.index(verifier.GATE_MARKER)
    end = text.index("\n\n", text.index("\n\n", gate) + 2) + 2
    section_start = text.rindex("\n", 0, gate) + 1
    target.write_text(text[:section_start] + text[end:], encoding="utf-8")

    findings = preflight.check_routes(tmp_path, verifier)

    assert findings[0] == {
        "code": "LIFECYCLE_GATE_MISSING",
        "path": victim,
        "detail": f"expected exactly one {verifier.GATE_MARKER!r} section, found 0",
    }
    # Stripping one tree also breaks mirror parity, and both facts are reported by path.
    assert [finding["code"] for finding in findings[1:]] == ["LIFECYCLE_MIRROR_DRIFT"]
    assert findings[1]["path"] == victim.split("/skills/", 1)[1]
    assert preflight.main(["--repository", str(tmp_path)]) == 1


def test_an_upstream_route_deletion_is_reported_not_ignored(tmp_path: Path) -> None:
    install_tree(tmp_path)
    victim = verifier.ACTIVE_ROUTE_PATHS[1]
    (tmp_path / victim).unlink()

    findings = preflight.check_routes(tmp_path, verifier)

    assert [(finding["code"], finding["path"]) for finding in findings] == [
        ("LIFECYCLE_ROUTE_ABSENT", victim)
    ]


def test_a_displaced_gutted_or_drifted_gate_each_fail(tmp_path: Path) -> None:
    install_tree(tmp_path)
    logical = verifier.LOGICAL_ROUTE_PATHS[0]
    agents = tmp_path / ".agents/skills" / logical
    claude = tmp_path / ".claude/skills" / logical
    original = agents.read_text(encoding="utf-8")

    lifecycle = verifier.LIFECYCLE_TOKENS[logical]
    displaced = original.replace(verifier.GATE_MARKER, "Displaced heading", 1)
    displaced = displaced.replace(lifecycle, f"{lifecycle}\n\n### {verifier.GATE_MARKER}\n", 1)
    for path in (agents, claude):
        path.write_text(displaced, encoding="utf-8")
    assert "LIFECYCLE_GATE_DISPLACED" in codes(tmp_path)

    gutted = original.replace("verify_evidence_boundary.py", "the gate is advisory")
    for path in (agents, claude):
        path.write_text(gutted, encoding="utf-8")
    assert "LIFECYCLE_GATE_GUTTED" in codes(tmp_path)

    lost_token = original.replace(lifecycle, "the lifecycle write moved", 1)
    for path in (agents, claude):
        path.write_text(lost_token, encoding="utf-8")
    assert "LIFECYCLE_TOKEN_ABSENT" in codes(tmp_path)

    for path in (agents, claude):
        path.write_text(original, encoding="utf-8")
    agents.write_text(original + "\ndrift\n", encoding="utf-8")
    assert "LIFECYCLE_MIRROR_DRIFT" in codes(tmp_path)


def test_a_relocated_route_is_caught_from_the_undeclared_side(tmp_path: Path) -> None:
    """A rename would leave the declared set passing, so orphan gates are reported too."""

    install_tree(tmp_path)
    assert codes(tmp_path) == []
    source = tmp_path / verifier.ACTIVE_ROUTE_PATHS[0]
    shutil.copy2(source, source.with_name("step-04-review-renamed.md"))

    findings = preflight.check_orphan_gates(tmp_path, verifier)

    assert [finding["code"] for finding in findings] == ["LIFECYCLE_GATE_UNDECLARED_ROUTE"]
    assert findings[0]["path"].endswith("step-04-review-renamed.md")


def test_a_stripped_context_workflow_identity_is_caught(tmp_path: Path) -> None:
    """The upgrade wiped these tokens too; the detector must not stop at the routes."""

    install_tree(tmp_path)
    logical = verifier.CONTEXT_WORKFLOW_PATHS[0]
    for tree in (".agents/skills", ".claude/skills"):
        target = tmp_path / tree / logical
        target.write_text(
            target.read_text(encoding="utf-8").replace("overlay_version", ""), encoding="utf-8"
        )

    findings = preflight.check_context_workflows(tmp_path, verifier)

    assert {finding["code"] for finding in findings} == {"LIFECYCLE_CONTEXT_IDENTITY_STRIPPED"}
    assert {finding["path"] for finding in findings} == {
        f".agents/skills/{logical}",
        f".claude/skills/{logical}",
    }
    assert preflight.main(["--repository", str(tmp_path)]) == 1


def test_an_absent_context_workflow_is_reported(tmp_path: Path) -> None:
    install_tree(tmp_path)
    logical = verifier.CONTEXT_WORKFLOW_PATHS[1]
    (tmp_path / ".agents/skills" / logical).unlink()

    findings = preflight.check_context_workflows(tmp_path, verifier)

    assert findings[0]["code"] == "LIFECYCLE_CONTEXT_WORKFLOW_ABSENT"
    assert findings[0]["path"] == f".agents/skills/{logical}"


def test_an_unreadable_skill_file_is_a_finding_not_a_silent_skip(tmp_path: Path) -> None:
    """A file the scan cannot read could be hiding anything; fail closed on it."""

    install_tree(tmp_path)
    undecodable = tmp_path / ".claude/skills/vendored/step-broken.md"
    undecodable.parent.mkdir(parents=True, exist_ok=True)
    undecodable.write_bytes(b"\xff\xfe not utf-8 \x80\x81")

    findings = preflight.check_orphan_gates(tmp_path, verifier)

    assert [finding["code"] for finding in findings] == ["LIFECYCLE_TREE_UNREADABLE"]
    assert findings[0]["path"] == ".claude/skills/vendored/step-broken.md"


def test_disagreeing_frozen_structures_are_a_usage_error_not_a_finding(
    tmp_path: Path, monkeypatch
) -> None:
    """A traceback exits 1, which the hook and CI cannot tell apart from a real finding."""

    install_tree(tmp_path)

    class Drifted:
        GATE_MARKER = verifier.GATE_MARKER
        CONTEXT_WORKFLOW_PATHS = verifier.CONTEXT_WORKFLOW_PATHS
        ACTIVE_ROUTE_PATHS = verifier.ACTIVE_ROUTE_PATHS
        LIFECYCLE_TOKENS = {
            logical: token
            for logical, token in verifier.LIFECYCLE_TOKENS.items()
            if logical != verifier.LOGICAL_ROUTE_PATHS[0]
        }

    assert "no lifecycle token" in (preflight.inventory_error(Drifted) or "")
    assert preflight.inventory_error(type("Dup", (), {"ACTIVE_ROUTE_PATHS": ("a/skills/x", "a/skills/x")})) == (
        "frozen route inventory has duplicates"
    )
    assert preflight.inventory_error(type("Flat", (), {"ACTIVE_ROUTE_PATHS": ("no-tree.md",)})) == (
        "frozen route inventory has a path outside a skill tree"
    )
    monkeypatch.setattr(preflight, "load_verifier", lambda: Drifted)
    assert preflight.main(["--repository", str(tmp_path)]) == 2


def test_the_cli_emits_json_and_a_pass_exit_code(tmp_path: Path, capsys) -> None:
    install_tree(tmp_path)

    assert preflight.main(["--repository", str(tmp_path), "--json"]) == 0
    document = json.loads(capsys.readouterr().out)

    assert document["result"] == "PASS"
    assert document["routes"] == 10
    assert document["contextWorkflows"] == 4
    assert document["findings"] == []


def test_a_missing_repository_is_a_usage_error_not_a_pass(tmp_path: Path) -> None:
    assert preflight.main(["--repository", str(tmp_path / "absent")]) == 2


def test_the_script_runs_as_a_subprocess_against_the_repository() -> None:
    completed = subprocess.run(
        [sys.executable, str(MODULE_PATH), "--repository", str(ROOT), "--json"],
        check=False,
        capture_output=True,
        text=True,
    )

    assert completed.returncode == 0, completed.stderr
    assert json.loads(completed.stdout)["result"] == "PASS"


def test_the_preflight_check_is_wired_into_ci() -> None:
    workflow = (ROOT / ".github/workflows/planning-authority-preflight.yml").read_text(
        encoding="utf-8"
    )

    assert "check_lifecycle_gate_preflight.py" in workflow


def build_hook_repository(root: Path, staged_filler: int) -> None:
    """Stage a BMAD-reinstall-sized skill-tree change in a scratch repository."""

    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    install_tree(root)
    for name in ("_bmad/scripts/check_lifecycle_gate_preflight.py", "_bmad/scripts/verify_evidence_boundary.py", ".githooks/pre-commit"):
        target = root / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / name, target)
    filler = root / ".claude/skills/vendored"
    filler.mkdir(parents=True, exist_ok=True)
    for index in range(staged_filler):
        (filler / f"step-{index:05d}.md").write_text(
            f"# Vendored upstream step {index}\n\nReinstalled by the upgrade.\n", encoding="utf-8"
        )
    subprocess.run(["git", "-C", str(root), "add", "-A"], check=True)


def run_hook(root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["bash", str(root / ".githooks/pre-commit")],
        cwd=root,
        check=False,
        capture_output=True,
        text=True,
    )


def test_the_pre_commit_hook_rejects_a_large_reinstall_that_stripped_a_gate(tmp_path: Path) -> None:
    """Executed, not grepped: a huge staged file list must not make the hook fail open.

    Piping the staged list into `grep -q` kills git with SIGPIPE, and under `set -o pipefail` the
    non-zero pipeline reads as "nothing staged" -- skipping the check on exactly the oversized
    BMAD reinstall it exists for. 2200 staged skill paths reproduces that.
    """

    build_hook_repository(tmp_path, staged_filler=2200)
    staged = subprocess.check_output(
        ["git", "-C", str(tmp_path), "diff", "--cached", "--name-only"], text=True
    ).splitlines()
    assert len(staged) > 2200

    assert run_hook(tmp_path).returncode == 0, "a correctly gated tree must commit"

    victim = verifier.ACTIVE_ROUTE_PATHS[0]
    target = tmp_path / victim
    text = target.read_text(encoding="utf-8")
    target.write_text(text.replace(verifier.GATE_MARKER, "Removed by the upgrade", 1), encoding="utf-8")

    completed = run_hook(tmp_path)

    assert completed.returncode != 0, completed.stdout
    assert victim in completed.stderr
    assert "LIFECYCLE_GATE_MISSING" in completed.stderr


def test_the_pre_commit_hook_fires_from_a_subdirectory_and_covers_its_own_removal(
    tmp_path: Path,
) -> None:
    """CWD-relative pathspecs would filter wrongly; the detector must also guard itself."""

    build_hook_repository(tmp_path, staged_filler=2)
    victim = verifier.ACTIVE_ROUTE_PATHS[0]
    target = tmp_path / victim
    target.write_text(
        target.read_text(encoding="utf-8").replace(verifier.GATE_MARKER, "gone", 1), encoding="utf-8"
    )

    completed = subprocess.run(
        ["bash", str(tmp_path / ".githooks/pre-commit")],
        cwd=tmp_path / "_bmad/scripts",
        check=False,
        capture_output=True,
        text=True,
    )

    assert completed.returncode != 0, completed.stdout

    # Staging only the detector's own deletion must still run the check.
    subprocess.run(
        ["git", "-C", str(tmp_path), "-c", "user.name=Fixture",
         "-c", "user.email=fixture@example.invalid", "commit", "-q", "-m", "seed"],
        check=True,
    )
    subprocess.run(
        ["git", "-C", str(tmp_path), "rm", "-q", "--cached", "_bmad/scripts/check_lifecycle_gate_preflight.py"],
        check=True,
    )
    (tmp_path / "_bmad/scripts/check_lifecycle_gate_preflight.py").unlink()

    completed = run_hook(tmp_path)

    assert completed.returncode == 1
    assert "cannot verify the lifecycle evidence gates" in completed.stderr


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
