# Story 1.8: Retrieve and List Conversations by Tenant Business Context

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want to retrieve and list conversations within an authorized tenant scope,
so that applications and operators can find the right conversation records without leaking inaccessible records or relying on provider session state.

## Acceptance Criteria

1. Given an authorized caller requests a conversation by `ConversationId`, when the read boundary evaluates tenant access and reads the projection, then it returns conversation detail with participant set, ordered message timeline metadata, attachment/file references, governance state placeholders where available, business references, provider correlation metadata, and freshness context, and the response exposes Conversations contracts rather than EventStore stream, snapshot, or projection internals.
2. Given an authorized caller lists conversations for a tenant, when filters such as external business identifier, project reference, folder reference, lifecycle state, date range, recent activity, or participant reference are supplied, then the result contains tenant-scoped conversation summaries and permission-safe pagination metadata, and external business identifiers are treated as correlation/search keys distinct from internal `ConversationId`.
3. Given a caller is unauthorized, tenant binding is invalid, or a requested conversation is nonexistent or cross-tenant, when retrieve or list is evaluated, then the response is content-safe and does not reveal titles, participant names, snippets, timestamps, counts, ordering gaps, business references, provider metadata, or existence of protected records, and the result maps to documented tenant-isolation or hidden/not-found semantics.
4. Given projections are stale, rebuilding, unavailable, or hidden by tenant isolation, when retrieve or list results are returned, then freshness state and safe next-action metadata are included where authorized, and the read boundary does not silently present stale or incomplete data as current.
5. Given retrieve/list tests run, when authorized reads, filtered lists, cross-tenant ID guessing, inaccessible records, stale projections, unavailable projections, and provider-session loss scenarios are exercised, then tests prove correct filtering, freshness signaling, content-safe denial, and conversation recoverability without provider-owned session authority.
6. Given any retrieve or list request, when the read boundary evaluates it, then local tenant access is accepted before any conversation projection lookup, existence check, count query, filter evaluation against stored data, pagination cursor resolution, participant index lookup, provider-session correlation lookup, or freshness metadata read can occur.

## Tasks / Subtasks

- [ ] Confirm prerequisite slices are present before implementing runtime behavior. (AC: 1-6)
  - [ ] Verify Story 1.2 contract types exist for identities, projections/read DTOs, trust/freshness states, typed errors, and schema/version metadata; if absent, implement or complete Story 1.2 first rather than defining parallel DTOs in this story.
  - [ ] Verify Story 1.5 tenant access service/projection exists and fails closed before projection reads; if absent, do not invent permissive authorization or rely on JWT tenant claims alone.
  - [ ] Verify Story 1.7 projection read models and freshness metadata exist; if absent, do not create an alternate transcript/read store in this story.
  - [ ] Verify Story 1.4, 1.4.1, and 1.4.2 event/read-model inputs exist for participants, ordered message metadata, file references, provider correlation metadata, and business references; if not, keep corresponding response fields as contract-safe placeholders only.
  - [ ] Record the dependency decision in implementation notes: each prerequisite is either available through its approved contract/abstraction, completed first, or represented by a safe placeholder/unavailable result. Missing prerequisites must not be replaced by new DTO families, new tenant access implementations, new projection stores, or direct EventStore reads in this story.

- [ ] Add or extend public retrieve/list contracts in `src/Hexalith.Conversations.Contracts`. (AC: 1-4)
  - [ ] Define query request contracts such as `GetConversationQuery` and `ListConversationsQuery` or align with the established local naming if Story 1.2 created different names.
  - [ ] Define permission-safe detail and summary contracts such as `ConversationDetailsV1`, `ConversationSummaryV1`, `ConversationListResult`, pagination metadata, and filter objects for external identifier, project, folder, lifecycle, date range, recent activity, and participant reference.
  - [ ] Ensure every query contract includes tenant binding, caller context/correlation metadata where already established, supported schema/contract version, and no EventStore stream, snapshot, sequence, envelope, projection topology, or raw storage fields.
  - [ ] Keep external business identifiers as tenant-scoped search/correlation keys. Do not allow them to replace `ConversationId`, `ProjectId`, `FolderId`, `FileId`, `PartyId`, or message identity.

