---
baseline_commit: 0664124
depends_on: 1-1-pin-the-conformance-oracle-green-on-main-and-snapshot-the-public-contract-shape
---

# Story 1.2: Measure the oracle's blind spots and backfill characterization tests

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want the conversation oracle's coverage gaps on the five release-gate behaviors measured and any uncovered behavior pinned by characterization tests before refactoring,
so that a silent fail-open regression cannot pass green through an unexercised path during the Boilerplate Reduction refactor.

> **Initiative context (read first):** This is **Story 1.2 of a behavior-preservation refactor** (Conversations Boilerplate Reduction), the second gate-zero story. Story 1.1 pinned the 14-suite conformance oracle green on `main` and snapshotted the public contract shape. **This story strengthens that oracle before any plumbing moves.** Its job is to *measure where the oracle is blind* on the five release-gate behaviors and *backfill characterization tests* that pin the **current observable behavior** so a later refactor that silently breaks a behavior turns a test RED. This story **moves no production code** under `src/` — it adds **test-only** artifacts and a measurement record. Do **not** "fix", strengthen, or refactor production logic; if current behavior looks wrong, you still pin *current* behavior (a characterization test asserts what the code does today, not what it should do) and log the concern. Decoupling internally-coupled suites is **Story 1.3**, not this story.

## Acceptance Criteria

### AC1 — Blind-spot measurement run and recorded (coverage + mutation/fault-injection)

**Given** the pinned 14-suite oracle from Story 1.1 (green on `main`)
**When** coverage and mutation/fault-injection analysis is run against the **five release-gate behaviors** — (1) tenant fail-closed, (2) governance audit-pairing, (3) idempotency, (4) redaction replay, (5) projection freshness
**Then** uncovered or weakly-asserted paths are identified and recorded in a committed, self-describing blind-spot analysis artifact under `docs/release-evidence/`
**And** for each of the five behaviors the artifact records: the production code path(s) under test, the test(s) currently exercising it, the measured gap (uncovered line/branch OR a surviving fault-injection mutation the oracle does not catch), and the disposition (backfilled | accepted-gap-with-rationale).

### AC2 — Characterization tests backfill identified blind spots and join the oracle

