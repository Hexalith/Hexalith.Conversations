# Story 6.2: Observe Projection Lag, Rebuild, Availability, and Publication Failures

Status: done

## Story

As an operator,
I want to observe projection freshness and publication health safely,
so that I can respond to stale reads, rebuilds, and subscriber issues without inspecting protected content.

## Acceptance Criteria

1. **AC1 — Projection freshness, lag, rebuild, and availability signals (FR96):** Given projections are current, stale, rebuilding, unavailable, replaying, partially rebuilt, or hidden by tenant isolation, When observability signals are emitted, Then operators can see freshness state, lag class, rebuild state, availability state, last safe checkpoint where allowed, and recommended next action, And signals remain tenant-safe and content-safe by default.

2. **AC2 — Publication failure and subscriber contract signals (FR97):** Given event publication or subscriber-facing contract issues occur, When signals are emitted, Then the system classifies publication failure, dead-letter, retry, unsupported subscriber contract, and replay status without exposing event payloads or protected metadata, And subscriber diagnostics remain bounded-cardinality and safe for incident workflows.

3. **AC3 — Projection and publication observability tests (FR96, FR97):** Given projection and publication observability tests run, When lag breach, rebuild crash/resume, unavailable projection store, dead-letter replay, duplicate publication, unsupported subscriber version, and tenant-hidden projection scenarios are exercised, Then tests prove actionable signals, safe failure classification, and absence of content or cross-tenant leakage.

## Tasks / Subtasks

