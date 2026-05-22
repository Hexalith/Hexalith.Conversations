# Story 2.8: Record and Review Privileged Operational Justification

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance reviewer,
I want privileged operational actions that touch tenant-scoped conversation data to include structured justification and reviewable audit evidence,
so that operator access is accountable and tenant-visible where policy requires.

## Acceptance Criteria

1. Structured justification is required before privileged action execution
   - Given an operator performs a privileged action that reads, rebuilds, repairs, exports, verifies, changes visibility, changes metadata, or otherwise touches tenant conversation data,
   - When the action is requested,
   - Then the system requires structured justification with tenant scope, affected conversation or scope, actor, operation class, policy basis, rationale, timestamp, and correlation metadata,
   - And the action cannot proceed when required justification is missing or invalid.

2. Privileged action outcomes are audit-linked and content-safe
   - Given a privileged action succeeds, fails, is denied, or is partially completed,
   - When audit evidence is recorded,
   - Then the audit record links justification, actor, timestamp, tenant, affected conversation or scope, policy basis, result, and resulting domain or operational evidence,
   - And the audit payload remains content-safe.

3. Authorized reviewers can inspect privileged-action history coherently
   - Given a reviewer opens privileged-action history,
   - When the reviewer is authorized for the tenant and audit scope,
   - Then the reviewer can inspect justification, actor, timestamp, tenant, affected conversation, policy basis, outcome, and audit handle as one coherent record,
   - And redacted or unavailable fields are clearly distinguished from missing fields.

4. Unsafe privileged actions fail closed
   - Given a privileged action is unauthorized, cross-tenant, stale, unsupported, or missing audit availability,
   - When the operation is evaluated,
   - Then the system returns a typed content-safe denial or audit-unavailable result,
   - And no privileged mutation or disclosure occurs.

5. Tests prove enforcement, reviewability, and non-disclosure
   - Given privileged-action tests run,
   - When approved access, missing justification, stale justification, unauthorized operator, cross-tenant target, audit unavailable, partial failure, and review-history scenarios are exercised,
   - Then tests prove structured justification enforcement, tenant-visible audit evidence, reviewability, typed failure semantics, and content-safe diagnostics.

## Tasks / Subtasks

- [x] Define privileged operational justification public contracts (AC: 1, 2, 3)
  - [x] Add a closed operation-class vocabulary under `src/Hexalith.Conversations.Contracts/Governance/` for the story's privileged action classes: read, rebuild, repair, export, verify, visibility change, metadata change, and generic tenant-data touch. Do not overload `AuditRecordActionClassification`; that vocabulary is audit-record policy treatment from Story 2.7.
  - [x] Reuse existing `GovernanceOperationKind.RecordPrivilegedJustification` and `PrivilegedActionClass` where they fit, but add the missing structured operation class if the current `OperationalOverride`, `ComplianceReview`, and `SupportAssistance` values are too coarse for AC1.
  - [x] Add a contract such as `PrivilegedOperationalJustificationV1` or `RecordPrivilegedOperationalJustificationCommand` that carries schema version, tenant scope, optional conversation identity or governed scope, actor Party ID, privileged operation class, policy reference, content-safe rationale, operation timestamp, correlation ID, optional causation ID, and optional affected audit/evidence handle.
  - [x] Add a result/detail contract for review such as `PrivilegedOperationalJustificationResult` and `PrivilegedOperationalJustificationDetailsV1` that links the justification to `GovernanceAuditEvidenceReference`, `GovernanceOutcome`, safe next action, and projection/trust metadata.
  - [x] Keep rationale/policy fields validation aligned with `GovernanceOperationMetadata`: required, bounded, content-safe, and absent from `ToString()` output.
  - [x] Do not include raw conversation content, prompt text, redacted text, Party personal data, provider payloads, raw upstream details, raw audit sink IDs, EventStore streams/positions/topology, storage paths, exception bodies, tokens, claims, or caller-editable tenant/user authority.

