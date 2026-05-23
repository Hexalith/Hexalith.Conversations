# Story 4.5: Provide Adopter-Facing Conformance Tests and CORE Fixture

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want adopter-facing conformance tests and a representative CORE fixture,
so that I can prove my integration respects Conversations contracts before deployment.

## Acceptance Criteria

1. Adopter-facing conformance suite covers the CORE integration surface and emits machine-readable results
   - Given an adopter installs or references the conformance test package,
   - When they run the adopter-facing test suite,
   - Then tests cover create conversation, append message, read timeline, tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, governance preconditions, and compatibility discovery,
   - And results are machine-readable and suitable for CI use.

2. The CORE fixture is synthetic, content-safe, and exercises happy-path and typed failure cases
   - Given the CORE fixture is loaded,
   - When contract tests execute against it,
   - Then the fixture includes at least one tenant-scoped conversation happy path with participants, message attribution, business references, projection freshness, and typed failure cases,
   - And fixture data is synthetic, content-safe, and does not require production tenant data or provider credentials.

3. Conformance failures map to a requirement, precondition, or release-gate category and distinguish failure classes
   - Given a conformance test fails,
   - When results are reported,
   - Then the failure maps to the relevant requirement, precondition, or release-gate category,
   - And output distinguishes product invariant failures from infrastructure, configuration, unavailable dependency, and execution failures.

4. CI execution proves adopter-readiness with safe, traceable output and no nested submodule dependency
   - Given conformance tests run in CI,
   - When supported, unsupported, stale, cross-tenant, duplicate command, projection lag, and sanitized error scenarios are exercised,
   - Then tests prove adopter-readiness, traceable failures, safe output, and no dependency on nested submodule initialization.

## Tasks / Subtasks

