"""Exercise the root-submodule checker against an isolated Git index."""

from pathlib import Path
import subprocess
import sys

import pytest


SCRIPT = Path(__file__).resolve().parents[2] / "scripts/check-root-submodules.py"
DECLARATION = '[submodule "example"]\npath = references/Example\nurl = https://example.com/Example.git\n'


def git(repository: Path, *arguments: str) -> None:
    subprocess.run(["git", "-C", str(repository), *arguments], check=True, capture_output=True)


@pytest.fixture
def repository(tmp_path: Path) -> Path:
    git(tmp_path, "init", "--quiet")
    (tmp_path / ".gitmodules").write_text(DECLARATION, encoding="utf-8")
    git(tmp_path, "add", "--", ".gitmodules")
    git(tmp_path, "update-index", "--add", "--cacheinfo", f"160000,{'a' * 40},references/Example")
    return tmp_path


def check(repository: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(SCRIPT), "--repository", str(repository)],
        check=False,
        capture_output=True,
        text=True,
        timeout=15,
    )


@pytest.mark.parametrize(
    ("declaration", "diagnostic"),
    [
        (
            '[submodule "second"]\npath = references/Example\nurl = https://example.com/Second.git\n',
            "duplicate root submodule path: references/Example",
        ),
        ("path = references/Example\n", "duplicate path for submodule example"),
    ],
)
def test_checker_rejects_duplicate_staged_paths(repository: Path, declaration: str, diagnostic: str) -> None:
    assert check(repository).returncode == 0
    (repository / ".gitmodules").write_text(DECLARATION + declaration, encoding="utf-8")
    git(repository, "add", "--", ".gitmodules")

    result = check(repository)

    assert result.returncode == 1
    assert result.stderr.splitlines() == [f"root submodules: {diagnostic}"]


def test_checker_reads_only_staged_declarations_and_gitlinks(repository: Path) -> None:
    gitmodules = repository / ".gitmodules"
    changed_declaration = DECLARATION.replace("references/Example", "references/Moved")
    gitmodules.write_text(changed_declaration, encoding="utf-8")
    assert check(repository).returncode == 0

    git(repository, "add", "--", ".gitmodules")
    gitmodules.write_text(DECLARATION, encoding="utf-8")

    result = check(repository)

    assert result.returncode == 1
    assert result.stderr.splitlines() == [
        "root submodules: references/Moved: declared but no gitlink is indexed",
        "root submodules: references/Example: indexed under references/ but not declared",
    ]
