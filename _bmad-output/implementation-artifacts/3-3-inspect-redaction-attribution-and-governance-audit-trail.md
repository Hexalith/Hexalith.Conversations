# Story 3.3: Inspect Redaction Attribution and Governance Audit Trail

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator,
I want inline redaction attribution and audit trail access,
so that I can understand why evidence changed and who authorized governance actions.

## Acceptance Criteria

1. Redaction placeholders expose only authorized attribution metadata
   - Given a conversation contains redacted or sensitive evidence,
   - When an authorized operator views the governed evidence timeline,
   - Then redaction placeholders show authorized category, policy reason class, actor attribution where allowed, timestamp, and audit reference,
   - And original redacted content is absent from visible text, hidden DTO fields, tooltips, accessible names, copied values, telemetry, logs, diagnostics, test snapshots, and responsive duplicate payloads.

2. Inline audit details are independently authorized
   - Given a governance audit trail exists,
   - When the operator opens inline audit details for a redaction, sensitivity mark, retention policy, or governance evidence anchor,
   - Then the detail result displays authorized audit entries with timestamp, actor, action, outcome, policy basis, rationale class, governed target, policy treatment, freshness, and evidence anchors,
   - And audit detail access is independently authorized from the parent conversation detail read.

3. Unavailable or withheld detail states fail closed without transition leaks
   - Given an audit or redaction detail is unavailable, unauthorized, stale, rebuilding, redacted, policy-blocked, malformed, cross-tenant, or partially withheld,
   - When the inline detail surface renders or refreshes,
   - Then it uses safe unavailable, restricted, redacted, rebuilding, hidden, or incomplete states,
   - And it does not briefly render, focus, announce, serialize, cache, or retain protected content during loading, denial, permission downgrade, tenant switch, or drawer-close transitions.

4. Tests prove audit readability and redaction non-disclosure
   - Given redaction and audit tests run,
   - When authorized audit, unauthorized audit, redacted evidence, missing audit anchor, stale projection, malformed handle, permission downgrade, and screen-reader/accessibility-label contract scenarios are exercised,
   - Then tests prove audit readability, independent audit authorization, redaction non-disclosure, content-safe accessible labels, and safe focus/transition metadata.

## Tasks / Subtasks

- [x] Extend governed detail contracts for inline redaction attribution (AC: 1, 3)
  - [x] Extend the existing `ConversationEvidenceEntryV1`, `ConversationRedactionProjectionV1`, and/or a new focused `ConversationRedactionAttributionV1` contract instead of adding a parallel transcript, viewer, or audit-store DTO stack.
  - [x] Include only safe redaction metadata: redaction category, policy reference, policy reason/rationale class, actor `PartyId` when policy allows, redaction timestamp, target kind/key, audit evidence handle, audit readiness, and safe placeholder.
  - [x] Keep original message text permanently absent after redaction materialization. Do not add fields for original content, redacted length, raw snippets, raw rationale, provider payload, upstream Party display names, EventStore stream names, storage offsets, snapshot IDs, or audit sink locations.
  - [x] Make missing redaction attribution explicit: `Unavailable`, `Incomplete`, `Redacted`, `Forbidden`, or equivalent existing vocabulary. Missing metadata must not become "safe", "current", "authorized", or "audit-ready" by default.
  - [x] Preserve additive public contract behavior and safe defaults for older projections. If a new public vocabulary is unavoidable, keep it closed, serialization-tested, and blocked by an ADR/waiver if it changes trust semantics.

- [x] Materialize redaction and governance evidence from existing projection state (AC: 1, 2, 3)
  - [x] Update `ConversationProjectionMaterializer.CreateEvidenceEntries()` and related projection contracts so message entries affected by redaction carry inline attribution metadata from `ConversationRedactionProjectionV1`, not client-side inference from placeholder text.
  - [x] Ensure redaction evidence entries and affected message entries link to the same audit evidence handle when available, while missing or unsafe handles downgrade audit readiness.
  - [x] Represent retention policy, sensitivity mark, and redaction audit anchors as governed evidence records with stable kind, timestamp, actor, policy basis, rationale class, target, trust state, audit readiness, and safe detail handle.
  - [x] Preserve chronological ordering by source event timestamp and projection ordering rules established in Story 3.2. Do not sort by hydrated labels, UI grouping, provider metadata, redaction category, or client-side arrival order.
  - [x] Keep redaction replay deterministic: replaying the same event history must produce the same placeholders, attribution metadata, audit handles, evidence-entry ordering, and freshness state.

