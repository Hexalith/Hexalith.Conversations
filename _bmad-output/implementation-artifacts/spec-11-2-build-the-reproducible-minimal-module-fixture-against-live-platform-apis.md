---
title: 'Story 11.2: Build the reproducible minimal-module fixture against live platform APIs'
type: 'feature'
created: '2026-10-18'
status: 'done'
baseline_commit: 'cc7647d0c6f942e3f8b0cc3a204af30621089cc0'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 11.1 corrected the thin-module recipe, but nothing proves a module built from it restores, builds and passes tests using only public platform APIs, so SM-2 (Story 11.3) has no reproducible actual to count.

**Approach:** Add the frozen 13-path `tests/fixtures/Hexalith.Conversations.MinimalModule/` fixture, a Python verifier and proof generator with a closed schema, an xUnit AppHost-boundary test, a fault matrix, and a closed Story 11.2 final-record route (pattern: Story 11.1). Checkpoint B only: no SM-2 computation or decision.

## Boundaries & Constraints

**Always:** The fixture contains exactly the 13 paths of `V9-MINIMAL-MODULE-FIXTURE-v1` (SHA-256 `4fa26d6f…df15c`, NFC UTF-8 LF, ordinal order), source-controlled, hand-authored, LF-normalized; only `packages.lock.json` files (generated, excluded from cost) may sit beside them. One non-packable, non-publishable test AppHost; no `ServiceDefaults` or reusable Aspire project. Module owns contracts, domain behavior, registration and tests only. The fixture compiles warnings-as-errors and uses public platform types only (no `InternalsVisibleTo`, no reflection into internals). Proof schema `hexalith.conversations.thin-module-fixture-proof.v1` has fields `schemaVersion`, `candidate`, `guidanceDigest`, `fixtureInventory`, `resolvedPublicApis`, `restore`, `build`, `tests`, `appHost`, `digests`; JSON and Markdown are deterministic and bound by a signable-payload digest. Blockers are exactly the codes in `11.2.json`. Fault matrix mutations restore byte-identically.

**Never:** Compute or decide SM-2; alter Story 11.1 outputs, the v1 baseline, or SM-1 `13,289`; edit submodules; add packable fixture projects; reintroduce the retired authority/evidence gates.

**Owner decision, 2026-10-18 (candidate rule):** As in Story 11.1, AC-11.2-01 binds the committed `HEAD` and recomputes the Story 11.1 final-record v2 and guidance digests; no `verify_story_candidate.py` is built.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Valid fixture | 13 exact paths, lock files, tests run | PASS; proof JSON/MD deterministic | N/A |
| Inventory change | path added/removed/renamed/edited-by-CRLF | FAIL | `MINIMAL_FIXTURE_INVENTORY_DRIFT` |
| AppHost wrong | none, two, packable or publishable | FAIL | `APPHOST_BOUNDARY_INVALID` |
| Reusable Aspire / ServiceDefaults / platform runtime in module | project or reference added | FAIL | `REUSABLE_ASPIRE_LIBRARY_PRESENT` |
| Internal API used | `InternalsVisibleTo`/internal symbol | FAIL | `NONPUBLIC_PLATFORM_API_USED` |
| Vacuous tests | zero tests, or any skipped/not-run | FAIL | `MINIMAL_FIXTURE_TESTS_VACUOUS` |
| Proof drift | JSON/MD/result edited after generation | FAIL | `FIXTURE_PROOF_DRIFT` |

**Owner decision, 2026-10-18 (platform-hosted proof):** AC-11.2-04 proves subscription/publication in-process: tests drive the public `Hexalith.EventStore.DomainService` registration and dispatchers (projection, query) and assert the platform-owned subscription/publication endpoints exist in `EventStoreDomainServiceEndpointInventory` with no module-authored endpoint. No Dapr runtime or live pub/sub round trip.

