# Story 1.2: Define Conversation Identity, Command, Event, and Error Contracts

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want clear Conversations contracts for identity, commands, events, projections, and typed errors,
so that I can integrate without learning EventStore internals or depending on unstable implementation details.

## Acceptance Criteria

1. Given the Contracts project exists, when conversation identity contracts are defined, then contracts include tenant-scoped `ConversationId` concepts distinct from provider identifiers, UI labels, external business identifiers, thread names, correlation IDs, causation IDs, idempotency keys, actor references, and upstream message identifiers, and stable reference concepts are available for `TenantId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`, `MessageId`, `BusinessReference`, and provider correlation metadata.
2. Given adopter systems need to create and evolve conversations, when initial command contracts are defined, then the contract package includes create-conversation, append-message, add-participant, attach-file-reference, update-metadata, and close/archive command shapes where release-scoped, and each command includes schema version, tenant binding, correlation/causation metadata, and idempotency support where applicable.
3. Given Conversations persists meaningful domain changes, when initial event contracts are defined, then events use Conversations language, carry schema/version metadata, and store stable IDs rather than Party personal data, provider session authority, raw upstream records, or file binaries, and provider-specific payload metadata is represented only as opaque, tenant-isolated, explicitly versioned extension data.
4. Given adopter systems must handle failures consistently, when typed error contracts are defined, then invalid, unauthorized, conflicting, duplicate, unsupported-version, tenant-mismatched, stale-projection, and hidden-by-tenant-isolation outcomes have documented machine-readable failure semantics, and error shapes are content-safe and do not reveal target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references.
5. Given contracts are public integration surface, when contract tests and documentation checks run, then contract types are serialization-friendly, nullable-clean, centrally packaged, and infrastructure-free, and no public contract exposes raw EventStore envelopes, snapshot mechanics, stream internals, SignalR group names, projection implementation details, provider payloads, raw upstream records, personal display/contact data, file binaries, server package types, or runtime service abstractions.

## Tasks / Subtasks

- [x] Add stable identity and reference contracts in `src/Hexalith.Conversations.Contracts`. (AC: 1, 3, 5)
  - [x] Create `Identifiers/ConversationId.cs`, `TenantId.cs`, `PartyId.cs`, `ProjectId.cs`, `FolderId.cs`, `FileId.cs`, `MessageId.cs`, and `BusinessReference.cs` as serialization-friendly public value contracts.
  - [x] Add provider correlation metadata as opaque metadata, not identity: e.g. `ProviderCorrelationMetadata` with provider name/type, provider session or response identifiers, metadata schema version, and a bounded key/value extension bag.
  - [x] Validate that `ConversationId` is tenant-scoped by contract usage and documentation, while provider IDs, UI labels, external identifiers, upstream message identifiers, thread names, correlation IDs, causation IDs, idempotency keys, and actor references stay separate metadata/reference fields.
  - [x] Add README identity guidance with an ID taxonomy that distinguishes internal conversation identity, tenant binding, Party actor attribution, upstream stable references, provider/thread correlation metadata, UI labels, external business references, and forbidden raw identifiers.
  - [x] Keep all identifier contracts in the Contracts assembly; do not reference `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.Projects`, `Hexalith.Folders`, `Hexalith.EventStore`, or server-only packages from Contracts.

- [x] Add command envelope and initial command contracts. (AC: 2, 5)
  - [x] Create `Commands/ConversationCommandMetadata.cs` with schema version, tenant binding, caller/actor Party ID, correlation ID, optional causation ID, and idempotency key support.
  - [x] Create command shapes for `CreateConversationCommand`, `AppendMessageCommand`, `AddParticipantCommand`, `AttachFileReferenceCommand`, `UpdateConversationMetadataCommand`, `CloseConversationCommand`, and `ArchiveConversationCommand`.
  - [x] Ensure every mutating command carries tenant scope, actor attribution, schema version, correlation/causation metadata, and idempotency metadata where applicable.
  - [x] Make command payloads carry stable IDs and allowed metadata only; they must not carry tokens, claims, raw provider payloads, tenant authorization state, Party personal data, file binaries, raw upstream records, or EventStore envelopes.
  - [x] Keep these as public contract DTOs only. Do not implement validators, handlers, aggregate methods, authorization, persistence, projection updates, EventStore dispatch, repositories, stores, runtime service interfaces, or command pipeline abstractions in this story.

- [x] Add event contract primitives and initial event contracts. (AC: 3, 5)
  - [x] Create `Events/ConversationEventMetadata.cs` with schema version, event type, tenant scope, conversation identity, actor Party ID where applicable, correlation/causation metadata, and committed timestamp metadata expected by public contracts.
  - [x] Create event contracts for `ConversationCreated`, `MessageAppended`, `ParticipantAdded`, `FileReferenceAttached`, `ConversationMetadataUpdated`, `ConversationClosed`, and `ConversationArchived`.
  - [x] Use past-tense Conversations domain names and stable references only. Events must not store Party display names, contact values, person/organization details, provider-owned session IDs as authority, raw prompt/provider payloads, raw upstream records, file binaries, or raw upstream problem details.
  - [x] Represent provider extension data as versioned opaque metadata and keep it tenant-scoped. Do not expose EventStore stream names, sequence numbers, snapshots, checkpoints, expected revisions, SignalR groups, projection names, or internal projection topology.

- [x] Add result, projection, trust/freshness, version, and error contracts needed by adopter-facing semantics. (AC: 2, 4, 5)
  - [x] Create `Results` contracts for command acceptance and stable outcomes, including assigned `ConversationId` for create, accepted command identity/correlation handle, and read-model visibility caveat where relevant.
  - [x] Create minimal projection/read contract shells under `Projections` that expose Conversations vocabulary and freshness/trust state without implementing projection storage, projection state machines, cursoring, rebuild lifecycle, EventStore sequencing, subscription state, tenant materialization behavior, or dispatch behavior.
  - [x] Create `TrustStates` contracts using the approved vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`.
  - [x] Create `Versioning` contracts for active contract/schema version and unsupported-version reporting, with one explicit JSON representation rule for schema version values so command, event, result, error, and projection shells do not drift independently.
  - [x] Create `Errors/ConversationErrorCode.cs` and a content-safe problem/result contract that covers at minimum `tenant_binding_missing`, `tenant_isolation_violation`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `idempotency_conflict`, `aggregate_not_found`, `schema_version_unsupported`, and `command_validation_failed`.
  - [x] Ensure error contracts include machine-readable code/category, retryability where meaningful, correlation/audit handle, optional documentation pointer, and safe field diagnostics, without leaking inaccessible tenant IDs, Party personal data, conversation existence, redacted content, provider identity/payloads, infrastructure/storage details, raw exception text, validation internals, or cross-tenant business references.
  - [x] Keep localized/user-facing display copy out of scope; any safe human-readable text is developer guidance only and must not be the primary machine contract.

- [x] Add focused contract tests and serialization checks. (AC: 1-5)
  - [x] Add tests in `tests/Hexalith.Conversations.Contracts.Tests` proving every public identifier, command, event, result, error, projection shell, trust/freshness, and versioning contract serializes and deserializes with `System.Text.Json` defaults used by web APIs, preserves required fields, and remains nullable-clean.
  - [x] Add representative stable JSON fixture tests for one valid command, event, error, result, projection shell, trust/freshness state, and version metadata contract to catch accidental casing, null emission, enum/value representation, and property-name drift.
  - [x] Add tests proving command and event contracts include tenant scope, schema version, correlation/causation metadata, actor Party attribution where required, and idempotency fields where applicable.
  - [x] Add reflection and serialized-JSON inspection tests proving forbidden personal/provider/file/upstream payload fields are absent from every public exported contract type, including type names, namespaces, public property names, and JSON property names.
  - [x] Extend or replace existing boundary tests so Contracts package references, project references, framework references, and forbidden namespace imports/usings are inspected from `.csproj` XML and source files, not only from `Assembly.GetReferencedAssemblies()`, because marker assemblies may not retain unused references.
  - [x] Add tests proving public contract names and serialized shapes do not expose EventStore or runtime terms such as envelope, EventStore, stream, snapshot, sequence, expected revision, checkpoint, SignalR group, projection topology, projection name, tenant projection internals, handler, dispatcher, repository, store, or EventStore aggregate identity.
  - [x] Add tests proving identifier/default/null cases cannot silently produce ambiguous empty contract values where the public shape requires a stable identity or metadata value.
  - [x] Add fail-closed error fixture tests proving unauthorized, nonexistent, cross-tenant, hidden-by-isolation, stale, unavailable, audit-unavailable, and unsupported-version outputs remain content-safe and non-disclosing.

- [x] Update developer documentation for the contract package. (AC: 4, 5)
  - [x] Add or update `README.md` contract-package guidance that names the supported `.NET client + shared contract package` integration path and explains that raw EventStore knowledge is not required.
  - [x] Document the stable distinction between `ConversationId`, tenant ID, Party ID, external business references, provider correlation metadata, labels, and thread names.
  - [x] Document typed error semantics and hygiene rules, including non-disclosure for cross-tenant and hidden-by-isolation outcomes.
  - [x] Show README examples that use only the client/shared-contract path and do not imply HTTP fallback, EventStore usage, Dapr, UI generation, workers, handlers, persistence, projection execution, or runtime service ownership.
  - [x] Link to readiness decisions and ADR tracker entries that future stories must resolve before behavior implementation, without accepting new ADR decisions in this story.

- [x] Validate and keep the implementation scoped. (AC: 5)
  - [x] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore` or, if restore/build artifacts are stale, run `dotnet restore`, `dotnet build`, and `dotnet test` against `Hexalith.Conversations.slnx`.
  - [x] Do not run recursive submodule initialization. Root-level submodule reads are allowed only where already available.
  - [x] Do not add EventStore, Dapr, ASP.NET Core, FrontComposer, Tenants, Parties, Projects, or Folders dependencies to `Hexalith.Conversations.Contracts`.
  - [x] Do not implement domain behavior, command handlers, aggregate state transitions, EventStore adapters, tenant projection, Party validation, projection stores, UI, workers, or conformance evidence in this story.

