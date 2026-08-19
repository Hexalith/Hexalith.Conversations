"""Fault-injection tests for the additive V14 current-candidate authority.

Faults are staged in tmp_path copies so repository fixtures are never mutated.
"""

from __future__ import annotations

from copy import deepcopy
import importlib.util
import json
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]


def _load(name: str):
    path = ROOT / f"_bmad/scripts/{name}.py"
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


publisher = _load("publish_v14_current_candidate_authority")
v9 = _load("publish_v9_planning_authority")
v13 = _load("publish_v13_current_proof_authority")

STAGED_PATHS = (
    publisher.AUTHORITY_PATH,
    publisher.AUTHORITY_SCHEMA_PATH,
    publisher.BUNDLE_PATH,
    publisher.CURRENT_PROOF_AUTHORITY_PATH,
    publisher.PROVENANCE_LEDGER_PATH,
)


def stage(tmp_path: Path) -> Path:
    staged = tmp_path / "repo"
    for relative in STAGED_PATHS:
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / relative).read_bytes())
    return staged


def test_v14_is_additive_pinned_and_outside_the_generated_companion_set() -> None:
    assert publisher.AUTHORITY_PATH not in v9.EXPECTED_OUTPUT_PATHS
    assert publisher.AUTHORITY_SCHEMA_PATH in v9.CANONICAL_PATHS
    assert publisher.AUTHORITY_PATH not in v9.CANONICAL_PATHS
    assert publisher.AUTHORITY_PATH in v9.PROTECTED_CANDIDATE_PATHS
    assert publisher.AUTHORITY_PATH in v9.expected_bundle_artifact_paths()


def test_v14_check_mode_is_green_and_preserves_the_hold() -> None:
    assert publisher.main(["--repository", str(ROOT), "--check"]) == 0
    authority = json.loads((ROOT / publisher.AUTHORITY_PATH).read_text(encoding="utf-8"))
    assert authority["authority"]["implementationHold"] == "ACTIVE"
    assert authority["completionEffect"] == {
        "ir0RerunAllowed": False,
        "holdLifted": False,
        "successorStarted": False,
        "releaseAuthorized": False,
    }
    assert authority["pointInTimePredecessor"]["supersedesEvidence"] is False
    assert authority["successor"] == "none"


def test_v14_records_v13_by_exact_digest_and_pinned_candidate() -> None:
    authority = json.loads((ROOT / publisher.AUTHORITY_PATH).read_text(encoding="utf-8"))
    v13_bytes = (ROOT / publisher.CURRENT_PROOF_AUTHORITY_PATH).read_bytes()
    assert authority["pointInTimePredecessor"]["sha256"] == publisher.sha256(v13_bytes)
    assert authority["pointInTimePredecessor"]["pinnedPlanningCandidate"] == json.loads(
        v13_bytes.decode("utf-8")
    )["authority"]["planningCandidate"]


