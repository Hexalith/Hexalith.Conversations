#!/usr/bin/env python3
"""Bind the exact Story 9.2 v4 scope and v7 Quality decision.

The preparation audit accepts uncommitted decision files for review. Acceptance
requires their exact bytes in the committed candidate and fresh tier execution.
"""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
from typing import Any


SPEC = importlib.util.spec_from_file_location(
    "story92_v3_scope_for_v4", Path(__file__).with_name("verify_story92_successor_v3_scope.py"))
assert SPEC and SPEC.loader
V3 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(V3)
V2 = V3.V2
AcceptanceError = V2.AcceptanceError
require = V2.require

BASE = "353e9dbf47a1472b29fe3f43d485a8ae3a03b884"
SCOPE = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-proposal-v4.json"
SCOPE_SHA = "0939819cbe661a25abcb8746dd85a7cecf2fdfd1ed33a957d1265d42257930b1"
SCOPE_MATERIAL_SHA = "f5dfa7f71b93bbb799559bbddef69464cd63d2a49dc8ffb127323c2e5e8f1c44"
REVIEW = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-review-v4.md"
REVIEW_SHA = "bc1b0ef24f49e31501104310b16ef9853483ddc0a30ec5bf3f73eec48c5865b2"
AUTH = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-authorization-v4.json"
AUTH_SHA = "a848504c12f4c638190657621470d78ad9a0c330f980017a4e93593afc21ef10"
QUALITY_PROPOSAL = "docs/release-evidence/conformance-oracle-tiering-migration-v7-proposal.json"
QUALITY_PROPOSAL_SHA = "b03b79cc3477442ee2035c3a1554f73a73384ddb3610fb60ffdfabe20d51e4bd"
QUALITY_MATERIAL_SHA = "f23e3d3d005b82be76dbdc825cc7abf6da5e65cd98ef37a65674c035c4f7ec7b"
QUALITY_REVIEW = "docs/release-evidence/conformance-oracle-tiering-migration-review-v7.md"
QUALITY_REVIEW_SHA = "14d3388713ef6c28fc2d6245f938d970f5ea584b8c086579024fe2c724e88967"
QUALITY_APPROVAL = "docs/release-evidence/conformance-oracle-tiering-migration-approval-v7.json"
QUALITY_APPROVAL_SHA = "8acb6e58c34a0de0931651e2597afdad1bd958a8be8a75fca616fcd56e770389"
API_DIFF = "docs/release-evidence/conformance-oracle-tiering-eventstore-released-api-diff-v2.json"
API_DIFF_SHA = "b47142a1282841ee1d55afcfbccd33dc61498c5b1a12e2f40fabc2023d126850"
API_COMPAT = "docs/release-evidence/conformance-oracle-tiering-eventstore-released-api-compat-v2.json"
API_COMPAT_SHA = "6bc586e1e1ba670ab1baf0a30a42fea7f89ebca9b7119e0d0a54cb4264c7be95"
ALLOWED_LATER_PATHS = V3.ALLOWED_LATER_PATHS | frozenset({
    AUTH, SCOPE, REVIEW, QUALITY_PROPOSAL, QUALITY_REVIEW, QUALITY_APPROVAL,
    API_DIFF, API_COMPAT,
    "_bmad/scripts/verify_story92_successor_v4_acceptance.py",
    "_bmad/scripts/tests/test_verify_story92_successor_v4_acceptance.py",
})


