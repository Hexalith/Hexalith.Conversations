#!/usr/bin/env python3
"""Exact preparation authority for the Story 9.2 fbe2f50 successor scope.

This grants no Quality approval or candidate acceptance. The v1/v2 and original
v3 final-record routes retain their own checks.
"""

from __future__ import annotations

import importlib.util
import json
import re
from pathlib import Path
from typing import Any


SPEC = importlib.util.spec_from_file_location(
    "story92_v2_authority_for_v3", Path(__file__).with_name("verify_story92_successor_v2_acceptance.py"))
assert SPEC and SPEC.loader
V2 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(V2)

SCOPE = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-proposal-v3.json"
SCOPE_SHA = "30cb277b1b549c13a7c06e380f5477f01843f9c665ce70855c21f28703c54811"
SCOPE_MATERIAL_SHA = "81b73f5642f6fff1f155d0f4bdce6b76dfad25a7a80e5b8991bdc3df53253745"
REVIEW = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-review-v3.md"
REVIEW_SHA = "52120291bb78df35b0ae8fe1eebf294172d2da0f4380b215a118eec2ecc08b9a"
AUTH = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-authorization-v3.json"
AUTH_SHA = "54f3193f21d05ce6a769e0f6e6a9071091fbff60844f65f3b07fcae32ee9efe9"
BASE = "fbe2f502eed26df45edc12e4a12e9917bf97b438"
PRIOR = V2.BASE
ALLOWED_LATER_PATHS = V2.ALLOWED_SUCCESSOR_PATHS | frozenset({
    AUTH, SCOPE, REVIEW,
    "_bmad/scripts/verify_story92_successor_v3_scope.py",
    "_bmad/scripts/prepare_story92_successor_v3.py",
    "_bmad/scripts/tests/test_verify_story92_successor_v3_scope.py",
    "docs/release-evidence/conformance-oracle-tiering-migration-v6-proposal.json",
    "docs/release-evidence/conformance-oracle-tiering-migration-review-v6.md",
    "docs/release-evidence/conformance-oracle-tiering-migration-approval-v6.json",
})
RETAINED_RECORD_PAIR = frozenset({
    "docs/release-evidence/story-9.2-final-record-v2.json",
    "docs/release-evidence/story-9.2-final-record-v2.md",
})
QUALITY_PROPOSAL = "docs/release-evidence/conformance-oracle-tiering-migration-v6-proposal.json"
QUALITY_PROPOSAL_SHA = "f07cf3dfbe526efad3a8965f6fe2b210b3e9368d852725c484ffc08018137503"
QUALITY_REVIEW = "docs/release-evidence/conformance-oracle-tiering-migration-review-v6.md"
QUALITY_REVIEW_SHA = "605c735f78fd3127dd4440d739ec2d66af4e3d7b422e14dd80bd55f60a45b6c9"
QUALITY_APPROVAL = "docs/release-evidence/conformance-oracle-tiering-migration-approval-v6.json"
# Set only after the user gives a Quality decision on the exact v6 packet.
QUALITY_APPROVAL_SHA: str | None = "dc66a8610bd6483ee6d8ad50e7f1d6dbe496c6f85e84ebf1b0ab1785cc75e57d"

AcceptanceError = V2.AcceptanceError
require = V2.require


