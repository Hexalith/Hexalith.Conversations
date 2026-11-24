---
title: 'Reject live record measurements from a drifted dependency checkout'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

A live v1 story record can report candidate-bound results while an unrelated initialized dependency is checked out at a different revision. Require the embedded promotion check to reject that mismatch for live record generation. Exercise a matched and a divergent checkout through the public CLI.

</frozen-after-approval>

## Implementation Notes

- The live generator blocks the embedded checkout mismatch and inspection-error warnings. The disposable CLI regression exercises a matching pin, a behind checkout, and a behind checkout with an unreadable index.
- The focused pytest selection passed all three cases, including the existing unevaluated promotion control. This inspection is an observation of the current checkouts. Builds and tests must be rerun after aligning a mismatched dependency.

## Review Triage Log

- Medium, fixed. The generator blocks failed inspection even when the promotion checker cannot report the mismatch directly.
- Medium, fixed documentation. Recovery requires rebuilding and retesting after aligning the checkout. The existing v1 manifest does not capture dependency identities at test execution.
- Low, fixed. CLI remediation directs the reader to the embedded diagnostics and identifies the remeasurement required after drift.
- Medium, deferred. A checkout can move after the embedded inspection. Closing that window requires binding dependencies to measured execution and covering concurrent checkouts.
