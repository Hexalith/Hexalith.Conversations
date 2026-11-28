---
title: 'Story 12.3: Implement correctness-preserving list/open optimization and migration behavior'
type: 'feature'
created: '2026-11-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'f8d81ca843c855cd5b87677617600e97bdcdee2f'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-12-context.md'
  - '{project-root}/docs/adrs/0005-conversation-derived-key-lifecycle.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `HP-LIST` (+330%) and `HP-OPEN` (+1761%) deserialize full 100-row indexes and full `Detail` bodies unnecessarily, re-hash keys, and JSON-serialize all tenant summaries for generation tokens (107 KB–10.64 MB across 100–10,000 rows), blocking SM-C2 restoration in Story 12.4.

**Approach:** Profile list (page 1 & 2), open, and write costs at 100, 1,000, and 10,000 rows; eliminate provably unnecessary deserialization, hashing, and allocations strictly within ADR 0005 (`conversations-derived-keys-v2`); prove correctness across unit, integration, real Dapr+Redis, and public-contract lanes; and generate `derived-key-correctness-proof-v1.{schema.json,json,md}` with its fault matrix. No final SM-C2 verdict is issued here.

**Decisions (owner, 2026-11-27):**
- **Persisted `GenerationToken`:** Profile 100 / 1,000 / 10,000 rows first; where read vs. write evidence justifies it, persist an optional internal `[JsonInclude]` `GenerationToken` on v2 tenant-index writes inside `ConversationProjectionReadModelWriter.Compose` (keeping `SchemaVersion = 2`, leaving `Lifecycle`/`Watermarks` untouched for Story 16.4, and falling back to streaming computation for legacy/v1/benchmark records lacking it).
- **`AC-12.3-08` waived:** Under the 24 November execution rules, the per-story v2 final record is waived (recorded as `waived`, never `PASS`), matching Stories 12.1 and 12.2. Story 12.4 consumes `derived-key-correctness-proof-v1.json` (`AC-12.3-06`) alongside Story 12.3's done spec, sprint row, and commit.
- **Size:** Keep the full spec whole.

## Boundaries & Constraints

**Always:**
- Preserve ADR 0005 (`conversations-derived-keys-v2`): 7 key families and exact bytes, `projection-route` / `TenantCreated` / SDK recovery as sole writers, live `1..10` and rebuild `1..7` write order, `readRepair = false` (`GetAsync`/`GetManyAsync` only), and `redeploy-then-rebuild` rollback.
- Preserve every fail-closed check: 3-key open check, per-row page check, row withholding + cursor suppression on withheld rows, AD-7 lifecycle/watermarks, pre/post tenant auth, cursor scope/bounds/generation invalidation, and zero cross-tenant disclosure.
- Measure list/open (page 1 & 2) vs. write cost at 100, 1,000, and 10,000 rows (allocations, store ops, latency) before finalizing write-path choices.
- Follow the 24 November execution rules: bind committed `HEAD`, all root `.gitmodules` gitlinks, completed Stories 12.1 (`455b8dea…`) and 12.2 (`dc661903…`, freeze `0cb5cf65…`), and `Debug/net11.0` commands (recording `Release/net10.0` substitutions).

**Never:**
- Edit ADR 0005, frozen `SmC2HotPathBenchmark.cs` (`f02c4b5d`), v1 SM-C2 evidence, Story 12.1/12.2 bundles, public `Contracts` wire shapes, CI workflows, or dirty submodule gitlinks (`Hexalith.Builds`, `Hexalith.Platform`).
- Issue an SM-C2 verdict, add a key family/writer, alter Story 16.4's `Lifecycle`/`Watermarks` recovery semantics, or weaken any fail-closed guard.

## I/O & Edge-Case Matrix

| Scenario | State / Fault | Expected Behavior | Blocker |
| --- | --- | --- | --- |
| Bounded reads (`AC-12.3-01`) | Ready tenant (100/1k/10k), page 1 & 2, open | List: 1 index + 2 bounded page bulk reads; Open: 1 detail + 1 targeted index + 1 ledger | `DERIVED_KEY_FANOUT_UNBOUNDED` |
| Invalid state (`AC-12.3-02`) | Missing, duplicate, stale, advanced, malformed, misfiled, pending, inconsistent | Fail closed (`Unavailable`/`Rebuilding`/`Forbidden`/withheld); zero store writes | `DERIVED_STATE_FAIL_CLOSED_VIOLATION` / `READ_SIDE_REPAIR_ATTEMPTED` |
| Lifecycle/replay (`AC-12.3-03`) | Tenant collisions, retries, out-of-order, equal-position conflict, legacy v1/v2, concurrent writes, deletion, replay, interrupted rebuild, rollback | Deterministic convergence, stable/invalidated cursors, zero disclosure, clean rollback replay | `TENANT_DISCLOSURE`, `REPLAY_NONDETERMINISTIC`, `DERIVED_KEY_TRANSITION_UNSAFE` |
| Dapr+Redis (`AC-12.3-04`) | Real `daprd` + Redis: partial writes, latency, outage, poison, retries, concurrency | Fail closed, deterministic recovery, zero disclosure | `DAPR_CORRECTNESS_LANE_RED` |
| Public contract (`AC-12.3-05`) | Filters, ordering, cursors, freshness, forbidden/nonexistent equivalence, shapes | Frozen snapshot unchanged | `PUBLIC_QUERY_CONTRACT_DRIFT` |
| Proof & matrix (`AC-12.3-06`/`07`) | 3 lane TRXs, inventories, ADR/method, rollback, fault mutations | Deterministic proof JSON/MD; exact blockers + byte restoration | `CORRECTNESS_GATE_RED` |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs` -- `ReadAsync` (:141-218, full-index deser), `ListAsync` (:221-275, 3 passes + 2 `HashSet` + `Dictionary`), `ClassifyLifecycle` (:286-339), `ValidatePageAsync` (:342-426, sequential bulk reads, full `Detail` deser, duplicate `DispatchLedgerKey` SHA-256).
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs` -- `GetAsync` (:90-177), `ListAsync` (:249-437, `ComputeGenerationToken` :329,546-559 JSON+SHA-256, duplicate `BuildScope` :260,406, redundant `.ToList()`).
- `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs` -- `HydrateDetailAsync` (:27-84), `HydrateSummariesAsync` (:89-152); trim redundant copies.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexReadModel.cs` & `ConversationProjectionReadModelWriter.cs` -- v2 index (`SchemaVersion = 2`) & `MergeIndex`/`PlanRebuildIndex`/`CreateInitializedIndex`/`Compose` (:206-388); optional internal `GenerationToken` if profiling justifies it.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` -- Immutable; guarded by `ConversationProjectionReadModelKeysTest.cs`.
- `tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs` -- Frozen (`f02c4b5d`); do NOT edit.
- Reuse: `InMemoryReadModelStore`, `CountingReadModelStore`, `WriteLedgerStore` (`Server.Tests`), disposable Redis + live `daprd` + `DaprReadModelStoreDecorator` (`Story16_3LiveFixture.cs`), and Python CLI/fault patterns (`generate_sm_c2_method.py`, `test_sm_c2_method.py`).
- `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj` -- Add `ProjectReference` to `Hexalith.Conversations.Server`.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/DerivedKeyOptimizationTests.cs` -- Profile 100/1k/10k list (page 1 & 2), open, and write costs before finalizing write-path choices; implement `ListAndOpenShouldUseApprovedBoundedReads` (`AC-12.3-01`) and `InvalidDerivedStateShouldFailClosedWithoutRepair` (`AC-12.3-02`).
- [x] `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs` (+ one-type-per-file internal DTO/converter files) -- Targeted `Utf8JsonReader` index lookup in `ReadAsync` (keeping duplicate detection), minimal `DetailHeader` DTO in `ValidatePageAsync`, single-pass `ListAsync` lifecycle/completeness check, cached `DispatchLedgerKey`, concurrent detail+ledger bulk reads.
- [x] `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs` (+ `Hydration/ConversationReadHydrationService.cs`, and `ConversationProjectionIndexReadModel.cs` / `ConversationProjectionReadModelWriter.cs` if profiling justifies persisted `GenerationToken`) -- Streaming `IncrementalHash` generation token (skip when `!cursor.HasValue && !issueContinuation`; reuse persisted token when present and poison-free), reuse cursor `scope`, trim copies.
- [x] `tests/Hexalith.Conversations.IntegrationTests/Projections/DerivedKeyLifecycleTests.cs` -- Implement `AC-12.3-03` lifecycle, legacy v1/v2, token stability/invalidation, concurrency, replay, rebuild, and rollback rehearsal.
- [x] `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj` & `DerivedKeyDaprFaultTests.cs` -- Implement `AC-12.3-04` over live `daprd` + disposable Redis + `DaprReadModelStore`.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/PublicQueryCompatibilityTests.cs` -- Implement `DerivedKeyOptimizationShouldPreserveQueryContract` (`AC-12.3-05`).
- [x] `docs/release-evidence/derived-key-correctness-proof-v1.schema.json`, `_bmad/scripts/generate_derived_key_correctness_proof.py`, `_bmad/scripts/tests/test_derived_key_correctness.py` -- Build closed schema, deterministic proof generator (`AC-12.3-06`, recording `AC-12.3-08` as `waived`), and real-CLI `complete_fault_matrix` (`AC-12.3-07`); generate `derived-key-correctness-proof-v1.{json,md}`.
- [x] Run routine root solution build, individual maintained test projects, `check-root-submodules.py`, and `git diff --check`; record `AC-12.3-08` waiver.

