# Validation Report — Conversations

- **UX specification:** `_bmad-output/planning-artifacts/ux-design-specification.md` (legacy format; no `DESIGN.md` / `EXPERIENCE.md` pair exists)
- **UX requirement map:** `_bmad-output/planning-artifacts/ux-requirement-map.md`
- **Design directions:** `_bmad-output/planning-artifacts/ux-design-directions.html`
- **Run at:** 2026-09-16T18:08:45+02:00
- **Lenses:** rubric walker · authority alignment · downstream consumability · accessibility

## Overall verdict

The legacy set is **broken as a downstream spine-pair contract despite unusually strong preserved safety and state material**. It records a coherent investigation model, fail-closed behavior, responsive disclosure rules, and an explicitly subordinate visual exploration, but it does not provide the machine-readable `DESIGN.md` / `EXPERIENCE.md` pair, normative design tokens, source-to-flow closure, or exact current UI-system and contract mappings needed by architecture, story-development, implementation, and test consumers. Because current authority keeps UX `preserved-not-activated`, these are blockers to any future activation—not authorization to implement the preserved screens.

The extra lenses sharpen the result. The v4 map and v14 non-activation route are current and human-readable, but the detailed specification is not bound by a current accepted hash; the memlog's contrary claim relies on a stale pending V6 draft. Downstream extraction is adequate for preserving the closed 52-decision/28-acceptance-ID denominator, not for implementing it safely. Accessibility is stronger than the visual-contract layer, but remains short of an activation-ready WCAG 2.2 AA contract.

## Category verdicts

- Flow coverage — broken
- Token completeness — broken
- Component coverage — broken
- State coverage — thin
- Visual reference coverage — strong
- Bloat & overspecification — thin
- Inheritance discipline — broken
- Shape fit — broken

Extra reviewers: authority alignment — **human-readable but mechanically incomplete** · downstream consumability — **broken for implementation, adequate for preservation/non-activation** · accessibility — **adequate preservation contract, not activation-ready**.

## Findings by severity

### Critical (3)

**[Token completeness] — Load-bearing visual roles have no normative values or Fluent 2 mappings** (`ux-design-specification.md:576-601,717-735`; `ux-design-directions.html:15-18`)

Current, stale, denied, degraded, redacted, unavailable, and blocked roles are named, but the five `--conversation-*` identifiers are undefined examples and the HTML palette is expressly non-normative. Downstream code cannot reproduce safety-relevant state differentiation without invention.

Fix: Under separate release authority, create `DESIGN.md` token frontmatter that inherits FrontComposer / Blazor Fluent UI V5, maps domain roles to Fluent 2 roles or component parameters, and states light/dark contrast obligations.

**[Shape fit] — The required peer spine pair does not exist** (`ux-design-specification.md:1-58,265-410,574-779,847-1566`)

There is no Google Labs-compatible `DESIGN.md`, no required `EXPERIENCE.md` structure, and no single extractable visual/behavioral contract for downstream consumers.

Fix: Only after approved authority permits an Update, distill the preserved decisions into the two peer files, retain `currentDisposition: preserved-not-activated`, link the legacy set as provenance, and make the spines win on conflict.

**[Downstream consumability] — Trust states lack one canonical source-owned UI vocabulary** (`ux-design-specification.md:1033,1042-1050,1096`; `ProjectionTrustState.cs:21-46`; `ProjectionFreshnessReasonCode.cs:21-51`)

The UX uses freshness, denial, degradation, restriction, completeness, and blocking terms that do not map one-to-one to the public trust-state and freshness-reason contracts, even though client-side trust inference is forbidden.

Fix: Publish a closed contract-field/wire-value/reason-code-to-display-state mapping with precedence, copy keys, allowed actions, and fail-closed fallbacks; reject unmapped or duplicate mappings in conformance tests.

### High (13)

**[Flow coverage] — Source-to-flow coverage is incomplete and undispositioned** (`prd.md:54-58,425-433`; `ux-design-specification.md:849-952`)

Current UJ-1 Nadia, UJ-2 Sam, and UJ-3 Priya are neither extracted nor explicitly excluded, while Marcus, Naomi, and Daniel lack preserved Key Flows.

