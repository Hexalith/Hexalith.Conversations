# Story 2.4: Redact Message Content with Audit Attribution

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an authorized governance operator,
I want to record redaction intent as an audited domain event,
so that protected content can be removed from governed surfaces while auditability remains intact.

## Acceptance Criteria

1. Authorized redaction command
   - Given an authorized governance operator submits a redact-message command with tenant scope, conversation identity, governed message or content target reference, redaction category, policy reference, rationale, actor attribution, schema version, operation timestamp, idempotency key, and correlation metadata,
   - When contract shape, tenant access, governance role, target, policy, schema version, idempotency, and audit-precondition checks pass,
   - Then the command is accepted through the Conversations application boundary and dispatched to the Conversation aggregate,
   - And no API, admin, worker, MCP/tool, export, rebuild, verification, or command-handler path can redact message content without the same tenant and governance checks.
   - And tenant access and governance permission are validated before aggregate load, EventStore stream resolution, target existence checks, projection reads, audit lookups, idempotency result disclosure, or differentiated errors that could reveal conversation or target existence.

2. Append-only redaction event is content-safe
   - Given a redaction command succeeds for a governed message or approved content segment target,
   - When the aggregate emits the redaction domain event,
   - Then the event records tenant ID, conversation ID, governed target reference, redaction category, policy reference, rationale, actor Party ID, event timestamp, schema version, correlation ID, causation ID when supplied, safe audit evidence linkage, and caller idempotency key when supplied,
   - And the event does not store original redacted content, message text, prompt fragments, Party personal data, provider payloads, file binaries, upstream detail objects, audit storage locations, EventStore stream coordinates, or unbounded diagnostics.
   - And the event is append-only redaction intent or an approved tombstone-style domain fact only; irreversible source-event rewriting, deletion, hard purge, legal-hold processing, projection replacement, UI masking, export behavior, log/trace scrubbing, and evidence-bundle behavior remain outside this story unless an approved ADR already owns the behavior.

3. Typed fail-closed and idempotent outcomes
   - Given the target is missing, already redacted, cross-tenant, unauthorized, unsupported-version, blocked by policy, hidden by stale tenant state, invalid for redaction, inaccessible, or tied to an idempotency conflict,
   - When the command is handled,
   - Then the system returns a typed documented rejection or a compatible idempotent no-op outcome according to policy,
   - And no misleading successful redaction event, audit success evidence, projection mutation, publication-ready event, export marker, cache update, or external side effect is emitted.
   - And unauthorized, nonexistent, cross-tenant, stale, audit-unavailable, policy-blocked, unsupported-target, already-redacted, invalid-target, inaccessible-conversation, and idempotency-conflict outcomes remain non-disclosing until tenant/governance authorization has succeeded.
   - And compatible duplicate requests return the stable sanitized result without appending duplicate redaction or audit evidence, while materially different category, policy, rationale, target, tenant context, schema, or redaction metadata with the same idempotency identity rejects as a typed conflict without mutation.

4. Audit pairing is mandatory
   - Given redaction is a governance mutation,
   - When a redaction succeeds,
   - Then paired audit evidence is recorded or emitted in the same governed operation boundary before the mutation is reported as successful,
   - And the audit evidence records actor, timestamp, tenant, conversation, target reference, policy basis, rationale, redaction category, operation kind, schema version, outcome, and safe correlation metadata.
   - And no redaction durable event may be committed or externally reported as successful unless the matching audit evidence has been durably accepted or an approved opaque audit handle is available for the same tenant, conversation, target, actor, operation timestamp, schema version, redaction category, and correlation metadata.
   - Given audit recording is unavailable, unsafe, uncertain, conflicting, blocked, or cannot prove pairing,
   - When the command is handled,
   - Then the redaction mutation fails closed with an audit-unavailable or policy-blocked outcome and no redaction event.
   - And retries after audit-unavailable, uncertain, conflicting, or unsafe-handle outcomes remain idempotent and do not disclose audit infrastructure details.

5. Deterministic replay state without projection authority
   - Given redaction events exist in the EventStore stream,
   - When aggregate state rebuilds from persisted events,
   - Then replay reconstructs redaction intent state by target reference, category, policy basis, actor attribution, rationale, audit linkage, and timestamps,
   - And duplicate, reordered, or replayed events do not create divergent redaction state or duplicate audit evidence.
   - And malformed, unsupported-version, unsafe-handle, or unpaired historic redaction events cannot upgrade projected trust; rebuilds must isolate or mark affected redaction state non-current using sanitized diagnostics.
   - And command decisions are made from authorized server-side checks plus aggregate replay state, never from projection, cache, export, UI, evidence bundle, or read-model state.

