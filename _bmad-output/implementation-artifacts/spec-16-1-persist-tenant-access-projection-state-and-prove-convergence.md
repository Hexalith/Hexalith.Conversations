---
title: 'Story 16.1: Persist tenant-access projection state and prove convergence'
type: 'feature'
created: '2026-10-22'
status: 'done'
baseline_commit: '0f068ad765c1bd004df332f19f05df5cfdc1865f'
route: 'dispatch'
review_loop_iteration: 2
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenant authorization uses a process-local projection lost on restart. Consumer-group delivery cannot populate every replica, and missing freshness evidence currently defaults to healthy.

**Approach:** Consume an additive Tenants durable projection capability backed by EventStore mutation/recovery infrastructure. Prove restart recovery without redelivery and consistent authorization across replicas.

**Decision:** Prepare prerequisites first; inherit Hexalith.Platform infrastructure and operational policy. On 2026-10-23 the user selected Platform enrollment and successor tooling preparation for review; runtime remains held. On 2026-10-25 the user approved additive prerequisite refresh with accepted CLI 4.35.2; runtime entry remains held. On 2026-10-25 the user authorized corrected CLI qualification and successor-tooling implementation under the separate prerequisite scope; runtime entry remains held.

**2026-10-27 decision:** Prepare a separate enrollment/adoption-remediation plan. This selects prerequisite planning; runtime entry remains held.

**2026-10-27 follow-up decision:** The user selected option 1: prepare a separate tooling-qualification and source-binding-refresh plan. This authorizes planning only; runtime entry remains held.

**2026-10-28 post-tooling decision:** The user selected option 1: prepare a separate published-artifact/Platform qualification and adoption-disposition plan. This authorizes planning only; runtime remains held.

**2026-10-30 current-tuple decision:** The user selected the recommended separate 4.38.3 qualification and binding-refresh plan. This authorizes preparation; implementation requires approval of that spec. Runtime entry remains held.

**2026-11-01 entry decision:** The user waived the Story 16.1 runtime-entry ceremony (4.38.3 prerequisite, AD-5 envelope, nine owner qualifications, ACTIVE hold) and authorized implementation under the routine current-change path. Waived items are recorded as observed/waived, never PASS; the 4.38.3 spec stays untouched. EventStore and Tenants changes are committed locally on each repository's own `main` in dependency order with that repository's commitlint; nothing is pushed without an explicit ask, and the Conversations commit moves the two gitlinks. Completion proof is native xUnit only; the tooling-v3 `6/6/0/0/0/0` final pair is not required. The full spec size was kept.

**2026-11-01 review decisions:** (1) History: the enrolled host registers an EventStore gateway client with the deletion runtime's gateway/authority-transport pattern and fails startup without it; Platform grants the Conversations workload read access to tenant `system`, domain `tenants` (documented owner action); rejection events advance the checkpoint. (2) Recovery driver: the tenants subscription must retry without dead-lettering (documented Platform requirement); no sweeper. (3) Staleness: "stale" means a missing or unproven record, with no time-based check; rebuilding tenant records after a state-store restore is a deferred operational-envelope step. (4) Amend forward on the existing local commits instead of reverting.

**2026-11-01 review-pass-2 decisions:** (5) With Keycloak disabled, the AppHost injects a development-only worker authority (generated secret parameter, loopback authority) so the host starts; history reads in that mode fail and deny. The local `conversations-tenant-access` realm client gains the audience and tenant/domain mappers so Keycloak-on gap recovery works. (6) Every unsafe tenant state (missing, gapped, corrupt, malformed, stale or unavailable) returns the same public denial as an unknown tenant: non-retryable, same code and reason. The precise reason stays in logs and diagnostics. Amend forward as before.

## Boundaries & Constraints

**Always:** Enforce architecture AD-5/AD-6. EventStore owns identity, CAS, retry, replay, fenced recovery and acknowledgements; Tenants owns transitions/checkpoints. Conversations evaluates one state/health/checkpoint snapshot per invocation. Only proven healthy state authorizes. Preserve compatibility and historical evidence.

**Never:** Direct Dapr persistence, unconditional saves, process-local concurrency, read repair, synthetic health, or early acknowledgement. Exclude 16.2/16.3, deployment and destructive migration. Rollback removes adoption/configuration; retain events, evidence and accepted records.

## I/O & Edge-Case Matrix

| State/delivery | Required behavior |
| --- | --- |
| Healthy, exact-next sequence | Atomically commit transition and checkpoint |
| Identical accepted duplicate | No-op; verify older sequences against authoritative history |
| Conflicting identity/payload | Persist terminal Corrupt; deny |
| Future sequence | Persist Gapped, immutable expected sequence, monotonic recoveryThrough and fingerprints; deny |
| CAS/provider/retry exhaustion | Retryable failure, no acknowledgement; authorization unavailable |
| Fenced recovery | Validate history/evidence; restore Healthy only with AD-6 final CAS and no pending evidence |
| Missing/stale/unavailable or malformed state | Deny without disclosing tenant existence |
| Restart or second replica | Reload the same persisted decision/checkpoint without redelivery |

</frozen-after-approval>

## Code Map

Paths below are relative to each owning repository: `references/Hexalith.EventStore` at `26945134`, `references/Hexalith.Tenants` at `2d3fa6da`, and the Conversations root.

- EventStore `src/Hexalith.EventStore.Client/Subscriptions/EventStoreDomainEventProcessor.cs:102,281-343` -- the envelope carries raw `Payload` bytes, but `EventStoreDomainEventContext` exposes no payload fingerprint or event type. A handler that throws releases the MessageId marker. `DomainService/EventStoreDomainEventsEndpointExtensions.cs:63-79` maps that throw to 500 (retry), and success to 200 (acknowledged).
- EventStore `Projections/IReadModelStore.cs:79` -- `TrySaveAsync` returns `false` on an ETag conflict and throws `DaprException` on a provider failure. `ReadModelWritePolicy.cs:30` sets `DefaultMaxAttempts = 3`, counting total attempts. Reuse both. Adding interface members would break implementers, so don't.
- EventStore `Streams/IAuthoritativeEventStreamReader.cs:13` -- reads whole streams only. For ranges, use `Gateway/IEventStoreGatewayClient.ReadStreamAsync(StreamReadRequest)` with `FromSequence`/`ToSequence`. Each `StreamReadEvent` carries `Payload`, `MessageId` and `SequenceNumber`.
- EventStore `Projections/SharedProjectionEpochCoordinator.cs` -- reuse its lease/epoch, `ComputeDeliveryDigest` and delivery-conflict patterns. Do not reroute the projection-dispatch path through it.
- EventStore `src/Hexalith.EventStore.Testing/Fakes/InMemoryReadModelStore.cs:33,45` -- real ETag behaviour, with `ConcurrentWriteBeforeTrySave` to simulate racing writers.
- Tenants `src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs` -- its only implementation is `InMemoryTenantProjectionStore`. `TenantLocalState` carries `LastEvent.LastSequenceNumber`. Keep both signatures and clone semantics.
- Tenants `Handlers/TenantProjectionEventHandler.cs` -- has a process-local `SemaphoreSlim`, skips only older sequences and reapplies equal ones. Extract its 9 event applications into a pure transition.
- Tenants `Registration/TenantServiceCollectionExtensions.cs` -- registers the in-memory store only when no store is registered yet (first registration wins). Keep that default.
- Conversations `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessService.cs:51,143-187` -- feeds Commons `TenantAccessEvaluator.EvaluateAsync` from separate state and health reads. `DefaultConversationTenantProjectionSignal.cs` synthesizes Healthy.
- Conversations `ConversationTenantAccessServiceCollectionExtensions.cs` -- keep the enrolled `Tenants:*` and declarative-subscription configuration (Platform owns subscriptions).
- Conversations `src/Hexalith.Conversations.Server/Program.cs:49-53` -- the `SINGLE-REPLICA-ONLY` comment. `_bmad/scripts/epic16_execution.py:15` and `verify_epic_16_prerequisites.py:56` look for it.
- Owner tests: EventStore `tests/Hexalith.EventStore.Client.Tests/Projections/`, Tenants `tests/Hexalith.Tenants.Client.Tests/`, Conversations `tests/Hexalith.Conversations.Server.Tests/TenantAccess/`. `tests/Hexalith.Conversations.IntegrationTests/Epic16/` does not exist yet. The live Dapr harness is in `IntegrationTests/Projections/`.

## Tasks & Acceptance

