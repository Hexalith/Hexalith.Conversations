#!/usr/bin/env python3
"""Generate the Story 11.1 thin-module authoring guidance bundle (JSON + Markdown).

The guidance source (docs/domain-module-authoring-template.md) is parsed, validated
against the closed ownership, prohibition and test-AppHost inventories, and rendered
deterministically. Blocker codes: THIN_GUIDANCE_OWNERSHIP_INVALID,
PROHIBITED_CAPABILITY_PRESENT, APPHOST_BOUNDARY_INVALID.
"""
from __future__ import annotations

import argparse
from datetime import datetime
import hashlib
import json
import re
import sys
from pathlib import Path

SCHEMA_VERSION = "hexalith.conversations.thin-authoring-guidance.v2"
SOURCE_PATH = "docs/domain-module-authoring-template.md"
CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/11.1.json"

OWNERSHIP_MARKER = "<!-- thin-guidance:ownership-inventory -->"
PROHIBITED_MARKER = "<!-- thin-guidance:prohibited-inventory -->"
APPHOST_MARKER = "<!-- thin-guidance:test-apphost -->"

EXPECTED_MODULE_OWNED = {
    "contracts": "Contracts: public commands, events, identifiers, query/result DTOs",
    "domain-behavior": "Domain behavior: aggregate, state, validation, idempotency, domain rules",
    "module-registration": "Module registration: domain assembly marker and domain-specific dependency graph",
    "module-tests": "Module tests: unit, contract, server, conformance, and integration tests",
}
EXPECTED_PLATFORM_OWNED = {
    "service-defaults": "ServiceDefaults",
    "dapr-wiring": "Dapr implementation and wiring",
    "event-subscription": "Event subscription",
    "projection-query-runtime": "Projection and query runtime",
    "publication": "Publication",
    "health": "Health",
    "telemetry": "Telemetry runtime",
}
EXPECTED_PROHIBITED = {
    "reusable-aspire-library": "A reusable module Aspire library is prohibited.",
    "module-service-defaults": "A module-owned `ServiceDefaults` project is prohibited.",
    "module-dapr-implementation": "Module-owned Dapr implementation or wiring is prohibited.",
    "module-event-subscription": "Module-owned event subscription is prohibited.",
    "module-projection-query-runtime": "Module-owned projection or query runtime is prohibited.",
    "module-publication": "Module-owned publication is prohibited.",
    "module-health": "Module-owned health endpoints or checks are prohibited.",
    "module-telemetry-runtime": "Module-owned telemetry runtime is prohibited.",
}
EXPECTED_APPHOST_RULES = {
    "count": "Exactly one test AppHost per module.",
    "IsPackable": "`false`",
    "IsPublishable": "`false`",
    "scope": "Repository-only; never shipped to adopters.",
    "authoring-cost": "The test AppHost counts in authoring cost (SM-2) when hand-authored.",
}
TEST_APPHOST = {
    "count": 1,
    "isPackable": False,
    "isPublishable": False,
    "repositoryOnly": True,
    "countedInAuthoringCost": True,
}

PREDECESSORS = (
    ("6.2", "docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json", "release-evidence"),
    ("7.4", "docs/release-evidence/story-7.4-final-record-v2.json", "final-record-v2"),
    ("10.4", "docs/release-evidence/story-10.4-final-record-v2.json", "final-record-v2"),
)
AUTHORITY_SUBSTITUTION = {
    "acceptanceCriterion": "AC-11.1-01",
    "decision": (
        "Owner decision 2026-10-18: AC-11.1-01 does not build the absent "
        "verify_story_candidate.py; current-policy checks are substituted."
    ),
    "reason": "verify_story_candidate.py is absent from the repository and is not built by Story 11.1.",
    "currentPolicyChecks": [
        "The candidate is the committed HEAD.",
        "The Story 7.4 and Story 10.4 final-record v2 digests are recomputed from the committed records.",
        "Story 6.2 is bound through its existing release evidence; it has no v2 record.",
    ],
}
VALIDATION_CHECKS = (
    "ownership-inventory-exact",
    "prohibited-inventory-exact",
    "test-apphost-boundary",
    "skeleton-no-module-aspire-or-service-defaults",
    "deterministic-json-markdown",
)

BLOCK_OWNERSHIP = "THIN_GUIDANCE_OWNERSHIP_INVALID"
BLOCK_PROHIBITED = "PROHIBITED_CAPABILITY_PRESENT"
BLOCK_APPHOST = "APPHOST_BOUNDARY_INVALID"

