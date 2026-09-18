#!/usr/bin/env python3
"""Generate or check the pending Owner-review preservation manifest v3."""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
MANIFEST_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.json"
MARKDOWN_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.md"
DIGEST_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.sha256"
SCHEMA_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.schema.json"
V2_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v2.json"
V2_MARKDOWN_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v2.md"
BASELINE_PATH = ROOT / "docs/release-evidence/release-baseline-v1.json"
SIGNED_DECISION_PATH = ROOT / "docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.json"
SIGNED_REPORT_PATH = ROOT / "docs/release-evidence/success-metric-report-and-attestation-v1.json"
FINAL_CONFORMANCE_PATH = ROOT / "docs/release-evidence/final-conformance-contract-diff-v1.json"
RUNTIME_EVIDENCE_PATH = ROOT / "_bmad-output/implementation-artifacts/apphost-runtime-boundary-working-tree-evidence-v2.json"
EVIDENCE_ROOT = ROOT / "_bmad-output/implementation-artifacts/preservation-traceability-v3"
CLEAN_XML_PATH = EVIDENCE_ROOT / "conformance-release-clean-head.xml"
OWNER_XML_PATH = EVIDENCE_ROOT / "conformance-owner-review.xml"
CLEAN_BUILD_LOG_PATH = EVIDENCE_ROOT / "solution-build-release.log"
OWNER_BUILD_LOG_PATH = EVIDENCE_ROOT / "conformance-build-owner-review.log"
TOOLCHAIN_PATH = EVIDENCE_ROOT / "toolchain-identity.txt"

ROOT_COMMIT = "d956c9b1de73bcf15969d5e1a6435d6d98a2dd49"
ROOT_TREE = "85efb51da9e790550f0991c5c59312ae86dcc838"
ROOT_PARENT = "c0abd5cb73d420bb2f4b5d04827461ad82c4528c"
BASELINE_COMMIT = "ceb7fbe958a6b89a3b1ad01a3c98252cc766b4fe"
APPROVED_SOURCE_COMMIT = "c6670fac7347ecd7240f7bab7e5e23147c8dfc65"
VERSION = "3.0.0-rc.1"
V1_IDS_SHA256 = "31a3a21b58aa3d704458a242adf87a41b95cc69b5f9b2b38994354735d3fb4bc"
APPROVED_IDS_SHA256 = "ec0d374dc6d947c662165de79eae9911acaa60bfaf4735e314923bbd9f0e6c61"
APPROVED_ADDITIONS_SHA256 = "93632988fa4c409847cd0976e11d5deea8c5a7cef01eaea9e8889e5bcbb01180"

METHOD_PATTERN = re.compile(
    r"public\s+(?:(?:static|async)\s+)*(?:void|Task|ValueTask|IEnumerable<[^>]+>)\s+([A-Za-z0-9_]+)\s*\("
)


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def canonical_json(value: Any) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False, sort_keys=False) + "\n"


def list_digest(values: list[str]) -> str:
    return sha256_bytes(("\n".join(values) + "\n").encode("utf-8"))


def git_text(*args: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(ROOT), *args],
        check=False,
        capture_output=True,
        text=True,
        timeout=60,
    )
    if completed.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed: {completed.stderr.strip()}")
    return completed.stdout.strip()


def git_bytes(*args: str) -> bytes:
    completed = subprocess.run(
        ["git", "-C", str(ROOT), *args],
        check=False,
        capture_output=True,
        timeout=60,
    )
    if completed.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed: {completed.stderr.decode(errors='replace').strip()}")
    return completed.stdout


def git_is_ancestor(ancestor: str, descendant: str) -> bool:
    completed = subprocess.run(
        ["git", "-C", str(ROOT), "merge-base", "--is-ancestor", ancestor, descendant],
        check=False,
        capture_output=True,
        timeout=60,
    )
    if completed.returncode == 0:
        return True
    if completed.returncode == 1:
        return False
    raise RuntimeError(
        "git merge-base --is-ancestor failed: "
        + completed.stderr.decode(errors="replace").strip()
    )


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def artifact_binding(path: Path, role: str) -> dict[str, Any]:
    return {
        "path": relative(path),
        "sha256": sha256_file(path),
        "bytes": path.stat().st_size,
        "role": role,
    }


