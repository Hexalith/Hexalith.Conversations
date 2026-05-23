# Story 5.8: Prove Provider Portability

Status: done

## Story

As a platform owner,
I want provider portability proof,
so that conversation history remains recoverable without provider-owned session authority.

## Acceptance Criteria

1. **AC1 — Provider portability conformance suite (FR90):** Given the conformance suite runs provider portability tests, When provider-owned correlation identifiers are stripped, changed, unavailable, migrated, duplicated, or inconsistent, Then conversation history remains recoverable from Conversations identity, stable references, and EventStore history, And provider IDs remain correlation metadata rather than durable source-of-truth identity, And diagnostics are content-safe and contain no provider payload or session data.

2. **AC2 — Invariants remain stable across provider configuration differences (FR90, NFR50-NFR52):** Given portability verification covers contract-level behavior, When persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence are evaluated, Then tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism remain invariant across provider configuration differences.

3. **AC3 — Manifest traceability (FR90):** Given provider portability evidence is generated, When conformance results are written to the release manifest, Then evidence maps portability outcomes to release-gate status, blocking versus waiverable classification, evidence retention location, approving ADR or waiver reference, and affected requirements, And it distinguishes portability failures from infrastructure or test execution failures, And the manifest entry maps to the `provider-portability` release gate.

4. **AC4 — Minimum automated evidence (FR90):** Given provider portability release-gate automation runs, When the minimum automated evidence set is recorded in the manifest, Then missing required evidence blocks gate closure unless an approved named waiver exists.

## Tasks / Subtasks