**Owner decision, 2026-10-18 (spec size):** Keep the full spec despite exceeding 1600 tokens.

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/src/Hexalith.EventStore.{Contracts,Client,DomainService,Aspire}` -- public platform APIs the fixture references by `ProjectReference` (`AddEventStoreDomainService`, `EventStoreAggregate<TState>`, `IDomainProjectionHandler`, `IDomainQueryHandler`, `AddAspireDaprDomainModule`); package versions are unpublished, so no `PackageReference` to them.
- `src/Hexalith.Conversations.Server/*.csproj`, `src/Hexalith.Conversations.AppHost/*.csproj` -- reference shape (conditional `UseHexalithProjectReferences`, `Aspire.AppHost.Sdk/13.6.1`); fixture is self-contained because `references/Hexalith.Builds` is not initialized and the root `Directory.Packages.props` is not among the 13 paths.
- `_bmad-output/planning-artifacts/v9/story-contracts/11.2.json` -- frozen contract; do not edit.
- `_bmad/scripts/generate_thin_authoring_guidance.py`, `verify_thin_authoring_guidance.py`, `tests/test_thin_authoring_guidance.py` -- patterns for generator, verifier, `FAULT_EXPECTATIONS` single source and fault matrix.
- `_bmad/scripts/generate_story_record.py` (11.1 route at ~9811-10080), `_bmad/schemas/story-final-record-v2.schema.json`, `tests/test_generate_story_record.py` -- add the closed 11.2 route by copying the 11.1 shape.
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` -- add `MinimalFixtureShouldContainOneNonShippingAppHostAndNoReusableAspireLibrary`; CRLF; `RepositoryLocator`.
- `docs/release-evidence/thin-authoring-guidance-v2.*`, `story-11.1-final-record-v2.*`, v1 baseline -- predecessors/history; read-only.

## Tasks & Acceptance

**Execution:**
- [x] `tests/fixtures/Hexalith.Conversations.MinimalModule/**` (13 paths + generated `packages.lock.json`) -- author the fixture; `RestorePackagesWithLockFile`, warnings-as-errors, central versions in its `Directory.Build.props` -- reproducible locked restore
- [x] `docs/release-evidence/thin-module-fixture-proof-v1.schema.json` -- closed schema with the ten fields
- [x] `_bmad/scripts/verify_minimal_module_fixture.py` -- inventory, containment, LF, AppHost and project-graph facts, internal-API scan; exact blockers
- [x] `_bmad/scripts/generate_minimal_module_fixture_proof.py` -- deterministic JSON/MD from TRX, inventory, guidance digest, resolved platform project/assembly hashes
- [x] `tests/.../DomainModuleAuthoringTemplateValidationTest.cs` -- AppHost/no-reusable-library method
- [x] `_bmad/scripts/tests/test_minimal_module_fixture.py` -- `checkpoint_b_fault_matrix` over every I/O row; byte-identical restore
- [x] `_bmad/scripts/generate_story_record.py`, schema, `test_generate_story_record.py` -- closed 11.2 route, `8/8/0/0/0/0`, receipts under `artifacts/v9/11.2/`
- [x] `docs/release-evidence/thin-module-fixture-proof-v1.{json,md}`, `story-11.2-final-record-v2.{json,md}` -- generate and commit

**Acceptance Criteria:**
- Given the committed candidate, when AC-11.2-01 to AC-11.2-07 run as in the contract, then each passes with zero skips.
- Given the generator run twice, when outputs are compared, then JSON and Markdown are byte-identical.
- Given any fault mutation, when the real checker runs, then it yields its exact blocker and bytes restore.

## Implementation Notes

- Candidate `d2fd5e6`; publication `028bb39` (proof pair, four receipts, final-record v2 pair). The first publication (`0fc7542`) was withdrawn (`de7cbc2`) so hermetic-test and README fixes could become the candidate.
- The fixture is a thin module over `ProjectReference`s to `references/Hexalith.EventStore` (`Contracts`, `Client`, `DomainService`, `Aspire`); package versions are pinned in the fixture `Directory.Build.props` and restore is locked by four `packages.lock.json` files.
- The inventory forces `ConversationModule.cs` to hold the registration extension, the projection, the query handler and the event observer; the AppHost composes the Server project by path (`IsAspireProjectResource="false"`) because the module has no host executable.
- Platform-hosted subscription/publication is proven in-process (owner decision): `/dapr/subscribe` and the domain-events route are sidecar-originated platform endpoints, and a published envelope reaches the module handler through `EventStoreDomainEventProcessor`.
- Restore state under `references/Hexalith.EventStore/**/obj` is shared with the umbrella `UseHexalithProjectReferences` build: run the locked restore immediately before the fixture build.
- `ScaffoldSmokeTest` now skips `tests/fixtures/`; the self-contained fixture owns its solution and inline package versions.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH/EC query handler `ConversationTitleQuery` documented as "returning the projected title" but returns `AggregateId`; test asserts the id | low | patch | `ConversationModule.cs` query `ExecuteAsync` serializes `{ query.AggregateId }`; correct the doc comment to say it echoes the aggregate id. |
| VG2 `APPROVED_READER_SUCCESSORS` relaxation in `evidence_boundary_adoption.py` has no negative test | medium | patch | Only the definition and one use exist; a widened `digest == successor` check would not fail any test. Add a pytest where a successor file differs by a byte and `ASSERTION_STRENGTH_WEAKENED` still fires. |
| VG1 fixture restore/build/tests run in no CI lane; `rederive=False` in the record path; fault matrix uses stubs | medium (unverified lane gap) | defer | `ci.yml` has no fixture reference and `submodules: false`; a CI lane needs the EventStore submodule and Aspire toolchain, which the local-lane design excludes. Settled by adding such a lane. |
| BH fixture depends on the EventStore submodule through `FixtureEventStoreRoot` | false | reject | Project references into `references/Hexalith.EventStore` are the specified approach (package versions unpublished). |
| BH xunit 4.0.x pin vs umbrella 3.2.2; no guard against umbrella leak | maybe-false | defer | Fixture has its own slnx and is skipped by the umbrella smoke test; would settle by an umbrella restore with the fixture present. |
| BH one test carries both `commandHandling` and `eventProduction` | false | reject | The command handler produces the event in the same call; a separate test would assert the same result. |
| BH/EC projection `EndsWith` match, null/malformed payload, duplicate delivery, empty command ids, `Domain` literal duplicated | low | reject | Fixture is a minimal do-nothing module fed by the platform's well-formed envelopes; guards add complexity with no everyday harm. |
| BH weak non-public-API patterns; graph parser ignores Update/Remove; `MapGet` regex false positives; EC SDK-marker, `/AppHost` path exemption, `rsplit` name match, C# test null `Include`, AppHost name mismatch | low | reject | Hardening beyond the specified faults; all contract fault classes are detected by the matrix. |
| BH `.lock` tracking not enforced, `.gitattributes` for lock files, non-git checkout | low | reject | Lock files are tracked in the candidate; the pinned-rule hardening exceeds the frozen inventory contract. |
| BH CI timeout 40->70, `CommonsVersion` 2.30.1->2.30.3, attestation supersession of the template | low | reject | Come from the pre-story main commit `7f67adb` and the 11.1 corrective authority, not this story's change. |
| EC proof generator: stale DLL by mtime, `dotnet --version` vs `global.json`, output path containment, empty Include in platform refs | low | defer | Local-lane tool robustness; none affects the committed candidate. |
| EC `--inventory-only` still runs `check_predecessor` | false | reject | AC-11.2-01 requires the Story 11.1 record check with the inventory. |
| EC reader successor reports `ASSERTION_STRENGTH_WEAKENED` instead of a distinct code | low | reject | The frozen 10.4 code is the contract. |

## Verification

**Commands:**
- `dotnet restore tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --locked-mode` -- expected: exit 0
- `python3 -m pytest -q _bmad/scripts/tests/test_minimal_module_fixture.py _bmad/scripts/tests/test_generate_story_record.py` -- expected: pass

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 11.2 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `11.2`
- Candidate: `a3e6c15b5882f5f6ca4319af72fd8720a387ac97`
- JSON content SHA-256 (all three digest fields zeroed): `3fbf3cf5acf46d56106b7d83f7736f841dd6ebe8ba4aa2aca53e802ea75e8589`

## Authority

| Field | Value |
| --- | --- |
| Epic | `epic-6-authority-2026-08-03-v10` |
| Architecture | `conversations-architecture-2026-08-03-v10` |
| Planning candidate | `1e9a61126d3b7a55b514b7c7c8942d5af03355e5` |
| Bundle digest | `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055` |

## Root gitlinks

| Path | Mode | Commit |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `160000` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `160000` | `1dfc39e3a3e6ab1c8ce95138490b39d76e7feb2c` |
| `references/Hexalith.Commons` | `160000` | `5c23c818f639c5bb3175f964c5549440bd5b876d` |
| `references/Hexalith.EventStore` | `160000` | `ce1eba0c459f4a00b2e555668e2e548af81f34f5` |
| `references/Hexalith.Folders` | `160000` | `c22b13137202f7bf8c60b80c095a69748435bd6e` |
| `references/Hexalith.FrontComposer` | `160000` | `50f4acbc56192b2ad42b912aa6a83edf3bf2c264` |
| `references/Hexalith.McpCli` | `160000` | `3cd4d33e71e03d81b1961e236abff3111944330c` |
| `references/Hexalith.Memories` | `160000` | `4f6b3b1265d15d208761e6419e5c825d3f39fea3` |
| `references/Hexalith.Parties` | `160000` | `749b26d303e448bddafb1e56402bfe676bc92d7f` |
| `references/Hexalith.Platform` | `160000` | `cf20db9e8a4697b0e9fb82d388751d355088e942` |
| `references/Hexalith.Projects` | `160000` | `42fec8c61bd698affb390925bc12a628d8e5eccc` |
| `references/Hexalith.Tenants` | `160000` | `1532aef62d4532562a5837447463297e64358333` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-11.2-ENTRY-v1` | `2aa59728bdc866263bf31a5f258fb80935fa07485459f795f07f6eb60eb123ac` |

