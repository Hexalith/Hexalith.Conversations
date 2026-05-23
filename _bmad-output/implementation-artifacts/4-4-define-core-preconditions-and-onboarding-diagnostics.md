# Story 4.4: Define CORE Preconditions and Onboarding Diagnostics

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want explicit CORE preconditions and onboarding diagnostics,
so that I can know whether my environment is ready before relying on Conversations behavior.

## Acceptance Criteria

1. CORE preconditions are documented with safe-failure behavior
   - Given an adopter prepares an integration,
   - When they review CORE preconditions,
   - Then documentation identifies required tenant projection freshness, audit sink availability, supported schema versions, contract compatibility, Party identity validation, idempotency key behavior, projection subscription health, and required configuration,
   - And each precondition explains the safe failure behavior when unmet.

2. Onboarding diagnostics return actionable, content-safe status
   - Given onboarding diagnostics run,
   - When tenant context, contract version, provider configuration, projection subscription, schema compatibility, audit availability, and Parties integration checks are evaluated,
   - Then diagnostics return actionable status with machine-readable codes, safe messages, and remediation pointers,
   - And checks do not leak tenant data, Party data, conversation existence, provider payloads, or production secrets.

3. Unmet preconditions produce typed safe failures, never silent weakening
   - Given a CORE precondition is unknown, failing, stale, or unsupported,
   - When an adopter attempts a dependent command or query,
   - Then the system returns a typed safe precondition failure or degraded-read result as defined by policy,
   - And it does not silently continue in a mode that weakens tenant isolation, audit pairing, freshness, or schema compatibility.

4. Diagnostic tests prove readiness signals and content safety
   - Given diagnostic tests run,
   - When ready, missing tenant context, stale tenant projection, audit sink unavailable, unsupported contract, missing provider config, projection subscription failure, and schema incompatibility scenarios are exercised,
   - Then tests prove accurate readiness signals, safe remediation guidance, and content-safe diagnostic output.

## Tasks / Subtasks

