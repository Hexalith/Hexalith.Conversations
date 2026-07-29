---
story_key: '6-2-migrate-conversations-to-platform-owned-hosting'
epic: 6
story_id: '6.2'
created: '2026-07-27'
status: 'in-progress'
baseline_commit: '29def441408becfbbbdc5c59b9af14a7717cb21f'
file_list_commit: 'dc69719a9ce7c25bb9755827f19c7e1ce2a87287'
submodule_promotions:
  - path: 'references/Hexalith.EventStore'
    require_remote: true
  - path: 'references/Hexalith.Builds'
    require_remote: true
  - path: 'references/Hexalith.Tenants'
    require_remote: true
# ^ The first entry is the exact transcription of the already-approved scope declared in
#   spec-6-2-migrate-conversations-to-platform-owned-hosting-2.md frontmatter (AC 1c).
#   Builds and Tenants are an APPROVED SCOPE EXPANSION: Jerome approved declaring all three
#   paths on 2026-07-27 when resolving Dev Notes -> Open decision D1 (option 2). Both moved
#   inside the baseline -> candidate window, both are initialized, clean, captured at mode
#   160000, and both recorded commits are present on their origin/main, so require_remote:
#   true is satisfiable. Declaring them converts two non-blocking UNDECLARED_GITLINK_CHANGE
#   warnings into evaluated declared scope, matching the Story 6.7 "undisclosed scope"
#   review precedent. Do not expand further without a new approval.
authority:
  overlay: 'epic-6-authority-2026-07-27-v3'
  architecture: 'conversations-architecture-2026-07-27-v3'
  adr: 'docs/adrs/0003-projection-read-store-population-proof.md'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/docs/adrs/0003-projection-read-store-population-proof.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-2-migrate-conversations-to-platform-owned-hosting-2.md'
---

# Story 6.2: Migrate Conversations to platform-owned hosting

Status: in-progress

## ⚠️ Read this first — most of this story is already implemented

A `bmad-quick-dev` spec route (`spec-6-2-…-2.md`, status `in-review`) implemented the
bulk of this story **before** this story record existed. The work is committed and the
solution is green. **Do not re-implement it.** This story record exists to (a) bind the
work to the epic's binding acceptance criteria, (b) close five concrete verified gaps,
and (c) carry it to `done` through the Story 6.7 gate.

Verified independently in this workspace at HEAD `04edf99` on 2026-07-27:

| Check | Command | Result |
| --- | --- | --- |
| Release build | `dotnet build Hexalith.Conversations.slnx -c Release -m:1 -p:NuGetAudit=false -p:UseHexalithProjectReferences=true` | **0 warnings, 0 errors** |
| AppHost evaluated properties | `dotnet msbuild …AppHost.csproj -getProperty:IsPackable -getProperty:IsPublishable` | `false` / `false` |
| AppHost topology | `ConversationsAppHostTopologyTest` | **8/8** |
| Async projection matrix | `ConversationAsyncProjectionHandlerTest` | **6/6** |
| Production-boundary population | `ConversationProjectionReadStorePopulationLiveTests` | **2/2** |
| Full conformance | `Hexalith.Conversations.Conformance.Tests` | **412/412**, 0 failed, 0 skipped |
| Whitespace | `git diff --check` | clean |
| Promotion gate (29def44 → HEAD) | `verify_submodule_promotion.py … --submodule references/Hexalith.EventStore --require-remote …` | `pass`, 0 blockers, **2 warnings** |

**The five open gaps are in Tasks T1–T5. Everything else is verification-only.**

## Story

As a **Conversations maintainer**,
I want **Conversations composed by platform-owned runtime capability, with the local AppHost retained only as a non-shipping module test harness and the production named-projection route durably populating the query read store**,
so that **the domain module carries no platform-owned hosting boilerplate and persisted query projections are proven on a production path rather than deferred**.

## Acceptance Criteria

