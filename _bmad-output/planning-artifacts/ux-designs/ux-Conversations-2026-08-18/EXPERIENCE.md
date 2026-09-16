---
name: Hexalith.Conversations
status: draft
currentDisposition: preserved-not-activated
activationAuthority: separate-approved-release-authority-required
updated: 2026-09-16
sources:
  - _bmad-output/planning-artifacts/ux-requirement-map.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/ux-design-directions.html
  - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md
  - _bmad-output/planning-artifacts/architecture.md
  - references/Hexalith.AI.Tools/hexalith-ux-instructions.md
---

# Hexalith.Conversations — Experience Spine

> Draft preservation contract. It does not activate product UI implementation, lift any hold, or select a release slice. `DESIGN.md` owns visual identity. These spines win over the illustrative HTML on conflict; the UX requirement map controls disposition, the PRD controls preserved product obligations, and final Architecture V15 controls the public state mappings below.

## Foundation

The primary product surface is a desktop-first, responsive operator/admin web experience composed through FrontComposer and Blazor Fluent UI V5. Standard shell, navigation, forms, lists, grids, dialogs, drawers, accordions, focus behavior, theme roles, typography, and density inherit from that system. This spine specifies only Conversations-specific behavioral deltas.

The defining experience is **Find → Open → Verify → Cite, Act, or Stop**. The operator opens a governed case file—not a chat transcript—and leaves with an answer, a source-owned confidence state, and a safe next action. Adopter applications own business-user continuity; typed contracts, clients, diagnostics, and conformance evidence own the developer experience.

Every trust-bearing value is supplied by Conversations-owned projections, public contracts, or command-availability metadata. The client formats state but never infers permission, freshness, completeness, redaction, audit readiness, participant identity, citation confidence, or action eligibility.

Current status is `preserved-not-activated`. Architecture V15 is final but keeps the implementation hold active and authorizes no UX scope.

## Information Architecture

| Surface | Reached from | Purpose | Journey closure |
|---|---|---|---|
| Investigation Workspace | FrontComposer tenant-scoped navigation | Keep safe discovery and governed reading in one operator workspace. | Sarah |
| Governed Record View | Select `Trust Preview Result Row` in `Tenant-scoped Find Pane` | Establish identity, trust, evidence completeness, timeline, citation, and safe action. | Sarah; Daniel |
| Evidence Detail View | Authorized `Citation Control`, trust detail, participant detail, or why-this-result action | Show one independently authorized evidence detail without losing record context. | Sarah; Helen |
| Forensic Review | Authorized switch from Governed Record View | Show exact event-level evidence for audit/security review. | Helen; Daniel |
| Evidence Acceptance Review | Acceptance/checklist entry point | Summarize conformance outcome, evidence, waiver, signer, scope, and downgrade state. | Julian; Helen |
| Operational Degradation Review | Machine-readable verification or incident workflow | Show audit-sink/projection failure class and preserve fail-closed governance boundaries. | Marcus |
| Adopter Continuity Surface | Adopter application requests a stable conversation or business context | Resume durable context without exposing storage/governance machinery. | Maya; Atlas |
| Developer Integration Surface | Contract package, client, quickstart, diagnostics, and conformance suite | Create, append, and read through typed contracts without EventStore leakage. | Diego |
| Stable-reference Boundary | Read-time hydration from upstream-owned identities | Preserve stable attribution while upstream modules own lifecycle state. | Naomi |
| Mobile Triage and Handoff | Responsive Investigation Workspace | Read authorized summary, inspect trust, copy an allowed citation, and hand off safely. | Sarah |

Desktop uses the Split Investigation Lens: `Tenant-scoped Find Pane` at left and Governed Record View at right. On narrow surfaces, discovery moves to an inherited drawer, while tenant scope, record identity, `Trust Posture Strip`, `Evidence Completeness Indicator`, and command eligibility remain before timeline reliance.

Any page-like surface, dialog, or detail panel with two or more sibling titled content sections uses one inherited Fluent accordion, one item per section. Titles, breadcrumbs, command bars, navigation chrome, and a single primary content region remain outside; the primary item is expanded by default when grouped.

