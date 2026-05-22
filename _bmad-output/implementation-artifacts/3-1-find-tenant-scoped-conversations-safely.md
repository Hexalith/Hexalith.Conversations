# Story 3.1: Find Tenant-Scoped Conversations Safely

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator,
I want to search for tenant-scoped conversations by external identifiers and business context,
so that I can find relevant governed records without leaking inaccessible records.

## Acceptance Criteria

1. Authorized tenant-scoped search returns only accessible summaries
   - Given an authorized operator enters a tenant scope and business search criteria,
   - When the search executes by customer, account, case ID, date range, project reference, folder reference, participant reference, lifecycle state, redaction state, freshness state, audit readiness, or verification state,
   - Then results include only accessible conversation summaries for that tenant,
   - And result rows include source-owned trust preview metadata needed to choose safely before opening a record.

2. Search disclosure surfaces do not leak protected existence
   - Given inaccessible, nonexistent, or cross-tenant records could match the search,
   - When results, counts, facets, ordering, autocomplete, pagination, recent searches, empty states, and response timing are rendered,
   - Then the workspace does not reveal protected existence, titles, snippets, participants, timestamps, business references, or sort gaps,
   - And safe empty copy such as "No accessible matches" is used where existence cannot be disclosed.

3. Result rows can explain why they are visible without unsafe detail
   - Given a result row is displayed,
   - When the operator inspects why it is visible,
   - Then the row can explain authorized scope, match source, freshness, redaction state, participant resolution state, and citation availability without exposing inaccessible records or redacted content,
   - And missing trust metadata downgrades the row to unknown, stale, unavailable, incomplete, or degraded.

4. Tests prove permission-safe discovery and trust previews
   - Given search workspace tests run,
   - When authorized search, no accessible matches, unauthorized-existing records, cross-tenant poison data, stale results, pagination, facets, autocomplete, and timing-sensitive cases are exercised,
   - Then tests prove permission-safe discovery, trust-preview behavior, tenant isolation, and no leakage through counts or metadata.

## Tasks / Subtasks

- [x] Extend tenant-scoped search contracts on the existing list-query path (AC: 1, 2, 3)
  - [x] Update `ConversationListFilterV1` rather than adding a parallel search contract. Keep exact tenant-scoped filters for `BusinessReference`, `ProjectId`, `FolderId`, `ParticipantPartyId`, lifecycle, and bounded date criteria.
  - [x] Add explicit filter fields for redaction state, freshness/trust state, audit readiness, and verification state using existing closed vocabularies where they are sufficient. Add new closed vocabularies only when existing public states cannot express the story safely.
  - [x] Treat customer, account, and case ID as adopter-owned external business references. Do not add provider session IDs, raw transcript text, prompt text, Party personal data, or broad free-text transcript search.
  - [x] Keep `ConversationPageMetadata` permission-safe: no total result count, inaccessible count, global `hasNext`, facet total, sort-gap hint, or cross-tenant aggregate.
  - [x] Add contract tests proving JSON shape, closed-vocabulary validation, invalid range rejection, no provider/session/EventStore fields, and safe `ToString()` behavior if new DTOs override it.

- [x] Add source-owned trust preview metadata to conversation summaries (AC: 1, 3)
  - [x] Extend `ConversationSummaryV1` and `ConversationSummaryProjectionV1` with compact trust-preview fields needed for the Find surface: freshness state, redaction state, participant resolution state, citation availability, audit readiness, verification state, and a safe why-visible/match-source explanation.
  - [x] Build trust previews from projection-owned metadata and response-scoped hydration results only. UI/client code must not infer trust from missing fields, timestamps, labels, hidden data, or disabled actions.
  - [x] Use `ProjectionTrustState` plus `ProjectionFreshnessReasonCode` for freshness and degraded states unless a documented gap requires a new contract.
  - [x] Preserve existing summary fields and serialization compatibility where possible; additive public fields must have safe defaults for older projections.

