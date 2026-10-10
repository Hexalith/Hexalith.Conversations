#!/usr/bin/env python3
"""Closed authority check for the Story 9.2 ec5c9be/v5 successor route.

This module only validates authority and committed candidate scope. It does not
substitute for tier execution, fault restoration, or the final-record checks.
The original v3 migration and candidate-environment route remains untouched.
"""

from __future__ import annotations

import hashlib
import importlib.util
import json
import subprocess
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
PREPARER_PATH = Path(__file__).with_name("prepare_story92_successor_v2.py")
SPEC = importlib.util.spec_from_file_location("story92_preparer_v2_acceptance", PREPARER_PATH)
assert SPEC and SPEC.loader
PREPARER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PREPARER)

PROPOSAL = "docs/release-evidence/conformance-oracle-tiering-migration-v5-proposal.json"
PROPOSAL_SHA = "0ce2f8a68fe91bea852bf66220259f6328858525b3ef05dcf86fc0699d90598f"
REVIEW = "docs/release-evidence/conformance-oracle-tiering-migration-review-v5.md"
REVIEW_SHA = "68927548a2934d6fe5dd84f38eb20d867843b3d48f04cd285cd57d16876d3cc0"
APPROVAL = "docs/release-evidence/conformance-oracle-tiering-migration-approval-v5.json"
APPROVAL_SHA = "6a0218ea421ff448dee93e066b6bbc801498cecd28d95e7322a783a97ebbceb3"
BASE = PREPARER.CANDIDATE
ALLOWED_SUCCESSOR_PATHS = frozenset({
    "_bmad-output/implementation-artifacts/spec-9-2-make-the-portable-tier-structural-and-prove-complete-monotonic-tier-execution.md",
    PREPARER.AUTH, PREPARER.SCOPE, PREPARER.SCHEMA,
    "_bmad/scripts/prepare_story92_successor_v2.py",
    "_bmad/scripts/verify_story92_successor_v2_acceptance.py",
    "_bmad/scripts/verify_conformance_tiering.py",
    "_bmad/scripts/generate_story_record.py",
    "_bmad/scripts/tests/test_prepare_story92_successor_v2.py",
    "_bmad/scripts/tests/test_verify_story92_successor_v2_acceptance.py",
    "_bmad/scripts/tests/test_conformance_tiering.py",
    "_bmad/scripts/tests/test_generate_story_record.py",
    "_bmad/schemas/story-final-record-v2.schema.json",
    "docs/runbooks/story-final-record-generation.md",
    PREPARER.API_EVIDENCE, PREPARER.API_COMPAT_EVIDENCE,
    PROPOSAL, REVIEW, APPROVAL,
})


class AcceptanceError(Exception):
    """A specific successor authority or candidate-scope failure."""

    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code


def require(value: Any, code: str, message: str) -> None:
    if not value:
        raise AcceptanceError(code, message)


