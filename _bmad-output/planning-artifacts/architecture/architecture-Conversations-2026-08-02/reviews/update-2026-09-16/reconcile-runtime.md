# Runtime Reconciliation — V15 AD-5 Through AD-8

- **Date:** 2026-09-16
- **Target reviewed:** `_bmad-output/planning-artifacts/architecture.md`, V15 AD-5 through AD-8
- **Inputs:** Epic 16 in the PRD epics; current `references/Hexalith.Tenants` tenant projection; current Conversations projection, query, rebuild, and AppHost code; required Hexalith LLM/state baselines
- **Mutation policy:** review only; `architecture.md` was not edited

## Verdict

**FAIL for runtime-convergence handoff; KEEP THE IMPLEMENTATION HOLD ACTIVE.**

AD-5 is a sound fail-closed entry gate, and AD-6 through AD-8 select the right
high-level owners and public outcomes. They do not yet form a buildable
consistency contract for Epic 16. The remaining defects are load-bearing:

1. AD-6 permits a Tenants-owned durable-provider implementation even though the
   Hexalith baseline requires persisted read models and missing persistence
   mechanics to live in EventStore; its payload identity cannot be computed
   canonically from the current handler seam, and its gap/recovery state machine
   is not closed.
2. AD-7 does not define what “contiguous evidence” means for a tenant-wide index
   assembled from multiple aggregate streams, and its literal single-writer and
   tombstone rules conflict with the current rebuild/erase paths.
3. AD-8 chooses the domain-event time source but leaves
   `ProjectionGeneratedAt`, lag/staleness, and replay identity unresolved. Two
   clean rebuilds can therefore still produce different JSON and hashes.

The operational envelope named by AD-5 does not exist, so Story 16.1 is blocked.
Stories 16.2 and 16.3 remain transitively blocked. Story 16.3 also has a separate
hard gate: the repository-local AppHost conflicts with the required Hexalith
baseline until the baseline owner grants an explicit exemption or the fixture
is relocated.

## Decision-by-decision reconciliation

| Decision | Epic 16 fit | Brownfield feasibility | Baseline fit | Disposition |
| --- | --- | --- | --- | --- |
| AD-5 | Correctly adds the production parity/provider/recovery gate that Story 16.1 needs. | Counterpart artifact is absent; current AppHost proof is local and directly seeds Tenants state rather than composing production Tenants. | Production topology belongs outside the domain module. | **Retain. Gate remains closed.** Add CAS/provider qualification to the counterpart artifact. |
| AD-6 | Correctly selects per-tenant atomic mutation, exact-next ordering, duplicate/corruption distinction, fail-closed gaps, and replay-owned recovery. | Current store is `GetAsync` + unconditional `SaveAsync`; handler serialization is process-local only and current tests accept noncontiguous starts/equal-sequence reapplication. | As written, “Tenants.Client owns … durable providers/replay” can authorize hand-rolled persistence/replay plumbing, which the baseline forbids. | **Amend before Story 16.1.** Bind the EventStore persistence/fingerprint/replay seam and close the unsafe-state transition table. |
| AD-7 | Correctly makes the existing tenant index the one Conversations lifecycle authority and gives exact public mappings. | Existing CAS writer is a good base, but rebuild plans also write the key and the platform erase flow deletes targets. A tenant-wide contiguous watermark is undefined. | Using `IReadModelStore`/`ReadModelWritePolicy` is compliant; a domain-owned replacement for generic erase/rebuild plumbing is not. | **Amend before Story 16.2.** Define the watermark proof domain and platform-coordinated tombstone/rebuild protocol. |
| AD-8 | Correctly gives payload occurrence time precedence for Conversations events and envelope persisted time for position-only events; its safe mapping is compatible with the public vocabulary. | The named envelope type is wrong for the projection seam, event identity is ambiguous, invalid time currently fails decode, and rebuild generation still reads `TimeProvider`. | Domain-specific selection belongs in Conversations; generic envelope transport remains EventStore-owned. | **Amend before Story 16.2.** Bind every persisted freshness time field and identity source to immutable inputs. |