## Voice and Tone

Microcopy is calm, exact, non-blaming, and permission-safe. Brand posture lives in `DESIGN.md`.

| Use | Meaning | Avoid |
|---|---|---|
| “Redacted” | The viewer may know content exists but may not see it. | Naming or hinting at the protected value. |
| “Unavailable” | The system cannot confirm whether content exists or cannot supply authorized evidence. | Collapsing unavailable into missing or forbidden. |
| “Restricted” | Access is denied by policy. | Raw policy names or protected entity hints. |
| “Still loading” | Trust metadata or content is pending. | An optimistic current/complete placeholder. |
| “Some events unavailable” | Evidence is incomplete within an authorized scope. | Claiming the record is complete. |
| “No accessible records match this query” | Permission-safe search result. | Confirming inaccessible records exist. |

State-change copy names the safe state class and next action, not protected detail. Diagnostics use business-safe language first; infrastructure terms remain in authorized detail. Localization is not currently committed: activation must either declare English-only operation or approve reason-code-to-resource-key behavior without weakening the distinctions above.

## Component Patterns

Behavioral contract. Visual anatomy lives in `DESIGN.md.Components`.

| Component | Use | Behavioral rules |
|---|---|---|
| Trust Fact | Results, headers, timelines, details, acceptance | Render source, timestamp, scope, confidence/status, and citation/audit reference when supplied. No source means no trust claim. |
| SafeReasonInline | Blocked, degraded, denied, or unavailable decision point | Show a short permission-safe reason without hover; never include protected metadata or hidden entity hints. |
| SafeReasonDetail | Authorized explanation from a summary | Reauthorize independently before rendering; denial yields only a safe unavailable state. |
| Redaction Placeholder | Timeline/detail content replaced by policy | Receive no raw protected content; remain absent from hidden DOM, accessible names, tooltip, copy, title, telemetry, and responsive duplicates. |
| Freshness Marker | Results, headers, timelines, details | Bind to `ProjectionFreshnessV1`; show source, timestamps, cursor/version, state, and reason without deriving freshness locally. |
| Command Availability Marker | Near governed actions | Bind to `ConversationCommandAvailabilityV1`; missing or contradictory metadata is unavailable and cannot enable an action. |
| Citation Control | Timeline evidence and acceptance evidence | Copy only authorized citation DTO content; recheck authorization; missing citation blocks evidence acceptance. |
| Participant Identity Marker | Results, header, timeline, detail | Bind to authorized attribution and resolution state; never merge identities client-side or expose unauthorized Parties data. |
| Tenant-scoped Find Pane | Investigation Workspace | Search and filter within tenant/permission scope; counts, facets, autocomplete, ordering, pagination, empty states, and material timing remain disclosure-safe. |
| Trust Preview Result Row | Tenant-scoped Find Pane | Preview source-owned trust before selection; keyboard-selectable; why-this-result detail remains independently authorized. |
| Governed Record Header | Governed Record View | Announce record and scope before evidence; keep temporal cursor, freshness, and action eligibility visible; deny without protected identity. |
| Trust Posture Strip | Governed Record View | Roll up freshness, completeness, citation, participant, audit, verification, and command state using conservative precedence. |
| Evidence Completeness Indicator | Immediately before timeline | State the scope of completeness; distinguish complete-within-permissions/index, incomplete-withheld, and unknown-metadata. |
| Evidence Timeline Entry | Governed Record View and Forensic Review | Preserve semantic chronology and keyboard access to citation/audit/redaction actions; never resemble chat bubbles. |
| Safe State Message | Empty, loading, denied, unavailable, stale, rebuilding, redacted, degraded | State what is known, confidence, and next safe action without confirming protected existence. |
| Evidence Detail Drawer | Evidence Detail View | Authorize on open, use generic pending framing, close on permission downgrade, return focus safely, and never flash protected content. |
| Command Gate | Governed Record View and governance forms | Render only server-owned availability; recheck immediately before execution; missing/stale/contradictory metadata fails closed. The accessible blocked-control mechanism remains open. |
| Permission-gated Forensic Timeline Mode | Forensic Review | Require separate permission and audit logging; exact identifiers/positions appear only when authorized; degradation remains explicit. |
| Evidence Acceptance Summary | Evidence Acceptance Review | Separate module evidence from inherited platform controls; expose pass/fail/waiver/synthetic status, signer, time, scope, and linked evidence. |
| Waiver and Blocker Summary | Evidence Acceptance Review | Expose owner, risk, expiry, compensating control, review date, and downgrade trigger; expired or partial acceptance never appears complete. |