Binding source: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`
→ *Story 6.2* (v2 overlay AC 1–6) **as superseded for AppHost ownership only** by the
v3 amendment *Story 6.2 Corrected Acceptance*. Both are approved authority — satisfy both.
Where they conflict (AC2 removal-of-AppHost), **v3 wins**.

**AC1 (SM-C2 baseline first).** The versioned pre-correction SM-C2 benchmark is captured
before runtime/projection/topology changes, **or reproducibly reconstructed from the
preserved source commit with the same versioned fixture**. An invented or incomparable
baseline blocks completion. `sm-c2-hot-path-inventory-v1` is frozen; all four rows
(`HP-CREATE`, `HP-APPEND`, `HP-LIST`, `HP-OPEN`) get exactly one baseline and one post
result under one identical envelope, and each must satisfy `post P95 <= 1.05 x baseline P95`.

**AC2 (test-only AppHost — v3).** Retain `src/Hexalith.Conversations.AppHost/`,
`tests/Hexalith.Conversations.AppHost.Tests/`, and their solution entries. The project is
**mechanically** non-packable and non-publishable, limited to Conversations Server /
Admin Web plus required platform dependencies for local module user and E2E testing.
Platform-host registration preserves topology, security, health, publication, admin
composition, and public contracts.

**AC3 (no module-owned generic capability).** `Hexalith.Conversations.ServiceDefaults` is
removed when it has no independently justified domain responsibility. No Conversations
Aspire, DAPR, publication, health, telemetry, projection/query, or subscription facade is
introduced. Generic gaps land on approved **public platform** surfaces, and **every
affected promotion passes Story 6.7's completion gate**.

**AC4 (named async projection route — ADR 0003).** Conversations exposes a canonical named
`IAsyncDomainProjectionHandler` route that reuses the existing materializer and persists
**both** the tenant-scoped per-conversation summary/detail model **and** the tenant index
through `ConversationProjectionReadModelWriter`, `ReadModelWritePolicy`, and the configured
`IReadModelStore`. Completion is reported **only after both writes are durable**.

**AC5 (production-boundary proof).** Versioned `projection-read-store-population-proof-v2`
evidence demonstrates an accepted append or authorized replay crossing the **production
EventStore named-dispatch boundary** into the Conversations handler, asserts the **actual
integration state-store end state** and the **production query result**, and does **not**
call the writer directly.

**AC6 (edge-case convergence).** Focused integration tests prove duplicate delivery, retry
after partial write, tenant isolation, bounded failure outcomes, derived-state deletion,
and full replay converge to an equivalent per-conversation record and a duplicate-free
tenant index. The legacy opaque projection response, DI resolution, mock calls, and HTTP
acceptance **alone are insufficient proof**.

**AC7 (v3 harness proof).** AppHost composition tests prove the harness consumes public
platform helpers, cannot be packed or published, and exercises production Server/EventStore
boundaries without becoming a deployment artifact.

## Tasks / Subtasks

### Open work

- [x] **T1 — Rebind the v2 proof's promotion evidence to the final story candidate** (AC: 3, 5) — **BLOCKING**
  - [x] `docs/release-evidence/projection-read-store-population-proof-v2.json` →
        `eventStorePromotion.commit` and `.requiredUmbrellaGitlinkCommit` are now `c8c7003`;
        `.umbrellaMechanicalGate.candidate` is `c398ea2`, `.recordedGitlink` is `c8c7003`,
        `warnings: []` and `blockers: []` are what the gate actually returned. The recorded
        gate result also carries `declaredScope` (3 paths), `changedGitlinks`, and the full
        `evaluated` array rather than a single summarised path.
  - [x] `docs/release-evidence/projection-read-store-population-proof-v2.md` → *Hosting and
        promotion* rewritten to the same commit/candidate, and states explicitly that the
        `0eb3657` / `b11b0c7` pair is superseded and why.
  - [x] `ProjectionReadStorePopulationProofValidationTest.cs` pinned to the real values —
        **not relaxed**. Assertions were added, not weakened: declared scope must equal the
        set of gitlinks that actually changed, every evaluated path must be
        initialized/clean/remote-available at mode `160000` with `recordedGitlink == head`,
        and `promotedCapabilityFilesChanged` must be empty.
  - [x] EventStore delta `0eb3657..c8c7003` confirmed against the submodule: exactly 2 commits
        (`e77c84da`, `c8c70030`) touching publication preflight, story docs, a nested pointer,
        and `ContainerPublishingGovernanceTests`. `EventStoreDomainServiceExtensions.cs` and
        `HexalithEventStoreDomainModuleExtensions.cs` are unchanged, and `AddDaprClient()` is
        still at line 310. Recorded as `eventStorePromotion.promotedCapabilityDelta`.
  - [x] Precedent closed and hardened. A gate result cannot name the commit containing it, so
        the evidence pins the last revision that moved a gitlink or production source, and a
        new test `RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` re-derives
        that binding from git on every run: candidate must be an ancestor of `HEAD`,
        `git diff --name-only <candidate>..HEAD -- references/` must be empty, and each
        recorded gitlink must equal `git rev-parse HEAD:references/<path>`. The evidence can
        now go red instead of going stale.

- [x] **T2 — Close the production-boundary proof-strength question** (AC: 5, 6) — **BLOCKING**
  - [x] Diagnosis confirmed: `ConversationProjectionReadStorePopulationLiveTests` composes
        `InMemoryReadModelStore` and calls `DomainProjectionDispatcher.DispatchAsync(...)`
        in-process. It is retained as the deterministic edge-case matrix, not as the AC5 proof.
  - [x] **Option (a) chosen by Jerome** — the fixture was strengthened; ADR 0003 Verification
        items 1 and 2 are satisfied as written, not narrowed. No residual gap is carried.
  - [x] `ConversationProjectionGatewayDispatchLiveTests` + `ConversationGatewayLiveFixture`
        drive delivery through `IProjectionUpdateOrchestrator` against a real `daprd` sidecar
        with a Redis-backed `statestore` component. The lane asserts the configured
        `IReadModelStore` really is `DaprReadModelStore`, that the projection refresh interval
        is `0` so `UpdateProjectionAsync` dispatches instead of registering polling work and
        returning against an empty store, and that the gateway discovered the
        `conversation-read-model` route from the domain service's own operational-index
        metadata. Structured logs from the passing run carry `ProjectionUpdateOrchestrator`
        and `NamedProjectionDispatchCoordinator` categories, so the gateway-side stages are
        observed rather than assumed. **2 passed, 0 failed, 0 skipped.**
  - [x] Recorded explicitly in the evidence JSON as `gatewayBoundaryEvidence`, with
        `resolution: "strengthened-fixture"` and `residualGap: "none"`, and mechanically
        asserted by `GatewayBoundaryEvidenceShouldCrossTheCoordinatorAndTheDaprStateStore`.
  - [x] Story 6.6 will hash-validate and rerun this proof; nothing is left implicit for it.

- [x] **T3 — Prove the non-shipping AppHost boundary from evaluated MSBuild properties** (AC: 2, 7)
  - [x] `ConversationsAppHostShouldBeMechanicallyNonShipping` now spawns
        `dotnet msbuild -getProperty:IsPackable -getProperty:IsPublishable` and asserts the
        **evaluated** values, failing loudly rather than skipping if evaluation cannot run.
  - [x] Both evaluate to `false` at HEAD; behaviour unchanged, guard hardened.
  - [x] **Fault-injected to prove it can fail.** A temporary
        `src/Hexalith.Conversations.AppHost/Directory.Build.targets` setting
        `<IsPackable>true</IsPackable>` — leaving the csproj XML reading `false` — flipped the
        evaluated value to `true` and turned the guard **red**. The previous XML-reading
        assertion would have stayed green through exactly that change. The injected file was
        removed and the guard returned green with the worktree clean.

- [x] **T4 — Make the SM-C2 baseline reconstruction auditable** (AC: 1)
  - [x] Both artifacts now carry the reconstruction provenance: method
        (`overlay-versioned-fixture-onto-preserved-source-commit`), the fixture overlay with
        `sha256 fd2c6184…` and `presentAtSourceCommit: false`, the measured production closure
        (`src/Hexalith.Conversations` + `src/Hexalith.Conversations.Contracts`), the
        equivalence argument, and the residual limitation.
  - [x] The reconstruction **is** evidenced, so AC1's *Block If* does not apply, and it is
        evidenced mechanically rather than asserted: `SmC2BaselineReconstructionValidationTest`
        uses git to confirm the fixture is absent at `29def44`, that the declared closure is
        unchanged between `29def44` and `HEAD`, and that the declared `changedFileCount`
        matches the real diff — with an `executedChecks == 3` guard so a skipped check cannot
        read as a pass. It also analyses the fixture's namespaces to confirm it depends only
        on the declared closure.
  - [x] The residual limitation is stated plainly in both artifacts rather than glossed:
        Story 6.2 changed no source inside the measured closure, so this gate confirms no
        regression rather than exercising the changed hosting and projection code. It is not a
        gate that could have failed for this story.

- [x] **T5 — Reconcile the story record, spec route, and sprint tracking** (AC: all)
  - [x] Both 6.2 specs are closed as `status: superseded` with `superseded_on`,
        `superseded_by`, and a `supersession_note` explaining the lineage. Exactly one live
        authority remains: this story record. Neither spec's intent contract was rewritten.
  - [x] File List derived mechanically from
        `git diff --name-only 29def44..HEAD` unioned with the working-tree changes staged in
        this session — **49 paths**, not hand-maintained.
  - [x] Sprint-status transitions carry the evidence that justifies each one, including the
        `SUBMODULE_DIRTY_UNTRACKED` blocker and its resolution.

### Already landed — verify, do not re-implement

Committed by `b11b0c7` ("refactor(hosting): adopt platform runtime"), `65c7699`, `48069d7`.

- [x] **AppHost retained and made non-shipping** (AC: 2, 7) — `IsPackable=false`,
      `IsPublishable=false`; topology thinned to `AddHexalithEventStoreSecurity`,
      `AddHexalithEventStoreGatewayProject`, `AddHexalithEventStore`, and
      `AddEventStoreDomainModule` from the public `Hexalith.EventStore.Aspire` surface;
      exactly three project resources (`eventstore`, `conversations`, `conversations-admin-web`).
- [x] **`Hexalith.Conversations.ServiceDefaults` removed** (AC: 3) — project, tests, solution
      entries, and `ScaffoldSmokeTest` expectations deleted; README updated.
- [x] **Generic `AddDaprClient()` moved to the platform** (AC: 3) — `Server/Program.cs` is back
      to the canonical two lines; `EventStoreDomainServiceExtensions.cs:310` now owns the
      idempotent registration. Promoted as `references/Hexalith.EventStore`.
- [x] **`ConversationAsyncProjectionHandler`** (AC: 4) — named route
      `conversation/conversation-read-model`, `IAsyncDomainProjectionRebuildHandler` with
      `RebuildSemantics = FullReplay`, reuses `ConversationProjectionMaterializer` +
      `ConversationProjectionEventDecoder`, persists via `ConversationProjectionReadModelWriter`,
      returns `Completed` only after `PersistAsync` covers both keys, maps
      `InvalidOperationException → Retryable(PartialRetry)` and other failures →
      `Indeterminate(HandlerFailure)`.
- [x] **Legacy `IDomainProjectionHandler` preserved** for v1 compatibility (AC: 4).
- [x] **Edge-case matrix tests** (AC: 6) — accepted append, stable duplicate, second-write
      failure + retry, unavailable store, cross-tenant input, derived-state deletion, full
      replay; 6 unit + 2 live, all green.
- [x] **SM-C2 baseline and post artifacts** (AC: 1) — all four rows pass under the frozen
      envelope; see T4 for the outstanding provenance gap.
- [x] **`projection-read-store-population-proof-v2.{json,md}` + conformance validator**
      (AC: 5) — 14 source bindings and 4 signed-v1 bindings all hash-verified against the
      working tree with **zero drift**; signed v1 artifacts byte-identical.

> Review note (2026-07-28): the patch set below intentionally changes bound production sources, so the
> historical v2 proof now detects source drift. Signed v1 evidence was not mutated. Story status remains
> `in-progress` until evidence is regenerated and the EventStore promotion is committed and captured.

### Review Findings

- [x] [Review][Patch] Detail-only partial writes remain invisible to tenant list consistency checks (HIGH) — When the detail write for a new conversation succeeds and the tenant-index write fails, `ListAsync` validates only conversations already named by the old index. Existing rows can therefore be returned as `Current` while the newly accepted conversation is omitted, violating AC6's rule that neither query may report a partial generation as current. Resolution: add an internal pending/completed dispatch ledger keyed by `dispatchId`; queries remain `Rebuilding` while the dispatch is pending.
- [x] [Review][Patch] Cross-key validation introduces an unbounded HP-LIST N+1 fan-out (HIGH) — `ListAsync` changed from one tenant-index read to one sequential detail-store read per indexed conversation. This conflicts with the prior explicit no-N+1 contract and the active SM-C2 HP-LIST gate, while the recorded benchmark excludes the changed Server path. Resolution: add bounded bulk/page reads for the candidate detail records and retain fail-closed cross-key consistency validation without an unbounded per-conversation fan-out.
- [x] [Review][Patch] Stable dispatch identity is ignored by handler persistence (HIGH) — `ProjectAsync` validates but never uses `dispatchId`, and materialization uses the current clock. A retry after both writes complete but before platform completion is recorded can therefore rewrite freshness timestamps and transiently split the keys instead of producing a stable idempotent result. Resolution: use the internal `dispatchId` ledger to make completed retries no-ops and prevent projection freshness timestamps from being rewritten.
- [x] [Review][Patch] Reject undecodable envelopes instead of silently dropping trailing events (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:65]
- [x] [Review][Patch] Do not acknowledge non-current materializations as completed dispatches (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:85]
- [x] [Review][Patch] Map deterministic rebuild decode failures to a terminal typed outcome (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:110]
- [x] [Review][Patch] Require qualified discriminator segment boundaries instead of arbitrary suffix matches (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:99]
- [x] [Review][Patch] Restrict durable aliases to actual persisted Conversations domain-event types (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:124]
- [x] [Review][Patch] Surface index-present/detail-missing state as mixed-generation rebuilding (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:51]
- [x] [Review][Patch] Compare summary content and complete freshness across detail and index keys (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:73]
- [x] [Review][Patch] Reject duplicate conversation summaries in the tenant index (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:105]
- [x] [Review][Patch] Sanitize foreign and duplicate sibling rows when preparing rebuild indexes (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:141]
- [x] [Review][Patch] Reject empty full-replay histories before preparing a replacement model (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:123]
- [x] [Review][Patch] Validate the projection envelope serialization format before JSON decoding (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:77]
- [x] [Review][Patch] Drain MSBuild stdout and stderr concurrently so the test timeout cannot deadlock (MEDIUM) [tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs:79]
- [x] [Review][Patch] Cover populated multi-conversation rebuild plans and persisted ETag concurrency (MEDIUM) [tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs:132]
- [x] [Review][Patch] Exercise durable payload decoding for every actual projected domain-event type (MEDIUM) [tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionDurableEventCoverageTest.cs:79]
- [x] [Review][Patch] Exercise the retained AppHost through a running Server/EventStore production boundary (HIGH) [tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs:124]

### Review Findings — pass 2 (2026-07-29, `bmad-code-review`, four blind layers)

Scope reviewed: `git diff 29def44..dc69719 -- src/ tests/` (45 files, +4,914/−417). Evidence JSON,
`_bmad/scripts/`, and planning docs were **not** in this chunk and remain unreviewed.
71 raw findings across four layers deduplicated to 42. Every finding below was re-verified against
the code at HEAD before triage; subagent severities were discarded and reassigned.

#### Decisions — resolved 2026-07-29 by Jerome

- [x] [Review][Decision] Every in-flight dispatch blanks the entire tenant's conversation list — `MarkPendingAsync` publishes a `Dispatches` entry before any summary exists (`ConversationProjectionReadModelWriter.cs:119-150`), so `ListAsync` hits `Dispatches.Count != summaries.Count` (`ConversationProjectionReadStore.cs:131-135`) for a new conversation, or `reference.LastAppliedEventPosition != summary.Freshness.LastAppliedEventPosition` (`:153`) for an existing one. Both throw `ConversationProjectionConsistencyException`, which `ConversationQueryHandler.cs:249` maps to `Rebuilding` with **zero** conversations for the whole tenant. Under steady write traffic a tenant's list is unavailable most of the time, keyed on one unrelated conversation. AC6 requires exposing cross-key inconsistency; it does not require tenant-wide scope. Options: (a) scope the fail-closed decision to the affected conversation and return the rest of the page, (b) keep tenant-wide fail-closed as the accepted trade-off and document it, (c) redesign the pending marker so it does not create a transient count mismatch. **RESOLVED: (a) scope the fail-closed decision to the affected conversation** — return the rest of the page; a conversation with an in-flight or inconsistent dispatch is excluded and reported Rebuilding for itself only.
- [x] [Review][Decision] No migration path — every pre-6.2 persisted read model becomes unreadable on deploy — `ConversationProjectedReadModels.DispatchId` and `ConversationProjectionIndexReadModel.Dispatches` are new members with `string.Empty` / empty-dictionary defaults, and no upcaster, backfill, or version discriminator appears anywhere in the diff. A persisted pre-6.2 value therefore fails `!hasDispatch` (`ConversationProjectionReadStore.cs:85-91`) on every detail read and `Dispatches.Count != summaries.Count` on every list read. Options: (a) accept it — Conversations is greenfield and nothing is deployed, (b) add an upcast/backfill path, (c) add a documented rebuild-on-deploy operator runbook. Note (a) is only safe if no environment holds populated projection state. **RESOLVED: (a) accept — Conversations is greenfield and no environment holds populated projection state.** No upcaster or backfill is added; record the assumption in the story.
- [x] [Review][Decision] The SM-C2 gate measures a closure this story provably did not touch — `measuredProductionClosure` is `["src/Hexalith.Conversations", "src/Hexalith.Conversations.Contracts"]`, and `git diff --name-only 29def44..dc69719 -- src/Hexalith.Conversations src/Hexalith.Conversations.Contracts` returns **0 files** (verified). `HP-LIST` is LINQ over a local `string[]` and `HP-OPEN` is a `Dictionary.TryGetValue` (`SmC2HotPathBenchmark.cs`), touching neither `ConversationQueryHandler` nor `ConversationProjectionReadStore`. The `post P95 <= 1.05 x baseline P95` assertion compares byte-identical code against itself, so the AC1 gate is structurally incapable of observing this story — including the `ListAsync` change below. The story already concedes this in T4 ("could not have failed for this story"), but the frozen inventory defines HP-LIST as the *canonical query path*. Options: (a) accept the recorded limitation as already disclosed, (b) extend the fixture to the real `ListAsync`/`ReadAsync` paths and re-measure both baseline and post, (c) escalate as an AC1 amendment. **RESOLVED: (b) extend the fixture to the real `ListAsync`/`ReadAsync` paths and re-measure both baseline and post** under the frozen envelope, so the AC1 gate can observe this story's hosting and projection changes.
- [x] [Review][Decision] `ListAsync` validates the whole tenant before paging, and each event now costs two full tenant-index rewrites — read side: the single tenant-index read was replaced by `1 + ceil(N/100) + ceil(M/100)` store reads covering **every** conversation in the tenant before `ConversationQueryHandler` applies `Skip/Take` (`ConversationProjectionReadStore.cs:137-197`); the `// never a per-conversation fan-out (NFR2, no N+1)` invariant was deleted. Write side: `MarkPendingAsync` and `PersistAsync` each run a full read-modify-write over the single tenant-index key (`ConversationAsyncProjectionHandler.cs:130-131`), roughly tripling store round-trips per event and creating a per-tenant write serialization point. The 2026-07-28 review accepted "bounded bulk/page reads" as the resolution; validating the entire tenant was not part of that resolution. Options: (a) validate only the requested page, (b) accept full-tenant validation as the price of fail-closed cross-key consistency, (c) re-scope NFR2. **RESOLVED: (a) validate only the requested page** — page first, then validate that page's rows, restoring read cost proportional to page size.
- [x] [Review][Decision] The legacy v1 decoder contract flipped from degrade-and-skip to throw — the deleted `DecodeEvents` carried an explicit rationale ("the skip never falsely degrades — fail-closed by construction"); the replacement throws `JsonException` on an unknown discriminator, non-positive sequence, non-`json` format, or empty payload, and `ConversationProjectionHandler.Project` calls `Decode` with no `try`/`catch`, so it propagates out of the v1 `IDomainProjectionHandler` seam with unspecified platform handling. Two approved authorities conflict: the 2026-07-28 review patch "Reject undecodable envelopes instead of silently dropping trailing events" versus AC4's "Legacy `IDomainProjectionHandler` preserved for v1 compatibility". Adding an event type in a future story now hard-fails both seams rather than degrading freshness. Options: (a) catch at the v1 seam and degrade there while the async route stays strict, (b) keep both strict and accept the v1 behaviour change, (c) revisit the 2026-07-28 patch. **RESOLVED: (a) catch at the v1 seam and degrade there; the async route stays strict.** Add a test for the v1 actor's behaviour on an undecodable envelope.
- [x] [Review][Decision] `Program.cs` registers `AddDataProtection()` — a generic platform gap patched inside the module — the same hunk that correctly removes `AddDaprClient()` (promoted to `EventStoreDomainServiceExtensions.cs:310`) adds `builder.Services.AddDataProtection();` at `Program.cs:33`. Its own comment says the dependency is the platform-owned `IQueryCursorCodec`. This is structurally the defect the story exists to fix, resolved the opposite way, against AC3 and the Never-list item "Never hide a generic platform gap behind Conversations — fix it in the owning public surface." There is no `AddEventStoreDomainServiceShouldOwnDataProtection` counterpart to the `DaprClient` test. Options: (a) promote it to `AddEventStoreDomainService` (a second EventStore promotion — needs approval and re-runs the 6.7 gate), (b) keep it in the module with a recorded justification that Data Protection is host policy, not platform capability. **RESOLVED: (a) promote to `AddEventStoreDomainService`** with a matching ownership test, mirroring the `AddDaprClient` fix. This is a second EventStore promotion — it requires a submodule commit and, under `require_remote: true`, remote availability.
- [x] [Review][Decision] Promotion state contradicts the recorded final record — the embedded record states **Result PASS** and "Gitlinks moved after the candidate: none", but re-running the story's own command at the same candidate `dc69719` returns `result: blocked` (exit 1) with `GITLINK_COMMIT_MISMATCH` on `references/Hexalith.EventStore` (declared, `require_remote: true`; recorded `b1d08da` vs submodule HEAD `c21a0bf`) and on `references/Hexalith.Memories` (`0c351ff` vs `5106c93`). Both moves sit uncommitted in the umbrella working tree; the EventStore delta is `efe9791` (nested deps) + `c21a0bf` (docs), touching no Conversations compile input. Separately, `ProjectionReadStorePopulationProofValidationTest` pins `warnings.Length == 3` with `UNDECLARED_GITLINK_CHANGE` for Commons, FrontComposer and Memories as the **expected passing state**, while Dev Notes → D1 still asserts "the final gate returns zero blockers and zero warnings". The Promotion Completion Invariant is currently false for a declared path. Options: (a) restore the drifted gitlinks (matches the recorded precedent for unrelated drift), (b) re-anchor the candidate to a new commit and regenerate the record, (c) declare the additional paths — a scope expansion needing approval. **RESOLVED: (b) re-anchor the candidate and regenerate the final record from measured state.** Declared scope is NOT expanded; the Commons/FrontComposer/Memories movements remain disclosed non-blocking warnings.
- [x] [Review][Decision] AC7's only runtime proof is opt-in, and dispatch-ledger keys grow without bound — `ConversationsAppHostRuntimeBoundaryTest` self-skips unless `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true` (set nowhere in the repo; there is no CI workflow directory), and when it does run it asserts only `GET /alive` on two resources — no command, no dispatch, no Server→EventStore interaction. The story's own notes record the default lane as "8 passed, 1 opt-in skipped". Separately, `projection:conversations-dispatch:{sha256}` keys are written once per dispatch (including failed ones and every rebuild) and never deleted, expired, or compacted, while `ListAsync` bulk-reads one per indexed conversation on every query. Options: decide whether the boundary lane becomes default-on (needs daprd/Redis in the ordinary lane) and whether ledger retention is TTL, delete-on-supersede, or an accepted operator task. **RESOLVED: strengthen the AC7 boundary lane to assert a real dispatch through the Server→EventStore boundary rather than liveness, and give dispatch-ledger keys a TTL sized to the platform redelivery window.**

