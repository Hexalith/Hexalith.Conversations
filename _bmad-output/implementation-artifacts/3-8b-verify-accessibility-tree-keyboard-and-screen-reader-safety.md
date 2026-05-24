# Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance operator using keyboard navigation or assistive technology,
I want investigation trust, citation, redaction, and command-gate states to be exposed safely,
so that accessible workflows preserve the same evidence ordering and non-disclosure guarantees as visual workflows.

## Acceptance Criteria

1. Keyboard and assistive-technology order preserves trust before reliance
   - Given a keyboard-only or screen-reader user completes the Find -> Read -> Trust workflow,
   - When focus moves through scope, identity, trust posture, evidence completeness, command gates, timeline, citation controls, audit controls, and diagnostics,
   - Then tenant scope, trust posture, evidence completeness, blocked-action reasons, and safe next actions are exposed before sensitive content reliance,
   - And redacted or unauthorized values are absent from accessible names, descriptions, live regions, headings, table summaries, focus announcements, and copied output.

2. Accessibility safety is proven across unsafe and degraded states
   - Given accessible workflows exercise no accessible matches, denied, redacted, stale, unresolved participant, blocked command, high-contrast, reduced-motion, browser zoom, and permission downgrade states,
   - When automated checks and manual keyboard/screen-reader evidence are generated,
   - Then the rendered workspace satisfies WCAG 2.1 AA expectations and safe state-announcement rules,
   - And each failure identifies component, disclosure surface, scenario, expected result, actual result, owner, and blocking classification.

3. Accessibility evidence is content-safe and traceable
   - Given snapshots, transcripts, focus traces, and assistive-technology notes are captured,
   - When the evidence is saved,
   - Then artifacts are tenant-safe, content-safe, and linked to the fixture set, readiness gate, conformance manifest, or release-evidence bundle.

## Tasks / Subtasks

- [x] Reopen-aware preflight against the now-existing rendered host (AC: 1-3)
  - [x] Confirm the 2026-05-24 reopen in `sprint-status.yaml`, `readiness-gates.md`, and this story before coding. Stories 3.8A, 3.8B, and 3.8C are separate work items; 3.8A is already in `review` and 3.8C is `ready-for-dev`.
  - [x] A real first-party rendered host now exists: `src/Hexalith.Conversations.Admin.Web` (created by Story 3.8A) renders the Find -> Read -> Trust workspace as HTML against synthetic fixtures, and `tests/Hexalith.Conversations.Admin.Web.Tests` runs a .NET Playwright evidence lane. **Do not recreate the host, the catalog, or a second demo model. Extend the existing surface.**
  - [x] Do not satisfy this story with DTO-only/server-only tests. Accessibility evidence must come from the rendered Admin Web surface exercised through a real browser (Playwright) plus manual keyboard/screen-reader passes.
  - [x] If the rendered host cannot be extended to prove accessible behavior (for example, the Playwright browser cannot be installed in the environment), stop and mark the story blocked rather than claiming accessibility evidence without a rendered surface. Browser install is via `pwsh tests/install-playwright.ps1`.
  - [x] Record owner, fixture set, evidence output, pass/fail gate, and review date in the implementation notes before moving to review:
    - Owner: UX / Test Architect accessibility evidence owner; Developer implementation owner.
    - Fixture set: the existing `BuyerAcceptanceInvestigationWorkspaceCatalog` fixtures plus any accessibility-specific extensions listed below.
    - Evidence output: `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/`.
    - Pass/fail gate: all ACs pass with automated accessibility checks, manual keyboard-only evidence, manual screen-reader evidence, and no forbidden sentinel in accessible names, descriptions, live regions, headings, table summaries, focus announcements, or copied output.
    - Review date: 2026-05-31 or before merge, whichever comes first.

- [x] Add accessibility semantics to the existing renderer (AC: 1, 2)
  - [x] Modify `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceRenderer.cs` (and the view model / catalog only if a safe field is genuinely missing) to express accessibility, not to add new disclosure. Keep all rendered text derived from the already-authorized/redacted `InvestigationWorkspaceViewModel` fields.
  - [x] Fix the heading outline. The renderer currently emits multiple `<h1>` elements (one per trust panel and for command eligibility) and `<h2>` per timeline row. Establish a coherent single-document heading hierarchy (one workspace/record `h1`, section `h2`s for trust panels/timeline/command gates, row-level `h3`s) so screen-reader heading navigation follows the trust order. Headings must never contain protected values.
  - [x] Add landmark structure and a skip affordance: a search/find landmark, the main workspace landmark (already `<main>`), and a way to jump to the governed record. Preserve the existing `aria-label`ed sections.
  - [x] Add `aria-live` (polite/assertive as appropriate) safe announcement regions for trust posture, freshness, command availability, redaction, and permission-downgrade changes. Live-region text must use the safe closed vocabulary (for example `Redacted`, `Unavailable`, `Restricted`, `Still loading`, `Some events unavailable`) and must announce class plus safe next action without protected detail.
  - [x] Expose blocked/disabled command reasons in the accessible description, not only in the `data-blocked-reason` attribute. Disabled governance buttons must surface the safe blocked reason and safe next action to assistive technology (for example via `aria-describedby` to safe text) without revealing whether a protected conversation, participant, provider, file, or event exists.
  - [x] Preserve server-owned command metadata in accessible state: eligibility, disabled state, required permission, precondition, risk level, freshness requirement, audit requirement, and safe blocked reason (already modeled on `ConversationCommandAvailabilityV1`).
  - [x] Keep the hidden-read path (`InvestigationWorkspaceViewModel.IsHiddenRead`) indistinguishable for unauthorized-existing and nonexistent records in the accessibility tree as well as the visual surface. The "no governed record is visible" message and accessible names must be identical for both.
  - [x] If virtualization or condensed timelines are added, preserve keyboard navigation, screen-reader context, and safe row summaries; offscreen content must not leave protected content in hidden DOM or hidden accessible text. (The current renderer emits the full timeline inline — do not introduce virtualization unless required, and if you do, test it.)

