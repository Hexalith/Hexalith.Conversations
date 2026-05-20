# Story 2.3: Mark Conversation Content as Sensitive

Status: ready-for-dev

## Story

As an authorized governance operator,
I want to mark conversation content as sensitive with policy attribution,
so that downstream projections, UI, exports, and evidence workflows can treat sensitive material safely.

## Acceptance Criteria

1. Authorized sensitivity marking command
   - Given an authorized governance operator submits a mark-sensitive command with tenant scope, conversation identity, governed target reference, sensitivity category, policy reference, actor attribution, rationale, schema version, operation timestamp, and correlation metadata,
   - When tenant access, governance role, target, policy, schema version, idempotency, and audit-precondition checks pass,
   - Then the command is accepted through the Conversations application boundary and dispatched to the Conversation aggregate,
   - And no handler, API, admin, worker, MCP/tool, export, rebuild, or verification path can mark content sensitive without the same tenant and governance checks.
   - And tenant access and governance permission are validated before aggregate load, EventStore stream resolution, target existence checks, projection reads, audit lookups, idempotency result disclosure, or differentiated errors that could reveal conversation or target existence.

2. Content-safe sensitivity-marked event
   - Given a mark-sensitive command succeeds for a conversation, message, attachment/file reference, participant attribution, or defined content segment,
   - When the aggregate emits the sensitivity-marked domain event,
   - Then the event carries tenant ID, conversation ID, governed target reference, sensitivity category, policy reference, rationale reference/text as approved by Story 2.1 contracts, actor Party ID, event timestamp, schema version, correlation ID, causation ID when supplied, and safe audit evidence linkage,
   - And the event does not store raw sensitive content, message text, prompt fragments, Party personal data, provider payloads, file binaries, upstream detail objects, audit storage locations, or unbounded diagnostics.
   - And policy attribution is limited to Story 2.1-safe policy identifiers, version/source category, bounded rationale, and opaque evidence handles; raw policy text, storage coordinates, audit sink identity, customer names, provider identifiers, and Party personal data are forbidden.
   - And defined content segment targets are identifier-only governed target references; offsets, excerpts, serialized text, provider payload coordinates, UI selection text, or exported document fragments are not valid durable target identity.
   - And the durable event timestamp and audit evidence timestamp are server-trusted operation metadata; caller-supplied timestamps are request context only unless Story 2.1 explicitly defines a trusted caller timestamp contract.

3. Fail-closed rejection behavior
   - Given the operator lacks permission, tenant state is missing/stale/disabled/unavailable, conversation tenant binding mismatches, target reference is missing/cross-tenant/unsupported/already-incompatible, policy reference is missing or invalid, rationale is missing, schema version is unsupported, idempotency conflicts, or the conversation is inaccessible,
   - When the command is handled,
   - Then a typed documented rejection is returned using existing Conversations-safe error/result vocabulary,
   - And no sensitivity mutation event, audit success evidence, projection update, publication-ready event, export marker, or other side effect is emitted.
   - And unauthorized, nonexistent, cross-tenant, stale, audit-unavailable, policy-blocked, unsupported-target, invalid-policy, invalid-target, inaccessible-conversation, and idempotency-conflict outcomes remain non-disclosing until tenant/governance authorization has succeeded.
   - And compatible duplicate requests return the stable sanitized result without appending duplicate sensitivity or audit evidence, while materially different category, policy, rationale, target, tenant context, or schema values with the same idempotency identity reject as typed conflicts without mutation.

