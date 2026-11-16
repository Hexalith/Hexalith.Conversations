# ADR 0005: Conversation Derived-Key Lifecycle

- Status: Approval-bound. This file is the decision text. It takes effect only
  when a named owner approval is recorded in
  `docs/release-evidence/derived-key-lifecycle-decision-v1.json` and bound to
  that bundle's signable-payload digest. Any edit to this file after approval
  voids the approval (`ADR_APPROVAL_MISSING`).
- Date: 2026-11-16
- Decision owners: Conversations projection owner / Release owner
- Related: Story 12.1 (`SC-12.1`), architecture DC-7 and DC-8, ADR 0003,
  `epic-16-retro-item-52-reconcile-duplicate-event-identity-semantics`

## Context

Epic 12 will optimize the list and open hot paths. Optimization must not
create a second write authority beside EventStore, and it must not change key
bytes or lifecycle without a decision. DC-7 ratified
`conversations-derived-keys-v1` with three key families (conversation, tenant
index, dispatch ledger) and the fixed state store `statestore`. Since then the
code has grown:

- four resolution families: metadata, selection, proof and fence;
- a configurable read-model store name,
  `EventStore:Projections:ReadModelStateStoreName`
  (`ConversationProjectionStoreOptions`). Direct legacy construction still
  defaults to the SDK default `statestore`.

No migration, backfill, production delete, rollback procedure or expiry other
than the dispatch-ledger TTL exists today. No key-grammar version exists in
code either. The index record's `LifecycleSchemaVersion = 2` versions the
record value, not the key grammar. This ADR records which behavior is current
and which is proposed, so that Story 12.3 can rely on it.

## Decision

### Grammar `conversations-derived-keys-v2`

`conversations-derived-keys-v2` is `conversations-derived-keys-v1` plus the
four as-built resolution families plus the configurable store name (legacy
default `statestore`). No key byte changes, so the migration treatment from
v1 is identity. That satisfies DC-7's successor rule. `seg(x)` is unpadded
base64url (`+` to `-`, `/` to `_`) over the UTF-8 identifier. `sha256(x)` is
the lowercase hexadecimal SHA-256 of the UTF-8 identifier. Any later grammar
change needs a versioned successor with an explicit migration or rebuild
treatment.

### Ownership and write authority

EventStore alone owns domain writes. Its streams are the only durable domain
record, and it owns ordered delivery and the stable dispatch identity. All
seven families are Conversations-owned, replaceable projection state. Each is
derived only from EventStore-delivered events.

The rule token `projection-route` names the only permitted writers:

- the canonical named asynchronous projection route,
  `ConversationAsyncProjectionHandler`: `ProjectAsync`, `ReconcileAsync` and
  the `PrepareRebuildAsync` plan that the SDK executes;
- the create-only `TenantCreated` index initializer. It projects the Tenants
  `TenantCreated` event that EventStore delivers, and only creates a missing
  `Ready(empty)` index;
- SDK batch recovery, but only to resume an interrupted batch manifest that
  the route itself created. It never authors new content.

Reads never repair. Query and read paths use only `GetAsync` and
`GetManyAsync`, and they map missing, stale, pending, advanced, malformed or
inconsistent state to trust states. Tenant authorization fails closed before
any read, write or rebuild. The dispatch-ledger key is the declared exception
to the tenant-segment rule: every ledger access validates the tenant,
conversation, dispatch identity and request fingerprint.

Out of scope:

- Tenants-owned `tenants-projection:v1:*` keys. They share the store but are
  governed by Tenants.
- Duplicate-event-identity semantics. They stay open under
  `epic-16-retro-item-52-reconcile-duplicate-event-identity-semantics`.

### Write order

The `order` token lists the live positions and the `PrepareRebuildAsync`
plan position of each family. The numbered sequence and the invariants
checked against it govern the accepted-append path of `ProjectAsync` only.
Live positions:

1. dispatch-ledger acquire: create-only, with TTL `RedeliveryWindow`;
2. resolution-proof pending claim (the outer read gate);
3. resolution-fence pending claim;
4. tenant-index pending dispatch reference;
5. conversation write (ETag CAS, newest generation wins);
6. tenant-index summary merge;
7. one coordinated SDK batch: fence, metadata and optional selection;
8. resolution-fence completion, after every family is verified durable;
9. resolution-proof completion;
10. dispatch-ledger completion.

Three writes fall outside that sequence:

- Before step 1, the route resumes any interrupted SDK batch and recovers the
  original generation from lasting proof.
- A rejected input writes only a proof position barrier, then stops.
- `ReconcileAsync` first retries the accepted-append path. If that retry
  neither completes nor fails terminally for native input, its only own write
  is one ETag-guarded tenant-index update (`TryReconcileTerminalDispatchAsync`).
  That update restores the conversation's previous dispatch reference, or
  removes the reference, when the terminal dispatch's pending reference never
  advanced the conversation key, the metadata or the indexed summary.
  `TryRestoreTerminalAsync` then only resumes interrupted SDK batches and
  reads the proof, which stays pending until a supported retry or rebuild
  completes it.

Writes are create-only, ETag CAS or newest-wins, so retries converge
idempotently. A partial write is retryable and never reported as completed.

The rebuild plan is one ETag or create-only batch. It runs proof, metadata,
conversation, tenant index, dispatch ledger, fence, then the optional native
selection. The plan writes nothing itself. It rejects state newer than the
plan (`PartialRetry`) and a reused ledger with a different identity
(`DeliveryIdentityConflict`).

### Compatibility, rebuild, backfill, deletion and expiry

- Compatibility (proposed): identity from v1. No key bytes change, so no
  migration runs.
- Rebuild (current): only a full EventStore replay through the fail-closed
  rebuild plan. An interrupted rebuild withholds reads until a supported
  retry or rebuild converges.
- Backfill (proposed): none. Readers and jobs never synthesize absent state.
- Deletion (proposed): derived state is removed only by an operator rebuild,
  meaning delete plus full replay from EventStore. No production delete path
  exists or is added. The known `TenantCreated` redelivery limitation for a
  lost index stays deferred with the tenant shared-rebuild follow-up.
  Governance-driven derived-key deletion and redaction stay open under the
  proposed index topics "Retention, deletion, tombstoning, legal hold,
  export, and derived-index lifecycle" and "Redaction replay". Either one
  needs a superseding decision with a versioned grammar successor.
- Expiry: only the dispatch ledger expires (current), after
  `RedeliveryWindow`. Every other family never expires (proposed). The proof
  outlives the ledger, so ledger expiry cannot reopen or rebind a generation.

### Rollback

- Story rollback removes only the Story 12.1 additions:
  - this ADR and its row in `docs/adrs/index.md`;
  - `_bmad/scripts/generate_derived_key_decision.py` and
    `_bmad/scripts/verify_derived_key_decision.py`;
  - the decision schema, JSON and Markdown under `docs/release-evidence/`;
  - `DerivedKeyLifecycleAdrValidationTest.cs` and
    `_bmad/scripts/tests/test_derived_key_decision.py`;
  - the two Story 12.1 lines in `.gitattributes`;
  - the `artifacts/v9/12.1/` results;
  - the Story 12.1 entries in the spec, `sprint-status.yaml` and
    `deferred-work.md`.

  Story 6.2 and SM-C2 baseline v1 stay immutable.
- Runtime rollback (proposed) redeploys the prior binaries, then rebuilds
  from EventStore. It never reverse-writes derived state and never repeats
  external side effects.

### Lifecycle table

Each row states one field of one family. The `Rule` cell is the
machine-checked token. `Status` is `current` (implemented and cited) or
`proposed` (decided here, absent from code today). Evidence IDs refer to the
evidence table below.