def load_inventory_module() -> Any:
    path = ROOT / "_bmad/scripts/generate_preservation_traceability_manifest.py"
    spec = importlib.util.spec_from_file_location("preservation_manifest_v2_generator", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load inventory extractor: {path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def historical_test_sources(commit: str) -> list[str]:
    return sorted(
        path
        for path in git_text(
            "ls-tree",
            "-r",
            "--name-only",
            commit,
            "--",
            "tests/Hexalith.Conversations.Conformance.Tests",
        ).splitlines()
        if path.endswith("Test.cs")
    )


def historical_methods(commit: str, allowed_classes: set[str] | None = None) -> tuple[list[str], list[str]]:
    facts: list[str] = []
    theories: list[str] = []
    namespace = "Hexalith.Conversations.Conformance.Tests"
    for path in historical_test_sources(commit):
        class_name = Path(path).stem
        if allowed_classes is not None and class_name not in allowed_classes:
            continue
        lines = git_text("show", f"{commit}:{path}").splitlines()
        pending: str | None = None
        attribute_line = 0
        for index, line in enumerate(lines, start=1):
            stripped = line.strip()
            if stripped.startswith("[Fact"):
                pending = "Fact"
                attribute_line = index
                continue
            if stripped.startswith("[Theory"):
                pending = "Theory"
                attribute_line = index
                continue
            if pending is None:
                continue
            match = METHOD_PATTERN.search(stripped)
            if match:
                fq_method = f"{namespace}.{class_name}.{match.group(1)}"
                (facts if pending == "Fact" else theories).append(fq_method)
                pending = None
            elif stripped and not stripped.startswith(("[", "//", "///")) and index - attribute_line > 20:
                pending = None
    return sorted(facts), sorted(theories)


def derive_v1_ids() -> tuple[list[str], list[str]]:
    baseline = json.loads(BASELINE_PATH.read_text(encoding="utf-8"))
    classes = [row["class"] for row in baseline["conformanceOracle"]["suiteClasses"]]
    facts: list[str] = []
    theories: list[str] = []
    for class_name in classes:
        path = f"tests/Hexalith.Conversations.Conformance.Tests/{class_name}.cs"
        pending: str | None = None
        for line in git_text("show", f"{BASELINE_COMMIT}:{path}").splitlines():
            stripped = line.strip()
            if stripped.startswith("[Fact"):
                pending = "Fact"
                continue
            if stripped.startswith("[Theory"):
                pending = "Theory"
                continue
            if pending is None:
                continue
            match = METHOD_PATTERN.search(stripped)
            if match:
                fq_method = f"Hexalith.Conversations.Conformance.Tests.{class_name}.{match.group(1)}"
                (facts if pending == "Fact" else theories).append(fq_method)
                pending = None
    if theories:
        raise ValueError(f"Unexpected v1 theory methods: {theories}")
    if len(classes) != 14 or len(facts) != 214:
        raise ValueError(f"Expected 14 v1 suites and 214 tests; got {len(classes)} and {len(facts)}")
    if list_digest(facts) != V1_IDS_SHA256:
        raise ValueError("The reconstructed v1 exact-ID digest changed")
    return facts, classes


def derive_approved_ids() -> tuple[list[str], list[str], dict[str, Any]]:
    facts, theories = historical_methods(APPROVED_SOURCE_COMMIT)
    expected_theories = {
        "Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail",
        "Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState",
    }
    if len(facts) != 368 or set(theories) != expected_theories:
        raise ValueError(f"Approved source method inventory changed: facts={len(facts)}, theories={theories}")

    projection_method = next(value for value in theories if "ConversationProjectionReadSurface" in value)
    projection_path = "tests/Hexalith.Conversations.Conformance.Tests/ConversationProjectionReadSurfaceConformanceTest.cs"
    projection_source = git_text("show", f"{APPROVED_SOURCE_COMMIT}:{projection_path}")
    projection_values = re.findall(r'^\s*\[InlineData\("([^"]+)"\)\]', projection_source, flags=re.MULTILINE)
    projection_ids = [f'{projection_method}(degradedState: \\"{value}\\")' for value in projection_values]

    tenant_method = next(value for value in theories if "LiveTenantFailClosed" in value)
    tenant_path = "tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs"
    tenant_source = git_text("show", f"{APPROVED_SOURCE_COMMIT}:{tenant_path}")
    tenant_rows = re.findall(
        r'\{\s*"([^"]+)",\s*ConversationTenantAccessDenialReason\.([A-Za-z0-9_]+)\s*\}',
        tenant_source,
    )
    tenant_ids = [
        f'{tenant_method}(trigger: \\"{trigger}\\", expectedReason: {reason})'
        for trigger, reason in tenant_rows
    ]
    approved = sorted([*facts, *projection_ids, *tenant_ids])
    if len(projection_ids) != 4 or len(tenant_ids) != 12 or len(approved) != 384:
        raise ValueError(
            f"Expected approved expansion 368+4+12=384; got {len(facts)}+{len(projection_ids)}+{len(tenant_ids)}"
        )
    if list_digest(approved) != APPROVED_IDS_SHA256:
        raise ValueError("The reconstructed approved exact-ID digest changed")
    return approved, theories, {
        "methodFactCount": len(facts),
        "theoryMethodCount": len(theories),
        "theoryCaseCount": len(projection_ids) + len(tenant_ids),
        "sourceIdentity": "fully-qualified method plus canonical ordered arguments",
        "displayIdentityDerivation": "reconstructed-from-approved-source",
        "historicalRunnerOutputAvailable": False,
        "note": "No tracked historical XML enumerates all 384 display names; method and ordered-argument identities are reconstructed from the decision-bound source. Current xUnit display spelling is stored separately by the candidate run.",
    }


def read_xunit(path: Path) -> tuple[list[str], dict[str, str], dict[str, Any]]:
    root = ET.parse(path).getroot()
    assembly = root.find("assembly")
    if assembly is None:
        raise ValueError(f"No assembly element in {path}")
    result_by_id = {test.attrib["name"]: test.attrib["result"] for test in assembly.iter("test")}
    ids = sorted(result_by_id)
    summary: dict[str, Any] = {
        "total": int(assembly.attrib["total"]),
        "passed": int(assembly.attrib["passed"]),
        "failed": int(assembly.attrib["failed"]),
        "skipped": int(assembly.attrib["skipped"]),
        "notRun": int(assembly.attrib["not-run"]),
        "startedAtUtc": assembly.attrib["start-rtf"],
        "finishedAtUtc": assembly.attrib["finish-rtf"],
        "testFramework": assembly.attrib["test-framework"],
        "runtime": assembly.attrib["environment"],
        "targetFramework": assembly.attrib["target-framework"],
    }
    if len(ids) != summary["total"] or len(ids) != len(set(ids)):
        raise ValueError(f"Runtime IDs are missing or duplicated in {path}")
    return ids, result_by_id, summary


def committed_change_set() -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for line in git_text("diff-tree", "--no-commit-id", "--name-status", "-r", "-M", ROOT_PARENT, ROOT_COMMIT).splitlines():
        parts = line.split("\t")
        status = parts[0]
        path = parts[-1]
        tree_line = git_text("ls-tree", ROOT_COMMIT, "--", path)
        metadata, resolved_path = tree_line.split("\t", 1)
        mode, object_type, oid = metadata.split()
        row: dict[str, Any] = {
            "status": status,
            "path": resolved_path,
            "mode": mode,
            "gitObjectType": object_type,
            "gitObjectId": oid,
        }
        if mode != "160000":
            data = git_bytes("show", f"{ROOT_COMMIT}:{resolved_path}")
            row["sha256"] = sha256_bytes(data)
            row["bytes"] = len(data)
        rows.append(row)
    if len(rows) != 50:
        raise ValueError(f"Expected 50 committed remediation paths; got {len(rows)}")
    return rows


def submodule_bindings() -> list[dict[str, str]]:
    config_lines = git_text("config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$").splitlines()
    rows: list[dict[str, str]] = []
    for line in config_lines:
        _, path = line.split(maxsplit=1)
        metadata = git_text("ls-tree", ROOT_COMMIT, "--", path).split("\t", 1)[0].split()
        mode, object_type, oid = metadata
        if mode != "160000" or object_type != "commit":
            raise ValueError(f"Expected root gitlink for {path}")
        rows.append({"path": path, "gitlink": oid})
    if len(rows) != 10:
        raise ValueError(f"Expected 10 root submodule gitlinks; got {len(rows)}")
    return sorted(rows, key=lambda row: row["path"])


def current_overlay_bindings() -> list[dict[str, Any]]:
    paths = [
        ROOT / "tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs",
        ROOT / "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
        ROOT / "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md",
    ]
    return [artifact_binding(path, "uncommitted-owner-review-overlay") for path in paths]


def category_mappings(current_ids: list[str], obligation_ids: set[str]) -> list[dict[str, Any]]:
    current = set(current_ids)

    def class_ids(*classes: str) -> set[str]:
        prefixes = [f"Hexalith.Conversations.Conformance.Tests.{name}." for name in classes]
        return {test_id for test_id in current if any(test_id.startswith(prefix) for prefix in prefixes)}

    def exact(class_name: str, method: str) -> str:
        test_id = f"Hexalith.Conversations.Conformance.Tests.{class_name}.{method}"
        if test_id not in current:
            raise ValueError(f"Mapped test is absent from the current candidate: {test_id}")
        return test_id

    mappings = [
        {
            "category": "tenant-isolation",
            "requirementIds": ["FR-20", "Feature-FR87", "Feature-NFR16", "Feature-NFR17"],
            "testIds": sorted(
                class_ids("TenantIsolationConformanceSuiteTest", "LiveTenantFailClosedOracleCharacterizationTest")
                | {exact("AdopterConformanceSuiteTest", "TenantBindingCheckShouldExerciseCrossTenantHiddenSideChannelShape")}
            ),
            "approvalState": "pending-owner-approval",
        },
        {
            "category": "idempotency",
            "requirementIds": ["FR-20", "Feature-FR88", "Feature-NFR22", "Feature-NFR23"],
            "testIds": sorted(
                class_ids(
                    "IdempotencyConformanceSuiteTest",
                    "LiveIdempotencyOracleCharacterizationTest",
                    "LiveIdempotencyConflictOracleCharacterizationTest",
                )
                | {exact("AdopterConformanceSuiteTest", "IdempotencyCheckShouldSurfaceNonRetryableConflictAsBlocked")}
            ),
            "approvalState": "pending-owner-approval",
        },
        {
            "category": "contract-validation",
            "requirementIds": ["FR-20", "Feature-FR92", "Feature-NFR49", "Feature-NFR53"],
            "testIds": sorted(
                class_ids("ContractValidationConformanceSuiteTest", "PublicContractShapeSnapshotGenerationTest")
                | {
                    exact("AdopterConformanceSuiteTest", "CompatibilityDiscoveryCheckShouldSurfaceUnsupportedAsBlockedTypedError"),
                    exact("AdopterConformanceSuiteTest", "ErrorEnvelopeCheckShouldReuseSharedTypedErrorCatalog"),
                }
            ),
            "approvalState": "pending-owner-approval",
        },
        {
            "category": "redaction-replay",
            "requirementIds": ["FR-20", "Feature-FR89", "Feature-NFR21"],
            "testIds": sorted(
                class_ids("RedactionConformanceSuiteTest", "TelemetryRedactionConformanceSuiteTest")
                | {
                    exact(
                        "LiveProjectionFreshnessOracleCharacterizationTest",
                        "LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction",
                    ),
                    exact(
                        "ConversationProjectionReadSurfaceConformanceTest",
                        "RedactedMessageShouldStaySuppressedThroughPublicReadSurface",
                    ),
                }
            ),
            "approvalState": "pending-owner-approval",
        },
        {
            "category": "provider-portability",
            "requirementIds": ["FR-20", "Feature-FR90", "Feature-NFR50", "Feature-NFR51", "Feature-NFR52"],
            "testIds": sorted(class_ids("ProviderPortabilityConformanceSuiteTest")),
            "approvalState": "pending-owner-approval",
        },
        {
            "category": "projection-freshness",
            "requirementIds": [
                "FR-20",
                "Feature-FR34",
                "Feature-FR36",
                "Feature-FR37",
                "Feature-NFR44",
                "Feature-NFR45",
                "Feature-NFR46",
                "Feature-NFR47",
                "Feature-NFR48",
            ],
            "testIds": sorted(
                class_ids("LiveProjectionFreshnessOracleCharacterizationTest", "ConversationProjectionReadSurfaceConformanceTest")
                | {exact("AdopterConformanceSuiteTest", "ProjectionFreshnessCheckShouldSurfaceStaleAsDegradedNonTrustBearing")}
            ),
            "approvalState": "pending-owner-approval",
        },
        {
            "category": "governance-audit-pairing",
            "requirementIds": ["FR-20", "Feature-FR47", "Feature-FR48", "Feature-FR49", "Feature-NFR20"],
            "testIds": sorted(
                class_ids(
                    "GovernanceAuditPairingSafetyNetConformanceTest",
                    "GovernanceAuditSinkFailClosedConformanceTest",
                    "BuyerAcceptanceConformanceSuiteTest",
                    "ReleaseScopeConformanceSuiteTest",
                    "SecondAdopterConformanceSuiteTest",
                )
                | {
                    exact(
                        "ReleaseConformanceArtifactGenerationTest",
                        "AuditIntegrityGateShouldBePassWhenGovernancePreconditionIsReady",
                    ),
                    exact(
                        "LiveProjectionFreshnessOracleCharacterizationTest",
                        "LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence",
                    ),
                    exact(
                        "ConversationProjectionReadSurfaceConformanceTest",
                        "GovernanceEvidenceShouldBeAnchoredThroughPublicReadSurface",
                    ),
                }
            ),
            "approvalState": "pending-owner-approval",
        },
    ]
    expected_categories = {
        "tenant-isolation",
        "idempotency",
        "contract-validation",
        "redaction-replay",
        "provider-portability",
        "projection-freshness",
        "governance-audit-pairing",
    }
    if {row["category"] for row in mappings} != expected_categories:
        raise ValueError("The seven-category mapping is incomplete")
    for row in mappings:
        missing_requirements = set(row["requirementIds"]) - obligation_ids
        missing_tests = set(row["testIds"]) - current
        if missing_requirements or missing_tests or not row["testIds"]:
            raise ValueError(
                f"Orphaned mapping in {row['category']}: requirements={missing_requirements}, tests={missing_tests}"
            )
    return mappings


def build_obligation_closures(
    inventory_module: Any,
    inventory: dict[str, list[dict[str, Any]]],
    mappings: list[dict[str, Any]],
    current_ids: list[str],
) -> tuple[list[dict[str, Any]], list[dict[str, Any]], list[str]]:
    """Attach a computed traceability closure and explicit activation state to every obligation."""
    current_set = set(current_ids)
    mapped_tests: dict[str, set[str]] = {}
    mapped_categories: dict[str, set[str]] = {}
    for mapping in mappings:
        for requirement_id in mapping["requirementIds"]:
            mapped_tests.setdefault(requirement_id, set()).update(mapping["testIds"])
            mapped_categories.setdefault(requirement_id, set()).add(mapping["category"])

    release_baseline_test = "tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs"
    client_test = "tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs"
    obligations: list[dict[str, Any]] = []
    dispositions: list[dict[str, Any]] = []

    for kind in inventory_module.OBLIGATION_KINDS:
        for source_row in inventory[kind]:
            row = dict(source_row)
            row["preservationState"] = "required"
            if kind == "initiative-fr":
                row["releaseActivationState"] = (
                    "deferred-not-active" if row["id"] == "FR-16" else "active-initiative-scope"
                )
            elif kind in {"feature-fr", "feature-nfr", "ux-decision", "ux-acceptance"}:
                row["releaseActivationState"] = "pending-not-inferred"
            else:
                row["releaseActivationState"] = "not-applicable"

            if row["id"] in mapped_tests:
                row["closure"] = {
                    "kind": "exact-test-traceability",
                    "state": "mapped-pending-owner-approval",
                    "categories": sorted(mapped_categories[row["id"]]),
                    "testIds": sorted(mapped_tests[row["id"]]),
                }
            elif kind == "conformance-assertion":
                fq_method = f"Hexalith.Conversations.Conformance.Tests.{row['title']}"
                runtime_ids = sorted(
                    test_id
                    for test_id in current_set
                    if test_id == fq_method or test_id.startswith(fq_method + "(")
                )
                row["closure"] = {
                    "kind": "runtime-test-identity",
                    "state": "mapped-current-candidate",
                    "testIds": runtime_ids,
                }
            elif kind == "public-contract":
                row["closure"] = {
                    "kind": "evidence",
                    "state": "source-bound",
                    "evidence": [
                        inventory_module.evidence_binding(
                            ROOT,
                            release_baseline_test,
                            "contract-surface-test-source",
                            row["source"]["path"],
                        )
                    ],
                }
            elif kind == "public-client":
                row["closure"] = {
                    "kind": "evidence",
                    "state": "source-bound",
                    "evidence": [
                        inventory_module.evidence_binding(
                            ROOT,
                            client_test,
                            "client-contract-test-source",
                            row["source"]["path"],
                        )
                    ],
                }
            elif kind == "current-control":
                row["closure"] = dict(row["closure"])
                row["closure"]["state"] = "source-bound"
            else:
                disposition_id = f"DISP-{row['id']}"
                row["closure"] = {
                    "kind": "governed-disposition",
                    "state": "pending",
                    "dispositionId": disposition_id,
                }
                dispositions.append(
                    {
                        "dispositionId": disposition_id,
                        "obligationId": row["id"],
                        "status": "pending",
                        "approvalEvidence": None,
                        "releaseActivationEffect": "none",
                        "proposal": "Preserve the obligation and require a future evidence mapping or explicit release decision; do not infer activation, acceptance, waiver, or closure.",
                    }
                )
            obligations.append(row)

    disposition_ids = {row["dispositionId"] for row in dispositions}
    orphan_ids: list[str] = []
    for row in obligations:
        closure = row["closure"]
        kind = closure["kind"]
        valid = False
        if kind in {"exact-test-traceability", "runtime-test-identity"}:
            test_ids = closure.get("testIds", [])
            valid = bool(test_ids) and set(test_ids) <= current_set
        elif kind == "evidence":
            evidence = closure.get("evidence", [])
            valid = bool(evidence)
            for item in evidence:
                evidence_path = ROOT / item["path"]
                authority_path = ROOT / item["authorityPath"]
                valid = valid and evidence_path.exists() and authority_path.exists()
                valid = valid and sha256_file(evidence_path) == item["sha256"]
                valid = valid and sha256_file(authority_path) == item["authoritySha256"]
        elif kind == "governed-disposition":
            valid = closure.get("dispositionId") in disposition_ids and closure.get("state") == "pending"
        row["orphan"] = not valid
        if row["orphan"]:
            orphan_ids.append(row["id"])
    return obligations, dispositions, sorted(orphan_ids)


def build_manifest() -> dict[str, Any]:
    head_commit = git_text("rev-parse", "HEAD")
    if head_commit != ROOT_COMMIT and not git_is_ancestor(ROOT_COMMIT, head_commit):
        raise ValueError(f"Expected bound source commit {ROOT_COMMIT} to be HEAD or its ancestor; found {head_commit}")
    if git_text("rev-parse", f"{ROOT_COMMIT}^{{tree}}") != ROOT_TREE:
        raise ValueError("The bound source tree changed")
    if git_text("rev-parse", f"{ROOT_COMMIT}^") != ROOT_PARENT:
        raise ValueError("The bound source parent changed")

    v1_ids, v1_suites = derive_v1_ids()
    approved_ids, approved_theories, derivation = derive_approved_ids()
    clean_ids, _, clean_summary = read_xunit(CLEAN_XML_PATH)
    current_ids, current_results, current_summary = read_xunit(OWNER_XML_PATH)
    if len(clean_ids) != 472 or clean_summary != {**clean_summary, "total": 472, "passed": 456, "failed": 16}:
        if clean_summary["total"] != 472 or clean_summary["passed"] != 456 or clean_summary["failed"] != 16:
            raise ValueError(f"Unexpected clean-HEAD conformance result: {clean_summary}")
    if current_summary["total"] != 473 or current_summary["passed"] != 456 or current_summary["failed"] != 17:
        raise ValueError(f"Unexpected Owner-review conformance result: {current_summary}")

    v1_set = set(v1_ids)
    approved_set = set(approved_ids)
    current_set = set(current_ids)
    if not v1_set <= approved_set:
        raise ValueError(f"Approved floor omits {len(v1_set - approved_set)} v1 tests")
    if not approved_set <= current_set:
        raise ValueError(f"Current candidate omits approved tests: {sorted(approved_set - current_set)}")
    approved_additions = sorted(approved_set - v1_set)
    pending_additions = sorted(current_set - approved_set)
    if len(approved_additions) != 170 or list_digest(approved_additions) != APPROVED_ADDITIONS_SHA256:
        raise ValueError("The 170 approved-addition inventory changed")
    if len(pending_additions) != 89:
        raise ValueError(f"Expected 89 pending additions; found {len(pending_additions)}")

    inventory_module = load_inventory_module()
    inventory = inventory_module.extract_inventory(ROOT)
    inventory_rows: list[dict[str, Any]] = []
    for kind in inventory_module.OBLIGATION_KINDS:
        for source_row in inventory[kind]:
            inventory_rows.append(dict(source_row))
    obligation_ids = [row["id"] for row in inventory_rows]
    duplicate_obligations = sorted(value for value, count in Counter(obligation_ids).items() if count > 1)
    missing_sources = sorted(
        row["id"]
        for row in inventory_rows
        if not row.get("source") or not (ROOT / row["source"]["path"]).exists()
    )
    expected_by_kind = {
        "initiative-fr": 20,
        "feature-fr": 104,
        "feature-nfr": 77,
        "ux-decision": 52,
        "ux-acceptance": 52,
        "public-contract": 196,
        "public-client": 7,
        "current-control": 15,
        "conformance-assertion": 446,
    }
    actual_by_kind = Counter(row["kind"] for row in inventory_rows)
    if dict(actual_by_kind) != expected_by_kind:
        raise ValueError(f"Obligation denominator changed: expected={expected_by_kind}, actual={dict(actual_by_kind)}")
    if len(inventory_rows) != 969 or duplicate_obligations or missing_sources:
        raise ValueError(
            f"Inventory proof failed: total={len(inventory_rows)}, duplicates={duplicate_obligations}, missingSources={missing_sources}"
        )

    mappings = category_mappings(current_ids, set(obligation_ids))
    obligations, dispositions, orphan_ids = build_obligation_closures(
        inventory_module,
        inventory,
        mappings,
        current_ids,
    )
    if orphan_ids or len(dispositions) + sum(row["closure"]["kind"] != "governed-disposition" for row in obligations) != 969:
        raise ValueError(f"Zero-orphan closure proof failed: orphanIds={orphan_ids}")
    schema_hash = sha256_file(SCHEMA_PATH)
    current_dll = ROOT / "tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll"
    signed_decision = json.loads(SIGNED_DECISION_PATH.read_text(encoding="utf-8"))
    runtime_evidence = json.loads(RUNTIME_EVIDENCE_PATH.read_text(encoding="utf-8"))

    artifact_paths = [
        (SCHEMA_PATH, "v3-machine-contract"),
        (V2_PATH, "predecessor-manifest"),
        (V2_MARKDOWN_PATH, "predecessor-projection"),
        (BASELINE_PATH, "v1-14-suite-214-test-authority"),
        (SIGNED_REPORT_PATH, "later-approved-384-test-report"),
        (SIGNED_DECISION_PATH, "later-approved-384-test-decision"),
        (FINAL_CONFORMANCE_PATH, "signed-release-conformance-evidence"),
        (RUNTIME_EVIDENCE_PATH, "historical-runtime-boundary-evidence"),
        (CLEAN_BUILD_LOG_PATH, "clean-head-release-build-log"),
        (CLEAN_XML_PATH, "clean-head-conformance-result"),
        (OWNER_BUILD_LOG_PATH, "owner-review-conformance-build-log"),
        (OWNER_XML_PATH, "owner-review-conformance-result"),
        (TOOLCHAIN_PATH, "toolchain-identity-capture"),
    ]

    failed_ids = sorted(test_id for test_id, result in current_results.items() if result == "Fail")
    summaries = [
        {
            "kind": kind,
            "expected": expected_by_kind[kind],
            "actual": actual_by_kind[kind],
            "orphanCount": sum(row["kind"] == kind and row["orphan"] for row in obligations),
        }
        for kind in inventory_module.OBLIGATION_KINDS
    ]
    manifest: dict[str, Any] = {
        "$schema": SCHEMA_PATH.name,
        "artifact": "preservation-traceability-manifest",
        "schemaVersion": 3,
        "manifestVersion": VERSION,
        "status": "pending-owner-approval",
        "approval": {
            "state": "pending",
            "requestedRole": "Owner",
            "approver": None,
            "approvedAtUtc": None,
            "approvalReference": None,
            "signature": None,
        },
        "lineage": {
            "predecessor": artifact_binding(V2_PATH, "superseded-draft-candidate"),
            "predecessorVersion": json.loads(V2_PATH.read_text(encoding="utf-8"))["manifestVersion"],
            "v1Baseline": artifact_binding(BASELINE_PATH, "immutable-floor-authority"),
            "laterApproval": artifact_binding(SIGNED_DECISION_PATH, "signed-source-report-decision"),
            "laterApprovalDecisionId": signed_decision["decisionId"],
            "laterApprovalScope": signed_decision["decision"]["scope"],
            "supersessionBoundary": "This pending successor preserves v1 and the signed 384-case cumulative floor additively; it does not mutate, waive, or replace either authority artifact.",
        },
        "immutabilityPolicy": {
            "mode": "strict-add-only",
            "appliesTo": "the original 14-suite/214-test floor, all 170 later approved additions, every current pending addition, all seven mappings, and all 969 required preservation obligations",
            "prohibitedOperations": [
                "removal",
                "replacement",
                "merging",
                "reclassification",
                "waiver",
                "substitution",
                "denominator-shrinkage",
            ],
            "effect": "No prohibited operation may establish acceptance, zero-orphan status, conformance, or release readiness.",
        },
        "sourceBinding": {
            "committedRemediation": {
                "commit": ROOT_COMMIT,
                "tree": ROOT_TREE,
                "parent": ROOT_PARENT,
                "branch": "main",
                "remoteTracking": "origin/main",
                "headEqualsRemoteTrackingAtInitialVerification": True,
                "rootAndSubmodulesCleanAtInitialVerification": True,
                "changedPathCount": 50,
                "changedPaths": committed_change_set(),
            },
            "submoduleGitlinks": submodule_bindings(),
            "ownerReviewOverlay": {
                "committed": False,
                "baseCommit": ROOT_COMMIT,
                "bindings": current_overlay_bindings(),
                "purpose": "Restore the exact approved historical test ID and reconcile pending PRD/addendum state without creating a commit.",
            },
            "schema": {"path": relative(SCHEMA_PATH), "sha256": schema_hash},
        },
        "buildBindings": [
            {
                "id": "clean-head-release-solution-build",
                "sourceState": ROOT_COMMIT,
                "dotnetSdk": "10.0.401",
                "sdkCommit": "e34a38d2ae",
                "msbuild": "18.9.11+e34a38d2a",
                "configuration": "Release",
                "targetFramework": "net10.0",
                "result": "pass",
                "warnings": 0,
                "errors": 0,
                "log": artifact_binding(CLEAN_BUILD_LOG_PATH, "build-log"),
                "toolchainCapture": artifact_binding(TOOLCHAIN_PATH, "toolchain-identity"),
                "conformanceAssemblySha256": "7115530821146e5aaaf1871ce99425414e54346946aae015b7946ba0c4278300",
            },
            {
                "id": "owner-review-overlay-conformance-build",
                "sourceState": f"{ROOT_COMMIT}+uncommitted-owner-review-overlay",
                "dotnetSdk": "10.0.401",
                "sdkCommit": "e34a38d2ae",
                "msbuild": "18.9.11+e34a38d2a",
                "configuration": "Release",
                "targetFramework": "net10.0",
                "result": "pass",
                "warnings": 0,
                "errors": 0,
                "log": artifact_binding(OWNER_BUILD_LOG_PATH, "build-log"),
                "toolchainCapture": artifact_binding(TOOLCHAIN_PATH, "toolchain-identity"),
                "conformanceAssemblySha256": sha256_file(current_dll),
            },
        ],
        "commandBindings": [
            {
                "id": "owner-review-toolchain-capture",
                "command": "dotnet --info",
                "sourceState": f"{ROOT_COMMIT}+uncommitted-owner-review-overlay",
                "result": "pass",
            },
            {
                "id": "clean-head-restore",
                "command": "dotnet restore Hexalith.Conversations.slnx --force-evaluate -p:NuGetAudit=false",
                "sourceState": ROOT_COMMIT,
                "result": "pass",
            },
            {
                "id": "clean-head-release-build",
                "command": "dotnet build Hexalith.Conversations.slnx --configuration Release --no-restore --verbosity:minimal -m:1 '-flp:LogFile=_bmad-output/implementation-artifacts/preservation-traceability-v3/solution-build-release.log;Verbosity=minimal'",
                "sourceState": ROOT_COMMIT,
                "result": "pass",
            },
            {
                "id": "clean-head-conformance",
                "command": "tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -result-xml _bmad-output/implementation-artifacts/preservation-traceability-v3/conformance-release-clean-head.xml",
                "sourceState": ROOT_COMMIT,
                "result": "fail",
                "summary": clean_summary,
            },
            {
                "id": "owner-review-overlay-build",
                "command": "dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore --verbosity:minimal -m:1 '-flp:LogFile=_bmad-output/implementation-artifacts/preservation-traceability-v3/conformance-build-owner-review.log;Verbosity=minimal'",
                "sourceState": f"{ROOT_COMMIT}+uncommitted-owner-review-overlay",
                "result": "pass",
            },
            {
                "id": "owner-review-overlay-conformance",
                "command": "tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -result-xml _bmad-output/implementation-artifacts/preservation-traceability-v3/conformance-owner-review.xml",
                "sourceState": f"{ROOT_COMMIT}+uncommitted-owner-review-overlay",
                "result": "fail",
                "summary": current_summary,
            },
        ],
        "artifactBindings": [artifact_binding(path, role) for path, role in artifact_paths],
        "testDenominator": {
            "identityScheme": "xunit-runtime-fully-qualified-display-id",
            "originalV1Floor": {
                "sourceCommit": BASELINE_COMMIT,
                "suiteCount": 14,
                "testCount": 214,
                "suiteClasses": v1_suites,
                "testIds": v1_ids,
                "testIdsSha256": list_digest(v1_ids),
                "derivation": "Exact [Fact] fully qualified IDs extracted from the 14 baseline suite sources at the baseline commit.",
            },
            "laterApprovedAdditions": {
                "approvalDecisionPath": relative(SIGNED_DECISION_PATH),
                "approvalDecisionSha256": sha256_file(SIGNED_DECISION_PATH),
                "approvedSourceCommit": APPROVED_SOURCE_COMMIT,
                "approvedCumulativeCount": 384,
                "approvedAdditionCount": 170,
                "testIds": approved_additions,
                "testIdsSha256": list_digest(approved_additions),
                "cumulativeTestIds": approved_ids,
                "cumulativeTestIdsSha256": list_digest(approved_ids),
                "theoryMethods": approved_theories,
                "identityDerivation": derivation,
            },
            "currentCandidate": {
                "approvalState": "pending-owner-approval",
                "sourceState": f"{ROOT_COMMIT}+uncommitted-owner-review-overlay",
                "testCount": len(current_ids),
                "passed": current_summary["passed"],
                "failed": current_summary["failed"],
                "skipped": current_summary["skipped"],
                "testIds": current_ids,
                "testIdsSha256": list_digest(current_ids),
                "pendingAdditionCount": len(pending_additions),
                "pendingAdditionIds": pending_additions,
                "failedTestIds": failed_ids,
            },
            "preservationProof": {
                "missingFromOriginalV1Floor": sorted(v1_set - current_set),
                "missingFromApprovedCumulativeFloor": sorted(approved_set - current_set),
                "duplicateCurrentTestIds": [],
                "originalV1RetainedCount": len(v1_set & current_set),
                "approvedCumulativeRetainedCount": len(approved_set & current_set),
                "denominatorShrinkage": False,
            },
        },
        "categoryMappings": mappings,
        "obligationSummary": {
            "expected": 969,
            "actual": len(obligations),
            "requiredPreservationObligations": len(obligations),
            "activeInitiativeRequirementCount": sum(
                row["releaseActivationState"] == "active-initiative-scope" for row in obligations
            ),
            "activeInitiativeRequirementIds": sorted(
                row["id"]
                for row in obligations
                if row["releaseActivationState"] == "active-initiative-scope"
            ),
            "deferredInitiativeRequirementCount": sum(
                row["releaseActivationState"] == "deferred-not-active" for row in obligations
            ),
            "deferredInitiativeRequirementIds": sorted(
                row["id"]
                for row in obligations
                if row["releaseActivationState"] == "deferred-not-active"
            ),
            "releaseActivatedLegacyRequirementCount": 0,
            "pendingNotInferredCount": sum(
                row["releaseActivationState"] == "pending-not-inferred" for row in obligations
            ),
            "activationNotApplicableCount": sum(
                row["releaseActivationState"] == "not-applicable" for row in obligations
            ),
            "closureCount": sum(bool(row.get("closure")) for row in obligations),
            "pendingGovernedDispositionCount": len(dispositions),
            "orphanCount": 0,
            "duplicateIdCount": 0,
            "byKind": summaries,
        },
        "obligations": obligations,
        "governedDispositions": dispositions,
        "zeroOrphanProof": {
            "result": "pass",
            "expectedObligationCount": 969,
            "actualObligationCount": len(obligations),
            "closureCount": sum(bool(row.get("closure")) for row in obligations),
            "pendingGovernedDispositionCount": len(dispositions),
            "orphanIds": orphan_ids,
            "duplicateObligationIds": duplicate_obligations,
            "missingSourceIds": missing_sources,
            "mappedCategoryCount": len(mappings),
            "unmappedCategories": [],
            "mappedMissingRequirementIds": [],
            "mappedMissingTestIds": [],
            "missingOriginalV1TestIds": sorted(v1_set - current_set),
            "missingApprovedCumulativeTestIds": sorted(approved_set - current_set),
        },
        "gateStates": {
            "FR-20": {"state": "PENDING", "reason": "Owner approval, 100% conformance, and all release gates are not complete."},
            "SM-C1": {"state": "PENDING", "reason": "The exact additive manifest is pending Owner approval and conformance is not 100%."},
            "SM-C2": {
                "state": "FAILED",
                "rule": "Every measured path must satisfy the universal p95 regression threshold of <=5%; one failing path fails the metric.",
            },
            "OQ-1": {
                "state": "BLOCKED",
                "reason": "Recorded Owner authority does not close the current-gitlink runtime proof, broad Conformance, ineffective-grant, unclaimed package-publication, conditional rollback, and pending exact successor-manifest approval blockers.",
            },
            "implementationHold": {
                "state": "ACTIVE",
                "releaseAllowed": False,
                "exitCondition": "All required gates must genuinely pass; this pending manifest does not relax the hold.",
            },
        },
        "limitations": [
            "This manifest and its Owner-review overlay are uncommitted; no commit, approval, ownership, waiver, or signature is inferred.",
            "The historical runtime boundary record remains useful evidence but binds root base c0abd5cb and EventStore gitlink 27cc17f rather than the final d956c9b source and 629168e gitlink.",
            "The fresh clean-HEAD run failed 16 of 472 tests; the Owner-review overlay run failed 17 of 473 tests, so 100% conformance is not established.",
            "The approved 384 display-ID set is reconstructed from decision-bound source because no tracked historical XML enumerates every display name; stable method and ordered-argument identity is authoritative.",
            f"Historical runtime evidence reports status={runtime_evidence.get('status')} and rootWorkingTreeCommitted={runtime_evidence.get('sourceBinding', {}).get('rootWorkingTreeCommitted')}; it is not promoted to final-HEAD proof.",
        ],
    }
    return manifest


def render_markdown(manifest: dict[str, Any], json_sha256: str) -> str:
    source = manifest["sourceBinding"]["committedRemediation"]
    overlay = manifest["sourceBinding"]["ownerReviewOverlay"]
    denominator = manifest["testDenominator"]
    zero = manifest["zeroOrphanProof"]
    summary = manifest["obligationSummary"]
    lines = [
        "# Preservation Traceability Manifest v3 — Owner Review",
        "",
        f"- Version: `{manifest['manifestVersion']}`",
        "- Status: `pending-owner-approval`",
        f"- Canonical JSON SHA-256: `{json_sha256}`",
        "- Approval, approver, signature, waiver, and ownership record: `null`",
        "",
        f"Owner approval is requested for manifest `{manifest['manifestVersion']}` bound to canonical JSON SHA-256 `{json_sha256}`. This draft records no approval and remains `pending-owner-approval` until approval is captured in a required hash-bound authority record.",
        "",
        "## Bound source",
        "",
        "### Committed remediation",
        "",
        f"- Remediation commit: `{source['commit']}`",
        f"- Tree: `{source['tree']}`",
        f"- Parent: `{source['parent']}`",
        f"- Committed changed paths: `{source['changedPathCount']}`",
        "",
        "| Root submodule | Gitlink |",
        "|---|---|",
    ]
    for row in manifest["sourceBinding"]["submoduleGitlinks"]:
        lines.append(f"| `{row['path']}` | `{row['gitlink']}` |")
    lines.extend(
        [
            "",
            "### Uncommitted Owner-review overlay",
            "",
            f"- Base commit: `{overlay['baseCommit']}`",
            "- Committed: `false`",
            "- Clean-HEAD evidence: Release solution build passed; Conformance failed `456/472`.",
            "- Overlay evidence: focused Release build passed; Conformance failed `456/473`.",
            "",
            "| Overlay file | SHA-256 |",
            "|---|---|",
        ]
    )
    for row in overlay["bindings"]:
        lines.append(f"| `{row['path']}` | `{row['sha256']}` |")
    lines.extend(
        [
            "",
            "## Gate state",
            "",
            "- FR-20: `PENDING`",
            "- SM-C1: `PENDING`",
            "- SM-C2: `FAILED` under the universal every-path p95 regression rule of `<=5%`",
            f"- OQ-1: `BLOCKED` — {manifest['gateStates']['OQ-1']['reason']}",
            "- Implementation hold: `ACTIVE`",
            "",
            "## Evidence limitations",
            "",
        ]
    )
    lines.extend(f"- {item}" for item in manifest["limitations"])
    lines.extend(
        [
            "",
            "## Additive test denominator",
            "",
            "| Boundary | Count | Exact-ID SHA-256 | State |",
            "|---|---:|---|---|",
            f"| Original v1 floor | {denominator['originalV1Floor']['testCount']} across 14 suites | `{denominator['originalV1Floor']['testIdsSha256']}` | immutable |",
            f"| Later approved additions | {denominator['laterApprovedAdditions']['approvedAdditionCount']} | `{denominator['laterApprovedAdditions']['testIdsSha256']}` | approved only through bound signed decision |",
            f"| Approved cumulative floor | {denominator['laterApprovedAdditions']['approvedCumulativeCount']} | `{denominator['laterApprovedAdditions']['cumulativeTestIdsSha256']}` | immutable |",
            f"| Current candidate | {denominator['currentCandidate']['testCount']} | `{denominator['currentCandidate']['testIdsSha256']}` | pending Owner approval; {denominator['currentCandidate']['failed']} failing |",
            f"| Pending additions beyond approved floor | {denominator['currentCandidate']['pendingAdditionCount']} | embedded in canonical JSON | pending |",
            "",
            "No removal, replacement, merging, reclassification, waiver, substitution, or denominator shrinkage may establish acceptance.",
            "",
            "## Seven-category mapping summary",
            "",
            "| Category | Requirement IDs | Exact test count | Approval |",
            "|---|---|---:|---|",
        ]
    )
    for mapping in manifest["categoryMappings"]:
        requirements = ", ".join(f"`{value}`" for value in mapping["requirementIds"])
        lines.append(
            f"| `{mapping['category']}` | {requirements} | {len(mapping['testIds'])} | `{mapping['approvalState']}` |"
        )
    lines.extend(
        [
            "",
            "## Preservation obligations and zero-orphan proof",
            "",
            f"The manifest inventories **{summary['requiredPreservationObligations']} required preservation obligations**. Exactly **{summary['activeInitiativeRequirementCount']} initiative requirements** are active initiative scope; deferred `FR-16` is explicitly `deferred-not-active`; and **{summary['releaseActivatedLegacyRequirementCount']} legacy requirements** are release-activated. **{summary['pendingNotInferredCount']} legacy/UX obligations** remain `pending-not-inferred`, and activation is not applicable to **{summary['activationNotApplicableCount']} supporting contract, client, control, and assertion rows**.",
            "",
            f"Every row has a computed closure: **{zero['closureCount']}/969**. Of these, **{zero['pendingGovernedDispositionCount']}** point to explicit pending governed dispositions with no activation effect. Orphans: **{len(zero['orphanIds'])}**. Duplicate IDs: **{len(zero['duplicateObligationIds'])}**. Missing mapped tests or requirements: **0**.",
            "",
            "This proves inventory and traceability completeness only. It does not establish release activation, conformance pass, approval, waiver, or acceptance.",
            "",
            "| Kind | Expected | Actual | Orphans |",
            "|---|---:|---:|---:|",
        ]
    )
    for row in summary["byKind"]:
        lines.append(f"| `{row['kind']}` | {row['expected']} | {row['actual']} | {row['orphanCount']} |")
    lines.extend(
        [
            "",
            "The canonical JSON contains all 969 obligation rows, 277 pending disposition records, exact source bindings, exact denominator IDs, commands, build identities, artifact hashes, and proof lists.",
            "",
            "## Reference appendix — exact seven-category mappings",
            "",
        ]
    )
    for mapping in manifest["categoryMappings"]:
        lines.extend(
            [
                f"### {mapping['category']}",
                "",
                "Requirements: " + ", ".join(f"`{value}`" for value in mapping["requirementIds"]),
                "",
                "Exact fully qualified test IDs:",
                "",
            ]
        )
        lines.extend(f"- `{test_id}`" for test_id in mapping["testIds"])
        lines.append("")
    return "\n".join(lines) + "\n"


def expected_outputs() -> dict[Path, bytes]:
    manifest = build_manifest()
    json_bytes = canonical_json(manifest).encode("utf-8")
    json_hash = sha256_bytes(json_bytes)
    markdown_bytes = render_markdown(manifest, json_hash).encode("utf-8")
    digest_bytes = (
        f"{json_hash}  {MANIFEST_PATH.name}\n"
        f"{sha256_bytes(markdown_bytes)}  {MARKDOWN_PATH.name}\n"
        f"{sha256_file(SCHEMA_PATH)}  {SCHEMA_PATH.name}\n"
    ).encode("utf-8")
    return {
        MANIFEST_PATH: json_bytes,
        MARKDOWN_PATH: markdown_bytes,
        DIGEST_PATH: digest_bytes,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="Check byte-exact generated artifacts without writing them.")
    args = parser.parse_args()
    immutable_hashes = {
        MANIFEST_PATH: "a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc",
        MARKDOWN_PATH: "c3dad90b6f0feb567bc1fa26a677dc3d88e23b112fd9767b30dfaecadf0e84f8",
        SCHEMA_PATH: "3abf8f0d90c2257e865353743ca612b71b0d425d53196769a57dcb66525fbdfb",
        DIGEST_PATH: "d88f1ccad166046cf17e3ae499dffb26cf3e8bfe9f7f24ac26d9190e67533979",
    }
    if args.check:
        stale = [relative(path) for path, digest in immutable_hashes.items() if not path.exists() or sha256_file(path) != digest]
        if stale:
            print("STALE " + ", ".join(stale))
            return 1
        print("PASS immutable preservation-traceability-manifest-v3 rc.1 byte-exact check")
        return 0
    print("REFUSED approved 3.0.0-rc.1 artifacts are immutable; generate an unapproved successor instead")
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