- [x] Harden `ConversationQueryHandler.ListAsync` for Story 3.1 filters and disclosure safety (AC: 1, 2, 3)
  - [x] Keep tenant access check before any projection read, filter evaluation, cursor validation that depends on stored rows, facet logic, detail lookup, hydration, or response shaping.
  - [x] Continue filtering poison rows by request tenant before ordering, paging, count calculation, freshness aggregation, hydration, or cursor generation.
  - [x] Apply new filters only to tenant-scoped candidates and only against safe projection fields. No filter should require loading raw EventStore history or full details for every candidate.
  - [x] Ensure stale, rebuilding, unavailable, forbidden, redacted, or missing trust metadata never becomes "current" by default. If a search includes non-current rows, the row and list result must carry explicit non-current state.
  - [x] Preserve signed cursor binding to tenant, caller, filter fingerprint, sort version, projection generation token, offset, max age, and key ID.
  - [x] Return the same hidden/empty shape for unauthorized, malformed, nonexistent, cross-tenant, and cursor-mismatch paths where existence cannot be disclosed.

- [x] Update the opt-in read API binding without adding unsafe endpoints (AC: 1, 2)
  - [x] Extend `ConversationReadApi.BuildFilter()` for the new filter parameters and fail closed with the existing hidden list shape on malformed input.
  - [x] Keep the route under `/api/v1/conversations/` with `RequireAuthorization()` and trusted tenant/caller binding from server-side claims.
  - [x] Do not add autocomplete, recent-search storage, facet totals, UI routes, FrontComposer components, or browser state in this story unless they are backed by permission-safe DTOs and explicit tests.
  - [x] If minimal API parameter binding is refactored, use explicit, documented binding sources where ambiguity could affect safety.

- [x] Add focused tests for search safety and current behavior preservation (AC: 1-4)
  - [x] Add contract tests in `ConversationQueryContractTest` or a focused new test file for new filter/summary/trust-preview DTOs, serialization, forbidden public surface vocabulary, range validation, and safe defaults.
  - [x] Add server tests in `ConversationQueryHandlerTest` for each filter dimension: business reference, project, folder, participant, lifecycle, date range, redaction state, freshness state, audit readiness, and verification state.
  - [x] Add server tests for unauthorized-existing records, nonexistent records, cross-tenant poison rows, stale/rebuilding/unavailable rows, missing trust metadata, malformed cursors, filter-fingerprint mismatch, projection-generation mismatch, and inaccessible candidates mixed with accessible rows.
  - [x] Add API tests in `ConversationReadApiTest` for new query-string binding, malformed values, hidden shape equivalence, and absence of unsafe IDs/content in responses.
  - [x] Add regression tests proving no totals/facet counts/autocomplete suggestions/recent-search metadata expose inaccessible records. If facets/autocomplete/recent searches remain unimplemented, assert they are absent rather than partially unsafe.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 3.1 evidence after implementation.

- [x] Preserve scope boundaries and stop for ADR when needed (AC: 1-4)
  - [x] Do not add a new durable search index, cache authority, database table, transcript table, vector index, Memories index, autocomplete store, recent-search store, export artifact, or background worker without an accepted ADR or waiver.
  - [x] Do not add UI trust components, evidence drawers, temporal links, citation copy, audit trail detail, verification engine, command gates, responsive/mobile workflows, or Leak Sentinel coverage beyond what is needed to prove Story 3.1 backend search safety.
  - [x] Do not expose raw EventStore stream names, event positions as storage authority, projection topology, raw provider payloads, Party display/contact data, raw redacted content, exception bodies, tokens, claims, or unauthorized resource existence.

## Dev Notes

### Epic and Business Context

- Epic 3 delivers the compliance investigation workspace: operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios. Story 3.1 is the Find entry point and covers FR56-FR57 plus UX-DR11, UX-DR21, UX-DR30, and UX-DR31. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.1: Find Tenant-Scoped Conversations Safely`]
- FR56 requires compliance operators to find tenant-scoped conversations by external identifiers such as customer, account, or case ID. FR57 requires narrowing by date range and business context. [Source: `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`]
- The v1 operator surface is read-only Find + Read governance viewing. Generate Evidence Bundle, broad compliance automation, semantic memory, vector search, and full governance product workflows are out of v1 scope unless later promoted. [Source: `_bmad-output/planning-artifacts/prd.md#Product Scope And MVP Force-Rank`; `_bmad-output/planning-artifacts/epics.md#Epic 3: Compliance Investigation Workspace`]

### Ready-for-Dev Preconditions