<!-- derived-key-lifecycle:table v1 -->
| Family | Field | Status | Rule | Statement | Evidence |
| --- | --- | --- | --- | --- | --- |
| `conversation` | `owner` | current | `eventstore-derived` | Replaceable summary and detail state materialized from the conversation's EventStore stream; the stream stays the only durable domain record. | `E01`, `E19` |
| `conversation` | `key` | current | `projection:conversations:<seg(tenantId)>:<seg(conversationId)>` | Ratified by v1; tenant and conversation segments isolate keys by construction. | `E20` |
| `conversation` | `writer` | current | `projection-route` | Written only by the route's `PersistGenerationAsync` step and by the rebuild plan, under ETag CAS where the newest generation wins. | `E01`, `E02`, `E03` |
| `conversation` | `readRepair` | current | `false` | Read paths only get the key and map missing, stale or inconsistent state to trust states; they never write it. | `E18`, `E12` |
| `conversation` | `order` | current | `live=5:write;rebuild=3` | Written after the proof claim and the index pending reference, before the summary merge; third operation of the rebuild plan. | `E01`, `E02`, `E03` |
| `conversation` | `compatibility` | proposed | `identity` | v1 key bytes are unchanged in v2, so no migration runs. | none |
| `conversation` | `rebuild` | current | `full-replay-fail-closed` | Rebuilt only from a full EventStore replay through the rebuild plan; a newer persisted generation rejects the plan. | `E03`, `E19`, `E21` |
| `conversation` | `backfill` | proposed | `none` | No backfill; absent state is produced only by live delivery or full replay. | none |
| `conversation` | `deletion` | proposed | `operator-rebuild-only` | Removed only by an operator rebuild: delete the derived keys, then replay from EventStore. | none |
| `conversation` | `expiry` | proposed | `none` | Never expires; an operator rebuild is the only replacement. | none |
| `conversation` | `rollback` | proposed | `redeploy-then-rebuild` | Redeploy prior binaries, then rebuild from EventStore; never reverse-write and never repeat side effects. | none |
| `tenant-index` | `owner` | current | `eventstore-derived` | Replaceable tenant-wide summary index derived from EventStore conversation deliveries and the delivered Tenants `TenantCreated` event. | `E01`, `E17` |
| `tenant-index` | `key` | current | `projection:conversations-index:<seg(tenantId)>` | Ratified by v1; one tenant-wide key per tenant, never shared across tenants. | `E20` |
| `tenant-index` | `writer` | current | `projection-route` | Merged by the route and replaced by its rebuild plan; the create-only `TenantCreated` initializer only creates a missing `Ready(empty)` index. | `E01`, `E17`, `E22` |
| `tenant-index` | `readRepair` | current | `false` | List reads never create, merge or repair the index; missing or version 1 state reads as unavailable or rebuilding. | `E18`, `E16` |
| `tenant-index` | `order` | current | `live=4:pending,6:merge;rebuild=4` | The pending dispatch reference precedes the conversation write and the summary merge follows it; fourth operation of the rebuild plan. | `E01`, `E02` |
| `tenant-index` | `compatibility` | proposed | `identity` | Key bytes are unchanged; the record keeps its own `LifecycleSchemaVersion = 2`, and a version 1 record reads as rebuilding. | `E16` |
| `tenant-index` | `rebuild` | current | `full-replay-fail-closed` | The rebuild plan replaces the whole value under its ETag, keeps verified siblings and never lowers a watermark. | `E03`, `E19` |
| `tenant-index` | `backfill` | proposed | `none` | No backfill; a missing index is never synthesized by a reader. | none |
| `tenant-index` | `deletion` | proposed | `operator-rebuild-only` | Removed only by an operator rebuild: delete the derived keys, then replay from EventStore. | none |
| `tenant-index` | `expiry` | proposed | `none` | Never expires; an operator rebuild is the only replacement. | none |
| `tenant-index` | `rollback` | proposed | `redeploy-then-rebuild` | Redeploy prior binaries, then rebuild from EventStore; never reverse-write and never repeat side effects. | none |
| `dispatch-ledger` | `owner` | current | `eventstore-derived` | Conversations-owned delivery ledger for the EventStore-owned stable dispatch identity; it records projection progress, never domain state. | `E01`, `E07` |
| `dispatch-ledger` | `key` | current | `projection:conversations-dispatch:<sha256(dispatchId)>` | Ratified by v1 as the tenant-segment exception: fixed-length, non-disclosing and validated against tenant, conversation, dispatch and fingerprint on every access. | `E20`, `E04`, `E05` |
| `dispatch-ledger` | `writer` | current | `projection-route` | Acquired create-only and completed under ETag by the route; the rebuild plan writes its completed ledger create-only or by ETag. | `E01`, `E03`, `E05` |
| `dispatch-ledger` | `readRepair` | current | `false` | Readers verify dispatch references against ledgers they only get; they never write a ledger. | `E18` |
| `dispatch-ledger` | `order` | current | `live=1:acquire,10:complete;rebuild=5` | Acquired before any other family is written and completed last, after every family is verified durable; fifth operation of the rebuild plan. | `E01`, `E02` |
| `dispatch-ledger` | `compatibility` | proposed | `identity` | v1 key bytes are unchanged in v2, so no migration runs. | none |
| `dispatch-ledger` | `rebuild` | current | `full-replay-fail-closed` | A reused ledger with a different identity rejects the rebuild; a completed ledger whose keys are missing re-persists instead of claiming completion. | `E04`, `E03` |
| `dispatch-ledger` | `backfill` | proposed | `none` | No backfill; a ledger exists only for a delivered dispatch. | none |
| `dispatch-ledger` | `deletion` | proposed | `operator-rebuild-only` | Apart from its TTL expiry, removed only by an operator rebuild. | none |
| `dispatch-ledger` | `expiry` | current | `redelivery-window-ttl` | Expires after `RedeliveryWindow`; the proof outlives it, so a later redelivery recovers the original generation and rejects identity reuse. | `E07`, `E14` |
| `dispatch-ledger` | `rollback` | proposed | `redeploy-then-rebuild` | Redeploy prior binaries, then rebuild from EventStore; never reverse-write and never repeat side effects. | none |
| `resolution-metadata` | `owner` | current | `eventstore-derived` | Bounded resolution metadata derived from the same delivery; it carries no domain authority. | `E09` |
| `resolution-metadata` | `key` | current | `projection:conversations-resolution:<sha256(tenantId)>:<sha256(conversationId)>` | As built and ratified by v2; SHA-256 tokens keep tenant and conversation isolation with bounded, non-disclosing segments. | `E20` |
| `resolution-metadata` | `writer` | current | `projection-route` | Written only in the route's coordinated SDK batch or by the rebuild plan; SDK batch recovery resumes only the route's own interrupted manifest. | `E09`, `E08`, `E15` |
| `resolution-metadata` | `readRepair` | current | `false` | Queries read metadata only behind a completed proof and never write it. | `E10`, `E12` |
| `resolution-metadata` | `order` | current | `live=7:batch;rebuild=2` | Written in the fence batch after the conversation and index writes; second operation of the rebuild plan. | `E09`, `E08` |
| `resolution-metadata` | `compatibility` | proposed | `identity` | As-built key bytes are unchanged by v2, so no migration runs. | none |
| `resolution-metadata` | `rebuild` | current | `full-replay-fail-closed` | The rebuild plan rejects newer metadata; an interrupted rebuild withholds reads and a restart recovers the original identity. | `E08`, `E13` |
| `resolution-metadata` | `backfill` | proposed | `none` | No backfill; metadata exists only for a delivered generation. | none |
| `resolution-metadata` | `deletion` | proposed | `operator-rebuild-only` | Removed only by an operator rebuild: delete the derived keys, then replay from EventStore. | none |
| `resolution-metadata` | `expiry` | proposed | `none` | Never expires; an operator rebuild is the only replacement. | none |
| `resolution-metadata` | `rollback` | proposed | `redeploy-then-rebuild` | Redeploy prior binaries, then rebuild from EventStore; never reverse-write and never repeat side effects. | none |
| `resolution-selection` | `owner` | current | `eventstore-derived` | Bounded native selected-generation record derived from the delivery; it holds no detail or rich manifest. | `E11` |
| `resolution-selection` | `key` | current | `projection:conversations-resolution-selection:<sha256(tenantId)>:<sha256(conversationId)>` | As built and ratified by v2; SHA-256 tokens keep tenant and conversation isolation. | `E20` |
| `resolution-selection` | `writer` | current | `projection-route` | Written only as an optional participant of the route's metadata batch or the rebuild plan, under its own ETag. | `E11`, `E15` |
| `resolution-selection` | `readRepair` | current | `false` | Reads compare the selection with the proof and never write it; a tampered selection refuses the proof. | `E23`, `E12` |
| `resolution-selection` | `order` | current | `live=7:batch;rebuild=7` | Joins the fence and metadata batch when a native selection exists; last, optional operation of the rebuild plan. | `E11` |
| `resolution-selection` | `compatibility` | proposed | `identity` | As-built key bytes are unchanged by v2, so no migration runs. | none |
| `resolution-selection` | `rebuild` | current | `full-replay-fail-closed` | Rebuilt only from replay when the generation carries native admission; an interrupted rebuild refuses reads. | `E11`, `E13` |
| `resolution-selection` | `backfill` | proposed | `none` | No backfill; a selection exists only for an admitted native generation. | none |
| `resolution-selection` | `deletion` | proposed | `operator-rebuild-only` | Removed only by an operator rebuild: delete the derived keys, then replay from EventStore. | none |
| `resolution-selection` | `expiry` | proposed | `none` | Never expires; an operator rebuild is the only replacement. | none |
| `resolution-selection` | `rollback` | proposed | `redeploy-then-rebuild` | Redeploy prior binaries, then rebuild from EventStore; never reverse-write and never repeat side effects. | none |
| `resolution-proof` | `owner` | current | `eventstore-derived` | Permanent, content-free generation proof derived from deliveries; it is the outer read gate. | `E09` |
| `resolution-proof` | `key` | current | `projection:conversations-resolution-proof:<sha256(tenantId)>:<sha256(conversationId)>` | As built and ratified by v2; SHA-256 tokens keep tenant and conversation isolation. | `E20` |
| `resolution-proof` | `writer` | current | `projection-route` | Claimed, completed or barred only by the route, where a rejected input writes a position barrier; the rebuild plan installs it first. | `E09`, `E06`, `E08` |
| `resolution-proof` | `readRepair` | current | `false` | A pending, barred or changed proof makes the read unavailable; reads never reopen or complete it. | `E10`, `E12` |
| `resolution-proof` | `order` | current | `live=2:pending,9:complete;rebuild=1` | Claimed right after the ledger acquire and completed only after the fence completes; first operation of the rebuild plan. | `E01`, `E08`, `E06` |
| `resolution-proof` | `compatibility` | proposed | `identity` | As-built key bytes are unchanged by v2, so no migration runs. | none |
| `resolution-proof` | `rebuild` | current | `full-replay-fail-closed` | The rebuild plan installs the replayed generation as the current proof and rejects a newer barrier or proof. | `E08`, `E13` |
| `resolution-proof` | `backfill` | proposed | `none` | No backfill; proof exists only for a claimed generation. | none |
| `resolution-proof` | `deletion` | proposed | `operator-rebuild-only` | Removed only by an operator rebuild: delete the derived keys, then replay from EventStore. | none |
| `resolution-proof` | `expiry` | proposed | `none` | Never expires and outlives the dispatch ledger, so ledger expiry cannot reopen or rebind a generation. | `E07` |
| `resolution-proof` | `rollback` | proposed | `redeploy-then-rebuild` | Redeploy prior binaries, then rebuild from EventStore; never reverse-write and never repeat side effects. | none |
| `resolution-fence` | `owner` | current | `eventstore-derived` | Internal content-free batch fence derived from deliveries; queries never retrieve it. | `E09` |
| `resolution-fence` | `key` | current | `projection:conversations-resolution-fence:<sha256(tenantId)>:<sha256(conversationId)>` | As built and ratified by v2; SHA-256 tokens keep tenant and conversation isolation. | `E20` |
| `resolution-fence` | `writer` | current | `projection-route` | Claimed, batched and completed only by the route or written by the rebuild plan; SDK batch recovery resumes only the route's own interrupted batch. | `E09`, `E08`, `E15` |
| `resolution-fence` | `readRepair` | current | `false` | Queries never read or write the fence or its retained manifest. | `E10` |
| `resolution-fence` | `order` | current | `live=3:pending,7:batch,8:complete;rebuild=6` | Claimed after the proof, carried in the metadata batch and completed before the proof; sixth operation of the rebuild plan. | `E09`, `E08` |
| `resolution-fence` | `compatibility` | proposed | `identity` | As-built key bytes are unchanged by v2, so no migration runs. | none |
| `resolution-fence` | `rebuild` | current | `full-replay-fail-closed` | The rebuild plan writes the replayed proof into the fence and rejects a newer fence generation. | `E08`, `E13` |
| `resolution-fence` | `backfill` | proposed | `none` | No backfill; a fence exists only for a claimed generation. | none |
| `resolution-fence` | `deletion` | proposed | `operator-rebuild-only` | Removed only by an operator rebuild: delete the derived keys, then replay from EventStore. | none |
| `resolution-fence` | `expiry` | proposed | `none` | Never expires; an operator rebuild is the only replacement. | none |
| `resolution-fence` | `rollback` | proposed | `redeploy-then-rebuild` | Redeploy prior binaries, then rebuild from EventStore; never reverse-write and never repeat side effects. | none |

