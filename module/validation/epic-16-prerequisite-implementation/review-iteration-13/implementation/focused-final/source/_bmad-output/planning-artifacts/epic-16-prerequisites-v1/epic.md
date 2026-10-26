# DRAFT: Epic 16 successor prerequisites

This package prepares the missing Epic 16 entry prerequisites for owner review.
Its status is **draft**, the implementation hold remains **ACTIVE**, and it grants
no story execution, lifecycle transition, owner approval, release, or push
authority. Draft validation establishes document structure, preserved coverage,
and internal consistency only. It does not establish production readiness or
authorize Story 16.1, 16.2, or 16.3.

## Validate and review this preparation

Run from the repository root:

```bash
python3 _bmad/scripts/verify_epic_16_prerequisites.py --repository .
uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py
```

The checker is read-only. Exit 0 means draft consistency `PASS`, exit 1 means
`FAIL`, and exit 2 means required inputs are unavailable (`BLOCKED`). Even a
consistent draft reports `entryReadiness=BLOCKED`, hold `ACTIVE`, and four false
authority flags. It never runs the legacy acceptance command strings.

Review the [operational envelope](../production-operational-envelope-v1.md) and
the seven pending qualification/adoption items in [package.json](package.json).
Infrastructure selection is resolved: the user chose **Hexalith.Platform**.
The package also
lists exact artifact digests and source bindings; its own mutable assessment is
excluded from that digest inventory. Normative draft text and its schema have
independent code-owned pins so recomputing declared hashes cannot silently weaken
them. Deliberate revisions must update the draft, checker pins and package
digests together. This snapshot check does not impose a new gate on future
routine changes or certify a later checkout.

The preserved outcome is durable tenant authorization, deterministic projection
truth derived from event history, and diagnosable live reconciliation across
restart and multiple replicas. The source remains the immutable V14 Epic 16 block
in [the published epic carrier](../prds/prd-Conversations-2026-06-02/epics.md), the
three [historical story contracts](../v9/story-contracts/16.1.json), and their
ordered entry inventories in the
[2026-08-18 proposal](../sprint-change-proposal-2026-08-18.md). The
[architecture](../architecture.md) retains its current marker and inherited
AD-5, AD-6, AD-7, and AD-8 requirements. This draft adds no active authority
marker and rewrites none of those sources.

## Preserved stories and dependency graph

| Story | Preserved title | Exact predecessors | Required scenario IDs |
| --- | --- | --- | --- |
| 16.1 | Persist tenant-access projection state and prove convergence | 7.4, IR-0 | AC-16.1-01 through AC-16.1-06 |
| 16.2 | Make replay time deterministic and missing-index state truthful | 16.1 | AC-16.2-01 through AC-16.2-06 |
| 16.3 | Diagnose AppHost preflight failures and prove terminal reconciliation live | 16.2 | AC-16.3-01 through AC-16.3-06 |

The proposed graph preserves the historical graph's exact **38 nodes and 61
edges**. Its Epic 16 chain is `7.4, IR-0 -> 16.1 -> 16.2 -> 16.3`; Story 16.3
also gates 12.1, 13.1, 14.1, and 15.1. The graph remains acyclic. Execution
authorization and the hold are separate global predicates, not invented graph
nodes. Accepted predecessor status alone cannot grant entry.

A4 remains mapped to 16.1, A5 to 16.2, and A6 to 16.3. Those actions require
compatible passing final records before closure. This preparation changes no
action or sprint status. Each successor draft preserves the six original AC IDs,
the unchanged rollback boundary, and the original final-record targets and
required `6/6/0/0/0/0` summary. Original command strings are retained verbatim in
`legacyCommand` as provenance; this package never executes them or represents
them as adopted successor commands.

The requirement inventory retains all thirteen original ordered E6-A4, E6-A5,
and E6-A6 entry IDs and their historical digests. Additional named requirements
map the inherited architecture decisions onto these same eighteen scenarios;
they do not add product scope, change the accepted denominator, or replace
measured execution with prose assertions.

## Proposed operational and ownership contracts

Conversations inherits Hexalith.Platform infrastructure and operational policy.
Use its module-enrollment kit, logical capabilities, injected configuration and
declarative subscriptions; Platform owns physical providers and hosted deployment.
Its Administrator/recovery-deputy roles and NFR-2 recovery targets apply. Those
targets remain subject to qualification, rather than new infrastructure choices
for Conversations. Existing `state/state` reuse requires the shared component's
matching recovery classification; record tenant projections as derived in the
module recovery inventory. A new component role is unnecessary unless Platform
requires distinct isolation/classification.

AD-5 assigns the
[production operational envelope](../production-operational-envelope-v1.md) to
Architect/Runtime. It must resolve local, CI, staging, and production parity;
providers and infrastructure responsibilities; state, pub/sub, secret, and
identity dependencies; health and telemetry; backup, rebuild, disaster recovery,
and operator runbooks; scaling limits; and waiver expiry. Only the Release owner
may accept a capacity or parity waiver. Platform's policy and targets are not
proof of an enrolled deployment. The envelope
and its gate remain a hard prerequisite for Story 16.1 and any future hold lift.

AD-6 assigns reusable immutable-envelope extraction, ETag/CAS persistence,
three-total-attempt retry, replay scheduling, fenced recovery leases, and durable
acknowledgement to EventStore. Tenants owns pure transition policy and the
projection/checkpoint schema behind `ITenantProjectionStore`; Conversations owns
its configuration and access adapter. Projection and checkpoint commit in one
CAS. Exact-next sequence, authoritative historical fingerprint comparison,
durable pending-gap evidence, a monotonic recovery horizon, and fenced replay
prevent concurrent replicas from losing or regressing access changes. Only
`Healthy` authorizes access. Reads never trigger recovery, and Conversations
never clears gap or corruption state.

