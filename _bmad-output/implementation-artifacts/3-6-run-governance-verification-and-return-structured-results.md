# Story 3.6: Run Governance Verification and Return Structured Results

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator,
I want to run governance verification for conversations, tenants, suites, or time windows,
so that I can distinguish product invariant failures from infrastructure execution failures.

## Acceptance Criteria

1. Authorized verification scope is explicit and tenant-safe
   - Given an authorized operator requests governance verification for a conversation, tenant, suite, or time window,
   - When the verification runs,
   - Then it checks audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, schema compatibility, and related conformance expectations within the requested scope,
   - And verification detail access remains tenant-scoped and fail-closed.

2. Structured results classify product failures separately from execution failures
   - Given verification completes,
   - When results are returned,
   - Then the response is structured, machine-readable, and suitable for CI and incident workflows,
   - And it distinguishes governance verification failures from infrastructure, dependency, unavailable data, stale projection, unsupported version, or execution failures.

3. Unsafe or unavailable targets fail closed without disclosure
   - Given verification cannot safely inspect a target,
   - When tenant access, projection freshness, audit availability, permission checks, schema compatibility, or event replay checks fail,
   - Then the result uses typed content-safe failure semantics,
   - And it does not reveal protected conversation existence, Party identifiers, redacted content, raw provider payload, raw EventStore topology, stack traces, or cross-tenant business references.

4. Verification execution is audited where tenant data is touched
   - Given a governance verification run touches tenant-scoped conversation data or is triggered by a privileged operator workflow,
   - When execution starts or completes,
   - Then the result includes a safe audit/justification reference or an explicit not-recorded reason where the selected scope is read-only local test evidence only,
   - And any privileged operational justification uses existing Conversations governance audit boundaries instead of adding an unaudited side channel.

5. Tests prove result shape, failure classification, and tenant isolation
   - Given verification tests run,
   - When passing verification, invariant failure, infrastructure failure, stale projection, missing audit pair, redaction replay failure, projection rebuild disagreement, unsupported schema, cross-tenant poison, and unauthorized scope scenarios are exercised,
   - Then tests prove structured outcomes, failure classification, tenant isolation, content-safe diagnostics, and release-gate suitability.

## Tasks / Subtasks

- [x] Define contract-first verification DTOs and vocabularies (AC: 1, 2, 3, 5)
  - [x] Add public contracts under `src/Hexalith.Conversations.Contracts` for verification request scope, suite selection, check result, run result, execution status, failure classification, evidence handle, and safe remediation.
  - [x] Keep contracts serialization-friendly records with explicit schema version, tenant scope, optional conversation scope, requested time window, selected suites, generated timestamp, correlation id, and safe summary.
  - [x] Use bounded closed vocabularies rather than free-form strings for suites and classifications. Minimum suite names: `audit-pairing`, `tenant-isolation`, `redaction-replay`, `projection-rebuild`, `provider-portability`, and `schema-compatibility`.
  - [x] Minimum classification states: passed, governance-failed, infrastructure-failed, dependency-unavailable, data-unavailable, stale-projection, unsupported-version, unauthorized-or-hidden, execution-failed, and not-applicable.
  - [x] Add converters in `ClosedVocabularyJsonConverters.cs` only for new closed vocabularies. Do not use open polymorphic JSON contracts or discriminator names that could conflict with result properties.
  - [x] Apply content-safe validation equivalent to governance/query contracts: no stack traces, raw exception text, EventStore stream names, provider payloads, Party personal data, raw tenant target identifiers, route secrets, hidden fields, or unbounded business references in result text.

- [x] Implement a verification orchestration service without creating a new write authority (AC: 1, 2, 3, 4)
  - [x] Add a focused server service such as `ConversationGovernanceVerificationService` under `src/Hexalith.Conversations.Server/Governance` or `src/Hexalith.Conversations.Server/Verification`.
  - [x] Reuse existing tenant access checks before reading projections, replaying events, or returning scoped detail. Do not trust route/query/body tenant values as authority.
  - [x] Reuse `ConversationProjectionReadService`, `IConversationProjectionReadStore`, `ConversationReplayVerifier`, `ConversationProjectionRebuildVerifier`, and `GovernanceAuditPairingSafetyNetTest` patterns where applicable.
  - [x] Treat `ProjectionFreshnessV1.AllowsTrustBearingDecision()` as the default gate. Verification that relies on current projection state must block on `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted` unless a check explicitly classifies itself as a replay-only proof and records that distinction.
  - [x] Separate product invariant failures from execution failures. A missing audit pair is a governance verification failure; an unavailable projection store, inaccessible event source, unsupported schema, or thrown dependency exception is an infrastructure/dependency/execution classification.
  - [x] Verification results are derived evidence only. Do not append conversation domain events, mutate aggregate state, alter projections, create transcript tables, update idempotency records, or introduce a new durable verification authority in this story.