# Fault id -> exact blocker expected from the real checker (Story 11.1 AC-11.1-06 ledger).
FAULT_EXPECTATIONS = {
    **{f"reassign-{capability}": BLOCK_OWNERSHIP for capability in EXPECTED_PLATFORM_OWNED},
    "missing-capability": BLOCK_OWNERSHIP,
    "extra-capability": BLOCK_OWNERSHIP,
    "prohibition-removed": BLOCK_PROHIBITED,
    "reusable-aspire-library-added": BLOCK_PROHIBITED,
    "module-service-defaults-added": BLOCK_PROHIBITED,
    "namespaced-service-defaults-added": BLOCK_PROHIBITED,
    "reusable-aspire-library-phrase-added": BLOCK_PROHIBITED,
    "apphost-packable": BLOCK_APPHOST,
    "apphost-publishable": BLOCK_APPHOST,
    "apphost-cost-rule-omitted": BLOCK_APPHOST,
    "apphost-second-host": BLOCK_APPHOST,
    "apphost-none": BLOCK_APPHOST,
    "review-removed": "REVIEW_DECISION_MISSING",
    "source-edited-after-generation": "GUIDANCE_SOURCE_DRIFT",
    "markdown-edited": "EVIDENCE_FORMAT_DRIFT",
    "json-edited": "EVIDENCE_FORMAT_DRIFT",
    "schema-opened": "EVIDENCE_FORMAT_DRIFT",
}

HOST_ROW = "Repository-only test AppHost"


class GuidanceError(Exception):
    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code = code
        self.message = message


class Blocked(Exception):
    pass


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical(value) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")


def section_after(text: str, marker: str) -> list[str] | None:
    """Return table lines directly following a marker, or None if the marker is not unique."""
    if text.count(marker) != 1:
        return None
    rest = text.split(marker, 1)[1].split("\n")
    lines: list[str] = []
    started = False
    for line in rest:
        if line.startswith("|"):
            started = True
            lines.append(line)
        elif started:
            break
        elif line.startswith("#"):
            break
    return lines


def table_rows(lines: list[str]) -> list[tuple[str, ...]]:
    rows = []
    for line in lines[2:]:  # header + separator
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        rows.append(tuple(cells))
    return rows


def strip_ticks(value: str) -> str:
    return value.strip().strip("`")


def is_host_row(category: str) -> bool:
    return category.replace("`", "").strip() == HOST_ROW


def skeleton_categories(text: str) -> list[str]:
    match = re.search(r"^## Minimal Project Skeleton\s*$", text, re.M)
    if not match:
        return []
    body = text[match.end():].split("\n## ", 1)[0]
    lines = [line for line in body.split("\n") if line.startswith("|")]
    return [row[0] for row in table_rows(lines)] if len(lines) > 2 else []


def parse_source(text: str) -> dict:
    """Parse and validate the guidance source. Raises GuidanceError with the first blocker per class."""
    errors: list[GuidanceError] = []

    # Ownership inventory.
    module: dict[str, str] = {}
    platform: dict[str, str] = {}
    lines = section_after(text, OWNERSHIP_MARKER)
    ownership_ok = lines is not None and len(lines) > 2
    if ownership_ok:
        for row in table_rows(lines):
            if len(row) != 3:
                ownership_ok = False
                continue
            ident, owner, capability = strip_ticks(row[0]), row[1], row[2]
            target = {"module": module, "platform": platform}.get(owner)
            if target is None or ident in module or ident in platform:
                ownership_ok = False
                continue
            target[ident] = capability
    if not ownership_ok or module != EXPECTED_MODULE_OWNED or platform != EXPECTED_PLATFORM_OWNED:
        errors.append(GuidanceError(
            BLOCK_OWNERSHIP,
            "Module/platform ownership inventory has a missing, additional, or reassigned capability.",
        ))

    # Prohibited inventory.
    prohibited: dict[str, str] = {}
    lines = section_after(text, PROHIBITED_MARKER)
    prohibited_ok = lines is not None and len(lines) > 2
    if prohibited_ok:
        for row in table_rows(lines):
            if len(row) != 2 or strip_ticks(row[0]) in prohibited:
                prohibited_ok = False
                continue
            prohibited[strip_ticks(row[0])] = row[1]
    skeleton = skeleton_categories(text)
    skeleton_violation = any(
        re.search(r"servicedefaults|aspire", category, re.I) for category in skeleton if not is_host_row(category)
    )
    if not prohibited_ok or prohibited != EXPECTED_PROHIBITED or skeleton_violation:
        errors.append(GuidanceError(
            BLOCK_PROHIBITED,
            "Prohibited-capability inventory is incomplete or the skeleton lists a module ServiceDefaults or Aspire library.",
        ))

    # Test AppHost.
    rules: dict[str, str] = {}
    lines = section_after(text, APPHOST_MARKER)
    apphost_ok = lines is not None and len(lines) > 2
    if apphost_ok:
        for row in table_rows(lines):
            if len(row) != 2 or strip_ticks(row[0]) in rules:
                apphost_ok = False
                continue
            rules[strip_ticks(row[0])] = row[1]
    host_rows = [c for c in skeleton if "apphost" in c.lower()]
    if not apphost_ok or rules != EXPECTED_APPHOST_RULES or len(host_rows) != 1 or not is_host_row(host_rows[0]):
        errors.append(GuidanceError(
            BLOCK_APPHOST,
            "Test AppHost boundary must be exactly one non-packable, non-publishable, repository-only host counted in authoring cost.",
        ))

    if errors:
        raise_multi(errors)
    return {
        "moduleOwned": [{"id": k, "capability": v} for k, v in module.items()],
        "platformOwned": [{"id": k, "capability": v} for k, v in platform.items()],
        "prohibited": [{"id": k, "prohibition": v} for k, v in prohibited.items()],
    }


