# Final Adversarial Re-review — Architecture V16 Pragmatic AR-15 Recovery

- **Review date:** 2026-09-19
- **Target:** corrected Architecture V16, Epic 7 AR-15 route correction, and compiled Epic 7 context
- **Lens:** adversarial divergence and bypass construction
- **Trust premise:** the sole repository owner's human review plus independent raw-Git inspection is the intentional AR-15 trust root
- **Constraint honored:** no GitHub ruleset, external validator service, nonce, no-bypass claim, external check identity, or second approver is required or proposed

## Final Verdict

**PASS — no material adversarial divergence remains under the stated trust premise.**

This verdict supersedes the earlier FAIL review. The corrected authority now
defines one convergent, pragmatic bootstrap route without reintroducing the
discarded external controls. It keeps `implementationHold=ACTIVE`, completes
AR-15 only after the committed-main rerun passes, and leaves Story 7.1 locked
until a separate owner-approved AD-4 `EXECUTION_ALLOWED` authority exists.

## Adversarial Closure Checks

1. **Trust root:** The repository owner's human review is explicitly the trust
   root. The owner derives graph, changed-path/mode, immutable-prefix, and
   gitlink facts from raw Git objects instead of trusting V22 declarations.
2. **Atomic graph:** V22 is exactly one commit with the selected current
   `main` commit as its sole parent. Merge, squash, rebase, multi-parent, and
   multi-commit interpretations are excluded.
3. **Exact scope:** The transaction is limited to the ordinal eight-path
   regular-file allowlist and rejects rename, mode, symlink, submodule,
   dependency, and all other path changes.
4. **Technical result versus approval:** The candidate resolver reports only
   technical facts. Candidate files, fields, messages, tests, and output cannot
   assert owner approval.
5. **Approval freshness:** The authenticated repository-host approval binds the
   emitted parent/candidate tuple and becomes stale if either SHA or current
   `main` changes.
6. **CI identity:** The owner observes the named `planning-authority` job
   complete successfully for the exact candidate SHA after running that exact
   committed resolver locally.
7. **Integration identity:** The owner advances `main` by fast-forward only to
   the approved candidate, so candidate and committed-main identities and
   trees cannot diverge.
8. **Gitlinks:** Parent, candidate, and committed `main` must have identical
   ordinal root-tree `(path, mode=160000, object-id)` tuples whose paths equal
   the root `.gitmodules` inventory, with no submodule traversal.
9. **Marker semantics:** V22 appends one complete marker, preserves V1-V16,
   pins the V16 block and V22 recovery record, carries `hold=ACTIVE`, and is
   selected immediately by the committed last-marker rule. A failed or blocked
   post-merge rerun leaves recovery incomplete rather than reviving V21.
10. **Result semantics:** `PASS`, `FAIL`, and `BLOCKED` remain distinct; every
    invocation, including caller-synthesized missing or malformed output,
    carries a nonempty assertion ledger. Every non-`PASS` result preserves
    `ACTIVE`.
11. **Historical V21 route:** V21 authority bytes remain immutable historical
    evidence. Its `ci-trust` publisher and tests are not current AR-15 gates,
    are unchanged by the eight-path V22 transaction, and are removed from the
    current preflight route and route inventory.
12. **Epic/context reconciliation:** The Epic append explicitly replaces the
    obsolete AR-15 entry-gate item and blocked-status sentence, while the
    compiled context selects V16 and carries the same one-commit,
    fast-forward, hold-preserving route.

The predecessor pins were independently rechecked: the V15 block is 35,772
bytes with SHA-256
`85c7418ca55b1c67be90eed78e280c792ce92360e0cf2817237e41a3bcdfccbe`,
and the selected V21 sidecar digest is
`296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`.
Both match the V16 marker.

## Remaining Material Findings

None.

## Publication Hygiene

The PASS assumes the V16 decision artifacts are published without unrelated
work. The existing modified `references/Hexalith.FrontComposer` gitlink is not
part of this decision and must not be included in its publication. This is a
worktree-separation condition, not an architecture defect and not authority to
stage, commit, revert, or otherwise alter that user-owned change.

## Gate Statement

Architecture V16 is a convergent substrate for the owner-reviewed V22 recovery.
AR-15 itself remains `PENDING_V22_RECOVERY`; this review does not claim that V22
exists or has passed. After V22 receives technical `PASS`, exact-tuple owner
approval, fast-forward integration, and committed-main `PASS`, the next action
is the separate AD-4 `EXECUTION_ALLOWED` authority request—not Story 7.1
implementation.
