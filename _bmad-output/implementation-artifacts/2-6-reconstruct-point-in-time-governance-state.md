# Story 2.6: Reconstruct Point-in-Time Governance State

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator,
I want to reconstruct conversation and governance state as it existed at a prior point in time,
so that audits and investigations can rely on stable historical evidence.

## Acceptance Criteria

1. Authorized temporal reconstruction returns the governed state at the requested anchor
   - Given a tenant-scoped conversation has message, participant, retention, sensitivity, redaction, archival, and audit events,
   - When an authorized point-in-time reconstruction is requested for a timestamp, event position, projection version, or contract-defined temporal cursor,
   - Then the system reconstructs message state and governance state as of that anchor,
   - And the response identifies the authoritative temporal anchor used through a Conversations-owned safe temporal anchor, not raw EventStore stream topology.

2. Current redaction and retention policy still governs historical views
   - Given redaction or retention changes occurred after the requested point,
   - When historical state is reconstructed,
   - Then the output follows the active redaction, retention, and disclosure policy for historical views,
   - And it does not reveal content that is redacted, retained only as audit evidence, unavailable, or unauthorized under current authorization and policy.

3. Invalid or unsafe temporal cursors fail closed
   - Given the temporal cursor is malformed, unsupported, cross-tenant, stale, unavailable, or outside retained coverage,
   - When reconstruction is requested,
   - Then the system returns a typed content-safe failure or migration-boundary response,
   - And it does not reveal whether protected records, events, redactions, policies, or audit evidence exist.

4. Projection-backed and replay-backed reconstruction exposes confidence metadata
   - Given reconstruction is projection-backed or replay-backed,
   - When freshness, rebuild state, unsupported schema, event gaps, out-of-order events, or unavailable projection data affects the result,
   - Then the response exposes freshness, completeness, and confidence metadata,
   - And it never presents incomplete historical state as authoritative.

5. Point-in-time tests prove deterministic, tenant-safe, redaction-safe behavior
   - Given point-in-time tests run,
   - When valid cursor, timestamp, event position, redacted content, retention changes, cross-tenant cursor, unsupported cursor, projection rebuild, and out-of-coverage scenarios are exercised,
   - Then tests prove deterministic reconstruction, tenant isolation, redaction safety, safe failure semantics, and stable temporal evidence behavior.

## Tasks / Subtasks

- [x] Define temporal reconstruction contracts without leaking EventStore internals (AC: 1, 3, 4)
  - [x] Add query/result contracts under `src/Hexalith.Conversations.Contracts/Queries/` for point-in-time detail reconstruction.
  - [x] Add a closed, validation-heavy temporal anchor/cursor contract that supports timestamp, safe source position, projection cursor/version, and contract-defined cursor forms.
  - [x] Include `SchemaVersion`, `TenantId`, `ConversationId`, caller/correlation metadata, safe next action, and confidence/freshness metadata.
  - [x] Do not expose EventStore stream names, aggregate actor IDs, raw event IDs, raw sequence numbers as substrate concepts, snapshot internals, or projection topology.
  - [x] Add serialization and forbidden-public-surface tests for the new contracts.

- [x] Extend deterministic replay to include governance events (AC: 1, 2, 4)
  - [x] Update `ConversationReplayVerifier` so replay can apply public and domain governance events already supported by `ConversationState`: `RetentionPolicySet`, `RetentionPolicyReplaced`, `ConversationContentMarkedSensitive`, and `MessageContentRedacted`, plus their domain counterparts where used by tests.
  - [x] Update event-type matching and metadata extraction for those governance events.
  - [x] Preserve existing fail-closed behavior for tenant mismatch, conversation mismatch, unsupported schema version, event-type mismatch, position gaps, reordered positions, duplicate non-idempotent event identities, malformed payloads, and unknown event types.
  - [x] Keep rejection events as ordered no-ops; they may prove negative command outcomes but must not mutate reconstructed state.

