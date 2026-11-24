---
title: 'Verify empty-history rejection at the rebuild receiver'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

The deferred Story 6.2 review finding calls for proof that the SDK dispatcher rejects an empty, non-erased history before Conversations writes state. Exercise the mapped rebuild receiver with the existing registered Conversations fixture.

</frozen-after-approval>

## Implementation Notes

- The tests submit the empty request to both mapped rebuild receivers after persisting a projection. Both receivers return the SDK's HTTP 400 with the malformed-history reason. The erased-source control reaches the handler and returns its typed rejection. The stored metadata and write count are preserved.
- The producer-mapped Release Server.Tests build passed with zero warnings. The native xUnit method passed both route cases.

## Review Triage Log

- Medium, fixed. An existing projection is read back after both refusals, and its backend rejects any attempted writes.
- Medium, fixed. Both mapped routes accept the erased-source zero-head shape at SDK admission, as demonstrated by the handler-level rejection.
