---
name: Hexalith.Conversations Quiet Evidence UI
description: Visual contract for the preserved Conversations governed-record experience, inheriting FrontComposer and Blazor Fluent UI V5 without redefining their theme.
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
  - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md
  - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/validation-report.md
  - _bmad-output/planning-artifacts/architecture.md
  - references/Hexalith.AI.Tools/hexalith-ux-instructions.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/reconcile-prd.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/reconcile-architecture.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/reconcile-ux-validation.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/.memlog.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/TRACEABILITY.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/CONTRACT-BINDINGS.md
colors: {}
typography:
  interface:
    note: Inherit the Blazor Fluent UI V5 Fluent 2 type ramp and component parameters without override.
  evidence:
    note: Inherit the Fluent body role; optimize for sustained reading of evidence timelines.
  metadata:
    note: Inherit an available Fluent caption or metadata role; timestamps and trust metadata must remain legible.
  identifier:
    note: Use the inherited monospace role only for authorized identifiers, hashes, correlation IDs, immutable references, and citation blocks.
rounded: {}
spacing:
  compact: 4px
  base: 8px
  section: 16px
  region: 24px
components:
  Trust Fact:
    note: Inherit Fluent typography and status roles; render only the public fields and display roles approved in CONTRACT-BINDINGS.md.
  SafeReasonInline:
    note: Inherit Fluent text roles; keep the safe reason visible beside the affected decision without revealing protected detail.
  SafeReasonDetail:
    note: Inherit Fluent detail-surface styling; visually separate independently authorized detail from its parent summary.
  Redaction Placeholder:
    note: Replace protected content at the same reading level without exposing the original value or its length; exact trust-role color mapping is open.
  Freshness Marker:
    note: Pair public freshness state and reason with their exact cursor, position, timestamp, generation, and lag fields; never use color as the sole signal.
  Command Availability Marker:
    note: Pair the source-owned availability state with a safe reason; never make visual availability the authorization source.
  Citation Control:
    note: Keep the citation affordance adjacent to its evidence and visually subordinate to the evidence itself.
  Participant Identity Marker:
    note: Show only authorized public attribution plus ParticipantResolutionState; no hydration-source field is assumed.
  Tenant-scoped Find Pane:
    note: Use inherited search and list styling with persistent tenant scope and compact trust previews.
  Trust Preview Result Row:
    note: Preserve record identity as primary, trust facts as secondary, and selection as an inherited Fluent interaction state.
  Governed Record Header:
    note: Present record identity and tenant-safe context first, then temporal cursor, trust posture, and command eligibility.
  Trust Posture Strip:
    note: Present source-owned posture dimensions in fixed order with restrained inline treatments; do not compute an aggregate winner.
  Evidence Completeness Indicator:
    note: Place the source-owned EvidenceCompletenessState immediately before timeline reliance; do not invent additional serialized categories.
  Evidence Timeline Entry:
    note: Use chronological case-file structure rather than chat bubbles; keep actor, time, evidence state, citation, and audit linkage together.
  Safe State Message:
    note: Use a full-width governed message with calm language, visible state, and next safe action.
  Evidence Detail Drawer:
    note: Use inherited drawer styling; generic framing remains visible until independent authorization succeeds.
  Command Gate:
    note: Keep allowed or blocked actions at the decision point with a visible safe reason; exact blocked-control mechanics remain open.
  Permission-gated Forensic Timeline Mode:
    note: Visually mark the exact-evidence mode and keep it distinct from the default human-readable chronology.
  Evidence Acceptance Summary:
    note: Show outcome, scope, signer, timestamp, freshness, and evidence sufficiency as a sober review summary.
  Waiver and Blocker Summary:
    note: Give owner, risk, expiry, compensating control, and review date equal visual weight; do not hide partial acceptance.
---

# Hexalith.Conversations — Design Spine

> Draft preservation contract. This document does not activate product UI work. The V4 UX requirement map controls disposition; Architecture V15 controls normative target state and ownership; the legacy specification preserves UX detail; and the PRD package supplies preservation context only as qualified by its current validation and the reconciliation records. This spine wins over the illustrative design-directions HTML on visual conflict, but does not supersede an accepted source or prove runtime conformance. Separate approved release authority is required for activation.

Requirement closure is recorded in `TRACEABILITY.md`; public contract bindings and intentionally non-renderable gaps are recorded in `CONTRACT-BINDINGS.md`.

## Brand & Style

The visual posture is **Quiet Evidence UI**: a calm, dense, operational case file in which the governed record is the protagonist. The interface communicates confidence with humility. It makes uncertainty, redaction, degraded evidence, and unavailable actions visible without becoming alarmist or decorative.

FrontComposer and Blazor Fluent UI V5 own the ordinary administration grammar. This document defines only Conversations-specific deltas needed to interpret evidence, permission, provenance, freshness, redaction, participant identity, citation, and action safety. It does not introduce a parallel theme.

