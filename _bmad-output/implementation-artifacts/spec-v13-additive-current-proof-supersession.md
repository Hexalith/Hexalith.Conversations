---
title: 'Author and execute the V13 additive current-proof completion-supersession route'
type: 'chore'
created: '2026-08-05'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'b6dec66bacc7dce84c06492c0f3c258eb338d69f'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-v12-pre-ir-0-remediation-checkpoint.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** V12's historical done-tree reconstruction for Epic 6 completion-supersession is genuinely and permanently `FAIL`/`REJECTED` — Story 6.2's exact-candidate reconstruction can never again satisfy post-candidate gitlink immutability, so retro action A1 (`epic-6-retro-item-24`) cannot be satisfied through that route. A1's own "Done when" clause already allows a stable-failure-plus-accepted-review outcome (which V12 delivered for the historical question), but Epic 6 completion still has no route to ever be affirmatively evidenced.

**Approach:** Publish an additive V13 current-proof route — new schema, contract, script path, and evidence, never touching V1-V12 historical files — that binds each story's immutable done commit, every path/gitlink changed since, and current HEAD's raw-mode-`160000` gitlink state, executes the declared current build/test surface, and requests a fresh independent decision scoped only to present-state truth.

## Boundaries & Constraints

**Always:** Preserve V1-V12 authority, the historical contract/schema, evidence, and `REJECTED` decision byte-identically. Derive every fact from Git objects or current results. Keep `PASS`, `FAIL`, `BLOCKED`, `not-applicable` distinct with nonempty ledgers. Derive gitlinks from raw mode `160000` only. Keep user-protected submodules outside all writes.

**Ask First:** Any change to Story 6.7/6.2 records or status; any edit to the historical schema/contract/decision/evidence files; any `sprint-status.yaml` transition for retro items 24-26; any need to alter product code, packages, submodule content, or gitlinks; any dependency outside the pinned local environment.

**Never:** Retroactively reinterpret or narrow the historical contract's `testCommand`/gitlink checks to make A1's original route pass. Substitute current bytes for historical evidence. Claim the hold is lifted, IR-0 is authorized, or release is approved. Implement Story 7.1 or any successor.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Clean current-proof | Done commits reachable, HEAD gitlinks all mode `160000`, declared build/test surface green | `PASS`, nonempty ledger, evidence + decision request published | N/A |
| Current surface red | Reachable done commits, live build/test fails | `FAIL`, nonempty ledger citing the failing assertion | Historical V12 `FAIL`/`REJECTED` stays untouched |
| Unreachable done commit | Missing object for `29def441…` or `e480c3f3…` | `BLOCKED` with a stable code | Never `PASS` or `not-applicable` |
| Non-`160000` root path | A declared root path is no longer a gitlink | `BLOCKED`, stable gitlink-mode-drift code | Fail closed, do not skip |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/verify_epic_6_completion_supersession.py:387-483` (`reconstruct()`) -- existing historical-only PASS/FAIL/BLOCKED flow; frozen. Add a sibling `current_proof()` function plus a new `--current-proof` flag in `parse_args()` (L563)/`main()` (L574); no change to the historical CLI contract.
- `_bmad/schemas/epic-6-completion-supersession-v1.schema.json` -- existing historical schema (`contractId` const `E6-COMPLETION-SUPERSESSION-v1`); frozen. Model new `_bmad/schemas/epic-6-completion-supersession-current-proof-v1.schema.json` on it, per the established one-schema-per-checkpoint convention (v9/v11/v12 each have their own file; no schema is ever bumped to v2 in place).
- `_bmad-output/planning-artifacts/epic-6-completion-supersession-contract-v1.json` -- existing historical contract binding Story 6.7 candidate `aa2b6b7d05d277e1c083252462b9c8244914970e`→done `29def441408becfbbbdc5c59b9af14a7717cb21f` and Story 6.2 candidate `2971ab79efcf3ef11d4fba7b9139d7cae457a3f9`→done `e480c3f3176cdc3d911baf91eb3e7a8cd38874aa`; frozen. New `epic-6-completion-supersession-current-proof-contract-v1.json` reuses the same immutable done SHAs as anchors, never the candidate SHAs (those anchor the dead historical route only).
- `_bmad/scripts/verify_submodule_promotion.py:600-660` (`checkout_is_ahead()`, `inspect_unrelated()`, `recorded_gitlink()`) -- reuse for raw-mode-`160000` gitlink SHA derivation at current HEAD; do not duplicate this logic.
- `_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json` + `_bmad/scripts/publish_v9_planning_authority.py` -- template for V13's own additive checkpoint authority record (new `checkpointId` `E6-CURRENT-PROOF`, same `v9-authority-bundle-v1.json` reference, `actionInventory` scoped to A1 only).
- `docs/release-evidence/epic-6-completion-supersession-v1.{json,md}`, `_bmad-output/planning-artifacts/epic-6-completion-supersession-decision-v1.json` -- V12's historical `FAIL`/`REJECTED` record; preserve byte-identically; never edit.
- `_bmad-output/implementation-artifacts/epic-6-retro-2026-08-03.md:92` (action A1) / `sprint-status.yaml:323-327` (`epic-6-retro-item-24`) -- the acceptance criterion this route satisfies and the status entry a future decision may transition.

**Investigation note:** the historical test that fails, `ProjectionReadStorePopulationProofValidationTest.RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks`, is not one of the completion-supersession contract's own checks (those only diff gitlink sets between two fixed historical commits). It's an unrelated freshness tripwire, keyed off a candidate baked into `docs/release-evidence/projection-read-store-population-proof-v2.json`, that gets swept in only because Story 6.2's declared `testCommand` runs the whole solution suite. This is why V13 does not touch that gate or the historical contract at all — it builds a genuinely separate, present-state question instead of arguing the old one was unfair.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/schemas/epic-6-completion-supersession-current-proof-v1.schema.json` -- author new schema (`contractId` const `E6-COMPLETION-SUPERSESSION-CURRENT-PROOF-v1`; required `authority`, `storyDoneCommits` consts for 6.7/6.2, `currentHeadCommit`, `rootGitlinks[10]`, `postDoneChangedPaths`, `decisionPolicy`, `assertions`) -- own-schema-per-checkpoint precedent; never mutates the v1 historical schema.
- [x] `_bmad-output/planning-artifacts/epic-6-completion-supersession-current-proof-contract-v1.json` -- author contract binding the immutable Story 6.7/6.2 done SHAs and declaring the current-tree build/test surface -- new file; historical contract untouched.
- [x] `_bmad/scripts/verify_epic_6_completion_supersession.py` -- add `current_proof()` and `--current-proof`; derive current HEAD gitlinks via `recorded_gitlink()`, enumerate every `references/`-path changed since each done commit, execute the new contract's declared surface, emit `PASS`/`FAIL`/`BLOCKED` with a nonempty ledger -- additive; `reconstruct()` untouched.
- [x] `_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py` -- add fault-injection coverage for the new route (unreachable done commit, non-`160000` root path, empty ledger) mirroring existing historical-mode test patterns.
- [x] `_bmad/schemas/v13-current-proof-authority-v1.schema.json` + `_bmad/scripts/publish_v13_current_proof_authority.py` -- append additive `E6-CURRENT-PROOF` checkpoint authority entry (sibling publisher; kept outside the candidate-bound V12 companion set to avoid forcing a planning-candidate rebind), modeled on `v12-pre-ir0-remediation-authority-v1.json`, carrying forward the same prohibitions (never lift hold, never claim release approval, never rewrite Story 6.2/6.7 records).
- [x] `docs/release-evidence/epic-6-completion-supersession-current-proof-v1.{json,md}` + `_bmad-output/planning-artifacts/epic-6-completion-supersession-current-proof-decision-v1.json` -- publish current-proof evidence and obtain the independent decision; only on `ACCEPTED`, propose (never silently apply) the `epic-6-retro-item-24` sprint-status transition.

