# Conversations Platform registration handoff — pending

## Current bounded tooling disposition — 2026-10-28

[The fresh source qualification attempt](validation/epic-16-tooling-qualification-v2/attempt-20261028-implementation-03/)
records the exact current root/dependency identities, mapped Debug readiness and cleanup,
525 passing producer source tests and nine passing action tests. The initial 180-second
adoption-class timeout is retained separately; its bounded retry passed all 396 tests.
Final current regressions also pass: 2,885 tooling, 1,516 generator, 131 adoption v1,
162 adoption v2 and 60 focused successor tests. Existing policy excludes 17 retired
live Story 11.3 cases; the earlier explicit-file FAIL is retained. The neutral helper
uses all seven producer-selected source environment values. Additive historical fixture
blobs restore old positive controls against unchanged accepted pins.

These tests qualify only their executed source scopes, including controlled package fixtures.
They provide no published-package, deployed-provider, production or human acceptance.

[Tooling v2](../_bmad-output/planning-artifacts/epic-16-tooling-v2/README.md) now supplies an
explicit held route. All six actual v2 Story 16.1 commands reach ENTRY_QUALIFICATION_PENDING
(BLOCKED/exit 2); v1 still fails SOURCE_DRIFT/exit 1 first on the current root tool manifest.
The attributes mismatch has a separate passing fault guard. Both actual routes measure zero
runtime tests and emit none of the seven runtime/final outputs.

Root CLI 4.38.1 and direct Platform 4.35.2 remain unchanged. The latest retained adoption
observation has six PASS/twelve FAIL, including the two archived generated-import findings.
Preserve every finding; new observations must report their exact actual inventory. No source
archive removal, scanner exemption, accepted fixture amendment or frozen host retirement is authorized.

The qualification ledger retains all thirteen release-review findings, ten deferred groups
and the original B8/B11 rejected dispositions. No preparation result closes the native launch,
timeout, process-discovery, forged-listing, list-cancellation, 67,001-byte claim-bound,
machine-crash durability, Windows/macOS native-host or batch-stale-binding gates. Owners must
qualify these against the actual accepted artifact/source/catalog tuple.

The exact producer-CI package, archive/hash/size/signature, NuSpec source/tag/CI identity,
packaged/loaded assemblies, loose/embedded schema/catalog and Platform release/source/Builds
pair remain pending, together with all 36 malformed-key controls on that artifact. Use only
isolated restoration for candidate qualification; local Debug sources cannot replace the
producer's required matched Release host package lane. The published testing-helper strategy,
source/package/deployed parity, operational envelope, recovery/operator/capacity disposition
and human SM-2 plus named owner entry remain separate BLOCKED inputs.

Entry remains null, independent trust empty, Story 16.1 draft/backlog and the hold ACTIVE;
all four authority flags remain false. The dated handoff below retains its original identities
and scope, including its historical offline exception and earlier pin/scanner observations.

## Earlier enrollment registration record

Static declaration/adoption validity does not register executables. This handoff
uses the direct Platform enrollment template at
`references/Hexalith.Platform/templates/domain-module-enrollment/platform-handoff.md`.
The accountable owners must review the actual registration, executable, image,
configuration and gateway bindings before ordinary CLI run/debug. No owner
acceptance, deployment, runtime entry or live parity is claimed here.

| Accountability and identity | Owner-recorded value |
| --- | --- |
| Status / outstanding registration work | pending; source prerequisite slice only |
| Module owner / repository / reviewed revision | Conversations maintainer, named signer pending; Hexalith.Conversations; baseline `dc5793db17fd6974200e80eb2b853187bc5f1669`, working-tree edits and untracked new sources |
| Platform owner / registration review reference | pending named signer and review reference |
| Direct Platform revision / tag / dirty state | `b117945e2ba7911fd2c47843f866487341300398`; accepted release/source pair and implementation registration pending; observation-time identities in `validation/epic-16-enrollment-remediation-2026-10-27/entry-preservation.json` |
| Builds catalog gitlink / accepted tool pin | `db897eac9f2819220b32d237109d7255218b46a9`; `hexalith.builds.module.cli` 4.35.2, roll-forward disabled; no pin/gitlink update |
| Implementation diff / composition-only regression evidence | current source diff; `validation/epic-16-enrollment-remediation-2026-10-27/`; synthetic mapped MSBuild and isolated SDK test stores |
| Registration acceptance by module owner / Platform owner / UTC date | pending / pending / pending |

