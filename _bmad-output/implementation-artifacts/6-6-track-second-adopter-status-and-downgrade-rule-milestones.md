# Story 6.6: Track Second-Adopter Status and Downgrade-Rule Milestones

Status: done

## Story

As a product owner,
I want to track second-adopter status and downgrade-rule review milestones,
so that release commitments adjust deliberately as adoption broadens.

## Acceptance Criteria

1. **AC1 — Second-adopter status record captures all required governance fields and triggers (FR103):**
   Given adopter status changes,
   When a second adopter is identified, qualified, deferred, or disqualified,
   Then the product record updates second-adopter status, affected requirements, review owner, milestone date, and downgrade-rule review trigger,
   And status changes are auditable and content-safe.

2. **AC2 — Downgrade-rule review milestone identifies capabilities requiring review (FR103):**
   Given a downgrade-rule review milestone is reached,
   When product owners inspect lifecycle commitments,
   Then the system identifies which v1, v1.1, vNext, deferred, waived, or conditional capabilities require review,
   And it links to relevant conformance evidence, buyer acceptance records, and compatibility policy.

3. **AC3 — Lifecycle tracking conformance suite proves status traceability and correct milestone triggers (FR103):**
   Given lifecycle tracking tests run,
   When second adopter added, milestone overdue, status reverted, waiver expired, and capability review scenarios are exercised,
   Then tests prove status traceability, safe audit output, and correct milestone triggers.

## Tasks / Subtasks

