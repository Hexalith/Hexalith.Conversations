# Story 1.7: Project Conversation Read Models with Freshness Metadata

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want tenant-safe conversation read models with explicit freshness metadata,
so that consumers can read conversation state without confusing stale, rebuilding, unavailable, or hidden data for current truth.

## Acceptance Criteria

1. Given conversation-created, participant-added, message-appended, reference-attached, metadata-updated, and lifecycle events are persisted, when projection handlers process the ordered event stream, then they derive tenant-scoped read models for conversation summary and conversation detail, and handlers tolerate duplicate, replayed, and out-of-order delivery according to documented projection behavior.
2. Given a projection read model is returned, when the consumer inspects it, then it includes freshness metadata such as projection version or cursor, last applied event position or timestamp, projection generated timestamp, stale indicator, lag duration where available, and freshness state, and freshness states distinguish current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
3. Given projection metadata is missing, contradictory, stale, or unavailable, when a read boundary or UI-facing contract formats the result, then it downgrades trust to unknown, stale, rebuilding, unavailable, or hidden rather than presenting the read model as current, and governed actions depending on current projection state are blocked or marked unavailable.
4. Given projection handlers materialize conversation timelines, when messages, participants, file references, provider correlation metadata, and business references are projected, then the read model contains only tenant-authorized, content-safe fields and stable IDs, and it does not persist Party personal data, raw upstream records, file binaries, raw provider payloads, or EventStore internals.
5. Given projection tests run, when ordered replay, duplicate delivery, projection deletion/rebuild, stale metadata, unavailable store, and mixed-tenant poison events are exercised, then tests prove deterministic read-model reconstruction, freshness-state behavior, duplicate tolerance, and fail-closed tenant isolation.

## Tasks / Subtasks

