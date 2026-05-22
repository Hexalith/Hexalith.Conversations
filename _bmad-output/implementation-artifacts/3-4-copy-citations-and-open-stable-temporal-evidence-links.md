# Story 3.4: Copy Citations and Open Stable Temporal Evidence Links

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator,
I want citation-ready references and stable temporal links,
so that I can cite conversation and audit evidence without exporting unsafe content.

## Acceptance Criteria

1. Citation copy uses permission-safe citation DTOs after authorization recheck
   - Given an operator is authorized to cite a transcript, evidence timeline, redaction, retention, sensitivity, freshness, or audit element,
   - When citation copy is requested,
   - Then the copied value is built from a Conversations-owned permission-safe citation DTO after tenant/caller authorization and projection freshness recheck,
   - And it includes only approved citation fields such as citation id, tenant scope, conversation id, evidence entry id, evidence kind, ISO timestamp, actor PartyId where authorized, audit evidence handle where available, temporal cursor, projection version/cursor, contract version, freshness/completeness state, and safe label,
   - And it omits redacted content, original message text after redaction, unauthorized participant details, Party personal data, provider payloads, EventStore stream/snapshot/envelope internals, storage offsets, raw exception text, hidden component fields, rendered-text-only values, and browser-selected text.

2. Stable temporal evidence links resolve deterministically
   - Given a temporal evidence link is opened for an authorized tenant and conversation,
   - When the link resolves by the v1 authoritative anchor,
   - Then it resolves through the composite v1 temporal cursor: safe source event position plus projection version/cursor, with timestamp as supporting metadata only,
   - And it returns the same legally meaningful conversation state for the same event history, projection version, contract version, tenant scope, conversation identity, and policy scope,
   - And the response states the authoritative anchor, projection freshness, completeness/confidence, current disclosure policy treatment, and safe next action.

3. Missing, redacted, stale, unavailable, malformed, cross-tenant, or unauthorized targets fail closed
   - Given the citation or temporal target is missing, deleted, redacted, stale, rebuilding, unavailable, malformed, cross-tenant, outside coverage, unsupported-version, policy-blocked, or unauthorized,
   - When citation copy or temporal resolution is requested,
   - Then the result is hidden, unavailable, rebuilding, redacted, incomplete, or denied with content-safe shape and side-channel-equivalent behavior where existence cannot be disclosed,
   - And it does not hide broken evidence behind a trusted citation state or reveal protected existence through URLs, route labels, browser title, empty states, clipboard output, accessible names, telemetry, timing-sensitive totals, or diagnostic text.

4. Tests prove safe citation output and temporal-link behavior
   - Given citation and temporal tests run,
   - When copy request, citation DTO serialization, malformed cursor, stale projection, redacted target, missing audit handle, deleted evidence, outside coverage, unsupported schema, cross-tenant link, unauthorized-existing record, permission downgrade, and minimal clipboard/browser/accessibility metadata scenarios are exercised,
   - Then tests prove safe citation output, stable temporal re-resolution, tenant isolation, current-only trust reliance, no EventStore/provider internals, no original redacted content, and no leakage through URLs, clipboard-ready text, browser-title-ready labels, or accessibility-label-ready fields.

## Tasks / Subtasks

- [x] Define citation contracts on the existing governed evidence model (AC: 1, 3, 4)
  - [x] Extend `ConversationEvidenceEntryV1` and `ConversationEvidenceTrustPostureV1` only as needed, or add focused contracts such as `ConversationCitationV1`, `ConversationCitationResult`, `ConversationCitationTargetV1`, and `GetConversationCitationQuery`. Do not add a parallel transcript, evidence bundle, export, search index, or UI-owned citation model.
  - [x] Citation DTOs must be server-owned, serialization-friendly, additive, and closed over safe vocabulary. Required safe fields should include schema version, tenant id, conversation id, evidence entry id, evidence kind, occurred-at timestamp, trust state, citation availability, audit readiness, projection cursor/version, temporal cursor, and safe copied text.
  - [x] For audit-linked entries, include `GovernanceAuditEvidenceReference` or its safe handle/policy/timestamp values only when already authorized. Missing audit handles must produce `Incomplete` or `Unavailable`, not a trusted citation.
  - [x] For redacted entries, citation text must use the canonical redaction placeholder and redaction attribution metadata from `ConversationRedactionAttributionV1`; original content must remain absent from DTOs, copied text, safe labels, tests, and diagnostics.
  - [x] Do not expose display names, contact values, Party-owned personal data, provider correlation payloads, raw EventStore names, storage offsets, snapshot ids, aggregate stream names, or unbounded raw business identifiers.

