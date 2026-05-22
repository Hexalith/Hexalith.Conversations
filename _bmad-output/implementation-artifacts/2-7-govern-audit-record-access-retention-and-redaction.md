# Story 2.7: Govern Audit Record Access, Retention, and Redaction

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance owner,
I want audit records to have explicit access, retention, export, and redaction behavior,
so that audit evidence remains reviewable without becoming an unmanaged disclosure surface.

## Acceptance Criteria

1. Audit-record actions are explicitly classified
   - Given audit records are created for governance and privileged actions,
   - When audit access policy is evaluated,
   - Then the system can classify audit-record actions as allowed, denied, redacted, exported, separately logged, or policy-blocked,
   - And each action remains tenant-scoped and actor-attributed.

2. Audit evidence has governed retention and redaction treatment
   - Given retention and redaction policy applies to governance audit records,
   - When audit records are projected, exported, viewed, or rebuilt,
   - Then the system applies the approved retention and redaction treatment for audit evidence,
   - And audit handling remains distinguishable from conversation message redaction and source event history.

3. Unauthorized audit read and export attempts are content-safe
   - Given an unauthorized or insufficiently scoped user requests audit details,
   - When the audit read or export boundary evaluates access,
   - Then the response is content-safe and does not leak protected tenant, Party, conversation, policy, redacted content, or operational details,
   - And access denial itself is auditable where policy requires.

4. Redacted or partially withheld audit records remain reviewable
   - Given an audit record is redacted or partially withheld,
   - When an authorized reviewer inspects the record,
   - Then the visible audit view preserves actor, timestamp, action class, outcome, policy basis, and rationale where allowed,
   - And withheld fields are represented with safe redaction or unavailable states.

5. Audit-record governance tests cover disclosure and mutation safety
   - Given audit-record governance tests run,
   - When allowed access, denied access, export, redaction, retention expiry, tamper attempt, tenant mismatch, and rebuild scenarios are exercised,
   - Then tests prove tenant isolation, citeable audit evidence, policy treatment, redaction safety, and no silent audit mutation paths.

## Tasks / Subtasks

- [x] Define public audit-record governance contracts and closed vocabularies (AC: 1, 2, 4)
  - [x] Add audit-record action classification vocabulary under `src/Hexalith.Conversations.Contracts/Governance/`, with only: `Allowed`, `Denied`, `Redacted`, `Exported`, `SeparatelyLogged`, and `PolicyBlocked`.
  - [x] Add a contract that describes audit-record policy treatment without implying source-event deletion, for example retention state, redaction state, access decision, export eligibility, separate-log requirement, and safe next action.
  - [x] Add a safe audit-record target shape that can identify `AuditEvidenceHandle` without overloading `GovernanceTarget.SegmentReference`; update `GovernanceTarget.ToTargetKey()` so `GovernedTargetKind.AuditRecord` does not fall through to `unsupported:AuditRecord`.
  - [x] Ensure contracts carry schema version, tenant scope, conversation identity or governed scope, actor attribution, timestamp, policy reference, action class, outcome, rationale class, correlation/causation metadata, and citeable audit handle where allowed.
  - [x] Do not add raw audit sink identifiers, storage locations, EventStore stream/position topology, exception bodies, policy internals, message text, redacted text, Party personal data, provider payload, or raw upstream details.

- [x] Add query/result contracts for authorized audit-record review (AC: 1, 3, 4)
  - [x] Add query/result DTOs under `src/Hexalith.Conversations.Contracts/Queries/`, such as `GetConversationAuditRecordQuery`, `ConversationAuditRecordResult`, and `ConversationAuditRecordDetailsV1`.
  - [x] Return `Visible`, `Hidden`, `Redacted`, `Unavailable`, or `PolicyBlocked` states using existing `ProjectionTrustState` and `ProjectionFreshnessReasonCode` where possible; add public states only if a contract test proves existing vocabulary cannot express the requirement.
  - [x] Include visible fields only when policy allows: actor, timestamp, action class, outcome, safe policy basis, content-safe rationale class, audit handle, governed target kind, and freshness/confidence metadata.
  - [x] Preserve hidden/forbidden behavior for unauthorized, cross-tenant, malformed-handle, stale-projection, unavailable-source, and tamper scenarios without differentiating protected record existence.

