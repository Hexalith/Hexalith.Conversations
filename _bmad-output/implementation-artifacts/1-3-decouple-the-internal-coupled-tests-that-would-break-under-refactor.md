---
baseline_commit: a68c6e3
depends_on: 1-2-measure-the-oracles-blind-spots-and-backfill-characterization-tests
---

# Story 1.3: Decouple the internal-coupled tests that would break under refactor

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want the internal-coupled test files re-expressed against public surface (or correctly classified as plumbing-only-retire),
so that the Boilerplate Reduction refactor does not produce false-negative test failures that mask real behavior preservation, and the FR-20 removed-test justification ledger is seeded before any plumbing moves.

> **Initiative context (read first):** This is **Story 1.3 of a behavior-preservation refactor** (Conversations Boilerplate Reduction), the **third and final gate-zero story** of Epic 1. Story 1.1 pinned the 14-suite conformance oracle green on `main` and snapshotted the public contract shape; Story 1.2 measured the oracle's blind spots and backfilled live-decision-code characterization tests. **This story makes the oracle refactor-survivable** by re-expressing the tests that are coupled to *plumbing the later epics will move* — so that when Epic 2/3 deletes/relocates a materializer, executor, guard, or diagnostics type, the oracle stays green for the right reason (behavior preserved) and goes red only for the right reason (behavior broken), never red merely because a moved internal type no longer compiles. The output is **(a)** a re-expressed governance safety-net test, **(b)** a triage of the remaining at-risk tests into `{re-express | plumbing-only-retire}`, and **(c)** a committed **at-risk register** that seeds the FR-20 removed-test justification ledger. This story moves **no `src/` production code** — it is a test-and-evidence story. Characterization/re-expressed tests must pin **current observable behavior** (green on `main`), not the refactor target. Do **not** delete any test here; deletion of plumbing-only tests happens later, in the specific Epic 2/3 story that removes the plumbing.

## Acceptance Criteria

### AC1 — `GovernanceAuditPairingSafetyNetTest` re-expressed against public surface; classified "re-express, never delete"

**Given** `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`, which today asserts the governance audit-pairing invariant by **reflecting over `ConversationAggregate` internals** (`typeof(ConversationAggregate).GetMethods()`, handler/service constructor parameter types, `IConversationGovernanceAuditService` shape)
**When** it is re-expressed
**Then** it asserts the same release-gate invariant — **every implemented governance mutation pairs its state-change event with audit evidence, and non-governance commands carry no audit dependency** — through the **public command/event/`DomainResult` surface**: dispatch each governance command (`SetConversationRetentionPolicy`, `MarkConversationContentSensitive`, `RedactMessageContent`) through `ConversationAggregate.Handle(command, state)` and assert the resulting `DomainResult.Events` contain the paired mutation event **carrying `GovernanceAuditEvidenceReference`**, and that the non-governance commands (`CreateConversation`, `AddParticipant`, `ReassignConversationProject`) emit their events **without** an audit-evidence requirement
**And** it no longer depends on `Server.CommandHandlers` / `Server.Governance` / `Server.Projections` / `Server.Queries` / `Server.Api` concrete plumbing types or reflection over them
**And** it is explicitly **classified "re-express, never delete"** in the at-risk register (AC5) because it is a behavior safety net, **NOT** plumbing — its survival through the refactor is the point.

### AC2 — `ConversationProjectionMaterializerTest` and `IdempotentConversationCommandExecutorTest` triaged (re-express OR plumbing-only-retire)

**Given** `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` and `tests/Hexalith.Conversations.Server.Tests/Idempotency/IdempotentConversationCommandExecutorTest.cs`, which today **directly instantiate the concrete plumbing** (`new ConversationProjectionMaterializer()` → `Project(...)` returning the Server-internal `ConversationProjectedReadModels`; `new IdempotentConversationCommandExecutor(store)` over `InMemoryConversationIdempotencyStore`)
**When** they are triaged per-assertion (not per-file — a single file mixes behavior-bearing and plumbing-only assertions)
**Then** each **release-gate behavior assertion** (redaction non-leakage on replay, projection freshness/degraded-state surfacing — stale/rebuilding/gap/poison/unavailable, governance evidence anchoring, idempotent duplicate-replay-without-mutation, conflict/retryable-uncertainty rejection) is **either** re-expressed to assert that behavior through the **public projection-read surface** (`ConversationProjectionReadService` / `ConversationQueryHandler` returning `Contracts.Projections.*` DTOs) **or** the public **`DomainResult` idempotency surface** (`DomainResult` + `Contracts`-facing `ConversationIdempotencyReplayResult` outcome fields) — so the behavior stays pinned after the plumbing moves
**And** each remaining **plumbing-only assertion** (one that asserts internal mechanics with no externally-observable release-gate behavior — e.g. reservation poisoning lifecycle, injected `TimeProvider` wiring, raw store snapshot record status) is **documented as plumbing-only-retire**, mapped to the **specific Epic 2/3 story that removes that plumbing**: the projection materializer is *Promote-orchestration/Keep-logic* → **Story 2.5 (FR-6, SDK projection seam)**; the idempotent command executor / bridge is *Consume aggregate base* → **Story 2.2 (FR-7, `EventStoreAggregate<TState>` dispatch/idempotency-bridge shims)**
**And** nothing is deleted in this story — plumbing-only tests are *marked for retirement in their owning story*, not removed now.

