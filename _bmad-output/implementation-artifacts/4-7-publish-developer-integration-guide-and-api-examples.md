# Story 4.7: Publish Developer Integration Guide and API Examples

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want concise integration guidance and examples,
so that I can use Conversations correctly without reverse-engineering architecture decisions.

## Acceptance Criteria

1. The integration guide explains Conversations responsibility boundaries and documents CORE behavior
   - Given developer documentation is published,
   - When an adopter reads the integration guide,
   - Then it explains Conversations responsibilities versus chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities,
   - And it documents tenant binding, Party identity, idempotency, typed errors, projection freshness, event publication, governance behavior, compatibility discovery, and CORE preconditions.

2. Examples cover the supported integration workflow at the .NET client / contract level
   - Given examples are provided,
   - When an adopter follows them,
   - Then examples cover .NET client setup, create conversation, append message, read timeline, handle typed errors, retry idempotently, inspect freshness, discover compatibility, and run conformance tests,
   - And examples avoid raw EventStore mechanics and unsafe provider-session identity assumptions.

3. Failure-mode documentation stays content-safe and never encourages bypassing fail-closed gates
   - Given documentation references operational or governance behavior,
   - When guidance describes failure modes,
   - Then it explains content-safe responses, audit handles where allowed, degraded reads, stale projections, unsupported schemas, and remediation paths,
   - And it does not expose sensitive policy internals or suggest bypassing fail-closed gates.

4. Documentation checks keep the docs aligned with package and client contracts; stale/unsafe examples fail validation
   - Given documentation checks run,
   - When links, examples, contract names, error codes, version metadata, and conformance commands are validated,
   - Then docs remain aligned with the package and client contracts,
   - And stale or unsafe examples fail validation.

## Tasks / Subtasks

- [x] Confirm scope, readiness gates, evidence policy, and reuse the already-shipped Epic 4 deliverables before writing anything (AC: 1-4)
  - [x] Confirm no readiness gate blocks Story 4.7 (it appears in no `Blocks` column), but honor the `.NET client versus raw HTTP fallback policy` gate (`decided`): the supported v1 path is `Hexalith.Conversations.Client` + `Hexalith.Conversations.Contracts`. Do NOT author raw HTTP fallback examples; raw HTTP requires later buyer/diagnostics approval. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`]
  - [x] This is a DOCUMENTATION story — the deliverable is the integration guide, examples, and a docs-validation safety net. Do NOT add production source behavior, new public contracts, new error/freshness/outcome vocabulary, durable state, a globally-runnable host, or raw HTTP examples. Stop for ADR if any of those become tempting.
  - [x] Document only what already exists. Inventory the six predecessor deliverables and reference them by their real contract/type names and file paths (see "Predecessor Deliverables to Document" in Dev Notes). Do NOT invent APIs, document unimplemented behavior, or describe internal handlers/EventStore mechanics.
  - [x] Reuse the existing README content rather than duplicating or contradicting it. `README.md` and `src/Hexalith.Conversations.Contracts/README.md` already carry the canonical identity taxonomy, JSON wire shapes, the typed-error catalog table, freshness vocabulary, CORE preconditions/onboarding diagnostics table, the conformance check->requirement->precondition->release-gate table, and the caller-metadata table. The guide consolidates and links these; it must NOT fork a second, drifting copy of the error/precondition tables.

- [x] Write the developer integration guide (AC: 1, 3)
  - [x] Add the guide where the architecture maps developer docs: `docs/` (FR100-FR104 scope lifecycle -> `docs/adrs`, `docs/api`, `README.md`; FR70-FR80 developer experience -> `Contracts`, `Client`, `samples`). Prefer a single `docs/integration-guide.md` (or `docs/api/integration-guide.md`) plus a link from the root `README.md`. Justify the chosen location in Dev Notes. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/architecture.md#API Documentation`]
  - [x] Responsibility-boundary section (AC1): explain what Conversations OWNS (tenant-scoped conversation record, durable `ConversationId` identity, participant attribution via stable `PartyId`, business references, idempotent commands, versioned domain events, projection freshness/trust state, typed sanitized errors, compatibility discovery, CORE preconditions) versus what it does NOT own (chatbot/agent orchestration, LLM provider behavior and provider-session identity, legal-hold systems, attachment/file storage, identity provider, tenant lifecycle [`Hexalith.Tenants`], Party personal data [`Hexalith.Parties`], project/folder/file lifecycle, and upstream business-record lifecycle). Use the boundary-contract table material. [Source: `_bmad-output/planning-artifacts/architecture.md#Boundary Contracts For External Dependencies`; `README.md` Contract Package Guidance]
  - [x] CORE behavior section (AC1): document tenant binding (fail-closed, claims-derived + local Tenants projection, never JWT alone), Party identity (durable `PartyId`, read-time hydration only, no persisted personal data), idempotency (idempotency key required; stable duplicate outcomes; `idempotency_conflict`/`idempotency_outcome_unknown`), typed errors (link the catalog), projection freshness (`Current` is the only trust-bearing state), event publication (safe transport metadata; correlation/causation), governance behavior (audit pairing, redaction non-disclosure, retention — described as behavior, not policy internals), compatibility discovery (`ConversationContractCompatibility`), and CORE preconditions (`ConversationCorePreconditionCatalog`). Cite each to the shipped type/contract.
  - [x] Failure-mode section (AC3): explain content-safe responses (closed `code`/`category`/`clientAction`), `auditHandle` where allowed, degraded reads, stale/`tenant_projection_stale`, unsupported schemas/`schema_version_unsupported`, and the remediation pointers — all sourced from the shipped error catalog and onboarding diagnostics. Explicitly state that fail-closed gates (tenant isolation, audit pairing, freshness) must not be bypassed and that cross-tenant denials collapse to a hidden `aggregate_not_found` shape that does not reveal existence. Do NOT document policy internals, EventStore mechanics, internal handler/dispatcher/repository names, stream/snapshot/envelope topology, storage keys, provider payloads, secrets, or production exception text.
  - [x] Keep the guide content-safe by the same rules the contract tests enforce: no tenant IDs, Party IDs, conversation IDs/existence, provider session/payload values, business-reference values, redacted text, raw exception text, `C:\`/`D:\` paths, or infrastructure substrate terms in free text. Closed-vocabulary machine identifiers (e.g. `projection-freshness`, `error-envelope`) are safe (Story 4.4/4.5 lesson). [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs` `EnsureContentSafe`]

