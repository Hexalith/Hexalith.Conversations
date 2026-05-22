# Story 2.5: Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance owner,
I want every governance mutation to require paired audit evidence,
so that no retention, sensitivity, redaction, archival, or privileged governance action can silently change state.

## Acceptance Criteria

1. Governed mutation success is transactionally paired with audit evidence
   - Given a governance mutation command is evaluated,
   - When audit recording is available and all policy, tenant, role, schema, state, idempotency, and target checks pass,
   - Then the system records or emits the domain mutation and paired audit evidence as one governed operation boundary,
   - And the response exposes only a safe audit handle where policy allows.

2. Audit-unavailable and unsafe audit states fail closed
   - Given audit recording is unavailable, ambiguous, stale, denied, unsafe to expose, policy-blocked, mismatched, or fails validation,
   - When a governance mutation command is submitted,
   - Then the command fails closed with a typed audit-unavailable, audit-required, policy-blocked, or audit-uncertain rejection,
   - And no governance domain mutation event, projection mutation, publication, success audit record, or success idempotency outcome is produced.

3. Non-governance command behavior during audit degradation is explicit
   - Given non-governance conversation activity occurs during audit degradation,
   - When the command does not mutate governance state,
   - Then the system follows the active ADR or policy for whether the activity may continue,
   - And the response clearly distinguishes non-governance allowance from governance mutation denial.

4. Every successful governance mutation has verifiable audit evidence
   - Given audit pairing is enforced,
   - When retention, sensitivity, redaction, archival, privileged metadata mutation, and audit-record action paths are exercised,
   - Then every successful governance mutation has a corresponding audit evidence record with tenant, conversation, actor, timestamp, policy basis, rationale, operation, and outcome,
   - And missing or mismatched audit evidence is treated as a release-blocking verification failure.

5. Audit enforcement tests prove no silent mutation paths
   - Given audit enforcement tests run,
   - When successful governance mutations, audit sink outage, partial audit failure, duplicate governance command, rejected governance command, and non-governance command behavior during audit degradation are exercised,
   - Then tests prove fail-closed governance behavior, paired evidence, no silent mutation paths, typed errors, tenant isolation, idempotency safety, and content-safe diagnostics.

## Tasks / Subtasks

- [x] Inventory all implemented governance mutation paths and keep the list explicit (AC: 1, 4)
  - [x] Include current implemented paths: `SetConversationRetentionPolicyCommandHandler`, `MarkConversationContentSensitiveCommandHandler`, and `RedactMessageContentCommandHandler`.
  - [x] Include current aggregate mutation methods: `ConversationAggregate.Handle(SetConversationRetentionPolicy)`, `Handle(MarkConversationContentSensitive)`, and `Handle(RedactMessageContent)`.
  - [x] Treat `GovernanceOperationKind.ArchiveConversation`, `LogicallyDeleteConversation`, `DeferForLegalHold`, `GovernAuditRecord`, and `RecordPrivilegedJustification` as vocabulary/prepared future paths only unless matching handlers already exist when implementation starts.

- [x] Add or extract one server-side audit-enforcement boundary for governed mutations (AC: 1, 2, 4)
  - [x] Reuse `IConversationGovernanceAuditService` and `ConversationGovernanceAuditResult`; do not create a parallel audit service or public audit substrate.
  - [x] Preserve the existing handler ordering: schema shape -> trusted tenant binding -> tenant/governance access -> semantic shape -> idempotency reservation -> state/target validation where required -> audit proof -> aggregate dispatch.
  - [x] Ensure audit service exceptions map to `ConversationErrorCode.AuditSinkUnavailable` with reason `audit_unavailable`.
  - [x] Ensure `AuditUnavailable`, missing evidence, `Uncertain`, `UnsafeEvidence`, and `PolicyBlocked` all return typed rejections without mutation events.
  - [x] Keep the boundary in `src/Hexalith.Conversations.Server/Governance/` or another existing server-side location; public `Contracts` must not reference server infrastructure.

