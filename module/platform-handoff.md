# Conversations Platform registration handoff — pending

## Story 16.3 user-waived test fixture — 2026-11-15

The user authorized `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`
and its production-boundary test consumers for Story 16.3 only, while retaining
non-packable/non-publishable status. This is a **user waiver**, not named
baseline-owner/Architect/Runtime approval. Platform relocation and the
minimal-module fixture remain outside scope; revisit relocation when an
adopted Platform replacement exists. Earlier pending-owner records below retain
their historical meaning.

[Routine native evidence](validation/story-16-3-current-20261115/README.md)
retains the source-mapped Debug commands, native xUnit logs and source/runtime
identities. The entry ceremony and generated six-scenario final pair are
**waived**, never PASS. Strict/historical tooling, V14 evidence, the ACTIVE hold
and all authority flags remain unchanged; no provider parity, operational
qualification or release acceptance follows.

Shared preflight diagnostics are EventStore-owned. A healthy resource with an
unavailable endpoint reports `APPHOST_ENDPOINT_NOT_READY`, its resource/endpoint
identity and bounded retry advice, without retaining response bodies or request
URLs. The AppHost boundary waits up to 90 seconds. A collision reports
`DAPR_CONTROL_PLANE_PORT_COLLISION` and the owning effective binding/port. The
AppHost lane checks the Dapr sidecar ports its model pins (today the EventStore
sidecar's HTTP port 3501) and fails if none are pinned; the shared `dapr init`
placement/scheduler must already be listening and unpinned sidecar ports are
allocated free at start, so neither is checked. Release that binding or select
isolated ports, then retry within the budget. A missing selected sidecar
reports `DAPR_EXECUTABLE_NOT_FOUND`. Caller cancellation remains cancellation.

The dedicated native collection enrolls the real catalog and SDK finite
receiver, creates durable tenant membership through the protected HTTP
consumer, and uses the production coordinator and authenticated
`project/v2/reconcile` route. It observes pending/index and terminal scheduler
state before retry, and query-visible persisted state afterwards. A private
Redis instance owns all actor/catalog/read-model test state and is removed on
disposal, together with temporary components and listeners. The real SDK worker
transport receives HTTP 403 from a separate loopback refusal authority; it issues
no token and native-source admission remains unproven under the predecessor's
ungranted history boundary. Every recovery subject must independently reach the
selected injected store failure, so an unavailable authority cannot substitute
for terminal-failure evidence. Privileged resolution metadata and tenant-history
gap recovery are not qualified. Tenant-wide rebuild/erase remain deferred under
the predecessor decisions.

The separate default-Keycloak CLI baseline is blocked: the existing worker
client description is 300 characters, exceeding Keycloak's 255-character
DESCRIPTION column during realm import. All three health waits timed out
(exit 17); targeted stop passed and the final Aspire inventory matched entry.
The native AppHost lane supplies explicit test credentials with Keycloak
disabled. The realm configuration needs separately scoped owner correction.

Rollback removes only the new diagnostics, fixture hooks/tests and this native
evidence slice, restoring the prior test readiness loop if needed. Preserve
production routes, authentication, all 16.1/16.2 state and predecessor evidence.
No commit, publication or push is authorized by this story.

## 4.38.3 measured candidate and held v3 — 2026-10-31

[The fresh isolation/qualification attempt](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/README.md)
repairs Admin Web with an Aspire-managed HTTP endpoint. The complete mapped
Debug baseline, all three health waits and the discovered Admin Web `/health`
request pass. All required active resources are healthy; abstract sidecars and
unstarted rebuilders retain their native states. Eight focused topology tests
pass. Only the verified created instance was stopped. The first rejected stop
command and supported retry are retained; the final session inventory matches
entry, with the transient external Memories session recorded separately.

The [measured candidate ledger](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/collection-measured/qualification-ledger.json)
retains passing exact 4.38.3 signed artifact/source/schema/catalog identity,
producer-CI Release payload/native observations, both positive formats and all
36 malformed-key controls. Adoption is now **29 rows: 7 PASS / 22 FAIL**;
[ordered deltas and owner dispositions](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/collection-measured/adoption-dispositions.json)
retain every observation without waivers. The exact CI and Release run artifact
inventories are empty. All **55 declared qualificationEvidence files remain
BLOCKED**, independently of broad producer success.

[Held tooling v3](../_bmad-output/planning-artifacts/epic-16-tooling-v3/README.md)
binds current sources and the immutable measured candidate summary while
preserving v1/v2 carriers and every runtime/native obligation. The root tool
manifest was already 4.38.3 at entry and is unchanged; version agreement grants
no adoption. The [attempt ledger](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/qualification-ledger.json)
and [held-command ledger](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/held-command-ledger.json) retain the current verification disposition.

Builds owners must supply the exact 55 declared files with matching inventory
hashes/sizes or publish a separately reviewed evidence successor. Platform owners
must reconcile recorded Builds `cc0db51b800be1d0d5badc3780395cfb5953d0f5` with
prepared source `d1931b1e803144048d927fff16cb8119afde541e`, publish a qualified
Platform identity, and supply authenticated candidate acceptance with distinct
generated/direct-host qualification. Conversations/policy owners retain all
22 adoption failures. Package preparation/action export, native Windows/macOS,
provider/AD-5/recovery/operator/capacity, registration, human SM-2 and trusted
owner entry remain separate required work. No pin/gitlink selection was changed.
Story 16.1 stays draft/backlog, entry null, trust empty and hold ACTIVE with all
four flags false. Missing required producer evidence blocks completion of this
prerequisite. The exact 4,775-case broad regression command reached its
1,800-second bound (exit 143) before completion; its partial output and focused
current regression fallback remain separate evidence. Earlier dated observations retain their original scopes.

## 4.38.3 prerequisite resume: source prepared, baseline failed — 2026-10-31

[The fresh resume attempt](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-02/README.md)
successfully fetched exact Builds commit
`cc0db51b800be1d0d5badc3780395cfb5953d0f5` from its canonical remote, without
recursive fetching, dependency selection changes or nested initialization. The
commit and `Props/Directory.Packages.props` are available. The previous
local-object blocker is resolved; a new Debug producer build and source
preparation both exited 0. Prepared/recorded source, version-range and absent
Platform-tag warnings confer no candidate or tuple acceptance.

The isolated mapped Debug AppHost launched. Conversations, EventStore, security,
state store and pub/sub became healthy. Admin Web exited **134** after failing to
bind `http://127.0.0.1:5000`: the port was already occupied. The required
`aspire wait conversations-admin-web --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --status healthy --timeout 120 --non-interactive`
exited **18**. Its empty endpoint inventory and native logs are retained.

The [new ledger](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-02/qualification-ledger.json)
is **BLOCKED**, with a **FAIL** baseline gate. Resolve Admin Web endpoint/isolation
through separately reviewed Conversations hosting work or owner-managed
environment preparation, then rerun the full baseline in a fresh attempt.
Collector/v3 implementation and independent artifact/36-control qualification
remain incomplete. Platform's recorded Builds source still differs from its
prepared 4.38.3 source; that separate owner correction/acceptance remains pending.

The verified created AppHost was stopped successfully and pre-existing sessions
were preserved. Tool pins, gitlinks, checkout revisions, frozen intent and prior
evidence are unchanged. No runtime tests or runtime/native/final evidence was
produced. Story 16.1 remains draft/backlog, entry null, trust empty, hold ACTIVE
and all four authority flags false. Earlier dated observations below retain
their original scope.

## 4.38.3 prerequisite baseline blocked — 2026-10-31

[The fresh prerequisite attempt](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-01/README.md)
stopped before source edits or AppHost launch. Direct-dependency preflight and
the existing producer CLI Debug build passed, but actual source preparation
exited **2 / PrerequisiteUnavailable / HXW009**: Platform-recorded Builds
`cc0db51b800be1d0d5badc3780395cfb5953d0f5` is unavailable in the local Builds
object database, preventing the required mapped Debug baseline. This observation
does not establish whether the actual upstream revision contains the named file.

The [current ledger](validation/epic-16-published-artifact-qualification/attempt-20261031-4383-01/qualification-ledger.json)
is **BLOCKED**. Collector/v3 implementation, independent 4.38.3 artifact identity
and all 36 controls remain incomplete. Builds/Platform owners must resolve the
source-identity/local-object prerequisite within separately reviewed scope,
followed by a fresh mapped baseline before implementation. The measured root pin
was already 4.38.3 with roll-forward false; the historical planning pin was
4.38.2. This attempt preserved that manifest, gitlinks, accepted evidence and the
existing unrelated AppHost session. Pin agreement supplies no artifact acceptance.
No runtime tests or runtime/native/final outputs were produced; no instance was
created to clean up. Story 16.1 stays draft/backlog, entry null, trust empty,
hold ACTIVE and all four authority flags false. The dated observations below
retain their historical scopes and identities.

## Selected sequence and AppHost exemption proposal — 2026-10-30

The user selected prerequisite-first execution and pursuit of a narrow AppHost
test-fixture exemption. This selects direction; named owner acceptance remains
pending. Story 16.3 stays draft/backlog with runtime entry BLOCKED.

1. Resolve the measured Platform/tool/catalog disagreement and package-preparation
   blocker using an owner-accepted tuple. Resolve all sixteen retained adoption
   failures through owner-reviewed remedies; retain their evidence and identities.
   Pin or dependency changes require their own reviewed scope.
2. Complete the existing operational, provider, recovery/operator/capacity,
   registration, human SM-2 and trusted owner-entry qualifications. Complete and
   accept Story 16.1, then 16.2, before implementing Story 16.3.
3. Obtain the fixture exemption below before Story 16.3 entry. Reuse existing
   shared diagnostics/evidence tooling when implementing that story. Early
   diagnostics preparation and Platform relocation are deferred.

**Proposed exemption terms — baseline-owner and Architect/Runtime acceptance pending:**

- Exact subject: `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`
  and its existing production-boundary test consumers. The minimal-module
  fixture is outside this proposal; both inventory entries remain frozen.
- Purpose: local/CI verification of production hosts, authentication and durable
  reconciliation. Retain non-packable/non-publishable status. After qualified
  entry, limit fixture changes to the diagnostics, failure injection and recovery
  assertions required by Story 16.3. EventStore owns shared helpers; Platform owns
  deployment and environment policy.
- Required acceptance record: named baseline owner and Architect/Runtime, exact
  reviewed revision/subject, approval date and reference, and accepted scope.
  Until recorded and independently qualified, `appHostOwnership` stays pending.
  This proposal grants no runtime, provider-parity or capacity waiver.
- Revisit when Platform offers an adopted replacement harness with qualified
  source/package/hosted mappings. Migrate active consumers and satisfy the
  inventory's retirement gates before removing the fixture.

## Published 4.38.1 qualification observation — 2026-10-28

[The fresh artifact attempt](validation/epic-16-published-artifact-qualification/parent-review-20261028/README.md)
measures **4.38.1** from Builds tag/source
`16ff70a2321ad576faaac3fe53088c2f1bdacae3`. Its
[final ledger](validation/epic-16-published-artifact-qualification/attempt-20261028-implementation-01/collection-05/qualification-ledger.json)
is **FAIL**: adoption remains exactly **six PASS / sixteen FAIL**. The
[ordered comparison and dispositions](validation/epic-16-published-artifact-qualification/attempt-20261028-implementation-01/collection-05/adoption-dispositions.json)
preserve every entry failure, including all four newly scanned implementation-03
imports, with no additions, removals, reason changes or owner acceptance.

The reviewed collector passes 71 focused tests. Missing declared producer
qualificationEvidence files remain separately BLOCKED; publisher payload binding
and nonempty native summaries provide no acceptance of those missing files.

The actual signed feed archive, its signature/SHA-512/size, NuSpec source,
successful exact-source CI/Release, packaged/loaded Tooling identity, three
loose/embedded/source schemas and embedded catalog versus source Props pass.
Signature removal into a separate archive reproduces the qualified producer CI
archive's full SHA-256 and 37,548,049-byte size; every uncompressed payload entry
also agrees. Signed and unsigned ZIP identities remain distinct. Both ordinary
real-manifest formats and **all 36 malformed-key controls** pass with unchanged
inputs. The producer's Linux Release native results and skips, all 289 selected
fixtures, the hidden exclusion and raw/filtered byte differences remain explicit.

Actual published `prepare-build --mode package` returns **HXW011 / exit 2**:
direct Platform `5889400b50e6e2cefb4ed6976ca53d086d3989e2` has no local release
tag. Complete package preparation and action export therefore remain **BLOCKED**.
Platform's exact 4.35.2 pin and documented catalog source remain separate from
4.38.1's range eligibility and embedded catalog; no integration-qualified pair or
reviewed registration is accepted.

The available recorded Platform Builds gitlink
`81ab049208678bd6616828824729c0ddac02f7d1` also differs from its documented
accepted 4.35.2 catalog/tag source
`4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`. The ledger records that measured
identity disagreement as **FAIL/PLATFORM_CATALOG_BINDING_MISMATCH**, separately
from the missing Platform tag's BLOCKED gate.

All thirteen producer review findings and ten deferred groups are bound to the
actual artifact source; B8/B11 retain their rejected dispositions. Linux skips, broad source/native success and byte equality close none of the exact launch,
timeout, discovery, listing, claim-bound, crash-policy or batch gates. Native
Windows/macOS, supported published testing helpers, provider parity, AD-5,
recovery/operator/capacity, registration and human SM-2 remain owner inputs.

The fresh normal mapped Debug AppHost baseline was healthy and its exact isolated
instance was stopped. It supplies no runtime-entry credit. The twelve held
commands still produce v1 **FAIL/SOURCE_DRIFT** and v2
**BLOCKED/ENTRY_QUALIFICATION_PENDING**, zero runtime measurements and seven absent
outputs. Entry stays null, independent trust empty, Story 16.1 draft/backlog and
the hold ACTIVE with all four authority flags false. Pins, dependencies, accepted
bindings/fixtures and frozen hosts are preserved. The following records retain
their earlier observation-time claims and inventories.

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
| Declarative subscriptions / delivery routes / dead-letter policy | declaration: `/tenants/events`, configured `Tenants:TopicName` (`tenants.events` in declaration), logical `tenants-events` and `tenants-dead-letter`; verify actual scoped pub/sub binding and dead-letter delivery/retention; pending |
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
pending. Story 16.1 (2026-11-01) replaced the per-replica in-memory tenant
projection on the enrolled path with a durable checkpointed record per tenant
in the enrolled read-model state store. Native xUnit tests prove, against a
local Dapr/Redis state store with deliveries and Tenants history supplied in
process, that a restarted host and a second replica read the same persisted
state, health and checkpoint, and that gaps, lagging history and racing first
writes converge. They do not prove Platform pub/sub delivery, the gateway
history grant or deployment, and the AD-5 envelope stays waived.

Story 16.1 owner actions:

- [ ] Platform grants the Conversations workload read access to the retained
  Tenants history through the EventStore gateway. EventStore never admits the
  reserved `system` tenant as a request scope or a grant, so the host reads
  `api/v1/streams/read` under the explicit platform scope `platform:tenants`
  (`X-Hexalith-Scope` header, domain `tenants`): the token carries the gateway
  audience plus exactly one `eventstore:permission=tenants:read` and no
  `eventstore:tenant=system` grant. On the platform scope this permission also
  admits the raw `system`/`tenants` stream history of every tenant, so Platform
  grants it only to the tenant-access worker identity. The enrolled host refuses
  to start without `Hexalith:Conversations:TenantAccess:GatewayBaseAddress` and
  the `EventStore:Authentication` worker credentials (declared in the module
  manifest). The local AppHost injects them: with Keycloak enabled through the
  `conversations-tenant-access` realm client, which carries that permission; with
  Keycloak disabled through a development-only loopback authority, so the host
  starts but every tenant-history read fails and denies.
- [ ] When the opt-in deletion runtime (`Hexalith:Conversations:DeletionRuntime`)
  is also enabled, it and the tenant access share one SDK gateway client: one
  `EventStore:Authentication` worker identity and one gateway origin. Grant that
  single identity both the tenants-stream read above and the deletion runtime's
  own gateway access, and set
  `Hexalith:Conversations:DeletionRuntime:GatewayBaseAddress` to the same origin
  as `Hexalith:Conversations:TenantAccess:GatewayBaseAddress`; the host refuses
  to start when the two origins differ.
- [ ] The shared unkeyed gateway client also makes EventStore native-source
  projection admission attempt an authoritative Conversation-stream read for
  each native-proof dispatch. With only the `tenants:read` permission, that read
  is refused and admission remains unproven; each dispatch pays one refused
  gateway round trip (the worker token is cached for about 285 seconds).
  Granting this identity Conversation-stream read access turns native admission
  on, so approve that grant deliberately and account for the extra gateway load.
- [ ] Platform configures the tenants subscription to retry without limit and
  without dead-lettering, with a bounded exponential backoff between attempts so
  a failing delivery does not spin. The delivery that persists a gap attempts
  recovery, and each later delivery retries it. Operators monitor the
  subscription for a delivery that keeps failing and for growing lag; until
  that is in place a stuck tenant denies silently. Metrics and alerts for this
  remain deferred.
- [ ] Platform provides an enrolled read-model state store that supports ETag
  compare-and-swap with create-only first writes, uses one key prefix shared by
  all replicas, and never expires or evicts `tenants-projection:v1:*` keys. An
  evicted record denies its tenant until a later delivery recovers it.
- [ ] Operators rebuild the tenant projection records (`tenants-projection:v1:*`)
  after a state-store restore; a restored older Healthy record is not detected.
- [ ] Follow the release train. EventStore `v4.0.0` and later already ship
  the `platform:tenants` scope on the human `api/v1/streams/read` route and the
  gateway client's `X-Hexalith-Scope` header. The Tenants catalog SDK
  allow-list (`Directory.Build.targets`) already accepts the 4.0.0–4.0.4
  cohort, and canonical Tenants packages require EventStore 4.0.4. What
  remains: publish Tenants with the durable projection on EventStore 4.0.4,
  then bump the Conversations package versions in `Directory.Packages.props`.
  Until then Conversations CI cannot compile in package mode.

Story 16.1 rollback:

1. Before anything else, scale the Conversations host to exactly one replica,
   and keep it there while the durable path is off. Step 2 returns each
   replica to its own in-memory projection, which loses state on restart and
   cannot converge across replicas.
2. Remove the enrolled durable registration and its configuration: the
   `AddHexalithTenantsDurableProjection` opt-in that the enrolled
   `AddConversationTenantAccess` registers, the
   `Hexalith:Conversations:TenantAccess:GatewayBaseAddress` setting and the
   `EventStore:Authentication` worker credentials (unless the deletion runtime
   still uses them).
3. Keep or drop the `tenants-projection:v1:*` state keys. Nothing reads them
   while the durable path is off. Before re-enabling, drop or rebuild them:
   kept records answer from the state they held at rollback, as after a
   restore.
4. Remove the new module manifest inputs: the gateway address; the
   `EventStore:Authentication` keys and the worker client secret (unless the
   deletion runtime still uses them, as in step 2); and the identity need and
   egress entries.
5. Platform revokes the `eventstore:permission=tenants:read` grant from the
   worker identity, because it admits the raw `system`/`tenants` history of
   every tenant. If the deletion runtime keeps that identity, the identity
   keeps only that runtime's own gateway access. Locally, remove the
   `tenants:read` claim from the `conversations-tenant-access` realm client
   (`src/Hexalith.Conversations.AppHost/KeycloakRealms/hexalith-realm.json`).
6. Restore the single-replica warning that `1a4d5db8` removed, in
   `Program.cs`, this handoff and `module/README.md`. Change
   `_bmad/scripts/tests/test_story_16_1_warning_removal.py` with it, since
   that test keeps the warning out of all three files.

Rollback keeps every EventStore event and all recorded evidence.

- [ ] Named module and Platform owners reviewed the exact implementation revision and every binding/adapter.
- [ ] The reviewed gateway tuple survives selection and disappears when its appId is unselected.
- [ ] All ten excluded failures and unsupported/new-source audit findings have separately approved dispositions.
- [ ] Ordinary CLI qualification, exact source/package/deployed parity and real-owner live/SM-2 evidence are accepted separately.
- [ ] Operational envelope, provider/recovery/operator/capacity gates and scoped owner entry are accepted.

Result: **pending**. Story 16.1 entry remains null, trust empty, draft/backlog and
`ACTIVE` hold unchanged. The latest six contract commands FAIL on source drift;
earlier BLOCKED results remain historical. No runtime
receipt/final pair or human acceptance is supplied by this source slice.

## Story 16.2 read-state change — 2026-11-03

The tenant conversation index (`projection:conversations-index:{base64url(tenantId)}`)
is now the tenant's only Conversations lifecycle record. Schema version 2 holds
the lifecycle, per-conversation watermarks, entries and dispatch references in
one compare-and-swap value. The tenants `TenantCreated` delivery creates it
`Ready` and empty, create-only, before the Tenants projection handler runs. If
that write fails, the delivery fails and is retried. With in-order delivery the
tenant therefore stays unauthorized until its index exists.

Out of order, that guarantee does not hold. When a later tenant event arrives
before `TenantCreated`, the Tenants projection records the gap and recovers
`TenantCreated` from gateway history without the Conversations initializer, so
the tenant can become healthy with no index. Its list then reads `Unavailable`
until `TenantCreated` itself is delivered. If a conversation is delivered first,
the index becomes `Rebuilding` and stays so until the deferred tenant-wide
rebuild.

List reads map the record without writing:

| Index state for an authorized tenant | List result | REST list status |
| --- | --- | --- |
| version 2 `Ready`, no conversations, no pending dispatch | `Current` / `current`, empty page | 200 |
| version 1 or version 2 `Rebuilding` | `Rebuilding` / `rebuilding`, proven rows still returned | 200 |
| lifecycle or source-position gap in a `Ready` index | `Rebuilding` / `gap_detected` | 200 |
| missing index, or state-store failure | `Unavailable` / `unavailable` | 503 |
| duplicated entries or contradictory lifecycle record | `Unavailable` / `metadata_contradictory` | 503 |

A missing or contradictory index now returns HTTP 503 from the REST list, where
it returned 200 before (an empty current page, or `Rebuilding` for duplicates).
Unauthorized and nonexistent tenants still read `Forbidden` before any index
read. Detail reads are unchanged.

Replay takes identity and time only from validated event inputs. A missing
identity or time, an identity reused with a different binding, or a generation
instant earlier than the replay anchor maps to `Unavailable` /
`metadata_contradictory`. For a generation earlier than its anchor, or a replay without an anchor,
the response preserves the observation time and reports null lag. Its public
last-applied marker is the Unix epoch for observations on or after the epoch,
or `0001-01-01T00:00:00.0000001Z` for earlier valid observations. The named
projection route never persists that failure as a completed projection.

Tenants created before Story 16.2 have a version 1 index or none. Their next
conversation delivery rewrites a version 1 index, or creates a missing one, as
version 2 `Rebuilding`; until then a missing index reads `Unavailable`. These
tenants never become `Ready`: a redelivered `TenantCreated` leaves an existing
index unchanged, and a per-conversation rebuild cannot prove tenant-wide
coverage. Adopting the EventStore tenant shared rebuild is a follow-up story;
until it lands these states are truthful. Do not hand-create or edit index
records.

Story 16.2 owner actions:

- [ ] Platform keeps `projection:conversations-index:*` keys under the same
  no-expiry, no-eviction, create-only-capable state-store terms as the tenant
  projection records. An evicted or lost index makes its tenant list
  `Unavailable` until the follow-up rebuild exists, unless `TenantCreated` is
  delivered again first. The host keeps delivery markers in memory, so a
  redelivery after a restart recreates the lost index as `Ready(empty)`: the
  list then reads `Current` and empty while the tenant's conversation details
  remain. No index loss is recoverable before that rebuild.
- [ ] The tenants subscription's unlimited retry, without dead-lettering (Story
  16.1), now also covers index initialization.