- [x] Build the temporal reconstruction service boundary (AC: 1, 3, 4)
  - [x] Add a server-side service under `src/Hexalith.Conversations.Server/Queries/` or `src/Hexalith.Conversations.Server/Projections/` that authorizes tenant access before reading any current projection, temporal cursor, replay stream, or target existence.
  - [x] Resolve the requested temporal anchor to an ordered bounded event set using only server-owned infrastructure.
  - [x] For timestamp anchors, include only events whose committed timestamp is at or before the anchor; handle same-timestamp ordering deterministically by source position.
  - [x] For position/projection-cursor anchors, include only events up to the safe source position represented by the cursor.
  - [x] Return a hidden/forbidden result for malformed, cross-tenant, unsupported, expired, outside-retained-coverage, and unknown cursors without differentiating protected existence.
  - [x] Return unavailable/rebuilding/confidence-limited results when the temporal source is unavailable, has gaps, is rebuilding, or cannot prove completeness.

- [x] Apply current disclosure policy to historical results (AC: 2)
  - [x] Ensure current redaction state is evaluated before returning historical message text, even when the requested anchor predates a redaction event.
  - [x] Add or project redaction read state so temporal responses can return redaction placeholders, policy reason class, actor attribution where allowed, timestamp, and audit handle without original redacted content.
  - [x] Include active retention policy at the requested anchor and separately identify when current retention/disclosure policy suppresses historical content.
  - [x] Keep audit evidence handles citeable where policy allows, but do not expose raw audit payload, raw message text, Party personal data, provider payloads, or unsafe upstream details.

- [x] Integrate the query handler without changing command behavior (AC: 1, 3, 4)
  - [x] Add a temporal query entry point to `ConversationQueryHandler` or a focused sibling handler, following the current `GetConversationQuery`/`ConversationDetailResult` pattern.
  - [x] Reuse `ConversationProjectionReadService`, `IConversationTenantAccessService`, and existing projection freshness vocabulary where possible.
  - [x] If the current projection is stale, rebuilding, unavailable, mixed-generation, or poisoned, block authoritative temporal claims unless the replay-backed source independently proves completeness.
  - [x] Do not add governance mutations, audit-record governance, privileged justification, export, evidence-bundle signing, UI time-slider behavior, or new background processing unless needed for the backend query contract.

- [x] Add focused tests and local evidence (AC: 1-5)
  - [x] Add contract tests for temporal anchor/result serialization, cursor validation, forbidden public substrate fields, and safe hidden/unavailable result shapes.
  - [x] Add replay tests proving retention, sensitivity, and redaction events reconstruct deterministically up to and after temporal anchors.
  - [x] Add server query tests for valid timestamp anchor, valid safe-position cursor, projection cursor, cross-tenant cursor, malformed cursor, unsupported cursor, unavailable source, projection rebuild, unsupported schema, and out-of-coverage behavior.
  - [x] Add redaction safety tests proving redacted message content is absent from temporal response details, safe next actions, diagnostics, `ToString()` output, and copied/citation-ready fields.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 2.6 evidence after implementation.

## Dev Notes

### Epic and Business Context

- Epic 2 covers governed retention, redaction, and audit. Story 2.6 is the read-side proof that previous governance work is historically inspectable, deterministic, and safe under tenant and disclosure policy. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Governed Retention, Redaction, and Audit`]
- Story 2.6 covers FR50: reconstruct message state and governance state as they existed at a prior point in time. It also supports the operator v1 success criterion: Find -> Read -> Trust with time-travel and attributed redactions. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.6: Reconstruct Point-in-Time Governance State`; `_bmad-output/planning-artifacts/prd.md#For operators / compliance stakeholders`]
- This is a temporal read/reconstruction capability. It must not create a new governance mutation path, bypass audit pairing, or make projections authoritative over EventStore history.

### Current Implementation State

