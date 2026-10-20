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
- [x] `docs/release-evidence/thin-module-fixture-proof-v1.{json,md}`, `story-11.2-final-record-v2.{json,md}` -- regenerate and commit against the corrective source candidate

**Acceptance Criteria:**
- Given the committed candidate, when AC-11.2-01 to AC-11.2-07 run as in the contract, then each passes with zero skips.
- Given the generator run twice, when outputs are compared, then JSON and Markdown are byte-identical.
- Given any fault mutation, when the real checker runs, then it yields its exact blocker and bytes restore.

## Implementation Notes

- The fourth-pass review withdrew publication `05d3820` (candidate `71aa15e`) for its repairs. Fresh proof and final-record pairs bind the corrective committed candidate and six measured receipts; prior attempts remain in Git history.
- The fixture is a thin module over `ProjectReference`s to `references/Hexalith.EventStore` (`Contracts`, `Client`, `DomainService`, `Aspire`); package versions are pinned in the fixture `Directory.Build.props` and restore is locked by four `packages.lock.json` files.
- The inventory forces `ConversationModule.cs` to hold the registration extension, the projection, the query handler and the event observer; the AppHost composes the Server project by path (`IsAspireProjectResource="false"`) because the module has no host executable.
- Platform-hosted subscription/publication is proven in-process (owner decision): `/dapr/subscribe` and the domain-events route are sidecar-originated platform endpoints, and a published envelope reaches the module handler through `EventStoreDomainEventProcessor`.
- Restore state under `references/Hexalith.EventStore/**/obj` is shared with the umbrella `UseHexalithProjectReferences` build: run the locked restore immediately before the fixture build.
- `ScaffoldSmokeTest` now skips `tests/fixtures/`; the self-contained fixture owns its solution and inline package versions.

## Spec Change Log