- [x] Provide the worked .NET client / contract examples (AC: 2)
  - [x] Decide and justify the example delivery surface in Dev Notes: either (a) fenced C# snippets embedded in the guide that mirror the existing `README.md` command-shape example and the `Hexalith.Conversations.Client` surface, or (b) a compilable `samples/Hexalith.Conversations.Sample/` project (architecture reserves `samples/Hexalith.Conversations.Sample/`). Prefer compilable/validated examples where feasible so AC4 can mechanically prove they stay aligned; if snippets only, they MUST be covered by the AC4 docs-validation net. [Source: `_bmad-output/planning-artifacts/architecture.md#Source Tree` (`samples/`); `README.md` example command shape]
  - [x] Examples must cover, end to end: .NET client setup/registration (`ConversationClientServiceCollectionExtensions`, `ConversationClientOptions`, `ConversationClientContext`), create conversation (`CreateConversationCommand` + `ConversationCreatedResult`), append message (`AppendMessageCommand`), read timeline (the read query/projection result), handle typed errors (branch on `ConversationError.Code`/`Category`/`ClientAction`), retry idempotently (reuse the same idempotency metadata; show `idempotency_conflict` vs `idempotency_outcome_unknown` handling), inspect freshness (`ProjectionTrustState`/`ProjectionFreshnessV1`, only `Current` is trust-bearing), discover compatibility (`ConversationContractCompatibility.Current`/`Evaluate(...)`), and run conformance tests (the `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...` command and how to read `ConformanceRunResultV1`). [Source: `src/Hexalith.Conversations.Client/*`; `src/Hexalith.Conversations.Contracts/*`]
  - [x] Examples must avoid raw EventStore mechanics and must NOT treat provider session IDs as durable conversation identity (`ProviderCorrelationMetadata` is opaque correlation only; `ConversationId` + `TenantId` is identity). Use the canonical URN-style wire shapes and case-sensitive closed-vocabulary spellings from `README.md`. (AC2)
  - [x] If a sample project is added, register it appropriately (e.g. in `Hexalith.Conversations.slnx` or a samples solution per local convention), keep it under Central Package Management with no new dependencies unless proven necessary, and ensure it builds with the pinned SDK and warnings-as-errors. Do NOT make it a globally-runnable production host or require Aspire/Dapr/tenant seed/provider credentials/nested submodules to build. [Source: `Directory.Packages.props`; `Directory.Build.props`; `global.json`; `README.md` Local Validation/Submodules]

- [x] Add the documentation-validation safety net so stale or unsafe examples fail (AC: 4)
  - [x] Add automated documentation checks that validate: (1) referenced contract type names and `ConversationErrorCode` values exist in the contracts assembly; (2) the error-code/category/client-action/documentation table in the docs matches `ConversationErrorCatalog`; (3) version/compatibility metadata in the docs matches `ConversationContractCompatibility.Current`; (4) the conformance command and `ConformanceRunResultV1` references are accurate; (5) documentation pointers are HTTPS and well-formed; (6) example free text passes the same content-safety/forbidden-surface scan used by `ForbiddenPublicSurfaceTest`. Follow the existing contract-test patterns (`ContractMetadataTest`, `ConversationErrorCatalogTest`, `ForbiddenPublicSurfaceTest`, `ContractPackageInventoryTest`) rather than inventing a new framework. [Source: `tests/Hexalith.Conversations.Contracts.Tests/*`]
  - [x] Decide where the docs-validation test lives (likely `tests/Hexalith.Conversations.Contracts.Tests` since it asserts alignment with the published contract surface) and read the doc file(s) deterministically (no network). If a `samples` project is added and compiled by the build, treat successful compilation as part of the example-alignment evidence and still scan its text for forbidden fragments.
  - [x] If example snippets are embedded in markdown, parse the fenced code/identifiers and assert they reference real members; reject snippets that reference removed/renamed contracts or error codes (this is the mechanism that makes AC4 "stale examples fail validation" real).

