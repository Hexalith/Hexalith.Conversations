# Addendum — Conversations Boilerplate Reduction

This addendum contains technical-how and grounding evidence that support the PRD but belong downstream (architecture / solution design). The PRD stays at capability altitude; this file carries the library mappings, the gap catalog, and the cross-module duplication evidence gathered during Discovery (3 Explore subagents, 2026-06-02). Figures are first-pass and approximate — confirm during architecture.

## A. Current baseline and implementation guardrail

**Authoritative SM-1 baseline:** Story 1.4 measured and accepted **13,289 LOC (37.15%)** on 2026-06-03 in the canonical, FR-2-governed repo-root `docs/release-evidence/consume-promote-keep-inventory-v1.json`. Its `sourceTotalLoc` verifies exactly 35,769 LOC. Under OQ-3, governance and hydration were classified as Keep now. The Contracts/Testing domain surface was classified Keep at the same Story 1.4 acceptance (moving ≈4.7k LOC out of the Discovery plumbing estimate); the authority for that call is the accepted inventory itself, whose split rows (`query-filters-response-shapes`, `domain-contract-types`, `conformance-contract-types`) record the per-row source-boundary rationale. Any post-acceptance reclassification appends to the inventory changeLog per repo-root `docs/release-evidence/classification-change-procedure-v1.json` — never silently (FR-2, §2 denominator rule). The inventory's acceptance record carries `status: accepted, acceptedDate: 2026-06-03` but no named acceptor; backfilling the named acceptance into the inventory changeLog is an open follow-up owned by the release owner. This inventory is the baseline Story 5.3 references.

**Historical Discovery estimate:** Total source ≈ 35,769 LOC; plumbing (Consume + Promote) ≈ 18,000 LOC (~50%); domain logic (Keep) ≈ 17,000 LOC. This first-pass estimate is preserved as provenance, not as the accepted baseline.

**Implementation guardrail:** Hosting, AppHost, Aspire, DAPR, ServiceDefaults, runtime projections/queries, telemetry scaffolding, and event subscriptions must land in and remain owned by the platform/domain-service SDK, never the Conversations domain module.

## B. Architecture and release decision register

### Open architecture decisions (OQ-1)

- Landing zone per promotion: existing module (Commons vs EventStore.*) vs a new dedicated shared abstractions module.
- Additive/backward-compatible API design so Folders/Projects/Memories/Parties/Tenants keep compiling.
- Whether governance/temporal/hydration orchestration (areas 2, 3, and 7) generalizes cleanly enough to be promoted in a follow-on phase.

### Legacy technical-how provenance

**Provenance:** May 2026 legacy root feature PRD, carried through `reconcile-legacy-root-prd.md` on 2026-07-14. These questions are retained here because they concern protocol, mechanism, platform wiring, or technical release fallback. They do not expand refactor scope, and legacy defaults are not current approvals.

### Open legacy technical-how questions

| ID | Legacy technical-how question | Current disposition |
|---|---|---|
| Legacy-TQ1 | Is the supported transport HTTP only or HTTP plus gRPC? | **Open.** Requires an explicit contract/architecture decision; the preserved product baseline remains transport-neutral. |
| Legacy-TQ2 | Is the idempotency key consumer-supplied or service-derived? | **Open.** The mechanism is undecided; `Feature-FR6`, `Feature-FR88`, and `Feature-NFR22` preserve stable externally observable idempotent behavior. |
| Legacy-TQ3 | What exact status and retry semantics apply to stale tenant projections? | **Open.** Mapping remains an architecture/API decision; fail-closed behavior and typed, sanitized errors remain mandatory. |
| Legacy-TQ4 | What pub/sub topic naming is used, and is the EventStore convention sufficient? | **Open.** The platform/domain-service SDK owns topic conventions and subscription plumbing; Conversations must not introduce module-owned runtime naming machinery. |
| Legacy-TQ5 | Is audit-pairing health exposed through pull or push semantics? | **Open.** The platform operational contract and architecture must decide; governance mutations still fail closed when audit recording is unavailable. |

### Open release exception

| ID | Legacy technical-how question | Current disposition |
|---|---|---|
| Legacy-TQ6 | May a release use raw HTTP if the supported .NET client misses GA? | **Open release exception.** `Feature-FR71` permits this only through explicit buyer acceptance; no exception is inferred. |

### Resolved for this refactor

| ID | Legacy technical-how question | Current disposition |
|---|---|---|
| Legacy-TQ7 | Is the EventStore envelope inherited as stable or changed by this initiative? | **Resolved for this refactor: inherited and unchanged.** Envelope redesign is out of scope, public clients must not leak EventStore mechanics, and compatibility remains gated by FR-20/SM-C1. |

## C. Conversations boilerplate inventory (first pass)

The table preserves the first-pass area estimates and classifications as **superseded provenance**: the sole FR-1 inventory object is the accepted repo-root `docs/release-evidence/consume-promote-keep-inventory-v1.json` (§A), which resolves every mixed or dual first-pass label below into exactly-one-classification split rows. In particular, row 8's "Promote (partial)" is resolved there as `publication-transport-marshaling` (422 LOC, **Promote, FR-13**, Story 3.5) plus `publication-failure-taxonomy` (131 LOC, Keep), and row 11's "Mixed" resolves into paired Consume/Keep rows. Rows below marked with dual labels do not satisfy FR-1/FR-2's exactly-one-classification consequence and must not be read as the accepted inventory.

