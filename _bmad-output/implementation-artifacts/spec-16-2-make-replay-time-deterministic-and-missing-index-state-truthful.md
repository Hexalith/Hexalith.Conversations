---
title: 'Story 16.2: Make replay time deterministic and missing-index state truthful'
type: 'feature'
created: '2026-11-03'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projection replay mixes event time with the wall clock. `LastAppliedEventTimestamp` falls back to the generation clock, and a contradictory generation time is clamped. Duplicates are matched by `EventId` alone, and position-only events lose their `MessageId`. So identical histories can materialize differently, and a changed reuse goes undetected. The tenant index has no lifecycle, so a missing or destroyed index reads as a current empty page (disclosed limitation D3).

**Approach:** Apply AD-8: take identity and time from validated event inputs, bind duplicates to the full binding, and persist internal deterministic replay state with `ReplayAnchorAt` and a `ReplayStateHash` over EventStore's canonical bytes. Apply AD-7: version 2 of the tenant index becomes the only lifecycle/watermark record, `TenantCreated` initializes it `Ready(empty)`, and reads map v1, missing and contradictory states truthfully without writing.

**2026-11-03 decisions:**
1. **Entry.** The user waived the Story 16.2 runtime-entry ceremony as for 16.1 (ACTIVE hold, owner qualifications, AD-5 envelope, tooling-v3 binding). The work follows the routine current-change path. Waived items are recorded as observed or waived, never as PASS. Completion proof is native xUnit only; the tooling-v3 `6/6/0/0/0/0` final pair is not required. Commits stay local, and nothing is pushed without an explicit ask.
2. **Rebuild.** Adoption of EventStore's tenant shared rebuild (`IAsyncDomainSharedProjectionRebuildHandler`) is deferred to a follow-up story. Until it lands, tenants created before 16.2 (v1 or missing index) read `Rebuilding` or `Unavailable`, which is truthful. Only `TenantCreated` creates `Ready`.
3. **Erase.** The tenant erase lifecycle (`Erasing`/`Erased`, quarantine, generation fencing) is deferred, because no producer exists. Generation fields arrive with the first story that increments them.
4. **Size.** The full spec size (about 3,500 tokens) is kept.

## Boundaries & Constraints

**Always:**
- EventStore history is the only write authority.
- Only `ConversationProjectionReadModelWriter` authors index transitions, and lifecycle fields share one CAS with index entries.
- Public contracts and their snapshot baselines stay unchanged; every reason code needed already exists.
- `ProjectionGeneratedAt` stays the actual generation instant and is excluded from replay identity.
- Story 16.1 tenant-access state and behavior are preserved.
- Diagnostics carry no tenant or personal content.

**Never:**
- Replay clock fallbacks or timestamp clamping.
- Inferring lifecycle on read, or writing on the query path.
- A separate lifecycle key, or Tenants state deciding emptiness.
- New public members.
- Rewriting accepted evidence bytes.

## I/O & Edge-Case Matrix

| Input / State | Expected behavior |
| --- | --- |
| Contract event | Identity `Metadata.EventId`, time `OccurredAt`; envelope time is diagnostic only |
| Position-only event | Identity nonblank persisted `MessageId`, time envelope `Timestamp` |
| Missing identity, or default/invalid time | Fails before deduplication: `Unavailable`/`metadata_contradictory` |
| Same identity and full binding | No-op |
| Same identity, different binding (type, sequence, format, payload SHA-256, UTC ticks) | `Unavailable`/`metadata_contradictory` |
| Generation instant earlier than anchor | `Unavailable`/`metadata_contradictory`, unclamped generation time, null lag |
| `TenantCreated` with no index | v2 `Ready(empty)`; an existing index is left unchanged |
| v2 `Ready`, zero summaries, no pending dispatch | `Current`/`current`, empty page |
| v1 index or v2 `Rebuilding` | `Rebuilding`/`rebuilding` |
| Lifecycle or source-position gap | `Rebuilding`/`gap_detected` |
| Missing index (authorized tenant) or provider failure | `Unavailable`/`unavailable` |
| Duplicate entries or contradictory lifecycle record | `Unavailable`/`metadata_contradictory` |
| Unauthorized or nonexistent tenant | `Forbidden`/`forbidden`, no index read (unchanged) |