- [ ] Confirm freshness and prerequisite gates before implementation. (AC: 1-5)
  - [ ] Link the existing readiness decision for Projection freshness blocking semantics and use only the approved vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`.
  - [ ] Treat `Current` as the only state accepted for trust-bearing decisions unless a later ADR explicitly permits a narrower degraded behavior.
  - [ ] Verify the branch contains or intentionally stubs the event/contract outputs from Stories 1.2, 1.3, 1.4, append-message/reference work, 1.5, and 1.6. If required event contracts or domain events are absent, add the smallest contract/domain fixtures needed for this story and record the dependency in tests; do not invent conflicting public names.

- [ ] Add or complete projection and freshness contracts in `src/Hexalith.Conversations.Contracts`. (AC: 2, 3, 4)
  - [ ] Add `Projections/ConversationSummaryProjectionV1.cs` and `Projections/ConversationDetailProjectionV1.cs`, or extend the Story 1.2 projection shells if already present.
  - [ ] Add a shared `ProjectionFreshnessV1` or equivalent contract with projection version/cursor, last applied event position or equivalent source cursor, last applied event timestamp, projection generated timestamp, optional lag duration, stale flag, freshness state, and safe reason code.
  - [ ] Keep public projection contracts in Conversations language. Do not expose EventStore envelopes, stream names, snapshots, expected revisions, raw projection topology, raw event payloads, or EventStore client types.
  - [ ] Represent timeline, participant, message, business reference, provider correlation, and file-reference data as stable IDs and content-safe metadata only. Do not include Party display names or upstream records in persisted projection contracts.

- [ ] Implement projection materialization under `src/Hexalith.Conversations.Server/Projections`. (AC: 1, 4, 5)
  - [ ] Add projection handler(s) for conversation-created, participant-added, message-appended, reference-attached, metadata-updated, and lifecycle events available on the branch.
  - [ ] Build summary and detail read models as derived, rebuildable state. Projection state must not become write-side authority and must be disposable/reconstructable from EventStore history.
  - [ ] Make handlers idempotent for duplicate/replayed events and deterministic for ordered replay. For out-of-order delivery, either buffer/reject/mark rebuilding according to the documented behavior; never silently produce a confident current read model from contradictory event order.
  - [ ] Store tenant scope on every projection record and reject or quarantine mixed-tenant poison events before mutation.
  - [ ] Do not add transcript tables, authoritative message stores, provider session stores, Memories/RAG indexes, export artifacts, UI state caches, or durable hydrated Party data in this story.

- [ ] Add the tenant-safe read boundary for projection results. (AC: 2, 3, 4)
  - [ ] Add query/read service behavior only as needed to return projection contracts with freshness metadata.
  - [ ] Check tenant access before projection read through the local tenant access boundary from Story 1.5. If Story 1.5 is not yet implemented on the branch, keep the read boundary fail-closed behind an interface/test fake rather than trusting request claims directly.
  - [ ] Return `Forbidden` or hidden-by-tenant-isolation semantics without revealing whether a protected conversation exists, including through counts, timestamps, business references, provider metadata, or pagination gaps.
  - [ ] Map missing, stale, rebuilding, unavailable, contradictory, or poisoned projection metadata to safe freshness states and block command availability metadata for actions requiring current projection state.

- [ ] Add deterministic rebuild and freshness behavior tests. (AC: 1-5)
  - [ ] Add tests under `tests/Hexalith.Conversations.Server.Tests/Projections` for ordered replay, duplicate event delivery, replayed event delivery, projection deletion/rebuild, out-of-order event behavior, stale metadata, unavailable projection store, contradictory metadata, and mixed-tenant poison events.
  - [ ] Add contract tests under `tests/Hexalith.Conversations.Contracts.Tests` proving projection contracts serialize with `System.Text.Json` web defaults, dates round-trip as ISO 8601-compatible `DateTimeOffset` values, and JSON property names do not expose EventStore or internal topology terms.
  - [ ] Add payload/property inspection tests proving read models do not persist Party personal data, file binaries, raw upstream records, provider prompt/response payloads, access tokens, claims, raw authorization state, or raw EventStore details.
  - [ ] Add boundary tests that inspect `.csproj` XML as well as compiled assembly references so forbidden dependencies cannot be hidden by unused marker assemblies.

- [ ] Document projection behavior and validation evidence. (AC: 1-5)
  - [ ] Add or update developer-facing docs explaining that read models are derived from EventStore history, include freshness metadata, and are not authoritative write state.
  - [ ] Document the duplicate/replay/out-of-order policy, rebuild behavior, and accepted freshness states for summary/detail projections.
  - [ ] Link Story 1.7 local evidence forward to Story 1.11 replay/schema-version work, Story 1.8 retrieve/list behavior, Story 3.x operator trust surfaces, Story 4.2 client behavior, and Story 6.2 projection-lag observability.
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run restore/build/test if assets are stale. Do not initialize nested submodules recursively.

## Dev Notes

### Scope Boundary

Story 1.7 owns derived summary/detail read models, projection handlers, projection freshness metadata, freshness-state downgrade behavior, and local projection tests. It does not own tenant access source-of-truth implementation, command idempotency storage, aggregate command behavior, publication contracts, Party/Project/Folder display hydration, admin UI components, conformance release packaging, export/evidence bundles, or EventStore schema evolution. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`; `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]

Projection freshness is unblocked by the readiness gate decided on 2026-05-17. The binding vocabulary is `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`. If an implementation path needs a public state or reason outside that list, stop for architecture clarification or ADR rather than adding a local enum value. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]

Stories 1.4.1 and 1.4.2 appear in the epics as append-message and reference slices, while sprint status currently tracks Story 1.4 as participant attribution and then Story 1.5/1.6 before 1.7. The dev agent must adapt to the branch reality: consume merged event/contract types where they exist; otherwise add only narrow test fixtures or internal projection input abstractions that preserve the public names and behavior promised by Story 1.2. Do not create a second public event vocabulary. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant-Safe Conversation Record`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Current Repository State and Previous Story Intelligence

