# Story 1.6: Add Idempotent Command Handling

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want duplicate conversation commands to return stable outcomes,
so that retries, client timeouts, and at-least-once delivery do not create duplicate conversations, messages, participants, or references.

## Acceptance Criteria

1. Given a create, append-message, add-participant, attach-reference, update-metadata, or close/archive command includes an idempotency key, when the same tenant, conversation scope, command type, idempotency key, and equivalent payload are submitted more than once, then the system returns the same externally visible logical outcome without emitting duplicate successful domain events. The stable outcome includes result category, durable conversation/message/participant/reference identity when relevant, typed rejection code when relevant, retryability, and safe correlation/audit handle semantics; it does not require byte-for-byte transport equality or raw EventStore status disclosure.
2. Given an idempotency key is reused with a different payload or incompatible command context, when the command is evaluated, then the system compares the ADR-approved normalized fingerprint for tenant ID, command type, aggregate or allocation scope, idempotency key, schema/contract version, canonical payload, and relevant command context, returns a typed idempotency-conflict rejection, and no conversation state mutation or publication occurs.
3. Given a command outcome is unknown to the caller because of timeout, retry, duplicate delivery, pending command status, or publication lag, when the caller resubmits the same idempotent command, then the system resolves the terminal stored or replayed command outcome when available, otherwise returns the ADR-approved typed retryable uncertainty outcome without emitting a second successful domain event, and the result does not depend on provider-owned session IDs.
4. Given duplicate or reordered command delivery occurs, when aggregate state, idempotency records, and projections are evaluated, then projections remain deterministic and no duplicate business effects appear in read models, and content-safe diagnostics distinguish duplicate, conflict, unsupported-version, and infrastructure uncertainty.
5. Given idempotency tests run, when duplicate equivalent commands, duplicate non-equivalent commands, concurrent same-key submissions, reordered delivery, duplicate event delivery, unknown client outcome retry, terminal close/archive replay, and tenant-mismatched key reuse are exercised, then tests prove stable outcomes, conflict rejection, tenant scoping, no duplicate events, no projection divergence, no terminal-state regression, and no cross-tenant leakage.
6. Given a caller lacks tenant access or presents a tenant-mismatched context, when the command is evaluated, then tenant access validation fails closed before aggregate lookup, idempotency lookup, command-status lookup, duplicate outcome replay, conflict disclosure, or projection access, and the response does not distinguish between a missing aggregate, existing idempotency record, conflicting key, or hidden conversation.

Evidence Note: This story must produce minimum local evidence for story closure. Release-gate idempotency evidence is carried forward into Story 5.6 for manifest aggregation and signing.

### Pre-Dev Party-Mode Review Decisions

The 2026-05-18 party-mode review clarified Story 1.6 without adding product scope:

- The idempotency ADR is an in-story gate: behavior code must not proceed until the command idempotency ADR is accepted and linked from `docs/adrs/index.md`.
- Public Contracts must expose Conversations-domain result and error categories only. EventStore command-status records, stream identifiers, state-store keys, expected revisions, sequence numbers, and raw status internals stay behind the server adapter boundary.
- The canonical idempotency fingerprint must be built from stable Conversations command contracts and explicit command context, not raw JSON byte order, provider session IDs, server-generated timestamps, transport headers, EventStore envelopes, or mutable Party display data.
- Tenant access is authoritative before idempotency outcome disclosure. Wrong-tenant, unauthorized, stale, missing, or unavailable tenant access paths must not reveal key existence, stored outcomes, conflicts, or hidden conversation existence.
- Unknown or pending command status is retry-safe but not self-mutating: the handler may resolve a terminal stored/replayed outcome, otherwise it must return the ADR-approved typed retryable uncertainty result and avoid appending a duplicate successful event.
- Projection duplicate/reorder proof is bounded to the conversation projections touched by the story's command types and remains local evidence for Story 5.6; this story does not implement the signed release manifest or a generic projection framework rewrite.
- Concurrency proof is required for same-tenant, same-key, equivalent-payload submissions so only one business mutation succeeds and all callers receive stable logical outcomes.

## Tasks / Subtasks