def require_pair_only_history(root: Path, before: str, candidate: str) -> None:
    """Keep every later Story 9.2 record change in its own paired commit.

    The final-record generator independently verifies the published pair's
    schema, candidate binding, and Markdown bytes.
    """
    pair = V3.RETAINED_RECORD_PAIR
    for revision in V2.git(root, "rev-list", "--reverse", f"{before}..{candidate}").splitlines():
        parents = V2.git(root, "rev-list", "--parents", "-n", "1", revision).split()
        require(len(parents) == 2, "SUCCESSOR_SCOPE_DRIFT", "post-v4 pair history contains a merge")
        parent = parents[1]
        tokens = [token for token in V2.git(root, "diff", "--name-status", "--no-renames", "-z",
                                              parent, revision).split("\0") if token]
        require(len(tokens) % 2 == 0, "SUCCESSOR_SCOPE_DRIFT", "post-v4 path status is malformed")
        delta = dict(zip(tokens[1::2], tokens[::2]))
        if set(delta) & pair:
            statuses = set(delta.values())
            require(set(delta) == pair and statuses in ({"A"}, {"M"}, {"D"}),
                    "SUCCESSOR_SCOPE_DRIFT", "Story 9.2 record change must be a pair-only commit")
            retained = revision if statuses != {"D"} else parent
            absent = parent if statuses == {"A"} else revision if statuses == {"D"} else None
            require(all(V2.candidate_blob(root, retained, path) is not None for path in pair)
                    and (absent is None or all(V2.candidate_blob(root, absent, path) is None for path in pair)),
                    "SUCCESSOR_SCOPE_DRIFT", "Story 9.2 record pair has an invalid publication or retraction")


def bound(path: str, digest: str) -> dict[str, str]:
    return {"path": path, "sha256": digest}


