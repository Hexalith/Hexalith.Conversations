# Story 1.10: Publish Versioned Conversation Domain Events

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a downstream Hexalith system,
I want to consume tenant-aware conversation domain events with explicit schema metadata,
so that projections and integrations can react to meaningful conversation changes without depending on internal EventStore mechanics.

## Acceptance Criteria

1. Given a meaningful conversation state change occurs, when the command succeeds and EventStore persists the domain event, then Conversations publishes tenant-aware domain events for supported changes such as conversation-created, participant-added, message-appended, reference-attached, metadata-updated, and lifecycle-changed, and published contracts use Conversations language rather than EventStore envelope or stream internals.
2. Given a published event is emitted, when downstream consumers inspect it, then the event includes schema version, event type, tenant scope, conversation identity, correlation/causation metadata, and stable references needed by the active contract, and it excludes Party personal data, raw provider payloads, file binaries, raw upstream records, redacted content, and cross-tenant metadata.
3. Given publication is delivered through Dapr/EventStore publication paths, when duplicate, replayed, or reordered delivery occurs, then downstream handlers can identify event type/version and process idempotently according to documented semantics, and projection notifications are treated as hints rather than source-of-truth state.
4. Given an event, command, or projection schema version is unsupported, when publication or consumption is validated, then unsupported versions fail with typed documented errors or compatibility diagnostics, and no consumer is required to understand internal aggregate snapshots, stream names, or SignalR group implementation details.
5. Given publication tests run, when successful events, rejected commands, duplicate delivery, unsupported versions, tenant mismatch, and content leakage cases are exercised, then tests prove correct event shape, no publication on rejected commands, bounded metadata, tenant isolation, schema metadata, and absence of forbidden payloads.

## Tasks / Subtasks

- [ ] Verify prerequisite contract and domain event names before adding publication code. (AC: 1-4)
  - [ ] Inspect the branch for Story 1.2-1.9 outputs in `src/Hexalith.Conversations.Contracts`, `src/Hexalith.Conversations`, and `src/Hexalith.Conversations.Server`.
  - [ ] Reuse existing event, metadata, schema-version, typed-error, projection, and idempotency contracts where they exist; do not create a second public event vocabulary.
  - [ ] If earlier event contracts are absent, add only the smallest versioned contract and test fixtures needed for this story, using names aligned with Story 1.2 and Story 1.7 guidance.
  - [ ] Confirm `_bmad-output/implementation-artifacts/readiness-gates.md` still records `v1 Conversations event consumers` and `EventStore envelope stability and evolution ownership` as `decided`; stop for ADR/update if either gate regresses.

- [ ] Define or complete the public Conversations event publication contract surface. (AC: 1, 2, 4)
  - [ ] Add or extend `src/Hexalith.Conversations.Contracts/Events` with versioned, serialization-friendly event contracts such as `ConversationCreatedV1`, `ParticipantAddedV1`, `MessageAppendedV1`, `ReferenceAttachedV1`, `MetadataUpdatedV1`, and `ConversationLifecycleChangedV1`, or use the exact established equivalents already present.
  - [ ] Add shared publication metadata such as `schemaVersion`, `eventType`, `tenantId`, `conversationId`, `correlationId`, `causationId`, contract timestamp, stable event/deduplication identity, and stable reference IDs required by the active contract.
  - [ ] Keep public event contracts infrastructure-free: no EventStore envelope types, Dapr types, stream names, sequence storage concepts, snapshot details, SignalR group names, ASP.NET Core types, or upstream client DTOs.
  - [ ] Add typed unsupported-version diagnostics or errors for event, command, and projection schema validation, reusing Story 1.2 error vocabulary if present.

- [ ] Add the server-side publication mapping boundary under `src/Hexalith.Conversations.Server/Publication`. (AC: 1-4)
  - [ ] Map persisted Conversations domain events into public Conversations publication contracts only after the command succeeds and EventStore persistence has completed.
  - [ ] Treat the Hexalith.EventStore envelope as inherited infrastructure; do not modify it or expose it. Conversations owns the domain event schema and public contract versioning.
  - [ ] Isolate EventStore/Dapr-specific references inside `Server/EventStore` or `Server/Publication`; do not leak those references into `Contracts`, domain aggregate logic, projections, read models, or client contracts.
  - [ ] Ensure rejected commands, no-op idempotent replays, failed tenant checks, failed Party validation, and incompatible payload/version checks do not publish successful state-change events.
  - [ ] If EventStore already publishes the persisted event to Dapr, implement only the Conversations-safe mapping/metadata and tests needed to prove the public shape; do not add a second publisher that duplicates delivery.