6. Local closure evidence
   - Given redaction command tests run,
   - When authorized redactions, duplicate commands, invalid targets, unsupported versions, stale tenant projection, cross-tenant denial, governance permission denial, audit unavailable, audit uncertain, audit unsafe, policy blocked, existing sensitivity marks, already-redacted state, and idempotency conflict scenarios are exercised,
   - Then tests prove command/event behavior, audit pairing, typed rejection semantics, deterministic replay, no duplicate mutation evidence, authorization-before-disclosure ordering, and absence of original redacted content from domain events, public contracts, samples, diagnostics, and test output.
   - And Story 2.4 produces minimum local evidence for story closure; release-gate redaction replay evidence remains carried forward into Story 5.7 for manifest aggregation and signing.

## Tasks / Subtasks

- [x] Add redaction command and result contracts (AC: 1, 3, 6)
  - [x] Add `RedactMessageContentCommand` under `src/Hexalith.Conversations.Contracts/Governance/` using existing `ConversationCommandMetadata`, `ConversationId`, `GovernanceTarget`, `RedactionCategory`, `GovernanceOperationMetadata`, policy reference, rationale, and UTC operation timestamp conventions.
  - [x] Add `ConversationRedactionResult` or equivalent sanitized outcome contract using `GovernanceOutcome`, `GovernanceRemediation`, `GovernanceAuditEvidenceReference`, and `ConversationError`.
  - [x] Add `ConversationCommandType.RedactMessageContentCommand` and include it in known command-type parsing and contract samples.
  - [x] Reuse `GovernanceTarget.ToTargetKey()` for redaction target identity; do not create a parallel target-key implementation.
  - [x] Restrict valid Story 2.4 targets to `Message` and approved opaque `ContentSegment` targets unless the existing governance contracts already define a safe broader target.
  - [x] Ensure command/result `ToString()`, XML docs, examples, and validation messages omit rationale text where unsafe, original content, provider payloads, audit storage details, EventStore internals, tenant/customer names, claims, tokens, and Party personal data.
  - [x] Add JSON round-trip samples for success, denied/hidden, audit-unavailable, policy-blocked, unsupported-target, already-redacted/no-op, and idempotency-conflict outcomes.

- [x] Add public and domain redaction event contracts (AC: 2, 5, 6)
  - [x] Add `ConversationEventType.MessageContentRedacted` or the local naming equivalent selected for Conversations vocabulary.
  - [x] Add public event contract under `src/Hexalith.Conversations.Contracts/Events/` that carries metadata, target, redaction category, policy reference, rationale, and safe audit evidence only.
  - [x] Add domain event under `src/Hexalith.Conversations/Events/` that implements `IEventPayload` and follows the `ConversationContentMarkedSensitiveDomainEvent` pattern.
  - [x] Add publication mapping in `ConversationPublicationMapper` and metadata/event-type validation in `ConversationPublicationMetadata` if required.
  - [x] Do not store original message text, before/after payloads, text excerpts, selector text, offsets into provider payloads, file binaries, or provider-owned coordinates in either public or domain events.

- [x] Add domain command, aggregate handling, and replay state (AC: 1, 2, 3, 5)
  - [x] Add `RedactMessageContent` in `src/Hexalith.Conversations/Commands/` with public command, safe audit evidence, and event ID.
  - [x] Extend `ConversationAggregate.Handle(...)` with redaction handling that validates created/open state, tenant binding, supported target shape, redaction category, policy/rationale presence, schema support, already-redacted state, timestamp ordering, and compatible duplicate semantics before emitting any event.
  - [x] Add `ConversationRedactionState` or equivalent replay-only state keyed by `GovernanceTarget.ToTargetKey()`.
  - [x] Extend `ConversationState` with redaction state lookup and `Apply` behavior for the redaction domain event.
  - [x] Ensure compatible duplicate redactions are no-op/idempotent and incompatible redactions are typed sanitized conflicts unless an approved superseding-event rule exists.
  - [x] Keep aggregate logic deterministic and side-effect free. The aggregate must not call Tenants, Parties, audit services, projections, clocks, logging, UI, export, storage, or policy catalogs.

