# Validation Report — Conversations Boilerplate Reduction

- **PRD:** `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md`
- **Rubric:** `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-09-16T18:02:53+02:00
- **Grade:** Poor

## Overall verdict

This is a strategically coherent, unusually well-bounded refactor PRD: its thesis is specific, its scope cuts are explicit, and most requirements carry concrete consequences plus preservation counter-metrics. The current artifact is nevertheless only fair under the rubric because its active implementation hold, executed-phase claim, unresolved architecture dependency, and provisional acceptance evidence are not reconciled into one actionable current disposition.

The adversarial pass materially lowers the consolidated grade to **Poor**. It found two release-blocking contradictions: the evidence system enforces a materially weaker SM-C2 rule than the PRD's universal 5% regression gate, and FR-20's cited 14-suite baseline does not demonstrably cover two behaviors in its declared closed category set. Seven additional high findings show that execution state, scope, ownership, and acceptance evidence have drifted far enough that this PRD is not safe as a current release, architecture, or story-generation authority.

## Dimension verdicts

- Decision-readiness — thin
- Substance over theater — strong
- Strategic coherence — strong
- Done-ness clarity — adequate
- Scope honesty — strong
- Downstream usability — adequate
- Shape fit — adequate

## Findings by severity

### Critical (2)

**[Adversarial]** — Enforced SM-C2 rule contradicts the PRD's inviolable performance gate (§2, §7 SM-C2, §12 OQ-5)

The PRD requires every identified command/read hot path to remain within 5% of the frozen P95 baseline. Current post-refactor evidence instead records HP-APPEND at +20.01% as ungated and accepts HP-LIST at +329.85% and HP-OPEN at +1760.88% against separate approved-cost ceilings. The normative requirement and enforced evidence can therefore produce opposite release decisions.

Fix: Choose one rule through explicit change control. Either restore the universal 5% gate and mark current results failed, or version the PRD with exact per-path rules, approver, rationale, expiry, successor work, ceilings, and evidence-schema binding.

**[Adversarial + Rubric done-ness]** — FR-20's closed preservation denominator does not contain the behaviors it claims to close (§4, §6.5 FR-20)

The cited 14-suite / 214-test baseline does not identify projection-freshness or governance-audit-pairing suites even though both are members of the PRD's closed seven-category behavior set. The requirements-to-tests traceability manifest remains draft and pending prerequisites, so 100% of the current manifest is not proof of 100% of the declared preservation boundary. This also subsumes the rubric concern that several preservation consequences lack an explicit frozen oracle.

Fix: Publish and approve one denominator artifact containing exact test IDs, categories, requirement IDs, source commit, and hashes; reconcile all seven declared categories against the 14 suites; and require zero orphaned active obligations before FR-20/SM-C1 acceptance or hold lift.

### High (7)

**[Adversarial + Rubric decision-readiness]** — “Final” and “executed” are stale, non-actionable readiness claims (§2, §5.3)

The PRD says Epics 1–6 executed and Epics 7–9 remain backlog, but current sprint status also contains Epics 10–16, corrective work, and an active global hold. The PRD neither summarizes the hold decision nor names its owner, outcomes, evidence, and resume/stop condition. A historical snapshot is being presented as the current chain-top readiness authority.

Fix: Mark the snapshot historical or generate it from one current authority. Add a compact hold decision row with question, owner, permitted outcomes, evidence inputs, and unblock condition; separate work performed from work evidenced, accepted, held, or rejected.

**[Adversarial]** — OQ-1 remains open after the implementation it was required to gate (§2, §5.3, §12; addendum §B)

If FR-10 through FR-15 stories executed, their landing zones were necessarily chosen, yet the PRD and addendum still call them open and do not bind per-FR decisions or owning-repository approval.

Fix: Publish a per-FR decision table covering repository/package, rejected options, approver, decision artifact, implementing revision, release vehicle, compatibility evidence, and rollback owner. Close OQ-1 or mark affected stories nonconforming.

**[Adversarial]** — FR-16 crossed the declared scope boundary through unconsumed platform work (§2, §5.2, FR-16; addendum §F)

Story 3.7 added shared EventStore command/event metadata and resolvers while direct Conversations adoption remained deferred—the exact unconsumed promotion the PRD excludes from the pilot.

Fix: Move that work to a separately authorized platform initiative or version the PRD to activate the precise FR-16 subset. Exclude it from pilot metrics and split platform creation from Conversations adoption in story status.

**[Adversarial]** — Template and SM-2 proof are called executed while mandatory evidence remains provisional (§5.3, §7 SM-2, FR-18, FR-19)

Phase 3 “Adopt & Prove” is reported as executed, but SM-2 expressly does not establish target attainment and Epic 11 still carries corrective fixture/template evidence work.

Fix: Mark FR-18, FR-19, SM-2, UJ-3, and Phase 3 proof pending until the reproducible fixture, walkthrough, measurement, and named acceptance exist.

**[Adversarial]** — The authoritative SM-1 denominator lacks required named acceptance (§7 SM-1; addendum §A)

The inventory is called accepted and authoritative while the addendum admits that it has no named acceptor. A JSON `status: accepted` field is not the approval discipline required by the PRD.

Fix: Obtain retrospective ratification bound to the exact hash and disclose its timing, or mark the denominator provisional and recalculate only after approval.

**[Adversarial]** — §14 has no machine-safe activation model (§0, §14.1, §14.4)

The section presents 181 normative-looking requirements while also saying their implementation and release activation are unknown. Downstream tooling cannot reliably distinguish implemented, preservation-gated, conditional, historical-only, active-release, and unguarded obligations.

Fix: Put §14 in a versioned contract catalog with explicit per-requirement state and a signed activation manifest. Keep only the active preservation subset and its hash in this refactor PRD.

**[Adversarial]** — Cross-repository authorization is asserted without an accountable grant (§5.1, §9, §11)

The PRD authorizes technical-module changes without naming EventStore, Commons, or FrontComposer owners or defining consumer coverage, release order, compatibility period, and rollback authority.

Fix: Add a signed cross-repository change charter with per-capability owners, pinned consumer matrix, required CI evidence, version/release sequence, compatibility window, and rollback procedure.

### Medium (3)

**[Adversarial]** — “No external/customer-facing surface” understates the preservation blast radius (§3, §3.2, §10, §14)

Developers are the direct users of the refactor, but adopter APIs, tenant isolation, operator workflows, publication behavior, and customer-visible failure modes are affected surfaces.

Fix: Distinguish refactor users from preservation-failure stakeholders and assign adopter, release, operator, and tenant-user review responsibilities.

**[Adversarial]** — No-sync-cross-service-call NFR lacks a closed verifier (§8)

The PRD permits either a dependency/call-boundary check or a trace assertion and names no fixed oracle, command, artifact schema, or pass criteria.

Fix: Select one required verifier; define hot-path boundaries, forbidden edges, command, artifact path/schema, and pass rule; bind it to the SM-C2 release gate.

**[Adversarial]** — Gate roles are generic where named authority is required (§12–§14, FR-20, SM-1)

Roles such as platform architect and release owner are not linked to named people or a versioned authority source, so approval validity cannot be checked for a decision date and scope.

Fix: Reference a versioned authority registry with person, role, effective dates, delegated scope, and supersession; bind approval evidence to the person and registry version.

### Low (1)

**[Adversarial]** — Superseded inventory remains easy to misuse as active architecture input (addendum opening and §C)

The warning is explicit, but the superseded table still contains implementation-ready recommendations while the authoritative JSON is not rendered alongside it.

Fix: Move the table to a historical appendix or render the accepted inventory as primary; add a machine-visible `superseded: true` marker plus the authority hash.

## Mechanical notes

- Primary IDs are contiguous and unique: FR-1..FR-20; UJ-1..UJ-3; Feature-FR1..Feature-FR104; Feature-NFR1..Feature-NFR77.
- Every UJ has a named protagonist, and checked local Markdown targets exist.
- All eight inline `[ASSUMPTION]` callouts round-trip through §13.
- The addendum opening broadly calls figures “first-pass and approximate” while §A declares the 13,289-LOC baseline authoritative. Narrow that disclaimer to historical estimates and candidate mappings.
- Historical review artifacts were read for audit context but were not used to carry forward the superseded 2026-08-18 grade or findings.

## Gate recommendation

Do not use this PRD to lift the implementation hold, authorize release, generate implementation stories, or certify the thin authoring template. Correct the critical and high findings, then rerun validation against the revised PRD, addendum, current sprint/authority bundle, and exact evidence hashes.

## Reviewer files

- `review-rubric.md` — current run, 2026-09-16
- `review-adversarial-general.md` — current run, 2026-09-16
- `review-editorial-prose-addendum.md` — historical audit context
- `review-editorial-prose-prd.md` — historical audit context
- `review-editorial-structure-addendum.md` — historical audit context
- `review-editorial-structure-prd.md` — historical audit context
- `review-legacy-preservation.md` — historical audit context
- `review-remediation-verification.md` — historical audit context