- [ ] Implement the server read/query boundary in `src/Hexalith.Conversations.Server`. (AC: 1-4, 6)
  - [ ] Add query handlers under `Server/Queries` or the existing local query folder that perform tenant access checks before any projection lookup.
  - [ ] Add read APIs under `Server/Api` or the established endpoint folder only after handlers exist; REST paths should stay plural, lowercase, and versioned, for example `/api/v1/conversations/{conversationId}` and `/api/v1/conversations`.
  - [ ] Query the Story 1.7 projection/read-model service only after authorization succeeds. Projection reads must surface `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, or `Redacted` freshness states through shared contracts.
  - [ ] Return only Conversations contracts and typed safe results. Do not return raw projection documents, EventStore envelopes, stream IDs, aggregate internals, storage keys, actor IDs, or route names.
  - [ ] Preserve the current fail-closed server posture until a real endpoint can pass all denial and freshness tests; do not remove the intentional server startup failure without replacing it with guarded API registration.

- [ ] Implement tenant-scoped filtering and pagination semantics. (AC: 2, 3)
  - [ ] Apply tenant scope before filters, counts, ordering, pagination cursors, or recent-activity selection are calculated.
  - [ ] Support filters only over projection fields approved by Story 1.7: external business identifier, project reference, folder reference, lifecycle state, date range, recent activity, and participant reference.
  - [ ] Define filter semantics in the public contract or tests before implementation: exact-match behavior, case/culture handling, null/empty handling, invalid combinations, deterministic ordering, bounded page size, and whether date range applies to created time, lifecycle transition time, last message activity, or another approved projected timestamp.
  - [ ] Make pagination metadata permission-safe: no total counts, ordering gaps, next-page existence, or cursor details may imply inaccessible records unless policy explicitly allows that disclosure.
  - [ ] Treat pagination tokens, if introduced, as opaque, tenant-scoped, caller-context-bound, and invalidated or safely denied when authorization, visibility, projection freshness, or filter context changes between page requests.
  - [ ] Keep list results as summaries with trust/freshness preview fields, not transcript snippets as the primary selection mechanism.

- [ ] Preserve non-disclosure and provider-session independence. (AC: 1, 3, 5, 6)
  - [ ] Map unauthorized, nonexistent, cross-tenant, hidden-by-isolation, stale-tenant-projection, and unavailable-projection cases to documented content-safe results.
  - [ ] Use the same external response shape for unauthorized, nonexistent, cross-tenant, inaccessible, and hidden-by-isolation detail reads unless an existing approved policy explicitly permits disclosure. Internal diagnostics may differ only behind non-public telemetry/log boundaries that do not expose protected record existence.
  - [ ] Ensure denial responses do not reveal titles, participant display names, snippets, timestamps, message counts, attachment counts, business references, provider metadata, tenant identifiers, or whether the protected record exists.
  - [ ] Prove retrieval by `ConversationId` and business-context listing work without provider session authority. Provider IDs may participate only as bounded correlation metadata when authorized.
  - [ ] Prove provider session IDs cannot authorize access, select tenant scope, widen list results, bypass business-context filters, or resolve a conversation before tenant access has succeeded.
  - [ ] Do not hydrate Party display/status data in this story unless Story 1.9 has already established the adapter. Stable `PartyId` references and safe unresolved/unavailable placeholders are sufficient for this story.

- [ ] Add focused query, contract, and boundary tests. (AC: 1-6)
  - [ ] Add contract tests under `tests/Hexalith.Conversations.Contracts.Tests` proving query/result DTOs serialize cleanly, include freshness and version metadata, keep identifiers distinct, and expose no EventStore terms.
  - [ ] Add server/query tests under `tests/Hexalith.Conversations.Server.Tests` proving tenant access is called before projection reads, existence checks, filter evaluation against stored data, provider-session correlation, and cursor resolution; denied requests must not touch projection storage.
  - [ ] Add list-filter tests for external identifier, project, folder, lifecycle, date range, recent activity, participant reference, pagination cursor, and mixed-tenant projection poison data.
  - [ ] Add denial tests for missing tenant, malformed tenant, disabled/unknown tenant, stale tenant projection, unavailable tenant projection, cross-tenant ID guessing, nonexistent conversation, inaccessible business context, stale projection, rebuilding projection, unavailable projection, hidden/redacted records, and provider-session loss.
  - [ ] Add response-shape equivalence tests for unauthorized, nonexistent, cross-tenant, inaccessible, and hidden detail reads, and pagination leakage tests for filtered-out records at page boundaries.
  - [ ] Add boundary tests that inspect `.csproj` XML, serialized response shapes, validation errors, route/openapi metadata if generated, exception-to-client mapping, and public contract property names so EventStore, Dapr, Tenants, Parties, actor/pubsub, stream, storage, projection implementation, and provider-authority details do not leak into `Contracts` or public API responses.

- [ ] Validate the implementation scope. (AC: 5)
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run `dotnet restore`, `dotnet build`, and `dotnet test .\Hexalith.Conversations.slnx` if assets are stale.
  - [ ] Do not run recursive submodule initialization. Root-level sibling module reads are enough for local pattern checks.
  - [ ] Leave `sprint-status.yaml` untouched during dev-story unless the dev workflow owns the status transition.

## Dev Notes

### Scope Boundary

Story 1.8 is a read/query boundary story. It retrieves and lists tenant-scoped conversation records from derived projections, after tenant authorization and with explicit freshness/trust context. It must not implement write-side mutation, new aggregate events, idempotency storage, projection rebuilding, tenant projection ingestion, Party hydration display policy, governance mutation, event publication, FrontComposer UI, conformance signing, or provider-specific session recovery. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.8: Retrieve and List Conversations by Tenant Business Context`]