def authority(root: Path, candidate: str, *, committed: bool = True,
              require_head: bool = True) -> dict[str, Any]:
    """Remeasure v1 through v4 history and the exact user scope decision.

    ``require_head=False`` is for an explicit historical preparation audit.
    Candidate acceptance uses the HEAD-bound default.
    """
    root = root.resolve()
    if require_head:
        require(V2.git(root, "rev-parse", "HEAD^{commit}") == candidate,
                "SUCCESSOR_CANDIDATE_INVALID", "candidate must be checkout HEAD")
    require(V2.git(root, "merge-base", BASE, candidate) == BASE,
            "SUCCESSOR_CANDIDATE_INVALID", "candidate does not descend from the v4 source")
    V3.validate(root, V3.BASE, committed=False, require_head=False)
    for path, digest in ((SCOPE, SCOPE_SHA), (REVIEW, REVIEW_SHA), (AUTH, AUTH_SHA),
                         (V3.QUALITY_PROPOSAL, V3.QUALITY_PROPOSAL_SHA),
                         (V3.QUALITY_REVIEW, V3.QUALITY_REVIEW_SHA),
                         (V3.QUALITY_APPROVAL, V3.QUALITY_APPROVAL_SHA)):
        V2.pinned(root, path, digest, candidate=candidate if committed else None)
    scope = json.loads((root / SCOPE).read_bytes())
    authorization = json.loads((root / AUTH).read_bytes())
    material = {key: value for key, value in scope.items() if key != "proposalSha256"}
    require(scope["schemaVersion"] == "hexalith.conversations.story-9.2-current-main-scope-proposal.v4"
            and scope["status"] == "prepared-unapproved"
            and scope["proposalSha256"] == SCOPE_MATERIAL_SHA == V2.sha(V2.PREPARER.V1.canonical(material))
            and scope["priorAuthorizedCandidate"] == V3.BASE
            and scope["measuredCommittedCandidate"] == BASE
            and scope["measuredCommittedTree"] == V2.git(root, "rev-parse", f"{BASE}^{{tree}}")
            and scope["priorQualityApproval"] == {
                **bound(V3.QUALITY_APPROVAL, V3.QUALITY_APPROVAL_SHA),
                "sourceCandidate": V3.BASE, "coversMeasuredCandidate": False}
            and not any(scope[key] for key in ("authorizationClaimed", "qualityApprovalClaimedForNewScope",
                                                "candidateAcceptanceClaimed", "workingTreeChangesIncluded")),
            "SUCCESSOR_SCOPE_INVALID", "v4 measured scope differs")
    require(authorization == {
        "schemaVersion": "hexalith.conversations.story-9.2-current-main-scope-authorization.v4",
        "storyId": "9.2", "status": "authorized", "authorization": authorization.get("authorization"),
        "proposal": {**bound(SCOPE, SCOPE_SHA), "proposalSha256": SCOPE_MATERIAL_SHA},
        "review": bound(REVIEW, REVIEW_SHA), "measuredCommittedCandidate": BASE,
        "priorAuthorizedCandidate": V3.BASE,
        "originalBaseline": "51aa06b856bcb0aaf22013153cfe02a51b046156",
        "workingTreeChangesIncluded": False, "qualityDecisionIncluded": False,
        "preserveOriginalBaselineFrozenContractsApprovalsAndAcceptedRecords": True,
    } and authorization["authorization"]["actor"] == "user"
      and authorization["authorization"]["statement"] == "I approve, apply recommended",
      "SUCCESSOR_AUTHORIZATION_INVALID", "v4 decision differs from the exact user response")
    try:
        changes = V2.PREPARER.with_sizes(root, V2.PREPARER.V1.changes(root, V3.BASE, BASE))
        promotions = V2.PREPARER.V1.promotions(root, V3.BASE, BASE)
    except V2.PREPARER.V1.PreparationError as error:
        raise AcceptanceError(error.code, str(error)) from error
    revisions = V2.git(root, "rev-list", "--reverse", f"{V3.BASE}..{BASE}").splitlines()
    history = []
    for revision in revisions:
        parents = V2.git(root, "rev-list", "--parents", "-n", "1", revision).split()
        require(len(parents) == 2, "SUCCESSOR_SCOPE_DRIFT", "v4 history contains a merge")
        history.append({"commit": revision, "parent": parents[1],
                        "subject": V2.git(root, "show", "-s", "--format=%s", revision)})
    require(changes == scope["changesSincePriorAuthorizedCandidate"]
            and promotions == scope["rootGitlinkPromotionHistorySincePriorAuthorizedCandidate"]
            and history == scope["interveningCommits"]
            and scope["counts"] == {"changedPaths": 28, "changedRootGitlinks": 7,
                                    "interveningCommits": 6, "owningPromotionCommits": 1,
                                    "productionPathsChanged": 0},
            "SUCCESSOR_SCOPE_DRIFT", "v4 Git history differs")
    # Compare the complete net gitlink set and the single owning promotion.
    expected_gitlinks = [{"path": row["path"], "object": row["object"]}
                         for row in V2.root_gitlinks(root, BASE)]
    require(scope["rootGitlinks"]["after"] == expected_gitlinks
            and len(promotions) == 1 and len(promotions[0]["gitlinks"]) == 7,
            "SUCCESSOR_SCOPE_DRIFT", "v4 root gitlinks differ")
    require(V2.root_gitlinks(root, candidate) == V2.root_gitlinks(root, BASE),
            "SUCCESSOR_SCOPE_DRIFT", "root gitlinks changed after v4 source")
    require(V2.touched_paths(root, BASE, candidate) <= ALLOWED_LATER_PATHS | V3.RETAINED_RECORD_PAIR,
            "SUCCESSOR_SCOPE_DRIFT", "post-v4 path outside the closed evidence/tooling set")
    require_pair_only_history(root, BASE, candidate)
    package_pin = V2.candidate_blob(root, candidate, "Directory.Packages.props")
    require(package_pin is not None and b"HexalithEventStoreVersion" not in package_pin,
            "SUCCESSOR_ENVIRONMENT_DRIFT", "root package override differs")
    catalog = scope["effectivePackageCatalog"]
    builds = root / "references/Hexalith.Builds"
    require(catalog["ownerGitlink"] == next(row["object"] for row in V2.root_gitlinks(root, candidate)
                                            if row["path"] == "references/Hexalith.Builds")
            and V2.git(builds, "rev-parse", "HEAD^{commit}") == catalog["ownerGitlink"]
            and V2.git(builds, "rev-parse", "HEAD:Props/Directory.Packages.props") == catalog["blobObject"]
            and V2.sha(V2.PREPARER.V1.git(builds, "cat-file", "blob", catalog["blobObject"])) == catalog["sha256"]
            and catalog["eventStoreVersion"] == "3.119.0",
            "SUCCESSOR_ENVIRONMENT_DRIFT", "effective released EventStore catalog differs")
    return {"route": "story-9.2-successor-v4", "candidate": candidate, "approvedSource": BASE,
            "scopeAuthorization": bound(AUTH, AUTH_SHA),
            "scopeProposal": {**bound(SCOPE, SCOPE_SHA), "proposalSha256": SCOPE_MATERIAL_SHA},
            "rootGitlinks": V2.root_gitlinks(root, candidate)}


