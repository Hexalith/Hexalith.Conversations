# Story 1.4: Add Conversation Participants with Stable Party Attribution

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want to add human users, AI agents, and LLMs as conversation participants,
so that participant membership is attributable through stable Party identities without storing Party personal data.

## Acceptance Criteria

1. Given an existing active conversation and a valid add-participant command, when the command is handled, then the aggregate emits a versioned participant-added event for a stable Party ID and participant role/type such as human, AI agent, or LLM, and the event stores stable identifiers and allowed metadata only.
2. Given participant membership is persisted, when the event contract, aggregate state, projections, logs, and tests are inspected, then no mutable Party personal data, contact values, names, identifiers beyond stable Party ID, upstream person details, organization details, raw Parties problem details, provider-owned session authority, prompt content, or provider payload is stored in durable Conversations events.
3. Given a participant command targets a closed, archived, unsupported, malformed, missing, incompatible, duplicate, or cross-tenant-unsafe conversation state, when the command is handled, then the system returns a typed documented rejection outcome and no successful participant-added event is emitted.
4. Given Parties cannot validate a new participant at command time, when the add-participant command is handled, then the write fails closed with a typed content-safe rejection and does not call the aggregate with unvalidated participant identity.
5. Given aggregate tests replay participant events, when conversation state is rehydrated, then state reconstructs participant membership and attribution metadata deterministically and tests prove human, AI agent, and LLM participants can be represented without treating provider IDs as source-of-truth identity.

## Tasks / Subtasks

- [x] Confirm prerequisite contract and aggregate foundations before implementation. (AC: 1-5)
  - [x] Verify Story 1.2 contract types exist for `ConversationId`, `TenantId`, `PartyId`, command/event metadata, typed errors, schema versioning, and the `ParticipantAdded` event; if they do not, implement only the missing participant-specific contract surface needed by this story and keep it aligned with Story 1.2.
  - [x] Verify Story 1.3 created `ConversationAggregate` and `ConversationState`; if not, stop and either implement Story 1.3 first or explicitly include the minimal aggregate foundation in this story after updating sprint scope.
  - [x] Re-read every existing file this story will update before editing it, especially contract, aggregate, state, command handler, validation, and test files created by Stories 1.2 and 1.3.

- [x] Add or complete participant contract types in `src/Hexalith.Conversations.Contracts`. (AC: 1-3, 5)
  - [x] Ensure `Commands/AddParticipantCommand` carries tenant binding, conversation identity, actor/caller Party ID, target participant Party ID, participant type/role, schema version, correlation/causation metadata, and idempotency metadata where the shared command contract requires it.
  - [x] Ensure `Events/ParticipantAdded` is past-tense, versioned, tenant-scoped, conversation-scoped, and carries only stable Party IDs, participant type/role, correlation/causation metadata, and deterministic timestamp/version metadata expected by public contracts.
  - [x] Define participant type/role vocabulary for at least human, AI agent, and LLM in Contracts or reuse the Story 1.2 type if it already exists.
  - [x] Keep Contracts infrastructure-free and do not reference Parties client DTOs, provider DTOs, EventStore envelopes, server abstractions, Dapr, HTTP, UI, or persistence packages from participant command/event contracts.
  - [x] Keep provider identifiers as optional correlation metadata only; never make provider session IDs, model names, UI labels, thread names, or external business identifiers the participant source of truth.
  - [x] Treat stable `PartyId` as the only durable participant attribution authority; provider/user/model identifiers may be transient validation inputs only and must never become replay authority.
  - [x] Document the public rejection/result names for duplicate participant, unsupported participant type, provider-only identity, Party validation unavailable, and tenant-context mismatch so adopters do not infer behavior from raw upstream errors or EventStore failures.