- [x] Confirm scope, readiness gates, and reuse existing primitives before implementation (AC: 1-4)
  - [x] Re-read `_bmad-output/implementation-artifacts/readiness-gates.md`; confirm `Projection freshness blocking semantics` and `Command availability metadata` (shared trust/freshness vocabulary) remain `decided` or `waived`. Story 4.4 is blocked otherwise.
  - [x] Treat the shared trust/freshness vocabulary as canonical: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted` from `ProjectionTrustState`. Only `Current` is trust-bearing. Do not invent diagnostic-only synonyms such as `ready`, `ok`, `healthy`, or `maybe`.
  - [x] Reuse the existing typed error catalog (`ConversationErrorCatalog`, `ConversationError`, `ConversationErrorCode`, `ConversationErrorClientAction`) from Story 4.3 for typed precondition failures and remediation. Do not create a parallel error envelope.
  - [x] Reuse `ConversationContractCompatibility.Current` / `Evaluate(...)` from Story 4.1 for the schema/contract-compatibility diagnostic instead of re-deriving version checks.
  - [x] Model the diagnostics orchestrator on the read-only Story 3.6 pattern in `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs`: a server-owned service that fails closed on missing tenant/caller authority, returns one typed run result composed of per-check results with machine-readable codes, classifications, and bounded remediation, and never leaks protected detail.

- [x] Define the CORE precondition + diagnostics contract surface (AC: 1, 2, 3)
  - [x] Add diagnostics result contracts under `src/Hexalith.Conversations.Contracts/Diagnostics` (new folder) using `SchemaVersion.Current` and closed-vocabulary types. Suggested types: `OnboardingDiagnosticRunResultV1`, `OnboardingDiagnosticCheckResultV1`, a closed `OnboardingDiagnosticCheck` vocabulary (tenant-context, contract-version, provider-configuration, projection-subscription, schema-compatibility, audit-availability, parties-integration), a closed `OnboardingDiagnosticStatus` vocabulary mapped to trust/freshness language (e.g. ready, degraded, blocked, unknown — but justify each value against the shared vocabulary gate and keep it bounded), and a `CorePreconditionV1` descriptor (precondition id, required state, safe-failure behavior, remediation pointer).
  - [x] Carry only structured, content-safe data: check id, status code, safe message, remediation guidance code, documentation URI, correlation id, and optional audit handle where allowed. Do not put tenant IDs, Party IDs, conversation IDs/existence, provider session/payload values, business-reference values, raw exception text, local file paths, or production secrets in any diagnostic field.
  - [x] Add a contract-owned catalog of CORE preconditions that documents required tenant projection freshness, audit sink availability, supported schema versions, contract compatibility, Party identity validation, idempotency key behavior, projection subscription health, and required configuration, plus the safe-failure behavior for each (AC1). Prefer a single source consumed by both docs and tests.
  - [x] Register all new diagnostics/precondition contracts in `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` so they participate in serialization, forbidden-surface, and content-safety scans. Add converters under `Serialization` only when closed vocabularies require them, following `ClosedVocabularyJsonConverters.cs`.
  - [x] Keep `Hexalith.Conversations.Contracts` infrastructure-free: no references to EventStore, Tenants, Parties, FrontComposer, ASP.NET Core, Dapr, server, client, or UI packages.

- [x] Implement a server-owned, read-only diagnostics service (AC: 2, 3)
  - [x] Add the orchestrator under `src/Hexalith.Conversations.Server/Diagnostics` (new folder; the architecture reserves `Server/Diagnostics` for FR95-FR99/FR70-FR80 diagnostics). Suggested type: `ConversationOnboardingDiagnosticsService`.
  - [x] Fail closed on missing/invalid trusted tenant context and caller authority before any tenant data is touched, mirroring `ConversationGovernanceVerificationService.VerifyAsync(...)`. Use `IConversationTenantAccessService` / `ConversationTenantAccessGuard` for tenant context and `ConversationTenantProjectionHealth` / `IConversationTenantProjectionSignal` for projection-subscription/freshness signals.
  - [x] Evaluate each diagnostic check from existing trusted signals only:
    - Tenant context: trusted tenant binding present and access allowed (fail-closed denial maps to `tenant_binding_missing` / hidden-equivalent status, not an existence disclosure).
    - Projection subscription / freshness: derive from `ConversationTenantProjectionHealth` and `ProjectionTrustState`; `Stale`/`Rebuilding`/`Unavailable` map to bounded degraded/blocked statuses with safe remediation.
    - Schema compatibility + contract version: delegate to `ConversationContractCompatibility.Evaluate(...)`; unsupported/invalid map to the catalog versioning error.
    - Audit availability: use existing governance audit status signals (`Server/Governance`) without leaking audit content.
    - Parties integration: use `IParticipantDirectory` / `ParticipantDirectoryValidationStatus` availability signal; `participant_validation_unavailable` is retryable.
    - Provider configuration: report a bounded configuration-gap status with no provider payload, prompt, response, or secret values.
  - [x] Map every failing/degraded/unknown check to a typed `ConversationError` via the shared catalog (code, category, retryability, client action, safe message, documentation pointer) so REST, .NET client, and conformance surfaces stay consistent.
  - [x] Preserve side-channel equivalence: denied, missing, cross-tenant, or unauthorized diagnostic requests must not reveal whether a protected tenant or conversation exists. Keep `Program.cs` fail-closed; expose diagnostics through an opt-in server API extension only if needed, following the `ConversationReadApi` / `ConversationCommandApi` guard pattern (`/api/v1/...`, camelCase route params, claims-derived tenant scope).

- [x] Enforce typed safe precondition failures on dependent operations (AC: 3)
  - [x] Where a dependent command/query path already exists (e.g. `ConversationCommandApi`, `ConversationReadApi`, command handlers), confirm that an unknown/failing/stale/unsupported CORE precondition already yields a typed safe failure or degraded-read result and never silently weakens tenant isolation, audit pairing, freshness, or schema compatibility. Add coverage proving this; only add new gating if a real gap exists, and stop for ADR before introducing new durable state or a new runtime gate semantic.
  - [x] Reuse existing fail-closed behavior (tenant access guard, freshness gate, audit pairing gate, idempotency executor); do not duplicate or relax these checks inside the diagnostics path.

- [x] Add focused tests across contracts and server (AC: 1-4)
  - [x] Contract tests: serialization with `JsonSerializerDefaults.Web`, closed-vocabulary parse/round-trip, additive-JSON tolerance, descriptor coverage for every check and precondition, and forbidden-surface/content-safety scans for all new fields.
  - [x] Server tests under `tests/Hexalith.Conversations.Server.Tests` using existing fakes (tenant access, projection health, participant directory, temporal source) for: ready, missing tenant context, stale tenant projection, audit sink unavailable, unsupported contract, missing provider config, projection subscription failure, and schema incompatibility (AC4).
  - [x] Assert each scenario returns accurate status, the correct catalog code/action/remediation, and content-safe output. Scan serialized diagnostic results and any client-visible/exception text for forbidden fragments: tenant IDs, Party IDs, conversation IDs, conversation existence hints, provider session/payload, business-reference values, redacted text, `EventStore`, `stream`, `snapshot`, `envelope`, `SignalR`, `subscription`, `handler`, `dispatcher`, `repository`, `store`, raw exception text, `C:\`, and `D:\`. (Closed-vocabulary tokens such as `projection-subscription` are safe machine identifiers; the leakage scan targets free-text and protected-value disclosure.)
  - [x] If any logging is added, use source-generated logging or static templates with semantic placeholders; never interpolate raw error text, secrets, or payload values.
  - [x] Run targeted tests first, then the full solution:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Diagnostic|FullyQualifiedName~Precondition|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~Versioning"`
    - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Diagnostic|FullyQualifiedName~Onboarding|FullyQualifiedName~TenantAccess|FullyQualifiedName~Governance"`
    - `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation`
    - `dotnet test Hexalith.Conversations.slnx`
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 4.4 evidence after implementation.

