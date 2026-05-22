# Story 3.7: Provide Self-Serve Buyer Acceptance Demo

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a buyer evaluator,
I want a seeded acceptance demo for governed conversation evidence,
so that I can validate the module's trust story without requiring production data.

## Acceptance Criteria

1. Seeded acceptance scenario is deterministic and tenant-safe
   - Given seeded demo data is loaded for an authorized demo tenant,
   - When the buyer opens or runs the acceptance scenario,
   - Then the demo exercises find, read, redaction, audit trail, time-travel, citation copy, projection freshness, verification output, blocked command metadata, and cross-tenant denial,
   - And seeded records are clearly identified as demo data without weakening tenant isolation or requiring production data.

2. Canonical trust-state fixtures cover the buyer walkthrough
   - Given the demo includes redacted, stale, missing citation, unresolved participant, blocked command, verification pass/fail, and cross-tenant poison fixtures,
   - When the buyer follows the guided scenario,
   - Then each state displays or returns safe trust posture, evidence completeness, and next safe action,
   - And cross-tenant poison sentinel values never appear in any client-observable surface, DTO, safe label, route-ready value, copied citation text, accessibility-label-ready field, diagnostics text, or verification summary.

3. Acceptance evidence summary is content-safe and scoped
   - Given the demo is used for acceptance evidence,
   - When the scenario completes,
   - Then the system can produce or link to a content-safe evidence summary showing pass/fail status, scenario scope, timestamp, runner or signer, selected verification output, and requirement mappings,
   - And the summary distinguishes module-level Conversations evidence from inherited platform controls such as EventStore, Tenants, Parties, FrontComposer, Dapr, and Aspire.

4. Demo execution remains read-oriented and does not create a new authority
   - Given the demo uses existing projections, citation, temporal, audit, command-gate, and verification services,
   - When seeded data is prepared or the scenario is executed,
   - Then it does not append conversation events, mutate aggregate state, create transcript tables, write production projection state, persist copied citations, create export artifacts, or bypass existing tenant and freshness gates,
   - And any optional HTTP or host surface remains protected by trusted authentication/authorization context and uses content-safe hidden/unavailable response shapes.

5. Tests prove repeatability, safe fixture handling, and acceptance readiness
   - Given demo tests run,
   - When seeded data setup, guided flow, find/read, citation copy, temporal reconstruction, redaction attribution, audit detail, tenant denial, stale projection, blocked command, verification summary, and cross-tenant poison scenarios are exercised,
   - Then tests prove repeatable demo behavior, no production dependency, safe fixture handling, module-vs-inherited evidence separation, no unsafe field disclosure, and readiness for buyer acceptance.

## Tasks / Subtasks

- [x] Define contract-first demo scenario and evidence summary DTOs (AC: 1, 2, 3, 5)
  - [x] Add additive public contracts only if needed under `src/Hexalith.Conversations.Contracts`, for example `BuyerAcceptanceDemoScenarioV1`, `BuyerAcceptanceDemoStepV1`, `BuyerAcceptanceDemoFixtureV1`, `BuyerAcceptanceEvidenceSummaryV1`, and closed vocabularies for step kind, fixture kind, expected trust state, and evidence classification.
  - [x] Keep DTOs serialization-friendly records using `SchemaVersion.Current`, `TenantId`, optional `ConversationId`, scenario id, step id, requirement mappings, generated timestamp, correlation id, safe label, safe next action, and evidence handles.
  - [x] Use bounded vocabularies and existing converter patterns in `ClosedVocabularyJsonConverters.cs` if new closed-vocabulary types are introduced. Do not introduce open polymorphic JSON hierarchies or free-form result classifications.
  - [x] Validate every safe text field with existing contract-validation style so it cannot carry redacted content, raw message text, Party personal data, provider payloads, raw EventStore topology, hidden tenant/conversation identifiers, stack traces, local paths, browser-selected values, or unbounded business references.
  - [x] Include explicit evidence ownership in the summary: `module`, `inherited-platform-control`, `not-applicable`, `waived`, or equivalent closed vocabulary. Do not over-claim inherited platform behavior as Conversations proof.