- [x] Task 1: Create `ProviderPortabilityConformanceFixtures.cs` with `ProviderPortabilityScenarioData` and `ProviderPortabilityConformanceSeedData` (AC: #1, #2, #3)
  - [x] Define `ProviderPortabilityScenarioData` sealed record with `ScenarioToken`, `ExpectedOutcome`, `ExpectedClassification`, `SafeMessage`, and optional `ExpectedErrorCode`
  - [x] Define `ProviderPortabilityConformanceSeedData` static class with `SyntheticDataMarker` const and `Scenarios` property — exactly 10 scenarios
  - [x] Verify all 10 scenario tokens pass content-safety (no `"tenant-"`, `"exception"`, `"provider-session"`, local paths, raw IDs)
  - [x] File: `src/Hexalith.Conversations.Testing/Fixtures/ProviderPortabilityConformanceFixtures.cs`

- [x] Task 2: Create `ProviderPortabilityConformanceSuite` runner (AC: #1, #2)
  - [x] Implement `Run(IReadOnlyList<ProviderPortabilityScenarioData> scenarios, string correlationId, DateTimeOffset evaluatedAt)` returning `ConformanceRunResultV1`
  - [x] Guard: `ArgumentNullException.ThrowIfNull(scenarios)`, `ArgumentException.ThrowIfNullOrWhiteSpace(correlationId)`, throw `ArgumentException` when `scenarios.Count == 0`
  - [x] Use `ConformanceCheck.EventPublication` as the check ID for every result
  - [x] `RequirementMappings = ["FR90"]`, `PreconditionMappings = ["portability-precondition"]`, `ReleaseGateMappings = ["provider-portability"]`
  - [x] Correlation ID per check: `$"corr-prt-{scenario.ScenarioToken}"`
  - [x] `SuiteId = "portability-conformance-suite"`, `RunnerId = "local-ci-runner"`
  - [x] Aggregation: `anyFailure = results.Any(r => r.FailureClassification.IsFailure)`, `anyDegraded = results.Any(r => r.Outcome.Equals(ConformanceOutcome.Degraded))`; `overallOutcome = anyFailure ? Blocked : anyDegraded ? Degraded : Ready`
  - [x] `overallClassification = anyFailure ? results.First(r => r.FailureClassification.IsFailure).FailureClassification : Conformant`
  - [x] For `ready` outcome: `remediationCode = "none"`, `error = null`; for `unknown`: `remediationCode = "hide-or-refresh"`; for `blocked`: `remediationCode = "fail-closed"`
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs`

- [x] Task 3: Create `ProviderPortabilityConformanceSuiteTest` with exactly 15 tests (AC: #1, #2, #3)
  - [x] Follow test pattern from `IdempotencyConformanceSuiteTest.cs` exactly
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuiteTest.cs`

- [x] Task 4: Extend conformance manifest fixture with 8th entry (AC: #3, #4)
  - [x] Append entry for Story 5.8 to `docs/release-evidence/conformance-manifest-v1-fixture.json`
  - [x] `testId = "story-5-8-portability-conformance"`, `requirementId = "FR90"`, `carryForwardCommitmentRef = "story-1-11-replay-portability-proof"`, `releaseGateId = "provider-portability"` (IS in the 7-gate closed vocabulary), `releaseDecisionStatus = "pass"`, `evidenceArtifactHandle = "portability-conformance-suite-result"`

- [x] Task 5: Update test count in test summary (AC: none / bookkeeping)
  - [x] Add ~15 new tests to Conformance test count in `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Dev Notes

### Scenario Design (10 scenarios, all conformant classification)

The 10 scenarios model the full provider portability surface. All have `ExpectedClassification = ConformanceFailureClassification.Conformant` because each scenario tests that the system CORRECTLY handles the case — conformant classification means the system passed, not that a violation was attempted.

| # | ScenarioToken | ExpectedOutcome | ExpectedErrorCode | Meaning |
|---|---|---|---|---|
| 1 | `"provider-id-stripped"` | `ready` | null | Conversation remains recoverable after provider correlation ID stripped |
| 2 | `"provider-id-changed"` | `ready` | null | Conversation remains recoverable after provider correlation ID changed |
| 3 | `"session-expiry-recoverable"` | `ready` | null | Conversation remains recoverable when provider session expires; EventStore is session-independent |
| 4 | `"provider-id-migrated"` | `ready` | null | Conversation remains recoverable after provider migrates its ID format |
| 5 | `"projection-rebuild-without-provider"` | `ready` | null | Projection rebuild from EventStore succeeds without provider session authority |
| 6 | `"replay-determinism-without-provider"` | `ready` | null | Aggregate replay is deterministic independent of provider correlation |
| 7 | `"provider-only-identity-blocked"` | `blocked` | `ProviderOnlyIdentityForbidden` | Command using provider-owned ID as conversation identity rejected fail-closed |
| 8 | `"session-authority-blocked"` | `blocked` | `ProviderOnlyIdentityForbidden` | Command requiring provider session as source-of-truth authority blocked fail-closed |
| 9 | `"cross-provider-correlation-hidden"` | `unknown` | `AggregateNotFound` | Cross-provider correlation probe hidden as aggregate-not-found to prevent side-channel disclosure |
| 10 | `"diagnostics-content-safety"` | `ready` | null | Diagnostic output is content-safe and contains no provider payload or session data |

Overall outcome: all 10 are conformant → `overallOutcome = ready`, `overallClassification = conformant`.

Outcome counts: 7 ready, 2 blocked, 1 unknown.

### ConformanceCheck for Provider Portability

`ConformanceVocabulary.cs` has NO `ProviderPortability` check. The correct check to use is `ConformanceCheck.EventPublication` because provider portability is fundamentally an event-persistence concern: events in EventStore must carry stable Conversations IDs (not provider session tokens), making them self-contained and provider-independent. Provider portability is proven when EventPublication checks validate that events are emitted with stable references rather than provider-owned session authority. This is analogous to how Story 5.7 used `GovernancePrecondition` for redaction replay (no "redaction replay" check exists; the closest conceptual boundary is governance preconditions).

### ReleaseGateId IS in Closed Vocabulary

`"provider-portability"` IS one of the 7 official `ReleaseGateId` values:
`tenant-isolation`, `audit-integrity`, `redaction-non-leakage`, `unsupported-schema-rejection`, `projection-rebuild-determinism`, `contract-compatibility`, **`provider-portability`**.

Set `releaseGateId = "provider-portability"` in the manifest entry. Do NOT set to `null`.

### Content Safety Critical Rules (carry-forward from Stories 5.5, 5.6, 5.7)

**Tokens that are BLOCKED** (UnsafeTerms blocklist):
- Any token containing `"tenant-"` (with hyphen) → blocked → avoid
- `"provider-session"` (with hyphen) appears in the JSON forbidden fragments array → avoid in scenario tokens

**Safe ScenarioTokens used:**
- `"session-expiry-recoverable"` (NOT `"provider-session-expired"` — avoid `"provider-session"`)
- `"session-authority-blocked"` (NOT `"provider-session-as-authority-blocked"`)
- `"portability-conformance-suite"` — safe SuiteId

**SafeMessage Content Rules:**
- Do NOT use the phrase "provider session" with hyphen in safe messages (use "session" or rephrase)
- Do NOT use "redacted content" (blocked in earlier stories)
- Avoid `"tenant-"` prefix in any safe message text

**RequiredMappingTokens vs RequiredSafeToken**: `RequirementMappings`, `PreconditionMappings`, `ReleaseGateMappings` use `RequiredMappingTokens` validation — no disclosure blocklist. This is why `"FR90"`, `"provider-portability"`, `"portability-precondition"` are all legal there.

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
    ["FR90"],                                       // RequirementMappings — RequiredMappingTokens
    ["portability-precondition"],                   // PreconditionMappings — RequiredMappingTokens, must be non-empty
    ["provider-portability"],                       // ReleaseGateMappings — RequiredMappingTokens
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
    anyFailure ? "One or more provider portability scenarios failed conformance."
               : "All provider portability scenarios conform to expected behaviour.",
    "portability-conformance-suite",   // SuiteId — RequiredSafeToken
    "local-ci-runner",                 // RunnerId — RequiredSafeToken
    correlationId,
    evaluatedAt,
    results)
```

### Test Structure — 15 Tests

Follow `IdempotencyConformanceSuiteTest.cs` exactly, substituting portability-specific values:

| Test Name | Assertion |
|---|---|
| `RunResultShouldHaveExactly10Checks` | `run.Checks.Count.ShouldBe(10)` |
| `AllChecksShouldUseEventPublicationCheckId` | `check.Check.Equals(ConformanceCheck.EventPublication)` |
| `EachScenarioShouldProduceExpectedConformanceOutcome` | loop over scenarios, `check.Outcome.ShouldBe(scenario.ExpectedOutcome)` |
| `EachScenarioCheckShouldBeClassifiedAsConformant` | `check.FailureClassification.Equals(Conformant)` + `run.OverallClassification.ShouldBe(Conformant)` |
| `AllChecksShouldCarryFR90RequirementAndPortabilityGateMappings` | `RequirementMappings.ShouldContain("FR90")`, `PreconditionMappings.ShouldNotBeEmpty()`, `ReleaseGateMappings.ShouldContain("provider-portability")` |
| `ReadyScenariosShouldHaveNullTypedError` | `Where(ready)`, `ShouldNotBeEmpty()`, `check.Error == null` |
| `BlockedScenariosShouldHaveNonNullTypedError` | `Where(blocked)`, `.Count().ShouldBe(2)`, `check.Error != null` |
| `UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `Where(unknown)`, `.Count().ShouldBe(1)`, `check.Error != null`, `check.Error!.Code.Equals(AggregateNotFound)`, `check.Error!.ClientAction == HideOrRefresh`, `!check.Error!.IsRetryable` |
| `AllConformantScenariosProduceOverallReadyOutcome` | `check.IsConformant`, `run.OverallOutcome.ShouldBe(Ready)`, `run.OverallClassification.ShouldBe(Conformant)` |
| `SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `run.SuiteId.ShouldBe("portability-conformance-suite")`, `run.RunnerId.ShouldBe("local-ci-runner")` |
| `RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | poison sentinels from `ConversationConformanceCoreFixtures.Create()` + forbidden fragments array |
| `RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | serialize twice → equal; assert `"suiteId":"portability-conformance-suite"`, `"overallOutcome":"ready"`, `"overallClassification":"conformant"`, `"failureClassification":"conformant"`; deserialize and deep-compare including `PreconditionMappings` |
| `NullScenariosListShouldThrow` | `Should.Throw<ArgumentNullException>(() => suite.Run(null!, ...))` |
| `EmptyScenariosListShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run([], ...))` |
| `NullCorrelationIdShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run(Scenarios, null!, ...))` |

**Forbidden fragments array** (copy verbatim from Story 5.7 test):
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

### Manifest 8th Entry

Append to `docs/release-evidence/conformance-manifest-v1-fixture.json` entries array (before the closing `]`):

```json
{
  "testId": "story-5-8-portability-conformance",
  "testName": "Provider portability conformance suite release-gating coverage",
  "requirementId": "FR90",
  "carryForwardCommitmentRef": "story-1-11-replay-portability-proof",
  "releaseGateId": "provider-portability",
  "passCriteria": "All 10 provider portability scenarios produce expected outcomes and run result serializes to content-safe camelCase JSON",
  "releaseDecisionStatus": "pass",
  "waiverReference": null,
  "measurementMethod": "automated-conformance-suite-test",
  "environment": "local-ci",
  "evidenceArtifactHandle": "portability-conformance-suite-result",
  "owner": "release-engineer",
  "lifecycleStage": "release-evidence",
  "registeredAtUtc": "2026-05-23T00:00:00+00:00"
}
```

### Test Count Expectation

- Before Story 5.8: 1219 total tests (Conformance: 95)
- After Story 5.8: ~1234 total tests (Conformance: ~110), +15 new conformance tests

### Existing Manifest Tests — No Breaking Change

`ManifestFixtureShouldHaveFourEntriesAfterStory54Update` was already updated to `ShouldBeGreaterThanOrEqualTo(4)` in Story 5.5. Adding the 8th manifest entry will not break this test.

### Documentation URI

```csharp
private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/conformance");
```

### Seed Data Safe Messages

Use non-sensitive, non-blocking messages for `SafeMessage`:

- `"provider-id-stripped"`: `"Conversation remains recoverable after provider correlation ID stripped; EventStore history uses stable Conversations IDs only."`
- `"provider-id-changed"`: `"Conversation remains recoverable after provider correlation ID changed; replay uses stable Conversations references rather than provider correlation."`
- `"session-expiry-recoverable"`: `"Conversation remains recoverable when the provider session expires; EventStore source of truth is independent of session state."`
- `"provider-id-migrated"`: `"Conversation remains recoverable after provider migrates its ID format; stable Conversations IDs remain unchanged throughout."`
- `"projection-rebuild-without-provider"`: `"Projection rebuild from EventStore succeeds without provider session authority; stable IDs drive the rebuild."`
- `"replay-determinism-without-provider"`: `"Aggregate replay is deterministic independent of provider correlation; provider IDs are stored as correlation metadata only."`
- `"provider-only-identity-blocked"`: `"Command using provider-owned ID as conversation identity rejected fail-closed; provider-only identity is forbidden."`
- `"session-authority-blocked"`: `"Command requiring provider session as conversation authority blocked fail-closed; EventStore is the sole durable source of truth."`
- `"cross-provider-correlation-hidden"`: `"Cross-provider correlation probe hidden as aggregate-not-found to prevent side-channel disclosure of provider boundaries."`
- `"diagnostics-content-safety"`: `"Diagnostic output is content-safe and contains no provider payload or protected data fragments."`

### Two-Level Evidence Rule

Story 5.8 follows the same two-level evidence rule as Stories 5.5, 5.6, and 5.7:
- **Production proof** (first level): Story 1.11 proved replay determinism, projection rebuild, and schema versioning including the provider portability dimension (events use stable Conversations IDs, not provider session tokens; rebuild succeeds without provider session authority).
- **Release-gating aggregation** (second level): Story 5.8 carries forward that production evidence and adds release-gate manifest coverage under `provider-portability`. No production behavior is re-implemented or re-tested.

### Error Code for Blocked Scenarios

Both blocked scenarios use `ConversationErrorCode.ProviderOnlyIdentityForbidden` (`"provider_only_identity_forbidden"`). This error code already exists in `ConversationErrorCode.cs` and in `ConversationErrorCatalog`. No new error codes are needed.

### Project Structure Notes

- Fixture file: `src/Hexalith.Conversations.Testing/Fixtures/ProviderPortabilityConformanceFixtures.cs` — follows pattern of `RedactionConformanceFixtures.cs` in same folder
- Suite runner: `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs` — follows pattern of `RedactionConformanceSuite.cs` in same folder
- Test file: `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuiteTest.cs` — follows pattern of `RedactionConformanceSuiteTest.cs` in same folder
- No new packages needed — `ConformanceCheck.EventPublication`, `ConversationErrorCode.ProviderOnlyIdentityForbidden`, and `ConversationErrorCode.AggregateNotFound` all exist in contracts already
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes
- Namespace for tests: `Hexalith.Conversations.Conformance.Tests`
- Namespace for fixtures: `Hexalith.Conversations.Testing.Fixtures`
- Target framework: `net10.0`; Central Package Management — do NOT add package versions to `.csproj`

### Validation Commands

```bash
# Targeted: new tests only
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~Portability"

# Full conformance suite: should go from 95 to ~110
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj

# Full build
dotnet build Hexalith.Conversations.slnx

# Full solution: should go from 1219 to ~1234
dotnet test Hexalith.Conversations.slnx
```

### References

- Previous story pattern: [Source: _bmad-output/implementation-artifacts/5-7-verify-redaction-replay-conformance.md]
- Suite runner reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/RedactionConformanceSuite.cs]
- Test reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/RedactionConformanceSuiteTest.cs]
- Fixture reference: [Source: src/Hexalith.Conversations.Testing/Fixtures/RedactionConformanceFixtures.cs]
- Conformance vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs]
- Error codes: [Source: src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs]
- Check result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs]
- Run result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs]
- Release gate vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs]
- Manifest fixture: [Source: docs/release-evidence/conformance-manifest-v1-fixture.json]
- Epic 5 story requirements: [Source: _bmad-output/planning-artifacts/epics.md — Epic 5, Story 5.8]
- Project context: [Source: _bmad-output/project-context.md]

