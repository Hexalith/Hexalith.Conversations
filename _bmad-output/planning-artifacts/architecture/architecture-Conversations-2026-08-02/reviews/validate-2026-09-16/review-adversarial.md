# Adversarial Divergence Review — `architecture.md` through V14

- **Review date:** 2026-09-16
- **Target:** `_bmad-output/planning-artifacts/architecture.md` (2,698 lines)
- **Effective architecture/epic overlay:** `conversations-architecture-2026-08-18-v14` / `epic-6-authority-2026-08-18-v14`
- **Intent:** validation only; the target was not changed
- **Lens:** construct two units one level down that can each follow every applicable live rule yet still produce incompatible authority, state, data, or operational behavior

## Verdict

**FAIL — 2 critical, 4 high, and 1 medium live divergence holes.** V13/V14 successfully close most of the 2026-08-18 review's defects, especially graph composition, derived-key grammar, ADR identity, SM-C2 usability, and the public trust-state value set. The remaining failures concentrate in two places: (1) current-authority/hold resolution now has three mutually different machine-readable states with no total precedence rule, and (2) V14 assigns the new durable/runtime outcomes without fixing the cross-unit mutation, lifecycle, time, vocabulary, recovery, and environment contracts needed to build them consistently.

## Severity register

| ID | Severity | Live divergence |
| --- | --- | --- |
| ADV-1 | CRITICAL | Marker-sidecar, V14 bundle, and mutable hold readers resolve different current candidate/authority/execution states |
| ADV-2 | CRITICAL | `ITenantProjectionStore` provider and tenant-event consumer have no cross-replica atomic mutation/duplicate-identity contract |
| ADV-3 | HIGH | Story 16.2's lifecycle/watermark fact has no single owner, storage shape, key grammar, or migration boundary |
| ADV-4 | HIGH | Replay-time producer and verifier may select different immutable time sources and different safe states for absent/invalid time |
| ADV-5 | HIGH | DC-2 delegates redaction/hydration condition mapping to a vocabulary sidecar that V14 does not publish or bundle |
| ADV-6 | HIGH | The operational/environmental envelope remains unresolved even though V14's new durable-store and AppHost proof depend on it |
| ADV-7 | MEDIUM | Tenant-projection gap detection has no recovery owner; the only named live reconciliation route owns a different projection-delivery ledger |

---

## ADV-1 — CRITICAL — Current authority and hold state have no total machine-readable precedence

**Builder pair.**

1. A checkpoint/authority resolver follows V13's binding discovery rule: take the last complete architecture marker and load the exact `sidecar-head` it names.
2. A publication/hold resolver follows V9/V14: take the regenerated authority bundle for `PC`, then consume the separately mutable `implementation-hold-v1.json`, which V9 declares the single hold-decision record.

**Letter-compliant constructions.**

- Builder 1 loads the sidecar named by the V14 begin/end markers. That sidecar identifies V12 authorities, planning candidate `151f965…`, hold `ACTIVE`, `successor: none`, and `ir0RerunAllowed: false` (`architecture.md:2415-2430`, `:2627`, `:2698`; `v14-current-candidate-authority-v1.json:2-15`, `:39-63`). V14 explicitly says this sidecar is pinned unchanged point-in-time evidence (`architecture.md:2635-2640`).
- Builder 2 loads the V14-regenerated bundle, which identifies V14 authorities and planning candidate `1e9a611…` (`v9-authority-bundle-v1.json:2-9`; `architecture.md:2683-2692`). It then loads the mutable hold record, as required by V9 (`architecture.md:2089-2102`). The current record says `global: false`, unlocks only `7.1-SCHEMAS`, and has `effectiveState: LIFTED` (`implementation-hold-v1.json:10-18`, `:30-37`).

**Incompatibility.** A sidecar-only reader returns V12/`151f965…`/no IR-0/hold active; a bundle-plus-hold reader returns V14/`1e9a611…`/a scoped lift. A third strict reader must reject the current hold record because V11 says there is **no scoped exception state** (`architecture.md:2233-2238`) and therefore resolves `ACTIVE`. The V14 prose distinguishes point-in-time sidecar evidence from current publication, but neither the sidecar schema nor the hold schema carries a common precedence/scope discriminator that makes all three resolutions converge mechanically. The current hold record expressly says it does not alter V1-V16 authority (`implementation-hold-v1.json:31-35`), so it cannot itself serve as a V14 architecture amendment.