- [x] Implement deterministic aggregate participant behavior in `src/Hexalith.Conversations`. (AC: 1-3, 5)
  - [x] Add or update `Conversations/ConversationAggregate.cs` with an add-participant command method that accepts validated command intent only.
  - [x] Add or update `Conversations/ConversationState.cs` to maintain participant membership keyed by stable Party ID plus approved role/type dimensions; by default, a duplicate is the same conversation, stable Party ID, and participant role/type unless the implemented contract explicitly forbids multiple roles for one Party.
  - [x] Apply `ParticipantAdded` events through deterministic replay logic; no wall-clock reads, HTTP calls, tenant lookups, Parties calls, UI shaping, EventStore calls, or logging from aggregate code.
  - [x] Reject closed, archived, unsupported, malformed, missing, incompatible, or duplicate participant states with typed domain results and no emitted success event; duplicate membership rejection is a domain state rule here, not the retry-safe idempotency contract owned by Story 1.6.
  - [x] Preserve existing conversation identity, lifecycle, creator attribution, business references, provider correlation metadata, and event replay behavior introduced by prior stories.
  - [x] Prove aggregate replay reconstructs participant state from `ParticipantAdded` events alone without calling Parties, providers, Tenants, read models, EventStore infrastructure, or command-time validators.

- [x] Add application-boundary participant validation only where the existing command pipeline already exists. (AC: 3-4)
  - [x] If Story 1.3 already introduced command handlers, add `Server/CommandHandlers/AddParticipantCommandHandler.cs` or equivalent in the established pattern.
  - [x] Validate command shape, schema version, tenant binding, conversation identity, actor Party ID, target Party ID, and participant type before aggregate invocation, then call the aggregate only after command-time Party proof has succeeded.
  - [x] Introduce or reuse a Conversations-owned adapter boundary such as `Server/Hydration/IParticipantDirectory` for command-time Party validation; keep it application-boundary only and do not reference it from Contracts, aggregate, or domain code.
  - [x] Make Parties validation unavailable, unknown, disabled, malformed, inaccessible, negative, indeterminate, or tenant-context-mismatched outcomes fail closed with content-safe typed errors.
  - [x] Treat "cross-tenant unsafe" as any participant addition where Party ownership or visibility for the command tenant cannot be proven by the application boundary.
  - [x] Do not implement full tenant access enforcement here unless it already exists from a prior story; Story 1.5 owns the comprehensive fail-closed tenant access gate.
  - [x] If the existing command pipeline has no tenant access service yet, preserve a single explicit guard seam for Story 1.5 instead of scattering tenant checks through participant validation, aggregate code, or transport code.

- [x] Add focused tests for participant contracts, aggregate behavior, and boundary hygiene. (AC: 1-5)
  - [x] Add contract tests proving `AddParticipantCommand` and `ParticipantAdded` serialize/deserialize with `System.Text.Json`, preserve required fields, and omit forbidden Party personal-data/provider payload fields by property-name inspection and representative JSON payload inspection.
  - [x] Add pure aggregate tests for successful human, AI agent, and LLM participant addition; deterministic replay; duplicate/conflicting participant rejection; closed/archived/incompatible state rejection; unsupported participant type rejection; provider-only identity rejection; and no event emission on rejection.
  - [x] Add application-boundary tests for Party validation success and fail-closed unavailable/unknown/inaccessible/timeout/error/not-found/tenant-mismatch cases if the command handler exists in scope, with typed rejection assertions instead of generic failure assertions.
  - [x] Keep aggregate tests pure and event-based; use a mocked or fake `IParticipantDirectory` only in command-handler/application-boundary tests, and include a proof that replay does not invoke the directory or any validation adapter.
  - [x] Add or extend boundary tests that inspect `.csproj` XML directly for forbidden references, not only `Assembly.GetReferencedAssemblies()`, because marker assemblies can make reflection-only checks pass vacuously.
  - [x] Ensure test fixtures are synthetic and tenant-safe; do not use real Party names, contact values, provider payloads, prompt content, or cross-tenant identifiers as fixture data.
  - [x] Inspect serialized events, aggregate snapshots if introduced, logs/diagnostic messages, `ToString()` output, and rejection payloads for forbidden personal-data/provider sentinels, not only the primary event JSON.

- [x] Update developer documentation and validation. (AC: 1-5)
  - [x] Update `README.md` or the existing contract guidance with participant attribution rules: stable Party IDs are durable, Party personal data is read-time hydration only, and provider IDs are correlation metadata only.
  - [x] Link to readiness decisions for Party hydration degraded states and projection freshness blocking semantics where relevant.
  - [x] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore` if assets are current; otherwise run restore/build/test for the solution.
  - [x] Do not initialize nested submodules, and do not run recursive submodule commands.

## Dev Notes

### Scope Boundary

Story 1.4 owns participant membership and stable Party attribution for conversations. It should not implement append-message authoring, file references, tenant access enforcement, idempotent command storage, read projections, publication, FrontComposer UI, conformance evidence, governance commands, or Party display hydration beyond the command-time validation adapter boundary needed to fail closed on writes. Story 1.4.1 in the epics owns append-message author attribution; Story 1.5 owns comprehensive tenant access enforcement; Story 1.6 owns idempotent command handling; Story 1.7 owns read-model freshness. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.4: Add Conversation Participants with Stable Party Attribution`; `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`]

