# Story 2.1: Define Governance Policy and Audit Contracts

Status: done

## Story

As a compliance integrator,
I want explicit governance and audit contracts for retention, sensitivity, redaction, and privileged actions,
so that governance behavior is enforceable, testable, and safe before mutation workflows are implemented.

## Acceptance Criteria

1. Governance contract coverage
   - Given governance contracts are added,
   - When retention, sensitivity, redaction, archival, legal-hold deferral, and privileged-action concepts are modeled,
   - Then each contract includes tenant scope, conversation identity, actor attribution, rationale, policy reference, timestamp, schema version, and correlation/causation metadata,
   - And contracts avoid raw message content, Party personal data, provider payloads, and unauthorized upstream details.

2. Audit evidence pairing shape
   - Given audit contracts are added,
   - When a governance mutation contract is defined,
   - Then a corresponding audit evidence shape exists for the same operation,
   - And the contract can represent success, denial, audit-unavailable failure, and policy-blocked outcomes.

3. Governance state semantics
   - Given governance state is projected or displayed later,
   - When redaction, retention, and legal-hold semantics are documented,
   - Then the contracts distinguish event history, projected/displayed content, audit records, derived materializations, archival, logical deletion, retention enforcement, and legal-hold deferral,
   - And they do not imply irreversible source-event deletion unless an approved ADR exists.

4. Contract serialization and validation evidence
   - Given contract tests run,
   - When governance and audit contract payloads are serialized and validated,
   - Then required rationale, actor, tenant, policy, schema version, and correlation fields are enforced,
   - And forbidden content and personal-data fields are absent from contract shapes.

5. Safe contract authority and disclosure boundaries
   - Given governance and audit contracts are public adopter-facing surfaces,
   - When operation timestamps, actor attribution, denial outcomes, audit-unavailable outcomes, policy-blocked outcomes, legal-hold deferrals, and audit evidence handles are serialized,
   - Then externally visible values are bounded, schema-versioned, content-safe, and derived from explicit Conversations contract inputs rather than handler, projection, storage, audit sink, exception, provider, or upstream internals,
   - And actor, policy, evidence, correlation, and causation identifiers do not reveal Party personal data, protected tenant existence, audit storage locations, raw diagnostics, or provider-owned details.

## Tasks / Subtasks

- [x] Add governance contract vocabulary under `src/Hexalith.Conversations.Contracts/Governance` (AC: 1, 3)
  - [x] Define bounded value objects or closed vocabularies for governance operation kind, governed target kind, retention action, sensitivity category, redaction category, archival state, legal-hold deferral, policy-blocked outcome, and privileged action class.
  - [x] Keep names in Conversations language. Do not expose storage, EventStore, projection topology, tenant implementation, raw upstream, or provider mechanics in public type/property names.
  - [x] Reuse existing `TenantId`, `ConversationId`, `MessageId`, `PartyId`, `FileId`, `SchemaVersion`, and string validation patterns instead of inventing parallel identifier types.

- [x] Add governance command/evidence metadata contracts (AC: 1, 2, 4)
  - [x] Introduce a shared governance metadata record that carries schema version, tenant ID, conversation ID, actor Party ID, rationale, policy reference, operation timestamp, correlation ID, and optional causation ID.
  - [x] Preserve compatibility with existing `ConversationCommandMetadata` and `ConversationEventMetadata`; prefer composition or obvious mapping over duplicating unrelated command metadata fields.
  - [x] Add safe audit handle/evidence reference contracts without exposing audit storage implementation details, persistence sequence numbers, logs, exceptions, or raw sink names.
  - [x] Keep command metadata, governance operation metadata, and audit evidence metadata semantically distinct; do not let idempotency keys, storage sequence values, projection checkpoints, or handler diagnostics become public audit evidence identity.
  - [x] Treat operation timestamps as explicit contract evidence with validation and UTC-safe serialization expectations; tests must reject default/min/max or caller-upgraded trust values that would make stale or unaudited activity look authoritative.

- [x] Add contract shapes for future Epic 2 mutation workflows (AC: 1, 2, 3)
  - [x] Model request/event/evidence shapes needed by Stories 2.2-2.8: set/replace retention policy, mark content sensitive, redact message content, archive/logically delete or close governance state where in scope, legal-hold deferral, audit-record governance, and privileged operational justification.
  - [x] Include explicit result/evidence states for success, denial, audit unavailable, and policy blocked; implementation should bind these to stable contract vocabulary such as `Succeeded`, `Denied`, `AuditUnavailableFailed`, and `PolicyBlocked` or document an equivalent domain-safe naming choice.
  - [x] Separate public outcome vocabulary from internal diagnostics; contracts may expose bounded retryability or remediation classes only when they do not reveal target existence, audit sink state, policy internals, upstream details, exception text, or cross-tenant facts.
  - [x] Add matrix-style sample/test coverage proving every governance mutation kind has paired audit evidence for each required result/evidence state.
  - [x] Do not implement command handlers, aggregate mutation methods, audit sinks, projection materializers, UI components, or source-event deletion behavior in this story.

