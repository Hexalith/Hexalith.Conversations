---
story_key: '6-2-migrate-conversations-to-platform-owned-hosting'
epic: 6
story_id: '6.2'
created: '2026-07-27'
status: 'ready-for-dev'
baseline_commit: '29def441408becfbbbdc5c59b9af14a7717cb21f'
submodule_promotions:
  - path: 'references/Hexalith.EventStore'
    require_remote: true
# ^ Exact transcription of the already-approved scope declared in
#   spec-6-2-migrate-conversations-to-platform-owned-hosting-2.md frontmatter (AC 1c).
#   NOT expanded. Two further root gitlinks (Builds, Tenants) have moved since the
#   baseline and currently surface as non-blocking UNDECLARED_GITLINK_CHANGE warnings;
#   see Dev Notes -> Open decision D1. Do not add them without Jerome's approval.
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

Status: ready-for-dev

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

- [ ] **T1 — Rebind the v2 proof's promotion evidence to the final story candidate** (AC: 3, 5) — **BLOCKING**
  - [ ] `docs/release-evidence/projection-read-store-population-proof-v2.json` →
        `eventStorePromotion.commit`, `.requiredUmbrellaGitlinkCommit`, and
        `.umbrellaMechanicalGate.{candidate,recordedGitlink,warnings}` are bound to
        `b11b0c7` / `0eb3657` / `warnings: []`. The umbrella has since moved:
        commit `48069d7` re-pointed `references/Hexalith.EventStore` to `c8c7003`,
        and `04edf99` moved `references/Hexalith.Builds`. Re-run the gate against the
        **final committed candidate** and rewrite these fields to what it actually returns.
  - [ ] `docs/release-evidence/projection-read-store-population-proof-v2.md` → update the
        *Hosting and promotion* paragraph to the same commit/candidate.
  - [ ] `tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs:76-89`
        hard-codes `0eb3657`, `b11b0c7`, and `warnings.GetArrayLength() == 0`. Update to the
        final values. **Do not relax the assertions to make them pass** — pin the real ones.
  - [ ] Confirm the EventStore delta `0eb3657..c8c7003` does not disturb the promoted
        capability. Measured: 2 commits, touching CI/publication preflight, story docs, and
        `ContainerPublishingGovernanceTests` only — `EventStoreDomainServiceExtensions.cs`
        and `HexalithEventStoreDomainModuleExtensions.cs` are unchanged. Record that finding.
  - [ ] Precedent: Story 6.7 review pass 2 caught this exact defect
        ("*Completion evidence does not correspond to any single revision*"). Do not repeat it.

- [ ] **T2 — Close the production-boundary proof-strength question** (AC: 5, 6) — **BLOCKING**
  - [ ] `ConversationProjectionReadStorePopulationLiveTests` currently composes
        `InMemoryReadModelStore` (from `Hexalith.EventStore.Testing.Fakes`) as the configured
        `IReadModelStore` and calls `DomainProjectionDispatcher.DispatchAsync(...)` in-process.
        It therefore crosses the **domain-service** dispatcher but **not** the gateway-side
        `ProjectionUpdateOrchestrator` / `NamedProjectionDispatchCoordinator` (both live in
        `Hexalith.EventStore.Server`), and not a DAPR-backed state store.
  - [ ] ADR 0003 → *Verification* items 1 and 2 require the append/replay to reach "*the
        EventStore named-projection coordinator, the domain-service dispatcher, and the
        Conversations named asynchronous handler*" and require "*the configured integration
        state-store adapter*" to contain the exact keys.
  - [ ] Choose **one** and record it explicitly in the evidence JSON:
        **(a)** strengthen the fixture to run through the retained AppHost harness against a
        DAPR-backed state store and the gateway coordinator, **or**
        **(b)** record a named-owner justification that the platform's own `IReadModelStore`
        fake plus the domain-service dispatcher is the approved integration boundary, with the
        residual gap stated in plain terms.
  - [ ] Option (b) requires Jerome or the Product Owner — it narrows ADR 0003's own
        verification wording. **Do not choose (b) silently.**
  - [ ] Story 6.6 hash-validates and reruns this proof. A gap left implicit here surfaces
        there as a release blocker.

- [ ] **T3 — Prove the non-shipping AppHost boundary from evaluated MSBuild properties** (AC: 2, 7)
  - [ ] `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs:30-31`
        reads raw `<IsPackable>` / `<IsPublishable>` XML elements out of the csproj. That is a
        shape check, not a mechanical one: an imported `.props`/`.targets` could flip either
        property with the XML unchanged, and the test would stay green.
  - [ ] Assert the **evaluated** values instead (`dotnet msbuild -getProperty:` or an
        equivalent evaluation), so the assertion measures what the build actually does.
  - [ ] Both evaluate to `false` today (verified at HEAD) — this hardens the guard, it does
        not change behavior.
  - [ ] Precedent: the repeated `"5% P95"` ⊂ `"45% P95"` class of defect. A guard that cannot
        fail is the failure.

- [ ] **T4 — Make the SM-C2 baseline reconstruction auditable** (AC: 1)
  - [ ] `sm-c2-hot-path-baseline-v1.json` declares `sourceCommit: 29def44`, but
        `tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs`
        did not exist at `29def44` — it was added by `b11b0c7` alongside the production edits.
  - [ ] AC1 explicitly permits reconstruction from the preserved source commit with the same
        versioned fixture, so this is **legitimate if and only if** the baseline run actually
        executed against the `29def44` production sources. Neither the JSON nor the MD states
        how.
  - [ ] Record the reconstruction method in both artifacts: the exact tree the baseline ran
        against, how the fixture was overlaid onto it, and the confirmation that the post run
        used the byte-identical fixture (`sha256 fd2c6184…`) and envelope.
  - [ ] If the reconstruction cannot be evidenced, AC1's *Block If* applies — say so rather
        than asserting the baseline.

- [ ] **T5 — Reconcile the story record, spec route, and sprint tracking** (AC: all)
  - [ ] Two 6.2 specs exist: `spec-6-2-…-hosting.md` (`status: blocked`, superseded by the
        2026-07-27 resolution) and `spec-6-2-…-hosting-2.md` (`status: in-review`,
        `review_loop_iteration: 0`, `warnings: [oversized, multiple-goals]` — **never code
        reviewed**). Reconcile them against this story record; do not leave two live specs.
  - [ ] This story's File List must be derived from `git diff --name-only <baseline>..HEAD`,
        not hand-maintained (Story 6.7 review-pass-2 finding).
  - [ ] Sprint-status transitions are owned by the dev/review workflows; record the evidence
        that justifies each one.

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
- Never claim atomic multi-key persistence. ADR 0003 deliberately keeps **two idempotent
  policy writes**; second-write uncertainty is **non-completion**, and queries must expose
  cross-key generation inconsistency rather than repair it on read.
- Never treat direct writer calls, DI resolution, mock counts, HTTP acceptance, or the legacy
  opaque `ProjectionResponse` as population proof.
- Never mutate signed v1 evidence, frozen Epic 1–5 history, retrospectives, the historical
  epic prefix, or Story 6.5 authoring-template evidence.
- Never initialize, update, fetch, or traverse **nested** submodules; root `.gitmodules` only.
  No `git submodule update --init --recursive` or `--remote`.
- Never `git add -A` / `git commit -a`. Stage only declared File List paths — this is the
  failure that required review correction in Stories 2.2, 3.3, and 6.1.

### Open decision D1 — undeclared changed gitlinks (needs Jerome)

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

### Debug Log References

### Completion Notes List

### File List