The chosen composition is **02. Split Investigation Lens**, strengthened by the Case File Console record header and Evidence Reader timeline. Safe discovery stays beside governed reading on wide surfaces; the record remains visually dominant over search, diagnostics, and application chrome.

## Colors

`colors` is intentionally empty. Current authority preserves semantic visual roles but does not approve custom hex values or exact Fluent 2 role mappings. Implementations must inherit supported Blazor Fluent UI V5 component parameters and Fluent 2 color roles until Product/UX and the platform UI owner approve any delta.

| Visual role | Preserved use | Constraint |
|---|---|---|
| Primary/action | Navigation focus, selection, and currently allowed primary action | Inherit Fluent UI V5; never imply authorization from color. |
| Neutral | Chrome, case-file structure, evidence content, tables, and supporting metadata | Record content remains visually primary. |
| Current/success | Source-owned current, verified, cite-ready, or action-ready state | Must not conceal stale, incomplete, redacted, or unresolved evidence. |
| Warning/stale | Stale projection, rebuild delay, partial identity hydration, or precondition risk | Label, reason, and timestamp carry the meaning. |
| Error/blocked | Denied, unavailable, failed verification, tenant mismatch, or blocked command | Use only from source-owned state and safe reasons. |
| Redaction | Authorized notice that content exists but is not visible | Must be visually distinct from warning and error; exact mapping is an open decision. |
| Degraded | Partially resolved or lower-confidence evidence | Must be visually distinct from warning and error; exact mapping is an open decision. |
| Information | Audit, projection, citation, or diagnostic detail | Must not appear more authoritative than canonical record content. |

Every load-bearing state combines visible text, structure, and an icon or shape where the inherited component provides one. Color is supplemental. The current contrast floor remains WCAG 2.1 AA; light, dark, high-contrast, and forced-colors verification is required when UI scope activates.

The values in `ux-design-directions.html` are illustrative only. They must not be copied into production tokens or treated as an approved reference palette.

## Typography

The Blazor Fluent UI V5 / Fluent 2 type ramp is the contract. No custom family, heading ramp, weight scale, or line-height scale is introduced.

- Record identity uses the inherited page-title or heading role.
- Section labels use compact inherited headings.
- Evidence uses the inherited body role with readable line length and line height.
- Timestamps, actor state, projection freshness, and citation anchors use an inherited metadata role but remain fully legible.
- Status labels are short, stable, and never depend on weight or color alone.
- Monospace is restricted to authorized identifiers, hashes, correlation IDs, immutable references, and citation blocks.
- Long authorized identifiers may wrap or truncate only when full-value access and copy remain available without hover through a keyboard-operable, screen-reader-named mechanism that remains usable at 200% text resize, 320 CSS-pixel reflow, high contrast, and forced colors.

## Layout & Spacing

The layout is dense, predictable, and case-file oriented. Use `{spacing.base}` as the foundation, `{spacing.compact}` only for tightly related metadata, `{spacing.section}` between sibling content sections, and `{spacing.region}` between major regions.

On wide surfaces, the Split Investigation Lens places the `Tenant-scoped Find Pane` beside the governed record. The record header precedes the `Trust Posture Strip`; the `Evidence Completeness Indicator` precedes `Evidence Timeline Entry` content; detail and diagnostics remain progressive. Actions sit beside the decision they affect, citations beside the evidence they cite, and redaction explanations beside the content they replace.

Do not create card-heavy dashboards or nested floating surfaces. Standard page-like surfaces with two or more sibling titled sections use the inherited Fluent accordion pattern; page titles, breadcrumbs, command bars, and a single primary content region remain outside it.

Responsive behavior and breakpoint capability are specified in `EXPERIENCE.md`. Layout changes never change trust order, DTO shape, redaction boundaries, or source-owned state.

## Elevation & Depth

Inherit Blazor Fluent UI V5 elevation, borders, focus treatment, and surface hierarchy. Conversations adds no custom shadows or tonal elevation scale. Hierarchy comes from information order, typography, and inherited component states—not ornamental depth.

## Shapes

Inherit Blazor Fluent UI V5 corner radii and control shapes. Conversations adds no custom radius scale. Pills or badges may appear only where supplied by an inherited status component and must always include visible text.

## Components

The names below are canonical across both spines. Retired legacy labels are provenance only.

