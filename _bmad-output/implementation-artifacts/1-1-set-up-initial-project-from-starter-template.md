# Story 1.1: Set Up Initial Project from Starter Template

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want a buildable Hexalith.Conversations module scaffold from the selected starter template,
so that future conversation features can be implemented inside the approved Hexalith architecture without reworking project boundaries.

## Acceptance Criteria

1. Given the Hexalith.Conversations repository, when the scaffold is created, then the solution contains the approved .NET 10 project structure for `Contracts`, `Client`, domain module, `Server`, `Testing`, `AppHost`, `ServiceDefaults`, and focused test projects, and project files use central package management without inline package versions.
2. Given the scaffold exists, when dependency references are added, then dependencies follow the approved boundary direction: contracts remain infrastructure-free, server/application code can use EventStore integration points, and client-facing contracts do not expose EventStore internals, and no sibling module source is copied into Conversations.
3. Given the first scaffold validation runs, when restore/build/test smoke checks execute, then the scaffold builds without requiring Aspire runtime, Dapr sidecars, tenant seed data, production secrets, provider credentials, or nested submodule initialization, and root-level submodule policy is documented or preserved.
4. Given future stories will implement domain behavior, when placeholder files or test fixtures are added, then they remain non-operative and fail closed at runtime, and they do not smuggle partial conversation persistence, tenant authorization, provider, UI, or worker behavior ahead of later stories.
5. Given pre-kickoff ADRs are required before dependent implementation, when the scaffold documentation is created, then the repository contains the approved ADR folder, ADR template, and decision tracker links for idempotency, tenant projection freshness, audit pairing, schema evolution, redaction replay, Party hydration, FrontComposer trust boundaries, and retention/deletion lifecycle, and dependent stories can link to recorded or explicitly waived decisions before implementation starts.

## Tasks / Subtasks

- [x] Create the root .NET scaffold and solution entry point. (AC: 1, 3)
  - [x] Add `global.json` aligned with the approved Hexalith.Conversations SDK policy: `10.0.300` with `rollForward` set to `latestPatch` (amended 2026-05-18 from the original `10.0.103` sibling pin after code review; the local SDK was `10.0.300` and the project formally adopts that baseline). If the local SDK is missing, stop and record the tooling mismatch instead of silently changing target frameworks.
  - [x] Add root `Directory.Build.props` adapted from sibling Hexalith modules, with `TargetFramework` `net10.0`, nullable enabled, implicit usings enabled, warnings as errors, deterministic builds, and Conversations-specific package metadata.
  - [x] Add root `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`; seed versions from sibling module conventions only where a scaffold project actually references the package.
  - [x] Create `Hexalith.Conversations.slnx` and include root solution items such as `AGENTS.md`, `CLAUDE.md`, `.gitmodules`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `LICENSE`, and any new `README.md`.
  - [x] Create starter projects from the Composite Hexalith .NET/Aspire scaffold: `src/Hexalith.Conversations.Contracts`, `src/Hexalith.Conversations.Client`, `src/Hexalith.Conversations`, `src/Hexalith.Conversations.Server`, `src/Hexalith.Conversations.Testing`, `src/Hexalith.Conversations.AppHost`, and `src/Hexalith.Conversations.ServiceDefaults`.
  - [x] Create focused smoke test projects at minimum for contracts, domain, server, and integration checks; add a client test project if client-specific compile or dependency-boundary assertions are introduced.

- [x] Wire references without violating project boundaries. (AC: 1, 2, 4)
  - [x] Keep `Hexalith.Conversations.Contracts` serialization-friendly and infrastructure-free; it must not reference server projects, Dapr, HTTP clients, EventStore server/runtime packages, FrontComposer shell packages, or UI packages.
  - [x] Let `Hexalith.Conversations.Client` reference public Contracts only plus approved client/runtime dependencies; it must not expose raw EventStore, Tenants, Parties, projection internals, or server-only abstractions.
  - [x] Keep the domain project deterministic and free of authorization, HTTP, Parties, Tenants, UI shaping, and persistence adapter calls.
  - [x] Keep EventStore integration references isolated to the server/application boundary and, when implementation later exists, under `Server/EventStore`.
  - [x] Use project references or package references to sibling modules; do not copy source files from `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.FrontComposer`, `Hexalith.Folders`, `Hexalith.Projects`, `Hexalith.Memories`, or `Hexalith.Commons`.
  - [x] Ensure every `<PackageReference>` in project files omits `Version`; every version belongs in `Directory.Packages.props`.