### AC3 — Oracle-survivability couplings flagged by Story 1.1 (and inherited from Story 1.2) reconciled into the same register

**Given** the Story 1.1 AC3 hand-off — the internally-coupled conformance suites **`TelemetryCardinalityConformanceSuiteTest`** and **`TelemetryRedactionConformanceSuiteTest`** (`using Hexalith.Conversations.Server.Diagnostics` / `Server.TenantAccess`), the verified discrepancy **`ConformanceStatusConformanceSuiteTest`** (clean test class, but its engine `ConformanceStatusConformanceSuite.cs` + `ConformanceStatusConformanceFixtures.cs` couple to `Server.Diagnostics`), the shared `TelemetryDisclosureConformanceFixtures.cs` coupling, and the **project-level `Hexalith.Conversations.Conformance.Tests.csproj → Hexalith.Conversations.Server` reference** — **plus** the Story 1.2 live characterization tests (`LiveTenantFailClosedOracleCharacterizationTest`, `LiveProjectionFreshnessOracleCharacterizationTest`, `LiveIdempotencyOracleCharacterizationTest`) which deliberately reach into `Server.*` to catch fail-open mutations and were recorded as a Story-1.3 survivability concern
**When** each is triaged
**Then** it is classified in the at-risk register as one of `{re-express | plumbing-only-retire | coupled-by-design-retarget-in-owning-story}` with rationale and (where applicable) the owning Epic 2/3 story that will retarget it when the underlying type moves (telemetry/diagnostics → **Story 3.3 / FR-15**; tenant-access → **Story 3.2 / FR-11**; projection seam → **Story 2.5 / FR-6**; idempotency bridge → **Story 2.2 / FR-7**)
**And** the **`Conformance.Tests → Server` project reference** is given an explicit disposition: a recorded plan for whether/when the public-surface suites stop transitively depending on the plumbing assembly (e.g. split a `Server`-coupled fixture project, or retarget the coupled suites in their owning stories) — **the reference is not removed in this story** (removing it would break the still-coupled telemetry suites, which is the owning stories' job), but the register states the target end-state so Story 5.1's oracle is structurally survivable
**And** no oracle suite's assertion strength is reduced by any re-expression (oracle strength only holds or increases — verified against the Story 1.1 baseline).

### AC4 — Every re-expressed test is green on unmodified `main` (pins current behavior, not the refactor target)

**Given** each re-expressed test from AC1/AC2/AC3
**When** it runs on unmodified `main` (at `baseline_commit`)
**Then** it passes — proving it captures **current observable behavior**, not the refactor's intended end-state
**And** the full conformance project plus the affected `Server.Tests` project run **100% green** with **no net loss of asserted behavior**: the re-expressed test asserts at least the release-gate invariant the original asserted (a re-expression may assert *more*, never *less*); if a re-expression cannot reach a specific assertion through the public surface, that assertion is recorded as a plumbing-only-retire entry (AC2/AC5) rather than silently dropped.

### AC5 — At-risk register committed; seeds the FR-20 removed-test justification ledger; carry-forward gaps recorded

**Given** the triage of all at-risk tests (AC1–AC3)
**Then** a committed, self-describing **at-risk register** artifact under `docs/release-evidence/` maps **each at-risk test (and, where a file is split per-assertion, each at-risk assertion group)** to its classification `{re-express, never delete | re-express | plumbing-only-retire | coupled-by-design-retarget-in-owning-story}`, with: the file path, the specific coupling (the internal type/namespace it binds to), the rationale, and — for every `*-retire` / `*-retarget` entry — the **owning Epic 2/3 story** that will retire/retarget it
**And** the register is explicitly stated to be the **seed of the FR-20 removed-test justification ledger** (the artifact Story 5.2 reconciles), so no later plumbing-only deletion is unaccounted for
**And** the **carry-forward gaps from Story 1.2** are folded in: the live governance audit *gate* (fail-closed-on-sink-failure) being `internal` (visible only to `Server.Tests`, unreachable from the oracle), and the observed-once test-parallelism race between `PublicContractShapeSnapshotGenerationTest` (writes a committed evidence file) and `ReleaseBaselineValidationTest` (reads it) — each recorded with a disposition (closed-here | accepted-with-rationale-and-owning-story)
**And** if a generator/validator test is added for the register, it mirrors the existing `ReleaseConformanceArtifactGenerationTest` / `PublicContractShapeSnapshotGenerationTest` pattern (repo-root discovery → deterministic indented-JSON write into `docs/release-evidence/` → re-read + re-validate + content-safety scan)
**And** only intended files are staged; **zero production source under `src/` changes**; no sibling submodule (EventStore, Tenants, Parties, Commons, Folders, Projects, Memories, FrontComposer, …) is touched; submodules are never recursed.

## Tasks / Subtasks

- [x] **Task 1 — Re-confirm the oracle is green at the current baseline (AC4 precondition)**
  - [x] Confirm `src/` and `tests/` working tree is clean; capture `git rev-parse HEAD` and record it as `baseline_commit` (update the frontmatter above if it drifted from `a68c6e3`). — HEAD `a68c6e3` matches frontmatter; `src/`/`tests/` clean.
  - [x] Run `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` and confirm the Story 1.2 baseline (**294 passed, 0 failed, 0 skipped**). Run `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` and record its current pass tally as the Server.Tests baseline. — Conformance **294 passed**; Server.Tests baseline **515 passed**. Both green.

- [x] **Task 2 — Re-express `GovernanceAuditPairingSafetyNetTest` against the public surface (AC1)**
  - [x] Read the current test (reflection inventory over `ConversationAggregate.GetMethods()`, handler/service constructor parameter types, `IConversationGovernanceAuditService`). Identify the **invariant** each `[Fact]` actually protects.
  - [x] Re-express the **behavior-bearing invariants** by driving `ConversationAggregate.Handle(command, state)` with each governance command and asserting the resulting `DomainResult.Events` carry the paired mutation event **with** `GovernanceAuditEvidenceReference`; assert the non-governance commands produce their events **without** an audit-evidence requirement. Public contract + core types only — no `Server.*`. → `GovernanceAuditPairingSafetyNetConformanceTest.cs`.
  - [x] Structural assertions that can only be proven by reflecting over `Server` concrete types (read-only boundaries; privileged-justification wiring; handler-type/ctor fragments) classified plumbing-only-retire in the register (AC5) rather than forcing a brittle public-surface mimic. Audit-pairing behavior assertions kept in the re-expressed public-surface test (incl. the stronger missing/mismatched-evidence fail-closed enforcement).
  - [x] Classified the re-expressed test **"re-express, never delete"** in the register. Green on `main` (6/6).
  - [x] **Decision honored:** the re-expression is relocated **into** `tests/Hexalith.Conversations.Conformance.Tests/` so Story 5.1's full-suite run includes it. The original Server.Tests file is **kept in place** (the dominant no-deletion guardrail overrides the "ideal physical move"); the placement + residual structural coupling are recorded in the register.

- [x] **Task 3 — Triage `ConversationProjectionMaterializerTest` (AC2)**
  - [x] Walked each `[Fact]`; sorted assertions into (a) release-gate behavior observable through a public projection read vs (b) plumbing-only raw-materializer mechanics.
  - [x] For (a): re-expressed through `ConversationProjectionReadService.ReadDetailAsync(...)` asserting on `Contracts.Projections.*` DTOs + `ProjectionTrustState`, seeding via the `FakeProjectionReadStore` pattern. → `ConversationProjectionReadSurfaceConformanceTest.cs`. Cross-references (does not duplicate) Story 1.2 `LiveProjectionFreshnessOracleCharacterizationTest` (raw-materializer level); the re-expression covers the read-service path.
  - [x] For (b): marked plumbing-only-retire → **owning Story 2.5 (FR-6, SDK projection seam)** in the register.
  - [x] Both buckets recorded in the register with rationale + owning story. Re-expression green on `main` (7/7 — the four degraded-state theory cases include the AC2-named `gap` state plus the mixed-tenant poison case).

- [x] **Task 4 — Triage `IdempotentConversationCommandExecutorTest` (AC2)**
  - [x] Walked each `[Fact]`; sorted into (a) release-gate behavior observable through the public `DomainResult` idempotency surface vs (b) plumbing-only internal store/executor mechanics.
  - [x] For (a): re-expressed asserting on `DomainResult` + `ConversationIdempotencyReplayResult.Outcome` public fields + the `ConversationRejectedDomainEvent` envelope → `LiveIdempotencyConflictOracleCharacterizationTest.cs`, covering the conflict / pending / rejection-reason-preservation / replay-payload-secret-exclusion cases that Story 1.2's `LiveIdempotencyOracleCharacterizationTest` (duplicate-replay) omits; cross-referenced in the register.
  - [x] For (b): marked plumbing-only-retire → **owning Story 2.2 (FR-7)** in the register.
  - [x] Both buckets recorded. Re-expression green on `main` (4/4).

- [x] **Task 5 — Reconcile the oracle-survivability couplings into the register (AC3)**
  - [x] Read the `using` blocks / coupled types of `TelemetryCardinalityConformanceSuite(Test)`, `TelemetryRedactionConformanceSuite(Test)`, `ConformanceStatusConformanceSuite.cs` + `ConformanceStatusConformanceFixtures.cs`, and `TelemetryDisclosureConformanceFixtures.cs` (`Server.Diagnostics`: `ConversationConformanceStatusClassifier`/`Class`, `ConversationConformanceTelemetry`, `ConversationProjectionTelemetry`; `Server.TenantAccess`).
  - [x] Classified each `coupled-by-design-retarget-in-owning-story`: telemetry/diagnostics → **Story 3.3 (FR-15)**; tenant-access → **Story 3.2 (FR-11)**, with rationale (these suites assert operational-telemetry behavior that genuinely needs the diagnostics types; retarget-in-owning-story, not full re-expression).
  - [x] Triaged the three Story 1.2 `Live*OracleCharacterizationTest` files `coupled-by-design-retarget-in-owning-story` (projection → 2.5; idempotency → 2.2; tenant-access → 3.2) — never deleted, only retargeted when the type moves.
  - [x] Gave the **`Conformance.Tests → Server` project reference** an explicit disposition (target end-state + path-to-get-there + owning story), recorded in `projectReferenceDisposition`; **not removed in this story**.

- [x] **Task 6 — Write the at-risk register + fold in Story 1.2 carry-forwards (AC5)**
  - [x] Wrote `docs/release-evidence/at-risk-test-register-v1.json` (+ `.md` header) with a `tests[]` array (`{ file, coupling, classification, rationale, owningStory, reExpressedAs, baselineGreen }`), the re-expressions added by this story, the project-reference disposition, the classification legend, the `baselineCommit`, and the top-level note that it **seeds the FR-20 removed-test justification ledger** (reconciled in Story 5.2).
  - [x] Folded in the two Story 1.2 carry-forward gaps: (1) the `internal` live governance audit gate — **partially closed here** (public materialized pairing invariant pinned in the oracle; internal sink-failure gate accepted-with-rationale, owning Story 2.1); (2) the observed-once parallelism race — **accept-with-rationale**, committed file stays byte-identical.
  - [x] Added a generator/validator test (`AtRiskTestRegisterGenerationTest.cs`) mirroring `PublicContractShapeSnapshotGenerationTest.cs`: repo-root discovery → deterministic indented-JSON write → re-read + re-validate + **content-safety scan**. Emitted artifact verified free of forbidden substrate terms.

- [x] **Task 7 — Final green + scope verification + commit (AC4, AC5)**
  - [x] Ran the conformance project and the `Server.Tests` project; both green with no net loss of asserted behavior. Conformance **294 → 316** (+22 = the four new test classes); Server.Tests **515 → 515** (unchanged). The governance re-expression was **added to the oracle, original kept** (not a physical move) per the no-deletion guardrail — recorded in the register/File List.
  - [x] Confirmed `git status` shows **no `src/` changes**; no sibling submodule touched; no submodule recursed.
  - [x] Staged only intended files (4 new conformance test files + `docs/release-evidence/at-risk-test-register-v1.(json|md)` + this story file + sprint-status).

## Dev Notes

### What this story IS (and is NOT)

- **IS:** an oracle-*survivability* + decision-spine story. Re-express the tests coupled to plumbing the later epics move, so the oracle reflects behavior, not internal structure; triage every at-risk test into `{re-express | plumbing-only-retire | coupled-by-design-retarget}`; commit the at-risk register that seeds the FR-20 ledger. Output = re-expressed governance safety net + triaged projection/idempotency tests + the committed register + reconciled Story-1.1/1.2 hand-offs.
- **IS NOT:** a refactor, a deletion, or a bug-fix. **Zero `src/` production code changes.** **No test is deleted here** — plumbing-only tests are *marked for retirement in their owning Epic 2/3 story*, not removed now (greenfield latitude licenses deletion *with the code* later, not in this gate-zero story). Re-expressed tests pin **current observable behavior** (green on `main`); if current behavior looks wrong, pin it and log a concern — don't change it.
- The downstream contract: **Story 5.2** reconciles the FR-20 removed-test justification ledger against *this story's at-risk register*; **Story 5.1** runs the full oracle and confirms green was never lost. A miscategorized test here either (a) breaks the oracle's compile under refactor for the wrong reason, or (b) lets a real conformance test get deleted as "plumbing." Both are the disasters this story prevents (NFR1).

### The three named at-risk Server.Tests files — current coupling and re-expression target

| File | Current coupling (why it breaks under refactor) | Owning move | Disposition |
|------|--------------------------------------------------|-------------|-------------|
| `Governance/GovernanceAuditPairingSafetyNetTest.cs` | Reflects over `ConversationAggregate` internals + `Server.CommandHandlers`/`Server.Governance`/`Server.Projections`/`Server.Queries`/`Server.Api` concrete handler/service types | Governance is *Keep* behavior; handlers may be re-registered via shared host (Story 2.1/FR-3) | **AC1: re-express, never delete** — assert audit-pairing via `Handle(command,state) → DomainResult.Events` carrying `GovernanceAuditEvidenceReference` |
| `Projections/ConversationProjectionMaterializerTest.cs` | `new ConversationProjectionMaterializer().Project(...)` → asserts on Server-internal `ConversationProjectedReadModels` | **Story 2.5 / FR-6** — Promote orchestration to SDK `IDomainProjectionHandler`, Keep field-selection/freshness logic | **AC2: split** — behavior via `ConversationProjectionReadService`→`Contracts.Projections.*`; orchestration mechanics → plumbing-only-retire @ 2.5 |
| `Idempotency/IdempotentConversationCommandExecutorTest.cs` | `new IdempotentConversationCommandExecutor(store)` over `InMemoryConversationIdempotencyStore`; asserts raw store records | **Story 2.2 / FR-7** — Consume `EventStoreAggregate<TState>` base; remove idempotency-bridge shims | **AC2: split** — observable idempotency via `DomainResult`/`ConversationIdempotencyReplayResult`; reserve/poison/timeprovider mechanics → plumbing-only-retire @ 2.2 |

### Public surfaces to re-express against (verified to exist on `main`)

- **Governance audit-pairing:** `ConversationAggregate.Handle(TCommand, ConversationState?)` is `public static`, returns `DomainResult` (`Success(IEventPayload[])` / `Rejection(IRejectionEvent[])` / `NoOp()`). The paired mutation events (`RetentionPolicySetDomainEvent`, `RetentionPolicyReplacedDomainEvent`, `ConversationContentMarkedSensitiveDomainEvent`, `MessageContentRedactedDomainEvent`) and `GovernanceAuditEvidenceReference` live in `Hexalith.Conversations.Events` / `Contracts.Governance`. This is the pure command/state/event surface — no Server plumbing needed. (Per project-context: "aggregate tests should be pure command/state/event tests; do not mock inside aggregate logic.")
- **Projection behavior:** `ConversationProjectionReadService.ReadDetailAsync(trustedTenantId, callerPrincipalId, routeTenantId, conversationId, ct)` → `ConversationProjectionReadResult`, applying the fail-closed tenant + freshness boundary and returning `Contracts.Projections.*` DTOs with `Contracts.TrustStates.ProjectionTrustState`. Seed it through `IConversationProjectionReadStore` — **reuse `FakeProjectionReadStore` already in `ConversationProjectionReadServiceTest.cs`**; do not author a new fake (project-context: "do not mock inside aggregate logic; reuse existing fakes").
- **Idempotency behavior:** the observable surface is `DomainResult` + `ConversationIdempotencyReplayResult` (in `src/Hexalith.Conversations/Idempotency/`, the core module — not Server, not Contracts) with public `Outcome` fields (`Category`, `TenantId`, `CommandType`, `ConversationId`, `RejectionCode`, `IsRetryable`, `AuditHandle`, `ResultPayload`) and the `ConversationRejectedDomainEvent` envelope (`Code`, `ReasonCode`). Note the replay-result/outcome types are in the `Idempotency` core namespace, which itself may shift under FR-7 — record that residual coupling in the register; the goal is to stop asserting on `Server.CommandHandlers` executor internals and the raw store.

### Do NOT duplicate Story 1.2's live characterization tests

Story 1.2 already added, **inside the conformance project**, live-decision-code characterization tests that pin several of these exact behaviors against the live types:
- `LiveProjectionFreshnessOracleCharacterizationTest` — live materializer freshness/redaction-replay (behaviors #4/#5).
- `LiveIdempotencyOracleCharacterizationTest` — live executor duplicate-replay-without-mutation (behavior #3).
- `LiveTenantFailClosedOracleCharacterizationTest` — live tenant guard fail-closed across all seven trigger states (behavior #1).

These are **deliberately coupled to `Server.*`** (that is how they catch fail-open mutations). In this story: **classify them `coupled-by-design-retarget-in-owning-story`** (projection→2.5, idempotency→2.2, tenant-access→3.2) — never delete; the owning refactor story retargets them to the moved/promoted type. When re-expressing the three Server.Tests files, **cover the cases these Live* tests do not already cover** and cross-reference them in the register — do not re-assert the same case in two places.

### The at-risk register — structure and role

This register is the **central deliverable** and the seed of the FR-20 removed-test justification ledger (Story 5.2). Place it at `docs/release-evidence/at-risk-test-register-v1.json` (+ `.md` header), alongside `release-baseline-v1.*`, `public-contract-shape-baseline-v1.json`, and `oracle-blind-spot-analysis-v1.*`. Classifications:
- **`re-express, never delete`** — behavior safety nets (the governance audit-pairing test). Survive the refactor; their deletion would silently drop a conformance behavior.
- **`re-express`** — behavior assertions whose coupling is incidental; re-expressed against the public surface here so the underlying plumbing can move freely.
- **`plumbing-only-retire`** — assertions/tests with no externally-observable release-gate behavior; retired **with their code** in the named owning Epic 2/3 story (each gets a rationale + owning story so Story 5.2 can reconcile every later test-count reduction).
- **`coupled-by-design-retarget-in-owning-story`** — tests that must keep touching live/internal types to do their job (Story 1.2 Live* tests, the telemetry suites); the owning story updates the type reference rather than dropping the test.

Each entry: `file`, `coupling` (type/namespace), `classification`, `rationale`, `owningStory` (for retire/retarget), `reExpressedAs` (for re-express), `baselineGreen`.

### Carry-forwards inherited from Story 1.2 (fold into the register)

1. **Internal governance audit gate** — the live fail-closed-on-sink-failure audit *gate* is `internal`, visible only to `Server.Tests`, unreachable from the oracle. Story 1.2 pinned the *public materialized pairing invariant* instead and handed the internal gate to this story. Disposition: if the AC1 public-surface re-expression reaches the pairing invariant, mark closed-here; otherwise record accepted-with-rationale + the owning story that will surface or retire it.
2. **Test-parallelism race** — observed once (not reproduced) between `PublicContractShapeSnapshotGenerationTest` (writes a committed evidence file) and `ReleaseBaselineValidationTest` (reads it) under xUnit parallelism. The committed file stays byte-identical. Disposition: record accept-with-rationale, or — if trivially serializable (e.g. an xUnit `[Collection]` to order writer-before-reader) — fix it as a test-only change and note it. Do not let it block the story.

### Critical guardrails (from project-context.md)

- **Behavior preservation is the dominant gate (NFR1).** A re-expression must assert **≥** the original's release-gate invariant; never trade assertion strength for decoupling. The oracle's strength only holds or increases vs the Story 1.1 baseline. If `main` is red, report it — do not paper over it.
- **No `src/` production changes.** This is a test-and-evidence story. The materializer/executor/guard are *read and asserted against*, never modified.
- **No deletions here.** Plumbing-only tests are *marked* for retirement in their owning story; greenfield latitude licenses deletion *with the code*, later. Deleting a test now would pre-empt Story 5.2's reconciliation.
- **Aggregate tests are pure command/state/event tests** — re-express governance via `Handle(command, state)`, no mocking inside aggregate logic, no new authorization fakes (reuse `FakeProjectionReadStore`, `Hexalith.EventStore.Testing` / `Hexalith.Tenants.Testing` idioms).
- **Content-safety:** any emitted artifact must contain only public Conversations concepts and test/type/behavior names — no `EventStore`, `snapshot`, `SignalR`, `dispatcher`, `repository`, provider payloads, raw exceptions, drive paths, poison sentinels, or Parties personal data.
- **Submodule rule (repo CLAUDE.md):** never recurse into nested submodules; initialize/update only root-level submodules. This story needs **no** submodule operation. The Conversations module (`src/Hexalith.Conversations.*`, `tests/Hexalith.Conversations.*`) lives at the **repository root**; the sibling `Hexalith.*` directories ARE submodules — leave them untouched.

### How to run / verify (Tech stack)

- .NET `10.0`, target `net10.0`, SDK pinned `10.0.300`; nullable enabled, implicit usings, warnings-as-errors; Central Package Management via `Directory.Packages.props`. Test stack: **xUnit v3** (`xunit.v3`), **Shouldly**, `Microsoft.NET.Test.Sdk`, `coverlet.collector 8.0.1`.
- Oracle: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` (Story 1.2 baseline: **294 passed**).
- Affected unit project: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` (capture its current tally in Task 1).
- Solution: `Hexalith.Conversations.slnx` at repo root.

### Project Structure Notes

- The re-expressed governance safety-net test should ideally **move** into `tests/Hexalith.Conversations.Conformance.Tests/` (it is a release-gate behavior; that project already references the contracts it needs, and moving it makes Story 5.1's full-suite run cover it). If moved, record it as a move (one file out of `Server.Tests`, one into `Conformance.Tests`) — a relocation, not a deletion — in the register and File List.
- Re-expressed projection/idempotency behavior tests likewise belong with the oracle where feasible (or stay in `Server.Tests` if they need the public read service wired with the existing fake — record the placement).
- **Detected variance to record (not fix):** the `Conformance.Tests → Server` project reference remains after this story (its removal is the owning telemetry/status stories' job once those suites are retargeted). The register states the target end-state; the reference is deliberately left in place.

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 1.3] — story statement + the four AC blocks (expanded above into AC1–AC5); `GovernanceAuditPairingSafetyNet` classified "re-express, never delete"; the register seeds the FR-20 ledger.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Epic 1] — gate-zero role; "the three internal-coupled tests decoupled/re-expressed against public surface before any refactor."
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR-20] — full release-gate conformance suite must pass; every removed test justified as plumbing-only; no conformance test silently dropped (Story 5.2 reconciles this story's register).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 2.2] — FR-7 `EventStoreAggregate<TState>` base / idempotency-bridge shims = owning move for the idempotency executor plumbing.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 2.5] — FR-6 SDK projection seam ("Promote orchestration, Keep logic") = owning move for the projection materializer; explicitly references "the `ConversationProjectionMaterializerTest` triaged in Story 1.3."
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 3.2 / Story 3.3] — FR-11 tenant-access / FR-15 telemetry = owning moves for the tenant-access and telemetry/diagnostics couplings in the oracle suites.
- [Source: _bmad-output/implementation-artifacts/1-1-pin-the-conformance-oracle-green-on-main-and-snapshot-the-public-contract-shape.md] — AC3 oracle-survivability hand-off: `TelemetryCardinalityConformanceSuiteTest`, `TelemetryRedactionConformanceSuiteTest`, the `ConformanceStatusConformanceSuiteTest` engine/fixtures discrepancy, `TelemetryDisclosureConformanceFixtures.cs`, and the `Conformance.Tests → Server` project reference.
- [Source: _bmad-output/implementation-artifacts/1-2-measure-the-oracles-blind-spots-and-backfill-characterization-tests.md] — the Live* characterization tests (do not duplicate; retarget-in-owning-story); the internal-governance-audit-gate carry-forward; the recorded test-parallelism race.
- [Source: tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs] — the reflection-coupled safety net to re-express (AC1).
- [Source: tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs] — the materializer test to triage (AC2).
- [Source: tests/Hexalith.Conversations.Server.Tests/Idempotency/IdempotentConversationCommandExecutorTest.cs] — the executor test to triage (AC2).
- [Source: tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs] — `FakeProjectionReadStore` + public-read assertion pattern to reuse for projection re-expression.
- [Source: src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs] — `public static DomainResult Handle(command, state)` governance command entry points (AC1 public surface).
- [Source: src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs] — `ReadDetailAsync(...)` public projection read surface (AC2).
- [Source: src/Hexalith.Conversations/Idempotency/ConversationIdempotencyReplayResult.cs] — public idempotency replay outcome surface (AC2).
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs] / [PublicContractShapeSnapshotGenerationTest.cs] — repo-root discovery + deterministic write-into-`docs/release-evidence/` + re-validate + content-safety pattern to mirror for the at-risk register.
- [Source: _bmad-output/project-context.md#Testing Rules] — conformance tests explicit and named; pure aggregate command/state/event tests; reuse Hexalith testing helpers; release-gate behaviors (tenant isolation, audit pairing, idempotency, redaction replay, projection freshness).
- [Source: Directory.Packages.props] — `coverlet.collector 8.0.1`; xUnit v3 / Shouldly versions.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (1M context) — BMAD dev-story workflow

### Debug Log References

- Baseline (Task 1): Conformance `294 passed, 0 failed, 0 skipped`; Server.Tests `515 passed, 0 failed, 0 skipped`. HEAD `a68c6e3` matches `baseline_commit`.
- Per-test green on `main`: `GovernanceAuditPairingSafetyNetConformanceTest` 6/6; `ConversationProjectionReadSurfaceConformanceTest` 7/7; `LiveIdempotencyConflictOracleCharacterizationTest` 4/4; `AtRiskTestRegisterGenerationTest` 5/5.
- Final (Task 7): Conformance `316 passed`; Server.Tests `515 passed`. No `src/` changes; no submodule touched (verified via `git status`).

### Completion Notes List

- **AC1** — Re-expressed the governance audit-pairing safety net against the public `ConversationAggregate.Handle(command, state) → DomainResult` surface (core + Contracts only, no `Server.*`), relocated **into the conformance oracle** as `GovernanceAuditPairingSafetyNetConformanceTest.cs`, classified **"re-express, never delete"**. The re-expression is *stronger* than the original inventory: it adds positive pairing (every governance mutation event carries `GovernanceAuditEvidenceReference`), negative enforcement (missing/mismatched evidence fails closed with `audit_pairing_required` / `audit_pairing_mismatch`), the non-governance no-audit-dependency invariant, and an aggregate-surface completeness check that uses no Server plumbing.
- **Placement decision (AC1):** the *re-expression* was relocated into the oracle; the **original Server.Tests file was kept in place** (not physically moved). The story's "ideal move" is softened by the repeatedly-stated **critical no-deletion guardrail** ("Do not delete any test here"; "nothing is deleted in this story"), which dominates. The original's purely-structural assertions (read-only-boundary reflection, privileged-justification wiring, handler-type/ctor fragments) are recorded as **plumbing-only-retire @ Story 2.1** so Story 5.2 reconciles them when the Server types relocate.
- **AC2 (projection)** — Re-expressed release-gate projection behavior through the **public read surface** (`ConversationProjectionReadService.ReadDetailAsync → Contracts.Projections.*`): redaction non-leakage through the returned DTO, governance evidence anchoring, and fail-closed gating for every degraded materializer state (`ConversationProjectionReadSurfaceConformanceTest.cs`). Covers the read-service path that Story 1.2's raw-materializer `LiveProjectionFreshnessOracleCharacterizationTest` does not. Raw-materializer orchestration mechanics → plumbing-only-retire @ Story 2.5.
- **AC2 (idempotency)** — Re-expressed observable idempotency behavior through `DomainResult` / `ConversationIdempotencyReplayResult` / `ConversationRejectedDomainEvent` (`LiveIdempotencyConflictOracleCharacterizationTest.cs`): conflict rejection before mutation, pending retryable uncertainty, rejection-reason preservation, replay-payload secret exclusion — the cases Story 1.2's `LiveIdempotencyOracleCharacterizationTest` (duplicate-replay) omits. Internal store/executor mechanics → plumbing-only-retire @ Story 2.2.
- **AC3** — Reconciled the Story 1.1 oracle-survivability couplings (telemetry/status suites + fixtures + the `Conformance.Tests → Server` project reference) and the three Story 1.2 `Live*` characterization tests into the register as `coupled-by-design-retarget-in-owning-story` with owning stories (telemetry/diagnostics → 3.3; tenant-access → 3.2; projection → 2.5; idempotency → 2.2). The project reference is **deliberately not removed** (target end-state + path recorded). No oracle suite's assertion strength was reduced (re-expressions add, never subtract; baseline strength held — conformance 294 unchanged, +22 net new).
- **AC4** — Every re-expressed test is green on unmodified `main` at `baseline_commit`. Conformance project + Server.Tests run 100% green with no net loss of asserted behavior; any assertion not reachable through the public surface is a recorded plumbing-only-retire entry, not a silent drop.
- **AC5** — Committed `docs/release-evidence/at-risk-test-register-v1.json` (+ `.md` header), generated and self-validated by `AtRiskTestRegisterGenerationTest.cs` (mirrors the existing release-evidence generator pattern; content-safety scanned). Register explicitly seeds the FR-20 removed-test justification ledger (Story 5.2) and folds in both Story 1.2 carry-forwards. **Zero `src/` changes; no sibling submodule touched; no submodule recursed.**

### File List

- `tests/Hexalith.Conversations.Conformance.Tests/GovernanceAuditPairingSafetyNetConformanceTest.cs` (new) — AC1 re-expression (relocated into the oracle).
- `tests/Hexalith.Conversations.Conformance.Tests/ConversationProjectionReadSurfaceConformanceTest.cs` (new) — AC2 projection read-surface re-expression.
- `tests/Hexalith.Conversations.Conformance.Tests/LiveIdempotencyConflictOracleCharacterizationTest.cs` (new) — AC2 idempotency re-expression (conflict/pending/reason/payload).
- `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` (new) — AC5 register generator/validator.
- `docs/release-evidence/at-risk-test-register-v1.json` (new) — the committed at-risk register (FR-20 ledger seed).
- `docs/release-evidence/at-risk-test-register-v1.md` (new) — human-readable register header.
- `_bmad-output/implementation-artifacts/1-3-decouple-the-internal-coupled-tests-that-would-break-under-refactor.md` (modified) — checkboxes, Dev Agent Record, Status → review.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — story 1.3 status → review.
- **Note:** the original `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`, `.../Projections/ConversationProjectionMaterializerTest.cs`, and `.../Idempotency/IdempotentConversationCommandExecutorTest.cs` are intentionally **unchanged** (no deletion in this story; triaged in the register).

### Change Log

| Date | Change |
|------|--------|
| 2026-06-03 | Story 1.3 implemented: re-expressed the three internal-coupled tests against the public surface and relocated the survivable nets into the conformance oracle (+22 tests, 294 → 316); committed the at-risk register seeding the FR-20 ledger; reconciled the Story 1.1/1.2 oracle-survivability couplings and carry-forwards. Zero `src/` changes. Status → review. |
| 2026-06-03 | Senior Developer Review (AI): adversarial review passed — 0 CRITICAL. Corrected stale test tallies (Conformance 315→316, +21→+22; `ConversationProjectionReadSurfaceConformanceTest` 6/6→7/7) and staged the previously-unstaged AC2 `gap` degraded-state case (+ matching register edits) so the staged snapshot matches the AC2-complete working tree. Status → done. |

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot · **Date:** 2026-06-03 · **Outcome:** Approved (auto-fixed)

### Verified against reality (not just claims)
- **Conformance project runs green: `316 passed, 0 failed, 0 skipped`** (baseline 294 + 22 new). The four new classes count 6 + 7 + 4 + 5 = 22 cases (verified per-class).
- **Server.Tests runs green: `515 passed`** — unchanged, consistent with zero Server.Tests edits.
- **Zero `src/` changes; no sibling submodule touched** (verified via `git status`). The original three Server.Tests files (`GovernanceAuditPairingSafetyNetTest`, `ConversationProjectionMaterializerTest`, `IdempotentConversationCommandExecutorTest`) are intact — no deletion, as required.
- **AC1** re-expression (`GovernanceAuditPairingSafetyNetConformanceTest`) is genuinely decoupled — no `Server.*` usings; drives `ConversationAggregate.Handle(...) → DomainResult` and is *stronger* than the original (adds missing/mismatched-evidence fail-closed enforcement).
- **AC5** register (`at-risk-test-register-v1.json`) is content-safe — forbidden-term scan (`EventStore`/`snapshot`/`SignalR`/`dispatcher`/`repository`/`poison`/drive paths) returned no hits; generator round-trips deterministically.

### Findings & dispositions (0 CRITICAL, 2 MEDIUM — both auto-fixed)
1. **[MEDIUM] Stale recorded tallies.** Story recorded Conformance `294 → 315 (+21)` and projection test `6/6`; the real working-tree run is `316 (+22)` and `7/7`. → **Fixed**: all six tally references in the story corrected.
2. **[MEDIUM] Unstaged AC-strengthening work (AC5 "only intended files are staged").** Three already-intended files held unstaged edits adding the AC2-named `gap` degraded-state case to the projection read-surface theory (+ matching register `.cs`/`.json` text). The staged snapshot omitted `gap`, which AC2 explicitly requires the public read surface to fail closed on. → **Fixed**: working-tree versions staged so the staged snapshot is AC2-complete and consistent.

No assertion strength was reduced by any re-expression (oracle holds 294 baseline + 22 net new). No production behavior was changed.
