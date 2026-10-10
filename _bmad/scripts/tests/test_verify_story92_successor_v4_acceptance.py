"""Focused current-candidate guards for Story 9.2 v4 authority and v7 Quality."""

from __future__ import annotations

import importlib.util
import json
import subprocess
from pathlib import Path

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location(
    "story92_v4_acceptance_test", ROOT / "_bmad/scripts/verify_story92_successor_v4_acceptance.py")
assert SPEC and SPEC.loader
V4 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(V4)


def test_exact_v4_scope_and_quality_pass_preparation_audit() -> None:
    scope = V4.authority(ROOT, V4.BASE, committed=False, require_head=False)
    approval = V4.approved_quality(ROOT, V4.BASE, committed=False)
    assert scope["scopeProposal"]["proposalSha256"] == V4.SCOPE_MATERIAL_SHA
    assert len(scope["rootGitlinks"]) == 10
    assert approval["sha256"] == V4.QUALITY_APPROVAL_SHA


def test_uncommitted_v4_decision_cannot_accept_candidate() -> None:
    assert V4.V2.candidate_blob(ROOT, V4.BASE, V4.AUTH) is None
    with pytest.raises(V4.AcceptanceError) as failure:
        V4.V2.pinned(ROOT, V4.AUTH, V4.AUTH_SHA, candidate=V4.BASE)
    assert failure.value.code == "SUCCESSOR_INPUT_NOT_COMMITTED"


def test_v4_scope_rejects_changed_promotion_history(monkeypatch: pytest.MonkeyPatch) -> None:
    original = V4.V2.PREPARER.V1.promotions
    monkeypatch.setattr(V4.V2.PREPARER.V1, "promotions",
                        lambda root, before, after: [] if (before, after) == (V4.V3.BASE, V4.BASE)
                        else original(root, before, after))
    with pytest.raises(V4.AcceptanceError) as failure:
        V4.authority(ROOT, V4.BASE, committed=False, require_head=False)
    assert failure.value.code == "SUCCESSOR_SCOPE_DRIFT"


def test_v7_quality_binding_includes_new_api_receipts() -> None:
    assert V4.approved_quality(ROOT, V4.BASE, committed=False)["sha256"] == V4.QUALITY_APPROVAL_SHA
    proposal = json.loads((ROOT / V4.QUALITY_PROPOSAL).read_bytes())
    assert proposal["requiredQualityBinding"]["scopeProposalSha256"] == V4.SCOPE_MATERIAL_SHA
    assert proposal["requiredQualityBinding"]["releasedEventStoreApiDiffSha256"] == V4.API_DIFF_SHA
    assert proposal["requiredQualityBinding"]["releasedEventStoreApiCompatSha256"] == V4.API_COMPAT_SHA


def test_v4_environment_is_separate_closed_record_shape() -> None:
    schema = json.loads((ROOT / "_bmad/schemas/story-final-record-v2.schema.json").read_bytes())
    validator = jsonschema.Draft202012Validator(schema).evolve(
        schema={"$defs": schema["$defs"], "$ref": "#/$defs/candidateEnvironmentV4"})
    scope = V4.authority(ROOT, V4.BASE, committed=False, require_head=False)
    environment = {"route": scope["route"], "approvedSourceCommit": scope["approvedSource"],
                   "scopeAuthorization": scope["scopeAuthorization"],
                   "scopeProposal": scope["scopeProposal"],
                   "qualityApproval": {"path": V4.QUALITY_APPROVAL, "sha256": V4.QUALITY_APPROVAL_SHA},
                   "rootGitlinks": scope["rootGitlinks"]}
    assert not list(validator.iter_errors(environment))
    environment["scopeProposal"]["proposalSha256"] = V4.V3.SCOPE_MATERIAL_SHA
    assert list(validator.iter_errors(environment))


def test_later_pair_history_requires_pair_only_commits(tmp_path: Path) -> None:
    repository = tmp_path / "history"
    repository.mkdir()

    def git(*args: str) -> str:
        return subprocess.run(["git", "-C", str(repository), *args], check=True,
                              capture_output=True, text=True).stdout.strip()

    git("init", "-q")
    git("config", "user.name", "Story 9.2 Test")
    git("config", "user.email", "story92@example.invalid")
    source = repository / "source.txt"
    source.write_text("original\n")
    git("add", "source.txt")
    git("commit", "-qm", "test: establish source")
    base = git("rev-parse", "HEAD")

    for path in V4.V3.RETAINED_RECORD_PAIR:
        target = repository / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text("paired record\n")
    git("add", "-A")
    git("commit", "-qm", "test: publish pair")
    published = git("rev-parse", "HEAD")
    V4.require_pair_only_history(repository, base, published)

    for path in V4.V3.RETAINED_RECORD_PAIR:
        (repository / path).unlink()
    git("add", "-A")
    git("commit", "-qm", "test: retract pair")
    retracted = git("rev-parse", "HEAD")
    V4.require_pair_only_history(repository, base, retracted)

    for path in V4.V3.RETAINED_RECORD_PAIR:
        (repository / path).write_text("replacement record\n")
    source.write_text("changed\n")
    git("add", "-A")
    git("commit", "-qm", "test: mix pair and source")
    with pytest.raises(V4.AcceptanceError) as failure:
        V4.require_pair_only_history(repository, base, git("rev-parse", "HEAD"))
    assert failure.value.code == "SUCCESSOR_SCOPE_DRIFT"
