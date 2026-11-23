# Epic 16 Context: Operational Projection Correctness And Recovery

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators and authorized callers durable tenant authorization, projection truth derived deterministically from event history, and live reconciliation they can diagnose, across restarts and multiple replicas. Recovery must restore query-visible state without weakening authorization, without showing missing state as current-empty, and without creating a second source of Conversation truth. The three story specs record their completed scope and the entry waivers used for their native xUnit verification.

## Stories

- Story 16.1: Persist tenant-access projection state and prove convergence
- Story 16.2: Make replay time deterministic and missing-index state truthful
- Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live

## Requirements & Constraints

- **Event-derived time.** A contract event takes its identity and time from metadata `EventId`/`OccurredAt`. Its envelope time is diagnostic only. A position-only event uses its persisted `MessageId`/`Timestamp`. Validate identity, exact payload bytes, and selected time before duplicate suppression. Missing or invalid identity or time fails closed. Replay never falls back to a clock (`UtcNow`, `Now`, `TimeProvider`).
- **Duplicates.** The binding is tenant, conversation, event type, sequence, serialization format, payload SHA-256, and selected UTC ticks. Exact reuse is a no-op. Changed reuse maps to `Unavailable`/`metadata_contradictory`.
- **Deterministic rebuilds (effective AC-16.2-02).** Two clean rebuilds under different clocks must produce byte-identical canonical v2 replay-state JSON, `ReplayAnchorAt`, and `ReplayStateHash`. Public freshness is generated anew, conforms to `ProjectionFreshnessV1`, stays out of replay identity, and is tested by semantic invariants, not byte equality.
- **Freshness meaning.** `ReplayAnchorAt` is the maximum validated contributing event or lifecycle time, and `LastAppliedEventTimestamp` equals it. `ProjectionGeneratedAt` is the actual completion instant, and `LagDuration` spans the two. When observation precedes the anchor, public freshness is `Unavailable` / `metadata_contradictory` with null lag. The response keeps the actual observation time and uses the Unix epoch as its last-applied marker. For an observation before the epoch, the marker is the earliest timestamp v1 accepts (one UTC tick after `DateTimeOffset.MinValue`). The true anchor remains in replay state.
- **Truthful empty.** Only a proven v2 `Ready` index with zero summaries and no pending dispatch is current-empty. A surviving detail record never proves empty.
- **Pure reads.** Queries make zero repair writes and never infer lifecycle. A state-store write ledger must prove this.
- **Preservation.** Keep the public query/freshness contract shape, safe vocabulary, redaction, tenant isolation, 16.1 state, and event history unchanged. Required results must be nonempty with zero failed, skipped, or not-run. Diagnostics carry no tenant or personal data.
- **Authority.** The formal entry path retains the `ACTIVE` hold. It requires a qualified Platform tool, module adoption, operational envelope, provider parity, recovery drill, operator coverage, capacity evidence or an accepted waiver, human SM-2 evidence, and owner entry. The formal prerequisites and their owners are recorded in the [entry investigation](story-16-1-entry-investigation-2026-10-30.md) and the [production operational envelope](../planning-artifacts/production-operational-envelope-v1.md).

## Technical Decisions

