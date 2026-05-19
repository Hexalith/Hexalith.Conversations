# Story 1.3: Create Tenant-Safe Conversation Aggregate

Status: done

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

- [x] Add the create-conversation domain command and aggregate entry point. (AC: 1, 3, 4)
  - [x] Add a domain-level `CreateConversation` command that consumes the public contract from Story 1.2 rather than defining a second public command shape.
  - [x] Before implementation, verify the Story 1.2 public create command, metadata, result, and rejection contracts are available; if they are not available, stop the dev-story as blocked instead of creating shadow DTOs, aliases, or temporary public contracts.
  - [x] Implement `ConversationAggregate : EventStoreAggregate<ConversationState>` with a static `Handle(CreateConversation command, ConversationState? state)` pattern aligned with the local EventStore sample aggregate.
  - [x] Return `DomainResult.Success(...)` with exactly one Conversations-domain `ConversationCreated` event for a valid new conversation.
  - [x] Return a typed rejection result for malformed command metadata, unsupported schema version, missing tenant binding, missing actor Party ID, missing/invalid conversation identity, create against already-created aggregate state, or forbidden provider/external identity substitution.

- [x] Model deterministic conversation state replay. (AC: 2)
  - [x] Add `ConversationState` with tenant ID, conversation ID, lifecycle state, creator Party ID, created timestamp, business references, provider correlation metadata, and schema/version information needed by replay.
  - [x] Add `Apply(ConversationCreated e)` so replaying the same ordered event history produces the same state.
  - [x] Ensure the aggregate and replay path copy timestamps only from command metadata or persisted event data; do not call wall-clock APIs inside aggregate handling or state application.
  - [x] Add any required no-op rejection/tombstone apply method only if EventStore replay requires it; keep rejection handling deterministic and side-effect free.
  - [x] Do not add message list, participant membership, file attachment behavior, projections, publication, or read-time Party hydration in this story.

- [x] Preserve tenant-safe and content-safe event semantics. (AC: 1, 3, 4)
  - [x] Keep durable event payloads to stable IDs and approved metadata: tenant ID, conversation ID, creator/actor Party ID, schema version, correlation/causation/idempotency metadata, business references, provider correlation metadata, and created timestamp.
  - [x] Ensure event and state types never contain Party display names, contact details, person/organization details, raw Parties/Tenants problem details, provider prompt/response payloads, raw upstream records, file metadata/content, EventStore stream/envelope/snapshot fields, access tokens, claims, or authorization state.
  - [x] Keep `ConversationId` the internal aggregate identity; provider IDs, labels, thread names, and external business IDs remain correlation or reference metadata only.
  - [x] Reject any attempt to use provider IDs, external IDs, labels, or thread names as `ConversationId`, tenant authority, actor authority, or Party identity authority.

- [x] Add the narrow application/domain dispatch boundary. (AC: 1, 3, 5)
  - [x] Add only the minimal handler/mapper needed to translate the Story 1.2 create command contract into the domain command and dispatch through the aggregate pattern.
  - [x] Validate required metadata structurally at the boundary before aggregate dispatch: null command, missing tenant, missing actor Party ID, missing conversation identity, malformed metadata, unsupported schema version, and tenant/actor/identity mismatch all fail closed with typed rejection and no success event.
  - [x] Do not implement tenant membership authorization, local Tenants projection, REST endpoints, Dapr actors, EventStore actor persistence, idempotency storage, publication, FrontComposer UI, or conformance manifest generation here. Those are owned by later stories.

