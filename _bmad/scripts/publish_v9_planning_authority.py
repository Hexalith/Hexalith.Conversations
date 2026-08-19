#!/usr/bin/env python3
"""Publish and validate the candidate-bound V14 planning companion set."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shlex
import subprocess
import sys
import tempfile
from typing import Any
import unicodedata


sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))

from config_utils import ConfigError, load_customization  # noqa: E402


BASE_EPIC_AUTHORITY = "epic-6-authority-2026-08-03-v10"
BASE_ARCHITECTURE_AUTHORITY = "conversations-architecture-2026-08-03-v10"
BASE_AUTHORITIES = {"epic": BASE_EPIC_AUTHORITY, "architecture": BASE_ARCHITECTURE_AUTHORITY}
V11_EPIC_AUTHORITY = "epic-6-authority-2026-08-04-v11"
V11_ARCHITECTURE_AUTHORITY = "conversations-architecture-2026-08-04-v11"
V11_AUTHORITIES = {"epic": V11_EPIC_AUTHORITY, "architecture": V11_ARCHITECTURE_AUTHORITY}
V12_EPIC_AUTHORITY = "epic-6-authority-2026-08-04-v12"
V12_ARCHITECTURE_AUTHORITY = "conversations-architecture-2026-08-04-v12"
V12_AUTHORITIES = {"epic": V12_EPIC_AUTHORITY, "architecture": V12_ARCHITECTURE_AUTHORITY}
EPIC_AUTHORITY = "epic-6-authority-2026-08-18-v14"
ARCHITECTURE_AUTHORITY = "conversations-architecture-2026-08-18-v14"
AUTHORITIES = {"epic": EPIC_AUTHORITY, "architecture": ARCHITECTURE_AUTHORITY}
EPICS_PATH = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md"
ARCHITECTURE_PATH = "_bmad-output/planning-artifacts/architecture.md"
UX_MAP_PATH = "_bmad-output/planning-artifacts/ux-requirement-map.md"
SPRINT_PATH = "_bmad-output/implementation-artifacts/sprint-status.yaml"
VIEW_V1_PATH = "_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md"
VIEW_V2_PATH = "_bmad-output/planning-artifacts/epic-6-current-execution-view-v2.md"
BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
GRAPH_PATH = "_bmad-output/planning-artifacts/v9-execution-graph-v1.json"
SUPERSESSION_PATH = "_bmad-output/planning-artifacts/v9-supersession-map-v1.json"
SLICE_PATH = "_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json"
REMEDIATION_PATH = "_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json"
CURRENT_PROOF_PATH = "_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json"
CURRENT_CANDIDATE_PATH = "_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json"
PUBLICATION_SCOPE_PATH = "_bmad-output/planning-artifacts/v14-planning-publication-scope-v1.json"
SPEC_PATH = "_bmad-output/implementation-artifacts/spec-epic-16-planning-authority-publication.md"
V14_PROPOSAL_PATH = "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18.md"
SUPERSESSION_CONTRACT_PATH = "_bmad-output/planning-artifacts/epic-6-completion-supersession-contract-v1.json"
RUNBOOK_PATH = "docs/runbooks/evidence-boundary-validation.md"
V9_EPIC_BLOCK_SIZE = 188677
V9_EPIC_BLOCK_DIGEST = "e7d6ea5759c12ab70f21b472656828bb4e5bcce2023d845f06a40cf1373d1c9d"
V9_ARCHITECTURE_BLOCK_SIZE = 18270
V9_ARCHITECTURE_BLOCK_DIGEST = "4686212387189e78f98de5352d12eb8544d1a9f78c97dfc446266fa3d4d3f3d9"
V10_EPIC_BLOCK_SIZE = 8746
V10_EPIC_BLOCK_DIGEST = "3c33462d0bc28f9fec36e571d7dcf4a60c77d02c94bd3675528a05d704d07588"
V10_ARCHITECTURE_BLOCK_SIZE = 3846
V10_ARCHITECTURE_BLOCK_DIGEST = "893315bff3f12d7b949dbeae2a2dfbb301023461ad62c0c6066480a87700774b"
V11_EPIC_BLOCK_SIZE = 5474
V11_EPIC_BLOCK_DIGEST = "6c9bd7164ef35e4093d69226a5988fe73f5400aab3049911a3298b0987d79f19"
V11_ARCHITECTURE_BLOCK_SIZE = 3042
V11_ARCHITECTURE_BLOCK_DIGEST = "a97385c11b92fb95f1acf7f4ba370404c4781855581c81a427ed6895a5a4c4d1"
V12_EPIC_BLOCK_SIZE = 6207
V12_EPIC_BLOCK_DIGEST = "39f1b51920e4866c47586caf549aafaec5678639b64c64b4b235788fce76e878"
V12_ARCHITECTURE_BLOCK_SIZE = 6075
V12_ARCHITECTURE_BLOCK_DIGEST = "3050b326c5759fc51bc0e800944b0a1a591ab1782f6798f12abfdc10051b5796"
V13_ARCHITECTURE_BLOCK_SIZE = 17857
V13_ARCHITECTURE_BLOCK_DIGEST = "c7d5c867385f22e359c5367fe2851fc4f7d016e0f28c04b5283b7b5ad604605a"
V14_EPIC_BLOCK_SIZE = 11093
V14_EPIC_BLOCK_DIGEST = "acd5c07c72d5145bb6477877cab21af710beb8cf172ccfde66837992e41c35c1"
V14_ARCHITECTURE_BLOCK_SIZE = 3873
V14_ARCHITECTURE_BLOCK_DIGEST = "d33d977fda0776377684439bb7e78769a6b9a0279c293b8a08e44dfad8466dc5"
FROZEN_STORY_CONTRACT_SCHEMA_DIGEST = "33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0"
CURRENT_PROOF_DIGEST = "f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f"
CURRENT_CANDIDATE_DIGEST = "e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f"
V9_STORY_CONTRACT_SCHEMA_IDENTITY = "hexalith.conversations.story-contract.v1"
V14_STORY_CONTRACT_SCHEMA_IDENTITY = "hexalith.conversations.v14-story-contract.v1"

SLICE_ID = "7.1-SCHEMAS"
SLICE_PREDECESSORS = ("6.2", "IR-0")
SLICE_WRITABLE_PATHS = (
    "_bmad/schemas/v9-acceptance-result-v1.schema.json",
    "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/scripts/tests/test_generate_story_record.py",
    "artifacts/v9/schema-slice/v2-schema-contract.xml",
)
SLICE_READ_ONLY_INPUTS = (
    "_bmad/schemas/v9-story-contract-v1.schema.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
    SLICE_PATH,
)
SLICE_PROHIBITED_PATHS = (
    {"match": "exact", "path": "_bmad/scripts/generate_story_record.py"},
    {"match": "exact", "path": ".gitmodules"},
    {"match": "exact", "path": "Directory.Packages.props"},
    {"match": "prefix", "path": "src/"},
    {"match": "prefix", "path": "tests/"},
    {"match": "prefix", "path": "references/"},
    {"match": "prefix", "path": "docs/release-evidence/"},
    {"match": "prefix", "path": "artifacts/v9/7.1/"},
    {"match": "prefix", "path": "_bmad-output/implementation-artifacts/"},
    {"match": "prefix", "path": "_bmad-output/planning-artifacts/"},
)
SLICE_COMMAND = (
    "python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py "
    "-k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml"
)
SLICE_ROLLBACK = (
    "Remove only the three new schemas, schema-specific test changes, and checkpoint result; "
    "preserve the existing story-contract schema, planning authority, publisher, completed history, "
    "and all non-checkpoint work."
)
EPIC_6_RETROSPECTIVE_IDS = (
    "epic-6-retro-item-24-produce-an-additive-epic-6-completion-su",
    "epic-6-retro-item-25-restore-the-submodule-promotion-and-evid",
    "epic-6-retro-item-26-harden-planning-authority-verification-t",
    "epic-6-retro-item-27-create-approved-successor-work-for-a-dur",
    "epic-6-retro-item-28-create-approved-successor-work-for-deter",
    "epic-6-retro-item-29-add-explicit-preflight-diagnostics-for-a",
)
# Item 24 may be `done` after an ACCEPTED V13 current-proof decision is
# human-applied; items 25-29 remain open until their owning checkpoints close.
EPIC_6_RETROSPECTIVE_STATUSES = ("done", "open", "open", "open", "open", "open")
EPIC_6_RETROSPECTIVE_DIGEST = "40ef127d1785c653a640554e75f6f16f2a9c3f92c14573aa21bdcb197ac2908c"

MECHANICAL_PATHS = (
    ".agents/skills/bmad-build/step-04-review.md",
    ".agents/skills/bmad-build/step-05-present.md",
    ".agents/skills/bmad-build/step-oneshot.md",
    ".agents/skills/bmad-build-auto/step-04-review.md",
    ".agents/skills/bmad-dev-story/SKILL.md",
    ".agents/skills/bmad-code-review/steps/step-04-present.md",
    ".claude/skills/bmad-build/step-04-review.md",
    ".claude/skills/bmad-build/step-05-present.md",
    ".claude/skills/bmad-build/step-oneshot.md",
    ".claude/skills/bmad-build-auto/step-04-review.md",
    ".claude/skills/bmad-dev-story/SKILL.md",
    ".claude/skills/bmad-code-review/steps/step-04-present.md",
)
MECHANICAL_LOGICAL_BODIES = (
    "bmad-build/step-04-review.md",
    "bmad-build/step-05-present.md",
    "bmad-build/step-oneshot.md",
    "bmad-build-auto/step-04-review.md",
    "bmad-dev-story/SKILL.md",
    "bmad-code-review/steps/step-04-present.md",
)
MECHANICAL_LIFECYCLE_TOKENS = {
    "bmad-build/step-04-review.md": "Change `{spec_file}` status to `in-review`",
    "bmad-build/step-05-present.md": "Change `{spec_file}` status to `done`",
    "bmad-build/step-oneshot.md": "status: 'done'",
    "bmad-build-auto/step-04-review.md": "Change `{spec_file}` status to `in-review`",
    "bmad-dev-story/SKILL.md": '<action>Update the story Status to: "review"</action>',
    "bmad-code-review/steps/step-04-present.md": "set `{new_status}` = `done`",
}
GUIDANCE_PATHS = (
    "_bmad/custom/bmad-build.toml",
    "_bmad/custom/bmad-build-auto.toml",
    "_bmad/custom/bmad-review.toml",
    RUNBOOK_PATH,
)
VALIDATOR_PATHS = (
    "_bmad/scripts/tests/test_publish_v9_planning_authority.py",
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
)
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
EXPECTED_UX_DECISION_IDS = tuple(f"UX-DR{value}" for value in range(1, 53))
EXPECTED_UX_ACCEPTANCE_IDS = (
    *(f"AC-SAFE-{value:03d}" for value in range(1, 9)),
    *(f"AC-RESP-{value:03d}" for value in range(1, 16)),
    "AC-A11Y-001",
    "AC-A11Y-002",
    "AC-LEAK-001",
    "AC-MOB-001",
    "AC-PERF-001",
)
OBLIGATION_LEDGER_ID = "V9-V8-OBLIGATION-LEDGER-v1"
OBLIGATION_LEDGER_DIGEST = "4dbffda456c4f40055985f303ed9d10d8e7839573e2486c4d01ca5508dca8f87"
GUIDANCE_SEMANTICS = (
    "Recompute every declared source hash",
    "repository-relative, contained by the resolved",
    "Recompute the signable payload from canonical manifest rows",
    "exact set equality",
    "raw Git mode `160000`",
    "asserted inventory row identities to equal the frozen source inventory exactly",
    "Pin roots of trust",
    "nonempty evaluated assertion ledger",
)
READER_PATHS = (
    "tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs",
    "tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs",
    "tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs",
    "tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs",
    "tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs",
    "tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs",
    "tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs",
    "tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs",
    "tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs",
)
ALIAS_PATHS = (
    ".agents/skills/bmad-dev-auto/SKILL.md",
    ".agents/skills/bmad-quick-dev/SKILL.md",
    ".claude/skills/bmad-dev-auto/SKILL.md",
    ".claude/skills/bmad-quick-dev/SKILL.md",
)
CUSTOMIZATION_DEFAULT_PATHS = tuple(
    f".{tree}/skills/{skill}/customize.toml"
    for tree in ("agents", "claude")
    for skill in ("bmad-build", "bmad-build-auto", "bmad-review")
)
SCHEMA_PATHS = (
    "_bmad/schemas/v9-story-contract-v1.schema.json",
    "_bmad/schemas/v9-inventory-v1.schema.json",
    "_bmad/schemas/v9-execution-graph-v1.schema.json",
    "_bmad/schemas/v9-supersession-map-v1.schema.json",
    "_bmad/schemas/v9-authority-bundle-v1.schema.json",
    "_bmad/schemas/v11-story-slice-authority-v1.schema.json",
    "_bmad/schemas/v12-pre-ir0-remediation-authority-v1.schema.json",
    "_bmad/schemas/epic-6-completion-supersession-v1.schema.json",
    "_bmad/schemas/v14-story-contract-v1.schema.json",
    "_bmad/schemas/v13-current-proof-authority-v1.schema.json",
    "_bmad/schemas/v14-current-candidate-authority-v1.schema.json",
)
V12_CANONICAL_PATHS = (
    "_bmad-output/implementation-artifacts/epic-6-context.md",
    SUPERSESSION_CONTRACT_PATH,
    "pyproject.toml",
    "uv.lock",
    ".github/workflows/planning-authority-preflight.yml",
    "_bmad/scripts/verify_submodule_promotion.py",
    "_bmad/scripts/tests/test_verify_submodule_promotion.py",
    "_bmad/scripts/verify_evidence_boundary.py",
    "_bmad/scripts/tests/test_verify_evidence_boundary.py",
    "_bmad/scripts/verify_epic_6_completion_supersession.py",
    "_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py",
    ".agents/skills/bmad-build/compile-epic-context.md",
    ".agents/skills/bmad-build/step-01-clarify-and-route.md",
    ".agents/skills/bmad-build-auto/compile-epic-context.md",
    ".agents/skills/bmad-build-auto/step-01-clarify-and-route.md",
    ".claude/skills/bmad-build/compile-epic-context.md",
    ".claude/skills/bmad-build/step-01-clarify-and-route.md",
    ".claude/skills/bmad-build-auto/compile-epic-context.md",
    ".claude/skills/bmad-build-auto/step-01-clarify-and-route.md",
)
CANONICAL_PATHS = (
    ".gitmodules",
    EPICS_PATH,
    ARCHITECTURE_PATH,
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18-e6-remediation-a3.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md",
    V14_PROPOSAL_PATH,
    PUBLICATION_SCOPE_PATH,
    *GUIDANCE_PATHS,
    "_bmad/scripts/publish_v9_planning_authority.py",
    "_bmad/scripts/publish_v13_current_proof_authority.py",
    "_bmad/scripts/publish_v14_current_candidate_authority.py",
    "_bmad/scripts/tests/test_publish_v13_current_proof_authority.py",
    "_bmad/scripts/tests/test_publish_v14_current_candidate_authority.py",
    *VALIDATOR_PATHS,
    *SCHEMA_PATHS,
    *V12_CANONICAL_PATHS,
)
PROTECTED_CANDIDATE_PATHS = (
    VIEW_V1_PATH,
    CURRENT_PROOF_PATH,
    CURRENT_CANDIDATE_PATH,
    *MECHANICAL_PATHS,
    *ALIAS_PATHS,
    *CUSTOMIZATION_DEFAULT_PATHS,
    *READER_PATHS,
)
EXPECTED_STORY_IDS = tuple(
    [f"7.{value}" for value in range(1, 5)]
    + [f"8.{value}" for value in range(1, 3)]
    + [f"9.{value}" for value in range(1, 3)]
    + [f"10.{value}" for value in range(1, 5)]
    + [f"11.{value}" for value in range(1, 4)]
    + [f"12.{value}" for value in range(1, 5)]
    + [f"13.{value}" for value in range(1, 4)]
    + [f"14.{value}" for value in range(1, 4)]
    + [f"15.{value}" for value in range(1, 3)]
    + [f"16.{value}" for value in range(1, 4)]
)
LEGACY_STORY_IDS = EXPECTED_STORY_IDS[:-3]
V14_AMENDED_STORY_IDS = ("12.1", "13.1", "14.1", "15.1", "16.1", "16.2", "16.3")
V14_PREDECESSOR_AMENDMENTS = {story_id: ("16.3",) for story_id in ("12.1", "13.1", "14.1", "15.1")}
PINNED_AUTHORITY_PATHS = (CURRENT_PROOF_PATH, CURRENT_CANDIDATE_PATH)
EXPECTED_OUTPUT_PATHS = (
    BUNDLE_PATH,
    GRAPH_PATH,
    SUPERSESSION_PATH,
    SLICE_PATH,
    REMEDIATION_PATH,
    VIEW_V2_PATH,
    UX_MAP_PATH,
    SPRINT_PATH,
    "_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json",
    "_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v3.json",
    "_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json",
    "_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json",
    "_bmad-output/planning-artifacts/v9/resolved-customization/bmad-build.json",
    "_bmad-output/planning-artifacts/v9/resolved-customization/bmad-build-auto.json",
    "_bmad-output/planning-artifacts/v9/resolved-customization/bmad-review.json",
    *(f"_bmad-output/planning-artifacts/v9/story-contracts/{story_id}.json" for story_id in EXPECTED_STORY_IDS),
)


class PublicationError(RuntimeError):
    """A stable fail-closed planning-publication failure."""

    def __init__(self, code: str, detail: str) -> None:
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail


def sha256(content: bytes) -> str:
    """Return a canonical lowercase SHA-256 digest."""

    return hashlib.sha256(content).hexdigest()


def json_bytes(value: Any) -> bytes:
    """Serialize deterministic UTF-8 JSON."""

    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def inventory_digest(paths: tuple[str, ...] | list[str]) -> str:
    """Hash the canonical NFC UTF-8 LF path inventory."""

    canonical = unicodedata.normalize("NFC", "\n".join(paths) + "\n")
    return sha256(canonical.encode("utf-8"))


def git(root: Path, *arguments: str) -> bytes:
    """Run one bounded non-interactive Git command."""

    try:
        result = subprocess.run(
            ("git", "-C", str(root), *arguments),
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=30,
            env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
        )
    except subprocess.TimeoutExpired as error:
        raise PublicationError("CANDIDATE_GIT_UNAVAILABLE", "Git command timed out") from error
    except OSError as error:
        raise PublicationError("CANDIDATE_GIT_UNAVAILABLE", str(error)) from error
    if result.returncode != 0:
        detail = result.stderr.decode("utf-8", errors="replace").strip()
        raise PublicationError("CANDIDATE_GIT_UNAVAILABLE", detail or "Git command failed")
    return result.stdout


def resolve_candidate(root: Path, requested: str | None, check: bool) -> str:
    """Resolve the one immutable planning-candidate commit."""

    if check and requested is None:
        bundle = root / BUNDLE_PATH
        if not bundle.is_file():
            raise PublicationError("BUNDLE_NOT_PUBLISHED", BUNDLE_PATH)
        try:
            requested = json.loads(bundle.read_text(encoding="utf-8"))["planningCandidate"]
        except (OSError, json.JSONDecodeError, KeyError, TypeError) as error:
            raise PublicationError("BUNDLE_INVALID", str(error)) from error
    revision = requested or "HEAD"
    candidate = git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").decode().strip()
    if not re.fullmatch(r"[0-9a-f]{40}", candidate):
        raise PublicationError("CANDIDATE_NOT_COMMITTED", candidate)
    return candidate


def candidate_blob(root: Path, candidate: str, relative_path: str) -> bytes:
    """Read an exact repository-relative blob from the planning candidate."""

    path = Path(relative_path)
    if path.is_absolute() or ".." in path.parts:
        raise PublicationError("REPOSITORY_PATH_ESCAPE", relative_path)
    try:
        return git(root, "show", f"{candidate}:{relative_path}")
    except PublicationError as error:
        raise PublicationError("CANDIDATE_PATH_MISSING", relative_path) from error


def require_candidate_bytes(root: Path, candidate: str, paths: tuple[str, ...]) -> None:
    """Require current protected bytes to equal their committed candidate blobs."""

    for relative_path in paths:
        current_path = root / relative_path
        if not current_path.is_file():
            raise PublicationError("CANDIDATE_PATH_MISSING", relative_path)
        try:
            current = current_path.read_bytes()
        except OSError as error:
            raise PublicationError("CANDIDATE_SOURCE_READ_FAILED", f"{relative_path}: {error}") from error
        if current != candidate_blob(root, candidate, relative_path):
            raise PublicationError("CANDIDATE_SOURCE_DRIFT", relative_path)


def marker_block(content: bytes, begin_token: str, end_token: str) -> bytes:
    """Extract one inclusive marker block from exact bytes."""

    begin = begin_token.encode("utf-8")
    end = end_token.encode("utf-8")
    if content.count(begin) != 1 or content.count(end) != 1:
        raise PublicationError("AUTHORITY_MARKER_INVALID", f"{begin_token} / {end_token}")
    start = content.find(begin)
    end_start = content.find(end, start + len(begin))
    close_start = content.find(b"-->", end_start + len(end)) if end_start >= 0 else -1
    if start < 0 or end_start <= start or close_start < end_start:
        raise PublicationError("AUTHORITY_MARKER_INVALID", f"{begin_token} / {end_token}")
    close = close_start + 3
    return content[start:close]


def validate_authority_prefixes(epics: bytes, architecture: bytes) -> tuple[str, str, str, str]:
    """Validate frozen V9-V13 history and the complete V14 successor."""

    v9_epic = marker_block(
        epics,
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:END",
    )
    if len(v9_epic) != V9_EPIC_BLOCK_SIZE or sha256(v9_epic) != V9_EPIC_BLOCK_DIGEST:
        raise PublicationError("V9_EPIC_PREFIX_DRIFT", sha256(v9_epic))
    v9_architecture = marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V9:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V9:END",
    )
    if (
        len(v9_architecture) != V9_ARCHITECTURE_BLOCK_SIZE
        or sha256(v9_architecture) != V9_ARCHITECTURE_BLOCK_DIGEST
    ):
        raise PublicationError("V9_ARCHITECTURE_PREFIX_DRIFT", sha256(v9_architecture))
    v10_epic_bytes = marker_block(
        epics,
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V10:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V10:END",
    )
    if len(v10_epic_bytes) != V10_EPIC_BLOCK_SIZE or sha256(v10_epic_bytes) != V10_EPIC_BLOCK_DIGEST:
        raise PublicationError("V10_EPIC_PREFIX_DRIFT", sha256(v10_epic_bytes))
    v10_architecture_bytes = marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V10:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V10:END",
    )
    if (
        len(v10_architecture_bytes) != V10_ARCHITECTURE_BLOCK_SIZE
        or sha256(v10_architecture_bytes) != V10_ARCHITECTURE_BLOCK_DIGEST
    ):
        raise PublicationError("V10_ARCHITECTURE_PREFIX_DRIFT", sha256(v10_architecture_bytes))
    v11_epic_bytes = marker_block(
        epics,
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:END",
    )
    if len(v11_epic_bytes) != V11_EPIC_BLOCK_SIZE or sha256(v11_epic_bytes) != V11_EPIC_BLOCK_DIGEST:
        raise PublicationError("V11_EPIC_AUTHORITY_DRIFT", sha256(v11_epic_bytes))
    v11_architecture_bytes = marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V11:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V11:END",
    )
    if (
        len(v11_architecture_bytes) != V11_ARCHITECTURE_BLOCK_SIZE
        or sha256(v11_architecture_bytes) != V11_ARCHITECTURE_BLOCK_DIGEST
    ):
        raise PublicationError("V11_ARCHITECTURE_AUTHORITY_DRIFT", sha256(v11_architecture_bytes))
    v12_epic_bytes = marker_block(
        epics,
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V12:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V12:END",
    )
    if len(v12_epic_bytes) != V12_EPIC_BLOCK_SIZE or sha256(v12_epic_bytes) != V12_EPIC_BLOCK_DIGEST:
        raise PublicationError("V12_EPIC_AUTHORITY_DRIFT", sha256(v12_epic_bytes))
    v12_architecture_bytes = marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V12:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V12:END",
    )
    if (
        len(v12_architecture_bytes) != V12_ARCHITECTURE_BLOCK_SIZE
        or sha256(v12_architecture_bytes) != V12_ARCHITECTURE_BLOCK_DIGEST
    ):
        raise PublicationError("V12_ARCHITECTURE_AUTHORITY_DRIFT", sha256(v12_architecture_bytes))
    v13_architecture_bytes = marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V13:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V13:END",
    )
    if (
        len(v13_architecture_bytes) != V13_ARCHITECTURE_BLOCK_SIZE
        or sha256(v13_architecture_bytes) != V13_ARCHITECTURE_BLOCK_DIGEST
    ):
        raise PublicationError("V13_ARCHITECTURE_AUTHORITY_DRIFT", sha256(v13_architecture_bytes))
    v14_epic_bytes = marker_block(
        epics,
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V14:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V14:END",
    )
    if len(v14_epic_bytes) != V14_EPIC_BLOCK_SIZE or sha256(v14_epic_bytes) != V14_EPIC_BLOCK_DIGEST:
        raise PublicationError("V14_EPIC_AUTHORITY_DRIFT", sha256(v14_epic_bytes))
    v14_architecture_bytes = marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V14:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V14:END",
    )
    if (
        len(v14_architecture_bytes) != V14_ARCHITECTURE_BLOCK_SIZE
        or sha256(v14_architecture_bytes) != V14_ARCHITECTURE_BLOCK_DIGEST
    ):
        raise PublicationError("V14_ARCHITECTURE_AUTHORITY_DRIFT", sha256(v14_architecture_bytes))
    try:
        v10_epic = v10_epic_bytes.decode("utf-8")
        v10_architecture = v10_architecture_bytes.decode("utf-8")
        v11_epic = v11_epic_bytes.decode("utf-8")
        v11_architecture = v11_architecture_bytes.decode("utf-8")
        v12_epic = v12_epic_bytes.decode("utf-8")
        v12_architecture = v12_architecture_bytes.decode("utf-8")
        v13_architecture = v13_architecture_bytes.decode("utf-8")
        v14_epic = v14_epic_bytes.decode("utf-8")
        v14_architecture = v14_architecture_bytes.decode("utf-8")
    except UnicodeError as error:
        raise PublicationError("AUTHORITY_MARKER_INVALID", str(error)) from error
    for source in (v10_epic, v10_architecture):
        if BASE_EPIC_AUTHORITY not in source or BASE_ARCHITECTURE_AUTHORITY not in source:
            raise PublicationError("AUTHORITY_IDENTITY_DRIFT", "v10 identity missing")
        if "hold" not in source.lower() or "ACTIVE" not in source:
            raise PublicationError("IMPLEMENTATION_HOLD_DRIFT", "ACTIVE hold missing")
    if v10_epic.count("Story 10.3 V10 Amendment") != 1 or v10_epic.count("Story 10.4 V10 Amendment") != 1:
        raise PublicationError("AUTHORITY_SCOPE_DRIFT", "effective Story 10.3/10.4 amendments missing")
    for source in (v11_epic, v11_architecture):
        if V11_EPIC_AUTHORITY not in source or V11_ARCHITECTURE_AUTHORITY not in source:
            raise PublicationError("AUTHORITY_IDENTITY_DRIFT", "v11 identity missing")
        if "hold" not in source.lower() or "ACTIVE" not in source:
            raise PublicationError("IMPLEMENTATION_HOLD_DRIFT", "ACTIVE hold missing")
    required_v11_epic_clauses = (
        "Story 7.1 V11 Schema-Checkpoint Amendment",
        "Exact execution predecessors:** `6.2`, `IR-0`",
        SLICE_COMMAND,
        "never marks Story\n7.1 `done`, never produces a final record, and never unlocks a successor",
        "epic-6-retrospective: done",
    )
    if v11_epic.count(
        "### Story 7.1 V11 Schema-Checkpoint Amendment: Authorize A Non-Story Slice"
    ) != 1:
        raise PublicationError("STORY_SLICE_AUTHORITY_DRIFT", "checkpoint amendment heading count")
    if any(clause not in v11_epic for clause in required_v11_epic_clauses):
        raise PublicationError("STORY_SLICE_AUTHORITY_DRIFT", "v11 epic checkpoint clause missing")
    required_v11_architecture_clauses = (
        "kind\n`checkpoint` and exact predecessors `6.2` and `IR-0`",
        "Story 7.2 still\ndepends on complete Story 7.1",
        "There is no scoped exception state",
        "all\nsix ordered open action rows exactly",
    )
    if any(clause not in v11_architecture for clause in required_v11_architecture_clauses):
        raise PublicationError("STORY_SLICE_AUTHORITY_DRIFT", "v11 architecture checkpoint clause missing")
    for source in (v12_epic, v12_architecture):
        if V12_EPIC_AUTHORITY not in source or V12_ARCHITECTURE_AUTHORITY not in source:
            raise PublicationError("AUTHORITY_IDENTITY_DRIFT", "v12 identity missing")
        if "hold" not in source.lower() or "ACTIVE" not in source:
            raise PublicationError("IMPLEMENTATION_HOLD_DRIFT", "v12 ACTIVE hold missing")
    required_v12_epic_clauses = (
        "### E6-REMEDIATION: Own A1-A3 Before Independent IR-0",
        "A1-A3 are all required before IR-0 may run",
        "Exactly twelve active route files",
        "implementationHold` remains `ACTIVE`",
    )
    required_v12_architecture_clauses = (
        "PC-PUBLICATION -> E6-REMEDIATION -> IR-0",
        "The graph has exactly 33 nodes",
        "Current root or\nsubmodule checkout bytes are never admissible substitutes",
        "E6-REMEDIATION may authorize only an independent IR-0 rerun",
    )
    if any(clause not in v12_epic for clause in required_v12_epic_clauses):
        raise PublicationError("REMEDIATION_AUTHORITY_DRIFT", "v12 epic checkpoint clause missing")
    if any(clause not in v12_architecture for clause in required_v12_architecture_clauses):
        raise PublicationError("REMEDIATION_AUTHORITY_DRIFT", "v12 architecture checkpoint clause missing")
    if (
        CURRENT_CANDIDATE_PATH.rsplit("/", 1)[-1] not in v13_architecture
        or CURRENT_CANDIDATE_DIGEST not in v13_architecture
    ):
        raise PublicationError("CURRENT_SIDECAR_HEAD_DRIFT", "V13 sidecar head missing")
    for source in (v14_epic, v14_architecture):
        if EPIC_AUTHORITY not in source or ARCHITECTURE_AUTHORITY not in source:
            raise PublicationError("AUTHORITY_IDENTITY_DRIFT", "V14 cross-pointer missing")
        if "ACTIVE" not in source:
            raise PublicationError("IMPLEMENTATION_HOLD_DRIFT", "V14 ACTIVE hold missing")
    required_v14_epic_clauses = (
        "### Story 16.1:",
        "### Story 16.2:",
        "### Story 16.3:",
        "DC-9",
        "DC-10",
        "DC-11",
        "38 nodes and 61 edges",
    )
    required_v14_architecture_clauses = (
        "38 nodes and 61",
        "E6-CURRENT-PROOF -> E6-CURRENT-CANDIDATE",
        "global execution predicate",
        CURRENT_CANDIDATE_DIGEST,
    )
    if any(clause not in v14_epic for clause in required_v14_epic_clauses):
        raise PublicationError("V14_EPIC_AUTHORITY_DRIFT", "required V14 clause missing")
    if any(clause not in v14_architecture for clause in required_v14_architecture_clauses):
        raise PublicationError("V14_ARCHITECTURE_AUTHORITY_DRIFT", "required V14 clause missing")
    return v10_epic, v11_epic, v12_epic, v14_epic


def extract_story_section(v9_block: str, story_id: str) -> str:
    """Extract one complete v9 story definition."""

    match = re.search(rf"^### Story {re.escape(story_id)}:.*$", v9_block, re.MULTILINE)
    if match is None:
        raise PublicationError("STORY_CONTRACT_MISSING", story_id)
    next_heading = re.search(r"^##{1,3} ", v9_block[match.end() :], re.MULTILINE)
    end = match.end() + next_heading.start() if next_heading else len(v9_block)
    return v9_block[match.start() : end].rstrip()


def field(section: str, name: str) -> str:
    """Extract and normalize one bold story field."""

    match = re.search(
        rf"\*\*{re.escape(name)}:\*\*(.*?)(?=\n\*\*|\n###|\Z)",
        section,
        re.DOTALL,
    )
    if match is None:
        raise PublicationError("STORY_CONTRACT_FIELD_MISSING", name)
    return " ".join(match.group(1).split())


def scenario_command(row: str) -> str:
    """Extract the exact command token from an acceptance table row."""

    tokens = re.findall(r"`([^`\n]+)`", row)
    prefixes = ("python3 ", "dotnet ", "uv ", "bash ", "git ")
    commands = [token for token in tokens if token.startswith(prefixes)]
    if len(commands) != 1:
        raise PublicationError("ATOMIC_COMMAND_INVALID", row[:160])
    return commands[0]


def parse_scenarios(section: str) -> list[dict[str, Any]]:
    """Parse exact acceptance IDs, commands, and contracts."""

    scenarios: list[dict[str, Any]] = []
    for match in re.finditer(r"^\| `(AC-[^`]+)` \|(?P<rest>.*)\|$", section, re.MULTILINE):
        row = match.group(0)
        command = scenario_command(row)
        direct_pytest = "pytest" in command
        scenarios.append(
            {
                "id": match.group(1),
                "command": command,
                "contract": " ".join(row.split()),
                "resultSemantics": {
                    "expected": "PASS",
                    "passExitCodes": [0],
                    "failExitCodes": [1, 5] if direct_pytest else [1],
                    "blockedExitCodes": [2, 3, 4] if direct_pytest else [2],
                    "notApplicableAllowed": match.group(1) == "AC-10.3-01",
                },
            }
        )
    if not scenarios:
        raise PublicationError("STORY_SCENARIOS_MISSING", section.splitlines()[0])
    ids = [scenario["id"] for scenario in scenarios]
    if len(ids) != len(set(ids)):
        raise PublicationError("STORY_SCENARIO_DUPLICATE", section.splitlines()[0])
    return scenarios


def extract_amendment(v10_block: str, heading: str, next_heading: str) -> str:
    """Extract one v10 amendment section."""

    start = v10_block.find(heading)
    end = v10_block.find(next_heading, start + len(heading))
    if start < 0 or end <= start:
        raise PublicationError("AUTHORITY_AMENDMENT_MISSING", heading)
    return v10_block[start:end].rstrip()


def parse_v14_proposal_inventories(proposal: str) -> dict[str, tuple[str, str, tuple[str, ...]]]:
    """Parse and independently hash the three approved Story 16 entry inventories."""

    inventories: dict[str, tuple[str, str, tuple[str, ...]]] = {}
    for story_id in ("16.1", "16.2", "16.3"):
        story_match = re.search(
            rf"^#### Story {re.escape(story_id)}:.*?(?=^#### Story |\Z)",
            proposal,
            re.MULTILINE | re.DOTALL,
        )
        if story_match is None:
            raise PublicationError("V14_PROPOSAL_INVENTORY_MISSING", story_id)
        inventory_match = re.search(
            r"\*\*Frozen inventory:\*\* `([^`]+)`, in order:\s*\n\n"
            r"```text\n(?P<rows>.*?)\n```\s*\n\nSHA-256: `([0-9a-f]{64})`\.",
            story_match.group(0),
            re.DOTALL,
        )
        if inventory_match is None:
            raise PublicationError("V14_PROPOSAL_INVENTORY_MISSING", story_id)
        rows = tuple(inventory_match.group("rows").splitlines())
        if not rows or any(not row or row != unicodedata.normalize("NFC", row) for row in rows):
            raise PublicationError("V14_PROPOSAL_INVENTORY_INVALID", story_id)
        if len(rows) != len(set(rows)):
            raise PublicationError("V14_PROPOSAL_INVENTORY_INVALID", f"{story_id}: duplicate rows")
        observed_digest = inventory_digest(list(rows))
        declared_digest = inventory_match.group(3)
        if observed_digest != declared_digest:
            raise PublicationError(
                "V14_PROPOSAL_INVENTORY_DIGEST_DRIFT",
                f"{story_id}: declared {declared_digest}; observed {observed_digest}",
            )
        inventories[story_id] = (inventory_match.group(1), observed_digest, rows)
    return inventories


def validate_v14_story_commands(
    story_id: str,
    scenarios: list[dict[str, Any]],
    final_paths: list[str],
) -> None:
    """Require Story 16 scenario commands to preserve literal script, ID, and output bindings."""

    verifier_path = f"_bmad/scripts/verify_story_{story_id.replace('.', '_')}.py"
    for scenario in scenarios[:5]:
        try:
            tokens = shlex.split(scenario["command"])
        except ValueError as error:
            raise PublicationError("V14_STORY_COMMAND_DRIFT", f"{story_id}: {error}") from error
        expected = [
            "python3",
            verifier_path,
            "--repository",
            ".",
            "--scenario",
            scenario["id"],
            "--candidate",
            "HEAD",
            "--output",
            f"artifacts/v14/{story_id}/{scenario['id']}.json",
        ]
        if tokens != expected:
            raise PublicationError("V14_STORY_COMMAND_DRIFT", f"{scenario['id']}: exact command")

    final = scenarios[5]
    try:
        tokens = shlex.split(final["command"])
    except ValueError as error:
        raise PublicationError("V14_STORY_COMMAND_DRIFT", f"{story_id}: {error}") from error
    expected_contract = f"_bmad-output/planning-artifacts/v9/story-contracts/{story_id}.json"
    expected = [
        "python3",
        "_bmad/scripts/generate_story_record.py",
        "--repository",
        ".",
        "--contract",
        expected_contract,
        "--format",
        "bundle",
        "--output-json",
        f"docs/release-evidence/story-{story_id}-final-record-v2.json",
        "--output-markdown",
        f"docs/release-evidence/story-{story_id}-final-record-v2.md",
    ]
    if tokens != expected or sorted((tokens[-3], tokens[-1])) != final_paths:
        raise PublicationError("V14_STORY_COMMAND_DRIFT", f"{final['id']}: exact command")


def parse_contracts(
    epics: str,
    candidate: str,
    v10_block: str,
    v14_block: str,
    proposal: str,
) -> dict[str, dict[str, Any]]:
    """Derive the 27 legacy and three V14 effective story contracts."""

    v9_block = marker_block(
        epics.encode("utf-8"),
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:END",
    ).decode("utf-8")
    story_matches = re.findall(r"^### Story (\d+\.\d+):", v9_block, re.MULTILINE)
    if tuple(story_matches) != LEGACY_STORY_IDS:
        raise PublicationError("STORY_CONTRACT_SET_DRIFT", repr(story_matches))

    ten_three_amendment = extract_amendment(
        v10_block,
        "### Story 10.3 V10 Amendment:",
        "### Story 10.4 V10 Amendment:",
    )
    ten_four_amendment = extract_amendment(
        v10_block,
        "### Story 10.4 V10 Amendment:",
        "### Publication, Hold, And Action State",
    )
    amendment_contracts = {
        match.group(1): " ".join(match.group(2).split())
        for match in re.finditer(r"^\| `(AC-10\.3-\d{2})` \| (.*?) \|$", ten_three_amendment, re.MULTILINE)
    }
    if len(amendment_contracts) != 8:
        raise PublicationError("STORY_10_3_AMENDMENT_DRIFT", str(len(amendment_contracts)))

    contracts: dict[str, dict[str, Any]] = {}
    for story_id in LEGACY_STORY_IDS:
        section = extract_story_section(v9_block, story_id)
        heading = section.splitlines()[0]
        title = heading.split(":", 1)[1].strip()
        scenarios = parse_scenarios(section)
        effective_section = section
        if story_id == "10.3":
            for scenario in scenarios:
                scenario["contract"] = amendment_contracts[scenario["id"]]
            effective_section = f"{section}\n\n{ten_three_amendment}"
        elif story_id == "10.4":
            for scenario in scenarios:
                if scenario["id"] == "AC-10.4-08":
                    scenario["contract"] = scenario["contract"].replace(
                        "summary `8/8/0/0/0/0`",
                        "summary `9/9/0/0/0/0`",
                    )
            scenarios.append(
                {
                    "id": "AC-10.4-09",
                    "command": "python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check",
                    "contract": " ".join(ten_four_amendment.split()),
                    "resultSemantics": {
                        "expected": "PASS",
                        "passExitCodes": [0],
                        "failExitCodes": [1],
                        "blockedExitCodes": [2],
                        "notApplicableAllowed": False,
                    },
                }
            )
            effective_section = f"{section}\n\n{ten_four_amendment}"
        expected_prefix = f"AC-{story_id}-"
        if any(not scenario["id"].startswith(expected_prefix) for scenario in scenarios):
            raise PublicationError("STORY_SCENARIO_ID_DRIFT", story_id)

        predecessor_text = field(section, "Exact predecessors")
        predecessors = sorted(re.findall(r"`(\d+\.\d+)`", predecessor_text))
        rollback = field(section, "Rollback boundary")
        candidate_binding = field(section, "Candidate binding")
        bounded_outcome = field(section, "Bounded outcome")
        inventory_match = re.search(
            r"\*\*Frozen (?:entry )?inventory:\*\*.*?`([^`]+)`.*?SHA-256\s+`([0-9a-f]{64})`",
            section,
            re.DOTALL,
        )
        final_record = field(section, "Generated final record")
        final_paths = sorted(
            set(
                re.findall(
                    r"(?:docs|_bmad-output)/[A-Za-z0-9_./-]*story-[0-9.]+-final-record-v2\.(?:json|md)",
                    final_record,
                )
            )
        )
        contracts[story_id] = {
            "schemaVersion": V9_STORY_CONTRACT_SCHEMA_IDENTITY,
            "storyId": story_id,
            "authority": {
                **BASE_AUTHORITIES,
                "planningCandidate": candidate,
                "candidateBinding": candidate_binding,
                "sectionSha256": sha256(effective_section.encode("utf-8")),
            },
            "predecessors": predecessors,
            "outcome": {"title": title, "bounded": bounded_outcome},
            "rollback": {"boundary": rollback},
            "inventory": (
                {"id": inventory_match.group(1), "sha256": inventory_match.group(2)}
                if inventory_match
                else None
            ),
            "scenarios": scenarios,
            "finalRecord": {
                "paths": final_paths,
                "summary": {
                    "required": len(scenarios),
                    "passed": len(scenarios),
                    "failed": 0,
                    "blocked": 0,
                    "skipped": 0,
                    "notRun": 0,
                },
            },
        }
    for story_id, predecessors in V14_PREDECESSOR_AMENDMENTS.items():
        contracts[story_id]["predecessors"] = sorted(
            set(contracts[story_id]["predecessors"] + list(predecessors))
        )
        contracts[story_id]["schemaVersion"] = V14_STORY_CONTRACT_SCHEMA_IDENTITY

    v14_story_matches = re.findall(r"^### Story (16\.[1-3]):", v14_block, re.MULTILINE)
    if tuple(v14_story_matches) != EXPECTED_STORY_IDS[-3:]:
        raise PublicationError("V14_STORY_CONTRACT_SET_DRIFT", repr(v14_story_matches))
    expected_inventories = parse_v14_proposal_inventories(proposal)
    for story_id in EXPECTED_STORY_IDS[-3:]:
        section = extract_story_section(v14_block, story_id)
        scenarios = parse_scenarios(section)
        if len(scenarios) != 6 or [row["id"] for row in scenarios] != [
            f"AC-{story_id}-{ordinal:02d}" for ordinal in range(1, 7)
        ]:
            raise PublicationError("V14_STORY_SCENARIO_DRIFT", story_id)
        predecessor_text = field(section, "Exact predecessors")
        predecessors = sorted(set(re.findall(r"`((?:\d+\.\d+)|IR-0)`", predecessor_text)))
        inventory_match = re.search(
            r"\*\*Frozen inventory:\*\*\s*`([^`]+)`; SHA-256\s*`([0-9a-f]{64})`",
            section,
            re.DOTALL,
        )
        if inventory_match is None or inventory_match.groups() != expected_inventories[story_id][:2]:
            raise PublicationError("V14_STORY_INVENTORY_DRIFT", story_id)
        final_record = field(section, "Generated final record")
        final_paths = sorted(
            set(
                re.findall(
                    rf"docs/release-evidence/story-{re.escape(story_id)}-final-record-v2\.(?:json|md)",
                    final_record,
                )
            )
        )
        if len(final_paths) != 2:
            raise PublicationError("V14_FINAL_RECORD_DRIFT", story_id)
        validate_v14_story_commands(story_id, scenarios, final_paths)
        contracts[story_id] = {
            "schemaVersion": V14_STORY_CONTRACT_SCHEMA_IDENTITY,
            "storyId": story_id,
            "authority": {
                **AUTHORITIES,
                "planningCandidate": candidate,
                "candidateBinding": field(section, "Candidate binding"),
                "sectionSha256": sha256(section.encode("utf-8")),
            },
            "predecessors": predecessors,
            "outcome": {
                "title": section.splitlines()[0].split(":", 1)[1].strip(),
                "bounded": field(section, "Bounded outcome"),
            },
            "rollback": {"boundary": field(section, "Rollback boundary")},
            "inventory": {"id": inventory_match.group(1), "sha256": inventory_match.group(2)},
            "scenarios": scenarios,
            "finalRecord": {
                "paths": final_paths,
                "summary": {"required": 6, "passed": 6, "failed": 0, "blocked": 0, "skipped": 0, "notRun": 0},
            },
        }
    if tuple(contracts) != EXPECTED_STORY_IDS:
        raise PublicationError("STORY_CONTRACT_SET_DRIFT", repr(tuple(contracts)))
    return contracts


def v11_story_slice_amendment(v11_epic: str) -> str:
    """Extract the exact canonical v11 Story 7.1 checkpoint amendment."""

    return extract_amendment(
        v11_epic,
        "### Story 7.1 V11 Schema-Checkpoint Amendment: Authorize A Non-Story Slice",
        "### V11 Publication, Hold, And Retrospective State",
    )


def render_story_slice(
    candidate: str,
    base_contract: bytes,
    v11_epic: str,
) -> bytes:
    """Render the closed Story 7.1 schema-checkpoint authority sidecar."""

    amendment = v11_story_slice_amendment(v11_epic)
    return json_bytes(
        {
            "schemaVersion": "hexalith.conversations.story-slice-authority.v1",
            "sliceId": SLICE_ID,
            "storyId": "7.1",
            "authority": {
                **V11_AUTHORITIES,
                "planningCandidate": candidate,
                "authorityBundlePath": BUNDLE_PATH,
            },
            "baseStoryContract": {
                "path": "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
                "sha256": sha256(base_contract),
                **BASE_AUTHORITIES,
            },
            "amendmentSectionSha256": sha256(amendment.encode("utf-8")),
            "predecessors": list(SLICE_PREDECESSORS),
            "holdRequirement": {
                "effectiveState": "LIFTED",
                "recordPath": "_bmad-output/planning-artifacts/implementation-hold-v1.json",
            },
            "writablePaths": list(SLICE_WRITABLE_PATHS),
            "readOnlyInputs": list(SLICE_READ_ONLY_INPUTS),
            "prohibitedPaths": list(SLICE_PROHIBITED_PATHS),
            "acceptance": {
                "scenarioId": "AC-7.1-01",
                "command": SLICE_COMMAND,
                "result": "PASS",
                "passExitCodes": [0],
                "failExitCodes": [1, 5],
                "blockedExitCodes": [2, 3, 4],
            },
            "completionEffect": {
                "storyDoneAllowed": False,
                "finalRecordProduced": False,
                "successorUnlocked": False,
            },
            "rollback": {"boundary": SLICE_ROLLBACK},
        }
    )


def validate_story_slice(
    slice_authority: dict[str, Any],
    base_contract: bytes,
    v11_epic: str,
    candidate: str,
) -> None:
    """Recompute the sidecar bindings and reject semantic drift."""

    expected = json.loads(render_story_slice(candidate, base_contract, v11_epic))
    if slice_authority != expected:
        raise PublicationError("STORY_SLICE_AUTHORITY_DRIFT", "closed checkpoint authority mismatch")


def render_remediation_authority(root: Path, candidate: str) -> bytes:
    """Render the closed V12 pre-IR-0 remediation sidecar."""

    actions = [
        {
            "id": "A1",
            "owner": "Dev workflow / Release owner",
            "scope": "Exact Story 6.7 and Story 6.2 done-tree reconstruction plus independent acceptance-evidence supersession decision.",
            "executionAuthority": "E6-REMEDIATION",
            "checkpointOwned": True,
            "status": "open",
        },
        {
            "id": "A2",
            "owner": "Dev workflow",
            "scope": "Promotion and evidence-boundary gates on exactly twelve active lifecycle routes.",
            "executionAuthority": "E6-REMEDIATION",
            "checkpointOwned": True,
            "status": "open",
        },
        {
            "id": "A3",
            "owner": "Architecture / Quality",
            "scope": "Fail-closed historical signatures, context identity, pinned planning verification, and automatic preflight.",
            "executionAuthority": "E6-REMEDIATION",
            "checkpointOwned": True,
            "status": "open",
        },
        {
            "id": "A4",
            "owner": "Architect / Runtime owner",
            "scope": "Durable event-fed tenant access, freshness and gap detection, restart, and multi-replica convergence.",
            "executionAuthority": "separately approved successor",
            "checkpointOwned": False,
            "status": "open",
        },
        {
            "id": "A5",
            "owner": "Projection owner",
            "scope": "Deterministic event-derived replay timestamps and trustworthy missing-index semantics.",
            "executionAuthority": "separately approved successor",
            "checkpointOwned": False,
            "status": "open",
        },
        {
            "id": "A6",
            "owner": "Test / AppHost owner",
            "scope": "Endpoint and Dapr port diagnostics plus live terminal reconciliation route coverage.",
            "executionAuthority": "separately approved successor",
            "checkpointOwned": False,
            "status": "open",
        },
    ]
    return json_bytes(
        {
            "schemaVersion": "hexalith.conversations.v12-pre-ir0-remediation-authority.v1",
            "checkpointId": "E6-REMEDIATION",
            "authority": {
                **V12_AUTHORITIES,
                "planningCandidate": candidate,
                "authorityBundlePath": BUNDLE_PATH,
                "implementationHold": "ACTIVE",
            },
            "predecessors": ["PC-PUBLICATION"],
            "successor": "IR-0",
            "actionInventory": actions,
            "activeRoutePaths": list(MECHANICAL_PATHS),
            "rootGitlinkPaths": list(ROOT_GITLINK_PATHS),
            "supersessionContract": {
                "path": SUPERSESSION_CONTRACT_PATH,
                "sha256": sha256(candidate_blob(root, candidate, SUPERSESSION_CONTRACT_PATH)),
            },
            "resultSemantics": {
                "states": ["PASS", "FAIL", "BLOCKED", "not-applicable"],
                "ledgerRequired": True,
                "skipsAllowed": False,
            },
            "prohibitions": [
                "rewrite completed Story 6.7 or Story 6.2 records",
                "substitute current bytes for historical evidence",
                "traverse nested submodules",
                "modify product code, packages, submodules, or gitlinks",
                "implement or start successors",
                "create implementation-hold-v1.json",
                "claim release approval",
            ],
            "completionEffect": {
                "ir0RerunAllowed": True,
                "holdLifted": False,
                "successorStarted": False,
                "releaseAuthorized": False,
            },
            "assertions": [
                "A1-A3 pass before IR-0.",
                "A4-A6 remain open under separately approved successor authority.",
                "The global implementation hold remains ACTIVE after a READY IR-0.",
            ],
        }
    )


def validate_remediation_authority(root: Path, candidate: str, authority: dict[str, Any]) -> None:
    """Recompute the V12 checkpoint sidecar and reject any closed-field drift."""

    expected = json.loads(render_remediation_authority(root, candidate))
    if authority != expected:
        raise PublicationError("REMEDIATION_AUTHORITY_DRIFT", "closed checkpoint authority mismatch")


def expected_graph_nodes(
    contracts: dict[str, dict[str, Any]],
    slice_authority: dict[str, Any],
) -> dict[str, dict[str, Any]]:
    """Build the exact current 38-node execution graph authority."""

    nodes: dict[str, dict[str, Any]] = {
        "6.2": {"id": "6.2", "kind": "historical-story", "predecessors": []},
        "PC-PUBLICATION": {"id": "PC-PUBLICATION", "kind": "publication", "predecessors": []},
        "E6-REMEDIATION": {
            "id": "E6-REMEDIATION",
            "kind": "checkpoint",
            "predecessors": ["PC-PUBLICATION"],
        },
        "E6-CURRENT-PROOF": {
            "id": "E6-CURRENT-PROOF",
            "kind": "checkpoint",
            "predecessors": ["E6-REMEDIATION"],
        },
        "E6-CURRENT-CANDIDATE": {
            "id": "E6-CURRENT-CANDIDATE",
            "kind": "checkpoint",
            "predecessors": ["E6-CURRENT-PROOF", "E6-REMEDIATION"],
        },
        "IR-0": {"id": "IR-0", "kind": "gate", "predecessors": ["E6-REMEDIATION"]},
        SLICE_ID: {
            "id": SLICE_ID,
            "kind": "checkpoint",
            "predecessors": list(slice_authority["predecessors"]),
        },
        "RG-15": {"id": "RG-15", "kind": "release-gate", "predecessors": ["15.2"]},
    }
    for story_id, contract in contracts.items():
        predecessors = list(contract["predecessors"])
        if story_id in ("7.1", "12.1"):
            predecessors.append("IR-0")
        if story_id == "7.1":
            predecessors.append(SLICE_ID)
        nodes[story_id] = {
            "id": story_id,
            "kind": "story",
            "predecessors": sorted(set(predecessors)),
        }
    return nodes


def validate_slice_graph_parity(
    graph: dict[str, Any],
    contracts: dict[str, dict[str, Any]],
    slice_authority: dict[str, Any],
) -> None:
    """Require exact sidecar, base-story, and graph checkpoint parity."""

    expected_nodes = expected_graph_nodes(contracts, slice_authority)
    expected_node_rows = [expected_nodes[node_id] for node_id in sorted(expected_nodes)]
    if graph.get("nodes") != expected_node_rows or len(expected_node_rows) != 38:
        raise PublicationError("CHECKPOINT_GRAPH_DRIFT", "exact 38-node graph inventory mismatch")
    expected_edges = {
        (predecessor, node["id"])
        for node in expected_node_rows
        for predecessor in node.get("predecessors", [])
    }
    actual_edges = {(edge.get("from"), edge.get("to")) for edge in graph.get("edges", [])}
    if len(actual_edges) != len(graph.get("edges", [])) or actual_edges != expected_edges:
        raise PublicationError("CHECKPOINT_GRAPH_DRIFT", "graph edge set mismatch")
    if len(actual_edges) != 61:
        raise PublicationError("CHECKPOINT_GRAPH_DRIFT", f"expected 61 edges; observed {len(actual_edges)}")


def validate_route_topology(root: Path, candidate: str) -> None:
    """Validate current route inventory, tree parity, and forwarding aliases."""

    if inventory_digest(list(MECHANICAL_PATHS)) != "966745d95e24aeb95af58a2bbfab11de7b08b8ab9f2447aa6c90a99c444292d4":
        raise PublicationError("WORKFLOW_INVENTORY_DRIFT", "mechanical inventory digest")
    for logical in MECHANICAL_LOGICAL_BODIES:
        agents = candidate_blob(root, candidate, f".agents/skills/{logical}")
        claude = candidate_blob(root, candidate, f".claude/skills/{logical}")
        if agents != claude:
            raise PublicationError("EVIDENCE_WORKFLOW_PARITY_DRIFT", logical)
        text = agents.decode("utf-8")
        marker = "V12 lifecycle evidence gates"
        lifecycle = MECHANICAL_LIFECYCLE_TOKENS[logical]
        required = (
            "verify_submodule_promotion.py",
            "verify_evidence_boundary.py",
            "PASS",
            "FAIL",
            "BLOCKED",
            "not-applicable",
        )
        if text.count(marker) != 1 or lifecycle not in text or text.index(marker) > text.index(lifecycle):
            raise PublicationError("EVIDENCE_GATE_DISPLACED", logical)
        if any(token not in text[text.index(marker) :] for token in required):
            raise PublicationError("EVIDENCE_GATE_NOT_USED", logical)
    aliases = {
        "bmad-dev-auto": "bmad-build-auto",
        "bmad-quick-dev": "bmad-build",
    }
    for tree in ("agents", "claude"):
        for alias, target in aliases.items():
            path = f".{tree}/skills/{alias}/SKILL.md"
            content = candidate_blob(root, candidate, path).decode("utf-8")
            if content.count(f"invoke `{target}` exactly once") != 2:
                raise PublicationError("EVIDENCE_ALIAS_ROUTE_INVALID", path)
            if "step-04-review" in content or "verify_evidence_boundary.py" in content:
                raise PublicationError("EVIDENCE_ALIAS_ROUTE_INVALID", f"forked gate in {path}")
        if candidate_blob(root, candidate, f".{tree}/skills/bmad-dev-auto/SKILL.md") != candidate_blob(
            root, candidate, f".{('claude' if tree == 'agents' else 'agents')}/skills/bmad-dev-auto/SKILL.md"
        ):
            raise PublicationError("EVIDENCE_WORKFLOW_PARITY_DRIFT", "bmad-dev-auto alias")
    if candidate_blob(root, candidate, ".agents/skills/bmad-quick-dev/SKILL.md") != candidate_blob(
        root, candidate, ".claude/skills/bmad-quick-dev/SKILL.md"
    ):
        raise PublicationError("EVIDENCE_WORKFLOW_PARITY_DRIFT", "bmad-quick-dev alias")


def render_resolved_customization(root: Path, candidate: str) -> dict[str, bytes]:
    """Resolve and validate team customization against current defaults."""

    outputs: dict[str, bytes] = {}
    runbook_reference = "file:{project-root}/docs/runbooks/evidence-boundary-validation.md"
    try:
        runbook = (root / RUNBOOK_PATH).read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        raise PublicationError("EVIDENCE_GUIDANCE_DRIFT", f"{RUNBOOK_PATH}: {error}") from error
    runbook_flat = " ".join(runbook.split())
    missing_semantics = [semantic for semantic in GUIDANCE_SEMANTICS if semantic not in runbook_flat]
    if missing_semantics:
        raise PublicationError("EVIDENCE_GUIDANCE_DRIFT", repr(missing_semantics))
    for skill in ("bmad-build", "bmad-build-auto", "bmad-review"):
        agents_default = f".agents/skills/{skill}/customize.toml"
        claude_default = f".claude/skills/{skill}/customize.toml"
        user_layer = root / f"_bmad/custom/{skill}.user.toml"
        if user_layer.exists():
            raise PublicationError("EVIDENCE_CUSTOMIZATION_RESOLUTION_FAILED", f"unbound user layer: {user_layer}")
        if candidate_blob(root, candidate, agents_default) != candidate_blob(root, candidate, claude_default):
            raise PublicationError("EVIDENCE_WORKFLOW_PARITY_DRIFT", f"{skill} customization default")
        try:
            resolved = load_customization(root, root / f".agents/skills/{skill}")
        except ConfigError as error:
            raise PublicationError("EVIDENCE_CUSTOMIZATION_RESOLUTION_FAILED", str(error)) from error
        workflow = resolved.get("workflow")
        if not isinstance(workflow, dict):
            raise PublicationError("EVIDENCE_CUSTOMIZATION_RESOLUTION_FAILED", skill)
        if skill in ("bmad-build", "bmad-build-auto"):
            persistent = workflow.get("persistent_facts", [])
            activation = " ".join(workflow.get("activation_steps_append", []))
            if runbook_reference not in persistent:
                raise PublicationError("EVIDENCE_GUIDANCE_NOT_USED", skill)
            required = ("PASS", "FAIL", "BLOCKED", "not-applicable")
            if any(value not in activation for value in required):
                raise PublicationError("EVIDENCE_GUIDANCE_DRIFT", skill)
        else:
            if runbook_reference not in workflow.get("review_guidance", []):
                raise PublicationError("EVIDENCE_GUIDANCE_NOT_USED", skill)
            lenses = workflow.get("lenses", [])
            evidence_lenses = [lens for lens in lenses if lens.get("code") == "evidence-boundary"]
            if len(evidence_lenses) != 1 or "exact changed-path" not in evidence_lenses[0].get("instruction", ""):
                raise PublicationError("EVIDENCE_GUIDANCE_DRIFT", skill)
        payload = {
            "schemaVersion": "hexalith.conversations.resolved-customization.v1",
            "planningCandidate": candidate,
            "authorities": AUTHORITIES,
            "skill": skill,
            "defaultPath": agents_default,
            "defaultSha256": sha256(candidate_blob(root, candidate, agents_default)),
            "teamPath": f"_bmad/custom/{skill}.toml",
            "teamSha256": sha256(candidate_blob(root, candidate, f"_bmad/custom/{skill}.toml")),
            "resolved": resolved,
        }
        outputs[f"_bmad-output/planning-artifacts/v9/resolved-customization/{skill}.json"] = json_bytes(payload)
    return outputs


def render_inventory(
    root: Path,
    candidate: str,
    inventory_id: str,
    paths: tuple[str, ...],
    *,
    mechanical: bool = False,
) -> bytes:
    """Render one path-and-blob-bound planning inventory."""

    expected_by_id = {
        "V9-EVIDENCE-WORKFLOWS-v2": MECHANICAL_PATHS,
        "V12-ACTIVE-LIFECYCLE-ROUTES-v1": MECHANICAL_PATHS,
        "V9-EVIDENCE-GUIDANCE-v2": GUIDANCE_PATHS,
        "V9-EVIDENCE-READERS-v1": READER_PATHS,
    }
    if len(paths) != len(set(paths)) or tuple(paths) != expected_by_id.get(inventory_id):
        raise PublicationError("INVENTORY_ORDER_DRIFT", inventory_id)
    rows = []
    for ordinal, relative_path in enumerate(paths, start=1):
        row: dict[str, Any] = {
            "ordinal": ordinal,
            "path": relative_path,
            "sha256": sha256(candidate_blob(root, candidate, relative_path)),
        }
        if mechanical:
            row["logicalBody"] = relative_path.split("/skills/", 1)[1]
            row["tree"] = "agents" if relative_path.startswith(".agents/") else "claude"
        else:
            row["tree"] = "project"
        rows.append(row)
    return json_bytes(
        {
            "schemaVersion": "hexalith.conversations.v9-inventory.v1",
            "inventoryId": inventory_id,
            "planningCandidate": candidate,
            "authorities": AUTHORITIES,
            "inventoryDigest": inventory_digest(list(paths)),
            "rows": rows,
        }
    )


def render_graph(
    candidate: str,
    contracts: dict[str, dict[str, Any]],
    slice_authority: dict[str, Any],
) -> bytes:
    """Render and validate the current acyclic story/gate graph."""

    nodes = expected_graph_nodes(contracts, slice_authority)
    for node in nodes.values():
        for predecessor in node["predecessors"]:
            if predecessor not in nodes:
                raise PublicationError("EXECUTION_GRAPH_GAP", f"{predecessor}->{node['id']}")

    visiting: set[str] = set()
    visited: set[str] = set()

    def visit(node_id: str) -> None:
        if node_id in visiting:
            raise PublicationError("EXECUTION_GRAPH_CYCLE", node_id)
        if node_id in visited:
            return
        visiting.add(node_id)
        for predecessor in nodes[node_id]["predecessors"]:
            visit(predecessor)
        visiting.remove(node_id)
        visited.add(node_id)

    for node_id in nodes:
        visit(node_id)
    for story_id in EXPECTED_STORY_IDS:
        pending = list(nodes[story_id]["predecessors"])
        ancestors: set[str] = set()
        while pending:
            predecessor = pending.pop()
            if predecessor in ancestors:
                continue
            ancestors.add(predecessor)
            pending.extend(nodes[predecessor]["predecessors"])
        if "IR-0" not in ancestors:
            raise PublicationError("EXECUTION_GRAPH_GAP", f"IR-0->{story_id}")
    edges = sorted(
        (
            {"from": predecessor, "to": node["id"]}
            for node in nodes.values()
            for predecessor in node["predecessors"]
        ),
        key=lambda edge: (edge["from"], edge["to"]),
    )
    graph = {
        "schemaVersion": "hexalith.conversations.v9-execution-graph.v1",
        "planningCandidate": candidate,
        "authorities": AUTHORITIES,
        "implementationHold": "ACTIVE",
        "nodes": [nodes[node_id] for node_id in sorted(nodes)],
        "edges": edges,
    }
    validate_slice_graph_parity(graph, contracts, slice_authority)
    return json_bytes(graph)


def render_supersession(candidate: str, epics: str) -> bytes:
    """Render all dispositions and the complete canonical 156-row v8 ledger."""

    dispositions = (
        ("6.3", "in-progress", 14, ("14.1", "14.2", "14.3"), "partial work is unaccepted input"),
        ("6.4", "backlog", 8, ("8.1", "8.2"), "superseded definition only"),
        ("6.5", "backlog", 11, ("11.1", "11.2", "11.3"), "superseded definition only"),
        ("6.6", "backlog", 15, ("15.1", "15.2"), "release decision remains non-story Gate RG-15"),
        ("6.8", "in-progress", 7, ("7.1", "7.2", "7.3", "7.4"), "partial work is unaccepted input"),
        ("6.9", "backlog", 9, ("9.1", "9.2"), "superseded definition only"),
        ("6.10", "backlog", 10, ("10.1", "10.2", "10.3", "10.4"), "superseded by corrected Epic 10"),
        ("6.11", "backlog", 12, ("12.1", "12.2", "12.3", "12.4"), "superseded definition only"),
        ("6.12", "ready-for-dev", 13, ("13.1", "13.2", "13.3"), "prepared story remains provenance"),
    )
    v9_block = marker_block(
        epics.encode("utf-8"),
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:END",
    ).decode("utf-8")
    ledger_start = v9_block.find("### Effective v8 acceptance-criterion mappings")
    supporting_start = v9_block.find(
        "### Checkpoint, prohibition, dependency, evidence, rollback, and gate mappings",
        ledger_start,
    )
    ledger_end = v9_block.find("## Publication State And Hold", supporting_start)
    if ledger_start < 0 or supporting_start <= ledger_start or ledger_end <= supporting_start:
        raise PublicationError("SUPERSESSION_OBLIGATION_DRIFT", "canonical ledger boundaries")
    acceptance_matches = re.findall(
        r"^\| `(V8-[^`]+)` \| `([^`]+)` \|$",
        v9_block[ledger_start:supporting_start],
        re.MULTILINE,
    )
    supporting_matches = re.findall(
        r"^\| `(V8-[^`]+)` \| `([^`]+)` \|$",
        v9_block[supporting_start:ledger_end],
        re.MULTILINE,
    )
    matches = acceptance_matches + supporting_matches
    if len(acceptance_matches) != 66 or len(matches) != 156:
        raise PublicationError(
            "SUPERSESSION_OBLIGATION_DRIFT",
            f"acceptance={len(acceptance_matches)} total={len(matches)}",
        )
    source_ids = [source_id for source_id, _ in matches]
    if len(source_ids) != len(set(source_ids)):
        raise PublicationError("SUPERSESSION_OBLIGATION_DRIFT", "duplicate obligation identity")
    digest_input = "".join(f"{source_id}|{binding}\n" for source_id, binding in matches).encode("utf-8")
    ledger_digest = sha256(digest_input)
    if ledger_digest != OBLIGATION_LEDGER_DIGEST:
        raise PublicationError("SUPERSESSION_OBLIGATION_DRIFT", ledger_digest)
    obligations = []
    for ordinal, (source_id, canonical_binding) in enumerate(matches, start=1):
        effective_bindings = canonical_binding.split(",")
        if source_id == "V8-6.10-AC9":
            effective_bindings.append("AC-10.4-09")
        obligations.append(
            {
                "ordinal": ordinal,
                "table": "acceptance-criteria" if ordinal <= len(acceptance_matches) else "supporting-obligations",
                "sourceId": source_id,
                "canonicalBinding": canonical_binding,
                "effectiveBindings": effective_bindings,
            }
        )
    extraction_start = v9_block.find("### Confirmed Requirement Extraction")
    extraction_end = v9_block.find("### Non-Goals And Active Hold", extraction_start)
    extraction = v9_block[extraction_start:extraction_end]
    required_denominator_clauses = (
        "exactly 20 initiative FRs and 104 `Feature-FR`s, hence 124/124 functional",
        "all 77 `Feature-NFR`s",
        "all 52 UX decisions and all 28 UX acceptance identifiers",
    )
    if extraction_start < 0 or extraction_end <= extraction_start or any(
        clause not in extraction for clause in required_denominator_clauses
    ):
        raise PublicationError("PRESERVATION_DENOMINATOR_DRIFT", "confirmed requirement extraction")
    if len([row for row in dispositions if row[0] == "6.10" and row[2] == 10]) != 1:
        raise PublicationError("SUPERSESSION_STORY_6_10_DRIFT", "expected one Epic 10 mapping")
    if len([row for row in obligations if row["sourceId"].startswith("V8-6.10-AC")]) != 10:
        raise PublicationError("SUPERSESSION_STORY_6_10_DRIFT", "expected ten AC obligations")
    return json_bytes(
        {
            "schemaVersion": "hexalith.conversations.v9-supersession-map.v1",
            "planningCandidate": candidate,
            "authorities": AUTHORITIES,
            "storyDispositions": [
                {
                    "sourceStory": source,
                    "sourceStatus": status,
                    "successorEpic": epic,
                    "successorStories": list(stories),
                    "salvagePolicy": policy,
                }
                for source, status, epic, stories, policy in dispositions
            ],
            "preservationDenominators": {
                "functionalRequirements": {"required": 124, "mapped": 124},
                "nonFunctionalRequirements": {"required": 77, "mapped": 77},
                "uxDecisions": {"required": 52, "mapped": 52},
                "uxAcceptanceCriteria": {"required": 28, "mapped": 28},
            },
            "obligationLedger": {
                "inventoryId": OBLIGATION_LEDGER_ID,
                "sha256": ledger_digest,
                "acceptanceCriteriaRows": len(acceptance_matches),
                "totalRows": len(obligations),
                "rows": obligations,
            },
        }
    )


def validate_current_view(content: str, contracts: dict[str, dict[str, Any]]) -> None:
    """Require exact checkpoint and effective story rows."""

    expected_schema_checkpoint = (
        "| 7.1-SCHEMAS | checkpoint | Closed Story 7.1 schema contracts | "
        "6.2, IR-0 | 1 |"
    )
    expected_remediation_checkpoint = (
        "| E6-REMEDIATION | checkpoint | Complete Epic 6 A1-A3 before independent IR-0 | "
        "PC-PUBLICATION | 3 |"
    )
    expected_current_proof = (
        "| E6-CURRENT-PROOF | checkpoint | Accepted current completion proof | "
        "E6-REMEDIATION | 1 |"
    )
    expected_current_candidate = (
        "| E6-CURRENT-CANDIDATE | checkpoint | Pinned point-in-time candidate authority | "
        "E6-CURRENT-PROOF, E6-REMEDIATION | 1 |"
    )
    expected_seven_one = (
        f"| 7.1 | story | {contracts['7.1']['outcome']['title']} | "
        f"6.2, {SLICE_ID}, IR-0 | {len(contracts['7.1']['scenarios'])} |"
    )
    expected_seven_two = (
        f"| 7.2 | story | {contracts['7.2']['outcome']['title']} | "
        f"7.1 | {len(contracts['7.2']['scenarios'])} |"
    )
    if (
        content.count(expected_schema_checkpoint) != 1
        or content.count(expected_remediation_checkpoint) != 1
        or content.count(expected_current_proof) != 1
        or content.count(expected_current_candidate) != 1
    ):
        raise PublicationError("CURRENT_VIEW_DRIFT", "checkpoint row mismatch")
    seven_one_rows = re.findall(r"^\| 7\.1 \|.*$", content, re.MULTILINE)
    seven_two_rows = re.findall(r"^\| 7\.2 \|.*$", content, re.MULTILINE)
    if seven_one_rows != [expected_seven_one] or seven_two_rows != [expected_seven_two]:
        raise PublicationError("CURRENT_VIEW_DRIFT", "Story 7.1/7.2 row mismatch")
    if len(re.findall(r"^\| (?:[7-9]|1[0-6])\.\d+ \| story \|", content, re.MULTILINE)) != 30:
        raise PublicationError("CURRENT_VIEW_DRIFT", "expected 30 story rows")
    for story_id in ("16.1", "16.2", "16.3"):
        if len(re.findall(rf"^\| {re.escape(story_id)} \| story \|", content, re.MULTILINE)) != 1:
            raise PublicationError("CURRENT_VIEW_DRIFT", f"Story {story_id} row mismatch")


def render_view(candidate: str, contracts: dict[str, dict[str, Any]]) -> bytes:
    """Render the non-amending v2 current execution view."""

    rows = "\n".join(
        f"| {story_id} | story | {contract['outcome']['title']} | "
        f"{', '.join(sorted(set(contract['predecessors'] + (['IR-0'] if story_id in ('7.1', '12.1') else []) + ([SLICE_ID] if story_id == '7.1' else []))))} | "
        f"{len(contract['scenarios'])} |"
        for story_id, contract in contracts.items()
    )
    schema_checkpoint_row = (
        "| 7.1-SCHEMAS | checkpoint | Closed Story 7.1 schema contracts | "
        "6.2, IR-0 | 1 |"
    )
    remediation_checkpoint_row = (
        "| E6-REMEDIATION | checkpoint | Complete Epic 6 A1-A3 before independent IR-0 | "
        "PC-PUBLICATION | 3 |"
    )
    current_proof_row = (
        "| E6-CURRENT-PROOF | checkpoint | Accepted current completion proof | "
        "E6-REMEDIATION | 1 |"
    )
    current_candidate_row = (
        "| E6-CURRENT-CANDIDATE | checkpoint | Pinned point-in-time candidate authority | "
        "E6-CURRENT-PROOF, E6-REMEDIATION | 1 |"
    )
    content = f"""---
