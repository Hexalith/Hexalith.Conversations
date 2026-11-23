from pathlib import Path
import subprocess
import sys
import unittest


class JourneyRunnerTest(unittest.TestCase):
    def test_live_modes_require_their_matching_arguments(self):
        runner = str(Path(__file__).with_name("run.py"))
        base = [sys.executable, runner, "--feed", "unused", "--version", "1.0.0"]
        cases = (
            ([], "Full qualification requires"),
            (["--fault-api-dll", "api"], "Full qualification requires"),
            (["--apphost", "app"], "Full qualification requires"),
            (["--build-only", "--apphost", "app"], "only used for full qualification"),
            (["--without-status-outage", "--fault-api-dll", "api"], "only used for full qualification"),
            (["--build-only", "--without-status-outage"], "not allowed with argument"),
        )
        for options, expected in cases:
            with self.subTest(options=options):
                result = subprocess.run(base + options, capture_output=True, text=True, timeout=10)
                self.assertEqual(result.returncode, 2)
                self.assertIn(expected, result.stderr)