4. Audit pairing is mandatory
   - Given sensitivity marking is a governance mutation,
   - When a sensitivity mark succeeds,
   - Then paired audit evidence is recorded or emitted in the same governed operation boundary before the mutation is reported as successful,
   - And the audit evidence records actor, timestamp, tenant, conversation, target reference, policy basis, rationale, outcome, schema version, and safe correlation metadata.
   - And no sensitivity-marked durable event may be committed or externally reported as successful unless the matching audit evidence has been durably accepted or an approved opaque audit handle is available for the same tenant, conversation, target, actor, operation timestamp, schema version, and correlation metadata.
   - And any partial, uncertain, reordered, duplicate, or contradictory audit/domain pairing result remains internal, retry-safe, non-disclosing, and cannot publish, project, export, or externally report a successful sensitivity mark.
   - Given audit recording is unavailable or cannot prove pairing,
   - When the command is handled,
   - Then the sensitivity mutation fails closed with an audit-unavailable outcome and no sensitivity-marked event.
   - And retries after audit-unavailable, uncertain, conflicting, or unsafe-handle outcomes remain idempotent and do not disclose prior audit infrastructure details.

5. Deterministic replay and projected sensitivity state
   - Given sensitivity-marked events exist in the event stream,
   - When aggregate state or read projections rebuild from persisted events,
   - Then replay reconstructs the same sensitivity state by target reference, category, policy basis, actor attribution, rationale, audit linkage, and timestamps,
   - And duplicate, reordered, or replayed events do not create divergent sensitivity state or duplicate audit evidence.
   - And malformed, unsupported-version, unsafe-handle, or unpaired historic sensitivity events cannot upgrade projected trust; rebuilds isolate or mark the affected sensitivity state non-current using sanitized diagnostics.
   - And command decisions are made from authorized server-side checks plus aggregate replay state, never from projection, cache, export, UI, evidence bundle, or read-model state.
   - Given authorized read paths expose sensitivity state,
   - When projections are current, stale, rebuilding, unavailable, or hidden by tenant isolation,
   - Then read models expose only safe category/trust/freshness metadata needed for later redaction, display, citation, export, and command gating,
   - And unauthorized consumers receive safe hidden, restricted, unavailable, or non-disclosing states without protected details.
   - And projection rebuilds derive sensitivity state only from accepted EventStore events; stale, missing, or hidden projection state cannot authorize or block a governance mutation.

6. Safe payload, disclosure, and observability boundaries
   - Given sensitivity commands, events, results, read models, logs, traces, diagnostics, sample JSON, fixtures, and tests are produced,
   - When values are serialized, displayed, logged, traced, copied, exported, or asserted,
   - Then outputs exclude raw content, protected content fragments, Party personal data, provider payloads, raw business references, audit storage details, EventStore substrate names, exception text, unbounded diagnostics, tokens, claims, and cross-tenant facts,
   - And unauthorized, nonexistent, cross-tenant, stale, audit-unavailable, policy-blocked, and unsupported-target outcomes use the same non-disclosing public response shape where required by Story 1.5 and Story 2.1.
   - And docs, fixtures, examples, assertion messages, and test display names use synthetic IDs and sanitized placeholders only; no user-facing UI, localization, accessibility, export, or evidence-bundle behavior is implemented beyond safe command/result text needed for this story.

## Tasks / Subtasks

- [ ] Add sensitivity command and contract integration (AC: 1, 3, 6)
  - [ ] Add or finalize the public mark-sensitive command shape under `src/Hexalith.Conversations.Contracts/Governance/` using Story 2.1 governance metadata, governed target, sensitivity category, policy reference, rationale, audit evidence, and outcome vocabulary.
  - [ ] Reuse existing `TenantId`, `ConversationId`, `MessageId`, `FileId`, `PartyId`, `SchemaVersion`, `ConversationCommandMetadata`, `ConversationEventMetadata`, and `ConversationError` conventions instead of inventing parallel identifiers or result types.
  - [ ] Model the governed target as a closed target type plus existing `ConversationId` and the existing target identifier where applicable; segment targets may carry only a safe opaque segment identifier or approved bounded descriptor, never raw content.
  - [ ] Represent defined content segments as bounded opaque references, ranges, or policy-approved target descriptors; never carry the actual segment text in command, event, projection, validation, or diagnostic payloads.
  - [ ] Add serialization samples for success, denial, audit-unavailable, policy-blocked, unsupported-target, and hidden-state results.

