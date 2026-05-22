# Story 4.3: Expose Typed Sanitized Errors and Remediation Guidance

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer,
I want typed sanitized errors with actionable remediation guidance,
so that I can handle failures safely without exposing protected conversation data.

## Acceptance Criteria

1. Error responses expose complete typed, safe semantics
   - Given a command, query, client call, or compatibility check fails,
   - When the error response is created,
   - Then it includes machine-readable code, category, retryability, client action, safe message, correlation ID, audit handle where allowed, and documentation pointer,
   - And it excludes target tenant identifiers, inaccessible Party IDs, conversation existence, redacted content, provider payloads, raw business references, and protected operational details.

2. Remediation guidance is bounded, predictable, and non-disclosing
   - Given failures are caused by unsupported schemas, missing preconditions, failed verification, tenant binding, stale projection, audit unavailability, provider configuration gaps, or projection subscription failure,
   - When remediation guidance is returned,
   - Then the guidance identifies the failure class and next safe action without leaking protected details,
   - And machine-readable codes allow adopter applications to branch predictably.

3. REST, .NET client, and compatibility surfaces stay consistent
   - Given the same failure can occur through REST, .NET client, or conformance tooling,
   - When the failure is surfaced,
   - Then typed error semantics remain consistent across integration paths,
   - And documentation examples use the same codes and safe message shape.

4. Tests prove typed semantics and leakage safety
   - Given error tests run,
   - When invalid command, unauthorized access, nonexistent or cross-tenant record, unsupported version, stale projection, audit unavailable, provider configuration gap, and onboarding failure scenarios are exercised,
   - Then tests prove typed semantics, remediation mapping, content-safe responses, and no leakage through logs, traces, diagnostics, or client exceptions.

## Tasks / Subtasks

- [x] Confirm scope, readiness decisions, and existing taxonomy before implementation (AC: 1-4)
  - [x] Re-read `_bmad-output/implementation-artifacts/readiness-gates.md`; confirm `.NET client versus raw HTTP fallback policy`, `Projection freshness blocking semantics`, `Command availability metadata`, and `Party hydration degraded states` are still `decided`.
  - [x] Preserve Story 4.2's supported path: shared `Contracts` plus `Hexalith.Conversations.Client`; do not add adopter-facing raw HTTP fallback examples unless a later buyer approval or diagnostics-only exception is recorded.
  - [x] Treat existing `ConversationError`, `ConversationErrorResult`, `ConversationErrorCode`, and `ConversationErrorCategory` as the base taxonomy. Do not create a parallel error envelope.

- [x] Complete the public error contract without breaking existing callers unnecessarily (AC: 1, 2)
  - [x] Add missing typed fields to the existing contract surface, preferably additive optional fields on `ConversationError`: a closed-vocabulary client action and a safe adopter-facing message.
  - [x] Add a closed-vocabulary type such as `ConversationErrorClientAction` or `ConversationRemediationAction` under `src/Hexalith.Conversations.Contracts/Errors`.
  - [x] Keep structured data only: action codes, safe message templates, documentation URI, correlation ID, optional audit handle, and field diagnostics. Do not put tenant IDs, Party IDs, conversation IDs, provider session IDs, business reference values, exception text, local paths, or operational topology in error payloads.
  - [x] Keep `DeveloperGuidance` if needed for backward compatibility, but make the new safe action/message fields the canonical adopter branch surface.
  - [x] Ensure every current code has a descriptor: `tenant_binding_missing`, `tenant_isolation_violation`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `idempotency_conflict`, `idempotency_outcome_unknown`, `idempotency_key_missing`, `aggregate_not_found`, `schema_version_unsupported`, `command_validation_failed`, `duplicate_participant`, `unsupported_participant`, `participant_validation_unavailable`, `tenant_context_mismatch`, and `provider_only_identity_forbidden`.

