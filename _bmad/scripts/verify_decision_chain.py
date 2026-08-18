#!/usr/bin/env python3
"""Verify the authority -> evidence -> decision chain for the additive current-proof route.

E6-REMEDIATION A3 §4.5. The V13 sidecar previously asserted A1's completion as a literal and the
preflight never consumed the evidence or the independent decision, so an authority-shape check could
read green while the decision it claims to rest on was absent, stale, or overreaching.

This validator resolves the chain in one direction and fails closed at the first break:

    v13-current-proof-authority-v1.json
        -> epic-6-completion-supersession-current-proof-decision-v1.json   (independent decision)
            -> docs/release-evidence/epic-6-completion-supersession-current-proof-v1.{json,md}

It proves the decision names the evidence it actually accepted (exact digests and evidence HEAD),
that the authority's A1 status is derived from that decision rather than asserted, and that neither
artifact claims authority the checkpoint may not carry. It never rewrites any artifact.

Result semantics match the rest of the lane: PASS / FAIL / BLOCKED / not-applicable, with a nonempty
assertion ledger and no skips.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any, Sequence


AUTHORITY_PATH = "_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json"
DECISION_PATH = (
    "_bmad-output/planning-artifacts/epic-6-completion-supersession-current-proof-decision-v1.json"
)
COMMIT_PATTERN = re.compile(r"[0-9a-f]{40}")


class DecisionChainError(RuntimeError):
    """A stable decision-chain result that must not become PASS."""

    def __init__(self, code: str, message: str, state: str = "FAIL") -> None:
        super().__init__(message)
        self.code = code
        self.message = message
        self.state = state


def sha256(content: bytes) -> str:
    """Return a lowercase SHA-256 digest."""

    return hashlib.sha256(content).hexdigest()


def assertion(assertion_id: str, subject: str, state: str, **details: Any) -> dict[str, Any]:
    """Create one non-vacuous assertion-ledger row."""

    row = {"id": assertion_id, "subject": subject, "state": state}
    row.update(details)
    return row


def load_json(root: Path, relative_path: str, code: str) -> tuple[dict[str, Any], bytes]:
    """Load one repository-relative JSON document and its exact bytes."""

    target = root / relative_path
    try:
        content = target.read_bytes()
    except OSError as error:
        raise DecisionChainError(code, f"{relative_path}: {error}", state="BLOCKED") from error
    try:
        document = json.loads(content.decode("utf-8"))
    except (UnicodeError, json.JSONDecodeError) as error:
        raise DecisionChainError(code, f"{relative_path}: {error}") from error
    if not isinstance(document, dict):
        raise DecisionChainError(code, f"{relative_path}: expected a JSON object")
    return document, content


def require(condition: bool, code: str, message: str) -> None:
    """Fail closed on a broken chain link."""

    if not condition:
        raise DecisionChainError(code, message)


def derive_a1_status(decision: dict[str, Any]) -> str:
    """Mirror the publisher's derivation so authority and validator cannot disagree silently."""

    accepted = decision.get("decision") == "ACCEPTED"
    satisfied = decision.get("effects", {}).get("a1PresentStateSatisfied") is True
    evidence_passed = decision.get("sourceEvidence", {}).get("result") == "PASS"
    return "done" if accepted and satisfied and evidence_passed else "open"


