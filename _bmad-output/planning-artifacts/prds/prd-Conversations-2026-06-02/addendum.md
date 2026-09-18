# Addendum — Conversations Boilerplate Reduction

<!-- superseded-inventory-table: true; authoritative-inventory: docs/release-evidence/consume-promote-keep-inventory-v1.json; sha256: 20bbedb5d0aa1dcd35729aac6a8500f7cf75d8f0c0719b8e1364da5e19fa22b7 -->

This addendum contains technical-how details and grounding evidence that support the PRD but belong in downstream architecture and solution-design documents. It records library mappings, the gap catalog, and historical cross-module duplication evidence gathered during Discovery (3 Explore subagents, 2026-06-02). The §C table is superseded provenance; do not use it as active architecture or inventory input. The marker above records the exact authoritative inventory path and hash. Other first-pass figures are approximate unless a versioned artifact is cited.

## A. Current baseline and implementation guardrail

**Implementation guardrail:** Hosting, AppHost, Aspire, DAPR, ServiceDefaults, runtime projections/queries, telemetry scaffolding, and event subscriptions must reside in and remain owned by the platform/domain-service SDK, never the Conversations domain module.

**Frozen SM-1 baseline pending authority completion:** Story 1.4 measured **13,289 LOC (37.15%)** on 2026-06-03 in the canonical, FR-2-governed repo-root `docs/release-evidence/consume-promote-keep-inventory-v1.json` (SHA-256 `20bbedb5d0aa1dcd35729aac6a8500f7cf75d8f0c0719b8e1364da5e19fa22b7`). Its `sourceTotalLoc` value verifies exactly 35,769 LOC. Under OQ-3, governance and hydration were classified as Keep now. The Contracts/Testing domain surface was classified Keep in the same artifact (moving ≈4.7k LOC out of the Discovery plumbing estimate); its split rows (`query-filters-response-shapes`, `domain-contract-types`, `conformance-contract-types`) record the per-row source-boundary rationale. Any reclassification appends to the inventory changeLog per repo-root `docs/release-evidence/classification-change-procedure-v1.json` — never silently (FR-2, §2 denominator rule). The inventory records `status: accepted` and `acceptedDate: 2026-06-03` but does not name an acceptor, so it freezes the denominator but does not establish SM-1 acceptance until an actual authority record ratifies this exact hash. No approver or owner is inferred here.

**Historical Discovery estimate:** Total source ≈ 35,769 LOC; plumbing (Consume + Promote) ≈ 18,000 LOC (~50%); domain logic (Keep) ≈ 17,000 LOC. This first-pass estimate is preserved as provenance, not as the accepted baseline.

## B. Architecture and release decision register

### OQ-1 recorded authority and remaining evidence gaps

The Initiative Landing-Zone Register in the repo-root `_bmad-output/planning-artifacts/architecture.md` records the technical mapping: FR-10 and FR-15 use EventStore ServiceDefaults/DomainService plus Commons Diagnostics; FR-11 uses Commons TenantAccess; FR-12 uses Commons Http; FR-13 uses EventStore Aspire plus applicable Commons helpers; and FR-14 uses Commons Serialization. The register also marks FR-16 as deferred and non-activated.

`OQ1-OWNER-AUTHORITY-001` records Jérôme Piquot's self-attested Owner scope over EventStore, Commons, and Conversations for FR-10 through FR-15. The hash-bound current architecture mapping supersedes the historical promote/adopt runbook account for OQ-1. `OQ1-EVENTSTORE-GRANT-001`, `OQ1-COMMONS-GRANT-001`, and `OQ1-CONVERSATIONS-CONSUMER-GRANT-001` bind the recorded source snapshots and bounded `OQ1-SOURCE-BUNDLE-001` vehicle. Root commit `d956c9b1de73bcf15969d5e1a6435d6d98a2dd49` commits the runtime-boundary and scoped IdentityModel compatibility remediation plus the full intended evidence set. A fresh clean-HEAD Release solution build passes with 0 warnings and 0 errors, but broad Conformance remains failed at 456/472. The retained `APPHOST-RUNTIME-BOUNDARY-WORKTREE-002` raw runs were executed against EventStore `27cc17f37774dde958a4d5bf9ba1e9b8d04ce07e`, while `d956c9b1…` binds EventStore `629168e3983e5a9cd1639013f39d758fb0068cac`; those runs therefore remain historical narrow evidence rather than current-gitlink runtime proof. Package publication is unclaimed and production rollback remains conditional. The grants remain issued but ineffective, and affected acceptance claims remain nonconforming. Governance/temporal/hydration orchestration (areas 2, 3, and 7) remains domain-owned unless separately authorized follow-on work changes that decision.

