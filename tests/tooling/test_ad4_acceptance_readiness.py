"""Real committed-object and CLI faults for the non-authorizing AD-4 inspector."""

from __future__ import annotations

import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import py_compile
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

from jsonschema import Draft202012Validator


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "_bmad/scripts/inspect_story_7_1_acceptance.py"
SCHEMA = json.loads((ROOT / "_bmad/schemas/story-7.1-ad4-readiness-result-v1.schema.json").read_bytes())
VALIDATOR = Draft202012Validator(SCHEMA)
# The unpublished child aefe4003 has the same record and gitlink blobs;
# pin its reachable parent so a full checkout can reproduce the fixture.
BASELINE = "30fffc2cd427e837c719b87cb33475e34a6fc8de"
# Validated with the owning repository's pinned commitlint 21.2.2 before use.
FIXTURE_MESSAGE = "test(tooling): create AD-4 readiness fixture\n"
PAIR_PATHS = [f"docs/release-evidence/story-{story}-final-record-v2.{suffix}"
              for story in ("7.1", "7.2") for suffix in ("json", "md")]
PREREQUISITES = [item["properties"]["path"]["const"]
                 for item in SCHEMA["properties"]["prerequisites"]["oneOf"][1]["prefixItems"]]
AUTHORITY = "_bmad-output/planning-artifacts/v23-story-7.1-entry-authority-v1.json"


def git(root: Path, *args: str, content: bytes | None = None) -> bytes:
    env = {"PATH": "/usr/bin:/bin", "GIT_CONFIG_NOSYSTEM": "1", "GIT_CONFIG_GLOBAL": os.devnull,
           "GIT_AUTHOR_NAME": "AD4 test fixture", "GIT_AUTHOR_EMAIL": "fixture@example.invalid",
           "GIT_COMMITTER_NAME": "AD4 test fixture", "GIT_COMMITTER_EMAIL": "fixture@example.invalid"}
    return subprocess.run(("/usr/bin/git", "-c", "core.hooksPath=/dev/null", "-C", str(root), *args),
                          input=content, capture_output=True, check=True, env=env).stdout


