---
title: 'Complete the next deferred-work items'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Continue the deferred-work ledger in its recorded order. Complete ten actionable items and give each its own tested change and commit. Record the disposition of any earlier item passed over. Build and test the batch, then push the verified commits to `main`.

</frozen-after-approval>

## Implementation Notes

- The legacy ledger records entries by source, in document order. The preceding batch's dispositions continue to apply.
- The gateway fixture already allocates isolated sidecar ports and retries bind conflicts. Its test guide now states the sequencing precondition for the shared local infrastructure.
- The fixture checks cover solution isolation and the exact inventory order. Historical package, skip-policy, publication, and v7 approval checks use their versioned inputs.
- The production host coverage exercises the enabled and disabled receipt verifier registrations. Admission checks the identity emitted by the pure handler before processing, including derived quarantine identities and content edits.
- Ten item commits are tracked in the ledger. The first three reached main while the remaining work was in progress. The fixture's stronger restore-graph check has its own follow-up commit.
- The audited package-mode Release solution build passed with zero warnings and errors. Running the maintained projects individually exposed a wall-clock timer in the backfill fixture and a stale EventStore version in the AppHost provenance test. The corrected backfill class passed all nine tests. All 21 AppHost tests passed with the live lane enabled. The gateway integration project passed all 54 tests and Admin Web passed all 14.
- The mapped Python suite passed 20 tests and 18 subtests. The affected historical tooling suite passed 24 tests. The isolated minimal-module compatibility suite passed all seven tests.

## Review Triage Log

- Medium. The mapped fixture isolation check now evaluates the SDK's actual solution restore graph. The unmapped preflight supplies solution properties to project evaluation.
- Medium. Missing committed v7 approval now has the specific diagnostic even when an exact untracked copy exists. The native v4 command reproduced the refusal at its accepted source pin.
- High. Fresh restricted events that consume a submitted identity now require the canonical allocation. Derived events are checked against the same retained prefix, and the no-op membership regression passed.
- Medium. Standalone v1 historical finality uses the first publication carrying the exact generated block and applies through HEAD for drafts. Tests exercise source drift in both states and later unrelated history.
- Medium. The frozen v2 history verifier uses its existing closure contract. Its historical test selection passed after the standalone check was introduced.
- Medium. Live and historical v1 generation both bind skip approvals to candidate frontmatter. The tests refuse dirty policy changes and tampered recorded identities or reasons.
- Low. The denial release notes use the public wire property `isRetryable`.
- Low. Historical package-profile instructions specify the clean Builds checkout and required cached remote reachability.
