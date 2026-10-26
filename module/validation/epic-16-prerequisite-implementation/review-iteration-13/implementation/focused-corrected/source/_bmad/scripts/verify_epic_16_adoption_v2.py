#!/usr/bin/env python3
"""Read-only current Epic 16 preparation; structural PASS grants no runtime entry.

Exit 0 is structural PASS, 1 is invalid/drifted input, 2 is unavailable input.
Commands remain data. No historical checker is run against current source pins.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re

import jsonschema

from contained_reader import ContainedReader, ReadError, relative_path
from evidence_boundary_current import digest
from generate_story_record import GateError, V2_CONTRACT_SCHEMA_VERSION, V2_V14_CONTRACT_SCHEMA_VERSION
import verify_epic_16_adoption as historic

BASE = "_bmad-output/planning-artifacts/epic-16-adoption-v2"
SCHEMA = "_bmad/schemas/epic-16-adoption-v2.schema.json"
TOOL = historic.TOOL
MANIFEST = historic.MANIFEST
MANIFEST_SCHEMA = historic.MANIFEST_SCHEMA
NATIVE = "module/validation/epic-16-adoption-v2"
OBSERVATION = f"{NATIVE}/results.json"
STORIES = ("16.1", "16.2", "16.3")
AUTHORITY = dict(implementationHold="ACTIVE", executionAllowed=False,
                 ownerApprovalClaimed=False, releaseAuthorized=False, pushAuthorized=False)
InputProblem = historic.InputProblem
parse = historic.parse

# Independently inspected current source roots. The bundle cannot redefine them.
SOURCE_PINS = {'_bmad-output/planning-artifacts/architecture.md': '948d9c98d99d2453a1ff3904f4f05392b3a2959e379c5fd6e466dc20ed1f05be',
 '_bmad-output/planning-artifacts/epics.md': 'e6411339d2db80770f7aa923b43caf9c9fe8c416fdf9c5f94e07cddb3d3ca6e7',
 '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md': '7098e7355522f503e136dc8c5e84ce5c873cb5bad08b92aa54495811c54853b6',
 '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18.md': '3e4cacb752c8e962691e391d6634d136274adbe2dfa4daea35f0d443ec455ddc',
 '_bmad-output/planning-artifacts/v9-execution-graph-v1.json': '989cea64a9f8bdcee5d69909d2c3f9662722e27ba19ba3d80e129fcab54c3747',
 '_bmad-output/planning-artifacts/v9/story-contracts/16.1.json': 'a533959de8227a0fe31d152f3578e63cd10ba129b51506693290b05f7fbfe1d7',
 '_bmad-output/planning-artifacts/v9/story-contracts/16.2.json': '09e8462c84dfd909df652993b4bb83ca5b65dd1bac14ced571994ae534ec97ab',
 '_bmad-output/planning-artifacts/v9/story-contracts/16.3.json': '4702547ebc24970cddfe034a2ab3f3b74babf3b97b16fd7e55787e0c311cafd8',
 '_bmad-output/planning-artifacts/implementation-hold-v1.json': '2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12',
 '_bmad-output/implementation-artifacts/sprint-status.yaml': '18744a7ddbf62ed1b02050fb644630f95ca1c80e6be497fa75cc00d3c73df500',
 'docs/runbooks/current-change-validation.md': 'd5284f00b8c850d43cabfcd45d2a4cf94d76a04a0bf7d6761ead7f17e5947801',
 '.gitmodules': 'f772e17bced54e00841b7e84c2e9e4e1624387883c129d2a851316cacab38987',
 'references/Hexalith.Platform/README.md': '3602335091b8e457b24884ff86e16bd5105e1cc764d8210aef0ef826f109df5c',
 'references/Hexalith.Platform/docs/domain-module-enrollment.md': 'f3871a49eb4a741a521ae02378f4fa494d7388313f6bbf22695b114173c4cb44',
 'references/Hexalith.Platform/hexalith-platform.json': '3ba6c30a8d602bbc3216f0a4f9f90fb9e1f2e11ea985b4fb5058c3611149fb23',
 'references/Hexalith.Platform/.config/dotnet-tools.json': '5d72a022a9416cb753da75cda1b0f1945fd938c065521fd9946aaa525fe048da',
 'references/Hexalith.Platform/_bmad-output/specs/spec-platform/SPEC.md': '7afec7574a99a72efb94b150e6520a09d56faea372d13e5a6d841040fb30e8d4',
 'references/Hexalith.Platform/_bmad-output/planning-artifacts/architecture/architecture-platform-2026-09-27/ARCHITECTURE-SPINE.md': 'cc95a53ab85becee18f924cc9a1a915a2a05fcce8caed3a3d46643ee1051e936',
 'references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformDaprBindings.cs': '18a0a7535ee3827e9c98d559079d53761e702463c96b57a63e3c24dd7d3a0db8',
 'references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformAppModel.cs': '8efd454b60cd9c663092016454674c649c5ad2279dcd3ced27aa87aa5b275064',
 '_bmad-output/planning-artifacts/production-operational-envelope-v1.md': '0114ba16bde6086cc30aa45e7eaafa8b7a5330fea4bfecfbf355fafcbbf67db3',
 '_bmad/schemas/epic-16-prerequisites-v1.schema.json': 'b7095d7191a96dcefb5a0c333958e5c7eccbd7fa1fae9cb8d62ea2ec8bcad497',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/epic.md': 'b0b110461e81457a12b35bc8515ad691d26a926b206189495241f7f52a50097c',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/story-contracts/16.1.json': '3e44d0498d231633a12ee122d72535c178870a1601ab8c552c815566fe257e08',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/story-contracts/16.2.json': 'a21c8c817e5126343110a5e6730883b5cf32738610b3f552ef6cdbbfe605a965',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/story-contracts/16.3.json': '5eeccfb8427e4ebccc1955f965d00e3c5655834a3c3005443c970be2dd99509a',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/package.json': '16eaf5b30e379ca50172a20fa5b069ac6a3cd4c675c5ca322d9178a1929f5746',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/inventories.json': '791b3a9de4a6f846fe265a5284ee2d8d930b30a2ec1cd48ca6ee1a8eb7e792b8',
 '_bmad-output/planning-artifacts/epic-16-prerequisites-v1/execution-graph.json': '989cea64a9f8bdcee5d69909d2c3f9662722e27ba19ba3d80e129fcab54c3747',
 '_bmad/scripts/verify_epic_16_prerequisites.py': 'b10e910aefdd7f9f19cb80bbb19936efd2340f641be45787c140cf45738bbfa6',
 '_bmad/scripts/generate_story_record.py': '3e0ac7d36b79420d322cf30922b1ab87afa91b1973b44a55ff5d277a0d739841',
 'references/Hexalith.Builds/schemas/hexalith.module-manifest.v2.json': '06d6804f3b111e803edd0d9142b03f36df344e26e2d71e50e2291fe15039daa4',
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
 'src/Hexalith.Conversations.Server/Program.cs': 'c5155b67f8478d4d74ec731161157d7e6d980ec1cfcd226e21409a1c4fef9ed9',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/README.md': 'bc7d953ab6ea741b488ae8c96f5d4a7dde6d1fe2ef0ff33a3ff79b2cb82a6157',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/execution-graph.json': '989cea64a9f8bdcee5d69909d2c3f9662722e27ba19ba3d80e129fcab54c3747',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/inventories.json': '791b3a9de4a6f846fe265a5284ee2d8d930b30a2ec1cd48ca6ee1a8eb7e792b8',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.1.json': 'da8c5462c77239f9f770a9b803b436824efb8e58f7f182fb66ec1343902beae7',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.2.json': '542f1de5e4af3c58c9df7d654e04dd2ea99b9f63cd50bcf864ef5c2ba1257963',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/story-contracts/16.3.json': '226a0a060951139346282a8048338ff9af35014fe7154d79a8863da56507c9dc',
 '_bmad/schemas/epic-16-adoption-v1.schema.json': '884d8efb4bf58571c99dca0b63ba0f97b10e50db0cb4bd238ed5a96f017f4f98',
 'module/hexalith.module-manifest.v2.json': '0bed1c297c9c9d78fdb8f37197ac5f71d829191c0535fdd78e4034aaa017bdf0',
 'module/hosting-inventory.json': '1bfd46248158637c950cd6764a47f926b1683568218208a0107d3b1ed3ada1e7',
 'module/sm-2-checklist.md': '71fb1992ca38602e4cfba34d1b5471459aff986a1ac87b774fb887feafaeec37',
 'module/validation/adoption-text.txt': '9468023ec91fcb1b68232b0a86034457378aaba4211cd6a9e5540f5cc9788f5a',
 'module/validation/adoption.json': '80b991caeea83f5e2d409853712f3c48ba12aadd2e14f1ecc07a3003159b824d',
 'module/validation/manifest.txt': 'bc037e128cae9965e41ebb9c79ae42d51a432e95acf1102acaa7ef5fe498bdf0',
 'module/validation/restore.txt': '5e16199b8629d57251372ca4a61c168330f1502b8688b02a3468323bda14c2ec',
 'module/validation/results.json': '32c65f853c202ed7e0ff6e7ec9525ce462a5496c5b573478a010964187ffaa02',
 'module/validation/version.txt': '53dbc1fffed5b37cb2587bcb7691e0ed28d44939df0b2cf1a4a76d07c5325b9d',
 '_bmad-output/planning-artifacts/epic-16-adoption-v1/package.json': '074189ef4eea79bddeef5935b27df01c637dc5bf832a17eb24933c52968364c0',
 '_bmad/scripts/verify_epic_16_adoption.py': 'b22b6d25ae86186d0575a19dfa09b810220c70d3fb9c0eadefc781a53ce68505',
 '_bmad/scripts/contained_reader.py': 'fac77aa5381c54fd7df3d2b70204b46b4be353992a6529a44dac2da46ea6001b'}
ARTIFACT_PINS = {'.config/dotnet-tools.json': '5d72a022a9416cb753da75cda1b0f1945fd938c065521fd9946aaa525fe048da',
 '.gitattributes': '388d83954dfd6a188ddd2beddaae31e43afe08ec5895639e502d699d5742a9c5',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/README.md': 'e12ee39652b9d7e18052716c955962d716f7e48b6fea2cd1f43f6a22d783ecb0',
 '_bmad/schemas/epic-16-adoption-v2.schema.json': '12278d301a8945ba929d28ecb7a8091663e40f23d821aa4cf3d2d3ec765d3a4f',
 'module/README.md': '777259424cd357f81b14664d491e7d42fb1bc6b06cd55402de546bafd0e871fb',
 'module/hexalith.module-manifest.v2.json': '0bed1c297c9c9d78fdb8f37197ac5f71d829191c0535fdd78e4034aaa017bdf0',
 'module/hosting-inventory.json': '1bfd46248158637c950cd6764a47f926b1683568218208a0107d3b1ed3ada1e7',
 'module/sm-2-checklist.md': '71fb1992ca38602e4cfba34d1b5471459aff986a1ac87b774fb887feafaeec37',
 'module/validation/epic-16-adoption-v2/adoption-json.stderr.txt': 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
 'module/validation/epic-16-adoption-v2/adoption-text.stderr.txt': 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
 'module/validation/epic-16-adoption-v2/adoption-text.txt': 'eeef394d27bac9795d97966acd397dbb19910544a14a4a07dcdd318c499e99be',
 'module/validation/epic-16-adoption-v2/adoption.json': 'aaa2a5212826e659132386da3f8cd7c187c83c2ff2646a075005f669c0053f99',
 'module/validation/epic-16-adoption-v2/manifest.stderr.txt': 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
 'module/validation/epic-16-adoption-v2/manifest.txt': 'bc037e128cae9965e41ebb9c79ae42d51a432e95acf1102acaa7ef5fe498bdf0',
 'module/validation/epic-16-adoption-v2/provenance.json': 'ed487a0e1e27afde2f08deaceef2cc43847fb2780935af1ec7ac07aedf3b62d1',
 'module/validation/epic-16-adoption-v2/restore.stderr.txt': 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
 'module/validation/epic-16-adoption-v2/restore.txt': 'cf2c6e1396b29887925ee3526920d83f7478ae89f450e93d0c5b29b1fbeec0b3',
 'module/validation/epic-16-adoption-v2/results.json': '0b60dda6ca0981ba0f1fabc26b3a21dc70e21fb5fbf414cdf1a67293dddfc1f8',
 'module/validation/epic-16-adoption-v2/version.stderr.txt': 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
 'module/validation/epic-16-adoption-v2/version.txt': '2263b9f44abd72ad7f9a4c5ad92505b0ebbccb4356f1e289438b203995fa6de9'}
ARTIFACTS = ['.config/dotnet-tools.json',
 '.gitattributes',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/README.md',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/execution-graph.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/inventories.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/16.1.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/16.2.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/16.3.json',
 '_bmad/schemas/epic-16-adoption-v2.schema.json',
 'module/README.md',
 'module/hexalith.module-manifest.v2.json',
 'module/hosting-inventory.json',
 'module/sm-2-checklist.md',
 'module/validation/epic-16-adoption-v2/adoption-json.stderr.txt',
 'module/validation/epic-16-adoption-v2/adoption-text.stderr.txt',
 'module/validation/epic-16-adoption-v2/adoption-text.txt',
 'module/validation/epic-16-adoption-v2/adoption.json',
 'module/validation/epic-16-adoption-v2/manifest.stderr.txt',
 'module/validation/epic-16-adoption-v2/manifest.txt',
 'module/validation/epic-16-adoption-v2/provenance.json',
 'module/validation/epic-16-adoption-v2/restore.stderr.txt',
 'module/validation/epic-16-adoption-v2/restore.txt',
 'module/validation/epic-16-adoption-v2/results.json',
 'module/validation/epic-16-adoption-v2/version.stderr.txt',
 'module/validation/epic-16-adoption-v2/version.txt']
REVIEW_BOUNDARY = ['.config/dotnet-tools.json',
 '.gitattributes',
 '_bmad-output/implementation-artifacts/spec-16-1-persist-tenant-access-projection-state-and-prove-convergence.md',
 '_bmad-output/implementation-artifacts/spec-epic-16-adoption-v2-preparation.md',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/README.md',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/execution-graph.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/inventories.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/package.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/16.1.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/16.2.json',
 '_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/16.3.json',
 '_bmad/schemas/epic-16-adoption-v2.schema.json',
 '_bmad/scripts/tests/fixtures/epic16-current-reference-inputs-v2.json',
 '_bmad/scripts/tests/fixtures/epic16-historic-root-inputs.json',
 '_bmad/scripts/tests/test_verify_epic_16_adoption.py',
 '_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py',
 '_bmad/scripts/verify_epic_16_adoption_v2.py',
 'module/README.md',
 'module/validation/epic-16-adoption-v2/adoption-json.stderr.txt',
 'module/validation/epic-16-adoption-v2/adoption-text.stderr.txt',
 'module/validation/epic-16-adoption-v2/adoption-text.txt',
 'module/validation/epic-16-adoption-v2/adoption.json',
 'module/validation/epic-16-adoption-v2/manifest.stderr.txt',
 'module/validation/epic-16-adoption-v2/manifest.txt',
 'module/validation/epic-16-adoption-v2/provenance.json',
 'module/validation/epic-16-adoption-v2/restore.stderr.txt',
 'module/validation/epic-16-adoption-v2/restore.txt',
 'module/validation/epic-16-adoption-v2/results.json',
 'module/validation/epic-16-adoption-v2/version.stderr.txt',
 'module/validation/epic-16-adoption-v2/version.txt']
COMMANDS = [
    ("restore", "dotnet tool restore", "restore.txt", 0),
    ("version", "dotnet tool run hexalith-module --version", "version.txt", 0),
    ("manifest", f"dotnet tool run hexalith-module -- validate --manifest {MANIFEST}", "manifest.txt", 1),
    ("adoption-text", f"dotnet tool run hexalith-module -- validate --manifest {MANIFEST} --adoption", "adoption-text.txt", 1),
    ("adoption-json", f"dotnet tool run hexalith-module -- validate --manifest {MANIFEST} --adoption --output json", "adoption.json", 1),
]


def expected_commands(story: str, preserved: dict) -> list[dict]:
    """Preserve the future interface and change only the wrapper path."""
    return [dict(row, command=row["command"].replace(historic.BASE, BASE))
            for row in historic.expected_commands(story, preserved)]


def read(reader: ContainedReader, path: str) -> bytes:
    """Retain the named missing input and secure-reader result distinction."""
    try:
        return reader.read(path)
    except ReadError as error:
        code = error.code
        if code == "INPUT_UNAVAILABLE" and path in (TOOL, f"{NATIVE}/restore.txt", f"{NATIVE}/version.txt"):
            code = "ACCEPTED_TOOL_UNAVAILABLE"
        raise InputProblem(code, f"{path}: {error}", unavailable=error.unavailable) from error


def verify(repository: Path) -> dict:
    """Check fixed contained inputs and report current observations separately."""
    report = dict(schemaVersion="hexalith.conversations.epic16-adoption-check.v2",
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

    for code, detail in (
        ("GENERATOR_UNSUPPORTED", "Generator supports V9/V14 contracts; v2 wrappers remain unsupported."),
        ("SCENARIO_VERIFIERS_UNAVAILABLE", "Fifteen measured scenario verifiers and successor final-record support remain unavailable."),
        ("QUALIFICATION_PENDING", "Source/Debug, package/Release, hosted parity, AD-5/AD-6, recovery and operator qualification remain pending."),
        ("SM2_UNVERIFIED", "Human active-checkout breakpoint/edit, readiness, cleanup and Story 3.11 acceptance remain unverified."),
        ("APPHOST_OWNERSHIP_PENDING", "Both inventory AppHosts remain frozen; baseline owner exemption/relocation and retirement gates remain pending."),
        ("OWNER_ENTRY_DECISION_PENDING", "All seven original owner decisions and a separately scoped runtime entry decision remain pending."),
        ("TENANTS_OPTIONS_BINDING_PENDING", "Bind Tenants:PubSubName/TopicName using AddHexalithTenants configureOptions before route qualification."),
        ("DATA_PROTECTION_REGISTRATION_PENDING", "Adopt SDK AddEventStoreDataProtection with IConfiguration and a stable application name; qualify persisted keys/restart."),
        ("DECLARATIVE_SUBSCRIPTIONS_PENDING", "Inject Dapr:DeclarativeSubscriptions=true; qualify protected /tenants/events, dead-letter behavior and absent discovery."),
        ("LIVE_TOOL_RECHECK_REQUIRED", "Native reports cover the recorded working tree and supported tracked source only; rerun both modes on the owner-reviewed tracked candidate."),
    ):
        entry(code, detail)
    reader = None
    try:
        reader = ContainedReader(repository.absolute())
        reader.__enter__()
        cache: dict[str, bytes] = {}

        def content(path: str) -> bytes:
            if path not in cache:
                cache[path] = read(reader, path)
            return cache[path]

        def load(path: str):
            return parse(content(path), path)

        # These inventories are code-owned; supplied paths never choose a read.
        for path, expected in SOURCE_PINS.items():
            check(f"source:{path}", digest(content(path)) == expected,
                  "SOURCE_BYTES_DRIFT", f"independently pinned current source differs: {path}")
        if report["blockers"]:
            raise InputProblem("SOURCE_BINDING_INVALID", "current source or preserved predecessor binding changed")
        check("schema-pin", digest(content(SCHEMA)) == ARTIFACT_PINS[SCHEMA],
              "SCHEMA_DRIFT", "refusing a schema that differs from its independent pin")
        if report["blockers"]:
            raise InputProblem("STRUCTURE_INVALID", "untrusted v2 schema")
        schema = load(SCHEMA)
        jsonschema.Draft202012Validator.check_schema(schema)

        def conform(document, definition: str) -> None:
            jsonschema.Draft202012Validator({"$ref": f"#/$defs/{definition}", "$defs": schema["$defs"]}).validate(document)

        package = load(f"{BASE}/package.json")
        conform(package, "package")
        check("generator-capability-schemas", package["capabilities"]["supportedGeneratorSchemas"]
              == [V2_CONTRACT_SCHEMA_VERSION, V2_V14_CONTRACT_SCHEMA_VERSION],
              "GENERATOR_CAPABILITY_DRIFT", "metadata must match current generator constants")
        check("source-inventory", package["sourceBindings"] == SOURCE_PINS,
              "SOURCE_INVENTORY_DRIFT", "exact current source inventory and pins required")
        check("artifact-inventory", [row["path"] for row in package["files"]] == ARTIFACTS,
              "ARTIFACT_INVENTORY_DRIFT", "exact ordered artifact inventory required, excluding the bundle and mutable specs")
        check("review-boundary", package["reviewBoundary"] == REVIEW_BOUNDARY,
              "REVIEW_BOUNDARY_DRIFT", "exact additive preparation review boundary required")
        for path in [*package["sourceBindings"], *package["reviewBoundary"], *(row["path"] for row in package["files"])]:
            relative_path(path)
        declared = {row["path"]: row["sha256"] for row in package["files"]}
        for path in ARTIFACTS:
            actual = digest(content(path))
            check(f"artifact:{path}", actual == declared.get(path),
                  "ARTIFACT_BYTES_DRIFT", f"declared digest differs: {path}")
            if path in ARTIFACT_PINS:
                check(f"fixed-artifact:{path}", actual == ARTIFACT_PINS[path],
                      "ENROLLMENT_OR_REVIEW_DRIFT", f"independent enrollment/observation snapshot differs: {path}")
        if "ENROLLMENT_OR_REVIEW_DRIFT" in report["blockers"]:
            raise InputProblem("SNAPSHOT_INVALID", "refusing altered snapshots as measured facts")
        predecessor = f"{historic.BASE}/package.json"
        check("predecessor-binding", package["predecessor"] == dict(path=predecessor, sha256=SOURCE_PINS[predecessor]),
              "PREDECESSOR_BINDING_DRIFT", "exact immutable adoption v1 predecessor required")
        check("pending-decisions", package["pendingDecisions"] == load(predecessor)["pendingDecisions"],
              "DECISION_DRIFT", "all seven original owner questions and decisions remain unchanged")
        for story in STORIES:
            old = load(f"{historic.BASE}/story-contracts/{story}.json")
            candidate = load(f"{BASE}/story-contracts/{story}.json")
            conform(candidate, "story")
            expected = dict(old, schemaVersion="hexalith.conversations.epic16-adoption-candidate.v2",
                            futureCommands=expected_commands(story, old["preservedContract"]))
            check(f"preserved-contract:{story}", candidate == expected,
                  "PRESERVED_CONTRACT_DRIFT", f"only wrapper identity and future command paths may change: {story}")
            relative_path(candidate["source"]["path"])
            for row in candidate["futureCommands"]:
                for path in row["outputs"]:
                    relative_path(path)
        for name, definition in (("inventories.json", "inventory"), ("execution-graph.json", "graph")):
            candidate = load(f"{BASE}/{name}")
            conform(candidate, definition)
            check(name, candidate == load(f"{historic.BASE}/{name}"),
                  "INVENTORY_OR_GRAPH_DRIFT", f"exact ordered preserved membership/relations required: {name}")
        report["preservedObligations"] = dict(scenarios=18, requirements=32, graphNodes=38, graphEdges=61,
                                               finalRecordSummary="6/6/0/0/0/0")

        tool = load(TOOL)["tools"]["hexalith.builds.module.cli"]
        accepted = load("references/Hexalith.Platform/.config/dotnet-tools.json")["tools"]["hexalith.builds.module.cli"]
        check("accepted-tool", tool == accepted == dict(version="4.35.2", commands=["hexalith-module"], rollForward=False)
              and tool["rollForward"] is False,
              "TOOL_PIN_DRIFT", "exact direct Platform accepted 4.35.2 entry with literal false rollForward required")
        manifest = load(MANIFEST)
        token_source = "references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Authentication/DaprAppChannelToken.cs"
        token_key = re.search(r'public const string ConfigurationKey\s*=\s*"([^"]+)"', content(token_source).decode()).group(1)
        module = manifest["modules"][0]
        inputs = module["runtime"]["extensions"][0]["configurationInputs"]
        check("app-channel-token", token_key == "APP_API_TOKEN"
              and [row["key"] for row in inputs if row["reference"] == "app-channel-token"] == [token_key]
              and [row["configurationKey"] for row in module["integration"]["secrets"] if row["name"] == "app-channel-token"] == [token_key],
              "TOKEN_CONTRACT_DRIFT", "both token fields must retain the real SDK key")
        check("declarative-subscription-input", any(row["key"] == "Dapr:DeclarativeSubscriptions"
              and row["reference"] == "dapr-declarative-subscriptions-true" for row in inputs),
              "SUBSCRIPTION_CONTRACT_DRIFT", "Platform must resolve the existing declarative subscription input to true")
        current_schema = load(MANIFEST_SCHEMA)
        jsonschema.Draft202012Validator.check_schema(current_schema)
        schema_errors = list(jsonschema.Draft202012Validator(current_schema).iter_errors(manifest))
        check("current-source-schema-accepts-token", not schema_errors,
              "MANIFEST_SCHEMA_INCOMPATIBLE", "current independently pinned Builds schema must accept the unchanged real SDK manifest")

        observation = load(OBSERVATION)
        conform(observation, "observation")
        adoption = load(f"{NATIVE}/adoption.json")
        provenance = load(f"{NATIVE}/provenance.json")
        check("observation-provenance", observation["provenance"] == dict(path=f"{NATIVE}/provenance.json",
              sha256=digest(content(f"{NATIVE}/provenance.json")))
              and observation["sourceRevision"] == provenance["sourceRevision"],
              "OBSERVATION_BINDING_DRIFT", "bind recorded working-tree and tracked scan provenance")
        for row, (identity, command, filename, exit_code) in zip(observation["commands"], COMMANDS, strict=True):
            output, stderr = f"{NATIVE}/{filename}", f"{NATIVE}/{identity}.stderr.txt"
            check(f"native-command:{identity}", row["id"] == identity and row["command"] == command
                  and row["exitCode"] == exit_code and row["output"] == output and row["stderr"] == stderr
                  and row["outputSha256"] == digest(content(output)) and row["stderrSha256"] == digest(content(stderr)),
                  "NATIVE_COMMAND_DRIFT", f"preserve exact command, exit, stdout and stderr: {identity}")
            relative_path(row["output"])
            relative_path(row["stderr"])
        native_fields = sorted(row["field"] for row in adoption["diagnostics"] if row["ruleId"] == "HXP002")
        check("published-tool-token-observation", native_fields == [
            "modules[0].integration.secrets[1].configurationKey",
            "modules[0].runtime.extensions[0].configurationInputs[12].key"],
            "MANIFEST_OBSERVATION_DRIFT", "published 4.35.2 still rejects both real SDK token fields despite current source schema acceptance")
        for field in native_fields:
            entry("ACCEPTED_TOOL_TOKEN_FORMAT_BLOCKED", f"HXP002 {field}: published accepted 4.35.2 still rejects {token_key}; source schema acceptance does not resolve native compatibility.")
        failures = [row for row in adoption["adoption"] if row["passed"] is False]
        check("native-row-inventory", len(adoption["adoption"]) == 24 and len(failures) == 18
              and sum(row["passed"] is True for row in adoption["adoption"]) == 6 and adoption["status"] == "failed",
              "STATIC_OBSERVATION_DRIFT", "preserve all 24 native rows including eighteen failures")
        commands = {row["id"]: row for row in observation["commands"]}
        report["observedStaticValidation"] = dict(acceptedTool=observation["restoredVersion"],
            currentSourceManifestSchemaResult="PASS" if not schema_errors else "FAIL",
            publishedToolTokenCompatibility="BLOCKED", ordinaryManifestResult="FAIL",
            ordinaryManifestExitCode=commands["manifest"]["exitCode"], adoptionExitCode=commands["adoption-json"]["exitCode"],
            adoptionTextExitCode=commands["adoption-text"]["exitCode"], trackedSourceOnly=True,
            additiveFilesTrackedAtObservation=False, sourceRevision=observation["sourceRevision"],
            passingRows=6, failingRows=18, failures=failures, runtimeExecuted=False, sm2Verified=False)
        report["historicalV1SourceDrift"] = [path for path, pin in historic.SOURCE_PINS.items()
                                               if SOURCE_PINS[path] != pin]
        for row in failures:
            codes = sorted({item["ruleId"] for item in adoption["diagnostics"]
                            if item["field"] == row["requirement"] and item["source"] == row["file"]})
            entry("MODULE_ADOPTION_FAILED", f"{' '.join(codes)} {row['requirement']} {row['file']}: {row['reason']}", "FAIL")
    except (InputProblem, ReadError) as error:
        state = "BLOCKED" if error.unavailable else "FAIL"
        report["assertionLedger"].append(dict(id="input-diagnostic", state=state))
        report["blockers"].append(error.code)
        report["diagnostics"].append(dict(code=error.code, detail=str(error)))
    except (OSError, GateError) as error:
        report["assertionLedger"].append(dict(id="input-diagnostic", state="BLOCKED"))
        report["blockers"].append("INPUT_UNAVAILABLE")
        report["diagnostics"].append(dict(code="INPUT_UNAVAILABLE", detail=str(error)))
    except (ValueError, KeyError, TypeError, AttributeError, RecursionError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
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
    print(json.dumps(report, indent=2, ensure_ascii=True))
    return {"PASS": 0, "FAIL": 1, "BLOCKED": 2}[report["result"]]


if __name__ == "__main__":
    raise SystemExit(main())
