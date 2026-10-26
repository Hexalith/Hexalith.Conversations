#!/usr/bin/env python3
"""Read-only Epic 16 enrollment/candidate consistency; never grant runtime entry.

Exit 0 is structural PASS, 1 is malformed/drifted input, 2 is unavailable input.
Entry remains BLOCKED even when structure passes. Commands are data, never run.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import re
from types import FunctionType, SimpleNamespace

import jsonschema

from contained_reader import ContainedReader, ReadError, relative_path
from evidence_boundary_current import digest
from generate_story_record import (GateError, V2_CONTRACT_SCHEMA_VERSION, V2_V14_CONTRACT_SCHEMA_VERSION,
                                   v2_reject_constant, v2_reject_duplicate_keys)
import verify_epic_16_prerequisites as prerequisite

BASE = "_bmad-output/planning-artifacts/epic-16-adoption-v1"
SCHEMA = "_bmad/schemas/epic-16-adoption-v1.schema.json"
TOOL = ".config/dotnet-tools.json"
MANIFEST = "module/hexalith.module-manifest.v2.json"
MANIFEST_SCHEMA = "references/Hexalith.Builds/schemas/hexalith.module-manifest.v2.json"
OBSERVATION = "module/validation/results.json"
AUTHORITY = dict(prerequisite.AUTHORITY)
# Additional independent source pins extend, rather than replace, the existing
# prerequisite checker and its immutable source/draft roots of trust.
EXTRA_SOURCE_PINS = {'_bmad-output/planning-artifacts/epic-16-prerequisites-v1/package.json': '16eaf5b30e379ca50172a20fa5b069ac6a3cd4c675c5ca322d9178a1929f5746',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/inventories.json': '791b3a9de4a6f846fe265a5284ee2d8d930b30a2ec1cd48ca6ee1a8eb7e792b8',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/execution-graph.json': '989cea64a9f8bdcee5d69909d2c3f9662722e27ba19ba3d80e129fcab54c3747',
 '_bmad/scripts/verify_epic_16_prerequisites.py': 'b10e910aefdd7f9f19cb80bbb19936efd2340f641be45787c140cf45738bbfa6',
 '_bmad/scripts/generate_story_record.py': 'c55a5e58f8a43d31906347bf90dddeb1661ebdfe251abf0c2b03e0a0f5c1cd50',
 'references/Hexalith.Builds/schemas/hexalith.module-manifest.v2.json': '888d719b58493b4ebe14975e96262eca87e00afee8fe3a451a83ac7da366293e',
 'references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceRoutes.cs': '7e8215d6e69c8c077e9995de7355cf72fb4d8d8863a781e599464633545f87f6',
 'references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServicePolicies.cs': '76710ac3b8a5f78fcfb2ca6ce5a4605e372b52d76a9f995fca259d48efb53fa6',
 'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Authentication/EventStoreWorkloadOperations.cs': '58f0adf647ad3f57034318a2dd2eeccf0d553491b819fc5525e3eaf3b7b5d6f7',
 'references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceSecurityExtensions.cs': '759e27337c8146dd8518d02ba5907d85ddf1c6a91d3dc316b40964b77f82f629',
 'src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj': '11aedb0ea85809e27284405ffad0eaed519c42ad2344a570ad9abfc1f6af6cd0',
 'tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj': '320077e5282671bbe921ce01c3ed7f8680d096f31a499e0e41a32ff49a02e36f',
 'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Authentication/DaprAppChannelToken.cs': '0934a549051cfbc026d11d0422f386adb3deefb0f83313bb9bf49c7e76d74788',
 'references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs': '881ef5a456d4ee778161d9383c72684679a0e7af03eff85f99597aee02748692',
 'references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDataProtectionServiceCollectionExtensions.cs': '7f01b7602baaf08a567c0e5d45c9db656c4e71305127951b03d23f3ef92da82b',
 'references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs': '16100f8857e93d74ecd2c48c60b34b6ba6fcdf95d433837f36907be04e570def',
 'src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs': 'f392210ede5f9eb15d3fa56808a276af8c101f25d6e7837c7b21f2b1b9522428',
 'src/Hexalith.Conversations.Server/Program.cs': 'c5155b67f8478d4d74ec731161157d7e6d980ec1cfcd226e21409a1c4fef9ed9'}
SOURCE_PINS = {**prerequisite.SOURCE_PINS, **prerequisite.DRAFT_PINS, **EXTRA_SOURCE_PINS}
# Fixed enrollment/documentation/CLI snapshot pins prevent rehashed weakening.
# Candidate assertions instead compare in full against the pinned draft bytes.
ARTIFACT_PINS = {'.config/dotnet-tools.json': '9e21fa262e48d20f4a29b9de40c6544c95bd14e2fd9e624f8d23ab73d419e25a',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/README.md': 'bc7d953ab6ea741b488ae8c96f5d4a7dde6d1fe2ef0ff33a3ff79b2cb82a6157',
 '_bmad/schemas/epic-16-adoption-v1.schema.json': '884d8efb4bf58571c99dca0b63ba0f97b10e50db0cb4bd238ed5a96f017f4f98',
 'module/README.md': '35b33d9fcd315aacebb70181e4313035ef9979b99cab2ba903f699572c977ef3',
 'module/hexalith.module-manifest.v2.json': '0bed1c297c9c9d78fdb8f37197ac5f71d829191c0535fdd78e4034aaa017bdf0',
 'module/hosting-inventory.json': '1bfd46248158637c950cd6764a47f926b1683568218208a0107d3b1ed3ada1e7',
 'module/sm-2-checklist.md': '71fb1992ca38602e4cfba34d1b5471459aff986a1ac87b774fb887feafaeec37',
 'module/validation/adoption-text.txt': '9468023ec91fcb1b68232b0a86034457378aaba4211cd6a9e5540f5cc9788f5a',
 'module/validation/adoption.json': '80b991caeea83f5e2d409853712f3c48ba12aadd2e14f1ecc07a3003159b824d',
 'module/validation/manifest.txt': 'bc037e128cae9965e41ebb9c79ae42d51a432e95acf1102acaa7ef5fe498bdf0',
 'module/validation/restore.txt': '5e16199b8629d57251372ca4a61c168330f1502b8688b02a3468323bda14c2ec',
 'module/validation/results.json': '32c65f853c202ed7e0ff6e7ec9525ce462a5496c5b573478a010964187ffaa02',
 'module/validation/version.txt': '53dbc1fffed5b37cb2587bcb7691e0ed28d44939df0b2cf1a4a76d07c5325b9d'}
ARTIFACTS = ['.config/dotnet-tools.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/README.md',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/execution-graph.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/inventories.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.1.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.2.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.3.json',
 '_bmad/schemas/epic-16-adoption-v1.schema.json',
 'module/README.md',
 'module/hexalith.module-manifest.v2.json',
 'module/hosting-inventory.json',
 'module/sm-2-checklist.md',
 'module/validation/adoption-text.txt',
 'module/validation/adoption.json',
 'module/validation/manifest.txt',
 'module/validation/restore.txt',
 'module/validation/results.json',
 'module/validation/version.txt']
REVIEW_BOUNDARY = ['.config/dotnet-tools.json',
 '_bmad-output/implementation-artifacts/spec-epic-16-adoption-preparation.md',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/README.md',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/execution-graph.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/inventories.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/package.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.1.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.2.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.3.json',
 '_bmad/schemas/epic-16-adoption-v1.schema.json',
 '_bmad/scripts/contained_reader.py',
 '_bmad/scripts/tests/fixtures/epic16-reference-inputs.json',
 '_bmad/scripts/tests/test_verify_epic_16_adoption.py',
 '_bmad/scripts/verify_epic_16_adoption.py',
 'module/README.md',
 'module/hexalith.module-manifest.v2.json',
 'module/hosting-inventory.json',
 'module/sm-2-checklist.md',
 'module/validation/adoption-text.txt',
 'module/validation/adoption.json',
 'module/validation/manifest.txt',
 'module/validation/restore.txt',
 'module/validation/results.json',
 'module/validation/version.txt']


class InputProblem(Exception):
    """A diagnosed unavailable or invalid contained input."""

    def __init__(self, code: str, detail: str, *, unavailable: bool = False):
        super().__init__(detail)
        self.code, self.unavailable = code, unavailable


def read(reader: ContainedReader, relative: str) -> bytes:
    """Read through the neutral descriptor reader with precise unavailable codes."""
    try:
        return reader.read(relative)
    except ReadError as error:
        code = error.code
        if code == "INPUT_UNAVAILABLE" and relative in (
                TOOL, "module/validation/restore.txt", "module/validation/version.txt"):
            code = "ACCEPTED_TOOL_UNAVAILABLE"
        raise InputProblem(code, f"{relative}: {error}", unavailable=error.unavailable) from error


def parse(content: bytes, path: str):
    """Strict JSON, rejecting duplicate keys, non-finite constants and overflow."""
    def finite_float(value: str) -> float:
        result = float(value)
        if not math.isfinite(result):
            raise ValueError("non-finite JSON number")
        return result
    try:
        return json.loads(content.decode("utf-8"), object_pairs_hook=v2_reject_duplicate_keys,
                          parse_constant=v2_reject_constant, parse_float=finite_float)
    except (ValueError, UnicodeError, RecursionError) as error:
        raise InputProblem("JSON_INVALID", f"invalid JSON in {path}: {error}") from error


def expected_commands(story: str, preserved: dict) -> list[dict]:
    """Specify the unavailable successor interface without executing commands."""
    contract = f"{BASE}/story-contracts/{story}.json"
    rows = []
    for number, scenario in enumerate(preserved["scenarios"], 1):
        identity = scenario["id"]
        if number < 6:
            outputs = [f"artifacts/v14/{story}/{identity}.json"]
            command = (f"python3 _bmad/scripts/verify_story_{story.replace('.', '_')}.py"
                       f" --repository . --contract {contract} --scenario {identity}"
                       f" --candidate HEAD --output {outputs[0]}")
        else:
            outputs = preserved["finalRecord"]["paths"]
            command = ("python3 _bmad/scripts/generate_story_record.py --repository ."
                       f" --contract {contract} --format bundle --output-json {outputs[0]}"
                       f" --output-markdown {outputs[1]}")
        rows.append(dict(scenarioId=identity, command=command, outputs=outputs,
                         state="unavailable", executed=False))
    return rows


def verify(repository: Path) -> dict:
    """Return structural facts and distinct pending/failed entry facts, read-only."""
    report = dict(schemaVersion="hexalith.conversations.epic16-adoption-check.v1",
                  validationScope="structural-consistency-only", result="PASS",
                  entryReadiness="BLOCKED", authority=dict(AUTHORITY),
                  assertionLedger=[], blockers=[], diagnostics=[], entryBlockers=[],
                  evidenceBoundary=dict(status="not-applicable", scope="runtime-publication",
                                        reason="Preparation publishes no runtime receipts or final records."))

    def check(identity: str, passed: bool, code: str, detail: str) -> None:
        report["assertionLedger"].append(dict(id=identity, state="PASS" if passed else "FAIL"))
        if not passed:
            report["blockers"].append(code)
            report["diagnostics"].append(dict(code=code, detail=detail))

    def entry(code: str, detail: str, state: str = "BLOCKED") -> None:
        report["entryBlockers"].append(dict(code=code, state=state, detail=detail))

    # These are actual missing capabilities/decisions, independent of caller data.
    entry("GENERATOR_UNSUPPORTED", "generate_story_record.py supports V9/V14, not the candidate wrapper.")
    entry("SCENARIO_VERIFIERS_UNAVAILABLE", "Fifteen measured scenario verifiers and successor final-record support are not implemented.")
    entry("QUALIFICATION_PENDING", "Source/package/hosted parity, AD-5 envelope acceptance and recovery qualification are pending.")
    entry("SM2_UNVERIFIED", "No enrolled runtime, debugger/edit, cleanup or Story 3.11 acceptance evidence was produced.")
    entry("APPHOST_OWNERSHIP_PENDING", "Both test AppHosts remain frozen; exemption/relocation and retirement gates are pending.")
    entry("OWNER_ENTRY_DECISION_PENDING", "The seven original owner decisions and separate Story 16.1 entry approval remain pending.")
    entry("TENANTS_OPTIONS_BINDING_PENDING", "Current AddHexalithTenants() registration does not bind configuration. Adopt its configureOptions delegate to bind Tenants:PubSubName and Tenants:TopicName before route qualification.")
    entry("DATA_PROTECTION_REGISTRATION_PENDING", "Injected settings do not register AddEventStoreDataProtection; adopt the SDK registration with IConfiguration and a stable application name, then qualify restart/key-ring persistence.")
    entry("DECLARATIVE_SUBSCRIPTIONS_PENDING", "Platform must inject Dapr:DeclarativeSubscriptions=true; qualify /tenants/events delivery, dead-letter behavior, app-channel protection and absent /dapr/subscribe discovery through the production route.")
    entry("LIVE_TOOL_RECHECK_REQUIRED", "Stored CLI outputs describe this preparation only; rerun accepted CLI checks against the owner-reviewed tracked candidate.")
    reader = None
    try:
        repository = repository.absolute()
        reader = ContainedReader(repository)
        reader.__enter__()
        cache: dict[str, bytes] = {}

        def content(path: str) -> bytes:
            if path not in cache:
                cache[path] = read(reader, path)
            return cache[path]

        def load(path: str):
            return parse(content(path), path)

        # Never follow a package-supplied source path, artifact path or command.
        # Validate these independent source bytes before consulting the draft checker.
        for path, expected in SOURCE_PINS.items():
            check(f"source:{path}", digest(content(path)) == expected,
                  "SOURCE_BYTES_DRIFT", f"pinned source differs: {path}")
        if report["blockers"]:
            raise InputProblem("SOURCE_BINDING_INVALID", "independent prerequisite/tooling/source bindings changed")
        # Run the unchanged prerequisite code over captured bytes. Its historical
        # Path-based helper is replaced only in an isolated function namespace;
        # neither original module globals nor historical source bytes change.
        snapshot_globals = dict(vars(prerequisite))
        snapshot_globals["contained_file"] = lambda _root, path, _description: SimpleNamespace(
            read_bytes=lambda: content(path))
        draft_report = FunctionType(prerequisite.verify.__code__, snapshot_globals)(repository)
        check("prerequisite-consistency", draft_report["result"] == "PASS",
              "PREREQUISITE_INVALID", f"prerequisite checker: {draft_report['result']}: {draft_report['blockers']}")
        report["prerequisiteAssertionCount"] = len(draft_report["assertionLedger"])

        check("schema-pin", digest(content(SCHEMA)) == ARTIFACT_PINS[SCHEMA],
              "SCHEMA_DRIFT", "adoption schema differs from its independent pin")
        if "SCHEMA_DRIFT" in report["blockers"]:
            raise InputProblem("STRUCTURE_INVALID", "refusing untrusted adoption schema")
        schema = load(SCHEMA)
        jsonschema.Draft202012Validator.check_schema(schema)

        def conform(document, name: str) -> None:
            jsonschema.Draft202012Validator({"$ref": f"#/$defs/{name}", "$defs": schema["$defs"]}).validate(document)

        package = load(f"{BASE}/package.json")
        conform(package, "package")
        check("generator-capability-schemas", package["capabilities"]["supportedGeneratorSchemas"]
              == [V2_CONTRACT_SCHEMA_VERSION, V2_V14_CONTRACT_SCHEMA_VERSION],
              "GENERATOR_CAPABILITY_DRIFT", "capability metadata must match the generator accepted schema constants")
        check("source-inventory", package["sourceBindings"] == SOURCE_PINS,
              "SOURCE_INVENTORY_DRIFT", "source inventory/digests must equal independently pinned sources")
        check("artifact-inventory", [row["path"] for row in package["files"]] == ARTIFACTS,
              "ARTIFACT_INVENTORY_DRIFT", "artifact paths must equal the exact ordered fixed inventory")
        for path in package["sourceBindings"]:
            relative_path(path)
        for row in package["files"]:
            relative_path(row["path"])
        check("review-boundary", package["reviewBoundary"] == REVIEW_BOUNDARY,
              "REVIEW_BOUNDARY_DRIFT", "review boundary changed; no runtime/source/history edits are included")
        for path in package["reviewBoundary"]:
            relative_path(path)
        declared = {row["path"]: row["sha256"] for row in package["files"]}
        for path in ARTIFACTS:
            actual = digest(content(path))
            check(f"artifact:{path}", actual == declared.get(path),
                  "ARTIFACT_BYTES_DRIFT", f"declared digest differs: {path}")
            if path in ARTIFACT_PINS:
                check(f"fixed-artifact:{path}", actual == ARTIFACT_PINS[path],
                      "ENROLLMENT_OR_REVIEW_DRIFT", f"independent enrollment/review snapshot differs: {path}")
        if "ENROLLMENT_OR_REVIEW_DRIFT" in report["blockers"]:
            raise InputProblem("SNAPSHOT_INVALID", "refusing to report altered enrollment/CLI snapshots as observed facts")
        check("pending-decisions", package["pendingDecisions"] == load(f"{prerequisite.BASE}/package.json")["pendingDecisions"],
              "DECISION_DRIFT", "all seven owner decisions/questions must remain pending and unchanged")
        for story in prerequisite.STORIES:
            original_path = f"{prerequisite.BASE}/story-contracts/{story}.json"
            original = load(original_path)
            candidate = load(f"{BASE}/story-contracts/{story}.json")
            conform(candidate, "story")
            check(f"preserved-contract:{story}", candidate["preservedContract"] == original,
                  "PRESERVED_CONTRACT_DRIFT", f"full assertions, inventory, graph bindings, rollback/final-record targets changed: {story}")
            check(f"draft-binding:{story}", candidate["source"] == dict(path=original_path, sha256=SOURCE_PINS[original_path]),
                  "DRAFT_BINDING_DRIFT", f"candidate draft binding changed: {story}")
            relative_path(candidate["source"]["path"])
            check(f"future-commands:{story}", candidate["futureCommands"] == expected_commands(story, original),
                  "FUTURE_COMMAND_DRIFT", f"exact ordered future commands/outputs changed: {story}")
        for name, definition in (("inventories.json", "inventory"), ("execution-graph.json", "graph")):
            candidate = load(f"{BASE}/{name}")
            conform(candidate, definition)
            check(name, candidate == load(f"{prerequisite.BASE}/{name}"),
                  "INVENTORY_OR_GRAPH_DRIFT", f"exact preserved membership/relations changed: {name}")

        tool = load(TOOL)
        accepted = load("references/Hexalith.Platform/.config/dotnet-tools.json")
        check("accepted-tool", tool["tools"]["hexalith.builds.module.cli"] == accepted["tools"]["hexalith.builds.module.cli"],
              "TOOL_PIN_DRIFT", "tool must be accepted 4.35.1, command hexalith-module, rollForward false")
        manifest = load(MANIFEST)
        token_source = "references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Authentication/DaprAppChannelToken.cs"
        token_key = re.search(r'public const string ConfigurationKey\s*=\s*"([^"]+)"',
                              content(token_source).decode("utf-8")).group(1)
        module = manifest["modules"][0]
        inputs = module["runtime"]["extensions"][0]["configurationInputs"]
        check("app-channel-token", [row["key"] for row in inputs if row["reference"] == "app-channel-token"] == [token_key]
              and [row["configurationKey"] for row in module["integration"]["secrets"] if row["name"] == "app-channel-token"] == [token_key],
              "TOKEN_CONTRACT_DRIFT", "both token configuration keys must match the SDK DaprAppChannelToken.ConfigurationKey")
        check("declarative-subscription-input", any(row["key"] == "Dapr:DeclarativeSubscriptions"
              and row["reference"] == "dapr-declarative-subscriptions-true" for row in inputs),
              "SUBSCRIPTION_CONTRACT_DRIFT", "declare the Platform input required to resolve to Dapr:DeclarativeSubscriptions=true")
        manifest_errors = list(jsonschema.Draft202012Validator(load(MANIFEST_SCHEMA)).iter_errors(manifest))
        observation = load(OBSERVATION)
        adoption = load("module/validation/adoption.json")
        commands = {row["id"]: row for row in observation["commands"]}
        def field_path(error) -> str:
            result = ""
            for part in error.absolute_path:
                result += f"[{part}]" if isinstance(part, int) else ("." if result else "") + part
            return result
        native_fields = sorted(row["field"] for row in adoption["diagnostics"] if row["ruleId"] == "HXP002")
        check("manifest-validation-observation", sorted(field_path(error) for error in manifest_errors) == native_fields
              and all(error.validator == "pattern" and error.instance == token_key for error in manifest_errors),
              "MANIFEST_OBSERVATION_DRIFT", "current strict accepted-schema errors must match the exact recorded native token-format failures")
        for error in manifest_errors:
            entry("ACCEPTED_TOOL_TOKEN_FORMAT_BLOCKED", f"HXP002 {field_path(error)}: accepted 4.35.1 rejects SDK key {token_key}; {commands['manifest']['command']}")
        # The raw snapshot is independently pinned; do not accept rehashed PASS claims.
        failures = [row for row in adoption["adoption"] if not row["passed"]]
        report["observedStaticValidation"] = dict(
            acceptedTool=observation["restoredVersion"],
            ordinaryManifestExitCode=commands["manifest"]["exitCode"],
            ordinaryManifestResult="FAIL" if commands["manifest"]["exitCode"] else "PASS",
            adoptionExitCode=commands["adoption-json"]["exitCode"],
            adoptionTextExitCode=commands["adoption-text"]["exitCode"], trackedSourceOnly=True, additiveFilesTrackedAtObservation=False,
            passingRows=len(adoption["adoption"]) - len(failures), failingRows=len(failures),
            failures=failures, runtimeExecuted=False, sm2Verified=False)
        check("observed-adoption-failures", adoption["status"] == "failed"
              and commands["adoption-json"]["exitCode"] != 0 and commands["adoption-text"]["exitCode"] != 0,
              "STATIC_OBSERVATION_DRIFT", "retain actual failing native adoption results and all reported rows")
        for row in failures:
            codes = sorted({diagnostic["ruleId"] for diagnostic in adoption["diagnostics"]
                            if diagnostic["field"] == row["requirement"] and diagnostic["source"] == row["file"]})
            prefix = " ".join(codes) + " " if codes else ""
            entry("MODULE_ADOPTION_FAILED", f"{prefix}{row['requirement']} {row['file']}: {row['reason']}", "FAIL")
    except (InputProblem, ReadError) as error:
        state = "BLOCKED" if error.unavailable else "FAIL"
        report["assertionLedger"].append(dict(id="input-diagnostic", state=state))
        report["blockers"].append(error.code)
        report["diagnostics"].append(dict(code=error.code, detail=str(error)))
    except (OSError, GateError) as error:
        report["assertionLedger"].append(dict(id="input-diagnostic", state="BLOCKED"))
        report["blockers"].append("INPUT_UNAVAILABLE")
        report["diagnostics"].append(dict(code="INPUT_UNAVAILABLE", detail=str(error)))
    except (ValueError, KeyError, TypeError, RecursionError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        check("input-structure", False, "STRUCTURE_INVALID", str(error).splitlines()[0])
    finally:
        if reader is not None:
            reader.__exit__(None, None, None)
    report["blockers"] = sorted(set(report["blockers"]))
    if any(row["state"] == "FAIL" for row in report["assertionLedger"]):
        report["result"] = "FAIL"
    elif report["blockers"]:
        report["result"] = "BLOCKED"
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