- [x] Document CORE preconditions for adopters minimally (AC: 1)
  - [x] Update `README.md` and/or `src/Hexalith.Conversations.Contracts/README.md` with a compact CORE preconditions table: precondition, required state, safe-failure behavior, and documentation/remediation pointer. Keep examples at the contract/client level, not raw HTTP fallback.
  - [x] Do not document raw server routes, EventStore mechanics, internal handler names, storage keys, projection topology, provider payloads, secrets, or production exception samples. The full developer integration guide remains Story 4.7.

- [x] Preserve scope boundaries and stop conditions (AC: 1-4)
  - [x] Do not implement Story 4.5 adopter conformance package, CORE fixture runner, or CI result schema.
  - [x] Do not implement Story 4.6 caller-metadata capture or Story 4.7 full developer integration guide / DocFX / expanded examples.
  - [x] Do not implement Epic 5 release signing, named waivers, deprecation policy publication, versioned conformance manifest, or release-gate evidence aggregation. Do not implement Epic 6 operational telemetry dashboards.
  - [x] Stop for ADR/architecture review before adding new durable state, a new public error/status/freshness vocabulary outside what the shared gates allow, a new runtime gate semantic, a globally-runnable host, or any degraded/fail-open behavior.

## Dev Notes

### Epic and Business Context

- Epic 4 makes adopter integration credible through a contract package, a supported .NET client, compatibility discovery, typed sanitized errors, onboarding diagnostics, remediation guidance, and CORE precondition documentation. Story 4.4 delivers the CORE preconditions and onboarding diagnostics. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`]
- Story 4.4 covers FR77 and FR79: actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities; plus adopter-facing CORE preconditions including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.4: Define CORE Preconditions and Onboarding Diagnostics`; `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`]
- Degraded-mode and compliance-blocker messages must avoid ambiguous or panic-inducing language; adopters must be able to tell whether data is safe, stale, hidden, unavailable, or awaiting governance action. Diagnostics must use safe, bounded language and the shared trust/freshness vocabulary. [Source: `_bmad-output/planning-artifacts/prd.md#NFR77`]

### Readiness Gate Context

- `Projection freshness blocking semantics` is `decided` and blocks Story 4.4. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted`; default accepted trust-bearing state is `Current` only. Diagnostics must classify projection-subscription/freshness using exactly this vocabulary. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#projection-freshness-blocking-semantics`]
- `Command availability metadata` is `decided`: server-owned metadata controls eligibility, disabled state, required permission, precondition, risk level, freshness, audit requirement, and blocked reason. Use this shared, server-owned trust/freshness vocabulary; do not infer readiness from cache age, HTTP status alone, or missing fields. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#command-availability-metadata`]
- `Party hydration degraded states` is `decided`: writes fail closed on Party validation failure; authorized reads may degrade display hydration with a safe unresolved/unavailable state. The Parties-integration diagnostic must reflect this without disclosing personal data. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#party-hydration-degraded-states`]

### Current Implementation State

