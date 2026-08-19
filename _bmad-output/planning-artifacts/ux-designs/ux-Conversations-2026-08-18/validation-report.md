# Validation Report — Conversations

- **UX specification:** `_bmad-output/planning-artifacts/ux-design-specification.md` (legacy format — no DESIGN.md/EXPERIENCE.md spine pair exists)
- **UX requirement map:** `_bmad-output/planning-artifacts/ux-requirement-map.md`
- **Design directions:** `_bmad-output/planning-artifacts/ux-design-directions.html`
- **Run at:** 2026-08-18T23:38:38+02:00
- **Lenses:** rubric walker · authority alignment · downstream consumability · accessibility

## Overall verdict

The legacy preservation set is an unusually rigorous **behavioral and safety contract** — states, fail-closed rules, leakage discipline, and acceptance identifiers extract cleanly, and the 52-decision/28-AC inventory is enforced by a named validator (epics.md:2599, Story 8.2). It is, however, **not a visual contract**: not one color value or Fluent token binding is committed anywhere in the markdown — the only concrete palette lives unbound in ux-design-directions.html — and 8 of the 11 frontmatter inputDocuments no longer exist on disk. A downstream consumer can build the behavior faithfully today; any consumer asked to mirror the visual design must invent values, which for a trust-critical UI (redaction vs. warning vs. degraded) is exactly where invention is unsafe.

The extra lenses sharpen rather than overturn that picture. **Authority alignment is clean**: the requirement map is a managed companion the V9 publisher rebinds at every candidate rebind, and at HEAD it pins exactly what the newest V14 sidecar pins — no critical or high findings — though its currency is time-limited by approved-but-uncommitted amendments that guarantee an imminent candidate rebind. **Downstream consumability** confirms identity-level extraction is exact (28/28 ordered AC parity) but finds that four required fields of the Story 8.1 disposition row schema have no source anywhere in the set, and that the freshness vocabulary splits three ways between spec and architecture despite the one-shared-enum mandate. **Accessibility** finds an unusually deep floor for a planning artifact, held back by two structural gaps: the floor is pinned to WCAG 2.1 AA rather than 2.2 AA, and blocked governance actions have a committed outcome but no committed mechanism for exposing their reason to keyboard and screen-reader users.

## Category verdicts

- Flow coverage — adequate
- Token completeness — thin
- Component coverage — adequate
- State coverage — strong
- Visual reference coverage — adequate
- Bloat & overspecification — adequate
- Inheritance discipline — thin
- Shape fit — thin (vs. spine-pair target; judgment, not a demand)

Extra reviewers: authority alignment — **current and coherently pinned** · downstream consumability — **mechanically consumable, lossy addressing** · accessibility — **deep floor, two structural gaps**.

## Findings by severity

### Critical (1)

**[Token completeness]** — Zero concrete color commitments in the spec (spec:592–601, 719, 729–734)
All eight semantic roles are prose only; the five `--conversation-*` tokens have names but no values and no Fluent alias bindings. Redaction and degraded are novel roles with no Fluent equivalent — downstream code must invent exactly the treatments the spec declares load-bearing.
Fix: Bind each semantic role to a named Fluent UI Blazor design-token alias or a hex value; at minimum mint the redaction and degraded tokens.

### High (7)

**[Token completeness]** — No contrast pairs stated for load-bearing combinations (spec:742, 1424, 1499)
Contrast is committed only as blanket "WCAG 2.1 AA"; no foreground/background pair or ratio for trust-state chips, the redaction marker, or degraded/stale banners.
Fix: State the pairs and target ratios for every trust-state chip and banner.

**[Component coverage]** — No custom component has a per-component visual spec (spec:1027–1053)
No anatomy, sizing, color usage, or state appearance for any of the 20 components; visuals exist only in the unbound HTML mockups.
Fix: Per-component visual notes, or an explicit binding of each composite to a named region of the chosen direction's mockup.

**[Inheritance discipline]** — 8 of 11 inputDocuments do not resolve on disk (spec:20–31)
All six research inputs (deleted in commits 0664124 and 440fd19) and both product briefs are absent, with no tombstone or archive pointer — for a preservation artifact whose banner rests on provenance.
Fix: Annotate each missing entry with its disposition (deleted-at-commit / archived-at-path) or restore archive copies.