Story 16.2 rollback: restore the previous Server build. Version 1 readers ignore
the version 2 fields and read a missing index as an empty current tenant again
(limitation D3). Keep the index keys; the restored writer rewrites them as
version 1 on its next delivery. On roll-forward, indexes the old writer rewrote
as version 1 read `Rebuilding`, and tenants created during the rollback window
have no index and read `Unavailable` until the follow-up rebuild.

## Pilot API source composition

The source deployment consists of `Hexalith.Conversations.Api` (Platform resource and Dapr app ID `conversations-api`), the internal `conversation` domain app, EventStore and the Tenants subscription. The API forwards the human bearer through its Dapr sidecar. EventStore submits commands to domain `conversation` and queries to `conversations`. The domain owns both write admission and the persisted read model. Source enrollment and the installed-package checks below describe the candidate. Release qualification is pending.

For a Platform source AppHost, select `Platform:ActiveModule=conversations` and its owner manifest. Supply the domain operator values through `Platform:Conversations:Domain:<key>` in AppHost configuration. Use a protected configuration provider for the history client secret. The binding value for `Conversations:Pilot:HumanPartyBindings` is a JSON array with one exact `TenantId`, `PrincipalId` and `PartyId` per enrolled human. For example, one entry has the shape `[{"TenantId":"pilot-a","PrincipalId":"reader-a","PartyId":"party-a"}]`. The workload verifier uses `Authentication:WorkloadJwtBearer:Authority` and `Issuer`. The history reader uses the four `EventStore:Authentication` settings `Authority`, `TokenEndpoint`, `ClientId` and `ClientSecret` with the `client_credentials` grant. Its authority must allow reading Tenants history in `platform:tenants`. The API uses the same human JWT issuer and audience as the gateway. Keep the domain and its state sidecar private.

