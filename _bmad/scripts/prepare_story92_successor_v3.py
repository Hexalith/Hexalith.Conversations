#!/usr/bin/env python3
"""Prepare the exact fbe2f50/v3 Story 9.2 Quality request, without approval."""

from __future__ import annotations

import argparse
import importlib.util
import json
import subprocess
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "story92_v3_scope_for_preparation", Path(__file__).with_name("verify_story92_successor_v3_scope.py"))
assert SPEC and SPEC.loader
V3 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(V3)
V2 = V3.V2
OUT = "docs/release-evidence/conformance-oracle-tiering-migration-v6-proposal.json"
REVIEW = "docs/release-evidence/conformance-oracle-tiering-migration-review-v6.md"
CLIENT_DIFF = ["dnx", "dotnet-inspect", "-y", "--", "diff", "--package",
               "Hexalith.EventStore.Client@3.117.1..3.118.0", "--table"]


def verifier() -> Any:
    path = Path(__file__).with_name("verify_conformance_tiering.py")
    spec = importlib.util.spec_from_file_location("story92_v3_tiering", path)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def canonical(value: Any) -> bytes:
    return V2.PREPARER.V1.canonical(value)


def digest(value: Any) -> str:
    return V2.sha(canonical(value))


def bound(path: str, sha: str) -> dict[str, str]:
    return {"path": path, "sha256": sha}


def validate_packet(root: Path, document: dict[str, Any]) -> dict[str, Any]:
    """Re-derive the complete migration and authority bound by a pending packet."""
    root = root.resolve()
    V3.validate(root, V2.git(root, "rev-parse", "HEAD^{commit}"), committed=False)
    prior_raw = V2.pinned(root, V2.PROPOSAL, V2.PROPOSAL_SHA, candidate=None)
    V2.pinned(root, V3.AUTH, V3.AUTH_SHA, candidate=None)
    prior = json.loads(prior_raw)
    derived = verifier().derive_migration(root, current_tree=True)
    prior_migration = prior["successorMaterial"]["proposedMigration"]
    prior_snapshot = prior["successorMaterial"]["currentSourceSnapshot"]
    V3.require(derived["proposal"] == prior_migration
               and derived["proposalSha256"] == prior["successorMaterial"]["proposedMigrationSha256"],
               "SUCCESSOR_MIGRATION_DRIFT", "complete live migration differs from the exact v5 proposal")
    material = document["successorMaterial"]
    source = material["currentSourceSnapshot"]
    rows = [[row["id"], row["rowSha256"]] for row in derived["proposal"]["changedAssertions"]]
    expected_scope = {"authorization": bound(V3.AUTH, V3.AUTH_SHA),
                      "proposal": {**bound(V3.SCOPE, V3.SCOPE_SHA), "proposalSha256": V3.SCOPE_MATERIAL_SHA},
                      "review": bound(V3.REVIEW, V3.REVIEW_SHA), "priorV1V2HistoryVerified": True}
    V3.require(material["approvedV3Scope"] == expected_scope
               and material["priorQualityProposal"] == bound(V2.PROPOSAL, V2.PROPOSAL_SHA)
               and material["priorQualityApproval"] == {**bound(V2.APPROVAL, V2.APPROVAL_SHA),
                                                         "sourceCandidate": V3.PRIOR, "coversV3Scope": False}
               and source["candidate"] == V3.BASE
               and source["tree"] == V2.git(root, "rev-parse", f"{V3.BASE}^{{tree}}")
               and source["rootGitlinks"] == V2.root_gitlinks(root, V3.BASE)
               and source["sourceFilesSha256"] == digest(prior_snapshot["sourceFiles"])
               and source["actualProductionSurfaceSha256"] == digest(prior_snapshot["actualProductionSurface"])
               and source["scopeProposalSha256"] == V3.SCOPE_MATERIAL_SHA
               and source["packagePin"] == {"path": "Directory.Packages.props",
                                            "sha256": V2.sha(V2.candidate_blob(root, V3.BASE, "Directory.Packages.props")),
                                            "eventStoreVersion": "3.118.0"}
               and material["migration"] == {"proposalSha256": derived["proposalSha256"],
                                             "equalsPriorV5Migration": True, "changedAssertionRows": rows,
                                             "publicDriftSha256": derived["proposal"]["publicSurface"]["driftSha256"],
                                             "frozenExecutedCaseFloor": 415,
                                             "liveControls": len(derived["proposal"]["liveControls"])}
               and document["proposalSha256"] == digest(material),
               "SUCCESSOR_BINDING_INVALID", "v6 source, scope, or migration material differs")
    api = material["releasedEventStoreApi"]
    confirmation = api["clientConfirmation"]
    expected_findings = {"client": "one binary-breaking constructor change and 28 additive changes across 25 types",
                         "contracts": "97 additive changes across 97 types in the prior exact receipt",
                         "domainService": "BothIncomplete metadata inspection before and after",
                         "serviceDefaults": "BothIncomplete metadata inspection before and after",
                         "sdkClient": "CP0002 for removed five-parameter constructor",
                         "sdkDomainServiceAndServiceDefaults": "RunApiCompat target passed",
                         "completeAdditiveReview": False}
    V3.require(api["releasedDiff"] == bound(V2.PREPARER.API_EVIDENCE, V2.PREPARER.API_EVIDENCE_SHA)
               and api["sdkApiCompat"] == bound(V2.PREPARER.API_COMPAT_EVIDENCE, V2.PREPARER.API_COMPAT_EVIDENCE_SHA)
               and api["versions"] == {"before": "3.117.1", "after": "3.118.0"}
               and api["findings"] == expected_findings
               and confirmation["command"] == CLIENT_DIFF and confirmation["exitCode"] == 0
               and "1 breaking, 28 additive across 25 types" in confirmation["stdout"]
               and confirmation["stdoutSha256"] == V2.sha(confirmation["stdout"].encode())
               and confirmation["stderrSha256"] == V2.sha(confirmation["stderr"].encode())
               and document["schemaVersion"] == "hexalith.conversations.conformance-oracle-tiering-migration.v6",
               "SUCCESSOR_API_EVIDENCE_INVALID", "v6 released-package evidence differs")
    binding = {"role": "Quality owner", "proposalSha256": digest(material),
               "scopeProposalSha256": V3.SCOPE_MATERIAL_SHA,
               "sourceSnapshotSha256": digest(source),
               "migrationProposalSha256": derived["proposalSha256"],
               "changedAssertionRows": rows,
               "publicDriftSha256": derived["proposal"]["publicSurface"]["driftSha256"],
               "releasedEventStoreApiDiffSha256": V2.PREPARER.API_EVIDENCE_SHA,
               "releasedEventStoreApiCompatSha256": V2.PREPARER.API_COMPAT_EVIDENCE_SHA}
    V3.require(document["requiredQualityBinding"] == binding
               and document["status"] == "quality-review-pending"
               and document["preparationOnly"] is True
               and document["qualityApprovalClaimed"] is False
               and document["acceptanceClaimed"] is False
               and document["acceptance"] == {"state": "blocked", "blockers": [
                   "SUCCESSOR_QUALITY_DECISION_REQUIRED", "FRESH_CANDIDATE_ACCEPTANCE_REQUIRED"]},
               "SUCCESSOR_QUALITY_BINDING_INVALID", "v6 Quality binding or pending state differs")
    return binding


