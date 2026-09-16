<!-- validation-run: 2026-09-16 current-run -->
# PRD Quality Review — Conversations Boilerplate Reduction

## Overall verdict

This is a strategically coherent, unusually well-bounded refactor PRD: its thesis is specific, its scope cuts are explicit, and most requirements carry concrete consequences plus preservation counter-metrics. The current artifact is nevertheless only **fair** as a decision instrument because the active implementation hold, executed-phase claim, unresolved architecture dependency, and provisional acceptance evidence are not reconciled into one actionable current disposition; a decision-maker cannot tell from the PRD alone what decision is required or what remains before the pilot can be accepted.

## Decision-readiness — thin

The intended product decision is clear in §2: Conversations is the pilot, FR-16 and fleet migration are deferred, contract preservation is inviolable, and promotion landing zones are delegated to architecture. The PRD also names real trade-offs instead of claiming a free win: additive shared APIs over fleet migration, behavior preservation over maximal LOC removal, and a limited pilot over extracting every plausible abstraction (§§5, 7, 9, 11–12).

The weakness is the *current* decision boundary. The readiness snapshot says the “implementation hold is ACTIVE pending an independent decision” but does not summarize the question, choices, owner, evidence required, or unblock condition. It also says “Phases 0–3 ... have executed,” while OQ-1 still must be resolved “before the corresponding implementation story starts,” SM-1 is explicitly “not a target-pass claim,” SM-2 remains “provisional,” and FR-20 cites a traceability artifact whose status is “draft / pending-prerequisites.” Those statements can all be historically true, but they do not add up to an actionable accept/reject/resume state inside this PRD.

### Findings

- **high** Active hold is not an actionable decision (§2, “implementation hold is ACTIVE pending an independent decision”) — The decisive current blocker is outsourced to `epic-6-completion-supersession-current-proof-v1.md`; the PRD gives no synopsis of the decision, named decision owner, available outcomes, or evidence that releases the hold. A decision-maker must leave the PRD before knowing what they are being asked to decide. *Fix:* Add a compact hold row that states the exact decision question, accountable owner, permitted outcomes, evidence inputs, and explicit resume/stop condition, while retaining the evidence file as detail.
- **medium** Execution and acceptance states are not reconciled (§§2, 5.3, 7, 12; FR-20) — “Phases 0–3 have executed” sits beside an unresolved pre-story landing-zone dependency, a directional-only SM-1 result, provisional SM-2 evidence, and a draft preservation traceability artifact. The reader cannot distinguish work performed from requirements accepted. *Fix:* Replace the phase-level completion shorthand with a compact current-state summary for the in-scope FR groups and SM-C gates using `performed`, `evidenced`, `accepted`, `held`, or `not accepted`, and explain how OQ-1 was handled for already-started stories.

## Substance over theater — strong

The content is earned. The Vision uses a frozen 35,769-line source baseline and a governed 13,289-line plumbing denominator; features name the actual consume/promote/keep boundaries; the addendum ties them to observed library seams and duplicated patterns. The developer journeys are few, named, and directly drive requirements rather than serving as persona decoration. NFRs and counter-metrics are product-specific—tenant fail-closed behavior, manifest-denominator preservation, bounded telemetry cardinality, replay safety, and a defined P95 regression envelope—rather than generic “secure/scalable/reliable” furniture.

Section 14 is exceptionally large, but it is not presented as invented novelty or current delivery scope. Its authority and disposition language repeatedly distinguishes preserved constraints from implemented, scheduled, or release-activated work, so the bulk has a defensible preservation purpose.

### Findings

No substantive findings.

## Strategic coherence — strong

The PRD has a clear thesis: remove domain-agnostic authoring cost from Conversations and make the result reusable without changing observable behavior. The inventory, consume/promote/adopt feature sequence (§6), primary metrics (SM-1 and SM-2), consolidation metric (SM-3), and preservation/performance counter-metrics (SM-C1 and SM-C2) all test that thesis. The MVP is a platform-pilot scope, not a convenience backlog: fleet migration, speculative promotions, metadata redesign, and new domain behavior are explicitly excluded.

The initiative also resists an easy but incoherent success claim. LOC reduction cannot be purchased by reducing the test denominator, and performance cannot be assumed from an in-process benchmark to cover cross-process calls. Those constraints materially strengthen the strategy.