| Module/server / appId / resourceId | Platform project binding / implementation reference | Image binding / catalog version / released declaration provenance | Accountable owner / review |
| --- | --- | --- | --- |
| `conversations/domain` / `conversation` / `conversations` | source executable `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` in the mapped Conversations root; actual Platform registration pending | image and catalog/release provenance pending; source project remains non-packable/non-publishable | Conversations and Platform owners; acceptance pending |

Every row below is scoped to `conversations/domain`. Platform supplies physical
components, local profiles, structured bindings and secrets; no physical name is
proposed or accepted here. The manifest's logical `state/state` recovery class
remains `authoritative-restore`; module projection inventories are derived and
remain `rebuild-only`.

| Full configuration key / module/server scope | Local profile / structured binding / secret reference | Authentication/signing propagation / token adapter and implementation | Owner / evidence |
| --- | --- | --- | --- |
| `EventStore:DomainService:AppId` | `eventstore-domainservice-appid`; resolve to declared appId | exact gateway target identity; pending installed binding | Platform / pending |
| `EventStore:CommandStatus:StateStoreName` | declared scoped state capability | SDK command-status store; provider identity pending | Platform / pending |
| `EventStore:Projections:ReadModelStateStoreName` | declared scoped state capability | immutable Conversations binding across SDK reads/writes/CAS/batches/recovery/native proofs | Conversations source tests; provider/live proof pending |
| `EventStore:Projections:CheckpointStateStoreName` | declared scoped state capability | SDK checkpoint store; provider identity pending | Platform / pending |
| `EventStore:DataProtection:StateStoreName` and `EventStore:DataProtection:PersistToStateStore` | scoped key-ring state and explicit boolean | SDK `AddEventStoreDataProtection`, stable application `Hexalith.Conversations`; unchanged cursor purpose `Hexalith.Conversations.QueryCursor.v1` | Platform and Conversations / persisted key-ring restart proof pending |
| `Tenants:PubSubName`, `Tenants:TopicName` | declared event capability and `tenants-topicname` reference | Tenants `configureOptions` delegate; protected `/tenants/events` POST | Tenants/Platform / delivery and provider parity pending |
| `EventStore:ProjectionChanges:PubSubName` | declared scoped event capability | EventStore SDK notifier; deployed routing pending | Platform/EventStore / pending |
| `Dapr:DeclarativeSubscriptions` | `dapr-declarative-subscriptions-true`, must resolve to true | SDK suppresses discovery and retains protected configured POST route | Platform / isolated route metadata proof only |
| `Authentication:JwtBearer:Authority`, `Authentication:JwtBearer:Issuer`, `Authentication:JwtBearer:Audience`, `Authentication:JwtBearer:ValidAudiences`, `Authentication:JwtBearer:AllowedAlgorithms`, `Authentication:JwtBearer:RequireHttpsMetadata`, `Authentication:JwtBearer:AllowInsecureSymmetricKey` | exact full keys in declaration; OIDC authority/JWKS egress pending | existing SDK bearer validation; no authentication-policy change | Identity/Platform / pending |
| `Authentication:JwtBearer:SigningKey` | secret reference `domain-jwt-signing-key` | existing SDK local/test adapter; production asymmetric signing/issuer/audience binding pending | Identity/Platform / pending |
| `Authentication:Workload:Audience`, `Authentication:Workload:AllowedCallers`, `Authentication:Workload:MaximumLifetimeSeconds` | declared workload configuration references | existing exact workload/operation authorization; gateway caller `eventstore` | Identity/Platform/EventStore / pending |
| `Authentication:WorkloadJwtBearer:Authority`, `Authentication:WorkloadJwtBearer:Issuer`, `Authentication:WorkloadJwtBearer:Audience`, `Authentication:WorkloadJwtBearer:ValidAudiences`, `Authentication:WorkloadJwtBearer:AllowedAlgorithms`, `Authentication:WorkloadJwtBearer:RequireHttpsMetadata` | exact full keys in declaration; workload OIDC metadata/JWKS egress pending | SDK signed workload assertion validation; issuer/audience/key propagation pending | Identity/Platform / pending |
| `APP_API_TOKEN` | secret reference `app-channel-token` | exact SDK sidecar application-channel token adapter; no alias; accepted CLI schema currently rejects this real key | Platform/Builds / pending accepted compatible tool and installed adapter evidence |