def validate(root: Path, candidate: str, *, committed: bool = True) -> dict[str, Any]:
    """Prove exact v1/v2/v3 committed history and constrain later tooling changes."""
    root = root.resolve()
    require(V2.git(root, "rev-parse", "HEAD^{commit}") == candidate,
            "SUCCESSOR_CANDIDATE_INVALID", "candidate must be the checkout HEAD")
    require(V2.git(root, "merge-base", BASE, candidate) == BASE,
            "SUCCESSOR_CANDIDATE_INVALID", "candidate does not descend from the authorized v3 source")
    # The prior validator proves the entire v1/v2 Git path/object and promotion
    # history, not merely the net source tree at ec5c9be.
    prior = V2.validate(root, PRIOR, committed=False, require_head=False)
    require(prior["approvedSource"] == PRIOR, "SUCCESSOR_SCOPE_DRIFT", "v2 scope differs")
    for path, digest in ((V2.PREPARER.V1.SCOPE_PATH, V2.PREPARER.V1.PINS["scopeSha256"]),
                         (V2.PREPARER.V1.AUTHORIZATION_PATH, V2.PREPARER.V1.PINS["authorizationSha256"]),
                         (V2.PREPARER.AUTH, V2.PREPARER.AUTH_SHA), (V2.PREPARER.SCOPE, V2.PREPARER.SCOPE_SHA),
                         (V2.PREPARER.API_EVIDENCE, V2.PREPARER.API_EVIDENCE_SHA),
                         (V2.PREPARER.API_COMPAT_EVIDENCE, V2.PREPARER.API_COMPAT_EVIDENCE_SHA),
                         (V2.PROPOSAL, V2.PROPOSAL_SHA), (V2.REVIEW, V2.REVIEW_SHA),
                         (V2.APPROVAL, V2.APPROVAL_SHA),
                         (SCOPE, SCOPE_SHA), (REVIEW, REVIEW_SHA), (AUTH, AUTH_SHA)):
        V2.pinned(root, path, digest, candidate=candidate if committed else None)
    scope = json.loads((root / SCOPE).read_bytes())
    authority = json.loads((root / AUTH).read_bytes())
    require(scope["schemaVersion"] == "hexalith.conversations.story-9.2-current-main-scope-proposal.v3"
            and scope["status"] == "prepared-unapproved"
            and scope["authorizationClaimed"] is False
            and scope["qualityApprovalClaimedForNewScope"] is False
            and scope["workingTreeChangesIncluded"] is False
            and scope["priorAuthorizedCandidate"] == PRIOR
            and scope["measuredCommittedCandidate"] == BASE
            and scope["existingQualityApproval"] == {
                "path": V2.APPROVAL, "sha256": V2.APPROVAL_SHA,
                "sourceCandidate": PRIOR, "coversNewScope": False}
            and scope["proposalSha256"] == SCOPE_MATERIAL_SHA
            and V2.sha(V2.PREPARER.V1.canonical({k: v for k, v in scope.items() if k != "proposalSha256"}))
            == SCOPE_MATERIAL_SHA,
            "SUCCESSOR_SCOPE_INVALID", "v3 proposal identity or material differs")
    require(authority["schemaVersion"] == "hexalith.conversations.story-9.2-current-main-scope-authorization.v3"
            and authority["status"] == "authorized"
            and authority["authorization"]["actor"] == "user"
            and authority["authorization"]["statement"] == "authorize exact v3 scop"
            and authority["proposal"] == {"path": SCOPE, "sha256": SCOPE_SHA,
                                           "proposalSha256": SCOPE_MATERIAL_SHA}
            and authority["review"] == {"path": REVIEW, "sha256": REVIEW_SHA}
            and authority["measuredCommittedCandidate"] == BASE
            and authority["priorAuthorizedCandidate"] == PRIOR
            and authority["workingTreeChangesIncluded"] is False
            and authority["qualityDecisionIncluded"] is False,
            "SUCCESSOR_AUTHORIZATION_INVALID", "v3 decision differs from exact preparation-only scope")
    try:
        changed = V2.PREPARER.with_sizes(root, V2.PREPARER.V1.changes(root, PRIOR, BASE))
        promotions = V2.PREPARER.V1.promotions(root, PRIOR, BASE)
    except V2.PREPARER.V1.PreparationError as error:
        raise AcceptanceError(error.code, str(error)) from error
    history = []
    for revision in V2.git(root, "rev-list", "--reverse", f"{PRIOR}..{BASE}").splitlines():
        parents = V2.git(root, "rev-list", "--parents", "-n", "1", revision).split()
        require(len(parents) == 2, "SUCCESSOR_SCOPE_DRIFT", "v3 history contains a merge")
        history.append({"commit": revision, "parent": parents[1],
                        "subject": V2.git(root, "show", "-s", "--format=%s", revision)})
    gitlink_rows = [row for row in changed if any(row[key] and row[key]["mode"] == "160000"
                                                  for key in ("before", "after"))]
    require(changed == scope["changesSincePriorAuthorizedCandidate"]
            and history == scope["interveningCommits"]
            and history == [{"commit": BASE, "parent": PRIOR,
                             "subject": "fix: add release evidence for EventStore API compatibility and migration review"}]
            and scope["counts"] == {"changedPaths": 17, "changedRootGitlinks": 6, "interveningCommits": 1}
            and len(changed) == 17 and len(gitlink_rows) == 6
            and promotions == [{"commit": BASE, "parent": PRIOR, "gitlinks":
                                V2.PREPARER.V1.changes(root, PRIOR, BASE)[-6:]}],
            "SUCCESSOR_SCOPE_DRIFT", "v3 path/object, commit, or promotion history differs")
    require(V2.root_gitlinks(root, candidate) == V2.root_gitlinks(root, BASE),
            "SUCCESSOR_SCOPE_DRIFT", "root gitlinks changed after the authorized v3 source")
    later = V2.touched_paths(root, BASE, candidate)
    require(later <= ALLOWED_LATER_PATHS | RETAINED_RECORD_PAIR, "SUCCESSOR_SCOPE_DRIFT",
            "unapproved post-v3 paths: " + ", ".join(sorted(later - ALLOWED_LATER_PATHS - RETAINED_RECORD_PAIR)))
    for revision in V2.git(root, "rev-list", "--reverse", f"{BASE}..{candidate}").splitlines():
        parent = V2.git(root, "rev-parse", f"{revision}^1")
        changed_paths = set(filter(None, V2.git(root, "diff", "--name-only", parent, revision).splitlines()))
        if changed_paths & RETAINED_RECORD_PAIR:
            require(changed_paths == RETAINED_RECORD_PAIR
                    and all(V2.candidate_blob(root, parent, path) is not None for path in RETAINED_RECORD_PAIR)
                    and all(V2.candidate_blob(root, revision, path) is None for path in RETAINED_RECORD_PAIR),
                    "SUCCESSOR_SCOPE_DRIFT", "the historical final record must be retracted as a pair-only commit")
    package_pin = V2.candidate_blob(root, candidate, "Directory.Packages.props")
    require(package_pin is not None and b"<HexalithEventStoreVersion>3.118.0</HexalithEventStoreVersion>" in package_pin,
            "SUCCESSOR_ENVIRONMENT_DRIFT", "candidate released EventStore pin differs")
    return {"route": "story-9.2-successor-v3-scope", "candidate": candidate,
            "approvedSource": BASE, "priorApprovedSource": PRIOR,
            "scopeAuthorization": {"path": AUTH, "sha256": AUTH_SHA},
            "scopeProposal": {"path": SCOPE, "sha256": SCOPE_SHA,
                              "proposalSha256": SCOPE_MATERIAL_SHA},
            "priorQualityApproval": {"path": V2.APPROVAL, "sha256": V2.APPROVAL_SHA,
                                     "coversNewScope": False},
            "rootGitlinks": V2.root_gitlinks(root, candidate), "committed": committed}