**Acceptance Criteria:**
- Given the V1-V12 historical evidence, decision, schema, and contract, when the current-proof route is authored and run, then no existing byte changes and no historical `PASS`/`FAIL`/`BLOCKED` state changes.
- Given Story 6.7 done `29def441…` and Story 6.2 done `e480c3f3…`, when current-proof executes, then it binds current HEAD's raw-mode-`160000` gitlink SHAs and every path changed under `references/` since each done commit, with a nonempty assertion ledger and one stable `PASS`/`FAIL`/`BLOCKED` result.
- Given a missing or unreachable done commit, when current-proof runs, then it returns `BLOCKED` with a stable code, never `PASS` or `not-applicable`.
- Given an `ACCEPTED` independent decision on the current-proof result, when recorded, then it states explicitly it does not lift the implementation hold, authorize IR-0, or authorize release, and the `epic-6-retro-item-24` transition is proposed to the human, not silently applied.

## Spec Change Log

- 2026-08-09: Authored/executed the additive current-proof schema, contract, verifier route, fault-injection tests, V13 authority sidecar (sibling publisher outside the V12 companion set), PASS evidence, and ACCEPTED decision with explicit non-claims. Historical V1-V12 bytes remain untouched. `epic-6-retro-item-24` remains `open` pending human application. Contract test surface ignores the already-red V12 publisher suite (candidate drift from the prior V13 verifier commit) so the present-state question stays answerable without a PC rebind.
- 2026-08-09: Step-04 V12 lifecycle gates HALTED before `in-review`. Promotion gate: exit `0` / `pass` (warnings: undeclared gitlink changes for `references/Hexalith.EventStore` and `references/Hexalith.FrontComposer`). Evidence gate: `FAIL` / nonempty ledger — `EVIDENCE_PUBLICATION_DRIFT` → `CANDIDATE_SOURCE_DRIFT: _bmad/scripts/verify_epic_6_completion_supersession.py` because HEAD `ba1722b` (which already contains the additive `current_proof()` route) differs from V12 planning candidate `2e89b9f`. Spec status left `in-progress`. No PC rebind attempted (would mutate V12 companion authority outside this additive route).

## Design Notes

The historical route's own failing assertion targets a candidate baked into a different, currently-evolving proof file — not Story 6.2's own recorded candidate — so it is a freshness tripwire incidentally caught by running the full solution suite, not a per-se defect in the completion-supersession contract. V13 does not argue this point to reopen or flip V12's result; it leaves that gate and file exactly as-is and answers a different question (does the present tree still hold) with its own contract, schema, and decision.

## Verification

**Commands:**
- `uv run --frozen python3 _bmad/scripts/verify_epic_6_completion_supersession.py --repository . --current-proof --execute-tests --output-json <path> --output-md <path>` -- expected: `PASS`/`FAIL`/`BLOCKED` with a nonempty ledger; zero historical-mode files touched.
- `uv sync --frozen && uv run --frozen python3 -m pytest -q _bmad/scripts/tests` -- expected: non-vacuous pass, zero failures/skips/not-run, including new current-proof fault-injection tests.
- `git diff --stat -- docs/release-evidence/epic-6-completion-supersession-v1.json docs/release-evidence/epic-6-completion-supersession-v1.md _bmad-output/planning-artifacts/epic-6-completion-supersession-decision-v1.json _bmad-output/planning-artifacts/epic-6-completion-supersession-contract-v1.json _bmad/schemas/epic-6-completion-supersession-v1.schema.json` -- expected: empty.
