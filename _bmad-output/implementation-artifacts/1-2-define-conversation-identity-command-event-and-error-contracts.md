# Story 1.2: Define Conversation Identity, Command, Event, and Error Contracts

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want clear Conversations contracts for identity, commands, events, projections, and typed errors,
so that I can integrate without learning EventStore internals or depending on unstable implementation details.

## Acceptance Criteria

1. Given the Contracts project exists, when conversation identity contracts are defined, then contracts include tenant-scoped `ConversationId` concepts distinct from provider identifiers, UI labels, external business identifiers, and thread names, and stable reference concepts are available for `TenantId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`, and provider correlation metadata.
2. Given adopter systems need to create and evolve conversations, when initial command contracts are defined, then the contract package includes create-conversation, append-message, add-participant, attach-file-reference, update-metadata, and close/archive command shapes where release-scoped, and each command includes schema version, tenant binding, correlation/causation metadata, and idempotency support where applicable.
3. Given Conversations persists meaningful domain changes, when initial event contracts are defined, then events use Conversations language, carry schema/version metadata, and store stable IDs rather than Party personal data, provider session authority, raw upstream records, or file binaries, and provider-specific payload metadata is represented only as opaque, tenant-isolated, explicitly versioned extension data.
4. Given adopter systems must handle failures consistently, when typed error contracts are defined, then invalid, unauthorized, conflicting, duplicate, unsupported-version, tenant-mismatched, stale-projection, and hidden-by-tenant-isolation outcomes have documented machine-readable failure semantics, and error shapes are content-safe and do not reveal target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references.
5. Given contracts are public integration surface, when contract tests and documentation checks run, then contract types are serialization-friendly, nullable-clean, centrally packaged, and infrastructure-free, and no public contract exposes raw EventStore envelopes, snapshot mechanics, stream internals, SignalR group names, or projection implementation details.

## Tasks / Subtasks

- [ ] Add stable identity and reference contracts in `src/Hexalith.Conversations.Contracts`. (AC: 1, 3, 5)
  - [ ] Create `Identifiers/ConversationId.cs`, `TenantId.cs`, `PartyId.cs`, `ProjectId.cs`, `FolderId.cs`, `FileId.cs`, `MessageId.cs`, and `BusinessReference.cs` as serialization-friendly public value contracts.
  - [ ] Add provider correlation metadata as opaque metadata, not identity: e.g. `ProviderCorrelationMetadata` with provider name/type, provider session or response identifiers, metadata schema version, and a bounded key/value extension bag.
  - [ ] Validate that `ConversationId` is tenant-scoped by contract usage and documentation, while provider IDs, UI labels, external identifiers, and thread names stay separate metadata/reference fields.
  - [ ] Keep all identifier contracts in the Contracts assembly; do not reference `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.Projects`, `Hexalith.Folders`, `Hexalith.EventStore`, or server-only packages from Contracts.

- [ ] Add command envelope and initial command contracts. (AC: 2, 5)
  - [ ] Create `Commands/ConversationCommandMetadata.cs` with schema version, tenant binding, caller/actor Party ID, correlation ID, optional causation ID, and idempotency key support.
  - [ ] Create command shapes for `CreateConversationCommand`, `AppendMessageCommand`, `AddParticipantCommand`, `AttachFileReferenceCommand`, `UpdateConversationMetadataCommand`, `CloseConversationCommand`, and `ArchiveConversationCommand`.
  - [ ] Ensure every mutating command carries tenant scope, actor attribution, schema version, correlation/causation metadata, and idempotency metadata where applicable.
  - [ ] Make command payloads carry stable IDs and allowed metadata only; they must not carry tokens, claims, raw provider payloads, tenant authorization state, Party personal data, file binaries, raw upstream records, or EventStore envelopes.
  - [ ] Keep these as public contract DTOs only. Do not implement validators, handlers, aggregate methods, authorization, persistence, projection updates, or EventStore dispatch in this story.