- [x] Add a tenant-safe execution surface only if it fits the current repo shape (AC: 1, 2, 3, 4)
  - [x] If adding an HTTP endpoint, place it outside `ConversationReadApi` unless it is a pure GET over existing read evidence; verification execution is an operator action and must not weaken Story 3.5 read-only guarantees.
  - [x] Prefer a dedicated route group such as `/api/v1/conversation-governance/verification` or equivalent, protected with `RequireAuthorization()`, trusted `tid` claim binding, caller `ClaimTypes.NameIdentifier`, and `X-Correlation-Id`.
  - [x] If the implementation chooses CLI-only or service-only for this story, provide the service and contracts plus tests; do not scaffold a full console/worker project unless the repo already has an approved pattern or a new project is explicitly needed.
  - [x] Verification scope inputs may specify conversation id, suite, and time window, but they must never supply tenant authority, caller authority, role, policy authority, freshness state, audit authority, or command availability.
  - [x] If a privileged verification run touches tenant data, require an existing `PrivilegedOperationalActionClass.Verify` justification path or return a content-safe blocked result. Do not silently bypass privileged justification.

- [x] Implement check adapters for current v1 evidence, with explicit gaps (AC: 1, 2, 3, 5)
  - [x] Audit pairing: verify implemented governance events have audit evidence references and classify missing or unsafe evidence as governance failure.
  - [x] Tenant isolation: verify requested scope matches trusted tenant, projection tenant, event metadata tenant, and read-store tenant; cross-tenant poison returns hidden/unauthorized or governance failure without echoing target identifiers.
  - [x] Redaction replay: replay ordered events and assert redacted messages remain placeholder-safe in reconstructed/projection outputs; classify redacted content reappearance as governance failure.
  - [x] Projection rebuild: reuse `ConversationProjectionRebuildVerifier` to compare rebuilt projection state with existing derived state; classify disagreement as governance failure or stale derived artifact, not infrastructure failure.
  - [x] Provider portability: verify provider correlation remains metadata only and is not used as conversation identity or authority; where the full portability proof is not yet implemented, return not-applicable or deferred with explicit release-scope classification rather than pretending pass.
  - [x] Schema compatibility: verify current schema version support and classify unsupported versions distinctly from malformed payloads and execution exceptions.
  - [x] Related conformance checks may be present, but every check must name requirement mappings, pass criteria, classification, safe detail, and evidence source.

- [x] Surface verification state back into existing trust/search models only from server-owned results (AC: 1, 2, 3, 5)
  - [x] Update `ConversationEvidenceTrustPostureV1` and `ConversationSearchTrustPreviewV1` only if the verification run produces a current, server-owned result that can safely influence `ConversationVerificationState`.
  - [x] Preserve the existing default of `ConversationVerificationState.Unknown` from `ConversationProjectionMaterializer` unless a verified result exists.
  - [x] Do not let UI/client-side state, route parameters, local storage, copied citations, command availability metadata, or operator-entered notes mark a conversation as verified.
  - [x] If verification state becomes filterable in list results, preserve Story 3.1 non-enumeration behavior for counts, ordering, pagination, empty states, and timing.

