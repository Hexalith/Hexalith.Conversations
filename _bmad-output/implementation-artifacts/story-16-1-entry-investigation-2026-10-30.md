# Story 16.1 entry investigation — 2026-10-30

Investigated clean Conversations `main` at
`fdd0e9454f56c0dce30a6b14ca7bc17c33b4f961`, tree
`d474ce9c796e4b764d1de91f798008db431fc4f0`. This resumes draft planning.
Story 16.1 remains draft/backlog; no runtime implementation is authorized by
these measurements.

## Current tuple and prerequisites

The root tool manifest now pins **4.38.2**. The installed command reports
`4.38.2+2574bf2f3da5367d8801f0d7661d66155e5d90b0`. Direct Platform pins
**4.38.3**, with prepared range `[4.38.3, 5.0.0-0)` and published Builds source
`d1931b1e803144048d927fff16cb8119afde541e`. Platform's README explicitly leaves
exact-candidate integration/CI, verification of its recorded Builds selection,
Platform identity publication and consumer adoption pending. Its earlier
accepted 4.35.2 evidence does not qualify this candidate. Root 4.38.2 is below
the prepared range. Neither pin was changed here.

The recorded direct root selections are:

| Dependency | Root gitlink and clean checkout HEAD |
| --- | --- |
| Builds | `a13d7972395f989955cf00f1047f1ba50b1a6b73` |
| Platform | `1e4e447c84109f2d8ba6e7e2357953e5cdbef9a4` |
| EventStore | `7b1df62102efb19da03553a890c62eb5ad6aeafe` |
| Tenants | `0d469d7c8fedc383f6258cbf2798e62cf9847304` |

The [entry state](../../module/validation/epic-16-published-artifact-qualification/entry-20261030-story161-01/entry-state.json)
retains all twelve raw mode-160000 gitlinks, direct checkout identities, protected
input digests and index digest. No nested dependencies were initialized or updated.

The tooling and published-artifact prerequisite specs are done within their
separate source/4.38.1 measurement scopes. The latter's sixteen adoption failures
and catalog disagreement remain historical failures; its unavailable owner/native
gates remain blocked. Those completed tasks do not qualify today's tuple.

The [October 30 handoff](../../module/platform-handoff.md) already selects
prerequisite-first execution and the AppHost exemption proposal. Keep that
sequence; do not ask the user to choose it again. The outstanding choice is
whether to prepare a separate current-tuple qualification/source-binding plan
or validate supplied accepted current owner records. The proposed AppHost
exemption still lacks named acceptance and concerns Story 16.3 entry.

## Fresh command results

[commands.json](../../module/validation/epic-16-published-artifact-qualification/entry-20261030-story161-01/commands.json)
records exact argument arrays, working directories, process bounds, native exits,
durations and separate stdout/stderr sizes and SHA-256 hashes. Capture reused the
existing qualification collector's bounded Runner; each product/check command
had a 60-second limit and an 8 MiB per-stream bound. No command hit a bound.
This directory contains entry observations, not a new artifact qualification.