- [ ] Resolve the idempotency decision before behavior code. (AC: 1-6)
  - [ ] Create or update the command idempotency ADR from the existing ADR template, using `docs/adrs/0001-idempotency-contract.md` if it is still the next available ADR file.
  - [ ] Record the approved idempotency key source, key scope, equivalent-payload canonicalization rule, stored outcome semantics, conflict behavior, TTL/retention expectations, and retry behavior after unknown outcomes.
  - [ ] Update `docs/adrs/index.md` so the Idempotency contract topic no longer remains only "Proposed" once the decision is accepted.
  - [ ] Do not implement behavior code until the ADR has accepted the fingerprint, stable outcome, conflict, tenant-disclosure, unknown/pending, and retention semantics.
  - [ ] Stop and request an explicit architecture decision if the implementation would need EventStore envelope changes, provider IDs as identity, cross-tenant deduplication, or a durable store outside the approved write path.

- [ ] Add Conversations-owned idempotency primitives without exposing EventStore internals. (AC: 1-3, 5-6)
  - [ ] Add domain/application primitives under `src/Hexalith.Conversations/Idempotency` for idempotency scope, payload fingerprint, stored outcome, and conflict/no-match decisions.
  - [ ] Scope each idempotency record by tenant ID, conversation scope or create-conversation allocation scope, command type, idempotency key, and schema/contract version.
  - [ ] Canonicalize the payload using Conversations command contracts and stable IDs only; exclude provider-owned session IDs as authority, timestamps generated by the server, EventStore envelopes, stream names, sequence numbers, raw payload bytes, and mutable Party display data.
  - [ ] Treat equivalent duplicate success and equivalent duplicate rejection as stable replayable outcomes; treat same key plus non-equivalent payload/context as `idempotency_conflict`.
  - [ ] Do not put EventStore SDK types, Dapr state APIs, or persistence exceptions into public Contracts or domain aggregate code.

- [ ] Reuse the EventStore command-status/idempotency surface through a Conversations adapter where it fits. (AC: 1-4, 6)
  - [ ] Investigate `Hexalith.EventStore` command status support before adding storage: `CommandEnvelope.MessageId`, `CommandStatusRecord`, `ICommandStatusStore`, `CommandStatusConstants`, `DaprCommandStatusStore`, and `InMemoryCommandStatusStore`.
  - [ ] Prefer a server-side adapter under `src/Hexalith.Conversations.Server/CommandHandlers` or `src/Hexalith.Conversations.Server/EventStore` over a new unrelated repository abstraction.
  - [ ] Keep any EventStore-specific code inside the approved server write-adapter boundary. Domain code may depend on Conversations idempotency abstractions only.
  - [ ] If EventStore command status is correlation-message based and cannot represent Conversations equivalence/conflict needs, wrap it with a Conversations-owned idempotency record rather than changing EventStore or leaking its model outward.
  - [ ] Preserve payload secrecy in logs and `ToString()` behavior. Diagnostics may include safe reason category, command type, tenant-scoped correlation handle, and retryability only.

- [ ] Wire idempotency into the write command flow after tenant access passes and before aggregate mutation. (AC: 1-4, 6)
  - [ ] Ensure tenant authorization from Story 1.5 still happens before aggregate load, command dispatch, EventStore read/write, idempotency outcome disclosure, or projection access.
  - [ ] For an equivalent duplicate with a terminal stored outcome, return the same logical command result without invoking `ConversationAggregate` or appending duplicate success events.
  - [ ] For an in-flight or unknown outcome, return a documented retryable/uncertain result or resolve from EventStore state according to ADR-001/ADR-002; do not silently retry in a way that can emit duplicate business effects.
  - [ ] For conflicting key reuse, return typed `idempotency_conflict` and do not load or mutate aggregate state after the conflict is known.
  - [ ] Keep aggregate handlers pure: `Handle(command, state?) -> DomainResult`. Use `DomainResult.Success`, `DomainResult.Rejection`, and `DomainResult.NoOp` consistently; do not throw domain exceptions for expected duplicate/conflict behavior.