#### Patches

- [x] [Review][Patch] A tenant with no conversations reports Rebuilding forever instead of an empty list [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:125] — APPLIED 2026-07-29
- [x] [Review][Patch] The 5-minute staleness gate makes replay or rebuild of any conversation older than 5 minutes impossible [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:93] — APPLIED 2026-07-29
- [x] [Review][Patch] Completed-ledger fast path returns Completed without proving either read-model key is durable [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:112] — APPLIED 2026-07-29
- [x] [Review][Patch] ConversationProjectionConsistencyException escapes three of five read-store consumers uncaught [src/Hexalith.Conversations.Server/Governance/ConversationPrivilegedOperationalJustificationService.cs:92] — APPLIED 2026-07-29
- [x] [Review][Patch] PrepareRebuildAsync silently drops sibling conversations from the tenant index [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:213] — APPLIED 2026-07-29
- [x] [Review][Patch] Request fingerprint spans per-delivery fields, so a benign redelivery becomes a terminal Failed [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:362] — APPLIED 2026-07-29
- [x] [Review][Patch] Decoder resolves any namespace prefix ending in a known alias [src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:163] — APPLIED 2026-07-29
- [x] [Review][Patch] ReadAsync returns an unvalidated foreign-tenant model instead of failing closed [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:57] — APPLIED 2026-07-29
- [x] [Review][Patch] Divergent write policies: detail overwrites unconditionally while the index is highest-position-wins [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs:69] — APPLIED 2026-07-29
- [x] [Review][Patch] `>=` in the dispatch-reference merge lets a same-position redelivery invalidate a healthy generation [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs:189] — APPLIED 2026-07-29
- [x] [Review][Patch] Cancellation between PersistAsync and CompleteDispatchAsync wedges fully-written correct data [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:131] — APPLIED 2026-07-29
- [x] [Review][Patch] Ledger CAS loops spin without backoff and report contention as PartialRetry, the same code the evidence binds to genuine partial writes [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:288] — APPLIED 2026-07-29
- [x] [Review][Patch] Rebuilding now discloses the existence of a conversation that was previously indistinguishable from absent [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:80] — APPLIED 2026-07-29
- [x] [Review][Patch] Key template can collide — identifier types permit `:` and the key is built by unescaped concatenation [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs:45] — APPLIED 2026-07-29
- [x] [Review][Patch] BulkReadAsync validates chunk membership with an O(chunk²) linear scan [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:227] — APPLIED 2026-07-29
- [x] [Review][Patch] ProjectAsync accepts an empty event batch where PrepareRebuildAsync rejects it [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:81] — APPLIED 2026-07-29
- [ ] [Review][Patch] Evidence `batchOperationCount: 2` contradicts the shipped three-operation rebuild plan, and the validator pins the stale constant [tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs:66]
- [x] [Review][Patch] ExecutedLiveBoundaryAssertions is incremented but never read — the documented anti-skip guard cannot fail [tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionGatewayDispatchLiveTests.cs:53] — APPLIED 2026-07-29
- [x] [Review][Patch] SM-C2 post evidence `sourceCommit` is the literal string `working-tree-candidate-from-29def44…`, bound to no revision [tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs:322] — APPLIED 2026-07-29
- [ ] [Review][Patch] SmC2BaselineReconstructionValidationTest skips when the source commit is unresolvable, and its `executedChecks == 3` anti-vacuity guard sits after the skip [tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs:130]
- [ ] [Review][Patch] The consumed-spec inventory exemption is satisfied by a self-attested flag in this story's own evidence [tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs:255]
- [x] [Review][Patch] The derived-state-deletion scenario deletes only two of the three derived key families, so the surviving-ledger path is never exercised [tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs:4446] — APPLIED 2026-07-29
- [ ] [Review][Patch] The dispatch-ledger key family is absent from the AC5 production-boundary evidence and its key assertions [tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs:2126]
- [ ] [Review][Patch] No test proves a second tenant cannot read the first tenant's projection records (ADR 0003 Verification 5) [tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs:1]
- [x] [Review][Patch] Nothing exercises the list query with two conversations — every end-to-end list proof uses a single-row tenant [tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs:1] — APPLIED 2026-07-29
- [x] [Review][Patch] ValidateDispatchIdentity and ComputeRequestFingerprint are never exercised in their failing direction [tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs:1] — APPLIED 2026-07-29
- [ ] [Review][Patch] The new detail Rebuilding branch in ConversationQueryHandler has no test [src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:95]
- [ ] [Review][Patch] Program.cs is executed by no test — host-composition tests re-type its registration sequence instead [tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainDiscoveryHostCompositionTest.cs:150]
- [ ] [Review][Patch] BulkReadAsync's hard requirement on IReadModelBulkStore has no test, and its failure maps to Unavailable rather than Rebuilding [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:204]
- [ ] [Review][Patch] The Git() test helper drains stdout and stderr sequentially and can deadlock; its ancestry assertion message is unreachable [tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs:2369]
- [ ] [Review][Patch] AppHost EventStore references are gated on `Configuration == 'Debug'`, and ScaffoldSmokeTest compensates by expecting the same path twice [tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs:197]
- [ ] [Review][Patch] The AppHost.Tests EventStore.Aspire reference drops the conditional source/package fallback pair every sibling reference uses [tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj:7]