- `ConversationState` already has replay state for the required governance concepts: active retention policy, sensitivity marks, redactions, messages, participants, file references, metadata, and lifecycle. Its `Apply` overloads already support public/domain retention, sensitivity, and redaction events. Use this instead of creating a second temporal state machine. [Source: `src/Hexalith.Conversations/State/ConversationState.cs`]
- `ConversationReplayVerifier` currently replays lifecycle, participant, message, file, and metadata events, but it does not yet dispatch retention, sensitivity, or redaction events. Story 2.6 must extend that verifier or create a focused temporal replay path that reuses `ConversationState` without duplicating state semantics. [Source: `src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`]
- `ConversationProjectionMaterializer` currently materializes current summary/detail projections with retention and sensitivity state, freshness metadata, gap/out-of-order/poison detection, and projection cursors. It does not currently project redaction state into `ConversationDetailProjectionV1`, and it does not provide point-in-time filtering by anchor. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`; `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`]
- `ConversationProjectionReadService` already applies fail-closed tenant access before projection reads and rejects unavailable, poison, forbidden, and mixed-generation states. Temporal reconstruction must preserve that ordering before any replay source, cursor decoding, target lookup, or differentiated failure response. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`]
- `ConversationQueryHandler` currently exposes current `GetConversationQuery` and `ListConversationsQuery` behavior only. The temporal query should match its content-safe result pattern while adding an explicit safe temporal anchor and confidence metadata. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`]
- `ProjectionFreshnessV1` already carries a safe projection cursor, last applied position, timestamp, generated-at time, lag, stale flag, trust state, and reason code. Prefer extending or composing this vocabulary instead of inventing unrelated trust metadata. [Source: `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`]

### Architecture Guardrails

- EventStore history is the only v1 source of truth for conversation state. Projections, caches, exports, UI models, conformance evidence, and admin views are derived and must be rebuildable. If derived state disagrees with replayed EventStore state, replay wins and derived state is marked stale, invalid, quarantined, or rebuilding. [Source: `_bmad-output/planning-artifacts/architecture.md#EventStore Authority Clarification`]
- Public contracts must hide EventStore mechanics. Temporal responses may expose Conversations-owned status, freshness, evidence references, and safe cursor metadata, but must not expose raw EventStore stream names, stream topology, aggregate actor IDs, snapshot mechanics, internal event names, or projection topology as stable public API. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Redaction is append-only, policy-governed, and auditable. Historical views still must honor current redaction and disclosure policy; temporal reconstruction is not permission to reveal pre-redaction text. [Source: `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`]
- Tenant access fails closed before aggregate load, command dispatch, projection read, export, rebuild, admin action, tool action, background job, verification detail access, or temporal reconstruction. Do not decode or resolve a temporal cursor in a way that leaks protected existence before tenant authorization. [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`]
- NFR42 and NFR43 require temporal evidence links to state their authoritative anchor and resolve deterministically enough to be legally meaningful. Use one safe anchor model and test it directly. [Source: `_bmad-output/planning-artifacts/prd.md#Data Integrity And Event Sourcing`]

### UX and Operator Context

- No UI implementation is required in this story, but backend DTOs must be usable by the future read-only governance viewer, evidence timeline, temporal navigation, citation controls, and trust summary band. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Platform Strategy`]
- The UI must render trust states from server-owned projections and command metadata; it must not reconstruct governance state client-side. Therefore Story 2.6 must return enough safe server-owned temporal state for later UI work. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Design System Acceptance Criteria`]
- Future temporal UI surfaces must show tenant scope, record identity, temporal cursor, trust posture, and evidence completeness before timeline reliance. The backend response should make those fields explicit and non-inferential. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Component Implementation Strategy`]

### Previous Story Intelligence

- Story 2.5 completed audit-pairing enforcement for retention, sensitivity, and redaction. It added `ConversationGovernanceAuditGate`, tightened handler ordering, validated returned audit evidence, and added `GovernanceAuditPairingSafetyNetTest` so implemented governance mutations cannot silently skip audit evidence. [Source: `_bmad-output/implementation-artifacts/2-5-enforce-audit-pairing-and-audit-unavailable-fail-closed-behavior.md#Dev Agent Record`]
- Carry forward the Story 2.5 boundary: this story reads historical evidence but must not add any mutation path that bypasses `IConversationGovernanceAuditService` or the existing command handlers.
- Story 2.5 full validation passed: focused aggregate/server/contract tests and `dotnet test Hexalith.Conversations.slnx` passed 556 tests. Treat the current tests as the baseline to preserve.