- [x] Validate and record evidence (AC: 1-4)
  - [x] Run targeted contract/doc tests first:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Documentation|FullyQualifiedName~IntegrationGuide|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ErrorCatalog"`
  - [x] If a sample project was added, build it (and any samples solution): `dotnet build` the sample so compile errors prove example drift.
  - [x] Run the full solution before closing: `dotnet test Hexalith.Conversations.slnx`. Confirm `dotnet build Hexalith.Conversations.slnx` is 0 warnings (warnings-as-errors).
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 4.7 evidence (docs-validation coverage, example-alignment proof, content-safety scan of the guide/examples, full-solution counts).

- [x] Preserve scope boundaries and stop conditions (AC: 1-4)
  - [x] Do NOT implement Epic 5 work: contract compatibility/deprecation POLICY publication (Story 5.1), signed release conformance artifacts (5.2), versioned conformance manifest/traceability (5.3), named waivers (5.4), portability/schema-evolution release proofs (5.5-5.9), release-gate validation/aggregation (5.10), or module-vs-platform evidence separation (5.11). Story 4.7 documents existing CORE behavior; it does not author release policy or release-gate evidence. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5`]
  - [x] Do NOT implement Epic 6 work: operational telemetry dashboards, incident/CI status surfaces, Admin UI, or responsibility-boundary GOVERNANCE documentation beyond the developer integration guide's responsibility section (Story 6.7 owns the operator/buyer responsibility-boundary documentation; 4.7 owns the adopter-developer integration guide). [Source: `_bmad-output/planning-artifacts/epics.md#Epic 6`]
  - [x] Do NOT add a DocFX/API-reference generation pipeline as a new build dependency unless proven necessary and ADR-approved; the minimum deliverable is the README/markdown guidance plus validated examples per `#API Documentation`. [Source: `_bmad-output/planning-artifacts/architecture.md#API Documentation`]
  - [x] Do NOT initialize nested submodules or require `git submodule update --init --recursive`; all doc/example validation runs after root-level submodule init only. [Source: `CLAUDE.md`; `_bmad-output/project-context.md#Development Workflow Rules`]

## Dev Notes

### Epic and Business Context

- Epic 4 makes adopter integration credible through a contract package, a supported .NET client, compatibility discovery, typed sanitized errors, onboarding diagnostics, remediation guidance, CORE preconditions, adopter-facing conformance tests, and safe caller metadata. Story 4.7 is the FINAL story of Epic 4: it publishes the consolidated developer integration guide and worked API examples so adopters can use Conversations correctly without reverse-engineering architecture decisions. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`; `_bmad-output/planning-artifacts/epics.md#Story 4.7: Publish Developer Integration Guide and API Examples`]
- Story 4.7 covers FR74 (adopters can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior), FR78 (adopter-facing remediation guidance alongside machine-readable error codes), and FR79 (adopter-facing CORE preconditions). [Source: `_bmad-output/planning-artifacts/prd.md#FR74`; `_bmad-output/planning-artifacts/prd.md#FR78`; `_bmad-output/planning-artifacts/prd.md#FR79`]
- This is a documentation + docs-validation story. The behavior it documents is already implemented across Stories 4.1-4.6; the value is consolidating it into a single, accurate, content-safe adopter-facing guide and proving the examples stay aligned with the shipped contracts (AC4).

### Readiness Gate Context

- No readiness gate lists Story 4.7 in its `Blocks` column, so no gate directly blocks this story. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`]
- The `.NET client versus raw HTTP fallback policy` is `decided`: the supported v1 path is the .NET client plus the shared contract package; raw HTTP examples require later buyer or diagnostics approval. The guide and examples must therefore stay at the client/contract level and must NOT publish raw HTTP fallback examples. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`]
- The Shared Trust/Freshness Vocabulary Gate requires all surfaces (API, client, diagnostics, conformance, and docs) to use the one approved trust/freshness vocabulary. The guide must use exactly `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted` (only `Current` is trust-bearing) and the closed onboarding readiness language `ready`/`degraded`/`blocked`/`unknown`; do not invent doc-only synonyms. [Source: `_bmad-output/planning-artifacts/epics.md#Shared Trust/Freshness Vocabulary Gate`]

