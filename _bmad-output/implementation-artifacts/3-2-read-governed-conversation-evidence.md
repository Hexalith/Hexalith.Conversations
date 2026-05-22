# Story 3.2: Read Governed Conversation Evidence

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator,
I want to open a governed conversation record with trust posture before timeline content,
so that I can decide whether the evidence is safe to rely on.

## Acceptance Criteria

1. Governed record opens with trust posture before timeline reliance
   - Given an authorized operator opens a conversation,
   - When the governed record view loads,
   - Then it displays tenant scope, record identity, temporal cursor, trust posture, evidence completeness, projection freshness, participant resolution, citation status, and command eligibility before timeline reliance,
   - And these trust claims come only from Conversations projections or command availability metadata.

2. Evidence timeline renders records, not casual chat bubbles
   - Given the evidence timeline is displayed,
   - When participants, messages, attachments, governance states, redactions, and freshness metadata are rendered,
   - Then entries appear as evidence records rather than casual chat bubbles,
   - And each entry preserves chronological order, actor attribution, timestamp, citation/audit anchors where available, and safe degraded states.

3. Missing or degraded trust metadata never becomes safe by default
   - Given trust metadata is missing, contradictory, stale, unavailable, or partially loaded,
   - When the record view renders,
   - Then it shows an explicit unknown, stale, unavailable, incomplete, blocked, or degraded state,
   - And it never presents the record as current, complete, cite-ready, or action-ready by default.

4. Governed record tests prove safe read behavior
   - Given governed record tests run,
   - When fully trusted, stale projection, missing citation, unresolved participant, partial evidence, unavailable projection, and cross-tenant attempts are exercised,
   - Then tests prove trust ordering, projection-owned state, fail-closed rendering, and absence of raw EventStore internals.

## Tasks / Subtasks

- [x] Extend governed detail contracts for a trust-before-timeline read model (AC: 1, 3)
  - [x] Extend `ConversationDetailsV1` and `ConversationDetailProjectionV1` rather than adding a parallel transcript/viewer DTO. The existing `GetConversationQuery` / `ConversationDetailResult` path is the governed record read boundary.
  - [x] Add a source-owned trust posture model for the opened record, reusing existing vocabularies where sufficient: `ProjectionTrustState`, `ProjectionFreshnessReasonCode`, `ConversationCitationAvailability`, `ConversationAuditReadinessState`, and `ConversationVerificationState`.
  - [x] Include explicit fields for tenant scope, record identity, temporal cursor or safe projection cursor, evidence completeness, projection freshness, participant resolution, citation status, audit readiness, verification state, and command eligibility.
  - [x] If command eligibility needs new public metadata, model it as server-owned command availability metadata with required permission, precondition, risk level, freshness requirement, audit requirement, blocked reason, and last evaluated timestamp. Missing metadata must become metadata-unavailable/blocked, not an optional disabled button.
  - [x] Do not expose raw EventStore stream names, event type names, storage offsets, replay mechanics, projection internals, SignalR group names, provider session IDs, tokens, claims, or upstream problem details.

- [x] Convert timeline data into evidence-entry contracts while preserving existing projection authority (AC: 2, 3)
  - [x] Extend or wrap `ConversationTimelineMessageProjectionV1` through a Conversations-owned evidence entry contract. Preserve stable `MessageId`, actor `PartyId`, timestamp, provider correlation only where already safe, and redacted placeholder behavior.
  - [x] Represent participants, messages, attachments, retention policy, sensitivity marks, redactions, and freshness as governed evidence records with explicit kind, actor attribution, timestamp, trust state, citation/audit availability, and safe degraded state.
  - [x] Preserve chronological ordering by projected event/message timestamp and existing projection ordering rules. Do not sort by hydrated display label, UI grouping, provider metadata, or client-side arrival order.
  - [x] Ensure redacted message content remains the approved placeholder from projection materialization. Do not retain original text in hidden fields, tooltips, accessibility labels, copied text, diagnostics, telemetry, test snapshots, or alternate mobile/condensed DTOs.
  - [x] Keep full redaction attribution drawer/audit trail expansion scoped to Story 3.3 unless this story needs a compact inline anchor to prove the timeline entry is citeable/degraded.