- [x] Implement an audit-record access policy boundary in server code (AC: 1, 3, 4)
  - [x] Add a focused service under `src/Hexalith.Conversations.Server/Governance/` or `src/Hexalith.Conversations.Server/Queries/` that evaluates audit-record access before resolving the target record or audit source.
  - [x] Reuse `IConversationTenantAccessService` and require tenant authorization before audit-handle parsing, current projection read, temporal reconstruction, source lookup, export shaping, or differentiated failure response.
  - [x] Reuse existing audit evidence contracts (`GovernanceAuditEvidence`, `GovernanceAuditEvidenceReference`, `AuditEvidenceHandle`) and existing read freshness vocabulary rather than introducing a separate audit-state language.
  - [x] Treat access denials as auditable when policy requires, but do not create a recursive unaudited governance mutation. If denial logging requires a durable record, route through the same audit gate or explicitly return `PolicyBlocked` until ADR coverage exists.

- [x] Apply retention/redaction treatment to derived audit views and rebuild paths (AC: 2, 4)
  - [x] Add a derived projection/read model for audit-record details only if it is rebuildable from EventStore governance events plus approved audit evidence references.
  - [x] Ensure audit-record redaction is distinct from conversation message redaction: audit views may withhold policy-sensitive fields while preserving citeable metadata; message redaction suppresses governed content from timeline/detail surfaces.
  - [x] Retention expiry must not silently delete source history. Without an accepted ADR for irreversible deletion, return an expired/withheld derived view and preserve safe evidence handle behavior where policy allows.
  - [x] Preserve current redaction enforcement from Story 2.6: historical or rebuilt audit views must not reveal content suppressed by current disclosure policy.

- [x] Add export classification without creating an unmanaged export surface (AC: 1, 2, 3, 5)
  - [x] Support an `Exported` action classification and a content-safe export result for allowed in-memory/API responses.
  - [x] Do not create durable export files, blob storage, queues, background export workers, evidence bundles, signed artifacts, or derived indexes in this story unless `docs/adrs` has an accepted ADR or explicit waiver for the lifecycle.
  - [x] Export results must include only safe audit fields and must carry tenant scope, freshness/confidence state, policy treatment, audit handle, and safe next action.
  - [x] Denied export must be indistinguishable from unavailable/hidden protected records where disclosure policy requires.

- [x] Integrate with current query/server patterns without changing governance command behavior (AC: 1-5)
  - [x] Add the audit-record query entry point to `ConversationQueryHandler` or a focused sibling handler if the dependency graph would otherwise make `ConversationQueryHandler` too broad.
  - [x] Do not change existing retention, sensitivity, redaction, or temporal reconstruction command/query success semantics except where audit-record contracts require safe references to existing evidence.
  - [x] Do not bypass `ConversationGovernanceAuditGate`, `IConversationGovernanceAuditService`, or the Story 2.5 audit-pairing safety net for any audit-record governance mutation.
  - [x] Keep all public contracts in `Contracts`, orchestration and authorization in `Server`, deterministic replay/materialization in the domain/server projection boundary, and tests beside the affected project.

- [x] Add focused tests and local evidence (AC: 1-5)
  - [x] Add contract tests for audit-record action vocabulary, target key generation, JSON shape, `ToString()` safety, unsupported vocabulary rejection, and forbidden public substrate fields.
  - [x] Add server tests for allowed read, denied read, denied export, policy-blocked export, redacted/withheld detail, stale or rebuilding projection, malformed audit handle, cross-tenant handle/target, and audit-source unavailability.
  - [x] Add projection/rebuild tests proving audit-record derived views preserve citeable metadata, apply retention/redaction treatment, distinguish audit redaction from message redaction, and do not reintroduce withheld fields after rebuild.
  - [x] Add safety-net coverage for tamper attempts and audit-record governance mutations so no path silently changes audit treatment without paired audit evidence or an explicit policy-blocked result.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 2.7 evidence after implementation.