- [x] Build citation values from projection and audit metadata after reauthorization (AC: 1, 3)
  - [x] Add citation resolution to `ConversationQueryHandler` through a focused service if useful, for example `ConversationCitationAccessService`, reusing `ConversationProjectionReadService.ReadDetailAsync()` and `ConversationAuditRecordAccessService` behavior rather than replaying EventStore history for ordinary citation copy.
  - [x] Recheck tenant/caller authorization from trusted server-side claims and current projection freshness before creating citation output. Only `ProjectionFreshnessV1.AllowsTrustBearingDecision()` may enable trusted citation copy unless this story records a narrower explicit exception.
  - [x] Resolve citation targets against `ConversationDetailsV1.EvidenceEntries`, redaction attribution, and audit evidence handles. Missing targets, stale projections, malformed ids, missing audit handles, and policy-blocked details must downgrade safely.
  - [x] Citation output must be generated from DTO fields, not from rendered DOM text, selected text, table cells, hidden inputs, component state, URL fragments, local storage, session storage, browser title, telemetry, or client-supplied tenant/caller/permission values.
  - [x] Keep copy as a read-only operation. Do not create an audit-export, evidence-bundle, persistent copied-citation log, durable citation table, background worker, or mutation command in this story.

- [x] Formalize temporal-link anchors and resolution over the existing temporal reconstruction path (AC: 2, 3, 4)
  - [x] Use the readiness-gate decision: v1 temporal evidence links carry tenant scope, conversation identity, safe source event position, projection cursor/version, contract version, and authorization recheck behavior. Timestamp is supporting display/correlation metadata only.
  - [x] Reuse `ConversationTemporalAnchorV1`, `GetConversationAtPointInTimeQuery`, `ConversationTemporalDetailResult`, and `ConversationTemporalReconstructionService`. Extend them only if the current contracts cannot carry the composite anchor and citation-safe labels.
  - [x] Add a stable contract cursor format only if the existing `temporal:v1:pos:{position}` form is insufficient. If extended, include projection cursor/version in the signed or validated payload and reject ambiguous/mismatched cursor forms.
  - [x] Preserve current temporal behavior: tenant access before event-source read, anchor tenant/conversation match before replay, current projection disclosure policy available before historical details, redacted current policy applied to historical message text, and hidden/unavailable/rebuilding results for gaps or outside coverage.
  - [x] Do not treat timestamps as authoritative legal anchors. Timestamp anchors may remain accepted request forms, but returned authoritative anchors must resolve to the safe source position plus projection version/cursor contract.

- [x] Add guarded HTTP route(s) only under the existing read API boundary (AC: 1, 2, 3)
  - [x] If an HTTP surface is needed, add it under the existing `/api/v1/conversations` group in `ConversationReadApi`, which already applies `RequireAuthorization()`.
  - [x] Candidate read-only routes: `GET /api/v1/conversations/{conversationId}/citations/{evidenceEntryId}` for a citation DTO and `GET /api/v1/conversations/{conversationId}/temporal` with a safe cursor query value for temporal resolution. Adjust exact shapes to fit implementation, but keep side-channel-safe parsing.
  - [x] Continue binding tenant from the authenticated `tid` claim, caller from `ClaimTypes.NameIdentifier`, and correlation from `X-Correlation-Id`. Never accept tenant, caller, role, permission, trust state, audit authority, hydration authority, or policy authority from route values, query strings, request bodies, hidden fields, or client state.
  - [x] Manually parse safety-sensitive ids/cursors instead of relying on framework model-binding failure shapes where hidden/unavailable equivalence matters.
  - [x] Map malformed, unauthorized, nonexistent, cross-tenant, outside-coverage, stale/rebuilding, and unavailable outcomes to the existing hidden/unavailable/rebuilding result style without leaking target existence.

