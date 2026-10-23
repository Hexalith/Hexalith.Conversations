# Conversations Platform enrollment preparation

This declaration prepares owner review. It changes no server behavior and grants
no runtime entry, approval, deployment, host retirement, release or push authority.
The Epic 16 implementation hold remains ACTIVE. The existing single-replica
warning, Story 16.1 draft status, sprint state and historical evidence remain
unchanged by this preparation. Pre-existing edits to the draft are preserved.

The accepted direct Platform tool is `hexalith.builds.module.cli` **4.35.1**, with
roll-forward disabled. Its restored identity was
`4.35.1+f8599224457dda99e7c0ae4870147dd51b464feb`. The root tool manifest copies
only that accepted entry. No dependency or gitlink was updated.

## Declaration and runtime ownership

`hexalith.module-manifest.v2.json` declares the Conversations domain server:
module `conversations`, server `domain`, Dapr app ID `conversation`, Platform
resource `conversations`, readiness `/ready`, and direct server dependencies
`eventstore`, `tenants`, `memories`. The root already declares those repositories
along with Platform, Builds and McpCli. The declaration uses logical `state` and
`events` roles and injected configuration keys. Shared `state/state` component
classification remains `authoritative-restore`; the Conversations and Tenants
projection inventories remain derived (`rebuild-only`). Recovery hooks and
qualified restore/rebuild demonstrations are pending; none are invented here.

The protected HTTP surface admits the `eventstore` workload for the exact SDK
operational POST route catalog, including `/project/v2/reconcile`. Operation
policies match `EventStoreDomainServicePolicies` and
`EventStoreWorkloadOperations`. The SDK requires both the receiver's Dapr app
channel token (`APP_API_TOKEN`, matching `DaprAppChannelToken.ConfigurationKey`)
and a valid short-lived workload JWT with the receiving audience,
caller and operation grants. The declaration does not turn caller inventory,
ACLs or transport identity into authorization. Platform must supply the
executable/image and gateway mappings, workload/JWT configuration, injected
state bindings, ACL/mTLS configuration, and private credentials. No secrets are
stored here. Dedicated `Authentication:WorkloadJwtBearer:Authority` discovery
and JWKS egress are declared independently of the public JWT issuer. Probe paths
remain SDK-owned.

The Tenants feed is declarative at `/tenants/events`, with logical
`tenants-events` and `tenants-dead-letter` topics. Its physical feed topic is
`system.tenants.events`, following Platform's enrolled Parties pattern;
Platform must inject the matching `Tenants:PubSubName` and `Tenants:TopicName`.
The current `AddHexalithTenants()` call does not bind either configuration key:
it retains SDK defaults. Adoption must pass the SDK's `configureOptions` delegate
and assign `options.PubSubName` and `options.TopicName` from those injected keys.
Unsupported `Tenants:Enabled`, `Tenants:ServiceName` and `Tenants:CommandApiAppId`
knobs have no current consumer and are not declared.

The declaration also names `Dapr:DeclarativeSubscriptions` through the
`dapr-declarative-subscriptions-true` configuration reference. Platform must
resolve this input to **true**. In the current SDK that exact boolean switch
suppresses `/dapr/subscribe` discovery while retaining the configured event
POST route. This preparation sets no runtime configuration. Future qualification
must prove protected `/tenants/events` delivery at the injected pub/sub/topic,
dead-letter routing and absent discovery, plus the production reconciliation
route; source declarations alone do not prove those effects.

Data Protection is another explicit adoption blocker. Injecting
`EventStore:DataProtection:PersistToStateStore` and its state-store key does not
call `AddEventStoreDataProtection`. Conversations currently uses its existing
Data Protection registration. Future adoption must invoke the SDK registration
with `IConfiguration` and a stable application name, then qualify persisted key
ring/restart behavior. Tenants delegate binding, SDK Data Protection registration
and declarative-subscription route qualification all remain **BLOCKED**, separate
from static scanner findings. No runtime source is changed here.

## Actual static validation

Run from the Conversations Git root:

```bash
dotnet tool restore
dotnet tool run hexalith-module --version
dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json
dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption
dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption --output json
python3 _bmad/scripts/verify_epic_16_adoption.py --repository .
```