The 2026-05-18 party-mode review clarified that Story 1.4 remains ready only if implementation avoids three silent decisions: it must not design full tenant authorization beyond fail-closed participant validation, it must not design retry-safe idempotent duplicate-command semantics, and it must not design read-side Party hydration or provider identity reconciliation. Those decisions remain deferred to Stories 1.5, 1.6, 1.7, or a later explicit provider-mapping story.

The 2026-05-18 advanced-elicitation pass tightened the same boundary without expanding scope: the implementation should expose explicit typed outcomes for participant-state and validation failures, keep one tenant-access seam for Story 1.5, treat duplicate participant membership as a domain rule rather than idempotency, and prove privacy across events, snapshots if any, diagnostics, and public rejection payloads.

At story creation time, `sprint-status.yaml` has Stories 1.2 and 1.3 as `ready-for-dev`, and this story moves Story 1.4 from `backlog` to `ready-for-dev`. The source tree still contains scaffold/marker files only under `src/Hexalith.Conversations.Contracts`, `src/Hexalith.Conversations`, and `src/Hexalith.Conversations.Server`. Treat this story as dependent on the contract and aggregate foundations being implemented before participant behavior starts; if they are not present on the branch, do not pretend the participant behavior can be cleanly layered. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; local source inspection on 2026-05-18]

### Current Repository State and Previous Story Intelligence

Story 1.1 created the buildable .NET 10 scaffold and completed review patches. The solution uses `global.json` SDK `10.0.300`, `net10.0`, nullable enabled, implicit usings, warnings as errors, central package management, xUnit v3, Shouldly, and marker-only assemblies. `Hexalith.Conversations.Server` is intentionally fail-closed at startup until behavior stories replace the inert scaffold with real application code. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Completion Notes List`; `global.json`; `Directory.Build.props`; `Directory.Packages.props`; `src/Hexalith.Conversations.Server/Program.cs`]

Story 1.2 has been created but not completed in sprint status. Carry forward its contract guidance: Contracts are the adopter-facing boundary, must remain infrastructure-free, and must not expose EventStore envelopes, stream names, snapshots, sequence numbers, projection topology, HTTP clients, Dapr, FrontComposer shell packages, or server-only abstractions. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Dev Notes`; `_bmad-output/project-context.md#Critical Implementation Rules`]

Story 1.3 is the immediate previous story context for this participant slice. It owns `ConversationAggregate`, `ConversationState`, conversation-created replay, lifecycle state, typed create rejection, and provider/external identity separation. This story must preserve that behavior and extend the aggregate state with participant membership only after reading the implemented Story 1.3 files. [Source: `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md#Tasks / Subtasks`; `_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md#Dev Notes`]

Carry forward the Story 1.1 review lesson: reflection-only dependency checks can pass vacuously when marker assemblies do not use a referenced package. Add `.csproj` XML inspection for forbidden references when this story introduces real contract/domain/server content. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Review Findings`; `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`]

Recent git history is documentation/scaffold focused: `062bee3 docs: create story 1.2 contract definitions`, `4479ced feat: Update subproject commits and add integration tests for scaffold validation`, and `c218a1e feat: Update subproject commits, finalize initial project setup, and enhance testing framework`. The working tree has unrelated modified root-level sibling submodules and process-note files; do not revert or rely on those changes for this story. [Source: `git log --oneline -5`; `git status --short` on 2026-05-18]

### Participant and Party Rules

Stable Party ID is the durable attribution anchor. Conversations may store `PartyId`, participant type/role, conversation identity, tenant scope, actor attribution, event metadata, schema version, and approved provider correlation metadata. Conversations must not store Party display names, contact values, identifiers beyond the approved stable Party ID, person details, organization details, lifecycle snapshots, or raw upstream Parties errors in events. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.4: Add Conversation Participants with Stable Party Attribution`; `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`]

