---
epic: 6
generated: '2026-08-01'
overlay_version: 'epic-6-authority-2026-08-01-v8'
architecture_version: 'conversations-architecture-2026-08-01-v8'
supersedes_overlay_version: 'epic-6-authority-2026-08-01-v7'
source_epics: '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md'
source_overlay_begin: 'EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN'
current_execution_view: '_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md'
status: 'authority-correction-only-not-ready'
---

# Epic 6 Context: PRD Alignment And Preservation Reconciliation

This developer context is derived from the append-only Epic 6 v2 overlay and
its approved v3-v8 amendments. It shares version
`epic-6-authority-2026-08-01-v8` with the active amendment and aligns with
`conversations-architecture-2026-08-01-v8`; semantic drift between them and the
deterministic current execution view is a conformance failure. The finalized
initiative PRD/addendum and approved comprehensive correction remain the
authority above this derived context.

Regenerated 2026-08-01 after the approved comprehensive implementation-readiness
authority correction. V8 republishes all twelve complete effective story
definitions, restores universal PRD SM-C2 authority, repairs UX planning
provenance, records the validated topology/checkpoints/BDD catalogue, and
imposes a global readiness hold. V7's projection-proof lifecycle, V6's
historical Story 6.2 disposition, V5's oracle tiering, V4's generated record,
and V3's test-AppHost boundary remain preserved context.

The frontmatter and this paragraph disagreed between 2026-07-28 and 2026-07-31: the frontmatter had been bumped to v5 while the body still described v4, and `source_overlay_begin` named an unversioned marker present in no amendment block. Both are corrected here, which is what the overlay's amendment-log rule exists to make checkable.

## Authority And Immutable History

- The initiative has 20 FRs: FR-1 through FR-20. FR-16 is the only non-activation and is deferred.
- The preservation denominator is all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria.
- The accepted SM-1 baseline remains 13,289 LOC.
- Epics 1-5, all 24 completed stories, completed Stories 6.1, 6.2, and 6.7, retrospectives, `done` states, the original epic-plan prefix, every v1-v7 authority block, accepted baselines, and signed v1 evidence remain immutable historical records.
- A delivered-to-inactive disposition or compatible public-contract change requires named owner approval, rationale, and compatibility evidence.
- Epic 6 is the only active corrective plan. It does not activate preserved feature scope.

## Global Implementation Hold

**AUTHORITY CORRECTION ONLY — NOT READY.** No remaining Epic 6 implementation
may start or resume until v8 authority validation passes and a separate fresh,
independent implementation-readiness assessment returns `READY`. Current
`in-progress` and `ready-for-dev` labels are lifecycle facts, not permission to
work. This context does not predetermine or publish that later assessment.

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

## Projection-Proof Evidence Lifecycle

