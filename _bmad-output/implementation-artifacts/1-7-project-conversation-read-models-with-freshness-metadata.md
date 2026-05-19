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
6. Given any summary, detail, list, count, pagination, diagnostic, telemetry, or command-availability surface observes an unauthorized, cross-tenant, non-existent, forbidden, or redacted conversation, when it returns a result, then the shape and metadata do not disclose existence through counts, gaps, timestamps, cursors, business references, provider metadata, freshness transitions, telemetry dimensions, or error details.
7. Given a full rebuild, partial rebuild, concurrent rebuild/read, gap, contradictory metadata, out-of-order event, mixed-tenant poison event, or projection-store failure occurs, when projection state is evaluated, then the story-defined decision matrix maps it deterministically to `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, or `Redacted`, and only `Current` enables trust-bearing decisions.

## Tasks / Subtasks

- [ ] Confirm freshness and prerequisite gates before implementation. (AC: 1-5)
  - [ ] Link the existing readiness decision for Projection freshness blocking semantics and use only the approved vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`.
  - [ ] Treat `Current` as the only state accepted for trust-bearing decisions unless a later ADR explicitly permits a narrower degraded behavior.
  - [ ] Define the story-local freshness decision matrix before coding: `Current` requires tenant-authorized, complete, non-contradictory, successfully generated projection metadata; stale thresholds, gaps, rebuild activity, projection-store failures, tenant denial, and redacted content must map to the approved non-current states.
  - [ ] Verify the branch contains or intentionally stubs the event/contract outputs from Stories 1.2, 1.3, 1.4, append-message/reference work, 1.5, and 1.6. If required event contracts or domain events are absent, add the smallest contract/domain fixtures needed for this story and record the dependency in tests; do not invent conflicting public names.
  - [ ] Inspect the actual branch state before adding projection abstractions. Prefer existing contracts, handlers, and tests; when prerequisites are missing, add only narrow Story 1.7 fixtures or internal input abstractions and document that dependency in tests.
  - [ ] Confirm freshness values are computed server-side from projection state only. Caller-supplied, cached, or deserialized freshness metadata must not upgrade trust and must fail closed when inconsistent with server-observed projection state.

- [ ] Add or complete projection and freshness contracts in `src/Hexalith.Conversations.Contracts`. (AC: 2, 3, 4)
  - [ ] Add `Projections/ConversationSummaryProjectionV1.cs` and `Projections/ConversationDetailProjectionV1.cs`, or extend the Story 1.2 projection shells if already present.
  - [ ] Add a shared `ProjectionFreshnessV1` or equivalent contract with projection version/cursor, last applied event position or equivalent source cursor, last applied event timestamp, projection generated timestamp, optional lag duration, stale flag, freshness state, and safe reason code.
  - [ ] Keep public projection contracts in Conversations language. Do not expose EventStore envelopes, stream names, snapshots, expected revisions, raw projection topology, raw event payloads, EventStore stream IDs, subscription names, checkpoint names, raw sequence tokens, provider payload fragments, or EventStore client types.
  - [ ] Use UTC `DateTimeOffset` semantics for all public freshness timestamps and define which metadata is safe for public read contracts versus internal diagnostics only.
  - [ ] Keep public freshness reason codes on a documented allowlist. Unknown, missing, contradictory, or unsupported freshness states/reason codes must be treated as non-current and must not enable trust-bearing decisions.
  - [ ] Represent timeline, participant, message, business reference, provider correlation, and file-reference data as stable IDs and content-safe metadata only. Do not include Party display names or upstream records in persisted projection contracts.