- [x] Add only non-operative placeholders needed for future work. (AC: 4)
  - [x] Placeholder code may provide assembly markers, namespace anchors, extension-method shells, or explicit `NotImplementedException` / fail-closed stubs only when needed for buildability.
  - [x] Do not implement `ConversationAggregate`, command handlers, tenant access, projection stores, provider integration, FrontComposer runtime behavior, governance commands, workers, EventStore writes, or read models in this story.
  - [x] If placeholder fixtures are created, make them synthetic and non-production; they must not persist conversations, authorize tenants, call providers, or imitate working domain behavior.

- [x] Create ADR and readiness documentation scaffolding. (AC: 5)
  - [x] Create `docs/adrs/0000-template.md` using the sibling ADR template style, with placeholders for status, context, decision, consequences, alternatives, and verification.
  - [x] Create a lightweight ADR index or decision tracker page linking the required decision topics: idempotency contract, tenant projection freshness, governance audit pairing, event schema evolution, redaction replay, Party hydration degraded states, FrontComposer trust boundaries, retention/deletion/tombstoning/legal-hold/export/derived-index lifecycle, EventStore envelope ownership, command availability metadata, temporal evidence anchor, and projection freshness blocking semantics.
  - [x] Link the tracker to `_bmad-output/implementation-artifacts/readiness-gates.md` and `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`.
  - [x] Do not mark any ADR as accepted unless the decision already exists in an approved readiness artifact or the story implementation explicitly records an approved decision.

- [x] Preserve submodule and local-development safety. (AC: 3)
  - [x] Preserve the existing `.gitmodules` root-level sibling module list.
  - [x] Do not run `git submodule update --init --recursive` or initialize nested submodules.
  - [x] Document that smoke validation must not require nested submodule initialization, Aspire runtime launch, Dapr sidecars, tenant seed data, production secrets, provider credentials, or external cloud resources.
  - [x] Use relative root-detection properties similar to sibling `Directory.Build.props` files if Conversations needs to locate root-level sibling modules.

- [x] Add scaffold smoke checks. (AC: 1, 2, 3, 4)
  - [x] Add compile-only or reflection-based tests that prove the scaffold projects load and build.
  - [x] Add a dependency-boundary smoke test or script/check that fails on inline package versions in `.csproj` files.
  - [x] Add a boundary check that public contract assemblies do not reference forbidden infrastructure packages or EventStore implementation/server packages.
  - [x] Run `dotnet restore`, `dotnet build`, and `dotnet test` against `Hexalith.Conversations.slnx`; capture any SDK/template mismatch in completion notes.

## Dev Notes

### Scope Boundary

Story 1.1 is scaffold support only. It supports the Epic 1 foundation but does not count as behavioral implementation coverage for FR1-FR41. Stories 1.2-1.11 own the actual identity, command, event, aggregate, participant, tenant-isolation, idempotency, projection, read, publication, replay, and schema-versioning behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Set Up Initial Project from Starter Template`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-18.md#Final Recommendation`]

Do not pull future domain behavior forward. In this story, a buildable placeholder is acceptable only when it is non-operative and fail-closed. Any working conversation persistence, authorization, provider integration, FrontComposer runtime surface, projection worker, governance command, EventStore write path, or read model is out of scope. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Set Up Initial Project from Starter Template`]

### Approved Starter and Tooling

Use the Composite Hexalith .NET/Aspire scaffold, not the Aspire Starter App as the primary template. The Aspire Starter App was rejected because it creates a sample full-stack app shape, while Conversations needs a bounded-context module shape with Contracts, Client, Server, projections, adapters, governance tests, and FrontComposer integration boundaries. [Source: `_bmad-output/planning-artifacts/architecture.md#Starter Template Evaluation`]

Architecture-provided initialization commands create:

- `Hexalith.Conversations.slnx`
- `src/Hexalith.Conversations.Contracts`
- `src/Hexalith.Conversations.Client`
- `src/Hexalith.Conversations`
- `src/Hexalith.Conversations.Server`
- `src/Hexalith.Conversations.Testing`
- `src/Hexalith.Conversations.AppHost`
- `src/Hexalith.Conversations.ServiceDefaults`
- `tests/Hexalith.Conversations.Contracts.Tests`
- `tests/Hexalith.Conversations.Tests`
- `tests/Hexalith.Conversations.Server.Tests`
- `tests/Hexalith.Conversations.IntegrationTests`

[Source: `_bmad-output/planning-artifacts/architecture.md#Selected Starter: Composite Hexalith .NET/Aspire Scaffold`]

Local sibling modules historically pin SDK `10.0.103` and use `net10.0`. The Conversations module formally adopts SDK `10.0.300` with `net10.0` as its baseline (amended 2026-05-18 from the original `10.0.103` after code-review decision; the architecture Version Note also describes the local SDK as `10.0.300-preview…`). Verify local tooling before generating projects, and do not silently downgrade to `net9.0` or change package versions inline to make local tooling pass. [Source: `Hexalith.Folders/global.json`; `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Version Note`]

Latest official documentation confirms that .NET 10 defaults `dotnet new sln` to SLNX format, while NuGet Central Package Management requires package versions in `Directory.Packages.props` and `PackageReference` entries without `Version` in project files. Aspire templates provide separate AppHost and ServiceDefaults project templates for adding Aspire orchestration to an existing solution. [Source: Microsoft Learn `dotnet new sln` SLNX default, 2026-05-18 lookup; Microsoft Learn NuGet Central Package Management, 2026-05-18 lookup; Microsoft Learn Aspire templates, 2026-05-18 lookup]

### Current Repository State

At story creation time, the Conversations repository root has planning artifacts, root-level sibling module submodules, `.gitmodules`, `AGENTS.md`, `CLAUDE.md`, `LICENSE`, `.gitignore`, screenshots, and an empty `docs` folder, but it does not yet contain root `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `README.md`, `Hexalith.Conversations.slnx`, `src/`, or `tests/` for Conversations. [Source: local repository inspection on 2026-05-18]

Existing `.gitmodules` contains root-level sibling modules for `Hexalith.AI.Tools`, `Hexalith.EventStore`, `Hexalith.Projects`, `Hexalith.Folders`, `Hexalith.Tenants`, `Hexalith.FrontComposer`, `Hexalith.Parties`, `Hexalith.Memories`, and `Hexalith.Commons`. Preserve this root-level policy and do not initialize nested submodules. [Source: `.gitmodules`; `AGENTS.md`]

### Project Structure Notes

The long-term architecture maps these boundaries:

- `Contracts`: public commands, projections, events, typed errors, identifiers, freshness/trust states, and version metadata.
- `Client`: typed adopter client over public contracts only, with no domain decisions and no EventStore/Tenants/Parties/projection internals exposed.
- `Hexalith.Conversations`: deterministic aggregate/domain logic only.
- `Server`: API boundary, validators, policies, adapters, projections, hydration, tenant access, publication, and server-owned integration.
- `Server/EventStore`: only approved write adapter boundary for EventStore-specific code.
- `Testing`: fixtures, fakes, builders, assertions, conformance helpers, and failure injection.
- `ServiceDefaults` and `AppHost`: observability, service discovery, resilience, and local orchestration defaults.

[Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]

The complete architecture includes future `Admin` and `Conformance` areas, but Story 1.1's starter-command minimum does not require implementing admin or conformance behavior. If the implementation creates those projects now, they must be empty/buildable shells only and must not introduce FrontComposer runtime behavior or release-evidence semantics ahead of their owning stories. [Source: `_bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure`; `_bmad-output/planning-artifacts/epics.md#Story 1.1: Set Up Initial Project from Starter Template`]

### Dependency and Boundary Guardrails

Central package management is mandatory. Project files must not include package `Version` attributes. Use `Directory.Packages.props` for all package versions and keep the list as small as the scaffold needs. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; Microsoft Learn NuGet Central Package Management, 2026-05-18 lookup]