### Predecessor Deliverables to Document (point the dev agent at what already exists — do NOT re-implement)

The guide and examples must reference these concrete, already-shipped deliverables by their real names and paths. The dev agent should READ these to extract accurate signatures and behavior, then describe/exemplify them — never re-build them.

- **Story 4.1 — Published Contracts package + compatibility metadata:** `Hexalith.Conversations.Contracts` exposes commands, projections, domain events, typed errors, schema/version metadata, and compatibility status; it excludes server infrastructure. `ConversationContractCompatibility.Current` and `Evaluate(ContractCompatibilityRequest)` produce content-safe, machine-readable compatibility status with bounded HTTPS remediation. Document compatibility discovery and version metadata from here. [Source: `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`; `src/Hexalith.Conversations.Contracts/README.md#Compatibility Discovery`; `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`]
- **Story 4.2 — Supported .NET client happy path:** `Hexalith.Conversations.Client` (`IConversationClient`/`ConversationClient`, `ConversationClientOptions`, `ConversationClientContext`, `ConversationClientResult`, `ConversationClientServiceCollectionExtensions`) implements create/append/read with typed results, freshness metadata, typed errors, idempotent retry, and tenant-safe behavior — no EventStore mechanics and no provider-session-as-identity. The setup/create/append/read/retry/freshness examples must mirror this client surface. [Source: `src/Hexalith.Conversations.Client/*`; `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`]
- **Story 4.3 — Typed sanitized errors + remediation:** `ConversationErrorCatalog`, `ConversationError` (`Code`, `Category`, `IsRetryable`, `ClientAction`, `SafeMessage`, `CorrelationId`, optional `AuditHandle`/`Documentation`/`SafeFieldDiagnostics`), the closed `ConversationErrorCode`/`ConversationErrorCategory`/`ConversationErrorClientAction` vocabularies, and the free-text safety blocklist (`EnsureContentSafe`). The error-handling example and the failure-mode section must branch on these closed codes and reuse the catalog table that already exists in both READMEs — do not author a drifting copy. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; `src/Hexalith.Conversations.Contracts/README.md#Typed Errors`; `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`]
- **Story 4.4 — CORE preconditions + onboarding diagnostics:** `ConversationCorePreconditionCatalog.All` (precondition id, evaluating diagnostic check, required trust state, typed `unmetErrorCode`, safe-failure description) and `ConversationOnboardingDiagnosticsService` returning `OnboardingDiagnosticRunResultV1` with the closed `OnboardingDiagnosticCheck`/`OnboardingDiagnosticStatus` vocabularies (`ready`/`degraded`/`blocked`/`unknown`). The CORE-preconditions section and degraded/stale failure-mode guidance must source from here; a denied/cross-tenant request collapses to a single hidden `unknown` result. [Source: `src/Hexalith.Conversations.Contracts/Diagnostics/ConversationCorePreconditionCatalog.cs`; `src/Hexalith.Conversations.Server/Diagnostics/ConversationOnboardingDiagnosticsService.cs`; `src/Hexalith.Conversations.Contracts/README.md#CORE Preconditions and Onboarding Diagnostics`; `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`]
- **Story 4.5 — Conformance tests + CORE fixture:** the adopter-facing `tests/Hexalith.Conversations.Conformance.Tests` suite, the `Hexalith.Conversations.Contracts.Conformance` contracts (`ConformanceCheck`, `ConformanceOutcome`, `ConformanceFailureClassification`, `ConformanceCheckResultV1`, `ConformanceRunResultV1`), and the synthetic `ConversationConformanceCoreFixtures` in `Hexalith.Conversations.Testing`. The "run conformance tests" example must use the real command (`dotnet test tests/Hexalith.Conversations.Conformance.Tests/...`) and explain how to consume the deterministic `ConformanceRunResultV1`, and may reuse the conformance-check->requirement->precondition->release-gate table that already exists in the contracts README. [Source: `tests/Hexalith.Conversations.Conformance.Tests/*`; `src/Hexalith.Conversations.Contracts/Conformance/*`; `src/Hexalith.Conversations.Testing/Fixtures/ConversationConformanceCoreFixtures.cs`; `src/Hexalith.Conversations.Contracts/README.md#Adopter Conformance Tests and CORE Fixture`; `_bmad-output/implementation-artifacts/4-5-provide-adopter-facing-conformance-tests-and-core-fixture.md`]
- **Story 4.6 — Caller metadata:** `CallerMetadata` (approved fields `ClientName`/`ClientVersion`/`ComposerSource`/`Origin`/`IntegrationContext` + bounded opaque `ExtensionData`), attached additively to `CreateConversationCommand`/`AppendMessageCommand`/`UpdateConversationMetadataCommand`, validated/bounded at the command boundary, and provenance-only (never tenant/authorization/governance/trust truth; correlation/causation stay on the command/event envelope). The guide must describe caller metadata as provenance only and reuse the existing caller-metadata table. [Source: `src/Hexalith.Conversations.Contracts/Identifiers/CallerMetadata.cs`; `src/Hexalith.Conversations.Contracts/README.md#Caller Metadata (Provenance Only)`; `_bmad-output/implementation-artifacts/4-6-capture-caller-metadata-for-attribution-audit-and-composition.md`]