### Findings

No substantive findings.

## Done-ness clarity — adequate

FR-1 through FR-20 generally include at least one testable consequence, and the central acceptance mechanisms are unusually explicit: frozen baselines, exact percentages, named evidence artifacts, closed conformance categories, approval discipline, and reproducibility fields. The boundary among FR-3, FR-10, and FR-13 is particularly useful for preventing duplicate stories.

Some “preserve existing behavior” requirements still lack a bound acceptance reference at the point where a story author needs it. FR-10 requires “existing health, telemetry, resilience, and discovery behavior” to remain observable; FR-12 requires “contract-compatible behavior”; FR-15 preserves “established metric names and cardinality.” FR-20 freezes public/adopter contracts and a conformance denominator, but the PRD does not state that these operational/client/telemetry specifics are all included in those baselines. The exact done condition is therefore strong at initiative level but uneven for those story slices.

### Findings

- **medium** Several preservation consequences have no explicit frozen oracle (§6.3, FR-10, FR-12, FR-15) — Phrases such as “existing ... behavior remains observable,” “contract-compatible behavior,” and “established metric names and cardinality are preserved” are testable only if the exact pre-change behaviors, errors, names, dimensions, and bounds are identified. The cited FR-20 manifest is not expressly declared to cover all of them. *Fix:* Bind each consequence to a named baseline or test/evidence identifier, or explicitly add it to the FR-20 preservation package with its comparison rule.

## Scope honesty — strong

The PRD is forthright about exclusions, follow-ons, assumptions, and unresolved product history. §5.2 names fleet migration, feature work, transport/provider changes, speculative promotions, FR-16, and UI redesign as non-goals. §12 assigns the one live architecture dependency; §14.1 and §14.4 repeatedly warn that “preserved” does not mean implemented or scheduled. The eight inline `[ASSUMPTION]` tags round-trip through §13 with owners and revisit triggers, including the risky greenfield/no-external-production premise before further test or public-type removal.

Open-item density is high only in the preserved legacy contract, where the text clearly labels those items as release/product dispositions outside this refactor. The refactor scope itself does not silently absorb them.

### Findings

No substantive findings.

## Downstream usability — adequate

The core requirement IDs are unique and contiguous (FR-1 through FR-20, including the explicitly deferred FR-16); preserved product IDs are separately namespaced and contiguous (Feature-FR1 through Feature-FR104 and Feature-NFR1 through Feature-NFR77). UJ-1 through UJ-3 each have a named protagonist and direct FR mappings. The glossary, explicit crosswalks, section-local technical links, and the separation of technical-how into `addendum.md` support extraction into architecture and stories.

The artifact is harder to source-extract than its mechanical hygiene suggests because downstream readers must distinguish refactor requirements from a very large preserved contract library and then join several repo-root evidence artifacts to know current acceptance state. The namespace and disposition language make that workable, but the unreconciled readiness state identified above prevents a `strong` rating.

### Findings

No additional findings beyond Decision-readiness.

## Shape fit — adequate

The primary shape fits an internal developer-platform refactor: capability groups and consequences are load-bearing; journeys are deliberately light; technical API mappings are moved to the addendum; operational and compatibility concerns receive more weight than end-user UX. This is the right bias for a chain-top document feeding architecture and stories.

Embedding the 181-item preserved product-contract baseline nearly doubles the document and creates a dual-purpose artifact, but the PRD explains why it is normative and rigorously prevents it from masquerading as current refactor scope. That makes the shape heavy rather than fundamentally mismatched.

### Findings

No substantive findings.

## Mechanical notes

- Primary IDs are contiguous and unique: FR-1..FR-20; UJ-1..UJ-3; preserved Feature-FR1..Feature-FR104; preserved Feature-NFR1..Feature-NFR77.
- Every UJ has a named protagonist (Nadia, Sam, Priya), and local Markdown file targets checked here exist.
- All eight inline `[ASSUMPTION]` callouts round-trip through §13; the combined §4/§9 and §9 assumption-index rows preserve both source meanings.
- The addendum opening says its “Figures are first-pass and approximate — confirm during architecture,” while §A declares the 13,289-LOC baseline authoritative and accepted and §C separately labels only the old table as superseded provenance. Narrow the opening disclaimer to the historical Discovery estimates and candidate mappings so it does not cast doubt on the accepted baseline.
