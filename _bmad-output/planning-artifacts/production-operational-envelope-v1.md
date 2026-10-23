# Production operational envelope v1

**Status: DRAFT for owner review — 2026-10-22.** Preparation for Story 16.1;
no deployment, operational approval, readiness claim, or hold lift. Facts below
come from inspected source and retained evidence, not a fresh runtime execution.
The user selected **Hexalith.Platform** as the infrastructure owner. Its hosting,
provider, identity and operational policies are inherited; their runtime
qualification remains separate. This readable Markdown does
not claim acceptance by the historical JSON publisher. Current change handling
follows `docs/runbooks/current-change-validation.md`.

## Observed environments and parity gaps

| Environment | Established facts | Unproven boundary |
| --- | --- | --- |
| Local harness | `ConversationsAppHostTopology` composes EventStore, Conversations Server, and Admin Web; the server resource is `conversations`, Dapr app ID `conversation`. Optional Keycloak supplies local identity. The AppHost is non-publishable. Shared `statestore` and `pubsub` use external dapr-init Redis at `localhost:6379`. | No Tenants service is composed. Default tenant access uses process-local `InMemoryTenantProjectionStore`; restart loses acknowledged history and replicas do not share it. |
| CI | `.github/workflows/ci.yml` delegates domain build, integration, and mandatory Aspire lanes to Builds. The inspected Builds workflow defaults to Dapr CLI `1.18.0`, runtime `1.18.2`, full `dapr init`, and Release builds. | The delegation uses `@main`; these source defaults are not an immutable receipt of an executed CI run. Live fixtures do not establish production provider parity. |
| Staging | Platform defines isolated staging on `hexalith.com`, sharing the designated Kubernetes installation with production. | Conversations enrollment, environment bindings and qualification evidence remain absent. |
| Production | Platform defines `tache.ai`, separate environment state, credentials, namespaces and Keycloak realm, deployed through its retained Helm package. | Platform's current declaration composition refuses non-Development hosts; local tests establish no hosted qualification. |

The gateway live fixture uses one host/app ID and real Dapr/Redis persistence,
with supplied tenant access and command authority. The retained AppHost test
disables Keycloak and configures test signing authority. Historical projection
evidence also discloses synthetic tenant-event seeding. These prove bounded
boundaries, not production Tenants/identity integration. The live fixture fails
when Redis, placement, or scheduler prerequisites are unavailable.

EventStore's separate disposable broker profile pins runtime `1.18.7`, CLI
`1.18.2`, and components `1.18.6`, but declares `productionEligible=false`, no
selected broker, and no HA/capacity/restore claim. Its version difference from
CI remains unresolved parity work.

## Hexalith.Platform infrastructure decision

**User decision:** “stick to Hexalith.Platform”. Conversations enrolls through
Platform's v2 declaration and accepted module tool. Platform assigns physical
component names, providers, app scopes, identity, telemetry and environment
bindings. Conversations consumes injected configuration through EventStore
`IReadModelStore`; it does not choose an independent Redis deployment.

Follow the existing Parties pattern: declare logical `state/state` and inject
readmodel/checkpoint configuration, plus logical `events/pubsub` and an explicit
Tenants subscription. The shared state component requires matching
`authoritative-restore` capability classification; `rebuild-only` on that same
component conflicts with EventStore/Tenants. This component-level classification
does not make the tenant projection authoritative: its module recovery inventory
still identifies derived state and the required rebuild/integrity procedure.
Use existing capabilities where sufficient; any distinct isolation/classification
binding belongs to Platform. Do not hardcode physical names, edit gateway-only
templates, or bypass declared component scopes.

Existing EventStore PostgreSQL/Cosmos production templates admit only
`eventstore` and `eventstore-admin`; Platform's current Works components exclude
`conversation` and `tenants.events`. None is an already configured production
Conversations store/feed. Platform's declaration renderer, rather than those
static preview components, owns new enrollment. Qualify insert/CAS conflicts, durable checkpoint and
gap evidence, namespace isolation, restart, replica convergence, fenced recovery,
and provider failure against the Platform-selected profile. The enrolled Parties
feed maps logical `tenants-events` to physical `system.tenants.events`, whereas
the client defaults to `tenants.events`; Conversations must consume its injected
topic/component and explicit `/tenants/events` subscription/dead-letter policy.
Platform's EventStore-ratified hosted profile owns broker qualification and
retention/redelivery, workload identity, topic scopes and transport protection.
Local Redis pub/sub is not the qualified hosted broker.

| Responsibility | Architectural owner / proposed operational assignment |
| --- | --- |
| Envelope and environment parity | Architect/Runtime owns AD-5 and consumes Platform's profile/qualification records. |
| Providers, Dapr, secrets, identity, backups | Hexalith.Platform owns infrastructure and deployment bindings. Hosted secrets use environment OpenBao through Dapr; separate Keycloak realms govern environment identity. |
| Releases, admission and recovery | Platform's Administrator owns release approval, stop clearance and production admission. The named recovery deputy receives notifications and performs documented recovery; production admission is never inferred from restoration. |
| Authoritative events and generic recovery | EventStore supplies durability, CAS/replay/lease/acknowledgement substrate. |
| Tenant transitions and checkpoints | Tenants owns schema and pure lifecycle/membership/role transitions. |
| Admission and safe diagnostics | Conversations consumes coherent state/health snapshots and denies unsafe state. It never clears recovery on reads. |
| Qualification and waivers | Quality verifies bound evidence; Release owner alone accepts capacity/parity waivers. No approval is implied here. |