**Execution (EventStore → Tenants → Conversations):**
- [x] EventStore `Subscriptions/` -- add additive `PayloadFingerprint` (SHA-256 of the exact payload bytes) and `EventType` to the context. Keep constructor and handler signatures unchanged.
- [x] EventStore `Projections/Checkpointed/` -- add a generic checkpointed projection writer and recovery worker. They follow Design Notes, reuse `IReadModelStore` and the 3-attempt policy, and use a range read for verification and replay. Durable outcomes return; retryable outcomes throw.
- [x] EventStore Client tests -- cover the AD-6 matrix with `InMemoryReadModelStore`: concurrent reverse-save, duplicates, equal-sequence mismatch, three or more interleaved future deliveries, concurrent horizon extension, retry exhaustion, lease expiry/restart, failed and successful recovery.
- [x] Tenants `Projections/`, `Handlers/`, `Registration/` -- add the pure `TenantProjectionTransition`, plus an opt-in durable snapshot store (state, health, checkpoint) on the EventStore writer. The handler routes through it when registered. Add matching Client tests.
- [x] Conversations `Server/TenantAccess/` -- read one durable snapshot per evaluation and bind both Commons adapters to it. Every non-Healthy, missing or malformed snapshot denies without disclosure. With the durable store present, nothing synthesizes Healthy. Register the store over the existing read-model state store.
- [x] Conversations `Server.Tests/TenantAccess/` and `IntegrationTests/Epic16/Story16_1EvidenceTests.cs` -- cover the I/O matrix at the consumer, plus live Dapr persisted end-state, restart without redelivery and two-replica convergence. After those proofs pass, remove `SINGLE-REPLICA-ONLY` and its two handoff mentions, and update the marker checks.

**Review loop 1 (amend forward, follow-up commits per repo):**
- [x] Tenants `Handlers/`, `Registration/` -- in durable mode, route every Tenants rejection event (`Contracts/Events/Rejections/`) through the durable store, so it advances the checkpoint without changing state. Test that a rejection followed by the next event stays Healthy without history.
- [x] Conversations `TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs` -- the enrolled path registers the EventStore gateway client, following the deletion runtime's pattern (`Agents/ConversationDeletionRuntimeHostExtensions.cs:126`, `ConversationDeletionRuntimeOptions.GatewayBaseAddress`, `AddEventStoreGatewayAuthorityTransport`). Startup fails if it is missing. Don't double-register when the deletion runtime is on. Keep the singleton store from holding a typed `HttpClient` for its whole lifetime; resolve one per use. Add a host-composition test (the same registrations as `Program.cs`) proving the history reader resolves and a rejection-induced or out-of-order gap recovers.
- [x] Conversations `TenantAccess/ConversationTenantSnapshotHealth.cs` -- a missing record denies exactly like a non-member of an existing tenant (`UnknownTenant`: non-retryable, same public code). Gapped, Corrupt and Unavailable keep their current mapping. Add a test comparing the two public decisions.
- [x] EventStore `Projections/Checkpointed/` -- these fixes:
  - throw `HistoryUnavailable` for a truncated page with no new events;
  - throw unavailable when the record disappears after the pending CAS;
  - use a per-invocation lease identity;
  - remove the defaults on persisted `Health`/`SchemaVersion` so missing fields are malformed, and set them explicitly when a record is created.
  Add tests for each.
- [x] Tenants `DurableTenantProjectionStore.KeyFor` -- hashed keys use a separator that safe ids cannot contain.
- [x] Tests -- history doubles assert the requested tenant/domain/aggregate. Add live cases with history that lags behind delivery, and with two replicas racing on the first write to the same empty key through Redis.
- [x] `_bmad/scripts/tests/conftest.py` -- retire the frozen-state `test_verify_epic_16_adoption_v2.py` suite, which the 16.1 sprint transition invalidates.
- [x] `src/Hexalith.Conversations.Server/Program.cs`, `module/platform-handoff.md` -- state only what the tests prove. Add the owner actions:
  - grant tenants-stream read access;
  - configure unbounded retry with no dead-letter for the tenants subscription;
  - rebuild tenant records after a restore;
  - follow the release train: publish EventStore, then Tenants with the EventStore bump, then bump Conversations' package versions in `Directory.Packages.props` before its CI can compile in package mode.

**Review loop 2 (amend forward, follow-up commits per repo):**
- [x] Conversations AppHost (`ConversationsAppHostTopology.cs`, `KeycloakRealms/`) -- with Keycloak disabled, inject a development-only worker authority so the host starts. With Keycloak enabled, give `conversations-tenant-access` the EventStore audience and the tenant `system` / domain `tenants` read claims in EventStore's claim format. Extend the topology tests, and run the boundary lane locally with `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true` until it passes.
- [x] Conversations `TenantAccess/ConversationTenantAccessDecision.cs` -- make every unsafe-state public denial (missing, gapped, corrupt, malformed, stale, unavailable) identical to an unknown tenant: non-retryable, same code and reason. Keep the internal reason. Add a test comparing the public shape of every state with a non-member of a healthy tenant. Update existing expectations only where this decision changes them.
- [x] `module/hexalith.module-manifest.v2.json` -- declare the new required configuration: the gateway base address, and `EventStore:Authentication:*` with the client secret as a secret. Declare the outbound gateway dependency as the schema allows. `hexalith-module validate` must pass.
- [x] Tenants `Projections/` -- a rejection or other non-creating event for a tenant with no state advances the checkpoint without materializing tenant state, so access denies as an unknown tenant. Rejection-only streams of never-created tenants must not leave durable tenant state. Add tests.
- [x] Small fixes:
  - handoff: drop the stale "does not inject" wording; add the Tenants catalog SDK allow-list step to the release train;
  - gateway registration: record the marker when the host supplies its own client; ignore keyed clients; add a positive loopback-HTTP origin test; assert on the full configuration key;
  - recovery: release the lease on every failure except lease loss; a null history payload throws `HistoryUnavailable`;
  - the diagnostics signal maps a missing record to `Unproven`;
  - add tests `ExpectedSequenceArrivingWhileGappedIsMergedAsEvidence` and a registration-level per-read gateway test.

**Acceptance Criteria:**
- Given acknowledged tenant events, when a host restarts without redelivery or a second replica reads, then both observe the same persisted state, health and checkpoint in the state store.
- Given concurrent deliveries and recovery, when authorization runs, then exactly one coherent snapshot decides, and only Healthy allows access.
- Given the change, when the owner Client, Server and Integration tests run individually in mapped Debug, then they pass with zero skips, and existing public signatures and default registrations stay compatible.

## Implementation Notes

Prior planning notes, entry facts and the complete earlier change log are preserved verbatim in [history](story-16-1-planning-history-2026-11-01-before-runtime-plan.md).

Baselines (2026-11-01): Conversations `0f068ad765c1bd004df332f19f05df5cfdc1865f`; EventStore `main` `269451340a87609a0a3c76fbc213022b01fd9cde`; Tenants `main` `2d3fa6da2312c1b5cf7f5c9461cc2d0ffc995b43`. Both submodule checkouts were clean and on `main`.

Implementation (2026-11-01):
- EventStore: the processor context now carries `EventType` and `PayloadFingerprint` (`EventStorePayloadFingerprint`, lowercase SHA-256 of the raw envelope payload). `Projections/Checkpointed/` adds `CheckpointedProjectionRecord<TState>` (state, health, checkpoint and accepted identity, immutable `Expected`, monotonic `RecoveryThrough`, sequence-ordered pending evidence, lease, fencing counter and version in one record), `CheckpointedProjectionWriter<TState>`, `CheckpointedProjectionRecoveryWorker<TState>`, and `CheckpointedProjectionHistory` (bounded `ReadStreamAsync` range reads). Writes are `IReadModelStore.TrySaveAsync` compare-and-swaps under the 3-total-attempt default; durable outcomes return and retryable ones throw `CheckpointedProjectionUnavailableException`.
- Tenants: `TenantProjectionTransition` is the pure transition; the handler keeps its local skip-older path and routes to `ITenantProjectionSnapshotStore.ApplyAsync` when the opt-in `AddHexalithTenantsDurableProjection(stateStoreName)` registers `DurableTenantProjectionStore`. `AddHexalithTenants` still defaults to the in-memory store. The durable store returns state only for a healthy record and refuses unconditional `SaveAsync`.
- Conversations: the enrolled `AddConversationTenantAccess(configuration)` also requires `EventStore:Projections:ReadModelStateStoreName` and registers the durable store over it. With a durable store, `ConversationTenantAccessSnapshot` reads one snapshot per evaluation and binds both Commons adapters to it. Missing, malformed, unavailable or untrusted snapshots map to stale health without version or watermark; Gapped maps to a gap and Corrupt to poisoning. The default signal reports persisted health instead of synthesizing Healthy. `SINGLE-REPLICA-ONLY` and its two handoff mentions are removed; `_bmad/scripts/tests/test_story_16_1_warning_removal.py` keeps them removed.
- Older-delivery verification and gap recovery use a registered `IEventStoreGatewayClient`; without one they stay retryable-unavailable and deny. Rejection events in the tenant stream have no projection handler, so they surface as gaps that recovery replays as checkpoint-only transitions.

