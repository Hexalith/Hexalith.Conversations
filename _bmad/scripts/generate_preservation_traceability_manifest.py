#!/usr/bin/env python3
"""Generate and validate the preservation traceability manifest v2."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import re
import subprocess
import sys
from dataclasses import dataclass
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any, Iterable


MANIFEST_PATH = Path("docs/release-evidence/preservation-traceability-manifest-v2.json")
MARKDOWN_PATH = Path("docs/release-evidence/preservation-traceability-manifest-v2.md")
DISPOSITION_PATH = Path("docs/release-evidence/preservation-non-activation-disposition-v2.json")
SCHEMA_PATH = Path("docs/release-evidence/preservation-traceability-manifest-v2.schema.json")

PRD_PATH = Path("_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md")
UX_MAP_PATH = Path("_bmad-output/planning-artifacts/ux-requirement-map.md")
UX_SPEC_PATH = Path("_bmad-output/planning-artifacts/ux-design-specification.md")
ARCHITECTURE_PATH = Path("_bmad-output/planning-artifacts/architecture.md")
EPIC_CONTEXT_PATH = Path("_bmad-output/implementation-artifacts/epic-6-context.md")
EPICS_PATH = Path("_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md")
TIER_DECISION_PATH = Path("docs/release-evidence/conformance-oracle-tiering-decision-v2.json")
CONTRACT_BASELINE_PATH = Path("docs/release-evidence/public-contract-shape-baseline-v1.json")
CANDIDATE_EVIDENCE_PATH = Path("TestResults/story-6-3-preservation-candidate-v2.json")

OBLIGATION_KINDS = (
    "initiative-fr",
    "feature-fr",
    "feature-nfr",
    "ux-decision",
    "ux-acceptance",
    "public-contract",
    "public-client",
    "current-control",
    "conformance-assertion",
)

SOURCE_BINDING_ROLES = {
    "obligation-authority",
    "cumulative-epic-authority",
    "tier-decision",
    "machine-contract",
    "solution-inventory",
    "build-authority",
}

CLOSING_DISPOSITIONS = {
    "preserved-non-activation",
    "delivered-to-inactive",
    "compatible-change",
}

NON_CLOSING_DISPOSITIONS = {
    "evidence-mapping",
    "story-6.9-tier-assignment",
}

EXPECTED_BASE_COUNTS = {
    "initiative-fr": 20,
    "feature-fr": 104,
    "feature-nfr": 77,
    "ux-decision": 52,
    "ux-acceptance": 52,
}

UX_ACCEPTANCE_SECTIONS = (
    "Design System Acceptance Criteria",
    "2.3 Success Criteria",
    "Safety Acceptance Criteria",
    "Responsive Acceptance Criteria",
)

FROZEN_V1_HASHES = {
    "docs/release-evidence/manifest.schema.json": "a7b22c8ec7eca96ed75b831a3e37e938c163468f46a0ac7d0f53e8f8ab7a99de",
    "docs/release-evidence/conformance-manifest-v1-fixture.json": "a26e44fbe0a19bea522864d654e2e38901e74d3e482f8f56f49bcfb35c59ee3f",
    "docs/release-evidence/release-baseline-v1.json": "a3f0b4a76aa99226dfb6a7d9a0c930f30705c4d4f8d8c32f97a5b3124a335932",
    "docs/release-evidence/release-baseline-v1.md": "183b392e8090619f2a40c7defe72679718c53c2832e3b6961b5308ba62e9f8f4",
    "docs/release-evidence/public-contract-shape-baseline-v1.json": "ebfc2f67e90ecc8a7734719c6e2673b6e8392ab2cae9956a8e98b7bf769acfca",
    "docs/release-evidence/success-metric-report-and-attestation-v1.json": "062ca0c7bc94279007077bda59eae867d21c12da2ffc0b59a0f389b99067e0fe",
    "docs/release-evidence/success-metric-report-and-attestation-v1.md": "aa7e52c11ce36fc2c9ea953e275c654e7f312016c990cb20be16666d87f9a2cd",
    "docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.json": "8091f6c26251420242a491cad100472dc1604a7163cc9d8df51bb1c742844856",
    "docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.md": "a73077c0b5416c5085796c2e808a45efe09f5eb6a4ddf852214ecc93a9209e0b",
}

CONTROL_DEFINITIONS = (
    (
        "CTRL-MODULE-PLATFORM-OWNERSHIP",
        "shared",
        EPIC_CONTEXT_PATH,
        "Conversations owns contracts, aggregate/domain behavior",
        "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
        "structural-test-source",
    ),
    (
        "CTRL-CANONICAL-HOST-SHAPE",
        "platform",
        EPIC_CONTEXT_PATH,
        "builder.AddEventStoreDomainService",
        "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
        "structural-test-source",
    ),
    (
        "CTRL-TEST-ONLY-APPHOST",
        "module",
        EPIC_CONTEXT_PATH,
        "target test infrastructure",
        "tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs",
        "boundary-test-source",
    ),
    (
        "CTRL-PROJECTION-POPULATION",
        "module",
        EPIC_CONTEXT_PATH,
        "Projection Read-Store Population",
        "tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs",
        "evidence-validation-test-source",
    ),
    (
        "CTRL-SM-C2-V6",
        "release-governance",
        EPIC_CONTEXT_PATH,
        "Amended pass rule (v6, 2026-07-31)",
        "tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs",
        "performance-validation-test-source",
    ),
    (
        "CTRL-PROMOTION-GATE",
        "release-governance",
        EPIC_CONTEXT_PATH,
        "Promotion Completion Invariant",
        "tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs",
        "workflow-gate-test-source",
    ),
    (
        "CTRL-FINAL-RECORD-GATE",
        "release-governance",
        EPIC_CONTEXT_PATH,
        "Final Record Invariant",
        "tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs",
        "workflow-gate-test-source",
    ),
    (
        "CTRL-IMMUTABLE-V1",
        "release-governance",
        EPIC_CONTEXT_PATH,
        "signed v1 evidence remain immutable",
        "tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs",
        "immutable-evidence-test-source",
    ),
    (
        "CTRL-CONTRACTS-SURFACE",
        "module",
        ARCHITECTURE_PATH,
        "defines commands, projections, events, typed errors",
        "tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs",
        "contract-surface-test-source",
    ),
    (
        "CTRL-CLIENT-SURFACE",
        "module",
        ARCHITECTURE_PATH,
        "wraps public API contracts only",
        "tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs",
        "client-boundary-test-source",
    ),
    (
        "CTRL-ROUTE-WIRE-BEHAVIOR",
        "module",
        "src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs",
        "MapGroup(\"/api/v1/conversations\")",
        "tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs",
        "route-wire-test-source",
    ),
    (
        "CTRL-EVENT-WIRE-BEHAVIOR",
        "module",
        ARCHITECTURE_PATH,
        "Unsupported event, command, or projection versions return typed documented errors",
        "tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuiteTest.cs",
        "event-schema-test-source",
    ),
    (
        "CTRL-ERROR-SEMANTICS",
        "module",
        ARCHITECTURE_PATH,
        "Return adopter-facing typed results/errors without EventStore leakage",
        "tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuiteTest.cs",
        "error-contract-test-source",
    ),
    (
        "CTRL-PACKAGE-VERSION-BEHAVIOR",
        "platform",
        ARCHITECTURE_PATH,
        "Central package management controls dependency versions",
        "tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs",
        "package-policy-test-source",
    ),
    (
        "CTRL-ORACLE-TIERING",
        "release-governance",
        EPIC_CONTEXT_PATH,
        "Conformance Oracle Tier Invariant",
        "tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs",
        "oracle-denominator-test-source",
    ),
)

EPIC_CONTROL_SECTIONS = {
    "Corrected Ownership Spine",
    "Still-Binding Safety Decisions",
    "Projection Read-Store Population (ADR 0003)",
    "SM-C2 Contract",
    "Final Record Invariant",
    "Promotion Completion Invariant",
    "Conformance Oracle Tier Invariant",
}

TOP_LEVEL_PROPERTIES = {
    "$schema",
    "artifact",
    "schemaVersion",
    "manifestVersion",
    "status",
    "supersession",
    "authorityVersions",
    "sourceBindings",
    "identityBindings",
    "immutableV1Bindings",
    "governanceInputs",
    "tiering",
    "mutationGovernance",
    "summaries",
    "obligations",
}


@dataclass(frozen=True)
class Diagnostic:
    """A stable validation diagnostic."""

    code: str
    subject: str
    message: str


class AuthorityExtractionError(ValueError):
    """A stable, source-derived denominator or catalog extraction failure."""

    def __init__(self, code: str, subject: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.subject = subject
        self.message = message


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def sha256_text(text: str) -> str:
    return sha256_bytes(text.encode("utf-8"))


def canonical_json(value: Any) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False, sort_keys=False) + "\n"


def compact_json(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def parse_iso_date(value: Any) -> date | None:
    if not isinstance(value, str):
        return None
    try:
        return date.fromisoformat(value)
    except ValueError:
        return None


def parse_iso_datetime(value: Any) -> datetime | None:
    if not isinstance(value, str):
        return None
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        return None
    if parsed.tzinfo is None:
        return None
    return parsed.astimezone(timezone.utc)


def git_output(root: Path, *args: str) -> str | None:
    completed = subprocess.run(
        ["git", "-C", str(root), *args],
        check=False,
        capture_output=True,
        text=True,
        timeout=30,
    )
    if completed.returncode != 0:
        return None
    return completed.stdout.strip()


def normalize_text(value: str) -> str:
    return " ".join(value.split())


def relative_text(root: Path, path: Path | str) -> str:
    candidate = Path(path)
    if candidate.is_absolute():
        candidate = candidate.relative_to(root)
    return candidate.as_posix()


def source_record(
    root: Path,
    path: Path | str,
    *,
    section: str,
    line: int,
    text: str,
    ordinal: int | None = None,
    text_hash: str | None = None,
) -> dict[str, Any]:
    relative = relative_text(root, path)
    normalized = normalize_text(text)
    record: dict[str, Any] = {
        "path": relative,
        "section": section,
        "line": line,
        "text": normalized,
        "textSha256": text_hash or sha256_text(normalized),
        "fileSha256": sha256_file(root / relative),
    }
    if ordinal is not None:
        record["ordinal"] = ordinal
    return record


def require_exact_ids(rows: list[dict[str, Any]], expected: list[str], subject: str) -> None:
    actual = [row.get("id") for row in rows]
    if actual != expected:
        missing = sorted(set(expected) - set(actual))
        duplicates = sorted({value for value in actual if actual.count(value) > 1})
        unknown = sorted(set(actual) - set(expected))
        raise AuthorityExtractionError(
            "DENOMINATOR_SOURCE_INVALID",
            subject,
            f"Expected exact IDs; missing={missing}, duplicates={duplicates}, unknown={unknown}.",
        )


def extract_initiative_frs(root: Path) -> list[dict[str, Any]]:
    lines = (root / PRD_PATH).read_text(encoding="utf-8").splitlines()
    rows: list[dict[str, Any]] = []
    heading = re.compile(r"^#### (FR-(\d+)):\s*(.+)$")
    for index, line in enumerate(lines):
        match = heading.match(line)
        if match is None:
            continue
        end = index + 1
        while end < len(lines) and not lines[end].startswith("#### FR-") and not lines[end].startswith("## 7."):
            end += 1
        block = "\n".join([line, *lines[index + 1 : end]]).strip()
        rows.append(
            {
                "id": match.group(1),
                "kind": "initiative-fr",
                "title": normalize_text(match.group(3)),
                "source": source_record(
                    root,
                    PRD_PATH,
                    section="6. Features",
                    line=index + 1,
                    text=block,
                ),
            }
        )
    rows = sorted(rows, key=lambda row: int(row["id"].split("-")[-1]))
    require_exact_ids(rows, [f"FR-{ordinal}" for ordinal in range(1, 21)], PRD_PATH.as_posix())
    return rows


def extract_feature_requirements(root: Path) -> dict[str, list[dict[str, Any]]]:
    lines = (root / PRD_PATH).read_text(encoding="utf-8").splitlines()
    pattern = re.compile(r"^- \*\*(Feature-(FR|NFR)(\d+)):\*\*\s*(.+)$")
    result: dict[str, list[dict[str, Any]]] = {"feature-fr": [], "feature-nfr": []}
    section = ""
    for index, line in enumerate(lines):
        if line.startswith("#### "):
            section = line[5:].strip()
        match = pattern.match(line)
        if match is None:
            continue
        kind = "feature-fr" if match.group(2) == "FR" else "feature-nfr"
        result[kind].append(
            {
                "id": match.group(1),
                "kind": kind,
                "title": normalize_text(match.group(4)),
                "source": source_record(
                    root,
                    PRD_PATH,
                    section=section,
                    line=index + 1,
                    text=match.group(4),
                ),
            }
        )
    for rows in result.values():
        rows.sort(key=lambda row: int(re.search(r"\d+$", row["id"]).group()))
    require_exact_ids(result["feature-fr"], [f"Feature-FR{ordinal}" for ordinal in range(1, 105)], PRD_PATH.as_posix())
    require_exact_ids(result["feature-nfr"], [f"Feature-NFR{ordinal}" for ordinal in range(1, 78)], PRD_PATH.as_posix())
    return result


def extract_ux_decisions(root: Path) -> list[dict[str, Any]]:
    lines = (root / UX_MAP_PATH).read_text(encoding="utf-8").splitlines()
    pattern = re.compile(r"^\| (UX-DR(\d+)) \| ([^|]+) \| ([^|]+) \|")
    rows: list[dict[str, Any]] = []
    for index, line in enumerate(lines):
        match = pattern.match(line)
        if match is None:
            continue
        text = f"{match.group(3).strip()}: {match.group(4).strip()}"
        rows.append(
            {
                "id": match.group(1),
                "kind": "ux-decision",
                "title": normalize_text(match.group(4)),
                "source": source_record(
                    root,
                    UX_MAP_PATH,
                    section="UX Requirement Map",
                    line=index + 1,
                    text=text,
                ),
            }
        )
    rows = sorted(rows, key=lambda row: int(row["id"].replace("UX-DR", "")))
    require_exact_ids(rows, [f"UX-DR{ordinal}" for ordinal in range(1, 53)], UX_MAP_PATH.as_posix())
    return rows


def slug(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")


def extract_ux_acceptance(root: Path) -> list[dict[str, Any]]:
    lines = (root / UX_SPEC_PATH).read_text(encoding="utf-8").splitlines()
    rows: list[dict[str, Any]] = []
    for section in UX_ACCEPTANCE_SECTIONS:
        heading_indices = [index for index, line in enumerate(lines) if line == f"### {section}"]
        if not heading_indices:
            raise AuthorityExtractionError(
                "UX_SECTION_MISSING",
                section,
                f"Normative UX heading '### {section}' is missing.",
            )
        if len(heading_indices) != 1:
            raise AuthorityExtractionError(
                "UX_SECTION_DUPLICATE",
                section,
                f"Normative UX heading '### {section}' occurs {len(heading_indices)} times.",
            )
        heading_index = heading_indices[0]
        section_rows: list[tuple[int, str]] = []
        for index in range(heading_index + 1, len(lines)):
            line = lines[index]
            if line.startswith("### "):
                break
            if line.startswith("- "):
                section_rows.append((index, line[2:].strip()))
        for ordinal, (index, text) in enumerate(section_rows, start=1):
            normalized = normalize_text(text)
            text_hash = sha256_text(normalized)
            rows.append(
                {
                    "id": f"UX-AC-{slug(section)}-{ordinal:02d}-{text_hash[:12]}",
                    "kind": "ux-acceptance",
                    "title": normalized,
                    "source": source_record(
                        root,
                        UX_SPEC_PATH,
                        section=section,
                        line=index + 1,
                        text=normalized,
                        ordinal=ordinal,
                        text_hash=text_hash,
                    ),
                }
            )
    if len(rows) != 52:
        raise AuthorityExtractionError(
            "DENOMINATOR_SOURCE_INVALID",
            UX_SPEC_PATH.as_posix(),
            f"Expected exactly 52 normative UX acceptance rows and found {len(rows)}.",
        )
    return rows


def extract_contract_surface(root: Path) -> list[dict[str, Any]]:
    document = json.loads((root / CONTRACT_BASELINE_PATH).read_text(encoding="utf-8"))
    file_hash = sha256_file(root / CONTRACT_BASELINE_PATH)
    rows: list[dict[str, Any]] = []
    lines = (root / CONTRACT_BASELINE_PATH).read_text(encoding="utf-8").splitlines()
    for item in document["types"]:
        qualified_name = f"{item['namespace']}.{item['name']}"
        canonical = compact_json(item)
        line = next(
            (
                index + 1
                for index, value in enumerate(lines)
                if value.strip() == f'"name": "{item["name"]}",'
            ),
            1,
        )
        source = {
            "path": CONTRACT_BASELINE_PATH.as_posix(),
            "section": "types",
            "line": line,
            "text": qualified_name,
            "textSha256": sha256_text(canonical),
            "fileSha256": file_hash,
        }
        rows.append(
            {
                "id": f"CONTRACT-{qualified_name}",
                "kind": "public-contract",
                "title": qualified_name,
                "source": source,
            }
        )
    return sorted(rows, key=lambda row: row["id"])


def extract_client_surface(root: Path) -> list[dict[str, Any]]:
    client_root = root / "src/Hexalith.Conversations.Client"
    declaration = re.compile(
        r"^public\s+(?:(?:sealed|static|abstract|partial|readonly)\s+)*(?:class|record|interface|enum|struct)\s+([A-Za-z0-9_]+)"
    )
    rows: list[dict[str, Any]] = []
    for path in sorted(client_root.glob("*.cs")):
        lines = path.read_text(encoding="utf-8").splitlines()
        for index, line in enumerate(lines):
            match = declaration.match(line.strip())
            if match is None:
                continue
            relative = path.relative_to(root).as_posix()
            rows.append(
                {
                    "id": f"CLIENT-{match.group(1)}",
                    "kind": "public-client",
                    "title": f"Hexalith.Conversations.Client.{match.group(1)}",
                    "source": source_record(
                        root,
                        relative,
                        section="public surface",
                        line=index + 1,
                        text=line.strip(),
                        text_hash=sha256_file(path),
                    ),
                }
            )
    return sorted(rows, key=lambda row: row["id"])


def find_source_line(root: Path, path: Path | str, needle: str) -> tuple[int, str]:
    lines = (root / path).read_text(encoding="utf-8").splitlines()
    for index, line in enumerate(lines):
        if needle in line:
            return index + 1, line.strip()
    raise ValueError(f"Required control source text not found: {path}: {needle}")


def evidence_binding(root: Path, path: Path | str, evidence_type: str, source_path: Path | str) -> dict[str, str]:
    relative = relative_text(root, path)
    source_relative = relative_text(root, source_path)
    return {
        "path": relative,
        "sha256": sha256_file(root / relative),
        "evidenceType": evidence_type,
        "authorityPath": source_relative,
        "authoritySha256": sha256_file(root / source_relative),
    }


def extract_current_controls(root: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for control_id, owner, path, needle, evidence_path, evidence_type in CONTROL_DEFINITIONS:
        line, text = find_source_line(root, path, needle)
        rows.append(
            {
                "id": control_id,
                "kind": "current-control",
                "title": normalize_text(text),
                "source": source_record(
                    root,
                    path,
                    section="current control",
                    line=line,
                    text=text,
                ),
                "controlOwner": owner,
                "closure": {
                    "kind": "evidence",
                    "evidence": [evidence_binding(root, evidence_path, evidence_type, path)],
                },
            }
        )
    return rows


def extract_conformance_assertions(root: Path) -> list[dict[str, Any]]:
    test_root = root / "tests/Hexalith.Conversations.Conformance.Tests"
    method_pattern = re.compile(
        r"public\s+(?:(?:static|async)\s+)*(?:void|Task|ValueTask|IEnumerable<[^>]+>)\s+([A-Za-z0-9_]+)\s*\("
    )
    rows: list[dict[str, Any]] = []
    for path in sorted(test_root.glob("*.cs")):
        lines = path.read_text(encoding="utf-8").splitlines()
        pending_attribute: tuple[int, str] | None = None
        for index, line in enumerate(lines):
            stripped = line.strip()
            if stripped.startswith("[Fact") or stripped.startswith("[Theory"):
                pending_attribute = (index + 1, "Theory" if stripped.startswith("[Theory") else "Fact")
                continue
            if pending_attribute is None:
                continue
            match = method_pattern.search(stripped)
            if match is not None:
                method = match.group(1)
                source_text = f"[{pending_attribute[1]}] {stripped}"
                rows.append(
                    {
                        "id": f"ASSERT-{path.stem}.{method}",
                        "kind": "conformance-assertion",
                        "title": f"{path.stem}.{method}",
                        "source": source_record(
                            root,
                            path.relative_to(root),
                            section="xUnit assertion",
                            line=pending_attribute[0],
                            text=source_text,
                        ),
                        "tier": "pending-story-6.9",
                    }
                )
                pending_attribute = None
            elif stripped.startswith("[") and not stripped.startswith("[InlineData"):
                continue
            elif stripped and not stripped.startswith("//") and index + 1 - pending_attribute[0] > 20:
                pending_attribute = None
    return sorted(rows, key=lambda row: row["id"])


def extract_inventory(root: Path) -> dict[str, list[dict[str, Any]]]:
    feature = extract_feature_requirements(root)
    return {
        "initiative-fr": extract_initiative_frs(root),
        "feature-fr": feature["feature-fr"],
        "feature-nfr": feature["feature-nfr"],
        "ux-decision": extract_ux_decisions(root),
        "ux-acceptance": extract_ux_acceptance(root),
        "public-contract": extract_contract_surface(root),
        "public-client": extract_client_surface(root),
        "current-control": extract_current_controls(root),
        "conformance-assertion": extract_conformance_assertions(root),
    }


def owner_for(row: dict[str, Any]) -> str:
    if row["kind"] in {"public-contract", "public-client", "feature-fr", "feature-nfr", "ux-decision", "ux-acceptance"}:
        return "module"
    if row["kind"] == "initiative-fr":
        number = int(row["id"].split("-")[1])
        return "platform" if 10 <= number <= 16 else "module"
    if row["kind"] == "conformance-assertion":
        return "release-governance"
    return row.get("controlOwner", "module")


def disposition_proposal(row: dict[str, Any]) -> tuple[str, str, str]:
    kind = row["kind"]
    if kind in {"feature-fr", "feature-nfr", "ux-decision", "ux-acceptance"}:
        return (
            "preserved-non-activation",
            "Preserve this obligation without activating new delivery scope in Epic 6.",
            "Preservation-only scope; current behavior may remain delivered where independently evidenced.",
        )
    if kind == "conformance-assertion":
        return (
            "story-6.9-tier-assignment",
            "Keep the assertion release-gated while Story 6.9 assigns its exact approved tier.",
            "The assertion remains in the denominator and may not be removed, skipped, renamed away, or weakened.",
        )
    return (
        "evidence-mapping",
        "Approve the exact evidence mapping before treating this obligation as release-closed.",
        "The initiative obligation remains active; this draft does not infer non-activation.",
    )


def build_disposition(root: Path, unresolved_rows: list[dict[str, Any]]) -> dict[str, Any]:
    decisions: list[dict[str, Any]] = []
    for row in unresolved_rows:
        proposed, rationale, scope = disposition_proposal(row)
        decisions.append(
            {
                "dispositionId": f"DISP-{row['id']}",
                "obligationId": row["id"],
                "proposedClosure": proposed,
                "evidenceOwner": "quality-owner",
                "controlOwner": owner_for(row),
                "approver": "release-owner",
                "rationale": rationale,
                "scope": scope,
                "decisionDate": None,
                "status": "pending",
                "approvalEvidence": None,
                "compatibilityEvidence": [],
                "replacementEvidence": [],
            }
        )
    return {
        "$schema": "preservation-traceability-manifest-v2.schema.json#/$defs/dispositionDocument",
        "artifact": "preservation-non-activation-disposition",
        "version": 2,
        "status": "pending-operator",
        "authority": {
            "path": EPICS_PATH.as_posix(),
            "sha256": sha256_file(root / EPICS_PATH),
            "version": "epic-6-authority-2026-07-31-v6",
        },
        "decisions": decisions,
    }


def load_or_build_disposition(root: Path, unresolved_rows: list[dict[str, Any]]) -> dict[str, Any]:
    draft = build_disposition(root, unresolved_rows)
    path = root / DISPOSITION_PATH
    if not path.exists():
        return draft
    try:
        existing = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return draft
    expected_ids = [item["obligationId"] for item in draft["decisions"]]
    actual_ids = [item.get("obligationId") for item in existing.get("decisions", [])]
    if expected_ids != actual_ids:
        return draft
    existing["authority"] = draft["authority"]
    return existing


def add_closures(root: Path, inventory: dict[str, list[dict[str, Any]]]) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    rows: list[dict[str, Any]] = []
    unresolved: list[dict[str, Any]] = []
    release_baseline_test = "tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs"
    client_test = "tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs"
    for kind in inventory:
        for original in inventory[kind]:
            row = copy.deepcopy(original)
            row["controlOwner"] = owner_for(row)
            if "closure" not in row:
                if kind == "public-contract":
                    row["closure"] = {
                        "kind": "evidence",
                        "evidence": [
                            evidence_binding(root, release_baseline_test, "contract-surface-test-source", row["source"]["path"])
                        ],
                    }
                elif kind == "public-client":
                    row["closure"] = {
                        "kind": "evidence",
                        "evidence": [evidence_binding(root, client_test, "client-contract-test-source", row["source"]["path"])],
                    }
                else:
                    row["closure"] = {
                        "kind": "governed-disposition",
                        "dispositionId": f"DISP-{row['id']}",
                    }
                    unresolved.append(row)
            rows.append(row)
    return rows, unresolved


def source_bindings(root: Path, rows: list[dict[str, Any]]) -> list[dict[str, str]]:
    bindings_by_path = {
        row["source"]["path"]: "obligation-authority"
        for row in rows
    }
    bindings_by_path.update(
        {
            EPICS_PATH.as_posix(): "cumulative-epic-authority",
            TIER_DECISION_PATH.as_posix(): "tier-decision",
            SCHEMA_PATH.as_posix(): "machine-contract",
            "Hexalith.Conversations.slnx": "solution-inventory",
            "Directory.Packages.props": "build-authority",
            "global.json": "build-authority",
        }
    )
    return [
        {
            "path": path,
            "sha256": sha256_file(root / path),
            "role": bindings_by_path[path],
        }
        for path in sorted(bindings_by_path)
    ]


def identity_bindings(root: Path) -> list[dict[str, str]]:
    release_baseline_path = root / "docs/release-evidence/release-baseline-v1.json"
    final_conformance_path = root / "docs/release-evidence/final-conformance-contract-diff-v1.json"
    release_baseline = json.loads(release_baseline_path.read_text(encoding="utf-8"))
    final_conformance = json.loads(final_conformance_path.read_text(encoding="utf-8"))
    return [
        {
            "kind": "baseline",
            "path": release_baseline_path.relative_to(root).as_posix(),
            "sha256": sha256_file(release_baseline_path),
            "identity": release_baseline["baselineCommit"],
        },
        {
            "kind": "build",
            "path": final_conformance_path.relative_to(root).as_posix(),
            "sha256": sha256_file(final_conformance_path),
            "identity": final_conformance["commit"]["sha"],
        },
        {
            "kind": "test",
            "path": final_conformance_path.relative_to(root).as_posix(),
            "sha256": sha256_file(final_conformance_path),
            "identity": final_conformance["conformanceRun"]["executedAtUtc"],
        },
    ]


def apply_story_69_tiers(root: Path, assertion_rows: list[dict[str, Any]]) -> str:
    decision = json.loads((root / TIER_DECISION_PATH).read_text(encoding="utf-8"))
    triage = decision.get("triageResults")
    if triage is None:
        return "pending-story-6.9"
    entries = triage.get("assertions") if isinstance(triage, dict) else triage
    if not isinstance(entries, list):
        return "invalid-story-6.9"
    assignments: dict[str, str] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            return "invalid-story-6.9"
        assertion_id = entry.get("assertionId")
        tier = entry.get("tier")
        if not isinstance(assertion_id, str) or tier not in {"portable", "module-internal"} or assertion_id in assignments:
            return "invalid-story-6.9"
        assignments[assertion_id] = tier
    expected_ids = {row["id"] for row in assertion_rows}
    if set(assignments) != expected_ids:
        return "invalid-story-6.9"
    for row in assertion_rows:
        row["tier"] = assignments[row["id"]]
    return "triaged"


def build_tiering(root: Path, assertion_rows: list[dict[str, Any]], triage_status: str) -> dict[str, Any]:
    path = root / TIER_DECISION_PATH
    decision = json.loads(path.read_text(encoding="utf-8"))
    triage = decision.get("triageResults")
    return {
        "decisionPath": TIER_DECISION_PATH.as_posix(),
        "decisionSha256": sha256_file(path),
        "decisionStatus": decision.get("status"),
        "triageStatus": triage_status,
        "triageSha256": None if triage is None else sha256_text(compact_json(triage)),
        "assertionCount": len(assertion_rows),
        "portableStructuralEvidence": None if triage is None else decision.get("portableStructuralEvidence"),
        "bothTiersReleaseGated": all(
            tier.get("releaseGate") is True for tier in decision.get("tiers", {}).values()
        ),
    }


def build_mutation_governance(root: Path, rows: list[dict[str, Any]]) -> dict[str, Any]:
    draft = {
        "changeId": "preservation-manifest-v2-initial",
        "predecessorSha256": FROZEN_V1_HASHES["docs/release-evidence/conformance-manifest-v1-fixture.json"],
        "changedIds": [row["id"] for row in rows],
        "rationale": "Create the separate complete v2 preservation contract without changing v1.",
        "approver": "release-owner",
        "status": "pending",
        "approvalEvidence": None,
        "replacementEvidence": [
            {
                "path": "docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.json",
                "sha256": FROZEN_V1_HASHES[
                    "docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.json"
                ],
            }
        ],
    }
    path = root / MANIFEST_PATH
    if not path.exists():
        return draft
    try:
        existing = json.loads(path.read_text(encoding="utf-8")).get("mutationGovernance")
    except (AttributeError, json.JSONDecodeError, OSError):
        return draft
    if not isinstance(existing, dict) or set(existing) != set(draft):
        return draft
    immutable_fields = (
        "changeId",
        "predecessorSha256",
        "changedIds",
        "rationale",
        "approver",
        "replacementEvidence",
    )
    if any(existing.get(field) != draft[field] for field in immutable_fields):
        return draft
    if existing.get("status") not in {"pending", "approved", "superseded"}:
        return draft
    return existing


def build_manifest(
    root: Path,
    disposition: dict[str, Any],
    rows: list[dict[str, Any]],
    triage_status: str,
) -> dict[str, Any]:
    tiering = build_tiering(
        root,
        [row for row in rows if row["kind"] == "conformance-assertion"],
        triage_status,
    )
    summaries = []
    for kind in (
        "initiative-fr",
        "feature-fr",
        "feature-nfr",
        "ux-decision",
        "ux-acceptance",
        "public-contract",
        "public-client",
        "current-control",
        "conformance-assertion",
    ):
        count = sum(row["kind"] == kind for row in rows)
        summaries.append({"kind": kind, "expected": count, "actual": count, "unresolved": sum(row["kind"] == kind and row["closure"]["kind"] == "governed-disposition" for row in rows)})
    mutation_governance = build_mutation_governance(root, rows)
    if triage_status != "triaged":
        status = "pending-prerequisites"
    elif disposition.get("status") != "approved" or mutation_governance.get("status") != "approved":
        status = "pending-operator"
    else:
        status = "release-gated"
    return {
        "$schema": SCHEMA_PATH.name,
        "artifact": "preservation-traceability-manifest",
        "schemaVersion": 2,
        "manifestVersion": "2.0.0-draft",
        "status": status,
        "supersession": {
            "predecessorPath": "docs/release-evidence/conformance-manifest-v1-fixture.json",
            "predecessorSha256": FROZEN_V1_HASHES["docs/release-evidence/conformance-manifest-v1-fixture.json"],
            "boundary": "V2 adds complete preservation traceability and release controls; it does not extend ConformanceManifestV1 or mutate any v1 byte.",
            "v1MutationAllowed": False,
        },
        "authorityVersions": {
            "architecture": "conversations-architecture-2026-07-31-v6",
            "epic": "epic-6-authority-2026-07-31-v6",
            "uxMap": "current-hash-bound",
        },
        "sourceBindings": source_bindings(root, rows),
        "identityBindings": identity_bindings(root),
        "immutableV1Bindings": [
            {"path": path, "sha256": digest} for path, digest in FROZEN_V1_HASHES.items()
        ],
        "governanceInputs": {
            "dispositionPath": DISPOSITION_PATH.as_posix(),
            "dispositionSha256": sha256_text(canonical_json(disposition)),
            "status": disposition["status"],
        },
        "tiering": tiering,
        "mutationGovernance": mutation_governance,
        "summaries": summaries,
        "obligations": rows,
    }


def markdown_escape(value: Any) -> str:
    return str(value).replace("|", "\\|").replace("\n", " ")


def render_markdown(manifest: dict[str, Any]) -> str:
    lines = [
        "# Preservation Traceability Manifest V2",
        "",
        "> Generated from `preservation-traceability-manifest-v2.json`. The JSON is authoritative; do not edit this projection.",
        "",
        f"- Manifest version: `{manifest['manifestVersion']}`",
        f"- Status: `{manifest['status']}`",
        f"- Architecture authority: `{manifest['authorityVersions']['architecture']}`",
        f"- Epic authority: `{manifest['authorityVersions']['epic']}`",
        f"- Story 6.9 triage: `{manifest['tiering']['triageStatus']}`",
        "- Supersession boundary: v2 adds complete traceability while every v1 byte remains immutable.",
        "",
        "## Inventory Summary",
        "",
        "| Kind | Expected | Actual | Governed disposition |",
        "| --- | ---: | ---: | ---: |",
    ]
    for summary in manifest["summaries"]:
        lines.append(
            f"| {markdown_escape(summary['kind'])} | {summary['expected']} | {summary['actual']} | {summary['unresolved']} |"
        )
    lines.extend(["", "## Authority Bindings", "", "| Path | SHA-256 | Role |", "| --- | --- | --- |"]) 
    for binding in manifest["sourceBindings"]:
        lines.append(f"| `{binding['path']}` | `{binding['sha256']}` | {binding['role']} |")
    lines.extend(["", "## Obligations", ""])
    grouped: dict[str, list[dict[str, Any]]] = {}
    for row in manifest["obligations"]:
        grouped.setdefault(row["kind"], []).append(row)
    for kind, rows in grouped.items():
        lines.extend(
            [
                f"### {kind}",
                "",
                "| ID | Control owner | Closure | Tier | Source | Source text SHA-256 |",
                "| --- | --- | --- | --- | --- | --- |",
            ]
        )
        for row in rows:
            closure = row["closure"]["kind"]
            tier = row.get("tier", "n/a")
            source = row["source"]
            lines.append(
                f"| `{markdown_escape(row['id'])}` | {row['controlOwner']} | {closure} | {tier} | "
                f"`{source['path']}:{source['line']}` | `{source['textSha256']}` |"
            )
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def generate_outputs(root: Path) -> tuple[dict[str, Any], dict[str, Any], str]:
    inventory = extract_inventory(root)
    triage_status = apply_story_69_tiers(root, inventory["conformance-assertion"])
    rows, unresolved = add_closures(root, inventory)
    disposition = load_or_build_disposition(root, unresolved)
    manifest = build_manifest(root, disposition, rows, triage_status)
    return manifest, disposition, render_markdown(manifest)


def safe_repository_path(root: Path, value: Any) -> tuple[Path | None, str | None]:
    if not isinstance(value, str) or not value or Path(value).is_absolute():
        return None, "Path must be a nonempty repository-relative path."
    candidate = (root / value).resolve()
    try:
        candidate.relative_to(root.resolve())
    except ValueError:
        return None, "Path escapes the repository."
    return candidate, None


def add_diagnostic(diagnostics: list[Diagnostic], code: str, subject: str, message: str) -> None:
    diagnostic = Diagnostic(code, subject, message)
    if diagnostic not in diagnostics:
        diagnostics.append(diagnostic)


def expected_rows(root: Path) -> list[dict[str, Any]]:
    inventory = extract_inventory(root)
    rows, _ = add_closures(root, inventory)
    return rows


def validate_outputs(
    root: Path,
    manifest: dict[str, Any],
    disposition: dict[str, Any],
    *,
    strict: bool,
    markdown: str | None = None,
) -> list[Diagnostic]:
    diagnostics: list[Diagnostic] = []
    unknown_properties = set(manifest) - TOP_LEVEL_PROPERTIES
    if unknown_properties:
        add_diagnostic(
            diagnostics,
            "SCHEMA_CLOSED_VOCABULARY",
            ",".join(sorted(unknown_properties)),
            "The v2 schema rejects undeclared top-level properties.",
        )

    obligations = manifest.get("obligations")
    if not isinstance(obligations, list) or not obligations:
        add_diagnostic(diagnostics, "EMPTY_INVENTORY", "obligations", "The obligation inventory must be nonempty.")
        obligations = []

    expected = expected_rows(root)
    expected_by_id = {row["id"]: row for row in expected}
    actual_ids = [row.get("id") for row in obligations if isinstance(row, dict)]
    duplicates = sorted({value for value in actual_ids if value is not None and actual_ids.count(value) > 1})
    for obligation_id in duplicates:
        add_diagnostic(diagnostics, "DUPLICATE_OBLIGATION", obligation_id, "Each obligation must occur exactly once.")
    actual_set = set(actual_ids)
    for missing in sorted(set(expected_by_id) - actual_set):
        add_diagnostic(diagnostics, "DENOMINATOR_GAP", missing, "An authoritative obligation is missing.")
    for unknown in sorted(value for value in actual_set - set(expected_by_id) if value is not None):
        add_diagnostic(diagnostics, "UNKNOWN_OBLIGATION", unknown, "The manifest contains an unknown obligation.")

    if len(obligations) != len(expected):
        add_diagnostic(
            diagnostics,
            "DENOMINATOR_GAP",
            "obligations",
            f"Expected {len(expected)} rows and found {len(obligations)}.",
        )

    for row in obligations:
        if not isinstance(row, dict) or row.get("id") not in expected_by_id:
            continue
        expected_row = expected_by_id[row["id"]]
        source = row.get("source", {})
        expected_source = expected_row["source"]
        if source.get("textSha256") != expected_source["textSha256"]:
            add_diagnostic(
                diagnostics,
                "SOURCE_TEXT_HASH_MISMATCH",
                row["id"],
                "The obligation source text no longer matches authoritative extraction.",
            )
        for key in ("path", "fileSha256", "section", "line"):
            if source.get(key) != expected_source.get(key):
                add_diagnostic(
                    diagnostics,
                    "SOURCE_BINDING_MISMATCH",
                    row["id"],
                    f"Source field '{key}' differs from authoritative extraction.",
                )
        if row.get("kind") == "ux-acceptance" and source.get("ordinal") != expected_source.get("ordinal"):
            add_diagnostic(diagnostics, "SOURCE_BINDING_MISMATCH", row["id"], "UX acceptance ordinal drifted.")
        if row.get("kind") == "conformance-assertion" and "tier" not in row:
            add_diagnostic(diagnostics, "TIER_REQUIRED", row["id"], "Every conformance assertion requires one tier field.")
        if row.get("kind") == "current-control" and row.get("controlOwner") != expected_row.get("controlOwner"):
            add_diagnostic(
                diagnostics,
                "CONTROL_OWNERSHIP_REVERSAL",
                row["id"],
                "The declared control owner differs from the architecture authority.",
            )

        closure = row.get("closure", {})
        if closure.get("kind") == "evidence":
            evidence_items = closure.get("evidence")
            if not isinstance(evidence_items, list) or not evidence_items:
                add_diagnostic(diagnostics, "EVIDENCE_REQUIRED", row["id"], "Evidence closure must be nonempty.")
                continue
            for evidence in evidence_items:
                path, path_error = safe_repository_path(root, evidence.get("path"))
                if path_error is not None:
                    add_diagnostic(diagnostics, "PATH_ESCAPE", row["id"], path_error)
                    continue
                relative = path.relative_to(root.resolve()).as_posix()
                if relative.startswith("references/") or "/bin/" in f"/{relative}/" or "/obj/" in f"/{relative}/":
                    add_diagnostic(diagnostics, "EVIDENCE_PATH_FORBIDDEN", row["id"], "Evidence must be root-owned source evidence.")
                    continue
                if relative in {MANIFEST_PATH.as_posix(), MARKDOWN_PATH.as_posix(), DISPOSITION_PATH.as_posix(), SCHEMA_PATH.as_posix()}:
                    add_diagnostic(diagnostics, "GENERATED_OUTPUT_EVIDENCE", row["id"], "Generated manifest outputs cannot evidence themselves.")
                if relative == source.get("path"):
                    add_diagnostic(diagnostics, "SELF_ATTESTED_EVIDENCE", row["id"], "Authority prose or source cannot evidence itself.")
                if not path.exists():
                    add_diagnostic(diagnostics, "EVIDENCE_MISSING", row["id"], f"Evidence path '{relative}' does not exist.")
                elif evidence.get("sha256") != sha256_file(path):
                    add_diagnostic(diagnostics, "EVIDENCE_HASH_MISMATCH", row["id"], f"Evidence path '{relative}' hash drifted.")
                authority_path, authority_error = safe_repository_path(root, evidence.get("authorityPath"))
                if authority_error is not None or authority_path is None or not authority_path.exists():
                    add_diagnostic(diagnostics, "EVIDENCE_STALE", row["id"], "Evidence authority binding is missing.")
                elif evidence.get("authoritySha256") != sha256_file(authority_path):
                    add_diagnostic(diagnostics, "EVIDENCE_STALE", row["id"], "Evidence was not bound to current authority bytes.")
        elif closure.get("kind") != "governed-disposition":
            add_diagnostic(diagnostics, "CLOSURE_REQUIRED", row["id"], "Exactly one governed closure is required.")

    bindings = manifest.get("sourceBindings", [])
    if not isinstance(bindings, list) or not bindings:
        add_diagnostic(diagnostics, "EMPTY_INVENTORY", "sourceBindings", "Source bindings must be nonempty.")
    for binding in bindings if isinstance(bindings, list) else []:
        path, error = safe_repository_path(root, binding.get("path"))
        if error is not None:
            add_diagnostic(diagnostics, "PATH_ESCAPE", str(binding.get("path")), error)
        elif path is None or not path.exists():
            add_diagnostic(diagnostics, "SOURCE_MISSING", str(binding.get("path")), "Authority source is missing.")
        elif binding.get("sha256") != sha256_file(path):
            add_diagnostic(diagnostics, "SOURCE_HASH_MISMATCH", str(binding.get("path")), "Authority source hash drifted.")

    immutable = manifest.get("immutableV1Bindings", [])
    immutable_by_path = {item.get("path"): item.get("sha256") for item in immutable if isinstance(item, dict)}
    for path, digest in FROZEN_V1_HASHES.items():
        if immutable_by_path.get(path) != digest or sha256_file(root / path) != digest:
            add_diagnostic(diagnostics, "IMMUTABLE_V1_MISMATCH", path, "Frozen v1 bytes differ from the independent root of trust.")

    expected_disposition_ids = {
        row["closure"]["dispositionId"]
        for row in obligations
        if isinstance(row, dict) and row.get("closure", {}).get("kind") == "governed-disposition"
    }
    decisions = disposition.get("decisions", []) if isinstance(disposition, dict) else []
    actual_disposition_ids = [item.get("dispositionId") for item in decisions if isinstance(item, dict)]
    if set(actual_disposition_ids) != expected_disposition_ids or len(actual_disposition_ids) != len(set(actual_disposition_ids)):
        add_diagnostic(diagnostics, "DISPOSITION_SCOPE_MISMATCH", "decisions", "Disposition decisions must match unresolved IDs exactly once.")
    for decision in decisions:
        if not isinstance(decision, dict):
            continue
        if not decision.get("rationale") or not decision.get("scope"):
            add_diagnostic(diagnostics, "DISPOSITION_INCOMPLETE", str(decision.get("obligationId")), "Rationale and scope are required.")
        roles = {decision.get("evidenceOwner"), decision.get("controlOwner"), decision.get("approver")}
        if None in roles or len(roles) != 3:
            add_diagnostic(diagnostics, "GOVERNANCE_ROLE_COLLISION", str(decision.get("obligationId")), "Evidence owner, control owner, and approver must be distinct.")
        if decision.get("proposedClosure") in {"delivered-to-inactive", "compatible-change"}:
            if not decision.get("compatibilityEvidence"):
                add_diagnostic(
                    diagnostics,
                    "COMPATIBILITY_EVIDENCE_REQUIRED",
                    str(decision.get("obligationId")),
                    "Delivered-to-inactive and compatible changes require compatibility evidence.",
                )
        if strict and decision.get("status") != "approved":
            add_diagnostic(diagnostics, "APPROVAL_PENDING", str(decision.get("obligationId")), "Named operator approval is pending.")
        if decision.get("status") == "approved" and (not decision.get("decisionDate") or not decision.get("approvalEvidence")):
            add_diagnostic(diagnostics, "APPROVAL_INVALID", str(decision.get("obligationId")), "Approved decisions require dated approval evidence.")

    disposition_digest = sha256_text(canonical_json(disposition))
    if manifest.get("governanceInputs", {}).get("dispositionSha256") != disposition_digest:
        add_diagnostic(diagnostics, "DISPOSITION_BINDING_MISMATCH", DISPOSITION_PATH.as_posix(), "The manifest does not bind the exact disposition bytes.")

    tiering = manifest.get("tiering", {})
    tier_path, tier_path_error = safe_repository_path(root, tiering.get("decisionPath"))
    if tier_path_error is not None or tier_path is None or not tier_path.exists():
        add_diagnostic(diagnostics, "TIER_DECISION_MISSING", str(tiering.get("decisionPath")), "Story 6.9 decision input is missing.")
    elif tiering.get("decisionSha256") != sha256_file(tier_path):
        add_diagnostic(diagnostics, "TIER_DECISION_HASH_MISMATCH", str(tiering.get("decisionPath")), "Story 6.9 decision bytes drifted.")
    if tiering.get("triageStatus") == "invalid-story-6.9":
        add_diagnostic(
            diagnostics,
            "TIER_TRIAGE_INVALID",
            "Story 6.9",
            "Triage must enumerate every assertion exactly once with portable or module-internal tier.",
        )
    if strict and tiering.get("triageStatus") != "triaged":
        add_diagnostic(diagnostics, "TIERING_INCOMPLETE", "Story 6.9", "The final assertion triage has not been produced.")
    if tiering.get("triageStatus") == "triaged":
        assertion_tiers = {
            row.get("tier")
            for row in obligations
            if isinstance(row, dict) and row.get("kind") == "conformance-assertion"
        }
        if not assertion_tiers or not assertion_tiers.issubset({"portable", "module-internal"}):
            add_diagnostic(diagnostics, "TIER_REQUIRED", "Story 6.9", "Final triage left an assertion without one approved tier.")
        structural_evidence = tiering.get("portableStructuralEvidence")
        if not isinstance(structural_evidence, dict) or structural_evidence.get("result") != "pass":
            add_diagnostic(
                diagnostics,
                "PORTABLE_TIER_EVIDENCE_REQUIRED",
                "Story 6.9",
                "Portable-tier freedom must be an executed passing structural result.",
            )
        else:
            structural_path, structural_error = safe_repository_path(root, structural_evidence.get("path"))
            if structural_error is not None or structural_path is None or not structural_path.exists():
                add_diagnostic(diagnostics, "PORTABLE_TIER_EVIDENCE_REQUIRED", "Story 6.9", "Structural result path is missing.")
            elif structural_evidence.get("sha256") != sha256_file(structural_path):
                add_diagnostic(diagnostics, "EVIDENCE_HASH_MISMATCH", "Story 6.9", "Structural result hash drifted.")
    if tiering.get("bothTiersReleaseGated") is not True:
        add_diagnostic(diagnostics, "TIER_RELEASE_GATE_REQUIRED", "tiers", "Both conformance tiers must remain release-gated.")

    mutation = manifest.get("mutationGovernance", {})
    if mutation.get("predecessorSha256") != FROZEN_V1_HASHES["docs/release-evidence/conformance-manifest-v1-fixture.json"]:
        add_diagnostic(diagnostics, "MUTATION_PREDECESSOR_MISMATCH", "mutationGovernance", "The v1 predecessor binding is invalid.")
    if mutation.get("changedIds") != [row.get("id") for row in obligations]:
        add_diagnostic(diagnostics, "MUTATION_CHANGED_IDS_MISMATCH", "mutationGovernance", "Changed IDs must be exact and ordered.")
    if strict and mutation.get("status") != "approved":
        add_diagnostic(diagnostics, "APPROVAL_PENDING", "mutationGovernance", "Manifest mutation approval is pending.")

    summaries = manifest.get("summaries", [])
    summary_by_kind = {item.get("kind"): item for item in summaries if isinstance(item, dict)}
    for kind in {row["kind"] for row in expected}:
        expected_count = sum(row["kind"] == kind for row in expected)
        actual_count = sum(row.get("kind") == kind for row in obligations if isinstance(row, dict))
        summary = summary_by_kind.get(kind)
        if summary is None or summary.get("expected") != expected_count or summary.get("actual") != actual_count:
            add_diagnostic(diagnostics, "SUMMARY_MISMATCH", kind, "Aggregate summary does not match independently derived rows.")

    if markdown is not None and markdown != render_markdown(manifest):
        add_diagnostic(diagnostics, "PROJECTION_DRIFT", MARKDOWN_PATH.as_posix(), "Markdown is not the byte-exact JSON projection.")

    return sorted(diagnostics, key=lambda item: (item.code, item.subject, item.message))


def write_outputs(root: Path, manifest: dict[str, Any], disposition: dict[str, Any], markdown: str) -> None:
    (root / MANIFEST_PATH).write_text(canonical_json(manifest), encoding="utf-8", newline="\n")
    (root / DISPOSITION_PATH).write_text(canonical_json(disposition), encoding="utf-8", newline="\n")
    (root / MARKDOWN_PATH).write_text(markdown, encoding="utf-8", newline="\n")


def load_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"Expected JSON object: {path}")
    return value


def repository_root(explicit: str | None) -> Path:
    if explicit is not None:
        return Path(explicit).resolve()
    completed = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        check=True,
        capture_output=True,
        text=True,
        timeout=30,
    )
    return Path(completed.stdout.strip()).resolve()


def build_summary(mode: str, diagnostics: list[Diagnostic]) -> dict[str, Any]:
    return {
        "artifact": "preservation-traceability-manifest-v2-validation",
        "mode": mode,
        "result": "pass" if not diagnostics else "fail",
        "diagnostics": [
            {"code": item.code, "subject": item.subject, "message": item.message}
            for item in diagnostics
        ],
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository")
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--allow-pending-operator", action="store_true")
    args = parser.parse_args(argv)

    try:
        root = repository_root(args.repository)
        generated_manifest, generated_disposition, generated_markdown = generate_outputs(root)
        if args.check:
            required = [root / MANIFEST_PATH, root / DISPOSITION_PATH, root / MARKDOWN_PATH, root / SCHEMA_PATH]
            missing = [path.relative_to(root).as_posix() for path in required if not path.exists()]
            if missing:
                diagnostics = [Diagnostic("GENERATED_OUTPUT_MISSING", path, "Run the generator before check mode.") for path in missing]
            else:
                manifest = load_json(root / MANIFEST_PATH)
                disposition = load_json(root / DISPOSITION_PATH)
                markdown = (root / MARKDOWN_PATH).read_text(encoding="utf-8")
                diagnostics = validate_outputs(
                    root,
                    manifest,
                    disposition,
                    strict=not args.allow_pending_operator,
                    markdown=markdown,
                )
                if canonical_json(manifest) != canonical_json(generated_manifest):
                    add_diagnostic(diagnostics, "GENERATED_JSON_DRIFT", MANIFEST_PATH.as_posix(), "Regenerate the authoritative JSON.")
                if canonical_json(disposition) != canonical_json(generated_disposition):
                    add_diagnostic(diagnostics, "GENERATED_DISPOSITION_DRIFT", DISPOSITION_PATH.as_posix(), "Regenerate the exact-ID decision input.")
                if markdown != generated_markdown:
                    add_diagnostic(diagnostics, "PROJECTION_DRIFT", MARKDOWN_PATH.as_posix(), "Regenerate the Markdown projection.")
        else:
            write_outputs(root, generated_manifest, generated_disposition, generated_markdown)
            diagnostics = validate_outputs(
                root,
                generated_manifest,
                generated_disposition,
                strict=not args.allow_pending_operator,
                markdown=generated_markdown,
            )
        mode = "structural" if args.allow_pending_operator else "strict"
        print(canonical_json(build_summary(mode, diagnostics)), end="")
        return 0 if not diagnostics else 1
    except (OSError, ValueError, KeyError, json.JSONDecodeError, subprocess.SubprocessError) as error:
        summary = build_summary("structural" if args.allow_pending_operator else "strict", [Diagnostic("INVALID_INPUT", "generator", str(error))])
        print(canonical_json(summary), end="")
        return 2


if __name__ == "__main__":
    sys.exit(main())