## State Patterns

### Source-owned public state mapping

Architecture V15 and the public contract define these mappings. A client must not synthesize alternatives.

| Public state / reason | Source condition | UX consequence | True empty allowed? |
|---|---|---|---|
| `Current` / `current` | V2 `Ready`, zero summaries, no pending dispatch | May render a true empty page when the authorized query also has no rows. | Yes, only here. |
| `Stale` / `stale_threshold_exceeded` | Public freshness exceeds the accepted threshold | Show stale evidence and block trust-dependent actions according to source-owned command metadata. | No. |
| `Rebuilding` / `rebuilding` | V1, `Erasing`, `Erased`, or `Rebuilding` lifecycle | Show rebuilding as a non-empty status; never collapse it to “no results.” | No. |
| `Rebuilding` / `gap_detected` | Lifecycle or source-position gap | Show a non-empty gap/rebuild status and fail closed. | No. |
| `Unavailable` / `unavailable` | Missing record for an authorized tenant or unavailable provider | Show content-safe unavailable state and fail closed. | No. |
| `Unavailable` / `metadata_contradictory` | Corrupt, mixed-generation, contradictory lifecycle, identity reuse, or invalid time evidence | Show content-safe unavailable/contradictory state and fail closed. | No. |
| `Forbidden` / `forbidden` | Nonexistent or unauthorized tenant at authorization boundary | Return a non-disclosing state; do not read the index or distinguish nonexistence from denial. | No. |
| `Redacted` / `redacted` | Policy-redacted authorized content | Show `Redaction Placeholder`; never receive or retain the original value. | No. |

Other public reason codes (`out_of_order_event`, `mixed_generation`, `poison_event`, `metadata_write_failed`) require a source-owned state/reason combination. Their exact display mapping is open; an unmapped or contradictory combination fails closed rather than being interpreted client-side.

`ProjectionFreshnessV1.LastAppliedEventTimestamp` is the applicable replay/event-time anchor. `ProjectionFreshnessV1.ProjectionGeneratedAt` is the actual UTC instant generation completed. `LagDuration` is the interval between them. The UI labels these meanings distinctly and never substitutes processing/query time.

### Surface-state closure

| Surface | Required states |
|---|---|
| Investigation Workspace | Cold load without disclosure; scoped search; no accessible matches; current; stale; rebuilding; unavailable; forbidden/non-disclosing; permission downgrade; content-safe error. |
| Governed Record View | Trust metadata pending; current; stale; rebuilding; incomplete/unknown; redacted; participant unresolved; citation unavailable; audit unavailable; permission downgrade; content-safe error. |
| Evidence Detail View | Pending independent authorization; authorized; blocked; stale; unavailable; audit-unavailable; downgrade-and-close; focus return. |
| Forensic Review | Unavailable; authorized; partially hidden; stale; audit-required; permission downgrade. |
| Evidence Acceptance Review | Loading; accepted; blocked; waived; expired waiver; incomplete; stale; synthetic sample; evidence unavailable. |
| Operational Degradation Review | Audit unavailable; projection delayed/blocked; verification pass/fail; governance action blocked; eligible non-governance continuation where source-authorized. |
| Adopter Continuity Surface | Authorized load; provider-session expired; degraded participant/attachment hydration; stale/unavailable projection; typed denied or content-safe failure. |
| Developer Integration Surface | Package/contract discovery; supported/unsupported version; tenant binding failure; stale projection; denied access; conformance pass/fail; typed remediation. |
| Stable-reference Boundary | Resolved current reference; unresolved/deleted upstream entity; permission-filtered hydration; source-owned degraded/unavailable state. |
| Mobile Triage and Handoff | Authorized summary; trust pending; read-only; blocked/absent mutation; safe citation; handoff available/unavailable; no hidden detail DOM. |
| Global shell | Offline/network loss and authentication/session expiry behavior remain open decisions; no optimistic trust or retained protected content is authorized. |