Contracts must stay infrastructure-free. Do not reference server infrastructure, Dapr implementation details, HTTP clients, EventStore server packages, UI shell packages, or generated FrontComposer files from `Contracts`. Public contracts must use Conversations vocabulary and must not expose raw EventStore envelopes, aggregate IDs as substrate concepts, snapshots, stream names, event positions, SignalR groups, projection topology, or raw hydration internals. [Source: `_bmad-output/project-context.md#Critical Implementation Rules`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]

Server/application code may depend on EventStore integration points, but EventStore-specific code belongs under the approved server write-adapter boundary. Domain logic must not contain authorization, tenant lookup, HTTP calls, Parties calls, UI shaping, or persistence adapter calls. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/planning-artifacts/architecture.md#File Organization Patterns`]

Use sibling modules as references or packages, not source templates. If dependency investigation is needed, read sibling source and docs, but do not make incidental sibling changes and do not copy their runtime implementation into Conversations. [Source: `_bmad-output/project-context.md#Development Workflow Rules`]

### Testing Standards

Use xUnit v3, Shouldly, NSubstitute, Testcontainers, and Hexalith testing helpers only as needed for scaffold smoke coverage. Do not add broad integration tests that require Aspire runtime, Dapr sidecars, tenant seed data, provider credentials, production secrets, cloud resources, or nested submodule initialization. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.1: Set Up Initial Project from Starter Template`]

Minimum scaffold validation should include:

- `dotnet restore Hexalith.Conversations.slnx`
- `dotnet build Hexalith.Conversations.slnx`
- `dotnet test Hexalith.Conversations.slnx`
- a boundary check for inline package versions in project files
- a boundary check that Contracts does not reference forbidden infrastructure assemblies

### ADR and Readiness Guidance

Create the ADR folder/template and tracker links, but do not decide ADRs in this story. The required decision topics include idempotency contract, tenant projection freshness, governance audit pairing, event schema evolution, redaction replay, Party hydration, FrontComposer trust boundaries, and retention/deletion lifecycle. Dependent implementation stories must link to recorded or explicitly waived decisions before their behavior starts. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Set Up Initial Project from Starter Template`; `_bmad-output/implementation-artifacts/readiness-gates.md`]

Readiness gates already decided on 2026-05-17 must be linked where relevant, especially EventStore envelope ownership, projection freshness blocking semantics, Party hydration degraded states, command availability metadata, temporal evidence anchor, and retention/deletion/tombstoning/legal-hold/export/derived-index lifecycle. These decisions are conservative and broader behavior requires ADR, buyer approval, or explicit release-scope promotion. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`]

### Anti-Reinvention Warnings

- Do not build a chatbot transcript table, transcript repository, provider session store, or conversation-memory authority.
- Do not hand-build a portal or generated UI output in Story 1.1.
- Do not create fake tenant authorization or fake Party identity behavior just to satisfy future tests.
- Do not treat provider IDs, external business IDs, UI labels, or thread names as durable conversation identity.
- Do not expose raw EventStore mechanics in public contracts or client APIs.
- Do not use Dapr sidecars, Aspire launch, seeded tenants, provider credentials, or cloud resources as a prerequisite for scaffold smoke tests.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.1: Set Up Initial Project from Starter Template`
- `_bmad-output/planning-artifacts/architecture.md#Starter Template Evaluation`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-18.md#Final Recommendation`
- `_bmad-output/project-context.md#Project Context for AI Agents`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `AGENTS.md`
- `.gitmodules`
- `Hexalith.Folders/global.json`
- `Hexalith.Folders/Directory.Build.props`
- `Hexalith.Folders/Directory.Packages.props`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet --list-sdks` confirmed `10.0.300` is installed.
- Red phase: `dotnet test .\tests\Hexalith.Conversations.IntegrationTests\Hexalith.Conversations.IntegrationTests.csproj --filter ScaffoldProjectsAndSolutionShouldExist` failed before `Hexalith.Conversations.slnx` existed.
- Green validation: `dotnet restore .\Hexalith.Conversations.slnx` passed.
- Green validation: `dotnet build .\Hexalith.Conversations.slnx --no-restore` passed with 0 warnings and 0 errors after marking the AppHost server reference as `IsAspireProjectResource="false"`.
- Green validation: `dotnet test .\Hexalith.Conversations.slnx --no-build` passed with 8 tests.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Created the Conversations scaffold using the user-requested .NET SDK `10.0.300`, `rollForward` `latestPatch`, and `net10.0` target framework.
- Added central build and package management files; all project `PackageReference` entries omit inline versions.
- Added Contracts, Client, domain, Server, Testing, AppHost, and ServiceDefaults projects with inert assembly markers only.
- Added focused Contracts, Client, domain, Server, and Integration smoke tests for project shape, SDK pinning, package-version boundaries, and forbidden infrastructure references.
- Added ADR template and decision tracker links without accepting new decisions in this story.
- Preserved root-level submodule policy and did not run recursive submodule initialization.

### File List

- `Directory.Build.props`
- `Directory.Packages.props`
- `Hexalith.Conversations.slnx`
- `README.md`
- `global.json`
- `docs/adrs/0000-template.md`
- `docs/adrs/index.md`
- `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`
- `src/Hexalith.Conversations.Contracts/ContractsAssemblyMarker.cs`
- `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj`
- `src/Hexalith.Conversations.Client/ClientAssemblyMarker.cs`
- `src/Hexalith.Conversations/Hexalith.Conversations.csproj`
- `src/Hexalith.Conversations/ConversationsAssemblyMarker.cs`
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`
- `src/Hexalith.Conversations.Server/ServerAssemblyMarker.cs`
- `src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj`
- `src/Hexalith.Conversations.Testing/TestingAssemblyMarker.cs`
- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`
- `src/Hexalith.Conversations.AppHost/Program.cs`
- `src/Hexalith.Conversations.ServiceDefaults/Hexalith.Conversations.ServiceDefaults.csproj`
- `src/Hexalith.Conversations.ServiceDefaults/ServiceDefaultsAssemblyMarker.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`
- `tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj`
- `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`
- `tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj`
- `tests/Hexalith.Conversations.Tests/DomainBoundaryTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj`
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`
- `tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj`
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Review Findings