- `ConversationGovernanceVerificationService` (Story 3.6) is the closest existing pattern for Story 4.4: a server-owned, read-only orchestrator that fails closed on missing trusted tenant/caller authority, runs multiple checks, returns a single typed run result built from per-check results carrying check name, machine-readable status/classification, safe detail, bounded remediation, and an optional safe evidence handle. Mirror this shape for onboarding diagnostics. [Source: `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs`; `src/Hexalith.Conversations.Contracts/Governance/ConversationGovernanceVerificationContracts.cs`]
- `ConversationContractCompatibility.Current` and `Evaluate(ContractCompatibilityRequest)` already produce content-safe, machine-readable compatibility status (`supported`/`deprecated`/`unsupported`/`invalid`), bounded remediation codes, and a typed `ConversationError` for invalid/unsupported versions. Delegate the schema-compatibility and contract-version diagnostics here. [Source: `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`]
- The typed error system (Story 4.3) is the canonical envelope: `ConversationErrorCatalog.Get(code)`/`CreateError(...)`, `ConversationError` with `Code`, `Category`, `IsRetryable`, `ClientAction`, `SafeMessage`, `CorrelationId`, optional `AuditHandle`, optional `Documentation`, optional `SafeFieldDiagnostics`, plus a free-text safety blocklist. Existing codes already cover most CORE preconditions: `tenant_binding_missing`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `schema_version_unsupported`, `participant_validation_unavailable`, `tenant_context_mismatch`, `idempotency_key_missing`. Reuse these; only propose a new code (e.g. for a provider-configuration gap) via the catalog with full descriptor coverage, and stop for ADR before changing the public error taxonomy. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorClientAction.cs`]
- Tenant access fail-closed primitives already exist: `IConversationTenantAccessService` / `ConversationTenantAccessService`, `ConversationTenantAccessGuard`, `ConversationTenantAccessDecision`, `ConversationTenantAccessDenialReason`, and `ConversationTenantAccessRequirement`. Projection-subscription/freshness signals exist via `ConversationTenantProjectionHealth` (IsStale/HasGap/HasRollback/IsPoisoned/Version/Watermark) and `IConversationTenantProjectionSignal`. Reuse these for the tenant-context and projection-subscription diagnostics. [Source: `src/Hexalith.Conversations.Server/TenantAccess/`]
- Parties integration runs through `IParticipantDirectory` and `ParticipantDirectoryValidationStatus` / `ParticipantDirectoryValidation`; never call Parties from aggregate logic. Use the directory availability signal for the Parties-integration diagnostic. [Source: `src/Hexalith.Conversations.Server/Hydration/IParticipantDirectory.cs`; `src/Hexalith.Conversations.Server/Hydration/ParticipantDirectoryValidationStatus.cs`]
- Governance audit status signals exist under `Server/Governance` (`ConversationGovernanceAuditStatus`, `ConversationGovernanceAuditGate`, `ConversationGovernanceAuditResult`). Use these for the audit-availability diagnostic without exposing audit content. [Source: `src/Hexalith.Conversations.Server/Governance/`]
- `src/Hexalith.Conversations.Server/Program.cs` is intentionally fail-closed (`NotImplementedException`); API extensions are opt-in for hosts/tests. Do not make the host broadly runnable as part of this story. [Source: `src/Hexalith.Conversations.Server/Program.cs`]
- Existing contract tests (`ContractSamples.cs`, `ForbiddenPublicSurfaceTest.cs`, `ContractSerializationTest.cs`, `ContractPackageInventoryTest.cs`, `ContractMetadataTest.cs`) are the safety net. New diagnostics contracts MUST be registered in `ContractSamples.AllContracts` or they bypass forbidden-surface/content-safety scanning. [Source: `tests/Hexalith.Conversations.Contracts.Tests/`]

### Architecture and Contract Guardrails

- The architecture reserves `Server/Diagnostics` and `Conformance/Verification` for FR95-FR99 observability, and maps FR70-FR80 developer experience to `Contracts`, `Client`, `samples`, and adopter fixtures. Place diagnostic result contracts in `Contracts` and the orchestrator in `Server/Diagnostics`. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- `Contracts` defines commands, projections, events, typed errors, IDs, freshness/trust states, and schema versions; `Server/Api` maps HTTP to validated commands/queries; public APIs must not expose EventStore stream names, positions, snapshots, envelopes, or projection topology. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Security gates must be consistent across REST APIs, the typed .NET client, admin UI, MCP/tool operations, worker/rebuild jobs, and verification/conformance commands. The diagnostics path cannot bypass tenant access projection checks, command availability checks, or content-safe response shaping. [Source: `_bmad-output/planning-artifacts/architecture.md#Security Gate Consistency`]
- Tenant access fails closed when local tenant state is missing, stale, ambiguous, disabled, lagging, rolled back, deleted, or unavailable. Missing or invalid tenant context must never default to a tenant or global query, and failures must not reveal whether a protected conversation exists. [Source: `_bmad-output/planning-artifacts/architecture.md#Authorization Pattern`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Error/response formats use content-safe Problem Details or existing typed error contracts with stable code, category, retryability, correlation id, and safe documentation pointer; failure responses must not distinguish unauthorized from nonexistent cross-tenant resources unless an ADR permits it. [Source: `_bmad-output/planning-artifacts/architecture.md#API Response Formats`]
- Trust/freshness states use one shared vocabulary across API, client, UI, diagnostics, and evidence; do not invent diagnostics-only synonyms. If write/command endpoints or routes are added, follow plural lowercase versioned REST paths (`/api/v1/...`) with camelCase route params. [Source: `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#projection-freshness-blocking-semantics`]
- Any new durable state, new runtime service endpoint, public error/status taxonomy change, schema evolution rule change, or degraded/fail-open behavior triggers ADR review before implementation. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]