#### Patch application status — 2026-07-29

**19 of 32 patches applied and verified. 13 remain open. Story stays `in-progress`.**

Measured after the applied set, Release, `-p:UseHexalithProjectReferences=true`:

| | |
| --- | --- |
| Release build | **0 warnings, 0 errors** |
| Total across 8 projects | **1,936 tests · 1 failed · 1 skipped** |
| Server | 642/642 (was 631 — 11 added) |
| Contracts · Domain · Client · Admin.Web · IntegrationTests | 618 · 185 · 29 · 14 · 14, all green |
| AppHost | 9, 1 opt-in skipped (the AC7 lane — still open, see D8) |
| Conformance | 425, **1 failed** |

The single failure is `ProofSourceAndSignedV1BindingsShouldRemainByteIdentical`: the v2 proof binds
production source hashes and this patch pass changed bound sources. That is the evidence guard doing
exactly what T1 added it for — it must be closed by regenerating the evidence, never by relaxing the pin.

**Additional defect found while applying D1/D4 — pre-existing, not caused by Story 6.2.**
`ConversationQueryHandler.HasMixedGenerations` compared `ProjectionCursor` across conversations, but that
cursor is the per-conversation applied position (`pos:0000000001`). Any tenant holding two conversations at
different event counts therefore listed as `Rebuilding` with an empty page. Verified byte-identical at
baseline `29def44`. It is also why every end-to-end list proof in the suite used a single-row tenant. The
check was removed — cross-key agreement is a per-conversation property, now verified per row — and
`ListShouldReturnConversationsSittingAtDifferentEventPositions` guards the regression.

**Correction to the finding as originally written:** `ConversationProjectionConsistencyException` escaped
**one** consumer, not three. `ConversationGovernanceVerificationService` and
`ConversationAuditRecordAccessService` both already catch `Exception` broadly. Only
`ConversationPrivilegedOperationalJustificationService` caught three specific types and missed it. The
governance service did have a separate real defect — it consumed a possibly-foreign-tenant read model with
no poison guard — which was fixed under the same item.

**Still open (13):** evidence regeneration and the vacuous conformance guards (P9–P13, P15, P27), the
remaining coverage gaps (P16 cross-tenant read, P19 detail Rebuilding branch, P20 executing `Program.cs`,
P31 non-bulk store), the AppHost csproj conditions (P28, P29), plus decisions D3 (SM-C2 re-measure), D7
(re-anchor + regenerate) and D8 (AC7 lane + ledger TTL). D6's source change is applied on both sides with
ownership tests, but the EventStore submodule commit and push are **not** made.

### Review Findings — pass 3, chunk 1 (2026-07-29, `bmad-code-review`, four blind layers)

Scope reviewed: runtime/production sources and focused tests in the baseline-to-`1b7a06b` diff. The four
layers raised 27 raw findings, deduplicated to 19 and re-verified against the code. One was dismissed:
foreign-tenant summaries are deliberately filtered before pagination, as the tenant-isolation test requires.
The live-boundary anti-skip finding is already the open pass-2 item beginning
`ExecutedLiveBoundaryAssertions is incremented but never read`; it counts in this pass's 18 patches and is
not duplicated below.

- [x] [Review][Patch] Advance list continuation by candidates consumed, not consistent rows returned (HIGH) [src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:343] — APPLIED 2026-07-29
- [x] [Review][Patch] Bind continuation tokens to the complete ordered index generation (HIGH) [src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:467] — APPLIED 2026-07-29
- [x] [Review][Patch] Encode opaque tenant and conversation identifiers instead of rejecting legal colon values (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs:45] — APPLIED 2026-07-29
- [x] [Review][Patch] Validate an existing rebuild ledger against operation identity before overwriting it (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:224] — APPLIED 2026-07-29
- [x] [Review][Patch] Preserve summary-less pending sibling dispatch references during rebuild (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:231] — APPLIED 2026-07-29
- [x] [Review][Patch] Require the completed-ledger fast path to prove matching detail and index generations (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:448] — APPLIED 2026-07-29
- [x] [Review][Patch] Prevent equal-position concurrent dispatches from splitting detail and index identities (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs:73] — APPLIED 2026-07-29
- [x] [Review][Patch] Fail live gateway tests on product startup and route-discovery faults instead of converting them to skips (HIGH) [tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs:166] — APPLIED 2026-07-29
- [x] [Review][Patch] Remove the released-ephemeral-port race from the live gateway fixture (MEDIUM) [tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs:144] — APPLIED 2026-07-29
- [x] [Review][Patch] Isolate process-global DAPR port environment mutations from every test collection (MEDIUM) [tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs:152] — APPLIED 2026-07-29
- [x] [Review][Patch] Require a successful health response before declaring the gateway app ready (MEDIUM) [tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs:482] — APPLIED 2026-07-29
- [x] [Review][Patch] Make the AppHost boundary lane enforce a real Server-to-EventStore dispatch instead of optional liveness only (HIGH) [tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs:31] — APPLIED 2026-07-29
- [x] [Review][Patch] Measure production create, append, list, and open paths in SM-C2 instead of toy substitutes (HIGH) [tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs:55] — APPLIED 2026-07-29
- [x] [Review][Patch] Cover matching detail/index data with a pending ledger through the real read-store validator (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:241] — APPLIED 2026-07-29
- [x] [Review][Patch] Cover privileged-query mapping of projection consistency failures (MEDIUM) [src/Hexalith.Conversations.Server/Governance/ConversationPrivilegedOperationalJustificationService.cs:92] — APPLIED 2026-07-29
- [x] [Review][Patch] Cover the governance verifier's second-read foreign-record poison guard (MEDIUM) [src/Hexalith.Conversations.Server/Governance/ConversationGovernanceVerificationService.cs:355] — APPLIED 2026-07-29
- [x] [Review][Patch] Add bounded retention for dispatch-ledger keys using the approved platform-redelivery-window TTL (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs:83] — APPLIED 2026-07-29

#### Patch application status — 2026-07-29

**18 of 18 chunk-1 patches are applied. Story stays `in-progress`.** The mandatory DAPR gateway lane passes
2/2 with zero skips. The mandatory AppHost runtime lane passes 1/1 after its real command exposed and fixed
the plural-resource/singular-DAPR-app-id topology mismatch. The production-path SM-C2 reconstruction is now
truthful and mechanically bound: CREATE and APPEND pass, while LIST (+297.19%) and OPEN (+1126.17%) fail the
5% Release threshold, so performance remains an open release blocker. Dispatch-ledger retention uses the
platform's validated 24-hour redelivery window; expired ledgers no longer expire matching durable
detail/index projections, while any present pending or poisoned ledger still fails closed.

#### Deferred

- [x] [Review][Defer] The tenant index remains a single state-store value, now also carrying a per-conversation dispatch map, with no size guard [src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexReadModel.cs:26] — deferred, pre-existing single-key index design
- [x] [Review][Defer] SameSummary compares serialized form, so any `[JsonIgnore]` or non-public member compares equal [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:273] — deferred, pre-existing comparison approach

### Review Findings — pass 4, chunk 1 (2026-07-29, `bmad-code-review`, four blind layers)

Scope reviewed: production/runtime sources in `git diff 29def441408becfbbbdc5c59b9af14a7717cb21f..966ed26 -- src/`.
The four layers raised 26 raw findings, deduplicated to 22 and re-verified against the code and platform
callers. Ten were dismissed as noise, previously resolved decisions, or unreachable under the platform's
dispatch invariants. Runtime tests, evidence/conformance, and workflow/generator/planning changes remain
separate review chunks.

#### Decisions

All decision-needed findings were resolved by Jerome on 2026-07-29 and converted to patch items below.

#### Patches

- [x] [Review][Patch] Add authoritative platform reconciliation for expired or terminally abandoned dispatch markers (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs:130] — APPLIED 2026-07-29 (decision option a)
- [x] [Review][Patch] Prebuild the EventStore gateway in the boundary test's exact configuration and verify revision provenance (HIGH) [src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj:13] — APPLIED 2026-07-29 (decision option b)
- [x] [Review][Patch] Handle persisted `ConversationRejectedDomainEvent` records as position-advancing projection no-ops (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:43] — APPLIED 2026-07-29
- [x] [Review][Patch] Bound non-cancellable dispatch-ledger completion with an independent timeout (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:158] — APPLIED 2026-07-29
- [x] [Review][Patch] Revalidate dispatch-ledger identity and status before marking the reloaded record completed (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:418] — APPLIED 2026-07-29
- [x] [Review][Patch] Preserve valid legacy-v1 events when one envelope is undecodable instead of replacing the entire generation with empty state (HIGH) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs:103] — APPLIED 2026-07-29
- [x] [Review][Patch] Replace sequential per-sibling ledger reads in rebuild planning with a bounded bulk operation (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:270] — APPLIED 2026-07-29
- [x] [Review][Patch] Pin full freshness-record generation equality, not only cursor mismatch, in read-store and read-service tests (MEDIUM) [tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadStoreFailClosedTest.cs:170] — APPLIED 2026-07-29
- [x] [Review][Patch] Cover the detail-query `Rebuilding` result when projection content is withheld (MEDIUM) [src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:91] — APPLIED 2026-07-29
- [x] [Review][Patch] Cover list consistency and infrastructure exception mappings from both list and page validation (MEDIUM) [src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:247] — APPLIED 2026-07-29
- [x] [Review][Patch] Prove unsupported-route, empty-history, and decode-failure handler outcomes write no projection keys (MEDIUM) [src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:86] — APPLIED 2026-07-29
- [x] [Review][Patch] Move `ConversationProjectionDispatchStatus` to its own C# file (LOW) [src/Hexalith.Conversations.Server/Projections/ConversationProjectionDispatchLedger.cs:19] — APPLIED 2026-07-29