- [x] Harden `ConversationQueryHandler.GetAsync` and detail hydration for trust ordering (AC: 1, 3, 4)
  - [x] Keep tenant authorization and projection freshness evaluation inside `ConversationProjectionReadService.ReadDetailAsync` before hydration, command metadata, timeline shaping, temporal cursor creation, or response rendering.
  - [x] Keep the current denial equivalence: unauthorized, nonexistent, malformed ID, and cross-tenant projection poison return the same hidden detail shape and do not disclose tenant, conversation, business reference, participant, or title existence.
  - [x] Preserve `ConversationProjectionReadService` behavior where only `ProjectionFreshnessV1.AllowsTrustBearingDecision()` enables trust-bearing actions. Stale, rebuilding, unavailable, forbidden, redacted, contradictory, mixed-generation, and poison states must block reliance.
  - [x] Detail hydration remains response-scoped. It may degrade participant/project/folder/file display through safe hydration states, but it must not change projection ordering, authorization, evidence completeness, citation readiness, or command eligibility.
  - [x] Add worst-state participant-resolution aggregation for detail reads if not already present. Do not infer participant resolution from non-empty `Participants`, labels, message authors, or successful project/folder hydration.

- [x] Update the opt-in read API without adding unsafe routes (AC: 1, 3, 4)
  - [x] Keep `GET /api/v1/conversations/{conversationId}` under the existing `/api/v1/conversations` route group with `RequireAuthorization()`.
  - [x] Continue binding tenant and caller from trusted server-side claims in `ConversationReadApi.TryGetTenantCaller()`. Do not accept tenant, caller, user, role, token, command permission, or hydration authority from query string, route values, or request body.
  - [x] Keep malformed conversation IDs on the hidden detail shape. Infrastructure failures return unavailable without echoing exception, storage, tenant, conversation, or provider details.
  - [x] Do not add temporal-link copy, citation-copy, audit-record drawer, verification-runner, command mutation, export, evidence bundle, autocomplete, recent item, or raw forensic mode endpoints in this story unless they are explicitly backed by permission-safe DTOs and independent authorization tests.

- [x] Add focused contract and server tests for governed record evidence reads (AC: 1-4)
  - [x] Extend `ConversationQueryContractTest` or add a focused contract test file for trust posture serialization, evidence completeness defaults, command eligibility defaults, evidence-entry shape, citation/audit availability states, and absence of EventStore/provider/session/transcript internals.
  - [x] Extend `ConversationQueryHandlerTest` for fully trusted current detail, stale detail, rebuilding detail, unavailable projection, mixed summary/detail generation, missing citation metadata, unresolved participant hydration, redacted timeline message, partial evidence, cross-tenant projection poison, nonexistent conversation, and unauthorized-existing conversation.
  - [x] Extend `ConversationReadApiTest` for detail route hidden/unavailable/visible behavior, malformed route value, safe claim binding, and body/JSON shape not exposing unsafe IDs or infrastructure terms.
  - [x] Extend `ConversationProjectionMaterializerTest` and `ConversationProjectionReadServiceTest` for projection-owned trust posture, timeline ordering, redaction placeholders, audit/citation metadata availability, evidence completeness, and current-only trust-bearing action eligibility.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` after implementation with Story 3.2 evidence.

- [x] Preserve scope boundaries and ADR stop conditions (AC: 1-4)
  - [x] Do not create a transcript table, durable evidence store, cache authority, export artifact, evidence bundle, background worker, durable UI state, Memories/RAG index, or separate projection authority without accepted ADR or waiver coverage.
  - [x] Do not implement full inline redaction attribution and governance audit trail inspection from Story 3.3, citation copy and stable temporal evidence links from Story 3.4, read-only command gates from Story 3.5, governance verification from Story 3.6, buyer demo from Story 3.7, or responsive/accessibility/leak sentinel verification from Stories 3.8A-3.8C.
  - [x] If a new public trust state, command availability vocabulary, evidence completeness vocabulary, or adopter-facing error taxonomy is required, stop for ADR/waiver unless it can be expressed by existing approved states.

## Dev Notes

### Epic and Business Context

- Epic 3 delivers the compliance investigation workspace: compliance operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios. Story 3.2 is the opened-record Read slice after Story 3.1 Find. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Compliance Investigation Workspace`]
- Story 3.2 covers FR58 and UX-DR1-UX-DR5, UX-DR12, UX-DR13, UX-DR18, UX-DR19, UX-DR22, UX-DR24, UX-DR25, UX-DR29, and UX-DR32. FR58 requires a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.2: Read Governed Conversation Evidence`; `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`]
- The operator v1 surface is a read-only Find + Read governance viewer. Full signed evidence bundle export is deferred to v1.1, and later Epic 3 stories own attribution drawers, citations, temporal links, verification, and buyer demo. [Source: `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`; `_bmad-output/planning-artifacts/epics.md#Story 3.3: Inspect Redaction Attribution and Governance Audit Trail`; `_bmad-output/planning-artifacts/epics.md#Story 3.4: Copy Citations and Open Stable Temporal Evidence Links`]