## Interaction Primitives

- **Search and filter:** tenant-scoped, permission-filtered, keyboard reachable. Counts, facets, ordering, pagination, autocomplete, timing, recent items, and empty copy must not expose inaccessible records.
- **Select and inspect:** selecting `Trust Preview Result Row` opens the governed record without losing search scope on wide surfaces. Focus lands on a safe record summary, not sensitive content.
- **Timeline navigation:** chronological and keyboard navigable. Virtualization preserves order, position context, focus restoration, redaction semantics, and excludes protected offscreen DOM.
- **Temporal reconstruction:** source-owned cursor changes reconstruct the governed record. Keyboard input, busy/change announcement, and post-reconstruction focus are open activation decisions; no local reconstruction inference is allowed.
- **Detail drill-down:** `Evidence Detail Drawer` authorizes independently on every open and closes on permission downgrade.
- **Citation copy:** construct output from an authorized citation/export DTO after recheck, never from arbitrary rendered text or a full component model.
- **Governed commands:** server metadata controls visibility/availability and every execution receives a fresh server recheck. A visible control is not proof of permission.
- **Blocked action explanation:** the outcome is fixed—reason visible without hover and available to keyboard/screen-reader users—but the exact focusable-control and programmatic-association mechanism is an open decision.
- **Forms:** collect operator intent only. Tenant, user, claims, tokens, and host authorization context are never editable. Local validation, server validation, and pre-execution recheck remain separate.
- **Dialogs and accordions:** dialogs are reserved for governance-changing confirmation. Multiple titled sibling sections use one inherited Fluent accordion; do not hide the sole primary content region.
- **Trust transitions:** downgrade visibly, close gated details, clear protected content, preserve only safe operator-entered intent, and never announce protected detail.

## Accessibility Floor

The current preserved baseline is **WCAG 2.1 AA** for operator/admin web surfaces. WCAG 2.2 promotion is an open activation-time decision.

- Keyboard-only and screen-reader users receive the same tenant scope, trust posture, redaction state, evidence completeness, blocked-action reason, and next safe action as pointer users.
- Focus order follows investigation order: scope, trust posture, completeness, timeline, evidence controls, command state, then diagnostics.
- Evidence details receive focus only after authorization and return focus safely when closed.
- Color is never the only carrier of current, stale, rebuilding, unavailable, forbidden, redacted, degraded, incomplete, or blocked state.
- Assistive output—including names, descriptions, live regions, headings, summaries, title, copy, and hidden helper text—obeys the same disclosure boundary as visible content.
- Reduced motion, high contrast, browser zoom, and narrow layouts preserve trust order and blocked reasons.
- Touch targets meet at least 44×44 px where touch operation is supported.
- Leak Sentinel coverage spans visible DOM, hidden DOM, accessibility tree, tooltip, URL/query state, browser title, clipboard, telemetry, screenshot, loading placeholder, and responsive duplicate surfaces.

Open accessibility decisions: the blocked-control mechanism; live-region transition matrix and urgency; explicit 200% text-resize and 320 CSS-pixel/equivalent 400% reflow criteria; localization; temporal-cursor focus/busy behavior; and the browser/assistive-technology evidence matrix.

## Responsive & Platform

