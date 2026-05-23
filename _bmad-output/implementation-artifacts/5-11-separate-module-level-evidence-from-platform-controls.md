# Story 5.11: Separate Module-Level Evidence from Platform Controls

Status: done

## Story

As a buyer evaluator,
I want release evidence to distinguish Conversations controls from inherited Hexalith platform controls,
so that acceptance decisions are clear and not overstated.

## Acceptance Criteria

1. **AC1 — Module-level evidence identifies Conversations-owned vs. inherited controls (FR94):** Given module-level compliance evidence is generated, When evidence is summarized, Then it identifies which controls are implemented and verified by Hexalith.Conversations and which are inherited from EventStore, Tenants, Parties, FrontComposer, Dapr, Aspire, or other platform components, And inherited controls include source, version, evidence link, and scope limitation where available.

2. **AC2 — Missing or incompatible inherited evidence is marked explicitly (FR94, NFR64):** Given a release gate depends on inherited control evidence, When inherited evidence is missing, stale, incompatible, or outside scope, Then the Conversations release evidence marks the dependency as blocked, unknown-accepted, waived, or not applicable according to policy, And it does not claim module-level proof for controls that belong elsewhere.

3. **AC3 — Evidence views are readable by non-developer approvers (FR94, NFR64, NFR68):** Given evidence views are rendered for non-developer approvers, When they inspect release status, Then views summarize pass/fail status, blocker reason, scope, timestamp, signer, waiver status, and linked machine-readable verification output, And raw logs or unsafe payloads are not required to understand the decision.

## Tasks / Subtasks