- [ ] Add domain command, aggregate handling, and replay state (AC: 1, 2, 3, 5)
  - [ ] Introduce a domain command such as `MarkConversationContentSensitive` in the domain layer and map it from the public contract at the server/application boundary.
  - [ ] Extend `ConversationAggregate` with a sensitivity handler that validates created/open state, tenant binding, target reference, sensitivity category, policy/rationale presence, schema support, and incompatible duplicate semantics before emitting any event.
  - [ ] Add a domain event such as `ConversationContentMarkedSensitiveDomainEvent` with public `ConversationEventMetadata` plus sensitivity-specific payload.
  - [ ] Extend `ConversationState` with replay-only sensitivity state keyed by target reference and `Apply` methods that deterministically update the mark history without side effects.
  - [ ] Define repeated-mark behavior explicitly: same target/category/policy/rationale metadata is idempotent; materially different category, policy, rationale, schema, or target metadata is a typed sanitized conflict unless an approved superseding-event rule exists in Story 2.1 contracts.
  - [ ] Ensure any idempotency or duplicate fingerprint uses canonical safe values and approved bounded rationale identity, not raw rationale text, policy internals, audit storage coordinates, provider data, or protected content fragments.
  - [ ] Do not implement message redaction, source-event deletion, legal-hold decisions, export workflows, full compliance UI, audit-record governance, or irreversible content removal in this story.

- [ ] Add application-boundary authorization and audit gates (AC: 1, 3, 4, 6)
  - [ ] Ensure tenant access is checked before aggregate load, target lookup, projection read, export/rebuild use, or command dispatch using the Story 1.5 guard/service pattern.
  - [ ] Ensure tenant/governance authorization also precedes EventStore stream resolution, audit lookup result disclosure, idempotency outcome disclosure, and any differentiated missing-target or missing-conversation response.
  - [ ] Add a governance permission/role requirement distinct from ordinary conversation read/write access; keep it narrow and Conversations-owned if the current tenant access model cannot express it.
  - [ ] Add or reuse an audit pairing boundary that can prove audit availability and safe evidence correlation before a success result is returned.
  - [ ] Fail closed when the audit seam is unavailable, returns an unsafe handle, cannot correlate to the sensitivity operation, reports an uncertain/conflicting outcome, cannot durably prove pairing, or would reveal audit infrastructure.
  - [ ] Preserve Story 1.6 idempotency behavior: duplicate compatible requests return stable outcomes, conflicting fingerprints reject without mutation, and unknown/pending outcomes remain retry-safe and non-mutating.

- [ ] Add safe result and error mapping (AC: 3, 4, 6)
  - [ ] Map unauthorized, hidden, cross-tenant, stale tenant projection, unsupported schema, missing rationale, invalid policy reference, invalid target, incompatible duplicate, audit unavailable, idempotency conflict, and aggregate-not-found cases to typed sanitized responses.
  - [ ] Keep internal diagnostics separate from public outcome vocabulary; public responses may include bounded retryability/remediation only when it does not reveal target existence, audit infrastructure, policy internals, upstream facts, exception details, or protected content.
  - [ ] Ensure safe audit handles are opaque Conversations-owned values, not storage paths, stream positions, projection checkpoints, audit sink keys, log IDs, or provider identifiers.
  - [ ] Ensure public responses expose only approved status, category, safe policy reference, safe target reference, correlation/causation where already allowed, retryability/remediation where non-disclosing, and approved opaque audit handles.

