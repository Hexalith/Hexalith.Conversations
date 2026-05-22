# Story 3.5: Preserve Read-Only Compliance Workflows and Safe Command Gates

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator,
I want read-only investigation workflows and clearly gated privileged actions,
so that investigation cannot accidentally mutate conversation state.

## Acceptance Criteria

1. Read-only investigation paths cannot mutate conversation state
   - Given an operator uses a workflow marked read-only,
   - When they search, open, inspect, cite, time-travel, or review evidence,
   - Then no conversation aggregate state, governance state, audit record, projection authority, EventStore stream, command idempotency record, export, browser storage, recent-item trace, or durable command intent is mutated,
   - And privileged or governance-changing actions are absent, disabled, or represented only through server-owned command availability metadata.

2. Privileged and governance-changing actions are classified and fail closed
   - Given a privileged action could mutate metadata, visibility, policy state, audit records, governance state, or operational evidence,
   - When the action appears in the governed workspace contract,
   - Then it is separately classified from read-only actions, includes safe server-provided availability, blocked reason, required permission, precondition state, freshness requirement, audit requirement, risk level, and last-evaluated timestamp,
   - And missing, stale, malformed, ambiguous, unauthorized, partial, or contradictory command metadata is treated as governed unavailable, not as a client-side optional disabled state.

3. Command execution handoff requires a fresh server recheck
   - Given command metadata marks an action available,
   - When a client or future FrontComposer surface attempts to execute the privileged action,
   - Then execution must route through the existing command handler boundary and recheck tenant, caller, role/requirement, projection freshness, command availability, audit availability, governance policy, idempotency, and target identity immediately before dispatch,
   - And route/query/body/hidden-field/client-state values must never supply tenant authority, caller authority, trust state, audit authority, command availability, or policy authority.

4. Permission or tenant transitions clear protected state
   - Given the operator loses permission, switches tenant, receives stale/rebuilding/unavailable freshness, or receives missing command metadata during review,
   - When the governed detail/citation/audit/temporal/read-only workflow observes the transition,
   - Then gated details close, protected detail/citation/link/clipboard-ready command data is cleared, only safe operator-entered intent is preserved where applicable, and the result exposes a safe next action,
   - And responses, route labels, browser-title-ready labels, accessibility-label-ready fields, telemetry-ready fields, counts, timing, and layout gaps do not imply protected record existence.

5. Tests prove read-only safety and command-gate behavior
   - Given read-only and command-gate tests run,
   - When read-only inspection, blocked command, available command metadata, missing command metadata, stale projection, audit unavailable, permission downgrade, tenant switch, malformed target, and cross-tenant poison scenarios are exercised,
   - Then tests prove no mutation in read-only paths, pre-execution recheck requirements, content-safe command safety metadata, and transition behavior that clears protected fields without disclosure.

## Tasks / Subtasks

- [x] Harden command availability contracts and defaults (AC: 1, 2, 5)
  - [x] Review `ConversationCommandAvailabilityV1` and `ConversationEvidenceTrustPostureV1`; extend only if the current fields cannot express separate read-only versus governance-changing classification, safe unavailable defaults, and immediate recheck requirements.
  - [x] Preserve additive, serialization-friendly contracts. Do not break existing `ConversationDetailsV1`, `ConversationSearchTrustPreviewV1`, citation, temporal, audit, or projection DTO shapes.
  - [x] Ensure default command metadata remains fail-closed when omitted. The existing default in `ConversationEvidenceTrustPostureV1` must keep unsafe actions unavailable, not hidden behind an empty list.
  - [x] Tighten validation if needed so action names, permissions, risk levels, blocked reasons, and labels cannot carry EventStore internals, provider payloads, browser-selected values, route secrets, Party personal data, raw exception text, or unbounded business references.
  - [x] Keep `set-retention-policy`, `mark-content-sensitive`, and `redact-message-content` aligned with implemented governance command names unless the current code has already introduced a canonical action catalog.

