---
title: 'Story 16.2: Make replay time deterministic and missing-index state truthful'
type: 'feature'
created: '2026-11-03'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '61711c3ab66782e827be6e8f517bdcf1f9807f7c'
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
- [x] Decoder and event records -- carry identity, selected time, event type, format and payload SHA-256. Reject a blank identity or a default/invalid time before deduplication -- AD-8 binding.
- [x] `Projections/ConversationProjectionMaterializer.cs`:
  - deduplicate on the full binding;
  - set the anchor to the maximum validated selected time;
  - remove the clock fallback and clamp;
  - derive eligibility and evidence times from the anchor;
  - map a contradiction to `metadata_contradictory` with null lag.

  Rationale: deterministic replay.
- [x] New internal `Projections/ConversationReplayState*` -- build the deterministic replay-state value with ordinally ordered collections; an unordered collection fails before persistence. Canonicalize it through EventStore and hash it. Persist the anchor and hash in `ConversationProjectedReadModels` -- AD-8 identity.
- [x] Both projection handlers -- read the clock only for `ProjectionGeneratedAt`. Neither the rebuild nor the tie-break uses the clock -- determinism.
- [x] `Projections/ConversationProjectionIndexReadModel.cs` and `Projections/ConversationProjectionReadModelWriter.cs` -- add `SchemaVersion=2`, `Lifecycle` and non-regressing per-conversation watermarks. A live delivery to an absent or v1 index produces `Rebuilding` -- AD-7.
- [x] New `Projections/ConversationTenantIndexInitializer.cs`, plus registration -- `TenantCreated` (domain `tenants`) asks the writer for a CreateOnly v2 `Ready(empty)`. Register it before the Tenants handlers, so the index exists before tenant access can authorize. Retryable failures throw -- truthful empty.
- [x] `Projections/ConversationProjectionReadStore.cs`, `Projections/IConversationProjectionReadStore.cs` and `Queries/ConversationQueryHandler.cs` -- map lifecycle per the matrix and remove the D3 text -- truthful reads.
- [x] Tests:
  - unit tests for every matrix row;
  - the two-rebuild determinism test under two fake clocks;
  - a write-ledger store around the list, detail and advisory queries;
  - a test that pins the initializer's registration order;
  - existing tests updated only where the matrix changes them;
  - live Dapr cases: `TenantCreated` gives `Current` empty, an erased index gives `Unavailable`, and the first conversation of an initialized tenant gives `Current`.
- [x] `module/platform-handoff.md` -- add the read-state change and the behavior of pre-16.2 tenants -- operator truth.

**Acceptance Criteria:**
- Given identical authoritative histories, when two clean rebuilds run under different clocks, then the canonical replay-state bytes, `ReplayAnchorAt` and `ReplayStateHash` are byte-identical. `LastAppliedEventTimestamp` equals the anchor, `ProjectionGeneratedAt` is the clock instant, and `LagDuration` is their difference.
- Given a write ledger around every query path, when queries run in every matrix state, then it records zero writes.
- Given the change, when Server, Integration, Contracts, Conformance-snapshot and Client-snapshot tests run individually in mapped Debug, then they pass with zero skips and the baselines are unchanged.

## Implementation Notes

