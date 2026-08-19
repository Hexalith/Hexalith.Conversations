"""Tests for atomic candidate-bound V14 planning publication."""

from __future__ import annotations

import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
from typing import Callable

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_v9_planning_authority.py"
SPEC = importlib.util.spec_from_file_location("publish_v9_planning_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)

EXPECTED_BUNDLE_ARTIFACT_PATHS = (
    ".gitmodules",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
    "_bmad-output/planning-artifacts/architecture.md",
    "_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md",
    "_bmad-output/planning-artifacts/epic-6-current-execution-view-v2.md",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04.md",
    "_bmad-output/planning-artifacts/ux-requirement-map.md",
    "_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json",
    "_bmad-output/planning-artifacts/v9-execution-graph-v1.json",
    "_bmad-output/planning-artifacts/v9-supersession-map-v1.json",
    "_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json",
    "_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json",
    "_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json",
    "_bmad-output/planning-artifacts/v9/resolved-customization/bmad-build-auto.json",
    "_bmad-output/planning-artifacts/v9/resolved-customization/bmad-build.json",
    "_bmad-output/planning-artifacts/v9/resolved-customization/bmad-review.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/10.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/10.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/10.3.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/10.4.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/11.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/11.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/11.3.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/12.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/12.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/12.3.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/12.4.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/13.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/13.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/13.3.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/14.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/14.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/14.3.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/15.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/15.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/7.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/7.3.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/7.4.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/8.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/8.2.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/9.1.json",
    "_bmad-output/planning-artifacts/v9/story-contracts/9.2.json",
    "_bmad/custom/bmad-build-auto.toml",
    "_bmad/custom/bmad-build.toml",
    "_bmad/custom/bmad-review.toml",
    "_bmad/schemas/v11-story-slice-authority-v1.schema.json",
    "_bmad/schemas/v9-authority-bundle-v1.schema.json",
    "_bmad/schemas/v9-execution-graph-v1.schema.json",
    "_bmad/schemas/v9-inventory-v1.schema.json",
    "_bmad/schemas/v9-story-contract-v1.schema.json",
    "_bmad/schemas/v9-supersession-map-v1.schema.json",
    "_bmad/scripts/publish_v9_planning_authority.py",
    "_bmad/scripts/tests/test_publish_v9_planning_authority.py",
    "docs/runbooks/evidence-boundary-validation.md",
    "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs",
)
EXPECTED_BUNDLE_ARTIFACT_PATHS = publisher.expected_bundle_artifact_paths()


def published_candidate() -> str:
    """Read the committed candidate bound by the checked-in bundle."""

    return json.loads((ROOT / publisher.BUNDLE_PATH).read_text(encoding="utf-8"))["planningCandidate"]


def dummy_outputs(prefix: str) -> dict[str, bytes]:
    """Create a complete managed output set for filesystem-boundary tests."""

    return {path: f"{prefix}:{path}\n".encode() for path in publisher.EXPECTED_OUTPUT_PATHS}


def test_complete_publication_is_deterministic_and_candidate_bound() -> None:
    """Every generated companion must reproduce from the one committed PC."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)

    assert set(outputs) == set(publisher.EXPECTED_OUTPUT_PATHS)
    assert len([path for path in outputs if "/story-contracts/" in path]) == 30
    assert publisher.SLICE_PATH in outputs
    assert publisher.REMEDIATION_PATH in outputs
    for path, content in outputs.items():
        assert (ROOT / path).read_bytes() == content
    bundle = json.loads(outputs[publisher.BUNDLE_PATH])
    assert bundle["planningCandidate"] == candidate
    assert bundle["implementationHold"] == "ACTIVE"
    assert bundle["epic5ActionA5"] == "open"
    assert [row["path"] for row in bundle["gitlinks"]] == list(publisher.ROOT_GITLINK_PATHS)
    artifact_paths = tuple(row["path"] for row in bundle["artifacts"])
    assert artifact_paths == EXPECTED_BUNDLE_ARTIFACT_PATHS
    assert publisher.BUNDLE_PATH not in artifact_paths
    assert not any("implementation-readiness-report" in path.lower() for path in artifact_paths)
    assert not any(path.endswith("implementation-hold-v1.json") for path in artifact_paths)
    roles = {row["path"]: row["role"] for row in bundle["artifacts"]}
    assert roles["_bmad-output/planning-artifacts/v9/story-contracts/7.1.json"] == "base-story-contract"
    assert roles[publisher.SLICE_PATH] == "story-slice-authority"
    assert roles[publisher.REMEDIATION_PATH] == "pre-ir0-remediation-authority"
    assert roles[publisher.CURRENT_PROOF_PATH] == "checkpoint-authority"
    assert roles[publisher.CURRENT_CANDIDATE_PATH] == "checkpoint-authority"


def test_story_contract_schema_and_representative_parsing_are_exact() -> None:
    """Contracts use the canonical closed shape and explicit result semantics."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)
    publisher.validate_schemas(ROOT, outputs)
    contracts = {
        document["storyId"]: document
        for path, content in outputs.items()
        if "/story-contracts/" in path
        for document in [json.loads(content)]
    }
    required = {
        "schemaVersion",
        "storyId",
        "authority",
        "predecessors",
        "outcome",
        "rollback",
        "inventory",
        "scenarios",
        "finalRecord",
    }
    assert set(contracts) == set(publisher.EXPECTED_STORY_IDS)
    for contract in contracts.values():
        assert set(contract) == required
        assert contract["schemaVersion"] == "hexalith.conversations.story-contract.v1"
        authorities = publisher.AUTHORITIES if contract["storyId"].startswith("16.") else publisher.BASE_AUTHORITIES
        assert contract["authority"]["epic"] == authorities["epic"]
        assert contract["authority"]["architecture"] == authorities["architecture"]
        assert contract["authority"]["planningCandidate"] == candidate
        assert contract["predecessors"] == sorted(set(contract["predecessors"]))
        assert contract["scenarios"]
        assert all(scenario["resultSemantics"]["expected"] == "PASS" for scenario in contract["scenarios"])
        assert all(scenario["resultSemantics"]["passExitCodes"] for scenario in contract["scenarios"])
        summary = contract["finalRecord"]["summary"]
        assert summary == {
            "required": len(contract["scenarios"]),
            "passed": len(contract["scenarios"]),
            "failed": 0,
            "blocked": 0,
            "skipped": 0,
            "notRun": 0,
        }
    assert len(contracts["10.3"]["scenarios"]) == 8
    assert len(contracts["10.4"]["scenarios"]) == 9
    assert [
        scenario["resultSemantics"]["notApplicableAllowed"]
        for scenario in contracts["10.3"]["scenarios"]
    ] == [True, False, False, False, False, False, False, False]
    assert all(
        not scenario["resultSemantics"]["notApplicableAllowed"]
        for story_id, contract in contracts.items()
        if story_id != "10.3"
        for scenario in contract["scenarios"]
    )
    assert contracts["10.4"]["scenarios"][-1]["id"] == "AC-10.4-09"
    assert all(len(contracts[story_id]["scenarios"]) == 6 for story_id in ("16.1", "16.2", "16.3"))
    assert contracts["16.1"]["predecessors"] == ["7.4", "IR-0"]
    assert contracts["16.2"]["predecessors"] == ["16.1"]
    assert contracts["16.3"]["predecessors"] == ["16.2"]
    assert all("16.3" in contracts[story_id]["predecessors"] for story_id in ("12.1", "13.1", "14.1", "15.1"))
    ac_ten_four_eight = next(row for row in contracts["10.4"]["scenarios"] if row["id"] == "AC-10.4-08")
    assert "summary `9/9/0/0/0/0`" in ac_ten_four_eight["contract"]
    ac_fourteen_three_two = next(row for row in contracts["14.3"]["scenarios"] if row["id"] == "AC-14.3-02")
    assert ac_fourteen_three_two["resultSemantics"]["expected"] == "PASS"

    fault = json.loads(outputs["_bmad-output/planning-artifacts/v9/story-contracts/16.1.json"])
    fault["finalRecord"]["summary"]["notRun"] = 1
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_schemas(
            ROOT,
            {"_bmad-output/planning-artifacts/v9/story-contracts/16.1.json": publisher.json_bytes(fault)},
        )
    assert error.value.code == "SCHEMA_VALIDATION_FAILED"


def test_story_slice_is_closed_candidate_bound_and_one_way_digest_ordered() -> None:
    """The v11 sidecar binds the base contract and amendment without self-reference."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)
    sidecar = json.loads(outputs[publisher.SLICE_PATH])
    base_path = "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json"
    base_contract = json.loads(outputs[base_path])
    epics = publisher.candidate_blob(ROOT, candidate, publisher.EPICS_PATH)
    architecture = publisher.candidate_blob(ROOT, candidate, publisher.ARCHITECTURE_PATH)
    _, v11_epic, _, _ = publisher.validate_authority_prefixes(epics, architecture)
    amendment = publisher.v11_story_slice_amendment(v11_epic)

    publisher.validate_story_slice(sidecar, outputs[base_path], v11_epic, candidate)
    assert sidecar["authority"] == {
        "epic": publisher.V11_EPIC_AUTHORITY,
        "architecture": publisher.V11_ARCHITECTURE_AUTHORITY,
        "planningCandidate": candidate,
        "authorityBundlePath": publisher.BUNDLE_PATH,
    }
    assert sidecar["baseStoryContract"]["sha256"] == hashlib.sha256(outputs[base_path]).hexdigest()
    assert sidecar["amendmentSectionSha256"] == hashlib.sha256(amendment.encode()).hexdigest()
    assert "bundleDigest" not in json.dumps(sidecar, sort_keys=True)
    assert sidecar["predecessors"] == ["6.2", "IR-0"]
    assert sidecar["writablePaths"] == list(publisher.SLICE_WRITABLE_PATHS)
    assert sidecar["readOnlyInputs"] == list(publisher.SLICE_READ_ONLY_INPUTS)
    assert base_contract["authority"]["epic"] == publisher.BASE_EPIC_AUTHORITY
    assert base_contract["authority"]["architecture"] == publisher.BASE_ARCHITECTURE_AUTHORITY
    assert len(base_contract["scenarios"]) == 6
    assert len(base_contract["finalRecord"]["paths"]) == 2
    assert hashlib.sha256((ROOT / publisher.SCHEMA_PATHS[0]).read_bytes()).hexdigest() == (
        publisher.FROZEN_STORY_CONTRACT_SCHEMA_DIGEST
    )

    bundle = json.loads(outputs[publisher.BUNDLE_PATH])
    sidecar_row = next(row for row in bundle["artifacts"] if row["path"] == publisher.SLICE_PATH)
    assert sidecar_row["sha256"] == hashlib.sha256(outputs[publisher.SLICE_PATH]).hexdigest()
    assert sidecar_row["role"] == "story-slice-authority"


def test_v11_canonical_markers_are_byte_pinned_and_single_amendment() -> None:
    """Current v11 semantics cannot drift behind hard-coded sidecar rendering."""

    candidate = published_candidate()
    epics = publisher.candidate_blob(ROOT, candidate, publisher.EPICS_PATH)
    architecture = publisher.candidate_blob(ROOT, candidate, publisher.ARCHITECTURE_PATH)
    v11_epic = publisher.marker_block(
        epics,
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:BEGIN",
        "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:END",
    )
    v11_architecture = publisher.marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V11:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V11:END",
    )
    assert len(v11_epic) == publisher.V11_EPIC_BLOCK_SIZE
    assert hashlib.sha256(v11_epic).hexdigest() == publisher.V11_EPIC_BLOCK_DIGEST
    assert len(v11_architecture) == publisher.V11_ARCHITECTURE_BLOCK_SIZE
    assert hashlib.sha256(v11_architecture).hexdigest() == publisher.V11_ARCHITECTURE_BLOCK_DIGEST
    assert v11_epic.decode().count(
        "### Story 7.1 V11 Schema-Checkpoint Amendment: Authorize A Non-Story Slice"
    ) == 1

    epic_fault = epics.replace(b"non-story execution slice", b"story execution slice", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_authority_prefixes(epic_fault, architecture)
    assert error.value.code == "V11_EPIC_AUTHORITY_DRIFT"

    architecture_fault = architecture.replace(b"There is no scoped exception state", b"There is one scoped exception state", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_authority_prefixes(epics, architecture_fault)
    assert error.value.code == "V11_ARCHITECTURE_AUTHORITY_DRIFT"


def test_v14_authorities_preserve_v13_and_pin_existing_checkpoint_heads() -> None:
    epics = (ROOT / publisher.EPICS_PATH).read_bytes()
    architecture = (ROOT / publisher.ARCHITECTURE_PATH).read_bytes()
    _, _, _, v14_epic = publisher.validate_authority_prefixes(epics, architecture)
    v14_architecture = publisher.marker_block(
        architecture,
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V14:BEGIN",
        "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V14:END",
    )
    assert len(v14_epic.encode()) == publisher.V14_EPIC_BLOCK_SIZE
    assert hashlib.sha256(v14_epic.encode()).hexdigest() == publisher.V14_EPIC_BLOCK_DIGEST
    assert len(v14_architecture) == publisher.V14_ARCHITECTURE_BLOCK_SIZE
    assert hashlib.sha256(v14_architecture).hexdigest() == publisher.V14_ARCHITECTURE_BLOCK_DIGEST
    assert hashlib.sha256((ROOT / publisher.CURRENT_PROOF_PATH).read_bytes()).hexdigest() == publisher.CURRENT_PROOF_DIGEST
    assert hashlib.sha256((ROOT / publisher.CURRENT_CANDIDATE_PATH).read_bytes()).hexdigest() == publisher.CURRENT_CANDIDATE_DIGEST

    v13_fault = architecture.replace(b"DC-9 (tier-migration strength)", b"DC-9 (tier-migration weakness)", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_authority_prefixes(epics, v13_fault)
    assert error.value.code == "V13_ARCHITECTURE_AUTHORITY_DRIFT"

    v14_fault = epics.replace(b"A4 \xe2\x86\x92 Story 16.1", b"A4 \xe2\x86\x92 Story 16.2", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_authority_prefixes(v14_fault, architecture)
    assert error.value.code == "V14_EPIC_AUTHORITY_DRIFT"


def test_v12_remediation_sidecar_is_closed_and_binds_exact_checkpoint_inventory() -> None:
    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)
    authority = json.loads(outputs[publisher.REMEDIATION_PATH])

    publisher.validate_remediation_authority(ROOT, candidate, authority)
    assert authority["checkpointId"] == "E6-REMEDIATION"
    assert authority["predecessors"] == ["PC-PUBLICATION"]
    assert authority["successor"] == "IR-0"
    assert [row["id"] for row in authority["actionInventory"]] == ["A1", "A2", "A3", "A4", "A5", "A6"]
    assert [row["checkpointOwned"] for row in authority["actionInventory"]] == [True, True, True, False, False, False]
    assert authority["activeRoutePaths"] == list(publisher.MECHANICAL_PATHS)
    assert authority["rootGitlinkPaths"] == list(publisher.ROOT_GITLINK_PATHS)
    assert authority["resultSemantics"] == {
        "states": ["PASS", "FAIL", "BLOCKED", "not-applicable"],
        "ledgerRequired": True,
        "skipsAllowed": False,
    }
    assert authority["completionEffect"] == {
        "ir0RerunAllowed": True,
        "holdLifted": False,
        "successorStarted": False,
        "releaseAuthorized": False,
    }

    mutation = json.loads(outputs[publisher.REMEDIATION_PATH])
    mutation["actionInventory"] = list(reversed(mutation["actionInventory"]))
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_remediation_authority(ROOT, candidate, mutation)
    assert error.value.code == "REMEDIATION_AUTHORITY_DRIFT"

    extra = json.loads(outputs[publisher.REMEDIATION_PATH])
    extra["unexpected"] = True
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_schemas(ROOT, {publisher.REMEDIATION_PATH: publisher.json_bytes(extra)})
    assert error.value.code == "SCHEMA_VALIDATION_FAILED"


def test_story_slice_and_checkpoint_graph_mutations_fail_closed() -> None:
    """Closed-field and exact-edge faults turn the v11 publication red."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)
    sidecar = json.loads(outputs[publisher.SLICE_PATH])
    graph = json.loads(outputs[publisher.GRAPH_PATH])
    contracts = {
        json.loads(content)["storyId"]: json.loads(content)
        for path, content in outputs.items()
        if "/story-contracts/" in path
    }
    _, v11_epic, _, _ = publisher.validate_authority_prefixes(
        publisher.candidate_blob(ROOT, candidate, publisher.EPICS_PATH),
        publisher.candidate_blob(ROOT, candidate, publisher.ARCHITECTURE_PATH),
    )
    base_contract = outputs["_bmad-output/planning-artifacts/v9/story-contracts/7.1.json"]

    extra_field = json.loads(outputs[publisher.SLICE_PATH])
    extra_field["unexpected"] = True
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_schemas(ROOT, {publisher.SLICE_PATH: publisher.json_bytes(extra_field)})
    assert error.value.code == "SCHEMA_VALIDATION_FAILED"

    mutations = (
        (("schemaVersion",), "hexalith.conversations.story-slice-authority.v2"),
        (("authority", "epic"), publisher.BASE_EPIC_AUTHORITY),
        (("authority", "architecture"), publisher.BASE_ARCHITECTURE_AUTHORITY),
        (("authority", "planningCandidate"), "f" * 40),
        (("authority", "authorityBundlePath"), "other-bundle.json"),
        (("baseStoryContract", "sha256"), "0" * 64),
        (("baseStoryContract", "epic"), publisher.EPIC_AUTHORITY),
        (("amendmentSectionSha256",), "1" * 64),
        (("predecessors",), ["6.2"]),
        (("holdRequirement", "effectiveState"), "ACTIVE"),
        (("holdRequirement", "recordPath"), "other-hold.json"),
        (("writablePaths",), list(reversed(publisher.SLICE_WRITABLE_PATHS))),
        (("readOnlyInputs",), list(publisher.SLICE_READ_ONLY_INPUTS[:-1])),
        (("prohibitedPaths",), [*publisher.SLICE_PROHIBITED_PATHS[:-1], {"match": "prefix", "path": "other/"}]),
        (("acceptance", "scenarioId"), "AC-7.1-02"),
        (("acceptance", "command"), "python3 -m pytest -q other.py"),
        (("acceptance", "result"), "FAIL"),
        (("acceptance", "passExitCodes"), [0, 1]),
        (("acceptance", "failExitCodes"), [1]),
        (("acceptance", "blockedExitCodes"), [2, 3]),
        (("completionEffect", "storyDoneAllowed"), True),
        (("completionEffect", "finalRecordProduced"), True),
        (("completionEffect", "successorUnlocked"), True),
        (("rollback", "boundary"), "Remove everything."),
    )
    for field_path, value in mutations:
        mutation = json.loads(outputs[publisher.SLICE_PATH])
        target = mutation
        for field in field_path[:-1]:
            target = target[field]
        target[field_path[-1]] = value
        with pytest.raises(publisher.PublicationError) as error:
            publisher.validate_story_slice(mutation, base_contract, v11_epic, candidate)
        assert error.value.code == "STORY_SLICE_AUTHORITY_DRIFT", field_path

    missing_edge = json.loads(outputs[publisher.GRAPH_PATH])
    missing_edge["edges"] = [
        edge
        for edge in missing_edge["edges"]
        if not (edge["from"] == publisher.SLICE_ID and edge["to"] == "7.1")
    ]
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_slice_graph_parity(missing_edge, contracts, sidecar)
    assert error.value.code == "CHECKPOINT_GRAPH_DRIFT"

    arbitrary_predecessor = json.loads(outputs[publisher.GRAPH_PATH])
    next(node for node in arbitrary_predecessor["nodes"] if node["id"] == "7.1")["predecessors"].append("7.2")
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_slice_graph_parity(arbitrary_predecessor, contracts, sidecar)
    assert error.value.code == "CHECKPOINT_GRAPH_DRIFT"

    arbitrary_node = json.loads(outputs[publisher.GRAPH_PATH])
    next(node for node in arbitrary_node["nodes"] if node["id"] == "7.3")["id"] = "ARBITRARY"
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_slice_graph_parity(arbitrary_node, contracts, sidecar)
    assert error.value.code == "CHECKPOINT_GRAPH_DRIFT"
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_schemas(ROOT, {publisher.GRAPH_PATH: publisher.json_bytes(arbitrary_node)})
    assert error.value.code == "SCHEMA_VALIDATION_FAILED"


def test_epic_six_retrospective_faults_fail_and_restore_byte_identically() -> None:
    """In-memory sprint faults fail while tracked evidence remains byte-identical."""

    path = ROOT / publisher.SPRINT_PATH
    before = path.read_bytes()
    source = before.decode()
    contracts = {
        document["storyId"]: document
        for contract_path in (ROOT / "_bmad-output/planning-artifacts/v9/story-contracts").glob("*.json")
        for document in [json.loads(contract_path.read_text(encoding="utf-8"))]
    }
    matches = list(
        publisher.re.finditer(
            r'^  - id: "(epic-6-retro-item-[^"]+)"\n.*?(?=^  - |\Z)',
            source[source.index("action_items:\n") :],
            publisher.re.MULTILINE | publisher.re.DOTALL,
        )
    )
    blocks = [match.group(0) for match in matches]
    mutations = (
        (
            source.replace("  epic-6-retrospective: done\n", "  epic-6-retrospective: optional\n", 1),
            "EPIC_6_RETROSPECTIVE_DRIFT",
        ),
        (source.replace(blocks[0], "", 1), "EPIC_6_RETROSPECTIVE_DRIFT"),
        (source.replace(blocks[0], blocks[0] + blocks[0], 1), "EPIC_6_RETROSPECTIVE_DRIFT"),
        (source.replace(blocks[0] + blocks[1], blocks[1] + blocks[0], 1), "EPIC_6_RETROSPECTIVE_DRIFT"),
        (
            source.replace("Produce an additive Epic 6", "Produce a subtractive Epic 6", 1),
            "EPIC_6_RETROSPECTIVE_DRIFT",
        ),
        (
            source.replace(blocks[0], blocks[0].replace("    status: done\n", "    status: open\n", 1), 1),
            "EPIC_6_RETROSPECTIVE_DRIFT",
        ),
        (
            source.replace(blocks[1], blocks[1].replace("    status: open\n", "    status: done\n", 1), 1),
            "EPIC_6_RETROSPECTIVE_DRIFT",
        ),
        (
            source.replace(
                "last_updated: 2026-08-19\n",
                "last_updated: 2026-08-19\nlast_updated: 2026-08-19\n",
                1,
            ),
            "SPRINT_PROJECTION_DRIFT",
        ),
        (
            source.replace(
                "  7-1-define-the-final-record-schema-and-deterministic-generator-core: backlog\n",
                "  7-1-define-the-final-record-schema-and-deterministic-generator-core: backlog\n"
                "  7.1-SCHEMAS: backlog\n",
                1,
            ),
            "SPRINT_PROJECTION_DRIFT",
        ),
    )
    for mutation, expected_code in mutations:
        with pytest.raises(publisher.PublicationError) as error:
            publisher.render_sprint(mutation.encode("utf-8"), contracts)
        assert error.value.code == expected_code
        assert path.read_bytes() == before

    rendered = publisher.render_outputs(ROOT, published_candidate())[publisher.SPRINT_PATH].decode()
    publisher.validate_sprint_structure(rendered)
    publisher.validate_epic_6_retrospective(rendered)
    assert "last_updated: 2026-08-19" in rendered
    assert len(publisher.re.findall(r"^last_updated:", rendered, publisher.re.MULTILINE)) == 1
    assert "  epic-6-retrospective: done" in rendered
    development = rendered[rendered.index("development_status:\n") : rendered.index("\naction_items:\n")]
    assert publisher.SLICE_ID not in development
    assert path.read_bytes() == before


def test_bundle_schema_rejects_invalid_hold_or_gitlink_scope() -> None:
    """Schema validation rejects a lifted hold and duplicate gitlink identities."""

    schema = json.loads((ROOT / publisher.SCHEMA_PATHS[4]).read_text(encoding="utf-8"))
    bundle = json.loads((ROOT / publisher.BUNDLE_PATH).read_text(encoding="utf-8"))
    bundle["implementationHold"] = "LIFTED"
    with pytest.raises(jsonschema.ValidationError):
        jsonschema.Draft202012Validator(schema).validate(bundle)

    bundle = json.loads((ROOT / publisher.BUNDLE_PATH).read_text(encoding="utf-8"))
    bundle["gitlinks"][1]["path"] = bundle["gitlinks"][0]["path"]
    bundle["gitlinks"][1]["commit"] = "f" * 40
    with pytest.raises(jsonschema.ValidationError):
        jsonschema.Draft202012Validator(schema).validate(bundle)


def test_explicit_check_candidate_is_respected_and_mismatch_fails(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """An explicit check candidate is never replaced by the bundle candidate."""

    bundle_candidate = "a" * 40
    requested_candidate = "b" * 40
    bundle = tmp_path / publisher.BUNDLE_PATH
    bundle.parent.mkdir(parents=True)
    bundle.write_text(json.dumps({"planningCandidate": bundle_candidate}), encoding="utf-8")
    revisions: list[str] = []

    def fake_git(root: Path, *arguments: str) -> bytes:
        revisions.append(arguments[-1])
        revision = arguments[-1].removesuffix("^{commit}")
        return f"{revision}\n".encode()

    monkeypatch.setattr(publisher, "git", fake_git)
    assert publisher.resolve_candidate(tmp_path, requested_candidate, check=True) == requested_candidate
    assert revisions[-1] == f"{requested_candidate}^{{commit}}"
    assert publisher.resolve_candidate(tmp_path, None, check=True) == bundle_candidate

    checked_root = tmp_path / "checked"
    old_outputs = dummy_outputs(bundle_candidate)
    new_outputs = dummy_outputs(requested_candidate)
    publisher.publish(checked_root, old_outputs, check=False)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(checked_root, new_outputs, check=True)
    assert error.value.code == "OUTPUT_DRIFT"


def test_ux_projection_rebinds_candidate_and_rejects_order_or_identity_faults() -> None:
    """UX projection carries the requested PC and exact ordered 52/28 identities."""

    source = (ROOT / publisher.UX_MAP_PATH).read_bytes()
    candidate = "f" * 40
    rendered = publisher.render_ux_map(source, candidate).decode()
    assert rendered.count(f"planningCandidate: {candidate}") == 1
    assert publisher.re.findall(r"^\| (UX-DR\d+) \|", rendered, publisher.re.MULTILINE) == list(
        publisher.EXPECTED_UX_DECISION_IDS
    )
    assert publisher.re.findall(
        r"^\| (AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}) \|",
        rendered,
        publisher.re.MULTILINE,
    ) == list(publisher.EXPECTED_UX_ACCEPTANCE_IDS)

    duplicate = source.replace(b"| UX-DR52 |", b"| UX-DR51 |", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.render_ux_map(duplicate, candidate)
    assert error.value.code == "UX_PARITY_DRIFT"

    swapped = source.replace(b"AC-SAFE-001", b"AC-SAFE-TMP", 1)
    swapped = swapped.replace(b"AC-SAFE-002", b"AC-SAFE-001", 1)
    swapped = swapped.replace(b"AC-SAFE-TMP", b"AC-SAFE-002", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.render_ux_map(swapped, candidate)
    assert error.value.code == "UX_PARITY_DRIFT"


def test_graph_is_ordinal_and_every_successor_is_downstream_of_ir_zero() -> None:
    """IR-0 gates both entry branches without being run or assigned a story identity."""

    outputs = publisher.render_outputs(ROOT, published_candidate())
    graph = json.loads(outputs[publisher.GRAPH_PATH])
    nodes = {node["id"]: node["predecessors"] for node in graph["nodes"]}
    assert len(graph["nodes"]) == 38
    assert len(graph["edges"]) == 61
    assert set(nodes) == set(publisher.EXPECTED_STORY_IDS) | {
        "6.2",
        "PC-PUBLICATION",
        "IR-0",
        "E6-REMEDIATION",
        "E6-CURRENT-PROOF",
        "E6-CURRENT-CANDIDATE",
        "RG-15",
        publisher.SLICE_ID,
    }
    assert nodes["E6-REMEDIATION"] == ["PC-PUBLICATION"]
    assert nodes["E6-CURRENT-PROOF"] == ["E6-REMEDIATION"]
    assert nodes["E6-CURRENT-CANDIDATE"] == ["E6-CURRENT-PROOF", "E6-REMEDIATION"]
    assert nodes["IR-0"] == ["E6-REMEDIATION"]
    assert nodes[publisher.SLICE_ID] == ["6.2", "IR-0"]
    assert nodes["7.1"] == ["6.2", publisher.SLICE_ID, "IR-0"]
    assert nodes["7.2"] == ["7.1"]
    assert nodes["12.1"] == ["16.3", "6.2", "IR-0"]
    assert nodes["16.1"] == ["7.4", "IR-0"]
    assert nodes["16.2"] == ["16.1"]
    assert nodes["16.3"] == ["16.2"]
    assert all(predecessors == sorted(predecessors) for predecessors in nodes.values())
    assert graph["edges"] == sorted(graph["edges"], key=lambda edge: (edge["from"], edge["to"]))
    assert len([node for node in graph["nodes"] if node["kind"] == "checkpoint"]) == 4

    for story_id in publisher.EXPECTED_STORY_IDS:
        pending = list(nodes[story_id])
        ancestors: set[str] = set()
        while pending:
            predecessor = pending.pop()
            if predecessor not in ancestors:
                ancestors.add(predecessor)
                pending.extend(nodes[predecessor])
        assert "IR-0" in ancestors, story_id


def test_current_view_projects_exact_checkpoint_and_story_dependencies() -> None:
    """The view has one exact checkpoint row and preserves Story 7.1/7.2 order."""

    outputs = publisher.render_outputs(ROOT, published_candidate())
    view = outputs[publisher.VIEW_V2_PATH].decode("utf-8")
    contracts = {
        json.loads(content)["storyId"]: json.loads(content)
        for path, content in outputs.items()
        if "/story-contracts/" in path
    }
    publisher.validate_current_view(view, contracts)
    assert view.count(
        "| 7.1-SCHEMAS | checkpoint | Closed Story 7.1 schema contracts | 6.2, IR-0 | 1 |"
    ) == 1
    assert view.count(
        "| E6-REMEDIATION | checkpoint | Complete Epic 6 A1-A3 before independent IR-0 | PC-PUBLICATION | 3 |"
    ) == 1
    assert view.count(
        "| E6-CURRENT-PROOF | checkpoint | Accepted current completion proof | E6-REMEDIATION | 1 |"
    ) == 1
    assert view.count(
        "| E6-CURRENT-CANDIDATE | checkpoint | Current candidate authority | E6-CURRENT-PROOF, E6-REMEDIATION | 1 |"
    ) == 1
    assert len(publisher.re.findall(r"^\| 16\.[1-3] \| story \|", view, publisher.re.MULTILINE)) == 3
    assert publisher.re.findall(r"^\| 7\.1 \|.*$", view, publisher.re.MULTILINE) == [
        "| 7.1 | story | Define the final-record schema and deterministic generator core | "
        "6.2, 7.1-SCHEMAS, IR-0 | 6 |"
    ]
    assert publisher.re.findall(r"^\| 7\.2 \|.*$", view, publisher.re.MULTILINE) == [
        "| 7.2 | story | Derive test, path, candidate, submodule, and gitlink facts | 7.1 | 11 |"
    ]

    mutations = (
        view.replace("6.2, IR-0 | 1 |", "6.2 | 1 |", 1),
        view.replace("6.2, 7.1-SCHEMAS, IR-0 | 6 |", "6.2, 7.1-SCHEMAS | 6 |", 1),
        view.replace("| 7.2 | story |", "| 7.2 | story |", 1).replace("facts | 7.1 | 11 |", "facts | 7.1-SCHEMAS | 11 |", 1),
    )
    for mutation in mutations:
        with pytest.raises(publisher.PublicationError) as error:
            publisher.validate_current_view(mutation, contracts)
        assert error.value.code == "CURRENT_VIEW_DRIFT"


def test_supersession_projects_the_exact_full_ledger_and_denominators() -> None:
    """The complete two-table ledger and preservation denominators remain non-vacuous."""

    supersession = json.loads((ROOT / publisher.SUPERSESSION_PATH).read_text(encoding="utf-8"))
    assert len(supersession["storyDispositions"]) == 9
    assert [
        row["successorEpic"]
        for row in supersession["storyDispositions"]
        if row["sourceStory"] == "6.10"
    ] == [10]
    ledger = supersession["obligationLedger"]
    assert ledger["inventoryId"] == publisher.OBLIGATION_LEDGER_ID
    assert ledger["sha256"] == publisher.OBLIGATION_LEDGER_DIGEST
    assert ledger["acceptanceCriteriaRows"] == 66
    assert ledger["totalRows"] == 156
    assert len(ledger["rows"]) == 156
    assert [row["ordinal"] for row in ledger["rows"]] == list(range(1, 157))
    assert len({row["sourceId"] for row in ledger["rows"]}) == 156
    ledger_payload = "".join(
        f"{row['sourceId']}|{row['canonicalBinding']}\n"
        for row in ledger["rows"]
    ).encode()
    assert hashlib.sha256(ledger_payload).hexdigest() == ledger["sha256"]
    story_ten = [row for row in ledger["rows"] if row["sourceId"].startswith("V8-6.10-AC")]
    assert len(story_ten) == 10
    assert "AC-10.4-09" in next(row for row in story_ten if row["sourceId"] == "V8-6.10-AC9")[
        "effectiveBindings"
    ]
    assert supersession["preservationDenominators"] == {
        "functionalRequirements": {"required": 124, "mapped": 124},
        "nonFunctionalRequirements": {"required": 77, "mapped": 77},
        "uxDecisions": {"required": 52, "mapped": 52},
        "uxAcceptanceCriteria": {"required": 28, "mapped": 28},
    }


def test_route_parity_aliases_and_inventory_order_have_stable_failures(monkeypatch: pytest.MonkeyPatch) -> None:
    """Route, alias, and inventory tuple mutations fail with stable codes."""

    original = publisher.candidate_blob

    def parity_fault(root: Path, candidate: str, path: str) -> bytes:
        content = original(root, candidate, path)
        return content + b"\nparity fault\n" if path == publisher.MECHANICAL_PATHS[6] else content

    monkeypatch.setattr(publisher, "candidate_blob", parity_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_route_topology(ROOT, published_candidate())
    assert error.value.code == "EVIDENCE_WORKFLOW_PARITY_DRIFT"

    def alias_fault(root: Path, candidate: str, path: str) -> bytes:
        content = original(root, candidate, path)
        if path == ".agents/skills/bmad-dev-auto/SKILL.md":
            return content.replace(b"invoke `bmad-build-auto` exactly once", b"invoke `bmad-build` exactly once", 1)
        return content

    monkeypatch.setattr(publisher, "candidate_blob", alias_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_route_topology(ROOT, published_candidate())
    assert error.value.code == "EVIDENCE_ALIAS_ROUTE_INVALID"

    monkeypatch.setattr(publisher, "candidate_blob", original)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.render_inventory(
            ROOT,
            published_candidate(),
            "V9-EVIDENCE-GUIDANCE-v2",
            tuple(reversed(publisher.GUIDANCE_PATHS)),
        )
    assert error.value.code == "INVENTORY_ORDER_DRIFT"


def exercise_guidance_mutation(
    relative_path: str,
    mutation: Callable[[bytes], bytes | None],
    expected_code: str,
) -> None:
    """Apply one real-file fixture and prove exact byte restoration."""

    path = ROOT / relative_path
    before = path.read_bytes()
    try:
        changed = mutation(before)
        if changed is None:
            path.unlink()
        else:
            path.write_bytes(changed)
        with pytest.raises(publisher.PublicationError) as error:
            publisher.render_resolved_customization(ROOT, published_candidate())
        assert error.value.code == expected_code
    finally:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(before)
    assert path.read_bytes() == before


def test_guidance_fault_fixtures_fail_and_restore_byte_identically() -> None:
    """The four AC-10.4-09 guidance faults are red and byte-restoring."""

    exercise_guidance_mutation(
        "_bmad/custom/bmad-build.toml",
        lambda _: None,
        "EVIDENCE_GUIDANCE_NOT_USED",
    )
    exercise_guidance_mutation(
        publisher.RUNBOOK_PATH,
        lambda content: content.replace(b"exact set equality", b"manifest containment", 1),
        "EVIDENCE_GUIDANCE_DRIFT",
    )
    exercise_guidance_mutation(
        publisher.RUNBOOK_PATH,
        lambda content: content.replace(
            b"Recompute every declared source hash",
            b"Trust every declared source hash",
            1,
        ),
        "EVIDENCE_GUIDANCE_DRIFT",
    )
    exercise_guidance_mutation(
        "_bmad/custom/bmad-review.toml",
        lambda content: content.replace(
            b"docs/runbooks/evidence-boundary-validation.md",
            b"docs/runbooks/redirected.md",
            1,
        ),
        "EVIDENCE_GUIDANCE_NOT_USED",
    )


def test_unbound_user_customization_layer_fails_closed() -> None:
    """A skill-specific user layer cannot alter candidate-bound resolved guidance."""

    user_layer = ROOT / "_bmad/custom/bmad-build.user.toml"
    assert not user_layer.exists()
    try:
        user_layer.write_text("[workflow]\npersistent_facts = []\n", encoding="utf-8")
        with pytest.raises(publisher.PublicationError) as error:
            publisher.render_resolved_customization(ROOT, published_candidate())
        assert error.value.code == "EVIDENCE_CUSTOMIZATION_RESOLUTION_FAILED"
    finally:
        user_layer.unlink(missing_ok=True)
    assert not user_layer.exists()


def test_dirty_worktree_scope_and_managed_namespaces_are_exact(tmp_path: Path) -> None:
    """Publication preserves unrelated dirt and rejects stale managed artifacts before writing."""

    unrelated_path = tmp_path / "_bmad-output/implementation-artifacts/epic-6-context.md"
    unrelated_path.parent.mkdir(parents=True)
    unrelated_bytes = b"pre-existing unrelated worktree bytes\n"
    unrelated_path.write_bytes(unrelated_bytes)
    outputs = dummy_outputs("generated")
    publisher.publish(tmp_path, outputs, check=False)
    assert unrelated_path.read_bytes() == unrelated_bytes
    actual_files = {
        path.relative_to(tmp_path).as_posix()
        for path in tmp_path.rglob("*")
        if path.is_file()
    }
    assert actual_files == set(outputs) | {"_bmad-output/implementation-artifacts/epic-6-context.md"}

    unexpected_outputs = {**outputs, "unexpected-publication-path.json": b"unexpected\n"}
    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(tmp_path, unexpected_outputs, check=False)
    assert error.value.code == "PUBLICATION_SCOPE_DRIFT"
    assert unrelated_path.read_bytes() == unrelated_bytes

    for stale_path in (
        "_bmad-output/planning-artifacts/v9/stale.json",
        "_bmad-output/planning-artifacts/v9/inventories/stale.json",
        "_bmad-output/planning-artifacts/v9/resolved-customization/stale.json",
        "_bmad-output/planning-artifacts/v9/story-contracts/stale.json",
        "_bmad-output/planning-artifacts/v9-stale-v1.json",
        "_bmad-output/planning-artifacts/v11-unexpected-authority.json",
    ):
        stale = tmp_path / stale_path
        stale.parent.mkdir(parents=True, exist_ok=True)
        stale.write_bytes(b"stale\n")
        with pytest.raises(publisher.PublicationError) as error:
            publisher.publish(tmp_path, outputs, check=False)
        assert error.value.code == "PUBLICATION_SCOPE_DRIFT"
        stale.unlink()


def test_mid_commit_failure_restores_the_complete_managed_set(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A mid-replacement filesystem failure restores every old byte and removes staging."""

    before_outputs = dummy_outputs("before")
    publisher.publish(tmp_path, before_outputs, check=False)
    original_replace = publisher.os.replace
    calls = 0

    def fail_once(source: Path, destination: Path) -> None:
        nonlocal calls
        calls += 1
        if calls == 5:
            raise OSError("injected mid-commit failure")
        original_replace(source, destination)

    monkeypatch.setattr(publisher.os, "replace", fail_once)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(tmp_path, dummy_outputs("after"), check=False)
    assert error.value.code == "PUBLICATION_WRITE_FAILED"
    for path, content in before_outputs.items():
        assert (tmp_path / path).read_bytes() == content
    assert not any(path.name.startswith(".v9-publication.") for path in tmp_path.iterdir())


def test_exact_gitlink_scope_and_faults(monkeypatch: pytest.MonkeyPatch) -> None:
    """Bundle gitlinks equal the ten root declarations and raw mode-160000 tree entries."""

    candidate = published_candidate()
    assert [row["path"] for row in publisher.gitlinks(ROOT, candidate)] == list(publisher.ROOT_GITLINK_PATHS)
    original_blob = publisher.candidate_blob

    def declaration_fault(root: Path, revision: str, path: str) -> bytes:
        content = original_blob(root, revision, path)
        if path == ".gitmodules":
            return content.replace(
                b"path = references/Hexalith.Tenants",
                b"path = references/Hexalith.NotTenants",
                1,
            )
        return content

    monkeypatch.setattr(publisher, "candidate_blob", declaration_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.gitlinks(ROOT, candidate)
    assert error.value.code == "GITLINK_SCOPE_MISMATCH"

    monkeypatch.setattr(publisher, "candidate_blob", original_blob)
    original_git = publisher.git
    raw = original_git(ROOT, "ls-tree", "-rz", candidate)

    def raw_fault(root: Path, *arguments: str) -> bytes:
        if arguments[:2] == ("ls-tree", "-rz"):
            entries = raw.split(b"\0")
            return b"\0".join(entry for entry in entries if b"references/Hexalith.Tenants" not in entry)
        return original_git(root, *arguments)

    monkeypatch.setattr(publisher, "git", raw_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.gitlinks(ROOT, candidate)
    assert error.value.code == "GITLINK_SCOPE_MISMATCH"


def test_stable_input_failures_replace_tracebacks(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """Git, marker, and schema read faults produce stable publication blockers."""

    def timeout(*args, **kwargs):
        raise subprocess.TimeoutExpired("git", 30)

    monkeypatch.setattr(publisher.subprocess, "run", timeout)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.git(ROOT, "rev-parse", "HEAD")
    assert error.value.code == "CANDIDATE_GIT_UNAVAILABLE"

    monkeypatch.setattr(
        publisher.subprocess,
        "run",
        lambda *args, **kwargs: (_ for _ in ()).throw(OSError("spawn")),
    )
    with pytest.raises(publisher.PublicationError) as error:
        publisher.git(ROOT, "rev-parse", "HEAD")
    assert error.value.code == "CANDIDATE_GIT_UNAVAILABLE"

    malformed = b"<!-- BEGIN --><!-- END"
    with pytest.raises(publisher.PublicationError) as error:
        publisher.marker_block(malformed, "<!-- BEGIN", "<!-- END")
    assert error.value.code == "AUTHORITY_MARKER_INVALID"

    original_read_text = Path.read_text

    def malformed_schema(path: Path, *args, **kwargs) -> str:
        if path == ROOT / publisher.SCHEMA_PATHS[0]:
            return "{"
        return original_read_text(path, *args, **kwargs)

    monkeypatch.setattr(Path, "read_text", malformed_schema)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_schemas(ROOT, {})
    assert error.value.code == "SCHEMA_VALIDATION_FAILED"

    monkeypatch.setattr(Path, "read_text", original_read_text)
    original_read_bytes = Path.read_bytes

    def failed_candidate_read(path: Path) -> bytes:
        if path == ROOT / publisher.EPICS_PATH:
            raise OSError("read")
        return original_read_bytes(path)

    monkeypatch.setattr(Path, "read_bytes", failed_candidate_read)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.require_candidate_bytes(ROOT, published_candidate(), (publisher.EPICS_PATH,))
    assert error.value.code == "CANDIDATE_SOURCE_READ_FAILED"


def test_inventory_outputs_are_exact_ordinal_tuples() -> None:
    """Workflow, guidance, and reader inventories preserve their frozen ordered tuples."""

    cases = (
        ("evidence-workflows-v2.json", publisher.MECHANICAL_PATHS),
        ("evidence-workflows-v3.json", publisher.MECHANICAL_PATHS),
        ("evidence-guidance-v2.json", publisher.GUIDANCE_PATHS),
        ("evidence-readers-v1.json", publisher.READER_PATHS),
    )
    for filename, expected in cases:
        inventory = json.loads((ROOT / f"_bmad-output/planning-artifacts/v9/inventories/{filename}").read_text())
        rows = inventory["rows"]
        assert [row["ordinal"] for row in rows] == list(range(1, len(expected) + 1))
        assert [row["path"] for row in rows] == list(expected)


def test_publication_preserves_the_unrun_independent_assessment_boundary() -> None:
    """Generated planning state must neither run nor predetermine IR-0."""

    outputs = publisher.render_outputs(ROOT, published_candidate())
    bundle = json.loads(outputs[publisher.BUNDLE_PATH])
    slice_authority = json.loads(outputs[publisher.SLICE_PATH])
    view = outputs[publisher.VIEW_V2_PATH].decode("utf-8")
    sprint = outputs[publisher.SPRINT_PATH].decode("utf-8")
    assert bundle["implementationHold"] == "ACTIVE"
    assert bundle["epic5ActionA5"] == "open"
    assert "IR-0: not run by this publication." in view
    assert "does not implement a story, run IR-0, lift the" in " ".join(view.split())
    assert "IR-0 was not run" in sprint
    assert sprint.count("# V14 PLANNING PUBLICATION:") == 1
    assert "A2-A6 remain open" in sprint
    assert "Epic 16 remain backlog" in sprint
    assert not any("ir-0" in path.lower() for path in outputs)
    assert not any("implementation-readiness-report" in row["path"].lower() for row in bundle["artifacts"])
    assert "READY" not in view
    assert "NOT READY" not in view
    assert "LIFTED" not in json.dumps(bundle)
    assert slice_authority["holdRequirement"]["effectiveState"] == "LIFTED"
    assert slice_authority["completionEffect"] == {
        "storyDoneAllowed": False,
        "finalRecordProduced": False,
        "successorUnlocked": False,
    }


def test_preflight_directly_selects_all_three_planning_validator_classes() -> None:
    """The CI filter must name real classes rather than fail-open aliases."""
    workflow = (ROOT / ".github/workflows/planning-authority-preflight.yml").read_text(
        encoding="utf-8"
    )
    assert (
        'FullyQualifiedName~ArchitecturePlanningAuthorityValidationTest|'
        'FullyQualifiedName~PlanningAuthorityV9ValidationTest|'
        'FullyQualifiedName~PlanningAuthorityV8ValidationTest'
    ) in workflow
    assert "FullyQualifiedName~V9PlanningAuthorityValidationTest" not in workflow
    assert "FullyQualifiedName~V8PlanningAuthorityValidationTest" not in workflow