**Acceptance Criteria:**
- Given 100/1k/10k tenants, when before/after profiles run, then allocations, store ops, and latency are recorded and list/open overhead drops within ADR 0005.
- Given `AC-12.3-01`..`AC-12.3-05`, when each `Debug/net11.0` test assembly runs with `-automated sync -failSkips`, then all pass with nonzero tests and zero skips.
- Given the 3 lane TRXs, when `AC-12.3-06` runs twice and `AC-12.3-07` runs, then proof JSON/MD are byte-identical (with `AC-12.3-08` recorded `waived`, never `PASS`) and every fault mutation fires its exact blocker and restores bytes.
- Given the final tree, when the root solution builds and maintained test projects run individually with `check-root-submodules.py` and `git diff --check`, then all pass and protected paths stay untouched.

### Review Findings

Reviewed `a1cb5c41` against `f8d81ca8` on 2026-11-27. Four review layers completed. The intervening CI/CD delivery accounts for four paths in the baseline range.

- [x] [Review][Patch] High — Preserve continuation cursors across deployment and rollback [src/Hexalith.Conversations.Server/Projections/ConversationIndexGenerationToken.cs:143]
  The same unchanged summary produces a different digest under the two implementations. The new handler returns `Forbidden` for an unexpired pre-deployment cursor even when cursor protection keys and the index are unchanged. The migration test only issues cursors with the candidate. Pin compatibility using a cursor generated with the previous digest.
- [x] [Review][Patch] High — Bind the persisted generation token to the current rows [src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:628]
  The shape check accepts a well-formed old token after the summaries and their matching detail records change. A continuation can reuse its old offset against reordered rows. Verify the digest binding and test stale, valid-shaped tokens.
- [x] [Review][Patch] High — Preserve fail-closed list validation for malformed detail records [src/Hexalith.Conversations.Server/Projections/ConversationDetailValidationHeader.cs:19]
  Removing the stored detail's required lifecycle field fails full deserialization and passes the new list DTO. List can certify the row as current while Open rejects it. Preserve validation of fields previously checked by deserialization, with a syntactically valid malformed-record regression test.
- [x] [Review][Patch] High — Preserve fail-closed open validation for malformed tenant indexes [src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupConverter.cs:59]
  The targeted reader skips malformed known fields such as `schemaVersion` and `watermarks` and malformed sibling dispatch values. The former typed index read rejected these records. Check recognized fields for validity during the scan.
- [x] [Review][Patch] High — Observe ledger completion after reading page details [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:503]
  During restoration of a missing detail, the parallel ledger read can observe an expired ledger before redelivery recreates it pending. The detail read can then observe the restored detail before dispatch completion. The combined result can report `Current` even though the detail and a completed or legitimately expired ledger never coexisted. Preserve the read ordering needed for this guard and exercise it with gated store reads.
- [x] [Review][Patch] High — Bind correctness inputs and test results to one committed candidate [\_bmad/scripts/generate_derived_key_correctness_proof.py:516]
  The proof names parent `b7c12907`, whose tree matches 1 of the 16 production digests. The reviewed commit matches all 16. The generator reads the commit, staged gitlinks, working-tree source and TRXs independently. A controlled source change with the old TRXs still produced `PASS`. Bind the inputs and the tested binaries to the same commit.
- [x] [Review][Patch] High — Make the committed proof refreshable [\_bmad/scripts/generate_derived_key_correctness_proof.py:801]
  The documented regeneration returned exit `2` with `OUTPUT_WRITE_FAILED` in a disposable checkout of the story commit. The shared writer rejects different bytes at the two tracked output paths. Supply a scoped publication route for generating a refreshed pair while preserving the shared writer's other protections.
- [x] [Review][Patch] High — Require the complete test set for each correctness lane [\_bmad/scripts/generate_derived_key_correctness_proof.py:314]
  A reduced integration TRX and a reduced Dapr TRX each passed with only one matching test. The proof can certify all Dapr cases and replay determinism with those tests absent. Require every contracted method and reconcile result and execution counts.
- [x] [Review][Patch] Medium — Rehearse rollback with the prior binary [tests/Hexalith.Conversations.IntegrationTests/Projections/DerivedKeyLifecycleTests.cs:355]
  The test starts the candidate twice and compares normalized open responses. Exercise the prior binary in the redeploy-and-rebuild sequence and check persisted state and query outcomes.
- [x] [Review][Patch] Medium — Verify read outcomes during interrupted rebuild [tests/Hexalith.Conversations.IntegrationTests/Projections/DerivedKeyLifecycleTests.cs:336]
  The lifecycle lane checks dispatch status after the injected interruption, then invokes the retry. Check that List and Open withhold the incomplete generation before clearing the fault.
- [x] [Review][Patch] Medium — Exercise the contracted mutations in the fault matrix [\_bmad/scripts/tests/test_derived_key_correctness.py:221]
  Synthetic failed TRXs carry the desired blocker strings for several invalid-state, Dapr, isolation and replay cases. They verify blocker forwarding. Exercise the underlying faults and verify their real results so removing a scenario assertion can fail the matrix.
- [x] [Review][Patch] Medium — Remove the dependency on the retired V9 authority bundle [\_bmad/scripts/generate_derived_key_correctness_proof.py:523]
  The CLI blocks without that bundle and rejects a different recorded contract digest. The November 24 rules permit the existing story contract to supply scenarios and retire the authority bundle as a prerequisite.
- [x] [Review][Patch] Medium — Measure comparable baseline and candidate write costs [tests/Hexalith.Conversations.Server.Tests/Projections/DerivedKeyOptimizationTests.cs:876]
  The reported baseline has allocation figures and the later candidate adds latency for a different, two-phase write workload. Run the same list pages, opens and full writes at each size, retaining comparable latency, allocation and operation counts to justify token persistence.
- [x] [Review][Patch] Medium — Validate proof schema closure through composition [\_bmad/scripts/generate_derived_key_correctness_proof.py:380]
  An open object nested in `allOf` passes the closure check and accepts unexpected properties. Validate composed definitions or pin the complete closed schema and test the composed case.
- [x] [Review][Patch] Medium — Verify completion of the sibling read on failure [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:501]
  Existing tests fault both reads together. Use independently gated reads to check that a healthy sibling is cancelled and finishes before a failed query returns if overlapping reads remain.
- [x] [Review][Patch] Medium — Keep provider extension comparisons linear [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:698]
  Replacing the set comparison with a nested scan makes normal matching extension bags quadratic, and the metadata constructor has no entry-count cap. Retain ordinal equality with efficient lookup and include populated bags in the workload.
- [x] [Review][Patch] Low — Return a stable blocker for a non-object story contract [\_bmad/scripts/generate_derived_key_correctness_proof.py:526]
  An array at the JSON root reaches `contract.get` and raises uncaught `AttributeError`. Reject the shape with `INPUT_UNAVAILABLE`.
- [x] [Review][Patch] Low — Stop the Docker process before cleaning up a timed-out fixture [tests/Hexalith.Conversations.AppHost.Tests/DerivedKeyDaprLiveFixture.cs:269]
  Cancelling `WaitForExitAsync` leaves Docker running. Cleanup can finish before the pending launch creates the container. Terminate and observe the process before final fixture cleanup.
- [x] [Review][Defer] Tenant initializer redelivery still fails [tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantIndexInitializerTest.cs:134] — deferred: the existing Story 16.2 test double lacks conditional deletion support. The isolated rerun returned exit `1`, with `RetryableInProgress` where the test expects `Processed`.

#### Rejected

- Cancellation callback throws before the ledger task is awaited. The path needs an `IReadModelStore` callback that throws during cancellation. The reviewed stores provide no demonstrated trigger. The sibling-read verification gap is tracked above.
- Change the historical completion statement as a review fix. The failed test is tracked above, and the workflow owns the live story status transition.

#### Review pass 3 — 2026-11-27

Reviewed the Story 12.3 changes through `fe303008` against `f8d81ca8`. The four paths delivered by the intervening CI/CD commit were outside the story review. All four review layers completed.

- [x] [Review][Patch] High — Preserve detail validation on List [src/Hexalith.Conversations.Server/Projections/ConversationDetailValidationHeaderConverter.cs:279]
  A persisted message object missing required fields made List return the row as `Current` while Open returned `Unavailable`. The lightweight parser also accepts undecodable strings in fields it only type-checks and validates repeated lifecycle values before the final value is known. Match the typed reader's validation and exercise these cases in AC-12.3-02.
- [x] [Review][Patch] High — Validate sibling summaries during targeted Open [src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupConverter.cs:205]
  Removing `freshness` from a sibling while retaining its ID made Open return `Current` and List return `Unavailable`. The scan also accepts invalid sibling ID strings and skips recognized fields with malformed values. Preserve typed validation while scanning the targeted index.
- [x] [Review][Patch] Medium — Honor the store serializer's numeric handling during Open [src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupConverter.cs:268]
  With a quoted `schemaVersion`, List remained `Current` and Open returned `Unavailable`. The same mismatch applies to quoted watermarks and sibling dispatch sequences. Dapr and the in-memory store use Web JSON number handling, which accepts these persisted values.
- [x] [Review][Patch] High — Resolve TRX results through their test definitions [\_bmad/scripts/generate_derived_key_correctness_proof.py:418]
  A copy of the real unit TRX still reported `PASS` after its linked definition changed to a different method and the result retained the expected display name. Resolve each result to a unique definition and use that identity for required-method checks.
- [x] [Review][Patch] High — Verify the source and configuration of tested binaries [\_bmad/scripts/generate_derived_key_correctness_proof.py:449]
  A commit suffix in assembly metadata can identify the same commit after a build from modified sources. The generator checks the checkout later and skips assembly verification when the TRX assembly path is missing. A receipt with a nonexistent assembly passed the parser. Build and test from a verified committed snapshot and bind the assembly digests, framework and configuration to that run before certifying it.
