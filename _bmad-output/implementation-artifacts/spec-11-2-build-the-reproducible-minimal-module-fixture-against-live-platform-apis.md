---
title: 'Story 11.2: Build the reproducible minimal-module fixture against live platform APIs'
type: 'feature'
created: '2026-10-18'
status: 'in-progress'
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
- [ ] `docs/release-evidence/thin-module-fixture-proof-v1.{json,md}`, `story-11.2-final-record-v2.{json,md}` -- regenerate and commit against the corrective source candidate

**Acceptance Criteria:**
- Given the committed candidate, when AC-11.2-01 to AC-11.2-07 run as in the contract, then each passes with zero skips.
- Given the generator run twice, when outputs are compared, then JSON and Markdown are byte-identical.
- Given any fault mutation, when the real checker runs, then it yields its exact blocker and bytes restore.

## Implementation Notes

- The prior inserted record is withdrawn for the third-pass repairs. Fresh proof and final-record pairs will bind the corrective committed candidate and six measured receipts; prior attempts remain in Git history.
- The fixture is a thin module over `ProjectReference`s to `references/Hexalith.EventStore` (`Contracts`, `Client`, `DomainService`, `Aspire`); package versions are pinned in the fixture `Directory.Build.props` and restore is locked by four `packages.lock.json` files.
- The inventory forces `ConversationModule.cs` to hold the registration extension, the projection, the query handler and the event observer; the AppHost composes the Server project by path (`IsAspireProjectResource="false"`) because the module has no host executable.
- Platform-hosted subscription/publication is proven in-process (owner decision): `/dapr/subscribe` and the domain-events route are sidecar-originated platform endpoints, and a published envelope reaches the module handler through `EventStoreDomainEventProcessor`.
- Restore state under `references/Hexalith.EventStore/**/obj` is shared with the umbrella `UseHexalithProjectReferences` build: run the locked restore immediately before the fixture build.
- `ScaffoldSmokeTest` now skips `tests/fixtures/`; the self-contained fixture owns its solution and inline package versions.

## Spec Change Log

- 2026-10-20: Implemented all twelve third-pass patch items. Restore/build now retain measured command receipts; proof and record validation bind the source commit, EventStore gitlink and unchanged source inputs. Release configuration propagates through transitive platform references. Added negative route, schema and serialization regressions.

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

### Review Findings (2026-10-19, third pass, `cc7647d..1f38912` without lock files and generated evidence)

