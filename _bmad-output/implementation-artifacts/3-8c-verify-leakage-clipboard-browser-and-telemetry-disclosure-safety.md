# Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator and release owner,
I want forbidden values absent from every investigation disclosure surface,
so that protected content, tenant boundaries, and governance state remain safe across rendered UI, browser surfaces, clipboard output, telemetry, and evidence artifacts.

## Acceptance Criteria

1. Leak Sentinel covers every rendered disclosure surface
   - Given Leak Sentinel and canonical disclosure fixtures are prepared,
   - When desktop, tablet, mobile, screen-reader, clipboard, tooltip, browser-title, telemetry, loading, empty, denied, redacted, stale, and responsive-duplicate states are exercised,
   - Then forbidden strings and structured values are absent from rendered DOM text, attributes, ARIA properties, page title, clipboard output, telemetry envelopes, screenshots, and accessibility snapshots where available,
   - And every result is traceable to the fixture, surface, and release-evidence path.

2. Browser, clipboard, route, tooltip, toast, empty, loading, and telemetry states are permission-safe
   - Given command availability, tenant isolation, redaction, and projection freshness change during an investigation workflow,
   - When browser titles, route metadata, breadcrumbs, tooltips, toasts, empty/loading states, telemetry events, screenshots, and copied citation payloads are emitted,
   - Then each surface uses permission-safe DTOs and approved bounded identifiers only,
   - And unauthorized, nonexistent, cross-tenant, redacted, hidden, stale, and unavailable states do not leak tenant, Party, conversation, provider, file, business-reference, prompt, or content values.

3. Disclosure failures are actionable release blockers unless explicitly waived
   - Given disclosure tests fail,
   - When the report is generated,
   - Then it names the exact surface, forbidden value class, fixture, owner, expected result, actual result, and blocking/non-blocking classification,
   - And the story cannot close until unsafe output is fixed or a new approved waiver records owner, approver, expiry, compensating control, buyer impact, and review date.

## Tasks / Subtasks

- [ ] Reopen-aware preflight and UI-host decision record (AC: 1-3)
  - [ ] Confirm the 2026-05-24 reopen in `sprint-status.yaml`, `readiness-gates.md`, and this story before coding. Stories 3.8A, 3.8B, and 3.8C are all ready-for-dev and remain separate work items.
  - [ ] Verify whether a real first-party rendered investigation workspace host exists. Current repository state has no `Hexalith.Conversations.Admin`, `Hexalith.Conversations.FrontComposer`, `Hexalith.Conversations.Admin.Web`, Razor component project, or Playwright workspace.
  - [ ] If no rendered host exists, create or adopt the narrowest first-party UI-host slice required to render Find -> Read -> Trust against synthetic fixtures. Do not satisfy this story with DTO-only/server-only tests.
  - [ ] If implementation cannot add or run a rendered host, stop and mark the story blocked. Do not mark disclosure evidence as done without a rendered surface.
  - [ ] Record owner, fixture set, evidence output, pass/fail gate, and review date in the implementation notes before moving to review:
    - Owner: Test Architect / Security disclosure evidence owner, Developer implementation owner.
    - Fixture set: canonical disclosure fixtures listed below.
    - Evidence output: `_bmad-output/implementation-artifacts/evidence/3-8c-leakage-clipboard-browser-telemetry-disclosure-safety/`.
    - Pass/fail gate: all ACs pass with no forbidden string or structured value in rendered DOM text, attributes, ARIA properties, page title, route metadata, clipboard output, telemetry envelopes, screenshots, accessibility snapshots where available, traces, or evidence summaries.
    - Review date: 2026-05-31 or before merge, whichever comes first.