- [x] Task 1: Create `SecondAdopterStatus` closed vocabulary (AC: #1, #2, #3)
  - [x] Create `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterVocabulary.cs`
  - [x] `SecondAdopterStatus` sealed record — 4 values:
    - `Identified` (`"identified"`)
    - `Qualified` (`"qualified"`)
    - `Deferred` (`"deferred"`)
    - `Disqualified` (`"disqualified"`)
  - [x] Each static property + `All` list + `Parse(string)` — follow `ConformanceVocabulary.cs` pattern exactly (sealed record, private ctor, `ValidateVocabularyValue`, `Known()`, `ParseKnown()`)
  - [x] Add `[JsonConverter(typeof(SecondAdopterStatusJsonConverter))]` on `SecondAdopterStatus`
  - [x] Add `SecondAdopterStatusJsonConverter` to `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` — inherit from `ConversationStringValueJsonConverter<T>`, implement `Create` and `GetValue`

- [x] Task 2: Create `SecondAdopterStatusEntryV1` record and `SecondAdopterStatusValidator` (AC: #1, #2, #3)
  - [x] Create `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterStatusEntryV1.cs`
  - [x] `SecondAdopterStatusEntryV1` positional record parameters (12 params, in order):
    - `EntryId string` — `ConformanceContractValidation.RequiredSafeToken`
    - `Status SecondAdopterStatus` — `ArgumentNullException.ThrowIfNull`
    - `AffectedRequirementsRef string` — `ConformanceContractValidation.RequiredSafeToken`
    - `ReviewOwner string` — `ConformanceContractValidation.RequiredSafeToken`
    - `MilestoneDateUtc DateTimeOffset` — `ConformanceContractValidation.RequiredUtcTimestamp`
    - `DowngradeRuleTriggered bool` — no validation needed
    - `CapabilityRef string` — `ConformanceContractValidation.RequiredSafeToken`
    - `WaiverRef string?` — `ConformanceContractValidation.OptionalSafeToken`
    - `WaiverExpiryDateUtc DateTimeOffset?` — nullable; when present, validate as UTC via `RequiredUtcTimestamp`
    - `StatusChangeRationaleRef string?` — `ConformanceContractValidation.OptionalSafeToken`
    - `ConformanceArtifactRef string?` — `ConformanceContractValidation.OptionalSafeToken`
    - `ReviewDateUtc DateTimeOffset` — `ConformanceContractValidation.RequiredUtcTimestamp`
  - [x] `SecondAdopterStatusValidator` static class in same file:
    - Method: `ValidateEntry(SecondAdopterStatusEntryV1 entry, DateTimeOffset evaluatedAt) → IReadOnlyList<string>`
    - [x] Error token `"milestone-overdue"`: when `entry.MilestoneDateUtc < evaluatedAt`
    - [x] Error token `"review-overdue"`: when `entry.ReviewDateUtc < evaluatedAt`
    - [x] Error token `"qualified-no-downgrade-trigger"`: when `entry.Status.Equals(SecondAdopterStatus.Qualified) && !entry.DowngradeRuleTriggered`
    - [x] Error token `"waiver-expired"`: when `entry.WaiverRef is not null && entry.WaiverExpiryDateUtc.HasValue && entry.WaiverExpiryDateUtc.Value < evaluatedAt`
    - [x] Error token `"reverted-missing-rationale"`: when `entry.Status.Equals(SecondAdopterStatus.Disqualified) && entry.StatusChangeRationaleRef is null`
    - [x] Return empty list when entry is valid

- [x] Task 3: Contracts vocabulary and validator tests (AC: #1, #2, #3)
  - [x] Create `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterVocabularyTest.cs`
    - [x] Test: `SecondAdopterStatus_AllContains4Values`
    - [x] Test: `SecondAdopterStatus_Parse_Identified_ReturnsIdentified`
    - [x] Test: `SecondAdopterStatus_Parse_Qualified_ReturnsQualified`
    - [x] Test: `SecondAdopterStatus_Parse_Deferred_ReturnsDeferred`
    - [x] Test: `SecondAdopterStatus_Parse_Disqualified_ReturnsDisqualified`
    - [x] Test: `SecondAdopterStatus_Parse_UnknownValue_ThrowsArgumentException`
    - [x] Test: `SecondAdopterStatus_SerializesAndDeserializesToCorrectValue` (round-trip `JsonSerializer.Serialize/Deserialize` for each of the 4 values)
    - [x] Test: `SecondAdopterStatus_Disqualified_WireValueIsDisqualified`
  - [x] Create `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterStatusValidatorTest.cs`
    - [x] Test: `ValidateEntry_Identified_FutureMilestone_ReturnsNoErrors`
    - [x] Test: `ValidateEntry_Qualified_WithTrigger_ReturnsNoErrors`
    - [x] Test: `ValidateEntry_Deferred_WithValidWaiver_ReturnsNoErrors`
    - [x] Test: `ValidateEntry_Disqualified_WithRationale_ReturnsNoErrors`
    - [x] Test: `ValidateEntry_MilestoneOverdue_ReturnsMilestoneOverdue`
    - [x] Test: `ValidateEntry_ReviewOverdue_ReturnsReviewOverdue`
    - [x] Test: `ValidateEntry_Qualified_NoTrigger_ReturnsQualifiedNoDowngradeTrigger`
    - [x] Test: `ValidateEntry_WaiverExpired_ReturnsWaiverExpired`
    - [x] Test: `ValidateEntry_Disqualified_NoRationale_ReturnsRevertedMissingRationale`
    - [x] Test: `ValidateEntry_Deferred_NoWaiverRef_DoesNotTriggerWaiverExpired` (when `WaiverRef is null`, `waiver-expired` must NOT fire even with past `WaiverExpiryDateUtc`)
  - [x] Update `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` — add `SecondAdopterStatus` (all 4 values) and `SecondAdopterStatusEntryV1` (one instance) to `AllContracts` (prevents regression in AllContracts serialization coverage test, as in Stories 6.4 and 6.5)

- [x] Task 4: Add second-adopter conformance suite fixture and runner (AC: #3)
  - [x] Create `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceFixtures.cs`
  - [x] `SecondAdopterScenarioData` sealed record parameters:
    - [x] `ScenarioId string` — bounded safe scenario identifier
    - [x] `Entry SecondAdopterStatusEntryV1` — the entry under test
    - [x] `ExpectedValidationErrors IReadOnlyList<string>` — empty = should pass; non-empty = validation must return these error tokens
    - [x] `SafeMessage string` — content-safe scenario description
  - [x] `SecondAdopterConformanceSeedData` static class with `SyntheticDataMarker = "synthetic-conformance-data"` and `Scenarios` property with exactly 10 deterministic records:
    - [x] `adopter-identified-baseline`: Status=Identified, DowngradeTriggered=false, FutureMilestone, FutureReview, no WaiverRef, no RationaleRef → no errors
    - [x] `adopter-qualified-trigger-set`: Status=Qualified, DowngradeTriggered=true, FutureMilestone, FutureReview → no errors
    - [x] `adopter-deferred-waiver-valid`: Status=Deferred, FutureMilestone, FutureReview, WaiverRef=`"deferred-scope-waiver"`, FutureWaiverExpiry → no errors
    - [x] `adopter-disqualified-rationale`: Status=Disqualified, FutureMilestone, FutureReview, StatusChangeRationaleRef=`"revert-rationale-001"` → no errors
    - [x] `adopter-qualified-capability-link`: Status=Qualified, DowngradeTriggered=true, FutureMilestone, FutureReview, ConformanceArtifactRef=`"capability-review-artifact"` → no errors
    - [x] `adopter-milestone-overdue`: Status=Identified, PastMilestone, FutureReview → `["milestone-overdue"]`
    - [x] `adopter-review-overdue`: Status=Qualified, DowngradeTriggered=true, FutureMilestone, PastReview → `["review-overdue"]`
    - [x] `adopter-qualified-no-trigger`: Status=Qualified, DowngradeTriggered=false, FutureMilestone, FutureReview → `["qualified-no-downgrade-trigger"]`
    - [x] `adopter-deferred-waiver-expired`: Status=Deferred, FutureMilestone, FutureReview, WaiverRef=`"expired-waiver-ref"`, PastWaiverExpiry → `["waiver-expired"]`
    - [x] `adopter-reverted-no-rationale`: Status=Disqualified, FutureMilestone, FutureReview, StatusChangeRationaleRef=null → `["reverted-missing-rationale"]`
  - [x] Fixed timestamps:
    - [x] `FutureMilestone` = `new DateTimeOffset(2027, 6, 1, 0, 0, 0, TimeSpan.Zero)`
    - [x] `PastMilestone` = `new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)`
    - [x] `FutureReviewDate` = `new DateTimeOffset(2027, 9, 1, 0, 0, 0, TimeSpan.Zero)`
    - [x] `PastReviewDate` = `new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)`
    - [x] `FutureWaiverExpiry` = `new DateTimeOffset(2027, 12, 1, 0, 0, 0, TimeSpan.Zero)`
    - [x] `PastWaiverExpiry` = `new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)`
  - [x] All scenarios use: `reviewOwner="release-engineer"`, `affectedRequirementsRef="FR103"`, `capabilityRef="second-adopter-capability"`
  - [x] Scenario IDs and token fields must use safe tokens (no "exception", "store", "tenant", "unknown" as freestanding substring)

  - [x] Create `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuite.cs`
  - [x] Suite class name: `SecondAdopterConformanceSuite`
  - [x] Documentation URI: `new Uri("https://docs.hexalith.local/conversations/compliance/v1/second-adopter")`
  - [x] `Run(IReadOnlyList<SecondAdopterScenarioData> scenarios, string correlationId, DateTimeOffset evaluatedAt) → ConformanceRunResultV1`
  - [x] Guard: `ArgumentNullException.ThrowIfNull(scenarios)`, `ArgumentException.ThrowIfNullOrWhiteSpace(correlationId)`, throw `ArgumentException` when `scenarios.Count == 0`
  - [x] For each scenario, call `SecondAdopterStatusValidator.ValidateEntry(scenario.Entry, evaluatedAt)` to get actual errors
  - [x] Conformance logic (same pattern as `ReleaseScopeConformanceSuite`):
    ```csharp
    bool isConformant = scenario.ExpectedValidationErrors.Count == 0
        ? actualErrors.Count == 0
        : scenario.ExpectedValidationErrors.All(e => actualErrors.Contains(e, StringComparer.Ordinal));
    ```
  - [x] When conformant: outcome=Ready, classification=Conformant, error=null, remediationCode=`"none"`
  - [x] When non-conformant: outcome=Blocked, classification=ProductInvariant, error=`ConversationErrorCatalog.CreateError(ConversationErrorCode.CommandValidationFailed, checkCorrelationId)`, remediationCode=`"fail-closed"`
  - [x] `ConformanceCheckResultV1` constructor call: `(SchemaVersion.Current, ConformanceCheck.GovernancePrecondition, scenario.ScenarioId, checkOutcome, checkClassification, ["FR103"], ["second-adopter-precondition"], ["second-adopter"], safeMessage, remediationCode, Documentation, checkCorrelationId, error)`
  - [x] Aggregation: `anyFailure = results.Any(r => r.FailureClassification.IsFailure)`, `anyDegraded = results.Any(r => r.Outcome.Equals(ConformanceOutcome.Degraded))`
  - [x] Suite ID: `"second-adopter-suite"`, Runner ID: `"local-ci-runner"`
  - [x] Safe summary: fail → `"One or more second-adopter lifecycle scenarios did not pass validation."` / pass → `"All second-adopter lifecycle scenarios conform to expected validator behaviour."`
  - [x] Correlation ID prefix for checks: `"corr-sa-"` + `scenario.ScenarioId`

- [x] Task 5: Second-adopter conformance suite tests (AC: #3)
  - [x] Create `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuiteTest.cs`
  - [x] Fixed `evaluatedAt` for all tests: `new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero)` (PastMilestone/PastReview/PastWaiverExpiry = Jan 2026 is expired; future values are future-valid)
  - [x] Fixed `correlationId` for all tests: `"corr-second-adopter-test"`
  - [x] Test: `RunResultShouldHaveExactly10Checks`
  - [x] Test: `AllChecksShouldUseGovernancePreconditionCheckId`
  - [x] Test: `AllPassScenariosShouldProduceReadyOutcome` (scenarios 1–5: adopter-identified-baseline, adopter-qualified-trigger-set, adopter-deferred-waiver-valid, adopter-disqualified-rationale, adopter-qualified-capability-link)
  - [x] Test: `AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails` (overall outcome = Ready when validator correctly flags; all 10 are conformant)
  - [x] Test: `AllChecksShouldBeClassifiedAsConformant`
  - [x] Test: `AllChecksShouldCarryFR103RequirementAndSecondAdopterMappings` (check RequirementMappings contains "FR103", PreconditionMappings not empty, ReleaseGateMappings contains "second-adopter")
  - [x] Test: `PassScenariosShouldHaveNullTypedError` (Ready outcome checks must not carry a typed error)
  - [x] Test: `SuiteIdAndRunnerIdShouldMatchSpecifiedValues` ("second-adopter-suite" and "local-ci-runner")
  - [x] Test: `RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` (serialize run result, check against coreFixture.PoisonSentinelValues + standard forbidden fragments list from Story 6.4/6.5 pattern)
  - [x] Test: `RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` (check `"suiteId":"second-adopter-suite"`, `"overallOutcome":"ready"`, `"overallClassification":"conformant"`, deserialize and compare)
  - [x] Test: `NullScenariosListShouldThrow`
  - [x] Test: `EmptyScenariosListShouldThrow`
  - [x] Test: `NullCorrelationIdShouldThrow`
  - [x] Test: `MilestoneOverdueShouldProduceConformantResult` — verifies `adopter-milestone-overdue` scenario is conformant (validator correctly flags it → Ready)
  - [x] Test: `RevertedNoRationaleShouldProduceConformantResult` — verifies `adopter-reverted-no-rationale` scenario is conformant

- [x] Task 6: Update conformance manifest and test summary (AC: none / bookkeeping)
  - [x] Add Story 6.6 entry to `docs/release-evidence/conformance-manifest-v1-fixture.json`:
    - [x] `testId`: `"story-6-6-second-adopter-status"`
    - [x] `testName`: `"Second-adopter status tracking and downgrade-rule review milestone validation"`
    - [x] `requirementId`: `"FR103"`
    - [x] `carryForwardCommitmentRef`: null
    - [x] `releaseGateId`: null
    - [x] `passCriteria`: `"All 10 second-adopter lifecycle scenarios produce expected outcomes and run result serializes to content-safe camelCase JSON"`
    - [x] `releaseDecisionStatus`: `"pass"`
    - [x] `waiverReference`: null
    - [x] `measurementMethod`: `"automated-conformance-suite-test"`
    - [x] `environment`: `"local-ci"`
    - [x] `evidenceArtifactHandle`: `"second-adopter-suite-result"`
    - [x] `owner`: `"release-engineer"`
    - [x] `lifecycleStage`: `"release-evidence"`
    - [x] `registeredAtUtc`: `"2026-05-23T00:00:00+00:00"`
  - [x] Add Story 6.6 section to `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Dev Notes

### Architecture: Contracts-Layer Vocabulary + Conformance Suite Pattern (Same as Stories 5.3–6.5)

Story 6.6 follows the **Contracts vocabulary + Conformance suite** pattern from Stories 5.3, 5.4, 5.11, 6.4, and 6.5 — NOT the Server/Diagnostics/telemetry pattern of Stories 6.1–6.3.

**Layer breakdown:**
- `Contracts` (`Hexalith.Conversations.Contracts`): new `SecondAdopterStatus` vocabulary + `SecondAdopterStatusEntryV1` record + `SecondAdopterStatusValidator`
- `Contracts.Tests` (`Hexalith.Conversations.Contracts.Tests`): vocabulary tests + validator tests + `ContractSamples.cs` update
- `Conformance.Tests` (`Hexalith.Conversations.Conformance.Tests`): fixtures + suite runner + suite tests

**No changes to:** Server, AppHost, Aspire, DI registrations, telemetry counters, `IMeterFactory`, `ILogger`, `Program.cs`.

### Vocabulary Pattern — MUST follow exactly

`SecondAdopterStatus` follows `ConformanceVocabulary.cs` (sealed record, private ctor, `ValidateVocabularyValue`, `Known()`, `ParseKnown()`):

```csharp
// File: src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterVocabulary.cs
using System.Text.Json.Serialization;
using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

[JsonConverter(typeof(SecondAdopterStatusJsonConverter))]
public sealed record SecondAdopterStatus
{
    public static SecondAdopterStatus Identified { get; } = new("identified");
    public static SecondAdopterStatus Qualified { get; } = new("qualified");
    public static SecondAdopterStatus Deferred { get; } = new("deferred");
    public static SecondAdopterStatus Disqualified { get; } = new("disqualified");

    private static readonly IReadOnlyDictionary<string, SecondAdopterStatus> KnownValues = Known(
        Identified, Qualified, Deferred, Disqualified);

    private SecondAdopterStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static IReadOnlyList<SecondAdopterStatus> All { get; } =
    [
        Identified, Qualified, Deferred, Disqualified,
    ];

    public static SecondAdopterStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(SecondAdopterStatus));

    public override string ToString() => Value;
}
```

### JSON Serializer Registration — MUST add converter

Add `SecondAdopterStatusJsonConverter` to `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` at the end of the file, following the existing pattern exactly:

```csharp
internal sealed class SecondAdopterStatusJsonConverter :
    ConversationStringValueJsonConverter<SecondAdopterStatus>
{
    protected override SecondAdopterStatus Create(string value) => SecondAdopterStatus.Parse(value);
    protected override string GetValue(SecondAdopterStatus value) => value.Value;
}
```

### `SecondAdopterStatusEntryV1` Constructor Pattern

Follow `CapabilityReleaseScopeEntryV1.cs` and `BuyerPartialAcceptanceItemV1.cs` positional record patterns:

```csharp
// File: src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterStatusEntryV1.cs
namespace Hexalith.Conversations.Contracts.Conformance;

public sealed record SecondAdopterStatusEntryV1(
    string EntryId,
    SecondAdopterStatus Status,
    string AffectedRequirementsRef,
    string ReviewOwner,
    DateTimeOffset MilestoneDateUtc,
    bool DowngradeRuleTriggered,
    string CapabilityRef,
    string? WaiverRef,
    DateTimeOffset? WaiverExpiryDateUtc,
    string? StatusChangeRationaleRef,
    string? ConformanceArtifactRef,
    DateTimeOffset ReviewDateUtc)
{
    public string EntryId { get; } = ConformanceContractValidation.RequiredSafeToken(EntryId, nameof(EntryId));
    public SecondAdopterStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));
    public string AffectedRequirementsRef { get; } = ConformanceContractValidation.RequiredSafeToken(AffectedRequirementsRef, nameof(AffectedRequirementsRef));
    public string ReviewOwner { get; } = ConformanceContractValidation.RequiredSafeToken(ReviewOwner, nameof(ReviewOwner));
    public DateTimeOffset MilestoneDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(MilestoneDateUtc, nameof(MilestoneDateUtc));
    public bool DowngradeRuleTriggered { get; } = DowngradeRuleTriggered;
    public string CapabilityRef { get; } = ConformanceContractValidation.RequiredSafeToken(CapabilityRef, nameof(CapabilityRef));
    public string? WaiverRef { get; } = ConformanceContractValidation.OptionalSafeToken(WaiverRef, nameof(WaiverRef));
    public DateTimeOffset? WaiverExpiryDateUtc { get; } = WaiverExpiryDateUtc.HasValue
        ? ConformanceContractValidation.RequiredUtcTimestamp(WaiverExpiryDateUtc.Value, nameof(WaiverExpiryDateUtc))
        : (DateTimeOffset?)null;
    public string? StatusChangeRationaleRef { get; } = ConformanceContractValidation.OptionalSafeToken(StatusChangeRationaleRef, nameof(StatusChangeRationaleRef));
    public string? ConformanceArtifactRef { get; } = ConformanceContractValidation.OptionalSafeToken(ConformanceArtifactRef, nameof(ConformanceArtifactRef));
    public DateTimeOffset ReviewDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ReviewDateUtc, nameof(ReviewDateUtc));
}
```

Note: The nullable `DateTimeOffset? WaiverExpiryDateUtc` pattern exactly mirrors `DateTimeOffset? ConditionalExpiry` in `CapabilityReleaseScopeEntryV1.cs:61–63`.

### Validator Pattern

Follow `CapabilityReleaseScopeValidator` and `BuyerPartialAcceptanceItemValidator` patterns:

```csharp
public static class SecondAdopterStatusValidator
{
    public static IReadOnlyList<string> ValidateEntry(SecondAdopterStatusEntryV1 entry, DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(entry);

        List<string> errors = [];

        if (entry.MilestoneDateUtc < evaluatedAt)
            errors.Add("milestone-overdue");

        if (entry.ReviewDateUtc < evaluatedAt)
            errors.Add("review-overdue");

        if (entry.Status.Equals(SecondAdopterStatus.Qualified) && !entry.DowngradeRuleTriggered)
            errors.Add("qualified-no-downgrade-trigger");

        if (entry.WaiverRef is not null &&
            entry.WaiverExpiryDateUtc.HasValue &&
            entry.WaiverExpiryDateUtc.Value < evaluatedAt)
            errors.Add("waiver-expired");

        if (entry.Status.Equals(SecondAdopterStatus.Disqualified) && entry.StatusChangeRationaleRef is null)
            errors.Add("reverted-missing-rationale");

        return errors;
    }
}
```

**Critical exclusions:**
- `Deferred` status with `WaiverRef is null` does NOT trigger `"waiver-expired"` (guard: `entry.WaiverRef is not null`).
- `Identified` status does NOT require `DowngradeRuleTriggered = true`.

### Conformance Suite Runner Pattern

Identical to `ReleaseScopeConformanceSuite.cs` — call `SecondAdopterStatusValidator.ValidateEntry(scenario.Entry, evaluatedAt)` per scenario; conformance logic:

```csharp
bool isConformant = scenario.ExpectedValidationErrors.Count == 0
    ? actualErrors.Count == 0
    : scenario.ExpectedValidationErrors.All(e => actualErrors.Contains(e, StringComparer.Ordinal));
```

All 10 scenarios are conformant with a correct implementation because the validator correctly flags or accepts each one. Overall outcome = Ready; classification = Conformant.

### Fixture Scenario Construction

Use deterministic `DateTimeOffset` values — no `DateTimeOffset.UtcNow`:
- `FutureMilestone = new DateTimeOffset(2027, 6, 1, 0, 0, 0, TimeSpan.Zero)`
- `PastMilestone = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)` (expired as of evaluatedAt 2026-05-23)
- `FutureReviewDate = new DateTimeOffset(2027, 9, 1, 0, 0, 0, TimeSpan.Zero)`
- `PastReviewDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)` (review-due as of evaluatedAt 2026-05-23)
- `FutureWaiverExpiry = new DateTimeOffset(2027, 12, 1, 0, 0, 0, TimeSpan.Zero)`
- `PastWaiverExpiry = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)` (expired as of evaluatedAt 2026-05-23)

Fixed `evaluatedAt` in all suite tests: `new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero)`.

Pass scenarios must use `FutureMilestone` and `FutureReviewDate`.

### Content Safety: Scenario IDs and SafeMessage Strings

Scenario IDs flow through `ConformanceCheckResultV1` constructor which runs `ConversationError.EnsureContentSafe()`.

**Approved scenario IDs (pre-verified as safe machine tokens):**
- `"adopter-identified-baseline"`, `"adopter-qualified-trigger-set"`, `"adopter-deferred-waiver-valid"`, `"adopter-disqualified-rationale"`, `"adopter-qualified-capability-link"`, `"adopter-milestone-overdue"`, `"adopter-review-overdue"`, `"adopter-qualified-no-trigger"`, `"adopter-deferred-waiver-expired"`, `"adopter-reverted-no-rationale"`

**DO NOT** use tokens containing: `"unknown"` (freestanding substring), `"tenant"`, `"store"`, `"exception"` in scenario IDs.

**SafeMessage strings must NOT contain:** `"sequence"`, `"unknown"`, `"exception"` as freestanding terms. Use neutral phrasing such as "second adopter status" or "lifecycle milestone" instead.

**Precondition/gate mappings are safe:** `"second-adopter-precondition"` and `"second-adopter"` go through `ConformanceContractValidation.RequiredMappingToken` which does NOT run the content blocklist — per Story 4.4 lesson.

### CS8122 Pitfall (carry-forward from Stories 5.5–6.5)

In xUnit v3 / Shouldly `ShouldAllBe` lambdas use `== null` / `!= null` not `is null` / `is not null`:
```csharp
// WRONG — CS8122
checks.ShouldAllBe(c => c.Error is null);
// CORRECT
checks.ShouldAllBe(c => c.Error == null);
```

### ContractSamples.cs Regression Prevention

From Stories 6.4 and 6.5 lessons: `ContractSamples.AllContracts` requires samples for every new type with `[JsonConverter]` or complex construction. Add after the `BuyerAcceptanceItemStatus` / `BuyerPartialAcceptanceItemV1` samples (lines ~1461–1480):

```csharp
SecondAdopterStatus.Identified,
SecondAdopterStatus.Qualified,
SecondAdopterStatus.Deferred,
SecondAdopterStatus.Disqualified,
new SecondAdopterStatusEntryV1(
    "entry-sample-001",
    SecondAdopterStatus.Qualified,
    "FR103",
    "release-engineer",
    new DateTimeOffset(2027, 6, 1, 0, 0, 0, TimeSpan.Zero),
    true,
    "second-adopter-capability",
    null,
    null,
    null,
    null,
    new DateTimeOffset(2027, 9, 1, 0, 0, 0, TimeSpan.Zero)),