## Predecessors

- `11.1`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-11.2-01` | `0` | `PASS` | `none` | `3` | `artifacts/v9/11.2/AC-11.2-01.json` | `04460bce7bbeb2fc0026b4d7d929a265e3d43fdd0703a8877d756a6310ea75b8` |
| `AC-11.2-02` | `0` | `PASS` | `none` | `1` | `docs/release-evidence/thin-module-fixture-proof-v1.json` | `921bfd0bfeb9630721e30c93edc4fdaa7d31c83f1a5563b9c4d607c72b584202` |
| `AC-11.2-03` | `0` | `PASS` | `none` | `1` | `docs/release-evidence/thin-module-fixture-proof-v1.json` | `921bfd0bfeb9630721e30c93edc4fdaa7d31c83f1a5563b9c4d607c72b584202` |
| `AC-11.2-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/minimal-module.trx` | `d9c4d0ba933fb7eb749139d910769e9549eb304e758407c05ae595a4d1c4ccb9` |
| `AC-11.2-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/AC-11.2-05.trx` | `a2b5f46824aa88840a5ec349a6a972417fd4daa26903ebb04d3f53a6b182a7d0` |
| `AC-11.2-06` | `0` | `PASS` | `none` | `2` | none | none |
| `AC-11.2-07` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/AC-11.2-07.xml` | `e20e7d9d943015d8635f92c029c624c14bacc971fde459470089449dc07c0ae6` |
| `AC-11.2-08` | `0` | `PASS` | `none` | `2` | none | none |