def approved_quality(root: Path, candidate: str, *, committed: bool = True) -> dict[str, str]:
    """Validate the digest-bound v7 user Quality decision."""
    root = root.resolve()
    for path, digest in ((API_DIFF, API_DIFF_SHA), (API_COMPAT, API_COMPAT_SHA),
                         (QUALITY_PROPOSAL, QUALITY_PROPOSAL_SHA),
                         (QUALITY_REVIEW, QUALITY_REVIEW_SHA),
                         (QUALITY_APPROVAL, QUALITY_APPROVAL_SHA)):
        V2.pinned(root, path, digest, candidate=candidate if committed else None)
    proposal = json.loads((root / QUALITY_PROPOSAL).read_bytes())
    approval = json.loads((root / QUALITY_APPROVAL).read_bytes())
    material = proposal["successorMaterial"]
    require(proposal["schemaVersion"] == "hexalith.conversations.conformance-oracle-tiering-migration.v7"
            and proposal["status"] == "scope-and-quality-review-pending"
            and proposal["proposalSha256"] == QUALITY_MATERIAL_SHA
            == V2.sha(V2.PREPARER.V1.canonical(material))
            and material["pendingV4Scope"]["proposal"] == {
                **bound(SCOPE, SCOPE_SHA), "proposalSha256": SCOPE_MATERIAL_SHA}
            and material["currentSourceSnapshot"]["candidate"] == BASE
            and material["currentSourceSnapshot"]["tree"] == V2.git(root, "rev-parse", f"{BASE}^{{tree}}")
            and material["releasedEventStoreApi"]["diff"] == bound(API_DIFF, API_DIFF_SHA)
            and material["releasedEventStoreApi"]["sdkApiCompat"] == bound(API_COMPAT, API_COMPAT_SHA)
            and proposal["requiredQualityBinding"]["sourceSnapshotSha256"]
            == V2.sha(V2.PREPARER.V1.canonical(material["currentSourceSnapshot"]))
            and proposal["requiredQualityBinding"]["migrationProposalSha256"]
            == material["migration"]["proposalSha256"]
            and proposal["requiredQualityBinding"]["releasedEventStoreApiDiffSha256"] == API_DIFF_SHA
            and proposal["requiredQualityBinding"]["releasedEventStoreApiCompatSha256"] == API_COMPAT_SHA
            and not any(proposal[key] for key in ("scopeAuthorizationClaimed", "qualityApprovalClaimed",
                                                    "candidateAcceptanceClaimed")),
            "SUCCESSOR_QUALITY_BINDING_INVALID", "v7 proposal material differs")
    require(approval == {
        "schemaVersion": "hexalith.conversations.conformance-oracle-tiering-migration-approval.v7",
        "status": "approved", "role": "Quality owner", "approver": "user",
        "approvalId": approval.get("approvalId"), "approvedOn": approval.get("approvedOn"),
        "evidence": approval.get("evidence"),
        "reviewedArtifacts": [bound(QUALITY_PROPOSAL, QUALITY_PROPOSAL_SHA),
                              bound(QUALITY_REVIEW, QUALITY_REVIEW_SHA)],
        "binding": proposal["requiredQualityBinding"],
        "acceptanceClaimed": False, "finalRecordClaimed": False,
    } and all(isinstance(approval[key], str) and approval[key].strip()
              and "SYNTHETIC-FIXTURE" not in approval[key].upper()
              for key in ("approvalId", "approvedOn", "evidence")),
            "SUCCESSOR_QUALITY_BINDING_INVALID", "v7 decision differs from approved packet")
    return bound(QUALITY_APPROVAL, QUALITY_APPROVAL_SHA)