**[Downstream consumability]** — Freshness vocabulary split across the contract boundary (spec:1096–1102 vs architecture.md:977, 1129, 1211)
Spec taxonomy `current/possibly-stale/stale/unknown/conflicting` vs architecture's mandated single shared enum with two differing enumerations; the spec itself uses "rebuilding" as a visible state yet excludes it from its own taxonomy.
Fix: Record the canonical enum (or an explicit mapping) in the UX-DR22/UX-DR29 disposition rationale during Story 8.1 execution; spec bytes stay untouched.

**[Downstream consumability]** — Map columns cannot populate the Story 8.1 disposition row schema (epics.md:2567–2569; map:21–23)
`rationale`, `evidenceOrControl`, `compatibility`, and `disclosureSafety` have no declared source in any artifact — the 8.1 implementer must author them from scratch for 80 rows.
Fix: Add these as map columns (or a companion table) before hash binding, or declare constant/default values per field class in the 8.1 story contract.

**[Accessibility]** — Floor pinned to WCAG 2.1 AA, not 2.2 AA (spec:744, 1424, 1499; map UX-DR44)
2.2-only criteria are formally untested: Focus Not Obscured (2.4.11) is load-bearing given sticky trust bands and drawers; Target Size Minimum (2.5.8) applies while the 44px commitment is touch-conditional; Redundant Entry (3.3.7) touches governance forms.
Fix: Rebaseline all three statements and UX-DR44 to WCAG 2.2 AA; add a Focus Not Obscured obligation and an unconditional 24×24 minimum target size.

**[Accessibility]** — Blocked-action exposure: committed outcome, no committed mechanism (spec:747, 1050, 1112; html:732)
Disabled buttons are unfocusable and silent by default, so "reason text without requiring hover" is unreachable for keyboard/screen-reader operators; the mocks carry the reason as the disabled button's own label. An operator cannot discover why a governance action is blocked.
Fix: Blocked Command Gates stay focusable (`aria-disabled="true"`), reason associated via `aria-describedby`, adjacent visible reason text meets 4.5:1; add to the Command Gate row and accessibility test list.

### Medium (24)

**[Flow coverage]** — Marcus (SRE) has no journey (prd.md:421)
Audit-sink degradation, machine-readable verification, and privileged-action justification review have component states but no ordered flow.
Fix: Add a Marcus flow, or record an explicit disposition in the map.

**[Flow coverage]** — No flow exercises a governance mutation (prd.md:506–508; spec:972)
Redact/retention machinery is fully specified but never journeyed; the v1 read-only boundary is stated only obliquely.
Fix: Add the mutation flow or commit the read-only-v1 boundary as an explicit decision with a map row.

**[Token completeness]** — The only concrete palette sits orphaned in the directions HTML (html:10–30)
Never declared normative or illustrative — values a consumer can neither safely adopt nor safely ignore.
Fix: One sentence in the spec adopting or disclaiming the showcase palette.

**[Token completeness]** — HTML has no degraded token and reuses warning styling, contradicting the spec (html:739, 879 vs spec:588–600)
The prose contract and the only visual artifact contradict each other on degraded-vs-warning distinctness.
Fix: Mint the distinct degraded treatment and correct the showcase, or note the deviation.

**[Component coverage]** — Three names for the record-level trust summary (spec:514, 1045, 1265; map:60)
Trust Banner / Trust Posture Strip / trust summary band — an undocumented rename.
Fix: One canonical name plus an alias note.

**[Visual reference coverage]** — The HTML read standalone is ambiguous about the winner (html:680–682)
Its comparison card recommends two directions and carries no marker that 02 Split Investigation Lens won.
Fix: Add a "chosen: 02" banner pointing at the spec's Design Direction Decision.

**[Visual reference coverage]** — The Design Direction Decision has no UX-DR row (map; epics.md:2599)
The biggest layout commitment is absent from the validator-frozen 52-row inventory.
Fix: Add UX-DR53 under a new authority version, or record in the map preamble that it is intentionally preserved via UX-DR18.

**[Bloat]** — Duplicate "Core User Experience" section pair (spec:76, 410)
Overlapping but non-identical "Defining Experience" children make heading-based extraction ambiguous.
Fix: Merge, or rename the first with a provenance note.

**[Bloat]** — Leakage-surface enumeration restated ~6 times with varying membership (spec:755, 1201, 1231, 1326, 1338, 1432)
A consumer cannot tell which enumeration is the canonical test surface.
Fix: One canonical list; every other statement references it.