- [ ] Implement projection materialization under `src/Hexalith.Conversations.Server/Projections`. (AC: 1, 4, 5)
  - [ ] Add projection handler(s) for conversation-created, participant-added, message-appended, reference-attached, metadata-updated, and lifecycle events available on the branch.
  - [ ] Build summary and detail read models as derived, rebuildable state. Projection state must not become write-side authority and must be disposable/reconstructable from EventStore history.
  - [ ] Make handlers idempotent for duplicate/replayed events and deterministic for ordered replay. For out-of-order delivery, either buffer/reject/mark rebuilding according to the documented behavior; never silently produce a confident current read model from contradictory event order.
  - [ ] Define the idempotency and ordering basis used by handlers, including duplicate event IDs, replay after rebuild, detected gaps, and expected final read model equivalence after projection deletion/rebuild.
  - [ ] Define checkpoint/update ordering so a failed metadata or checkpoint write after projection mutation cannot leave a publicly `Current` read model. Tests must cover retry/replay after this partial-failure case.
  - [ ] Keep summary, detail, and freshness metadata from the same accepted projection generation/cursor when marking a read `Current`; mixed-generation summary/detail reads must degrade to a non-current state.
  - [ ] Store tenant scope on every projection record and reject or quarantine mixed-tenant poison events before mutation.
  - [ ] Choose and document one deterministic poison-event behavior for this story: reject, quarantine, or mark rebuilding/unavailable. Never project poison data into another tenant and never mark poisoned state `Current`.
  - [ ] Do not add transcript tables, authoritative message stores, provider session stores, Memories/RAG indexes, export artifacts, UI state caches, or durable hydrated Party data in this story.

- [ ] Add the tenant-safe read boundary for projection results. (AC: 2, 3, 4)
  - [ ] Add query/read service behavior only as needed to return projection contracts with freshness metadata.
  - [ ] Check tenant access before projection read through the local tenant access boundary from Story 1.5. If Story 1.5 is not yet implemented on the branch, keep the read boundary fail-closed behind an interface/test fake rather than trusting request claims directly.
  - [ ] Return `Forbidden` or hidden-by-tenant-isolation semantics without revealing whether a protected conversation exists, including through counts, result shape, timestamps, business references, provider metadata, cursors, pagination gaps, diagnostics, telemetry, or timing-sensitive metadata.
  - [ ] Treat missing tenant context, empty tenant ID, malformed tenant ID, mismatched tenant context, unknown tenant state, and cross-tenant query attempts as fail-closed. Tests must prove there is no fallback to unscoped reads.
  - [ ] Map missing, stale, rebuilding, unavailable, contradictory, or poisoned projection metadata to safe freshness states and block command availability metadata for actions requiring current projection state.
  - [ ] Use the same tenant-access and freshness-evaluation path for detail, list, count, pagination, and command-availability surfaces so one surface cannot disclose existence or trust metadata that another hides.

- [ ] Add deterministic rebuild and freshness behavior tests. (AC: 1-5)
  - [ ] Add tests under `tests/Hexalith.Conversations.Server.Tests/Projections` for ordered replay, duplicate event delivery, replayed event delivery, gap detection, projection deletion/rebuild equivalence, concurrent rebuild/read behavior, out-of-order event behavior, stale metadata, missing metadata, unavailable projection store, contradictory metadata, projection-store failure, and mixed-tenant poison events.
  - [ ] Add contract tests under `tests/Hexalith.Conversations.Contracts.Tests` proving projection contracts serialize with `System.Text.Json` web defaults, dates round-trip as ISO 8601-compatible `DateTimeOffset` values, unknown fields do not grant trust, unknown freshness states/reason codes fail closed, missing freshness fields fail closed, and JSON property names do not expose EventStore or internal topology terms.
  - [ ] Add projection failure tests for checkpoint-after-mutation failure, mixed-generation summary/detail reads, caller-supplied freshness upgrade attempts, and replay after a failed metadata write.
  - [ ] Add payload/property inspection tests proving read models do not persist Party personal data, names, emails, external user IDs, raw provider subjects, avatars, profile blobs, file binaries, raw upstream records, provider prompt/response payloads, access tokens, claims, raw authorization state, or raw EventStore details.
  - [ ] Add boundary tests that inspect `.csproj` XML as well as compiled assembly references so forbidden dependencies cannot be hidden by unused marker assemblies.
  - [ ] Keep projection tests local and hermetic with in-memory/local fakes that can inject duplicate, gap, poison, rebuild, and store-failure conditions. Do not require Aspire, Dapr sidecars, EventStore server runtime, tenant seed data, cloud credentials, or nested submodule initialization.