- [x] Expose independently authorized audit-detail reads through the existing query boundary (AC: 2, 3)
  - [x] Reuse `GetConversationAuditRecordQuery`, `ConversationAuditRecordResult`, `ConversationAuditRecordDetailsV1`, and `ConversationAuditRecordAccessService`; extend them only if Story 3.3 requires additional safe inline-audit metadata.
  - [x] Add a guarded API route only if needed for this story, under the existing `/api/v1/conversations` `RequireAuthorization()` group, for example `GET /api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}`.
  - [x] Bind tenant and caller only from trusted server-side claims via `ConversationReadApi.TryGetTenantCaller()`. Do not accept tenant, caller, role, permission, trust state, audit authority, or hydration authority from route values, query string, request body, hidden fields, or client state.
  - [x] Keep malformed handles, malformed conversation IDs, unauthorized-existing records, nonexistent records, cross-tenant projection poison, stale/rebuilding/unavailable projections, and policy-blocked actions content-safe and side-channel equivalent where existence cannot be disclosed.
  - [x] Keep audit-detail reads read-only. Do not add audit export, evidence bundle generation, mutation, separate durable audit cache, audit search index, background worker, recent-audit state, or UI command execution in this story.

- [x] Add trust-component-ready safe display metadata without building the full admin shell (AC: 1, 3, 4)
  - [x] Provide safe labels, safe next actions, drawer titles, and accessibility-label-ready fields only from server-owned projection/audit metadata. UI code must not infer redaction reason, audit readiness, actor authority, freshness, or command availability from display text.
  - [x] If an Admin/FrontComposer project exists by implementation time, place custom trust primitives under the established admin/trust component boundary and use FrontComposer/Fluent UI conventions. If no Admin project exists, do not create a full admin shell just for this story; keep the slice in contracts/server/API tests and record UI E2E as not applicable.
  - [x] Drawer/detail metadata must authorize before detail content is resolved. Denied or downgraded states must clear protected detail and return focus to a safe summary target; DTOs should include enough safe state for later UI implementation to do this without guessing.
  - [x] Keep mobile/responsive duplicate verification, clipboard/browser-title leak scanning, broad accessibility-tree verification, and Leak Sentinel automation scoped to Stories 3.8A-3.8C unless a minimal contract test is needed here.

- [x] Add focused contract, server, projection, API, and safety tests (AC: 1-4)
  - [x] Extend `ConversationEvidenceContractTest` or add a focused contract test for redaction attribution JSON shape, safe defaults, safe labels/accessibility text, audit-handle linkage, forbidden original-content fields, and forbidden EventStore/provider/audit-storage vocabulary.
  - [x] Extend `AuditRecordGovernanceContractTest` if audit detail contracts gain fields for inline detail display, drawer state, or policy treatment.
  - [x] Extend `ConversationProjectionMaterializerTest` for redacted message attribution, redaction evidence-entry linkage, missing audit handle downgrade, sensitivity/retention audit anchors, replay determinism, and no original text in projection/evidence entries.
  - [x] Extend `ConversationAuditRecordAccessServiceTest` for independent authorization after parent detail visibility, malformed handle, cross-tenant projection poison, unavailable store exceptions, stale/rebuilding projections, policy-blocked detail, expired/redacted audit treatment, and redaction audit details.
  - [x] Extend `ConversationQueryHandlerTest` and `ConversationReadApiTest` if an API route is added, proving trusted claim binding, route group authorization metadata, hidden/unavailable shapes, no unsafe exception terms, and no client-supplied tenant/caller authority.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` after implementation with Story 3.3 evidence and whether UI E2E was applicable.

- [x] Preserve scope boundaries and ADR stop conditions (AC: 1-4)
  - [x] Do not create a transcript table, durable audit detail store, export artifact, evidence bundle, cache authority, search index, Memories/RAG index, queue worker, browser storage, or new projection authority without accepted ADR or waiver coverage.
  - [x] Do not implement citation copy or stable temporal evidence link opening from Story 3.4.
  - [x] Do not implement read-only compliance command gates or privileged action execution from Story 3.5.
  - [x] Do not implement governance verification runner/results from Story 3.6, buyer demo fixtures from Story 3.7, or broad responsive/accessibility/leak sentinel gates from Stories 3.8A-3.8C.
  - [x] Stop for ADR/waiver if implementation needs a new public trust state, audit authority model, durable retention/export surface, raw audit sink dependency, new privileged execution path, or a rule that allows non-current projections to drive trust-bearing audit details.

## Dev Notes

### Epic and Business Context

- Epic 3 delivers the compliance investigation workspace: compliance operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios. Story 3.3 is the redaction/audit inspection slice after Story 3.2 established the governed detail read. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Compliance Investigation Workspace`]
- Story 3.3 covers FR59 and FR60: operators inspect inline redaction attribution and view the governance audit trail inline. It also maps to UX-DR6-UX-DR8, UX-DR12, UX-DR15-UX-DR17, UX-DR20, UX-DR26, and UX-DR33. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Inspect Redaction Attribution and Governance Audit Trail`; `_bmad-output/planning-artifacts/ux-requirement-map.md`]
- The product intent is a governed case file, not a casual transcript. Timeline entries, redactions, citations, audit events, and degraded states must appear as evidence in context. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Core User Experience`]