## Dev Notes

### Scope Boundary

Story 1.2 creates the public contract language that later stories consume. It should add contract types, serialization/documentation guidance, and contract tests only. It must not implement runtime behavior. Story 1.3 owns aggregate create behavior; Story 1.4 owns participant aggregate behavior; Story 1.4.1 owns append-message behavior; Story 1.6 owns idempotent command handling; Story 1.7 owns read-model freshness metadata behavior; Story 1.10 owns publication behavior; Story 1.11 owns replay and schema-version behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant-Safe Conversation Record`]

The Contracts assembly is the adopter-facing boundary. It must expose Conversations concepts, not EventStore mechanics, and must remain independent of server infrastructure, HTTP clients, Dapr, FrontComposer shell packages, EventStore server/runtime packages, and generated UI files. [Source: `_bmad-output/project-context.md#Critical Implementation Rules`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]

### Current Repository State and Previous Story Intelligence

Story 1.1 created the buildable `.NET 10` scaffold and completed review patches. The current repository includes `src/Hexalith.Conversations.Contracts`, `Client`, domain, `Server`, `Testing`, `AppHost`, `ServiceDefaults`, and focused test projects. Existing marker-only assemblies are intentionally inert. Build files use central package management and target `net10.0` with SDK `10.0.300`. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Completion Notes List`; `global.json`; `Directory.Build.props`; `Directory.Packages.props`]

Carry forward the Story 1.1 review lesson: assembly-reference boundary tests can pass vacuously when marker assemblies do not use a package. For Story 1.2, inspect `.csproj` XML directly for forbidden references in addition to any compiled assembly checks. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Review Findings`; `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`]

Current test style uses xUnit v3, Shouldly, XML inspection for project files, and deterministic test factories in `src/Hexalith.Conversations.Testing`. Keep copyright headers and namespace style aligned with existing files. [Source: `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`; `src/Hexalith.Conversations.Testing/Factories/ConversationTestIds.cs`]

### Contract Shape Guidance

Use C# records or readonly record structs for stable public value contracts where that keeps JSON serialization straightforward. Prefer explicit required constructor parameters or `required` members over mutable optional bags for load-bearing fields. Keep nullable annotations clean and warnings-as-errors clean. [Source: `_bmad-output/project-context.md#Language-Specific Rules`]

The contract package should use JSON-friendly camelCase wire semantics through normal ASP.NET/System.Text.Json defaults, but source property names can remain idiomatic PascalCase. If custom serialization attributes are introduced, keep them in `System.Text.Json`/BCL space; do not introduce Newtonsoft.Json or infrastructure dependencies. [Source: `_bmad-output/planning-artifacts/architecture.md#Format Patterns`; `Directory.Packages.props`]

Stable references for this story should be Conversations-owned contract concepts over string values, not copied upstream runtime behavior. `TenantId`, `PartyId`, `ProjectId`, `FolderId`, and `FileId` are references to upstream-owned identities; Conversations stores stable IDs and resolves mutable display or lifecycle state later through owning modules. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Boundaries`; `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`]

### Identity and Reference Rules

`ConversationId` is an internal tenant-scoped Conversations identity. It is not a provider session ID, UI label, external business identifier, project/folder/file identifier, Party identifier, or thread name. Provider and external identifiers may be stored only as correlation or business-reference metadata and must never replace the internal conversation identity. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.2: Define Conversation Identity, Command, Event, and Error Contracts`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

Contract vocabulary decisions fixed in this story:

- `ConversationId` is the Conversations-owned durable identity and must be interpreted together with tenant scope; do not derive it from provider conversation IDs, UI route IDs, external thread IDs, correlation IDs, causation IDs, idempotency keys, actor references, or upstream message IDs.
- `TenantId`, `PartyId`, `ProjectId`, `FolderId`, and `FileId` are opaque stable reference contracts for upstream-owned identities; this story must not copy upstream runtime behavior, lifecycle state, display data, authorization state, or personal data into Conversations contracts.
- Provider/thread references, external business references, and UI labels are metadata or references only. They are never authority for tenant isolation, aggregate identity, actor attribution, or idempotency.
- Actor attribution may identify the stable actor reference needed by the contract, but must not include display names, emails, phone numbers, avatars, provider user IDs, or upstream Party records.
- Public error contracts are machine-readable first. User-facing localized copy is out of scope for this story; any safe developer guidance must not reveal tenant existence, conversation existence, provider details, personal data, storage internals, or raw upstream failures.

Events and commands may reference Party IDs, but must not persist Party display names, contact values, identifiers beyond the stable Party ID, person details, organization details, or upstream Party problem details. Read-time hydration is future server/client behavior, not a Story 1.2 contract implementation shortcut. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`]

File references are stable Hexalith.Folders file IDs plus allowed metadata. File binaries and raw upstream file records stay out of Conversations events and commands. Project and folder references are stable IDs only and resolve at read time through upstream canonical state. [Source: `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`; `_bmad-output/planning-artifacts/architecture.md#Data Boundaries`]

### Command and Event Metadata

Every public command shape must carry tenant binding, caller/actor Party attribution, schema version, correlation ID, optional causation ID, and idempotency support where applicable. The PRD command envelope names these fields as tenant ID, caller Party ID, correlation ID, idempotency key, command type, payload, and schema version. [Source: `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`]

Events are immutable, versioned Conversations contracts. Event metadata must carry schema/version metadata, tenant scope, conversation identity, event type, correlation/causation metadata, actor Party ID where applicable, and timestamp metadata expected by public contracts. Do not expose raw EventStore envelope, stream, snapshot, expected revision, or storage position details as public contract fields. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#EventStore envelope stability and evolution ownership`]

Use Conversations domain names for event contracts: `ConversationCreated`, `MessageAppended`, `ParticipantAdded`, `FileReferenceAttached`, `ConversationMetadataUpdated`, `ConversationClosed`, and `ConversationArchived`. Rejection/failure semantics should be typed errors/results in this story; rejection events belong only if they are explicit contract events and do not imply runtime aggregate behavior. [Source: `_bmad-output/planning-artifacts/architecture.md#Naming Patterns`; `_bmad-output/planning-artifacts/epics.md#Story 1.2: Define Conversation Identity, Command, Event, and Error Contracts`]

### Typed Error and Trust Vocabulary

Typed errors must be machine-readable and content-safe. Cover at minimum: invalid command shape, unauthorized/forbidden, conflict, duplicate/idempotency conflict, unsupported schema version, tenant mismatch, stale tenant projection, hidden-by-tenant-isolation/nonexistent aggregate, audit sink unavailable, and audit pairing required. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.2: Define Conversation Identity, Command, Event, and Error Contracts`; `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`]

The v1 error-code list already names `tenant_binding_missing`, `tenant_isolation_violation`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `idempotency_conflict`, `aggregate_not_found`, `schema_version_unsupported`, and `command_validation_failed`. The story may add contract categories or aliases only if tests and docs keep non-disclosure behavior clear. [Source: `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`]

Trust/freshness state vocabulary is approved as `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`. Story 1.2 can define the contract enum/value but must not implement projection freshness behavior. Future stories that do not explicitly accept degraded freshness states must block on anything except `Current`. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]

### Anti-Reinvention and Non-Disclosure Warnings

- Do not introduce transcript tables, repository abstractions, cache-backed authorities, provider session stores, or memory stores.
- Do not copy source from sibling modules. Reference concepts as stable IDs and let future adapters integrate through approved boundaries.
- Do not expose EventStore internals in public contract names, XML docs, JSON property names, README examples, results, errors, or tests.
- Do not treat provider session IDs, external business IDs, UI labels, thread names, or generated route names as source-of-truth identity.
- Do not add raw HTTP fallback examples as the normal integration path; v1 supports the `.NET client + shared contract package` path first.
- Do not make unauthorized, nonexistent, or cross-tenant conversation states distinguishable through error text, timing assumptions, or field names.

[Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#.NET client versus raw HTTP fallback policy`; `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`]