- Projection freshness blocking semantics are decided. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; if a story does not explicitly declare accepted states, only `Current` is acceptable for trust-bearing decisions. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- Shared trust/freshness vocabulary is approved through the same readiness decision and current contracts already expose `ProjectionTrustState`, `ProjectionFreshnessReasonCode`, and `ProjectionFreshnessV1`. [Source: `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`; `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessReasonCode.cs`; `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`]
- UX safety gate ownership is recorded as decided with single-threaded Architect ownership; plan the story as a focused backend/contract slice with explicit tests, not broad parallel UI work. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Architect and second-engineer availability`; `_bmad-output/implementation-artifacts/readiness-gates.md`]

### Current Implementation State

- `ListConversationsQuery` already carries `SchemaVersion`, trusted `TenantId`, `CallerPrincipalId`, `CorrelationId`, an exact-match `ConversationListFilterV1`, and bounded `ConversationPageRequest`. Extend this contract rather than introducing a new search API. [Source: `src/Hexalith.Conversations.Contracts/Queries/ListConversationsQuery.cs`]
- `ConversationListFilterV1` currently supports `BusinessReference`, `ProjectId`, `FolderId`, lifecycle, projected-at bounds, recent activity after, and participant Party ID. It does not yet support redaction state, freshness state, audit readiness, verification state, or explicit result trust-preview filters required by Story 3.1. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationListFilterV1.cs`]
- `ConversationListResult` already avoids total counts and exposes only returned accessible count plus opaque continuation cursor. Preserve this model; do not add global totals, inaccessible counts, or facet totals. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationListResult.cs`; `src/Hexalith.Conversations.Contracts/Queries/ConversationPageMetadata.cs`]
- `ConversationSummaryV1` and `ConversationSummaryProjectionV1` already expose stable summary identifiers, business/project/folder references, participant Party IDs, message/file counts, provider correlation metadata, and response-scoped hydration. They lack explicit why-visible and trust-preview fields for redaction, participant resolution, citation availability, audit readiness, and verification state. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationSummaryV1.cs`; `src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjectionV1.cs`]
- `ConversationQueryHandler.ListAsync` already checks tenant access before projection read, filters mixed-tenant poison rows before filtering/order/page, blocks mixed-generation rows as `Rebuilding`, signs cursors against tenant/caller/filter/generation/offset, hydrates summaries after paging, and aggregates worst-case freshness. Preserve those ordering and side-channel protections. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`]
- `IConversationProjectionReadStore.ListAsync(TenantId)` currently returns all candidate summaries for a tenant; any new filter pushdown must preserve the same fail-closed tenant boundary and cannot turn the store into authoritative search state. [Source: `src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs`]
- `ConversationReadApi` maps `GET /api/v1/conversations/`, requires authorization on the route group, binds tenant/caller from claims, maps malformed filters to the hidden list shape, and binds the current filter query-string parameters manually. Extend this opt-in API only through safe filter binding. [Source: `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`]
- Current focused tests already cover detail denial shape, tenant mismatch rejection, list authorization before read/filter, exact filter dimensions, mixed-generation rows, worst-case freshness aggregation, cursor tampering/expiry/tenant/filter/generation mismatch, and API binding for current filters. Add Story 3.1 tests next to these instead of duplicating test harnesses. [Source: `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`; `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`]

### Architecture Guardrails

- Public APIs expose Conversations contracts, not EventStore envelopes, aggregate IDs as substrate concepts, snapshot mechanics, stream internals, SignalR groups, or raw projection internals. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Every trust-bearing read includes projection freshness metadata; absence must never imply authorization, freshness, successful hydration, or safety. [Source: `_bmad-output/planning-artifacts/architecture.md#API Response Formats`]
- Tenant access fails closed before aggregate load, command dispatch, projection read, export, rebuild, admin action, MCP/tool action, background job execution, or verification detail access. The search path is a projection read and must keep this ordering. [Source: `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`]
- Projections are derived, disposable, and non-authoritative. A search projection can optimize reads, but it must not introduce facts that cannot be reconstructed from EventStore plus approved read-time hydration sources. [Source: `_bmad-output/planning-artifacts/architecture.md#State Management Patterns`]
- Party personal data is read-time hydration only. Search contracts may carry stable `PartyId` references and safe hydration states, but must not persist or return names, emails, phone numbers, avatars, contact channels, or upstream problem details as durable conversation state. [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`]
- FR56-FR69 operator workflows map architecturally to admin trust components, evidence timelines, and temporal navigation, but Story 3.1 should first establish the backend/contracts that those surfaces consume. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- ADR stop condition: adding a durable search index, cache, autocomplete store, recent-search store, new projection authority, public trust state, privileged execution path, or fail-open degraded behavior requires an accepted ADR or explicit waiver before implementation. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`; `docs/adrs/index.md`]