### Ready-for-Dev Preconditions

- Projection freshness blocking semantics are decided. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; only `Current` is acceptable for trust-bearing audit detail unless a story explicitly records an exception. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- Command availability metadata is server-owned and missing/stale/ambiguous metadata disables unsafe actions. Story 3.3 should render read-only audit/redaction detail state; command execution remains Story 3.5. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Command availability metadata`]
- Party hydration degraded states are decided. Authorized reads may show safe `PartyId` attribution and degraded display state, but durable state must not store Party personal data and audit detail must not expose upstream Party details. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`]
- UX safety gate ownership is recorded as single-threaded through Architect capacity. Keep this story narrowly focused on redaction/audit contracts, projection materialization, query/API behavior, and minimal trust-component-ready metadata. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Architect and second-engineer availability`]

### Current Implementation State

- Story 3.2 added `ConversationEvidenceTrustPostureV1`, `ConversationEvidenceEntryV1`, and `ConversationCommandAvailabilityV1` to the governed detail read. Detail responses serialize trust posture and evidence entries before message-shaped data. Extend this path instead of creating a parallel viewer. [Source: `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md`; `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`; `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`]
- `ConversationRedactionProjectionV1` already carries target, redaction category, safe policy reference, reason class, optional actor `PartyId`, timestamp, optional `GovernanceAuditEvidenceReference`, trust state, and safe placeholder. Use it as the redaction attribution source. [Source: `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs`]
- `ConversationProjectionMaterializer` already suppresses redacted message text to `[redacted]`, creates separate `Redaction` evidence entries, and marks affected message evidence as `Redacted`. It does not yet attach full redaction attribution metadata to affected message entries or expose an inline audit-detail-ready model. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`]
- `ConversationAuditRecordAccessService` already resolves audit details from active retention policy, sensitivity marks, and redactions through tenant authorization, projection freshness checks, handle parsing, policy treatment, and safe hidden/unavailable/rebuilding/policy-blocked results. Build on it rather than adding a second audit detail service. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`]
- `ConversationQueryHandler.GetAuditRecordAsync()` already exposes the service internally, but `ConversationReadApi` currently maps only list and detail routes. If Story 3.3 needs HTTP audit detail access, add the route under the existing authorized route group. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`; `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`]
- There is no `Hexalith.Conversations.Admin` project in the current source tree. Do not scaffold a full admin UI unless an approved implementation decision promotes that scope; provide safe DTO/component metadata for later FrontComposer/Admin work. [Source: local source tree inspection; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]

### Architecture Guardrails

