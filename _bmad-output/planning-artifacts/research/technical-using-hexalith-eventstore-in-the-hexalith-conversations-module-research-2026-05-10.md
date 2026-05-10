---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 1
research_type: 'technical'
research_topic: 'Using Hexalith.EventStore in the Hexalith.Conversations module'
research_goals: 'Determine how Hexalith.EventStore should be used by Hexalith.Conversations, including architecture, integration points, coding patterns, persistence flow, and risks.'
user_name: 'Jerome'
date: '2026-05-10'
web_research_enabled: true
source_verification: true
---

# Research Report: technical

**Date:** 2026-05-10
**Author:** Jerome
**Research Type:** technical

---

## Research Overview

This research investigates how `Hexalith.Conversations` should use `Hexalith.EventStore` as its event-sourced persistence and integration substrate. The analysis combines local repository inspection, existing Conversations planning artifacts, EventStore source/docs, and current public documentation for Dapr, .NET Aspire, CQRS/event sourcing, API security, OpenAPI, RFC 9457 Problem Details, and DevOps implementation practices.

The central finding is that Conversations should use EventStore internally for aggregate command processing, event persistence, snapshots, pub/sub publication, and projection invalidation, while exposing adopter-facing Conversations contracts and client APIs that hide EventStore internals. The full synthesis below consolidates the technology stack, integration patterns, architecture decisions, implementation roadmap, risk register, and source-verification trail.

---

## Technical Research Scope Confirmation

**Research Topic:** Using Hexalith.EventStore in the Hexalith.Conversations module
**Research Goals:** Determine how Hexalith.EventStore should be used by Hexalith.Conversations, including architecture, integration points, coding patterns, persistence flow, and risks.

**Technical Research Scope:**

- Architecture Analysis - design patterns, frameworks, system architecture
- Implementation Approaches - development methodologies, coding patterns
- Technology Stack - languages, frameworks, tools, platforms
- Integration Patterns - APIs, protocols, interoperability
- Performance Considerations - scalability, optimization, patterns

**Research Methodology:**

- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-05-10

---

## Technology Stack Analysis

### Web Search And Source Verification

Research was verified against the public Hexalith.EventStore repository, current Dapr documentation, Microsoft .NET Aspire documentation, and the local workspace source tree. Public search confirms `Hexalith.EventStore` is an active public GitHub repository with latest release `v3.11.1` dated 2026-05-05, and the repository README describes it as a DAPR-native event sourcing server for .NET. Local repository docs align with that public README and contain more detailed architecture, package, command API, query API, envelope, and identity guidance.

Sources:

- Public repository: https://github.com/Hexalith/Hexalith.EventStore
- Local package guide: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\nuget-packages.md`
- Local architecture guide: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`
- Local command API guide: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\command-api.md`
- Local query API guide: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\query-api.md`
- Dapr state management: https://docs.dapr.io/developing-applications/building-blocks/state-management/
- Dapr actors: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/
- Dapr building blocks: https://docs.dapr.io/concepts/building-blocks-concept/
- .NET Aspire AppHost: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview
- CloudEvents 1.0 announcement: https://www.cncf.io/announcements/2019/10/28/serverless-specification-cloudevents-reaches-version-1-0/

Confidence: high for EventStore stack and package roles, because local docs and public README agree. Confidence: medium for Conversations runtime integration details, because this repository currently contains planning artifacts for Hexalith.Conversations but no first-class `src/Hexalith.Conversations.*` projects yet.

### Programming Languages

Hexalith.Conversations should be implemented as a C#/.NET module, matching the EventStore and neighboring Hexalith modules. `Hexalith.EventStore` targets `net10.0`, enables nullable reference types and implicit usings, and treats warnings as errors in `Directory.Build.props`. The same defaults should be mirrored by Conversations once runtime projects are created.

The recommended language model for Conversations is:

- C# records for commands and immutable event payloads.
- C# classes for aggregate state where `Apply(...)` methods mutate private state during replay.
- Static aggregate handler methods with the shape `Handle(Command, State?) -> DomainResult`, using `EventStoreAggregate<TState>`.
- No direct database or broker client usage in Conversations domain logic.

For the conversation domain, the first aggregate should be `ConversationAggregate : EventStoreAggregate<ConversationState>`. Commands should model the PRD lifecycle: `CreateConversation`, `AppendMessage`, `AddParticipant`, `AttachFileReference`, `UpdateConversationMetadata`, `SetRetentionPolicy`, `MarkSensitiveData`, `RedactMessageContent`, and `CloseOrArchiveConversation`, phased according to release scope.

Popular Language: C# on .NET 10.
Emerging Language: none recommended for core runtime; non-.NET subscribers may consume published envelopes later through CloudEvents and JSON.
Language Evolution: keep contracts versioned and additive; persisted events must remain replayable across versions.
Performance Characteristics: aggregate state replay is straightforward in C#; larger conversations require projection-first reads and snapshot policy rather than loading all messages for every user-facing query.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\Directory.Build.props`
- `D:\Hexalith.Conversations\Hexalith.EventStore\README.md`
- https://github.com/Hexalith/Hexalith.EventStore

### Development Frameworks And Libraries

The core library stack for Conversations should be role-separated by project:

| Conversations project | EventStore dependency | Purpose |
| --- | --- | --- |
| `Hexalith.Conversations.Contracts` | `Hexalith.EventStore.Contracts` | Public command, event, projection, error, and identity contract types. |
| `Hexalith.Conversations.Server` or domain service | `Hexalith.EventStore.Client` | Aggregate registration, `EventStoreAggregate<TState>`, domain processor activation. |
| EventStore host/app integration | `Hexalith.EventStore.Server` | Command gateway, actors, state, snapshots, pub/sub, command status. |
| `Hexalith.Conversations.Client` | Raw HTTP or generated client over EventStore APIs | Adopter-friendly commands and queries without EventStore leakage. |
| `Hexalith.Conversations.Tests` | `Hexalith.EventStore.Testing` | Builders, fakes, assertions for command/event behavior. |
| `Hexalith.Conversations.AppHost` | `Hexalith.EventStore.Aspire` | Local Aspire/Dapr topology orchestration. |

EventStore's package guide defines six published package roles: Contracts, Client, Server, SignalR, Testing, and Aspire. Conversations should not reference `Hexalith.EventStore.Server` from its domain contracts; keep server infrastructure out of public contracts.

Major Frameworks: Hexalith.EventStore, ASP.NET Core, Dapr, .NET Aspire, MediatR, FluentValidation, SignalR.
Micro-frameworks: the EventStore `EventStoreAggregate<TState>` programming model should be enough for the first aggregate.
Evolution Trends: EventStore centralizes command routing, envelope metadata, snapshots, publication, and query invalidation; Conversations should build domain semantics above that substrate rather than reimplementing it.
Ecosystem Maturity: local EventStore docs include package guidance, command/query references, identity scheme, event envelope, and deployment guidance, making it suitable as the module substrate.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\nuget-packages.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\getting-started\first-domain-service.md`
- https://github.com/Hexalith/Hexalith.EventStore

### Database And Storage Technologies

Conversations should not choose a database directly. Hexalith.EventStore uses Dapr state management and pub/sub as the storage and distribution abstraction. Dapr's official docs describe state management as a key/value API over swappable supported state stores, and EventStore's architecture guide uses the state store for actor state, event streams, snapshots, command status, and idempotency records.

Recommended storage posture:

- Treat `Hexalith.EventStore` as the authoritative write-side persistence path.
- Store conversation facts as immutable events, not mutable transcript rows.
- Build read models/projections for conversation list, message timeline, attachment list, governance state, and recent activity.
- Use EventStore identity rules: `tenant`, `domain`, and `aggregateId` derive actor IDs, state keys, event stream keys, snapshot keys, pub/sub topics, and dead-letter topics.
- Keep tenant and domain lowercase; choose aggregate IDs that are stable, opaque, and tenant-scoped.

Relational Databases: PostgreSQL can be a Dapr state backend, but Conversations should not couple to PostgreSQL APIs.
NoSQL Databases: Cosmos DB or other Dapr state stores can be used through component configuration.
In-Memory Databases: Redis is a likely local/dev state, pub/sub, and config backend in the existing EventStore docs.
Data Warehousing: out of scope for command processing; downstream analytics should consume published events or governed projections.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\identity-scheme.md`
- https://docs.dapr.io/developing-applications/building-blocks/state-management/

### Development Tools And Platforms

The development workflow should follow the EventStore repo:

- .NET SDK and `dotnet build`/`dotnet test`.
- Aspire AppHost for local multi-service topology and dashboard-driven debugging.
- Dapr CLI/sidecars for state, actors, pub/sub, configuration, and service invocation.
- Docker Desktop for local infrastructure.
- Swagger/OpenAPI for command/query smoke testing and generated clients.
- xUnit v3, Shouldly, NSubstitute, and EventStore testing fakes for tests.
- OpenTelemetry packages already used by EventStore service defaults.

The API layer matters for Conversations consumers. EventStore's command API accepts asynchronous `POST /api/v1/commands` submissions and returns `202 Accepted` with a correlation ID. It also exposes command validation, command status, query execution, query validation, projection invalidation, and optional SignalR projection-change hints. Conversations should hide those substrate mechanics behind a `Hexalith.Conversations.Client` happy path where possible.

IDE And Editors: no special requirement beyond standard .NET tooling.
Version Control: repository uses git submodules; only root-level submodules should be initialized or updated unless nested submodules are explicitly requested.
Build Systems: centrally managed NuGet package versions are already used by EventStore.
Testing Frameworks: use EventStore testing helpers for command/event flows and add adopter-facing conformance tests for Conversations contracts.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\command-api.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\query-api.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\Directory.Packages.props`
- https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview

### Cloud Infrastructure And Deployment

Hexalith.EventStore is designed around Dapr infrastructure portability. Dapr official docs list state management, pub/sub, service invocation, actors, and configuration among the building blocks; EventStore uses those exact blocks for aggregate state, event streams, projections, command routing, domain service invocation, and tenant/domain service resolution.

Recommended deployment posture for Conversations:

- Local development: Aspire AppHost plus Dapr sidecars and local Redis-backed components.
- Containerized deployment: EventStore deployment guides support Docker Compose and Kubernetes-style Dapr components.
- Azure deployment: Azure Container Apps is a natural target if aligned with EventStore deployment docs.
- Pub/sub events should use tenant/domain topic partitioning, e.g. `{tenant}.{domain}.events`, with Conversations domain topic names derived by EventStore identity conventions.
- External subscribers should expect a CloudEvents wrapper with a flat EventStore envelope payload when consuming Dapr pub/sub messages.

Major Cloud Providers: Azure is the strongest implied fit because EventStore includes Azure Container Apps deployment guidance and the broader stack uses .NET Aspire.
Container Technologies: Docker and Kubernetes/Dapr sidecars.
Serverless Platforms: not recommended as the first Conversations runtime shape; Dapr sidecars and actors are central.
CDN And Edge Computing: irrelevant to write-side persistence; only UI/static hosting may use it later.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\event-envelope.md`
- https://docs.dapr.io/concepts/building-blocks-concept/
- https://www.cncf.io/announcements/2019/10/28/serverless-specification-cloudevents-reaches-version-1-0/

### Technology Adoption Trends

The strongest architectural trend in the existing Hexalith ecosystem is vertical alignment: modules use EventStore for aggregate persistence, Dapr for portable infrastructure, and module-specific contract/client packages so adopters do not need to learn substrate internals. The Conversations PRD explicitly reinforces this: adopter developers should integrate through published contracts and a supported client, execute a minimal create/append/read happy path, and rely on documented tenant binding, Party identity, idempotency, projection freshness, event publication, and governance behavior.

For Conversations, this implies:

- Use EventStore directly inside the module implementation.
- Do not expose raw EventStore command envelopes, aggregate IDs, projection mechanics, snapshots, replay, or SignalR details as the primary adopter experience.
- Keep public command/event/projection contracts stable and versioned.
- Build conformance tests around duplicate commands, tenant isolation, event schema evolution, projection rebuild, and redaction replay.
- Treat projections as the read API for user and operator workflows; do not use write-side event streams as UI query storage.

Migration Patterns: replace bespoke chatbot transcript storage with Conversations client commands and projections.
Emerging Technologies: Dapr actors and Aspire AppHost fit the existing EventStore topology rather than being optional add-ons.
Legacy Technology: direct SQL transcript tables with audit columns should be retired for this bounded context.
Community Trends: current Dapr docs remain active through v1.17-era updates, and EventStore public repository activity/release history indicates the substrate is evolving.

Sources:

- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md`
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\product-brief-Hexalith.Conversations.md`
- https://github.com/Hexalith/Hexalith.EventStore
- https://docs.dapr.io/concepts/building-blocks-concept/

---

## Integration Patterns Analysis

### Web Search And Source Verification

Integration-pattern research was verified against current Dapr documentation, current .NET Aspire documentation, protocol standards, public Hexalith.EventStore repository documentation, and the local EventStore docs. The most relevant current facts are:

- Dapr service invocation supports HTTP/gRPC inter-service calls with service discovery, tracing, error handling, and security concerns handled by the sidecar layer.
- Dapr actors build on service invocation and state management and provide identity-bound stateful objects; EventStore maps each aggregate identity to an actor.
- Dapr pub/sub uses CloudEvents 1.0 by default and supports topic subscriptions, dead-letter handling, and broker abstraction.
- OpenAPI is the standard machine-readable description mechanism for HTTP APIs; EventStore exposes Swagger/OpenAPI for command/query APIs.
- RFC 9457 is the current Problem Details standard for HTTP API errors, replacing RFC 7807.

Sources:

- Dapr service invocation: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/
- Dapr actors: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/
- Dapr state overview: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/
- Dapr pub/sub: https://docs.dapr.io/developing-applications/building-blocks/pubsub/
- Dapr pub/sub API and CloudEvents: https://docs.dapr.io/reference/api/pubsub_api/
- .NET Aspire AppHost: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview
- OpenAPI Specification 3.1.1: https://spec.openapis.org/oas/v3.1.1.html
- RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457
- OAuth 2.0 bearer tokens: https://www.rfc-editor.org/rfc/rfc6750
- Hexalith.EventStore public repository: https://github.com/Hexalith/Hexalith.EventStore

Confidence: high for protocol and EventStore integration mechanics. Confidence: medium for the exact Conversations API façade because the runtime module is not implemented yet and must be designed from the PRD and adjacent Hexalith modules.

### API Design Patterns

Hexalith.Conversations should expose a Conversations-owned API/client façade while using EventStore's command and query APIs internally. The product requirement is explicit: adopter developers should integrate through published contract/client packages and should not need EventStore internals. Therefore, avoid making adopters construct raw EventStore `tenant`, `domain`, `aggregateId`, `commandType`, `payload`, and `extensions` envelopes as the primary integration path.

Recommended API layers:

1. `Hexalith.Conversations.Contracts`: typed commands, events, projections, typed errors, schema-version metadata, and DTOs.
2. `Hexalith.Conversations.Client`: typed operations such as `CreateConversationAsync`, `AppendMessageAsync`, `GetConversationAsync`, and `ListConversationsAsync`.
3. Internal EventStore adapter: maps typed Conversations operations into EventStore command/query calls.
4. EventStore Command API: asynchronous `POST /api/v1/commands`, status polling, validation, archived command/replay support.
5. EventStore Query API: projection query execution, `If-None-Match`/ETag support, preflight validation, projection invalidation.

RESTful APIs: use typed resource-oriented Conversations methods at the client boundary, with EventStore REST retained as the substrate boundary. OpenAPI should be generated for the public Conversations API and for the underlying EventStore host.
GraphQL APIs: not recommended for v1. The PRD emphasizes conformance, tenant safety, and projection freshness; GraphQL can be revisited after stable projections and authorization policy exist.
RPC and gRPC: Dapr service invocation may use HTTP/gRPC internally, but Conversations domain code should stay on EventStore's aggregate programming model rather than hand-writing gRPC service contracts.
Webhook Patterns: external webhooks are not a v1 need; use EventStore-published domain events for system-to-system notification.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\command-api.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\query-api.md`
- OpenAPI Specification 3.1.1: https://spec.openapis.org/oas/v3.1.1.html
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md`

### Communication Protocols

The write path should use HTTP at the adopter and command gateway boundary, Dapr service invocation between EventStore and domain services, Dapr actors for aggregate execution, Dapr state APIs for persistence, and Dapr pub/sub for event publication.

Recommended command flow:

```text
Adopter / Chatbot
  -> Hexalith.Conversations.Client
  -> Conversations API or direct EventStore adapter
  -> EventStore POST /api/v1/commands
  -> MediatR validation / authorization / routing
  -> Dapr AggregateActor for tenant:conversation:conversationId
  -> Conversations domain processor Handle(command, state)
  -> EventStore persists event envelopes
  -> Dapr pub/sub publishes tenant-isolated events
  -> projections update read models
  -> Query API / Conversations client reads projections
```