## Recovery and operator procedure proposal

1. **Detect:** distinguish store/sidecar outage, missing state, stale/gapped state,
   corruption, retry exhaustion, and lost recovery lease using bounded diagnostics.
2. **Contain:** retain denial for every non-Healthy tenant snapshot; isolate
   affected workloads and preserve checkpoint/evidence. Never bypass admission.
3. **Backup/restore:** Platform selects native backup/PITR and tests an isolated
   restore. Preserve authoritative events and coupled metadata; EventStore's
   source-guard cutover runbook prohibits selective checkpoint/model rollback.
4. **Rebuild:** EventStore schedules authoritative replay under a fenced lease;
   Tenants applies contiguous transitions and validates observed fingerprints.
   Only a successful final CAS may restore Healthy. No query-triggered repair.
5. **Verify/escalate:** compare persisted checkpoints, pending evidence, decisions,
   and replica/restart outcomes; confirm tenant isolation before restoring traffic.
   Escalate platform failures to Platform and transition conflicts to Tenants.

Inherit Platform NFR-2 targets: **RPO at most one hour continuously** and **RTO at
most four hours within declared response coverage**. Backups start every 30
minutes; retain frequent points for seven days and daily points for 30 days.
Recovery-point monitoring runs at least every 15 minutes. An isolated restore
drill runs before G2, monthly thereafter and after material recovery changes.
These are Platform targets, expressly not demonstrated guarantees. Platform
publishes response coverage, proves deputy access and prepares replacement
capacity. Conversations supplies its state inventory and integrity/rebuild hooks;
it does not invent separate recovery targets or relax AD-6 authorization safety.

## Health, telemetry, capacity, and waiver review

The SDK exposes `/alive`, `/ready`, and `/health` and configures OpenTelemetry;
OTLP export depends on configuration. A process probe does not prove a Healthy
tenant projection. Conversations declares bounded tenant-denial, projection
freshness/rebuild, and publication counters; collector wiring, dashboards,
alerts, retention, and on-call delivery still need Platform environment proof.
Platform configures the telemetry sink/exporters/egress. Its private GitHub issue
notifications reach Administrator and the recovery deputy. Exclude
payloads, credentials, principal/tenant identifiers, and unbounded metric labels.

Current evidence supports the disclosed single-replica constraint, with tenant
state loss on restart. It establishes no production throughput ceiling.
Platform's declaration defaults to one replica. Inherit its environment resource
limits and qualify partition/evidence bounds, concurrency and backpressure with
the enrolled workload. Two-replica convergence alone is not capacity
qualification; Platform promises no HA, 24/7 coverage or throughput guarantee.

Review decisions required before acceptance:

- Complete owner-reviewed Platform enrollment: module manifest, hosting
  inventory, source/package mapping, scoped configuration, Tenants feed and ACLs.
- Bind the accepted Platform tool/catalog and runtime profile; qualify actual
  environment secrets, identity and provider semantics.
- Prove Platform recovery targets, response coverage, restore and telemetry for
  the enrolled state inventory; qualify declared resource limits.
- Record Release-owner waiver disposition. No waiver is granted. Any proposed
  waiver must bind scope, evidence, rationale, and an explicit expiry; expiry
  requires renewed disposition, never automatic continuation.

## Sources

- `src/Hexalith.Conversations.AppHost/{ConversationsAppHostTopology.cs,Hexalith.Conversations.AppHost.csproj}`
- `src/Hexalith.Conversations.Server/{Program.cs,Diagnostics/ConversationTelemetryDefinitions.cs}`
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs`
- `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs`
- `.github/workflows/ci.yml`; `references/Hexalith.Builds/.github/workflows/domain-ci.yml`
- `references/Hexalith.EventStore/{deploy/dapr,docs/guides/disaster-recovery.md,docs/operations/projection-delivery-v2-cutover.md}`
- `references/Hexalith.Platform/DaprComponents/{statestore.yaml,pubsub.yaml}`
- `references/Hexalith.Platform/{README.md,docs/domain-module-enrollment.md,.config/dotnet-tools.json}`
- `references/Hexalith.Platform/_bmad-output/specs/spec-platform/SPEC.md`, roles and NFR-2.
- `references/Hexalith.Platform/_bmad-output/planning-artifacts/architecture/architecture-platform-2026-09-27/ARCHITECTURE-SPINE.md`, module declaration, production profile, secrets and notification conventions.
- `docs/{responsibility-boundaries.md,release-evidence/projection-read-store-population-proof-v2.md}`
- `_bmad-output/planning-artifacts/architecture.md`, AD-5/AD-6.
