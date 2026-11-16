# Derived-Key Lifecycle Decision v1

Schema: `hexalith.conversations.derived-key-lifecycle-decision.v1`

## Candidate

- Story: `12.1`
- Contract: `_bmad-output/planning-artifacts/v9/story-contracts/12.1.json` (`e271984c3d37654d40b6c3fb10a1e79668861ba6bc8f8682ace2cd8ec1e5f641`)
- Planning candidate: `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`
- ADR: `docs/adrs/0005-conversation-derived-key-lifecycle.md` (`2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068`)
- Entry inventory: `V9-12.1-ENTRY-v1` = `V8-6.11-AC1`, `V8-6.11-DEPENDENCY-6.2`, `V8-6.11-PROHIBITION-EVENTSTORE-ONLY-WRITE-AUTHORITY` (`b8b755088071d62e36f8e6829d90a8fef3dc8eb54e8f521297ef22b3bfdebf0b`)
- Rule: The decision candidate is the Story 12.1 contract and its planning candidate, never HEAD. The contract digest must equal its v9 authority-bundle row. Every Story 6.2 predecessor file must be byte-identical in the working tree and at the planning candidate, and the Story 6.2 candidate must be the record's file_list_commit and an ancestor of the planning candidate. Story 16.3 is bound at its done commit: the spec blob and the sprint row recorded there must both be done. Unresolvable history is BLOCKED, never PASS.
- Predecessor `story-6.2-record` (Story 6.2): `_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md` (`1b87966f2b48d18c1f1d642e679febca26bbc591e8f270e6deb96393ea39034e`); candidate `2971ab79efcf3ef11d4fba7b9139d7cae457a3f9`
- Predecessor `sm-c2-hot-path-baseline-v1-json` (Story 6.2): `docs/release-evidence/sm-c2-hot-path-baseline-v1.json` (`4cb4e66744b26ead0a79a218aed662005f14007e8e103dd019836761569118ea`)
- Predecessor `sm-c2-hot-path-baseline-v1-md` (Story 6.2): `docs/release-evidence/sm-c2-hot-path-baseline-v1.md` (`c0a94476ad4a1ceeef73ee601e9079997077830604ff3b1300a08dc20cae8c33`)
- Predecessor `sm-c2-hot-path-baseline-v1-xunit` (Story 6.2): `docs/release-evidence/sm-c2-hot-path-baseline-v1.xunit.xml` (`06d219bb065592b235c842f2fcb6b193dccda6c67045d1e80737725b5927b773`)
- Predecessor `story-16.3-spec` (Story 16.3): `_bmad-output/implementation-artifacts/spec-16-3-diagnose-apphost-preflight-failures-and-prove-terminal-reconciliation-live.md` (`f3f00f085f82ad4639246a1b513c71cffcc4ea5112f4bacc11857be198fc2462`); candidate `8da7f2bc8997c03a6aea3d72464369496c7b9717`
- `AC-12.1-01`: **waived**. Owner decision 2026-11-16: the candidate-verifier route is waived and recorded as waived, never PASS.
- `AC-12.1-06`: **waived**. Owner decision 2026-11-16: the v2 final pair is waived and recorded as waived, never PASS; Story 12.2 binds Story 12.1 by its done spec, sprint row and commit.

## Grammar

- Ratified: `conversations-derived-keys-v2`, superseding `conversations-derived-keys-v1`; migration treatment `identity`
- v1 families: `conversation`, `tenant-index`, `dispatch-ledger`
- Added families: `resolution-metadata`, `resolution-selection`, `resolution-proof`, `resolution-fence`
- seg(x) is unpadded base64url (+ to -, / to _) over the UTF-8 identifier; sha256(x) is the lowercase hexadecimal SHA-256 of the UTF-8 identifier
- State store: configuration key `EventStore:Projections:ReadModelStateStoreName`, legacy default `statestore`, configurable true
- In-code grammar version: none
- Out of scope: Tenants-owned tenants-projection:v1:* keys share the store and are governed by Tenants.
- Out of scope: Duplicate-event-identity semantics stay open under epic-16-retro-item-52-reconcile-duplicate-event-identity-semantics.

## Key families

| Family | Origin | Key | Prefix | Tenant segment |
| --- | --- | --- | --- | --- |
| `conversation` | `conversations-derived-keys-v1` | `projection:conversations:<seg(tenantId)>:<seg(conversationId)>` | `projection:conversations:` | true |
| `tenant-index` | `conversations-derived-keys-v1` | `projection:conversations-index:<seg(tenantId)>` | `projection:conversations-index:` | true |
| `dispatch-ledger` | `conversations-derived-keys-v1` | `projection:conversations-dispatch:<sha256(dispatchId)>` | `projection:conversations-dispatch:` | false |
| `resolution-metadata` | `conversations-derived-keys-v2` | `projection:conversations-resolution:<sha256(tenantId)>:<sha256(conversationId)>` | `projection:conversations-resolution:` | true |
| `resolution-selection` | `conversations-derived-keys-v2` | `projection:conversations-resolution-selection:<sha256(tenantId)>:<sha256(conversationId)>` | `projection:conversations-resolution-selection:` | true |
| `resolution-proof` | `conversations-derived-keys-v2` | `projection:conversations-resolution-proof:<sha256(tenantId)>:<sha256(conversationId)>` | `projection:conversations-resolution-proof:` | true |
| `resolution-fence` | `conversations-derived-keys-v2` | `projection:conversations-resolution-fence:<sha256(tenantId)>:<sha256(conversationId)>` | `projection:conversations-resolution-fence:` | true |

## Write ordering

Live (`ProjectAsync`):