#### Patch application status — pass 4, chunk 1 (2026-07-29)

**12 of 12 selected patches are applied. Story stays `in-progress`.** The authoritative reconciliation
contract was added to EventStore and exercised through the durable named-dispatch retry ledger; Conversations
retries convergence first and compensates only a still-pending marker whose detail generation did not advance.
The AppHost boundary now prebuilds the gateway in the active test configuration and verifies a clean commit SHA
or a dirty-worktree SHA plus content hash before launch.

Validation: Conversations Server **668/668**; AppHost Debug **9/9**; AppHost Release live boundary **1/1**;
EventStore DomainService focused **72/72** and broad non-nested-submodule lane **111/111**; EventStore Server
**2,870 passed, 25 pre-existing ATDD skips, 0 failed**. Relevant Debug and Release builds completed with
0 warnings/errors, both repository diffs pass `git diff --check`, and root affected-project formatting passes.
Full EventStore DomainService remains **147 passed / 1 failed** only because the forbidden-to-initialize nested
`references/Hexalith.Tenants` authoring-guard subject is absent. EventStore-wide `dotnet format` remains noisy
from its pre-existing `.editorconfig` CRLF versus `.gitattributes` LF conflict and unrelated naming diagnostics;
the compiled analyzers and focused tests are clean.

## Dev Notes

### Binding sequence — check before starting

`6.1 -> 6.7 -> 6.2`, and 6.2 precedes 6.5; 6.6 is last.
Story 6.1 is `done`. Story 6.7 is `done`. Both prerequisites are satisfied.
The frozen SM-C2 benchmark is also a pre-change gate for 6.2 (see T4).

### Ownership spine — the rule this story exists to enforce

Conversations owns contracts, aggregate/domain behavior, validators, handlers,
projections/read-model semantics, domain adapters, domain telemetry definitions,
client/testing assets, optional domain UI, **and one non-packable, non-publishable
module-scoped AppHost limited to local Conversations user and E2E tests**.

It is **not** a production or deployment composition root. Platform deployment owns
production topology and composition. `EventStore.DomainService`, `EventStore.ServiceDefaults`,
and `EventStore.Aspire` own generic hosting, endpoints, DAPR resources, health, telemetry
wiring, query/projection runtime, and subscriptions.

Canonical host pair — never teach or call `MapEventStoreDomainService()` directly:

```csharp
builder.AddEventStoreDomainService(/* domain assemblies/options */);
app.UseEventStoreDomainService();
```

FR landing zones: FR-10 → EventStore.ServiceDefaults + DomainService · FR-11 →
Commons.TenantAccess · FR-12 → Commons.Http · FR-13 → Platform deployment + EventStore.Aspire ·
FR-14 → Commons.Serialization · FR-15 → Commons.Diagnostics + EventStore domain telemetry ·
FR-16 deferred.

### Hard prohibitions (from the frozen spec's Never list)

- Never pack, publish, deploy, or describe `Hexalith.Conversations.AppHost` as production
  composition; never move the harness into `FrontComposer.AppHost` or `EventStore.AppHost`.
- Never add a Conversations Aspire/runtime facade or retain generic ServiceDefaults, DAPR,
  health, telemetry, query, projection, publication, or subscription plumbing.
- Never hide a generic platform gap behind Conversations — fix it in the owning public surface.
- Never introduce direct DAPR state writes, query-time replay, or silent backfill.
- Never claim atomic immediate multi-key persistence. The dispatch ledger and index marker make
  sequential partial progress observable; uncertainty is **non-completion**, and queries must expose
  cross-key generation inconsistency rather than repair it on read. Coordinated rebuild promotion remains
  platform-owned through the batch protocol.
- Never treat direct writer calls, DI resolution, mock counts, HTTP acceptance, or the legacy
  opaque `ProjectionResponse` as population proof.
- Never mutate signed v1 evidence, frozen Epic 1–5 history, retrospectives, the historical
  epic prefix, or Story 6.5 authoring-template evidence.
- Never initialize, update, fetch, or traverse **nested** submodules; root `.gitmodules` only.
  No `git submodule update --init --recursive` or `--remote`.
- Never `git add -A` / `git commit -a`. Stage only declared File List paths — this is the
  failure that required review correction in Stories 2.2, 3.3, and 6.1.

### Open decision D1 — undeclared changed gitlinks — **RESOLVED 2026-07-27 (option 2)**

Jerome approved declaring all three changed root gitlinks with `require_remote: true`. The
frontmatter records this as an approved AC-1c scope expansion. The two warnings below became
evaluated declared scope, and the final gate returns **zero blockers and zero warnings**.

The original decision text is kept below unchanged for the audit trail.


The gate at baseline `29def44` → HEAD returns `pass` with **two non-blocking warnings**:

```
UNDECLARED_GITLINK_CHANGE  references/Hexalith.Builds   (bb02cdc8…, clean, captured)
UNDECLARED_GITLINK_CHANGE  references/Hexalith.Tenants  (4ca5f86f…, clean, captured)
```

Per the frozen contract these paths **join the affected set and are fully evaluated anyway**
— both are initialized, clean, and exactly captured at mode `160000`, with
`require_remote` defaulting to `false`. So they do not block.

AC 1c forbids expanding a declaration without approval. Three options, Jerome's call:
1. Leave declared scope as EventStore only and accept the two warnings (status quo, passes).
2. Declare all three paths with the appropriate `require_remote` (expanded scope → approval).
3. Move this story's `baseline_commit` forward so those gitlinks fall outside the window
   (changes what the gate proves — least honest of the three).

**Do not resolve this by editing frontmatter unilaterally.**

`require_remote: true` is the correct value for `references/Hexalith.EventStore`: the
submodule is shared with other clones, so a local-only commit would leave the umbrella
recording a gitlink nobody else can resolve. `true` is the default for `references/...`.

### Reading the promotion gate's output

Exit `0` = pass (warnings may still be present) · `1` = valid invocation with blockers ·
`2` = invalid invocation or untrustworthy repository state. Exit-2 codes
(`GIT_COMMAND_FAILED`, `BASELINE_NOT_ANCESTOR`, `INVALID_SCOPE`, `INTERNAL_ERROR`) appear in
`blockers[]` but are **not** in the frozen blocker table — disambiguate on `result: "error"`.

Blockers you could plausibly hit here: `SUBMODULE_DIRTY_TRACKED`, `SUBMODULE_DIRTY_UNTRACKED`,
`REMOTE_COMMIT_UNAVAILABLE`, `GITLINK_COMMIT_MISMATCH`, `GITLINK_MODE_NOT_160000`,
`UNCAPTURED_SUBMODULE_PROMOTION`.

Warnings: `UNDECLARED_GITLINK_CHANGE` (path still joins the affected set and is fully
evaluated), `UNRELATED_SUBMODULE_DIRTY`, `UNRELATED_GITLINK_DRIFT`, `BASELINE_NOT_PROVIDED`,
`SCOPE_NOT_EVALUATED`. The last two are treated as **blockers** by every gated workflow when
scope is declared — a run that evaluated nothing is not a pass.

### Current state of the code — read these before touching anything