The current working tree is still largely scaffolded: `Contracts`, domain, `Server`, `Testing`, `Client`, `AppHost`, and `ServiceDefaults` projects exist, but production code is marker-only in the files inspected. Story 1.2 and Story 1.3 story files are ready-for-dev context, not proof that their code has landed on this branch. The implementation must inspect current files before editing and must not assume all earlier story outputs exist. [Source: `src/Hexalith.Conversations.Contracts/ContractsAssemblyMarker.cs`; `src/Hexalith.Conversations/ConversationsAssemblyMarker.cs`; `src/Hexalith.Conversations.Server/ServerAssemblyMarker.cs`; `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`; `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md`]

Carry forward the prior review lesson: compiled assembly-reference tests can pass vacuously when marker assemblies do not use a package. For Story 1.7 dependency boundaries, inspect `.csproj` XML directly as well as `Assembly.GetReferencedAssemblies()`. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Current Repository State and Previous Story Intelligence`; `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`; `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`]

Recent git history shows documentation/story creation and scaffold validation work (`062bee3 docs: create story 1.2 contract definitions`, `4479ced feat: Update subproject commits and add integration tests for scaffold validation`, `c218a1e feat: Update subproject commits, finalize initial project setup, and enhance testing framework`). This reinforces that Story 1.7 should preserve the existing scaffold/test style and avoid broad runtime assumptions. [Source: `git log --oneline -5`]

### Architecture Compliance

EventStore remains the only v1 write-side authority. Projections are derived, repairable, rebuildable, and non-authoritative. If projection state disagrees with EventStore history, EventStore wins and the projection must be marked stale, invalid, quarantined, or rebuilding with a content-safe repair path. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/project-context.md#Framework-Specific Rules`]

Read paths should prefer projection-backed records with explicit trust metadata over unbounded aggregate replay. Ordinary reads must not reconstruct unlimited event history on demand. Heavy verification, rebuild, export, and temporal reconstruction workflows are separate bounded operations and out of this story unless represented by test fixtures for projection rebuild. [Source: `_bmad-output/planning-artifacts/architecture.md#Operational Trust Risks`; `_bmad-output/planning-artifacts/architecture.md#Read-Model And Performance Implications`]

Tenant access fails closed before projection read. Do not trust JWT/request claims alone. The local Tenants projection decides access; if that dependency is missing, stale, unavailable, ambiguous, disabled, or inconsistent, deny or hide rather than returning partial data. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`]

Every trust-bearing read includes projection freshness metadata. Absence must not imply authorization, freshness, successful hydration, or safety. Missing or contradictory metadata must produce degraded freshness/trust state and block governed decisions that require current state. [Source: `_bmad-output/planning-artifacts/architecture.md#Format Patterns`; `_bmad-output/planning-artifacts/prd.md#Projection Freshness`]

### Projection Contract Guidance

Minimum freshness shape for v1 should include the following fields or clearly documented equivalents: projection version or contract version, source cursor/event position equivalent, last applied event timestamp, projection generated timestamp, stale indicator, lag duration where available, freshness state, and a safe reason code. PRD NFR45 names `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration` as the preferred shape. [Source: `_bmad-output/planning-artifacts/prd.md#Projection Freshness`; `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`]

Versioned contracts should make ownership visible, for example `ConversationProjectionV1`, `ConversationDetailsV1`, and `ConversationCreatedV1`, unless the codebase already standardized an equivalent versioning mechanism in Story 1.2. [Source: `_bmad-output/planning-artifacts/architecture.md#Schema Naming Rule`]

Projection contracts should serialize through BCL/System.Text.Json only. Microsoft documentation confirms `System.Text.Json` parses and writes `DateTime`/`DateTimeOffset` using the ISO 8601-1:2019 extended profile; do not add Newtonsoft.Json or custom date converters unless tests prove a contract need. [Source: Microsoft Learn, `https://learn.microsoft.com/dotnet/standard/datetime/system-text-json-support`]