### Ready-for-Dev Preconditions

- Projection freshness blocking semantics are decided. Canonical states are `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`; if a story does not explicitly declare accepted states, only `Current` is acceptable for trust-bearing decisions. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- Command availability metadata is decided and server-owned. Missing, stale, ambiguous, malformed, unauthorized, or partially loaded command metadata disables unsafe actions. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Command availability metadata`]
- Party hydration degraded states are decided. Authorized reads may degrade Party display hydration, but command-time participant validation fails closed and durable state must not store Party personal data. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Party hydration degraded states`]
- UX safety gate ownership is recorded as single-threaded through Architect capacity. Keep this story as a focused contract/server read-safety slice with enough component contract guidance for later Admin/TrustComponents work; do not broaden into all Epic 3 UI verification. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Architect and second-engineer availability`]

### Current Implementation State

- `ConversationReadApi` already maps `GET /api/v1/conversations/{conversationId}` and `GET /api/v1/conversations/` under `/api/v1/conversations` with `RequireAuthorization()`. It binds tenant from the `tid` claim, caller from `ClaimTypes.NameIdentifier`, correlation from `X-Correlation-Id`, and maps malformed detail requests to a hidden 404 shape. Extend this endpoint; do not add a separate viewer route. [Source: `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`]
- `GetConversationQuery` carries `SchemaVersion`, trusted `TenantId`, trusted `CallerPrincipalId`, safe `CorrelationId`, and a Conversations-owned `ConversationId`. It does not expose provider session IDs or EventStore identifiers. [Source: `src/Hexalith.Conversations.Contracts/Queries/GetConversationQuery.cs`]
- `ConversationQueryHandler.GetAsync` already reads through `ConversationProjectionReadService.ReadDetailAsync`, returns hidden/unavailable when projection is not available for trust, hydrates detail after projection acceptance, and returns `ConversationDetailResult.Visible(...)`. Preserve this order. [Source: `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`]
- `ConversationProjectionReadService.ReadDetailAsync` performs tenant access before projection storage, catches projection store failures as unavailable, rejects missing records as hidden, rejects tenant/conversation poison as forbidden, rejects mixed summary/detail generations as rebuilding, and only returns a projection when `ProjectionFreshnessV1.AllowsTrustBearingDecision()` is true. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`]
- `ConversationDetailsV1` currently exposes freshness, lifecycle, label, business/project/folder references, provider correlation, participants, messages, file references, a string `GovernanceState`, attributes, response-scoped hydration, sensitivity marks, and redactions. It does not yet expose a governed record trust posture, evidence completeness, citation state, detail-level participant resolution aggregate, command eligibility metadata, or evidence-entry abstraction. [Source: `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`]
- `ConversationDetailProjectionV1` currently includes active retention policy, sensitivity marks, redactions, messages, participants, files, and freshness. It is the right source for projection-owned trust posture; avoid computing trust in the UI from raw lists or missing fields. [Source: `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`]
- `ConversationTimelineMessageProjectionV1` is still message-shaped: `MessageId`, `AuthorPartyId`, `Text`, `CreatedAt`, and optional provider correlation. Story 3.2 should either extend it safely or introduce a separate evidence-entry contract so the Read view does not look like a casual transcript. [Source: `src/Hexalith.Conversations.Contracts/Projections/ConversationTimelineMessageProjectionV1.cs`]
- `ConversationProjectionMaterializer` already replaces redacted message text with `ConversationRedactionProjectionV1.Placeholder`, tracks retention/sensitivity/redaction projections, orders messages by timestamp then position, and creates Story 3.1 search trust previews. Build on this projection materialization rather than replaying raw EventStore history for ordinary reads. [Source: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`]
- `ConversationReadHydrationService` hydrates Party, Project, Folder, and File references after authorization and projection acceptance. It maps upstream failures into safe unavailable/redacted/stale/rebuilding states and never returns raw upstream problem details. Detail trust must treat unresolved hydration as degraded, not as current. [Source: `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs`]
- Temporal reconstruction contracts and service already exist (`GetConversationAtPointInTimeQuery`, `ConversationTemporalDetailsV1`, `ConversationTemporalDetailResult`, `ConversationTemporalReconstructionService`). Story 3.2 may surface the current projection cursor/temporal anchor in the trust header, but stable temporal evidence links and citation-copy workflows belong to Story 3.4. [Source: `src/Hexalith.Conversations.Contracts/Queries/GetConversationAtPointInTimeQuery.cs`; `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`; `_bmad-output/planning-artifacts/epics.md#Story 3.4: Copy Citations and Open Stable Temporal Evidence Links`]
- Existing focused tests already prove detail denial equivalence, tenant access before projection read, cross-tenant projection poison rejection, detail hydration after authorized projection read, mixed-generation projection blocking, route authorization metadata, API hidden/unavailable detail shapes, and Story 3.1 search filters/trust preview behavior. Add tests next to these harnesses. [Source: `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`; `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs`; `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`; `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs`]

### Architecture Guardrails

- Public APIs expose Conversations commands, projections, result contracts, version metadata, typed errors, and freshness state. They must not expose EventStore envelopes, stream names, event positions as storage authority, snapshot mechanics, projection topology, or raw projection internals. [Source: `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Every trust-bearing read includes projection freshness metadata. Absence must never imply authorization, freshness, successful hydration, or safety. [Source: `_bmad-output/planning-artifacts/architecture.md#API Response Formats`; `_bmad-output/planning-artifacts/architecture.md#Loading And Freshness Patterns`]
- Read models are derived, repairable, and non-authoritative. Projections may optimize reads but must not introduce facts that cannot be reconstructed from EventStore plus approved read-time hydration sources. [Source: `_bmad-output/planning-artifacts/architecture.md#State Management Patterns`]
- Tenant access fails closed before aggregate load, command dispatch, projection read, export, rebuild, admin action, MCP/tool action, background job execution, or verification detail access. This story touches projection-backed reads, so tenant access must remain first. [Source: `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`]
- Party personal data is read-time hydration only. Do not persist or project Party names, contact values, identifiers, person details, organization details, raw audit details, or raw upstream problem details in conversation events/projections/logs/caches. [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- ADR stop conditions apply if the implementation adds a durable store, cache, index, export artifact, worker queue, evidence artifact, public trust state, command availability vocabulary, error taxonomy, privileged execution path, or altered tenant/freshness/redaction/hydration semantics. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`; `docs/adrs/index.md`]

### UX and Component Contract Requirements

- The detail experience should behave like a governed case file, not a transcript. The first screen must show reconstructed record context, trust posture, evidence completeness, and permitted actions before the operator relies on timeline content. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Vision and Experience Goals`; `_bmad-output/planning-artifacts/ux-design-specification.md#Find Read Trust Visual Rhythm`]
- Trust-bearing UI renders Conversations-owned projections and command availability metadata. It may format trust data but must not infer trust, permission, freshness, redaction impact, citation confidence, evidence completeness, or action eligibility from missing data, disabled buttons, timestamps, labels, cache age, or client state. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Component Architecture Principle`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR3`]
- A standard trust summary band appears before the timeline at every breakpoint: tenant scope, record identity, freshness, completeness, citation status, participant resolution, and command eligibility. Story 3.2 should produce the DTO/component contract needed for this order even if full responsive verification lands in Story 3.8A. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR32`; `_bmad-output/planning-artifacts/ux-design-specification.md#Find Read Trust Visual Rhythm`]
- Use FrontComposer and Fluent UI Blazor as baseline design system foundations, but add custom Conversations components only where trust interpretation demands it. Trust primitives should include `Trust Fact`, `SafeReasonInline`, `Freshness Marker`, `Command Availability Marker`, `Citation Control`, `Participant Identity Marker`, and `Redaction Placeholder`. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR1`; `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR12`; `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Components`]
- Evidence timeline entries must present evidence records, not chat bubbles. Each entry needs source-owned state: kind, actor, timestamp, visibility/redaction state, citation/audit availability, source projection version/freshness, and safe degraded state. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Components`; `_bmad-output/planning-artifacts/epics.md#Story 3.2: Read Governed Conversation Evidence`]
- Unknown never becomes assumed-safe. When states conflict, the safer state wins: blocked over available, stale over current, incomplete over complete, redacted over visible, unknown over assumed. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR29`; `_bmad-output/planning-artifacts/ux-design-specification.md#Trust Precedence Model`]