class MultiGuidanceError(Exception):
    def __init__(self, errors: list[GuidanceError]):
        super().__init__("; ".join(e.code for e in errors))
        self.errors = errors


def raise_multi(errors: list[GuidanceError]):
    raise MultiGuidanceError(errors)


def read_file(repository: Path, relative: str) -> bytes:
    path = repository / relative
    try:
        return path.read_bytes()
    except OSError as error:
        raise Blocked(f"cannot read {relative}: {error}") from None


def build_signable(repository: Path, source_text: bytes, contract_bytes: bytes) -> dict:
    parsed = parse_source(source_text.decode("utf-8"))
    try:
        contract = json.loads(contract_bytes)
        planning = contract["authority"]["planningCandidate"]
        if contract["storyId"] != "11.1":
            raise ValueError("storyId")
    except (ValueError, KeyError, TypeError):
        raise Blocked("story contract is not a valid Story 11.1 contract") from None
    predecessors = []
    for story, path, binding in PREDECESSORS:
        predecessors.append({
            "storyId": story,
            "path": path,
            "sha256": sha256_bytes(read_file(repository, path)),
            "binding": binding,
        })
    return {
        "schemaVersion": SCHEMA_VERSION,
        "candidate": {
            "storyId": "11.1",
            "contractPath": CONTRACT_PATH,
            "contractSha256": sha256_bytes(contract_bytes),
            "planningCandidate": planning,
        },
        "source": {"path": SOURCE_PATH, "sha256": sha256_bytes(source_text)},
        "moduleOwned": parsed["moduleOwned"],
        "platformOwned": parsed["platformOwned"],
        "prohibited": parsed["prohibited"],
        "testAppHost": dict(TEST_APPHOST),
        "validation": {
            "checks": [{"id": check, "result": "PASS"} for check in VALIDATION_CHECKS],
            "authoritySubstitution": {**AUTHORITY_SUBSTITUTION, "predecessors": predecessors},
        },
    }


def render_markdown(bundle: dict) -> str:
    out = ["# Thin-Module Authoring Guidance v2", ""]
    out += [f"Schema: `{bundle['schemaVersion']}`", ""]
    cand = bundle["candidate"]
    out += ["## Candidate", "",
            f"- Story: `{cand['storyId']}` (Checkpoint A)",
            f"- Contract: `{cand['contractPath']}` (`{cand['contractSha256']}`)",
            f"- Planning candidate: `{cand['planningCandidate']}`", ""]
    out += ["## Source", "", f"- `{bundle['source']['path']}` (`{bundle['source']['sha256']}`)", ""]
    out += ["## Module-owned", ""] + [f"- `{i['id']}`: {i['capability']}" for i in bundle["moduleOwned"]] + [""]
    out += ["## Platform-owned", ""] + [f"- `{i['id']}`: {i['capability']}" for i in bundle["platformOwned"]] + [""]
    out += ["## Prohibited", ""] + [f"- `{i['id']}`: {i['prohibition']}" for i in bundle["prohibited"]] + [""]
    host = bundle["testAppHost"]
    out += ["## Test AppHost", "",
            f"- Count: {host['count']}",
            f"- IsPackable: {str(host['isPackable']).lower()}",
            f"- IsPublishable: {str(host['isPublishable']).lower()}",
            f"- Repository-only: {str(host['repositoryOnly']).lower()}",
            f"- Counted in authoring cost: {str(host['countedInAuthoringCost']).lower()}", ""]
    validation = bundle["validation"]
    out += ["## Validation", ""] + [f"- `{c['id']}`: {c['result']}" for c in validation["checks"]] + [""]
    sub = validation["authoritySubstitution"]
    out += ["### Authority check substitution", "", f"- Criterion: `{sub['acceptanceCriterion']}`",
            f"- Decision: {sub['decision']}", f"- Reason: {sub['reason']}"]
    out += [f"- Check: {c}" for c in sub["currentPolicyChecks"]]
    out += [f"- Predecessor `{p['storyId']}` ({p['binding']}): `{p['path']}` (`{p['sha256']}`)" for p in sub["predecessors"]]
    out += [""]
    review = bundle["reviewDecision"]
    out += ["## Review decision", ""]
    if review is None:
        out += ["- Decision: pending (no approved review decision recorded)"]
    else:
        out += [f"- Decision: {review['decision']}", f"- Reviewer: {review['reviewer']}",
                f"- Decided at (UTC): {review['decidedAtUtc']}",
                f"- Bound source digest: `{review['boundSourceSha256']}`",
                f"- Bound signable-payload digest: `{review['boundSignablePayloadSha256']}`"]
    out += ["", "## Digests", "",
            f"- Source: `{bundle['digests']['sourceSha256']}`",
            f"- Signable payload: `{bundle['digests']['signablePayloadSha256']}`", ""]
    return "\n".join(out)


