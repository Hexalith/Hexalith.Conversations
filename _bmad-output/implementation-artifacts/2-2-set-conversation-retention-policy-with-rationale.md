# Story 2.2: Set Conversation Retention Policy with Rationale

Status: ready-for-dev

## Story

As an authorized governance operator,
I want to set or replace a conversation retention policy with rationale,
so that conversation retention is explicit, auditable, tenant-scoped, and replay-safe.

## Acceptance Criteria

1. Authorized retention mutation command
   - Given an authorized governance operator submits a set-retention-policy command with tenant scope, conversation identity, policy reference, actor attribution, rationale, schema version, operation timestamp, and correlation metadata,
   - When the tenant access, governance role, policy reference, schema version, idempotency, and audit-precondition checks pass,
   - Then the command is accepted through the Conversations application boundary and dispatched to the Conversation aggregate,
   - And no handler, API, admin, worker, or tool path can set retention without the same tenant and governance checks.
   - And tenant access and governance permission are validated before EventStore stream resolution, aggregate hydration, projection reads, audit lookups, idempotency result disclosure, or any response mapping that could reveal conversation existence.

2. Retention policy set and replace events
   - Given a conversation has no active retention policy,
   - When the retention command succeeds,
   - Then the aggregate emits a retention-policy-set domain event carrying tenant ID, conversation ID, policy reference, rationale reference/text as approved by Story 2.1 contracts, actor Party ID, event timestamp, schema version, correlation ID, causation ID when supplied, and safe audit evidence linkage.
   - Given a conversation already has an active retention policy,
   - When a replacement command succeeds,
   - Then the aggregate emits a retention-policy-replaced domain event that records the new active policy and enough prior-policy reference to reconstruct the transition without copying raw policy internals or unsafe diagnostics.
   - And setting the same policy/rationale with the same idempotency identity returns the original sanitized outcome without appending duplicate domain or audit evidence, while replacing an active policy with a materially different policy or rationale emits exactly one replacement event in persisted event order.

3. Fail-closed rejection behavior
   - Given the operator lacks permission, tenant state is missing/stale/disabled/unavailable, conversation tenant binding mismatches, the policy reference is missing or invalid, rationale is missing, schema version is unsupported, idempotency conflicts, or the conversation is inaccessible,
   - When the command is handled,
   - Then a typed documented rejection is returned using existing Conversations-safe error/result vocabulary,
   - And no retention mutation event, audit success evidence, projection update, publication-ready event, or side effect is emitted.
   - And unsupported/missing/future schema versions, missing actor attribution, malformed correlation metadata, untrusted idempotency state, and unsafe rationale or policy-reference values fail closed with the same non-disclosing public response shape required for tenant-denied and conversation-hidden outcomes.

4. Audit pairing is mandatory
   - Given retention changes are governance mutations,
   - When a retention command succeeds,
   - Then paired audit evidence is recorded or emitted in the same governed operation boundary before the mutation is reported as successful,
   - And the response exposes only a safe audit handle or evidence reference where policy allows.
   - Given audit recording is unavailable or cannot prove pairing,
   - When the command is handled,
   - Then the retention mutation fails closed with an audit-unavailable outcome and no retention-policy-set/replaced event.
   - And no externally successful result is returned unless the retention domain evidence and audit evidence are durably correlated according to the Story 2.1 governance/audit contract pattern.

5. Deterministic replay and projected state
   - Given retention policy events exist in the event stream,
   - When aggregate state or read projections rebuild from persisted events,
   - Then replay reconstructs the same active retention policy, prior-policy transition metadata, actor attribution, rationale, policy basis, and timestamps,
   - And duplicate, reordered, or replayed events do not create divergent active policy state or duplicate audit evidence.
   - And event sequence, not operation timestamp alone, is the authority for replacement ordering; operation timestamps, actor attribution, correlation metadata, schema version, and audit handles are validated by orchestration and recorded by the aggregate without aggregate access to clocks, tenant services, audit services, projections, identity context, or configuration.
   - And projected active retention state is derived descriptive governance state only; it is not authoritative for command decisions and must not schedule deletion, redact content, suppress content, expire records, alter legal-hold behavior, or trigger UI/workflow side effects in this story.