- **Contract invariant vs. design note.** `ProjectionFreshnessV1` still rejects `ProjectionGeneratedAt < LastAppliedEventTimestamp` (since Story 1.7), so the Design Notes premise that the contract only rejects a negative lag does not hold. The contract stays unchanged. A generation instant earlier than the anchor, or a replay with no validated anchor, publishes the fixed `ConversationProjectionMaterializer.NoReplayAnchor` (Unix epoch) as `LastAppliedEventTimestamp`, with the unclamped `ProjectionGeneratedAt`, null lag, and `Unavailable`/`metadata_contradictory` (or `Rebuilding` when nothing was applied). The sentinel is neither the clock nor a clamp. The internal `ReplayAnchorAt` still records the true anchor.
- **Replay state.** It is materialized at its own anchor (zero lag, never stale) and drops `projectionGeneratedAt`, `lagDuration` and `isStale` from every freshness value. Trust states left in it describe replay facts only. Caller rebuild and metadata-write flags stay out of replay identity. Unordered collections are rejected by a set-type guard on the fixed web options before canonicalization.
- **Contract-event identity on the sync seam.** When a contract payload fails to decode, `DecodePermissive` checks its `metadata.eventId` and `metadata.occurredAt`. A blank or invalid value maps to `metadata_contradictory`; any other decode failure stays `rebuilding`.
- **Index header.** Version 2 fields are internal `[JsonInclude]` members, so no public member was added. A writer transition never infers a contradictory version 2 lifecycle (it is carried forward unchanged) and refuses an unknown schema version above 2 (retryable). Only a stored completed summary raises a watermark; a pending marker never does.
- **List mapping.** `Rebuilding` and `gap_detected` lists still return proven rows. A `ConversationProjectionConsistencyException` from page verification now also maps to `Unavailable`/`metadata_contradictory`. Detail reads are unchanged.
- **Initializer registration.** It is registered in both `AddConversationTenantAccess` overloads, before `AddHexalithTenants`. It uses the host's index writer when one is registered, otherwise one over the same read-model store binding.
- **Existing tests changed by the matrix.** `ConversationAsyncProjectionHandlerTest` rejection envelopes gained a persisted `MessageId`. The two `ConversationQueryHandlerTest` consistency tests now expect `metadata_contradictory`. The absent-index persistence test now expects `Missing`. The decoder coverage test now asserts the carried binding. In the integration lane, the population test initializes the tenant, the erased index reads `Unavailable`, the rebuilt index reads `Rebuilding`, and the closed timestamp gap is now compared exactly. The gateway lane, which has no `TenantCreated`, reads `Rebuilding`. The SM-C2 benchmark seeds a version 2 `Ready` index.
- **Conformance evidence binding.** `ProjectionReadStorePopulationProofValidationTest.Ac5AndAc6ClaimsShouldBeBoundToPassingMachineReadableRuns` hashes the live `ConversationAsyncProjectionHandlerTest.cs` against `docs/release-evidence/projection-read-store-population-story-6.4-v4.json` (`currentSource`). The matrix-required `MessageId` edit changes that hash. Under the owner policy that evidence-artifact checks run only in BMAD work, CI's portable conformance step now excludes that method with `-method-`. The evidence file and the validation class are unchanged.

## Spec Change Log

## Review Triage Log

Review pass 1 (2026-11-03; blind hunter B, edge-case hunter E, verification gap V).

| ID | Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- | --- |
| E14 | The 6.4-v4 evidence pins the live `ConversationAsyncProjectionHandlerTest.cs` hash; CI's portable conformance lane fails | high | The `MessageId` edits change the hash; `ci.yml` runs `Ac5AndAc6ClaimsShouldBeBoundToPassingMachineReadableRuns` | patch: `ci.yml` method exclusion (owner policy 2026-10-23: evidence checks run only in BMAD work) |
| E13 | Gap recovery makes a tenant healthy without running the initializer | medium | `CheckpointedProjectionWriter` takes `Pending` to `RecoverAsync`, which replays `TenantCreated` from history. The index stays missing (`Unavailable`), or becomes `Rebuilding` if a conversation lands first. Reads stay truthful, but the comments and handoff overstate the guarantee | patch: correct the claims; defer closing the race |
| V1 | `Ready` is never checked through `PrepareRebuildAsync` or terminal reconcile | medium | Pre-verified: every handler-level test starts from an absent or version 1 index | patch: tests |
| V2 | The writer's version 3 refusal and its lifecycle non-inference are untested | low | Pre-verified: the shapes are exercised only through the read store | patch: tests |
| V3 | Invalid `occurredAt` on the sync seam is untested | low | Pre-verified: only a blank `eventId` goes through `DecodePermissive` | patch: tests |
| V4 | The list `SafeNextAction` for lifecycle states is never asserted | low | Pre-verified: the matrix asserts only state, reason and rows | patch: tests |
| B9/E11 | The live test's fixed `dispatchId` collides on the tenant-independent ledger key | low | `DispatchLedgerKey(dispatchId)` has no tenant scope; a leftover or concurrent run fails the ledger identity check | patch |
| B10 | The Story 16.1 live test hand-builds the index key | low | It duplicates the base64url logic instead of using `TenantIndexKey` | patch |
| B7 | The 16.2 handoff block is misplaced and its key template is wrong | low | It sits before the 16.1 gate checklist; the real key is `{base64url(tenantId)}` | patch |
| B8 | The handoff rollback text is incomplete | low | It omits roll-forward, tenants created during rollback, and EventStore's existing `MessageId` enforcement | patch |
| B6 | The epoch sentinel is not disclosed to consumers | low | Only non-current freshness carries it. `AllowsPersistence` admits only `Current`/`Stale`, so it is never persisted, but the sync seam exposes it | patch: handoff |
| V6 | The REST list now returns 503 for a missing or contradictory index | low | `ListToHttpResult` maps `Unavailable` to 503; the handoff is silent | patch: handoff |
| B1/E6 | The `ReadState` default `Ready` fails open | low | `ConversationProjectionReadStore` is the only production snapshot builder and always sets it; `ConversationQueryHandler` is the only consumer | reject |
| B2/E12 | The duplicate tie-break uses clock-derived persisted fields | low | It applies only to corrupt same-position duplicates, is deterministic over persisted rows, and never reads the clock | reject |
| B3 | Accepted proof-v2 now differs from production | low | Revision-bound historical evidence; the spec forbids rewriting it; the handoff documents the new behavior | reject |
| B4 | Status and verification are unrecorded | false | Sprint sync and the verification summary are step-05 outputs; the fix would edit the spec | reject |
| B5 | The Design Notes deviation is not logged | low | `ProjectionFreshnessV1:122` forces the sentinel; the deviation is in Implementation Notes; the fix would edit the spec | reject |
| B11 | No diagnostics for the new states | low | The spec requires none; the fix adds logging members | reject |
| B12/E2 | An unknown schema version retries forever or throws untyped | low | Only a future version 3 writer produces it | reject |
| B13 | Legacy generations re-persist once | low | An idempotent convergence write; a version 1 index becomes `Rebuilding` anyway | reject |
| B14 | Projection cost and wire change | low | The gated read paths are unchanged; the persisted fields are spec-mandated | reject |
| V5 | The writer repairs missing version 2 watermarks | low | Only a corrupt record has none; the fix adds branches | reject |
| E1 | A position-only slice with a blank tenant throws | false | `DomainProjectionDispatcher.ValidateRequest:1053` rejects a blank tenant or aggregate first | reject |
| E3 | An index erased during reconcile becomes `Rebuilding` | low | A race; before this change it also wrote an empty index | reject |
| E4 | Version 1 with lifecycle fields is repaired | low | No writer emits that shape | reject |
| E5 | Foreign-tenant watermarks cause a permanent gap | low | Corruption only | reject |
| E7 | A clock before 1970 throws | low | Unreachable clock | reject |
| E8 | `IsValidReplayTime` year bounds | false | Mirrors `ConversationEventMetadata.ValidateTimestamp`; a future anchor maps to contradictory per the matrix | reject |
| E9 | An unknown type with odd metadata maps to contradictory | low | No other vocabulary reaches the conversation stream | reject |
| E10 | A null `MessageId` never projects | false | A frozen matrix row; `ProjectionEventWireBuilder` copies the required envelope `MessageId` | reject |
| E15 | A set behind an `IEnumerable` member is undetected | false | No projection contract member has such a type | reject |