### Previous Story Intelligence

- Story 3.1 completed with full regression validation at 642 passing tests. Treat that as the baseline to preserve. [Source: `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md#Debug Log References`]
- Story 3.1 established the current Epic 3 pattern: extend existing list/read contracts, keep tenant access before projection/filter/cursor work, use projection-owned trust metadata, map missing metadata to unavailable/unknown rather than current, and prove no unsafe totals/facets/autocomplete/recent-search surfaces. Apply the same posture to the opened record. [Source: `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md#Implementation Plan`; `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md#Completion Notes List`]
- Story 3.1 review fixed three relevant hazards: older projections defaulting to confident trust metadata, malformed API input failing open to defaults, and list freshness aggregation hiding non-current accessible matches beyond the current page. For Story 3.2, older/missing detail trust metadata must degrade, malformed detail input must return hidden/unavailable safely, and any non-current evidence component must downgrade the opened-record posture before timeline reliance. [Source: `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md#Senior Developer Review (AI)`]
- Story 3.1 likely update files overlap with Story 3.2: contracts under `Contracts/Queries` and `Contracts/Projections`, `ConversationProjectionMaterializer`, `ConversationProjectionReadService`, `ConversationQueryHandler`, `ConversationReadHydrationService`, `ConversationReadApi`, and focused contract/server/API/projection tests. Reuse these harnesses instead of creating a second query stack. [Source: `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md#File List`]

