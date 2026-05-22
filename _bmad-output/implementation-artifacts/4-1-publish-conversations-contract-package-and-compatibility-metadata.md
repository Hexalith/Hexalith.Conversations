# Story 4.1: Publish Conversations Contract Package and Compatibility Metadata

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want a published contract package with version and compatibility metadata,
so that I can integrate against stable Conversations commands, projections, events, and typed errors.

## Acceptance Criteria

1. Contract package contents expose the adopter-safe public surface
   - Given the Conversations contract package is built,
   - When package contents are inspected,
   - Then it exposes commands, projections, domain events, typed errors, schema/version metadata, and compatibility status for the active contract version,
   - And it excludes server infrastructure, EventStore envelopes, snapshot mechanics, stream internals, SignalR group names, and UI implementation details.

2. Compatibility discovery is machine-readable and content-safe
   - Given an adopter needs to discover compatibility,
   - When they query version or compatibility metadata through the package or service contract,
   - Then the response identifies active command, projection, event, and client package versions,
   - And unsupported or deprecated versions are represented with machine-readable status and safe remediation pointers.

3. Contract validation proves package safety and serialization behavior
   - Given contract package validation runs,
   - When serialization, nullable, dependency direction, schema version, and package inventory checks execute,
   - Then public contracts remain serialization-friendly, infrastructure-free, centrally versioned, and documented enough for adopter use,
   - And no forbidden Party personal data or provider payload fields are exposed.

4. Compatibility tests cover supported and unsafe version scenarios
   - Given compatibility tests run,
   - When supported, deprecated, unsupported, additive, and malformed contract-version scenarios are exercised,
   - Then tests prove discoverability, typed compatibility status, safe failure semantics, and no leakage of internal EventStore implementation.

## Tasks / Subtasks

- [x] Extend existing contract versioning metadata instead of creating a parallel package model (AC: 1, 2, 4)
  - [x] Update or add types under `src/Hexalith.Conversations.Contracts/Versioning`, building on `ContractVersionInfo` and `SchemaVersion`.
  - [x] Add closed-vocabulary compatibility status values for at least `supported`, `deprecated`, `unsupported`, and malformed/invalid input handling. Keep these as contract-owned vocabulary types with existing JSON converter patterns if needed.
  - [x] Represent active command, projection, event, contracts package, and client package versions in one adopter-facing metadata shape. Include package IDs and semantic package versions, but do not expose assembly paths, local build paths, EventStore positions, stream names, commit hashes that are not already safe package metadata, or server topology.
  - [x] Include safe remediation pointers for deprecated and unsupported versions. Prefer documentation URIs and bounded machine-readable guidance over free-form operational details.
  - [x] Keep `SchemaVersion.Current` as the active v1 schema unless an accepted ADR explicitly changes the schema-versioning strategy.

- [x] Add package metadata and inventory validation for the existing packable projects (AC: 1, 3)
  - [x] Use the existing `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`; do not create a new contracts project.
  - [x] Ensure `Hexalith.Conversations.Contracts` has NuGet metadata needed by adopters: `PackageId`, package version source, description/tags inherited or project-specific as appropriate, repository/license metadata, and README packaging.
  - [x] Include a package README in the package using the existing SDK-style `PackageReadmeFile`/`None Pack="true"` pattern already present in root build props unless the project needs a more specific README.
  - [x] If client package version metadata depends on `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`, keep that project packable and version-aligned, but do not implement Story 4.2 client happy-path behavior here.
  - [x] Add package inventory tests that pack the contracts project to a temporary test output and inspect the `.nupkg`/`.nuspec` for expected metadata and absence of forbidden dependencies or internal files.