## Dev Notes

### Epic and Business Context

- Epic 2 covers governed retention, redaction, and audit. Story 2.7 closes FR51-FR53 by making audit evidence itself governed: citeable, policy-treated, access-controlled, export-classified, and redaction-safe. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.7: Govern Audit Record Access, Retention, and Redaction`]
- Story 2.7 builds on Stories 2.1-2.6. Existing governance mutations already emit audit evidence, fail closed when audit is unavailable, and support point-in-time reconstruction under current disclosure policy. This story must govern the audit-record surface, not reimplement retention/redaction commands. [Source: `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md`; `_bmad-output/implementation-artifacts/2-5-enforce-audit-pairing-and-audit-unavailable-fail-closed-behavior.md`]
- Story 2.8 covers structured privileged operational justification. Do not implement privileged justification workflows here except for preserving extension points and `RecordPrivilegedJustification` compatibility. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.8: Record and Review Privileged Operational Justification`]

### Current Implementation State

- `GovernanceOperationKind.GovernAuditRecord` and `GovernedTargetKind.AuditRecord` already exist, but there is no action-classification vocabulary for `Allowed`, `Denied`, `Redacted`, `Exported`, `SeparatelyLogged`, or `PolicyBlocked`. Story 2.7 should add explicit vocabulary instead of reusing `GovernanceOutcome` for read/export actions. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`]
- `GovernanceTarget.ToTargetKey()` currently handles conversation, message, file, participant, and content segment. `AuditRecord` falls through to `unsupported:{Kind.Value}`, which is unsafe for audit-record policy treatment and tests should catch it. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`]
- `GovernanceAuditEvidenceReference` exposes a safe audit handle, policy reference, and captured timestamp; `GovernanceAuditEvidence` pairs that reference with operation metadata, target, outcome, and remediation. Use these existing contracts as inputs for audit-record views. Do not invent raw audit sink IDs or storage references. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`; `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidence.cs`]
- `IConversationGovernanceAuditService` currently records audit evidence for retention, sensitivity, and redaction mutations only. It does not yet expose audit-record read, export, denial logging, retention treatment, or audit-record redaction behavior. [Source: `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`]
- `ConversationGovernanceAuditGate` converts audit recording failures to `AuditUnavailable`, preserving fail-closed governance behavior. Any new audit-record mutation or denial logging path must use the same fail-closed posture or return policy-blocked until the audit path can prove evidence. [Source: `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`]
- `ConversationQueryHandler` currently exposes current detail, list, and point-in-time detail flows. It authorizes through projection read services and delegates temporal reconstruction to `ConversationTemporalReconstructionService`; it has no audit-record read/export entry point. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`]
- `ConversationTemporalReconstructionService` authorizes tenant access before cursor/source resolution, applies current redaction policy to historical views, and returns hidden/unavailable/rebuilding outcomes without leaking protected existence. Follow this ordering for audit-record access. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`]
- `ConversationProjectionMaterializer` already projects retention, sensitivity, and message redaction state into conversation details and suppresses redacted message text. Story 2.7 may extend projections for audit-record views only if the new derived state is rebuildable and does not become authority. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`; `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs`]

### Architecture Guardrails