- [x] Close the audit-evidence pairing gap across existing commands (AC: 1, 2, 4)
  - [x] Redaction already validates returned audit evidence against command policy reference and operation timestamp; keep that behavior.
  - [x] Add pre-audit state validation for retention and sensitivity where the handler can prove rejection before audit: missing conversation, tenant mismatch, closed lifecycle, invalid target, incompatible duplicate, or other aggregate validation that does not require audit proof.
  - [x] Add equivalent returned-evidence validation for retention policy and sensitivity commands so successful audit evidence cannot be accepted when `PolicyReference` or `CapturedAt` differs from the command.
  - [x] Return `ConversationErrorCode.AuditPairingRequired` with a stable reason such as `audit_pairing_mismatch` for mismatched returned evidence.
  - [x] Confirm aggregate validation for all governance mutations still rejects null audit evidence with reason `audit_pairing_required`.

- [x] Preserve fail-closed tenant, disclosure, and idempotency ordering (AC: 2, 3, 5)
  - [x] Tenant/governance denial must happen before aggregate load, EventStore stream resolution, target existence checks, projection reads, audit calls, idempotency result disclosure, or differentiated errors.
  - [x] Idempotency conflicts and completed duplicate replay must not create duplicate audit evidence.
  - [x] Compatible no-op behavior for sensitivity/redaction must not become an unaudited mutation and must not produce duplicate audit evidence.
  - [x] Non-governance command behavior during audit degradation must remain unchanged unless an active ADR explicitly permits a change; missing ADR coverage must block new behavior instead of inventing implicit allowance.

- [x] Add focused regression tests for cross-operation audit enforcement (AC: 2, 4, 5)
  - [x] Add/extend server tests for retention, sensitivity, and redaction covering audit unavailable, uncertain, unsafe evidence, policy blocked, audit-service exception, mismatched evidence, and successful evidence.
  - [x] Assert no mutation event type is emitted for audit failures: `RetentionPolicySetDomainEvent`, `RetentionPolicyReplacedDomainEvent`, `ConversationContentMarkedSensitiveDomainEvent`, or `MessageContentRedactedDomainEvent`.
  - [x] Assert audit service call counts for denied, invalid, duplicate, no-op, and successful paths.
  - [x] Add aggregate tests proving null/mismatched audit evidence fails before mutation events where command-level validation can evaluate the mismatch.
  - [x] Add a named conformance-style test or explicit test group that exercises all implemented governance mutations as a release-gate audit-pairing safety net.

- [x] Update local evidence and story status artifacts (AC: 5)
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with the Story 2.5 evidence.
  - [x] Keep `sprint-status.yaml` transitions consistent with the workflow.
  - [x] Do not implement projection redaction, point-in-time reconstruction, audit-record governance, privileged operational justification, UI/export behavior, or release manifest signing in this story.

## Dev Notes

### Epic and Business Context

- Epic 2 covers governed retention, redaction, and audit. Its business value is that authorized users can apply governance controls with paired audit evidence and fail-closed audit behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Governed Retention, Redaction, and Audit`]
- Story 2.5 is the cross-cutting enforcement story for FR47-FR49: every governance mutation must be audit-paired, governance mutation must reject when audit recording is unavailable, and non-governance activity during audit degradation must follow an explicit ADR/policy. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.5: Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior`]
- This story is not a new governance feature surface. It hardens the existing retention, sensitivity, and redaction implementation so future governance mutation paths cannot drift into unaudited success.

### Current Implementation State