### Review Findings

Review pass 2 (2026-11-04) covered `61711c3a..eea3a881` with four reviewers: blind hunter, edge-case hunter, verification gap, and acceptance auditor.

- [x] [Review][Defer] A redelivered `TenantCreated` after index loss recreates `Ready(empty)` [src/Hexalith.Conversations.Server/Projections/ConversationTenantIndexInitializer.cs:58] — deferred: Jerome chose option 3 on 2026-11-04. The guard belongs to the tenant shared-rebuild follow-up (decision 2). A Tenants-checkpoint guard would break the frozen "Tenants state deciding emptiness" rule and stop a late `TenantCreated` from creating a missing index after E13 gap recovery. A Dapr marker store would narrow the hole host-wide without closing it. The handoff claim is corrected now.
  - The create-only initializer cannot tell a first `TenantCreated` from a replayed one.
  - The host uses EventStore's default `InMemoryEventStoreDomainEventMarkerStore`: no Conversations or Tenants source calls `AddDaprEventStoreDomainEventMarkerStore`. After a restart, a redelivered or replayed `TenantCreated` therefore runs the handler again.
  - If the index key is gone at that point (erased, evicted, or lost in a restore), the empty-ETag write succeeds. It creates v2 `Ready(empty)` for a tenant that may still have conversation detail keys, so the list reads `Current` and empty. Later live deliveries merge into `Ready` with partial coverage. This brings back the D3 false-current page.
  - It also contradicts the handoff owner action, which says a lost index "stays that way until the follow-up rebuild exists".
  - Untested: `ErasedIndexShouldListAsUnavailable` stops before any redelivery, and the unit redelivery test returns `Duplicate` before reaching the handler.
  - Sources: blind hunter, edge-case hunter. Verdict: medium.