- [x] [Review][Patch] SDK pin formally adopted as `10.0.300` (decision: amend spec/architecture). Story Dev Notes and `architecture.md#Version Note` updated 2026-05-18.
- [x] [Review][Patch] `Hexalith.Conversations.Server.csproj` switched to `Microsoft.NET.Sdk.Web` with a fail-closed `Program.cs` that throws on startup (decision: align with architecture's webapi shape).
- [x] [Review][Patch] `FindRepositoryRoot` now uses `Hexalith.Conversations.slnx` as the root sentinel instead of `_bmad-output/` [`tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs:104-120`].
- [x] [Review][Patch] `ProjectPackageReferencesShouldNotDeclareInlineVersions` now asserts the project files list is non-empty and that at least one `<PackageReference>` was inspected [`tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs:69-97`].
- [x] [Review][Patch] `ReadmeShouldDocumentSmokeValidationSafety` now asserts all 7 spec-required phrases [`tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs:96-110`].
- [x] [Review][Defer] Boundary tests rely on `Assembly.GetReferencedAssemblies()` on empty marker assemblies — compiler retains only used references, so forbidden `<PackageReference>` additions can slip through [`tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`, `Client.Tests/ClientBoundaryTest.cs`, `Server.Tests/ServerBoundaryTest.cs`, `Tests/DomainBoundaryTest.cs`] — deferred, pre-existing scaffold-only constraint; revisit when stories 1.2+ introduce content.
- [x] [Review][Defer] No `Hexalith.Conversations.slnx` vs disk parity check — adding/removing a csproj on disk without updating the slnx (or vice versa) is undetected [`tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`] — deferred, coverage enhancement.
- [x] [Review][Defer] `Directory.Build.props` sibling-module root probes silently set empty paths when sibling folders are absent [`Directory.Build.props:11-13`] — deferred, only material when sibling references are actually wired in later stories.

## Change Log

- 2026-05-18: Implemented Story 1.1 scaffold and moved story to review.
- 2026-05-18: Code review captured findings (2 decision-needed, 3 patch, 3 deferred).
- 2026-05-18: Code review patches applied — SDK 10.0.300 formalized; Server switched to `Microsoft.NET.Sdk.Web` with fail-closed `Program.cs`; smoke-test sentinel decoupled from `_bmad-output/`; non-vacuous PackageReference assertion added; README phrase coverage extended to all seven spec phrases. Build clean (0/0), 8 tests pass. Story moved to done.
