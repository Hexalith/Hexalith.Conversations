# Story 6.8B: Validate Operational Telemetry Cardinality Gates

Status: done

## Story

As a platform operator and reliability engineer,
I want a validation suite that proves every operational telemetry dimension is bounded and approved,
so that metric cardinality stays small and fixed under high-volume invocation and can never explode from raw identifiers, free text, or an unbounded `gate_id`.

## Acceptance Criteria

1. **AC1 — Each closed-vocabulary enum has a small fixed cardinality budget:** The validation enumerates every closed-vocabulary enum and asserts its member count is small and fixed (a cardinality budget), and that all stay under a conservative ceiling.

2. **AC2 — Metric tag KEYS are a fixed approved set per counter:** The validation asserts the observed dimension-key set for each counter exactly matches the approved set (no missing, no extra keys) under load.

3. **AC3 — Distinct tag-value set stays within the approved closed vocabulary under high-cardinality invocation:** Under many high-cardinality invocations the distinct emitted tag-value set per dimension stays within the approved closed vocabulary; raw ids / free text are never emitted as dimensions.

4. **AC4 — `gate_id` is the only string dimension and is constrained to a bounded approved set:** The validation proves `gate_id` is the only dimension outside the class/boolean vocabularies, that every emitted `gate_id` is within the bounded approved set, and that the cardinality gate would catch an unbounded/raw value (fixture of approved safe gate ids + rejection of raw candidates).

## Scope

VALIDATION story. Validates the same already-existing telemetry surfaces as Story 6.8A (`ConversationRejectionTelemetry`, `ConversationProjectionTelemetry`, `ConversationConformanceTelemetry` in `src/Hexalith.Conversations.Server/Diagnostics/`). No production source was modified. The suite lives in `tests/Hexalith.Conversations.Conformance.Tests/` and drives the real telemetry classes under a high-cardinality load through a captured `MeterListener`. Read-only: no command dispatch, event appends, projection writes, governance mutations, or external calls.

## What Was Implemented

### Files created

- `tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuite.cs` — the load harness. `RunHighCardinalityLoad()` constructs the three real telemetry classes, attaches a `MeterListener`, and invokes every closed-vocabulary class value across `HighCardinalityIterations` (50) iterations, varying a per-call unique correlation id (never a dimension) and cycling the bounded `gate_id` set. `IsGateIdWithinApprovedBudget(string)` is the cardinality gate that accepts approved gate ids and rejects unbounded/raw values.
- `tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuiteTest.cs` — the 6.8B validation tests (19 facts).

Shared with 6.8A: `TelemetryDisclosureConformanceFixtures.cs` (approved gate-id set, approved dimension keys, approved booleans, enum-derived closed vocabularies), `TelemetryValidationTestHelpers.cs` (`FakeMeterFactory`, `CapturingLogger<T>`), and `CapturedMeasurement` from the redaction suite.

### Suite: TelemetryCardinalityConformanceSuiteTest (19 test cases)

Cardinality budgets (AC1):
1. `CommandRejectionClassShouldHaveFixedCardinalityBudget` — 9 members (incl. None).
2. `TenantDenialClassShouldHaveFixedCardinalityBudget` — 6.
3. `PrivilegedAccessClassShouldHaveFixedCardinalityBudget` — 3.
4. `ProjectionFreshnessClassShouldHaveFixedCardinalityBudget` — 6.
5. `ProjectionLagClassShouldHaveFixedCardinalityBudget` — 5.
6. `PublicationFailureClassShouldHaveFixedCardinalityBudget` — 6.
7. `ConformanceStatusClassShouldHaveFixedCardinalityBudget` — 8.
8. `TenantAccessRequirementShouldHaveFixedCardinalityBudget` — 4.
9. `EveryClosedVocabularyEnumShouldStayWithinASmallBudgetCeiling` — all enums ≤ 16.
10. `ApprovedGateIdVocabularyShouldBeBoundedAndSmall` — 8 distinct approved gate ids.

Tag KEY sets (AC2):
11. `EachCounterShouldOnlyEverEmitItsApprovedDimensionKeySet` — observed keys per counter equal the approved set.
12. `EveryApprovedCounterShouldBeExercisedUnderLoad` — all 7 counters exercised.