- [x] Add validation and boundary mapping (AC: 1, 3, 4, 6)
  - [x] Add `RedactMessageContentBoundary` and `RedactMessageContentValidation` following the split shape/semantic/audit-evidence pattern from sensitivity and retention.
  - [x] Validate shape without doing lookups that can reveal target existence.
  - [x] Reject unsupported schema, missing metadata, missing target, unsupported target kind, malformed segment reference, missing category, missing policy reference, missing rationale, unsafe timestamps, missing event ID, tenant mismatch, already-redacted incompatible state, and unsafe audit evidence with typed sanitized rejection events.
  - [x] Preserve the existing non-disclosure ordering if `IdempotentConversationCommandExecutor` constrains handler order: tests must prove no lookup, load, audit detail, or idempotency result leaks before tenant/governance authorization succeeds.

- [x] Add application-boundary authorization, audit, and idempotency gates (AC: 1, 3, 4, 6)
  - [x] Add `RedactMessageContentCommandHandler` under `src/Hexalith.Conversations.Server/CommandHandlers/`.
  - [x] Reuse `ConversationTenantAccessGuard.RunAsync(..., ConversationTenantAccessRequirement.Governance, ...)` before aggregate load, EventStore stream resolution, target checks, projection reads, audit lookups, or idempotency outcome disclosure.
  - [x] Extend `IConversationGovernanceAuditService` with a redaction-specific audit method, or a safe generalized governed-mutation method if that already fits the local pattern.
  - [x] Map `ConversationGovernanceAuditStatus.Succeeded`, `AuditUnavailable`, `Uncertain`, `UnsafeEvidence`, and `PolicyBlocked` to sanitized redaction outcomes and rejection events without mutation.
  - [x] Preserve Story 1.6 idempotency behavior: compatible duplicate commands return stable outcomes, conflicting fingerprints reject without mutation, and unknown/pending outcomes remain retry-safe and non-mutating.
  - [x] Add `ConversationCommandFingerprint.CreateForRedaction(...)` using canonical safe values only: tenant, conversation, target key, redaction category, safe policy reference, approved rationale representation, schema, and operation timestamp. Do not include raw content, text excerpts, provider data, audit storage coordinates, or diagnostics.

- [x] Add minimal derived redaction state only where needed (AC: 2, 5, 6)
  - [x] This story may add replay-derived aggregate redaction state and minimal publication/contract samples needed for command closure.
  - [x] Do not implement projection/read-model masking, search materialization updates, temporal reconstruction, UI display masking, clipboard/citation behavior, exports, caches, log/trace redaction, evidence bundles, or derived index propagation. Those are Story 2.4.1, 2.4.2, and 2.4.3 responsibilities.
  - [x] If a tiny projection placeholder is needed to keep existing detail projection contracts compiling, mark it derived, discardable, and non-authoritative, and do not expose original content.

- [x] Add focused automated tests (AC: 1-6)
  - [x] Contract tests cover required metadata, governed target, redaction category, rationale, policy reference, schema version, timestamp, actor, tenant, conversation, correlation, causation, safe audit handle, JSON round trips, command/event type parsing, and forbidden field names.
  - [x] Aggregate tests cover message target success, content-segment target success if supported, unsupported target kinds, invalid targets, missing rationale, invalid policy reference, unsupported schema, oversized/unsafe metadata, tenant mismatch, closed/archived state where applicable, compatible duplicate redaction, incompatible duplicate redaction, deterministic replay, and no-event rejection paths.
  - [x] Server/application tests cover tenant authorization before aggregate load, EventStore stream resolution, target lookup, projection read, audit lookup/disclosure, or idempotency outcome disclosure; governance permission denial; stale/missing tenant projection; audit-unavailable/audit-conflict/unsafe-evidence fail-closed behavior; idempotent duplicate/conflict behavior; and same-shape non-disclosure.
  - [x] Publication tests cover public event mapping, rejected persistence outcomes, tenant mismatch, event type mismatch, unsupported schema, and absence of storage internals from diagnostics.
  - [x] Privacy/forbidden-surface tests prove public type names, property names, JSON payloads, logs/traces where testable, exception messages, test snapshots, audit-facing errors, `ToString()`, validation messages, assertion output, curated fixtures, docs, and sample JSON do not expose original content, provider payloads, Party personal data, audit storage, EventStore internals, raw diagnostics, claims/tokens, or cross-tenant facts.