HTTP/HTTPS Protocols: required for public command/query surfaces; responses should use `202 Accepted` for asynchronous command acceptance and typed problem details for errors.
WebSocket Protocols: use only indirectly through SignalR projection-change hints, not as the authoritative conversation state stream.
Message Queue Protocols: do not couple Conversations to AMQP, Kafka, RabbitMQ, Redis Streams, or Azure Service Bus APIs directly; let Dapr pub/sub components select the broker.
gRPC and Protocol Buffers: Dapr may use gRPC internally; no v1 need to expose a public Conversations gRPC contract.

The EventStore documentation has a contract nuance that Conversations must resolve before implementation: the command lifecycle and quickstart describe retry safety through a causation/message id and idempotency records, while the command API reference states that command submission itself does not provide idempotency guarantees. Conversations should define its own command idempotency contract explicitly, then map it to the current EventStore field that the installed version actually honors.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\command-lifecycle.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\getting-started\quickstart.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\command-api.md`
- Dapr service invocation: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/
- Dapr actors: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/

### Data Formats And Standards

The primary data format should be JSON over HTTP, with typed C# contracts in the Conversations packages. EventStore stores event payloads as serialized bytes inside event envelopes; the envelope metadata carries tenant, domain, aggregate identity, sequence number, timestamp, correlation, causation, user, domain service version, event type, serialization format, payload, and extensions. Published events are wrapped by Dapr pub/sub as CloudEvents 1.0 messages.

Conversations event payloads must contain enough information to rebuild projections deterministically. For example, `MessageAppended` should include the stable message id, participant/Party id, role, content reference or content payload according to policy, provider metadata references, occurred-at semantics, and any schema-versioned extension metadata needed for replay. Redaction should be modeled as later events, not destructive mutation of old events.

JSON and XML: use JSON for API requests/responses and event payload serialization; XML is not recommended.
Protobuf and MessagePack: defer; EventStore supports a `serializationFormat` field and pre-serialized payload hooks, but v1 should optimize for inspectability and contract tests.
CSV and Flat Files: useful only for export/reporting; not for command/event integration.
Custom Data Formats: use typed Conversations event schemas plus version metadata, not opaque custom blobs.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\event-envelope.md`
- Dapr pub/sub API and CloudEvents: https://docs.dapr.io/reference/api/pubsub_api/
- CloudEvents 1.0 announcement: https://www.cncf.io/announcements/2019/10/28/serverless-specification-cloudevents-reaches-version-1-0/

### System Interoperability Approaches

The main interoperability principle is "typed module boundary, substrate hidden." Conversations should interoperate with:

- `Hexalith.EventStore` for command processing, persistence, snapshots, pub/sub, query pipeline, and projection invalidation.
- `Hexalith.Tenants` through a local tenant-access projection that fails closed.
- `Hexalith.Parties` through stable party IDs for human users, AI agents, and LLM identities.
- `Hexalith.Projects` and `Hexalith.Folders` through stable references rather than ownership of upstream data.
- `Hexalith.FrontComposer` through command/projection metadata and UI-ready projection contracts.
- External chatbot/agent adopters through `Hexalith.Conversations.Client`, not raw EventStore internals.

Point-to-Point Integration: acceptable for the typed Conversations client calling the module API; avoid direct database or broker point-to-point integrations.
API Gateway Patterns: EventStore already provides the command/query gateway; Conversations may add a module façade if adopter ergonomics need a cleaner API.
Service Mesh: not a first requirement; Dapr sidecars cover the immediate service invocation and pub/sub abstraction.
Enterprise Service Bus: not recommended; Dapr pub/sub plus typed contracts are sufficient for v1.

Sources:

- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\product-brief-Hexalith.Conversations.md`
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`

### Microservices Integration Patterns

Conversations should follow EventStore's command-event architecture:

- Commands target one aggregate identity: `{tenant}:{conversation}:{conversationId}`.
- The aggregate actor serializes writes per conversation.
- Events are persisted before publication.
- Projections consume ordered events and provide read models.
- Read APIs query projections rather than event streams.
- Projection change notifications are invalidation hints, not state.

API Gateway Pattern: EventStore is the internal command/query gateway. A Conversations façade can wrap it for adopter ergonomics.
Service Discovery: use Aspire AppHost for local orchestration and Dapr service invocation for runtime service lookup.
Circuit Breaker Pattern: configure through Dapr resiliency policies and HTTP resilience packages in service defaults; do not hand-code per-call retry loops in domain logic.
Saga Pattern: keep out of the first aggregate unless cross-module workflows become required. If a conversation command must coordinate with projects, folders, or parties, prefer references plus eventual projection validation over distributed transactions.

For projections, favor idempotent handlers because pub/sub and projection rebuilds can deliver duplicate or replayed events. The EventStore projection planning docs explicitly describe at-least-once delivery and full replay for projection builder scenarios, with idempotent domain projection handlers absorbing duplicates.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\superpowers\specs\2026-03-15-server-managed-projection-builder-design.md`
- .NET Aspire AppHost: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview
- Dapr service invocation: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/

### Event-Driven Integration

Event-driven integration is the right default for downstream consumers. Each meaningful state change in Conversations should produce a domain event, and EventStore should publish those events to tenant/domain-scoped topics. Consumers should not rely on synchronous callbacks to learn that a message was appended or content was redacted.

Recommended event categories:

- Lifecycle: `ConversationCreated`, `ConversationArchived`, `ConversationReopened` if in scope.
- Participants: `ParticipantAdded`, `ParticipantRemoved`, `ParticipantRoleChanged`.
- Messages: `MessageAppended`, `MessageLinkedToProviderResponse`, `MessageContentRedacted`.
- Attachments and context: `AttachmentReferenceAdded`, `ProjectLinked`, `FolderLinked`.
- Governance: `RetentionPolicySet`, `SensitiveDataMarked`, `RedactionApplied`, `GovernanceActionRejected`.
- Rejections: domain rejection events for invalid lifecycle, authorization-safe command refusal, stale tenant state, unsupported schema version, and governance precondition failures.

Publish-Subscribe Patterns: use Dapr pub/sub; topics are tenant/domain isolated by EventStore identity conventions.
Event Sourcing: EventStore is the source of truth; projections are rebuildable materializations.
Message Broker Patterns: broker-specific behavior must stay behind Dapr component configuration.
CQRS Patterns: commands and projections are separate. The module should document projection freshness states such as current, stale, rebuilding, unavailable, and hidden by tenant policy.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\identity-scheme.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\query-api.md`
- Dapr pub/sub: https://docs.dapr.io/developing-applications/building-blocks/pubsub/
- Dapr pub/sub API and CloudEvents: https://docs.dapr.io/reference/api/pubsub_api/

### Integration Security Patterns

Security must be enforced before any aggregate or projection access. EventStore already authenticates command/query endpoints with JWT bearer tokens and tenant-aware authorization; Conversations must add module-level rules for tenant binding, Party attribution, governance privileges, and content-safe error handling.

Recommended security pattern:

- Accept OAuth2/JWT bearer tokens at HTTP boundaries.
- Require tenant context in every command/query.
- Compare tenant from request body/path/header/client context against token claims and local Tenants projection.
- Validate actor/Party identity before attributing conversation actions.
- Use RFC 9457-style problem details for typed errors, but ensure errors do not reveal inaccessible tenant IDs, Party IDs, conversation existence, provider payloads, or redacted content.
- Keep provider payload and prompt/message content out of logs, traces, error details, metric dimensions, and unrestricted extension metadata.
- Treat SignalR notifications as invalidation only; re-query authorized projections after reconnect or refresh hints.

OAuth 2.0 and JWT: EventStore already requires bearer tokens; Conversations should preserve that model for module APIs.
API Key Management: not recommended for first-party module integration; use service identity/JWT or Dapr service invocation where appropriate.
Mutual TLS: Dapr service invocation can provide secure service-to-service communication capabilities; exact mTLS posture depends on deployment mode.
Data Encryption: rely on platform and Dapr component capabilities for transport/storage, but Conversations must also define content redaction and payload protection policies because conversation content is sensitive.

Sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\command-api.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\query-api.md`
- RFC 6750 OAuth 2.0 bearer tokens: https://www.rfc-editor.org/rfc/rfc6750
- RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457
- Dapr service invocation: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/

### Integration Recommendation

Use this integration structure for the first Conversations implementation:

1. Create `Hexalith.Conversations.Contracts` with typed commands/events/projections and schema-versioned error contracts.
2. Implement `ConversationAggregate : EventStoreAggregate<ConversationState>` in the Conversations domain service.
3. Register the domain with `AddEventStore()` / `UseEventStore()` and let EventStore derive the domain name, actor identity, streams, snapshots, and topics.
4. Build `Hexalith.Conversations.Client` as the adopter-facing API. It should own idempotency token naming, command status polling, tenant-safe errors, and projection freshness translation.
5. Use EventStore command/query APIs internally; do not expose EventStore envelopes as the primary consumer contract.
6. Publish every meaningful state change as a Conversations event and keep projection handlers idempotent.
7. Use Tenants and Parties as reference systems through local projections/stable IDs; do not copy their lifecycle state into the Conversation aggregate as source-of-truth data.
8. Define the idempotency contract explicitly before coding because local EventStore docs currently contain mixed wording around command submission idempotency.

The shortest safe first slice is: `CreateConversation`, `AppendMessage`, `GetConversation`, `ListConversations`, and the corresponding `ConversationCreated` / `MessageAppended` events plus conversation-detail and message-timeline projections.

---

## Architectural Patterns and Design

### Web Search And Source Verification

Architectural research was verified against Microsoft Azure Architecture Center pattern guidance, current Dapr resiliency/security-adjacent documentation, OWASP API Security guidance, OpenTelemetry semantic convention documentation, and local Hexalith.EventStore docs. The external architecture guidance aligns strongly with the local EventStore architecture: use event sourcing where immutable history and replay matter, combine it with CQRS/materialized views for query workloads, use bounded contexts and aggregates for consistency boundaries, and keep resiliency/authorization explicit.

Sources:

- Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- Azure CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Azure microservices domain analysis: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis
- Azure Cache-Aside pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside
- Dapr resiliency: https://docs.dapr.io/concepts/resiliency-concept/
- OWASP API Security Top 10 2023: https://owasp.org/blog/2023/07/03/owasp-api-top10-2023
- OpenTelemetry semantic conventions: https://opentelemetry.io/docs/concepts/semantic-conventions/
- Local EventStore architecture: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`
- Local EventStore security model: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\security-model.md`
- Local EventStore Dapr component reference: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\dapr-component-reference.md`

Confidence: high for the recommended architecture style. The main remaining uncertainty is not the pattern choice, but the exact v1 scope and how much of the adopter-facing API façade is implemented in the first release.

### System Architecture Patterns

Hexalith.Conversations should be a bounded-context module, not a generic transcript table or chatbot-owned persistence layer. Its core responsibility is the durable, tenant-scoped conversation record: lifecycle, participants as stable references, message history, attachment references, provider correlation metadata, retention/redaction/governance state, and projections for readers/operators.

The recommended architecture is:

```text
Hexalith.Conversations.Contracts
  -> commands, events, projections, typed errors, schema/version constants

Hexalith.Conversations.Domain / Server
  -> ConversationAggregate : EventStoreAggregate<ConversationState>
  -> Handle(command, state) pure command handlers
  -> Apply(event) replay methods

Hexalith.Conversations.Client
  -> adopter-friendly create/append/read/list methods
  -> maps to EventStore command/query/status APIs
  -> hides EventStore envelope, polling, ETag, and projection mechanics

Hexalith.Conversations.Projections
  -> conversation detail
  -> message timeline
  -> conversation list / recent activity
  -> participant and attachment views
  -> governance/redaction/retention views

Hexalith.Conversations.AppHost / deployment
  -> composes EventStore, Dapr components, Tenants, Parties, FrontComposer/admin surface
```

The aggregate should be `Conversation`, not `Message`, for the first implementation. EventStore actors already serialize writes per aggregate identity; a single conversation aggregate gives clean command ordering for append, redaction, retention, closure, and participant mutations. Split aggregates later only if measured stream length, hot conversations, or governance contention prove the single aggregate too coarse.

Trade-off: a single Conversation stream is simpler and more auditable, but hot or very long conversations can increase replay/projection cost. Mitigate through snapshots, projection-first reads, and explicit stream length monitoring.

Sources:

- Azure domain analysis: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis
- Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\product-brief-Hexalith.Conversations.md`

### Design Principles and Best Practices

The primary design principle is to keep the Conversations domain pure and the EventStore substrate contained. Domain code should define commands, events, state, and handlers; it should not know Redis, PostgreSQL, Cosmos DB, Kafka, Service Bus, Dapr YAML, SignalR groups, or EventStore storage keys. This matches EventStore's documented programming model: domain logic is `Handle(Command, State?) -> DomainResult`, while the runtime handles routing, persistence, snapshots, publication, and command status.

Design rules for Conversations:

- Use domain events as business facts, not CRUD deltas.
- Make events replay-sufficient. A projection rebuild must not call an LLM provider, upstream chatbot, or volatile external API to reconstruct conversation state.
- Treat rejection events as first-class audit facts when business rules refuse a command.
- Keep upstream module state referenced, not owned. Store Party, Project, Folder, and attachment IDs; resolve authoritative display/lifecycle state through upstream modules or projections.
- Keep sensitive free text out of command extensions, logs, metric dimensions, and error details.
- Define schema evolution rules before v1: additive fields, unsupported version behavior, upcaster boundary, and deprecation windows.
- Write architecture decision records for aggregate boundary, event naming/versioning, idempotency, redaction semantics, projection freshness, and tenant-safe errors.

The most important bounded-context decision: Conversations owns conversation history and governance state; it does not own tenant membership, Party identity, file binaries, project lifecycle, folder hierarchy, LLM orchestration, or chatbot UX.

Sources:

- Azure CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- `D:\Hexalith.Conversations\Hexalith.EventStore\README.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\getting-started\first-domain-service.md`

### Scalability and Performance Patterns

Use CQRS deliberately. The write side should optimize for correctness, auditability, tenant isolation, idempotency, and ordered append. The read side should optimize for the actual user/operator queries through materialized projections. Microsoft guidance notes that event sourcing is commonly paired with CQRS because event stores are usually not efficient query stores; this fits Conversations exactly.

Recommended projection set:

- `ConversationDetailProjection`: title, lifecycle, metadata, current governance state, freshness.
- `MessageTimelineProjection`: ordered visible messages with redaction state and attribution.
- `ConversationListProjection`: tenant-scoped listing by business context, recent activity, project/folder references.
- `ParticipantProjection`: participant roster and stable Party references.
- `AttachmentProjection`: file/folder attachment references only, no binaries.
- `GovernanceProjection`: retention, sensitive-data flags, redactions, audit handles.
- `OperationalProjection`: lag, rebuild state, last applied sequence/timestamp, blocked/degraded states.

Performance guidance:

- Never read the event stream directly for normal UI/API reads.
- Use EventStore snapshots to reduce aggregate rehydration cost.
- Use projection freshness metadata so clients can distinguish current, stale, rebuilding, unavailable, and hidden-by-policy states.
- Use `If-None-Match`/ETag support where the EventStore query API supports it.
- Treat SignalR as an invalidation hint only; clients re-query projections.
- Benchmark append-message latency in separate stages: accepted, persisted, published, projection visible.
- Monitor stream length and event size. Conversation messages can grow large; define payload size and attachment-reference policies early.

The cache-aside pattern may apply to expensive read models, but cache invalidation must be event/projection driven and tenant-aware. Do not cache raw conversation content globally or by untrusted identifiers.

Sources:

- Azure CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- Azure Cache-Aside pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\query-api.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\configuration-reference.md`

### Integration and Communication Patterns

Architecturally, EventStore is the internal command/event spine; Conversations is the domain module on top. This gives a clean separation:

- Adopters talk to Conversations contracts/client.
- Conversations maps commands/queries to EventStore.
- EventStore routes commands to the Conversation aggregate actor.
- The aggregate emits events.
- EventStore persists and publishes events.
- Projections update read models.
- FrontComposer/admin surfaces read projections and submit governance commands.

For cross-module interoperability:

- Tenants: local projection, fail closed before aggregate/projection access.
- Parties: stable Party references for humans, AI agents, and LLMs.
- Projects/Folders: stable links and attachment references, no ownership of upstream lifecycle.
- FrontComposer: projection/command metadata to compose admin views.
- External adopters: client package and conformance tests, not raw EventStore knowledge.

Dapr resiliency should handle service invocation/pub-sub retries, timeouts, and circuit breakers at the sidecar/configuration level. Domain handlers should stay deterministic and retry-safe, not embed infrastructure retry loops.

Sources:

- Dapr resiliency: https://docs.dapr.io/concepts/resiliency-concept/
- Dapr service invocation: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\dapr-component-reference.md`
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md`

### Security Architecture Patterns

Conversations is a high-risk API surface because object identifiers are tenant-scoped and content is sensitive. OWASP's API Security Top 10 2023 identifies authorization as the largest API challenge and includes broken object-level authorization as the top risk. For Conversations, that maps directly to conversation ID guessing, cross-tenant timeline access, projection enumeration, and governance-action misuse.

Security architecture:

- Require authentication and tenant context at every public entry point.
- Authorize before aggregate access, projection access, replay, rebuild, search, export, admin operation, or diagnostic lookup.
- Treat unauthorized, nonexistent, and cross-tenant conversations as indistinguishable to non-privileged callers unless policy explicitly allows disclosure.
- Keep EventStore's six-layer security model, then add Conversations module-level authorization for Party role, governance privilege, and tenant projection freshness.
- Use content-safe RFC 9457 problem details: stable error codes and documentation pointers, no inaccessible identifiers or sensitive snippets.
- Make redaction architectural, not cosmetic. Redacted content must not reappear in projections, caches, logs, traces, errors, exports, or temporal views where policy says it is hidden.
- Keep privileged governance actions paired with audit events and rationale.

EventStore already enforces JWT auth, claims transformation, endpoint authorization, MediatR authorization, actor tenant validation before state rehydration, and Dapr access control. Conversations should consume those layers but not assume they replace module-specific tenant/Party/governance checks.

Sources:

- OWASP API Security Top 10 2023: https://owasp.org/blog/2023/07/03/owasp-api-top10-2023
- OWASP API Security Project: https://owasp.org/www-project-api-security/
- RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\security-model.md`
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md`

### Data Architecture Patterns

Use event streams for durable history and projections for query state. Do not create a parallel mutable transcript store as the system of record. Conversations data architecture should be append-first:

- Command: user's intent to change the conversation.
- Event: immutable fact about what changed or why a command was rejected.
- State: aggregate state reconstructed for validation.
- Projection: query-optimized materialized view.
- Audit/evidence: governance-oriented projection or paired event record.

Recommended event stream identity:

```text
tenant = {tenantId}
domain = conversation or conversations
aggregateId = {conversationId}
actor id = {tenantId}:{domain}:{conversationId}
topic = {tenantId}.{domain}.events
```

Recommended event design:

- Persist provider IDs as metadata/reference fields only, not as source-of-truth identity.
- Use stable message IDs so redaction and citation can target exact messages.
- Use sequence numbers/event positions for temporal evidence.
- Use event payload versions and domain service version metadata.
- Separate attachment references from attachment binaries.
- Use redaction events to change visibility/projection state while preserving immutable audit history.
- Define whether message content itself is stored in events, stored as protected payload, or stored via content reference with a governed content store. This is an architecture decision, not an implementation detail.

The hardest data decision is message content storage. EventStore supports payload protection hooks, and Conversations needs redaction/replay guarantees. If content is stored directly in event payloads, redaction must never be represented as deletion from the event stream; it must be projection/display/payload-protection policy. If content is stored by reference, rebuild must have durable access to the referenced protected content or a redacted substitute.

Sources:

- Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\event-envelope.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\identity-scheme.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\api\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.Security.IEventPayloadProtectionService.md`

### Deployment and Operations Architecture

Deployment architecture should mirror EventStore's Dapr/Aspire model:

- Local development: Aspire AppHost starts EventStore, Conversations domain service, Dapr sidecars, state store, pub/sub, config store, Tenants/Parties dependencies as needed.
- Test/integration: run through Aspire or equivalent containerized topology with fake or local Dapr components.
- Production: Dapr-enabled container platform such as Kubernetes or Azure Container Apps, with scoped state/pub-sub/config components and explicit access-control policies.

Operational architecture must include:

- Projection rebuild tooling and evidence that rebuilds are deterministic.
- Dead-letter monitoring and replay procedures.
- Event drain monitoring for publish failures.
- Snapshot interval configuration per domain, because conversations may be longer-lived than simple samples.
- Backup/restore validation for event streams, snapshots, command archives/status where relevant, projection stores, and tenant projection state.
- OpenTelemetry tracing/logging with bounded cardinality. Do not use conversation IDs, free text, provider payloads, or raw error strings as metric dimensions.
- Release gates for tenant isolation, idempotency, redaction replay, projection rebuild, unsupported schema versions, and content-safe errors.

Dapr component scoping is a real architectural control. EventStore docs note that only the `eventstore` app should access state store/pub-sub components; domain services should not access infrastructure directly. Conversations should preserve that boundary.

Sources:

- .NET Aspire AppHost: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview
- Dapr resiliency: https://docs.dapr.io/concepts/resiliency-concept/
- OpenTelemetry semantic conventions: https://opentelemetry.io/docs/concepts/semantic-conventions/
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\dapr-component-reference.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\configuration-reference.md`

### Architectural Decision Backlog

Before coding beyond the first vertical slice, capture these ADRs:

| ADR | Decision |
| --- | --- |
| ADR-001 | Domain name and aggregate identity: `conversation` vs `conversations`, conversation ID format, topic naming. |
| ADR-002 | Aggregate boundary: single Conversation aggregate for v1, split criteria for future hot streams. |
| ADR-003 | Idempotency contract: adopter token name, duplicate semantics, mapping to EventStore causation/message ID. |
| ADR-004 | Message content storage: event payload, protected payload, or content-reference pattern. |
| ADR-005 | Redaction semantics: immutable source events, projected visibility, audit evidence, temporal views. |
| ADR-006 | Projection freshness model: standard fields and caller behavior under stale/rebuilding/unavailable states. |
| ADR-007 | Tenant and Party authorization: local projections, fail-closed behavior, denial/error contract. |
| ADR-008 | Contract versioning: command/event/projection schema versioning, unsupported-version errors, upcaster boundary. |
| ADR-009 | Public API façade: raw EventStore fallback vs required Conversations client package for v1. |
| ADR-010 | Operational evidence: rebuild, redaction replay, dead-letter replay, audit pairing, release-gate artifacts. |

Recommended first architecture slice:

1. `ConversationAggregate` with `CreateConversation` and `AppendMessage`.
2. `ConversationCreated` and `MessageAppended` events.
3. Detail and timeline projections with freshness metadata.
4. Typed client happy path: create, append, poll status if needed, read timeline.
5. Tenant/Party authorization stubs wired to fail closed where dependencies are absent.
6. Unit tests with EventStore testing helpers plus one integration-style command-to-projection fixture.

---

## Implementation Approaches and Technology Adoption

### Web Search And Source Verification

Implementation research was verified against current Microsoft Cloud Adoption Framework guidance, Microsoft DevOps and ASP.NET Core testing documentation, DORA metrics guidance, current Dapr/EventStore docs, and local EventStore implementation/test code. The implementation strategy should be incremental and evidence-driven: build a narrow vertical slice first, prove command-to-event-to-projection behavior, then expand governance and cross-module integration behind conformance tests.

Sources:

- Microsoft Cloud Adoption Framework: https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/overview
- Microsoft cloud migration strategies: https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/adopt/cloud-adoption
- Microsoft DevOps overview: https://learn.microsoft.com/en-us/devops/what-is-devops
- DORA metrics: https://dora.dev/guides/dora-metrics/
- ASP.NET Core integration testing: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
- Testing ASP.NET Core services and web apps: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/test-aspnet-core-services-web-apps
- Local EventStore sample aggregate: `D:\Hexalith.Conversations\Hexalith.EventStore\samples\Hexalith.EventStore.Sample\Counter\CounterAggregate.cs`
- Local EventStore aggregate base: `D:\Hexalith.Conversations\Hexalith.EventStore\src\Hexalith.EventStore.Client\Aggregates\EventStoreAggregate.cs`
- Local EventStore testing helpers: `D:\Hexalith.Conversations\Hexalith.EventStore\src\Hexalith.EventStore.Testing`

Confidence: high for implementation patterns that follow EventStore samples and package APIs. Confidence: medium for project naming and exact layering because Conversations runtime projects have not yet been created.

### Technology Adoption Strategies

Adopt EventStore through a thin vertical slice, not a full platform build. Microsoft Cloud Adoption Framework guidance emphasizes planning, readiness, adoption, governance, security, and management as iterative concerns; for this module, that maps well to a staged adoption path:

1. **Foundation slice**: create contracts, aggregate, two commands, two events, two projections, and unit tests.
2. **EventStore runtime slice**: register the aggregate with `AddEventStore()` / `UseEventStore()`, run through an AppHost, and prove command-to-event persistence.
3. **Read model slice**: add detail/timeline projections and query through a typed client.
4. **Security slice**: wire tenant and Party checks, fail-closed behavior, and content-safe errors.
5. **Governance slice**: add retention, sensitivity, and redaction events after content-storage and redaction ADRs are accepted.
6. **Adopter slice**: chatbot integration and a conformance pack proving create/append/read behavior.

Do not migrate the chatbot by replacing all transcript storage in one step. Add Conversations as a parallel capability, run the create/append/read loop with seeded or mirrored traffic, then cut over once idempotency, redaction, projection freshness, and tenant isolation gates are passing.