| # | Area | ~LOC / files | Class | Target capability |
|---|------|--------------|-------|-------------------|
| 1 | Queries / cursor / read-model hydration boundary | 5,327 / 14 | Consume + Promote | SDK `IDomainQueryHandler`, `IQueryCursorCodec`, `QueryCursorScope`; keep query filters/response shapes |
| 2 | Governance / verification / audit | 4,337 / 10 | Keep now, promote-later candidate | generic check→evidence→verify→result flow could be promoted; domain evidence/remediation vocabulary remains domain-owned |
| 3 | Projections (materializer, rebuild, state) | 2,975 / 12 | Promote orchestration, Keep logic | SDK `IDomainProjectionHandler`; keep field selection / freshness formula |
| 4 | Diagnostics / telemetry / classifiers | 2,442 / 24 | Promote | shared meter/counter/classifier scaffolding (FR-15) |
| 5 | Validation logic | 2,663 / 13 | Keep | conversation business rules |
| 6 | Tenant-access projection + DI | 1,086 / 9 | Promote | generic `TenantAccessProjectionHandler<TEvent,TProjection>` + registration (FR-11) |
| 7 | Hydration (reference resolution) | 828 / 8 | Keep now, promote-later candidate | cross-domain reference binding pattern |
| 8 | Publication / event composition | 638 / 8 | Promote (partial) | transport marshaling is generic; the failure taxonomy remains domain-specific |
| 9 | DI / ServiceCollection extensions | 363 / 9 | Promote / Consume | shared host (FR-3) + shared registration helpers |
| 10 | Serialization converters | 174 / 6 | Consume | Commons `TypeMapper` / generic converters / source-gen context base (FR-8, FR-14) |
| 11 | Test scaffolding / fixtures | 1,755 / 11 | Mixed | consume EventStore.Testing assertions/fakes; keep domain conformance scenarios |
| 12 | Aggregate scaffolding | — | Consume | `EventStoreAggregate<TState>` reflection dispatch (FR-7) |

**Top hotspots by volume:** Queries/cursor (5.3k) → Governance (4.3k) → Projections (3.0k) → Diagnostics (2.4k) → Validation (2.7k, Keep).

## D. Existing technical-module surface to CONSUME (FR-3..FR-9)

Concrete implementation mappings remain here rather than in the normative PRD. FR-10 and FR-13 also consume this platform surface; §F identifies any platform-owned extension still required.