### Git Intelligence

- Recent commits show sequential story slices with additive contracts, server-side guard ordering, focused tests, and full regression validation:
  - `5825a54 feat(story-3.1): Find Tenant-Scoped Conversations Safely`
  - `14ef92a feat(story-2.8): Record and Review Privileged Operational Justification`
  - `01f58ae feat(story-2.7): Govern Audit Record Access Retention and Redaction`
  - `eb2f625 feat(story-2.6): Reconstruct Point-in-Time Governance State`
  - `15e7605 feat(story-2.5): Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior`
- Follow the same shape: contract-first, projection-owned trust state, fail-closed server query behavior, focused test coverage, then full `dotnet test Hexalith.Conversations.slnx`. Do not introduce new infrastructure just because later Epic 3 stories mention drawers, citations, temporal links, verification, responsive layouts, or leak scanning.

### File Structure Requirements

- Likely UPDATE files:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailResult.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailsV1.cs` only if temporal header compatibility requires shared trust metadata.
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs` only if existing citation/audit/verification vocabularies need reuse or narrow extension.
  - `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
  - `src/Hexalith.Conversations.Contracts/Projections/ConversationTimelineMessageProjectionV1.cs` only if extending the message projection is safer than adding a separate evidence entry.
  - `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs` only if additional helper behavior is needed; avoid changing canonical freshness semantics.
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` if new closed vocabularies are introduced.
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
  - `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`
  - `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
  - `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs`
  - `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Hydration/ConversationReadHydrationServiceTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Likely NEW files, only if useful:
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceCompletenessV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
  - `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceVocabularies.cs`
  - `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
- Keep public DTOs in `Contracts`, projection/materialization in `Server/Projections`, authorization/query orchestration in `Server/Queries`, response-scoped hydration in `Server/Hydration`, HTTP binding in `Server/Api`, and tests beside the affected project.
- There is no expected need for a new package dependency. If a dependency becomes necessary, respect Central Package Management through `Directory.Packages.props`; project files should remain versionless under CPM. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `https://learn.microsoft.com/nuget/consume-packages/central-package-management`]