Source: Microsoft Cloud Adoption Framework: https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/overview

### Development Workflows and Tooling

Use the existing Hexalith/EventStore .NET workflow:

- `net10.0`, nullable enabled, warnings as errors.
- Central package management through `Directory.Packages.props`.
- C# records for commands/events/projections, classes for mutable replay state.
- `AddEventStore()` assembly scanning for aggregates/projections.
- `UseEventStore()` activation validation at host startup.
- xUnit v3/Shouldly/NSubstitute for tests, matching EventStore conventions.
- Aspire AppHost for local multi-service orchestration.
- Swagger/OpenAPI for command/query smoke testing and typed client generation.

Suggested project layout:

```text
src/
  Hexalith.Conversations.Contracts/
  Hexalith.Conversations/
  Hexalith.Conversations.Client/
  Hexalith.Conversations.Projections/
  Hexalith.Conversations.Server/
  Hexalith.Conversations.Aspire/
  Hexalith.Conversations.ServiceDefaults/
tests/
  Hexalith.Conversations.Contracts.Tests/
  Hexalith.Conversations.Tests/
  Hexalith.Conversations.Client.Tests/
  Hexalith.Conversations.Projections.Tests/
  Hexalith.Conversations.IntegrationTests/
samples/
  Hexalith.Conversations.Sample/
```

Minimum first contracts:

- Commands: `CreateConversation`, `AppendMessage`.
- Events: `ConversationCreated`, `MessageAppended`.
- Rejections: `ConversationAlreadyExists`, `ConversationNotFound`, `ConversationClosed`, `ParticipantNotAllowed`, `UnsupportedSchemaVersion`.
- Projections: `ConversationDetail`, `MessageTimeline`, `ConversationListItem`.

Source: Microsoft DevOps overview: https://learn.microsoft.com/en-us/devops/what-is-devops

### Testing and Quality Assurance

Test from the inside out:

1. **Aggregate unit tests**: instantiate `ConversationAggregate` as `IDomainProcessor`, build `CommandEnvelope` payloads, call `ProcessAsync`, assert `DomainResult`.
2. **Replay tests**: apply event sequences to `ConversationState` and prove the state is deterministic.
3. **Projection tests**: feed event DTOs to projection handlers and assert read models, freshness metadata, redaction visibility, and duplicate tolerance.
4. **Registration tests**: verify `AddEventStore(typeof(ConversationAggregate).Assembly)` discovers keyed processor/domain activation.
5. **Security tests**: tenant mismatch, unknown tenant, disabled tenant, missing Party, insufficient role, cross-tenant ID guessing, and content-safe error responses.
6. **Conformance tests**: adopter-style create -> append -> read timeline happy path, plus duplicate command and projection rebuild fixtures.
7. **Integration tests**: Aspire/AppHost or WebApplicationFactory-based tests for API behavior where the HTTP boundary exists.

Local EventStore sample tests show the pattern: serialize a typed command into a `CommandEnvelope`, process it through the aggregate, and assert emitted event types. The `CommandEnvelopeBuilder`, `DomainResultAssertions`, fake event publisher, fake aggregate actor, and fake projection actors should be reused rather than re-created.

Source: ASP.NET Core integration testing: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0

### Deployment and Operations Practices

Deployment should follow the EventStore topology:

- Local: Aspire AppHost with EventStore, Conversations domain service, Dapr sidecars, Redis-backed components, and local auth.
- CI: unit and contract tests first; integration tests with containerized dependencies for release gates.
- Staging: Dapr components configured with production-like state/pub-sub backends and access control.
- Production: Dapr-enabled container platform with scoped state stores, pub/sub, config store, dead-letter routes, OpenTelemetry, and backup/restore runbooks.

Operational practices:

- Monitor command states: accepted, completed, rejected, publish failed, failed.
- Monitor projection lag, rebuild progress, and last applied event sequence.
- Monitor dead-letter topics and EventStore drain recovery.
- Track tenant-denial counts and governance rejection reasons without leaking content.
- Run operational drills for Dapr sidecar restart, state store degradation, pub/sub outage, dead-letter replay, projection rebuild crash/resume, and redaction replay.
- Define RPO/RTO separately for event streams, projections, tenant projection state, and audit evidence.

Source: Dapr resiliency policies: https://docs.dapr.io/concepts/resiliency-concept/

### Team Organization and Skills

The first implementation needs a small cross-functional team:

- Domain engineer: commands, events, aggregate state, projection logic.
- Platform/EventStore engineer: AppHost, Dapr components, EventStore registration, command/query adapter.
- Security/governance engineer: tenant/Party authorization, redaction, content-safe errors, audit requirements.
- Test architect: conformance tests, replay/rebuild tests, failure-mode fixtures.
- Adopter engineer: chatbot/client integration and developer-experience feedback.

Required skills:

- C#/.NET 10 and ASP.NET Core.
- Event sourcing, CQRS, aggregate design, projection rebuilds.
- Dapr state/pub-sub/actors/service invocation.
- .NET Aspire local orchestration.
- OpenTelemetry and content-safe observability.
- Tenant isolation and API authorization testing.

Source: Microsoft DevOps overview: https://learn.microsoft.com/en-us/devops/what-is-devops

### Cost Optimization and Resource Management

The main cost drivers are event payload size, projection write amplification, state store backend choice, pub/sub volume, and rebuild frequency. Conversations can become storage-heavy because message content, provider metadata, attachments, and governance records accumulate over time.

Cost controls:

- Store attachment references, not binaries.
- Decide early whether message content lives in event payloads, protected payloads, or a governed content-reference store.
- Enforce EventStore event size limits and request body limits.
- Keep projections purpose-specific; do not materialize every possible admin query in v1.
- Use snapshots to reduce rehydration cost but avoid very low snapshot intervals that increase write amplification.
- Track storage growth per conversation, event count per conversation, projection write count per command, and pub/sub delivery volume.

Source: EventStore configuration reference: `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\configuration-reference.md`

### Risk Assessment and Mitigation

| Risk | Impact | Mitigation |
| --- | --- | --- |
| EventStore idempotency semantics unclear | Duplicate message append or inconsistent adopter expectations | Define Conversations idempotency contract and test against installed EventStore behavior before coding broad commands. |
| Conversation streams grow too large | Slow command rehydration and projection rebuilds | Snapshot policy, projection-first reads, stream length monitoring, later split criteria. |
| Redaction modeled incorrectly | Sensitive content leaks through replay, projections, logs, or temporal views | ADR before implementation; redaction replay tests are release blockers. |
| Tenant projection stale or missing | Cross-tenant access or false allow | Fail closed before aggregate/projection access; test stale/missing projection cases. |
| Public client leaks EventStore internals | Poor adoption and brittle consumers | Typed Conversations client hides command envelopes, status polling, and projection mechanics. |
| Pub/sub duplicate/reordered delivery | Divergent projections | Idempotent projection handlers and replay tests. |
| Dapr component drift between environments | Works locally but fails in staging/production | Component config review, AppHost parity, deployment smoke tests, access-control tests. |

Source: DORA metrics and resilience framing: https://dora.dev/guides/dora-metrics/

## Technical Research Recommendations

### Implementation Roadmap

**Phase 0: ADRs and skeleton**

- Decide domain name, aggregate ID format, idempotency token, content storage, redaction semantics, projection freshness, and versioning.
- Create project skeletons and package references.
- Add build/test CI.

**Phase 1: First vertical slice**

- Implement `CreateConversation` / `AppendMessage`.
- Implement `ConversationCreated` / `MessageAppended`.
- Implement `ConversationState`.
- Implement detail and timeline projection handlers.
- Add aggregate, replay, projection, and registration tests.

**Phase 2: Client and adopter path**

- Implement typed Conversations client.
- Provide create/append/read happy path.
- Add command status and typed error mapping.
- Add chatbot sample or fixture.

**Phase 3: Tenant/Party enforcement**

- Wire Tenants local projection and Party identity validation.
- Add fail-closed checks before commands/queries/rebuilds.
- Add adversarial cross-tenant tests.

**Phase 4: Governance**

- Add retention, sensitivity, and redaction commands/events.
- Add redaction replay and audit pairing tests.
- Add operator projections.

**Phase 5: Operational readiness**

- Add dead-letter/replay runbooks, rebuild evidence, telemetry dashboards, and release-gate conformance manifest.

### Technology Stack Recommendations

- Runtime: C#/.NET 10.
- Persistence/eventing: Hexalith.EventStore via Dapr state, actors, pub/sub.
- Local orchestration: .NET Aspire.
- Public developer surface: `Hexalith.Conversations.Contracts` and `Hexalith.Conversations.Client`.
- Testing: xUnit v3, Shouldly, NSubstitute, Hexalith.EventStore.Testing.
- Observability: OpenTelemetry and structured logs with bounded, content-safe dimensions.