- [x] Build deterministic seeded demo fixtures from existing projection and event models (AC: 1, 2, 4, 5)
  - [x] Prefer `src/Hexalith.Conversations.Testing` for reusable fixture builders and `tests/fixtures` only if static fixture files are necessary. Keep all data synthetic and named as demo data.
  - [x] Reuse `ConversationProjectionMaterializer`, `ConversationProjectedReadModels`, `ConversationDetailProjectionV1`, `ConversationSummaryProjectionV1`, `ProjectionFreshnessV1`, `ConversationEvidenceEntryV1`, `ConversationRedactionAttributionV1`, `ConversationCommandAvailabilityV1`, and governance verification contracts instead of creating a parallel demo transcript model.
  - [x] Provide at minimum these canonical fixtures: authorized full-trust conversation, redacted message with audit evidence, stale projection, missing citation or incomplete audit evidence, unresolved participant hydration, blocked governance command metadata, verification pass, verification governance failure or infrastructure failure, and cross-tenant poison with unique sentinel values.
  - [x] Ensure the cross-tenant poison fixture includes unique forbidden strings that tests can scan for and that the fixture never reaches visible/safe output for the authorized demo tenant.
  - [x] Seed read-side data through an in-memory `IConversationProjectionReadStore` or test/demo-only service registration. Do not write production projection stores, EventStore streams, idempotency records, audit records, or persistent export files.

- [x] Provide a self-serve scenario runner over current server boundaries (AC: 1, 3, 4)
  - [x] Add a focused service such as `ConversationBuyerAcceptanceDemoService` only if it helps compose existing query, citation, temporal, audit-record, and verification services into a repeatable scenario.
  - [x] The runner should execute the scenario as a sequence of existing operations: list by business reference, read detail, resolve redaction/audit detail, resolve citation, resolve temporal cursor, inspect command metadata, run or attach governance verification output, and attempt cross-tenant denial.
  - [x] Bind tenant and caller authority from the trusted server/test boundary. Route/query/body/demo manifest values may identify the demo scenario but must never supply tenant authority, caller authority, role, trust state, audit authority, command availability, or policy authority.
  - [x] If an HTTP endpoint is implemented, keep it separate from mutation APIs and protect it with `RequireAuthorization()` on a route group such as `/api/v1/conversations/demo/acceptance`. Do not place mutation semantics under `ConversationReadApi`.
  - [x] If no HTTP surface is necessary for the current repo shape, keep the service and tests self-serve through test/demo host composition; document the executable test or sample entry point in the evidence summary.

- [x] Generate content-safe acceptance evidence summary (AC: 3, 4, 5)
  - [x] Produce an in-memory result or contract DTO that names scenario id, demo tenant, synthetic-data marker, generated timestamp, runner/signer identifier where available, correlation id, step results, pass/fail status, requirement mappings, and selected verification output.
  - [x] Link or embed Story 3.6 `ConversationGovernanceVerificationRunResultV1` summaries only after sanitizing to the same content-safe vocabulary. Do not include raw replay events, raw projection details, stack traces, raw provider payloads, or protected target identifiers.
  - [x] Distinguish Conversations module evidence from inherited controls: EventStore persistence/replay authority, Tenants fail-closed access source of truth, Parties read-time hydration ownership, FrontComposer UI composition ownership, Dapr/Aspire hosting behavior, and any not-yet-implemented Admin UI surface.
  - [x] Keep full signed conformance artifact generation, release manifest signing, named waiver lifecycle, evidence bundle export, and buyer partial-acceptance record persistence out of this story unless an approved scope decision promotes them.
  - [x] Use stable, deterministic ordering for summary steps and requirement mappings so tests can snapshot or assert the output without brittle timestamps beyond the generated-at field.