## Findings

### RT-1 — HIGH — AD-6 crosses the EventStore persistence boundary and lacks an enforceable event fingerprint

AD-6 says Tenants.Client owns one atomic conditional mutation and binds
`ITenantProjectionStore` plus “its durable providers” (`architecture.md:2849-2867`).
The required baseline instead says domain persisted read models use
`IReadModelStore` plus `ReadModelWritePolicy`, and missing persistence capability
is added to EventStore rather than hand-rolled in the domain
(`hexalith-state-instructions.md:5-14,40-50`; `hexalith-llm-instructions.md:59-63,121-134`).
EventStore already provides the required ETag-aware primitive and a three-attempt
reload/merge loop (`IReadModelStore.cs:23-70`;
`ReadModelWritePolicy.cs:28-101`).

The current Tenants surface proves why the amendment is necessary:

- `ITenantProjectionStore` exposes only `GetAsync` and unconditional `SaveAsync`
  (`ITenantProjectionStore.cs:6-21`).
- `TenantProjectionEventHandler` protects only one process with a semaphore,
  accepts any absent-state sequence, ignores only strictly older events, and then
  unconditionally saves (`TenantProjectionEventHandler.cs:25-26,125-150`).
- persisted metadata carries message ID, sequence, timestamp, and correlation,
  but no payload/event-type fingerprint or unsafe-state metadata
  (`TenantProjectionEventMetadata.cs:6-10`).

The architecture's `canonicalPayloadDigest` is not implementable consistently
from this seam. The handler receives a deserialized event plus
`EventStoreDomainEventContext`; that context does not carry raw payload bytes,
serialization format, or event type. Re-serializing the CLR object would make the
digest dependent on serializer settings/property representation and can omit
unknown additive fields. The EventStore processor still has the original
envelope and is therefore the only safe layer to compute a canonical delivery
fingerprint.

**Required correction:** Tenants owns the tenant-projection transition function,
state/key grammar, and safe outcome. EventStore owns the durable
`IReadModelStore` provider, optimistic-concurrency policy, raw-envelope
fingerprint, subscription/replay transport, and any missing generic recovery
seam. Bind either:

- `ITenantProjectionStore` as a Tenants-facing adapter over
  `IReadModelStore`/`ReadModelWritePolicy`, with no independent durable provider;
  or
- a new EventStore atomic projection-mutation abstraction that supplies the
  expected ETag and a platform-computed fingerprint.

The fingerprint contract must name its exact components and format. At minimum
it must include event type, serialization format, and original payload bytes (or
an EventStore-owned canonical equivalent), be stable across replicas/restarts,
remain internal/non-logged, and define behavior when legacy input lacks a
fingerprint.

### RT-2 — HIGH — AD-6 does not close the gap/corruption/recovery state machine

The ordering clauses are directionally correct but leave incompatible legal
implementations. For a current sequence `n` and observed `n+2`, the rule does not
say whether the persisted cursor remains `n`, advances to `n+2`, or stores both;
whether later live delivery of `n+1` may apply; whether any live event may clear
`GapDetected`; or what atomic evidence replaces the unsafe state after replay.
“Independently verified snapshot boundary” likewise has no authority, schema, or
verification rule.

This matters because Epic 16 requires restart identity, two-replica convergence,
and explicit gap/corrupt fault proofs (`epics.md:4392-4412`). A fail-closed but
permanently stuck state does not satisfy recovery. The Story 16.3
`project/v2/reconcile` route owns named Conversations projection delivery, not
the Tenants.Client access projection; it cannot silently become the tenant-gap
repair route.