### File Structure Guidance

- Likely new contract files (under `src/Hexalith.Conversations.Contracts/Diagnostics`):
  - `OnboardingDiagnosticRunResultV1.cs`
  - `OnboardingDiagnosticCheckResultV1.cs`
  - `OnboardingDiagnosticCheck.cs` (closed vocabulary)
  - `OnboardingDiagnosticStatus.cs` (closed vocabulary, mapped to shared trust/freshness language)
  - `CorePreconditionV1.cs` and `ConversationCorePreconditionCatalog.cs`
- Likely new server files (under `src/Hexalith.Conversations.Server/Diagnostics`):
  - `ConversationOnboardingDiagnosticsService.cs`
  - `ConversationOnboardingDiagnosticsServiceCollectionExtensions.cs` (DI registration, if needed)
- Likely update files:
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (only if new closed vocabularies need converters)
  - `README.md` and/or `src/Hexalith.Conversations.Contracts/README.md`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` if the public file inventory changes.
- Likely new test files:
  - `tests/Hexalith.Conversations.Contracts.Tests/Diagnostics/OnboardingDiagnosticContractsTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationOnboardingDiagnosticsServiceTest.cs`
- Central Package Management is active. Any required package version belongs in `Directory.Packages.props`, never inline in `.csproj` files. Avoid new dependencies unless proven necessary. [Source: `Directory.Packages.props`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]

### Testing Requirements

- Contract tests must prove closed vocabulary, `JsonSerializerDefaults.Web` serialization, additive-JSON tolerance, descriptor coverage for every diagnostic check and CORE precondition, safe messages, bounded remediation codes, HTTPS documentation pointers, and forbidden-surface safety.
- Server diagnostics tests must cover all AC4 scenarios with deterministic fakes (no live server, no sleeps, no external services): ready, missing tenant context, stale tenant projection, audit sink unavailable, unsupported contract, missing provider config, projection subscription failure, schema incompatibility.
- Side-channel tests must prove denied/missing/cross-tenant diagnostic requests do not reveal protected tenant or conversation existence and stay equivalent to the hidden/unavailable shape used elsewhere.
- Leakage tests must scan serialized diagnostic output, any log-like diagnostics, and any exception/fallback text for the forbidden fragments listed in the tasks.
- Use xUnit v3, Shouldly, NSubstitute, and existing Tenants/Parties/EventStore testing helpers and fakes; reuse existing authorization test patterns rather than inventing new fakes. [Source: `_bmad-output/project-context.md#Testing Rules`]

### Previous Story Intelligence

- Story 4.1 created compatibility metadata and safe remediation pointers and fixed compatibility invariant enforcement (e.g. supported status must not carry remediation; unsupported/invalid must carry a typed error). Story 4.4's compatibility diagnostic must reuse `Evaluate(...)` and respect those invariants rather than re-deriving status. [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`]
- Story 4.2 implemented the supported .NET client and explicitly deferred onboarding diagnostics and CORE precondition checks to Story 4.4. Diagnostics should map consistently with the client's typed-result behavior so REST, client, and conformance surfaces agree. [Source: `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`]
- Story 4.3 centralized typed sanitized errors in `ConversationErrorCatalog`/`ConversationError` with `ClientAction`/`SafeMessage` fields and hardened free-text safety (rejecting tenant, Party, conversation, provider-session/payload, business-reference, local-path, and raw-exception markers). Reuse this catalog for all precondition failures; do not introduce a parallel error/remediation model and do not weaken the free-text guardrails. Story 4.3 also added QA coverage for handler-supplied audit-unavailable, stale-projection, participant/onboarding-unavailable, and provider-identity remediation errors at the command API boundary — keep those consistent. [Source: `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`]
- Recent commits are story-scoped and test-heavy: `feat(story-4.3): Expose typed sanitized errors and remediation guidance`, `feat(story-4.2): Provide supported .NET client happy path`, `feat(story-4.1): Add contract compatibility metadata`. Continue with focused red tests and boundary/content-safety checks before full-solution validation. [Source: `git log --oneline -5`]