- [x] Add focused contract, service, API/CLI-boundary, and safety tests (AC: 1-5)
  - [x] Add contract serialization and validation tests for verification DTOs, closed vocabularies, safe diagnostic text, stable JSON shape, and forbidden vocabulary.
  - [x] Add service tests for passing verification, missing audit pair, redaction replay failure, projection rebuild disagreement, unsupported schema, stale projection, dependency unavailable, thrown exception, unauthorized scope, and cross-tenant poison.
  - [x] Extend query/projection tests only where verification state is intentionally projected into `ConversationVerificationState`; preserve unknown defaults for records that have not been verified.
  - [x] If an HTTP endpoint is added, test trusted claim binding, authorization metadata, malformed input hidden shape, content-safe 4xx/5xx result bodies, cancellation behavior, and no mutation endpoint under the read API group.
  - [x] Add a safety-net test proving verification boundaries do not directly depend on mutation command handlers, `IdempotentConversationCommandExecutor`, aggregate mutation methods, or EventStore append APIs.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` after implementation with Story 3.6 evidence.

- [x] Preserve scope boundaries and ADR stop conditions (AC: 1-5)
  - [x] Do not implement Story 3.7 seeded buyer demo fixtures or Story 3.8 responsive/accessibility/leak sentinel browser evidence in this story.
  - [x] Do not implement full signed conformance artifacts, release manifest signing, named waiver lifecycle, per-tenant continuous health endpoint, evidence bundle export, dashboards, or a full Admin UI shell unless separately promoted by scope decision.
  - [x] Stop for ADR/waiver if implementation needs a new durable verification store, a new event source abstraction over production EventStore internals, public raw stream cursors, cross-tenant verification from a single request, background queued execution, new governance mutation path, or long-running worker/process orchestration.

## Dev Notes

### Epic and Business Context

- Epic 3 delivers the compliance investigation workspace: compliance operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios. Story 3.6 is the verification slice after Story 3.5 preserved read-only workflows and safe command gates. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Compliance Investigation Workspace`]
- Story 3.6 covers FR66-FR68: operators can run governance verification for a conversation, tenant, suite, or time window; receive structured results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related checks; and distinguish governance failures from infrastructure/execution failures. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.6: Run Governance Verification and Return Structured Results`]
- The PRD's Sarah journey expects `governance verify --conversation X` to prove governance mutations are audit-paired without exposing protected data. The Marcus SRE journey expects production-suitable verification such as `conformance verify --suite audit_pairing --tenant T --since 03:00` with structured JSON output. [Source: `_bmad-output/planning-artifacts/prd.md#Journey 3`; `_bmad-output/planning-artifacts/prd.md#Journey 5`]
- v1 operator surface is narrow: CLI/service/API-style verification plus runbook-ready structured output is in scope; full Generate Evidence Bundle is v1.1 and must not be implemented here. [Source: `_bmad-output/planning-artifacts/prd.md#MVP / v1 Release Scope`]
- UX mapping for this story: UX-DR23, UX-DR26, UX-DR27, and UX-DR38. The key UX implications are safe telemetry/result metadata, evidence detail components, review/waiver summaries where applicable, and quality gates for leakage, tenant isolation, trust provenance, and command safety. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md`]

### Ready-for-Dev Preconditions

- Projection freshness blocking semantics are decided. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; only `Current` enables trust-bearing decisions by default. Verification and privileged background work block on non-current projections unless an ADR grants a narrower exception. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#projection-freshness-blocking-semantics`]
- Command availability metadata is server-owned. If verification appears as a workspace action later, clients render eligibility from server metadata and still recheck on execution. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#command-availability-metadata`]
- v1 stays narrow for lifecycle/export. Full evidence bundle export, full retention editor, automatic legal hold, future derived indexes, and broad lifecycle automation remain out of scope without ADR/scope approval. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#retention-deletion-tombstoning-legal-hold-export-and-derived-index-lifecycle`]
- Story 3.8 is split into later responsive/mobile, accessibility, and leakage/clipboard/browser/telemetry safety stories. Story 3.6 should add DTO/API/service leakage tests for verification result surfaces, but broad browser/accessibility/Leak Sentinel evidence belongs to Story 3.8A-3.8C. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Story 3.8 assignment plan`]

### Current Implementation State

- There is no `Hexalith.Conversations.Admin`, `Hexalith.Conversations.Conformance`, CLI, worker, or web UI project in the solution today. Current implementation scope is contracts, domain, server/API, projections, query services, governance services, and tests. Do not scaffold a large UI or conformance project unless the implementation proves it is necessary and scope-approved. [Source: `Hexalith.Conversations.slnx`; `src/` directory]
- Search and detail contracts already carry `ConversationVerificationState` with values `Verified`, `Unverified`, `Failed`, `Unavailable`, and `Unknown`. `ConversationProjectionMaterializer` currently sets verification state to `Unknown`. Preserve that fail-closed default until a server-owned verification result exists. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs`; `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`]
- `ConversationReadApi` is GET-only under `/api/v1/conversations`, requires authorization, binds tenant from the authenticated `tid` claim, binds caller from `ClaimTypes.NameIdentifier`, and maps malformed/hidden cases to content-safe shapes. Do not put verification mutation/execution semantics into this read-only route group if it would break Story 3.5. [Source: `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`; Story 3.5]
- `ConversationQueryHandler` is a read boundary over tenant access, projection read store, read hydration, citation access, temporal reconstruction, audit detail, and privileged justification review. Keep it read-oriented; verification orchestration should be a focused service unless it is strictly retrieving existing result state. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`]
- `ConversationProjectionRebuildVerifier` already rebuilds derived projection state from ordered events and produces safe local evidence with schema, tenant, conversation, freshness, pass flag, reason code, produced timestamp, and cursor. Reuse this instead of inventing a second projection rebuild proof path. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionRebuildVerifier.cs`]
- `ConversationReplayVerifier` already replays ordered persisted conversation events with bounded diagnostic codes for tenant mismatch, conversation mismatch, unsupported schema, event type mismatch, event position gaps/reordering, duplicate event identity, malformed payload, and replay invariant violation. Reuse this for redaction replay/schema compatibility checks where possible. [Source: `src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`]
- `GovernanceAuditPairingSafetyNetTest` inventories implemented governance mutation paths and proves read-only workspace boundaries do not directly depend on mutation handlers, audit gates, or idempotency mutation paths. Use this as the pattern for verification safety-net coverage. [Source: `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`]
- `PrivilegedOperationalActionClass.Verify` already exists in governance vocabularies. Use it for privileged verification justification instead of adding a parallel action vocabulary. [Source: `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`]

