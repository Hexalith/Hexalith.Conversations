---
title: 'Close five deferred record verification safeguards'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '0fe0efb512645f6c3e3df9e64554ed104d0a7b60'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred repository-tooling scopes remain: record generation silently collapses duplicate root submodule paths; fixture proofs select the first assembly copy without detecting contradictory later copies; early CLI errors honor the first format flag instead of argparse's last flag; pytest passing-exit semantics lack a nondefault-policy regression; and read-only record verification lacks complete worktree and Git-state preservation coverage.

**Approach:** Complete these five scopes as the requested batch. Reject duplicate committed declarations, compare every present consuming assembly copy while retaining deterministic selection, and align early-error format precedence with argparse. Add disposable regressions for nondefault passing exits and preservation of file bytes, Git index, refs, HEAD, status and gitlinks in live and historical read-only verification. Preserve accepted evidence, fixture sources and locks, original ledger entries, dependencies, CI policy and sprint lifecycle values. Validate and independently review the changes, then commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Clean main at the canonical baseline above. This is a small routine tooling batch with settled implementation choices and no new public API, migration or deployment; push is explicitly authorized. No epic story or effective final-record contract applies. Historical evidence-boundary and final-record generation are not-applicable.
- Sources: September 22 duplicate-declaration finding; October 19 stale-assembly sub-scope; July 30 pass-8 repeated-format sub-scope; September 23 missing passExitCodes regression; July 28 non-mutation regression gap. Preserve the unresolved portions of compound entries.
- Independent investigation confirmed that unrelated-submodule diagnostics, output containment and exclusive temporary creation already have guards. SDK rederivation remains a separate measurement-policy question and is not counted in this batch.
- `_bmad/scripts/generate_story_record.py`: root_submodule_paths and pre_parse_output_format; preserve candidate-blob reads and supported full/inline/abbreviated format flags.
- `_bmad/scripts/generate_minimal_module_fixture_proof.py`: find_assembly; compare all present candidate copies, preserve preferred selection, missing-output behavior and Release-output cross-check.
- `_bmad/scripts/tests/test_generate_story_record.py`: committed duplicate-path fixtures; nondefault passExitCodes; actual CLI early errors; disposable read-only snapshots including index/refs and worktree state, with passing and failing controls.
- `_bmad/scripts/tests/test_minimal_module_fixture.py`: hermetic multi-copy controls and hidden stale-copy negatives, preserving the accepted fixture and proof bytes.
- Verification: focused affected Python selections, complete fixture-proof suite, existing npm tooling lane, whitespace and pinned commitlint. Inspect CI after push and report existing failures separately from changes attributable to this batch.
- The full-state regression exposed real index-cache writes during live verification. Both the generator and its embedded promotion checker now disable optional Git locks, preserving index bytes and timestamps even when a stat refresh would otherwise occur. All eleven new generator cases pass, including passing/failing live and historical controls. The initial test run also caught a misplaced test-helper timestamp statement; it was corrected before the passing run.
- Complete fixture-proof and promotion suites pass 196 tests with zero failures/errors/skips; receipt `/tmp/conversations-five-record-fixture-promotion.xml`. Two existing record_property/xunit2 warnings are retained. The affected generator selection passes 123 tests, zero failures/errors/skips; receipt `/tmp/conversations-five-record-generator-affected.xml`, exact 76-function selection `/tmp/conversations-five-record-generator-nodes.txt`. This includes the v1 live/historical cases, v2 malformed/empty/fault/determinism cases and existing Story 7.2 duplicate mapping.
- Both Story 7.3 operational workflow scenarios pass, as do the current indexed root check and whitespace. No accepted record was regenerated. The historical ledger remains an unchanged byte prefix, and the diff contains no fixture, lock, accepted-evidence or gitlink path.
- Baseline CI run `39984098562` at the entry revision already fails `ci / minimal-module platform compatibility` at `Locked restore and build the platform-backed fixture`; all other executed jobs pass. This is the separately deferred frozen fixture lock-graph failure. GitHub returned no raw job log through either run-level or job-level log retrieval, so the fresh observation establishes the failed step rather than a new native error-code capture. No lock or restore gate is changed.
- The existing `npm test` lane passes all 92 tests. A disposable hidden-copy probe confirms the archived baseline observer accepts a stale later copy while the corrected observer rejects it with MINIMAL_FIXTURE_BUILD_FAILED. All 411 tests in the three completed selections pass. Fetch confirms main and origin/main are identical before the task commit. The exact full commit message passes pinned commitlint with zero problems/warnings; retained evidence `/tmp/conversations-five-record-commitlint.log`.
- Blind Hunter returned five findings; all receive small patches. The review-fix selection passes 21 cases. Final affected suites are rerun after these patches; prior counts above describe the pre-review implementation.

## Review Triage Log

- R1 — low, patch: format discovery continued beyond argparse's `--` terminator. Stop scanning at the terminator and exercise a real CLI parser error in addition to injected early errors.
- R2 — medium, patch: fixture-owned assemblies supplied only their own directory, omitting stale consuming copies. Keep the own output first while scanning the other consuming directories too; add identical/stale fixture-copy controls.
- R3 — low, patch: the preservation snapshot omitted empty directories and directory modes. Capture both and prove creation, removal and permission changes alter the snapshot.
- R4 — medium, patch: a linked worktree stores its index outside its root. Resolve and snapshot both its absolute Git directory and common directory, then run every passing/failing live/historical preservation case in ordinary and linked worktrees.
- R5 — low, patch: two-copy coverage would not detect stopping at a second identical copy. Add three-copy controls with equal first and second copies and a differing third; preserve published output bytes on failure.
- R2 follow-up — medium, patch: the first review patch could select a consuming copy when the owning project's Release DLL was absent. Require that owning output before comparison and cover missing-own output alongside identical/stale consuming copies. The independent recheck confirmed the other four findings closed.

## Final Verification

- Independent recheck confirms all five findings closed, including the owning-output correction; no new deferral remains.
- Final affected generator selection: 131 passed, zero failures/errors/skips; `/tmp/conversations-five-record-generator-final.xml`. Final complete fixture-proof suite: 111 passed, zero failures/errors/skips; `/tmp/conversations-five-record-fixture-postreview.xml`. The unchanged final promotion implementation passed all 90 cases in the earlier combined suite; npm tooling passed all 92. Total final coverage: 424 passing tests.
- Both Story 7.3 operational workflow checks, the current indexed root-submodule check and whitespace pass. Ledger prefix and protected tracked paths remain unchanged. Final fetch shows zero divergence; pinned commitlint accepts the exact full message. Historical evidence and generated final records are not-applicable to this routine unnumbered batch. Push to main is explicitly authorized; the existing baseline minimal-module locked-restore failure remains separately deferred.