- [x] Add optional API/demo-host wiring only if it fits the current repo shape (AC: 1, 3, 4, 5)
  - [x] Current solution has `Contracts`, `Client`, domain, `Server`, `ServiceDefaults`, `AppHost`, and `Testing`, but no `Hexalith.Conversations.Admin`, `Hexalith.Conversations.Conformance`, CLI, worker, or web UI project. Do not scaffold a full UI/conformance/CLI project solely for this story.
  - [x] If adding server registration, follow the existing extension pattern beside `ConversationGovernanceVerificationServiceCollectionExtensions.cs` or an equivalent focused registration location.
  - [x] If adding a route group, use trusted claims in the same style as `ConversationReadApi`: `tid` for tenant binding, `ClaimTypes.NameIdentifier` for caller, and `X-Correlation-Id` for correlation.
  - [x] Manually parse scenario ids or cursors where hidden/unavailable equivalence matters. Do not let framework model-binding errors disclose unsafe request details.
  - [x] Keep response bodies content-safe for malformed, unauthorized, nonexistent, cross-tenant, stale, rebuilding, unavailable, and unsupported-version paths.

- [x] Add focused contract, service, API/demo-host, and safety tests (AC: 1-5)
  - [x] Add contract tests for demo scenario and evidence summary DTO JSON shape, closed vocabularies, stable serialization, safe text validation, requirement mappings, module-vs-inherited evidence classification, and forbidden vocabulary.
  - [x] Add fixture-builder tests proving deterministic ids, synthetic-data markers, canonical fixture coverage, no duplicate scenario step ids, and no production-data dependency.
  - [x] Add service tests for full demo walkthrough, stale projection, missing citation/incomplete audit evidence, unresolved participant hydration, blocked command metadata, verification pass/fail classification, cross-tenant poison denial, and content-safe evidence summary output.
  - [x] If an HTTP endpoint or demo host is added, extend API tests for route-group authorization, trusted claim binding, malformed scenario id, unauthorized/cross-tenant denial, unavailable dependencies, and no mutation endpoint under the demo surface.
  - [x] Add a safety-net test proving demo code does not call aggregate mutation methods, governance command handlers, `IdempotentConversationCommandExecutor`, EventStore append APIs, `ConversationGovernanceAuditGate.RecordRequiredAsync()`, production projection writes, or export persistence.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` after implementation with Story 3.7 evidence.

- [x] Preserve scope boundaries and ADR stop conditions (AC: 1-5)
  - [x] Do not implement Story 3.8A responsive/mobile safe triage, Story 3.8B accessibility-tree/keyboard/screen-reader safety, or Story 3.8C leakage/clipboard/browser/telemetry disclosure safety beyond DTO/API/service leakage tests required here.
  - [x] Do not implement Epic 5 signed release conformance artifact, release manifest signing, named waiver lifecycle, adopter-facing conformance package, or full evidence bundle export.
  - [x] Do not implement a full Admin/FrontComposer shell, dashboards, legal-hold automation, cross-tenant global admin browsing, queue workers, durable demo store, transcript tables, secondary authoritative read stores, Memories/RAG indexes, browser storage, recent-demo history, or persistent copied-citation logs.
  - [x] Stop for ADR/waiver if implementation needs a new durable authority, production seeded data, raw EventStore cursor exposure beyond existing temporal contract cursors, a new privileged execution path, cross-tenant verification from one request, export lifecycle behavior, mobile governance-changing action, or UI-generated trust state.

## Dev Notes

### Epic and Business Context

- Epic 3 delivers the compliance investigation workspace: operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios. Story 3.7 is the buyer-facing acceptance slice after Story 3.6 added structured verification results. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Compliance Investigation Workspace`]
- Story 3.7 covers FR69: the product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.7: Provide Self-Serve Buyer Acceptance Demo`; `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`]
- The PRD carry-forward requirement is a 5-minute self-serve path with seeded data demonstrating one redaction, one time-travel view, one citation copy, and one cross-tenant denial. This is required so buyer GA acceptance is evidence-based, not trust-based. [Source: `_bmad-output/planning-artifacts/prd.md#carryForwardCallouts`]
- Julian's buyer-acceptance journey expects the buyer to inspect signed or structured evidence and distinguish complete controls from warnings or deferred commitments. This story should make the acceptance walkthrough concrete without implementing the full Epic 5 release artifact machinery. [Source: `_bmad-output/planning-artifacts/prd.md#Scenario Sketch - Julian, Platform Owner`]
- UX mapping for this story: UX-DR28 canonical fixtures, UX-DR37 safety AC set, UX-DR38 quality gates, and UX-DR52 canonical responsive fixtures. Story 3.7 should define and exercise canonical fixtures, while broad browser/responsive/accessibility/leak scanning remains in Story 3.8A-3.8C. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md`]

### Ready-for-Dev Preconditions

- Projection freshness blocking semantics are decided. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; only `Current` enables trust-bearing decisions unless a story records a narrower exception. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- Temporal evidence anchor is decided. v1 temporal links use EventStore event position plus projection version; timestamp is supporting metadata only. Demo temporal fixtures must use the same composite cursor contract. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Temporal evidence anchor`]
- Command availability metadata is server-owned. Demo blocked-command steps must render or return existing metadata and preserve mandatory fresh server recheck semantics; UI disabled state is not authorization. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Command availability metadata`]
- Retention/export scope remains narrow for v1. Full evidence bundle export, full retention editor, automatic legal hold, future derived indexes, and broad lifecycle automation remain out of scope without ADR/scope approval. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle`]
- Story 3.8 is already split into responsive/mobile, accessibility, and leakage/clipboard/browser/telemetry verification stories. Story 3.7 should define canonical fixtures that 3.8 can reuse, but it should not absorb the 3.8 evidence checklist. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Story 3.8 assignment plan`]

### Current Implementation State

- There is no `Hexalith.Conversations.Admin`, `Hexalith.Conversations.Conformance`, CLI, worker, or web UI project in the solution today. Current implementation scope is contracts, domain, server/API, projections, query services, governance services, `Testing`, and focused test projects. [Source: `Hexalith.Conversations.slnx`; `src/` directory]
- `ConversationReadApi` currently maps authorized GET routes under `/api/v1/conversations` for list, detail, citation, temporal, and audit-record reads. It binds tenant from the authenticated `tid` claim, caller from `ClaimTypes.NameIdentifier`, and correlation from `X-Correlation-Id`; malformed/hidden paths return content-safe hidden or unavailable shapes. [Source: `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`]
- `ConversationQueryHandler` is the tenant-safe read boundary over `ConversationProjectionReadService`, list filters, hydration, citation access, temporal reconstruction, audit-record access, and privileged justification review. Keep demo orchestration above this boundary rather than bypassing it. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`]
- `ConversationProjectionMaterializer` already builds summary/detail projections, trust posture, search trust previews, evidence entries, redaction attribution, citation availability, audit readiness, command availability defaults, and freshness metadata from ordered events. Demo fixtures should reuse this materializer or its projection models instead of constructing unrelated transcript data. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`]
- `ConversationCitationAccessService` and temporal routes already prove citation copy and temporal evidence link behavior with server-owned DTOs, strict cursor parsing, current-freshness gates, and permission-downgrade clearing. Demo citation/time-travel steps should consume those paths. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationCitationAccessService.cs`; `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`; Story 3.4]
- `ConversationGovernanceVerificationService` already produces structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and schema compatibility, while distinguishing governance failures from stale/dependency/data/execution failures. Demo evidence summary should reuse these contracts and classifications. [Source: `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs`; Story 3.6]
- `src/Hexalith.Conversations.Testing` currently contains basic deterministic id helpers and repository test context. This is the likely place for reusable demo fixture builders if they need to be shared by multiple test assemblies. [Source: `src/Hexalith.Conversations.Testing/Factories/ConversationTestIds.cs`; `src/Hexalith.Conversations.Testing/Fixtures/RepositoryTestContext.cs`]
- `Directory.Packages.props` currently pins Aspire `13.2.2`, Microsoft.Extensions/OpenTelemetry packages, and testing packages. It does not currently include `Microsoft.AspNetCore.Mvc.Testing`; if implementation adds WebApplicationFactory-style integration tests, add the version centrally, not inline in a `.csproj`. [Source: `Directory.Packages.props`]

