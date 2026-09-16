---
name: Hexalith.Conversations UX Traceability
status: draft
currentDisposition: preserved-not-activated
activationAuthority: separate-approved-release-authority-required
updated: 2026-09-16
proposedTestStatus: identifiers-reserved-not-implemented-or-executed
sources:
  - _bmad-output/planning-artifacts/ux-requirement-map.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/EXPERIENCE.md
---

# Hexalith.Conversations UX Traceability

> Draft preservation trace only. This artifact neither activates product UI work nor changes the authority or disposition of any source requirement. Each source ID below has exactly one primary current disposition. Proposed test IDs reserve stable names only; no test is claimed implemented, run, passed, or release-authoritative.

## Authority and disposition rules

- `DESIGN.md` owns visual decisions; `EXPERIENCE.md` owns behavior, state, interaction, accessibility, and flows.
- `OD-01` through `OD-10` are the stable provisional identifiers in the current [`EXPERIENCE.md` Open Decisions](EXPERIENCE.md#open-decisions) table. They are phase blockers, not implementation choices.
- `Architecture V15` means the final, hold-active `conversations-architecture-2026-09-16-v15` amendment in [`architecture.md`](../../architecture.md#2026-09-16-v15-current-authority-and-runtime-convergence-amendment). It supersedes the map's V14 architecture binding only for an exact rule named below; it does not activate UX.
- `Not product UX` keeps refactor/tooling/runtime work outside the product spines. It does not discard the upstream engineering requirement.
- A proposed test ID is supporting evidence for its row, not a second disposition.

## Open-decision registry

| ID | Current numbered open decision |
|---|---|
| OD-01 | Exact Fluent/custom mappings for load-bearing visual state roles and contrast evidence. |
| OD-02 | WCAG 2.2 AA promotion decision. |
| OD-03 | Blazor Fluent UI V5 prerelease/stable dependency policy. |
| OD-04 | Accessible blocked-action control and reason mechanism. |
| OD-05 | Complete source-owned public reason-code and presentation-label mapping. |
| OD-06 | Activation-time role/surface/breakpoint capability matrix. |
| OD-07 | Global offline/network-loss and authentication/session-expiry behavior. |
| OD-08 | English-only versus governed localization/resource-key contract. |
| OD-09 | Live-region, zoom/reflow, temporal-focus, and browser/assistive-technology validation matrix. |
| OD-10 | Accepted source bindings/tombstones for legacy provenance and current spine authority. |

## Preserved UX decision closure

| Source ID | Exactly one current disposition | Proposed evidence |
|---|---|---|
| UX-DR1 | [`EXPERIENCE.md` → Foundation](EXPERIENCE.md#foundation) | — |
| UX-DR2 | [`DESIGN.md` → Components](DESIGN.md#components) | — |
| UX-DR3 | [`EXPERIENCE.md` → Public vocabulary and V15 normative target mapping](EXPERIENCE.md#public-vocabulary-and-v15-normative-target-mapping) | UX-TST-STATE-001; UX-TST-CMD-001 |
| UX-DR4 | [`EXPERIENCE.md` → Inspiration & Anti-patterns](EXPERIENCE.md#inspiration--anti-patterns) | — |
| UX-DR5 | [`DESIGN.md` → Colors](DESIGN.md#colors) | UX-TST-A11Y-004 |
| UX-DR6 | [`EXPERIENCE.md` → Component Patterns → `Redaction Placeholder`](EXPERIENCE.md#component-patterns) | UX-TST-LEAK-001 |
| UX-DR7 | [`EXPERIENCE.md` → Component Patterns → `Trust Fact`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-002 |
| UX-DR8 | [`EXPERIENCE.md` → Component Patterns → `Participant Identity Marker`](EXPERIENCE.md#component-patterns) | UX-TST-LEAK-001 |
| UX-DR9 | [`EXPERIENCE.md` → Component Patterns → `Command Gate`](EXPERIENCE.md#component-patterns) | UX-TST-CMD-001; UX-TST-CMD-002 |
| UX-DR10 | [`EXPERIENCE.md` → Interaction Primitives](EXPERIENCE.md#interaction-primitives) | UX-TST-FOCUS-001 |
| UX-DR11 | [`EXPERIENCE.md` → Surface-state closure](EXPERIENCE.md#surface-state-closure) | UX-TST-STATE-004 |
| UX-DR12 | [`DESIGN.md` → Components](DESIGN.md#components) | — |
| UX-DR13 | [`EXPERIENCE.md` → Component Patterns](EXPERIENCE.md#component-patterns) | UX-TST-STATE-004 |
| UX-DR14 | [`EXPERIENCE.md` → Component Patterns → `Command Gate`](EXPERIENCE.md#component-patterns) | UX-TST-CMD-001 |
| UX-DR15 | [`EXPERIENCE.md` → Component Patterns → `Evidence Timeline Entry`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-004 |
| UX-DR16 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-001; UX-TST-A11Y-002 |
| UX-DR17 | [`EXPERIENCE.md` → Component Patterns](EXPERIENCE.md#component-patterns) | UX-TST-STATE-004 |
| UX-DR18 | [`DESIGN.md` → Components](DESIGN.md#components) | — |
| UX-DR19 | [`EXPERIENCE.md` → Component Patterns → `Trust Posture Strip`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-002 |
| UX-DR20 | [`EXPERIENCE.md` → Component Patterns → `Evidence Detail Drawer`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-003; UX-TST-FOCUS-001 |
| UX-DR21 | [`EXPERIENCE.md` → Component Patterns → `Tenant-scoped Find Pane`](EXPERIENCE.md#component-patterns) | UX-TST-TENANT-001 |
| UX-DR22 | Superseded by exact [`Architecture V15` → AD-8 public freshness semantics](../../architecture.md#ad-8--replay-identity-time-and-canonical-bytes) | UX-TST-STATE-002 |
| UX-DR23 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001 |
| UX-DR24 | [`EXPERIENCE.md` → Component Patterns](EXPERIENCE.md#component-patterns) | UX-TST-STATE-004 |
| UX-DR25 | [`EXPERIENCE.md` → Flow 1, Governed investigation](EXPERIENCE.md#flow-1--governed-investigation-sarah-compliance-operator-reviewing-a-disputed-exchange) | UX-TST-A11Y-001; UX-TST-TENANT-001 |
| UX-DR26 | [`EXPERIENCE.md` → Component Patterns](EXPERIENCE.md#component-patterns) | UX-TST-STATE-003 |
| UX-DR27 | [`EXPERIENCE.md` → Key Flows](EXPERIENCE.md#key-flows) | UX-TST-RESP-001 |
| UX-DR28 | [`EXPERIENCE.md` → Public vocabulary and V15 normative target mapping](EXPERIENCE.md#public-vocabulary-and-v15-normative-target-mapping) | UX-TST-STATE-001 |
| UX-DR29 | [`EXPERIENCE.md` → Component Patterns → `Trust Posture Strip`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-002 |
| UX-DR30 | [`EXPERIENCE.md` → Surface-state closure](EXPERIENCE.md#surface-state-closure) | UX-TST-STATE-004 |
| UX-DR31 | [`EXPERIENCE.md` → Component Patterns → `Tenant-scoped Find Pane`](EXPERIENCE.md#component-patterns) | UX-TST-TENANT-001 |
| UX-DR32 | [`EXPERIENCE.md` → Component Patterns → `Trust Posture Strip`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-002 |
| UX-DR33 | [`EXPERIENCE.md` → Interaction Primitives](EXPERIENCE.md#interaction-primitives) | UX-TST-STATE-003; UX-TST-FOCUS-001 |
| UX-DR34 | [`EXPERIENCE.md` → Interaction Primitives → Forms](EXPERIENCE.md#interaction-primitives) | UX-TST-CMD-002 |
| UX-DR35 | [`EXPERIENCE.md` → Interaction Primitives → Citation copy](EXPERIENCE.md#interaction-primitives) | UX-TST-LEAK-001 |
| UX-DR36 | [`EXPERIENCE.md` → Interaction Primitives → Trust transitions](EXPERIENCE.md#interaction-primitives) | UX-TST-FOCUS-001 |
| UX-DR37 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-001; UX-TST-LEAK-001; UX-TST-CMD-002 |
| UX-DR38 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001; UX-TST-TENANT-001; UX-TST-CMD-001 |
| UX-DR39 | [`EXPERIENCE.md` → Responsive & Platform](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-001 |
| UX-DR40 | [`EXPERIENCE.md` → Responsive & Platform](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-002; UX-TST-LEAK-001 |
| UX-DR41 | [`EXPERIENCE.md` → Responsive & Platform](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-001 |
| UX-DR42 | [`EXPERIENCE.md` → Responsive & Platform → Mobile](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-003 |
| UX-DR43 | [`EXPERIENCE.md` → Responsive & Platform](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-001 |
| UX-DR44 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-004 |
| UX-DR45 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-002; UX-TST-LEAK-001 |
| UX-DR46 | [`EXPERIENCE.md` → Voice and Tone](EXPERIENCE.md#voice-and-tone) | UX-TST-A11Y-003 |
| UX-DR47 | [`EXPERIENCE.md` → Surface-state closure → Governed Record View](EXPERIENCE.md#surface-state-closure) | UX-TST-STATE-004 |
| UX-DR48 | [`EXPERIENCE.md` → Interaction Primitives → Timeline navigation](EXPERIENCE.md#interaction-primitives) | UX-TST-FOCUS-001; UX-TST-LEAK-001 |
| UX-DR49 | [`EXPERIENCE.md` → Responsive & Platform → Mobile](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-002; UX-TST-FOCUS-001 |
| UX-DR50 | Open Decision OD-09 | UX-TST-A11Y-003; UX-TST-FOCUS-001; UX-TST-RESP-004 |
| UX-DR51 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001 |
| UX-DR52 | Open Decision OD-09 | UX-TST-RESP-001; UX-TST-RESP-004 |

## Preserved UX acceptance closure

| Source ID | Exactly one current disposition | Proposed evidence |
|---|---|---|
| AC-SAFE-001 | [`EXPERIENCE.md` → Public vocabulary and V15 normative target mapping → `Forbidden / forbidden`](EXPERIENCE.md#public-vocabulary-and-v15-normative-target-mapping) | UX-TST-TENANT-001 |
| AC-SAFE-002 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001 |
| AC-SAFE-003 | [`EXPERIENCE.md` → Component Patterns → `Evidence Detail Drawer`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-003 |
| AC-SAFE-004 | [`EXPERIENCE.md` → Component Patterns → `Command Gate`](EXPERIENCE.md#component-patterns) | UX-TST-CMD-002 |
| AC-SAFE-005 | [`EXPERIENCE.md` → Public vocabulary and V15 normative target mapping](EXPERIENCE.md#public-vocabulary-and-v15-normative-target-mapping) | UX-TST-STATE-001; UX-TST-CMD-001 |
| AC-SAFE-006 | [`EXPERIENCE.md` → Component Patterns → `Trust Posture Strip`](EXPERIENCE.md#component-patterns) | UX-TST-STATE-002 |
| AC-SAFE-007 | [`EXPERIENCE.md` → Component Patterns → `Tenant-scoped Find Pane`](EXPERIENCE.md#component-patterns) | UX-TST-TENANT-001 |
| AC-SAFE-008 | [`EXPERIENCE.md` → Surface-state closure](EXPERIENCE.md#surface-state-closure) | UX-TST-STATE-004 |
| AC-RESP-001 | [`EXPERIENCE.md` → Responsive & Platform](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-001 |
| AC-RESP-002 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001 |
| AC-RESP-003 | [`EXPERIENCE.md` → Responsive & Platform → Mobile](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-003 |
| AC-RESP-004 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-002; UX-TST-LEAK-001 |
| AC-RESP-005 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-001 |
| AC-RESP-006 | [`EXPERIENCE.md` → Interaction Primitives → Trust transitions](EXPERIENCE.md#interaction-primitives) | UX-TST-FOCUS-001 |
| AC-RESP-007 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-RESP-004 |
| AC-RESP-008 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001 |
| AC-RESP-009 | [`EXPERIENCE.md` → Responsive & Platform](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-002 |
| AC-RESP-010 | [`EXPERIENCE.md` → Responsive & Platform](EXPERIENCE.md#responsive--platform) | UX-TST-LEAK-001; UX-TST-RESP-002 |
| AC-RESP-011 | [`EXPERIENCE.md` → Responsive & Platform → Mobile](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-003 |
| AC-RESP-012 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-003 |
| AC-RESP-013 | Open Decision OD-09 | UX-TST-FOCUS-001 |
| AC-RESP-014 | [`EXPERIENCE.md` → Surface-state closure](EXPERIENCE.md#surface-state-closure) | UX-TST-STATE-004 |
| AC-RESP-015 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001 |
| AC-A11Y-001 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-002 |
| AC-A11Y-002 | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-001 |
| AC-LEAK-001 | [`EXPERIENCE.md` → Accessibility Floor → Leak Sentinel coverage](EXPERIENCE.md#accessibility-floor) | UX-TST-LEAK-001 |
| AC-MOB-001 | [`EXPERIENCE.md` → Responsive & Platform → Mobile](EXPERIENCE.md#responsive--platform) | UX-TST-RESP-003 |
| AC-PERF-001 | [`EXPERIENCE.md` → Surface-state closure](EXPERIENCE.md#surface-state-closure) | UX-TST-PERF-001; UX-TST-LEAK-001 |

## UX-relevant PRD group closure

Group labels and ranges are references only; the PRD remains authoritative for their wording and release disposition.

| PRD group | Exactly one current disposition | Proposed evidence |
|---|---|---|
| Current refactor FR-1–FR-20 and UJ-1–UJ-3 | Not product UX | Engineering/conformance evidence owned by the refactor PRD; separation is recorded in [`EXPERIENCE.md` → Refactor/tooling journey disposition](EXPERIENCE.md#refactortooling-journey-disposition). |
| Preserved actor and acceptance journeys | [`EXPERIENCE.md` → Key Flows](EXPERIENCE.md#key-flows) | UX-TST-A11Y-001; UX-TST-TENANT-001 |
| Feature-FR1–Feature-FR12 — Conversation Lifecycle | [`EXPERIENCE.md` → Key Flows](EXPERIENCE.md#key-flows) | UX-TST-STATE-004 |
| Feature-FR13–Feature-FR18 — Participant Attribution | [`EXPERIENCE.md` → Component Patterns → `Participant Identity Marker`](EXPERIENCE.md#component-patterns) | UX-TST-LEAK-001 |
| Feature-FR19–Feature-FR25 — Business Context And References | [`EXPERIENCE.md` → Flow 6, Stable-reference continuity](EXPERIENCE.md#flow-6--stable-reference-continuity-naomi-cross-product-owner-reviewing-an-upstream-lifecycle-change) | UX-TST-STATE-004 |
| Feature-FR26–Feature-FR32 — Tenant Access And Isolation | [`EXPERIENCE.md` → Public vocabulary and V15 normative target mapping](EXPERIENCE.md#public-vocabulary-and-v15-normative-target-mapping) | UX-TST-TENANT-001 |
| Feature-FR33–Feature-FR41 — Event Sourcing, Projections, And Publication | Superseded by exact [`Architecture V15` → AD-7/AD-8 runtime and public-state authority](../../architecture.md#ad-7--conversations-lifecycle-and-watermark-authority) | UX-TST-STATE-001; UX-TST-STATE-002 |
| Feature-FR42–Feature-FR55 — Governance And Audit | Open Decision OD-06 | UX-TST-CMD-001; UX-TST-CMD-002; UX-TST-CMD-003 |
| Feature-FR56–Feature-FR69 — Operator And Compliance Workflows | [`EXPERIENCE.md` → Key Flows](EXPERIENCE.md#key-flows) | UX-TST-A11Y-001; UX-TST-FOCUS-001 |
| Feature-FR70–Feature-FR80 — Consumer Contracts And Developer Experience | [`EXPERIENCE.md` → Flow 3, Typed adopter integration](EXPERIENCE.md#flow-3--typed-adopter-integration-diego-adopter-developer-proving-the-minimal-path) | — |
| Feature-FR81–Feature-FR94 — Compatibility, Evidence, And Release Gates | [`EXPERIENCE.md` → Flow 4, Evidence-based acceptance](EXPERIENCE.md#flow-4--evidence-based-acceptance-julian-platform-owner-and-helen-security-reviewer-evaluating-release-evidence) | UX-TST-STATE-004 |
| Feature-FR95–Feature-FR99 — Observability And Operations | [`EXPERIENCE.md` → Flow 5, Audit-sink degradation](EXPERIENCE.md#flow-5--audit-sink-degradation-marcus-sre-diagnosing-an-incident) | UX-TST-STATE-004 |
| Feature-FR100–Feature-FR104 — Scope Boundaries And Lifecycle Commitments | [`EXPERIENCE.md` → Foundation](EXPERIENCE.md#foundation) | — |
| Feature-NFR1–Feature-NFR8 — Measurement, Evidence, And Waiver Discipline | [`EXPERIENCE.md` → Flow 4, Evidence-based acceptance](EXPERIENCE.md#flow-4--evidence-based-acceptance-julian-platform-owner-and-helen-security-reviewer-evaluating-release-evidence) | — |
| Feature-NFR9–Feature-NFR15 — Performance | [`EXPERIENCE.md` → Key Flows](EXPERIENCE.md#key-flows) | UX-TST-PERF-001 |
| Feature-NFR16–Feature-NFR21 — Security And Privacy | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-TENANT-001; UX-TST-LEAK-001; UX-TST-CMD-002 |
| Feature-NFR22–Feature-NFR29 — Reliability, Resilience, And Recovery | [`EXPERIENCE.md` → Surface-state closure](EXPERIENCE.md#surface-state-closure) | UX-TST-STATE-004 |
| Feature-NFR30–Feature-NFR37 — Scalability, Capacity, And Cost | Not product UX | Runtime capacity, SLO, and release evidence remain upstream-owned; UX consumes only source-owned state. |
| Feature-NFR38–Feature-NFR43 — Data Integrity And Event Sourcing | [`EXPERIENCE.md` → Flow 7, Post-harm testimony](EXPERIENCE.md#flow-7--post-harm-testimony-daniel-operations-leader-reconstructing-what-happened) | UX-TST-STATE-002 |
| Feature-NFR44–Feature-NFR48 — Projection Freshness | Superseded by exact [`Architecture V15` → AD-7 public mapping and AD-8 freshness semantics](../../architecture.md#ad-7--conversations-lifecycle-and-watermark-authority) | UX-TST-STATE-001; UX-TST-STATE-002 |
| Feature-NFR49–Feature-NFR54 — Integration And Compatibility | [`EXPERIENCE.md` → Flow 3, Typed adopter integration](EXPERIENCE.md#flow-3--typed-adopter-integration-diego-adopter-developer-proving-the-minimal-path) | — |
| Feature-NFR55–Feature-NFR61 — Operability And Observability | [`EXPERIENCE.md` → Flow 5, Audit-sink degradation](EXPERIENCE.md#flow-5--audit-sink-degradation-marcus-sre-diagnosing-an-incident) | UX-TST-LEAK-001 |
| Feature-NFR62–Feature-NFR68 — Compliance, Retention, And Release Evidence | [`EXPERIENCE.md` → Flow 4, Evidence-based acceptance](EXPERIENCE.md#flow-4--evidence-based-acceptance-julian-platform-owner-and-helen-security-reviewer-evaluating-release-evidence) | UX-TST-CMD-001 |
| Feature-NFR69–Feature-NFR75 — Accessibility And Human Trust | [`EXPERIENCE.md` → Accessibility Floor](EXPERIENCE.md#accessibility-floor) | UX-TST-A11Y-001; UX-TST-A11Y-002; UX-TST-A11Y-003; UX-TST-A11Y-004; UX-TST-FOCUS-001 |
| Feature-NFR76–Feature-NFR77 — Content-safe Human Trust Messaging | [`EXPERIENCE.md` → Voice and Tone](EXPERIENCE.md#voice-and-tone) | UX-TST-A11Y-003 |
| Preserved qualitative constraints and ownership boundaries | [`EXPERIENCE.md` → Foundation](EXPERIENCE.md#foundation) | — |

## Proposed test contract

All IDs are reserved and normative only after separate activation authority approves the relevant open decisions, fixtures, owners, environments, and evidence locations.

| Proposed test ID | Normative concern | Minimum assertion | Current readiness |
|---|---|---|---|
| UX-TST-A11Y-001 | Keyboard and screen-reader task completion | Complete Find → Read → Trust → Cite or safe stop with equivalent scope, trust, and failure information. | Spine-defined; fixture/evidence owner unassigned. |
| UX-TST-A11Y-002 | Accessible disclosure safety | Accessibility tree, names, descriptions, headings, summaries, and helper text contain no unauthorized or redacted value. | Spine-defined; fixture/evidence owner unassigned. |
| UX-TST-A11Y-003 | Reading order and state announcements | Trust metadata precedes reliant content; meaningful safe transitions are announced without protected detail. | Blocked by OD-09 and OD-08. |
| UX-TST-A11Y-004 | Non-color and contrast behavior | Every load-bearing state survives light, dark, high-contrast, forced-colors, and zoom/reflow checks without color-only meaning. | Blocked by OD-01 and OD-09. |
| UX-TST-LEAK-001 | Cross-surface leakage | Scan visible/hidden DOM, accessibility tree, URL/query, title, tooltip, clipboard, telemetry, screenshot, placeholders, responsive duplicates, and viewport menus. | Spine-defined; fixture/evidence owner unassigned. |
| UX-TST-TENANT-001 | Tenant isolation and non-enumeration | Cross-tenant, nonexistent, unauthorized, stale-binding, and malformed-scope cases fail before data access with indistinguishable content-safe output. | Architecture/source-defined; fixture/evidence owner unassigned. |
| UX-TST-STATE-001 | Public state mapping | Every authoritative condition maps to the exact public state/reason; unknown or contradictory combinations fail closed. | Core V15 table defined; full coverage blocked by OD-05. |
| UX-TST-STATE-002 | Source-owned trust fields and freshness evidence | Individual posture dimensions remain source-owned and fixed-order with no client aggregate; replay anchor, generation instant, and lag semantics match V15. | Core V15 target semantics defined; runtime-conformance evidence blocked by AB-02. |
| UX-TST-STATE-003 | Independent detail authorization | Drawer authorizes on each open, never flashes protected content, closes/clears on downgrade, and restores safe focus. | Focus evidence blocked by OD-09. |
| UX-TST-STATE-004 | Surface state closure | Each IA surface covers cold/loading, current, empty where allowed, stale/degraded, rebuilding, unavailable/forbidden, error, retry/recovery, and downgrade where applicable. | Global shell cases blocked by OD-07. |
| UX-TST-FOCUS-001 | Safe focus lifecycle | Navigation, filtering, drawer/dialog close, virtualization, handoff, reconstruction, downgrade, and recovery land on or retain a safe focus target. | Blocked by OD-09. |
| UX-TST-RESP-001 | Breakpoint trust order | Mobile, tablet, desktop, and wide desktop preserve scope → identity → trust → completeness → eligibility before reliance. | Spine-defined; fixture/evidence owner unassigned. |
| UX-TST-RESP-002 | Responsive disclosure equivalence | Every breakpoint uses the same permission-safe contract and introduces no protected duplicate markup or payload. | Spine-defined; fixture/evidence owner unassigned. |
| UX-TST-RESP-003 | Mobile command safety | Governance mutations are absent or fail closed unless separately designed, authorized, accepted, and tested. | Capability coverage blocked by OD-06 and OD-04. |
| UX-TST-RESP-004 | Adaptive accessibility | Reduced motion, high contrast, browser zoom, 320 CSS-pixel/equivalent reflow, and narrow layouts preserve trust order and safe reasons. | Measurements/evidence matrix blocked by OD-09. |
| UX-TST-CMD-001 | Source-owned command availability | Missing, stale, inconsistent, or unauthorized command metadata never produces an enabled action. | Capability coverage blocked by OD-06. |
| UX-TST-CMD-002 | Pre-execution command safety | Tenant, role, trust, preconditions, and source-owned availability are rechecked immediately before execution. | Capability coverage blocked by OD-06. |
| UX-TST-CMD-003 | Accessible blocked action | Keyboard and screen-reader users can discover the safe reason without triggering the command or relying on hover. | Mechanism blocked by OD-04. |
| UX-TST-PERF-001 | Safe loading and investigation timing | Loading, lazy, virtualized, partial, and error states remain generic/content-safe while source-owned performance targets are measured. | Numeric targets/environments remain PRD-owned. |

## Completeness check

| Inventory | Source unique IDs | Trace rows | Missing | Duplicate | Extra |
|---|---:|---:|---:|---:|---:|
| Preserved UX decisions | 52 | 52 | 0 | 0 | 0 |
| Preserved UX acceptance criteria | 28 | 28 | 0 | 0 | 0 |

PRD coverage is group-level by the PRD's own current-refactor, preserved functional, preserved non-functional, actor/journey, and qualitative-boundary groupings. Open decisions remain open; this trace does not convert proposed evidence into release evidence or change either spine from `draft` / `preserved-not-activated`.
