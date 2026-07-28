---
epic: 6
generated: '2026-07-28'
overlay_version: 'epic-6-authority-2026-07-28-v5'
architecture_version: 'conversations-architecture-2026-07-28-v5'
supersedes_overlay_version: 'epic-6-authority-2026-07-28-v4'
source_epics: '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md'
source_overlay_begin: 'EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN'
status: 'active-corrective-context'
---

# Epic 6 Context: PRD Alignment And Preservation Reconciliation

This developer context is derived from the append-only Epic 6 v2 overlay and its approved v3 and v4 amendments. It shares version `epic-6-authority-2026-07-28-v4` with the active amendment and aligns with `conversations-architecture-2026-07-28-v4`; semantic drift between them is a conformance failure. The finalized initiative PRD/addendum and approved correction proposals remain the authority above this derived context.

Regenerated 2026-07-28 after the approved mechanical final-record amendment. V4 supersedes v3 only by adding Story 6.8 and amending the binding order; V3 superseded v2 only for the treatment of `Hexalith.Conversations.AppHost`. Every preservation, projection-population, promotion, performance, signed-evidence, and readiness obligation remains binding.

## Authority And Immutable History

- The initiative has 20 FRs: FR-1 through FR-20. FR-16 is the only non-activation and is deferred.
- The preservation denominator is all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria.
- The accepted SM-1 baseline remains 13,289 LOC.
- Epics 1-5, all 24 completed stories, retrospectives, `done` states, the original epic-plan prefix, the v1/v2 authority overlays, and signed v1 evidence remain immutable historical records.
- A delivered-to-inactive disposition or compatible public-contract change requires named owner approval, rationale, and compatibility evidence.
- Epic 6 is the only active corrective plan. It does not activate preserved feature scope.

## Corrected Ownership Spine

Conversations owns contracts, aggregate/domain behavior, validators, handlers, projections/read-model semantics, domain adapters, domain telemetry definitions, client/testing assets, optional domain UI, and one non-packable, non-publishable module-scoped AppHost limited to local Conversations user and end-to-end tests. It is not a production or deployment composition root. Platform deployment owns production topology and composition; platform libraries own reusable runtime capability. EventStore DomainService, ServiceDefaults, and Aspire own generic hosting, endpoints, DAPR resources, health, telemetry wiring, query/projection runtime, and subscriptions.

`Hexalith.Conversations.AppHost` and its focused tests are target test infrastructure. Story 6.2 makes the non-shipping boundary mechanical and retains their solution entries. `Hexalith.Conversations.ServiceDefaults` remains pre-6.2 migration input and is removed when it has no independently justified domain responsibility. No reusable Conversations Aspire, DAPR, publication, health, telemetry, projection/query, or subscription facade is authorized.

The canonical domain-host pair is:

```csharp
builder.AddEventStoreDomainService(/* domain assemblies/options */);
app.UseEventStoreDomainService();
```

Do not teach direct `MapEventStoreDomainService()` use.

| FR | Public landing zone |
| --- | --- |
| FR-10 | EventStore.ServiceDefaults + EventStore.DomainService |
| FR-11 | Hexalith.Commons.TenantAccess |
| FR-12 | Hexalith.Commons.Http |
| FR-13 | Platform deployment + EventStore.Aspire |
| FR-14 | Hexalith.Commons.Serialization |
| FR-15 | Hexalith.Commons.Diagnostics + EventStore domain telemetry |
| FR-16 | deferred, non-activated |

OQ-1 through OQ-5 are resolved in architecture. Governance/temporal/hydration behavior remains domain-owned; performance uses SM-C2; preserved absolute targets activate only through a current release decision.

## Still-Binding Safety Decisions

- Keep versioned events, compatible readers/upcasters, deterministic mixed-stream replay, and typed failure for unsupported versions.
- EventStore history wins over projections/caches/exports. Derived disagreement causes quarantine/stale/rebuild state, and rebuild does not replay external side effects.
- Tenants access and participant validation writes fail closed. Authorized Party hydration reads may degrade only to a policy-defined non-personal placeholder with explicit state.
- Idempotency preserves equivalent retry outcomes, rejects payload mismatch, and records unknown outcome explicitly rather than blindly retrying.
- Governance mutations require paired audit/domain evidence. Approved legal-policy exceptions require named owner, rationale, scope, and evidence.

## Projection Read-Store Population (ADR 0003)

[ADR 0003](../../docs/adrs/0003-projection-read-store-population-proof.md) is accepted architecture authority for Stories 6.2 and 6.6. Production population of the Conversations query read store is **mandatory proof**. The signed July 14 v1 residual-risk acceptance remains valid only for its immutable bound scope and cannot satisfy Epic 6 or v2 readiness, nor act as a waiver.

