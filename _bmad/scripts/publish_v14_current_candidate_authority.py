#!/usr/bin/env python3
"""Publish and validate the additive V14 E6-CURRENT-CANDIDATE authority sidecar.

E6-REMEDIATION A3 §4.4. V13 answered a present-state question at one commit and was accepted there.
It is point-in-time evidence: BMAD 6.11 left four lifecycle routes legitimately ahead of the frozen
planning candidate, so a new candidate was required, and rebinding the bundle would otherwise have
rewritten V13's bytes.

V14 resolves that by splitting the two roles:

* V13 stays byte-frozen and pinned to the candidate its own decision names.
* V14 is the *current* candidate authority and deliberately follows the live bundle.

V14 supersedes nothing. It records V13 as a point-in-time predecessor by exact digest, pins the
relocated sprint-status provenance ledger, and keeps the implementation hold ACTIVE. It authorizes
neither IR-0, nor a successor, nor release.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any


AUTHORITY_PATH = "_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json"
AUTHORITY_SCHEMA_PATH = "_bmad/schemas/v14-current-candidate-authority-v1.schema.json"
BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
CURRENT_PROOF_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json"
PROVENANCE_LEDGER_PATH = "_bmad-output/implementation-artifacts/sprint-status-provenance-v1.md"
RELOCATED_COMMENT_COUNT = 90
AUTHORITIES = {
    "epic": "epic-6-authority-2026-08-04-v12",
    "architecture": "conversations-architecture-2026-08-04-v12",
}


class CurrentCandidateAuthorityError(RuntimeError):
    """Stable fail-closed V14 current-candidate authority failure."""

    def __init__(self, code: str, detail: str) -> None:
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail


def sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def json_bytes(value: Any) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def read_bytes(root: Path, relative_path: str, code: str) -> bytes:
    try:
        return (root / relative_path).read_bytes()
    except OSError as error:
        raise CurrentCandidateAuthorityError(code, f"{relative_path}: {error}") from error


def resolve_bundle_candidate(root: Path) -> str:
    """Resolve the live planning candidate. V14 follows the bundle by design; V13 must not."""

    try:
        bundle = json.loads((root / BUNDLE_PATH).read_text(encoding="utf-8"))
        candidate = bundle["planningCandidate"]
    except (OSError, UnicodeError, json.JSONDecodeError, KeyError, TypeError) as error:
        raise CurrentCandidateAuthorityError("CURRENT_CANDIDATE_BUNDLE_UNAVAILABLE", str(error)) from error
    if not isinstance(candidate, str) or re.fullmatch(r"[0-9a-f]{40}", candidate) is None:
        raise CurrentCandidateAuthorityError("CURRENT_CANDIDATE_BUNDLE_PC_INVALID", repr(candidate))
    return candidate


def resolve_point_in_time_predecessor(root: Path) -> tuple[str, str]:
    """Return V13's exact digest and the candidate it is pinned to."""

    content = read_bytes(root, CURRENT_PROOF_AUTHORITY_PATH, "CURRENT_CANDIDATE_V13_UNAVAILABLE")
    try:
        pinned = json.loads(content.decode("utf-8"))["authority"]["planningCandidate"]
    except (UnicodeError, json.JSONDecodeError, KeyError, TypeError) as error:
        raise CurrentCandidateAuthorityError("CURRENT_CANDIDATE_V13_INVALID", str(error)) from error
    if not isinstance(pinned, str) or re.fullmatch(r"[0-9a-f]{40}", pinned) is None:
        raise CurrentCandidateAuthorityError("CURRENT_CANDIDATE_V13_PC_INVALID", repr(pinned))
    return sha256(content), pinned


def resolve_provenance_ledger(root: Path) -> str:
    """Pin the relocated sprint-status provenance ledger by exact digest and comment count."""

    content = read_bytes(root, PROVENANCE_LEDGER_PATH, "CURRENT_CANDIDATE_LEDGER_UNAVAILABLE")
    text = content.decode("utf-8", errors="strict")
    try:
        block = text.split("```yaml\n", 1)[1].rsplit("```\n", 1)[0]
    except IndexError as error:
        raise CurrentCandidateAuthorityError(
            "CURRENT_CANDIDATE_LEDGER_INVALID", "provenance block delimiters missing"
        ) from error
    lines = block.splitlines()
    if len(lines) != RELOCATED_COMMENT_COUNT:
        raise CurrentCandidateAuthorityError(
            "CURRENT_CANDIDATE_LEDGER_COUNT_DRIFT", f"expected {RELOCATED_COMMENT_COUNT} observed {len(lines)}"
        )
    if not all(line.lstrip().startswith("#") for line in lines):
        raise CurrentCandidateAuthorityError(
            "CURRENT_CANDIDATE_LEDGER_INVALID", "every relocated provenance line must be a comment"
        )
    return sha256(content)


