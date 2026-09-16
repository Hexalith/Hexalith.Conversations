---
name: Conversations UX validation reconciliation
status: draft
updated: 2026-09-16
sources:
  - validation-report.md
  - review-rubric.md
  - review-authority-alignment.md
  - review-downstream-consumability.md
  - review-accessibility.md
---

# UX Validation Reconciliation

## Outcome

The current validation was distilled into draft `DESIGN.md` and `EXPERIENCE.md` without editing the preserved legacy specification, generated requirement map, or design-direction HTML. The spines retain `preserved-not-activated`; Architecture V15 remains final with the implementation hold active.

## Incorporated

| Validation concern | Reconciliation |
|---|---|
| Missing spine pair | Created draft peer spines with canonical DESIGN and EXPERIENCE shapes. |
| FrontComposer / Fluent inheritance incomplete | Bound Foundation to FrontComposer + Blazor Fluent UI V5 and current reuse/no-theme-redefinition/accordion rules. |
| No IA-to-state closure | Added surface/journey closure and surface-state matrices. |
| Component names drift | Chose the 20 exact names from the legacy Custom Components tables and mirrored them in both spines. Legacy aliases were not carried as active names. |
| Source-owned trust vocabulary incomplete | Added final Architecture V15 mappings for `Current/current`, `Rebuilding/rebuilding`, `Rebuilding/gap_detected`, `Unavailable/unavailable`, `Unavailable/metadata_contradictory`, and `Forbidden/forbidden`; also preserved public `Stale` and `Redacted` combinations from current contracts. |
| Freshness time ambiguity | Defined `LastAppliedEventTimestamp` as replay/event-time anchor and `ProjectionGeneratedAt` as actual UTC generation-completion time, per V15. |
| Flow shape | Converted preserved product journeys into numbered, named-protagonist flows with explicit climax and failure paths. |
| Refactor/product actor ambiguity | Explicitly excluded PRD UJ-1 Nadia, UJ-2 Sam, and UJ-3 Priya from product UX while preserving all nine product actors separately. |
| Visual reference ambiguity | Linked the HTML as non-normative provenance; the spines win on visual-artifact conflict. |
| Repeated invariants and bloat | Consolidated trust, leakage, command, copy/export, responsive, and accessibility rules into one decision locus each. |
| Stale memlog authority claims | Followed the additive correction: the V4 map is pinned; the specification is preservation-controlled but not bound by the stale V2 draft. |

## Explicitly Deferred

No validation finding below was silently resolved by invention:

- exact custom color values or Fluent 2 trust-role mappings;
- WCAG 2.2 promotion;
- Fluent UI V5 prerelease/stable policy;
- exact blocked-action accessibility mechanism;
- full public reason-code-to-display mapping beyond accepted V15/current-contract combinations;
- release capability slicing for export or governance mutation;
- global offline and session-expiry behavior;
- localization policy;
- live-region urgency/de-duplication, measurable zoom/reflow, temporal-cursor focus, and assistive-technology matrix;
- accepted current binding/tombstones for the legacy specification and eight missing historical inputs.

Current PRD validation findings are remediation proposals, not accepted product requirements. They were used to preserve the hold, separate refactor actors from product actors, and avoid treating §14 as activation authority.

## Qualitative Ideas Not Promoted

- Illustrative HTML hex values, borders, shadows, radii, and control semantics were not promoted.
- The HTML’s degraded-as-warning styling was rejected because the preserved specification requires distinct treatment.
- Repeated emotional/inspiration prose was reduced to the load-bearing Quiet Evidence UI posture, inspirations, and anti-patterns.
- Missing historical briefs and research files were not listed as resolvable spine sources; governed tombstones remain required.
- Legacy component aliases (`Trust Banner`, `Citation Drawer`, shortened map names, Basic/Full variants) were not retained as canonical names.

## Proactive Rubric Pass 1

| Check | Result | Remaining gap |
|---|---|---|
| Flow coverage | Adequate | All named product actors are represented and all PRD refactor UJs are explicitly excluded from product UX. Activation still needs an approved capability matrix and source-to-requirement trace artifact. |
| Token completeness | Thin by authority | Every defined spacing/component token is present; no brace reference is unresolved. `colors: {}` and `rounded: {}` are intentional because exact deltas are unapproved. This remains activation-blocking. |
| Component coverage | Strong | All 20 canonical custom names appear in both `DESIGN.md.Components` and `EXPERIENCE.md.Component Patterns`. |
| State coverage | Adequate | Every IA surface has states; global offline/network and session-expiry behavior remain explicit open decisions. |
| Visual reference coverage | Strong | No `imports/`, `mockups/`, or `wireframes/` exist. The one external HTML direction artifact is linked, scoped, and subordinate. |

## Unresolved Activation Gaps

The spine pair is suitable as a draft preservation distillate, not an implementation contract. Activation remains blocked by the open decisions above, the current authority/binding gap, and the unresolved PRD release-state findings. No reviewer file was rewritten and neither spine was marked final.