This story is intentionally downstream of Stories 1.2, 1.5, and 1.7. In the current sprint status, Story 1.6 is still `backlog`, and some ready-for-dev predecessor stories may still be unimplemented. A dev agent must treat missing prerequisite implementations as blockers or placeholders, not as permission to invent alternate APIs, permissive authorization, or a transcript store. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant-Safe Conversation Record`]

### Architecture Compliance

Read paths should prefer projection-backed records with explicit trust metadata over raw aggregate replay. Ordinary reads must not reconstruct unbounded EventStore history on demand; heavy verification, rebuild, export, and temporal reconstruction are separate workflows. [Source: `_bmad-output/planning-artifacts/architecture.md#Operational Trust Risks`]

Projection reads are derived and repairable, never authoritative. EventStore remains the only durable write-side source of truth, and projections must not introduce facts that cannot be reconstructed from EventStore plus approved read-time hydration sources. If derived state disagrees with authoritative replay, query responses must surface stale/invalid/rebuilding/unavailable state instead of presenting the projection as current. [Source: `_bmad-output/planning-artifacts/architecture.md#Projection & Read-Model Ownership`; `_bmad-output/planning-artifacts/architecture.md#Data Boundaries`]

Tenant access fails closed before projection access. Do not call Tenants synchronously on the hot read path as the source of truth, and do not trust JWT tenant claims alone. Use the local Tenants projection/service from Story 1.5; missing, stale, unavailable, inconsistent, disabled, unknown, or insufficient tenant state denies or omits data safely. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/planning-artifacts/architecture.md#Tenant Isolation Architecture`]

### Ready-For-Dev Gate Decisions

Projection freshness blocking semantics are decided. The canonical freshness vocabulary is `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`. If this story does not explicitly declare an exception for an operation, only `Current` is acceptable for trust-bearing action; stale, rebuilding, and unavailable states must not be silently treated as complete/current. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]

Party hydration degraded states are decided. Command-time participant validation fails closed, while authorized reads may degrade display hydration to safe unresolved/unavailable state. This story can return stable Party IDs and participant metadata already present in projections; Story 1.9 owns current display/status hydration from Parties and upstream sources. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`; `_bmad-output/planning-artifacts/epics.md#Story 1.9: Resolve Parties and Upstream References at Read Time`]

The EventStore envelope decision is decided. Conversations owns domain schemas and public contract versioning, while EventStore envelope details remain inherited infrastructure and must not appear in adopter APIs or query responses. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#EventStore envelope stability and evolution ownership`]

### Pre-Dev Party-Mode Review Decisions

The 2026-05-18 party-mode review clarified Story 1.8 without changing product scope:

- Dependency gates are explicit. Story 1.8 may compose approved contracts, tenant-access abstractions, and projection/read abstractions from predecessor stories, or return safe placeholder/unavailable results when those predecessor contracts intentionally expose placeholder fields. It must not create substitute tenant authorization, projection storage, query-owned transcript tables, or parallel identity/freshness/error DTO families.
- Authorization ordering is a hard boundary. Tenant access must succeed before any projection lookup, existence check, stored-data filter evaluation, count, cursor resolution, provider-session correlation lookup, participant index lookup, or freshness metadata read. Tests should use spies/fakes that fail on any pre-authorization read.
- Non-disclosure is same-shape by default. Unauthorized, nonexistent, cross-tenant, inaccessible, and hidden-by-isolation detail reads should collapse to the same external safe result shape unless a later approved policy allows disclosure. List responses and pagination metadata must be accessible-result-relative only.
- Freshness states are externally observable only after authorization permits them. `Stale`, `Rebuilding`, `Unavailable`, and `Redacted` must not confirm protected record existence to unauthorized callers; unknown freshness or visibility states fail closed.
- Provider session data is correlation-only. Provider session IDs, thread IDs, storage partition names, route names, and EventStore stream names cannot authorize, select tenant scope, widen list results, or appear in adopter-facing public contracts.
- Participant filtering is opaque-reference filtering only. Story 1.8 may filter by participant references already present in approved projections, but Party display/status hydration, personal-data lookup, email/name matching, and current Party policy decisions remain Story 1.9 scope.