- [x] Compute command-gate metadata at the governed read boundary (AC: 1, 2, 4)
  - [x] Reuse `ConversationProjectionMaterializer.CreateTrustPosture()` and its `DefaultCommandEligibility()` output as the starting point; do not create a second transcript/detail model or UI-owned command catalog.
  - [x] If caller-specific availability is required, evaluate it after `ConversationProjectionReadService.ReadDetailAsync()` has accepted tenant scope and projection trust, inside `ConversationQueryHandler` or a focused service. Do not put caller/role decisions in aggregate logic or projection replay.
  - [x] Treat `ProjectionFreshnessV1.AllowsTrustBearingDecision()` as the default gate: only `Current`/`Current`/non-stale metadata may enable command availability unless this story records a narrower explicit exception.
  - [x] Use the readiness decision that command availability is server-owned metadata with eligibility, disabled state, required permission, precondition, risk level, freshness, audit requirement, and blocked reason. Missing/stale/ambiguous metadata disables unsafe actions.
  - [x] Preserve command availability as metadata only for read workflows. Do not add command execution to `ConversationReadApi`.

- [x] Preserve the existing read-only API surface (AC: 1, 3, 5)
  - [x] Keep search/list/detail/citation/temporal/audit-record reads under the existing authorized `/api/v1/conversations` read group in `ConversationReadApi`.
  - [x] Do not add POST/PUT/PATCH/DELETE endpoints to `ConversationReadApi`, and do not call `ConversationAggregate.Handle(...)`, command handlers, `IdempotentConversationCommandExecutor`, EventStore append APIs, or `ConversationGovernanceAuditGate.RecordRequiredAsync()` from read-only routes.
  - [x] Continue binding tenant from the authenticated `tid` claim, caller from `ClaimTypes.NameIdentifier`, and correlation from `X-Correlation-Id`; never accept tenant, caller, role, permission, availability, audit authority, trust state, or policy authority from client inputs.
  - [x] Keep malformed/cross-tenant/unauthorized outcomes side-channel-safe: hidden/forbidden shapes must not echo target ids, command names supplied by attackers, route metadata, unavailable internals, or policy details the caller is not allowed to know.
  - [x] If a command handoff endpoint is required, stop for scope/ADR unless it belongs to an existing command API boundary rather than the read API.

- [x] Make privileged action handoff explicit without implementing new mutations (AC: 2, 3)
  - [x] Document or encode that available command metadata is advisory until execution rechecks the same tenant/caller/current-freshness/audit/policy conditions at the command handler boundary.
  - [x] Reuse existing governance command handlers for implemented mutations: `SetConversationRetentionPolicyCommandHandler`, `MarkConversationContentSensitiveCommandHandler`, and `RedactMessageContentCommandHandler`.
  - [x] Preserve `ConversationGovernanceAuditGate` semantics: governance mutations fail closed when audit recording is unavailable. Do not queue unaudited governance writes.
  - [x] Preserve `GovernanceAuditPairingSafetyNetTest` as the mutation inventory. If a new governance mutation path is introduced, update that inventory and add audit-pairing tests in the same story.
  - [x] Keep privileged operational justification review read-only in this story. Do not implement Story 3.6 verification execution, Story 3.7 demo fixtures, or broad Story 3.8 responsive/accessibility/leak scanning.

- [x] Support safe transition behavior in response contracts (AC: 4, 5)
  - [x] Ensure denied/unavailable/rebuilding/hidden responses for detail, citation, temporal, audit-record, and list paths omit protected DTO fields such as `safeCopiedText`, `temporalCursor`, audit details, command action labels, and evidence ids when the caller no longer has access.
  - [x] Preserve and extend existing permission-downgrade behavior from Story 3.4 citation tests: after denial, clipboard/link-ready metadata must be absent and protected ids must not be echoed.
  - [x] If adding UI or component-ready DTO fields, include only browser-title-ready, accessibility-label-ready, and route-label-ready safe text from server-owned metadata; do not derive labels from rendered content, selection, local storage, route fragments, or full component models.
  - [x] If an Admin/FrontComposer project exists by implementation time, put custom command-gate primitives under that established trust-component boundary. Use Fluent UI Blazor disabled/loading/focusable-disabled patterns only as rendering of server-owned metadata, not as authority.
  - [x] Keep no-Admin-project behavior valid: contracts/server/API/tests are sufficient for this story if no `Hexalith.Conversations.Admin` or web project exists.