- [x] [Review][Patch] Correct the handoff's lost-index claim: a lost index reads `Unavailable` only until `TenantCreated` is delivered again (decision resolution, applied 2026-11-04). [module/platform-handoff.md:532]
- [x] [Review][Patch] The REST list status for each lifecycle state is never checked over HTTP. Add `ConversationReadApiTest` list cases: missing index gives 503 `Unavailable`/`unavailable`; contradictory gives 503 `metadata_contradictory`; `Rebuilding` gives 200 with rows. The fake `ListAsync` always returns a `Ready` snapshot. [tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs:948]
- [x] [Review][Patch] A page-verification bulk-provider fault maps to `metadata_contradictory`, where the frozen matrix says "provider failure → `Unavailable`/`unavailable`". In `ValidatePageAsync`, only `BulkReadAsync` throws, when the bulk store returns a wrong-sized, duplicated, or unrequested key set. [src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:314] Applied: the page-verification catch now falls through to `unavailable`, and `PageValidationConsistencyFailureShouldReturnMetadataContradictory` became `PageValidationBulkProviderFaultShouldReturnUnavailable`. This supersedes the Implementation Notes line saying both consistency tests expect `metadata_contradictory`; only the list-read test still does.
- [x] [Review][Patch] The initializer redelivery test never reaches the initializer. The same-`MessageId` redelivery returns `Duplicate` from the in-memory marker, so `second.ETag.ShouldBe(first.ETag)` holds trivially. Assert `Duplicate`, and redeliver with a fresh `MessageId` so the handler runs against the existing index. [tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantIndexInitializerTest.cs:75]
- [x] [Review][Patch] The live first-conversation test can read `Stale`. Its `ConversationCreated.OccurredAt` is the static `At`, captured when the type initializes, and Story 16.1 tests trigger that through `TenantIndexKey`. If more than 5 minutes pass before the conversation is projected, the default 5-minute stale threshold fails the `Current` assertion. Capture the time per replica. [tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_2TenantIndexLiveTests.cs:48]
- [x] [Review][Patch] The AC1 determinism test is narrow. It replays only a created event and one rejection, so the participant, attachment, message, redaction, and sensitivity anchor paths never run under two clocks. Its bytes assertion compares `BuildReplayState` with itself. Widen the history and drop the tautology. [tests/Hexalith.Conversations.Server.Tests/Projections/ConversationReplayDeterminismTest.cs:284]
- [x] [Review][Patch] The AC2 write ledger skips the `Forbidden` row. `QueryPathsShouldNeverWriteInAnyState` covers list, detail, and advisory in nine states, but `Forbidden` is ledger-checked for list only. [tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTenantIndexReadMatrixTest.cs:114]
- [x] [Review][Patch] The deferred-work entry for Story 16.2's failure response is still open, although 16.2 resolved it with the `NoReplayAnchor` envelope. Append a superseding resolution note, following the ledger convention. [_bmad-output/implementation-artifacts/deferred-work.md:1204]

Rejected (pass 2):

- **The CI exclusion leaves a deterministic evidence failure** (blind hunter, acceptance auditor). Low. A `ci.yml` exclusion is the owner-policy way to retire freeze checks (2026-10-07 and 2026-10-23), and `AFailingProofResultMustBlockStoryCompletion` is a precedent. The exclusion is disclosed in the Implementation Notes. Going further would change the evidence-validation class, which the notes keep unchanged.
- **Spec `done` disagrees with sprint `review`, and `review_loop_iteration` is 0** (blind hunter, acceptance auditor, edge-case hunter). The fix edits the spec under review, and this workflow's status sync sets both.
- **The pass-1 rejection rationale is contradicted, and the Spec Change Log is empty** (blind hunter). The fix edits the spec.
- **`SafeNextAction` promises a repair that never happens** (blind hunter). Low. "Rebuilding" is the existing wording for a rebuilding state. A gap clears on that conversation's next delivery. The handoff documents that pre-16.2 tenants wait for the follow-up rebuild.
- **The handoff has no owner action for the 200 → 503 change** (blind hunter). Low. The pre-16.2 paragraph and the REST status table already disclose it, and decision 2 accepted it.
- **The buyer demo's find step fails for legacy tenants** (blind hunter). Low. Reporting such a tenant as non-current is truthful under decision 2.
- **Missing `metadata` maps to `rebuilding` but missing `eventId` maps to contradictory** (blind hunter, edge-case hunter). Low. It needs a corrupt contract payload, and only on the sync seam. Both outcomes are non-current and fail closed; the async path rejects either way. The fix adds a branch.
- **The Results cannot be traced to a run** (blind hunter). The fix edits the spec. Routine-path proof is native xUnit only (decision 1).
- **The epoch sentinel was chosen without sign-off** (acceptance auditor). Low, and the fix edits the spec. With `ProjectionFreshnessV1` unchanged, it is the only value that avoids both a clock fallback and a clamp. The frozen row (unclamped generation time, null lag) holds, and it is disclosed in the Implementation Notes and the handoff (pass-1 B5 and B6).
- **The initializer task does not close the out-of-order race** (acceptance auditor). Already triaged as pass-1 E13: the claims were corrected and the race deferred in `deferred-work.md`. The remaining "closes the race" wording is in the Design Notes, so fixing it edits the spec.
- **The writer repairs a v2 record that has no watermarks** (acceptance auditor). Low. Only corruption produces that shape, because `Compose` always writes watermarks. The fix adds a branch (pass-1 V5).
- **`ReadState` defaults to `Ready` on the public read-store seam** (acceptance auditor). Low. No alternative `IConversationProjectionReadStore` exists, and a custom store already had D3 semantics. A fail-closed default would change the public `Empty` and every fake (pass-1 B1 and E6).
- **The parameterless overload throws without an `IReadModelStore`** (edge-case hunter). Low. No composition lacks a store: `Program.cs` uses the configured overload, and the tests register one. The fix adds a guard.
- **A `TenantCreated` with a null `Domain` skips the initializer on the in-memory Tenants path, and the comment claims otherwise** (edge-case hunter). Low. EventStore publishers set `Domain`, and the in-memory overload is not the production path. The outcome fails closed (`Unavailable`), and the fix adds a branch.