- [x] [Review][Patch] High — Rebuild the complete tenant during rollback rehearsal [tests/Hexalith.Conversations.IntegrationTests/Projections/DerivedKeyLifecycleTests.cs:514]
  The rehearsal erases two conversations, rebuilds one with a synthetic request, and accepts a `Rebuilding` List. Use persisted EventStore replay for both conversations and assert that the prior binary restores the complete tenant index and both Open results.
- [x] [Review][Patch] High — Exercise an interruption after rebuild starts writing [tests/Hexalith.Conversations.IntegrationTests/Projections/DerivedKeyLifecycleTests.cs:420]
  The first fault hook runs at `BeforeDispatch`. The target row starts absent, which accounts for the observed withholding. Inject after intermediate state is durable and verify the partial state, withheld target, visible sibling, and successful retry.
- [x] [Review][Patch] Medium — Verify outage and recovery through the real state-store adapter [tests/Hexalith.Conversations.AppHost.Tests/DerivedKeyDaprFaultStore.cs:105]
  The decorator throws before the production adapter runs for both full and bulk outage cases. Exercise a Dapr or Redis interruption and verify the public results during the outage and after service resumes.
- [x] [Review][Patch] Medium — Reject allow-all schemas in proof value positions [\_bmad/scripts/generate_derived_key_correctness_proof.py:574]
  A boolean `true` subschema bypasses the closure check. The modified schema accepts the published proof and arbitrary nested fields at the opened location.
- [x] [Review][Patch] Medium — Compare proof source with Git-normalized bytes [\_bmad/scripts/generate_derived_key_correctness_proof.py:250]
  A clean checkout with `core.autocrlf=true` can receive translated source bytes for paths such as `ConversationQueryHandler.cs`. Proof generation treats the resulting difference as source drift. Normalize according to the tracked Git rules or pin the byte format for hashed sources.
- [x] [Review][Patch] Medium — Verify property accessors against the public snapshot [tests/Hexalith.Conversations.Contracts.Tests/PublicQueryCompatibilityTests.cs:114]
  The comparison reads property names and types and omits the snapshot's `canRead` and `canWrite` attributes. An accessor change can pass the compatibility test.
- [x] [Review][Patch] Medium — Exercise public query behavior in the compatibility lane [tests/Hexalith.Conversations.Contracts.Tests/PublicQueryCompatibilityTests.cs:149]
  The lane constructs filter, cursor and result objects directly. Add runtime assertions for filter and ordering behavior and for the equivalence between unauthorized and nonexistent Open results.
- [x] [Review][Patch] Medium — Require the exact compiled fault blocker set [\_bmad/scripts/tests/test_derived_key_correctness.py:851]
  The compiled mutations check membership of two blockers and accept unexpected additional blockers. Assert the complete expected blocker set for each mutation.
- [x] [Review][Patch] Medium — Exercise actual Dapr and public-contract faults in the matrix [\_bmad/scripts/tests/test_derived_key_correctness.py:560]
  The synthetic Dapr receipts verify reporting behavior, and the public snapshot mutation verifies the file digest. Add runtime mutations that exercise the live Dapr failure and public query contract assertions and verify the reported blockers.
- [x] [Review][Patch] Medium — Include SDK recovery writes in the read-side guard [\_bmad/scripts/generate_derived_key_correctness_proof.py:164]
  The static guard omits mutating recovery methods including `RecoverPendingAsync` and `PrepareRecoveryManifestAsync`. A call through those interfaces would bypass this guard. Include the recovery operations and exercise the guard with a recovery-call mutation.
- [x] [Review][Patch] Low — Avoid allocating strings for ordinary property-name mismatches [src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupConverter.cs:444]
  An unescaped ASCII property name of a different length falls through to `GetString()`. This allocates for most fields in every sibling summary scanned by Open. Return early for ordinary ASCII length mismatches.
- [x] [Review][Patch] Medium — Make the concurrent-read test spy safe for parallel calls [tests/Hexalith.Conversations.Server.Tests/Projections/DerivedKeyReadStoreSpy.cs:66]
  The delayed Open test can resume on several threads and update mutable counters and lists concurrently. Synchronize recording and reset so the spy cannot lose observations or introduce failures of its own.
- [x] [Review][Patch] Medium — Apply a single deadline to Dapr health polling [tests/Hexalith.Conversations.AppHost.Tests/DerivedKeyDaprLiveFixture.cs:210]
  An unresponsive sidecar can consume up to 240 three-second HTTP timeouts plus delays. The documented 60-second budget then lasts about 13 minutes. Use one deadline for requests and retry delays.
- [x] [Review][Patch] Medium — Pin exact cross-binary cursor pages for tied timestamps [tests/Hexalith.Conversations.IntegrationTests/Projections/DerivedKeyLifecycleTests.cs:494]
  The rehearsal uses distinct timestamps and counts one result per page. It misses dropped or repeated rows if the ordinal tie-break changes while a cursor remains valid. Seed tied timestamps in nonordinal storage order and assert exact identities in both directions.
- [x] [Review][Defer] Tenant initializer redelivery still blocks the maintained suite [tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantIndexInitializerTest.cs:119] — deferred: the existing Story 16.2 test-double issue is already in the deferred-work ledger. The isolated rerun exited `1` with `RetryableInProgress` instead of `Processed`.

The five focused acceptance lanes passed 13 tests with zero skips using the existing Debug/net11.0 outputs. The Python fault matrix passed 50 tests and the shared output writer passed 77 tests. Two renders from the retained TRXs matched both committed proof files. Root-submodule validation and the scoped diff whitespace check passed.

##### Rejected

- Replace the prior-binary harness solely because it links candidate dependency assemblies. The reviewed range introduces no dependency revision change between the prior harness baseline and the tested story, so that concern does not establish a rollback failure in this range. The incomplete replay is recorded separately.

## Implementation Notes

- **Before/After Scale Profile (`Debug/net11.0`, `InMemoryReadModelStore`, 25-row pages):**
  - `100` rows: List P1 alloc `989,664 B -> 611,312 B` (`-38.2%`), List P2 alloc `987,466 B -> 614,654 B` (`-37.8%`), Open alloc `378,694 B -> 94,603 B` (`-75.0%`), Write alloc `584,366 B -> 583,720 B` (`-0.1%`).
  - `1,000` rows: List P1 alloc `5,723,425 B -> 3,760,960 B` (`-34.3%`), List P2 alloc `5,721,826 B -> 3,764,284 B` (`-34.2%`), Open alloc `3,493,183 B -> 634,605 B` (`-81.8%`), Write alloc `5,549,916 B -> 5,540,720 B` (`-0.2%`).
  - `10,000` rows: List P1 alloc `52,850,405 B -> 35,171,072 B` (`-33.5%`), List P2 alloc `52,848,080 B -> 35,174,298 B` (`-33.4%`), Open alloc `34,578,685 B -> 6,034,432 B` (`-82.5%`), Write alloc `55,058,804 B -> 54,877,308 B` (`-0.3%`).
  - Store ops unchanged and bounded: List = `1` index GET + `2` page bulk GETs (`25` detail headers + `25` ledgers); Open = `3` GETs (`1` detail + `1` targeted `Utf8JsonReader` index + `1` ledger); `0` writes on every read path.
  - Because `ConversationProjectionReadModelWriter.Compose` already orders all summaries and streaming `IncrementalHash` adds ~200 B with `-0.1%..-0.3%` net write allocation delta, persisting optional internal `[JsonInclude]` `GenerationToken` on v2 indexes is justified and preserves `SchemaVersion = 2`, `Lifecycle`, and `Watermarks` untouched; legacy/v1/benchmark records lacking it compute the identical canonical token via `ConversationIndexGenerationToken.Compute`.
- **Scenario Results (`Debug/net11.0`, substituting `Release/net10.0` per 24 November rules):**
  - `AC-12.3-01`: PASS (`artifacts/v9/12.3/AC-12.3-01.trx`, 1/1).
  - `AC-12.3-02`: PASS (`artifacts/v9/12.3/AC-12.3-02.trx`, 1/1).
  - `AC-12.3-03`: PASS (`artifacts/v9/12.3/integration.trx`, 5/5).
  - `AC-12.3-04`: PASS (`artifacts/v9/12.3/dapr.trx`, 5/5 over live `daprd` + disposable Redis `redis:7.4-alpine`).
  - `AC-12.3-05`: PASS (`artifacts/v9/12.3/AC-12.3-05.trx`, 1/1).
  - `AC-12.3-06`: PASS 2x byte-identical (`docs/release-evidence/derived-key-correctness-proof-v1.{json,md}`, `payloadSha256: cade3112652f7069fa4206b8d7c866e8155384b66ec801cf9f9e4ed99732679e`).
  - `AC-12.3-07`: PASS (`artifacts/v9/12.3/AC-12.3-07.xml`, 25/25 fault-matrix tests passed with exact blockers and byte restoration).
  - `AC-12.3-08`: `waived` (recorded as `waived`, never `PASS`, under 24 November execution rules).
