---
baseline_commit: dfecb715d87b4c7a0abcc95be99b446a8719cfb3
---

# Story 2.4: Persist read models via the shared store + write policy

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversations maintainer,
I want conversation read models persisted and read through the shared EventStore `IReadModelStore` (+ ETag) and updated through the `ReadModelWritePolicy` optimistic-concurrency loop,
so that the module has a real, durable read-model persistence substrate — closing the production read-store binding gap deferred from Story 2.3 — with no-lost-update concurrency behavior proven, and without any hand-rolled state-store or merge-on-write code surviving.

This is the **fourth story of Epic 2** (Consume Existing Technical-Module Surface) and the fourth `src/` production change in the initiative. It covers **FR-5**. Relevant NFRs: **NFR1** (behavior preservation), **NFR2** (no hot-path read regression — durable read by key, no N+1, snapshot/projection use preserved), **NFR5** (replay safety — the write transform is idempotent), **NFR8** (public-surface / EventStore-concept boundary preserved).

> **READ THIS FIRST — this story is greenfield-adopt, NOT remove-and-replace.** The epics labels Story 2.4 *(remove-and-replace)* on the addendum's first-pass assumption that Conversations had "hand-written Dapr state-store calls and merge-on-write loops" to delete. **It does not.** Verified against the working tree at `dfecb71`: there is **zero** Dapr/`IStateStoreManager`/`DaprClient`/ETag/merge-loop/optimistic-concurrency code anywhere under `src/` for read-model persistence. What actually exists is:
>
> 1. A **read-only abstraction** `IConversationProjectionReadStore` (`ReadAsync`/`ListAsync`) that **five production services depend on** but which has **no production DI registration** — it is only ever satisfied by in-memory fakes in tests. Story 2.3's Dev Agent Record explicitly deferred this: *"The persisted projection read-store binding (`IConversationProjectionReadStore`) remains a Story 2.4 concern; it is faked in the discovery/dispatch tests."* **The production host cannot resolve `ConversationQueryHandler` today** (missing `IConversationProjectionReadStore`) — this story closes that gap.
> 2. A `ConversationProjectionMaterializer` that materializes read models **in memory from a full event replay** and **persists nothing**. The materializer orchestration is **out of scope** here — it is `Server/Projections/**` = inventory area `projection-orchestration` (**Promote → FR-6 / Story 2.5**). Do **not** touch the materializer's replay/dispatch orchestration in this story.
>
> So this story **adds** the persistence substrate the SDK already provides (pure consume — no submodule edit, no local code to delete): register the SDK read-model store, implement the **production** `IConversationProjectionReadStore` over the SDK `IReadModelStore`, and add a thin write path through `ReadModelWritePolicy` whose optimistic-concurrency / no-lost-update behavior is proven by tests. The "remove-and-replace → greenfield-adopt" disposition correction is recorded per the Story 1.5 escape hatch (see AC-5 / Task 6).

## Acceptance Criteria

1. **(AC-1 — the SDK read-model store is registered and the production `IConversationProjectionReadStore` binding is closed)**
   Given the Conversations server host (`AddConversationQueriesCore` / `Program.cs`), when the application's service provider is built, then `IReadModelStore` resolves to the SDK `DaprReadModelStore` via `services.AddEventStoreReadModelStore()` (with `DaprClient` available — `AddDaprClient()` registered if the shared host does not already provide it), **and** `IConversationProjectionReadStore` resolves to a **new production implementation** backed by that `IReadModelStore`. After this story, `ConversationQueryHandler`, `ConversationProjectionReadService`, `ConversationAuditRecordAccessService`, `ConversationGovernanceVerificationService`, and `ConversationPrivilegedOperationalJustificationService` all resolve from the real host (the deferred-from-2.3 binding gap is closed). A host-composition test proves the full query/governance dependency graph builds with the production registrations (no missing-service throw).

2. **(AC-2 — read models are persisted/updated through `ReadModelWritePolicy` with optimistic concurrency)**
   Given a materialized `ConversationProjectedReadModels` (summary + detail), when it is persisted, then persistence goes through `ReadModelWritePolicy` (`UpdateAsync` / `ApplyEventsAsync` / `MergeAsync`) against `IReadModelStore` using a stable, tenant-scoped key scheme — **never** a direct unconditional `IReadModelStore.SaveAsync` on the conversation read-model write path, and **never** a hand-rolled read-modify-write loop. The write transform passed to the policy is **idempotent** (re-applying the same materialization yields the same persisted value — NFR5). There is no `DaprClient`/`IStateStoreManager`/`SaveStateAsync`/`TrySaveStateAsync` call and no hand-rolled ETag/merge loop anywhere under `src/Hexalith.Conversations.*` (grep-clean) — all concurrency is delegated to the SDK policy.

3. **(AC-3 — concurrent writers do not lose updates)**
   Given two writers racing to update the same conversation read model, when one write loses the ETag race, then the SDK policy reloads the latest value and re-applies the transform (no lost update; first-write-wins with bounded retry), and the final persisted state reflects both writers' effects. This is proven by a test that injects exactly one ETag conflict (via the SDK `InMemoryReadModelStore.ConcurrentWriteBeforeTrySave` hook) and asserts the retry observed the competing write and re-applied over it (not a blind overwrite). Retry exhaustion surfaces the policy's `InvalidOperationException` (fail-loud, not silent loss) — asserted by a second test.

4. **(AC-4 — read path behavior, freshness gates, and public contract shapes are unchanged)**
   Given the production `IConversationProjectionReadStore` reads persisted read models by tenant + conversation key (detail/summary) and the tenant-scoped list, when `ConversationProjectionReadService.ReadDetailAsync` / `ConversationQueryHandler.ListAsync` consume it, then all existing fail-closed behavior is preserved **by construction**: a missing key → the same `Forbidden`/`null` shape; a store exception → `Unavailable` (no raw error leak); tenant/conversation mismatch → `PoisonEvent`; mixed-generation summary/detail → `Rebuilding`; freshness gating (`AllowsTrustBearingDecision`) unchanged. `ListAsync` reads the tenant boundary **without N+1** per-conversation round-trips (a tenant-scoped index read model or equivalent single read — NFR2, no hot-path regression). The `Contracts/Projections` DTOs (`ConversationSummaryProjectionV1`, `ConversationDetailProjectionV1`, `ProjectionFreshnessV1`, …) are **not modified** — they are Keep domain surface and are in the 196-type public-contract-shape baseline; the **public contract-shape diff vs the Story 1.1 snapshot is empty**. The store interface and its implementation live in the **Server** assembly (not the public adopter surface — NFR8).