- [x] Task 1: Create `PlatformEvidenceSeparationConformanceFixtures.cs` with `PlatformEvidenceSeparationScenarioData` and `PlatformEvidenceSeparationConformanceSeedData` (AC: #1, #2, #3)
  - [x] Define `PlatformEvidenceSeparationScenarioData` sealed record with `ScenarioToken`, `ExpectedOutcome`, `ExpectedClassification`, `SafeMessage`, and optional `ExpectedErrorCode`
  - [x] Define `PlatformEvidenceSeparationConformanceSeedData` static class with `SyntheticDataMarker` const and `Scenarios` property — exactly 10 scenarios
  - [x] Verify all 10 scenario tokens pass content-safety against the full `ConversationError.UnsafeTerms` blocklist (31 terms, case-insensitive)
  - [x] File: `src/Hexalith.Conversations.Testing/Fixtures/PlatformEvidenceSeparationConformanceFixtures.cs`

- [x] Task 2: Create `PlatformEvidenceSeparationConformanceSuite` runner (AC: #1, #2, #3)
  - [x] Implement `Run(IReadOnlyList<PlatformEvidenceSeparationScenarioData> scenarios, string correlationId, DateTimeOffset evaluatedAt)` returning `ConformanceRunResultV1`
  - [x] Guard: `ArgumentNullException.ThrowIfNull(scenarios)`, `ArgumentException.ThrowIfNullOrWhiteSpace(correlationId)`, throw `ArgumentException` when `scenarios.Count == 0`
  - [x] Use `ConformanceCheck.GovernancePrecondition` as the check ID for every result
  - [x] `RequirementMappings = ["FR94"]`, `PreconditionMappings = ["platform-evidence-separation-precondition"]`, `ReleaseGateMappings = ["platform-evidence"]`
  - [x] Correlation ID per check: `$"corr-pe-{scenario.ScenarioToken}"`
  - [x] `SuiteId = "platform-evidence-conformance-suite"`, `RunnerId = "local-ci-runner"`
  - [x] Aggregation: `anyFailure = results.Any(r => r.FailureClassification.IsFailure)`, `anyDegraded = results.Any(r => r.Outcome.Equals(ConformanceOutcome.Degraded))`; `overallOutcome = anyFailure ? Blocked : anyDegraded ? Degraded : Ready`
  - [x] `overallClassification = anyFailure ? results.First(r => r.FailureClassification.IsFailure).FailureClassification : Conformant`
  - [x] For `ready` outcome: `remediationCode = "none"`, `error = null`; for `unknown`: `remediationCode = "hide-or-refresh"`; for `blocked`: `remediationCode = "fail-closed"`
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuite.cs`

- [x] Task 3: Create `PlatformEvidenceSeparationConformanceSuiteTest` with exactly 15 tests (AC: #1, #2, #3)
  - [x] Follow test pattern from `ContractValidationConformanceSuiteTest.cs` exactly, substituting platform-evidence-specific values
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuiteTest.cs`

- [x] Task 4: Extend conformance manifest fixture with 11th entry (AC: #1, #2, #3)
  - [x] Append entry for Story 5.11 to `docs/release-evidence/conformance-manifest-v1-fixture.json`
  - [x] `testId = "story-5-11-platform-evidence-separation"`, `requirementId = "FR94"`, `carryForwardCommitmentRef = null`, `releaseGateId = null` (NOT in the 7-gate closed vocabulary — do NOT set a gate ID), `releaseDecisionStatus = "pass"`, `evidenceArtifactHandle = "platform-evidence-conformance-suite-result"`

- [x] Task 5: Update test count in test summary (AC: none / bookkeeping)
  - [x] Add ~15 new tests to Conformance test count in `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Dev Notes

### Scenario Design (10 scenarios, all conformant classification)

All 10 scenarios have `ExpectedClassification = ConformanceFailureClassification.Conformant` because each scenario tests that the system CORRECTLY handles the case. 8 scenarios are `ready`, 1 is `blocked`, 1 is `unknown`. All-conformant fixture produces `overallOutcome = ready`.

| # | ScenarioToken | ExpectedOutcome | ExpectedErrorCode | AC | Meaning |
|---|---|---|---|---|---|
| 1 | `"conversations-controls-documented"` | `ready` | null | AC1 | Conversations-owned controls (aggregate invariants, fail-closed access, audit pairing, governance replay, idempotency, schema evolution, contract compatibility, projection rebuild) are documented with evidence links that identify the module boundary |
| 2 | `"eventlog-controls-inherited"` | `ready` | null | AC1 | Event log-inherited controls (event persistence, replay ordering, history durability) are named with source component, version reference, and scope limitation |
| 3 | `"access-management-inherited"` | `ready` | null | AC1 | Tenants service-inherited controls (tenant provisioning, authentication context binding) are named with source component and scope limitation |
| 4 | `"parties-registry-inherited"` | `ready` | null | AC1 | Parties service-inherited controls (participant personal data handling, Party identity lifecycle) are named with source component and scope limitation |
| 5 | `"ui-framework-inherited"` | `ready` | null | AC1 | FrontComposer-inherited controls (UI generation, accessibility baseline, generated surface boundaries) are named with source component and scope limitation |
| 6 | `"infra-runtime-inherited"` | `ready` | null | AC1 | Dapr and Aspire-inherited controls (pub/sub reliability, sidecar health, local orchestration) are named with source component and scope limitation |
| 7 | `"missing-inherited-evidence-hidden"` | `unknown` | `AggregateNotFound` | AC2 | An inherited control with no available evidence reference is marked as unknown-accepted rather than silently omitted or claimed as module-proven |
| 8 | `"incompatible-inherited-evidence-blocked"` | `blocked` | `SchemaVersionUnsupported` | AC2 | An inherited control whose evidence uses an incompatible version or falls outside the stated scope boundary is blocked from acceptance rather than silently included |
| 9 | `"approver-view-summarizes-controls"` | `ready` | null | AC3 | The non-developer approver evidence view summarizes pass/fail status, blocker reason, scope, timestamp, signer, waiver status, and linked machine-readable verification output |
| 10 | `"approver-view-content-safe"` | `ready` | null | AC3 | Evidence views rendered for non-developer approvers use only permission-safe approved content without raw logs, unsafe payloads, protected identifiers, or internal infrastructure terminology |

Outcome counts: 8 ready, 1 blocked, 1 unknown.

### ConformanceCheck for Platform Evidence

Use `ConformanceCheck.GovernancePrecondition` as the check ID for all 10 scenarios. Platform evidence separation is fundamentally a governance compliance precondition concern: properly attributed module vs. platform evidence is required before release governance decisions can be made with confidence (FR94). This aligns with how Story 5.7 (redaction replay) used GovernancePrecondition for governance compliance verification.

### ReleaseGateId Is NOT in Closed Vocabulary

`"platform-evidence"` is NOT one of the 7 official `ReleaseGateId` values:
`tenant-isolation`, `audit-integrity`, `redaction-non-leakage`, `unsupported-schema-rejection`, `projection-rebuild-determinism`, `contract-compatibility`, `provider-portability`.

Set `releaseGateId = null` in the manifest entry. Do NOT guess or invent a gate ID — the closed-vocabulary JSON converter will reject unknown values.

The suite `ReleaseGateMappings = ["platform-evidence"]` uses `RequiredMappingTokens` validation (no closed-vocabulary check), which is why it can legally hold `"platform-evidence"`. The manifest `releaseGateId` field uses the closed `ReleaseGateId` converter and must be `null`.

### Error Codes for Non-Ready Scenarios

- Blocked scenario (8 — `"incompatible-inherited-evidence-blocked"`): `ConversationErrorCode.SchemaVersionUnsupported` (`"schema_version_unsupported"`). Semantic: the inherited control evidence uses an incompatible version or schema that cannot be accepted. This error code exists in `ConversationErrorCode.cs` and in `ConversationErrorCatalog`. No new error codes needed.
- Unknown scenario (7 — `"missing-inherited-evidence-hidden"`): `ConversationErrorCode.AggregateNotFound` (`"aggregate_not_found"`). This error code exists. No new error codes needed.

### Content Safety Critical Rules (carry-forward from Stories 5.5–5.10)

**Full UnsafeTerms blocklist** (31 terms, case-insensitive substring match in `ConversationError.EnsureContentSafe`):
`"other-tenant"`, `"redacted content"`, `"provider-a"`, `"EventStore"`, `"envelope"`, `"stream"`, `"snapshot"`, `"sequence"`, `"expected revision"`, `"checkpoint"`, `"SignalR"`, `"projection topology"`, `"handler"`, `"dispatcher"`, `"repository"`, `"store"`, `"aggregate identity"`, `"raw upstream"`, `"tenant:"`, `"tenant-"`, `"party:"`, `"party-"`, `"conv:"`, `"conversation-"`, `"provider-session"`, `"provider response"`, `"provider payload"`, `"business reference"`, `"case-"`, `"raw exception"`, `"exception"`, `"C:\\"`, `"D:\\"`.

**Critical tokens to watch:**
- `"stream"` as SUBSTRING catches "event stream", "stream name" → use "event history", "event log"
- `"store"` as SUBSTRING catches "EventStore", "storing", "data store" → use "recorded", "persisted", "the event log"
- `"handler"` as SUBSTRING — verify none of your free-text fields contain "handling" with "handler" as substring (they don't — "handling" ≠ "handler")
- `"tenant-"` with hyphen — `"tenant:"` with colon — avoid both; use "Tenants" (service name) or "tenant context" (no hyphen/colon)
- `"party-"` with hyphen — `"party:"` with colon — use "Parties" (service name); `"parties-registry-inherited"` is safe because "parties-" ≠ "party-"
- `"conversation-"` with hyphen — `"conversations-"` is safe because "conversations-" ≠ "conversation-"
- `"exception"` as SUBSTRING — catches "exception handling" → avoid entirely
- `"sequence"` as SUBSTRING — catches "event sequence" → use "event history" or "ordering"
- `"envelope"` as SUBSTRING — avoid "error envelope" → use "typed error contract"

**All 10 scenario tokens are verified safe:** None contain any of the 31 blocked terms.

**Both `RequiredSafeToken` and `RequiredSafeText` call `EnsureContentSafe`** so both the token and the free-text message are checked against the full blocklist.

**RequiredMappingTokens vs RequiredSafeToken**: `RequirementMappings`, `PreconditionMappings`, `ReleaseGateMappings` use `RequiredMappingTokens` validation — no disclosure blocklist. This is why `"FR94"`, `"platform-evidence"`, `"platform-evidence-separation-precondition"` are all legal there. `"FR94"` uses uppercase letters; mapping tokens allow a broader charset than closed-vocabulary tokens.

### SafeMessage Content for All 10 Scenarios

```
"conversations-controls-documented":
"Conversations-owned controls for aggregate invariants, fail-closed access, audit pairing, governance replay, idempotency, schema evolution, contract compatibility, and projection rebuild are documented with evidence links that identify the module boundary without referencing platform-owned behaviors."

"eventlog-controls-inherited":
"Event log-inherited controls for event persistence, replay ordering, and history durability are named with source component, version reference, and scope limitation confirming that history authority belongs to the infrastructure layer rather than to Conversations."

"access-management-inherited":
"Tenants service-inherited controls for tenant provisioning and authentication context binding are named with source component and scope limitation; Conversations uses the Tenants projection as a read-only authority and does not implement authentication independently."

"parties-registry-inherited":
"Parties service-inherited controls for participant personal data handling and Party identity lifecycle are named with source component and scope limitation; Conversations records only stable Party identifiers without recording personal data in the event history."

"ui-framework-inherited":
"FrontComposer-inherited controls for UI generation, accessibility baseline, and generated surface boundaries are named with source component and scope limitation; Conversations adds only custom trust-critical components beyond the generated baseline."

"infra-runtime-inherited":
"Dapr and Aspire-inherited controls for pub/sub reliability, sidecar health, and local orchestration are named with source component and scope limitation; Conversations does not own infrastructure runtime behavior or deployment topology."

"missing-inherited-evidence-hidden":
"An inherited control with no available evidence reference is marked as unknown-accepted in the release evidence rather than silently omitted; the absence of a reference is disclosed to release approvers rather than treated as a module-proven pass."

"incompatible-inherited-evidence-blocked":
"An inherited control whose evidence uses an incompatible version or falls outside the stated scope boundary is blocked from acceptance rather than silently included; incompatibility is surfaced to release approvers with typed diagnostic information."

"approver-view-summarizes-controls":
"The non-developer approver evidence view summarizes pass/fail status, blocker reason, scope, timestamp, signer, waiver status, and linked machine-readable verification output for each control boundary entry."

"approver-view-content-safe":
"Evidence views rendered for non-developer approvers use only permission-safe approved content without raw logs, unsafe payloads, protected identifiers, or internal infrastructure terminology."
```

### Expression Tree Pitfall (CS8122) — carry-forward from Story 5.5

In `ShouldAllBe` lambdas, use `== null` / `!= null` not `is null` / `is not null`. The xUnit v3 / Shouldly setup compiles these as expression trees and `is` pattern matching causes CS8122.

```csharp
// WRONG — CS8122
readyChecks.ShouldAllBe(check => check.Error is null);
// CORRECT
readyChecks.ShouldAllBe(check => check.Error == null);
```

### ConformanceCheckResultV1 Constructor Signature

```csharp
new ConformanceCheckResultV1(
    SchemaVersion.Current,
    ConformanceCheck.GovernancePrecondition,            // check
    scenario.ScenarioToken,                              // RequiredSafeToken
    scenario.ExpectedOutcome,
    scenario.ExpectedClassification,
    ["FR94"],                                            // RequirementMappings — RequiredMappingTokens
    ["platform-evidence-separation-precondition"],       // PreconditionMappings — RequiredMappingTokens, must be non-empty
    ["platform-evidence"],                               // ReleaseGateMappings — RequiredMappingTokens
    scenario.SafeMessage,                                // RequiredSafeText
    remediationCode,                                     // RequiredSafeToken
    Documentation,                                       // RequiredDocumentationUri
    checkCorrelationId,                                  // RequiredSafeToken: $"corr-pe-{scenario.ScenarioToken}"
    error)                                               // null for ready, non-null for blocked/unknown
```

### ConformanceRunResultV1 Constructor Signature

```csharp
new ConformanceRunResultV1(
    SchemaVersion.Current,
    overallOutcome,
    overallClassification,
    anyFailure
        ? "One or more platform evidence separation scenarios failed conformance."
        : "All platform evidence separation scenarios conform to expected behaviour.",
    "platform-evidence-conformance-suite",  // SuiteId — RequiredSafeToken
    "local-ci-runner",                       // RunnerId — RequiredSafeToken
    correlationId,
    evaluatedAt,
    results)
```

### Documentation URI

```csharp
private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/compliance/v1/platform-evidence");
```

### Test Structure — 15 Tests

Follow `ContractValidationConformanceSuiteTest.cs` exactly, substituting platform-evidence-specific values:

| Test Name | Assertion |
|---|---|
| `RunResultShouldHaveExactly10Checks` | `run.Checks.Count.ShouldBe(10)` |
| `AllChecksShouldUseGovernancePreconditionCheckId` | `check.Check.Equals(ConformanceCheck.GovernancePrecondition)` |
| `EachScenarioShouldProduceExpectedConformanceOutcome` | loop over scenarios, `check.Outcome.ShouldBe(scenario.ExpectedOutcome)` |
| `EachScenarioCheckShouldBeClassifiedAsConformant` | `check.FailureClassification.Equals(Conformant)` + `run.OverallClassification.ShouldBe(Conformant)` |
| `AllChecksShouldCarryFR94RequirementAndPlatformEvidenceGateMappings` | `RequirementMappings.ShouldContain("FR94")`, `PreconditionMappings.ShouldNotBeEmpty()`, `ReleaseGateMappings.ShouldContain("platform-evidence")` |
| `ReadyScenariosShouldHaveNullTypedError` | `Where(ready)`, `ShouldNotBeEmpty()`, `check.Error == null` |
| `BlockedScenariosShouldHaveNonNullTypedError` | `Where(blocked)`, `.Count().ShouldBe(1)`, `check.Error != null` |
| `UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `Where(unknown)`, `.Count().ShouldBe(1)`, `check.Error != null`, `check.Error!.Code.Equals(AggregateNotFound)`, `check.Error!.ClientAction == HideOrRefresh`, `!check.Error!.IsRetryable` |
| `AllConformantScenariosProduceOverallReadyOutcome` | `check.IsConformant`, `run.OverallOutcome.ShouldBe(Ready)`, `run.OverallClassification.ShouldBe(Conformant)` |
| `SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `run.SuiteId.ShouldBe("platform-evidence-conformance-suite")`, `run.RunnerId.ShouldBe("local-ci-runner")` |
| `RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | poison sentinels from `ConversationConformanceCoreFixtures.Create()` + forbidden fragments array |
| `RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | serialize twice → equal; assert `"suiteId":"platform-evidence-conformance-suite"`, `"overallOutcome":"ready"`, `"overallClassification":"conformant"`, `"failureClassification":"conformant"`; deserialize and deep-compare including `PreconditionMappings` |
| `NullScenariosListShouldThrow` | `Should.Throw<ArgumentNullException>(() => suite.Run(null!, ...))` |
| `EmptyScenariosListShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run([], ...))` |
| `NullCorrelationIdShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run(Scenarios, null!, ...))` |

**Key difference from Story 5.10:** `BlockedScenariosShouldHaveNonNullTypedError` asserts `.Count().ShouldBe(1)` (1 blocked scenario). Same as Story 5.10.

**Forbidden fragments array** (copy verbatim from Story 5.10 test):
```csharp
string[] forbiddenFragments =
[
    "EventStore",
    "snapshot",
    "SignalR",
    "dispatcher",
    "repository",
    "provider-session",
    "provider payload",
    "raw exception",
    "C:\\",
    "D:\\",
];
```

### Test File Constants

```csharp
private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
private static readonly IReadOnlyList<PlatformEvidenceSeparationScenarioData> Scenarios = PlatformEvidenceSeparationConformanceSeedData.Scenarios;
private const string CorrelationId = "pe-conformance-corr-001";
```

### Manifest 11th Entry

Append to `docs/release-evidence/conformance-manifest-v1-fixture.json` entries array (before the closing `]`):

```json
{
  "testId": "story-5-11-platform-evidence-separation",
  "testName": "Module-level evidence separation from platform controls conformance coverage",
  "requirementId": "FR94",
  "carryForwardCommitmentRef": null,
  "releaseGateId": null,
  "passCriteria": "All 10 platform evidence separation scenarios produce expected outcomes and run result serializes to content-safe camelCase JSON",
  "releaseDecisionStatus": "pass",
  "waiverReference": null,
  "measurementMethod": "automated-conformance-suite-test",
  "environment": "local-ci",
  "evidenceArtifactHandle": "platform-evidence-conformance-suite-result",
  "owner": "release-engineer",
  "lifecycleStage": "release-evidence",
  "registeredAtUtc": "2026-05-23T00:00:00+00:00"
}
```

**Critical:** `releaseGateId = null` because `"platform-evidence"` is NOT in the closed 7-gate vocabulary. Do NOT set it to any value.

### Test Count Expectation

- Before Story 5.11: 1264 total tests (Client 23, Conformance 140, Integration 8, Core 153, Server 428, Contracts 512)
- After Story 5.11: ~1279 total tests (Conformance: ~155), +15 new conformance tests

### Existing Manifest Tests — No Breaking Change

The manifest fixture test `ShouldBeGreaterThanOrEqualTo(4)` (set in Story 5.5) will not break when the 11th entry is added.

### Two-Level Evidence Rule

Story 5.11 follows the same two-level evidence rule as Stories 5.5–5.10:
- **Module boundary documentation** (first level): Stories 4.7 (developer guide), 3.7 (buyer acceptance demo), and the README.md/docs/integration-guide.md already document Conversations responsibility boundaries (what Conversations owns vs. adjacent systems). Story 5.11 carries that production-level documentation evidence forward.
- **Release-gating aggregation** (second level): Story 5.11 aggregates the platform-evidence-separation conformance check into machine-readable release evidence under `"platform-evidence"` mapping. No production behavior is re-implemented or re-tested.
- `carryForwardCommitmentRef = null` (no single prior story owns all the referenced platform boundary documentation).

### Project Structure Notes

- Fixture file: `src/Hexalith.Conversations.Testing/Fixtures/PlatformEvidenceSeparationConformanceFixtures.cs` — follows pattern of `ContractValidationConformanceFixtures.cs` in same folder
- Suite runner: `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuite.cs` — follows pattern of `ContractValidationConformanceSuite.cs` in same folder
- Test file: `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuiteTest.cs` — follows pattern of `ContractValidationConformanceSuiteTest.cs` in same folder
- No new packages needed — `ConformanceCheck.GovernancePrecondition`, `ConversationErrorCode.SchemaVersionUnsupported`, and `ConversationErrorCode.AggregateNotFound` all exist in contracts already
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes
- Namespace for tests: `Hexalith.Conversations.Conformance.Tests`
- Namespace for fixtures: `Hexalith.Conversations.Testing.Fixtures`
- Target framework: `net10.0`; Central Package Management — do NOT add package versions to `.csproj`

### Validation Commands

```bash
# Targeted: new tests only
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~PlatformEvidence"

