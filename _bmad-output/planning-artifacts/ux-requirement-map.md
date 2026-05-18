# UX Requirement Map

This map stabilizes the `UX-DR` labels used in `epics.md` against the UX source document. Update this file whenever `ux-design-specification.md` changes so story traceability remains reproducible.

| UX-DR | Source Section | Summary | Primary Epics / Stories | Notes |
| --- | --- | --- | --- | --- |
| UX-DR1 | Design system foundation | Use FrontComposer and Fluent UI Blazor as the baseline design system. | Stories 3.1-3.8 | Generated-first UI foundation. |
| UX-DR2 | Core experience / component strategy | Add custom Conversations UI only where trust interpretation demands it. | Stories 3.1-3.8 | Evidence, redaction, freshness, citation, participant identity, and action safety. |
| UX-DR3 | Trust as a contract | Render trust states and action enablement only from Conversations-owned outputs. | Stories 3.1, 3.2, 3.5 | Server-owned trust metadata. |
| UX-DR4 | Generated-first boundaries | Prevent the admin UI from browsing raw EventStore streams as the primary experience. | Stories 3.1-3.3 | Governed projections only. |
| UX-DR5 | Visual design foundation | Implement reusable state treatments for trust, freshness, denial, degradation, and redaction. | Stories 3.1-3.8 | Shared state vocabulary. |
| UX-DR6 | Redaction safety | Implement redaction notices that never expose original values through any surface. | Stories 2.4.2, 3.3, 3.8 | Disclosure-surface safety. |
| UX-DR7 | Evidence cues | Implement audit markers, evidence anchors, and chain-of-custody cues. | Stories 3.3, 3.4 | Copyable, stable, accessible evidence. |
| UX-DR8 | Participant identity | Implement participant identity resolution and degraded hydration states safely. | Stories 1.3, 3.2, 3.3 | No unauthorized Parties personal data. |
| UX-DR9 | Command availability | Implement command availability and blocked reasons from server metadata. | Stories 3.5, 4.4 | Fail closed when metadata is missing or stale. |
| UX-DR10 | Citation and temporal reconstruction | Implement citation and temporal reconstruction affordances using stable evidence metadata. | Stories 3.4, 3.8 | Safe copy behavior. |
| UX-DR11 | Generated surfaces | Use generated-first surfaces for search, filtering, lists, details, forms, loading, and empty states. | Stories 3.1, 3.2 | FrontComposer baseline. |
| UX-DR12 | Custom trust-critical surfaces | Build custom evidence timeline, citation, redaction, audit, freshness, and command-gate components. | Stories 3.2-3.5 | Custom-reviewed components. |
| UX-DR13 | Trust component contracts | Custom trust-critical components declare inputs, fail-closed behavior, and tests. | Stories 3.2-3.5 | Component contract requirement. |
| UX-DR14 | Gated action tests | Provide tests proving gated actions disable on absent or stale metadata. | Story 3.5 | Command safety gate. |
| UX-DR15 | Evidence timeline tests | Prove timelines and citation components handle missing/deleted evidence as degraded. | Stories 3.2, 3.4 | No false trust. |
| UX-DR16 | Accessibility tests | Prove evidence timeline and audit entries are keyboard and screen-reader accessible. | Stories 3.3, 3.8 | Blocked reasons must not require hover. |
| UX-DR17 | Trust primitives | Implement trust primitives such as Trust Fact, SafeReason, Freshness Marker, and Citation Control. | Stories 3.1-3.5 | Reusable primitives. |
| UX-DR18 | Investigation composites | Implement Find Pane, Trust Preview, Governed Header, Evidence Timeline, and related composites. | Stories 3.1-3.7 | Find -> Read -> Trust flow. |
| UX-DR19 | Evidence trust model | Use a shared trust model for trust-bearing components. | Stories 3.1, 3.2, 4.4 | Shared contract. |
| UX-DR20 | Detail authorization | Detail drawers independently authorize and close on permission downgrade. | Stories 3.2, 3.3, 3.5 | Defense in depth. |
| UX-DR21 | Search disclosure safety | Search counts, facets, autocomplete, pagination, and timing are permission-safe. | Story 3.1 | No inaccessible record leaks. |
| UX-DR22 | Freshness evidence | Trust-bearing components expose projection version, timestamp, or freshness source. | Stories 1.7, 3.2 | No stale trust labels. |
| UX-DR23 | Safe telemetry | Implement trust workflow telemetry without protected content or unbounded sensitive identifiers. | Stories 3.6, 6.8 | Redaction and cardinality safe. |
| UX-DR24 | Trust primitive test suite | Implement trust primitives before higher-order components with safety tests. | Stories 3.1-3.2 | Ordering dependency. |
| UX-DR25 | Core investigation flow | Implement Find -> Open -> Verify -> Cite, Act, or Stop with trust-before-reliance. | Stories 3.1-3.7 | Operator journey. |
| UX-DR26 | Evidence detail components | Implement citation, audit linkage, participant resolution, freshness, and command reasoning details. | Stories 3.2-3.6 | Detail surfaces. |
| UX-DR27 | Review and enhancement flows | Implement forensic timeline, evidence acceptance, waiver summary, and responsive find drawer. | Stories 3.6, 3.7 | Review support. |
| UX-DR28 | Canonical fixtures | Define canonical trust-state fixtures. | Stories 3.7, 3.8 | Fixture set. |
| UX-DR29 | Trust precedence | Implement deterministic trust precedence. | Stories 3.1, 3.2 | Unknown never becomes assumed-safe. |
| UX-DR30 | Safe states | Implement safe empty, loading, denied, unavailable, stale, redacted, degraded, and no-access states. | Stories 3.1, 3.2 | No protected existence leaks. |
| UX-DR31 | Tenant-scoped search | Implement tenant-scoped, permission-filtered, trust-previewed search. | Story 3.1 | Business-safe filters. |
| UX-DR32 | Trust summary band | Show tenant scope, identity, freshness, completeness, citation, participant, and command state before timeline. | Story 3.2 | Every breakpoint. |
| UX-DR33 | Drawers and dialogs | Use drawers for evidence details and dialogs for governance-changing confirmations. | Stories 3.3, 3.5 | Interaction pattern. |
| UX-DR34 | Governance form safety | Governance forms collect only operator intent, not tenant/user/token context. | Story 3.5 | No editable authority fields. |
| UX-DR35 | Copy/export safety | Copy and export use permission-safe DTOs after authorization recheck. | Stories 3.4, 3.8 | Not rendered text selection. |
| UX-DR36 | Trust transitions | Permission downgrade and metadata changes clear protected content and close gated details. | Story 3.5 | Safe state transitions. |
| UX-DR37 | Safety AC set | Implement AC-SAFE-001 through AC-SAFE-008. | Stories 3.7, 3.8 | Safety acceptance suite. |
| UX-DR38 | UX quality gates | Implement leakage, tenant isolation, trust provenance, and command safety gates. | Stories 3.6-3.8 | Quality gates. |
| UX-DR39 | Responsive strategy | Use desktop-first operator/admin governance workflows with safe mobile triage. | Story 3.8 | Mobile read-only by default. |
| UX-DR40 | Responsive disclosure surfaces | Treat all responsive variants as independent disclosure surfaces. | Story 3.8 | Duplicated markup must be safe. |
| UX-DR41 | Breakpoint trust order | Preserve scope, identity, trust, completeness, and command eligibility before reliance at every breakpoint. | Story 3.8 | Layout invariant. |
| UX-DR42 | Mobile mutation default | Block mobile governance-changing actions unless explicitly designed and tested. | Story 3.8 | Narrow-screen safety. |
| UX-DR43 | Breakpoints | Use mobile, tablet, desktop, and wide desktop breakpoints. | Story 3.8 | Standard viewport set. |
| UX-DR44 | WCAG 2.1 AA | Meet WCAG 2.1 AA for operator/admin web surfaces. | Story 3.8 | Accessibility quality gate. |
| UX-DR45 | Assistive output safety | Assistive technology output obeys tenant, permission, and redaction rules. | Story 3.8 | ARIA/live region safety. |
| UX-DR46 | Accessibility microcopy | Use safe microcopy for redacted, unavailable, restricted, loading, and partial states. | Story 3.8 | No sensitive tooltips or labels. |
| UX-DR47 | Trust metadata loading | Trust metadata loads before or with trust-bearing content; placeholders are generic. | Story 3.8 | No protected length/count/timing leaks. |
| UX-DR48 | Virtualized timeline safety | Virtualized timelines preserve order, navigation, and redaction semantics. | Story 3.8 | Hidden DOM safety. |
| UX-DR49 | Mobile handoff links | Mobile triage and handoff links use only permission-safe identifiers and temporal cursors. | Story 3.8 | No protected URL content. |
| UX-DR50 | Responsive/accessibility matrix | Run responsive and accessibility tests across breakpoints and trust states. | Story 3.8 | Cross-state test matrix. |
| UX-DR51 | Leak Sentinel | Check DOM, attributes, ARIA, title, clipboard, telemetry, screenshots, and accessibility snapshots. | Story 3.8 | Disclosure scan. |
| UX-DR52 | Canonical responsive fixtures | Use canonical responsive/accessibility fixtures. | Story 3.8 | Fixture names preserved in epics. |