### Response and Filtering Matrix

| Scenario | External detail result | List behavior | Freshness/disclosure rule |
| --- | --- | --- | --- |
| Authorized and current | Conversation details contract | Accessible summaries only | `Current` may expose approved public fields. |
| Authorized and stale/rebuilding/unavailable | Safe typed result with approved next-action metadata | Safe empty or degraded result per contract | Do not present stale/incomplete data as current. |
| Authorized and redacted/hidden by policy | Safe redacted or hidden result | Omit or safe redacted summary per approved contract | No snippets, counts, participant names, or protected metadata. |
| Unauthorized, invalid tenant, cross-tenant, inaccessible, or nonexistent | Same content-safe shape by default | No accessible matches | Do not reveal existence, timestamps, counts, ordering gaps, business references, provider metadata, or freshness of protected records. |

Filter behavior must be deterministic and contract-defined before implementation: allowed fields, exact/case/culture matching, null/empty handling, invalid combinations, date/recent-activity source field, bounded page size, token opacity, and token invalidation after authorization, visibility, projection freshness, or filter-context changes.

### Current Repository State and Previous Story Intelligence

At story creation time, the repository has scaffold projects and boundary tests. `src/Hexalith.Conversations.Server/Program.cs` still throws an intentional `NotImplementedException` because the server has no API surface yet. `src/Hexalith.Conversations.Contracts`, `src/Hexalith.Conversations`, and `src/Hexalith.Conversations.Server` are marker-oriented and do not yet contain the Story 1.8 runtime contracts/queries/projections. [Source: `src/Hexalith.Conversations.Server/Program.cs`; `src/Hexalith.Conversations.Contracts/ContractsAssemblyMarker.cs`; `src/Hexalith.Conversations/ConversationsAssemblyMarker.cs`]

Previous Story 1.3 guidance is directly relevant: aggregate and event payloads must remain stable-ID and metadata only; provider/external IDs are correlation/search metadata only; read projections and read APIs are later-slice behavior; boundary tests should inspect `.csproj` XML because marker assemblies can make compiled-reference checks pass vacuously. Carry these lessons into query contracts and server tests. [Source: `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md#Current Repository State and Previous Story Intelligence`; `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md#Domain Model Guardrails`]

Recent git history shows the project has mainly scaffold, validation, and Story 1.2 documentation work (`062bee3`, `4479ced`, `c218a1e`). That reinforces keeping Story 1.8 scoped to query contracts/handlers/tests and avoiding broad infrastructure rewrites. [Source: `git log -5 --pretty=format:'%h %s'`]

### Query Contract Guidance

Expected public contract concepts belong under `src/Hexalith.Conversations.Contracts`, likely in folders such as `Queries`, `Projections`, `Results`, `Errors`, `TrustStates`, and `Versioning` depending on what Story 1.2 established. Use the established names if they exist; do not create duplicate versions of identity, trust, freshness, or error contracts. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#File and Test Placement`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]

`ConversationDetailsV1` should expose authorized conversation identity, participant set, ordered message timeline metadata, attachment/file references, governance state placeholders where available, business references, provider correlation metadata, and freshness context. It must not expose EventStore stream names, event positions, aggregate snapshots, projection storage keys, Dapr actor IDs, or raw projection implementation details. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.8: Retrieve and List Conversations by Tenant Business Context`; `_bmad-output/planning-artifacts/architecture.md#API Boundaries`]

`ConversationSummaryV1` or equivalent list rows should show enough trust posture for Find -> Read -> Trust selection without becoming transcript snippets. Rows should expose safe identity, business context, freshness, redaction state if already modeled, participant resolution state if already modeled, and citation availability placeholder if already modeled. Do not base selection on transcript-like snippets alone. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Search And Filtering Patterns`; `_bmad-output/planning-artifacts/ux-design-specification.md#Find Read Trust Visual Rhythm`]

