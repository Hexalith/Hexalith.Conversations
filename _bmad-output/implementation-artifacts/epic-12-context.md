# Epic 12 Context: Universal Performance Restoration

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators correctness-preserving projection performance under the universal SM-C2 rule: each of the four frozen hot paths must independently show usable, comparable evidence that candidate P95 is at most 1.05 times baseline P95. Current evidence fails (HP-APPEND +20%, HP-LIST +330%, HP-OPEN +1761%; HP-CREATE -50%), which blocks release attestation. The list/open cost comes from mandatory fail-closed cross-key validation (detail, tenant-index and dispatch-ledger reads, plus per-row reads during page verification). Only provably unnecessary fan-out may be removed, never the checks themselves.

## Stories

- Story 12.1: Approve derived-key ownership, lifecycle, and rollback
- Story 12.2: Freeze the benchmark method and signal-quality algorithm
- Story 12.3: Implement correctness-preserving list/open optimization and migration behavior
- Story 12.4: Produce candidate-bound evidence and enforce universal SM-C2

## Requirements & Constraints

- Inventory `sm-c2-hot-path-inventory-v1` is fixed in the order `HP-APPEND`, `HP-CREATE`, `HP-LIST`, `HP-OPEN`. No row can be selected, dropped or reordered after measurement. Every row uses exactly `postP95 <= 1.05 * baselineP95`. Cost ceilings, disclosure, correctness rationale, owner acceptance, recorded-only status and unusable signal never substitute for it. Changing the target needs a separate PRD-level decision.
- Frozen envelope members: workload/data shape, concurrency, environment/runtime, warm/cold classification (cold never substitutes for warm). Versioned members (tool/version, repetition policy, raw-sample volume/processing) change only through a named, recorded envelope revision that re-measures baseline and candidate as a pair from the preserved fixture. Anything not listed as versioned is frozen.
- A row has usable signal when its paired P95 confidence half-width is at most 5% of baseline P95. The half-width is computed over the retained raw samples by one method that is declared before observation and is identical for both runs; if no method is declared, it is the percentile bootstrap. P95 is nearest-rank. Adverse samples are never discarded after observation. HP-CREATE and HP-APPEND previously showed within-run dispersion about two orders of magnitude wider than the threshold, so getting usable signal for them is part of the work.
- The benchmark measures the in-process warm path only. It proves closure-local regression and is not evidence for the separate invariant against synchronous cross-service calls or unbounded history loads on hot paths. Preserved absolute latency targets, such as the 500 ms open P95, are not active gates unless a release decision activates them.
- SM-C2 baseline v1 (JSON, Markdown and xUnit result) and Story 6.2 evidence are immutable. The baseline's Markdown and JSON disagree on the fixture SHA-256 (`4838a5a1…` against `1a43bacc…`). This open provenance blocker must be surfaced. Do not repair it or resolve it by preferring one value.
- Optimization preserves the public query contract: filters, stable ordering, cursor continuation, redaction, freshness vocabulary, typed errors, forbidden/nonexistent indistinguishability and response shapes. UX stays preserved and not activated, so there is no UI scope.
- Unit, integration and real Dapr state-store correctness lanes must run, execute real tests and pass before any performance verdict. They cover:
  - invalid derived state: missing, duplicate, stale, advanced, malformed, misfiled, pending and inconsistent;
  - cross-tenant collisions, retries and idempotency, duplicate or out-of-order delivery, and equal-position conflicts;
  - partial writes, latency, an unavailable store, poison records and concurrency;
  - interrupted rebuild, deletion and deterministic replay.
- Evidence binds the candidate, predecessors, root/EventStore gitlinks, benchmark binary and source, raw samples, environment and digests. JSON is authoritative; Markdown is its deterministic rendering. Missing, stale, skipped or zero-test results and any drift block completion. Approvals name the owner and bind the exact approved digest.

## Technical Decisions

- EventStore is the only durable domain write authority. Derived keys are replaceable projection state, and reads never materialize, backfill or repair them. Rebuild and rollback re-derive from the event stream: redeploy the prior binaries, then run a full replay. They never reverse-write derived state or repeat external side effects.
- Accepted ADR 0005 is the lifecycle authority. Its grammar `conversations-derived-keys-v2` is v1 plus four resolution families (metadata, selection, proof and fence) plus a configurable store name. Migration from v1 is identity, because no key bytes change.
  - All seven families are owned by Conversations.
  - Only the canonical named projection route (including reconcile and the rebuild plan) and the create-only tenant-created index initializer may write them.
  - Only the dispatch ledger expires, after the redelivery window, and the proof outlives it.
  - Deletion happens only through an operator rebuild. There is no backfill.
- Story 12.3 may optimize only inside that lifecycle. Any of these needs a new decision and a versioned grammar successor: a new family, a key-byte change, an extra writer, a read-side write, a new expiry or a reordered write. Rows marked "proposed" are commitments, not shipped behavior; implementing one must cite the ADR and add current evidence. Any edit to ADR 0005 voids its owner approval.
- A projection completes only when both the conversation record and the tenant-index writes land. Partial-write uncertainty stays incomplete until an idempotent retry converges. Dispatch-ledger keys have no tenant segment, so the tenant fail-closed check applies on every ledger access. Tenant authorization fails closed before every read, write and rebuild.
- `ProjectionTrustState` (`Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted`) is consumed, never redeclared. The temporal anchor is the `ProjectionFreshnessV1` composite of schema version, cursor and last-applied event position. HP-OPEN comparability also anchors on it. Timestamps are display and lag metadata only.
- The local AppHost stays a non-packable, non-publishable test fixture unless a separately approved amendment follows a contrary baseline-owner decision. Decision, method, production migration and final evidence (12.1-12.4) are each separately reversible.

## Cross-Story Dependencies

- Order: 6.2, 16.3 and IR-0 come before 12.1, then 12.2. Story 12.3 needs both 12.1 and 12.2. Story 12.4 follows 12.3 and feeds release revalidation (15.1). The epic exits only when all four are done.
- Story 12.1 is done (2026-11-16). ADR 0005 is accepted, and the named owner approval is bound to the decision-payload digest. The owner waived AC-12.1-01 (the candidate verifier) and AC-12.1-06 (the v2 final record). Both are recorded as waived, never PASS, so no 12.1 final record exists. Story 12.2 must instead bind 12.1 through:
  - its done spec;
  - its sprint row, which must itself read `done`;
  - its commit;
  - the approved decision JSON/Markdown pair.
- The contracted `bin/Release/net10.0` command paths can no longer be produced: the target framework is now net11.0, and source mode builds only Debug. Story 12.1 ran the same tests from Debug/net11.0 and disclosed the change. Baseline v1 was measured on .NET 10 Release, and environment/runtime is a frozen envelope member. Stories 12.2 and 12.4 therefore cannot assume runtime comparability between baseline and candidate.
- Planning artifacts still record the global hold as ACTIVE. In practice, stories have started on explicit owner decisions recorded in each spec, as with 12.1 and Epic 16. Reconciling that planning text is an open release-owner retro action, not Epic 12 work.