- [ ] Make projections and publication tolerant of duplicates/reordering. (AC: 3-5)
  - [ ] Ensure projection handlers use idempotent operations: set/update by stable message ID, participant ID, reference ID, or conversation ID; avoid blind list append/counter increment semantics.
  - [ ] Deduplicate projection/event processing by event/message identity where available, while keeping handler operations idempotent as defense in depth.
  - [ ] Confirm duplicate command replay does not publish extra successful domain events. If publication failure leaves an uncertain terminal state, surface content-safe uncertainty instead of inventing a duplicate event.
  - [ ] Add diagnostics that distinguish duplicate, idempotency conflict, unsupported schema/version, tenant mismatch, stale/missing tenant projection, and infrastructure uncertainty without leaking target tenant, Party data, raw payload, provider payload, or inaccessible conversation existence.

- [ ] Add focused automated tests and local evidence. (AC: 1-6)
  - [ ] Cover a shared command matrix for create, append-message, add-participant, attach-reference, update-metadata, close, and archive across duplicate equivalent payload, non-equivalent payload, same key with different command type, same key with different conversation/allocation scope, different tenant, unknown/pending outcome, and concurrent duplicate submission.
  - [ ] Add unit tests for idempotency scope and canonical payload equivalence: identical payload, property-order differences if applicable, optional-null/default equivalence per ADR, different payload, different command type, different tenant, different conversation, and different schema version.
  - [ ] Add domain/application tests proving duplicate equivalent commands return stable success/no-op/rejection outcomes and do not call aggregate mutation twice.
  - [ ] Add conflict tests proving key reuse with a different payload/context returns `idempotency_conflict`, mutates nothing, publishes nothing, and produces content-safe diagnostics.
  - [ ] Add retry-after-unknown-outcome tests for timeout, duplicate delivery, and publication lag paths using in-memory fakes. Do not require Aspire runtime, Dapr sidecars, tenant seed data, provider credentials, production secrets, cloud resources, or nested submodule initialization.
  - [ ] Add projection determinism tests proving duplicate/reordered deliveries do not create duplicate messages, participants, references, titles, lifecycle transitions, or divergent read models.
  - [ ] Add tenant-scoping tests proving the same idempotency key cannot reveal or replay another tenant's outcome.
  - [ ] Add local evidence notes that can feed Story 5.6: command types covered, duplicate/conflict/unknown cases covered, storage/fake used, and any release-gate gaps deferred explicitly.

- [ ] Validate and keep the implementation scoped. (AC: 1-5)
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore` first. If assets are stale, run `dotnet restore`, `dotnet build`, and `dotnet test` against `Hexalith.Conversations.slnx`.
  - [ ] Do not run recursive submodule initialization. Root-level sibling module reads are allowed only where already available.
  - [ ] Do not add package versions directly to `.csproj` files; use `Directory.Packages.props`.
  - [ ] Do not implement Story 1.7 read-model freshness, Story 1.10 publication contracts beyond duplicate-safety hooks, Story 1.11 schema-evolution proof, or Story 5.6 signed release evidence in this story.

## Dev Notes

### Scope Boundary

Story 1.6 implements Conversations command idempotency behavior and local proof only. It must not become a generic EventStore rewrite, a transcript store, a provider session cache, a conformance manifest signer, or a projection freshness story. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

This story is behaviorally dependent on earlier Epic 1 slices. It should not be coded against placeholders unless the current branch already contains the Story 1.2 contracts plus the Story 1.3, 1.4, 1.4.1, 1.4.2, and 1.5 command/aggregate/tenant-access seams needed for create, append-message, add-participant, attach-reference, update-metadata, and close/archive behavior. If those seams are absent, implement only ADR/testable abstractions that do not pretend full command behavior exists. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant-Safe Conversation Record`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

Story 5.6 owns release-gating idempotency manifest aggregation and signing. Story 1.6 owns minimum local evidence: duplicate equivalent command, conflicting duplicate command, reordered delivery, unknown client outcome retry, tenant mismatch, no duplicate events, and no projection divergence. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`; `_bmad-output/planning-artifacts/epics.md#Story 5.6: Verify Idempotent Command Conformance`]

