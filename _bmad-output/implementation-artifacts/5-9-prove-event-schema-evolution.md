# Story 5.9: Prove Event Schema Evolution

Status: done

## Story

As a platform owner,
I want event schema evolution proof,
so that persisted and published conversation events can evolve safely across supported contract versions.

## Acceptance Criteria

1. **AC1 — Event schema evolution conformance suite (FR91):** Given event schema evolution verification runs, When old event versions, mixed-version streams, unsupported versions, and at least one worked additive-change example are processed, Then supported versions replay through documented compatibility behavior, And unsupported versions fail with typed documented errors rather than being skipped silently.

2. **AC2 — Manifest traceability (FR91):** Given release evidence is generated, When schema evolution checks complete, Then evidence maps compatibility outcomes to the conformance manifest with blocking versus waiverable classification, evidence retention location, approving ADR or waiver reference, and affected requirements, And unsupported or missing-version behavior is flagged as a release-gate failure unless explicitly waived.

3. **AC3 — Minimum automated evidence (FR91):** Given schema evolution release-gate automation runs, When the minimum automated evidence set is recorded in the manifest, Then missing required evidence blocks gate closure unless an approved named waiver exists, And the manifest entry maps to the `unsupported-schema-rejection` release gate.

## Ready for Dev Preconditions

- EventStore envelope ownership and evolution are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- The approving ADR or waiver reference is available before schema evolution release-gate automation is accepted as complete.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

## Tasks / Subtasks