### Filtering and Pagination Rules

Allowed Story 1.8 filter concepts are external business identifier, project reference, folder reference, lifecycle state, date range, recent activity, and participant reference. Apply tenant authorization and tenant projection freshness before evaluating these filters. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.8: Retrieve and List Conversations by Tenant Business Context`; `_bmad-output/planning-artifacts/prd.md#Conversation Lifecycle`]

Permission safety applies to counts, facets, ordering, pagination, autocomplete, recent items, and timing. Pagination metadata must not leak inaccessible records through total counts, next-page hints, cursor contents, ordering gaps, empty-page behavior, or latency patterns. Use safe empty/denial semantics such as "no accessible matches" in UI-facing guidance; API contracts should convey the same content-safe outcome through typed results. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Disclosure Surface Inventory`; `_bmad-output/planning-artifacts/ux-design-specification.md#Search And Filtering Patterns`]

External business identifiers are tenant-scoped discovery keys only. They must remain distinct from internal `ConversationId` and from stable upstream references such as `ProjectId`, `FolderId`, `FileId`, and `PartyId`. [Source: `_bmad-output/planning-artifacts/prd.md#Business Context And References`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### Non-Disclosure and Error Semantics

Unauthorized, nonexistent, cross-tenant, and hidden-by-tenant-isolation outcomes must be indistinguishable to non-privileged callers unless policy explicitly permits disclosure. Error/problem/result shapes must not reveal target tenant details, Party details, conversation existence, redacted content, provider payloads, business references, timestamps, counts, or ordering gaps. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

Typed errors should reuse the Story 1.2 vocabulary. At minimum, Story 1.8 needs safe mappings for missing tenant binding, tenant isolation violation, stale tenant projection, projection unavailable/rebuilding/stale, aggregate hidden/not found, unsupported schema/projection version, and query validation failure. Do not add ad hoc strings if a shared enum/result contract exists. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Typed Error and Trust Vocabulary`; `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`]

### File and Test Placement

Expected production files should extend existing folders if they already exist; otherwise use the architecture-approved boundaries:

- `src/Hexalith.Conversations.Contracts/Queries`
- `src/Hexalith.Conversations.Contracts/Projections`
- `src/Hexalith.Conversations.Contracts/Results`
- `src/Hexalith.Conversations.Contracts/Errors`
- `src/Hexalith.Conversations.Server/Api`
- `src/Hexalith.Conversations.Server/Queries`
- `src/Hexalith.Conversations.Server/Projections`
- `src/Hexalith.Conversations.Server/TenantAccess`

Expected tests belong under:

- `tests/Hexalith.Conversations.Contracts.Tests`
- `tests/Hexalith.Conversations.Server.Tests`
- `tests/Hexalith.Conversations.IntegrationTests` only for narrow end-to-end read-boundary checks that do not require external cloud resources or nested submodules.

[Source: `_bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]

### Existing Code To Preserve

`src/Hexalith.Conversations.Server/Program.cs` currently fails closed by design. Story 1.8 may replace or extend that fail-closed behavior only when API registration, tenant authorization, projection-read handling, and denial tests are in place. Do not make the server runnable with unguarded placeholder endpoints. [Source: `src/Hexalith.Conversations.Server/Program.cs`; `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`]

Existing boundary tests assert that Contracts stays infrastructure-free and Server references only approved scaffold projects. Update these tests to reflect real query implementation, but preserve their intent: Contracts must not reference EventStore, Dapr, FrontComposer, ASP.NET Core, HTTP clients, Tenants, Parties, Projects, or Folders runtime packages directly. Server may add approved application/query dependencies only in the proper boundary. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`; `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`]

### Local Pattern and Library Notes

Use .NET `net10.0`, nullable enabled, implicit usings, warnings as errors, and Central Package Management. NuGet Central Package Management keeps versions in `Directory.Packages.props`; project `PackageReference` entries should not include `Version` attributes. Official Microsoft docs also confirm `net10.0` is the base TFM for .NET 10 SDK-style projects. [Source: `Directory.Build.props`; `Directory.Packages.props`; Microsoft Learn NuGet Central Package Management, https://learn.microsoft.com/nuget/consume-packages/central-package-management; Microsoft Learn Target frameworks, https://learn.microsoft.com/dotnet/standard/frameworks]

Current package baselines include xUnit v3 `3.2.2`, Shouldly `4.3.0`, Aspire.Hosting `13.2.2`, Microsoft.Extensions `10.4.0`, and OpenTelemetry `1.15.x`. Do not upgrade packages as part of this story unless a local dependency is missing and the version is added centrally with tests. [Source: `Directory.Packages.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]

