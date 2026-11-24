"""Verify that the installed Build routes receive the project's validation policy."""

from pathlib import Path
import shutil
import subprocess
import sys

import pytest


ROOT = Path(__file__).resolve().parents[3]


@pytest.mark.parametrize("skill_tree", [".agents", ".claude"])
def test_rendered_build_routes_load_current_change_validation(tmp_path: Path, skill_tree: str) -> None:
    for relative in ("_bmad/config.toml", "_bmad/custom/config.toml", "_bmad/custom/bmad-build.toml"):
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, target)

    result = subprocess.run(
        [
            sys.executable, str(ROOT / "_bmad/scripts/render_skill.py"),
            "--project-root", str(tmp_path),
            "--skill", str(ROOT / skill_tree / "skills/bmad-build"),
        ],
        capture_output=True, text=True, check=True, timeout=15,
    )
    prefix = "read and follow "
    assert result.stdout.startswith(prefix)
    rendered = Path(result.stdout.removeprefix(prefix).strip())
    assert rendered.is_relative_to(tmp_path / "_bmad/render")
    text = rendered.read_text(encoding="utf-8")
    facts = text.split("### Step 2: Load Persistent Facts", 1)[1].split("### Step 3:", 1)[0]
    activation = text.split("### Step 3: Execute Append Steps", 1)[1].split("## WORKFLOW ARCHITECTURE", 1)[0]

    assert "file:{project-root}/docs/runbooks/current-change-validation.md" in facts
    assert "follow the current-change runbook" in activation
    assert "require the full product build and test suite before marking a product story done" in activation
    assert "Reproduce historical evidence-boundary results only when the spec explicitly asks" in activation