- Position 1: `dispatch-ledger` acquire
- Position 2: `resolution-proof` pending
- Position 3: `resolution-fence` pending
- Position 4: `tenant-index` pending
- Position 5: `conversation` write
- Position 6: `tenant-index` merge
- Position 7: `resolution-metadata` batch
- Position 7: `resolution-selection` batch
- Position 7: `resolution-fence` batch
- Position 8: `resolution-fence` complete
- Position 9: `resolution-proof` complete
- Position 10: `dispatch-ledger` complete

Rebuild plan (`PrepareRebuildAsync`):

- Position 1: `resolution-proof`
- Position 2: `resolution-metadata`
- Position 3: `conversation`
- Position 4: `tenant-index`
- Position 5: `dispatch-ledger`
- Position 6: `resolution-fence`
- Position 7: `resolution-selection`

## Lifecycle

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

## Rollback

- Story: remove only ADR 0005, its validator/generated decision, fixtures/results, and Story 12.1 record; Story 6.2 and SM-C2 v1 stay immutable.
- Runtime: Redeploy the prior binaries, then rebuild from EventStore; never reverse-write derived state and never repeat external side effects.

## Evidence tests

- `E01`: `ConversationAsyncProjectionHandlerTest.AcceptedAppendShouldCompleteOnlyAfterBothExactKeysAreDurable` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`)
- `E02`: `ConversationAsyncProjectionHandlerTest.SecondWriteFailureShouldBeRetryableAndRetryShouldConverge` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`)
- `E03`: `ConversationAsyncProjectionHandlerTest.FullReplayPlanShouldContainExactGenerationAndLedgerKeysWithoutMutatingLiveState` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`)
- `E04`: `ConversationAsyncProjectionHandlerTest.RebuildShouldRejectAnExistingLedgerWithDifferentIdentity` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`)
- `E05`: `ConversationAsyncProjectionHandlerTest.CompletionShouldRejectAReplacedLedgerIdentityWithoutCompletingIt` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`)
- `E06`: `ConversationAsyncProjectionHandlerTest.CrossTenantEventShouldFailWithOnlyAnAcceptedRoutePositionBarrier` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`)
- `E07`: `ConversationResolutionMetadataProjectionTest.LedgerExpiryAndRestartRecoverOriginalGenerationAndRejectIdentityReuse` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs`)
- `E08`: `ConversationResolutionMetadataProjectionTest.RebuildAtomicallyPromotesMetadataProofAndLegacyGenerationWithExpectedEtags` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs`)
- `E09`: `ConversationResolutionMetadataProjectionTest.ProductionDispatchPersistsIsolatedSubsetAndPermanentContentFreeProof` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs`)
- `E10`: `ConversationResolutionMetadataProjectionTest.ReaderRacingACompletedGateCannotFetchRichRecoveryManifestThroughMetadata` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataProjectionTest.cs`)
- `E11`: `ConversationResolutionNativeProvenanceTest.OriginalVerifiedProofIsCommittedWithSubsetGenerationAndSelectionAndSurvivesRetry` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionNativeProvenanceTest.cs`)
- `E12`: `ConversationResolutionNativeProvenanceTest.UntrustedWireProofDoesNotCertifyLegacyGenerationAndReadsCannotRepairIt` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionNativeProvenanceTest.cs`)
- `E13`: `ConversationResolutionRebuildCompletionTest.InterruptedActualSdkRebuildRefusesWithoutRichReadsAndRestartRecoversOriginalIdentity` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionRebuildCompletionTest.cs`)
- `E14`: `ConversationProjectionReadModelPersistenceTest.MatchingProjectionShouldRemainReadableAfterDispatchLedgerExpires` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs`)
- `E15`: `ConversationProjectionConfigurationTest.PendingRecoveryUsesTheConfiguredStore` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionConfigurationTest.cs`)
- `E16`: `ConversationTenantIndexLifecycleTest.LiveDeliveryToAVersionOneIndexShouldProduceRebuildingWithSeededWatermarks` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationTenantIndexLifecycleTest.cs`)
- `E17`: `ConversationTenantIndexInitializerTest.TenantCreatedShouldInitializeTheIndexAndRedeliveryShouldLeaveItUnchanged` (`tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantIndexInitializerTest.cs`)
- `E18`: `ConversationTenantIndexReadMatrixTest.QueryPathsShouldNeverWriteInAnyState` (`tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTenantIndexReadMatrixTest.cs`)
- `E19`: `ConversationProjectionReadStorePopulationLiveTests.DerivedStateDeletionAndFullReplayShouldRestoreEquivalentKeysAndQueries` (`tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs`)
- `E20`: `DerivedKeyLifecycleAdrValidationTest.EventStoreShouldRemainOnlyWriteAuthority` (`tests/Hexalith.Conversations.Conformance.Tests/DerivedKeyLifecycleAdrValidationTest.cs`)
- `E21`: `ConversationReplayDeterminismTest.TwoCleanRebuildsUnderDifferentClocksShouldProduceByteIdenticalReplayState` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationReplayDeterminismTest.cs`)
- `E22`: `ConversationAsyncProjectionHandlerTest.StableDuplicateShouldConvergeWithoutDuplicatingTheTenantIndex` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`)
- `E23`: `ConversationResolutionNativeEnrollmentTest.ReconciliationPreservesOriginalGenerationAndSelectionTamperingRefusesProof` (`tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionNativeEnrollmentTest.cs`)

## Approval

- Decision: approved
- Approver: Jerome Piquot
- Decided at (UTC): 2026-11-16T12:13:38Z
- Bound ADR digest: `2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068`
- Bound signable-payload digest: `13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe`

## Digests

- ADR: `2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068`
- Signable payload: `13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe`
