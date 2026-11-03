# Epic 16 Context: Operational Projection Correctness And Recovery

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators and authorized callers durable tenant authorization, projection truth derived deterministically from event history, and live reconciliation they can diagnose, across restarts and multiple replicas. Recovery must restore query-visible state without weakening authorization, without showing missing state as current-empty, and without creating a second source of Conversation truth. Story 16.1 has delivered durable tenant-access state. Stories 16.2 and 16.3 remain.

## Stories

- Story 16.1: Persist tenant-access projection state and prove convergence
- Story 16.2: Make replay time deterministic and missing-index state truthful
- Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live

## Requirements & Constraints

- **Event-derived time.** A contract event takes its identity and time from metadata `EventId`/`OccurredAt`. Its envelope time is diagnostic only. A position-only event uses its persisted `MessageId`/`Timestamp`. Validate identity, exact payload bytes, and selected time before duplicate suppression. Missing or invalid identity or time fails closed. Replay never falls back to a clock (`UtcNow`, `Now`, `TimeProvider`).
- **Duplicates.** The binding is tenant, conversation, event type, sequence, serialization format, payload SHA-256, and selected UTC ticks. Exact reuse is a no-op. Changed reuse maps to `Unavailable`/`metadata_contradictory`.
- **Deterministic rebuilds (effective AC-16.2-02).** Two clean rebuilds under different clocks must produce byte-identical canonical v2 replay-state JSON, `ReplayAnchorAt`, and `ReplayStateHash`. Public freshness is generated anew, conforms to `ProjectionFreshnessV1`, stays out of replay identity, and is tested by semantic invariants, not byte equality.
- **Freshness meaning.** `ReplayAnchorAt` is the maximum validated contributing event or lifecycle time, and `LastAppliedEventTimestamp` equals it. `ProjectionGeneratedAt` is the actual completion instant, and `LagDuration` spans the two. A generation time earlier than the anchor fails closed and is never clamped.
- **Truthful empty.** Only a proven v2 `Ready` index with zero summaries and no pending dispatch is current-empty. A surviving detail record never proves empty.
- **Pure reads.** Queries make zero repair writes and never infer lifecycle. A state-store write ledger must prove this.
- **Preservation.** Keep the public query/freshness contract shape, safe vocabulary, redaction, tenant isolation, 16.1 state, and event history unchanged. Required results must be nonempty with zero failed, skipped, or not-run. Diagnostics carry no tenant or personal data.
- **Authority.** Planning keeps the hold `ACTIVE`. The successor scenario verifiers do not exist yet. Selected runtime commands return `BLOCKED`/`ENTRY_QUALIFICATION_PENDING`, and the record generator cannot consume the successor contracts. Story 16.1 ran only after an explicit owner waiver with native xUnit proof. Story 16.2 needs its own entry decision.

## Technical Decisions

- **Ownership.** EventStore history is the only Conversation write authority, and projections are derived and rebuildable. Platform owns hosting and providers. Shared capabilities go into EventStore or Tenants as additive seams, never duplicated in Conversations.
- **Sole lifecycle authority.** The v2 `ConversationProjectionIndexReadModel` at `projection:conversations-index:<tenant-segment>` holds lifecycle, the Tenants watermark, per-aggregate watermarks, dispatch references, and the rebuild barrier/manifest. There is no separate lifecycle key, and lifecycle and entries share one CAS boundary. Only `ConversationProjectionReadModelWriter` authors transition plans. EventStore executes them.
- **Transitions.**
  - `TenantCreated` with `Absent` goes to `Ready(empty)`.
  - `Absent` or v1 goes to `Rebuilding`.
  - `Ready` goes to `Erasing`, then `Erased`.
  - `Ready` or `Erased` goes to `Rebuilding`, then `Ready`; leaving `Erased` needs an owner-authorized rebuild.
- **Generations.** Each erase or rebuild bumps the generation on detail, dispatch, manifest, watermarks, and queued references. Stale-generation writes are rejected. A query with mismatched generations returns `Rebuilding`.
- **Erase.** Delete only retiring-generation targets, and keep the tombstone plus non-payload quarantine references. A delivery during `Erasing` or `Erased` is acknowledged only after it is durably quarantined, and it can never recreate content.
- **Rebuild.**
  1. Discover authoritative tenant streams at barrier B, never from the old index.
  2. Durably queue later deliveries.
  3. Prove exact-next sequence per aggregate through B.
  4. Drain through cutover barrier C.
  5. CAS to `Ready` with complete watermarks.

  Watermarks never regress. A gap in global position is not a tenant-stream gap.
- **Canonical bytes.** The EventStore batch canonicalizer is the serialization authority (web defaults, compact UTF-8, ordinal-sorted properties). Conversations supplies ordinal-ordered collections.
  - An unordered collection without a stable key fails before persistence.
  - The hash excludes freshness, wrappers, ETags, batch IDs, and TTL.
  - New persistence metadata needs a versioned EventStore API and golden vectors first.
- **Tenant access (16.1).** Only `Healthy` state authorizes. Unsafe states give the same denial as an unknown tenant, and reads never trigger recovery.
- **Story 16.3.** Use the stable codes `APPHOST_ENDPOINT_NOT_READY` and `DAPR_CONTROL_PLANE_PORT_COLLISION`. The live proof must use production `POST /project/v2/reconcile` plus durable end-state and query observations. Direct handler calls do not count.

## UX & Interaction Patterns

Render only the closed public state/reason pairs:

| Condition | Public state / reason | Empty page allowed? |
| --- | --- | --- |
| Proven v2 `Ready`, zero summaries, no pending dispatch | `Current` / `current` | Yes, only here |
| v1, `Erasing`, `Erased`, or `Rebuilding` | `Rebuilding` / `rebuilding` | No |
| Lifecycle or source-position gap | `Rebuilding` / `gap_detected` | No |
| Missing record for an authorized tenant, or provider failure | `Unavailable` / `unavailable` | No |
| Corrupt, mixed-generation, or contradictory state | `Unavailable` / `metadata_contradictory` | No |
| Unauthorized or nonexistent tenant | `Forbidden` / `forbidden`, with no index read | No |

The UI labels the replay timestamp, the generation timestamp, and lag separately. It never confirms that protected data exists.

## Cross-Story Dependencies

- The chain is `7.4, IR-0 -> 16.1 -> 16.2 -> 16.3`. Story 16.3 also gates 12.1, 13.1, 14.1, and 15.1.
- Story 16.2 builds on 16.1's state and must not change it.
- Story 16.3 relies on 16.2's truthful query states.
- Before 16.3 starts, the baseline owner and Architect/Runtime must choose a module-AppHost exemption or a relocation.
- Each story's final record is a pair at `docs/release-evidence/story-16.N-final-record-v2.*` with summary `6/6/0/0/0/0`. Story 16.1 skipped it under its waiver.