- Public APIs expose Conversations contracts and typed result shapes, not raw EventStore envelopes, stream names, event positions as storage authority, snapshots, projection topology, SignalR groups, provider sessions, or audit sink internals. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Tenant access fails closed before aggregate load, command dispatch, projection read, export, rebuild, admin action, MCP/tool action, background job execution, or verification detail access. Audit detail is a projection-backed read and must keep tenant authorization first. [Source: `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Projections are derived and repairable. Redaction/audit detail may optimize reads but must remain reconstructable from EventStore events plus approved read-time sources; it must not become a new authority. [Source: `_bmad-output/planning-artifacts/architecture.md#State Management Patterns`]
- Redacted content must not reappear in primary projections, audit views, caches, exported reports, temporal views, replay/rebuild outputs, logs, traces, errors, observability payloads, hidden DOM, accessible names, tooltips, clipboard data, or responsive duplicate layouts. [Source: `_bmad-output/planning-artifacts/prd.md#Security And Privacy`; `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Design & Accessibility`]
- Party personal data is read-time hydration only. Redaction attribution can carry stable `PartyId` and safe degraded state, but not display names, contact values, identifiers, raw upstream problem details, or Party-owned personal data. [Source: `_bmad-output/project-context.md#Critical Implementation Rules`; `Hexalith.Parties/_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### UX Requirements for Redaction and Audit Detail

- Redaction placeholders are trust-bearing surfaces. They must show safe category/reason/attribution metadata only when authorized and must never hide original content in CSS, hidden DOM, accessible names, tooltips, copied text, diagnostics, telemetry, or alternate layout payloads. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Visual Design System`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR6`]
- Audit markers and evidence anchors must sit close to the timeline entries they explain. Inline detail should use drawer-style interaction metadata, but the source of truth remains server-owned projection/audit metadata. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Information Architecture`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR7`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR33`]
- Evidence drawers independently authorize and close/clear protected content on permission downgrade. This applies even when the parent timeline entry remains visible as a redacted placeholder. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR20`; `_bmad-output/planning-artifacts/ux-design-specification.md#Security And Privacy Considerations`]
- Trust primitives are data-bound renderers, not policy engines. A future UI may format redaction/audit metadata, but it must not decide permission, freshness, eligibility, redaction, or audit readiness. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Component Architecture`]

### Previous Story Intelligence

- Story 3.2 established the exact implementation pattern for Epic 3 read slices: extend existing contracts, keep tenant authorization and freshness evaluation in `ConversationProjectionReadService`, hydrate only after accepted projection reads, and add focused contract/server/API tests before full solution validation. [Source: `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md#Dev Agent Record`]
- Story 3.2 review fixed evidence-entry chronology, fail-closed participant-resolution aggregation, and projection-store exception coarsening. Story 3.3 must preserve chronological evidence ordering and coarse infrastructure failures to content-safe unavailable results. [Source: `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md#Senior Developer Review (AI)`]
- Story 3.1 review fixed overconfident fallback trust metadata for older projections and malformed query input that did not fail closed. Story 3.3 must default older/missing redaction or audit metadata to unavailable/incomplete and reject malformed audit handles without target disclosure. [Source: `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md#Senior Developer Review (AI)`]
- Story 2.7 already created governed audit-record contracts and access tests. Reuse the `ConversationAuditRecordAccessService` shape for read/export policy treatment; do not add unmanaged export or alternate audit retrieval. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md#Story 2.7 Audit Record Governance Evidence`]
- Story 2.4 redaction evidence proves redaction replay, audit-pairing, command boundary safety, and placeholder suppression. Story 3.3 should surface that proof through governed reads, not reopen the write-side redaction command design. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md#Story 2.4 Redaction Evidence`]

### Git Intelligence

- Recent commits show the established sequence: `0fda25e feat(story-3.2): Read Governed Conversation Evidence`, `5825a54 feat(story-3.1): Find Tenant-Scoped Conversations Safely`, `14ef92a feat(story-2.8): Record and Review Privileged Operational Justification`, `01f58ae feat(story-2.7): Govern Audit Record Access Retention and Redaction`, and `eb2f625 feat(story-2.6): Reconstruct Point-in-Time Governance State`.
- Follow the same shape: additive public contracts, projection-owned trust metadata, server-side guard ordering, fail-closed result shaping, focused tests, test summary update, then full solution validation.

### File Structure Requirements

- Likely UPDATE files:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordDetailsV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordResult.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/GetConversationAuditRecordQuery.cs`
  - `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
  - `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs`
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
  - `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/AuditRecordGovernanceContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Likely NEW files, only if useful:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationRedactionAttributionV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceDetailLinkV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditDetailAvailabilityV1.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationRedactionAttributionContractTest.cs`
- Keep public DTOs in `Contracts`, projection/materialization in `Server/Projections`, authorization/query orchestration in `Server/Queries`, HTTP binding in `Server/Api`, and tests beside the affected project.
- There is no expected need for a new package dependency. If one becomes necessary, use Central Package Management through `Directory.Packages.props`; project `PackageReference` entries remain versionless.

### Testing Requirements

- Run focused contract tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationEvidence|FullyQualifiedName~Redaction|FullyQualifiedName~AuditRecord|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
- Run focused server projection/query/API tests:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"`
- Run regression coverage around tenant access, hydration, temporal/rebuild boundaries, and Story 3.2 detail reads if shared contracts are touched:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~Temporal|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationQueryRegistrationTest"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`
- Microsoft documentation confirms `dotnet test --filter` supports `FullyQualifiedName~...` contains expressions and boolean composition. ASP.NET Core Minimal API route groups support shared metadata such as `RequireAuthorization()`, and special types such as `HttpContext`, `ClaimsPrincipal`, and `CancellationToken` can be bound by route handlers. Keep explicit manual parsing where Conversations needs hidden-shape side-channel equivalence. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`; `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`; `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`]

### Latest Technical Information

- ASP.NET Core route groups can apply authorization metadata once to a shared endpoint prefix. If adding an audit-detail endpoint, keep it under the existing authorized group instead of duplicating authorization metadata per route. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`]
- Minimal API binding can infer route, query, DI, and special types, but automatic binding failures can produce framework-shaped responses. Continue manual parsing for safety-sensitive route values and handles where hidden/unavailable response equivalence matters. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0#binding-precedence`]
- NuGet Central Package Management keeps versions in `Directory.Packages.props`; with CPM, `PackageReference` items should not carry inline `Version` attributes. [Source: `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Out of Scope

- Do not implement Story 3.4 citation copy, clipboard behavior, or stable temporal evidence link opening.
- Do not implement Story 3.5 read-only command gates, privileged command execution, governance-changing confirmation dialogs, or command mutation routes.
- Do not implement Story 3.6 governance verification runner/results, Story 3.7 buyer acceptance demo, or Story 3.8 responsive/accessibility/leak sentinel evidence gates.
- Do not build a full Admin/FrontComposer UI shell if it does not exist. This story may prepare trust-component-safe contracts and metadata, but the full UI verification matrix belongs to later stories unless explicitly promoted.
- Do not add audit exports, evidence bundles, durable audit caches, transcript tables, alternate read stores, Memories/RAG indexes, autocomplete/recent-history stores, or background workers.
- Do not mutate conversation aggregate state from Find/Read/Trust/audit-detail paths.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.3: Inspect Redaction Attribution and Governance Audit Trail`
- `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/architecture.md#State Management Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Component Architecture`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Security And Privacy Considerations`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Design & Accessibility`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md`
- `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `docs/adrs/index.md`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationAuditRecordQuery.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/AuditRecordGovernanceContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`
- `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationEvidence|FullyQualifiedName~Redaction|FullyQualifiedName~AuditRecord|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 32 passed after review fixes.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~ConversationReadApiTest"` - 35 passed after review fixes.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 94 passed after review fixes.
- 2026-05-22: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~Temporal|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationQueryRegistrationTest"` - 152 passed.
- 2026-05-22: `dotnet test Hexalith.Conversations.slnx` - 665 passed after review fixes.

### Completion Notes List

- Added `ConversationRedactionAttributionV1` and additive `ConversationEvidenceEntryV1` metadata for safe redaction attribution, governed targets, audit evidence links, rationale class, labels, accessibility text, and next actions.
- Updated projection materialization so redacted message evidence and redaction evidence use `ConversationRedactionProjectionV1` metadata, link to the same audit handle, preserve redacted placeholders, and expose safe retention/sensitivity/redaction audit anchors.
- Added `GET /api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}` under the existing authorized read route group, reusing `ConversationAuditRecordAccessService` and trusted claim binding.
- Added focused contract, projection, API, and regression tests; UI E2E is not applicable because no Admin/FrontComposer project exists for this story.
- Added QA gap tests for permission-downgraded inline audit detail clearing and redaction audit records with missing audit anchors.
- Review hardened redaction contracts so placeholders are canonical, attribution target keys match governed targets, redacted visible text cannot diverge from the placeholder, and redaction attribution readiness cannot be masked by a ready evidence-entry state.

### File List

- `_bmad-output/implementation-artifacts/3-3-inspect-redaction-attribution-and-governance-audit-trail.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Conversations.Contracts/Governance/GovernanceContractValidation.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationRedactionAttributionV1.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Senior Developer Review (AI)

Reviewer: Jerome on 2026-05-22

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- HIGH: Redaction placeholder contracts accepted arbitrary visible text, so a malformed projection/DTO could serialize message text as a redaction placeholder. Fixed by enforcing the canonical `[redacted]` marker in `ConversationRedactionProjectionV1` and `ConversationRedactionAttributionV1`.
- HIGH: Redacted evidence entries could carry visible text that did not match the safe redaction attribution placeholder. Fixed by validating redacted entry visible text against the canonical placeholder and the attached attribution.
- HIGH: Redaction attribution audit readiness could disagree with the evidence entry audit readiness, allowing missing redaction audit metadata to be masked by a ready parent entry. Fixed by requiring readiness consistency and by using redaction-specific readiness for redacted message evidence in the materializer.
- MEDIUM: Redaction attribution accepted a caller-supplied target key that could drift from the governed target object. Fixed by validating `TargetKey` against `GovernanceTarget.ToTargetKey()`.
- MEDIUM: Audit-record projection read failures only coarsened selected exception types; unexpected store failures could escape the service boundary. Fixed by returning content-safe unavailable audit-record results for non-cancellation projection read exceptions.
- MEDIUM: The audit HTTP route lacked direct coverage for missing trusted tenant claims and unexpected store failures. Fixed with API tests proving hidden/no-read behavior for missing claims and content-safe 503 behavior for audit store failures.
- MEDIUM: The story File List omitted changed contract/support files. Fixed by adding the governance validation, redaction projection, and audit access service files.

Validation completed:

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationEvidence|FullyQualifiedName~Redaction|FullyQualifiedName~AuditRecord|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 32 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 94 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~Temporal|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationQueryRegistrationTest"` - 152 passed.
- `dotnet test Hexalith.Conversations.slnx` - 665 passed.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 3, Story 3.1, Story 3.2, Story 3.3, and Story 3.4 boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR59-FR60, FR56-FR69, NFR16-NFR21, projection freshness, redaction non-leakage, accessibility, and release-evidence obligations.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on public contract boundaries, EventStore authority, tenant fail-closed reads, projection repairability, Party hydration, FrontComposer boundaries, ADR triggers, and testing patterns.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`, focusing on redaction safety, audit markers, evidence drawers, custom trust primitives, detail authorization, safe accessible output, and drawer/dialog patterns.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md` and sibling EventStore, Tenants, Parties, and FrontComposer project contexts relevant to EventStore authority, tenant fail-closed checks, Party personal-data ownership, FrontComposer generated-first boundaries, CPM, and submodule rules.
  - Loaded previous Story 3.2 and Story 3.1 context and recent git history.
  - Read current implementation files for governed detail contracts, evidence entries, redaction projections, audit-record query contracts, audit access service, query handler, read API, projection materializer, and focused tests.
  - Checked official Microsoft documentation for ASP.NET Core Minimal API route groups, parameter binding, `dotnet test --filter`, and NuGet Central Package Management.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to existing governed detail/audit query boundaries instead of a new transcript, audit store, or UI shell.
  - Added explicit guardrails for redaction attribution, safe placeholders, audit handle linkage, independent audit authorization, transition safety, and current-only audit reliance.
  - Preserved Story 3.4/3.5/3.6/3.7/3.8 boundaries so later citation, command gate, verification, buyer demo, responsive, accessibility, and leak-sentinel work does not leak into Story 3.3.
  - Added likely file touch list, focused test commands, ADR stop conditions, and latest Microsoft documentation references.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture/UX guardrails, prior-story learnings, test requirements, and explicit out-of-scope boundaries.

## Change Log

- 2026-05-22: Review auto-fixed audit-record store exception coarsening, audit route missing-claim/store-failure test coverage, story File List completeness, and refreshed validation evidence.
- 2026-05-22: Senior developer review auto-fixed redaction placeholder, target-key, visible-text, and audit-readiness contract invariants; updated validation evidence and marked the story done.
- 2026-05-22: Implemented Story 3.3 inline redaction attribution, governed audit-detail API access, safe trust-component metadata, focused tests, QA gap tests, and validation evidence.
- 2026-05-22: Created Story 3.3 context from Epic 3 requirements, PRD/architecture/UX/readiness/project context, previous Story 3.1/3.2 learnings, current redaction/audit/detail-query implementation, recent git history, and official Microsoft documentation.