Restore and version checks exited **0**. Ordinary declaration validation and
both adoption modes exited **1** (`UsageOrManifest`). The correct SDK
`APP_API_TOKEN` key is rejected by accepted 4.35.1 with HXP002 at
`modules[0].integration.secrets[1].configurationKey` and
`modules[0].runtime.extensions[0].configurationInputs[12].key`: its key format
permits alphanumerics and colon segments, but no underscore. Fixing that
upstream accepted schema/tool compatibility is **BLOCKED**; no alias or tool-pin
change is introduced. Exact command, exit and output
bindings are in [validation/results.json](validation/results.json); the complete
[readable report](validation/adoption-text.txt) and [JSON report](validation/adoption.json)
retain all 21 rows: six PASS and fifteen FAIL (the declaration plus fourteen
source findings). The native ordinary/adoption failures remain failures.

Eleven dependency-selector failures identify `Directory.Build.props`, the
AppHost, Client, Contracts, Server and Domain project files, and AppHost,
Contracts, Integration, Server and Domain test project files. Three
`dapr-subscriptions` failures identify:

- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs`
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs`
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs`

Every exact path and reason remains in the reports. The tool pin,
workspace, direct references, gitlinks, absent nested references and hosting
inventory passed the accepted tool's bounded static checks. Those rows establish
no runtime proof. The accepted 4.35.1 scanner has documented false-positive
limits; no failure is waived or converted to PASS here.

The scanner reads tracked files. During this preparation the additive files are
untracked because staging is prohibited. No source was hidden, renamed, removed,
or excluded to change scanning. The owner must rerun both adoption modes against
the intended reviewed/tracked candidate and review unsupported/generated
constructs. Stored reports are a working-tree snapshot, not qualification of a
later checkout or certification of all new Python files.

## Remaining work and owner procedure

Review the additive [Epic 16 adoption package](../_bmad-output/planning-artifacts/epic-16-adoption-v1/README.md)
and its read-only checker. A structural PASS has `entryReadiness=BLOCKED` and
four false authority flags. Retained scanner FAIL rows are entry blockers;
they do not make the package structurally inconsistent merely by being recorded.

1. Conversations and Platform owners review the declaration against source,
   agree executable/image/gateway mappings, physical topic injection and scoped
   state/configuration/identity inputs. Review each scanner failure. A separate
   migration must remove dependency-root discovery and module-owned mode/root
   assignments, consume tool mappings, inject literal Dapr names and replace
   SDK subscription discovery through producer-supported declarative hosting.
   Preserve public contracts, receiver authorization and durable semantics.
2. Qualify source/Debug, package/Release and hosted operation with exact runtime,
   provider, catalog, tool and mapping identities. Establish the AD-5 operational
   envelope, operator/recovery-deputy coverage, recovery targets/drills, resource
   bounds and any Release-owned scoped/expiring waiver. No run/debug/deploy
   commands were executed for this preparation.
3. Complete [sm-2-checklist.md](sm-2-checklist.md) through observed human evidence:
   real active-checkout breakpoint and edit, direct-only dependency checks,
   readiness, stop/cleanup and Story 3.11 owner acceptance. It remains unverified.
4. Adopt the Epic 16 successor schema/generator and measured scenario verifiers;
   all are explicitly unavailable for execution today. Keep all eighteen
   scenarios, thirty-two requirements, graph edges and rollback/final-record
   obligations. Release with Architecture, Quality and Planning Tooling must
   record a separate entry decision after prerequisites pass.
5. Keep both AppHosts in [hosting-inventory.json](hosting-inventory.json) frozen.
   The inventory names tracked consumers, including solution/test/CI and fixture
   tooling; immutable evidence also records these paths as historical references.
   `DomainModuleAuthoringTemplateValidationTest.cs` actively reads both hosts;
   `check_package_refresh_environment.py` actively reads the main host.
   `PackageEnvironmentAuthorityV18ValidationTest.cs` is a historical Git-blob
   reference, not an active filesystem dependency.
   No module-owned Aspire or ServiceDefaults production project exists. Neither
   AppHost is packable or publishable. Baseline owner with Architect/Runtime must
   explicitly decide exemption or relocation before Story 16.3. Retirement also
   requires producer adoption, owner-reviewed source/package/deployed parity,
   consumer migration and applicable EventStore AD-22 exact-subject removal
   authority (or an explicit owner-reviewed not-applicable disposition).

Scoped evidence status is **not-applicable to runtime publication**: preparation
publishes no runtime receipts or final records. Historical authority publishers
and the Story 16.1 lifecycle are not resumed by this package.
