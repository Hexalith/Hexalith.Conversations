# Epic 16 Context: Operational Projection Correctness And Recovery

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators and authorized callers durable tenant authorization, deterministic projection truth derived from event history, and diagnosable live reconciliation across restarts and multiple replicas. Durable recovery must restore query-visible state without weakening authorization, fabricating current-empty results, or creating another source of Conversation truth.

## Stories

- Story 16.1: Persist tenant-access projection state and prove convergence
- Story 16.2: Make replay time deterministic and missing-index state truthful
- Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live

## Requirements & Constraints

- Epic 16 execution remains held. Historical planning publication and an IR-0 `READY` result do not authorize implementation. Resolve execution authority at the evaluated commit through the current protected checker; missing, invalid, stale, or unscoped authority means `ACTIVE`.
- Before Story 16.1 entry, publish append-only successor Epic 16 and story carriers incorporating the operational-envelope, atomic tenant-mutation, lifecycle, and replay contracts. Regenerate their inventories, graph, and validators while preserving historical V14 carriers and accepted evidence. This context is not such a publication.
- Architect/Runtime must supply `_bmad-output/planning-artifacts/production-operational-envelope-v1.md` before Story 16.1 or any future hold-lift decision. It must cover environment parity, infrastructure/provider ownership, dependencies, health, telemetry, recovery/runbooks, scaling, and waiver expiry. Release owns capacity/parity waivers.
- Unknown, stale, gapped, corrupt, or unavailable tenant projection state must deny authorization. Prove restart recovery without redelivery, two-replica convergence, duplicate tolerance, ordering safety, and durable recovery under concurrency and provider failures.
- Preserve public contract shape and the seven preservation categories: tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, and governance audit-pairing. Diagnostics and evidence must not disclose tenant or personal data.
- Prove terminal recovery through the live `project/v2/reconcile` production route: durable pending work must be observable, retry must clear terminal work, and corrected state must become query-visible. Direct handler invocation cannot establish this proof. Readiness and effective Dapr control-plane port collisions require stable safe diagnostics.
- Required evidence must be measured, nonempty, candidate-bound, and free of failed, skipped, or unexecuted required results. Generate compatible JSON/Markdown final records from the canonical contracts; preserve accepted predecessors and signed history.

## Technical Decisions

EventStore history is the sole Conversation write authority. Projections, indexes, caches, and evidence are derived and rebuildable; queries perform no repair writes. EventStore owns generic transport, CAS/ETag persistence, retry, replay scheduling, fenced leases, and acknowledgements. Tenants owns tenant transition policy and projection/checkpoint shape. Conversations consumes these public seams. Platform deployment retains hosting ownership.

Tenant mutation commits projection and checkpoint atomically. Apply only the exact-next sequence; identical fingerprints are no-ops, conflicting identities become `Corrupt`, and provider/retry exhaustion becomes `Unavailable`. Persist gaps and a monotonically extending recovery horizon. Acknowledge only durable outcomes. Fenced replay validates authoritative history and observed fingerprints before CAS-restoring `Healthy`; stale workers cannot erase newer progress. Every non-healthy state denies authorization.

The v2 Conversation index at `projection:conversations-index:<tenant-segment>` is the sole lifecycle/watermark authority. Its lifecycle, entries, aggregate watermarks, dispatch references, and rebuild manifest share a CAS boundary. Only the projection writer authors transition plans. `TenantCreated` can initialize `Ready(empty)`; missing/v1 state requires rebuild. Erasure retains a tombstone/control record, and only authorized rebuild leaves `Erased`. Generation fencing prevents delayed deliveries from recreating erased data. Rebuild discovers authoritative streams, catches up through a cutover barrier, and proves exact-next sequence within each stream; numeric global-position gaps do not establish tenant-stream gaps.

Validate event identity, exact persisted payload binding, and selected time before deduplication. Contract events use metadata `EventId` and `OccurredAt`; position-only events use persisted `MessageId` and envelope timestamp. Invalid identity/time or changed duplicate bindings fail closed; do not introduce clock fallbacks. Canonical replay-state bytes, internal `ReplayAnchorAt`, and `ReplayStateHash` must match for identical history. Public `ProjectionGeneratedAt` is the actual generation instant, with semantically valid lag/freshness, and is excluded from replay identity. Complete provider documents and operational timestamps are not required to be byte-identical. EventStore owns canonical serialization; Conversations orders collections explicitly.

## UX & Interaction Patterns

Only proven v2 `Ready` state with no summaries or pending dispatch may produce a current empty page. Missing/unavailable state maps to `Unavailable/unavailable`; lifecycle or sequence gaps map to `Rebuilding/gap_detected`; corrupt or mixed-generation metadata maps to `Unavailable/metadata_contradictory`. Unauthorized and nonexistent tenants share non-disclosing `Forbidden/forbidden` behavior without reading the index. Keep rebuilding, unavailable, forbidden, and true empty distinct. Existing UX documents preserve optional UI behavior but do not activate UI implementation.

## Cross-Story Dependencies

The published dependency chain is `7.4` and `IR-0` → `16.1` → `16.2` → `16.3`. Story 16.3 additionally precedes `12.1`, `13.1`, `14.1`, and `15.1`. Compatible predecessor records and current execution gates are required; backlog or ready status alone grants no authority.

Before Story 16.3 entry, the Hexalith baseline owner and Architect/Runtime must explicitly resolve the module AppHost conflict through an exemption or relocation amendment. The existing non-packable fixture cannot itself establish baseline conformance or new runtime authority. Shared platform capability changes belong to their owning repositories and must be consumed through approved additive surfaces.
