# Release Conformance Baseline v1 — Conversations Boilerplate Reduction

**Story:** 1.1 — Pin the conformance oracle green on `main` and snapshot the public contract shape
**Status:** Behavior-preservation baseline (gate zero). Captured 2026-06-02.
**Initiative:** Conversations Boilerplate Reduction (behavior-preservation refactor).

This directory's `release-baseline-v1.*` and `public-contract-shape-baseline-v1.json` files are the **trusted,
unchanging behavioral baseline** that every later refactor in Epics 2–5 diffs against. They are committed
evidence, not throwaway console output. **Story 1.1 moved no production code** — it only ran the existing
oracle, proved it green, captured a deterministic public-contract-shape snapshot, and classified each suite's
refactor-survivability.

---

## What these artifacts are

| File | What it is | Consumed by |
|------|------------|-------------|
| `release-baseline-v1.json` | The named FR-20 baseline run record: commit SHA, the 14 `*ConformanceSuiteTest` classes, test counts, all-pass result, run date, plus the AC3 oracle-survivability classification. | **FR-20** (release-gate conformance must stay green) |
| `release-baseline-v1.md` | This self-describing header — what the baseline is, the exact commands that produced it, and the Story 1.3 coupling hand-off. | Humans / Story 1.3 |
| `public-contract-shape-baseline-v1.json` | Deterministic reflection snapshot of every exported public type/member of `Hexalith.Conversations.Contracts` (196 types). | **Story 5.1** (final public-contract-shape diff) |

---

## AC1 — Oracle proven green on unmodified `main`

- **Baseline commit:** `ceb7fbe958a6b89a3b1ad01a3c98252cc766b4fe` (branch `main`, `src/` and `tests/` working tree clean).
- **Exact command:**

  ```sh
  dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj
  ```

- **Result:** 14 `*ConformanceSuiteTest` classes → **214 tests, 100% pass**. Full project on unmodified `main`
  → **248 tests, 248 passed, 0 failed, 0 skipped** (the other 34 tests are the 4 non-suite validation classes:
  `ConformanceManifestValidationTest`, `CoreFixtureContentSafetyTest`, `ReleaseConformanceArtifactGenerationTest`,
  `ReleaseWaiverValidationTest`).
- **Toolchain:** .NET SDK `10.0.302`, target `net10.0`, xUnit v3 + Shouldly.

> A red oracle would invalidate the entire baseline premise. The suite was **not** modified, weakened, or
> "fixed" to make it pass — it was already green on unmodified `main`.

The 14 suite classes and their release-gate behaviors are enumerated in `release-baseline-v1.json`
(`conformanceOracle.suiteClasses`).

---

## AC2 — Public contract-shape snapshot

- **Artifact:** `public-contract-shape-baseline-v1.json` — 196 exported public types of
  `Hexalith.Conversations.Contracts`, with members, property types, constructor/method signatures, and (where
  present) enum members. Sorted by namespace → type → member for byte-stable diffing.
- **Exact command (re-runnable identically by Story 5.1):**

  ```sh
  dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj \
    --filter "FullyQualifiedName~PublicContractShapeSnapshotGenerationTest"
  ```

- **Generator:** `tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs`
  (a test-only addition by Story 1.1, mirroring the existing `ReleaseConformanceArtifactGenerationTest`
  repo-root-discovery → deterministic write → re-read + re-validate pattern). This is **not** production code
  under `src/`.
- **Six release-gate areas covered** (see `releaseGateAreaCoverage` in the JSON, asserted by
  `SnapshotShouldCoverAllSixReleaseGateBehaviorAreas`): tenant isolation, governance/audit, idempotency,
  redaction, projection freshness, contract validation.
- **Note on closed vocabularies:** Conversations models its closed vocabularies (`ConversationErrorCode`,
  etc.) with the Hexalith **smart-enum record** pattern (records with static readonly instances), not CLR
  `enum`s. The snapshot therefore captures these members as **static properties**; this is expected and the
  full closed vocabulary is present.
- **Content safety:** the captured surface is scanned for forbidden substrate fragments
  (`EventStore`, `snapshot`, `SignalR`, `dispatcher`, `repository`, provider payloads, raw exceptions, drive
  paths) by `SnapshotShouldBeContentSafe`. It contains only public Conversations contract type/member names.

---

## AC3 — Oracle survivability classification & Story 1.3 hand-off

Each suite was classified by inspecting `using` directives and instantiated types of the suite test class **and
its companion engine/fixture files**. Full structured detail is in `release-baseline-v1.json`
(`oracleSurvivability`). Summary:

**Public-surface-only (11 suites)** — survive the refactor: `TenantIsolation`, `Idempotency`,
`ContractValidation`, `Redaction`, `EventSchemaEvolution`, `ProviderPortability`, `PlatformEvidenceSeparation`,
`BuyerAcceptance`, `ReleaseScope`, `SecondAdopter`, `Adopter`.

**Internally-coupled → flagged to Story 1.3 (3 suites):**

1. **`TelemetryCardinalityConformanceSuiteTest`** — `TelemetryCardinalityConformanceSuiteTest.cs` +
   `TelemetryCardinalityConformanceSuite.cs` `using Hexalith.Conversations.Server.Diagnostics` /
   `Server.TenantAccess`; bind to `ConversationCommandRejectionClass`, `ConversationTenantDenialClass`.
2. **`TelemetryRedactionConformanceSuiteTest`** — `TelemetryRedactionConformanceSuiteTest.cs` +
   `TelemetryRedactionConformanceSuite.cs`, same `Server.Diagnostics` / `Server.TenantAccess` coupling.
3. **`ConformanceStatusConformanceSuiteTest`** — ⚠️ **discrepancy from the story's pre-analysis table.** The
   story classified this suite (#7) as public-surface-only. Verification per AC3 ("verify, do not trust it
   blindly") found that while the test class itself uses only `Testing.Fixtures`, its companion engine
   `ConformanceStatusConformanceSuite.cs` and `ConformanceStatusConformanceFixtures.cs` depend on
   `Hexalith.Conversations.Server.Diagnostics` (`ConversationConformanceStatusClassifier`,
   `ConversationConformanceStatusClass`). **It is internally coupled and is hereby flagged to Story 1.3.**

**Also noted (not a suite test class):** `TelemetryDisclosureConformanceFixtures.cs` is a shared fixture that
also depends on `Server.Diagnostics` / `Server.TenantAccess`; recorded so Story 1.3 does not miss it.

**Project-level coupling (survivability risk):**
`Hexalith.Conversations.Conformance.Tests.csproj` references **`Hexalith.Conversations.Server`** (alongside
`Contracts`, `Client`, `Testing`). This reference is the structural reason the suites above can compile against
`Server.Diagnostics`. **Recorded as a Story 1.3 survivability risk — deliberately NOT removed here** (removing
it would break the coupled suites' compilation, which is exactly Story 1.3's job).

> Nothing was decoupled, removed, or "improved" in Story 1.1. Classification and flagging only.

---

## Regenerating / verifying

```sh
# AC1 — re-prove the oracle green (full project)
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj

# AC1 — just the 14 *ConformanceSuiteTest classes (expect 214 passed)
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj \
  --filter "FullyQualifiedName~ConformanceSuiteTest"

# AC2 — regenerate + re-validate the public contract-shape snapshot
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj \
  --filter "FullyQualifiedName~PublicContractShapeSnapshotGenerationTest"
```