**Why critical.** This is the execution-authority switch. The incompatible outputs are not descriptive differences: they decide whether a checkpoint may run and which candidate/authority its evidence must bind.

**Disposition: DISCUSS / UPDATE.** Add one append-only decision that defines the total resolver algorithm and schema-level scope rules: current architecture, current `PC`, checkpoint point-in-time state, and effective hold state must each have a distinct field/source; define whether scoped lifts exist, and reject any record not expressible under that grammar. Do not repair historical sidecar bytes in place.

---

## ADV-2 — CRITICAL — Cross-replica tenant projection mutation has no atomicity owner

**Builder pair.**

1. The Tenants.Client durable-provider builder implements the V14-owned `ITenantProjectionStore` capability.
2. The Conversations tenant-event consumer builder applies ordered tenant events and relies on that store under two replicas.

**Letter-compliant constructions.**

- The provider implements the existing `GetAsync(tenantId)` / `SaveAsync(state)` contract as a durable last-write-wins store. The interface carries no expected version, ETag, compare-and-swap result, transaction, or merge callback (`references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs:6-21`). That is a valid implementation of the interface and of V14's requirement to place durable storage behind it (`architecture.md:2642-2647`).
- The consumer uses its existing per-process tenant semaphore, reads a state, ignores only an already-newer sequence, applies the event, and calls unconditional `SaveAsync` (`references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs:125-139`). This obeys the current client contract and the V14 assignment to consume/configure rather than duplicate generic storage.

**Incompatibility.** Replica A and B can read sequence `n-1`, apply non-commutative events `n` and `n+1`, and save in reverse order. A last-write-wins provider can persist sequence `n` after `n+1`, or persist `n+1` with state that never incorporated `n`. V14 requires two replicas to converge and unsafe gap/regression state to fail closed (`architecture.md:2648-2649`; `epics.md:4408-4412`) but does not bind who detects the conflict, what constitutes an equivalent duplicate, whether same-sequence/different-message is corruption, or what atomic operation the store must expose. Two providers can therefore implement incompatible mutation protocols while both satisfy the named interface; in the unsafe schedule an earlier active-state write can erase a later disable transition.

**Disposition: DISCUSS / UPDATE.** Bind a single cross-replica write protocol and owner: event identity tuple, monotonic sequence rule, equal-sequence mismatch rule, atomic compare/merge primitive, retry budget, and fail-closed terminal outcome. The interface and its provider conformance suite must expose enough information to enforce that protocol rather than infer it from tests.

---

## ADV-3 — HIGH — The initialized-empty lifecycle/watermark fact can be built in two incompatible stores

**Builder pair.**

1. A Story 16.1/Tenants builder treats the durable tenant projection's creation/sequence metadata as the proof that a tenant is initialized even when it has no conversations.
2. A Story 16.2/Conversations builder adds a Conversations-owned lifecycle record or extends the tenant conversation-index record so the query store can distinguish initialized-empty from erased/missing.

**Letter-compliant constructions.** Both are event-fed, both avoid query-side repair, and both preserve the public API shape, exactly as V14 requires (`architecture.md:2650-2651`; `epics.md:4425-4432`). The current read store demonstrates why the fact is load-bearing: a missing index is presently returned as an empty current page, while its own comment records that erased and never-created states are indistinguishable (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:139-160`).

**Incompatibility.** The Tenants-backed reader and the Conversations-index-backed reader can disagree after tenant creation, index deletion, rebuild, or store outage. One can report initialized-empty from tenant lifecycle state while the other reports `Unavailable` because its separate watermark key is absent. Conversely, extending the existing index and creating a separate lifecycle key yields two writers for the same truth. V14 names only an "event-fed lifecycle/watermark fact"; it does not bind its authority owner, state-store record shape, key, atomic relationship to the index, tombstone/rebuild behavior, or versioned migration. DC-7 says any derived-key grammar change needs a versioned successor with migration/rebuild treatment (`architecture.md:2538-2555`), but V14 names no successor grammar for this new fact.

**Disposition: DISCUSS / UPDATE.** Choose one authority and one persisted shape. Bind its key grammar/version, initialization event, atomicity relative to index writes, deletion/rebuild semantics, and the exact read mapping for never-used, initialized-empty, erased, corrupt, and unavailable.

---

## ADV-4 — HIGH — Replay-visible time has two legal immutable sources and two legal degraded outcomes

**Builder pair.**

1. The materializer builder derives time from the domain event's `CommittedAt` where present and the envelope timestamp for position-only events.
2. The replay/conformance builder treats the event envelope timestamp as the one canonical immutable clock, or treats absent/invalid time as a different safe trust state.

**Letter-compliant constructions.** V14 says only that replay-visible time comes from immutable event inputs and that missing/invalid time yields a typed safe state (`architecture.md:2650-2651`; `epics.md:4425-4428`, `:4440-4444`). The brownfield code exposes both candidates: position-only events use the envelope `Timestamp` (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:193-197`), while ordinary events advance with payload `CommittedAt` (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs:630-670`). The current fallback to `projectionGeneratedAt` when no event timestamp exists is exactly the defect V14 intends to remove (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs:115-128`).