- EventStore history remains the v1 source of truth. Audit-record projections, export responses, evidence views, and caches are derived and must be rebuildable. Derived state disagreement with replayed EventStore/audit evidence must surface stale, invalid, quarantined, rebuilding, or unavailable state. [Source: `_bmad-output/planning-artifacts/architecture.md#EventStore Authority Clarification`]
- Tenant authorization must fail closed before audit-record read, export, rebuild, verification, admin, tool, background job, or source lookup. Do not parse or resolve audit handles in a way that reveals record existence before tenant authorization. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/planning-artifacts/architecture.md#Process Patterns`]
- Redaction is append-only, policy-governed, and auditable by default. Original source events are not rewritten unless a legal/compliance ADR explicitly authorizes irreversible source-event redaction. [Source: `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`]
- Export, durable evidence artifacts, derived indexes, retention, deletion, tombstoning, and legal-hold lifecycle behavior are ADR-triggered. Story 2.7 can classify and safely shape export responses, but durable export artifacts require accepted ADR coverage or a waiver. [Source: `docs/adrs/index.md`; `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]
- Public APIs must expose Conversations contracts, typed results, freshness/trust state, and safe errors; they must not expose EventStore envelopes, stream names, actor IDs as substrate concepts, snapshot internals, raw projection topology, audit store paths, or implementation exceptions. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Observability and diagnostics must be metadata-only and bounded-cardinality. Audit-record denials, exports, tamper attempts, and policy-blocked outcomes must not log protected content, raw policy internals, unauthorized tenant IDs, Party personal data, or redacted values. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/architecture.md#Metadata-Only Definition`]

### UX and Future Operator Context

- No UI implementation is required in this story. Backend DTOs must still support future Epic 3 inline audit trail, evidence detail drawer, citation copy, and governance verification surfaces. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Inspect Redaction Attribution and Governance Audit Trail`; `_bmad-output/planning-artifacts/ux-requirement-map.md`]
- Future audit UI entries need timestamp, actor, action, outcome, policy basis, rationale where allowed, evidence anchors, and independent authorization from the parent timeline. Return those fields from server-owned contracts rather than expecting the UI to infer them. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Inspect Redaction Attribution and Governance Audit Trail`]
- Safe denied, unavailable, redacted, stale, rebuilding, and no-access states must be explicit. Absence must not imply authorization, freshness, successful hydration, or safety. [Source: `_bmad-output/planning-artifacts/architecture.md#Format Patterns`; `_bmad-output/planning-artifacts/ux-requirement-map.md`]

### Previous Story Intelligence

- Story 2.6 implemented point-in-time reconstruction contracts, redaction projection DTOs, deterministic governance replay support, and a server reconstruction service that applies current disclosure policy to historical state. Carry forward the same access-before-source-resolution ordering. [Source: `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md#Completion Notes List`]
- Story 2.6 review fixes are directly relevant: safe-position/cursor anchors beyond retained coverage must fail closed; incomplete temporal sources must not return authoritative details; redaction state must suppress later materialized message text. Apply the same mindset to audit-record retained coverage, incomplete audit sources, and audit-view rebuilds. [Source: `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md#Senior Developer Review (AI)`]
- Current full validation after Story 2.6 passed `572` tests. Treat that as the baseline to preserve. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md#Story 2.6 Point-in-Time Governance Reconstruction Evidence`]

### Git Intelligence

- Recent commits show the Epic 2 pattern: additive contract-first implementation, server orchestration after tenant/idempotency checks, deterministic aggregate/replay/projection support, focused tests, and full-solution validation.
  - `eb2f625 feat(story-2.6): Reconstruct Point-in-Time Governance State`
  - `15e7605 feat(story-2.5): Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior`
  - `f9375e1 feat(story-2.4): Redact Message Content with Audit Attribution`
  - `5204e66 feat(story-2.3): Mark Conversation Content as Sensitive`
  - `229e1fa feat(story-2.2): Set Conversation Retention Policy with Rationale`
- Follow that shape for Story 2.7: contracts first, server policy boundary second, projection/rebuild behavior only where needed, tests in the same story, then update local evidence.

### File Structure Requirements