- [x] Strengthen contract discovery and sample coverage (AC: 1, 2, 3, 4)
  - [x] Update `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` so all new versioning and compatibility contracts participate in serialization and forbidden-surface checks.
  - [x] Extend `ContractMetadataTest`, `ContractsAssemblyBoundaryTest`, `ForbiddenPublicSurfaceTest`, or add focused versioning tests as appropriate.
  - [x] Ensure `ContractVersionInfo` or its successor validates nullability, active/minimum supported invariants, package version strings, contract family names, and status/remediation combinations.
  - [x] Add tests proving additive v1 JSON fields are tolerated where the contract allows extension, while missing, zero, negative, malformed, or unsupported schema/package versions fail with safe typed compatibility results.
  - [x] Add tests proving compatibility metadata JSON does not contain `EventStore`, `stream`, `snapshot`, `envelope`, `SignalR`, `subscription`, server route internals, Party personal data terms, provider payload terms, raw exception text, or local file paths.

- [x] Preserve public contract and dependency boundaries (AC: 1, 3)
  - [x] Keep `Hexalith.Conversations.Contracts` free of references to `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.FrontComposer`, ASP.NET Core, Dapr, server projects, UI projects, and HTTP clients.
  - [x] Do not expose raw EventStore command envelopes, stream IDs, snapshots, aggregate identities, storage positions, subscription names, SignalR groups, projection topology, server handler names, repository names, or generated UI details.
  - [x] Do not add Party display names, email, phone, avatar, person details, organization details, provider prompts, provider responses, raw provider payloads, or redacted content to public compatibility metadata.
  - [x] Keep compatibility status as adopter-facing contract truth, not an authorization, tenant-access, governance, or freshness decision.

- [x] Add or update adopter-facing documentation enough for package use (AC: 2, 3)
  - [x] Update `README.md` or a package-specific markdown file referenced by the package to describe active v1 contract version discovery, compatibility status meanings, safe remediation pointers, and the boundary between contracts and the future .NET client.
  - [x] Document that the v1 supported integration path is shared contracts plus the .NET client, but Story 4.1 only publishes contract/package compatibility metadata; Story 4.2 implements the client happy path.
  - [x] Document that raw HTTP fallback examples remain out of normal v1 adopter guidance unless later buyer approval or diagnostics scope is recorded.
  - [x] Keep examples and text free of production tenant IDs, Party personal data, provider payloads, EventStore terminology, and internal server details.

- [x] Add focused tests and validation evidence (AC: 1-4)
  - [x] Run targeted contract tests first:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"`
  - [x] Run package validation:
    - `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation`
  - [x] Run the full solution before closing:
    - `dotnet test Hexalith.Conversations.slnx`
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 4.1 evidence after implementation.

- [x] Preserve scope boundaries and ADR stop conditions (AC: 1-4)
  - [x] Do not implement Story 4.2 .NET client create/append/read happy path, Story 4.3 typed error remediation expansion beyond compatibility needs, Story 4.4 onboarding diagnostics, Story 4.5 adopter conformance package, or Story 4.7 full integration guide.
  - [x] Do not implement Epic 5 signed conformance artifacts, release manifest signing, named waiver lifecycle, deprecation policy publication, or release-gate evidence aggregation.
  - [x] Do not add a server compatibility endpoint unless the implementation can do so without introducing new API/versioning semantics beyond the contract shape required here. If a runtime endpoint is needed, stop for architecture review unless an approved readiness decision or ADR already covers it.
  - [x] Stop for ADR/waiver before changing public error taxonomy, adding a new trust/freshness state, exposing a new public status vocabulary outside compatibility metadata, changing event schema evolution rules, or weakening any existing fail-closed/non-disclosure boundary.

## Dev Notes

### Epic and Business Context