- [x] Confirm scope, readiness gates, evidence policy, and reuse existing primitives before implementation (AC: 1-4)
  - [x] Re-read `_bmad-output/implementation-artifacts/readiness-gates.md`; confirm `.NET client versus raw HTTP fallback policy` (which lists Story 4.5) and `Projection freshness blocking semantics` remain `decided` or `waived`. Story 4.5 is blocked otherwise.
  - [x] Honor the two-level evidence rule: Story 4.5 closes on minimum local evidence — the adopter-facing conformance package and CORE fixture run locally or in CI and produce machine-readable safe results. Do NOT implement release-gate aggregation, signed artifacts, manifest rows, or waiver governance; those are carried forward into Story 5.10. [Source: `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`]
  - [x] Preserve Story 4.2's supported integration path: the shared `Hexalith.Conversations.Contracts` package plus `Hexalith.Conversations.Client`. Conformance assertions and the fixture target Conversations contracts/client surfaces. Do NOT add adopter-facing raw HTTP fallback examples; raw HTTP fallback requires a separate buyer/diagnostics approval per the readiness gate.
  - [x] Reuse the shared trust/freshness vocabulary as canonical: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted` from `ProjectionTrustState`; only `Current` is trust-bearing. Reuse the closed onboarding diagnostic status vocabulary (`ready`/`degraded`/`blocked`/`unknown`) from Story 4.4 where the conformance suite reports readiness. Do NOT invent conformance-only synonyms such as `ok`, `healthy`, `pass-ish`, or `maybe` for trust/freshness or readiness.
  - [x] Reuse the existing typed error catalog from Story 4.3 (`ConversationErrorCatalog`, `ConversationError`, `ConversationErrorCode`, `ConversationErrorCategory`, `ConversationErrorClientAction`) for the error-envelope conformance check and for typed failure cases in the fixture. Do NOT create a parallel error envelope.
  - [x] Reuse Story 4.1 `ConversationContractCompatibility.Current` / `Evaluate(...)` for the compatibility-discovery conformance check and Story 4.4 `ConversationOnboardingDiagnosticsService` / `ConversationCorePreconditionCatalog` for governance-precondition coverage. Do NOT re-derive version or precondition checks.

- [x] Define a content-safe, machine-readable conformance result contract surface (AC: 1, 3)
  - [x] Add conformance result contracts under `src/Hexalith.Conversations.Contracts/Conformance` (new folder) using `SchemaVersion.Current` and closed-vocabulary types. Suggested types: `ConformanceRunResultV1`, `ConformanceCheckResultV1`, a closed `ConformanceCheck` vocabulary (create-conversation, append-message, read-timeline, tenant-binding, party-identity, idempotency, error-envelope, projection-freshness, event-publication, governance-precondition, compatibility-discovery), a closed `ConformanceOutcome` vocabulary, and a closed `ConformanceFailureClassification` vocabulary.
  - [x] `ConformanceOutcome` must reuse/align with the shared trust/freshness and Story 4.4 readiness language rather than introducing synonyms; justify each value against the Shared Trust/Freshness Vocabulary Gate.
  - [x] `ConformanceFailureClassification` must distinguish product-invariant failure from infrastructure, configuration, unavailable-dependency, and execution failure (AC3). Map each check result to the relevant requirement (FR/NFR id), precondition id (from `ConversationCorePreconditionCatalog`), and release-gate category so Story 5.10 can later aggregate without rework.
  - [x] Carry only structured, content-safe data: check id, outcome code, failure classification, requirement/precondition/release-gate identifiers, safe message, remediation guidance code, documentation URI, correlation id, and optional audit handle where allowed. Do NOT put tenant IDs, Party IDs, conversation IDs/existence, provider session/payload values, business-reference values, raw exception text, local file paths, or production secrets in any conformance field.
  - [x] If the conformance result reuses typed failures, embed the shared `ConversationError`/`ConversationErrorResult` rather than re-serializing free text.
  - [x] Register every new conformance contract in `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` so it participates in serialization, forbidden-surface, and content-safety scans. Add converters under `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` only when new closed vocabularies require them, following the existing pattern.
  - [x] Keep `Hexalith.Conversations.Contracts` infrastructure-free: no references to EventStore, Tenants, Parties, FrontComposer, ASP.NET Core, Dapr, server, client, or UI packages.

- [x] Build the deterministic, synthetic CORE fixture (AC: 2)
  - [x] Place reusable CORE fixture builders in `src/Hexalith.Conversations.Testing/Fixtures` (the project that already hosts `BuyerAcceptanceDemoFixtures`); use `tests/fixtures/adopter-happy-path` only if static fixture files are necessary. Keep all data synthetic and clearly marked (mirror the `SyntheticDataMarker = "synthetic-demo-data"` convention).
  - [x] Reuse existing projection and contract models — `ConversationDetailProjectionV1`, `ConversationSummaryProjectionV1`, `ProjectionFreshnessV1`, `ConversationParticipantProjectionV1`, `ConversationTimelineMessageProjectionV1`, `BusinessReference`, `ConversationError`/`ConversationErrorResult`, and the diagnostic/compatibility contracts — instead of inventing a parallel fixture transcript model. Follow the `BuyerAcceptanceDemoFixtures` builder shape.
  - [x] Provide at minimum: one authorized tenant-scoped happy-path conversation with participants, message attribution, business references, and `Current` projection freshness; plus typed failure cases covering unsupported schema/version, stale projection, cross-tenant denial (hidden/unavailable shape), duplicate-command idempotency conflict, and a sanitized error-envelope case.
  - [x] Include unique cross-tenant poison sentinel values (mirror `PoisonSentinelValues`) that conformance content-safety tests scan for and that must never appear in any authorized-tenant client-observable surface, safe label, copied text, diagnostics text, or conformance summary.
  - [x] Do NOT require production tenant data or provider credentials. Do NOT append conversation events, mutate aggregate state, write production projection stores, persist export artifacts, or initialize nested submodules to load the fixture.

- [x] Add the adopter-facing conformance test project/suite (AC: 1, 3, 4)
  - [x] Add `tests/Hexalith.Conversations.Conformance.Tests` (the architecture reserves this test project name) and register it in `Hexalith.Conversations.slnx`. Mirror sibling test-project setup: `net10.0`, nullable enabled, implicit usings, warnings-as-errors, xUnit v3, Shouldly, NSubstitute. Reference only adopter-supported surfaces (`Hexalith.Conversations.Contracts`, `Hexalith.Conversations.Client`, `Hexalith.Conversations.Testing`) — do not reference `Hexalith.Conversations.Server` internals from the adopter-facing suite unless a deterministic in-process boundary is required, and if so keep it behind the same opt-in pattern used by existing tests. (NSubstitute is not added: the suite is deterministic and fixture-driven, so no mocking dependency is required; this respects Central Package Management's "avoid new dependencies unless proven necessary".)
  - [x] Decide explicitly (and record in Dev Notes) whether a separate packable `src/Hexalith.Conversations.Conformance` library is needed for adopters to reference, or whether the reusable suite/fixtures ship through `Hexalith.Conversations.Testing` plus the test project. The architecture reserves `src/Hexalith.Conversations.Conformance` (`Manifest/`, `Suites/`, `Evidence/`, `Verification/`), but Story 4.5 must deliver only the minimum local-evidence slice; defer the Manifest/Evidence/signing surface to Story 5.10. If a packable conformance library is added, it must follow Central Package Management and stay infrastructure-free at the adopter boundary.
  - [x] Implement one conformance check per `ConformanceCheck` vocabulary value (create conversation, append message, read timeline, tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, governance preconditions, compatibility discovery), each asserting against the CORE fixture and producing a `ConformanceCheckResultV1` with the correct requirement/precondition/release-gate mapping and failure classification.
  - [x] Exercise the AC4 scenario matrix: supported, unsupported, stale, cross-tenant, duplicate command, projection lag, and sanitized error. Assert each produces accurate outcome, the correct failure classification, traceable mapping, and content-safe output.
  - [x] Make results machine-readable and CI-suitable: serialize `ConformanceRunResultV1` to deterministic web JSON. Assert the serialized run result is content-safe and stable (camelCase, additive-JSON tolerant) so CI can consume it.
  - [x] Guarantee no nested submodule dependency (AC4): the suite and fixture must run after root-level submodule init only. Do NOT add steps requiring `git submodule update --init --recursive` or nested submodule initialization. [Source: `CLAUDE.md`; `_bmad-output/project-context.md#Development Workflow Rules`]

