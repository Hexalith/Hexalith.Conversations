# Spine Pair Review — Conversations

## Overall verdict

**Broken as a downstream spine-pair contract, despite unusually strong preserved safety and state material.** The legacy set records a coherent investigation model, rich fail-closed behavior, and an explicitly subordinate visual exploration, but it does not provide the machine-readable `DESIGN.md` / `EXPERIENCE.md` pair, design tokens, source-to-flow closure, or current inherited-system mappings that architecture and story-development consumers need. Because current authority keeps UX `preserved-not-activated`, these are blockers to any future activation, not authorization to implement the preserved screens (`ux-requirement-map.md:14-23`; `_bmad-output/planning-artifacts/epics.md:296-298`).

## 1. Flow coverage — broken

Checked every named journey in the two extant frontmatter sources against the four legacy journey diagrams. The UX set covers Sarah, Maya and Atlas, Diego, and Julian and Helen, with unhappy-path branches, but does not close the complete source inventory (`ux-design-specification.md:20-31, 847-952`; `prds/prd-Conversations-2026-06-02/prd.md:50-58, 421-433`).

### Findings

- **High** Source-to-flow coverage is incomplete and not dispositioned. None of the current-initiative journeys `UJ-1` Nadia, `UJ-2` Sam, or `UJ-3` Priya is extracted or explicitly excluded as non-UI, and three of the nine preserved acceptance journeys—Marcus's audit-sink degradation, Naomi's cross-product lifecycle boundary, and Daniel's post-harm recovery—have no Key Flow. The requirement map inventories UX decisions and UX acceptance IDs but does not map these source journeys or source requirements to flows (`prds/prd-Conversations-2026-06-02/prd.md:54-58, 425-433`; `ux-requirement-map.md:25-27, 82-90`; `ux-design-specification.md:849-952`). *Fix:* In a future approved Update, add a source-coverage table that preserves each source name verbatim and either links it to a Key Flow or records a reasoned non-UX / out-of-scope disposition; add the three missing load-bearing product flows.
- **Medium** The four represented journeys are Mermaid graphs rather than numbered Key Flows and do not identify an explicit climax beat. Their decision branches do model failure paths, but a downstream extractor cannot reliably distinguish the successful climax from an intermediate outcome (`ux-design-specification.md:849-870, 880-897, 906-922, 931-945`). *Fix:* Add numbered steps, an explicit **Climax** step, and a separately labeled failure path for each retained flow; the diagrams may remain as supplements.

## 2. Token completeness — broken

Checked the legacy YAML frontmatter, all named semantic colors and spacing/type guidance, the five example domain CSS variables, and every `{path.to.token}` reference. No design-token frontmatter or brace references exist (`ux-design-specification.md:1-35, 574-735`).

### Findings

- **Critical** Load-bearing visual roles have names but no normative values or inherited Fluent 2 token mappings. The document requires visually distinct current, stale, denied, degraded, redacted, unavailable, and blocked states, then offers five undefined `--conversation-*` examples; the only hex values are in an HTML exploration explicitly declared illustrative and non-normative (`ux-design-specification.md:576-601, 717-735`; `ux-design-directions.html:15-18`). This leaves downstream code unable to reproduce state differentiation, including redaction and degraded-state contrast. *Fix:* Under separately approved release authority, create `DESIGN.md` frontmatter that inherits FrontComposer / Blazor Fluent UI V5, maps every domain role to a named Fluent 2 role or component parameter, defines only genuine brand deltas in spec-valid form, and records light/dark and contrast obligations for every load-bearing combination.

## 3. Component coverage — broken

Extracted the named trust primitives, investigation composites, merged component decisions, generated/Fluent foundation components, and component aliases. The legacy tables are behaviorally detailed, but there is no peer visual component contract (`ux-design-specification.md:974-1060`).

### Findings