### Testing Requirements

- Run focused contract tests first:
  - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ForbiddenPublicSurfaceTest"`
- Run focused server query/projection/hydration/API tests:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationReadHydrationServiceTest|FullyQualifiedName~ConversationReadApiTest"`
- Run regression coverage around tenant access, temporal/audit boundaries, and Story 3.1 list behavior if any shared code is touched:
  - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Temporal|FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryRegistrationTest"`
- Then run:
  - `dotnet test Hexalith.Conversations.slnx`
- Microsoft documentation confirms `dotnet test --filter` supports `FullyQualifiedName~...` contains expressions and boolean composition. ASP.NET Core Minimal API special types include `HttpContext`, `ClaimsPrincipal`, and `CancellationToken`, and binding failures can produce default 400/500 behavior; keep manual safety-sensitive binding where side-channel equivalence requires a hidden shape instead of default framework errors. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`; `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`]

### Latest Technical Information

- ASP.NET Core Minimal API route groups support shared endpoint metadata such as `RequireAuthorization()`. Keep the existing group-level authorization instead of per-route drift. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`; `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-10.0#route-groups`]
- Minimal API parameter binding supports route, query, header, body, form, DI, custom binding, and special types; automatic binding failures may reveal a different HTTP status shape than Conversations wants for hidden records. Continue using explicit/manual parsing for safety-sensitive identifiers and query values. [Source: `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`]
- NuGet Central Package Management keeps versions in `Directory.Packages.props`; with CPM, `PackageReference` entries should not carry inline `Version` attributes. .NET 10 also makes missing versions an error outside CPM, so avoid new dependencies for this story unless clearly necessary. [Source: `https://learn.microsoft.com/nuget/consume-packages/central-package-management`; `https://learn.microsoft.com/dotnet/core/compatibility/sdk/10.0/nu1015-packagereference-version`]

### Out of Scope

