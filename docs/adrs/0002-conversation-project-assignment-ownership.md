# ADR 0002: Conversation Project Assignment Ownership

- Status: Accepted
- Date: 2026-05-26
- Decision owners: Jerome / BMad Story 2.2 implementation
- Related readiness gate: Epic 2 AR-G1 / PR-1 upstream capability

## Context

Projects needs to link, move, and unlink conversations. Conversations already owns the
durable conversation aggregate and exposes `ProjectId` on creation and read models, but
there is no post-creation command or event for changing that stable project reference.

The architecture uses Pattern A: Projects reads conversations by a Conversations-owned
back-reference instead of maintaining an unbounded mutable membership list on the
Projects aggregate. If Projects also stored a separate mutable conversation membership
list, the two modules could disagree after retries, partial failures, delayed projection
catch-up, or explicit unlink operations.

## Decision

Conversations owns the conversation-to-project assignment. The upstream capability is a
Conversations command and past-tense Conversations event that can assign, reassign, and
explicitly clear the current `ProjectId` after conversation creation.

Projects must not maintain a separate mutable conversation membership list for this
relationship. Projects may request a link, move, or unlink through the Conversations
command boundary and then rely on the Conversations projection and list filter once the
read model catches up.

Explicit clearing is included in this story. A missing target project field is not an
unlink request; callers must use an explicit operation shape so null and omission cannot
accidentally remove an assignment.

## Consequences

The relationship has one write authority and one event history. Assignment movement is
auditable through Conversations metadata and remains tenant-scoped, idempotent, and
content-safe.

Projects-side Story 2.3 can compose user-facing link, move, and unlink behavior without
copying membership state. It must handle read-model lag by using existing projection
freshness and trust-state contracts rather than patching stale membership locally.

## Alternatives Considered

- Store `ConversationId[]` or a mutable membership projection inside Projects. Rejected
  because it creates dual write ownership and stale/divergent membership risk.
- Stretch `ConversationMetadataUpdated` to mutate `ProjectId`. Rejected because project
  assignment is a relationship-changing fact, not safe metadata decoration.
- Treat missing target project as clear. Rejected because serializers and partial payloads
  could turn accidental omission into unlink behavior.
- Defer explicit clear to Projects. Rejected because unlink still changes the
  Conversations-owned relationship and must be represented upstream.

## Verification

Story 2.2 verifies this decision with Conversations-local tests for:

- additive command and event contracts with explicit clear semantics;
- aggregate assignment, reassignment, clearing, replay, and no-op duplicate behavior;
- tenant-first command handling and idempotency conflict behavior;
- projection materialization and list filtering by current `ProjectId`; and
- payload/privacy scans proving only stable identifiers and metadata are emitted.