- [ ] Document and implement idempotent consumer semantics for duplicate/replayed/reordered delivery. (AC: 3)
  - [ ] Provide a stable event identity or deduplication key that consumers can use without knowing stream names, aggregate snapshots, or EventStore storage topology.
  - [ ] Preserve correlation and causation metadata from command handling and idempotency flows so downstream diagnostics can trace publication without payload disclosure.
  - [ ] Document that pub/sub and projection notifications are hints: consumers must treat EventStore history as authoritative and must tolerate at-least-once delivery.
  - [ ] Reject or quarantine tenant-mismatched or unsupported-version messages before any projection or downstream mutation.

- [ ] Add focused contract, publication, and boundary tests. (AC: 1-5)
  - [ ] Add contract serialization tests under `tests/Hexalith.Conversations.Contracts.Tests/Events` proving JSON names, schema version, event type, tenant scope, conversation identity, correlation/causation metadata, and stable references are present.
  - [ ] Add property/payload scanning tests proving published event contracts exclude Party display names, contact data, identifiers, person/organization details, raw provider prompts/responses, file binaries, raw upstream records, redacted content, tokens, claims, EventStore stream names, snapshots, envelopes, SignalR groups, and projection internals.
  - [ ] Add server publication tests under `tests/Hexalith.Conversations.Server.Tests/Publication` for successful event mapping, rejected-command no-publication, duplicate/replayed event identity stability, reordered delivery handling, unsupported-version diagnostics, and tenant-mismatch rejection/quarantine.
  - [ ] Update `.csproj` XML boundary tests so `Contracts` stays infrastructure-free and EventStore/Dapr references, if required, stay only in approved server publication/write-adapter boundaries.
  - [ ] Use fake EventStore/Dapr publication adapters for normal unit tests; do not require Aspire runtime, live Dapr sidecars, Redis, tenant seed data, provider credentials, external cloud resources, or nested submodule initialization.

- [ ] Validate the implementation scope. (AC: 5)
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run `dotnet restore`, `dotnet build`, and `dotnet test .\Hexalith.Conversations.slnx` if assets are stale.
  - [ ] Do not run recursive submodule initialization. Root-level sibling module reads are enough when EventStore publication behavior needs inspection.
  - [ ] Do not add named cross-module consumers, release manifest signing, provider portability proof, schema evolution/upcaster conformance, replay/rebuild proof, governance audit events, FrontComposer UI, SignalR client behavior, or raw HTTP adopter examples in this story.
  - [ ] Leave `sprint-status.yaml` untouched during dev-story unless the dev workflow owns the status transition.

## Dev Notes

### Scope Boundary

Story 1.10 creates or completes the Conversations-safe event publication contract and mapping boundary for already persisted conversation state changes. It does not make another Hexalith module a committed v1 consumer, and it does not own provider portability or release conformance packaging. The readiness decision says v1 events are internal/publication-ready; named cross-module consumption requires v1.1 scope or an ADR. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.10: Publish Versioned Conversation Domain Events`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#v1-conversations-event-consumers`]

Publication must happen after a successful command result and durable EventStore persistence. Rejections, failed tenant access, failed participant validation, idempotency conflicts, incompatible duplicate payloads, unsupported versions, and no-op outcomes must not publish successful state-change events. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`; `_bmad-output/planning-artifacts/architecture.md#Integration Points`]

Story 1.11 owns replay/schema-versioning proof and projection rebuild behavior. Story 5.8 and Story 5.9 own provider portability and event schema evolution release evidence. This story may add local tests and compatibility diagnostics needed for publication, but it must not broaden into conformance manifest signing or full replay/upcaster proof. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior`; `_bmad-output/planning-artifacts/epics.md#Story 5.9: Prove Event Schema Evolution`]

### Architecture Compliance

Hexalith.EventStore is the authoritative write-side substrate. Conversations owns domain event names, schemas, and public contract versioning, while EventStore owns aggregate routing, persistence, snapshots, command status, publication plumbing, and projection invalidation. Do not expose raw EventStore envelopes, stream internals, snapshot mechanics, storage offsets, expected revisions, or SignalR groups as Conversations contracts. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#eventstore-envelope-stability-and-evolution-ownership`]