Verification (2026-11-01, native xUnit, Debug, assemblies run directly with TRX):
- EventStore `e9eee2aa9` Client tests: 2296 total, 2292 passed, 2 failed, 2 not run. The 2 failures are `DevelopmentRouteCatalogBuilderTests` file-tool cases: `tools/routing-catalog.cs` cannot load `Hexalith.EventStore.Server` in this workspace, which this change does not touch. The 2 not-run cases are the opt-in `PendingBatchRecoveryLiveTests`; run against an isolated daprd/Redis sidecar they passed 2/2.
- Tenants `1afae9c4` Client tests (`-p:UseHexalithProjectReferences=true`): 82/82 passed, 0 skipped.
- Conversations builds need the Platform tool mapping (`dotnet tool run hexalith-module -- prepare-build --mode source`). `Server.Tests`: 1537/1537 passed, 0 skipped. `IntegrationTests`: 33/33 passed, 0 skipped, including the four `Epic16/Story16_1EvidenceTests` live Dapr cases.
- `python3 -m pytest -q _bmad/scripts/tests`: 5716 passed, 119 failed, 57 errors, 17 deselected. The new `test_story_16_1_warning_removal.py` checks pass. Every failure also fails on a clean clone of `0f068ad7` with these changes applied, except the 58 `test_verify_epic_16_adoption_v2.py` cases. Those fail only because the uncommitted `sprint-status.yaml` edit drifts a pinned source; copying that file into the clean clone reproduces all 58.
- `python3 scripts/check-root-submodules.py --repository .`: PASS with the two gitlinks staged. `git diff --check`: clean in all three repositories. `hexalith-module validate`: validated.
- Local commits, not pushed: EventStore `e9eee2aa9`, Tenants `1afae9c4`, Conversations `1a4d5db8` (moves both gitlinks). This spec, `sprint-status.yaml` and the planning-history file stay uncommitted.

Review loop 1 (2026-11-01, follow-up commits; nothing amended, rebased or pushed):
- EventStore `8449adfdf`: history throws `HistoryUnavailable` for a truncated page with no new events and for a range beyond the stream head (lagging history is never classified as missing); a scope-factory `CheckpointedProjectionHistory` constructor resolves the gateway client per read; recovery throws the new `RecordMissing` reason when the record vanished after the pending CAS; each recovery invocation leases under `WorkerId/<guid>`; persisted `Health`/`SchemaVersion` have no defaults and `CheckpointedProjectionRecord.CreateEmpty()` sets them.
- Tenants `5b3e4aa7`: durable mode registers `TenantRejectionCheckpointHandler<T>` for all 14 rejection types (checkpoint-only); registration builds the history with the scope-factory reader; hashed keys use `HashedKeyMarker` `sha256:`.
- Conversations `c8f45dd6` (moves both gitlinks): the enrolled path requires `Hexalith:Conversations:TenantAccess:GatewayBaseAddress` and the `EventStore:Authentication` worker keys and registers the SDK gateway client once (`ConversationEventStoreGatewayRegistration`, shared with the deletion runtime, which reuses it for the same origin and refuses a different one; a host-supplied client is kept). A missing record maps to `ConversationTenantSnapshotHealth.Absent`, so it denies as `UnknownTenant` (non-retryable, same public code as a non-member). `conftest.py` retires `test_verify_epic_16_adoption_v2.py`. `Program.cs` and the handoff state only what the tests prove and list the owner actions.
- Conversations `bd948eca` (AppHost): the local topology injects `Hexalith__Conversations__TenantAccess__GatewayBaseAddress` from the gateway's `http` endpoint and, with Keycloak enabled, the `EventStore__Authentication` client-credentials keys for the dedicated realm client `conversations-tenant-access`, whose secret is a generated parameter injected into Keycloak. `ConversationsAppHostTopologyTest` 10/10. With Keycloak disabled (the opt-in AppHost boundary lane) no worker credential exists, so the Conversations host still refuses to start there.
- Focused verification (mapped Debug, assemblies run directly): EventStore `Projections.Checkpointed` 37/37; Tenants Client 86/86; Conversations `Server.Tests` `TenantAccess`+`HostComposition` 200/200 and `Agents` 464/464 (includes `ConversationDeletionRuntimeHostTests` 55/55); `IntegrationTests` `Story16_1EvidenceTests` 7/7 on three consecutive runs; `test_story_16_1_warning_removal.py` 2/2. Full-suite runs are left to the coordinator.

Review loop 2 (2026-11-02, follow-up commits; nothing amended, rebased or pushed):
- EventStore `503a48a55`: a transition may return a null state, so a non-creating event advances the checkpoint without materializing state; recovery releases its lease on every unavailable failure except `RecoveryLeaseLost`; a null history payload throws `HistoryUnavailable`. New `CheckpointedProjectionReviewLoop2Tests` include `ExpectedSequenceArrivingWhileGappedIsMergedAsEvidence`.
- Tenants `13ea4333`: `TenantProjectionCheckpointTransition` returns null until `TenantCreated`, and `GetSnapshotAsync` returns null for a Healthy record without state, so a rejection-only stream of a never-created tenant leaves only a checkpoint and denies as an unknown tenant. Adds `RegisteredDurableStoreResolvesAGatewayClientPerHistoryRead`.
- Conversations `c9e6fdeb` (moves EventStore to `503a48a55` and Tenants to `13ea4333`): every unsafe-state denial uses the unknown-tenant public shape (`TenantIsolationViolation`, `tenant_isolation_violation`, never retryable); only a missing or malformed tenant binding stays `tenant_binding_missing`. The internal reason and `IsRetryable` stay on the decision. `EveryUnsafeStatePublicDenialMatchesANonMemberOfAHealthyTenant` compares each state with a non-member of a healthy tenant. With Keycloak disabled, the AppHost injects the development-only loopback authority `http://127.0.0.1:9/conversations-development-authority` (run mode only, generated secret), so the host starts and every history read denies. With Keycloak enabled, the realm client `conversations-tenant-access` gets an audience mapper for `hexalith-eventstore` and hardcoded claims `eventstore:tenant=system`, `eventstore:domain=tenants`, `eventstore:permission=StreamRead`. That format comes from EventStore's `POST api/v1/streams/read` policy, `ClaimsTenantValidator` and `ClaimsRbacValidator`. The manifest declares the gateway address, the `EventStore:Authentication` keys, the client secret (`tenant-access-worker-client-secret`), the identity need `tenant-access-history-reader` and the egress entries `tenant-access-gateway-stream-read` and `tenant-access-worker-token`. The gateway registration records its marker for a host-supplied client and ignores keyed clients. The diagnostics signal maps a missing record to `Unproven`. The handoff adds the Tenants catalog SDK allow-list step to the release train.
- Verification (mapped Debug, assemblies run directly): EventStore `Projections.Checkpointed` 41/41; Tenants Client 90/90; Conversations `Server.Tests` 1556/1556, 0 skipped; `IntegrationTests` `Story16_1EvidenceTests` 7/7 on two runs; `AppHost.Tests` 11 passed with the boundary case skipped by default. With `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true` the boundary lane passed 1/1. `hexalith-module validate`: validated. `--adoption`: 24 findings, all present before this change. `check-root-submodules.py`: PASS. `git diff --cached --check`: clean.
- Conversations `4d6008e4`: decision (6) is applied to the two delegated owner-query paths as well. Before, `GetConversationAdvisoryMetadataDomainQueryHandler` threw on a retryable denial and returned unavailable. `ConversationResolutionMetadataAuthorization` counted unhealthy projection reasons as unavailable. Both now return the unknown-tenant safe denial for every non-allowed decision; only a thrown exception stays unavailable. Each path has a new test, over six unhealthy reasons, that compares the unhealthy result with the unknown-tenant result. All 12 new cases fail against the previous source. The 12 classes that use these paths pass 366/366.
- Review pass 3 fixes (2026-11-02): Tenants `e3a8db4c` registers checkpoint handlers only for the ten tenant-domain rejection types. Its durable apply path acknowledges a delivery from any domain other than `tenants` as `Duplicate`, without reading or writing. A new test proves that a tenant named `global-administrators` keeps its record. Conversations `bab14ae9` moves the Tenants gitlink and changes these files: the READMEs, the integration guide, the `projection-freshness` precondition and the `stale-projection`/`unavailable-projection` fixtures now describe non-retryable `tenant_isolation_violation`; the handoff now covers the shared worker identity and gateway origin, subscription backoff and monitoring, and a rollback procedure; the misnamed durable test is renamed `...OneNonRetryableNonDisclosingResult`. New tests: `KeycloakDisabledPublishTopologyInjectsNoWorkerAuthority` and `ManifestDeclaresEveryKeyTheEnrolledHostRequiresAtStartup`. Covering tests: Tenants Client 91/91. Both new Tenants assertions fail against `13ea4333`. Server `TenantAccess` 179/179; AppHost topology 12/12; Contracts documentation 41/41 and catalog 76/76; portable tenant-isolation conformance 24/24; `test_story_16_1_warning_removal.py` 2/2.
- Not changed: the tooling-v3 `binding.json` still pins the manifest at `0bed1c29…`, so a real-repository `verify_story_16_*` run now reports source drift for it. The final tooling pair is not required for this story, and the tooling suites rebind their fixtures from the live tree. `test_epic16_tooling.py`, `_v2`, `_v3` and `test_story_16_1_warning_removal.py`: 3066 passed, 1 failed. The failure is `test_held_selected_v3_emits_no_runtime_outputs`, which evaluates the live repository: it returns `FAIL`/`SOURCE_DRIFT` where the test expects `BLOCKED`. It already failed before this story because `.config/dotnet-tools.json` drifted in `1e1d891c`; the manifest is now a second drift subject.