def verify(root: Path) -> dict[str, Any]:
    """Resolve and prove the complete authority -> evidence -> decision chain."""

    ledger: list[dict[str, Any]] = []

    authority, authority_bytes = load_json(root, AUTHORITY_PATH, "DECISION_CHAIN_AUTHORITY_UNAVAILABLE")
    decision, decision_bytes = load_json(root, DECISION_PATH, "DECISION_CHAIN_DECISION_UNAVAILABLE")
    ledger.append(assertion("CHAIN-01", "authority-loaded", "PASS", sha256=sha256(authority_bytes)))
    ledger.append(assertion("CHAIN-02", "decision-loaded", "PASS", sha256=sha256(decision_bytes)))

    source = decision.get("sourceEvidence")
    require(isinstance(source, dict), "DECISION_CHAIN_SOURCE_MISSING", "decision.sourceEvidence is required")
    assert isinstance(source, dict)

    # The decision must name the evidence it accepted, by exact bytes -- not merely by path.
    evidence_relative = source.get("jsonPath")
    require(
        isinstance(evidence_relative, str) and bool(evidence_relative),
        "DECISION_CHAIN_SOURCE_MISSING",
        "decision.sourceEvidence.jsonPath is required",
    )
    assert isinstance(evidence_relative, str)
    evidence, evidence_bytes = load_json(root, evidence_relative, "DECISION_CHAIN_EVIDENCE_UNAVAILABLE")
    observed_json_digest = sha256(evidence_bytes)
    require(
        source.get("jsonSha256") == observed_json_digest,
        "DECISION_CHAIN_EVIDENCE_DIGEST_DRIFT",
        f"{evidence_relative}: decision={source.get('jsonSha256')!r} observed={observed_json_digest!r}",
    )
    ledger.append(assertion("CHAIN-03", evidence_relative, "PASS", sha256=observed_json_digest))

    markdown_relative = source.get("markdownPath")
    if isinstance(markdown_relative, str) and markdown_relative:
        try:
            markdown_bytes = (root / markdown_relative).read_bytes()
        except OSError as error:
            raise DecisionChainError(
                "DECISION_CHAIN_EVIDENCE_UNAVAILABLE", f"{markdown_relative}: {error}", state="BLOCKED"
            ) from error
        observed_markdown_digest = sha256(markdown_bytes)
        require(
            source.get("markdownSha256") == observed_markdown_digest,
            "DECISION_CHAIN_EVIDENCE_DIGEST_DRIFT",
            f"{markdown_relative}: decision={source.get('markdownSha256')!r} observed={observed_markdown_digest!r}",
        )
        ledger.append(assertion("CHAIN-04", markdown_relative, "PASS", sha256=observed_markdown_digest))

    # The decision's bound evidence HEAD must be the HEAD the evidence itself recorded. A decision
    # pointing at a different commit than the evidence it cites is the exact stale-proof failure mode.
    decision_head = source.get("evidenceHeadCommit")
    evidence_head = evidence.get("currentHeadCommit")
    require(
        isinstance(decision_head, str) and COMMIT_PATTERN.fullmatch(decision_head) is not None,
        "DECISION_CHAIN_HEAD_INVALID",
        f"decision.sourceEvidence.evidenceHeadCommit={decision_head!r}",
    )
    require(
        isinstance(evidence_head, str) and COMMIT_PATTERN.fullmatch(evidence_head) is not None,
        "DECISION_CHAIN_HEAD_INVALID",
        f"evidence.currentHeadCommit={evidence_head!r}",
    )
    require(
        decision_head == evidence_head,
        "DECISION_CHAIN_HEAD_MISMATCH",
        f"decision={decision_head!r} evidence={evidence_head!r}",
    )
    ledger.append(assertion("CHAIN-05", "evidence-head-binding", "PASS", commit=decision_head))

    require(
        source.get("result") == evidence.get("result"),
        "DECISION_CHAIN_RESULT_MISMATCH",
        f"decision={source.get('result')!r} evidence={evidence.get('result')!r}",
    )
    ledger.append(assertion("CHAIN-06", "evidence-result-binding", "PASS", result=evidence.get("result")))

    # Neither the decision nor the evidence may claim authority this checkpoint cannot carry.
    effects = decision.get("effects", {})
    require(
        effects.get("implementationHold") == "ACTIVE",
        "DECISION_CHAIN_HOLD_DRIFT",
        f"decision.effects.implementationHold={effects.get('implementationHold')!r}",
    )
    require(
        evidence.get("implementationHold") == "ACTIVE",
        "DECISION_CHAIN_HOLD_DRIFT",
        f"evidence.implementationHold={evidence.get('implementationHold')!r}",
    )
    for field in ("ir0RerunAuthorized", "successorStarted", "releaseAuthorized"):
        require(
            effects.get(field) is False,
            "DECISION_CHAIN_OVERREACH",
            f"decision.effects.{field}={effects.get(field)!r}",
        )
    ledger.append(assertion("CHAIN-07", "hold-and-authority-preserved", "PASS", hold="ACTIVE"))

    # The authority must bind the same candidate as its decision and derive A1 rather than assert it.
    require(
        authority.get("authority", {}).get("planningCandidate")
        == decision.get("authority", {}).get("planningCandidate"),
        "DECISION_CHAIN_CANDIDATE_MISMATCH",
        f"authority={authority.get('authority', {}).get('planningCandidate')!r} "
        f"decision={decision.get('authority', {}).get('planningCandidate')!r}",
    )
    require(
        authority.get("authority", {}).get("implementationHold") == "ACTIVE",
        "DECISION_CHAIN_HOLD_DRIFT",
        f"authority.implementationHold={authority.get('authority', {}).get('implementationHold')!r}",
    )

    inventory = authority.get("actionInventory")
    require(
        isinstance(inventory, list) and len(inventory) == 1 and inventory[0].get("id") == "A1",
        "DECISION_CHAIN_INVENTORY_INVALID",
        "authority.actionInventory must contain exactly A1",
    )
    assert isinstance(inventory, list)
    expected_status = derive_a1_status(decision)
    require(
        inventory[0].get("status") == expected_status,
        "DECISION_CHAIN_STATUS_NOT_DERIVED",
        f"authority={inventory[0].get('status')!r} derived={expected_status!r}",
    )
    ledger.append(
        assertion(
            "CHAIN-08",
            "a1-status-derived-from-decision",
            "PASS",
            status=expected_status,
            candidate=decision.get("authority", {}).get("planningCandidate"),
        )
    )

    if not ledger:
        raise DecisionChainError("DECISION_CHAIN_LEDGER_EMPTY", "assertion ledger must be nonempty")

    return {
        "schemaVersion": "hexalith.conversations.decision-chain.v1",
        "result": "PASS",
        "authorityPath": AUTHORITY_PATH,
        "decisionPath": DECISION_PATH,
        "evidencePath": evidence_relative,
        "evidenceHeadCommit": decision_head,
        "derivedA1Status": expected_status,
        "implementationHold": "ACTIVE",
        "assertionLedger": ledger,
        "blockers": [],
    }


def failure_document(error: DecisionChainError) -> dict[str, Any]:
    """Render a stable fail-closed result that can never be read as PASS."""

    return {
        "schemaVersion": "hexalith.conversations.decision-chain.v1",
        "result": error.state,
        "authorityPath": AUTHORITY_PATH,
        "decisionPath": DECISION_PATH,
        "assertionLedger": [assertion(error.code, "decision-chain", error.state, message=error.message)],
        "blockers": [{"code": error.code, "state": error.state, "message": error.message}],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    parser.add_argument("--output", default=None)
    return parser


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the chain validator and preserve PASS/FAIL/BLOCKED semantics."""

    args = build_parser().parse_args(arguments)
    repository = Path(args.repository)
    try:
        document = verify(repository)
    except DecisionChainError as error:
        document = failure_document(error)
    content = json.dumps(document, indent=2, ensure_ascii=False) + "\n"
    if args.output:
        output = Path(args.output)
        if not output.is_absolute():
            output = repository.resolve() / output
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(content, encoding="utf-8")
    sys.stdout.write(content)
    return {"PASS": 0, "not-applicable": 0, "FAIL": 1, "BLOCKED": 2}[document["result"]]


if __name__ == "__main__":
    raise SystemExit(main())