**Incompatibility.** Both builders can eliminate wall-clock replay fallback and produce byte-stable output, yet disagree whenever envelope and payload timestamps differ. For absent/invalid time, one can emit `Unavailable` and another `Rebuilding`; both are typed, safe, non-current states under the public vocabulary. Their replay hashes, evidence timestamps, freshness lag, and expected conformance output are incompatible.

**Disposition: DISCUSS / UPDATE.** Bind the timestamp precedence table per event class, validation rules, timezone/precision normalization, behavior for conflicting immutable timestamps, and the exact state/reason-code pair for absent and invalid time.

---

## ADV-5 — HIGH — The condition-to-vocabulary contract promised by DC-2 is absent

**Builder pair.**

1. A Party/redaction hydration builder maps deleted, inaccessible, dependency-down, filtered, and redacted conditions onto the closed `ProjectionTrustState` values.
2. A UI/conformance/evidence builder decides which of those conditions must be indistinguishable and asserts a different mapping.

**Letter-compliant constructions.** DC-2 correctly ratifies the shipped public value set (`Current, Stale, Rebuilding, Unavailable, Forbidden, Redacted`) but explicitly delegates the redaction/hydration display-state sets, condition mapping, and per-surface indistinguishability policy to `_bmad-output/planning-artifacts/conversations-vocabulary-v1.json` (`architecture.md:2475-2494`). A builder may therefore use only existing values and still choose, for example, `Forbidden` for inaccessible Party data while another expects indistinguishable `Unavailable`; neither invents a synonym.

**Evidence of non-closure.** `git ls-tree -r --name-only 1e9a61126d3b7a55b514b7c7c8942d5af03355e5 -- _bmad-output/planning-artifacts` contains no `conversations-vocabulary-v1.json`, and the V14 bundle contains no such path. The review gate in DC-2 blocks affected successors from entering review, which is safe, but it does not make the missing mapping a decision.

**Disposition: DEFER TO THE EXISTING REVIEW BLOCKER, THEN UPDATE.** Publish and bundle the promised sidecar before any affected successor enters review. It must ratify the existing contract and enumerate the condition-to-state/reason mapping plus per-surface indistinguishability; it must not widen the public value set merely to close the planning gap.

---

## ADV-6 — HIGH — V14 consumes an operational envelope that remains only an owned open dimension

**Builder pair.**

1. The Story 16.1 provider/integration builder proves durable restart and two-replica convergence in the module test AppHost against one state-store/provider/environment profile.
2. The platform deployment/operator builds production topology, persistence, endpoint readiness, port allocation, runbooks, and capacity policy under its existing ownership.

**Letter-compliant constructions.** Conversations owns only a non-shipping test AppHost while platform deployment owns production topology (`architecture.md:331-335`, `:1940-1945`). V14 requires durable state and AppHost endpoint/port diagnostics but names no production profile (`architecture.md:2644-2655`; `epics.md:4458-4476`). V13 explicitly records environment topology, infrastructure/provider strategy, runbooks, and capacity-waiver ownership as an open dimension that must be decided, deferred with owner, or delegated before **any hold-lift decision** (`architecture.md:2612-2618`). No named counterpart artifact appears in the V14 overlay or bundle.

**Incompatibility.** The local builder can pass with a provider and consistency profile whose concurrency, retention, endpoint discovery, and restart behavior differ from production. The production operator can satisfy platform ownership with a different provider/profile for which the local proof and stable diagnostic classifications are not valid. Both respect their ownership boundary; no parity contract connects them.

**Current-state evidence.** A later mutable hold record performs a scoped lift (`global: false`, only `7.1-SCHEMAS`) without naming an operational-envelope decision/defer/delegate artifact (`implementation-hold-v1.json:10-18`, `:30-37`). That narrow schema-only lift does not yet execute Story 16, but it demonstrates that the V13 "before any hold-lift decision" gate is not mechanically represented.

