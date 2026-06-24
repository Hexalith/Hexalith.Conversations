# Thin Authoring Template Validation v1

**Story:** 4.1 - Document the thin authoring template, validated against post-refactor Conversations.
**Template:** `docs/domain-module-authoring-template.md`
**Validation date:** 2026-06-24
**Result:** The template is grounded in live Conversations source and release evidence. Optional or deferred items are explicitly marked.

## Validation Method

- Read the current Conversations source anchors for every shared capability before authoring the template.
- Read `docs/release-evidence/promote-adopt-runbook.md`, `docs/release-evidence/consume-promote-keep-inventory-v1.md`, and `docs/release-evidence/release-baseline-v1.md`.
- Treat generated build-output directories as non-evidence. Source, tests, release evidence, and story artifacts are the only anchors used here.

## Live Capability Mapping

| Template row | Current Conversations proof | Disposition |
|---|---|---|
| Shared host | `src/Hexalith.Conversations.Server/Program.cs` calls `builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly)` and `app.UseEventStoreDomainService()`. | Mandatory |
| Aggregate | `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs` derives from `EventStoreAggregate<ConversationState>` and exposes static `Handle(...)` methods for domain commands. | Mandatory |
| Query/cursor | `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs` registers `AddEventStoreQueryCursorCodec(...)`; `src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs` implements thin `IDomainQueryHandler` adapters; `src/Hexalith.Conversations.Server/Queries/ConversationListCursor.cs` keeps domain-only cursor scope, fingerprint, and bounds. | Mandatory |
| Read model | `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs` registers `AddEventStoreReadModelStore()`; `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs` reads through `IReadModelStore`; `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs` writes through `ReadModelWritePolicy`. | Mandatory |
| Projection | `src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs` implements the `IDomainProjectionHandler` full-replay seam and delegates field selection/freshness/evidence to the materializer. | Mandatory |
| Tenant access | `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs` uses `services.AddTenantAccess<...>(static services => services.AddHexalithTenants())`; `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessService.cs` maps neutral fail-closed evaluation into Conversations-safe decisions. | Mandatory |
| Typed client | `src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs` keeps `AddHexalithConversationsClient` as a thin facade over `HttpClientRegistration.AddTypedHttpClient`. | Mandatory when a public .NET client is shipped |
| Aspire/Dapr | `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` uses `AddAspireDaprDomainModule(new AspireDaprDomainModuleOptions(...))` with shared EventStore state-store/pubsub resources and an optional admin web resource. | Mandatory for local distributed hosting; admin web is optional |
| ServiceDefaults | `src/Hexalith.Conversations.ServiceDefaults/ConversationsServiceDefaults.cs` wraps `AddHexalithServiceDefaults(...)`; `src/Hexalith.Conversations.Server/Program.cs` receives runtime defaults through the EventStore domain-service host, so duplicate defaults are avoided. | Optional hook |
| Serialization | `src/Hexalith.Conversations.Contracts/Serialization/ConversationsJsonContext.cs` is the source-generated public contract context; `src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs` uses `JsonSerializationOptions.CreateWeb([ConversationsJsonContext.Default], includeReflectionFallback: true)` and a shared polymorphic registry for public event lookup. | Mandatory for public contracts |
| Telemetry | `src/Hexalith.Conversations.Server/Diagnostics/ConversationTelemetryDefinitions.cs` defines the meter and bounded counters; `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs`, `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs`, and `src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceTelemetry.cs` wrap shared bounded telemetry helpers with content-safe logs. | Mandatory for release-gate and operations evidence |
| Testing/evidence | `docs/release-evidence/release-baseline-v1.md` defines conformance and public contract-shape gates; `docs/release-evidence/consume-promote-keep-inventory-v1.md` defines the Consume/Promote/Keep baseline; `docs/release-evidence/promote-adopt-runbook.md` records the promote/adopt mechanics and Story 3.7 disposition. `tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` adds drift-prevention tests for this artifact. | Mandatory |

## Optional And Deferred Items

| Item | Current evidence | Template treatment |
|---|---|---|
| `Admin.Web` and FrontComposer operator UI | `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` includes an admin web resource; `docs/release-evidence/consume-promote-keep-inventory-v1.md` classifies `admin-web-frontcomposer` as Keep and domain-specific. | Optional unless the domain needs operator UI. Excluded from SM-2 minimal skeleton. |
| Publication subscribers | `docs/release-evidence/consume-promote-keep-inventory-v1.md` splits publication transport mechanics from Conversations-owned publication failure taxonomy. | Optional unless the domain publishes or subscribes to external transport flows. |
| Governance workflows | `docs/release-evidence/consume-promote-keep-inventory-v1.md` marks governance evidence vocabulary as Keep and a promote-later candidate. | Optional unless the domain owns governed retention, redaction, privileged action, or audit state. |
| FR-16 public DTO metadata adoption | `docs/release-evidence/promote-adopt-runbook.md` records the Story 3.7 disposition: EventStore command/event metadata exists, but direct Conversations public DTO adoption was deferred to preserve clean contract boundaries and public wire shape. | Optional platform capability. Do not require public DTOs to reference EventStore metadata interfaces. |
| ServiceDefaults project | `src/Hexalith.Conversations.ServiceDefaults/ConversationsServiceDefaults.cs` provides a module hook; `src/Hexalith.Conversations.Server/Program.cs` uses the EventStore domain-service runtime path. | Include only when a module-owned hook is needed. Avoid double registration. |

## Release-Gate Obligations Carried Forward

New modules must plan these gates from the first story:

- Fail-closed tenant access for missing, stale, unavailable, disabled, ambiguous, or insufficient projection state.
- Idempotency boundaries for command submission and replay.
- Governance/audit pairing where the domain owns governed state.
- Redaction and non-disclosure rules that protect durable events, logs, and public problem details.
- Projection freshness and rebuilding/unavailable states surfaced to callers.
- Provider portability and durable identity rules that avoid external provider session IDs as aggregate identity.
- Content-safe telemetry with bounded dimensions.
- Public contract-shape stability and conformance evidence.

These obligations come from `docs/release-evidence/release-baseline-v1.md`, `docs/release-evidence/promote-adopt-runbook.md`, and the critical rules in `_bmad-output/project-context.md`.

## Story 4.2 handoff

Story 4.2 can measure minimal-module authoring cost without redefining scope:

- Count files and LOC in these included categories when present: `Contracts`, `Client`, domain/core, `Server`, `AppHost`, `ServiceDefaults`, `Testing`, and focused test projects.
- Exclude these optional categories from the SM-2 baseline unless the story explicitly brings them into scope: `Admin.Web`, FrontComposer trust components, publication subscribers, governance workflows, generated output, and local developer artifacts.
- Count only hand-authored source, tests, docs, and project files that a new module author owns.
- Keep shared platform libraries and sibling submodule source out of the domain-module authoring-cost total.

## Validation Summary

The template lists every required live capability with the real adoption one-liner, distinguishes mandatory from optional/deferred surfaces, carries the Story 3.7 metadata disposition honestly, and gives Story 4.2 a measurable skeleton boundary. It cites current Conversations source/evidence anchors rather than aspirational future code.