```

Without this, the existing `AllContracts` serialization coverage test will fail.

### Suite Test Pattern — Test Names for Fail Scenarios

Following Stories 6.4/6.5 patterns for scenario-specific conformance tests:
- `MilestoneOverdueShouldProduceConformantResult` — checks `adopter-milestone-overdue` → Ready (validator correctly flagged it)
- `RevertedNoRationaleShouldProduceConformantResult` — checks `adopter-reverted-no-rationale` → Ready

The pattern: conformant = validator correctly handles the scenario (both correctly-passing and correctly-flagging-error cases).

### Project Structure Notes

- New vocabulary file: `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterVocabulary.cs` — namespace `Hexalith.Conversations.Contracts.Conformance`
- New record + validator file: `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterStatusEntryV1.cs` — same namespace
- Updated serialization: `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` — append one converter at the end
- New Contracts tests (vocabulary): `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterVocabularyTest.cs` — namespace `Hexalith.Conversations.Contracts.Tests.Conformance`
- New Contracts tests (validator): `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterStatusValidatorTest.cs` — same namespace
- Updated: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- New Conformance.Tests fixtures: `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceFixtures.cs` — namespace `Hexalith.Conversations.Conformance.Tests`
- New Conformance.Tests suite runner: `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuite.cs` — same namespace
- New Conformance.Tests suite test: `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuiteTest.cs` — same namespace
- Target framework: `net10.0`; Central Package Management — do NOT add package versions to `.csproj`
- Copyright header: `// Copyright (c) ITANEO. All rights reserved.`