- [x] Add aggregate-focused tests and safety scans. (AC: 1-5)
  - [x] Add tests in `tests/Hexalith.Conversations.Tests` for successful create, deterministic rehydrate, duplicate create rejection, unsupported schema version rejection, missing tenant rejection, missing actor rejection, missing identity rejection, and provider/external identity separation.
  - [x] Add tests for accepted command/event schema version values and missing, malformed, unsupported, future, or legacy schema version rejection.
  - [x] Add payload/property inspection tests proving create events and state do not expose Party display names, emails, phone numbers, provider authority IDs, provider payloads, file metadata/content, raw upstream records, EventStore stream/envelope/snapshot terms, access tokens, claims, or authorization state.
  - [x] Add boundary tests that inspect `.csproj` XML for expected EventStore domain references and forbidden Server/UI/Tenants/Parties/HTTP/Dapr/Aspire/provider-SDK dependencies where assembly references could be optimized away.
  - [x] Use xUnit v3 and Shouldly; reuse `Hexalith.Conversations.Testing` factories when useful.

- [x] Validate the implementation scope. (AC: 5)
  - [x] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore`, or run `dotnet restore`, `dotnet build`, and `dotnet test .\Hexalith.Conversations.slnx` if assets are stale.
  - [x] Do not run recursive submodule initialization. Root-level sibling module reads are enough if EventStore samples or contracts need inspection.
  - [x] Leave `sprint-status.yaml` untouched during dev-story unless the dev workflow owns the status transition.

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

GPT-5 Codex

### Debug Log References

- `dotnet test .\tests\Hexalith.Conversations.Tests\Hexalith.Conversations.Tests.csproj --no-restore` failed before implementation with missing aggregate/state/validation namespaces as expected.
- `dotnet test .\tests\Hexalith.Conversations.Tests\Hexalith.Conversations.Tests.csproj --no-restore` passed after implementation: 22 / 22.
- `dotnet test .\Hexalith.Conversations.slnx --no-restore` passed after integration-boundary update: 93 / 93 across current test projects.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Verified Story 1.2 create command, metadata, event metadata, schema version, ID, result, and rejection contracts existed before implementation.
- Added `CreateConversation` domain command, `ConversationAggregate`, deterministic `ConversationState`, Conversations-domain `ConversationCreated` event, and content-safe `ConversationRejected` rejection event.
- Added narrow `CreateConversationBoundary` mapper/validator path with fail-closed typed rejections for null/malformed command data, unsupported schema versions, duplicate creation, missing identity, invalid event identity, invalid created timestamp, and provider/external identity substitution.
- Preserved story scope: no tenant membership authorization, tenant projection, Dapr actors, REST endpoints, EventStore actor persistence, idempotency storage, publication, UI, messages, participants, attachments, projections, or read-time Party hydration were added.
- Added aggregate, replay, validation, payload-safety, and project-boundary tests; updated scaffold integration expectations for the new approved EventStore domain references.

### File List

- `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Conversations/Hexalith.Conversations.csproj`
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
- `src/Hexalith.Conversations/Commands/CreateConversation.cs`
- `src/Hexalith.Conversations/Events/ConversationCreated.cs`
- `src/Hexalith.Conversations/Events/ConversationRejected.cs`
- `src/Hexalith.Conversations/State/ConversationLifecycleState.cs`
- `src/Hexalith.Conversations/State/ConversationState.cs`
- `src/Hexalith.Conversations/Validation/CreateConversationBoundary.cs`
- `src/Hexalith.Conversations/Validation/CreateConversationValidation.cs`
- `tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj`
- `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateCreateTest.cs`
- `tests/Hexalith.Conversations.Tests/Boundaries/DomainProjectBoundaryTest.cs`
- `tests/Hexalith.Conversations.Tests/State/ConversationStateSafetyTest.cs`
- `tests/Hexalith.Conversations.Tests/Validation/CreateConversationBoundaryTest.cs`
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`

## Party-Mode Review