- [x] Add focused contract, projection, query, API, and safety tests (AC: 1-5)
  - [x] Extend `ConversationEvidenceContractTest` or add `ConversationCommandAvailabilityContractTest` for fail-closed defaults, classification, safe blocked reasons, required permission/risk/audit/freshness fields, serialization order, and forbidden vocabulary.
  - [x] Extend `ConversationProjectionMaterializerTest` for default command metadata across current, stale, rebuilding, unavailable, redacted, missing audit evidence, and unsupported-schema projections.
  - [x] Extend `ConversationQueryHandlerTest` for read-only paths preserving command metadata, missing metadata fallback, stale projection blocking, available metadata if supported, audit-unavailable blocking, permission downgrade, tenant switch/cross-tenant poison, and no projection-to-aggregate mutation calls.
  - [x] Extend `ConversationReadApiTest` for GET-only read routes, group authorization metadata, trusted claim binding, client-supplied command metadata ignored, hidden/unavailable responses clearing command/citation/audit fields, and no mutation endpoint under the read API.
  - [x] Add or extend a governance safety-net test proving read-only workflows do not invoke implemented governance command handlers, `ConversationGovernanceAuditGate`, EventStore append, or idempotency mutation paths.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` after implementation with Story 3.5 evidence and note whether UI/component E2E was applicable.

- [x] Preserve scope boundaries and ADR stop conditions (AC: 1-5)
  - [x] Do not scaffold a full Admin/FrontComposer shell solely for this story.
  - [x] Do not implement privileged command execution routes, confirmation dialogs, retention editor, evidence bundle export, audit export, legal-hold automation, browser storage, recent-action lists, transcript tables, secondary read stores, Memories/RAG indexes, queue workers, or new projection authorities.
  - [x] Do not treat generated UI disabled state as authorization. Server metadata and command-handler rechecks remain authoritative.
  - [x] Stop for ADR/waiver if implementation needs a new durable command-gate store, public permission model, raw EventStore cursor exposure, non-current projection reliance, client-side policy authority, mobile governance-changing action, export lifecycle behavior, or a new governance mutation path.

## Dev Notes

### Epic and Business Context

- Epic 3 delivers the compliance investigation workspace: compliance operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios. Story 3.5 is the safety gate after Story 3.4 made citation copy and temporal evidence links available. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Compliance Investigation Workspace`]
- Story 3.5 covers FR64 and FR65. The epic acceptance criteria require read-only investigation paths for search/open/inspect/cite/time-travel/review and explicit gating for privileged actions that could mutate metadata, visibility, policy state, audit records, or governance state. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.5: Preserve Read-Only Compliance Workflows and Safe Command Gates`]
- The PRD requires governance mutations to have paired audit evidence and to fail closed when audit recording is unavailable. It also requires non-governance activity during audit degradation to continue only when it does not mutate governance state. [Source: `_bmad-output/planning-artifacts/prd.md#Governance And Audit`]
- UX mapping for this story: UX-DR3, UX-DR9, UX-DR14, UX-DR20, UX-DR33, UX-DR34, UX-DR36, UX-DR37, and UX-DR38. The key UX rule is that gated actions render only from Conversations-owned command availability metadata; missing/stale metadata disables governed actions. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md`]

### Ready-for-Dev Preconditions

- Command availability metadata is decided. Server-owned command metadata controls eligibility, disabled state, required permission, precondition, risk level, freshness requirement, audit requirement, and blocked reason. Missing, stale, ambiguous, malformed, unauthorized, or partially loaded metadata disables unsafe actions. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Command availability metadata`]
- Projection freshness blocking semantics are decided. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; if a story does not declare an exception, only `Current` enables trust-bearing decisions or command eligibility. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- Retention/deletion/legal-hold/export lifecycle is narrow for v1. Full evidence bundle export, full retention editor, automatic legal-hold automation, future derived indexes, and broad lifecycle automation remain out of scope unless promoted by ADR and release-scope approval. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle`]
- Story 3.8 was split into responsive/mobile, accessibility, and leakage/clipboard/browser/telemetry safety gates. Story 3.5 should add minimal DTO/API safety tests for command gates, but broad browser/accessibility/leak-sentinel evidence belongs to Stories 3.8A-3.8C. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Story 3.8 assignment plan`]