| Path | State | Notes for this story |
| --- | --- | --- |
| `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` | UPDATED | `IsPackable`/`IsPublishable` = `false`; `Aspire.AppHost.Sdk/13.4.6`; conditional `HexalithEventStoreRoot` project refs for source mode |
| `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` | UPDATED | Public EventStore.Aspire helpers only; 3 project resources; JWT bearer security applied to gateway + server; Keycloak realm path resolution retained |
| `src/Hexalith.Conversations.Server/Program.cs` | UPDATED | Canonical two-line host + `AddConversationTenantAccess()` + `AddConversationQueries(...)`. **No `AddDaprClient`** — the platform owns it now |
| `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs` | NEW | The AC4 production route. `ProjectAsync` = sequential writer; `PrepareRebuildAsync` = platform batch plan with `CreateOnly`/`Match(ETag)` concurrency |
| `…/ConversationProjectionHandler.cs` | UPDATED | Legacy v1 synchronous route; decoding/materialization now shared via the decoder |
| `…/ConversationProjectionEventDecoder.cs` | NEW | Shared decode used by both routes — keep them sharing it |
| `…/ConversationProjectionReadModelWriter.cs` | EXISTING | Two-key sequential persistence; idempotent replace/merge is what makes retry converge |
| `…/ConversationProjectionReadModelKeys.cs` | EXISTING | `projection:conversations:{tenant}:{conversation}` and `projection:conversations-index:{tenant}`, store `statestore` |
| `…/ConversationProjectionReadStore.cs`, `…ReadService.cs`, `Queries/ConversationQueryHandler.cs` | UPDATED | Queries must not trust a detail/index generation until both keys agree; no query-time backfill |
| `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs` | NEW | The AC5 proof lane — see T2 |
| `tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs` | NEW | The SM-C2 fixture — see T4 |
| `tests/…Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | NEW | Validates the v2 evidence — see T1 |
| `src/Hexalith.Conversations.ServiceDefaults/`, `tests/…ServiceDefaults.Tests/` | REMOVED | Untracked `bin`/`obj` leftovers remain on disk; harmless, but clean them so a stray `obj` cannot be globbed |
| `references/Hexalith.EventStore/src/…/EventStoreDomainServiceExtensions.cs:310` | PROMOTED | `builder.Services.AddDaprClient()` — the promoted generic capability |

### Test harness facts you will need

- **Package-mode restore is broken here and it is not your bug.** `dotnet restore` fails
  `NU1102` on unpublished EventStore proof versions
  (`Hexalith.EventStore.Contracts`/`.Client` at `999.1.20-proof.…`). Use the approved source
  fallback on **every** restore/build/test command:
  `-p:UseHexalithProjectReferences=true`.
- **xUnit v3 / Microsoft.Testing.Platform:** do not rely on project-level
  `dotnet test --filter`. Build the test project, then invoke the built executable with
  single-dash `-class` / `-method`. Run test projects individually; use the `.slnx` for
  restore/build only.
- **xunit is pinned to stable 3.2.2 across the umbrella.** Do not let a submodule bump it to
  a 4.0.0-pre — that breaks umbrella restore with `NU1608`.
- Nine test projects, 1,887 tests at the Story 6.1/6.7 baseline; conformance is now **412**
  (408 + 4 from the new proof validator).

### Previous-story intelligence — the review patterns that will be applied to you

Stories 6.1 and 6.7 were both sent back by adversarial four-layer review. Every finding below
is a pattern a reviewer will look for in *this* story:

- **Guards that cannot fail.** 6.1: a "historical binding" that compared a declared hash
  against the commit it was computed from — 19/19 tautological, 0 able to fail. 6.7: a
  boundary allowlist containing exactly the seven violations it existed to catch. **T3 is the
  same shape in this story.**
- **Substring guards.** `"5% P95"` ⊂ `"45% P95"` made the only numeric performance guard
  vacuous. Anchor numbers numerically and assert polarity-bearing clauses verbatim.
- **Evidence bound to no single revision.** 6.7 recorded counts from a tree that was never the
  candidate. **T1 is the same shape in this story.**
- **Hand-maintained "completeness" checks.** 6.7's File List test compared prose against a
  constant in the same file. Derive from `git diff --name-only`.
- **Gitlinks swept into a commit.** 2.2, 3.3, 6.1 all required correction for this. Stage only
  declared paths.
- **Undisclosed scope.** 6.7 shipped four undeclared gitlink promotions while its frontmatter
  asserted it was not promotion-bearing. Frontmatter is what the gate and downstream agents
  parse — prose corrections do not reach them.
- **Fault injection must target acceptance criteria, not hashes.** 6.1 pass 1 injected only
  SHA-256/byte mutations — the checks that cannot fail to notice. Inject one mutation per AC.

### Scope this story does not touch

- No PRD, feature, journey, interaction, accessibility, or FrontComposer behavior change
  (`sprint-change-proposal-2026-07-27.md` §3). UX is preservation-only and belongs to Story 6.4.
- No public contract change. No Epic 1–5 history, retrospective, or signed v1 evidence change.
- No FrontComposer.AppHost or EventStore.AppHost selection or modification.
- No activation of FR-16 or of any preserved feature scope.

### Project Structure Notes

The target tree keeps `src/Hexalith.Conversations.AppHost/` and
`tests/Hexalith.Conversations.AppHost.Tests/` with their solution entries — this is the v3
exception, and it is narrow. It excludes any reusable Conversations Aspire library and any
generic ServiceDefaults facade. `Hexalith.Conversations.slnx` currently lists 7 `src` and 8
`tests` projects; `ServiceDefaults` and `ServiceDefaults.Tests` were correctly removed from
both `src`/`tests` folders and the solution.

Only the `.slnx` solution format is used. One C# type per file. File-scoped namespaces,
Allman braces, `_camelCase` private fields, primary constructors preferred, XML docs on all
public/protected/internal members, `TreatWarningsAsErrors=true`.

### References

- [Source: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` → *Story 6.2* (v2 AC 1–6) and *Appendix: 2026-07-27 Module Test-AppHost Authority Amendment* → *Story 6.2 Corrected Acceptance* (v3 AC 1–5)]
- [Source: `_bmad-output/implementation-artifacts/epic-6-context.md` → *Corrected Ownership Spine*, *Projection Read-Store Population (ADR 0003)*, *SM-C2 Contract*, *Stories → 6.2*, *Binding Sequence*, *Promotion Completion Invariant*]
- [Source: `_bmad-output/planning-artifacts/architecture.md` → *SM-C2 Versioned Hot-Path Inventory And Gate*, *Still-Binding Domain And Runtime Decisions*, *Projection Read-Store Population Decision*, *Promotion Completion Invariant*, *Corrective Readiness*]
- [Source: `docs/adrs/0003-projection-read-store-population-proof.md` → *Decision* items 1–6, *Consequences*, *Verification* items 1–8]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27.md` → *Corrected Ownership Invariant*, *Frozen Story 6.2*, *Success Criteria*]
- [Source: `_bmad-output/implementation-artifacts/spec-6-2-migrate-conversations-to-platform-owned-hosting-2.md` → *Intent Contract*, *Code Map*, *Tasks & Acceptance*, *Verification*]
- [Source: `_bmad-output/implementation-artifacts/6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md` → *Dev Notes → Checker contract* (blocker/warning code tables, exit codes), *Review Findings pass 2*]
- [Source: `docs/runbooks/submodule-promotion-completion-gate.md`]
- [Source: `_bmad-output/project-context.md` → framework, testing, workflow, and critical don't-miss rules]
- [Source: `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` → technology stack, DDD architecture, C# standards, testing standards, Git rules]

## Verification

Run every command with `-p:UseHexalithProjectReferences=true`.

```bash
dotnet restore Hexalith.Conversations.slnx -p:UseHexalithProjectReferences=true
dotnet build Hexalith.Conversations.slnx --configuration Release -m:1 -p:NuGetAudit=false -p:UseHexalithProjectReferences=true
dotnet msbuild src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj -getProperty:IsPackable -getProperty:IsPublishable
```

Expected: restore succeeds in source mode; build 0 warnings / 0 errors with the AppHost
retained and Conversations ServiceDefaults absent; both properties `false`.

```bash
tests/Hexalith.Conversations.AppHost.Tests/bin/Release/net10.0/Hexalith.Conversations.AppHost.Tests -class Hexalith.Conversations.AppHost.Tests.ConversationsAppHostTopologyTest
tests/Hexalith.Conversations.Server.Tests/bin/Release/net10.0/Hexalith.Conversations.Server.Tests -class Hexalith.Conversations.Server.Tests.Projections.ConversationAsyncProjectionHandlerTest
tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests -class Hexalith.Conversations.IntegrationTests.Projections.ConversationProjectionReadStorePopulationLiveTests
tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest
tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests
```

Expected: 8/8, 6/6, 2/2, 4/4, and the full conformance suite green with **0 skipped**.
A skip in the live population lane is a failure — AC5 is not satisfied by a skipped test.

```bash
python3 _bmad/scripts/verify_submodule_promotion.py \
  --repository . \
  --baseline 29def441408becfbbbdc5c59b9af14a7717cb21f \
  --candidate <final-committed-umbrella-revision> \
  --submodule references/Hexalith.EventStore \
  --require-remote references/Hexalith.EventStore \
  --format json
git diff --check
```

Expected: exit 0, `result: pass`, zero blockers, and `evaluated` non-empty (a run reporting
`SCOPE_NOT_EVALUATED` or a vacuous zero-scope pass proves nothing). `git diff --check` clean.

Run `git diff --check` **and** `git diff --check <baseline>` — the bare working-tree form
passed in Story 6.7 while the range form reported a blank line at EOF. Report both.

Run the full nine-project regression before completion; the Story 6.1/6.7 baseline is
1,887/1,887 with 0 failed and 0 skipped.

## Dev Agent Record

### Agent Model Used

`claude-opus-5` (BMAD `dev-story` workflow, 2026-07-27).

`Codex (GPT-5)` (BMAD `code-review` workflow, 2026-07-28).

`Codex (GPT-5)` (BMAD `dev-story` workflow, 2026-07-29).

### Debug Log References

**Blocker encountered and resolved — `SUBMODULE_DIRTY_UNTRACKED` (`references/Hexalith.Tenants`).**
With the approved three-path declaration the gate returned `result: blocked`, exit `1`:
`references/Hexalith.Tenants contains untracked files`. The untracked file was
`_bmad-output/implementation-artifacts/spec-gh-actions-30291329462-90061623240.md`, a draft
bugfix spec for the Tenants release-version floor, written at `21:27:48` — under a minute
before it was found, by a concurrent session working in that repository. It was unrelated to
Story 6.2. Verified empirically that narrowing the declaration would **not** have avoided it:
with only `references/Hexalith.EventStore` declared, Tenants still joined the affected set and
still blocked, adding two `UNDECLARED_GITLINK_CHANGE` warnings on top. Jerome chose to park the
draft outside the submodule rather than commit, delete, or halt. It was moved byte-identically
(`sha256 9b0bc8c2…`, verified before and after) to
`<session-scratchpad>/parked-from-Hexalith.Tenants/`. No Tenants commit, push, or gitlink move
was made. Committing it there instead would have advanced the Tenants `HEAD` past the recorded
gitlink `0ded4a1` and dragged unrelated Tenants work into this story's promotion scope.

**Regression found and fixed — public event vocabulary widened from 13 to 26 names.**
The full regression caught `ConversationProjectionHandlerTest.PublicEventRegistryShouldExposeTheLegacyThirteenEventNames`
failing at `953bf71`. Commit `953bf71` fixed a real production defect — a persisted envelope
names events by the durable CLR type (`…DomainEvent`), which suffix resolution can never match
against the public name, so every replayed event was silently dropped — but it fixed it by
registering both names in the *same* registry, which widened the public
`PublicEventTypeEntries` map from 13 entries to 26. That is a public contract change this story
is prohibited from making. Fixed by splitting the durable aliases into a separate
`DurableEventTypes` registry consulted only by `TryResolvePublicEventType`, leaving the public
vocabulary at exactly 13. New guard `DurableAliasesShouldNotWidenThePublicEventVocabulary`
asserts the two registries stay disjoint, 13 each, and correctly paired.

**Fault injection — one mutation per guard, all confirmed able to fail.**

| Mutation | Target | Result |
| --- | --- | --- |
| Candidate repointed to the baseline `29def44` | T1 live re-derivation | **2 failed** |
| `residualGap: "gateway coordinator not crossed"` | T2 gateway evidence | **1 failed** |
| `inMemoryFakeUsed: true` | T2 DAPR store claim | **1 failed** |
| `references/Hexalith.Tenants` dropped from declared scope | T1 declared-vs-changed scope | **1 failed** |
| `Directory.Build.targets` setting `IsPackable=true`, csproj XML untouched | T3 non-shipping boundary | **1 failed** |

Evidence file restored byte-identically after each injection (`diff` confirmed), and the
injected `Directory.Build.targets` was removed with the worktree left clean.

**Promotion gate at candidate `953bf71`** — exit `0`, `result: pass`, **0 blockers, 0 warnings**;
`changed_gitlinks` exactly equal to the declared set; all three paths initialized, clean,
remote-available, `recorded_mode 160000`, `recorded_gitlink == head`.

**Environment notes.** Every restore/build/test used `-p:UseHexalithProjectReferences=true`;
package-mode restore remains independently broken by the documented `NU1102` unpublished
EventStore proof versions. Test projects were run as built xUnit v3 executables.

**Stale proof binding repaired (2026-07-29).** The fresh regression correctly turned red
because root gitlinks moved after the proof's recorded candidate. The current promotion
checker run from baseline `29def44` to committed candidate `7472632` passes with zero
blockers under the existing three approved declarations. It reports three
`UNDECLARED_GITLINK_CHANGE` warnings for later unrelated Commons, FrontComposer, and
Memories movements; those paths were evaluated clean and exactly captured without silently
expanding this story's scope. The proof JSON, Markdown summary, and conformance pins were
rebound to that measured state, the focused proof validator returned green, and a fresh
Release build plus all eight test-project artifacts completed green, including the opt-in
live AppHost boundary with no skip.

**Final-record generation gate blocked (2026-07-28).** Candidate
`65b3ed4180b7ab105763b2a8f491e1a899466c33` passed the promotion checker with zero blockers
and two disclosed `UNDECLARED_GITLINK_CHANGE` warnings. The final-record generator parsed all
eight TRX artifacts (1,925 passed, 0 failed, 0 skipped), resolved the candidate, and found the
record section, but returned `blocked` with these stable diagnostics:

- `SUBMODULE_INTERNAL_PATH` (10 occurrences): the legacy review-patch list includes the six
  EventStore production paths and four EventStore test paths under
  `references/Hexalith.EventStore`. Those paths belong to the submodule's record; the umbrella
  record must contain only the root gitlink promotion.
- `FILE_LIST_DRIFT` (2 occurrences): the existing record is missing 32 derived umbrella paths,
  carries 13 unexpected entries, and has one `### File List` heading spanning two path lists.
  The required remediation is to replace the complete File List region with the generator's
  rendered block. The workflow forbids that insertion until the JSON pre-gate returns `pass`,
  so completion remains halted without hand-editing counts, paths, or commits into agreement.