Fix: Add a source-coverage table that maps every verbatim source journey to a flow or an approved non-UX/deferred disposition; add the missing load-bearing flows.

**[Component coverage] — Custom components lack a normative visual contract** (`ux-design-specification.md:1023-1053`; `ux-design-directions.html:15-18`)

Behavioral inputs, fail-closed rules, accessibility notes, and tests are detailed, but component anatomy, visual states, and token bindings are not normative.

Fix: Give every canonical custom component an identically named `DESIGN.md` Components entry and `EXPERIENCE.md` Component Patterns row; explicitly inherit unchanged Fluent components.

**[State coverage] — No IA-to-state closure can be proven** (`ux-design-specification.md:90-102,694-700,814-825,1040-1053`)

Surfaces are scattered across platform prose, layout decisions, component tables, and journeys; no single inventory shows reached-from paths or state ownership.

Fix: Add exact-name Information Architecture and State Patterns tables covering each surface and applicable empty/load/error/permission/focus states.

**[Inheritance discipline] — Eight declared inputs are unresolved** (`ux-design-specification.md:23-30,49-54`)

Both product briefs and all six research documents are absent at their declared paths, so machine consumers cannot resolve the provenance chain.

Fix: Preserve missing inputs through governed tombstones/digests and keep current `sources:` limited to resolvable authorities.

**[Inheritance discipline] — Current FrontComposer / Fluent UI V5 inheritance is underspecified** (`ux-design-specification.md:267-275,646-650,717-735`; `hexalith-ux-instructions.md:5-36`)

The legacy spec does not pin V5, map to Fluent 2 roles, or carry the repository's no-theme-redefinition and reuse-first rules.

Fix: Name FrontComposer + Blazor Fluent UI V5 in Foundation and express only behavioral and visual deltas through supported component parameters and Fluent 2 roles.

**[Inheritance discipline] — UX trust vocabulary is not mapped to current architecture vocabulary** (`ux-design-specification.md:329-336,1096-1102,1448-1458`; `epics.md:285-288`)

UX labels such as denied, restricted, possibly-stale, unknown, and conflicting have no explicit relationship to the canonical `ProjectionTrustState` values.

Fix: Add a source-owned state-to-presentation mapping that keeps canonical enum values distinct from derived display conditions.

**[Authority alignment] — The current bundle pins the map but not the detailed specification** (`ux-requirement-map.md:3,19-21`; `v9-authority-bundle-v1.json:247`; `epics.md:2586-2587`)

The v14 route identifies the current non-activation disposition, but the current specification bytes are unbound. The apparent V2 fallback is pending, V6-bound, and contains obsolete hashes.

Fix: Complete the planned Stories 8.1–8.2 disposition bundle, bind current hashes for both sources, validate the 52/28 inventories, and mark obsolete V2 artifacts unmistakably as provenance-only.

**[Downstream consumability] — Component inputs are concepts rather than exact schema bindings** (`ux-design-specification.md:243-247,999,1137,1208-1212`; `ConversationEvidenceTrustPostureV1.cs:16`; `ConversationCommandAvailabilityV1.cs:13`)

Names such as `TrustPosture`, `EvidenceTrustModel or equivalent`, and generic “projection metadata” do not specify DTOs, properties, nullability, cardinality, or authorization boundaries.

Fix: Add a component-contract manifest naming each exact DTO/property, transform owner, absence semantics, cardinality, authorization precondition, and output copy key.

**[Downstream consumability] — Canonical actors and requirement IDs do not close over the journeys** (`prd.md:421-433,703`; `ux-design-specification.md:849-952`)

Marcus and Daniel are absent, and the UX carries no Feature-FR/Feature-NFR or canonical journey identifiers, preventing mechanical need-to-surface traceability.

Fix: Publish a trace matrix from actor/journey and requirement ID to flow, surface, component/state, acceptance IDs, and disposition.

**[Downstream consumability] — Decision source anchors are not mechanically resolvable** (`ux-requirement-map.md:27,30-31,35,42`; `epics.md:2563-2567`)

The 52 IDs are complete, but informal `Source Section` labels are not stable path-plus-anchor references and do not expose dependencies.

Fix: Generate the planned disposition rows with exact source anchors/digests and typed links to requirements, components, flows, and acceptance IDs.