### Previous Story Intelligence

- Story 3.5 added command availability classification, mandatory fresh server recheck semantics, stronger safe-vocabulary validation, and tests proving read-only boundaries do not reference mutation execution paths. Story 3.6 must preserve those boundaries: verification may inspect evidence, but it must not become a hidden command execution route or mutation bypass. [Source: `_bmad-output/implementation-artifacts/3-5-preserve-read-only-compliance-workflows-and-safe-command-gates.md`]
- Story 3.5 established that unavailable or stale metadata disables governed actions and that missing/stale/ambiguous metadata is not an optional UI state. Apply the same approach to verification: missing evidence produces a typed non-pass result, never an assumed pass. [Source: `_bmad-output/implementation-artifacts/3-5-preserve-read-only-compliance-workflows-and-safe-command-gates.md#Completion Notes List`]
- Story 3.4 implemented citation and temporal access with strict cursor/target parsing and permission downgrade clearing. Reuse that posture for verification scope parsing: invalid scope, malformed cursor/window, unsupported suite, or cross-tenant target must fail closed with a safe result shape. [Source: `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md`]
- Recent commits show Epic 3 has followed a contract-first, projection-owned trust metadata pattern: Story 3.2 introduced governed evidence/trust posture, Story 3.3 added redaction/audit details, Story 3.4 added citation/temporal links, and Story 3.5 hardened read-only command gates. Continue that sequence with verification contracts and focused tests before any UI. [Source: `git log -5 --oneline`]

### Architecture Guardrails

- EventStore remains authoritative for writes. Verification must not append events, mutate aggregates, create direct transcript storage, expose raw EventStore stream/envelope/snapshot mechanics, or create another source of truth. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Projections, admin UI state, exports, caches, verification snapshots, and future indexes are derived. Verification output is evidence/result data, not conversation authority. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Tenant access must fail closed before projection or aggregate access. Verification must not trust JWT tenant claims alone; the local tenant access projection is the access decision source. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Disclosure rules apply to output JSON, safe summaries, diagnostic codes, logs, traces, metrics, screenshots, conformance artifacts, route labels, browser-title-ready labels, and any future UI metadata. [Source: `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`]
- Party personal data remains owned by Hexalith.Parties. Verification results may use stable Party IDs only where authorized and must not serialize Party display names, contacts, identifiers, person details, organization details, or upstream problem details. [Source: `_bmad-output/project-context.md#Critical Implementation Rules`]
- Verification output must distinguish product invariant failure from infrastructure failure. Do not collapse all non-pass outcomes into generic failed; operators and CI need to know whether the system invariant is broken or the check could not safely execute. [Source: `_bmad-output/planning-artifacts/prd.md#Operability And Observability`]

