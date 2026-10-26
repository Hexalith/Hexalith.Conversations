#!/usr/bin/env python3
"""Read-only consistency checks for the Epic 16 draft; never authorize execution.

Exit 0 means the preparation package is internally consistent, 1 means invalid,
and 2 means required inputs are unavailable. Entry readiness always stays BLOCKED.
No acceptance commands, authority resolver, publishers or runtime are invoked.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

import jsonschema

from evidence_boundary_current import digest
from generate_story_record import GateError, contained_file, v2_parse_json

BASE = "_bmad-output/planning-artifacts/epic-16-prerequisites-v1"
ENVELOPE = "_bmad-output/planning-artifacts/production-operational-envelope-v1.md"
SCHEMA = "_bmad/schemas/epic-16-prerequisites-v1.schema.json"
BASELINE = "60f18214d7a4565d28096628af95d867ddd5e874"
STORIES = ("16.1", "16.2", "16.3")
ADS = ["AD-5", "AD-6", "AD-7", "AD-8"]
AUTHORITY = dict(implementationHold="ACTIVE", executionAllowed=False,
                 ownerApprovalClaimed=False, releaseAuthorized=False, pushAuthorized=False)
PLATFORM_DECISION = dict(owner="Hexalith.Platform",
                         platformRevision="10c50afc3cab41506fbb260e9fbc48a1804e0b61",
                         selectedBy="user", instruction="stick to Hexalith.Platform",
                         productionQualificationClaimed=False)
SOURCE_PINS = {
    "_bmad-output/planning-artifacts/architecture.md": "948d9c98d99d2453a1ff3904f4f05392b3a2959e379c5fd6e466dc20ed1f05be",
    "_bmad-output/planning-artifacts/epics.md": "e6411339d2db80770f7aa923b43caf9c9fe8c416fdf9c5f94e07cddb3d3ca6e7",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md": "7098e7355522f503e136dc8c5e84ce5c873cb5bad08b92aa54495811c54853b6",
    "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18.md": "3e4cacb752c8e962691e391d6634d136274adbe2dfa4daea35f0d443ec455ddc",
    "_bmad-output/planning-artifacts/v9-execution-graph-v1.json": "989cea64a9f8bdcee5d69909d2c3f9662722e27ba19ba3d80e129fcab54c3747",
    "_bmad-output/planning-artifacts/v9/story-contracts/16.1.json": "a533959de8227a0fe31d152f3578e63cd10ba129b51506693290b05f7fbfe1d7",
    "_bmad-output/planning-artifacts/v9/story-contracts/16.2.json": "09e8462c84dfd909df652993b4bb83ca5b65dd1bac14ced571994ae534ec97ab",
    "_bmad-output/planning-artifacts/v9/story-contracts/16.3.json": "4702547ebc24970cddfe034a2ab3f3b74babf3b97b16fd7e55787e0c311cafd8",
    "_bmad-output/planning-artifacts/implementation-hold-v1.json": "2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12",
    "_bmad-output/implementation-artifacts/sprint-status.yaml": "df1a0bbae47828c76d678db58c592e33000e82ce7956b460a32ce416e9a5df65",
    "docs/runbooks/current-change-validation.md": "54cb71749300c6f4188999170b7c825fd97f181f2f4902b94c6245b2af2ce9d7",
    ".gitmodules": "f772e17bced54e00841b7e84c2e9e4e1624387883c129d2a851316cacab38987",
    "references/Hexalith.Platform/README.md": "1fbfdb11a8a74bcdb0ce5e93366da4694bb7429d81710e7a05a56938f3577d36",
    "references/Hexalith.Platform/docs/domain-module-enrollment.md": "ecf10bc1896a3827be20bee7cf1defb56025b5ebf25151a3b82e6472fe9bb34c",
    "references/Hexalith.Platform/hexalith-platform.json": "a2bef51e9bb0f11ccc6b49757a9cf28a7d71692ed588448a73b12f0a3d0db103",
    "references/Hexalith.Platform/.config/dotnet-tools.json": "9e21fa262e48d20f4a29b9de40c6544c95bd14e2fd9e624f8d23ab73d419e25a",
    "references/Hexalith.Platform/_bmad-output/specs/spec-platform/SPEC.md": "7afec7574a99a72efb94b150e6520a09d56faea372d13e5a6d841040fb30e8d4",
    "references/Hexalith.Platform/_bmad-output/planning-artifacts/architecture/architecture-platform-2026-09-27/ARCHITECTURE-SPINE.md": "cc95a53ab85becee18f924cc9a1a915a2a05fcce8caed3a3d46643ee1051e936",
    "references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformDaprBindings.cs": "d860b5f140ca605aeb530074b9e388dffd7ffbf94603abdc77addcd03708ee9c",
    "references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformAppModel.cs": "491a63452a4bb362a4bb3c7c5b5160647d90eb8e293e04d8c1cc3602b0c49216"
}
LEGACY_IDS = {
    "16.1": ["E6-A4-DURABLE-TENANT-ACCESS", "E6-A4-FRESHNESS-GAP-DETECTION",
             "E6-A4-RESTART-CONVERGENCE", "E6-A4-MULTI-REPLICA-CONVERGENCE",
             "E6-A4-SINGLE-REPLICA-WARNING-REMOVAL-GATE"],
    "16.2": ["E6-A5-EVENT-DERIVED-TIMESTAMPS", "E6-A5-EXACT-REPLAY",
             "E6-A5-MISSING-INDEX-SEMANTICS", "E6-A5-DERIVED-STATE-DELETION"],
    "16.3": ["E6-A6-ENDPOINT-READINESS-DIAGNOSTIC", "E6-A6-DAPR-PORT-COLLISION-DIAGNOSTIC",
             "E6-A6-LIVE-TERMINAL-RECONCILIATION", "E6-A6-DURABLE-RETRY-CLEARANCE"],
}
NEW_IDS = {
    "AD5-ENVELOPE-OWNERSHIP", "AD5-ENVIRONMENT-PROVIDER-PARITY",
    "AD5-DEPENDENCIES-HEALTH-TELEMETRY", "AD5-RECOVERY-RUNBOOKS-SCALING-WAIVERS",
    "AD6-PLATFORM-TENANT-OWNERSHIP", "AD6-ATOMIC-SEQUENCE-FINGERPRINT",
    "AD6-DURABLE-GAP-HORIZON", "AD6-ACKNOWLEDGEMENT-RETRY",
    "AD6-FENCED-REPLAY-RECOVERY", "AD6-CONCURRENCY-FAULT-MATRIX",
    "AD7-SOLE-CONTROL-INDEX-CAS", "AD7-LIFECYCLE-GENERATION-ERASE",
    "AD7-AUTHORITATIVE-REBUILD-CATCHUP", "AD7-SAFE-QUERY-MAPPINGS",
    "AD8-IDENTITY-PAYLOAD-TIME", "AD8-DUPLICATE-FULL-BINDING",
    "AD8-REPLAY-OBSERVATION-SEPARATION", "AD8-CANONICAL-REPLAY-BYTES",
    "ARCH-APPHOST-OWNERSHIP-DECISION",
}
ARTIFACTS = sorted([ENVELOPE, SCHEMA, f"{BASE}/epic.md", f"{BASE}/inventories.json",
                    f"{BASE}/execution-graph.json", *(f"{BASE}/story-contracts/{s}.json" for s in STORIES)])
DRAFT_PINS = {
    ENVELOPE: "0114ba16bde6086cc30aa45e7eaafa8b7a5330fea4bfecfbf355fafcbbf67db3",
    SCHEMA: "b7095d7191a96dcefb5a0c333958e5c7eccbd7fa1fae9cb8d62ea2ec8bcad497",
    f"{BASE}/epic.md": "b0b110461e81457a12b35bc8515ad691d26a926b206189495241f7f52a50097c",
    f"{BASE}/story-contracts/16.1.json": "3e44d0498d231633a12ee122d72535c178870a1601ab8c552c815566fe257e08",
    f"{BASE}/story-contracts/16.2.json": "a21c8c817e5126343110a5e6730883b5cf32738610b3f552ef6cdbbfe605a965",
    f"{BASE}/story-contracts/16.3.json": "5eeccfb8427e4ebccc1955f965d00e3c5655834a3c3005443c970be2dd99509a",
}
DECISION_IDS = ["operational-provider-and-scope", "environment-versions-and-parity",
                "accountable-operators-and-identity", "recovery-targets-and-restore-drill",
                "capacity-limits-and-waivers", "apphost-exemption-or-relocation",
                "successor-adoption-and-scoped-execution"]


def verify(repository: Path) -> dict:
    """Validate fixed draft inputs and produce a non-authorizing assertion ledger."""
    report = dict(schemaVersion="hexalith.conversations.epic16-prerequisite-check.v1",
                  validationScope="draft-consistency-only", result="PASS", entryReadiness="BLOCKED",
                  authority=dict(AUTHORITY), infrastructureDecision=dict(PLATFORM_DECISION),
                  assertionLedger=[], blockers=[], pendingDecisions=[])
    repository = repository.resolve()

    def check(identity: str, passed: bool, code: str) -> None:
        report["assertionLedger"].append(dict(id=identity, state="PASS" if passed else "FAIL"))
        if not passed:
            report["blockers"].append(code)

    def read(path: str) -> bytes:
        return contained_file(repository, path, "draft input").read_bytes()

    def load(path: str) -> dict:
        return v2_parse_json(read(path))

    def conform(value: dict, definition: str) -> None:
        jsonschema.Draft202012Validator({"$ref": f"#/$defs/{definition}",
                                        "$defs": schema["$defs"]}).validate(value)

    try:
        schema_bytes = read(SCHEMA)
        check("schema-root", digest(schema_bytes) == DRAFT_PINS[SCHEMA], "SCHEMA_DRIFT")
        if report["blockers"]:
            raise ValueError("schema differs from the code-owned draft definition")
        schema = v2_parse_json(schema_bytes)
        jsonschema.Draft202012Validator.check_schema(schema)
        package = load(f"{BASE}/package.json")
        conform(package, "package")
        check("draft-authority", package["authority"] == AUTHORITY
              and package["baselineRevision"] == BASELINE, "DRAFT_AUTHORITY_INVALID")
        check("infrastructure-decision", package["infrastructureDecision"] == PLATFORM_DECISION,
              "INFRASTRUCTURE_DECISION_DRIFT")
        check("pending-decisions", [row["id"] for row in package["pendingDecisions"]] == DECISION_IDS,
              "DECISION_INVENTORY_DRIFT")
        check("source-inventory", package["sourceBindings"] == SOURCE_PINS, "SOURCE_INVENTORY_DRIFT")
        for path, expected in SOURCE_PINS.items():
            check(f"source:{path}", digest(read(path)) == expected, "SOURCE_BYTES_DRIFT")
        rows = package["files"]
        check("artifact-inventory", [row["path"] for row in rows] == ARTIFACTS, "ARTIFACT_INVENTORY_DRIFT")
        # Read only code-owned paths, never a manifest-supplied path or command.
        declared = {row["path"]: row["sha256"] for row in rows}
        for path in ARTIFACTS:
            check(f"artifact:{path}", digest(read(path)) == declared.get(path), "ARTIFACT_BYTES_DRIFT")
            if path in DRAFT_PINS:
                check(f"draft-pin:{path}", digest(read(path)) == DRAFT_PINS[path], "DRAFT_CONTENT_DRIFT")

        inventory = load(f"{BASE}/inventories.json")
        conform(inventory, "inventory")
        check("legacy-membership", list(inventory["legacyInventories"]) == list(STORIES), "LEGACY_INVENTORY_DRIFT")
        for story in STORIES:
            old = load(f"_bmad-output/planning-artifacts/v9/story-contracts/{story}.json")
            ids = inventory["legacyInventories"].get(story, [])
            check(f"legacy:{story}", ids == LEGACY_IDS[story]
                  and digest(("\n".join(ids) + "\n").encode()) == old["inventory"]["sha256"],
                  "LEGACY_INVENTORY_DRIFT")
        expected_requirements = NEW_IDS | {identity for ids in LEGACY_IDS.values() for identity in ids}
        requirement_ids = [row["id"] for row in inventory["requirements"]]
        check("requirement-membership", len(requirement_ids) == len(expected_requirements)
              and set(requirement_ids) == expected_requirements, "REQUIREMENT_INVENTORY_DRIFT")
        coverage: dict[str, list[str]] = {identity: [] for identity in expected_requirements}

        for story in STORIES:
            old = load(f"_bmad-output/planning-artifacts/v9/story-contracts/{story}.json")
            draft = load(f"{BASE}/story-contracts/{story}.json")
            conform(draft, "story")
            check(f"story-binding:{story}", draft["storyId"] == story
                  and draft["predecessorContract"] == f"_bmad-output/planning-artifacts/v9/story-contracts/{story}.json"
                  and draft["architectureDecisions"] == ADS and draft["predecessors"] == old["predecessors"]
                  and draft["rollback"] == old["rollback"] and draft["finalRecord"] == old["finalRecord"]
                  and draft["outcome"]["title"] == old["outcome"]["title"], "STORY_BINDING_DRIFT")
            check(f"scenarios:{story}", [row["id"] for row in draft["scenarios"]]
                  == [row["id"] for row in old["scenarios"]], "SCENARIO_INVENTORY_DRIFT")
            legacy_commands = {row["id"]: row["command"] for row in old["scenarios"]}
            for row in draft["scenarios"]:
                check(f"command:{row['id']}", row["legacyCommand"] == legacy_commands.get(row["id"]), "LEGACY_COMMAND_DRIFT")
                for identity in row["requirementIds"]:
                    if identity in coverage:
                        coverage[identity].append(row["id"])
                    else:
                        check(f"requirement:{identity}", False, "REQUIREMENT_INVENTORY_DRIFT")
        check("nonempty-coverage", all(coverage.values()), "REQUIREMENT_COVERAGE_MISSING")
        check("coverage-relations", all(row["scenarioIds"] == sorted(coverage.get(row["id"], []))
                                       for row in inventory["requirements"]), "REQUIREMENT_COVERAGE_DRIFT")

        graph = load(f"{BASE}/execution-graph.json")
        original = load("_bmad-output/planning-artifacts/v9-execution-graph-v1.json")
        check("graph-preserved", graph == original, "GRAPH_DRIFT")
        nodes = {node["id"]: set(node["predecessors"]) for node in graph["nodes"]}
        edges = {(edge["from"], edge["to"]) for edge in graph["edges"]}
        derived = {(parent, node) for node, parents in nodes.items() for parent in parents}
        check("graph-relations", len(nodes) == len(graph["nodes"]) == 38
              and len(edges) == len(graph["edges"]) == 61 and edges == derived
              and all(parent in nodes for parents in nodes.values() for parent in parents), "GRAPH_RELATION_INVALID")
        remaining = {node: set(parents) for node, parents in nodes.items()}
        ordered = []
        while remaining:
            ready = sorted(node for node, parents in remaining.items() if not parents)
            if not ready:
                break
            ordered.extend(ready)
            for node in ready:
                del remaining[node]
            for parents in remaining.values():
                parents.difference_update(ready)
        check("graph-acyclic", not remaining, "GRAPH_CYCLE")
        check("graph-order", inventory["topologicalOrder"] == ordered, "GRAPH_ORDER_DRIFT")
        report["pendingDecisions"] = package["pendingDecisions"]
    except (OSError, GateError) as error:
        report["blockers"].append("INPUT_UNAVAILABLE")
        report["assertionLedger"].append(dict(id="inputs", state="BLOCKED"))
        report["detail"] = str(error)
    except (ValueError, KeyError, TypeError, RecursionError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        check("input-structure", False, "DRAFT_STRUCTURE_INVALID")
        report["detail"] = str(error).splitlines()[0]
    report["blockers"] = sorted(set(report["blockers"]))
    if report["blockers"]:
        report["result"] = "BLOCKED" if report["blockers"] == ["INPUT_UNAVAILABLE"] else "FAIL"
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path("."))
    options = parser.parse_args()
    report = verify(options.repository)
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return {"PASS": 0, "FAIL": 1, "BLOCKED": 2}[report["result"]]


if __name__ == "__main__":
    raise SystemExit(main())