### File and Test Placement

Expected production files belong under:

- `src/Hexalith.Conversations.Contracts/Identifiers`
- `src/Hexalith.Conversations.Contracts/Commands`
- `src/Hexalith.Conversations.Contracts/Events`
- `src/Hexalith.Conversations.Contracts/Errors`
- `src/Hexalith.Conversations.Contracts/Projections`
- `src/Hexalith.Conversations.Contracts/Results`
- `src/Hexalith.Conversations.Contracts/TrustStates`
- `src/Hexalith.Conversations.Contracts/Versioning`
- `src/Hexalith.Conversations.Contracts/Serialization` only when needed for BCL/System.Text.Json converters or constants, not runtime behavior

Expected tests belong under `tests/Hexalith.Conversations.Contracts.Tests`, with shared deterministic factories added to `src/Hexalith.Conversations.Testing` only when they are reusable and do not smuggle runtime behavior. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`]

### Validation

Run the solution tests after implementing contract files. If `--no-restore` fails due to stale assets, run a full restore/build/test sequence against `Hexalith.Conversations.slnx`. Validation must not require Aspire runtime launch, Dapr sidecars, tenant seed data, production secrets, provider credentials, external cloud resources, or nested submodule initialization. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Testing Standards`; `README.md`]

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.2: Define Conversation Identity, Command, Event, and Error Contracts`
- `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/prd.md#Data Schemas & Wire Formats`
- `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `Directory.Build.props`
- `Directory.Packages.props`
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build .\src\Hexalith.Conversations.Contracts\Hexalith.Conversations.Contracts.csproj --no-restore` - passed.
- `dotnet test .\tests\Hexalith.Conversations.Contracts.Tests\Hexalith.Conversations.Contracts.Tests.csproj --no-restore` - passed, 12 tests.
- `dotnet test .\Hexalith.Conversations.slnx --no-restore` - passed, 39 tests across 5 test projects.

### Implementation Plan

- Keep Story 1.2 contract-only: public DTO/value records, version/trust/error vocabularies, README guidance, and tests only.
- Use stable Conversations-owned identity and upstream reference contracts without taking dependencies on sibling runtime modules.
- Guard public JSON and source boundaries with serialization fixture, reflection, `.csproj`, and source-import tests.

### Completion Notes List

- Review follow-up patches resolved for primitive JSON wire shapes, closed vocabularies, validation guardrails, safe error hygiene, boundary tests, and documentation alignment.
- Added stable identifier/reference contracts, opaque provider correlation metadata, schema-version contracts, and trust/freshness vocabulary.
- Added command, event, result, projection, and content-safe error contracts without handlers, validators, persistence, projection execution, UI, or server/runtime abstractions.
- Added contract serialization fixtures, metadata coverage tests, forbidden public-surface tests, identifier validation tests, fail-closed error tests, and direct project/source boundary checks.
- Updated README contract-package guidance for the supported `.NET client + shared contract package` integration path, identity taxonomy, safe error semantics, freshness states, readiness decisions, and ADR tracker.

## Party-Mode Review

- Date: 2026-05-18T11:00:03Z
- Selected story key: `1-2-define-conversation-identity-command-event-and-error-contracts`
- Command/skill invocation used: `/bmad-party-mode 1-2-define-conversation-identity-command-event-and-error-contracts; review;`
- Participating BMAD agents: Winston (System Architect), John (Product Manager), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - Identity taxonomy needed sharper adopter-facing separation between `ConversationId`, tenant binding, upstream stable references, provider/thread metadata, UI labels, external business references, correlation/causation identifiers, idempotency keys, and actor attribution.
  - Contract-only boundaries needed explicit guards against handlers, dispatch, repositories, stores, validators, projection execution, EventStore details, SignalR groups, runtime service interfaces, and projection topology entering the public contract package.
  - Typed errors needed stronger non-disclosure examples, including safe machine-readable codes/categories and no leakage of tenant existence, conversation existence, provider details, personal data, storage internals, raw exceptions, or upstream failures.
  - Serialization and boundary tests needed to cover every public contract family, representative JSON fixtures, forbidden type/property/namespace/JSON names, `.csproj` references, forbidden imports/usings, nullable/default traps, and fail-closed error fixtures.
- Changes applied:
  - Expanded AC1 and AC5 to name additional identity distinctions and forbidden public contract surfaces.
  - Added task-level guidance for ID taxonomy documentation, contract-only command/event/projection boundaries, explicit schema-version JSON representation, error non-disclosure, JSON fixture tests, reflection/JSON forbidden-surface tests, boundary/import checks, default/null tests, and fail-closed error fixtures.
  - Added Dev Notes contract vocabulary decisions for identity, stable upstream references, provider/thread metadata, actor attribution, and machine-readable error copy.
  - Added `Serialization` file placement guidance limited to BCL/System.Text.Json converters or constants.
- Findings deferred:
  - Exact identifier backing format (`Guid`, `Ulid`, opaque string, or value object).
  - Exact actor attribution naming and whether actor category/type is included.
  - Exact schema-version primitive representation beyond requiring one explicit JSON rule in this story.
  - Whether public errors use closed enum-like vocabulary or extensible string codes, and whether safe developer text is included.
  - Whether trust/freshness vocabulary remains enum-like or becomes an extensible versioned structure.
- Final recommendation: ready-for-dev

### File List

- `README.md`
- `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Conversations.Contracts/Commands/AddParticipantCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/ArchiveConversationCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/AttachFileReferenceCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/CloseConversationCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/UpdateConversationMetadataCommand.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCategory.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorResult.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationArchived.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationClosed.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationCreated.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventType.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationMetadataUpdated.cs`
- `src/Hexalith.Conversations.Contracts/Events/FileReferenceAttached.cs`
- `src/Hexalith.Conversations.Contracts/Events/MessageAppended.cs`
- `src/Hexalith.Conversations.Contracts/Events/ParticipantAdded.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/BusinessReference.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/ConversationId.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/FileId.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/FolderId.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/MessageId.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/PartyId.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/ProjectId.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/TenantId.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationMessageProjection.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjection.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshness.cs`
- `src/Hexalith.Conversations.Contracts/Results/ConversationCommandAcceptedResult.cs`
- `src/Hexalith.Conversations.Contracts/Results/ConversationCommandType.cs`
- `src/Hexalith.Conversations.Contracts/Results/ConversationCreatedResult.cs`
- `src/Hexalith.Conversations.Contracts/Results/ReadModelVisibility.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ConversationIntValueJsonConverter.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ConversationStringValueJsonConverter.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/IdentifierJsonConverters.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ProjectionTrustStateJsonConverter.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/SchemaVersionJsonConverter.cs`
- `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractVersionInfo.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractMetadataTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs`

### Review Findings

_Generated 2026-05-18 by `/bmad-code-review 1.2` against commit `5ba11d5`. Sources: Blind Hunter (adversarial, diff-only), Edge Case Hunter (branch/boundary walk), Acceptance Auditor (AC compliance — verdict `approve`)._

#### Decision-needed (resolved 2026-05-18 — derived patches added below)

- [x] [Review][Decision] **RESOLVED**: JSON wire shape `{"value":...}` envelope for every strongly-typed ID, `SchemaVersion`, and `ProjectionTrustState` — every payload nests one extra level per identifier and per version field. Three viable directions: (a) add a `JsonConverter<T>` per wrapper that projects to a plain primitive (`"tenant-001"` / `1` / `"Current"`); (b) keep the envelope and document it explicitly as the canonical wire shape; (c) keep envelope but expose a typed factory. Story AC5 promises "one explicit JSON representation rule for schema version values"; current implementation pins envelope shape via fixture but never decides whether envelope is the rule. Sources: blind 2.1/2.2/2.3; edge H5 (cross-type rehydration is closed by option (a)).
- [x] [Review][Decision] **RESOLVED**: Closed vocabulary enforcement for `ConversationError.Code`, `ConversationError.Category`, `ConversationCommandAcceptedResult.CommandType`, `ConversationEventMetadata.EventType`, `ProjectionTrustState.Value` — all are bare `string`. `ConversationErrorCode` constants exist but the type does not couple to them. Choice: (a) convert to enum + `JsonStringEnumConverter`; (b) introduce a closed-vocabulary primitive `record ConversationErrorCode(string Value)` with private ctor and static well-known instances; (c) keep open-string and add reflection-based contract tests asserting only known values are emitted. Sources: blind 2.4/2.5/2.6/1.9; edge H3/H8/M10.
- [x] [Review][Decision] **RESOLVED**: `ConversationCommandAcceptedResult.ConversationId` is non-nullable but the result is intended for every mutating command, including `CreateConversationCommand` where the ID is server-assigned. Asymmetry: `ConversationCreatedResult` carries no `CommandType`, while `ConversationCommandAcceptedResult` does. Resolve scope: (a) make `ConversationId` nullable on the generic accepted result; (b) keep non-null and document that create-flow always uses `ConversationCreatedResult`; (c) unify both results. Source: blind 1.13/4.7.
- [x] [Review][Decision] **RESOLVED**: `ConversationEventMetadata` has no stable per-event identifier — dedup, replay-correlation, and audit-handle scenarios in downstream stories (1.6 idempotency, 1.10 publication, 1.11 replay) typically rely on `EventId`. Decide now whether to add `EventId : Guid|Ulid|string` at the contract layer or document the omission and require downstream stories to compose one. Source: blind 2.17.
- [x] [Review][Decision] **RESOLVED**: `MessageAppended.Text` is an unbounded inline `string` of message body content. Story 1.2 spec is silent on whether message text is allowed inline at the contract layer vs. an opaque content-addressable reference; no length cap, no encoding contract, no chunking guidance. Decide whether to (a) keep inline `Text` with documented constraints, (b) replace with `MessageContentReference` opaque ID + adapter resolution at read time, (c) defer to Story 1.4.1 (append-message behavior) and add a TODO marker now. Source: blind 2.18; acceptance auditor noted in AC3.

