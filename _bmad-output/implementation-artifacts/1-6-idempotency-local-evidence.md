# Story 1.6 Local Idempotency Evidence

Date: 2026-05-19

## Scope Covered

- Command scope and canonical fingerprint coverage includes create conversation, append message, add participant, attach file reference, update metadata, close conversation, and archive conversation.
- Duplicate equivalent command coverage includes completed success replay, pending in-flight uncertainty, expired record uncertainty, poisoned record uncertainty, version-incompatible record uncertainty, terminal close replay, and terminal archive replay.
- Conflict coverage proves same scoped idempotency key plus different command meaning returns `idempotency_conflict` without invoking mutation.
- Tenant coverage proves add-participant tenant denial occurs before idempotency lookup, state load, participant validation, aggregate dispatch, outcome replay, or conflict disclosure.
- Projection coverage proves duplicate and reordered create, participant, message, file, metadata, close, and archive deliveries remain deterministic through event-id deduplication and set/update-by-stable-ID operations.

## Storage/Fake Used

- Local proof uses `InMemoryConversationIdempotencyStore` with atomic lock-based reserve/complete behavior and bounded record snapshots.
- EventStore command status is represented through `EventStoreCommandStatusIdempotencyBridge`, which treats EventStore status as an internal signal. Pending statuses return retryable uncertainty, and terminal statuses require Conversations/EventStore replay before a logical outcome can be exposed.

## Privacy and Non-Disclosure Evidence

- Stored idempotency records contain scoped key metadata, SHA-256 fingerprints, lifecycle state, timestamps, record version, and minimal logical outcome metadata only.
- Tests assert record debug output does not expose safe labels used as payload content, provider session/response references, EventStore vocabulary, stream vocabulary, or raw payload vocabulary.
- Public contract additions are limited to `idempotency_outcome_unknown`; EventStore status, stream, envelope, sequence, expected revision, and status storage details remain outside public Contracts.

## Deferred Release-Gate Gaps

- Story 5.6 still owns signed release-manifest aggregation and long-lived conformance evidence.
- Production durable idempotency persistence and retention cleanup automation remain outside this story unless a later ADR/story pulls them in.
- Full command-handler integration for append-message, attach-reference, update-metadata, close, and archive awaits the stories that introduce those behavior handlers. Story 1.6 covers their contract fingerprint, replay semantics, and terminal replay proof without pretending those handlers already exist.