def render_current_candidate_authority(root: Path, candidate: str) -> bytes:
    """Render the additive V14 E6-CURRENT-CANDIDATE sidecar."""

    v13_digest, v13_pinned = resolve_point_in_time_predecessor(root)
    return json_bytes(
        {
            "schemaVersion": "hexalith.conversations.v14-current-candidate-authority.v1",
            "checkpointId": "E6-CURRENT-CANDIDATE",
            "authority": {
                **AUTHORITIES,
                "planningCandidate": candidate,
                "authorityBundlePath": BUNDLE_PATH,
                "implementationHold": "ACTIVE",
            },
            "predecessors": ["E6-REMEDIATION", "E6-CURRENT-PROOF"],
            "successor": "none",
            "pointInTimePredecessor": {
                "checkpointId": "E6-CURRENT-PROOF",
                "path": CURRENT_PROOF_AUTHORITY_PATH,
                "sha256": v13_digest,
                "pinnedPlanningCandidate": v13_pinned,
                "supersedesEvidence": False,
            },
            "provenanceLedger": {
                "path": PROVENANCE_LEDGER_PATH,
                "sha256": resolve_provenance_ledger(root),
                "relocatedCommentCount": RELOCATED_COMMENT_COUNT,
                "statusValuesChanged": 0,
            },
            "resultSemantics": {
                "states": ["PASS", "FAIL", "BLOCKED", "not-applicable"],
                "ledgerRequired": True,
                "skipsAllowed": False,
            },
            "prohibitions": [
                "rewrite completed Story 6.7 or Story 6.2 records",
                "substitute current bytes for historical evidence",
                "edit V1-V13 historical schema, contract, decision, or evidence",
                "rewrite published history",
                "traverse nested submodules",
                "modify product code, packages, submodules, or gitlinks",
                "implement or start successors",
                "create implementation-hold-v1.json",
                "claim release approval",
                "lift the implementation hold",
                "authorize IR-0",
                "silently apply epic-6-retro-item-24 sprint-status transition",
            ],
            "completionEffect": {
                "ir0RerunAllowed": False,
                "holdLifted": False,
                "successorStarted": False,
                "releaseAuthorized": False,
            },
            "assertions": [
                "V14 binds the current planning candidate; V13 stays pinned to the candidate its own decision names.",
                "V14 supersedes no V1-V13 evidence, schema, contract, or decision and rewrites no published history.",
                "The relocated sprint-status provenance ledger is pinned by exact digest and changed no status value.",
                "A green V14 never lifts the hold, authorizes IR-0, starts a successor, or authorizes release.",
            ],
        }
    )


def validate_current_candidate_authority(root: Path, candidate: str, authority: dict[str, Any]) -> None:
    """Recompute the V14 sidecar and reject any closed-field drift."""

    expected = json.loads(render_current_candidate_authority(root, candidate))
    if authority != expected:
        raise CurrentCandidateAuthorityError(
            "CURRENT_CANDIDATE_AUTHORITY_DRIFT", "closed checkpoint authority mismatch"
        )
    try:
        import jsonschema
    except ImportError as error:
        raise CurrentCandidateAuthorityError("SCHEMA_VALIDATION_UNAVAILABLE", str(error)) from error
    try:
        schema = json.loads((root / AUTHORITY_SCHEMA_PATH).read_text(encoding="utf-8"))
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(authority)
    except (OSError, UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise CurrentCandidateAuthorityError("CURRENT_CANDIDATE_AUTHORITY_SCHEMA_INVALID", str(error)) from error


def publish(root: Path, *, check: bool) -> str:
    """Check or write the additive V14 authority sidecar."""

    candidate = resolve_bundle_candidate(root)
    rendered = render_current_candidate_authority(root, candidate)
    validate_current_candidate_authority(root, candidate, json.loads(rendered))
    target = root / AUTHORITY_PATH
    if check:
        if not target.is_file() or target.read_bytes() != rendered:
            raise CurrentCandidateAuthorityError("CURRENT_CANDIDATE_AUTHORITY_DRIFT", AUTHORITY_PATH)
        return candidate
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(rendered)
    return candidate


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args(argv)
    root = Path(args.repository).resolve()
    try:
        candidate = publish(root, check=args.check)
        print(f"V14_CURRENT_CANDIDATE_AUTHORITY_OK PATH={AUTHORITY_PATH} PLANNING_CANDIDATE={candidate}")
        return 0
    except CurrentCandidateAuthorityError as error:
        print(f"{error.code}: {error.detail}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