### Current Implementation State

- There is no `Hexalith.Conversations.Admin` or web UI project in the Conversations solution. Current implementation scope is contracts, server/API, projections, query services, governance services, and tests. Do not scaffold UI unless a separate approved decision promotes it. [Source: `Hexalith.Conversations.slnx`; `src/` directory]
- `ConversationCommandAvailabilityV1` already carries action name, availability state, required permission, precondition state, risk level, freshness requirement state, audit requirement, blocked reason, and last evaluated timestamp. It currently validates only non-empty text, so this story may need stronger safe-token/safe-text validation. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`]
- `ConversationEvidenceTrustPostureV1` includes `CommandEligibility` and fail-closed default metadata when omitted. Its default action is `read-governed-record` with unavailable command metadata. Preserve this no-empty-authority behavior. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`]
- `ConversationProjectionMaterializer.CreateTrustPosture()` currently builds command eligibility using `DefaultCommandEligibility()`. The default metadata lists `set-retention-policy`, `mark-content-sensitive`, and `redact-message-content` as unavailable from the governed read surface. This is the safest starting point for read-only workflows. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`]
- `ConversationReadApi` maps only GET routes under `/api/v1/conversations` and applies `RequireAuthorization()` to the group. It binds tenant from `tid`, caller from `ClaimTypes.NameIdentifier`, and correlation from `X-Correlation-Id`; it already avoids client-supplied tenant/caller authority for detail, list, citation, temporal, and audit-record reads. [Source: `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`]
- `ConversationQueryHandler` reads detail/list/citation/temporal/audit through projection and query services. It should remain a read boundary; do not route write commands through it. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`]
- Governance command handlers already exist for retention, sensitivity, and redaction. The audit-pairing safety net enumerates implemented governance mutation paths and proves privileged justification is an audit boundary, not an aggregate mutation path. [Source: `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`]

### Previous Story Intelligence

- Story 3.4 implemented server-owned citation contracts, citation query/service/API, and composite temporal anchors. It added explicit tests proving citation/temporal routes bind trusted claims, clear clipboard/link-ready metadata after permission downgrade, and reject malformed cursor/target inputs without projection reads. Reuse these patterns for command-gate transition tests. [Source: `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md`]
- Story 3.4 review fixed strict temporal cursor validation, citation DTO/target disclosure validation, and future-position citation cursor handling. Apply the same posture to command metadata: reject unsafe or ambiguous metadata rather than rendering it as a harmless disabled action. [Source: `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md#Senior Developer Review (AI)`]
- Recent commits are all Epic 3 read-workspace slices: Story 3.1 search, Story 3.2 governed evidence read, Story 3.3 redaction/audit details, and Story 3.4 citation/temporal links. Continue the pattern of contract-first DTOs, projection-owned trust metadata, manual safety-sensitive parsing, and focused tests before full-solution validation. [Source: `git log -5 --oneline`]

### Architecture Guardrails