- [x] Provide trust-component-ready copy/link metadata without building a full Admin shell (AC: 1, 3, 4)
  - [x] There is currently no `Hexalith.Conversations.Admin` project. Do not scaffold a full FrontComposer/Admin UI solely for this story unless a separate approved implementation decision promotes that scope.
  - [x] Provide safe labels, copy button labels, copied-text values, link labels, unavailable reasons, browser-title-ready text, and accessibility-label-ready fields only from server-owned citation/temporal DTOs.
  - [x] If an Admin/FrontComposer project exists by implementation time, put custom citation and temporal-navigation trust primitives under the established admin trust-component boundary and use FrontComposer/Fluent UI conventions.
  - [x] Treat clipboard and URLs as disclosure surfaces. Clipboard-ready output must be an explicit DTO field; temporal URLs must carry only safe opaque or contract-defined cursor values and never embed titles, participant names, snippets, redacted content, raw business references, provider ids, or Party personal data.
  - [x] Keep broad desktop/tablet/mobile responsive verification, full accessibility-tree scanning, browser-title leak scanning, telemetry leak scanning, screenshot checks, and Leak Sentinel automation scoped to Stories 3.8A-3.8C unless minimal contract/API tests are needed here.

- [x] Add focused contract, server, API, temporal, and safety tests (AC: 1-4)
  - [x] Extend `ConversationEvidenceContractTest` or add `ConversationCitationContractTest` for citation DTO JSON shape, copied-text shape, safe labels, redaction placeholder behavior, missing-audit downgrade, temporal cursor presence, and forbidden vocabulary.
  - [x] Extend `TemporalReconstructionContractTest` for composite authoritative anchor output, projection cursor/version metadata, timestamp-as-supporting-metadata behavior, and serialization without storage/provider internals.
  - [x] Extend `ConversationTemporalReconstructionServiceTest` for deterministic re-resolution by safe source position plus projection cursor/version, malformed cursor, cross-tenant cursor, stale/rebuilding current projection, incomplete temporal source, unsupported schema, and redacted current policy over historical content.
  - [x] Extend `ConversationQueryHandlerTest` and/or new citation service tests for authorized citation, missing evidence entry, redacted target, missing audit handle, stale projection, projection store failure, unauthorized-existing record, and cross-tenant projection poison.
  - [x] Extend `ConversationReadApiTest` if routes are added, proving route-group authorization metadata, trusted claim binding, manual cursor parsing, 404 hidden equivalence, 503 unavailable/rebuilding behavior, and no unsafe response terms.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` after implementation with Story 3.4 evidence and note whether UI E2E was applicable.

- [x] Preserve scope boundaries and ADR stop conditions (AC: 1-4)
  - [x] Do not implement Story 3.5 read-only command gates, privileged command execution, governance-changing confirmation dialogs, or command mutation routes.
  - [x] Do not implement Story 3.6 governance verification runner/results, Story 3.7 buyer demo fixtures, or broad Story 3.8 responsive/accessibility/leak-sentinel gates.
  - [x] Do not implement signed evidence bundle export, audit export, durable citation history, browser storage, recent-citation lists, transcript tables, alternate read stores, Memories/RAG indexes, search/autocomplete indexes, queue workers, or new projection authorities.
  - [x] Stop for ADR/waiver if implementation needs a new durable store, public trust state, raw EventStore cursor exposure, non-current projection reliance, legal-anchor change away from safe source position plus projection version, new privileged execution path, or retention/export behavior outside the current v1 lifecycle decision.

## Dev Notes

### Epic and Business Context

- Epic 3 delivers the compliance investigation workspace: compliance operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios. Story 3.4 is the Cite and stable temporal-link slice after Story 3.3 made redaction attribution and audit detail inspectable. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.4: Copy Citations and Open Stable Temporal Evidence Links`]
- Story 3.4 covers FR62 and FR63. FR62 requires citation-ready references for transcript and audit elements. FR63 requires stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by contract. [Source: `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`]
- UX mapping: UX-DR7 evidence cues, UX-DR10 citation and temporal reconstruction, UX-DR26 evidence detail components, and UX-DR35 copy/export safety. The UX explicitly requires copy behavior from permission-safe DTOs after authorization recheck, not rendered text selection. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md`]
- Product carry-forward requires every Read-view element to have a paste-ready citation block with audit ID, ISO timestamp, actor, hash/integrity metadata where available, conversation ID, and tenant ID. That is a product goal, but the implementation must still omit unauthorized or redacted details. [Source: `_bmad-output/planning-artifacts/prd.md#carryForwardCallouts`]