### Likely Files to Create or Update

- NEW `src/Hexalith.Conversations.Contracts/Governance/ConversationGovernanceVerification*.cs` or `src/Hexalith.Conversations.Contracts/Verification/*.cs`: request/result/suite/classification contracts and closed vocabularies.
- UPDATE `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`: add converters only for new closed vocabularies.
- UPDATE `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs` only if new verification states are needed; prefer existing states unless the story proves they are insufficient.
- NEW `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs` or `src/Hexalith.Conversations.Server/Verification/*.cs`: orchestration across tenant access, projections, replay, rebuild, and audit-pairing checks.
- UPDATE `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs` only if adding a pure result retrieval route; do not add verification execution semantics under read-only routes.
- NEW `src/Hexalith.Conversations.Server/Api/ConversationGovernanceVerificationApi.cs` if an HTTP execution surface is implemented.
- UPDATE `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs` only if exposing previously computed verification state in existing read/list flows.
- UPDATE `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs` only if verification result state is intentionally projected into trust/search models; preserve `Unknown` default.
- UPDATE or ADD tests under `tests/Hexalith.Conversations.Contracts.Tests`, `tests/Hexalith.Conversations.Server.Tests/Governance`, `tests/Hexalith.Conversations.Server.Tests/Api`, `tests/Hexalith.Conversations.Server.Tests/Projections`, and `tests/Hexalith.Conversations.Tests/Replay`.

### Testing Requirements

- Run focused contract tests:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ConversationQuery|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
- Run focused server verification/projection/query/API tests:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationProjectionRebuildVerifierTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"`
- Run replay/domain verification regressions:
  - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifier|FullyQualifiedName~ConversationAggregateRedaction|FullyQualifiedName~ConversationAggregateRetentionPolicy|FullyQualifiedName~ConversationAggregateSensitivity"`
- Run tenant/governance command regressions if verification touches audit readiness, tenant access, or privileged justification:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~ConversationPrivilegedOperationalJustificationService|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`

### Latest Technical Information

- ASP.NET Core route groups support applying common metadata such as authorization to all endpoints in a group. If an HTTP verification route is added, use a protected route group instead of duplicating authorization metadata per endpoint. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`]
- Minimal API endpoint filters can run before and after route handlers and can modify arguments/results. They can be useful for cross-cutting verification metadata, but tenant/caller authority must still come from server-side authentication and authorization state. [Source: `https://learn.microsoft.com/aspnet/core/mvc/controllers/filters?view=aspnetcore-10.0#how-filters-work`]
- .NET hosted services and `BackgroundService` are the current platform pattern for long-running/background work, but this story should not introduce a worker unless verification genuinely needs queued or long-running execution and scope approval exists. [Source: `https://learn.microsoft.com/dotnet/core/extensions/workers`]
- System.Text.Json supports records and custom converters, but .NET 10 validates metadata/property-name conflicts earlier for polymorphic contracts. Prefer simple closed-vocabulary records and existing converter patterns over open polymorphic verification result hierarchies. [Source: `https://learn.microsoft.com/dotnet/core/compatibility/serialization/10/property-name-validation`; `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/polymorphism`]
- `dotnet test --filter` supports `FullyQualifiedName~...`, boolean `|` and `&`, and xUnit traits. Use targeted filters first, then full solution validation. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]

### Out of Scope