- [x] Update serialization registration and curated samples (AC: 4)
  - [x] Extend contract sample fixtures so every new public record is covered by `ContractSamples.AllContracts`.
  - [x] Add stable representative JSON fixtures for governance metadata, a governance command contract, a governance event/evidence contract, and a denied/audit-unavailable outcome.
  - [x] Add any required `System.Text.Json` converters only when existing converter patterns cannot cover the new type safely.

- [x] Add focused contract validation tests (AC: 1, 2, 4)
  - [x] Required-field tests cover rationale, actor Party ID, tenant ID, conversation ID, policy reference, schema version, timestamp, and correlation ID.
  - [x] Uniform metadata validation covers null, empty, whitespace, default timestamp, malformed correlation, and omitted tenant scope cases across both command and audit evidence contracts; privileged actions must not infer tenant scope from conversation identity alone.
  - [x] Forbidden-surface tests prove no raw message content, Party personal data, provider payloads, upstream detail objects, EventStore terms, storage terms, exception details, tokens, or claims appear in public type names, property names, JSON property names, or curated sample payloads.
  - [x] Forbidden-surface tests also prove no server/runtime package concepts, provider SDK type names, handler/projection names, audit sink names, storage locations, stream/revision details, or raw diagnostics appear in public contracts or fixtures.
  - [x] Serialization round-trip tests use `System.Text.Json` web defaults and follow the existing JSON-equivalence style.
  - [x] `ToString()`, XML documentation examples, sample fixture names, assertion failure messages, and validation error strings must remain content-safe and must not echo rationale text, policy details, raw actor values beyond approved identifiers, exception content, or unsafe free-text values.
  - [x] Add a dependency-boundary test or build assertion proving `Hexalith.Conversations.Contracts` does not reference server/runtime assemblies, EventStore client/runtime packages, provider SDKs, storage libraries, handler assemblies, projection assemblies, or UI packages.

- [x] Add documentation-ready developer notes in XML docs where public contracts would otherwise be ambiguous (AC: 3, 4)
  - [x] State that redaction is append-only and policy governed by default.
  - [x] State that event history, projected/displayed content, audit records, derived materializations, archival, logical deletion, retention enforcement, and legal-hold deferral are distinct concepts.
  - [x] State that irreversible source-event deletion is out of scope unless a future approved ADR explicitly authorizes it.

## Dev Notes

### Source Foundation

- Epic 2 is the governed retention, redaction, and audit epic. Its purpose is to let authorized users apply retention, sensitivity, redaction, archival, and privileged-action governance with paired audit evidence and fail-closed audit behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Governed Retention, Redaction, and Audit`]
- Story 2.1 covers contract definitions only. It prepares the public vocabulary and evidence shapes that later mutation stories consume. Do not implement Story 2.2 retention mutation, Story 2.3 sensitivity mutation, Story 2.4 redaction mutation, Story 2.5 audit enforcement, or Story 2.6-2.8 workflows here. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.1: Define Governance Policy and Audit Contracts`]
- Requirements covered: FR42-FR49 and FR51-FR53. These include retention policy, sensitivity, redaction, audit pairing, audit-unavailable fail-closed behavior, audit citation, and audit-record governance. [Source: `_bmad-output/planning-artifacts/epics.md#Requirements Inventory`]

### Architecture Constraints

- EventStore remains the only write authority. This story must not add direct transcript, governance, audit, or projection storage. New durable state or bypass paths require an ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Contracts expose Conversations concepts and hide substrate mechanics. Public APIs must not expose EventStore envelopes, stream names, snapshots, aggregate substrate identity, subscription internals, raw projection internals, storage details, or raw exception data. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Governance mutations require paired audit/domain evidence and fail closed when audit recording is unavailable. This story defines contract shapes for that invariant; Story 2.5 enforces the runtime boundary. [Source: `_bmad-output/planning-artifacts/architecture.md#Governance Security`]
- Redaction is append-only, policy governed, and auditable by default. Original source events remain governed by storage policy unless a legal/compliance ADR authorizes irreversible source-event redaction. [Source: `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`]
- Tenant isolation and privacy are separate axes. Contracts may contain stable IDs required for attribution and evidence, but must not embed Party personal data, display names, contact channels, provider-owned session authority, raw upstream records, message text, redacted content, or file binaries. [Source: `_bmad-output/project-context.md#Critical Implementation Rules`]