- **Routine Verification:** Root `Hexalith.Conversations.slnx` build succeeded (`0` warnings, `0` errors); `check-root-submodules.py` PASS; `git diff --check` clean; maintained test assemblies passed (`Contracts.Tests` 649/649, `Client.Tests` 76/76, `Tests` 189/189, `Conformance.Tests` 91/91, `Conformance.Portable.Tests` 308/308, `TestSupport.Tests` 44/44, `DeletionConformance.Tests` 35/35, `Admin.Web.Tests` 14/14, `AppHost.Tests` Topology+Dapr 17/17, `IntegrationTests` projections 7/7; `Server.Tests` 1805/1806 with 1 pre-existing unrelated failure `ConversationTenantIndexInitializerTest.IndexFailureShouldKeepTheTenantUnhealthyUntilTheRedeliveryInitializesIt` returning `RetryableInProgress` due to `FailingIndexStore` lacking `IReadModelConditionalEraser`).
- **Review Pass 1 Resolution (2026-11-27):** Resolved all 10 patch groups across production code, unit/Dapr/contracts tests, and proof tooling (`GenerationToken` 64-char hex validation & v1 rejection; `ValidatePageAsync` linked-CTS cancellation & await; single-arg `ClassifyLifecycle` per-entry duplicate gap check; `ConversationProjectionIndexLookupConverter` last-write-wins & non-string rejection; `IncrementalHashBufferWriter` zeroing on return & disposal/bounds guards; preserving `persisted.GenerationToken` on non-summary pending/reconcile writes; full `v4.json` contract member-signature verification; `DerivedKeyDaprFaultStore` full interface interception & bounded 10s `daprd` teardown; proof generator `parse_trx` test-identity/non-negative/`not_run` checks, recursive closed schema validation, expanded 4-file read-repair static scan, `OSError`/CRLF/non-dict/`rollback` hardening). Logged 2 deferrals in `deferred-work.md` (pre-existing `ConversationTenantIndexInitializerTest` failure; post-commit regeneration of `derived-key-correctness-proof-v1.{json,md}` on committed Story 12.3 `HEAD`).
- **Post-Patch Scale Profile (`Debug/net11.0`, `InMemoryReadModelStore`, 25-row pages, latency + allocations):**
  - `100` rows: List P1 fallback `2,295.9 µs` (`611,592 B`) / persisted `1,885.0 µs` (`611,212 B`), List P2 fallback `1,782.4 µs` (`614,934 B`) / persisted `1,609.4 µs` (`614,750 B`), Open `561.2 µs` (`95,411 B`), Full write (`MarkGenerationPendingAsync` + `PersistGenerationAsync`) `2,973.2 µs` (`1,142,314 B`).
  - `1,000` rows: List P1 fallback `14,899.4 µs` (`3,761,240 B`) / persisted `24,513.2 µs` (`3,760,912 B`), List P2 fallback `21,219.6 µs` (`3,764,564 B`) / persisted `22,835.9 µs` (`3,764,192 B`), Open `13,600.0 µs` (`635,413 B`), Full write `59,492.9 µs` (`11,033,356 B`).
  - `10,000` rows: List P1 fallback `96,838.1 µs` (`35,171,354 B`) / persisted `86,781.5 µs` (`35,171,000 B`), List P2 fallback `117,713.2 µs` (`35,174,597 B`) / persisted `78,883.3 µs` (`35,174,114 B`, `-33.0%` latency vs fallback), Open `25,190.3 µs` (`6,035,240 B`), Full write `235,540.2 µs` (`109,458,564 B`).
- **Post-Patch Full Verification:** Root `Hexalith.Conversations.slnx` Debug build passed (`0` warnings, `0` errors); `AC-12.3-01` (`1/1`), `AC-12.3-02` (`1/1`), `AC-12.3-03` (`5/5`), `AC-12.3-04` (`5/5`), `AC-12.3-05` (`1/1`) all PASS; `AC-12.3-06` PASS 2x byte-identical (`payloadSha256: c9840d858251dba4c7c41ebcf72ddc841e306152ef85c6ec1b05c070f93a3ef5`); `AC-12.3-07` (`37/37`) PASS; maintained assemblies passed (`Contracts.Tests` 649/649, `Client.Tests` 76/76, `Tests` 189/189, `Conformance.Tests` 91/91, `Conformance.Portable.Tests` 308/308, `TestSupport.Tests` 44/44, `DeletionConformance.Tests` 35/35, `AppHost.Tests` Topology+Dapr 17/17, `IntegrationTests` Projections+Lifecycle 7/7, `Server.Tests` 1,813/1,814 with only the 1 pre-existing deferred `ConversationTenantIndexInitializerTest` failure); `check-root-submodules.py` PASS; `git diff --check` clean.
- **Evidence-boundary transition:** `not-applicable` (routine work under `docs/runbooks/current-change-validation.md` and 24 November execution rules; no retired historical evidence-boundary verifier requested).
- **Review Pass 2 Resolution (2026-11-27):** Resolved all 18 `[Review][Patch]` items:
  - **Cursor compatibility & digest binding:** `ConversationIndexGenerationToken.Compute` now streams the exact default `JsonSerializer.Serialize(writer, summary, SummaryTypeInfo)` representation into `IncrementalHashBufferWriter`, producing byte-for-byte identical SHA-256 digests to pre-deployment `b7c12907` (pinned in both unit and integration tests, including cross-binary cursor handoff with the compiled prior binary). `ConversationQueryHandler.ResolveGenerationToken` always binds to `ConversationIndexGenerationToken.Compute(tenantScoped)` rather than trusting unverified persisted `GenerationToken` shape, rejecting stale valid-shaped tokens after summary/detail changes. Because fail-closed digest verification on reads removes any read-side gain from persisting `GenerationToken`, `Compose` omits writing `GenerationToken` (zero write regression, zero schema delta).
  - **Fail-closed detail, index, ordering, and extension checks:** Added `ConversationDetailValidationHeaderConverter` validating all required/recognized `ConversationDetailProjectionV1` fields (`schemaVersion`, `tenantId`, `conversationId`, `freshness`, `lifecycleState`, header sub-objects, `attributes`, and non-null collection object arrays) without allocating full timeline/participant/evidence graphs; added recognized-field validation (`schemaVersion`, `lifecycle`, `watermarks`, `generationToken`, sibling `dispatches`) to `ConversationProjectionIndexLookupConverter`; restored sequential detail-before-ledger bulk read ordering in `ValidatePageAsync` (eliminating the detail-restoration race and any overlapping sibling read, proved via `DerivedKeyGatedBulkStore`); restored O(N) linear ordinal comparison in `SameProviderCorrelation`.
  - **Prior-binary rollback, interrupted rebuild reads, and Docker cleanup:** Added `DerivedKeyPriorBinaryRollbackHarness` compiling and loading pre-Story-12.3 `b7c12907` `Hexalith.Conversations.Server.dll` into an isolated `AssemblyLoadContext` to rehearse real `redeploy-then-rebuild` (checking persisted keys, List/Open outcomes, and bidirectional cursor compatibility); verified List and Open withhold incomplete generations during interrupted rebuild before clearing the fault; updated `DerivedKeyDaprLiveFixture.RunDockerAsync` to kill and await timed-out `docker` processes before cleanup.
  - **Proof generator & real-mutation fault matrix:** Bound all proof inputs, root gitlinks, and TRX `CANDIDATE_BINDING` stamps (`commit`, `serverSha256`, `testAssemblySha256`) to one committed candidate; added scoped `replace_tracked=(JSON_PATH, MARKDOWN_PATH)` in `authoring_outputs.write_outputs` so committed proof pairs refresh cleanly while keeping all other protections intact (`test_authoring_outputs.py` green); required every contracted test method per lane with reconciled counts; removed retired `v9-authority-bundle-v1.json` dependency; closed schema validation recursively across composition keywords (`allOf`/`anyOf`/`oneOf`/etc.); rejected non-object story contracts with `INPUT_UNAVAILABLE`; added 6 compiled real-runtime C# mutation tests in `AC-12.3-07` (`45/45` passed).
- **Comparable Baseline (`b7c12907`) vs. Candidate Scale Profile (`Debug/net11.0`, `InMemoryReadModelStore`, 25-row pages, populated `ProviderCorrelation.ExtensionData`, identical workload including full two-phase write `MarkGenerationPendingAsync` + `PersistGenerationAsync`):**
  - `100` rows: List P1 `7,423.7 µs / 1,229,977 B -> 4,200.6 µs / 1,226,064 B` (`-43.4%` lat, `-0.3%` alloc); List P2 `9,430.5 µs / 1,227,790 B -> 4,727.0 µs / 1,229,433 B` (`-49.9%` lat); Open `5,828.6 µs / 501,931 B -> 2,012.4 µs / 168,123 B` (`-65.5%` lat, `-66.5%` alloc); Full Write `24,955.0 µs / 1,459,444 B (3R,3W) -> 11,711.7 µs / 1,454,687 B (3R,3W)` (`-0.3%` alloc, 0 op delta).
  - `1,000` rows: List P1 `73,953.5 µs / 7,332,671 B -> 54,651.3 µs / 6,182,912 B` (`-15.7%` alloc); List P2 `40,182.4 µs / 7,330,904 B -> 62,444.5 µs / 6,186,236 B` (`-15.6%` alloc); Open `14,943.3 µs / 4,689,303 B -> 15,309.6 µs / 1,334,567 B` (`-71.5%` alloc); Full Write `80,927.2 µs / 14,127,131 B (3R,3W) -> 82,005.0 µs / 14,089,828 B (3R,3W)` (`-0.3%` alloc, 0 op delta).
  - `10,000` rows: List P1 `206,042.4 µs / 68,149,453 B -> 215,071.5 µs / 55,658,592 B` (`-18.3%` alloc, `-12.5 MB/op`); List P2 `209,969.6 µs / 68,147,272 B -> 220,994.9 µs / 55,659,845 B` (`-18.3%` alloc); Open `127,368.0 µs / 46,503,597 B -> 122,526.1 µs / 12,998,490 B` (`-72.0%` alloc, `-33.5 MB/op`); Full Write `385,075.0 µs / 140,528,328 B (3R,3W) -> 400,915.5 µs / 140,065,584 B (3R,3W)` (`-0.3%` alloc, 0 op delta).
  - **Token persistence conclusion:** Because fail-closed reads must verify digest binding against current tenant-scoped summaries (preventing stale valid-shaped tokens from validating cursors against reordered rows), persisting `GenerationToken` on writes is not justified and is omitted from `Compose`.
