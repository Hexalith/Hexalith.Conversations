---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 1
research_type: 'technical'
research_topic: 'How to use Hexalith.Tenants to manage tenant isolation in Hexalith.Conversations'
research_goals: 'Determine the architecture, integration points, event/projection flow, enforcement rules, tests, and operational safeguards needed for Hexalith.Conversations to consume Hexalith.Tenants for tenant isolation.'
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

This research evaluates how `Hexalith.Conversations` should use `Hexalith.Tenants` to enforce tenant isolation across commands, queries, projections, tools, administration, and operations. The analysis combines local source review of `Hexalith.Tenants`, `Hexalith.Parties`, tests, Dapr deployment assets, and PRD requirements with current external guidance for Dapr pub/sub, ASP.NET Core authorization, CQRS, event sourcing, ProblemDetails, Zero Trust, and API object-level authorization.

The central conclusion is that `Hexalith.Tenants` should remain the tenant source of truth, while `Hexalith.Conversations` owns a local, fail-closed tenant access boundary. Conversations should consume tenant lifecycle and membership events from `system.tenants.events`, maintain a durable tenant projection, and verify requested tenant and user context before loading aggregates, dispatching commands, reading projections, invoking tools, or running administrative operations.

The full synthesis at the end of this document turns the step-by-step findings into a recommended architecture, implementation roadmap, test strategy, operational readiness model, risk register, and ADR backlog.

---

## Technical Research Scope Confirmation

**Research Topic:** How to use Hexalith.Tenants to manage tenant isolation in Hexalith.Conversations
**Research Goals:** Determine the architecture, integration points, event/projection flow, enforcement rules, tests, and operational safeguards needed for Hexalith.Conversations to consume Hexalith.Tenants for tenant isolation.

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

### Programming Languages

Hexalith.Conversations should use the same .NET/C# stack already used by `Hexalith.Tenants` and adjacent modules. `Hexalith.Tenants` targets `net10.0` in `Hexalith.Tenants/Directory.Build.props`, and Microsoft lists .NET 10 as an LTS release supported until November 2028. This makes `net10.0` the right baseline for a new Conversations module in this workspace, provided the deployment environment is patched monthly.

The implementation language should be C# with nullable reference types and the existing Hexalith style: records for contracts, small service abstractions for authorization boundaries, and explicit enum mapping for role decisions. Tenant authorization should not be spread through controllers as raw claim checks; it should be centralized behind a Conversations-owned `ITenantAccessService` equivalent.

_Popular Languages:_ C#/.NET for service code; YAML for Dapr components and subscription/access-control configuration.
_Emerging Languages:_ Not relevant for the module boundary; adopting another runtime would increase authorization drift.
_Language Evolution:_ .NET 10 LTS is current enough for a greenfield module while staying on a supported long-term track.
_Performance Characteristics:_ C#/.NET is suitable for low-latency command/query APIs; the tenant access path should be an in-process projection lookup, not a network call on every request.
_Sources:_ Microsoft .NET releases and support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support; local: `Hexalith.Tenants/Directory.Build.props`.

### Development Frameworks and Libraries

The Conversations service should register the Tenants client pipeline with `AddHexalithTenants(...)`. That extension registers `ITenantProjectionStore`, the built-in `TenantProjectionEventHandler`, handlers for tenant lifecycle/membership/configuration events, and `TenantEventProcessor` (`Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs:25`, `:85-103`). The existing `Hexalith.Parties` module already composes this pattern in `AddParties(...)`, then adds its own `ITenantAccessService` over the shared Tenants projection store (`Hexalith.Parties/src/Hexalith.Parties.CommandApi/Extensions/PartiesServiceCollectionExtensions.cs:74-82`).

For API authorization, ASP.NET Core policy/resource authorization remains useful at the HTTP edge, but tenant membership and role decisions should be evaluated by the Tenants-backed access service. Microsoft's policy-based authorization model is built around requirements, handlers, and `IAuthorizationService`, which is a good fit if Conversations later wants a formal `TenantAccessRequirement` handler rather than direct service calls.

_Major Frameworks:_ ASP.NET Core for REST/CommandApi endpoints; Dapr ASP.NET Core for CloudEvents subscriptions; Hexalith.EventStore for event-sourced command handling; Hexalith.Tenants client for tenant event projection.
_Micro-frameworks:_ A small Conversations-owned authorization abstraction mirroring Parties: `ITenantAccessService`, `TenantAccessDecision`, `TenantAccessRequirement`, and a denial translator.
_Evolution Trends:_ Keep tenant authorization as a reusable domain service, not ad hoc controller attributes, so REST, MCP/tools, projection rebuilds, and admin operations call the same path.
_Ecosystem Maturity:_ The local Tenants client already supplies the projection pipeline; Conversations mainly needs to wrap it with module-specific permissions.
_Sources:_ ASP.NET Core policy authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies; local: `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`.

### Database and Storage Technologies

Tenant isolation decisions in Conversations should be backed by a local projection of `Hexalith.Tenants` events. The default Tenants client store is in-memory (`InMemoryTenantProjectionStore`), but the Parties documentation explicitly warns that production deployments need replay/rebuild procedures or a durable `ITenantProjectionStore` because an in-memory store starts empty after process restart and therefore fails closed as `UnknownTenant` (`Hexalith.Parties/docs/tenant-access-projection.md:52`).

For production Conversations, implement or configure a durable `ITenantProjectionStore` before calling `AddHexalithTenants`, so the extension does not register the in-memory default. Candidate backends are the same Dapr state stores used elsewhere in Hexalith: PostgreSQL, Cosmos DB, Redis, or another supported Dapr state component. Dapr state management supports key/value state APIs, optional query APIs, ETags/optimistic concurrency depending on the store, and transactional outbox patterns where the backend supports them.

_Relational Databases:_ PostgreSQL is appropriate for durable tenant access projections when operators want SQL inspection and backup/replay tooling.
_NoSQL Databases:_ Cosmos DB is suitable if the deployment standard already uses it for Dapr state and partitioned tenant data.
_In-Memory Databases:_ The default in-memory projection store is acceptable for tests/local development only.
_Data Warehousing:_ Not in the authorization path; analytics must consume redacted, tenant-safe events separately.
_Sources:_ Dapr state management: https://docs.dapr.io/developing-applications/building-blocks/state-management/; Dapr state overview/outbox: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/; local: `Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs`.

### Development Tools and Platforms

Use the existing test style and Tenants/Parties test helpers. `Hexalith.Parties` already has tests proving fail-closed behavior for missing tenant/user, unknown tenant, disabled tenant, missing member, insufficient role, stale store failure, and event-driven revocation after `TenantDisabled` or `UserRemovedFromTenant` events. Conversations should lift that test matrix almost directly, then add conversation-specific checks for command routing, projection reads, snapshots/rebuilds, redaction/governance commands, and MCP/tool entry points.

For local development, the Tenants sample and Parties CommandApi both show the Dapr subscription wiring: `app.UseCloudEvents()`, `app.MapSubscribeHandler()`, and `app.MapTenantEventSubscription()` (`Hexalith.Parties/src/Hexalith.Parties.CommandApi/Program.cs:46-49`). Conversations should expose the same `/tenants/events` subscription endpoint through the Tenants client extension, using configured `Tenants:PubSubName` and `Tenants:TopicName`.

_IDE and Editors:_ Any .NET 10-capable IDE; no special requirement beyond SDK alignment.
_Version Control:_ Keep Tenants integration changes isolated in the Conversations module; do not fork tenant contracts.
_Build Systems:_ Existing MSBuild/Directory.Packages pattern, with package versions pinned through central package management.
_Testing Frameworks:_ xUnit v3, Shouldly, NSubstitute, plus module-level conformance tests driven by Tenants testing helpers.
_Sources:_ Local: `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Program.cs`; `Hexalith.Parties/tests/Hexalith.Parties.CommandApi.Tests/Authorization/TenantAccessServiceTests.cs`.

### Cloud Infrastructure and Deployment

Tenant event propagation should use Dapr pub/sub. The Tenants event contract says all tenant domain events are published as CloudEvents on `system.tenants.events`, and the Tenants client defaults match that topic (`Hexalith.Tenants/docs/event-contract-reference.md`; `Hexalith.Tenants/src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs:4-8`). Dapr documentation confirms pub/sub uses CloudEvents by default and delivers messages at least once; a subscriber that fails or returns a non-200 response can receive the event again. Therefore, Conversations must treat tenant-event handlers as idempotent and safe for duplicate delivery.

Deployment must also configure Dapr topic access so the Conversations CommandApi app id is allowed to subscribe to `system.tenants.events`, and must define a dead-letter/retry story for poisoned tenant events. Dapr dead-letter topics are designed for messages that cannot be delivered or processed after retries; this should be part of the operational runbook because stale tenant projection state is a security-relevant degradation, not a harmless background lag.

_Major Cloud Providers:_ Azure Container Apps or Kubernetes are natural fits if the rest of Hexalith runs Dapr sidecars there.
_Container Technologies:_ Dapr sidecars plus configured pub/sub/state components.
_Serverless Platforms:_ Not recommended for the core CommandApi if tenant projection warm state and subscription processing are required continuously.
_CDN and Edge Computing:_ Not relevant to tenant authorization; do not push tenant access decisions to edge caches.
_Sources:_ Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; Dapr publish/subscribe retries: https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/; Dapr dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/; local: `Hexalith.Tenants/docs/event-contract-reference.md`.

### Technology Adoption Trends