**Required correction:** bind an explicit transition table with persisted fields
such as `lastContiguousSequence`, `lastIdentity`, `health`,
`expectedNextSequence`, and `firstObservedGapSequence`. A gap/corruption event
must atomically preserve the last safe authorization state, record bounded unsafe
metadata, and deny all trust-bearing decisions. Live delivery must not clear the
unsafe flag. Define one EventStore-owned replay/backfill invocation and one
Tenants-owned deterministic replacement proof; only a CAS write of a complete,
contiguous replay result may return the record to current. Remove the snapshot
exception unless a concrete platform-attested snapshot contract is named.

### RT-3 — HIGH — AD-7's tenant-wide watermark has no valid contiguity domain

`ConversationProjectionIndexReadModel` is a per-tenant index of many Conversation
aggregates. Its current entries carry per-conversation aggregate positions
(`ConversationProjectionIndexReadModel.cs:20-32`), while
`ProjectionEventDto.GlobalPosition` explicitly says positive global values may
contain gaps and do **not** prove contiguous consumption
(`ProjectionEventDto.cs:86-93`). AD-7 nevertheless maps a “source-position gap”
and lets rebuild become current only after “contiguous evidence” without saying
which stream is contiguous (`architecture.md:2877-2895`).

Two builders can therefore comply differently: one can require every
Conversation summary's aggregate sequence to be contiguous; another can treat
the highest tenant/global position as a watermark. The latter is unsound, while
the former still cannot prove that no Conversation aggregate is absent from the
index without an authoritative aggregate inventory.

**Required correction:** name the proof domain. A compatible minimum is:

- tenant initialization is proven by the Tenants `TenantCreated` event identity
  recorded in the v2 index;
- each indexed Conversation proves its own contiguous aggregate sequence through
  its dispatch reference/detail generation;
- `Current` requires v2 initialization, no lifecycle/rebuild tombstone, no
  pending dispatch, no per-conversation gap/contradiction, and a platform-owned
  rebuild-completeness proof for the tenant scope;
- `GlobalPosition` may be stored only as a high-water observation and never as
  contiguity evidence.

If EventStore cannot provide the aggregate-inventory/rebuild-completeness proof,
the architecture must narrow the claim instead of inventing it in Conversations.

### RT-4 — HIGH — AD-7's literal single-writer/tombstone rule conflicts with current platform rebuild and erase mechanics

The immediate path is compatible with the selected owner:
`ConversationProjectionReadModelWriter` already mutates the index through
`ReadModelWritePolicy` CAS (`ConversationProjectionReadModelWriter.cs:15-31,95-123,125-163`).
The full-replay path does not route the mutation through that writer. Instead,
`ConversationAsyncProjectionHandler.PrepareRebuildAsync` reads and constructs the
entire index and returns a direct batch write to its key
(`ConversationAsyncProjectionHandler.cs:223-296,350-385`). Thus “only
`ConversationProjectionReadModelWriter` mutates it” is false unless “writer” is
defined as the sole owner of mutation **logic** and the handler delegates plan
construction to it.

The tombstone rule also needs a platform change or explicit coordination. The
generic EventStore eraser conditionally deletes registered targets and verifies
that they are absent before completing. A rule that the same target remains as a
`Rebuilding` tombstone cannot be implemented by a Conversations-specific erase
path without duplicating platform plumbing, which the baseline forbids.

**Required correction:** make the writer the single owner of index transition
functions and batch-operation construction, while EventStore remains the
physical executor. Bind a platform-owned erase/rebuild protocol that either:

- excludes the lifecycle anchor from deletion and CAS-replaces it with a
  tombstone before deleting generation data; or
- adds a generic replace-with-tombstone operation to the EventStore lifecycle.

Define conflict/resume behavior when TenantCreated initialization, live
projection dispatch, erasure, and rebuild race on the same v2 key. The v1→v2
rule also needs an explicit persisted discriminator whose absence is recognized
as v1; a property defaulting to 2 is not a valid legacy detector.

