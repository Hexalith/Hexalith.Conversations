"""Verify the checkpoint-B fixture source bytes and inventory against the frozen proof."""
from hashlib import sha256
from pathlib import Path
import json
import os
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / "tests/fixtures/Hexalith.Conversations.MinimalModule"
PROOF = ROOT / "docs/release-evidence/thin-module-fixture-proof-v1.json"
PROOF_SHA256 = "01e384e6578d57e09d78e686a4cf6420123c0fbc69419f29946e6235d2fef0c3"


class MinimalModuleFrozenFixtureTests(unittest.TestCase):
    def verify_frozen_fixture(self, fixture):
        proof = PROOF.read_bytes()
        self.assertEqual(PROOF_SHA256, sha256(proof).hexdigest())
        proof = json.loads(proof)
        fixture_root = Path(proof["fixtureInventory"]["root"])
        authored = proof["fixtureInventory"]["paths"]
        locks = proof["restore"]["lockFiles"]
        self.assertEqual((13, 4), (len(authored), len(locks)))
        expected = {Path(row["path"]).relative_to(fixture_root): row["sha256"] for row in [*authored, *locks]}
        self.assertEqual(17, len(expected))
        projects = {path.parent for path in expected if path.suffix == ".csproj"}
        actual = set()
        for directory, subdirectories, filenames in os.walk(fixture):
            path = Path(directory)
            relative = path.relative_to(fixture)
            if relative in projects:
                subdirectories[:] = [name for name in subdirectories if name not in {"bin", "obj"}]
            actual.update(relative / name for name in filenames)
            actual.update(relative / name for name in subdirectories if (path / name).is_symlink())
        self.assertEqual(set(expected), actual, "Frozen fixture inventory differs from checkpoint B")
        for path, digest in expected.items():
            self.assertFalse((fixture / path).is_symlink(), str(path))
            self.assertEqual(digest, sha256((fixture / path).read_bytes()).hexdigest(), str(path))
        return set(expected)

    def test_frozen_fixture_matches_checkpoint_b(self):
        expected = self.verify_frozen_fixture(FIXTURE)
        tracked = subprocess.check_output(
            ["git", "ls-files", "-z", "--", str(FIXTURE.relative_to(ROOT))], cwd=ROOT, text=True,
        ).rstrip("\0").split("\0")
        self.assertCountEqual(
            [(FIXTURE.relative_to(ROOT) / path).as_posix() for path in expected],
            tracked,
            "Tracked fixture inventory differs from checkpoint B",
        )

    def test_frozen_fixture_rejects_byte_and_inventory_drift(self):
        with tempfile.TemporaryDirectory() as temporary:
            fixture = Path(temporary) / "fixture"
            proof = json.loads(PROOF.read_bytes())
            for row in [*proof["fixtureInventory"]["paths"], *proof["restore"]["lockFiles"]]:
                source = ROOT / row["path"]
                path = fixture / source.relative_to(FIXTURE)
                path.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(source, path)
            path = fixture / "Directory.Build.props"
            original = path.read_bytes()
            path.write_bytes(original + b"\n")
            with self.assertRaisesRegex(AssertionError, "Directory.Build.props"):
                self.verify_frozen_fixture(fixture)
            path.unlink()
            with self.assertRaisesRegex(AssertionError, "Frozen fixture inventory differs"):
                self.verify_frozen_fixture(fixture)
            path.write_bytes(original)
            (fixture / "unexpected.txt").touch()
            with self.assertRaisesRegex(AssertionError, "Frozen fixture inventory differs"):
                self.verify_frozen_fixture(fixture)