- Likely UPDATE files:
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidence.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs` only if current details need safe audit-record references for future inline trail support.
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs` only if existing hidden/authorization/audit/versioning codes cannot express audit-record failures safely.
  - `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessReasonCode.cs` only if existing reason codes cannot express audit-record retention/redaction/access states.
  - `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs` only if denial logging or audit-record governance requires an audit service extension.
  - `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs` only if audit-record derived views are materialized from event streams.
  - `tests/Hexalith.Conversations.Contracts.Tests/GovernanceContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- Likely NEW files:
  - `src/Hexalith.Conversations.Contracts/Governance/AuditRecordAccessPolicyV1.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/AuditRecordActionClassification.cs`
  - `src/Hexalith.Conversations.Contracts/Governance/AuditRecordPolicyTreatmentV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/GetConversationAuditRecordQuery.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordDetailsV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordResult.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/AuditRecordGovernanceContractTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAuditRecordProjectionTest.cs` if projection materialization is added.
- Keep durable artifact creation out of this story unless ADR coverage exists. A new in-memory result DTO is acceptable; a new durable export/evidence file, queue, worker, blob, table, index, or cache is not.

### Testing Requirements

- Run focused tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest"`
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` if projection materialization changes.
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`
- Use existing xUnit v3 and Shouldly patterns. Microsoft Learn documents `dotnet test --filter` expressions with `FullyQualifiedName~...` matching, and NuGet Central Package Management requires versions in `Directory.Packages.props` while project `PackageReference` entries omit `Version`. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`; `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Out of Scope

- Do not implement Story 2.8 privileged operational justification workflows.
- Do not create a UI audit drawer, evidence timeline, citation-copy UI, accessibility tests, or browser/clipboard tests; these belong to Epic 3 unless promoted.
- Do not create durable export artifacts, signed evidence bundles, release manifests, background export workers, derived indexes, blob stores, queues, or cache authorities without accepted ADR or waiver coverage.
- Do not implement cryptographic redaction, source-event deletion, irreversible audit deletion, legal-hold release workflows, full retention automation, or cross-module compliance automation.
- Do not expose raw audit sink locations, EventStore stream topology, raw positions as public authority, exception bodies, storage paths, provider payloads, Party personal data, raw policy internals, or redacted message text.
- Do not allow audit-record read/export paths to bypass tenant access, projection freshness, redaction policy, or audit-pairing rules.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 2.7: Govern Audit Record Access, Retention, and Redaction`
- `_bmad-output/planning-artifacts/prd.md#Functional Requirements`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/planning-artifacts/prd.md#Data Integrity And Event Sourcing`
- `_bmad-output/planning-artifacts/architecture.md#EventStore Authority Clarification`
- `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`
- `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`
- `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `_bmad-output/implementation-artifacts/2-6-reconstruct-point-in-time-governance-state.md`
- `_bmad-output/implementation-artifacts/2-5-enforce-audit-pairing-and-audit-unavailable-fail-closed-behavior.md`
- `docs/adrs/index.md`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidence.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`
- `src/Hexalith.Conversations.Server/Governance/IConversationGovernanceAuditService.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - passed, 63 tests.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest"` - passed, 46 tests.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - passed, 69 tests.
- `dotnet test Hexalith.Conversations.slnx` - passed, 595 tests.

### Completion Notes List

- Added closed audit-record action classification contracts and policy-treatment DTOs using existing trust/freshness vocabularies.
- Added safe audit-record target support with `AuditEvidenceHandle` and deterministic `audit:{handle}` target keys.
- Added audit-record query/detail/result contracts for governed review and in-memory export responses.
- Added `ConversationAuditRecordAccessService` and `ConversationQueryHandler.GetAuditRecordAsync()` so tenant authorization happens before audit handle parsing or projection reads.
- Added query handler coverage proving `GetAuditRecordAsync()` reaches the governed audit-record read boundary.
- Implemented content-safe hidden, unavailable, rebuilding, redacted/withheld, exported, and policy-blocked audit-record outcomes without creating durable export artifacts or changing governance command behavior.
- Added contract/server coverage for vocabulary, JSON shape, public substrate safety, allowed/denied access, denied export, policy-blocked export, redaction/retention treatment, stale/rebuild states, malformed handles, cross-tenant poison, source unavailability, rebuild preservation, and mutation attempts.
- Review fixed audit-record target key validation so missing handles cannot produce the non-unique `audit:` key.
- Review fixed audit-record query diagnostics so `ToString()` does not echo caller input or raw audit-handle text.
- Review fixed audit-record requested-action handling so outcome-only classes such as `Denied` and `Redacted` cannot be treated as allowed reads.