Completion (2026-11-02):
- Submodule sync: root `14ea14e3` moves the ten direct submodules to their latest `main`. The story commits were rebased first: EventStore is now at `b89acfbc8`, 3 commits ahead of its `origin/main`, and Tenants at `7050a706`, 4 ahead. The owner committed the story artifacts as `cfde6c21` and pushed the root `main`. The EventStore and Tenants commits remain local only, so nobody can fetch the root gitlinks until those two are pushed (EventStore first, then Tenants).
- Final native verification at the synced tree (mapped Debug, assemblies run directly with TRX):
  - EventStore Client: 2308/2310. The 2 not-run cases are the opt-in `PendingBatchRecoveryLiveTests`. The `DevelopmentRouteCatalogBuilderTests` failures are fixed upstream.
  - Tenants Client: 91/91.
  - Conversations `Server.Tests`: 1569/1569.
  - `IntegrationTests`: 36/36, including `Story16_1EvidenceTests`.
  - `AppHost.Tests`: 13/13 with `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`.
  - `Contracts.Tests`: 648/648.
  - Every run had 0 skips. `check-root-submodules.py` passes and `git diff --check` is clean.
- `_bmad/scripts/tests` lane: 5608 passed, 65 failed, 57 errors, 17 deselected. 61 failures and all 57 errors match the pre-review run: package-refresh environment, story104 receipts, the Epic 6 supersession and `test_held_selected_v3_emits_no_runtime_outputs`. Four failures are new:
  - `test_round13_defaults_validate_selected_kind_encoding_and_complete_blob[truncated-constant]` failed during a build in the full lane and passes 9/9 alone.
  - Three `test_verify_epic_16_adoption.py` cases report `ARTIFACT_BYTES_DRIFT`. That v1 verifier pins the pre-runtime digest of `module/hexalith.module-manifest.v2.json`, which this story changes. `conftest.py` retires the v1 suite (131 cases) the same way it retires v2; name the file to reproduce it. Routine collection is now 5599 selected and 17 deselected.
- Story completion gate (v9): `_bmad-output/planning-artifacts/v9/story-contracts/16.1.json` exists, but the owner waived the gate under Open Question 3 (native xUnit proof only). Observed results:
  - The exact v9 commands exit 2 (usage: `--contract` required).
  - The tooling-v3 AC-16.1-01 command returns `FAIL`, exit 1 and `SOURCE_DRIFT`, with 0 tests and `runtimeQualified: false`.
  - No final-record pair was generated or inserted.
  - These results are recorded as observed and waived, never as PASS.

## Spec Change Log

2026-10-25: refreshed investigation; frozen intent unchanged. History remains in `spec-epic-16-adoption-v2-preparation.md`.

2026-10-26: replaced stale tooling tasks with completed prerequisite facts and remaining entry work; preserved frozen intent verbatim.

2026-10-27: reconciled committed remediation/CI changes and measured drift; preserved frozen intent and draft/backlog.

2026-10-28: refreshed pins, baseline and failure inventory; preserve accepted evidence, frozen intent and draft/backlog.

2026-10-28 post-tooling: refresh current scanner/source-binding facts and add the remaining scope question; KEEP frozen intent, completed prerequisite evidence and draft/backlog unchanged.

2026-10-28 split: regenerate this draft around runtime work; archive prior notes, retain the complete prior change log and record the human's prerequisite-planning choice. KEEP accepted evidence, runtime hold and draft/backlog.

2026-10-30: refreshed current tuple, exact adoption delta and source-binding failures; archived prior planning verbatim. KEEP frozen intent, accepted evidence, draft/backlog and runtime hold.

2026-10-30 decision: recorded the user's prerequisite-planning choice and removed the answered scope question. KEEP runtime tasks, draft/backlog and ACTIVE hold.

2026-11-01 runtime plan: archived the prior draft verbatim in [history](story-16-1-planning-history-2026-11-01-before-runtime-plan.md) and regenerated Code Map, tasks and ACs from fresh EventStore `26945134`, Tenants `2d3fa6da` and Conversations investigation. Added entry, cross-repo and evidence questions; root tool now 4.41.1. KEEP frozen intent verbatim and draft/backlog until answered.

2026-11-01 answers: recorded the entry-waiver, cross-repo and native-evidence decisions in the frozen block and removed Open Questions. KEEP task order and native-only proof.

2026-11-01 review loop 1: Review rows 1, 4 and 6 were intent gaps, answered in the frozen review decisions. Rows 2, 3, 5, 13 and 15 were spec gaps; rows 10, 11 and 21–25 were patches. Added the review-loop tasks above.
- Known-bad state avoided: a tenant permanently denied after a rejection or out-of-order delivery; tenant existence leaked through retryability; docs claiming proofs the shipped host lacks.
- KEEP: the EventStore checkpointed writer, recovery worker and their AD-6 test matrix; the pure `TenantProjectionTransition`; the opt-in durable store and in-memory default; one-snapshot-per-evaluation; the live Redis restart/two-replica lane; all existing public signatures.

2026-11-01 review loop 2: Pass-2 rows 29–31 were intent gaps, answered in the frozen pass-2 decisions. Rows 32–33 were spec gaps; rows 34–37, 40–42, 44–45 and 47–48 were patches. Added the review-loop-2 tasks.
- Known-bad state avoided: CI's AppHost boundary lane failing to start the host; tenant existence disclosed while unhealthy; a Platform host missing required inputs; durable records for never-created tenants.
- KEEP: everything listed for loop 1, plus loop 1's gateway registration, rejection checkpointing, `Absent` authorization mapping and live first-write/lagging-history cases.

## Review Triage Log