Keep Central Package Management intact. NuGet documentation confirms project files should use `<PackageReference />` without `Version` when versions are centrally managed in `Directory.Packages.props`; add or update package versions centrally only when a new dependency is truly required. [Source: Microsoft Learn, `https://learn.microsoft.com/nuget/consume-packages/central-package-management`; `Directory.Packages.props`]

### Data and Non-Disclosure Rules

Projection records may persist stable IDs and content-safe metadata from conversation events. They must not persist hydrated Party display names, contact values, person or organization details, raw upstream records, raw provider prompt/response payloads, file binaries, access tokens, claims, raw tenant authorization state, raw upstream problem details, or redacted content. Party and upstream display hydration belongs to Story 1.9 read-time adapters. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`; `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`]

Projection reads must avoid cross-tenant existence leakage through result counts, facets, ordering, pagination gaps, timestamps, errors, autocomplete, telemetry, URLs, or diagnostics. Unauthorized, nonexistent, and cross-tenant records remain indistinguishable to non-privileged callers unless a later policy explicitly allows disclosure. [Source: `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`; `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`]

### File and Test Placement

Expected production files belong under:

- `src/Hexalith.Conversations.Contracts/Projections`
- `src/Hexalith.Conversations.Contracts/TrustStates`
- `src/Hexalith.Conversations.Contracts/Versioning`
- `src/Hexalith.Conversations.Server/Projections`
- `src/Hexalith.Conversations.Server/TenantAccess` only if adapting to the Story 1.5 interface already present

Expected tests belong under:

- `tests/Hexalith.Conversations.Contracts.Tests`
- `tests/Hexalith.Conversations.Server.Tests/Projections`
- `tests/Hexalith.Conversations.Server.Tests/Boundaries`
- `tests/Hexalith.Conversations.Tests` only for pure domain/event fixtures that are not server projection behavior

Shared deterministic builders may be added to `src/Hexalith.Conversations.Testing` only when reusable across future stories and free of runtime behavior. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `src/Hexalith.Conversations.Testing/Factories/ConversationTestIds.cs`]

### Testing Requirements

Use xUnit v3 and Shouldly to match existing tests. xUnit.net v3 supports `[Fact]`, `[Theory]`, and `[InlineData]` patterns for .NET 8 or later, which is compatible with this repository's `net10.0` target. [Source: Context7 xUnit.net documentation; `tests/Hexalith.Conversations.Tests/Testing/ConversationTestIdsTest.cs`; `Directory.Build.props`]

Projection tests must be local and deterministic. They should not require Aspire runtime, Dapr sidecars, EventStore server runtime, tenant seed data, provider credentials, external cloud resources, or nested submodule initialization. Use in-memory event sequences and projection store fakes unless a local approved EventStore testing helper is already present. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`]

Test names should make failure modes explicit: current projection, stale projection, rebuilding projection, unavailable projection store, forbidden/hidden result, duplicate delivery, replayed delivery, out-of-order event, mixed-tenant poison event, projection deletion/rebuild, and forbidden payload/property scan. [Source: `_bmad-output/planning-artifacts/prd.md#Projection Freshness`; `_bmad-output/planning-artifacts/architecture.md#Testing And Release Evidence`]

### Stop Conditions

Stop for architecture clarification before coding if implementation needs to store new durable state outside derived projections, introduce a new public freshness/trust state or error code, cache hydrated Party data, expose EventStore internals publicly, accept stale/rebuilding/unavailable projections for trust-bearing actions, create a worker/export/rebuild service beyond local testable projection behavior, or use provider session IDs as durable identity. [Source: `_bmad-output/planning-artifacts/architecture.md#Agent Conflict Stop Conditions`]

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`
- `_bmad-output/planning-artifacts/architecture.md#Data Architecture`
- `_bmad-output/planning-artifacts/architecture.md#Read-Model And Performance Implications`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/prd.md#Event Sourcing, Projections, And Publication`
- `_bmad-output/planning-artifacts/prd.md#Projection Freshness`
- `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR22`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`
- `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`
- `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`
- `https://learn.microsoft.com/dotnet/standard/datetime/system-text-json-support`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