def test_v14_and_v13_stay_pinned_across_a_later_bundle_rebind(tmp_path: Path) -> None:
    """Published checkpoint heads remain point-in-time evidence after a later bundle rebind."""

    staged = stage(tmp_path)
    for relative in (v13.CURRENT_PROOF_DECISION_PATH, v13.CURRENT_PROOF_CONTRACT_PATH, v13.CURRENT_PROOF_AUTHORITY_SCHEMA_PATH):
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / relative).read_bytes())

    rebound = "0" * 39 + "1"
    bundle_path = staged / publisher.BUNDLE_PATH
    bundle = json.loads(bundle_path.read_text(encoding="utf-8"))
    # V13's pin comes from its own decision, not from whatever the bundle happens to say.
    pinned_before = json.loads(
        (staged / publisher.CURRENT_PROOF_AUTHORITY_PATH).read_text(encoding="utf-8")
    )["authority"]["planningCandidate"]
    bundle["planningCandidate"] = rebound
    bundle_path.write_text(json.dumps(bundle, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    assert publisher.resolve_bundle_candidate(staged) == rebound
    assert publisher.resolve_pinned_candidate(staged) != rebound
    before = (staged / publisher.AUTHORITY_PATH).read_bytes()
    publisher.publish(staged, check=False)
    rewritten = json.loads((staged / publisher.AUTHORITY_PATH).read_text(encoding="utf-8"))
    assert rewritten["authority"]["planningCandidate"] == publisher.resolve_pinned_candidate(staged)
    assert (staged / publisher.AUTHORITY_PATH).read_bytes() == before

    # V13 also stays pinned to its own decision's candidate and its bytes never move.
    assert rewritten["pointInTimePredecessor"]["pinnedPlanningCandidate"] == pinned_before
    assert (staged / publisher.CURRENT_PROOF_AUTHORITY_PATH).read_bytes() == (
        ROOT / publisher.CURRENT_PROOF_AUTHORITY_PATH
    ).read_bytes()


def test_tampering_with_the_provenance_ledger_fails_closed(tmp_path: Path) -> None:
    """F-18: deleting a relocated provenance line must not pass silently."""

    staged = stage(tmp_path)
    ledger = staged / publisher.PROVENANCE_LEDGER_PATH
    text = ledger.read_text(encoding="utf-8")
    head, block = text.split("```yaml\n", 1)
    body, tail = block.rsplit("```\n", 1)
    lines = body.splitlines(keepends=True)
    ledger.write_text(head + "```yaml\n" + "".join(lines[:-1]) + "```\n" + tail, encoding="utf-8")

    with pytest.raises(publisher.CurrentCandidateAuthorityError) as error:
        publisher.publish(staged, check=True)
    assert error.value.code == "CURRENT_CANDIDATE_LEDGER_COUNT_DRIFT"


def test_a_non_comment_line_in_the_ledger_fails_closed(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    ledger = staged / publisher.PROVENANCE_LEDGER_PATH
    text = ledger.read_text(encoding="utf-8")
    head, block = text.split("```yaml\n", 1)
    body, tail = block.rsplit("```\n", 1)
    lines = body.splitlines(keepends=True)
    lines[0] = "  6-2-migrate-conversations-to-platform-owned-hosting: backlog\n"
    ledger.write_text(head + "```yaml\n" + "".join(lines) + "```\n" + tail, encoding="utf-8")

    with pytest.raises(publisher.CurrentCandidateAuthorityError) as error:
        publisher.publish(staged, check=True)
    assert error.value.code == "CURRENT_CANDIDATE_LEDGER_INVALID"


def test_missing_ledger_or_predecessor_fails_closed(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    (staged / publisher.PROVENANCE_LEDGER_PATH).unlink()
    with pytest.raises(publisher.CurrentCandidateAuthorityError) as error:
        publisher.publish(staged, check=True)
    assert error.value.code == "CURRENT_CANDIDATE_LEDGER_UNAVAILABLE"

    staged = stage(tmp_path / "second")
    (staged / publisher.CURRENT_PROOF_AUTHORITY_PATH).unlink()
    with pytest.raises(publisher.CurrentCandidateAuthorityError) as error:
        publisher.publish(staged, check=True)
    assert error.value.code == "CURRENT_CANDIDATE_V13_UNAVAILABLE"


def test_closed_field_mutations_fail_closed(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    candidate = publisher.resolve_bundle_candidate(staged)
    authority = json.loads(publisher.render_current_candidate_authority(staged, candidate))

    for mutate in (
        lambda a: a["completionEffect"].__setitem__("holdLifted", True),
        lambda a: a["completionEffect"].__setitem__("releaseAuthorized", True),
        lambda a: a["authority"].__setitem__("implementationHold", "LIFTED"),
        lambda a: a["pointInTimePredecessor"].__setitem__("supersedesEvidence", True),
        lambda a: a["provenanceLedger"].__setitem__("statusValuesChanged", 1),
        lambda a: a.__setitem__("successor", "IR-0"),
    ):
        mutation = deepcopy(authority)
        mutate(mutation)
        with pytest.raises(publisher.CurrentCandidateAuthorityError) as error:
            publisher.validate_current_candidate_authority(staged, candidate, mutation)
        assert error.value.code in {
            "CURRENT_CANDIDATE_AUTHORITY_DRIFT",
            "CURRENT_CANDIDATE_AUTHORITY_SCHEMA_INVALID",
        }
