# Epic 12 Context: Universal Performance Restoration

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Restore correctness-preserving projection performance across all four frozen command and read hot paths. Completion requires comparable, usable evidence that every path independently meets the universal SM-C2 threshold while preserving tenant isolation, EventStore authority, public contracts, and deterministic replay. Historical performance failures and accepted Story 6.2 evidence remain immutable; new proof is additive and candidate-bound.

## Stories

- Story 12.1: Approve derived-key ownership, lifecycle, and rollback
- Story 12.2: Freeze the benchmark method and signal-quality algorithm
- Story 12.3: Implement correctness-preserving list/open optimization and migration behavior
- Story 12.4: Produce candidate-bound evidence and enforce universal SM-C2

## Requirements & Constraints

- The frozen inventory is `sm-c2-hot-path-inventory-v1`: `HP-APPEND`, `HP-CREATE`, `HP-LIST`, and `HP-OPEN`. Every row must satisfy `postP95 <= 1.05 * baselineP95`. Cost ceilings, disclosure, owner acceptance, correctness rationale, and recorded-only dispositions cannot replace that gate.
- Preserve all approved conformance obligations and public contract shapes. Optimization must preserve filtering, stable ordering, cursor continuation, redaction, freshness, error semantics, and forbidden/nonexistent equivalence. Shared capabilities must not add synchronous cross-service calls or unbounded history loads on hot paths.
- Baseline and candidate measurements must share the frozen workload/data shape, concurrency, environment/runtime, and warm/cold classification. Tool versions, repetitions, and raw-sample processing may change only through a recorded envelope revision that remeasures baseline and candidate as a pair. Cold measurements cannot substitute for warm rows.
- Declare warmup, repetitions, raw retention, nearest-rank P95, environment control, and signal rules before observing results. Retain adverse samples. Usable signal requires paired P95 confidence half-width no greater than 5% of baseline P95, using the same declared algorithm for both runs; absent a declaration, use percentile bootstrap.
- Unit, integration, and real Dapr correctness lanes must execute nonvacuously and pass before final performance acceptance. Cover invalid derived state, cross-tenant collisions, duplicate/out-of-order delivery, equal-position conflicts, partial writes, concurrency, interrupted rebuild, deletion/expiry, and deterministic replay.
- Decision and measurement approvals must identify the authorized owner and bind the exact approved digest. A role label or unrelated authority grant does not establish approval. Missing lifecycle approval blocks acceptance.
- Evidence binds the actual source candidate, predecessor records, applicable root/submodule identities, executable inputs, raw results, and digests. JSON is authoritative; generated Markdown must agree. Drift, missing results, skipped execution, or unusable measurements block completion.

## Technical Decisions

- EventStore remains the only durable domain write authority. Derived keys are replaceable projection state; readers never materialize, backfill, or repair durable state. Rebuild and rollback derive from the event stream and must not reverse-write derived data or repeat external side effects.
- EventStore owns ordered delivery, stable dispatch identity, and canonical named-projection routing. Conversations owns domain materialization, tenant-scoped keys, freshness, and query semantics through the shared read-model store and write policy. Projection completion requires both conversation record and tenant-index writes; partial-write uncertainty remains incomplete until idempotent retry converges.
- Freeze key ownership, write ordering, compatibility, rebuild, backfill, deletion, expiry, and rollback in approved ADR 0005 before production optimization. Changes to the accepted `conversations-derived-keys-v1` grammar require a versioned successor with migration or rebuild treatment. Dispatch-ledger keys are the declared tenant-segment exception; every ledger access still enforces tenant isolation.
- Tenant authorization fails closed before reads, writes, and rebuilds. Missing, duplicate, stale, advanced, malformed, misfiled, pending, or inconsistent derived state cannot yield a trusted response or trigger read-side repair.
- Preserve the shared `ProjectionTrustState` vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`. The temporal evidence anchor is the `ProjectionFreshnessV1` composite of projection schema version, cursor, and last-applied event position; timestamps are display/lag metadata.
- Keep decision, measurement, production migration, and final evidence work separately reversible. Preserve immutable Story 6.2, baseline v1, and historical proof. The local AppHost remains a non-packable, non-publishable test fixture under the recorded interpretation; a contrary baseline-owner decision requires a separately approved amendment.

## UX & Interaction Patterns

Preserve source-owned trust, freshness, redaction, temporal evidence, and safe typed failures across existing read surfaces. Clients must not infer trust or permissions from optimized responses. This epic does not activate preserved product UI or introduce acceptance/waiver mutation flows.

## Cross-Story Dependencies

- Effective entry to Story 12.1 requires both immutable completed Story 6.2 and completed Story 16.3; the later dependency amendment supplements the older epic body. The current sprint lists Story 16.3 as backlog.
- Story 12.2 follows 12.1. Production optimization in 12.3 requires both 12.1 and 12.2. Final universal evidence in 12.4 follows the completed production correctness proof in 12.3. All four stories must be done for the epic to exit; Story 12.4 also feeds release attestation.
- The global implementation hold is not lifted by this context, planning publication, historical completion, or a narrow checkpoint grant. The recorded September hold decision unlocks only `7.1-SCHEMAS`; effective entry authority and predecessor proof must be verified before execution.