- [ ] Define Leak Sentinel and forbidden value catalog (AC: 1-3)
  - [ ] Reuse the existing conformance/test safety vocabulary where possible before adding new scanning helpers. Extract shared helpers only if it reduces duplication across DOM, telemetry, clipboard, screenshot, and evidence scans.
  - [ ] Define fixture-specific forbidden strings and structured values for tenant IDs/names, Party IDs/names, conversation IDs, provider names/types/session IDs, business references, file names, prompt/content snippets, raw event positions, raw EventStore envelopes, privileged caller metadata, and redaction source details.
  - [ ] Include cross-tenant poison values that are unique, high-signal, and never valid safe labels.
  - [ ] Define approved bounded identifiers and safe labels separately from forbidden values so tests do not reject legitimate closed-vocabulary outputs such as `Redacted`, `Restricted`, `Unavailable`, `Stale`, `Forbidden`, and `Some events unavailable`.
  - [ ] Ensure scanner output is content-safe. Failure reports may name value class and fixture alias, but must not echo protected values into logs, traces, screenshots, or release evidence.

- [ ] Build or adapt the rendered disclosure-safe workspace (AC: 1, 2)
  - [ ] Preferred architecture is FrontComposer + Blazor + Fluent UI, following the existing FrontComposer research path: `Hexalith.Conversations.FrontComposer` for annotated UI contracts and `Hexalith.Conversations.Admin.Web` or equivalent for the interactive host.
  - [ ] Ensure every rendered surface receives already-authorized/redacted DTOs. Components must not receive full records and hide unsafe fields with CSS, inactive tabs, offscreen panels, `display:none`, visually hidden text, route state, or responsive breakpoints.
  - [ ] Keep browser titles, document metadata, route parameters, breadcrumbs, recent-item labels, tab labels, telemetry tags, and copied payloads on approved safe identifiers only.
  - [ ] Do not query raw event streams, raw logs, EventStore envelopes, aggregate IDs, projection internals, provider sessions, durable provider IDs, or privileged caller claims from UI code.
  - [ ] Keep command metadata server-owned: eligibility, disabled state, required permission, precondition, risk level, freshness requirement, audit requirement, and safe blocked reason.

- [ ] Implement canonical disclosure fixtures and evidence harness (AC: 1-3)
  - [ ] Reuse `BuyerAcceptanceDemoFixtures` and Story 3.7 trust-state vocabulary where possible. Extend in `src/Hexalith.Conversations.Testing` if reusable fixture builders are needed.
  - [ ] Cover at minimum:
    - `TenantA_Admin_FullTrust`
    - `TenantA_Reviewer_RedactedParticipants`
    - `TenantA_MobileTriage_ReadOnly`
    - `TenantB_NoAccess_CrossTenantPoison`
    - `UnauthorizedExisting_IndistinguishableFromMissing`
    - `Nonexistent_IndistinguishableFromUnauthorized`
    - `PermissionDowngrade_WhileDrawerOpen`
    - `MissingCitation_IncompleteEvidence`
    - `UnresolvedParticipant_DegradedHydration`
    - `BlockedCommand_SafeReasonOnly`
    - `StaleProjection_CommandUnavailable`
    - `MixedTimeline_PartialLoad_RedactedEvents`
    - `VirtualizedTimeline_RestrictedRowsAdjacentToVisibleRows`
    - `ResponsiveDuplicate_StickyHeaderDrawerCard`
    - `TooltipToastLoadingEmptyDenied_Redacted`
    - `ClipboardCitation_CopyPayload`
    - `BrowserTitleRouteMetadata_SafeIdentifiers`
    - `TelemetryEnvelope_RedactedAndBounded`
    - `ScreenshotTraceEvidence_ContentSafe`
  - [ ] Exercise desktop, tablet, mobile, wide desktop, high contrast, reduced motion, browser zoom, loading, empty, denied, stale, redacted, hidden, unavailable, permission downgrade, and responsive duplicate states.
  - [ ] Evidence must include machine-readable test results, surface matrix, fixture matrix, forbidden-value-class matrix, screenshots/traces where useful, telemetry scan output, clipboard scan output, and a release-evidence summary.