### File List

- `_bmad-output/implementation-artifacts/2-7-govern-audit-record-access-retention-and-redaction.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Governance/AuditRecordActionClassification.cs`
- `src/Hexalith.Conversations.Contracts/Governance/AuditRecordPolicyTreatmentV1.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationAuditRecordQuery.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/AuditRecordGovernanceContractTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs`

## Senior Developer Review (AI)

### Review Summary

- Reviewer: GPT-5 Codex on 2026-05-22
- Story status before review: review
- Story status after review: done
- MCP documentation reference checked: Microsoft Learn `dotnet test --filter` / selective unit tests.
- Git/story File List comparison: no source-file discrepancies found; changed source/test files are represented in the story File List.

### Findings Fixed

- [x] [High] `GovernanceTarget.ToTargetKey()` allowed an `AuditRecord` target without `AuditEvidenceHandle`, producing the non-unique key `audit:` instead of failing closed. Fixed by throwing when an audit-record target lacks the safe audit evidence handle and added regression coverage in `AuditRecordGovernanceContractTest`.
- [x] [Medium] `GetConversationAuditRecordQuery.ToString()` used record default output and could echo caller-supplied raw audit-handle text if a malformed query object was logged. Fixed with a content-safe `ToString()` override and regression coverage.
- [x] [Medium] `ConversationAuditRecordAccessService` treated requested outcome classes `Denied` and `Redacted` as allowed reads. Fixed by policy-blocking outcome-only requested actions and adding server regression coverage.

### Checklist Validation

- [x] Story file loaded and status verified as reviewable.
- [x] Acceptance Criteria and completed tasks cross-checked against source and tests.
- [x] File List validated against git status/diff output.
- [x] Architecture/project context loaded, including tenant fail-closed, projection freshness, EventStore authority, and metadata-only rules.
- [x] Contract, server, projection/rebuild, security, and disclosure-safety review performed on changed files.
- [x] All confirmed issues auto-fixed; no critical issues remain.
- [x] Focused and full solution tests passed after fixes.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Critical source documents loaded: sprint status, Epic 2 story requirements, PRD requirement mapping, architecture guardrails, UX requirement map, project context, Story 2.6, Story 2.5, recent git history, current governance contracts, audit evidence contracts, audit gate/service, query handler, temporal reconstruction service, projection materializer, contract samples, test evidence, ADR index, and official Microsoft documentation for `dotnet test --filter` and NuGet Central Package Management.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code gaps, likely update/new files, previous story learnings, tenant/redaction/audit/EventStore guardrails, ADR stop conditions, focused test requirements, and explicit out-of-scope boundaries.
- Checklist fixes applied in YOLO mode: called out the existing `AuditRecord` target-key gap, blocked durable export/artifact creation without ADR coverage, required tenant authorization before audit-handle/source resolution, and separated audit-record redaction from message redaction.

## Change Log

- 2026-05-22: Created Story 2.7 context from Epic 2 requirements, PRD/architecture/UX/project context, previous Story 2.6 and Story 2.5 learnings, current governance/audit/query/projection code, recent git history, ADR status, and Microsoft .NET/NuGet documentation.
- 2026-05-22: Implemented governed audit-record access, retention/redaction treatment, safe in-memory export classification, server policy boundary, focused tests, and validation evidence.
- 2026-05-22: Completed senior developer review, fixed audit-record target-key validation, query diagnostic safety, requested-action policy blocking, and refreshed validation evidence.