- [x] Update developer-facing docs and local evidence (AC: 1, 2, 4, 6)
  - [x] Add XML docs for redaction command/event/result contracts explaining that redaction is append-only governed intent with mandatory audit evidence.
  - [x] Document that this story does not implement projection masking, source-event deletion, legal hold, retention enforcement, audit-record governance, UI/export behavior, or operational log/trace scrubbing.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` or the current local evidence location with the Story 2.4 command, domain, server, publication, and privacy test run results.

## Dev Notes

### Source Foundation

- Epic 2 is the governed retention, redaction, and audit epic. Its goal is policy-governed mutation with paired audit evidence and fail-closed audit behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Governed Retention, Redaction, and Audit`]
- Story 2.4 covers FR44-FR47 and FR51. It records redaction intent as audited domain evidence, not full projection masking or release-gate redaction replay. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.4: Redact Message Content with Audit Attribution`]
- Story 2.4 scope is command, domain event, typed rejection, and paired audit behavior only. Projection/read-model behavior is Story 2.4.1, client-visible disclosure safety is Story 2.4.2, and operational/export/log/trace safety is Story 2.4.3. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.4: Redact Message Content with Audit Attribution`]
- Story 2.1 already defines the governance/audit vocabulary, safe evidence handles, policy-blocked/audit-unavailable outcomes, governance target model, redaction category vocabulary, and forbidden disclosure surfaces. Reuse those contracts. [Source: `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`]
- Story 2.2 established the first governed mutation pattern for retention policy set/replace. Story 2.3 reused that pattern for target-based sensitivity marking and is the closest implementation template for this story. [Source: `_bmad-output/implementation-artifacts/2-2-set-conversation-retention-policy-with-rationale.md`; `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md`]

### Architecture Constraints

- EventStore remains the only durable write-side authority for conversation state. Redaction must be a domain command and persisted domain event, not a transcript table update, projection-only flag, cache write, export-state write, UI-only mask, or direct storage rewrite. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Redaction is append-only, policy-governed, and auditable by default. Original events remain governed by storage policy unless a legal/compliance ADR explicitly authorizes irreversible source-event redaction. [Source: `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`]
- Durable events use stable references and must not store Party personal data, display names, contact channels, provider-owned session authority, raw upstream records, message content, sensitive content, or file binaries. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Tenant authorization fails closed before aggregate load, command dispatch, projection read, admin action, MCP/tool operation, export, verification detail access, rebuild, or background work that can infer conversation data. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- Governance mutations require paired audit/domain evidence and fail closed when audit recording is unavailable. Non-governance commands may continue during audit degradation only by explicit ADR; redaction is governance mutation and must fail closed. [Source: `_bmad-output/planning-artifacts/architecture.md#Governance Security`]
- Public APIs expose Conversations commands, projections, result contracts, version metadata, typed errors, and freshness state. Do not expose EventStore envelopes, stream names, snapshot mechanics, stream positions, projection topology, or raw persistence diagnostics. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Redacted content must not leak through projections, UI, logs, traces, accessibility tree, clipboard, exports, caches, evidence artifacts, or derived indexes. Story 2.4 must not implement all of those surfaces, but it must avoid introducing original content into the event/result surfaces it does own. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`]

### Existing Code Patterns To Reuse

- Public governance contracts are sealed records with constructor validation and XML docs under `src/Hexalith.Conversations.Contracts/Governance/`.
- Existing governance vocabulary already includes:
  - `GovernanceOperationKind.RedactMessageContent`
  - `RedactionCategory.DisplayMask`
  - `RedactionCategory.ContentSuppression`
  - `RedactionCategory.ReferenceWithheld`
  - `GovernanceOutcome.Succeeded`, `Denied`, `AuditUnavailableFailed`, and `PolicyBlocked`
  - `GovernanceRemediation.RetryWhenAuditAvailable`, `RequestAuthorization`, and policy remediation values
  [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`]
- Existing target identity should be reused through `GovernanceTarget.ToTargetKey()`, which was introduced after Story 2.3 review to prevent target-key drift between aggregate and projection paths. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`; `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md#Senior Developer Review (AI)`]
- Existing metadata contracts to reuse:
  - `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`
  - `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceOperationMetadata.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`
  - `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationError*.cs`