- Do not implement Story 3.7 self-serve buyer acceptance demo, seeded buyer fixtures, or demo route flows.
- Do not implement Story 3.8A responsive/mobile safe triage, Story 3.8B accessibility-tree/keyboard/screen-reader safety, or Story 3.8C leakage/clipboard/browser/telemetry disclosure safety beyond DTO/API/service leakage tests required here.
- Do not implement Epic 5 release manifest signing, named waiver process, adopter-facing conformance package, or full signed conformance artifact.
- Do not implement a full Admin/FrontComposer shell, dashboards, evidence bundle export, audit export, legal-hold automation, queue workers, direct EventStore browsing, transcript tables, secondary authoritative read stores, Memories/RAG indexes, or new projection authorities.
- Do not mutate conversation aggregate state from verification. Any operational audit/justification record must use existing governance audit boundaries and must not create unpaired governance mutations.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.6: Run Governance Verification and Return Structured Results`
- `_bmad-output/planning-artifacts/prd.md#Journey 3 — Sarah, Compliance Operator`
- `_bmad-output/planning-artifacts/prd.md#Journey 5 — Marcus, On-call SRE`
- `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`
- `_bmad-output/planning-artifacts/prd.md#Operability And Observability`
- `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`
- `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`
- `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Quality Gates`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-5-preserve-read-only-compliance-workflows-and-safe-command-gates.md`
- `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceContractValidation.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionRebuildVerifier.cs`
- `src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionRebuildVerifierTest.cs`
- `tests/Hexalith.Conversations.Tests/Replay/ConversationReplayVerifierTest.cs`
- `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`
- `https://learn.microsoft.com/aspnet/core/mvc/controllers/filters?view=aspnetcore-10.0#how-filters-work`
- `https://learn.microsoft.com/dotnet/core/extensions/workers`
- `https://learn.microsoft.com/dotnet/core/compatibility/serialization/10/property-name-validation`
- `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/polymorphism`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Ran focused and full validation for Story 3.6; all listed commands passed.
- 2026-05-22: Senior review auto-fixes applied for privileged verify-justification validation and temporal source failure classification; focused and full validation passed.

### Implementation Plan

- Added contract-first verification DTOs and closed vocabularies under `Hexalith.Conversations.Contracts/Governance`, with JSON converters and serialization fixtures.
- Added a focused `ConversationGovernanceVerificationService` under server governance boundaries. The service uses trusted tenant/caller inputs, existing tenant access, current projection freshness gating, replay verification, projection rebuild verification, and existing privileged verification audit references.
- Kept the execution surface service-only for this story. No HTTP endpoint, CLI project, worker, durable verification store, EventStore append path, or trust/search verification-state projection was added.
- Added focused contract/service/safety coverage and updated the BMAD test summary with Story 3.6 evidence.

### Completion Notes List

- Implemented structured verification contracts for request scope, suite selection, check results, run results, execution status, failure classification, evidence handles, and safe remediation.
- Implemented tenant-safe verification orchestration that blocks on missing trusted authority, missing verify justification, non-current projection freshness, unavailable replay proof, unsupported schema, and cross-tenant poison with content-safe classifications.
- Added v1 adapters for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and schema compatibility using existing projection/replay/rebuild boundaries.
- Preserved `ConversationVerificationState.Unknown` defaults by not projecting transient verification results into search/trust models without a durable server-owned result source.
- Service-only execution was selected because this repo has no approved CLI/worker/Admin shell pattern for this story and the HTTP endpoint was optional.
- Validation passed: 66 focused contract tests, 51 replay/domain tests, 122 server verification/read-boundary tests, 133 tenant/governance command tests, and 743 full solution tests.
- Senior review auto-fixes tightened privileged verification to require an existing `PrivilegedOperationalActionClass.Verify` justification result and preserved distinct stale-projection/data-unavailable classifications for temporal evidence source gaps.
- Final validation passed: 66 focused contract tests, 51 replay/domain tests, 125 server verification/read-boundary tests, 133 tenant/governance command tests, and 746 full solution tests.

### File List

- `_bmad-output/implementation-artifacts/3-6-run-governance-verification-and-return-structured-results.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Governance/ConversationGovernanceVerificationContracts.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationServiceCollectionExtensions.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/GovernanceVerificationContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationGovernanceVerificationServiceTest.cs`

## Senior Developer Review (AI)

### Review Findings