**Given** an identified blind spot from AC1
**When** a characterization test is written that asserts the **current observable behavior** on that path
**Then** it runs green on unmodified `main` (at `baseline_commit`)
**And** it is added to the conformance oracle (lives in / runs with `tests/Hexalith.Conversations.Conformance.Tests`, or is explicitly registered as an oracle test so Story 5.1's full-suite run includes it)
**And** each backfill test is traceable to the AC1 artifact entry it closes.

### AC3 — Tenant fail-closed adversarial path catches a fail-open mutation (NFR3)

**Given** tenant fail-closed specifically (NFR3 — the dominant fail-closed invariant)
**When** the blind-spot pass runs
**Then** an **adversarial cross-tenant denial path is exercised against the live decision code** (`ConversationTenantAccessService` / `ConversationTenantAccessGuard`) such that a **fail-open mutation is *caught*, not assumed impossible** — i.e. when the guard is deliberately flipped to fail-open (or a deny branch is short-circuited), at least one oracle test goes RED, and this catch is **demonstrated and recorded** (the fault-injection experiment + the red result) in the AC1 artifact
**And** the same adversarial test passes (denies) on unmodified `main`
**And** the cross-tenant denial covers the fail-closed trigger states required by project-context: **missing / unknown / disabled / stale / ambiguous / insufficient / unavailable** tenant-access state (those not already provably caught are backfilled).

### AC4 — Remaining accepted gaps logged with explicit rationale; oracle still 100% green

**Given** any release-gate-behavior path left uncovered after the backfill pass
**Then** each remaining accepted coverage gap is logged in the AC1 artifact with an explicit rationale (why it is acceptable to leave uncovered now, and which later story — if any — closes it)
**And** no release-gate behavior is left with an *unrecorded* gap.

**Given** the full conformance test project after the backfill
**When** `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...` is run on `main`
**Then** it is **100% green** (260 baseline tests from Story 1.1 + the new characterization tests, all passing)
**And** no existing suite was weakened, deleted, or had an assertion removed (oracle strength only increases — verified against the Story 1.1 baseline).

### AC5 — Artifacts committed and self-describing; scope guardrails honored

**Given** the blind-spot analysis artifact and the new characterization tests
**Then** the artifact is committed under `docs/release-evidence/` with a header (what it is, exact commands used to generate the coverage/mutation evidence, that it is the Story 1.2 oracle-strengthening record, and the `baseline_commit` it was measured at)
**And** if a repeatable generator/validator test is added it mirrors the existing `ReleaseConformanceArtifactGenerationTest` / `PublicContractShapeSnapshotGenerationTest` pattern (repo-root discovery → deterministic write into `docs/release-evidence/` → re-read + re-validate + content-safety scan)
**And** only intended files are staged; **zero production source under `src/` changes**; no sibling submodule (EventStore, Tenants, Parties, Commons, Folders, Projects, Memories, FrontComposer, …) is touched; submodules are never recursed.

## Tasks / Subtasks

- [x] **Task 1 — Re-confirm the oracle is green at the current baseline (AC4 precondition)**
  - [x] Confirm `src/` and `tests/` working tree is clean; capture `git rev-parse HEAD` and record it as `baseline_commit` (update the frontmatter above if it drifted from `0664124`).
  - [x] Run `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` and confirm the Story 1.1 baseline (**260 passed, 0 failed, 0 skipped**). If anything is red, STOP and report — a red oracle invalidates the blind-spot premise. Do **not** "fix" a red test to proceed.

- [x] **Task 2 — Measure coverage on the five release-gate behaviors (AC1)**
  - [x] Run line/branch coverage with the already-configured collector: `dotnet test ... --collect:"XPlat Code Coverage"` (coverlet.collector `8.0.1` is in `Directory.Packages.props` — **reuse it; do not add a new coverage tool**). Run across the conformance project **and** the server/aggregate test projects that exercise the five behaviors (see Dev Notes for the project list), so coverage reflects what the *whole* test corpus pins, not just the conformance suite.
  - [x] For each of the five behaviors, locate the production code path (Dev Notes maps these) and read the covering test(s). Record: covered? branch-covered? **asserted on, or merely executed?** (executed-but-unasserted is a blind spot — coverage tools count it as covered).
  - [x] **Critical distinction to measure:** the 14 conformance suites assert on a synthetic **scenario-engine** (`*ConformanceSuite.Run(seedData,…)` → `ConformanceRunResultV1` outcomes), driven by `Hexalith.Conversations.Testing` seed data — they largely do **not** execute the live `src/Hexalith.Conversations.Server` decision code (tenant guard, projection materializer, idempotency executor). Determine, per behavior, whether the oracle exercises the **real production decision path** or only the scenario-engine mirror of it. A behavior whose live enforcement code is only reachable through `Server.Tests` (not the oracle) is a prime blind spot for AC2/AC3.

- [x] **Task 3 — Mutation / fault-injection probe (AC1, AC3)**
  - [x] **Do not introduce Stryker.NET into this repo.** It is not configured in Conversations (no root `.config/dotnet-tools.json`); only sibling submodules (EventStore/Parties/FrontComposer) carry it, and FrontComposer's `tests/.../Mutation/stryker-*.json` is a *reference pattern only* — do not edit submodules. Use **targeted manual fault-injection** instead: for each of the five behaviors, identify the single most safety-critical branch (e.g. the tenant deny return, the audit-pairing guard, the idempotency dedup check, the redaction-on-replay step, the freshness-downgrade decision), temporarily flip it to the fail-open/no-op variant **in a throwaway local edit you revert**, re-run the oracle + targeted tests, and record whether any test catches it.
  - [x] A **surviving** mutation (no test goes red) is a recorded blind spot → backfill in Task 4. A **caught** mutation is recorded as proof of strength. **Revert every fault-injection edit** — none of it lands in `src/`. Capture the experiment (what was flipped, command, red/green result) in the AC1 artifact.
  - [x] Prioritize **tenant fail-closed** (AC3): prove that flipping `ConversationTenantAccessGuard` / `ConversationTenantAccessService` to fail-open makes at least one oracle test RED. If today nothing catches it, that is the headline blind spot AC3 requires you to close.

- [x] **Task 4 — Backfill characterization tests (AC2, AC3)**
  - [x] For each surviving-mutation / unasserted blind spot, write a characterization test asserting **current observable behavior** (what the code does today). Prefer **public-surface assertions** (Contracts / Client / `DomainResult` / projection query results) so the new tests survive the refactor; where catching a fail-open mutation *requires* touching `Server` internals, do so but record the coupling in the artifact and cross-reference Story 1.3 (the oracle-survivability triage) — do not let an internal-coupling concern block pinning the behavior.
  - [x] **Tenant fail-closed (AC3):** add the adversarial cross-tenant denial test against the live guard/service covering missing / unknown / disabled / stale / ambiguous / insufficient / unavailable state. It must (a) deny on `main` and (b) go red under the Task 3 fail-open mutation. Reuse the Tenants/Parties authorization test patterns and `Hexalith.EventStore.Testing` / `Hexalith.Tenants.Testing` helpers per project-context — do **not** invent new authorization fakes.
  - [x] Place tests so Story 5.1's full-suite run includes them: ideally in `tests/Hexalith.Conversations.Conformance.Tests`; if a test must live in `Server.Tests` for access reasons, register/reference it as an oracle test and note it in the artifact.
  - [x] Run each new test on `main`; confirm green (it pins current behavior, not a refactor target).

- [x] **Task 5 — Record the blind-spot analysis artifact + accepted gaps (AC1, AC4, AC5)**
  - [x] Write `docs/release-evidence/oracle-blind-spot-analysis-v1.json` (+ a `.md` header) following Story 1.1's evidence conventions: for each of the five behaviors — production path, covering test(s), measured gap, fault-injection experiment + result, disposition (backfilled with the new test name | accepted-gap-with-rationale + closing story). Include the exact coverage/mutation commands and the `baseline_commit`.
  - [x] If you add a generator/validator test, mirror `ReleaseConformanceArtifactGenerationTest.cs` / `PublicContractShapeSnapshotGenerationTest.cs`: repo-root discovery → deterministic indented-JSON write into `docs/release-evidence/` → re-read + re-validate + **content-safety scan** (no `EventStore`, `snapshot`, `SignalR`, `dispatcher`, `repository`, provider payloads, raw exceptions, drive paths, or Parties personal data in the emitted artifact).
  - [x] Log every remaining accepted gap with rationale; ensure no release-gate behavior has an unrecorded gap (AC4).

- [x] **Task 6 — Final green + commit (AC4, AC5)**
  - [x] Revert all Task 3 fault-injection edits; confirm `git status` shows **no `src/` changes**.
  - [x] Run the full conformance project: confirm **260 + N** green (N = new characterization tests), 0 failed, 0 skipped.
  - [x] Stage only intended files (new test files + `docs/release-evidence/` artifact + this story file + sprint-status). Confirm no sibling submodule is touched and no submodule was recursed.

## Dev Notes

### What this story IS (and is NOT)

- **IS:** an oracle-*strengthening* story. Measure where the Story 1.1 oracle is blind on the five release-gate behaviors, prove the gaps with coverage + targeted fault-injection, and pin current behavior with characterization tests so a future refactor that breaks a behavior turns RED. Output = committed blind-spot artifact + new test-only characterization tests + a demonstrated fail-open catch for tenant isolation.
- **IS NOT:** a refactor, a decoupling, or a bug-fix. No `src/` production code changes. Characterization tests assert **what the code does today**, not what it should do — if current behavior seems wrong, pin it and log the concern, don't change it. Decoupling the internally-coupled telemetry/conformance-status suites is **Story 1.3**. Removing plumbing-only tests happens later, per the Epic 2/3 story that removes the plumbing.
- The downstream contract: **Story 5.1** runs the *full* oracle (including this story's backfills) and confirms green was never lost. A blind spot left unmeasured here means a silent fail-open could ride through the whole refactor undetected — that is the disaster this story exists to prevent (NFR1).

### The five release-gate behaviors → production code → current test coverage

| # | Behavior | Production code under test (`src/`) | Currently exercised by | Blind-spot risk |
|---|----------|-------------------------------------|------------------------|-----------------|
| 1 | **Tenant fail-closed (NFR3)** | `Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessService.cs`, `…/ConversationTenantAccessGuard.cs` | `TenantIsolationConformanceSuite(Test)` asserts **scenario-engine outcomes** (12 scenarios, outcome/error-code level); live guard exercised mainly in `Server.Tests` (tenant-access tests) | **HIGH** — oracle may assert the *mirror*, not the live guard. AC3 demands a fail-open mutation be caught against the real code. |
| 2 | **Governance audit-pairing** | `Hexalith.Conversations.Server/Governance/*` (verification service, audit gate); aggregate governance mutation paths (SetRetentionPolicy, MarkContentSensitive, RedactMessageContent) | Composite across `BuyerAcceptance`/`ReleaseScope`/`SecondAdopter`/`AdopterConformanceSuite`; **`Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`** is the explicit safety-net inventory (this is the "re-express, never delete" test flagged for Story 1.3) | **MEDIUM** — pairing proven at outcome level + via the safety-net inventory; verify every mutation path emits its paired audit event and that a *dropped* pairing is caught. |
| 3 | **Idempotency** | `Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs`; `Hexalith.Conversations/Idempotency/*` (store, decision, outcome) | `IdempotencyConformanceSuite(Test)` (8 scenarios, outcome level); `Server.Tests/Idempotency/IdempotentConversationCommandExecutorTest.cs` | **MEDIUM** — verify a broken dedup (treating a replay as new) is caught, not just that the happy path dedups. |
| 4 | **Redaction replay** | `Hexalith.Conversations/State/ConversationRedactionState.cs`; aggregate redaction path; `…Contracts/Projections/ConversationRedactionProjectionV1.cs` | `RedactionConformanceSuite(Test)` (10 scenarios); `Tests/Aggregates/ConversationAggregateRedactionTest.cs`; `Contracts.Tests/RedactionContractTest.cs` | **MEDIUM** — verify that redacted content stays non-leaking **on replay/rebuild** (not just at first write), and that auditability/rationale survives. |
| 5 | **Projection freshness** | `Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`, `…/ConversationProjectionAccumulator.cs`; `…/Diagnostics/ConversationProjectionFreshnessClassifier.cs`; `…Contracts/TrustStates/ProjectionTrustState.cs` | Only `AdopterConformanceSuite.CheckProjectionFreshness()` (happy path) in the oracle; downgrade/stale behavior unit-tested in `Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` + `Diagnostics/ConversationProjectionFreshnessClassifierTest.cs` | **HIGH** — the oracle only proves *fresh*; the **stale/rebuilding/degraded** surfacing (a release-gate concern per project-context) is not pinned in the oracle. Prime backfill target. |

### Conformance suite mechanics (why coverage ≠ assertion here)

The 14 `*ConformanceSuiteTest` classes drive a `*ConformanceSuite` engine over **seed data** from `Hexalith.Conversations.Testing` and assert on the resulting `ConformanceRunResultV1` (counts, `ConformanceOutcome`, `ConformanceFailureClassification`, error codes, gate/requirement mappings, JSON stability, content-safety). Example: `TenantIsolationConformanceSuiteTest` asserts "exactly 12 checks, all `TenantBinding`, 7 Blocked with typed errors, 3 Unknown→`AggregateNotFound`, overall Ready, stable camelCase round-trip." This is strong **for the scenario engine**, but a coverage tool will mark the *engine* covered while the **live `Server` decision code may be entirely unexercised by the oracle**. Your AC1 job is to make that distinction explicit per behavior; your AC3 job is to prove (by fault-injection) that the live tenant guard's fail-open is actually caught by *something in the oracle*.

### Coverage & mutation tooling — use what's here, add nothing heavy

- **Coverage:** `coverlet.collector 8.0.1` is already in `Directory.Packages.props` and referenced by every test project. Use `dotnet test … --collect:"XPlat Code Coverage"` and read the produced Cobertura XML. **Do not add a different coverage package.**
- **Mutation:** **Stryker.NET is intentionally NOT used in this story.** It is not configured at the Conversations repo root (no root `.config/dotnet-tools.json`); it exists only inside sibling submodules (`Hexalith.EventStore`, `Hexalith.Parties`, `Hexalith.FrontComposer`) which are **off-limits** (submodule rule + scope rule). Do not install it as a repo tool just for this measurement. Use **targeted manual fault-injection** (flip one safety-critical branch, run tests, revert) — it is sufficient to satisfy AC1/AC3 ("a fail-open mutation is caught") and leaves no tooling footprint. Record each experiment in the artifact so it is reproducible without the tool.
- FrontComposer's `…/Mutation/stryker-*.json` and `mutation-target-manifest.json` are a **reference for how a Hexalith module documents mutation targets** — read for inspiration, never edit.

### Existing tooling/patterns to REUSE (do not reinvent)

- **Artifact generator/validator pattern:** `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` and `PublicContractShapeSnapshotGenerationTest.cs` — repo-root discovery → create `docs/release-evidence/` → write deterministic indented JSON → re-read + re-validate + content-safety scan. Mirror this if you emit `oracle-blind-spot-analysis-v1.json` as a generated artifact.
- **Content-safety scan list** (from `TenantIsolationConformanceSuiteTest` + `CoreFixtureContentSafetyTest`): the emitted artifact must not contain `EventStore`, `snapshot`, `SignalR`, `dispatcher`, `repository`, `provider-session`, `provider payload`, `raw exception`, drive paths (`C:\`, `D:\`), poison sentinels, or any Parties personal data — only public Conversations concepts and test/behavior names.
- **Story 1.1 evidence artifacts** already in `docs/release-evidence/`: `release-baseline-v1.(json|md)` (named FR-20 baseline + survivability classification), `public-contract-shape-baseline-v1.json` (196-type snapshot). Place the new blind-spot artifact alongside these, matching their naming/casing; **do not overwrite** them.
- **Test helpers:** `Hexalith.EventStore.Testing`, `Hexalith.Tenants.Testing`, `Hexalith.Conversations.Testing.Fixtures`, Shouldly, NSubstitute, xUnit v3 — reuse; don't hand-roll fakes (project-context Testing Rules).

### Test projects that exercise the five behaviors (for the coverage run)

- `tests/Hexalith.Conversations.Conformance.Tests/` — the oracle (41 source files; 14 suites + engines/fixtures + artifact tests).
- `tests/Hexalith.Conversations.Server.Tests/` — live decision code: `TenantAccess/`, `Governance/` (incl. `GovernanceAuditPairingSafetyNetTest.cs`), `Idempotency/IdempotentConversationCommandExecutorTest.cs`, `Projections/ConversationProjectionMaterializerTest.cs` + `…RebuildVerifierTest.cs` + `…ReadServiceTest.cs`, `Diagnostics/ConversationProjectionFreshnessClassifierTest.cs`.
- `tests/Hexalith.Conversations.Tests/` — pure aggregate tests: `Aggregates/ConversationAggregateRedactionTest.cs`, `…SensitivityTest.cs`, `…RetentionPolicyTest.cs`.
- `tests/Hexalith.Conversations.Contracts.Tests/` — `RedactionContractTest.cs`, `ProjectionFreshnessContractTest.cs`, `GovernanceContractTest.cs`, `AuditRecordGovernanceContractTest.cs`.

### Critical guardrails (from project-context.md)

- **Behavior preservation is the dominant gate (NFR1).** Never make a test pass by weakening another. The oracle's assertion strength must only **increase**. If `main` is red, report it — do not paper over it.
- **Fail-closed is by construction, tested adversarially (NFR3).** Cross-tenant access must be impossible by construction; AC3 requires you to *prove the oracle catches a fail-open*, not assume it. Cover missing/unknown/disabled/stale/ambiguous/insufficient/unavailable tenant state.
- **Redaction preserves auditability**; **governance commands emit paired audit/domain events with rationale** — characterization tests here pin those invariants, they don't redefine them.
- **Dapr pub/sub is at-least-once**; projection/event handlers tolerate duplicates and replay; **projection reads must surface stale/rebuilding/unavailable** rather than pretend fresh — the projection-freshness backfill should pin the degraded-state surfacing.
- Do **not** emit Parties personal data, raw EventStore envelopes, snapshot mechanics, tenant/auth context, or drive paths in any artifact (content-safety scan).
- **Submodule rule (repo CLAUDE.md):** never recurse into nested submodules; initialize/update only root-level submodules. This story needs **no** submodule operation. The Conversations module (`src/Hexalith.Conversations.*`, `tests/Hexalith.Conversations.*`) lives at the **repository root**, not in a submodule; the sibling `Hexalith.*` dirs ARE submodules — leave them untouched.
- **Greenfield latitude does not license deletion here** — this story only *adds* tests; nothing is removed.

### Project Structure Notes

- New characterization tests belong in `tests/Hexalith.Conversations.Conformance.Tests/` (preferred, so they ride the oracle) following the existing `*ConformanceSuiteTest` / xUnit v3 + Shouldly style; if a test must live in `Server.Tests` to reach the live guard, register it as an oracle test and record the placement + any internal coupling in the artifact (cross-ref Story 1.3, which owns survivability).
- The blind-spot artifact belongs in `docs/release-evidence/` (matches Story 1.1 evidence + the release/manifest/waiver fixtures). Keep generated output deterministic (sorted, normalized) so it diffs cleanly.
- **Detected variance to record (not fix):** any blind-spot test you must place in `Server.Tests` (internally coupled) is out of line with "public-surface-only oracle" — record it and hand it to Story 1.3; do not decouple here.

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 1.2] — story statement + the three AC blocks (expanded above into AC1–AC5).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Epic 1] — gate-zero oracle role; the safety-net + decision-spine this epic delivers; relation to Story 1.1 (pin) and 1.3 (decouple).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR-20] — full release-gate conformance suite must pass; no conformance test silently dropped; the oracle this story strengthens is what FR-20/Story 5.1 run.
- [Source: _bmad-output/implementation-artifacts/1-1-pin-the-conformance-oracle-green-on-main-and-snapshot-the-public-contract-shape.md] — the pinned 14 suites, the 260-test baseline, the survivability classification (3 internally-coupled suites flagged to Story 1.3), and the evidence-artifact conventions in `docs/release-evidence/`.
- [Source: _bmad-output/project-context.md#Testing Rules] — conformance tests explicit and named; tenant isolation, audit pairing, idempotency, redaction replay are release-gate concerns; xUnit v3 / Shouldly / NSubstitute / Testcontainers; reuse Tenants/Parties authorization test patterns; fail-closed cover list.
- [Source: _bmad-output/project-context.md#Critical Don't-Miss Rules] — cross-tenant impossible by construction + tested adversarially; projection reads surface stale/rebuilding; Dapr at-least-once replay tolerance; redaction preserves auditability; governance paired-audit invariant.
- [Source: tests/Hexalith.Conversations.Conformance.Tests/TenantIsolationConformanceSuiteTest.cs] — the scenario-engine assertion pattern (outcomes/classification/error-codes/content-safety) that coverage will mark "covered" while the live guard may be unexercised.
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs] — repo-root discovery + deterministic write-into-`docs/release-evidence/` + re-validate + content-safety pattern to mirror for the blind-spot artifact.
- [Source: tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs] — the governance audit-pairing safety-net inventory (Story-1.3 "re-express, never delete") relevant to behavior #2.
- [Source: tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs] — current freshness/downgrade behavior to characterize into the oracle (behavior #5).
- [Source: src/Hexalith.Conversations.Server/TenantAccess/] — the live fail-closed decision code (`ConversationTenantAccessService`, `ConversationTenantAccessGuard`) the AC3 adversarial test must exercise and the fault-injection must turn red.
- [Source: Directory.Packages.props] — `coverlet.collector 8.0.1` (the configured coverage collector to reuse); no Stryker at repo root.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8, 1M context)

### Debug Log References

- Baseline oracle at `0664124`: **260 passed, 0 failed, 0 skipped** (`dotnet test tests/Hexalith.Conversations.Conformance.Tests/...`). Premise holds.
- After backfill: **294 passed, 0 failed, 0 skipped** (260 baseline + 34 new: 29 live-decision-code characterization test *cases* across 18 methods — incl. a 12-case fail-closed trigger-state theory — + 5 artifact generator/validator tests). Verified green across consecutive full runs.
- **Fault-injection experiments (all throwaway, reverted; `src/` ends byte-clean):**
  - Tenant (AC3 headline): flipped `ConversationTenantAccessService` non-member deny → Allowed. `LiveServiceShouldDenyCrossTenantMemberLeakage` went RED **and** the original 260-test oracle (backfills filtered out) stayed all-pass under the same flip — proving the blind spot.
  - Governance: dropped the audited retention mutation in `ConversationProjectionMaterializer.Apply(RetentionPolicySet)` → `LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence` RED.
  - Idempotency: re-executed the Duplicate arm in `IdempotentConversationCommandExecutor` → `LiveExecutorShouldReplayDuplicateWithoutReinvokingMutation` RED.
  - Redaction replay: leaked original text in `ConversationProjectionMaterializer.Apply(MessageAppended)` → `LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction` RED.
  - Projection freshness: suppressed the stale downgrade in `CreateFreshness` → `LiveMaterializerShouldSurfaceStaleProjectionAsNonTrustBearing` RED.
- Observed once (not reproduced): a transient race between the pre-existing `PublicContractShapeSnapshotGenerationTest` (writes a committed evidence file) and `ReleaseBaselineValidationTest` (reads it) under parallelism. Committed file stays byte-identical; not caused by this story; recorded for Story 1.3.

### Completion Notes List

- **Root finding:** the 14 oracle suites assert a synthetic scenario engine; the live `Server` decision code for all five behaviors was exercised only by the (non-oracle) `Server.Tests` project, so a fail-open mutation rode green through the oracle. The conformance `.csproj` already references `Hexalith.Conversations.Server`, so the backfilled characterization tests exercise the **live** code from inside the oracle — a fail-open mutation now turns the oracle RED.
- **Dispositions:** all five behaviors **backfilled** (live-decision-code characterization tests pinning current observable behavior, green on `main`). One accepted gap recorded with rationale: the live governance audit *gate* (fail-closed-on-sink-failure) is `internal` (visible only to `Server.Tests`) and unreachable from the oracle → the oracle backfill pins the public materialized pairing invariant instead; the internal gate is handed to **Story 1.3** (internal-coupling triage). Second accepted gap: exhaustive non-gate branch coverage, kept exercised by Story 5.1's full-suite run.
- **Scope guardrails honored:** zero production `src/` changes (verified `git status`), no sibling submodule touched, no submodule recursed. Reused `coverlet.collector 8.0.1`; Stryker.NET not introduced. Reused `Hexalith.Tenants` / `Hexalith.EventStore` testing idioms and existing fakes' construction patterns; no new authorization fakes invented.
- **Traceability (AC2):** the artifact's `behaviors[].backfillTests` name each backfill test method; each test class doc-comment names its behavior and references the artifact.

### File List

- `tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/LiveProjectionFreshnessOracleCharacterizationTest.cs` (new — projection freshness #5, redaction replay #4, governance pairing #2)
- `tests/Hexalith.Conversations.Conformance.Tests/LiveIdempotencyOracleCharacterizationTest.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` (new — generator/validator)
- `docs/release-evidence/oracle-blind-spot-analysis-v1.json` (new — generated artifact)
- `docs/release-evidence/oracle-blind-spot-analysis-v1.md` (new — header)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — 1-2 → in-progress → review)
- `_bmad-output/implementation-artifacts/1-2-measure-the-oracles-blind-spots-and-backfill-characterization-tests.md` (modified — this story file)

## Change Log

| Date | Change |
|------|--------|
| 2026-06-03 | Story 1.2 implemented: measured the oracle's blind spots on the five release-gate behaviors (coverage + targeted manual fault-injection), backfilled live-decision-code characterization tests (29 cases / 18 methods) into the conformance oracle, demonstrated a fail-open catch for all five behaviors (AC3 tenant catch proven against the live guard with the original oracle shown blind), and committed the self-describing `oracle-blind-spot-analysis-v1.json`/`.md` evidence with a generator/validator test. Oracle 294 green (260 + 34). Zero `src/` changes. Status → review. |
| 2026-06-03 | Story 1.2 review (auto-fix): corrected the recorded oracle size to the actual `dotnet test` count (**294** = 260 + 34, was mis-recorded as 286 in the story and 281 in the evidence artifact), regenerated `oracle-blind-spot-analysis-v1.json`/`.md`, completed behavior-1 backfill-test traceability (all 8 tenant tests listed), and added a self-consistency assertion (`total == baseline + new`) to `OracleBlindSpotAnalysisArtifactGenerationTest`. Oracle re-run: 294 green. Status → done. |
