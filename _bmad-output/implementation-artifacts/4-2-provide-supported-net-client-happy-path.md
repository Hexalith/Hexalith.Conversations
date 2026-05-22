# Story 4.2: Provide Supported .NET Client Happy Path

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want a supported .NET client for the core create, append, and read workflow,
so that I can integrate Conversations without hand-coding raw HTTP or EventStore details.

## Acceptance Criteria

1. .NET client executes the supported v1 create, append, and read workflow
   - Given the .NET client is configured with tenant context, caller metadata, correlation metadata, and endpoint settings,
   - When an adopter calls create conversation, append message, and read timeline methods,
   - Then the client sends Conversations contract commands and queries using the supported v1 integration path,
   - And it returns typed results, freshness metadata, and typed errors without exposing EventStore mechanics.

2. Idempotent retry behavior preserves server semantics
   - Given the adopter repeats a command after a timeout or unknown outcome,
   - When the .NET client resubmits with the same idempotency metadata,
   - Then it surfaces stable duplicate outcomes or idempotency conflicts consistently with server semantics,
   - And it does not treat provider session IDs as durable conversation identity.

3. Raw HTTP fallback remains gated and non-promotional
   - Given raw HTTP fallback is buyer-accepted or required for diagnostics,
   - When fallback guidance is used,
   - Then raw HTTP examples preserve the same tenant binding, idempotency, error, freshness, and schema-version behavior as the .NET client,
   - And fallback guidance does not encourage bypassing the contract package.

4. Client tests prove mapping, safety, freshness, and retry behavior
   - Given client tests run,
   - When happy path, timeout retry, unsupported schema, stale projection, tenant denial, sanitized error, and raw HTTP parity scenarios are exercised,
   - Then tests prove the client maps requests and responses correctly, preserves typed errors, and remains tenant-safe.

## Tasks / Subtasks

- [x] Confirm readiness gates and keep raw HTTP fallback out of ordinary scope (AC: 1, 3)
  - [x] Verify `_bmad-output/implementation-artifacts/readiness-gates.md` still records `Projection freshness blocking semantics` and `.NET client versus raw HTTP fallback policy` as `decided`.
  - [x] Do not add adopter-facing raw HTTP examples, parity tests, README snippets, or fallback docs unless a buyer approval or diagnostics-only exception is recorded.
  - [x] Treat raw HTTP as an internal transport implementation detail of the .NET client, not the adopter integration surface.

- [x] Implement a thin supported client over the existing contract package (AC: 1, 2, 4)
  - [x] Add client types under `src/Hexalith.Conversations.Client`, not under `Contracts` unless a type is truly shared wire contract.
  - [x] Keep `Hexalith.Conversations.Client` dependent on `Hexalith.Conversations.Contracts` and allowed Microsoft HTTP/DI packages only; it must not reference `Hexalith.Conversations.Server`, `Hexalith.EventStore`, Dapr, ASP.NET Core server abstractions, Tenants, Parties, FrontComposer, or UI packages.
  - [x] Provide a small adopter-facing API such as `IConversationClient` / `ConversationClient` with `CreateConversationAsync`, `AppendMessageAsync`, and `GetConversationAsync` or equivalent names using existing contract DTOs:
    - `CreateConversationCommand`
    - `AppendMessageCommand`
    - `GetConversationQuery` inputs or a client context that builds it
    - `ConversationCreatedResult`
    - `ConversationCommandAcceptedResult`
    - `ConversationDetailResult`
    - `ConversationErrorResult` / `ConversationError`
  - [x] Add configuration types such as `ConversationClientOptions` / `ConversationClientContext` only for endpoint, tenant binding, caller principal or actor metadata, correlation, causation, idempotency, and serializer settings. Do not duplicate command, query, projection, event, or error contracts.
  - [x] Add DI registration, for example `AddHexalithConversationsClient(...)`, using the typed-client pattern where practical.
  - [x] Preserve `ClientAssemblyMarker.ContractsMarkerType` and extend `ClientBoundaryTest` instead of deleting the existing marker/boundary checks.