### Project Structure Notes

- Alignment with Epic 5 conformance pattern: all files follow the exact same structure as Stories 5.5, 5.6, 5.7
- No new ConformanceCheck, ConformanceOutcome, ReleaseGateId, or ConversationErrorCode values — all required vocabulary exists
- No src/ library additions — this is a pure testing/evidence story like 5.5–5.7

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Bug fix: 6 SafeMessage values in Dev Notes contained blocked UnsafeTerms ("EventStore", "store", "provider payload"). Replaced with content-safe equivalents: "EventStore" → "the event log" or "the event history source of truth"; "stored as" → "kept as"; "provider payload" → "infrastructure terms".

### Completion Notes List

- Implemented `ProviderPortabilityScenarioData` record and `ProviderPortabilityConformanceSeedData` with 10 content-safe synthetic scenarios (7 ready, 2 blocked/ProviderOnlyIdentityForbidden, 1 unknown/AggregateNotFound).
- Implemented `ProviderPortabilityConformanceSuite` runner using `ConformanceCheck.EventPublication`, FR90 requirement mapping, and `provider-portability` release gate mapping.
- Implemented 15 `[Fact]` tests in `ProviderPortabilityConformanceSuiteTest` following the RedactionConformanceSuiteTest pattern exactly.
- Extended `conformance-manifest-v1-fixture.json` with 8th entry for `provider-portability` release gate.
- Updated test summary with Story 5.8 section.
- All 1234 solution tests pass (Client 23, Conformance 110, Integration 8, Core 153, Server 428, Contracts 512). +15 new tests from baseline of 1219.

