# Story 6.1: Observe Command Rejections and Tenant Isolation Denials Safely

Status: done

## Story

As an operator,
I want to observe command rejection counts, tenant isolation denials, and privileged access attempts by safe reason,
so that I can detect problems without exposing conversation content or protected tenant data.

## Acceptance Criteria

1. **AC1 — Command rejections emit bounded content-safe signals (FR95):** Given commands are rejected for validation, authorization, tenant binding, unsupported schema, idempotency conflict, stale projection, audit unavailable, or policy reasons, When observability signals are emitted, Then metrics, logs, traces, and dashboards classify rejection reason with bounded cardinality, And they exclude conversation content, conversation IDs where not approved, Party personal data, provider payloads, raw business identifiers, redacted content, and inaccessible tenant details.

2. **AC2 — Tenant isolation denials and privileged access emit bounded content-safe signals (FR98):** Given tenant isolation denials or privileged access attempts occur, When operator signals are inspected, Then signals identify safe reason class, operation class, retryability, correlation metadata, and escalation path, And they do not reveal target tenant, inaccessible Party, protected conversation existence, or cross-tenant business references.

3. **AC3 — Observability tests prove signal usefulness and content safety:** Given observability tests run, When rejection, denial, privileged access, cross-tenant guessing, malformed metadata, and redaction cases are exercised, Then tests prove signal usefulness, bounded labels, content-safe output, and no leakage through logs, traces, metrics, or diagnostics.

## Tasks / Subtasks

