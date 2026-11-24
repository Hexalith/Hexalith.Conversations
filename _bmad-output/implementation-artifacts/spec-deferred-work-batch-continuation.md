---
title: 'Complete the next deferred-work items'
type: 'chore'
created: '2026-11-24'
status: 'in-progress'
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
- Ten item commits are tracked in the ledger. Full verification and publication follow on the committed batch.