### Evidence

<!-- derived-key-lifecycle:evidence v1 -->
| ID | Test | File |
| --- | --- | --- |
| `E01` | `ConversationAsyncProjectionHandlerTest.AcceptedAppendShouldCompleteOnlyAfterBothExactKeysAreDurable` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` |
| `E02` | `ConversationAsyncProjectionHandlerTest.SecondWriteFailureShouldBeRetryableAndRetryShouldConverge` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` |
| `E03` | `ConversationAsyncProjectionHandlerTest.FullReplayPlanShouldContainExactGenerationAndLedgerKeysWithoutMutatingLiveState` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` |
| `E04` | `ConversationAsyncProjectionHandlerTest.RebuildShouldRejectAnExistingLedgerWithDifferentIdentity` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` |
| `E05` | `ConversationAsyncProjectionHandlerTest.CompletionShouldRejectAReplacedLedgerIdentityWithoutCompletingIt` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` |
| `E06` | `ConversationAsyncProjectionHandlerTest.CrossTenantEventShouldFailWithOnlyAnAcceptedRoutePositionBarrier` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` |
| `E07` | `ConversationResolutionMetadataProjectionTest.LedgerExpiryAndRestartRecoverOriginalGenerationAndRejectIdentityReuse` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs` |
| `E08` | `ConversationResolutionMetadataProjectionTest.RebuildAtomicallyPromotesMetadataProofAndLegacyGenerationWithExpectedEtags` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs` |
| `E09` | `ConversationResolutionMetadataProjectionTest.ProductionDispatchPersistsIsolatedSubsetAndPermanentContentFreeProof` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs` |
| `E10` | `ConversationResolutionMetadataProjectionTest.ReaderRacingACompletedGateCannotFetchRichRecoveryManifestThroughMetadata` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs` |
| `E11` | `ConversationResolutionNativeProvenanceTest.OriginalVerifiedProofIsCommittedWithSubsetGenerationAndSelectionAndSurvivesRetry` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionNativeProvenanceTest.cs` |
| `E12` | `ConversationResolutionNativeProvenanceTest.UntrustedWireProofDoesNotCertifyLegacyGenerationAndReadsCannotRepairIt` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionNativeProvenanceTest.cs` |
| `E13` | `ConversationResolutionRebuildCompletionTest.InterruptedActualSdkRebuildRefusesWithoutRichReadsAndRestartRecoversOriginalIdentity` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionRebuildCompletionTest.cs` |
| `E14` | `ConversationProjectionReadModelPersistenceTest.MatchingProjectionShouldRemainReadableAfterDispatchLedgerExpires` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs` |
| `E15` | `ConversationProjectionConfigurationTest.PendingRecoveryUsesTheConfiguredStore` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionConfigurationTest.cs` |
| `E16` | `ConversationTenantIndexLifecycleTest.LiveDeliveryToAVersionOneIndexShouldProduceRebuildingWithSeededWatermarks` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationTenantIndexLifecycleTest.cs` |
| `E17` | `ConversationTenantIndexInitializerTest.TenantCreatedShouldInitializeTheIndexAndRedeliveryShouldLeaveItUnchanged` | `tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantIndexInitializerTest.cs` |
| `E18` | `ConversationTenantIndexReadMatrixTest.QueryPathsShouldNeverWriteInAnyState` | `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTenantIndexReadMatrixTest.cs` |
| `E19` | `ConversationProjectionReadStorePopulationLiveTests.DerivedStateDeletionAndFullReplayShouldRestoreEquivalentKeysAndQueries` | `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs` |
| `E20` | `DerivedKeyLifecycleAdrValidationTest.EventStoreShouldRemainOnlyWriteAuthority` | `tests/Hexalith.Conversations.Conformance.Tests/DerivedKeyLifecycleAdrValidationTest.cs` |
| `E21` | `ConversationReplayDeterminismTest.TwoCleanRebuildsUnderDifferentClocksShouldProduceByteIdenticalReplayState` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationReplayDeterminismTest.cs` |
| `E22` | `ConversationAsyncProjectionHandlerTest.StableDuplicateShouldConvergeWithoutDuplicatingTheTenantIndex` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` |
| `E23` | `ConversationResolutionNativeEnrollmentTest.ReconciliationPreservesOriginalGenerationAndSelectionTamperingRefusesProof` | `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionNativeEnrollmentTest.cs` |

