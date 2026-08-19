# Validation Report — Conversations Boilerplate Reduction

- **PRD:** `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md`
- **Rubric:** `.claude/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-08-18T22:51:13+02:00
- **Grade:** Poor

## Overall verdict

This is a strong, decision-ready refactor PRD across all seven rubric dimensions — its decision architecture (§2 readiness snapshot, §12 register with dated OQ resolutions and named owners) and its preservation-gate rigor (FR-20's frozen versioned manifest, SM-C1/SM-C2 counter-metrics) are exemplary, and the unusual embedding of the 181-requirement legacy baseline in §14 is deliberate, firewalled, and honestly dispositioned. The rubric's residual risks are life-cycle rather than structural: point-in-time "target met" claims baked into a final document that will silently go stale, and an untraced linkage between §14's preserved requirements and FR-20's frozen test denominator.

The adversarial pass materially shifts the picture and sets the grade. It surfaced two critical contract-integrity defects, both re-verified on disk by the synthesis pass before grading: **C1** — the PRD is written as a plan with before-first-change preconditions (Phase 0 freezes, FR-20's manifest) while simultaneously reporting the refactor 70.43% complete, and the SM-C2 baseline artifact it depends on post-dates finalization by seventeen days and self-declares as a reconstruction under a story-level rule ("AC1") the PRD never defines or cites; **C2** — SM-C2 never pins measurement altitude, and the implemented gate is an in-process microbenchmark (HP-CREATE P95 = 1.76 µs) structurally blind to §8's "no synchronous cross-service calls" invariant, which has no verifying gate anywhere in the document.

The split verdict is real, not contradictory: as a piece of PM craft the document is excellent, but as a standalone finalized contract its inviolable gates hang on baseline artifacts it never binds, and a motivated implementer could pass every metric without delivering the promise. The grade formula gates on the confirmed criticals. Most fixes are localized — cite the frozen artifacts normatively, pin the SM-C2 envelope boundary, restate SM-3, and extend the no-silent-denominator-reduction rule to SM-1 — and would move the document to Good or better.

## Dimension verdicts

- Decision-readiness — strong
- Substance over theater — strong
- Strategic coherence — strong
- Done-ness clarity — strong
- Scope honesty — strong
- Downstream usability — strong
- Shape fit — strong

## Findings by severity

Overlapping rubric/adversarial findings are merged under the higher severity with both attributions.

### Critical (2)

**[Adversarial C1]** — A post-hoc document masquerading as a plan: Phase 0 / FR-20 preconditions are unsatisfiable given its own status claims (§5.3, §6.5 FR-20, §7 SM-1/SM-2, §2, frontmatter)
FR-20 demands the preservation manifest exist "before the first refactor change," yet §7 SM-1 in the same finalized document reports "Current evidence: 70.43%; target met." The PRD cites neither the preservation manifest nor the SM-C2 baseline by path or version. Disk-verified: `docs/release-evidence/sm-c2-hot-path-baseline-v1.md` is dated 2026-07-31 — seventeen days after finalization — and self-declares as "the AC1-permitted reconstruction," where "AC1" is a story-level criterion the PRD never defines. The PRD has no rule at all for retroactive-baseline fidelity.
Fix: State per-phase execution status at finalization; cite the frozen preservation manifest and SM-C2 baseline by path/version in FR-20 and SM-C2; add a requirement governing when a pre-refactor baseline may be reconstructed retroactively and what fidelity evidence it must carry.

**[Adversarial C2]** — SM-C2's measurement altitude is unspecified, so the 5% gate is satisfiable by the weakest possible benchmark — and the implemented gate chose exactly that (§7 SM-C2, §8 Performance, §5.3 Phase 0)
SM-C2's evidence list never pins the measurement boundary (in-process vs sidecar vs transport). §8's "no synchronous cross-service calls on hot paths" invariant is structurally uncheckable by an in-process benchmark and has no verifying gate anywhere. Disk-verified: the implemented baseline records HP-CREATE P95 = 1.76 µs/op and states it is "an in-process warm-path benchmark, not a sidecar/network latency test" — a promoted capability adding one synchronous sidecar call would be invisible to the gate. "Every identified hot path" also has no identifying artifact, so an empty identification passes vacuously.
Fix: Pin the envelope boundary in SM-C2; reference the hot-path inventory artifact normatively; add a separate testable consequence (call-graph/trace/dependency-boundary check) for §8's cross-service-call invariant.

### High (5)

**[Adversarial H1 + Rubric Strategic-coherence]** — SM-3 is unachievable inside the declared scope: "single source of truth" while fleet migration is out of scope (§7 SM-3 vs §5.2; addendum §E rows 2–4)
The duplicated copies live in Folders and Projects, which the pilot never touches; "exactly one source of truth" is false by construction, so SM-3 either fails at pilot close or gets silently reinterpreted.
Fix: Restate SM-3 as: canonical shared implementation exists, Conversations consumes it, surviving sibling copies registered as follow-on migration debt.

**[Adversarial H2]** — A Promote-classified area has no realizing FR: publication/event composition (638 LOC) is scope-homeless (addendum §C row 8; §6.3, §6.4 FR-17, addendum §F)
The only Promote row with no FR reference, absent from the §F backlog too; the 638 LOC sit inside the SM-1 denominator with no authorized mechanism to remove or externalize them. Two implementers can build incompatible outcomes from the same text.
Fix: Bind area 8's generic half to an FR, or reclassify it Keep/backlog with an FR-2-logged rationale, and reconcile the SM-1 denominator.

**[Adversarial H3]** — The SM-1 denominator was shrunk ≈4.7k LOC by a reclassification with no citable decision authority (addendum §A; §2; §6.1 FR-2; §12)
The Contracts/Testing→Keep move (~18k → 13,289 plumbing LOC) has no covering decision: OQ-3 doesn't reach it, the decision log is silent. §2 forbids "silent denominator reduction" only for the FR-20 test manifest; SM-1's LOC denominator has no equivalent protection — a hole precisely where the headline metric lives.
Fix: Extend the no-silent-denominator-reduction rule to the SM-1 baseline and cite the Contracts/Testing decision (owner, rationale, artifact) in addendum §A.

**[Adversarial H4]** — SM-2 sits at exactly 50.00% on self-declared low-confidence evidence, against a counterfactual baseline the PRD never defines (§7 SM-2; §6.4 FR-19; §12 OQ-2)
An inclusive ≥50% target met at exactly 50.00% means one file of measurement noise decides attainment, and the "frozen Story 4.1 measurement boundary" is invoked normatively but defined nowhere in the package.
Fix: Cite the Story 4.1 boundary artifact normatively, define the counterfactual construction rules, and require the FR-19 artifact to state single-file sensitivity.

**[Adversarial H5]** — FR-3 / FR-10 / FR-13 acceptance consequences overlap despite the crosswalk built to prevent exactly that (§6.3 crosswalk; §6.2 FR-3; §6.3 FR-10, FR-13)
FR-3's first consequence claims the AppHost/Aspire/ServiceDefaults-removal evidence the crosswalk assigns to FR-10/FR-13; story generation will produce forbidden duplicates or hollow acceptance.
Fix: Strip the removal clause from FR-3's consequences; FR-3 evidences host adoption and operation continuity only.

### Medium (9)

**[Rubric Decision-readiness]** — Point-in-time attainment claims embedded in a final requirements document (§7 SM-1, SM-2)
"Current evidence: 70.43%; target met" and "(50.00% files / 67.95% LOC)" are 2026-07-14 snapshots inside a `status: final` document; the versioned release-evidence artifacts, not these sentences, are the actual authority. (Staleness facet of the same cluster as C1.)
Fix: Rephrase attainment sentences as dated pointers to the authoritative artifacts, or strip them from the metric definitions.

**[Rubric Done-ness clarity]** — The §14 ↔ FR-20 enforcement linkage is only as strong as pre-refactor test coverage, and nothing requires mapping it (§0, §14.1, FR-20)
Any of the 181 preserved requirements with no passing pre-refactor test is guarded on paper only; the PRD never requires the coverage map that would reveal which ones those are.
Fix: Require the preservation manifest to record which Feature-FR/NFR each denominator test traces to, and list uncovered §14 obligations as explicitly unguarded.

**[Adversarial M1 + Rubric Shape-fit]** — The Vision contradicts the accepted baseline the addendum declares authoritative (§1 vs addendum §A)
"About half of it is plumbing" vs the accepted 13,289 LOC (37.15%); the "half" framing is what stakeholders will quote.
Fix: Use 37.15% in §1, keeping ~50% only as labeled Discovery provenance.

**[Adversarial M2]** — CORE is defined over "all eight preserved acceptance journeys" — §14.3 lists nine (§4 Glossary vs §14.3)
CORE is load-bearing (Feature-FR77/79/93 gate on it); the off-by-one lets an implementer argue any one journey out of scope.
Fix: Say "nine," or name the acceptance set explicitly.

**[Adversarial M3 + Rubric Downstream-usability]** — The §6.3 FR→addendum crosswalk is wrong in both directions, and FR-15's promised duplication evidence does not exist (§6.3 vs addendum §E, §F)
FR-15 appears in §E nowhere (its grounding is §C row 4 and §D); §E/§F row placements don't match the stated mapping; FR-15 rests on a "confirmed Conversations need" no artifact confirms.
Fix: Correct the mapping table; add FR-15's need evidence or label it a single-consumer promotion under R1's second branch.

**[Adversarial M4]** — §14.1's blanket disposition is vacuous for the process third of the preserved baseline (§14.1 vs §14.6)
Process/planning NFRs and accessibility audits cannot appear in FR-20's frozen manifest; "preserved as a constraint on FR-20" is unfalsifiable for them and invertible by a literal reader.
Fix: Partition §14.5/§14.6 into behavior-preserving vs process/roadmap classes.

**[Adversarial M5]** — The greenfield-latitude assumption's revisit trigger fires only after the irreversible action it licenses (§9; §13)
Plumbing-only test deletions happen in Phases 1–3; the production-status verification is scheduled at release time — after the tests are gone.
Fix: Move the verification to Phase 0; keep the release-time re-check as a second gate.

**[Adversarial M6]** — FR-20 protects only what got into the manifest, and manifest completeness has no criterion (§6.5 FR-20; §4; §9)
The conformance suite is defined with an open "etc." list; non-manifested behavioral tests are deletable with zero FR-20 process. The implementation was more careful than the PRD requires — the wrong direction for a contract.
Fix: Enumerate a closed category list and require a versioned plumbing-only justification ledger for deleted non-manifested tests.

**[Adversarial M7]** — "Already-demonstrated generic SDK seam" — the only soft edge on a 9.5k-LOC Keep boundary — is undefined (§6.3 Notes; §12 OQ-3; §5.2)
The single discretionary opening in the OQ-3 Keep decision is left entirely to implementer judgment.
Fix: Define "already-demonstrated" (released version + existing consumer or conformance test) or require per-use architect sign-off.

### Low (6)

**[Rubric Done-ness clarity]** — FR-18's template validation names no mechanism (§6.4 FR-18)
No check, reviewer, or evidence artifact; template-vs-module drift has no detector.
Fix: Name the validation act (e.g., recorded walkthrough attached to pilot acceptance).

**[Rubric Downstream-usability]** — Protagonist name collision: two Mayas (§3.3 UJ-1 vs §14.3)
"Maya's journey" is ambiguous across the refactor/legacy boundary.
Fix: Rename one protagonist.

**[Adversarial L1]** — SM-1 "Validates FR-3..FR-17" sweeps in deferred FR-16 (§7 SM-1 vs §2, §12 OQ-4)
Should read "FR-3..FR-15, FR-17." (Also in the rubric's mechanical notes.)
Fix: Correct the range.

**[Adversarial L2]** — Vision overstates the duplication evidence (§1 vs addendum §E row 2)
§1 lists four modules copying the tenant-access handler; §E evidences Folders and Projects only.
Fix: Match the Vision's module list to §E.

**[Adversarial L3]** — Addendum §E pre-empts OQ-1 with a nonexistent landing zone (addendum §E row 8 vs §4, §12 OQ-1)
"Commons.Testing" is not a listed technical module; naming it quietly answers the architect's reserved question.
Fix: Label it as a candidate, not a target.

**[Adversarial L4]** — The only inventory in the package violates FR-1/FR-2's own consequences (addendum §C vs §6.1)
§C carries dual classifications FR-1/FR-2 forbid and sums to ≈22.6k of 35,769 LOC; the compliant artifact lives outside the package. (Related: Feature-FR102 hard-codes "the Option A v1 deal" while §14.4 declares no option selected.)
Fix: Mark §C as superseded provenance and cite `docs/release-evidence/consume-promote-keep-inventory-v1.json` as the sole FR-1 object.

## Mechanical notes

- §7 SM-1 "Validates FR-3..FR-17" includes deferred FR-16; should read "FR-3..FR-15, FR-17."
- §6.3 mapping table is not exclusive: FR-10/FR-13 also appear in addendum §E, FR-11/FR-12/FR-14 also in §F.
- Addendum §D attribution drift (disk-verified): `IDomainQueryHandler`/`IDomainProjectionHandler` live in `Hexalith.EventStore.DomainService`, not EventStore.Client.
- Evidence paths (`docs/release-evidence/*.json`) are repo-root but not marked as such.
- Assumptions Index roundtrip clean: 8 inline tags ↔ 7 index rows (both §9 tags share one row).
- ID continuity clean: FR-1..20, Feature-FR1..104, Feature-NFR1..77 contiguous, no duplicates; all referenced IDs resolve.
- Glossary drift: "boilerplate" (defined) vs "plumbing" (undefined) used interchangeably; classification anchors the meaning.
- Feature-NFR30's "The PRD must define…" now ambiguously denotes the legacy feature PRD inside the embedded baseline.
- §14.1's relative archive link resolves (disk-verified).

## Reviewer files

- `review-rubric.md`
- `review-adversarial-general.md`