- EventStore owns ordered delivery, stable dispatch identity, and canonical named-projection routing. Conversations owns event materialization, tenant-scoped read-model keys, use of the shared write policy/store, freshness, and query semantics.
- A scoped named `IAsyncDomainProjectionHandler` is the production population owner for the persisted query store. The legacy synchronous `IDomainProjectionHandler` is version-1 compatibility only, and its opaque gateway response is **not** query-store population evidence.
- Queries never replay, materialize, or silently backfill projection state.
- A durable completed projection outcome requires **both** the per-conversation summary/detail record and the tenant index write to complete. Partial-write uncertainty is non-completion and must converge through idempotent retry.
- Story 6.2 produces `projection-read-store-population-proof-v2` from an accepted append or authorized replay crossing the production named-dispatch boundary, the actual integration state-store end state, and the production query result. Direct writer invocation, DI resolution, mock call counts, and HTTP acceptance are supporting evidence only.

## SM-C2 Contract

Frozen inventory version: `sm-c2-hot-path-inventory-v1`.

| ID | Kind | Operation |
| --- | --- | --- |
| HP-CREATE | command-warm | authorized conversation creation |
| HP-APPEND | command-warm-idempotent | append including duplicate replay and payload mismatch |
| HP-LIST | read-warm | authorized filtered/cursored list |
| HP-OPEN | read-warm | detail with freshness, redaction, evidence, and Party hydration |

Every baseline row has exactly one post disposition. Each must satisfy `post P95 <= 1.05 x baseline P95` with identical workload/data, concurrency, environment/runtime, tooling, warm/cold classification, repetitions, raw evidence processing, and commit-bound evidence. The module test AppHost exercises the same production code boundaries before and after; it does not become production topology.

## Stories

### 6.1 Rebaseline architecture and planning authority

- Preserve the v2 authority as historical corrective provenance.
- Apply the v3 exception only to the non-shipping module test AppHost; production composition and reusable runtime capability remain platform-owned.
- Preserve signed v1 evidence and the original epic prefix.

### 6.2 Migrate Conversations to platform-owned hosting

- Freeze or reconstruct the versioned benchmark before runtime, projection, or topology changes.
- Retain the existing AppHost and its tests solely as non-packable, non-publishable module test infrastructure; do not select or modify FrontComposer.AppHost or EventStore.AppHost.
- Remove the local ServiceDefaults facade when it has no domain responsibility. Put generic gaps in the owning public platform surface and pass Story 6.7 for promotions.
- Expose a canonical named `IAsyncDomainProjectionHandler` route that reuses the materializer and persists both the tenant-scoped per-conversation summary/detail model and tenant index through `ConversationProjectionReadModelWriter`, `ReadModelWritePolicy`, and the configured `IReadModelStore`. Report completion only after both writes are durable.
- Produce versioned `projection-read-store-population-proof-v2` evidence for an accepted append or authorized replay crossing the production EventStore named-dispatch boundary into the Conversations handler, asserting the actual integration state-store end state and production query result. Do not call the writer directly.
- Prove duplicate delivery, partial-write retry, tenant isolation, bounded failure outcomes, derived-state deletion, and full replay converge to an equivalent per-conversation record and duplicate-free tenant index.

### 6.3 Create the complete preservation traceability manifest

- Cover all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, all UX acceptance criteria, public contracts, and current controls with zero gaps.
- Require evidence or named-owner non-activation plus rationale; bind hashes, versioned mutation governance, and module/platform control ownership.

### 6.4 Repair UX provenance and preservation governance

- Treat UX as preservation-only unless separately activated; repair provenance/mappings while retaining historical mappings as labeled provenance.
- Preserve current FrontComposer/Fluent UI V5 governance; authorize no production UI change.

### 6.5 Correct the thin authoring template and reproduce SM-2

- Include one non-packable, non-publishable module test AppHost in the template and count its hand-authored files and LOC.
- Prohibit reusable module-owned Aspire, generic ServiceDefaults, DAPR, health, telemetry, projection/query, publication, or subscription capability.
- Use live public platform APIs and a reproducible fixture/versioned v2 measurement while preserving the 13,289-LOC baseline.

### 6.6 Revalidate and issue superseding attestation

- Run the complete manifest, public-contract, SM-C2, SM-1/SM-2/SM-3, test-AppHost boundary, and platform-composition gates.
- Issue versioned v2 evidence, a separate supersession record, and a new release-owner decision without mutating v1.
- Consume and hash-validate accepted ADR 0003 and the Story 6.2 `projection-read-store-population-proof-v2` artifacts, and rerun their focused conformance and rebuild gates. Do not inherit the signed v1 projection-population deferral as proof or as a waiver for current readiness.
- Run last and require readiness `READY` before release closure.

### 6.7 Mechanically block incomplete submodule promotions from completion

- Declare exact root `references/...` promotion paths and remote-availability policy.
- Require initialized/clean affected submodules and exact mode-`160000` committed root gitlinks; include changed gitlinks since baseline.
- Block review/completion with stable codes; warn on unrelated state.
- Read root `.gitmodules` only. Never initialize, update, or traverse nested submodules.

