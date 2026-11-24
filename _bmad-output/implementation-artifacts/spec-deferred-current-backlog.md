---
title: 'Continue the deferred work ledger'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Work through the deferred ledger in document order. Apply and test outstanding changes owned by Conversations, append a disposition for each completed or skipped item, and commit each completed item separately. Run the full current build and maintained test projects, then push the verified commits to main.

</frozen-after-approval>

## Implementation Notes

- The Story 8.2 fixtures in `_bmad/scripts/tests/test_generate_story_record.py` exercise the current verifier against its accepted candidate. Use the candidate Git tree to supply historical inputs, including the retired C# guard, and keep the original scope and fault checks active.
- Story 11.2 build receipts use the SDK and warning policy evaluated by MSBuild. The generator and verifier agree on the receipt value, and the accepted legacy receipt provides a native SDK diagnostic. The proof and final-record tests use the measured fixture source and lock files from the checkpoint.
- The ledger records 20 confirmed remaining Conversations follow-up groups. The original entries also contain prior closures, obsolete routes and dependency-owned work, so the group count does not represent a count of ledger bullets.

## Review Triage Log

- Medium. Both historical fixture lanes use checkpoint sources and locks. Negative tests retain their original scope and receipt checks.
- Medium. The receipt uses evaluated SDK versions, rejects conflicting or missing observations and replaces a previous valid receipt when a property probe times out. Build results retain the actual process exit. A successful build with invalid SDK evidence exits nonzero from the wrapper.
- Medium. The receipt reader rejects duplicate JSON keys and malformed versions. The tests cover the accepted legacy diagnostic form and a fresh measurement with a different SDK version.
- Medium. The runbook records the limits on reproducing a proof from an earlier receipt that contains no SDK observation.
- Medium. The Story 11.2 Git fixtures retain the full commit and index while omitting unrelated archived build outputs from the working tree. The proof verifier reports invalid receipts by scenario, and a regression checks the accepted receipt digest and SDK against the published proof.