The existing Hexalith direction is clear: tenant lifecycle, membership, roles, and configuration remain owned by `Hexalith.Tenants`; consuming modules keep a local projection and fail closed. The Conversations product brief and PRD both commit to this: tenant access is enforced through local projections of Tenants events, and missing/stale/lagging/rolled-back state must fail closed (`_bmad-output/planning-artifacts/product-brief-Hexalith.Conversations.md:49`, `_bmad-output/planning-artifacts/prd.md:716`, `:1348-1349`).

The practical adoption pattern is: do not call Tenants synchronously for every command; do not trust a JWT tenant claim by itself; do not store tenant membership in Conversations domain state. Instead, trust the request tenant only as requested context, authorize it against the local Tenants projection immediately before any aggregate or projection access, then include the tenant id in every EventStore aggregate identity, projection key, and audit/correlation envelope.

_Migration Patterns:_ Reuse the Parties authorization/projection pattern first; extract a shared Hexalith tenant-access package later only after a second module proves identical needs.
_Emerging Technologies:_ Dapr's newer AI/conversation APIs are not relevant to tenant isolation; they should not displace the Hexalith.Conversations domain model.
_Legacy Technology:_ Raw tenant claims and per-controller role checks should be treated as legacy/unsafe for this module.
_Community Trends:_ Official Dapr guidance reinforces at-least-once, CloudEvents-based messaging; the local Hexalith pattern already accounts for duplicate delivery and fail-closed projection gaps.
_Sources:_ Local: `_bmad-output/planning-artifacts/product-brief-Hexalith.Conversations.md`; `_bmad-output/planning-artifacts/prd.md`; Dapr docs linked above.

### Stack Recommendation

For Hexalith.Conversations, use `Hexalith.Tenants` as the authorization source of truth through an event-fed local projection:

1. Reference `Hexalith.Tenants.Client` and register `AddHexalithTenants(options => configuration.GetSection("Tenants").Bind(options))`.
2. Map the Dapr subscription endpoint with `UseCloudEvents`, `MapSubscribeHandler`, and `MapTenantEventSubscription`.
3. Add a Conversations-owned `ITenantAccessService` modeled on Parties, backed by `ITenantProjectionStore`.
4. Require `Read`, `Write`, or `Admin/Governance` access before every conversation command, query, projection read, rebuild, MCP/tool invocation, and admin action.
5. Store tenant id as part of every Conversation aggregate/projection key and reject mismatches between route/header/claim/body/aggregate tenant.
6. Use a durable projection store in production; keep in-memory only for tests and local development.
7. Treat all tenant-event processing as at-least-once and eventually consistent; test duplicate, missing, stale, out-of-order, and poisoned events.

**Confidence:** High for the local integration pattern because `Hexalith.Parties` already implements it. Medium for the exact production store choice because it depends on the deployment target and EventStore state-store standard.

---

## Integration Patterns Analysis

### API Design Patterns

Conversations should expose tenant-scoped command/query APIs, but tenant authorization must be an internal enforcement layer rather than a public API convention. A caller may present a tenant id through a trusted request header/claim/route, yet the service must authorize that context against `Hexalith.Tenants` projection state before any EventStore command dispatch, actor/projection query, rebuild, or tool operation.

REST is sufficient for the CommandApi and read endpoints because the module already follows ASP.NET Core and Hexalith.EventStore conventions. GraphQL does not add value for tenant isolation and can make authorization harder because nested resolvers can accidentally cross tenant boundaries. gRPC can be considered later for internal clients, but the first integration should preserve the existing REST/ProblemDetails patterns already proven in Parties.

_RESTful APIs:_ Use route/header/claim tenant context as requested scope; validate access with `ITenantAccessService`; return RFC 9457 `application/problem+json` for denial, following the Parties translator pattern (`TenantAccessDenialTranslator.cs:40-60`).
_GraphQL APIs:_ Not recommended for MVP tenant isolation because every resolver would need the same fail-closed guard.
_RPC and gRPC:_ Optional future internal transport; authorization semantics must remain identical to REST.
_Webhook Patterns:_ Do not use webhooks for tenant state. Consume Tenants events through Dapr pub/sub using the Tenants client subscription pipeline.
_Sources:_ RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457; local: `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Authorization/TenantAccessDenialTranslator.cs`.

### Communication Protocols

The authoritative tenant-state integration is event-driven Dapr pub/sub. `Hexalith.Tenants` publishes tenant domain events to `system.tenants.events`; consuming services register `MapTenantEventSubscription()` and receive a `TenantEventEnvelope` with `MessageId`, `AggregateId`, `TenantId`, `EventTypeName`, `SequenceNumber`, `Timestamp`, `CorrelationId`, `SerializationFormat`, and `Payload` (`TenantEventEnvelope.cs:17-26`). The Tenants client converts `AggregateId` into the managed tenant id in `TenantEventContext`, so Conversations must not confuse the envelope `TenantId` value (`system`) with the tenant being authorized (`TenantEventContext.cs:6-16`).

Dapr pub/sub uses CloudEvents and at-least-once delivery. That confirms the local Hexalith design: deduplicate by event identity/message id, make handlers idempotent, and expect redelivery when an endpoint fails. Dapr topic scoping should also be configured so only the Tenants publisher can publish tenant events and only approved consumers, including Conversations CommandApi, can subscribe.

_HTTP/HTTPS Protocols:_ REST over HTTPS at the service edge; Dapr sidecar HTTP callbacks for `/tenants/events`.
_WebSocket Protocols:_ Not part of tenant isolation. If streaming conversation updates are added later, each subscription must first pass tenant access and then remain bound to one tenant.
_Message Queue Protocols:_ Use Dapr pub/sub abstraction rather than hard-coding Kafka/RabbitMQ/Service Bus in Conversations.
_gRPC and Protocol Buffers:_ Optional Dapr transport detail or future client surface; not required for MVP isolation.
_Sources:_ Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; Dapr publish/subscribe retries: https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/; Dapr topic scoping: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/; CloudEvents spec: https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md; local: `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventEnvelope.cs`.

### Data Formats and Standards

The tenant event payload format should remain the Tenants client format: EventStore envelope metadata plus JSON payload bytes deserialized by `EventTypeName`. Conversations should not define a second tenant event schema. Unknown event types should be skipped/logged as the Tenants client already does, while invalid payloads should fail processing so Dapr can retry and eventually dead-letter according to deployment configuration.

For error responses, Conversations should use `application/problem+json` with stable `type`, `status`, `title`, `detail`, and `instance` fields plus safe extensions such as `correlationId` and `reasonCode`. RFC 9457 explicitly supports machine-readable problem details and extension members that clients ignore when unrecognized. Never include raw tenant membership dictionaries, claimed tenant/body tenant pairs, prompt content, or cross-tenant target identifiers in public denial details.

_JSON and XML:_ JSON is the correct contract format for commands, projections, tenant event payloads, and ProblemDetails. XML is unnecessary.
_Protobuf and MessagePack:_ Defer until a measured performance need appears; changing serialization must not change tenant enforcement semantics.
_CSV and Flat Files:_ Not appropriate for tenant authorization. Bulk export/import must be tenant-scoped and separately authorized.
_Custom Data Formats:_ Avoid custom tenant event wrappers; use `TenantEventEnvelope` and Tenants contracts.
_Sources:_ RFC 9457: https://www.rfc-editor.org/rfc/rfc9457; CloudEvents required attributes: https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md; local: `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`.

### System Interoperability Approaches

Conversations should interoperate with `Hexalith.Tenants` by consuming events, not by owning tenant lifecycle or duplicating membership state in the conversation aggregate. The local projection is an enforcement cache, not a second authority. A tenant claim says "this is the requested tenant context"; the projection decides whether it is authorized.

For deployment, copy the Parties model: declarative subscription for `system.tenants.events` with dead-letter routing, and Dapr access-control/topic scoping that makes tenant topics protected. Parties currently documents a Tenants subscription using `pubsubname: pubsub`, topic `system.tenants.events`, route `/events/tenants`, dead-letter topic `deadletter.system.tenants.events`, and scope `commandapi` (`subscription-tenants.yaml:17-22`). Conversations can use `/tenants/events` from `MapTenantEventSubscription()` or a declarative route, but it should standardize one route and align the Dapr subscription file with the actual mapped endpoint.

_Point-to-Point Integration:_ Avoid synchronous Tenants checks on every request as the primary path; use only for administrative diagnostics or strong-consistency fallbacks if an architecture decision adds them.
_API Gateway Patterns:_ Gateway authentication may validate JWTs and tenant claims, but it cannot replace module-local Tenants membership authorization.
_Service Mesh:_ Useful for transport security/observability, but tenant access remains application-level.
_Enterprise Service Bus:_ Not needed; Dapr pub/sub is the existing abstraction.
_Sources:_ Dapr topic scoping: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/; local: `Hexalith.Parties/deploy/dapr/subscription-tenants.yaml`; `Hexalith.Parties/deploy/dapr/accesscontrol.yaml`.

### Microservices Integration Patterns

The recommended pattern is an event-fed local authorization projection plus a fail-closed access service. This is intentionally similar to a CQRS read model: Tenants emits lifecycle/membership/role/configuration events; Conversations consumes them into `ITenantProjectionStore`; command/query handlers consult `IConversationTenantAccessService`; only then do they load or write conversation aggregates/projections.

