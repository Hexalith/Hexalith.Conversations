# Story 2.1: Define Governance Policy and Audit Contracts

Status: ready-for-dev

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

## Tasks / Subtasks

- [ ] Add governance contract vocabulary under `src/Hexalith.Conversations.Contracts/Governance` (AC: 1, 3)
  - [ ] Define bounded value objects or closed vocabularies for governance operation kind, governed target kind, retention action, sensitivity category, redaction category, archival state, legal-hold deferral, policy-blocked outcome, and privileged action class.
  - [ ] Keep names in Conversations language. Do not expose storage, EventStore, projection topology, tenant implementation, raw upstream, or provider mechanics in public type/property names.
  - [ ] Reuse existing `TenantId`, `ConversationId`, `MessageId`, `PartyId`, `FileId`, `SchemaVersion`, and string validation patterns instead of inventing parallel identifier types.

- [ ] Add governance command/evidence metadata contracts (AC: 1, 2, 4)
  - [ ] Introduce a shared governance metadata record that carries schema version, tenant ID, conversation ID, actor Party ID, rationale, policy reference, operation timestamp, correlation ID, and optional causation ID.
  - [ ] Preserve compatibility with existing `ConversationCommandMetadata` and `ConversationEventMetadata`; prefer composition or obvious mapping over duplicating unrelated command metadata fields.
  - [ ] Add safe audit handle/evidence reference contracts without exposing audit storage implementation details, persistence sequence numbers, logs, exceptions, or raw sink names.

- [ ] Add contract shapes for future Epic 2 mutation workflows (AC: 1, 2, 3)
  - [ ] Model request/event/evidence shapes needed by Stories 2.2-2.8: set/replace retention policy, mark content sensitive, redact message content, archive/logically delete or close governance state where in scope, legal-hold deferral, audit-record governance, and privileged operational justification.
  - [ ] Include explicit result/evidence states for success, denial, audit unavailable, and policy blocked.
  - [ ] Do not implement command handlers, aggregate mutation methods, audit sinks, projection materializers, UI components, or source-event deletion behavior in this story.

- [ ] Update serialization registration and curated samples (AC: 4)
  - [ ] Extend contract sample fixtures so every new public record is covered by `ContractSamples.AllContracts`.
  - [ ] Add stable representative JSON fixtures for governance metadata, a governance command contract, a governance event/evidence contract, and a denied/audit-unavailable outcome.
  - [ ] Add any required `System.Text.Json` converters only when existing converter patterns cannot cover the new type safely.

- [ ] Add focused contract validation tests (AC: 1, 2, 4)
  - [ ] Required-field tests cover rationale, actor Party ID, tenant ID, conversation ID, policy reference, schema version, timestamp, and correlation ID.
  - [ ] Forbidden-surface tests prove no raw message content, Party personal data, provider payloads, upstream detail objects, EventStore terms, storage terms, exception details, tokens, or claims appear in public type names, property names, JSON property names, or curated sample payloads.
  - [ ] Serialization round-trip tests use `System.Text.Json` web defaults and follow the existing JSON-equivalence style.

- [ ] Add documentation-ready developer notes in XML docs where public contracts would otherwise be ambiguous (AC: 3, 4)
  - [ ] State that redaction is append-only and policy governed by default.
  - [ ] State that event history, projected/displayed content, audit records, derived materializations, archival, logical deletion, retention enforcement, and legal-hold deferral are distinct concepts.
  - [ ] State that irreversible source-event deletion is out of scope unless a future approved ADR explicitly authorizes it.

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
- Governance target contracts should identify safe target references such as conversation, message, attachment/file reference, participant attribution, audit record, or defined content segment. They must not carry raw content values.
- Audit evidence contracts should be citeable through stable safe handles and policy references. They must not expose audit sink names, log locations, raw storage keys, exception messages, or unbounded diagnostics.
- Closed vocabularies are preferred for outcome and operation classes so later handlers cannot invent incompatible strings. If represented as records/classes for converter compatibility, add JSON fixtures and unsafe term tests.

### Testing Requirements

- Extend contract tests before considering the story complete. Every new public record must be present in `ContractSamples.AllContracts` and survive JSON round trip.
- Add representative fixture JSON for the important governance/evidence shapes so future changes are visible in review.
- Add negative validation tests for missing rationale, missing policy reference, missing actor, missing tenant, missing conversation, invalid timestamp, and missing correlation ID.
- Add forbidden-content tests that construct governance/evidence payloads with unsafe terms and assert validation rejects them where fields are free text.
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

- L08: Party-mode review and advanced elicitation are separate hardening passes. This story has no completed party-mode or advanced-elicitation trace yet; those must be added by later pre-dev hardening runs, not assumed from this create-story operation. [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08 - Party Review Vs. Elicitation`]

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

N/A - story created by BMAD create-story automation.

### Debug Log References

- Preflight JSON: `_bmad-output/process-notes/predev-preflight-latest.json`

### Completion Notes List

- Story context created from Epic 2, Story 2.1, architecture governance/redaction/API constraints, project context, and current contract/test patterns.
- Status set to ready-for-dev; `sprint-status.yaml` owns the queue state.

### File List

- `_bmad-output/implementation-artifacts/2-1-define-governance-policy-and-audit-contracts.md`