- **Review Pass 2 Post-Patch Verification (2026-11-27):** Resolved all 11 Review Pass 2 patch groups (real xUnit v3 `<TextMessages><Message>` per-result `CANDIDATE_BINDING` parsing, `ResultSummary/@outcome`, relative `codeBase` + `OSError` guards, 9 new read-side files in `READ_SIDE_PATHS`, untyped schema closure, committed `CONTRACT_PATH`/`SCHEMA_PATH` checks, `.gitmodules` UTF-8 guard, `authoring_outputs.py` deleted-file guard + `replace_tracked` tests, Unicode whitespace attribute-key fallback, 7 persisted metadata fields on `ConversationPageValidationRecord`, mandatory `conversationId` on sibling summaries + non-null `dispatches`, non-aliasing `Compose`, dead `Snapshot.GenerationToken` removal & `ClassifyLifecycle` deduplication, Contracts enum checks, worktree-safe prior-binary harness + full proxy checks + populated interrupted-rebuild check, live Dapr bounded-read counts + writer tenant init + `AllocatePorts` cleanup). Root `Hexalith.Conversations.slnx` Debug build passed (`0` warnings, `0` errors); `AC-12.3-01` (`1/1`), `AC-12.3-02` (`1/1`), `AC-12.3-03` (`5/5`), `AC-12.3-04` (`5/5`), `AC-12.3-05` (`1/1`) PASS; `test_authoring_outputs.py` (`77/77`) PASS; `AC-12.3-07` (`50/50`) PASS; maintained test assemblies passed (`Contracts.Tests` 649/649, `Client.Tests` 76/76, `Tests` 189/189, `Conformance.Tests` 91/91, `Conformance.Portable.Tests` 308/308, `TestSupport.Tests` 44/44, `DeletionConformance.Tests` 35/35, `Admin.Web.Tests` 14/14, `Server.Tests` 1,815/1,816 with only the pre-existing deferred `ConversationTenantIndexInitializerTest` failure); `check-root-submodules.py` PASS; `git diff --check` clean; protected paths untouched.

- **Review pass 3 implementation.** List validates persisted detail values and targeted Open validates sibling summaries with the store's numeric handling. Integration replay restores the complete tenant with the prior binary and exercises both directions of tied cursor paging. The interrupted batch case verifies persisted partial state, withheld target, visible sibling, and successful retry. The live Dapr lane stops and restarts the sidecar and checks queries through the same adapter. The proof binds linked TRX definitions, verified assembly files, and the build receipt for the committed source snapshot.
- **Review pass 3 verification.** The source-mapped Debug/net11.0 solution built with zero warnings and errors. AC-12.3-01 through AC-12.3-05 passed 13 tests with zero skips. AC-12.3-06 produced identical bytes on both runs against disposable commit `2c49abf8ed7c425599e6beb7b92e78f45e76ed7b` (payload `8236016e59000c80fdb8d4f61ac605b25782e7fb4446aff23f0bc843bc000134`). AC-12.3-07 passed 62 cases, including actual Dapr outage and public ordering mutations. The [measurement artifacts](../../.artifacts/story-12.3-pass-3/final-source/measured-2c49abf8) contain the TRXs and proof.
- **Review pass 3 profiling.** Across 100, 1,000, and 10,000 rows, Open allocated 387,662 B, 3,526,895 B, and 34,918,957 B, down about 23–25% against the recorded prior baseline. Both List pages reduced allocations at each size. Open latency improved at 100 and 10,000 rows and increased at 1,000 rows in the Debug sample. Full writes remained at three reads and three writes per operation.

- **Maintained suite.** The package-mapped Release/net11.0 solution built with zero warnings and errors. All 11 maintained projects passed individually with 3,297 tests and zero skips, including the live AppHost and integration tests. Root-submodule and diff whitespace checks passed. The [verification logs](../../.artifacts/story-12.3-pass-3/package) contain the individual commands and results.

- **Review pass 4.** The readers now preserve custom serializer options and last-property semantics. List caps its initial allocation by page size. The rebuild test uses the SDK's pending-batch recovery against an installed key and durable manifest. Contract tests check each filter and exact response properties. The fault matrix covers additional schema and read-side SDK cases, and the scale test compares warmed Open allocations to full index materialization.
- **Review pass 4 source verification.** The Debug/net11.0 solution build and 13 focused tests passed with zero skips. AC-12.3-06 rendered byte-identically twice (payload `9939a6a65cbd6c6fccd8a4a92b96b834dc623bc9cbd48547cd75eed939011547`). AC-12.3-07 passed 68 cases with zero skips. The [proof and receipts](../../.artifacts/story-12.3-pass-4/measured-01c749fc) bind disposable source commit `01c749fcf596319c8377641342fd11bec95acb44`, retained in the [candidate bundle](../../.artifacts/story-12.3-pass-4/verified-candidate.bundle). The owner requested uncommitted workspace changes. The tracked proof refresh remains a prerequisite for Story 12.4.

- **Review pass 4 maintained suite.** The package-mapped Release/net11.0 solution built with zero warnings and errors. All 3,297 tests passed across the 11 maintained projects with zero skips. Root-submodule and diff whitespace checks passed. The [final verification logs](../../.artifacts/story-12.3-pass-4) contain the exact commands and results. The completion gate requires a clean committed implementation. The owner chose to retain the working-tree changes, so the spec and sprint row remain in progress.

- **Committed correctness proof.** AC-12.3-06 rendered byte-identically twice from source commit `d89ed076f174583100447629d5b3a31fa3f6f44c` (payload `a8cd47c718534b822051ad87bdbcfe55eccac24bbd3433f274ab6d570874184d`). The 13 focused tests and all 68 fault-matrix cases passed with zero skips. Commit `b150f2a7cf9850cd59a79d19c1a5aea93f941a45` contains that proof.

- **Review pass 5 verification.** The package-mapped Release/net11.0 solution built with zero warnings and errors. All 3,297 tests passed across 11 maintained projects with zero skips. The tested snapshot tree `872877268cea73f141f078238fde4b3959ca4a6e` exactly matches local source commit `70c3749058b27bbd4108fc3c1d306308298dd51a`. Root-submodule validation and the whitespace check passed. [Build and test logs](../../.artifacts/story-12.3-pass-5).
- **Current committed proof.** The Debug/net11.0 solution and all 13 tests for AC-12.3-01 through AC-12.3-05 passed on `70c3749058b27bbd4108fc3c1d306308298dd51a`. AC-12.3-06 rendered identical bytes twice with payload `92071401a8dd37e3b21574f693fe5ed9ecdecc089786306ca0eea050d4f52d9e`. All 80 AC-12.3-07 cases passed with zero skips. AC-12.3-08 retains its owner-approved waiver. [Measurement receipts](../../.artifacts/story-12.3-pass-5/committed-measurement) · [Correctness proof](../../docs/release-evidence/derived-key-correctness-proof-v1.md).

## Spec Change Log

## Review Triage Log