### Git Intelligence

- Recent sequence:
  - `15e7605 feat(story-2.5): Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior`
  - `f9375e1 feat(story-2.4): Redact Message Content with Audit Attribution`
  - `5204e66 feat(story-2.3): Mark Conversation Content as Sensitive`
  - `229e1fa feat(story-2.2): Set Conversation Retention Policy with Rationale`
  - `8ec43fb feat(story-2.1): Define Governance Policy and Audit Contracts`
- The established Epic 2 pattern is additive contracts, focused server/domain implementation, aggregate or replay support, idempotency/audit preservation, publication/projection mapping when needed, and explicit contract/server/aggregate tests. Follow that pattern for temporal contracts and replay/query tests.

### File Structure Requirements

- Likely UPDATE files:
  - `src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`
  - `src/Hexalith.Conversations/Replay/ConversationReplayResult.cs` only if the result needs temporal anchor/confidence metadata; prefer a separate temporal result if that keeps existing replay tests stable.
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventRecord.cs` only if safe temporal anchor metadata requires additional public source fields.
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
  - `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
  - `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs` only if existing freshness metadata cannot represent temporal confidence safely.
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs` only if existing hidden/unavailable/schema/audit/idempotency codes cannot express temporal failures safely.
  - `tests/Hexalith.Conversations.Tests/Replay/ConversationReplayVerifierTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- Likely NEW files:
  - `src/Hexalith.Conversations.Contracts/Queries/GetConversationAtPointInTimeQuery.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailResult.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchor.cs` or `ConversationTemporalAnchorV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalCursor.cs` if cursor encoding is separate from the anchor value object.
  - `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationTemporalQueryHandler.cs` or `src/Hexalith.Conversations.Server/Projections/ConversationTemporalReconstructionService.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalQueryHandlerTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs`
- Keep new public contracts in `Contracts`. Keep replay/domain behavior in `Hexalith.Conversations`. Keep tenant authorization, cursor decoding, projection/replay source access, and handler orchestration in `Server`.

### Testing Requirements

- Run focused tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
  - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifierTest|FullyQualifiedName~Temporal"`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`
- Use xUnit v3 and Shouldly as existing tests do. Microsoft Learn documents `dotnet test --filter <Expression>` with `FullyQualifiedName~...` filtering, and NuGet Central Package Management requires package versions in `Directory.Packages.props` while project `PackageReference` entries omit `Version`. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`; `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Out of Scope

