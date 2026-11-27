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

## Design Notes

- **Streaming DTOs on existing keys:** `InMemoryReadModelStore` and `DaprReadModelStore` both call `JsonSerializer.Deserialize<TValue>`, honoring `[JsonConverter]` on internal DTOs. `ReadAsync` scans `tenant-index` via `Utf8JsonReader` for the target `conversationId` (still counting all `summaries` matches for duplicate detection and reading only `dispatches[id]`). `ValidatePageAsync` reads only `Summary`, `DispatchId`, and `DetailHeader` (`TenantId`, `ConversationId`, `Freshness`).
- **Canonical generation token:** Hash ordinal-sorted summary fields via `IncrementalHash(SHA256)` + `Utf8JsonWriter` into a small pooled buffer (skipping sort allocation when already ordered), guaranteeing identical tokens between writer and read fallback.
- **One type per `.cs` file:** Each new internal DTO, converter, and test helper lives in its own `.cs` file.

## Verification

**Commands:**
- `dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --workspace <tmp> --mode source --output json`
- Build & run `AC-12.3-01`..`AC-12.3-05` (`Debug/net11.0`, `-automated sync -failSkips -trx artifacts/v9/12.3/...`) -- expected: exit `0`, 0 failed/skipped.
- Run `AC-12.3-06` twice (byte equality) and `python3 -m pytest -q _bmad/scripts/tests/test_derived_key_correctness.py -k complete_fault_matrix --junitxml=artifacts/v9/12.3/AC-12.3-07.xml` -- expected: exit `0`.
- Root `dotnet build Hexalith.Conversations.slnx`, individual maintained test projects, `python3 scripts/check-root-submodules.py --repository .`, `git diff --check` -- expected: all exit `0`.
