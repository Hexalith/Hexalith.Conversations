#!/usr/bin/env python3
"""Verify the Story 11.1 guidance bundle against its source, schema, format parity and review decision.

Blocker codes: THIN_GUIDANCE_OWNERSHIP_INVALID, PROHIBITED_CAPABILITY_PRESENT,
APPHOST_BOUNDARY_INVALID, GUIDANCE_SOURCE_DRIFT, EVIDENCE_FORMAT_DRIFT, REVIEW_DECISION_MISSING.
Exit 0 PASS, 1 FAIL, 2 BLOCKED.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_thin_authoring_guidance as gen  # noqa: E402
import authoring_outputs as output_writer  # noqa: E402

SCHEMA_PATH = "docs/release-evidence/thin-authoring-guidance-v2.schema.json"
RESULT_SCHEMA = "hexalith.conversations.thin-authoring-guidance-result.v1"
UTC = re.compile(r"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z")


def verify(repository: Path, evidence: str, markdown: str, require_review: bool) -> list[dict]:
    blockers: list[dict] = []

    def add(code: str, message: str) -> None:
        if code not in {b["code"] for b in blockers}:
            blockers.append({"code": code, "message": message})

    try:
        raw = gen.read_file(repository, evidence)
        md_bytes = gen.read_file(repository, markdown)
        schema = json.loads(gen.read_file(repository, SCHEMA_PATH))
        contract_bytes = gen.read_file(repository, gen.CONTRACT_PATH)
        source_bytes = gen.read_file(repository, gen.SOURCE_PATH)
    except (gen.Blocked, ValueError) as error:
        raise gen.Blocked(str(error)) from None
    try:
        bundle = json.loads(raw)
    except ValueError:
        add("EVIDENCE_FORMAT_DRIFT", "Evidence JSON is not parseable.")
        return blockers

    try:
        import jsonschema  # noqa: PLC0415
    except ImportError:
        raise gen.Blocked("the jsonschema package is unavailable") from None
    schema_errors = list(jsonschema.Draft202012Validator(schema).iter_errors(bundle))
    if schema_errors:
        add("EVIDENCE_FORMAT_DRIFT", "Evidence does not satisfy the closed v2 schema: " + schema_errors[0].message)
        return blockers

    # Source rules: the real guidance source must still satisfy the ownership/prohibition/AppHost inventories.
    signable = None
    try:
        signable = gen.build_signable(repository, source_bytes, contract_bytes)
    except gen.MultiGuidanceError as error:
        for item in error.errors:
            add(item.code, item.message)
    # (a Blocked error from missing predecessor files propagates as BLOCKED.)

    if signable is not None:
        if bundle["source"]["sha256"] != signable["source"]["sha256"]:
            add("GUIDANCE_SOURCE_DRIFT", "The guidance source changed after the bundle was generated.")
        elif gen.signable_of(bundle) != signable or bundle["digests"]["signablePayloadSha256"] != gen.sha256_bytes(gen.canonical(signable)) \
                or bundle["digests"]["sourceSha256"] != signable["source"]["sha256"]:
            add("EVIDENCE_FORMAT_DRIFT", "The evidence JSON differs from the deterministic projection of its source.")
    elif bundle["source"]["sha256"] != gen.sha256_bytes(source_bytes):
        add("GUIDANCE_SOURCE_DRIFT", "The guidance source changed after the bundle was generated.")

    # Format parity: Markdown is a pure function of the JSON, and the JSON carries its digest.
    if raw.decode("utf-8") != gen.serialize_json(bundle):
        add("EVIDENCE_FORMAT_DRIFT", "Evidence JSON bytes are not in canonical serialization.")
    expected_md = gen.render_markdown(bundle)
    if md_bytes != expected_md.encode("utf-8") or bundle["digests"]["markdownSha256"] != gen.sha256_bytes(md_bytes):
        add("EVIDENCE_FORMAT_DRIFT", "Markdown differs from the deterministic rendering of the JSON bundle.")

    if require_review:
        review = bundle["reviewDecision"]
        digest = bundle["digests"]["signablePayloadSha256"]
        valid = (
            isinstance(review, dict)
            and review.get("decision") == "approved"
            and bool(str(review.get("reviewer", "")).strip())
            and gen.valid_utc(str(review.get("decidedAtUtc", "")))
            and review.get("boundSignablePayloadSha256") == digest
            and review.get("boundSourceSha256") == bundle["source"]["sha256"]
        )
        if not valid:
            add("REVIEW_DECISION_MISSING", "No approved reviewer decision bound to the signable-payload digest.")
    return blockers


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--evidence", required=True)
    parser.add_argument("--markdown", required=True)
    parser.add_argument("--require-review-decision", action="store_true")
    parser.add_argument("--output")
    args = parser.parse_args(argv)
    repository = Path(args.repository)
    try:
        blockers = verify(repository, args.evidence, args.markdown, args.require_review_decision)
        result, code = ("FAIL", 1) if blockers else ("PASS", 0)
    except (gen.Blocked, UnicodeDecodeError) as error:
        blockers, result, code = [{"code": "BLOCKED", "message": str(error)}], "BLOCKED", 2
    report = {"schemaVersion": RESULT_SCHEMA, "result": result, "blockers": blockers}
    if args.output:
        try:
            output_writer.write_outputs(repository, [(args.output, (json.dumps(report, indent=2) + "\n").encode("utf-8"))],
                                        (args.evidence, args.markdown, SCHEMA_PATH, gen.SOURCE_PATH, gen.CONTRACT_PATH,
                                         *(path for _, path, _ in gen.PREDECESSORS)))
        except output_writer.OutputError as error:
            blockers, result, code = [{"code": "BLOCKED", "message": str(error)}], "BLOCKED", 2
    for blocker in blockers:
        print(f"{blocker['code']}: {blocker['message']}", file=sys.stderr)
    print(result)
    return code


if __name__ == "__main__":
    sys.exit(main())