- [x] Preserve side-channel safety and fail-closed behavior in conformance coverage (AC: 2, 4)
  - [x] The cross-tenant scenario must assert the hidden/unavailable result shape used elsewhere (`ConversationReadApi` side-channel equivalence) and must not reveal whether a protected tenant or conversation exists. Do NOT make unauthorized vs nonexistent distinguishable in conformance output.
  - [x] Reuse existing fail-closed primitives where the suite touches server boundaries (tenant access guard, freshness gate, audit pairing, idempotency executor); do NOT duplicate or relax these checks inside the conformance path. (The adopter-facing suite stays read-oriented against the contracts/fixture surface and does not touch server boundaries directly.)
  - [x] If any logging is added, use source-generated logging or static templates with semantic placeholders; never interpolate raw error text, secrets, protected IDs, or payload values. (No logging was added.)

- [x] Validate and record evidence (AC: 1-4)
  - [x] Run targeted contract tests for new conformance contracts:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Conformance|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~Versioning"`
  - [x] Run the new conformance test project:
    - `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`
  - [x] Pack the contracts package (and conformance package if added) to confirm adopter-safe inventory:
    - `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation`
  - [x] Run the full solution before closing:
    - `dotnet test Hexalith.Conversations.slnx`
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 4.5 evidence (machine-readable conformance run, CORE fixture coverage, AC4 scenario matrix, content-safety scans, full-solution counts).

- [x] Document the adopter conformance package and CORE fixture minimally (AC: 1, 2)
  - [x] Update `README.md` and/or `src/Hexalith.Conversations.Contracts/README.md` with a compact table: conformance check, requirement/precondition/release-gate mapping, and how to run the adopter suite in CI. Keep examples at the contract/client level, not raw HTTP fallback.
  - [x] Do NOT document raw server routes, EventStore mechanics, internal handler names, storage keys, projection topology, provider payloads, secrets, or production exception samples. The full developer integration guide remains Story 4.7.