**Resolution summary (5/5 chose Recommended):**
1. Wire shape → add `JsonConverter<T>` per wrapper; primitives on the wire. Closes edge H5 (cross-type rehydration).
2. Vocabulary → closed-set value-object per family (`record Foo(string Value)` with private ctor + static instances + `JsonConverter`). Supersedes original Patch entry on `ProjectionTrustState` arbitrary-string fix.
3. Result asymmetry → keep `ConversationCommandAcceptedResult.ConversationId` non-null; add `CommandType` to `ConversationCreatedResult`; document split.
4. EventId → add required `string EventId` to `ConversationEventMetadata`; producers choose representation.
5. `MessageAppended.Text` → keep inline string for v1; add XML-doc TODO referencing Story 1.4.1 for length cap, encoding, and inline-vs-reference policy.

#### Patch (unambiguous fixes)

##### Patches derived from resolved decisions

- [x] [Review][Patch][D1] Add `JsonConverter<T>` for `ConversationId`, `TenantId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`, `MessageId` that read/write a plain JSON string and route through the validating ctor on read. [src/Hexalith.Conversations.Contracts/Identifiers/*.cs + src/Hexalith.Conversations.Contracts/Serialization/]
- [x] [Review][Patch][D1] Add `JsonConverter<SchemaVersion>` that reads/writes a plain integer (`1`) and routes through `SchemaVersion`'s validating ctor. [src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs + Serialization/]
- [x] [Review][Patch][D1] Add `JsonConverter<ProjectionTrustState>` that reads/writes a plain string (`"Current"`) and resolves to the closed-set static instance. [src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs + Serialization/]
- [x] [Review][Patch][D1] Decide and apply wire shape for `BusinessReference` and `ProviderCorrelationMetadata` — they are compound records, not single-value wrappers; default is to keep their object shape (no envelope to flatten). Document in README.
- [x] [Review][Patch][D1] Update every fixture in `ContractSerializationTest` and `ContractSamples` to assert the new flat shape (e.g. `"tenantId":"tenant-001"`, `"schemaVersion":1`, `"trustState":"Current"`). [tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs; ContractSamples.cs]
- [x] [Review][Patch][D1] Add cross-type rehydration test: assert that `JsonSerializer.Deserialize<ConversationId>(JsonSerializer.Serialize(new TenantId("x")))` either fails or is detectable (the converter ties JSON string → typed wrapper at deserialize time only when reading the correct property; round-trip via raw string proves the system-wide cross-type ambiguity is no longer silent). [tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs]
- [x] [Review][Patch][D2] Convert `ConversationError.Code` from `string` to a closed-set value-object `ConversationErrorCode` (record with private ctor + static well-known instances `TenantBindingMissing`, `TenantIsolationViolation`, etc.; `Parse(string)` factory rejects unknown values; `JsonConverter` flattens to/from string). Migrate the existing `ConversationErrorCode` constants class into this value-object. [src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs; Errors/ConversationError.cs]
- [x] [Review][Patch][D2] Convert `ConversationError.Category` to a closed-set value-object `ConversationErrorCategory` (or document mapping from `ConversationErrorCode → ConversationErrorCategory` and remove the `Category` field if it duplicates code semantics). [src/Hexalith.Conversations.Contracts/Errors/]
- [x] [Review][Patch][D2] Convert `ConversationCommandAcceptedResult.CommandType` to a closed-set value-object `ConversationCommandType` (static instances for each of the 7 command shapes); same for `ConversationCreatedResult.CommandType`. [src/Hexalith.Conversations.Contracts/Results/]
- [x] [Review][Patch][D2] Convert `ConversationEventMetadata.EventType` to a closed-set value-object `ConversationEventType` (static instances for each of the 7 event shapes). [src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs]
- [x] [Review][Patch][D2] Convert `ProjectionTrustState` from arbitrary-string wrapper to closed-set value-object (the six approved values: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted`). Private ctor + `Parse(string)` + `JsonConverter`. Supersedes the original "ProjectionTrustState arbitrary strings" patch below. [src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs]
- [x] [Review][Patch][D3] Add `string CommandType` to `ConversationCreatedResult` for symmetry with `ConversationCommandAcceptedResult`. Update fixture in `ContractSerializationTest`. Document in README that create-flow always uses `ConversationCreatedResult` and `ConversationCommandAcceptedResult.ConversationId` is non-null because it targets an existing aggregate. [src/Hexalith.Conversations.Contracts/Results/ConversationCreatedResult.cs; README.md; ContractSerializationTest.cs]
- [x] [Review][Patch][D4] Add required `string EventId` to `ConversationEventMetadata` (placed after `SchemaVersion` per the chosen design). Validate `ThrowIfNullOrWhiteSpace`. [src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs]
- [x] [Review][Patch][D4] Update every event fixture and sample to populate `EventId`. Add `ContractMetadataTest` assertion that `ConversationEventMetadata` exposes `EventId` of type `string`. [tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs; ContractSerializationTest.cs; ContractMetadataTest.cs]
- [x] [Review][Patch][D5] Add XML-doc TODO on `MessageAppended.Text` referencing Story 1.4.1 for length cap, encoding contract, and inline-vs-reference policy. No behavior change in this story. [src/Hexalith.Conversations.Contracts/Events/MessageAppended.cs]

##### Patches from original review (Blind / Edge / Auditor)

- [x] [Review][Patch] `init`-able `Value` property on every validated wrapper defeats the ctor guard — `new ConversationId("x") with { Value = "" }` succeeds; same for `TenantId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`, `MessageId`, `BusinessReference.System`/`.Value`, `ProjectionTrustState`, `SchemaVersion`. Convert to positional records or move validation into the `init` setter. [src/Hexalith.Conversations.Contracts/Identifiers/*.cs, TrustStates/ProjectionTrustState.cs, Versioning/SchemaVersion.cs]
- [x] [Review][Patch] ~~`ProjectionTrustState` accepts any non-whitespace string~~ — **superseded by [D2] closed-set value-object patches above.**
- [x] [Review][Patch] `ContractSerializationTest.PublicContractsShouldRoundTripWithSystemTextJsonWebDefaults` asserts `ShouldNotBeNull` only — not a round-trip. Records support value equality; add `deserialized.ShouldBe(sample)`. Replace `AssertJsonEquivalent` (which compares whitespace-sensitive `JsonElement.ToString()`) with `JsonNode.DeepEquals` or canonical re-serialization. [tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs:30-58]
- [x] [Review][Patch] No test covers JSON paths that bypass the validating ctor: `Deserialize("{}", ...)`, `Deserialize("{\"value\":\"\"}", ...)`, `Deserialize("{\"value\":null}", ...)`, `Deserialize("{\"value\":\"  \"}", ...)`. Add per-identifier negative-deserialization test that asserts each malformed payload either throws or surfaces a detectable invalid state. [tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs]
- [x] [Review][Patch] `IdentifierValidationTest` never exercises the `null` branch — only `string.Empty` (and one whitespace case for `TenantId`). `ArgumentException.ThrowIfNullOrWhiteSpace` throws different exception subtypes per input; cover `null` + `"\t"` + `"\n"` per identifier. [tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs]
- [x] [Review][Patch] `ForbiddenPublicSurfaceTest` uses case-insensitive substring matching against legitimate vocabulary (`"Stream"` matches `"Upstream"`, `"Person"` matches `"Personal"`, `"Raw"` matches `"draw"`). Convert to word-boundary regex or AST-based name scan. Also: `SerializedJsonShouldAvoidForbiddenTerms` scans full serialized JSON values, not just keys/property names. [tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs:80-89]
- [x] [Review][Patch] Forbidden-terms list missing four spec-listed terms: `Store`, `Subscription`, `AggregateIdentity`, `RawUpstream`. Add to the audit list. [tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs]
- [x] [Review][Patch] `FailClosedErrorsShouldRemainContentSafe` is tautological — it asserts that a curated sample doesn't contain strings the test itself controls. Two problems: (1) it never proves the contract excludes leaks, only that the sample is curated; (2) it covers only 5 of the 9 listed codes (`TenantBindingMissing`, `AuditPairingRequired`, `IdempotencyConflict`, `CommandValidationFailed` are not asserted). Construct an unsafe-shaped error fixture and assert the contract surface excludes/strips it; extend to all 9 codes. [tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs]
- [x] [Review][Patch] `ContractsSourceFilesShouldNotImportForbiddenNamespaces` uses substring `using <prefix>` match — misses `global using`, inline fully-qualified type references, multi-line `using`s, and `using static`. Replace with Roslyn `SyntaxTree.GetCompilationUnitRoot().Usings`. [tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs:78-95]
- [x] [Review][Patch] `ContractsProjectFileShouldNotDeclareForbiddenReferences` uses 5-level relative `..` path traversal — brittle under `<UseArtifactsOutput>` or alternate `--output`. Walk upward from `AppContext.BaseDirectory` until `.git` or `Hexalith.Conversations.slnx` is found. [tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs]
- [x] [Review][Patch] `ContractMetadataTest` asserts only property *names* on metadata types — a future change replacing `TenantId TenantId` with `string TenantId` slips through. Add type-equality assertions. Also: `propertyInfo.ShouldNotBeNull(...)` is fine (extension), but assertion message is `commandType.Name` only — include property name for diagnostic. [tests/Hexalith.Conversations.Contracts.Tests/ContractMetadataTest.cs]
- [x] [Review][Patch] `SafeError` test fixture hardcodes `IsRetryable=false` for every code, including `IdempotencyConflict` and `TenantProjectionStale` which are typically retryable. Parameterize per code; pin retryability semantics in fixtures so README's "retryability where meaningful" claim is enforced. [tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs]
- [x] [Review][Patch] `ContractVersionInfo` and `UnsupportedSchemaVersion` do not validate `minimumSupportedSchemaVersion <= activeSchemaVersion`. Add ctor validation; add test. [src/Hexalith.Conversations.Contracts/Versioning/ContractVersionInfo.cs]
- [x] [Review][Patch] `SchemaVersion` rejects `< 1` (test covers 0) but `-1`, `int.MinValue`, `int.MaxValue` are not tested. Add boundary cases. [tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs]
- [x] [Review][Patch] `ConversationErrorResult.Errors` accepts empty list and `null` entries — wire payload `{"errors":[]}` round-trips silently. Add validation (`Count > 0`, no null elements) + test. [src/Hexalith.Conversations.Contracts/Errors/ConversationErrorResult.cs]
- [x] [Review][Patch] `ProjectionFreshness.ObservedAt`, `ConversationEventMetadata.CommittedAt`, `ConversationCreated.CreatedAt`, `ConversationMessageProjection.CreatedAt` accept `default(DateTimeOffset)` (`0001-01-01`). Reject `<= DateTimeOffset.MinValue` or require Kind/Offset; add tests. [src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshness.cs; Events/ConversationEventMetadata.cs; Events/ConversationCreated.cs; Projections/ConversationMessageProjection.cs]
- [x] [Review][Patch] `ConversationCommandMetadata.CorrelationId` and `ConversationError.CorrelationId` are typed `string` (non-nullable) but allow `""` / whitespace via positional ctor. Add `ArgumentException.ThrowIfNullOrWhiteSpace`. [src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs; Errors/ConversationError.cs]
- [x] [Review][Patch] `CloseConversationCommand.ReasonCode` and `ArchiveConversationCommand.ReasonCode` are `string?` defaulted `null` but accept `""` when supplied. Add `ThrowIfNullOrWhiteSpace` when value is non-null. [src/Hexalith.Conversations.Contracts/Commands/CloseConversationCommand.cs; ArchiveConversationCommand.cs]
- [x] [Review][Patch] `ProviderCorrelationMetadata.ProviderName` and `ProviderType` accept empty/whitespace strings. Add validation. [src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs]
- [x] [Review][Patch] `ConversationEventMetadata`: `CausationId` and `ActorPartyId` are positional non-defaulted nullables; ergonomics differ from `ConversationCommandMetadata` which defaults `CausationId = null, IdempotencyKey = null`. Add matching defaults. [src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs]
- [x] [Review][Patch] README canonical error-code list omits `tenant_binding_missing` though the constant is defined in `ConversationErrorCode`. Add the entry or remove the constant. [README.md; src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs]
- [x] [Review][Patch] Story Completion Notes line "Ultimate context engine analysis completed - comprehensive developer guide created" has no corresponding artifact in the File List. Remove or replace with a truthful summary line. [_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md]
- [x] [Review][Patch] `<exception cref="ArgumentException">` XML documentation is present only on `ConversationId.cs`; the other 7 identifier wrappers + `BusinessReference` + `ProjectionTrustState` + `SchemaVersion` throw the same exception but don't document it. Propagate. [src/Hexalith.Conversations.Contracts/Identifiers/*.cs; TrustStates/ProjectionTrustState.cs; Versioning/SchemaVersion.cs]
- [x] [Review][Patch] `ProviderCorrelationShouldNotReplaceConversationIdentity` test is tautological — it asserts two unrelated string literals are unequal. Replace with a type-system check (e.g., assert `typeof(ProviderCorrelationMetadata).GetProperty("ProviderSessionReference")?.PropertyType != typeof(ConversationId)`) or remove. [tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs]
- [x] [Review][Patch] `using System.Xml.Linq;` ordered after `using Xunit;` in `ContractsAssemblyBoundaryTest.cs` — violates the `System.*`-first ordering used elsewhere in the codebase. [tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs]
- [x] [Review][Patch] `ConversationSummaryProjection.ParticipantPartyIds : IReadOnlyList<PartyId>?` is nullable; prefer non-null with `Array.Empty<PartyId>()` default to spare adopter null-checks. [src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjection.cs]
- [x] [Review][Patch] Fixture coverage is sparse — AC5 calls for "one valid command, event, error, result, projection shell, trust/freshness state, and version metadata contract" (covered at family level) but per-shape JSON drift in `MessageAppended`, `ParticipantAdded`, `FileReferenceAttached`, `ConversationClosed`, `ConversationArchived`, `ConversationMetadataUpdated`, `ConversationCommandAcceptedResult`, `ConversationErrorResult`, `ContractVersionInfo`, `UnsupportedSchemaVersion`, `ConversationMessageProjection`, `ProjectionFreshness` is not pinned. Add fixtures (or document explicitly that family-level coverage is sufficient). [tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs]
- [x] [Review][Patch] No fixture pins the `causationId: null` JSON emission shape — the null case is unobserved. Add. [tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs; ContractSerializationTest.cs]
- [x] [Review][Patch] `ProjectionFreshness.SchemaVersion` property collides nominally with the parent metadata's `SchemaVersion`. Rename to e.g. `ProjectionContractSchemaVersion` for adopter clarity. [src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshness.cs]

#### Deferred (pre-existing, out-of-scope, or downstream)

- [x] [Review][Defer] Free-form `IReadOnlyDictionary<string,string>` extension bags — `ProviderCorrelationMetadata.ExtensionData`, `UpdateConversationMetadataCommand.Attributes`, `ConversationMetadataUpdated.Attributes`, `ConversationError.SafeFieldDiagnostics` — claimed as "bounded" by README/story but no size cap, no forbidden-key enforcement, no allowed-key list. Real fix is policy + governance, likely in Story 1.10/Epic 2. [src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs; Commands/UpdateConversationMetadataCommand.cs; Events/ConversationMetadataUpdated.cs; Errors/ConversationError.cs] — deferred, scope belongs to governance epic
- [x] [Review][Defer] No `[JsonPolymorphic]` / discriminator on commands or events — adopters who round-trip heterogeneous lists must hand-roll. Acceptable at contract-only stage; revisit when wire transport is defined. — deferred, post-contract concern
- [x] [Review][Defer] README "raw EventStore knowledge is not required" claim is somewhat overstated given the `{"value":...}` envelope ergonomics. Revisit wording after Decision-needed #1 (wire shape) is resolved. [README.md] — deferred, pending wire-shape decision
- [x] [Review][Defer] `BusinessReference.System` property name collides with `System` namespace; no separator constraint between `System` and `Value` (e.g., `"crm:case" + "123"` vs `"crm" + "case:123"` produce indistinguishable joined tuples). Renaming is a breaking contract change. [src/Hexalith.Conversations.Contracts/Identifiers/BusinessReference.cs] — deferred, contract-evolution decision
- [x] [Review][Defer] `ConversationError.Documentation : Uri?` accepts non-https, relative, `javascript:` URIs. Minor surface; restrict in error-handling layer. [src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs] — deferred, narrow risk at contract layer
- [x] [Review][Defer] Cross-identifier-type silent rehydration ambiguity — `JsonSerializer.Deserialize<ConversationId>(JsonSerializer.Serialize(new TenantId("x")))` succeeds because both wrap `{"value":"x"}`. Closed implicitly by Decision-needed #1 if a typed converter projects to primitives. — deferred, linked to wire-shape decision
- [x] [Review][Defer] Unicode / surrogate-pair / very-long ID inputs untested at the contract layer. Runtime story will need length caps. [src/Hexalith.Conversations.Contracts/Identifiers/*.cs] — deferred, runtime concern
- [x] [Review][Defer] `ConversationSummaryProjection.ParticipantPartyIds` allows duplicate `PartyId` entries — producer-side concern; not a contract-level rejection. [src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjection.cs] — deferred, producer hygiene
- [x] [Review][Defer] `_bmad-output/process-notes/predev-preflight-latest.json` is committed with `"result": "fail"` alongside `Status: review`. Should be cleaned but not blocking. [_bmad-output/process-notes/predev-preflight-latest.json] — deferred, process artifact

#### Round 2 — 2026-05-18 (post-patch audit, diff `5ba11d5..HEAD`)

_Re-review of the patches applied in `b5447a6`. Sources: Blind Hunter, Edge Case Hunter, Acceptance Auditor (verdict `approve` with cosmetic notes). 6 decision-needed, 22 patch, 3 defer, 5 dismissed. The Acceptance Auditor approved AC compliance and confirmed all five D1–D5 resolutions landed; the issues below are correctness, validation, and test-honesty regressions surfaced by the adversarial layers._

##### Decision-needed (Round 2) — resolved 2026-05-18

User accepted "follow best practices" for all six. Resolutions captured below; derived patches appended to the Patch list as R2-P23 through R2-P28.

1. **R2-D1 → per-type prefix.** Adopt URN-style prefixed flat strings: `tenant:001`, `conv:001`, `party:001`, `project:001`, `folder:001`, `file:001`, `message:001`. Each identifier `JsonConverter` writes `"<prefix>:<value>"` and validates the prefix on Read; the closed cross-type behavior is enforced by a per-type prefix registry. Industry pattern (Stripe, AWS ARN, URN). Wire-shape break is accepted because v1 is pre-release.
2. **R2-D2 → strict integer only.** `SchemaVersion` JSON wire shape is canonical `int`. `1.0`, `1e0`, `"1"` are rejected. Documented in README.
3. **R2-D3 → ordinal-strict on closed vocabularies.** `Parse` and JSON Read continue to use `StringComparer.Ordinal`. Canonical PascalCase is the only legal encoding. Documented in README.
4. **R2-D4 → extended blocklist applied to all free-text fields.** Blocklist expanded to the spec-listed forbidden terms (EventStore/stream/snapshot/dispatcher/handler/repository/store/aggregate-identity plus the original five). Applied to `DeveloperGuidance`, `SafeFieldDiagnostics` keys and values, `CorrelationId`, and `AuditHandle`. README documents that blocklists are best-effort; the primary non-disclosure mechanism is the closed-vocabulary `Code`/`Category`.
5. **R2-D5 → require ActorPartyId, keep CausationId optional.** `ConversationEventMetadata.ActorPartyId` becomes required positional; `CausationId` stays optional with default null (first events in a causation chain legitimately have no upstream cause). Matches `ConversationCommandMetadata` shape.
6. **R2-D6 → move CommandType to last position.** `ConversationCreatedResult` constructor re-orders `CommandType` to the trailing position so existing positional adopter call sites remain valid. `[JsonPropertyOrder]` not introduced.

Original decision text retained below for traceability.

- [x] [Review][Decision][R2-D1] **Cross-type identifier JSON substitution remains silent.** Prior review's D1 patch list explicitly said: "the converter ties JSON string → typed wrapper at deserialize time only when reading the correct property; round-trip via raw string proves the system-wide cross-type ambiguity is no longer silent." The implementation made the opposite true: `FlatIdentifierJsonShouldDependOnDestinationContractType` in `IdentifierValidationTest.cs` asserts `JsonSerializer.Deserialize<ConversationId>(JsonSerializer.Serialize(new TenantId("tenant-001"))).Value == "tenant-001"` is the expected behavior. Pick one: (a) accept silent cross-type substitution as the v1 wire policy (close the gap by removing the prior D1 promise from the docs); (b) add a per-type marker/prefix (e.g., `"tenant:tenant-001"` / `"conv:conv-001"`); (c) wrap in a typed JSON object (regress on D1). Sources: blind-critical, blind-test-theatre, edge-medium.
- [x] [Review][Decision][R2-D2] **`SchemaVersion` JSON converter strictness.** `ConversationIntValueJsonConverter.Read` rejects `1.0`, `1e0`, `1.5` because it uses `TryGetInt32` on `JsonTokenType.Number`. JS clients regularly emit integers with a trailing `.0` via `JSON.stringify(1.0)`. Pick one: (a) keep strict integer-only (today's behavior, must be documented in README); (b) accept any numeric that round-trips to int (`reader.GetDouble() % 1 == 0`); (c) accept JSON string `"1"` as well, matching identifier flat-string convention. Source: edge-high.
- [x] [Review][Decision][R2-D3] **Closed-vocabulary case sensitivity.** `ConversationErrorCode.Parse`, `ConversationErrorCategory.Parse`, `ConversationEventType.Parse`, `ConversationCommandType.Parse`, `ProjectionTrustState.Parse` all use `StringComparer.Ordinal`. JS adopters working in camelCase will emit `"current"` (lowercase) and get a runtime throw. Pick one: (a) keep ordinal-strict (canonical PascalCase wire); (b) ordinal-ignore-case on `Parse`/JSON read while keeping canonical capitalization on `Write`. Source: edge-medium.
- [x] [Review][Decision][R2-D4] **`ConversationError.ValidateSafeText` blocklist is a 5-term substring filter.** Today blocks only `"other-tenant"`, `"exists"`, `"redacted content"`, `"provider-a"`, `"storage"` against `DeveloperGuidance` and `SafeFieldDiagnostics` keys/values. Three concrete problems: (a) substring `"exists"` triggers false positives on legitimate English (`"non-existent"`, `"co-exists"`, `"pre-exists"`); (b) substring `"storage"` triggers on `"blob storage limits"`, `"storage layer"`; (c) `CorrelationId` and `AuditHandle` (both free-text fields documented as "safe") are not validated at all. Pick one: (a) ratify the current minimal blocklist as illustrative and downgrade README guarantee; (b) replace with a closed-token allowlist that rejects all free-text except enumerated values; (c) extend to a serious blocklist (the spec's full forbidden-term set) and apply to all free-text fields. Sources: blind-critical, edge-high, auditor-minor.
- [x] [Review][Decision][R2-D5] **`ConversationEventMetadata` weakened `ActorPartyId` and `CausationId` from required-positional to optional.** Prior review/spec describes these as required attribution and causation metadata ("Every public event must carry … actor Party ID where applicable … causation metadata"). Current signature: `ActorPartyId = null, CausationId = null` with default null. Pick one: (a) keep optional (today's behavior, document that producers may emit null); (b) require both positionally (the original story 1.2 intent); (c) require `ActorPartyId` but keep `CausationId` optional (matches `ConversationCommandMetadata` shape). Source: blind-critical.
- [x] [Review][Decision][R2-D6] **`ConversationCreatedResult` inserts `CommandType` as second positional parameter.** Wire shape `{"schemaVersion":1,"commandType":"…","tenantId":"…","conversationId":"…","correlationId":"…"}` is a breaking wire-position change for any pre-existing v1 adopter even though no other diff appears to reference `ConversationCreatedResult` positional construction. Pick one: (a) accept the SemVer-major break (v1 is pre-release so this is acceptable); (b) move `CommandType` to the last position to preserve positional construction order; (c) add `[JsonPropertyOrder]` and ratify wire order independently of constructor order. Source: blind-critical.

##### Patch (Round 2)

- [x] [Review][Patch][R2-P1] `ConversationSummaryProjection.ParticipantPartyIds` parameter typed `IReadOnlyList<PartyId>` (non-nullable) with default `null!`. The `null!` suppresses the warning but the nullability contract is now a lie: callers passing `null` compile, and the normalizer `?? Array.Empty<PartyId>()` silently accepts it. Either make the parameter genuinely nullable (`IReadOnlyList<PartyId>?`) and keep the normalizer, or remove the default and require an explicit empty collection. Also: a list containing `null` elements is not rejected; on serialization the identifier converters dereference `.Value` and crash. [src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjection.cs]
- [x] [Review][Patch][R2-P2] `ConversationEventMetadata` constructor reordered `CommittedAt` before `ActorPartyId`/`CausationId`, but the `<param>` XML doc block did not. The doc still lists `committedAt` last; IntelliSense/SemVer-aware tooling now shows parameters in a misleading order. Reorder the `<param>` tags to match the positional record. [src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs]
- [x] [Review][Patch][R2-P3] JSON `null` for non-nullable identifier properties bypasses the ctor guard. `System.Text.Json` short-circuits `null` for reference-type converters before invoking `Read`. The test `IdentifierJsonShouldRejectMalformedValues` even asserts `Deserialize<ConversationId>("null").ShouldBeNull()` — codifying the bypass. Override `HandleNull = true` on each identifier converter and have `Read` throw `JsonException` when the token is `Null` (so a payload `{"tenantId":null}` rejects rather than silently producing `default(record)` whose `Value` is null). Replicate for `SchemaVersionJsonConverter` and `ProjectionTrustStateJsonConverter`. [src/Hexalith.Conversations.Contracts/Serialization/ConversationStringValueJsonConverter.cs; ConversationIntValueJsonConverter.cs; ProjectionTrustStateJsonConverter.cs]
- [x] [Review][Patch][R2-P4] `MessageAppended` XML doc embeds a literal `TODO Story 1.4.1 …` in `<param name="text">`. Public XML docs ship in NuGet IntelliSense for adopters. Move the TODO to an `<remarks>` block or to a code comment on the property; keep the `<param>` description user-facing. [src/Hexalith.Conversations.Contracts/Events/MessageAppended.cs]
- [x] [Review][Patch][R2-P5] `ContractVersionInfo.ActiveSchemaVersion` and `UnsupportedSchemaVersion.{RequestedSchemaVersion,MinimumSupportedSchemaVersion,ActiveSchemaVersion}` are redeclared after the primary-constructor with `= <param>;` initializers and no body. They shadow auto-generated properties with identical accessors and add only noise. Either remove the redeclarations (rely on primary-constructor props) or give them a validation/normalization body. [src/Hexalith.Conversations.Contracts/Versioning/ContractVersionInfo.cs]
- [x] [Review][Patch][R2-P6] `ValidateTimestamp` rejects `<= DateTimeOffset.MinValue` only. `MaxValue`, `DateTimeKind.Unspecified` payloads, and arbitrary offsets pass. Tighten to: require `Year >= 2000` (or another agreed business floor), require `Year <= 9000`, and either require `Offset == TimeSpan.Zero` or document offset semantics in README. Apply consistently across `ConversationEventMetadata.CommittedAt`, `ProjectionFreshness.ObservedAt`, `ConversationMessageProjection.CreatedAt`, `ConversationCreated.CreatedAt`. Add tests pinning each rejection. [src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs; Projections/ProjectionFreshness.cs; Projections/ConversationMessageProjection.cs; tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs]
- [x] [Review][Patch][R2-P7] `ConversationEventType` and `ConversationCommandType` static factory members have no individual XML `<summary>` comments, unlike `ConversationErrorCode` and `ConversationErrorCategory`. Adopter IntelliSense shows blank tooltips. Add one-line summaries to each well-known instance. [src/Hexalith.Conversations.Contracts/Events/ConversationEventType.cs; Results/ConversationCommandType.cs]
- [x] [Review][Patch][R2-P8] `AssertPropertyType<T, string?>(...)` in `ContractMetadataTest` cannot verify nullable-reference annotation — `typeof(string?)` is `typeof(string)` for reference types. The tests for `CausationId`, `IdempotencyKey`, `ActorPartyId` therefore do not constrain nullability. Replace with `NullabilityInfoContext` inspection (read `NullabilityInfo.WriteState`/`ReadState` against the property/parameter). [tests/Hexalith.Conversations.Contracts.Tests/ContractMetadataTest.cs]
- [x] [Review][Patch][R2-P9] `FailClosedErrorsShouldRemainContentSafe` iterates over `ContractSamples.SafeError(code)` and asserts the JSON does not contain known-unsafe substrings — but the sample is constructed with hardcoded safe values (`"hidden"`, `"audit-001"`, `"The requested operation was not accepted."`). The check is tautological. The lone unsafe-input throw at the end is a single negative case. Add a parameterized adversarial test that constructs `new ConversationError(... developerGuidance: "<unsafe>", safeFieldDiagnostics: {"key":"<unsafe>"} ...)` for each unsafe term in the blocklist and asserts `ArgumentException` is raised — across all 9 codes, not just one. [tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs]
- [x] [Review][Patch][R2-P10] `ProviderCorrelationShouldNotReplaceConversationIdentity` only inspects reflection metadata — `typeof(ProviderCorrelationMetadata).GetProperty("ProviderSessionReference").PropertyType.ShouldNotBe(typeof(ConversationId))`. The property is declared `string?` so the assertion is trivially true regardless of intent. Replace with a structural assertion: e.g., assert that `ConversationId` and `ProviderCorrelationMetadata` cannot satisfy each other's contracts (no overlap in required public properties), or that a sample `ProviderCorrelationMetadata` JSON cannot deserialize into `ConversationId`. [tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs]
- [x] [Review][Patch][R2-P11] `ReleasedContractShapesShouldHaveSerializationFixtureCoverage` compares a hand-curated `expectedTypes[]` against `ContractSamples.AllContracts`. Adding a new public contract that the developer forgets to add to `expectedTypes` slides past. Replace `expectedTypes` with assembly scan: `typeof(ConversationId).Assembly.GetExportedTypes().Where(t => t.IsValueContract())` (or whatever marker rule applies) and require each to appear in `AllContracts`. [tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs]
- [x] [Review][Patch][R2-P12] `Should.Throw<Exception>(...)` in `IdentifierJsonShouldRejectMalformedValues` catches every exception type — a `NullReferenceException` from a converter bug would pass. Tighten to `Should.Throw<JsonException>` (or `ArgumentException` per the policy chosen for R2-P3 and R2-D2). [tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs]
- [x] [Review][Patch][R2-P13] `SchemaVersion(0)` / `SchemaVersion(-1)` invoked via JSON deserialization bubbles `ArgumentOutOfRangeException` rather than `JsonException`. Adopters catching `JsonException` to gate malformed payloads will miss it. Wrap converter-internal exceptions: `try { return new SchemaVersion(value); } catch (ArgumentException ex) { throw new JsonException("Schema version out of range.", ex); }`. Replicate across `ConversationStringValueJsonConverter` (identifier ctors throw `ArgumentException` for empty/whitespace). [src/Hexalith.Conversations.Contracts/Serialization/ConversationIntValueJsonConverter.cs; ConversationStringValueJsonConverter.cs]
- [x] [Review][Patch][R2-P14] `UnsupportedSchemaVersion(active: 1, min: 1, requested: 1)` constructs cleanly even though the type's name promises the request is unsupported. Add a ctor invariant: `requested < min || requested > active`, with `ArgumentOutOfRangeException` and a test. [src/Hexalith.Conversations.Contracts/Versioning/ContractVersionInfo.cs]
- [x] [Review][Patch][R2-P15] `ConversationErrorResult` stores the input list reference without copying — external mutation of the caller's list after construction silently mutates `Errors`. Defensive-copy in the ctor: `Errors = errors?.ToArray() ?? throw …;`. Add a test that mutates the caller's list and asserts `result.Errors` is unchanged. [src/Hexalith.Conversations.Contracts/Errors/ConversationErrorResult.cs]
- [x] [Review][Patch][R2-P16] ~~Identifier `JsonConverter.Write` accepts `default(ConversationId)` silently.~~ **Dismissed during patch application as a false positive.** Record-class types (`sealed record ConversationId(string Value)`) have `null` as their `default`, not a "null-property" struct-like state. `System.Text.Json` short-circuits `null` references on Write before invoking the converter, so the `Write` method never receives a default-constructed record with `Value == null`.
- [x] [Review][Patch][R2-P17] `PublicContractsShouldRoundTripWithSystemTextJsonWebDefaults` re-serializes the deserialized object and asserts `AssertJsonEquivalent` — useful but does not assert `deserialized.ShouldBe(sample)`. Records support value equality; add the equality assertion alongside the JSON-equivalence assertion to catch any converter that drops a value silently. [tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs]
- [x] [Review][Patch][R2-P18] No test pins behavior of `JsonSerializer.Deserialize<ConversationErrorCode>("\"bogus_code\"")`, `<ConversationEventType>("\"BogusEvent\"")`, etc. Add per-closed-vocabulary negative deserialization tests that assert the failure mode chosen for R2-D2/R2-D3 (currently `ArgumentException` from `Parse`). [tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs]
- [x] [Review][Patch][R2-P19] `OptionalReasonCodesShouldRejectWhitespaceWhenProvided` covers `" "` and `"\n"` only. Add `"\r\n"` and `"\t"` cases — Windows wire payloads frequently carry `\r\n` line endings. [tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs]
- [x] [Review][Patch][R2-P20] `ProviderCorrelationMetadata.ExtensionData` accepts a dictionary containing `null` string values (legal in .NET `IReadOnlyDictionary<string, string>` at the type system level). Add ctor validation that rejects null/whitespace keys and null values, and add a fixture test. [src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs; ContractValidationTest.cs]
- [x] [Review][Patch][R2-P21] `ForbiddenPublicSurfaceTest` does not pin the regex-vs-substring policy change: no test asserts that the legitimate identifier `AggregateNotFound` is NOT flagged by the `Aggregate` rule, nor that a bare `Stream` property name WOULD be flagged. Add positive-allow + negative-block fixture tests. [tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs]
- [x] [Review][Patch][R2-P22] `ValidateSafeDiagnostics` discards the return value of `ValidateSafeText` for both key and value, while `DeveloperGuidance` captures it. Functionally fine today (the validator throws on failure), but if `ValidateSafeText` ever becomes a normalizer (trim/lowercase), the diagnostic dictionary path would silently keep unnormalized data. Capture the return: `validatedKey = ValidateSafeText(diagnostic.Key, …);` and rebuild the dictionary, or rename to `EnsureSafeText` to make the void-return intent explicit. [src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs]

##### Patch derived from Round 2 decisions

- [x] [Review][Patch][R2-P23] **D1: per-type prefix for identifier JSON wire shape.** Adopt URN-style prefixes: `tenant:`, `conv:`, `party:`, `project:`, `folder:`, `file:`, `message:`. Modify `ConversationStringValueJsonConverter` (or split into per-type converters) so Write emits `"<prefix>:<value>"` and Read rejects payloads lacking the expected prefix with `JsonException`. Replace `FlatIdentifierJsonShouldDependOnDestinationContractType` test with a `JsonIdentifierShouldRejectCrossTypeRehydration` test asserting `Deserialize<ConversationId>(Serialize(new TenantId("x")))` throws. Update every JSON fixture in `ContractSerializationTest` and `ContractSamples` to the prefixed shape. Update README identifier section with the new wire shape. [src/Hexalith.Conversations.Contracts/Serialization/*.cs; tests/Hexalith.Conversations.Contracts.Tests/IdentifierValidationTest.cs; ContractSerializationTest.cs; ContractSamples.cs; README.md]
- [x] [Review][Patch][R2-P24] **D2: SchemaVersion strict integer.** Confirm `ConversationIntValueJsonConverter` rejects `1.0`, `1e0`, JSON strings, and overflow with `JsonException` (not `ArgumentOutOfRangeException` — see also R2-P13). Add a `SchemaVersionShouldRejectFractionalAndStringPayloads` test in `ContractValidationTest`. README updated to state wire shape: "schemaVersion is a positive integer in JSON; JS clients must serialize without trailing `.0`." [src/Hexalith.Conversations.Contracts/Serialization/ConversationIntValueJsonConverter.cs; tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs; README.md]
- [x] [Review][Patch][R2-P25] **D3: ordinal-strict closed vocabularies.** No code change required (already strict). Add explicit tests for `Parse("current")`, `Parse("CONVERSATIONCREATED")`, etc. asserting `ArgumentException` for each closed-vocab type. Update README closed-vocabulary section: "Closed vocabularies are case-sensitive in canonical PascalCase. JSON Read does not normalize case." [tests/Hexalith.Conversations.Contracts.Tests/ContractValidationTest.cs; README.md]
- [x] [Review][Patch][R2-P26] **D4: extended blocklist on all free-text fields.** Expand `ConversationError.ValidateSafeText` blocklist to include the spec's full forbidden-term set (EventStore, envelope, stream, snapshot, sequence, expected revision, checkpoint, SignalR, projection topology, handler, dispatcher, repository, store, aggregate identity, raw upstream — combined with the original 5). Apply `ValidateSafeText` to `CorrelationId` and `AuditHandle` in `ConversationError`. Replace the existing false-positive-prone substrings (`"exists"`, `"storage"`) with `\bexists\b` regex anchors so legitimate compound words (`non-existent`) are not rejected. Add adversarial test parameterized over every unsafe term + every protected field, asserting `ArgumentException`. README updated: blocklist is best-effort; primary non-disclosure is the closed-vocabulary `Code`/`Category`. [src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs; tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs; README.md]
- [x] [Review][Patch][R2-P27] **D5: require ActorPartyId; CausationId stays optional.** Change `ConversationEventMetadata.ActorPartyId` from `PartyId? ActorPartyId = null` to required positional `PartyId ActorPartyId`. Move `CausationId = null` to remain the trailing optional. Validate non-null in ctor (rely on positional record's compile-time guarantee for required reference types under nullable-enabled). Update every event sample in `ContractSamples` and every fixture in `ContractSerializationTest`. [src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs; tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs; ContractSerializationTest.cs; ContractMetadataTest.cs]
- [x] [Review][Patch][R2-P28] **D6: move ConversationCreatedResult.CommandType to last position.** Reorder ctor: `(SchemaVersion, TenantId, ConversationId, CorrelationId, ConversationCommandType CommandType)`. Update JSON property order via natural ctor order (no `[JsonPropertyOrder]`). Update `ContractSamples.CreatedResult()` and `ContractSerializationTest` expected JSON fixture. Document in README: create-flow result is `ConversationCreatedResult`; positional construction `(version, tenant, conversation, correlation, commandType)`. [src/Hexalith.Conversations.Contracts/Results/ConversationCreatedResult.cs; tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs; ContractSerializationTest.cs; README.md]

##### Deferred (Round 2)

- [x] [Review][Defer][R2-W1] `with`-clone on identifier records is silently broken — `init` removed in favor of validating ctors closes the validation hole but also blocks `existing with { Value = "new" }`. Adopters porting from prior shapes get an unhelpful compile error with no migration guide. Document the lock-down in README and add a test that asserts `with`-clone is intentionally unsupported (or accept the breakage). [src/Hexalith.Conversations.Contracts/Identifiers/*.cs] — deferred, trade-off accepted in prior review's "init-able defeats ctor guard" patch; revisit if adopter feedback surfaces
- [x] [Review][Defer][R2-W2] `ProviderCorrelationMetadata.ExtensionData` unbounded — already in deferred-work.md as governance concern; flagged again because no progress this round. [src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs] — deferred, owner: governance epic
- [x] [Review][Defer][R2-W3] `ConversationCreated.CreatedAt` is a pass-through getter that reads `Metadata.CommittedAt`. A producer that emits `committedAt: T1, createdAt: T2` will silently deserialize `CreatedAt = T1` (because there is no setter and the property is recomputed). No test pins this aliasing. Belongs to the Story 1.4.1 / Story 1.10 publication scope. [src/Hexalith.Conversations.Contracts/Events/ConversationCreated.cs] — deferred, downstream story owns event publication shape

##### Dismissed as noise (Round 2)

5 findings dismissed:

- `ProjectionFreshness` `schemaVersion` → `projectionContractSchemaVersion` rename — intentional per prior review's nominal-collision patch.
- `ProjectionTrustState` ctor public→private — intentional v1 closed-vocab narrowing per prior D2 resolution.
- README claim about `ConversationCommandAcceptedResult.ConversationId` non-null — verifiable in the record signature; not a contract bug.
- `AggregateNotFound` → category `Hidden` — closed-vocab decision already taken in prior D2.
- `ConversationEventMetadata` reorder treated as breaking — separate from R2-D5; the type-change of `EventType` is the substantive break and is covered by D2 resolution.

## Change Log

- 2026-05-18: Round 2 patches applied. URN-style prefixed identifier wire shape (`tenant:`, `conv:`, `party:`, `project:`, `folder:`, `file:`, `message:`), extended unsafe-term blocklist applied to every free-text error field, required `ActorPartyId` on `ConversationEventMetadata`, `CommandType` moved to last position on `ConversationCreatedResult`, tightened timestamp validation, null guards on identifier-typed envelope properties, defensive list snapshotting in `ConversationErrorResult`, NullabilityInfoContext-based metadata tests, adversarial blocklist tests, closed-vocab case-sensitivity tests, assembly-scan fixture coverage. R2-P16 dismissed as false positive during application. `dotnet test Hexalith.Conversations.slnx` = 75 / 75 passed. Story moved to `done`.
- 2026-05-18: Round 2 code review (`/bmad-code-review 1.2`) against `5ba11d5..HEAD`. Acceptance auditor verdict `approve`. Blind Hunter + Edge Case Hunter surfaced 6 decision-needed, 22 patch, 3 defer, 5 dismissed regressions in validation, JSON converters, test honesty, and contract metadata. Findings appended under Round 2.
- 2026-05-18: Addressed code review findings - primitive JSON wire contracts, closed vocabularies, validation guardrails, safe error tests, boundary checks, and README updates; moved story back to review.
- 2026-05-18: Code review (`/bmad-code-review 1.2`) appended 5 decision-needed, 27 patch, 9 deferred findings; acceptance auditor verdict approve with minor AC5 gaps.
- 2026-05-18: Implemented public contract identities, commands, events, results, projections, trust states, versioning, safe errors, README guidance, and contract guardrail tests; moved story to review.
- 2026-05-18: Party-mode review applied story hardening for identity taxonomy, contract-only boundaries, safe errors, serialization fixtures, and forbidden-surface tests.
- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