### `AC-11.2-01`

Command: `python3 _bmad/scripts/verify_minimal_module_fixture.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.2.json --fixture tests/fixtures/Hexalith.Conversations.MinimalModule --inventory-only --output artifacts/v9/11.2/AC-11.2-01.json`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-01#0001` | `exact frozen fixture inventory verified at the committed candidate` | `PASS` |
| `AC-11.2-01#0002` | `candidate is the committed HEAD` | `PASS` |
| `AC-11.2-01#0003` | `Story 11.1 final-record v2 and guidance digests recomputed` | `PASS` |

### `AC-11.2-02`

Command: `dotnet restore tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --locked-mode`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-02#0001` | `locked restore: every project has a committed lock file and the proof binds the resolved packages` | `PASS` |

### `AC-11.2-03`

Command: `dotnet build tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --configuration Release --no-restore`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-03#0001` | `warnings-as-errors Release build: the proof binds the fixture and public platform assembly hashes` | `PASS` |

### `AC-11.2-04`

Command: `dotnet tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Tests.dll -automated sync -failSkips -trx artifacts/v9/11.2/minimal-module.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-04#0001` | `non-vacuous minimal-module tests with zero failed, skipped or not-run` | `PASS` |

### `AC-11.2-05`

Command: `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.DomainModuleAuthoringTemplateValidationTest.MinimalFixtureShouldContainOneNonShippingAppHostAndNoReusableAspireLibrary -trx artifacts/v9/11.2/AC-11.2-05.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-05#0001` | `MinimalFixtureShouldContainOneNonShippingAppHostAndNoReusableAspireLibrary` | `PASS` |

### `AC-11.2-06`

