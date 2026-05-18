# Story 1.9: Resolve Parties and Upstream References at Read Time

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want conversation reads to hydrate participant and upstream reference display data from canonical sources,
so that stored conversation events remain stable and privacy-safe while users still see current authorized context.

## Acceptance Criteria

1. Given a conversation read model contains stable `PartyId`, `ProjectId`, `FolderId`, and `FileId` references, when an authorized read request is composed, then the read boundary uses Conversations-owned adapters to hydrate authorized display/status data from upstream canonical sources, and durable conversation events and projections remain based on stable IDs rather than mutable upstream display data.
2. Given a Party can be resolved for the caller and tenant scope, when participant display data is hydrated, then the response includes only authorized participant display/status fields allowed by policy, and it does not persist or expose unauthorized Parties personal data, contact values, identifiers, person details, or organization details.
3. Given an upstream Party, Project, Folder, or File reference is deleted, inaccessible, stale, unavailable, or policy-filtered, when the conversation is read, then the response uses a safe degraded, unresolved, redacted, or unavailable state, and it does not mutate historical events or imply that inaccessible upstream data exists unless policy allows disclosure.
4. Given upstream hydration is slow or partially unavailable, when a read response is composed, then the system avoids N+1 behavior through batching or documented bounded calls where available, and authorized reads may degrade display hydration while command-time participant validation remains fail-closed.
5. Given hydration tests run, when Party rename, deleted Party, inaccessible Party, unavailable Parties adapter, stale upstream reference, and unauthorized upstream reference scenarios are exercised, then tests prove read-time display updates without event rewrites, safe degradation, no Party personal-data persistence, and no cross-tenant disclosure.

## Tasks / Subtasks