- [ ] Add projection/read-model integration for sensitivity state only (AC: 2, 5, 6)
  - [ ] Extend the existing projection accumulator/materializer only as needed to expose authorized sensitivity state, target reference, category, policy basis, audit handle, and trust/freshness metadata.
  - [ ] Keep projected state derived and rebuildable; projections, caches, exports, UI state, and evidence bundles are not authoritative.
  - [ ] During stale/rebuilding/unavailable projection states, expose bounded trust/freshness signals and do not make governance decisions from stale projected state unless an approved ADR explicitly allows it.
  - [ ] Make hidden/restricted read states indistinguishable where required to avoid revealing protected target existence or cross-tenant facts.
  - [ ] Add no mutation-specific storage outside EventStore; any materialized sensitivity state must be rebuildable from accepted sensitivity events and safe to discard.

- [ ] Add focused automated tests (AC: 1-6)
  - [ ] Contract tests cover required metadata, governed target, sensitivity category, rationale, policy reference, schema version, timestamp, actor, tenant, conversation, correlation, causation, safe audit handle, JSON round trips, and forbidden field names.
  - [ ] Aggregate tests cover marking conversation/message/file/participant/segment targets, invalid targets, missing rationale, invalid policy reference, malformed policy attribution, unsupported schema, oversized/unsafe metadata, tenant mismatch, closed/archived state if applicable, compatible repeated marks, incompatible duplicate marks, deterministic replay, and no-event rejection paths.
  - [ ] Server/application tests cover tenant authorization before aggregate load, EventStore stream resolution, target lookup, projection read, audit lookup/disclosure, or idempotency outcome disclosure; governance permission denial; stale/missing tenant projection; audit-unavailable/audit-conflict fail-closed behavior; idempotent duplicate/conflict behavior; and same-shape non-disclosure.
  - [ ] Projection tests cover deterministic rebuild from sensitivity events, target-keyed state for every supported target type, duplicate/reordered event tolerance, stale projection signaling, hidden/restricted read semantics, no authority leakage from projection state, and no cross-tenant sensitivity-state disclosure.
  - [ ] Target matrix tests cover conversation, message, file/attachment, participant, and defined segment targets with at least one success path, invalid/inaccessible path, cross-tenant denial, replay assertion, and projection assertion each.
  - [ ] Privacy/forbidden-surface tests prove public type names, property names, JSON payloads, logs/traces where testable, exception messages, test snapshots, audit-facing errors, `ToString()`, validation messages, assertion output, curated fixtures, docs, and sample JSON do not expose forbidden infrastructure, personal-data, provider, upstream, audit-storage, EventStore, raw diagnostic, claim/token, cross-tenant, or protected-content terms.

- [ ] Update developer-facing docs and samples (AC: 1, 4, 6)
  - [ ] Add XML docs for sensitivity command/event/result contracts explaining that sensitivity marking is append-only governance metadata with mandatory audit evidence.
  - [ ] Document that sensitivity marking does not redact content, delete source events, enforce retention, govern audit records, or implement UI/export workflows; those are owned by later stories unless explicitly promoted by ADR.
  - [ ] Add sample JSON for mark-sensitive success, denied, hidden/restricted, audit-unavailable, policy-blocked, and unsupported-target outcomes using content-safe values only.
  - [ ] Document the safe target model, operation ordering, duplicate/conflict behavior, and audit-unavailable retry behavior without exposing policy internals or audit infrastructure.

## Dev Notes

### Source Foundation

- Epic 2 is the governed retention, redaction, and audit epic. Its purpose is to let authorized users apply retention, sensitivity, redaction, archival, and privileged-action governance with paired audit evidence and fail-closed audit behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Governed Retention, Redaction, and Audit`]
- Story 2.3 covers FR43 and FR47-FR49: mark conversation content as sensitive, require paired audit evidence, fail closed when audit recording is unavailable, and permit non-governance activity during audit degradation only where explicitly safe. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.3: Mark Conversation Content as Sensitive`]
- Story 2.1 defines governance/audit contract vocabulary, safe metadata, outcome states, evidence handles, policy-blocked/audit-unavailable semantics, and forbidden disclosure surfaces. Implementation should consume those contracts rather than reshaping governance vocabulary in Story 2.3. [Source: `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`]
- Story 2.2 establishes the first Epic 2 mutation pattern for retention. Reuse its intended governance command, audit pairing, idempotency, replay, and projection approach for sensitivity where the domain differs only by target/category semantics. [Source: `_bmad-output/implementation-artifacts/2-2-set-conversation-retention-policy-with-rationale.md`]