Command: `python3 _bmad/scripts/generate_minimal_module_fixture_proof.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.2.json --fixture tests/fixtures/Hexalith.Conversations.MinimalModule --test-result artifacts/v9/11.2/minimal-module.trx --output-json docs/release-evidence/thin-module-fixture-proof-v1.json --output-markdown docs/release-evidence/thin-module-fixture-proof-v1.md`

| Bound output | SHA-256 |
| --- | --- |
| `docs/release-evidence/thin-module-fixture-proof-v1.json` | `921bfd0bfeb9630721e30c93edc4fdaa7d31c83f1a5563b9c4d607c72b584202` |
| `docs/release-evidence/thin-module-fixture-proof-v1.md` | `70fa94c639578c537ec5b4d481544c6ce87fe1fedcfa20193476386cf996361a` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-06#0001` | `proof JSON and Markdown are deterministic, schema-valid and bound by the signable-payload digest` | `PASS` |
| `AC-11.2-06#0002` | `proof binds commands, tool versions, public API hashes, AppHost facts, inventory and results` | `PASS` |

### `AC-11.2-07`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_minimal_module_fixture.py -k checkpoint_b_fault_matrix --junitxml=artifacts/v9/11.2/AC-11.2-07.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-07#0001` | `checkpoint B fault matrix, every mutation detected and restored` | `PASS` |

### `AC-11.2-08`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.2.json --format bundle --output-json docs/release-evidence/story-11.2-final-record-v2.json --output-markdown docs/release-evidence/story-11.2-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-08#0001` | `AC-11.2-01 through AC-11.2-07 and Story 11.1 are current and compatible` | `PASS` |
| `AC-11.2-08#0002` | `record binds SC-11.2, Story 11.1, fixture, proof, receipts and mutation ledger` | `PASS` |

## Minimal-module fixture proof

- Story 11.1 record: `docs/release-evidence/story-11.1-final-record-v2.json` (`6ff02bf2f6a71dea275c420d7860a9a62b9423d245046703b8c906363a91667a`)
- Guidance bundle: `4f078797f4b5c85aef91b3ea9cedd29dac25a3f28b62dcdca1d8a2db4335c0e2`; signable payload `9ce146a26cd693261392928a2f767d3752b0101f6097e8a0bc7757784a07ef80`
- Fixture inventory `V9-MINIMAL-MODULE-FIXTURE-v1`: `4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c` (13 paths under `tests/fixtures/Hexalith.Conversations.MinimalModule`)
- Fixture sources: `60ceac3a6acc59ed8339cc6883301963ab5754b41184afb54b2736c17e2e3a90`
- Proof JSON: `docs/release-evidence/thin-module-fixture-proof-v1.json` (`921bfd0bfeb9630721e30c93edc4fdaa7d31c83f1a5563b9c4d607c72b584202`)
- Proof Markdown: `docs/release-evidence/thin-module-fixture-proof-v1.md` (`70fa94c639578c537ec5b4d481544c6ce87fe1fedcfa20193476386cf996361a`)
- Proof schema: `docs/release-evidence/thin-module-fixture-proof-v1.schema.json` (`87aeb0afc4f8a91428da562dc139beada0a00ffbc19bc7ac4318eb0ed04c06aa`)
- Proof signable payload: `4250d304520acb499909b0418eb73c58917dcf037f75348129b983e401743ad6`
- Resolved public APIs: `6186e01d8a022a03580b017f2074539991110f6f0d34b79ecda253cc092b9ae1`
- Measured test result: `d9c4d0ba933fb7eb749139d910769e9549eb304e758407c05ae595a4d1c4ccb9`

| Lock file | SHA-256 |
| --- | --- |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/packages.lock.json` | `52cbaccb8876b9df65fa7e691cf18b6755378e69aa50451ad5e4e211fcac0314` |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/packages.lock.json` | `d9f8362c9a38b3ad08481559e89dbebc92c474a06e1579923182f4419796295c` |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/packages.lock.json` | `78b06f0a95f4be921ace42500c2eba6e7578c0dccb162a2400d838c9b15a2423` |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/packages.lock.json` | `7a55c490e0263300eb97079fffc684ca61b1f00acd057bcf213f7f743744d104` |