The source profile binds `Authentication:QueryAuthority:RequiredQueries:conversations` on the gateway to include both `conversation-detail` and `conversation-list`. The gateway's workload issuer must be able to obtain a resource-bound `domain-service:query` assertion for the selected tenant and Conversations domain. The API forwards the authenticated human bearer to the gateway. Its Dapr grants cover POST on the human command and query actions at `/api/v1/commands/submit` and `/api/v1/queries/submit`, and GET on a single message ID under `/api/v1/commands/status/`. The submission actions use separate leaf paths because Dapr resolves a populated parent rule before its children. Only the internal domain service consumes the operator's Party bindings and Tenants history credentials. The read-model store and its Dapr credentials belong to that internal workload.

A detail request without the gateway caller binding is denied before any projection read. A bound global administrator requesting ordinary detail still needs current tenant Read access. Bounded and delegated detail retain their restricted caller requirements.

Configure the EventStore gateway with `EventStore:CommandStatus:TtlSeconds` of at least `90000` and a status store that honors it. Mutation receipts remain replayable for 24 hours from finalization. The extra hour accounts for the interval between the status write and receipt finalization. Also set `EventStore:Idempotency:TerminalRetentionSeconds` to at least the status TTL. EventStore checks on startup that this terminal retention, 86,400 seconds by default, is not shorter than the status TTL, so a gateway configured with the status TTL alone does not start. The local qualification AppHost sets both values to `90000`. The full runner requires a status TTL of at least `90000` and a terminal retention of at least that TTL in the gateway resource environment that the AppHost reports.