6. Safe payload, disclosure, and observability boundaries
   - Given retention policy commands, events, results, logs, traces, test fixtures, and sample JSON are produced,
   - When retention policy data is serialized, displayed, logged, or asserted in tests,
   - Then outputs exclude raw message content, Party personal data, provider payloads, upstream detail objects, audit storage locations, EventStore substrate names, exception text, unbounded diagnostics, tokens, claims, and cross-tenant facts,
   - And unauthorized, nonexistent, cross-tenant, stale, audit-unavailable, and policy-blocked outcomes use the same non-disclosing public response shape where required by Story 1.5 and Story 2.1.
   - And rationale text, policy references, safe audit handles, and correlation metadata use bounded Conversations-owned values that cannot contain storage paths, stream names, provider identifiers, audit sink keys, projection checkpoints, raw claims, tokens, exception text, Party names, contact data, or tenant/customer names.

## Tasks / Subtasks

- [ ] Add retention policy command and contract integration (AC: 1, 3, 6)
  - [ ] Add or finalize the public set/replace retention command shape under `src/Hexalith.Conversations.Contracts/Governance/` using Story 2.1 governance metadata, policy reference, rationale, audit evidence, and outcome vocabulary.
  - [ ] Reuse existing `TenantId`, `ConversationId`, `PartyId`, `SchemaVersion`, `ConversationCommandMetadata`, and `ConversationError` conventions instead of inventing parallel identifier or result types.
  - [ ] Keep public names in Conversations language; do not expose EventStore, storage, projection, handler, audit sink, provider, tenant implementation, or upstream mechanics.
  - [ ] Add or extend serialization samples so the retention command, success result, denial result, audit-unavailable result, and policy-blocked result are covered by contract round-trip tests.

- [ ] Add domain command, aggregate handling, and replay state (AC: 1, 2, 3, 5)
  - [ ] Introduce a domain command such as `SetConversationRetentionPolicy` in the domain layer and map it from the public contract at the server/application boundary.
  - [ ] Extend `ConversationAggregate` with a retention handler that validates created/open state, tenant binding, policy/rationale presence, schema support, and replacement semantics before emitting any event.
  - [ ] Add domain events such as `RetentionPolicySetDomainEvent` and `RetentionPolicyReplacedDomainEvent` with public `ConversationEventMetadata` plus retention-specific payload.
  - [ ] Extend `ConversationState` with replay-only retention state and `Apply` methods that deterministically set or replace the active policy from events.
  - [ ] Do not implement irreversible source-event deletion, retention enforcement jobs, audit-record retention, UI workflows, or redaction behavior in this story.

- [ ] Add application-boundary authorization and audit precondition gates (AC: 1, 3, 4, 6)
  - [ ] Ensure tenant access is checked before aggregate load or command dispatch using the Story 1.5 guard/service pattern.
  - [ ] Add a governance permission/role requirement that is distinct from ordinary conversation read/write access; if the existing tenant access model cannot express it, add a narrow Conversations-owned requirement rather than broadening all write access.
  - [ ] Add an audit pairing boundary or adapter seam that can prove audit availability before a success result is returned.
  - [ ] Fail closed when the audit seam is unavailable, returns an unsafe handle, cannot correlate to the retention operation, or reports an uncertain outcome.
  - [ ] Preserve idempotency behavior from Story 1.6: duplicate compatible requests return stable outcomes, conflicting fingerprints reject without mutation, and unknown/pending outcomes remain retry-safe and non-mutating.
  - [ ] Ensure duplicate compatible requests reuse the original safe result, timestamp, and audit handle when policy allows, and prove they emit no duplicate retention or audit evidence.

- [ ] Add safe result and error mapping (AC: 3, 4, 6)
  - [ ] Map unauthorized, hidden, cross-tenant, stale tenant projection, unsupported schema, missing rationale, invalid policy reference, audit unavailable, idempotency conflict, and aggregate-not-found cases to typed sanitized responses.
  - [ ] Include missing actor attribution, malformed correlation metadata, unsafe rationale, future schema version, untrusted idempotency state, and duplicate-command conflict in the rejection matrix.
  - [ ] Keep internal diagnostics separate from public outcome vocabulary; public responses may include bounded retryability/remediation only when it does not reveal target existence, audit infrastructure, policy internals, upstream facts, or exception details.
  - [ ] Ensure safe audit handles are opaque Conversations-owned values, not storage paths, stream positions, projection checkpoints, audit sink keys, log IDs, or provider identifiers.