- Do not implement UI time-slider, citation-copy UI, evidence bundle generation, signed release artifacts, export workflows, or audit-record governance. Those are later Epic 3, Epic 5, and Story 2.7/2.8 concerns unless a newer sprint change proposal promotes them.
- Do not implement archive, logical delete, legal hold, audit-record retention/redaction, or privileged operational justification workflows unless matching commands/events already exist when implementation begins.
- Do not expose raw EventStore mechanics as public temporal anchors.
- Do not create a transcript table, durable cache authority, derived index, or background queue for temporal state without an ADR.
- Do not reveal pre-redaction content merely because the requested anchor predates the redaction.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 2.6: Reconstruct Point-in-Time Governance State`
- `_bmad-output/planning-artifacts/prd.md#For operators / compliance stakeholders`
- `_bmad-output/planning-artifacts/prd.md#Data Integrity And Event Sourcing`
- `_bmad-output/planning-artifacts/architecture.md#EventStore Authority Clarification`
- `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`
- `_bmad-output/planning-artifacts/architecture.md#Process Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Design System Acceptance Criteria`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `_bmad-output/implementation-artifacts/2-5-enforce-audit-pairing-and-audit-unavailable-fail-closed-behavior.md`
- `src/Hexalith.Conversations/State/ConversationState.cs`
- `src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 22 passed.
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifierTest|FullyQualifiedName~Temporal"` - 20 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` - 53 passed.
- `dotnet test Hexalith.Conversations.slnx` - 572 passed.

### Completion Notes List

- Story context created from sprint status, Epic 2 requirements, PRD/architecture/UX/project-context rules, prior Story 2.5, current replay/projection/query implementation, current test evidence, recent git history, and current Microsoft .NET/NuGet documentation.
- Added safe temporal reconstruction query/result contracts, anchor validation, confidence metadata, and redaction projection DTOs without exposing EventStore internals as public API.
- Extended deterministic replay and projection materialization for retention, sensitivity, and redaction governance events, including current redaction suppression of projected and temporal message text.
- Added `ConversationTemporalReconstructionService` plus a `ConversationQueryHandler` entry point that authorizes tenant access before cursor/source resolution, bounds events by timestamp or safe position, returns safe hidden/unavailable/rebuilding outcomes, and applies current disclosure policy to historical details.
- Added focused contract, replay, projection, and server temporal tests; focused filters and the full solution suite pass.
- Review auto-fix hardened temporal reconstruction so safe-position/cursor anchors beyond retained coverage fail closed and incomplete temporal sources do not return authoritative details.
- Review auto-fix hardened projection redaction ordering so existing redaction read state suppresses later materialized message text.

### File List

- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalConfidenceV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationAtPointInTimeQuery.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalEventSourceResult.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalEventSourceState.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
- `src/Hexalith.Conversations.Server/Queries/IConversationTemporalEventSource.cs`
- `src/Hexalith.Conversations.Server/Queries/UnavailableConversationTemporalEventSource.cs`
- `src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs`
- `tests/Hexalith.Conversations.Tests/Replay/ConversationReplayVerifierTest.cs`

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Critical source documents loaded: sprint status, epics, PRD, architecture, UX design artifacts, project context, Story 2.5, recent git history, current replay state/verifier, projection materializer/read-service/query handler, existing projection/query contracts, and focused tests.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, implementation tasks, likely update/new files, current-code gaps, previous story learnings, tenant/redaction/freshness/EventStore-authority guardrails, explicit out-of-scope boundaries, local package/tooling rules, and required regression tests.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-22.

### Findings Fixed

- [HIGH] Safe-position and cursor anchors beyond the retained temporal source tail could reconstruct the last available event instead of failing closed. Fixed in `ConversationTemporalReconstructionService` and covered by `OutOfCoverageAndUnsupportedSchemaShouldNotRevealProtectedDetails`.
- [HIGH] Incomplete temporal sources could return visible details with an authoritative anchor and stable-evidence next action. Fixed by returning a rebuilding result without details when source completeness cannot be proven; covered by `RebuildingOrIncompleteSourcesShouldReturnConfidenceLimitedResults`.
- [HIGH] Projection materialization could expose later message text if redaction read state existed before that message was materialized. Fixed message application to honor existing redaction state and added `EarlierRedactionStateShouldSuppressLaterProjectedMessageText`.

### Review Checklist

- [x] Story file loaded from `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md`.
- [x] Story Status verified as reviewable before review.
- [x] Epic and Story IDs resolved as 2.6.
- [x] Architecture/project context and planning references loaded from `_bmad-output`.
- [x] Microsoft Learn documentation search performed for `dotnet test --filter` behavior.
- [x] Acceptance Criteria cross-checked against implementation.
- [x] File List reviewed against git status and source changes.
- [x] Tests mapped to ACs and expanded for review fixes.
- [x] Code quality and security review performed on changed source files.
- [x] Outcome: approved after auto-fixes.
- [x] Story status and sprint status synced to `done`.

## Change Log

- 2026-05-22: Created Story 2.6 context from Epic 2 requirements, architecture constraints, UX trust requirements, project context, prior Story 2.5 audit-pairing implementation, current replay/projection/query code, tests, recent git history, and Microsoft .NET/NuGet documentation.
- 2026-05-22: Implemented point-in-time temporal reconstruction contracts, governance replay/projection support, tenant-safe server reconstruction, current redaction disclosure enforcement, focused tests, and test evidence; story moved to review.
- 2026-05-22: Senior developer review auto-fixed retained-coverage, incomplete-source, and redaction-ordering issues; focused filters and full solution suite passed; story moved to done.
