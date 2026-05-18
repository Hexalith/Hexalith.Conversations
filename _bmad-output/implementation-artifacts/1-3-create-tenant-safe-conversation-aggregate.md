# Story 1.3: Create Tenant-Safe Conversation Aggregate

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want to create a tenant-scoped conversation through the Conversations domain model,
so that every conversation begins as a replayable, authorization-boundary-safe, EventStore-backed business record.

## Acceptance Criteria

1. Given a valid create-conversation command with tenant context, actor Party ID, schema version, idempotency metadata, and optional business references, when the application handler dispatches the command, then `ConversationAggregate` emits a versioned conversation-created event using Conversations language, and the event stores stable identifiers and metadata only, not Party personal data, provider session authority, raw upstream records, EventStore stream/envelope mechanics, or file binaries.
2. Given a conversation-created event exists, when aggregate state is rehydrated from ordered event history, then the resulting `ConversationState` contains the tenant-scoped conversation identity, lifecycle state, creator attribution, business references, provider correlation metadata where supplied, and creation timestamp copied from persisted event data, and the result is deterministic for the same ordered event history without wall-clock, provider, Parties, Tenants, or authorization lookups during replay.
3. Given a create-conversation command is invalid, malformed, unsupported-version, or missing required stable identifiers, when the command is handled, then the aggregate or boundary validator returns a typed rejection outcome with a stable machine-readable code, and no successful conversation-created event is emitted.
4. Given the command references provider identifiers or external business identifiers, when the conversation identity is assigned, then the internal `ConversationId` remains distinct from provider IDs, external identifiers, labels, and thread names, and provider/external IDs are stored only as correlation or business-reference metadata.
5. Given aggregate unit tests run, when valid and invalid create-conversation scenarios are executed, then tests prove emitted event shape, replayed state, rejection behavior, schema version handling, and absence of forbidden Party/provider/file payload data, and tests do not require Dapr, Aspire, tenant seed data, provider credentials, or initialized nested submodules.
6. Given adopter-facing contracts, events, results, errors, README examples, or test names are added for this story, when they are reviewed, then they use Conversations vocabulary only and do not expose raw EventStore terms such as stream, envelope, snapshot, sequence, append, expected revision, or aggregate-version internals.

## Tasks / Subtasks

- [ ] Add the create-conversation domain command and aggregate entry point. (AC: 1, 3, 4)
  - [ ] Add a domain-level `CreateConversation` command that consumes the public contract from Story 1.2 rather than defining a second public command shape.
  - [ ] Before implementation, verify the Story 1.2 public create command, metadata, result, and rejection contracts are available; if they are not available, stop the dev-story as blocked instead of creating shadow DTOs, aliases, or temporary public contracts.
  - [ ] Implement `ConversationAggregate : EventStoreAggregate<ConversationState>` with a static `Handle(CreateConversation command, ConversationState? state)` pattern aligned with the local EventStore sample aggregate.
  - [ ] Return `DomainResult.Success(...)` with exactly one Conversations-domain `ConversationCreated` event for a valid new conversation.
  - [ ] Return a typed rejection result for malformed command metadata, unsupported schema version, missing tenant binding, missing actor Party ID, missing/invalid conversation identity, create against already-created aggregate state, or forbidden provider/external identity substitution.

- [ ] Model deterministic conversation state replay. (AC: 2)
  - [ ] Add `ConversationState` with tenant ID, conversation ID, lifecycle state, creator Party ID, created timestamp, business references, provider correlation metadata, and schema/version information needed by replay.
  - [ ] Add `Apply(ConversationCreated e)` so replaying the same ordered event history produces the same state.
  - [ ] Ensure the aggregate and replay path copy timestamps only from command metadata or persisted event data; do not call wall-clock APIs inside aggregate handling or state application.
  - [ ] Add any required no-op rejection/tombstone apply method only if EventStore replay requires it; keep rejection handling deterministic and side-effect free.
  - [ ] Do not add message list, participant membership, file attachment behavior, projections, publication, or read-time Party hydration in this story.

- [ ] Preserve tenant-safe and content-safe event semantics. (AC: 1, 3, 4)
  - [ ] Keep durable event payloads to stable IDs and approved metadata: tenant ID, conversation ID, creator/actor Party ID, schema version, correlation/causation/idempotency metadata, business references, provider correlation metadata, and created timestamp.
  - [ ] Ensure event and state types never contain Party display names, contact details, person/organization details, raw Parties/Tenants problem details, provider prompt/response payloads, raw upstream records, file metadata/content, EventStore stream/envelope/snapshot fields, access tokens, claims, or authorization state.
  - [ ] Keep `ConversationId` the internal aggregate identity; provider IDs, labels, thread names, and external business IDs remain correlation or reference metadata only.
  - [ ] Reject any attempt to use provider IDs, external IDs, labels, or thread names as `ConversationId`, tenant authority, actor authority, or Party identity authority.