### Ready-for-Dev Preconditions

- Temporal evidence anchor is decided. v1 uses a composite temporal cursor of EventStore event position plus projection version. Timestamp is supporting display/correlation metadata only. Temporal links must carry tenant scope, conversation identity, event position, projection version, contract version, and authorization recheck behavior. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Temporal evidence anchor`]
- Command availability metadata is decided and server-owned. Story 3.4 should not depend on client-side command eligibility, but if citation/link affordances render beside commands, missing/stale/ambiguous metadata must disable unsafe actions. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Command availability metadata`]
- Projection freshness blocking semantics are decided. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; only `Current` enables trust-bearing citation copy or temporal reliance unless a story records an explicit exception. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- UX safety gate ownership is recorded as single-threaded through Architect capacity, and Story 3.8 is split into 3.8A responsive/mobile, 3.8B accessibility, and 3.8C leakage/clipboard/browser/telemetry gates. Keep Story 3.4 focused on contracts/server/API and minimal trust-component-safe metadata. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Story 3.8 assignment plan`]

### Current Implementation State

- Story 3.2 added `ConversationEvidenceTrustPostureV1`, `ConversationEvidenceEntryV1`, and `ConversationCommandAvailabilityV1` to the governed detail read. Story 3.4 should extend this path instead of creating a separate transcript or evidence viewer. [Source: `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md`; `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`; `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`]
- Story 3.3 added `ConversationRedactionAttributionV1`, safe redaction metadata on evidence entries, and the audit-record route `GET /api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}` under the existing authorized read group. Build citation-to-audit behavior on this, not a second audit access service. [Source: `_bmad-output/implementation-artifacts/3-3-inspect-redaction-attribution-and-governance-audit-trail.md`; `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`; `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`]
- `ConversationEvidenceEntryV1` currently has entry id, kind, actor PartyId, timestamp, trust state, citation availability, audit readiness, degraded state, message/file ids, visible text, provider correlation, policy reference, governed target, rationale class, audit evidence, safe labels, safe next action, and redaction attribution. It enforces canonical redacted visible text. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`]
- `ConversationProjectionMaterializer.CreateEvidenceEntries()` currently marks citation availability as available only when freshness allows trust-bearing decisions, links redacted message evidence to redaction audit metadata, and produces evidence entries for freshness, messages, participants, attachments, retention, sensitivity, and redaction. Citation copy should consume this evidence-entry model. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`]
- Temporal reconstruction contracts and service already exist: `ConversationTemporalAnchorV1`, `GetConversationAtPointInTimeQuery`, `ConversationTemporalDetailsV1`, `ConversationTemporalDetailResult`, and `ConversationTemporalReconstructionService`. There is no HTTP route for temporal reconstruction yet in `ConversationReadApi`. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs`; `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`; `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`]
- `ConversationTemporalReconstructionService` already authorizes tenant read access before event-source reads, rejects mismatched anchor tenant/conversation, resolves safe position and cursor anchors, checks current projection availability before historical replay, applies current redaction policy to historical messages, and returns hidden/unavailable/rebuilding on unsafe states. Preserve these ordering guarantees. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`]
- `ConversationQueryCursor` is a signed opaque list continuation cursor and is not a temporal evidence cursor. Do not reuse list cursor semantics for legal temporal anchors unless a new purpose-bound cursor contract is created and tested. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs`]
- There is no `Hexalith.Conversations.Admin` project in the current source tree. UI implementation is limited unless the Admin/FrontComposer project exists by the time Story 3.4 is developed. [Source: local source tree inspection; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]