- [x] [Review][Decision] AC-11.2-02 and AC-11.2-03 are recorded as exit 0 / PASS but never measured — The route hard-codes `exitCode=0, result='PASS'` and cites the proof JSON as both result files. The proof hard-codes `lockedMode` and `warningsAsErrors`, so a stale lock file that `--locked-mode` would reject still passes. "Repository-approved sources" is neither checked nor recorded: there is no `nuget.config`, and the frozen inventory allows none in the fixture. Resolved 2026-10-20 (Jerome): measured receipts. (blind-hunter+edge-case-hunter+acceptance-auditor, medium)
- [x] [Review][Patch] Measure AC-11.2-02 and AC-11.2-03: run each frozen command through a receipt wrapper that captures the exit code and console log (`artifacts/v9/11.2/AC-11.2-02.log`, `AC-11.2-03.log`). Bind both receipts in the route. Take `lockedMode` and `warningsAsErrors` in the proof from those receipts, and record the restore sources from `.nupkg.metadata` [_bmad/scripts/generate_story_record.py:10277]
- [x] [Review][Patch] Record-route fixture commits nothing on a clean tree, so all 26 `test_story_112_*` tests error at setup and `ci / repository` is red at `1f38912` (run 39204841905: 2038 passed, 26 errors); add `--allow-empty` [_bmad/scripts/tests/test_generate_story_record.py:12921] (verification-gap+acceptance-auditor, high)
- [x] [Review][Patch] The proof binds Debug-built platform assemblies under a Release build. All five `resolvedPublicApis.assemblies` embed `obj/Debug/net10.0` PDB paths and equal `references/Hexalith.EventStore/src/*/bin/Debug` outputs, which the umbrella Debug build also writes. A `.slnx` build sets `ShouldUnsetParentConfigurationAndPlatform` for the out-of-solution references, which drops `Configuration=Release`. Set it to `false` in the fixture props [tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props:4] (found in triage while verifying the path-bound hash claims, medium)
- [x] [Review][Patch] The record route's `rederive=False` branch skips the proof's lock files, guidance Markdown and source digests, and platform project hashes. A probe committed a lock-file edit after the proof, and the record still came out PASS, with live `fixture.lockFiles` differing from the stale `restore.lockFiles` [_bmad/scripts/verify_minimal_module_fixture.py:538] (blind-hunter+edge-case-hunter+acceptance-auditor+verification-gap, medium)
- [x] [Review][Patch] The proof's `candidate` does not bind SC-11.2: there is no commit and no `references/Hexalith.EventStore` gitlink, so platform identity rests on csproj hashes and build-dependent DLL hashes. Add both fields to the closed schema, the generator, the Markdown and `verify_proof`, and have the route check them against the candidate [_bmad/scripts/generate_minimal_module_fixture_proof.py:166] (blind-hunter+acceptance-auditor, medium)
- [x] [Review][Patch] No test exercises the `rederive=False` proof comparison: disabling it leaves all 26 route tests passing. Add a test that commits a fixture edit after the proof and expects rejection [_bmad/scripts/tests/test_generate_story_record.py:13042] (verification-gap, medium)
- [x] [Review][Patch] `v2_11_2_publication_check` is untested: `publication_check=None` leaves all 26 tests passing. Add a published-proof-drift variant [_bmad/scripts/tests/test_generate_story_record.py:13196] (verification-gap, medium)
- [x] [Review][Patch] The schema `else` test passes for the wrong reason (the 11.1 `thinGuidanceExecution` requirement); assert the `fixtureProofExecution` exclusion itself [_bmad/scripts/tests/test_generate_story_record.py:13082] (verification-gap, low)
- [x] [Review][Patch] The receipt-to-scenario digest loop and the `predecessors` check in `v2_11_2_pair_errors` are never reached; add `receipts[0].sha256` and `predecessors` mutations [_bmad/scripts/generate_story_record.py:10135] (verification-gap, low)
- [x] [Review][Patch] The AC-11.2-05 repository-only assertion is vacuous: the absolute path always contains `/tests/fixtures/`. Assert on the fixture-relative path [tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs:299] (edge-case-hunter, low)
- [x] [Review][Patch] The fixture projection binds payloads with default options instead of `EventStorePayloadSerialization.Options`, which every platform reader uses [tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs:62] (edge-case-hunter, low)
- [x] [Review][Patch] The ledger drops the triaged "empty Include in platform refs" item, and the three 11.2 entries sit under the 11.1 heading [_bmad-output/implementation-artifacts/deferred-work.md:600] (blind-hunter, low)
- [x] [Review][Defer] The AC-11.2-01 receipt does not carry HEAD or the predecessor digests, and the route sets the candidate from HEAD [_bmad/scripts/verify_minimal_module_fixture.py:556] — deferred: this extends the owner-accepted 11.1 deferral "receipts bound to the candidate only by mtime and name"; the route reuses `v2_11_1_read_receipt` (medium).
- [x] [Review][Defer] Assembly hashes embed the absolute checkout path (no `ContinuousIntegrationBuild`/`PathMap`), so `rederive=True` cannot reproduce at another path [tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props:4] — deferred: platform assemblies take the submodule's own props, which this story may not edit. This follows the existing path-bound record convention and is settled with the deferred fixture CI lane (medium).
- [x] [Review][Defer] The registration test never shows that the platform host discovered `ConversationAggregate`, and command handling calls the aggregate directly [tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs:48] — deferred: medium, unverified. Settle it by dispatching `CreateConversation` through the platform host's in-process command path.
- [x] [Review][Defer] The planning text says the 13 paths are in "ordinal order", but the frozen digest `4fa26d6f…` matches only the listed order (sorted order gives `49521de4…`) [_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md] — deferred: the fix edits planning text, and this frozen spec repeats the wording; the code follows the digest (low).

#### Rejected