- [x] Centralize error descriptors and mapping (AC: 1-3)
  - [x] Add a contract-owned catalog/factory, for example `ConversationErrorDescriptor` plus `ConversationErrorCatalog`, that maps each code to category, retryability, client action, safe message, default documentation pointer, and audit-handle allowance.
  - [x] Make `ConversationErrorCode.IsRetryable(...)` agree with the catalog, or collapse retryability lookup into one source and update tests.
  - [x] Use absolute HTTPS documentation pointers. Keep existing placeholder docs host only if that is already accepted locally; otherwise use a stable internal docs URI shape without raw paths.
  - [x] Ensure unknown or unsupported raw failures coarsen to a bounded typed outcome such as `idempotency_outcome_unknown` or `command_validation_failed`; never propagate raw server text.
  - [x] Do not encode HTTP status as the primary programmatic branch. Status can remain transport metadata; code/action/category are the contract.

- [x] Update REST/server mapping to use the shared error catalog (AC: 1-4)
  - [x] Refactor local `ErrorResult(...)` construction in `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs` to call the shared catalog/factory or a narrow server adapter over it.
  - [x] Preserve current fail-closed behavior in `src/Hexalith.Conversations.Server/Program.cs`; do not make the host broadly runnable as part of this story.
  - [x] Preserve `ConversationReadApi` side-channel behavior. Hidden, forbidden, malformed, nonexistent, and cross-tenant reads currently return hidden result shapes; do not convert them into distinguishable typed error bodies unless the hidden shape remains equivalent.
  - [x] If ASP.NET Core Problem Details is introduced for empty-body or unhandled-error fallback, keep `ConversationErrorResult` as the canonical Conversations contract or embed only safe Conversations fields in Problem Details extensions. Do not expose stack traces, route internals, exception messages, endpoint metadata, or development error pages to adopters.
  - [x] Add or update server tests for invalid command metadata, route/body mismatch, unauthenticated caller, missing tenant claim, tenant mismatch, handler-supplied idempotency conflict, audit unavailable, and malformed body.

- [x] Update the .NET client fallback and typed error parsing (AC: 1-3)
  - [x] Refactor `src/Hexalith.Conversations.Client/ConversationClient.cs` fallback methods to use the same code/category/retryability/action/message mapping as REST and compatibility checks.
  - [x] Preserve `ConversationClientResult<T>` as the public success/error wrapper; do not expose `HttpResponseMessage`, raw route names, server handler names, EventStore terms, or raw exception text.
  - [x] Keep non-seekable response-stream handling from Story 4.2.
  - [x] Add client tests proving typed fallback mapping for non-JSON 400/401/403/404/409/500 responses, timeout/unknown outcome, unsupported schema before send, and typed server error bodies with new action/message fields.

- [x] Update compatibility and future conformance-facing mapping (AC: 2, 3)
  - [x] Refactor `ConversationContractCompatibility.Evaluate(...)` to use the shared catalog for unsupported/invalid schema or package versions.
  - [x] Keep `ContractCompatibilityRemediation` guidance codes bounded; do not duplicate free-text remediation rules in compatibility code.
  - [x] Add test coverage proving compatibility errors and ordinary REST/client errors serialize with the same `ConversationError` shape.
  - [x] Prepare conformance tooling by making the catalog easy to consume from test projects, but do not implement the Story 4.5 conformance package.