## Consequences

- Story 12.3 may optimize reads and writes only inside this lifecycle. A new
  family, a key byte change, another writer, a read-side write, a new expiry
  or a reordered write needs a new decision and a versioned grammar
  successor.
- Proposed rows are commitments, not implemented behavior. Implementing one
  (for example an operator rebuild command) must cite this ADR and add
  current evidence.
- Rollback never needs a reverse migration, because derived keys are always
  rebuilt from EventStore.

## Alternatives Considered

- Keep v1 and treat the resolution families as unversioned. Rejected: DC-7
  requires a versioned successor for any grammar change, and Epic 12 needs
  every family decided.
- Add read-side backfill or repair to hide missing state. Rejected: it creates
  a second write authority and breaks fail-closed trust states.
- TTL for derived state other than the ledger. Rejected: proof must outlive
  the ledger, and expiry would make reads silently lose generations.

## Verification

- `_bmad/scripts/generate_derived_key_decision.py` parses the lifecycle table
  into a canonical signable payload. It binds the Story 12.1 contract, its
  planning candidate, the recomputed entry inventory, the Story 6.2 record and
  candidate, and the SM-C2 baseline v1 files. It fails with
  `DERIVED_KEY_ADR_INCOMPLETE`, `EVENTSTORE_AUTHORITY_VIOLATED`,
  `DERIVED_KEY_TRANSITION_UNSAFE` or `ROLLBACK_PLAN_INVALID`. Missing Git
  history is `BLOCKED`.
- `_bmad/scripts/verify_derived_key_decision.py` recomputes the payload, its
  digests, the Markdown and the approval binding (`ADR_APPROVAL_MISSING`).
- `DerivedKeyLifecycleAdrValidationTest.EventStoreShouldRemainOnlyWriteAuthority`
  checks the decided key templates against the runtime key builders, and
  checks that every family names the projection route as its sole writer,
  with read repair `false`.