- [x] Wire supported transport without leaking implementation details (AC: 1, 2, 4)
  - [x] Reuse the existing read route shape from `ConversationReadApi`: `GET /api/v1/conversations/{conversationId}` returns `ConversationDetailResult` and maps hidden/unavailable states safely.
  - [x] If create/append HTTP endpoints are still missing, add the narrowest opt-in server API extension needed for this story, for example `ConversationCommandApi`, guarded by the same authorization and tenant/caller extraction principles as `ConversationReadApi`.
  - [x] Keep `src/Hexalith.Conversations.Server/Program.cs` fail-closed unless the story explicitly implements a runnable host. Existing API extensions are intentionally opt-in for hosts/tests.
  - [x] Write requests using v1 contract command bodies and safe headers only. At minimum preserve `X-Correlation-Id` behavior and idempotency metadata from `ConversationCommandMetadata.IdempotencyKey`.
  - [x] Map response status and bodies into typed success/error results. Do not return raw `HttpResponseMessage`, raw exception text, route internals, EventStore status, stream names, storage positions, or projection topology as the public client result.

- [x] Preserve idempotency and provider-portability semantics (AC: 2)
  - [x] Use `ConversationCommandMetadata.IdempotencyKey` as the caller retry key; repeated calls with the same metadata must send the same idempotency metadata.
  - [x] Do not derive `ConversationId`, idempotency scope, or retry identity from `ProviderCorrelationMetadata` or provider session IDs.
  - [x] Add tests proving same-key duplicate retry returns stable typed duplicate/replay behavior or conflict behavior, and changed payload with same key does not look successful.
  - [x] Align client behavior with `IdempotentConversationCommandExecutor`, `ConversationCommandFingerprint`, `ConversationIdempotencyReplayResult`, and `EventStoreCommandStatusIdempotencyBridge` semantics; EventStore terminal/pending status is never a public client outcome by itself.

- [x] Preserve freshness, tenant safety, and sanitized error behavior (AC: 1, 4)
  - [x] Treat `ProjectionTrustState.Current` plus `ProjectionFreshnessReasonCode.Current` plus `IsStale == false` as the only trust-bearing read success unless a narrower exception is documented in the story implementation.
  - [x] Return stale, rebuilding, unavailable, forbidden, and redacted read outcomes as typed `ConversationDetailResult`/typed error outcomes without converting them into successful "fresh" timeline data.
  - [x] Preserve side-channel equivalence for denied, malformed, missing, or cross-tenant reads. The client must not disclose whether a protected conversation exists.
  - [x] Preserve `ConversationError` safe fields, documentation pointers, retryability, and safe diagnostics. Do not expose target tenant IDs, inaccessible Party IDs, redacted content, provider payloads, EventStore terms, raw routes, raw exceptions, or local file paths.

- [x] Add focused tests and package validation evidence (AC: 1-4)
  - [x] Add client tests under `tests/Hexalith.Conversations.Client.Tests` using a fake `HttpMessageHandler` or equivalent deterministic transport fixture.
  - [x] Verify outgoing create/append/read requests use the expected route, JSON shape, schema version, tenant metadata, correlation metadata, causation metadata, idempotency key, and no EventStore/raw-storage terms.
  - [x] Verify typed success mapping for create, append, and current read timeline.
  - [x] Verify typed mapping for unsupported schema/package compatibility, stale projection, unavailable projection, forbidden/hidden tenant denial, sanitized server error, timeout/unknown outcome retry, duplicate replay, and idempotency conflict.
  - [x] Extend `ClientBoundaryTest` to ensure the client assembly still references contracts and allowed Microsoft HTTP abstractions only.
  - [x] If server write endpoints are added, add `ConversationCommandApiTest` coverage for authorization, tenant/caller extraction, idempotency, safe error status mapping, and no read-route regression.
  - [x] If no raw HTTP fallback approval exists, add a negative coverage check proving no adopter-facing raw HTTP fallback API, docs, or examples were added. Add raw HTTP parity tests only when the required buyer approval or diagnostics-only exception exists.
  - [x] Run targeted tests first:
    - `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj`
    - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationReadApi|FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~Idempotency"`
  - [x] Run package validation:
    - `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation`
  - [x] Run the full solution before closing:
    - `dotnet test Hexalith.Conversations.slnx`
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 4.2 evidence after implementation.

