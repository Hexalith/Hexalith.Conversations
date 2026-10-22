"""Run the real script with synthetic Git/HTTP boundaries; never qualify a live seam."""

import base64
import copy
import json
import os
from pathlib import Path
import subprocess
import unittest

import test_ext_conv_ai_1_qualification as qualification

HEAD, ROOT = qualification.HEAD, qualification.ROOT


OPERATIONS = """identity party receiverIdentity c1Membership c1ParticipantReadAfterFirst c1MembershipRetry
c1ParticipantRead c1Post c1MessageReadAfterFirst c1PostRetry c1MessageRead c1RosterRead c1Catalogue
c1ZeroAgentSource c1Count c1CurrentRead c1Edit c1EditedRead c1Delete c1DeletedRead c2Approve c2SourceBefore
c2Dispatch c2SourceAfter c2ReceiverLookup c3Approve c3SourcePending c3ReceiverOutage c3SourceOutage c3Restart
c3SourceRestart c3TargetRollover c3ReceiverIdentity c3CurrentBinding c3Retry c3SourceAcknowledged
c3ReceiverLookup c4CrossTenant c4Revoked c4DeletedRead c4RemovedRead c4PoisonSourceBefore c4PoisonInject
c4PoisonSourceAfter c4ReceiverLookup""".split()
RECEIVER_QUERIES = {"receiverIdentity", "c3ReceiverIdentity", "c2ReceiverLookup", "c3ReceiverLookup", "c4ReceiverLookup"}