- [x] Add server-side privileged justification enforcement boundary (AC: 1, 2, 4)
  - [x] Add a focused service under `src/Hexalith.Conversations.Server/Governance/` or `src/Hexalith.Conversations.Server/Queries/`, for example `ConversationPrivilegedOperationalJustificationService`.
  - [x] Require tenant access before parsing target-specific details, resolving audit handles, reading projections, dispatching privileged work, recording differentiated denials, or returning any target-specific response.
  - [x] Map privileged action classes to tenant access requirements deliberately: privileged read/export/verification/rebuild review uses at least `Admin`; governance-changing metadata/visibility/policy mutations use `Governance`; never let these paths run under ordinary `Read` alone.
  - [x] Require `Current` projection freshness unless the contract explicitly declares a narrower accepted state. `Stale`, `Rebuilding`, and `Unavailable` must block privileged decisions by default.
  - [x] Treat missing, malformed, stale, unsupported, cross-tenant, or policy-blocked justification as a content-safe denial. The response must not reveal protected target existence.
  - [x] Reuse `ConversationGovernanceAuditGate.RecordRequiredAsync()` for required audit evidence. Any audit exception, uncertain evidence, unsafe evidence, or unavailable sink fails closed.

- [x] Extend audit service contracts without bypassing audit pairing (AC: 2, 4)
  - [x] Extend `IConversationGovernanceAuditService` with a privileged justification method such as `RecordPrivilegedOperationalJustificationAsync(...)` that returns `ConversationGovernanceAuditResult`.
  - [x] The audit evidence must use `GovernanceOperationKind.RecordPrivilegedJustification` and include target/scope, actor, policy basis, rationale class or content-safe rationale where policy allows, operation class, outcome, timestamp, and correlation metadata.
  - [x] Record evidence for succeeded, failed, denied, partial, and audit-unavailable outcomes where policy requires, but do not create recursive unaudited audit writes. If denial logging itself cannot be audited safely, return `PolicyBlocked` or `AuditUnavailable` instead of writing unaudited state.
  - [x] Update `GovernanceAuditPairingSafetyNetTest` so `RecordPrivilegedJustification` moves from future-only vocabulary into the explicitly implemented privileged operational audit boundary, without falsely treating ordinary non-governance commands as audit-sink dependent.

- [x] Add governed review-history query behavior (AC: 3, 4)
  - [x] Provide a tenant-scoped review query/result under `src/Hexalith.Conversations.Contracts/Queries/` and a server query service that returns privileged-action history or one privileged-action record only after authorization.
  - [x] Build review details from existing audit evidence/projection-safe references. If the current implementation cannot reconstruct a history without a new durable store, return a documented unavailable/policy-blocked result and stop for ADR before adding storage.
  - [x] Use existing `ProjectionTrustState` and `ProjectionFreshnessReasonCode` for visible, forbidden, redacted, unavailable, rebuilding, stale, and policy-blocked states. Add public states only if contract tests prove the existing vocabulary cannot express AC3.
  - [x] Redacted/unavailable fields must be explicit states. Missing fields must mean absent data, not hidden data. Do not let the UI or caller infer hidden privileged evidence from nulls.
  - [x] Integrate through `ConversationQueryHandler` only if the dependency graph stays focused; otherwise add a focused sibling handler and register it in `ConversationQueryServiceCollectionExtensions`.

- [x] Integrate with existing privileged and audit-record surfaces without scope creep (AC: 1-5)
  - [x] Make Story 2.7 audit-record review understand privileged justification records where they are represented by safe `GovernanceAuditEvidenceReference` data.
  - [x] Preserve existing retention, sensitivity, redaction, audit-record read/export, temporal reconstruction, and list/detail query semantics.
  - [x] Do not add a real rebuild worker, repair worker, export worker, verification engine, metadata mutation command, visibility mutation command, MCP/tool operation, UI drawer, durable export artifact, evidence bundle, derived index, cache authority, or new storage table in this story without an accepted ADR or explicit waiver.
  - [x] If a privileged action wrapper is added for future rebuild/export/verification paths, it must be a reusable precondition/audit boundary and tests must prove it does not execute the supplied operation when justification, tenant access, freshness, or audit availability fails.

- [x] Add focused tests and local evidence (AC: 1-5)
  - [x] Add contract tests for privileged operation-class vocabulary, justification command/detail JSON shape, required fields, unsupported vocabulary rejection, `ToString()` safety, and forbidden substrate/personal-data fields.
  - [x] Add server tests for approved privileged action, missing justification, malformed rationale/policy, stale timestamp or stale freshness, unauthorized operator, tenant mismatch, cross-tenant projection poison, audit unavailable, unsafe audit evidence, partial operation failure, and policy-blocked denial logging.
  - [x] Add review-history tests for authorized reviewer, unauthorized reviewer, redacted/withheld fields, unavailable audit source, malformed handle, and indistinguishable protected-record behavior.
  - [x] Add safety-net tests proving no privileged action delegate executes before authorization, current freshness, valid justification, and successful audit precondition.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 2.8 evidence after implementation.