- [x] Ensure a keyboard-traversable focus order exists (AC: 1)
  - [x] The current surface is a single static HTML page with disabled buttons and no interactive drawers/menus. Provide a deterministic, testable keyboard focus order that traverses scope -> identity -> trust posture -> evidence completeness -> command gates -> timeline -> citation/audit controls -> diagnostics, using natural DOM order, headings, and focusable controls. Avoid positive `tabindex`; rely on document order.
  - [x] If a citation/audit affordance or drawer is added to make AC1 meaningful, it must trap focus only after authorization is established, must not briefly focus or announce protected content, and must restore focus on close. Adding any interactive affordance must not introduce UI-owned trust state or client-side authorization inference.
  - [x] Focus-visible styling must remain readable under high contrast / forced colors and 200 percent zoom (the renderer already has `@media (forced-colors: active)` and `prefers-reduced-motion` blocks — extend, do not remove them).

- [x] Reuse and extend canonical accessibility fixtures (AC: 1-3)
  - [x] Reuse the existing `BuyerAcceptanceInvestigationWorkspaceCatalog` (built from `BuyerAcceptanceDemoFixtures.Create()`), which already provides: `TenantA_Admin_FullTrust`, `TenantA_Reviewer_RedactedParticipants`, `TenantA_MobileTriage_ReadOnly`, `TenantB_NoAccess_CrossTenantPoison`, `MixedTimeline_PartialLoad_RedactedEvents`, `VirtualizedTimeline_RestrictedRowsAdjacentToVisibleRows`, `UnauthorizedExisting_IndistinguishableFromMissing`, `PermissionDowngrade_WhileDrawerOpen`, `MissingCitation_IncompleteEvidence`, `UnresolvedParticipant_DegradedHydration`, `HighContrast_ReducedMotion_BrowserZoom`.
  - [x] Cover the following accessibility scenarios, mapping each to an existing fixture or adding a narrow extension where a true gap exists (extend in `src/Hexalith.Conversations.Testing` or the catalog, keep them deterministic and synthetic):
    - `KeyboardOnly_FindReadTrust` (focus-order traversal; can be a test scenario over `TenantA_Admin_FullTrust`)
    - `AssistiveTech_RedactionAnnouncement` (safe live-region/redaction announcement; map to `TenantA_Reviewer_RedactedParticipants` / `MixedTimeline_PartialLoad_RedactedEvents`)
    - `BlockedCommand_SafeReasonOnly` (map to `TenantA_MobileTriage_ReadOnly` / `PermissionDowngrade_WhileDrawerOpen`)
    - `StaleProjection_CommandUnavailable` (add if no existing fixture exposes a stale/unavailable freshness state with blocked commands)
    - `Nonexistent_IndistinguishableFromUnauthorized` (pair with `UnauthorizedExisting_IndistinguishableFromMissing`; assert identical accessible output)
    - `TenantB_NoAccess_CrossTenantPoison`, `UnresolvedParticipant_DegradedHydration`, `MissingCitation_IncompleteEvidence`, `HighContrast_ReducedMotion_BrowserZoom` (existing)
  - [x] If you add fixtures, update the `InvestigationWorkspaceRendererTest.CatalogShouldExposeRequiredResponsiveFixtures` ordered expectation (it asserts the exact fixture list) so the 3.8A baseline does not regress.
  - [x] Include the existing cross-tenant poison sentinel values (`BuyerAcceptanceDemoSeedData.PoisonSentinelValues`) and assert they never appear in accessible names, descriptions, live regions, headings, table summaries, focus announcements, copied output, or evidence artifacts.

