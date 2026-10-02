---
title: 'Epic 7 retro items 31-32: green _bmad/scripts lane and CI-enforced completion gate'
type: 'chore'
created: '2026-10-02'
status: 'done'
route: 'oneshot'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Commit `0db6207` retired the planning-authority preflight and V12 lifecycle gates but left their suites in `_bmad/scripts/tests`, so the lane is permanently red (retro F-11), and nothing automatic keeps the `STORY-COMPLETION-GATE` blocks in place or re-checks a story after its done commit (retro F-3, F-6, G-4).

**Approach:** Item 32: retire the suites `0db6207` made obsolete from the default lane (kept in Git, runnable by naming the file), without weakening live tests or editing closed records, until the full lane is 0 failed / 0 errors / 0 unexplained skips. Item 31: in the existing `ci / repository` job, run the AC-7.3-01/02 verifier scenarios and the green lane; in each gate route (`.agents`/`.claude` twins byte-identical), rerun `--verify-inserted-record` after the done commit. No restored preflight, no successor authority, no new job. No v9 story contract exists for this work, so the story completion gate does not apply.

</frozen-after-approval>

## Implementation Notes

- No v9 story contract exists for this work (`v9/story-contracts/` holds only 7.1-16.3), so the completion gate does not apply. `story_key` is unset; sprint-status is not touched.
- Baseline at `19a9cfa` (`TMPDIR=/var/tmp python3 -m pytest -q -p no:cacheprovider _bmad/scripts/tests`): 1451 tests, 250 failed, 86 errors, 0 skipped (17m14s). `test_resolve_current_planning_authority.py` + `test_verify_evidence_boundary.py` = 76 failed / 18 errors, matching the retro. A no-submodule clone (CI-like) gave 278 failed / 86 errors: 2 extra in the preservation manifest suite (absolute paths) and 11 in the Epic 6 supersession suite (`E6_GITLINK_OBJECT_UNAVAILABLE`).
- Every failure traces to `0db6207`: the deleted `planning-authority-preflight.yml`, the V12 lifecycle-gate sections it removed from the routes, or the V9 authority it excluded from CI (`CANDIDATE_SOURCE_DRIFT` on `architecture.md`).
- Item 32, smallest option per suite:
  - `_bmad/scripts/tests/conftest.py` (new): `collect_ignore` retires 14 suites. That is the 11 failing ones plus the passing V18, V25, and V26 suites, which `0db6207` itself declared out of CI (runbook V23-V29 boundary, `ci.yml` V18 exclusion). Naming a file still runs it, and nothing is reported as skipped. V13, V14, V17, decision-chain, and Epic 6 suites stay, because `0db6207` did not retire them.
  - `test_verify_submodule_promotion.py`: the verifier stays live (the generator's v1 route imports it). Its 7 V12 route-contract tests now read the routes at `0bb017e6` (`0db6207^`), the last revision with those gates. Two sibling tests had been passing vacuously and are now meaningful again.
  - `test_generate_preservation_traceability_manifest.py`: its rc2 restore test recomputed the candidate digest from live route files, which `0db6207` and Story 7.3 rewrote. This is the same class as the two C# methods `0db6207` excluded from CI. I reused the owner's existing fixture pin from `fd2c74b` (branch `fix/v28-preserved-authority`, merged and then reset away on 2026-10-02) for this file only. It pins the frozen receipt digest and the absolute paths, which also fixes the clone-path failures.
- Item 31: `ci / repository` now uses full history (the generator tests read closed 7.x records), Python 3.11 through `actions/setup-python@v7.0.0`, and the locked `uv` environment. It runs AC-7.3-01/02 and the lane, minus the Epic 6 supersession suite, which needs initialized submodules. The timeout went from 5 to 40 minutes. The 7.3 block text ("no CI job or hook enforces it") stays true: CI enforces that the gate is present, not that it is run, so no block bytes changed.
- F-6: each of the four routes now has a post-done-commit `--verify-inserted-record` rerun outside the block, without repeating any verifier span or transition marker. Each `.agents/.claude` twin was copied byte for byte. The new test `test_v2_done_commit_moving_a_gitlink_fails_inserted_record_verification` (4 cases, named outside every 7.x `-k` selector) shows that a lifecycle-only done commit verifies, while one that also moves a gitlink fails with `CANDIDATE_NOT_FINAL` + `GITLINK_DRIFT`.
- Surprise: concurrent tooling rebased `main` onto `origin/main` (`1687354`) mid-run, staged this work, and staged unrelated `references/Hexalith.Projects` and `references/Hexalith.Tenants` gitlink moves. The commits use a pathspec, so those gitlinks stay staged and are not committed.
- Second surprise: `/pushall` committed the pre-review state as `23e27a5`, linearized it into `04abe68` (adding the owner's `fd2c74b` pins to four suites that are already retired), and pushed it with `66da9cb` and `46fa94e`. Uncommitted route rewording was lost and has been reapplied. The owner then asked to "check and fix any missing items", so the review fixes land as one follow-up commit on top.
- GitHub CI on `66da9cb` (run 36973689869), `ci / repository`: CPython 3.11.16, AC-7.3-01 and AC-7.3-02 `PASS`, `594 passed in 508.34s`, job 8m40s.
- Review fixes:
  - The rerun clause now applies the gate's blocker branch, with a validated rollback commit. In bmad-build-auto it halts `blocked` instead of falling through to `HALT with status done`.
  - `test_every_gate_route_reruns_inserted_record_verification_after_its_done_commit` pins that clause on all 8 surfaces, after the block and the done transition. It is probed red for both a dropped and a displaced clause.
  - The preservation-traceability rc2 suite is retired instead of pinned. It rebinds published Epic 6 evidence to the live routes and the `global.json` SDK pin, so any SDK bump would turn it red. The test file keeps its `HEAD` bytes.
  - CI uses `uv sync --locked`, `-ra`, and JUnit output, and uploads the lane XML and verifier JSON.
  - Runbook wording now matches what the verifier actually detects.
- Final lane: 15 retired suites (collected only when named). The local directory lane and the CI lane (minus Epic 6 supersession) results are in the follow-up commit message.

## Review Triage Log

- `[false]` Staged index differs from the worktree, so a commit would carry stale `ci.yml` and runbook content. Commits here use a pathspec (`git commit -- <paths>`), which records worktree content. The concurrent `/pushall` commit also captured the worktree state.
- `[medium]` Nothing guards the post-done-commit rerun clause, which sits outside the gate block. Patched: the new route guard test, probed red for a dropped and a displaced clause.
- `[low]` In bmad-build-auto, the failure branch says "then HALT" right before "HALT with status `done`". Patched: it now halts with status `blocked` and blocking condition `done commit failed inserted-record verification`.
- `[low]` The four rollback clauses restated the blocker branch, had drifted from each other, and did not require a validated commit. Patched: one shared clause that applies the gate's blocker branch with a validated Conventional Commit.
- `[medium]` The rc2 preservation test pin is brittle on the next SDK bump and weakens what the test checks. Verified that `parse_toolchain_capture` compares against the live `global.json` SDK. Patched by retiring the suite (`conftest.py`), not by pinning it.
- `[low]` CI ignores the whole Epic 6 supersession suite, although 30 of its 41 tests need no submodule objects. Deferred to `deferred-work.md` (marker plus `-m` deselection).
- `[low]` `collect_ignore` has no guard, and the docstring misstated the CI exclusions. Patched the docstring, which now lists each retirement reason. A pin test was rejected: it would only duplicate the list, and edits are visible in review.
- `[low]` CI keeps no evidence and runs pytest without `-ra`. Patched: `-ra`, JUnit output, and an upload with `if: always()`. Skip prevention stays with the in-lane `test_static_anti_skip_guard.py`.
- `[low]` `uv sync --frozen` cannot detect lock drift. Patched to `--locked`.
- `[false]` No evidence the CI lane passes without submodules. A local no-submodule clone gave 594 passed, and GitHub run 36973689869 gave 594 passed. The notes were not empty; they were written before review.
- `[low]` The runbook called this the "default" lane and overstated what "alters" detects. Patched the wording.
- `[low]` The runbook lacks local reproduction steps and does not mention the route rerun. Deferred to item 34 (`deferred-work.md`).
- `[low]` The V12 route tests keep live-sounding names. Rejected: the comment block above them names the pinned revision, and renaming would churn node IDs for no behavioural gain.