Review pass 1 (2026-11-01; Blind Hunter B, Edge Case Hunter E, Verification Gap V). Verdict | route | evidence.

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 1 | B1, V1, V-other1, E2, E15: the enrolled host has no history reader, so gaps and older cross-replica redeliveries can never recover and the tenant is denied forever | high | intent_gap | `AddHexalithTenantsDurableProjection` resolves `IEventStoreGatewayClient` optionally. Only the opt-in deletion runtime registers one (`ConversationDeletionRuntimeHostExtensions.cs:126`). Every recovery and convergence test injects a history double. Which gateway endpoint, workload identity and `system`/`tenants` read grant to use is not settled by the spec. |
| 2 | E1 (part of B1): persisted rejection and no-handler tenant events are acknowledged as Skipped and never advance the checkpoint | high | bad_spec | The processor returns `SkippedNoHandlers`/`SkippedUnknownEventType` (`EventStoreDomainEventProcessor.cs:229,302`). Rejections are stored as stream events (`AggregateActor.cs:1445`), so the next projected event always arrives as a gap. |
| 3 | B3, E17: the SINGLE-REPLICA disclosure was removed, and the Program/handoff text claims proofs the shipped host does not meet | medium | bad_spec | The live lane calls the processor directly, serves history from an in-process double and bypasses pub/sub. Combined with #1, a restart or replica can still deny legitimate callers. |
| 4 | B4, E4: a Gapped record has no recovery driver once its delivery is dead-lettered and no later event arrives | medium | intent_gap | Recovery runs only on the delivery path (Design Notes). Platform owns the dead-letter policy. The spec does not say whether the subscription must retry without limit or EventStore must sweep. |
| 5 | E14: tenant existence is disclosed through retryability | medium | bad_spec | A missing snapshot maps to `Unproven` (IsStale), which gives a retryable `TenantAccessStale` before any state check (`TenantAccessEvaluator.cs:324`). A healthy existing tenant's non-member gets a non-retryable isolation violation (`ConversationTenantAccessDecision.cs:184-188`). Previously a null state produced `UnknownTenant`. This breaks the frozen matrix row "Deny without disclosing tenant existence". |
| 6 | B7: the durable path has no freshness or rollback signal, so an older Healthy record keeps authorizing after a state-store restore or a stalled subscription | medium | intent_gap | `ConversationTenantSnapshotHealth.From` never sets `HasRollback`/`Watermark`. The frozen matrix lists "stale" state, but the spec never defines staleness. |
| 7 | B2, V2: identity equality between published bytes and gateway-read bytes is unproven | maybe-false (high if true) | defer | Every history double copies the envelope. Settling it needs a live EventStore publish followed by a gateway range read (AppHost lane), with protected and unprotected payloads. |
| 8 | B5: one forged envelope at the accepted sequence makes the tenant Corrupt | false | reject | The frozen matrix requires "Conflicting identity/payload → Persist terminal Corrupt" and AD-6 requires the same at an accepted sequence. Verifying against history first would contradict both. |
| 9 | E7 (and the operator-path part of B5): a deterministic TransitionFailed or MalformedRecord retries forever, and Corrupt has no repair path | medium | defer | Fail-closed denial is intended. The frozen intent makes Corrupt terminal and says Conversations never clears it, so a repair or rebuild path is outside this story. |
| 10 | B6: a stored record missing `health`/`schemaVersion` loads as Healthy v1, because initializers default to valid values | low | patch | `CheckpointedProjectionRecord` initializes `Health = Healthy` and `SchemaVersion = 1`. This needs an externally corrupted record. The fix is a direct correction. |
| 11 | B8, V-other3: the pytest baseline claim does not isolate the change, and the 58 `test_verify_epic_16_adoption_v2.py` failures come from the sprint-status transition | medium | patch | The frozen-state suite asserts 16.1 is backlog. Per the retirement policy for story freeze checks, retire it through conftest. The baseline methodology part is maybe-false: settling it needs an unmodified `0f068ad7` run. |
| 12 | B9a: EventStore Client has 2 failed `DevelopmentRouteCatalogBuilderTests` and 2 not-executed tests | maybe-false | defer | The not-executed tests are pre-existing opt-in `PendingBatchRecoveryLiveTests` and passed 2/2 live. The failures are in untouched Routing code. Settling it needs a baseline `269451340a87` run. |
| 13 | B9b: no package-mode build and no release order. Conversations CI uses package references, which lack the new types until EventStore and then Tenants publish | high | bad_spec | Pinned package versions predate `Checkpointed`. The push/release train (EventStore, then Tenants with an EventStore bump, then Conversations package bumps) is not specified. |
| 14 | B10: wrong EventStore short SHA (`26945164` should be `26945134`) and stale Code Map wording | low | reject | The fix edits this build's spec. Corrected during the amendment. |
| 15 | B11: live tests pre-populate history, never race two first writers through Redis, and history doubles don't check tenant/domain/aggregate | medium | bad_spec | `TwoReplicasConvergeOnOnePersistedDecision` appends all history first and sends `events[0]` to one replica. The doubles ignore the request's stream identity. |
| 16 | B12: unbounded Pending growth while Gapped, and the 30 s lease is not renewed during multi-page replay | maybe-false (medium) | defer | Tenant streams are short today. Settling it needs a measured long-stream replay. |
| 17 | B13: duplicated history doubles, the `ScenarioNN` binding in `epic16_tooling.py:441`, and missing docs | low | reject | Developer-only. The fix is more than a direct correction, and the binding is moot since the tooling final pair was dropped. |
| 18 | E3: tenants created before the durable opt-in have no record and are denied until a new event arrives | medium | defer | Pre-existing: the in-memory projection was equally empty after every start. Once #1 is resolved, the next event backfills from sequence 1. |
| 19 | E5: replaying PayloadVersion > 1 throws TransitionFailed | false | reject | No tenant event has an upcaster, and Tenants' own authority rejects `PayloadVersion > 1` (`TenantsGlobalAdministratorAuthority.cs:53`). |
| 20 | E6: an unknown history event type is skipped silently | low | reject | Only reachable through a contract type rename. The fix adds branching. |
| 21 | E8: a truncated history page with no new events returns a partial range, which persists terminal HistoryMissing Corrupt | medium | patch | `CheckpointedProjectionHistory.cs` returns when `highest == cursor` even if `IsTruncated`. The direct fix is to throw HistoryUnavailable. |
| 22 | E9: a record that disappears after the Pending CAS is acknowledged as Recovered | low | patch | `AcquireLeaseAsync` returns Recovered for `current is null`. The direct fix is to throw unavailable. |
| 23 | E10: concurrent same-process recoveries share a WorkerId and supersede each other's lease | low | patch | The lease-held check exempts the same WorkerId, so it causes churn and redeliveries. The direct fix is a per-invocation lease id. |
| 24 | E11: a custom `IConversationTenantProjectionSignal` is ignored when the durable store is present | low | reject | No such signal is registered, and the fix adds branching. |
| 25 | E12: a safe tenant id `sha256-<hex>` can collide with another tenant's hashed key | low | patch | `KeyFor` uses a prefix that safe ids can produce. The direct fix is a separator safe ids cannot contain. |
| 26 | E13: the live sidecar's port pre-allocation races concurrent lanes | low | reject | Intermittent, test-only. The fix is a retry or port-0 redesign. |
| 27 | E16: enrolled registration now requires `ReadModelStateStoreName` and replaces a custom store | low | reject | The host already requires the key. No enrolled consumer registers a custom store. |
| 28 | V-other2: envelopes with a blank domain retry forever on the durable path | low | reject | The current `EventPublisher` always sets the domain. The fix adds branching. |