### UX Requirements for Find

- Use generated-first surfaces for search, filtering, lists, details, forms, loading, and empty states; add custom Conversations UI only where trust interpretation demands it. Story 3.1 should provide safe contracts and states for those generated/composed surfaces. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR11`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR2`]
- Search is tenant-scoped, permission-filtered, and trust-previewed. Result rows should show record identity, business context, freshness, redaction state, participant resolution state, citation availability, and why the result is visible when authorized. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Search And Filtering Patterns`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR31`]
- Filters, facets, result counts, pagination, ordering, autocomplete, recent searches, and timing must not leak inaccessible records. If these features are not implemented in this story, tests should prove they are absent or safe rather than partially present. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Search And Filtering Patterns`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR21`]
- Empty and loading states are trust-bearing. Use "No accessible matches" when existence cannot be disclosed, and never default missing metadata to current, complete, or action-ready. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Empty And Loading States`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR30`]
- Unknown never becomes assumed-safe. Apply deterministic trust precedence and explicit states for unknown, stale, unavailable, redacted, degraded, denied, and no-access outcomes. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR29`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR30`]

### Previous Story Intelligence

- Story 2.8 completed with full solution validation at 625 passing tests. Use that as the baseline to preserve when adding Story 3.1 contract and query behavior. [Source: `_bmad-output/implementation-artifacts/2-8-record-and-review-privileged-operational-justification.md#Debug Log References`]
- Story 2.8 established the current pattern for governed query work: additive public contracts first, tenant authorization before any target/source resolution, current freshness by default, explicit forbidden/unavailable/rebuilding/stale/redacted states, content-safe review details, and focused tests. [Source: `_bmad-output/implementation-artifacts/2-8-record-and-review-privileged-operational-justification.md#Completion Notes List`]
- Story 2.8 review fixed a trusted-caller authorization bug. For Story 3.1, `CallerPrincipalId` must remain a trusted server-bound identity from the request context/claims, never a user-editable filter or client-supplied authority. [Source: `_bmad-output/implementation-artifacts/2-8-record-and-review-privileged-operational-justification.md#Senior Developer Review (AI)`]
- Story 2.7 and Story 2.8 both emphasize that missing handles/details and withheld fields must not leak existence through nulls, non-unique keys, or diagnostic text. Apply the same rule to search result metadata, why-visible explanations, and filter failures. [Source: `_bmad-output/implementation-artifacts/2-7-govern-audit-record-access-retention-and-redaction.md`; `_bmad-output/implementation-artifacts/2-8-record-and-review-privileged-operational-justification.md`]

### Git Intelligence

- Recent commits show the established implementation sequence:
  - `14ef92a feat(story-2.8): Record and Review Privileged Operational Justification`
  - `01f58ae feat(story-2.7): Govern Audit Record Access Retention and Redaction`
  - `eb2f625 feat(story-2.6): Reconstruct Point-in-Time Governance State`
  - `15e7605 feat(story-2.5): Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior`
  - `f9375e1 feat(story-2.4): Redact Message Content with Audit Attribution`
- Follow the same shape: additive contracts, server-side guard ordering, fail-closed result shaping, focused tests, then full solution validation. Do not introduce broad infrastructure just because future Epic 3 stories mention evidence detail, citations, temporal links, verification, or UI workflows.

### File Structure Requirements