- [x] Preserve scope boundaries and stop conditions (AC: 1-4)
  - [x] Do NOT implement Story 4.6 caller-metadata capture or Story 4.7 full developer integration guide / DocFX / expanded examples.
  - [x] Do NOT implement Epic 5 release signing, versioned conformance manifest, named-waiver lifecycle, deprecation policy publication, or release-gate evidence aggregation (Story 5.10 consumes this story's local evidence). Do NOT implement Epic 6 telemetry/Admin UI.
  - [x] Stop for ADR/architecture review before adding new durable state, a new public error/status/freshness/outcome vocabulary outside what the shared gates allow, a new runtime gate semantic, a globally-runnable host, or any degraded/fail-open behavior. (The new closed conformance outcome/classification vocabularies stay bounded and align to the shared trust/freshness + Story 4.4 readiness gates; no ADR trigger was crossed.)

## Dev Notes

### Epic and Business Context

- Epic 4 makes adopter integration credible through a contract package, a supported .NET client, compatibility discovery, typed sanitized errors, onboarding diagnostics, remediation guidance, CORE preconditions, and now adopter-facing conformance tests. Story 4.5 delivers the adopter-facing conformance suite and a representative CORE fixture. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`; `_bmad-output/planning-artifacts/epics.md#Story 4.5: Provide Adopter-Facing Conformance Tests and CORE Fixture`]
- Story 4.5 covers FR73 (adopter developers can run adopter-facing conformance tests before deployment) and FR74 (adopters can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior). [Source: `_bmad-output/planning-artifacts/prd.md#FR73`; `_bmad-output/planning-artifacts/prd.md#FR74`]
- Two-level evidence is binding: implementation stories close on minimum local evidence; release-gate evidence (manifest aggregation, signing, waivers) closes later in Epic 5. Story 4.5 closes when the adopter-facing conformance package and CORE fixture run locally or in CI and produce machine-readable safe results; Story 5.10 consumes this local evidence and adds release-gating contract validation and CORE fixture manifest coverage. Do not build the manifest/signing surface here. [Source: `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`; `_bmad-output/implementation-artifacts/readiness-gates.md`]

### Readiness Gate Context

- `.NET client versus raw HTTP fallback policy` is `decided` and explicitly lists Story 4.5 in its Blocks column: the .NET client plus the shared contract package is the supported v1 path; raw HTTP examples require later buyer or diagnostics approval. Conformance assertions and the CORE fixture must target Conversations contracts/client, not raw HTTP fallback. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`]
- `Projection freshness blocking semantics` is `decided`: canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted`; only `Current` is trust-bearing. The projection-freshness and stale/projection-lag conformance checks must classify with exactly this vocabulary. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#projection-freshness-blocking-semantics`]
- The Shared Trust/Freshness Vocabulary Gate requires conformance output to use the one approved trust/freshness vocabulary and metadata shape; do not let conformance output diverge from API, client, and diagnostics surfaces. [Source: `_bmad-output/planning-artifacts/epics.md#Shared Trust/Freshness Vocabulary Gate`]

### Current Implementation State

- The architecture reserves `src/Hexalith.Conversations.Conformance` (`Manifest/`, `Suites/`, `Evidence/`, `Verification/`) and `tests/Hexalith.Conversations.Conformance.Tests`, plus `tests/fixtures/adopter-happy-path`. Neither the Conformance source project, the Conformance test project, nor `tests/fixtures` exists yet — this story creates the minimum local-evidence slice. Add the test project to `Hexalith.Conversations.slnx` (currently lists Client/Contracts/Integration/Server/Tests under `/tests/`). [Source: `_bmad-output/planning-artifacts/architecture.md#Source Tree`; `Hexalith.Conversations.slnx`]
- `Hexalith.Conversations.Testing` already hosts deterministic synthetic fixtures: `Fixtures/BuyerAcceptanceDemoFixtures.cs` (Story 3.7) demonstrates the canonical pattern — a `SyntheticDataMarker`, deterministic `DateTimeOffset`, authorized vs poison tenants, full/redacted/stale/missing-citation/unresolved-participant/blocked-command/verification fixtures, and unique `PoisonSentinelValues` for cross-tenant leakage scanning. Build the CORE fixture in the same project and shape; reuse `Factories/ConversationTestIds.cs` for stable IDs. The `Testing` project is `IsPackable=true` and references `Contracts` + `Hexalith.Conversations` only. [Source: `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemoFixtures.cs`; `src/Hexalith.Conversations.Testing/Factories/ConversationTestIds.cs`; `src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj`]
- The typed error system (Story 4.3) is the canonical envelope: `ConversationErrorCatalog.Get(code)` / `CreateError(...)`, `ConversationError` with `Code`, `Category`, `IsRetryable`, `ClientAction`, `SafeMessage`, `CorrelationId`, optional `AuditHandle`/`Documentation`/`SafeFieldDiagnostics`, plus a free-text safety blocklist. Reuse this for the error-envelope conformance check and typed fixture failures. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`]
- `ConversationContractCompatibility.Current` / `Evaluate(ContractCompatibilityRequest)` (Story 4.1) produces content-safe, machine-readable compatibility status and bounded remediation; delegate the compatibility-discovery conformance check here rather than re-deriving versions. [Source: `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`]
- `ConversationOnboardingDiagnosticsService` and `ConversationCorePreconditionCatalog` (Story 4.4) document and evaluate CORE preconditions with the shared error catalog and the closed `OnboardingDiagnosticCheck`/`OnboardingDiagnosticStatus` vocabularies mapped to `ready`/`degraded`/`blocked`/`unknown`. Use the precondition catalog for the governance-precondition conformance mapping and align `ConformanceOutcome` to this readiness language. [Source: `src/Hexalith.Conversations.Contracts/Diagnostics/ConversationCorePreconditionCatalog.cs`; `src/Hexalith.Conversations.Server/Diagnostics/ConversationOnboardingDiagnosticsService.cs`; `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`]
- The existing contract test safety net (`ContractSamples.cs`, `ForbiddenPublicSurfaceTest.cs`, `ContractSerializationTest.cs`, `ContractPackageInventoryTest.cs`) scans serialized contracts for forbidden infrastructure/personal-data terms and verifies adopter-safe package inventory. New conformance contracts MUST be registered in `ContractSamples.AllContracts` or they bypass the scans; `ContractPackageInventoryTest` already forbids `Hexalith.Conversations.Server`, `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, `Dapr`, `Microsoft.AspNetCore`, `obj/`, `tests/`, and `.Tests` from any packaged inventory. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`; `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`; `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`]
- `src/Hexalith.Conversations.Server/Program.cs` is intentionally fail-closed; API extensions are opt-in for hosts/tests. Do not make the host broadly runnable. The adopter-facing suite should prefer the contracts/client/fixture surface and any deterministic in-process boundary already used by existing tests. [Source: `src/Hexalith.Conversations.Server/Program.cs`; `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`]

### Architecture and Contract Guardrails

- The architecture maps FR70-FR80 developer experience to `Contracts`, `Client`, `samples`, and `tests/fixtures/adopter-happy-path`, and FR81-FR94 compatibility/evidence to `Conformance`, `docs/release-evidence`, and `tests/Hexalith.Conversations.Conformance.Tests`. Story 4.5 sits at the boundary: deliver adopter conformance tests + CORE fixture (FR73/FR74) without building the Epic 5 release-evidence/manifest surface. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- `Conformance` produces release evidence and verification outputs and does NOT mutate conversation state directly; conformance evidence is derived, not authoritative. Keep the conformance path read-oriented and side-effect-free on durable state. [Source: `_bmad-output/planning-artifacts/architecture.md#Service Boundaries`; `_bmad-output/planning-artifacts/architecture.md#Data Boundaries`]
- Conformance evidence must be traceable to FR/NFR/carry-forward commitments and must not become decorative; the per-check requirement/precondition/release-gate mapping (AC3) is the mechanism that keeps it traceable for Story 5.10. [Source: `_bmad-output/planning-artifacts/architecture.md#Anti-Patterns`; `_bmad-output/planning-artifacts/architecture.md#Conformance Tests`]
- Error/response formats use content-safe Problem Details or existing typed error contracts with stable code, category, retryability, correlation id, and safe documentation pointer; failure responses must not distinguish unauthorized from nonexistent cross-tenant resources unless an ADR permits it. The conformance suite must assert, not violate, these invariants. [Source: `_bmad-output/planning-artifacts/architecture.md#API Response Formats`]
- Public APIs/contracts must not expose EventStore stream names, positions, snapshots, envelopes, projection topology, SignalR groups, handler names, or storage terms. New conformance contracts must stay in adopter-safe Conversations language. [Source: `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Conformance packs are a named architectural verification mechanism (xUnit v3, Shouldly, NSubstitute, Testcontainers, conformance packs); fixtures are shared under `tests/fixtures` and must not fork per test project; conformance tests map to FR/NFR IDs and release gates. [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns`; `_bmad-output/planning-artifacts/architecture.md#File Organization Patterns`]
- Story Safety Rule: no implementation story may introduce durable state, cache, export, memory write, cross-boundary contract, or new privileged execution path without naming its owning decision, failure semantics, and conformance evidence. Adding a new public outcome/classification vocabulary or a packable conformance library is a load-bearing choice — keep it bounded and stop for ADR if it crosses an ADR trigger. [Source: `_bmad-output/planning-artifacts/architecture.md#Story Safety Rule`; `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]

### File Structure Guidance

- Likely new contract files (under `src/Hexalith.Conversations.Contracts/Conformance`):
  - `ConformanceRunResultV1.cs`
  - `ConformanceCheckResultV1.cs`
  - `ConformanceCheck.cs` (closed vocabulary)
  - `ConformanceOutcome.cs` (closed vocabulary, aligned to shared trust/freshness + Story 4.4 readiness language)
  - `ConformanceFailureClassification.cs` (closed vocabulary: product-invariant vs infrastructure vs configuration vs unavailable-dependency vs execution)
- Likely new fixture files (under `src/Hexalith.Conversations.Testing/Fixtures`):
  - `ConversationConformanceCoreFixtures.cs` (synthetic happy-path + typed failure cases + poison sentinels), modeled on `BuyerAcceptanceDemoFixtures.cs`.
- Likely new test files:
  - `tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` (new project; add to `Hexalith.Conversations.slnx`)
  - `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuiteTest.cs`
  - `tests/Hexalith.Conversations.Conformance.Tests/CoreFixtureContentSafetyTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceContractsTest.cs`
- Likely update files:
  - `Hexalith.Conversations.slnx` (register the new test project, and the conformance source project if added)
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (register conformance contracts)
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs` (cover new free-text fields)
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (only if new closed vocabularies need converters)
  - `README.md` and/or `src/Hexalith.Conversations.Contracts/README.md`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` if the public file inventory changes.
- Central Package Management is active. Any required package version belongs in `Directory.Packages.props`, never inline in `.csproj`. Avoid new dependencies unless proven necessary. [Source: `Directory.Packages.props`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]

### Testing Requirements

- Contract tests must prove closed vocabulary, `JsonSerializerDefaults.Web` serialization, additive-JSON tolerance, descriptor coverage for every conformance check, requirement/precondition/release-gate mapping, safe messages, bounded remediation codes, HTTPS documentation pointers, and forbidden-surface/content-safety scans for all new conformance fields.
- The adopter-facing suite must cover all eleven `ConformanceCheck` values (create conversation, append message, read timeline, tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, governance preconditions, compatibility discovery) plus the AC4 scenario matrix (supported, unsupported, stale, cross-tenant, duplicate command, projection lag, sanitized error).
- Side-channel tests must prove the cross-tenant scenario returns the hidden/unavailable shape and does not reveal protected tenant/conversation existence; failure classification must distinguish product-invariant from infrastructure/configuration/unavailable-dependency/execution failures.
- Content-safety/leakage tests must scan serialized conformance run results, the CORE fixture, and any log-like/exception text for forbidden fragments: tenant IDs, Party IDs, conversation IDs, conversation existence hints, provider session/payload, business-reference values, redacted text, poison sentinel values, `EventStore`, `stream`, `snapshot`, `envelope`, `SignalR`, `handler`, `dispatcher`, `repository`, `store`, raw exception text, `C:\`, and `D:\`. (Closed-vocabulary tokens such as `projection-freshness` or `error-envelope` are safe machine identifiers; the scan targets free-text and protected-value disclosure.)
- Machine-readable output must be deterministic and CI-suitable: serialized `ConformanceRunResultV1` must round-trip and remain stable so CI can parse pass/fail/classification.
- The suite and fixture must run with only root-level submodules initialized; no test step may require nested submodule init or `git submodule update --init --recursive`.
- Use xUnit v3, Shouldly, NSubstitute, and existing testing helpers/fakes (tenant access, projection health, participant directory, temporal source); reuse existing authorization/serialization test patterns rather than inventing new fakes. No sleeps, no live servers, no external services. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/planning-artifacts/architecture.md#Testing Stack`]

### Previous Story Intelligence

- Story 3.7 (buyer acceptance demo) established the canonical synthetic-fixture pattern now in `Hexalith.Conversations.Testing`: deterministic data, explicit synthetic markers, full/redacted/stale/missing-citation/unresolved-participant/blocked-command/verification/cross-tenant-poison fixtures, unique poison sentinel values scanned by content-safety tests, and read-oriented execution that never appends events or writes production state. Reuse this exact pattern for the CORE fixture; do not invent a parallel fixture model. [Source: `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`; `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemoFixtures.cs`]
- Story 4.1 created compatibility metadata and enforced invariants (supported status must not carry remediation; unsupported/invalid carries a typed error). The compatibility-discovery conformance check must reuse `Evaluate(...)` and respect those invariants. [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`]
- Story 4.2 implemented the supported .NET client happy path and deferred conformance tooling to Story 4.5; the suite should align with the client's typed-result behavior so REST, client, and conformance surfaces agree. Keep Story 4.2's non-seekable response handling and tenant-denial fallback regressions intact if the suite touches the client. [Source: `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`]
- Story 4.3 centralized typed sanitized errors in `ConversationErrorCatalog`/`ConversationError` with `ClientAction`/`SafeMessage` and hardened free-text safety (rejecting tenant, Party, conversation, provider-session/payload, business-reference, local-path, and raw-exception markers). Reuse this catalog for the error-envelope check and typed fixture failures; do not weaken the free-text guardrails. It explicitly noted: "Prepare conformance tooling by making the catalog easy to consume from test projects, but do not implement the Story 4.5 conformance package" — Story 4.5 is the consumer. [Source: `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`]
- Story 4.4 added `ConversationOnboardingDiagnosticsService`, `ConversationCorePreconditionCatalog`, and the closed onboarding diagnostic vocabularies mapped to `ready`/`degraded`/`blocked`/`unknown`, and proved side-channel equivalence for freshness/availability denials collapsing to a hidden `unknown` shape. Reuse the precondition catalog and diagnostic vocabulary; align `ConformanceOutcome` to the same readiness language and preserve side-channel equivalence in the cross-tenant conformance check. A recurring Story 4.4 lesson: a content-safety blocklist that is too broad collides with legitimate closed-vocabulary tokens (e.g. `subscription`, `party-`, `tenant-`) — scope leakage scans to free-text/protected values, not closed machine identifiers. [Source: `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`]
- Recent commits are story-scoped and test-heavy: `feat(story-4.4): Define core preconditions and onboarding diagnostics`, `feat(story-4.3): Expose typed sanitized errors and remediation guidance`, `feat(story-4.2): Provide supported .NET client happy path`, `feat(story-4.1): Add contract compatibility metadata`. Continue with focused tests, content-safety/side-channel checks, and full-solution validation before closing. [Source: `git log --oneline -5`]

### Latest Technical Notes

- `System.Text.Json` supports records/immutable types; new closed-vocabulary conformance contracts should follow the existing `JsonSerializerDefaults.Web` plus custom-converter pattern in `ClosedVocabularyJsonConverters.cs`. [Source: `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/deserialization#deserialization-behavior`]
- `dotnet test --filter` supports `FullyQualifiedName~...` and `|` composition for xUnit selection; run targeted filters first, then full-solution validation. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]
- Microsoft library logging guidance recommends source-generated logging and warns against string interpolation; if the suite emits logs, use static templates/source-generated logging and never log raw error payloads, protected IDs, provider payloads, secrets, or exception detail as client-visible diagnostics. [Source: `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`]
- The repository pins .NET SDK `10.0.300` and targets `net10.0` with nullable, implicit usings, and warnings-as-errors; the new conformance test project must match these sibling defaults. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `global.json`]