| Receipt | Path | SHA-256 |
| --- | --- | --- |
| `AC-11.2-01` | `artifacts/v9/11.2/AC-11.2-01.json` | `04460bce7bbeb2fc0026b4d7d929a265e3d43fdd0703a8877d756a6310ea75b8` |
| `AC-11.2-04` | `artifacts/v9/11.2/minimal-module.trx` | `d9c4d0ba933fb7eb749139d910769e9549eb304e758407c05ae595a4d1c4ccb9` |
| `AC-11.2-05` | `artifacts/v9/11.2/AC-11.2-05.trx` | `a2b5f46824aa88840a5ec349a6a972417fd4daa26903ebb04d3f53a6b182a7d0` |
| `AC-11.2-07` | `artifacts/v9/11.2/AC-11.2-07.xml` | `e20e7d9d943015d8635f92c029c624c14bacc971fde459470089449dc07c0ae6` |

## Fault injection

30 observed mutation results are bound to the retained receipts.

| Fault | Expected blocker | Detected | Restored |
| --- | --- | --- | --- |
| `inventory-path-added` | `MINIMAL_FIXTURE_INVENTORY_DRIFT` | `True` | `True` |
| `inventory-path-removed` | `MINIMAL_FIXTURE_INVENTORY_DRIFT` | `True` | `True` |
| `inventory-path-renamed` | `MINIMAL_FIXTURE_INVENTORY_DRIFT` | `True` | `True` |
| `inventory-crlf-edit` | `MINIMAL_FIXTURE_INVENTORY_DRIFT` | `True` | `True` |
| `inventory-lock-file-misplaced` | `MINIMAL_FIXTURE_INVENTORY_DRIFT` | `True` | `True` |
| `apphost-none` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `apphost-second` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `apphost-packable` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `apphost-publishable` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `reusable-aspire-library-added` | `REUSABLE_ASPIRE_LIBRARY_PRESENT` | `True` | `True` |
| `service-defaults-project-added` | `REUSABLE_ASPIRE_LIBRARY_PRESENT` | `True` | `True` |
| `service-defaults-reference-added` | `REUSABLE_ASPIRE_LIBRARY_PRESENT` | `True` | `True` |
| `platform-runtime-project-reference-added` | `REUSABLE_ASPIRE_LIBRARY_PRESENT` | `True` | `True` |
| `dapr-package-added` | `REUSABLE_ASPIRE_LIBRARY_PRESENT` | `True` | `True` |
| `module-authored-endpoint` | `REUSABLE_ASPIRE_LIBRARY_PRESENT` | `True` | `True` |
| `internals-visible-to-item` | `NONPUBLIC_PLATFORM_API_USED` | `True` | `True` |
| `internals-visible-to-attribute` | `NONPUBLIC_PLATFORM_API_USED` | `True` | `True` |
| `nonpublic-reflection` | `NONPUBLIC_PLATFORM_API_USED` | `True` | `True` |
| `lock-file-missing` | `MINIMAL_FIXTURE_RESTORE_FAILED` | `True` | `True` |
| `lock-file-policy-disabled` | `MINIMAL_FIXTURE_RESTORE_FAILED` | `True` | `True` |
| `warnings-as-errors-disabled` | `MINIMAL_FIXTURE_BUILD_FAILED` | `True` | `True` |
| `build-output-missing` | `MINIMAL_FIXTURE_BUILD_FAILED` | `True` | `True` |
| `tests-zero` | `MINIMAL_FIXTURE_TESTS_VACUOUS` | `True` | `True` |
| `tests-skipped` | `MINIMAL_FIXTURE_TESTS_VACUOUS` | `True` | `True` |
| `tests-failed` | `MINIMAL_FIXTURE_TESTS_VACUOUS` | `True` | `True` |
| `capability-test-missing` | `MINIMAL_FIXTURE_TESTS_VACUOUS` | `True` | `True` |
| `proof-json-edited` | `FIXTURE_PROOF_DRIFT` | `True` | `True` |
| `proof-markdown-edited` | `FIXTURE_PROOF_DRIFT` | `True` | `True` |
| `result-edited` | `FIXTURE_PROOF_DRIFT` | `True` | `True` |
| `fixture-edited-after-proof` | `FIXTURE_PROOF_DRIFT` | `True` | `True` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-11.2-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-11.2-final-record-v2.md` |

## Rollback boundary

remove only the frozen fixture, fixture proof generator, validator, results, generated proof, and Story 11.2 record; retain Story 11.1.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `8` | `8` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