## Dev Notes

### Epic and Business Context

- Epic 2 covers governed retention, redaction, and audit. Story 2.8 closes FR54-FR55 by making privileged operational access accountable: justification is required, audit-linked, and reviewable by authorized compliance reviewers. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.8: Record and Review Privileged Operational Justification`]
- This story builds directly on Stories 2.1-2.7. Governance contracts and audit evidence exist; governance mutations fail closed when audit evidence is unavailable; audit-record access/review/export treatment is now tenant-scoped and content-safe. Do not reimplement those mechanisms. [Source: `_bmad-output/implementation-artifacts/2-7-govern-audit-record-access-retention-and-redaction.md`; `_bmad-output/implementation-artifacts/2-5-enforce-audit-pairing-and-audit-unavailable-fail-closed-behavior.md`]
- PRD requirements for this story are FR54 and FR55. Related non-functional constraints are NFR60-NFR61 for privileged operational records, NFR55-NFR58 for content-safe privileged access observability, and NFR65 for privileged-view behavior tests. [Source: `_bmad-output/planning-artifacts/prd.md#Governance And Audit`; `_bmad-output/planning-artifacts/prd.md#Operability And Observability`; `_bmad-output/planning-artifacts/prd.md#Compliance, Retention, And Release Evidence`]
- Local closure evidence is enough for this story. Epic 5 owns signed release manifest aggregation and waiver governance. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Two-level evidence semantics`]

### Current Implementation State

- `GovernanceOperationKind.RecordPrivilegedJustification` and `PrivilegedActionClass` already exist in public governance vocabulary, but there is no dedicated privileged operational justification command/detail/result contract, no server enforcement boundary, and no audit service method for recording privileged justifications. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`; `src/Hexalith.Conversations.Contracts/Governance/GovernanceRequest.cs`; `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`]
- `GovernanceOperationMetadata` already validates tenant, conversation, actor, rationale, policy reference, UTC timestamp, correlation ID, and causation ID, and its `ToString()` omits rationale/policy for safety. Use this pattern instead of inventing looser justification validation. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceOperationMetadata.cs`]
- `GovernanceAuditEvidence` and `GovernanceAuditEvidenceReference` provide safe evidence references and outcome linkage. Use them for privileged justification records; do not expose raw audit persistence details. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidence.cs`; `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`]
- `ConversationGovernanceAuditGate` catches audit-service failures and returns `AuditUnavailable`. Required privileged justification evidence must flow through this gate or an equivalent fail-closed wrapper. [Source: `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`]
- `ConversationAuditRecordAccessService` shows the correct ordering for audit detail reads: tenant authorization first, safe handle parsing second, projection read third, current freshness check, then detail shaping. Follow this ordering for privileged-action review history. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`]
- `ConversationTenantAccessRequirement` has `Read`, `Write`, `Admin`, and `Governance`. Privileged operational access should not use ordinary `Read` as the only gate; classify the operation and choose `Admin` or `Governance` explicitly. [Source: `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessRequirement.cs`]
- `GovernanceAuditPairingSafetyNetTest` currently lists `RecordPrivilegedJustification` as future-only vocabulary. Story 2.8 should update this test so the implemented privileged justification boundary is inventoried without adding false audit requirements to ordinary conversation activity. [Source: `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`]

### Architecture Guardrails

- Tenant authorization fails closed before aggregate load, projection read, admin action, MCP/tool operation, export, verification detail access, rebuild, repair, or any background work that can read, write, rebuild, export, or infer conversation data. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- No privileged path may bypass tenant access projection checks, command availability checks, or content-safe response shaping. [Source: `_bmad-output/planning-artifacts/architecture.md#Security Gate Consistency`]
- EventStore remains the only durable source of truth for conversation state. Projections, exports, UI models, evidence views, and caches are derived and must not become authority. [Source: `_bmad-output/planning-artifacts/architecture.md#EventStore Authority Clarification`]
- Adding a new durable store, cache, index, export artifact, worker queue, evidence artifact, public trust/freshness state, public error taxonomy, privileged execution path, or fail-open degraded behavior requires ADR or approved waiver before implementation. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`; `docs/adrs/index.md`]
- Projection freshness blocking semantics are decided: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; if a story does not explicitly declare accepted states, only `Current` is acceptable for trust-bearing decisions, governance, export, verification, privileged background work, and command eligibility. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- v1 lifecycle/export scope stays narrow. Full evidence bundle export, full retention editor, automatic legal hold, future derived indexes, and broad lifecycle automation are out of v1 unless promoted by ADR and release-scope approval. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle`]