def prepare(root: Path) -> dict[str, Any]:
    root = root.resolve()
    V3.require(V2.git(root, "rev-parse", "HEAD^{commit}") == V3.BASE,
               "SUCCESSOR_CANDIDATE_INVALID", "v3 Quality preparation requires the exact authorized fbe2f50 source")
    scope = V3.validate(root, V3.BASE, committed=False)
    V3.require(V2.candidate_blob(root, V3.BASE, "Directory.Packages.props")
               == (root / "Directory.Packages.props").read_bytes(),
               "SUCCESSOR_SOURCE_DIRTY", "working package pin differs from the fbe2f50 commit")
    tier = verifier()
    derived = tier.derive_migration(root, current_tree=True)
    tier.approved_migration(root, derived, current_tree=True, require_approval=False)
    old = json.loads((root / V2.PROPOSAL).read_bytes())
    old_material = old["successorMaterial"]
    proposal = derived["proposal"]
    rows = [[row["id"], row["rowSha256"]] for row in proposal["changedAssertions"]]
    old_rows = [[row["id"], row["rowSha256"]] for row in old_material["proposedMigration"]["changedAssertions"]]
    V3.require(proposal == old_material["proposedMigration"]
               and derived["proposalSha256"] == old_material["proposedMigrationSha256"]
               and rows == old_rows and len(rows) == 14
               and proposal["publicSurface"]["driftSha256"]
               == old_material["proposedMigration"]["publicSurface"]["driftSha256"],
               "SUCCESSOR_MIGRATION_DRIFT", "migration, changed rows, or public drift differ from the prior Quality material")
    tree = V2.PREPARER.V1.tree(root, V3.BASE)
    old_files = old_material["currentSourceSnapshot"]["sourceFiles"]
    for row in old_files:
        entry = tree.get(row["path"])
        V3.require(entry is not None and entry["object"] == row["object"]
                   and V2.sha(V2.PREPARER.V1.git(root, "cat-file", "blob", entry["object"])) == row["sha256"],
                   "SUCCESSOR_SOURCE_DRIFT", "committed production source differs: " + row["path"])
    V3.require({path for path in tree if path.startswith("src/") and tree[path]["type"] == "blob"}
               == {row["path"] for row in old_files},
               "SUCCESSOR_SOURCE_DRIFT", "production source file set differs")
    surface = tier.TIERING.surface(tier.TIERING.WorkTree(root))
    V3.require(surface == old_material["currentSourceSnapshot"]["actualProductionSurface"],
               "SUCCESSOR_SOURCE_DRIFT", "production source surface differs")
    source = {"candidate": V3.BASE, "tree": V2.git(root, "rev-parse", f"{V3.BASE}^{{tree}}"),
              "rootGitlinks": V2.root_gitlinks(root, V3.BASE),
              "sourceFilesSha256": digest(old_files), "actualProductionSurfaceSha256": digest(surface),
              "packagePin": {"path": "Directory.Packages.props",
                             "sha256": V2.sha(V2.candidate_blob(root, V3.BASE, "Directory.Packages.props")),
                             "eventStoreVersion": "3.118.0"},
              "scopeProposalSha256": V3.SCOPE_MATERIAL_SHA}
    V3.require(len(source["rootGitlinks"]) == 10 and source["tree"] != old_material["currentSourceSnapshot"]["tree"],
               "SUCCESSOR_SOURCE_DRIFT", "v3 source tree or root gitlinks were not freshly measured")
    command = CLIENT_DIFF
    probe = subprocess.run(command, cwd=root, capture_output=True, check=False, text=True, timeout=120)
    V3.require(probe.returncode == 0 and "1 breaking, 28 additive across 25 types" in probe.stdout,
               "SUCCESSOR_API_EVIDENCE_INVALID", "released Client API diff differs")
    api = {"releasedDiff": bound(V2.PREPARER.API_EVIDENCE, V2.PREPARER.API_EVIDENCE_SHA),
           "sdkApiCompat": bound(V2.PREPARER.API_COMPAT_EVIDENCE, V2.PREPARER.API_COMPAT_EVIDENCE_SHA),
           "versions": {"before": "3.117.1", "after": "3.118.0"},
           "clientConfirmation": {"command": command, "exitCode": probe.returncode,
                                  "stdout": probe.stdout, "stdoutSha256": V2.sha(probe.stdout.encode()),
                                  "stderr": probe.stderr, "stderrSha256": V2.sha(probe.stderr.encode())},
           "findings": {"client": "one binary-breaking constructor change and 28 additive changes across 25 types",
                        "contracts": "97 additive changes across 97 types in the prior exact receipt",
                        "domainService": "BothIncomplete metadata inspection before and after",
                        "serviceDefaults": "BothIncomplete metadata inspection before and after",
                        "sdkClient": "CP0002 for removed five-parameter constructor",
                        "sdkDomainServiceAndServiceDefaults": "RunApiCompat target passed",
                        "completeAdditiveReview": False}}
    material = {"approvedV3Scope": {"authorization": bound(V3.AUTH, V3.AUTH_SHA),
                                     "proposal": {**bound(V3.SCOPE, V3.SCOPE_SHA),
                                                  "proposalSha256": V3.SCOPE_MATERIAL_SHA},
                                     "review": bound(V3.REVIEW, V3.REVIEW_SHA),
                                     "priorV1V2HistoryVerified": True},
                "priorQualityApproval": {**bound(V2.APPROVAL, V2.APPROVAL_SHA),
                                         "sourceCandidate": V3.PRIOR, "coversV3Scope": False},
                "priorQualityProposal": bound(V2.PROPOSAL, V2.PROPOSAL_SHA),
                "currentSourceSnapshot": source,
                "migration": {"proposalSha256": derived["proposalSha256"],
                              "equalsPriorV5Migration": True, "changedAssertionRows": rows,
                              "publicDriftSha256": proposal["publicSurface"]["driftSha256"],
                              "frozenExecutedCaseFloor": 415, "liveControls": len(proposal["liveControls"])},
                "releasedEventStoreApi": api}
    material_sha = digest(material)
    binding = {"role": "Quality owner", "proposalSha256": material_sha,
               "scopeProposalSha256": V3.SCOPE_MATERIAL_SHA,
               "sourceSnapshotSha256": digest(source),
               "migrationProposalSha256": derived["proposalSha256"],
               "changedAssertionRows": rows,
               "publicDriftSha256": proposal["publicSurface"]["driftSha256"],
               "releasedEventStoreApiDiffSha256": V2.PREPARER.API_EVIDENCE_SHA,
               "releasedEventStoreApiCompatSha256": V2.PREPARER.API_COMPAT_EVIDENCE_SHA}
    result = {"schemaVersion": "hexalith.conversations.conformance-oracle-tiering-migration.v6",
              "storyId": "9.2", "status": "quality-review-pending", "preparationOnly": True,
              "qualityApprovalClaimed": False, "acceptanceClaimed": False,
              "successorMaterial": material, "proposalSha256": material_sha,
              "requiredQualityBinding": binding,
              "acceptance": {"state": "blocked", "blockers": ["SUCCESSOR_QUALITY_DECISION_REQUIRED",
                                                               "FRESH_CANDIDATE_ACCEPTANCE_REQUIRED"]}}
    validate_packet(root, result)
    return result


