# ADR 0003: Projection Read-Store Population Proof

- Status: Accepted
- Date: 2026-07-15
- Decision owners: Conversations Architect / Developer
- Approved by: Administrator
- Approval reference: `sprint-change-proposal-2026-07-15-projection-read-store-population.md`
- Related readiness gate: Stories 6.2 and 6.6; FR-5, FR-6, FR33-FR37; NFR22, NFR38, and NFR62; Epic 5 retrospective action A3

## Context

Conversations has the two halves of a persisted query projection, but they are not joined
on a production path:

- `ConversationProjectionHandler` implements the legacy synchronous
  `IDomainProjectionHandler`. It materializes one opaque full-replay response for the
  gateway projection actor and explicitly does not populate the separate query-side
  read store.
- `ConversationProjectionReadModelWriter` persists the per-conversation summary/detail
  model and per-tenant index through `IReadModelStore` and `ReadModelWritePolicy`, but it
  is invoked directly only by tests and is not called by a production projection route.
- Host-composition tests prove that the handler, store, and writer resolve. They do not
  prove that EventStore delivery causes either read-store key to be populated.

Story 5.3 therefore recorded projection read-store population as unproven. The signed
July 14 v1 release-owner decision accepted that residual risk only for its bound v1
attestation scope and explicitly stated that population remained deferred and was not
represented as proven. That signed evidence remains immutable.

The current architecture and PRD require ordinary queries to read persisted projections,
require replay-equivalent rebuilds, and make unwaived projection determinism gaps release
blocking. Epic 6 is already correcting the production host boundary and issuing v2
evidence, so it is the contained point at which to close the gap.

## Decision

Production projection read-store population must be proven. The existing v1 deferral is
not carried forward as authority for Epic 6 readiness or the v2 attestation.

Conversations shall expose a canonical, named, scoped `IAsyncDomainProjectionHandler`
route for its persisted query projection. The route shall:

1. consume the tenant, aggregate identity, and ordered event slice supplied by the
   platform projection dispatcher;
2. reuse the conversation-owned materializer rather than duplicate projection rules;
3. persist the materialized `ConversationProjectedReadModels` and the tenant summary
   index through `ConversationProjectionReadModelWriter`, `ReadModelWritePolicy`, and the
   configured `IReadModelStore`;
4. accept the platform's stable dispatch identity and return a durable completed outcome
   only after both store writes have completed;
5. map cancellation, optimistic-concurrency exhaustion, partial-write uncertainty, and
   storage failures to the platform's bounded retryable, indeterminate, or failed outcome
   taxonomy without reporting false completion; and
6. remain idempotent under duplicate delivery, retry, and full replay. A retry after a
   per-conversation write but before a tenant-index write must converge without duplicate
   index entries or a divergent read model.

The legacy `IDomainProjectionHandler` may remain for the version-1 opaque full-replay
protocol, but neither its returned `ProjectionResponse` nor gateway-actor persistence is
proof of query read-store population.

Queries continue to read only the persisted Conversations read store. Query-time replay,
on-read materialization, or silent backfill is prohibited because it would hide lag,
split write ownership, and weaken freshness semantics.

The historical v1 risk acceptance remains valid only for the immutable artifact and
scope to which it was signed. It does not close this ADR, Story 6.2, Story 6.6, or current
readiness. A future deferral would require a new, separately approved named waiver with
an explicit excluded release scope, owner, expiry, compensating controls, and evidence
that the affected query behavior is not claimed as production-ready; it cannot inherit
the v1 decision implicitly.

## Consequences

The production platform boundary becomes the single cause of persisted projection
updates, while Conversations continues to own materialization, tenant-scoped keys,
freshness, and read semantics. EventStore remains the authoritative history and the read
store remains disposable and rebuildable.

Story 6.2 gains focused implementation and integration-proof work. Story 6.6 consumes
that proof instead of discovering the gap during final attestation. No PRD, UX, public
contract, or completed Epic 1-5 history changes.

Both the per-conversation record and tenant index are currently separate writes. The
handler must therefore treat an uncertain second write as non-completion and rely on the
writer's idempotent replace/merge behavior for convergence on retry. This is an explicit
operational trade-off, not an atomicity claim.

Until the proof passes, architecture status remains `READY FOR CORRECTIVE IMPLEMENTATION
ONLY`; Stories 6.2 and 6.6 cannot be completed and a readiness rerun cannot return
`READY` on the strength of the v1 deferral.

## Alternatives Considered

- Continue the July 14 deferral into Epic 6. Rejected because the v1 decision expressly
  does not prove population, while persisted query projections and rebuild equivalence
  remain active requirements and an unwaived release gate.
- Treat direct writer tests and DI resolution as proof. Rejected because they do not show
  that production EventStore delivery invokes the writer or leaves the configured state
  store in the required end state.
- Populate projections during queries. Rejected because it makes reads mutate state,
  masks freshness and availability failures, and creates a second orchestration owner.
- Make the legacy synchronous handler block on the asynchronous writer. Rejected because
  sync-over-async is unsafe and the platform already provides the canonical asynchronous
  named-projection seam.
- Store only the legacy opaque gateway projection. Rejected because Conversations list
  and detail queries use their separate persisted read-store contract, including a
  tenant index and explicit freshness states.

## Verification

Story 6.2 shall produce `projection-read-store-population-proof-v2` as versioned JSON and
Markdown evidence and focused automated integration tests. Proof is sufficient only when
all of the following are demonstrated through production composition:

1. an accepted conversation event append or an authorized full replay reaches the
   EventStore named-projection coordinator, the domain-service dispatcher, and the
   Conversations named asynchronous handler without a direct test call to the writer;
2. the configured integration state-store adapter contains the exact tenant-scoped
   `projection:conversations:{tenant}:{conversation}` record and
   `projection:conversations-index:{tenant}` entry after dispatch;
3. the production query handler reads those persisted values and reports the expected
   freshness and last-applied event position;
4. duplicate dispatch, retry after an injected partial failure, and replay converge to
   byte-equivalent logical state with one index entry per conversation;
5. a second tenant cannot read, alter, or infer the first tenant's projection records;
6. storage failure cannot produce a completed dispatch outcome or a falsely current read;
7. deleting derived projection state and replaying authoritative EventStore history
   rebuilds an equivalent per-conversation record and tenant index; and
8. the test asserts state-store end state, not only HTTP status, handler output, mock call
   count, service resolution, or the legacy gateway projection response.

Story 6.6 shall validate the evidence hashes and rerun the relevant conformance and
rebuild gates before producing the superseding v2 attestation.