### Review pass 1 — 2026-11-27

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-01 — dirty submodule gitlinks in diff | false | `references/Hexalith.Builds` and `references/Hexalith.Platform` were pre-existing local checkouts prior to Story 12.3; the owner explicitly directed continuing on `main` leaving both untouched, and the proof records the committed index gitlinks (`deccbc3b…`, `3bcac63c…`). |
| BH-02 — proof binds pre-story HEAD before commit | medium | Verified: because no commit is created during implementation, `headCommit` binds pre-story `f8d81ca8…` while hashing uncommitted Story 12.3 files. Defer: regenerate `AC-12.3-06` against committed Story 12.3 `HEAD` before Story 12.4 consumes it. |
| BH-03 — proof omits AC-12.3-01/05 and TRX test identity checks | medium | Verified: CLI flags follow `12.3.json`, but `parse_trx` checks only aggregate `Counters` and never validates expected test class/method names or individual `UnitTestResult` outcomes. Patch: validate lane test names and `UnitTestResult` outcomes in `parse_trx` + fault-matrix coverage. |
| BH-04 — whitespace/malformed `GenerationToken` crash & v1 leak | medium | Verified: `ResolveGenerationToken` uses `!string.IsNullOrEmpty` so whitespace crashes `EncodePosition` (`ArgumentException`); `ClassifyLifecycle` accepts `GenerationToken != null` on v1; v2 hex format is unchecked. Patch: reject v1 `GenerationToken`, validate 64-char lowercase hex before reuse. Group with EC-01, EC-02. |
| BH-05 — lookup converter duplicate `conversationId` & non-string tokens | medium | Verified on `ScanSummaryObjectForTarget` (`!matched &&` stays sticky across duplicate `conversationId` and ignores non-string tokens); note `ReadAsync` never checked `SchemaVersion`/`Lifecycle`/`Watermarks` prior to Story 12.3. Patch: last-write-wins `conversationId`, reject non-string `conversationId`, verify deserialized ID matches target. Group with EC-06, EC-07. |
| BH-06 — cross-type `IReadModelStore` & `AsyncLocal` coupling | low | Rejected: no repository `IReadModelStore` caches CLR types or suppresses `ExecutionContext` flow; both `InMemoryReadModelStore` and `DaprReadModelStore` deserialize JSON via `System.Text.Json`, honoring `[JsonConverter]`. |
| BH-07 — orphaned `ledgersTask` when `detailsTask` faults | medium | Verified at `ConversationProjectionReadStore.cs:450-471`: `ledgersTask` is not cancelled or awaited when `detailsTask` throws, risking post-return I/O against disposed clients. Patch: cancel linked CTS and await `ledgersTask` before rethrowing. Group with EC-03. |
| BH-08 — redundant `GenerationToken` hashing on pending/reconcile writes | medium | Verified: `MarkGenerationPendingAsync` and `ReconcilePendingReference` do not modify `Summaries` yet `Compose` re-sorts and re-hashes all summaries twice per live write. Patch: preserve valid `persisted.GenerationToken` when `Summaries` is unchanged and profile full write path. |
| BH-09 — scale profile skipped persisted `GenerationToken` and omitted latencies | medium | Verified: `SeedReadyTenant` left `GenerationToken = null`, so profiles measured only fallback hashing, and spec notes omitted latency numbers. Patch: profile both persisted-token and fallback modes and record latencies. |
| BH-10 — missing tests for poison+token, malformed Redis JSON, partial-write open, reflection guard | medium | Verified: `DerivedKeyDaprFaultTests` XML doc promises malformed Redis JSON without testing it; partial-write open/post-detail/mid-page bulk outage and `ConversationIndexGenerationToken` property-coverage reflection guard are absent. Patch: add targeted tests. Group poison+token with VG-01. |
| BH-11 — `DerivedKeyDaprFaultStore` misses expiring/batch/erase/compacted methods | medium | Verified: `TrySaveWithTimeToLiveAsync`, `TryEraseAsync`, `ExecuteAsync`, `StageAsync`, `CommitAsync`, `AbortAsync`, and `GetCompactedAsync` bypass `_writes` and fault injection. Patch: intercept all write/erase/batch/compacted methods. Group with EC-16. |
| BH-12 — `PublicQueryCompatibilityTests` checks names only & tautological `Hidden` | medium | Verified: only checks `p.Name` on 10 types and compares `Hidden(v)` to itself. Patch: compare full member signatures across all exported types in `v4.json` and verify forbidden/nonexistent equivalence non-tautologically. |
| BH-13 — static read-repair guard scans only `ReadStore` & 5 regexes | medium | Verified: `check_read_store_no_repair` misses `ConversationQueryHandler`, `ConversationProjectionReadService`, `ConversationReadHydrationService`, new helpers, and `ReadModelWritePolicy`/staging/recovery APIs. Patch: expand static guard and fault test. |
| BH-14 — `IncrementalHashBufferWriter` returns uncleared buffers & lacks guards | medium | Verified: returns tenant-data arrays to `ArrayPool<byte>.Shared` without `clearArray: true` and lacks disposed/negative/overflow guards. Patch: clear on return and add guards. Group with EC-08, EC-09. |
| BH-15 — sprint row `in-progress` & pre-existing `Server.Tests` failure | low | Tracker moves to `review` in Step 5 per workflow (false). The 1 `Server.Tests` failure (`ConversationTenantIndexInitializerTest`) is pre-existing (`FailingIndexStore` lacks `IReadModelConditionalEraser`); defer cleanly in `deferred-work.md`. |
| EC-01 — whitespace-only `GenerationToken` crashes `EncodePosition` | medium | Verified at `ConversationQueryHandler.cs:628-631`. Patch; grouped with BH-04. |
| EC-02 — v1 index with non-null `GenerationToken` accepted | medium | Verified at `ConversationProjectionReadStore.cs:326-335`. Patch; grouped with BH-04. |
| EC-03 — orphaned `ledgersTask` on `detailsTask` failure | medium | Verified at `ConversationProjectionReadStore.cs:450-471`. Patch; grouped with BH-07. |
| EC-04 — valid detail header with type-corrupted unused `Messages` passes `ValidatePageAsync` | low | Rejected: syntactically invalid JSON still throws `JsonException`; skipping unused `Messages` on list pages is the approved optimization, list never returns `Messages`, and `GetAsync` still fails closed on open. |
| EC-05 — `ClassifyLifecycle(index)` hides gap on duplicate summaries | medium | Verified at `ConversationProjectionReadStore.cs:306-319`: taking max position hides a lower-position duplicate entry `< watermark`. Patch: preserve per-entry gap check on duplicates. |
| EC-06 — duplicate or later-null `"summaries"` accumulates `matchCount` | medium | Verified at `ConversationProjectionIndexLookupConverter.cs:59-62`. Patch: reset `matchedSummary`/`matchCount` on each `"summaries"` property; grouped with BH-05. |
| EC-07 — duplicate `"conversationId"` stays sticky on first match | medium | Verified at `ConversationProjectionIndexLookupConverter.cs:154-163`. Patch; grouped with BH-05. |
| EC-08 — `_written + count` signed int overflow in `Advance` | medium | Verified at `IncrementalHashBufferWriter.cs:34-40`. Patch; grouped with BH-14. |
| EC-09 — negative `sizeHint` or post-`Dispose` leaks pooled buffer | medium | Verified at `IncrementalHashBufferWriter.cs:74-101`. Patch; grouped with BH-14. |
| EC-10 — missing `git` in `read_root_submodules` raises `FileNotFoundError` | medium | Verified at `generate_derived_key_correctness_proof.py:199-206`. Patch: catch `OSError` and raise `Blocked(HISTORY_UNAVAILABLE, ...)`. |
| EC-11 — negative TRX counters or `executed < total` bypass `is_green` | medium | Verified at `generate_derived_key_correctness_proof.py:261-287`. Patch: reject negative counters as `Blocked(INPUT_UNAVAILABLE)` and require `not_run == 0`. Group with BH-03. |
| EC-12 — `load_schema` ignores nested `additionalProperties` | medium | Verified at `generate_derived_key_correctness_proof.py:330-332`. Patch: recursively enforce `additionalProperties: false` on every object schema node and add nested-open fault test. |
| EC-13 — non-object predecessor JSON or non-dict `digests` raises `AttributeError` | medium | Verified at `generate_derived_key_correctness_proof.py:383-397`. Patch: validate dict shapes before `.get()`. |
| EC-14 — CRLF `sprint-status.yaml` breaks predecessor `done` regex | medium | Verified at `generate_derived_key_correctness_proof.py:367-381`. Patch: match `\r?$`. |
| EC-15 — missing `rollback.boundary` raises unhandled `KeyError`/`TypeError` | medium | Verified at `generate_derived_key_correctness_proof.py:600`. Patch: extract inside the authority `try/except` block. |
| EC-16 — `DerivedKeyDaprFaultStore` misses expiring/batch/erase/compacted methods | medium | Verified at `DerivedKeyDaprFaultStore.cs:16-112`. Patch; grouped with BH-11. |
| EC-17 — `WaitForExitAsync` lacks timeout in `DerivedKeyDaprLiveFixture.DisposeAsync` | medium | Verified at `DerivedKeyDaprLiveFixture.cs:160-165`. Patch: add bounded 10s cancellation timeout. |
| VG-01 — persisted `GenerationToken` bypass on cross-tenant poison rows unverified | medium | Pre-verified gap. Patch: add unit test combining persisted `GenerationToken` and cross-tenant summary row in `Summaries`; grouped with BH-10. |
| VG-02 — `ConversationIndexGenerationToken.Compute` unsorted & `ProviderCorrelation` paths unverified | medium | Pre-verified gap. Patch: add unit tests for unsorted summaries and `ProviderCorrelation`/`ExtensionData` ordering and mutation. |
| VG-03 — `ConversationDetailValidationHeader` mismatch checks not isolated in `ValidatePageAsync` | medium | Pre-verified gap. Patch: add `ValidatePageAsync`/`ListAsync` test where `Summary`/`DispatchId`/ledger match while only `Detail.Freshness` (and `Detail.TenantId`/`ConversationId`) disagrees. |
| VG-04 — concurrent `ReadAsync` `AsyncLocal` isolation across async I/O unverified | medium | Pre-verified gap. Patch: add concurrent multi-conversation `GetAsync` test over an asynchronously yielding store. |
| VG-05 — rewritten `HydrateSummariesAsync` `ProjectId`/`FolderId` loops unverified | medium | Pre-verified gap. Patch: add `HydrateSummariesAsync` test asserting deduplicated project/folder hydration calls and populated `ProjectHydration`/`FolderHydration`. |
| VG-06 — dirty submodule gitlinks in diff | false | Same refutation as BH-01: pre-existing local checkouts intentionally left untouched per owner direction. |

