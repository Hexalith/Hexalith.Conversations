# Story 6.8A: Validate Operational Telemetry Redaction

Status: done

## Story

As a platform operator and security reviewer,
I want a validation suite that proves the operational telemetry surfaces redact unsafe values,
so that metric dimensions and structured logs can never leak conversation content, user free text, raw business-record identifiers, prompt/content fragments, unbounded fault strings, provider payloads, redacted content, unauthorized identifiers, or cross-tenant Party details.

## Acceptance Criteria

1. **AC1 — Telemetry metric tags and structured-log shapes exclude unsafe values:** The validation drives the real telemetry surfaces and proves the emitted metric dimensions and log messages exclude conversation content, user free text, raw business-record identifiers, prompt/content fragments, unbounded fault strings, provider payloads, redacted content, unauthorized identifiers, and cross-tenant Party details.

2. **AC2 — Metric dimensions only carry closed-vocabulary class values plus bounded booleans plus the bounded gate id:** The validation proves every emitted dimension value is either a closed-vocabulary class token, a bounded boolean (`"true"`/`"false"`), or the bounded `gate_id` — never a raw conversation/Party/provider/file identifier.

3. **AC3 — Telemetry APIs reject the sentinel `None` enum value:** The validation asserts each telemetry method that defines a `None` guard throws `ArgumentException` when the sentinel is supplied, preventing emission of a `none` dimension value from uninitialized code paths.

A failure must identify the surface, the forbidden value class, and the fixture that tripped it.

## Scope

This is a VALIDATION story. The telemetry it validates already exists in `src/Hexalith.Conversations.Server/Diagnostics/`:

- `ConversationRejectionTelemetry` (`conversations.command.rejections`, `conversations.tenant.denials`, `conversations.privileged.access`)
- `ConversationProjectionTelemetry` (`conversations.projection.freshness`, `conversations.projection.rebuild`, `conversations.publication.failures`)
- `ConversationConformanceTelemetry` (`conversations.conformance.outcomes`)

No production source was modified. The suite lives in `tests/Hexalith.Conversations.Conformance.Tests/` (which has a ProjectReference to Server) and drives the real telemetry classes via a test `IMeterFactory` + capturing loggers, capturing live emissions with a `MeterListener`. It performs no aggregate command dispatch, event appends, projection writes, governance mutations, or external calls.

## What Was Implemented

### Files created

- `tests/Hexalith.Conversations.Conformance.Tests/TelemetryDisclosureConformanceFixtures.cs` — shared fixtures: approved `gate_id` set (8 tokens), approved dimension KEY set per counter, approved boolean vocabulary, the forbidden-value fixtures (9 disclosure classes), the closed-vocabulary class-token expectations derived from the real enums, and the `TelemetryValidationScenario` list (13 scenarios). Also `ForbiddenValueFixture` record.
- `tests/Hexalith.Conversations.Conformance.Tests/TelemetryValidationTestHelpers.cs` — `FakeMeterFactory` (`IMeterFactory`) and `CapturingLogger<T>` (`ILogger<T>`), duplicated locally because the Server.Tests internal helpers are not referenced by Conformance.Tests.
- `tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuite.cs` — the validation harness. `Run()` constructs the three real telemetry classes, attaches a `MeterListener` to the `Hexalith.Conversations` meter, exercises all 13 scenarios, and returns a `TelemetryCaptureResult` (captured measurements + log messages). `NoneGuardProbes()` returns the 8 `None`-sentinel guard probes. Supporting records: `CapturedMeasurement`, `TelemetryCaptureResult`, `NoneGuardProbe`.
- `tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuiteTest.cs` — the 6.8A validation tests (13 facts).

### Suite: TelemetryRedactionConformanceSuiteTest (13 test cases)