### Out of Scope

- No Story 4.6 caller-metadata capture, or Story 4.7 full developer integration guide, DocFX/API reference pipeline, expanded API examples, or raw HTTP public examples.
- No Epic 5 release signing, versioned conformance manifest, named-waiver lifecycle, deprecation policy publication, or release-gate evidence aggregation. Story 5.10 consumes Story 4.5 local evidence and adds release-gating contract validation and CORE fixture manifest coverage. No Epic 6 telemetry dashboards or Admin UI work.
- No `src/Hexalith.Conversations.Conformance` `Manifest/`, `Evidence/`, or `Verification/` signing/aggregation surface — only the minimum local-evidence conformance slice and CORE fixture.
- No new durable state, transcript tables, runtime health dashboard, FrontComposer surface, background worker, raw EventStore endpoint, or globally-runnable host.
- No new public error/status/freshness/outcome vocabulary outside the shared gates, and no degraded/fail-open behavior, without ADR approval.
- No raw HTTP fallback examples (blocked by the `.NET client versus raw HTTP fallback policy` gate).

### References

- `_bmad-output/planning-artifacts/epics.md#Story 4.5: Provide Adopter-Facing Conformance Tests and CORE Fixture`
- `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`
- `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`
- `_bmad-output/planning-artifacts/epics.md#Shared Trust/Freshness Vocabulary Gate`
- `_bmad-output/planning-artifacts/prd.md#FR73`
- `_bmad-output/planning-artifacts/prd.md#FR74`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Service Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#API Response Formats`
- `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`
- `_bmad-output/planning-artifacts/architecture.md#Conformance Tests`
- `_bmad-output/planning-artifacts/architecture.md#Story Safety Rule`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`
- `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`
- `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`
- `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`
- `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`
- `_bmad-output/project-context.md`
- `CLAUDE.md`
- `README.md`
- `Hexalith.Conversations.slnx`
- `src/Hexalith.Conversations.Contracts/README.md`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Diagnostics/ConversationCorePreconditionCatalog.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationOnboardingDiagnosticsService.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Program.cs`
- `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemoFixtures.cs`
- `src/Hexalith.Conversations.Testing/Factories/ConversationTestIds.cs`
- `src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`
- `Directory.Build.props`
- `Directory.Packages.props`
- `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/deserialization#deserialization-behavior`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`
- `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context) (claude-opus-4-7[1m])