### Latest Technical Notes

- Microsoft library logging guidance recommends source-generated logging and warns against string interpolation; if diagnostics emit logs, use static templates/source-generated logging and never log raw error payloads, protected IDs, provider payloads, secrets, or exception detail as client-visible diagnostics. [Source: `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`]
- ASP.NET Core 10 supports content-safe Problem Details via `AddProblemDetails()` / `IProblemDetailsService`; if any diagnostics endpoint is exposed, keep Conversations-safe typed fields canonical and never surface stack traces, route internals, exception messages, endpoint metadata, or the developer exception page to adopters. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0#problem-details`]
- `System.Text.Json` supports records/immutable types; new closed-vocabulary diagnostic contracts should follow the existing `JsonSerializerDefaults.Web` plus custom-converter pattern in `ClosedVocabularyJsonConverters.cs`. [Source: `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/deserialization#deserialization-behavior`]
- `dotnet test --filter` supports `FullyQualifiedName~...` and `|` composition for xUnit selection; run targeted filters first, then full-solution validation. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]

### Out of Scope

- No Story 4.5 adopter conformance package, CORE fixture runner, executable conformance runner, or CI result schema.
- No Story 4.6 caller-metadata capture, or Story 4.7 full developer integration guide, DocFX/API reference pipeline, expanded API examples, or raw HTTP public examples.
- No Epic 5 release signing, versioned conformance manifest, named-waiver lifecycle, deprecation policy publication, or release-gate evidence aggregation; no Epic 6 telemetry dashboards or Admin UI work.
- No new durable state, transcript tables, runtime health dashboard, FrontComposer surface, background worker, raw EventStore endpoint, or globally-runnable host.
- No new public error/status/freshness vocabulary outside the shared gates, and no degraded/fail-open behavior, without ADR approval.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 4.4: Define CORE Preconditions and Onboarding Diagnostics`
- `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`
- `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`
- `_bmad-output/planning-artifacts/prd.md#NFR77`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/architecture.md#Authorization Pattern`
- `_bmad-output/planning-artifacts/architecture.md#Security Gate Consistency`
- `_bmad-output/planning-artifacts/architecture.md#API Response Formats`
- `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`
- `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`
- `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`
- `_bmad-output/project-context.md`
- `README.md`
- `src/Hexalith.Conversations.Contracts/README.md`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorClientAction.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs`
- `src/Hexalith.Conversations.Contracts/Governance/ConversationGovernanceVerificationContracts.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessService.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantProjectionHealth.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`
- `src/Hexalith.Conversations.Server/Hydration/IParticipantDirectory.cs`
- `src/Hexalith.Conversations.Server/Hydration/ParticipantDirectoryValidationStatus.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditStatus.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `src/Hexalith.Conversations.Server/Program.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `Directory.Build.props`
- `Directory.Packages.props`
- `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`
- `https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0#problem-details`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context) — autonomous BMAD dev-story worker (YOLO mode).

### Debug Log References