Command-time participant validation fails closed when Parties cannot validate a new participant. Authorized reads may later degrade Party display hydration, but this story is a write-side participant membership story; it must not persist hydrated display data to compensate for read-time degradation. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]

Wrap Parties behind a Conversations-owned adapter such as `IParticipantDirectory` under `Server/Hydration` or an established local equivalent. The aggregate and domain state must never call Parties, Tenants, EventStore, HTTP, Dapr, UI, RAG/Memories, export, or logging services. [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`; `_bmad-output/planning-artifacts/architecture.md#Service Boundaries`; `_bmad-output/project-context.md#Critical Implementation Rules`]

### Architecture Compliance

The expected write flow remains: API/request DTO -> validator -> tenant access check when available -> Party validation/policy/idempotency when available -> aggregate command -> EventStore append -> projection update -> query/read response. This story should implement only the portions that exist at this sprint point and keep placeholders fail-closed where later stories own the behavior. [Source: `_bmad-output/planning-artifacts/architecture.md#Integration Points`]

`ConversationAggregate` is deterministic and side-effect free. It accepts validated command intent, emits domain events, and rehydrates state from ordered events. It must not inspect JWT claims, query tenant projection state, call Parties, assign provider session authority, hydrate display values, or infer UI trust states. [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`; `_bmad-output/project-context.md#Framework-Specific Rules`]

EventStore is the only v1 source of truth for conversation state. Participant events are Conversations domain events, not transcript-table rows, cache entries, or provider history records. Projections, caches, exports, UI state, conformance evidence, and future Memories/RAG indexes are derived and must not become participant authority. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Boundaries`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### File Structure Guidance

Expected production locations after prerequisites are in place:

- `src/Hexalith.Conversations.Contracts/Commands/AddParticipantCommand.cs`
- `src/Hexalith.Conversations.Contracts/Events/ParticipantAdded.cs`
- `src/Hexalith.Conversations.Contracts/Participants/*` or the existing participant contract folder established by Story 1.2
- `src/Hexalith.Conversations/Conversations/ConversationAggregate.cs`
- `src/Hexalith.Conversations/Conversations/ConversationState.cs`
- `src/Hexalith.Conversations/Participants/*` for domain participant value objects if needed
- `src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs` only if the server command pipeline exists
- `src/Hexalith.Conversations.Server/Hydration/IParticipantDirectory.cs` or equivalent adapter boundary if command-time Parties validation is implemented

Expected test locations:

- `tests/Hexalith.Conversations.Contracts.Tests/*Participant*`
- `tests/Hexalith.Conversations.Tests/*Participant*`
- `tests/Hexalith.Conversations.Server.Tests/*Participant*` only if server handler/validation is in scope
- shared deterministic fixture additions under `src/Hexalith.Conversations.Testing` only when reusable and non-operative

[Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; local source inspection on 2026-05-18]

### Testing Requirements

Use pure aggregate tests for domain behavior and keep authorization/application orchestration outside aggregate tests. Cover successful participant addition for human, AI agent, and LLM; replay determinism; duplicate/conflicting participant rejection; closed/archived/incompatible-state rejection; malformed/missing identity rejection; and no event emission on rejection. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.4: Add Conversation Participants with Stable Party Attribution`]

Privacy tests must inspect command/event serialized property names and representative JSON payloads to prove forbidden Party personal data and provider payload fields are absent. Include forbidden-name sentinels such as `displayName`, `email`, `phone`, `personDetails`, `organizationDetails`, `contact`, `providerPayload`, `prompt`, and `rawProblem` only as test expectations, not as real fixture data. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/prd.md#Security And Privacy`]

If server handler work is in scope, add tests proving Parties validation unavailable/unknown/inaccessible states fail closed before aggregate invocation. Tenant projection enforcement can remain a later Story 1.5 responsibility, but this story must not create an unsafe bypass that later tenant checks cannot wrap. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`; `_bmad-output/planning-artifacts/architecture.md#Component Boundaries`]

### Latest Technical Information

Microsoft Learn confirms `net10.0` is a supported .NET 10 TFM and is suitable as the base TFM for portable app/library components; keep the repo's `net10.0` target and do not downgrade to `net9.0`. [Source: Microsoft Learn, Target frameworks in SDK-style projects, 2026-05-18 lookup: https://learn.microsoft.com/dotnet/standard/frameworks]

NuGet Central Package Management requires versions in `Directory.Packages.props` and `<PackageReference>` entries without `Version` attributes in individual project files. Preserve that pattern for any new package references. [Source: Microsoft Learn, Central Package Management, 2026-05-18 lookup: https://learn.microsoft.com/nuget/consume-packages/central-package-management]

`System.Text.Json` supports records and immutable types, and .NET 9+ can enforce non-optional constructor parameters with `RespectRequiredConstructorParameters`; contract tests should verify required participant fields rather than assuming constructor parameters are always enforced by default serializer settings. [Source: Microsoft Learn, System.Text.Json immutable types and required properties, 2026-05-18 lookup: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/immutability and https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/required-properties]

### Anti-Reinvention Warnings

- Do not create a transcript table, participant table as source of truth, provider session store, memory store, or module-owned Party cache as participant authority.
- Do not copy Parties contracts or runtime behavior into Conversations; reference stable Party IDs and use a Conversations-owned adapter boundary.
- Do not store Party personal data in durable events, aggregate state, logs, errors, traces, metrics, snapshots, test fixtures, or docs examples.
- Do not call Parties, Tenants, EventStore, HTTP, Dapr, FrontComposer, or Memories from aggregate/domain code.
- Do not expose raw EventStore mechanics, Parties error payloads, or tenant authorization internals in public contracts or typed errors.
- Do not implement append-message behavior under this story except to ensure participant structures are usable by the later author-attribution story.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.4: Add Conversation Participants with Stable Party Attribution`
- `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant-Safe Conversation Record`
- `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Data Boundaries`
- `_bmad-output/planning-artifacts/prd.md#Functional Requirements`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md`
- `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-19: Targeted red tests failed for missing participant vocabulary, aggregate participant behavior, and server validation boundary before implementation.
- 2026-05-19: `dotnet test .\tests\Hexalith.Conversations.Contracts.Tests\Hexalith.Conversations.Contracts.Tests.csproj --no-restore` passed.
- 2026-05-19: `dotnet test .\tests\Hexalith.Conversations.Tests\Hexalith.Conversations.Tests.csproj --no-restore` passed.
- 2026-05-19: `dotnet test .\tests\Hexalith.Conversations.Server.Tests\Hexalith.Conversations.Server.Tests.csproj --no-restore` passed.
- 2026-05-19: `dotnet test .\Hexalith.Conversations.slnx --no-restore` passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Confirmed Story 1.2 and 1.3 foundations existed, then completed participant command/event contracts with closed participant type/role vocabularies and documented typed rejection codes.
- Added deterministic aggregate participant handling, replayed membership state keyed by stable Party ID plus type/role, and typed no-success-event rejections for unsafe state, duplicate membership, tenant/context mismatch, unsupported shape, and provider-only identity substitution.
- Added application-boundary `IParticipantDirectory` validation and handler logic so Party proof failures fail closed before aggregate dispatch.
- Added participant contract, aggregate, server-boundary, and privacy/safety tests; full solution tests pass.

### File List

- _bmad-output/implementation-artifacts/1-4-add-conversation-participants-with-stable-party-attribution.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- README.md
- src/Hexalith.Conversations.Contracts/Commands/AddParticipantCommand.cs
- src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs
- src/Hexalith.Conversations.Contracts/Events/ParticipantAdded.cs
- src/Hexalith.Conversations.Contracts/Participants/ParticipantRole.cs
- src/Hexalith.Conversations.Contracts/Participants/ParticipantType.cs
- src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs
- src/Hexalith.Conversations/Commands/AddParticipant.cs
- src/Hexalith.Conversations/Events/ParticipantAddedDomainEvent.cs
- src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs
- src/Hexalith.Conversations/State/ConversationLifecycleState.cs
- src/Hexalith.Conversations/State/ConversationParticipant.cs
- src/Hexalith.Conversations/State/ConversationState.cs
- src/Hexalith.Conversations/Validation/AddParticipantBoundary.cs
- src/Hexalith.Conversations/Validation/AddParticipantValidation.cs
- src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs
- src/Hexalith.Conversations.Server/Hydration/IParticipantDirectory.cs
- src/Hexalith.Conversations.Server/Hydration/ParticipantDirectoryValidation.cs
- src/Hexalith.Conversations.Server/Hydration/ParticipantDirectoryValidationStatus.cs
- tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs
- tests/Hexalith.Conversations.Contracts.Tests/ParticipantContractTest.cs
- tests/Hexalith.Conversations.Server.Tests/AddParticipantCommandHandlerTest.cs
- tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateCreateTest.cs
- tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateParticipantTest.cs
- tests/Hexalith.Conversations.Tests/State/ConversationStateSafetyTest.cs

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-18: Party-mode review applied boundary, privacy, duplicate, fail-closed validation, and replay-test clarifications.
- 2026-05-18: Advanced elicitation applied typed-outcome, validation-order, tenant-seam, duplicate-domain-rule, replay, and privacy proof clarifications.
- 2026-05-19: Implemented participant stable Party attribution, aggregate replay, fail-closed boundary validation, documentation, and tests.

## Party-Mode Review

- ISO date and time: 2026-05-18T12:21:21Z
- Selected story key: `1-4-add-conversation-participants-with-stable-party-attribution`
- Command/skill invocation used: `/bmad-party-mode 1-4-add-conversation-participants-with-stable-party-attribution; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - Story 1.4 is directionally ready, but implementation risk concentrates around aggregate/application boundary leakage, durable Party/provider privacy, duplicate participant semantics, and testable fail-closed behavior.
  - `IParticipantDirectory` belongs at the application boundary only; Contracts and aggregate/domain code must remain infrastructure-free and external-service-free.
  - Stable `PartyId` is the only durable participant attribution authority; provider IDs, display names, Party profiles, raw upstream errors, and provider payloads must not become durable event authority.
  - Cross-tenant unsafe participant additions should fail closed when Party ownership or visibility for the command tenant cannot be proven, while full tenant authorization remains Story 1.5.
- Changes applied:
  - Clarified `ParticipantAdded` durable fields and forbidden contract dependencies.
  - Clarified stable `PartyId` authority and provider identifier limits.
  - Clarified duplicate participant key semantics.
  - Clarified `IParticipantDirectory` placement and fail-closed validation outcomes.
  - Clarified aggregate replay and privacy/fail-closed test expectations.
- Findings deferred:
  - Full tenant authorization matrix remains Story 1.5.
  - Idempotent duplicate-command handling remains Story 1.6.
  - Read-model freshness, Party display hydration, and participant display names remain Story 1.7 or later.
  - Provider-specific identity reconciliation and model registry semantics remain out of scope unless represented as validated Party mappings in a later story.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- ISO date and time: 2026-05-18T23:04:05Z
- Selected story key: `1-4-add-conversation-participants-with-stable-party-attribution`
- Command/skill invocation used: `/bmad-advanced-elicitation 1-4-add-conversation-participants-with-stable-party-attribution`
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
  - The story was already ready for development, but its implementer needed sharper separation between domain duplicate membership, Story 1.6 idempotency, command-time Party proof, and later Story 1.5 tenant authorization.
  - Privacy proof needed to cover secondary leakage surfaces such as snapshots if introduced, diagnostics, `ToString()` methods, rejection payloads, and test fixtures, not only the durable event JSON.
  - Adopter experience is safer when typed rejection/result names are documented up front for duplicate, unsupported participant type, provider-only identity, Party validation unavailable, and tenant-context mismatch outcomes.
- Changes applied:
  - Added explicit typed rejection/result documentation expectations.
  - Clarified validation order and aggregate invocation only after command-time Party proof succeeds.
  - Clarified duplicate participant membership as a domain state rule, not retry-safe idempotency.
  - Added a single tenant-access seam expectation for Story 1.5.
  - Expanded replay and privacy tests to prove validation adapters are not invoked and secondary leakage surfaces stay clean.
- Findings deferred:
  - Full tenant access policy, role matrix, and freshness behavior remain Story 1.5 or later.
  - Retry-safe command idempotency, same-command deduplication, and conflict fingerprinting remain Story 1.6.
  - Read-time display hydration, degraded participant display states, and provider mapping remain Story 1.7 or later explicit provider-mapping work.
- Final recommendation: ready-for-dev
