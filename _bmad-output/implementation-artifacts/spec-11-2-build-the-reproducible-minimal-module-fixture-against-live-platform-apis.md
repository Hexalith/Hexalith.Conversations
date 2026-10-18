---
title: 'Story 11.2: Build the reproducible minimal-module fixture against live platform APIs'
type: 'feature'
created: '2026-10-18'
status: 'in-review'
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