- Do not build the full admin UI shell, responsive split investigation layout, accessibility tree verification, clipboard/browser-title/telemetry leak sentinel, or mobile triage workflow in this story. Component contract/data shape is in scope only where needed to make trust-before-timeline rendering possible.
- Do not implement full inline redaction attribution and governance audit trail inspection; Story 3.3 owns that.
- Do not implement citation copy or stable temporal evidence link opening; Story 3.4 owns that.
- Do not implement read-only compliance command gates and safe command execution blocking beyond server-owned command eligibility metadata needed by the header; Story 3.5 owns the workflow.
- Do not implement governance verification runner/results; Story 3.6 owns that.
- Do not implement buyer acceptance demo fixtures; Story 3.7 owns that.
- Do not create durable transcript tables, secondary stores, caches, exports, evidence bundles, memory indexes, queue workers, or new projection authorities without an ADR/waiver.
- Do not mutate aggregate state from any Find/Read/Trust path.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.2: Read Governed Conversation Evidence`
- `_bmad-output/planning-artifacts/prd.md#Operator And Compliance Workflows`
- `_bmad-output/planning-artifacts/architecture.md#API Naming Conventions`
- `_bmad-output/planning-artifacts/architecture.md#API Response Formats`
- `_bmad-output/planning-artifacts/architecture.md#State Management Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Component Architecture Principle`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Components`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Find Read Trust Visual Rhythm`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/3-1-find-tenant-scoped-conversations-safely.md`
- `_bmad-output/project-context.md#Critical Don't-Miss Rules`
- `docs/adrs/index.md`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationQuery.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailResult.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchTrustPreviewV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs`
- `src/Hexalith.Conversations.Contracts/Queries/GetConversationAtPointInTimeQuery.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationTimelineMessageProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`
- `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0`
- `https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests`
- `https://learn.microsoft.com/nuget/consume-packages/central-package-management`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-22: Red phase confirmed with focused contract/server tests failing on missing Story 3.2 trust/evidence contracts.
- 2026-05-22: Focused contract tests passed: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 20 passed.
- 2026-05-22: Focused server read tests passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationReadHydrationServiceTest|FullyQualifiedName~ConversationReadApiTest"` - 93 passed.
- 2026-05-22: Boundary regression tests passed: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Temporal|FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryRegistrationTest"` - 148 passed.
- 2026-05-22: Full regression passed: `dotnet test Hexalith.Conversations.slnx` - 653 passed.
- 2026-05-22: AI review fixed evidence chronology for participant/attachment entries, fail-closed participant-resolution aggregation, and broad projection-store exception coarsening.

### Implementation Plan

- Extend the existing governed detail projection/query contracts instead of adding a transcript/viewer route or DTO stack.
- Reuse approved trust vocabularies for freshness, citation, audit readiness, verification, and completeness/participant resolution states; add only server-owned command availability metadata without introducing new command availability or evidence completeness vocabularies.
- Materialize governed evidence entries from projection state for messages, participants, attachments, retention policy, sensitivity marks, redactions, and freshness metadata while preserving chronological projection ordering and redaction placeholders.
- Preserve the existing read boundary ordering: tenant authorization and projection freshness checks remain in `ConversationProjectionReadService.ReadDetailAsync`; hydration stays response-scoped and only updates participant resolution aggregation.
- Keep the existing `GET /api/v1/conversations/{conversationId}` route and claim binding; do not add unsafe routes, mutation endpoints, exports, citation-copy, verification, or later Epic 3 UI workflows.

### Completion Notes List

- Added `ConversationEvidenceTrustPostureV1`, `ConversationEvidenceEntryV1`, and `ConversationCommandAvailabilityV1` contracts for governed detail reads.
- Extended `ConversationDetailProjectionV1` and `ConversationDetailsV1` with trust posture and evidence entries; missing metadata defaults to explicit unavailable/unknown/blocked states.
- Updated `ConversationProjectionMaterializer` to produce projection-owned trust posture, blocked command eligibility metadata, and governed evidence records with redaction-safe message placeholders.
- Updated detail hydration to aggregate worst-state participant resolution without changing projection ordering, evidence completeness, citation readiness, or command eligibility.
- Added focused contract, projection, hydration, query handler, and API tests, and updated the Story 3.2 test summary evidence.
- Added QA follow-up tests for non-current detail projection blocking, missing citation/partial evidence metadata propagation, malformed detail route hiding, and trusted claim binding.
- No new routes, durable stores, transcript tables, export artifacts, evidence bundles, background workers, cache authorities, command mutation workflows, or new public trust-state vocabularies were added.