- EventStore remains authoritative for writes. Read workflows must not append events, mutate aggregates, create derived authoritative state, or expose raw EventStore stream/envelope/snapshot mechanics. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Projections, admin UI state, exports, caches, verification snapshots, and future indexes are derived. If this story touches projections, it must keep them repairable and derived from accepted events. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Tenant access must fail closed before projection or aggregate access. Do not trust JWT tenant claims alone; the local tenant access projection decides access. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`]
- UX must render trust, freshness, redaction, tenant isolation, provenance, and command availability from governed domain outputs, not client inference. Absence must not look safe. [Source: `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`]
- Disclosure surfaces include HTTP status codes, JSON bodies, URLs, browser-title-ready labels, route labels, hidden DOM, ARIA labels, clipboard payloads, telemetry, logs, diagnostics, screenshots, and evidence artifacts. Permission-safe DTOs are required per surface. [Source: `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`]

### Likely Files To Update

- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`: currently defines the command availability contract and only checks non-empty strings. Likely update for safe vocabulary/classification fields or stronger validation.
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`: currently owns default fail-closed command eligibility. Preserve fallback behavior and adjust only if the command availability contract changes.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`: currently creates default command eligibility for retention/sensitivity/redaction as unavailable from the read surface. Update here if command metadata shape or safe blocked reasons change.
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`: if caller-specific command metadata is needed, add it after authorized projection reads and before returning `ConversationDetailsV1`. Preserve read-only behavior.
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`: likely update tests more than implementation. If touched, preserve GET-only routes, group authorization, trusted claim binding, and hidden/unavailable response shapes.
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs` or a new `ConversationCommandAvailabilityContractTest.cs`: add contract/serialization/safe-vocabulary coverage.
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`: add command metadata state matrix coverage.
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`: add read-boundary and transition tests.
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`: add read-only route/claim/input/hidden-shape tests.
- `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`: update only if a governance mutation inventory changes; otherwise add a separate read-only safety-net test.

### Testing Requirements

