#!/usr/bin/env python3
"""Publish and validate the additive V13 E6-CURRENT-PROOF authority sidecar.

This companion stays outside the candidate-bound V12 companion set produced by
``publish_v9_planning_authority.py``. Inlining it into that publisher would
change a CANONICAL_PATHS byte and force a planning-candidate rebind; V13 instead
references the existing ``v9-authority-bundle-v1.json`` and planning candidate
without rewriting V1-V12 companions.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any


CURRENT_PROOF_AUTHORITY_PATH = "_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json"
CURRENT_PROOF_AUTHORITY_SCHEMA_PATH = "_bmad/schemas/v13-current-proof-authority-v1.schema.json"
CURRENT_PROOF_CONTRACT_PATH = (
    "_bmad-output/planning-artifacts/epic-6-completion-supersession-current-proof-contract-v1.json"
)
BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
AUTHORITIES = {
    "epic": "epic-6-authority-2026-08-04-v12",
    "architecture": "conversations-architecture-2026-08-04-v12",
}
ROOT_GITLINK_PATHS = (
    "references/Hexalith.AI.Tools",
    "references/Hexalith.Builds",
    "references/Hexalith.Commons",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)


class CurrentProofAuthorityError(RuntimeError):
    """Stable fail-closed V13 current-proof authority failure."""

    def __init__(self, code: str, detail: str) -> None:
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail


def sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def json_bytes(value: Any) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def resolve_bundle_candidate(root: Path) -> str:
    try:
        bundle = json.loads((root / BUNDLE_PATH).read_text(encoding="utf-8"))
        candidate = bundle["planningCandidate"]
    except (OSError, UnicodeError, json.JSONDecodeError, KeyError, TypeError) as error:
        raise CurrentProofAuthorityError("CURRENT_PROOF_BUNDLE_UNAVAILABLE", str(error)) from error
    if not isinstance(candidate, str) or re.fullmatch(r"[0-9a-f]{40}", candidate) is None:
        raise CurrentProofAuthorityError("CURRENT_PROOF_BUNDLE_PC_INVALID", repr(candidate))
    return candidate


def render_current_proof_authority(root: Path, bundle_candidate: str) -> bytes:
    """Render the additive V13 E6-CURRENT-PROOF sidecar.

    Binds the same v9 authority bundle and planning candidate as V12, scopes
    actionInventory to A1 only, and never authorizes hold lift, IR-0, release,
    or a silent epic-6-retro-item-24 transition.
    """

    try:
        contract_bytes = (root / CURRENT_PROOF_CONTRACT_PATH).read_bytes()
    except OSError as error:
        raise CurrentProofAuthorityError("CURRENT_PROOF_CONTRACT_UNAVAILABLE", str(error)) from error
    return json_bytes(
        {
            "schemaVersion": "hexalith.conversations.v13-current-proof-authority.v1",
            "checkpointId": "E6-CURRENT-PROOF",
            "authority": {
                **AUTHORITIES,
                "planningCandidate": bundle_candidate,
                "authorityBundlePath": BUNDLE_PATH,
                "implementationHold": "ACTIVE",
            },
            "predecessors": ["E6-REMEDIATION"],
            "successor": "none",
            "actionInventory": [
                {
                    "id": "A1",
                    "owner": "Dev workflow / Release owner",
                    "scope": (
                        "Additive present-state current-proof of Story 6.7 and Story 6.2 "
                        "done-commit reachability, every post-done references/ path change, "
                        "all ten root mode-160000 gitlinks at current HEAD, and the declared "
                        "current-tree build/test surface, plus an independent decision that "
                        "never rewrites V1-V12 historical evidence."
                    ),
                    "executionAuthority": "E6-CURRENT-PROOF",
                    "checkpointOwned": True,
                    "status": "done",
                }
            ],
            "rootGitlinkPaths": list(ROOT_GITLINK_PATHS),
            "currentProofContract": {
                "path": CURRENT_PROOF_CONTRACT_PATH,
                "sha256": sha256(contract_bytes),
            },
            "resultSemantics": {
                "states": ["PASS", "FAIL", "BLOCKED", "not-applicable"],
                "ledgerRequired": True,
                "skipsAllowed": False,
            },
            "prohibitions": [
                "rewrite completed Story 6.7 or Story 6.2 records",
                "substitute current bytes for historical evidence",
                "edit V1-V12 historical schema, contract, decision, or evidence",
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
                "retroItem24TransitionRequiresHuman": True,
            },
            "assertions": [
                "A1 is answered only by the additive current-proof route and an independent decision.",
                "V1-V12 historical FAIL/REJECTED completion-supersession evidence remains byte-identical and authoritative for the historical question.",
                "An ACCEPTED current-proof decision never lifts the hold, authorizes IR-0, starts a successor, or authorizes release.",
                "Any epic-6-retro-item-24 sprint-status transition requires explicit human application.",
            ],
        }
    )


def validate_current_proof_authority(root: Path, bundle_candidate: str, authority: dict[str, Any]) -> None:
    """Recompute the V13 current-proof sidecar and reject any closed-field drift."""

    expected = json.loads(render_current_proof_authority(root, bundle_candidate))
    if authority != expected:
        raise CurrentProofAuthorityError("CURRENT_PROOF_AUTHORITY_DRIFT", "closed checkpoint authority mismatch")
    try:
        import jsonschema
    except ImportError as error:
        raise CurrentProofAuthorityError("SCHEMA_VALIDATION_UNAVAILABLE", str(error)) from error
    try:
        schema = json.loads((root / CURRENT_PROOF_AUTHORITY_SCHEMA_PATH).read_text(encoding="utf-8"))
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(authority)
    except (OSError, UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise CurrentProofAuthorityError("CURRENT_PROOF_AUTHORITY_SCHEMA_INVALID", str(error)) from error


def publish(root: Path, *, check: bool) -> None:
    """Check or write the additive V13 authority sidecar."""

    candidate = resolve_bundle_candidate(root)
    rendered = render_current_proof_authority(root, candidate)
    authority = json.loads(rendered)
    validate_current_proof_authority(root, candidate, authority)
    target = root / CURRENT_PROOF_AUTHORITY_PATH
    if check:
        if not target.is_file() or target.read_bytes() != rendered:
            raise CurrentProofAuthorityError("CURRENT_PROOF_AUTHORITY_DRIFT", CURRENT_PROOF_AUTHORITY_PATH)
        return
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(rendered)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args(argv)
    root = Path(args.repository).resolve()
    try:
        publish(root, check=args.check)
        print(f"V13_CURRENT_PROOF_AUTHORITY_OK PATH={CURRENT_PROOF_AUTHORITY_PATH}")
        return 0
    except CurrentProofAuthorityError as error:
        print(f"{error.code}: {error.detail}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