## Design Notes

Replay state is the materialized detail and summary with observation freshness removed. It keeps `LastAppliedEventPosition` and `ReplayAnchorAt`. `ReplayStateHash` is the lowercase-hex SHA-256 of `ReadModelBatchCanonicalJson.Serialize(state)`.

On a contradiction, the public freshness keeps the real `ProjectionGeneratedAt`, with `LagDuration = null` and `Unavailable`/`metadata_contradictory`. The contract only rejects a negative lag, so no clamp is needed.

The `TenantCreated` initializer runs before the Tenants handler, because the processor dispatches sequentially. A tenant becomes `Healthy` only after its index is `Ready`, which closes the race in which a conversation is created first.

## Verification

**Commands:**
- Prepare the source-mapped build (`dotnet tool run hexalith-module -- prepare-build --mode source`). Then build `Server.Tests`, `IntegrationTests`, `Contracts.Tests`, `Conformance.Tests` and `Client.Tests` in Debug, and run each assembly directly with `-trx`. Expected: pass, nonzero counts, 0 skipped.
- `python3 scripts/check-root-submodules.py --repository .` and `git diff --check`. Expected: pass.

**Results (2026-11-03, after review pass 1):** All five lanes, plus both conformance tiers, ran from directly invoked Debug assemblies in source-mapped mode (mapping hash `84b68a30…`), each with a TRX file. Every lane passed with zero failed and zero skipped:

| Lane | Passed |
| --- | --- |
| Server | 1671 |
| Integration (live Dapr and Redis) | 39 |
| Contracts | 648 |
| `PublicContractShapeSnapshotGenerationTest` (portable assembly) | 6 |
| `PublicClientApiSnapshotTest` | 1 |
| Portable conformance, with CI's exclusions | 296 |
| Internal conformance, with CI's exclusions | 90 |

- The snapshot baselines under `docs/` are unchanged.
- `python3 scripts/check-root-submodules.py --repository .` passed, `git diff --check` is clean, and no gitlink moved from `61711c3a`.
- The evidence-boundary gate is **not-applicable**: this spec requires no evidence-boundary verification, so the routine current-change checks apply.
- The v9 story completion gate (`_bmad-output/planning-artifacts/v9/story-contracts/16.2.json`) is **waived** by 2026-11-03 decision 1 (native xUnit proof only). It was not run, and no final-record pair was generated or inserted. It is recorded as waived, never as PASS.

**Results (2026-11-04, after review pass 2):** These lanes ran from directly invoked Debug assemblies in source-mapped mode (mapping hash `84b68a30…`), each with a TRX file. Every lane passed with zero failed and zero skipped:

| Lane | Passed |
| --- | --- |
| Server | 1675 (four new cases: three over HTTP, one `Forbidden` ledger) |
| Integration (live Dapr and Redis) | 39 |
| Portable conformance, with CI's exclusions | 296 |
| Internal conformance, with CI's exclusions | 90 |

- The Contracts, Client and public snapshot lanes were not rerun: pass 2 changed no contract, client or public surface.
- `python3 scripts/check-root-submodules.py --repository .` passed, `git diff --check` is clean, and no gitlink moved.
