---
title: 'Report projection freshness and recovery state accurately'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resolve the five projection guidance entries deferred from `spec-sync-upgrade-and-release.md`. Describe the query response to mixed generations, stale and redacted data, healthy nonempty lists, and the qualifications for operational recovery. Ensure every valid observation time before the replay anchor yields a representable freshness response.

</frozen-after-approval>

## Implementation Notes

- Use the public query handler, the Story 16.2 replay contract, and the recorded per-story entry decisions as the sources for the Epic 16 context note.
- Preserve the existing Unix epoch marker for normal observations and the true anchor in internal replay state. Exercise the earliest contract-supported observation and the transition at the Unix epoch, including replay without an event.
- Limit this change to Conversations. Use its committed dependency revisions to validate the result.

## Review and verification

- Review clarified precedence of lifecycle and pending dispatch, the continuation rule for a withheld row, and the worst-match rule for stale pages. The context now identifies tenant-wide erase and shared rebuild as planned work.
- Reopened the November 4 pre-anchor disposition after the two pre-epoch cases threw on the entry implementation. The epoch boundary already passed. After the fix, all 1,738 Server tests passed with zero skips in Release against the pinned Builds catalog.
- Both Story 7.3 workflow scenarios passed. The indexed root-submodule check and local documentation link checks passed. This freeform change uses routine current-change validation.
- The AppHost compiled, but the Aspire CLI could not start it because its per-user cache directory was read-only. The shared pre-push checker on the clean baseline could not clone its anonymous GitHub policy branch in this environment.
