---
baseline_commit: 48d3099
---

# Story 3.4: Promote & adopt the shared ServiceDefaults base

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a technical-module maintainer,
I want a shared ServiceDefaults base with module-specific extension hooks, adopted by Conversations into its currently-empty ServiceDefaults slot,
so that observability, health, resilience, and service discovery have one home instead of a copied per-module file.

## Acceptance Criteria

**AC-1 - Resolve and promote the FR-10 ServiceDefaults base in the ratified technical module.**
Given Epic 3 OQ-1 has already been ratified as "Commons, all Epic-3",
When the shared ServiceDefaults capability is promoted,
Then it lives in a new additive `Hexalith.Commons.ServiceDefaults` library under `Hexalith.Commons`, uses self-contained build props like Stories 3.1-3.3, and exposes a reusable base for OpenTelemetry, health checks, service discovery, and HTTP resilience with module-specific hooks.
[Source: docs/release-evidence/promote-adopt-runbook.md#0-resolve-the-landing-zone-gating-precondition-dont-promote-into-the-dark; _bmad-output/implementation-artifacts/3-3-promote-adopt-the-diagnostics-telemetry-scaffolding-helper.md#Previous story intelligence]

**AC-2 - Preserve the existing Conversations runtime ServiceDefaults path.**
Given `src/Hexalith.Conversations.Server/Program.cs` currently calls `builder.AddEventStoreDomainService(...)`,
And `Hexalith.EventStore.DomainService` currently calls `builder.AddServiceDefaults()` from `Hexalith.EventStore.ServiceDefaults`,
When Conversations adopts the shared base,
Then health, telemetry, service discovery, and resilience registration remain active exactly once, no duplicate/competing ServiceDefaults registration is introduced, and `ConversationsDomainServiceHostCompositionTest.ServiceDefaultsHealthEndpointShouldResolve` still sees `/health`, `/alive`, and `/ready`.
[Source: src/Hexalith.Conversations.Server/Program.cs; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs; tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs]

**AC-3 - Replace the empty Conversations ServiceDefaults marker with a real thin wrapper.**
Given `src/Hexalith.Conversations.ServiceDefaults` currently contains only `ServiceDefaultsAssemblyMarker`,
When the story adopts the shared base,
Then `Hexalith.Conversations.ServiceDefaults` exposes Conversations-owned hooks/configuration over the shared base, keeps domain-specific names and activity/meter sources in Conversations, and records FR-17 delete as N/A because there is no local copy to remove.
[Source: src/Hexalith.Conversations.ServiceDefaults/ServiceDefaultsAssemblyMarker.cs; docs/release-evidence/consume-promote-keep-inventory-v1.json#service-defaults-greenfield]

**AC-4 - Preserve health endpoint behavior and development JSON response behavior.**
Given the current EventStore ServiceDefaults maps `/health`, `/alive`, and `/ready`, treats `Healthy` and `Degraded` as HTTP 200 and `Unhealthy` as HTTP 503, and writes detailed JSON responses for `/health` and `/ready` in Development,
When the shared base is adopted,
Then those Conversations-visible route paths, status-code mappings, liveness/readiness predicates, and development response semantics remain behavior-identical unless an explicitly recorded architecture decision changes them.
[Source: Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs; tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs]

**AC-5 - Preserve observability continuity and disclosure safety.**
Given NFR4 requires metric names, dimensions, health endpoints, and dashboard/alert contracts to keep working,
When OpenTelemetry registration runs through the shared base,
Then it preserves JSON console logging, OpenTelemetry logging scopes/formatted-message behavior, ASP.NET Core/HTTP/runtime instrumentation, OTLP exporter activation via `OTEL_EXPORTER_OTLP_ENDPOINT`, health-probe trace exclusion, and content-safe bounded telemetry conventions established in Story 3.3.
[Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-10; _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-20; _bmad-output/implementation-artifacts/3-3-promote-adopt-the-diagnostics-telemetry-scaffolding-helper.md#Metric contract to preserve]

**AC-6 - Generalization handles known sibling variance without forcing sibling rewrites.**
Given Folders, Memories, Parties, Projects, and EventStore carry near-identical ServiceDefaults code with real differences,
When the Commons base is designed,
Then the base supports module service name/resource naming, custom activity sources, custom meters, health endpoint path options, liveness/readiness tag selection, default readiness checks, Dapr/Redis/FalkorDB/custom health or tracing hooks, and development response writer customization, while remaining additive and backward-compatible for sibling modules.
[Source: Hexalith.Folders/src/Hexalith.Folders.ServiceDefaults/Extensions.cs; Hexalith.Memories/src/Hexalith.Memories.ServiceDefaults/Extensions.cs; Hexalith.Parties/src/Hexalith.Parties.ServiceDefaults/Extensions.cs; Hexalith.Projects/src/Hexalith.Projects.ServiceDefaults/ProjectsServiceDefaults.cs; Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs]

**AC-7 - Release gates, sibling compatibility, and submodule mechanics hold.**
And the promoted helper module tests pass, Conversations Server tests pass, the full Conversations conformance suite is monotonic at **>= 361**, the public-contract-shape baseline diff is empty, dependent sibling builds are green against the promoted API, and the Commons promotion is committed as a separate submodule commit plus a root-level gitlink pointer bump. Never initialize nested submodules recursively.
[Source: _bmad-output/implementation-artifacts/3-3-promote-adopt-the-diagnostics-telemetry-scaffolding-helper.md#Completion Notes List; docs/release-evidence/promote-adopt-runbook.md#Ordered checklist copy per story]

## Tasks / Subtasks

- [x] **Task 0 - Record the FR-10 landing zone before code edits.** (AC: 1, 7)
  - [x] Add a Story 3.4 FR-10 entry to `docs/release-evidence/promote-adopt-runbook.md` stating the ratified landing zone: `Hexalith.Commons.ServiceDefaults` in the `Hexalith.Commons` submodule.
  - [x] Mirror Stories 3.1-3.3 build mechanics: self-contained `Directory.Build.props` in the new Commons library so umbrella builds do not require Commons' nested `Hexalith.Builds`.
  - [x] Verify root-level submodule pointers before building; do not use recursive submodule commands.

- [x] **Task 1 - Characterize existing ServiceDefaults behavior before replacement.** (AC: 2, 4, 5)
  - [x] Pin route mapping for `/health`, `/alive`, and `/ready` in Conversations host composition tests.
  - [x] Pin health status code mapping: `Healthy` -> 200, `Degraded` -> 200, `Unhealthy` -> 503.
  - [x] Pin Development JSON response shape for `/health` and `/ready` enough to catch accidental plaintext-only regression.
  - [x] Pin health-probe trace exclusion for `/health`, `/alive`, and `/ready`.
  - [x] Pin the current registration side effects: service discovery registered, HTTP client defaults include standard resilience and service discovery, OpenTelemetry logging/metrics/tracing configured, OTLP exporter activates only when `OTEL_EXPORTER_OTLP_ENDPOINT` is present.

- [x] **Task 2 - Promote the shared Commons ServiceDefaults base with module-owned tests.** (AC: 1, 4, 5, 6, 7)
  - [x] Create `Hexalith.Commons/src/libraries/Hexalith.Commons.ServiceDefaults/`.
  - [x] Provide additive extension/helper APIs that let a module configure:
    - service/resource name,
    - activity sources and meter names,
    - health endpoint paths, defaulting to `/health`, `/alive`, `/ready`,
    - liveness/readiness tags,
    - whether the default self check is liveness-only or liveness+readiness,
    - additional health checks,
    - extra metrics/tracing/logging hooks,
    - development health response writer.
  - [x] Keep generic defaults domain-neutral; do not reference Conversations contracts, EventStore server internals, Tenants, Parties, FrontComposer, Dapr actor types, Redis, or FalkorDB from the Commons base.
  - [x] Add Commons tests for null guards, default registration, endpoint path selection, liveness/readiness predicates, status code mapping, Development JSON writer behavior, OTLP env-gated exporter registration, health trace filter, service discovery, and HTTP resilience defaults.
  - [x] Add tests proving hook execution order: shared defaults first, then module hooks, without silently dropping module-specific instrumentation.

- [x] **Task 3 - Adopt without double-registering the current runtime path.** (AC: 2, 3, 4, 5)
  - [x] Read `Hexalith.EventStore.DomainService.EventStoreDomainServiceExtensions` before editing anything that affects `builder.AddEventStoreDomainService`.
  - [x] Prefer a behavior-preserving path where the actual runtime ServiceDefaults call used by Conversations delegates to the Commons base exactly once.
  - [x] If direct Conversations ServiceDefaults adoption requires bypassing SDK defaults, add only an additive EventStore DomainService overload/option that preserves current default behavior for all existing callers.
  - [x] Do not call both EventStore ServiceDefaults and Conversations ServiceDefaults independently unless tests prove the registration is idempotent and all routes/instrumentation are not duplicated.
  - [x] Replace `ServiceDefaultsAssemblyMarker` with a real Conversations wrapper/hook surface, or keep the marker only beside real extension code if a marker remains useful.

- [x] **Task 4 - Preserve sibling shapes and avoid premature sibling rewrites.** (AC: 6, 7)
  - [x] Use EventStore, Folders, Memories, Parties, and Projects ServiceDefaults as examples of the supported option/hook surface.
  - [x] Do not refactor every sibling module to the new base unless required for a compile/API proof; this story's required adopter is Conversations.
  - [x] Build sibling modules against the additive API to prove NFR6, especially EventStore because it owns the current runtime ServiceDefaults path used by Conversations.
  - [x] If any sibling has to be edited for compile compatibility, keep those edits thin, backward-compatible facades over the Commons base and commit the touched submodule separately.

- [x] **Task 5 - Update Conversations references and tests.** (AC: 2, 3, 4, 5, 7)
  - [x] Update `Directory.Build.props` Commons-root detection to include `Hexalith.Commons.ServiceDefaults`.
  - [x] Add guarded `ProjectReference` entries for the new Commons library wherever Conversations adopts it from source.
  - [x] Update `Hexalith.Conversations.slnx` to include the new library and tests if the repo convention requires promoted Commons projects in the umbrella solution.
  - [x] Add or extend Conversations tests under `tests/Hexalith.Conversations.Server.Tests` and/or a focused ServiceDefaults test project to prove the AC-2 through AC-5 behavior.
  - [x] Keep `ConversationsDomainServiceHostCompositionTest` green and extend it only with behavior-preserving assertions.

- [x] **Task 6 - Release-gate proof.** (AC: 4, 5, 7)
  - [x] Run the new Commons ServiceDefaults tests. (14/14 passed)
  - [x] Run `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj -c Release`. (589/589 passed; deterministic across 15 review re-runs after the telemetry test-isolation fix.)
  - [x] Run `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release`; required count is `>= 361`. (361/361 passed — monotonic gate met.)
  - [x] Run a full Release build of `Hexalith.Conversations.slnx` with warnings as errors. (0 warnings / 0 errors.)
  - [x] Verify the public-contract-shape diff remains empty. (Contracts assembly untouched; conformance contract-shape guards green at 361.)
  - [x] Build relevant sibling modules against the promoted API and record evidence. (EventStore ServiceDefaults delegates through Commons and compiles green in the umbrella Release build; the host-composition test exercises the EventStore runtime path.)

- [x] **Task 7 - Submodule commit, pointer bump, and final record.** (AC: 1, 7)
  - [x] Commit the Commons promotion in `Hexalith.Commons` as its own submodule commit and push if the workflow requires remote availability.
  - [x] Bump the root `Hexalith.Commons` gitlink in the umbrella repo.
  - [x] If EventStore or another sibling was edited, commit and bump that submodule separately.
  - [x] Generate the Dev Agent Record last, after gates are green, to avoid stale counts and file-list drift.

## Dev Notes

### Current implementation to read before editing

`src/Hexalith.Conversations.ServiceDefaults` is an empty greenfield slot today: the project carries ServiceDefaults package references, but the only source file is `ServiceDefaultsAssemblyMarker`. That is why FR-17 delete is N/A: there is no local ServiceDefaults implementation to remove. [Source: src/Hexalith.Conversations.ServiceDefaults/ServiceDefaultsAssemblyMarker.cs; src/Hexalith.Conversations.ServiceDefaults/Hexalith.Conversations.ServiceDefaults.csproj]

`src/Hexalith.Conversations.Server/Program.cs` does not reference `Hexalith.Conversations.ServiceDefaults`. Its observable ServiceDefaults behavior arrives through `builder.AddEventStoreDomainService(...)`, which calls `builder.AddServiceDefaults()` inside `Hexalith.EventStore.DomainService`. Do not break the two-line host pattern or accidentally register two sets of health endpoints. [Source: src/Hexalith.Conversations.Server/Program.cs; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]

`Hexalith.EventStore.ServiceDefaults.Extensions` is the live behavior Conversations sees today: it configures OpenTelemetry logging, JSON console logging, ASP.NET Core/HTTP/runtime metrics, tracing with health-probe filtering, OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, service discovery, HTTP resilience, a `self` liveness check, and `/health`, `/alive`, `/ready` endpoint mapping. Development `/health` and `/ready` use a detailed JSON response writer that tolerates non-serializable health-check data. [Source: Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs]

`ConversationsDomainServiceHostCompositionTest` already asserts `/health`, `/alive`, and `/ready` routes are mapped alongside EventStore domain routes. Extend this as the first Conversations guardrail before changing runtime wiring. [Source: tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs]

### Sibling variance the shared base must support

EventStore and Parties are closest to the default Aspire shape: `/health`, `/alive`, `/ready`, health-probe trace exclusion, JSON console logging, OpenTelemetry metrics/tracing, service discovery, HTTP resilience, and dev JSON health responses. Parties intentionally registers no placeholder self check because real Dapr health checks are added in `Program.cs`. [Source: Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs; Hexalith.Parties/src/Hexalith.Parties.ServiceDefaults/Extensions.cs]

Projects uses the same endpoint paths, adds a module name, uses `IncludeFormattedMessage = false`, treats the `self` check as both live and ready, and writes metadata-only Development health JSON. [Source: Hexalith.Projects/src/Hexalith.Projects.ServiceDefaults/ProjectsServiceDefaults.cs; Hexalith.Projects/tests/Hexalith.Projects.Server.Tests/ServiceDefaultsEndpointTests.cs]

Folders uses `/health/live` and `/health/ready` plus `/health` compatibility alias, has a monitored snapshot readiness check, and reports degraded-but-serving readiness as HTTP 200. The base must support this shape via options/hooks even if Conversations keeps `/health`, `/alive`, `/ready`. [Source: Hexalith.Folders/src/Hexalith.Folders.ServiceDefaults/Extensions.cs; Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/ServiceDefaultsHealthEndpointTests.cs]

Memories adds Redis/FalkorDB tracing hooks and keyed `IConnectionMultiplexer` guards. Keep those dependencies out of Commons; provide extension hooks so Memories can attach them in its own module. [Source: Hexalith.Memories/src/Hexalith.Memories.ServiceDefaults/Extensions.cs; Hexalith.Memories/src/Hexalith.Memories.ServiceDefaults/Telemetry/FalkorDbSemanticAttributeProcessor.cs]

### Architecture and product guardrails

ServiceDefaults owns composition, observability, and hosting defaults only; it must not import domain decisions, authorization logic, projection replay logic, or public contract vocabulary. [Source: _bmad-output/planning-artifacts/architecture.md#Project boundary guardrails]

Runtime configuration lives under `Server/Configuration`, `AppHost`, and `ServiceDefaults`; ServiceDefaults owns shared OpenTelemetry, health, discovery, and resilience defaults. [Source: _bmad-output/planning-artifacts/architecture.md#Development Workflow Integration]

FR-10 is greenfield-adopt for Conversations. Do not manufacture a delete just to satisfy FR-17; record that no local copy exists and adoption is by adding real hooks over the shared base. [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 3.4; docs/release-evidence/consume-promote-keep-inventory-v1.json#service-defaults-greenfield]

No UI/UX redesign applies. The PRD explicitly says this is an internal developer-platform refactor and existing generated admin behavior is preserved under FR-20. Ignore unrelated UX-map rows that mention Story 3.4 unless a later approved story changes scope. [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#UX/UI Requirements]

### Previous story intelligence

Story 3.1 established the promote -> test -> adopt -> delete/facade -> conformance -> sibling-build -> submodule-commit -> root-gitlink-bump pipeline. Story 3.2 reinforced "delete duplicated mechanics, not domain vocabulary." Story 3.3 ratified Commons for all Epic-3 promotions and ended with conformance 361 green, so this story's monotonic gate is `>= 361`. [Source: docs/release-evidence/promote-adopt-runbook.md; _bmad-output/implementation-artifacts/3-1-promote-adopt-the-generic-typed-httpclient-registration.md; _bmad-output/implementation-artifacts/3-2-promote-adopt-the-generic-tenant-access-projection-handler-registration.md; _bmad-output/implementation-artifacts/3-3-promote-adopt-the-diagnostics-telemetry-scaffolding-helper.md]

The recurring failure pattern is stale story metadata, missed submodule commit/pointer mechanics, and assuming nested Commons submodules are initialized. Keep the Commons library self-contained and generate the Dev Agent Record last. [Source: docs/release-evidence/promote-adopt-runbook.md#Build-infrastructure caveat discovered in 3.1 read before promoting into Commons again]

### Latest technical specifics

Microsoft's Aspire health-check guidance distinguishes application endpoint checks from AppHost resource checks and documents default app health endpoints when `AddServiceDefaults` and `MapDefaultEndpoints` are called: readiness at `/health` and liveness at `/alive`; it also recommends protecting non-development health endpoints if enabled. Conversations currently adds `/ready` through Hexalith's local ServiceDefaults convention, so preserve the local three-endpoint contract. [Source: https://aspire.dev/fundamentals/health-checks/]

The repository already pins the relevant service-defaults package family through Central Package Management: `Microsoft.Extensions.Http.Resilience` 10.6.0, `Microsoft.Extensions.ServiceDiscovery` 10.6.0, and `OpenTelemetry.Extensions.Hosting` 1.15.3. NuGet lists those versions as supporting `net10.0` or compatible higher targets. Do not upgrade package versions as part of this story; align to repo pins unless an ADR explicitly authorizes a version change. [Source: Directory.Packages.props; https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience/10.6.0; https://www.nuget.org/packages/Microsoft.Extensions.ServiceDiscovery/10.6.0; https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting/1.15.3]

### Project Structure Notes

- Likely new shared code: `Hexalith.Commons/src/libraries/Hexalith.Commons.ServiceDefaults/` and `Hexalith.Commons/test/Hexalith.Commons.ServiceDefaults.Tests/`.
- Conversations files likely touched: `Directory.Build.props`, `Hexalith.Conversations.slnx`, `src/Hexalith.Conversations.ServiceDefaults/`, `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`, and `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs`.
- Possible EventStore files if the runtime path must delegate through the SDK: `Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs`, `Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs`, and EventStore ServiceDefaults/DomainService tests. Keep changes additive/backward-compatible.
- Do not edit generated files under `obj/` or build output under `bin/`.
- Keep package versions in `Directory.Packages.props`; `.csproj` files should contain unversioned `PackageReference` entries.
- Never initialize nested submodules or use recursive submodule update commands.

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 3.4]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-10]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-20]
- [Source: _bmad-output/planning-artifacts/architecture.md#Development Workflow Integration]
- [Source: _bmad-output/project-context.md#Development Workflow Rules]
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.json#service-defaults-greenfield]
- [Source: docs/release-evidence/promote-adopt-runbook.md]
- [Source: src/Hexalith.Conversations.ServiceDefaults/ServiceDefaultsAssemblyMarker.cs]
- [Source: src/Hexalith.Conversations.Server/Program.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]
- [Source: tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-08: BMAD create-story workflow resolved with no activation prepend/append steps; persistent project context loaded from `_bmad-output/project-context.md` plus sibling project-context files discovered by the configured glob.
- 2026-06-08: Story 3.4 selected explicitly by user; sprint status showed `3-4-promote-adopt-the-shared-servicedefaults-base: backlog` and `epic-3: in-progress`.
- 2026-06-08: Input discovery loaded PRD/epics, architecture, project context, Story 3.3, runbook, inventory, current Conversations ServiceDefaults/Server/AppHost files, EventStore DomainService/ServiceDefaults files, sibling ServiceDefaults examples, and current package/version context.
- 2026-06-08: Dev-story workflow resolved with no activation prepend/append steps; persistent project context loaded from root and sibling `project-context.md` files; `baseline_commit: 48d3099` preserved.
- 2026-06-08: Root-level submodule status inspected before build; no recursive submodule commands used.
- 2026-06-08: Implemented `Hexalith.Commons.ServiceDefaults` as a domain-neutral shared base with service/resource naming, activity/meter names, health endpoint paths/tags, optional self-check readiness, health/logging/metrics/tracing hooks, development JSON writer, OTLP env gate, service discovery, and HTTP resilience defaults.
- 2026-06-08: EventStore `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, and `MapDefaultEndpoints` now delegate to Commons while preserving the existing Conversations runtime path through `AddEventStoreDomainService`.
- 2026-06-08: Added Conversations ServiceDefaults wrapper with Conversations-owned service/meter naming over the Commons base; Server `Program.cs` remains on the existing EventStore DomainService path to avoid double registration.
- 2026-06-08: VSTest execution blocked in this sandbox for Commons, Server, and Conformance test projects by `System.Net.Sockets.SocketException (13): Permission denied` when the test platform opens its local listener; test assemblies compile.
- 2026-06-08: Validation completed: full `Hexalith.Conversations.slnx` Release build 0 warnings/0 errors; EventStore, Projects, Parties, Folders, and Memories ServiceDefaults builds 0 warnings/0 errors; public-contract-shape baseline diff empty.
- 2026-06-08: Submodule commits created locally: Commons `6adbf2b` (`feat: add shared service defaults base`) and EventStore `667db888` (`refactor: delegate service defaults to commons`); root gitlinks are bumped in the umbrella working tree.

### Completion Notes List

- Story context generated by BMAD create-story workflow on 2026-06-08; validated against `.claude/skills/bmad-create-story/checklist.md`.
- Key implementation hazard captured: Conversations currently receives ServiceDefaults through `AddEventStoreDomainService`, so adoption must avoid double registration and preserve the live SDK runtime path.
- FR-17 delete is explicitly N/A for Story 3.4 because Conversations has only a greenfield marker project, not a local ServiceDefaults implementation.
- FR-10 landing zone recorded in the promote/adopt runbook as `Hexalith.Commons.ServiceDefaults` in the `Hexalith.Commons` submodule.
- Commons ServiceDefaults promoted with self-contained build props and module-owned tests; generic defaults stay domain-neutral and expose module hooks for known sibling variance without taking dependencies on Conversations/EventStore server internals/Tenants/Parties/FrontComposer/Dapr/Redis/FalkorDB.
- EventStore ServiceDefaults is a backward-compatible facade over Commons, preserving `/health`, `/alive`, `/ready`, status-code mapping, development JSON responses, health-probe trace exclusion, JSON console logging, OpenTelemetry scopes/formatted messages, service discovery, HTTP resilience, and OTLP env-gated export.
- Conversations ServiceDefaults now has a real thin wrapper/hook surface over Commons while the live Server host continues to use the existing EventStore DomainService path exactly once.
- Release-gate test execution is not complete in this sandbox because VSTest cannot open its local socket; story remains `in-progress` until Commons/Server/Conformance tests can be executed and pass in an environment that permits the test platform listener.

### File List

- Directory.Build.props
- Hexalith.Conversations.slnx
- docs/release-evidence/promote-adopt-runbook.md
- src/Hexalith.Conversations.ServiceDefaults/ConversationsServiceDefaults.cs
- src/Hexalith.Conversations.ServiceDefaults/Hexalith.Conversations.ServiceDefaults.csproj
- tests/Hexalith.Conversations.ServiceDefaults.Tests/Hexalith.Conversations.ServiceDefaults.Tests.csproj
- tests/Hexalith.Conversations.ServiceDefaults.Tests/ConversationsServiceDefaultsTest.cs
- tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs
- tests/Hexalith.Conversations.Server.Tests/Diagnostics/TelemetryTestHelpers.cs (review fix: meter-instance-scoped listener ownership)
- tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationConformanceTelemetryTest.cs (review fix: scope MeterListener to own factory)
- tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationTelemetryGuardsTest.cs (review fix: scope MeterListener to own factory)
- _bmad-output/implementation-artifacts/3-4-promote-adopt-the-shared-servicedefaults-base.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- Hexalith.Commons (gitlink 17820f8 -> c76c1fe; dev commit 6adbf2b superseded by review fix c76c1fe)
- Hexalith.Commons/Directory.Packages.props
- Hexalith.Commons/src/libraries/Hexalith.Commons.ServiceDefaults/Directory.Build.props
- Hexalith.Commons/src/libraries/Hexalith.Commons.ServiceDefaults/Hexalith.Commons.ServiceDefaults.csproj
- Hexalith.Commons/src/libraries/Hexalith.Commons.ServiceDefaults/HexalithServiceDefaults.cs
- Hexalith.Commons/src/libraries/Hexalith.Commons.ServiceDefaults/HexalithServiceDefaultsOptions.cs
- Hexalith.Commons/test/Hexalith.Commons.ServiceDefaults.Tests/Hexalith.Commons.ServiceDefaults.Tests.csproj
- Hexalith.Commons/test/Hexalith.Commons.ServiceDefaults.Tests/HexalithServiceDefaultsTest.cs
- Hexalith.EventStore (gitlink 6be8c5d -> 667db888)
- Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs
- Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj

### Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-06-08 | 0.1 | Promoted shared ServiceDefaults base to Commons, delegated EventStore ServiceDefaults to Commons, added Conversations thin wrapper and host-composition guardrails, committed Commons/EventStore submodule changes, and left story in-progress because VSTest execution is blocked by sandbox socket permissions. | GPT-5 Codex |
| 2026-06-08 | 0.2 | Senior review: executed all blocked release gates (Commons 14/14, Conversations ServiceDefaults 7/7, Server 589/589, Conformance 361/361, Release build 0/0). Fixed a flaky telemetry test (process-global `MeterListener` cross-talk) by scoping listeners to their own `FakeMeterFactory` meter instances; fixed a per-request options allocation in the shared trace filter; superseded the red Commons commit 6adbf2b with c76c1fe (corrected hook-order test now passes) and re-bumped the root gitlink. Status -> done. | Senior Review (AI) |

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot (AI-assisted adversarial review) · **Date:** 2026-06-08 · **Outcome:** Approve (auto-fix applied)

### Scope

Reviewed the promoted `Hexalith.Commons.ServiceDefaults` base, the EventStore delegation facade, the Conversations thin wrapper, the new/changed tests, build mechanics, and submodule pointers against AC-1..AC-7. Crucially, the dev agent could not run any tests in its sandbox (`SocketException (13): Permission denied`), so every release gate was unverified. The test platform socket works in this environment, so all gates were executed.

### Acceptance criteria verdict

- **AC-1 (promote base):** IMPLEMENTED — `Hexalith.Commons.ServiceDefaults` is additive, self-contained build props, domain-neutral, exposes OpenTelemetry/health/discovery/resilience with module hooks.
- **AC-2 (preserve runtime path):** IMPLEMENTED — `AddEventStoreDomainService` still drives ServiceDefaults exactly once; host-composition test `AddEventStoreDomainServiceShouldRegisterServiceDefaultsSideEffects` proves a single `self` check + discovery/resilience/OpenTelemetry. `/health`,`/alive`,`/ready` still resolve.
- **AC-3 (real Conversations wrapper):** IMPLEMENTED — `ConversationsServiceDefaults` adds Conversations-owned service/meter naming over the base; FR-17 delete correctly N/A.
- **AC-4 (health endpoint/dev-JSON behavior):** IMPLEMENTED — status-code map (Healthy/Degraded→200, Unhealthy→503), dev JSON writer for `/health` and `/ready` only, health-probe trace exclusion all preserved and pinned by tests.
- **AC-5 (observability continuity):** IMPLEMENTED — JSON console logging, scopes/formatted messages, ASP.NET Core/HTTP/runtime instrumentation, OTLP env-gated export, health-probe trace filter preserved; covered by Commons tests.
- **AC-6 (sibling variance):** IMPLEMENTED — base supports service/resource name, activity sources, meters, endpoint paths, liveness/readiness tags, default self-check readiness toggle, additional health checks, logging/metrics/tracing/dev-writer hooks; no Conversations/EventStore/Dapr/Redis/FalkorDB coupling.
- **AC-7 (release gates):** NOW SATISFIED after the fixes below.

### Findings and resolution (all auto-fixed)

1. **[CRITICAL → FIXED] Committed Commons gitlink pointed at a red test.** Dev commit `6adbf2b` (the gitlink target) contained `AddHexalithServiceDefaultsShouldExecuteModuleHooksAfterSharedRegistration` asserting `["logging","metrics","tracing","health"]`. The OpenTelemetry logging hook runs *lazily*, so that assertion fails against the committed code; the corrected assertion existed only as an *uncommitted* working-tree edit. AC-7 ("promoted helper module tests pass" + "committed as a separate submodule commit plus a root gitlink bump") was therefore not actually met. Resolved by committing the fix as Commons `c76c1fe` and re-bumping the root gitlink.
2. **[CRITICAL → FIXED] Flaky Server test suite (gate-blocking).** `ConversationConformanceTelemetryTest` and `ConversationTelemetryGuardsTest` each start a process-global `MeterListener` filtered only by instrument *name* (`conversations.conformance.outcomes`). Running as separate xUnit collections they execute in parallel and capture each other's measurements (`captured.Count should be 1 but was 2`; observed ~20-25% failure rate). Resolved by adding `FakeMeterFactory.Owns(Meter)` and scoping each listener to instruments created by its own factory. Verified deterministic across 15 consecutive full-suite runs.
3. **[MEDIUM → FIXED] Per-request allocation on the tracing hot path.** The shared AspNetCore trace filter rebuilt and re-validated a `HexalithServiceDefaultsOptions` on every request via `ShouldTraceHttpRequest(context, _ => CopyEndpointOptions(...))`. Resolved by capturing the already-built `options` directly and adding a private `ShouldTraceHttpRequest(HttpContext, HexalithServiceDefaultsOptions)` overload; the now-dead `CopyEndpointOptions` was removed. Public API unchanged.
4. **[MEDIUM → FIXED] File List drift.** The new `tests/Hexalith.Conversations.ServiceDefaults.Tests/*` project was absent from the File List; added, along with the review-touched telemetry test files.

### Gate evidence (this environment)

- `Hexalith.Conversations.slnx` Release build: **0 warnings / 0 errors**.
- Commons.ServiceDefaults.Tests: **14/14**. Conversations.ServiceDefaults.Tests: **7/7**.
- Conversations.Server.Tests: **589/589** (deterministic, 15/15 re-runs after fix #2).
- Conversations.Conformance.Tests: **361/361** — monotonic gate `>= 361` met.
- Public-contract-shape: Contracts assembly untouched; conformance contract-shape guards green.
- Submodule pointers: Commons `c76c1fe`, EventStore `667db888`; root gitlinks bumped in the umbrella working tree (no nested submodule init).

### Notes / non-blocking

- EventStore `Hexalith.EventStore.ServiceDefaults.csproj` references Commons.ServiceDefaults only under `Condition="'$(HexalithCommonsRoot)' != ''"` with no fallback; standalone EventStore CI relies on its own `Directory.Build.props` resolving `HexalithCommonsRoot` from its Commons submodule (or, eventually, the published package). Green within this umbrella; flagged for the EventStore repo's standalone pipeline.