### UX and Future Operator Context

- No UI implementation is required in this story. Backend DTOs must still support future Epic 3 review surfaces where operators inspect audit trails, evidence details, and command gates. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Inspect Redaction Attribution and Governance Audit Trail`; `_bmad-output/planning-artifacts/epics.md#Story 3.5: Preserve Read-Only Compliance Workflows and Safe Command Gates`]
- Future review UI needs server-owned trust states and cannot infer safety from missing fields. Return explicit visible, forbidden, redacted, unavailable, rebuilding, stale, or policy-blocked states. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR29`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR30`]
- Governance forms must collect operator intent only, not tenant/user/token authority. This story's contracts should avoid editable tenant authority fields in future generated/composed surfaces. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR34`]

### Previous Story Intelligence

- Story 2.7 implemented closed audit-record action classification, safe audit-record target keys, audit-record query/detail/result contracts, and `ConversationAuditRecordAccessService`. Reuse those contracts for review where possible and do not reintroduce raw audit sink/location fields. [Source: `_bmad-output/implementation-artifacts/2-7-govern-audit-record-access-retention-and-redaction.md#Completion Notes List`]
- Story 2.7 review fixes are directly relevant: missing audit handles must not produce non-unique keys, query `ToString()` must not echo caller input, and outcome-only requested actions cannot be treated as allowed reads. Apply the same rules to privileged justification handles, query diagnostics, and requested operation classes. [Source: `_bmad-output/implementation-artifacts/2-7-govern-audit-record-access-retention-and-redaction.md#Senior Developer Review (AI)`]
- Story 2.6 established access-before-source-resolution ordering for point-in-time reconstruction and current disclosure policy for historical views. Privileged review history must follow that same ordering. [Source: `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md`]
- Current full validation after Story 2.7 passed `595` tests. Treat that as the baseline to preserve. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md#Story 2.7 Audit Record Governance Evidence`]

### Git Intelligence

- Recent commits show the Epic 2 pattern: additive contracts first, server orchestration after tenant/access checks, audit-pairing gate before governed success, deterministic projection/replay support only where needed, focused tests, then full solution validation.
  - `01f58ae feat(story-2.7): Govern Audit Record Access Retention and Redaction`
  - `eb2f625 feat(story-2.6): Reconstruct Point-in-Time Governance State`
  - `15e7605 feat(story-2.5): Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior`
  - `f9375e1 feat(story-2.4): Redact Message Content with Audit Attribution`
  - `5204e66 feat(story-2.3): Mark Conversation Content as Sensitive`
- Follow that shape for Story 2.8. Avoid implementing broad privileged infrastructure just because the vocabulary names future operation classes.

### File Structure Requirements

- Likely UPDATE files:
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceRequest.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidence.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs` only if existing safe error codes cannot express privileged denial/audit unavailable results.
  - `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessReasonCode.cs` only if existing freshness reasons cannot express privileged review states.
  - `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
  - `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs` only if the review entry point belongs there.
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/GovernanceContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/AuditRecordGovernanceContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- Likely NEW files:
  - `src/Hexalith.Conversations.Contracts/Governance/PrivilegedOperationalActionClass.cs` or equivalent closed operation-class vocabulary.
  - `src/Hexalith.Conversations.Contracts/Governance/PrivilegedOperationalJustificationV1.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/RecordPrivilegedOperationalJustificationCommand.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/PrivilegedOperationalJustificationResult.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/GetPrivilegedOperationalJustificationQuery.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/PrivilegedOperationalJustificationDetailsV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/PrivilegedOperationalJustificationHistoryResult.cs`
  - `src/Hexalith.Conversations.Server/Governance/ConversationPrivilegedOperationalJustificationService.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationPrivilegedJustificationReviewService.cs` if review-history behavior is separated from the enforcement service.
  - `tests/Hexalith.Conversations.Contracts.Tests/PrivilegedOperationalJustificationContractTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationPrivilegedOperationalJustificationServiceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationPrivilegedJustificationReviewServiceTest.cs`