### Skill Development Requirements

The team should invest in:

- EventStore aggregate handler and projection patterns.
- Dapr component scoping, pub/sub behavior, actors, and resiliency policies.
- Tenant-safe API design and broken object-level authorization prevention.
- Replay/rebuild testing for event-sourced systems.
- Redaction and audit design for immutable event streams.

### Success Metrics and KPIs

Delivery metrics:

- Lead time for change.
- Deployment frequency.
- Change failure rate.
- Failed deployment recovery time.

Domain/platform metrics:

- Append-message accepted/persisted/published/projection-visible latency.
- Projection lag by tenant/projection type.
- Dead-letter count and age.
- Event publish failure count.
- Duplicate command handling correctness.
- Cross-tenant denial correctness.
- Redaction replay pass rate.
- Projection rebuild determinism pass rate.
- Adopter happy-path integration time.

Sources:

- DORA metrics: https://dora.dev/guides/dora-metrics/
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\tests\Hexalith.EventStore.Sample.Tests\Counter\CounterAggregateTests.cs`
- `D:\Hexalith.Conversations\Hexalith.EventStore\src\Hexalith.EventStore.Testing`

---

## Research Synthesis: Comprehensive Hexalith.EventStore Usage for Hexalith.Conversations

### Executive Summary

`Hexalith.Conversations` should be implemented as a tenant-isolated, event-sourced bounded context built on `Hexalith.EventStore`. The module should not invent a parallel transcript database, and it should not require adopters to understand EventStore envelopes, snapshots, actor IDs, pub/sub topics, or projection internals. Instead, EventStore should be the internal write/read substrate, while Conversations publishes typed contracts, a typed client package, module-specific projections, and conformance tests.

The recommended first implementation slice is intentionally narrow: `CreateConversation`, `AppendMessage`, `GetConversation`, and `ListConversations`, backed by `ConversationCreated` and `MessageAppended` events plus detail/timeline/list projections. This proves the core command-to-event-to-projection path before adding governance-heavy capabilities such as retention, sensitivity marking, and redaction. The most important pre-code decisions are idempotency semantics, message content storage, redaction replay behavior, tenant/Party authorization, projection freshness, and schema versioning.

Strategically, EventStore fits Conversations because the domain needs replayable history, auditability, provider-independent continuity, tenant-isolated publication, and rebuildable projections. The risk is not whether EventStore is the right substrate; the risk is leaking substrate complexity to adopters or under-specifying security/governance behavior in a content-sensitive API.

**Key Technical Findings**

- EventStore is the correct persistence path for Conversations aggregate state and domain events.
- Conversations needs a typed contracts/client boundary so adopters do not use raw EventStore command/query mechanics.
- A single `ConversationAggregate` is the right v1 consistency boundary; split only after measured stream-length or hot-aggregate pressure.
- Reads should use materialized projections, not direct event-stream reads.
- Tenant and object-level authorization must run before aggregate, projection, replay, rebuild, export, admin, or diagnostic access.
- Redaction and message-content storage are architecture decisions, not implementation details.
- Idempotency must be explicitly defined by Conversations because local EventStore documentation uses mixed wording around command submission idempotency.

**Top Recommendations**

1. Implement `Hexalith.Conversations.Contracts`, `Hexalith.Conversations.Client`, domain aggregate, projections, and testing packages as separate roles.
2. Use `ConversationAggregate : EventStoreAggregate<ConversationState>` with static `Handle(...)` methods and replayable `Apply(...)` state methods.
3. Define ADRs for aggregate identity, idempotency, content storage, redaction semantics, projection freshness, tenant/Party authorization, and schema versioning before expanding beyond the first vertical slice.
4. Build conformance tests early: duplicate command, projection rebuild, tenant isolation, content-safe errors, redaction replay, unsupported schema version.
5. Treat SignalR/projection notifications as refresh hints only; the Query API/projections remain the authoritative read model.

### Table of Contents

1. Research Introduction and Methodology
2. Technical Landscape and Architecture
3. Technology Stack
4. Integration and Interoperability
5. Performance and Scalability
6. Security and Compliance
7. Implementation Roadmap
8. Risk Assessment
9. Success Metrics
10. Source Verification

### 1. Research Introduction and Methodology

This research matters now because Conversations is planned as the durable memory and audit layer for AI-assisted work across Hexalith. The product brief and PRD make EventStore persistence, tenant isolation, pub/sub publication, projection rebuild, redaction, and adopter ergonomics core requirements rather than optional infrastructure choices.

The methodology combined:

- Local source inspection of `Hexalith.EventStore`, especially aggregate, registration, command lifecycle, query/projection, envelope, identity, security, configuration, and testing helpers.
- Local planning review of `Hexalith.Conversations` product brief, distillate, PRD, and adjacent Tenants/Parties/FrontComposer research artifacts.
- Current public-source verification using official Dapr, Microsoft Learn, CNCF, OWASP, OpenAPI, RFC, OpenTelemetry, and DORA sources.
- Pattern synthesis across CQRS, event sourcing, Dapr actors/pub-sub/state, tenant authorization, materialized views, and DevOps adoption.

Primary current external sources include Microsoft Azure Architecture Center for CQRS and Event Sourcing, CNCF for Dapr maturity, Dapr docs for sidecar/building-block behavior, OWASP API Security Top 10 2023 for object-level authorization risk, OpenAPI for API contract description, RFC 9457 for HTTP error envelopes, and DORA for delivery metrics.

Sources:

- Azure Event Sourcing: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- Azure CQRS: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Dapr CNCF project: https://www.cncf.io/projects/dapr/
- Dapr sidecar overview: https://docs.dapr.io/concepts/dapr-services/sidecar/
- OWASP API Top 10 2023: https://owasp.org/API-Security/editions/2023/en/0x11-t10/

### 2. Technical Landscape and Architecture

The dominant architecture should be event-sourced CQRS inside the Conversations bounded context:

- Commands express intent.
- The Conversation aggregate validates intent against replayed state.
- Domain events record what happened or why a command was rejected.
- EventStore persists immutable envelopes and publishes events.
- Projections materialize user, adopter, and operator read models.
- Clients read projections through a Conversations client/API, not through event streams.

This aligns with Azure Architecture Center guidance that event sourcing is commonly combined with CQRS and materialized views because event stores are not normally optimized for arbitrary queries. It also aligns with EventStore's own programming model: domain logic is pure command/state-to-domain-result code, while EventStore owns routing, persistence, snapshots, publication, and status.

Recommended module architecture:

| Layer | Responsibility |
| --- | --- |
| `Hexalith.Conversations.Contracts` | Commands, events, projections, typed errors, schema versions. |
| `Hexalith.Conversations` / Domain | `ConversationAggregate`, `ConversationState`, domain validation. |
| `Hexalith.Conversations.Projections` | Detail, timeline, list, participant, attachment, governance, operational projections. |
| `Hexalith.Conversations.Client` | Adopter-friendly create/append/read/list API and typed error/status mapping. |
| `Hexalith.Conversations.Server` | HTTP/module façade if needed, EventStore adapter, auth boundary. |
| `Hexalith.Conversations.Aspire` | Local topology composition with EventStore and Dapr. |
| Tests/conformance | Aggregate, replay, projection, tenant, redaction, idempotency, integration fixtures. |

### 3. Technology Stack

The recommended stack is:

- C# / .NET 10, matching EventStore and sibling Hexalith modules.
- `Hexalith.EventStore.Contracts` in Conversations contracts.
- `Hexalith.EventStore.Client` in the domain service for `EventStoreAggregate<TState>`, `AddEventStore()`, and `UseEventStore()`.
- `Hexalith.EventStore.Server` in the host/gateway layer that owns actors, command routing, persistence, and pub/sub.
- `Hexalith.EventStore.Testing` in tests.
- `Hexalith.EventStore.Aspire` in local AppHost/orchestration.
- Dapr state/pub-sub/actors/service invocation/configuration as EventStore's infrastructure abstraction.
- OpenTelemetry for tracing/logging/metrics, with content-safe dimensions.

Do not add direct database, broker, or provider SDK dependencies to the Conversation aggregate. Infrastructure portability depends on domain code staying Dapr-free and database-free.

### 4. Integration and Interoperability

Recommended command flow:

```text
Adopter / Chatbot
  -> Hexalith.Conversations.Client
  -> Conversations API/facade or internal adapter
  -> EventStore Command API
  -> Dapr AggregateActor for tenant:conversation:conversationId
  -> ConversationAggregate.Handle(command, state)
  -> EventStore persists envelopes
  -> EventStore publishes Dapr CloudEvents
  -> Projection handlers update read models
  -> Conversations client reads projections
