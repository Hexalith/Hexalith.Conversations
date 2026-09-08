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
    """Copy only the declared routes, mirroring how an upgrade reinstalls them."""

    for relative in verifier.ACTIVE_ROUTE_PATHS:
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative, target)


def codes(root: Path) -> list[str]:
    findings = preflight.check_routes(root, verifier) + preflight.check_orphan_gates(root, verifier)
    for finding in findings:
        assert finding["detail"], finding
    return [finding["code"] for finding in findings]


def test_the_installed_repository_passes_and_names_every_declared_route() -> None:
    assert preflight.check_routes(ROOT, verifier) == []
    assert preflight.check_orphan_gates(ROOT, verifier) == []
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


def test_the_cli_emits_json_and_a_pass_exit_code(tmp_path: Path, capsys) -> None:
    install_tree(tmp_path)

    assert preflight.main(["--repository", str(tmp_path), "--json"]) == 0
    document = json.loads(capsys.readouterr().out)

    assert document["result"] == "PASS"
    assert document["routes"] == 10
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


def test_the_pre_commit_hook_invokes_the_preflight_check() -> None:
    """The wiring is the point: an upgrade commit must hit this check."""

    hook = (ROOT / ".githooks/pre-commit").read_text(encoding="utf-8")

    assert "check_lifecycle_gate_preflight.py" in hook
    assert ".claude/skills" in hook and ".agents/skills" in hook
    workflow = (ROOT / ".github/workflows/planning-authority-preflight.yml").read_text(
        encoding="utf-8"
    )
    assert "check_lifecycle_gate_preflight.py" in workflow


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
