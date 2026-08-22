---
title: 'Restore E6-REMEDIATION A2 lifecycle evidence gates'
type: 'bugfix'
created: '2026-08-16'
status: 'in-progress'
baseline_commit: '1a7c08a4d37d826a151e4eb43faa092f21be0365'
submodule_promotions: []
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** BMAD 6.11 synchronization commit `4ba45a7ebf0312486f2dd61db98d4f68e73d373f` removed the V12 promotion and evidence-boundary gate blocks from all twelve active review/done routes. Lifecycle transitions can therefore reach review or done without the fail-closed checks required by E6-REMEDIATION A2, and the route-contract lane is red.

**Approach:** Reinsert only the deleted, previously validated V12 gate blocks from the last known-good revision `572bd66a7faef5e5bc14ea965967e2995a99ac4c` at their current lifecycle anchors in both skill trees. Preserve every unrelated BMAD 6.11 change and use the existing verifiers and mutation tests without weakening their contracts.

## Boundaries & Constraints

**Always:** Keep the six logical routes byte-identical between `.agents` and `.claude`; place exactly one bounded gate before each lifecycle write; run submodule promotion before evidence-boundary validation; preserve `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` as distinct outcomes; require a nonempty assertion ledger; leave lifecycle state unchanged and halt for every non-continuing result. Preserve the existing retrospective and pre-existing `sprint-status.yaml` edits.

**Ask First:** Any missing historical gate blob, lifecycle anchor or required placeholder; any need to alter verifier logic, route inventory, customization resolution, planning authority, action-item status, or files outside the twelve authorized routes; any incompatibility between the historical blocks and current BMAD 6.11 semantics.

**Never:** Restore whole pre-6.11 route files; revert `uv run` or other BMAD 6.11 changes; weaken, delete, relocate, or bypass a verifier assertion; absorb A3 context/publication repairs; modify product code, packages, submodules, gitlinks, completed story records, planning authority, or the implementation hold; claim IR-0 readiness, hold lift, story completion, or release authorization.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Applicable clean transition | Committed candidate, declared promotions, both verifiers succeed, nonempty ledger | Continue to the existing review/done lifecycle write | Preserve verifier evidence and status semantics |
| Valid non-applicable transition | No applicable evidence-boundary change and verifier records `not-applicable` with a nonempty ledger | Continue without misreporting `PASS` | Missing or empty result halts |
| Promotion or evidence fault | Dirty/unbound promotion, `FAIL`, `BLOCKED`, skipped/missing result, or empty ledger | Do not execute the lifecycle write | Halt with the original stable result/code |
| Review patch changes evidence | Automated review applies a patch after an earlier green result | Rerun both gates before finalization | A stale earlier result cannot authorize done |

</frozen-after-approval>

## Code Map

- `{.agents,.claude}/skills/bmad-build/{step-04-review.md,step-05-present.md,step-oneshot.md}` -- restore the historical V12 block immediately before `in-review`, `Mark Spec Done`, or trace generation containing `status: 'done'`.
- `{.agents,.claude}/skills/bmad-build-auto/step-04-review.md` -- restore the review-transition gate and retain the required post-patch rerun before final done.
- `{.agents,.claude}/skills/bmad-dev-story/SKILL.md` -- restore the XML action before Story status becomes `review`, without reverting its BMAD 6.11 `uv run` changes.
- `{.agents,.claude}/skills/bmad-code-review/steps/step-04-present.md` -- restore the review gate before status selection; a failed gate must force `in-progress` and prevent the done branch.
- `_bmad/scripts/verify_evidence_boundary.py:19` -- read-only source of the exact twelve-route inventory, marker, lifecycle tokens, parity, command tokens, and result semantics.
- `_bmad/scripts/tests/test_verify_evidence_boundary.py:20`, `_bmad/scripts/tests/test_verify_submodule_promotion.py:945`, `_bmad/scripts/tests/test_generate_story_record.py:1383` -- existing positive, placement, parity, decoy, mutation, and bounded-span acceptance tests; do not edit to make restoration pass.
- Git object `572bd66a7faef5e5bc14ea965967e2995a99ac4c` -- read-only source for the gate hunks only. Current route bodies remain authoritative outside those hunks.

