"""Fault-injection tests for the authority -> evidence -> decision chain validator.

Every fault is staged in a tmp_path copy, so the repository fixtures are never mutated and cannot be
left drifted by a failing assertion.
"""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/verify_decision_chain.py"
SPEC = importlib.util.spec_from_file_location("verify_decision_chain", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
chain = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(chain)

EVIDENCE_JSON = "docs/release-evidence/epic-6-completion-supersession-current-proof-v1.json"
EVIDENCE_MARKDOWN = "docs/release-evidence/epic-6-completion-supersession-current-proof-v1.md"
STAGED_PATHS = (chain.AUTHORITY_PATH, chain.DECISION_PATH, EVIDENCE_JSON, EVIDENCE_MARKDOWN)


def stage(tmp_path: Path) -> Path:
    """Copy the exact chain fixtures into an isolated repository view."""

    staged = tmp_path / "repo"
    for relative in STAGED_PATHS:
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / relative).read_bytes())
    return staged


def write_json(root: Path, relative: str, document: dict) -> None:
    (root / relative).write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def read_json(root: Path, relative: str) -> dict:
    return json.loads((root / relative).read_text(encoding="utf-8"))


def digest_of(root: Path, relative: str) -> str:
    return chain.sha256((root / relative).read_bytes())


def rebind_decision(root: Path, mutate) -> None:
    """Apply a mutation to the decision and repair its own digest bindings.

    Without repairing digests every fault would fail on the digest check first, which would make the
    later assertions vacuous -- the test would pass without ever exercising the rule it names.
    """

    decision = read_json(root, chain.DECISION_PATH)
    mutate(decision)
    decision["sourceEvidence"]["jsonSha256"] = digest_of(root, EVIDENCE_JSON)
    decision["sourceEvidence"]["markdownSha256"] = digest_of(root, EVIDENCE_MARKDOWN)
    write_json(root, chain.DECISION_PATH, decision)


def expect(root: Path, code: str, state: str = "FAIL") -> None:
    with pytest.raises(chain.DecisionChainError) as error:
        chain.verify(root)
    assert error.value.code == code, f"expected {code}, got {error.value.code}: {error.value.message}"
    assert error.value.state == state


def test_intact_chain_passes_with_a_nonempty_ledger(tmp_path: Path) -> None:
    document = chain.verify(stage(tmp_path))
    assert document["result"] == "PASS"
    assert document["implementationHold"] == "ACTIVE"
    assert document["derivedA1Status"] == "done"
    assert len(document["assertionLedger"]) >= 8
    assert all(row["state"] == "PASS" for row in document["assertionLedger"])
    assert document["blockers"] == []


def test_decision_bound_to_a_different_evidence_head_fails_closed(tmp_path: Path) -> None:
    """F-8: a decision citing a HEAD the evidence never recorded is a stale proof."""

    staged = stage(tmp_path)
    rebind_decision(staged, lambda d: d["sourceEvidence"].__setitem__("evidenceHeadCommit", "0" * 39 + "1"))
    expect(staged, "DECISION_CHAIN_HEAD_MISMATCH")


def test_tampered_evidence_bytes_fail_the_digest_binding(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    evidence = read_json(staged, EVIDENCE_JSON)
    evidence["injected"] = True
    write_json(staged, EVIDENCE_JSON, evidence)
    expect(staged, "DECISION_CHAIN_EVIDENCE_DIGEST_DRIFT")


def test_result_disagreement_between_decision_and_evidence_fails(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    rebind_decision(staged, lambda d: d["sourceEvidence"].__setitem__("result", "FAIL"))
    expect(staged, "DECISION_CHAIN_RESULT_MISMATCH")


def test_a_decision_that_lifts_the_hold_fails_closed(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    rebind_decision(staged, lambda d: d["effects"].__setitem__("implementationHold", "LIFTED"))
    expect(staged, "DECISION_CHAIN_HOLD_DRIFT")


@pytest.mark.parametrize("field", ["ir0RerunAuthorized", "successorStarted", "releaseAuthorized"])
def test_a_decision_that_overreaches_fails_closed(tmp_path: Path, field: str) -> None:
    staged = stage(tmp_path)
    rebind_decision(staged, lambda d, f=field: d["effects"].__setitem__(f, True))
    expect(staged, "DECISION_CHAIN_OVERREACH")


def test_asserted_a1_status_that_is_not_derived_fails_closed(tmp_path: Path) -> None:
    """F-9 at the chain level: authority may not encode a status its decision does not support."""

    staged = stage(tmp_path)
    rebind_decision(staged, lambda d: d.__setitem__("decision", "REJECTED"))
    expect(staged, "DECISION_CHAIN_STATUS_NOT_DERIVED")


def test_candidate_mismatch_between_authority_and_decision_fails_closed(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    rebind_decision(staged, lambda d: d["authority"].__setitem__("planningCandidate", "0" * 39 + "1"))
    expect(staged, "DECISION_CHAIN_CANDIDATE_MISMATCH")


def test_missing_evidence_blocks_rather_than_passing(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    (staged / EVIDENCE_JSON).unlink()
    expect(staged, "DECISION_CHAIN_EVIDENCE_UNAVAILABLE", state="BLOCKED")


def test_missing_decision_blocks_rather_than_passing(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    (staged / chain.DECISION_PATH).unlink()
    expect(staged, "DECISION_CHAIN_DECISION_UNAVAILABLE", state="BLOCKED")


def test_cli_exit_codes_preserve_result_semantics(tmp_path: Path) -> None:
    staged = stage(tmp_path)
    assert chain.main(["--repository", str(staged)]) == 0

    rebind_decision(staged, lambda d: d["sourceEvidence"].__setitem__("evidenceHeadCommit", "0" * 39 + "1"))
    assert chain.main(["--repository", str(staged)]) == 1

    (staged / EVIDENCE_JSON).unlink()
    assert chain.main(["--repository", str(staged)]) == 2


def test_repository_fixtures_are_never_mutated_by_the_faults(tmp_path: Path) -> None:
    before = {relative: (ROOT / relative).read_bytes() for relative in STAGED_PATHS}
    staged = stage(tmp_path)
    rebind_decision(staged, lambda d: d["effects"].__setitem__("releaseAuthorized", True))
    expect(staged, "DECISION_CHAIN_OVERREACH")
    for relative, content in before.items():
        assert (ROOT / relative).read_bytes() == content, f"{relative} was mutated by a fault"
