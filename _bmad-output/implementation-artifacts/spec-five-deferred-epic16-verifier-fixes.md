---
title: 'Close five deferred Epic 16 verifier findings'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Make the Epic 16 verifier report outcomes supported by its measurements. Require final runtime qualification to agree with the five scenario receipts. Check retained file aliasing on the descriptor that supplies the bytes. Accept either measured Dapr control-plane endpoint in collision evidence while preserving the two-replica denial control. Prove replay follows the owners' behavior for empty attribute updates and absent tenant members. Report invalid verifier arguments as a structured failure while preserving interactive help.

</frozen-after-approval>

## Implementation Notes

- A final pair requires all five passing rows to report the requested scope and its corresponding Boolean qualification. The pair copies the qualification from those agreed results.
- The contained reader checks the link count on the opened file before and after reading. The capture reader maps hard links to the existing `NATIVE_CAPTURE_ALIAS` outcome.
- The fault matrix accepts placement or scheduler and requires the captured state, restored endpoint, emitted diagnostic and cleanup port to agree with the selected runtime endpoint.
- `ConversationState`, both projection implementations, and the retained Tenants `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs` at `3b60914ace5201a711fb51c1036d3679e0512ec8` establish the replay expectations. The Python regressions exercise the verifier's handling of those inputs.
- The Epic 16 parser reports invalid arguments using the generator's `ARGUMENT_INVALID` failure schema. Help still exits through argparse.

## Review Triage Log

- Medium, patched. Invalid invocations now conform to the closed generator failure schema and use a fixed diagnostic that omits supplied values.
- Medium, patched. An explicit Epic 16 scope or recognized contract reaches the Epic 16 parser even with missing or repeated arguments.
- Medium, patched. Singleton options reject repetition.
- Low, patched. Invalid argument diagnostics omit caller values and point to the help output.
- Medium, patched. Inserted-record verification enforces the retained reader on the final committed-byte comparison.
- Medium, patched. The dependency reread must match the digest of the passing AC-16.3-03 result.
- Low, patched. Capture read failures identify the affected artifact in the ledger.
- Low, addressed in the spec. The replay regressions pin verifier behavior and cite the retained Tenants source. `ConversationProjectionAccumulatorTest.EmptyAttributesShouldNotClearExistingProjectionAttributes` independently covers empty maps in the product.

## Verification

- The focused Epic 16 suites passed 68 cases using the pinned .NET 11 SDK and the .NET 10 runtime required by the native helper. A second run passed the scheduler cases and the native xUnit capture and fresh-checkout verification, including an alias added after inventory verification.
- The generator argument and output-format suite passed 21 cases. Commitlint validated the exact Conventional Commit message.
- Scoped evidence-boundary verification and Epic 16 record publication are not applicable to this routine change.