5. **(AC-5 — disposition correction logged; ledger updated; standing conformance gate holds)**
   The "remove-and-replace → greenfield-adopt" correction for FR-5/Story 2.4 (no bespoke Dapr/merge code existed; the work is additive adoption + closing the deferred read-store binding) is recorded as an **append-only** `Story24StructuralDispositions` section in the FR-20 at-risk register (`tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` → **regenerate** `docs/release-evidence/at-risk-test-register-v1.{json,md}`; **never hand-edit** the JSON), following the `Story22`/`Story23StructuralDispositions` precedent. The full conformance suite is **100% green** on the story branch and **≥ 353 (monotonic)** — Story 2.3 closed at **353**; the new persistence/concurrency tests and the Story24 ledger fact must hold or grow the count, never regress (assertion strength must not drop vs the Story 1.1 baseline). The **public contract-shape diff** vs the Story 1.1 snapshot (`docs/release-evidence/public-contract-shape-baseline-v1.json`, 196 types) is **empty**. No `src/` **public** contract change. No hot-path read regression (NFR1/NFR2).

## Tasks / Subtasks

- [x] **Task 1 — Map the current persistence reality & SDK seams (read-only baseline)** (AC: 1, 2, 4)
  - [x] Confirm `IConversationProjectionReadStore` (`src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs`) is read-only (`ReadAsync(tenantId, conversationId)`, `ListAsync(tenantId)`) and has **no production registration** (grep all `*ServiceCollectionExtensions.cs` + `Program.cs`). Confirm the 5 production consumers (query handler, projection-read service, audit-record access, governance-verification, privileged-justification) inject it as a required ctor parameter.
  - [x] Confirm there is **no** Dapr/state-store/ETag/merge code under `src/Hexalith.Conversations.*` (grep `DaprClient|IStateStoreManager|SaveStateAsync|TrySaveStateAsync|ETag|FirstWrite|optimistic`). Record the empty result in the Dev Agent Record — this is the evidence the story is greenfield-adopt, not remove-and-replace.
  - [x] Re-read the SDK seams (EventStore submodule, pure consume — **no edit**): `IReadModelStore` (`GetAsync`→`ReadModelEntry<TValue>(Value, ETag)`, `SaveAsync`, `TrySaveAsync(…, etag)`), `ReadModelWritePolicy` (`UpdateAsync` / `ApplyEventsAsync` / `MergeAsync`, default 3 attempts, throws `InvalidOperationException` on exhaustion, transform MUST be idempotent), `ReadModelWriteContext`, `AddEventStoreReadModelStore()`, `DaprReadModelStore`, and the testing fake `InMemoryReadModelStore` (`ConcurrentWriteBeforeTrySave`, `SeedRaw`, `Snapshot`). See **SDK seams** in Dev Notes for exact signatures.
  - [x] Study the **sibling precedent** `Hexalith.Tenants/src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs` (StateStoreName `"statestore"`, key prefixes, `ReadModelWritePolicy.ApplyEventsAsync` for per-aggregate models + `MergeAsync` for the singleton index, idempotent merge returning a **new** instance with dedup) and `Hexalith.Tenants/src/Hexalith.Tenants/Program.cs` (`AddDaprClient()` then `AddEventStoreReadModelStore()`). Mirror this shape.
  - [x] **Before building, verify submodule gitlinks are at their recorded commits** (root-level, non-recursive — CLAUDE.md compliant; never `--init --recursive`). Story 2.2/2.3 found drift in Tenants/Parties/FrontComposer that broke the Release build (EventStore must be `ad2c957`). See Carry-forward.