### Architecture Constraints

- EventStore remains the only durable write-side authority for conversation state. Sensitivity marks must flow through domain commands and persisted domain events, not direct transcript/governance tables, projection writes, export-state writes, or UI-only flags. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Durable events use Conversations language and stable references. They must not store Party personal data, display names, contact channels, provider-owned session authority, raw upstream records, message content, sensitive content, or file binaries. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Governance mutations require paired audit/domain evidence and must fail closed when audit recording is unavailable. Non-governance commands may continue during audit degradation only by explicit ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Governance Security`]
- Public APIs expose domain-first Conversations commands, projections, result contracts, version metadata, typed errors, and freshness state while hiding EventStore mechanics and raw projection/storage internals. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Redaction is append-only and policy-governed by default; irreversible source-event redaction requires a future legal/compliance ADR. Sensitivity marking must not imply redaction, deletion, retention enforcement, or export suppression in this story. [Source: `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`]

### Advanced Elicitation Hardening

- The 2026-05-20 advanced-elicitation pass kept Story 2.3 inside mark-sensitive scope but sharpened success authority: a public success requires a trusted domain event and trusted audit evidence for the same server-side operation. Caller timestamps, projection state, idempotency artifacts, and audit handles are correlation inputs, not authority to report success.
- Rationale and policy attribution are high-risk disclosure surfaces. Store and compare only the Story 2.1-approved representation, and never echo raw rationale, raw policy text, storage coordinates, provider identifiers, Party personal data, protected content fragments, or target lifecycle facts through public errors, idempotency conflicts, test names, sample data, logs, traces, or `ToString()` output.
- Target lifecycle ambiguity is fail-closed. If a target is redacted, inaccessible, unsupported, hidden by tenant/governance rules, stale, or otherwise unsafe to prove after authorization, the command returns a typed sanitized rejection without revealing whether the target exists or why it is unavailable.
- Replay and projection rebuilds must treat malformed, unsupported-version, unsafe-handle, or unpaired historic sensitivity events as untrusted input. They may isolate or mark derived sensitivity state non-current, but they must not upgrade trust, duplicate audit evidence, or become command authority.

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
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionAccumulator.cs`
- Existing tests to extend:
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Tests/Aggregates/`
  - `tests/Hexalith.Conversations.Tests/State/`
  - `tests/Hexalith.Conversations.Server.Tests/TenantAccess/`
  - `tests/Hexalith.Conversations.Server.Tests/Idempotency/`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/`

### Implementation Guardrails