- [x] Add the automated accessibility evidence lane (AC: 1-3)
  - [x] Add an accessibility test class in `tests/Hexalith.Conversations.Admin.Web.Tests` that mirrors `Responsive/ResponsiveEvidenceHarnessTest.cs`: use the shared `[Collection(RenderedWorkspaceCollection.Name)]`, `AdminWebHostFixture` (process-hosted loopback server), and `PlaywrightFixture` (single headless Chromium). Write evidence under `RepositoryPaths.FindRoot()` + `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/`.
  - [x] Drive the browser with accessible role/label selectors (`Page.GetByRole`, `GetByLabel`) and stable `data-testid` contracts. Do not use CSS-class selectors or arbitrary text selectors for framework behavior. Tests must wait for observable state (`WaitForAsync`), not fixed sleeps.
  - [x] Capture the accessibility tree with `Locator.AriaSnapshotAsync()` (prefer this over the legacy `Accessibility.SnapshotAsync`) and assert: accessible role/name structure, heading outline order, landmark presence, and that trust-order names precede timeline reliance.
  - [x] Compute and scan accessible names/descriptions: `aria-label`, resolved `aria-describedby` text, `title`, headings, table/row summaries, and live-region text. Assert no poison sentinel and no protected value appears in any of them across every fixture.
  - [x] Exercise keyboard traversal in the browser (`Page.Keyboard.PressAsync("Tab")` sequence) and record the focus-order trace; assert scope/identity/trust/completeness/command-gate precede timeline reliance.
  - [x] Use browser contexts for high contrast / forced colors and reduced motion (the existing harness sets `ForcedColors.Active` + `ReducedMotion.Reduce` for `HighContrast_ReducedMotion_BrowserZoom`) and a 200 percent zoom equivalent, verifying focus visibility, labels, trust order, and safe blocked-action reasons survive.
  - [x] Emit machine-readable evidence: accessibility-tree/aria snapshot results, focus-order trace, accessible-name forbidden-sentinel scan, fixture matrix, and a content-safe `evidence-summary.md` suitable for release-evidence linkage. Keep the summary scoped to 3.8B and explicit that 3.8A responsive and 3.8C disclosure closure are owned elsewhere.
  - [x] Run the focused Admin Web lane first, then the existing .NET test lanes. Do not regress the 3.8A baseline (the full suite was 1519 passing after 3.8A).

- [x] Complete manual keyboard and screen-reader evidence (AC: 1-3)
  - [x] Run the Find -> Read -> Trust workflow with keyboard only against the running host (`dotnet run --project src/Hexalith.Conversations.Admin.Web`, then `http://127.0.0.1:<port>/investigations?fixture=<id>`). Record tab sequence, escape/close behavior for any added affordance, command-gate behavior, and recovery from denied/stale/permission-downgrade states.
  - [x] Run at least one screen-reader pass on Windows, preferably Narrator or NVDA, and record content-safe notes or transcripts. If another assistive technology is used, document the reason.
  - [x] Confirm the screen-reader order matches the trust order and does not announce protected content before tenant scope, trust posture, evidence completeness, and safe blocked-action context.
  - [x] Confirm high contrast, reduced motion, and 200 percent browser zoom preserve focus visibility, labels, trust order, and safe blocked-action reasons.

- [x] Update release evidence and status artifacts after implementation (AC: 3)
  - [x] Add a Story 3.8B evidence summary under `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/`.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with the accessibility/browser evidence, fixture matrix, manual evidence notes, and executed commands.
  - [x] Update `tests/README.md` to note the accessibility lane sits alongside the 3.8A responsive lane in `Hexalith.Conversations.Admin.Web.Tests`.
  - [x] Update conformance manifest/release evidence only if the implementation produces evidence that should replace the superseded waiver reference. Avoid touching `docs/release-evidence/conformance-manifest-v1-fixture.json` blindly because it already has unrelated local edits in this worktree.
  - [x] Keep Story 3.8A responsive evidence and Story 3.8C disclosure evidence separate. Do not claim closure for those domains from this story.

- [x] Preserve scope boundaries and stop conditions (AC: 1-3)
  - [x] Do not redo or regress Story 3.8A responsive layout/mobile safe-triage verification beyond accessibility-order assertions; keep the existing responsive lane green.
  - [x] Do not implement the Story 3.8C full Leak Sentinel, clipboard, browser-title, tooltip, screenshot-disclosure, or telemetry-disclosure suite beyond accessibility-specific leakage checks required here.
  - [x] Do not implement full evidence bundle export, named waiver runtime workflow, release signing, legal hold automation, retention editor, global admin browsing, Memories/RAG indexes, or transcript tables.
  - [x] Stop for ADR or explicit approval if implementation needs a new durable authority, persistent browser storage for protected data, mobile governance mutation, cross-tenant global operator search, raw event browser, or UI-owned trust state.

## Dev Notes

### Reopen Context