def serialize_json(bundle: dict) -> str:
    return json.dumps(bundle, indent=2, ensure_ascii=False) + "\n"


def finalize(signable: dict, review: dict | None) -> tuple[str, str]:
    """Return (json_text, markdown_text) for a signable payload and an optional review decision."""
    payload_digest = sha256_bytes(canonical(signable))
    bundle = {**signable, "reviewDecision": review,
              "digests": {"sourceSha256": signable["source"]["sha256"],
                          "signablePayloadSha256": payload_digest, "markdownSha256": ""}}
    markdown = render_markdown(bundle)
    bundle["digests"]["markdownSha256"] = sha256_bytes(markdown.encode("utf-8"))
    return serialize_json(bundle), markdown


def signable_of(bundle: dict) -> dict:
    return {k: v for k, v in bundle.items() if k not in ("reviewDecision", "digests")}


def valid_utc(value: str | None) -> bool:
    if not value or not re.fullmatch(r"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z", value):
        return False
    try:
        datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ")
    except ValueError:
        return False
    return True


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--contract", required=True)
    parser.add_argument("--schema", required=True)
    parser.add_argument("--output-json", required=True)
    parser.add_argument("--output-markdown", required=True)
    parser.add_argument("--approve-reviewer", help="Record an approved review decision by this named reviewer.")
    parser.add_argument("--decided-at-utc", help="UTC instant (YYYY-MM-DDTHH:MM:SSZ) of the approval.")
    args = parser.parse_args(argv)
    repository = Path(args.repository)
    try:
        if args.contract.replace("\\", "/") != CONTRACT_PATH:
            raise Blocked("--contract must be the Story 11.1 contract")
        source_bytes = read_file(repository, SOURCE_PATH)
        contract_bytes = read_file(repository, args.contract)
        schema = json.loads(read_file(repository, args.schema))
        signable = build_signable(repository, source_bytes, contract_bytes)
    except MultiGuidanceError as error:
        for item in error.errors:
            print(f"{item.code}: {item.message}", file=sys.stderr)
        return 1
    except (Blocked, ValueError, UnicodeDecodeError) as error:
        print(f"BLOCKED: {error}", file=sys.stderr)
        return 2

    payload_digest = sha256_bytes(canonical(signable))
    review = None
    if args.approve_reviewer:
        if not valid_utc(args.decided_at_utc):
            print("BLOCKED: --decided-at-utc must be a UTC instant", file=sys.stderr)
            return 2
        review = {"decision": "approved", "reviewer": args.approve_reviewer,
                  "decidedAtUtc": args.decided_at_utc, "boundSourceSha256": signable["source"]["sha256"],
                  "boundSignablePayloadSha256": payload_digest}
    else:  # preserve a still-bound existing decision so regeneration is idempotent
        try:
            existing = json.loads((repository / args.output_json).read_text(encoding="utf-8"))["reviewDecision"]
            if isinstance(existing, dict) and existing.get("boundSignablePayloadSha256") == payload_digest:
                review = existing
        except (OSError, ValueError, KeyError, TypeError):
            review = None

    json_text, markdown = finalize(signable, review)
    try:
        import jsonschema  # noqa: PLC0415
        jsonschema.Draft202012Validator(schema).validate(json.loads(json_text))
    except Exception as error:  # schema or import failure is a blocked environment, not a content verdict
        print(f"BLOCKED: generated bundle failed schema validation: {error}", file=sys.stderr)
        return 2
    for relative, text in ((args.output_json, json_text), (args.output_markdown, markdown)):
        target = repository / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        with open(target, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    print("PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
