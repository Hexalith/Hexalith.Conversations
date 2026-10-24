---
title: 'Five deferred successor validation safeguards'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five still-open deferred scopes: successor xUnit method selectors with parameterized TRX names, domain status fields confused with machine verdicts, incomplete scenario-count summaries, assumed solution/build output paths, and disclosure of the historical manifest checks that generic record generation does not replace. Verify, independently review, append precise ledger resolutions, commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry tree is clean on main at `8947b7ddbeaa64d7a039c58a23f99d2a255d2ad0`. The user's five-item batch and push authorization cover this focused tooling change. No human-visible choice, migration, dependency change or deployment is needed. The footprint is existing Python tooling/tests, its operational runbook, this spec and the append-only ledger.
- The first four scopes are the October 3 successor findings in deferred-work.md. Reuse v2_successor_scenario_from_results and its disposable tests. Match an exact method identity before any parameter suffix; retain actual row identities in the assertion ledger and reject lookalikes. Keep passing candidate-stamped assembly, TRX counters and no-skip requirements.
- Python outputs need explicit machine evidence. A result field remains authoritative; a strictly integer zero exitCode can establish success independently of an unrelated domain status. Preserve recognized legacy verdict statuses, reject explicit failure/conflicts, boolean/noninteger exits and absent verdicts. When summary claims any of the six scenario counters, require all six, positive required/passed equality, nonnegative integers and zero failure categories. Domain summaries with other keys retain their existing meaning.
- Normalize solution Project Path separators for build and restore. Discover actual single-target assembly paths with the existing bounded read-only MSBuild evaluator in verify_conformance_tiering.py, resolved=False; never guess assembly names/frameworks. Preserve contained paths, source stamps, output hashes and freshness. Missing, malformed or ambiguous metadata fails closed with existing blockers. Regressions include custom AssemblyName/framework/output directory, Windows separators, normal defaults, foreign stamps and unsafe/unavailable metadata. Existing retained Python verification must execute no subprocess.
- The fifth scope is July 28's missing disclosure for retired immutability, required-pair and non-mutating public-contract checks. Document the generic generator's limits and separate applicable checks without introducing a new blanket gate or changing historical claims. Preserve accepted evidence and original ledger entries.
- Follow current-change validation, focused Python checks, both Story 7.3 operational guards, whitespace and pinned commitlint. This freeform spec has no dotted story contract or explicit evidence-boundary requirement; those publication gates are not-applicable. Do not alter fixture locks, lifecycle rows or gitlinks.
- Baseline CI run 40023778253 fails only the frozen minimal-module locked restore/build step; all other executed jobs passed. Preserve this separately deferred fixture limitation and distinguish it from this batch's results.
- The first result-validation run reproduced 16 failures across the three result scopes (`/tmp/conversations-five-successor-before.xml`). After the changes, all 43 probes pass (`/tmp/conversations-five-successor-results.xml`), including actual parameterized names from a retained native TRX. A scan of retained release-evidence JSON found no existing partial six-counter summaries requiring compatibility changes.
- Build discovery reproduced 15 failing cases before correction (`/tmp/conversations-successor-build-before.xml`); all 23 new cases pass afterward. Combined build/default/retained-Python checks pass 26 cases (`/tmp/conversations-successor-build-final.xml`). Native SDK 10.0.402 read-only evaluation confirms imported AssemblyName, net9.0 and custom output without restore (`/tmp/conversations-successor-native-evaluation.log`); the controlled stamped output is not a compiled binary claim.
- Both Story 7.3 operational guards pass with 33 and 22 assertions, the root checker passes and whitespace is clean. Two preliminary guard invocations rejected non-contract output paths with ARGUMENT_INVALID; rerunning their exact declared commands passes. No governed lifecycle transition occurred after those rejected invocations. Evidence-boundary and governed final-record publication are not-applicable for this routine freeform spec.
- The broader v2 lane exposed an undefined CLOSED_RECORDS in the Story 7.4 test fixture. An untouched shared clone at the full entry revision reproduces the same NameError (`/tmp/conversations-five-successor-baseline-v2.xml`). A minimal test-only repair restores the exact frozen three-record inventory under V2_HISTORY_RECORDS and updates its residual uses. This preserves the Story 7.4 anchors and does not restore retired v1 live-history/count assertions. Accepted records remain unchanged.