- Validation order matters: schema and required contract fields, tenant access, governance permission, target validation, audit precondition, idempotency/fingerprint handling, aggregate validation, audit pairing, then success response. If the established idempotency executor requires a different internal order, preserve non-disclosure and no-mutation guarantees in tests.
- No operation may disclose target existence before authorization. Treat envelope validation as shape-only until tenant and governance access are proven; aggregate load, target existence checks, projection reads, audit lookup details, idempotency result disclosure, and differentiated errors happen only after the access gates pass.
- Preferred operation order: validate contract envelope shape without lookup disclosure; authorize tenant/governance access; load/replay aggregate; validate target belongs to the conversation; prepare or record mandatory audit evidence; commit the sensitivity domain event with safe audit linkage; project safe derived sensitivity state.
- Sensitivity category and target reference are metadata. They must help later redaction/display/export/citation decisions without becoming raw content, personal data, policy internals, or audit storage identity.
- The target model must be stable and bounded: a closed target kind, the conversation ID, and an existing target ID where applicable. Defined segment targets must be opaque identifier-only references unless Story 2.1 already provides a safer bounded descriptor.
- Rationale is required but sensitive as free text. Store only what Story 2.1 permits; never echo unsafe rationale in `ToString()`, validation errors, logs, traces, assertion messages, public diagnostics, or sample data.
- Target references must be stable and tenant-bound. A content segment target should identify a durable bounded target, not an index into transient UI text, raw prompt text, provider message payload, or exported document fragment.
- Replacement/duplicate semantics must be explicit. Compatible repeated marks can be idempotent; incompatible categories or policy bases must either emit a clearly governed transition or reject with a typed sanitized outcome. Do not silently overwrite prior sensitivity state.
- For this story, changing or removing an existing sensitivity mark is not implicitly in scope. If Story 2.1 contracts do not already define a superseding-event rule, treat same idempotency identity with materially different target, category, policy, rationale, schema, or tenant context as a sanitized conflict without mutation.
- Audit handles are evidence correlation values, not storage identity. They must be opaque, stable enough for adopter citation where allowed, and safe to return in denial/audit-unavailable cases only when policy allows.
- Read models may expose safe sensitivity category/trust metadata to authorized consumers. They must not become the authority for whether a mark exists or whether a later governance command may bypass the event stream.
- Non-governance conversation activity during audit degradation remains an ADR/policy-controlled allowance; sensitivity marking always mutates governance state and must fail closed while audit pairing cannot be proven.

### Scope Boundaries

- In scope:
  - Mark-sensitive command contract integration.
  - Domain command/event handling for sensitivity marks.
  - Audit precondition/pairing seam sufficient to fail closed and prove success pairing.
  - Sensitivity state in aggregate replay and authorized read projections.
  - Contract, aggregate, server/application, projection, privacy, and serialization tests.
- Out of scope:
  - Message redaction or content replacement.
  - Retention enforcement jobs or deletion workflows.
  - Legal-hold decision engine.
  - Audit-record retention/redaction governance.
  - Full compliance investigation UI, exports, and evidence bundles.
  - Irreversible source-event deletion.
  - New authoritative storage outside EventStore.

### Testing Requirements

- Run at minimum:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
  - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj`
- If project references, serialization registration, shared primitives, or projection contracts move, run `dotnet test Hexalith.Conversations.slnx`.
- Tests must prove no mutation when audit is unavailable, tenant access is stale/missing, governance permission fails, idempotency conflicts, target validation fails, or command validation fails.
- Tests must include failure paths and assertion text checks where feasible, because unsafe values often leak through non-JSON surfaces after JSON payloads are cleaned.
- Tests must prove audit pairing thresholds for success, audit unavailable, audit conflict, unsafe handle, retry after uncertain outcome, and deterministic replay from accepted events only.
- Tests must prove idempotency behavior for same command replay, same idempotency key with same payload, same key with different category/policy/rationale/target/schema, and stale tenant context.
- Tests must prove projections cannot authorize, deny, or otherwise decide sensitivity mutations; projection state is only derived read state and can be stale, rebuilding, hidden, unavailable, or discarded without changing command authority.

### Lessons Applied

- L08: Party-mode review and advanced elicitation are separate hardening passes. Story 2.3 now has a completed party-mode trace and a completed advanced-elicitation trace; implementation should treat both as pre-dev clarification, not scope expansion. [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08 - Party Review Vs. Elicitation`]

## References

- `_bmad-output/planning-artifacts/epics.md#Story 2.3: Mark Conversation Content as Sensitive`
- `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`
- `_bmad-output/implementation-artifacts/2-2-set-conversation-retention-policy-with-rationale.md`
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

- Story context created from Epic 2 Story 2.3, Story 2.1 prerequisite contracts, Story 2.2 mutation pattern, architecture governance/data/API constraints, project context, and current code/test patterns.
- Status set to ready-for-dev; `sprint-status.yaml` owns the queue state.

### File List

- `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md`

## Party-Mode Review