- [ ] Add projection/read-model integration only for active retention state (AC: 5, 6)
  - [ ] Extend the existing projection accumulator/materializer only as needed to expose active retention state and freshness/trust metadata for authorized read paths.
  - [ ] Keep projected state derived and rebuildable; do not make projections, caches, exports, UI state, or evidence bundles authoritative.
  - [ ] During stale/rebuilding/unavailable projection states, expose bounded trust/freshness signals and do not make governance decisions from stale projected state unless an approved ADR explicitly allows it.

- [ ] Add focused automated tests (AC: 1-6)
  - [ ] Contract tests cover required metadata, rationale, policy reference, schema version, timestamp, actor, tenant, conversation, correlation, causation, safe audit handle, and JSON round trips.
  - [ ] Aggregate tests cover set, replace, duplicate replay, invalid state, missing rationale, invalid policy reference, unsupported schema, tenant mismatch, closed/archived state if applicable, and no-event rejection paths.
  - [ ] Server/application tests cover tenant authorization before aggregate load, governance permission denial, stale/missing tenant projection, audit-unavailable fail-closed behavior, idempotent duplicate/conflict behavior, and same-shape non-disclosure.
  - [ ] Projection tests cover deterministic rebuild from retention events, active-policy replacement semantics, duplicate/reordered event tolerance, stale projection signaling, and no authority leakage from projection state.
  - [ ] Privacy/forbidden-surface tests prove public type names, property names, JSON payloads, logs/traces where testable, `ToString()`, validation messages, assertion output, and curated fixtures do not expose forbidden infrastructure, personal-data, provider, upstream, audit-storage, or raw diagnostic terms.

- [ ] Update developer-facing docs and samples (AC: 1, 4, 6)
  - [ ] Add XML docs for retention command/event/result contracts explaining that retention policy changes are append-only governance mutations with mandatory audit evidence.
  - [ ] Document that legal hold, source-event deletion, audit-record retention/redaction, redaction propagation, retention enforcement jobs, and UI workflows are owned by later stories unless explicitly promoted by ADR.
  - [ ] Add sample JSON for set, replace, denied, audit-unavailable, and policy-blocked outcomes using content-safe values only.

## Dev Notes

### Source Foundation

- Epic 2 is the governed retention, redaction, and audit epic. Its purpose is to let authorized users apply retention, sensitivity, redaction, archival, and privileged-action governance with paired audit evidence and fail-closed audit behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Governed Retention, Redaction, and Audit`]
- Story 2.2 covers FR42 and FR47-FR49: set/replace conversation retention policy, require paired audit evidence, fail closed when audit recording is unavailable, and allow non-governance activity during audit degradation only where explicitly permitted. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.2: Set Conversation Retention Policy with Rationale`]
- Story 2.1 is the immediate prerequisite and defines governance/audit contract vocabulary, safe metadata, outcome vocabulary, audit evidence handles, legal-hold deferral semantics, and forbidden disclosure surfaces. Implementation should consume those contracts rather than reshaping governance vocabulary in Story 2.2. [Source: `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`]

### Architecture Constraints

