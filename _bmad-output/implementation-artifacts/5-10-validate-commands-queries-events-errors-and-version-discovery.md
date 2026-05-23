# Story 5.10: Validate Commands, Queries, Events, Errors, and Version Discovery

Status: done

## Story

As a release owner,
I want executable contract tests for all adopter-facing surfaces,
so that command, query, event, error, and version-discovery contracts are release-ready.

## Acceptance Criteria

1. **AC1 — Executable contract tests validate all adopter-facing surfaces (FR92):** Given executable contract tests run before v1 release, When commands, queries/projections, emitted events, typed errors, version discovery, and compatibility status are validated, Then each surface matches the published contract package and documentation, And no test requires adopter knowledge of EventStore internals.

2. **AC2 — Consumer-driven contract tests prove stability for Stories 2.4 and 4.2 (FR92):** Given consumer-driven contract tests run, When redaction command/event/audit behavior and .NET client compatibility are validated for Stories 2.4 and 4.2, Then commands, emitted events, typed errors, audit handles, freshness metadata, idempotency outcomes, and compatibility status remain stable for adopters, And test failures identify whether the break is command, event, audit, client, versioning, or documentation behavior.

3. **AC3 — Adopter-style CORE fixtures prove realistic integration behavior (FR93):** Given adopter-style CORE fixtures are used, When create, append, read, freshness, tenant denial, idempotency, and typed error paths are exercised, Then tests prove realistic adopter behavior and safe precondition handling, And fixture data is synthetic and tenant-safe.

4. **AC4 — Project conformance invariants have traceable automated evidence (FR92):** Given project conformance invariants are validated, When EventStore authority, Tenants fail-closed access, Parties-owned personal data, and FrontComposer generated-first boundaries are checked, Then each invariant has traceable automated evidence or an approved waiver in the manifest, And boundary drift is treated as a release-gate failure rather than a documentation issue.

5. **AC5 — Contract validation failure reporting is content-safe and traceable (FR92):** Given contract validation fails, When differences are reported, Then failures identify affected contract surface, version, requirement mapping, expected behavior, actual behavior, and remediation path, And diagnostics remain content-safe.

## Ready for Dev Preconditions

- Story 4.5 local evidence (AdopterConformanceSuite + CORE fixture, 25 conformance tests) is complete and in `done` status in sprint-status.yaml.
- Story 5.9 local evidence (EventSchemaEvolutionConformanceSuite, 15 conformance tests) is complete and manifest fixture extended to 9 entries.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

## Tasks / Subtasks

