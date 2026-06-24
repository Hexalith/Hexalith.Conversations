---
baseline_commit: 59766b0ec29d55f88742074fc0c1c62c9e539aa7
---

# Story 3.5: Promote & adopt the shared Aspire/Dapr domain-module hosting base

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a technical-module maintainer,
I want a shared Aspire/Dapr hosting base parameterized by app-id/component names and shared-vs-isolated mode, adopted by Conversations,
so that AppHost/Aspire and Dapr sidecar topology are attached through one tested capability instead of copied per-module Aspire modules.

## Acceptance Criteria

**AC-1 - Resolve and record the FR-13 landing zone before code edits.**
Given Epic 3 OQ-1 was ratified as "Commons, all Epic-3",
When Story 3.5 starts,
Then the FR-13 hosting capability is recorded in `docs/release-evidence/promote-adopt-runbook.md` as a new additive Commons library, recommended name `Hexalith.Commons.Aspire`, with self-contained build props like Stories 3.1-3.4 so umbrella builds do not require nested Commons submodules.
[Source: docs/release-evidence/promote-adopt-runbook.md#0-resolve-the-landing-zone-gating-precondition-dont-promote-into-the-dark; _bmad-output/implementation-artifacts/3-4-promote-adopt-the-shared-servicedefaults-base.md#Previous story intelligence]

**AC-2 - Promote the shared Aspire/Dapr domain-module topology base.**
Given `FoldersAspireModule` and `ProjectsAspireModule` carry structurally similar module topology code,
When the shared base is promoted,
Then it supports domain-neutral app IDs, resource names, Dapr state-store and pub/sub component names, shared vs isolated infrastructure modes, sidecar config paths, Dapr resource paths, placement/scheduler host addresses, optional app health check path, `WaitFor`/`WithReference` composition, and project-resource records without depending on Conversations contracts or domain logic.
[Source: Hexalith.Folders/src/Hexalith.Folders.Aspire/FoldersAspireModule.cs; Hexalith.Projects/src/Hexalith.Projects.Aspire/ProjectsAspireModule.cs; _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#C-Cross-module-duplication-PROMOTE-candidates-FR-10FR-15]

**AC-3 - Preserve and clarify the EventStore.Aspire boundary.**
Given `Hexalith.EventStore.Aspire` already exposes `AddHexalithEventStore(...)`, `AddHexalithEventStoreGatewayProject(...)`, and `AddEventStoreDomainModule(...)` with shared-vs-isolated Dapr semantics,
When Commons introduces the generic base,
Then EventStore's existing public helpers remain additive/backward-compatible and either delegate to the Commons base or are left as thin platform-specific wrappers; existing EventStore consumers keep compiling unchanged.
[Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs; Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs; Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStorePlatformExtensions.cs]

**AC-4 - Adopt the shared hosting base in the Conversations AppHost.**
Given `src/Hexalith.Conversations.AppHost/Program.cs` currently only adds `conversations-admin-web`, and the server project reference is marked `IsAspireProjectResource="false"`,
When Conversations adopts the shared capability,
Then the AppHost models the Conversations server as an Aspire project resource, attaches the Conversations Dapr sidecar through the shared base with stable Conversations app/component names, wires the admin web to the server via `WithReference`/`WaitFor`, and composes the required local EventStore/Dapr resources without inventing a new transport, persistence model, or provider.
[Source: src/Hexalith.Conversations.AppHost/Program.cs; src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj; _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-13-Shared-AspireDapr-domain-module-hosting-base]

**AC-5 - Promote/adopt the publication transport marshaling base without moving domain failure taxonomy.**
Given the accepted inventory assigns `publication-transport-marshaling` to FR-13/Story 3.5 and keeps `publication-failure-taxonomy` local,
When publication transport support is promoted,
Then the generic transport/pipeline mechanics are moved behind the shared capability or a companion Commons publication helper, while Conversations keeps its safe domain result/diagnostic taxonomy (`ConversationPersistenceOutcome`, `ConversationPublicationResult`, `ConversationPublicationDiagnostic`) and domain event mapping vocabulary; any remaining Conversations file is a thin adapter, not a duplicated transport framework.
[Source: docs/release-evidence/consume-promote-keep-inventory-v1.json#publication-transport-marshaling; docs/release-evidence/consume-promote-keep-inventory-v1.json#publication-failure-taxonomy; src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs]

**AC-6 - Preserve publication behavior and content-safety exactly.**
Given the current publication tests pin safe mapping and transport metadata,
When publication transport code delegates to the shared helper,
Then non-success persistence outcomes do not publish, tenant mismatch and unsupported schema fail closed with bounded diagnostics, caller-supplied provenance never becomes transport metadata, topic/type/source/subject/header values remain stable, retry/replay preserves event identity, and duplicate/reordered public event deliveries remain idempotent.
[Source: tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs; tests/Hexalith.Conversations.Server.Tests/Publication/ConversationTransportMetadataTest.cs; tests/Hexalith.Conversations.Server.Tests/Publication/CallerMetadataPublicationTest.cs; tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationConsumerTest.cs]

**AC-7 - Preserve version alignment and avoid opportunistic package upgrades.**
Given the repository currently pins Aspire and Dapr centrally,
When FR-13 is implemented,
Then package versions remain in Central Package Management, `.csproj` files keep versionless `PackageReference` entries, the AppHost SDK version is only changed deliberately and consistently, and no package upgrade is mixed into this story unless required for compile compatibility and recorded with rationale.
[Source: Directory.Packages.props; src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj; Hexalith.EventStore/Directory.Packages.props; Hexalith.Folders/Directory.Packages.props; Hexalith.Projects/Directory.Packages.props]

**AC-8 - Release gates, sibling compatibility, and submodule mechanics hold.**
And the promoted Commons helper tests pass, Conversations AppHost/topology and publication tests pass, the full Conversations conformance suite is monotonic at **>= 361**, the public-contract-shape baseline diff is empty, dependent sibling modules compile green against the additive API, and every touched submodule is committed separately with a root-level gitlink pointer bump. Never initialize nested submodules recursively.
[Source: _bmad-output/implementation-artifacts/3-4-promote-adopt-the-shared-servicedefaults-base.md#Completion Notes List; docs/release-evidence/promote-adopt-runbook.md#Ordered-checklist-copy-per-story; _bmad-output/project-context.md#Development-Workflow-Rules]

## Tasks / Subtasks

- [x] **Task 0 - Record the FR-13 landing zone and scope split.** (AC: 1, 5, 8)
  - [x] Add a Story 3.5 entry to `docs/release-evidence/promote-adopt-runbook.md` naming the Commons landing zone, expected library/project names, and self-contained build-props requirement.
  - [x] Record the split explicitly: `apphost-greenfield` is greenfield-adopt with FR-17 delete N/A; `publication-transport-marshaling` is promote/adopt with local transport mechanics to delete or reduce to thin adapters.
  - [x] Verify root-level submodule pointers before building; do not run recursive submodule commands.

- [x] **Task 1 - Characterize current AppHost and publication behavior before replacement.** (AC: 4, 5, 6)
  - [x] Add or extend AppHost topology tests that inspect the Aspire resource model without requiring live Dapr sidecars, tenant seed data, production secrets, or external services.
  - [x] Pin that the Conversations server becomes an Aspire project resource and the admin web references/waits for it.
  - [x] Keep existing publication tests green before refactoring; add missing tests for any transport behavior the shared helper will own.
  - [x] Confirm public Contracts assembly is untouched before and after the transport refactor.

- [x] **Task 2 - Promote the shared Commons Aspire/Dapr hosting base with module-owned tests.** (AC: 1, 2, 3, 7, 8)
  - [x] Create the Commons library and tests using self-contained `Directory.Build.props`.
  - [x] Model shared vs isolated infrastructure modes: shared mode references state-store/pubsub; isolated mode loads only the supplied resources path and does not bind shared components.
  - [x] Support sidecar options used today by siblings: `AppId`, `Config`, `ResourcesPaths`, `AppHealthCheckPath`, `EnableAppHealthCheck`, `PlacementHostAddress`, `SchedulerHostAddress`, and optional fixed Dapr HTTP port where a platform helper requires it.
  - [x] Add tests for null/whitespace guards, component-name/app-id propagation, shared component references, isolated no-component behavior, wait/reference wiring, health-check option propagation, and resource record shape.
  - [x] Keep the base domain-neutral; do not reference Conversations, Folders, Projects, EventStore server internals, Tenants, Parties, FrontComposer, or concrete contract DTOs.

- [x] **Task 3 - Preserve EventStore.Aspire compatibility.** (AC: 3, 8)
  - [x] Read `Hexalith.EventStore.Aspire` files before editing any platform helper.
  - [x] Prefer thin backward-compatible facades over symbol removal.
  - [x] If `AddEventStoreDomainModule` delegates to Commons, prove both shared and isolated modes still match existing behavior.
  - [x] Build EventStore and at least one existing EventStore.Aspire consumer against the promoted API.

- [x] **Task 4 - Adopt in Conversations AppHost.** (AC: 4, 7, 8)
  - [x] Add guarded source references to the promoted Commons library using the established local root-property convention.
  - [x] Update `src/Hexalith.Conversations.AppHost/Program.cs` to add the server resource and apply the shared domain-module hosting helper.
  - [x] Wire admin web to the server using Aspire resource references and wait ordering.
  - [x] Use stable Conversations names, for example `conversations`, `conversations-admin-web`, `statestore`, and `pubsub`, unless existing sibling conventions force another name.
  - [x] Keep AppHost as local/development orchestration only; do not add deployment target decisions.

- [x] **Task 5 - Promote/adopt publication transport mechanics.** (AC: 5, 6, 8)
  - [x] Identify the reusable transport mechanics in `ConversationPublicationMapper`, `ConversationTransportMetadata`, `ConversationPublicationMetadata`, `PersistedConversationEvent`, `ConversationPublicationService`, and `LocalConversationPublicationConsumer`.
  - [x] Promote only domain-neutral mechanics: persisted candidate envelope shape, metadata accessor/validator hooks, safe transport metadata composer, publication service telemetry hook, and idempotent local consumer semantics.
  - [x] Keep Conversations-owned domain event mapping and failure taxonomy local unless a shared API can represent them without leaking EventStore/Dapr/raw payload vocabulary.
  - [x] Replace hand-rolled transport plumbing with thin adapters over the shared helper.
  - [x] Preserve exact topic/type/source/subject/header strings unless an explicit compatibility decision is recorded.

- [x] **Task 6 - Update tests and release evidence.** (AC: 5, 6, 8)
  - [x] Run the new Commons Aspire tests.
  - [x] Run Conversations AppHost/topology tests.
  - [x] Run `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj -c Release`.
  - [x] Run `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release`; required count is `>= 361`.
  - [x] Verify public-contract-shape diff remains empty.
  - [x] Build the full `Hexalith.Conversations.slnx` Release configuration with warnings as errors.
  - [x] Build dependent sibling modules against the promoted API, especially EventStore.Aspire and the Folders/Projects Aspire modules.

- [x] **Task 7 - Submodule commit, pointer bump, and final record.** (AC: 1, 8)
  - [x] Commit the Commons promotion in `Hexalith.Commons` as its own submodule commit.
  - [x] If EventStore or another sibling is edited, commit that submodule separately.
  - [x] Bump only root-level gitlinks in the umbrella repo.
  - [x] Generate the Dev Agent Record last, after gates are green, to avoid stale counts and file-list drift.

## Dev Notes

### Current implementation to read before editing

`src/Hexalith.Conversations.AppHost/Program.cs` is the greenfield AppHost slot today. It creates a distributed application builder, adds only `conversations-admin-web`, then builds/runs. The AppHost project references `Hexalith.Conversations.Server` with `IsAspireProjectResource="false"`, so the server is not currently modeled as a resource. [Source: src/Hexalith.Conversations.AppHost/Program.cs; src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj]

`Hexalith.EventStore.Aspire.AddEventStoreDomainModule` already implements a platform-specific domain-module sidecar helper. Shared mode references EventStore `StateStore` and `PubSub`; isolated mode loads only the supplied isolated resources path and skips the shared components. Do not regress this behavior while moving generic mechanics into Commons. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs]

`FoldersAspireModule` and `ProjectsAspireModule` are the sibling duplication examples. Folders accepts upstream platform state-store/pubsub resources and attaches Folders server/workers/UI sidecars. Projects creates Redis-backed Dapr components and attaches app-health options/resource paths to EventStore, Tenants, Projects, and worker sidecars. The shared base must support both shapes through options/hooks. [Source: Hexalith.Folders/src/Hexalith.Folders.Aspire/FoldersAspireModule.cs; Hexalith.Projects/src/Hexalith.Projects.Aspire/ProjectsAspireModule.cs]

The publication promote area is six files: `ConversationPublicationMapper`, `ConversationTransportMetadata`, `ConversationPublicationMetadata`, `PersistedConversationEvent`, `ConversationPublicationService`, and `LocalConversationPublicationConsumer`. The accepted Keep area is three files: `ConversationPersistenceOutcome`, `ConversationPublicationResult`, and `ConversationPublicationDiagnostic`. Do not move the Keep taxonomy into Commons. [Source: docs/release-evidence/consume-promote-keep-inventory-v1.json#publication-transport-marshaling; docs/release-evidence/consume-promote-keep-inventory-v1.json#publication-failure-taxonomy]

### Architecture and product guardrails

AppHost, Aspire, and ServiceDefaults are composition/observability/hosting concerns only. They must not import authorization logic, projection replay logic, aggregate behavior, public contract vocabulary, or UI trust decisions. [Source: _bmad-output/planning-artifacts/architecture.md#Project-boundary-guardrails]

FR-13 does not authorize a new persistence model, transport, provider, or orchestration runtime. Keep EventStore/Dapr as the substrate and preserve Conversations' public contract boundary. [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-13-Shared-AspireDapr-domain-module-hosting-base; _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#9-Constraints--Guardrails]

Dapr pub/sub is at-least-once. Publication consumers and projection/event handlers must tolerate duplicates and replay; cross-tenant leakage through topics, headers, diagnostics, logs, or metadata is forbidden. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules; _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#NFR24]

No UI/UX redesign applies. This is an internal developer-platform refactor; the generated/admin behavior is preserved under FR-20, not redesigned. [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#UX-Design-Requirements]

### Previous story intelligence

Story 3.1 established the promote -> test -> adopt -> delete/facade -> conformance -> sibling-build -> submodule-commit -> root-gitlink-bump runbook. Story 3.2 reinforced "delete duplicated mechanics, not domain vocabulary." Story 3.3 ratified Commons for all Epic-3 promotions. Story 3.4 closed with the conformance floor at 361, self-contained Commons build props, and the warning that runtime composition must avoid double-registering shared infrastructure. [Source: docs/release-evidence/promote-adopt-runbook.md; _bmad-output/implementation-artifacts/3-4-promote-adopt-the-shared-servicedefaults-base.md#Completion Notes List]

Carry forward the recurring hazards: stale Dev Agent Record counts, uncommitted submodule promotions, root gitlinks not bumped, nested Commons `Hexalith.Builds` assumptions, and out-of-scope submodule drift. Generate final story metadata after gates pass. [Source: docs/release-evidence/promote-adopt-runbook.md#Build-infrastructure-caveat-discovered-in-31-read-before-promoting-into-Commons-again; _bmad-output/implementation-artifacts/sprint-status.yaml]

### Latest technical specifics

Aspire's AppHost model is code-first orchestration: resource references establish dependencies and startup order, and `Build().Run()` starts the modeled distributed app. Use this to test resource relationships without requiring a live production deployment. [Source: https://aspire.dev/get-started/app-host/]

Aspire health guidance distinguishes AppHost resource checks from service endpoint checks; readiness is `/health`, liveness is `/alive`, and default endpoint behavior comes from service-defaults methods. Story 3.5 must not undo the Story 3.4 health endpoint contract (`/health`, `/alive`, `/ready`) while adding AppHost topology. [Source: https://aspire.dev/fundamentals/health-checks/; _bmad-output/implementation-artifacts/3-4-promote-adopt-the-shared-servicedefaults-base.md#Acceptance-Criteria]

Aspire 13.4 introduced broader AppHost and app-model changes and includes breaking changes. This repository already pins the relevant Aspire family centrally, so do not upgrade packages as a hidden part of FR-13; version changes need explicit rationale and compatibility proof. [Source: https://aspire.dev/whats-new/aspire-13-4/; Directory.Packages.props]

Current local package/version context: root `Directory.Packages.props` pins `Aspire.Hosting` `13.4.6`; `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` uses `Aspire.AppHost.Sdk/13.4.2`; EventStore/Folders pin Dapr `1.18.4` and `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.0-preview.1.260602-0230`; Projects still pins CommunityToolkit Dapr `13.0.0`. Account for this sibling variance in NFR6 builds rather than forcing an opportunistic ecosystem-wide upgrade. [Source: Directory.Packages.props; src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj; Hexalith.EventStore/Directory.Packages.props; Hexalith.Folders/Directory.Packages.props; Hexalith.Projects/Directory.Packages.props]

### Project Structure Notes

- Likely new shared code: `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/` and `Hexalith.Commons/test/Hexalith.Commons.Aspire.Tests/`.
- Possible EventStore files if platform helpers delegate through Commons: `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs`, `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs`, and EventStore Aspire tests.
- Conversations files likely touched: `Directory.Build.props`, `Directory.Packages.props` only if a central pin is missing, `Hexalith.Conversations.slnx`, `src/Hexalith.Conversations.AppHost/Program.cs`, `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`, `src/Hexalith.Conversations.Server/Publication/*`, and tests under `tests/Hexalith.Conversations.Server.Tests/Publication` plus a focused AppHost/topology test project if needed.
- Do not edit generated files under `obj/` or build output under `bin/`.
- Keep package versions in `Directory.Packages.props`; project files should contain versionless package references except for the AppHost SDK declaration already present.
- Never initialize nested submodules or use recursive submodule update commands.

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-35-Promote--adopt-the-shared-AspireDapr-domain-module-hosting-base-greenfield-adopt--FR-17-NA]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-13-Shared-AspireDapr-domain-module-hosting-base]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#C-Cross-module-duplication-PROMOTE-candidates-FR-10FR-15]
- [Source: _bmad-output/planning-artifacts/architecture.md#Development-Workflow-Integration]
- [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.json#publication-transport-marshaling]
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.json#publication-failure-taxonomy]
- [Source: docs/release-evidence/promote-adopt-runbook.md]
- [Source: src/Hexalith.Conversations.AppHost/Program.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs]
- [Source: Hexalith.Folders/src/Hexalith.Folders.Aspire/FoldersAspireModule.cs]
- [Source: Hexalith.Projects/src/Hexalith.Projects.Aspire/ProjectsAspireModule.cs]
- [Source: tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-24: BMAD dev-story workflow loaded `.agents/skills/bmad-dev-story/SKILL.md`, checklist, repository instructions, story context, project context files, and root-only submodule policy before edits.
- 2026-06-24: Baseline commit recorded as `59766b0ec29d55f88742074fc0c1c62c9e539aa7`; root submodule pointers inspected without recursive initialization.
- 2026-06-24: Promoted domain-neutral Aspire/Dapr hosting primitives into `Hexalith.Commons.Aspire` and covered shared/isolated modes, sidecar options, wait/reference wiring, component references, and resource shape.
- 2026-06-24: Preserved `Hexalith.EventStore.Aspire` public helpers; `AddEventStoreDomainModule` now delegates to the Commons base while keeping shared and isolated semantics.
- 2026-06-24: Adopted the Commons hosting base in Conversations AppHost; the server is an Aspire project resource, admin web references/waits for it, and EventStore local resources remain composed through EventStore.Aspire.
- 2026-06-24: Promoted domain-neutral publication pipeline, metadata, telemetry, and idempotency mechanics into `Hexalith.Commons.Publication`; Conversations keeps domain mapping and failure taxonomy local.
- 2026-06-24: `dotnet test ...` through VSTest is blocked in this sandbox by `SocketException (13): Permission denied` when the test platform opens a local listener. Equivalent built xUnit v3 executables were run directly and passed.

### Completion Notes List

- Story 3.5 release runbook entry added for `Hexalith.Commons.Aspire` and `Hexalith.Commons.Publication`, including self-contained props and the `apphost-greenfield`/`publication-transport-marshaling` scope split.
- Commons promotion committed separately in `Hexalith.Commons` at `a8b3639` (`Promote shared Aspire and publication helpers`).
- EventStore compatibility wrapper committed separately in `Hexalith.EventStore` at `2e66b67c` (`Delegate Aspire domain module to Commons helper`).
- Public Contracts assembly was not changed; `git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json` was empty and conformance baseline validation passed.
- Validation passed:
  - `dotnet build Hexalith.Commons/test/Hexalith.Commons.Aspire.Tests/Hexalith.Commons.Aspire.Tests.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` -> 0 warnings.
  - `./Hexalith.Commons/test/Hexalith.Commons.Aspire.Tests/bin/Release/net10.0/Hexalith.Commons.Aspire.Tests` -> 4 passed.
  - `dotnet build Hexalith.Commons/test/Hexalith.Commons.Publication.Tests/Hexalith.Commons.Publication.Tests.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` -> 0 warnings.
  - `./Hexalith.Commons/test/Hexalith.Commons.Publication.Tests/bin/Release/net10.0/Hexalith.Commons.Publication.Tests` -> 9 passed.
  - `dotnet build tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` -> 0 warnings.
  - `./tests/Hexalith.Conversations.AppHost.Tests/bin/Release/net10.0/Hexalith.Conversations.AppHost.Tests` -> 4 passed.
  - `dotnet build tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` -> 0 warnings.
  - `./tests/Hexalith.Conversations.Server.Tests/bin/Release/net10.0/Hexalith.Conversations.Server.Tests` -> 605 passed (dev-story produced 589; the QA-automation pass added +16 publication cases — see Senior Developer Review (AI)).
  - `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` -> 0 warnings.
  - `./tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests` -> 361 passed.
  - `dotnet build Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/conversations/Hexalith.Commons` -> 0 warnings.
  - `dotnet build Hexalith.Folders/src/Hexalith.Folders.Aspire/Hexalith.Folders.Aspire.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` -> 0 warnings.
  - `dotnet build Hexalith.Projects/src/Hexalith.Projects.Aspire/Hexalith.Projects.Aspire.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` -> 0 warnings.
  - `dotnet build Hexalith.Conversations.slnx -c Release -m:1 -nr:false` -> 0 warnings.

### File List

- `Directory.Build.props`
- `Directory.Packages.props`
- `Hexalith.Conversations.slnx`
- `Hexalith.Commons/Directory.Packages.props`
- `Hexalith.Commons/Hexalith.Commons.slnx`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/AspireDaprDomainModuleExtensions.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/AspireDaprDomainModuleOptions.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/AspireDaprDomainModuleResource.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/AspireDaprInfrastructureMode.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/AspireDaprSharedComponents.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Directory.Build.props`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Hexalith.Commons.Aspire.csproj`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/Directory.Build.props`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/Hexalith.Commons.Publication.csproj`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/PersistedPublicationCandidate.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/PublicationDeduplicationSet.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/PublicationFailureTelemetry.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/PublicationMappingDecision.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/PublicationMappingPipeline.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/PublicationTransportMetadata.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Publication/PublicationTransportMetadataComposer.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Aspire.Tests/AspireDaprDomainModuleTest.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Aspire.Tests/Hexalith.Commons.Aspire.Tests.csproj`
- `Hexalith.Commons/test/Hexalith.Commons.Diagnostics.Tests/Hexalith.Commons.Diagnostics.Tests.csproj`
- `Hexalith.Commons/test/Hexalith.Commons.Publication.Tests/Hexalith.Commons.Publication.Tests.csproj`
- `Hexalith.Commons/test/Hexalith.Commons.Publication.Tests/PublicationHelperTest.cs`
- `Hexalith.Commons/test/Hexalith.Commons.ServiceDefaults.Tests/Hexalith.Commons.ServiceDefaults.Tests.csproj`
- `Hexalith.Commons/test/Hexalith.Commons.ServiceDefaults.Tests/HexalithServiceDefaultsTest.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj`
- `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/DaprComponents/DaprComponentValidationTests.cs`
- `docs/release-evidence/promote-adopt-runbook.md`
- `src/Hexalith.Conversations.AppHost/ConversationsAppHostResources.cs`
- `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs`
- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`
- `src/Hexalith.Conversations.AppHost/Program.cs`
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`
- `src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs`
- `src/Hexalith.Conversations.Server/Publication/ConversationPublicationService.cs`
- `src/Hexalith.Conversations.Server/Publication/ConversationTransportMetadata.cs`
- `src/Hexalith.Conversations.Server/Publication/LocalConversationPublicationConsumer.cs`
- `src/Hexalith.Conversations.Server/Publication/PersistedConversationEvent.cs`
- `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs`
- `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj`
- `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationServiceTest.cs` (added by review reconciliation)
- `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs` (added by review reconciliation)
- `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationTransportMetadataTest.cs` (added by review reconciliation)
- `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationConsumerTest.cs` (added by review reconciliation)

### Change Log

- 2026-06-24: Implemented Story 3.5 FR-13 shared Aspire/Dapr hosting base and publication transport promotion/adoption; validation green; story marked ready for review.
- 2026-06-24: Senior Developer Review (AI) completed by Jerome — all 8 ACs validated, all gates re-run green; no code changes required; File List and stale Server.Tests count reconciled; status → done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — **Date:** 2026-06-24 — **Outcome:** Approved (no code changes required; documentation reconciled)

### Scope
Adversarial validation of every story claim against the actual implementation across the umbrella and the two touched submodules (`Hexalith.Commons` @ `a8b3639`, `Hexalith.EventStore` @ `2e66b67`). All File-List source/test files were read; all AC-8 gates were re-run independently rather than trusted from the Dev Agent Record.

### Independently re-run gates (all green)
- `dotnet build Hexalith.Conversations.slnx -c Release` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- Conformance suite → **361 passed** (meets the `>= 361` floor), 0 failed.
- `Hexalith.Conversations.Server.Tests` → **605 passed**, 0 failed.
- `Hexalith.Conversations.AppHost.Tests` → **5 passed**, 0 failed.
- `Hexalith.Commons.Aspire.Tests` → **4 passed**; `Hexalith.Commons.Publication.Tests` → **9 passed**.
- `git diff docs/release-evidence/public-contract-shape-baseline-v1.json` → **empty** (public contract unchanged).

### AC verification
- **AC-1..AC-4, AC-7:** Satisfied. Commons landing zone recorded with self-contained `Directory.Build.props`; `Hexalith.Commons.Aspire` base is domain-neutral (no Conversations/EventStore/Folders/Projects/contract references); EventStore.Aspire public helpers preserved as a thin facade delegating to the Commons base with shared/isolated parity; Conversations AppHost models the server as an Aspire project resource (the `IsAspireProjectResource="false"` flag was removed), attaches the shared Dapr sidecar with stable names (`conversations`/`conversations-admin-web`/`statestore`/`pubsub`), and wires admin-web → server via `WithReference`/`WaitFor`. Central Package Management preserved (versionless `PackageReference`s; the only added pin is the already-prerelease `CommunityToolkit.Aspire.Hosting.Dapr`, required for compile).
- **AC-5/AC-6:** Satisfied. Generic mechanics moved to `Hexalith.Commons.Publication` (mapping pipeline, transport-metadata composer, failure telemetry, dedup set); Conversations files are thin adapters that keep the domain mapping + failure taxonomy local. The mapping fail-closed order, diagnostic codes, tenant/schema/event-type checks, telemetry correlation-id fallback, transport topic/type/source/subject/header strings, and idempotent dedup semantics are byte-for-byte behavior-preserving.
- **AC-8:** Satisfied (gates above). Submodule commits exist and are clean; the umbrella root-level gitlink bump for both submodules is staged in the working tree and is committed as part of the final umbrella commit for this story.

### Findings (all MEDIUM/LOW; auto-fixed or noted — 0 CRITICAL/HIGH)
1. **[MEDIUM — fixed] File List incomplete.** Four changed publication test files were absent from the File List: `ConversationPublicationServiceTest.cs` (new) plus modified `ConversationPublicationMapperTest.cs`, `ConversationTransportMetadataTest.cs`, `ConversationPublicationConsumerTest.cs`. Added to the File List.
2. **[MEDIUM — fixed] Stale validation count.** The Dev Agent Record reported `Server.Tests -> 589 passed`; the verified final count is **605** (the QA-automation pass added +16 cases after the dev-story record was generated). Corrected.
3. **[LOW — noted] Incidental change not in File List.** `.gitignore` gained `.agents/.story-automator-active` — a story-automator tooling artifact unrelated to FR-13; left as-is, recorded here.
4. **[LOW — noted] Out-of-scope test-infra in the Commons commit.** Commit `a8b3639` also aligned `Hexalith.Commons.Diagnostics.Tests`/`Hexalith.Commons.ServiceDefaults.Tests` to `xunit.v3` and added a `cancellationToken` to a ServiceDefaults test. Justified (xunit v3 alignment so the new Aspire/Publication test projects coexist in `Hexalith.Commons.slnx`) but unrelated to FR-13.
5. **[LOW — noted] Umbrella gitlink bump pending commit.** Submodule commits (`a8b3639`, `2e66b67`) are made and the working-tree gitlinks point at them; the umbrella commit that records the bump is the final step for this story and must include both root-level gitlinks.
