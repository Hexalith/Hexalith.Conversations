---
title: 'Five deferred record portability safeguards'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five deferred verification scopes: reconcile generated historical File Lists with live record-output self-accounting, prevent a passing live record from combining different root HEAD observations, replace historical tests coupled to workspace commits with disposable repository fixtures, cover corrupt unrelated submodule gitdir metadata, and cover case-mismatched promotion paths. Verify the changes, append precise ledger resolutions, commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry tree is clean on main at `93be6f2f52a7b4a8083208270f3c8bbe87613d30`. The user authorized this five-item batch and pushing to main. This small change is confined to existing Python tooling, two test modules, operational documentation, this spec and the append-only ledger. No dependency, deployment, data migration or public API change is needed.
- The first scope is the October 23 measurement-review R5 finding. Preserve exact committed-range equality for sources and gitlink exclusion; account explicitly for the story and its optional sibling sprint output. Preserve pre-generator historical behavior and accepted record bytes. Regressions must include equal baseline/candidate, normal ranges, absent/present sprint outputs and extra source-path rejection.
- The second scope is the July 28 and July 30 pass-8 unsnapshotted HEAD finding. Pin the root head used by staged-delta and candidate-binding inspection, keep the embedded checker on the canonical candidate, and reject a head change detected before returning a live verdict. A deterministic disposable race probe must block while an unchanged head passes. This is detection of observed concurrent movement, not a repository lock or atomic filesystem snapshot.
- The third scope is the July 28 test hermeticity finding. Replace live-workspace historical classification/count assertions and the hard-coded 37-path range with independently constructed disposable pre-generator/generated records and exact known inventories. Preserve native CLI invocation and read-only checks; retained native receipts stay untouched.
- The remaining two scopes close the July 27 case-mismatch and pass-2 corrupt-gitdir verification gaps. Current production code already rejects undeclared casing and warns UNRELATED_SUBMODULE_UNINSPECTABLE; add regression evidence without claiming new production behavior. Preserve nonblocking unrelated state and noninitialization.
- Reuse verify_live, worktree_path_status, derive_file_list, evaluate_candidate_binding and verify_historical in _bmad/scripts/generate_story_record.py. Extend disposable umbrella/CLI helpers in its existing test module. Checker regressions belong in _bmad/scripts/tests/test_verify_submodule_promotion.py. Document changed semantics in docs/runbooks/story-final-record-generation.md.
- Follow current-change validation, focused Python checks, both Story 7.3 operational guards, whitespace and pinned commitlint. No dotted epic story or effective evidence contract applies; historical evidence-boundary and final-record publication are not-applicable. Preserve prior ledger entries, accepted evidence, fixture locks, sprint lifecycle and gitlinks.
- Baseline CI run 40014677675 already fails. Inspect its failed job before attributing any pushed result to this change; the prior batch records a separately deferred frozen fixture lock-graph mismatch.
- Five new fault probes first failed: three equal-baseline output cases returned FILE_LIST_DRIFT, while both deterministic concurrent-head cases incorrectly passed (`/tmp/conversations-five-portability-before.xml`). The initial corrected selection passed all nine cases; expanded output coverage now includes normal ranges too and passes all eight race/output cases (`/tmp/conversations-five-portability-focused-final.xml`).
- The promotion-checker suite passes all 94 cases (`/tmp/conversations-five-portability-promotion.xml`), including four new coverage cases. Two existing pytest JUnit record_property warnings are unrelated to the change. Both Story 7.3 operational guards pass (33 and 22 assertions); the indexed root checker and whitespace checks pass. No evidence-boundary or governed final-record contract applies.
- Baseline CI run 40014677675 failed only the frozen fixture's locked restore/build step; all other executed jobs passed. This confirms the separately recorded pre-existing lock-graph limitation.
- The expanded pre-review v1 lane passes 142 cases (`/tmp/conversations-five-portability-final-v1.xml`); its exact function inventory is `/tmp/conversations-five-portability-final-v1-nodes.txt`. Shared v2 checks pass 80 cases (`/tmp/conversations-five-portability-v2.xml`), exercising default helper behavior and the v1 compatibility route.

## Review Triage Log

- R1 — medium, patch: a recorded sibling sprint output could override a candidate directory mode. The independent reviewer reproduced a passing forged entry. Reject incompatible known directory/gitlink modes; native CLI controls now include both shapes and an untampered passing record for each.
- R2 — medium, patch: the positive sprint cases did not prove the candidate-mode fallback. Add omission probes for a committed regular sprint output in normal and equal-baseline ranges, requiring FILE_LIST_DRIFT and the exact missing sibling path.
- R3 — medium, patch: old-range fixtures did not distinguish pre-generator output semantics. Add an equal-baseline pre-generator record with an unchanged committed sibling sprint and assert an empty derived inventory and unchanged repository state.
- R4 — medium, patch: generated roundtrips alone would not protect older valid shapes after removing workspace-history tests. Add a manually authored legacy seven-column record with a legacy hash prefix and no build manifest, independent of current generation/rendering helpers.
- R5 — medium, patch: the replacement positive fixtures lacked changed gitlink promotions. The independent legacy fixture now promotes a real disposable submodule commit, verifies both canonical commit identities and raw modes, and excludes the gitlink from the exact File List.
- R6 — medium, patch: addition-only inventories missed source removals. The independent fixture covers deletion and rename, checking deleted paths and both rename paths against an explicit inventory. These fixtures preserve repository state around historical verification.

## Final Verification

- The post-review focused selection passes 13 cases (`/tmp/conversations-five-portability-review.xml`). Independent reviewer recheck passes 11 cases and confirms R1–R6 resolved, with no outstanding review finding.
- The existing npm tooling lane passes 92 tests (`/tmp/conversations-five-portability-npm.log`). Shared v2 and promotion results remain 80 and 94 passing cases; neither corresponding behavior nor its tests changed after those checks. The final affected v1 rerun is recorded below on completion.
- Pinned commitlint validates the exact full candidate message with zero problems or warnings (`/tmp/conversations-five-portability-commitlint.log`). Pre-commit fetch reports no divergence from origin/main. Push is explicitly authorized by the user. Evidence-boundary and governed final-record publication are not-applicable for this freeform routine change.
- Final post-review affected v1 verification passes 147 cases with no failures/errors/skips (`/tmp/conversations-five-portability-final-v1.xml`), with the exact inventory in `/tmp/conversations-five-portability-final-v1-nodes.txt`. Together with 94 promotion, 80 shared v2 and 92 npm tests, 413 distinct selected test cases pass; repeated focused runs are not counted.
- Exact changed-path inspection finds only the six intended files. The original ledger prefix is byte-identical to entry. Both Story 7.3 guards, the root checker and whitespace checks pass. Accepted evidence, fixture locks, dependency pins, lifecycle rows and gitlinks remain unchanged.