- [ ] Document projection behavior and validation evidence. (AC: 1-5)
  - [ ] Add or update developer-facing docs explaining that read models are derived from EventStore history, include freshness metadata, and are not authoritative write state.
  - [ ] Document the duplicate/replay/out-of-order policy, rebuild behavior, and accepted freshness states for summary/detail projections.
  - [ ] Document the public freshness reason-code allowlist separately from internal diagnostic fields and explain that unknown public values are non-current by default.
  - [ ] Link Story 1.7 local evidence forward to Story 1.11 replay/schema-version work, Story 1.8 retrieve/list behavior, Story 3.x operator trust surfaces, Story 4.2 client behavior, and Story 6.2 projection-lag observability.
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run restore/build/test if assets are stale. Do not initialize nested submodules recursively.

## Dev Notes

### Scope Boundary

Story 1.7 owns derived summary/detail read models, projection handlers, projection freshness metadata, freshness-state downgrade behavior, and local projection tests. It does not own tenant access source-of-truth implementation, command idempotency storage, aggregate command behavior, publication contracts, Party/Project/Folder display hydration, admin UI components, conformance release packaging, export/evidence bundles, or EventStore schema evolution. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`; `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]

Projection freshness is unblocked by the readiness gate decided on 2026-05-17. The binding vocabulary is `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`. If an implementation path needs a public state or reason outside that list, stop for architecture clarification or ADR rather than adding a local enum value. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]

Stories 1.4.1 and 1.4.2 appear in the epics as append-message and reference slices, while sprint status currently tracks Story 1.4 as participant attribution and then Story 1.5/1.6 before 1.7. The dev agent must adapt to the branch reality: consume merged event/contract types where they exist; otherwise add only narrow test fixtures or internal projection input abstractions that preserve the public names and behavior promised by Story 1.2. Do not create a second public event vocabulary. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant-Safe Conversation Record`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Advanced Elicitation Hardening

The 2026-05-19 advanced elicitation pass clarified that freshness is computed trust evidence, not caller input. Public contracts may carry freshness metadata out of the service, but no request, cached payload, deserialized client object, or replay fixture may upgrade a projection to `Current`; only server-observed projection metadata can do that.

Projection checkpointing is a reliability boundary for this story. Implementations must prevent a partially written projection plus failed checkpoint/metadata update from being reported as `Current`. If the selected local storage abstraction cannot make projection mutation and metadata update atomic, the fallback behavior is duplicate-safe replay with a non-current public state until the projection and metadata agree.

Summary and detail projections must not be stitched across generations as `Current`. During rebuild, catch-up, deletion/rebuild, or concurrent read windows, the public result must either prove that projection data and freshness metadata come from the same accepted generation/cursor or degrade to `Rebuilding`, `Stale`, or `Unavailable`.

Public freshness reason codes are an allowlisted trust vocabulary. Unknown, missing, unsupported, or internally diagnostic-only values are treated as non-current and must not leak storage topology, tenant authorization internals, provider details, or EventStore identifiers.

### Current Repository State and Previous Story Intelligence

The current working tree is still largely scaffolded: `Contracts`, domain, `Server`, `Testing`, `Client`, `AppHost`, and `ServiceDefaults` projects exist, but production code is marker-only in the files inspected. Story 1.2 and Story 1.3 story files are ready-for-dev context, not proof that their code has landed on this branch. The implementation must inspect current files before editing and must not assume all earlier story outputs exist. [Source: `src/Hexalith.Conversations.Contracts/ContractsAssemblyMarker.cs`; `src/Hexalith.Conversations/ConversationsAssemblyMarker.cs`; `src/Hexalith.Conversations.Server/ServerAssemblyMarker.cs`; `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`; `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md`]

Carry forward the prior review lesson: compiled assembly-reference tests can pass vacuously when marker assemblies do not use a package. For Story 1.7 dependency boundaries, inspect `.csproj` XML directly as well as `Assembly.GetReferencedAssemblies()`. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Current Repository State and Previous Story Intelligence`; `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`; `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`]