### Debug Log References

- Hit the recurring Story 4.4 "blocklist too broad" lesson: the shared `ConversationError` free-text disclosure blocklist (`tenant-`, `party-`, `case-`, `envelope`, ...) collided with legitimate closed traceability tokens such as `release-gate-tenant-isolation` and the closed `error-envelope` check id. Resolved by giving conformance requirement/precondition/release-gate mapping tokens a closed-token validator that enforces the bounded charset (which already excludes `:`/`\`/`/`) but does NOT run the free-text blocklist, and by keeping correlation ids / safe messages free of those substrings. Closed-vocabulary tokens are safe machine identifiers; the content-safety scan targets free-text and protected-value disclosure only.
- Adjusted the `ConformanceCheckResultV1` typed-error invariant to be outcome-based (mirroring Story 4.4 `OnboardingDiagnosticCheckResultV1`): a `ready` outcome carries no error; any non-ready outcome (`degraded`/`blocked`/`unknown`) embeds the observed shared `ConversationError`. This correctly models a conformant check that legitimately observed an expected typed failure (e.g. a conformant idempotency check that surfaced a non-retryable conflict).

### Completion Notes List

- Added the adopter-facing conformance contract surface under `src/Hexalith.Conversations.Contracts/Conformance`: closed `ConformanceCheck` (11 CORE checks), `ConformanceOutcome` (`ready`/`degraded`/`blocked`/`unknown`, aligned to the shared trust/freshness + Story 4.4 readiness language; no synonyms), `ConformanceFailureClassification` (`conformant` plus product-invariant/infrastructure/configuration/unavailable-dependency/execution), `ConformanceCheckResultV1`, and `ConformanceRunResultV1`. Each check result carries requirement/precondition/release-gate traceability and embeds the shared `ConversationError` for typed failures. The contracts stay infrastructure-free and were registered in `ContractSamples.cs` plus the closed-vocabulary JSON converters.
- Built the deterministic, synthetic, content-safe CORE fixture `ConversationConformanceCoreFixtures` in `Hexalith.Conversations.Testing`, reusing existing projection/error contracts (no parallel transcript model) following the Story 3.7 `BuyerAcceptanceDemoFixtures` pattern. It provides one authorized happy-path conversation (participants, message attribution, business references, `Current` freshness) plus unsupported/stale/cross-tenant/duplicate-command/sanitized-error typed failures and unique poison sentinels. Loading it appends no events, mutates no state, and needs no nested submodule init.
- Added the reserved `tests/Hexalith.Conversations.Conformance.Tests` project, registered it in `Hexalith.Conversations.slnx`, and implemented the `AdopterConformanceSuite` runner reusing Story 4.1 `ConversationContractCompatibility.Evaluate`, Story 4.3 `ConversationErrorCatalog`, and Story 4.4 `ConversationCorePreconditionCatalog`. The suite implements one check per `ConformanceCheck` value, exercises the AC4 scenario matrix, and emits a deterministic, CI-suitable `ConformanceRunResultV1`.
- Decision: the minimum local-evidence slice ships through `Hexalith.Conversations.Testing` (fixture) plus the new conformance test project. No separate packable `src/Hexalith.Conversations.Conformance` library and no Manifest/Evidence/signing/aggregation surface were created; those are deferred to Story 5.10 per the two-level evidence rule.
- AC4: no nested git submodule was introduced; the suite and fixture run after root-level submodule init only and no step uses `git submodule update --init --recursive`.
- Validation: targeted contract tests 58 passed; conformance test project 16 passed; `dotnet pack` produced the adopter-safe contracts `.nupkg`; full solution `dotnet test Hexalith.Conversations.slnx` all passed — Client 23, Conformance 16, Integration 8, Core 139, Server 423, Contracts 333 (942 total). `dotnet build Hexalith.Conversations.slnx` succeeds with 0 warnings (warnings-as-errors).

### Senior Developer Review (AI)

- 2026-05-23: Adversarial review found that the AC4 cross-tenant scenario was only proven against the fixture in `CoreFixtureContentSafetyTest` and was never exercised through the machine-readable `AdopterConformanceSuite.Run()` result — so the run result carried no `cross-tenant` scenario, no per-check outcome/classification/traceable mapping for it, and never produced the hidden side-channel-equivalent `unknown` outcome. This left the suite portion of the AC4 scenario-matrix task materially incomplete despite being marked done.
- Fix: `CheckTenantBinding` now exercises the cross-tenant denial directly. It asserts the authorized read stays tenant-scoped AND that the cross-tenant request collapses to the hidden shape (`aggregate_not_found` / `Hidden` category / `HideOrRefresh`, non-retryable), emitting the `cross-tenant` scenario with the conformant `unknown` outcome carrying the typed denial. Added `TenantBindingCheckShouldExerciseCrossTenantHiddenSideChannelShape`, and updated the matrix and outcome-coverage tests to require `cross-tenant` and the `unknown` outcome in the run result. No production contract changed; the closed vocabularies, side-channel equivalence, and content-safety guarantees are unchanged and still proven.
- Re-validation: `dotnet build Hexalith.Conversations.slnx` succeeds with 0 warnings; `dotnet test Hexalith.Conversations.slnx` all passed — Client 23, Conformance 25, Integration 8, Core 139, Server 423, Contracts 349 (967 total). AC4 no-nested-submodule guarantee re-verified: `.gitmodules` unchanged and all submodules remain root-level. Status set to done; sprint-status `4-5-provide-adopter-facing-conformance-tests-and-core-fixture` set to done.

### File List

- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs` (new)
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs` (new)
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs` (new)
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs` (new)
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (modified)
- `src/Hexalith.Conversations.Contracts/README.md` (modified)
- `src/Hexalith.Conversations.Testing/Fixtures/ConversationConformanceCoreFixtures.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuite.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuiteTest.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/CoreFixtureContentSafetyTest.cs` (new)
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceContractsTest.cs` (new)
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (modified)
- `Hexalith.Conversations.slnx` (modified)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)