```

Cross-module integration:

- Tenants: local projection, fail closed on missing/stale/unknown tenant state.
- Parties: stable Party IDs for humans, AI agents, and LLMs.
- Projects/Folders: stable references only; no ownership of upstream lifecycle or file binaries.
- FrontComposer: command/projection metadata for generated/admin views.
- Chatbot/adopters: typed client and conformance fixtures.

The public developer experience should be:

```csharp
await conversations.CreateConversationAsync(...);
await conversations.AppendMessageAsync(...);
ConversationTimeline timeline = await conversations.GetTimelineAsync(...);
```

not:

```json
{
  "tenant": "...",
  "domain": "...",
  "aggregateId": "...",
  "commandType": "...",
  "payload": {}
}
```

The latter remains an internal EventStore substrate shape or an emergency/raw fallback, not the v1 adoption path.

### 5. Performance and Scalability

The first performance rule is to keep reads off the write stream. Conversations should expose projections for timeline, detail, list, participant, attachment, governance, and operational state. Projection freshness should be explicit, with fields such as `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration`, or a documented equivalent.

Important performance tactics:

- Configure EventStore snapshots for long-running Conversation streams.
- Benchmark append-message as separate stages: accepted, persisted, published, projection-visible.
- Track event count per conversation and projection write amplification.
- Avoid storing attachment binaries in Conversations.
- Enforce payload/request limits and avoid unbounded extension metadata.
- Keep SignalR as invalidation only; clients re-query projections with ETag support where available.

Scaling risks should be measured before splitting the aggregate. The single aggregate model is the best starting point because it preserves command ordering and audit semantics. Split only with evidence.

### 6. Security and Compliance

Conversations is a sensitive object-level API: a caller can guess or replay tenant/conversation/message identifiers. OWASP API Security Top 10 2023 identifies broken object-level authorization as the leading API risk category. For Conversations, that risk maps to cross-tenant timeline reads, command submission against another tenant's conversation, projection enumeration, governance misuse, and content leakage through errors/logs/traces.

Security architecture requirements:

- Authorize before aggregate access, projection access, replay, rebuild, search, export, diagnostics, or admin mutation.
- Make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged callers unless policy explicitly permits disclosure.
- Use typed RFC 9457-style errors without leaking tenant IDs, Party IDs, conversation existence, provider payloads, prompts, or redacted content.
- Treat redaction as a replay and projection problem, not a UI-only behavior.
- Keep governance mutations paired with audit events and rationale.
- Keep content out of logs, metric dimensions, traces, command extensions, and raw error strings.

EventStore already supplies multiple defense layers: JWT authentication, claims transformation, endpoint authorization, MediatR authorization, actor tenant validation before state rehydration, and Dapr access control. Conversations must add module-specific tenant projection checks, Party attribution, governance permission checks, and content-safe error behavior.

### 7. Implementation Roadmap

**Phase 0: Decisions and Skeleton**

- ADRs: domain name, aggregate identity, idempotency, content storage, redaction, projection freshness, tenant/Party auth, versioning.
- Create project skeletons and CI.
- Add package references and central package versions.

**Phase 1: First Vertical Slice**

- `CreateConversation`, `AppendMessage`.
- `ConversationCreated`, `MessageAppended`.
- `ConversationAggregate`, `ConversationState`.
- Detail/timeline projections.
- Aggregate, replay, projection, and registration tests.

**Phase 2: Client and Adopter Experience**

- Typed Conversations client.
- Command status and typed error mapping.
- Happy-path sample: create -> append -> read.
- Raw EventStore API hidden from normal adopters.

**Phase 3: Tenant/Party Enforcement**

- Tenants projection and fail-closed checks.
- Party attribution and participant validation.
- Cross-tenant and missing/stale projection tests.

**Phase 4: Governance**

- Retention, sensitivity, redaction commands/events.
- Redaction replay tests.
- Audit pairing and operator projections.

**Phase 5: Operational Readiness**

- Dead-letter and drain runbooks.
- Projection rebuild evidence.
- Telemetry dashboards.
- Conformance manifest and release gates.

### 8. Risk Assessment

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Idempotency semantics are unclear | Duplicate messages or adopter confusion | Define Conversations idempotency contract and test installed EventStore behavior. |
| EventStore internals leak to adopters | Brittle client integrations | Typed client and docs with raw fallback explicitly secondary. |
| Redaction is under-modeled | Sensitive data reappears in replay/projections/logs | ADR plus redaction replay conformance tests before governance release. |
| Tenant projection is stale/missing | Cross-tenant access or false authorization | Fail closed before aggregate/projection access; test stale/missing cases. |
| Single aggregate becomes hot | Latency/rebuild pressure | Snapshot, projection-first reads, stream monitoring, split criteria ADR. |
| Pub/sub duplicates/reordering | Divergent projections | Idempotent projection handlers and deterministic rebuild tests. |
| Dapr component drift | Local/staging/production mismatch | AppHost parity, component config review, smoke tests, access-control checks. |
| Content storage choice is late | Rework of events/projections/security | Decide protected payload vs content reference before redaction implementation. |

### 9. Success Metrics

Delivery metrics:

- Lead time for change.
- Deployment frequency.
- Change failure rate.
- Failed deployment recovery time.

Module metrics:

- Append-message accepted/persisted/published/projection-visible latency.
- Projection lag by tenant and projection type.
- Dead-letter count and age.
- Event publish failure count.
- Duplicate command correctness.
- Cross-tenant denial correctness.
- Projection rebuild determinism pass rate.
- Redaction replay pass rate.
- Adopter happy-path integration time.

Release-gate candidates:

- Tenant isolation tests pass.
- Idempotency tests pass.
- Projection rebuild tests pass.
- Redaction replay tests pass once governance is in scope.
- Unsupported schema version behavior is typed and documented.
- Error/log/metric content-safety tests pass.

### 10. Source Verification

Primary local sources:

- `D:\Hexalith.Conversations\Hexalith.EventStore\README.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\architecture-overview.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\command-lifecycle.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\event-envelope.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\concepts\identity-scheme.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\command-api.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\query-api.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\reference\nuget-packages.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\security-model.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\docs\guides\configuration-reference.md`
- `D:\Hexalith.Conversations\Hexalith.EventStore\src\Hexalith.EventStore.Client\Aggregates\EventStoreAggregate.cs`
- `D:\Hexalith.Conversations\Hexalith.EventStore\tests\Hexalith.EventStore.Sample.Tests\Counter\CounterAggregateTests.cs`
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\product-brief-Hexalith.Conversations.md`
- `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md`

Primary public sources:

- Hexalith.EventStore public repo: https://github.com/Hexalith/Hexalith.EventStore
- Azure Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- Azure CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Azure domain analysis: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis
- Dapr building blocks: https://docs.dapr.io/concepts/building-blocks-concept/
- Dapr state management: https://docs.dapr.io/developing-applications/building-blocks/state-management/
- Dapr actors: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/
- Dapr pub/sub API: https://docs.dapr.io/reference/api/pubsub_api/
- Dapr service invocation: https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/
- Dapr CNCF project: https://www.cncf.io/projects/dapr/
- .NET Aspire AppHost: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview
- OpenAPI 3.1.1: https://spec.openapis.org/oas/v3.1.1.html
- RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457
- OAuth2 Bearer Tokens RFC 6750: https://www.rfc-editor.org/rfc/rfc6750
- OWASP API Security Top 10 2023: https://owasp.org/API-Security/editions/2023/en/0x11-t10/
- DORA metrics: https://dora.dev/guides/dora-metrics/
- ASP.NET Core integration testing: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0

### Conclusion

`Hexalith.EventStore` should be the authoritative event-sourcing substrate for `Hexalith.Conversations`. The implementation should begin with a narrow, tested vertical slice that proves create/append/read behavior end to end, then add tenant/Party enforcement and governance features behind explicit ADRs and release-gate tests.

The strongest architectural posture is: EventStore inside, Conversations contracts outside. That keeps persistence, routing, snapshots, pub/sub, and projection invalidation in the substrate, while keeping adopters focused on the domain they actually care about: durable, tenant-safe, auditable conversations.

**Technical Research Completion Date:** 2026-05-10  
**Technical Confidence Level:** High for EventStore usage pattern; medium for exact implementation packaging until runtime projects are created.  
**Recommended Next Action:** create ADRs 001-010, then implement the first vertical slice with aggregate/replay/projection/conformance tests.

---

<!-- Content will be appended sequentially through research workflow steps -->