### Previous Story Intelligence

- Story 3.6 selected a service-only verification execution surface because this repo has no approved CLI/worker/Admin shell pattern. Story 3.7 should follow the same discipline: service/tests are valid if no approved self-serve UI host exists. [Source: `_bmad-output/implementation-artifacts/3-6-run-governance-verification-and-return-structured-results.md#Completion Notes List`]
- Story 3.6 review tightened privileged verification justification and temporal source failure classification. Demo verification steps must not accept arbitrary audit evidence as privileged proof and must preserve stale-projection versus data-unavailable versus dependency-unavailable distinctions. [Source: `_bmad-output/implementation-artifacts/3-6-run-governance-verification-and-return-structured-results.md#Senior Developer Review (AI)`]
- Story 3.5 established that command metadata is advisory, server-owned, fail-closed, and always requires fresh server recheck before execution. Demo blocked-command steps should demonstrate this metadata without implementing command execution. [Source: `_bmad-output/implementation-artifacts/3-5-preserve-read-only-compliance-workflows-and-safe-command-gates.md`]
- Story 3.4 review fixed strict temporal projection-cursor validation, citation disclosure validation, unsafe citation target rejection, and future-position citation cursor handling. Demo temporal and citation fixtures must include regression coverage for malformed/mismatched cursors and redacted citation targets. [Source: `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md#Senior Developer Review (AI)`]
- Recent commits are all Epic 3 read-workspace slices: search, governed read, redaction/audit details, citation/temporal links, command gates, and governance verification. Continue the pattern of contract-first DTOs, focused services, in-memory fakes, safety-net tests, and full-solution validation. [Source: `git log -5 --oneline`]

