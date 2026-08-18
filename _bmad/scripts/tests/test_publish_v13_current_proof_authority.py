"""Fault-injection tests for additive V13 current-proof authority publication."""

from __future__ import annotations

from copy import deepcopy
import importlib.util
import json
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_v13_current_proof_authority.py"
SPEC = importlib.util.spec_from_file_location("publish_v13_current_proof_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)

V9_MODULE_PATH = ROOT / "_bmad/scripts/publish_v9_planning_authority.py"
V9_SPEC = importlib.util.spec_from_file_location("publish_v9_planning_authority", V9_MODULE_PATH)
assert V9_SPEC is not None and V9_SPEC.loader is not None
v9 = importlib.util.module_from_spec(V9_SPEC)
V9_SPEC.loader.exec_module(v9)


def test_v13_current_proof_authority_is_closed_a1_only_and_outside_v12_bundle() -> None:
    candidate = publisher.resolve_bundle_candidate(ROOT)
    assert publisher.CURRENT_PROOF_AUTHORITY_PATH not in v9.EXPECTED_OUTPUT_PATHS
    assert publisher.CURRENT_PROOF_AUTHORITY_SCHEMA_PATH not in v9.CANONICAL_PATHS
    assert publisher.CURRENT_PROOF_AUTHORITY_PATH not in v9.expected_bundle_artifact_paths()

    authority = json.loads(publisher.render_current_proof_authority(ROOT, candidate))
    publisher.validate_current_proof_authority(ROOT, candidate, authority)

    assert authority["checkpointId"] == "E6-CURRENT-PROOF"
    assert authority["predecessors"] == ["E6-REMEDIATION"]
    assert authority["successor"] == "none"
    assert [row["id"] for row in authority["actionInventory"]] == ["A1"]
    assert authority["actionInventory"][0]["status"] == "done"
    assert authority["actionInventory"][0]["executionAuthority"] == "E6-CURRENT-PROOF"
    assert authority["authority"]["authorityBundlePath"] == publisher.BUNDLE_PATH
    assert authority["authority"]["planningCandidate"] == candidate
    assert authority["authority"]["implementationHold"] == "ACTIVE"
    assert authority["completionEffect"] == {
        "ir0RerunAllowed": False,
        "holdLifted": False,
        "successorStarted": False,
        "releaseAuthorized": False,
        "retroItem24TransitionRequiresHuman": True,
    }
    assert "lift the implementation hold" in authority["prohibitions"]
    assert "authorize IR-0" in authority["prohibitions"]
    assert "silently apply epic-6-retro-item-24 sprint-status transition" in authority["prohibitions"]

    committed = json.loads((ROOT / publisher.CURRENT_PROOF_AUTHORITY_PATH).read_text(encoding="utf-8"))
    publisher.validate_current_proof_authority(ROOT, candidate, committed)

    mutation = deepcopy(authority)
    mutation["actionInventory"][0]["status"] = "open"
    with pytest.raises(publisher.CurrentProofAuthorityError) as error:
        publisher.validate_current_proof_authority(ROOT, candidate, mutation)
    assert error.value.code == "CURRENT_PROOF_AUTHORITY_DRIFT"

    extra = deepcopy(authority)
    extra["unexpected"] = True
    with pytest.raises(publisher.CurrentProofAuthorityError) as error:
        publisher.validate_current_proof_authority(ROOT, candidate, extra)
    assert error.value.code in {"CURRENT_PROOF_AUTHORITY_DRIFT", "CURRENT_PROOF_AUTHORITY_SCHEMA_INVALID"}


def test_v13_current_proof_authority_check_mode_is_green() -> None:
    assert publisher.main(["--repository", str(ROOT), "--check"]) == 0


def _decision() -> dict:
    return json.loads((ROOT / publisher.CURRENT_PROOF_DECISION_PATH).read_text(encoding="utf-8"))


def test_v13_candidate_is_pinned_to_its_decision_and_cannot_follow_a_bundle_rebind(tmp_path: Path) -> None:
    """F-6: a later planning-candidate rebind must not rewrite point-in-time V13 bytes."""

    decision = _decision()
    pinned = publisher.resolve_pinned_candidate(ROOT, decision)
    assert pinned == decision["authority"]["planningCandidate"]

    # Stage a repository view whose bundle has been rebound to a different candidate.
    rebound = "0" * 39 + "1"
    assert rebound != pinned
    staged = tmp_path / "repo"
    for relative in (
        publisher.BUNDLE_PATH,
        publisher.CURRENT_PROOF_DECISION_PATH,
        publisher.CURRENT_PROOF_CONTRACT_PATH,
        publisher.CURRENT_PROOF_AUTHORITY_PATH,
        publisher.CURRENT_PROOF_AUTHORITY_SCHEMA_PATH,
    ):
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / relative).read_bytes())
    bundle_path = staged / publisher.BUNDLE_PATH
    bundle = json.loads(bundle_path.read_text(encoding="utf-8"))
    bundle["planningCandidate"] = rebound
    bundle_path.write_text(json.dumps(bundle, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    # The pin still resolves from the decision, and --check stays green against unchanged V13 bytes.
    assert publisher.resolve_pinned_candidate(staged, _decision()) == pinned
    assert publisher.main(["--repository", str(staged), "--check"]) == 0
    assert publisher.resolve_bundle_candidate(staged) == rebound


def test_v13_a1_status_is_derived_from_the_decision_not_asserted() -> None:
    """F-9: forcing A1 done without a valid accepting decision must fail closed."""

    accepted = _decision()
    assert publisher.derive_a1_status(accepted) == "done"

    for mutation, field in (
        ({**accepted, "decision": "REJECTED"}, "decision"),
        ({**accepted, "effects": {**accepted["effects"], "a1PresentStateSatisfied": False}}, "effects"),
        ({**accepted, "sourceEvidence": {**accepted["sourceEvidence"], "result": "FAIL"}}, "sourceEvidence"),
    ):
        assert publisher.derive_a1_status(mutation) == "open", f"{field} must not yield done"

    # A decision that overreaches is rejected before any status is derived.
    for field in ("ir0RerunAuthorized", "successorStarted", "releaseAuthorized"):
        overreach = {**accepted, "effects": {**accepted["effects"], field: True}}
        with pytest.raises(publisher.CurrentProofAuthorityError) as error:
            publisher.assert_decision_preserves_hold(overreach)
        assert error.value.code == "CURRENT_PROOF_DECISION_OVERREACH"

    lifted = {**accepted, "effects": {**accepted["effects"], "implementationHold": "LIFTED"}}
    with pytest.raises(publisher.CurrentProofAuthorityError) as error:
        publisher.assert_decision_preserves_hold(lifted)
    assert error.value.code == "CURRENT_PROOF_DECISION_HOLD_DRIFT"