### Change Log

| Date | Change |
| --- | --- |
| 2026-05-23 | Story 4.5: Added adopter-facing conformance contracts (`Conformance` closed vocabularies and `ConformanceCheckResultV1`/`ConformanceRunResultV1`), the deterministic synthetic CORE fixture in `Hexalith.Conversations.Testing`, and the reserved `tests/Hexalith.Conversations.Conformance.Tests` project (registered in the solution) with the `AdopterConformanceSuite` runner covering all eleven CORE checks and the AC4 scenario matrix. Reused Story 4.1 compatibility, Story 4.3 typed errors, and Story 4.4 CORE preconditions; added contract, suite, and content-safety/side-channel tests; documented the conformance suite and CORE fixture in the contracts README and test-summary. Full solution: 942 tests passing. Status set to review. |
| 2026-05-23 | Senior review: `CheckTenantBinding` now exercises the AC4 cross-tenant scenario through the machine-readable run result, collapsing the cross-tenant denial to the hidden side-channel-equivalent `unknown` outcome with the typed `aggregate_not_found` denial; hardened the suite scenario-matrix and outcome-coverage tests. Full solution: 967 tests passing (Client 23, Conformance 25, Integration 8, Core 139, Server 423, Contracts 349). Status set to done. |

## Story Context Validation