| Surface width | Preserved behavior |
|---|---|
| Mobile, 320–767 px | Read-only triage by default. Show scope, identity, trust, completeness, authorized evidence, safe citation, and handoff. Mutations are absent or blocked unless separately designed, authorized, and tested. |
| Tablet, 768–1023 px | Constrained review. Preserve trust summary and timeline; filters and secondary detail move into inherited drawers/accordion regions. |
| Desktop, 1024 px+ | Full Split Investigation Lens for investigation and role-gated approval. |
| Wide desktop, 1440 px+ | Same authority and information order; extra width must not become portal sprawl or reveal more data. |

Every breakpoint is an independent disclosure surface. CSS hiding is never authorization. Layout may change density, navigation, and source-owned action availability; it must not change DTO shape, redaction policy, trust precedence, metadata order, or disclosure boundaries.

## Inspiration & Anti-patterns

- **Lifted from GitHub checks:** compact source-backed trust rollups, gated actions, safe reasons, and detail on demand—not developer CI vocabulary.
- **Lifted from Azure Portal:** scoped resource context, layered investigation, activity evidence, and progressive drill-down—not navigation sprawl.
- **Lifted from audit/incident review:** chain of custody, evidence packet, redaction notice, access decision, and explicit disposition.
- **Rejected:** chat transcript styling, avatars as primary anchors, raw event-stream browsing, global search that leaks existence, ambiguous green states, color-only status, UI-only governance, decorative gradients, AI glow, and playful conversation chrome.

## Key Flows

The flows below are **preserved product journeys**. They do not activate UI scope or settle open release slicing.

### Flow 1 — Governed investigation (Sarah, compliance operator, reviewing a disputed exchange)

1. Sarah enters the Investigation Workspace with the active tenant and permission scope visible.
2. She searches by an authorized external identifier, date, or business context.
3. `Tenant-scoped Find Pane` returns only accessible results through `Trust Preview Result Row`.
4. She selects a candidate and opens Governed Record View.
5. `Governed Record Header`, `Trust Posture Strip`, and `Evidence Completeness Indicator` establish scope and reliance before evidence.
6. She reviews `Evidence Timeline Entry` items, authorized redaction, citations, audit linkage, and temporal state.
7. **Climax:** Sarah copies a citation-ready reference or reaches an explicit safe stop with the trust state and next action understood.

Failure: no accessible result, forbidden scope, stale/rebuilding/unavailable evidence, missing citation, or incomplete evidence produces `Safe State Message` and no optimistic reliance or governance mutation.

### Flow 2 — Durable continuity (Maya, business user, and Atlas, AI agent, resuming after provider-session loss)

1. Maya returns through an adopter application using stable conversation or business context.
2. The adopter requests the record within tenant and permission scope.
3. Conversations returns ordered context, current attribution, attachments/references, and source-owned trust state.
4. Atlas receives durable context without treating provider correlation IDs as authority.
5. **Climax:** Maya and Atlas resume the work without rebuilding context or learning the persistence substrate.

Failure: denied, stale, unavailable, or degraded hydration returns a typed, content-safe state; the adopter does not infer or expose missing context.

### Flow 3 — Typed adopter integration (Diego, adopter developer, proving the minimal path)

1. Diego installs the published contract/client package and opens the supported quickstart.
2. He creates a conversation through typed contracts.
3. He appends a participant/message and attaches a stable business or file reference.
4. He reads the timeline projection and source-owned trust metadata.
5. He runs adopter-facing conformance tests.
6. **Climax:** the supported create → append → read path passes without exposing EventStore envelopes, raw streams, or client-inferred trust.

Failure: unsupported contract, missing tenant context, stale projection, denied access, or failed verification returns a safe typed code and remediation guidance.

### Flow 4 — Evidence-based acceptance (Julian, platform owner, and Helen, security reviewer, evaluating release evidence)

1. Julian opens Evidence Acceptance Review for a declared scope.
2. Helen runs the seeded/adversarial checks for tenant isolation, stale/missing projection, audit pairing, redaction replay, and release gates.
3. They inspect signed artifacts, versioned manifests, scope, signer, time, and inherited-platform boundaries.
4. `Evidence Acceptance Summary` and `Waiver and Blocker Summary` expose partial, waived, stale, or blocked evidence without hiding it.
5. **Climax:** Julian records an explicit accept, reject, waiver, or blocker outcome linked to the authoritative evidence.