The required 16.1 proof retains restart without redelivery and two replicas with
single consumer-group delivery. It adds the specified reverse-save,
equal-sequence mismatch, three interleaved future deliveries, concurrent horizon
extension, retry exhaustion, lease expiry/restart, failed/successful recovery,
and two-replica fault schedules. Every acknowledged outcome must already be
durable. Remove the single-replica warning only after restart and convergence
proofs pass.

AD-7 makes the v2 `ConversationProjectionIndexReadModel` at the existing
`projection:conversations-index:<tenant-segment>` key the sole Conversations
lifecycle/watermark authority. Only the projection writer authors transition
plans; EventStore executes its CAS plans. Lifecycle fields, entries, watermarks,
dispatch references, and rebuild barriers share the control boundary. Erase
retains a tombstone and generation-scoped non-payload quarantine references.
Rebuild discovers authoritative streams, captures scan/cutover barriers, durably
queues catch-up, and proves exact-next sequence within each aggregate. Global
position gaps cannot establish tenant-stream gaps. Queries distinguish current
empty, rebuilding, unavailable, corrupt, and forbidden without repair writes.

AD-8 validates identity, exact persisted payload binding, and selected time
before duplicate suppression. Contract events use metadata `EventId` and
`OccurredAt`; position-only events use persisted `MessageId` and envelope
`Timestamp`. Full duplicate bindings include tenant, conversation, event type,
sequence, serialization format, payload fingerprint, and selected UTC ticks.
Missing identity/time or contradictory reuse fails closed; no replay wall-clock
fallback is introduced.

## Explicit Story 16.2 acceptance replacement

The historical AC-16.2-02 assertion that complete projection JSON and every
domain/freshness timestamp are byte-identical is replaced under AD-8:

> Canonical deterministic v2 replay-state JSON, `ReplayAnchorAt`, and
> `ReplayStateHash` are byte-identical for identical authoritative inputs, while
> public observation-time freshness is generated anew, conforms to
> `ProjectionFreshnessV1`, is excluded from replay identity, and is tested for
> semantic invariants rather than byte equality.

EventStore owns the fixed canonicalizer: web defaults, compact UTF-8, recursively
ordinal-sorted object properties, and stable ordered collections supplied by
Conversations. The replay hash covers exact canonical deterministic value bytes,
excluding observation freshness, provider wrappers, ETags, batch IDs, TTL, and
the separate coordinated-batch fingerprint. Provider conformance proves the
deserialize-and-canonical-reserialize round trip.

`ReplayAnchorAt` is the maximum validated contributing event/lifecycle time.
Public `LastAppliedEventTimestamp` uses the applicable replay anchor;
`ProjectionGeneratedAt` is the actual generation-completion instant;
`LagDuration` spans them. Generation before the anchor is contradictory and
fails closed. Complete provider documents and observation timestamps may differ
between rebuilds. The successor draft changes Story 16.2's bounded time wording
to state this distinction while retaining its title, other preserved behavior,
six scenario IDs, final-record requirements, and rollback boundary.

## Owner review and future adoption

Planning Tooling coordinates reconciliation with Release, Architecture, Quality,
EventStore, Tenants, and Conversations owners. Architect/Runtime supplies the
operational decisions, provider responsibilities, and recovery objectives;
Release alone accepts waivers and records any separately governed scoped
execution decision. Quality reviews coverage, fault strength, and public
boundary preservation. EventStore and Tenants owners establish the additive
public seams and their conformance tests before Conversations consumes them.

Platform enrollment requires Conversations-owned
`module/hexalith.module-manifest.v2.json`, `module/hosting-inventory.json` and
`module/sm-2-checklist.md`, using Platform's exact accepted tool pin. Platform
supplies executable/image and gateway mappings, server authentication/configuration,
scoped store bindings and the declarative Tenants feed. Follow the enrolled
Parties readmodel/checkpoint pattern and physical topic injection. Current
Conversations lacks those enrollment files and mappings; its subscription
discovery and dependency-root fallbacks require the kit's adoption checks.
Retain/freeze the existing module AppHost pending the separate retirement gates.
Static adoption validation and live source/package/hosted evidence remain distinct.

Before Story 16.3 entry, the Hexalith baseline owner with Architect/Runtime must
explicitly choose a module-AppHost exemption or a relocation amendment. The
existing non-packable fixture is brownfield test infrastructure and cannot
assert that decision, production hosting ownership, or new runtime authority.
The live proof must use the production `project/v2/reconcile` route and durable
end-state/query observations; direct handler calls and a second reconciler
cannot replace it.

All three successor wrappers explicitly record
`generatorCompatible=false`, `scenarioVerifiersAvailable=false`, and
`requiresOwnerDecision=true`. The existing generator accepts V9/V14 contract
schemas and does not consume these draft wrappers. The missing scenario
verifiers are future implementation deliverables. Adoption must establish the
effective successor contracts, inventory/graph bindings, exact executable
commands, compatible generator/schema handling, appropriate current scoped
verification, and owner decisions without altering accepted historical bytes.

The [current-change policy](../../../docs/runbooks/current-change-validation.md)
retires routine V19–V29 authority publication/resolver gates. This draft does not
restart them or repair their historical diagnostics. Product safety entry
requirements, explicit story evidence obligations, and the independent Story
7.3 completion guard remain applicable. Until actual owner decisions and
adoption satisfy the entry requirements, the hold stays **ACTIVE**, execution
is disallowed, and no final record or completed runtime proof is claimed.