### File List

- `_bmad-output/implementation-artifacts/3-2-read-governed-conversation-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationFileReferenceProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationParticipantProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Hydration/ConversationReadHydrationServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`

## Senior Developer Review (AI)

Reviewer: Jerome on 2026-05-22

### Findings Fixed

- [HIGH] Evidence entries for participants and attachments used the detail projection's last-applied timestamp instead of the source event timestamp, so non-message evidence could be chronologically misplaced. Fixed by carrying optional `OccurredAt` timestamps on participant/file projections, materializing them from event metadata, and asserting full evidence-entry ordering.
- [MEDIUM] Participant-resolution aggregation treated `Forbidden` as less severe than stale/rebuilding/unavailable states. Fixed the worst-state priority so forbidden hydration remains fail-closed even when mixed with other degraded states.
- [MEDIUM] Projection detail/list read stores only coarsened selected exception types. Fixed read/list paths to coarsen all non-cancellation store exceptions to unavailable, with regression coverage for infrastructure-shaped exceptions.

### Validation

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 20 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationReadHydrationServiceTest|FullyQualifiedName~ConversationReadApiTest"` - 93 passed.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Temporal|FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryRegistrationTest"` - 148 passed.
- `dotnet test Hexalith.Conversations.slnx` - 653 passed.

### Outcome

Approved after automatic fixes. No critical issues remain.

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, with full Epic 3 and Story 3.2 sections extracted.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR56-FR69, FR58, temporal/citation/read-only boundaries, and NFR freshness/redaction/accessibility obligations.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on API/format/state/error/loading/ADR/file-boundary patterns.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-requirement-map.md` and `_bmad-output/planning-artifacts/ux-design-specification.md`, focusing on governed record header, trust posture strip, evidence completeness, evidence timeline entries, trust primitives, and trust-before-reliance order.
  - Loaded project context from `_bmad-output/project-context.md` and sibling module project-context summaries for EventStore, Tenants, Parties, FrontComposer, and related dependencies.
  - Loaded previous Story 3.1 and recent git history.
  - Read current implementation files for detail query contracts, projection detail contracts, timeline message projection, projection read service, query handler, read API, hydration, temporal reconstruction, and focused tests.
  - Checked official Microsoft documentation for Minimal API binding/route groups, `dotnet test --filter`, and NuGet Central Package Management.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to the existing detail read boundary instead of a new transcript/viewer stack.
  - Added explicit guardrails for trust posture, evidence completeness, participant resolution, citation status, command eligibility, and current-only reliance.
  - Added older/missing metadata downgrade requirements learned from Story 3.1 review.
  - Blocked full Story 3.3/3.4/3.5/3.6/3.7/3.8 scope from leaking into 3.2.
  - Added focused test commands and likely file touch list.
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped implementation tasks, architecture/UX guardrails, current-code constraints, previous-story learnings, test requirements, and ADR stop conditions.

## Change Log

- 2026-05-22: Created Story 3.2 context from Epic 3 requirements, PRD/architecture/UX/readiness/project context, previous Story 3.1 learnings, current detail-query/projection/read API code, recent git history, and official Microsoft documentation.
- 2026-05-22: Implemented governed evidence read contracts, projection-owned trust posture/evidence entries, hydration participant-resolution aggregation, API/read-boundary tests, and Story 3.2 validation evidence.
- 2026-05-22: AI review fixed evidence-entry chronology, fail-closed participant-resolution aggregation, projection-store exception coarsening, and updated Story 3.2 status to done.