1. `RunShouldEmitAtLeastOneMeasurementPerCounter` — all 7 counters are exercised.
2. `EveryMeasurementShouldCarryOnlyApprovedDimensionKeys` — no dimension key outside the approved per-counter set.
3. `NoMeasurementDimensionShouldEverCarryAForbiddenValue` — scans every captured dimension value against all 9 forbidden-value fixtures.
4. `NoMeasurementDimensionShouldCarryRawIdentifierShapes` — no raw conversation/Party/provider/file id markers in any dimension.
5. `ClassDimensionsShouldOnlyCarryClosedVocabularyTokens` — every class dimension value is within its enum-derived closed vocabulary.
6. `BooleanDimensionsShouldOnlyCarryBoundedTrueOrFalseTokens` — `retryable`/`blocking` carry only `"true"`/`"false"`.
7. `GateIdDimensionShouldOnlyCarryApprovedBoundedGateIds` — every emitted `gate_id` is in the bounded approved set.
8. `GateIdShouldBeTheOnlyStringDimensionOutsideTheClassAndBooleanVocabularies` — proves the dimension shape contract.
9. `NoStructuredLogMessageShouldEverCarryAForbiddenValue` — scans every captured log message against the 9 fixtures.
10. `StructuredLogMessagesShouldNotCarryTenantOrPartyOrConversationIdShapes` — log messages exclude `TenantId`/`ConversationId`/`PartyId` and raw id prefixes.
11. `EveryTelemetrySurfaceShouldRejectTheSentinelNoneValue` — all 8 `None` guards throw `ArgumentException` (AC3).
12. `NoneSentinelGuardShouldPreventEmissionOfANoneDimensionValue` — no captured dimension ever equals `none`.
13. `FixtureForbiddenValuesShouldCoverEveryRequiredDisclosureClass` — fixture self-guard: all 9 required disclosure classes are present in the scan set (keeps the redaction proof non-vacuous).

### Scenarios exercised

The 13 `TelemetryValidationScenario` members drive the real surfaces: normal operations, redaction event, cross-tenant denial, provider fault, malformed metadata, privileged access, stale projection, audit-unavailable, duplicate command, projection lag, rebuild state, subscriber failure, configuration gap. Each scenario records real signals; the forbidden content is supplied only via the typed correlation-id parameter (bound for ILogger, never a metric tag) so redaction can be proven on the captured output.

### Closed-vocabulary cardinality budgets asserted

The class-dimension assertions are driven by the live enum members (lowercased, sentinel `None` excluded):

| Dimension key | Source enum | Token count (excl. None) |
|---|---|---|
| `rejection_class` | `ConversationCommandRejectionClass` | 8 |
| `denial_class` | `ConversationTenantDenialClass` | 5 |
| `access_class` | `ConversationPrivilegedAccessClass` | 2 |
| `freshness_class` / `rebuild_class` | `ConversationProjectionFreshnessClass` | 5 |
| `lag_class` | `ConversationProjectionLagClass` | 4 |
| `failure_class` | `ConversationPublicationFailureClass` | 5 |
| `status_class` | `ConversationConformanceStatusClass` | 7 |
| `operation_class` | `ConversationTenantAccessRequirement` | 4 (no None) |
| `gate_id` | bounded approved set | 8 |

## Dev Notes

- The forbidden-value scan uses case-insensitive `ShouldNotContain` against synthetic disclosure fixtures spanning all 9 required classes (conversation content, user free text, raw business-record id, prompt fragment, unbounded fault string, provider payload, redacted content, unauthorized identifier, cross-tenant Party detail).
- Capture reuses the production `MeterListener` approach from the Server.Tests telemetry tests, but listens on the meter NAME (`Hexalith.Conversations`) rather than a single instrument name, so a single listener captures all 7 counters in one run.
- Scenario tokens and fixture machine names are kept clear of the content-safety blocklist substrings; the suites do not serialize free-text descriptions to JSON, so the conformance-suite forbidden-fragment scan does not apply here.
- The `None` guard validation asserts the existing throw behaviour — the guards already throw `ArgumentException` before `Counter.Add`, so no `none` token can ever be emitted (cross-checked by `NoneSentinelGuardShouldPreventEmissionOfANoneDimensionValue`).
- No production source modified; suites are additive and self-contained to preserve the existing green baseline.

## Validation

Commands run (Windows / .NET 10 host):

```
dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj
dotnet test  tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~TelemetryRedaction"
dotnet test  tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj
```

Results:

- Build: succeeded, 0 warnings, 0 errors.
- `TelemetryRedaction` filter: **Passed! Failed: 0, Passed: 13, Skipped: 0, Total: 13.**
- Full Conformance.Tests project (after adding both 6.8A and 6.8B): **Passed! Failed: 0, Passed: 248, Skipped: 0, Total: 248** (216 pre-existing + 32 new across 6.8A/6.8B). No regressions.

## Change Log

- 2026-05-24: Story 6.8A implemented — operational-telemetry redaction validation suite (`TelemetryRedactionConformanceSuite` + test, shared `TelemetryDisclosureConformanceFixtures`, local `TelemetryValidationTestHelpers`). 13 validation tests drive the real telemetry surfaces and prove metric dimensions and structured logs exclude unsafe values, dimensions carry only closed-vocabulary class tokens + bounded booleans + bounded gate_id, and all 8 `None` sentinels are rejected. No production source modified.
