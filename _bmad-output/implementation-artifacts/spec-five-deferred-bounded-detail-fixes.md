---
title: 'Five deferred bounded conversation detail fixes'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Apply the five verified findings BH-01, BH-02, BH-03, BH-04, and BH-07 from the Story 12.2 build review. Bounded detail should preserve its purpose and authorized data from the owner through to the client. Use the isolated advisory read for advisory detail and carry its own freshness. Exercise the client, owner, and caller admission paths with focused tests. Record the five resolutions in the deferred-work ledger.

</frozen-after-approval>

## Implementation Notes

- Entry revision is `dfa04bd67464bc2be6712808d5df369d43e11d01`. The five findings cover one existing detail flow and require no schema migration or new public surface. The user requested direct publication to `main` after verification.
- `ConversationClient.GetConversationAsync` will verify bounded purpose, identity and permitted response fields. Preserve ordinary detail and typed content-free responses. Use `ConversationClientTest` to cover supported bounded results and unexpected owner responses.
- `ConversationReadApi` and `PilotQueries` will share the existing pilot Boolean vocabulary for single `true` or `false` selectors. Reject invalid, repeated, and conflicting selectors before reading projections. Cover the existing identity-header rules at the same boundary.
- `ConversationQueryHandler` will route advisory detail through `GetConversationAdvisoryMetadataDomainQueryHandler` before any native read. Reuse the two Read decisions and isolated metadata checks. Carry the accepted advisory source's freshness internally and preserve the standalone advisory query's wire shape.
- `ConversationQueryHandlerTest`, `ConversationDomainQueryDispatchTest`, `ConversationReadApiTest`, and existing SDK dispatch fixtures provide coverage for rich projections, unavailable native data, stale advisory metadata, and verified Projects scope. Historical evidence verification is not applicable to these current product fixes.
- Both HTTP routes retain the pilot selector vocabulary. The SDK detail reader rejects duplicate JSON properties using detail-specific options. Client validation preserves typed hidden, rebuilding, and unavailable outcomes with local guidance.
- The bounded owner read checks the isolated row against a completed generation proof when one exists. The standalone advisory query retains its existing acceptance rules and response shape.
- The authenticated SDK fixture uses the real `/query` endpoint, workload and caller verification, catalog receiver guard, and a local catalog snapshot. The suite covers a verified Projects caller with and without `projects.read`, signed duplicate selectors, and advisory detail with no native projection.

## Review Triage Log

- R1, medium. Normalize bounded response guidance and check reason codes on content-free outcomes. Client cases exercise normalization and an inconsistent reason.
- R2, medium. Require the endpoint's HTTP status for the corresponding detail outcome. A visible body on HTTP 503 is rejected.
- R3, medium. Require trust-bearing native freshness and admit Current or Stale advisory observations. Positive stale advisory and invalid visible states are covered.
- R4, medium. Match bounded advisory rows to any present completed proof. The mismatch case returns unavailable while the standalone advisory tests retain their existing behavior.
- R5, medium. Reject duplicate SDK detail payload properties before dispatch. Signed HTTP coverage observes no projection access for a repeated selector.
- R6, medium. Exercise the advisory detail route over authenticated SDK HTTP. The test verifies isolated freshness, zero native reads, and omitted rich fields.
- R7, low. The existing native envelope test exercised a positive result. It now checks the project link, omitted rich fields, and zero hydration calls.
- R8, low. Added five resolutions after verification and confirmed the original ledger is a byte-identical prefix.
- Independent recheck found no remaining issue with the reviewed code and tests.

## Verification

- The final Release test assemblies passed 1,725 server tests and 56 client tests. The server run includes authenticated SDK HTTP cases, owner read tests, and both detail API entry points.
- Fresh consumer restore and compilation passed against locally packed Client and Contracts releases. Both Story 7.3 scenarios passed with 33 and 22 assertions, and the root submodule and whitespace checks passed.
- The baseline Platform AppHost failed to start under the available package mapping with `HXW006`. A fresh package preparation returned `HXW011` because Platform pin `44dd1e52904cd07ef49be74edd947c75f9b76374` has no release tag after fetching upstream tags.