### Architecture Guardrails

- EventStore remains authoritative for writes. Demo setup must not become a second write path, transcript store, production seed store, or persistent evidence authority. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Projections, admin UI state, caches, exports, verification snapshots, and evidence summaries are derived or presentation state. If this story creates evidence summary DTOs, they must be rebuildable/reproducible and not authoritative conversation records. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Tenant isolation and redaction apply to every observable surface: HTTP status/body, search counts, URLs, route labels, browser-title-ready labels, hidden/accessibility text, clipboard payloads, logs, traces, diagnostics, screenshots, and release evidence. Story 3.7 must test DTO/API/service outputs now and leave full browser/accessibility scanning to Story 3.8. [Source: `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`]
- UX must treat trust, freshness, redaction, tenant isolation, and provenance as governed domain outputs. Demo steps must consume Conversations-owned projections and command metadata; clients must not infer trust from fixture names or UI state. [Source: `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`]
- FrontComposer-generated screens are acceptable only for baseline administration. Evidence review, temporal navigation, trust posture, redaction, audit, citation, and disclosure surfaces require custom-reviewed components. Because no Admin project exists, do not scaffold one solely for this story. [Source: `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`]
- Every feature story should identify command/query DTOs, validators, handlers/services, projection/read-model impact, tenant access rule, audit/disclosure impact, and required tests. This story is primarily read/demo composition; any mutation-like behavior is a stop condition. [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`]

### File Structure Requirements

Likely files to add or update:

- ADD `src/Hexalith.Conversations.Contracts/Queries/BuyerAcceptanceDemo*.cs` or `src/Hexalith.Conversations.Contracts/Governance/BuyerAcceptance*.cs` only if public demo scenario/evidence summary contracts are needed.
- UPDATE `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` only if new closed vocabularies need JSON converters.
- ADD `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemo*.cs` or `src/Hexalith.Conversations.Testing/Factories/BuyerAcceptanceDemo*.cs` for deterministic synthetic fixture builders.
- ADD `src/Hexalith.Conversations.Server/Queries/ConversationBuyerAcceptanceDemoService.cs` or `src/Hexalith.Conversations.Server/Governance/ConversationBuyerAcceptanceDemoService.cs` if scenario composition needs a server service.
- ADD or UPDATE server service collection extension files only if implementation needs DI registration for demo services.
- ADD `src/Hexalith.Conversations.Server/Api/ConversationBuyerAcceptanceDemoApi.cs` only if an HTTP self-serve surface is implemented; keep it protected and read-oriented.
- UPDATE `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs` only if reusing the existing read route group is safer than a separate demo route. Preserve GET-only read semantics and hidden/unavailable response shapes.
- ADD tests under `tests/Hexalith.Conversations.Contracts.Tests` for demo contracts and closed vocabularies.
- ADD tests under `tests/Hexalith.Conversations.Server.Tests` for demo service/API behavior and safety nets.
- UPDATE `_bmad-output/implementation-artifacts/tests/test-summary.md` after implementation.

Files that should usually not be touched for this story:

- Do not edit aggregate mutation handlers or governance command handlers except to add safety-net references if a test inventory needs them.
- Do not edit sibling modules such as EventStore, Tenants, Parties, FrontComposer, Folders, Memories, or Commons.
- Do not create `Hexalith.Conversations.Admin`, `Hexalith.Conversations.Conformance`, CLI, worker, export, or browser-test projects unless an approved scope decision exists.

### Testing Requirements

- Run focused contract/demo DTO tests:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
- Run focused server demo/query/verification tests:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationGovernanceVerificationServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest"`
- Run citation, temporal, audit, and command-gate regressions if the demo touches those DTOs or services:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"`
- Run projection/read hydration regressions if fixture builders materialize projections:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`

### Latest Technical Information

- ASP.NET Core route groups support a shared route prefix plus common metadata such as `RequireAuthorization()`. If this story adds a demo endpoint, protect the whole demo group instead of duplicating authorization on every handler. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`]
- Minimal API route groups and endpoint filters apply group metadata/filters to grouped endpoints, but handler code must still bind tenant/caller authority from authenticated server context. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0#routing`]
- ASP.NET Core integration tests can use `WebApplicationFactory`, `TestServer`, mock authentication handlers, and `ConfigureTestServices` to replace services. If Story 3.7 adds API integration tests, use test-only service overrides for seeded projections and test auth rather than production data. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/test-min-api?view=aspnetcore-10.0#aspnet-core-integration-tests`; `https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0#mock-authentication`]
- `System.Text.Json` supports records and immutable types, and source generation can improve startup/trimming/performance. Keep demo DTOs simple records with existing converter patterns; do not introduce Newtonsoft.Json or open polymorphism for scenario steps. [Source: `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft#scenarios-using-jsonserializer`; `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/reflection-vs-source-generation#source-generation`]
- `dotnet test --filter` supports `FullyQualifiedName~...`, boolean `|` and `&`, and xUnit traits. Use targeted filters first, then full solution validation. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]