### Existing Documentation State (consolidate and link; do NOT fork a drifting copy)

- `README.md` (root) already documents: Contract Package Guidance, the identity taxonomy (`ConversationId`/`TenantId`/`PartyId`/etc.), the URN-style JSON wire shapes, `SchemaVersion` integer rules, case-sensitive closed vocabularies, the `ParticipantType` wire-vs-property table, a worked `CreateConversationCommand` example, the full typed-error catalog table, the freshness vocabulary, result shapes, and Local Validation / Submodules. The integration guide should link to and extend this, not contradict or duplicate it. [Source: `README.md`]
- `src/Hexalith.Conversations.Contracts/README.md` already documents: Compatibility Discovery, Supported v1 Integration Path, the typed-error table, the CORE preconditions/onboarding diagnostics table, the conformance-check->requirement->precondition->release-gate table, the caller-metadata table, and the Safe Surface boundary. This is the closest thing to an existing adopter reference; the new guide should consolidate the developer-journey narrative and examples and cross-link these tables as the canonical source. [Source: `src/Hexalith.Conversations.Contracts/README.md`]
- `docs/` already contains `docs/adrs/` (with `index.md`, `0001-idempotency-contract.md`), `docs/projection-read-models.md`, and `docs/conversation-publication-events.md`. The architecture reserves `docs/api` and `docs/release-evidence`; the integration guide fits naturally under `docs/` or `docs/api/`. No `samples/` directory exists yet (architecture reserves `samples/Hexalith.Conversations.Sample/`). [Source: `docs/*`; `_bmad-output/planning-artifacts/architecture.md#Source Tree`; `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Architecture and Documentation Guardrails

- `#API Documentation`: "Use OpenAPI for server contracts and README/API guidance for adopter workflows. Contract compatibility tests verify commands, projections, events, errors, and version discovery." The minimum deliverable is README/markdown adopter guidance plus validated examples — a DocFX/API-reference generation pipeline is NOT required and should not be added as a new build dependency without ADR approval. [Source: `_bmad-output/planning-artifacts/architecture.md#API Documentation`]
- `#API Pattern` / `#Architectural Boundaries`: public docs must expose Conversations commands, projections, result contracts, version metadata, typed errors, and freshness state only. Do NOT document EventStore envelopes, aggregate IDs as substrate concepts, snapshot mechanics, stream internals, SignalR groups, projection topology, internal handler/dispatcher/repository names, or storage keys. [Source: `_bmad-output/planning-artifacts/architecture.md#API Pattern`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- `#Boundary Contracts For External Dependencies`: the responsibility-boundary section should reflect the allowed/forbidden use of `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.FrontComposer`, and `Hexalith.Memories` — Conversations versus chatbot/LLM/legal-hold/attachment/identity/tenant/project/folder/upstream lifecycle. [Source: `_bmad-output/planning-artifacts/architecture.md#Boundary Contracts For External Dependencies`]
- Content safety is the same standard the contract tests enforce: free text must pass the `ConversationError.EnsureContentSafe` blocklist material; closed-vocabulary machine identifiers are safe (Story 4.4/4.5 lesson — do not over-scope the scan and collide with legitimate tokens like `error-envelope` or `projection-freshness`). [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`]
- Story Safety Rule / ADR triggers: a documentation story should not introduce durable state, a new public vocabulary, a new runtime gate semantic, a globally-runnable host, a new build/codegen dependency, or any degraded/fail-open behavior. If the example surface requires any of these, STOP for ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Story Safety Rule`; `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]

### File Structure Guidance

- Likely new doc/example files:
  - `docs/integration-guide.md` (or `docs/api/integration-guide.md`) — the consolidated adopter developer integration guide.
  - Optional `samples/Hexalith.Conversations.Sample/` — a compilable example project (architecture-reserved path) if examples are delivered as compiled code rather than markdown snippets.
- Likely new test files (docs-validation safety net for AC4):
  - `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs` (or similarly named) — asserts referenced contract type names and `ConversationErrorCode` values exist, the doc error/version tables match `ConversationErrorCatalog`/`ConversationContractCompatibility.Current`, documentation pointers are HTTPS, and the guide/example free text passes the forbidden-surface/content-safety scan.
- Likely update files:
  - `README.md` (root) — add a link to the new integration guide; do not duplicate its content.
  - `Hexalith.Conversations.slnx` (only if a `samples` project is added and is part of the validated build).
  - `docs/adrs/index.md` (only if a doc location/DocFX decision warrants an ADR — otherwise leave untouched).
  - `_bmad-output/implementation-artifacts/tests/test-summary.md` — add Story 4.7 evidence.
- Central Package Management is active. Any required package version belongs in `Directory.Packages.props`, never inline in `.csproj`. Avoid new dependencies (including DocFX) unless proven necessary and ADR-approved. [Source: `Directory.Packages.props`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]

### Testing Requirements

- The docs-validation safety net is the AC4 mechanism and is the primary new test work. It must mechanically prove docs stay aligned with the package/client contracts and that stale or unsafe examples fail: assert referenced contract/type/error-code names resolve against the contracts assembly; assert the documented error catalog (code/category/retryable/client-action/documentation) matches `ConversationErrorCatalog`; assert documented version/compatibility metadata matches `ConversationContractCompatibility.Current`; assert the conformance command and `ConformanceRunResultV1` references are accurate; assert all documentation pointers are HTTPS; and scan the guide/example free text with the same forbidden-surface/content-safety material as `ForbiddenPublicSurfaceTest`.
- Reuse existing contract-test patterns (`ContractMetadataTest`, `ConversationErrorCatalogTest`, `ForbiddenPublicSurfaceTest`, `ContractPackageInventoryTest`, `ContractSerializationTest`) — read the doc files deterministically (no network, no live server), use xUnit v3 + Shouldly, and add no new dependency unless proven necessary. [Source: `_bmad-output/project-context.md#Testing Rules`; `tests/Hexalith.Conversations.Contracts.Tests/*`]
- If examples are delivered as a compilable `samples` project, successful compilation under warnings-as-errors is part of the alignment evidence; still scan the sample source text for forbidden fragments.
- All doc/example validation must run with only root-level submodules initialized; no test step may require nested submodule init or `git submodule update --init --recursive`. [Source: `CLAUDE.md`; `_bmad-output/project-context.md#Development Workflow Rules`]

### Previous Story Intelligence

- Stories 4.1-4.6 deferred the "full developer integration guide / DocFX / expanded examples / raw HTTP public examples" explicitly to Story 4.7 (see each story's Out of Scope). Story 4.7 is the consumer of all six. Do not re-open or re-implement their behavior — document and exemplify it. [Source: `_bmad-output/implementation-artifacts/4-3-...md`; `4-4-...md`; `4-5-...md`; `4-6-...md` Out of Scope sections]
- Recurring Story 4.4/4.5/4.6 lesson: a content-safety blocklist that is too broad collides with legitimate closed-vocabulary tokens (e.g. `error-envelope`, `projection-freshness`, `case-`, `tenant-`). Scope the docs content-safety scan to free-text/protected-value disclosure, not closed machine identifiers; reuse `ConversationError.EnsureContentSafe` material for free text. The Story 4.6 debug log even shows the sample value `"case-intake"` colliding with the `case-` fragment — be precise. [Source: `_bmad-output/implementation-artifacts/4-4-...md`; `4-5-...md`; `4-6-...md` Debug Log/Previous Story Intelligence]
- House style for Epic 4 stories: focused tests, content-safety/forbidden-surface scans, targeted-then-full validation (`dotnet test Hexalith.Conversations.slnx`), and 0-warning builds (warnings-as-errors). Recent commits are story-scoped: `feat(story-4.6): ...`, `feat(story-4.5): ...`, `feat(story-4.4): ...`. Continue this pattern. [Source: `git log --oneline -8`]
- Story 4.5 chose to ship the minimum slice through `Hexalith.Conversations.Testing` + the conformance test project and deferred the packable `src/Hexalith.Conversations.Conformance` Manifest/Evidence surface to Story 5.10. Story 4.7 should similarly favor the minimum: markdown guide + validated examples + a docs-validation test, not a heavyweight docs pipeline. [Source: `_bmad-output/implementation-artifacts/4-5-...md`]

### Latest Technical Notes

- The repository pins .NET SDK `10.0.300` and targets `net10.0` with nullable, implicit usings, and warnings-as-errors; any new sample/test project must match these sibling defaults. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `global.json`; `Directory.Build.props`]
- `dotnet test --filter` supports `FullyQualifiedName~...` and `|` composition for xUnit selection; run targeted filters first, then full-solution validation. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]
- For reading markdown/doc files inside a test, prefer reading the file relative to the repository root via a deterministic path resolution helper (the existing `ContractPackageInventoryTest` already locates repo files); do not fetch over the network. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`]

### Out of Scope

- No Epic 5 work: contract compatibility/deprecation POLICY publication (5.1), signed release conformance artifacts (5.2), versioned conformance manifest/traceability (5.3), named waivers (5.4), tenant-isolation/idempotency/redaction-replay/portability/schema-evolution release proofs (5.5-5.9), release-gate validation/aggregation (5.10), or module-vs-platform evidence separation (5.11).
- No Epic 6 work: operational telemetry dashboards, projection-lag/availability/publication observability, conformance/verification status surfaces for incidents/CI, release-scope classification, buyer partial-acceptance/waiver review, second-adopter milestones, telemetry redaction/cardinality gates, or the operator/buyer responsibility-boundary GOVERNANCE documentation (Story 6.7).
- No DocFX/API-reference generation pipeline or other new build dependency without ADR approval.
- No raw HTTP fallback examples (blocked by the `.NET client versus raw HTTP fallback policy` gate).
- No new production source behavior, new public command/projection/event/error/freshness/outcome vocabulary, new durable state, new runtime gate, globally-runnable host, or degraded/fail-open behavior.
- No re-implementation of Stories 4.1-4.6 deliverables — document and exemplify only.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 4.7: Publish Developer Integration Guide and API Examples`
- `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`
- `_bmad-output/planning-artifacts/epics.md#Shared Trust/Freshness Vocabulary Gate`
- `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`
- `_bmad-output/planning-artifacts/epics.md#Epic 6`
- `_bmad-output/planning-artifacts/prd.md#FR74`
- `_bmad-output/planning-artifacts/prd.md#FR78`
- `_bmad-output/planning-artifacts/prd.md#FR79`
- `_bmad-output/planning-artifacts/architecture.md#API Documentation`
- `_bmad-output/planning-artifacts/architecture.md#API Pattern`
- `_bmad-output/planning-artifacts/architecture.md#Boundary Contracts For External Dependencies`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/architecture.md#Source Tree`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Story Safety Rule`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`
- `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`
- `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`
- `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`
- `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`
- `_bmad-output/implementation-artifacts/4-5-provide-adopter-facing-conformance-tests-and-core-fixture.md`
- `_bmad-output/implementation-artifacts/4-6-capture-caller-metadata-for-attribution-audit-and-composition.md`
- `_bmad-output/project-context.md`
- `CLAUDE.md`
- `README.md`
- `src/Hexalith.Conversations.Contracts/README.md`
- `src/Hexalith.Conversations.Client/IConversationClient.cs`
- `src/Hexalith.Conversations.Client/ConversationClient.cs`
- `src/Hexalith.Conversations.Client/ConversationClientOptions.cs`
- `src/Hexalith.Conversations.Client/ConversationClientContext.cs`
- `src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Diagnostics/ConversationCorePreconditionCatalog.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/CallerMetadata.cs`
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationOnboardingDiagnosticsService.cs`
- `src/Hexalith.Conversations.Testing/Fixtures/ConversationConformanceCoreFixtures.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractMetadataTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationErrorCatalogTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`
- `docs/adrs/index.md`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Resolved local `bmad-dev-story` workflow customization; loaded config, project context, sprint status, readiness gates, READMEs, contracts, client surface, and contract-test patterns.
- 2026-05-23: Red phase targeted docs test failed as expected before `docs/integration-guide.md` existed: 4 documentation tests failed on missing guide.
- 2026-05-23: Targeted docs/contract validation passed: 28 tests.
- 2026-05-23: Full validation passed: `dotnet build Hexalith.Conversations.slnx` 0 warnings/0 errors; `dotnet test Hexalith.Conversations.slnx` 1025 tests passed.

### Completion Notes List

- Confirmed no Story 4.7 readiness gate blocker and honored the decided .NET client plus shared contracts path; no raw HTTP fallback examples were added.
- Published `docs/integration-guide.md` under the existing docs surface and linked it from `README.md`; the guide consolidates and links canonical README tables instead of duplicating error/precondition catalogs.
- Chose embedded C# snippets rather than a sample project to keep this documentation story within the minimum markdown plus validation scope and avoid introducing a runnable host or new dependencies.
- Added documentation validation in `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs` to keep examples, error tables, compatibility metadata, conformance references, HTTPS pointers, and content-safety rules aligned with shipped contracts.
- Updated Story 4.7 evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.

### File List

- `README.md`
- `docs/integration-guide.md`
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideWorkflowExampleTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/4-7-publish-developer-integration-guide-and-api-examples.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Story Context Validation

- Checklist reviewed: `.claude/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 4, Story 4.7 ACs (FR74/FR78/FR79), the Shared Trust/Freshness Vocabulary Gate, the Two-Level Evidence Rules, and the downstream Epic 5-6 boundaries (Story 4.7 is the final Epic 4 story).
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR74 (documented CORE behavior), FR78 (remediation guidance with machine-readable codes), and FR79 (CORE preconditions).
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on `#API Documentation` (README/API guidance, no required DocFX), `#API Pattern`/`#Architectural Boundaries` (no EventStore/substrate disclosure), `#Boundary Contracts For External Dependencies` (responsibility boundaries), `#Requirements to Structure Mapping`/`#Source Tree` (`docs/api`, `samples/Hexalith.Conversations.Sample/`), the Story Safety Rule, and ADR triggers.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`, including .NET 10 / SDK 10.0.300, central package management, Contracts/Client/Server/Testing boundaries, fail-closed tenant isolation, content-safety/logging rules, the "public contracts need README/API guidance" rule, and the root-level-only submodule policy (also enforced by `CLAUDE.md`).
  - Loaded sibling Stories 4.5 and 4.6 for house style; predecessor Stories 4.1-4.4; the current sprint status; readiness gates/decisions; the root `README.md` and `src/Hexalith.Conversations.Contracts/README.md` (existing adopter documentation state); the `Hexalith.Conversations.Client` surface; the contracts/diagnostics/conformance/caller-metadata source; the contract test safety net; the `docs/` and (absent) `samples/` layout; and recent git history.
  - Checked the `.NET client versus raw HTTP fallback policy` readiness gate to confirm examples stay at the client/contract level and exclude raw HTTP.
- Checklist fixes applied in YOLO mode:
  - Pointed dev work at the six already-shipped Epic 4 deliverables (Contracts package + compatibility metadata, .NET client, typed errors + remediation, CORE preconditions + onboarding diagnostics, conformance tests + CORE fixture, caller metadata) by real type/path, so the agent documents and exemplifies rather than re-implements.
  - Directed the guide to consolidate and cross-link the existing README tables (typed-error catalog, CORE preconditions, conformance mapping, caller metadata) instead of forking a drifting copy.
  - Made AC4 concrete and testable: a docs-validation safety net that asserts referenced contract/type/error-code names resolve, the documented error/version tables match `ConversationErrorCatalog`/`ConversationContractCompatibility.Current`, documentation pointers are HTTPS, and the guide/example free text passes the forbidden-surface/content-safety scan — reusing existing contract-test patterns.
  - Added explicit guardrails for the shared trust/freshness vocabulary, content safety (with the recurring "don't over-scope the blocklist" lesson), no-raw-HTTP, no-EventStore/substrate disclosure, no new vocabulary/durable state/host, no DocFX dependency without ADR, and no nested submodule init.
  - Added likely new/updated file lists (guide, optional samples project, docs-validation test), targeted-then-full validation commands, latest technical references, previous-story learnings, and explicit out-of-scope boundaries (Epic 5 release policy/evidence, Epic 6 telemetry/Admin UI and the Story 6.7 responsibility-boundary governance doc).
- Validation result: ready-for-dev. The story has concrete acceptance criteria, scoped documentation tasks, an enforceable AC4 docs-validation requirement, current-deliverable references, architecture/content-safety guardrails, and explicit scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Senior Developer Review (AI)

**Reviewer:** Claude Sonnet 4.6 | **Date:** 2026-05-23

**Git vs Story Discrepancies:** 1 found — `IntegrationGuideWorkflowExampleTest.cs` present in git but missing from story File List (fixed).

**Issues Found:** 0 Critical, 0 High, 1 Medium, 1 Low

### Fixed Issues

- **[MEDIUM] Missing file in story File List** — `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideWorkflowExampleTest.cs` was created and shipped but absent from the Dev Agent Record File List. Added to File List.
- **[LOW] CallerMetadata not documented in guide** — Guide introduction claims to consolidate "caller-metadata surfaces" but contained no `CallerMetadata` description or link to the canonical table, violating the Dev Notes requirement. Added a provenance-only paragraph with a canonical link in the CORE Behavior section. Added `CallerMetadata` to `RequiredGuideFragments` in `IntegrationGuideValidationTest.cs` to prevent future drift.

**Outcome:** All 10 documentation tests pass after fixes. Full solution: 1027 tests, 0 failures, 0 build warnings. Status set to **done**.

## Change Log

- 2026-05-23: Review complete — fixed missing `IntegrationGuideWorkflowExampleTest.cs` from File List, added `CallerMetadata` section to guide, added `CallerMetadata` to validation required fragments. Status set to done.
- 2026-05-23: Implemented Story 4.7 developer integration guide, validated C# client/contract examples, automated docs-alignment safety net, README link, and test-summary evidence. Status set to review.
- 2026-05-23: Created Story 4.7 context (final story of Epic 4) from Story 4.7 / FR74/FR78/FR79 requirements, the PRD/architecture (API Documentation, boundary contracts, source tree), readiness gates (no blocker; raw-HTTP policy constrains examples), project context, the six already-shipped Epic 4 deliverables (Stories 4.1-4.6), the existing root and contracts READMEs, the `Hexalith.Conversations.Client` surface, the contract test safety net, and sibling-story house style. Defined a documentation + docs-validation deliverable with an enforceable AC4 alignment net and explicit Epic 5-6 out-of-scope boundaries. Status set to ready-for-dev.
