# Addendum — Conversations Boilerplate Reduction

Technical-how and grounding evidence that supports the PRD but belongs downstream (architecture / solution design). The PRD stays at capability altitude; this file carries the library mappings, the gap catalog, and the cross-module duplication evidence gathered during Discovery (3 Explore subagents, 2026-06-02). Figures are first-pass and approximate — confirm during architecture.

## A. Conversations boilerplate inventory (first pass)

Total source ≈ 35,769 LOC; estimated plumbing (Consume + Promote) ≈ 18,000 LOC (~50%); domain logic (Keep) ≈ 17,000 LOC.

> **Confirmed downstream (Story 1.4, 2026-06-03):** this first-pass plumbing estimate was measured and accepted as **13,289 LOC (37.15%)** in the canonical, FR-2-governed `docs/release-evidence/consume-promote-keep-inventory-v1.json` (`sourceTotalLoc` 35,769 verified exactly; governance + hydration resolved Keep-now per OQ-3 and Contracts/Testing domain surface attributed Keep moved ≈4.7k LOC out of plumbing). The inventory — not this estimate — is the SM-1 baseline Story 5.3 references. This first-pass figure is preserved as the historical estimate; see the inventory for the accepted value.

| # | Area | ~LOC / files | Class | Target capability |
|---|------|--------------|-------|-------------------|
| 1 | Queries / cursor / read-model hydration boundary | 5,327 / 14 | Consume + Promote | SDK `IDomainQueryHandler`, `IQueryCursorCodec`, `QueryCursorScope`; keep query filters/response shapes |
| 2 | Governance / verification / audit | 4,337 / 10 | Keep now, promote-later candidate | generic check→evidence→verify→result flow could promote; domain evidence/remediation vocab stays |
| 3 | Projections (materializer, rebuild, state) | 2,975 / 12 | Promote orchestration, Keep logic | SDK `IDomainProjectionHandler`; keep field selection / freshness formula |
| 4 | Diagnostics / telemetry / classifiers | 2,442 / 24 | Promote | shared meter/counter/classifier scaffolding (FR-15) |
| 5 | Validation logic | 2,663 / 13 | Keep | conversation business rules |
| 6 | Tenant-access projection + DI | 1,086 / 9 | Promote | generic `TenantAccessProjectionHandler<TEvent,TProjection>` + registration (FR-11) |
| 7 | Hydration (reference resolution) | 828 / 8 | Keep now, promote-later candidate | cross-domain reference binding pattern |
| 8 | Publication / event composition | 638 / 8 | Promote (partial) | transport marshaling generic; failure taxonomy domain |
| 9 | DI / ServiceCollection extensions | 363 / 9 | Promote / Consume | shared host (FR-3) + shared registration helpers |
| 10 | Serialization converters | 174 / 6 | Consume | Commons `TypeMapper` / generic converters / source-gen context base (FR-8, FR-14) |
| 11 | Test scaffolding / fixtures | 1,755 / 11 | Mixed | consume EventStore.Testing assertions/fakes; keep domain conformance scenarios |
| 12 | Aggregate scaffolding | — | Consume | `EventStoreAggregate<TState>` reflection dispatch (FR-7) |

**Top hotspots by volume:** Queries/cursor (5.3k) → Governance (4.3k) → Projections (3.0k) → Diagnostics (2.4k) → Validation (2.7k, Keep).

## B. Existing technical-module surface to CONSUME (FR-3..FR-9)

- **EventStore.DomainService** — `AddEventStoreDomainService([options][,assemblies])` + `UseEventStoreDomainService()`: two-line host; scans assembly, registers `IDomainProcessor`/`IDomainQueryHandler`/`IDomainProjectionHandler`, maps canonical endpoints. → FR-3.
- **EventStore.Client** —
  - `EventStoreAggregate<TState>`: reflection command dispatch (`Handle(TCommand,TState?)`) + replay (`Apply(TEvent)`); `OnConfiguring` hook. → FR-7.
  - `IDomainQueryHandler`, `IQueryCursorCodec`, `QueryCursorScope`. → FR-4.
  - `IDomainProjectionHandler` (stateless full-replay). → FR-6.
  - `IReadModelStore` (+ ETag) and `ReadModelWritePolicy` (reload-merge, optimistic concurrency, retries). → FR-5.
  - `IEventStoreGatewayClient` + `AddEventStoreGatewayClient()`.
  - `AddEventStore([options][,assemblies])` discovery.
