# ADR 0001: Command Idempotency Contract

- Status: Accepted
- Date: 2026-05-19
- Decision owners: Jerome / BMad Story 1.6 implementation
- Related readiness gate: Story 1.6, Story 5.6 release-gate evidence follow-up

## Context

Conversation commands are delivered across retry-prone boundaries. Callers can time out,
gateways can retry, and Dapr pub/sub can deliver at least once. Duplicate create,
append-message, add-participant, attach-reference, update-metadata, close, or archive
commands must not create duplicate business effects, and conflict checks must not reveal
hidden tenant or conversation state.

EventStore already provides command identity and advisory command status through
`CommandEnvelope.MessageId`, `CommandStatusRecord`, and tenant-scoped status lookup.
Those records are correlation/status oriented and do not, by themselves, define the
Conversations command equivalence tuple, public conflict behavior, or non-disclosing
retry semantics required by this module.

## Decision

Conversations owns the command idempotency contract at its server write boundary.
Tenant access is always evaluated first. No idempotency lookup, command-status lookup,
stored outcome replay, conflict response, aggregate load, projection read, or publication
decision may occur until trusted tenant access has succeeded.

The caller-supplied idempotency key comes from `ConversationCommandMetadata.IdempotencyKey`.
It is scoped by all of the following values:

- trusted tenant ID;
- public command type;
- conversation scope, or create-conversation allocation scope chosen by the server;
- idempotency key;
- command schema version.

The normalized fingerprint is produced from stable Conversations command contracts and
explicit command context only. Canonicalization includes stable tenant, command type,
scope, schema version, durable IDs, safe command fields, and ordered safe metadata.
Canonicalization excludes provider-owned session or response IDs as authority,
server-generated timestamps, transport headers, raw JSON byte order, EventStore envelopes,
stream names, expected revisions, sequence numbers, mutable Party display data, raw
provider payloads, exception text, and raw command bodies.

Equivalent duplicate terminal outcomes are replayed as stable logical outcomes. The
stable outcome may include result category, conversation/message/participant/reference
identity, typed rejection code, retryability, and safe correlation or audit handles. It
does not expose EventStore command status internals and does not require byte-for-byte
transport equality.

Reusing the same scoped key with a different fingerprint is an idempotency conflict. The
public response uses the Conversations `idempotency_conflict` code, mutates no
conversation state, publishes no successful domain event, and does not disclose which
field differed.

The reservation lifecycle is atomic:

1. Reserve: create a pending record only if no non-expired record exists for the scoped
   key.
2. Duplicate: if the fingerprint matches a completed record, return the stored logical
   outcome without invoking aggregate mutation.
3. Pending or unknown: if the fingerprint matches a pending, poisoned, stale, expired,
   version-incompatible, or infrastructure-uncertain record, resolve a terminal outcome
   from authoritative EventStore history when that can be proven; otherwise return a
   typed retryable uncertainty outcome without appending a second success event.
4. Conflict: if the fingerprint differs, return the typed conflict outcome without
   aggregate load or mutation.
5. Complete: after the aggregate and publication boundary produce a terminal logical
   outcome, finalize the reserved record with minimal versioned metadata.

Idempotency records store only the scoped key, fingerprint, lifecycle state, retention
timestamps, schema/contract version, and bounded logical outcome metadata. They do not
store raw command payloads, provider payloads, Party personal data, EventStore envelopes,
stream identifiers, expected revisions, sequence numbers, transport headers, exception
details, mutable display values, or file binaries.

The default retention expectation is twenty-four hours, matching the current EventStore
command-status TTL. Longer release-gate retention or signed evidence belongs to Story
5.6. Expired records are not silently overwritten when doing so could duplicate a
business effect; the server must resolve from EventStore history or return retryable
uncertainty.

## Consequences

This decision keeps idempotency in Conversations vocabulary while allowing a server
adapter to reuse EventStore command-status signals where they help. It adds an explicit
atomic reserve/complete protocol before full production persistence is introduced. The
protocol is intentionally stricter than correlation-id status lookup because it includes
payload equivalence and tenant-first disclosure rules.

Public contracts remain free of EventStore internals. Diagnostics can distinguish
duplicate, conflict, unsupported version, pending/unknown, tenant mismatch, and
infrastructure uncertainty internally, but public errors stay content-safe.

## Alternatives Considered

- Use EventStore `CommandEnvelope.MessageId` alone. Rejected because it does not capture
  Conversations payload equivalence, scoped conflict comparison, or tenant-disclosure
  behavior.
- Cache full public responses. Rejected because byte-for-byte response equality is not
  required and response caching risks storing raw payload or transport details.
- Use provider session or response IDs. Rejected because providers are not durable
  identity authorities for Conversations.
- Global cross-service deduplication. Deferred; Story 1.6 only needs module-local command
  idempotency proof.
- Allow expired key reuse by default. Rejected because it can duplicate business effects
  when the caller retries after a timeout and the original mutation succeeded.

## Verification

Story 1.6 verifies this decision with local tests for:

- scoped key construction across create, append-message, add-participant, attach-reference,
  update-metadata, close, and archive commands;
- canonical semantic equivalence and negative cases for lossy normalization;
- atomic concurrent reservation so only one mutation delegate can win;
- duplicate equivalent success, no-op, and rejection replay;
- conflict rejection with no aggregate mutation or publication;
- retryable uncertainty for pending, stale, poisoned, expired, and unsupported-version
  records when no terminal EventStore outcome can be proven;
- tenant-scoped non-disclosure before idempotency lookup; and
- projection duplicate/reorder determinism through set/update-by-id behavior.