- Story 3.8B was waived on 2026-05-24 because v1 was recorded as headless and no rendered UI host existed. Jerome reopened 3.8A, 3.8B, and 3.8C on 2026-05-24. The reopen is captured in `sprint-status.yaml`, `readiness-gates.md`, `readiness-gate-decisions-2026-05-24.md`, and the three story files. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/implementation-artifacts/readiness-gates.md#Story 3.8 assignment plan`]
- The reopen keeps the split evidence domains separate. 3.8A owns responsive layout and mobile safe-triage evidence; 3.8B owns accessibility tree, keyboard, and screen-reader safety; 3.8C owns rendered leakage, clipboard, browser, and telemetry disclosure safety. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Story 3.8 assignment plan`]
- The old waiver is superseded historical context, not a completion claim. DTO-level non-disclosure from Stories 3.1-3.7 and telemetry redaction/cardinality from 6.8A/6.8B are useful controls but do not prove accessible rendered behavior. [Source: `_bmad-output/implementation-artifacts/deferred-work.md`; `docs/release-evidence/waiver-story-3-8-investigation-workspace-ui-host.json`]

### Current Implementation State (UPDATED after Story 3.8A)

This is the most important change since the prior draft of this story. **The rendered host now exists.** Story 3.8A (status `review`) created it. Do not assume a headless/no-UI state.

- `src/Hexalith.Conversations.Admin.Web` is a `Microsoft.NET.Sdk.Web` minimal-API host. It references only `Hexalith.Conversations.Contracts` and `Hexalith.Conversations.Testing`. Routes: `/` redirects to `/investigations`; `/health` returns ok; `/investigations/fixtures` returns the fixture catalog as JSON; `/investigations?fixture=<id>` returns the rendered HTML workspace. It exposes `public partial class Program;` for host tests. [Source: `src/Hexalith.Conversations.Admin.Web/Program.cs`; `src/Hexalith.Conversations.Admin.Web/Hexalith.Conversations.Admin.Web.csproj`]
- It is **not** Blazor, not FrontComposer, and has **no Fluent UI** dependency. `InvestigationWorkspaceRenderer.Render(InvestigationWorkspaceViewModel)` builds a complete standalone HTML document via `StringBuilder` with an inline `<style>` block and a small inline `<script>` that classifies the viewport and stamps `data-current-viewport` / `data-telemetry-label`. All text is `WebUtility.HtmlEncode`d. [Source: `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceRenderer.cs`]
- `InvestigationWorkspaceViewModel` is a permission-safe record carrying only safe labels plus already-authorized projection/query DTOs (`ConversationSummaryProjectionV1`, `ConversationDetailProjectionV1`, `ConversationEvidenceEntryV1`, `ConversationCommandAvailabilityV1`). `IsHiddenRead` is true when `Summary`/`Detail` are null (the safe-denial path). [Source: `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceViewModel.cs`]
- `BuyerAcceptanceInvestigationWorkspaceCatalog : IInvestigationWorkspaceCatalog` adapts `BuyerAcceptanceDemoFixtures.Create()` into the 11 fixtures listed above; `TenantA_Admin_FullTrust` is the default and the two `Hidden(...)` fixtures (`TenantB_NoAccess_CrossTenantPoison`, `UnauthorizedExisting_IndistinguishableFromMissing`) drive the indistinguishable safe-denial path. [Source: `src/Hexalith.Conversations.Admin.Web/Rendering/BuyerAcceptanceInvestigationWorkspaceCatalog.cs`]
- `tests/Hexalith.Conversations.Admin.Web.Tests` is the browser lane. It references `Microsoft.Playwright`, `xunit.v3`, and `Shouldly`. Patterns to reuse: `Fixtures/AdminWebHostFixture.cs` (starts `dotnet <assembly>` on a free loopback port, waits on `/health`), `Fixtures/PlaywrightFixture.cs` (single headless Chromium; throws a clear "run `pwsh tests/install-playwright.ps1`" message if the browser is missing), `Fixtures/RenderedWorkspaceCollection.cs` (xUnit collection sharing both fixtures), `Support/RepositoryPaths.cs` (`FindRoot()` locates `Hexalith.Conversations.slnx`), `Responsive/ResponsiveEvidenceHarnessTest.cs` (the harness to mirror), and `Rendering/InvestigationWorkspaceRendererTest.cs` (pure render/catalog assertions, including the exact fixture-list assertion). [Source: those files under `tests/Hexalith.Conversations.Admin.Web.Tests/`]
- Browser binaries are installed once per machine with `pwsh tests/install-playwright.ps1`. `tests/README.md` documents the Admin Web lane and the install step. [Source: `tests/install-playwright.ps1`; `tests/README.md`]
- There are existing local edits in unrelated files, including `docs/release-evidence/conformance-manifest-v1-fixture.json`. Work with them; do not revert or overwrite unrelated changes. The `src/Hexalith.Conversations.Admin.Web/bin` and `obj` build outputs are present in the working tree — do not commit build artifacts. [Source: `git status --short` observed during story creation]

### Accessibility Gaps in the Current Renderer (what to change)

The 3.8A renderer was built for responsive/trust-order evidence, not accessibility. Treat these as concrete work items, not optional polish:

- **Heading hierarchy is flat and duplicated.** Each trust panel and the command-eligibility panel emits an `<h1>`; timeline rows emit `<h2>`. There is no single document heading and no nested outline. Screen-reader heading navigation will be confusing and will not express the trust hierarchy. [Source: `InvestigationWorkspaceRenderer.RenderRankedPanel` / `RenderCommandEligibility` / `RenderTimeline`]
- **No live regions.** AC1/AC2 require safe announcements for trust/freshness/command/redaction/permission-downgrade changes; the renderer currently emits no `aria-live` region. [Source: `InvestigationWorkspaceRenderer`]
- **Blocked reasons are not in the accessible name/description.** Disabled governance buttons carry `data-blocked-reason` and visible "Blocked: <action>" text, but the safe reason is not wired to the accessible description. AC1 requires blocked-action reasons exposed to AT before reliance. [Source: `InvestigationWorkspaceRenderer.RenderCommandEligibility`]
- **Landmarks are minimal.** Only `<main>` and `aria-label`ed `<section>`s exist; there is no search landmark or skip affordance. [Source: `InvestigationWorkspaceRenderer.Render`]
- **What must be preserved:** the existing `data-testid` contracts (`workspace-root`, `tenant-scope`, `record-identity`, `trust-posture`, `evidence-completeness`, `command-eligibility`, `timeline`, `timeline-row`, `command-action`, `sticky-summary`, `authorized-drawer-summary`, `safe-skeleton`), the `data-trust-rank` ordering, the viewport-classifying script and telemetry-label stamping, the `forced-colors`/`prefers-reduced-motion` CSS blocks, the HTML-encoding of all text, and the indistinguishable hidden-read path. The 3.8A responsive harness and `InvestigationWorkspaceRendererTest` assert on these; keep them green.

### Epic and Business Context

- Epic 3 is the compliance investigation workspace. Stories 3.1-3.7 delivered tenant-safe find/read, governed evidence, redaction/audit inspection, citations, temporal links, command gates, verification results, and buyer demo contracts/services. Story 3.8B verifies accessible rendered use of that workflow on the host 3.8A built. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety`; `_bmad-output/implementation-artifacts/epic-3-retro-2026-05-24.md`]
- The accessible workflow is a release concern, not an enhancement. NFR69-NFR77 require operator/admin web experiences to be usable with keyboard navigation, assistive technology, high contrast, reduced motion, and safe announcements. Requirements covered: FR56-FR69 verification support; UX-DR44-UX-DR50, UX-DR52; NFR69-NFR77. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.8B`; `_bmad-output/planning-artifacts/prd.md#NFR69`; `_bmad-output/planning-artifacts/prd.md#NFR77`]
- The operator promise is workflow-oriented: locate by external identifier, read governed transcript/evidence, understand redaction/audit/freshness, cite safely, and stop without crossing tenant or disclosure boundaries. Accessibility evidence must prove that same promise for keyboard and assistive-technology users. [Source: `_bmad-output/planning-artifacts/prd.md#Operator workflow`; `_bmad-output/planning-artifacts/prd.md#FR56-FR69`]