### Current Repository State and Previous Story Intelligence

The repository currently has the scaffold from Story 1.1 and a ready-for-dev Story 1.2 file, but sprint status still shows Stories 1.3, 1.4, 1.5, and this story as backlog at story creation time. Source currently contains marker-style projects and minimal testing helpers, not the full command contracts, aggregate, tenant access service, command handlers, or projections that this story ultimately needs. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Completion Notes List`; `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`; local repository inspection on 2026-05-18]

Carry forward Story 1.1 review learning: boundary tests that use `Assembly.GetReferencedAssemblies()` can pass vacuously when marker assemblies do not use package references. For this story, dependency-boundary tests should inspect `.csproj` XML directly when proving forbidden EventStore/Dapr/Tenants/Parties references are absent from Contracts/domain projects. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Review Findings`; `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`]

Recent git history shows the repository has just created Story 1.2 docs and scaffold/testing commits, so implementation agents must re-check the current branch before assuming the command contracts or aggregate files exist. Relevant commits include `062bee3 docs: create story 1.2 contract definitions`, `4479ced feat: Update subproject commits and add integration tests for scaffold validation`, and `c218a1e feat: Update subproject commits, finalize initial project setup, and enhance testing framework`. [Source: `git log --oneline -5` on 2026-05-18]

### Architecture and ADR Guardrails

ADR-002 is explicitly required for command idempotency before dependent behavior proceeds. The ADR must settle duplicate handling, idempotency key scope, equivalent payload semantics, stable outcome semantics, conflict behavior, and evidence requirements. Do not infer these from implementation convenience. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Backlog Created By Core Decisions`; `docs/adrs/index.md`]

EventStore is the only durable source of truth for v1 conversation state. Idempotency records may cache or replay command outcomes, but they must not become a parallel transcript, conversation state store, read-model authority, or provider-session authority. If stored idempotency outcome disagrees with replayed EventStore state, EventStore history wins and the derived/idempotency artifact must be marked uncertain, invalid, stale, or repairable according to the ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

Tenant access remains the first write/read gate. Story 1.6 must not let a duplicate-key lookup disclose that another tenant or hidden conversation has an outcome. The lookup key and diagnostics must be tenant-scoped, and missing/stale/unavailable tenant access must fail closed before outcome disclosure. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]

### EventStore and Local Dependency Intelligence

Hexalith.EventStore already defines command identity and command status concepts. `CommandEnvelope.MessageId` is documented as the unique command identity/idempotency key, `CommandStatusRecord` stores terminal and non-terminal lifecycle status, `ICommandStatusStore` reads/writes status by tenant and correlation ID, and the default status key format is `{tenantId}:{correlationId}:status` with a 24-hour default TTL. Reuse or adapt these where they fit; do not invent a second command-status substrate without proving the gap. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatusRecord.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/ICommandStatusStore.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/CommandStatusConstants.cs`]

EventStore's aggregate programming model is pure domain handling: typed static `Handle` methods return `DomainResult`, where success emits one or more normal event payloads, rejection emits rejection events, and no-op emits no events. Conversations aggregate code should keep idempotency orchestration outside aggregate logic except for deterministic no-op decisions that are actual domain invariants. [Source: `Hexalith.EventStore/README.md#The Programming Model`; `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs`]

Tenants documentation uses the same three-outcome model and documents idempotent event processing for Dapr at-least-once delivery: deduplicate by message/event identity and still make handlers idempotent through set/update/remove semantics. Conversations projections should follow that defense-in-depth model rather than relying on only one deduplication layer. [Source: `Hexalith.Tenants/docs/event-contract-reference.md#Three-Outcome Model`; `Hexalith.Tenants/docs/idempotent-event-processing.md`]

### Idempotency Semantics to Preserve