- **High** The custom component inventory supplies inputs, forbidden inputs, states, fail-closed behavior, accessibility, and tests, but not normative visual anatomy or component-token entries. The visual-direction HTML cannot fill that role because it is explicitly illustrative (`ux-design-specification.md:1023-1053`; `ux-design-directions.html:15-18`). *Fix:* Give every canonical custom component an identically named `DESIGN.md` Components entry with visual anatomy/state appearance and an `EXPERIENCE.md` Component Patterns row with behavior; explicitly inherit unchanged Fluent components.
- **Medium** Component names are not stable across the set: `Trust Banner` becomes `Trust Posture Strip`; `Citation Drawer` becomes the shared `Evidence Detail Drawer`; the map shortens `SafeReasonInline` / `SafeReasonDetail` to `SafeReason`, and shortens `Tenant-scoped Find Pane`, `Trust Preview Result Row`, `Governed Record Header`, and `Evidence Timeline Entry` (`ux-design-specification.md:511-518, 1027-1053, 1055-1060`; `ux-requirement-map.md:45-46`). *Fix:* Select one canonical name per component, use it verbatim in both spines and the requirement map, and record retired names as explicit aliases only where preservation requires them.

## 4. State coverage — thin

Walked the implied Find, result-list, selected-record, evidence-timeline, detail-drawer, command, forensic-review, acceptance-review, responsive, adopter, and developer surfaces against empty, load, focus, error, denial, degradation, and transition behavior. The set has strong local state semantics and conservative trust precedence, but lacks a closed IA-to-state matrix (`ux-design-specification.md:814-825, 1040-1053, 1188-1251, 1293-1311`).

### Findings

- **High** There is no explicit Information Architecture surface inventory, so mechanical state closure is impossible. Surfaces are introduced in platform prose, the chosen-direction layout, component tables, and journeys, but no single table proves that each stated need lands on a surface and each surface owns its empty/load/error/permission/focus states (`ux-design-specification.md:90-102, 694-700, 814-825, 1040-1053`). *Fix:* Add `EXPERIENCE.md` Information Architecture and State Patterns tables keyed by the same exact surface names, including ownership and reached-from paths.
- **Medium** Global cold-load, network/offline loss, authentication/session expiry, and shell-level failure behavior are unspecified. The document thoroughly handles projection, permission, redaction, and partial-load failures, but none of those global conditions appears in the set (`ux-design-specification.md:1293-1299, 1460-1468`). *Fix:* State whether each global condition applies; where it does, define fail-closed presentation, retry/re-auth behavior, safe retained context, and announcement/focus behavior.

## 5. Visual reference coverage — strong

Checked `mockups/`, `wireframes/`, and `imports/`: none exists and there are no eligible files to orphan. The auxiliary `ux-design-directions.html` is named at the relevant design-decision section, identifies Direction 02 as chosen, and states both conflict precedence and the non-normative status of its colors/borders/state styling (`ux-design-specification.md:781-825`; `ux-design-directions.html:3-18, 676-679, 810-821`).

### Findings

- None.

## 6. Bloat & overspecification — thin

Checked for upstream restatement, repeated decision loci, decorative narrative, pixel specifications that should be tokens, and material no downstream consumer needs. The content is relevant, but the 1,566-line legacy specification repeatedly restates the same trust and safety decisions.

### Findings

- **Medium** Trust provenance, fail-closed behavior, leakage prevention, command recheck, and accessibility rules recur across experience principles, failure guardrails, component implementation, consistency patterns, acceptance criteria, quality gates, and implementation guidelines (`ux-design-specification.md:114-125, 361-406, 1062-1119, 1188-1235, 1313-1346, 1547-1566`). This creates multiple plausible decision loci and raises drift risk even where the repeated rules currently agree. *Fix:* Distill each decision once into a compact spine table, inherit requirement text by source reference, and leave preservation history and detailed test obligations in the requirement map or upstream authority rather than restating them in the spines.

## 7. Inheritance discipline — broken

Resolved every `inputDocuments` entry, compared journey/component/state vocabulary with current repository authority, and checked the named UI-system inheritance. Three sources resolve (PRD, addendum, project context); eight do not.

### Findings