- `IConversationGovernanceAuditService` currently exposes per-operation audit methods for retention policy changes, sensitivity marks, and redaction. The result model uses `ConversationGovernanceAuditStatus.Succeeded`, `AuditUnavailable`, `Uncertain`, `UnsafeEvidence`, and `PolicyBlocked`. [Source: `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`; `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditResult.cs`; `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditStatus.cs`]
- Retention and sensitivity handlers already guard tenant/governance access before audit calls, map audit failures to sanitized rejections, and dispatch only after audit success. They do not yet validate all state/target conflicts before audit, and they do not yet validate that returned audit evidence matches the command policy reference and operation timestamp. [Source: `src/Hexalith.Conversations.Server/CommandHandlers/SetConversationRetentionPolicyCommandHandler.cs`; `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs`]
- Redaction is the strongest current pattern: it validates state/target before audit, returns compatible duplicate no-op before audit, records audit evidence, validates returned evidence with `ValidateAuditEvidenceProvided`, and dispatches only after pairing succeeds. Use this as the reference behavior for retention and sensitivity where applicable. [Source: `src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs`; `src/Hexalith.Conversations/Validation/RedactMessageContentValidation.cs`; `src/Hexalith.Conversations/Validation/RedactMessageContentBoundary.cs`]
- Aggregate validation already rejects missing audit evidence for retention, sensitivity, and redaction using `ConversationErrorCode.AuditPairingRequired` and `audit_pairing_required`. Redaction also rejects policy/timestamp mismatch as `audit_pairing_mismatch`; retention and sensitivity need equivalent validation if returned evidence can be checked before domain event creation. [Source: `src/Hexalith.Conversations/Validation/SetConversationRetentionPolicyValidation.cs`; `src/Hexalith.Conversations/Validation/MarkConversationContentSensitiveValidation.cs`; `src/Hexalith.Conversations/Validation/RedactMessageContentValidation.cs`]
- Governance operation vocabulary already includes future operations for archive, logical delete, legal hold deferral, audit-record governance, and privileged justification. Do not implement those workflows unless corresponding handlers already exist; Story 2.5 should make the enforcement pattern ready for them. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`]

### Architecture Guardrails

- Governance mutations require paired audit/domain evidence and must fail closed when audit recording is unavailable. Non-governance commands may continue during audit degradation only by explicit ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Governance Security`]
- Architecture ADR backlog names ADR-004 for governance audit pairing enforcement and audit-unavailable command behavior. If implementation needs to change non-governance behavior during audit degradation, stop and require the ADR rather than encoding implicit policy. [Source: `_bmad-output/planning-artifacts/architecture.md#Open Questions and ADR Backlog`]
- Testing and release evidence are architectural constraints. Automated evidence must prove tenant fail-closed access, EventStore write authority, governance audit pairing, projection freshness signaling, adapter contract compliance, and redaction non-disclosure. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing and Release Evidence`]
- No write path may bypass EventStore or mutate governance state directly. Projection, publication, admin, worker, MCP/tool, export, rebuild, verification, and command-handler paths are not exempt from the same tenant/governance/audit boundaries. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- If a conflict changes public contracts, durable state, governance behavior, or disclosure surfaces, create or update an ADR before implementation. [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`]

### File Structure Requirements