- Existing domain patterns to extend:
  - `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs` for static `Handle(Command, State?) -> DomainResult` handlers.
  - `src/Hexalith.Conversations/State/ConversationState.cs` for deterministic replay and `Apply` methods.
  - `src/Hexalith.Conversations/Validation/*` for split boundary validation returning typed rejection events before success.
  - `src/Hexalith.Conversations/Idempotency/ConversationCommandFingerprint.cs` for canonical safe fingerprinting.
- Existing server patterns to extend:
  - `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`
  - `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs`
  - `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
  - `src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs`
- Existing tests to extend:
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/SensitivityContractTest.cs` as a pattern for a redaction contract test
  - `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateSensitivityTest.cs` as a pattern for aggregate redaction tests
  - `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs` as a pattern for handler/audit/tenant ordering tests
  - `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs`

### Files Likely To Update

- `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
  - Current state: redaction operation and category vocabulary exists, but no public redaction command/result contract is present.
  - This story changes: only extend if command/result parsing or additional redaction outcome vocabulary is truly required.
  - Preserve: bounded known-value parsing and content-safe token validation.
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`
  - Current state: owns the deterministic target key used across sensitivity replay/projection.
  - This story changes: likely no change unless redaction needs target shape helpers.
  - Preserve: single source of truth for target keys; do not duplicate switch logic in redaction code.
- `src/Hexalith.Conversations.Contracts/Results/ConversationCommandType.cs`
  - Current state: knows create, append, participant, retention, and sensitivity command types.
  - This story changes: add the redaction command type and parsing sample coverage.
  - Preserve: canonical PascalCase values and case-sensitive parsing.
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventType.cs`
  - Current state: knows lifecycle, retention, and sensitivity event types.
  - This story changes: add the redaction event type and parsing sample coverage.
  - Preserve: public event vocabulary only; avoid storage terminology.
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
  - Current state: routes create, participant, retention, and sensitivity domain commands; sensitivity returns `DomainResult.NoOp()` for compatible duplicate target marks.
  - This story changes: add redaction command handling with the same fail-closed, replay-driven pattern.
  - Preserve: deterministic aggregate behavior and no external service calls.
- `src/Hexalith.Conversations/State/ConversationState.cs`
  - Current state: stores messages, participants, retention policy, and sensitivity marks with deterministic apply methods.
  - This story changes: add replay-only redaction state and lookup, without removing or rewriting message content.
  - Preserve: EventStore replay authority and duplicate-event idempotence.
- `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs`
  - Current state: closest server implementation pattern for tenant/governance guard, audit status mapping, idempotency, and aggregate dispatch.
  - This story changes: create a redaction handler from the same orchestration pattern.
  - Preserve: authorization before lookup/disclosure and sanitized rejection mapping.
- `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
  - Current state: contains retention and sensitivity audit methods returning `ConversationGovernanceAuditResult`.
  - This story changes: add redaction audit method or a local generalized operation seam.
  - Preserve: internal audit status vocabulary and safe evidence reference semantics.
- `src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs`
  - Current state: maps retention and sensitivity domain events to public event contracts and rejects persistence failures with safe diagnostics.
  - This story changes: map redaction domain event to public event contract.
  - Preserve: no EventStore internals or payload content in diagnostics.
- `tests/Hexalith.Conversations.*`
  - Current state: Story 2.3 added focused contract, aggregate, server, projection, publication, and privacy tests.
  - This story changes: add parallel redaction tests without broad projection/UI/export scope.
  - Preserve: xUnit v3, Shouldly, NSubstitute patterns and central package management.

### Previous Story Intelligence

- Story 2.3 completed the target-based governance mutation pattern and is the implementation template for Story 2.4. It added `MarkConversationContentSensitiveCommand`, `ConversationContentMarkedSensitive`, `ConversationSensitivityMarkResult`, target-keyed replay state, handler audit gates, publication mapping, projection materialization, and tests. [Source: `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md#Completion Notes List`]
- Senior review for Story 2.3 found duplicated target-key logic in three places and fixed it by centralizing on `GovernanceTarget.ToTargetKey()`. Redaction must reuse that helper from the start. [Source: `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md#Senior Developer Review (AI)`]
- Story 2.3 proved the accepted operation order: schema-shape validation, fail-closed tenant binding, governance tenant access guard, semantic-shape validation, idempotency execution, audit pairing, and aggregate dispatch. Redaction should follow that unless tests prove the same non-disclosure guarantees under a different local ordering. [Source: `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md#Senior Developer Review (AI)`]
- Story 2.3 deliberately scoped out message redaction, source-event deletion, legal-hold decisions, export workflows, full compliance UI, audit-record governance, and irreversible content removal. Story 2.4 brings in only redaction command/event/audit intent and still leaves projection/UI/operational propagation to the split stories. [Source: `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md#Scope Boundaries`]
- Story 2.3 advanced elicitation emphasized timestamp authority, audit/domain partial failures, unsafe rationale/policy leakage, target lifecycle ambiguity, idempotency fingerprint safety, and replay/projection trust upgrades. Apply the same hardening to redaction. [Source: `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md#Advanced Elicitation`]

