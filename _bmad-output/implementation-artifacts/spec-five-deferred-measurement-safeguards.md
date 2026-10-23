---
title: 'Five deferred measurement safeguards'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five remaining July 30 pass-8 deferred scopes in story-record measurement: reject an embedded promotion result that evaluated no scope; prove require-remote forwarding behavior; preserve repeated skipped-test occurrences through live generation and historical verification; represent unavailable execution counts in legacy seven-column tables without inventing evidence; and exercise TRX parsing against retained native receipts. Verify the changes, append precise ledger resolutions and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry tree is clean on main at `b5407828ba39729286dda6efa2f3f963f77f873c`. The user explicitly requests five deferred items and authorizes committing and pushing. This small correction affects the existing generator, its tests, the runbook, this spec and the append-only ledger. No public API, dependency, data migration or deployment change is needed.
- Independent investigation confirms the vacuity, skip roundtrip and missing legacy executed-count issues. Remote forwarding already works but lacks regression coverage. Native TRX coverage is a separate named sub-scope of the original untested-branches entry. Previously fixed bold-header classification, File List annotation precedence and exclusive temporary-file creation are not counted again.
- Reuse `run_promotion_gate`, `verify_live`, `parse_recorded_test_results`, `parse_recorded_skip_policy`, `verify_historical` and `render_markdown` in `_bmad/scripts/generate_story_record.py`. Keep existing blocker codes and preserve the embedded checker document. Preserve the global exact-test-name skip approval policy; assembly-specific authorization remains a separate deferred decision.
- Extend `_bmad/scripts/tests/test_generate_story_record.py` using its disposable umbrella, native CLI and block insertion helpers. Retain native receipt bytes untouched; use their actual parser inputs and copied fault variants. Cover positive controls and failed remote, vacuous scope, missing/tampered skip counts and legacy/mixed table shapes.
- Follow current-change validation, focused Python checks, both Story 7.3 operational guards, whitespace and pinned commitlint. No dotted epic story or effective final-record contract applies; historical evidence-boundary and final-record publication are not-applicable. Preserve accepted records, fixture locks, lifecycle tracking and gitlinks.
- Baseline CI run `40003097087` fails only `ci / minimal-module platform compatibility` at `Locked restore and build the platform-backed fixture`; all other executed jobs pass. This pre-existing frozen fixture lock-graph failure remains separately deferred and is not counted as a result of this batch.
- The new focused selection first measured four failures and five passing controls (`/tmp/conversations-five-measurements-before.xml`), reproducing the three behavior defects independently of the two coverage-only scopes. All nine focused cases now pass (`/tmp/conversations-five-measurements-focused.xml`). The skip table retains yes/no for historical single occurrences, and execution counts absent from historical tables are represented by null rather than inferred arithmetic.
- Before review, all 132 affected v1 cases and 60 shared v2 cases passed. The existing npm tooling lane passes all 92 tests. Both Story 7.3 operational guards pass with 33 and 22 assertions; the indexed root-submodule checker and whitespace check pass. Review fixes require a final affected rerun below.

## Review Triage Log

- R1 — medium, patch: historical verification discarded parsed skip occurrence counts, so rendering its passing document changed repeated observations to no. Retain the occurrence map in historical JSON and reuse it during rendering. One/two-occurrence regressions now render, reinsert and verify again.
- R2 — medium, patch: accepting em dashes in modern eight-column execution fields let measured evidence disappear. Only seven-column project rows may lack execution evidence. Historical rendering preserves the legacy layout and discloses Executed as unavailable; modern missing execution and mixed layouts fail closed.
- R3 — medium, patch: pre-existing unchecked numeric execution values were exposed by the adjacent unavailable-evidence change. Require nonnegative counters and numeric Executed equal to Passed plus Failed. Historical CLI regressions reject negative and inconsistent values even with matching tampered totals.
- R4 — medium, defer: historical skip-table identities/reasons are not compared with the versioned frontmatter policy. A disposable probe accepts a renamed identity and fabricated reason. This predates occurrence counting and is a separate policy-source binding scope; retain the finding in the ledger.
- R5 — medium, defer: equal baseline/candidate with explicit promotion scope can pass live generation but fail historical File List verification because live output self-accounting includes the story while the committed range is empty. A disposable probe confirms FILE_LIST_DRIFT. The selected fix governs promotion-scope evaluation, and this pre-existing live/historical file-boundary mismatch remains a separate ledger scope.
- The post-review focused selection passes all 12 cases (`/tmp/conversations-five-measurements-review.xml`). No review finding remains untriaged.

## Final Verification

- The independent recheck confirms R1–R3 resolved with no new actionable regression. Its additional disposable probe combines repeated skips within one project with legacy execution counts and survives rendering, insertion and historical verification. R4/R5 remain separately deferred as documented above.
- Final affected v1 selection passes all 135 cases with zero failures/errors/skips (`/tmp/conversations-five-measurements-final-v1.xml`). Its exact inventory is `/tmp/conversations-five-measurements-final-v1-nodes.txt`, reproducible by collecting test functions before the first v2 schema-contract test in the existing module. Shared v2 and npm results are recorded below once complete.
- Both Story 7.3 operational guards pass, with 33 and 22 assertions. The indexed root-submodule checker, whitespace and full original-ledger-prefix checks pass. Only this spec, the additive ledger, generator, generator tests and runbook change. Accepted receipts, fixture locks, dependency pins, sprint tracking and gitlinks remain unchanged.
- The pinned commitlint 21.2.3 validates the exact full candidate commit message with zero problems or warnings (`/tmp/conversations-five-measurements-commitlint.log`). Final pre-commit fetch reports no main/origin divergence. Push is explicitly authorized. Historical evidence-boundary and final-record publication are not-applicable; the separate baseline frozen-fixture CI failure is unchanged by this batch.
- Final shared v2 selection passes all 60 cases with zero failures/errors/skips (`/tmp/conversations-five-measurements-final-v2.xml`); the existing npm tooling lane passes 92 cases (`/tmp/conversations-five-measurements-npm.log`). Combined with the 135 final v1 cases, completed coverage is 287 passing tests without counting repeated focused runs. The exact v2 inventory is `/tmp/conversations-five-measurements-v2-nodes.txt`.