- [ ] Add the narrow application/domain dispatch boundary. (AC: 1, 3, 5)
  - [ ] Add only the minimal handler/mapper needed to translate the Story 1.2 create command contract into the domain command and dispatch through the aggregate pattern.
  - [ ] Validate required metadata structurally at the boundary before aggregate dispatch: null command, missing tenant, missing actor Party ID, missing conversation identity, malformed metadata, unsupported schema version, and tenant/actor/identity mismatch all fail closed with typed rejection and no success event.
  - [ ] Do not implement tenant membership authorization, local Tenants projection, REST endpoints, Dapr actors, EventStore actor persistence, idempotency storage, publication, FrontComposer UI, or conformance manifest generation here. Those are owned by later stories.

- [ ] Add aggregate-focused tests and safety scans. (AC: 1-5)
  - [ ] Add tests in `tests/Hexalith.Conversations.Tests` for successful create, deterministic rehydrate, duplicate create rejection, unsupported schema version rejection, missing tenant rejection, missing actor rejection, missing identity rejection, and provider/external identity separation.
  - [ ] Add tests for accepted command/event schema version values and missing, malformed, unsupported, future, or legacy schema version rejection.
  - [ ] Add payload/property inspection tests proving create events and state do not expose Party display names, emails, phone numbers, provider authority IDs, provider payloads, file metadata/content, raw upstream records, EventStore stream/envelope/snapshot terms, access tokens, claims, or authorization state.
  - [ ] Add boundary tests that inspect `.csproj` XML for expected EventStore domain references and forbidden Server/UI/Tenants/Parties/HTTP/Dapr/Aspire/provider-SDK dependencies where assembly references could be optimized away.
  - [ ] Use xUnit v3 and Shouldly; reuse `Hexalith.Conversations.Testing` factories when useful.

- [ ] Validate the implementation scope. (AC: 5)
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run `dotnet restore`, `dotnet build`, and `dotnet test .\Hexalith.Conversations.slnx` if assets are stale.
  - [ ] Do not run recursive submodule initialization. Root-level sibling module reads are enough if EventStore samples or contracts need inspection.
  - [ ] Leave `sprint-status.yaml` untouched during dev-story unless the dev workflow owns the status transition.

## Dev Notes

### Pre-Dev Party-Mode Review Decisions

The 2026-05-18 party-mode review clarified Story 1.3 without changing its scope:

- Story 1.2 is a hard prerequisite for public create command, metadata, result, and rejection contracts. If those contracts are unavailable when development starts, stop as blocked instead of inventing parallel DTOs.
- This story proves tenant/actor/identity presence and consistency plus authorization-boundary-safe fail-closed behavior. Full tenant membership authorization, local Tenants projection freshness, and permission decisions remain out of scope until Story 1.5.
- Duplicate create rejection means create against already-created aggregate state for the same internal `ConversationId`. Idempotency storage, repeated command detection across infrastructure, and duplicate provider/external correlation detection are out of scope.
- Provider IDs, external business IDs, labels, and thread names are optional correlation/reference metadata only. They never satisfy tenant, actor, Party, or internal conversation identity authority.
- Rejections must use stable machine-readable codes suitable for localization and adopter handling; human-readable English text must not be the only contract.

### Scope Boundary

Story 1.3 creates the first real write-side domain slice for creating a conversation. It may add aggregate, state, event, boundary validation, and aggregate tests. It must not implement append-message, participants, file references, projections, read APIs, EventStore actor persistence, tenant access projection, idempotency storage, audit pairing, event publication, FrontComposer UI, conformance manifest aggregation, or governance commands. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.3: Create Tenant-Safe Conversation Aggregate`; `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant-Safe Conversation Record`]

Story 1.2 is the contract prerequisite. If its contract package has not been implemented on the current branch, do not invent parallel public DTOs for Story 1.3. Either consume the merged Story 1.2 types or keep any temporary domain-only command internal and align it with the Story 1.2 names before completion. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Scope Boundary`]

### Architecture Compliance

Use `Hexalith.EventStore` as the authoritative write-side substrate. The aggregate should follow the EventStore sample pattern: `ConversationAggregate : EventStoreAggregate<ConversationState>` with static `Handle(Command, State?) -> DomainResult`, returning success events, rejection events, or no-op results as appropriate. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Counter/CounterAggregate.cs`]

Keep tenant access authorization outside aggregate logic. This story must require tenant binding and actor attribution on commands/events, but full membership/role enforcement and local Tenants projection freshness are owned by Story 1.5. Until that exists, boundary validation must fail closed for missing or malformed tenant metadata and must not call Tenants synchronously from aggregate logic. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]

Use Conversations language for durable events and public-facing names. Do not expose EventStore envelopes, stream names, snapshot mechanics, sequence numbers, expected revisions, SignalR groups, Dapr actor IDs, or projection topology through public contracts, domain events, errors, README examples, or test names. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Decision Pressure Points`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#EventStore envelope stability and evolution ownership`]

### Current Repository State and Previous Story Intelligence