| Component | Visual anatomy | State appearance |
|---|---|---|
| Trust Fact | Compact label/value pair containing only fields approved in `CONTRACT-BINDINGS.md`. | Missing required source-owned metadata means no confident visual claim; use an inherited neutral or unavailable treatment. |
| SafeReasonInline | Short visible explanation immediately adjacent to the affected state or action. | Calm, permission-safe text; no hover-only disclosure. |
| SafeReasonDetail | Independently framed detail within `Evidence Detail Drawer`. | Generic pending or unavailable framing until authorization succeeds. |
| Redaction Placeholder | Content-replacement block at the same reading level as the hidden evidence. | Visible “Redacted” semantics; no blur, hidden original, or length-revealing skeleton. |
| Freshness Marker | `FreshnessState`, `ReasonCode`, `ProjectionCursor`, public position, last-applied time, generated time, and lag when present. | Current, stale, rebuilding, and unavailable remain text-distinct; exact color mappings remain blocked by `OD-01`. |
| Command Availability Marker | Action state, safe reason, required permission/precondition summary, and evaluation time. | Available never appears from missing metadata; unavailable or contradictory metadata is visibly fail-closed. |
| Citation Control | Compact inline affordance beside the evidence it cites. | Broken or missing citation remains visible as degraded evidence rather than disappearing. |
| Participant Identity Marker | Authorized display identity supplied by the owning boundary plus source-owned `ParticipantResolutionState`; no hydration-source field is assumed. | Unresolved, stale, filtered, or unavailable identity never looks definitive; unsupported detail is non-renderable. |
| Tenant-scoped Find Pane | Search, filters, permission-safe result count/facets, and result list under persistent tenant scope. | Loading, no accessible matches, stale results, restricted scope, and denied scope have distinct text treatments. |
| Trust Preview Result Row | Record identity and business-safe context first; compact trust facts and safe next hint second. | Selection uses inherited focus/selection styling; trust state is never communicated by selection alone. |
| Governed Record Header | Record identity, tenant-safe context, temporal cursor, freshness, and action eligibility in that order. | Denied or unavailable state does not expose protected identity. |
| Trust Posture Strip | Fixed-order presentation of freshness, completeness, citation, participant, audit, verification, and command fields from `ConversationEvidenceTrustPostureV1`. | Stable geometry; no client-computed aggregate or winner. An aggregate is non-renderable until an owned public contract exists. |
| Evidence Completeness Indicator | Source-owned `EvidenceCompletenessState` immediately before timeline content. | Display copy must come from the approved mapping in `OD-05`; the client does not mint scoped completeness categories. |
| Evidence Timeline Entry | Chronological marker, actor/time metadata, content or placeholder, and adjacent citation/audit affordances. | Redacted, permission-filtered, stale, missing-citation, and audit-unavailable entries stay ordered and visibly distinct. |
| Safe State Message | Full-width governed message with state, safe explanation, and next action. | Empty, loading, denied, unavailable, stale, rebuilding, redacted, and degraded are not interchangeable. |
| Evidence Detail Drawer | Inherited drawer shell with generic title until authorization; one detail concern at a time. | Pending, authorized, blocked, stale, unavailable, and audit-unavailable states do not flash protected content. |
| Command Gate | Group of `ConversationCommandAvailabilityV1` entries with source-owned action, permission, precondition, risk, freshness, audit, safe reason, evaluation time, classification, and recheck fields. | Missing, stale, inconsistent, or unauthorized metadata presents a governed unavailable/blocked state. Exact blocked-control interaction mechanics remain blocked by `OD-04`. |
| Permission-gated Forensic Timeline Mode | Explicit mode banner plus exact authorized evidence fields in chronological order. | Unavailable, authorized, partially hidden, stale, and audit-required modes remain visibly labeled. |
| Evidence Acceptance Summary | Conceptual read-only review card/table showing only fields backed by an approved evidence contract. | Mutation or acceptance recording is non-renderable until `OD-06` is resolved; no visual state proves hold lift. |
| Waiver and Blocker Summary | Conceptual read-only presentation of approved waiver/blocker evidence only. | Mutation is non-renderable until `OD-06`; active, expired, blocked, and deferred never collapse into a generic warning. |

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit FrontComposer and Blazor Fluent UI V5 for ordinary controls and theme behavior. | Recreate Fluent typography, color, spacing, focus, disabled, or density behavior in custom CSS. |
| Present the record as a governed case file with evidence and provenance in context. | Use chat bubbles, avatars as primary anchors, playful typing affordances, or AI “magic” decoration. |
| Keep trust posture visible before evidence reliance. | Use a clean or green-looking screen as an implicit claim of completeness. |
| Pair state with text, reason, time, and evidence. | Rely on color, weight, icon, or hidden tooltip alone. |
| Present trust dimensions in the source DTO's fixed order. | Compute a client-side overall posture or precedence winner. |
| Keep diagnostics one layer deeper than business-safe state. | Make raw EventStore streams or infrastructure logs the primary operator view. |
| Render redaction from safe source data. | Blur, mask, hide with CSS, or retain original content in DOM, accessibility, copy, title, telemetry, or responsive duplicates. |
| Keep the selected governed record visually dominant. | Turn the Split Investigation Lens into two competing dashboards. |
| Preserve inherited component semantics and focus treatment. | Copy interaction semantics from the illustrative HTML mock. |