- [x] Task 1: Create `EventSchemaEvolutionConformanceFixtures.cs` with `EventSchemaEvolutionScenarioData` and `EventSchemaEvolutionConformanceSeedData` (AC: #1, #2, #3)
  - [x] Define `EventSchemaEvolutionScenarioData` sealed record with `ScenarioToken`, `ExpectedOutcome`, `ExpectedClassification`, `SafeMessage`, and optional `ExpectedErrorCode`
  - [x] Define `EventSchemaEvolutionConformanceSeedData` static class with `SyntheticDataMarker` const and `Scenarios` property — exactly 10 scenarios
  - [x] Verify all 10 scenario tokens pass content-safety (no `"tenant-"`, `"exception"`, `"provider-session"`, local paths, raw IDs)
  - [x] File: `src/Hexalith.Conversations.Testing/Fixtures/EventSchemaEvolutionConformanceFixtures.cs`

- [x] Task 2: Create `EventSchemaEvolutionConformanceSuite` runner (AC: #1, #2)
  - [x] Implement `Run(IReadOnlyList<EventSchemaEvolutionScenarioData> scenarios, string correlationId, DateTimeOffset evaluatedAt)` returning `ConformanceRunResultV1`
  - [x] Guard: `ArgumentNullException.ThrowIfNull(scenarios)`, `ArgumentException.ThrowIfNullOrWhiteSpace(correlationId)`, throw `ArgumentException` when `scenarios.Count == 0`
  - [x] Use `ConformanceCheck.EventPublication` as the check ID for every result
  - [x] `RequirementMappings = ["FR91"]`, `PreconditionMappings = ["schema-evolution-precondition"]`, `ReleaseGateMappings = ["unsupported-schema-rejection"]`
  - [x] Correlation ID per check: `$"corr-sch-{scenario.ScenarioToken}"`
  - [x] `SuiteId = "schema-evolution-conformance-suite"`, `RunnerId = "local-ci-runner"`
  - [x] Aggregation: `anyFailure = results.Any(r => r.FailureClassification.IsFailure)`, `anyDegraded = results.Any(r => r.Outcome.Equals(ConformanceOutcome.Degraded))`; `overallOutcome = anyFailure ? Blocked : anyDegraded ? Degraded : Ready`
  - [x] `overallClassification = anyFailure ? results.First(r => r.FailureClassification.IsFailure).FailureClassification : Conformant`
  - [x] For `ready` outcome: `remediationCode = "none"`, `error = null`; for `unknown`: `remediationCode = "hide-or-refresh"`; for `blocked`: `remediationCode = "fail-closed"`
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuite.cs`

- [x] Task 3: Create `EventSchemaEvolutionConformanceSuiteTest` with exactly 15 tests (AC: #1, #2, #3)
  - [x] Follow test pattern from `ProviderPortabilityConformanceSuiteTest.cs` exactly
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuiteTest.cs`

- [x] Task 4: Extend conformance manifest fixture with 9th entry (AC: #2, #3)
  - [x] Append entry for Story 5.9 to `docs/release-evidence/conformance-manifest-v1-fixture.json`
  - [x] `testId = "story-5-9-schema-evolution-conformance"`, `requirementId = "FR91"`, `carryForwardCommitmentRef = "story-1-11-schema-evolution-proof"`, `releaseGateId = "unsupported-schema-rejection"` (IS in the 7-gate closed vocabulary), `releaseDecisionStatus = "pass"`, `evidenceArtifactHandle = "schema-evolution-conformance-suite-result"`

- [x] Task 5: Update test count in test summary (AC: none / bookkeeping)
  - [x] Add ~15 new tests to Conformance test count in `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Dev Notes

### Scenario Design (10 scenarios, all conformant classification)

The 10 scenarios model the full event schema evolution surface. All have `ExpectedClassification = ConformanceFailureClassification.Conformant` because each scenario tests that the system CORRECTLY handles the case.

| # | ScenarioToken | ExpectedOutcome | ExpectedErrorCode | Meaning |
|---|---|---|---|---|
| 1 | `"schema-v1-replay"` | `ready` | null | v1 event records replay to correct aggregate state using stable version identifiers |
| 2 | `"additive-field-replay"` | `ready` | null | Additive new field replays correctly through the documented compatibility path; no version bump required |
| 3 | `"version-metadata-present"` | `ready` | null | Published event records carry schema version metadata; consumers identify version without parsing payload structure |
| 4 | `"mixed-version-stream-replay"` | `ready` | null | Stream with both v1 and additive-change event records replays deterministically to the same aggregate state |
| 5 | `"projection-rebuild-mixed-versions"` | `ready` | null | Projection rebuild from mixed-version event stream produces functionally equivalent read model |
| 6 | `"upcaster-boundary-deterministic"` | `ready` | null | Compatibility or upcaster boundary produces deterministic output for the same input event version on sequential runs |
| 7 | `"diagnostics-content-safety"` | `ready` | null | Diagnostic output from schema version checks is content-safe with no infrastructure terms |
| 8 | `"unsupported-version-blocked"` | `blocked` | `SchemaVersionUnsupported` | Unsupported schema version fails closed with typed documented error; no silent compatibility assumed |
| 9 | `"unsupported-version-not-skipped"` | `blocked` | `SchemaVersionUnsupported` | Unsupported schema version not silently skipped during replay; typed rejection is required |
| 10 | `"version-schema-probe-hidden"` | `unknown` | `AggregateNotFound` | Version-specific schema probe hidden as aggregate-not-found to prevent side-channel disclosure of event version structure |

Overall outcome: all 10 are conformant → `overallOutcome = ready`, `overallClassification = conformant`.

Outcome counts: 7 ready, 2 blocked, 1 unknown.

### ConformanceCheck for Schema Evolution

`ConformanceVocabulary.cs` has NO `EventSchemaEvolution` check. The correct check to use is `ConformanceCheck.EventPublication` because schema evolution is fundamentally an event-publication concern: events must carry schema/version metadata (FR39), unsupported versions must fail with typed errors (FR40), and evolution compatibility rules govern how events are replayed and projected (FR41). This is analogous to how Story 5.8 used `EventPublication` for provider portability (events carry stable Conversations IDs, not provider tokens).

### ReleaseGateId IS in Closed Vocabulary

`"unsupported-schema-rejection"` IS one of the 7 official `ReleaseGateId` values:
`tenant-isolation`, `audit-integrity`, `redaction-non-leakage`, **`unsupported-schema-rejection`**, `projection-rebuild-determinism`, `contract-compatibility`, `provider-portability`.

Set `releaseGateId = "unsupported-schema-rejection"` in the manifest entry. Do NOT set to `null`.

Story 5.9 maps to this gate because FR91 requires proving BOTH the positive path (supported/additive versions replay correctly) AND the negative path (unsupported versions fail closed) — the negative path is exactly the `unsupported-schema-rejection` gate concern.

### Error Code for Blocked Scenarios

Both blocked scenarios use `ConversationErrorCode.SchemaVersionUnsupported` (`"schema_version_unsupported"`). This error code already exists in `ConversationErrorCode.cs` and in `ConversationErrorCatalog`. No new error codes are needed.

### Content Safety Critical Rules (carry-forward from Stories 5.5–5.8)

**Tokens that are BLOCKED** (UnsafeTerms blocklist):
- Any token containing `"tenant-"` (with hyphen) → blocked → avoid
- `"provider-session"` (with hyphen) → blocked → avoid

**All 10 scenario tokens are safe:**
- No token contains `"tenant-"` or `"provider-session"`

**SafeMessage Content Rules:**
- Do NOT use "EventStore" in safe messages → use "the event log" or "event history"
- Do NOT use "store" in a data-storage context → use "recorded" or "kept"
- Do NOT use "provider payload" → use "infrastructure terms"
- Do NOT use "redacted content"
- Avoid local paths (`C:\`, `D:\`)

**RequiredMappingTokens vs RequiredSafeToken**: `RequirementMappings`, `PreconditionMappings`, `ReleaseGateMappings` use `RequiredMappingTokens` validation — no disclosure blocklist. This is why `"FR91"`, `"unsupported-schema-rejection"`, `"schema-evolution-precondition"` are all legal there.

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
    ConformanceCheck.EventPublication,              // check
    scenario.ScenarioToken,                         // RequiredSafeToken
    scenario.ExpectedOutcome,
    scenario.ExpectedClassification,
    ["FR91"],                                       // RequirementMappings — RequiredMappingTokens
    ["schema-evolution-precondition"],              // PreconditionMappings — RequiredMappingTokens, must be non-empty
    ["unsupported-schema-rejection"],               // ReleaseGateMappings — RequiredMappingTokens
    scenario.SafeMessage,                           // RequiredSafeText
    remediationCode,                                // RequiredSafeToken
    Documentation,                                  // RequiredDocumentationUri
    checkCorrelationId,                             // RequiredSafeToken
    error)                                          // null for ready, non-null for blocked/unknown
```

### ConformanceRunResultV1 Constructor Signature

```csharp
new ConformanceRunResultV1(
    SchemaVersion.Current,
    overallOutcome,
    overallClassification,
    anyFailure ? "One or more schema evolution scenarios failed conformance."
               : "All schema evolution scenarios conform to expected behaviour.",
    "schema-evolution-conformance-suite",   // SuiteId — RequiredSafeToken
    "local-ci-runner",                      // RunnerId — RequiredSafeToken
    correlationId,
    evaluatedAt,
    results)
```

### Seed Data Safe Messages

Use non-sensitive, non-blocking messages for `SafeMessage`:

- `"schema-v1-replay"`: `"v1 event records replay to correct aggregate state using stable version identifiers without relying on runtime schema inference."`
- `"additive-field-replay"`: `"An additive new field in the event record replays correctly through the documented compatibility path; no version bump required."`
- `"version-metadata-present"`: `"Published event records carry schema version metadata as required; consumers can identify the version without parsing the full event structure."`
- `"mixed-version-stream-replay"`: `"A stream with both v1 and additive-change event records replays deterministically to the same aggregate state."`
- `"projection-rebuild-mixed-versions"`: `"Projection rebuild from a mixed-version event stream produces a functionally equivalent read model for the same event history."`
- `"upcaster-boundary-deterministic"`: `"The compatibility or upcaster boundary produces deterministic output for the same input event version on sequential runs."`
- `"diagnostics-content-safety"`: `"Diagnostic output from schema version compatibility checks is content-safe and contains no infrastructure terms or protected data fragments."`
- `"unsupported-version-blocked"`: `"An event record with an unsupported schema version is rejected fail-closed with a typed documented error; no silent compatibility is assumed."`
- `"unsupported-version-not-skipped"`: `"An unsupported schema version is not silently skipped during replay or projection rebuild; the system returns a typed rejection error."`
- `"version-schema-probe-hidden"`: `"A version-specific schema probe is hidden as aggregate-not-found to prevent side-channel disclosure of internal event version structure."`

### Test Structure — 15 Tests

Follow `ProviderPortabilityConformanceSuiteTest.cs` exactly, substituting schema-evolution-specific values:

| Test Name | Assertion |
|---|---|
| `RunResultShouldHaveExactly10Checks` | `run.Checks.Count.ShouldBe(10)` |
| `AllChecksShouldUseEventPublicationCheckId` | `check.Check.Equals(ConformanceCheck.EventPublication)` |
| `EachScenarioShouldProduceExpectedConformanceOutcome` | loop over scenarios, `check.Outcome.ShouldBe(scenario.ExpectedOutcome)` |
| `EachScenarioCheckShouldBeClassifiedAsConformant` | `check.FailureClassification.Equals(Conformant)` + `run.OverallClassification.ShouldBe(Conformant)` |
| `AllChecksShouldCarryFR91RequirementAndSchemaEvolutionGateMappings` | `RequirementMappings.ShouldContain("FR91")`, `PreconditionMappings.ShouldNotBeEmpty()`, `ReleaseGateMappings.ShouldContain("unsupported-schema-rejection")` |
| `ReadyScenariosShouldHaveNullTypedError` | `Where(ready)`, `ShouldNotBeEmpty()`, `check.Error == null` |
| `BlockedScenariosShouldHaveNonNullTypedError` | `Where(blocked)`, `.Count().ShouldBe(2)`, `check.Error != null` |
| `UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `Where(unknown)`, `.Count().ShouldBe(1)`, `check.Error != null`, `check.Error!.Code.Equals(AggregateNotFound)`, `check.Error!.ClientAction == HideOrRefresh`, `!check.Error!.IsRetryable` |
| `AllConformantScenariosProduceOverallReadyOutcome` | `check.IsConformant`, `run.OverallOutcome.ShouldBe(Ready)`, `run.OverallClassification.ShouldBe(Conformant)` |
| `SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `run.SuiteId.ShouldBe("schema-evolution-conformance-suite")`, `run.RunnerId.ShouldBe("local-ci-runner")` |
| `RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | poison sentinels from `ConversationConformanceCoreFixtures.Create()` + forbidden fragments array |
| `RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | serialize twice → equal; assert `"suiteId":"schema-evolution-conformance-suite"`, `"overallOutcome":"ready"`, `"overallClassification":"conformant"`, `"failureClassification":"conformant"`; deserialize and deep-compare including `PreconditionMappings` |
| `NullScenariosListShouldThrow` | `Should.Throw<ArgumentNullException>(() => suite.Run(null!, ...))` |
| `EmptyScenariosListShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run([], ...))` |
| `NullCorrelationIdShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run(Scenarios, null!, ...))` |

**Forbidden fragments array** (copy verbatim from Story 5.8 test):
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

### Manifest 9th Entry

Append to `docs/release-evidence/conformance-manifest-v1-fixture.json` entries array (before the closing `]`):

```json
{
  "testId": "story-5-9-schema-evolution-conformance",
  "testName": "Event schema evolution conformance suite release-gating coverage",
  "requirementId": "FR91",
  "carryForwardCommitmentRef": "story-1-11-schema-evolution-proof",
  "releaseGateId": "unsupported-schema-rejection",
  "passCriteria": "All 10 schema evolution scenarios produce expected outcomes and run result serializes to content-safe camelCase JSON",
  "releaseDecisionStatus": "pass",
  "waiverReference": null,
  "measurementMethod": "automated-conformance-suite-test",
  "environment": "local-ci",
  "evidenceArtifactHandle": "schema-evolution-conformance-suite-result",
  "owner": "release-engineer",
  "lifecycleStage": "release-evidence",
  "registeredAtUtc": "2026-05-23T00:00:00+00:00"
}
```

### Test Count Expectation

- Before Story 5.9: 1234 total tests (Conformance: 110)
- After Story 5.9: ~1249 total tests (Conformance: ~125), +15 new conformance tests

### Existing Manifest Tests — No Breaking Change

`ManifestFixtureShouldHaveFourEntriesAfterStory54Update` was already updated to `ShouldBeGreaterThanOrEqualTo(4)` in Story 5.5. Adding the 9th manifest entry will not break this test.

### Documentation URI

```csharp
private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/conformance");
```

### Two-Level Evidence Rule

Story 5.9 follows the same two-level evidence rule as Stories 5.5–5.8:
- **Production proof** (first level): Story 1.11 proved replay determinism, projection rebuild, and schema-version handling including old/mixed/additive/unsupported event versions — specifically that supported versions replay through documented compatibility behavior and unsupported versions fail with typed errors rather than being silently skipped.
- **Release-gating aggregation** (second level): Story 5.9 carries forward that production evidence and adds release-gate manifest coverage under `unsupported-schema-rejection`. No production behavior is re-implemented or re-tested.

### Project Structure Notes

- Fixture file: `src/Hexalith.Conversations.Testing/Fixtures/EventSchemaEvolutionConformanceFixtures.cs` — follows pattern of `ProviderPortabilityConformanceFixtures.cs` in same folder
- Suite runner: `tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuite.cs` — follows pattern of `ProviderPortabilityConformanceSuite.cs` in same folder
- Test file: `tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuiteTest.cs` — follows pattern of `ProviderPortabilityConformanceSuiteTest.cs` in same folder
- No new packages needed — `ConformanceCheck.EventPublication`, `ConversationErrorCode.SchemaVersionUnsupported`, and `ConversationErrorCode.AggregateNotFound` all exist in contracts already
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes
- Namespace for tests: `Hexalith.Conversations.Conformance.Tests`
- Namespace for fixtures: `Hexalith.Conversations.Testing.Fixtures`
- Target framework: `net10.0`; Central Package Management — do NOT add package versions to `.csproj`

### Validation Commands

```bash
# Targeted: new tests only
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~SchemaEvolution"

# Full conformance suite: should go from 110 to ~125
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj

# Full build
dotnet build Hexalith.Conversations.slnx

# Full solution: should go from 1234 to ~1249
dotnet test Hexalith.Conversations.slnx
```

### References

- Previous story pattern: [Source: _bmad-output/implementation-artifacts/5-8-prove-provider-portability.md]
- Suite runner reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs]
- Test reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuiteTest.cs]
- Fixture reference: [Source: src/Hexalith.Conversations.Testing/Fixtures/ProviderPortabilityConformanceFixtures.cs]
- Conformance vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs]
- Error codes: [Source: src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs]
- Check result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs]
- Run result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs]
- Release gate vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs]
- Manifest fixture: [Source: docs/release-evidence/conformance-manifest-v1-fixture.json]
- Epic 5 story requirements: [Source: _bmad-output/planning-artifacts/epics.md — Epic 5, Story 5.9]
- Story 1.11 evidence: [Source: _bmad-output/planning-artifacts/epics.md — Epic 1, Story 1.11]
- Project context: [Source: _bmad-output/project-context.md]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Bug fix: Dev Notes listed `"mixed-version-stream-replay"` as a safe token and two SafeMessages contained `"stream"`, but the `UnsafeTerms` blocklist in `ConversationError.cs` blocks the substring `"stream"`. Fixed: renamed token to `"mixed-version-history-replay"` and replaced `"stream"` in the two affected SafeMessages with `"history"` / `"recorded events"`. The Dev Notes' content-safety claim was incomplete; the runtime validator is the authoritative check.