artifact: epic-6-current-execution-view-v2
generated: '2026-08-19'
generator_version: '1.0.0'
generation_command: 'python3 _bmad/scripts/publish_v9_planning_authority.py --repository .'
planning_candidate: '{candidate}'
epic_authority: '{EPIC_AUTHORITY}'
architecture_authority: '{ARCHITECTURE_AUTHORITY}'
implementation_hold: 'ACTIVE'
status: 'candidate-bound-planning-publication'
---

# Epic 6 Current Execution View V2

> **PLANNING PUBLICATION ONLY — IMPLEMENTATION HOLD ACTIVE.** This generated
> view projects V14, `E6-REMEDIATION`, the current-proof checkpoints, and the inherited
> non-story `7.1-SCHEMAS` checkpoint. It does not implement a story, run IR-0, lift the
> hold, close Epic 5 action A5, or authorize release.

The canonical epic authority and architecture overlay remain the semantic
sources. This file is regenerated from their committed blobs at `PC` and is
non-amending.

| Execution unit | Kind | Bounded outcome | Effective predecessors | AC count |
| --- | --- | --- | --- | ---: |
{remediation_checkpoint_row}
{current_proof_row}
{current_candidate_row}
{schema_checkpoint_row}
{rows}

## Gate State

- IR-0: not run by this publication.
- Implementation hold: `ACTIVE`.
- `E6-REMEDIATION`: planning-authorized A1-A3 checkpoint; completion evidence is external to this bundle.
- `7.1-SCHEMAS`: planning-only and non-executable while the hold is active.
- Epic 5 action A5: `open` until a compatible Story 10.4 `9/9/0/0/0/0` final record passes.
"""
    validate_current_view(content, contracts)
    return content.encode("utf-8")


def render_ux_map(source: bytes, candidate: str) -> bytes:
    """Rebind the preserved 52/28 UX projection without semantic activation."""

    text = source.decode("utf-8")
    text = re.sub(
        r"^authorityVersion: .*?$",
        "authorityVersion: ux-preservation-planning-2026-08-18-v4",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    text = re.sub(
        r"^(?:planningCandidate|epicAuthority|architectureAuthority):.*\n",
        "",
        text,
        flags=re.MULTILINE,
    )
    text = text.replace(
        "currentDisposition: preserved-not-activated\n",
        "currentDisposition: preserved-not-activated\n"
        f"planningCandidate: {candidate}\n"
        f"epicAuthority: {EPIC_AUTHORITY}\n"
        f"architectureAuthority: {ARCHITECTURE_AUTHORITY}\n",
        1,
    )
    text = text.replace("Story 6.4 disposition contract", "Stories 8.1-8.2 preservation contract")
    text = text.replace("defined by Epic 6 v8", "rebound by Epic 8 under v14")
    text = text.replace("rebound by Epic 8 under v10", "rebound by Epic 8 under v14")
    text = text.replace("rebound by Epic 8 under v11", "rebound by Epic 8 under v14")
    text = text.replace("rebound by Epic 8 under v12", "rebound by Epic 8 under v14")
    decisions = tuple(re.findall(r"^\| (UX-DR\d+) \|", text, re.MULTILINE))
    criteria = tuple(
        re.findall(r"^\| (AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}) \|", text, re.MULTILINE)
    )
    if decisions != EXPECTED_UX_DECISION_IDS or criteria != EXPECTED_UX_ACCEPTANCE_IDS:
        raise PublicationError("UX_PARITY_DRIFT", f"decisions={decisions!r} criteria={criteria!r}")
    if text.count("preserved-not-activated") < 81:
        raise PublicationError("UX_DISPOSITION_DRIFT", "preservation disposition missing")
    return text.encode("utf-8")


def slugify(value: str) -> str:
    """Create a stable sprint-status story slug."""

    return re.sub(r"^-|-$", "", re.sub(r"[^a-z0-9]+", "-", value.lower()))


def validate_sprint_structure(text: str) -> None:
    """Require one top-level update date and no checkpoint lifecycle identity."""

    if len(re.findall(r"^last_updated: [^\n]+$", text, re.MULTILINE)) != 1:
        raise PublicationError("SPRINT_PROJECTION_DRIFT", "expected one top-level last_updated")
    development_start = text.find("development_status:\n")
    actions_start = text.find("\naction_items:\n", development_start)
    if development_start < 0 or actions_start < 0:
        raise PublicationError("SPRINT_PROJECTION_DRIFT", "development_status boundary missing")
    development = text[development_start:actions_start]
    if re.search(rf"^  [^:\n]*{re.escape(SLICE_ID)}[^:\n]*:", development, re.MULTILINE):
        raise PublicationError("SPRINT_PROJECTION_DRIFT", "checkpoint cannot have a lifecycle key")
    if re.search(r"^  [^:\n]*E6-REMEDIATION[^:\n]*:", development, re.MULTILINE):
        raise PublicationError("SPRINT_PROJECTION_DRIFT", "remediation checkpoint cannot have a lifecycle key")


def validate_epic_6_retrospective(text: str) -> None:
    """Preserve the completed retrospective and its six ordered status-bound rows."""

    development_start = text.find("development_status:\n")
    actions_start = text.find("\naction_items:\n", development_start)
    if development_start < 0 or actions_start < 0:
        raise PublicationError("EPIC_6_RETROSPECTIVE_DRIFT", "sprint boundaries missing")
    development = text[development_start:actions_start]
    if development.count("  epic-6-retrospective: done\n") != 1:
        raise PublicationError("EPIC_6_RETROSPECTIVE_DRIFT", "retrospective must remain done")
    action_text = text[actions_start + 1 :]
    matches = list(
        re.finditer(
            r'^  - id: "(epic-6-retro-item-[^"]+)"\n.*?(?=^  - |\Z)',
            action_text,
            re.MULTILINE | re.DOTALL,
        )
    )
    identities = tuple(match.group(1) for match in matches)
    payload = "".join(match.group(0) for match in matches).encode("utf-8")
    if identities != EPIC_6_RETROSPECTIVE_IDS or sha256(payload) != EPIC_6_RETROSPECTIVE_DIGEST:
        raise PublicationError("EPIC_6_RETROSPECTIVE_DRIFT", f"rows={identities!r} digest={sha256(payload)}")
    observed_statuses = []
    for match in matches:
        status_match = re.search(r"^    status: (\w+)$", match.group(0), re.MULTILINE)
        if status_match is None:
            raise PublicationError("EPIC_6_RETROSPECTIVE_DRIFT", f"{match.group(1)} missing status")
        observed_statuses.append(status_match.group(1))
    if tuple(observed_statuses) != EPIC_6_RETROSPECTIVE_STATUSES:
        raise PublicationError(
            "EPIC_6_RETROSPECTIVE_DRIFT",
            f"statuses={observed_statuses!r} expected={list(EPIC_6_RETROSPECTIVE_STATUSES)!r}",
        )


def render_sprint(source: bytes, contracts: dict[str, dict[str, Any]]) -> bytes:
    """Regenerate the successor backlog while preserving comments and action items."""

    text = source.decode("utf-8")
    validate_sprint_structure(text)
    validate_epic_6_retrospective(text)
    text = re.sub(r"^# V(?:10|11|12|14) PLANNING PUBLICATION:.*\n", "", text, flags=re.MULTILINE)
    text = re.sub(r"^last_updated: .*?$", "last_updated: 2026-08-19", text, count=1, flags=re.MULTILINE)
    notice = (
        "# V14 PLANNING PUBLICATION: authorities epic-6-authority-2026-08-18-v14 and "
        "conversations-architecture-2026-08-18-v14 are candidate-bound by "
        "v9-authority-bundle-v1.json. GLOBAL IMPLEMENTATION HOLD remains ACTIVE; "
        "IR-0 was not run, A2-A6 remain open, and all successor stories including Epic 16 remain backlog.\n"
    )
    updated_line = "last_updated: 2026-08-19\n"
    text = text.replace(updated_line, updated_line + notice, 1)
    start = text.find("development_status:\n")
    end = text.find("\naction_items:\n", start)
    if start < 0 or end < 0:
        raise PublicationError("SPRINT_PROJECTION_DRIFT", "development_status boundary missing")
    existing = text[start:end]
    prefix_lines = []
    for line in existing.splitlines()[1:]:
        if line.startswith("  epic-6:"):
            break
        prefix_lines.append(line)
    status_lines = ["development_status:", *prefix_lines]
    status_lines.extend(
        (
            "  epic-6: done",
            "  6-1-rebaseline-architecture-and-planning-authority: done",
            "  6-2-migrate-conversations-to-platform-owned-hosting: done",
            "  6-7-mechanically-block-incomplete-submodule-promotions-from-completion: done",
            "  epic-6-retrospective: done",
        )
    )
    for epic in range(7, 17):
        status_lines.append(f"  epic-{epic}: backlog")
        for story_id, contract in contracts.items():
            if story_id.startswith(f"{epic}."):
                major, minor = story_id.split(".")
                status_lines.append(f"  {major}-{minor}-{slugify(contract['outcome']['title'])}: backlog")
        status_lines.append(f"  epic-{epic}-retrospective: optional")
    text = text[:start] + "\n".join(status_lines) + text[end:]
    successor_rows = re.findall(r"^  (?:[7-9]|1[0-6])-\d+-[^:]+: backlog$", text, re.MULTILINE)
    if len(successor_rows) != 30:
        raise PublicationError("SPRINT_PROJECTION_DRIFT", f"{len(successor_rows)} successor stories")
    a5 = re.search(
        r'action: "Promote the Story 5\.3 evidence-boundary validation pattern into reusable dev/review guidance\.".*?status: (\S+)',
        text,
        re.DOTALL,
    )
    if a5 is None or a5.group(1) != "open":
        raise PublicationError("EPIC_5_ACTION_A5_DRIFT", "A5 must remain open")
    validate_epic_6_retrospective(text)
    validate_sprint_structure(text)
    return text.encode("utf-8")


def gitlinks(root: Path, candidate: str) -> list[dict[str, str]]:
    """Validate and read the exact ten root-declared raw mode-160000 gitlinks."""

    try:
        gitmodules = candidate_blob(root, candidate, ".gitmodules").decode("utf-8")
        output = git(root, "ls-tree", "-rz", candidate).decode("utf-8")
    except UnicodeError as error:
        raise PublicationError("GITLINK_SCOPE_MISMATCH", str(error)) from error
    declared = tuple(sorted(re.findall(r"^\s*path\s*=\s*(\S+)\s*$", gitmodules, re.MULTILINE)))
    if declared != ROOT_GITLINK_PATHS or len(declared) != len(set(declared)):
        raise PublicationError("GITLINK_SCOPE_MISMATCH", f".gitmodules={declared!r}")
    rows = []
    for entry in output.split("\0"):
        match = re.fullmatch(r"160000 commit ([0-9a-f]{40})\t(.+)", entry)
        if match:
            rows.append({"path": match.group(2), "commit": match.group(1)})
    rows.sort(key=lambda row: row["path"])
    raw_paths = tuple(row["path"] for row in rows)
    if raw_paths != ROOT_GITLINK_PATHS:
        raise PublicationError("GITLINK_SCOPE_MISMATCH", f"raw={raw_paths!r}")
    return rows


def artifact_row(path: str, content: bytes, role: str, owner: str, source: str, schema: str | None) -> dict[str, Any]:
    """Create one deterministic authority-bundle row."""

    return {
        "path": path,
        "sha256": sha256(content),
        "role": role,
        "owner": owner,
        "source": source,
        "schemaIdentity": schema,
    }


def expected_bundle_artifact_paths() -> tuple[str, ...]:
    """Return the exact self-excluding canonical/generated bundle inventory."""

    return tuple(
        sorted(
            (
                *CANONICAL_PATHS,
                SPEC_PATH,
                VIEW_V1_PATH,
                *PINNED_AUTHORITY_PATHS,
                *(path for path in EXPECTED_OUTPUT_PATHS if path != BUNDLE_PATH),
            )
        )
    )


def render_bundle(root: Path, candidate: str, outputs: dict[str, bytes]) -> bytes:
    """Render the self-excluding complete authority bundle."""

    rows: list[dict[str, Any]] = []
    for path in CANONICAL_PATHS:
        role = "schema" if path in SCHEMA_PATHS else "canonical-authority-input"
        owner = "Quality owner" if path in SCHEMA_PATHS or "/scripts/" in path else "Planning owner"
        rows.append(artifact_row(path, candidate_blob(root, candidate, path), role, owner, "candidate", None))
    rows.append(
        artifact_row(
            SPEC_PATH,
            candidate_blob(root, candidate, SPEC_PATH),
            "implementation-spec",
            "Planning owner",
            "candidate",
            None,
        )
    )
    rows.append(
        artifact_row(VIEW_V1_PATH, candidate_blob(root, candidate, VIEW_V1_PATH), "immutable-v8-provenance", "Planning owner", "candidate", None)
    )
    for path in PINNED_AUTHORITY_PATHS:
        schema = (
            "hexalith.conversations.v13-current-proof-authority.v1"
            if path == CURRENT_PROOF_PATH
            else "hexalith.conversations.v14-current-candidate-authority.v1"
        )
        rows.append(
            artifact_row(path, candidate_blob(root, candidate, path), "checkpoint-authority", "Release owner", "candidate", schema)
        )
    for path, content in outputs.items():
        if path == BUNDLE_PATH:
            continue
        if "/story-contracts/" in path:
            role = "base-story-contract" if path.endswith("/7.1.json") else "story-contract"
            story_id = Path(path).stem
            schema = (
                V14_STORY_CONTRACT_SCHEMA_IDENTITY
                if story_id in V14_AMENDED_STORY_IDS
                else V9_STORY_CONTRACT_SCHEMA_IDENTITY
            )
            owner = "Product Manager"
        elif path == SLICE_PATH:
            role, owner, schema = (
                "story-slice-authority",
                "Product Manager",
                "hexalith.conversations.story-slice-authority.v1",
            )
        elif path == REMEDIATION_PATH:
            role, owner, schema = (
                "pre-ir0-remediation-authority",
                "Planning owner",
                "hexalith.conversations.v12-pre-ir0-remediation-authority.v1",
            )
        elif "/inventories/" in path:
            role, owner, schema = "inventory", "Workflow owner", "hexalith.conversations.v9-inventory.v1"
        elif path == GRAPH_PATH:
            role, owner, schema = "execution-graph", "Workflow owner", "hexalith.conversations.v9-execution-graph.v1"
        elif path == SUPERSESSION_PATH:
            role, owner, schema = "supersession-map", "Product Manager", "hexalith.conversations.v9-supersession-map.v1"
        elif "/resolved-customization/" in path:
            role, owner, schema = "resolved-customization", "Workflow owner", "hexalith.conversations.resolved-customization.v1"
        else:
            role, owner, schema = "generated-projection", "Workflow owner", None
        rows.append(artifact_row(path, content, role, owner, "generated", schema))
    rows.sort(key=lambda row: row["path"])
    if len({row["path"] for row in rows}) != len(rows):
        raise PublicationError("BUNDLE_PATH_DUPLICATE", "artifact path repeated")
    bundle_paths = tuple(row["path"] for row in rows)
    if bundle_paths != expected_bundle_artifact_paths():
        raise PublicationError("BUNDLE_INVENTORY_DRIFT", repr(bundle_paths))
    roles = {row["path"]: row["role"] for row in rows}
    if roles.get("_bmad-output/planning-artifacts/v9/story-contracts/7.1.json") != "base-story-contract":
        raise PublicationError("BUNDLE_ROLE_DRIFT", "Story 7.1 base contract role")
    if roles.get(SLICE_PATH) != "story-slice-authority":
        raise PublicationError("BUNDLE_ROLE_DRIFT", "Story 7.1 sidecar role")
    if roles.get(REMEDIATION_PATH) != "pre-ir0-remediation-authority":
        raise PublicationError("BUNDLE_ROLE_DRIFT", "E6 remediation sidecar role")
    if roles.get(SPEC_PATH) != "implementation-spec":
        raise PublicationError("BUNDLE_ROLE_DRIFT", "implementation spec role")
    if any(roles.get(path) != "checkpoint-authority" for path in PINNED_AUTHORITY_PATHS):
        raise PublicationError("BUNDLE_ROLE_DRIFT", "current checkpoint authority role")
    if any(
        "implementation-readiness-report" in path.lower()
        or path.endswith("implementation-hold-v1.json")
        for path in bundle_paths
    ):
        raise PublicationError("BUNDLE_INVENTORY_DRIFT", "mutable gate or hold result")
    digest_payload = "".join(f"{row['sha256']}  {row['path']}\n" for row in rows).encode("utf-8")
    bundle = {
        "schemaVersion": "hexalith.conversations.v9-authority-bundle.v1",
        "bundleVersion": "v9-authority-bundle-v1",
        "planningCandidate": candidate,
        "authorities": AUTHORITIES,
        "implementationHold": "ACTIVE",
        "epic5ActionA5": "open",
        "gitlinks": gitlinks(root, candidate),
        "artifacts": rows,
        "bundleDigest": sha256(digest_payload),
    }
    return json_bytes(bundle)


def validate_schemas(root: Path, outputs: dict[str, bytes]) -> None:
    """Validate schemas and every generated JSON document."""

    try:
        import jsonschema
    except ImportError as error:
        raise PublicationError("SCHEMA_VALIDATION_UNAVAILABLE", str(error)) from error
    try:
        schemas = {path: json.loads((root / path).read_text(encoding="utf-8")) for path in SCHEMA_PATHS}
        for schema in schemas.values():
            jsonschema.Draft202012Validator.check_schema(schema)
        for path, content in outputs.items():
            if not path.endswith(".json"):
                continue
            document = json.loads(content)
            if "/story-contracts/" in path:
                story_id = Path(path).stem
                schema_path = SCHEMA_PATHS[8] if story_id in V14_AMENDED_STORY_IDS else SCHEMA_PATHS[0]
            elif "/inventories/" in path:
                schema_path = SCHEMA_PATHS[1]
            elif path == GRAPH_PATH:
                schema_path = SCHEMA_PATHS[2]
            elif path == SUPERSESSION_PATH:
                schema_path = SCHEMA_PATHS[3]
            elif path == BUNDLE_PATH:
                schema_path = SCHEMA_PATHS[4]
            elif path == SLICE_PATH:
                schema_path = SCHEMA_PATHS[5]
            elif path == REMEDIATION_PATH:
                schema_path = SCHEMA_PATHS[6]
            else:
                continue
            jsonschema.Draft202012Validator(schemas[schema_path]).validate(document)
        for path, schema_path in (
            (CURRENT_PROOF_PATH, SCHEMA_PATHS[9]),
            (CURRENT_CANDIDATE_PATH, SCHEMA_PATHS[10]),
        ):
            jsonschema.Draft202012Validator(schemas[schema_path]).validate(
                json.loads((root / path).read_text(encoding="utf-8"))
            )
    except (OSError, UnicodeError, json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise PublicationError("SCHEMA_VALIDATION_FAILED", str(error)) from error


def render_outputs(root: Path, candidate: str) -> dict[str, bytes]:
    """Validate all inputs, then render the complete companion set in memory."""

    require_candidate_bytes(root, candidate, CANONICAL_PATHS + PROTECTED_CANDIDATE_PATHS)
    epics = candidate_blob(root, candidate, EPICS_PATH)
    architecture = candidate_blob(root, candidate, ARCHITECTURE_PATH)
    v10_epic, v11_epic, _v12_epic, v14_epic = validate_authority_prefixes(epics, architecture)
    if sha256(candidate_blob(root, candidate, SCHEMA_PATHS[0])) != FROZEN_STORY_CONTRACT_SCHEMA_DIGEST:
        raise PublicationError("STORY_CONTRACT_SCHEMA_DRIFT", SCHEMA_PATHS[0])
    validate_route_topology(root, candidate)
    if inventory_digest(list(GUIDANCE_PATHS)) != "e0a9adf0319286763f44d586ac323203a4af3d7faa4005e23768ce4a7c8f335d":
        raise PublicationError("EVIDENCE_GUIDANCE_DRIFT", "guidance inventory digest")
    if inventory_digest(list(READER_PATHS)) != "247cd610f7fd162f3e01f1db713f16328b2d009081da14a468e767411209a3bc":
        raise PublicationError("EVIDENCE_READER_INVENTORY_DRIFT", "reader inventory digest")
    for path, expected_digest in (
        (CURRENT_PROOF_PATH, CURRENT_PROOF_DIGEST),
        (CURRENT_CANDIDATE_PATH, CURRENT_CANDIDATE_DIGEST),
    ):
        observed = sha256(candidate_blob(root, candidate, path))
        if observed != expected_digest:
            raise PublicationError("CURRENT_SIDECAR_HEAD_DRIFT", f"{path}: {observed}")
    contracts = parse_contracts(
        epics.decode("utf-8"),
        candidate,
        v10_epic,
        v14_epic,
        candidate_blob(root, candidate, V14_PROPOSAL_PATH).decode("utf-8"),
    )
    outputs: dict[str, bytes] = {
        f"_bmad-output/planning-artifacts/v9/story-contracts/{story_id}.json": json_bytes(contract)
        for story_id, contract in contracts.items()
    }
    base_story_contract_path = "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json"
    outputs[SLICE_PATH] = render_story_slice(candidate, outputs[base_story_contract_path], v11_epic)
    slice_authority = json.loads(outputs[SLICE_PATH])
    validate_story_slice(slice_authority, outputs[base_story_contract_path], v11_epic, candidate)
    outputs[REMEDIATION_PATH] = render_remediation_authority(root, candidate)
    validate_remediation_authority(root, candidate, json.loads(outputs[REMEDIATION_PATH]))
    outputs.update(render_resolved_customization(root, candidate))
    outputs["_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json"] = render_inventory(
        root, candidate, "V9-EVIDENCE-WORKFLOWS-v2", MECHANICAL_PATHS, mechanical=True
    )
    outputs["_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v3.json"] = render_inventory(
        root, candidate, "V12-ACTIVE-LIFECYCLE-ROUTES-v1", MECHANICAL_PATHS, mechanical=True
    )
    outputs["_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json"] = render_inventory(
        root, candidate, "V9-EVIDENCE-GUIDANCE-v2", GUIDANCE_PATHS
    )
    outputs["_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json"] = render_inventory(
        root, candidate, "V9-EVIDENCE-READERS-v1", READER_PATHS
    )
    outputs[GRAPH_PATH] = render_graph(candidate, contracts, slice_authority)
    outputs[SUPERSESSION_PATH] = render_supersession(candidate, epics.decode("utf-8"))
    outputs[VIEW_V2_PATH] = render_view(candidate, contracts)
    outputs[UX_MAP_PATH] = render_ux_map(candidate_blob(root, candidate, UX_MAP_PATH), candidate)
    outputs[SPRINT_PATH] = render_sprint(candidate_blob(root, candidate, SPRINT_PATH), contracts)
    outputs[BUNDLE_PATH] = render_bundle(root, candidate, outputs)
    validate_schemas(root, outputs)
    return outputs


def validate_managed_namespace(root: Path) -> None:
    """Reject stale files only inside publication-owned namespaces."""

    expected = set(EXPECTED_OUTPUT_PATHS)
    actual: set[str] = set()
    v9_root = root / "_bmad-output/planning-artifacts/v9"
    if v9_root.exists():
        try:
            actual.update(
                path.relative_to(root).as_posix()
                for path in v9_root.rglob("*")
                if path.is_file()
            )
        except OSError as error:
            raise PublicationError("PUBLICATION_SCOPE_DRIFT", str(error)) from error
    planning_root = root / "_bmad-output/planning-artifacts"
    if planning_root.exists():
        try:
            actual.update(path.relative_to(root).as_posix() for path in planning_root.glob("v9-*.json") if path.is_file())
            actual.update(
                path.relative_to(root).as_posix()
                for path in planning_root.glob("v11-*.json")
                if path.is_file()
            )
            actual.update(
                path.relative_to(root).as_posix()
                for path in planning_root.glob("v12-*.json")
                if path.is_file()
            )
        except OSError as error:
            raise PublicationError("PUBLICATION_SCOPE_DRIFT", str(error)) from error
    unexpected = sorted(actual - expected)
    if unexpected:
        raise PublicationError("PUBLICATION_SCOPE_DRIFT", f"stale={unexpected!r}")


def replace_managed_set(root: Path, outputs: dict[str, bytes]) -> None:
    """Stage the complete set, then replace it with byte-restoring rollback."""

    ordered = sorted(outputs)
    prior: dict[str, bytes | None] = {}
    replaced: list[str] = []
    try:
        root.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(prefix=".v9-publication.", dir=root) as temporary:
            staging_root = Path(temporary)
            for ordinal, relative_path in enumerate(ordered):
                target = root / relative_path
                prior[relative_path] = target.read_bytes() if target.is_file() else None
                staged = staging_root / "staged" / f"{ordinal:03d}"
                staged.parent.mkdir(parents=True, exist_ok=True)
                staged.write_bytes(outputs[relative_path])
                if staged.read_bytes() != outputs[relative_path]:
                    raise OSError(f"staged byte drift: {relative_path}")
            try:
                for ordinal, relative_path in enumerate(ordered):
                    target = root / relative_path
                    target.parent.mkdir(parents=True, exist_ok=True)
                    os.replace(staging_root / "staged" / f"{ordinal:03d}", target)
                    replaced.append(relative_path)
            except OSError as error:
                rollback_errors: list[str] = []
                for rollback_ordinal, relative_path in enumerate(reversed(replaced)):
                    target = root / relative_path
                    try:
                        previous = prior[relative_path]
                        if previous is None:
                            target.unlink(missing_ok=True)
                        else:
                            restore = staging_root / "restore" / f"{rollback_ordinal:03d}"
                            restore.parent.mkdir(parents=True, exist_ok=True)
                            restore.write_bytes(previous)
                            os.replace(restore, target)
                    except OSError as rollback_error:
                        rollback_errors.append(f"{relative_path}: {rollback_error}")
                if rollback_errors:
                    raise PublicationError(
                        "PUBLICATION_WRITE_FAILED",
                        f"{error}; rollback={rollback_errors!r}",
                    ) from error
                raise PublicationError("PUBLICATION_WRITE_FAILED", str(error)) from error
    except PublicationError:
        raise
    except OSError as error:
        raise PublicationError("PUBLICATION_WRITE_FAILED", str(error)) from error


def publish(root: Path, outputs: dict[str, bytes], check: bool) -> None:
    """Check or atomically replace every expected output after full validation."""

    actual_scope = set(outputs)
    expected_scope = set(EXPECTED_OUTPUT_PATHS)
    if actual_scope != expected_scope:
        missing = sorted(expected_scope - actual_scope)
        unexpected = sorted(actual_scope - expected_scope)
        raise PublicationError(
            "PUBLICATION_SCOPE_DRIFT",
            f"missing={missing!r} unexpected={unexpected!r}",
        )
    validate_managed_namespace(root)
    if check:
        try:
            drift = [
                path
                for path, content in outputs.items()
                if not (root / path).is_file() or (root / path).read_bytes() != content
            ]
        except OSError as error:
            raise PublicationError("OUTPUT_DRIFT", str(error)) from error
        if drift:
            raise PublicationError("OUTPUT_DRIFT", ", ".join(drift))
        return
    replace_managed_set(root, outputs)


def main() -> int:
    """Publish or check the complete candidate-bound planning authority."""

    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", default=".")
    parser.add_argument("--candidate")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    root = Path(args.repository).resolve()
    try:
        candidate = resolve_candidate(root, args.candidate, args.check)
        outputs = render_outputs(root, candidate)
        publish(root, outputs, args.check)
        bundle = json.loads(outputs[BUNDLE_PATH])
        print(f"V14_PLANNING_AUTHORITY_OK PC={candidate} BUNDLE={bundle['bundleDigest']}")
        return 0
    except PublicationError as error:
        print(f"{error.code}: {error.detail}", file=sys.stderr)
        return 1
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        print(f"PUBLICATION_INPUT_INVALID: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