- [x] **Task 2 — Register the SDK read-model store on the host** (AC: 1)
  - [x] In `ConversationQueryServiceCollectionExtensions.AddConversationQueriesCore` (the private core called by both `AddConversationQueries` overloads), register `services.AddEventStoreReadModelStore()` (idempotent `TryAddSingleton<IReadModelStore, DaprReadModelStore>`).
  - [x] Ensure `DaprClient` is available: the SDK store and the SDK `DaprStateStoreHealthCheck` both resolve `DaprClient`. **Verify whether the shared host (`AddEventStoreDomainService`) already registers `DaprClient`** — if not, add `builder.Services.AddDaprClient()` in `Program.cs` **before** the store is used (mirror Tenants' `Program.cs:47`). Do not register Dapr twice; `TryAdd*` semantics make a redundant call safe, but confirm the resolution path.
  - [x] Keep the state-store component name consistent with the sibling convention (`"statestore"`) unless a Conversations-specific component is already configured — record the chosen name in the Dev Agent Record.
- [x] **Task 3 — Implement the production read-store + write path over `IReadModelStore`** (AC: 1, 2, 4)
  - [x] Add a production `ConversationProjectionReadStore : IConversationProjectionReadStore` (new file under `src/Hexalith.Conversations.Server/Projections/`) that reads persisted read models via `IReadModelStore.GetAsync<…>(storeName, key)`:
    - **Detail/summary read** (`ReadAsync`): key `projection:conversations:{tenantId}:{conversationId}` → the persisted `ConversationProjectedReadModels` (summary + detail pair from the same materialization pass). Return `null` when absent (preserving the existing `Forbidden` shape in `ConversationProjectionReadService`).
    - **List** (`ListAsync`): read a **single** tenant-scoped index read model `projection:conversations-index:{tenantId}` holding the visible `ConversationSummaryProjectionV1` set — **not** a per-conversation fan-out (NFR2, no N+1). See the **Read-model keying** table in Dev Notes; the exact index shape is a recorded design decision (Tenants uses a singleton index — mirror it per-tenant here).
  - [x] Add the **write path** through `ReadModelWritePolicy` (a thin `ConversationProjectionReadModelWriter` or equivalent persistence component under `Server/Projections/`): persist a materialized `ConversationProjectedReadModels` via `ReadModelWritePolicy.UpdateAsync`/`ApplyEventsAsync` (per-conversation key) and merge its summary into the tenant index via `ReadModelWritePolicy.MergeAsync` (idempotent dedup by `ConversationId` + projection generation, returning a **new** instance — mirror Tenants' `MergeAuditState`). The transform/merge MUST be idempotent (NFR5). Pass a `ReadModelWriteContext` for diagnostics.
  - [x] Register both the write component and `IConversationProjectionReadStore` → `ConversationProjectionReadStore` in `AddConversationQueriesCore` (production binding). Use `TryAdd*` so test compositions can override with a fake.
  - [x] **Scope discipline:** do **not** implement `IDomainProjectionHandler`, do **not** wire the materializer to call the writer on replay, and do **not** remove/alter `ConversationProjectionMaterializer`'s orchestration — that is Story 2.5 (FR-6). This story delivers the persistence **substrate** (store binding + read adapter + write-via-policy seam + concurrency proof); 2.5 drives it. The writer is the documented seam 2.5 will call.
- [x] **Task 4 — Add persistence + concurrency tests** (AC: 2, 3, 4)
  - [x] Add the **EventStore.Testing** project reference to `tests/Hexalith.Conversations.Server.Tests` so tests can use the SDK `InMemoryReadModelStore` (with its `ConcurrentWriteBeforeTrySave`/`SeedRaw`/`Snapshot` hooks). This is the canonical store double — using it introduces **no** duplicate in-module fake (it does not conflict with Story 2.7, which *removes* duplicates; adding the reference is the correct consume). If a CPM/reference constraint blocks it, fall back to a minimal local `IReadModelStore` fake **with the same conflict-injection hook**, and note it for 2.7.
  - [x] **Round-trip test:** persist a `ConversationProjectedReadModels` via the writer, then read it back via `ConversationProjectionReadStore.ReadAsync` and `ListAsync` (through the index) — assert identical summary/detail and that `ListAsync` performs a single index read (no per-conversation fan-out).
  - [x] **No-lost-update test (AC-3):** seed a competing write through `InMemoryReadModelStore.ConcurrentWriteBeforeTrySave` to force exactly one ETag conflict, then clear the hook; assert the policy reloaded and re-applied over the competing value (final state reflects both, not a blind overwrite — mirror EventStore's `ReadModelWritePolicyTests.UpdateAsync_EtagConflictThenSuccess_ReloadsAndMergesLatest`).
  - [x] **Retry-exhaustion test:** keep the conflict hook firing so every `TrySave` loses; assert `ReadModelWritePolicy` throws `InvalidOperationException` (fail-loud) rather than silently dropping the update.
  - [x] **Idempotency test (NFR5):** apply the same materialization/merge twice; assert the persisted value is unchanged the second time (no duplicate index entries; dedup by `ConversationId`/generation).
  - [x] **Read fail-closed re-assertions (AC-4):** with the production `ConversationProjectionReadStore` over an `InMemoryReadModelStore`, drive `ConversationProjectionReadService.ReadDetailAsync` through: absent key → `Forbidden`; store throws → `Unavailable`; tenant/conversation mismatch → `PoisonEvent`; mixed-generation → `Rebuilding`. These re-express the existing fake-backed assertions against the real read-store-over-`IReadModelStore` path — **do not weaken** them. Existing tests that use in-memory `FakeProjectionReadStore` stay green unchanged.
  - [x] **Host-composition test (AC-1):** assert the production service provider (with `AddConversationQueries` + `AddEventStoreReadModelStore` + a test `DaprClient` or in-memory store override) resolves `ConversationQueryHandler` and the four governance/read services with no missing-service throw. Extend/align with `ConversationsDomainDiscoveryHostCompositionTest` (its `EmptyProjectionReadStore` fake is test-only; the production binding is the new fact).
  - [x] Use only packages already in the Conversations CPM (xUnit v3, Shouldly, NSubstitute) plus the additive `Hexalith.EventStore.Testing` project reference — no new package version.
- [x] **Task 5 — Verify the read path has no hot-path regression** (AC: 4, NFR2)
  - [x] Confirm `ConversationQueryHandler.ListAsync` consumes `ListAsync` as a single tenant-scoped index read (no per-conversation `GetAsync` loop); confirm `ReadDetailAsync` is a single keyed `GetAsync`. No synchronous cross-service hot-path calls added. Snapshot/projection use is preserved (the read store is the projection read side; the aggregate write path is untouched).
- [x] **Task 6 — Record the disposition in the FR-20 ledger + log the greenfield-adopt correction** (AC: 5)
  - [x] Extend `AtRiskTestRegisterGenerationTest.cs` with a parallel `Story24StructuralDispositions` section recording: (1) FR-5/2.4 is **greenfield-adopt** — there was **no** bespoke Dapr state-store / merge-on-write code to delete (the epics' "remove-and-replace" label is corrected here); the work **adds** the SDK `IReadModelStore` + `ReadModelWritePolicy` adoption and closes the deferred-from-2.3 production `IConversationProjectionReadStore` binding (Consume, FR-5); (2) the new persistence/concurrency tests are additive (no test removed/weakened); (3) the `Contracts/Projections` field-selection/freshness DTOs remain **Keep** and shape-unchanged. **Regenerate** the `.json` via the test; **never hand-edit**. Update the companion `.md`. Append-only — do not rewrite accepted rows.
  - [x] **Inventory note (read before assuming a changeLog entry is needed):** there is **no FR-5 / read-model-store area** in `consume-promote-keep-inventory-v1.json` — the closest area, `projection-orchestration` (`src/Hexalith.Conversations.Server/Projections/**`, **Promote**, 1,800 LOC), is earmarked for **FR-6 / Story 2.5** and is **not** reclassified by this story. This story **adds** production code into that folder (the read-store + writer) but **changes no area's Consume/Promote/Keep label**, so the FR-2 no-silent-change invariant is not engaged and **no inventory `reclassification` changeLog entry is required** (mirrors Story 2.3's "no glob empties → no changeLog entry" reasoning). The "remove-and-replace → greenfield-adopt" correction is a **story-scope disposition** recorded in the FR-20 ledger (above), not an inventory relabel. If your implementation unexpectedly empties or reclassifies an existing area, follow the `classification-change-procedure-v1` append-only `changeLog` procedure (Story 1.5) instead. Do **not** mutate any area's frozen `approxLoc`.
- [x] **Task 7 — Run the standing conformance gate and generate the Dev Agent Record last** (AC: 5)
  - [x] Build `Hexalith.Conversations.slnx` **Release** (0 warnings — warnings-as-errors). Run the full conformance suite + Server/Tests per-project (`dotnet test tests/Hexalith.Conversations.Conformance.Tests/`, etc. — Conformance/Server tests run **per-project**, not solution-wide). Confirm green **≥ 353 (monotonic)**, public-contract-shape baseline JSON **byte-unchanged** (diff empty), no `src/` **public** contract change.
  - [x] **Generate the Dev Agent Record test counts / File List from the final `dotnet test` run as the LAST step** (Epic 1 retro P1/P2 + the 2.3 MEDIUM-1 count-drift recurrence — generate it last so the record matches the working tree at first review).

## Dev Notes

### Read-model keying — recommended scheme (mirror the Tenants precedent)

| Read model | Key | Write via | Read via |
|---|---|---|---|
| Per-conversation summary+detail pair (`ConversationProjectedReadModels`) | `projection:conversations:{tenantId}:{conversationId}` | `ReadModelWritePolicy.UpdateAsync`/`ApplyEventsAsync` (idempotent transform) | `IConversationProjectionReadStore.ReadAsync` → `IReadModelStore.GetAsync` |
| Per-tenant summary index (visible `ConversationSummaryProjectionV1` set) | `projection:conversations-index:{tenantId}` | `ReadModelWritePolicy.MergeAsync` (idempotent merge, dedup by `ConversationId`+generation, returns a **new** instance) | `IConversationProjectionReadStore.ListAsync` → single `IReadModelStore.GetAsync` (no N+1) |

- State-store component name: `"statestore"` (sibling convention; confirm/record). Keys are tenant-scoped strings — the SDK `IReadModelStore` has no built-in tenant awareness, the **caller bakes tenant into the key** (Tenants does the same). This keeps cross-tenant reads impossible by construction (a different tenant → a different key).
- The persisted value types are the **existing** `ConversationProjectedReadModels` (Server) / `ConversationSummaryProjectionV1` + `ConversationDetailProjectionV1` (Contracts, already in the public baseline). They must be JSON-serializable (records are). **Do not reshape them** — persistence rides the existing shapes (AC-4/AC-5 empty-diff gate).
- The exact index shape (singleton-per-tenant vs a different partition) is a **recorded design decision** — the Tenants singleton-index pattern is the recommended default. If you choose differently, document why in the Dev Agent Record; the binding invariant is: `ListAsync(tenantId)` is **one** store read, not a fan-out.

### SDK seams (authoritative facts — pure consume, no submodule edit)

- **`IReadModelStore`** (`Hexalith.EventStore.Client.Projections`, `Hexalith.EventStore.Client.csproj`):
  - `Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken)` where `TValue : class` → `ReadModelEntry<TValue>(TValue? Value, string? ETag)` (`(null, null)` when absent).
  - `Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken)` — unconditional last-write-wins. **Do not use on the conversation read-model write path** (AC-2) — always go through the policy.
  - `Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken)` — first-write-wins; returns `false` on ETag conflict (not an exception). [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs:1-80`]
- **`ReadModelWritePolicy`** (same namespace; **static** helper):
  - `UpdateAsync<TValue>(store, storeName, key, Func<TValue?,TValue> update, ReadModelWriteContext context = default, ILogger? logger = null, int maxAttempts = 3, CancellationToken = default)` — read current+ETag → run `update` → `TrySaveAsync`; **on conflict re-reads and re-runs `update`** up to `maxAttempts`; throws `InvalidOperationException` on exhaustion (store/key/attempts in the message). `update` MUST be idempotent.
  - `ApplyEventsAsync<TValue>(…, IReadOnlyCollection<ProjectionEventDto?> events, Func<TValue> defaultFactory, Action<TValue,ProjectionEventDto> applyEvent, …)` — seed-or-load then apply each event; `applyEvent` MUST be idempotent (events may re-apply on retry).
  - `MergeAsync<TValue>(…, TValue incoming, Func<TValue> defaultFactory, Func<TValue,TValue,TValue> merge, …)` — seed-or-load then merge; `merge` MUST be idempotent and return a **new** instance (never mutate the persisted arg). [Source: `…/Client/Projections/ReadModelWritePolicy.cs:1-289`]
- **`AddEventStoreReadModelStore(this IServiceCollection)`** (`Hexalith.EventStore.Client.Registration`): `TryAddSingleton<IReadModelStore, DaprReadModelStore>()`; requires a registered `DaprClient`. [Source: `…/Client/Registration/ReadModelStoreServiceCollectionExtensions.cs:19-23`]
- **`DaprReadModelStore`** (production): wraps `DaprClient` — `GetStateAndETagAsync`, `SaveStateAsync`, `TrySaveStateAsync(…, new StateOptions { Concurrency = ConcurrencyMode.FirstWrite })`. [Source: `…/Client/Projections/DaprReadModelStore.cs:1-69`]
- **`InMemoryReadModelStore`** (`Hexalith.EventStore.Testing.Fakes`, public): realistic ETag/first-write-wins semantics; `Action? ConcurrentWriteBeforeTrySave` (fires before each `TrySaveAsync` ETag check — inject a competing `SeedRaw` then clear it), `SeedRaw<T>`, `Snapshot<T>`, `Count`. JSON round-trips on read/write to avoid reference aliasing. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/InMemoryReadModelStore.cs:1-130`]
- **Sibling precedent (mirror this):** `Hexalith.Tenants/src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:24-149` — `StateStoreName="statestore"`, key prefixes, `ApplyEventsAsync` for per-aggregate + `MergeAsync` for the singleton index, `MergeAuditState` idempotent dedup-by-EventId returning a new instance. Registration: `Hexalith.Tenants/src/Hexalith.Tenants/Program.cs:47` (`AddDaprClient()`), `:64` (`AddEventStoreReadModelStore()`). Policy tests to mirror: `Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests/Projections/ReadModelWritePolicyTests.cs:24-72`.

### The current persistence reality (authoritative facts)

- `IConversationProjectionReadStore` (`src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs:14-37`) — read-only (`ReadAsync(tenantId, conversationId)`, `ListAsync(tenantId)`). **No production registration anywhere** — only ~12 in-memory test fakes (`FakeProjectionReadStore`/`EmptyProjectionReadStore`). Five production services require it: `ConversationQueryHandler` (`Queries/ConversationQueryHandler.cs:38`), `ConversationProjectionReadService` (`Projections/ConversationProjectionReadService.cs:19`), `ConversationAuditRecordAccessService` (`Queries/ConversationAuditRecordAccessService.cs:20`), `ConversationGovernanceVerificationService` (`Governance/ConversationGovernanceVerificationService.cs:38`), `ConversationPrivilegedOperationalJustificationService` (`Governance/…Service.cs:21`).
- `ConversationProjectedReadModels` (`Projections/ConversationProjectedReadModels.cs:15-28`) — `sealed record (ConversationSummaryProjectionV1 Summary, ConversationDetailProjectionV1 Detail)`. This is the per-conversation persisted value.
- `ConversationProjectionReadService.ReadDetailAsync` (`:39-110`) — the fail-closed read boundary: tenant-access gate → `ReadAsync` (catch → `Unavailable`) → null → `Forbidden` → tenant/conversation mismatch → `PoisonEvent` → mixed-generation → `Rebuilding` → freshness `AllowsTrustBearingDecision` gate. **Preserve every branch** (AC-4).
- `ConversationQueryHandler.ListAsync` (`:178-321`) — calls `_projectionReadStore.ListAsync(tenantId)` (catch → `Unavailable`), tenant-scoped poison guard, mixed-generation guard, generation-token pagination. The list must stay a single index read (NFR2).
- `Program.cs` (`src/Hexalith.Conversations.Server/Program.cs:22-31`) — the two-line shared host (Story 2.1) + `AddConversationTenantAccess()` + `AddConversationQueries(builder.Configuration)` (Story 2.3). It does **not** register `DaprClient` or `IReadModelStore`. `AddConversationQueriesCore` (`Queries/ConversationQueryServiceCollectionExtensions.cs:86`) is the private core where the store + read-store registration belongs.
- `ConversationProjectionMaterializer.cs` (`Projections/ConversationProjectionMaterializer.cs`, 953 LOC) — in-memory full-replay materializer; **persists nothing**; **out of scope** (Promote → FR-6/2.5). Do not touch its orchestration.

### Scope Boundaries — what this story does and does NOT do

**DOES (FR-5, greenfield-adopt):**
- Register the SDK `IReadModelStore` (`AddEventStoreReadModelStore()`) + ensure `DaprClient` (Task 2).
- Implement the **production** `IConversationProjectionReadStore` over `IReadModelStore` (read side; closes the deferred-from-2.3 binding) + a thin write path through `ReadModelWritePolicy` (Task 3).
- Prove no-lost-update / retry-exhaustion / idempotency / fail-closed-read behavior (Task 4); record the disposition (Task 6).

**DOES NOT (actively avoid scope creep):**
- **Do NOT implement `IDomainProjectionHandler`, touch `ConversationProjectionMaterializer`'s replay/dispatch orchestration, or remove the materializer** — that is Story 2.5 (FR-6). This story provides the persistence substrate; 2.5 wires the projection seam that drives it.
- **Do NOT modify `Contracts/Projections` DTOs** (`ConversationSummaryProjectionV1`, `ConversationDetailProjectionV1`, `ProjectionFreshnessV1`, freshness/reason-code converters). They are Keep and in the 196-type public baseline — changing them breaks the empty-diff gate. Persist them as-is.
- **Do NOT touch the query cursor / `IDomainQueryHandler` adapters (2.3), the aggregate (2.2), or the host wiring beyond adding the store/Dapr registration (2.1).**
- **Do NOT edit** EventStore/Tenants/Parties/FrontComposer sources — the SDK read-model seams already exist (pure consume; no backward-compat edit needed). Do NOT consolidate `ServiceDefaults`/`AppHost`/`Aspire` (Epic 3).
- **Do NOT** adopt the projection seam (2.5), serialization helpers (2.6), or broadly swap to EventStore.Testing fakes (2.7) — adding the `EventStore.Testing` reference for `InMemoryReadModelStore` is the one allowed test-side consume here, and it removes no duplicate.

### Standing conformance gate (applies to every Epic 2–4 story)

Suite 100% green on the branch; public contract-shape diff vs the Story 1.1 snapshot empty or explicitly approved & recorded; the local copy deleted **where one exists** (here: none — greenfield-adopt, recorded); no test deleted/weakened without a recorded FR-20 ledger justification. Gate **≥ 353 (monotonic)** (Story 2.3 closed at 353). [Source: epics.md#Epic-2 standing-conformance-gate; 2.3 close = 353]

### Carry-forward technical-debt awareness (do not let it flake the gate)

- **Submodule working-tree drift (CRITICAL — broke the 2.2 Release build):** verify all root-level submodule gitlinks are at their recorded commits before building (EventStore `ad2c957`, FrontComposer `451830b`, Parties `485616f`, Tenants `5b4424e` per `git submodule status` at `dfecb71` — all clean). Root-level checkout, non-recursive (CLAUDE.md). Never `git submodule update --init --recursive`. [Source: 2.2 §1 / 2.3 Debug Log]
- **Generate the Dev Agent Record (counts + File List) LAST** from the final `dotnet test` run — the count drifted in all 5 Epic 1 stories, the 2.2 first submission, and again in 2.3 (MEDIUM-1, 530→535). [Source: epic-1-retro P1/P2; 2.3 review MEDIUM-1]
- **Conformance/Server tests run per-project**, not solution-wide. Use `Hexalith.Conversations.slnx` for restore/build only. [Source: 2.2 Project Structure Notes]
- **T1 parallelism race (closed by 2.1):** any new Conformance test that reads/writes `docs/release-evidence/*` must stay inside the existing `ReleaseEvidenceArtifactCollection` `[Collection]`. [Source: epic-1-retro §7 T1]
- **T2 / projectReferenceDisposition:** the `Conformance.Tests → Server` reference is removed only by the **last** owning story of {2.2, 2.5, 3.2, 3.3}. **2.4 is not in that set → leave the reference untouched.** [Source: 2.3 Dev Notes]
- **Admin.Web Playwright E2E lane** needs Chromium — environmental, unrelated; do not chase it. [Source: 2.1/2.2/2.3 Completion Notes]
- **Prove behavior, not mirrors (Epic 1 L1 / A1):** the no-lost-update test must inject a real ETag conflict and assert reload-and-reapply (not just a status/call-count). The fail-closed read tests must assert the safe shapes through the real `ConversationProjectionReadStore`-over-`IReadModelStore` path.

### Project Structure Notes

- Module follows the Hexalith project shape: `Contracts`, `Client`, `Server`, `Admin.Web`, `AppHost`, `ServiceDefaults`, `Testing`, with `tests/Hexalith.Conversations.*.Tests` mirrors. The read-model store + writer live in the **Server** assembly (`src/Hexalith.Conversations.Server/Projections/`); the persisted DTO shapes live in `Contracts/Projections` (public-contract-shape baseline). New tests under `tests/Hexalith.Conversations.Server.Tests/Projections/`.
- Inventory: there is **no FR-5 read-model-store area**. `projection-orchestration` (`Server/Projections/**`, **Promote**, 1,800 LOC) is FR-6/2.5; `projection-field-selection-freshness-shape` (`Contracts/Projections/**`, **Keep**, 1,175 LOC) is the DTOs. This story adds production code under the Server/Projections subtree (the read-store + writer) without relabeling either area — no inventory changeLog entry required (see Task 6 inventory note). [Source: `docs/release-evidence/consume-promote-keep-inventory-v1.json` (projection-orchestration, projection-field-selection-freshness-shape)]

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-2.4] — story statement + ACs + standing gate (note: epics labels "remove-and-replace"; corrected to greenfield-adopt here per verified reality + Story 1.5 escape hatch).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR-Coverage-Map] — FR-5 → Epic 2.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#B] — EventStore.Client `IReadModelStore` (+ETag) + `ReadModelWritePolicy` (reload-merge/optimistic-concurrency/retries) → FR-5.
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.{md,json}] — no FR-5 area; `projection-orchestration` (Promote→2.5) vs `projection-field-selection-freshness-shape` (Keep); frozen `approxLoc`.
- [Source: docs/release-evidence/classification-change-procedure-v1.md] — Story 1.5 escape hatch; append-only `changeLog`; reclassification vs disposition distinction (no inventory relabel needed here).
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs:1-80] — store seam (Get/Save/TrySave, `ReadModelEntry<T>`, ETag).
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs:1-289] — `UpdateAsync`/`ApplyEventsAsync`/`MergeAsync`, retry budget, exhaustion throw, idempotency requirement.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/ReadModelStoreServiceCollectionExtensions.cs:19-23] — `AddEventStoreReadModelStore()`.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/DaprReadModelStore.cs:1-69] — Dapr first-write-wins production impl.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/InMemoryReadModelStore.cs:1-130] — test double + `ConcurrentWriteBeforeTrySave`/`SeedRaw`/`Snapshot`.
- [Source: Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests/Projections/ReadModelWritePolicyTests.cs:24-72] — conflict-then-success reload-and-merge test to mirror (AC-3).
- [Source: Hexalith.Tenants/src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:24-149] — sibling read-model write precedent (keys, `ApplyEventsAsync`/`MergeAsync`, idempotent merge).
- [Source: Hexalith.Tenants/src/Hexalith.Tenants/Program.cs:47,64] — `AddDaprClient()` + `AddEventStoreReadModelStore()` wiring order.
- [Source: src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs:14-37] — read-only abstraction to implement in production.
- [Source: src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs:39-110] — fail-closed read boundary to preserve (AC-4).
- [Source: src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:38,178-321] — consumer + `ListAsync` (no N+1).
- [Source: src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs:86] — `AddConversationQueriesCore` (where to register store + read-store).
- [Source: src/Hexalith.Conversations.Server/Program.cs:22-31] — host; add `AddDaprClient()`/store reg.
- [Source: docs/release-evidence/public-contract-shape-baseline-v1.json] — `ConversationSummaryProjectionV1`/`ConversationDetailProjectionV1` in the 196-type baseline (diff must stay empty).
- [Source: _bmad-output/implementation-artifacts/2-3-adopt-the-sdk-query-handler-cursor-codec-remove-hand-rolled-hmac-cursor.md] — prior story; gate at 353; deferred read-store binding; `StoryNNStructuralDispositions` idiom; submodule-drift CRITICAL; P1/P2 count-drift hazard.