- [ ] Define the response-safe hydration contract surface in `src/Hexalith.Conversations.Contracts`. (AC: 1-3)
  - [ ] Add or extend projection/read DTOs so conversation detail responses can carry stable references plus hydration state for Party, Project, Folder, and File references.
  - [ ] Use approved trust/freshness vocabulary where it applies: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`.
  - [ ] Add explicit unresolved/degraded fields that do not require clients to infer safety from `null`, empty display text, missing objects, or HTTP status alone.
  - [ ] Keep contract DTOs infrastructure-free: no `Hexalith.Parties`, `Hexalith.Projects`, `Hexalith.Folders`, `HttpClient`, EventStore, Dapr, FrontComposer, or ASP.NET Core references.

- [ ] Add Conversations-owned upstream adapter abstractions under `src/Hexalith.Conversations.Server`. (AC: 1-4)
  - [ ] Introduce server-side interfaces such as `IParticipantDirectory`, `IBusinessReferenceDirectory`, or capability-specific adapters under `Hydration`, `Participants`, or `References`.
  - [ ] Wrap upstream clients behind these adapters; do not let controller/query composition code call Parties/Folders/Projects clients directly.
  - [ ] Normalize upstream success, forbidden, not-found, gone/deleted, stale, timeout, throttled, and unavailable outcomes into Conversations-owned hydration states.
  - [ ] Ensure adapters accept tenant scope, caller context, correlation ID, and cancellation token; never accept tokens, claims, raw tenant authorization state, or user-editable authority fields as DTO payload.

- [ ] Compose hydrated conversation read responses after tenant and projection checks. (AC: 1-4)
  - [ ] Run tenant access before projection read and before any upstream hydration attempt.
  - [ ] Use Story 1.7/1.8 projection read models as the input; do not hydrate directly from EventStore streams or aggregate replay for ordinary reads.
  - [ ] Preserve stable IDs from the projection and add transient display/status details only to the response object.
  - [ ] Keep hydrated Party, Project, Folder, and File display data out of durable events, projections, logs, traces, caches, transcript-shaped artifacts, and testing snapshots that represent persisted state.
  - [ ] Keep unauthorized, nonexistent, deleted, and cross-tenant references indistinguishable unless an approved policy permits disclosure.

- [ ] Implement bounded hydration behavior and failure mapping. (AC: 3, 4)
  - [ ] Batch Party lookups where the upstream API supports it; if only single lookup exists, use request-scoped deduplication and document the remaining bound.
  - [ ] Do not add a durable cache for hydrated Party personal data. Any cache that outlives a request requires ADR approval, TTL, tenant scope, redaction policy context, and tests.
  - [ ] Map upstream timeouts, throttling, and adapter errors to safe `Unavailable` or degraded hydration state without leaking raw upstream problem details.
  - [ ] Preserve the distinction between command-time participant validation and read-time hydration: writes fail closed when participant validation cannot be trusted; authorized reads may degrade display hydration by policy.

- [ ] Add focused tests for hydration, degradation, and non-disclosure. (AC: 1-5)
  - [ ] Add tests in `tests/Hexalith.Conversations.Server.Tests/Hydration` for Party rename, deleted/erased Party, inactive Party, inaccessible Party, adapter unavailable, stale upstream reference, and unauthorized upstream reference.
  - [ ] Add tests proving response hydration updates when upstream display data changes without rewriting conversation events or projection stored references.
  - [ ] Add tests proving forbidden Parties personal data is absent from hydrated responses unless explicitly allowed: contact channels, identifiers, person details, organization details, name history, and raw upstream problem details.
  - [ ] Add cross-tenant poison tests using sentinel values in inaccessible upstream data and assert those values never appear in responses, logs, errors, serialized DTOs, or test snapshots.
  - [ ] Add batching/deduplication tests so repeated references in a timeline do not trigger unbounded N+1 calls.
  - [ ] Extend boundary tests to inspect `.csproj` XML for forbidden dependencies in `Contracts` and to ensure upstream client dependencies, if added, stay in `Server` or another approved adapter boundary.

- [ ] Validate the implementation scope. (AC: 5)
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run `dotnet restore`, `dotnet build`, and `dotnet test .\Hexalith.Conversations.slnx` if assets are stale.
  - [ ] Do not run recursive submodule initialization. Root-level sibling module reads are enough when adapter contract details need inspection.
  - [ ] Do not add FrontComposer UI, admin evidence drawers, citation copy, temporal evidence links, exports, durable caches, projection rebuild jobs, governance commands, or release manifest aggregation in this story.
  - [ ] Leave `sprint-status.yaml` untouched during dev-story unless the dev workflow owns the status transition.

## Dev Notes

### Scope Boundary

Story 1.9 is read-time enrichment only. It hydrates display/status context for stable upstream references already present in projection-backed conversation reads. It must not create new durable conversation state, rewrite historical events, persist Party personal data into projections, add transcript tables, or make upstream modules authoritative for Conversations history. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.9: Resolve Parties and Upstream References at Read Time`; `_bmad-output/planning-artifacts/architecture.md#Data Boundaries`]

This story assumes the read-model and retrieve/list foundations from Stories 1.7 and 1.8 exist. If those stories are not implemented on the branch, do not invent a parallel read stack just to complete hydration. Implement only the reusable contract/adapter pieces that can attach cleanly to the projection-backed read boundary, or stop and sequence the missing prerequisites first. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`; `_bmad-output/planning-artifacts/epics.md#Story 1.8: Retrieve and List Conversations by Tenant Business Context`]

Story 1.4, Story 1.4.1, and Story 1.4.2 define participant, message, file, and business-reference events that this read path consumes. If their contracts or projection fields are absent, do not substitute mutable upstream records for missing stable IDs. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.4: Add Conversation Participants with Stable Party Attribution`; `_bmad-output/planning-artifacts/epics.md#Story 1.4.2: Attach File and Upstream Business References`]

### Architecture Compliance

Ordinary reads must follow the approved flow: tenant check -> projection read -> read-time hydration -> permission-safe DTO. Read paths should prefer projection-backed records with explicit trust metadata over raw aggregate replay, and command success must not imply immediate query visibility. [Source: `_bmad-output/planning-artifacts/architecture.md#Integration Points`; `_bmad-output/planning-artifacts/architecture.md#Loading And Freshness Patterns`]

`Hexalith.Parties` owns Party identity and personal data. Conversations may store stable Party IDs and transiently hydrate authorized display/status data at read time through a Conversations-owned adapter. Party display names, contact channels, identifiers, person details, organization details, and raw Parties problem details must not become durable conversation state. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#party-hydration-degraded-states`]

