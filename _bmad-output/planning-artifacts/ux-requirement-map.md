---
authorityVersion: ux-preservation-planning-2026-08-04-v3
source: _bmad-output/planning-artifacts/ux-design-specification.md
currentDisposition: preserved-not-activated
planningCandidate: 1e72e63cbf2b556b8dc6fe732428c66f51985ac7
epicAuthority: epic-6-authority-2026-08-04-v11
architectureAuthority: conversations-architecture-2026-08-04-v11
currentOwner: Stories 8.1-8.2 preservation contract
activationAuthority: separate-approved-release-authority-required
---

# UX Requirement Map

> **Preservation-only UX authority.** This map preserves product UX decisions
> and acceptance obligations. It does not activate product UI implementation
> or assign current feature-delivery ownership. Activation requires separate
> approved release authority.

The `Current disposition` column is authoritative for the corrective
initiative. `Historical provenance` retains the original story mapping solely
for navigation and cannot authorize work. The Story 6.4 disposition artifacts
will bind source hashes and owners during story execution; their planned paths
are rebound by Epic 8 under v11 but are not implemented by this planning publication.

## UX Decision Inventory

| UX-DR | Source Section | Summary | Current disposition | Historical provenance |
| --- | --- | --- | --- | --- |
| UX-DR1 | Design system foundation | Use FrontComposer and Fluent UI Blazor as the baseline design system. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.8 |
| UX-DR2 | Core experience / component strategy | Add custom Conversations UI only where trust interpretation demands it. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.8 |
| UX-DR3 | Trust as a contract | Render trust states and action enablement only from Conversations-owned outputs. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1, 3.2, 3.5 |
| UX-DR4 | Generated-first boundaries | Prevent the admin UI from browsing raw EventStore streams as the primary experience. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.3 |
| UX-DR5 | Visual design foundation | Preserve reusable state treatments for trust, freshness, denial, degradation, and redaction. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.8 |
| UX-DR6 | Redaction safety | Preserve redaction notices that never expose original values through any surface. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 2.4.2, 3.3, 3.8 |
| UX-DR7 | Evidence cues | Preserve audit markers, evidence anchors, and chain-of-custody cues. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.3, 3.4 |
| UX-DR8 | Participant identity | Preserve safe participant resolution and degraded hydration states. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 1.3, 3.2, 3.3 |
| UX-DR9 | Command availability | Preserve server-owned command availability and blocked reasons. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.5, 4.4 |
| UX-DR10 | Citation and temporal reconstruction | Preserve stable citation and temporal-reconstruction affordances. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.4, 3.8 |
| UX-DR11 | Generated surfaces | Preserve generated-first search, list, detail, form, loading, and empty states. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1, 3.2 |
| UX-DR12 | Custom trust-critical surfaces | Preserve the custom evidence, citation, redaction, audit, freshness, and command-gate boundary. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.2-3.5 |
| UX-DR13 | Trust component contracts | Preserve explicit inputs, fail-closed behavior, and tests for trust-critical components. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.2-3.5 |
| UX-DR14 | Gated action tests | Preserve proof that gated actions disable on absent or stale metadata. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.5 |
| UX-DR15 | Evidence timeline tests | Preserve degraded handling for missing or deleted evidence. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.2, 3.4 |
| UX-DR16 | Accessibility tests | Preserve keyboard and screen-reader accessibility for evidence and audit surfaces. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.3, 3.8 |
| UX-DR17 | Trust primitives | Preserve Trust Fact, SafeReason, Freshness Marker, and Citation Control primitives. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.5 |
| UX-DR18 | Investigation composites | Preserve Find Pane, Trust Preview, Governed Header, Evidence Timeline, and related composites. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.7 |
| UX-DR19 | Evidence trust model | Preserve one shared trust model for trust-bearing components. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1, 3.2, 4.4 |
| UX-DR20 | Detail authorization | Preserve independent drawer authorization and close-on-downgrade behavior. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.2, 3.3, 3.5 |
| UX-DR21 | Search disclosure safety | Preserve permission-safe counts, facets, autocomplete, pagination, and timing. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.1 |
| UX-DR22 | Freshness evidence | Preserve projection version, timestamp, or freshness source on trust-bearing components. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 1.7, 3.2 |
| UX-DR23 | Safe telemetry | Preserve content-safe, bounded trust-workflow telemetry. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.6, 6.8 |
| UX-DR24 | Trust primitive test suite | Preserve trust primitives before higher-order components with safety tests. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.2 |
| UX-DR25 | Core investigation flow | Preserve Find -> Open -> Verify -> Cite, Act, or Stop. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1-3.7 |
| UX-DR26 | Evidence detail components | Preserve citation, audit, participant, freshness, and command-reason details. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.2-3.6 |
| UX-DR27 | Review and enhancement flows | Preserve forensic timeline, acceptance, waiver, and responsive-find designs. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.6, 3.7 |
| UX-DR28 | Canonical fixtures | Preserve the canonical trust-state fixture set. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.7, 3.8 |
| UX-DR29 | Trust precedence | Preserve deterministic conservative trust precedence. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1, 3.2 |
| UX-DR30 | Safe states | Preserve distinct empty, loading, denied, unavailable, stale, redacted, degraded, and no-access states. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.1, 3.2 |
| UX-DR31 | Tenant-scoped search | Preserve tenant-scoped, permission-filtered, trust-previewed search. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.1 |
| UX-DR32 | Trust summary band | Preserve tenant scope, identity, freshness, completeness, citation, participant, and command state before timeline reliance. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.2 |
| UX-DR33 | Drawers and dialogs | Preserve drawers for evidence details and dialogs for governance-changing confirmation. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.3, 3.5 |
| UX-DR34 | Governance form safety | Preserve intent-only governance forms with no editable authority context. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.5 |
| UX-DR35 | Copy/export safety | Preserve authorization-rechecked, permission-safe copy and export DTOs. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.4, 3.8 |
| UX-DR36 | Trust transitions | Preserve safe clearing and closure on permission or metadata downgrade. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.5 |
| UX-DR37 | Safety AC set | Preserve AC-SAFE-001 through AC-SAFE-008. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.7, 3.8 |
| UX-DR38 | UX quality gates | Preserve leakage, tenant-isolation, trust-provenance, and command-safety gates. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Stories 3.6-3.8 |
| UX-DR39 | Responsive strategy | Preserve desktop-first governance workflows with safe mobile triage. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR40 | Responsive disclosure surfaces | Preserve every responsive variant as an independent disclosure surface. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR41 | Breakpoint trust order | Preserve scope, identity, trust, completeness, and command eligibility before reliance. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR42 | Mobile mutation default | Preserve the default block on narrow-screen governance mutations. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR43 | Breakpoints | Preserve mobile, tablet, desktop, and wide-desktop breakpoints. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR44 | WCAG 2.1 AA | Preserve WCAG 2.1 AA for operator/admin web surfaces. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR45 | Assistive output safety | Preserve tenant, permission, and redaction safety in assistive output. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR46 | Accessibility microcopy | Preserve safe microcopy for redacted, unavailable, restricted, loading, and partial states. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR47 | Trust metadata loading | Preserve trust metadata loading before or with trust-bearing content. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR48 | Virtualized timeline safety | Preserve order, navigation, and redaction semantics in virtualized timelines. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR49 | Mobile handoff links | Preserve permission-safe identifiers and temporal cursors in handoff links. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR50 | Responsive/accessibility matrix | Preserve responsive and accessibility testing across breakpoints and trust states. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR51 | Leak Sentinel | Preserve DOM, ARIA, title, clipboard, telemetry, screenshot, and accessibility-snapshot scanning. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |
| UX-DR52 | Canonical responsive fixtures | Preserve the canonical responsive/accessibility fixtures. | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical: Story 3.8 |

## Generated Acceptance-Criterion Inventory

The planning-authority validator derives every explicit acceptance-criterion
identifier from `ux-design-specification.md` and requires exact, ordered,
one-to-one parity with this table. An identifier cannot be omitted, duplicated,
renamed, or assigned implementation ownership here.

| Criterion | Source Section | Current disposition | Historical provenance |
| --- | --- | --- | --- |
| AC-SAFE-001 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-SAFE-002 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-SAFE-003 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-SAFE-004 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-SAFE-005 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-SAFE-006 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-SAFE-007 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-SAFE-008 | Safety Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical UX safety contract |
| AC-RESP-001 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-002 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-003 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-004 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-005 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-006 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-007 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-008 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-009 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-010 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-011 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-012 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-013 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-014 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-RESP-015 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical responsive contract |
| AC-A11Y-001 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical accessibility contract |
| AC-A11Y-002 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical accessibility contract |
| AC-LEAK-001 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical leakage contract |
| AC-MOB-001 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical mobile contract |
| AC-PERF-001 | Responsive Acceptance Criteria | preserved-not-activated; Stories 8.1-8.2 preservation contract | Historical loading/performance-safety contract |