- EventStore remains the only durable write-side authority for conversation state. Retention changes must flow through domain commands and persisted domain events, not direct transcript/governance tables or projection writes. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Durable events use Conversations language and stable references. They must not store Party personal data, display names, contact channels, provider-owned session authority, raw upstream records, message content, or file binaries. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Governance mutations require paired audit/domain evidence and must fail closed when audit recording is unavailable. Non-governance commands may continue during audit degradation only by explicit ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Governance Security`]
- Public APIs expose domain-first Conversations commands, projections, result contracts, version metadata, typed errors, and freshness state while hiding EventStore mechanics and raw projection/storage internals. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Redaction is append-only and policy-governed by default; irreversible source-event redaction requires a future legal/compliance ADR. Retention policy setting must not imply deletion or redaction enforcement in this story. [Source: `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`]

### Existing Code Patterns To Reuse

- Public commands and events currently use sealed records with XML documentation and constructor validation under `src/Hexalith.Conversations.Contracts`.
- Existing metadata contracts to reuse:
  - `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`
  - `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`
  - `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationError*.cs`
- Existing domain patterns to extend:
  - `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs` for static `Handle(Command, State?) -> DomainResult` handlers.
  - `src/Hexalith.Conversations/State/ConversationState.cs` for replay-only deterministic state and `Apply` methods.
  - `src/Hexalith.Conversations/Validation/*` for validation classes that return typed rejection events before any success event is emitted.
- Existing server patterns to reuse:
  - `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`
  - `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs`
  - `src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs`
- Existing tests to extend:
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Tests/Aggregates/`
  - `tests/Hexalith.Conversations.Server.Tests/TenantAccess/`
  - `tests/Hexalith.Conversations.Server.Tests/Idempotency/`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/`

### Implementation Guardrails

- Validation order matters: schema and required contract fields, tenant access, governance permission, audit precondition, idempotency/fingerprint handling, aggregate validation, audit pairing, then success response. If the established idempotency executor requires a different internal order, preserve non-disclosure and no-mutation guarantees in tests.
- Tenant access and governance permission must occur before any aggregate load, EventStore stream resolution, projection lookup, audit lookup, idempotency result disclosure, or response shape that could reveal whether a conversation exists.
- Retention rationale is required but sensitive as free text. Store only what Story 2.1 permits; never echo unsafe rationale in `ToString()`, validation errors, logs, traces, assertion messages, or public diagnostics.
- Rationale validation should be stable and bounded before dev handoff: reject null, empty, whitespace-only, unsafe, or over-limit values using sanitized validation codes, and keep localization of human rationale content out of scope.
- Policy references are required and must be safe to expose. If a policy reference can reveal protected policy internals, expose a bounded public reference and keep internal policy details out of public contracts.
- Story 2.2 validates policy-reference shape and safety only unless Story 2.1 already defines a catalog-authority contract; global, tenant, external, or compliance catalog ownership is a deferred decision.
- Audit handles are evidence correlation values, not storage identity. They must be opaque, stable enough for adopter citation where allowed, and safe to return in denial/audit-unavailable cases only when policy allows.
- Audit/domain pairing is a success gate: no retention-policy-set/replaced event may be externally reported as successful unless required audit evidence is durably recorded or atomically linked in the same governed operation boundary.
- Replacement semantics must be deterministic: replaying events in persisted order yields exactly one active retention policy and an auditable prior-policy transition record where appropriate.
- Duplicate/replayed retention events should not produce divergent state. Command-time duplicate/conflict handling remains the authority for accepted/rejected command behavior.
- The aggregate records supplied, validated timestamps and metadata only. It must not read clocks, identity context, tenant context, audit services, projections, configuration, or diagnostics.

### Scope Boundaries

- In scope:
  - Retention set/replace command contract integration.
  - Domain command/event handling for retention policy set/replaced.
  - Audit precondition/pairing seam sufficient to fail closed and prove success pairing.
  - Active retention state in aggregate replay and authorized read projections.
  - Contract, aggregate, server/application, projection, privacy, and serialization tests.
- Out of scope:
  - Retention enforcement jobs or deletion workflows.
  - Legal-hold decision engine.
  - Audit-record retention/redaction governance.
  - Message redaction or sensitivity marking implementation.
  - Irreversible source-event deletion.
  - Full compliance investigation UI and evidence export workflows.
  - New authoritative storage outside EventStore.
  - Any lifecycle side effect from active retention projection state, including scheduling deletion, redaction, suppression, expiration, legal-hold changes, or UI management behavior.

### Testing Requirements

- Run at minimum:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
  - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj`
- If project references, serialization registration, or shared primitives move, run `dotnet test Hexalith.Conversations.slnx`.
- Tests must prove no mutation when audit is unavailable, tenant access is stale/missing, governance permission fails, idempotency conflicts, or command validation fails.
- Tests must include failure paths and assertion text checks where feasible, because unsafe values often leak through non-JSON surfaces after JSON payloads are cleaned.
- Tests must prove authorization-before-load/read/disclosure ordering, no domain event without audit evidence, idempotent duplicate commands without duplicate audit/domain evidence, replacement projection rebuild from persisted event order, aggregate replay without clocks/services, and no deletion/redaction/enforcement/legal-hold/UI side effects.
- Privacy tests must scan success and rejection JSON, logs/traces where testable, telemetry tags, `ToString()`, XML docs, curated samples, projection DTOs, and assertion output for EventStore stream names, provider names, audit sink details, storage paths, projection checkpoints, raw exception text, claims, tokens, Party personal data, and cross-tenant identifiers.

### Lessons Applied

- L08: Party-mode review and advanced elicitation are separate hardening passes. Story 2.2 now has a completed party-mode trace; later automation should run advanced elicitation only after this dated trace and treat both passes as pre-dev clarification, not scope expansion. [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08 - Party Review Vs. Elicitation`]

## References

- `_bmad-output/planning-artifacts/epics.md#Story 2.2: Set Conversation Retention Policy with Rationale`
- `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`
- `_bmad-output/planning-artifacts/architecture.md#Data Architecture`
- `_bmad-output/planning-artifacts/architecture.md#Governance Security`
- `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`
- `_bmad-output/project-context.md#Critical Implementation Rules`
- `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
- `src/Hexalith.Conversations/State/ConversationState.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`

## Dev Agent Record

### Agent Model Used

N/A - story created by BMAD create-story automation.

### Debug Log References

- Preflight JSON: `_bmad-output/process-notes/predev-preflight-latest.json`

### Completion Notes List

- Story context created from Epic 2 Story 2.2, Story 2.1 prerequisite contracts, architecture governance/data/API constraints, project context, and current code/test patterns.
- Status set to ready-for-dev; `sprint-status.yaml` owns the queue state.

### File List

- `_bmad-output/implementation-artifacts/2-2-set-conversation-retention-policy-with-rationale.md`

## Party-Mode Review

- ISO date and time: 2026-05-20T17:10:59Z
- Selected story key: `2-2-set-conversation-retention-policy-with-rationale`
- Command/skill invocation used: `/bmad-party-mode 2-2-set-conversation-retention-policy-with-rationale; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - All reviewers agreed the story was directionally correct but needed sharper pre-dev acceptance language around tenant/governance authorization ordering, audit/domain durability, idempotency and replacement semantics, privacy-safe public payloads, deterministic timestamp handling, and non-enforcement scope.
  - Reviewers also flagged adopter-facing validation clarity for rationale, policy references, schema versions, safe audit handles, typed rejection taxonomy, and privacy scans over non-JSON surfaces.
- Changes applied:
  - Added acceptance criteria requiring tenant/governance authorization before EventStore stream resolution, aggregate hydration, projection reads, audit lookups, idempotency result disclosure, or response mapping that could reveal existence.
  - Clarified audit/domain pairing as a success gate: no externally successful result without durably correlated retention domain evidence and audit evidence, and audit-unavailable outcomes emit no retention mutation event.
  - Clarified idempotent duplicate behavior, replacement behavior, rejection taxonomy, schema-version handling, rationale/policy-reference safety, deterministic timestamp and metadata handling, aggregate service/clock prohibitions, and projection-derived non-authority.
  - Added explicit non-enforcement scope for active retention projection state and expanded tests for authorization-before-load, no domain event without audit evidence, duplicate command evidence suppression, replacement projection rebuild, privacy scans, and no deletion/redaction/enforcement/legal-hold/UI side effects.
  - Updated L08 lesson wording to record that party-mode review is complete and advanced elicitation remains a later separate pass.
- Findings deferred:
  - Retention enforcement engine, deletion/redaction workflows, legal-hold precedence, UI management flows, policy catalog authority/lifecycle, localization of rationale content, and provider/audit backend architecture remain deferred to later stories or ADRs.
- Final recommendation: ready-for-dev