**[Downstream consumability] — Acceptance IDs are inventory-grade, not executable contracts** (`ux-requirement-map.md:82-118`; `ux-design-specification.md:1325-1330,1533-1537`)

The 28 identifiers preserve the denominator but omit precise Given/When/Then inputs, expected outputs, fixture links, test lanes, and evidence paths.

Fix: Publish machine-readable scenarios and an AC-to-fixture/test matrix with positive and fail-closed assertions.

**[Accessibility] — The normative floor remains WCAG 2.1 AA** (`ux-design-specification.md:742-752,1422-1424,1497-1500`; `ux-requirement-map.md:71-73`)

A compliant implementation could omit WCAG 2.2 Focus Not Obscured and Target Size Minimum checks that matter for sticky trust regions, drawers, and dense controls.

Fix: At future activation, rebaseline the PRD, map, spec, and automated checks to WCAG 2.2 AA; add explicit focus-obscuration and pointer-target tests while retaining the stronger 44×44 touch target.

**[Accessibility] — Blocked-action reasons lack a reachable interaction mechanism** (`ux-design-specification.md:744-747,1048-1050,1111-1112,1432-1434`; `ux-design-directions.html:1089-1097`)

The contract requires a safe reason but does not specify how keyboard and screen-reader users reach or associate it with a blocked control.

Fix: Commit a focusable `aria-disabled` or equivalent Command Gate pattern with programmatically associated visible reason text, activation prevention, and downgrade tests.

### Medium (13)

**[Flow coverage] — Existing journeys are diagrams, not canonical Key Flows** (`ux-design-specification.md:849-945`)

Four Mermaid flows model branches but lack numbered steps and an explicit climax beat.

Fix: Add numbered steps, explicit **Climax** markers, and separately labeled failure paths.

**[Component coverage] — Component names drift across artifacts** (`ux-design-specification.md:511-518,1027-1060`; `ux-requirement-map.md:45-46`)

Trust Banner/Posture Strip, Citation/Evidence Detail Drawer, and shortened map names prevent exact-name extraction.

Fix: Select canonical names and preserve retired terms only as explicit aliases.

**[State coverage] — Global shell failures are unspecified** (`ux-design-specification.md:1293-1299,1460-1468`)

Cold load, offline/network loss, session expiry, and shell-level failure behavior are missing despite strong domain-state coverage.

Fix: State applicability and define fail-closed presentation, retry/re-auth, retained context, announcement, and focus behavior.

**[Bloat] — Safety decisions have multiple plausible decision loci** (`ux-design-specification.md:114-125,361-406,1062-1119,1188-1235,1313-1346,1547-1566`)

Trust provenance, fail-closed behavior, leakage prevention, command recheck, and accessibility rules recur throughout the 1,566-line monolith.

Fix: Distill each decision once into compact spine tables and keep preservation history/test detail in the requirement map or upstream authority.

**[Authority alignment] — Missing source paths lack in-document tombstones** (`ux-design-specification.md:23-30,49-54`)

The provenance list supplies no deletion commit, historical blob, archive replacement, or disposition for eight absent inputs.

Fix: Record original path, last authoritative revision/blob, deletion commit, disposition, and replacement in the governed Story 8.1 output.

**[Authority alignment] — The workspace memlog contains obsolete authority claims** (`.memlog.md:9-10`; `publish_v9_planning_authority.py:1583`)

It claims a live `7bbd057f…` binding and V3 renderer, while the referenced V2 manifest is stale and the current renderer emits V4.

Fix: Append corrective entries through `memlog.py`, record the current V4 renderer, and keep the specification-unbound finding open until an accepted current bundle exists.

**[Downstream consumability] — The inherited UI system contract omits current rules** (`ux-design-specification.md:269,300,978`; `hexalith-ux-instructions.md:7,24,31,43`)

The set does not carry V5, legacy-token prohibitions, reuse-first behavior, or the FluentAccordion rule for multi-section page-like surfaces.

Fix: Bind the inherited system/version, map composites to exact standard components, specify accordion placement, and add token-conformance tests.

**[Downstream consumability] — “v1” design scope precedes an approved release slice** (`ux-design-specification.md:814-822,1237-1239,1273`; `prd.md:441-442,730`)

