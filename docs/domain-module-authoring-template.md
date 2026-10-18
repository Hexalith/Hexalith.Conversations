# Hexalith Domain Module Authoring Template

**Status:** v2 authoring template (Story 11.1 correction), validated against post-refactor `Hexalith.Conversations`.
**Audience:** authors of new Hexalith business-domain modules.

Use this template to start a thin module that writes domain logic while the platform hosts the infrastructure: host composition, EventStore persistence, tenant access, query/projection runtime, client registration, hosting, serialization, telemetry, and evidence. A module owns contracts, domain behavior, module registration, and module tests. The platform owns everything else; see [Ownership Inventory](#ownership-inventory).

## Minimal Project Skeleton

Create only the project categories the domain needs:

| Category | Responsibility | Included in SM-2 baseline |
|---|---|---|
| `Contracts` | Public commands, events, identifiers, query/result DTOs, projections, versioning, and JSON context. Keep this boundary free of server infrastructure. | Yes |
| `Client` | Thin typed client facade and options for adopters. | Yes |
| Domain/core | Aggregate, state, internal domain commands/events, validation, idempotency, and domain rules. | Yes |
| `Server` | Module registration only: the domain assembly marker and the domain-specific dependency graph registered beside the platform-hosted domain service. It does not own projection runtime, event subscription, publication, health, or telemetry runtime. | Yes |
| Repository-only test `AppHost` | Exactly one non-shipping test AppHost that composes the module against the platform-owned Aspire/Dapr wiring for repository tests. It is never packed, published, or reused by adopters, and it counts in authoring cost. | Yes |
| `Testing` | Shared test fixtures and conformance helpers that are domain-owned. | Yes, only if present |
| Focused test projects | Unit, contract, server, conformance, and integration tests matching changed behavior. | Yes |
| `Admin.Web` | Optional operator UI surface. Use only when the domain needs a governance or operations UI. | No |
| FrontComposer trust components | Optional generated/admin metadata and trust UI. Do not hand-edit generated output. | No |
| Publication subscribers | Platform-owned publication and subscription runtime. A module does not author these workers. | No |
| Governance workflows | Optional, required only when the domain owns governed retention, redaction, privileged access, or audit state. | No |

A module never contains a `ServiceDefaults` project and never contains a reusable Aspire library. The platform supplies service defaults, Dapr wiring, event subscription, projection/query runtime, publication, health, and telemetry.

The v1 Story 4.2 baseline measured the minimal authoring cost by counting the SM-2 baseline categories of the v1 template; that baseline is historical and unchanged. Excluded categories are intentionally outside the minimal skeleton unless the new domain explicitly needs them.

## Shared Capability Checklist

| Capability | Required pattern | Module-specific values to supply |
|---|---|---|
| Shared host | Use `builder.AddEventStoreDomainService(domainAssembly, serverAssembly)` and `app.UseEventStoreDomainService()`. Register only the domain-specific dependency graph beside it. | Domain assembly marker, server assembly marker, domain service registrations. |
| Aggregate | Implement `EventStoreAggregate<TState>` with static `Handle(command, state)` methods. Keep replay-safe state application deterministic. | Aggregate name, state type, domain commands, domain events, validation rules. |
| Query/cursor | The platform hosts the query runtime. The module supplies thin `IDomainQueryHandler` adapters over domain query logic, using the platform `AddEventStoreQueryCursorCodec(...)`, `QueryCursorScope`, and domain-only cursor bounds. | Query types, cursor purpose, filter fingerprint, sort version, max age/offset policy. |
| Read model | The platform provides `AddEventStoreReadModelStore()`, `IReadModelStore`, and `ReadModelWritePolicy`. The module supplies read-model DTOs and a merge policy only; it does not hand-roll Dapr state-store update loops. | Stable state-store name, tenant-scoped keys, read-model DTOs, merge/update policy. |
| Projection | The platform hosts projection and event-subscription runtime. The module supplies a stateless full-replay `IDomainProjectionHandler` and domain materializer logic; it never subscribes to events or publishes them itself. | Projection domain, projection type, event decoder, materializer, stale/rebuild policy. |
| Tenant access | Register `services.AddTenantAccess<...>(static services => services.AddHexalithTenants())`. Map neutral fail-closed evaluation into domain-safe decisions. | Requirement enum, denial vocabulary, projection health signal, role-to-requirement rules. |
| Typed client | Keep a thin `AddXxxClient` facade over `HttpClientRegistration.AddTypedHttpClient`. | Client interface, implementation, options type, endpoint selector, validation timing. |
| Aspire/Dapr | Platform-owned. The platform exposes `AddAspireDaprDomainModule(new AspireDaprDomainModuleOptions(...))`; only the single repository-only test AppHost calls it, to compose the module for tests. A module never implements Dapr components, pub/sub, or state-store wiring, and never ships an Aspire library. | Dapr app id, resource name, state-store/pubsub component mode, upstream references (test AppHost only). |
| ServiceDefaults | Platform-owned. The platform provides `AddHexalithServiceDefaults(...)` and the domain-service host registers it. A module must not create a `ServiceDefaults` project or a hook over it. | None; the module supplies no service-defaults code. |
| Serialization | Use a source-generated JSON context with `JsonSerializationOptions.CreateWeb([...])`. Use the shared polymorphic registry for explicit type lookup and keep local converters only for real domain rules. | Public contract types, event types, converter rules, optional reflection fallback boundary. |
| Telemetry | Platform-owned runtime, health, and exporters. Domain metric names are declared through the platform `BoundedTelemetryMeter` and `BoundedTelemetryCounterDefinition` contract with bounded dimensions; the module keeps content-safe log templates and owns no telemetry runtime or health endpoint. | Meter name, counter names, bounded dimension keys, classifier enums, safe log templates. |
| Testing/evidence | Maintain conformance tests, public contract-shape snapshots, at-risk test register entries, documentation validation tests, and release evidence artifacts. | Domain gates, required pass counts, evidence files, focused tests for drift. |

## Ownership Inventory

<!-- thin-guidance:ownership-inventory -->

| Id | Owner | Capability |
|---|---|---|
| `contracts` | module | Contracts: public commands, events, identifiers, query/result DTOs |
| `domain-behavior` | module | Domain behavior: aggregate, state, validation, idempotency, domain rules |
| `module-registration` | module | Module registration: domain assembly marker and domain-specific dependency graph |
| `module-tests` | module | Module tests: unit, contract, server, conformance, and integration tests |
| `service-defaults` | platform | ServiceDefaults |
| `dapr-wiring` | platform | Dapr implementation and wiring |
| `event-subscription` | platform | Event subscription |
| `projection-query-runtime` | platform | Projection and query runtime |
| `publication` | platform | Publication |
| `health` | platform | Health |
| `telemetry` | platform | Telemetry runtime |

A module must never reimplement or reassign a platform-owned capability.

## Prohibited Module Capabilities

<!-- thin-guidance:prohibited-inventory -->

| Id | Prohibition |
|---|---|
| `reusable-aspire-library` | A reusable module Aspire library is prohibited. |
| `module-service-defaults` | A module-owned `ServiceDefaults` project is prohibited. |
| `module-dapr-implementation` | Module-owned Dapr implementation or wiring is prohibited. |
| `module-event-subscription` | Module-owned event subscription is prohibited. |
| `module-projection-query-runtime` | Module-owned projection or query runtime is prohibited. |
| `module-publication` | Module-owned publication is prohibited. |
| `module-health` | Module-owned health endpoints or checks are prohibited. |
| `module-telemetry-runtime` | Module-owned telemetry runtime is prohibited. |

## Repository-Only Test AppHost

<!-- thin-guidance:test-apphost -->

| Rule | Value |
|---|---|
| `count` | Exactly one test AppHost per module. |
| `IsPackable` | `false` |
| `IsPublishable` | `false` |
| `scope` | Repository-only; never shipped to adopters. |
| `authoring-cost` | The test AppHost counts in authoring cost (SM-2) when hand-authored. |

## Mandatory Guardrails

- Do not expose raw EventStore envelopes, aggregate actor IDs, snapshot mechanics, projection internals, or substrate routing as the primary adopter API.
- Do not introduce direct persistence tables for domain writes. EventStore is the authoritative write path.
- Do not fail open on tenant access. Missing, stale, disabled, ambiguous, unavailable, or insufficient projection state denies access.
- Do not persist Party personal data in durable domain events unless a governance decision explicitly approves an immutable audit snapshot.
- Do not put unbounded replay on hot read/write paths. Use EventStore snapshots, query-side read models, and bounded cursor policy.
- Use central package management and keep project package references versionless. For Hexalith modules, import the shared `references/Hexalith.Builds/Props/Directory.Packages.props` baseline through the module `Directory.Packages.props`; add local `PackageVersion` entries only as documented exceptions.
- Do not initialize nested submodules recursively. Initialize only root-declared submodules under `references/` when needed.
- Do not use generated build output as evidence or documentation source of truth.

## Release-Gate Obligations

Plan these gates from the first story; do not retrofit them after a module ships. Each maps to the same obligations validated for Conversations in `docs/release-evidence/thin-authoring-template-validation-v1.md`.

- **Fail-closed tenant access** for missing, stale, unavailable, disabled, ambiguous, or insufficient projection state.
- **Idempotency boundaries** for command submission and replay.
- **Governance/audit pairing** where the domain owns governed retention, redaction, privileged action, or audit state.
- **Redaction and non-disclosure** rules that protect durable events, logs, and public problem details.
- **Projection freshness** and rebuilding/unavailable states surfaced to callers, never silently served as fresh.
- **Provider portability** and durable identity rules that avoid external provider session IDs as aggregate identity.
- **Content-safe telemetry** with bounded dimensions and no content in metrics or logs.
- **Public contract-shape stability** and conformance evidence from the first release.

## Story 3.7 Metadata Disposition

EventStore command/event metadata support exists as an additive platform capability. It is not a blanket requirement for public domain DTOs.

For a new domain, metadata may be adopted when it does not leak EventStore dependencies into `Contracts`, does not add serialized members solely for routing, and does not reshape public command/event vocabularies. If adoption would violate the public contract boundary, keep metadata behind an adapter or defer it with evidence. This preserves clean bounded-context contracts and matches the Conversations FR-16 public DTO metadata adoption disposition.

## Authoring Sequence

1. Create the minimal project skeleton, including exactly one repository-only test AppHost, and decide which optional categories are out of scope.
2. Define public contracts and the aggregate state/event model before host or UI work.
3. Register the module against the platform-hosted shared host, aggregate base, query/cursor, read model, projection, tenant access, client, hosting, serialization, telemetry, and testing/evidence capabilities using the checklist above. Do not author platform-owned capabilities.
4. Add tests with each capability, starting with domain and contract tests before integration and conformance lanes.
5. Produce release evidence that maps each adopted capability to live source/test anchors.
6. Verify public contract-shape stability and record any deferred optional capability honestly.