### Scope Boundary

- Do NOT add telemetry counters, `ILogger`, or `IMeterFactory`
- Do NOT add DI registration in `Program.cs`
- Do NOT add `None=0` enum value — sealed records don't use the enum pattern
- Do NOT modify `ConformanceRunResultV1`, `ConformanceCheckResultV1`, or other existing Contracts types
- Do NOT add new `ConformanceCheck` values or `ReleaseGateId` values — use `ConformanceCheck.GovernancePrecondition` as in Stories 6.4 and 6.5
- Do NOT create a new projection, aggregate, or database table
- Do NOT touch Server, AppHost, or Aspire projects

### Current Test Count

- After Story 6.5: 1438 total (Client 23, Conformance 201, Integration 8, Core 153, Server 503, Contracts ~550)
- New tests for Story 6.6:
  - Contracts.Tests: ~18 new (8 vocabulary + 10 validator)
  - Conformance.Tests: ~15 new (suite tests)
- Expected after Story 6.6: ~1471 total

### Validation Commands

```bash
# Targeted: vocabulary tests
dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~SecondAdopter"

# Targeted: validator tests
dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~SecondAdopterStatusValidator"

# Targeted: conformance suite tests
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~SecondAdopterConformance"

# Full Contracts suite: should go from ~550 to ~568
dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj

# Full Conformance suite: should go from 201 to ~216
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj

# Full solution
dotnet test Hexalith.Conversations.slnx
```