The authentication keys above match `hexalith.module-manifest.v2.json`; no alternate key or alias is introduced. All secrets, issuer/audience bindings and profiles remain owner
inputs. Tests explicitly selecting `PersistToStateStore=false` use SDK ephemeral
keys and confer no restart qualification. Owner acceptance requires true plus
persisted-key/restart evidence at the selected provider; composition inspection
alone cannot supply it.

| Tenant | Domain | Version | appId | Method | Gateway settings / preservation and filtering evidence / owner |
| --- | --- | --- | --- | --- | --- |
| pending tenant scope (fixture uses `*`) | `conversation` (current aggregate/projection domain) | pending catalog version (fixture uses `v1`) | `conversation` | `process` | exact source/package/installed tuple and selected-app preservation/filtering proof pending; EventStore + Platform owners |

The manifest's module ID is `conversations`; current aggregate/projection domain
is `conversation`. Query-only domain adapters also declare `conversations`.
Owners must review the full required tuple set rather than infer domain routing
from the module ID or copy a test tuple into production. Required protected POST
routes are the manifest's complete `invocationRules`, including
`/project/v2/reconcile` and all owner-proof/rebuild/recovery routes. Preserve the
existing workload policies, operation catalog, sidecar token and authorization.

| Dapr and acceptance evidence | Reference / owner / status |
| --- | --- |
| Capabilities / injected scoped components | declaration's four scoped state keys and two event capabilities; physical names/provider versions/backup policy pending Platform owner |
| Declarative subscriptions / delivery routes / dead-letter policy | declaration: `/tenants/events`, configured `Tenants:TopicName` (`system.tenants.events` in declaration), logical `tenants-events` and `tenants-dead-letter`; verify actual scoped pub/sub binding and dead-letter delivery/retention; pending |
| Deny-by-default receiver ACL / approved callers and verbs / mTLS | exact manifest caller `eventstore`, protected POST route catalog, SDK sidecar policy; deployment ACLs, token adapters and mTLS proof pending Platform/Identity |
| Static/model policy evidence | mapped source/Debug and package/Release reference evaluation are synthetic; isolated SDK store contents and host construction are source evidence only |
| Ordinary public CLI run/debug/down command and diagnostics | not run; approved offline pre-edit AppHost exception applies only to this prerequisite slice |
| Real-owner live readiness / policy acceptance | pending named owners; no runtime launched |
| Human SM-2 checklist / breakpoint / source edit / Story 3.11 | `sm-2-checklist.md` remains pending and unverified |
| Package/released-image acceptance | pending exact package hashes, versions, dependency graph, loaded image/assembly identities and release/source pair |
| Historical Dapr timeout disposition / new live evidence | unresolved / pending; no historical failure converted to PASS |

## Outstanding gates

Builds/Platform owners must qualify the exact published successor against the
full retained review inventory, including historical B6 structured-list
cancellation and platform-specific discovery cases. The isolated 4.38.0 artifact
fixes token-key format but is unqualified; later corrections in current Builds
source do not prove their inclusion in that artifact. Accepted 4.35.2 pins and
all twelve raw root gitlinks remain unchanged.

The ten excluded native findings remain failures, with exact identities in the
fresh native report and the approved remediation plan: one declaration/tool
failure, six retained source captures, the accepted MinimalModule fixture,
and two frozen host/direct-consumer project files. They need separate owner
scope; no scanner exemption, archive migration, fixture rewrite, dependency-only
freeze amendment, host retirement or dependency update is authorized here.
Both hosts, their topology/flags and frozen consumers remain intact.