### Architecture Guardrails

- Public APIs must expose Conversations contracts, typed results, version metadata, safe trust/freshness states, and citation/temporal DTOs. They must not expose EventStore envelopes, stream names, raw event-store topology, snapshot mechanics, projection topology, SignalR groups, provider sessions, tokens, claims, or raw audit sink internals. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Tenant access fails closed before projection read, temporal reconstruction, audit detail, citation generation, export, rebuild, admin action, MCP/tool action, or verification detail. Citation and temporal-link paths are read paths, but they still require the same tenant and projection gates as detail reads. [Source: `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Projection freshness metadata is part of every trust-bearing read. Citation status must never be inferred from non-empty messages, visible labels, URL availability, copied text success, or UI state. [Source: `_bmad-output/planning-artifacts/architecture.md#API Response Formats`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR3`]
- Redacted content must not reappear in projections, audit views, temporal views, copied citation text, URLs, accessibility labels, browser titles, logs, traces, errors, observability payloads, screenshots, caches, exports, or derived indexes. [Source: `_bmad-output/planning-artifacts/prd.md#Security And Privacy`; `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Design & Accessibility`]
- Party personal data is read-time hydration only. Citation output may include stable `PartyId` when authorized, but not display names, contact values, identifiers, person details, organization details, or raw upstream problem details. [Source: `_bmad-output/project-context.md#Critical Implementation Rules`]
- Temporal reconstruction, export, verification, and rebuild use bounded workflows with status/trust metadata. Ordinary citation/link reads must not perform unbounded replay unless they are explicitly using the temporal reconstruction service and returning confidence-limited results. [Source: `_bmad-output/planning-artifacts/architecture.md#Read-Model And Performance Implications`]

### UX Requirements for Citation and Temporal Links

- Citation controls are trust-bearing surfaces. They must display availability and safe next action from Conversations-owned projection/citation metadata, not from DOM availability or client-side inference. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR10`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR17`]
- Copy/export safety requires copied citations, summaries, rows, timeline entries, or evidence details to be built from permission-safe DTOs after authorization recheck. Story 3.4 covers citation copy; export/evidence bundle remains out of scope. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR35`]
- Evidence detail drawers are the intended interaction pattern for citation and audit linkage, but drawer content must independently authorize and clear protected content on denial or permission downgrade. Full drawer behavior and focus verification may wait for Admin/3.8 if no UI exists. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR20`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR33`]
- URLs, browser titles, route labels, breadcrumbs, clipboard payloads, hidden DOM, ARIA labels, live regions, tooltips, and responsive duplicate layouts are disclosure surfaces. Minimal Story 3.4 contract tests should prevent unsafe copy/link text; broad rendered-surface scanning belongs to 3.8C. [Source: `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`; `_bmad-output/planning-artifacts/epics.md#Story 3.8C`]

### Previous Story Intelligence

- Story 3.3 review fixed redaction placeholder consistency, target-key drift, audit-readiness masking, and unexpected projection-store exception coarsening. Story 3.4 must not let a parent citation state mask missing redaction/audit metadata, and store failures must become content-safe unavailable results. [Source: `_bmad-output/implementation-artifacts/3-3-inspect-redaction-attribution-and-governance-audit-trail.md#Senior Developer Review (AI)`]
- Story 3.3 established that audit detail access is independently authorized from the parent timeline view. Citation-to-audit behavior must preserve this: a timeline citation can reference an audit handle, but opening audit detail still uses `ConversationAuditRecordAccessService`. [Source: `_bmad-output/implementation-artifacts/3-3-inspect-redaction-attribution-and-governance-audit-trail.md#Acceptance Criteria`]
- Story 3.2 established trust-before-timeline ordering and current-only trust-bearing decisions. Story 3.4 must preserve that ordering for copied citations and temporal links, including participant-resolution degradation and missing citation metadata. [Source: `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md#Dev Notes`]
- Recent commits show the Epic 3 pattern: extend contracts first, wire server/query/API narrowly, add focused tests, run targeted regressions, and keep Admin UI work out until an Admin project exists. [Source: git history `5825a54`, `0fda25e`, `8f75161`]