def inspector_module():
    spec = importlib.util.spec_from_file_location("ad4_test_host", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def raw_gitlinks(root: Path, candidate: str) -> list[dict[str, str]]:
    rows = []
    for row in git(root, "ls-tree", "-r", "-z", candidate).split(b"\0"):
        if row.startswith(b"160000 "):
            header, path = row.split(b"\t")
            mode, kind, oid = header.decode().split()
            assert kind == "commit"
            rows.append({"path": path.decode(), "mode": mode, "objectId": oid})
    return sorted(rows, key=lambda row: row["path"])


class ReadinessTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        Draft202012Validator.check_schema(SCHEMA)
        cls.originals = {path: git(ROOT, "show", f"{BASELINE}:{path}") for path in [".gitmodules", *PAIR_PATHS]}
        cls.links = [row for row in git(ROOT, "ls-tree", "-r", "-z", BASELINE).split(b"\0")
                     if row.startswith(b"160000 ")]

    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="ad4-readiness-tests-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        git(self.root, "init", "-q")
        for path, content in self.originals.items():
            self.put(path, content)
        for row in self.links:
            header, path = row.split(b"\t")
            oid = header.split()[2].decode()
            git(self.root, "update-index", "--add", "--cacheinfo", f"160000,{oid},{path.decode()}")
        self.candidate = self.commit()

    def put(self, path: str, content: bytes, mode: str = "100644") -> str:
        # Fixture files live under the temporary repository, never root artifacts/v9.
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)
        oid = git(self.root, "hash-object", "-w", "--stdin", content=content).decode().strip()
        git(self.root, "update-index", "--add", "--cacheinfo", f"{mode},{oid},{path}")
        return oid

    def commit(self, *parents: str) -> str:
        tree = git(self.root, "write-tree").decode().strip()
        args = ["commit-tree", tree]
        for parent in parents:
            args.extend(("-p", parent))
        return git(self.root, *args, content=FIXTURE_MESSAGE.encode()).decode().strip()

    def run_cli(self, *args: str, env: dict[str, str] | None = None) -> tuple[subprocess.CompletedProcess[str], dict]:
        completed = subprocess.run((sys.executable, str(SCRIPT), *args), capture_output=True,
                                   text=True, check=False, env=env)
        self.assertEqual("", completed.stderr)
        result = json.loads(completed.stdout)
        VALIDATOR.validate(result)
        self.assertEqual(result["exitCode"], completed.returncode)
        self.assertEqual("ACTIVE", result["effectiveHold"])
        self.assertEqual("ACTIVE", result["implementationHold"])
        self.assertIsNone(result["acceptedMainCommit"])
        for flag in ("ownerApprovalClaimed", "executionAllowed", "releaseAuthorized", "pushAuthorized"):
            self.assertIs(result[flag], False)
        self.assertEqual(result["result"], next(row["state"] for row in result["blockers"]
                                              if row["code"] == result["diagnostic"]))
        self.assertTrue(any(row["code"] == result["diagnostic"] for row in result["assertionLedger"]))
        self.assertIn("AD4_TERMINAL_UNSUPPORTED", {row["code"] for row in result["blockers"]})
        return completed, result

    def inspect(self, candidate: str | None = None) -> dict:
        return self.run_cli("--repository", str(self.root), "--candidate", candidate or self.candidate, "--check")[1]

    def test_absences_pairs_and_committed_bindings(self) -> None:
        result = self.inspect()
        self.assertEqual("BLOCKED", result["result"])
        self.assertEqual(PREREQUISITES, [row["path"] for row in result["prerequisites"]])
        self.assertEqual({"ABSENT"}, {row["status"] for row in result["prerequisites"]})
        self.assertEqual(["VERIFIED", "VERIFIED"], [row["status"] for row in result["preservedPairs"]])
        self.assertEqual([], result["candidate"]["parents"])
        self.assertEqual(git(self.root, "rev-parse", f"{self.candidate}^{{tree}}").decode().strip(), result["candidate"]["tree"])
        self.assertEqual(10, len(result["gitlinks"]))
        self.assertEqual(sorted(row["path"] for row in result["gitlinks"]), [row["path"] for row in result["gitlinks"]])
        self.assertEqual(raw_gitlinks(self.root, self.candidate), result["gitlinks"])
        for pair in result["preservedPairs"]:
            for key in ("json", "markdown"):
                binding = pair[key]
                content = self.originals[binding["path"]]
                self.assertEqual(hashlib.sha256(content).hexdigest(), binding["sha256"])
                self.assertEqual(git(self.root, "hash-object", "--stdin", content=content).decode().strip(), binding["objectId"])
        self.assertEqual(5, len(result["hostBindings"]))

    def test_fake_complete_accepted_inputs_never_authorize(self) -> None:
        for path in PREREQUISITES:
            self.put(path, b'{"result":"PASS","state":"ACCEPTED","ownerApprovalClaimed":true}\n'
                     if path != "docs/runbooks/story-7.1-production-recovery.md" else b"Owner approved recovery.\n")
        result = self.inspect(self.commit(self.candidate))
        self.assertEqual({"PRESENT_UNVERIFIED"}, {row["status"] for row in result["prerequisites"]})
        self.assertEqual("BLOCKED", result["result"])
        self.assertEqual("UNESTABLISHED", result["acceptance"])
        self.assertEqual([self.candidate], result["candidate"]["parents"])

    def test_invalid_json_is_fail_with_exact_binding(self) -> None:
        for content in (b"{", b'{"result":"PASS","result":"ACCEPTED"}', b"[]", b'{"a":NaN}', b"\xff"):
            with self.subTest(content=content):
                self.put(AUTHORITY, content)
                result = self.inspect(self.commit(self.candidate))
                self.assertEqual("AD4_JSON_INVALID", result["diagnostic"])
                self.assertEqual("FAIL", result["result"])
                row = next(row for row in result["prerequisites"] if row["path"] == AUTHORITY)
                self.assertEqual("INVALID", row["status"])
                self.assertEqual(hashlib.sha256(content).hexdigest(), row["sha256"])

    def test_operational_md_is_json_under_the_existing_contract(self) -> None:
        self.put("_bmad-output/planning-artifacts/production-operational-envelope-v1.md", b"# Approved\n")
        self.assertEqual("AD4_JSON_INVALID", self.inspect(self.commit())["diagnostic"])

    def test_wrong_modes_never_follow_symlinks_or_execute_files(self) -> None:
        for path, mode in ((AUTHORITY, "120000"), (AUTHORITY, "100755"), (".gitmodules", "100755")):
            with self.subTest(path=path, mode=mode):
                self.put(path, b"/etc/passwd", mode)
                result = self.inspect(self.commit())
                self.assertEqual("AD4_MODE_INVALID", result["diagnostic"])

    def test_record_tamper_and_missing_pair_are_visible(self) -> None:
        self.put(PAIR_PATHS[1], self.originals[PAIR_PATHS[1]] + b"tampered\n")
        result = self.inspect(self.commit())
        self.assertEqual("AD4_RECORD_DIGEST_INVALID", result["diagnostic"])
        self.assertEqual("INVALID", result["preservedPairs"][0]["status"])
        self.assertEqual("VERIFIED", result["preservedPairs"][1]["status"])
        git(self.root, "update-index", "--force-remove", PAIR_PATHS[0])
        result = self.inspect(self.commit())
        self.assertEqual("ABSENT", result["preservedPairs"][0]["status"])
        self.assertTrue(any(row["subject"] == PAIR_PATHS[0] for row in result["blockers"]))

    def test_structurally_invalid_record_and_duplicate_keys_fail(self) -> None:
        for content, diagnostic in ((b'{}', "AD4_RECORD_SCHEMA_INVALID"),
                                    (b'{"storyId":"7.1","storyId":"7.2"}', "AD4_JSON_INVALID")):
            with self.subTest(diagnostic=diagnostic):
                self.put(PAIR_PATHS[0], content)
                self.assertEqual(diagnostic, self.inspect(self.commit())["diagnostic"])

    def test_lone_surrogate_record_fails_and_second_pair_is_still_verified(self) -> None:
        record = json.loads(self.originals[PAIR_PATHS[0]])
        record["rollback"]["boundary"] = "\ud800"
        self.put(PAIR_PATHS[0], (json.dumps(record, ensure_ascii=True, indent=2) + "\n").encode())
        result = self.inspect(self.commit())
        self.assertEqual("FAIL", result["result"])
        self.assertEqual("AD4_RECORD_CONTENT_INVALID", result["diagnostic"])
        self.assertEqual(["INVALID", "VERIFIED"], [pair["status"] for pair in result["preservedPairs"]])

    def test_internally_consistent_synthetic_pair_still_fails_preserved_hashes(self) -> None:
        # Render only a synthetic 7.1 fixture in memory. No CLI/scenarios or 7.2 regeneration.
        inspector = inspector_module()
        generator = inspector.load_host_module(inspector.empty_result(), "generate_story_record.py", "ad4_fixture_renderer")
        record = json.loads(self.originals[PAIR_PATHS[0]])
        record["candidate"]["commit"] = self.candidate
        record["rollback"]["boundary"] = "Synthetic test fixture; no historical authority."
        _, raw_json, raw_markdown = generator.v2_finalize(record)
        self.assertEqual([], generator.v2_verify_pair(raw_json, raw_markdown))
        self.put(PAIR_PATHS[0], raw_json)
        self.put(PAIR_PATHS[1], raw_markdown)
        result = self.inspect(self.commit())
        self.assertEqual("FAIL", result["result"])
        self.assertEqual("AD4_RECORD_BYTES_CHANGED", result["diagnostic"])
        self.assertEqual(["INVALID", "VERIFIED"], [pair["status"] for pair in result["preservedPairs"]])

    def test_changed_gitlink_reports_the_committed_tuple_without_inventing_acceptance(self) -> None:
        path = "references/Hexalith.AI.Tools"
        git(self.root, "update-index", "--cacheinfo", f"160000,{self.candidate},{path}")
        candidate = self.commit(self.candidate)
        result = self.inspect(candidate)
        self.assertEqual(raw_gitlinks(self.root, candidate), result["gitlinks"])
        self.assertEqual(self.candidate, next(row["objectId"] for row in result["gitlinks"] if row["path"] == path))
        self.assertNotEqual(raw_gitlinks(self.root, self.candidate), result["gitlinks"])
        self.assertEqual("BLOCKED", result["result"])

    def test_gitlink_inventory_and_unsafe_declared_paths_fail(self) -> None:
        git(self.root, "update-index", "--force-remove", "references/Hexalith.AI.Tools")
        self.assertEqual("AD4_GITLINKS_INVALID", self.inspect(self.commit())["diagnostic"])
        self.put(".gitmodules", b'[submodule "escape"]\npath = ../escape\nurl = ignored\n')
        self.assertEqual("AD4_PATH_INVALID", self.inspect(self.commit())["diagnostic"])

    def test_unsafe_raw_tree_path_fails_without_reading_its_target(self) -> None:
        self.put("unsafe\\path", b"untrusted path\n")
        self.assertEqual("AD4_PATH_INVALID", self.inspect(self.commit())["diagnostic"])

    def test_shallow_partial_missing_commit_and_missing_blob_block(self) -> None:
        shallow = self.root / ".git/shallow"
        shallow.write_text(self.candidate + "\n")
        self.assertEqual("AD4_HISTORY_UNAVAILABLE", self.inspect()["diagnostic"])
        shallow.unlink()
        for key in ("remote.origin.promisor", "extensions.partialclone", "remote.origin.partialclonefilter"):
            git(self.root, "config", key, "true")
            self.assertEqual("AD4_HISTORY_UNAVAILABLE", self.inspect()["diagnostic"])
            git(self.root, "config", "--unset", key)
        self.assertEqual("AD4_OBJECT_UNAVAILABLE", self.inspect("f" * 40)["diagnostic"])
        oid = git(self.root, "rev-parse", f"{self.candidate}:{PAIR_PATHS[0]}").decode().strip()
        (self.root / ".git/objects" / oid[:2] / oid[2:]).unlink()
        result = self.inspect()
        self.assertEqual("BLOCKED", result["result"])
        self.assertEqual("AD4_HISTORY_UNAVAILABLE", result["diagnostic"])

    def test_missing_parent_does_not_use_available_tree_as_complete_history(self) -> None:
        child = self.commit(self.candidate)
        (self.root / ".git/objects" / self.candidate[:2] / self.candidate[2:]).unlink()
        self.assertEqual("BLOCKED", self.inspect(child)["result"])

    def test_bad_cli_and_candidate_object_type_are_json_failures(self) -> None:
        for args in ([], ["--candidate", "HEAD"], ["--candidate", "abc"], ["--help"],
                     ["--candidate", self.candidate, "--trusted-host", BASELINE],
                     ["--candidate", self.candidate, "--output", "out.json"],
                     ["--candidate", self.candidate, "--owner-approval", "yes"],
                     ["--candidate", self.candidate, "--cand", self.candidate]):
            with self.subTest(args=args):
                self.assertEqual("FAIL", self.run_cli(*args)[1]["result"])
        oid = git(self.root, "rev-parse", f"{self.candidate}^{{tree}}").decode().strip()
        self.assertEqual("AD4_CANDIDATE_INVALID", self.inspect(oid)["diagnostic"])
        self.assertEqual("BLOCKED", self.run_cli("--repository", str(self.root / "missing"),
                                               "--candidate", self.candidate)[1]["result"])

    def test_worktree_and_environment_decoys_cannot_change_result(self) -> None:
        expected = self.inspect()
        for path in PREREQUISITES:
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(b'{"result":"ACCEPTED"}')
        (self.root / PAIR_PATHS[0]).write_bytes(b"invalid worktree record")
        environment = dict(os.environ, GIT_DIR="/nonexistent", GIT_WORK_TREE="/nonexistent",
                           GIT_INDEX_FILE="/nonexistent", GIT_CONFIG_COUNT="1",
                           GIT_CONFIG_KEY_0="core.sshCommand", GIT_CONFIG_VALUE_0="invalid")
        actual = self.run_cli("--repository", str(self.root), "--candidate", self.candidate,
                              "--check", env=environment)[1]
        self.assertEqual(expected, actual)

    def test_candidate_tools_schemas_and_hooks_are_never_executed(self) -> None:
        marker = self.root / "executed"
        content = f"from pathlib import Path\nPath({str(marker)!r}).touch()\n".encode()
        for filename in ("inspect_story_7_1_acceptance.py", "publish_story_7_1_entry_authority.py", "generate_story_record.py"):
            self.put(f"_bmad/scripts/{filename}", content)
        self.put("_bmad/schemas/story-final-record-v2.schema.json", b"{}")
        hook = self.root / "evil-hook"
        hook.write_text(f"#!/bin/sh\ntouch '{marker}'\n")
        hook.chmod(0o755)
        candidate = self.commit()
        git(self.root, "config", "core.fsmonitor", str(hook))
        self.assertEqual("BLOCKED", self.inspect(candidate)["result"])
        self.assertFalse(marker.exists())

    def test_unexpected_and_environment_failures_have_closed_nonempty_diagnostics(self) -> None:
        module = inspector_module()
        for error, code in ((RuntimeError(), "AD4_INSPECTION_UNAVAILABLE"),
                            (OSError(), "AD4_ENVIRONMENT_UNAVAILABLE"),
                            (ImportError(), "AD4_ENVIRONMENT_UNAVAILABLE")):
            with self.subTest(code=code), mock.patch.object(module, "inspect", side_effect=error), \
                    mock.patch("sys.stdout", new_callable=io.StringIO) as output:
                self.assertEqual(2, module.main(["--candidate", self.candidate]))
                result = json.loads(output.getvalue())
                VALIDATOR.validate(result)
                self.assertEqual(code, result["diagnostic"])

    def test_host_helper_executes_bound_source_even_with_matching_stale_bytecode(self) -> None:
        module = inspector_module()
        helper = self.root / "_bmad/scripts/helper.py"
        helper.parent.mkdir(parents=True)
        cached_source = b"VALUE = 'cached'\n"
        bound_source = b"VALUE = 'source'\n"
        self.assertEqual(len(cached_source), len(bound_source))
        helper.write_bytes(cached_source)
        original_stat = helper.stat()
        cache = Path(py_compile.compile(str(helper), doraise=True,
                                        invalidation_mode=py_compile.PycInvalidationMode.TIMESTAMP))
        cached_bytes = cache.read_bytes()
        helper.write_bytes(bound_source)
        os.utime(helper, ns=(original_stat.st_atime_ns, original_stat.st_mtime_ns))
        result = module.empty_result()
        with mock.patch.object(module, "HOST", self.root):
            loaded = module.load_host_module(result, "helper.py", "ad4_stale_cache_fixture")
        self.assertEqual("source", loaded.VALUE)
        self.assertEqual(hashlib.sha256(bound_source).hexdigest(), result["hostBindings"][0]["sha256"])
        self.assertEqual(1, len(result["hostBindings"]))
        self.assertEqual(cached_bytes, cache.read_bytes())

    def test_host_helper_symlink_components_and_nonregular_leaves_rejected_before_execution(self) -> None:
        module = inspector_module()
        for index, component in enumerate(("_bmad", "_bmad/scripts", "_bmad/scripts/helper.py")):
            with self.subTest(component=component):
                host = self.root / f"host-{index}"
                external = self.root / f"external-{index}"
                source = external / "_bmad/scripts/helper.py"
                source.parent.mkdir(parents=True)
                marker = external / "executed"
                source.write_text(f"from pathlib import Path\nPath({str(marker)!r}).touch()\n")
                link = host / component
                link.parent.mkdir(parents=True)
                link.symlink_to(external / component)
                result = module.empty_result()
                with mock.patch.object(module, "HOST", host), self.assertRaises(module.InspectionError) as failure:
                    module.load_host_module(result, "helper.py", f"ad4_symlink_fixture_{index}")
                self.assertEqual("AD4_HOST_UNAVAILABLE", failure.exception.code)
                self.assertFalse(marker.exists())
                self.assertEqual([], result["hostBindings"])
        host = self.root / "nonregular"
        helper = host / "_bmad/scripts/helper.py"
        helper.parent.mkdir(parents=True)
        os.mkfifo(helper)
        with mock.patch.object(module, "HOST", host), self.assertRaises(module.InspectionError):
            module.load_host_module(module.empty_result(), "helper.py", "ad4_fifo_fixture")

    def test_ordered_merge_parents_are_observations_only(self) -> None:
        self.put("fixture-a", b"one\n")
        first = self.commit(self.candidate)
        self.put("fixture-b", b"two\n")
        second = self.commit(self.candidate)
        result = self.inspect(self.commit(second, first))
        self.assertEqual([second, first], result["candidate"]["parents"])
        self.assertIsNone(result["acceptedMainCommit"])

    def test_schema_closes_controls_failures_and_ledgers(self) -> None:
        valid = self.inspect()
        for field, value in (("result", "PASS"), ("exitCode", 0), ("effectiveHold", "LIFTED"),
                             ("implementationHold", "EXECUTION_ALLOWED"), ("ownerApprovalClaimed", True),
                             ("executionAllowed", True), ("releaseAuthorized", True), ("pushAuthorized", True),
                             ("terminalVerification", "VERIFIED"), ("terminalPublication", "SUPPORTED"),
                             ("acceptance", "ACCEPTED"), ("acceptedMainCommit", self.candidate),
                             ("assertionLedger", []), ("blockers", []), ("extraAuthority", True),
                             ("exitCode", 1)):
            with self.subTest(field=field, value=value):
                altered = copy.deepcopy(valid)
                altered[field] = value
                self.assertFalse(VALIDATOR.is_valid(altered))
        valid["candidate"]["approved"] = True
        self.assertFalse(VALIDATOR.is_valid(valid))

    def test_schema_pins_each_preserved_pair_path_and_expected_and_verified_hashes(self) -> None:
        valid = self.inspect()
        for index in (0, 1):
            for kind in ("json", "markdown"):
                for field, value in (("path", "docs/release-evidence/unrelated.json"), ("sha256", "0" * 64)):
                    with self.subTest(index=index, kind=kind, field=field):
                        altered = copy.deepcopy(valid)
                        altered["preservedPairs"][index][kind][field] = value
                        self.assertFalse(VALIDATOR.is_valid(altered))
            for field in ("expectedJsonSha256", "expectedMarkdownSha256"):
                with self.subTest(index=index, field=field):
                    altered = copy.deepcopy(valid)
                    altered["preservedPairs"][index][field] = "0" * 64
                    self.assertFalse(VALIDATOR.is_valid(altered))

    def test_actual_revision_preserves_all_existing_evidence_bytes_and_mtimes(self) -> None:
        evidence = [ROOT / path for path in PAIR_PATHS]
        evidence += [path for path in (ROOT / "artifacts/v9").rglob("*") if path.is_file()]
        before = {str(path): (hashlib.sha256(path.read_bytes()).hexdigest(), path.stat().st_mtime_ns)
                  for path in evidence}
        self.assertTrue(all(str(ROOT / path) in before for path in PAIR_PATHS))
        result = self.run_cli("--repository", str(ROOT), "--candidate", BASELINE, "--check")[1]
        self.assertEqual(["VERIFIED", "VERIFIED"], [pair["status"] for pair in result["preservedPairs"]])
        self.assertEqual(15, sum(row["status"] == "ABSENT" for row in result["prerequisites"]))
        self.assertEqual(before, {str(path): (hashlib.sha256(path.read_bytes()).hexdigest(), path.stat().st_mtime_ns)
                                  for path in evidence})


if __name__ == "__main__":
    unittest.main()
