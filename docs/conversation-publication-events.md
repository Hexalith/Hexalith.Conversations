# Conversation Publication Events

Conversation publication events are hints that a persisted conversation fact exists. EventStore history remains authoritative for rebuilds and trust-bearing state reconstruction.

Consumers must deduplicate by the default identity tuple:

```text
(tenantId, conversationId, eventId, schemaVersion)
```

The public metadata also exposes this tuple as `deduplicationKey`. Retries and transport replays preserve the same `eventId`, `deduplicationKey`, `correlationId`, `causationId`, tenant scope, and conversation identity. A retry is not a new domain change.

Delivery is at least once and may be replayed or reordered. Consumers may use a per-conversation revision only when a future contract explicitly provides one; v1 publication does not promise strict ordering from transport metadata alone.

Unsupported major schema versions fail closed at the publication or consumption boundary. Additive v1 fields may be ignored only when all required v1 metadata is present and the active contract type permits that behavior.

Tenant-mismatched or unsupported-version messages must be rejected or quarantined before projection mutation or downstream side effects. Diagnostics are bounded to event type, schema version, tenant scope, conversation identity, event identity, correlation ID, and causation ID; they must not include command bodies, event payload fragments, Party personal data, provider payloads, transport internals, or raw exception text.

Transport publication failure does not roll back durable persistence. The publication path retries or surfaces a bounded publication diagnostic without re-emitting a new successful domain change.