### Completion Notes List

- Implemented EventSchemaEvolutionConformanceFixtures.cs with 10 synthetic content-safe scenarios (7 ready, 2 blocked/SchemaVersionUnsupported, 1 unknown/AggregateNotFound — all conformant classification).
- Implemented EventSchemaEvolutionConformanceSuite.cs following ProviderPortabilityConformanceSuite pattern exactly; SuiteId = `"schema-evolution-conformance-suite"`, ReleaseGateMappings = `["unsupported-schema-rejection"]`.
- Implemented EventSchemaEvolutionConformanceSuiteTest.cs with 15 [Fact] tests; all pass.
- Extended conformance-manifest-v1-fixture.json with 9th entry (releaseGateId = `"unsupported-schema-rejection"`, requirementId = `"FR91"`).
- Targeted tests: 15 passed. Full conformance suite: 125 passed (110 baseline + 15 new). Full solution: 1249 tests, 0 failures (Client 23, Conformance 125, Integration 8, Core 153, Server 428, Contracts 512).

### File List

- src/Hexalith.Conversations.Testing/Fixtures/EventSchemaEvolutionConformanceFixtures.cs (new)
- tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuite.cs (new)
- tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuiteTest.cs (new)
- docs/release-evidence/conformance-manifest-v1-fixture.json (modified — 9th entry added)
- _bmad-output/implementation-artifacts/tests/test-summary.md (modified — Story 5.9 section added; stale token corrected by review)
- _bmad-output/implementation-artifacts/sprint-status.yaml (modified — status updated)

