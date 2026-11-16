#!/usr/bin/env python3
"""Verify the Story 12.1 derived-key lifecycle decision bundle against ADR 0005, Git and its approval.

Every value is recomputed rather than trusted: the closed schema, the bundle's own lifecycle rules, the
ADR-derived payload with its contract, inventory and predecessor bindings (including the planning-candidate
bytes in Git), the signable-payload and Markdown digests, the canonical JSON bytes, the deterministic Markdown
and the approval bound to the payload and ADR digests.
Blocker codes: DERIVED_KEY_ADR_INCOMPLETE, EVENTSTORE_AUTHORITY_VIOLATED, DERIVED_KEY_TRANSITION_UNSAFE,
ROLLBACK_PLAN_INVALID, ADR_APPROVAL_MISSING, PREDECESSOR_CANDIDATE_MISMATCH, ENTRY_INVENTORY_MISMATCH,
EVIDENCE_FORMAT_DRIFT. Exit 0 PASS, 1 FAIL, 2 BLOCKED (unresolvable Git history or unavailable input).
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_derived_key_decision as gen  # noqa: E402
import authoring_outputs as output_writer  # noqa: E402

RESULT_SCHEMA = "hexalith.conversations.derived-key-lifecycle-decision-result.v1"

# Fault id -> exact blocker codes from the real verifier for decision-bundle faults (AC-12.1-05 ledger).
# "resigned" faults recompute every digest and rebind the approval, so only independent recomputation sees them.
VERIFIER_FAULT_EXPECTATIONS = {
    "approval-removed": (gen.BLOCK_APPROVAL,),
    "approval-bound-to-stale-digest": (gen.BLOCK_APPROVAL,),
    "approval-unnamed": (gen.BLOCK_APPROVAL,),
    "adr-edited-after-approval": (gen.BLOCK_APPROVAL,),
    "markdown-edited": (gen.BLOCK_FORMAT,),
    "json-unparseable": (gen.BLOCK_FORMAT,),
    "json-noncanonical": (gen.BLOCK_FORMAT,),
    "schema-opened": (gen.BLOCK_FORMAT,),
    "resigned-second-writer": (gen.BLOCK_AUTHORITY, gen.BLOCK_FORMAT),
    "resigned-read-repair": (gen.BLOCK_AUTHORITY, gen.BLOCK_FORMAT),
    "resigned-proof-expiry": (gen.BLOCK_UNSAFE, gen.BLOCK_FORMAT),
    "resigned-reverse-write-rollback": (gen.BLOCK_ROLLBACK, gen.BLOCK_FORMAT),
    "resigned-key-prefix": (gen.BLOCK_FORMAT,),
}


class Ledger:
    """Assertion ledger plus first-message-per-code blockers."""

    def __init__(self) -> None:
        self.rows: list[dict] = []
        self.blockers: list[tuple[str, str]] = []

    def check(self, identity: str, subject: str, passed: bool, code: str | None = None, message: str = "") -> bool:
        self.rows.append({"id": identity, "subject": subject, "state": "PASS" if passed else "FAIL"})
        if not passed and code:
            self.blockers.append((code, message or subject))
        return passed

    def errors(self, identity: str, subject: str, errors: list[tuple[str, str]]) -> None:
        self.rows.append({"id": identity, "subject": subject, "state": "FAIL" if errors else "PASS"})
        self.blockers += errors


def verify(repository: Path, evidence: str, markdown: str, require_approval: bool) -> tuple[Ledger, dict | None]:
    """Return the ledger and the parsed bundle. Raises gen.Blocked when the check cannot run."""
    ledger = Ledger()
    if [evidence.replace("\\", "/"), markdown.replace("\\", "/")] != [gen.JSON_PATH, gen.MARKDOWN_PATH]:
        raise gen.Blocked(gen.ARGUMENT_INVALID, "the closed Story 12.1 decision paths are required")
    raw = gen.read_file(repository, gen.JSON_PATH)
    md_bytes = gen.read_file(repository, gen.MARKDOWN_PATH)
    adr_bytes = gen.read_file(repository, gen.ADR_PATH)
    contract_bytes = gen.read_file(repository, gen.CONTRACT_PATH)
    try:
        schema = json.loads(gen.read_file(repository, gen.SCHEMA_PATH))
        import jsonschema  # noqa: PLC0415
        validator = jsonschema.Draft202012Validator(schema)
    except ImportError:
        raise gen.Blocked(gen.TOOLING_UNAVAILABLE, "the jsonschema package is unavailable") from None
    except ValueError:
        raise gen.Blocked(gen.INPUT_UNAVAILABLE, "the decision schema is not JSON") from None

    try:
        bundle = json.loads(raw)
    except ValueError:
        ledger.check("json:parse", gen.JSON_PATH, False, gen.BLOCK_FORMAT, "The decision JSON is not parseable.")
        return ledger, None
    schema_errors = sorted(validator.iter_errors(bundle), key=lambda error: list(error.absolute_path))
    if not ledger.check("json:schema", gen.SCHEMA_PATH, not schema_errors, gen.BLOCK_FORMAT,
                        "The decision does not satisfy the closed v1 schema: "
                        + (schema_errors[0].message if schema_errors else "")):
        return ledger, None

    # The bundle's own lifecycle must satisfy the rules, independent of its ADR.
    ledger.errors("bundle:lifecycle", "recorded families x fields", gen.evaluate(gen.cells_from_bundle(bundle),
                                                                                 bundle["evidenceTests"]))

    # Recompute the payload from the current ADR, contract, inventory and Git-bound predecessors.
    adr_digest = gen.sha256_bytes(adr_bytes)
    recorded_adr = bundle["candidate"]["adr"]["sha256"]
    signable = None
    try:
        signable = gen.build_signable(repository, adr_bytes, contract_bytes)
        ledger.check("adr:recompute", gen.ADR_PATH, True)
    except gen.DecisionError as error:
        ledger.errors("adr:recompute", gen.ADR_PATH, error.errors)
    adr_current = ledger.check("adr:digest", gen.ADR_PATH, recorded_adr == adr_digest
                               and bundle["digests"]["adrSha256"] == adr_digest, gen.BLOCK_APPROVAL,
                               "ADR 0005 changed after the decision was generated; its approval is stale.")
    if signable is not None and adr_current:
        ledger.check("payload:recompute", "signable payload", gen.signable_of(bundle) == signable, gen.BLOCK_FORMAT,
                     "The decision differs from the deterministic projection of ADR 0005 and its bindings.")
    recorded_digest = bundle["digests"]["signablePayloadSha256"]
    ledger.check("payload:digest", "signablePayloadSha256",
                 recorded_digest == gen.sha256_bytes(gen.canonical(gen.signable_of(bundle))), gen.BLOCK_FORMAT,
                 "The recorded signable-payload digest does not recompute.")

    # Format parity: canonical JSON bytes, and Markdown as a pure function of the JSON.
    ledger.check("json:canonical", gen.JSON_PATH, raw.decode("utf-8", "replace") == gen.serialize_json(bundle),
                 gen.BLOCK_FORMAT, "Decision JSON bytes are not in canonical serialization.")
    try:
        expected_md = gen.render_markdown(bundle).encode("utf-8")
    except (KeyError, TypeError, ValueError):
        expected_md = None
    ledger.check("markdown:parity", gen.MARKDOWN_PATH, md_bytes == expected_md
                 and bundle["digests"]["markdownSha256"] == gen.sha256_bytes(md_bytes), gen.BLOCK_FORMAT,
                 "Markdown differs from the deterministic rendering of the decision JSON.")

    approval = bundle["approval"]
    if require_approval or approval is not None:
        ledger.check("approval:bound", "named owner approval", adr_current
                     and gen.approval_valid(approval, recorded_digest, adr_digest), gen.BLOCK_APPROVAL,
                     "No named owner approval is bound to the current signable-payload and ADR digests.")
    return ledger, bundle


def _report_inputs(repository: Path, bundle: dict | None) -> tuple[str, ...]:
    """Protect ADR citations even when decision parsing or verification did not return a bundle."""
    try:
        _, evidence_tests, _ = gen.parse_adr(gen.read_file(repository, gen.ADR_PATH).decode("utf-8"))
    except (gen.Blocked, UnicodeDecodeError):
        evidence_tests = []
    if isinstance(bundle, dict):
        evidence_tests += bundle["evidenceTests"]
    return gen.readonly_inputs(evidence_tests)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, allow_abbrev=False)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--evidence", required=True)
    parser.add_argument("--markdown", required=True)
    parser.add_argument("--require-approval", action="store_true")
    parser.add_argument("--output")
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    bundle = None
    try:
        ledger, bundle = verify(repository, args.evidence, args.markdown, args.require_approval)
        blockers = [{"code": code, "message": message} for code, message in gen.order_errors(ledger.blockers)]
        rows = ledger.rows
        result, code = ("FAIL", 1) if blockers or any(row["state"] == "FAIL" for row in rows) else ("PASS", 0)
    except gen.Blocked as error:
        blockers = [{"code": error.code, "message": error.message}]
        rows = [{"id": "inputs", "subject": "Git history and decision inputs", "state": "SKIPPED"}]
        result, code = "BLOCKED", 2
    if not rows:  # a vacuous ledger is never PASS
        rows = [{"id": "nonempty-ledger", "subject": "decision verification", "state": "FAIL"}]
        blockers, result, code = [{"code": gen.BLOCK_FORMAT, "message": "No check executed."}], "FAIL", 1
    report = {
        "schemaVersion": RESULT_SCHEMA,
        "result": result,
        "requireApproval": args.require_approval,
        "signablePayloadSha256": (bundle or {}).get("digests", {}).get("signablePayloadSha256")
        if isinstance(bundle, dict) else None,
        "assertionLedger": rows,
        "blockers": blockers,
    }
    if args.output:
        try:
            output_writer.write_outputs(repository, [(args.output, (json.dumps(report, indent=2) + "\n").encode("utf-8"))],
                                        (gen.JSON_PATH, gen.MARKDOWN_PATH, *_report_inputs(repository, bundle)))
        except output_writer.OutputError as error:
            report["blockers"].append({"code": gen.OUTPUT_WRITE_FAILED, "message": str(error)})
            result, code = "BLOCKED", 2
    for blocker in report["blockers"]:
        print(f"{blocker['code']}: {blocker['message']}", file=sys.stderr)
    print(result)
    return code


if __name__ == "__main__":
    sys.exit(main())