- Keep public contracts in `Contracts`, orchestration/authorization/audit gates in `Server`, deterministic aggregate logic in `Hexalith.Conversations` only when a real conversation-domain mutation is in scope, and tests beside the affected project.

### Testing Requirements

- Run focused tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest"`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` if query registration or projection behavior changes.
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`
- Use existing xUnit v3 and Shouldly patterns. Microsoft Learn documents `dotnet test --filter` expressions with `FullyQualifiedName~...` matching, and NuGet Central Package Management requires project `PackageReference` entries to omit `Version` while versions live in `Directory.Packages.props`. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`; `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Out of Scope

- Do not build a UI privileged-action history drawer, evidence timeline, command gate UI, accessibility workflow, browser-title safety test, clipboard test, or responsive layout in this story.
- Do not create durable export files, evidence bundles, release manifests, background export/rebuild/repair/verification workers, blob storage, queues, database tables, indexes, caches, or MCP/tool actions without accepted ADR or waiver coverage.
- Do not implement full conversation metadata or visibility mutation commands unless they already exist and this story only wraps them with required justification.
- Do not alter existing retention, sensitivity, redaction, audit-record access, point-in-time reconstruction, conversation list/detail, or idempotency success semantics except where privileged justification contracts require safe evidence links.
- Do not expose raw audit sink locations, EventStore stream topology, storage positions as public authority, exception bodies, raw policy internals, provider payloads, Party personal data, tenant claims, tokens, raw conversation content, redacted content, or unauthorized resource existence.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 2.8: Record and Review Privileged Operational Justification`
- `_bmad-output/planning-artifacts/prd.md#Governance And Audit`
- `_bmad-output/planning-artifacts/prd.md#Operability And Observability`
- `_bmad-output/planning-artifacts/prd.md#Compliance, Retention, And Release Evidence`
- `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`
- `_bmad-output/planning-artifacts/architecture.md#EventStore Authority Clarification`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/planning-artifacts/architecture.md#Blocking Freshness Rule`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `_bmad-output/implementation-artifacts/2-7-govern-audit-record-access-retention-and-redaction.md`
- `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md`
- `_bmad-output/implementation-artifacts/2-5-enforce-audit-pairing-and-audit-unavailable-fail-closed-behavior.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `docs/adrs/index.md`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceOperationMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceRequest.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidence.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`
- `src/Hexalith.Conversations.Contracts/Governance/AuditRecordActionClassification.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationAuditRecordQuery.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordResult.cs`
- `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessRequirement.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 69 passed.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest"` - 36 passed.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 97 passed.
- 2026-05-22: `dotnet test Hexalith.Conversations.slnx` - 619 passed.
- 2026-05-22: Review fix validation `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~ConversationQueryRegistrationTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~Projection"` - 119 passed.
- 2026-05-22: Review fix validation `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 69 passed.
- 2026-05-22: Review fix validation `dotnet test Hexalith.Conversations.slnx` - 625 passed.

### Completion Notes List

- Added closed privileged operation-class contracts, structured privileged operational justification command/details/result/query DTOs, and content-safe serialization/`ToString()` safeguards.
- Added the privileged operational enforcement service that gates execution on tenant authorization, current projection freshness, valid structured justification, and required audit evidence through `ConversationGovernanceAuditGate`.
- Added the governed privileged-action review service and query-handler entry point for authorized compliance review with explicit forbidden, unavailable, rebuilding, stale, redacted, and current states.
- Extended the governance audit service boundary for privileged justification records and updated the audit-pairing safety net so the boundary is inventoried without making ordinary conversation commands audit-dependent.
- Added focused Story 2.8 contract, server enforcement, review-history, and safety-net tests; full solution validation passed with 619 tests.

### File List

- `_bmad-output/implementation-artifacts/2-8-record-and-review-privileged-operational-justification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Governance/PrivilegedOperationalJustificationV1.cs`
- `src/Hexalith.Conversations.Contracts/Governance/RecordPrivilegedOperationalJustificationCommand.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetPrivilegedOperationalJustificationQuery.cs`
- `src/Hexalith.Conversations.Contracts/Queries/PrivilegedOperationalJustificationDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/PrivilegedOperationalJustificationResult.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationPrivilegedOperationalJustificationService.cs`
- `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
- `src/Hexalith.Conversations.Server/Governance/PrivilegedOperationalActionOutcome.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationPrivilegedJustificationReviewService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs`
- `src/Hexalith.Conversations.Server/Queries/IPrivilegedOperationalJustificationReviewSource.cs`
- `src/Hexalith.Conversations.Server/Queries/UnavailablePrivilegedOperationalJustificationReviewSource.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/PrivilegedOperationalJustificationContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationPrivilegedOperationalJustificationServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationPrivilegedJustificationReviewServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryRegistrationTest.cs`

