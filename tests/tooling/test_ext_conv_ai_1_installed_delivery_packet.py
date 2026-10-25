"""Prove the sealed EXT-CONV-AI-1 replacement packet stays NotSubmitted."""

import hashlib
import json
from pathlib import Path
import unittest


EVIDENCE = Path("/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1")
DELIVERY = EVIDENCE / "delivery-20261025-8039d2a"
PACKET = DELIVERY / "replacement-acceptance-packet.json"
MANIFEST = DELIVERY / "live-manifest.json"
JOURNAL = EVIDENCE / "attempt-journal"
CANONICAL = "8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3"
TWELVE = (
    "configuration",
    "plan",
    "hostProfile",
    "hostVerifierEvidence",
    "sourceAnchor",
    "partyAnchor",
    "approvalAnchor",
    "workerAnchor",
    "receiverAnchor",
    "lookupAnchor",
    "observerAnchor",
    "controlAnchor",
)
BINDINGS = (
    "liveManifest",
    "servicePartyEnrollment",
    "workerMachineBinding",
    "approvalIssuer",
    "commandSourceAuthority",
    "sourcePublicationAuthority",
    "hostDeliveryTransport",
    "conformanceReceiver",
    "independentLookup",
    "retainedOwnerAttemptJournal",
)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_json(path):
    def pairs(items):
        value = {}
        for key, item in items:
            if key in value:
                raise ValueError(f"duplicate key: {key}")
            value[key] = item
        return value

    return json.loads(path.read_text(), object_pairs_hook=pairs)


class InstalledDeliveryPacketTests(unittest.TestCase):
    def setUp(self):
        self.packet = load_json(PACKET)

    def test_missing_inputs_stay_null_and_packet_is_not_submitted(self):
        replacement = self.packet["replacement"]
        files = replacement["runtimeQualificationBinding"]["files"]
        self.assertEqual(self.packet["status"], "NotSubmitted")
        self.assertFalse(self.packet["packetComplete"])
        self.assertFalse(self.packet["accepted"])
        self.assertFalse(self.packet["available"])
        self.assertEqual(replacement["status"], "NotSubmitted")
        self.assertEqual(replacement["targetIntegrationDate"], "2026-10-25")
        self.assertEqual(replacement["historicalMissedIntegrationDate"], "2026-10-08")
        self.assertIn("Six core V1 seams", replacement["requiredArtifact"])
        self.assertEqual(set(files), set(TWELVE))
        for name in TWELVE:
            self.assertIsNone(files[name]["path"])
            self.assertIsNone(files[name]["sha256"])
        self.assertEqual(set(self.packet["installedBindings"]), set(BINDINGS))
        for name in BINDINGS:
            self.assertIsNone(self.packet["installedBindings"][name])
        manifest_hash = sha256(MANIFEST)
        self.assertEqual(
            replacement["runtimeQualificationBinding"]["manifestSha256"],
            manifest_hash,
        )
        self.assertIn(CANONICAL, replacement["verificationCommand"])
        self.assertEqual(load_json(MANIFEST)["targetCommit"], CANONICAL)

    def test_absent_receiver_blocks_separation_claim(self):
        separation = self.packet["separation"]
        self.assertIsNone(separation["installedReceiver"])
        self.assertIsNone(separation["restoreGraph"])
        self.assertIsNone(separation["receiverPrincipalId"])
        self.assertIsNone(separation["lookupPrincipalId"])
        self.assertIsNone(separation["principalsDistinct"])
        self.assertIsNone(separation["agentsPackageIncluded"])
        self.assertFalse(separation["sourceTreeCountedAsInstallation"])
        self.assertIn("RECEIVER_NOT_INSTALLED", self.packet["notSubmittedBecause"])

    def test_qualification_gate_reserves_nothing(self):
        verification = self.packet["verification"]
        self.assertFalse(verification["cohortExecuted"])
        self.assertFalse(verification["attemptReserved"])
        self.assertFalse(verification["liveCommandExecuted"])
        self.assertFalse(self.packet["attemptReserved"])
        self.assertEqual(self.packet["attemptJournalEntries"], [])
        self.assertEqual(self.packet["attemptJournalDirectory"], str(JOURNAL))
        self.assertTrue(JOURNAL.is_dir())
        self.assertEqual(list(JOURNAL.iterdir()), [])
        for lane in ("C1", "C2", "C3", "C4"):
            self.assertEqual(verification[lane], "NOT_RUN")
        self.assertFalse(self.packet["prerequisites"]["hostLivePassPathQualified"])
        self.assertFalse(self.packet["prerequisites"]["passPathManufactured"])
        self.assertEqual(self.packet["prerequisites"]["protectionAcceptedStatus"], "Uncommitted")
        retained = {
            EVIDENCE / "delivery-20261022-71fab44/replacement-acceptance-packet.blocked.json":
                "579b59180e13df164757204113bae20f3c15a07d0867432631b5a1cb2ad023df",
            EVIDENCE / "delivery-20261025-ed53473/replacement-acceptance-packet.blocked.json":
                "20530c84523119b21e609d7e4dbdb95abe08aff867b58097dfa51105cac76932",
        }
        for path, digest in retained.items():
            self.assertEqual(sha256(path), digest)