Durable and published events must use Conversations language and past-tense domain names such as `ConversationCreated`, `MessageAppended`, `ParticipantAdded`, `ReferenceAttached`, `MetadataUpdated`, and `ConversationLifecycleChanged`. Schema versions are contract versions, not hidden serializer details. [Source: `_bmad-output/planning-artifacts/architecture.md#Naming Patterns`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`]

Event payloads may carry stable references only: tenant scope, conversation identity, Party IDs, Project/Folder/File IDs, message IDs, provider correlation metadata where approved, schema version, event type, correlation/causation, and contract timestamps. They must not carry Party display names, contact channels, identifiers, person or organization details, raw provider payloads, file binaries, raw upstream records, authorization state, claims, tokens, redacted content, or raw upstream problem details. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/architecture.md#Event System Patterns`]

### Current Repository State and Previous Story Intelligence

The current Conversations source tree is still mostly scaffold/marker code. `src/Hexalith.Conversations.Contracts`, `src/Hexalith.Conversations`, and `src/Hexalith.Conversations.Server` exist, but inspected production files contain project references and marker types rather than the earlier story behavior. Treat story files 1.2-1.9 as planned context, not proof that implementation has landed. [Source: `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`; `src/Hexalith.Conversations/Hexalith.Conversations.csproj`; `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`; `_bmad-output/implementation-artifacts/1-9-resolve-parties-and-upstream-references-at-read-time.md#Current Repository State and Previous Story Intelligence`]

Carry forward the repeated boundary-test lesson from earlier stories: compiled assembly-reference checks can pass vacuously when assemblies contain only marker code. For this story, update tests to inspect `.csproj` XML directly when proving that `Contracts` remains infrastructure-free and EventStore/Dapr references stay in approved server boundaries. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Current Repository State and Previous Story Intelligence`; `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`; `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`]

Recent git history is mainly story/scaffold work (`4c27576 feat: Add story 1.9 for resolving parties and upstream references at read time`, `062bee3 docs: create story 1.2 contract definitions`, `4479ced feat: Update subproject commits and add integration tests for scaffold validation`). Preserve the scaffold-first style and avoid assuming a full runtime stack exists. [Source: `git log --oneline -5`]

### EventStore and Dapr Publication Intelligence

Local EventStore contracts include `EventMetadata` with message ID, aggregate ID/type, tenant ID, domain, sequence number, global position, timestamp, correlation ID, causation ID, user ID, domain service version, event type name, metadata version, and serialization format; `EventEnvelope.ToString()` redacts payload bytes. Use these as inherited infrastructure facts when mapping, but expose a Conversations-safe contract shape. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/EventMetadata.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/EventEnvelope.cs`]

EventStore integration tests prove current Dapr publication emits CloudEvents with topic shape such as `tenant-a.counter.events`, type ending in the domain event name, source `hexalith-eventstore/{tenant}/{domain}`, ID based on correlation/sequence, and tenant/domain/aggregate metadata. Conversations may rely on this behavior for local proof, but public Conversations contracts must not require consumers to know EventStore stream topology or substrate aggregate IDs. [Source: `Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/ContractTests/PubSubDeliveryProofTests.cs`]

Current Dapr docs state that Dapr pub/sub uses CloudEvents 1.0 and automatically wraps published messages unless raw payload behavior is requested. Dapr also documents at-least-once redelivery when the app does not return success. Publication tests and docs must therefore assume duplicate delivery and use explicit event identity/version semantics. [Source: Dapr docs, `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/`; Dapr docs, `https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/`]

### Versioning and Unsupported Versions

Conversation events are immutable versioned contracts. Evolution uses additive schema changes, upcasters, or new event types. Breaking changes require compatibility proof through conformance tests. In-place event rewrites are forbidden unless a legal/compliance ADR explicitly permits source-event redaction or hard delete. [Source: `_bmad-output/planning-artifacts/architecture.md#Migration / Versioning`]