### Likely Files to Touch

- Likely UPDATE files:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/GetConversationAtPointInTimeQuery.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailsV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailResult.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs`
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
  - `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Likely NEW files, only if useful:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationResult.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationTargetV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/GetConversationCitationQuery.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationCitationAccessService.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationCitationContractTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationCitationAccessServiceTest.cs`
- Keep public DTOs in `Contracts`, projection/citation materialization in `Server/Projections` or `Server/Queries`, authorization/query orchestration in `Server/Queries`, HTTP binding in `Server/Api`, and tests beside the affected project.
- There is no expected need for a new package dependency. If one becomes necessary, use Central Package Management through `Directory.Packages.props`; project `PackageReference` entries remain versionless.

### Update File Guardrails

- `ConversationEvidenceEntryV1.cs`: currently carries governed evidence entry metadata, citation availability, audit readiness, safe labels, and redaction attribution with canonical redaction placeholder enforcement. Extend only additively for citation DTO/link needs, and preserve validation that redacted visible text cannot diverge from the safe placeholder.
- `ConversationEvidenceTrustPostureV1.cs`: currently summarizes tenant, conversation, freshness, trust, citation, audit, verification, and command readiness before timeline reliance. Add citation/temporal readiness only if a posture-level signal is needed; do not let a parent ready state mask a missing or unsafe entry-level citation.
- `ConversationTemporalAnchorV1.cs`: currently supports one anchor kind at a time: timestamp, safe source position, projection cursor, or contract cursor. Story 3.4 may need a composite returned anchor; preserve strict rejection of ambiguous request forms and keep timestamp non-authoritative.
- `ConversationTemporalDetailResult.cs` and `ConversationTemporalDetailsV1.cs`: currently return visible, hidden, unavailable, or rebuilding temporal reconstruction results with confidence and freshness metadata. Extend with citation-safe labels or composite anchor metadata only; preserve content-safe hidden/unavailable shapes.
- `ConversationTemporalReconstructionService.cs`: currently authorizes tenant read access before temporal event reads, validates anchor tenant/conversation, checks current projection disclosure policy, applies current redaction policy to historical messages, and fails closed for gaps/outside coverage. Preserve this ordering; do not move event-source reads before authorization or current disclosure checks.
- `ConversationQueryHandler.cs`: currently routes detail, temporal, audit-record, privileged-justification, and list queries through focused services. Add citation access through a focused service or handler method; preserve trusted tenant/caller binding and do not bypass `ConversationProjectionReadService`.
- `ConversationReadApi.cs`: currently maps detail, audit-record, and list routes under `/api/v1/conversations` with group-level `RequireAuthorization()` and manual safety-sensitive parsing. Add citation/temporal routes only under this group; preserve 404 hidden equivalence and 503 unavailable/rebuilding style.
- `ConversationProjectionMaterializer.cs`: currently builds evidence entries and sets citation availability from projection freshness. If citation fields are materialized here, preserve chronological ordering, redaction attribution linkage, and no-original-content guarantees.
- Existing tests in `ConversationEvidenceContractTest`, `TemporalReconstructionContractTest`, `ConversationTemporalReconstructionServiceTest`, `ConversationQueryHandlerTest`, `ConversationReadApiTest`, and `ConversationProjectionMaterializerTest` already enforce much of the safe-read pattern. Extend them instead of creating broad UI/E2E suites in this story.

### Testing Requirements

- Run focused contract tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~TemporalReconstruction|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
- Run focused server citation/temporal/query/API tests:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"`
- Run projection/audit/read regressions if shared evidence or audit metadata is touched:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`

### Latest Technical Information

- ASP.NET Core route groups support applying shared metadata such as `RequireAuthorization()` to a common endpoint prefix. If Story 3.4 adds citation or temporal routes, keep them under the existing authorized group instead of duplicating per-route authorization. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`]
- Minimal API binding can populate route/query/DI/special parameters, but safety-sensitive identifiers and cursors should still be manually parsed when Conversations needs hidden/unavailable side-channel equivalence instead of framework-shaped binding errors. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-10.0`; `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`]
- Blazor JS interop calls are asynchronous across render modes; a future UI clipboard primitive should use asynchronous `IJSRuntime` interop unless it is explicitly WebAssembly-only. [Source: `https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0#asynchronous-javascript-calls`]
- Blazor `NavigationManager` query helpers URL-encode query parameter names/values and use culture-invariant formatting for supported scalar/array types. If a UI link builder is added later, prefer framework helpers over manual query-string concatenation while still avoiding sensitive values in URLs. [Source: `https://learn.microsoft.com/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0#query-strings`]
- Browser `navigator.clipboard.writeText()` requires a secure context and returns a `Promise`; MDN also documents Clipboard API security expectations around user interaction/activation. Future UI code must treat clipboard failures as safe unavailable states, not as reasons to fall back to copying DOM selection. [Source: `https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText`; `https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API`]
- `dotnet test --filter` supports `FullyQualifiedName~...` contains expressions and `|`/`&` composition for selected xUnit test runs. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]

