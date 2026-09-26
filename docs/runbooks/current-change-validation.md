# Current Change Validation

This policy takes effect when the change that adds this file is pushed to `main`. It governs
future changes to the Conversations repository, including ordinary root submodule
updates. It does not change the meaning or bytes of earlier V23–V29 records.

## Routine path

1. Make a focused change on `main`. Run the relevant local checks before committing,
   including `python3 scripts/check-root-submodules.py --repository .` for root
   submodule changes.
2. Push to `main` after those checks pass. CI then builds and tests the product.
   The `ci / repository` job checks that
   each root `.gitmodules` declaration has exactly one mode-`160000` gitlink in
   the checked-out index and that no undeclared path appears under `references/`.
   It reads neither nested submodules nor historical authority records.
3. For a gitlink update, review the before and after commit IDs in the staged
   Git diff and explain the affected paths and reason in the commit message.
4. Check CI after the push. Fix a failed product or repository check before the
   next change. Keep linear history and disallow force pushes and deletion.

CI reports the state of a direct push after it lands. GitHub branch protection
does not require a pull request or a status check for this single-contributor
workflow.

Repository-local BMad build and review workflows use this current-tree check
and focused tests for new routine work. Generated final records and historical
evidence checks run only when a spec explicitly requires them. The pre-commit
hook checks staged whitespace; the commit-message hook still enforces the
repository's Conventional Commit policy.

## Historical boundary

The V23–V29 publishers, resolver, evidence verifier, schemas, records, and their
tests remain in Git for historical inspection. They are not required for future
pushes, CI, or routine lifecycle transitions. Do not create another
authority successor solely because a new commit touches a path they governed.
The default Python test selection is current repository tooling; invoke a
historical authority test file explicitly when reproducing its result.
The V29 result for `0bb017e641eb7bd03864466526c4918f9c55ef70` remains a
historical `FAIL`; this policy makes no retroactive `PASS` or authorization claim.
The recorded V29 hold remains `ACTIVE`, with all four authority flags false.

The old [evidence-boundary runbook](evidence-boundary-validation.md) is retained
for reproducing historical results. It is no longer the operational route for
new work. Release decisions and product safety checks continue to use their own
specific requirements.

## GitHub branch setting

Keep the existing linear-history requirement, force-push ban, and deletion ban
on `main`. Do not add a pull-request requirement or required status check while
direct pushes are the normal workflow. CI failures require follow-up fixes but
cannot prevent the push that triggered them.