- **Ownership.** EventStore history is the only Conversation write authority, and projections are derived and rebuildable. Platform owns hosting and providers. Shared capabilities go into EventStore or Tenants as additive seams, never duplicated in Conversations.
- **Sole lifecycle authority.** The v2 `ConversationProjectionIndexReadModel` at `projection:conversations-index:<tenant-segment>` carries lifecycle, conversation watermarks and dispatch references. The AD-7 design places the Tenants watermark and tenant-wide rebuild barrier and manifest in that record when shared rebuild is implemented. There is no separate lifecycle key, and lifecycle and entries share one CAS boundary. Only `ConversationProjectionReadModelWriter` authors transition plans. EventStore executes them.
- **Lifecycle design.** The initial index transitions shipped in Story 16.2. Tenant-wide erase and shared rebuild remain follow-up work, as recorded in the [Platform handoff](../../module/platform-handoff.md#story-162-read-state-change--2026-11-03).
  - `TenantCreated` with `Absent` goes to `Ready(empty)`.
  - `Absent` or v1 goes to `Rebuilding`.
  - `Ready` goes to `Erasing`, then `Erased`.
  - `Ready` or `Erased` goes to `Rebuilding`, then `Ready`; leaving `Erased` needs an owner-authorized rebuild.
- **Generations.** The planned tenant-wide erase and rebuild protocols fence detail, dispatch, manifest, watermarks and queued references by generation. A mismatch while proving a returned conversation produces `Rebuilding` / `mixed_generation`. A contradictory tenant index makes the entire list `Unavailable` / `metadata_contradictory`.
- **Planned tenant erase.** Delete only retiring-generation targets, and keep the tombstone plus non-payload quarantine references. A delivery during `Erasing` or `Erased` is acknowledged only after it is durably quarantined, and it can never recreate content.
- **Planned tenant-wide rebuild.**
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

The list reports freshness across accessible matches for the supplied filters, including matches on later pages. Read the state together with its reason and the rows actually returned.

| Condition | Public state / reason | What the caller can show |
| --- | --- | --- |
| Ready v2 index, current matches, completed dispatch | `Current` / `current` | Authorized summaries and the continuation when present |
| Ready v2 index, no matching rows, completed dispatch | `Current` / `current` | “No accessible matches.” Only a complete unfiltered index with zero entries supports a tenant-wide empty result. |
| Ready index, complete dispatch, stale is the worst freshness among matching summaries | `Stale` / `stale_threshold_exceeded` | Available summaries with their freshness warning. Ordinary rich detail withholds protected content while stale. |
| A returned item carries a redacted preview or citation | Item trust state `Redacted` | Its safe label or placeholder. Other rows and the page can still be current. |
| v1 index or v2 rebuilding lifecycle | `Rebuilding` / `rebuilding` | Proven rows and retry guidance |
| Index watermark gap | `Rebuilding` / `gap_detected` | Proven rows and retry guidance |
| Ready index with incomplete dispatch or a row without one proven generation | `Rebuilding` / `mixed_generation` | Show proven rows. A withheld row suppresses the continuation. Dispatch pending elsewhere can retain it. A native detail generation mismatch withholds the detail. |
| Missing index for an authorized tenant, or provider failure | `Unavailable` / `unavailable` | Retry guidance |
| Duplicate entries or contradictory index metadata | `Unavailable` / `metadata_contradictory` | Retry guidance |
| Tenant Read denied | `Forbidden` / `forbidden` | Safe denial before the index read |

A filtered empty page only describes that filter result. An absent index and a surviving detail record cannot establish that the tenant is empty. Index loss followed by a redelivered `TenantCreated` can still recreate an empty index. The [operational handoff](../../module/platform-handoff.md#story-162-read-state-change--2026-11-03) records this recovery limitation.

The UI labels the replay timestamp, the generation timestamp, and lag separately. It never confirms that protected data exists.

## Cross-Story Dependencies

- The chain is `7.4, IR-0 -> 16.1 -> 16.2 -> 16.3`. Story 16.3 also gates 12.1, 13.1, 14.1, and 15.1.
- Story 16.2 builds on 16.1's state and must not change it.
- Story 16.3 relies on 16.2's truthful query states.
- Each story used a scoped owner waiver for entry and the generated final pair, with native xUnit verification. The 16.3 decision also covered the existing non-packable AppHost fixture. Production qualification and release retain their own approval requirements. See the decisions for [16.1](spec-16-1-persist-tenant-access-projection-state-and-prove-convergence.md), [16.2](spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md) and [16.3](spec-16-3-diagnose-apphost-preflight-failures-and-prove-terminal-reconciliation-live.md).