EventStore projection samples show projection handlers rebuilding read state from `ProjectionRequest` event sequences and returning `ProjectionResponse` with opaque state. Conversations must wrap any EventStore projection mechanics behind Conversations-owned contracts and add freshness/trust metadata rather than leaking `ProjectionRequest`, `ProjectionResponse`, event type names, or opaque JSON state directly to adopters. [Source: `Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Counter/Projections/CounterProjectionHandler.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionRequest.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionResponse.cs`]

### Anti-Reinvention Warnings

- Do not create `Messages`, `ChatTranscripts`, `ConversationMemory`, or query-owned durable tables as source-of-truth state.
- Do not reconstruct read responses by directly exposing EventStore streams, snapshots, event sequence numbers, or raw projection JSON.
- Do not call Tenants or Parties directly from aggregate/domain logic.
- Do not persist Party display names, emails, avatars, contact values, person details, organization details, or raw upstream problem details in projections unless a later approved policy explicitly permits it.
- Do not treat provider session IDs as authority for retrieval or continuation. Provider data is correlation metadata only.
- Do not return denied result counts, page gaps, recent-item traces, snippets, browser/UI labels, telemetry labels, or timing-sensitive details that imply protected records exist.
- Do not add FrontComposer or admin UI behavior here; Story 1.8 creates the API/query substrate that later UI/operator stories consume.

[Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/architecture.md#Examples To Avoid`; `_bmad-output/planning-artifacts/ux-design-specification.md#Disclosure Surface Inventory`]

### Validation

Run focused contract/server tests first, then the full solution test command. Validation must not require Aspire launch, Dapr sidecars, tenant seed data, provider credentials, production secrets, external cloud resources, or nested submodule initialization. Add test fixtures/fakes only under approved Testing or test project boundaries and keep them deterministic. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.8: Retrieve and List Conversations by Tenant Business Context`]

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.8: Retrieve and List Conversations by Tenant Business Context`
- `_bmad-output/planning-artifacts/architecture.md#Operational Trust Risks`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/prd.md#Conversation Lifecycle`
- `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Search And Filtering Patterns`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`
- `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md`
- `src/Hexalith.Conversations.Server/Program.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`
- `Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Counter/Projections/CounterProjectionHandler.cs`
- Microsoft Learn NuGet Central Package Management: https://learn.microsoft.com/nuget/consume-packages/central-package-management
- Microsoft Learn Target frameworks: https://learn.microsoft.com/dotnet/standard/frameworks

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 1.8 was explicitly created while prerequisite predecessor stories remain unimplemented; implementation must verify or complete prerequisites before coding runtime behavior.

### File List

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-18: Party-mode review applied dependency gate, authorization-ordering, non-disclosure, pagination, provider-session, and boundary-test clarifications.

## Party-Mode Review

- ISO date and time: 2026-05-18T20:42:20Z
- Selected story key: 1-8-retrieve-and-list-conversations-by-tenant-business-context
- Command/skill invocation used: `/bmad-party-mode 1-8-retrieve-and-list-conversations-by-tenant-business-context; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: Reviewers agreed Story 1.8 had the right read-boundary intent but needed sharper pre-dev constraints around dependency gates, fail-closed tenant authorization before any read-side access, same-shape non-disclosure for unauthorized/nonexistent/cross-tenant outcomes, freshness-state existence leakage, provider-session independence, filter semantics, pagination leakage, and public boundary tests.
- Changes applied: Added AC 6 for authorization-before-read ordering; added prerequisite decision recording; clarified filter semantics, cursor safety, provider-session negative tests, same-shape denial behavior, response-shape equivalence tests, boundary leak surfaces, pre-dev party-mode decisions, and a response/filtering matrix.
- Findings deferred: Human product/architecture decisions remain for whether unauthorized and nonexistent outcomes use `Forbidden`, `NotFound`, or another safe contract state; whether stale authorized reads may return data; whether `Redacted` means metadata-only visibility or whole-record suppression; exact lifecycle/recent-activity semantics across providers; external business identifier uniqueness scope; approximate count policy; timing-leak thresholds; cursor invalidation policy; Party display hydration; and projection rebuild/remediation behavior.
- Final recommendation: ready-for-dev