- Epic 4 shifts from internal governed conversation workflows to adopter integration readiness. The developer-facing outcome is stable contracts, a supported .NET integration path, compatibility discovery, typed safe failures, onboarding diagnostics, conformance tests, and integration guidance. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`]
- Story 4.1 covers FR70 and FR75: adopters need a published contract package defining commands, projections, events, and typed errors, plus active contract version and compatibility status discovery for commands, projections, events, and client packages. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.1: Publish Conversations Contract Package and Compatibility Metadata`; `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`]
- The PRD locks the packaging model as shared contract package plus per-language thin clients. v1 ships at minimum the .NET client and the contract package; the contract package remains the source of truth for DTOs, command shapes, projection shapes, error envelope, and event schema. [Source: `_bmad-output/planning-artifacts/prd.md#API Documentation & Versioning`]
- Readiness decisions confirm that v1 integration uses the .NET client plus shared contract package, and raw HTTP examples are omitted unless later buyer approval or diagnostics-only exception is recorded. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`]
- The EventStore envelope is inherited infrastructure. Conversations owns public domain event names, schemas, contract versioning, and compatibility tests, but Story 4.1 must not expose or evolve EventStore internals. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#eventstore-envelope-stability-and-evolution-ownership`]

### Current Implementation State

- `src/Hexalith.Conversations.Contracts` already exists, targets `net10.0`, is packable, and contains public commands, events, governance contracts, identifiers, projections, queries, results, serialization converters, trust states, and versioning. [Source: `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`; source tree]
- `src/Hexalith.Conversations.Client` already exists, is packable, and references only the contracts project today. It currently exposes only `ClientAssemblyMarker`; Story 4.1 may identify the client package version in metadata but must not implement the Story 4.2 client workflow. [Source: `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`; `src/Hexalith.Conversations.Client/ClientAssemblyMarker.cs`]
- `ContractVersionInfo` currently describes a contract family name, active schema version, and minimum supported schema version. `UnsupportedSchemaVersion` validates requested unsupported versions without exposing runtime details. This is the primary place to extend or compose compatibility metadata. [Source: `src/Hexalith.Conversations.Contracts/Versioning/ContractVersionInfo.cs`]
- `SchemaVersion.Current` is currently `1` and serializes as a positive integer through the custom `SchemaVersionJsonConverter`. Existing tests prove positive integer serialization, additive metadata tolerance, invalid version rejection, and absence of topology terms in event metadata serialization. [Source: `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`; `tests/Hexalith.Conversations.Contracts.Tests/Versioning/SchemaVersionCompatibilityTest.cs`]
- Existing boundary tests already enforce that `Contracts` does not reference EventStore, Tenants, Parties, FrontComposer, ASP.NET Core, Dapr, HTTP clients, or other infrastructure assemblies/namespaces. Extend these tests rather than replacing them. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`]
- Existing forbidden-surface tests scan exported type/property names and serialized sample JSON for infrastructure, personal-data, provider-payload, and raw exception vocabulary. New compatibility contracts must be added to `ContractSamples.AllContracts` or they can bypass this safety net. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`; `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]
- Existing typed errors include `schema_version_unsupported`, `versioning` category, safe documentation pointer, safe field diagnostics, retryability, audit handle, and developer guidance with a best-effort unsafe-term blocklist. Use these contracts for compatibility failure semantics when possible instead of adding a parallel error envelope. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCategory.cs`]
- Root `Directory.Build.props` sets `net10.0`, nullable, implicit usings, warnings-as-errors, deterministic builds, package metadata defaults, central README packaging for packable projects, and root-level sibling module detection only. Do not add package versions directly to project files. [Source: `Directory.Build.props`; `Directory.Packages.props`; project AGENTS.md submodule rules]

### Architecture and Contract Guardrails