### Existing Code Patterns To Reuse

- Existing public contract records live under `src/Hexalith.Conversations.Contracts` and use sealed records with XML documentation, constructor validation, nullable enabled code, and `System.Text.Json` web-default serialization tests.
- Reuse these existing contract primitives:
  - `Commands/ConversationCommandMetadata.cs` for schema version, tenant, actor, correlation, causation, and idempotency conventions.
  - `Events/ConversationEventMetadata.cs` for event metadata conventions and timestamp validation.
  - `Identifiers/TenantId.cs`, `ConversationId.cs`, `PartyId.cs`, `MessageId.cs`, and `FileId.cs` for stable references.
  - `Versioning/SchemaVersion.cs` for public schema versioning.
  - `Errors/ConversationError*.cs` for typed, sanitized failure vocabulary.
- Existing tests to extend:
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs`

### Contract Shape Guidance

- Prefer one shared metadata/value shape for governance operations rather than repeating tenant, actor, policy, rationale, timestamp, schema, and correlation fields across every command/evidence record.
- Rationale must be a required, non-empty, content-safe string. Policy reference must be required, non-empty, and safe to return to adopters or auditors. Correlation ID must remain required.
- Timestamps must be `DateTimeOffset` and validated with the same plausible business range pattern used by `ConversationEventMetadata`.
- Timestamps, actor attribution, and evidence handles must not imply runtime success by themselves. Outcome contracts should carry the explicit result/evidence state, and tests should prove stale, denied, policy-blocked, and audit-unavailable records cannot be mistaken for successful audited mutations.
- Governance target contracts should identify safe target references such as conversation, message, attachment/file reference, participant attribution, audit record, or defined content segment. They must not carry raw content values.
- Audit evidence contracts should be citeable through stable safe handles and policy references. They must not expose audit sink names, log locations, raw storage keys, exception messages, or unbounded diagnostics.
- Closed vocabularies are preferred for outcome and operation classes so later handlers cannot invent incompatible strings. If represented as records/classes for converter compatibility, add JSON fixtures and unsafe term tests.
- Evidence handles should be opaque Conversations-owned values. They may correlate command, event, and audit evidence for adopters, but they must not be EventStore positions, audit sink keys, storage paths, stream names, projection checkpoints, or provider identifiers.
- Legal-hold handling must be represented as an explicit deferral or policy-blocked outcome. Do not model legal hold as a silent no-op, deletion, archival, redaction, or audit suppression path.
- Contract names and payload fields must stay business-facing and storage-neutral. Any EventStore, provider, upstream, storage, handler, projection, or runtime term in public names or serialized payloads is a review failure unless already allowed by an existing Conversations contract convention.

### Advanced Elicitation Hardening

The 2026-05-20 advanced elicitation pass kept Story 2.1 inside its contract-only scope and sharpened the places where public contracts can accidentally become authority, diagnostics, or disclosure surfaces. Governance contracts must distinguish command intent, operation metadata, event evidence, audit evidence, and public outcome state. Correlation and causation values help connect records, but they are not storage identity, audit sink identity, or proof of successful audited mutation.

Contract tests should deliberately exercise adversarial disclosure paths: unsafe `ToString()` output, XML documentation examples, fixture names, validation messages, denial payloads, audit-unavailable payloads, legal-hold deferral payloads, and failed serialization/validation assertions. These surfaces must remain as safe as the JSON payload itself.

### Testing Requirements

- Extend contract tests before considering the story complete. Every new public record must be present in `ContractSamples.AllContracts` and survive JSON round trip.
- Add representative fixture JSON for the important governance/evidence shapes so future changes are visible in review.
- Add a governance mutation/evidence matrix fixture covering success, denial, audit-unavailable failure, and policy-blocked outcomes for each Story 2.2-2.8 operation family.
- Add negative validation tests for missing rationale, missing policy reference, missing actor, missing tenant, missing conversation, invalid timestamp, and missing correlation ID.
- Add forbidden-content tests that construct governance/evidence payloads with unsafe terms and assert validation rejects them where fields are free text.
- Add semantic distinction tests proving contracts distinguish source event history, projected/displayed content, audit records, derived materializations, archival, logical deletion, retention enforcement, and legal-hold deferral without implying source-event deletion.
- Run at minimum:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
  - If project references or shared primitives move, run `dotnet test Hexalith.Conversations.slnx`.

### Project Structure Notes

- Primary write set should be limited to:
  - `src/Hexalith.Conversations.Contracts/Governance/`
  - `src/Hexalith.Conversations.Contracts/Serialization/` only if converters are needed
  - `tests/Hexalith.Conversations.Contracts.Tests/`
- Avoid changes in:
  - `src/Hexalith.Conversations/` aggregate/domain runtime
  - `src/Hexalith.Conversations.Server/` handlers, tenant access, projections, or audit runtime
  - sibling submodules such as EventStore, Tenants, Parties, FrontComposer, Folders, or Projects
- If a later developer discovers that contract names trip `ForbiddenPublicSurfaceTest`, adjust names toward Conversations-safe vocabulary rather than weakening the test.

### Lessons Applied

- L08: Party-mode review and advanced elicitation are separate hardening passes. Story 2.1 now has a completed party-mode trace and a completed advanced-elicitation trace; later implementation should treat those as pre-dev clarifications, not as permission to expand beyond contract-first scope. [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08 - Party Review Vs. Elicitation`]