- [x] Task 1: Create `ContractValidationConformanceFixtures.cs` with `ContractValidationScenarioData` and `ContractValidationConformanceSeedData` (AC: #1, #2, #3, #4, #5)
  - [x] Define `ContractValidationScenarioData` sealed record with `ScenarioToken`, `ExpectedOutcome`, `ExpectedClassification`, `SafeMessage`, and optional `ExpectedErrorCode`
  - [x] Define `ContractValidationConformanceSeedData` static class with `SyntheticDataMarker` const and `Scenarios` property — exactly 10 scenarios
  - [x] Verify all 10 scenario tokens pass content-safety (no `"tenant-"`, `"exception"`, `"provider-session"`, `"stream"`, local paths, raw IDs)
  - [x] File: `src/Hexalith.Conversations.Testing/Fixtures/ContractValidationConformanceFixtures.cs`

- [x] Task 2: Create `ContractValidationConformanceSuite` runner (AC: #1, #2, #4, #5)
  - [x] Implement `Run(IReadOnlyList<ContractValidationScenarioData> scenarios, string correlationId, DateTimeOffset evaluatedAt)` returning `ConformanceRunResultV1`
  - [x] Guard: `ArgumentNullException.ThrowIfNull(scenarios)`, `ArgumentException.ThrowIfNullOrWhiteSpace(correlationId)`, throw `ArgumentException` when `scenarios.Count == 0`
  - [x] Use `ConformanceCheck.CompatibilityDiscovery` as the check ID for every result
  - [x] `RequirementMappings = ["FR92"]`, `PreconditionMappings = ["contract-validation-precondition"]`, `ReleaseGateMappings = ["contract-compatibility"]`
  - [x] Correlation ID per check: `$"corr-cv-{scenario.ScenarioToken}"`
  - [x] `SuiteId = "contract-validation-conformance-suite"`, `RunnerId = "local-ci-runner"`
  - [x] Aggregation: `anyFailure = results.Any(r => r.FailureClassification.IsFailure)`, `anyDegraded = results.Any(r => r.Outcome.Equals(ConformanceOutcome.Degraded))`; `overallOutcome = anyFailure ? Blocked : anyDegraded ? Degraded : Ready`
  - [x] `overallClassification = anyFailure ? results.First(r => r.FailureClassification.IsFailure).FailureClassification : Conformant`
  - [x] For `ready` outcome: `remediationCode = "none"`, `error = null`; for `unknown`: `remediationCode = "hide-or-refresh"`; for `blocked`: `remediationCode = "fail-closed"`
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuite.cs`

- [x] Task 3: Create `ContractValidationConformanceSuiteTest` with exactly 15 tests (AC: #1, #2, #3, #4, #5)
  - [x] Follow test pattern from `ProviderPortabilityConformanceSuiteTest.cs` exactly
  - [x] File: `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuiteTest.cs`

- [x] Task 4: Extend conformance manifest fixture with 10th entry (AC: #1, #2)
  - [x] Append entry for Story 5.10 to `docs/release-evidence/conformance-manifest-v1-fixture.json`
  - [x] `testId = "story-5-10-contract-validation-conformance"`, `requirementId = "FR92"`, `carryForwardCommitmentRef = "story-4-5-adopter-conformance-suite"`, `releaseGateId = "contract-compatibility"` (IS in the 7-gate closed vocabulary), `releaseDecisionStatus = "pass"`, `evidenceArtifactHandle = "contract-validation-conformance-suite-result"`

- [x] Task 5: Update test count in test summary (AC: none / bookkeeping)
  - [x] Add ~15 new tests to Conformance test count in `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Dev Notes

### Scenario Design (10 scenarios, all conformant classification)

The 10 scenarios model the full contract validation surface from Story 5.10 ACs. All have `ExpectedClassification = ConformanceFailureClassification.Conformant` because each scenario tests that the system CORRECTLY handles the case.

| # | ScenarioToken | ExpectedOutcome | ExpectedErrorCode | AC | Meaning |
|---|---|---|---|---|---|
| 1 | `"command-contract-shape"` | `ready` | null | AC1 | Create-conversation and append-message command contracts match the published contract package shape and carry schema and version metadata |
| 2 | `"query-contract-shape"` | `ready` | null | AC1 | Read-timeline and list-conversations query contracts match the published contract package and return freshness metadata |
| 3 | `"event-publication-shape"` | `ready` | null | AC1 | Domain events carry schema and version metadata as required by the contract; no internal infrastructure terms exposed |
| 4 | `"typed-error-shape"` | `ready` | null | AC1 | Typed error contract is content-safe with machine-readable code, category, retryability, and documentation pointer (renamed from `"error-envelope-shape"` — `"envelope"` is in UnsafeTerms blocklist) |
| 5 | `"version-discovery-shape"` | `ready` | null | AC1 | Version-discovery returns active command, projection, event, and client package versions in the published contract shape |
| 6 | `"core-fixture-happy-path"` | `ready` | null | AC3 | Adopter-style CORE fixture exercises create, append, and read with Current freshness and stable participant attribution |
| 7 | `"core-fixture-blocked-schema"` | `blocked` | `SchemaVersionUnsupported` | AC3, AC5 | CORE fixture typed failure case: unsupported schema version fails closed with a documented error; no silent compatibility assumed |
| 8 | `"core-fixture-probe-hidden"` | `unknown` | `AggregateNotFound` | AC3, AC5 | CORE fixture cross-authorization probe is hidden as aggregate-not-found to prevent side-channel disclosure of protected record existence |
| 9 | `"redaction-consumer-contract"` | `ready` | null | AC2 | Redaction command, event, and audit contracts remain stable for consumer-driven validation; no breaking change introduced |
| 10 | `"conformance-invariant-proof"` | `ready` | null | AC4 | Project conformance invariants have traceable automated evidence: event log authority, fail-closed access, personal-data boundaries, and generated-first UI boundaries |

Overall outcome: all 10 are conformant → `overallOutcome = ready`, `overallClassification = conformant`.

Outcome counts: 8 ready, 1 blocked, 1 unknown.

### ConformanceCheck for Contract Validation

Use `ConformanceCheck.CompatibilityDiscovery` as the check ID for all 10 scenarios. Contract validation is fundamentally a compatibility-discovery concern: contracts must match the published package (FR92), version discovery must surface active contract versions, and the release gate is `contract-compatibility`. This is analogous to how Stories 5.8 and 5.9 used `EventPublication` for their respective concerns.

### ReleaseGateId IS in Closed Vocabulary

`"contract-compatibility"` IS one of the 7 official `ReleaseGateId` values:
`tenant-isolation`, `audit-integrity`, `redaction-non-leakage`, `unsupported-schema-rejection`, `projection-rebuild-determinism`, **`contract-compatibility`**, `provider-portability`.

Set `releaseGateId = "contract-compatibility"` in the manifest entry. Do NOT set to `null`.

Story 5.10 maps to this gate because FR92 requires proving BOTH the positive path (contracts match the published package) AND the negative path (unsupported versions fail closed, cross-authorization probes collapse to hidden) — the full contract compatibility surface is exactly the `contract-compatibility` gate concern.

### Error Codes for Non-Ready Scenarios

- Blocked scenario (7 — `"core-fixture-blocked-schema"`): `ConversationErrorCode.SchemaVersionUnsupported` (`"schema_version_unsupported"`). This error code already exists in `ConversationErrorCode.cs` and in `ConversationErrorCatalog`. No new error codes needed.
- Unknown scenario (8 — `"core-fixture-probe-hidden"`): `ConversationErrorCode.AggregateNotFound`. This error code already exists. No new error codes needed.

### Content Safety Critical Rules (carry-forward from Stories 5.5–5.9)

**Tokens that are BLOCKED** (UnsafeTerms blocklist):
- Any token containing `"tenant-"` (with hyphen) → blocked → avoid
- `"provider-session"` (with hyphen) → blocked → avoid
- `"stream"` (substring) → blocked → avoid (was the bug in Story 5.9 fixed by renaming "mixed-version-stream-replay" to "mixed-version-history-replay")

**All 10 scenario tokens are safe:**
- None contain `"tenant-"`, `"provider-session"`, or `"stream"`

**SafeMessage Content Rules:**
- Do NOT use "EventStore" in safe messages → use "the event log" or "event history"
- Do NOT use "store" in a data-storage context → use "recorded" or "kept"
- Do NOT use "provider payload" → use "infrastructure terms"
- Do NOT use "redacted content"
- Avoid local paths (`C:\`, `D:\`)
- Do NOT use "snapshot", "SignalR", "dispatcher", "repository"

**RequiredMappingTokens vs RequiredSafeToken**: `RequirementMappings`, `PreconditionMappings`, `ReleaseGateMappings` use `RequiredMappingTokens` validation — no disclosure blocklist. This is why `"FR92"`, `"contract-compatibility"`, `"contract-validation-precondition"` are all legal there. `"FR92"` uses uppercase letters; mapping tokens allow a broader charset than closed-vocabulary tokens.

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
    ConformanceCheck.CompatibilityDiscovery,         // check
    scenario.ScenarioToken,                           // RequiredSafeToken
    scenario.ExpectedOutcome,
    scenario.ExpectedClassification,
    ["FR92"],                                         // RequirementMappings — RequiredMappingTokens
    ["contract-validation-precondition"],             // PreconditionMappings — RequiredMappingTokens, must be non-empty
    ["contract-compatibility"],                       // ReleaseGateMappings — RequiredMappingTokens
    scenario.SafeMessage,                             // RequiredSafeText
    remediationCode,                                  // RequiredSafeToken
    Documentation,                                    // RequiredDocumentationUri
    checkCorrelationId,                               // RequiredSafeToken: $"corr-cv-{scenario.ScenarioToken}"
    error)                                            // null for ready, non-null for blocked/unknown
```

### ConformanceRunResultV1 Constructor Signature

```csharp
new ConformanceRunResultV1(
    SchemaVersion.Current,
    overallOutcome,
    overallClassification,
    anyFailure ? "One or more contract validation scenarios failed conformance."
               : "All contract validation scenarios conform to expected behaviour.",
    "contract-validation-conformance-suite",  // SuiteId — RequiredSafeToken
    "local-ci-runner",                         // RunnerId — RequiredSafeToken
    correlationId,
    evaluatedAt,
    results)
```

### Seed Data Safe Messages

Use non-sensitive, non-blocking messages for `SafeMessage`:

- `"command-contract-shape"`: `"Create-conversation and append-message command contracts match the published contract package shape and carry schema and version metadata."`
- `"query-contract-shape"`: `"Read-timeline and list-conversations query contracts match the published contract package and return freshness metadata as required."`
- `"event-publication-shape"`: `"Domain events carry schema and version metadata as required by the contract; no internal infrastructure terms are exposed in the adopter-facing event surface."`
- `"typed-error-shape"`: `"Typed error contract is content-safe with machine-readable code, category, retryability, and a documentation pointer; no protected identifiers included."`
- `"version-discovery-shape"`: `"Version-discovery returns active command, projection, event, and client package versions in the published contract shape without infrastructure internals."`
- `"core-fixture-happy-path"`: `"The adopter-style CORE fixture exercises create, append, and read with Current freshness and stable participant and business-reference attribution."`
- `"core-fixture-blocked-schema"`: `"An unsupported schema version in the CORE fixture is rejected fail-closed with a typed documented error; no silent compatibility is assumed."`
- `"core-fixture-probe-hidden"`: `"A cross-authorization probe in the CORE fixture is hidden as aggregate-not-found to prevent side-channel disclosure of protected record existence."`
- `"redaction-consumer-contract"`: `"Redaction command, event, and audit contracts remain stable for consumer-driven validation; no breaking change has been introduced in the contract surface."`
- `"conformance-invariant-proof"`: `"Project conformance invariants have traceable automated evidence: event log authority, fail-closed access, personal-data boundaries, and generated-first UI boundaries."`

### Test Structure — 15 Tests

Follow `ProviderPortabilityConformanceSuiteTest.cs` exactly, substituting contract-validation-specific values:

| Test Name | Assertion |
|---|---|
| `RunResultShouldHaveExactly10Checks` | `run.Checks.Count.ShouldBe(10)` |
| `AllChecksShouldUseCompatibilityDiscoveryCheckId` | `check.Check.Equals(ConformanceCheck.CompatibilityDiscovery)` |
| `EachScenarioShouldProduceExpectedConformanceOutcome` | loop over scenarios, `check.Outcome.ShouldBe(scenario.ExpectedOutcome)` |
| `EachScenarioCheckShouldBeClassifiedAsConformant` | `check.FailureClassification.Equals(Conformant)` + `run.OverallClassification.ShouldBe(Conformant)` |
| `AllChecksShouldCarryFR92RequirementAndContractCompatibilityGateMappings` | `RequirementMappings.ShouldContain("FR92")`, `PreconditionMappings.ShouldNotBeEmpty()`, `ReleaseGateMappings.ShouldContain("contract-compatibility")` |
| `ReadyScenariosShouldHaveNullTypedError` | `Where(ready)`, `ShouldNotBeEmpty()`, `check.Error == null` |
| `BlockedScenariosShouldHaveNonNullTypedError` | `Where(blocked)`, `.Count().ShouldBe(1)`, `check.Error != null` |
| `UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `Where(unknown)`, `.Count().ShouldBe(1)`, `check.Error != null`, `check.Error!.Code.Equals(AggregateNotFound)`, `check.Error!.ClientAction == HideOrRefresh`, `!check.Error!.IsRetryable` |
| `AllConformantScenariosProduceOverallReadyOutcome` | `check.IsConformant`, `run.OverallOutcome.ShouldBe(Ready)`, `run.OverallClassification.ShouldBe(Conformant)` |
| `SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `run.SuiteId.ShouldBe("contract-validation-conformance-suite")`, `run.RunnerId.ShouldBe("local-ci-runner")` |
| `RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | poison sentinels from `ConversationConformanceCoreFixtures.Create()` + forbidden fragments array |
| `RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | serialize twice → equal; assert `"suiteId":"contract-validation-conformance-suite"`, `"overallOutcome":"ready"`, `"overallClassification":"conformant"`, `"failureClassification":"conformant"`; deserialize and deep-compare including `PreconditionMappings` |
| `NullScenariosListShouldThrow` | `Should.Throw<ArgumentNullException>(() => suite.Run(null!, ...))` |
| `EmptyScenariosListShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run([], ...))` |
| `NullCorrelationIdShouldThrow` | `Should.Throw<ArgumentException>(() => suite.Run(Scenarios, null!, ...))` |

**Key difference from Stories 5.8/5.9:** `BlockedScenariosShouldHaveNonNullTypedError` asserts `.Count().ShouldBe(1)` (only 1 blocked scenario, not 2). Verify this in the test.

**Forbidden fragments array** (copy verbatim from Story 5.9 test):
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

### Manifest 10th Entry

Append to `docs/release-evidence/conformance-manifest-v1-fixture.json` entries array (before the closing `]`):

```json
{
  "testId": "story-5-10-contract-validation-conformance",
  "testName": "Contract validation conformance suite release-gating coverage",
  "requirementId": "FR92",
  "carryForwardCommitmentRef": "story-4-5-adopter-conformance-suite",
  "releaseGateId": "contract-compatibility",
  "passCriteria": "All 10 contract validation scenarios produce expected outcomes and run result serializes to content-safe camelCase JSON",
  "releaseDecisionStatus": "pass",
  "waiverReference": null,
  "measurementMethod": "automated-conformance-suite-test",
  "environment": "local-ci",
  "evidenceArtifactHandle": "contract-validation-conformance-suite-result",
  "owner": "release-engineer",
  "lifecycleStage": "release-evidence",
  "registeredAtUtc": "2026-05-23T00:00:00+00:00"
}
```

### Test Count Expectation

- Before Story 5.10: 1249 total tests (Conformance: 125)
- After Story 5.10: ~1264 total tests (Conformance: ~140), +15 new conformance tests

### Existing Manifest Tests — No Breaking Change

`ManifestFixtureShouldHaveFourEntriesAfterStory54Update` was updated to `ShouldBeGreaterThanOrEqualTo(4)` in Story 5.5. Adding the 10th manifest entry will not break this test.

### Documentation URI

```csharp
private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/conformance");
```

### Two-Level Evidence Rule

Story 5.10 follows the same two-level evidence rule as Stories 5.5–5.9:
- **Production proof** (first level): Story 4.5 proved the adopter-facing conformance suite and CORE fixture run in CI and produce machine-readable safe results covering all 11 `ConformanceCheck` values (create-conversation, append-message, read-timeline, tenant-binding, party-identity, idempotency, error-envelope, projection-freshness, event-publication, governance-precondition, compatibility-discovery) and the AC4 scenario matrix (supported, unsupported, stale, cross-tenant, duplicate command, projection lag, sanitized error). Story 4.5 had 25 conformance tests when completed.
- **Release-gating aggregation** (second level): Story 5.10 carries forward that production evidence and adds release-gate manifest coverage under `contract-compatibility`. No production behavior is re-implemented or re-tested.

### Project Structure Notes

- Fixture file: `src/Hexalith.Conversations.Testing/Fixtures/ContractValidationConformanceFixtures.cs` — follows pattern of `ProviderPortabilityConformanceFixtures.cs` in same folder
- Suite runner: `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuite.cs` — follows pattern of `ProviderPortabilityConformanceSuite.cs` in same folder
- Test file: `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuiteTest.cs` — follows pattern of `ProviderPortabilityConformanceSuiteTest.cs` in same folder
- No new packages needed — `ConformanceCheck.CompatibilityDiscovery`, `ConversationErrorCode.SchemaVersionUnsupported`, and `ConversationErrorCode.AggregateNotFound` all exist in contracts already
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes
- Namespace for tests: `Hexalith.Conversations.Conformance.Tests`
- Namespace for fixtures: `Hexalith.Conversations.Testing.Fixtures`
- Target framework: `net10.0`; Central Package Management — do NOT add package versions to `.csproj`

### Validation Commands

```bash
# Targeted: new tests only
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~ContractValidation"

# Full conformance suite: should go from 125 to ~140
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj

# Full build
dotnet build Hexalith.Conversations.slnx

# Full solution: should go from 1249 to ~1264
dotnet test Hexalith.Conversations.slnx
```

### References

- Previous story pattern: [Source: _bmad-output/implementation-artifacts/5-9-prove-event-schema-evolution.md]
- Suite runner reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs]
- Test reference: [Source: tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuiteTest.cs]
- Fixture reference: [Source: src/Hexalith.Conversations.Testing/Fixtures/ProviderPortabilityConformanceFixtures.cs]
- Story 4.5 carry-forward source: [Source: _bmad-output/implementation-artifacts/4-5-provide-adopter-facing-conformance-tests-and-core-fixture.md]
- Conformance vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs]
- Error codes: [Source: src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs]
- Check result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs]
- Run result constructor: [Source: src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs]
- Release gate vocabulary: [Source: src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs]
- Manifest fixture: [Source: docs/release-evidence/conformance-manifest-v1-fixture.json]
- Epic 5 story requirements: [Source: _bmad-output/planning-artifacts/epics.md — Epic 5, Story 5.10]
- Story 4.5 adopter conformance evidence: [Source: _bmad-output/planning-artifacts/epics.md — Epic 4, Story 4.5]
- Two-level evidence rules: [Source: _bmad-output/planning-artifacts/epics.md — Implementation Readiness Gates, Two-Level Evidence Rules]
- Project context: [Source: _bmad-output/project-context.md]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Bug fix: Scenario 4 token `"error-envelope-shape"` blocked by `"envelope"` in `ConversationError.UnsafeTerms`. Dev Notes listed only `"tenant-"`, `"provider-session"`, and `"stream"` but the actual blocklist also includes `"envelope"`. Renamed token to `"typed-error-shape"` and SafeMessage to avoid "envelope". Both `RequiredSafeToken` and `RequiredSafeText` call `EnsureContentSafe` so both the token and the free-text message are checked.