## Senior Developer Review (AI)

### Review Outcome

Approved after automatic fixes. No critical issues remain.

### Findings and Fixes

- HIGH: `ConversationPrivilegedOperationalJustificationService` authorized privileged actions with the correlation ID as the caller principal. Fixed by requiring a trusted `callerPrincipalId` parameter and passing that to `IConversationTenantAccessService`; tests now prove the caller binding.
- HIGH: Partial, denied, and throwing privileged delegates were returned against precondition audit evidence recorded as `Succeeded`. Fixed by recording a final outcome audit for non-successful delegate outcomes and converting unexpected delegate exceptions into content-safe, audited `PolicyBlocked` results.
- MEDIUM: `ConversationPrivilegedJustificationReviewService` returned details even when the review source supplied stale/rebuilding/unavailable freshness. Fixed by failing closed with explicit non-current result states and no details.
- MEDIUM: `AddConversationQueries()` registered the privileged review service without a default `IPrivilegedOperationalJustificationReviewSource`, which could break handler resolution for hosts that had not yet configured a durable source. Fixed with a fail-closed unavailable review source and registration coverage.

### Validation Checklist

- [x] Story status verified as reviewable before review.
- [x] Acceptance Criteria and completed tasks cross-checked against implementation.
- [x] Story File List compared with git changes and updated for review-created files.
- [x] Code quality, security/fail-closed behavior, and test quality reviewed for changed source files.
- [x] Official Microsoft `dotnet test --filter` documentation consulted for focused validation syntax.
- [x] Review notes appended, Change Log updated, status set to `done`, and sprint status synced.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Critical source documents loaded: sprint status, Epic 2 story requirements, PRD requirement mapping, architecture guardrails, UX requirement map, readiness gates and decisions, project context, Story 2.7, Story 2.6, Story 2.5, recent git history, current governance contracts, audit evidence contracts, audit gate/service, audit-record query/review service, query handler, tenant access requirement vocabulary, audit safety-net tests, test evidence, ADR index, and official Microsoft documentation for `dotnet test --filter` and NuGet Central Package Management.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code gaps, likely update/new files, previous story learnings, tenant/freshness/audit/EventStore guardrails, ADR stop conditions, focused test requirements, and explicit out-of-scope boundaries.
- Checklist fixes applied in YOLO mode: blocked new durable privileged stores/workers/export artifacts without ADR coverage, required tenant authorization before target/audit-handle/source resolution, required current freshness by default, moved `RecordPrivilegedJustification` out of vague future vocabulary into an explicit audit-boundary task, and separated backend review contracts from future Epic 3 UI work.

## Change Log

- 2026-05-22: Implemented Story 2.8 privileged operational justification contracts, enforcement boundary, audit-service extension, governed review query behavior, safety-net tests, and local evidence. Status moved to review after full solution validation.
- 2026-05-22: Senior developer review fixed trusted caller authorization, non-success outcome audit linkage, exception-safe delegate handling, stale review evidence blocking, and fail-closed query DI registration. Status moved to done after full solution validation.
- 2026-05-22: Created Story 2.8 context from Epic 2 requirements, PRD/architecture/UX/readiness/project context, previous Story 2.7/2.6/2.5 learnings, current governance/audit/query code, recent git history, ADR status, and Microsoft .NET/NuGet documentation.