## Developer Context

### Technical Requirements (dev agent guardrails)

- .NET 10 (`net10.0`), SDK pinned `10.0.302` (`global.json`). Nullable enabled, implicit usings, **warnings-as-errors** — do not suppress broadly. File-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix, CRLF, `.ConfigureAwait(false)` on awaits in library code. ITANEO copyright header on every created/edited source file.
- Central Package Management (`Directory.Packages.props`) — never put package versions in `.csproj`; never introduce a new package version. The only new reference is the **project** reference to `Hexalith.EventStore.Testing` in `Server.Tests` (mirror the existing conditional `HexalithEventStoreRoot` reference pattern used for `Hexalith.EventStore.Client`).
- Keep the change scoped to Conversations artifacts + the test/ledger updates this story mandates. **Do not edit** EventStore/Tenants/Parties/FrontComposer sources (the SDK seams already exist — pure consume).
- This is **greenfield-adopt persistence plumbing**: register the store, implement the read adapter + write-via-policy seam, prove concurrency. Resist building the projection handler or wiring replay-to-write (2.5).

### Architecture Compliance

- Let EventStore own read-model persistence integrity and optimistic concurrency — the SDK store + `ReadModelWritePolicy` delegate exactly this; adopting them **strengthens** the EventStore-concept boundary (NFR8). Do not hand-roll a state-store or a read-modify-write loop.
- Fail closed for tenant/projection failures, missing/stale/unavailable state — the read boundary already does (`Forbidden`/`Unavailable`/`PoisonEvent`/`Rebuilding`); preserve every branch. Cross-tenant reads are impossible by construction (tenant baked into the key).
- Do not expose raw EventStore envelopes / `IReadModelStore` as the adopter API — `IConversationProjectionReadStore` stays the Conversations-owned read seam; the public surface is unchanged Conversations query DTOs.
- Keep hot read paths local after authorization; no synchronous cross-service calls; `ListAsync` is a single index read (NFR2). Tenant events are at-least-once/out-of-order — the write transform must be idempotent (NFR5).
- Keep authorization/tenant lookups out of the persistence layer; the read service authorizes **before** any store read (preserve ordering).