- [ ] Add DOM, attribute, ARIA, and browser-surface scans (AC: 1, 2)
  - [ ] Add Playwright coverage for the rendered host using role/label selectors or stable `data-testid` contracts. Do not use CSS-class selectors or arbitrary text selectors for framework behavior.
  - [ ] Scan rendered DOM text, attributes, form values, data attributes, links, hidden DOM, visually hidden text, ARIA properties, live-region text, headings, table summaries, tooltip text, toast text, empty/loading/error text, and responsive duplicate markup.
  - [ ] Scan document title, route metadata, breadcrumbs, tab labels, link URLs, query strings, hash fragments, and browser-visible history state for forbidden values.
  - [ ] Verify unauthorized-existing and nonexistent cases remain indistinguishable across visible UI, hidden UI, titles, routes, telemetry, screenshots, and clipboard output.
  - [ ] Verify permission downgrade while a drawer, menu, or detail panel is open removes unsafe content before any new browser, telemetry, screenshot, or copied-output artifact is emitted.

- [ ] Add clipboard, citation, and browser copy-path checks (AC: 1, 2)
  - [ ] Test all supported copy actions: citation copy, stable temporal link copy, visible cell/card copy if supported, keyboard shortcut copy if supported, and any export/copy affordance introduced by the rendered host.
  - [ ] Assert copied payloads contain only approved safe identifiers, redaction-safe labels, bounded citation metadata, and permitted temporal anchors.
  - [ ] Assert copied payloads do not include raw tenant/Party/conversation/provider/file/business-reference/prompt/content values, raw event positions outside approved temporal cursor shape, redaction source details, or privileged caller metadata.
  - [ ] If a copy action is unavailable, assert it is absent or disabled with a content-safe reason.

- [ ] Add telemetry and evidence-artifact disclosure scans (AC: 1-3)
  - [ ] Reuse Story 6.8A/6.8B telemetry redaction and cardinality vocabulary for operational telemetry expectations. Do not invent new high-cardinality label shapes for UI telemetry.
  - [ ] Scan telemetry event names, tags, dimensions, counters, logs, traces, exception-safe messages, browser console output, and test harness output for forbidden values.
  - [ ] Scan screenshots, Playwright traces, accessibility snapshots where available, generated markdown summaries, JSON evidence, and release-evidence links before saving or attaching them.
  - [ ] Keep failure reports content-safe: name the surface and forbidden value class, not the protected value itself.

- [ ] Add failure classification and release blocking rules (AC: 3)
  - [ ] Classify failures as blocking by default when a forbidden protected value reaches a rendered, browser, clipboard, telemetry, screenshot, trace, accessibility snapshot, or release-evidence surface.
  - [ ] Allow non-blocking classification only when the surfaced value is in the approved bounded safe-label catalog and the report explains why it is safe.
  - [ ] Require a new approved waiver if implementation intentionally ships a remaining disclosure gap. The waiver must name owner, approver, expiry, compensating control, buyer impact, and review date.

- [ ] Update release evidence and status artifacts after implementation (AC: 3)
  - [ ] Add a Story 3.8C evidence summary under `_bmad-output/implementation-artifacts/evidence/3-8c-leakage-clipboard-browser-telemetry-disclosure-safety/` or a more specific release-evidence path approved during implementation.
  - [ ] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with disclosure/browser evidence, fixture matrix, surface matrix, telemetry/clipboard scan results, and executed commands.
  - [ ] Update conformance manifest/release evidence only if the implementation produces evidence that should replace the superseded waiver reference. Avoid touching `docs/release-evidence/conformance-manifest-v1-fixture.json` blindly because it already has unrelated local edits in this worktree.
  - [ ] Keep Story 3.8A responsive evidence and Story 3.8B accessibility evidence separate. Do not claim closure for those domains from this story except for disclosure scans over their surfaces.

- [ ] Preserve scope boundaries and stop conditions (AC: 1-3)
  - [ ] Do not implement Story 3.8A full responsive layout/mobile safe-triage verification beyond viewport/surface coverage required for disclosure scans.
  - [ ] Do not implement Story 3.8B full keyboard-only walkthrough, screen-reader transcript, or WCAG evidence beyond accessibility-snapshot disclosure scans where available.
  - [ ] Do not implement full evidence bundle export, named waiver runtime workflow, release signing, legal hold automation, retention editor, global admin browsing, Memories/RAG indexes, or transcript tables.
  - [ ] Stop for ADR or explicit approval if implementation needs a new durable authority, persistent browser storage for protected data, mobile governance mutation, cross-tenant global operator search, raw event browser, telemetry containing high-cardinality business values, or UI-owned trust state.