Project, Folder, File, and attachment references are also stable IDs owned upstream. v1 does not subscribe to upstream lifecycle events for full cross-module orchestration; read-time resolution uses the upstream module's current canonical state where available and falls back to safe degraded states when unavailable or unauthorized. [Source: `_bmad-output/planning-artifacts/prd.md#Business Context And References`; `_bmad-output/planning-artifacts/architecture.md#External Integrations`]

### Current Repository State and Previous Story Intelligence

The current Conversations repository is still mostly scaffold/marker code. `src/Hexalith.Conversations.Server` references only `Contracts` and domain, and current server tests assert no EventStore runtime or Dapr dependency. Hydration implementation will likely add the first real server-side adapter boundary, so update boundary tests intentionally and inspect `.csproj` XML, not only compiled assembly references. [Source: `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`; `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`; `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Current Repository State and Previous Story Intelligence`]

Previous story files are ready-for-dev, not completed implementation evidence. Treat their guidance as planned contract and aggregate shape, but verify actual source before reusing types. Do not assume Story 1.2/1.3 files exist until the branch contains them. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`; `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md`]

### Upstream Adapter Intelligence

`Hexalith.Parties.Client` currently exposes `IPartiesQueryClient.GetPartyAsync`, `ListPartiesAsync`, and `SearchPartiesAsync`. `PartyDetail` includes `[PersonalData]` fields such as `DisplayName`, `SortName`, `NameHistory`, plus `PersonDetails`, `OrganizationDetails`, `ContactChannels`, `Identifiers`, consent, restriction, activity, and erasure state. Conversations must whitelist the response fields it returns instead of passing `PartyDetail` through. [Source: `Hexalith.Parties/src/Hexalith.Parties.Client/Abstractions/IPartiesQueryClient.cs`; `Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs`; `Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyIndexEntry.cs`]

The Parties client is configured through `AddPartiesClient`, `HttpClient`, and `PartiesClientOptions`, and it validates a configured tenant. If reused, wrap it inside Conversations hydration adapters so the read boundary owns tenant/caller mapping, result shaping, failure mapping, and correlation behavior. [Source: `Hexalith.Parties/src/Hexalith.Parties.Client/Extensions/PartiesClientServiceCollectionExtensions.cs`; `Hexalith.Parties/src/Hexalith.Parties.Client/HttpPartiesQueryClient.cs`]

`Hexalith.Folders.Client` has generated query methods such as `GetFolderLifecycleStatusAsync`, `GetFolderFileMetadataAsync`, and workspace/status diagnostics with freshness headers. Treat generated clients as upstream transport details behind a Conversations adapter, and expose only Conversations-safe reference display/status states in read responses. [Source: `Hexalith.Folders/src/Hexalith.Folders.Client/Generated/HexalithFoldersClient.g.cs`]

The local `Hexalith.Projects` folder is an umbrella-style checkout rather than a direct `src` module in this workspace. If Project reference hydration is required before a local typed client exists, keep the Conversations adapter interface in place and use an in-memory/fake implementation for tests rather than hard-coding assumptions about a missing Projects client. [Source: `Hexalith.Projects/_bmad-output/project-context.md`; local workspace inspection]

### Security and Privacy Guardrails

- Hydration must happen after tenant authorization and projection freshness checks.
- Response DTOs may include stable IDs and authorized display/status fields only; do not return raw upstream DTOs.
- Do not log upstream display names, Party personal data, raw problem details, inaccessible resource IDs, search terms, or protected business-reference values.
- Deleted, erased, inaccessible, forbidden, stale, and unavailable references must produce explicit safe states; absence must not look like success.
- Cross-tenant, unauthorized, and nonexistent references remain indistinguishable unless policy explicitly permits disclosure.
- Hydrated values are response-scoped by default. Any durable cache, export, evidence artifact, or diagnostic payload containing hydrated display data requires explicit policy and ADR coverage.

[Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/planning-artifacts/ux-design-specification.md#Interaction Design Patterns`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### Performance and Resilience Guardrails