The idempotency equivalence tuple for Story 1.6 is at minimum: tenant scope, conversation scope or create-allocation scope, command type, idempotency key, schema/contract version, and canonical payload. ADR-002 may add details, but it must not weaken tenant isolation or let provider IDs act as durable identity. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`; `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`]

Equivalent duplicate command outcomes are stable logical outcomes, not necessarily byte-for-byte identical HTTP responses. The stable adopter-facing result must preserve command result category, assigned conversation/message/reference identity when relevant, typed error/rejection code when relevant, retryability, and safe correlation/audit handle semantics defined by contracts. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]

Conflicting idempotency key reuse must be a typed `idempotency_conflict` rejection and must not mutate state, publish a domain event, expose raw payload differences, reveal inaccessible tenant/conversation data, or disclose provider-owned payload. [Source: `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`; `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`]

Do not use `CorrelationId` alone as the public idempotency key unless ADR-002 explicitly chooses that and documents how it remains distinct from tracing. Correlation and causation are diagnostics; idempotency is retry safety. EventStore currently has correlation/status infrastructure, so the Conversations adapter must make the chosen semantics explicit. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/CommandStatusConstants.cs`; `_bmad-output/planning-artifacts/architecture.md#ADR Backlog Created By Core Decisions`]

### Projection and Publication Guardrails