- Likely UPDATE files:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationListFilterV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ListConversationsQuery.cs` only if documentation or validation changes are needed.
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationListResult.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationSummaryV1.cs`
  - `src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjectionV1.cs`
  - `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessReasonCode.cs` only if existing reasons cannot express the result safely.
  - `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs` only if an ADR/waiver approves a new public trust state.
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` if a new closed vocabulary is added.
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs`
  - `src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs` only if safe filter pushdown is added.
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectedReadModels.cs` only if summary/detail materialization shape changes.
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
  - `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs`
  - `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Likely NEW files, only if useful:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchTrustPreviewV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchMatchSource.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditReadinessState.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationVerificationState.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationSearchContractTest.cs`
- Keep public DTOs in `Contracts`, projection/materialization code in `Server/Projections`, authorization/query orchestration in `Server/Queries`, HTTP binding in `Server/Api`, and tests beside the affected project.

### Testing Requirements

- Run focused contract tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationSearch|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
- Run focused server query/API/projection tests:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"`
- Run regression coverage around tenant access and hydration if affected:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~ConversationQueryRegistrationTest"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`
- Microsoft docs confirm `dotnet test --filter` supports `FullyQualifiedName~...` contains expressions and boolean composition. NuGet Central Package Management keeps versions in `Directory.Packages.props`; project `PackageReference` entries should not add `Version` attributes. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`; `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Latest Technical Information

- ASP.NET Core minimal API route groups support applying metadata such as `RequireAuthorization()` to all endpoints in a group. Keep the existing `/api/v1/conversations` group authorization model when adding Story 3.1 query binding. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`; `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0#authorization`]
- Minimal API parameter binding supports route values, query string, headers, body, form values, DI services, special types such as `HttpContext` and `CancellationToken`, and custom binding. If the current manual binding is refactored, use explicit binding for safety-sensitive values. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`]
- .NET 10/NuGet behavior makes accidental package version placement more visible. If any new dependency is absolutely required, add its version centrally and keep project references versionless under CPM. Prefer no new dependency for this story. [Source: `https://learn.microsoft.com/dotnet/core/compatibility/sdk/10.0/nu1015-packagereference-version`; `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Out of Scope

- Do not build the governed record Read view, evidence timeline, redaction attribution drawer, audit trail drawer, citation copy, temporal evidence links, verification runner, buyer demo, responsive layout, accessibility tree tests, clipboard tests, browser-title tests, or Leak Sentinel helper in this story.
- Do not add durable search indexes, autocomplete stores, recent-search stores, transcript tables, vector/Memories indexes, evidence bundles, export artifacts, queues, workers, caches, or new database tables without accepted ADR or waiver coverage.
- Do not implement free-text transcript search or snippets. This story is exact tenant-scoped discovery by business-safe identifiers and metadata.
- Do not mutate conversation aggregate state from search/list/read paths.
- Do not disclose unauthorized resource existence through HTTP status, result counts, facet totals, sort gaps, empty-state wording, timing, cursor errors, diagnostics, telemetry, logs, labels, accessibility text, or hidden response fields.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.1: Find Tenant-Scoped Conversations Safely`
- `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`
- `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`
- `_bmad-output/planning-artifacts/architecture.md#API Response Formats`
- `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`
- `_bmad-output/planning-artifacts/architecture.md#State Management Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Search And Filtering Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Empty And Loading States`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `_bmad-output/implementation-artifacts/2-8-record-and-review-privileged-operational-justification.md`
- `_bmad-output/implementation-artifacts/2-7-govern-audit-record-access-retention-and-redaction.md`
- `docs/adrs/index.md`
- `src/Hexalith.Conversations.Contracts/Queries/ListConversationsQuery.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationListFilterV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationListResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationPageMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationSummaryV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs`
- `src/Hexalith.Conversations.Server/Projections/IConversationProjectionReadStore.cs`
- `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`
- `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`
- `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Red phase `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationSearch|FullyQualifiedName~ForbiddenPublicSurfaceTest"` failed as expected before implementation because Story 3.1 DTOs and filters did not exist.
- 2026-05-22: Focused contract validation passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationSearch|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 17 passed.
- 2026-05-22: Focused server/API/projection validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 114 passed.
- 2026-05-22: Tenant access/hydration regression validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~ConversationQueryRegistrationTest"` - 138 passed.
- 2026-05-22: Full regression validation passed: `dotnet test Hexalith.Conversations.slnx` - 638 passed.
- 2026-05-22: Code review fix validation passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationSearch|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 18 passed.
- 2026-05-22: Code review fix validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 117 passed.
- 2026-05-22: Code review fix validation passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~ConversationQueryRegistrationTest"` - 138 passed.
- 2026-05-22: Code review full regression validation passed: `dotnet test Hexalith.Conversations.slnx` - 642 passed.

### Implementation Plan