- Initial content-safety blocklist was over-broad and collided with legitimate closed vocabulary (`subscription`, `party-`, `tenant-`). Resolved by reusing the canonical `ConversationError.EnsureContentSafe` free-text guardrail for free-text fields, constraining tokens to the closed token charset (which already excludes `:`/`\`/`/`), and renaming colliding precondition IDs (`projection-freshness`, `participant-identity-validation`) and the Parties remediation code so legitimate diagnostic vocabulary stays valid while protected values cannot pass.
- Forbidden-fragment leakage scans were scoped to free-text and protected-value disclosure rather than the closed-vocabulary tokens (e.g. `projection-subscription`), which are safe machine identifiers.

### Completion Notes List

- AC1: Added a contract-owned `ConversationCorePreconditionCatalog` documenting all eight required CORE preconditions (projection freshness, audit sink availability, supported schema versions, contract compatibility, participant identity validation, idempotency key behavior, projection subscription health, required configuration), each with required trust-bearing `Current` state, a safe-failure description, and a typed unmet error code reused from the shared `ConversationErrorCatalog`. The same catalog backs both the README table and the contract tests.
- AC2: Added a read-only `ConversationOnboardingDiagnosticsService` (Server/Diagnostics) modeled on the Story 3.6 governance verification orchestrator. It binds tenant/caller authority from the trusted boundary, evaluates the closed `OnboardingDiagnosticCheck` set from existing trusted signals (tenant access, `ConversationTenantProjectionHealth`, `ConversationContractCompatibility.Evaluate`, audit availability, participant directory availability, provider configuration), and returns one `OnboardingDiagnosticRunResultV1` with machine-readable codes, safe messages, bounded remediation, and HTTPS documentation pointers. No tenant/Party/conversation/provider/secret detail is carried.
- AC3: Failing/degraded/unknown checks map to typed `ConversationError` values via the shared catalog; the diagnostics path reuses (does not duplicate or relax) the existing fail-closed tenant access guard, freshness gate, audit pairing, and idempotency executor. A dependency-boundary test proves the service does not depend on mutation execution boundaries. No new durable state, runtime gate semantic, public vocabulary, or globally-runnable host was added (no ADR trigger crossed).
- AC4: Server tests exercise ready, missing tenant context, denied-access side-channel equivalence, stale tenant projection, projection subscription failure, audit sink unavailable, unsupported contract, schema incompatibility, missing provider config, Parties integration unavailable, and throwing-signal fail-closed behavior, each asserting accurate status, the correct catalog code, and content-safe output.
- Closed vocabularies use the shared trust/freshness language (`ready`/`degraded`/`blocked`/`unknown` mapped to `Current`/`Stale`-`Rebuilding`/`Unavailable`/hidden); no diagnostics-only synonyms were introduced.
- Default DI signals fail closed (audit unavailable, directory unavailable, provider config missing) and log once, so production must wire trust-bearing signals before relying on diagnostics.
- Tests: targeted Contracts 61 passed; targeted diagnostics server 16 passed; contracts package pack succeeded; full solution `dotnet test Hexalith.Conversations.slnx` passed — Client 23, Integration 8, Core 139, Server 403, Contracts 298 (871 total).

### File List

- `src/Hexalith.Conversations.Contracts/Diagnostics/OnboardingDiagnosticVocabulary.cs` (new)
- `src/Hexalith.Conversations.Contracts/Diagnostics/DiagnosticContractValidation.cs` (new)
- `src/Hexalith.Conversations.Contracts/Diagnostics/OnboardingDiagnosticCheckResultV1.cs` (new)
- `src/Hexalith.Conversations.Contracts/Diagnostics/OnboardingDiagnosticRunResultV1.cs` (new)
- `src/Hexalith.Conversations.Contracts/Diagnostics/CorePreconditionV1.cs` (new)
- `src/Hexalith.Conversations.Contracts/Diagnostics/ConversationCorePreconditionCatalog.cs` (new)
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (modified — added diagnostic check/status converters)
- `src/Hexalith.Conversations.Contracts/README.md` (modified — CORE preconditions and diagnostics section)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationOnboardingDiagnosticsService.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/IConversationOnboardingDiagnosticSignals.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/DefaultConversationOnboardingDiagnosticSignals.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationOnboardingDiagnosticsServiceCollectionExtensions.cs` (new)
- `tests/Hexalith.Conversations.Contracts.Tests/Diagnostics/OnboardingDiagnosticContractsTest.cs` (new)
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (modified — registered diagnostic/precondition contracts)
- `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationOnboardingDiagnosticsServiceTest.cs` (new)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified — Story 4.4 evidence)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — 4.4 status)

## Story Context Validation

- Checklist reviewed: `.claude/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 4, Story 4.4 ACs and Ready-for-Dev preconditions, and downstream Story 4.5-4.7 / Epic 5-6 boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR77, FR79, FR70-FR80 developer experience commitments, and NFR77 safe-language requirements.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on architectural boundaries, requirements-to-structure mapping (`Server/Diagnostics`, `Conformance/Verification`), authorization pattern, security gate consistency, API response formats/naming, and ADR triggers.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`, including .NET 10, central package management, Contracts/Client/Server boundaries, fail-closed tenant isolation, Parties/Tenants/Audit rules, logging safety, and root-level submodule policy.
  - Loaded previous Stories 4.1-4.3, current sprint status, readiness gates/decisions, README/package docs, existing compatibility/error/tenant-access/projection-health/participant-directory/governance source, the Story 3.6 governance verification service as the orchestrator pattern, existing contract tests, and recent git history.
  - Checked official Microsoft documentation for ASP.NET Core 10 Problem Details, .NET library logging guidance, `System.Text.Json` deserialization behavior, and `dotnet test --filter`.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to the existing error catalog, compatibility metadata, tenant-access/projection-health/participant-directory/governance signals, and the Story 3.6 read-only orchestrator pattern instead of new parallel models.
  - Added explicit guardrails for shared trust/freshness vocabulary, content-safe diagnostic output, side-channel equivalence, fail-closed authority binding, fail-closed `Program.cs`, ContractSamples registration, and minimal adopter docs.
  - Added likely new/updated file lists, targeted/full validation commands, package validation, official Microsoft documentation references, previous-story learnings, and explicit out-of-scope boundaries.
  - Kept Story 4.5 conformance package, Story 4.6 caller metadata, Story 4.7 integration guide, Epic 5 release signing/waivers/manifest, and Epic 6 telemetry/Admin UI out of scope.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture guardrails, test requirements, latest technical references, and explicit scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Senior Developer Review (AI)

