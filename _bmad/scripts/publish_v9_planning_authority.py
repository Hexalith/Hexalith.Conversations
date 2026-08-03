#!/usr/bin/env python3
"""Publish and validate the candidate-bound v9/v10 planning companion set."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
from typing import Any


sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))

from config_utils import ConfigError, load_customization  # noqa: E402


EPIC_AUTHORITY = "epic-6-authority-2026-08-03-v10"
ARCHITECTURE_AUTHORITY = "conversations-architecture-2026-08-03-v10"
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
V9_EPIC_BLOCK_SIZE = 188677
V9_EPIC_BLOCK_DIGEST = "e7d6ea5759c12ab70f21b472656828bb4e5bcce2023d845f06a40cf1373d1c9d"
V9_ARCHITECTURE_BLOCK_SIZE = 18270
V9_ARCHITECTURE_BLOCK_DIGEST = "4686212387189e78f98de5352d12eb8544d1a9f78c97dfc446266fa3d4d3f3d9"

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
GUIDANCE_PATHS = (
    "_bmad/custom/bmad-build.toml",
    "_bmad/custom/bmad-build-auto.toml",
    "_bmad/custom/bmad-review.toml",
    "docs/runbooks/evidence-boundary-validation.md",
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
)
CANONICAL_PATHS = (
    EPICS_PATH,
    ARCHITECTURE_PATH,
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md",
    *GUIDANCE_PATHS,
    "_bmad/scripts/publish_v9_planning_authority.py",
    *SCHEMA_PATHS,
)
PROTECTED_CANDIDATE_PATHS = (
    VIEW_V1_PATH,
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

    return sha256(("\n".join(paths) + "\n").encode("utf-8"))


def git(root: Path, *arguments: str) -> bytes:
    """Run one bounded non-interactive Git command."""

    result = subprocess.run(
        ("git", "-C", str(root), *arguments),
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=30,
        env={**os.environ, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0"},
    )
    if result.returncode != 0:
        detail = result.stderr.decode("utf-8", errors="replace").strip()
        raise PublicationError("CANDIDATE_GIT_UNAVAILABLE", detail or "Git command failed")
    return result.stdout


def resolve_candidate(root: Path, requested: str | None, check: bool) -> str:
    """Resolve the one immutable planning-candidate commit."""

    if check:
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
        if current_path.read_bytes() != candidate_blob(root, candidate, relative_path):
            raise PublicationError("CANDIDATE_SOURCE_DRIFT", relative_path)


def marker_block(content: bytes, begin_token: str, end_token: str) -> bytes:
    """Extract one inclusive marker block from exact bytes."""

    begin = begin_token.encode("utf-8")
    end = end_token.encode("utf-8")
    if content.count(begin) != 1 or content.count(end) != 1:
        raise PublicationError("AUTHORITY_MARKER_INVALID", f"{begin_token} / {end_token}")
    start = content.index(begin)
    end_start = content.index(end, start)
    close = content.index(b"-->", end_start) + 3
    return content[start:close]


def validate_authority_prefixes(epics: bytes, architecture: bytes) -> tuple[str, str]:
    """Validate immutable v9 blocks and complete v10 successors."""

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
    v10_epic = marker_block(
        epics,
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V10:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V10:END",
    ).decode("utf-8")
    v10_architecture = marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V10:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V10:END",
    ).decode("utf-8")
    for source in (v10_epic, v10_architecture):
        if EPIC_AUTHORITY not in source or ARCHITECTURE_AUTHORITY not in source:
            raise PublicationError("AUTHORITY_IDENTITY_DRIFT", "v10 identity missing")
        if "hold" not in source.lower() or "ACTIVE" not in source:
            raise PublicationError("IMPLEMENTATION_HOLD_DRIFT", "ACTIVE hold missing")
    if v10_epic.count("Story 10.3 V10 Amendment") != 1 or v10_epic.count("Story 10.4 V10 Amendment") != 1:
        raise PublicationError("AUTHORITY_SCOPE_DRIFT", "effective Story 10.3/10.4 amendments missing")
    return v10_epic, v10_architecture


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
        results = [
            result
            for result in ("PASS", "FAIL", "BLOCKED", "not-applicable")
            if result in row
        ]
        scenarios.append(
            {
                "id": match.group(1),
                "command": scenario_command(row),
                "contract": " ".join(row.split()),
                "requiredResults": results,
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


def parse_contracts(epics: str, candidate: str, v10_block: str) -> dict[str, dict[str, Any]]:
    """Derive all 27 effective story contracts."""

    v9_block = marker_block(
        epics.encode("utf-8"),
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:END",
    ).decode("utf-8")
    story_matches = re.findall(r"^### Story (\d+\.\d+):", v9_block, re.MULTILINE)
    if tuple(story_matches) != EXPECTED_STORY_IDS:
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
    for story_id in EXPECTED_STORY_IDS:
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
            scenarios.append(
                {
                    "id": "AC-10.4-09",
                    "command": "python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check",
                    "contract": " ".join(ten_four_amendment.split()),
                    "requiredResults": ["PASS", "FAIL", "BLOCKED", "not-applicable"],
                }
            )
            effective_section = f"{section}\n\n{ten_four_amendment}"
        expected_prefix = f"AC-{story_id}-"
        if any(not scenario["id"].startswith(expected_prefix) for scenario in scenarios):
            raise PublicationError("STORY_SCENARIO_ID_DRIFT", story_id)

        predecessor_text = field(section, "Exact predecessors")
        predecessors = re.findall(r"`(\d+\.\d+)`", predecessor_text)
        rollback = field(section, "Rollback boundary")
        candidate_binding = field(section, "Candidate binding")
        bounded_outcome = field(section, "Bounded outcome")
        inventory_match = re.search(
            r"\*\*Frozen (?:entry )?inventory:\*\*.*?`([^`]+)`.*?SHA-256\s+`([0-9a-f]{64})`",
            section,
            re.DOTALL,
        )
        final_paths = sorted(
            set(
                re.findall(
                    r"(?:docs|_bmad-output)/[A-Za-z0-9_./-]*story-[0-9.]+-final-record-v2\.(?:json|md)",
                    section,
                )
            )
        )
        contracts[story_id] = {
            "schemaVersion": "hexalith.conversations.v9-story-contract.v1",
            "planningCandidate": candidate,
            "authorities": AUTHORITIES,
            "storyId": story_id,
            "title": title,
            "boundedOutcome": bounded_outcome,
            "predecessors": predecessors,
            "rollbackBoundary": rollback,
            "candidateBinding": candidate_binding,
            "entryInventory": (
                {"id": inventory_match.group(1), "sha256": inventory_match.group(2)}
                if inventory_match
                else None
            ),
            "authoritySectionSha256": sha256(effective_section.encode("utf-8")),
            "finalRecordPaths": final_paths,
            "scenarios": scenarios,
        }
    return contracts


def validate_route_topology(root: Path, candidate: str) -> None:
    """Validate current route inventory, tree parity, and forwarding aliases."""

    if inventory_digest(list(MECHANICAL_PATHS)) != "966745d95e24aeb95af58a2bbfab11de7b08b8ab9f2447aa6c90a99c444292d4":
        raise PublicationError("WORKFLOW_INVENTORY_DRIFT", "mechanical inventory digest")
    for logical in MECHANICAL_LOGICAL_BODIES:
        agents = candidate_blob(root, candidate, f".agents/skills/{logical}")
        claude = candidate_blob(root, candidate, f".claude/skills/{logical}")
        if agents != claude:
            raise PublicationError("EVIDENCE_WORKFLOW_PARITY_DRIFT", logical)
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
    for skill in ("bmad-build", "bmad-build-auto", "bmad-review"):
        agents_default = f".agents/skills/{skill}/customize.toml"
        claude_default = f".claude/skills/{skill}/customize.toml"
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


def render_graph(candidate: str, contracts: dict[str, dict[str, Any]]) -> bytes:
    """Render and validate the current acyclic story/gate graph."""

    nodes: dict[str, dict[str, Any]] = {
        "6.2": {"id": "6.2", "kind": "historical-story", "predecessors": []},
        "PC-PUBLICATION": {"id": "PC-PUBLICATION", "kind": "publication", "predecessors": []},
        "IR-0": {"id": "IR-0", "kind": "gate", "predecessors": ["PC-PUBLICATION"]},
        "RG-15": {"id": "RG-15", "kind": "release-gate", "predecessors": ["15.2"]},
    }
    for story_id, contract in contracts.items():
        nodes[story_id] = {"id": story_id, "kind": "story", "predecessors": contract["predecessors"]}
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
    edges = sorted(
        (
            {"from": predecessor, "to": node["id"]}
            for node in nodes.values()
            for predecessor in node["predecessors"]
        ),
        key=lambda edge: (edge["from"], edge["to"]),
    )
    return json_bytes(
        {
            "schemaVersion": "hexalith.conversations.v9-execution-graph.v1",
            "planningCandidate": candidate,
            "authorities": AUTHORITIES,
            "implementationHold": "ACTIVE",
            "nodes": [nodes[node_id] for node_id in sorted(nodes)],
            "edges": edges,
        }
    )


def render_supersession(candidate: str, epics: str) -> bytes:
    """Render all unfinished-story dispositions and 66 v8 AC obligations."""

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
    matches = re.findall(
        r"^\| `(V8-6\.(?:3|4|5|6|8|9|10|11|12)-AC\d+)` \| `([^`]+)` \|$",
        marker_block(
            epics.encode("utf-8"),
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN",
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:END",
        ).decode("utf-8"),
        re.MULTILINE,
    )
    if len(matches) != 66 or len(dict(matches)) != 66:
        raise PublicationError("SUPERSESSION_OBLIGATION_DRIFT", str(len(matches)))
    obligations = []
    for source_id, raw_bindings in matches:
        bindings = raw_bindings.split(",")
        if source_id == "V8-6.10-AC9" and "AC-10.4-09" not in bindings:
            bindings.append("AC-10.4-09")
        obligations.append({"sourceId": source_id, "bindings": bindings})
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
            "v8AcceptanceObligations": obligations,
        }
    )


def render_view(candidate: str, contracts: dict[str, dict[str, Any]]) -> bytes:
    """Render the non-amending v2 current execution view."""

    rows = "\n".join(
        f"| {story_id} | {contract['title']} | {', '.join(contract['predecessors'])} | {len(contract['scenarios'])} |"
        for story_id, contract in contracts.items()
    )
    content = f"""---