### Out of Scope

- Do not implement full Admin/FrontComposer shell work if no Admin project exists.
- Do not implement Story 3.5 command gates, privileged command execution, confirmation dialogs, or mutation routes.
- Do not implement Story 3.6 governance verification runner/results.
- Do not implement Story 3.7 buyer acceptance demo or seeded demo fixtures.
- Do not implement broad Story 3.8A responsive layout verification, Story 3.8B accessibility-tree/keyboard/screen-reader verification, or Story 3.8C Leak Sentinel/clipboard/browser/telemetry scans beyond minimal DTO/API safety checks required here.
- Do not create evidence bundle export, audit export, citation history, durable copied-text state, local/browser storage, transcript tables, secondary evidence stores, Memories/RAG indexes, or new projection authorities.
- Do not mutate conversation aggregate state from citation copy or temporal-link resolution paths.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.4: Copy Citations and Open Stable Temporal Evidence Links`
- `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`
- `_bmad-output/planning-artifacts/prd.md#Security And Privacy`
- `_bmad-output/planning-artifacts/prd.md#Data Integrity And Event Sourcing`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`
- `_bmad-output/planning-artifacts/architecture.md#Read-Model And Performance Implications`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Design & Accessibility`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md`
- `_bmad-output/implementation-artifacts/3-3-inspect-redaction-attribution-and-governance-audit-trail.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationRedactionAttributionV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationAtPointInTimeQuery.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailResult.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationAuditRecordAccessService.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`
- `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`
- `https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0`
- `https://learn.microsoft.com/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0#query-strings`
- `https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Focused contracts lane passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~TemporalReconstruction|FullyQualifiedName~ForbiddenPublicSurfaceTest"` (30 passed).
- 2026-05-22: Focused server/API lane passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` (72 passed).
- 2026-05-22: Projection/audit/read regression lane passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` (183 passed).
- 2026-05-22: Full regression passed: `dotnet test Hexalith.Conversations.slnx` (678 passed).

### Completion Notes List

- Added server-owned citation contracts and query DTOs for permission-safe citation copy: `ConversationCitationV1`, `ConversationCitationResult`, `ConversationCitationTargetV1`, and `GetConversationCitationQuery`.
- Added `ConversationCitationAccessService` and `ConversationQueryHandler.GetCitationAsync()` to resolve citation DTOs through the existing tenant authorization and projection freshness boundary.
- Added citation and temporal read routes under the existing authorized `/api/v1/conversations` API group, with trusted claim binding and side-channel-safe malformed target/cursor handling.
- Extended temporal anchors and reconstruction output so authoritative temporal links resolve to composite anchors carrying safe source position plus projection cursor/version, with timestamp as supporting metadata only.
- Added focused citation, temporal, query, API, serialization-fixture, and safety tests; UI E2E remains not applicable because no Admin/FrontComposer project exists for this story.
- QA automation follow-up added explicit redacted citation placeholder/attribution coverage, cross-tenant citation projection poison coverage, browser-selection vocabulary checks, and citation permission-downgrade clearing of clipboard/link metadata.