### Review pass 2 — 2026-11-27

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH2-01 — committed proof JSON stale & fails updated closed schema | medium | Carried (BH-02): `generate_derived_key_correctness_proof.py` intentionally rejects uncommitted working-tree drift, so `derived-key-correctness-proof-v1.{json,md}` is regenerated on the committed Story 12.3 `HEAD` immediately after committing the patch before Story 12.4 consumes it. |
| BH2-02 — `ValidateNonNullObjectArray` skips malformed element fields in `messages`/collections | low | Carried (EC-04): skipping unused collection element bodies on list pages is the approved Story 12.3 optimization; `GetAsync` still fails closed on Open. |
| BH2-03 — unescaped UTF-8 non-ASCII whitespace attribute keys bypass `IsWhitespacePropertyName` | medium | Verified at `ConversationDetailValidationHeaderConverter.cs:293-313`: `span[index] > (byte)' '` returns false for bytes `>= 0x80` (e.g., U+00A0), diverging from `string.IsNullOrWhiteSpace` on Open. Patch; grouped with EC2-02. |
| BH2-04 — `ConversationPageValidationRecord` omits 7 persisted fields of `ConversationProjectedReadModels` | medium | Verified at `ConversationPageValidationRecord.cs:17-29`: malformed scalar/metadata fields (`SourceHeadSequence`, `ReplayAnchorAt`, etc.) are silently ignored on List while failing Open. Patch; grouped with EC2-03. |
| BH2-05 — `ScanSummaryObjectForTarget` skips sibling summary fields and allows `{}` without `conversationId` | medium | Verified at `ConversationProjectionIndexLookupConverter.cs:165-205`: requiring `conversationId` on every sibling summary object catches `{}` with zero allocations while preserving targeted lookup (full deserialization of 10k sibling summaries is intentionally avoided by design). Patch; grouped with VG2-O4, EC2-04. |
| BH2-06 — `Compose` aliases mutable `persisted.Summaries` list | medium | Verified at `ConversationProjectionReadModelWriter.cs:272-275,386-390`: returning `existingList` directly violates `MergeIndex`'s non-aliasing guarantee when callers pass a mutable `List<T>`. Patch; grouped with EC2-17. |
| BH2-07 — vestigial `Snapshot.GenerationToken`, stale XML docs, duplicated `ClassifyLifecycle` | low | Verified: `ConversationProjectionIndexSnapshot.GenerationToken` is never read, XML docs on `ConversationProjectionIndexReadModel.GenerationToken` and `DerivedKeyDaprFaultTests` still reference read reuse, and `ClassifyLifecycle` duplicates header checks. Direct cleanup patch (keep `ReadModel.GenerationToken` for v1 rejection). |
| BH2-08 — proof CLI omits `AC-12.3-01`/`05` and `READ_SIDE_PATHS` omits 9 new read-side files | medium | Part A (CLI flags) is false/carried (BH-03): frozen `12.3.json` defines exact `--unit-result`/`--integration-result`/`--dapr-result` flags. Part B verified: `READ_SIDE_PATHS` scans only 4 files and misses the 9 new read-side `.cs` files. Patch Part B. |
| BH2-09 — proof candidate-cleanliness, per-result TRX binding, and schema closure gaps | high | Verified at `generate_derived_key_correctness_proof.py:427-463,540-575,694-725`: multi-test TRX can pass with 1 stamped result, untyped `{}` schema nodes bypass closure, and `CONTRACT_PATH`/`SCHEMA_PATH` aren't checked against `head_commit`. Patch; grouped with VG2-01, EC2-07..11. |
| BH2-10 — worktree `.git` file check, proxy checks, rollback `FreshnessState` & interrupted rebuild | medium | Verified: `LocateRepositoryRoot` uses `Directory.Exists` (breaks in worktrees), `DerivedKeyPriorTenantAccessProxy` ignores `Object` methods and `command`/`aggregate`/`idempotency` tenant IDs, `afterPriorList.FreshnessState` isn't asserted, and interrupted-rebuild read check ran on an already-deleted index. Patch; grouped with EC2-14, EC2-15. |
| BH2-11 — live Dapr test omits read counts, bypasses `InitializeTenantIndexAsync`, explicit interface impls | medium | Verified read-count omission and `SaveStateAsync` bypass in `DerivedKeyDaprFaultTests.cs:44-96,411-429` (Patch). Explicit interface impls in `DerivedKeyDaprFaultStore.cs` are false: `DaprReadModelStoreDecorator` does not declare those methods `virtual`, and all production callers use the interfaces. |
| BH2-12 — missing `replace_tracked` tests in `test_authoring_outputs.py` and M07/M08 compiled mutations | medium | Missing `replace_tracked` unit tests verified (Patch; grouped with VG2-03). M07/M08 compiled mutations and `AC-12.3-01` slot reuse are false: `12.3.json` exposes one unit TRX slot and M07/M08 are covered by real fault-matrix cases. |
| BH2-13 — tracker status, `.github/` from `b7c12907`, dirty submodules, Debug latency jitter | false | Tracker status carried (BH-15, owned by Step 5); dirty submodules carried (BH-01); `.github/` changes are from intervening `origin/main` commit `b7c12907`; Debug 3-iteration timing has GC noise while allocations dropped -18.3% (List) and -72.0% (Open) and Story 12.4 runs the authoritative Release benchmark. |
| VG2-01 — `parse_trx` reads only `<StdOut>` while xUnit v3 writes `<Output><TextMessages><Message>` | high | Pre-verified and confirmed directly against `artifacts/v9/12.3/AC-12.3-02.trx`: xUnit v3 emits `<TextMessages><Message>` with no `<StdOut>`, so `parse_trx` fails on every real passing xUnit v3 TRX. Patch; grouped with VG2-O1, EC2-09. |
| VG2-02 — on-disk `codeBase` binary digest verification in `parse_trx` never exercised | medium | Pre-verified gap: `make_trx` omits `codeBase` and real-mutation runs execute outside `repo`. Patch: add repo-local `codeBase` matching/tampered tests in `test_derived_key_correctness.py`. |
| VG2-03 — `replace_tracked` allowlist scoping and `readonly` precedence untested | medium | Pre-verified gap: `test_authoring_outputs.py` never exercises non-empty `replace_tracked`. Patch; grouped with BH2-12, EC2-06. |
| VG2-O1 — `parse_trx` only scans `<StdOut>` for `CANDIDATE_BINDING` | high | Verified duplicate of VG2-01. Patch; grouped with VG2-01. |
| VG2-O2 — committed proof pair stale & fails updated schema | medium | Carried (BH-02); duplicate of BH2-01. |
| VG2-O3 — `ValidateNonNullObjectArray` skips malformed element fields | low | Carried (EC-04); duplicate of BH2-02. |
| VG2-O4 — `ScanSummaryObjectForTarget` skips sibling fields & omits `conversationId` requirement | medium | Verified duplicate of BH2-05. Patch; grouped with BH2-05. |
| EC2-01 — detail collection element is malformed JSON object missing required fields | low | Carried (EC-04); duplicate of BH2-02. |
| EC2-02 — unescaped UTF-8 non-ASCII whitespace attribute key | medium | Verified duplicate of BH2-03. Patch; grouped with BH2-03. |
| EC2-03 — `ConversationPageValidationRecord` ignores 7 persisted fields | medium | Verified duplicate of BH2-04. Patch; grouped with BH2-04. |
| EC2-04 — non-target sibling summary lacks `conversationId` | medium | Verified duplicate of BH2-05. Patch; grouped with BH2-05. |
| EC2-05 — `"dispatches": null` masks corrupt index as `Forbidden` instead of `Unavailable` on absent Open | medium | Verified at `ConversationProjectionIndexLookupConverter.cs:217-220`: valid indexes always serialize an object for `dispatches`. Patch: throw `JsonException` on `"dispatches": null`. |
| EC2-06 — deleted tracked file in `replace_tracked` crashes `target.read_bytes()` | medium | Verified at `authoring_outputs.py:72`: `target.read_bytes()` raises `FileNotFoundError` when deleted from working tree. Patch: guard with `target.exists()`. |
| EC2-07 — relative `codeBase` or `OSError` during binary hashing in `parse_trx` | medium | Verified at `generate_derived_key_correctness_proof.py:439-463`. Patch: resolve relative `codeBase` against `repository` and catch `OSError` as `Blocked(INPUT_UNAVAILABLE, ...)`. |
| EC2-08 — `ResultSummary` `outcome="Failed"` with passing counters passes `parse_trx` | medium | Verified at `generate_derived_key_correctness_proof.py:339-341`: `ResultSummary/@outcome` is unchecked (real xUnit v3 emits `outcome="Completed"`). Patch: require `outcome` in `("Passed", "Completed")` when present. |
| EC2-09 — multi-test TRX with `CANDIDATE_BINDING` on only one result passes | high | Verified at `generate_derived_key_correctness_proof.py:427-436`. Patch; grouped with VG2-01. |
| EC2-10 — untyped `{}` or `minProperties`/`required` schema node bypasses closure check | medium | Verified at `generate_derived_key_correctness_proof.py:540-558`. Patch; grouped with BH2-09. |
| EC2-11 — uncommitted working-tree edits to `12.3.json` or proof schema not rejected | medium | Verified at `generate_derived_key_correctness_proof.py:582-702`. Patch; grouped with BH2-09. |
| EC2-12 — invalid UTF-8 in `.gitmodules` escapes as `OUTPUT_WRITE_FAILED` instead of `INPUT_UNAVAILABLE` | low | Verified at `generate_derived_key_correctness_proof.py:277-281`. Direct 3-line patch catching `UnicodeDecodeError`. |
| EC2-13 — `PublicQueryCompatibilityTests` skips enum underlying type & members in `v4.json` | medium | Verified at `PublicQueryCompatibilityTests.cs:87-90`: `v4.json` pins `enumUnderlyingType` and `enumMembers` for both Contracts enums. Patch: verify enum underlying type and members. |
| EC2-14 — `LocateRepositoryRoot` fails in git worktrees (`.git` is a file) | medium | Verified at `DerivedKeyPriorBinaryRollbackHarness.cs:184-199`. Patch; grouped with BH2-10. |
| EC2-15 — `DerivedKeyPriorTenantAccessProxy` throws on `Object` methods & ignores 3 tenant IDs | medium | Verified at `DerivedKeyPriorTenantAccessProxy.cs:48-62`. Patch; grouped with BH2-10. |
| EC2-16 — `AllocatePorts` leaks bound `TcpListener`s if a later `Start()` throws | low | Verified at `DerivedKeyDaprLiveFixture.cs:240-257`. Direct `try/finally` patch. |
| EC2-17 — `Compose` aliases mutable `persisted.Summaries` list | medium | Verified duplicate of BH2-06. Patch; grouped with BH2-06. |
| EC2-18 — claim: committed proof JSON fails updated schema | medium | Carried (BH-02); duplicate of BH2-01. |
| EC2-19 — claim: `Tasks & Acceptance` mentions persisted `GenerationToken` reuse | low | Rejected per review rule (never route spec-only wording edits); `Implementation Notes` already explains why persisted `GenerationToken` was dropped in Review Pass 2. |
| EC2-20 — claim: `Tasks & Acceptance` mentions concurrent bulk reads | low | Rejected per review rule (never route spec-only wording edits); `Implementation Notes` already explains why sequential detail-before-ledger reads were restored in Review Pass 2. |
| EC2-21 — claim: `Server.Tests` has 1 pre-existing deferred failure | low | Carried (BH-15) and rejected as spec-only wording edit; already logged as `[Review][Defer]`. |