The preserved design includes command gates and governance mutation patterns while read-only versus export/mutation scope remains unresolved.

Fix: Require an activation-time capability matrix (`in`, `read-only`, `blocked`, `deferred`, `out`) before generating stories.

**[Downstream consumability] — Historical provenance and the claimed binding are unresolved** (`ux-design-specification.md:23-30`; `.memlog.md:9`; `preservation-traceability-manifest-v2.json:5-15,50-56`)

Eight paths are dead, while the memlog points to a pending V6 draft whose hashes are obsolete.

Fix: Publish governed tombstones, bind current source hashes in the planned disposition bundle, and correct the memlog additively.

**[Accessibility] — Live trust updates are underspecified** (`ux-design-specification.md:1432-1444,1515-1518`; `prd.md:700-703`)

The contract does not define which state transitions announce, their urgency, duplicate suppression, busy state, or focus behavior.

Fix: Add a transition matrix for freshness, completeness, redaction, command loss, permission downgrade, degradation/error, and rebuild completion with acceptance tests.

**[Accessibility] — Zoom and reflow requirements are not measurable** (`ux-design-specification.md:1399-1408,1435-1436,1516-1519`)

Breakpoints do not establish 200% text resize or 320 CSS-pixel/equivalent 400% zoom behavior.

Fix: Add WCAG 1.4.4/1.4.10 checks, including long localized strings, identifiers, sticky regions, drawers, gates, summaries, and focus visibility.

**[Accessibility] — Governance microcopy has no localization contract** (`ux-design-specification.md:1027-1034,1277-1283,1448-1458`)

Localized deployments could collapse distinct Redacted/Unavailable/Restricted meanings or diverge visible and accessible labels.

Fix: Declare English-only scope or require reason codes plus localizable keys, pseudo-localization, reflow, name-parity, language/direction, and semantic-distinction tests.

**[Accessibility] — Test coverage does not trace every trust workflow** (`prd.md:697-703`; `ux-design-specification.md:1277-1283,1497-1506`)

Named lanes for audit search, verification review, degraded/error recovery, and timed diagnostic scenarios are absent.

Fix: Trace Feature-NFR69–75 and UX-DR44–52 to automated, keyboard, screen-reader, zoom/reflow, forced-colors, reduced-motion, and manual evidence.

### Low (2)

**[Accessibility] — Temporal-cursor navigation lacks a keyboard/focus contract** (`ux-design-specification.md:296-313,347-359,742-746,1042-1045,1559-1562`)

A temporal jump can replace the timeline and lose focus or context even when ordinary navigation passes.

Fix: Specify keyboard input, busy/change announcement, and safe post-reconstruction focus; test it with virtualized timelines.

**[Accessibility] — The design-direction HTML is not an accessible reference implementation** (`ux-design-directions.html:15-18,299-318,823-853,1089-1097`)

The non-normative mock uses non-interactive divs, visual-only disabled styling, smooth scrolling without a reduced-motion override, and control sizes below the committed touch target.

Fix: Label it explicitly as visual direction only; if retained as executable HTML, add real semantics, keyboard behavior, reduced-motion support, narrow reflow, committed target sizes, and the blocked-action mechanism.

## Mechanical notes

- The map preserves **52 unique UX decision IDs** and **28 unique acceptance IDs** in exact order; its SHA-256 matches the current v14 bundle pin.
- The current specification SHA-256 is `948a5ac40a05fce510bffdd6818e3fcf3c871874b8779468954de57e452d8f18`; it does not match the stale V2 manifest's `7bbd057f…` value.
- No `imports/`, `mockups/`, or `wireframes/` files exist in the validation workspace. The linked HTML exploration is non-normative, names Direction 02 as chosen, and states conflict precedence.
- No `{path.to.token}` references exist. The principal name drift is Trust Banner/Posture Strip, Citation/Evidence Detail Drawer, shortened map component names, and Basic/Full Command Gate variants.
- The four Mermaid `flowchart TD` diagrams are mechanically plausible; their gap is canonical Key Flow shape, not syntax.
- Current repository authority says UX remains `preserved-not-activated`; this validation does not activate product UI work.

## Reviewer files

- `review-rubric.md`
- `review-authority-alignment.md`
- `review-downstream-consumability.md`
- `review-accessibility.md`