## Dev Notes

### Reopen Context

- Story 3.8C was waived on 2026-05-24 because v1 was recorded as headless and no rendered UI host existed. Jerome reopened 3.8A, 3.8B, and 3.8C on 2026-05-24. The reopen is captured in `sprint-status.yaml`, `readiness-gates.md`, `readiness-gate-decisions-2026-05-24.md`, and the three story files. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-24.md#Reopen Addendum: Stories 3.8A, 3.8B, and 3.8C`]
- The reopen keeps the split evidence domains separate. 3.8A owns responsive layout and mobile safe-triage evidence; 3.8B owns accessibility tree, keyboard, and screen-reader safety; 3.8C owns rendered leakage, clipboard, browser, and telemetry disclosure safety. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Story 3.8 assignment plan`]
- The old waiver is superseded historical context, not a completion claim. DTO-level non-disclosure from Stories 3.1-3.7 and telemetry redaction/cardinality from 6.8A/6.8B are useful controls, but they do not prove rendered disclosure safety. [Source: `_bmad-output/implementation-artifacts/deferred-work.md`; `docs/release-evidence/waiver-story-3-8-investigation-workspace-ui-host.json`]

### Epic and Business Context

- Epic 3 is the compliance investigation workspace. Stories 3.1-3.7 delivered tenant-safe find/read, governed evidence, redaction/audit inspection, citations, temporal links, command gates, verification results, and buyer demo contracts/services. Story 3.8C verifies every rendered, browser, clipboard, telemetry, and evidence disclosure surface created by that workflow. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety`; `_bmad-output/implementation-artifacts/epic-3-retro-2026-05-24.md`]
- Disclosure leakage is a release concern, not only a UI bug. FR56-FR69 require safe investigation workflows, and NFR19-NFR21, NFR55-NFR61, and NFR69-NFR77 require safe non-disclosure, observability, and operator usability. [Source: `_bmad-output/planning-artifacts/prd.md#FR56-FR69`; `_bmad-output/planning-artifacts/prd.md#NFR69`; `_bmad-output/planning-artifacts/prd.md#NFR77`]
- The operator promise is workflow-oriented: locate by external identifier, read governed transcript/evidence, understand redaction/audit/freshness, cite safely, and stop without crossing tenant or disclosure boundaries. Story 3.8C proves those boundaries across non-obvious surfaces. [Source: `_bmad-output/planning-artifacts/prd.md#Operator workflow`; `_bmad-output/planning-artifacts/prd.md#FR56-FR69`]

### Current Implementation State

