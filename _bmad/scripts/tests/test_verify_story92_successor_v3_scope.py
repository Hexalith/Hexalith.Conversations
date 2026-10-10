"""Exact v3 preparation authority and pending v6 Quality binding."""

from __future__ import annotations

import importlib.util
import json
import shutil
import subprocess
from pathlib import Path

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]


def load(name: str, path: str):
    specification = importlib.util.spec_from_file_location(name, ROOT / path)
    assert specification and specification.loader
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


V3 = load("story92_v3_scope_test", "_bmad/scripts/verify_story92_successor_v3_scope.py")
TIER = load("story92_v3_tier_test", "_bmad/scripts/verify_conformance_tiering.py")
PREPARE = load("story92_v3_packet_test", "_bmad/scripts/prepare_story92_successor_v3.py")


@pytest.fixture(scope="module")
def authorized_source(tmp_path_factory: pytest.TempPathFactory) -> Path:
    checkout = tmp_path_factory.mktemp("story92-v3-authority") / "checkout"
    subprocess.run(["git", "-C", str(ROOT), "worktree", "add", "--detach", str(checkout), V3.BASE],
                   check=True, capture_output=True)
    try:
        for relative in (V3.SCOPE, V3.REVIEW, V3.AUTH, V3.V2.APPROVAL):
            destination = checkout / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, destination)
        yield checkout
    finally:
        subprocess.run(["git", "-C", str(ROOT), "worktree", "remove", "--force", str(checkout)],
                       check=True, capture_output=True)


def test_exact_v3_history_and_six_promotions_pass_preparation_audit(authorized_source: Path) -> None:
    binding = V3.validate(authorized_source, V3.BASE, committed=False)
    assert binding["scopeProposal"]["proposalSha256"] == V3.SCOPE_MATERIAL_SHA
    assert binding["priorQualityApproval"]["coversNewScope"] is False
    assert len(binding["rootGitlinks"]) == 10


def test_uncommitted_authority_cannot_accept_candidate(authorized_source: Path) -> None:
    with pytest.raises(V3.AcceptanceError) as error:
        V3.validate(authorized_source, V3.BASE)
    assert error.value.code == "SUCCESSOR_INPUT_NOT_COMMITTED"


def test_v3_authorization_byte_change_rejected(authorized_source: Path) -> None:
    target = authorized_source / V3.AUTH
    original = target.read_bytes()
    try:
        target.write_bytes(original + b" ")
        with pytest.raises(V3.AcceptanceError) as error:
            V3.validate(authorized_source, V3.BASE, committed=False)
        assert error.value.code == "SUCCESSOR_INPUT_DRIFT"
    finally:
        target.write_bytes(original)