- BH `dotnet_sdk_version` falls back to `global.json` — low: already in the ledger's 11.2 proof-generator robustness entry.
- BH/AA/EC conditional, `Choose` and `Target` property groups are ignored, and warning exemptions are not in the proof — low: NU1901–NU1904 and CA2007 mirror the umbrella `Directory.Build.props` and `tests/Directory.Build.props`, and a conditional bypass needs deliberate authoring.
- BH packability is checked only on the AppHost — low: the fixture props set `IsPackable=false` for every project, so an override needs a deliberate edit.
- BH/EC the `Validate` filter drops only `dapr/subscribe`, and the endpoint check looks only for `EndpointDataSource` subclasses — low: the verifier's `PLATFORM_RUNTIME_SOURCE` scan and the `module-authored-endpoint` fault cover module routes.
- BH publication is proven consumer-side and in-process — false: the 2026-10-18 platform-hosted-proof owner decision specifies exactly this.
- BH/EC/AA the AppHost composes a library it cannot run, and `platformHelper` is a constant — low: Implementation Notes acknowledge it, the frozen inventory has no host executable, and AC-11.2-05 asserts `AddAspireDaprDomainModule` in `AppHost.cs`.
- BH/AA the fault matrix accepts the expected blocker among several — low: adding or removing a project also drifts the frozen inventory.
- BH/EC TRX counters, nested `InnerResults` and lenient JUnit names — low: these are runner-produced receipts, and the change would be hardening only.
- BH bare `pytest.raises` without `match=` — low: the demonstrated gaps are carried as patches above.
- BH/EC/VG the reader-successor pin skips migration reconstruction and needs re-pinning per edit, and its `a3e6c15` test is collect-ignored — low: the pin design is from pre-story `7f67adb`, and CI still guards the rule through `test_approved_assertion_correction_preserves_every_other_entry_byte`.
- AA the `a3e6c15` re-pin of the 10.4 reader digests is undeclared — rejected: the fix edits this spec.
- BH/AA lifecycle bookkeeping (status, stale candidate in Implementation Notes, `review_loop_iteration`, baseline before `7f67adb`) — rejected: the fix edits this spec, and this review's status sync sets the lifecycle fields.
- BH/EC `ScaffoldSmokeTest` exempts all of `tests/fixtures/` — false: Implementation Notes record this as a deliberate rule for self-contained fixtures.
- BH the IdentityModel pin is not in the guidance, and the README omits prerequisites — low: the guidance is a Story 11.1 output (Never), and the contract commands are the run source of truth.
- BH the generator maps its own schema failure to BLOCKED — false: an own-output schema failure is a generator defect, not a fixture verdict.
- BH/EC receipts use `text eol=lf` — low: this is the same convention as the 10.4 receipts.
- BH/EC/VG `.slnx` project drift raises no blocker — low: the proof binds the slnx bytes, and drift needs a deliberate edit.
- EC/AA a symlinked frozen path skips the containment and LF checks — low: the route requires `fixtureFileCount == 13`, so no PASS record can follow.
- EC lowercase or semicolon package ids, a per-project lock opt-out, and the MSBuild 2003 xmlns — low: hardening only.
- EC the verifier passes without `--test-result` — low: every contract command supplies it.
- EC a malformed lock JSON gives a traceback — low: the input is NuGet-generated, and the run still exits nonzero.
- EC `verifier.Blocked` surfaces as `INTERNAL_ERROR` — low: the route still fails closed.
- EC the proof pair is already committed at the candidate — low: not a workflow path.
- EC a checkout path containing `bin` or `obj` — false: the C# test fails loudly.
- AA the internal-symbol and not-run rows have no fault — low: an internal reference fails to compile (CS0122), and not-run outcomes are counted.
- AA the module configures event subscription — false: `UseEventStoreDomainService` maps the routes only when a consumer is registered, and the module authors no route or worker.
- AA a source file under a nested `bin` or `obj` escapes the inventory — low: only deliberate evasion reaches it.
- AA `BLOCKED` is written as a blocker code — low: it is the exit-2 result state and never reaches the record.
- VG `ci / admin-web` is red in the same run — false for this diff: it is a Playwright `captureScreenshot` protocol error.

## Verification

**Third-pass repair checks (2026-10-20):**
- Both measured restore/build wrappers exited 0; all platform outputs use Release.
- Fixture native tests: 6 passed, zero skips. AppHost boundary native test: 1 passed.
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k story_112 --tb=short`: 37 passed. Static source-binding regression selection: 4 passed.
- Contracts build without restore encountered stale Commons converter dependencies; rebuilding with `--configuration Release -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` passed.
- Exact source-bound AC receipts and publication are retained in the final record below; the full two-file Python suite is also required before completion.

**Commands:**
- `dotnet restore tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --locked-mode` -- expected: exit 0
- `python3 -m pytest -q _bmad/scripts/tests/test_minimal_module_fixture.py _bmad/scripts/tests/test_generate_story_record.py` -- expected: pass

<!-- STORY-FINAL-RECORD:BEGIN -->
<!-- Fresh source-bound final record pending regeneration. -->
<!-- STORY-FINAL-RECORD:END -->