| Command | Result |
| --- | --- |
| `python3 scripts/check-root-submodules.py --repository .` | PASS / exit 0 |
| `dotnet tool run hexalith-module --version` | Exit 0; actual 4.38.2 identity above |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json` | PASS / exit 0 |
| Same validation with `--adoption --output json`, and separately with `--adoption` | FAIL / exit 1 / HXA001; six PASS, twenty-one FAIL |
| Six exact commands in tooling-v1 `story-contracts/16.1.json` | Each FAIL / exit 1 / SOURCE_DRIFT |
| Six exact commands in tooling-v2 `story-contracts/16.1.json` | Each FAIL / exit 1 / SOURCE_DRIFT |

The twelve story commands were executed exactly as declared, including output
arguments. Every result has one failing assertion on `.config/dotnet-tools.json`,
zero measured tests, `runtimeQualified=false`, ACTIVE hold and four false
authority flags. Their result candidate is null because source verification
stops before candidate evaluation; the capture records the actual root revision.
All five runtime receipts and both final records remain absent.

Independent rehashing of every bound source finds seven mismatches among v1's
71 rows and one among v2's 81 rows. The complete rows are in
[source-bindings.json](../../module/validation/epic-16-published-artifact-qualification/entry-20261030-story161-01/source-bindings.json).
For v2, the root tool manifest expects
`5b826b67ce6520bba1fa80ef3982e56cc71b38d7ced437c0aaf5b1697339b411`
and measures
`f718f3ed50cc549069b785e38325a00af635f45d09a6926590e08d142a850c20`.
The other eighty v2 source rows still agree. Preserve accepted v1/v2 bytes;
overwriting their hashes would not qualify entry.

The [adoption delta](../../module/validation/epic-16-published-artifact-qualification/entry-20261030-story161-01/adoption-delta.json)
compares ordered human/JSON rows exactly and preserves multiplicity against
collection-05's earlier 22-row result. All sixteen previous failure rows remain
unchanged. Five new failures concern the retained 4.38.1 package's
`g4-host/bin/EventStoreHost/appsettings.json`, one in each collection-01 through
collection-05 isolated tool tree. No rows were removed. These are retained
configuration observations, not newly authored live bindings; do not erase,
exclude or amend them to obtain a passing scan. The current inventory has one
tool-pin, twelve dependency-selector and eight Dapr failures.

## Runtime code map after qualified entry

- EventStore Client `Subscriptions/EventStoreDomainEventProcessor.cs` receives
  exact raw envelope payload bytes; its context does not yet carry the required
  fingerprint. Reuse `Projections/IReadModelStore.cs` and `TrySaveAsync`,
  `ReadModelWritePolicy.cs` with three total attempts, applicable epoch
  coordination, and `Streams/IAuthoritativeEventStreamReader.cs`. EventStore
  must own the generic atomic outcome, pending-evidence/horizon, fenced replay
  and durable acknowledgement substrate. Existing primitives alone do not
  implement AD-6.
- Tenants Client `Projections/ITenantProjectionStore.cs` exposes unconditional
  Get/Save; `Registration/TenantServiceCollectionExtensions.cs` defaults to the
  in-memory singleton. `Handlers/TenantProjectionEventHandler.cs` serializes only
  within a process, skips older sequences and reapplies equal ones without
  authoritative fingerprint verification. Add coherent durable snapshots and
  pure transitions through EventStore; preserve existing signatures, clone
  semantics and case-sensitive member identities.
- Conversations `Server/TenantAccess/ConversationTenantAccessService.cs` still
  reads state and health separately. `DefaultConversationTenantProjectionSignal.cs`
  synthesizes Healthy for stores without a signal. Add the missing snapshot
  adapter and evaluate one persisted snapshot per invocation. Preserve shared
  Commons evaluator parity, safe denial behavior and the enrolled delivery
  configuration in `ConversationTenantAccessServiceCollectionExtensions.cs`.
- Extend owner Client subscription/projection tests and Conversations
  `Server.Tests/TenantAccess/`. Add the missing
  `IntegrationTests/Epic16/Story16_1EvidenceTests.cs` only after entry. Execute
  the frozen fault matrix with persisted end-state, native source/product
  captures, restart without redelivery and two-replica convergence. Keep the
  restart/single-replica warning until those proofs pass.

## Entry decision and validation

Architecture AD-5 still requires the accepted operational envelope. The
envelope remains DRAFT and `module/sm-2-checklist.md` remains pending. V2 retains
`entry: null` and its independent trust table is empty. Nine qualifications must
actually pass: `platformTool`, `adoption`, `operationalEnvelope`, `providerParity`,
`recoveryDrill`, `operatorCoverage`, `capacityWaiver`, `sm2`, and `ownerEntry`.
Entry also binds trusted bytes, an ancestor authorization revision and compatible
hashed `7.4`/`IR-0` predecessors. Current source drift fails first; missing owner
qualification remains a separate blocker rather than a measured PASS.

This is one cross-repository runtime goal; the draft proposes no deployment,
destructive migration or external data mutation. A separate current-tuple plan
would need its own approval before implementation, pin changes or a source-binding
successor. A chat choice cannot manufacture provider, human SM-2 or owner evidence.

Planning validation rehashes every captured command stream, checks the twelve
exact command arrays, nonempty failure ledgers and zero runtime counts, compares
both adoption formats and the complete delta, and verifies all protected inputs,
raw dependency identities, frozen intent, index and seven absent outputs.
Only this report, the refreshed runtime draft, a byte-preserved planning archive
and the fresh observation directory are changed. No AppHost was started because
no code changed. Native product tests, runtime evidence publication and historical
evidence-boundary replay are not-applicable to this planning refresh. FAIL and
BLOCKED continue to halt runtime review/completion.