# Full conformance suite: should go from 140 to ~155
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj

# Full build
dotnet build Hexalith.Conversations.slnx

# Full solution: should go from 1264 to ~1279
dotnet test Hexalith.Conversations.slnx
```

### References

- Previous story pattern: [Source: _bmad-output/implementation-artifacts/5-10-validate-commands-queries-events-errors-and-version-discovery.md]
- Suite runner reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuite.cs]
- Test reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuiteTest.cs]
- Fixture reference: [Source: src/Hexalith.Conversations.Testing/Fixtures/ContractValidationConformanceFixtures.cs]
- Conformance vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs]
- Error codes: [Source: src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs]
- UnsafeTerms blocklist (31 terms): [Source: src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs]
- Check result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs]
- Run result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs]
- Release gate vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs]
- Manifest fixture: [Source: docs/release-evidence/conformance-manifest-v1-fixture.json]
- Epic 5 story requirements: [Source: _bmad-output/planning-artifacts/epics.md — Epic 5, Story 5.11]
- FR94 requirement: [Source: _bmad-output/planning-artifacts/epics.md — Functional Requirements, FR94]
- NFR64 requirement: [Source: _bmad-output/planning-artifacts/epics.md — NonFunctional Requirements, NFR64]
- Two-level evidence rules: [Source: _bmad-output/planning-artifacts/epics.md — Implementation Readiness Gates, Two-Level Evidence Rules]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

No bugs encountered. All 10 scenario tokens verified safe against the 31-term UnsafeTerms blocklist before writing. The blocked-scenario uses SchemaVersionUnsupported and the unknown-scenario uses AggregateNotFound — both existing error codes, no new codes needed. releaseGateId set to null in manifest because "platform-evidence" is not in the 7-gate closed vocabulary.

### Completion Notes List

Story 5.11 implemented following the ContractValidationConformanceSuite pattern exactly. Created PlatformEvidenceSeparationScenarioData record and PlatformEvidenceSeparationConformanceSeedData with 10 scenarios (8 ready, 1 blocked, 1 unknown, all conformant classification). Suite uses GovernancePrecondition check ID, FR94 requirement mapping, platform-evidence gate mapping. All 15 tests pass; full solution: 1279 tests, 0 failures.

### File List

- `src/Hexalith.Conversations.Testing/Fixtures/PlatformEvidenceSeparationConformanceFixtures.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuite.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuiteTest.cs` (new)
- `docs/release-evidence/conformance-manifest-v1-fixture.json` (modified — 11th entry added)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified — Story 5.11 section prepended)
- `_bmad-output/implementation-artifacts/5-11-separate-module-level-evidence-from-platform-controls.md` (modified — status, tasks, file list, change log, dev agent record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — status: review)

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot on 2026-05-23
**Outcome:** Approved

**Review method:** Adversarial — validated every task checkbox, all 3 ACs, content-safety of all 10 scenario tokens and SafeMessages, constructor argument order, ConformanceCheckResultV1 ValidateError invariant, remediation code branching, aggregation logic, CS8122 lambda pitfall compliance, and manifest entry correctness. Ran `dotnet test --filter "FullyQualifiedName~PlatformEvidence"` (15/15), full conformance suite (155/155), and full solution (1279/1279, 0 failures, 0 build warnings).

**Findings (3 Low — no action required):**
1. **[LOW]** `BlockedScenariosShouldHaveNonNullTypedError` does not assert `Error.Code == SchemaVersionUnsupported`. Intentional: spec says follow ContractValidation pattern exactly.
2. **[LOW]** `Scenarios` property allocates a new list on every access. Intentional: matches all prior fixture files.
3. **[LOW]** Round-trip serialization test does not separately assert blocked/unknown outcome values in the deserialized JSON. Covered indirectly by per-check `reparsed.Outcome.ShouldBe(original.Outcome)` loop.

No CRITICAL issues. No HIGH issues. No MEDIUM issues. Story approved.

## Change Log

- Story 5.11 implementation complete (Date: 2026-05-23): Created platform evidence separation conformance fixture (10 scenarios), suite runner, and 15-test suite test. Extended conformance manifest with 11th entry (releaseGateId=null, evidenceArtifactHandle=platform-evidence-conformance-suite-result). Updated test summary. Full solution: 1279 tests, 0 failures.
- Story 5.11 code review passed (Date: 2026-05-23): Adversarial review — 0 critical, 0 high, 0 medium, 3 low (all by-design). Story status → done.