The public outcome table itself is feasible and compatible with the existing
closed vocabulary: `current`, `rebuilding`, `gap_detected`, `unavailable`,
`metadata_contradictory`, and `forbidden` already exist. Authorization-first/no
index-read behavior also matches the current query ordering. Preserve that
table.

### RT-5 — HIGH — AD-8 still permits non-deterministic rebuild JSON

AD-8 correctly chooses `ConversationEventMetadata.OccurredAt` for normal
Conversations events and the persisted envelope timestamp for position-only
events. The current materializer already follows that broad precedence
(`ConversationProjectionMaterializer.cs:630-670`), but the architecture names
`EventStoreDomainEventContext.Timestamp`; the named projection/rebuild seam
actually receives `ProjectionEventDto.Timestamp` through
`ConversationProjectionEventDecoder` (`ConversationProjectionEventDecoder.cs:171-207`).
This should be corrected to avoid mixing subscription and projection contracts.

More importantly, AD-8 binds rebuild hashes and lag calculation but decides only
`LastAppliedEventTimestamp`. The persisted `ProjectionGeneratedAt` and
`LagDuration` remain free. Current rebuild preparation uses
`_timeProvider.GetUtcNow()` when a ledger does not already exist
(`ConversationAsyncProjectionHandler.cs:271-277`), and the materializer embeds
that value into `ProjectionGeneratedAt`, lag, stale state, summary, and detail
(`ConversationProjectionMaterializer.cs:115-190`). Two clean rebuilds with
different operation identities can therefore produce different JSON, directly
violating AC-16.2-02's byte-identical requirement (`epics.md:4438-4445`).

**Required correction:** add a per-field immutable-time table for at least:

| Persisted/public field | Required immutable source |
| --- | --- |
| `LastAppliedEventTimestamp` | Maximum selected replay-visible domain time after UTC normalization. |
| `ProjectionGeneratedAt` | One explicitly named immutable event-derived anchor; never `TimeProvider`, query time, or rebuild invocation time. |
| `LagDuration` / `IsStale` / freshness reason | Deterministic function of the two named immutable anchors and the fixed threshold, or explicitly excluded/redefined through a public-contract decision. |
| evidence/hash timestamps | The same selected anchors; no clean-rebuild operation clock. |

The chosen generation anchor must preserve the public invariant
`ProjectionGeneratedAt >= LastAppliedEventTimestamp`. If envelope persisted time
is selected for that field, define the typed outcome when producer occurrence
time is later than it; do not clamp silently to a wall clock.

### RT-6 — MEDIUM — Replay identity and invalid-time mapping are ambiguous at the actual decoder seam

AD-8 says the same event identity with a different selected time is contradictory
but does not say whether identity means public `ConversationEventMetadata.EventId`,
persisted `ProjectionEventDto.MessageId`, `(aggregate, sequence)`, or the complete
delivery fingerprint. Current code deduplicates normal events by public EventId
and silently ignores repeats before comparing timestamps
(`ConversationProjectionMaterializer.cs:646-670`). For position-only events the
decoder discards `ProjectionEventDto.MessageId` entirely and retains only
sequence plus timestamp (`ConversationProjectionEventDecoder.cs:193-197`), even
though the DTO supplies MessageId (`ProjectionEventDto.cs:71-81`).

Invalid domain time is also rejected during contract deserialization by
`ConversationEventMetadata`; the async handler maps decoder exceptions to a
generic failed/handler-failure result, not a persisted
`Unavailable/metadata_contradictory` projection. The desired public mapping is
valid, but the architecture must state which layer converts invalid time into a
typed invalid-event marker without applying domain state.

**Required correction:** use the public EventId as the domain-event identity and
the persisted MessageId as the position-only identity, require their bounded
presence where applicable, preserve them in `ConversationProjectionEventRecord`,
and compare first-seen selected UTC ticks on a duplicate. Bind missing/invalid
identity to the same or another explicit safe mapping. Have the decoder emit a
typed invalid-time record that advances no trusted state; the materializer then
emits `Unavailable/metadata_contradictory` deterministically.

