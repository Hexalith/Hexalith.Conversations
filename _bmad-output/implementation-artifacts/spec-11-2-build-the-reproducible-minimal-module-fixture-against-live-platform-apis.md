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
- [x] `docs/release-evidence/thin-module-fixture-proof-v1.{json,md}`, `story-11.2-final-record-v2.{json,md}` -- regenerate and commit against the corrective source candidate

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
# Story 11.2 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `11.2`
- Candidate: `71aa15e639c8cea15c46aa1f159df38cb363d06a`
- JSON content SHA-256 (all three digest fields zeroed): `495cd50849cf24002e321d76ec0c440e53b60ecff77787108780048a29ac4fb9`

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
| `AC-11.2-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/AC-11.2-02.log` | `7a99d7964c9f2f2600bbef90d2f4c9d5a22cdbb6f2aaffdb6935ae4b03ff5e82` |
| `AC-11.2-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/AC-11.2-03.log` | `760f7d83331e33863f8d2ee3fd9ae4d23f7371fa37b6971f16a1f8e630e23e64` |
| `AC-11.2-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/minimal-module.trx` | `09f4e57fdee7c3650a4c26dee24cd45cf839e4fdc2541c500692d852b7592707` |
| `AC-11.2-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/AC-11.2-05.trx` | `1efa85b591928692c310e9ff5c0bbde56b389736b15c8c2653162a314991ffd8` |
| `AC-11.2-06` | `0` | `PASS` | `none` | `2` | none | none |
| `AC-11.2-07` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.2/AC-11.2-07.xml` | `7a45e671b44816849fd8ae5e15f72b63b96f4ef4224e71e4302a6b76bce3259f` |
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
| `AC-11.2-02#0001` | `measured locked restore with resolved NuGet sources` | `PASS` |

### `AC-11.2-03`

Command: `dotnet build tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --configuration Release --no-restore`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.2-03#0001` | `measured warnings-as-errors Release build using public platform APIs` | `PASS` |

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
| `docs/release-evidence/thin-module-fixture-proof-v1.json` | `5f875010f891efd1c872e4624a67217f3f77ae3509bec504291ca7956f0d3eb8` |
| `docs/release-evidence/thin-module-fixture-proof-v1.md` | `e3d0c22ec0d887cef398f8faf2fad633128dbc83424cfbb49fb03a9060d87252` |

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
- Fixture sources: `5b8821187234c3cacb503139173b9bd2c655cc1746b9e36a4c69e3ab3ee6048f`
- Proof JSON: `docs/release-evidence/thin-module-fixture-proof-v1.json` (`5f875010f891efd1c872e4624a67217f3f77ae3509bec504291ca7956f0d3eb8`)
- Proof Markdown: `docs/release-evidence/thin-module-fixture-proof-v1.md` (`e3d0c22ec0d887cef398f8faf2fad633128dbc83424cfbb49fb03a9060d87252`)
- Proof schema: `docs/release-evidence/thin-module-fixture-proof-v1.schema.json` (`6e944259e9ead83e1a70b62c6e0fc597702b3fcb32e4c2e160b616cd9b89efa7`)
- Proof signable payload: `5ef921ccc31496500ab84a0446693ea83c1438e9211996603b97cf5b193ae328`
- Resolved public APIs: `2fd59bcada80898b3a6aaa8c5feb31766c5356a90fff2093c734ad1f87d3d9e9`
- Measured test result: `09f4e57fdee7c3650a4c26dee24cd45cf839e4fdc2541c500692d852b7592707`

| Lock file | SHA-256 |
| --- | --- |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/packages.lock.json` | `52cbaccb8876b9df65fa7e691cf18b6755378e69aa50451ad5e4e211fcac0314` |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/packages.lock.json` | `d9f8362c9a38b3ad08481559e89dbebc92c474a06e1579923182f4419796295c` |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/packages.lock.json` | `78b06f0a95f4be921ace42500c2eba6e7578c0dccb162a2400d838c9b15a2423` |
| `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/packages.lock.json` | `7a55c490e0263300eb97079fffc684ca61b1f00acd057bcf213f7f743744d104` |

| Receipt | Path | SHA-256 |
| --- | --- | --- |
| `AC-11.2-01` | `artifacts/v9/11.2/AC-11.2-01.json` | `04460bce7bbeb2fc0026b4d7d929a265e3d43fdd0703a8877d756a6310ea75b8` |
| `AC-11.2-02` | `artifacts/v9/11.2/AC-11.2-02.log` | `7a99d7964c9f2f2600bbef90d2f4c9d5a22cdbb6f2aaffdb6935ae4b03ff5e82` |
| `AC-11.2-03` | `artifacts/v9/11.2/AC-11.2-03.log` | `760f7d83331e33863f8d2ee3fd9ae4d23f7371fa37b6971f16a1f8e630e23e64` |
| `AC-11.2-04` | `artifacts/v9/11.2/minimal-module.trx` | `09f4e57fdee7c3650a4c26dee24cd45cf839e4fdc2541c500692d852b7592707` |
| `AC-11.2-05` | `artifacts/v9/11.2/AC-11.2-05.trx` | `1efa85b591928692c310e9ff5c0bbde56b389736b15c8c2653162a314991ffd8` |
| `AC-11.2-07` | `artifacts/v9/11.2/AC-11.2-07.xml` | `7a45e671b44816849fd8ae5e15f72b63b96f4ef4224e71e4302a6b76bce3259f` |

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