def render_review(document: dict[str, Any]) -> bytes:
    material = document["successorMaterial"]
    binding = document["requiredQualityBinding"]
    source = material["currentSourceSnapshot"]
    api = material["releasedEventStoreApi"]
    lines = ["# Story 9.2 successor Quality review v6", "",
             "Preparation only. The v3 scope authorization does not approve new Quality material or candidate acceptance.", "",
             f"- Authorized source commit: `{source['candidate']}`; tree: `{source['tree']}`.",
             f"- V3 scope authorization SHA-256: `{V3.AUTH_SHA}`; proposal material: `{V3.SCOPE_MATERIAL_SHA}`.",
             f"- New source snapshot SHA-256: `{binding['sourceSnapshotSha256']}`.",
             f"- New proposal material SHA-256: `{document['proposalSha256']}`.",
             f"- Prior v5 Quality approval SHA-256: `{V2.APPROVAL_SHA}`; it covers only `{V3.PRIOR}`.", "",
             "## Exact root gitlinks", "", "| Path | Commit |", "| --- | --- |"]
    lines += [f"| `{row['path']}` | `{row['object']}` |" for row in source["rootGitlinks"]]
    lines += ["", "## Migration and released API", "",
              f"Fresh migration digest: `{binding['migrationProposalSha256']}`; equal to the exact v5 migration.",
              f"All {len(binding['changedAssertionRows'])} ordered changed-row digests and public drift "
              f"`{binding['publicDriftSha256']}` are unchanged; the 415-case floor and three controls remain required.",
              f"Released EventStore API diff SHA-256: `{api['releasedDiff']['sha256']}`; SDK API compatibility "
              f"receipt SHA-256: `{api['sdkApiCompat']['sha256']}`.",
              "The repeated Client 3.117.1→3.118.0 diff reports one breaking constructor signature and 28 additive "
              "changes across 25 types. Contracts has 97 additive changes across 97 types in the prior exact receipt. "
              "DomainService and ServiceDefaults remain BothIncomplete under dotnet-inspect because metadata "
              "inspection failed before and after. The .NET 10 RunApiCompat receipt reports Client CP0002 for "
              "the removed five-parameter marker-store constructor; DomainService and ServiceDefaults pass that "
              "target. Complete additive API review is not claimed.", "",
              "## Exact Quality decision requested", "", "```json", json.dumps(binding, indent=2), "```", "",
              "A genuine Quality owner must review and bind this new source/scope material. Fresh committed-candidate "
              "AC01–10, restored fault receipts, final pair, and insertion verification remain required. No Quality "
              "approval or acceptance is claimed.", ""]
    return "\n".join(lines).encode()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=ROOT)
    parser.add_argument("--validate", action="store_true", help="Verify existing pending bytes without rewriting them.")
    args = parser.parse_args()
    root = args.repository.resolve()
    if args.validate:
        packet = json.loads((root / OUT).read_bytes())
        validate_packet(root, packet)
        V3.require((root / REVIEW).read_bytes() == render_review(packet),
                   "SUCCESSOR_BINDING_INVALID", "v6 review differs from the pending packet")
        print(f"VERIFIED: {OUT} {V2.sha((root / OUT).read_bytes())}; Quality decision pending")
        return 0
    result = prepare(root)
    proposal = (json.dumps(result, indent=2, ensure_ascii=False) + "\n").encode()
    review = render_review(result)
    (root / OUT).write_bytes(proposal)
    (root / REVIEW).write_bytes(review)
    print(f"PREPARED: {OUT} {V2.sha(proposal)}; {REVIEW} {V2.sha(review)}; Quality decision pending")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
