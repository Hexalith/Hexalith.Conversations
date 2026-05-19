# Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform owner,
I want proof that conversation records can be replayed, rebuilt, and evolved safely,
so that the first conversation substrate is trustworthy before governance and compliance workflows build on it.

## Acceptance Criteria

1. Given a tenant-scoped conversation event stream exists, when aggregate state is rehydrated from the ordered events, then the reconstructed state matches the expected conversation identity, lifecycle, participants, messages, business references, provider correlation metadata, and attribution, and replay is deterministic for the same event history and contract version.
2. Given v1 projections are deleted or marked rebuilding, when the projection rebuild process replays persisted events, then it produces functionally equivalent summary and detail read models for the same tenant, conversation, event history, and contract version, and rebuild progress, stale state, unavailable state, and completion are surfaced through freshness metadata.
3. Given old, mixed, additive, or unsupported event versions exist in a stream, when replay and projection handlers process them, then supported versions replay through documented compatibility or upcaster behavior, and unsupported versions fail with typed documented errors rather than being skipped silently.
4. Given derived state disagrees with replayed EventStore state, when verification detects the disagreement, then EventStore history wins, the derived artifact is marked stale, invalid, quarantined, or rebuilding, and content-safe diagnostics are emitted, and governed disclosure actions remain blocked unless a later ADR explicitly permits action on stale state.
5. Given replay and rebuild tests run, when projection deletion, duplicate events, mixed-version streams, unsupported versions, stale derived state, tenant mismatch, and provider correlation changes are exercised, then tests prove deterministic replay, rebuild equivalence, version-aware behavior, tenant isolation, provider-correlation non-authority, and safe diagnostics, and the output can feed the release-evidence placeholder or manifest entry for Epic 1.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate event schema evolution evidence is carried forward into Story 5.9 for manifest aggregation and signing.

## Tasks / Subtasks