- Public contracts expose Conversations language, not substrate language. Commands, projections, events, typed errors, IDs, freshness/trust states, and schema versions belong in `Contracts`; server handlers, EventStore adapters, tenant projections, hydration adapters, and UI composition do not. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Public APIs and contracts must not expose EventStore stream names, event positions, snapshots, envelopes, projection topology, raw EventStore command status, generated UI internals, or storage-specific error semantics. [Source: `_bmad-output/planning-artifacts/architecture.md#API Boundaries`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Versioned contract ownership must be visible in type names, metadata, tests, and evidence. The architecture examples use names such as `MessageAppendedV1`, `ConversationCreatedV1`, `ConversationProjectionV1`, and `ConversationDetailsV1`; if this story adds new versioned DTOs, follow that pattern. [Source: `_bmad-output/planning-artifacts/architecture.md#Schema Naming Rule`]
- The shared vocabulary rule applies. Do not invent local synonyms for established states or categories. Compatibility-specific statuses are allowed for this story, but they must be closed, documented, serialized consistently, and tested. [Source: `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`]
- Compatibility metadata is not tenant authorization, governance truth, Party validation, projection freshness, or runtime health. It should tell adopters what contract/package versions are active, deprecated, unsupported, or invalid, and where to find safe remediation guidance. [Source: `_bmad-output/planning-artifacts/architecture.md#Pattern Precedence Rule`; `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`]
- Any new durable state, runtime service endpoint, public error taxonomy change, schema evolution rule change, or degraded/fail-open behavior triggers ADR review before implementation. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`; `_bmad-output/planning-artifacts/architecture.md#Agent Conflict Stop Conditions`]

### Package and Serialization Guidance

- Microsoft NuGet guidance: SDK-style projects use MSBuild pack properties such as `IsPackable`, `PackageId`, `PackageVersion`, `PackageReadmeFile`, `RepositoryUrl`, `RepositoryType`, `PackageTags`, and related metadata; `PackageReadmeFile` must point to a Markdown file included in the package. [Source: `https://learn.microsoft.com/nuget/reference/msbuild-targets#pack-target`; `https://learn.microsoft.com/nuget/reference/msbuild-targets#packagereadmefile`]
- `dotnet pack` builds a project and creates `.nupkg` output. In .NET 10, `dotnet pack` also supports packing a `.nuspec` directly, but this repo should use the existing SDK-style project files unless a later packaging ADR says otherwise. [Source: `https://learn.microsoft.com/dotnet/core/tools/dotnet-pack`]
- NuGet package README guidance says README files improve package onboarding and are displayed by NuGet.org/Package Manager when included correctly. For this story, the README only needs enough contract/compatibility guidance for package use; the full developer integration guide remains Story 4.7. [Source: `https://learn.microsoft.com/nuget/nuget-org/package-readme-on-nuget-org`; `https://learn.microsoft.com/nuget/quickstart/create-and-publish-a-package-using-visual-studio#add-a-read-me-or-another-file`]
- `System.Text.Json` supports records and immutable types, and ASP.NET Core web defaults differ from bare serializer defaults. Existing tests use `JsonSerializerDefaults.Web`; new compatibility contracts should follow that pattern and keep custom converters in `Serialization` when closed vocabularies require them. [Source: `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft#table-of-differences`; `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/deserialization#deserialization-behavior`]
- `dotnet test --filter` supports `FullyQualifiedName~...`, exact/non-exact/contains operators, and `|`/`&` composition for xUnit test selection. Use targeted filters first, then full-solution validation. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#syntax`; `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]

### Previous Story Intelligence

- Story 4.1 is the first Epic 4 story, so there is no previous Epic 4 implementation file. Recent work from Story 3.7 shows the repo pattern for additive public contracts: add DTOs/closed vocabularies, register converters when needed, update `ContractSamples`, add contract tests, keep service/runtime behavior out of scope unless explicitly required, and run focused tests before the full solution. [Source: `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`]
- Story 3.7 review fixes emphasized scope binding, fail-closed handling for missing caller authority, canonical cursor validation, and manifest integrity. For Story 4.1, the analogous risk is accepting ambiguous or malformed compatibility input as supported, or letting metadata imply runtime authority it does not have. [Source: `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md#Senior Developer Review (AI)`]
- Recent commits show implementation has been story-scoped and contract/test heavy: `feat(story-3.7): Add buyer acceptance demo fixtures`, `feat(story-3.6): Add governance verification results`, and Story 3.5/3.4 read-only compliance/citation work. Continue this pattern with focused contract/package tests before broad runtime changes. [Source: `git log --oneline -5`]

### Testing Requirements

- Add red-phase tests before implementation where practical:
  - Compatibility metadata returns supported status for active v1 contract/package versions.
  - Deprecated and unsupported versions return machine-readable status plus safe remediation pointer.
  - Malformed schema/package version inputs do not throw raw exceptions into serialized adopter output.
  - Package inventory excludes server, EventStore, UI, Dapr, ASP.NET Core, Tenants, Parties, FrontComposer, generated `obj`, and test assemblies.
  - Serialized compatibility metadata passes forbidden public-surface and content-safety checks.
- Keep tests deterministic. Package inventory tests should write to a temporary output directory or `.artifacts/package-validation` and clean/ignore generated outputs according to repo conventions.
- Full closure requires `dotnet test Hexalith.Conversations.slnx` after targeted tests and pack validation.

### Out of Scope

- No Story 4.2 client happy-path implementation: do not add create/append/read client methods, HTTP transport, retry policy, or raw HTTP parity examples.
- No Story 4.3 broad typed-error taxonomy expansion beyond what compatibility status requires.
- No Story 4.4 onboarding diagnostics or CORE precondition checking.
- No Story 4.5 adopter-facing conformance test package, CORE fixture package, CI result schema, or executable conformance runner.
- No Story 4.7 full developer integration guide, API examples, or DocFX/API reference pipeline beyond package README sufficiency.
- No Epic 5 release manifest signing, deprecation policy publication, named-waiver process, signed conformance artifact, or release evidence aggregation.
- No new transcript tables, durable compatibility store, runtime health dashboard, Admin UI, FrontComposer surface, background worker, raw EventStore endpoint, or global discovery endpoint.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 4.1: Publish Conversations Contract Package and Compatibility Metadata`
- `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`
- `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`
- `_bmad-output/planning-artifacts/prd.md#API Documentation & Versioning`
- `_bmad-output/planning-artifacts/prd.md#Integration And Compatibility`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Schema Naming Rule`
- `_bmad-output/planning-artifacts/architecture.md#Enforceable Architecture Guardrails`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`
- `_bmad-output/project-context.md`
- `Directory.Build.props`
- `Directory.Packages.props`
- `README.md`
- `docs/adrs/index.md`
- `docs/conversation-publication-events.md`
- `docs/projection-read-models.md`
- `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractVersionInfo.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractMetadataTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Versioning/SchemaVersionCompatibilityTest.cs`
- `https://learn.microsoft.com/nuget/reference/msbuild-targets#pack-target`
- `https://learn.microsoft.com/nuget/reference/msbuild-targets#packagereadmefile`
- `https://learn.microsoft.com/dotnet/core/tools/dotnet-pack`
- `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/deserialization#deserialization-behavior`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase confirmed: targeted contract test filter failed before implementation because Story 4.1 compatibility types were missing.
- Targeted contract tests passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"` - 38 passed.
- Package validation passed: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` produced `Hexalith.Conversations.Contracts.1.0.0.nupkg`.
- Package inventory tests passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ContractPackageInventory"` - 2 passed.
- QA follow-up targeted contract and package tests passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~ContractPackageInventory"` - 44 passed.
- QA follow-up package validation passed: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation`.
- Senior review targeted contract and package tests passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~ContractPackageInventory"` - 49 passed.
- Senior review package validation passed: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation`.
- Full solution passed after senior review fixes: `dotnet test Hexalith.Conversations.slnx` - Contracts 273, Client 1, Integration 8, Core 139, Server 377.

### Completion Notes List

- Added contract-owned compatibility metadata under `Versioning` for active v1 command, projection, event, contracts package, and .NET client package discovery.
- Added `supported`, `deprecated`, `unsupported`, and `invalid` compatibility status vocabulary with the existing closed-vocabulary JSON converter pattern.
- Added safe compatibility evaluation and QA follow-up coverage for supported, deprecated package, unsupported schema/package, and malformed schema/package inputs with bounded remediation pointers and typed versioning errors.
- Added contracts/client NuGet metadata and a contracts-specific package README for adopter package use without implementing Story 4.2 client behavior.
- Added package inventory, serialization, additive JSON, dependency-boundary, forbidden-surface, and content-safety test coverage.
- Senior review fixed missing compatibility status/remediation/error invariant enforcement and added client package metadata alignment coverage.
- Updated Story 4.1 test evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.

### File List

- `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`
- `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`
- `src/Hexalith.Conversations.Contracts/README.md`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractVersionInfo.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-22

### Review Outcome

Approved after auto-fixes. No critical issues remain.

### Findings Fixed

- [x] [HIGH] Compatibility metadata and result constructors claimed safe status/remediation semantics but did not reject contradictory combinations, such as `supported` with remediation pointers or `unsupported` without typed failure details. Fixed in `ContractCompatibilityMetadata`/`ContractCompatibilityResult` with focused tests.
- [x] [MEDIUM] `ContractVersionInfo.Status` could be initialized to `null`, weakening the closed-vocabulary nullability guard. Fixed with a validated init setter and regression coverage.
- [x] [MEDIUM] Compatibility package-version evaluation compared both contracts and client package inputs against the contracts package version. Fixed evaluation to use the matching active package version for each field.
- [x] [MEDIUM] Client package metadata was updated but not covered by package inventory tests. Added coverage that the client project remains packable, package-id aligned, version-source inherited, and limited to the assembly marker plus contracts reference.

### Checklist Validation

- Story status was reviewable, story ID 4.1 resolved, project context and architecture guardrails loaded, and official Microsoft NuGet/System.Text.Json documentation was checked.
- Acceptance criteria, completed tasks, story File List, git changes, package contents, serialization behavior, forbidden-surface coverage, and contract/package tests were cross-checked.
- Validation passed: targeted contract/package tests, explicit `dotnet pack`, and full solution test suite.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 4, Story 4.1, and downstream Stories 4.2-4.7 boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR70-FR80, contract package/client packaging decisions, integration compatibility NFRs, security/privacy, typed errors, and package documentation.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on public contract boundaries, schema naming, shared vocabulary, ADR triggers, EventStore non-disclosure, package/client structure, and conformance guardrails.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`; no UI implementation is in scope, but disclosure-surface and telemetry/content-safety rules remain relevant to serialized metadata.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`, including .NET 10, central package management, EventStore/Tenants/Parties/FrontComposer boundaries, fail-closed behavior, and submodule policy.
  - Loaded current sprint status, readiness gates, readiness decisions, ADR index, README, publication/read-model docs, current contract/versioning/error files, contract boundary tests, forbidden-surface tests, package project files, current client project metadata, recent Story 3.7 learnings, and recent git history.
  - Checked official Microsoft documentation for NuGet/MSBuild pack metadata, package README inclusion, `dotnet pack`, `System.Text.Json` record/immutable deserialization behavior, and `dotnet test --filter`.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to existing `Contracts` and `Client` packable projects instead of a new contract package project.
  - Added explicit guardrails for compatibility vocabulary, package inventory tests, forbidden public-surface scanning, serialization behavior, README packaging, current `SchemaVersion.Current` ownership, and no EventStore/server/UI leakage.
  - Added package/test commands, current file touch list, architecture stop conditions, recent-story lessons, and official Microsoft documentation references.
  - Kept .NET client happy path, onboarding diagnostics, adopter conformance package, full integration guide, release signing, deprecation policy publication, and runtime compatibility endpoint scope out of Story 4.1 unless separately approved.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture/package guardrails, test requirements, latest technical references, and explicit out-of-scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Change Log

- 2026-05-22: Senior developer review auto-fixed compatibility invariant enforcement, client metadata test coverage, and marked Story 4.1 done after validation.
- 2026-05-22: Implemented Story 4.1 contract compatibility metadata, package README/metadata, package inventory validation, serialization/content-safety coverage, and validation evidence.
- 2026-05-22: Created Story 4.1 context from Epic 4 requirements, PRD/architecture/UX/readiness/project context, current contracts/client source, existing contract tests, Story 3.7 learnings, recent git history, and official Microsoft documentation.