### Out of Scope

- Do not implement full Admin/FrontComposer UI, dashboards, browser walkthroughs, responsive/mobile layout verification, accessibility-tree verification, screenshot checks, telemetry leak scans, or full Leak Sentinel automation. Those belong to Story 3.8A-3.8C.
- Do not implement Epic 5 signed conformance artifacts, release manifest signing, named waiver workflow, public adopter conformance package, compatibility manifest, or release-gate signature infrastructure.
- Do not implement full evidence bundle export, audit export, legal-hold automation, retention editor, cross-tenant global browsing, background workers, long-running queued verification, or persistent acceptance records.
- Do not create transcript tables, alternate read stores, durable demo stores, Memories/RAG indexes, browser/local storage, recent-demo history, or persistent copied citation logs.
- Do not mutate conversation aggregate state from demo setup, demo execution, citation copy, temporal-link resolution, audit detail review, verification summary generation, or cross-tenant denial checks.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.7: Provide Self-Serve Buyer Acceptance Demo`
- `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`
- `_bmad-output/planning-artifacts/prd.md#Scenario Sketch - Julian, Platform Owner`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/planning-artifacts/prd.md#Accessibility And Human Trust`
- `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`
- `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`
- `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Quality Gates`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Design & Accessibility`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md`
- `_bmad-output/implementation-artifacts/3-5-preserve-read-only-compliance-workflows-and-safe-command-gates.md`
- `_bmad-output/implementation-artifacts/3-6-run-governance-verification-and-return-structured-results.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `Hexalith.EventStore/_bmad-output/project-context.md`
- `Hexalith.Tenants/_bmad-output/project-context.md`
- `Hexalith.Parties/_bmad-output/project-context.md`
- `Hexalith.FrontComposer/_bmad-output/project-context.md`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
- `src/Hexalith.Conversations.Contracts/Governance/ConversationGovernanceVerificationContracts.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationCitationAccessService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs`
- `src/Hexalith.Conversations.Testing/Factories/ConversationTestIds.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationGovernanceVerificationServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`
- `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/test-min-api?view=aspnetcore-10.0#aspnet-core-integration-tests`
- `https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0#mock-authentication`
- `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/reflection-vs-source-generation#source-generation`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - red phase failed before demo contracts existed, then passed after contract implementation and review fixes; 18 passed.
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - red phase failed before fixture builder existed, then passed after deterministic fixture implementation; 2 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - red phase failed before runner service existed, then passed after service implementation, temporal replay fixture wiring, QA gap fixes, and review fixes; 8 passed.
- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 78 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationGovernanceVerificationServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` - 121 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 52 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 145 passed.
- `dotnet test Hexalith.Conversations.slnx` - 774 passed.

### Completion Notes List

- Added additive buyer acceptance demo contracts with closed vocabularies for step kind, fixture kind, expected trust state, execution status, and evidence ownership.
- Added content-safe evidence summary DTOs with deterministic step ordering, selected sanitized governance verification output, requirement mappings, runner/correlation metadata, and module-vs-inherited evidence scope.
- Added deterministic synthetic fixture builders in `Hexalith.Conversations.Testing` covering full-trust, redacted, stale, missing citation/incomplete audit, unresolved participant, blocked command metadata, verification pass/fail, and cross-tenant poison sentinel cases.
- Added a read-only `ConversationBuyerAcceptanceDemoService` and DI registration that composes current query/projection boundaries and attached verification outputs without adding an HTTP endpoint, durable store, export artifact, or mutation authority.
- Added contract, fixture, service, DI, content-safety, temporal reconstruction, missing/out-of-scope verification and cross-tenant probe partial-outcome, cross-tenant poison, and mutation-boundary safety tests. No Story 3.8 UI/browser/accessibility scope or Epic 5 conformance/export scope was implemented.
- Review auto-fixes now fail closed when caller authority is missing, require temporal demo steps to carry a canonical composite temporal cursor, and reject scenario steps that reference undeclared fixture kinds.

### File List

- `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Governance/BuyerAcceptanceDemoContracts.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationBuyerAcceptanceDemoService.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationBuyerAcceptanceDemoServiceCollectionExtensions.cs`
- `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemoFixtures.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/BuyerAcceptanceDemoContractTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationBuyerAcceptanceDemoServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj`
- `tests/Hexalith.Conversations.Tests/Testing/BuyerAcceptanceDemoFixtureTest.cs`

### Senior Developer Review (AI)

- Review result: approved after auto-fixes.
- Findings fixed:
  - Verification pass/fail evidence was accepted without checking that the verification run belonged to the scenario tenant/conversation scope. `ConversationBuyerAcceptanceDemoService` now filters attached verification output to the scenario scope before step evaluation and summary projection.
  - Cross-tenant denial could be satisfied by a same-tenant hidden/missing read. The runner now requires the probe tenant to differ from the trusted tenant before treating a forbidden projection read as cross-tenant denial evidence.
  - Missing caller authority was replaced with a synthetic `hidden-caller`, allowing a permissive test or host tenant-access adapter to treat the demo as authorized. The runner now fails every step closed without a trusted caller, suppresses attached verification summaries, and avoids tenant/projection reads in that state.
  - Temporal demo steps could not carry the canonical composite temporal cursor because the DTO validator rejected the `projection` segment required by the existing temporal contract. The demo contract now accepts only the bounded composite `temporal:v1:pos:{position}:projection:{version}` shape, fixtures include it, and the runner uses it.
  - Scenario construction allowed steps to reference fixture kinds not declared by the scenario manifest. `BuyerAcceptanceDemoScenarioV1` now rejects undeclared step fixture kinds.
- Tests added:
  - `DemoRunnerShouldIgnoreVerificationEvidenceOutsideScenarioScope`
  - `DemoRunnerShouldNotTreatSameTenantHiddenReadAsCrossTenantDenial`
  - `DemoRunnerShouldFailClosedWhenCallerAuthorityIsMissing`
  - `DemoStepShouldAcceptCompositeTemporalCursor`
  - `DemoStepShouldRejectMalformedTemporalCursor`
  - `DemoScenarioShouldRejectStepsForUndeclaredFixtureKinds`
- Validation:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 18 passed.
  - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 2 passed.
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 8 passed.
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 78 passed.
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationGovernanceVerificationServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` - 121 passed.
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 52 passed.
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 145 passed.
  - `dotnet test Hexalith.Conversations.slnx` - 774 passed.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 3, Story 3.7, and Stories 3.1-3.6 continuity.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR69, Julian buyer acceptance journey, Sarah operator workflow, seeded demo carry-forward, tenant isolation, redaction non-disclosure, evidence summaries, and accessibility/safety requirements.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on EventStore authority, derived evidence, FrontComposer boundaries, disclosure surfaces, UX trust contract, implementation guardrails, and testing/release evidence boundaries.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`, focusing on canonical fixtures, safety ACs, quality gates, responsive/accessibility follow-on ownership, and Leak Sentinel boundaries.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md` and sibling module contexts for EventStore, Tenants, Parties, FrontComposer, Projects, Folders, Commons, and Memories, focusing on tenant isolation, EventStore authority, Party personal-data ownership, FrontComposer generated-first UI boundaries, package management, and submodule policy.
  - Loaded previous Story 3.6, Story 3.5, Story 3.4, readiness gates, readiness decisions, recent git history, and current source/test files for read APIs, query handler, citation, temporal reconstruction, command availability, verification, projection materialization, and testing helpers.
  - Checked official Microsoft documentation for ASP.NET Core route groups, Minimal API route grouping/testing, WebApplicationFactory/test authentication, System.Text.Json records/source generation, and `dotnet test --filter`.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to existing read, citation, temporal, audit, command-gate, verification, projection, and testing boundaries instead of a new transcript model, production seed store, UI shell, export system, or conformance authority.
  - Added explicit guardrails for deterministic synthetic fixtures, cross-tenant poison sentinel scanning, content-safe evidence summaries, module-vs-inherited evidence classification, current-freshness reliance, and no mutation/new authority from demo execution.
  - Added likely file touch list, focused test commands, latest technical references, previous-story lessons, and ADR stop conditions.
  - Kept Story 3.8 browser/responsive/accessibility/leak-sentinel evidence, Epic 5 signed release artifacts, full Admin UI, exports, workers, and durable acceptance records out of Story 3.7.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture/UX guardrails, prior-story intelligence, test requirements, latest technical references, and explicit out-of-scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Change Log

- 2026-05-22: Created Story 3.7 context from Epic 3 requirements, PRD/architecture/UX/readiness/project context, previous Story 3.4-3.6 learnings, current read/citation/temporal/verification implementation, recent git history, and official Microsoft documentation.
- 2026-05-22: Implemented contract-first buyer acceptance demo DTOs, deterministic synthetic fixture builder, read-only scenario runner, content-safe evidence summary, safety tests, and validation evidence; moved story to review.
- 2026-05-22: Completed senior review auto-fixes for scoped verification evidence and true cross-tenant denial proof; moved story to done.
- 2026-05-22: Completed story-automator review auto-fixes for missing caller fail-closed behavior, canonical composite temporal cursor handling, and scenario fixture integrity; kept story done.