| Requirement | Exact unresolved source identity |
| --- | --- |
| declaration | `module/hexalith.module-manifest.v2.json` |
| dependency-selectors | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/Directory.Build.props` |
| dependency-selectors | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/references/Hexalith.EventStore/Directory.Build.props` |
| dependency-selectors | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/references/Hexalith.Tenants/Directory.Build.props` |
| dependency-selectors | `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` |
| dependency-selectors | `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj` |
| dependency-selectors | `tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props` |
| dapr-subscriptions | `module/validation/epic-16-prerequisite-implementation/review-iteration-4/implementation/protocol-goldens-final/ConversationProjectionReadModelKeys.cs` |
| dapr-subscriptions | `module/validation/epic-16-prerequisite-implementation/review-iteration-4/implementation/protocol-goldens/ConversationProjectionReadModelKeys.cs` |
| dapr-subscriptions | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` |


Source/Debug, package/Release and deployed parity require the same accepted tool,
catalog/mapping hash, source revisions, package identities, runtime/provider
versions, configured capabilities, protected routes and observed behavior.
Builds.Tooling's public materializer is non-packable: source-only mapping tests
use its mapped project and evaluate package references from Debug. Existing
Contracts package inventory/metadata assertions remain active in package/Release;
no Release runtime or package parity is claimed by these tests. A published
producer testing-helper strategy remains qualification work.

**CI integration gate — pending; source status reconciled 2026-10-27:** at
`26298b31eb4d362f88e008246ac3acb160128330`, root `.github/workflows/ci.yml`
and current Builds `domain-ci.yml` invoke `Github/prepare-module-build` before
affected restores/builds. The producer action restores the exact accepted tool,
calls `prepare-build --mode package`, and exports verified mapping imports.
This source path was added after the prerequisite slice; it needs no duplicate
implementation. Accepted root/Platform CLI 4.35.2 lacks that command, and a fresh
invocation fails HXC001/exit 1. Owners must qualify a compatible published
successor and exact Platform/catalog/tool pairing before accepting integration.
No CI run, Release build, pin update or package fallback is claimed here.

The [current entry investigation](../_bmad-output/implementation-artifacts/story-16-1-entry-investigation-2026-10-27.md)
also records six actual Story 16.1 commands failing SOURCE_DRIFT/exit 1 on the
later committed `.gitattributes` change. The prior BLOCKED observations remain
unchanged; current failures precede the still-null entry check. An additive
source-binding refresh needs separate scope. Accepted evidence, current bound
sources, null entry, empty trust and runtime hold are preserved.

Architect/Runtime and Release must resolve the draft operational envelope:
local/CI/staging/production provider parity, backup/restore/rebuild and recovery
drills, targets/deadlines, named operators and recovery deputies with verified
access, resource/capacity limits, and any scoped, expiring Release-owned waiver.
Human breakpoint/edit, readiness and stop/cleanup evidence remains separately
pending. The in-memory tenant projection is still per-replica and lost on
restart; the SINGLE-REPLICA-ONLY warning remains. Durable tenant-projection
implementation and Story 16.1 lifecycle work remain outside this slice.

- [ ] Named module and Platform owners reviewed the exact implementation revision and every binding/adapter.
- [ ] The reviewed gateway tuple survives selection and disappears when its appId is unselected.
- [ ] All ten excluded failures and unsupported/new-source audit findings have separately approved dispositions.
- [ ] Ordinary CLI qualification, exact source/package/deployed parity and real-owner live/SM-2 evidence are accepted separately.
- [ ] Operational envelope, provider/recovery/operator/capacity gates and scoped owner entry are accepted.

Result: **pending**. Story 16.1 entry remains null, trust empty, draft/backlog and
`ACTIVE` hold unchanged. The latest six contract commands FAIL on source drift;
earlier BLOCKED results remain historical. No runtime
receipt/final pair or human acceptance is supplied by this source slice.
