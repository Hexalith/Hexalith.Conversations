"""Exercise owner gates without executing a committed dependency or any HTTP request."""

import base64
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "eng/ext-conv-ai-1-qualification.ps1"
HEAD = "1" * 40
PLATFORM = "2" * 40
PROTECTION = "3" * 40


class QualificationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.configuration = {
            "tenantId": "tenant-a", "servicePartyId": "party", "workerPrincipalId": "worker",
            "namespaceInstallationId": "installation", "receiverPrincipalId": "receiver",
            "lookupPrincipalId": "lookup", "receiverStorageIdentity": "receiver-store",
            "attemptJournalDirectory": str(self.root / "owner-journal"),
        }
        self.manifest = self.configuration | {"operations": {
            "post": {"method": "POST", "bodyFile": "bodies/post.json"},
            "retry": {"method": "POST", "bodyFile": "bodies/post.json"},
        }}
        self.write("bodies/post.json", {"intent": "exact"})
        self.write("configuration.json", self.configuration)
        self.write("hostProfile.json", {"installedProfile": "qualification"})
        self.write("hostVerifierEvidence.json", {
            "targetCommit": PLATFORM, "verificationMode": "Live", "passPath": True,
            "profileSha256": self.digest("hostProfile.json"),
        })
        self.write("plan.json", {"schemaVersion": 1, "bodyFiles": {
            "bodies/post.json": self.digest("bodies/post.json"),
        }})
        names = ["configuration", "plan", "hostProfile", "hostVerifierEvidence", "sourceAnchor",
                 "partyAnchor", "approvalAnchor", "workerAnchor", "receiverAnchor", "lookupAnchor",
                 "observerAnchor", "controlAnchor"]
        for name in names[4:]:
            self.write(name + ".json", {"publicAnchor": name})
        self.write("manifest.json", self.manifest)
        self.binding = {
            "schemaVersion": 1, "cohortId": "OQ-5.4-1", "manifestSha256": self.digest("manifest.json"),
            "files": {name: {"path": name + ".json", "sha256": self.digest(name + ".json")} for name in names},
        }
        self.status = "Committed"
        self.host_target = PLATFORM
        self.secrets_target = PLATFORM
        self.cohort = "Accepted by the project owner; contract-conformance receiver; Level 4."

    def write(self, path, content):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(json.dumps(content), encoding="utf-8")

    def digest(self, path):
        return hashlib.sha256((self.root / path).read_bytes()).hexdigest()

    def rebind(self, name, content):
        self.write(name + ".json", content)
        self.binding["files"][name]["sha256"] = self.digest(name + ".json")

    def register(self):
        records = []
        for name, target, status in [
            ("EXT-CONV-AI-1", HEAD, "Committed"),
            ("EXT-PROTECTION-1", PROTECTION, self.status),
            ("EXT-HOST-1", self.host_target, "Committed"),
            ("EXT-SECRETS-1", self.secrets_target, "Committed"),
        ]:
            records.append(f"### {name} — fixture\n\n| `AcceptedStatus` | `{status}` |\n"
                           f"| `TargetVersionOrCommit` | `{target}` |\n")
            if name == "EXT-CONV-AI-1" and self.binding is not None:
                records.append("| `RuntimeQualificationBinding` | `" + json.dumps(self.binding) + "` |\n")
        return "\n".join(records) + "\n## Owner Qualification Cohorts\n\n### OQ-5.4-1 — fixture\n\n" + self.cohort

    def run_ps(self, expression):
        env = os.environ | {"OQ_TEST_ROOT": str(self.root), "OQ_TEST_HELPER": str(HELPER)}
        script = """
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $env:OQ_TEST_HELPER
try {
    $root = $env:OQ_TEST_ROOT
    $manifestPath = Join-Path $root 'manifest.json'
    $manifestBytes = [IO.File]::ReadAllBytes($manifestPath)
    $manifest = [Text.Encoding]::UTF8.GetString($manifestBytes) | ConvertFrom-Json -AsHashtable
    $register = Get-Content (Join-Path $root 'register.md') -Raw
""" + expression + """
} catch { [Console]::Error.Write($_.Exception.Message); exit 2 }
"""
        (self.root / "register.md").write_text(self.register(), encoding="utf-8")
        return subprocess.run(["pwsh", "-NoProfile", "-NonInteractive", "-Command", script],
                              env=env, capture_output=True, text=True, timeout=30)

    def qualify(self):
        return self.run_ps(f"Get-OqQualification $register $manifestPath $manifest '{HEAD}' $manifestBytes | ConvertTo-Json -Depth 20")

    def assert_blocked(self, code):
        result = self.qualify()
        self.assertEqual(2, result.returncode, result.stdout + result.stderr)
        self.assertIn(code, result.stderr)

    def test_complete_accepted_inputs_pass_without_runtime_effects(self):
        result = self.qualify()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertRegex(json.loads(result.stdout)["Fingerprint"], r"^[0-9a-f]{64}$")

    def test_uncommitted_protection_refuses(self):
        self.status = "Uncommitted"
        self.assert_blocked("OQ_DEPENDENCY_NOT_COMMITTED:EXT-PROTECTION-1")

    def test_no_owner_accepted_binding_refuses(self):
        self.binding = None
        self.assert_blocked("OQ_ACCEPTED_BINDING_MISSING")

    def test_unexpected_accepted_file_is_not_ignored(self):
        self.binding["files"]["unexpectedAnchor"] = self.binding["files"]["sourceAnchor"]
        self.assert_blocked("OQ_FILE_SET_MISMATCH")

    def test_changed_cohort_refuses(self):
        self.cohort = "Proposed only."
        self.assert_blocked("OQ_COHORT_NOT_ACCEPTED")

    def test_unpinned_host_target_refuses(self):
        self.host_target = "TBD"
        self.assert_blocked("OQ_DEPENDENCY_NOT_COMMITTED:EXT-HOST-1")

    def test_different_platform_targets_refuse(self):
        self.secrets_target = "4" * 40
        self.assert_blocked("OQ_PLATFORM_TARGETS_DIFFER")

    def test_manifest_change_voids_acceptance(self):
        self.write("manifest.json", self.manifest | {"changed": True})
        self.assert_blocked("OQ_FILE_HASH_MISMATCH")

    def test_hash_is_bound_to_the_consumed_manifest_bytes(self):
        result = self.run_ps(f"""
$manifestBytes = [Text.Encoding]::UTF8.GetBytes('{{}}')
Get-OqQualification $register $manifestPath $manifest '{HEAD}' $manifestBytes | Out-Null
""")
        self.assertEqual(2, result.returncode)
        self.assertIn("OQ_CONSUMED_MANIFEST_HASH_MISMATCH", result.stderr)

    def test_post_body_snapshot_survives_a_later_file_change(self):
        result = self.run_ps(f"""
$q = Get-OqQualification $register $manifestPath $manifest '{HEAD}' $manifestBytes
[IO.File]::WriteAllText((Join-Path $root 'bodies/post.json'), '{{"intent":"tampered"}}')
[Text.Encoding]::UTF8.GetString($q.Bodies['bodies/post.json'])
""")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual({"intent": "exact"}, json.loads(result.stdout))

    def test_anchor_change_voids_acceptance(self):
        self.write("receiverAnchor.json", {"publicAnchor": "replacement"})
        self.assert_blocked("OQ_FILE_HASH_MISMATCH")

    def test_missing_anchor_refuses(self):
        del self.binding["files"]["workerAnchor"]
        self.assert_blocked("OQ_FILE_BINDING_MISSING:workerAnchor")

    def test_configuration_substitution_refuses(self):
        self.rebind("configuration", self.configuration | {"workerPrincipalId": "foreign"})
        self.assert_blocked("OQ_CONFIGURATION_MISMATCH:workerPrincipalId")

    def test_shared_receiver_lookup_identity_refuses(self):
        self.manifest["lookupPrincipalId"] = "receiver"
        self.write("manifest.json", self.manifest)
        self.binding["manifestSha256"] = self.digest("manifest.json")
        self.rebind("configuration", self.manifest | {"operations": None})
        self.assert_blocked("OQ_RECEIVER_ROLE_SEPARATION_MISSING")

    def test_component_only_host_evidence_refuses(self):
        self.rebind("hostVerifierEvidence", {"targetCommit": PLATFORM, "verificationMode": "Component",
                                             "passPath": True, "profileSha256": self.digest("hostProfile.json")})
        self.assert_blocked("OQ_HOST_PASS_PATH_NOT_PINNED")

    def test_no_host_pass_path_refuses(self):
        self.rebind("hostVerifierEvidence", {"targetCommit": PLATFORM, "verificationMode": "Live",
                                             "passPath": False, "profileSha256": self.digest("hostProfile.json")})
        self.assert_blocked("OQ_HOST_PASS_PATH_NOT_PINNED")

    def test_changed_post_body_voids_acceptance(self):
        self.write("bodies/post.json", {"intent": "changed"})
        self.assert_blocked("OQ_FILE_HASH_MISMATCH")

    def test_missing_or_unexpected_plan_body_refuses(self):
        self.rebind("plan", {"schemaVersion": 1, "bodyFiles": {"unexpected.json": "0" * 64}})
        self.assert_blocked("OQ_PLAN_BODY_SET_MISMATCH")

    def test_absolute_file_refuses(self):
        self.binding["files"]["sourceAnchor"]["path"] = str(self.root / "sourceAnchor.json")
        self.assert_blocked("OQ_FILE_BINDING_INVALID")

    def test_contained_symlink_is_not_an_accepted_file(self):
        anchor = self.root / "sourceAnchor.json"
        anchor.rename(self.root / "actual-anchor.json")
        anchor.symlink_to(self.root / "actual-anchor.json")
        self.assert_blocked("OQ_FILE_LINK_FORBIDDEN")

    def test_parent_directory_symlink_is_not_containment(self):
        (self.root / "bodies").rename(self.root / "actual-bodies")
        (self.root / "bodies").symlink_to(self.root / "actual-bodies", target_is_directory=True)
        self.assert_blocked("OQ_FILE_LINK_FORBIDDEN")

    def test_symlink_above_manifest_root_refuses(self):
        original = self.root
        installed = original / "installed"
        installed.mkdir()
        for path in list(original.iterdir()):
            if path != installed:
                path.rename(installed / path.name)
        (original / "alias").symlink_to(original, target_is_directory=True)
        self.root = original / "alias/installed"
        self.assert_blocked("OQ_FILE_LINK_FORBIDDEN")

    def test_noncanonical_post_cannot_evade_body_inventory(self):
        for operation in self.manifest["operations"].values():
            operation["method"] = "post"
        self.write("manifest.json", self.manifest)
        self.binding["manifestSha256"] = self.digest("manifest.json")
        self.rebind("plan", {"schemaVersion": 1, "bodyFiles": {}})
        self.assert_blocked("OQ_METHOD_INVALID")

    def test_copied_manifest_cannot_reserve_another_attempt(self):
        expression = f"""
$q = Get-OqQualification $register $manifestPath $manifest '{HEAD}' $manifestBytes
Reserve-OqAttempt $q 'first-evidence.json'
"""
        first = self.run_ps(expression)
        self.assertEqual(0, first.returncode, first.stderr)
        copied = tempfile.TemporaryDirectory()
        self.addCleanup(copied.cleanup)
        shutil.copytree(self.root, copied.name, dirs_exist_ok=True,
                        ignore=shutil.ignore_patterns("owner-journal"))
        self.root = Path(copied.name)
        second = self.run_ps(expression)
        self.assertEqual(2, second.returncode)
        self.assertIn("OQ_ATTEMPT_ALREADY_RECORDED_OR_JOURNAL_UNAVAILABLE", second.stderr)

    def test_one_attempt_survives_changed_evidence_destination(self):
        expression = f"""
$q = Get-OqQualification $register $manifestPath $manifest '{HEAD}' $manifestBytes
$first = Reserve-OqAttempt $q 'original-evidence.json'
$original = Get-Content $first -Raw
try {{ $null = Reserve-OqAttempt $q 'new-evidence.json'; throw 'duplicate was accepted' }}
catch {{ if ($_.Exception.Message -ne 'OQ_ATTEMPT_ALREADY_RECORDED_OR_JOURNAL_UNAVAILABLE') {{ throw }} }}
if ((Get-Content $first -Raw) -cne $original) {{ throw 'original journal changed' }}
Write-Output 'preserved'
"""
        result = self.run_ps(expression)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("preserved", result.stdout.strip())

    def test_property_order_cannot_reset_attempt_identity(self):
        result = self.qualify()
        self.assertEqual(0, result.returncode, result.stderr)
        first = json.loads(result.stdout)["Fingerprint"]
        self.binding["files"] = dict(reversed(list(self.binding["files"].items())))
        result = self.qualify()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(first, json.loads(result.stdout)["Fingerprint"])

    def payload(self, body):
        self.write("payload.json", body)
        return self.run_ps("Read-OqPayload (Get-Content (Join-Path $root 'payload.json') -Raw | ConvertFrom-Json -AsHashtable) | ConvertTo-Json")

    def test_sdk_query_payload_decodes(self):
        body = {"success": True, "payloadBytes": base64.b64encode(b'{"outcome":0}').decode()}
        result = self.payload(body)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual({"outcome": 0}, json.loads(result.stdout))

    def test_direct_query_payload_remains_supported(self):
        result = self.payload({"success": True, "payload": {"outcome": 0}})
        self.assertEqual(0, result.returncode, result.stderr)

    def test_failed_query_cannot_carry_available_payload(self):
        result = self.payload({"success": False, "payload": {"outcome": 0}})
        self.assertEqual(2, result.returncode)
        self.assertIn("OQ_QUERY_UNAVAILABLE", result.stderr)

    def test_nonboolean_query_success_refuses(self):
        result = self.payload({"success": "true", "payload": {"outcome": 0}})
        self.assertEqual(2, result.returncode)

    def test_invalid_sdk_payloads_refuse(self):
        for raw in ("!invalid!", base64.b64encode(b'\xff').decode(), base64.b64encode(b'[]').decode()):
            with self.subTest(raw=raw):
                result = self.payload({"success": True, "payloadBytes": raw})
                self.assertEqual(2, result.returncode)
                self.assertIn("OQ_QUERY_INVALID", result.stderr)

    def test_ambiguous_query_payload_refuses(self):
        result = self.payload({"success": True, "payload": {"outcome": 0}, "payloadBytes": "e30="})
        self.assertEqual(2, result.returncode)

    def test_receiver_requires_the_sdk_encoded_envelope(self):
        for body in ({"outcome": 0}, {"success": True, "payload": {"outcome": 0}}):
            with self.subTest(body=body):
                self.write("payload.json", body)
                result = self.run_ps("Read-OqPayload (Get-Content (Join-Path $root 'payload.json') -Raw | ConvertFrom-Json -AsHashtable) -RequireSdk")
                self.assertEqual(2, result.returncode)
                self.assertIn("OQ_QUERY_INVALID", result.stderr)

    def test_enrolled_provider_assertion_is_reread_after_rotation(self):
        result = self.run_ps("""
$path = Join-Path $root 'lookup.jwt'
[IO.File]::WriteAllText($path, 'first-assertion')
$first = Get-OqCredential ('file:' + $path) -AllowPublishedAssertion
[IO.File]::WriteAllText(($path + '.next'), 'second-assertion')
[IO.File]::Move(($path + '.next'), $path, $true)
$second = Get-OqCredential ('file:' + $path) -AllowPublishedAssertion
if ($first -cne 'first-assertion' -or $second -cne 'second-assertion') { throw 'stale assertion' }
Write-Output 'rotated'
""")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("rotated", result.stdout.strip())


if __name__ == "__main__":
    unittest.main()