def approved_quality(root: Path, candidate: str) -> dict[str, Any]:
    """Consume a later genuine v6 decision only after its exact digest is pinned."""
    root = root.resolve()
    require(QUALITY_APPROVAL_SHA is not None, "SUCCESSOR_QUALITY_DECISION_REQUIRED",
            "the exact v6 Quality approval digest has not been authorized and pinned")
    proposal_raw = V2.pinned(root, QUALITY_PROPOSAL, QUALITY_PROPOSAL_SHA, candidate=candidate)
    review_raw = V2.pinned(root, QUALITY_REVIEW, QUALITY_REVIEW_SHA, candidate=candidate)
    approval_raw = V2.pinned(root, QUALITY_APPROVAL, QUALITY_APPROVAL_SHA, candidate=candidate)
    spec = importlib.util.spec_from_file_location(
        "story92_v3_packet_validation", Path(__file__).with_name("prepare_story92_successor_v3.py"))
    assert spec and spec.loader
    packet_module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(packet_module)
    proposal = json.loads(proposal_raw)
    try:
        packet_module.validate_packet(root, proposal)
    except packet_module.V3.AcceptanceError as error:
        raise AcceptanceError(error.code, str(error)) from error
    require(review_raw == packet_module.render_review(proposal),
            "SUCCESSOR_QUALITY_BINDING_INVALID", "v6 review differs from its proposal")
    approval = json.loads(approval_raw)
    require(approval == {
        "schemaVersion": "hexalith.conversations.conformance-oracle-tiering-migration-approval.v6",
        "status": "approved", "role": "Quality owner", "approver": "user",
        "approvalId": approval.get("approvalId"), "approvedOn": approval.get("approvedOn"),
        "evidence": approval.get("evidence"),
        "reviewedArtifacts": [bound(QUALITY_PROPOSAL, QUALITY_PROPOSAL_SHA),
                              bound(QUALITY_REVIEW, QUALITY_REVIEW_SHA)],
        "binding": proposal["requiredQualityBinding"],
        "acceptanceClaimed": False, "finalRecordClaimed": False,
    } and all(isinstance(approval.get(key), str) and approval[key].strip()
              and "SYNTHETIC-FIXTURE" not in approval[key].upper()
              for key in ("approvalId", "approvedOn", "evidence")),
            "SUCCESSOR_QUALITY_BINDING_INVALID", "v6 Quality decision differs from exact approved material")
    require(re.fullmatch(r"\d{4}-\d{2}-\d{2}", approval["approvedOn"]) is not None,
            "SUCCESSOR_QUALITY_BINDING_INVALID", "v6 Quality decision has no ISO date")
    return {"path": QUALITY_APPROVAL, "sha256": QUALITY_APPROVAL_SHA}


def bound(path: str, sha: str) -> dict[str, str]:
    return {"path": path, "sha256": sha}