Review pass 2 (2026-11-01, after loop 1 at EventStore `8449adfdf`, Tenants `5b3e4aa7`, Conversations `bd948eca`). Full coordinator run: Tenants 86/86, Conversations Server 1549/1549, Integration 36/36, AppHost 10/10 plus 1 opt-in not run; EventStore 2302/2306 (the 2 Routing failures and 2 opt-in live cases from row 12); root submodules PASS.

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 29 | B, V-other, E: the CI AppHost boundary lane runs with Keycloak disabled, gets no worker authority, and the host refuses to start | high | intent_gap | `RuntimeBoundaryTestsEnabled` is true when `GITHUB_ACTIONS` and `CI` are both true (`ConversationsAppHostRuntimeBoundaryTest.cs:42-48`). `EventStore__Authentication__*` is injected only when `security is not null`. CI sets `aspire-continue-on-error: false`. The spec does not settle how Keycloak-off mode gets an authority. |
| 30 | B: the local `conversations-tenant-access` realm client has no audience or tenant/domain mappers, so local gap recovery is still rejected | medium | intent_gap | The realm client has `fullScopeAllowed: false` and no protocol mappers. Settled together with #29. |
| 31 | B, V-other, E: public denials for a Gapped, Corrupt, malformed or unavailable existing tenant differ from the non-retryable `UnknownTenant` given for a missing record, so existence is disclosed while a tenant is unhealthy | medium | intent_gap | `IsPublicRetryable` (`ConversationTenantAccessDecision.cs`) is true for stale, gap and unavailable. The frozen row covers malformed and unavailable too. Strict non-disclosure costs members their retry hint. |
| 32 | B: the module manifest does not declare the new required gateway and `EventStore:Authentication:*` inputs, so a Platform-provisioned host fails at startup | high | bad_spec | `configurationInputs` lists only six Dapr keys and two secrets. |
| 33 | E: a rejection delivered before `TenantCreated` (for example `TenantNotFoundRejection`) materializes a Healthy record with Status Unknown, and every rejection for an arbitrary id creates a durable record | medium | bad_spec | Loop 1 routes rejections to the store. The transition builds state from null, so the denial becomes `UnmappedStatus` instead of `UnknownTenant`. |
| 34 | B, V-other, E: the handoff says the AppHost "does not inject them yet" and omits the Keycloak-off refusal | medium | patch | Contradicted by `bd948eca`. A docs-only fix. |
| 35 | B: the release train omits the Tenants catalog SDK allow-list, which accepts EventStore 3.129.0–3.130.2 only | medium | patch | `references/Hexalith.Tenants/Directory.Build.targets:11-24`. Add the step to the handoff. |
| 36 | B: when the host supplies its own client, the registration records no marker, so the deletion runtime later adds a second client | low | patch | `ConversationEventStoreGatewayRegistration.Add(keepHostClient: true)` returns early. The direct fix records the marker. |
| 37 | B, E: there is no positive test for a loopback HTTP gateway origin, and non-loopback HTTP is refused outside run mode | low | patch | Add the loopback case. Refusing non-loopback HTTP matches the deletion runtime, and deployment is outside the intent. |
| 38 | B: the Implementation Notes and Code Map contradict the loop-1 changes | low | reject | The fix edits this build's spec. |
| 39 | B: AC3 is not shown at the final commits, and Verification leaves out the AppHost tests | false | reject | The coordinator's full run above covers the final commits, including AppHost.Tests. |
| 40 | B: `MissingEnrolledDeliveryInputs…` asserts only on `key.Split(':')[0]` | low | patch | Assert on the full key. |
| 41 | V: no test delivers `Expected` to a Gapped record | Regression gap (pre-verified) | patch | Add `ExpectedSequenceArrivingWhileGappedIsMergedAsEvidence`. |
| 42 | V: no registration-level test proves the gateway client is resolved per read | Regression gap (pre-verified) | patch | Add a counting scoped-gateway test to `TenantDurableProjectionRegistrationTests`. |
| 43 | E: startup does not validate Authority/TokenEndpoint shape | low | reject | A misconfiguration fails closed and is visible in logs. The fix adds validation logic. |
| 44 | E: a host with only a keyed `IEventStoreGatewayClient` skips the unkeyed registration | low | patch | Filter `!IsKeyedService`. |
| 45 | E: recovery keeps its lease after a ProviderUnavailable, MalformedRecord or ConcurrencyExhausted failure | low | patch | It only delays redelivery by `LeaseDuration`. The direct fix broadens the release filter. |
| 46 | E: a gap range over `MaxHistoryPages × HistoryPageSize` never completes | low | reject | That is more than 100k tenant events. Related to deferred row 16. |
| 47 | E: a null history payload fingerprints as empty bytes and persists Corrupt | medium | patch | `StreamReadEvent.Payload` is nullable. Throw `HistoryUnavailable` instead. |
| 48 | E: onboarding diagnostics report "current" for a never-projected tenant, because the signal maps a missing record to `Absent` | medium | patch | `DefaultConversationTenantProjectionSignal` serves diagnostics only when a durable store is present. Map null to `Unproven` there, and keep `Absent` for the authorization snapshot. |
| 50 | B, E: unknown or short-name history types are skipped silently | low | reject | carried row 20. The live registry keys full names only (`EventStoreDomainEventsServiceCollectionExtensions.cs:111-114`), and EventPersister writes full names. |
| 51 | E: replaying PayloadVersion > 1 fails | false | reject | carried row 19. |
| 52 | E: a blank-domain envelope retries forever | low | reject | carried row 28. |

Review pass 3 (2026-11-02, after loop 2 at EventStore `503a48a55`, Tenants `13ea4333`, Conversations `4d6008e4`). Full coordinator run with `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`: Tenants 90/90, Conversations Server 1568/1568, Integration 36/36, AppHost 12/12 including the boundary lane; EventStore 2306/2310 (the row-12 Routing failures and opt-in live cases); root submodules PASS.

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 53 | B: triage row 49 is missing; sprint status differs from the spec; the Verification text is out of date; Implementation Notes are superseded | low | reject | The fixes edit this build's spec. |
| 54 | B: AC3 is not shown at the final commits | false | reject | The full run above is at the final commits. The only non-passing EventStore cases are the deferred row-12 items. |
| 55 | B, V-other: README:93, `ConversationCorePreconditionCatalog.cs:28-33`, `docs/integration-guide.md:199` and the `TenantIsolationConformanceFixtures` stale/unavailable scenarios still describe retryable `tenant_projection_stale` for tenant-access denials | medium | patch | Decision (6) made these denials `tenant_isolation_violation` and non-retryable. The code is still used for state-load failures. |
| 56 | B, E: tenant access and the deletion runtime share one `EventStore:Authentication` identity and gateway origin, and that is not documented | medium | patch | Loop 1 shares one gateway client. The AppHost never enables the deletion runtime. Document that one worker identity needs both grants and the same origin when both are on. |
| 57 | B: the unbounded-retry owner action states no backoff or monitoring | medium | patch | Add a backoff and monitoring requirement to the handoff. Metrics and alerts stay under deferred row 9. |
| 58 | B: no rollback procedure, although the frozen intent says rollback removes adoption/configuration | medium | patch | A handoff section: revert the enrolled registration, keep or drop the `tenants-projection:v1:*` keys, remove the manifest inputs, and note the warning-removal test. |
| 59 | B: a missing record's internal reason is `UnknownTenant`, so a never-projected tenant can't be told from a nonexistent one | low | reject | Telemetry only. Distinguishing them needs evaluator changes. Related to deferred cold-start row 18. |
| 60 | B: the tooling-v3 `binding.json` pins the old manifest hash, and the restore-rebuild step exists only as a handoff checkbox | medium | defer | `test_held_selected_v3_emits_no_runtime_outputs` already fails at baseline `0f068ad7`. The restore rebuild is deferred by decision (3). |
| 61 | B: a NUL byte in `ConversationTenantAccessServiceTest.cs` renders the file as binary in the diff | low | reject | Pre-existing: `0f068ad7` already has 2 NUL bytes. |
| 62 | B: the test `MalformedAndUnavailableSnapshotsDenyWithOneRetryableNonDisclosingResult` is now misnamed | low | patch | A rename. |
| 63 | B: the durable store never checks a delivery's tenant/domain, and GlobalAdmin* rejection types (domain and aggregate `global-administrators`) get checkpoint handlers | medium | patch | `TenantIdentity.cs:15-16`. A tenant with id `global-administrators` would collide. Guard on domain `tenants` and register only tenant-domain rejection types. |
| 64 | V: the publish-mode guard on the development-only authority is untested | Regression gap (pre-verified) | patch | Add `KeycloakDisabledPublishTopologyInjectsNoWorkerAuthority`. |
| 65 | V: nothing ties the new manifest inputs to the keys the host requires | Regression gap (pre-verified) | patch | Add a Server.Tests manifest-versus-required-keys test for the new keys. |
| 66 | V, B: the Keycloak-on history-read grant is never exercised end to end | Broken-verification gap (pre-verified) | defer | Needs a Keycloak-enabled AppHost lane. |
| 67 | E: a host-supplied client reused by the deletion runtime lacks the authority transport | low | reject | No production host supplies a client. Related to row 36. |
| 68 | E: the lease is not released after cancellation or other exceptions | low | reject | A bounded `LeaseDuration` delay that fails safe. The fix needs I/O after cancellation. |
| 69 | E: Authority/TokenEndpoint validation | low | reject | carried row 43. |
| 70 | E, B: publish-mode non-loopback gateway origin | low | patch | carried row 37 (the loopback test was added). |
| 71 | E: a blank-domain envelope retries forever | low | reject | carried row 52. |
| 72 | E: unknown history types are skipped | low | reject | carried row 50. |
| 73 | E: PayloadVersion > 1 | false | reject | carried row 51. |

Review pass 4 (2026-11-02, after completion at Conversations `918fa9a6`, EventStore `b89acfbc8`, Tenants `7050a706`; Blind Hunter B, Edge Case Hunter E, Verification Gap V, Acceptance Auditor A). 42 findings: 2 decision-needed (both resolved), 9 patch (one from decision 2), 2 defer (one from decision 1), 25 rejected.

### Review Findings