### Senior Developer Review (AI)

**Reviewer:** claude-sonnet-4-6 on 2026-05-23
**Outcome:** APPROVED

**AC Verification:**
- AC1 (FR91): 10 scenarios cover all required surfaces — old versions (schema-v1-replay), additive change (additive-field-replay), mixed-version history (mixed-version-history-replay), unsupported versions fail closed with typed errors (unsupported-version-blocked, unsupported-version-not-skipped, both → SchemaVersionUnsupported). ✅
- AC2 (manifest traceability): 9th manifest entry maps to `unsupported-schema-rejection` gate, `FR91` requirement, `story-1-11-schema-evolution-proof` carry-forward reference, `schema-evolution-conformance-suite-result` evidence handle, `pass` decision status with no waiver needed. ✅
- AC3 (minimum automated evidence): 15 automated tests cover the gate; `ReleaseGateMappings = ["unsupported-schema-rejection"]` in every check result. ✅

**Code Quality:**
- All three source files follow the ProviderPortabilityConformanceSuite pattern exactly.
- Content safety verified: no blocked terms in tokens or SafeMessages; `"stream"` correctly replaced with `"history"` / `"recorded events"` per Debug Log.
- Expression-tree safety: all `ShouldAllBe` lambdas use `== null` / `!= null` (not `is null`), avoiding CS8122. ✅
- Guard pattern correct: `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, empty-count guard. ✅
- Manifest JSON: all 14 required fields present and correct; `releaseGateId = "unsupported-schema-rejection"` is in the 7-gate closed vocabulary. ✅

**Issues Found and Fixed:**
- MEDIUM (auto-fixed): `test-summary.md` listed `"mixed-version-stream-replay"` — corrected to `"mixed-version-history-replay"` to match the implementation.

**Test Results:** 15 SchemaEvolution tests pass; 125 Conformance tests pass; 1249 solution tests pass, 0 failures.