Keep the API and domain host clocks synchronized. Domain admission accepts an API stamp up to one minute ahead of its clock, the bearer `ClockSkew` that EventStore's `JwtBearerAuthenticationContract` configures. A larger forward skew is rejected as an invalid owner allocation, and the API returns `command_validation_failed`. The aggregate uses the domain clock when it executes an admitted create or append. An append retains the conversation's latest event time if that is later, so committed occurrences stay in stream order. An existing caller-stamped Agent or governance event can still supply that later time.

For an isolated Platform source deployment with a separately managed OIDC issuer, configure the gateway, Tenants and Conversations domain workload verifiers with the issuer, each receiver's own audience and the issuer's signing algorithm. Install the issuer trust chain. On the gateway and Tenants, set `Authentication:WorkloadIssuer:TokenEndpoint`, `Workload`, `ClientId`, the protected `ClientSecret`, and `EnableResourceBindingRequests=true`. The issuer must grant only the operations and resource bindings each client needs. A fresh installation uses the Tenants owner's authenticated administrator bootstrap and its gateway binding. Once the administrator is available, use the Tenants commands to create two tenants and assign a contributor in one and a reader in the other. Wait for both membership events on `tenants.events` to reach the Conversations domain before starting the package consumer.

To prepare a local feed, use the producer's package mapping and Release configuration. Set `PACKAGE_FEED` to an empty directory outside the source tree and `PACKAGE_VERSION` to a fresh prerelease version. Run from the Conversations root.