artifact: epic-6-current-execution-view-v2
generated: '2026-08-03'
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
> view projects the v10-corrected v9 authority. It does not implement a story,
> run IR-0, lift the hold, close Epic 5 action A5, or authorize release.

The canonical epic authority and architecture overlay remain the semantic
sources. This file is regenerated from their committed blobs at `PC` and is
non-amending.

| Story | Bounded outcome | Exact predecessors | AC count |
| --- | --- | --- | ---: |
{rows}

## Gate State

- IR-0: not run by this publication.
- Implementation hold: `ACTIVE`.
- Epic 5 action A5: `open` until a compatible Story 10.4 `9/9/0/0/0/0` final record passes.
"""
    return content.encode("utf-8")


def render_ux_map(source: bytes, candidate: str) -> bytes:
    """Rebind the preserved 52/28 UX projection without semantic activation."""

    text = source.decode("utf-8")
    text = re.sub(
        r"^authorityVersion: .*?$",
        "authorityVersion: ux-preservation-planning-2026-08-03-v2",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    if "planningCandidate:" not in text.split("---", 2)[1]:
        text = text.replace(
            "currentDisposition: preserved-not-activated\n",
            "currentDisposition: preserved-not-activated\n"
            f"planningCandidate: {candidate}\n"
            f"epicAuthority: {EPIC_AUTHORITY}\n"
            f"architectureAuthority: {ARCHITECTURE_AUTHORITY}\n",
            1,
        )
    text = text.replace("Story 6.4 disposition contract", "Stories 8.1-8.2 preservation contract")
    text = text.replace("defined by Epic 6 v8", "rebound by Epic 8 under v10")
    decisions = re.findall(r"^\| UX-DR\d+ \|", text, re.MULTILINE)
    criteria = re.findall(r"^\| AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3} \|", text, re.MULTILINE)
    if len(decisions) != 52 or len(criteria) != 28:
        raise PublicationError("UX_PARITY_DRIFT", f"{len(decisions)}/52 {len(criteria)}/28")
    if text.count("preserved-not-activated") < 81:
        raise PublicationError("UX_DISPOSITION_DRIFT", "preservation disposition missing")
    return text.encode("utf-8")


def slugify(value: str) -> str:
    """Create a stable sprint-status story slug."""

    return re.sub(r"^-|-$", "", re.sub(r"[^a-z0-9]+", "-", value.lower()))


def render_sprint(source: bytes, contracts: dict[str, dict[str, Any]]) -> bytes:
    """Regenerate the successor backlog while preserving comments and action items."""

    text = source.decode("utf-8")
    text = re.sub(r"^last_updated: .*?$", "last_updated: 2026-08-03", text, count=1, flags=re.MULTILINE)
    notice = (
        "# V10 PLANNING PUBLICATION: authorities epic-6-authority-2026-08-03-v10 and "
        "conversations-architecture-2026-08-03-v10 are candidate-bound by "
        "v9-authority-bundle-v1.json. GLOBAL IMPLEMENTATION HOLD remains ACTIVE; "
        "IR-0 was not run, Epic 5 action A5 remains open, and successor stories remain backlog.\n"
    )
    updated_line = "last_updated: 2026-08-03\n"
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
            "  epic-6-retrospective: optional",
        )
    )
    for epic in range(7, 16):
        status_lines.append(f"  epic-{epic}: backlog")
        for story_id, contract in contracts.items():
            if story_id.startswith(f"{epic}."):
                major, minor = story_id.split(".")
                status_lines.append(f"  {major}-{minor}-{slugify(contract['title'])}: backlog")
        status_lines.append(f"  epic-{epic}-retrospective: optional")
    text = text[:start] + "\n".join(status_lines) + text[end:]
    successor_rows = re.findall(r"^  (?:[7-9]|1[0-5])-\d+-[^:]+: backlog$", text, re.MULTILINE)
    if len(successor_rows) != 27:
        raise PublicationError("SPRINT_PROJECTION_DRIFT", f"{len(successor_rows)} successor stories")
    a5 = re.search(
        r'action: "Promote the Story 5\.3 evidence-boundary validation pattern into reusable dev/review guidance\.".*?status: (\S+)',
        text,
        re.DOTALL,
    )
    if a5 is None or a5.group(1) != "open":
        raise PublicationError("EPIC_5_ACTION_A5_DRIFT", "A5 must remain open")
    return text.encode("utf-8")


def gitlinks(root: Path, candidate: str) -> list[dict[str, str]]:
    """Read candidate gitlinks from raw tree mode 160000."""

    output = git(root, "ls-tree", "-r", candidate).decode("utf-8")
    rows = []
    for line in output.splitlines():
        match = re.match(r"160000 commit ([0-9a-f]{40})\t(.+)$", line)
        if match:
            rows.append({"path": match.group(2), "commit": match.group(1)})
    return sorted(rows, key=lambda row: row["path"])


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


def render_bundle(root: Path, candidate: str, outputs: dict[str, bytes]) -> bytes:
    """Render the self-excluding complete authority bundle."""

    rows: list[dict[str, Any]] = []
    for path in CANONICAL_PATHS:
        role = "schema" if path in SCHEMA_PATHS else "canonical-authority-input"
        owner = "Quality owner" if path in SCHEMA_PATHS or "/scripts/" in path else "Planning owner"
        rows.append(artifact_row(path, candidate_blob(root, candidate, path), role, owner, "candidate", None))
    rows.append(
        artifact_row(VIEW_V1_PATH, candidate_blob(root, candidate, VIEW_V1_PATH), "immutable-v8-provenance", "Planning owner", "candidate", None)
    )
    for path, content in outputs.items():
        if path == BUNDLE_PATH:
            continue
        if "/story-contracts/" in path:
            role, owner, schema = "story-contract", "Product Manager", "hexalith.conversations.v9-story-contract.v1"
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
    schemas = {path: json.loads((root / path).read_text(encoding="utf-8")) for path in SCHEMA_PATHS}
    try:
        for schema in schemas.values():
            jsonschema.Draft202012Validator.check_schema(schema)
        for path, content in outputs.items():
            if not path.endswith(".json"):
                continue
            document = json.loads(content)
            if "/story-contracts/" in path:
                schema_path = SCHEMA_PATHS[0]
            elif "/inventories/" in path:
                schema_path = SCHEMA_PATHS[1]
            elif path == GRAPH_PATH:
                schema_path = SCHEMA_PATHS[2]
            elif path == SUPERSESSION_PATH:
                schema_path = SCHEMA_PATHS[3]
            elif path == BUNDLE_PATH:
                schema_path = SCHEMA_PATHS[4]
            else:
                continue
            jsonschema.Draft202012Validator(schemas[schema_path]).validate(document)
    except (json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise PublicationError("SCHEMA_VALIDATION_FAILED", str(error)) from error


def render_outputs(root: Path, candidate: str) -> dict[str, bytes]:
    """Validate all inputs, then render the complete companion set in memory."""

    require_candidate_bytes(root, candidate, CANONICAL_PATHS + PROTECTED_CANDIDATE_PATHS)
    epics = candidate_blob(root, candidate, EPICS_PATH)
    architecture = candidate_blob(root, candidate, ARCHITECTURE_PATH)
    v10_epic, _ = validate_authority_prefixes(epics, architecture)
    validate_route_topology(root, candidate)
    if inventory_digest(list(GUIDANCE_PATHS)) != "e0a9adf0319286763f44d586ac323203a4af3d7faa4005e23768ce4a7c8f335d":
        raise PublicationError("EVIDENCE_GUIDANCE_DRIFT", "guidance inventory digest")
    if inventory_digest(list(READER_PATHS)) != "247cd610f7fd162f3e01f1db713f16328b2d009081da14a468e767411209a3bc":
        raise PublicationError("EVIDENCE_READER_INVENTORY_DRIFT", "reader inventory digest")
    contracts = parse_contracts(epics.decode("utf-8"), candidate, v10_epic)
    outputs: dict[str, bytes] = {
        f"_bmad-output/planning-artifacts/v9/story-contracts/{story_id}.json": json_bytes(contract)
        for story_id, contract in contracts.items()
    }
    outputs.update(render_resolved_customization(root, candidate))
    outputs["_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json"] = render_inventory(
        root, candidate, "V9-EVIDENCE-WORKFLOWS-v2", MECHANICAL_PATHS, mechanical=True
    )
    outputs["_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json"] = render_inventory(
        root, candidate, "V9-EVIDENCE-GUIDANCE-v2", GUIDANCE_PATHS
    )
    outputs["_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json"] = render_inventory(
        root, candidate, "V9-EVIDENCE-READERS-v1", READER_PATHS
    )
    outputs[GRAPH_PATH] = render_graph(candidate, contracts)
    outputs[SUPERSESSION_PATH] = render_supersession(candidate, epics.decode("utf-8"))
    outputs[VIEW_V2_PATH] = render_view(candidate, contracts)
    outputs[UX_MAP_PATH] = render_ux_map(candidate_blob(root, candidate, UX_MAP_PATH), candidate)
    outputs[SPRINT_PATH] = render_sprint(candidate_blob(root, candidate, SPRINT_PATH), contracts)
    outputs[BUNDLE_PATH] = render_bundle(root, candidate, outputs)
    validate_schemas(root, outputs)
    return outputs


def write_atomically(path: Path, content: bytes) -> None:
    """Replace one generated artifact atomically."""

    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=path.parent, prefix=f".{path.name}.", delete=False) as temporary:
        temporary.write(content)
        temporary_path = Path(temporary.name)
    os.replace(temporary_path, path)


def publish(root: Path, outputs: dict[str, bytes], check: bool) -> None:
    """Check or atomically replace every expected output after full validation."""

    contract_directory = root / "_bmad-output/planning-artifacts/v9/story-contracts"
    if contract_directory.exists():
        actual = {path.name for path in contract_directory.glob("*.json")}
        expected = {f"{story_id}.json" for story_id in EXPECTED_STORY_IDS}
        if actual - expected:
            raise PublicationError("STORY_CONTRACT_SET_DRIFT", repr(sorted(actual - expected)))
    if check:
        drift = [path for path, content in outputs.items() if not (root / path).is_file() or (root / path).read_bytes() != content]
        if drift:
            raise PublicationError("OUTPUT_DRIFT", ", ".join(drift))
        return
    for relative_path, content in outputs.items():
        write_atomically(root / relative_path, content)


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
        print(f"V9_PLANNING_AUTHORITY_OK PC={candidate} BUNDLE={bundle['bundleDigest']}")
        return 0
    except PublicationError as error:
        print(f"{error.code}: {error.detail}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