## Senior Developer Review (AI)

### Review Date

2026-05-22

### Reviewer

GPT-5 Codex

### Outcome

Approved after automatic fix.

### Findings Fixed

- [x] HIGH: Contract cursors with a mismatched `projection` segment were parsed by position only, allowing a stale or conflicting composite cursor to resolve instead of failing closed. Fixed `ConversationTemporalReconstructionService` to parse strict contract cursor shapes and compare supplied projection version with current projection freshness before reading temporal evidence.
- [x] MEDIUM: API cursor preflight accepted malformed projection segments because it only searched for a `pos` token. Tightened `ConversationReadApi` temporal cursor parsing so malformed projection cursor forms return the hidden shape without projection reads.

### Validation

- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~TemporalReconstruction|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 30 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 72 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 183 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 678 passed.

### Checklist

- [x] Acceptance Criteria cross-checked against implementation.
- [x] File List reviewed against git changes.
- [x] Tests mapped to citation, temporal, API, and safety requirements.
- [x] Code quality and security review performed on changed source files.
- [x] Sprint status synced to `done`.

### File List

- `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationTargetV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationCitationQuery.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationCitationAccessService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationCitationContractTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs`

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 3, Story 3.4, and Story 3.1-3.3 continuity.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR62-FR63, FR56-FR69, NFR16-NFR21, NFR42-NFR43, NFR69-NFR72, and citation/temporal carry-forward callouts.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on API boundaries, projection freshness, temporal evidence, EventStore non-disclosure, FrontComposer boundaries, and clipboard/accessibility disclosure surfaces.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`, focusing on citation controls, temporal reconstruction, copy safety, evidence drawers, and later 3.8 safety gates.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`.
  - Loaded previous Story 3.3 and Story 3.2 files, recent git history, readiness gates, and readiness decisions.
  - Read current implementation files for governed evidence entries, trust posture, citation availability vocabulary, projection freshness, temporal anchors/details/results, temporal reconstruction, query handler, read API, query cursor, and focused temporal/API tests.
  - Checked official Microsoft documentation for ASP.NET Core route groups, Minimal API route handling/binding, Blazor JS interop, Blazor navigation query helpers, and `dotnet test --filter`; checked MDN for Clipboard API/writeText browser security behavior.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to existing governed detail, audit, and temporal query boundaries instead of a new transcript, export, audit store, or UI shell.
  - Added explicit guardrails for permission-safe citation DTOs, authorization recheck, current-only citation reliance, composite temporal anchors, timestamp-as-supporting-metadata, and side-channel-safe failures.
  - Added likely file touch list, focused test commands, ADR stop conditions, latest technical references, and prior-story review lessons.
  - Kept Story 3.5/3.6/3.7/3.8 scope out of Story 3.4 while preserving enough metadata for future trust components.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture/UX guardrails, prior-story learnings, test requirements, latest technical references, and explicit out-of-scope boundaries.

## Change Log

- 2026-05-22: Implemented Story 3.4 citation DTO/query/service/API and composite temporal anchor behavior; added focused contract/server/API/regression tests; moved story and sprint status to review.
- 2026-05-22: Senior Developer Review fixed strict temporal projection-cursor validation, added regression coverage, and moved story/sprint status to done.
- 2026-05-22: Created Story 3.4 context from Epic 3 requirements, PRD/architecture/UX/readiness/project context, previous Story 3.2/3.3 learnings, current citation/temporal/read API implementation, recent git history, official Microsoft documentation, and MDN Clipboard API documentation.
