# Oracle Blind-Spot Analysis v1 — Conversations Boilerplate Reduction

**Story:** Story 1.2 — Measure the oracle's blind spots and backfill characterization tests (gate zero, second story).
**Status:** Oracle-strengthening record. Captured 2026-06-03.
**Baseline commit:** `06641240a01e745b5db299da361f81dd6d505e6d` (branch `main`, `src/` and `tests/` clean at capture).

| Artifact | What it is |
|----------|------------|
| `oracle-blind-spot-analysis-v1.json` | The machine-readable blind-spot record: for each of the five release-gate behaviors — production path(s), the tests covering them, the measured gap, the fault-injection experiment and its result, and the disposition. Plus accepted gaps and detected variances. |
| `oracle-blind-spot-analysis-v1.md` | This self-describing header. |

## What this measures

Story 1.1 pinned the 14-suite conformance oracle green on `main`. This story measured **where that oracle was blind** on the five release-gate behaviors — (1) tenant fail-closed, (2) governance audit-pairing, (3) idempotency, (4) redaction replay, (5) projection freshness — and **backfilled characterization tests** that pin current observable behavior so a later refactor that breaks a behavior turns the oracle RED.

**Unifying finding:** the 14 oracle suites assert a synthetic scenario engine driven by seed data; the **live server decision code** (tenant access service/guard, idempotency executor, projection materializer, freshness classifier, governance pairing) was exercised only by the **server unit-test project, which is not part of the oracle**. A fail-open mutation in live code therefore rode green through the oracle. The backfill adds live-decision-code characterization tests **inside the conformance project** so the oracle now catches such mutations.

## How it was generated

- **Coverage** (per-behavior branch location, reusing the configured collector — no new tool):
  `dotnet test <project> --collect:"XPlat Code Coverage"`
- **Mutation / fault-injection** (targeted manual; Stryker.NET intentionally not introduced):
  flip one live deny/downgrade/dedup branch → run the oracle → observe the named backfill test go RED → revert.
- **Oracle run:**
  `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`
- **Generated/validated by:** `OracleBlindSpotAnalysisArtifactGenerationTest.GenerateAndSaveArtifactFile`
  (repo-root discovery → deterministic indented-JSON write into `docs/release-evidence/` → re-read + re-validate + content-safety scan).

## Result

- Oracle after backfill: **294 passed, 0 failed, 0 skipped** = 260 Story 1.1 baseline tests + 34 new tests (29 live-decision-code characterization test cases across 18 methods, including a 12-case fail-closed trigger-state theory, plus 5 artifact generator/validator tests). No existing suite was weakened, deleted, or had an assertion removed.
- **AC3 demonstrated catch:** flipping the live tenant guard's non-member deny to fail-open turns `LiveServiceShouldDenyCrossTenantMemberLeakage` RED, while the original 260-test oracle (backfills excluded) stayed all-pass under the same flip — proving the blind spot existed and is now closed.
- A fail-open fault-injection catch was demonstrated for **all five** behaviors (see the JSON `behaviors[].faultInjection.result`).
- This story moved **no production code**: all fault-injection edits were throwaway and reverted; `git status` shows zero `src/` changes.