</frozen-after-approval>

## Code Map

Server paths are under `src/Hexalith.Conversations.Server/`. EventStore is at `b46fa93e`.

**Decoding**
- `Projections/ConversationProjectionEventDecoder.cs:196-210`: wraps a position-only event as `ConversationProjectionPositionOnlyEvent(evt.Timestamp)`, which drops `MessageId`; `Timestamp` is never validated.
- `Projections/ConversationProjectionEventDecoder.cs:125`: the sync path swallows invalid metadata as `isRebuilding`.
- `ConversationProjectionEventRecord.cs:13` is `(Position, Event)` only. Extend both records to carry the binding.
- Payload hash: the public `EventStorePayloadFingerprint.Compute` (EventStore Client `Subscriptions/`).

**Materialization**
- `Projections/ConversationProjectionMaterializer.cs`:
  - `:716-726`: deduplicates on `EventId` only.
  - `:751-753` and `:889`: position-only ordering and time.
  - `:127-134`: the clock fallback and clamp.
  - `:274/284/294/316`: eligibility and evidence times taken from the generation clock; they must use the anchor.
  - `:329/387`: evidence falls back to `LastAppliedEventTimestamp`.
- `Projections/ConversationProjectionHandler.cs:108`, and `Projections/ConversationAsyncProjectionHandler.cs:170,198,347`: clock reads. Keep them only for `ProjectionGeneratedAt`.
- `Projections/ConversationAsyncProjectionHandler.cs`:
  - `:419`: the index tie-break uses generation time.
  - `:487-511`: the rebuild batch.
  - `:727-778`: the request fingerprint. Keep it.

**Replay state**
- `Projections/ConversationProjectedReadModels.cs`: the internal detail wrapper. Add `ReplayAnchorAt` and `ReplayStateHash` here, so the public contract is not touched.
- EventStore `Hexalith.EventStore.Client/Projections/ReadModelBatchCanonicalJson.Serialize` is internal, but `InternalsVisibleTo` exposes it to Server (`Hexalith.EventStore.Client.csproj:13`). It is the source of the canonical bytes.

**Index and writer**
- `Projections/ConversationProjectionIndexReadModel.cs`: holds only `Summaries` and `Dispatches` today. A record with no version is v1.
- `Projections/ConversationProjectionReadModelWriter.cs`: index merge at `:133-146`, index read/update at `:305/:328`.

**Reads**
- `Projections/ConversationProjectionReadStore.cs`:
  - `:237-251`: D3, where a null index becomes `Empty`.
  - `:257-260`: a duplicate summary throws.
  - `:271-275`: incomplete dispatch. Keep it.
- `Projections/IConversationProjectionReadStore.cs:44-48`: documents the D3 behavior.
- `Queries/ConversationQueryHandler.cs`:
  - `:232-245`: authorization runs before any read. Keep it.
  - `:250-265`, `:310-323`, `:362-364` and `AggregateFreshness` (~`:497`): the state mapping.
- No query path writes today. Prove that with a ledger.

**Tenant feed**
- `TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs:93-98` registers the Tenants handlers.
- The EventStore processor runs every `IEventStoreDomainEventHandler<T>` in registration order (`EventStoreDomainEventProcessor.cs:339`).
- `TenantCreated(TenantId, Name, Description, CreatedAt)` is defined at `Tenants.Contracts/Events/TenantCreated.cs:3`.