- [x] Expand content-safety tests across contracts, client, server, and diagnostics (AC: 1-4)
  - [x] Extend `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs` so all new error fields reject or avoid unsafe fragments.
  - [x] Add contract tests for every code descriptor: code, category, retryability, action, safe message, documentation pointer, audit-handle allowance, JSON shape, closed-vocabulary parse behavior, and additive JSON tolerance.
  - [x] Add serialization scans for forbidden fragments: inaccessible tenant IDs, Party IDs, conversation IDs, redacted text, provider session references, provider payload, business reference values, `EventStore`, `stream`, `snapshot`, `envelope`, `SignalR`, `handler`, `dispatcher`, `repository`, `store`, raw exception text, `C:\`, and `D:\`.
  - [x] Add server/client tests that log or receive raw failures internally but assert client-visible bodies and exception/fallback messages stay content-safe.
  - [x] If any logging is added, use source-generated logging or static message templates with semantic placeholders; never interpolate raw error text or payload values.

- [x] Update adopter-facing documentation minimally (AC: 2, 3)
  - [x] Update `README.md` and `src/Hexalith.Conversations.Contracts/README.md` with a compact error table: code, category, retryability, client action, safe message intent, and documentation pointer.
  - [x] Keep examples at the contract/client level, not raw HTTP fallback.
  - [x] Do not document raw server routes, EventStore mechanics, internal handler names, storage keys, projection topology, or production exception samples.

- [x] Validate and record evidence (AC: 1-4)
  - [x] Run targeted contract tests:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Error|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~Versioning|FullyQualifiedName~ContractSerialization"`
  - [x] Run targeted client tests:
    - `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj`
  - [x] Run targeted server tests:
    - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~ConversationReadApi|FullyQualifiedName~Governance|FullyQualifiedName~Idempotency"`
  - [x] Pack contracts and client packages:
    - `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation`
    - `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation`
  - [x] Run full solution before closing:
    - `dotnet test Hexalith.Conversations.slnx`
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 4.3 evidence.

## Dev Notes

### Epic and Business Context

- Epic 4 is about making adopter integration credible through contracts, .NET client support, compatibility discovery, typed sanitized errors, diagnostics, conformance tests, and guidance. Story 4.3 covers FR78 and FR80: remediation guidance and typed sanitized error responses. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.3: Expose Typed Sanitized Errors and Remediation Guidance`; `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`]
- Diego's developer journey treats errors as part of the developer UX. Failed conformance or runtime failures should return typed remediation guidance and safe typed errors, not raw EventStore, tenant, Party, provider, or exception details. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Diego: Developer Create -> Append -> Read`]
- The PRD says typed errors are part of the public contract package and .NET client path. The error envelope must let adopters branch safely without learning EventStore internals or duplicating tenant checks. [Source: `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`; `_bmad-output/planning-artifacts/architecture.md#API Response Formats`]

### Current Implementation State

- `ConversationError` already exists and carries `SchemaVersion`, `Code`, `Category`, `IsRetryable`, `CorrelationId`, optional `AuditHandle`, optional `Documentation`, optional `SafeFieldDiagnostics`, and optional `DeveloperGuidance`. It has a best-effort unsafe-term blocklist for free-text fields. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`]
- `ConversationErrorCode` and `ConversationErrorCategory` are closed-vocabulary record types with JSON converters. `ConversationErrorCode.IsRetryable(...)` currently defines retryability for `tenant_projection_stale`, `participant_validation_unavailable`, `idempotency_outcome_unknown`, and `audit_sink_unavailable`. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCategory.cs`]
- `ConversationErrorResult` wraps one or more errors and validates non-empty, non-null lists. Preserve this wrapper unless an explicit architecture decision replaces it. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorResult.cs`]
- Compatibility metadata already has safe remediation codes and HTTPS documentation pointers via `ContractCompatibilityRemediation`; invalid and unsupported checks produce a `ConversationError`. Refactor this to the shared catalog instead of duplicating mapping. [Source: `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`]
- `ConversationCommandApi` currently has local `ErrorResult(...)` helpers that create typed errors for metadata validation, auth, tenant mismatch, route/body mismatch, and handler outcomes. Story 4.3 should remove mapping drift from those local helpers. [Source: `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`; `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs`]
- `ConversationReadApi` intentionally returns hidden/unavailable typed read results rather than error bodies for several denied/malformed/missing paths. Preserve side-channel equivalence; do not make unauthorized vs nonexistent records distinguishable. [Source: `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`; `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`]
- `ConversationClient` already maps typed server error bodies, unsupported schemas before send, idempotency conflict, tenant denial fallback, not found fallback, timeout/unknown outcome, and sanitized non-JSON server errors into `ConversationClientResult<T>`. Story 4.3 should harden these with shared catalog action/message fields. [Source: `src/Hexalith.Conversations.Client/ConversationClient.cs`; `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`]
- Existing contract tests already scan public surface and serialized fixtures for forbidden infrastructure and personal-data terms. Extend these instead of writing unrelated scanners. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`; `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`]