**[Inheritance discipline]** — Date anachronism in the provenance chain (spec frontmatter)
`completedAt: 2026-05-13` precedes the declared canonical input `prd-Conversations-2026-06-02`; the rebinding has no frontmatter date.
Fix: Add a `reboundAt`/note field distinguishing original inputs from rebound authority.

**[Inheritance discipline]** — Authority-version skew between spec (v1) and map (v3) (spec:32; map:2)
No supersession statement; a consumer checking "same preservation regime?" gets two answers. (The authority lens judges this stale-by-design; the ergonomic gap stands.)
Fix: Stamp both artifacts with the current version or add a supersession line to the map.

**[Inheritance discipline]** — UX-DR Source Section labels resolve only by interpretation (map; e.g. UX-DR3 → spec:124)
~10+ labels match no literal spec heading; all 52 resolve to real content, but not mechanically.
Fix: Use literal headings or add anchors.

**[Shape fit]** — ux-design-directions.html carries no preservation-authority banner (vs spec:44–47, map:13–17)
The only artifact of the three a viewer could mistake for live design work.
Fix: Add the banner as a visible note plus an HTML comment.

**[Authority alignment]** — Currency is time-limited by pending approved amendments (A3 proposal worktree +23/−3; publish_v9_planning_authority.py:266)
The uncommitted CP-1..CP-3 amendments sit in a `CANONICAL_PATHS` file, guaranteeing an imminent candidate rebind after which `151f965` must be re-read, not cached.
Fix: Consumers re-read the map/V14 pin at use time; when the amendment commits, add the readiness proposal to `CANONICAL_PATHS`.

**[Downstream consumability]** — Non-resolving and ambiguous Source Section labels (map UX-DR2, DR28, DR51)
UX-DR2 spans two same-named H2s plus a third section; UX-DR28 matches two distinct fixture lists; UX-DR51 appears in two sections.
Fix: Normalize Source Section values to exact heading text before Story 8.1 freezes the source hashes.

**[Downstream consumability]** — Generated-vs-custom boundary for search stated both ways (spec:340–341 vs 825, 1042–1043)
"Generated-first: search and filtering" versus custom Find Pane and Trust Preview Result Row composites.
Fix: Record the intended split in the UX-DR11/DR12/DR18 disposition rationale.

**[Downstream consumability]** — Two overlapping trust contract models, neither committed (spec:247–251 vs 999, 1137)
TrustPosture/EvidenceItem/CommandAvailability versus "EvidenceTrustModel or equivalent"; architecture names neither.
Fix: Name the binding model in the UX-DR19 rationale, or record it as an explicitly open architecture decision with an owner.

**[Downstream consumability]** — Unnumbered acceptance sets sit outside the 28-ID denominator (spec:361–369, 1109–1119)
18 testable obligations with no identifiers survive only via the whole-file hash; inventory-keyed consumers will not surface them.
Fix: A map note declaring which UX-DR rows carry them, or identifiers in a future authorized revision.

**[Downstream consumability]** — Record-level trust summary has four unreconciled names (spec:225, 514, 818/1045, 1265; map:60)
The merged-components list reconciles four other components but not these.
Fix: One reconciliation sentence in the UX-DR32 disposition rationale.

**[Downstream consumability]** — Normative force unmarked: 124 "should" vs 101 "must" (e.g. spec:755)
Obligation extraction cannot distinguish binding from advisory.
Fix: A one-line convention in the map — non-activating and cheap before freeze.

**[Accessibility]** — Live-update announcement is intent-level (spec:1434)
One "should" sentence covers all state-change classes; a screen-reader operator can keep relying on a view that silently went stale.
Fix: Per-class politeness levels, announcement copy pattern, and stale-transition/redaction-replacement acceptance criteria.

**[Accessibility]** — Contrast targets declared but never bound to values (spec:729–734, 744)
The only concrete values live in the non-normative mocks, where verified (4.74:1) and warning (4.78:1) pass with almost no margin; token drift would silently break AA.
Fix: Bind minimum ratios and reference values to each `--conversation-status-*` token; add a token-level automated contrast test.

**[Accessibility]** — No localization commitment for regulated microcopy (spec:833, 1030, 1449–1456)
Redacted ≠ Unavailable ≠ Restricted is defined only as English strings; translation could collapse governance-meaningful distinctions.
Fix: Reason codes + localizable catalog keys, with a translation check that the five state words remain pairwise distinct per locale.