The current repository has marker-only Conversations projects plus focused boundary tests. `src/Hexalith.Conversations` currently references `Contracts` only, `tests/Hexalith.Conversations.Tests` references the domain and Testing projects, and the project uses `net10.0`, SDK `10.0.300`, nullable enabled, implicit usings, warnings as errors, and central package management. [Source: `src/Hexalith.Conversations/Hexalith.Conversations.csproj`; `tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj`; `Directory.Build.props`; `Directory.Packages.props`; `global.json`]

Carry forward Story 1.2's review guidance even though that story is still pre-dev: compiled assembly-reference checks can pass vacuously when marker assemblies do not use a package. For dependency boundaries, inspect `.csproj` XML directly as well as compiled references. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Current Repository State and Previous Story Intelligence`; `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`]

### Domain Model Guardrails

- `ConversationId` is the aggregate identity and must stay tenant-scoped.
- Initial lifecycle should represent a created/open conversation only; close/archive lifecycle changes are later stories.
- `ConversationState` should be replay-only domain state, not a read projection or transcript table.
- Business references should stay stable identifiers or opaque metadata, not upstream records.
- Provider correlation metadata is allowed only as bounded, tenant-scoped correlation metadata; it is never authority.
- Timestamps should be supplied through command metadata or a deterministic boundary abstraction where possible; do not call wall-clock time inside replay logic.

[Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`; `_bmad-output/planning-artifacts/epics.md#Story 1.3: Create Tenant-Safe Conversation Aggregate`]

### Schema Version and Rejection Guidance

Use the Story 1.2 schema version constants and rejection taxonomy when those contracts are available. If Story 1.2 has not named them yet, dev-story must pause for product/architecture clarification rather than choosing arbitrary version values. Rejected create commands must be typed and content-safe. Use Story 1.2 error semantics where available; at minimum preserve machine-readable codes for missing tenant binding, tenant mismatch/isolation violation where applicable, unsupported schema version, aggregate already exists, identity substitution, malformed metadata, and command validation failure. Error text must not reveal cross-tenant conversation existence, Party details, provider payloads, raw business references, or target tenant details. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Typed Error and Trust Vocabulary`; `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`]

### File and Test Placement

Expected production files belong under:

- `src/Hexalith.Conversations/Aggregates`
- `src/Hexalith.Conversations/Commands`
- `src/Hexalith.Conversations/Events`
- `src/Hexalith.Conversations/State`
- `src/Hexalith.Conversations/Validation` or another existing local boundary folder if established during implementation

Expected tests belong under:

- `tests/Hexalith.Conversations.Tests/Aggregates`
- `tests/Hexalith.Conversations.Tests/State`
- `tests/Hexalith.Conversations.Tests/Validation`
- `tests/Hexalith.Conversations.Tests/Boundaries`

Shared deterministic factories may be added to `src/Hexalith.Conversations.Testing` only when they are reusable across future stories and do not smuggle runtime behavior. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `src/Hexalith.Conversations.Testing/Factories/ConversationTestIds.cs`]

### Validation

Validation must stay local and deterministic. Aggregate tests should be pure command/state/event tests and should not require Dapr, Aspire AppHost, EventStore server runtime, tenant seed data, provider credentials, production secrets, external cloud resources, or nested submodule initialization. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.3: Create Tenant-Safe Conversation Aggregate`]

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.3: Create Tenant-Safe Conversation Aggregate`
- `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Decision Pressure Points`
- `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`
- `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Counter/CounterAggregate.cs`
- `Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Counter/State/CounterState.cs`
- `tests/Hexalith.Conversations.Tests/DomainBoundaryTest.cs`
- `Directory.Build.props`
- `Directory.Packages.props`

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

## Party-Mode Review

- Date/time: 2026-05-18T11:08:55Z
- Selected story key: `1-3-create-tenant-safe-conversation-aggregate`
- Command/skill invocation used: `/bmad-party-mode 1-3-create-tenant-safe-conversation-aggregate; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: Reviewers converged on Story 1.2 prerequisite risk, authorization wording overreach, duplicate/idempotency ambiguity, identity substitution ambiguity, schema-version underdefinition, EventStore vocabulary leakage risk, timestamp determinism, payload privacy inspection gaps, and boundary mapper scope pressure.
- Changes applied: Clarified authorization-boundary-safe wording; added Story 1.2 blocking precondition; narrowed duplicate create to already-created aggregate state; added deterministic timestamp handling; expanded forbidden payload and EventStore-leakage checks; added provider/external identity authority rejection; specified boundary fail-closed validation cases; added schema-version test guidance; added stable rejection-code/localization guidance.
- Findings deferred: Product/architecture must confirm Story 1.2 concrete contract type names, schema-version constants, and rejection taxonomy before implementation if they are still unavailable. True tenant membership authorization and infrastructure idempotency remain later-story responsibilities unless scope is explicitly changed.
- Final recommendation: ready-for-dev

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-18: Party-mode review completed; low-risk clarifications applied and deferred decisions recorded.