### Library / Framework Requirements

- **`Hexalith.EventStore.Client`** (`Hexalith.EventStore.Client.Projections`) — `IReadModelStore`, `ReadModelEntry<T>`, `ReadModelWritePolicy`, `ReadModelWriteContext`, `DaprReadModelStore`; registration `AddEventStoreReadModelStore()` (`…Client.Registration`). Already referenced by Server (transitively via DomainService) and Server.Tests.
- **`Dapr.Client` / Dapr.AspNetCore** — `DaprClient` + `AddDaprClient()` (backs the store). Available transitively via the EventStore packages; register `AddDaprClient()` in `Program.cs` if the shared host does not already provide `DaprClient`.
- **`Hexalith.EventStore.Testing`** (`…Testing.Fakes.InMemoryReadModelStore`) — add the project reference to `Server.Tests` (additive; no duplicate fake introduced).
- Versions via CPM: Dapr `1.17.7`, Aspire `13.2.x`/`13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`.

### File Structure Requirements

- New production code under `src/Hexalith.Conversations.Server/Projections/` (`ConversationProjectionReadStore.cs`, the writer, the tenant-index read-model type if introduced). New tests under `tests/Hexalith.Conversations.Server.Tests/Projections/`. Evidence artifacts under `docs/release-evidence/` are written by generation tests, never hand-edited.