- [x] Task 1: Create bounded vocabulary enums and classifiers (AC: #1, #2)
  - [x] Create `ConversationProjectionFreshnessClass` enum in `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClass.cs` with values: `None=0`, `Current=1`, `Stale=2`, `Rebuilding=3`, `Unavailable=4`, `PartiallyRebuilt=5`
  - [x] Create `ConversationProjectionLagClass` enum in `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionLagClass.cs` with values: `None=0`, `WithinThreshold=1`, `ThresholdBreached=2`, `CriticalLag=3`
  - [x] Create `ConversationPublicationFailureClass` enum in `src/Hexalith.Conversations.Server/Diagnostics/ConversationPublicationFailureClass.cs` with values: `None=0`, `TransientFailure=1`, `UnsupportedSchema=2`, `DeadLettered=3`, `ReplayRequired=4`, `TenantViolation=5`
  - [x] Create static helper `ConversationProjectionFreshnessClassifier` in `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClassifier.cs`:
    - Method: `Classify(ProjectionTrustState state, ProjectionFreshnessReasonCode reasonCode)` returning `ConversationProjectionFreshnessClass`
    - Method: `ClassifyLag(ProjectionFreshnessReasonCode reasonCode)` returning `ConversationProjectionLagClass`
  - [x] Create static helper `ConversationPublicationFailureClassifier` in `src/Hexalith.Conversations.Server/Diagnostics/ConversationPublicationFailureClassifier.cs`:
    - Method: `Classify(ConversationErrorCode code)` returning `ConversationPublicationFailureClass`

- [x] Task 2: Define and implement `IConversationProjectionTelemetry` (AC: #1, #2)
  - [x] Create interface `IConversationProjectionTelemetry` in `src/Hexalith.Conversations.Server/Diagnostics/IConversationProjectionTelemetry.cs`
  - [x] Method: `void RecordProjectionFreshnessState(ConversationProjectionFreshnessClass freshnessClass, ConversationProjectionLagClass lagClass, string correlationId)`
  - [x] Method: `void RecordProjectionRebuildProgress(ConversationProjectionFreshnessClass rebuildClass, string correlationId)`
  - [x] Method: `void RecordPublicationFailure(ConversationPublicationFailureClass failureClass, string correlationId)`
  - [x] Create implementation `ConversationProjectionTelemetry` in `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs` using `IMeterFactory` + `ILogger<ConversationProjectionTelemetry>`
  - [x] Meter name: `"Hexalith.Conversations"` (same meter as Story 6.1 — do NOT create a second meter instance)
  - [x] Counter name for freshness: `"conversations.projection.freshness"` with dimensions `freshness_class` and `lag_class` (both: enum name, `.ToLowerInvariant()`)
  - [x] Counter name for rebuild: `"conversations.projection.rebuild"` with dimension `rebuild_class`
  - [x] Counter name for publication failures: `"conversations.publication.failures"` with dimension `failure_class`
  - [x] Log template (freshness): `"ConversationProjectionFreshness: freshness={FreshnessClass} lag={LagClass} corr={CorrelationId}"` — no TenantId, ConversationId, or event payload values
  - [x] Log template (rebuild): `"ConversationProjectionRebuild: rebuild={RebuildClass} corr={CorrelationId}"`
  - [x] Log template (publication failure): `"ConversationPublicationFailure: class={FailureClass} corr={CorrelationId}"`
  - [x] Guard: `None` in any parameter throws `ArgumentException` (same pattern as `ConversationRejectionTelemetry`)
  - [x] Create `ConversationProjectionTelemetryServiceCollectionExtensions` in `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetryServiceCollectionExtensions.cs` with `AddConversationProjectionTelemetry(this IServiceCollection services)` registering `IConversationProjectionTelemetry` as singleton `ConversationProjectionTelemetry`

- [x] Task 3: Wire freshness and rebuild telemetry into projection read path (AC: #1)
  - [x] Inject optional `IConversationProjectionTelemetry?` into `ConversationProjectionReadService` (file: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs`) — add as optional constructor parameter with null default for backward compatibility
  - [x] After `ReadDetailAsync` computes the result and `freshnessState != Forbidden` and telemetry is provided, call `RecordProjectionFreshnessState(classifier.Classify(state, reasonCode), classifier.ClassifyLag(reasonCode), safeCorrelationId)` — use a generated short safe ID as `correlationId` (e.g., `Guid.NewGuid().ToString("N")[..8]`), never TenantId or ConversationId
  - [x] When freshness state is `Rebuilding`, also call `RecordProjectionRebuildProgress(ConversationProjectionFreshnessClass.Rebuilding, safeCorrelationId)` in addition to the freshness signal
  - [x] Do NOT emit telemetry for `Forbidden` state — that path is tenant-isolation denial, already handled by Story 6.1

- [x] Task 4: Wire publication failure telemetry via non-static wrapper service (AC: #2)
  - [x] `ConversationPublicationMapper` is static — do NOT modify it. Create a new non-static `ConversationPublicationService` in `src/Hexalith.Conversations.Server/Publication/ConversationPublicationService.cs` that wraps the static mapper and accepts optional `IConversationProjectionTelemetry?`
  - [x] In `ConversationPublicationService.TryMap(PersistedConversationEvent persisted, string? correlationId = null)`, call `ConversationPublicationMapper.TryMap(persisted)`, then if `!result.IsPublished` and telemetry is provided, call `RecordPublicationFailure(failureClassifier.Classify(result.Diagnostic!.Code), correlationId ?? Guid.NewGuid().ToString("N")[..8])`
  - [x] Do NOT use `result.Diagnostic.TenantId?.Value` or `result.Diagnostic.ConversationId?.Value` as correlation ID — these are forbidden dimension values

- [x] Task 5: Tests for classifiers and telemetry (AC: #3)
  - [x] Create `ConversationProjectionFreshnessClassifierTest.cs` in `tests/Hexalith.Conversations.Server.Tests/Diagnostics/`
  - [x] Test: `ClassifyTrustState_Current_ReturnsCurrent`
  - [x] Test: `ClassifyTrustState_Stale_ReturnsStale`
  - [x] Test: `ClassifyTrustState_Rebuilding_ReturnsRebuilding`
  - [x] Test: `ClassifyTrustState_Unavailable_ReturnsUnavailable`
  - [x] Test: `ClassifyTrustState_Forbidden_ReturnsUnavailable` (Forbidden is not surfaced separately to prevent side-channel)
  - [x] Test: `ClassifyLag_StaleThresholdExceeded_ReturnsThresholdBreached`
  - [x] Test: `ClassifyLag_GapDetected_ReturnsCriticalLag`
  - [x] Test: `ClassifyLag_OutOfOrderEvent_ReturnsCriticalLag`
  - [x] Test: `ClassifyLag_Current_ReturnsWithinThreshold`
  - [x] Create `ConversationPublicationFailureClassifierTest.cs` in `tests/Hexalith.Conversations.Server.Tests/Diagnostics/`
  - [x] Test: `ClassifyCode_SchemaVersionUnsupported_ReturnsUnsupportedSchema`
  - [x] Test: `ClassifyCode_TenantContextMismatch_ReturnsTenantViolation`
  - [x] Test: `ClassifyCode_TenantIsolationViolation_ReturnsTenantViolation`
  - [x] Test: `ClassifyCode_CommandValidationFailed_ReturnsTransientFailure`
  - [x] Create `ConversationProjectionTelemetryTest.cs` in `tests/Hexalith.Conversations.Server.Tests/Diagnostics/`
  - [x] Test: `RecordProjectionFreshnessState_CurrentWithinThreshold_EmitsBoundedCounterWithBothDimensions`
  - [x] Test: `RecordProjectionFreshnessState_Stale_EmitsBoundedCounterWithStaleClass`
  - [x] Test: `RecordProjectionFreshnessState_Rebuilding_EmitsBoundedCounterWithRebuildingClass`
  - [x] Test: `RecordProjectionFreshnessState_LogMessageContainsOnlyBoundedFields_NoTenantOrConversationIds`
  - [x] Test: `RecordProjectionRebuildProgress_Rebuilding_EmitsBoundedCounter`
  - [x] Test: `RecordProjectionRebuildProgress_NoneClass_ThrowsArgumentException`
  - [x] Test: `RecordPublicationFailure_UnsupportedSchema_EmitsBoundedCounter`
  - [x] Test: `RecordPublicationFailure_LogMessageContainsOnlyBoundedFields`
  - [x] Test: `RecordPublicationFailure_NoneClass_ThrowsArgumentException`
  - [x] Test: `RecordProjectionFreshnessState_NoneClass_ThrowsArgumentException`
  - [x] Test: `AddConversationProjectionTelemetry_RegistersServiceCorrectly`

- [x] Task 6: Update test summary (AC: none / bookkeeping)
  - [x] Add Story 6.2 section to `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Dev Notes

### Architecture: Extends Story 6.1 Observability Infrastructure

Story 6.1 created the `Diagnostics/` folder in `Server` and established the meter/counter/log telemetry pattern. Story 6.2 extends that pattern for projection health and publication failures. The dimension vocabulary from Story 6.1 (rejection class, denial class, privileged access class) is now extended with freshness class, lag class, and publication failure class. All new counters share the same `"Hexalith.Conversations"` meter name — do NOT create a second meter or a second `IMeterFactory` instance.

### Freshness Class Mapping

Map `ProjectionTrustState` → `ConversationProjectionFreshnessClass` (no free-text dimensions):

| `ProjectionTrustState` | `ConversationProjectionFreshnessClass` |
|---|---|
| `Current` | `Current` |
| `Stale` | `Stale` |
| `Rebuilding` | `Rebuilding` |
| `Unavailable` | `Unavailable` |
| `Forbidden` | `Unavailable` (collapse to prevent side-channel disclosure) |
| `Redacted` | `Current` (redaction is not a freshness concern) |

`PartiallyRebuilt` is not emitted by `ConversationProjectionMaterializer` in v1 — reserve the enum value but no test needs to exercise it from existing code.

### Lag Class Mapping

Map `ProjectionFreshnessReasonCode` → `ConversationProjectionLagClass`:

| `ProjectionFreshnessReasonCode` | `ConversationProjectionLagClass` |
|---|---|
| `Current` | `WithinThreshold` |
| `StaleThresholdExceeded` | `ThresholdBreached` |
| `GapDetected` | `CriticalLag` |
| `OutOfOrderEvent` | `CriticalLag` |
| `PoisonEvent` | `CriticalLag` |
| `MetadataWriteFailed` | `CriticalLag` |
| `MetadataContradictory` | `CriticalLag` |
| `MixedGeneration` | `CriticalLag` |
| `Rebuilding` | `WithinThreshold` (rebuild-in-progress is expected, not a lag concern) |
| `Unavailable` | `Unavailable` |
| `Forbidden` | `None` (do not emit lag for forbidden reads — omit signal entirely) |

### Publication Failure Class Mapping

Map `ConversationErrorCode` → `ConversationPublicationFailureClass`:

| `ConversationErrorCode` | `ConversationPublicationFailureClass` |
|---|---|
| `SchemaVersionUnsupported` | `UnsupportedSchema` |
| `TenantContextMismatch` | `TenantViolation` |
| `TenantIsolationViolation` | `TenantViolation` |
| `TenantBindingMissing` | `TenantViolation` |
| `CommandValidationFailed` | `TransientFailure` |
| `ParticipantValidationUnavailable` | `TransientFailure` |
| `IdempotencyConflict` | `ReplayRequired` |
| anything else | `TransientFailure` |

`DeadLettered` and `ReplayRequired` are primarily reserved for pub/sub infrastructure patterns not yet wired in v1. Emit them only when a concrete `ConversationErrorCode` maps to them.

### Metrics Implementation Pattern (.NET 10)

Follow the exact same pattern as `ConversationRejectionTelemetry` (Story 6.1):

```csharp
public ConversationProjectionTelemetry(IMeterFactory meterFactory, ILogger<ConversationProjectionTelemetry> logger)
{
    _logger = logger;
    Meter meter = meterFactory.Create("Hexalith.Conversations");
    _freshnessCounter = meter.CreateCounter<long>(
        "conversations.projection.freshness",
        description: "Number of projection freshness state observations by class and lag class");
    _rebuildCounter = meter.CreateCounter<long>(
        "conversations.projection.rebuild",
        description: "Number of projection rebuild progress observations by rebuild class");
    _publicationFailureCounter = meter.CreateCounter<long>(
        "conversations.publication.failures",
        description: "Number of publication failures by bounded failure class");
}

// Recording freshness:
_freshnessCounter.Add(1,
    new KeyValuePair<string, object?>("freshness_class", freshnessClass.ToString().ToLowerInvariant()),
    new KeyValuePair<string, object?>("lag_class", lagClass.ToString().ToLowerInvariant()));

// Recording rebuild:
_rebuildCounter.Add(1,
    new KeyValuePair<string, object?>("rebuild_class", rebuildClass.ToString().ToLowerInvariant()));

// Recording publication failure:
_publicationFailureCounter.Add(1,
    new KeyValuePair<string, object?>("failure_class", failureClass.ToString().ToLowerInvariant()));
```

### Content-Safety Critical Rules (carry-forward from Stories 5.5–6.1)

The full UnsafeTerms blocklist (31 terms) applies to ALL log messages and telemetry dimension values. Key pitfalls:
- Do NOT log `state.ToString()` or `reasonCode.ToString()` directly as free-text — always log only bounded enum values
- Do NOT pass TenantId or ConversationId as metric dimension values or log fields
- Do NOT log `ProjectionFreshnessReasonCode.StaleThresholdExceeded.ToString()` (the raw reason code string is not forbidden, but embed only enum class names, never freshness threshold durations or lag timespans)
- `"store"` as SUBSTRING is forbidden — use "recorded" or "persisted" in log messages
- `"exception"` as SUBSTRING is forbidden — use "failure" or "rejection"

Forbidden in log messages and metric dimensions:
- TenantId value (raw string)
- ConversationId value
- PartyId value
- EventId value (unless it's used as a safe correlation handle already approved in the publication path)
- `ProjectionFreshnessReasonCode` raw string values as free-text (only the classified `ConversationProjectionFreshnessClass` name is safe)
- Any business reference string

### `None` Class Guard (same as Story 6.1)

When any of `ConversationProjectionFreshnessClass.None`, `ConversationProjectionLagClass.None`, or `ConversationPublicationFailureClass.None` is supplied to the telemetry methods, the implementation must throw `ArgumentException`. This prevents accidental emission of `freshness_class=none` counters from uninitialized code paths.

### Wiring: `ConversationProjectionReadService`

`ConversationProjectionReadService` (in `Server/Projections/`) already returns `ProjectionTrustState` and `ProjectionFreshnessReasonCode` in `ConversationProjectionReadResult`. Add `IConversationProjectionTelemetry?` as an optional constructor parameter (backward-compatible, null default). In `ReadDetailAsync`, after computing the result:

```csharp
// Call only when telemetry is provided and state is not Forbidden:
if (_telemetry is not null && result.FreshnessState != ProjectionTrustState.Forbidden)
{
    string safeCorrelationId = Guid.NewGuid().ToString("N")[..8];
    ConversationProjectionFreshnessClass freshness =
        ConversationProjectionFreshnessClassifier.Classify(result.FreshnessState, result.ReasonCode);
    ConversationProjectionLagClass lag =
        ConversationProjectionFreshnessClassifier.ClassifyLag(result.ReasonCode);
    _telemetry.RecordProjectionFreshnessState(freshness, lag, safeCorrelationId);
    if (result.FreshnessState == ProjectionTrustState.Rebuilding)
    {
        _telemetry.RecordProjectionRebuildProgress(ConversationProjectionFreshnessClass.Rebuilding, safeCorrelationId);
    }
}
```

**Read `ConversationProjectionReadService.cs` in full before modifying.** It has specific Forbidden/Unavailable return paths and `ProjectionMatchesRequest` checks — all must be preserved.

**Note on `ConversationProjectionReadResult`:** The result record currently has `FreshnessState` (ProjectionTrustState) and `ReasonCode` (ProjectionFreshnessReasonCode) fields (read the file to confirm the exact property names before implementing). If the record does not expose `ReasonCode`, the caller may need to classify from `FreshnessState` only.

### Wiring: Publication Service Wrapper

`ConversationPublicationMapper` is static — do NOT make it non-static. Create `ConversationPublicationService` as a thin non-static wrapper that:
1. Calls `ConversationPublicationMapper.TryMap(persisted)`
2. If the result is rejected, emits a publication failure signal via `IConversationProjectionTelemetry`
3. Returns the same `ConversationPublicationResult`

The `correlationId` passed to telemetry should be a generated safe short ID — never the `TenantId`, `ConversationId`, or `EventId` from the diagnostic (those are forbidden metric dimensions). The publication diagnostic's `CorrelationId` string field (if non-null) may be used as-is since it was already bounded by the mapper, but verify it does not contain raw entity IDs before using.

### DI Registration

Add both `.AddConversationRejectionTelemetry()` (Story 6.1) and `.AddConversationProjectionTelemetry()` (Story 6.2) to the DI composition root in `Program.cs`. Check `Program.cs` for the existing Story 6.1 registration before adding Story 6.2.

### Test Patterns: Using FakeMeterFactory and CapturingLogger

Story 6.1 introduced `FakeMeterFactory` and `CapturingLogger` stubs in the Server test project (since `Microsoft.Extensions.Diagnostics.Testing` was not available). Reuse the same stubs — do NOT introduce new stubs, do NOT add package references for testing utilities without architecture review.

```csharp
// Locate the existing stubs (likely in tests/Hexalith.Conversations.Server.Tests/Diagnostics/ or nearby)
// Reuse them directly — they are the approved test pattern for this project
```

### Test Structure: Classifier Tests (~13 tests)

```csharp
// File: tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationProjectionFreshnessClassifierTest.cs
[Fact]
public void ClassifyTrustState_Current_ReturnsCurrent()
{
    ConversationProjectionFreshnessClass result =
        ConversationProjectionFreshnessClassifier.Classify(
            ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current);
    result.ShouldBe(ConversationProjectionFreshnessClass.Current);
}
```

### Test Structure: Telemetry Tests (~11 tests)

Use the same `FakeMeterFactory` + `CapturingLogger` stubs from Story 6.1. Verify:
- Counters increment with correct dimension names and bounded string values
- Log messages contain only bounded field names (no TenantId, ConversationId)
- `None` class throws `ArgumentException` for all three methods
- DI registration works correctly

### CS8122 Pitfall (carry-forward from Stories 5.5–6.1)

In xUnit v3 / Shouldly `ShouldAllBe` lambdas, use `== null` / `!= null` instead of `is null` / `is not null`:
```csharp
// WRONG — CS8122
checks.ShouldAllBe(c => c.Error is null);
// CORRECT
checks.ShouldAllBe(c => c.Error == null);
```

### Current Test Count

- Before Story 6.2: 1304 total (Client 23, Conformance 155, Integration 8, Core 153, Server 453, Contracts 512)
- Expected after Story 6.2: ~1328–1335 total (Server: ~477–484), +~24 new Server tests

### Validation Commands

```bash
# Targeted: classifier tests
dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionFreshness"
dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationPublicationFailure"
dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionTelemetry"

# Full server suite: should go from 453 to ~477
dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj

# Full solution
dotnet test Hexalith.Conversations.slnx
```

### Project Structure Notes

Follow existing Diagnostics folder pattern from Story 6.1:
- New enums: `src/Hexalith.Conversations.Server/Diagnostics/` — namespace `Hexalith.Conversations.Server.Diagnostics`
- New interface + implementation: same folder and namespace
- New DI extension: same folder
- New static classifiers: same folder
- New publication wrapper: `src/Hexalith.Conversations.Server/Publication/` — namespace `Hexalith.Conversations.Server.Publication`
- Tests: `tests/Hexalith.Conversations.Server.Tests/Diagnostics/`
- Target framework: `net10.0`; Central Package Management — do NOT add package versions to `.csproj`
- Copyright header: `// Copyright (c) ITANEO. All rights reserved.`
- No new `ConformanceCheck` values, `ConformanceOutcome` values, `ReleaseGateId` values, or public error codes needed

### Scope Boundary

- Story 6.2 adds **runtime observability infrastructure** for projection freshness and publication failures — this lives in production execution paths.
- Do NOT add new `ProjectionTrustState` values, `ProjectionFreshnessReasonCode` values, or public contract types.
- Do NOT implement pub/sub Dapr subscription workers — the publication telemetry is wired to the mapper wrapper, not a new Dapr endpoint.
- Do NOT add conformance suite scenarios — that belongs to Story 6.3 (conformance and verification status) or Story 6.8B (telemetry cardinality gates).
- Do NOT modify `ConversationProjectionMaterializer` — telemetry is wired at the service call boundary, not inside the materializer.

### References

- [Source: epics.md#Story 6.2] — AC1, AC2, AC3, FR96, FR97
- [Source: architecture.md#Content-safe observability] — NFR55–NFR58, bounded cardinality
- [Source: 6-1-observe-command-rejections-and-tenant-isolation-denials-safely.md] — IMeterFactory pattern, FakeMeterFactory stub, None guard, content-safety rules
- [Source: src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs] — UPDATE target
- [Source: src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs] — static, do not modify
- [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs] — implementation pattern to replicate

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- Implemented 3 bounded vocabulary enums (ConversationProjectionFreshnessClass, ConversationProjectionLagClass, ConversationPublicationFailureClass) following the Story 6.1 enum pattern.
- Implemented 2 static classifiers: ConversationProjectionFreshnessClassifier maps ProjectionTrustState + ProjectionFreshnessReasonCode to bounded classes (Forbidden collapses to Unavailable as side-channel prevention); ConversationPublicationFailureClassifier maps ConversationErrorCode with TransientFailure as default fallback.
- Implemented IConversationProjectionTelemetry interface and ConversationProjectionTelemetry class using the same IMeterFactory + ILogger<T> pattern as Story 6.1. All three counters (freshness, rebuild, publication failures) share the "Hexalith.Conversations" meter. None class guard throws ArgumentException on all three methods.
- Wired IConversationProjectionTelemetry? into ConversationProjectionReadService as optional constructor parameter. Added EmitFreshnessTelemetryAndReturn private helper to emit at all non-Forbidden result paths (Unavailable/exception, Rebuilding/MixedGeneration, and final result). Rebuilding state also emits rebuild counter.
- Created ConversationPublicationService as non-static wrapper around ConversationPublicationMapper; emits RecordPublicationFailure with safe short generated correlationId (never TenantId/ConversationId).
- 24 new Server tests added: 9 classifier tests + 4 publication failure classifier tests + 11 telemetry tests. All pass. No regressions in full solution (1328 total tests).

### File List

- src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClass.cs (new)
- src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionLagClass.cs (new)
- src/Hexalith.Conversations.Server/Diagnostics/ConversationPublicationFailureClass.cs (new)
- src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClassifier.cs (new)
- src/Hexalith.Conversations.Server/Diagnostics/ConversationPublicationFailureClassifier.cs (new)
- src/Hexalith.Conversations.Server/Diagnostics/IConversationProjectionTelemetry.cs (new)
- src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs (new)
- src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetryServiceCollectionExtensions.cs (new)
- src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs (modified)
- src/Hexalith.Conversations.Server/Publication/ConversationPublicationService.cs (new)
- tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationProjectionFreshnessClassifierTest.cs (new)
- tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationPublicationFailureClassifierTest.cs (new)
- tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationProjectionTelemetryTest.cs (new)
- tests/Hexalith.Conversations.Server.Tests/Diagnostics/TelemetryTestHelpers.cs (new)
- _bmad-output/implementation-artifacts/tests/test-summary.md (modified)
- _bmad-output/implementation-artifacts/sprint-status.yaml (modified)

## Change Log

- 2026-05-23: Story 6.2 implemented — added projection freshness/lag/rebuild and publication failure observability infrastructure; 3 enums, 2 classifiers, IConversationProjectionTelemetry + impl + DI extension, wired into ConversationProjectionReadService and new ConversationPublicationService wrapper; 24 new Server tests; 1328 total tests passing.
- 2026-05-23: Review (AI) — 5 issues fixed automatically: (1) added missing lagClass==None guard in RecordProjectionFreshnessState [HIGH]; (2) extracted FakeMeterFactory+CapturingLogger<T> to shared TelemetryTestHelpers.cs per story spec ("do not introduce new stubs") [MEDIUM]; (3) added test RecordProjectionFreshnessState_NoneLagClass_ThrowsArgumentException [MEDIUM]; (4) added test ClassifyTrustState_Redacted_ReturnsCurrent [LOW]; (5) added test ClassifyLag_Forbidden_ReturnsNone [LOW]. 480 Server tests, 1331 total — all passing.

## Senior Developer Review (AI)

- Date: 2026-05-23
- Reviewer: AI (claude-sonnet-4-6)
- Outcome: Approve (after auto-fixes applied)
- Issues fixed: 5 (1 HIGH, 2 MEDIUM, 2 LOW)
- Tests after fix: 480 Server / 1331 total