- [x] Task 1: Create bounded rejection class vocabulary (AC: #1)
  - [x] Create `ConversationCommandRejectionClass` enum in `src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClass.cs` with values: `None=0`, `Validation=1`, `TenantBinding=2`, `TenantIsolation=3`, `TenantProjectionUnavailable=4`, `Idempotency=5`, `AuditUnavailable=6`, `PolicyRejection=7`, `Infrastructure=8`
  - [x] Create `ConversationTenantDenialClass` enum in `src/Hexalith.Conversations.Server/Diagnostics/ConversationTenantDenialClass.cs` with values: `None=0`, `MissingContext=1`, `UnknownOrDisabled=2`, `InsufficientAccess=3`, `ProjectionUnavailable=4`, `ContextMismatch=5`
  - [x] Create `ConversationPrivilegedAccessClass` enum in `src/Hexalith.Conversations.Server/Diagnostics/ConversationPrivilegedAccessClass.cs` with values: `None=0`, `AuthorizedPrivilegedOperation=1`, `UnauthorizedPrivilegedAttempt=2`
  - [x] Add static helper `ConversationCommandRejectionClassifier` in `src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClassifier.cs` with `Classify(ConversationErrorCode code)` returning `ConversationCommandRejectionClass` and `Classify(ConversationTenantAccessDenialReason reason)` returning `ConversationTenantDenialClass`

- [x] Task 2: Define and implement `IConversationRejectionTelemetry` (AC: #1, #2)
  - [x] Create interface `IConversationRejectionTelemetry` in `src/Hexalith.Conversations.Server/Diagnostics/IConversationRejectionTelemetry.cs`
  - [x] Method: `void RecordCommandRejection(ConversationCommandRejectionClass rejectionClass, ConversationTenantAccessRequirement operationClass, bool isRetryable, string correlationId)`
  - [x] Method: `void RecordTenantDenial(ConversationTenantDenialClass denialClass, ConversationTenantAccessRequirement operationClass, bool isRetryable, string correlationId)`
  - [x] Method: `void RecordPrivilegedAccessAttempt(ConversationPrivilegedAccessClass accessClass, ConversationTenantAccessRequirement operationClass, string correlationId)`
  - [x] Create implementation `ConversationRejectionTelemetry` in `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs` using `System.Diagnostics.Metrics.Meter` for counters and `ILogger<ConversationRejectionTelemetry>` for structured logging
  - [x] Meter name: `"Hexalith.Conversations"` (matches ServiceDefaults registration scope)
  - [x] Counter name for command rejections: `"conversations.command.rejections"` with dimension `rejection_class` (bounded enum name, lowercase), dimension `operation_class` (bounded enum name, lowercase), dimension `retryable` (`"true"`/`"false"`)
  - [x] Counter name for tenant denials: `"conversations.tenant.denials"` with dimension `denial_class`, dimension `operation_class`, dimension `retryable`
  - [x] Counter name for privileged access: `"conversations.privileged.access"` with dimension `access_class`, dimension `operation_class`
  - [x] Log template (command rejection): `"ConversationCommandRejected: class={RejectionClass} operation={OperationClass} retryable={IsRetryable} corr={CorrelationId}"` — no TenantId, PartyId, ConversationId, or content fields
  - [x] Log template (tenant denial): `"ConversationTenantDenied: class={DenialClass} operation={OperationClass} retryable={IsRetryable} corr={CorrelationId}"` — same content-safety constraint
  - [x] Log template (privileged access): `"ConversationPrivilegedAccess: class={AccessClass} operation={OperationClass} corr={CorrelationId}"`
  - [x] Create `ConversationRejectionTelemetryServiceCollectionExtensions` in `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetryServiceCollectionExtensions.cs` with `AddConversationRejectionTelemetry(this IServiceCollection services)` registering `IConversationRejectionTelemetry` as singleton `ConversationRejectionTelemetry`

- [x] Task 3: Wire telemetry into existing access guard (AC: #1, #2)
  - [x] Inject `IConversationRejectionTelemetry` into `ConversationTenantAccessGuard` (file: `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`)
  - [x] Call `RecordTenantDenial` after each `ConversationTenantAccessDecision.Denied(...)` result using classifier helper to map `DenialReason` to `ConversationTenantDenialClass`
  - [x] Call `RecordCommandRejection` for tenant-binding related rejections (types `MissingTenant`, `MalformedTenant` map to `ConversationCommandRejectionClass.TenantBinding`)
  - [x] Inject `IConversationRejectionTelemetry` into `IdempotentConversationCommandExecutor` (file: `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs`)
  - [x] Call `RecordCommandRejection` with `Idempotency` class on idempotency conflict rejections
  - [x] Inject `IConversationRejectionTelemetry` into governance command handlers (`SetConversationRetentionPolicyCommandHandler`, `RedactMessageContentCommandHandler`, `MarkConversationContentSensitiveCommandHandler`)
  - [x] Call `RecordCommandRejection` with `AuditUnavailable` class when `ConversationGovernanceAuditStatus` indicates unavailable

- [x] Task 4: Tests for vocabulary, classifier, and telemetry (AC: #3)
  - [x] Create `ConversationCommandRejectionClassifierTest.cs` in `tests/Hexalith.Conversations.Server.Tests/Diagnostics/`
  - [x] Test: `ClassifyErrorCode_TenantBindingMissing_ReturnsTenantBinding`
  - [x] Test: `ClassifyErrorCode_TenantIsolationViolation_ReturnsTenantIsolation`
  - [x] Test: `ClassifyErrorCode_TenantProjectionStale_ReturnsTenantProjectionUnavailable`
  - [x] Test: `ClassifyErrorCode_CommandValidationFailed_ReturnsValidation`
  - [x] Test: `ClassifyErrorCode_SchemaVersionUnsupported_ReturnsValidation`
  - [x] Test: `ClassifyErrorCode_IdempotencyConflict_ReturnsIdempotency`
  - [x] Test: `ClassifyErrorCode_AuditSinkUnavailable_ReturnsAuditUnavailable`
  - [x] Test: `ClassifyDenialReason_MissingTenant_ReturnsMissingContext`
  - [x] Test: `ClassifyDenialReason_MalformedTenant_ReturnsMissingContext`
  - [x] Test: `ClassifyDenialReason_UnknownTenant_ReturnsUnknownOrDisabled`
  - [x] Test: `ClassifyDenialReason_TenantDisabled_ReturnsUnknownOrDisabled`
  - [x] Test: `ClassifyDenialReason_InsufficientRole_ReturnsInsufficientAccess`
  - [x] Test: `ClassifyDenialReason_TenantAccessUnavailable_ReturnsProjectionUnavailable`
  - [x] Test: `ClassifyDenialReason_TenantAccessStale_ReturnsProjectionUnavailable`
  - [x] Test: `ClassifyDenialReason_TenantMismatch_ReturnsContextMismatch`
  - [x] Create `ConversationRejectionTelemetryTest.cs` in `tests/Hexalith.Conversations.Server.Tests/Diagnostics/`
  - [x] Test: `RecordCommandRejection_EmitsCounterWithBoundedDimensions_NoConversationIdDimension`
  - [x] Test: `RecordCommandRejection_LogMessageContainsOnlyBoundedFields_NoTenantOrPartyIds`
  - [x] Test: `RecordTenantDenial_EmitsCounterWithBoundedDimensions_NoTargetTenantValue`
  - [x] Test: `RecordTenantDenial_LogMessageContainsOnlyBoundedFields_NoCrosstenantData`
  - [x] Test: `RecordPrivilegedAccessAttempt_EmitsCounterWithBoundedDimensions`
  - [x] Test: `RecordCommandRejection_NullOrEmptyCorrelationId_ThrowsArgumentException`
  - [x] Test: `RecordTenantDenial_NoneClass_DoesNotEmit` (guard: None class emits nothing to avoid noise)

- [x] Task 5: Update test summary (AC: none / bookkeeping)
  - [x] Add Story 6.1 section to `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Dev Notes

### Architecture: Epic 6 vs Epic 5

Epic 5 stories (5.5–5.11) added **conformance suite runners** (no production runtime changes). Story 6.1 is different: it adds **runtime observability infrastructure** that lives in production execution paths. The telemetry signals fire on actual command rejections and tenant denials during request processing.

This is the first Epic 6 story, which means it sets the observability dimension vocabulary that Stories 6.2 and 6.3 will extend. Design dimension names carefully — they must be stable and not require renaming in later stories.

### Bounded Rejection Class Mapping

Map `ConversationErrorCode` to `ConversationCommandRejectionClass` (no free-text dimensions):

| `ConversationErrorCode` value | `ConversationCommandRejectionClass` |
|---|---|
| `TenantBindingMissing` | `TenantBinding` |
| `TenantIsolationViolation` | `TenantIsolation` |
| `TenantProjectionStale` | `TenantProjectionUnavailable` |
| `TenantContextMismatch` | `TenantIsolation` |
| `CommandValidationFailed` | `Validation` |
| `SchemaVersionUnsupported` | `Validation` |
| `IdempotencyConflict` | `Idempotency` |
| `IdempotencyOutcomeUnknown` | `Idempotency` |
| `IdempotencyKeyMissing` | `Idempotency` |
| `AuditSinkUnavailable` | `AuditUnavailable` |
| `AuditPairingRequired` | `AuditUnavailable` |
| `AggregateNotFound` | `TenantIsolation` (public code for hidden/unauthorized) |
| `DuplicateParticipant` | `Validation` |
| `UnsupportedParticipant` | `Validation` |
| `ParticipantValidationUnavailable` | `Infrastructure` |
| `ProviderOnlyIdentityForbidden` | `Validation` |

### Bounded Denial Class Mapping

Map `ConversationTenantAccessDenialReason` to `ConversationTenantDenialClass`:

| `ConversationTenantAccessDenialReason` value(s) | `ConversationTenantDenialClass` |
|---|---|
| `MissingTenant`, `MalformedTenant`, `MissingCaller` | `MissingContext` |
| `UnknownTenant`, `TenantDisabled` | `UnknownOrDisabled` |
| `MissingMember`, `InsufficientRole`, `UnmappedRole`, `UnmappedStatus` | `InsufficientAccess` |
| `TenantAccessUnavailable`, `TenantAccessStale`, `TenantAccessGapDetected`, `TenantAccessRolledBack` | `ProjectionUnavailable` |
| `TenantMismatch`, `MalformedProjection`, `TenantProjectionPoisoned` | `ContextMismatch` |

### Operation Class Dimension Source

`ConversationTenantAccessRequirement` is already the closed-vocabulary operation class. Use `.ToString().ToLowerInvariant()` as the `operation_class` dimension value. Inspect `ConversationTenantAccessRequirement.cs` for its exact values before implementing — do not hard-code string values.

### Metrics Implementation Pattern (.NET 10)

```csharp
// In ConversationRejectionTelemetry constructor
_commandRejectionCounter = meter.CreateCounter<long>(
    "conversations.command.rejections",
    description: "Number of command rejections by bounded reason class");
_tenantDenialCounter = meter.CreateCounter<long>(
    "conversations.tenant.denials",
    description: "Number of tenant isolation denials by bounded denial class");
_privilegedAccessCounter = meter.CreateCounter<long>(
    "conversations.privileged.access",
    description: "Number of privileged access attempts by access class");

// Recording (all string dimensions come from enum names, not free text):
_commandRejectionCounter.Add(1,
    new KeyValuePair<string, object?>("rejection_class", rejectionClass.ToString().ToLowerInvariant()),
    new KeyValuePair<string, object?>("operation_class", operationClass.ToString().ToLowerInvariant()),
    new KeyValuePair<string, object?>("retryable", isRetryable ? "true" : "false"));
```

The `Meter` instance should be injected via `IMeterFactory` (registered by ServiceDefaults/AddOpenTelemetry) rather than `new Meter(...)`:

```csharp
public ConversationRejectionTelemetry(IMeterFactory meterFactory, ILogger<ConversationRejectionTelemetry> logger)
{
    _logger = logger;
    Meter meter = meterFactory.Create("Hexalith.Conversations");
    // create counters...
}
```

### Structured Logging Pattern (content-safe)

```csharp
// Correct — only bounded enum values, no TenantId, PartyId, ConversationId, or content
_logger.LogInformation(
    "ConversationCommandRejected: class={RejectionClass} operation={OperationClass} retryable={IsRetryable} corr={CorrelationId}",
    rejectionClass, operationClass, isRetryable, correlationId);

// WRONG — do not log TenantId, ConversationId, errorCode.Value, DenialReason.ToString() as free-text label
_logger.LogWarning("Denied tenant={TenantId} for op={Op}", tenantId, requirement); // FORBIDDEN
```

### Forbidden Metric Dimension Values

These must NEVER appear as metric dimension values or structured log fields (mirrors NFR57-58):
- `TenantId` value (raw tenant ID string)
- `PartyId` value
- `ConversationId` value
- `ConversationErrorCode.Value` as a free-text string dimension (use `ConversationCommandRejectionClass` instead)
- Any business reference string
- Raw error messages, exception messages, or `DenialReason.ToString()` values
- Provider correlation identifiers

The only allowed dimension values are names from closed-vocabulary enums (`ConversationCommandRejectionClass`, `ConversationTenantDenialClass`, `ConversationPrivilegedAccessClass`, `ConversationTenantAccessRequirement`).

### Content-Safety Critical Rules (carry-forward from Stories 5.5–5.11)

The full UnsafeTerms blocklist (31 terms) used in conformance tests also applies to log messages and telemetry string values:
`"other-tenant"`, `"redacted content"`, `"provider-a"`, `"EventStore"`, `"envelope"`, `"stream"`, `"snapshot"`, `"sequence"`, `"expected revision"`, `"checkpoint"`, `"SignalR"`, `"projection topology"`, `"handler"`, `"dispatcher"`, `"repository"`, `"store"`, `"aggregate identity"`, `"raw upstream"`, `"tenant:"`, `"tenant-"`, `"party:"`, `"party-"`, `"conv:"`, `"conversation-"`, `"provider-session"`, `"provider response"`, `"provider payload"`, `"business reference"`, `"case-"`, `"raw exception"`, `"exception"`, `"C:\\"`, `"D:\\"`.

Watch for:
- `"store"` as SUBSTRING — "data store", "EventStore" all blocked. Use "persisted" or "recorded".
- `"handler"` as SUBSTRING — do not use in free-text log messages; use "processor" or omit.
- `"tenant-"` with hyphen — forbidden. Use "tenant context" (no hyphen).
- `"exception"` as SUBSTRING — do not log exception messages; log safe reason class instead.
- `"conversation-"` with hyphen — forbidden in dimension values. `"conversations"` (plural, no hyphen after the s) is safe.

### CS8122 Pitfall (carry-forward from Stories 5.5–5.11)

In xUnit v3 / Shouldly `ShouldAllBe` lambdas, use `== null` / `!= null` instead of `is null` / `is not null`:
```csharp
// WRONG — CS8122
checks.ShouldAllBe(c => c.Error is null);
// CORRECT
checks.ShouldAllBe(c => c.Error == null);
```

### `None` Class Guard

When `ConversationCommandRejectionClass.None` or `ConversationTenantDenialClass.None` is supplied, the telemetry implementation must throw `ArgumentException` (signal that this means "no rejection" and should not be recorded). This prevents accidental emission of `rejection_class=none` counters from code paths that did not intend to signal anything. Test: `RecordTenantDenial_NoneClass_DoesNotEmit` should use `Should.Throw<ArgumentException>`.

### Wiring: Existing TenantAccess Guard

`ConversationTenantAccessGuard` (in `Server/TenantAccess/`) already calls `ConversationTenantAccessDecision.Denied(...)` and returns the decision. Add `IConversationRejectionTelemetry` constructor injection there. Call:
1. `RecordTenantDenial(classifier.Classify(decision.DenialReason), decision.Requirement, decision.IsRetryable, correlationId)` — after denial is returned
2. For `MissingTenant`/`MalformedTenant` denial reasons, also call `RecordCommandRejection(ConversationCommandRejectionClass.TenantBinding, decision.Requirement, decision.IsRetryable, correlationId)` because these appear as command rejections from the caller's view

**Read `ConversationTenantAccessGuard.cs` in full before modifying** — it is an UPDATE file. Understand its current call sites (guards, middleware, handler wiring) and what must be preserved.

### Wiring: Idempotency Command Executor

`IdempotentConversationCommandExecutor` in `Server/CommandHandlers/` handles the idempotency key check. After any rejection that maps to `IdempotencyConflict` or `IdempotencyKeyMissing`, call `RecordCommandRejection(ConversationCommandRejectionClass.Idempotency, ...)`.

**Read `IdempotentConversationCommandExecutor.cs` in full before modifying.**

### Wiring: Governance Command Handlers

`ConversationGovernanceAuditGate` (`Server/Governance/ConversationGovernanceAuditGate.cs`) is the chokepoint for audit-unavailable rejections. Add telemetry there rather than in each individual governance handler to avoid repeating the same injection in three places. **Read it in full before modifying.**

### DI Registration

Add `.AddConversationRejectionTelemetry()` call to the server startup in `Program.cs` (or wherever the DI composition root is for the Server project). Check `Program.cs` to see the existing registration pattern before adding.

### Test Structure: Classifier Tests (~15 tests)

```csharp
// File: tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationCommandRejectionClassifierTest.cs
// Pattern: one [Fact] per error code mapping + one per denial reason mapping
[Fact]
public void ClassifyErrorCode_TenantBindingMissing_ReturnsTenantBinding()
{
    ConversationCommandRejectionClass result = 
        ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.TenantBindingMissing);
    result.ShouldBe(ConversationCommandRejectionClass.TenantBinding);
}
```

### Test Structure: Telemetry Tests (~7 tests)

Use `NSubstitute` for `ILogger<ConversationRejectionTelemetry>`. Use `TestMeterFactory` (from `Microsoft.Extensions.Diagnostics.Testing` if available, or write a simple stub) to capture counter increments. Verify:
- Counters are incremented with correct dimension names and bounded string values
- Log messages contain only bounded field names (no TenantId, PartyId, or ConversationId as field values)
- `None` class throws `ArgumentException`

If `Microsoft.Extensions.Diagnostics.Testing` is not yet in `Directory.Packages.props`, do NOT add it — write a minimal `FakeMeterFactory` wrapper instead to avoid touching package management without architecture review.

### Current Test Count

- Before Story 6.1: 1279 (Client 23, Conformance 155, Integration 8, Core 153, Server 428, Contracts 512)
- After Story 6.1: ~1301 total (Server: ~450), +~22 new Server tests

### Validation Commands

```bash
# Targeted: new tests only
dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandRejection"
dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationRejection"

# Full server suite: should go from 428 to ~450
dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj

# Full solution
dotnet test Hexalith.Conversations.slnx
```

### Project Structure Notes

Follow existing Diagnostics folder patterns:
- New enums: `src/Hexalith.Conversations.Server/Diagnostics/` — same namespace `Hexalith.Conversations.Server.Diagnostics`
- New interface + impl: same folder
- New DI extension: same folder
- Tests: `tests/Hexalith.Conversations.Server.Tests/Diagnostics/`
- Target framework: `net10.0`; Central Package Management — do NOT add package versions to `.csproj`
- Copyright header: `// Copyright (c) ITANEO. All rights reserved.`
- No new `ConformanceCheck` values, `ConformanceOutcome` values, `ReleaseGateId` values, or public error codes needed

### Architecture Precedence

Story Safety Rule applies: this story introduces a **new privileged execution path** (telemetry from authorization code). Before implementing, verify:
- `IConversationRejectionTelemetry` injection does NOT add a synchronous call that can block or throw on the hot path (it must be fire-and-forget counter increments + `ILogger` calls)
- `ConversationRejectionTelemetry` does NOT call Tenants projection, Parties, or EventStore
- All dimension values are bounded to closed-vocabulary enum names — zero free-text dimensions

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- Implemented all 5 tasks. NSubstitute and Microsoft.Extensions.Diagnostics.Testing were not available; used hand-rolled FakeMeterFactory + CapturingLogger stubs in tests per Dev Notes guidance.
- ConversationTenantAccessGuard kept as static class (backward-compatible); telemetry wired via optional parameters (telemetry, correlationId) to avoid breaking existing 428 passing tests. Governance handlers (Set/Redact/Mark) inject IConversationRejectionTelemetry optionally and pass it through to the guard.
- IdempotentConversationCommandExecutor gained optional IConversationRejectionTelemetry? constructor param; renamed internal Rejection dispatch to RecordIdempotencyRejectionAndReturn to emit counter before returning.
- All 16 ConversationErrorCode values and all 17 ConversationTenantAccessDenialReason values are classified; 15 classifier tests + 8 telemetry tests added = 23 new Server tests. Full solution: 1302 tests, 0 failures.
- None guard enforced via ArgumentException in both RecordCommandRejection and RecordTenantDenial.

### File List

- `src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClass.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationTenantDenialClass.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationPrivilegedAccessClass.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClassifier.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/IConversationRejectionTelemetry.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs` (new)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetryServiceCollectionExtensions.cs` (new)
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs` (modified)
- `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs` (modified)
- `src/Hexalith.Conversations.Server/CommandHandlers/SetConversationRetentionPolicyCommandHandler.cs` (modified)
- `src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs` (modified)
- `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationCommandRejectionClassifierTest.cs` (new)
- `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationRejectionTelemetryTest.cs` (new)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)

## Senior Developer Review (AI)

Reviewer: claude-sonnet-4-6 on 2026-05-23

**Outcome: Approved with auto-fixes applied**

**Findings fixed:**

- **[MEDIUM] M1 — Inaccurate `operation_class` on idempotency rejections from governance commands**: `IdempotentConversationCommandExecutor.RecordIdempotencyRejectionAndReturn` hard-coded `ConversationTenantAccessRequirement.Write`. Added `operationRequirement` constructor parameter (default `Write`) so governance callers can supply `Governance`. All 451 pre-existing tests unaffected.
- **[MEDIUM] M2 — Missing test for `RecordCommandRejection(None, ...)` throwing `ArgumentException`**: Added `RecordCommandRejection_NoneClass_ThrowsArgumentException` to `ConversationRejectionTelemetryTest.cs`.
- **[MEDIUM] M3 — Missing test for `RecordPrivilegedAccessAttempt(None, ...)` throwing `ArgumentException`**: Added `RecordPrivilegedAccessAttempt_NoneClass_ThrowsArgumentException`.
- **[LOW] L1 — Misleading test name**: Renamed `RecordTenantDenial_NoneClass_DoesNotEmit` → `RecordTenantDenial_NoneClass_ThrowsArgumentException`.

**Post-fix validation:** 1304 total solution tests, 0 failures (Server: 453, +2 net new).

**AC verification:**
- AC1: All 16 ConversationErrorCode values map to bounded ConversationCommandRejectionClass; counters emit only closed-vocabulary enum names; no TenantId/PartyId/ConversationId dimensions. ✓
- AC2: All 17 ConversationTenantAccessDenialReason values map to bounded ConversationTenantDenialClass; telemetry fires in TenantAccessGuard on denial; MissingTenant/MalformedTenant also fire TenantBinding command rejection. ✓
- AC3: 25 tests (15 classifier + 10 telemetry including None-class guards) prove signal usefulness, bounded labels, content-safe output, and None-class enforcement. ✓

## Change Log

- 2026-05-23: Story 6.1 implemented — Added bounded rejection/denial/privileged-access vocabulary enums and classifier; implemented IConversationRejectionTelemetry with IMeterFactory-based counters and content-safe structured logs; wired into ConversationTenantAccessGuard, IdempotentConversationCommandExecutor, and three governance handlers; 23 new tests; 1302 total solution tests, 0 failures.
- 2026-05-23: Code review (AI, claude-sonnet-4-6) — Auto-fixed 4 issues: added `operationRequirement` param to IdempotentConversationCommandExecutor, added 2 None-class guard tests, renamed misleading test name; 1304 total tests, 0 failures. Status → done.
