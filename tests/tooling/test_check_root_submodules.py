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
            subprocess.run(["git", "-C", str(root), "add", "--", ".gitmodules"], check=True)
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
            subprocess.run(["git", "-C", str(root), "add", "--", ".gitmodules"], check=True)
            path = root / "references/One"
            path.parent.mkdir()
            path.write_text("ordinary file", encoding="utf-8")
            subprocess.run(["git", "-C", str(root), "add", "--", "references/One"], check=True)
            self.assertIn(
                "references/One: expected a resolved mode-160000 gitlink", checker.check(root)
            )

    def fixture(self, root: Path) -> None:
        subprocess.run(["git", "init", "-q", str(root)], check=True)
        (root / ".gitmodules").write_text(
            '[submodule "one"]\n\tpath = references/One\n\turl = https://example.com/One.git\n',
            encoding="utf-8",
        )
        subprocess.run(["git", "-C", str(root), "add", "--", ".gitmodules"], check=True)
        subprocess.run(
            ["git", "-C", str(root), "update-index", "--add", "--cacheinfo",
             "160000," + "a" * 40 + ",references/One"],
            check=True,
        )

    def test_separate_names_cannot_declare_the_same_path(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            with (root / ".gitmodules").open("a", encoding="utf-8") as stream:
                stream.write(
                    '[submodule "two"]\n\tpath = references/One\n\turl = https://example.com/Two.git\n'
                )
            subprocess.run(["git", "-C", str(root), "add", "--", ".gitmodules"], check=True)
            self.assertEqual(
                ["duplicate root submodule path: references/One"], checker.check(root)
            )

    def test_repeated_path_setting_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            with (root / ".gitmodules").open("a", encoding="utf-8") as stream:
                stream.write("\tpath = references/One\n")
            subprocess.run(["git", "-C", str(root), "add", "--", ".gitmodules"], check=True)
            self.assertEqual(["duplicate path for submodule one"], checker.check(root))

    def test_worktree_cannot_hide_invalid_staged_declarations(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            path = root / ".gitmodules"
            original = path.read_text(encoding="utf-8")
            path.write_text(original.replace("references/One", "references/Two"), encoding="utf-8")
            subprocess.run(["git", "-C", str(root), "add", "--", ".gitmodules"], check=True)
            path.write_text(original, encoding="utf-8")
            self.assertEqual(
                ["references/Two: declared but no gitlink is indexed",
                 "references/One: indexed under references/ but not declared"],
                checker.check(root),
            )

    def test_unstaged_declaration_edits_do_not_change_staged_result(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            (root / ".gitmodules").write_text("invalid worktree content", encoding="utf-8")
            self.assertEqual([], checker.check(root))

    def test_staged_declarations_need_no_worktree_copy(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            (root / ".gitmodules").unlink()
            self.assertEqual([], checker.check(root))

    def test_unstaged_declarations_cannot_replace_missing_index_entry(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            subprocess.run(
                ["git", "-C", str(root), "rm", "--cached", "-f", "--", ".gitmodules"],
                check=True, capture_output=True,
            )
            self.assertEqual(["root .gitmodules is not indexed"], checker.check(root))

    def test_conflicted_index_declarations_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            object_id = subprocess.check_output(
                ["git", "-C", str(root), "rev-parse", ":.gitmodules"], text=True
            ).strip()
            subprocess.run(
                ["git", "-C", str(root), "update-index", "--index-info"],
                input=f"0 {'0' * 40}\t.gitmodules\n100644 {object_id} 2\t.gitmodules\n",
                text=True, check=True,
            )
            self.assertEqual(
                ["root .gitmodules must be a resolved regular indexed file"], checker.check(root)
            )

    def test_multistage_declaration_conflict_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.fixture(root)
            object_id = subprocess.check_output(
                ["git", "-C", str(root), "rev-parse", ":.gitmodules"], text=True
            ).strip()
            subprocess.run(
                ["git", "-C", str(root), "update-index", "--index-info"],
                input=f"0 {'0' * 40}\t.gitmodules\n" + "".join(
                    f"100644 {object_id} {stage}\t.gitmodules\n" for stage in (1, 2, 3)
                ),
                text=True, check=True,
            )
            self.assertEqual(
                ["root .gitmodules must be a resolved regular indexed file"], checker.check(root)
            )


if __name__ == "__main__":
    unittest.main()