### UX and Accessibility Guardrails

- Accessibility baseline is WCAG 2.1 AA. Keyboard-only, automated checks, high contrast, reduced motion, browser zoom, and manual screen-reader validation are required for high-risk workflows. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Requirements`]
- Focus order must follow the trust hierarchy: scope, trust posture, evidence completeness, command gates, timeline, citation/audit controls, diagnostics. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Requirements`]
- Assistive-technology output is user-visible. Accessible names, descriptions, live regions, headings, table summaries, copied text, browser titles, and focus order must obey tenant, permission, and redaction rules. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Disclosure Boundary`]
- Safe accessibility microcopy uses classes such as `Redacted`, `Unavailable`, `Restricted`, `Still loading`, and `Some events unavailable`; it must not include sensitive values in tooltip, ARIA, empty, error, live, or toast text. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR46`]
- Virtualized timelines must preserve order, navigation, focus, and screen-reader context. Offscreen virtualization must not leave protected content in hidden DOM or accessible text. [Source: `_bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR48`]

### Architecture Guardrails

- UI is not the authority. Conversations owns command validation, EventStore persistence, projections, audit pairing, tenant filtering, redaction semantics, temporal reconstruction, and verification results. UI renders governed projections and command metadata. [Source: `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`]
- Permission-safe DTOs are required before rendering. CSS hiding, `display:none`, inactive tabs, offscreen panels, viewport-only hiding, and visually hidden text are not authorization or redaction controls. The accessibility tree is a first-class disclosure surface. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Disclosure Boundary`; `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`]
- Projection freshness must remain explicit. Command success does not imply query visibility; trust-bearing UI must distinguish current, stale, rebuilding, unavailable, forbidden, and redacted states. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`]
- Command availability metadata is server-owned. Accessible disabled-state and blocked-reason text must not infer protected facts from client-side authorization logic. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Command availability metadata`]
- Generated command payloads and UI contracts must not include tenant identity, caller identity, tokens, claims, or host authorization context. The view models stay internal to `Hexalith.Conversations.Admin.Web`. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`]

### Current Files To Read Before Editing

Read these completely before modifying related behavior:

- `src/Hexalith.Conversations.Admin.Web/Program.cs`
- `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceRenderer.cs`
- `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceViewModel.cs`
- `src/Hexalith.Conversations.Admin.Web/Rendering/BuyerAcceptanceInvestigationWorkspaceCatalog.cs`
- `src/Hexalith.Conversations.Admin.Web/Rendering/IInvestigationWorkspaceCatalog.cs`
- `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceFixtureSummary.cs`
- `tests/Hexalith.Conversations.Admin.Web.Tests/Responsive/ResponsiveEvidenceHarnessTest.cs`
- `tests/Hexalith.Conversations.Admin.Web.Tests/Rendering/InvestigationWorkspaceRendererTest.cs`
- `tests/Hexalith.Conversations.Admin.Web.Tests/Fixtures/AdminWebHostFixture.cs`
- `tests/Hexalith.Conversations.Admin.Web.Tests/Fixtures/PlaywrightFixture.cs`
- `tests/Hexalith.Conversations.Admin.Web.Tests/Fixtures/RenderedWorkspaceCollection.cs`
- `tests/Hexalith.Conversations.Admin.Web.Tests/Support/RepositoryPaths.cs`
- `src/Hexalith.Conversations.Testing/Fixtures/BuyerAcceptanceDemoFixtures.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs`
- `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs`
- `tests/README.md`
- `tests/install-playwright.ps1`
- `_bmad-output/implementation-artifacts/3-8a-verify-responsive-layout-and-mobile-safe-triage.md` (the just-completed previous story)
- `_bmad-output/implementation-artifacts/3-8c-verify-leakage-clipboard-browser-and-telemetry-disclosure-safety.md` (sibling boundary)