- [ ] Confirm replay, freshness, and schema-version gates before implementation. (AC: 1-5)
  - [ ] Verify `_bmad-output/implementation-artifacts/readiness-gates.md` still records EventStore envelope ownership and Projection freshness blocking semantics as `decided` or `waived`.
  - [ ] Use the readiness decision that EventStore envelope stability is inherited infrastructure for v1; Conversations owns domain event schemas, public contract versions, compatibility tests, and typed unsupported-version behavior.
  - [ ] Use the approved freshness vocabulary `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; only `Current` enables trust-bearing decisions unless a later ADR grants a narrower exception.
  - [ ] Treat `docs/adrs/0001-idempotency-contract.md` as active branch context only if it is committed or present during implementation; do not depend on uncommitted ADR state without re-reading the branch.
  - [ ] Stop for architecture clarification before changing EventStore envelope semantics, adding a public trust state, storing new authoritative durable state outside EventStore, or adding a long-running production rebuild worker beyond local proof fixtures.

- [ ] Add or complete deterministic replay proof for aggregate state. (AC: 1, 3)
  - [ ] Reuse `ConversationAggregate`, `ConversationState`, `ConversationCreatedDomainEvent`, `ParticipantAddedDomainEvent`, `ConversationRejectedDomainEvent`, `ConversationEventMetadata`, and `SchemaVersion` where present; do not create a second replay/event vocabulary.
  - [ ] Add only the narrowest fixtures needed for message, reference, metadata, lifecycle, or provider-correlation events that are absent on the branch; fixture names must match established public event names in `src/Hexalith.Conversations.Contracts/Events`.
  - [ ] Prove replay is side-effect free and deterministic for ordered event streams by comparing reconstructed state from the same event sequence across repeated runs.
  - [ ] Prove duplicate-tolerant behavior only where it is explicitly documented, such as participant replay no-op for duplicate membership. Do not silently tolerate duplicate creation or unknown event types.
  - [ ] Reject or surface malformed metadata, missing tenant identity, tenant mismatch, missing conversation identity, missing schema version, and unsupported schema version as typed content-safe failures before a confident replay result is returned.
  - [ ] Ensure provider correlation data remains metadata only. Provider session, response, or model identifiers must never become durable conversation identity or replay authority.

- [ ] Add or complete projection rebuild proof around existing projection contracts. (AC: 2, 4, 5)
  - [ ] Reuse `ConversationSummaryProjection`, `ConversationMessageProjection`, `ProjectionFreshness`, and `ProjectionTrustState` where present; extend them only when required by Story 1.7 commitments and current tests.
  - [ ] Add a server-side projection rebuild proof under `src/Hexalith.Conversations.Server/Projections` only if the branch has no existing equivalent. Keep it a local deterministic rebuild service or test seam, not a production scheduler.
  - [ ] Rebuild summary/detail projections from ordered event fixtures and prove equivalence after projection deletion or rebuild from scratch for the same tenant, conversation, event history, and contract version.
  - [ ] Surface rebuilding, stale, unavailable, forbidden, redacted, and current states through the existing `ProjectionFreshness`/`ProjectionTrustState` vocabulary. Missing or contradictory metadata must not be reported as `Current`.
  - [ ] Make rebuild tenant-scoped. Mixed-tenant poison events, cross-tenant conversation IDs, tenant-mismatched metadata, and tenant-hidden records must fail closed or quarantine before any derived state becomes visible.
  - [ ] When rebuilt projection state disagrees with an existing derived artifact, mark the existing artifact stale, invalid, quarantined, or rebuilding; EventStore replay remains the authority.
  - [ ] Keep rebuild diagnostics content-safe: include bounded identifiers such as tenant scope, conversation identity, schema version, event type, event identity, projection contract version, and correlation/causation IDs only. Do not include message content, Party display data, provider payloads, raw upstream records, redacted content, EventStore stream names, storage offsets, or raw exception text.

- [ ] Define version compatibility and unsupported-version behavior. (AC: 3, 5)
  - [ ] Use `SchemaVersion.Current` and existing schema-version serialization behavior for v1 contracts unless a committed ADR has superseded it.
  - [ ] Document and test the v1 compatibility rule: required v1 fields must be present; additive v1 fields may be ignored only when the active contract permits it; unsupported major/future versions fail closed.
  - [ ] Add at least one local additive-version fixture that demonstrates compatible replay/projection behavior without requiring the full upcasting framework deferred to Story 5.9.
  - [ ] Unknown historical event types are correctness failures unless a documented compatibility rule says otherwise. Do not silently skip unknown events during replay or rebuild.
  - [ ] Unsupported event, command, or projection versions must map to typed documented errors or compatibility diagnostics using existing `ConversationError`/`ConversationErrorCode` vocabulary where possible.
  - [ ] Public errors and diagnostics must not echo unsupported payload fragments, raw JSON, provider metadata, authorization context, rejected command bodies, or raw infrastructure exception messages.

- [ ] Add local verification output that can feed later release evidence. (AC: 4, 5)
  - [ ] Produce a small local evidence object or test result fixture that records covered test IDs, story key, contract/schema versions, projection contract version, tenant scope, rebuild status, pass/fail status, safe diagnostic code, and timestamp.
  - [ ] Keep release signing, conformance manifest aggregation, provider portability proof, and event schema evolution release evidence out of this story; Story 5.8 and Story 5.9 own those release-gate artifacts.
  - [ ] Include provider correlation change tests only to prove provider IDs are not authority. Do not implement a full provider migration or portability suite here.
  - [ ] Ensure verification and rebuild detail access uses the same tenant-access and redaction/freshness rules as normal reads. Background, admin, CLI, test, or diagnostic paths are not privileged bypasses.

- [ ] Add focused tests. (AC: 1-5)
  - [ ] Add aggregate/domain replay tests under `tests/Hexalith.Conversations.Tests/Aggregates` or `tests/Hexalith.Conversations.Tests/Replay` for ordered replay, duplicate participant event replay, duplicate creation failure, rejection-event no-op, schema-version validation, tenant mismatch, malformed metadata, provider-correlation non-authority, and deterministic repeated replay.
  - [ ] Add projection rebuild tests under `tests/Hexalith.Conversations.Server.Tests/Projections` for projection deletion/rebuild equivalence, rebuilding state, stale metadata, unavailable projection store, mixed-tenant poison events, derived-state disagreement, duplicate/replayed events, unsupported-version diagnostics, and content-safe diagnostic output.
  - [ ] Add contract/versioning tests under `tests/Hexalith.Conversations.Contracts.Tests/Versioning` proving schema-version JSON shape, current version behavior, missing/unsupported version rejection, additive-field tolerance where approved, and no EventStore/Dapr/internal topology terms in public contracts.
  - [ ] Add payload/privacy scans proving replay, rebuild, diagnostics, logs, and evidence fixtures exclude Party personal data, names, emails, contact values, raw provider prompts/responses, provider session authority, file binaries, raw upstream records, authorization claims, tokens, redacted content, EventStore stream names, storage positions, snapshots, Dapr topics, SignalR groups, and raw exception messages.
  - [ ] Keep tests local and deterministic with in-memory event sequences, fake projection stores, fake clocks, fake diagnostics, and safe fixture IDs. Do not require Aspire runtime, Dapr sidecars, EventStore server runtime, tenant seed data, provider credentials, external cloud resources, or nested submodule initialization.
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run restore/build/test if assets are stale. Do not run recursive submodule initialization.

- [ ] Validate implementation scope before closing the story. (AC: 1-5)
  - [ ] Confirm EventStore remains the only v1 write authority and every projection/cache/evidence fixture created by this story declares itself derived, disposable, and rebuildable.
  - [ ] Confirm public APIs/contracts still expose Conversations concepts only; no EventStore envelopes, stream names, snapshots, expected revisions, storage offsets, subscription topology, or raw projection internals are public.
  - [ ] Confirm trust-bearing reads, verification, rebuild, export-like proof, and diagnostics block on `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and unsupported-version states unless an ADR explicitly permits an exception.
  - [ ] Leave `sprint-status.yaml` untouched during dev-story unless the dev workflow owns the status transition.