### Testing Requirements

- xUnit v3 + Shouldly + NSubstitute, run per-project. Use the SDK `InMemoryReadModelStore` as the store double (`ConcurrentWriteBeforeTrySave` for the conflict-injection AC-3 test).
- **Prove behavior, not mirrors:** the no-lost-update test injects a real ETag conflict and asserts reload-and-reapply; the retry-exhaustion test asserts the `InvalidOperationException`; the fail-closed read tests assert the safe shapes through the production read-store path.
- Conformance suite must stay **≥ 353 and monotonic**; assertion strength must not drop vs the Story 1.1 baseline. The new tests are additive (no test removed). Public contract-shape diff empty.
- Integration-test rule (EventStore convention): a Tier-2/3 test inspects real end-state, not only a status code or mock call count — applies if you add any request-level integration test.

### Previous-Story Intelligence (2.1 / 2.2 / 2.3 carry-forward)

- **2.1 (host):** two-line shared host; left it ready to discover `IDomainProjectionHandler` (2.5) without re-touching `Program.cs`. Established the evidence-generation-test idiom. Closed the T1 race.
- **2.2 (aggregate base):** deletion-dominant; closed at 352; `StoryNNStructuralDispositions` ledger idiom; **CRITICAL submodule drift** broke the Release build (verify gitlinks); count-drift hazard.
- **2.3 (query/cursor):** closed at **353**; thin `IDomainQueryHandler` adapters; **deferred the production `IConversationProjectionReadStore` binding to THIS story** (faked in dispatch tests) — closing it is AC-1 here. Its review re-hit the count-drift hazard (530→535) — generate the record last.
- **L1 / A1 — coverage ≠ live-path exercise.** Pin behavior by fault injection (the conflict-injection + fail-closed-read tests).
- **A2 / A3 — ledger entry for any structural disposition**; reclassifications go through the `classification-change-procedure-v1` append-only changeLog (not needed here — no area relabels). **Append-only** — never rewrite accepted rows.

### Git Intelligence (recent work patterns)