- Story 6.2's `projection-read-store-population-proof-v2` is immutable point-in-time evidence for umbrella candidate `856ee997cd35eb1d432fcb288a75a7b5bf3c5b58` and EventStore gitlink `e645901928eed9759e28e1086f23dc96875c3ac3`.
- Historical validation reads root blobs from the recorded umbrella candidate and platform blobs from that candidate's recorded submodule commits. It never substitutes current `HEAD` or current submodule worktrees.
- Story 6.12 authors ADR 0004 and produces additive `projection-read-store-population-proof-v3` with full predecessor hashes and exactly one approved current chain head.
- In-scope projection dependency drift without a successor fails with `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; unrelated root gitlink movement does not invalidate historical proof.
- Story 6.3 records v2 as historical and v3 as current. Story 6.6 validates the chain and reruns the current head; v2 alone cannot prove a later release candidate.

## SM-C2 Contract

Frozen inventory version: `sm-c2-hot-path-inventory-v1`.

| ID | Kind | Operation |
| --- | --- | --- |
| HP-CREATE | command-warm | authorized conversation creation |
| HP-APPEND | command-warm-idempotent | append including duplicate replay and payload mismatch |
| HP-LIST | read-warm | authorized filtered/cursored list |
| HP-OPEN | read-warm | detail with freshness, redaction, evidence, and Party hydration |

Every baseline row has exactly one post disposition, measured with identical workload/data, concurrency, environment/runtime, tooling, warm/cold classification, repetitions, raw evidence processing, and commit-bound evidence. The module test AppHost exercises the same production code boundaries before and after; it does not become production topology.

**Current v8 rule.** PRD SM-C2/OQ-5 is the sole current metric authority. Every
one of HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN must have usable comparable
signal under the identical frozen envelope and satisfy
`post P95 <= 1.05 x baseline P95`. The v6 ceiling/disclosure rule is immutable
Story 6.2 completion context only; it is not a Story 6.6 pass option. Story 6.11
owns correctness-preserving remediation and measurement for all four rows and
must complete before Story 6.6. Changing the target requires separate approved
PRD-level authority.

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
- Distinguish candidate-bound projection history from current release assurance, bind the complete predecessor chain, and identify exactly one approved current proof head. Story 6.3 remains `in-progress` until Story 6.12 passes.

### 6.4 Repair UX provenance and preservation governance

- Cite the canonical PRD/addendum, retain a prominent preservation-only banner,
  and label the Phase 0-3 sequence as historical/future activation rather than
  active Epic 6 work.
- Produce the exact v1 UX disposition schema/JSON/deterministic Markdown and a
  zero-gap validator that covers UX-DR1-52 plus every explicit UX acceptance
  criterion once, with source hashes, owner, rationale, evidence/control, and
  labeled historical provenance.
- Preserve current FrontComposer/Fluent UI V5 governance; authorize no
  production UI change. Story 6.8 gates completion.

### 6.5 Correct the thin authoring template and reproduce SM-2

- Include one non-packable, non-publishable module test AppHost in the template and count its hand-authored files and LOC.
- Prohibit reusable module-owned Aspire, generic ServiceDefaults, DAPR, health, telemetry, projection/query, publication, or subscription capability.
- Use live public platform APIs and a reproducible fixture/versioned v2 measurement while preserving the 13,289-LOC baseline.
- Review through ordered checkpoints 6.5-A authoring contract, 6.5-B minimal
  fixture, and 6.5-C measurement/conclusion. A checkpoint alone cannot complete
  the story; Stories 6.8 and 6.10 gate completion.

### 6.6 Revalidate and issue superseding attestation

- Run the complete manifest, public-contract, universal four-row SM-C2,
  SM-1/SM-2/SM-3, test-AppHost boundary, and platform-composition gates. A v6
  ceiling, disclosure, or unusable signal cannot pass.
- Issue versioned v2 evidence, a separate supersession record, and a new release-owner decision without mutating v1.
- Consume and hash-validate accepted ADR 0003 and the immutable Story 6.2 `projection-read-store-population-proof-v2` predecessor at its recorded candidate; consume and rerun the latest approved projection-proof chain head. Do not cite v2 alone or inherit the signed v1 projection-population deferral as proof or waiver for current readiness.
- Run last. Execute a fresh independent assessment against the exact candidate,
  publish its complete actual result unchanged, and never instruct the assessor
  to return a particular verdict. Release closure is a separate decision and
  remains blocked unless the preserved result is `READY`.

### 6.7 Mechanically block incomplete submodule promotions from completion

- Declare exact root `references/...` promotion paths and remote-availability policy.
- Require initialized/clean affected submodules and exact mode-`160000` committed root gitlinks; include changed gitlinks since baseline.
- Block review/completion with stable codes; warn on unrelated state.
- Read root `.gitmodules` only. Never initialize, update, or traverse nested submodules.

### 6.8 Generate the final story record mechanically from measured state

- Emit the completion record from four derived sources only: parsed machine-readable test-result artifacts, the git-derived path set between the work baseline and the committed candidate with source-tree dirt blocked outside record outputs and declared TRX inputs, mode-`160000` root gitlink entries from that candidate, and the Story 6.7 promotion-checker document embedded verbatim.
- Take counts only from result artifacts. The root `.slnx` defines required root-owned test projects; a missing or zero-test artifact blocks, totals are computed, failures block, skips require exact versioned identity-and-reason policy, and stale artifacts are never carried forward.
- Derive one file list. Reject any path inside a root-declared submodule and emit gitlink promotions in a separate labeled section with recorded commit and mode.
- Bind state to the final candidate: it must be an ancestor of the committed head, only record-output paths may follow it, and no gitlink may move, so a superseded binding goes red rather than stale.
- Make the four completion surfaces emit one document-and-Markdown bundle rather than author, verify the inserted digest, block `review` and `done` on generator blockers, refuse a pass that derived nothing, and guard the invocation against silent removal.
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

### 6.10 Consolidate the evidence-boundary validation pattern

- Add one non-packable, assembly-neutral `Hexalith.Conversations.TestSupport`
  helper for repository location, bounded UTF-8 Git facts, recomputed manifests,
  exact boundary assertions, and a non-vacuous assertion ledger.
- Require repository-contained canonical hashes, recomputed signable payloads,
  exact changed-file equality, raw mode-`160000` gitlink rejection, explicit
  unavailable-history skip, and failure on zero executed assertions.
- Enforce the stable evidence-boundary blocker/warning contract in the five
  governed workflow bodies in both active trees and the two render twins;
  migrate every evidence reader at unchanged assertion strength with zero
  day-one exemptions.
- Fault-inject every guard and repair Story 6.7's gate-span displacement
  coupling. Story 6.10 is independent of 6.12, follows 6.8 and 6.9, and gates
  completion of 6.3, 6.5, and 6.6.

### 6.11 Restore the universal SM-C2 gate without weakening projection correctness

- Author an ADR before production change covering per-conversation derived-key
  ownership, ordering, compatibility, rebuild/backfill, deletion/expiry, and
  rollback while EventStore remains write authority.
- Remove full-index/per-row fan-out only with explicit correctness proof;
  preserve fail-closed tenant isolation, stale/poison/partial state behavior,
  replay, retry, conflict, public query shape, and non-disclosure.
- Predeclare one measurement method for all four rows, retain raw samples, and
  treat unusable signal as failure.
- Complete only when HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN each satisfy
  `post P95 <= 1.05 x baseline P95` with every correctness gate green. Story
  6.11 follows completed 6.2, is independent of 6.10/6.12, and is mandatory
  before 6.6.

### 6.12 Version projection proofs without rewriting completed history

- Preserve Story 6.2 and its v2 proof artifacts byte-for-byte and validate their bindings at the recorded candidate/gitlinks.
- Replace perpetual-current-checkout assumptions with candidate-aware historical validation; remain strict against mutated or unresolvable recorded objects.
- Author ADR 0004 and generate `projection-read-store-population-proof-v3` with full predecessor hashes, one approved current head, exact changed dependency identities, named owner/rationale, and fresh deterministic/gateway/state-store/query/deletion/replay evidence.
- Fail undeclared in-scope drift with `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; ignore unrelated root gitlink movement for historical validity.
- Fault-inject mutation, wrong identity, broken/forked chain, stale binding, and missing/red/skipped/vacuous run cases; complete only through Story 6.8's generated final-record gate.
- Review through 6.12-A historical validity/lifecycle (AC1-AC3), 6.12-B
  successor/current guard (AC4-AC5), and 6.12-C fault injection/handoff/closure
  (AC6-AC8). Checkpoints do not advance the story; 6.8 remains the entry gate.