- **EventStore.ServiceDefaults** — `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, `MapDefaultEndpoints`. → FR-9/FR-10.
- **EventStore.Aspire** — `AddHexalithEventStore`, `AddEventStoreDomainModule` (shared vs isolated Dapr). → FR-13.
- **EventStore.Testing** — `DomainResultAssertions`, envelope/sequence/isolation assertions, `FakeEventStoreGatewayClient`, `InMemoryStateManager`, terminatable compliance. → FR-9.
- **Commons** — `TypeMapper`/`NameTypeMapper` (under-used polymorphic registry), `FluentValidateOptions<T>`, `IEquatableObject`/`EquatableHelper`, `UniqueIdHelper`/Ulid, `ISettings`/`SettingsHelper`. → FR-8/FR-14.
- **FrontComposer** — `FrontComposerGenerator` (source-gen for `[Command]`/`[Projection]`), `FrontComposerTestBase`/host builder. (Preserve generated behavior, FR in feature PRD.)

## C. Cross-module duplication → PROMOTE candidates (FR-10..FR-15)

Modules compared: Conversations, Folders, Projects, Memories, Tenants, Parties.

| Rank | Pattern | Where | Similarity | Recommendation | FR |
|------|---------|-------|-----------|----------------|----|
| 1 | ServiceDefaults extensions | Folders/Memories/Tenants/Parties `*.ServiceDefaults/Extensions.cs` | near-identical (name swap; Memories adds Redis, Parties adds Dapr health) | promote-with-generalization (hooks) | FR-10 |
| 2 | Tenant-access projection handler (~80 LOC) | Folders, Projects `Projections/TenantAccess/*Handler.cs` | structurally identical | generic `<TEvent,TProjection>` | FR-11 |
| 3 | Tenant-access DI (`AddXxxTenantAccess`) | Folders, Projects `*ServiceCollectionExtensions.cs` | identical pattern | promote-as-is (generic factory) | FR-11 |
| 4 | Client typed-HttpClient registration | Folders, Projects `*.Client/*ClientServiceCollectionExtensions.cs` | identical, domain-agnostic | promote-as-is | FR-12 |
| 5 | Aspire/Dapr module topology | Folders, Projects `*.Aspire/*AspireModule.cs` | structurally similar | base + pluggable names/mode | FR-13 |
| 6 | JsonContext setup | Memories contexts (`[JsonSerializable]` lists + resolver combine) | identical pattern | source-gen context base | FR-14 |
| 7 | Domain-processor registration (`TryAddEnumerable`) | Folders, Projects | identical | `AddDomainProcessor<T>()` helper | FR-3/FR-10 |
| 8 | HealthCheck / client registration tests | Folders/Memories/EventStore | same shape | shared test fixtures (Commons.Testing) | FR-9 |
| — | Program.cs wiring | all | thin, minor variance | leave-per-module | — |

**Proven local standard to emulate:** EventStore's generic `AddEventStore<TAggregate>()` template-method extension — adopt this style for tenant-access, client, and health-check registration.

## D. Confirmed capability GAPS (no helper exists yet)

Build only those Conversations consumes in-pilot; rest = follow-on backlog.
1. `ICommandContract` / `IEventContract` compile-time metadata (parallel to existing `IQueryContract`) — FR-16 (conditional).
2. Polymorphic JSON registration helper / source-gen catalog (publicize `TypeMapper`) — FR-14.
3. Generic tenant-access projection handler — FR-11.
4. Generic ServiceDefaults base with hooks — FR-10.
5. Generic typed-HttpClient registration — FR-12.
6. Generic Aspire/Dapr module hosting base — FR-13.
7. Tier-3 integration test harness (command→event→projection→query) — backlog.
8. Snapshot/event-upcasting hook on `EventStoreAggregate<TState>` — backlog.
9. Command-level authorization/validator discovery convention — backlog.
10. Deadletter/poison-pill domain hook — backlog.

## E. Architecture decisions deferred (OQ-1)

- Landing zone per promotion: existing module (Commons vs EventStore.*) vs a new dedicated shared abstractions module.
- Additive/backward-compatible API design so Folders/Projects/Memories/Parties/Tenants keep compiling.
- Whether governance/temporal/hydration orchestration (areas 2,3,7) generalize cleanly enough to promote in a follow-on.