def sha(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def git(root: Path, *args: str) -> str:
    result = subprocess.run(["git", "-C", str(root), *args], capture_output=True, check=False)
    require(result.returncode == 0, "SUCCESSOR_GIT_INVALID", "Git could not measure the candidate")
    return result.stdout.decode("utf-8").strip()


def candidate_blob(root: Path, candidate: str, path: str) -> bytes | None:
    result = subprocess.run(["git", "-C", str(root), "show", f"{candidate}:{path}"], capture_output=True, check=False)
    return result.stdout if result.returncode == 0 else None


def pinned(root: Path, path: str, digest: str, *, candidate: str | None) -> bytes:
    source = root / path
    require(source.is_file() and not source.is_symlink(), "SUCCESSOR_INPUT_MISSING", path)
    content = source.read_bytes()
    require(sha(content) == digest, "SUCCESSOR_INPUT_DRIFT", path)
    if candidate is not None:
        require(candidate_blob(root, candidate, path) == content,
                "SUCCESSOR_INPUT_NOT_COMMITTED", path)
    return content


def root_gitlinks(root: Path, revision: str) -> list[dict[str, str]]:
    rows = []
    for line in git(root, "ls-tree", revision, "references").splitlines():
        # A tree entry for references itself does not expose its children.
        if line.startswith("160000 "):
            raise AcceptanceError("SUCCESSOR_GIT_INVALID", "references must be a tree")
    for line in git(root, "ls-tree", "-r", revision, "references").splitlines():
        metadata, path = line.split("\t", 1)
        mode, kind, obj = metadata.split()
        if mode == "160000":
            rows.append({"path": path, "mode": mode, "type": kind, "object": obj})
    return sorted(rows, key=lambda row: row["path"])


def touched_paths(root: Path, before: str, after: str) -> set[str]:
    paths: set[str] = set()
    for revision in git(root, "rev-list", "--reverse", f"{before}..{after}").splitlines():
        parents = git(root, "rev-list", "--parents", "-n", "1", revision).split()
        require(len(parents) == 2, "SUCCESSOR_HISTORY_INVALID", "successor history contains a merge")
        paths.update(filter(None, git(root, "diff", "--name-only", parents[1], revision).splitlines()))
    return paths


def validate(root: Path, candidate: str, *, committed: bool = True, require_head: bool = True) -> dict[str, Any]:
    """Bind the exact v2 source authorization and v5 Quality approval.

    ``committed=False`` is for an explicit preparation audit only. Final-record
    generation must use the default and rejects uncommitted authority bytes.
    """
    root = root.resolve()
    if require_head:
        require(git(root, "rev-parse", "HEAD^{commit}") == candidate,
                "SUCCESSOR_CANDIDATE_INVALID", "candidate must be the checkout HEAD")
    require(git(root, "merge-base", BASE, candidate) == BASE,
            "SUCCESSOR_CANDIDATE_INVALID", "candidate does not descend from the approved source")
    try:
        authorization, scope = PREPARER.authority(root)
        _, prior_scope = PREPARER.V1.authority(root)
    except PREPARER.V1.PreparationError as error:
        raise AcceptanceError(error.code, str(error)) from error
    for path, digest in ((PREPARER.V1.SCOPE_PATH, PREPARER.V1.PINS["scopeSha256"]),
                         (PREPARER.V1.AUTHORIZATION_PATH, PREPARER.V1.PINS["authorizationSha256"])):
        pinned(root, path, digest, candidate=candidate if committed else None)
    for path, digest in ((PREPARER.AUTH, PREPARER.AUTH_SHA), (PREPARER.SCOPE, PREPARER.SCOPE_SHA),
                         (PREPARER.API_EVIDENCE, PREPARER.API_EVIDENCE_SHA),
                         (PREPARER.API_COMPAT_EVIDENCE, PREPARER.API_COMPAT_EVIDENCE_SHA)):
        pinned(root, path, digest, candidate=candidate if committed else None)
    try:
        prior_changes = PREPARER.V1.changes(root, PREPARER.V1.PINS["originalCandidate"], PREPARER.PRIOR)
        prior_promotions = PREPARER.V1.promotions(root, PREPARER.V1.PINS["originalCandidate"], PREPARER.PRIOR)
        measured_scope = PREPARER.with_sizes(root, PREPARER.V1.changes(root, PREPARER.PRIOR, BASE))
    except PREPARER.V1.PreparationError as error:
        raise AcceptanceError(error.code, str(error)) from error
    require(prior_changes == prior_scope["committedChangesSinceAcceptedCandidate"]
            and prior_promotions == prior_scope["rootGitlinkPromotionHistorySinceAcceptedCandidate"],
            "SUCCESSOR_SCOPE_DRIFT", "the v1 Git path/object or promotion history differs")
    require(measured_scope == scope["changesSincePriorAuthorizedCandidate"],
            "SUCCESSOR_SCOPE_DRIFT", "the v2 Git path/object changes differ from the authorization")
    revisions = git(root, "rev-list", "--reverse", f"{PREPARER.PRIOR}..{BASE}").splitlines()
    history = []
    for revision in revisions:
        parents = git(root, "rev-list", "--parents", "-n", "1", revision).split()
        require(len(parents) == 2, "SUCCESSOR_SCOPE_DRIFT", "approved v2 history contains a merge")
        history.append({"commit": revision, "parent": parents[1],
                        "subject": git(root, "show", "-s", "--format=%s", revision)})
    require(history == scope["interveningCommits"], "SUCCESSOR_SCOPE_DRIFT", "v2 commit chain differs")
    proposal_raw = pinned(root, PROPOSAL, PROPOSAL_SHA, candidate=candidate if committed else None)
    pinned(root, REVIEW, REVIEW_SHA, candidate=candidate if committed else None)
    approval_raw = pinned(root, APPROVAL, APPROVAL_SHA, candidate=candidate if committed else None)
    proposal, approval = json.loads(proposal_raw), json.loads(approval_raw)
    material = proposal["successorMaterial"]
    require(proposal["proposalSha256"] == sha(PREPARER.V1.canonical(material))
            and proposal["proposalSha256"] == approval["binding"]["proposalSha256"]
            and material["approvedScope"]["proposalSha256"] == PREPARER.SCOPE_MATERIAL_SHA
            and material["currentSourceSnapshot"]["candidate"] == BASE
            and material["currentSourceSnapshot"]["tree"] == git(root, "rev-parse", f"{BASE}^{{tree}}")
            and material["currentSourceSnapshot"]["rootGitlinks"] == root_gitlinks(root, BASE)
            and approval["status"] == "approved" and approval["role"] == "Quality owner"
            and approval["approver"] == "user" and approval["binding"] == proposal["requiredQualityBinding"]
            and approval["acceptanceClaimed"] is False and approval["finalRecordClaimed"] is False,
            "SUCCESSOR_QUALITY_BINDING_INVALID", "v5 Quality material or approved source differs")
    require(approval["reviewedArtifacts"] == [
        {"path": PROPOSAL, "sha256": PROPOSAL_SHA}, {"path": REVIEW, "sha256": REVIEW_SHA}],
        "SUCCESSOR_QUALITY_BINDING_INVALID", "reviewed artifact bytes differ")
    require(approval["binding"]["releasedEventStoreApiDiffSha256"] == PREPARER.API_EVIDENCE_SHA
            and approval["binding"]["releasedEventStoreApiCompatSha256"] == PREPARER.API_COMPAT_EVIDENCE_SHA,
            "SUCCESSOR_QUALITY_BINDING_INVALID", "released API evidence is detached")
    require(root_gitlinks(root, candidate) == root_gitlinks(root, BASE),
            "SUCCESSOR_SCOPE_DRIFT", "root gitlinks changed after the v2 authorized source")
    paths = touched_paths(root, BASE, candidate)
    require(paths <= ALLOWED_SUCCESSOR_PATHS,
            "SUCCESSOR_SCOPE_DRIFT", "unapproved successor paths: " + ", ".join(sorted(paths - ALLOWED_SUCCESSOR_PATHS)))
    return {"route": "story-9.2-successor-v2", "candidate": candidate,
            "approvedSource": BASE, "scopeProposalSha256": PREPARER.SCOPE_MATERIAL_SHA,
            "proposalSha256": proposal["proposalSha256"], "migrationProposalSha256": material["proposedMigrationSha256"],
            "approval": {"path": APPROVAL, "sha256": APPROVAL_SHA},
            "apiDiffSha256": PREPARER.API_EVIDENCE_SHA,
            "apiCompatSha256": PREPARER.API_COMPAT_EVIDENCE_SHA,
            "committed": committed}