| Technical module | Existing surface | Conversations use / ownership constraint | FR |
|---|---|---|---|
| EventStore.DomainService | `AddEventStoreDomainService([options][,assemblies])` + `UseEventStoreDomainService()` | The platform-owned host uses this two-line integration to scan the domain assembly, register `IDomainProcessor`/`IDomainQueryHandler`/`IDomainProjectionHandler`, and map canonical endpoints. Conversations does not own the host. | FR-3 |
| EventStore.Client | `EventStoreAggregate<TState>` | Reflection command dispatch (`Handle(TCommand,TState?)`) + replay (`Apply(TEvent)`); `OnConfiguring` hook. | FR-7 |
| EventStore.DomainService + Client | `IDomainQueryHandler` (DomainService); `IQueryCursorCodec`, `QueryCursorScope` (Client) | Replace the local query orchestrator and HMAC cursor implementation while preserving accepted/rejected token behavior and page ordering. | FR-4 |
| EventStore.DomainService | `IDomainProjectionHandler` | Stateless full-replay. | FR-6 |
| EventStore.Client | `IReadModelStore` (+ ETag) and `ReadModelWritePolicy` | Reload-merge, optimistic concurrency, retries. | FR-5 |
| EventStore.Client | `IEventStoreGatewayClient` + `AddEventStoreGatewayClient()` | Existing gateway client and registration surface. | — |
| EventStore.Client | `AddEventStore([options][,assemblies])` | Existing discovery surface. | — |
| EventStore.ServiceDefaults | `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, `MapDefaultEndpoints`; EventStore.DomainService `AddEventStoreDomainTelemetry` and convention-owned diagnostics registration | Consume existing platform-host capabilities. Conversations may provide domain instrumentation metadata but owns no ServiceDefaults project. | FR-9/FR-10/FR-15 |
| EventStore.Aspire | `AddHexalithEventStore`, `AddEventStoreDomainModule` | Consume existing platform AppHost capabilities, including shared versus isolated DAPR infrastructure modes and platform-owned sidecar health behavior. Conversations owns no AppHost or Aspire project. | FR-13 |
| EventStore.Testing | `DomainResultAssertions`, envelope/sequence/isolation assertions, `FakeEventStoreGatewayClient`, `InMemoryStateManager`, terminatable compliance | Consume the existing test surface. | FR-9 |
| Commons | `TypeMapper`/`NameTypeMapper` (under-used polymorphic registry), `FluentValidateOptions<T>`, `IEquatableObject`/`EquatableHelper`, `UniqueIdHelper`/Ulid, `ISettings`/`SettingsHelper` | Consume the existing common helpers. | FR-8/FR-14 |
| FrontComposer | `FrontComposerGenerator` (source-gen for `[Command]`/`[Projection]`), `FrontComposerTestBase`/host builder | Preserve generated behavior. | Feature-FR76 |

## E. Cross-module duplication → shared-capability candidates (FR-10..FR-15)

Modules compared: Conversations, Folders, Projects, Memories, Tenants, Parties.

`Hexalith.Tenants` appears here only as a **domain module included in the comparison and as a dependency/consumer**. It is not a technical-module landing zone; generic hosting/runtime behavior belongs in EventStore, Commons, FrontComposer, or another genuine shared technical module.

| Rank | Pattern | Where | Similarity | Recommendation | FR |
|------|---------|-------|-----------|----------------|----|
| 1 | Legacy per-module ServiceDefaults extensions | Folders/Memories/Tenants/Parties `*.ServiceDefaults/Extensions.cs` | near-identical (name swap; Memories adds Redis, Parties adds Dapr health) | consume existing EventStore ServiceDefaults/domain-telemetry surface; extend only in the platform when a required generic hook is absent | FR-10 |
| 2 | Tenant-access projection handler (~80 LOC) | Folders, Projects `Projections/TenantAccess/*Handler.cs` | structurally identical | generic `<TEvent,TProjection>` | FR-11 |
| 3 | Tenant-access DI (`AddXxxTenantAccess`) | Folders, Projects `*ServiceCollectionExtensions.cs` | identical pattern | promote-as-is (generic factory) | FR-11 |
| 4 | Client typed-HttpClient registration | Folders, Projects `*.Client/*ClientServiceCollectionExtensions.cs` | identical, domain-agnostic | promote-as-is | FR-12 |
| 5 | Legacy per-module Aspire/Dapr topology | Folders, Projects `*.Aspire/*AspireModule.cs` | structurally similar | consume existing EventStore AppHost/domain-module capability; extend only in EventStore.Aspire for unsupported generic topology behavior | FR-13 |
| 6 | JsonContext setup | Memories contexts (`[JsonSerializable]` lists + resolver combine) | identical pattern | source-gen context base | FR-14 |
| 7 | Domain-processor registration (`TryAddEnumerable`) | Folders, Projects | identical | `AddDomainProcessor<T>()` helper | FR-3/FR-10 |
| 8 | HealthCheck / client registration tests | Folders/Memories/EventStore | same shape | shared test fixtures (candidate landing zone only — e.g. a Commons testing package or EventStore.Testing; the final zone is an OQ-1 architecture decision) | FR-9 |
| — | Program.cs wiring | historical module hosts | thin, minor variance | keep only in the platform host; domain modules supply SDK registration metadata | — |

**Proven local standard to emulate:** EventStore's generic `AddEventStore<TAggregate>()` template-method extension — adopt this style for tenant-access, client, and health-check registration.

## F. Gap catalog and current disposition

Build only capabilities Conversations consumes in-pilot; all others remain follow-on backlog.

| # | Capability or gap | Current disposition | FR |
|---|---|---|---|
| 1 | `ICommandContract` / `IEventContract` compile-time metadata, parallel to existing `IQueryContract` | **Backlog.** Explicitly deferred from the pilot on 2026-07-14 because contract reshaping is unnecessary for the core boilerplate-reduction proof. | FR-16 |
| 2 | Polymorphic JSON registration helper / source-gen catalog | Publicize `TypeMapper` for in-pilot consumption. | FR-14 |
| 3 | Generic tenant-access projection handler | Build for in-pilot consumption. | FR-11 |
| 4 | Generic observability/health hook | **Consume/extend.** `EventStore.ServiceDefaults` already supplies `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, and `MapDefaultEndpoints`; `EventStore.DomainService` supplies `AddEventStoreDomainTelemetry`. Consume these. If Conversations requires a generic hook that the platform-owned surface does not yet support, extend that surface; do not create a Conversations ServiceDefaults or hosting module. | FR-10 |
| 5 | Generic typed-HttpClient registration | Build for in-pilot consumption. | FR-12 |
| 6 | Generic naming, mode, component, or sidecar behavior for Aspire/DAPR topology | **Consume/extend.** `EventStore.Aspire` already supplies `AddHexalithEventStore` and `AddEventStoreDomainModule` for platform-owned shared/isolated DAPR topology. Consume these. If required generic behavior is unsupported, extend `EventStore.Aspire`; do not create a Conversations AppHost/Aspire/hosting module. | FR-13 |
| 7 | Tier-3 integration test harness (command→event→projection→query) | **Backlog.** | — |
| 8 | Snapshot/event-upcasting hook on `EventStoreAggregate<TState>` | **Backlog.** | — |
| 9 | Command-level authorization/validator discovery convention | **Backlog.** | — |
| 10 | Deadletter/poison-pill domain hook | **Backlog.** | — |