- [ ] Add event contract primitives and initial event contracts. (AC: 3, 5)
  - [ ] Create `Events/ConversationEventMetadata.cs` with schema version, event type, tenant scope, conversation identity, actor Party ID where applicable, correlation/causation metadata, and committed timestamp metadata expected by public contracts.
  - [ ] Create event contracts for `ConversationCreated`, `MessageAppended`, `ParticipantAdded`, `FileReferenceAttached`, `ConversationMetadataUpdated`, `ConversationClosed`, and `ConversationArchived`.
  - [ ] Use past-tense Conversations domain names and stable references only. Events must not store Party display names, contact values, person/organization details, provider-owned session IDs as authority, raw prompt/provider payloads, raw upstream records, file binaries, or raw upstream problem details.
  - [ ] Represent provider extension data as versioned opaque metadata and keep it tenant-scoped. Do not expose EventStore stream names, sequence numbers, snapshots, or internal projection topology.

- [ ] Add result, projection, trust/freshness, version, and error contracts needed by adopter-facing semantics. (AC: 2, 4, 5)
  - [ ] Create `Results` contracts for command acceptance and stable outcomes, including assigned `ConversationId` for create, accepted command identity/correlation handle, and read-model visibility caveat where relevant.
  - [ ] Create minimal projection/read contract shells under `Projections` that expose Conversations vocabulary and freshness/trust state without implementing projection storage.
  - [ ] Create `TrustStates` contracts using the approved vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`.
  - [ ] Create `Versioning` contracts for active contract/schema version and unsupported-version reporting.
  - [ ] Create `Errors/ConversationErrorCode.cs` and a content-safe problem/result contract that covers at minimum `tenant_binding_missing`, `tenant_isolation_violation`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `idempotency_conflict`, `aggregate_not_found`, `schema_version_unsupported`, and `command_validation_failed`.
  - [ ] Ensure error contracts include machine-readable code/category, retryability where meaningful, correlation/audit handle, optional documentation pointer, and safe field diagnostics, without leaking inaccessible tenant IDs, Party personal data, conversation existence, redacted content, provider payloads, or cross-tenant business references.

- [ ] Add focused contract tests and serialization checks. (AC: 1-5)
  - [ ] Add tests in `tests/Hexalith.Conversations.Contracts.Tests` proving contracts serialize and deserialize with `System.Text.Json` defaults used by web APIs, preserve required fields, and remain nullable-clean.
  - [ ] Add tests proving command and event contracts include tenant scope, schema version, correlation/causation metadata, actor Party attribution where required, and idempotency fields where applicable.
  - [ ] Add tests proving forbidden personal/provider/file/upstream payload fields are absent from public event and command contracts by property-name inspection.
  - [ ] Extend or replace existing boundary tests so Contracts package references and framework references are inspected from `.csproj` XML, not only from `Assembly.GetReferencedAssemblies()`, because marker assemblies may not retain unused references.
  - [ ] Add tests proving public contract names and serialized shapes do not expose EventStore terms such as envelope, stream, snapshot, sequence, expected revision, SignalR group, projection topology, or EventStore aggregate identity.

- [ ] Update developer documentation for the contract package. (AC: 4, 5)
  - [ ] Add or update `README.md` contract-package guidance that names the supported `.NET client + shared contract package` integration path and explains that raw EventStore knowledge is not required.
  - [ ] Document the stable distinction between `ConversationId`, tenant ID, Party ID, external business references, provider correlation metadata, labels, and thread names.
  - [ ] Document typed error semantics and hygiene rules, including non-disclosure for cross-tenant and hidden-by-isolation outcomes.
  - [ ] Link to readiness decisions and ADR tracker entries that future stories must resolve before behavior implementation, without accepting new ADR decisions in this story.

- [ ] Validate and keep the implementation scoped. (AC: 5)
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore` or, if restore/build artifacts are stale, run `dotnet restore`, `dotnet build`, and `dotnet test` against `Hexalith.Conversations.slnx`.
  - [ ] Do not run recursive submodule initialization. Root-level submodule reads are allowed only where already available.
  - [ ] Do not add EventStore, Dapr, ASP.NET Core, FrontComposer, Tenants, Parties, Projects, or Folders dependencies to `Hexalith.Conversations.Contracts`.
  - [ ] Do not implement domain behavior, command handlers, aggregate state transitions, EventStore adapters, tenant projection, Party validation, projection stores, UI, workers, or conformance evidence in this story.

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

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