- Likely UPDATE files:
  - `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs` only if the enforcement boundary requires an additional generalized method; prefer preserving the existing methods if they are sufficient.
  - `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditResult.cs`
  - `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditStatus.cs`
  - `src/Hexalith.Conversations.Server/CommandHandlers/SetConversationRetentionPolicyCommandHandler.cs`
  - `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs`
  - `src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs`
  - `src/Hexalith.Conversations/Validation/SetConversationRetentionPolicyValidation.cs`
  - `src/Hexalith.Conversations/Validation/MarkConversationContentSensitiveValidation.cs`
  - `src/Hexalith.Conversations/Validation/RedactMessageContentValidation.cs`
  - `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
  - `tests/Hexalith.Conversations.Server.Tests/TenantAccess/SetConversationRetentionPolicyCommandHandlerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs`
  - `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRetentionPolicyTest.cs`
  - `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateSensitivityTest.cs`
  - `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs`
- Likely NEW files, only if they reduce duplication without widening public API:
  - `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationGovernanceAuditGateTest.cs`
- Keep public contracts as sealed records with existing validation and XML documentation conventions. Do not add package versions to `.csproj` files; this repository uses central package management in `Directory.Packages.props`. [Source: `Directory.Packages.props`; Microsoft Learn Central Package Management]

### Testing Requirements

- Run focused tests first:
  - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter FullyQualifiedName~Governance`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter FullyQualifiedName~TenantAccess`
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
- Then run the solution:
  - `dotnet test Hexalith.Conversations.slnx`
- Current local tooling uses .NET SDK `10.0.300`, target framework `net10.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and Microsoft.NET.Test.Sdk `18.3.0`. Keep those pins unless a separate dependency story changes them. [Source: `global.json`; `Directory.Build.props`; `Directory.Packages.props`]
- Microsoft Learn confirms `dotnet test` is the .NET test driver and that NuGet Central Package Management expects package versions in `Directory.Packages.props` with project `PackageReference` entries omitting `Version`. Use that pattern if any new test project/package reference is needed. [Source: `https://learn.microsoft.com/dotnet/core/tools/dotnet-test-vstest`; `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Previous Story Intelligence

- Story 2.4 implemented redaction command, result, public event, domain command/event, aggregate handling, replay-only state, validation boundary, audit gate, idempotency fingerprinting, and publication mapping. [Source: `_bmad-output/implementation-artifacts/2-4-redact-message-content-with-audit-attribution.md#Dev Agent Record`]
- Story 2.4 review fixed two audit-related defects that directly affect this story:
  - Redaction recorded audit evidence before state/target/conflict validation, creating side effects for invalid targets or compatible duplicate redactions.
  - Successful audit evidence was accepted without verifying returned policy reference and timestamp matched the command.
  [Source: `_bmad-output/implementation-artifacts/2-4-redact-message-content-with-audit-attribution.md#Senior Developer Review (AI)`]
- Carry those fixes into retention and sensitivity. Do not call audit for invalid state/target paths where the handler can prove rejection first, and do not accept mismatched audit evidence as proof of a governed mutation.
- Previous validation was environment-limited: no-build probes had passed, but compile/test execution after review fixes was blocked in the sandbox by denied writes to generated test output/coverage mapping files. Re-run full tests in a writable local environment for Story 2.5. [Source: `_bmad-output/implementation-artifacts/2-4-redact-message-content-with-audit-attribution.md#Senior Developer Review (AI)`]

### Git Intelligence

- Recent story sequence:
  - `f9375e1 feat(story-2.4): Redact Message Content with Audit Attribution`
  - `5204e66 feat(story-2.3): Mark Conversation Content as Sensitive`
  - `229e1fa feat(story-2.2): Set Conversation Retention Policy with Rationale`
  - `8ec43fb feat(story-2.1): Define Governance Policy and Audit Contracts`
  - `668cca4 feat(story-1.11): Prove Replay, Schema Versioning, and Projection Rebuild Behavior`
- The last three commits show the local implementation pattern: add public contract and result shapes, server command handler, domain command/event, aggregate validation, state replay support, idempotency fingerprinting, publication mapping when a new event exists, and focused contract/server/aggregate/idempotency tests.
- Story 2.5 should be smaller than Stories 2.2-2.4: it should harden and centralize audit enforcement for existing paths, not add a new durable event type unless implementation discovers a real missing audit-enforcement event contract.

### Out of Scope

- Do not implement archive, logical delete, legal hold, audit-record governance, or privileged operational justification workflows unless they already exist when implementation starts.
- Do not implement projection redaction, point-in-time reconstruction, UI/export behavior, signed release manifest rows, evidence bundle generation, or audit-record retention/redaction policy.
- Do not introduce a Roslyn analyzer; the PRD explicitly dropped it from v1 in favor of code-level aggregate/server enforcement plus property/conformance tests.
- Do not queue unaudited governance writes for later audit. Audit-write failure mode is block/fail-closed, never queue.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 2.5: Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior`
- `_bmad-output/planning-artifacts/prd.md#MVP Definition`
- `_bmad-output/planning-artifacts/architecture.md#Governance Security`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `_bmad-output/implementation-artifacts/2-4-redact-message-content-with-audit-attribution.md#Senior Developer Review (AI)`
- `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/SetConversationRetentionPolicyCommandHandler.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs`
- `src/Hexalith.Conversations/Validation/SetConversationRetentionPolicyValidation.cs`
- `src/Hexalith.Conversations/Validation/MarkConversationContentSensitiveValidation.cs`
- `src/Hexalith.Conversations/Validation/RedactMessageContentValidation.cs`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`
- `https://learn.microsoft.com/dotnet/core/tools/dotnet-test-vstest`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Initial parallel focused test execution caused a build-output file lock; reran validation commands serially.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter FullyQualifiedName~Governance` completed with no matching aggregate tests.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationAggregateRetentionPolicyTest|FullyQualifiedName~ConversationAggregateSensitivityTest|FullyQualifiedName~ConversationAggregateRedactionTest"` passed 31 tests.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter FullyQualifiedName~TenantAccess` passed 125 tests.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` passed 156 tests.
- 2026-05-22: `dotnet test Hexalith.Conversations.slnx` passed 555 tests.
- 2026-05-22: Review fix tightened `GovernanceAuditPairingSafetyNetTest` to fail if any audited aggregate command is missing from the explicit governance mutation inventory.
- 2026-05-22: Review fix added non-governance command inventory coverage proving current non-governance aggregate paths remain outside governance audit-sink degradation handling.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Governance"` passed 128 tests.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationAggregateRetentionPolicyTest|FullyQualifiedName~ConversationAggregateSensitivityTest|FullyQualifiedName~ConversationAggregateRedactionTest"` passed 31 tests.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` passed 156 tests.
- 2026-05-22: `dotnet test Hexalith.Conversations.slnx` passed 556 tests.

### Completion Notes List

- Story context created from sprint status, Epic 2 requirements, PRD/architecture/project-context rules, Story 2.4 implementation and review learnings, recent git history, current governance handlers, validation code, and tests.
- Story explicitly identifies the audit evidence pairing gap in retention and sensitivity relative to the hardened redaction path.
- Story status set to ready-for-dev.
- Added `ConversationGovernanceAuditGate` to centralize fail-closed audit-service exception handling without changing public contracts or creating a parallel audit service.
- Retention and sensitivity handlers now validate state/target/no-op conditions before audit where possible, validate returned audit evidence against command policy/timestamp, and dispatch only after pairing succeeds.
- Retention and sensitivity aggregate validation now rejects mismatched audit evidence with `ConversationErrorCode.AuditPairingRequired` and reason `audit_pairing_mismatch`, matching the existing redaction behavior.
- Added regression tests for audit-service exception mapping, mismatched evidence rejection, pre-audit invalid state/target/no-op behavior, and a named governance audit-pairing safety-net inventory.
- Review fixed the safety-net test so omitted audited aggregate commands and non-governance command drift are detected explicitly.
- Story moved to `review` after focused and full solution validation passed.
- Senior developer review completed with no remaining critical issues; story moved to `done`.

### File List

- `_bmad-output/implementation-artifacts/2-5-enforce-audit-pairing-and-audit-unavailable-fail-closed-behavior.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs`
- `src/Hexalith.Conversations.Server/CommandHandlers/SetConversationRetentionPolicyCommandHandler.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`
- `src/Hexalith.Conversations/Validation/MarkConversationContentSensitiveBoundary.cs`
- `src/Hexalith.Conversations/Validation/MarkConversationContentSensitiveValidation.cs`
- `src/Hexalith.Conversations/Validation/SetConversationRetentionPolicyBoundary.cs`
- `src/Hexalith.Conversations/Validation/SetConversationRetentionPolicyValidation.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/SetConversationRetentionPolicyCommandHandlerTest.cs`
- `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRetentionPolicyTest.cs`
- `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateSensitivityTest.cs`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-22

Outcome: Approved after automatic fixes. No CRITICAL issues remain.

Findings and fixes:
- MEDIUM: `GovernanceAuditPairingSafetyNetTest` listed implemented governance paths but would not fail if a new audited aggregate command was added outside the inventory. Fixed by deriving audited aggregate command types from required `GovernanceAuditEvidenceReference` usage and asserting exact inventory coverage.
- MEDIUM: AC 3 / AC 5 lacked explicit non-governance audit-degradation evidence. Fixed by adding release-gate coverage that current non-governance aggregate commands are `CreateConversation` and `AddParticipant`, and that `AddParticipantCommandHandler` does not depend on `IConversationGovernanceAuditService`.

Validation:
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Governance"` - 128 passed.
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationAggregateRetentionPolicyTest|FullyQualifiedName~ConversationAggregateSensitivityTest|FullyQualifiedName~ConversationAggregateRedactionTest"` - 31 passed.
- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` - 156 passed.
- `dotnet test Hexalith.Conversations.slnx` - 556 passed.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Critical source documents loaded: sprint status, epics, PRD, architecture, UX design artifacts, project context, Story 2.4, recent git history, current governance server handlers, validation classes, aggregate code, and focused tests.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, implementation tasks, update-file targets, existing-code current state, previous story learnings, audit/tenant/idempotency guardrails, explicit out-of-scope boundaries, local package/tooling rules, and required regression tests.

## Change Log

- 2026-05-22: Created Story 2.5 context from Epic 2 requirements, architecture constraints, project context, previous Story 2.4 review findings, current governance command handlers, validation code, tests, recent git history, and current Microsoft .NET/NuGet documentation.
- 2026-05-22: Implemented audit pairing enforcement hardening for existing governance mutation paths; added regression/safety-net tests and local evidence; focused and full solution tests passed; status moved to review.
- 2026-05-22: Senior developer review tightened audit-pairing and non-governance safety-net coverage; focused and full solution validation passed; status moved to done.