**Disposition: DISCUSS / KEEP RUNTIME WORK BLOCKED.** Name the authoritative operational counterpart before any runtime/story lift: supported environment profiles, production state provider and consistency requirements, AppHost-to-production parity assertions, configuration/secret ownership, health/alert/runbook owner, capacity-waiver signer, and rollback/recovery boundary. If the decision belongs to platform deployment, bind that exact artifact and version instead of duplicating it here.

---

## ADV-7 — MEDIUM — Gap detection and terminal reconciliation have different unconnected owners

**Builder pair.**

1. The tenant projection builder detects a sequence gap, marks the state unsafe, and permanently fails closed; V14 does not require it to replay/backfill.
2. The Story 16.3 builder proves the existing `project/v2/reconcile` route clears a terminal named-projection delivery item and deliberately creates no second reconciliation implementation.

**Letter-compliant constructions.** V14 requires tenant access to fail closed for gapped state (`architecture.md:2648-2649`) and separately says Story 16.3 proves the live route without creating a second reconciliation implementation (`architecture.md:2652-2653`). The canonical story scopes that route to durable pending projection work and query visibility (`epics.md:4458-4464`, `:4474-4476`). Nothing says that route owns or can rebuild the Tenants.Client local access projection.

**Incompatibility.** One component produces a durable, safe-but-stuck tenant gap; the other truthfully reports success for reconciling a different EventStore named-projection ledger. Operators can be told reconciliation succeeded while tenant authorization remains permanently unavailable. Neither unit violates its bounded outcome.

**Disposition: DISCUSS / UPDATE.** Assign the tenant-gap recovery owner and route. Either extend a named platform reconciliation contract to this state with tenant-safe evidence, or define a separate rebuild/backfill operation and explicitly state that `project/v2/reconcile` does not clear tenant-access gaps.

---

## Prior-finding and dimension disposition

| Area | Disposition at V14 |
| --- | --- |
| SM-C2 usability/envelope | **Closed** by DC-1 (`architecture.md:2459-2474`). |
| Public trust/freshness value set | **Closed** by the first half of DC-2; condition mapping remains live as ADV-5 (`architecture.md:2475-2494`). |
| Record dirt vs promotion cleanliness | **Closed** by DC-3 (`architecture.md:2495-2500`). |
| Overlay graph composition | **Closed** by DC-4 and V14's exact 38-node/61-edge composition (`architecture.md:2501-2515`, `:2658-2677`). |
| Temporal evidence anchor | **Closed** for public evidence by DC-5; replay-time source precedence remains live as ADV-4 (`architecture.md:2516-2528`). |
| Audit degradation / ADR namespace / derived keys | **Closed** by DC-6 through DC-8 (`architecture.md:2529-2562`). |
| Tier strength, AppHost interpretation, ceiling retirement | **Closed** by DC-9 through DC-11 and carried into V14 (`architecture.md:2563-2578`; `epics.md:4487-4492`). |
| UX acceptance denominator | **Closed after the prior review:** the current UX map enumerates all 28 IDs, and Story 8.1 binds that exact frozen inventory (`_bmad-output/planning-artifacts/ux-requirement-map.md:82-118`; `_bmad-output/planning-artifacts/v9/story-contracts/8.1.json:81-84`). |
| Authority discovery / hold | **Reopened in a narrower machine-state form** as ADV-1; this is not the old frozen-frontmatter defect.
| Projection-handler ownership | **Partially live:** it is subsumed by ADV-3/ADV-7 for the new event-fed lifecycle fact; V14 does not select the mutation path.
| Legal source-event treatment | **Controlled deferred item, not counted:** source mutation still requires an approved legal/compliance ADR (`architecture.md:810-820`), so two implementation units cannot lawfully choose independently yet.
| Metadata identifier co-emission | **Stop-gated, not counted:** uncertain metadata classification must stop for an architecture decision (`architecture.md:1240-1255`).
| Operational/environmental envelope | **Explicit but unresolved**; escalated as ADV-6 because V14 introduces stories that depend on it and a later scoped hold record does not carry the required decision/delegation.

## Final gate statement

The V14 overlay is materially stronger than V12 and should retain its append-only closure work. It is not yet a convergent build contract for current authority resolution or Epic 16 runtime implementation. ADV-1 and ADV-2 require architecture decisions before any additional execution authorization; ADV-3 through ADV-7 require either tightened rules or explicit, named deferred gates before their affected stories enter review.