### RT-7 — GATE — AD-5 and the AppHost deferral correctly block Epic 16 today

`_bmad-output/planning-artifacts/production-operational-envelope-v1.md` is absent.
AD-5 makes it a hard entry condition for Story 16.1
(`architecture.md:2831-2847`), and Epic 16 orders `16.1 -> 16.2 -> 16.3`
(`epics.md:4381-4387,4415-4417,4447-4449`). No Epic 16 story may therefore enter
implementation from this update.

The counterpart artifact must qualify the selected production state provider
for the exact first-write/ETag semantics used by AD-6/AD-7, not merely name a
provider. It must also bind persistence/retention, backup, replay throughput,
cross-replica consistency, secret/identity ownership, alerts, operator recovery,
and waiver expiry to local/CI/staging/production parity.

The AppHost gate is also correctly explicit. The baseline says a domain module
must not ship its own `*.AppHost` and that local orchestration lives in a
platform/host repository (`hexalith-llm-instructions.md:121-134,215-233`;
`hexalith-state-instructions.md:25-38`). The current repository contains
`src/Hexalith.Conversations.AppHost`; setting `IsPackable=false` and
`IsPublishable=false` does not itself create a baseline exemption
(`Hexalith.Conversations.AppHost.csproj:1-18`). AD-7's Deferred table correctly
requires a baseline-owner decision before Story 16.3
(`architecture.md:2966-2974`). Preserve that hard gate: either record an explicit
fixture exemption, or relocate the fixture/proof to the platform/host boundary.
The existing module AppHost may be inspected as brownfield evidence but cannot
establish production parity or authorize new module-owned runtime mechanics.

## Required amendment set

Before the updated spine can claim that Epic 16 runtime divergence is closed:

1. **Amend AD-6 ownership:** Tenants owns transition semantics; EventStore owns
   durable CAS provider, raw-envelope fingerprint, subscription, and replay
   plumbing.
2. **Publish the AD-6 state machine:** exact state fields, duplicate/corruption/
   gap transitions, retry-exhaustion outcome, and the sole replay-clear proof.
3. **Amend AD-7 watermark semantics:** name per-aggregate versus tenant/rebuild
   evidence and prohibit treating global high-water as contiguous.
4. **Bind platform erase/rebuild coordination:** make the Conversations writer
   own transition logic/plan construction while EventStore executes tombstone,
   erase, rebuild, and resume mechanics.
5. **Amend AD-8 time table:** name the actual projection DTO source, event
   identities, invalid-time conversion, deterministic `ProjectionGeneratedAt`,
   lag/staleness, and evidence/hash inputs.
6. **Keep gates closed:** require the production operational envelope before
   16.1 and the AppHost baseline-owner disposition before 16.3. Neither local
   tests nor non-shipping project flags satisfy those gates.

## What already converges and should be preserved

- EventStore history remains source truth; tenant and Conversation projections
  are derived and fail closed.
- AD-6's exact-next ordering, equal-sequence mismatch classification, three-total-
  attempt budget, no query repair, and concurrency/restart fault schedule are the
  correct invariants once placed on the platform seam.
- AD-7's existing-key choice and public mapping table close the initialized-empty
  versus missing ambiguity without widening public vocabulary.
- AD-8's domain-occurrence-over-envelope precedence for normal Conversations
  events, UTC normalization, and `Unavailable/metadata_contradictory` mapping are
  correct.
- AD-5's counterpart ownership and waiver authority are appropriate; its missing
  artifact is a deliberate blocker, not permission to infer local topology as
  production.

## Final handoff

Do not mark the runtime portion of V15 reviewer-clean until RT-1 through RT-6
are reflected in the spine or converted into explicit hard-entry counterpart
contracts with named owners and revisit conditions. RT-7 is already represented
as a hard gate and should remain so. No source, submodule, AppHost, or dependency
change is authorized by this reconciliation.