- Checklist reviewed: `.claude/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 4, Story 4.5 ACs, the Two-Level Evidence Rules (Story 4.5 local-evidence closure, Story 5.10 carry-forward), the Shared Trust/Freshness Vocabulary Gate, and downstream Story 4.6-4.7 / Epic 5-6 boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR73 (adopter conformance tests) and FR74 (documented tenant binding, Party identity, idempotency, error envelope, freshness, publication, governance behavior).
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on the reserved `Conformance`/`Conformance.Tests`/`tests/fixtures` structure, requirements-to-structure mapping (FR70-FR80, FR81-FR94), service/data boundaries, API response formats/naming, conformance-test patterns, the Story Safety Rule, and ADR triggers.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`, including .NET 10 / SDK 10.0.300, central package management, Contracts/Client/Server/Testing boundaries, fail-closed tenant isolation, content-safety/logging rules, and the root-level-only submodule policy (also enforced by `CLAUDE.md` and AC4).
  - Loaded previous Stories 3.7 and 4.1-4.4, the current sprint status, readiness gates/decisions, README/package docs, existing error/compatibility/diagnostics/testing source, the `BuyerAcceptanceDemoFixtures` synthetic-fixture pattern, the contract test safety net, the test-summary evidence format, the solution file, and recent git history.
  - Checked official Microsoft documentation for `System.Text.Json` deserialization behavior, `dotnet test --filter` selection, and .NET library logging guidance.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to the existing error catalog, compatibility metadata, CORE precondition catalog/diagnostics, synthetic `Testing` fixtures, and the reserved `Conformance.Tests` project rather than new parallel models.
  - Added explicit guardrails for shared trust/freshness + readiness vocabulary, machine-readable CI output, requirement/precondition/release-gate traceability, content-safe/side-channel-equivalent cross-tenant coverage, fail-closed reuse, no-nested-submodule execution, ContractSamples registration, and minimal adopter docs.
  - Added likely new/updated file lists (including the new test project and slnx registration), targeted/full validation commands, package validation, official Microsoft documentation references, previous-story learnings, and explicit out-of-scope boundaries (notably no Epic 5 manifest/signing/waiver surface; Story 5.10 carry-forward).
  - Kept Story 4.6 caller metadata, Story 4.7 integration guide, raw HTTP fallback, Epic 5 release signing/manifest/waivers, and Epic 6 telemetry/Admin UI out of scope.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture guardrails, test requirements, latest technical references, and explicit scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.