## Dev Notes

### Scope Boundary

Story 1.11 proves local replay, schema-version handling, and projection rebuild behavior for the Epic 1 foundation. It does not own full event schema evolution release evidence, provider portability release proof, signed conformance manifests, production rebuild orchestration, export/evidence bundle signing, governance redaction replay, FrontComposer UI, or named downstream consumers. Story 5.8 owns provider portability release proof; Story 5.9 owns event schema evolution release-gate evidence. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior`; `_bmad-output/planning-artifacts/epics.md#Story 5.9: Prove Event Schema Evolution`]

This story may create local evidence fixtures that later stories can aggregate, but it must not imply GA release evidence is complete. The local closure bar is automated proof that the current branch can replay and rebuild deterministic, tenant-scoped v1 state and reject unsupported versions safely. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior`]

### Gate Decisions

The EventStore envelope ownership gate is decided: Hexalith.EventStore is stable inherited infrastructure for v1, while Conversations owns domain event schemas, public contract versioning, and compatibility tests. Do not evolve the EventStore envelope or expose it publicly in this story. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#EventStore envelope stability and evolution ownership`]

Projection freshness blocking semantics are decided: use `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; only `Current` is accepted for trust-bearing decisions unless an ADR grants an exception. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]

Temporal evidence anchor is decided for later evidence links: v1 temporal cursor is EventStore event position plus projection version, with timestamp as supporting metadata. Story 1.11 can reference this in tests or evidence fixtures, but must not expose raw EventStore topology as public API. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Temporal evidence anchor`]

### Current Branch Reality

The branch already contains production contract and domain files that earlier story artifacts did not assume were landed. Inspect before editing and build on them. Key current names include `ConversationEventMetadata`, `ConversationEventType`, `SchemaVersion`, `ProjectionFreshness`, `ProjectionTrustState`, `ConversationSummaryProjection`, `ConversationMessageProjection`, `ConversationAggregate`, `ConversationState`, `ConversationCreatedDomainEvent`, `ParticipantAddedDomainEvent`, and `ConversationRejectedDomainEvent`. [Source: `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`; `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`; `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshness.cs`; `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`; `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`; `src/Hexalith.Conversations/State/ConversationState.cs`]