- [x] [Review][Decision] Root `main` points at EventStore and Tenants commits that were never pushed, so CI cannot check out — **Resolved 2026-11-02 (Jerome: push EventStore, then Tenants):** both were rebased onto their advanced `origin/main` (upstream added only a story 2.14 spec and nested gitlinks, and every story file is byte-identical) and pushed: EventStore `311d61d29..4db51a968`, then Tenants `7b4b1302..0543a34a`. Root `6fc8c7fe` (pushed) moves both gitlinks to those commits. The package-mode release train is deferred below. Original finding: The CI run on `main` at `918fa9a6` (run 41430484017) fails `build-and-test`, `conformance`, `release-tooling`, `admin-web` and the minimal-module job at submodule checkout: "upload-pack: not our ref b89acfbc8…; Fetched in submodule path 'references/Hexalith.EventStore', but it did not contain b89acfbc8". Every story push since `c9e6fdeb` (EventStore `503a48a55`) failed the same way. EventStore `b89acfbc8` (3 commits ahead of `origin/main`) and Tenants `7050a706` (4 ahead) exist only on this workstation, so nobody can clone the root with submodules. Pushing them is not enough: package-mode CI cannot compile the new `Checkpointed` and durable types until the release train publishes EventStore, then Tenants, and Conversations bumps `Directory.Packages.props`. Until then none of the new C# tests run in CI. The baseline `0f068ad7` was already red, for a different reason (HXW011: Platform release tag and Builds pin). Options: push EventStore then Tenants now; also run the release train; or keep CI red and track it. (B+A+V)
- [x] [Review][Decision] The gateway client tenant access registers also turns on native-source projection admission — **Resolved 2026-11-02 (Jerome: option B, document and accept):** keep the shared client and add a handoff owner note (patch below). Original finding: On every enrolled host, `ConversationEventStoreGatewayRegistration.Add` registers an unkeyed `IEventStoreGatewayClient` (`ConversationTenantAccessServiceCollectionExtensions.cs:87`). EventStore `DomainProjectionNativeSourceAdmission.cs:58` resolves that service if it is present. Before 16.1 it was absent unless the deletion runtime was on, so a native-proof projection request stayed unproven without any I/O. Now, every named projection dispatch that carries a `NativeSourceProof` (set by `NamedProjectionDispatchCoordinator`) reads the whole Conversation stream through the tenant-access worker identity. That identity's documented grant covers only tenant `system` and domain `tenants`. The read is refused (or fails at token acquisition with Keycloak off), so the result is still unproven. The cost: every such dispatch pays an authenticated gateway round trip (up to 30 s) and leaves a denied read on the gateway, and admission semantics change silently if the identity is ever granted more. Options: give the tenant-access history a keyed or private client (this departs from review loop 1's shared-client design); keep the shared client, grant Conversation-stream reads and document that admission now succeeds; or accept the cost and document it in the handoff. (E)
- [x] [Review][Patch] Add a handoff owner note on native-source projection admission. It now attempts an authoritative Conversation-stream read with the tenant-access worker identity; while that identity holds only the `system`/`tenants` grant the read is refused and the projection stays unproven, at the cost of one refused gateway round trip per native-proof dispatch (the token is cached for about 285 s). Granting that identity Conversation-stream reads turns native admission on, so the grant must be deliberate [module/platform-handoff.md:402]
- [x] [Review][Patch] Test the checkpointed failure branches: a transition that throws (`TransitionFailed`: the record stays Gapped, the lease is released, never Corrupt), `MaxHistoryPages` exhaustion, `MaxHorizonExtensions` exhaustion, two different events at one sequence in a history page, and Tenants history with `PayloadVersion = 2` or a non-json format [references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/Checkpointed/CheckpointedProjectionRecoveryWorker.cs:123]
- [x] [Review][Patch] The docs say onboarding diagnostics report stale tenant access, but `ConversationOnboardingDiagnosticsService` returns Hidden for every denied decision, and durable access denies every non-Healthy snapshot. Correct the precondition catalog, both READMEs and the integration guide [src/Hexalith.Conversations.Contracts/Diagnostics/ConversationCorePreconditionCatalog.cs:33]
- [x] [Review][Patch] Tie the conformance fixture's `ExpectedErrorCode` for `stale-projection` and `unavailable-projection` to the live `ConversationTenantAccessDecision.ToRejection` code and its non-retryability, so a revert of those rows fails a test [src/Hexalith.Conversations.Testing/Fixtures/TenantIsolationConformanceFixtures.cs:67]
- [x] [Review][Patch] The deletion runtime copies the shared gateway-origin and worker-authority validation. In the `Program.cs` composition its copy can no longer be reached, and its five `EventStore:Authentication:*` rows in `IncompleteConfigurationRefusesWithoutEchoingValues` now pass on the tenant-access refusal. Call `ConversationEventStoreGatewayRegistration.TryParse` and `AuthorityFailures` instead, and make those rows assert the refusal they actually exercise [src/Hexalith.Conversations.Server/Agents/ConversationDeletionRuntimeHostExtensions.cs:57]
- [x] [Review][Patch] The manifest test does not check the `tenant-access-history-reader` identity need or the `tenant-access-gateway-stream-read` and `tenant-access-worker-token` egress entries [tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantAccessManifestTest.cs:26]
- [x] [Review][Patch] The public `CheckpointedProjectionEvent.FromHistory` turns a null payload into empty bytes, which contradicts the history reader's own rule ("a missing payload is not the empty payload"). Reject a null payload instead [references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/Checkpointed/CheckpointedProjectionEvent.cs:72]
- [x] [Review][Patch] The deferred-work entry about the two failing `DevelopmentRouteCatalogBuilderTests` is still open, although Completion records them as fixed upstream [_bmad-output/implementation-artifacts/deferred-work.md:1134]
- [x] [Review][Patch] `Program.cs` and the handoff say gap recovery runs only on redelivery. In fact the writer starts recovery in the same delivery that commits Pending, and any later delivery drives it again [module/platform-handoff.md:402]
- [x] [Review][Defer] Package-mode CI cannot compile Story 16.1 until the release train runs [Directory.Packages.props:1] — deferred (decision 1, 2026-11-02): pushing fixed CI checkout, but CI uses package references and the pinned EventStore/Tenants versions predate the `Checkpointed` and durable types. Publish EventStore, then Tenants with the EventStore bump (inside the Tenants catalog SDK allow-list), then bump the Conversations package versions, as `module/platform-handoff.md` lists.
- [x] [Review][Defer] No release note covers the public denial change: unhealthy tenant access is now a non-retryable `tenant_isolation_violation`, and the published `Hexalith.Conversations.Testing` fixtures changed their expected codes [docs/release-notes-1.3.1.md:1] — deferred: a release-time item. Write candidate notes when the release train publishes EventStore, Tenants and the Conversations package bump; the version is unknown until then.

#### Rejected

- B1: unknown tenant event types are checkpoint-only during recovery. low: pre-existing, since the in-memory projection and the live processor never applied unknown types either (carried rows 20/50/72). Failing closed would add branching.
- B2: history with `PayloadVersion > 1` makes recovery fail forever. false: no tenant event is v2, and `TenantsGlobalAdministratorAuthority.cs:53` rejects `PayloadVersion > 1` (carried rows 19/51/73).
- B5, E2: startup does not validate the Authority/TokenEndpoint shape. low: a misconfiguration fails closed and is logged; the fix is new validation logic (carried rows 43/69).
- B6a, V4, E1: AppHost publish mode binds a non-loopback HTTP gateway origin. low: the frozen intent excludes deployment, and Platform supplies the origin through the manifest (carried rows 37/70).
- B6b: `KeycloakDisabledPublishTopologyInjectsNoWorkerAuthority` evaluates the environment in run mode. false: the topology branches on `builder.ExecutionContext.IsRunMode` at composition time, so the publish builder never adds those variables or the parameter, and the test asserts the parameter is absent.
- B7: nothing internal records why an unhealthy projection was denied. false: Commons `TenantAccessEvaluator.Denied` logs every denial with its reason (`TenantAccessEvaluator.cs:495`). Classifying `TenantProjectionPoisoned` as `ContextMismatch` predates 16.1 (`957765da`).
- B9: the checkpoint and lease options are not configurable. low: this matters only for the deferred long-replay item (row 16), and the fix adds public surface.
- B10: the warning-removal guard runs only in the BMAD lane. low: CI is BMAD-agnostic by policy (2026-10-23), and moving the guard is more than a direct correction.
- B11, E8: four copies of the history test double, and the copied sidecar port probe. low: developer-only test duplication and an intermittent test race (carried rows 17/26).
- B12, E7: a host-supplied client against the recorded marker origin. low: no production host supplies its own client (carried row 67).
- B14b: `test_held_selected_v3_emits_no_runtime_outputs` is still collected. low: it already fails at `0f068ad7`, and the open ledger entry to re-bind or retire tooling-v3 already tracks it.
- A2: retry exhaustion leaves the tenant authorized. false: a lost compare-and-swap means another writer committed, and authorization reads the newest persisted Healthy record. The "authorization unavailable" row covers a store failure, which is what the test name states.
- A3: `Absent` health is value-equal to Healthy. low: review loop 1 chose this mapping so a missing record denies as `UnknownTenant`, and the denial comes from the same single snapshot read. Distinguishing the two needs evaluator changes (row 59).
- A6: AC3's zero skips are not shown for the EventStore Client tests. The fix edits this spec, and the two cases not run are the pre-existing opt-in live tests (rows 12/54).
- A8: retiring the v1 adoption suite goes beyond the task list. false: there is no bad outcome. It follows the conftest retirement policy and is recorded in Completion and in `94896cb8`.
- E4, E9: `FromDelivery` throws before the domain filter. false: this is unreachable, because `EventPublisher` always sets the domain and the processor always sets the event type and fingerprint (carried rows 28/52/71).
- E5: a tenants-domain delivery under a tenant other than `system` changes a record. false: `TenantsSystemTenantValidator` refuses every Tenants command outside tenant `system`, so no such event exists.
- E6: protected history payloads are fingerprinted as ciphertext. false: `StreamsController` unprotects every event before returning it (`TryMakeReplayEventReadableAsync`, line 232) and refuses unreadable ones. Whether live and history bytes are equal stays in the existing deferred entry.
- E12: the development authority fails only because nothing listens on 127.0.0.1:9. low: every read fails closed either way, and a stall would need something listening on port 9.