- Date/time: 2026-05-18T11:08:55Z
- Selected story key: `1-3-create-tenant-safe-conversation-aggregate`
- Command/skill invocation used: `/bmad-party-mode 1-3-create-tenant-safe-conversation-aggregate; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: Reviewers converged on Story 1.2 prerequisite risk, authorization wording overreach, duplicate/idempotency ambiguity, identity substitution ambiguity, schema-version underdefinition, EventStore vocabulary leakage risk, timestamp determinism, payload privacy inspection gaps, and boundary mapper scope pressure.
- Changes applied: Clarified authorization-boundary-safe wording; added Story 1.2 blocking precondition; narrowed duplicate create to already-created aggregate state; added deterministic timestamp handling; expanded forbidden payload and EventStore-leakage checks; added provider/external identity authority rejection; specified boundary fail-closed validation cases; added schema-version test guidance; added stable rejection-code/localization guidance.
- Findings deferred: Product/architecture must confirm Story 1.2 concrete contract type names, schema-version constants, and rejection taxonomy before implementation if they are still unavailable. True tenant membership authorization and infrastructure idempotency remain later-story responsibilities unless scope is explicitly changed.
- Final recommendation: ready-for-dev

## Review Findings

### Decisions Resolved

- [x] [Review][Decision] Parallel `ConversationCreated` domain event shadows the public Story 1.2 contract — **Resolved (rename):** renamed the domain events to `ConversationCreatedDomainEvent` and `ConversationRejectedDomainEvent` to remove the public-API name collision. The public `Hexalith.Conversations.Contracts.Events.ConversationCreated` remains the publication shape (owned by Story 1.10). `IdempotencyKey`, `IEventPayload`/`IRejectionEvent` markers, and replay semantics stay in the domain layer. The aggregate's `DomainConversationCreated` `using` alias was removed; XmlDoc on the renamed types documents the domain/public boundary.
- [x] [Review][Decision] `Apply(ConversationCreated)` silently overwrites state on duplicate replay — **Resolved (throw):** added a deterministic invariant guard in `ConversationState.Apply(ConversationCreatedDomainEvent)` that throws `InvalidOperationException` when `IsCreated == true`. Corrupted streams that contain duplicate creation events now fail loudly at replay rather than silently rewriting tenant binding, creator attribution, or creation timestamp. New `ReplayingDuplicateConversationCreatedShouldThrowReplayInvariantViolation` test pins the behavior.
- [x] [Review][Decision] `ConversationRejected` taxonomy alignment with Story 1.2 — **Dismissed (document boundary):** the durable rejection event and the caller-facing `ConversationError`/`ConversationErrorResult` envelope are deliberately separate event-sourcing concerns. The rejection event keeps the minimal Story 1.2 `ConversationErrorCode` plus a stable `ReasonCode`; the caller pipeline (Story 1.5+) and publication shape (Story 1.10) wrap it with `Category`, `IsRetryable`, audit handles, and documentation at response time. XmlDoc on `ConversationRejectedDomainEvent` documents this boundary explicitly.

### Patches Applied

- [x] [Review][Patch] Identity-substitution check now covers `BusinessReference.System` — `src/Hexalith.Conversations/Validation/CreateConversationValidation.cs`.
- [x] [Review][Patch] Identity-substitution check now covers `ProviderCorrelationMetadata.ProviderName`, `ProviderType`, and every `ExtensionData` key and value — extracted into a dedicated `ProviderCorrelationCarriesIdentity` helper in `src/Hexalith.Conversations/Validation/CreateConversationValidation.cs`.
- [x] [Review][Patch] Identity-substitution check now covers `command.EventId` — `src/Hexalith.Conversations/Validation/CreateConversationValidation.cs`.
- [x] [Review][Patch] `ConversationCreatedDomainEvent.Metadata` is now null-guarded at the record initializer with `Metadata = Metadata ?? throw new ArgumentNullException(nameof(Metadata))` — `src/Hexalith.Conversations/Events/ConversationCreatedDomainEvent.cs`.
- [x] [Review][Patch] Split `metadata is null` from `metadata.TenantId is null`: the former now returns `CommandValidationFailed:metadata_missing`, the latter retains `TenantBindingMissing:tenant_binding_missing`. New `MissingCommandMetadataShouldReturnMetadataMissingRejection` test pins the split.
- [x] [Review][Patch] Added aggregate-level validator coverage: `NullDomainCommandShouldReturnTypedRejection`, `NullPublicCommandShouldReturnTypedRejection`, `MissingCommandMetadataShouldReturnMetadataMissingRejection`, `MissingEventIdentityShouldReturnTypedRejection` (theory), and `CreatedAtBeforeBusinessRangeShouldReturnTypedRejection`. The substitution theory now exercises 15 substitution paths (was 4).
- [x] [Review][Patch] Added `SerializedRejectionEventShouldNotContainForbiddenPayloadTerms` content-safety test that serializes a `ConversationRejectedDomainEvent` carrying caller-supplied `CorrelationId` and `CausationId` and asserts no forbidden payload terms appear.

### Deferred

- [x] [Review][Defer] `IsBusinessTimestamp` accepts year 9999 / `DateTimeOffset.MaxValue` — `src/Hexalith.Conversations/Validation/CreateConversationValidation.cs:144-145`. Symmetric with `ConversationEventMetadata.ValidateTimestamp`; spec does not define an upper bound. Improvement, not blocker. — deferred, scope tightening for governance epic.
- [x] [Review][Defer] `ConversationStateSafetyTest` positive assertions on fixture sentinels are tautological — `tests/Hexalith.Conversations.Tests/State/ConversationStateSafetyTest.cs:1021-1024`. The forbidden-term scan is the load-bearing assertion; positive sentinel checks add no safety guarantee. — deferred, test-strengthening pass.
- [x] [Review][Defer] `DomainProjectBoundaryTest` uses Windows backslash literals — `tests/Hexalith.Conversations.Tests/Boundaries/DomainProjectBoundaryTest.cs:883-889`. Will break on Linux CI; not regressive today. — deferred, cross-platform CI pass.
- [x] [Review][Defer] `DomainProjectBoundaryTest` uses hardcoded 5-level `..` traversal — `tests/Hexalith.Conversations.Tests/Boundaries/DomainProjectBoundaryTest.cs:867`. Fragile to runtime layout changes (`bin/Debug/net10.0/<rid>`). — deferred, test infrastructure refactor.
- [x] [Review][Defer] `ConversationRejected.ReasonCode` validation throws on JSON deserialize of null/whitespace — `src/Hexalith.Conversations/Events/ConversationRejected.cs:37-41`. Replay of malformed rejection terminates with exception rather than typed no-op. — deferred, owned by Story 1.11 (replay safety).
- [x] [Review][Defer] `schema_version_missing` shares `ConversationErrorCode.SchemaVersionUnsupported` — `src/Hexalith.Conversations/Validation/CreateConversationValidation.cs:46-49`. Distinct `ReasonCode` preserved; differentiation at the top-level code may be desirable for adopters. — deferred, error taxonomy refinement.
- [x] [Review][Defer] `ScaffoldSmokeTest` mixes forward-slash and backslash path conventions across test files — `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`. Inconsistency surfaces with cross-platform CI. — deferred, cross-platform CI pass.

## Change Log

- 2026-05-19: Code-review patches applied (best-practice resolution for all 3 decisions and all 7 patches). Renamed domain events to `ConversationCreatedDomainEvent` / `ConversationRejectedDomainEvent` to eliminate the Story 1.2 public-API name collision. Added replay-invariant guard in `Apply(ConversationCreatedDomainEvent)`. Extended identity-substitution check to cover `EventId`, `BusinessReference.System`, `ProviderName`, `ProviderType`, and every `ExtensionData` key/value. Split `metadata_missing` from `tenant_binding_missing`. Null-guarded `Metadata` at the event initializer. Added 12 new tests; suite runs 111/111 green (was 93/93). Story moved to `done`.
- 2026-05-19: Code-review (multi-layer adversarial) recorded: 3 decision-needed, 7 patches, 7 deferred, 13 dismissed.
- 2026-05-18: Implemented tenant-safe conversation aggregate creation, deterministic replay state, typed rejection validation, narrow boundary dispatch, and aggregate/project safety tests; moved story to review.
- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-18: Party-mode review completed; low-risk clarifications applied and deferred decisions recorded.