## Review Triage Log

- R1 — medium, patch: solution normalization could erase a lexical root-gitlink boundary. Three red alias probes now pass after checking lexical and physical boundaries for both project inputs and evaluated outputs. No real submodule was traversed or modified.
- R2 — medium, patch: relative TargetPath metadata was interpreted from the repository root. It now uses the owning project directory, with a positive regression.
- R3 — medium, patch: different projects could bind one output and violate outputFiles uniqueness. Reject duplicate resolved target paths as ambiguous output identity; a two-project distinct-output control remains passing.
- R4 — medium, patch: symlink loops could escape path-error handling. Strict solution-path resolution and bounded exception translation produce OUTPUT_PATH_INVALID, including build and restore regressions.
- R5 — medium, defer: solution-to-project BuildType configuration mapping is a native-reproduced pre-existing gap in both baseline and new discovery. Record it separately rather than adding solution configuration semantics to this batch.
- R6 — medium, defer: projects excluded from a solution build are still included by generic discovery, also reproduced at baseline. Record a separate effective-participation follow-up.
- R7 — medium, defer: generic restore still assumes obj/project.assets.json instead of evaluated custom intermediate output. This pre-existing restore-metadata scope is separate from the original solution separator/build TargetPath findings. Record it separately and disclose all three limitations in the runbook.
- The independent reviewer confirmed the small frozen-inventory test repair preserves the original Story 7.4 identities and mutation assertions. The post-review build suite passes 31 cases (`/tmp/conversations-five-successor-build-review-final.xml`).
- R2 recheck also covered native parent-relative TargetPath metadata. Normalize repository-contained parent segments and Windows separators while preserving raw, normalized and physical boundaries and rejecting normalization that changes the actual target. All 76 new result/build cases now pass (`/tmp/conversations-five-successor-new-final.xml`); independent native evaluation confirms the relative and parent-relative cases. Reviewer recheck reports no remaining R1–R4 finding.

## Final Verification

- The expanded 618-case v2 run initially passed 567 and exposed 51 pre-existing fixture failures (`/tmp/conversations-five-successor-v2.xml`). Forty-nine were the removed historical inventory; two were Story 9.2's insertion fixture lacking a newly required disposable sprint row. Both failure classes reproduce at the untouched entry revision. The fixture repairs preserve all original assertions and accepted evidence; independent review confirms both repairs.
- All 49 historical cases pass in four independent partitions (`/tmp/conversations-five-successor-rerun-0.xml` through `-3.xml`). Both insertion cases pass (`/tmp/conversations-five-successor-retention-repair.xml`), preserving the exact rederivation call and content-drift rejection. The replacement identity set equals the original 51 failing cases exactly; the 567 other cases remain passing. This was focused revalidation of affected fixtures, not a second unfiltered 618-case run.
- The final focused successor regression run passes 120 cases (`/tmp/conversations-five-successor-verified-final.xml`). The existing npm tooling lane passes 92 cases (`/tmp/conversations-five-successor-npm.log`). Across the 618 existing cases, 76 new cases and 92 npm cases, 786 distinct selected cases pass; repeated runs are not double-counted. The merged identity/count check is retained in `/tmp/conversations-five-successor-verification-summary.json`. The broad run's 26 pre-existing JUnit record_property warnings do not affect its assertions.
- Both Story 7.3 guards, root checker, whitespace and pinned commitlint pass. Evidence-boundary and governed final-record publication remain not-applicable. Only seven intended files change, the ledger's original prefix is byte-identical, and accepted evidence, fixture locks, dependency pins, real sprint rows and root gitlinks remain unchanged. Push to main is authorized; baseline CI's separately tracked frozen fixture restore/build failure is unchanged by this tooling batch.
