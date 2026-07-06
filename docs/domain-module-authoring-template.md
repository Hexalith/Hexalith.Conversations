# Hexalith Domain Module Authoring Template

**Status:** v1 authoring template, validated against post-refactor `Hexalith.Conversations`.
**Audience:** authors of new Hexalith business-domain modules.

Use this template to start a thin module that writes domain logic while adopting shared platform capabilities for host composition, EventStore persistence, tenant access, query/projection seams, client registration, hosting, serialization, telemetry, and evidence.

## Minimal Project Skeleton

Create only the project categories the domain needs:

| Category | Responsibility | Included in SM-2 baseline |
|---|---|---|
| `Contracts` | Public commands, events, identifiers, query/result DTOs, projections, versioning, and JSON context. Keep this boundary free of server infrastructure. | Yes |
| `Client` | Thin typed client facade and options for adopters. | Yes |
| Domain/core | Aggregate, state, internal domain commands/events, validation, idempotency, and domain rules. | Yes |
| `Server` | Application boundary, query adapters, projection adapters, tenant access, read-store wiring, diagnostics wrappers, and domain-service host. | Yes |
| `AppHost` | Local Aspire topology and Dapr component composition for the domain module. | Yes |
| `ServiceDefaults` | Optional module-owned hook over shared service defaults when a domain-specific entrypoint needs it. | Yes, only if present |
| `Testing` | Shared test fixtures and conformance helpers that are domain-owned. | Yes, only if present |
| Focused test projects | Unit, contract, server, conformance, and integration tests matching changed behavior. | Yes |
| `Admin.Web` | Optional operator UI surface. Use only when the domain needs a governance or operations UI. | No |
| FrontComposer trust components | Optional generated/admin metadata and trust UI. Do not hand-edit generated output. | No |
| Publication subscribers | Optional domain-specific publication workers or transport subscribers. | No |
| Governance workflows | Optional, required only when the domain owns governed retention, redaction, privileged access, or audit state. | No |

Story 4.2 can measure the minimal authoring cost by counting the SM-2 baseline categories above. Excluded categories are intentionally outside the minimal skeleton unless the new domain explicitly needs them.

## Shared Capability Checklist

| Capability | Required pattern | Module-specific values to supply |
|---|---|---|
| Shared host | Use `builder.AddEventStoreDomainService(domainAssembly, serverAssembly)` and `app.UseEventStoreDomainService()`. Register only the domain-specific dependency graph beside it. | Domain assembly marker, server assembly marker, domain service registrations. |
| Aggregate | Implement `EventStoreAggregate<TState>` with static `Handle(command, state)` methods. Keep replay-safe state application deterministic. | Aggregate name, state type, domain commands, domain events, validation rules. |
| Query/cursor | Implement thin `IDomainQueryHandler` adapters over domain query logic. Use `AddEventStoreQueryCursorCodec(...)`, `QueryCursorScope`, and domain-only cursor bounds. | Query types, cursor purpose, filter fingerprint, sort version, max age/offset policy. |
| Read model | Use `AddEventStoreReadModelStore()`, `IReadModelStore`, and `ReadModelWritePolicy`. Do not hand-roll Dapr state-store update loops. | Stable state-store name, tenant-scoped keys, read-model DTOs, merge/update policy. |
| Projection | Implement `IDomainProjectionHandler` as a stateless full-replay seam. Keep field selection, freshness, and evidence in domain materializer logic. | Projection domain, projection type, event decoder, materializer, stale/rebuild policy. |
| Tenant access | Register `services.AddTenantAccess<...>(static services => services.AddHexalithTenants())`. Map neutral fail-closed evaluation into domain-safe decisions. | Requirement enum, denial vocabulary, projection health signal, role-to-requirement rules. |
| Typed client | Keep a thin `AddXxxClient` facade over `HttpClientRegistration.AddTypedHttpClient`. | Client interface, implementation, options type, endpoint selector, validation timing. |
| Aspire/Dapr | Use `AddAspireDaprDomainModule(new AspireDaprDomainModuleOptions(...))` with shared or isolated infrastructure mode. | Dapr app id, resource name, state-store/pubsub component mode, upstream references. |
| ServiceDefaults | Provide a module hook over `AddHexalithServiceDefaults(...)` only where needed. Avoid duplicate defaults when the domain-service host already registers runtime defaults. | Service name, meter/activity names, health/readiness hooks. |
| Serialization | Use a source-generated JSON context with `JsonSerializationOptions.CreateWeb([...])`. Use the shared polymorphic registry for explicit type lookup and keep local converters only for real domain rules. | Public contract types, event types, converter rules, optional reflection fallback boundary. |
| Telemetry | Define a `BoundedTelemetryMeter` and `BoundedTelemetryCounterDefinition` contract with bounded dimensions. Keep content-safe logs and wrapper services that preserve domain metric names. | Meter name, counter names, bounded dimension keys, classifier enums, safe log templates. |
| Testing/evidence | Maintain conformance tests, public contract-shape snapshots, at-risk test register entries, documentation validation tests, and release evidence artifacts. | Domain gates, required pass counts, evidence files, focused tests for drift. |

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

1. Create the minimal project skeleton and decide which optional categories are out of scope.
2. Define public contracts and the aggregate state/event model before host or UI work.
3. Wire the shared host, aggregate base, query/cursor, read model, projection, tenant access, client, hosting, serialization, telemetry, and testing/evidence capabilities using the checklist above.
4. Add tests with each capability, starting with domain and contract tests before integration and conformance lanes.
5. Produce release evidence that maps each adopted capability to live source/test anchors.
6. Verify public contract-shape stability and record any deferred optional capability honestly.