The failure path must be explicit. If the projection store throws, is empty after restart, is stale beyond an SLO, or contains an unknown role/status, the access service denies with a structured reason (`tenant-state-stale`, `unknown-tenant`, `tenant-disabled`, `not-member`, `insufficient-role`, etc.). Parties already proves this mapping in `TenantAccessService`: missing tenant/user, unknown tenant, disabled tenant, missing member, insufficient role, and projection-store failure all deny (`TenantAccessService.cs:21-73`).

_API Gateway Pattern:_ Use gateway authentication for identity, but enforce tenant authorization in Conversations.
_Service Discovery:_ Use existing Dapr/Aspire conventions for app ids and sidecars.
_Circuit Breaker Pattern:_ Apply to optional synchronous Tenants diagnostics only; the local access service should fail closed rather than retry user requests into latency spikes.
_Saga Pattern:_ Not needed for tenant access decisions. Tenant membership changes are eventually consumed as facts; they do not participate in Conversations transactions.
_Sources:_ ASP.NET Core policy authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies; local: `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Authorization/TenantAccessService.cs`.

### Event-Driven Integration

Tenants event handling in Conversations should be idempotent and replayable. `TenantEventProcessor` deduplicates by `MessageId`, resolves `EventTypeName`, deserializes the payload, creates `TenantEventContext`, and dispatches registered handlers (`TenantEventProcessor.cs:62-114`). Because Dapr delivery is at least once, durable deduplication should be added for production if tenant projection correctness after restarts matters. The in-memory processor cache is not enough for long-running production guarantees.

Out-of-order delivery is the main risk to call out. The Parties docs state the Tenants client deduplicates by `MessageId` only and does not enforce `SequenceNumber` ordering. For Conversations, a production `ITenantProjectionStore` should track the last applied `SequenceNumber` per managed tenant and either reject/park out-of-order events or rebuild from EventStore when a gap is detected. Without that, a late `UserRemovedFromTenant` or `TenantDisabled` can be overwritten by an older event and reopen access incorrectly.

_Publish-Subscribe Patterns:_ Tenants publishes; Conversations subscribes; consumers do not know each other directly.
_Event Sourcing:_ Tenants remains the source of truth; Conversations stores its own tenant-scoped conversation events separately.
_Message Broker Patterns:_ Broker choice remains behind Dapr; configure retries and dead-letter topics.
_CQRS Patterns:_ Tenant access projection is a local read model optimized for authorization.
_Sources:_ Dapr at-least-once behavior: https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/; Dapr dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/; local: `Hexalith.Parties/docs/tenant-access-projection.md`; `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`.

### Integration Security Patterns

Authentication and tenant authorization must remain separate. OAuth 2.0/JWT bearer tokens can authenticate the caller and carry a requested tenant claim, but bearer tokens are simply access tokens; they do not prove current Tenants membership by themselves. Conversations must validate token issuer/audience/signature at the edge, extract user id and requested tenant, then authorize that pair through the Tenants projection.

For agent/tool integrations, copy the Parties MCP pattern: capture tenant/user from the authenticated HTTP context into session context, call `ITenantAccessService.CheckAccessAsync(...)`, and throw/return a consistent tool-facing authorization error before any projection read or command route (`McpTenantAuthorization.cs:6-29`). Agent tools are especially sensitive because they may chain actions quickly; no tool should rely only on session tenant text.

_OAuth 2.0 and JWT:_ Use for authentication and user identity; do not treat tenant claims as sufficient authorization.
_API Key Management:_ Avoid API keys for tenant-scoped user operations; they are poor carriers for user/tenant membership.
_Mutual TLS:_ Useful between Dapr sidecars/services; not a replacement for tenant authorization.
_Data Encryption:_ Required platform concern, but isolation is primarily enforced by authorization, tenant-scoped keys/identities, and projection filtering.
_Sources:_ OAuth bearer tokens: https://oauth.net/2/bearer-tokens/; Dapr security/topic scoping: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/; local: `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Mcp/McpTenantAuthorization.cs`.

### Conversations Integration Blueprint

Recommended end-to-end flow:

1. Incoming request authenticates through existing JWT bearer configuration.
2. Conversations extracts `tenantId`, `userId`, and `correlationId` from trusted context.
3. The endpoint/tool validates that any body tenant matches the trusted tenant context; mismatch returns `payload-tenant-conflict`.
4. `IConversationTenantAccessService.CheckAccessAsync(tenantId, userId, requirement)` reads `ITenantProjectionStore`.
5. Access denial returns safe RFC 9457 ProblemDetails or a tool-specific exception with the same reason code.
6. Only after authorization does the handler load/append to EventStore using tenant-scoped aggregate identity, for example `{tenantId}:conversations:{conversationId}` or the existing Hexalith identity convention chosen by the architecture step.
7. Read projections are partitioned by tenant and conversation id; queries must include tenant in the actor id/state key/index partition.
8. Tenant events continuously update the local projection; disabled tenants and removed users deny after the corresponding event is processed.
9. Stale, missing, rolled-back, or failed projection state denies before aggregate/projection access.

**Confidence:** High that this is the correct integration pattern because it is already implemented in `Hexalith.Parties` and explicitly required by the Conversations PRD. Medium on the exact subscription route and durable projection-store implementation because those are deployment decisions that should be standardized in the Conversations architecture artifact.

---

## Architectural Patterns and Design

### System Architecture Patterns

Hexalith.Conversations should remain its own bounded context and consume `Hexalith.Tenants` as an upstream authority, not as embedded domain state. Microsoft’s microservice domain-analysis guidance aligns with this: bounded contexts define separate domain models, and a microservice should generally not span multiple bounded contexts. For Hexalith, that means Conversations owns conversation lifecycle, messages, redaction, retention, and conversation projections; Tenants owns tenant lifecycle, membership, roles, and tenant configuration.

The primary architecture should be event-sourced CQRS:

- `Conversation` is the aggregate and consistency boundary for conversation state.
- `Hexalith.EventStore` is the append-only write model for Conversations events.
- Conversation read models are tenant-partitioned projections.
- Tenant access is a separate local read model fed by `Hexalith.Tenants` events.

This also matches the Conversations PRD, which states that tenant decisions are consumed from `Hexalith.Tenants` projections with fail-closed semantics on missing or stale state (`_bmad-output/planning-artifacts/prd.md:219`, `:716`, `:1348-1349`).

_Recommended decision:_ Treat `IConversationTenantAccessService` as a required architectural boundary for every command/query/tool/admin path. Do not let controllers, tools, aggregate handlers, or projection actors each invent authorization checks.

_Source:_ Microsoft domain analysis for microservices: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis; microservice boundaries: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/microservice-boundaries; local PRD references above.

### Design Principles and Best Practices

The design should follow three principles.

First, tenant isolation is a precondition, not a business-rule side effect. Authorization must occur before aggregate load, command dispatch, projection read, rebuild, snapshot access, or tool execution. This prevents both accidental data disclosure and object-level authorization defects. OWASP’s API Security Top 10 calls out broken object-level authorization as a top API risk because endpoints that take user-controlled object IDs create a broad attack surface.

Second, tenant context is immutable within a request. A request body cannot override a trusted tenant claim/header/route. If body tenant differs from trusted context, reject with a stable `payload-tenant-conflict` style error before doing any lookup.

Third, Conversations stores stable upstream IDs, not upstream state. It can store `TenantId` as scope, `PartyId`, `ProjectId`, and `Folder/FileId` references, but tenant role/member/configuration state remains projected from Tenants and should not be copied into conversation events except as safe audit metadata where required.

_Recommended decision:_ Add an architecture fitness rule/test that rejects new command/query/admin/tool handlers that do not call the shared tenant access path.

_Source:_ OWASP API Security Project: https://owasp.org/www-project-api-security/; Microsoft tactical DDD guidance: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/tactical-domain-driven-design.

### Scalability and Performance Patterns

The tenant access path should be fast and local: one projection-store lookup by `tenantId`, then an in-memory role/status decision. This avoids turning every conversation read/write into a synchronous Tenants network dependency. It also gives Conversations independent scalability for high-volume read paths.

The trade-off is eventual consistency. Microsoft’s CQRS guidance notes that separated read and write models can be stale and require careful handling when a user acts on stale data. For tenant isolation, the correct handling is not “best effort”; it is a defined fail-closed policy when projection freshness is unknown, stale beyond SLO, rolled back, or gap-detected.

Recommended projection metadata:

- `TenantId`
- `TenantStatus`
- `Members[userId] = TenantRole`
- `Configuration` subset needed by Conversations, such as keys under `conversations.*`
- `LastAppliedSequenceNumber`
- `LastAppliedMessageId`
- `LastUpdatedUtc`
- `ProjectionGeneration` or rebuild marker

_Recommended decision:_ Production `ITenantProjectionStore` must expose freshness/order metadata. Access checks should deny as `tenant-state-stale` when metadata is absent or violates configured SLO.

_Source:_ Microsoft CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs; Microsoft Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing.

### Integration and Communication Patterns

The architectural integration is asynchronous by default:

1. Tenants writes tenant lifecycle/membership events.
2. EventStore publishes them through Dapr pub/sub.
3. Conversations subscribes to `system.tenants.events`.
4. Tenants client updates the local tenant access projection.
5. Conversations enforces access synchronously against that local projection during requests.

This keeps Tenants and Conversations loosely coupled while preserving a single authority for tenant state. Microsoft’s event-driven architecture guidance describes common consumer-side patterns, including simple event processing where an event triggers immediate consumer work. That is exactly the Tenants projection update use case.

Conversations should also publish its own conversation events, but those events must always include tenant-scoped identity. Downstream consumers should never be forced to infer tenant scope from an untrusted payload field.