- Run focused contract tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCommandAvailability|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
- Run focused server projection/query/API/governance tests:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"`
- Run tenant/governance command regressions if command handoff or availability logic touches authorization or audit readiness:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`

### Latest Technical Information

- ASP.NET Core route groups support applying shared metadata such as `RequireAuthorization()` to a common endpoint prefix. Keep any read endpoint under the existing authorized route group rather than duplicating per-route authorization. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`]
- Minimal API binding failures can generate framework-shaped 400/500 responses. For safety-sensitive command or evidence identifiers where hidden/unavailable equivalence matters, continue manual parsing and content-safe result mapping. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0#custom-binding`]
- ASP.NET Core .NET 10 identifies known API endpoints and cookie auth now returns 401/403 rather than HTML redirects for protected API endpoints. Conversations still maps domain denial to hidden/forbidden bodies where non-enumeration requires it. [Source: `https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#authentication-and-authorization`]
- Blazor event handling supports async `Task` handlers and automatically rerenders after events; async `void` must be avoided. Future command-gate components should use async handlers with in-flight disabled/loading state and server recheck on action. [Source: `https://learn.microsoft.com/aspnet/core/blazor/components/event-handling?view=aspnetcore-10.0`]
- Blazor server-side threat guidance recommends disabling buttons while async work is in progress to prevent multiple dispatches, and basing decisions on current app/server state rather than stale UI state. [Source: `https://learn.microsoft.com/aspnet/core/blazor/security/interactive-server-side-rendering?view=aspnetcore-10.0#interactions-with-the-browser-client`]
- Fluent UI Blazor MCP documentation for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0.26098` shows `FluentButton` supports `Disabled`, `DisabledFocusable`, `Loading`, `OnClick`, and safe label/tooltip patterns. If UI exists, render server-owned command metadata through these controls but do not treat disabled UI as authorization. [Source: Fluent UI Blazor MCP `FluentButton` docs, version `5.0.0.26098`]
- Fluent UI Blazor dialog guidance reserves modal dialogs for important, irreversible, or potentially destructive choices and recommends validation before closing. If a future mutation confirmation appears, it must be outside this read-only story and must route through command-handler rechecks. [Source: Fluent UI Blazor MCP `FluentDialog` docs, version `5.0.0.26098`]
- `dotnet test --filter` supports `FullyQualifiedName~...` contains expressions and `|`/`&` composition for xUnit runs. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]

### Out of Scope

- Do not implement Story 3.6 governance verification runner/results.
- Do not implement Story 3.7 self-serve buyer acceptance demo or seeded demo fixtures.
- Do not implement Story 3.8A responsive/mobile safe triage, Story 3.8B accessibility-tree/keyboard/screen-reader verification, or Story 3.8C leakage/clipboard/browser/telemetry disclosure safety beyond minimal DTO/API tests required here.
- Do not implement a full Admin/FrontComposer shell, privileged command execution routes, command confirmation dialogs, full retention editor, evidence bundle export, audit export, legal-hold automation, browser storage, recent-action lists, transcript tables, secondary evidence stores, Memories/RAG indexes, queue workers, or new projection authorities.
- Do not mutate conversation aggregate state from search, detail, audit-record review, citation copy, temporal-link resolution, or privileged-justification review paths.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.5: Preserve Read-Only Compliance Workflows and Safe Command Gates`
- `_bmad-output/planning-artifacts/prd.md#Governance And Audit`
- `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`
- `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`
- `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Design System Foundation`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`
- `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationGovernanceAuditGate.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
- `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`
- `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0#custom-binding`
- `https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#authentication-and-authorization`
- `https://learn.microsoft.com/aspnet/core/blazor/components/event-handling?view=aspnetcore-10.0`
- `https://learn.microsoft.com/aspnet/core/blazor/security/interactive-server-side-rendering?view=aspnetcore-10.0#interactions-with-the-browser-client`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Red phase confirmed for new command availability contract coverage; test compile failed before additive classification/recheck fields existed.
- 2026-05-22: Focused contract validation passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCommandAvailability|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 61 passed.
- 2026-05-22: Focused projection/query/API/governance validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 122 passed.
- 2026-05-22: Governance command regression validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"` - 118 passed.
- 2026-05-22: Full solution validation passed: `dotnet test Hexalith.Conversations.slnx` - 709 passed.
- 2026-05-22: Senior review contract validation passed after auto-fixes: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCommandAvailability|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 66 passed.
- 2026-05-22: Senior review projection/query/API/governance validation passed after auto-fixes: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 122 passed.
- 2026-05-22: Senior review governance command regression validation passed after auto-fixes: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"` - 118 passed.
- 2026-05-22: Senior review full solution validation passed after auto-fixes: `dotnet test Hexalith.Conversations.slnx` - 714 passed.

### Completion Notes List

- Added additive command availability metadata for `ActionClassification` and `RequiresFreshServerRecheck`; existing constructors remain compatible and default command metadata stays fail-closed.
- Added QA gap coverage proving valid available governance command metadata remains advisory and requires fresh server recheck metadata at the contract and query boundaries.
- Added safe validation for command action names, permissions, risk levels, classifications, and blocked reasons to reject infrastructure/client/raw-failure vocabulary and unsafe token shapes.
- Preserved `ConversationProjectionMaterializer.CreateTrustPosture()` as the server-owned command metadata source; default governance commands remain unavailable from read workflows and carry recheck/audit/freshness metadata.
- Preserved the read-only API surface: no mutation endpoints or command handoff routes were added to `ConversationReadApi`.
- Added contract, projection, query, API, and governance safety-net tests proving fail-closed defaults, advisory command metadata, GET-only read routes, client metadata rejection, stale transition clearing, and no direct read-boundary dependency on mutation handlers/audit gate/idempotency execution.
- UI/component E2E is not applicable for Story 3.5 because no `Hexalith.Conversations.Admin` or web project exists in this repository.
- Senior review auto-fixed command metadata validation so unsafe infrastructure/client/personal-data vocabulary is rejected even when attackers vary separators or casing, such as `Event Store`, `provider-payload`, `raw-exception`, and `hidden field`.
- Senior review auto-fixed command metadata validation so unavailable governed commands cannot opt out of `RequiresFreshServerRecheck`; every command handoff contract now remains server-recheck gated.

