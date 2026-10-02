#!/usr/bin/env python3
"""Generate the closed Story 8.1 UX preservation disposition bundle."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any

import jsonschema


SCHEMA_VERSION = "hexalith.conversations.ux-preservation-disposition.v1"
STATUS = "preserved-not-activated"
BANNER = ("Preservation only: these UX obligations are not activated for product UI "
          "implementation. Historical story mappings are non-current provenance; "
          "release activation requires separate approved authority.")
SPEC_PATH = "_bmad-output/planning-artifacts/ux-design-specification.md"
MAP_PATH = "_bmad-output/planning-artifacts/ux-requirement-map.md"
CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/8.1.json"
PREDECESSOR_PATH = "docs/release-evidence/story-7.4-final-record-v2.json"
EXPECTED_DECISIONS = [f"UX-DR{number}" for number in range(1, 53)]
EXPECTED_ACCEPTANCE = [
    *(f"AC-SAFE-{number:03d}" for number in range(1, 9)),
    *(f"AC-RESP-{number:03d}" for number in range(1, 16)),
    "AC-A11Y-001", "AC-A11Y-002", "AC-LEAK-001", "AC-MOB-001", "AC-PERF-001",
]
ROW_FIELDS = ("id", "status", "owner", "rationale", "sourcePath", "sourceSha256",
              "evidenceOrControl", "historicalMappings", "compatibility", "disclosureSafety")


class DispositionError(Exception):
    """A stable, content-safe disposition blocker."""

    def __init__(self, code: str, detail: str) -> None:
        super().__init__(detail)
        self.code = code


def digest(content: bytes) -> str:
    """Return the lowercase SHA-256 digest of exact bytes."""
    return hashlib.sha256(content).hexdigest()


def file_bytes(root: Path, relative: str) -> bytes:
    """Read a regular repository file without following a path outside the root."""
    path = root / relative
    try:
        if not path.resolve(strict=True).is_relative_to(root) or not path.is_file():
            raise OSError("unsafe source")
        return path.read_bytes()
    except OSError as error:
        raise DispositionError("UX_SOURCE_UNBOUND", f"source is missing or unreadable: {relative}") from error


def frontmatter_value(content: str, key: str) -> str:
    """Extract exactly one scalar identity from YAML frontmatter."""
    match = re.match(r"\A---\n(.*?)\n---\n", content, re.S)
    if match is None:
        raise DispositionError("UX_SOURCE_UNBOUND", f"source frontmatter is missing: {key}")
    values = re.findall(rf"^{re.escape(key)}:\s*([^\n]+)$", match.group(1), re.M)
    if len(values) != 1 or not values[0].strip():
        raise DispositionError("UX_SOURCE_UNBOUND", f"source version is missing: {key}")
    return values[0].strip().strip("'\"")


def schema() -> dict[str, Any]:
    """Return the one closed disposition schema."""
    string = {"type": "string", "minLength": 1}
    sha = {"type": "string", "pattern": "^[0-9a-f]{64}$"}
    path = {"type": "string", "pattern": "^[^/][^\\\\]*$", "minLength": 1}
    mapping = {
        "type": "object", "additionalProperties": False,
        "required": ["reference", "classification", "current"],
        "properties": {"reference": string, "classification": {"const": "historical-provenance"},
                       "current": {"const": False}},
    }
    row = {
        "type": "object", "additionalProperties": False, "required": list(ROW_FIELDS),
        "properties": {
            "id": string, "status": {"const": STATUS}, "owner": string,
            "rationale": string, "sourcePath": path, "sourceSha256": sha,
            "evidenceOrControl": string,
            "historicalMappings": {"type": "array", "items": mapping, "minItems": 1},
            "compatibility": string, "disclosureSafety": string,
        },
    }
    source = {
        "type": "object", "additionalProperties": False,
        "required": ["path", "version", "sha256"],
        "properties": {"path": path, "version": string, "sha256": sha},
    }
    return {
        "$schema": "https://json-schema.org/draft/2020-12/schema",
        "$id": "https://hexalith.io/schemas/conversations/ux-preservation-disposition-v1.schema.json",
        "title": "Conversations UX Preservation Disposition V1",
        "type": "object", "additionalProperties": False,
        "required": ["schemaVersion", "authority", "candidate", "sources", "status",
                     "preservationBanner", "decisions", "acceptanceCriteria",
                     "historicalProvenance", "renderedMarkdownSha256"],
        "properties": {
            "schemaVersion": {"const": SCHEMA_VERSION},
            "authority": {
                "type": "object", "additionalProperties": False,
                "required": ["epic", "architecture", "planningCandidate", "bundleDigest",
                             "inventoryId", "inventorySha256", "contractPath", "contractSha256"],
                "properties": {
                    "epic": string, "architecture": string, "planningCandidate": string,
                    "bundleDigest": sha, "inventoryId": string, "inventorySha256": sha,
                    "contractPath": path, "contractSha256": sha,
                },
            },
            "candidate": {
                "type": "object", "additionalProperties": False,
                "required": ["storyId", "bindingRule", "predecessorPath", "predecessorSha256"],
                "properties": {"storyId": {"const": "8.1"},
                               "bindingRule": {"const": "SC-8.1 is HEAD^{commit} at final-record generation"},
                               "predecessorPath": {"const": PREDECESSOR_PATH},
                               "predecessorSha256": sha},
            },
            "sources": {"type": "array", "items": False, "minItems": 2, "maxItems": 2,
                        "prefixItems": [source, source]},
            "status": {"const": STATUS},
            "preservationBanner": {"const": BANNER},
            "decisions": {"type": "array", "items": row, "minItems": 52, "maxItems": 52},
            "acceptanceCriteria": {"type": "array", "items": row, "minItems": 28, "maxItems": 28},
            "historicalProvenance": {
                "type": "object", "additionalProperties": False,
                "required": ["classification", "currentImplementationOwner", "note"],
                "properties": {"classification": {"const": "non-current"},
                               "currentImplementationOwner": {"const": False}, "note": string},
            },
            "renderedMarkdownSha256": sha,
        },
    }


def parse_table(content: str, heading: str, code: str) -> list[list[str]]:
    """Extract one canonical Markdown table in source order."""
    section = re.search(rf"^## {re.escape(heading)}\n(.*?)(?=^## |\Z)", content, re.M | re.S)
    if section is None:
        raise DispositionError(code, f"missing {heading} table")
    rows = []
    for line in section.group(1).splitlines():
        if not line.startswith("| ") or line.startswith("| ---") or line.startswith("| UX-DR | ") or line.startswith("| Criterion | "):
            continue
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        rows.append(cells)
    return rows


def inventory(root: Path, spec: str, requirement_map: str, source_hashes: dict[str, str]) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    """Project the exact frozen decision and acceptance inventories."""
    decisions_table = parse_table(requirement_map, "UX Decision Inventory", "UX_DECISION_INVENTORY_DRIFT")
    acceptance_table = parse_table(requirement_map, "Generated Acceptance-Criterion Inventory", "UX_ACCEPTANCE_INVENTORY_DRIFT")
    source_acceptance = re.findall(r"^- \*\*(AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}):\*\* (.+)$", spec, re.M)
    if [row[0] for row in decisions_table] != EXPECTED_DECISIONS or any(len(row) != 5 for row in decisions_table):
        raise DispositionError("UX_DECISION_INVENTORY_DRIFT", "the decision inventory differs from UX-DR1 through UX-DR52 in source order")
    if ([row[0] for row in acceptance_table] != EXPECTED_ACCEPTANCE
            or [row[0] for row in source_acceptance] != EXPECTED_ACCEPTANCE
            or any(len(row) != 4 for row in acceptance_table)):
        raise DispositionError("UX_ACCEPTANCE_INVENTORY_DRIFT", "the acceptance inventories differ from the frozen 28 IDs")

    def mapping(value: str, code: str) -> list[dict[str, Any]]:
        if not (value.startswith("Historical ") or value.startswith("Historical: ")):
            raise DispositionError(code, "a historical mapping lost its provenance label")
        return [{"reference": value, "classification": "historical-provenance", "current": False}]

    def disposition(value: str, code: str) -> str:
        if value != f"{STATUS}; Stories 8.1-8.2 preservation contract":
            raise DispositionError("UX_ACTIVATION_UNAUTHORIZED" if not value.startswith(STATUS) else code,
                                   "a source row has an unauthorized disposition or owner")
        return "Stories 8.1-8.2 preservation contract"

    decisions = []
    for identifier, section, summary, state, historical in decisions_table:
        decisions.append({"id": identifier, "status": STATUS,
                          "owner": disposition(state, "UX_CURRENT_STORY_INVALID"),
                          "rationale": summary, "sourcePath": MAP_PATH,
                          "sourceSha256": source_hashes[MAP_PATH],
                          "evidenceOrControl": f"{MAP_PATH}#UX-Decision-Inventory:{section}",
                          "historicalMappings": mapping(historical, "UX_CURRENT_STORY_INVALID"),
                          "compatibility": "Preserved obligation; future activation requires separate authorization.",
                          "disclosureSafety": "No product disclosure or UI implementation is authorized."})
    acceptance = []
    for (identifier, section, state, historical), (_, requirement) in zip(acceptance_table, source_acceptance):
        acceptance.append({"id": identifier, "status": STATUS,
                           "owner": disposition(state, "UX_CURRENT_STORY_INVALID"),
                           "rationale": requirement, "sourcePath": SPEC_PATH,
                           "sourceSha256": source_hashes[SPEC_PATH],
                           "evidenceOrControl": f"{SPEC_PATH}#{section}:{identifier}",
                           "historicalMappings": mapping(historical, "UX_CURRENT_STORY_INVALID"),
                           "compatibility": "Preserved acceptance obligation; no feature-delivery claim.",
                           "disclosureSafety": "No product disclosure or UI implementation is authorized."})
    return decisions, acceptance


def markdown(document: dict[str, Any]) -> bytes:
    """Render deterministic Markdown without embedding its own digest."""
    lines = ["# UX Preservation Disposition V1", "", f"> **{BANNER}**", "",
             f"Status: `{STATUS}`", "", "## Source bindings", "",
             "| Path | Version | SHA-256 |", "| --- | --- | --- |"]
    for source in document["sources"]:
        lines.append(f"| `{source['path']}` | `{source['version']}` | `{source['sha256']}` |")
    for title, rows in (("Decisions", document["decisions"]), ("Acceptance criteria", document["acceptanceCriteria"])):
        lines.extend(["", f"## {title}", "", "| ID | Disposition | Owner | Rationale | Source | Historical mapping |",
                      "| --- | --- | --- | --- | --- | --- |"])
        for row in rows:
            safe = lambda value: str(value).replace("|", "\\|").replace("\n", " ")
            lines.append(f"| `{row['id']}` | `{row['status']}` | {safe(row['owner'])} | {safe(row['rationale'])} | "
                         f"`{row['sourcePath']}` (`{row['sourceSha256']}`) | "
                         f"{safe(row['historicalMappings'][0]['reference'])} (non-current) |")
    lines.extend(["", "Historical mappings are non-current provenance and carry no implementation ownership.", ""])
    return "\n".join(lines).encode("utf-8")


def generate(root: Path, contract_path: str) -> tuple[bytes, bytes, bytes]:
    """Derive and validate schema, JSON, and Markdown from canonical inputs."""
    if contract_path != CONTRACT_PATH:
        raise DispositionError("UX_SCHEMA_INVALID", "the Story 8.1 contract path is not canonical")
    source_bytes = {path: file_bytes(root, path) for path in (SPEC_PATH, MAP_PATH)}
    try:
        source_text = {path: data.decode("utf-8") for path, data in source_bytes.items()}
        contract_bytes = file_bytes(root, contract_path)
        contract = json.loads(contract_bytes)
        predecessor_bytes = file_bytes(root, PREDECESSOR_PATH)
        predecessor = json.loads(predecessor_bytes)
        bundle = json.loads(file_bytes(root, "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"))
    except (UnicodeError, ValueError) as error:
        raise DispositionError("UX_SCHEMA_INVALID", "an authority input is malformed or the candidate is unresolved") from error
    if (contract.get("storyId") != "8.1" or contract.get("predecessors") != ["7.4"]
            or predecessor.get("storyId") != "7.4"
            or bundle.get("planningCandidate") != contract["authority"]["planningCandidate"]):
        raise DispositionError("UX_SCHEMA_INVALID", "the Story 8.1 authority or predecessor binding is invalid")
    for source_path, content in source_text.items():
        if "> **Preservation-only UX authority.**" not in content:
            raise DispositionError("UX_ACTIVATION_UNAUTHORIZED", f"preservation banner is missing: {source_path}")
    hashes = {path: digest(data) for path, data in source_bytes.items()}
    decisions, acceptance = inventory(root, source_text[SPEC_PATH], source_text[MAP_PATH], hashes)
    predecessor_candidate = predecessor.get("candidate", {}).get("commit")
    if not isinstance(predecessor_candidate, str) or not re.fullmatch(r"[0-9a-f]{40}", predecessor_candidate):
        raise DispositionError("UX_SCHEMA_INVALID", "the predecessor candidate is invalid")
    for source_path, current_bytes in source_bytes.items():
        original = subprocess.run(
            ["git", "show", f"{predecessor_candidate}:{source_path}"],
            cwd=root, capture_output=True, check=False,
        )
        if original.returncode != 0:
            raise DispositionError("UX_SOURCE_UNBOUND", f"predecessor source is unavailable: {source_path}")
        if original.stdout != current_bytes:
            raise DispositionError("UX_SOURCE_DRIFT", f"canonical source changed: {source_path}")
    sources = [
        {"path": SPEC_PATH, "version": frontmatter_value(source_text[SPEC_PATH], "preservationAuthorityVersion"), "sha256": hashes[SPEC_PATH]},
        {"path": MAP_PATH, "version": frontmatter_value(source_text[MAP_PATH], "authorityVersion"), "sha256": hashes[MAP_PATH]},
    ]
    document = {
        "schemaVersion": SCHEMA_VERSION,
        "authority": {"epic": contract["authority"]["epic"],
                      "architecture": contract["authority"]["architecture"],
                      "planningCandidate": contract["authority"]["planningCandidate"],
                      "bundleDigest": bundle["bundleDigest"],
                      "inventoryId": contract["inventory"]["id"],
                      "inventorySha256": contract["inventory"]["sha256"],
                      "contractPath": CONTRACT_PATH, "contractSha256": digest(contract_bytes)},
        "candidate": {"storyId": "8.1", "bindingRule": "SC-8.1 is HEAD^{commit} at final-record generation",
                      "predecessorPath": PREDECESSOR_PATH, "predecessorSha256": digest(predecessor_bytes)},
        "sources": sources, "status": STATUS, "preservationBanner": BANNER,
        "decisions": decisions, "acceptanceCriteria": acceptance,
        "historicalProvenance": {"classification": "non-current", "currentImplementationOwner": False,
                                 "note": "Historical story references are navigation only; they cannot authorize implementation."},
        "renderedMarkdownSha256": "0" * 64,
    }
    markdown_bytes = markdown(document)
    document["renderedMarkdownSha256"] = digest(markdown_bytes)
    schema_document = schema()
    try:
        jsonschema.Draft202012Validator.check_schema(schema_document)
        jsonschema.validate(document, schema_document)
    except jsonschema.ValidationError as error:
        raise DispositionError("UX_SCHEMA_INVALID", f"generated disposition violates its schema: {error.json_path}") from error
    schema_bytes = (json.dumps(schema_document, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    json_bytes = (json.dumps(document, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    if markdown(document) != markdown_bytes or digest(markdown_bytes) != document["renderedMarkdownSha256"]:
        raise DispositionError("UX_RENDER_DRIFT", "the Markdown rendering is not deterministic")
    return schema_bytes, json_bytes, markdown_bytes


def main() -> int:
    """Run the exact Story 8.1 generator command."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--contract", required=True)
    parser.add_argument("--output-schema", required=True)
    parser.add_argument("--output-json", required=True)
    parser.add_argument("--output-markdown", required=True)
    args = parser.parse_args()
    root = Path(args.repository).resolve()
    try:
        if not (root / ".git").exists():
            raise DispositionError("UX_SCHEMA_INVALID", "repository is not a Git checkout")
        outputs = (args.output_schema, args.output_json, args.output_markdown)
        expected = ("docs/release-evidence/ux-preservation-disposition-v1.schema.json",
                    "docs/release-evidence/ux-preservation-disposition-v1.json",
                    "docs/release-evidence/ux-preservation-disposition-v1.md")
        if outputs != expected:
            raise DispositionError("UX_SCHEMA_INVALID", "output paths differ from the canonical bundle")
        generated = generate(root, args.contract)
        for relative, content in zip(outputs, generated):
            target = root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
        print("PASS: UX preservation disposition bundle generated")
        return 0
    except DispositionError as error:
        print(f"FAIL: {error.code}: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