### References

- [Source: epics.md#Story 6.6] — AC1, AC2, AC3, FR103
- [Source: epics.md#FR103] — Product can track second-adopter status and trigger downgrade-rule review milestones
- [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs] — sealed record vocabulary pattern to replicate
- [Source: src/Hexalith.Conversations.Contracts/Conformance/CapabilityReleaseScopeEntryV1.cs] — positional record + nullable DateTimeOffset pattern (`ConditionalExpiry`)
- [Source: src/Hexalith.Conversations.Contracts/Conformance/BuyerPartialAcceptanceItemV1.cs] — most recent positional record + validator pattern
- [Source: src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs] — converter pattern to append
- [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs] — RequiredSafeToken, OptionalSafeToken, RequiredUtcTimestamp methods
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ReleaseScopeConformanceSuite.cs] — suite runner structural pattern (follow exactly)
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ReleaseScopeConformanceFixtures.cs] — fixture sealed record + seed data pattern
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ReleaseScopeConformanceSuiteTest.cs] — suite test pattern
- [Source: tests/Hexalith.Conversations.Conformance.Tests/BuyerAcceptanceConformanceSuite.cs] — most recent suite runner (Story 6.5)
- [Source: tests/Hexalith.Conversations.Conformance.Tests/BuyerAcceptanceConformanceFixtures.cs] — most recent fixtures (Story 6.5)
- [Source: tests/Hexalith.Conversations.Conformance.Tests/BuyerAcceptanceConformanceSuiteTest.cs] — most recent suite test (Story 6.5)
- [Source: docs/release-evidence/conformance-manifest-v1-fixture.json] — manifest entry format (15 existing entries; add entry 16)
- [Source: tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs] — must add samples to prevent AllContracts serialization regression

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