At story creation time, Story 1.6 implementation work is active in the working tree (`1-6-add-idempotent-command-handling: in-progress`). Treat its code and ADR files as branch reality only after re-reading them during dev-story. Do not rely on automation-run observations as a substitute for implementation-time inspection. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/process-notes/predev-preflight-latest.json`]

### Replay and Authority Rules

EventStore is the only durable v1 source of truth. Aggregates replay from ordered Conversations events; projections, caches, exports, UI state, memories, verification snapshots, and local evidence fixtures are derived and repairable. If derived state disagrees with replayed EventStore history, EventStore wins and the derived artifact must become stale, invalid, quarantined, or rebuilding. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/project-context.md#Framework-Specific Rules`]

Replay must be deterministic and side-effect free. Application handlers perform tenant authorization, Party validation, policy checks, idempotency checks, and command mapping before aggregate invocation; aggregate replay must not call Tenants, Parties, provider APIs, Dapr, EventStore clients, or projection stores. [Source: `_bmad-output/planning-artifacts/architecture.md#Validation Strategy`; `_bmad-output/project-context.md#Critical Implementation Rules`]

Provider correlation metadata is allowed only as metadata. Provider chat/session/response IDs must not become durable conversation identity, dedupe authority, replay authority, sort authority, or projection ownership. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior`]

### Schema Versioning

Conversation events are immutable, versioned Conversations contracts. Evolution uses additive schema changes, upcasters, or new event types. Breaking changes require compatibility proof through conformance tests. In-place event rewrites are forbidden unless an approved legal/compliance ADR defines source-event redaction or hard delete behavior. [Source: `_bmad-output/planning-artifacts/architecture.md#Migration / Versioning`]

Unsupported event, command, or projection schema versions must return typed documented errors. Tests must cover old event replay, mixed-version stream replay, unknown event handling, projection compatibility, and unsupported-version failure. Unknown historical event types are correctness failures unless a documented compatibility rule says otherwise. [Source: `_bmad-output/planning-artifacts/architecture.md#Migration / Versioning`; `_bmad-output/planning-artifacts/architecture.md#Event System Patterns`]

The current public version primitive is `SchemaVersion` with `SchemaVersion.Current = new(1)` and a positive integer invariant. Use that existing contract unless a committed ADR supersedes it before implementation starts. [Source: `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`]

### Projection Rebuild and Freshness

Projection rebuild proves derived read models can be deleted or marked rebuilding and regenerated from EventStore history into functionally equivalent summary/detail state for the same tenant, conversation, event history, and contract version. Rebuild proof must include progress/trust states and must not present partial or contradictory state as `Current`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior`; `_bmad-output/implementation-artifacts/1-7-project-conversation-read-models-with-freshness-metadata.md#Advanced Elicitation Hardening`]

Story 1.7 already established that freshness is server-computed trust evidence. Caller-supplied, cached, or deserialized freshness metadata must not upgrade trust. Mixed-generation summary/detail reads, failed checkpoint-after-mutation, stale metadata, contradictory metadata, and unavailable stores must degrade to a non-current state. [Source: `_bmad-output/implementation-artifacts/1-7-project-conversation-read-models-with-freshness-metadata.md#Advanced Elicitation Hardening`]