Unsupported event, command, or projection schema versions must return typed documented errors or compatibility diagnostics. Do not silently skip unknown historical event types during replay or projection consumption; unknown types are correctness failures unless a documented compatibility rule says otherwise. [Source: `_bmad-output/planning-artifacts/architecture.md#Migration / Versioning`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

Use `System.Text.Json` web/default serialization unless the branch already established a stronger contract requirement. Microsoft documentation confirms `System.Text.Json` parses and writes `DateTime` and `DateTimeOffset` according to the ISO 8601-1:2019 extended profile, so do not add Newtonsoft.Json or custom date converters just for timestamps. [Source: Microsoft Learn, `https://learn.microsoft.com/dotnet/standard/datetime/system-text-json-support`]

### File and Test Placement

Expected production files, depending on prerequisite story state, belong under:

- `src/Hexalith.Conversations.Contracts/Events` for versioned public event payloads and shared publication metadata.
- `src/Hexalith.Conversations.Contracts/Versioning` or the existing equivalent for schema-version and unsupported-version contracts.
- `src/Hexalith.Conversations.Server/Publication` for mapping, filtering, event identity, and delivery-boundary adapters.
- `src/Hexalith.Conversations.Server/EventStore` only for EventStore-specific integration details.
- `src/Hexalith.Conversations/Conversations` or the established domain folder for deterministic aggregate-emitted domain events.

Expected tests belong under:

- `tests/Hexalith.Conversations.Contracts.Tests/Events`
- `tests/Hexalith.Conversations.Contracts.Tests/Versioning`
- `tests/Hexalith.Conversations.Server.Tests/Publication`
- `tests/Hexalith.Conversations.Server.Tests/Boundaries`
- `tests/Hexalith.Conversations.Tests` only for pure domain event fixtures and aggregate result behavior

Shared fake publishers or builders may be added to `src/Hexalith.Conversations.Testing` only if they are deterministic, runtime-free, and reusable by later publication/projection/replay stories.

### Security and Privacy Guardrails

- Tenant authorization must complete before aggregate load, projection read, publication detail access, or audit-sensitive metadata access.
- Publication records must be tenant-scoped and must never combine metadata from multiple tenants.
- Do not log event payloads, command payloads, personal data, raw provider content, raw upstream problem details, secrets, tokens, claims, or user-controllable display names.
- Publication failures are infrastructure outcomes; domain rejections are expected domain outcomes. Do not dead-letter or publish rejected commands as successful conversation changes.
- Downstream event handlers must be idempotent and replay-safe; duplicate delivery must not create duplicate read-model or integration effects.
- Projection notifications are hints. EventStore history remains authoritative when published hints disagree with replayed history.

[Source: `_bmad-output/project-context.md#Critical Implementation Rules`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Integration Points`]

### Anti-Reinvention Warnings

- Do not create a separate message bus abstraction if EventStore/Dapr publication already provides the persistence-after-publication path needed for this story.
- Do not add direct Dapr or EventStore references to `Contracts` or the typed client.
- Do not expose raw EventStore envelopes as the public Conversations event contract.
- Do not invent named downstream consumers; v1 has no committed cross-module consumer dependency.
- Do not publish from aggregate logic, projections, read models, UI components, or test helpers that bypass command persistence.
- Do not use provider session IDs, Party display names, project labels, folder paths, or file names as durable event identity.
- Do not make projection state authoritative or use publication events as a substitute for EventStore replay.

### Validation

Validation must stay local and deterministic by default. Unit tests should use fake publishers, fake persisted-event envelopes, and sentinel data to prove no forbidden values leak. Integration/E2E publication proof through Aspire/Dapr may be added only if the local topology already exists and remains optional for ordinary story validation. [Source: `_bmad-output/project-context.md#Testing Rules`; `Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/ContractTests/PubSubDeliveryProofTests.cs`]

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.10: Publish Versioned Conversation Domain Events`
- `_bmad-output/planning-artifacts/epics.md#Story 1.6: Add Idempotent Command Handling`
- `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`
- `_bmad-output/planning-artifacts/epics.md#Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior`
- `_bmad-output/planning-artifacts/architecture.md#Data Architecture`
- `_bmad-output/planning-artifacts/architecture.md#Migration / Versioning`
- `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/1-9-resolve-parties-and-upstream-references-at-read-time.md`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/EventMetadata.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/EventEnvelope.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/ContractTests/PubSubDeliveryProofTests.cs`
- `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`
- Dapr CloudEvents docs: `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/`
- Dapr publish/subscribe docs: `https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/`
- Microsoft Learn System.Text.Json DateTime support: `https://learn.microsoft.com/dotnet/standard/datetime/system-text-json-support`

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
