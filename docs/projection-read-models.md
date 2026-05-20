# Conversation Projection Read Models

Story 1.7 establishes conversation summary and detail read models as derived, rebuildable state. EventStore history remains the write-side authority; projection records can be deleted and reconstructed from accepted conversation events without changing durable conversation truth.

## Public Freshness States

The approved public freshness vocabulary is:

- `Current`
- `Stale`
- `Rebuilding`
- `Unavailable`
- `Forbidden`
- `Redacted`

Only `Current` enables trust-bearing decisions. `ProjectionFreshnessV1.AllowsTrustBearingDecision()` requires `FreshnessState == Current`, `ReasonCode == current`, and `IsStale == false`. Unknown states, unknown reason codes, missing fields, contradictory timestamps, stale metadata, rebuild windows, store failures, tenant denial, and redaction do not enable trust-bearing behavior.

## Reason Codes

Public reason codes are allowlisted in `ProjectionFreshnessReasonCode`. They are safe domain trust tokens and must not expose EventStore internals, tenant authorization internals, provider payloads, checkpoint identifiers, subscription names, or storage topology.

Current Story 1.7 reason codes are:

- `current`
- `stale_threshold_exceeded`
- `rebuilding`
- `unavailable`
- `forbidden`
- `redacted`
- `metadata_missing`
- `metadata_contradictory`
- `gap_detected`
- `out_of_order_event`
- `mixed_generation`
- `poison_event`
- `metadata_write_failed`

## Projection Behavior

Projection materialization uses the public conversation event ID for duplicate/replay idempotency and the safe source position for gap and ordering checks. Duplicate and replayed events are ignored after the first accepted application. Ordered replay is deterministic. Gaps and child events observed before `ConversationCreated` degrade the public result to `Rebuilding`; they are not reported as current truth.

Mixed-tenant or mismatched-conversation events are treated as poison events. Story 1.7 chooses deterministic `Unavailable` behavior for poison events: the event is not projected into the target tenant/conversation, and the public freshness reason is `poison_event`.

Projection mutation and freshness metadata must agree before a read can be `Current`. If metadata writing fails after projection mutation, the public result is `Unavailable` with `metadata_write_failed`. During active rebuild, catch-up, or mixed-generation summary/detail reads, the result is non-current.

## Read Boundary

`ConversationProjectionReadService` checks the local tenant access boundary before reading projection state. Denied, cross-tenant, malformed, and missing projection reads return hidden `Forbidden` semantics without touching or disclosing projection state. Projection store failures return `Unavailable` without a partial detail projection. Summary and detail freshness metadata must come from the same cursor/generation before the detail can be returned.

This local evidence feeds forward into Story 1.8 retrieve/list behavior, Story 1.11 replay and schema-version proofs, Story 3.x operator trust surfaces, Story 4.2 client behavior, and Story 6.2 projection-lag observability.