### Git Intelligence

- Recent commits show a stable Epic 2 implementation sequence:
  - `8ec43fb feat(story-2.1): Define Governance Policy and Audit Contracts`
  - `229e1fa feat(story-2.2): Set Conversation Retention Policy with Rationale`
  - `5204e66 feat(story-2.3): Mark Conversation Content as Sensitive`
- Story 2.2 and 2.3 both touched contracts, domain commands/events, aggregate/state/validation, idempotency, server command handlers, governance audit service, projection/publication mapping, contract samples, aggregate tests, server tests, and sprint status. Story 2.4 should stay in the same files/folders and avoid new infrastructure unless a real redaction-specific invariant requires it.
- Story 2.3's implementation added tests before and around contracts, aggregate behavior, authorization/audit gates, publication, projection, and privacy. Keep that test spread, but do not expand into Story 2.4.1/2.4.2/2.4.3 surfaces.

### Latest Technical Information

- The repository pins `.NET SDK 10.0.300` and targets `net10.0`; use the local SDK and target framework rather than downgrading or changing framework monikers. [Source: `global.json`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- NuGet Central Package Management is already enabled through `Directory.Packages.props`. New package references must omit inline `Version` attributes and add package versions centrally only when a new dependency is unavoidable. [Source: `Directory.Packages.props`; Microsoft Learn NuGet Central Package Management: https://learn.microsoft.com/nuget/consume-packages/central-package-management]
- .NET test execution remains through `dotnet test`, and Microsoft Learn documents xUnit.net as a supported .NET testing framework. Keep the local xUnit v3/Shouldly/NSubstitute style and avoid introducing MSTest/NUnit or new assertion libraries. [Source: Microsoft Learn Testing in .NET: https://learn.microsoft.com/dotnet/core/testing/]
- No dependency upgrade is required for Story 2.4. The work is contract/domain/server/test expansion using existing packages and local Hexalith modules.

### Implementation Guardrails

- Redaction command validation order matters. Validate schema shape first without disclosing existence; authorize tenant/governance; then load aggregate state, validate target eligibility, evaluate idempotency, record/prove audit evidence, dispatch the aggregate, and return sanitized success or rejection.
- If the existing idempotency executor requires a slightly different internal order, preserve these invariants in tests: no aggregate load, target lookup, projection read, audit lookup detail, or idempotency result disclosure before tenant/governance authorization succeeds.
- Redaction target identity must be stable and bounded. `Message` targets use existing `MessageId`; `ContentSegment` targets may use only an opaque safe segment reference. Do not persist text offsets, selected text, prompt fragments, provider payload coordinates, exported document fragments, UI selection text, or original message values.
- Redaction category is policy metadata, not content. Keep it bounded to existing `RedactionCategory` vocabulary unless Story 2.1 contracts already define a safe extension rule.
- Rationale is required but sensitive. Store only Story 2.1-approved safe text; never echo unsafe rationale in `ToString()`, validation errors, logs, traces, assertion messages, public diagnostics, or sample data.
- Audit handles are evidence correlation values, not storage identity. They must be opaque, safe to cite where policy allows, and never reveal audit sink identity, log IDs, storage paths, stream coordinates, provider IDs, or projection checkpoints.
- Redaction success requires audit/domain pairing. No durable redaction event may be committed or externally reported as successful when audit is unavailable, uncertain, unsafe, policy-blocked, mismatched, or cannot prove pairing.
- Already-redacted semantics must be explicit. Compatible duplicate redaction can be idempotent/no-op. A materially different category, policy, rationale, schema, target, or tenant context for the same idempotency identity must reject as a sanitized conflict unless an approved superseding-event rule exists.
- Do not implement projection masking, UI placeholders, clipboard/citation behavior, operational log/trace scanning, exports, caches, evidence bundles, derived indexes, legal-hold processing, source-event rewriting, or irreversible hard delete in Story 2.4.

### Testing Requirements

- Run at minimum:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
  - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj`
- If project references, serialization registration, shared primitives, event/command type vocabularies, publication mapping, or projection contracts move, run `dotnet test Hexalith.Conversations.slnx`.
- Tests must prove no mutation when audit is unavailable, tenant access is stale/missing, governance permission fails, idempotency conflicts, target validation fails, command validation fails, target is already redacted incompatibly, or schema version is unsupported.
- Tests must prove absence of original redacted content in command/result/event JSON, domain events, publication mapping, logs/traces where testable, `ToString()`, XML examples, curated samples, validation messages, and assertion output.
- Tests must prove authorization-before-load/read/disclosure ordering and same-shape non-disclosure for unauthorized, nonexistent, cross-tenant, stale, hidden, audit-unavailable, policy-blocked, unsupported-target, and already-redacted cases.
- Tests must prove deterministic replay from accepted redaction events only; projection, cache, export, UI, and evidence-bundle state cannot authorize or block redaction commands.

### Scope Boundaries

- In scope:
  - Redact-message command contract integration.
  - Redaction domain command/event handling.
  - Audit precondition/pairing seam sufficient to fail closed and prove success pairing.
  - Replay-only redaction intent state in aggregate state.
  - Contract, aggregate, server/application, publication, privacy, idempotency, and serialization tests.
- Out of scope:
  - Projection/read-model masking and point-in-time reconstruction. Story 2.4.1 owns it.
  - UI, accessibility tree, clipboard, citation, screenshot, responsive duplicate, and browser-surface safety. Story 2.4.2 owns it.
  - Operational/export/log/trace/error/diagnostic/cache/derived-index redaction verification. Story 2.4.3 owns it.
  - Retention enforcement jobs or deletion workflows.
  - Legal-hold decision engine or precedence.
  - Audit-record retention/redaction governance.
  - Full compliance investigation UI, exports, and evidence bundles.
  - Irreversible source-event deletion or hard purge.
  - New authoritative storage outside EventStore.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 2.4: Redact Message Content with Audit Attribution`
- `_bmad-output/planning-artifacts/epics.md#Story 2.4.1: Apply Redaction to Projections and Read Models`
- `_bmad-output/planning-artifacts/epics.md#Story 2.4.2: Verify UI, Accessibility, Clipboard, and Citation Redaction Safety`
- `_bmad-output/planning-artifacts/epics.md#Story 2.4.3: Verify Operational, Export, Log, Trace, and Error Redaction Safety`
- `_bmad-output/planning-artifacts/architecture.md#Data Architecture`
- `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`
- `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Architecture Verification Strategy`
- `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`
- `_bmad-output/project-context.md#Critical Implementation Rules`
- `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`
- `_bmad-output/implementation-artifacts/2-2-set-conversation-retention-policy-with-rationale.md`
- `_bmad-output/implementation-artifacts/2-3-mark-conversation-content-as-sensitive.md`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs`
- `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
- `src/Hexalith.Conversations/State/ConversationState.cs`
- Microsoft Learn NuGet Central Package Management: https://learn.microsoft.com/nuget/consume-packages/central-package-management
- Microsoft Learn Testing in .NET: https://learn.microsoft.com/dotnet/core/testing/

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` - 155 passed.
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj` - 132 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` - 237 passed.
- `dotnet test Hexalith.Conversations.slnx` - 533 passed.
- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-build --no-restore` - 155 passed.
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-build --no-restore` - 132 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-build --no-restore` - 237 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-restore -p:DisableMsCoverageReferencedPathMaps=true` - blocked before test execution because the sandbox denied writing the generated Coverlet source-root mapping file under `tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0`.
- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` - blocked before test execution because the sandbox denied writing generated coverage mapping files under `tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0`.
- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore -p:OutputPath=...` - blocked before test execution because the sandbox denied creating the alternate output directory.

### Completion Notes List

- Implemented append-only redaction command, result, public event, domain command/event, aggregate handling, replay-only state, validation boundary, audit gate, idempotency fingerprinting, and publication mapping.
- Redaction target support is intentionally limited to `Message` and opaque `ContentSegment` targets and reuses `GovernanceTarget.ToTargetKey()` for deterministic identity.
- Redaction success requires paired safe audit evidence; audit unavailable, uncertain, unsafe, policy-blocked, unauthorized, tenant-mismatched, invalid-target, and idempotency-conflict paths reject without redaction mutation events.
- No projection masking, read-model redaction, UI/export behavior, legal hold processing, source-event deletion, audit-record governance, or operational log/trace scrubbing was implemented in this story.
- Updated local evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- QA automation follow-up added redaction server-boundary tests for unsupported schema rejection before tenant/idempotency/load/audit disclosure, stale state-load coarsening before audit, and completed duplicate replay without state load or duplicate audit evidence.
- Code review follow-up moved replay/state validation before audit recording, prevents compatible duplicate redactions from creating duplicate audit evidence, and rejects mismatched audit evidence before mutation dispatch.

### File List

- `_bmad-output/implementation-artifacts/2-4-redact-message-content-with-audit-attribution.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventType.cs`
- `src/Hexalith.Conversations.Contracts/Events/MessageContentRedacted.cs`
- `src/Hexalith.Conversations.Contracts/Governance/ConversationRedactionResult.cs`
- `src/Hexalith.Conversations.Contracts/Governance/RedactMessageContentCommand.cs`
- `src/Hexalith.Conversations.Contracts/Results/ConversationCommandType.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs`
- `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
- `src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs`
- `src/Hexalith.Conversations.Server/Publication/ConversationPublicationMetadata.cs`
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
- `src/Hexalith.Conversations/Commands/RedactMessageContent.cs`
- `src/Hexalith.Conversations/Events/MessageContentRedactedDomainEvent.cs`
- `src/Hexalith.Conversations/Idempotency/ConversationCommandFingerprint.cs`
- `src/Hexalith.Conversations/State/ConversationRedactionState.cs`
- `src/Hexalith.Conversations/State/ConversationState.cs`
- `src/Hexalith.Conversations/Validation/RedactMessageContentBoundary.cs`
- `src/Hexalith.Conversations/Validation/RedactMessageContentValidation.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/RedactionContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Publication/PublicationSamples.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/SetConversationRetentionPolicyCommandHandlerTest.cs`
- `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs`
- `tests/Hexalith.Conversations.Tests/Idempotency/ConversationCommandFingerprintTest.cs`

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Critical source documents loaded: sprint status, epics, PRD, architecture, UX specifications, project context, Story 2.1, Story 2.2, Story 2.3, recent git history, and likely update files.
- Validation result: ready-for-dev. The story explicitly covers scope, ACs, file locations, current code patterns, previous-story learnings, audit/tenant/idempotency guardrails, out-of-scope boundaries, latest local package guidance, and required tests.

## Change Log

- 2026-05-22: Implemented Story 2.4 redaction command/event/audit/idempotency/publication path with focused contract, aggregate, server, publication, and regression tests.
- 2026-05-22: Created Story 2.4 context from Epic 2 requirements, architecture constraints, Story 2.1 governance contracts, Story 2.2 retention mutation pattern, Story 2.3 sensitivity mutation implementation, project context, recent git history, and current source/test patterns.
- 2026-05-22: Added QA automation follow-up tests for Story 2.4 server-boundary disclosure ordering, stale state-load handling, and duplicate replay behavior.
- 2026-05-22: Code review fixed audit-before-validation side effects and audit-evidence pairing validation; added regression coverage for invalid targets, compatible duplicate no-op, and mismatched audit evidence.

## Senior Developer Review (AI)

### Reviewer

GPT-5 Codex

### Findings

- Fixed CRITICAL: `RedactMessageContentCommandHandler` recorded audit evidence before state/target/conflict validation, so invalid targets or compatible duplicate redactions could create audit side effects despite no mutation. The handler now validates replay state before audit and returns no-op for compatible existing redaction before calling the audit service.
- Fixed CRITICAL: successful audit evidence was accepted without verifying that the returned policy reference and timestamp matched the redaction command. The domain validation now rejects mismatched evidence with `audit_pairing_mismatch` before mutation dispatch.

### Validation

- Source-level sanity checks passed for changed redaction validation, boundary, handler, aggregate tests, and server tests.
- Full compile/test execution remains blocked in this sandbox by denied writes to generated test output/coverage mapping files. Existing compiled no-build test probes passed earlier, but they do not compile the review follow-up changes.

### Outcome

Approved after auto-fixes, with environment-limited validation noted above.