Recent git history shows documentation/story creation and scaffold validation work (`062bee3 docs: create story 1.2 contract definitions`, `4479ced feat: Update subproject commits and add integration tests for scaffold validation`, `c218a1e feat: Update subproject commits, finalize initial project setup, and enhance testing framework`). This reinforces that Story 1.7 should preserve the existing scaffold/test style and avoid broad runtime assumptions. [Source: `git log --oneline -5`]

### Architecture Compliance

EventStore remains the only v1 write-side authority. Projections are derived, repairable, rebuildable, and non-authoritative. If projection state disagrees with EventStore history, EventStore wins and the projection must be marked stale, invalid, quarantined, or rebuilding with a content-safe repair path. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/project-context.md#Framework-Specific Rules`]

Read paths should prefer projection-backed records with explicit trust metadata over unbounded aggregate replay. Ordinary reads must not reconstruct unlimited event history on demand. Heavy verification, rebuild, export, and temporal reconstruction workflows are separate bounded operations and out of this story unless represented by test fixtures for projection rebuild. [Source: `_bmad-output/planning-artifacts/architecture.md#Operational Trust Risks`; `_bmad-output/planning-artifacts/architecture.md#Read-Model And Performance Implications`]

Tenant access fails closed before projection read. Do not trust JWT/request claims alone. The local Tenants projection decides access; if that dependency is missing, stale, unavailable, ambiguous, disabled, or inconsistent, deny or hide rather than returning partial data. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`]

Every trust-bearing read includes projection freshness metadata. Absence must not imply authorization, freshness, successful hydration, or safety. Missing or contradictory metadata must produce degraded freshness/trust state and block governed decisions that require current state. [Source: `_bmad-output/planning-artifacts/architecture.md#Format Patterns`; `_bmad-output/planning-artifacts/prd.md#Projection Freshness`]

The public freshness contract is a domain trust contract, not a storage topology leak. It may expose stable read-model version, safe cursor or position equivalents, UTC event/projection timestamps, lag duration, freshness state, stale flag, and a safe reason code. Raw EventStore stream IDs, subscription names, checkpoint identifiers, revision tokens, provider payload fragments, and internal sequence tokens are internal diagnostics only unless an ADR later approves a public equivalent.

Story 1.7's default decision matrix is fail-closed: `Current` means tenant-authorized, complete, non-contradictory projection metadata generated successfully from accepted events; `Stale` means metadata is complete but older than the configured/default freshness threshold; `Rebuilding` means projection rebuild, gap repair, or ordered replay catch-up is active or required; `Unavailable` means the projection store or metadata source cannot be trusted; `Forbidden` means tenant access denies or hides the record; and `Redacted` means policy permits the record shape but suppresses content. Only `Current` supports trust-bearing decisions.

### Projection Contract Guidance

Minimum freshness shape for v1 should include the following fields or clearly documented equivalents: projection version or contract version, source cursor/event position equivalent, last applied event timestamp, projection generated timestamp, stale indicator, lag duration where available, freshness state, and a safe reason code. PRD NFR45 names `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration` as the preferred shape. [Source: `_bmad-output/planning-artifacts/prd.md#Projection Freshness`; `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`]

Versioned contracts should make ownership visible, for example `ConversationProjectionV1`, `ConversationDetailsV1`, and `ConversationCreatedV1`, unless the codebase already standardized an equivalent versioning mechanism in Story 1.2. [Source: `_bmad-output/planning-artifacts/architecture.md#Schema Naming Rule`]

Projection contracts should serialize through BCL/System.Text.Json only. Microsoft documentation confirms `System.Text.Json` parses and writes `DateTime`/`DateTimeOffset` using the ISO 8601-1:2019 extended profile; do not add Newtonsoft.Json or custom date converters unless tests prove a contract need. [Source: Microsoft Learn, `https://learn.microsoft.com/dotnet/standard/datetime/system-text-json-support`]