```bash
dotnet pack src/Hexalith.Conversations.Contracts -c Release -p:Version="$PACKAGE_VERSION" -p:PackageVersion="$PACKAGE_VERSION" -o "$PACKAGE_FEED"
dotnet pack src/Hexalith.Conversations.Client -c Release -p:Version="$PACKAGE_VERSION" -p:PackageVersion="$PACKAGE_VERSION" -o "$PACKAGE_FEED"
python3 tests/packaged-client-journey/run.py --feed "$PACKAGE_FEED" --version "$PACKAGE_VERSION" --build-only
```

The versioned `tests/packaged-client-journey/platform-apphost.cs.template` hosts the local qualification graph using the ordinary Platform composition API. Export `HEXALITH_SOURCE_MAPPING_PATH` and `HEXALITH_SOURCE_MAPPING_HASH` for the prepared Platform source mapping. Export `CONVERSATIONS_SOURCE_ROOT` and `EVENTSTORE_SOURCE_ROOT` as the two reviewed checkout paths selected by that mapping. The AppHost verifies both roots, reads the Conversations manifest from its mapped checkout, and checks that it composes `conversations-api` before starting. [Platform AppHost source mapping](https://github.com/Hexalith/Hexalith.Platform/blob/66b64388c78ceba7b744a1092abc221b75d2bb8d/docs/platform-apphost-form.md)

Supply a protected flat JSON dictionary through `CONVERSATIONS_OPERATOR_BINDINGS` using the owner/server/configuration-key names in Platform's local profile. The ordinary source profile shares its human JWT settings across the gateway and API. An operator issuing the human tokens supplies the corresponding human signing and verification settings. For fresh Tenants startup, include `tenants/domain/Tenants:BootstrapGlobalAdminEnabled` and `tenants/domain/Tenants:BootstrapGlobalAdminUserId`. Set the domain history and Party settings described above. Set `CONVERSATIONS_WORKLOAD_ISSUER` to the HTTPS Keycloak realm, `CONVERSATIONS_OIDC_CA_BUNDLE` to its trusted CA bundle, and `CONVERSATIONS_GATEWAY_OIDC_SECRET_FILE` and `CONVERSATIONS_TENANTS_OIDC_SECRET_FILE` to the protected credentials for the `eventstore` and `tenants` clients. Named accounts and issuer grants belong to the local operator.

In the disposable Platform checkout, copy the qualification template to the file-based AppHost entry point.

```bash
export PLATFORM_APPHOST="$PLATFORM_ROOT/apphost.cs"
cp "$CONVERSATIONS_SOURCE_ROOT/tests/packaged-client-journey/platform-apphost.cs.template" "$PLATFORM_APPHOST"
aspire start --isolated --apphost "$PLATFORM_APPHOST"
```

For the fresh local verification lane, set `CONVERSATIONS_GATEWAY_ORIGIN` to the gateway's loopback origin, `CONVERSATIONS_CATALOG_RECEIPT` to its run-owned `eventstore.committed.json`, and `CONVERSATIONS_ADMIN_TOKEN` to an operator bearer authorized to create tenants, add members and read their command status. Export the two human identities and the domain sidecar inputs from the integration guide. Use disposable tenant names. Run these commands after the source AppHost reports Healthy and the gateway commits its routing catalog.

```bash
python3 tests/packaged-client-journey/provision.py
python3 tests/packaged-client-journey/run.py --feed "$PACKAGE_FEED" --version "$PACKAGE_VERSION" \
    --apphost "$PLATFORM_APPHOST" --fault-api-dll "$CONVERSATIONS_API_DLL"
```

Full qualification checks the reader's exact tenant, principal and Party binding in the running domain's Aspire configuration and checks the reader's healthy durable membership before the denied create. Use the selected AppHost's public API URL and domain sidecar port for the journey. The runner launches a second copy of the API on loopback to interrupt the status read for the message ID in the gateway receipt. The consumer verifies the stored append before retrying it at the regular API and checks that the retry leaves one message. The CLI uses the local Aspire resource to discover the API's sidecar and human bearer configuration. `PLATFORM_APPHOST` names that running AppHost file and `CONVERSATIONS_API_DLL` is the built `Hexalith.Conversations.Api.dll`. To run partial live checks, use `--without-status-outage` in place of both arguments.

For a fresh source deployment, use the Conversations story commit and the Platform and EventStore commits recorded in its root gitlinks. The following command prints both required owner revisions from the Conversations checkout.

```bash
git ls-tree HEAD references/Hexalith.Platform references/Hexalith.EventStore
```

The local source bundle provides an alternative for an operator who has the three base revisions. Extract it, check out each base identified in `candidate.json` in its own disposable checkout, then run `git apply --check` and `git apply` with that repository's patch. Prepare the producer-mapped Platform checkout with all three resulting source trees before building the packages and starting the AppHost.

When verification ends, stop the local AppHost with `aspire stop --apphost "$PLATFORM_APPHOST"`. Shut down the separately managed disposable issuer and discard its local credentials.
