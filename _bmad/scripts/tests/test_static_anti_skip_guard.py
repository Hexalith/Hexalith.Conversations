"""Static regression guard against ambient skips in Python tooling tests."""

from __future__ import annotations

import ast
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
TEST_ROOT = ROOT / "_bmad/scripts/tests"


def _dotted_name(node: ast.AST) -> str | None:
    if isinstance(node, ast.Name):
        return node.id
    if isinstance(node, ast.Attribute):
        parent = _dotted_name(node.value)
        return f"{parent}.{node.attr}" if parent else node.attr
    return None


def _uses_root_argument(node: ast.Call) -> bool:
    arguments = [*node.args, *(keyword.value for keyword in node.keywords)]
    return any(isinstance(argument, ast.Name) and argument.id == "ROOT" for argument in arguments)


def _source_diagnostics(relative_path: str, source: str) -> list[tuple[str, int, str]]:
    tree = ast.parse(source, filename=relative_path)
    diagnostics: list[tuple[str, int, str]] = []

    for node in ast.walk(tree):
        dotted_name = _dotted_name(node)
        call_name = _dotted_name(node.func) if isinstance(node, ast.Call) else None
        if isinstance(node, ast.Call) and call_name == "pytest.skip":
            diagnostics.append((relative_path, node.lineno, "pytest.skip call"))
        elif (
            isinstance(node, (ast.Attribute, ast.Name))
            and ((isinstance(node, ast.Attribute) and node.attr == "skipif") or dotted_name == "skipif")
        ):
            diagnostics.append((relative_path, node.lineno, "skipif expression"))
        elif (
            isinstance(node, ast.Call)
            and call_name is not None
            and call_name.split(".")[-2:] == ["verifier", "worktree_dirt"]
            and _uses_root_argument(node)
        ):
            diagnostics.append((relative_path, node.lineno, "verifier.worktree_dirt(ROOT) call"))

    return sorted(diagnostics)


def test_python_tooling_lane_has_no_ambient_skip_constructs() -> None:
    prohibited_fixtures = {
        "fixtures/ambient-dirt.py": "verifier.worktree_dirt(ROOT)\n",
        "fixtures/direct-skip.py": "pytest.skip('ambient state')\n",
        "fixtures/skipif-call.py": "skipif(condition)\n",
        "fixtures/skipif-decorator.py": "@pytest.mark.skipif(condition, reason='ambient')\ndef check():\n    pass\n",
        "fixtures/skipif-marker.py": "pytestmark = pytest.mark.skipif(condition, reason='ambient')\n",
    }
    fixture_diagnostics = sorted(
        diagnostic
        for relative_path, source in prohibited_fixtures.items()
        for diagnostic in _source_diagnostics(relative_path, source)
    )
    assert fixture_diagnostics == [
        ("fixtures/ambient-dirt.py", 1, "verifier.worktree_dirt(ROOT) call"),
        ("fixtures/direct-skip.py", 1, "pytest.skip call"),
        ("fixtures/skipif-call.py", 1, "skipif expression"),
        ("fixtures/skipif-decorator.py", 1, "skipif expression"),
        ("fixtures/skipif-marker.py", 1, "skipif expression"),
    ]

    allowed_source = """
# pytest.skip('comment only') and pytest.mark.skipif(True, reason='comment only')
EXPLANATION = "pytest.skip('string only'); skipif(True); verifier.worktree_dirt(ROOT)"
verifier.worktree_dirt(tmp_path)
"""
    assert _source_diagnostics("fixtures/allowed.py", allowed_source) == []

    diagnostics = sorted(
        diagnostic
        for path in TEST_ROOT.rglob("*.py")
        for diagnostic in _source_diagnostics(path.relative_to(ROOT).as_posix(), path.read_text(encoding="utf-8"))
    )
    assert diagnostics == [], "Prohibited ambient-skip constructs:\n" + "\n".join(
        f"{path}:{line}: {message}" for path, line, message in diagnostics
    )
