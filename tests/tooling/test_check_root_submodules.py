"""Focused checks for the current-tree root submodule gate."""

from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import subprocess
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[2] / "scripts/check-root-submodules.py"
SPEC = spec_from_file_location("check_root_submodules", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
checker = module_from_spec(SPEC)
SPEC.loader.exec_module(checker)


class RootSubmoduleCheckTests(unittest.TestCase):
    def test_current_repository_matches_its_declarations(self) -> None:
        self.assertEqual([], checker.check(SCRIPT.parents[1]))

    def test_missing_and_unexpected_gitlinks_are_reported(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            subprocess.run(["git", "init", "-q", str(root)], check=True)
            (root / ".gitmodules").write_text(
                '[submodule "one"]\n\tpath = references/One\n\turl = https://example.com/One.git\n',
                encoding="utf-8",
            )
            self.assertIn("references/One: declared but no gitlink is indexed", checker.check(root))
            subprocess.run(
                [
                    "git",
                    "-C",
                    str(root),
                    "update-index",
                    "--add",
                    "--cacheinfo",
                    "160000," + "a" * 40 + ",references/Two",
                ],
                check=True,
            )
            problems = checker.check(root)
            self.assertIn("references/Two: indexed under references/ but not declared", problems)
            self.assertIn("references/One: declared but no gitlink is indexed", problems)

    def test_regular_file_cannot_stand_in_for_a_gitlink(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            subprocess.run(["git", "init", "-q", str(root)], check=True)
            (root / ".gitmodules").write_text(
                '[submodule "one"]\n\tpath = references/One\n\turl = https://example.com/One.git\n',
                encoding="utf-8",
            )
            path = root / "references/One"
            path.parent.mkdir()
            path.write_text("ordinary file", encoding="utf-8")
            subprocess.run(["git", "-C", str(root), "add", "--", "references/One"], check=True)
            self.assertIn(
                "references/One: expected a resolved mode-160000 gitlink", checker.check(root)
            )


if __name__ == "__main__":
    unittest.main()