- Reviewer: Jérôme Piquot on 2026-05-23 (autonomous adversarial code review).
- Outcome: Approve. 0 CRITICAL issues. All four acceptance criteria are implemented and proven by tests; every `[x]` task maps to real, verified code.
- AC validation: AC1 (CORE precondition catalog with safe-failure behavior) — `ConversationCorePreconditionCatalog` documents all eight required preconditions, each reusing the shared `ConversationErrorCatalog` code and trust-bearing `Current` state; the Contracts README table is sourced from the same catalog. AC2 (actionable, content-safe diagnostics) — `ConversationOnboardingDiagnosticsService` evaluates the closed check set from existing trusted signals and returns one typed run result; content-safety scans pass. AC3 (typed safe failures, no silent weakening) — every non-ready check carries a typed `ConversationError`; the service reuses the existing fail-closed tenant-access guard rather than duplicating or relaxing it. AC4 (diagnostic tests) — all eight required scenarios plus deprecated/invalid/gap/rollback/throwing/cancellation edge cases are covered.
- Reuse confirmed: Story 4.1 `ConversationContractCompatibility.Evaluate`, Story 4.3 `ConversationErrorCatalog`/`ConversationError`, and the Story 3.6 governance verification orchestrator shape are all reused; no parallel error, status, or freshness vocabulary was introduced. Contracts project stays infrastructure-free.
- Side-channel finding (addressed): the production `ConversationTenantAccessService` fails closed on stale/gap/rollback/poisoned/unavailable projection state, and the platform contract (`ConversationTenantAccessDecision.ToRejection`) requires those outcomes to stay externally indistinguishable from unauthorized/missing requests. The orchestrator already collapses any access denial to the hidden `unknown` shape, which is correct and preserves side-channel equivalence. The original tests only modeled an authorization-style denial; added a production-faithful `[Theory]` proving freshness/availability denials collapse to the same hidden `unknown` result with no tenant-existence or freshness disclosure. No runtime gate semantic was changed (no ADR trigger crossed).
- Minor (non-blocking): `EvaluateTenantContext` accepts an unused `correlationId` parameter; left as-is for signature symmetry with the other evaluators.

## Change Log

- 2026-05-23: Created Story 4.4 context from Epic 4 requirements, PRD/architecture/readiness/project context, current compatibility/error/tenant-access/projection/governance source, the Story 3.6 governance verification orchestrator pattern, Stories 4.1-4.3 learnings, recent git history, and official Microsoft documentation.
- 2026-05-23: Implemented CORE preconditions and onboarding diagnostics. Added contract-owned diagnostic result contracts, closed `OnboardingDiagnosticCheck`/`OnboardingDiagnosticStatus` vocabularies (mapped to the shared trust/freshness language), `CorePreconditionV1`, and `ConversationCorePreconditionCatalog` reusing the shared `ConversationErrorCatalog`; added a read-only `ConversationOnboardingDiagnosticsService` modeled on the Story 3.6 orchestrator that fails closed on missing/denied authority and derives every check from existing trusted signals; reused the existing fail-closed dependent gating without duplication; documented preconditions in the Contracts README; and added contract and server tests. Full solution validation passed (871 tests). Status set to review.
- 2026-05-23: Senior Developer Review (AI). 0 CRITICAL issues. Added a production-faithful side-channel `[Theory]` to `ConversationOnboardingDiagnosticsServiceTest` proving freshness/availability access denials (stale/gap/rollback/poisoned/unavailable) collapse to the hidden `unknown` shape, externally indistinguishable from unauthorized/missing requests. Full solution validation re-run and passed (909 tests: Client 23, Integration 8, Core 139, Server 423, Contracts 316). Status set to done.