- ISO date and time: 2026-05-20T18:01:12Z
- Selected story key: `2-3-mark-conversation-content-as-sensitive`
- Command/skill invocation used: `/bmad-party-mode 2-3-mark-conversation-content-as-sensitive; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - All reviewers agreed the story was directionally aligned with Stories 2.1 and 2.2, but initially needed tighter pre-dev language before implementation around authorization-before-lookup ordering, audit/domain durable pairing, governed target identity, idempotency conflict behavior, projection non-authority, and privacy/test evidence.
  - The main implementation traps were cross-tenant existence disclosure through target lookup or idempotency result disclosure, accidental storage of raw segment/policy/rationale content, committing a sensitivity event without provable audit pairing, and allowing projection state to become command authority.
- Changes applied:
  - Clarified tenant/governance authorization ordering before aggregate load, EventStore stream resolution, target checks, projection reads, audit lookups, idempotency disclosure, or differentiated errors.
  - Clarified safe policy attribution, identifier-only segment targets, public response limits, same-payload idempotency, incompatible duplicate conflict behavior, and no implicit unmark/update scope.
  - Clarified audit/domain pairing as a success gate: no sensitivity-marked durable event or external success without durably accepted matching audit evidence or an approved opaque audit handle.
  - Clarified projection state as replay-derived, non-authoritative, discardable read state that cannot authorize or block governance mutations.
  - Expanded test requirements for target matrix coverage, authorization-before-disclosure, audit-unavailable/audit-conflict fail-closed behavior, idempotency variants, projection rebuild/non-authority, and privacy scans over non-JSON surfaces.
- Findings deferred:
  - Exact sensitivity category vocabulary, policy attribution schema names, segment identity model beyond safe opaque references, sensitivity update/unmark semantics, export/evidence bundle behavior, compliance UI, legal-hold interaction, retention/redaction enforcement, and audit-record governance remain deferred to Story 2.1 contracts, later stories, or ADRs.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- ISO date and time: 2026-05-20T20:03:51Z
- Selected story key: `2-3-mark-conversation-content-as-sensitive`
- Command/skill invocation used: `/bmad-advanced-elicitation 2-3-mark-conversation-content-as-sensitive`
- Batch 1 method names:
  - Red Team vs Blue Team
  - Security Audit Personas
  - Failure Mode Analysis
  - Self-Consistency Validation
  - Critique and Refine
- Reshuffled Batch 2 method names:
  - First Principles Analysis
  - Pre-mortem Analysis
  - Architecture Decision Records
  - Socratic Questioning
  - User Persona Focus Group
- Findings summary:
  - The story was already ready for development after party-mode review, but advanced elicitation found remaining implementability risks around trusted timestamp authority, audit/domain partial failures, unsafe rationale and policy attribution leakage, target lifecycle ambiguity, idempotency fingerprint safety, and projection/replay trust upgrades from malformed historic events.
- Changes applied:
  - Clarified that durable event and audit timestamps are server-trusted operation metadata and that caller timestamps are request context unless Story 2.1 explicitly defines otherwise.
  - Clarified that partial, uncertain, reordered, duplicate, or contradictory audit/domain pairing cannot publish, project, export, or externally report success.
  - Added replay/rebuild guardrails for malformed, unsupported-version, unsafe-handle, or unpaired historic sensitivity events.
  - Added safe idempotency fingerprint guidance that excludes raw rationale text, policy internals, audit storage coordinates, provider data, and protected content fragments.
  - Added advanced-elicitation hardening notes for success authority, disclosure surfaces, target lifecycle fail-closed behavior, and replay/projection non-authority.
  - Updated L08 wording to show both pre-dev hardening passes are complete.
- Findings deferred:
  - Exact sensitivity category vocabulary, policy attribution schema names, trusted caller timestamp rules, target lifecycle taxonomy, superseding/unmark semantics, export/evidence bundle behavior, legal-hold interaction, and audit-record governance remain deferred to Story 2.1 contracts, later stories, or ADRs.
- Final recommendation: ready-for-dev