Rebuild, verification, and diagnostics are privileged-looking paths but are not authorization bypasses. Tenant access fails closed before projection read, rebuild detail access, export, verification detail access, admin action, MCP/tool action, or background work that can read, write, rebuild, export, or infer conversation data. [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### File and Test Placement

Expected production files, only where needed, belong under:

- `src/Hexalith.Conversations/State` or `src/Hexalith.Conversations/Aggregates` for pure replay behavior that belongs to the domain model.
- `src/Hexalith.Conversations/Events` for internal domain event fixtures when current public contracts are absent.
- `src/Hexalith.Conversations.Contracts/Versioning` and `src/Hexalith.Conversations.Contracts/Errors` for public schema-version and typed error behavior.
- `src/Hexalith.Conversations.Contracts/Projections` and `src/Hexalith.Conversations.Contracts/TrustStates` for public projection/freshness contracts.
- `src/Hexalith.Conversations.Server/Projections` for deterministic rebuild proof, derived projection state, and server-side diagnostics.
- `src/Hexalith.Conversations.Testing` only for reusable deterministic builders or fakes that future stories can share.

Expected tests belong under:

- `tests/Hexalith.Conversations.Tests/Aggregates` or `tests/Hexalith.Conversations.Tests/Replay`
- `tests/Hexalith.Conversations.Contracts.Tests/Versioning`
- `tests/Hexalith.Conversations.Contracts.Tests/Projections`
- `tests/Hexalith.Conversations.Server.Tests/Projections`
- `tests/Hexalith.Conversations.Server.Tests/Boundaries`

Do not add direct EventStore/Dapr references to `Contracts` or typed client packages. Boundary tests should inspect `.csproj` XML and source imports as well as compiled assembly references because marker assemblies can make reflection-only checks pass vacuously. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`; `_bmad-output/implementation-artifacts/1-7-project-conversation-read-models-with-freshness-metadata.md#Current Repository State and Previous Story Intelligence`]

### Security and Privacy Guardrails

- Rebuild and replay diagnostics must be content-safe and bounded.
- Do not log or serialize message text, redacted content, Party display names, contact values, personal identifiers, raw provider prompts/responses, raw upstream records, file binaries, access tokens, claims, authorization state, rejected command bodies, raw JSON payload fragments, or raw exception text.
- Do not expose EventStore stream names, storage offsets, snapshots, expected revisions, envelopes, subscription topology, Dapr topics, SignalR groups, or projection internals in public contracts or adopter-facing errors.
- Unauthorized, nonexistent, cross-tenant, forbidden, and hidden-by-tenant records must remain indistinguishable through read shape, rebuild status, diagnostics, counts, cursors, timestamps, telemetry dimensions, or evidence fixture fields.

[Source: `_bmad-output/project-context.md#Critical Implementation Rules`; `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`]

### Anti-Reinvention Warnings

- Do not build a transcript table, authoritative projection store, separate event store, or provider-session recovery system.
- Do not create a second public event, projection, schema-version, trust-state, or error vocabulary when branch types already exist.
- Do not make projection state authoritative or use published events as a substitute for EventStore replay.
- Do not implement full upcasting framework, conformance manifest signing, provider portability migration, production rebuild scheduler, governance redaction replay, or FrontComposer/admin UI in this story.
- Do not use provider IDs, Party display names, file names, project labels, or folder paths as replay identity, dedupe keys, or evidence anchors.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior`
- `_bmad-output/planning-artifacts/epics.md#Story 1.7: Project Conversation Read Models with Freshness Metadata`
- `_bmad-output/planning-artifacts/epics.md#Story 1.10: Publish Versioned Conversation Domain Events`
- `_bmad-output/planning-artifacts/epics.md#Story 5.9: Prove Event Schema Evolution`
- `_bmad-output/planning-artifacts/architecture.md#Data Architecture`
- `_bmad-output/planning-artifacts/architecture.md#Migration / Versioning`
- `_bmad-output/planning-artifacts/architecture.md#Process Patterns`
- `_bmad-output/planning-artifacts/prd.md#Event Sourcing, Projections, And Publication`
- `_bmad-output/planning-artifacts/prd.md#Projection Freshness`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/1-7-project-conversation-read-models-with-freshness-metadata.md`
- `_bmad-output/implementation-artifacts/1-10-publish-versioned-conversation-domain-events.md`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshness.cs`
- `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
- `src/Hexalith.Conversations/State/ConversationState.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`
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

- 2026-05-19: Story created and moved to ready-for-dev by BMAD create-story workflow.