Failure: missing, stale, unsigned, synthetic-only, or contradictory evidence remains non-accepted and names its owner/revisit condition without lifting a hold.

### Flow 5 — Audit-sink degradation (Marcus, SRE, diagnosing an incident)

1. Marcus receives a source-owned audit-unavailable or verification-failure state.
2. Governance-changing work remains fail-closed; only independently eligible non-governance work may continue.
3. He consumes machine-readable verification and content-safe failure classification.
4. Any privileged tenant-touching action carries structured justification and must be recorded for the affected tenant.
5. **Climax:** Marcus can identify the failure class, allowed boundary, and recorded next action without exposing tenant content.

Failure: if audit evidence cannot support the governance action, the action remains blocked. This flow does not invent a governance mutation UI.

### Flow 6 — Stable-reference continuity (Naomi, cross-product owner, reviewing an upstream lifecycle change)

1. Naomi changes or retires an upstream-owned Party, Project, Folder, or file entity through its owning module.
2. Conversations retains the stable identifier in the durable record.
3. Read-time hydration asks the owning module for the current authorized representation.
4. The UX receives only the source-owned resolved, degraded, permission-filtered, or unavailable state.
5. **Climax:** the governed record preserves attribution without Conversations taking ownership of upstream lifecycle orchestration.

Failure: unresolved or inaccessible upstream data remains degraded/unavailable; the client never merges identities or guesses lifecycle state.

### Flow 7 — Post-harm testimony (Daniel, operations leader, reconstructing what happened)

1. Daniel enters an authorized Governed Record View after a harmful outcome.
2. He reviews the immutable, time-ordered, attributed evidence and governance state.
3. He verifies freshness, redaction, completeness, audit linkage, and temporal position before relying on the record.
4. He follows authorized detail into Forensic Review where exact evidence is permitted.
5. **Climax:** Daniel obtains provable testimony about what happened and what governance state applied at the relevant time.

Failure: incomplete, contradictory, stale, unavailable, or restricted evidence leads to an explicit safe stop; Conversations does not claim harm prevention, AI-remediation, or automatic legal hold.

### Refactor/tooling journey disposition

These current-initiative actors are deliberately **not product UX protagonists**. They have no product surface or Key Flow in this spine.

| PRD journey | Disposition |
|---|---|
| UJ-1. Nadia retires hand-rolled plumbing from Conversations. | Refactor maintainer workflow; preserve behavior through engineering/conformance evidence, not product UI. |
| UJ-2. Sam promotes the tenant-access handler everyone copied. | Platform extraction workflow; no product screen implied. |
| UJ-3. Priya stands up a brand-new domain module on the thin template. | Developer/tooling proof for the refactor PRD; distinct from Diego’s preserved adopter-product journey. |

## Open Decisions

These are phase blockers for activation, not blanks to fill by implementation:

1. Approve exact Fluent 2 mappings or custom values for redaction, degraded, current, stale, unavailable, forbidden, and blocked visual roles, including light/dark/high-contrast evidence.
2. Decide whether and how to raise the accessibility floor from WCAG 2.1 AA to WCAG 2.2 AA.
3. Decide the Fluent UI V5 prerelease/stable dependency policy.
4. Choose and test the accessible blocked-action control/reason mechanism without changing the preserved outcome.
5. Complete the source-owned mapping for all public reason codes and presentation labels; the client must not infer it.
6. Decide the activation-time capability matrix for read-only, copy, export, retention, redaction, replay, restore, escalation, and other governance mutations.
7. Define global offline/network-loss and authentication/session-expiry behavior.
8. Decide English-only operation versus a governed localization/resource-key contract.
9. Specify live-region transition behavior, zoom/reflow measurements, temporal-cursor focus, and the browser/assistive-technology validation matrix.
10. Publish accepted source bindings/tombstones for the legacy UX provenance and current spine authority before treating these drafts as implementation contracts.