Preservation manifest `3.0.0-rc.1` and its detached Owner approval remain immutable, effective predecessor evidence. The remediated current source is represented by unapproved successor `3.0.0-rc.2`; rc.1 approval does not transfer to that successor, even when its 473-test run is green. FR-20 and SM-C1 therefore remain PENDING, SM-C2 remains FAILED, OQ-1 remains BLOCKED, and the implementation hold remains ACTIVE until the exact successor receives independent authority without a waiver or denominator change.

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

This table preserves the first-pass area estimates and classifications as **superseded provenance**: the sole FR-1 inventory object is the canonical denominator-freezing artifact `docs/release-evidence/consume-promote-keep-inventory-v1.json` (§A), which self-records acceptance while named acceptance remains pending. It resolves every mixed or dual first-pass label below into exactly-one-classification split rows. In particular, row 8's "Promote (partial)" is resolved there as `publication-transport-marshaling` (422 LOC, **Promote, FR-13**, Story 3.5) plus `publication-failure-taxonomy` (131 LOC, Keep), and row 11's "Mixed" resolves into paired Consume/Keep rows. Rows below marked with dual labels do not satisfy FR-1/FR-2's exactly-one-classification consequence and must not be read as the canonical inventory.

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

**Top hotspots by volume:** Queries/cursor (5.3k), Governance (4.3k), Projections (3.0k), Diagnostics (2.4k), and Validation (2.7k, Keep).

## D. Active technical-module surface

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

## E. Cross-module duplication evidence and capability candidates

Modules compared: Conversations, Folders, Projects, Memories, Tenants, Parties.

`Hexalith.Tenants` appears here only as a **domain module in the comparison and as a dependency/consumer**. It is not a technical-module landing zone; generic hosting/runtime behavior belongs in EventStore, Commons, FrontComposer, or another genuine shared technical module.

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

Build only the capabilities that Conversations consumes in the pilot; treat all others as follow-on backlog.

| # | Capability or gap | Current disposition | FR |
|---|---|---|---|
| 1 | `ICommandContract` / `IEventContract` compile-time metadata, parallel to existing `IQueryContract` | **Deferred and non-activated.** Story 3.7 records platform metadata/resolver work, but Conversations did not consume it. That work remains outside pilot authorization and metrics; it cannot count as FR-16 acceptance or be retroactively pulled into scope without separate authority. | FR-16 |
| 2 | Polymorphic JSON registration helper / source-gen catalog | Publicize `TypeMapper` for in-pilot consumption. | FR-14 |
| 3 | Generic tenant-access projection handler | Build for in-pilot consumption. | FR-11 |
| 4 | Generic observability/health hook | **Consume/extend.** `EventStore.ServiceDefaults` already supplies `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, and `MapDefaultEndpoints`; `EventStore.DomainService` supplies `AddEventStoreDomainTelemetry`. Consume these. If Conversations requires a generic hook that the platform-owned surface does not yet support, extend that surface; do not create a Conversations ServiceDefaults or hosting module. | FR-10 |
| 5 | Generic typed-HttpClient registration | Build for in-pilot consumption. | FR-12 |
| 6 | Generic naming, mode, component, or sidecar behavior for Aspire/DAPR topology | **Consume/extend.** `EventStore.Aspire` already supplies `AddHexalithEventStore` and `AddEventStoreDomainModule` for platform-owned shared/isolated DAPR topology. Consume these. If required generic behavior is unsupported, extend `EventStore.Aspire`; do not create a Conversations AppHost/Aspire/hosting module. | FR-13 |
| 7 | Tier-3 integration test harness (command→event→projection→query) | **Backlog.** | — |
| 8 | Snapshot/event-upcasting hook on `EventStoreAggregate<TState>` | **Backlog.** | — |
| 9 | Command-level authorization/validator discovery convention | **Backlog.** | — |
| 10 | Deadletter/poison-pill domain hook | **Backlog.** | — |
