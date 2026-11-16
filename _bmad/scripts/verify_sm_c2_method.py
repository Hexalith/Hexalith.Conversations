#!/usr/bin/env python3
"""Independently recompute the SM-C2 v2 method, its algorithm vectors, bindings and approval.

Exit 0 PASS, 1 FAIL, 2 BLOCKED. --allow-unapproved is a draft check only; the contracted
invocation always requires approval. Every applicable result has a nonempty assertion ledger.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import authoring_outputs as output_writer
import generate_sm_c2_method as gen

RESULT_SCHEMA = "hexalith.conversations.sm-c2-measurement-method-result.v2"
SCHEMA_SHA256 = "73238d2795ea20f19f0918269e6333e160fb9e3894e9ddeaa003f7fa0dae61d8"


class Ledger:
    """Executed assertions and deterministic public blockers."""
    def __init__(self):
        self.rows: list[dict] = []
        self.blockers: list[tuple[str, str]] = []

    def check(self, identity: str, passed: bool, code: str, message: str) -> bool:
        self.rows.append({"id": identity, "subject": message, "state": "PASS" if passed else "FAIL"})
        if not passed:
            self.blockers.append((code, message))
        return passed


def substitute_keys(value) -> list[str]:
    """Approval metadata is outside this scan; no approval substitute is legal inside the payload."""
    found = []
    if isinstance(value, dict):
        for key, item in value.items():
            normalized = re.sub(r"[^a-z]", "", key.lower())
            if any(token in normalized for token in ("ceiling", "disclosure", "approval", "recordedonly",
                                                       "correctnessrationale", "owneracceptance")):
                found.append(key)
            if normalized in ("gate", "result", "status", "rule") and isinstance(item, str) \
                    and re.sub(r"[^a-z]", "", item.lower()) in (
                        "recorded", "recordedonly", "recordednotgated", "approvedcostceiling",
                        "costceiling", "disclosureonly", "ownerapproval", "correctnessrationale"):
                found.append(f"{key}={item}")
            found.extend(substitute_keys(item))
    elif isinstance(value, list):
        for item in value:
            found.extend(substitute_keys(item))
    return found


def schema_closed(value) -> bool:
    if isinstance(value, dict):
        if value.get("type") == "object" and (value.get("additionalProperties") is not False
                or set(value.get("required", [])) != set(value.get("properties", {}))):
            return False
        return all(schema_closed(item) for item in value.values())
    return not isinstance(value, list) or all(schema_closed(item) for item in value)


def inspect_method(bundle: dict, expected: dict, ledger: Ledger) -> None:
    payload = gen.signable_of(bundle)
    if not ledger.check("threshold:no-substitute", not substitute_keys(payload), gen.BLOCK_THRESHOLD,
                        "No ceiling, disclosure, approval, correctness rationale or recorded-only substitute key."):
        return
    def missing_fields(actual, wanted) -> bool:
        if isinstance(actual, dict) and isinstance(wanted, dict):
            return bool(set(wanted) - set(actual)) or any(missing_fields(actual[key], value) for key, value in wanted.items())
        if isinstance(actual, list) and isinstance(wanted, list):
            return any(missing_fields(a, b) for a, b in zip(actual, wanted))
        return False
    if missing_fields(payload, expected):
        ledger.check("method:required-fields", False, gen.BLOCK_INCOMPLETE, "Every required payload field exists.")
        return
    recorded_rows = payload.get("rows")
    inventory_ok = isinstance(recorded_rows, list) and all(isinstance(row, dict) for row in recorded_rows) \
        and [row.get("hotPathId") for row in recorded_rows] == list(gen.ROWS)
    ledger.check("rows:inventory", inventory_ok, gen.BLOCK_INCOMPLETE, "Exactly four rows in ordinal inventory order.")
    if inventory_ok:
        for actual, wanted in zip(recorded_rows, expected["rows"], strict=True):
            threshold = actual.get("threshold")
            ledger.check(f"threshold:{wanted['hotPathId']}", gen.canonical(threshold) == gen.canonical(wanted["threshold"]), gen.BLOCK_THRESHOLD,
                         "Every row requires usable signal and the exact decimal 1.05 threshold.")
            ledger.check(f"metadata:{wanted['hotPathId']}",
                         gen.canonical({key: value for key, value in actual.items() if key != "threshold"})
                         == gen.canonical({key: value for key, value in wanted.items() if key != "threshold"}),
                         gen.BLOCK_INCOMPLETE, "Row metadata comes from the immutable baseline.")
    groups = {
        gen.BLOCK_INCOMPLETE: ("schemaVersion", "candidate"),
        gen.BLOCK_RETENTION: ("warmup", "repetitions", "retention"),
        gen.BLOCK_ENVIRONMENT: ("classification", "environment", "comparability"),
        gen.BLOCK_SIGNAL: ("percentile", "signalQuality", "selfChecks"),
    }
    for code, fields in groups.items():
        for field in fields:
            if field not in payload:
                ledger.check(f"method:{field}", False, gen.BLOCK_INCOMPLETE, f"Required method field {field} exists.")
            else:
                ledger.check(f"method:{field}", gen.canonical(payload[field]) == gen.canonical(expected[field]), code,
                             f"Recompute frozen {field} from independent constants and bound sources.")
    ledger.check("method:fields", set(payload) == set(expected), gen.BLOCK_INCOMPLETE, "The 12 payload fields are exact.")


def verify(repository: Path, evidence: str, markdown: str, allow_unapproved: bool) -> tuple[Ledger, dict | None]:
    ledger = Ledger()
    if [evidence, markdown] != [gen.JSON_PATH, gen.MARKDOWN_PATH]:
        raise gen.Blocked(gen.ARGUMENT_INVALID, "the contracted method JSON and Markdown paths are required")
    raw, md = gen.read_file(repository, evidence), gen.read_file(repository, markdown)
    try:
        bundle = json.loads(raw)
    except ValueError:
        ledger.check("json:parse", False, gen.BLOCK_INCOMPLETE, "The method JSON must be parseable.")
        return ledger, None
    if not isinstance(bundle, dict):
        ledger.check("json:object", False, gen.BLOCK_INCOMPLETE, "The method must be a JSON object.")
        return ledger, None
    try:
        schema = gen.load_schema(repository, SCHEMA_SHA256)
        expected = gen.build_payload(repository)
    except gen.MethodError as error:
        ledger.rows.append({"id": "inputs:binding", "subject": "approved schema and historical input binding", "state": "FAIL"})
        ledger.blockers += error.errors
        return ledger, bundle
    inspect_method(bundle, expected, ledger)
    try:
        import jsonschema
    except ImportError:
        raise gen.Blocked(gen.TOOLING_UNAVAILABLE, "jsonschema is unavailable") from None
    ledger.check("schema:closed", schema_closed(schema) and len(schema.get("required", [])) == 14,
                 gen.BLOCK_INCOMPLETE, "All schema objects are closed and all 14 top-level fields are required.")
    # Semantic diagnostics take precedence over the schema's const diagnostics for the same mutation.
    if not ledger.blockers:
        all_errors = list(jsonschema.Draft202012Validator(schema).iter_errors(bundle))
        approval_errors = [error for error in all_errors if (error.absolute_path and error.absolute_path[0] == "approval")
                           or error.message == "'approval' is a required property"]
        schema_errors = [error for error in all_errors if error not in approval_errors]
        ledger.check("json:schema", not schema_errors,
                     gen.BLOCK_INCOMPLETE, "The method satisfies its closed schema.")
        ledger.check("approval:schema", not approval_errors, gen.BLOCK_APPROVAL,
                     "Approval has exactly the closed metadata fields.")
    actual_digest = gen.sha256_bytes(gen.canonical(gen.signable_of(bundle)))
    digests = bundle.get("digests")
    ledger.check("payload:digest", isinstance(digests, dict) and digests.get("signablePayloadSha256") == actual_digest,
                 gen.BLOCK_APPROVAL, "Recompute the current canonical payload digest.")
    approval = bundle.get("approval")
    if not allow_unapproved or approval is not None:
        valid = gen.approval_valid(approval, actual_digest)
        ledger.check("approval:bound", valid, gen.BLOCK_APPROVAL, "A named owner approved the current payload digest.")
        if valid:
            ledger.check("approval:predeclared", not gen.approval_postdated(repository, approval, actual_digest),
                         gen.BLOCK_POSTDATED, "Approval is no later than publication, or verification before publication.")
    ledger.check("json:canonical", raw == gen.serialize_json(bundle).encode(), gen.BLOCK_INCOMPLETE,
                 "JSON has deterministic UTF-8 LF serialization.")
    try:
        expected_md = gen.render_markdown(bundle).encode()
    except (KeyError, TypeError, ValueError):
        expected_md = None
    ledger.check("markdown:parity", md == expected_md and isinstance(digests, dict)
                 and digests.get("markdownSha256") == gen.sha256_bytes(md), gen.BLOCK_INCOMPLETE,
                 "Markdown is the deterministic rendering, with a recomputed digest.")
    return ledger, bundle


def refuse_foreign_output(repository: Path, output: str) -> None:
    """Only replace an existing verifier report; never replace any other file or alias."""
    target = output_writer.output_target(repository, output)
    if not os.path.lexists(target):
        return
    try:
        existing = json.loads(target.read_bytes()) if target.is_file() and not target.is_symlink() else None
    except (OSError, ValueError):
        existing = None
    if not isinstance(existing, dict) or existing.get("schemaVersion") != RESULT_SCHEMA:
        raise output_writer.OutputError("output would replace a file that is not an earlier method verifier report")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, allow_abbrev=False)
    for field in ("repository", "evidence", "markdown"):
        parser.add_argument("--" + field, required=True)
    parser.add_argument("--allow-unapproved", action="store_true", help="Draft validation only; never the approval acceptance gate.")
    parser.add_argument("--output")
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    bundle = None
    try:
        ledger, bundle = verify(repository, args.evidence, args.markdown, args.allow_unapproved)
        rows = ledger.rows
        blockers = [{"code": code, "message": message} for code, message in gen.order_errors(ledger.blockers)]
        result, code = ("FAIL", 1) if blockers else ("PASS", 0)
    except gen.Blocked as error:
        rows = [{"id": "inputs", "subject": "Git history and method inputs", "state": "SKIPPED"}]
        blockers = [{"code": error.code, "message": error.message}]
        result, code = "BLOCKED", 2
    if not rows:
        rows = [{"id": "nonempty-ledger", "subject": "method verification", "state": "FAIL"}]
        blockers = [{"code": gen.BLOCK_INCOMPLETE, "message": "No assertions executed."}]
        result, code = "FAIL", 1
    recorded_digests = bundle.get("digests") if isinstance(bundle, dict) else None
    report = {"schemaVersion": RESULT_SCHEMA, "result": result, "requireApproval": not args.allow_unapproved,
              "signablePayloadSha256": recorded_digests.get("signablePayloadSha256") if isinstance(recorded_digests, dict) else None,
              "assertionLedger": rows, "blockers": blockers}
    if args.output:
        try:
            refuse_foreign_output(repository, args.output)
            output_writer.write_outputs(repository, [(args.output, (json.dumps(report, indent=2) + "\n").encode())],
                                        (gen.JSON_PATH, gen.MARKDOWN_PATH, *gen.readonly_inputs()))
        except output_writer.OutputError as error:
            report["blockers"].append({"code": gen.OUTPUT_WRITE_FAILED, "message": str(error)})
            result, code = "BLOCKED", 2
    for blocker in report["blockers"]:
        print(f"{blocker['code']}: {blocker['message']}", file=sys.stderr)
    print(result)
    return code


if __name__ == "__main__":
    sys.exit(main())