### Likely File Changes

Expected modifications (host now exists, so most work is edits, not new projects):

- `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceRenderer.cs` (accessibility semantics: headings, landmarks, live regions, accessible descriptions, focus order)
- `src/Hexalith.Conversations.Admin.Web/Rendering/BuyerAcceptanceInvestigationWorkspaceCatalog.cs` and/or `InvestigationWorkspaceViewModel.cs` only if a safe accessibility field is genuinely missing (for example a stale-freshness fixture)
- `tests/Hexalith.Conversations.Admin.Web.Tests/Rendering/InvestigationWorkspaceRendererTest.cs` (if the fixture list changes, keep the ordered assertion correct)

Expected new files:

- `tests/Hexalith.Conversations.Admin.Web.Tests/Accessibility/AccessibilityEvidenceHarnessTest.cs` (mirrors the responsive harness)
- Possibly `tests/Hexalith.Conversations.Admin.Web.Tests/Accessibility/*` supporting records/helpers
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/**` (aria/accessibility-tree snapshot, focus-order trace, accessible-name sentinel scan, fixture matrix, evidence-summary.md, manual screen-reader notes)

Expected updates:

- `tests/README.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Release evidence or conformance manifest entries only after evidence exists and only with care for existing unrelated local edits

### Previous Story Intelligence (Story 3.8A, status `review`)

- 3.8A deliberately built **the narrowest first-party rendered host** instead of a full FrontComposer/Blazor/Fluent UI product surface. View models are internal to `Hexalith.Conversations.Admin.Web`, so no new public UI contracts and no FrontComposer contract tests were needed. 3.8B should keep that posture: extend the internal renderer, do not introduce public UI contracts unless an ADR approves it. [Source: `_bmad-output/implementation-artifacts/3-8a-verify-responsive-layout-and-mobile-safe-triage.md#Completion Notes List`]
- 3.8A reused `BuyerAcceptanceDemoFixtures` and existing projection/query DTOs rather than creating a parallel demo/transcript model. Do the same. [Source: same file, Implementation Plan/Completion Notes]
- 3.8A proved the browser lane works end to end: `dotnet test tests/Hexalith.Conversations.Admin.Web.Tests/...` (4 passed), full `dotnet test Hexalith.Conversations.slnx` (1519 passed), and `dotnet build ... --configuration Release` (0 warnings/errors). Keep warnings-as-errors clean and do not regress the suite. [Source: same file, Completion Notes List]
- 3.8A is in `review` and has no Senior Developer Review section yet. If review produces fixes to the renderer or harness before 3.8B merges, rebase 3.8B accessibility changes onto those fixes. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/implementation-artifacts/3-8a-...md`]
- Story 3.7 review fixes remain relevant: verification evidence must match scenario tenant/conversation scope; cross-tenant denial must use a genuinely different tenant; missing caller authority must fail closed; temporal cursors use the canonical composite shape; scenario steps cannot reference undeclared fixture kinds. [Source: `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md#Senior Developer Review (AI)`]

### Latest Technical Information

- The host uses no UI component framework. Earlier drafts referenced Fluent UI Blazor + FrontComposer; that path was **not** taken by 3.8A. Do not add Fluent UI or FrontComposer to satisfy this story; accessibility is achieved by improving the hand-rendered HTML. If a component framework is ever introduced it requires an ADR and central package alignment with the local FrontComposer dependency decision, not an inline package add. [Source: `src/Hexalith.Conversations.Admin.Web/Hexalith.Conversations.Admin.Web.csproj`; `Hexalith.FrontComposer/_bmad-output/project-context.md#Technology Stack & Versions`]
- Playwright for .NET (`Microsoft.Playwright`, already referenced) supports isolated browser contexts with `ForcedColors`, `ReducedMotion`, viewport, traces, and screenshots (used by the 3.8A harness). For the accessibility tree, prefer `Locator.AriaSnapshotAsync()` and role-based locators (`Page.GetByRole`, `GetByLabel`); the older `Accessibility.SnapshotAsync` is legacy/deprecated. [Source: `tests/.../ResponsiveEvidenceHarnessTest.cs`; Context7 `/microsoft/playwright-dotnet` docs]
- Microsoft accessibility guidance favors automation-first accessibility checks, keyboard validation, high-contrast verification, and manual screen-reader validation for high-risk scenarios — exactly the layered evidence this story requires. [Source: Microsoft Learn accessibility testing guidance]

### Testing Requirements

Minimum focused validation after implementation:

- `dotnet test tests/Hexalith.Conversations.Admin.Web.Tests/Hexalith.Conversations.Admin.Web.Tests.csproj` (responsive lane stays green; new accessibility lane passes) — requires `pwsh tests/install-playwright.ps1` once per machine
- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationTestIds"`
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest"`
- `dotnet test Hexalith.Conversations.slnx`
- `dotnet build Hexalith.Conversations.slnx --configuration Release` (0 warnings, 0 errors)

Evidence validation must include:

- Accessibility-tree / ARIA snapshot results per fixture.
- Heading-outline and landmark assertions.
- Focus-order trace for Find -> Read -> Trust (scope/identity/trust/completeness/command-gate before timeline reliance).
- Accessible-name/description forbidden-sentinel scan (covers `aria-label`, resolved `aria-describedby`, `title`, headings, table/row summaries, live regions).
- Manual keyboard-only walkthrough notes.
- Manual screen-reader transcript or notes (Windows Narrator/NVDA preferred).
- High contrast, reduced motion, and 200 percent browser zoom coverage.
- Safe announcement assertions for redaction, stale/freshness, denied, unavailable, unresolved participant, blocked command, and permission downgrade states.

### Out of Scope

- Story 3.8A: responsive layout matrix, mobile safe-triage verification, responsive duplicate scans, and viewport-specific telemetry labels beyond what is needed to support accessibility evidence (the 3.8A lane already covers these and must remain green).
- Story 3.8C: full Leak Sentinel suite, clipboard payload checks, browser-title checks, tooltip checks, screenshot disclosure suite, and full telemetry disclosure scan beyond accessibility-specific leakage checks.
- Full Generate Evidence Bundle workflow, release signing, named waiver runtime approval workflow, durable evidence store, legal hold automation, retention editor, global admin search, cross-tenant operator browsing, transcript tables, Memories/RAG indexes, browser/local storage for protected data, and raw EventStore stream browsing.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety`
- `_bmad-output/planning-artifacts/prd.md#FR56-FR69`
- `_bmad-output/planning-artifacts/prd.md#NFR69`
- `_bmad-output/planning-artifacts/prd.md#NFR77`
- `_bmad-output/planning-artifacts/architecture.md#Disclosure Surface Inventory`
- `_bmad-output/planning-artifacts/architecture.md#UX Trust Contract`
- `_bmad-output/planning-artifacts/architecture.md#Architecture Verification Strategy`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Requirements`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Disclosure Boundary`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-24.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/3-8a-verify-responsive-layout-and-mobile-safe-triage.md`
- `_bmad-output/implementation-artifacts/3-7-provide-self-serve-buyer-acceptance-demo.md`
- `_bmad-output/implementation-artifacts/epic-3-retro-2026-05-24.md`
- `_bmad-output/project-context.md`
- `tests/README.md`
- Context7: `/microsoft/playwright-dotnet`
- Microsoft Learn: ASP.NET Core accessibility testing, ARIA roles and accessible names

## Dev Agent Record

### Agent Model Used

claude-opus-4-7[1m] (Claude Code dev-story workflow).

### Debug Log References

- Preflight: `3-8a` is now `done` in `sprint-status.yaml` (the story draft assumed `review`); the rendered host and Playwright lane already exist and the 3.8A baseline ran green (6/6) before any change. Playwright Chromium confirmed cached under `%LOCALAPPDATA%/ms-playwright`, so the story is not blocked. The catalog already contains `TenantA_Stale_RebuildingProjection` (the stale scenario the draft said might need adding), so no new fixture was required and `InvestigationWorkspaceRendererTest.CatalogShouldExposeRequiredResponsiveFixtures` did not need editing.
- Red/green: added 7 renderer accessibility unit tests, confirmed 5 failed against the 3.8A renderer (single h1, heading outline, skip link/landmarks, live region, aria-describedby) and 2 passed as pre-existing invariants, then implemented the renderer changes to green (12/12 renderer unit tests).
- Build-safety: removed a `<see cref>` to the not-yet-created harness type and an unused const to keep warnings-as-errors clean.

### Completion Notes List

- Extended `InvestigationWorkspaceRenderer` with accessibility semantics only — no new disclosure. Every rendered string still comes from the already-authorized/redacted `InvestigationWorkspaceViewModel`; no view-model or catalog field was added.
- Heading outline fixed: one document `<h1>` (banner), section `<h2>` for Trust order and Evidence timeline, `<h3>` for the five trust panels and timeline rows — replacing the previous duplicated `<h1>`/`<h2>`. Screen-reader heading navigation now follows the trust order.
- Added a `#governed-record` skip link, a banner landmark, and a `role="search"` find landmark; `<main>` is the skip target (`tabindex="-1"`, `aria-labelledby="workspace-title"`).
- Added a safe `aria-live="polite"` `role="status"` region announcing the safe trust/completeness/command classes from the safe labels (identical for both hidden-read fixtures).
- Disabled/blocked commands now expose the safe blocked reason through `aria-describedby` to a visible `command-reason` span, while preserving the 3.8A `disabled aria-disabled="true"` contract and the `data-blocked-reason` attribute.
- Redundant responsive duplicate surfaces are marked `aria-hidden="true"` so assistive technology hears the trust posture once; they remain visible and in the DOM for the 3.8A responsive harness (verified green).
- Added a `:focus-visible` outline that switches to system `Highlight` under `forced-colors: active`; preserved the existing forced-colors/reduced-motion CSS blocks.
- New `AccessibilityEvidenceHarnessTest` drives headless Chromium over 15 rows (12 fixtures at desktop + 1 forced-colors/reduced-motion + 2 at 200% zoom), capturing the accessibility tree (`AriaSnapshotAsync`), heading outline, landmark roles, keyboard focus-order trace, and resolved accessible-name surface, with a forbidden-sentinel scan over all of them.
- Indistinguishability: `HiddenReadRendersIdenticallyForUnauthorizedAndNonexistentRecords` proves the renderer emits byte-identical HTML for an unauthorized-existing and a nonexistent hidden read; the browser lane confirms the cross-tenant and unauthorized-existing fixtures show only the canonical denial with no evidence rows and no sentinel.
- Screen-reader evidence note (transparency): the accessibility tree captured via `AriaSnapshotAsync` is the programmatic surface Narrator/NVDA render to speech, used here as the assistive-technology inspection with the reason documented per the task ("if another assistive technology is used, document the reason"). The human audible Narrator/NVDA confirmation cannot be performed by an automated agent and is provided as an explicit checklist in `manual-keyboard-screen-reader-notes.md` for the accessibility evidence owner (review date 2026-05-31 or before merge).
- Conformance manifest intentionally not touched: per the task it is only updated if evidence should replace the superseded waiver reference, and it already carries unrelated local edits in this worktree; the 3.8B evidence bundle is self-contained under `evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/`.
- Validation: Admin.Web lane 14/14; full `dotnet test Hexalith.Conversations.slnx` 1529 passed / 0 failed / 0 skipped (up from 1519); `dotnet build ... --configuration Release` 0 warnings / 0 errors. 3.8A responsive lane and 3.8C scope kept separate.

### File List

- `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceRenderer.cs` (modified — accessibility semantics: banner h1, search landmark, skip link, h2/h3 outline, aria-live region, aria-describedby blocked reasons, aria-hidden duplicates, focus-visible/forced-colors styling)
- `tests/Hexalith.Conversations.Admin.Web.Tests/Rendering/InvestigationWorkspaceAccessibilityTest.cs` (new — 7 renderer accessibility unit tests)
- `tests/Hexalith.Conversations.Admin.Web.Tests/Accessibility/AccessibilityEvidenceHarnessTest.cs` (new — Playwright accessibility evidence harness)
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/accessibility-matrix.json` (new — generated)
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/aria-snapshots.json` (new — generated)
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/focus-order-trace.json` (new — generated)
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/accessible-name-scan.json` (new — generated)
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/fixture-matrix.json` (new — generated)
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/evidence-summary.md` (new — generated)
- `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/manual-keyboard-screen-reader-notes.md` (new — authored)
- `tests/README.md` (modified — accessibility lane documentation)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified — Story 3.8B section)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — 3-8b status transitions)
- `_bmad-output/implementation-artifacts/3-8b-verify-accessibility-tree-keyboard-and-screen-reader-safety.md` (modified — this story file)

## Story Context Validation

- Checklist reviewed: `.claude/skills/bmad-create-story/checklist.md`.
- Input discovery completed:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, Story 3.8B (lines 1558-1590) and the 3.8A/3.8C split boundaries.
  - Loaded `{prd_content}` requirement references FR56-FR69 and NFR69-NFR77.
  - Loaded `{architecture_content}` disclosure-surface and UX-trust-contract guardrails.
  - Loaded `{ux_content}` accessibility requirements, disclosure boundary, and UX-DR46/UX-DR48.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`.
  - **Read the actual Story 3.8A implementation** (`src/Hexalith.Conversations.Admin.Web/**` and `tests/Hexalith.Conversations.Admin.Web.Tests/**`) to replace the now-stale "no rendered host exists" assumption with the real host, renderer, catalog, and Playwright test patterns, and to identify concrete accessibility gaps to fix.
  - Loaded previous Story 3.8A (status `review`) and Story 3.7 review learnings; loaded the 2026-05-24 reopen/assignment plan.
- Checklist fixes applied:
  - Story now extends the existing rendered host instead of asking the dev to create one, and forbids DTO-only completion.
  - Story names owner, fixture set, evidence output, pass/fail gate, and review date.
  - Story separates 3.8B from independently reopened 3.8A/3.8C evidence domains and protects the 3.8A baseline.
  - Story identifies the exact existing files to read, the concrete renderer accessibility gaps to close, and the likely edited/new locations.
  - Story adds stop conditions for missing browser runtime, unsafe accessible text, hidden authorization, UI-owned trust state, mobile mutation, and raw EventStore browsing.
- Validation result: ready-for-dev.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Change Log

- 2026-05-24: Reopened Story 3.8B per Jerome's instruction, marked the rendered-UI waiver superseded for the full 3.8 split, and created the initial ready-for-dev story context.
- 2026-05-24: Regenerated the story after Story 3.8A implemented the rendered `Hexalith.Conversations.Admin.Web` host and `Hexalith.Conversations.Admin.Web.Tests` Playwright lane. Replaced the "no UI host" assumption with the real host/renderer/catalog/test patterns, documented the renderer's accessibility gaps to close, and retargeted tasks to extend the existing surface.
- 2026-05-24: Implemented accessibility semantics in `InvestigationWorkspaceRenderer` (single-h1 heading outline, banner/search/main landmarks + skip link, safe `aria-live` region, `aria-describedby` blocked-command reasons, `aria-hidden` responsive duplicates, forced-colors focus-visible styling) with no new disclosure. Added 7 renderer accessibility unit tests and a Playwright `AccessibilityEvidenceHarnessTest` (accessibility tree, heading outline, landmarks, keyboard focus order, accessible-name sentinel scan, high-contrast/reduced-motion/200%-zoom) generating the 3.8B evidence bundle and manual keyboard/screen-reader notes. Admin.Web lane 14/14; full suite 1529 passed/0 failed; Release build 0 warnings. Status → review.