### File List

- `src/Hexalith.Conversations.Testing/Fixtures/ProviderPortabilityConformanceFixtures.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuiteTest.cs` (new)
- `docs/release-evidence/conformance-manifest-v1-fixture.json` (modified — 8th entry added)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified — Story 5.8 section added)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — status updated)

## Senior Developer Review (AI)

**Reviewer:** AI Senior Developer | **Date:** 2026-05-23

**Outcome:** Approved — no critical or high-severity issues

**Checklist:**
- [x] Story file loaded and status verified as reviewable (review)
- [x] Epic 5, Story 8 IDs resolved
- [x] Git vs File List cross-reference: 0 discrepancies (3 new + 3 modified, all match)
- [x] All 4 Acceptance Criteria validated as implemented
- [x] All 5 tasks marked [x] confirmed complete by passing tests
- [x] 15 [Fact] tests confirmed present and passing
- [x] Full solution: 1234 tests, 0 failures (Conformance 95→110, +15)
- [x] Content safety: no forbidden fragments in fixture tokens, safe messages, or suite output
- [x] CS8122 guard: ShouldAllBe lambdas use `== null`/`!= null` exclusively
- [x] Manifest 8th entry verified: all required fields match story spec
- [x] Sprint status synced

**Findings:** 0 High, 2 Medium (design-consistent, no fix needed), 2 Low (informational)

## Change Log

- 2026-05-23: Story 5.8 implemented — ProviderPortabilityConformanceSuite (10 scenarios, FR90, provider-portability gate), 15 conformance tests, manifest 8th entry. Total solution tests: 1234 (Conformance 95→110).
- 2026-05-23: Senior Developer Review — Approved. 0 critical/high issues. Status → done.