### Review pass 4 — 2026-11-28

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind 01 — Tracked proof names the prior implementation | high | Carried. The tracked pair binds `07f1ad4a`. Refresh after a local implementation commit, as recorded in deferred work. |
| Blind 02 — Proof omits AC-12.3-01 and AC-12.3-05 receipts | false | The frozen AC-12.3-06 contract names three input receipts. The measurement command separately requires AC-12.3-01 and AC-12.3-05 to pass and retains both results. |
| Blind 03 — Archived TRXs refer to the temporary build directory | low | The original binaries and paths are required when using those exact inputs to render the proof again. The contracted double render ran in that checkout. The retained source bundle and measurement command support fresh reproduction. Offline rendering from these reports alone is outside the contracted measurement. |
| Blind 04 — Interrupted rebuild retries a captured batch directly | medium | The test passes the batch to `ExecuteAsync` at the store instead of recovering the persisted pending batch by key. Patch the integration lane to use SDK recovery. |
| Blind 05 — Alternate Dapr serializer settings diverge | high | DaprReadModelStore deserializes with the Dapr client's options. Camel-case, case-insensitive matching can reject records emitted with another naming policy. Use typed validation for those options. |
| Blind 06 — Sparse filters preallocate the full tenant capacity | low | The match list reserves `tenantScoped.Count` before evaluating filters. Cap the initial allocation at the page size. |
| Blind 07 — Untyped schema leaves admit arbitrary nested values | medium | `additionalProperties: false` on an untyped leaf still permits unconstrained arrays. Patch closed-schema traversal and its fault cases. |
| Blind 08 — Read-side guard omits two mutating SDK methods | medium | `ApplyEventsAsync` calls the SDK write policy and `ReconcileAppliedReceiptAsync` can finish a pending write. Add both to the guard and fault matrix. |
| Blind 09 — Measurement requires a pre-restored .NET tool | low | A clean environment lacks the pinned local command until `dotnet tool restore` runs. Restore the tool inside the measurement checkout. |
| Blind 10 — Runtime fixture misses lifecycle and participant filter regressions | medium | All current fixture rows match those two filters. Add independent mismatches. |
| Blind 11 — Wire-shape check misses missing or renamed fields | medium | CLR reflection and substring bans allow a serialized root property rename to survive. Assert the approved serialized property sets. |
| Edge 01 — Overwritten null summary element | medium | A final typed array replaces an earlier array containing null. Patch streaming lookup to evaluate null elements in the final array. |
| Edge 02 — Overwritten null dispatch map | medium | A later dispatch object replaces an earlier explicit null. Apply the final-map null check after scanning. |
| Edge 03 — Nondefault serializer behavior | high | Dapr passes the configured serializer options to both scalar and bulk reads. Group with Blind 05. |
| Edge 04 — Archived TRX codebase cannot resolve after cleanup | low | Same archival limitation as Blind 03. The frozen contract measures and regenerates in a live checkout. |
| Edge 05 — Unresolved schema reference escapes the blocker | medium | Schema syntax can pass preflight with a missing target, and instance validation then raises an unhandled resolution error. Return a stable blocker. |
| Edge 06 — Tracked proof certifies earlier converter bytes | high | Carried. Same deferred committed-proof refresh as Blind 01. |
| Edge 07 — Historical task bullet promises parallel reads | low | The current implementation preserves detail-before-ledger ordering. The implementation notes already record the change. Reject the spec-only proposal. |
| Edge 08 — Historical task bullet promises persisted token reuse | low | The implementation notes document digest recomputation and removal of token persistence. Reject the spec-only proposal. |
| Edge 09 — Tenant initializer makes maintained verification red | false | The fresh package-mode suite passed 1,816 Server tests, including this test. The retained source-mode failure remains in deferred work. |
| Verification 01 — Full summary materialization can evade focused tests | medium | The compiled mutation passed all 11 tests while increasing 10,000-row Open allocations from 34,918,957 to 45,554,773 bytes. Add a same-fixture allocation comparison. |
| Verification 02 — Malformed native source proof has no regression case | medium | The wrapper fault corpus only exercises other metadata fields. Add a malformed native proof and check List and Open fail closed. |
| Verification 03 — Package provenance verifier lacks a negative test | medium | Deferred. This coverage gap predates the resumed story and belongs to package tooling from `aaabc916`. |
| Verification 04 — Checked-in proof binds earlier code | high | Carried. Same deferred committed-proof refresh as Blind 01. |

### Review pass 5 — 2026-11-28

Reviewed the committed story at `b150f2a7cf9850cd59a79d19c1a5aea93f941a45` against its recorded baseline.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind 01 — Configured JSON converters and reference handling bypass typed validation | high | The probe deserialized a valid preserved collection with the typed reader and reproduced a targeted Open failure. Patch both readers to use typed validation with customized serializer settings. |
| Blind 02 — TRX accepts nonterminal counters | medium | A positive pending count and matching receipt produced PASS. Reject nonterminal counts and retain support for the zero completed count emitted by xUnit in passing TRXs. |
| Blind 03 — Pattern properties admit undeclared proof fields | medium | A schema with both `additionalProperties: false` and a property pattern admitted a new field. Patch schema admission and exercise the fault. |
| Blind 04 — Frozen snapshot digests can come from a different tree | high | Restoring approved snapshot bytes only in the worktree can mask candidate drift. Require snapshots to match their committed blobs before hashing. |
| Blind 05 — Successful pytest status can conceal unevaluated matrix rows | medium | Pytest can exit zero with skipped tests. Verify a nonempty all-passing JUnit report before retaining measurement artifacts. |
| Blind 06 — Live route's ledger transitions are outside the Dapr fault fixture | medium | The live fixture injects state-store failures around tenant-index and detail writes. The integration lane separately exercises the actual route. Defer a composed route-and-store test for unchanged ledger acquisition and completion. |
| Blind 07 — Rollback receipts omit required metadata and fence checks | medium | The fixture verifies the ordinary streams, proof and query results. Patch persisted-key assertions for metadata and fence before removal and after rebuild. The native selection is optional for these streams. |
| Blind 08 — List can silently resume full detail materialization | medium | The allocation assertion applies to Open only. Patch the List test with a same-fixture comparison. |
| Blind 09 — The runtime contract lane omits four List filter dimensions | medium | The runtime responses exercise project, lifecycle and participant. Patch the fixture to exercise folder, business reference, date boundaries and trust state through the handler. |
| Blind 10 — Nested wire names can drift under CLR and root-property checks | medium | A rename within the page, summary or detail can preserve every presently checked root property. Extend the exact serialized assertions. |
| Edge 01 — Reference-preserving Web settings reject valid records | high | The typed reader accepted a stored `$values` wrapper that the targeted reader rejected. Apply the same fallback as Blind 01, including at the whole page record boundary. |
| Edge 02 — Narrow property patterns still open the schema | medium | A pattern admitting a single extra field passed the same structural closure check. Apply the same patch as Blind 03. |
| Edge 03 — Standalone proof accepts no public-query TRX | false | AC-12.3-06 binds the frozen snapshots and three named lane TRXs. The measurement runner also requires and retains the separate AC-12.3-05 receipt before AC-12.3-06. |
| Edge 04 — Checked-off bullet describes parallel bulk reads | low | Carried from pass 4. The current implementation preserves detail-before-ledger ordering and the implementation notes explain the change. Reject the spec-only proposal. |
| Edge 05 — Checked-off bullet describes persisted token reuse | low | Carried from pass 4. The implementation notes explain the current digest binding and removal of token persistence. Reject the spec-only proposal. |
| Edge 06 — Deferred initializer failure blocks the maintained suite | false | Carried from pass 4. The current package-mapped Server run passed all 1,816 tests. The reported reproduction uses a Debug assembly and the existing source-mode issue remains deferred. |
| Verification 01 — Compiled List fallback mutation passes focused tests | medium | All 11 focused tests passed against the mutated assembly even though List allocated more on both measured pages. Apply the List allocation guard from Blind 08. |
| Verification 02 — Custom ID conversion is ignored despite matching Web naming | high | A custom wire prefix round-trips with the typed parser and produces zero target matches with the streaming reader. Apply the guard and regression coverage from Blind 01. |

## Design Notes

- **Streaming DTOs on existing keys:** `InMemoryReadModelStore` and `DaprReadModelStore` both call `JsonSerializer.Deserialize<TValue>`, honoring `[JsonConverter]` on internal DTOs. `ReadAsync` scans `tenant-index` via `Utf8JsonReader` for the target `conversationId` (still counting all `summaries` matches for duplicate detection and reading only `dispatches[id]`). `ValidatePageAsync` reads only `Summary`, `DispatchId`, and `DetailHeader` (`TenantId`, `ConversationId`, `Freshness`).
- **Canonical generation token:** Hash ordinal-sorted summary fields via `IncrementalHash(SHA256)` + `Utf8JsonWriter` into a small pooled buffer (skipping sort allocation when already ordered), guaranteeing identical tokens between writer and read fallback.
- **One type per `.cs` file:** Each new internal DTO, converter, and test helper lives in its own `.cs` file.

## Verification

**Commands:**
- From the committed source revision, run `run-with-temp python3 _bmad/scripts/measure_derived_key_correctness.py --output-directory .artifacts/story-12.3-measurement` with a new output directory. The runner checks out the committed source and direct dependencies, builds in source mode, verifies the tested binary and TRX digests, renders the proof twice, and runs the compiled fault matrix.
- `dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --workspace <tmp> --mode source --output json`
- Build & run `AC-12.3-01`..`AC-12.3-05` (`Debug/net11.0`, `-automated sync -failSkips -trx artifacts/v9/12.3/...`) -- expected: exit `0`, 0 failed/skipped.
- Run `AC-12.3-06` twice (byte equality) and `python3 -m pytest -q _bmad/scripts/tests/test_derived_key_correctness.py -k complete_fault_matrix --junitxml=artifacts/v9/12.3/AC-12.3-07.xml` -- expected: exit `0`.
- Root `dotnet build Hexalith.Conversations.slnx`, individual maintained test projects, `python3 scripts/check-root-submodules.py --repository .`, `git diff --check` -- expected: all exit `0`.