Distinct tag-value bound under load (AC3):
13. `DistinctDimensionValuesUnderLoadShouldStayWithinTheApprovedClosedVocabulary` — every observed value is approved.
14. `DistinctDimensionValueCountPerKeyShouldNotExceedItsCardinalityBudget` — distinct count per key ≤ vocabulary size.
15. `HighCardinalityLoadShouldProduceManyMeasurementsButFewDistinctTagValues` — >1000 measurements, ≤64 distinct (key=value) pairs (proves the bound is non-vacuous).

`gate_id` bounded string dimension + gate (AC4):
16. `GateIdShouldBeTheOnlyDimensionCarryingValuesOutsideClassAndBooleanVocabularies` — only free-string dimension is `gate_id`.
17. `EveryEmittedGateIdUnderLoadShouldBeWithinTheBoundedApprovedSet`.
18. `CardinalityGateShouldRejectUnboundedOrRawGateIdValues` — gate rejects raw conversation/Party/business-record ids, free text, GUIDs, empty.
19. `CardinalityGateShouldAcceptEveryApprovedGateId`.

### Scenarios exercised

The high-cardinality load exercises every operational scenario class across all surfaces: normal ops (authorized access, current freshness, pass), duplicate commands (idempotency), projection lag (critical/breached), rebuild states (rebuilding/partially-rebuilt), subscriber failures (unsupported-schema/dead-lettered/replay-required/transient/tenant-violation), redaction events, cross-tenant denials, provider faults, privileged access (authorized + unauthorized), and configuration gaps (execution-failure conformance). Every non-`None` enum value of every dimension is emitted at least 50 times.

### Closed-vocabulary cardinality budgets asserted

| Enum | Member count |
|---|---|
| `ConversationCommandRejectionClass` | 9 |
| `ConversationTenantDenialClass` | 6 |
| `ConversationPrivilegedAccessClass` | 3 |
| `ConversationProjectionFreshnessClass` | 6 |
| `ConversationProjectionLagClass` | 5 |
| `ConversationPublicationFailureClass` | 6 |
| `ConversationConformanceStatusClass` | 8 |
| `ConversationTenantAccessRequirement` | 4 |
| Approved `gate_id` set | 8 |

Approved `gate_id` vocabulary: `tenant-isolation`, `audit-integrity`, `redaction-non-leakage`, `unsupported-schema-rejection`, `projection-rebuild-determinism`, `contract-compatibility`, `provider-portability`, `suite-run`.

## Dev Notes

- The single `MeterListener` listens on the meter NAME (`Hexalith.Conversations`) so one run captures all 7 counters. `CapturedMeasurement` carries the instrument name so per-counter key/value assertions are possible.
- `EachCounterShouldOnlyEverEmitItsApprovedDimensionKeySet` uses `SetEquals` (not subset) so an omitted approved dimension also fails — every approved key must actually be exercised.
- `HighCardinalityLoadShouldProduceManyMeasurementsButFewDistinctTagValues` is the anti-vacuous guard: the load is genuinely large (>1000 emissions), yet the distinct `(key=value)` space stays ≤64, which is the core cardinality property.
- The cardinality gate (`IsGateIdWithinApprovedBudget`) is the enforcement surface the suite exercises for `gate_id`; it accepts the 8 approved tokens and rejects raw ids, free text, GUID-suffixed gates, and empty.
- No production source modified; suite is additive and self-contained to preserve the existing green baseline.

## Validation

Commands run (Windows / .NET 10 host):

```
dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj
dotnet test  tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~TelemetryCardinality"
dotnet test  tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj
```

Results:

- Build: succeeded, 0 warnings, 0 errors.
- `TelemetryCardinality` filter: **Passed! Failed: 0, Passed: 19, Skipped: 0, Total: 19.**
- Full Conformance.Tests project (after adding both 6.8A and 6.8B): **Passed! Failed: 0, Passed: 248, Skipped: 0, Total: 248** (216 pre-existing + 32 new across 6.8A/6.8B). No regressions.

## Change Log

- 2026-05-24: Story 6.8B implemented — operational-telemetry cardinality-gate validation suite (`TelemetryCardinalityConformanceSuite` + test, sharing `TelemetryDisclosureConformanceFixtures`). 19 validation tests prove each closed-vocabulary enum has a small fixed cardinality budget, metric tag KEYS are a fixed approved set per counter, the distinct tag-value set stays within the approved closed vocabulary under high-cardinality load, and `gate_id` is the only string dimension constrained to a bounded approved set (with a gate that rejects unbounded/raw values). No production source modified.