- [x] HIGH: Verification accepted any `GovernanceAuditEvidenceReference` as privileged proof instead of requiring an existing `PrivilegedOperationalActionClass.Verify` justification path. Fixed by requiring `PrivilegedOperationalJustificationDetailsV1` with operation class `Verify`, succeeded outcome, current visible freshness, matching tenant, and matching conversation scope before tenant evidence is touched. [src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs](D:/Hexalith.Conversations/src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs:51)
- [x] HIGH: Rebuilding or retained-coverage temporal source failures collapsed into `dependency-unavailable`, losing the structured product/execution classification required for CI and incident workflows. Fixed by mapping rebuilding temporal evidence to `stale-projection` and retained coverage misses to `data-unavailable`, with regression coverage. [src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs](D:/Hexalith.Conversations/src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs:472)

### Validation Checklist

- [x] Story file loaded from `_bmad-output/implementation-artifacts/3-6-run-governance-verification-and-return-structured-results.md`
- [x] Story Status verified as reviewable before review (`review`)
- [x] Epic and Story IDs resolved (`3.6`)
- [x] Story Context located in the story file
- [x] Epic Tech Spec/planning context located through referenced planning artifacts
- [x] Architecture/standards docs loaded from `_bmad-output/project-context.md`
- [x] Tech stack detected: C#/.NET 10, xUnit v3, Shouldly, ASP.NET Core server contracts
- [x] MCP doc search performed against Microsoft Learn for System.Text.Json converter guidance
- [x] Acceptance Criteria cross-checked against implementation
- [x] File List reviewed and validated against git status
- [x] Tests identified and mapped to ACs; gaps fixed
- [x] Code quality review performed on changed files
- [x] Security review performed on changed files and dependency boundaries
- [x] Outcome decided: Approved after auto-fixes
- [x] Review notes appended under "Senior Developer Review (AI)"
- [x] Change Log updated with review entry
- [x] Status updated to `done`
- [x] Sprint status synced
- [x] Story saved successfully

### Review Validation

- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationGovernanceVerificationServiceTest"` - 18 passed.
- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ConversationQuery|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 66 passed.
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifier|FullyQualifiedName~ConversationAggregateRedaction|FullyQualifiedName~ConversationAggregateRetentionPolicy|FullyQualifiedName~ConversationAggregateSensitivity"` - 51 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationProjectionRebuildVerifierTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 125 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~ConversationPrivilegedOperationalJustificationService|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"` - 133 passed.
- `dotnet test Hexalith.Conversations.slnx` - 746 passed.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 3, Story 3.6, and Stories 3.1-3.5 continuity.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on Sarah and Marcus verification journeys, FR66-FR68, NFR55-NFR59, tenant safety, and structured verification output.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on EventStore authority, derived verification evidence, disclosure surfaces, implementation guardrails, and conformance evidence boundaries.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`, focusing on verification, evidence detail, safe telemetry, review flows, and quality gates.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`.
  - Loaded previous Story 3.5, readiness gates, readiness decisions, recent git history, and current source/test files for verification state, command availability, read API, query handler, projection materialization, projection rebuild, replay verification, governance vocabulary, and audit-pairing safety.
  - Checked official Microsoft documentation for ASP.NET Core route groups, endpoint/filter behavior, .NET worker/hosted-service guidance, System.Text.Json polymorphism/property-conflict behavior, and `dotnet test --filter`.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to existing replay, projection rebuild, tenant access, audit-pairing, and trust metadata boundaries instead of inventing a new source of truth.
  - Added explicit guardrails for structured result classification, current-freshness gating, product-vs-infrastructure distinction, privileged verification justification, content-safe diagnostics, and no mutation from verification.
  - Added likely file touch list, focused test commands, latest technical references, prior-story lessons, and ADR stop conditions.
  - Kept Story 3.7, Story 3.8, Epic 5 release artifacts, UI shell, export, worker, and continuous health endpoint scope out of Story 3.6.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture/UX guardrails, prior-story intelligence, test requirements, latest technical references, and explicit out-of-scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Change Log

- 2026-05-22: Senior review auto-fixed privileged verify-justification validation and temporal source failure classification; updated tests, test summary, story status, and sprint status.
- 2026-05-22: Implemented Story 3.6 governance verification contracts, focused service orchestration, DI registration, safety tests, and test summary evidence.
- 2026-05-22: Created Story 3.6 context from Epic 3 requirements, PRD/architecture/UX/readiness/project context, previous Story 3.5 learnings, current verification-adjacent implementation, recent git history, and official Microsoft documentation.