- The current solution contains `Contracts`, `Client`, domain, `Server`, `ServiceDefaults`, `AppHost`, `Testing`, and test projects. It does not contain a Conversations Admin, FrontComposer, Razor component, browser UI, or Playwright project today. [Source: `Hexalith.Conversations.slnx`; `src/`; `tests/README.md`]
- Current tests are backend/.NET focused. `tests/README.md` says browser/E2E tooling should be added only when a Conversations UI surface exists and recommends Playwright when that surface exists. [Source: `tests/README.md`]
- Story 3.7 added deterministic buyer acceptance fixtures and read-only demo services. Those fixtures are the best starting point for disclosure scenarios; do not create a parallel transcript/demo model. [Source: `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`; `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemoFixtures.cs`]
- Stories 6.8A and 6.8B added telemetry redaction and cardinality validation. Reuse their vocabulary and expectations when scanning UI telemetry. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/implementation-artifacts/tests/test-summary.md`]
- There are existing local edits in unrelated files, including `docs/release-evidence/conformance-manifest-v1-fixture.json`. Work with them; do not revert or overwrite unrelated changes. [Source: `git status --short` observed during story creation]

### Disclosure Guardrails

- Disclosure surfaces include URLs, browser titles, breadcrumbs, DOM text, hidden DOM, responsive duplicates, ARIA labels, live regions, tooltips, clipboard payloads, logs, traces, metrics, screenshots, and release evidence. Story 3.8C owns full rendered-surface disclosure verification. [Source: `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`]
- Permission-safe DTOs are required before rendering. CSS hiding, `display:none`, inactive tabs, offscreen panels, viewport-only hiding, and visually hidden text are not authorization or redaction controls. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Disclosure Boundary`]
- Copy/export safety requires copied payloads to use safe identifiers and redaction-aware labels. Raw protected content, tenant details, provider session values, file names, business references, and prompt/content text must not enter clipboard output. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR35`]
- Leak Sentinel must scan DOM text, attributes, ARIA, page title, clipboard output, telemetry, screenshots, and accessibility snapshots. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR51`]
- Canonical fixtures must cover all UI states and redaction/tenant/safety edges. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR52`]

### Architecture Guardrails

- UI is not the authority. Conversations owns command validation, EventStore persistence, projections, audit pairing, tenant filtering, redaction semantics, temporal reconstruction, and verification results. UI renders governed projections and command metadata. [Source: `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`; `_bmad-output/planning-artifacts/research/technical-hexalith-frontcomposer-administration-ui-for-hexalith-conversations-research-2026-05-10.md#Target Administration Architecture`]
- Projection freshness must remain explicit. Command success does not imply query visibility; trust-bearing UI must distinguish current, stale, rebuilding, unavailable, forbidden, and redacted states. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`; `_bmad-output/planning-artifacts/architecture.md#Read-Model And Performance Implications`]
- Command availability metadata is server-owned. UI telemetry, tooltips, toasts, and disabled-state copy must not infer protected facts from client-side authorization logic. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Command availability metadata`]
- Release evidence must stay content-safe. Evidence summaries may identify fixture aliases, surface classes, and failure classes; they must not copy protected values into markdown, JSON, traces, screenshots, or logs. [Source: `_bmad-output/planning-artifacts/architecture.md#Architecture Verification Strategy`]

### Current Files To Read Before Editing

Read these files completely before modifying related behavior:

- `Hexalith.Conversations.slnx`
- `Directory.Packages.props`
- `src/Hexalith.Conversations.AppHost/Program.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Governance/ConversationBuyerAcceptanceDemoService.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationDetailProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationSummaryProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Projections/ConversationTimelineMessageProjectionV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs`
- `src/Hexalith.Conversations.Contracts/Governance/BuyerAcceptanceDemoContracts.cs`
- `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemoFixtures.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationBuyerAcceptanceDemoServiceTest.cs`
- `tests/README.md`
- `_bmad-output/implementation-artifacts/3-8a-verify-responsive-layout-and-mobile-safe-triage.md`
- `_bmad-output/implementation-artifacts/3-8b-verify-accessibility-tree-keyboard-and-screen-reader-safety.md`
- `_bmad-output/implementation-artifacts/6-8a-validate-operational-telemetry-redaction.md`
- `_bmad-output/implementation-artifacts/6-8b-validate-operational-telemetry-cardinality-gates.md`

If adopting FrontComposer directly, also read the local FrontComposer sample/docs before coding:

- `Hexalith.FrontComposer/samples/Counter/Counter.Web/Program.cs`
- `Hexalith.FrontComposer/docs/skills/frontcomposer/samples/new-bounded-context.md`
- `Hexalith.FrontComposer/docs/skills/frontcomposer/security/tenant-and-policy-boundaries.md`
- `Hexalith.FrontComposer/docs/skills/frontcomposer/testing/generated-code-validator.md`

### Likely File Changes

Expected new files if no UI host exists:

- `src/Hexalith.Conversations.FrontComposer/**`
- `src/Hexalith.Conversations.Admin.Web/**`
- `tests/Hexalith.Conversations.FrontComposer.Tests/**`
- `tests/Hexalith.Conversations.Admin.Web.Tests/**`
- `tests/e2e/**` or a dedicated .NET Playwright test project, depending on the chosen browser-test pattern
- Shared test helper for forbidden value scanning if needed
- `_bmad-output/implementation-artifacts/evidence/3-8c-leakage-clipboard-browser-telemetry-disclosure-safety/**`

Expected updates:

- `Hexalith.Conversations.slnx`
- `Directory.Packages.props` if new packages are needed
- `src/Hexalith.Conversations.AppHost/Program.cs` if the Admin/Web host joins local Aspire composition
- `tests/README.md` if browser/E2E tooling is added
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Release evidence or conformance manifest entries only after evidence exists and only with care for existing unrelated local edits

### Previous Story Intelligence

- Story 3.7 intentionally did not scaffold an Admin/FrontComposer shell or browser UI because no approved UI host existed. It produced deterministic synthetic fixtures, read-only scenario runner behavior, content-safe evidence summaries, and the existing server/contract baseline. Story 3.8C should reuse those fixtures and prove rendered disclosure behavior, not redo the backend demo. [Source: `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md#Completion Notes List`]
- Story 3.7 review fixes are directly relevant: verification evidence must match scenario tenant/conversation scope; cross-tenant denial must actually use a different tenant; missing caller authority must fail closed; temporal cursors must use the canonical composite shape; scenario steps cannot reference undeclared fixture kinds. [Source: `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md#Senior Developer Review (AI)`]
- Stories 3.4-3.6 repeatedly scoped responsive/accessibility/clipboard/browser-title/telemetry/Leak Sentinel evidence out to 3.8A-3.8C. Do not claim those earlier stories covered rendered-surface leakage behavior. [Source: `_bmad-output/implementation-artifacts/3-4-copy-citations-and-open-stable-temporal-evidence-links.md`; `_bmad-output/implementation-artifacts/3-5-preserve-read-only-compliance-workflows-and-safe-command-gates.md`; `_bmad-output/implementation-artifacts/3-6-run-governance-verification-and-return-structured-results.md`]
- Story 6.8A/6.8B evidence is relevant for telemetry redaction and cardinality only. It does not prove UI telemetry, browser console, clipboard, screenshot, route, title, or DOM disclosure safety. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/implementation-artifacts/tests/test-summary.md`]

### Latest Technical Information

- Fluent UI Blazor MCP documentation is for `Microsoft.FluentUI.AspNetCore.Components` version `5.0.0.26098`. Conversations currently has no Fluent UI package reference. Sibling FrontComposer context pins `5.0.0-rc.2-26098.1`; if a direct package reference is unavoidable, align centrally with the local FrontComposer dependency decision instead of adding an arbitrary latest version inline. [Source: Fluent UI Blazor MCP `get_version_info`; `Hexalith.FrontComposer/_bmad-output/project-context.md#Technology Stack & Versions`]
- Fluent UI Blazor installation requires `AddFluentUIComponents()`, a `FluentProviders` layout component, required styles, and interactive rendering. If the Admin host uses Fluent UI directly, ensure render mode, services, styles, and providers are registered. [Source: Fluent UI Blazor MCP `Installation` docs]
- Blazor layouts are reusable Razor components, usually under `Shared` or `Layout`; component-specific `.razor.css` files are the normal scoped styling path. [Source: Microsoft Learn, ASP.NET Core Blazor layouts and CSS isolation]
- Playwright for .NET can create isolated browser contexts with viewport, color scheme, reduced motion, traces, screenshots, permissions, clipboard interaction, and page metadata inspection. Use that isolation for disclosure-surface scans and evidence capture. [Source: Context7 `/microsoft/playwright-dotnet` docs]

### Testing Requirements

Minimum focused validation after implementation:

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationTestIds"`
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest"`
- New FrontComposer/Admin/bUnit disclosure test project commands introduced by the implementation
- New Playwright disclosure suite covering DOM/attribute/ARIA/title/route/clipboard/telemetry/screenshot/evidence surfaces
- `dotnet test Hexalith.Conversations.slnx`

Evidence validation must include:

- Surface matrix for DOM text, attributes, ARIA, page title, route metadata, breadcrumbs, tooltips, toasts, empty/loading/denied states, clipboard output, telemetry envelopes, browser console, screenshots, traces, accessibility snapshots where available, and release-evidence files.
- Fixture matrix for tenant-safe, redacted, denied, nonexistent, cross-tenant, stale, unavailable, partial-load, permission-downgrade, responsive-duplicate, tooltip/toast, clipboard, telemetry, and screenshot states.
- Forbidden-value-class matrix with content-safe failure reports.
- Safe-label allowlist and bounded-identifier allowlist.
- Blocking/non-blocking classification for every failure.