Recent commits: `feat(story-2.3): Adopt the SDK query-handler + cursor codec…`, `feat(story-2.2): Adopt EventStoreAggregate<TState> base-class conventions`, `feat(story-2.1): Wire Conversations onto the shared two-line domain-service host`. Reuse: the evidence-generation-test idiom for `docs/release-evidence/*` (repo-root discovery → deterministic indented-JSON write → re-read + re-validate + content-safety scan; regenerate, never hand-edit); the `StoryNNStructuralDispositions` ledger section; Conventional Commits scope `feat(story-2.4): …`. This story is the **fourth** `src/` production change and the first **persistence-substrate adoption** in Epic 2.

### Project Context Reference

`_bmad-output/project-context.md` is binding. Most-relevant rules for this story:
- "Use EventStore snapshots/projections for long conversations rather than loading unbounded history; keep hot read/write paths local after authorization." — the read store serves projection reads by key; `ListAsync` is a single index read (NFR2).
- "Dapr pub/sub is at-least-once; all projection/event handlers must tolerate duplicates and replay." — the write transform/merge is idempotent (NFR5).
- "Projection reads must surface stale/rebuilding/unavailable states rather than pretending data is fresh." — preserve the `Unavailable`/`Rebuilding`/freshness branches in the read boundary.
- "Fail closed for authorization, tenant projection failures, unknown/stale state." — cross-tenant impossible by construction (tenant in the key); read boundary fails closed; authorize before any store read.
- "Treat EventStore as a bounded-context dependency; do not reimplement its runtime behavior." — consume `IReadModelStore`/`ReadModelWritePolicy`; do not hand-roll a store or merge loop.
- "Do not expose raw EventStore mechanics as the adopter API." — `IConversationProjectionReadStore` stays the Conversations-owned seam; the public query DTOs are unchanged.
- "Never initialize nested submodules / no `--init --recursive`." — root-level submodule only; verify gitlinks first.

## Open Questions / Notes for the Dev Agent