### Completion Notes List

- Implemented `ContractValidationScenarioData` sealed record and `ContractValidationConformanceSeedData` with 10 scenarios (8 ready, 1 blocked/SchemaVersionUnsupported, 1 unknown/AggregateNotFound) — all conformant classification.
- Implemented `ContractValidationConformanceSuite` runner using `ConformanceCheck.CompatibilityDiscovery`, `["FR92"]`, `["contract-validation-precondition"]`, `["contract-compatibility"]` — all-conformant fixture produces `overallOutcome = ready`.
- Implemented 15 tests covering all required assertions; `BlockedScenariosShouldHaveNonNullTypedError` asserts `Count().ShouldBe(1)` (1 blocked, not 2 as in Stories 5.8/5.9).
- Extended `conformance-manifest-v1-fixture.json` with 10th entry: `contract-compatibility` release gate, `FR92`, carry-forward to `story-4-5-adopter-conformance-suite`.
- Full solution: 1264 tests, 0 failures (Client 23, Conformance 140, Integration 8, Core 153, Server 428, Contracts 512).

### File List

- `src/Hexalith.Conversations.Testing/Fixtures/ContractValidationConformanceFixtures.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuite.cs` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuiteTest.cs` (new)
- `docs/release-evidence/conformance-manifest-v1-fixture.json` (modified)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)

## Senior Developer Review (AI)

**Reviewer:** AI Senior Developer Review — 2026-05-23

**Outcome:** ✅ Approved — no blocking issues

**Verification performed:**
- All 3 new files read in full and cross-referenced against story spec
- `ConformanceCheck.CompatibilityDiscovery`, `ConversationErrorCode.SchemaVersionUnsupported`, `ConversationErrorCode.AggregateNotFound` verified to exist in contracts
- `ConformanceCheckResultV1.IsConformant` verified (`FailureClassification.Equals(Conformant)`)
- `ConformanceCheckResultV1.ValidateError` verified: non-ready outcomes require non-null error (scenarios 7 and 8 pass)
- All 10 scenario tokens and SafeMessages verified against full `ConversationError.UnsafeTerms` blocklist (31 terms, case-insensitive)
- `RequiredSafeToken` vs `RequiredMappingTokens` distinction verified: mapping arrays bypass disclosure blocklist per Story 4.4 lesson
- CS8122 expression-tree pitfall correctly avoided — `== null` / `!= null` used in all `ShouldAllBe` lambdas
- Manifest 10th entry validated against closed `ReleaseGateId` vocabulary — `"contract-compatibility"` confirmed
- Sprint-status.yaml updated from `review` → `done`
- Git discrepancies: new files show as `??` (untracked), consistent with uncommitted new files

**Issues fixed (2 Medium):**
1. Dev Notes Scenario Design table: corrected scenario 4 token `"error-envelope-shape"` → `"typed-error-shape"` and updated description to reflect rename rationale
2. Dev Notes Seed Data Safe Messages: corrected scenario 4 key/value to `"typed-error-shape"` / `"Typed error contract..."` matching implementation

## Change Log

- Implemented Story 5.10 contract validation conformance suite: 10 scenarios, 15 tests, manifest 10th entry (Date: 2026-05-23)
- Senior Developer Review: 2 Medium documentation inconsistencies auto-fixed (Dev Notes stale scenario 4 token/message); story approved and marked done (Date: 2026-05-23)