### Out of Scope

- Story 3.8A: full responsive layout matrix, mobile safe-triage verification, trust-order layout assertions, and viewport-specific telemetry label checks beyond what is needed for disclosure surface coverage.
- Story 3.8B: full accessibility-tree, keyboard-only walkthrough, screen-reader transcript, accessible-name leakage suite, WCAG evidence, and manual assistive-technology evidence beyond accessibility-snapshot disclosure scans where available.
- Full Generate Evidence Bundle workflow, release signing, named waiver runtime approval workflow, durable evidence store, legal hold automation, retention editor, global admin search, cross-tenant operator browsing, transcript tables, Memories/RAG indexes, browser/local storage for protected data, and raw EventStore stream browsing.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety`
- `_bmad-output/planning-artifacts/prd.md#FR56-FR69`
- `_bmad-output/planning-artifacts/prd.md#NFR19`
- `_bmad-output/planning-artifacts/prd.md#NFR55`
- `_bmad-output/planning-artifacts/prd.md#NFR69`
- `_bmad-output/planning-artifacts/prd.md#NFR77`
- `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`
- `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`
- `_bmad-output/planning-artifacts/architecture.md#Architecture Verification Strategy`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Disclosure Boundary`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Disclosure Boundary`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/planning-artifacts/research/technical-hexalith-frontcomposer-administration-ui-for-hexalith-conversations-research-2026-05-10.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-24.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`
- `_bmad-output/implementation-artifacts/6-8a-validate-operational-telemetry-redaction.md`
- `_bmad-output/implementation-artifacts/6-8b-validate-operational-telemetry-cardinality-gates.md`
- `_bmad-output/implementation-artifacts/epic-3-retro-2026-05-24.md`
- `_bmad-output/project-context.md`
- `Hexalith.FrontComposer/_bmad-output/project-context.md`
- `tests/README.md`
- Fluent UI Blazor MCP docs: version info, installation, `FluentDataGrid`, `FluentTabs`
- Microsoft Learn: ASP.NET Core Blazor layouts, CSS isolation, accessibility testing
- Context7: `/microsoft/playwright-dotnet`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List

## Story Context Validation

- Checklist reviewed: `.agents/skills/bmad-create-story/checklist.md`.
- Input discovery completed:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 3 and Story 3.8C acceptance criteria and split boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR56-FR69, NFR19-NFR21, NFR55-NFR61, and NFR69-NFR77.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on disclosure surfaces, UX trust contract, FrontComposer boundaries, testing/release evidence, and project structure.
  - Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md`, focusing on Leak Sentinel, clipboard safety, browser title/route safety, responsive duplicate leakage, telemetry disclosure, and canonical fixtures.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md` plus FrontComposer and umbrella workspace context for package, UI, testing, and submodule guardrails.
  - Loaded previous Story 3.7, telemetry Stories 6.8A/6.8B, and recent status/waiver artifacts, including the 2026-05-24 reopen decision.
  - Checked official/current docs through Microsoft Learn, Fluent UI Blazor MCP, and Context7 Playwright .NET docs during the 3.8 story creation pass.
- Checklist fixes applied:
  - Story explicitly prevents DTO-only completion and requires a real rendered surface.
  - Story names owner, fixture set, evidence output, pass/fail gate, and review date.
  - Story separates 3.8C from independently reopened 3.8A/3.8B evidence domains.
  - Story identifies existing files to read and likely new/updated project locations.
  - Story adds specific stop conditions for missing UI host, unsafe browser/clipboard/telemetry/evidence output, hidden authorization, UI-owned trust state, mobile mutation, high-cardinality telemetry, and raw EventStore browsing.
- Validation result: ready-for-dev.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Change Log

- 2026-05-24: Reopened Story 3.8C per Jerome's instruction, marked the rendered-UI waiver superseded for the full 3.8 split, and created this ready-for-dev story context.