Dapr pub/sub delivery is at-least-once, so duplicate and reordered deliveries are expected, not exceptional. The story must prove duplicate command submission and duplicate event delivery together where feasible, because PRD NFR23 specifically requires induced duplicates, reordering, subscriber-visible replay, idempotency expectations, and deduplication-window expiry. [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`; Dapr docs: `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/`]

Projection handlers must be deterministic under replay. Avoid operations where duplicate application changes state, such as blind list append, counter increment, or notification send without an external deduplication guard. Prefer dictionary/set/update-by-id patterns keyed by conversation/message/participant/reference IDs. [Source: `Hexalith.Tenants/docs/idempotent-event-processing.md`; `_bmad-output/planning-artifacts/architecture.md#Architecture Verification Strategy`]

### File and Test Placement

Expected production files, depending on what earlier stories have created, belong under:

- `src/Hexalith.Conversations/Idempotency`
- `src/Hexalith.Conversations/Conversations`
- `src/Hexalith.Conversations.Server/CommandHandlers`
- `src/Hexalith.Conversations.Server/EventStore`
- `src/Hexalith.Conversations.Server/Projections`
- `src/Hexalith.Conversations.Contracts/Results`
- `src/Hexalith.Conversations.Contracts/Errors`
- `src/Hexalith.Conversations.Contracts/Commands`

Expected tests belong under:

- `tests/Hexalith.Conversations.Tests` for pure domain/idempotency behavior.
- `tests/Hexalith.Conversations.Server.Tests` for command handler/adaptor behavior and no-duplicate-publication assertions.
- `tests/Hexalith.Conversations.IntegrationTests` only for boundary/flow tests that can run without Aspire runtime, Dapr sidecars, tenant seed data, provider credentials, production secrets, cloud resources, or nested submodule initialization.
- `src/Hexalith.Conversations.Testing` for reusable builders/fakes only when they are generic enough not to smuggle runtime behavior.

[Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `_bmad-output/project-context.md#Testing Rules`]

### Latest Technical Information

Current official .NET documentation confirms .NET 10 is the active platform generation for this repo's SDK/target direction, and NuGet Central Package Management still expects versions to live centrally in `Directory.Packages.props` with project `PackageReference` entries kept versionless. Do not downgrade target frameworks or add inline package versions to make local tests pass. [Source: Microsoft Learn, `.NET 10 overview`, `https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview`; Microsoft Learn, `Central Package Management`, `https://learn.microsoft.com/en-gb/nuget/consume-packages/central-package-management`; local `global.json`; `Directory.Packages.props`]

Dapr's official pub/sub documentation continues to describe at-least-once delivery. Treat exactly-once as not available at this boundary; design for duplicate delivery and idempotent handlers. [Source: Dapr docs, `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/`; Dapr pub/sub API reference, `https://docs.dapr.io/reference/api/pubsub_api/`]

### Anti-Reinvention and Non-Disclosure Warnings

- Do not create a transcript table, command table that becomes authoritative conversation state, provider session store, or memory store.
- Do not expose EventStore `CommandEnvelope`, stream names, expected revisions, sequence numbers, snapshots, state-store keys, or command-status internals through public Contracts or adopter API responses.
- Do not use provider session IDs, provider response IDs, external business IDs, labels, thread names, or generated route names as idempotency authority.
- Do not let duplicate-key checks bypass tenant access or reveal hidden conversation existence.
- Do not persist Party display names, contact values, person/organization details, raw upstream problem details, file binaries, raw provider payloads, or raw prompt content in idempotency records unless an approved ADR explicitly says why and how it is governed.
- Do not silently collapse conflict, duplicate, unsupported-version, tenant mismatch, and infrastructure uncertainty into one ambiguous failure.

[Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/architecture.md#Critical Conflict Resolution`; `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`]

### Validation

Run solution validation after implementation:

```powershell
dotnet test .\Hexalith.Conversations.slnx --no-restore
```

If restore/build assets are stale:

```powershell
dotnet restore .\Hexalith.Conversations.slnx
dotnet build .\Hexalith.Conversations.slnx --no-restore
dotnet test .\Hexalith.Conversations.slnx --no-build
```

Validation must not require Aspire runtime launch, Dapr sidecars, tenant seed data, provider credentials, production secrets, external cloud resources, or nested submodule initialization. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Validation`; `README.md`]

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`
- `_bmad-output/planning-artifacts/epics.md#Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections`
- `_bmad-output/planning-artifacts/architecture.md#ADR Backlog Created By Core Decisions`
- `_bmad-output/planning-artifacts/architecture.md#Data Architecture`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md`
- `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `docs/adrs/index.md`
- `Hexalith.EventStore/README.md`
- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatusRecord.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/ICommandStatusStore.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/CommandStatusConstants.cs`
- `Hexalith.Tenants/docs/event-contract-reference.md`
- `Hexalith.Tenants/docs/idempotent-event-processing.md`
- Microsoft Learn `.NET 10 overview`: `https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview`
- Microsoft Learn `Central Package Management`: `https://learn.microsoft.com/en-gb/nuget/consume-packages/central-package-management`
- Dapr pub/sub overview: `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/`

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-18: Party-mode review applied ADR gate, fingerprint, tenant-disclosure, unknown-outcome, concurrency, and projection-test clarifications.

## Party-Mode Review

- Date/time: 2026-05-18T14:22:21Z
- Selected story key: 1-6-add-idempotent-command-handling
- Command/skill invocation used: `/bmad-party-mode 1-6-add-idempotent-command-handling; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - The story was directionally sound but needed sharper executable semantics before development around ADR dependency, stable outcome shape, fingerprint/conflict comparison, tenant-first disclosure rules, unknown/pending command outcomes, concurrency, and projection duplicate/reorder proof.
  - All reviewers recommended a story update before `bmad-dev-story`; the required changes were low-risk clarifications rather than new product scope.
- Changes applied:
  - Clarified stable externally visible idempotent outcomes without requiring byte-for-byte transport equality or exposing EventStore status internals.
  - Clarified ADR-approved fingerprint inputs and prohibited raw JSON, provider IDs, server timestamps, transport fields, EventStore envelopes, and mutable Party display data as authority.
  - Added an explicit tenant-access acceptance criterion requiring fail-closed authorization before idempotency lookup, command-status lookup, duplicate replay, conflict disclosure, or projection access.
  - Clarified pending/unknown command outcome behavior as retry-safe and non-mutating until the ADR-approved typed uncertainty result or terminal replayed outcome is available.
  - Added concurrent duplicate submission and terminal close/archive replay to the test evidence expectations.
  - Bounded projection duplicate/reorder proof to affected conversation projections and local evidence for Story 5.6.
  - Clarified that the idempotency ADR is an in-story gate and behavior code must not proceed until the ADR is accepted and indexed.
- Findings deferred:
  - Release-gate idempotency evidence aggregation and signing remain deferred to Story 5.6.
  - Cross-service or global idempotency infrastructure remains out of scope.
  - Long-term idempotency retention/cleanup details must be accepted in the ADR, but broader operational automation can be deferred unless required for correctness.
  - Provider-message identity remains non-authoritative and out of scope for durable idempotency identity.
- Final recommendation: ready-for-dev