- Extended the existing list-query contract rather than adding a new search endpoint or durable index.
- Added closed search vocabularies only where the existing trust vocabulary could not safely express citation availability, audit readiness, verification state, or match source.
- Built trust previews from projection-owned metadata first, then response-scoped participant hydration after tenant-safe paging.
- Kept tenant access, poison-row filtering, cursor binding, malformed input handling, and safe empty shapes on the existing disclosure-safe list path.

### Completion Notes List

- Added Story 3.1 filter fields to `ConversationListFilterV1`: redaction state, freshness state, audit readiness, and verification state.
- Added `ConversationSearchTrustPreviewV1` and closed vocabularies for citation availability, audit readiness, verification state, and safe match source.
- Extended `ConversationSummaryV1` and `ConversationSummaryProjectionV1` with safe trust-preview defaults for older projections.
- Updated projection materialization, list query filtering, cursor fingerprinting, and hydration so trust previews stay source-owned and response-scoped.
- Extended the opt-in read API query binding for new filters and made malformed date/vocabulary input fail closed with the hidden list shape.
- Added focused contract, server, API, hydration, and regression tests proving permission-safe discovery, no unsafe totals/facets/autocomplete/recent-search surfaces, and current behavior preservation.
- Code review fixed missing fail-closed handling for malformed `pageSize`, non-current freshness aggregation across all filtered accessible matches before paging, and overconfident fallback trust metadata for older projections.

### File List

- `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationListFilterV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationListResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchTrustPreviewV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationSummaryV1.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Critical source documents loaded: sprint status, Epic 3 story requirements, PRD FR56-FR57, architecture guardrails, UX requirement map/design specification, readiness gates and decisions, project context, Story 2.8/2.7 learnings, recent git history, current query/list/read API/projection/hydration contracts and tests, ADR index, and official Microsoft documentation for `dotnet test --filter`, NuGet Central Package Management, and ASP.NET Core minimal APIs.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code gaps, likely update/new files, prior-story learnings, tenant/freshness/disclosure/EventStore guardrails, ADR stop conditions, focused test requirements, and explicit out-of-scope boundaries.
- Checklist fixes applied in YOLO mode: blocked new durable search/autocomplete/recent-search/index infrastructure without ADR coverage; required tenant authorization before projection/filter/count/facet/cursor work; required permission-safe counts/cursors/empty states; tied trust previews to server/projection-owned metadata; and separated backend search safety from later Epic 3 UI, citation, temporal, verification, responsive, and accessibility work.

## Senior Developer Review (AI)

### Review Date

2026-05-22

### Outcome

Approve after automatic fixes. No critical issues remain.

### Findings and Fixes

- [x] [HIGH] Older projections without explicit Story 3.1 trust metadata defaulted some trust-preview fields to confident values. Fixed `ConversationSearchTrustPreviewV1.FromFreshness()` so missing trust metadata degrades to unavailable/unknown states, and added contract regression coverage.
- [x] [HIGH] Malformed `pageSize` query input silently fell back to the default page size instead of returning the hidden list shape. Fixed `ConversationReadApi.BuildPage()` to fail closed and added API coverage.
- [x] [MEDIUM] List freshness aggregation used only the returned page window plus continuation lookahead, so a non-current accessible match later in the same filtered result set could be hidden from the list-level trust state. Fixed aggregation to use all accessible filtered matches before paging and added server regression coverage beyond the lookahead row.

### Checklist Validation

- Story file loaded and status verified as reviewable before review.
- Acceptance criteria and completed tasks cross-checked against changed contracts, query handler, API binding, projection materialization, hydration, and tests.
- File List checked against git changes and updated coverage.
- Microsoft Learn documentation checked for ASP.NET Core route groups, authorization metadata, and Minimal API parameter binding behavior.
- Focused and full validation passed after fixes.

## Change Log

- 2026-05-22: Created Story 3.1 context from Epic 3 requirements, PRD/architecture/UX/readiness/project context, previous Story 2.8/2.7 learnings, current query/projection/read API code, recent git history, ADR status, and Microsoft .NET/NuGet/ASP.NET Core documentation.
- 2026-05-22: Implemented Story 3.1 tenant-scoped Find backend contracts, trust previews, query/API filtering, disclosure-safe empty behavior, and focused validation coverage.
- 2026-05-22: Code review completed with automatic fixes for trust-preview defaults, malformed page-size handling, and list freshness aggregation across all filtered accessible matches before paging; story moved to done.