1. **Read-model index shape (recommended, decide & record):** `ListAsync(tenantId)` needs a single tenant-scoped index read (NFR2, no N+1). The recommended scheme is a per-tenant index read model `projection:conversations-index:{tenantId}` merged via `ReadModelWritePolicy.MergeAsync` (mirror Tenants' singleton index). If you choose a different partitioning, document why and keep the one-read invariant.
2. **DaprClient provisioning:** confirm whether `AddEventStoreDomainService` already registers `DaprClient` (the SDK `DaprStateStoreHealthCheck` resolves it). If yes, `AddEventStoreReadModelStore()` alone suffices; if no, add `AddDaprClient()` in `Program.cs` (mirror Tenants). Record the finding.
3. **Writer wiring boundary:** this story delivers the write-via-policy seam and proves it in isolation; it does **not** wire the materializer/projection handler to call it on replay (that is Story 2.5/FR-6). Confirm the writer is reachable via DI for 2.5 but not invoked on a production hot path yet.
4. **EventStore.Testing reference:** adding it to `Server.Tests` is the one allowed test-side consume here. It introduces no duplicate fake and does not pre-empt Story 2.7 (which removes duplicates). If a build constraint blocks the reference, a minimal local `IReadModelStore` fake with a conflict hook is an acceptable fallback (flag it for 2.7).

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- **Submodule gitlinks verified clean before building** (CRITICAL carry-forward): `Hexalith.EventStore` `ad2c957`, `Hexalith.FrontComposer` `451830b`, `Hexalith.Parties` `485616f`, `Hexalith.Tenants` `5b4424e` — all at recorded commits; no `--init --recursive` used.
- **Greenfield-adopt confirmed (Task 1):** grep for `DaprClient|IStateStoreManager|SaveStateAsync|TrySaveStateAsync|ETag|FirstWrite|optimistic` over `src/Hexalith.Conversations.*` found **no** read-model-persistence state-store / merge / ETag / optimistic-concurrency code — the work is additive adoption, not remove-and-replace.
- **DaprClient provisioning (Open Question 2):** confirmed `AddEventStoreDomainService` does **not** register a `DaprClient` (no `AddDaprClient` in `EventStoreDomainServiceExtensions` / `AddServiceDefaults` / `AddEventStore`). Added `builder.Services.AddDaprClient()` to `Program.cs` (mirrors the Tenants host).
- **State-store component name:** `"statestore"` (sibling Tenants convention), recorded in `ConversationProjectionReadModelKeys`.
- **`ServerBoundaryTest` re-expression (recorded design decision):** calling `AddDaprClient()` (from `Dapr.AspNetCore`) introduces a `Dapr.Client` **assembly-metadata** reference because the extension's signature names a `Dapr.Client` type — empirically confirmed by running the guard. The assembly-level *absence* clause was re-expressed to a *presence* assertion; the **direct** `Dapr.Client` package/project guard (csproj-level, the second test method) stays fully intact, so Conversations still takes no direct package dependency. Recorded append-only as `story24StructuralDispositions` (FR-20 ledger) + the companion `.md`, mirroring the Story 2.1 re-expression of this same guard.
- **Admin.Web Playwright E2E lane (2 failures):** `PlaywrightFixture` → `BrowserType.LaunchAsync` (Chromium not installed) — the documented environmental carry-forward, unrelated to this story; the other 12 Admin.Web tests pass.

### Completion Notes List

Delivered the FR-5 persistence **substrate** (greenfield-adopt; the materializer-to-writer replay wiring stays a Story 2.5 concern):

- **AC-1 — store registered + production read-store binding closed.** `AddConversationQueriesCore` now calls `AddEventStoreReadModelStore()` and binds `IConversationProjectionReadStore` → new production `ConversationProjectionReadStore` (+ registers `ConversationProjectionReadModelWriter`), all `TryAdd*` so test compositions can override. `Program.cs` adds `AddDaprClient()`. The new host-composition fact (`ProductionHostShouldResolveReadStoreBindingAndConsumerGraph`) proves `IReadModelStore` → `DaprReadModelStore`, `IConversationProjectionReadStore` → `ConversationProjectionReadStore`, and the query/governance consumer graph builds with no fake — closing the binding deferred from Story 2.3.
- **AC-2 — writes go through `ReadModelWritePolicy`.** `ConversationProjectionReadModelWriter.PersistAsync` writes the per-conversation pair via `ReadModelWritePolicy.UpdateAsync` (idempotent full-replace transform) and merges the summary into a per-tenant index via `ReadModelWritePolicy.MergeAsync` (idempotent dedup by conversation identity, newest generation wins, returns a new instance). No direct `IReadModelStore.SaveAsync` and no hand-rolled read-modify-write loop on the write path; grep-clean of bespoke state-store/ETag code under `src/Hexalith.Conversations.*`.
- **AC-3 — no lost update + fail-loud.** `ConcurrentIndexWriteIsReloadedAndReapplied` injects exactly one ETag conflict on the index merge via `InMemoryReadModelStore.ConcurrentWriteBeforeTrySave` and asserts the final index reflects **both** writers' conversations (reload-and-reapply, not a blind overwrite). `ExhaustedIndexRetriesFailLoud` keeps the conflict firing and asserts the policy throws `InvalidOperationException`.
- **AC-4 — read path / freshness / contracts unchanged.** `ConversationProjectionReadStore.ReadAsync` is a single keyed `GetAsync`; `ListAsync` is a single tenant-index `GetAsync` (proven no-N+1 by a counting store decorator). The fail-closed shapes (missing → Forbidden, throw → Unavailable, identity mismatch → PoisonEvent, mixed generation → Rebuilding, current → trust-bearing) are re-expressed through the production read store over `IReadModelStore` without weakening; the existing fake-backed read tests stay green unchanged. `Contracts/Projections` DTOs are untouched and the public contract-shape baseline diff is empty.
- **AC-5 — disposition logged, gate holds.** `story24StructuralDispositions` recorded in the FR-20 register (regenerated JSON; never hand-edited) + the companion `.md`: greenfield-adopt correction, the `ServerBoundaryTest` re-expression, and the Contracts DTOs confirmed Keep. No inventory area relabeled → no inventory `changeLog` entry (no FR-5 area; `projection-orchestration` stays Story 2.5).

**Scope discipline honored:** no `IDomainProjectionHandler`, no change to `ConversationProjectionMaterializer` orchestration, no `Contracts/Projections` reshape, no host wiring beyond the store + Dapr registration.

**Validation (per-project, Release, 0 warnings):** Conformance **354** (≥ 353, monotonic +1 — the new Story24 ledger fact); Server.Tests **548**; Contracts.Tests **587**; Client.Tests **25**; Domain Tests **185**; IntegrationTests **8**; Admin.Web.Tests **12 pass / 2 env-only Playwright failures**. Public-contract-shape baseline JSON **byte-unchanged** (empty diff). No `src/` public contract change.

### Senior Developer Review (AI)

**Reviewer:** Jerome Piquot (automated adversarial review) — 2026-06-03
**Outcome:** Approved (auto-fix applied). Status → done.

Validated every AC and every `[x]` task against the working tree at `dfecb71`, rebuilt Release (0 warnings), and re-ran the changed suites. Findings:

- **AC-1..AC-5 — IMPLEMENTED (verified, not just claimed).** `AddEventStoreReadModelStore()` + `AddDaprClient()` wired; production `IConversationProjectionReadStore` → `ConversationProjectionReadStore` and `ConversationProjectionReadModelWriter` bound via `TryAdd*`; the host-composition test resolves `IReadModelStore` → `DaprReadModelStore`, the read-store binding, and the query/governance consumer graph. Writes go through `ReadModelWritePolicy.UpdateAsync`/`MergeAsync`; **grep-clean** confirmed — no `IReadModelStore.SaveAsync`/`DaprClient`/ETag/merge-loop on the write path under `src/Hexalith.Conversations.*`. The AC-3 concurrency tests inject a *real* ETag conflict through `InMemoryReadModelStore.ConcurrentWriteBeforeTrySave` (verified against the SDK fake's semantics — the hook fires before the ETag compare) and assert reload-and-reapply / fail-loud exhaustion — behavior, not mirrors. Fail-closed read shapes (`Forbidden`/`Unavailable`/`PoisonEvent`/`Rebuilding`/`Current`) re-expressed through the real store path and confirmed against the read service's actual branches. `ListAsync` proven single-read (no N+1) via a counting decorator.
- **Public-contract-shape diff empty — VERIFIED LEGITIMATE.** The new `public` Server types do not affect the baseline because the snapshot scans **only** the `Hexalith.Conversations.Contracts` assembly; `public-contract-shape-baseline-v1.json` is byte-unchanged. Conformance **354** green (≥ 353 monotonic). FR-20 ledger `story24StructuralDispositions` regenerated (JSON not hand-edited), `.md` companion updated.

**MEDIUM-1 (fixed) — Dev Agent Record count drift.** Completion Notes claimed `Server.Tests 545`; the actual per-project run is **548** (recurrence of the explicitly-tracked P1/P2 / 2.3-MEDIUM-1 count-drift hazard). Corrected to 548.

**LOW-1 (noted, not changed) — wider-than-needed visibility.** `ConversationProjectionReadStore`, `ConversationProjectionReadModelWriter`, and `ConversationProjectionIndexReadModel` are `public` even though the Server assembly grants `InternalsVisibleTo` to `Server.Tests` and the sibling `ConversationProjectionReadModelKeys` is `internal`; they could be `internal` to keep the Server surface minimal (NFR8 spirit). Defensible as-is for DI; left for the dev's discretion / 2.7 hygiene.

**LOW-2 (noted) — per-conversation write has no generation guard.** `UpdateAsync(_ => models)` is correctly idempotent for the same materialization (AC-2/NFR5) and the tenant index merge does enforce newest-generation-wins, but the per-conversation key itself is a full-replace with no generation comparison. Acceptable for the persistence **substrate** — Story 2.5 (FR-6) owns the replay-ordering that drives the writer — flagged for 2.5 awareness.

### Change Log

- 2026-06-03 — Senior Developer Review (AI): approved; corrected Dev Agent Record `Server.Tests` count 545 → 548 (verified 548 green); Status review → done. No CRITICAL/HIGH findings; two LOW observations recorded for 2.5/2.7.
- 2026-06-03 — Adopted the shared persisted read-model store + write policy (FR-5): registered `AddEventStoreReadModelStore()` + `AddDaprClient()`, added production `ConversationProjectionReadStore` (read) + `ConversationProjectionReadModelWriter` (write via `ReadModelWritePolicy`) + per-tenant `ConversationProjectionIndexReadModel`, closing the deferred-from-2.3 `IConversationProjectionReadStore` binding. Added persistence/concurrency/idempotency/fail-closed tests + the AC-1 host-composition test. Re-expressed the `ServerBoundaryTest` `Dapr.Client` assembly-metadata clause and recorded `story24StructuralDispositions` in the FR-20 ledger.

### File List

**Production (new):**
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexReadModel.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs`

**Production (modified):**
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs` — register the SDK store + production read-store + writer in `AddConversationQueriesCore`
- `src/Hexalith.Conversations.Server/Program.cs` — `AddDaprClient()`

**Tests (new):**
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadStoreFailClosedTest.cs`

**Tests (modified):**
- `tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` — additive `Hexalith.EventStore.Testing` project reference
- `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainDiscoveryHostCompositionTest.cs` — AC-1 production host-composition fact
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs` — re-expressed `Dapr.Client` assembly-metadata clause
- `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` — `Story24StructuralDispositions` record + builder + anchored-and-green fact

**Evidence (regenerated / updated; JSON never hand-edited):**
- `docs/release-evidence/at-risk-test-register-v1.json` — regenerated with `story24StructuralDispositions`
- `docs/release-evidence/at-risk-test-register-v1.md` — Story 2.4 structural-dispositions section