- **High** Eight frontmatter inputs are broken: both product briefs and all six research documents no longer exist at the declared paths (`ux-design-specification.md:23-30`). The prose labels this metadata historical and narrows current initiative authority to the PRD/addendum, but machine consumers still encounter unresolved declared inputs (`ux-design-specification.md:49-54`). *Fix:* Preserve provenance with explicit tombstones/digests or move historical inputs to a non-source provenance field; keep `sources:` limited to resolvable current authorities.
- **High** UI-system inheritance is underspecified against current repository authority. The UX set says `FrontComposer and Fluent UI Blazor`, offers custom domain CSS-variable examples, and never pins V5 or maps its roles to Fluent 2; current Hexalith authority requires FrontComposer plus **Blazor Fluent UI V5**, existing components first, and Fluent 2 roles/component parameters without theme redefinition (`ux-design-specification.md:267-275, 646-650, 717-735`; `references/Hexalith.AI.Tools/hexalith-ux-instructions.md:5-36`). *Fix:* Name the inherited system/version in Foundation, express only behavioral deltas in `EXPERIENCE.md`, and make `DESIGN.md` reference V5 component parameters / Fluent 2 roles rather than minting an unbound theme layer.
- **High** Trust-state vocabulary has no explicit mapping to the current normative architecture vocabulary. The UX set uses `denied`, `restricted`, `degraded`, `possibly-stale`, `unknown`, and `conflicting`, while current authority names `ProjectionTrustState` as `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted` (`ux-design-specification.md:329-336, 1096-1102, 1448-1458`; `_bmad-output/planning-artifacts/epics.md:285-288`). These may be valid presentation states, but without a mapping a consumer can accidentally treat UX copy as a second domain enum. *Fix:* Add a source-owned state-to-presentation mapping that preserves the canonical enum names and distinguishes derived display conditions from contract states.

## 8. Shape fit — broken

Compared the legacy set with the canonical DESIGN spine order and required EXPERIENCE sections, including triggered Inspiration and Responsive sections. Relevant material exists, but it is interleaved in one legacy specification rather than expressed as two peer contracts.

### Findings

- **Critical** Neither `DESIGN.md` nor `EXPERIENCE.md` exists. Consequently there is no Google Labs-compatible design frontmatter; no canonical Brand & Style → Colors → Typography → Layout & Spacing → Elevation & Depth → Shapes → Components → Do's and Don'ts body; and no extractable Foundation, Information Architecture, Voice and Tone, Component Patterns, State Patterns, Interaction Primitives, Accessibility Floor, and Key Flows spine (`ux-design-specification.md:1-58, 265-410, 574-779, 847-1566`). *Fix:* Only after approved authority permits an Update, distill—not rewrite—the preserved decisions into the two peer files, retain `currentDisposition: preserved-not-activated`, link this legacy set as provenance, and make the spines explicitly win on conflict with visual references.

## Mechanical notes

- **Frontmatter:** `ux-design-specification.md` has workflow/provenance metadata, not DESIGN token frontmatter; `ux-requirement-map.md` has explicit authority/disposition metadata and preserves 52 UX decisions plus 28 UX acceptance IDs (`ux-requirement-map.md:1-9, 25-118`).
- **References:** Eight of eleven `inputDocuments` entries are unresolved. The requirement map's `source` resolves. The design-directions artifact resolves from the specification and declares the specification authoritative.
- **Token references:** No `{path.to.token}` references exist anywhere in the three-artifact set. The five `--conversation-*` identifiers are examples, not defined normative tokens (`ux-design-specification.md:728-735`).
- **Name consistency:** The principal drift is `Trust Banner` / `Trust Posture Strip`, `Citation Drawer` / `Evidence Detail Drawer`, shortened map names, and `Basic` / `Full` variants of `Command Gate` without a canonical naming rule.
- **Visual inventory:** No `imports/`, `mockups/`, or `wireframes/` files exist in the validation workspace. `ux-design-directions.html` is a linked auxiliary exploration, not an orphan and not a normative token source.
- **Mermaid:** Four fenced `flowchart TD` diagrams are balanced and mechanically plausible (`ux-design-specification.md:853-870, 884-897, 910-922, 935-945`). Their structural deficiency is rubric shape—no numbered steps or explicit climax—not apparent Mermaid syntax failure.
- **Authority guardrail:** Current repository authority says UX remains `preserved-not-activated`; this review does not activate product UI work (`_bmad-output/planning-artifacts/epics.md:296-298`).