## Binding Sequence

Completed historical spine: `6.1 -> 6.7 -> 6.2 -> 6.8`.

- No remaining edge activates until the independent readiness result is `READY`.
- 6.8 follows 6.2 and precedes completion of 6.3, 6.4, 6.5, and 6.6.
- 6.9 follows 6.1. Together 6.8 and 6.9 precede 6.10.
- 6.8 precedes 6.12. Stories 6.10, 6.11, and 6.12 are mutually
  independent after their direct predecessors.
- 6.9 + 6.10 + 6.12 precede completion of 6.3.
- 6.8 + 6.10 precede completion of 6.5.
- 6.2 precedes 6.11; the universal four-row result precedes 6.6.
- 6.3 + 6.4 + 6.5 + 6.8 + 6.9 + 6.10 + 6.11 + 6.12 precede 6.6.
- 6.6 is last and preserves the independent assessment's actual result.

## Final Record Invariant

Counts, file paths, submodule state, and root gitlink state in a completion record are derived outputs. For every story completing after Story 6.2 they are produced by the record generator and inserted verbatim. Narrative prose may surround a generated record but may not restate its numbers, a second hand-maintained file list is a conformance failure, and a record that no longer describes the final candidate must go red rather than stale.

## Promotion Completion Invariant

Before promotion-bearing work reaches `done`, every affected root-declared submodule must be clean including untracked files, satisfy the declared availability policy, and be represented by its exact commit in a mode-`160000` gitlink in the committed umbrella revision. The affected set is the declaration plus gitlinks changed since the work baseline. Unrelated state warns; only affected state blocks. Nested submodules are never initialized or traversed.

## Conformance Oracle Tier Invariant

The conformance oracle has two declared tiers. The portable tier binds only Contracts, Client, and Testing and references no non-packable module assembly; this is asserted by a test over the resolved compile surface, not claimed in prose. The module-internal tier binds `Hexalith.Conversations.Server` legitimately and by design. Tier membership governs what an assertion may bind, never whether it runs. Making a public contract wider, or an assertion weaker, in order to move a check into the portable tier is a conformance failure. An assertion that cannot be re-expressed at full strength belongs in the module-internal tier, and recording it there is a correct outcome rather than a deferral.

## Projection-Proof Lifecycle Invariant

Completed projection proof is validated at its declared candidate and dependency identities, never by silently substituting the current checkout. Current readiness is represented by exactly one approved predecessor-linked chain head with fresh executed evidence. Successors are additive, predecessor hashes are full and immutable, Story 6.2 remains done, and v2 is never rewritten to follow later platform movement.

## V8 Publication Boundary

V8 changes planning authority, deterministic planning projections, UX planning
provenance/mapping, sprint hold prose, and planning-authority validation only.
It does not implement any remaining story, run or predetermine readiness,
modify the finalized PRD/addendum, historical epic prefix, v1-v7 blocks,
completed records, retrospectives, accepted baselines, signed evidence, runtime
source, public contracts, package/deployment topology, or submodule
contents/gitlinks.