## References

- `_bmad-output/planning-artifacts/epics.md#Story 2.1: Define Governance Policy and Audit Contracts`
- `_bmad-output/planning-artifacts/architecture.md#Governance Security`
- `_bmad-output/planning-artifacts/architecture.md#Redaction Semantics`
- `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`
- `_bmad-output/project-context.md#Critical Implementation Rules`
- `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`

## Dev Agent Record

### Agent Model Used

Codex GPT-5.5 requested when supported; implemented in the current Codex dev-story session.

### Debug Log References

- Preflight JSON: `_bmad-output/process-notes/predev-preflight-latest.json`
- Red phase: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` failed before implementation because `Hexalith.Conversations.Contracts.Governance` did not exist.
- Green/regression: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` passed with 126 tests.
- Full regression: `dotnet test Hexalith.Conversations.slnx` passed.

### Completion Notes List

- Story context created from Epic 2, Story 2.1, architecture governance/redaction/API constraints, project context, and current contract/test patterns.
- Story status moved to review; `sprint-status.yaml` owns the queue state.
- Added governance vocabulary, operation metadata, target, request, audit evidence handle/reference, and audit evidence contracts under the Contracts project only.
- Added content-safe validation for rationale, policy, correlation, causation, UTC timestamps, and opaque evidence references without exposing runtime, storage, provider, handler, projection, or diagnostic internals.
- Added governance JSON converters, curated contract samples, stable JSON fixtures, outcome matrix coverage, closed-vocabulary tests, unsafe disclosure tests, and full contract fixture coverage.
- Verified no command handlers, aggregate mutations, projections, audit sinks, UI components, or source-event deletion behavior were implemented.

### File List

- `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Conversations.Contracts/Governance/AuditEvidenceHandle.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidence.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceAuditEvidenceReference.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceContractValidation.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceOperationMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceRequest.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceTarget.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/GovernanceContractTest.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`

### Change Log

- 2026-05-22: Implemented Story 2.1 governance and audit contract surface with closed vocabularies, metadata, request/evidence records, validation, converters, samples, and tests.
- 2026-05-22: Updated story and sprint tracking to review after full solution regression passed.
- 2026-05-22: Senior review (Claude Opus 4.7) hardened AC5 ToString disclosure surface; overrode `ToString()` on `GovernanceOperationMetadata` and `GovernanceAuditEvidenceReference` to omit rationale/policy text, added `GovernanceRecordsShouldKeepToStringContentSafe` test, full solution regression passed (460 tests), story moved to done.

## Party-Mode Review