**[Accessibility]** — Fluent inheritance is category-level; rc.2 risk unacknowledged (spec:978–989)
No component/variant named; "accessibility foundations from Fluent UI" is an unvalidated assumption at 5.0.0-rc.2 quality.
Fix: Component inventory mapping each generated surface to the named Fluent component, plus a one-time keyboard + screen-reader conformance smoke.

### Low (19)

**[Flow coverage]** — Daniel and Naomi have no flows and no disposition line (prd.md:424–425). Fix: one disposition sentence each.

**[Token completeness]** — Monospace usage rule names no face or token (spec:657). Fix: name the face or Fluent typography token.

**[Component coverage]** — The map abbreviates component names ("SafeReason", "Trust Preview", "Governed Header"; map:45–46). Fix: align to exact spec names.

**[State coverage]** — No offline/disconnected or session-expiry state named (spec:1307–1311). Fix: one row assigning them to an existing state class.

**[Visual reference coverage]** — The requirement map never references ux-design-directions.html. Fix: reference it from the map preamble as exploration provenance.

**[Bloat]** — "Render trust, never infer" restated ~9 times (spec:245…1329). Fix: collapse on migration.

**[Bloat]** — Emotional-response and inspiring-products narrative beyond the load-bearing residue (spec:127–157, 161–209). Fix: trim on migration.

**[Inheritance discipline]** — No Feature-FR/NFR identifier appears anywhere in the spec (e.g. spec:967). Fix (optional): a Feature-FR ↔ UX-DR crosswalk.

**[Shape fit]** — Migration sequencing: mint DESIGN.md tokens first; the behavioral material ports nearly verbatim; the AC identifiers and 52-row map must survive unchanged or under a new validator-blessed authority version.

**[Authority alignment]** — A3 proposal internally inconsistent: §2 "regenerates byte-identically" vs §5 regeneration that changed the `planningCandidate` line at `29c56fa`. Fix: restate precisely in the forthcoming amendment commit; do not rewrite committed text.

**[Authority alignment]** — The spec is not byte-protected by the candidate gate; source-hash binding deferred to Story 8.1 behind the hold (disclosed in map:21–23). Fix: optionally add the spec to the protected set at the next rebind.

**[Authority alignment]** — V8-era prose in epics.md:2116–2121 still names Story 6.4 as disposition owner (superseded by Epic 8; epics.md is byte-frozen). Fix: rely on the map's provenance note; retire the sentence if epics.md is ever reissued.

**[Downstream consumability]** — Map-coined names the spec never uses; UX-DR17 lists 4 of 8 primitives (map UX-DR17/DR18/DR30). Fix: align summaries; name all eight primitives.

**[Downstream consumability]** — Two canonical loop names at AC level ("Find → Read → Trust" vs "Find → Open → Verify → Cite, Act, or Stop"). Fix: equivalence note in the UX-DR25 rationale.

**[Downstream consumability]** — ux-design-directions.html sits outside the preservation contract (AC-8.1-02 fixes sources to spec + map); its drift is invisible to the 8.2 validator. Fix: annotate it as non-canonical exploration provenance; optionally add it to the source inventory via authorized change.

**[Downstream consumability]** — Authority-stamp skew traceable only via proposals (spec v1, map v3, 8.1 contract v10, v13/v14 sidecars). Fix: Story 8.1 disposition restates the full chain; map note that its stamp supersedes the spec's.

**[Accessibility]** — Mock component boundaries fail 3:1 non-text contrast (#d4dbe4 on white = 1.40:1; html:231–242). Fix: declare mock borders non-normative; require 3:1 for inputs and interactive rows.

**[Accessibility]** — Mocks label redactions with the bare code "R2" (html:848, 988, 1046), conflicting with spec:1249. Fix: visible "Redacted" word plus code; accessible name reads the full state.

**[Accessibility]** — Temporal-cursor navigation absent from every keyboard-access enumeration (spec:313, 819, 1044 vs 745, 970, 1561). Fix: add it to the keyboard list; specify focus behavior after a cursor jump.

## Reviewer files

- `review-rubric.md`
- `review-authority-alignment.md`
- `review-downstream-consumability.md`
- `review-accessibility.md`
