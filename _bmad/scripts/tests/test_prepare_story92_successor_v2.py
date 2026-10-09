"""Hermetic checks for the additive, preparation-only Story 9.2 v2 route."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "_bmad/scripts/prepare_story92_successor_v2.py"
SPEC = importlib.util.spec_from_file_location("story92_successor_v2", SCRIPT)
assert SPEC and SPEC.loader
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


def copied_authority(tmp_path: Path) -> Path:
    for relative in (MODULE.AUTH, MODULE.SCOPE):
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / relative).read_bytes())
    return tmp_path


def test_exact_v2_authorization_is_preparation_only(tmp_path: Path) -> None:
    decision, scope = MODULE.authority(copied_authority(tmp_path))
    assert decision["measuredCommittedCandidate"] == MODULE.CANDIDATE
    assert decision["qualityDecisionIncluded"] is False
    assert scope["proposalSha256"] == MODULE.SCOPE_MATERIAL_SHA
    assert scope["counts"] == {"changedPaths": 46, "changedRootGitlinks": 7, "interveningCommits": 8}


@pytest.mark.parametrize("relative,code", [
    (MODULE.AUTH, "SUCCESSOR_AUTHORIZATION_INVALID"),
    (MODULE.SCOPE, "SUCCESSOR_SCOPE_INVALID"),
])
def test_mutated_or_missing_closed_input_rejects(tmp_path: Path, relative: str, code: str) -> None:
    root = copied_authority(tmp_path)
    target = root / relative
    before = target.read_bytes()
    target.write_bytes(before + b" ")
    with pytest.raises(MODULE.V1.PreparationError) as error:
        MODULE.authority(root)
    assert error.value.code == code
    target.write_bytes(before)
    assert target.read_bytes() == before
    target.unlink()
    with pytest.raises(MODULE.V1.PreparationError) as error:
        MODULE.authority(root)
    assert error.value.code == "SUCCESSOR_INPUT_MISSING"


def test_synthetic_decision_rejects_even_when_test_repins_bytes(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    root = copied_authority(tmp_path)
    target = root / MODULE.AUTH
    decision = json.loads(target.read_bytes())
    decision["authorization"]["actor"] = "fixture"
    target.write_bytes(MODULE.V1.json_bytes(decision))
    monkeypatch.setattr(MODULE, "AUTH_SHA", MODULE.V1.sha(target.read_bytes()))
    with pytest.raises(MODULE.V1.PreparationError) as error:
        MODULE.authority(root)
    assert error.value.code == "SUCCESSOR_AUTHORIZATION_INVALID"


def test_wrong_candidate_rejects_before_consuming_source(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    _, scope = MODULE.authority(copied_authority(tmp_path / "authority"))
    monkeypatch.setattr(MODULE, "CANDIDATE", "0" * 40)
    with pytest.raises(MODULE.V1.PreparationError) as error:
        MODULE.validate_source(ROOT, scope, tmp_path / "authority")
    assert error.value.code == "SUCCESSOR_CANDIDATE_INVALID"


def test_pending_document_schema_and_render_are_deterministic() -> None:
    document = json.loads((ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-v5-proposal.json").read_bytes())
    schema = json.loads((ROOT / MODULE.SCHEMA).read_bytes())
    jsonschema.Draft202012Validator(schema).validate(document)
    material = document["successorMaterial"]
    assert document["qualityApprovalClaimed"] is False
    assert document["acceptanceClaimed"] is False
    assert document["proposalSha256"] == MODULE.V1.sha(MODULE.V1.canonical(material))
    assert material["currentSourceSnapshot"]["candidate"] == MODULE.CANDIDATE
    review = (ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-review-v5.md").read_bytes()
    assert MODULE.render(document) == review == MODULE.render(document)


def test_proposal_cannot_claim_quality_approval_or_acceptance() -> None:
    document = json.loads((ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-v5-proposal.json").read_bytes())
    schema = json.loads((ROOT / MODULE.SCHEMA).read_bytes())
    validator = jsonschema.Draft202012Validator(schema)
    for field in ("qualityApprovalClaimed", "acceptanceClaimed"):
        altered = dict(document)
        altered[field] = True
        assert list(validator.iter_errors(altered)), field


def test_released_package_api_evidence_binds_exact_results() -> None:
    binding = MODULE.released_api_evidence(ROOT)
    evidence = json.loads((ROOT / MODULE.API_EVIDENCE).read_bytes())
    assert binding == {
        "path": MODULE.API_EVIDENCE,
        "sha256": MODULE.API_EVIDENCE_SHA,
        "state": "incomplete-quality-review-required",
        "toolVersion": "0.26.0+d236a7a",
    }
    assert evidence["findings"]["client"] == {
        "state": "complete", "breaking": 1, "additive": 28, "affectedTypes": 25,
    }
    assert evidence["findings"]["contracts"]["additive"] == 97
    for name in ("domainService", "serviceDefaults"):
        assert evidence["findings"][name]["state"] == "BothIncomplete"
    assert evidence["findings"]["allFourPackageComparisonsComplete"] is False
    assert evidence["qualityApprovalClaimed"] is False
    assert evidence["acceptedStoryRecordChanged"] is False
    constructor = evidence["findings"]["clientMarkerStoreConstructor"]
    assert constructor["beforeParameterCount"] == 5
    assert constructor["afterParameterCount"] == 6
    assert constructor["newParameterOptional"] is True
    assert constructor["binarySignatureChanged"] is True


def test_released_package_api_evidence_rejects_mutated_record(tmp_path: Path) -> None:
    path = tmp_path / MODULE.API_EVIDENCE
    path.parent.mkdir(parents=True)
    path.write_bytes((ROOT / MODULE.API_EVIDENCE).read_bytes() + b" ")
    with pytest.raises(MODULE.V1.PreparationError) as error:
        MODULE.released_api_evidence(tmp_path)
    assert error.value.code == "SUCCESSOR_API_EVIDENCE_INVALID"


def test_released_package_api_evidence_works_without_local_nuget_cache(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(Path, "home", lambda: tmp_path)
    assert MODULE.released_api_evidence(ROOT)["sha256"] == MODULE.API_EVIDENCE_SHA


def test_pending_packet_binds_released_package_api_evidence() -> None:
    document = json.loads((ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-v5-proposal.json").read_bytes())
    schema = json.loads((ROOT / MODULE.SCHEMA).read_bytes())
    binding = document["successorMaterial"]["releasedEventStoreApiDiff"]
    assert binding == MODULE.released_api_evidence(ROOT)
    assert document["requiredQualityBinding"]["releasedEventStoreApiDiffSha256"] == MODULE.API_EVIDENCE_SHA
    altered = json.loads(json.dumps(document))
    del altered["requiredQualityBinding"]["releasedEventStoreApiDiffSha256"]
    assert list(jsonschema.Draft202012Validator(schema).iter_errors(altered))
    review = (ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-review-v5.md").read_text()
    assert "1 breaking and 28 additive" in review
    assert "BothIncomplete" in review
    assert "genuine Quality decision remain required" in review


def test_sdk_api_compat_evidence_binds_exact_binary_results() -> None:
    binding = MODULE.released_api_compat_evidence(ROOT)
    evidence = json.loads((ROOT / MODULE.API_COMPAT_EVIDENCE).read_bytes())
    assert binding == {
        "path": MODULE.API_COMPAT_EVIDENCE,
        "sha256": MODULE.API_COMPAT_EVIDENCE_SHA,
        "state": "partial-api-compatibility-observation-quality-review-required",
        "sdkVersion": "10.0.401",
    }
    assert [(row["id"], row["exitCode"]) for row in evidence["probes"]] == [
        ("client", 1), ("domainservice", 0), ("servicedefaults", 0),
    ]
    assert evidence["findings"]["client"]["diagnostic"] == "CP0002"
    assert evidence["findings"]["contracts"]["state"] == "not-measured-by-this-tool"
    assert evidence["findings"]["allAdditiveApiReviewComplete"] is False
    assert evidence["qualityApprovalClaimed"] is False


def test_sdk_api_compat_evidence_rejects_mutated_record(tmp_path: Path) -> None:
    path = tmp_path / MODULE.API_COMPAT_EVIDENCE
    path.parent.mkdir(parents=True)
    path.write_bytes((ROOT / MODULE.API_COMPAT_EVIDENCE).read_bytes() + b" ")
    with pytest.raises(MODULE.V1.PreparationError) as error:
        MODULE.released_api_compat_evidence(tmp_path)
    assert error.value.code == "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID"


def test_pending_packet_binds_sdk_api_compatibility() -> None:
    document = json.loads((ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-v5-proposal.json").read_bytes())
    schema = json.loads((ROOT / MODULE.SCHEMA).read_bytes())
    assert document["successorMaterial"]["releasedEventStoreApiCompat"] == MODULE.released_api_compat_evidence(ROOT)
    assert document["requiredQualityBinding"]["releasedEventStoreApiCompatSha256"] == MODULE.API_COMPAT_EVIDENCE_SHA
    altered = json.loads(json.dumps(document))
    del altered["requiredQualityBinding"]["releasedEventStoreApiCompatSha256"]
    assert list(jsonschema.Draft202012Validator(schema).iter_errors(altered))
    review = (ROOT / "docs/release-evidence/conformance-oracle-tiering-migration-review-v5.md").read_text()
    assert "RunPackageValidation/RunApiCompat" in review
    assert "CP0002" in review
    assert "SDK target does not complete additive API review" in review