- Date: 2026-05-20T12:03:19Z
- Selected story key: `2-1-define-governance-policy-and-audit-contracts`
- Command/skill invocation used: `/bmad-party-mode 2-1-define-governance-policy-and-audit-contracts; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - Story is ready for development as a contract-first governance/audit slice, provided implementation stays out of runtime handlers, aggregates, projections, audit sinks, UI, and source-event deletion.
  - Audit pairing must be explicit for success, denial, audit-unavailable failure, and policy-blocked outcomes; generic audit envelopes alone are not sufficient unless matrix tests prove every required evidence path.
  - Contract vocabulary must distinguish redaction, retention, archival, logical deletion, derived materialization cleanup, audit records, and legal-hold deferral without implying source-event deletion.
  - Privacy, forbidden-surface, and dependency-boundary tests are the main guardrails against leaking EventStore, storage, provider, upstream, server/runtime, Party personal data, raw content, or diagnostics.
- Changes applied:
  - Clarified required audit outcome vocabulary and matrix-style evidence coverage.
  - Added uniform metadata validation expectations including malformed correlation and tenant-scope omission cases.
  - Expanded forbidden-surface expectations to include server/runtime, provider SDK, handler/projection, audit sink, storage location, stream/revision, and diagnostic terms.
  - Added dependency-boundary test expectation for the Contracts project.
  - Added explicit legal-hold deferral/policy-blocked semantics and semantic distinction tests for governance state concepts.
- Findings deferred:
  - Final public type names for governance/evidence outcomes may be adjusted during implementation if they remain domain-safe and tests bind the chosen vocabulary.
  - Irreversible source-event deletion remains out of scope unless a future approved ADR authorizes it.
- Final recommendation: ready-for-dev

## Senior Developer Review (AI)

- Date: 2026-05-22
- Reviewer: Jérôme Piquot (review session executed by Claude Opus 4.7)
- Skill invocation: `bmad-story-automator-review` (auto-fix mode)
- Outcome: Approve (after auto-fix)
- Tests: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` → 143 passed; `dotnet test Hexalith.Conversations.slnx` → 460 passed across Contracts, Client, Server, Integration, and Tests projects.
- Findings:
  - HIGH (fixed): AC5 ToString safety task was marked complete but `GovernanceOperationMetadata` and `GovernanceAuditEvidenceReference` relied on the default record `ToString()`, which echoes `Rationale` and `PolicyReference` free-text. No test asserted ToString content-safety.
  - MEDIUM: `GovernanceContractValidation.UnsafeTerms` contains substring duplicates (`storage`/`storage location`, `sdk`/`provider sdk`). Cosmetic only; substring `Contains` already covers both forms. Not fixed.
  - LOW: Closed-vocabulary `Value` properties and `AuditEvidenceHandle.Value` lack XML docs. Consistent with existing patterns; not fixed.
  - LOW: `_bmad-output/implementation-artifacts/tests/test-summary.md` was untracked and missing from the story File List. Added to File List.
- Fixes applied:
  - Overrode `ToString()` on `GovernanceOperationMetadata` to expose only stable identifiers (schema, tenant, conversation, actor, timestamp, correlation, causation) and omit `Rationale` and `PolicyReference`.
  - Overrode `ToString()` on `GovernanceAuditEvidenceReference` to expose only `Handle` and `CapturedAt` and omit `PolicyReference`.
  - Added `GovernanceRecordsShouldKeepToStringContentSafe` test in `GovernanceContractTest.cs` to assert `ToString()` output never echoes rationale or policy sample text for metadata, audit evidence reference, request, and full evidence shapes.
  - Updated File List with the previously untracked test summary artifact.
- Git vs Story File List: aligned after this review (`test-summary.md` added).
- Recommendation: Done.

## Advanced Elicitation

- Date: 2026-05-20T15:01:15Z
- Selected story key: `2-1-define-governance-policy-and-audit-contracts`
- Command/skill invocation used: `/bmad-advanced-elicitation 2-1-define-governance-policy-and-audit-contracts`
- Batch 1 method names: Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation; Critique and Refine
- Reshuffled Batch 2 method names: First Principles Analysis; Pre-mortem Analysis; Architecture Decision Records; Socratic Questioning; User Persona Focus Group
- Findings summary:
  - Story 2.1 was already ready for development after party-mode review, but advanced elicitation found implementability risks around public contracts becoming accidental authority or diagnostics surfaces.
  - Command metadata, governance operation metadata, event evidence, audit evidence, and public outcome state must remain separate enough that later handlers cannot treat correlation IDs, idempotency keys, projection checkpoints, or storage positions as audit proof.
  - Disclosure safety must cover non-obvious surfaces such as `ToString()`, XML documentation examples, fixture names, validation messages, denial payloads, audit-unavailable payloads, and assertion output.
- Changes applied:
  - Added AC 5 for safe contract authority and disclosure boundaries.
  - Clarified metadata separation, operation timestamp trust, public-vs-internal outcome vocabulary, opaque evidence handles, stale/denied/audit-unavailable state semantics, and non-JSON disclosure tests.
  - Updated Lessons Applied to reflect that party-mode review and advanced elicitation are both complete for this story.
- Findings deferred:
  - Exact public names for governance operation identity, audit evidence handles, retryability classes, and outcome vocabulary remain implementation choices as long as tests bind safe domain semantics.
  - Runtime audit enforcement, audit sink implementation, aggregate mutations, projection behavior, UI disclosure handling, and source-event deletion remain out of scope for Story 2.1.
- Final recommendation: ready-for-dev