Keep Central Package Management intact. NuGet documentation confirms project files should use `<PackageReference />` without `Version` when versions are centrally managed in `Directory.Packages.props`; add or update package versions centrally only when a new dependency is truly required. [Source: Microsoft Learn, `https://learn.microsoft.com/nuget/consume-packages/central-package-management`; `Directory.Packages.props`]

### Data and Non-Disclosure Rules

Projection records may persist stable IDs and content-safe metadata from conversation events. They must not persist hydrated Party display names, contact values, person or organization details, raw upstream records, raw provider prompt/response payloads, file binaries, access tokens, claims, raw tenant authorization state, raw upstream problem details, or redacted content. Party and upstream display hydration belongs to Story 1.9 read-time adapters. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`; `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`]

Projection reads must avoid cross-tenant existence leakage through result counts, facets, ordering, pagination gaps, timestamps, errors, autocomplete, telemetry, URLs, or diagnostics. Unauthorized, nonexistent, and cross-tenant records remain indistinguishable to non-privileged callers unless a later policy explicitly allows disclosure. [Source: `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`; `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`]

The same non-disclosure rule applies to summary lists, details, counts, cursors, pagination tokens, command-availability hints, telemetry dimensions, and diagnostics. Forbidden, redacted, cross-tenant, and non-existent records must not be distinguishable through response shape or freshness metadata.

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
- 2026-05-18: Party-mode review applied freshness decision matrix, tenant non-disclosure, rebuild/poison-event, boundary-test, and privacy clarifications.
- 2026-05-19: Advanced elicitation applied trust-computation, checkpoint-ordering, mixed-generation, and public reason-code clarifications.

## Party-Mode Review

- ISO date and time: 2026-05-18T18:14:00Z
- Selected story key: 1-7-project-conversation-read-models-with-freshness-metadata
- Command/skill invocation used: `/bmad-party-mode 1-7-project-conversation-read-models-with-freshness-metadata; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: Reviewers agreed Story 1.7 was directionally sound but needed sharper pre-dev instructions for public freshness semantics, fail-closed tenant reads before lookup, same-shape non-disclosure across list/detail/count/pagination/telemetry, deterministic rebuild/out-of-order/poison-event behavior, public contract boundaries, and hermetic test evidence.
- Changes applied: Added acceptance criteria and tasks for freshness decision matrix, UTC/public-vs-internal metadata, branch-reality inspection, tenant fail-closed cases, non-disclosing list/detail/pagination/telemetry behavior, deterministic rebuild and poison-event handling, contract-shape tests, `.csproj` boundary tests, privacy scans, and local failure-injection projection tests.
- Findings deferred: Exact projection storage/indexing engine, stale SLO configuration beyond the default deterministic threshold, cache headers/ETags, provider display hydration, schema evolution/upcasting, export/evidence bundles, admin UI behavior, and provider mapping remain outside Story 1.7 unless a later ADR or story brings them into scope.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- ISO date and time: 2026-05-19T02:02:15Z
- Selected story key: 1-7-project-conversation-read-models-with-freshness-metadata
- Command/skill invocation used: `/bmad-advanced-elicitation 1-7-project-conversation-read-models-with-freshness-metadata`
- Batch 1 method names: Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation; Critique and Refine
- Reshuffled Batch 2 method names: First Principles Analysis; Pre-mortem Analysis; Architecture Decision Records; Socratic Questioning; User Persona Focus Group
- Findings summary: The story was ready-for-dev but needed sharper instructions for freshness as computed server-side trust evidence, checkpoint/metadata partial failures, mixed-generation read hazards, consistent non-disclosure across read surfaces, and public reason-code allowlisting.
- Changes applied: Added tasks and dev notes for caller-supplied freshness fail-closed behavior, unknown state/reason-code handling, checkpoint-after-mutation failure, summary/detail generation consistency, shared read-surface authorization/freshness paths, and documentation of public reason-code boundaries.
- Findings deferred: Exact projection storage transaction mechanism, checkpoint schema, stale threshold configuration, generation identifier shape, background rebuild orchestration, and operator diagnostic detail remain implementation or later ADR decisions.
- Final recommendation: ready-for-dev