Hydration must not produce unbounded per-message upstream calls. Prefer upstream bulk APIs where available; otherwise deduplicate IDs per request, bound concurrency, propagate cancellation, and expose degraded hydration state when dependencies are slow or unavailable. The PRD warm-cache target for opening a conversation is P95 <= 500 ms for up to 500 messages, 20 human participants, 5 AI agents, and 50 concurrent opens/sec/tenant, so hydration latency must be measured separately from projection read latency. [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`; `_bmad-output/planning-artifacts/architecture.md#Performance And Scalability Strategy`]

If adding HTTP resilience for upstream clients, keep package versions centralized in `Directory.Packages.props`. The local repo currently pins `Microsoft.Extensions.Http.Resilience` at `10.4.0`; NuGet lists `10.6.0` as current on 2026-05-18, so do not silently upgrade as part of this story unless the change is deliberate and the whole solution is validated. Microsoft guidance for `Microsoft.Extensions.Http.Resilience` also warns to avoid stacking multiple resilience handlers; configure one standard or custom handler per client. [Source: `Directory.Packages.props`; Microsoft Learn: `https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience`; NuGet: `https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience`]

The repo targets .NET `10.0` with SDK `10.0.300`. .NET 10 includes Microsoft.Testing.Platform support in `dotnet test`, but the current test projects use xUnit v3 through the existing runner/package setup; keep validation aligned with the repo unless a separate test-platform migration story approves a change. [Source: `global.json`; `Directory.Packages.props`; Microsoft Learn: `https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview`]

### File and Test Placement

Expected production files, depending on prerequisite story state, belong under:

- `src/Hexalith.Conversations.Contracts/Projections` or `ReadModels` for response-safe hydrated DTOs and hydration state contracts.
- `src/Hexalith.Conversations.Server/Hydration` for adapter interfaces, result mapping, batching/deduplication, and upstream client wrappers.
- `src/Hexalith.Conversations.Server/Queries` or existing read boundary folders established by Story 1.8 for conversation read composition.
- `src/Hexalith.Conversations.Server/Configuration` only for adapter options and DI registration.

Expected tests belong under:

- `tests/Hexalith.Conversations.Server.Tests/Hydration`
- `tests/Hexalith.Conversations.Server.Tests/Queries`
- `tests/Hexalith.Conversations.Contracts.Tests/Projections`
- `tests/Hexalith.Conversations.Server.Tests/Boundaries`

Shared deterministic factories may be added to `src/Hexalith.Conversations.Testing` only when reusable across future stories and free of runtime behavior.

### Anti-Reinvention Warnings

- Do not create a transcript table, durable participant-display snapshot, or cache-backed read authority.
- Do not copy Parties, Folders, Projects, or Tenants contracts into Conversations.
- Do not put upstream HTTP calls, tenant authorization, or hydration inside `ConversationAggregate` or domain state.
- Do not expose upstream DTOs, raw problem details, EventStore projection internals, route names, generated client names, or hydration-source internals as public Conversations contracts.
- Do not add UI trust components, citation copy behavior, or admin drawers here; Epic 3 owns operator UI surfaces.
- Do not treat provider IDs, Party display names, project labels, folder paths, or file names as stable identity.

### Validation

Validation must stay local and deterministic. Unit tests should use fake adapter implementations and sentinel data; integration tests with real upstream services can be added only when the required AppHost/Dapr/EventStore topology exists and remains optional for normal unit validation. Tests must not require production secrets, provider credentials, initialized nested submodules, external cloud resources, or live upstream services. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`]

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.9: Resolve Parties and Upstream References at Read Time`
- `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`
- `_bmad-output/planning-artifacts/epics.md#Story 1.8: Retrieve and List Conversations by Tenant Business Context`
- `_bmad-output/planning-artifacts/prd.md#Business Context And References`
- `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Integration Points`
- `_bmad-output/planning-artifacts/architecture.md#Process Patterns`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `Hexalith.Parties/src/Hexalith.Parties.Client/Abstractions/IPartiesQueryClient.cs`
- `Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs`
- `Hexalith.Folders/src/Hexalith.Folders.Client/Generated/HexalithFoldersClient.g.cs`
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
