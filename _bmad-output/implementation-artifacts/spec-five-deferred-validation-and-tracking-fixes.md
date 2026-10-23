---
title: 'Close five deferred validation and tracking gaps'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'b2fa7d93c1a8c0eca6c02fe1c7ecb9418ae5bee4'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred repository-validation and tracking gaps remain: the root submodule checker lacks duplicate-path coverage and compares working-tree declarations with staged gitlinks; the retained PowerShell TRX checker lets unrelated failures alter a green receipt's status and accepts substring contract-test identities; the sprint header presents historical V14 planning state as current.

**Approach:** Complete those five ledger entries with focused regression coverage and a provenance-preserving header correction. Check declarations and gitlinks from the same index, reject ambiguous declarations, isolate each TRX validation's failures, and require exact case-sensitive test identity while retaining compatibility with bare method selectors. Retire the obsolete header's present-tense assertions and point to the current-change policy and actual tracking rows. Preserve all lifecycle values, historical evidence, accepted manifests, dependencies and gitlinks. Validate locally, review through the build workflow, commit with the pinned Conventional Commit validator, push to main as requested, and inspect CI.

</frozen-after-approval>

## Implementation Notes

- Investigation: clean main at the full baseline above. The user confirmed earlier workspace changes were pushed. The requested batch explicitly contains five deferred items; it introduces no public API, dependency changes, migration or deployment. Its small footprint and settled intent use the one-shot route.
- Sources: September 25 Story 7.2 duplicate-path and staged-declaration entries; October 8 Current Blind 9 and 10; October 1 stale V14 sprint-header entry (also reported October 11). The final release-plan recheck and temporary contract-snapshot output already exist and are not counted again.
- `scripts/check-root-submodules.py::check`: use staged `.gitmodules` via Git's config blob reader, requiring a resolved regular staged file. Keep index mode/identity validation and root-only behavior. Adapt existing disposable fixtures to stage declarations. Add duplicate paths under separate names, repeated path fields, contradictory staged/worktree declarations in both directions, absent worktree declarations, and missing/conflicted staged declarations.
- `tests/Test-StoryFinalRecord.ps1::Test-TrxResult`: record the caller failure count on entry and use only added failures for this invocation's status. Match a qualified selector against the entire native test name, or a bare method against the final dot-separated segment, case-sensitively; never use wildcards. Extend its disposable PowerShell suite with wrong prefix/suffix/case, fully qualified controls, duplicate exact identities and direct function checks for pre-existing versus new errors.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: replace only the obsolete V14 status comment with historical provenance and current-policy pointers. Do not change dates or lifecycle rows for a comment correction. Verify parsed YAML data is unchanged.
- Verification: root-checker unittest suite and real index check; PowerShell disposable suite; existing `npm test`; both Story 7.3 workflow-surface checks; whitespace. Verify preserved artifacts against the entry revision. Historical evidence-boundary and contracted final-record gates are explicitly not-applicable to this routine unnumbered batch. No historical receipt is regenerated.
- Implemented all five scopes. The root checker now uses the indexed blob and validates its mode/stage; all ten root-checker tests pass. Sprint non-comment bytes match the entry revision exactly. Added twelve direct PowerShell helper scenarios alongside the original disposable integration scenarios, and updated their adjacent test documentation. The additive ledger records only these five resolutions.
- Both Story 7.3 workflow checks pass with their exact operational output paths under `artifacts/v9/7.3/`. Initial attempts to redirect those outputs to `/tmp` or another artifacts directory returned `ARGUMENT_INVALID` (exit 2); rerunning the contracted commands succeeded (exit 0), with no source change or accepted-evidence publication. These are untracked operational checks, not regenerated historical acceptance receipts.
- Initial PowerShell verification passed all 37 scenarios. Existing `npm test` passed all 88 tests, including the ten root-checker cases at that point, plus its seven-case compatibility-result check. Three in-memory root-checker mutants independently proved that the staged-tree, duplicate-path and repeated-setting regressions reject removal of their respective guards. No source file was mutated for those controls.
- Review corrections add ordinal identity comparison, four valid-XML Unicode lookalike negatives, two distinct-class ambiguity/disambiguation controls, a real three-stage index conflict, and staging instructions in the routine runbook. All eleven final root-checker tests pass. An attempted nonregular-mode fixture failed during setup because Git itself rejects a symlink `.gitmodules` (`git update-index --cacheinfo 120000,...,.gitmodules`, exit 128); removed that unreachable fixture rather than mocking a supported Git state into existence.
- Baseline remote CI run 39971222910 at the entry commit already reports a frozen minimal-module locked-restore failure: `dotnet restore tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --locked-mode` reports NU1004 for changed EventStore ServiceDefaults/Aspire dependency graphs. The same job's seven external compatibility assertions pass. This batch preserves the accepted fixture/locks and dependency pins, and does not claim to repair that independent baseline blocker.
- Final focused verification passed: 43 PowerShell scenarios, eleven root-checker tests, the real indexed root check, and both Story 7.3 workflow checks. The existing npm tooling lane passed 88 tests before the last isolated review additions; the affected root and PowerShell suites were then rerun successfully. Ledger entry bytes remain an unchanged prefix, and every non-comment sprint byte is unchanged. Whitespace passed. Full candidate commit message passed the pinned commitlint CLI with zero problems/warnings; log `/tmp/conversations-five-validation-commitlint.log`. Fetch found no main divergence.
- The exact baseline CI restore step sets `fixture=tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx` and `compatibility=$PWD/tests/compatibility/MinimalModule/RegisteredCommand.targets`, then runs `dotnet restore "$fixture" --locked-mode -p:Configuration=Release "-p:CustomAfterMicrosoftCommonTargets=$compatibility"`; it exited 1 with NU1004. Baseline build-and-test, conformance, repository, release-tooling, admin-web and Windows TestSupport jobs pass. Product sources, accepted fixture/locks, package pins and gitlinks remain unchanged in this batch.

## Review Triage Log

- R1 — medium, patch: PowerShell `-ceq` ignores certain Unicode characters, allowing lookalike native names to satisfy the exact-identity claim. Replaced both branches with ordinal string equality and added U+00AD/U+200B controls for bare and qualified selectors.
- R2 — low, patch: The routine runbook did not say that the checker evaluates staged declarations and gitlinks. Added explicit exact-path staging guidance before the check.
- R3 — low, partial patch: Added a genuine three-stage declaration conflict to cover the multiple-entry branch. The nonregular-mode fixture cannot be created through Git (`Invalid path '.gitmodules'`, exit 128); rejected that portion as an unsupported fixture state. The checker retains its defensive regular-file mode check.
- R4 — medium, patch: Existing duplicate coverage also trips the independent duplicate-native-identity guard. Added two distinct classes sharing a method: bare selection must fail as ambiguous; qualified selection must succeed without unrelated duplicate errors.
- R5 — low, defer: A comment-only configuration already produced Git config exit 1 before this batch. The CLI catches the resulting ValueError and fails closed; the defect is an unhelpful diagnostic, not acceptance of undeclared gitlinks. Recorded a separate follow-up without expanding this batch's five selected scopes.
