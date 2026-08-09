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
    mutation["actionInventory"][0]["status"] = "done"
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