- [x] Preserve scope boundaries and stop conditions (AC: 1-4)
  - [x] Do not implement Story 4.3 broad typed error remediation expansion except minimal typed mapping needed for client behavior.
  - [x] Do not implement Story 4.4 onboarding diagnostics, CORE precondition checks, or configuration gap scanners.
  - [x] Do not implement Story 4.5 adopter conformance package or CORE fixture runner.
  - [x] Do not implement Story 4.7 full developer integration guide, DocFX/API reference pipeline, or expanded API examples beyond minimal package/client README updates needed for this story.
  - [x] Do not implement Epic 5 release manifest signing, named waivers, release-gate evidence aggregation, deprecation policy publication, or versioned conformance manifest.
  - [x] Stop for ADR/architecture review before adding new durable state, changing public error taxonomy, changing trust/freshness vocabulary, weakening fail-closed tenant checks, or exposing EventStore/server internals through the client.

## Dev Notes

### Epic and Business Context

- Epic 4 makes adopter integration credible through a contract package, supported .NET client, compatibility discovery, typed sanitized errors, onboarding diagnostics, conformance tests, and developer guidance. Story 4.2 is the supported v1 client path. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`]
- Story 4.2 covers FR71, FR72, and FR74: adopters can use a supported .NET client, execute the minimal create -> append -> read happy path, and rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.2: Provide Supported .NET Client Happy Path`; `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`]
- The PRD locks the packaging model as shared contract package plus per-language thin clients. The contract package is the source of truth for DTOs, command shapes, projection shapes, error envelope, and event schema. The .NET client must be a thin language-idiomatic wrapper, not a second contract model. [Source: `_bmad-output/planning-artifacts/prd.md#API Documentation & Versioning`]
- Diego's adopter journey requires a five-line happy path that creates a conversation, appends a message, and reads the timeline without EventStore knowledge. The story implementation should make that path possible through the .NET client while keeping raw HTTP fallback out of ordinary guidance. [Source: `_bmad-output/planning-artifacts/prd.md#MVP User Journeys`; `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`]

### Readiness Gate Context

- `.NET client versus raw HTTP fallback policy` is decided: v1 uses the .NET client plus shared contract package. Raw HTTP examples require later buyer approval or diagnostics-only exception. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`]
- `Projection freshness blocking semantics` is decided and blocks Story 4.2. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; when a story does not explicitly allow other states, only `Current` is acceptable for trust-bearing decisions. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#projection-freshness-blocking-semantics`]
- EventStore envelope ownership is decided: Conversations owns public domain event names, schemas, contract versioning, and compatibility tests, but does not expose or evolve EventStore envelopes in this project. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#eventstore-envelope-stability-and-evolution-ownership`]

### Current Implementation State