Implemented all 6 tasks. Created `SecondAdopterStatus` sealed record vocabulary (4 values), `SecondAdopterStatusEntryV1` positional record (12 params), and `SecondAdopterStatusValidator` (5 error tokens). Added `SecondAdopterStatusJsonConverter` to serialization. Created 10-scenario conformance fixture, suite runner, and suite tests. Updated `ContractSamples.cs`, manifest, and test summary. All 1471 solution tests pass: 568 Contracts (+18), 216 Conformance (+15), no regressions.

### File List

- `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterVocabulary.cs` (new)
- `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterStatusEntryV1.cs` (new)
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (modified — added SecondAdopterStatusJsonConverter)
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterVocabularyTest.cs` (new)
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterStatusValidatorTest.cs` (new)
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (modified — added SecondAdopterStatus samples and SecondAdopterStatusEntryV1 instance)
- `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceFixtures.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuite.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuiteTest.cs` (new)
- `docs/release-evidence/conformance-manifest-v1-fixture.json` (modified — added story-6-6 entry)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified — added Story 6.6 section)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — status updated to review)

### Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot on 2026-05-23
**Outcome:** Approved

**AC Validation:** All 3 ACs implemented and verified by passing tests.
**Task Audit:** All tasks confirmed done. One subtask ("Conformance logic") was marked `[ ]` but was actually implemented in `SecondAdopterConformanceSuite.cs:84-86` — corrected to `[x]`.
**Code Quality:** Implementation follows established vocabulary/conformance-suite pattern from Stories 5.3–6.5 exactly. Validator guards are correct (WaiverRef null-guard, status-specific rules). No security, performance, or maintainability issues found.
**Test Quality:** 18 contracts tests + 15 conformance suite tests cover all ACs and edge cases. CS8122 pitfall handled correctly (`== null` not `is null`). Round-trip serialization test verified.
**Git vs Story:** All story File List entries confirmed present. Modified files (`ClosedVocabularyJsonConverters.cs`, `ContractSamples.cs`, manifest, test-summary, sprint-status) all documented. New files untracked as expected for new implementation.

**Issues Found:** 1 Medium (documentation error — task checkbox), 0 High, 0 Critical. Auto-fixed.

## Change Log

- 2026-05-23: Story 6.6 implemented. SecondAdopterStatus vocabulary (4 values), SecondAdopterStatusEntryV1 (12-param record), SecondAdopterStatusValidator (5 error tokens), SecondAdopterConformanceSuite (10 scenarios), 33 new tests. Full solution: 1471 tests, 0 failures.
- 2026-05-23: Code review complete. 1 medium issue auto-fixed (Conformance logic task checkbox). Status set to done.
