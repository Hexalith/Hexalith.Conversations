# Epic 16 Context: Operational Projection Correctness And Recovery

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators and authorized callers durable tenant authorization, deterministic projection truth derived from event history, and diagnosable live reconciliation across restarts and multiple replicas. Recovery must restore query-visible state without weakening authorization, presenting missing state as current-empty, or creating another source of Conversation truth.

## Stories

- Story 16.1: Persist tenant-access projection state and prove convergence
- Story 16.2: Make replay time deterministic and missing-index state truthful
- Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live

## Requirements & Constraints

Epic 16 runtime entry remains held. Prerequisite, adoption and tooling preparation preserve the eighteen scenarios, thirty-two requirements, thirty-eight graph nodes, sixty-one edges, predecessor identities and rollback boundaries. Structural or synthetic success does not qualify runtime, provide owner acceptance or lift the ACTIVE hold. Current-change policy governs routine work; historical authority records remain immutable observations at their original candidates.

Runtime entry requires independently trusted, story-scoped owner qualification bound to a pre-existing authorization revision and compatible predecessors. Qualifications cover the Platform tool, adoption, operational envelope, provider parity, recovery drill, operator coverage, capacity disposition, human SM-2 and owner entry. An entry file or passing local topology alone cannot supply these decisions. Architect/Runtime owns the operational envelope; Release alone accepts capacity or parity waivers.

Conversations inherits Hexalith.Platform infrastructure, provider, identity, telemetry and deployment policy through logical capabilities and injected configuration. Local Redis and source/Debug success do not establish package/Release or hosted parity. Recovery inherits Platform RPO/RTO and response-coverage targets, subject to measured qualification, operator access, restore drills and explicit waiver expiry.

Preserve public contracts and tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness and governance audit-pairing. Unknown, stale, gapped, corrupt or unavailable tenant state denies authorization. Diagnostics and evidence disclose no tenant or personal content. Remove the single-replica warning only after restart and convergence proofs pass.

Acceptance requires real candidate-bound native execution, persisted end-state and durable acknowledgement evidence. Required results are nonempty, passing and free of failed, skipped or unexecuted cases. Generate the compatible JSON/Markdown final pair only after each story's six scenarios establish `6/6/0/0/0/0`. Preserve accepted evidence, predecessor bindings and rollback scope; unavailable qualification is BLOCKED, never PASS.

## Technical Decisions

EventStore history is the sole Conversation write authority. Projections and indexes are derived and rebuildable; queries never repair them. EventStore owns raw-envelope binding, CAS/ETag persistence, the three-total-attempt retry policy, replay scheduling, fenced recovery and acknowledgements. Tenants owns pure transition policy and projection/checkpoint shape. Conversations consumes a coherent snapshot; Platform retains hosting ownership.

Tenant transition and checkpoint commit in one CAS. Exact-next sequence advances state; accepted identical duplicates are no-ops, with older sequences checked against authoritative history. Conflicting identity becomes terminal Corrupt. Persist future-delivery fingerprints, immutable expected sequence and a monotonic recovery horizon. Acknowledge only durable outcomes. Fenced recovery validates history and pending evidence before CAS-restoring Healthy; an expired or superseded worker cannot erase newer progress. Authorization never initiates recovery.

The v2 Conversation index is the sole lifecycle/watermark authority. Its entries, lifecycle, generation, aggregate watermarks, dispatch references and rebuild manifest share a CAS boundary. Only the projection writer authors transition plans. Tenant creation may initialize Ready(empty); missing/v1 state requires replay/rebuild. Erasure retains a tombstone and non-payload quarantine references. Generation fencing prevents delayed deliveries from recreating erased data, and only authorized rebuild leaves Erased.

Rebuild discovers authoritative tenant streams at a scan barrier, queues later deliveries, drains through a cutover barrier and proves exact-next sequence within every stream before Ready. Numeric global-position gaps do not prove tenant-stream gaps. Preserve newer same-generation progress and post-cutover queued work.

Validate identity, exact persisted payload binding and selected time before deduplication. Contract events use metadata EventId and OccurredAt; position-only events use persisted MessageId and envelope timestamp. Missing identity/time or changed duplicate bindings fail closed, without clock fallback. Identical history must reproduce canonical deterministic replay-state bytes, internal ReplayAnchorAt and ReplayStateHash. Public ProjectionGeneratedAt is the actual generation instant; freshness and lag follow semantic invariants and are excluded from replay identity. EventStore owns canonical serialization; Conversations supplies explicitly ordered collections.

## UX & Interaction Patterns

Keep true empty, rebuilding, unavailable and forbidden distinct. Only proven v2 Ready with zero summaries and no pending dispatch is current-empty. Missing state/provider failure is unavailable; lifecycle or sequence gaps are rebuilding; contradictory metadata fails closed. Unauthorized and nonexistent tenants receive the same non-disclosing forbidden behavior without reading the index. Cross-key generation mismatches cannot disclose partial derived content.

Trust signals and safe next actions derive from server-owned state. UI documents preserve the intended behavior but do not activate UI implementation. Readiness and effective Dapr control-plane port failures need stable, content-safe diagnostics.

## Cross-Story Dependencies

The preserved chain is `7.4, IR-0 -> 16.1 -> 16.2 -> 16.3`; Story 16.3 also gates 12.1, 13.1, 14.1 and 15.1. Compatible predecessor records and current entry qualifications are separate requirements; backlog or ready status grants no runtime authority.

Story 16.3 must prove pending-work clearance and corrected query-visible state through the live production reconciliation route; direct handler invocation and HTTP success alone are insufficient. Before its entry, the baseline owner and Architect/Runtime must explicitly resolve the module AppHost conflict by exemption or relocation. Shared technical capabilities belong to their owning repositories and are consumed through approved additive surfaces.
