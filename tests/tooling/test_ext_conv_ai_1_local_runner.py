"""Exercise the Local runner's inherited producer mapping at its process boundary."""

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
RUNNER = ROOT / "eng/verify-ext-conv-ai-1.ps1"
IMPORTS = (
    "CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps",
    "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets",
    "AfterMicrosoftNETSdkTargets",
)
MAPPING_KEYS = IMPORTS + ("HEXALITH_SOURCE_MAPPING_HASH", "Configuration")


class LocalRunnerMappingTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        # Copy only the runner: synthetic tests must never overwrite real built assemblies.
        (self.root / "eng").mkdir()
        (self.root / "eng" / RUNNER.name).write_bytes(RUNNER.read_bytes())
        self.mapping = {"HEXALITH_SOURCE_MAPPING_HASH": "a" * 64, "Configuration": "Debug"}
        for name in IMPORTS:
            path = self.root / (name + ".xml")
            path.write_text("<Project />")
            self.mapping[name] = str(path)
        self.calls = self.root / "calls.jsonl"
        executable = self.root / "dotnet"
        executable.write_text(f"#!{sys.executable}\n" + r'''
import json, os
from pathlib import Path
import sys
args = sys.argv[1:]
if Path(args[0]).name == "pwsh.dll":
    os.execv(os.environ["OQ_REAL_DOTNET"], [os.environ["OQ_REAL_DOTNET"]] + args)
keys = json.loads(os.environ["OQ_MAPPING_KEYS"])
with open(os.environ["OQ_CALLS"], "a") as output:
    output.write(json.dumps({"args": args, "environment": {k: os.environ.get(k) for k in keys}}) + "\n")
if args[0] == "msbuild":
    properties = {"HexalithSourceMappingMode": "source", "UseHexalithProjectReferences": "true",
                  "HexalithSourceMappingHash": os.environ["HEXALITH_SOURCE_MAPPING_HASH"],
                  "HexalithSourceMappingApplies": "true", "Configuration": "Debug"}
    properties.update(json.loads(os.environ.get("OQ_SELECTION_OVERRIDE", "{}")))
    print(json.dumps({"Properties": properties}))
elif args[0] == "build":
    name = Path(args[1]).stem
    directory = Path(args[args.index("--artifacts-path") + 1]) / "bin" / name / "debug"
    directory.mkdir(parents=True, exist_ok=True)
    (directory / (name + ".dll")).write_bytes(b"synthetic process boundary fixture")
else:
    methods = ["MembershipRetryRemovalAndSerializedReplay", "DeterministicPostingLostAcknowledgementAndConcurrentIntent",
               "FacilitatorRosterAndCurrentEditedDeletedRedactedContent",
               "CompleteTenantActiveCountIncludesZeroAgentCallsAndMissingCatalogueIsUnavailable",
               "ApprovedDeletionPublicationDeliveryBackfillRolloverAndPoison",
               "CrossTenantUnrelatedPartyAndRevokedAuthorityFailBeforeDisclosure"] if "-class" in args else ["Synthetic"]
    expected = "Hexalith.Conversations.Server.Tests.Agents.ConversationAgentSixSeamTests"
    tests = "".join(f'<test type="{expected}" method="{method}" result="Pass" />' for method in methods)
    Path(args[args.index("-result-xml") + 1]).write_text(
        '<assemblies><assembly errors="0" failed="0" skipped="0" not-run="0">' + tests + '</assembly></assemblies>')
''')
        executable.chmod(0o755)
        git = self.root / "git"
        git.write_text(f"#!{sys.executable}\nprint('1' * 40)\n")
        git.chmod(0o755)

    def run_local(self, mapping=None, selection=None):
        self.calls.unlink(missing_ok=True)
        env = {key: value for key, value in os.environ.items() if key not in MAPPING_KEYS}
        env.update(self.mapping if mapping is None else mapping)
        env.update({"PATH": str(self.root) + os.pathsep + env["PATH"], "OQ_CALLS": str(self.calls),
                    "OQ_MAPPING_KEYS": json.dumps(MAPPING_KEYS),
                    "OQ_REAL_DOTNET": shutil.which("dotnet"),
                    "OQ_SELECTION_OVERRIDE": json.dumps(selection or {})})
        result = subprocess.run(
            ["pwsh", "-NoProfile", "-File", str(self.root / "eng" / RUNNER.name),
             "-Mode", "Local", "-ArtifactsPath", str(self.root / "artifacts")],
            env=env, capture_output=True, text=True, timeout=45,
        )
        calls = [json.loads(line) for line in self.calls.read_text().splitlines()] if self.calls.exists() else []
        return result, calls

    def test_complete_mapping_reaches_all_projects_without_selecting_roots_or_versions(self):
        result, calls = self.run_local()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        builds = [call for call in calls if call["args"][0] == "build"]
        self.assertEqual(len(builds), 5)
        self.assertIn("Hexalith.Conversations.DeletionConformance.Tests.csproj", builds[-1]["args"][1])
        self.assertEqual(len(calls), 12)  # Evaluation, five builds, six assembly executions.
        for call in calls:
            self.assertEqual(call["environment"], self.mapping)
            self.assertFalse(any(arg.startswith(("-p:Hexalith", "-p:UseHexalith")) for arg in call["args"]))
        evidence = json.loads(next((self.root / "artifacts").glob("run-*/local-evidence.json")).read_text())
        self.assertEqual(evidence["SourceMappingHash"], self.mapping["HEXALITH_SOURCE_MAPPING_HASH"])
        self.assertFalse(evidence["LiveReady"])

    def test_each_missing_environment_value_refuses_before_any_child(self):
        for name in MAPPING_KEYS:
            with self.subTest(name=name):
                result, calls = self.run_local({key: value for key, value in self.mapping.items() if key != name})
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(calls, [])

    def test_invalid_mapping_inputs_refuse_before_any_child(self):
        for change in ({"Configuration": "Release"}, {"HEXALITH_SOURCE_MAPPING_HASH": "changed"},
                       {IMPORTS[0]: str(self.root / "missing.xml")}):
            with self.subTest(change=change):
                result, calls = self.run_local(self.mapping | change)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(calls, [])

    def test_evaluated_mapping_mismatch_refuses_before_builds(self):
        for change in ({"HexalithSourceMappingMode": "package"}, {"UseHexalithProjectReferences": "false"},
                       {"HexalithSourceMappingHash": "b" * 64}, {"HexalithSourceMappingApplies": "false"},
                       {"Configuration": "Release"}):
            with self.subTest(change=change):
                result, calls = self.run_local(selection=change)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual([call["args"][0] for call in calls], ["msbuild"])


if __name__ == "__main__":
    unittest.main()