- `src/Hexalith.Conversations.Client` exists, is packable, references only `Hexalith.Conversations.Contracts`, and currently exposes only `ClientAssemblyMarker`. This is the intended project to extend for Story 4.2. [Source: `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`; `src/Hexalith.Conversations.Client/ClientAssemblyMarker.cs`]
- `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs` currently proves the client assembly references Contracts and not Server, EventStore, or Dapr. Extend this safety net. [Source: `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`]
- Story 4.1 added active v1 compatibility metadata for command, projection, event, contracts package, and client package versions. Reuse `ConversationContractCompatibility.Current` and `Evaluate(...)`; do not create a parallel version-discovery model. [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`; `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`]
- Existing public command contracts include `CreateConversationCommand`, `AppendMessageCommand`, and `ConversationCommandMetadata`; these already carry schema version, tenant, actor Party, correlation, causation, and idempotency metadata. [Source: `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`; `src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs`; `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`]
- Existing public read contracts include `GetConversationQuery`, `ConversationDetailResult`, `ConversationDetailsV1`, and `ProjectionFreshnessV1`. Preserve the freshness and hidden/unavailable semantics instead of flattening reads into nullable timeline data. [Source: `src/Hexalith.Conversations.Contracts/Queries/GetConversationQuery.cs`; `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailResult.cs`; `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`; `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`]
- Existing result and error contracts include `ConversationCreatedResult`, `ConversationCommandAcceptedResult`, `ConversationError`, and `ConversationErrorResult`. Map client responses into these types where possible. [Source: `src/Hexalith.Conversations.Contracts/Results/ConversationCreatedResult.cs`; `src/Hexalith.Conversations.Contracts/Results/ConversationCommandAcceptedResult.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorResult.cs`]
- `ConversationReadApi` already maps guarded GET routes under `/api/v1/conversations`, derives tenant scope from authenticated claims, accepts `X-Correlation-Id`, and returns hidden/forbidden/unavailable read bodies without leaking protected existence. Preserve this route behavior. [Source: `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`; `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`]
- `src/Hexalith.Conversations.Server/Program.cs` still throws a fail-closed `NotImplementedException`. Do not turn this into an unguarded runnable service as part of the client story. [Source: `src/Hexalith.Conversations.Server/Program.cs`]
- Domain aggregate and idempotency primitives already exist in `src/Hexalith.Conversations`: `ConversationAggregate`, `ConversationCommandFingerprint`, `IdempotentConversationCommandExecutor`, `ConversationIdempotencyReplayResult`, and `EventStoreCommandStatusIdempotencyBridge`. The client must reflect their public semantics without exposing their internals. [Source: `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`; `src/Hexalith.Conversations/Idempotency/ConversationCommandFingerprint.cs`; `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs`; `src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs`]

### Architecture and Contract Guardrails

- Public APIs expose Conversations commands, projections, result contracts, version metadata, typed errors, and freshness state. They must not expose EventStore envelopes, aggregate IDs as substrate concepts, snapshot mechanics, stream internals, SignalR groups, or raw projection internals. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Security gates must be consistent across REST APIs, typed .NET client, FrontComposer admin UI, MCP/tool operations, worker/rebuild jobs, verification/conformance commands, exports, and evidence bundles. The client cannot bypass tenant access projection checks, command availability checks, or content-safe response shaping. [Source: `_bmad-output/planning-artifacts/architecture.md#Security Gate Consistency`]
- Tenant access fails closed when local tenant state is missing, stale, ambiguous, disabled, lagging, rolled back, deleted, or unavailable. Missing or invalid tenant context must never default to a tenant or global query. [Source: `_bmad-output/planning-artifacts/architecture.md#Authorization Pattern`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Public routes use plural lowercase versioned REST paths such as `/api/v1/conversations/{conversationId}/messages`; route parameters use camelCase. If write endpoints are added, follow this shape and keep raw route details out of adopter-facing client result types. [Source: `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`]
- Trust/freshness states must use one shared vocabulary across API, client, UI, diagnostics, and evidence. Do not invent client-only synonyms such as "ready", "valid", "lagged", or "maybe current". [Source: `_bmad-output/planning-artifacts/architecture.md#Shared Trust/Freshness Vocabulary Gate`; `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`]
- The client package remains adopter-facing. It must not expose EventStore status names, stream IDs, snapshots, storage positions, projection topology, raw handler names, generated UI details, tenant projection internals, Party personal data, provider prompt/response payloads, or raw exception text. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`]

### UX and Developer Experience Notes

- There is no UI implementation in Story 4.2, but developer experience is part of the UX spec: developers should experience create -> append -> read as a dependable contract with safe typed failure states, without learning EventStore internals or rebuilding tenant isolation. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Core Experience`; `_bmad-output/planning-artifacts/ux-design-specification.md#Defining The Experience`]
- Trust states remain contract-owned even for adopters. The client may format or expose typed states, but it must not infer trust, permission, freshness, redaction, participant resolution, or command availability from local cache age, HTTP status alone, missing fields, or display text. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Design System Foundation`; `_bmad-output/planning-artifacts/ux-design-specification.md#Final Interaction Rules`]
- Safe language and safe state behavior apply to client errors as much as UI states. Denied, unavailable, stale, redacted, and degraded states must not leak protected existence or content. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Safe States`; `_bmad-output/planning-artifacts/ux-requirement-map.md`]

### Latest Technical Notes

- Official .NET guidance recommends `IHttpClientFactory` for DI-managed logical clients; typed clients are the most structured factory consumption pattern and are configured with `AddHttpClient`. Use this for a DI registration extension unless the implementation deliberately chooses a static/singleton `HttpClient` with `PooledConnectionLifetime`. [Source: `https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory#the-ihttpclientfactory-type`; `https://learn.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#multiple-ways-to-use-ihttpclientfactory`]
- `System.Net.Http.Json` provides `PostAsJsonAsync` and `ReadFromJsonAsync`; the basic overloads use `JsonSerializerDefaults.Web`, while source-generated `JsonTypeInfo`/`JsonSerializerContext` overloads avoid the dynamic-code/trimming warnings shown on some overloads. Because this repo treats warnings as errors, prefer source-generated context or explicit serializer options if warnings appear. [Source: `https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient#make-an-http-request`; `https://learn.microsoft.com/dotnet/api/system.net.http.json.httpclientjsonextensions.postasjsonasync`; `https://learn.microsoft.com/dotnet/api/system.net.http.json.httpcontentjsonextensions.readfromjsonasync`]
- Microsoft's current resilience guidance says `Microsoft.Extensions.Http.Polly` is deprecated; use `Microsoft.Extensions.Resilience` or `Microsoft.Extensions.Http.Resilience` for new HTTP resilience. The repo already pins `Microsoft.Extensions.Http.Resilience` centrally at `10.4.0`. Do not add `Microsoft.Extensions.Http.Polly`. [Source: `https://learn.microsoft.com/dotnet/core/resilience/`; `Directory.Packages.props`]
- Central Package Management is active. If `Microsoft.Extensions.Http`, `System.Net.Http.Json`, or another Microsoft package is needed, add the version in `Directory.Packages.props` and a versionless `PackageReference` in the project. Do not add inline versions to `.csproj` files. [Source: `Directory.Packages.props`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]

### Previous Story Intelligence

- Story 4.1 completed the contract/package compatibility layer and explicitly did not implement Story 4.2 client behavior. Build on its compatibility metadata, package metadata, and test patterns. [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`]
- Story 4.1 added/updated:
  - `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`
  - `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
  - `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`
  [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md#File List`]
- Story 4.1 review fixed compatibility invariant enforcement, client package metadata coverage, and package-specific contracts/client version evaluation. For Story 4.2, analogous risks are swallowing typed failures as success, treating HTTP status alone as truth, or letting the client drift from active package compatibility metadata. [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md#Senior Developer Review (AI)`]
- Recent commits are story-scoped and test-heavy: `feat(story-4.1): Add contract compatibility metadata`, `feat(story-3.7): Add buyer acceptance demo fixtures`, and governance verification/read-only compliance stories. Continue with focused red tests and boundary tests before broad solution validation. [Source: `git log --oneline -5`]

### File Structure Guidance

- Likely update files:
  - `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`
  - `src/Hexalith.Conversations.Client/ClientAssemblyMarker.cs` only if marker documentation needs adjustment
  - `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`
  - `Directory.Packages.props` if new packages are needed
  - `README.md` or `src/Hexalith.Conversations.Contracts/README.md` only for minimal client/package guidance
- Likely new client files:
  - `src/Hexalith.Conversations.Client/ConversationClient.cs`
  - `src/Hexalith.Conversations.Client/IConversationClient.cs`
  - `src/Hexalith.Conversations.Client/ConversationClientOptions.cs`
  - `src/Hexalith.Conversations.Client/ConversationClientContext.cs`
  - `src/Hexalith.Conversations.Client/ConversationClientResult.cs` or equivalent typed wrapper if needed
  - `src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs`
  - `src/Hexalith.Conversations.Client/Serialization/ConversationClientJsonContext.cs` if source-generated JSON metadata is used
- Likely new/updated test files:
  - `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`
  - `tests/Hexalith.Conversations.Client.Tests/ConversationClientRegistrationTest.cs`
  - `tests/Hexalith.Conversations.Client.Tests/ConversationClientSerializationTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs` only if write endpoints are added
- Only add server command API files if the happy path cannot be implemented/tested against existing opt-in endpoints:
  - `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`

### Testing Requirements

- Client tests should not require a live server. Use fake HTTP handlers for transport mapping and explicit server API tests for server route behavior.
- Serialization tests must use the same `JsonSerializerDefaults.Web` and closed-vocabulary converter behavior expected by contract tests.
- Content-safety tests should scan serialized client-visible success and error results for forbidden fragments: `EventStore`, `stream`, `snapshot`, `envelope`, `SignalR`, `subscription`, `server route`, `handler`, `dispatcher`, `repository`, `store`, `displayName`, `email`, `phone`, `provider payload`, `raw exception`, `C:\`, and `D:\`.
- Idempotency tests must cover duplicate replay, changed-payload conflict, timeout/unknown outcome retry, and same-key same-payload behavior without using provider session IDs as durable identity.
- Freshness tests must cover `Current`, `Stale`, `Rebuilding`, `Unavailable`, and `Forbidden`. Only `Current` can be treated as a trust-bearing timeline success.

### Out of Scope

- No general raw HTTP integration guide or public raw HTTP fallback examples without buyer approval or diagnostics-only exception.
- No EventStore envelope, stream, snapshot, projection topology, or command-status exposure.
- No new contract package or duplicated DTO hierarchy.
- No onboarding diagnostics, CORE precondition scanner, provider configuration checks, or diagnostic wizard.
- No adopter conformance package, release manifest, signed evidence artifact, named waiver lifecycle, or deprecation policy publication.
- No Admin UI, FrontComposer work, browser UX implementation, or telemetry dashboard.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 4.2: Provide Supported .NET Client Happy Path`
- `_bmad-output/planning-artifacts/epics.md#Implementation Readiness Gates`
- `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`
- `_bmad-output/planning-artifacts/prd.md#API Documentation & Versioning`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Authorization Pattern`
- `_bmad-output/planning-artifacts/architecture.md#Security Gate Consistency`
- `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Core Experience`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`
- `_bmad-output/project-context.md`
- `README.md`
- `docs/projection-read-models.md`
- `docs/conversation-publication-events.md`
- `docs/adrs/0001-idempotency-contract.md`
- `Directory.Build.props`
- `Directory.Packages.props`
- `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`
- `src/Hexalith.Conversations.Client/ClientAssemblyMarker.cs`
- `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`
- `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationQuery.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorResult.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs`
- `src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs`
- `https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory#the-ihttpclientfactory-type`
- `https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient#make-an-http-request`
- `https://learn.microsoft.com/dotnet/api/system.net.http.json.httpclientjsonextensions.postasjsonasync`
- `https://learn.microsoft.com/dotnet/api/system.net.http.json.httpcontentjsonextensions.readfromjsonasync`
- `https://learn.microsoft.com/dotnet/core/resilience/`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Verified readiness gates were decided before implementation.
- 2026-05-22: Red phase confirmed client tests failed before implementation because supported client types and DI references were missing.
- 2026-05-22: Targeted validation passed:
  - `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 17 passed after review fix.
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationReadApi|FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~Idempotency"` - 56 passed.
  - `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - produced `Hexalith.Conversations.Client.1.0.0.nupkg`.
  - `dotnet test Hexalith.Conversations.slnx` - all solution tests passed.

### Implementation Plan

- Add a thin adopter-facing `IConversationClient` / `ConversationClient` over the existing v1 contract DTOs.
- Preserve command idempotency, correlation, tenant, caller, typed error, and freshness behavior without exposing EventStore or raw HTTP response mechanics.
- Add the narrow opt-in server command API extension needed by the client route shape while keeping `Program.cs` fail-closed.
- Prove behavior through deterministic fake HTTP client tests, server endpoint tests, package inventory, and boundary tests.

### Completion Notes List

- Implemented the supported .NET client happy path for create, append, and read using existing contract DTOs and typed `ConversationClientResult<T>` outcomes.
- Added `ConversationClientContext`, endpoint options, and `AddHexalithConversationsClient(...)` typed-client DI registration with Microsoft `IHttpClientFactory`.
- Added narrow opt-in `ConversationCommandApi` server endpoints for `POST /api/v1/conversations/` and `POST /api/v1/conversations/{conversationId}/messages`; hosts still opt in explicitly and `Program.cs` remains fail-closed.
- Preserved idempotency metadata through command bodies and safe headers, mapped duplicate replay/conflict/unknown outcome as typed results/errors, and kept provider session references out of route/header identity.
- Preserved freshness semantics by returning `ConversationDetailResult` unchanged for current, stale, rebuilding, unavailable, and forbidden read outcomes.
- Added raw HTTP fallback negative coverage; no adopter-facing fallback API, examples, README snippets, or docs were added.
- Updated Story 4.2 evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Review fix hardened client deserialization for non-seekable HTTP response streams and added tenant-denial fallback coverage.

### File List

- `Directory.Packages.props`
- `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`
- `src/Hexalith.Conversations.Client/ConversationClient.cs`
- `src/Hexalith.Conversations.Client/ConversationClientContext.cs`
- `src/Hexalith.Conversations.Client/ConversationClientOptions.cs`
- `src/Hexalith.Conversations.Client/ConversationClientResult.cs`
- `src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs`
- `src/Hexalith.Conversations.Client/IConversationClient.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`
- `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs`
## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 4, Story 4.2, readiness gates, Story 4.1 prerequisite context, and downstream Story 4.3-4.7 boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR70-FR80, the locked shared-contract-plus-thin-client packaging model, Diego's developer journey, typed errors, and compatibility/freshness NFRs.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on API/client boundaries, tenant authorization, EventStore non-disclosure, shared freshness vocabulary, project structure, and central package management.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`; no UI is in scope, but developer experience, safe states, and trust/freshness non-inference remain binding.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`, including .NET 10, central package management, Contracts/Client boundaries, fail-closed tenant rules, idempotency rules, and submodule policy.
  - Loaded Story 4.1, current sprint status, readiness gates, readiness decisions, README/package docs, projection and publication docs, ADR index/idempotency ADR, current client/contracts/server files, existing tests, recent test summary, and recent git history.
  - Checked official Microsoft documentation for `IHttpClientFactory`, typed clients, `System.Net.Http.Json`, JSON serializer context overloads, and current .NET resilience package guidance.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to the existing `Hexalith.Conversations.Client` package and shared `Contracts` DTOs instead of creating duplicate contracts.
  - Added explicit guardrails for missing write endpoints, opt-in server API extensions, fail-closed `Program.cs`, typed error/freshness mapping, idempotency replay/conflict behavior, and raw HTTP fallback gating.
  - Added current file touch list, likely new file list, targeted test commands, package validation, official Microsoft documentation references, previous Story 4.1 learnings, and architecture stop conditions.
  - Kept onboarding diagnostics, conformance package, full integration guide, raw HTTP public examples, release signing, deprecation policy lifecycle, UI work, and observability dashboards out of scope.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture/client guardrails, test requirements, latest technical references, and explicit out-of-scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Senior Developer Review (AI)

### Review Date

2026-05-22

### Review Outcome

Approve

### Findings

- [x] [HIGH] Fixed response deserialization that treated non-seekable HTTP content streams as malformed before attempting JSON deserialization. Real `HttpClient` transports commonly provide non-seekable response streams, so successful server responses could have been mapped to typed fallback errors instead of typed success results. Fixed in `src/Hexalith.Conversations.Client/ConversationClient.cs`; regression coverage added in `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`.

### Review Validation

- `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 17 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationReadApi|FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~Idempotency"` - 56 passed.
- `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - passed.
- `dotnet test Hexalith.Conversations.slnx` - passed.

## Change Log

- 2026-05-22: Code review fixed non-seekable HTTP response deserialization and added tenant-denial fallback coverage; story marked done.
- 2026-05-22: Implemented supported .NET client happy path, opt-in command API route extension, focused tests, package validation evidence, and Story 4.2 test-summary evidence.
- 2026-05-22: Created Story 4.2 context from Epic 4 requirements, PRD/architecture/UX/readiness/project context, current client/contracts/server source, Story 4.1 learnings, recent git history, and official Microsoft documentation.