def test_intermediate_promotion_history_is_required(authorized_source: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    original = V3.V2.PREPARER.V1.promotions
    monkeypatch.setattr(V3.V2.PREPARER.V1, "promotions",
                        lambda root, before, after: [] if (before, after) == (V3.PRIOR, V3.BASE)
                        else original(root, before, after))
    with pytest.raises(V3.AcceptanceError) as error:
        V3.validate(authorized_source, V3.BASE, committed=False)
    assert error.value.code == "SUCCESSOR_SCOPE_DRIFT"


def test_verifier_leaves_new_quality_gate_closed(authorized_source: Path) -> None:
    report = TIER.verify_authorized_successor_v3_scope(authorized_source, V3.BASE)
    assert report["result"] == "FAIL"
    assert report["blockers"] == [{"code": "SUCCESSOR_INPUT_NOT_COMMITTED",
                                    "message": V3.V2.APPROVAL}]


def test_v6_packet_binds_authorized_source_and_prior_api_evidence() -> None:
    proposal = json.loads((ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-v6-proposal.json").read_bytes())
    material = proposal["successorMaterial"]
    binding = proposal["requiredQualityBinding"]
    assert proposal["proposalSha256"] == V3.V2.sha(V3.V2.PREPARER.V1.canonical(material))
    assert material["approvedV3Scope"]["authorization"] == {"path": V3.AUTH, "sha256": V3.AUTH_SHA}
    assert material["currentSourceSnapshot"]["tree"] == V3.V2.git(ROOT, "rev-parse", f"{V3.BASE}^{{tree}}")
    assert material["currentSourceSnapshot"]["rootGitlinks"] == V3.V2.root_gitlinks(ROOT, V3.BASE)
    assert binding["sourceSnapshotSha256"] == V3.V2.sha(
        V3.V2.PREPARER.V1.canonical(material["currentSourceSnapshot"]))
    assert binding["releasedEventStoreApiDiffSha256"] == V3.V2.PREPARER.API_EVIDENCE_SHA
    assert binding["releasedEventStoreApiCompatSha256"] == V3.V2.PREPARER.API_COMPAT_EVIDENCE_SHA
    assert len(binding["changedAssertionRows"]) == 14
    assert proposal["qualityApprovalClaimed"] is False and proposal["acceptanceClaimed"] is False
    # The retained packet belongs to fbe2f50. Current main has seven later
    # gitlink promotions, which the historical validator must reject.
    with pytest.raises(PREPARE.V3.AcceptanceError) as error:
        PREPARE.validate_packet(ROOT, proposal)
    assert error.value.code == "SUCCESSOR_SCOPE_DRIFT"


def test_v6_packet_rejects_a_change_outside_row_digests(monkeypatch: pytest.MonkeyPatch) -> None:
    proposal = json.loads((ROOT / PREPARE.OUT).read_bytes())
    actual = PREPARE.verifier().derive_migration(ROOT, current_tree=True)
    prior = json.loads((ROOT / V3.V2.PROPOSAL).read_bytes())["successorMaterial"]
    actual["proposal"] = prior["proposedMigration"]
    actual["proposalSha256"] = prior["proposedMigrationSha256"]
    actual["proposal"]["declarations"]["completionInventory"]["unreviewed"] = "change"
    # Isolate the migration check from the separately exercised current-main
    # scope drift; the mutation must still fail against the approved v6 bytes.
    monkeypatch.setattr(PREPARE.V3, "validate", lambda *_args, **_kwargs: {})
    monkeypatch.setattr(PREPARE, "verifier", lambda: type("ChangedVerifier", (), {"derive_migration":
                                                        staticmethod(lambda *_args, **_kwargs: actual)})())
    with pytest.raises(PREPARE.V3.AcceptanceError) as error:
        PREPARE.validate_packet(ROOT, proposal)
    assert error.value.code == "SUCCESSOR_MIGRATION_DRIFT"


def test_v3_record_environment_has_separate_closed_schema() -> None:
    schema = json.loads((ROOT / "_bmad/schemas/story-final-record-v2.schema.json").read_bytes())
    validator = jsonschema.Draft202012Validator(schema).evolve(
        schema={"$defs": schema["$defs"], "$ref": "#/$defs/candidateEnvironmentV3"})
    environment = {"route": "story-9.2-successor-v3", "approvedSourceCommit": V3.BASE,
                   "scopeAuthorization": {"path": V3.AUTH, "sha256": V3.AUTH_SHA},
                   "scopeProposal": {"path": V3.SCOPE, "sha256": V3.SCOPE_SHA,
                                     "proposalSha256": V3.SCOPE_MATERIAL_SHA},
                   "qualityApproval": {"path": V3.QUALITY_APPROVAL, "sha256": "a" * 64},
                   "rootGitlinks": V3.V2.root_gitlinks(ROOT, V3.BASE)}
    assert not list(validator.iter_errors(environment))
    environment["approvedSourceCommit"] = V3.PRIOR
    assert list(validator.iter_errors(environment))


def test_fault_source_inputs_include_v3_authority_and_preparer() -> None:
    paths = TIER.fault_source_paths(ROOT)
    assert "_bmad/scripts/verify_story92_successor_v3_scope.py" in paths
    assert "_bmad/scripts/prepare_story92_successor_v3.py" in paths
    assert V3.AUTH in paths and V3.SCOPE in paths
    assert "docs/release-evidence/conformance-oracle-tiering-migration-v6-proposal.json" in paths


def test_v3_authority_code_and_packet_bytes_invalidate_fault_receipt(tmp_path: Path) -> None:
    paths = TIER.fault_source_paths(ROOT)
    for relative in paths:
        destination = tmp_path / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, destination)
    original = TIER.fault_source_digest(tmp_path)
    assert original == TIER.fault_source_digest(ROOT)
    for relative in ("_bmad/scripts/verify_story92_successor_v3_scope.py",
                     "docs/release-evidence/conformance-oracle-tiering-migration-v6-proposal.json"):
        target = tmp_path / relative
        content = target.read_bytes()
        try:
            target.write_bytes(content + b" ")
            assert TIER.fault_source_digest(tmp_path) != original
        finally:
            target.write_bytes(content)