**Final-record remediation completed (2026-07-29).** The legacy two-list inventory was
replaced from the generator's derived entries and committed at candidate `e74e542`. The
promotion checker returned `pass` with zero blockers under the unchanged three-path approved
scope; Commons, FrontComposer, and Memories remain disclosed non-blocking changed-gitlink
warnings. The final-record generator then returned `pass`: all three derivation inputs true,
eight TRX artifacts parsed, 86 paths with zero missing or unexpected entries, and computed
totals of 1,925 passed, 0 failed, and 0 skipped. Its Markdown block below is inserted verbatim.

**Historical-record regression repaired (2026-07-29).** The Story 6.8 workflow tests still
classified Story 6.2 as a pre-generator record and required the two legacy File List warnings.
Those expectations turned red after the mandatory current workflow generated this record.
They now require the stronger generated-record invariants: one generated block, a derived list
identical to the declared list, no drift, no warnings, and read-only historical verification.
The generator and promotion workflow suite passes 129/129.

**Final candidate re-anchored (2026-07-29).** Because the historical-record regression is a
tracked completion guard, it and the completion metadata were committed before the gates at
candidate `dc69719`. The complete Release build and all eight test projects were rerun after
that commit. Both gates pass at the new fixed candidate with the same 86 derived paths and
computed totals of 1,925 passed, 0 failed, and 0 skipped; the workflow suite remains 129/129.

### Completion Notes List

- **2026-07-29 workflow compatibility:** updated the historical verifier regression to treat
  this completed Story 6.2 record as generated while preserving strict blocking behavior for
  malformed generated records; all 129 generator/promotion tests pass.

- **2026-07-29 review handoff:** the mechanical final record now passes at fixed candidate
  `dc69719a9ce7c25bb9755827f19c7e1ce2a87287`, with the generator-derived File List and
  test totals embedded verbatim. Story 6.2 is ready for independent code review.

- **2026-07-29 completion revalidation:** repaired the proof's stale candidate binding from
  live gate output without widening the approved promotion scope. Fresh Release restore/build,
  all eight Conversations test projects, DAPR/Redis production-boundary integration, and the
  opt-in live AppHost boundary completed successfully and produced new TRX artifacts for the
  mechanical final record.

- **Code-review patch pass (2026-07-28): all 18 selected findings implemented.** Added a stable
  pending/completed dispatch ledger, fail-closed cross-key/ledger validation, bounded 100-key platform bulk
  pages, strict envelope decoding, typed deterministic rebuild rejection, sanitized three-key rebuild plans,
  and complete discriminator/event coverage. The real AppHost launch exposed and fixed two additional
  production blockers hidden by model-only tests: Conversations had no Aspire HTTP endpoint and Program did
  not register the Data Protection provider required by the cursor codec.
- **Review validation:** Conversations Server **631/631**; focused projection suite **121/121**; full Dapr-backed
  integration **14/14**; opt-in real AppHost Server/EventStore boundary **1/1**; ordinary AppHost suite **8 passed,
  1 opt-in skipped**; EventStore dispatcher **30/30**. Relevant builds completed with 0 warnings/errors when
  project references were isolated from the repository's existing mixed package/source graph.
- **Honest review blockers:** full Conformance is **417/418** because the immutable v2 proof correctly detects
  changed source hashes; full EventStore DomainService is **146/147** because its nested Tenants subject is not
  initialized and repository policy forbids initializing nested submodules; EventStore Client tests remain
  unbuildable due their pre-existing references to source folders excluded by the current aggregate project.
  The promotion gate is `blocked` by `SUBMODULE_DIRTY_TRACKED` and `SUBMODULE_DIRTY_UNTRACKED` in
  `references/Hexalith.EventStore`, as expected for an uncommitted promotion. No commit, push, staging, signed
  evidence rewrite, dependency update, or nested-submodule initialization was performed.

- **T1 (blocking) closed.** Evidence rebound from the stale `0eb3657` / `b11b0c7` pair to
  `c8c7003` / candidate `953bf71` with the real gate output. Assertions were **added, not
  relaxed**: declared scope must equal the gitlinks that actually changed, every evaluated path
  must be clean/initialized/remote-available at mode `160000`, and the promoted-capability
  delta must be empty. The EventStore delta `0eb3657..c8c7003` was measured directly in the
  submodule — 2 commits, neither promoted-capability file touched, `AddDaprClient()` still at
  line 310 — and recorded. The self-reference problem (a gate result cannot name the commit
  containing it) is handled by pinning the last revision that moved a gitlink or production
  source and re-deriving that binding from git on every conformance run, so the evidence goes
  **red** rather than stale if a declared gitlink moves later.
- **T2 (blocking) closed by strengthening, not by narrowing.** Jerome chose option (a). The
  gateway lane crosses `ProjectionUpdateOrchestrator` and `NamedProjectionDispatchCoordinator`
  into a real `daprd` sidecar with a Redis-backed `statestore`, and asserts the production
  query result. ADR 0003 Verification 1-2 is satisfied as written; `residualGap: "none"`.
- **T3 closed and proven falsifiable** by the `Directory.Build.targets` injection — the exact
  scenario the old XML-reading assertion could not catch.
- **T4 closed.** Reconstruction provenance recorded in both artifacts and validated by git
  rather than asserted, with an anti-vacuity `executedChecks == 3` guard. The residual
  limitation is stated plainly: this gate could not have failed for this story.
- **T5 closed.** Both 6.2 specs are `superseded`; one live authority remains. File List derived
  mechanically (49 paths).
- **Regression suite: 1,908 passed, 0 failed, 0 skipped** across 8 test projects. The
  Story 6.1/6.7 baseline of 1,887 across *nine* projects is superseded: AC3 removed
  `Hexalith.Conversations.ServiceDefaults.Tests`, so eight projects is the correct count now.
  Conformance is **418** (was 412; +4 SM-C2 reconstruction, +2 promotion re-derivation and
  gateway evidence, and the durable-vocabulary guard lands in Server.Tests).
- **Release build: 0 warnings, 0 errors.**
- **Honest exceptions, not silently absorbed:**
  - `git diff --check` is **not** clean in the working tree. Both findings
    (`_bmad/render/bmad-quick-dev/step-05-present.md:88`,
    `step-oneshot.md:95`, new blank line at EOF) are in files modified **before this session
    started** and outside this story's File List. They were left untouched rather than
    "fixed", per the preserve-user-changes rule. The committed range is clean.
  - An untracked
    `_bmad-output/implementation-artifacts/spec-gh-actions-30291329462-90061623240.md`
    (7,059 bytes, `sha256 feb883e2…`) appeared in the umbrella at `21:45:43` from the same
    concurrent session. It is a different, later revision of the parked Tenants draft. It was
    **not staged** and does not affect the promotion gate, which evaluates submodule state.
  - AC1's SM-C2 gate confirms no regression but could not have failed for this story, because
    no source inside the measured closure changed. Recorded in the evidence, not glossed.

<!-- STORY-FINAL-RECORD:BEGIN -->

**Final record** — `story-final-record-v1`, result **PASS**, mode `live`. The JSON document is authoritative; this Markdown is rendered from it.

Derived: test results **yes**, candidate **yes**, record section **yes** · 8 test artifact(s) parsed · 86 file-list path(s) · 6 gitlink promotion(s) evaluated.

Baseline `29def441408becfbbbdc5c59b9af14a7717cb21f` → candidate `dc69719a9ce7c25bb9755827f19c7e1ce2a87287`.

### File List