### Architecture and Contract Guardrails

- `Contracts` defines typed errors and must remain infrastructure-free. Do not reference ASP.NET Core, EventStore, Dapr, Tenants, Parties, FrontComposer, logging, or server packages from `Hexalith.Conversations.Contracts`. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/project-context.md#Critical Implementation Rules`]
- `Client` wraps public API contracts only and makes no domain decisions. It must not reference server internals, EventStore, Dapr, Tenants, Parties, FrontComposer, or UI packages. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Organization`; `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`]
- Public APIs expose Conversations commands, projections, results, version metadata, typed errors, and freshness state. They must not expose EventStore envelopes, stream names, sequence numbers, snapshots, SignalR groups, projection internals, handler names, storage terms, or raw route internals. [Source: `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`]
- Error response formats must use content-safe Problem Details or existing typed error contracts with stable code, category, retryability, correlation ID, and safe documentation pointer. Failure responses must not distinguish unauthorized from nonexistent cross-tenant resources unless an ADR explicitly permits it. [Source: `_bmad-output/planning-artifacts/architecture.md#API Response Formats`]
- Trust/freshness states share one vocabulary across API, client, UI, diagnostics, and evidence. Do not invent error-only synonyms for `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, or `Redacted`. [Source: `_bmad-output/planning-artifacts/architecture.md#Code Naming Conventions`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#projection-freshness-blocking-semantics`]
- Tenant access must fail closed before aggregate/projection access when tenant binding is missing, stale, ambiguous, disabled, lagging, rolled back, deleted, unavailable, or mismatched. Error details must not reveal whether the protected conversation exists. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`]

### File Structure Guidance

- Likely update files:
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCategory.cs`
  - `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
  - `src/Hexalith.Conversations.Client/ConversationClient.cs`
  - `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
  - `README.md`
  - `src/Hexalith.Conversations.Contracts/README.md`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs`
  - `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` if the public file inventory changes.
- Likely new files:
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorClientAction.cs`
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorDescriptor.cs`
  - `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationErrorCatalogTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Api/ConversationErrorMappingTest.cs` if mapping grows beyond command API tests.
- Avoid new package dependencies unless proven necessary. Central Package Management is active; any required version belongs in `Directory.Packages.props`, not inline in `.csproj` files. [Source: `Directory.Packages.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]

### Testing Requirements

- Contract tests must prove closed vocabulary, serialization, additive tolerance, descriptor coverage for every error code, safe messages, safe action values, HTTPS documentation pointers, and retryability consistency.
- Client tests must prove typed mapping from:
  - Unsupported schema before request send.
  - Typed server `ConversationErrorResult`.
  - Non-JSON 400/401/403/404/409/500 responses.
  - Timeout or unknown outcome.
  - Tenant denial fallback.
  - Idempotency conflict.
- Server tests must prove `ConversationCommandApi` uses the shared mapping for invalid/missing metadata, unauthenticated caller, missing tenant/caller claims, tenant mismatch, route/body mismatch, handler errors, audit unavailable, and idempotency conflict.
- Read API tests must preserve hidden/unavailable side-channel behavior. Do not add tests that expect cross-tenant reads to reveal typed failure details.
- Leakage tests must scan serialized errors, log-like diagnostics, and client fallback exceptions for the forbidden fragments listed in the tasks.
- Use xUnit v3, Shouldly, deterministic fake HTTP handlers, ASP.NET endpoint invocation patterns already present in tests, and no sleeps or external services.

### Previous Story Intelligence