### 6.8 Generate the final story record mechanically from measured state

- Emit the completion record from four derived sources only: parsed machine-readable test-result artifacts, the git-derived path set between the work baseline and the committed candidate unioned with the tracked working-tree delta, mode-`160000` root gitlink entries from that candidate, and the Story 6.7 promotion-checker document embedded verbatim.
- Take counts only from result artifacts. A declared project with no artifact is not run and blocks; totals are computed, not transcribed; an artifact older than the newest file in the derived list blocks as stale instead of being carried forward.
- Derive one file list. Reject any path inside a root-declared submodule and emit gitlink promotions in a separate labeled section with recorded commit and mode.
- Bind gitlink state to the final candidate: it must be an ancestor of the committed head with no declared gitlink movement after it, so a superseded binding goes red rather than stale.
- Make the four completion surfaces generate rather than author, block `review` and `done` on generator blockers, refuse a pass that derived nothing, and guard the invocation against silent removal.
- Verify closed records read-only in historical mode, without claiming to reconstruct a former uncommitted working tree, and fault-inject every guard.
- Do not modify production source, public contracts, signed evidence, or submodule content, and do not claim to have wired any gate into continuous integration.

### 6.9 Tier the conformance oracle and make the portable tier structural

- Triage every conformance file binding a `Hexalith.Conversations.Server` namespace. Re-express it against public Contracts/Client/Testing surfaces at unchanged assertion strength, or assign it to the module-internal tier with the exact type and reason it cannot move.
- Widening the public contract is not an available resolution. Weakening an assertion to make it portable is a conformance failure. An assertion that cannot move at full strength belongs in the module-internal tier and that is a correct outcome, not a deferral.
- Assert the portable tier's freedom from non-packable module assemblies from the resolved compile surface, not from project-file text.
- Remove, skip, rename away, and weaken nothing. Executed conformance test count is monotonic against the pre-split figure computed across both tiers, and that figure is derived from a machine-readable result artifact rather than transcribed.
- Record named-owner approval, rationale, and a versioned manifest update for the reclassification of the three manifested denominator suites. Frozen denominator membership is unchanged; only recorded tier changes.
- Supersede the v1 `projectReferenceDisposition` target end-state with a versioned v2 disposition artifact. Do not edit v1 artifacts.
- Declare both tiers to the Story 6.8 generator and the solution file so neither is silently unrun.
- A single portable project with the reference removed is a valid successful outcome if the triage proves the assertions re-express at unchanged strength. The commitment is to tier the oracle, not to produce two projects.

## Binding Sequence

`6.1 -> 6.7 -> 6.2 -> 6.8`

- 6.7 and the frozen benchmark precede 6.2 completion.
- 6.3/6.4 may proceed after 6.1 where dependencies allow.
- 6.2 precedes 6.5.
- 6.8 follows 6.2 and precedes the completion of 6.3, 6.4, 6.5, and 6.6.
- 6.9 may proceed after 6.1 and precedes the completion of 6.3 and 6.6. It is outside the `6.1 -> 6.7 -> 6.2 -> 6.8` spine because it changes no production source, performs no promotion, and depends on neither the hosting migration nor the record generator.
- 6.6 is last.

## Final Record Invariant

Counts, file paths, submodule state, and root gitlink state in a completion record are derived outputs. For every story completing after Story 6.2 they are produced by the record generator and inserted verbatim. Narrative prose may surround a generated record but may not restate its numbers, a second hand-maintained file list is a conformance failure, and a record that no longer describes the final candidate must go red rather than stale.

## Promotion Completion Invariant

Before promotion-bearing work reaches `done`, every affected root-declared submodule must be clean including untracked files, satisfy the declared availability policy, and be represented by its exact commit in a mode-`160000` gitlink in the committed umbrella revision. The affected set is the declaration plus gitlinks changed since the work baseline. Unrelated state warns; only affected state blocks. Nested submodules are never initialized or traversed.

## Conformance Oracle Tier Invariant

The conformance oracle has two declared tiers. The portable tier binds only Contracts, Client, and Testing and references no non-packable module assembly; this is asserted by a test over the resolved compile surface, not claimed in prose. The module-internal tier binds `Hexalith.Conversations.Server` legitimately and by design. Tier membership governs what an assertion may bind, never whether it runs. Making a public contract wider, or an assertion weaker, in order to move a check into the portable tier is a conformance failure. An assertion that cannot be re-expressed at full strength belongs in the module-internal tier, and recording it there is a correct outcome rather than a deferral.

## Story 6.1 Verification Boundary

The v3 amendment changes planning authority, generated context, and conformance validation only. It must not modify the finalized PRD/addendum, historical epic prefix, v1/v2 overlay content, retrospectives, signed v1 evidence, runtime source, solution membership, submodule contents/gitlinks, UX governance, thin-template evidence, or release evidence.