- `.agents/skills/bmad-code-review/steps/step-04-present.md` (modified)
- `.agents/skills/bmad-dev-story/SKILL.md` (modified)
- `.agents/skills/bmad-quick-dev/step-05-present.md` (modified)
- `.agents/skills/bmad-quick-dev/step-oneshot.md` (modified)
- `.claude/skills/bmad-code-review/steps/step-04-present.md` (modified)
- `.claude/skills/bmad-dev-story/SKILL.md` (modified)
- `.claude/skills/bmad-quick-dev/step-05-present.md` (modified)
- `.claude/skills/bmad-quick-dev/step-oneshot.md` (modified)
- `Hexalith.Conversations.slnx` (modified)
- `README.md` (modified)
- `_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md` (modified)
- `_bmad-output/implementation-artifacts/6-8-generate-the-final-story-record-mechanically-from-measured-state.md` (new)
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified)
- `_bmad-output/implementation-artifacts/epic-6-context.md` (modified)
- `_bmad-output/implementation-artifacts/spec-6-2-migrate-conversations-to-platform-owned-hosting-2.md` (new)
- `_bmad-output/implementation-artifacts/spec-6-2-migrate-conversations-to-platform-owned-hosting.md` (modified)
- `_bmad-output/implementation-artifacts/spec-gh-actions-30291329462-90061623240.md` (new)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)
- `_bmad-output/planning-artifacts/architecture.md` (modified)
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` (modified)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md` (new)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern.md` (new)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-release-owner-decision-ledger-closure.md` (new)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28.md` (new)
- `_bmad/render/bmad-quick-dev/spec-template.md` (modified)
- `_bmad/render/bmad-quick-dev/step-02-plan.md` (modified)
- `_bmad/render/bmad-quick-dev/step-04-review.md` (modified)
- `_bmad/render/bmad-quick-dev/step-05-present.md` (modified)
- `_bmad/render/bmad-quick-dev/step-oneshot.md` (modified)
- `_bmad/scripts/generate_story_record.py` (new)
- `_bmad/scripts/tests/test_generate_story_record.py` (new)
- `_bmad/scripts/tests/test_verify_submodule_promotion.py` (modified)
- `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` (new)
- `docs/release-evidence/conformance-oracle-tiering-decision-v2.md` (new)
- `docs/release-evidence/projection-read-store-population-proof-v2.json` (new)
- `docs/release-evidence/projection-read-store-population-proof-v2.md` (new)
- `docs/release-evidence/sm-c2-hot-path-baseline-v1.json` (new)
- `docs/release-evidence/sm-c2-hot-path-baseline-v1.md` (new)
- `docs/release-evidence/sm-c2-hot-path-post-v1.json` (new)
- `docs/release-evidence/sm-c2-hot-path-post-v1.md` (new)
- `docs/runbooks/story-final-record-generation.md` (new)
- `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` (modified)
- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` (modified)
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` (modified)
- `src/Hexalith.Conversations.Server/Program.cs` (modified)
- `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs` (new)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectedReadModels.cs` (modified)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionConsistencyException.cs` (new)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionDispatchLedger.cs` (new)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionDispatchReference.cs` (new)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs` (new)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs` (modified)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexReadModel.cs` (modified)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` (modified)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs` (modified)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs` (modified)
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs` (modified)
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs` (modified)
- `src/Hexalith.Conversations.ServiceDefaults/ConversationsServiceDefaults.cs` (deleted)
- `src/Hexalith.Conversations.ServiceDefaults/Hexalith.Conversations.ServiceDefaults.csproj` (deleted)
- `src/Hexalith.Conversations.ServiceDefaults/ServiceDefaultsAssemblyMarker.cs` (deleted)
- `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs` (new)
- `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs` (modified)
- `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj` (modified)
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` (modified)
- `tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` (modified)
- `tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs` (new)
- `tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` (modified)
- `tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs` (new)
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs` (new)
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionGatewayDispatchLiveTests.cs` (new)
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs` (new)
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainDiscoveryHostCompositionTest.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs` (new)
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionDurableEventCoverageTest.cs` (new)
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionHandlerTest.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadStoreFailClosedTest.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs` (modified)
- `tests/Hexalith.Conversations.ServiceDefaults.Tests/ConversationsServiceDefaultsTest.cs` (deleted)
- `tests/Hexalith.Conversations.ServiceDefaults.Tests/Hexalith.Conversations.ServiceDefaults.Tests.csproj` (deleted)
- `tests/README.md` (modified)

### Gitlink Promotions

| Path | Declared | Recorded mode | Recorded commit | Baseline commit |
| --- | --- | --- | --- | --- |
| `references/Hexalith.Builds` | yes | `160000` | `1b1c0b0360715b82de48b618fc4e94e7e01e8092` | `0b8f0c83263b7150c98341ceca8cd3cd8404a375` |
| `references/Hexalith.Commons` | no | `160000` | `f2b5f1b12b478dce902756876138a60cde4fde65` | `427530e27eab40b12e85832698da6962fd0c5a48` |
| `references/Hexalith.EventStore` | yes | `160000` | `b1d08dac328ee6a2f9b4ef07a1a14ad5756ba94e` | `b2d3402552fbadf529c220fcc739da9d06d285fe` |
| `references/Hexalith.FrontComposer` | no | `160000` | `b6efcad5b293017f9805e4fc7dc982b92abff678` | `7870526090a8596082e3df034ecacf4c07881a04` |
| `references/Hexalith.Memories` | no | `160000` | `0c351ff970b39a80a90821020feb0e6e8faf0183` | `b073aa577ad3006300a5d7192392bb0ca656944b` |
| `references/Hexalith.Tenants` | yes | `160000` | `96bdfd8a485d5fbee76cd660ce5257bb5fd54f1d` | `4a9124ec174179652d9480ea56e70f97f8a45a37` |

### Test Results

| Test project | State | Total | Passed | Failed | Skipped | Artifact SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| Conformance | PARSED | 425 | 425 | 0 | 0 | `a2ee55816f9f1f1a` |
| Server | PARSED | 631 | 631 | 0 | 0 | `405d50034187cb8d` |
| Contracts | PARSED | 618 | 618 | 0 | 0 | `bee6bcaf120103e9` |
| Domain | PARSED | 185 | 185 | 0 | 0 | `8a0079af7a27f05c` |
| Admin.Web | PARSED | 14 | 14 | 0 | 0 | `a224fa89e7337052` |
| IntegrationTests | PARSED | 14 | 14 | 0 | 0 | `581dbef5cc24c3cb` |
| Client | PARSED | 29 | 29 | 0 | 0 | `819e8b1ececb4f30` |
| AppHost | PARSED | 9 | 9 | 0 | 0 | `be0edf7559824a31` |
| **Total (computed)** | **8 parsed** | **1925** | **1925** | **0** | **0** | — |

### Candidate Binding

- Candidate `dc69719a9ce7c25bb9755827f19c7e1ce2a87287` · committed head `dc69719a9ce7c25bb9755827f19c7e1ce2a87287` · ancestor of head: **yes**
- Gitlinks moved after the candidate: none

### Promotion Completion Gate

- Result **PASS** · declared: references/Hexalith.EventStore, references/Hexalith.Builds, references/Hexalith.Tenants · changed gitlinks: references/Hexalith.Builds, references/Hexalith.Commons, references/Hexalith.EventStore, references/Hexalith.FrontComposer, references/Hexalith.Memories, references/Hexalith.Tenants · evaluated: references/Hexalith.EventStore, references/Hexalith.Builds, references/Hexalith.Tenants, references/Hexalith.Commons, references/Hexalith.FrontComposer, references/Hexalith.Memories
- WARNING `UNDECLARED_GITLINK_CHANGE`: gitlink changed between baseline and candidate without a declaration: references/Hexalith.Commons
- WARNING `UNDECLARED_GITLINK_CHANGE`: gitlink changed between baseline and candidate without a declaration: references/Hexalith.FrontComposer
- WARNING `UNDECLARED_GITLINK_CHANGE`: gitlink changed between baseline and candidate without a declaration: references/Hexalith.Memories

<!-- STORY-FINAL-RECORD:END -->

## Change Log

| Date | Change |
| --- | --- |
| 2026-07-29 | Re-anchored the SM-C2 and projection proof to implementation commit `28e217e` and EventStore `4c63f5d3`; the promotion gate passes from an isolated clean checkout with 0 blockers and 4 disclosed undeclared-gitlink warnings, without moving or capturing concurrent Builds, Memories, or Tenants worktrees. |
| 2026-07-29 | Code review pass 3 chunk 1: applied all 18 selected patches, made gateway/AppHost lanes mandatory, fixed the canonical `conversation` DAPR app-id topology, added bounded dispatch-ledger TTL semantics, and replaced the toy SM-C2 closure with production paths. Functional lanes pass; LIST/OPEN fail the 5% SM-C2 gate, so the story remains `in-progress`. |
| 2026-07-29 | Re-anchored the final record at candidate `dc69719` after committing the historical-record guard; reran the Release build and all eight test projects before regenerating the bound record. |
| 2026-07-29 | Updated the historical-record regression from the obsolete pre-generator Story 6.2 expectation to strict generated-record verification; generator/promotion workflow suite is 129/129. |
| 2026-07-29 | Replaced the legacy File List with the mechanically derived 86-path inventory, inserted the passing final record verbatim, bound `file_list_commit`, and moved the story to `review`. |
| 2026-07-29 | Rebound stale projection proof evidence to the current committed gitlink state without expanding the approved three-path promotion scope; refreshed its validator pins and reran the Release build plus all eight test-project lanes, including the opt-in live AppHost boundary. |
| 2026-07-28 | Code review: implemented all 18 selected adversarial findings; added stable dispatch-ledger consistency, bounded bulk reads, strict decode/rebuild behavior, real AppHost runtime coverage, and the production endpoint/Data Protection fixes revealed by that launch. Returned story to `in-progress` pending evidence regeneration and EventStore promotion capture. |
| 2026-07-27 | T1 closed: v2 proof evidence and its conformance validator rebound from `0eb3657`/`b11b0c7` to `c8c7003`/candidate `c398ea2`, with the promoted-capability delta measured and recorded, and a git-backed re-derivation added so the binding can go red instead of stale. |
| 2026-07-27 | T2 closed by strengthening the fixture (Jerome's decision): gateway lane crosses `ProjectionUpdateOrchestrator` and `NamedProjectionDispatchCoordinator` into a DAPR/Redis `statestore`; recorded as `gatewayBoundaryEvidence` with `residualGap: "none"` and mechanically asserted. |
| 2026-07-27 | T3 closed: non-shipping AppHost boundary asserted from evaluated MSBuild properties and fault-injected to prove it fails when an import flips `IsPackable`. |
| 2026-07-27 | T4 closed: SM-C2 reconstruction provenance recorded in both artifacts and validated by git with an anti-vacuity check; residual limitation stated. |
| 2026-07-27 | T5 closed: both 6.2 specs marked `superseded`; File List derived mechanically at 49 paths. |
| 2026-07-27 | Fixed a public contract regression introduced by `953bf71`: durable `…DomainEvent` aliases had widened `PublicEventTypeEntries` from 13 to 26 names. Split into a separate durable registry and guarded. |
| 2026-07-27 | Resolved a `SUBMODULE_DIRTY_UNTRACKED` gate blocker by parking an unrelated concurrent-session draft out of `references/Hexalith.Tenants` byte-identically, with no submodule commit, push, or gitlink move. |
| 2026-07-27 | Corrected an out-of-scope gitlink capture (`c398ea2`). Commit `953bf71` had swept an unrelated `references/Hexalith.Tenants` fast-forward (`f1053a3` → `0ded4a1`, a tenant search-page change) into the story after it drifted into the working tree mid-session. Per the release owner's decision on D1 the recorded gitlink is restored to `f1053a3` — the value this story's own promotion window established — instead of promoting an unrelated commit. `0ded4a1` remains on the submodule's `main`/`origin/main`. `references/Hexalith.Memories` had drifted the same way and was restored in the working tree only (it was never captured). |
| 2026-07-27 | Rebound the promotion evidence to candidate `c398ea2` (`39b9206`). The story's own git-backed validator rejected the stale binding, which is the behaviour T1 added it for. Gate at final `HEAD`: `pass`, 0 blockers, 0 warnings, 3/3 declared paths evaluated clean, remote-available, exact at mode `160000`. |