**Tests that pin today's behavior**
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs:480`: an absent index reads as empty.
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs:734,751`: expect `mixed_generation`.
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs:99-136`: an erased index lists as `Current`.
- Several fakes return `IndexSnapshot.Empty`.
- `docs/release-evidence/projection-read-store-population-proof-v2.json` is accepted evidence. Leave it unchanged.
- The snapshot baselines must stay green: `tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` and the Client `PublicClientApiSnapshotTest`.

## Tasks & Acceptance

**Execution:**
- [ ] Decoder and event records -- carry identity, selected time, event type, format and payload SHA-256. Reject a blank identity or a default/invalid time before deduplication -- AD-8 binding.
- [ ] `Projections/ConversationProjectionMaterializer.cs`:
  - deduplicate on the full binding;
  - set the anchor to the maximum validated selected time;
  - remove the clock fallback and clamp;
  - derive eligibility and evidence times from the anchor;
  - map a contradiction to `metadata_contradictory` with null lag.

  Rationale: deterministic replay.
- [ ] New internal `Projections/ConversationReplayState*` -- build the deterministic replay-state value with ordinally ordered collections; an unordered collection fails before persistence. Canonicalize it through EventStore and hash it. Persist the anchor and hash in `ConversationProjectedReadModels` -- AD-8 identity.
- [ ] Both projection handlers -- read the clock only for `ProjectionGeneratedAt`. Neither the rebuild nor the tie-break uses the clock -- determinism.
- [ ] `Projections/ConversationProjectionIndexReadModel.cs` and `Projections/ConversationProjectionReadModelWriter.cs` -- add `SchemaVersion=2`, `Lifecycle` and non-regressing per-conversation watermarks. A live delivery to an absent or v1 index produces `Rebuilding` -- AD-7.
- [ ] New `Projections/ConversationTenantIndexInitializer.cs`, plus registration -- `TenantCreated` (domain `tenants`) asks the writer for a CreateOnly v2 `Ready(empty)`. Register it before the Tenants handlers, so the index exists before tenant access can authorize. Retryable failures throw -- truthful empty.
- [ ] `Projections/ConversationProjectionReadStore.cs`, `Projections/IConversationProjectionReadStore.cs` and `Queries/ConversationQueryHandler.cs` -- map lifecycle per the matrix and remove the D3 text -- truthful reads.
- [ ] Tests:
  - unit tests for every matrix row;
  - the two-rebuild determinism test under two fake clocks;
  - a write-ledger store around the list, detail and advisory queries;
  - a test that pins the initializer's registration order;
  - existing tests updated only where the matrix changes them;
  - live Dapr cases: `TenantCreated` gives `Current` empty, an erased index gives `Unavailable`, and the first conversation of an initialized tenant gives `Current`.
- [ ] `module/platform-handoff.md` -- add the read-state change and the behavior of pre-16.2 tenants -- operator truth.

**Acceptance Criteria:**
- Given identical authoritative histories, when two clean rebuilds run under different clocks, then the canonical replay-state bytes, `ReplayAnchorAt` and `ReplayStateHash` are byte-identical. `LastAppliedEventTimestamp` equals the anchor, `ProjectionGeneratedAt` is the clock instant, and `LagDuration` is their difference.
- Given a write ledger around every query path, when queries run in every matrix state, then it records zero writes.
- Given the change, when Server, Integration, Contracts, Conformance-snapshot and Client-snapshot tests run individually in mapped Debug, then they pass with zero skips and the baselines are unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Replay state is the materialized detail and summary with observation freshness removed. It keeps `LastAppliedEventPosition` and `ReplayAnchorAt`. `ReplayStateHash` is the lowercase-hex SHA-256 of `ReadModelBatchCanonicalJson.Serialize(state)`.

On a contradiction, the public freshness keeps the real `ProjectionGeneratedAt`, with `LagDuration = null` and `Unavailable`/`metadata_contradictory`. The contract only rejects a negative lag, so no clamp is needed.

The `TenantCreated` initializer runs before the Tenants handler, because the processor dispatches sequentially. A tenant becomes `Healthy` only after its index is `Ready`, which closes the race in which a conversation is created first.

## Verification

**Commands:**
- Prepare the source-mapped build (`dotnet tool run hexalith-module -- prepare-build --mode source`). Then build `Server.Tests`, `IntegrationTests`, `Contracts.Tests`, `Conformance.Tests` and `Client.Tests` in Debug, and run each assembly directly with `-trx`. Expected: pass, nonzero counts, 0 skipped.
- `python3 scripts/check-root-submodules.py --repository .` and `git diff --check`. Expected: pass.