class LiveRunnerTests(unittest.TestCase):
    def setUp(self):
        self.f = qualification.QualificationTests()
        self.f.setUp()
        self.addCleanup(self.f.doCleanups)
        self.root = self.f.root
        self.a, self.b = "a" * 40, "b" * 40
        self.roles = ["service", "human", "approver", "worker", "party", "receiver", "control", "observer", "receiverChannel"]
        self.environment = os.environ | {"OQ_TEST_ROOT": str(self.root), "OQ_TEST_SCRIPT": str(ROOT / "eng/verify-ext-conv-ai-1-live.ps1")}
        for role in self.roles:
            self.environment["OQ_TEST_CREDENTIAL_" + role] = "synthetic-credential-" + role
        manifest = self.f.manifest
        manifest.update({
            "schemaVersion": 1, "targetCommit": HEAD, "foreignTenantId": "tenant-b", "messageId": "message-one",
            "zeroAgentConversationId": "zero", "receiverTargetVersion": self.a, "rolloverReceiverTargetVersion": self.b,
            "storageIdentity": "source-store", "createdFromInclusive": "2026-10-22T00:00:00+00:00",
            "createdToExclusive": "2026-10-23T00:00:00+00:00",
            "bases": {name: f"http://127.0.0.1:{12000 + i}/" for i, name in enumerate(["gateway", "parties", "receiver", "control", "observer"])},
            "credentialReferences": {role: "env:OQ_TEST_CREDENTIAL_" + role for role in self.roles},
            "operations": {},
        })
        for name in OPERATIONS:
            receiver = name in RECEIVER_QUERIES
            body_file = "bodies/" + name + ".json"
            self.f.write(body_file, {"testOperation": name})
            manifest["operations"][name] = {
                "base": "receiver" if receiver else "parties" if name == "party" else "gateway",
                "credential": "receiver" if receiver else "party" if name == "party" else "service",
                "method": "POST", "path": "query" if receiver else name, "bodyFile": body_file,
            }
            if receiver:
                manifest["operations"][name].update(authentication="EventStoreWorkload", sidecarCredential="receiverChannel")
        self.f.rebind("plan", {"schemaVersion": 1, "bodyFiles": {
            operation["bodyFile"]: self.f.digest(operation["bodyFile"]) for operation in manifest["operations"].values()
        }})
        self.responses = self.make_responses()

    def make_responses(self):
        m = self.f.manifest
        identity = {"commit": HEAD, "tenantId": m["tenantId"], "namespaceInstallationId": m["namespaceInstallationId"],
                    "receiverTargetVersion": self.a, "storageIdentity": m["storageIdentity"], "authorityRevision": "authority-one"}
        receiver = {"schemaVersion": 1, "receiverKind": "ContractConformance", "qualificationScope": "OQ-5.4-1",
                    "evidenceLevel": 4, "agentsRuntimeEnabled": False, "tenantId": m["tenantId"], "targetVersion": self.a,
                    "storageIdentity": m["receiverStorageIdentity"], "receiverPrincipalId": m["receiverPrincipalId"],
                    "lookupPrincipalId": m["lookupPrincipalId"]}
        participants = [{"participantPartyId": m["servicePartyId"], "participantRole": "Member"},
                        {"participantPartyId": "human", "participantRole": "Facilitator"}]
        message = {"messageId": m["messageId"], "text": "synthetic content", "deleted": False,
                   "provenance": {"aiGenerated": True, "agentCallTraceReference": "trace", "humanEdited": False,
                                  "editedByPartyId": None}}
        member = {"outcome": 0, "sourceRevision": 2, "agentParticipantPresent": True, "participants": participants}
        posted = {"outcome": 0, "sourceRevision": 3, "messages": [message]}
        edited = copy.deepcopy(posted)
        edited["messages"][0]["provenance"].update(humanEdited=True, editedByPartyId="human")
        deleted = copy.deepcopy(edited)
        deleted["messages"][0].update(deleted=True, text=None)
        values = {name: {"outcome": 0} for name in OPERATIONS}
        values.update({
            "identity": identity, "party": {"tenantId": m["tenantId"], "partyId": m["servicePartyId"],
                                            "partyType": "Organization", "active": True, "identityRevision": "party-one"},
            "receiverIdentity": receiver, "c3ReceiverIdentity": receiver | {"targetVersion": self.b},
            "c3CurrentBinding": identity | {"receiverTargetVersion": self.b},
            "c1ParticipantReadAfterFirst": member, "c1ParticipantRead": member,
            "c1Post": {"outcome": 0, "messageId": m["messageId"]},
            "c1PostRetry": {"outcome": 0, "messageId": m["messageId"]},
            "c1MessageReadAfterFirst": posted, "c1MessageRead": posted,
            "c1RosterRead": {"outcome": 0, "participants": participants},
            "c1CurrentRead": posted, "c1EditedRead": edited, "c1DeletedRead": deleted,
            "c1Catalogue": {"complete": True, "tenantId": m["tenantId"], "checkpoint": "catalogue-one",
                            "entries": [{"conversationId": "zero", "active": True, "createdAt": "2026-10-22T01:00:00Z"}]},
            "c1ZeroAgentSource": {"conversationId": "zero", "tenantId": m["tenantId"], "isCreated": True,
                                  "isDeleted": False, "agentCallCount": 0, "sourceRevision": 1},
            "c1Count": {"outcome": 0, "count": 1, "createdFromInclusive": m["createdFromInclusive"],
                        "createdToExclusive": m["createdToExclusive"], "catalogCheckpoint": "catalogue-one"},
        })

        def signal(name, revision):
            return {"conversationDeletionSignalId": name + "-signal", "sourceRevision": revision,
                    "sourceStream": m["tenantId"] + ":conversation:" + name, "tenantId": m["tenantId"],
                    "conversationId": name, "approvalReference": "approval-" + name, "sourceContractVersion": 1}

        def receipt(source, target):
            return {"signalId": source["conversationDeletionSignalId"], "sourceRevision": source["sourceRevision"],
                    "protectedDeletionRevision": 1, "targetVersion": target, "evidence": "oq-conformance-v1:synthetic"}

        c2, c3, c4 = signal("c2", 5), signal("c3", 7), signal("c4", 9)
        c2_receipt, c3_receipt = receipt(c2, self.a), receipt(c3, self.b)
        values["c2SourceBefore"] = {"outcome": 0, "signal": c2, "acknowledgement": None}
        values["c2SourceAfter"] = {"outcome": 0, "signal": c2, "acknowledgement": c2_receipt, "acknowledgedSourceRevision": 5}
        values["c2ReceiverLookup"] = {"outcome": 0, "acknowledgement": c2_receipt}
        pending = {"outcome": 0, "signal": c3, "acknowledgement": None, "lastAttemptId": "attempt-c3",
                   "targetVersion": self.a, "acknowledgedSourceRevision": 0}
        for name in ("c3SourcePending", "c3SourceOutage", "c3SourceRestart"):
            values[name] = pending
        values["c3SourceAcknowledged"] = pending | {"targetVersion": self.b, "acknowledgement": c3_receipt, "acknowledgedSourceRevision": 7}
        values["c3ReceiverLookup"] = {"outcome": 0, "acknowledgement": c3_receipt}
        for name, outcome in (("c4CrossTenant", 4), ("c4Revoked", 4), ("c4DeletedRead", 2), ("c4RemovedRead", 3)):
            values[name] = {"outcome": outcome, "messages": None}
        values["c4PoisonSourceBefore"] = {"outcome": 0, "signal": c4, "acknowledgement": None, "acknowledgedSourceRevision": 0}
        values["c4PoisonSourceAfter"] = values["c4PoisonSourceBefore"] | {"outcome": 7, "poisonCode": "source-or-receipt-conflict"}
        values["c4ReceiverLookup"] = {"outcome": 6, "acknowledgement": None}
        responses = {}
        for name, body in values.items():
            if name in RECEIVER_QUERIES:
                body = {"success": True, "payloadBytes": base64.b64encode(json.dumps(body).encode()).decode()}
            responses[name] = {"status": 503 if name == "c3ReceiverOutage" else 409 if name == "c4PoisonInject" else 200,
                               "body": body}
        return responses

    def run_runner(self, suffix="artifacts", interrupt=""):
        self.f.write("manifest.json", self.f.manifest)
        self.f.binding["manifestSha256"] = self.f.digest("manifest.json")
        (self.root / "register.md").write_text(self.f.register(), encoding="utf-8")
        self.f.write("responses.json", self.responses)
        environment = self.environment | {"OQ_TEST_ARTIFACTS": str(self.root / suffix), "OQ_TEST_INTERRUPT": interrupt}
        script = r"""
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function global:git {
    $global:LASTEXITCODE = 0
    if ($args -contains 'rev-parse') { return ('1' * 40) }
    if ($args -contains 'status') { return }
    if ($args -contains 'show') { return Get-Content (Join-Path $env:OQ_TEST_ROOT 'register.md') -Raw }
    throw 'Unexpected synthetic Git request'
}
function global:Invoke-WebRequest {
    param($Uri, $Method, $Headers, $TimeoutSec, $SkipHttpErrorCheck, $ErrorAction, $ContentType, $Body, $MaximumRedirection)
    $request = [Text.Encoding]::UTF8.GetString([byte[]]$Body) | ConvertFrom-Json -AsHashtable -DateKind String
    $operation = $request.testOperation
    [IO.File]::AppendAllText((Join-Path $env:OQ_TEST_ROOT 'requests.txt'), ($operation + "`n"))
    $latest = Get-ChildItem -LiteralPath $env:OQ_TEST_ARTIFACTS -Directory | Sort-Object Name | Select-Object -Last 1
    $current = Get-Content (Join-Path $latest.FullName 'live-evidence.json') -Raw | ConvertFrom-Json -AsHashtable
    if ($current.Gates[-1].Code -cne $operation -or $current.Gates[-1].Result -cne 'PENDING') { throw 'Request had no persisted pending record' }
    if (-not (Test-Path -LiteralPath $current.AttemptJournal)) { throw 'Request preceded attempt reservation' }
    if ($Uri.AbsolutePath -ceq '/query') {
        if ($Headers.Contains('Authorization') -or -not $Headers.Contains('X-Hexalith-Workload-Assertion') -or
            -not $Headers.Contains('dapr-api-token')) { throw 'Wrong receiver authentication headers' }
    }
    if ($MaximumRedirection -ne 0) { throw 'Redirects were enabled' }
    if ($env:OQ_TEST_INTERRUPT -ceq $operation) { [Environment]::Exit(77) }
    $responses = Get-Content (Join-Path $env:OQ_TEST_ROOT 'responses.json') -Raw | ConvertFrom-Json -AsHashtable -DateKind String
    $response = $responses[$operation]
    return @{ StatusCode = $response.status; Content = ($response.body | ConvertTo-Json -Depth 20 -Compress) }
}
try {
    & $env:OQ_TEST_SCRIPT -LiveManifest (Join-Path $env:OQ_TEST_ROOT 'manifest.json') -ArtifactsPath $env:OQ_TEST_ARTIFACTS
} catch { [Console]::Error.Write($_.Exception.Message); exit 1 }
"""
        result = subprocess.run(["pwsh", "-NoProfile", "-NonInteractive", "-Command", script],
                                env=environment, capture_output=True, text=True, timeout=45)
        evidence_paths = sorted((self.root / suffix).glob("*/live-evidence.json"))
        self.assertTrue(evidence_paths, result.stdout + result.stderr)
        evidence = json.loads(evidence_paths[-1].read_text(encoding="utf-8-sig"))
        requests = (self.root / "requests.txt").read_text().splitlines() if (self.root / "requests.txt").exists() else []
        return result, evidence, requests

    def test_synthetic_pipeline_covers_authentication_scope_and_all_lanes(self):
        result, evidence, requests = self.run_runner()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("PASS", evidence["Result"])
        self.assertEqual(["C1", "C2", "C3", "C4"], [lane["Name"] for lane in evidence["Lanes"]])
        self.assertEqual(OPERATIONS, requests)
        self.assertTrue(evidence["AgentsStory83Gated"])

    def test_equivalent_utc_count_serializations_match(self):
        for name in ("createdFromInclusive", "createdToExclusive"):
            self.responses["c1Count"]["body"][name] = self.f.manifest[name].replace("+00:00", "Z")
        result, evidence, _ = self.run_runner()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("PASS", evidence["Result"])

    def test_invalid_window_refuses_before_requests_or_reservation(self):
        self.f.manifest["createdToExclusive"] = self.f.manifest["createdFromInclusive"]
        result, evidence, requests = self.run_runner()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("RUNTIME_WINDOW_INVALID", evidence["Gates"][-1]["Code"])
        self.assertEqual([], requests)
        self.assertFalse(Path(self.f.configuration["attemptJournalDirectory"]).exists())

    def test_wrong_count_window_cannot_pass_c1(self):
        self.responses["c1Count"]["body"]["createdToExclusive"] = "2026-10-24T00:00:00Z"
        result, evidence, _ = self.run_runner()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("C1_PERSISTENCE_MISMATCH", evidence["Gates"][-1]["Code"])

    def test_unmet_prerequisite_makes_zero_requests_and_consumes_no_attempt(self):
        self.f.status = "Uncommitted"
        result, evidence, requests = self.run_runner()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("BLOCKED", evidence["Result"])
        self.assertEqual("OQ_DEPENDENCY_NOT_COMMITTED:EXT-PROTECTION-1", evidence["Gates"][-1]["Code"])
        self.assertEqual([], requests)
        self.assertFalse(Path(self.f.configuration["attemptJournalDirectory"]).exists())

    def test_repeat_attempt_makes_zero_additional_requests(self):
        result, first, before = self.run_runner()
        self.assertEqual(0, result.returncode, result.stderr)
        original = Path(first["AttemptJournal"]).read_bytes()
        result, evidence, after = self.run_runner("different-artifacts")
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("OQ_ATTEMPT_NOT_RESERVED", evidence["Gates"][-1]["Code"])
        self.assertEqual(before, after)
        self.assertEqual(original, Path(first["AttemptJournal"]).read_bytes())

    def test_receiver_scope_failure_precedes_membership(self):
        payload = {"outcome": 0, "agentsRuntimeEnabled": True}
        self.responses["receiverIdentity"]["body"]["payloadBytes"] = base64.b64encode(json.dumps(payload).encode()).decode()
        result, evidence, requests = self.run_runner()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("FAIL", evidence["Result"])
        self.assertNotIn("c1Membership", requests)

    def test_receiver_bare_response_is_not_a_qualified_sdk_result(self):
        self.responses["receiverIdentity"]["body"] = {"outcome": 0}
        result, evidence, requests = self.run_runner()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("SOURCE_QUERY_INVALID", evidence["Gates"][-1]["Code"])
        self.assertNotIn("c1Membership", requests)

    def test_unqualified_rollover_prevents_retry(self):
        body = json.loads(base64.b64decode(self.responses["c3ReceiverIdentity"]["body"]["payloadBytes"]))
        body["agentsRuntimeEnabled"] = True
        self.responses["c3ReceiverIdentity"]["body"]["payloadBytes"] = base64.b64encode(json.dumps(body).encode()).decode()
        result, evidence, requests = self.run_runner()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("RECEIVER_SCOPE_MISMATCH", evidence["Gates"][-1]["Code"])
        self.assertNotIn("c3Retry", requests)

    def test_http_failure_cannot_report_an_available_outcome(self):
        self.responses["c1Membership"]["status"] = 503
        result, evidence, _ = self.run_runner()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("LIVE_HTTP_FAILURE", evidence["Gates"][-1]["Code"])

    def test_interruption_retains_pending_operation_and_original_attempt(self):
        result, evidence, requests = self.run_runner(interrupt="c2Approve")
        self.assertEqual(77, result.returncode, result.stderr)
        self.assertEqual("c2Approve", requests[-1])
        self.assertEqual("PENDING", evidence["Gates"][-1]["Result"])
        self.assertEqual("c2Approve", evidence["Gates"][-1]["Code"])
        self.assertTrue(Path(evidence["AttemptJournal"]).exists())


if __name__ == "__main__":
    unittest.main()