- 2026-10-20 (fourth pass): Re-pinned the reader-successor digest of the edited AppHost test, which turned `ci / repository` red at `05d3820`. The verifier CLI takes `--candidate`, and re-derivation binds it, so a committed proof re-verifies. The measured locked restore first removes the fixture no-op caches. The generator requires each platform assembly to equal its Release output, the platform check counts untracked sources, and static proof verification also binds package sources and the AppHost SDK. The evidence-wrapper usage left the counted README, and the third-pass deferrals moved to the ledger. Restricting the Release policy to platform references was reverted after measurement (Debug builds through the SDK's transitive references), so the fixture's build files keep their `71aa15e` bytes.
- 2026-10-20 (build review, `71aa15e`): Static proof verification derives test facts from the retained TRX and lock files and commands from the frozen contract; measurement and proof acceptance require the platform checkout at the candidate gitlink.
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

### Build review triage (2026-10-20)

All three workflow layers completed before classification. Rows below retain every finding before grouping.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH01 historical package-refresh profile now rejects accepted October 8 evidence | medium | defer | Reproduced `python3 _bmad/scripts/check_package_refresh_environment.py --repository . --check`: exit 1, `REFRESH_SDK_DRIFT`. The changed pins originate in pre-existing commit `7f67adb8bb67b668dd37d64e9d4795e37dcfb758`; record a separate historical-profile repair. |
| BH02 execution receipts lack candidate/input/output digests | medium | carried defer | Carried: the owner-accepted 11.1 receipt binding and third-pass AC-11.2-01 deferral use `v2_11_1_read_receipt` freshness/name checks. The new command receipts use that same consumer; do not create another provenance scheme in this patch. |
| BH03 advertised EventStore gitlink can differ from actual checkout | medium | patch | The root status helper ignores submodules, while project and assembly hashes read their checkout. Require platform HEAD and tracked-source cleanliness to match the candidate gitlink. |
| BH04 static proof accepts contradictory test totals/cases/capabilities/xUnit | medium | patch | Re-signed proofs with invented totals pass `rederive=False`; derive the deterministic test facts from the retained TRX and locked packages. |
| BH05 static proof accepts contradictory commands and SDK text | medium | patch / carried limitation | Compare all three execution commands with the frozen contract. SDK probing/fallback robustness remains the existing deferred generator item; this patch adds no execution-receipt schema. |
| BH06 package origins lack a new approved-feed policy | medium | carried owner decision | Carried: the third-pass owner decision resolved the approved-sources finding with measured receipts and actual `.nupkg.metadata` origins. The measured fixture resolves solely from nuget.org; no new feed-approval policy was requested. |
| BH07 omitted solution entries escape the graph checker | low | carried reject | Carried: the prior `.slnx` drift finding was rejected because the proof binds its bytes and the bypass requires deliberate fixture editing. |
| BH08 conditional AppHost packability escapes static parsing | low | carried reject | Carried: conditional/Choose/Target bypasses were rejected as deliberate authoring; the normal fixture has unconditional non-shipping properties. |
| BH09 symlinked expected fixture path yields twelve facts | low | carried reject | Carried: the third-pass symlink finding was rejected because a closed record requires thirteen fixture facts, so no PASS record follows. |
| BH10 no live fixture CI lane | medium (unverified lane gap) | carried defer | Carried: the existing ledger already requires an EventStore/Aspire fixture execution lane. Local fixture execution passed; do not duplicate the deferred entry. |
| BH11 AppHost composes the module library as a runnable resource | low | carried reject | Carried: the frozen in-process proof decision and implementation notes explicitly acknowledge this fixture shape; no runtime AppHost round trip is claimed. |
| EC01 re-signed proof accepts invented deterministic execution facts | medium | patch | Same demonstrated test-count, case, xUnit and command contradictions as BH04/BH05. Recorded package origins remain governed by the existing measured-source decision. |
| EC02 platform source revision/dirt is ignored | medium | patch | Same platform checkout binding defect as BH03; check both measured proof generation and record acceptance. |
| EC03 dangling README symlink passes inventory | low | carried reject | Carried: the same twelve-fact inventory bypass and thirteen-fact record guard were adjudicated in the third pass. |
| EC04 empty solution passes graph validation | low | carried reject | Carried: the same solution-drift finding remains a deliberate-edit hardening item. |
| EC05 aborted TRX summary is not reconciled with result rows | low | carried reject | Carried: the third-pass TRX-counter finding was rejected as runner-receipt hardening. Native fixture execution in this run exited zero with six results and no skips. |
| EC06 second proof output write fails after replacing JSON | low | reject | A filesystem failure exits generation and the resulting mismatched pair is rejected by proof verification. Transactional rollback adds branches for an uncommon local-tool failure. |
| VG01 normal CI does not execute the live fixture | medium (unverified lane gap) | carried defer | Carried: same previously recorded fixture CI lane; local tests and the retained proof are current for this source attempt. |
| VG02 static proof counts and commands can contradict retained inputs | medium | patch | The review's hermetic probes demonstrate accepted contradictory metadata. Add focused negative coverage for source-derived deterministic facts. |

Surviving patch groups: deterministic proof metadata (BH04/BH05/EC01/VG02), and platform checkout identity (BH03/EC02). Both groups are fixed: 53 fixture-verifier tests and 39 Story 11.2 route tests passed. The pre-existing package-refresh profile defect is separately deferred.

Final verification: `python3 -m pytest -q _bmad/scripts/tests/test_minimal_module_fixture.py _bmad/scripts/tests/test_generate_story_record.py --junitxml=/tmp/story112-full-validation.xml` exited 0: **1434 passed, zero failed/skipped**, with 26 existing JUnit `record_property` warnings. Both Story 7.3 completion-workflow guards passed. All eight Story 11.2 acceptance scenarios passed against the final source candidate, both output pairs were byte-identical across repeated generation, and the final ten-path atomic publication passed insertion verification. The retired evidence-boundary verifier is **not-applicable** under this story's current-change contract. Native fixture tests: 6 passed; AppHost boundary: 1 passed.

### Review Findings (2026-10-20, fourth pass, `1f38912..364889e` without lock files and generated evidence)

- [x] [Review][Decision] `ci / repository` is red at the publication, and its fix is a source change the completion gate forbids after the record-only commit — Line 299 of `DomainModuleAuthoringTemplateValidationTest.cs` changed the file digest from `e50659d3…` to `0888d86a…`, but `APPROVED_READER_SUCCESSORS` still pins `e50659d3…`. CI run 39362622671 at `05d3820` (job 118432123438): 3 failed, 2093 passed (`test_approved_assertion_correction_preserves_every_other_entry_byte[none]`, `test_complete_fault_matrix_readers[EXEMPTION…]`, `[READER_INVENTORY…]`). The re-pin, and every patch below, is a later source commit that `v2_11_1_descendant_errors` rejects ("publication boundary or later source changed"). Choose: withdraw the publication and rebind against a corrective candidate, or accept the current record and land fixes as routine post-done work (the 9.2 precedent). Resolved 2026-10-20 (Jerome): withdraw and rebind; the re-pin rides in the corrective candidate [_bmad/scripts/evidence_boundary_adoption.py:32] (acceptance-auditor, high)
- [x] [Review][Decision] The published proof cannot be re-verified after it is committed — `verify_proof` compares `candidate.commit` with `candidate_commit or HEAD`, the verifier CLI has no candidate option, and `rederive=True` re-derives through `build_signable`, which always binds HEAD. Measured at `364889e`: `verify_minimal_module_fixture.py … --proof-json docs/release-evidence/thin-module-fixture-proof-v1.json` exits 1 with `FIXTURE_PROOF_DRIFT`; with `candidate_commit='71aa15e…'` and `rederive=True` the only differing field is `candidate.commit`. A proof is always committed after its candidate, so the real checker never passes on the committed repository, and Story 11.3 must bind this proof. Choose: thread the candidate through `build_signable` and add `--candidate` to the CLI, or take the candidate from the proof (ancestor of HEAD, same EventStore gitlink), or defer to Story 11.3. Resolved 2026-10-20 (Jerome): thread the candidate through `build_signable` and add `--candidate` [_bmad/scripts/verify_minimal_module_fixture.py:588] (edge-case-hunter+acceptance-auditor, medium)
- [x] [Review][Patch] A no-op locked restore does not validate lock files, and the retained AC-11.2-02 receipt is a no-op ("All projects are up-to-date for restore"). Reproduced in a scratch project: after editing `resolved` in `packages.lock.json`, `dotnet restore --locked-mode` exits 0 as a no-op; with `obj/project.nuget.cache` removed it fails `NU1403`. The wrapper must remove the four fixture `obj/project.nuget.cache` files before running the frozen command [_bmad/scripts/run_minimal_module_command.py:28] (blind-hunter+acceptance-auditor, medium)
- [x] [Review][Defer] The `ProjectReference` `ItemDefinitionGroup` gives in-solution references a global property the solution build lacks, so `MinimalModule.Contracts` and `MinimalModule.Server` each build twice (retained `AC-11.2-03.log`; reproduced with a two-project scratch `.slnx`, single build without it) [tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props:27] — deferred: the per-reference patch, measured at `cb11f93`, built each fixture project once, but the SDK's transitive references of the Tests project lack that metadata, so `Client`, `ServiceDefaults` and `Contracts` also built in Debug into the shared `references/Hexalith.EventStore/src/*/bin/Debug`. The frozen 13-path inventory admits no build file that sets the policy globally, so the measured-good `ItemDefinitionGroup` is restored. Settle by listing the platform projects in the fixture `.slnx` if the thin-module recipe accepts it (blind-hunter+edge-case-hunter, medium)
- [x] [Review][Patch] `verify_proof(rederive=False)` never compares `restore.sources` or `build.toolVersions.aspireAppHostSdk`, so a re-signed proof with an invented feed or SDK passes the record route — the same class as the fixed BH04/EC01 [_bmad/scripts/verify_minimal_module_fixture.py:640] (blind-hunter+edge-case-hunter+acceptance-auditor, medium)
- [x] [Review][Patch] `restore_sources` is pinned only to the value a hard-coded default returns: replacing its body with `return ["https://api.nuget.org/v3/index.json"]` passes every test. Add a non-default-feed case and a missing-metadata case expecting `MINIMAL_FIXTURE_RESTORE_FAILED` [_bmad/scripts/tests/test_minimal_module_fixture.py:143] (verification-gap, medium)
- [x] [Review][Patch] No test feeds a successful wrapper receipt to `command_receipt`: adding a key to the wrapper's receipt passes every test but breaks every real receipt. Extend the wrapper test with an exit-0 run accepted by `verifier.command_receipt` [_bmad/scripts/tests/test_minimal_module_fixture.py:305] (verification-gap+blind-hunter, medium)
- [x] [Review][Patch] The wrapper's `TreatWarningsAsErrors` evaluation can be replaced by `enabled = True` with every test passing; the fake msbuild always answers `true`. Add msbuild-`false` and msbuild-failure cases [_bmad/scripts/run_minimal_module_command.py:35] (verification-gap+blind-hunter, medium)
- [x] [Review][Patch] The proof-to-receipt binding checks in `verify_proof` and the route are unreached: deleting both passes every test. Add a valid but different restore receipt in a `verify_proof` test and in a route test through `read_receipt` [_bmad/scripts/verify_minimal_module_fixture.py:637] (verification-gap+acceptance-auditor, medium)
- [x] [Review][Patch] Nothing checks that the hashed platform assemblies were built in Release, so the third-pass Debug-binding defect could return silently. Compare each resolved platform assembly with its `bin/Release/net10.0` output in `build_signable` and add a hermetic test [_bmad/scripts/generate_minimal_module_fixture_proof.py:115] (verification-gap, medium)
- [x] [Review][Patch] The platform checkout check runs `git status --untracked-files=no`, but SDK globbing compiles an untracked `*.cs` under `references/Hexalith.EventStore/src/**` into the bound assemblies; the live checkout is clean, so `--untracked-files=all` costs nothing [_bmad/scripts/verify_minimal_module_fixture.py:213] (blind-hunter+edge-case-hunter+acceptance-auditor+verification-gap, low)
- [x] [Review][Patch] The fixture README, one of the 13 counted paths, now carries repository evidence instructions; the wrapper docstring already gives the usage. Remove the nine added lines [tests/fixtures/Hexalith.Conversations.MinimalModule/README.md:20] (blind-hunter+acceptance-auditor, low)
- [x] [Review][Patch] `restore_sources` iterates `sorted(folders)`, dropping NuGet's `packageFolders` precedence; keep assets-file order [_bmad/scripts/generate_minimal_module_fixture_proof.py:74] (edge-case-hunter, low)
- [x] [Review][Patch] The four third-pass `[Review][Defer]` items are absent from the ledger, and the build-review package-refresh entry sits under the 2026-10-19 heading; transfer them in the corrective candidate, which may still edit the ledger [_bmad-output/implementation-artifacts/deferred-work.md:593] (blind-hunter+acceptance-auditor, low)

#### Rejected

- BH restore sources come from the shared cache's `.nupkg.metadata` — rejected: the third-pass owner decision chose exactly those origins.
- BH/AA `lockedMode` is `"--locked-mode" in arguments` — low: the frozen command carries the flag, so the value is accurate; the real gap is the no-op patch above.
- BH/AA the wrapper and `verify()` report platform drift as `FIXTURE_PROOF_DRIFT` with exit 1 — low: the message names the cause and the code is in the closed set.
- BH the wrapper does not check fixture or root cleanliness — low: needs an edit-measure-revert sequence; same class as the carried BH02 receipt-provenance deferral.
- BH the build log ends with an exempted `NU1902` (MessagePack 2.5.302) — low: the NU1901–NU1904 exemption was adjudicated in the third pass; the package comes through the platform Aspire dependency.
- BH/EC `verify_proof` reports an invalid receipt as "the proof is malformed" and returns platform blockers early — low: it still fails closed, and the generator reports the receipt error precisely first.
- BH stale or symlinked `.log` receipts are untested — low: they use the tested `v2_11_1_read_receipt` reader.
- BH the fault matrix has no restore/build row — false: AC-11.2-07 covers the spec's I/O rows, which list none.
- BH payload serialization is fixed in one call only — low: `EventStorePayloadSerialization.Options` uses Web defaults with case-insensitive reads, so no payload misreads.
- BH the Verification section, Spec Change Log and Implementation Notes are stale — rejected: the fix edits this spec.
- BH the `.log` receipts are git-ignored — false: `/artifacts/v9/*` ignores every 11.2 receipt, published by force-add like 10.4 and 11.1.
- BH the scenario-to-policy mapping is duplicated — low: the receipt-shape patch above covers producer/consumer drift.
- EC malformed `project.assets.json` or `.nupkg.metadata` gives a traceback — low: NuGet-generated input, as in the third pass.
- EC a failed wrapper run leaves an older receipt — false: the route rejects receipts older than the candidate, and a surviving same-candidate receipt is still a valid measurement.
- AA the final-verification text claims a green run with the evidence-boundary verifier not-applicable — rejected: the fix edits this spec; the CI failure is the first decision.
- AA AC-11.2-04 registration is partial — already deferred in the third pass.

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
<!-- Fresh source-bound final record pending fourth-pass remeasurement. -->
<!-- STORY-FINAL-RECORD:END -->