### File List

- `_bmad-output/implementation-artifacts/3-5-preserve-read-only-compliance-workflows-and-safe-command-gates.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationCommandAvailabilityContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`

## Senior Developer Review (AI)

### Review Result

Approved after automatic fixes. No critical, high, or medium issues remain.

### Review Notes

- Verified the story File List against Git status, including the untracked Story 3.5 story file and new `ConversationCommandAvailabilityContractTest.cs`.
- Cross-checked AC1-AC5 against the changed contract, projection, query, API, and governance safety-net tests.
- Confirmed the QA follow-up coverage proves valid available governance command metadata remains advisory and requires fresh server recheck metadata.
- Confirmed read API coverage remains GET-only and client-supplied command authority is ignored.
- Fixed HIGH issue: `ConversationCommandAvailabilityV1` rejected only literal forbidden vocabulary, so separator/casing variants such as `Event Store`, `provider-payload`, `raw-exception`, `browser selected`, and `hidden field` could pass into command metadata. Added normalized forbidden-vocabulary validation and regression coverage.
- Fixed MEDIUM issue: unavailable command metadata could be constructed with `RequiresFreshServerRecheck = false`, weakening the safe handoff contract for later command surfaces. The validator now requires the fresh-server-recheck flag for every command metadata instance.
- Confirmed validation evidence is current after fixes: focused contract lane 66 passed, focused server lane 122 passed, governance command regression lane 118 passed, and full solution 714 passed.

### Residual Risk

- UI/component E2E remains not applicable because this repository has no `Hexalith.Conversations.Admin` or web UI surface for Story 3.5.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 3, Story 3.5, and Stories 3.1-3.4 continuity.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on governance/audit, operator workflows, security/privacy, projection freshness, accessibility, and command-gate requirements.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on EventStore authority, projection/read model boundaries, disclosure surfaces, UX trust contract, and implementation guardrails.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`, focusing on command availability, safe action gates, transitions, and generated-first UI boundaries.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`.
  - Loaded previous Story 3.4, readiness gates, readiness decisions, recent git history, and current source/test files for command availability, trust posture, projection freshness, read API, query handler, projection materializer, and governance audit safety.
  - Checked official Microsoft documentation for ASP.NET Core route groups, Minimal API authorization/parameter binding, .NET 10 auth behavior, Blazor event handling/server-side interaction safeguards, and `dotnet test --filter`.
  - Checked Fluent UI Blazor MCP documentation for `FluentButton` and `FluentDialog` on version `5.0.0.26098`; no Conversations project currently references the package, so UI guidance is conditional.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to existing command availability, trust posture, projection, query, read API, and governance safety-net boundaries instead of a new UI shell or mutation route.
  - Added explicit guardrails for read-only no-mutation behavior, server-owned command metadata, current-freshness-only default, audit-unavailable fail-closed behavior, permission/tenant transition clearing, and client-authority rejection.
  - Added likely file touch list, focused test commands, latest technical references, prior-story lessons, and ADR stop conditions.
  - Kept Story 3.6, Story 3.7, and Story 3.8 scope out of Story 3.5 while preserving DTO/API safety requirements needed by later trust components.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture/UX guardrails, prior-story intelligence, test requirements, latest technical references, and explicit out-of-scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Change Log

- 2026-05-22: Senior developer review auto-fixed command metadata vocabulary normalization and mandatory fresh-server-recheck validation; story remains done and sprint status synced.
- 2026-05-22: Completed senior developer review for Story 3.5; no blocking issues remained after QA gap coverage, story marked done, and sprint status synced.
- 2026-05-22: Implemented Story 3.5 command-gate contract hardening, read-boundary preservation tests, safety-net coverage, and validation evidence.
- 2026-05-22: Created Story 3.5 context from Epic 3 requirements, PRD/architecture/UX/readiness/project context, previous Story 3.4 learnings, current command availability/read API/governance implementation, recent git history, official Microsoft documentation, and Fluent UI Blazor MCP documentation.