Review pass 5 (2026-11-02, after review-pass-4 patches; Blind Hunter B, Edge Case Hunter E, Verification Gap V). Verdict | route | evidence.

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 74 | B1: package-mode release train remains open and the handoff says the Tenants catalog stops at 3.130.2 | medium | patch | The release train is already deferred by the pass-4 owner decision. The current `references/Hexalith.Tenants/Directory.Build.targets` accepts through 3.132.1, so the handoff's range is stale and can misdirect the release owner; correct that sentence. |
| 75 | B2: Keycloak worker token is untested against the real gateway | Broken-verification gap | defer | Carried row 66: the realm JSON and synthetic transport do not prove a Keycloak-enabled stream read. A Keycloak-enabled AppHost lane remains deferred. |
| 76 | B3: real published-event and gateway-history parity is untested | maybe-false (high if true) | defer | Carried row 7: history doubles copy delivery bytes. A real publish/read test with protection on and off would settle it. |
| 77 | B4: existing tenants have no durable record until another event arrives | medium | defer | Carried row 18: the old process-local projection was also empty after restart; the next event can backfill through history, but cold-start backfill remains operational work. |
| 78 | B5: a restored older Healthy state can authorize | medium | defer | Carried row 60 and the 2026-11-01 staleness decision: restore reconciliation is outside this story and the handoff records the required rebuild. |
| 79 | B6: terminal Corrupt and deterministic failures lack repair | medium | defer | Carried row 9: the frozen intent makes Corrupt terminal. An operator repair/rebuild path is separately deferred. |
| 80 | B7: a 30-second lease is not renewed through long history replay | maybe-false (medium if true) | defer | Carried row 16: a measured long-stream replay is needed to establish the tenant-stream risk. |
| 81 | B8: Pending evidence has no size limit | maybe-false (medium if true) | defer | Carried row 16: a measured sustained gap is needed to establish a state-store size failure. |
| 82 | B9: Platform's retry/no-dead-letter policy is still an owner action | medium | defer | Carried row 4 as resolved policy choice: `module/platform-handoff.md` now specifies unbounded retry, backoff and monitoring, but Platform deployment work remains unchecked. |
| 83 | B10: a host-supplied gateway client can target another origin | low | reject | Carried row 67: no production host supplies its own unkeyed gateway client; the enrolled Program uses the SDK registration. An extra compatibility contract would add complexity for an unused path. |
| 84 | B11: manifest HTTPS egress differs from local AppHost HTTP | low | reject | The manifest describes deployment egress; the local AppHost uses a loopback HTTP endpoint, which `TryParse` explicitly permits. Deployment is excluded by the frozen intent, and this difference does not make the local host fail. |
| 85 | B12: the parameterless registration still uses an in-memory store | false | reject | The overload is explicitly the local compatibility path; production `Program.cs` calls `AddConversationTenantAccess(builder.Configuration)`, which installs the durable store. |
| 86 | V1: actual EventPublisher-to-StreamsController parity is unverified | maybe-false (high if true) | defer | Carried row 7: the live test's gateway double copies the envelope; protected and unprotected real publish/read parity remains deferred. |
| 87 | V2: the tenant-access service-account token is not exercised against EventStore | Broken-verification gap | defer | Carried row 66: tests inspect realm JSON and a synthetic token response, not a Keycloak-issued token accepted by the real gateway. |
| 88 | E1: a published non-loopback HTTP gateway origin fails startup | low | reject | Carried row 70: `TryParse` deliberately permits HTTPS and loopback HTTP only; the frozen intent excludes deployment, and the loopback development path has a positive test. |
| 89 | E2: a host-supplied unkeyed client can read the wrong origin | low | reject | Carried row 67: no production host supplies one. The SDK-registered client is bound to the validated origin. |
| 90 | E3: duplicate history sequence with equal evidence but conflicting serialization metadata is accepted | medium | patch | `CheckpointedProjectionHistory.ReadRangeAsync` compares only `Evidence` on duplicate sequences, then discards the second event. A malformed gateway page can therefore let replay use the first metadata and mark Healthy; reject the ambiguity as HistoryUnavailable. |
| 91 | E4: a dual-interface custom store can make diagnostics report Current despite a gapped snapshot | false | reject | The enrolled registration replaces `ITenantProjectionStore` with `DurableTenantProjectionStore`, which does not implement `IConversationTenantProjectionSignal`; production diagnostics reads its snapshot health. A custom dual-interface store is not the enrolled path. |

## Design Notes

The record stores state, health (`Healthy|Gapped|Corrupt|Unavailable`), `lastContiguousSequence` with its accepted fingerprint, `expected`, `recoveryThrough`, sequence-keyed pending evidence, and a lease (fencing token plus record version). One CAS write covers all of it.

How each delivery is handled:
- **Exact-next sequence:** apply the transition.
- **Same sequence, same fingerprint:** no-op.
- **Older sequence:** compare the fingerprint with authoritative history at that sequence.
- **Future sequence:** record Gapped and merge the evidence.
- **Recovery:** after a pending commit, the delivery path tries recovery under the lease. If recovery is unavailable, the delivery throws, and the redelivery re-drives recovery.
- **Authorization:** never starts recovery.

## Verification

**Commands:**
- Build each owner Client test project and Conversations `Server.Tests`/`IntegrationTests` in Debug with `-p:UseHexalithProjectReferences=true`. Run each assembly directly with `-trx <file>`. Expected: passing, nonzero counts, 0 skipped.
- Run `python3 -m pytest -q _bmad/scripts/tests`. Expected: the marker-check updates stay green.
- Run `python3 scripts/check-root-submodules.py --repository .` and `git diff --check`. Expected: pass.

**Review pass 4, 2026-11-02:** Mapped Debug native runs passed EventStore Client 2313/2313, Tenants Client 93/93, Conversations Server.Tests 1570/1570, and focused Contracts onboarding 37/37, with zero skipped or not executed in each TRX. The Story 16.1 live Integration class passed 7/7 with zero skipped or not executed using `dotnet tests/Hexalith.Conversations.IntegrationTests/bin/Debug/net10.0/Hexalith.Conversations.IntegrationTests.dll -automated sync -failSkips -parallel none -class Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests -trx /tmp/story161-integration-focused.trx`. The full Integration assembly built. Its unfiltered run with `timeout 180s dotnet tests/Hexalith.Conversations.IntegrationTests/bin/Debug/net10.0/Hexalith.Conversations.IntegrationTests.dll -automated sync -failSkips -parallel none -result-trx /tmp/story161-integration-full-serial.trx -reporter quiet` reached the existing `SmC2HotPathBenchmark.FrozenWarmPathInventoryEmitsComparableRawSamples`, produced no TRX, and timed out with exit 124; the full assembly remains unverified in this pass. The focused warning marker tests passed 2/2, the root-submodule checker passed, and `git diff --check` passed in all three changed repositories.

**After review pass 5 patches:** The source-mapped IntegrationTests and Server.Tests projects rebuilt in Debug with zero warnings and errors. Native TRX results: EventStore Client 2315/2315 with the isolated-endpoint `PendingBatchRecoveryLiveTests` class excluded, Tenants Client 93/93, Conversations Server.Tests 1570/1570, and the Story 16.1 live Integration class 7/7; each recorded zero not-executed tests. The EventStore unfiltered attempt produced 2315 passed and two `FAIL_SKIP: Requires isolated Dapr endpoints` failures in that opt-in class; the filtered run passed. `python3 scripts/check-root-submodules.py --repository .` and `git diff --check` in all three repositories passed. The full Integration assembly remains unverified for the benchmark timeout above.

The IntegrationTests assembly passed 35/35 with zero not-executed using `-class- Hexalith.Conversations.IntegrationTests.Performance.SmC2HotPathBenchmark`; that existing performance benchmark is the sole exclusion. The frozen 2026-11-01 user decision uses native xUnit as Story 16.1 completion proof and waives the v9/tooling-v3 `6/6/0/0/0/0` final pair, so the historical v9 record commands were not run.