## Tasks & Acceptance

**Execution:**
- [x] Restore each historical gate hunk at its surviving current anchor in the six `.agents` routes, preserving all surrounding current bytes.
- [x] Apply the identical six hunks to the mirrored `.claude` routes and confirm exact pairwise byte equality.
- [x] Run the focused route-contract and fault-injection lane, then run the complete Python tooling lane and classify any remaining failures strictly as pre-existing A3 or protected working-tree concerns.

**Acceptance Criteria:**
- Given the current twelve-route inventory, when active-route validation runs, then all twelve files contain exactly one gate before their lifecycle write, each `.agents`/`.claude` pair is byte-identical, and the assertion ledger is nonempty.
- Given promotion, gate removal, gutted clause, displaced clause, decoy, parity, skipped-result, and empty-ledger fixtures, when the focused tests run, then every named fault turns red with its stable code and restores fixtures byte-identically.
- Given the current BMAD 6.11 route bodies, when the repair is diffed against HEAD and `572bd66`, then only the deleted gate hunks return; `uv run`, context workflow, and all other upgrade changes remain intact.
- Given the A2-focused test selection, when it completes, then it has zero failed, skipped, or not-run selected tests. Full-suite A3 failures are reported without being weakened or folded into A2.

## Spec Change Log

- 2026-08-22: The human approved a narrow scope expansion to
  `_bmad/scripts/tests/test_verify_submodule_promotion.py` after the matrix audit
  showed that removing the post-review gate-rerun clause did not turn a test red.
  The route contract now requires that clause and a byte-restoring mutation test
  covers its removal.

## Verification

**Commands:**
- `uv run --frozen python3 -m pytest -q --tb=short _bmad/scripts/tests/test_verify_evidence_boundary.py _bmad/scripts/tests/test_verify_submodule_promotion.py _bmad/scripts/tests/test_generate_story_record.py -k 'active_route_inventory or route_gate_faults or displaced_gate_and_cross_tree_parity or completion_workflows_gate or workflow_contract_check or workflow_contract_rejects_enforcement_clause_outside_gate or both_skill_trees_stay_byte_identical or current_route_inventory or v12_gate_span'` -- expected: all selected A2 tests pass with no skips or not-run results.
- `uv run --frozen python3 -m pytest -q --tb=short _bmad/scripts/tests` -- expected: all A2 route-contract failures are eliminated; any remaining failures are unchanged A3/protected-worktree failures and are reported distinctly.
- `git diff --check` plus pairwise comparison of the six logical routes -- expected: no whitespace errors or mirror drift, and no product, planning-authority, submodule, or gitlink change.

**Results (2026-08-22):**
- Exact A2-focused lane: `29 passed, 134 deselected`; no selected failures or skips.
- Explicit I/O and edge-case matrix audit: `10 passed`.
- Complete Python tooling lane: `272 passed, 9 failed`; all nine failures were
  protected `CANDIDATE_SOURCE_DRIFT:
  _bmad/scripts/tests/test_verify_submodule_promotion.py`
  publication-authority failures, distinct from the green A2 route-contract
  lane. The user-authorized additive candidate rebind owns that prerequisite;
  this A2 record does not weaken or bypass it.
- Approved test-file `git diff --check`: passed. The focused lane also confirmed
  exact twelve-route inventory and six-pair byte parity.
- Review-transition promotion gate: `pass` at committed candidate
  `36febdd94faaaf0db99fcb4d0feae82ab4df115c`, with seven visible
  `UNDECLARED_GITLINK_CHANGE` warnings and no blockers.
- Review-transition evidence gate: `FAIL` with nonempty assertion ledger and
  stable code `EVIDENCE_SCOPE_BASELINE_MISMATCH`; the publication scope expects
  baseline `4e3828cfaa189604c11be34e8d67bc94520785d8`, while this spec freezes
  `1a7c08a4d37d826a151e4eb43faa092f21be0365`. Lifecycle status remains
  `in-progress`.