- Story 4.1 created the compatibility metadata and safe remediation pointers. It found and fixed risks around compatibility invariant enforcement and package-specific contracts/client version evaluation. For Story 4.3, the analogous risk is drift between compatibility errors, REST errors, and client fallback errors. [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`; `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs`]
- Story 4.2 implemented the supported .NET client happy path and added opt-in command routes. It explicitly deferred broad typed error remediation expansion to Story 4.3. Build on `ConversationClient`, `ConversationCommandApi`, and the client/server tests rather than starting a new transport or error wrapper. [Source: `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`]
- Story 4.2 review fixed non-seekable HTTP response deserialization and tenant-denial fallback coverage. Keep those regression tests intact when refactoring client error handling. [Source: `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md#Senior Developer Review (AI)`]
- Recent commits are story-scoped and test-heavy: `feat(story-4.2): Provide supported .NET client happy path`, `feat(story-4.1): Add contract compatibility metadata`, and Epic 3 governance/acceptance stories. Continue with focused red tests and boundary checks before full-solution validation. [Source: `git log --oneline -5`]

### Latest Technical Notes

- ASP.NET Core 10 supports `IProblemDetailsService` and `AddProblemDetails()` for generating problem details responses for empty client/server error responses, with customization through `ProblemDetailsOptions.CustomizeProblemDetails`, custom `IProblemDetailsWriter`, or `IProblemDetailsService`. If used, configure it so Conversations-safe typed fields are emitted and raw framework details are not. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0#problem-details`]
- Microsoft documents that the Developer Exception Page can include stack traces, query string parameters, cookies, headers, and endpoint metadata, and warns not to expose detailed exception information publicly. Story 4.3 tests should protect against these details surfacing through production error paths. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0#developer-exception-page`]
- ASP.NET Core Minimal API `TypedResults` can improve unit testing and OpenAPI metadata, but `Results<T1,TN>` signatures become more verbose when multiple result types are returned. Use this only if it clarifies the server adapter; do not churn existing route code for style alone. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0#typedresults-vs-results`]
- Microsoft library logging guidance recommends source-generated logging for most library scenarios and warns against string interpolation in logging. If this story touches logs, use static templates/source-generated logging and never log raw error payloads, protected IDs, provider payloads, or exception detail as client-visible diagnostics. [Source: `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`]

### Out of Scope

- No Story 4.4 onboarding diagnostics runner, CORE precondition scanner, provider configuration checker, projection subscription diagnostic service, or diagnostic wizard.
- No Story 4.5 adopter conformance package or public conformance fixture runner.
- No Story 4.7 full developer integration guide, DocFX/API reference pipeline, expanded samples, or raw HTTP public examples.
- No Epic 5 release signing, versioned conformance manifest, named waiver lifecycle, deprecation policy publication, or release-gate aggregation.
- No new durable state, direct EventStore API exposure, raw server exception envelope, telemetry dashboard, Admin UI, FrontComposer work, browser UX, provider integration, or new transport protocol.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 4.3: Expose Typed Sanitized Errors and Remediation Guidance`
- `_bmad-output/planning-artifacts/prd.md#Consumer Contracts And Developer Experience`
- `_bmad-output/planning-artifacts/prd.md#Tenant Access And Isolation`
- `_bmad-output/planning-artifacts/architecture.md#API Response Formats`
- `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Diego: Developer Create -> Append -> Read`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`
- `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`
- `_bmad-output/project-context.md`
- `README.md`
- `src/Hexalith.Conversations.Contracts/README.md`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCategory.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorResult.cs`
- `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs`
- `src/Hexalith.Conversations.Client/ConversationClient.cs`
- `src/Hexalith.Conversations.Client/ConversationClientResult.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs`
- `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs`
- `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`
- `https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0#problem-details`
- `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0#typedresults-vs-results`
- `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Confirmed Story 4.3 readiness gates remain `decided`: `.NET client versus raw HTTP fallback policy`, `Projection freshness blocking semantics`, `Command availability metadata`, and `Party hydration degraded states`.
- 2026-05-22: Verified shared `ConversationError`/`ConversationErrorResult` taxonomy remained the canonical envelope; added shared catalog/action/message fields without introducing a parallel error body.
- 2026-05-22: Targeted contract validation passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Error|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~Versioning|FullyQualifiedName~ContractSerialization"` - 49 passed.
- 2026-05-22: Targeted client validation passed: `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 23 passed.
- 2026-05-22: Targeted server validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~ConversationReadApi|FullyQualifiedName~Governance|FullyQualifiedName~Idempotency"` - 112 passed.
- 2026-05-22: Package validation passed for contracts and client. The first parallel client pack attempt failed with a transient shared contracts DLL lock; serial rerun succeeded.
- 2026-05-22: Full solution validation passed: `dotnet test Hexalith.Conversations.slnx` - Client 23, Contracts 278, Integration 8, Core 139, Server 386.
- 2026-05-22: QA automation follow-up added command API coverage for handler-supplied audit unavailable, stale projection, participant/onboarding unavailable, and provider-identity remediation errors. Focused command API validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi"` - 13 passed.
- 2026-05-22: QA automation Story 4.3 focused validation passed: contracts filter - 49 passed; client project - 23 passed; server `ConversationCommandApi|ConversationReadApi|Governance|Idempotency` filter - 116 passed.
- 2026-05-22: Senior developer review tightened free-text safety guardrails for typed errors, sanitized unsupported closed-vocabulary exception messages, and split invalid package-version compatibility remediation from invalid schema-version remediation.
- 2026-05-22: Senior review validation passed: contracts filter - 50 passed; client project - 23 passed; server `ConversationCommandApi|ConversationReadApi|Governance|Idempotency` filter - 116 passed; contracts pack passed; client pack command passed; full solution passed with Client 23, Contracts 279, Integration 8, Core 139, Server 390.

### Completion Notes List

- Added `ConversationErrorClientAction`, `ConversationErrorDescriptor`, and `ConversationErrorCatalog` as the canonical contract-owned mapping for code, category, retryability, bounded action, safe message, HTTPS documentation pointer, and audit-handle allowance.
- Extended `ConversationError` with additive `ClientAction` and `SafeMessage` fields while preserving `DeveloperGuidance` for backward-compatible safe text.
- Refactored compatibility checks, client fallback errors, and command API local error construction to use the shared catalog rather than local drift-prone mappings.
- Preserved `ConversationReadApi` hidden/unavailable side-channel behavior and did not introduce Problem Details or raw HTTP adopter examples.
- Expanded contract, client, and server tests for descriptor coverage, closed-vocabulary parsing, additive JSON tolerance, non-JSON fallback mapping, malformed/authorization command API failures, idempotency conflict, timeout/unknown outcome, and leakage safety.
- Added QA automation follow-up coverage proving handler-supplied audit unavailable, stale projection, participant/onboarding unavailable, and provider-identity remediation errors remain catalog-aligned and content-safe at the command API boundary.
- Updated README and contract package README with compact contract/client-level error tables and bounded remediation semantics.
- Updated Story 4.3 evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Senior review fixed content-safety gaps by rejecting tenant, Party, conversation, provider-session/payload, raw business-reference, local-path, and exception markers in every `ConversationError` free-text field.
- Senior review fixed closed-vocabulary parser/converter diagnostics so unsupported error code/category/action values do not echo raw protected input.
- Senior review fixed compatibility remediation mapping so invalid package versions return `send-semantic-package-version` rather than schema-specific guidance.

### File List

- README.md
- _bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- src/Hexalith.Conversations.Client/ConversationClient.cs
- src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs
- src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs
- src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCategory.cs
- src/Hexalith.Conversations.Contracts/Errors/ConversationErrorClientAction.cs
- src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs
- src/Hexalith.Conversations.Contracts/Errors/ConversationErrorDescriptor.cs
- src/Hexalith.Conversations.Contracts/README.md
- src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs
- src/Hexalith.Conversations.Contracts/Serialization/ConversationStringValueJsonConverter.cs
- src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs
- src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs
- tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs
- tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs
- tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs
- tests/Hexalith.Conversations.Contracts.Tests/ConversationErrorCatalogTest.cs
- tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs
- tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs
- tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs

### Change Log

- 2026-05-22: Implemented Story 4.3 typed sanitized error catalog, client/server/compatibility mapping, leakage tests, adopter docs, validation evidence, and moved story to review.
- 2026-05-22: Senior developer review auto-fixed typed error content-safety gaps, sanitized unsupported closed-vocabulary diagnostics, corrected invalid package-version remediation, refreshed validation evidence, and moved story to done.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-22

Outcome: Approved after auto-fixes. No critical issues remain.

### Findings

1. High - `ConversationError` free-text validation did not reject several protected leak classes required by AC1 and AC4, including tenant IDs, Party IDs, conversation IDs, provider session/payload markers, raw business references, local paths, and raw exception labels. A caller or server adapter could still construct an otherwise typed error with protected details in `DeveloperGuidance`, `SafeMessage`, `AuditHandle`, `CorrelationId`, or diagnostics.
2. Medium - Unsupported error code/category/action parsing and the shared closed-vocabulary JSON converter echoed raw unsupported values in exception messages. That made malformed typed error payload diagnostics capable of carrying protected input such as tenant identifiers.
3. Medium - Invalid package-version compatibility checks reused the schema-specific `send-positive-integer-schema-version` remediation code. That gave adopter tooling a bounded but incorrect next action for malformed package versions.

### Auto-Fixes Applied

- Expanded `ConversationError` free-text guardrails and regression samples to reject tenant, Party, conversation, provider-session/payload, business-reference, local-path, and exception markers across every protected free-text field.
- Changed `ConversationErrorCode.Parse`, `ConversationErrorCategory.Parse`, `ConversationErrorClientAction.Parse`, and the shared string-value JSON converter to return sanitized unsupported-value diagnostics without raw input or inner exception text.
- Added `send-semantic-package-version` compatibility remediation and mapped invalid package-version diagnostics to it while preserving invalid schema-version guidance.
- Added regression assertions for sanitized parser/converter messages and corrected compatibility remediation branching.

### Checklist Validation

- Story file loaded and status verified as reviewable before updates.
- Acceptance Criteria 1-4 cross-checked against contracts, compatibility mapping, client fallback, server command API, tests, and docs.
- File List reconciled with git-discovered changes and updated for review-touched files.
- Security/content-safety review performed on changed contract, client, server, and test files.
- Official Microsoft documentation lookup performed for `System.Text.Json` converter error-handling behavior.
- Sprint status synced to `done`.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 4, Stories 4.1-4.5, Story 4.3 ACs, readiness gates, and downstream scope boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR26-FR32 and FR70-FR80 for tenant-safe developer experience and typed error commitments.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on API response formats, public API naming, project boundaries, trust/freshness vocabulary, and Developer Experience structure mapping.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`; no UI is in scope, but developer journey, safe explanation, and trust-before-reliance rules apply.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md` and sibling module contexts, with emphasis on .NET 10, central package management, typed contracts, fail-closed tenant isolation, structured errors, logging safety, and root-level submodule policy.
  - Loaded previous Story 4.2, current sprint status, readiness gates/decisions, README/package docs, existing contracts/client/server error files, current tests, test summary, and recent git history.
  - Checked official Microsoft documentation for ASP.NET Core 10 Problem Details, Minimal API typed responses, and .NET library logging guidance.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to existing error contracts, client result wrapper, command API, compatibility metadata, and test fixtures instead of creating duplicate models.
  - Added explicit guardrails for side-channel-safe read results, centralized error catalog, action/message fields, REST/client/compatibility consistency, Problem Details usage limits, and logging safety.
  - Added current file touch list, likely new files, targeted validation commands, package validation, official Microsoft documentation references, previous-story learnings, and out-of-scope boundaries.
  - Kept Story 4.4 diagnostics, Story 4.5 conformance package, Story 4.7 integration guide, raw HTTP examples, release signing, Admin UI, and telemetry dashboards out of scope.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture guardrails, test requirements, latest technical references, and explicit scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.