_Recommended decision:_ Make tenant id part of every event envelope/aggregate identity/projection key, and add conformance tests proving a conversation id from tenant A cannot be loaded via tenant B even if the raw id is guessed.

_Source:_ Microsoft event-driven architecture style: https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven; Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/.

### Security Architecture Patterns

Use a zero-trust posture inside the module: no implicit trust from network location, gateway routing, Dapr app id, JWT tenant claim, or tool session context. NIST SP 800-207 describes zero trust as shifting focus from static network perimeters to users, assets, and resources, with authentication and authorization performed before resource access. For Conversations, the resource is a tenant-scoped conversation aggregate/projection.

Security architecture should include these enforcement gates:

- Authentication gate: valid token, trusted issuer/audience, user id present.
- Tenant context gate: tenant id present and normalized.
- Payload consistency gate: body/route/header/claim tenant values cannot conflict.
- Tenants projection gate: active tenant, active membership, sufficient role, fresh state.
- Resource scope gate: aggregate/projection key tenant equals trusted tenant.
- Audit gate: governance commands that mutate retention/redaction require audit availability and fail closed if audit write is unavailable.

Role mapping should be explicit and cumulative, matching Parties:

- `TenantReader`: read/list/timeline only.
- `TenantContributor`: reader permissions plus create conversation, append message, add participant, attach file reference, update title/metadata.
- `TenantOwner`: contributor permissions plus retention, redaction, archive/close, projection rebuild, administrative inspect/export where in scope.

_Recommended decision:_ Use `TenantOwner` for governance/admin operations; do not invent Conversations-specific tenant roles until Tenants supports extensible roles as an upstream contract.

_Source:_ NIST SP 800-207: https://csrc.nist.gov/pubs/sp/800/207/final; OWASP API Security Project: https://owasp.org/www-project-api-security/; local Parties role mapping: `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Authorization/TenantAccessService.cs:69-73`.

### Data Architecture Patterns

Conversation data should be partitioned by tenant at every level:

- Event stream identity includes tenant id.
- Projection actor/state keys include tenant id.
- List indexes are tenant-specific.
- Timeline/message projections are tenant-specific.
- Snapshot/rebuild cursors include tenant id.
- Audit/correlation records include tenant id in safe structured metadata.

Do not use a global conversation id index that can reveal existence across tenants. If a caller asks for a conversation id under the wrong tenant, return a tenant-safe not-found/denied response that does not disclose whether the conversation exists elsewhere.

Tenant access projection is a materialized authorization view. It is not a cache of arbitrary tenant data; it is a minimal, purpose-built read model. Keep the schema small so it can be rebuilt and validated quickly.

_Recommended decision:_ Define a `TenantScopedConversationId` or equivalent value object early. Avoid passing naked `conversationId` strings into repository/projection APIs that can load data without tenant scope.

_Source:_ Microsoft Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing; Microsoft CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs.

### Deployment and Operations Architecture

Production deployment needs more than code-level checks. It must make the tenant event feed observable and recoverable:

- Dapr pub/sub component scopes restrict who can publish/subscribe to `system.tenants.events`.
- The Tenants topic is treated as protected.
- Conversations subscription has bounded retry and a dead-letter topic.
- Tenant projection lag has an SLO and alert.
- Projection rebuild is tenant-scoped and requires `TenantOwner`/operator authorization.
- Rebuild cannot mix tenants in one operation unless each affected tenant receives an audit record and the operation is explicitly designed for cross-tenant SRE work.
- Startup readiness should report whether tenant projection state is usable; if not, CommandApi can be live but tenant-protected operations fail closed.

Dapr’s topic scoping supports allowed topics, publishing scopes, subscription scopes, and protected topics. This should be used to prevent accidental or malicious apps from subscribing to tenant events or publishing forged tenant events.

_Recommended decision:_ Add a deployment validation check equivalent to Parties’ Tenants subscription validation, but for Conversations: expected app id, topic, route, dead-letter topic, retry policy, and pub/sub scopes must all be present.

_Source:_ Dapr pub/sub topic scoping: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/; Dapr dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/; local Parties deployment files: `Hexalith.Parties/deploy/dapr/subscription-tenants.yaml`, `Hexalith.Parties/deploy/dapr/accesscontrol.yaml`.

### Architecture Decision Summary

Recommended ADRs for Hexalith.Conversations:

1. **Tenants as Source of Truth:** Conversations consumes tenant lifecycle/membership/role/configuration from `Hexalith.Tenants`; it does not own tenant state.
2. **Fail-Closed Tenant Access Boundary:** Every resource access goes through one shared access service before EventStore/projection/tool/admin work.
3. **Tenant-Scoped Identity:** Conversation aggregate ids, projection keys, snapshot keys, and audit metadata are tenant-scoped by construction.
4. **Durable Tenant Projection:** Production uses a durable `ITenantProjectionStore` with sequence/freshness metadata; in-memory projection is test/local only.
5. **Event-Driven Authorization Projection:** Tenants events update the local access projection through Dapr pub/sub; duplicate, missing, stale, out-of-order, and poisoned events are tested.
6. **Structured Safe Denials:** REST returns RFC 9457 ProblemDetails; tools return matching reason codes; public errors never disclose cross-tenant existence.
7. **Deployment Guardrails:** Dapr topic scopes, dead-letter topics, retries, readiness, lag alerts, and conformance checks are release gates.

**Confidence:** High on the architectural pattern and enforcement placement. The only medium-confidence areas are exact storage backend, exact route naming, and whether a synchronous strong-consistency authorization plugin is required for high-risk operations; those should be resolved during implementation architecture.

---

## Implementation Approaches and Technology Adoption

### Technology Adoption Strategies

Adopt `Hexalith.Tenants` in Conversations through a foundation-first slice rather than by sprinkling authorization checks into each endpoint as features arrive. The first implementation milestone should be a minimal tenant-isolation foundation that all later conversation commands and projections are forced to use.

Recommended adoption sequence:

1. Add `Hexalith.Tenants.Client` and register `AddHexalithTenants(...)`.
2. Add a Conversations-owned authorization layer: `IConversationTenantAccessService`, `ConversationTenantAccessDecision`, `ConversationTenantAccessRequirement`, and denial reason enum.
3. Map the Tenants event subscription endpoint and prove it subscribes to `system.tenants.events`.
4. Implement unit tests from the Parties role/fail-closed matrix.
5. Implement command/query/tool guards before any real conversation features are merged.
6. Replace the default in-memory projection store with a durable production implementation before production readiness.
7. Add conformance/deployment gates as release blockers.

This is a strangler-style adoption inside a greenfield module: build the enforcement boundary first, then route every feature through it. Avoid a "later hardening" plan; tenant isolation is a CORE PRD requirement and a release gate.

_Source:_ Azure Well-Architected operational excellence emphasizes standardized processes, observability, automation, safe deployments, and incident response: https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/; local PRD: `_bmad-output/planning-artifacts/prd.md:268`, `:1348-1349`.

### Development Workflows and Tooling

Use the existing Hexalith development workflow: central package management, xUnit v3, Shouldly, NSubstitute, Dapr/Aspire local hosting where needed, and module-specific test projects. The key implementation workflow is to keep tenant isolation changes in a dedicated foundation story and require all later stories to add tests proving they pass through the shared access service.

Suggested Conversations file/module layout:

```text
src/Hexalith.Conversations.CommandApi/Authorization/
  IConversationTenantAccessService.cs
  ConversationTenantAccessService.cs
  ConversationTenantAccessDecision.cs
  ConversationTenantAccessDenialReason.cs
  ConversationTenantAccessRequirement.cs
  ConversationTenantAccessDenialTranslator.cs

src/Hexalith.Conversations.CommandApi/Mcp/
  ConversationMcpTenantAuthorization.cs

src/Hexalith.Conversations.CommandApi/Extensions/
  ConversationsServiceCollectionExtensions.cs

tests/Hexalith.Conversations.CommandApi.Tests/Authorization/
tests/Hexalith.Conversations.CommandApi.Tests/Tenants/
tests/Hexalith.Conversations.Conformance.Tests/TenantIsolation/
```

The Parties module already gives a working template: `TenantAccessService` maps Tenants roles to module requirements and fails closed for missing tenant, missing user, unknown tenant, disabled tenant, missing member, insufficient role, and projection-store failure. Conversations should copy the structure, rename it for its domain, and adjust requirements to include governance/admin operations.

_Source:_ ASP.NET Core integration testing guidance supports test web hosts and in-memory test servers for focused request-pipeline tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0; local: `Hexalith.Parties/tests/Hexalith.Parties.CommandApi.Tests/Authorization/TenantAccessServiceTests.cs`.

### Testing and Quality Assurance

Testing should be layered so fast unit tests catch mapping mistakes, integration tests catch pipeline gaps, and conformance tests catch adversarial cross-tenant failures.

Minimum test suites:

- **Role matrix:** `TenantReader`, `TenantContributor`, `TenantOwner` mapped to `Read`, `Write`, `Admin/Governance`.
- **Fail-closed inputs:** missing tenant id, missing user id, unknown tenant, disabled tenant, missing member, unknown role, throwing projection store, stale projection.
- **Event pipeline:** `TenantCreated`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantDisabled`, duplicate message id, invalid payload, unknown event type.
- **Request authorization:** REST command and query endpoints call authorization before EventStore/projection access.
- **Tool authorization:** MCP/tool helpers call authorization before command routing or projection reads.
- **Cross-tenant adversarial:** guessed conversation id, route/body tenant mismatch, replayed command from another tenant, mixed-tenant projection rebuild.
- **Operations:** Dapr subscription exists, topic is correct, dead-letter topic configured, protected topic scopes configured, projection lag/staleness denies.

Use `Hexalith.Tenants.Testing` for test setup. `InMemoryTenantService` delegates to the real Tenants aggregate logic, while `TenantTestHelpers.CreateTenantWithOwner(...)` creates common tenant/member arrangements quickly (`Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs:17`, `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs:95`).

_Source:_ ASP.NET Core integration testing: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0; local Parties tests: `Hexalith.Parties/tests/Hexalith.Parties.CommandApi.Tests/Tenants/TenantEventInfrastructureTests.cs`.

### Deployment and Operations Practices

Production deployment must make tenant isolation observable and recoverable. The app can be healthy at the process level while tenant authorization is failing closed because the projection is empty, stale, or poisoned. That state must be visible.

Operational requirements:

- Readiness reports whether tenant projection store is reachable and freshness metadata is valid.
- Metrics include authorization denials by reason, projection lag, last applied sequence per tenant, tenant-event processing failures, dead-letter count, and projection rebuild status.
- Logs include safe tenant correlation only; no prompt/message content, inaccessible conversation ids, or raw membership dictionaries.
- Traces include authorization check spans with low-cardinality outcome tags.
- Runbooks cover Dapr sidecar failure, missing subscription, dead-letter replay, projection rebuild, and stale projection fail-closed incidents.

.NET OpenTelemetry is the right instrumentation path. Microsoft’s .NET observability guidance names logs, metrics, and distributed tracing as the three observability pillars and explains how OpenTelemetry collects and exports them across .NET services.

_Source:_ .NET OpenTelemetry observability: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel; Azure operational excellence: https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/.

### Team Organization and Skills

This work needs a small cross-functional slice rather than only feature coding:

- **Domain engineer:** Conversations aggregate/projection design and command flow.
- **Tenants/EventStore engineer:** event contract, projection store, replay/rebuild, ordering/freshness behavior.
- **Security/test engineer:** adversarial conformance cases, threat model, release gates.
- **Platform/SRE:** Dapr subscription, topic scopes, dead-letter, telemetry, runbooks.
- **Developer experience/docs:** integration guide, error catalog, contract/conformance package.

The riskiest skill area is not C# syntax; it is distributed authorization under eventual consistency. The team needs clear ownership for stale projection behavior, sequence gap policy, and signed conformance artifact generation before implementation starts.

_Source:_ DORA metrics guidance emphasizes measuring each application/service in context rather than blending across teams: https://dora.dev/guides/dora-metrics/.

### Cost Optimization and Resource Management

Tenant access checks should be cheap in the request path: one local projection lookup and no synchronous Tenants call. That keeps hot conversation reads/writes from scaling Tenants load linearly.

Cost-sensitive choices:

- Start with in-memory projection only for local/test.
- Use one durable projection store shared by CommandApi instances in production.
- Store minimal tenant access state rather than full tenant details.
- Track freshness metadata without high-cardinality metrics.
- Avoid per-message or per-conversation metric labels.
- Rebuild tenant access projections from Tenants/EventStore events rather than persisting redundant snapshots in many places.

The main cost to budget is engineering/test cost, not runtime cost. The PRD already calls out the engineering cliff: tenant isolation conformance is CORE and not cuttable.

_Source:_ Azure Well-Architected operational excellence and cost/process guidance: https://learn.microsoft.com/en-us/azure/well-architected/; local PRD timeline/risk notes: `_bmad-output/planning-artifacts/prd.md:135`, `:386`, `:388`.

### Risk Assessment and Mitigation

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Missing tenant projection after restart | All legitimate requests deny, or unsafe fallback is introduced | Durable projection store; readiness state; explicit `unknown-tenant` fail-closed behavior |
| Out-of-order Tenants events | Removed user or disabled tenant can regain access | Track per-tenant sequence; detect gaps/regressions; deny stale; rebuild |
| Body tenant overrides trusted tenant | Cross-tenant write/read | Reject `payload-tenant-conflict` before lookup |
| Endpoint forgets authorization | Direct aggregate/projection access | Shared helper, architecture fitness test, code review checklist |
| Tool/MCP path bypasses REST guard | Agent can access unauthorized tenant | Tool-level authorization helper copied from Parties pattern |
| Public error leaks existence | Attacker learns cross-tenant conversation ids | Tenant-safe ProblemDetails; conformance tests for guessed ids |
| Dapr subscription misconfigured | Projection stale forever | Deployment validation, dead-letter/retry config, projection lag alert |
| High-cardinality telemetry | Cost spike/privacy leak | Approved dimensions only; no conversation id/content labels |

_Source:_ OWASP API Security Top 10 for object-level authorization risk: https://owasp.org/www-project-api-security/; Dapr topic scopes/dead-letter docs cited above; local PRD NFR16/NFR17/NFR19.

## Technical Research Recommendations

### Implementation Roadmap

**Phase 0 - ADRs and contracts**

- Write ADRs for Tenants source of truth, fail-closed access boundary, tenant-scoped identity, durable projection, and deployment guardrails.
- Define `Read`, `Write`, `Admin/Governance` requirements.
- Define denial reason codes and ProblemDetails shape.

**Phase 1 - Foundation**

- Register `AddHexalithTenants`.
- Map Tenants subscription.
- Implement `IConversationTenantAccessService`.
- Add role matrix and fail-closed tests.
- Add REST/tool authorization helpers.

**Phase 2 - Conversation CORE path**

- Enforce tenant access on `CreateConversation`, `AddParticipant`, `AppendMessage`, `AttachFileReference`, and chatbot read projections.
- Add route/body/claim tenant mismatch tests.
- Make aggregate/projection keys tenant-scoped.

**Phase 3 - Production hardening**

- Implement durable `ITenantProjectionStore`.
- Add freshness/sequence metadata and stale/gap fail-closed behavior.
- Add Dapr subscription/access-control validation.
- Add OpenTelemetry metrics/logs/traces and runbooks.

**Phase 4 - Conformance**

- Build tenant isolation conformance suite and signed CI artifact.
- Include cross-tenant ID guessing, stale projection, poisoned tenant events, malformed metadata, and mixed-tenant rebuild.

### Technology Stack Recommendations

- .NET 10/C# for services and tests.
- ASP.NET Core for REST/CommandApi.
- Hexalith.EventStore for conversation event sourcing.
- Hexalith.Tenants.Client for tenant projection pipeline.
- Dapr pub/sub for `system.tenants.events`.
- Durable Dapr state store or approved database for production `ITenantProjectionStore`.
- OpenTelemetry/Aspire ServiceDefaults for observability.
- xUnit v3, Shouldly, NSubstitute, and ASP.NET Core `WebApplicationFactory` for tests.

### Skill Development Requirements

- Tenants event contracts and projection pipeline.
- EventStore aggregate identity and command routing.
- Dapr pub/sub subscriptions, scopes, retries, and dead-letter topics.
- ASP.NET Core authorization, ProblemDetails, and integration testing.
- OpenTelemetry metrics/traces/logs with low-cardinality labels.
- Threat modeling for multi-tenant object-level authorization.

### Success Metrics and KPIs

Use product/security gates plus delivery metrics:

- Zero cross-tenant access in conformance suite.
- 100% CORE command/query/tool paths covered by tenant authorization tests.
- Tenant projection lag under defined SLO.
- All stale/missing/unknown/rolled-back tenant states fail closed.
- Dapr subscription/dead-letter/scope validation passes in CI/deployment.
- Signed conformance artifact produced for every release.
- DORA delivery metrics tracked for the Conversations service: change lead time, deployment frequency, failed deployment recovery time, change fail rate, and deployment rework rate.

_Source:_ DORA metrics: https://dora.dev/guides/dora-metrics/.

---

<!-- Content will be appended sequentially through research workflow steps -->

---

# Tenant Isolation by Construction: Comprehensive Technical Research for Using Hexalith.Tenants in Hexalith.Conversations

## Executive Summary

`Hexalith.Conversations` should use `Hexalith.Tenants` as the authoritative tenant system, but it should not delegate every authorization decision to a remote tenant API at request time. The right architecture is an event-fed local tenant projection and a Conversations-owned access service that fails closed whenever tenant state is missing, stale, disabled, ambiguous, or insufficient for the requested operation. This keeps tenant checks fast, consistent across entry points, and aligned with the event-sourced architecture already used across Hexalith modules.

The existing `Hexalith.Parties` implementation provides the strongest local precedent. Parties registers `AddHexalithTenants`, subscribes to `system.tenants.events`, uses the shared Tenants projection store, and exposes a module-owned `ITenantAccessService` with safe denial reasons. Conversations should copy that boundary shape, but strengthen production behavior with a durable `ITenantProjectionStore`, per-tenant freshness metadata, sequence/gap handling, Dapr subscription validation, and explicit authorization gates before every sensitive data access path.

The strategic implication is simple: tenant isolation must be a module invariant, not an HTTP middleware convenience. Route values, JWT claims, request bodies, projection keys, tool session context, and admin/rebuild workflows all need a single tenant authorization decision model. If these paths disagree, Conversations risks object-level authorization failures, cross-tenant data exposure, and hard-to-debug operational drift.

**Key Technical Findings:**

- `Hexalith.Tenants.Client` already supplies the core integration pipeline through `AddHexalithTenants`, tenant event handlers, `TenantEventProcessor`, and `ITenantProjectionStore`.
- `Hexalith.Parties` demonstrates the desired consuming-module pattern: local access service, fail-closed denials, tenant event subscription, and tests for role/membership/lifecycle scenarios.
- Dapr pub/sub delivery is at least once, so tenant event processing and projection updates must be idempotent and duplicate tolerant.
- The current default Tenants client projection store is in-memory; Conversations needs durable state for production readiness and restart safety.
- Tenant identity in the request is only requested context. Conversations must verify tenant, user, role, and operation against the local Tenants projection before loading or returning conversation data.
- Safe denials must avoid revealing whether inaccessible tenant or conversation resources exist.

**Technical Recommendations:**

- Adopt the Parties pattern as the baseline: `AddHexalithTenants`, `MapTenantEventSubscription`, module-owned tenant access service, denial translator, and conformance tests.
- Enforce tenant authorization before aggregate load, command dispatch, projection read, tool invocation, rebuild, snapshot access, export, and administrative operation.
- Implement a production durable `ITenantProjectionStore` with freshness, sequence, duplicate, and gap metadata.
- Use explicit role mapping: `TenantReader` for read, `TenantContributor` for read/write, and `TenantOwner` for read/write/admin or governance.
- Standardize Dapr subscription route, topic, dead-letter topic, app scopes, and access control as deployable guardrails.
- Build a tenant isolation conformance suite that must pass before release.

## Table of Contents

1. Technical Research Introduction and Methodology
2. Technical Landscape and Architecture Analysis
3. Implementation Approaches and Best Practices
4. Technology Stack Evolution and Current Trends
5. Integration and Interoperability Patterns
6. Performance and Scalability Analysis
7. Security and Compliance Considerations
8. Strategic Technical Recommendations
9. Implementation Roadmap and Risk Assessment
10. Future Technical Outlook and Innovation Opportunities
11. Technical Research Methodology and Source Verification
12. Technical Appendices and Reference Materials

## 1. Technical Research Introduction and Methodology

### Technical Research Significance

Tenant isolation is one of the highest-risk architecture concerns for `Hexalith.Conversations` because conversations contain user messages, participant metadata, attachment references, and automation/tool context. A single missing tenant check can become an object-level authorization flaw, especially when APIs expose stable identifiers or when agents/tools can access the same data through non-REST paths.

The current local ecosystem makes this research immediately actionable. `Hexalith.Tenants` already defines tenant lifecycle, membership, role, and configuration events. `Hexalith.Parties` already consumes those events to protect a bounded context. `Hexalith.Conversations` can therefore avoid inventing a new authorization model and instead adopt a proven local boundary with production hardening.

_Technical Importance:_ Tenant isolation must be enforced at the domain and data access boundary, not only at the HTTP edge.
_Business Impact:_ Correct isolation protects confidentiality, supports enterprise governance, and makes Conversations safe for chatbot, MCP/tool, and administrative workflows.
_Sources:_ OWASP API Security object-level authorization: https://owasp.org/www-project-api-security/; NIST Zero Trust Architecture: https://csrc.nist.gov/pubs/sp/800/207/final.

### Technical Research Methodology

The research combined four evidence streams:

- Local code review of `Hexalith.Tenants`, `Hexalith.Parties`, tenant tests, Dapr deployment files, and Conversations planning artifacts.
- Architecture comparison against CQRS, event sourcing, event-driven architecture, and microservice boundary guidance.
- Runtime and integration verification against current Dapr pub/sub, topic scope, dead-letter, and state management documentation.
- Security and API behavior review using ASP.NET Core authorization, RFC 9457 ProblemDetails, OWASP API Security, and NIST Zero Trust guidance.

_Technical Scope:_ Tenant identity, membership, roles, event projection, command/query enforcement, projection rebuilds, tool/MCP authorization, deployment configuration, observability, and tests.
_Data Sources:_ Local Hexalith code and authoritative external documentation.
_Analysis Framework:_ Fail-closed security boundary, event-fed projection, bounded-context ownership, defense in depth, and conformance testing.
_Time Period:_ Current research as of 2026-05-10, with .NET 10 and Dapr 1.17.x context from the local repositories.
_Technical Depth:_ Architecture and implementation guidance suitable for ADRs, epics, tests, and production readiness gates.

### Technical Research Goals and Objectives

**Original Technical Goals:** Determine the architecture, integration points, event/projection flow, enforcement rules, tests, and operational safeguards needed for `Hexalith.Conversations` to consume `Hexalith.Tenants` for tenant isolation.

**Achieved Technical Objectives:**

- Identified the local `Hexalith.Parties` pattern as the preferred reference implementation for Tenants integration.
- Mapped tenant event subscription, projection, role evaluation, denial translation, and test patterns.
- Defined where Conversations must enforce tenant authorization across REST, commands, projections, tools, rebuilds, and admin flows.
- Identified production hardening gaps in default in-memory projection behavior and event ordering/freshness handling.
- Produced a phased implementation roadmap and risk register.

## 2. Technical Landscape and Architecture Analysis

### Current Technical Architecture Patterns

The recommended architecture is event-driven and bounded-context aligned. `Hexalith.Tenants` owns tenant lifecycle and membership. `Hexalith.Conversations` owns conversation behavior and storage, but it keeps a local projection of tenant state so it can make fast authorization decisions without creating a runtime dependency on the Tenants command/API service for every request.

This matches CQRS and event-sourcing guidance: tenant state changes are published as events, Conversations maintains a read model optimized for authorization decisions, and conversation aggregates remain protected behind an access boundary.

_Dominant Patterns:_ Bounded contexts, event-fed local projections, fail-closed authorization service, CQRS read model, and event-sourced aggregate protection.
_Architectural Evolution:_ Move from edge-only authorization to consistent module-level authorization across every entry point.
_Architectural Trade-offs:_ Eventual consistency requires freshness and stale-state handling, but avoids synchronous cross-service coupling on every operation.
_Sources:_ Microsoft CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs; Microsoft Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing; Microsoft microservice boundaries: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/microservice-boundaries.

### System Design Principles and Best Practices

The core design principle is "authorize before access." Conversations should authorize before it loads a conversation aggregate, resolves a projection key, emits a command, invokes a tool, starts a rebuild, or exposes an admin view. Authorization after load is too late because it can leak existence, timing, metadata, or internal error differences.

Tenant context should be consistent and explicit. If route tenant, body tenant, claim tenant, or tool session tenant conflict, Conversations should reject the request with a tenant-safe denial before any lookup. The Tenants event envelope detail matters here: tenant-management events use system context in the envelope, while the managed tenant is carried through the aggregate id/event context. Conversations should use the projected managed tenant state, not blindly trust request-level metadata.

_Design Principles:_ Verify tenant and user context, fail closed on uncertainty, prevent resource existence leakage, centralize role mapping, and keep authorization decisions auditable.
_Best Practice Patterns:_ Local projection, module-owned access service, safe denial translator, explicit operation requirements, and testable conformance suite.
_Architectural Quality Attributes:_ Low latency, restart safety, observability, operational diagnosability, and consistent behavior across REST/tools/admin paths.
_Sources:_ ASP.NET Core policy authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies; RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457.

## 3. Implementation Approaches and Best Practices

### Current Implementation Methodologies

Conversations should implement tenant isolation incrementally, starting with the infrastructure boundary and tests before applying it to every domain path. The first production-quality slice is not a UI or endpoint; it is the access service, projection store, subscription mapping, denial translator, and conformance tests.

The implementation should mirror Parties naming and behavior where useful, while using Conversations-specific names for ownership clarity: for example, `IConversationTenantAccessService`, `ConversationTenantAccessDecision`, `ConversationTenantAccessRequirement`, and `ConversationTenantAccessDenialTranslator`.

_Development Approaches:_ Contract-first authorization model, test-first role matrix, fail-closed negative tests, and explicit enforcement points.
_Code Organization Patterns:_ Keep Tenants projection plumbing in service registration, authorization decisions in a small application service, and endpoint/tool adapters thin.
_Quality Assurance Practices:_ Unit tests for role mapping, integration tests for endpoint/tool denial behavior, and deployment tests for Dapr subscription metadata.
_Deployment Strategies:_ Validate Dapr topic, route, scope, access-control, dead-letter topic, and projection readiness before serving tenant-scoped traffic.
_Sources:_ ASP.NET Core integration testing: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0; Dapr publish/subscribe how-to: https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/.

### Implementation Framework and Tooling

The service registration baseline should include:

- `AddHexalithTenants(options => configuration.GetSection("Tenants").Bind(options))`
- Conversations-owned tenant access service registration
- Dapr CloudEvents middleware
- Dapr subscribe handler
- Tenants event subscription endpoint
- Durable tenant projection store registration for production

The default Tenants options are a useful starting point: pub/sub name `pubsub`, topic `system.tenants.events`, and command API app id `commandapi`. Conversations should make these explicit in configuration to avoid hidden environment drift.

_Development Frameworks:_ .NET 10, ASP.NET Core, Dapr ASP.NET Core integration, Hexalith.Tenants.Client, Hexalith.EventStore, OpenTelemetry/Aspire service defaults.
_Tool Ecosystem:_ xUnit v3, Shouldly, NSubstitute, `WebApplicationFactory`, Dapr component YAML validation, and CI conformance artifacts.
_Build and Deployment Systems:_ CI should run tenant isolation tests, Dapr subscription validation, and projection persistence tests.
_Sources:_ .NET support lifecycle: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support; Dapr state management: https://docs.dapr.io/developing-applications/building-blocks/state-management/.

## 4. Technology Stack Evolution and Current Trends

### Current Technology Stack Landscape

The local repositories already converge on .NET/C#, ASP.NET Core, Dapr, Aspire, and event-sourced Hexalith modules. `Hexalith.Tenants` targets `net10.0`, making .NET 10 the natural baseline for Conversations as long as the deployment environment follows LTS patching practices.

The important trend is not a new framework choice; it is operational maturity. Tenant isolation depends on reliable event delivery, durable projection state, stale-state detection, safe errors, and observability. The local stack supports this, but Conversations must wire the pieces deliberately.

_Programming Languages:_ C# for service code and tests; YAML for Dapr components and deployment policy.
_Frameworks and Libraries:_ ASP.NET Core, Dapr, Hexalith.Tenants.Client, Hexalith.EventStore, OpenTelemetry.
_Database and Storage Technologies:_ Durable Dapr state store or approved persistent database for tenant projection state.
_API and Communication Technologies:_ REST/CommandApi for service calls; Dapr pub/sub and CloudEvents for tenant event propagation.
_Sources:_ Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; CloudEvents specification: https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md.

### Technology Adoption Patterns

Conversations should adopt `Hexalith.Tenants` as a platform capability rather than embedding tenant membership logic. The module-specific work is to translate platform tenant state into Conversations operation requirements.

_Adoption Trends:_ Local bounded contexts consume shared platform events and expose their own access boundary.
_Migration Patterns:_ Start with fail-closed authorization in new Conversations paths, then backfill enforcement into existing command/query/tool flows.
_Emerging Technologies:_ Agent/tool surfaces increase the importance of non-HTTP authorization paths; Conversations must treat tools as first-class tenant-scoped entry points.

## 5. Integration and Interoperability Patterns

### Current Integration Approaches

The primary integration is event subscription to Tenants events. Dapr delivers CloudEvents to the mapped subscription endpoint, `TenantEventProcessor` identifies the event type, and Tenants event handlers update local state. Conversations then reads projected tenant state through its access service.

The secondary integration is identity and request context. User identity may arrive through JWT, headers, route values, or tool session state, but none of those should be accepted as authorization by themselves. They are inputs to the access decision.

_API Design Patterns:_ Tenant id in route or explicit request context, body/route/claim consistency validation, and safe ProblemDetails denial response.
_Service Integration:_ Dapr pub/sub from Tenants to Conversations, with app scopes and dead-letter configuration.
_Data Integration:_ Tenant lifecycle, status, membership, roles, and configuration projected into a local store.
_Sources:_ Dapr topic scopes: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/; Dapr dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/.

### Interoperability Standards and Protocols

Dapr pub/sub and CloudEvents provide the interoperability layer for tenant events. RFC 9457 ProblemDetails provides a consistent HTTP error contract for denials. OAuth bearer token guidance informs the identity transport model, but token claims still require projection-backed tenant verification.

_Standards Compliance:_ CloudEvents for event envelope shape, ProblemDetails for HTTP error payloads, OAuth bearer token conventions for identity transport.
_Protocol Selection:_ Dapr pub/sub is appropriate for eventual-consistency projection; synchronous API calls should be reserved for commands or explicit administrative workflows.
_Integration Challenges:_ Duplicate delivery, out-of-order/gap detection, stale projections, route mismatch, subscription misconfiguration, and partial deployment.
_Sources:_ OAuth bearer tokens: https://oauth.net/2/bearer-tokens/; RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457.

## 6. Performance and Scalability Analysis

### Performance Characteristics and Optimization

Tenant access checks should be local reads against a compact projection keyed by tenant id and user id. This makes authorization cheap enough to run before every protected operation. The performance risk is not the access check itself; it is projection reliability under event bursts, restarts, stale state, and high tenant cardinality.

_Performance Benchmarks:_ No numeric benchmark is recommended until Conversations has an implementation, but the target should be in-process projection lookup latency rather than network-call latency.
_Optimization Strategies:_ Cache by tenant/user only if the durable projection store becomes a bottleneck, and preserve invalidation/freshness semantics.
_Monitoring and Measurement:_ Track projection lag, duplicate events, rejected stale checks, denied reasons, subscription delivery failures, and dead-letter counts.
_Sources:_ .NET OpenTelemetry observability: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel; Dapr pub/sub retries: https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/.

### Scalability Patterns and Approaches

Scalability requires partitioning and observability discipline. Projection keys should include tenant id, and conversation aggregate/projection keys should make tenant scope explicit. Telemetry dimensions should remain low cardinality: tenant status, denial reason, operation category, and component are safer than conversation ids or message ids.

_Scalability Patterns:_ Tenant-scoped aggregate identity, local read model, idempotent event handling, durable projection state, and bounded telemetry cardinality.
_Capacity Planning:_ Plan around tenant count, members per tenant, tenant event volume, conversation event volume, and projection rebuild duration.
_Elasticity and Auto-scaling:_ Multiple Conversations instances can consume pub/sub, but projection store concurrency and idempotency must be validated.

## 7. Security and Compliance Considerations

### Security Best Practices and Frameworks

The primary threat is cross-tenant object access. Conversations should therefore deny by default when tenant state is unknown, disabled, stale, missing membership, or below the required role. It should also normalize denial responses so attackers cannot infer whether a tenant, conversation, message, or participant exists.

The role model should be explicit:

| Tenant role | Conversations permissions |
| --- | --- |
| `TenantReader` | Read |
| `TenantContributor` | Read and Write |
| `TenantOwner` | Read, Write, Admin/Governance |

_Security Frameworks:_ Zero Trust principles, OWASP API object-level authorization controls, ASP.NET Core authorization, and safe ProblemDetails denials.
_Threat Landscape:_ Cross-tenant id guessing, confused deputy via tools, stale projection authorization, payload/route/claim mismatch, disabled tenant replay, and leaked resource existence.
_Secure Development Practices:_ Negative tests first, central access service, no ad hoc claim checks, safe denial mapping, and conformance testing before release.
_Sources:_ OWASP API Security: https://owasp.org/www-project-api-security/; NIST Zero Trust Architecture: https://csrc.nist.gov/pubs/sp/800/207/final.

### Compliance and Regulatory Considerations

The research did not identify a single regulation that drives all requirements for Conversations, but tenant isolation is a foundation for any later compliance posture involving confidentiality, auditability, data residency, retention, or customer separation. The technical work should therefore create audit-ready evidence: ADRs, tests, deployment validation, signed conformance artifacts, and observable denial metrics.

_Industry Standards:_ ProblemDetails, CloudEvents, OAuth bearer token conventions, and OpenTelemetry conventions.
_Regulatory Compliance:_ Future compliance depends on provable tenant separation, least privilege, audit trails, and controlled administrative access.
_Audit and Governance:_ Maintain ADRs, conformance results, deployment manifests, and operational runbooks.
_Sources:_ Azure Well-Architected operational excellence: https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/.

## 8. Strategic Technical Recommendations

### Technical Strategy and Decision Framework

Conversations should make four architectural decisions explicit:

| Decision | Recommendation |
| --- | --- |
| Tenant source of truth | `Hexalith.Tenants` owns tenant lifecycle, membership, roles, and status |
| Enforcement boundary | Conversations-owned access service checks every protected operation |
| Projection model | Event-fed local projection with durable production storage |
| Failure mode | Fail closed for missing, stale, disabled, unknown, inconsistent, or insufficient tenant state |

_Architecture Recommendations:_ Adopt the Parties consumption pattern, make tenant projection durable, and standardize denial semantics.
_Technology Selection:_ Use .NET 10, ASP.NET Core, Dapr, Hexalith.Tenants.Client, Hexalith.EventStore, and OpenTelemetry.
_Implementation Strategy:_ Build access service and tests first, then apply enforcement to each Conversations command/query/tool/admin path.
_Sources:_ Microsoft domain analysis for microservices: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis; tactical DDD guidance: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/tactical-domain-driven-design.

### Competitive Technical Advantage

The advantage is not novelty; it is trustworthy consistency. A Conversations module that enforces tenant isolation through one shared decision model across APIs, agents, tools, projections, and operations will be easier to audit, safer to extend, and less prone to drift as features grow.

_Technology Differentiation:_ Tenant isolation becomes a testable platform invariant.
_Innovation Opportunities:_ Tool/MCP tenant authorization can become a reusable Hexalith pattern for agentic workflows.
_Strategic Technology Investments:_ Durable projection infrastructure, conformance automation, and observability around tenant decisions.

## 9. Implementation Roadmap and Risk Assessment

### Technical Implementation Framework

**Phase 0 - ADRs and contracts**

- Write ADRs for Tenants as source of truth, fail-closed access boundary, tenant-scoped identity, durable projection, and Dapr deployment guardrails.
- Define `Read`, `Write`, and `Admin/Governance` requirements.
- Define denial reasons and ProblemDetails/tool response shape.

**Phase 1 - Foundation**

- Register `AddHexalithTenants` in Conversations.
- Map Tenants CloudEvents subscription.
- Implement Conversations tenant access service and denial translator.
- Add role matrix, unknown tenant, disabled tenant, missing member, insufficient role, stale projection, and conflict tests.

**Phase 2 - CORE conversation path**

- Enforce tenant access on conversation creation, participant updates, message append, attachment reference operations, chatbot reads, and projection queries.
- Make aggregate and projection keys tenant-scoped.
- Add route/body/claim mismatch tests.

**Phase 3 - Production hardening**

- Implement durable `ITenantProjectionStore`.
- Add per-tenant freshness, sequence, duplicate, and gap metadata.
- Validate Dapr subscription, topic scopes, access control, and dead-letter behavior.
- Add OpenTelemetry metrics, logs, traces, alerts, and runbooks.

**Phase 4 - Conformance**

- Build a tenant isolation conformance suite.
- Include cross-tenant id guessing, stale projection, disabled tenant, malformed tenant events, duplicate event delivery, and mixed-tenant rebuild scenarios.
- Publish signed conformance evidence for releases.

_Implementation Phases:_ Foundation, CORE enforcement, production hardening, and conformance.
_Technology Migration Strategy:_ Introduce authorization infrastructure first, then migrate each endpoint/tool/admin operation to the shared service.
_Resource Planning:_ Requires application engineering, platform/Dapr configuration, security review, and test automation.

### Technical Risk Management

| Risk | Impact | Mitigation |
| --- | --- | --- |
| In-memory projection used in production | Restart causes false denials or inconsistent behavior | Durable `ITenantProjectionStore` and readiness checks |
| Duplicate or out-of-order tenant events | Incorrect membership/status projection | Idempotency, sequence metadata, gap detection, and replay tests |
| Authorization only at REST edge | Tool/admin/projection paths bypass tenant checks | Shared access service used by every entry point |
| Route/body/claim tenant mismatch | Confused-deputy or cross-tenant write | Reject before lookup with safe conflict denial |
| Public error reveals existence | Attacker can enumerate tenant/conversation ids | Normalize denial responses and test guessed ids |
| Dapr subscription route/topic drift | Projection stops updating | Deployment validation and lag/dead-letter alerts |

_Sources:_ Dapr dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/; OWASP API Security: https://owasp.org/www-project-api-security/.

## 10. Future Technical Outlook and Innovation Opportunities

### Emerging Technology Trends

Near term, the critical work is operational hardening rather than new capability. Conversations needs the tenant boundary to be visible, testable, and durable. Medium term, the same pattern can protect additional agent/tool workflows and richer conversation governance. Long term, tenant policy could evolve from fixed role mapping toward tenant-specific configuration, provided policy evaluation remains centralized and auditable.

_Near-term Technical Evolution:_ Durable tenant projection, conformance suite, deployment validation, and tool authorization.
_Medium-term Technology Trends:_ Reusable tenant authorization patterns across Hexalith bounded contexts.
_Long-term Technical Vision:_ Tenant policy and governance as a platform capability with consistent module adapters.

### Innovation and Research Opportunities

Conversations can pioneer a reusable pattern for tool-safe tenant authorization. The MCP/tool path is especially important because agentic workflows may not naturally pass through REST controllers. A hardened tool authorization helper, modeled after Parties but generalized for Conversations, would reduce future risk.

_Research Opportunities:_ Policy versioning, projection replay validation, tenant-specific configuration controls, and synthetic cross-tenant attack tests.
_Emerging Technology Adoption:_ Agent/tool authorization conformance as a release gate.
_Innovation Framework:_ Treat every new entry point as untrusted until it proves it calls the shared access service.

## 11. Technical Research Methodology and Source Verification

### Comprehensive Technical Source Documentation

**Primary Local Technical Sources:**

- `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Client/Subscription/TenantEventEnvelope.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs`
- `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Authorization/TenantAccessService.cs`
- `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Authorization/TenantAccessDenialTranslator.cs`
- `Hexalith.Parties/src/Hexalith.Parties.CommandApi/Mcp/McpTenantAuthorization.cs`
- `Hexalith.Parties/tests/Hexalith.Parties.CommandApi.Tests/Authorization/TenantAccessServiceTests.cs`
- `Hexalith.Parties/tests/Hexalith.Parties.CommandApi.Tests/Tenants/TenantEventInfrastructureTests.cs`
- `Hexalith.Parties/deploy/dapr/subscription-tenants.yaml`
- `Hexalith.Parties/deploy/dapr/accesscontrol.yaml`

**Primary External Sources:**

- .NET release support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support
- ASP.NET Core authorization policies: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies
- ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
- Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/
- Dapr publish/subscribe how-to: https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/
- Dapr dead-letter topics: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/
- Dapr topic scopes: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/
- Dapr state management: https://docs.dapr.io/developing-applications/building-blocks/state-management/
- CloudEvents specification: https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md
- RFC 9457 Problem Details: https://www.rfc-editor.org/rfc/rfc9457
- OAuth bearer tokens: https://oauth.net/2/bearer-tokens/
- Microsoft CQRS pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Microsoft Event Sourcing pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- Microsoft event-driven architecture style: https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven
- Microsoft microservice boundaries: https://learn.microsoft.com/en-us/azure/architecture/microservices/model/microservice-boundaries
- NIST SP 800-207 Zero Trust Architecture: https://csrc.nist.gov/pubs/sp/800/207/final
- OWASP API Security Project: https://owasp.org/www-project-api-security/
- Azure Well-Architected operational excellence: https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/
- .NET OpenTelemetry observability: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
- DORA metrics: https://dora.dev/guides/dora-metrics/

### Technical Research Quality Assurance

_Technical Source Verification:_ Local implementation claims were checked against repository code and tests. External platform claims were checked against current official documentation where available.
_Technical Confidence Levels:_ High for local integration pattern, Dapr at-least-once delivery, default options, Parties authorization model, and recommended fail-closed architecture. Medium for exact production storage choice because the final persistence technology should align with the wider Hexalith deployment environment.
_Technical Limitations:_ This research did not implement code in Conversations, benchmark projection storage, or verify the final deployment topology. Those belong in implementation stories and CI/deployment validation.
_Methodology Transparency:_ The recommendation is intentionally conservative: reuse the local Parties pattern, strengthen projection durability, and enforce tenant decisions at every protected boundary.

## 12. Technical Appendices and Reference Materials

### Detailed Technical Decision Tables

| Concern | Recommended decision | Reason |
| --- | --- | --- |
| Tenant authority | `Hexalith.Tenants` | Single source of truth for tenant lifecycle, membership, roles, status |
| Conversations enforcement | Module-owned access service | Keeps authorization consistent across REST, commands, projections, tools, admin |
| Projection storage | Durable production store | Survives restart and supports readiness/freshness guarantees |
| Event delivery assumptions | At least once, idempotent handling | Dapr pub/sub can redeliver messages |
| Denial semantics | Safe ProblemDetails/tool reason codes | Prevents resource existence leakage |
| Role mapping | Reader, Contributor, Owner to Read/Write/Admin | Simple and auditable permission model |

### ADR Backlog

- ADR: `Hexalith.Tenants` is the source of truth for tenant state in Conversations.
- ADR: Conversations owns a fail-closed tenant access service.
- ADR: Tenant context must be verified across route, body, claim, and tool session inputs.
- ADR: Tenant projection storage must be durable in production.
- ADR: Tenant event handling must be idempotent and sequence/freshness aware.
- ADR: Public denials must not leak resource existence.
- ADR: Dapr topic, route, scope, and dead-letter configuration are deployment guardrails.

### Test Inventory

- Role matrix tests for Reader, Contributor, Owner, unknown role, and missing membership.
- Tenant lifecycle tests for created, enabled, disabled, and deleted/unknown states.
- Projection freshness tests for empty store, stale store, gap detection, duplicate events, and restart behavior.
- Request consistency tests for route/body/claim/tool tenant mismatch.
- REST denial tests for safe RFC 9457 ProblemDetails output.
- Tool/MCP denial tests for tenant-safe reason codes.
- Event subscription tests for topic name, route, CloudEvents handling, unknown event type, invalid payload, and duplicate message id.
- Cross-tenant conformance tests for id guessing, projection reads, command dispatch, admin operations, and rebuild workflows.

### Operational Checklist

- Dapr subscription points at `system.tenants.events`.
- Dead-letter topic is configured and monitored.
- Topic scopes and access control restrict tenant events to expected consumers.
- Tenant projection lag and stale-state denials are observable.
- Readiness fails or tenant-scoped traffic is denied when projection state is unavailable.
- Logs include denial reason and operation category but do not include message content or high-cardinality identifiers.
- Runbooks cover projection replay, dead-letter drain, stale projection recovery, and tenant event schema changes.

---

## Technical Research Conclusion

`Hexalith.Conversations` should not merely "use tenants"; it should make tenant isolation a first-class invariant. The safest and most maintainable design is to consume `Hexalith.Tenants` events, build a local durable tenant projection, and authorize every sensitive operation through a Conversations-owned access service that fails closed.

The local `Hexalith.Parties` implementation proves the pattern is already compatible with this codebase. The remaining work for Conversations is to adopt the pattern deliberately, strengthen it for production durability and operational visibility, and make tenant isolation conformance part of the release contract.

**Next Steps:**

- Create the ADRs listed in the appendix.
- Implement the Conversations tenant access service and denial translator.
- Register Tenants client and subscription plumbing in Conversations.
- Add the role matrix and fail-closed tests before wiring business endpoints.
- Add durable projection storage and operational readiness checks before production use.

**Technical Research Completion Date:** 2026-05-10
**Research Period:** Current comprehensive technical analysis
**Source Verification:** Local repository review plus current authoritative external documentation
**Technical Confidence Level:** High for architecture and integration pattern; medium for final projection storage technology until deployment infrastructure is selected.

_This comprehensive technical research document serves as the technical reference for using `Hexalith.Tenants` to manage tenant isolation in `Hexalith.Conversations`._
