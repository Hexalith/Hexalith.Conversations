# Story 5.4: Support Named Waivers for Release-Gate Exceptions

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release approver,
I want a named-waiver process for release-gate exceptions,
so that accepted risks are explicit, owned, time-bound, and visible to buyers where needed.

## Acceptance Criteria

1. Named waiver records all required governance fields
   - Given a release gate is not green,
   - When a waiver is requested,
   - Then the waiver records owner, approver, affected requirement or gate, affected stories, risk, compensating control, expiry date, buyer impact, buyer acceptance status where customer-facing, evidence links, and review date,
   - And automatic release blockers cannot be waived without explicit named approval.

2. Waiver lifecycle states are distinguishable in release evidence
   - Given a waiver is active, expired, rejected, or superseded,
   - When release evidence is generated,
   - Then the conformance artifact and admin evidence views distinguish pass, fail, waived, unknown-accepted, expired waiver, and blocker states,
   - And stale or unexplained waivers are treated as findings.

3. Waiver validation covers all governance traceability scenarios
   - Given waiver tests run,
   - When active waiver, expired waiver, missing approver, missing compensating control, blocker waiver, buyer-facing waiver, and waiver review scenarios are exercised,
   - Then tests prove governance traceability, release decision clarity, and content-safe evidence output.

## Tasks / Subtasks

- [x] Confirm scope and existing infrastructure before editing (AC: 1-3)
  - [x] Honor the Two-Level Evidence semantics gate: Story 5.4 defines the waiver contract type, lifecycle vocabulary, validator, schema document, and a synthetic fixture. It does NOT implement runtime waiver approval workflows, database storage, background workers, admin UI surfaces, or waiver aggregation for Stories 5.5-5.11. [Source: `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`; `_bmad-output/implementation-artifacts/readiness-gates.md`]
  - [x] Reuse the existing conformance vocabulary and validation infrastructure: `ConformanceContractValidation.RequiredSafeToken`, `RequiredSafeText`, `RequiredUtcTimestamp`, `OptionalSafeToken` from `ConformanceContractValidation.cs`; the sealed-record closed-vocabulary pattern from `ConformanceVocabulary.cs`; `ReleaseGateId` from `ReleaseGateStatus.cs`; and the `ConversationStringValueJsonConverter<T>` base from `ClosedVocabularyJsonConverters.cs`. Do not reinvent or duplicate these patterns. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`; `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs`]
  - [x] Confirm `src/Hexalith.Conversations.Conformance/` does NOT exist (same check as Story 5.3). If it does NOT exist, builder/seeder code stays in tests. Contract types always go in `src/Hexalith.Conversations.Contracts/Conformance/`. [Source: `_bmad-output/implementation-artifacts/5-3-maintain-versioned-conformance-manifest-with-traceability.md#Project Structure Decision`]
  - [x] Do not introduce new public conformance outcome values, trust/freshness states, or error codes without an ADR. The `WaiverLifecycleStatus` type is new but is specific to the waiver entity, not a change to `ConformanceOutcome`, `ReleaseGateStatus`, or `ConformanceCheck` vocabularies. Stop for an ADR if the implementation requires modifying any existing closed vocabulary. [Source: `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`; `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]
  - [x] Verify current test count from `_bmad-output/implementation-artifacts/tests/test-summary.md` (1124 after Story 5.3 review fixes) to track regression baseline.

- [x] Define the waiver lifecycle status vocabulary (AC: 1, 2)
  - [x] Add `WaiverLifecycleStatus` as a new closed vocabulary type in `src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs` following the `ReleaseGateStatus` sealed-record pattern (sealed record, private constructor, four static properties, `All`, `Parse`, JSON converter). Values:
    - `"active"` — waiver is currently valid and approved
    - `"expired"` — waiver has passed its ExpiryDateUtc and is a finding
    - `"rejected"` — waiver request was explicitly denied by an approver
    - `"superseded"` — waiver was replaced by a newer named waiver
  - [x] Add computed properties:
    - `IsActive` → true only when `Equals(Active)`
    - `IsStale` → true when `Equals(Expired) || Equals(Superseded)` (stale waivers are findings per AC2)
  - [x] Add `WaiverLifecycleStatusJsonConverter` to `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` using `ConversationStringValueJsonConverter<WaiverLifecycleStatus>` as base, following the `ReleaseGateStatusJsonConverter` pattern exactly. [Source: `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`]

- [x] Define the ReleaseWaiverV1 record (AC: 1, 2)
  - [x] Add `ReleaseWaiverV1` as a sealed record in `src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs` with these validated fields:
    - `string WaiverId` — stable bounded machine-readable identifier (`RequiredSafeToken`)
    - `string Owner` — bounded owner identifier (`RequiredSafeToken`)
    - `string? Approver` — optional bounded approver identifier (`OptionalSafeToken`); null valid at construction, but `ValidateWaiver` enforces non-null when `IsBlocker=true`
    - `string AffectedRequirementId` — FR or NFR identifier such as `"FR87"` or `"NFR62"` (`RequiredSafeToken`)
    - `ReleaseGateId? AffectedGateId` — optional release gate; null valid when waiver applies to a non-gate requirement
    - `IReadOnlyList<string> AffectedStoryIds` — non-empty list of bounded safe tokens (story key identifiers); validated like `ConformanceManifestChangeV1.AffectedRequirementIds`
    - `bool IsBlocker` — explicit flag indicating this waiver covers an automatic release blocker (NFR62 categories: tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, contract compatibility)
    - `string Risk` — bounded risk description (`RequiredSafeText`)
    - `string CompensatingControl` — bounded compensating control description (`RequiredSafeText`)
    - `DateTimeOffset ExpiryDateUtc` — UTC expiry date (`RequiredUtcTimestamp`); must be future relative to creation in practice but construction does not enforce this (validator does)
    - `string BuyerImpact` — bounded buyer impact description (`RequiredSafeText`)
    - `string? BuyerAcceptanceStatus` — nullable bounded token (`OptionalSafeToken`); null means not customer-facing; token values such as `"buyer-accepted"`, `"buyer-pending"`, `"not-applicable"` are the expected safe tokens
    - `IReadOnlyList<string> EvidenceLinks` — bounded list of safe token evidence artifact handles; empty list allowed (null not allowed); each non-null element validated as safe token
    - `DateTimeOffset ReviewDateUtc` — UTC review date (`RequiredUtcTimestamp`); when past, `ValidateWaiver` emits `"stale-review-date"`
    - `WaiverLifecycleStatus LifecycleStatus` — required lifecycle status
    - `DateTimeOffset CreatedAtUtc` — UTC creation timestamp (`RequiredUtcTimestamp`)
  - [x] Validate `AffectedStoryIds` non-empty, no null or whitespace, each element passes `RequiredSafeToken`. Follow the `ValidateAffectedIds` helper pattern from `ConformanceManifestChangeV1` — implement a private static `ValidateAffectedStoryIds` method in the record. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs`]
  - [x] Validate `EvidenceLinks` null-guard: null throws `ArgumentNullException`; each non-null element in the list validated as safe token via `OptionalSafeToken`. Use a private static helper `ValidateEvidenceLinks`.

- [x] Add waiver validation helper (AC: 1, 2, 3)
  - [x] Add `ReleaseWaiverValidator` as a static class in `src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs` with a `ValidateWaiver(ReleaseWaiverV1 waiver, DateTimeOffset evaluatedAt)` method returning `IReadOnlyList<string>` of content-safe typed error tokens. The `evaluatedAt` parameter enables deterministic testing without `DateTime.UtcNow`. Validate:
    - `"blocker-requires-approver"`: when `waiver.IsBlocker && waiver.Approver is null`
    - `"expired-waiver"`: when `waiver.ExpiryDateUtc < evaluatedAt` (regardless of lifecycle status; an active waiver with a past expiry is still expired)
    - `"stale-review-date"`: when `waiver.ReviewDateUtc < evaluatedAt`
    - `"buyer-facing-missing-acceptance"`: when `waiver.IsBlocker && waiver.AffectedGateId is not null && waiver.BuyerAcceptanceStatus is null`
    - Return empty list for a valid waiver
  - [x] All error tokens must be content-safe: never include caller-supplied free text or bounded field data that could expose protected identifiers. Tokens are literal strings like `"blocker-requires-approver"`. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs#ValidateArtifact`]

- [x] Author the waiver schema document (AC: 1, 3)
  - [x] Create `docs/release-evidence/waiver.schema.json` as a structured human-navigable JSON specification (not a formal JSON Schema dialect), following the `manifest.schema.json` spirit from Story 5.3. Include sections: `schemaVersion`, `title`, `description`, `fields` (array of `{name, type, required, validation, description}` objects for each `ReleaseWaiverV1` field), `validationRules` (array of `{ruleName, description, errorToken}` objects matching `ValidateWaiver` checks), and `exampleRecord` (a sample serialized `ReleaseWaiverV1`). This document must be navigable by non-developer release approvers per NFR68. [Source: `docs/release-evidence/manifest.schema.json`; `_bmad-output/planning-artifacts/prd.md#NFR68`]

- [x] Create and commit the synthetic waiver fixture file (AC: 1, 2, 3)
  - [x] Create `docs/release-evidence/release-waiver-v1-fixture.json` as a deterministic synthetic fixture waiver representing an active named waiver. Use these values:
    - `waiverId`: `"waiver-story-5-4-named-waiver-process"`
    - `owner`: `"release-engineer"`
    - `approver`: `"release-approver"`
    - `affectedRequirementId`: `"FR85"`
    - `affectedGateId`: null (this waiver is not gate-specific; it covers the named-waiver process itself)
    - `affectedStoryIds`: `["5-4-support-named-waivers-for-release-gate-exceptions"]`
    - `isBlocker`: false (the named-waiver process story is not a release blocker gate)
    - `risk`: `"Named waiver process documentation may need iteration before GA"`
    - `compensatingControl`: `"Waiver schema document and fixture provide navigable governance evidence for release approvers"`
    - `expiryDateUtc`: `"2027-01-01T00:00:00+00:00"` (future relative to 2026-05-23)
    - `buyerImpact`: `"Buyer can review named waivers through release evidence documents"`
    - `buyerAcceptanceStatus`: null (not customer-facing at this stage)
    - `evidenceLinks`: `["release-waiver-v1-fixture", "waiver-schema-doc"]`
    - `reviewDateUtc`: `"2026-12-01T00:00:00+00:00"` (future; periodic review before expiry)
    - `lifecycleStatus`: `"active"`
    - `createdAtUtc`: `"2026-05-23T00:00:00+00:00"`
  - [x] The fixture must be content-safe: no real tenant IDs, Party IDs, conversation IDs, local paths, or raw exceptions. [Source: `_bmad-output/project-context.md#Testing Rules`]
  - [x] Update `docs/release-evidence/conformance-manifest-v1-fixture.json` to add a fourth entry for Story 5.4:
    - `testId`: `"story-5-4-named-waiver-process"`
    - `testName`: `"Named waiver contract type validation and governance traceability"`
    - `requirementId`: `"FR85"`
    - `carryForwardCommitmentRef`: null
    - `releaseGateId`: null
    - `passCriteria`: `"Waiver contract type validates with zero errors and fixture proves governance traceability"`
    - `releaseDecisionStatus`: `"pass"`
    - `waiverReference`: null
    - `measurementMethod`: `"automated-contract-validation-test"`
    - `environment`: `"local-ci"`
    - `evidenceArtifactHandle`: `"release-waiver-v1-fixture"`
    - `owner`: `"release-engineer"`
    - `lifecycleStage`: `"release-evidence"`
    - `registeredAtUtc`: `"2026-05-23T00:00:00+00:00"`

- [x] Write contract and validation tests (AC: 1-3)
  - [x] Add `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` covering:
    - `WaiverLifecycleStatus` closed-vocabulary completeness (4 values: active, expired, rejected, superseded), JSON rejection of synonyms (`"valid"`, `"invalid"`, `"cancelled"`, `"done"`), `Parse` round-trips for all 4 values
    - `WaiverLifecycleStatus.IsActive`: true only for `active`, false for expired/rejected/superseded
    - `WaiverLifecycleStatus.IsStale`: true for expired and superseded, false for active and rejected
    - `ReleaseWaiverV1` construction-time validation (all 16 fields)
    - `ReleaseWaiverValidator.ValidateWaiver` (all 4 error tokens exercised)
    - Stable camelCase web JSON shape, round-trip, additive-JSON tolerance
    - Fixture file validation (existence, deserialization, zero errors, content-safety)
    - Manifest fixture has 4 entries
  [Source: `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs`; `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs`]
  - [x] Register `WaiverLifecycleStatus` and `ReleaseWaiverV1` samples in `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` so existing serialization, forbidden-surface, and content-safety scans cover the new types automatically. Use future dates for timestamps in the sample to avoid accidental expiry. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]
  - [x] Add `tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` covering:
    - The fixture waiver passes `ValidateWaiver` with zero errors (use `evaluatedAt = new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero)` — all fixture dates are future relative to this)
    - Waiver with `IsBlocker=true` and null `Approver` returns `"blocker-requires-approver"` error
    - Waiver with past `ExpiryDateUtc` returns `"expired-waiver"` error
    - Waiver with past `ReviewDateUtc` returns `"stale-review-date"` error
    - Fixture content-safety scan (no local paths, no EventStore tokens, no raw exceptions)
    - Waiver serializes to stable camelCase JSON and round-trips deterministically
    - `WaiverLifecycleStatus.All` returns exactly 4 values
  [Source: `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs`]

- [x] Update local evidence and run validation (AC: 1-3)
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 5.4 evidence: new type paths, new test paths, targeted test results, full solution results.
  - [x] Run targeted tests first:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ReleaseWaiver|FullyQualifiedName~WaiverLifecycle"` — 42 passed
    - `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` — 50 passed (43 existing + 7 new)
  - [x] Run full solution validation:
    - `dotnet build Hexalith.Conversations.slnx` — succeeded, 0 warnings, 0 errors
    - `dotnet test Hexalith.Conversations.slnx` — 1173 total, 0 failures (1124 baseline + 49 new)
  - [x] Confirm no test, docs check, or setup step requires nested submodule initialization. [Source: `AGENTS.md`; `_bmad-output/project-context.md#Development Workflow Rules`]

- [x] Preserve story boundaries and stop conditions (AC: 1-3)
  - [x] Do not implement runtime waiver approval workflows, database storage for waivers, admin UI surfaces for waiver management, or background workers.
  - [x] Do not implement waiver aggregation or cross-story waiver status tracking for Stories 5.5-5.11.
  - [x] Do not implement Story 5.5-5.9 per-domain conformance suites.
  - [x] Do not modify `ReleaseGateStatus`, `ConformanceOutcome`, or `ConformanceCheck` closed vocabularies; the `WaiverLifecycleStatus` is a new entity-specific vocabulary, not a modification of existing conformance gate vocabularies.
  - [x] Do not add a new public package, CLI tool, or globally runnable host.
  - [x] Stop for ADR if implementation needs a new conformance outcome value added to existing closed vocabularies, or if waiver storage requires a durable store.

## Dev Notes

### Epic and Business Context

- Epic 5 is the release-owner layer. Story 5.4 is the named-waiver process: defining the contract type, lifecycle vocabulary, and validator that makes waivers explicit, owned, time-bound, and visible. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`]
- FR85: The product can support a named-waiver process for release-gate exceptions. FR86: The product can classify verification and release-gate failures as blocking or non-blocking. [Source: `_bmad-output/planning-artifacts/prd.md#FR85`; `_bmad-output/planning-artifacts/prd.md#FR86`]
- NFR62 is the binding constraint for automatic release blockers: "Tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, and contract breakage are automatic release blockers unless explicitly waived through the named-waiver process." The `IsBlocker` field in `ReleaseWaiverV1` maps directly to this NFR. [Source: `_bmad-output/planning-artifacts/prd.md#NFR62`]
- NFR63: every release must produce a signed conformance artifact and versioned manifest. Story 5.4's waiver fixture integrates with the manifest fixture by providing the waiver record that Story 5.3's `WaiverReference` field points to. [Source: `_bmad-output/planning-artifacts/prd.md#NFR63`]
- NFR68: release and conformance evidence must be navigable by non-developer approvers. `waiver.schema.json` serves this requirement for the waiver entity. [Source: `_bmad-output/planning-artifacts/prd.md#NFR68`]

### Existing Surfaces to Reuse

- `ConformanceVocabulary.cs` — `ConformanceCheck` and `ConformanceOutcome` show the exact sealed-record, private-constructor, static-factory, `All`, `Parse`, `JsonConverter` pattern that `WaiverLifecycleStatus` must follow exactly. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`]
- `ReleaseGateStatus.cs` — `ReleaseGateStatus` (4 values with `IsBlocking`) and `ReleaseGateId` (7 gate IDs). `ReleaseWaiverV1.AffectedGateId` reuses `ReleaseGateId` directly — no new gate ID needed. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs`]
- `ConformanceContractValidation.cs` — provides `RequiredSafeToken`, `RequiredSafeText`, `RequiredUtcTimestamp`, `OptionalSafeToken`. All new record types must use these at construction time. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`]
- `ReleaseConformanceArtifactV1.cs` — `ValidateArtifact` static method returning `IReadOnlyList<string>` of content-safe token errors is the exact pattern for `ReleaseWaiverValidator.ValidateWaiver`. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs`]
- `ConformanceManifestV1.cs` — `ConformanceManifestChangeV1.ValidateAffectedIds` private helper is the model for `ReleaseWaiverV1.ValidateAffectedStoryIds`. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs`]
- `ClosedVocabularyJsonConverters.cs` — `ReleaseGateStatusJsonConverter` uses `ConversationStringValueJsonConverter<T>` as base; add `WaiverLifecycleStatusJsonConverter` the same way. [Source: `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`]
- `ContractSamples.cs` — participates in serialization, forbidden-surface, and content-safety scans. Register all new waiver types to get free scan coverage without writing additional scan tests. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]
- `AppContext.BaseDirectory`-based repo-root path — the established pattern for deterministic test file reads. The waiver fixture and the updated manifest fixture tests must use the same `FindRepositoryRoot()` helper pattern. [Source: `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs`]
- `docs/release-evidence/` — already contains `contract-compatibility-policy.md`, `release-conformance-artifact-v1-fixture.json`, `manifest.schema.json`, and `conformance-manifest-v1-fixture.json`. The waiver schema document and waiver fixture JSON belong there. [Source: `docs/release-evidence/`]

### WaiverLifecycleStatus Design

Exactly four values — no more, no less:

| Value | Description | IsActive | IsStale |
|---|---|---|---|
| `active` | Waiver is currently valid and approved | true | false |
| `expired` | Past ExpiryDateUtc; treated as a finding | false | true |
| `rejected` | Waiver request was denied by approver | false | false |
| `superseded` | Replaced by a newer named waiver | false | true |

`IsStale` is true for `expired` and `superseded` because both represent outdated waivers that may affect gate status without being `active`. `rejected` waivers are not stale — they were never accepted.

JSON rejection test must prove synonyms like `"valid"`, `"invalid"`, `"cancelled"`, `"done"`, `"pending"` are all invalid.

### ReleaseWaiverV1 Field Summary

```
WaiverId:              RequiredSafeToken   — e.g. "waiver-story-5-4-named-waiver-process"
Owner:                 RequiredSafeToken   — e.g. "release-engineer"
Approver:              OptionalSafeToken   — null OK at construction; ValidateWaiver enforces non-null for IsBlocker=true
AffectedRequirementId: RequiredSafeToken   — e.g. "FR87" or "NFR62"
AffectedGateId:        ReleaseGateId?      — null when waiver is not gate-specific
AffectedStoryIds:      IReadOnlyList<string> — non-empty; each element RequiredSafeToken; no null elements
IsBlocker:             bool                — true = automatic release blocker (NFR62 categories)
Risk:                  RequiredSafeText    — bounded risk description
CompensatingControl:   RequiredSafeText    — bounded compensating control description
ExpiryDateUtc:         RequiredUtcTimestamp — UTC expiry; ValidateWaiver flags past expiry
BuyerImpact:           RequiredSafeText    — bounded buyer impact description
BuyerAcceptanceStatus: OptionalSafeToken   — null = not customer-facing
EvidenceLinks:         IReadOnlyList<string> — non-null; empty OK; each element RequiredSafeToken
ReviewDateUtc:         RequiredUtcTimestamp — UTC review date; ValidateWaiver flags past review
LifecycleStatus:       WaiverLifecycleStatus — required
CreatedAtUtc:          RequiredUtcTimestamp — UTC creation timestamp
```

### ValidateWaiver Error Tokens

| Token | Condition |
|---|---|
| `"blocker-requires-approver"` | `IsBlocker=true` AND `Approver is null` |
| `"expired-waiver"` | `ExpiryDateUtc < evaluatedAt` |
| `"stale-review-date"` | `ReviewDateUtc < evaluatedAt` |
| `"buyer-facing-missing-acceptance"` | `IsBlocker=true` AND `AffectedGateId is not null` AND `BuyerAcceptanceStatus is null` |

All tokens are literal strings. Never append field values from the waiver record to the token string.

### waiver.schema.json Approach

Follow `manifest.schema.json` structure:

```json
{
  "schemaVersion": "1",
  "title": "Hexalith.Conversations Release Waiver Schema",
  "description": "...",
  "fields": [
    { "name": "waiverId", "type": "string", "required": true, "validation": "safe-token", "description": "..." },
    ...
  ],
  "validationRules": [
    { "ruleName": "blocker-requires-approver", "description": "...", "errorToken": "blocker-requires-approver" },
    { "ruleName": "expired-waiver", "description": "...", "errorToken": "expired-waiver" },
    { "ruleName": "stale-review-date", "description": "...", "errorToken": "stale-review-date" },
    { "ruleName": "buyer-facing-missing-acceptance", "description": "...", "errorToken": "buyer-facing-missing-acceptance" }
  ],
  "exampleRecord": { ... }
}
```

The descriptions, field descriptions, and validation rule descriptions must be content-safe.

### ValidateWaiver Signature — Testability

Use `DateTimeOffset evaluatedAt` as an explicit parameter instead of `DateTimeOffset.UtcNow`. This pattern ensures deterministic expiry tests without mocking or clock injection.

Example:
```csharp
public static IReadOnlyList<string> ValidateWaiver(ReleaseWaiverV1 waiver, DateTimeOffset evaluatedAt)
```

In tests, use `new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero)` as the evaluation anchor for current-time tests, and `new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)` for tests that need dates to appear expired.

### Testing Requirements

- Primary test surface: contract-level tests for the lifecycle vocabulary, `ReleaseWaiverV1` construction, `ValidateWaiver`, JSON shape, round-trip, additive tolerance, and fixture file validation.
- Secondary surface: conformance-project validation tests proving the fixture waiver passes validation with zero errors, each validator error is exercisable, and content-safety holds.
- Use xUnit v3, Shouldly, deterministic synthetic fixtures, `AppContext.BaseDirectory`-based repo-root file reads, and explicit `DateTimeOffset` for time-sensitive validation tests (no `DateTime.UtcNow` or `TimeProvider` injection needed since `evaluatedAt` is a parameter).
- Content-safety: register new types in `ContractSamples.cs`; do not introduce real tenant IDs, Party IDs, conversation IDs, local paths, or raw exceptions in test data or fixture files.
- Targeted test filter: `FullyQualifiedName~ReleaseWaiver|FullyQualifiedName~WaiverLifecycle`.
- Run full `dotnet test tests/Hexalith.Conversations.Conformance.Tests` after changes to catch regressions in existing Story 5.2 and 5.3 tests.
- The updated `conformance-manifest-v1-fixture.json` (now 4 entries) must still pass the existing `FixtureManifestShouldPassValidateManifestWithZeroErrors` test in `ConformanceManifestValidationTest.cs` — do not break existing conformance tests.

### Previous Story Intelligence (5.3)

- Story 5.3 established `ConformanceManifestLifecycleStage` (6 values), `ConformanceManifestRowV1` (14 fields), `ConformanceManifestChangeV1`, `ConformanceManifestV1`, `ConformanceManifestValidator`, the `docs/release-evidence/manifest.schema.json` schema document approach, and the JSON fixture file pattern. Story 5.4 follows all the same patterns: new contract types in `src/Hexalith.Conversations.Contracts/Conformance/`, JSON converter in `ClosedVocabularyJsonConverters.cs`, fixture JSON in `docs/release-evidence/`. No deviation from established pattern. [Source: `_bmad-output/implementation-artifacts/5-3-maintain-versioned-conformance-manifest-with-traceability.md`]
- Story 5.3 added 47 tests (41 contract + 6 conformance); solution total was 1124 after review fixes. Story 5.4 should add approximately 25-30 contract tests and 6-7 conformance tests; expected total is approximately 1156. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md`]
- The `ConformanceManifestRowV1.WaiverReference` field (a `string?` safe token) was designed in Story 5.3 specifically as a pointer to a `ReleaseWaiverV1` record. Story 5.4 defines the waiver record type that this reference points to. The connection is: `WaiverReference = "release-waiver-v1-fixture"` → `docs/release-evidence/release-waiver-v1-fixture.json` → `ReleaseWaiverV1` record. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs`]
- `ConformanceManifestValidator.ValidateManifest` already emits `"missing-waiver-reference"` when status is `waived` and `WaiverReference` is null. This existing validator check is a natural complement to Story 5.4: the waiver record provides the content that the reference points to.
- Story 5.3's `docs/release-evidence/conformance-manifest-v1-fixture.json` must be extended with a Story 5.4 row. The extended fixture must still pass `ValidateManifest` with zero errors (the new row has status `"pass"` and no `waiverReference` needed).
- Story 5.2 established that `ReleaseConformanceArtifactBuilder` uses an injected `TimeProvider`. The `ValidateWaiver` signature uses an explicit `DateTimeOffset evaluatedAt` parameter instead (simpler for a static helper without injection needs — consistent with Story 5.3's `ValidateManifest` which takes only the manifest).
- Recent git commits use format `feat(story-5.N): Description`. Use `feat(story-5.4): Support named waivers for release-gate exceptions`.

### Architecture Guardrails

- New waiver types belong in `src/Hexalith.Conversations.Contracts/Conformance/` to stay infrastructure-free and adopter-stable. No EventStore envelopes, Dapr actors, HTTP clients, server infrastructure, or UI shell references are permitted in Contracts. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Public contracts must not expose EventStore stream names, event positions, snapshots, projection topology, raw exception text, test framework namespaces, server routes, local paths, or unsafe free-text. All free-text fields must pass the forbidden-surface scan. [Source: `_bmad-output/planning-artifacts/architecture.md#API Pattern`]
- Central Package Management is active. Do not add package versions directly to `.csproj` files. [Source: `Directory.Packages.props`]
- SDK `10.0.300`, target `net10.0`, nullable enabled, implicit usings, warnings as errors. [Source: `global.json`; `Directory.Build.props`]
- `WaiverLifecycleStatus` is a NEW entity-specific closed vocabulary — it does NOT modify `ReleaseGateStatus`, `ConformanceOutcome`, or `ConformanceCheck`. Do not extend any existing closed vocabulary without an ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`]

### Out of Scope

- Runtime waiver approval workflows, admin UI for waiver management, database storage for waivers
- Waiver aggregation or cross-story waiver status tracking for Stories 5.5-5.11
- Full adversarial tenant isolation conformance suite (Story 5.5)
- Full idempotency conformance suite (Story 5.6)
- Full redaction replay conformance suite (Story 5.7)
- Provider portability proof (Story 5.8), event schema evolution proof (Story 5.9)
- Aggregated contract test manifest rows, CORE fixture manifest rows (Story 5.10)
- Module-vs-platform evidence separation (Story 5.11)
- Modifications to `ReleaseGateStatus`, `ConformanceOutcome`, or `ConformanceCheck` closed vocabularies
- PKI/cryptographic signing infrastructure, HSM integration
- New durable waiver stores, background workers, export pipelines, CLI tools, globally runnable hosts
- New runtime tenant authorization, audit pairing, redaction replay, idempotency, projection freshness, event persistence, or client transport behavior
- Admin UI surfaces for waiver navigation or waiver review workflows

### Files Likely to Touch

- New files:
  - `src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs` — `WaiverLifecycleStatus`, `ReleaseWaiverV1`, `ReleaseWaiverValidator`
  - `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` — contract and fixture validation tests
  - `tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` — waiver validation tests
  - `docs/release-evidence/waiver.schema.json` — structured schema specification document
  - `docs/release-evidence/release-waiver-v1-fixture.json` — synthetic deterministic fixture waiver

- Update files:
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` — add `WaiverLifecycleStatusJsonConverter`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` — register `WaiverLifecycleStatus` and `ReleaseWaiverV1` samples
  - `docs/release-evidence/conformance-manifest-v1-fixture.json` — add Story 5.4 manifest row (4th entry)
  - `_bmad-output/implementation-artifacts/tests/test-summary.md` — Story 5.4 evidence entry

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`
- `_bmad-output/planning-artifacts/epics.md#Story 5.4: Support Named Waivers for Release-Gate Exceptions`
- `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`
- `_bmad-output/planning-artifacts/prd.md#FR85`
- `_bmad-output/planning-artifacts/prd.md#FR86`
- `_bmad-output/planning-artifacts/prd.md#NFR62`
- `_bmad-output/planning-artifacts/prd.md#NFR63`
- `_bmad-output/planning-artifacts/prd.md#NFR68`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/5-3-maintain-versioned-conformance-manifest-with-traceability.md`
- `_bmad-output/implementation-artifacts/5-2-generate-signed-release-conformance-artifact.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/project-context.md`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs`
- `docs/release-evidence/manifest.schema.json`
- `docs/release-evidence/conformance-manifest-v1-fixture.json`
- `docs/release-evidence/release-conformance-artifact-v1-fixture.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Story ID `"5-4-support-named-waivers-for-release-gate-exceptions"` contains "exception" which is blocked by `ConversationError.EnsureContentSafe`. Fixed by using mapping-token character validation (no disclosure blocklist) for `AffectedStoryIds` — consistent with the Story 4.4 precedent that traceability tokens must not be subjected to the free-text blocklist.
- Null-guard tests for `EvidenceLinks` and `LifecycleStatus` must call the constructor directly rather than through `BuildWaiver` helper because the helper's `??` coalesces `null!` into defaults before the constructor sees it.

### Completion Notes List

- Implemented `WaiverLifecycleStatus` (4 values: active/expired/rejected/superseded; `IsActive`, `IsStale` computed properties; sealed-record closed-vocabulary pattern) in `ReleaseWaiverV1.cs`.
- Implemented `ReleaseWaiverV1` sealed record with 16 validated fields. `AffectedStoryIds` uses mapping-token character validation (no disclosure blocklist) to allow story IDs containing "exception". `EvidenceLinks` null-guarded with `ArgumentNullException`.
- Implemented `ReleaseWaiverValidator.ValidateWaiver(waiver, evaluatedAt)` returning 4 content-safe error tokens; `evaluatedAt` parameter makes tests deterministic without clock injection.
- Added `WaiverLifecycleStatusJsonConverter` to `ClosedVocabularyJsonConverters.cs` following existing pattern exactly.
- Created `docs/release-evidence/waiver.schema.json` with 16 field specs, 4 validation rules, and example record; navigable by non-developer approvers (NFR68).
- Created `docs/release-evidence/release-waiver-v1-fixture.json` with all-future dates (expiry 2027-01-01, review 2026-12-01), passes `ValidateWaiver` with evaluatedAt=2026-05-23, content-safe.
- Extended `docs/release-evidence/conformance-manifest-v1-fixture.json` from 3 to 4 entries; existing `FixtureManifestShouldPassValidateManifestWithZeroErrors` test still passes.
- Added 42 contract tests and 7 conformance tests. Full solution: 1173 tests, 0 failures, 0 warnings.

### File List

- `src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs` (new)
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (modified — added `WaiverLifecycleStatusJsonConverter`)
- `docs/release-evidence/waiver.schema.json` (new)
- `docs/release-evidence/release-waiver-v1-fixture.json` (new)
- `docs/release-evidence/conformance-manifest-v1-fixture.json` (modified — added Story 5.4 entry; now 4 entries)
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` (new — 42 tests)
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (modified — registered `WaiverLifecycleStatus` and `ReleaseWaiverV1` samples)
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` (new — 7 tests)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified — Story 5.4 evidence)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — status updated)

### Senior Developer Review (AI)

**Date:** 2026-05-23
**Reviewer:** AI Code Review (bmad-story-automator-review)

**Git vs Story Discrepancies:** 0 (all application source changes properly documented)
**Issues Found:** 0 Critical, 0 High, 1 Medium, 1 Low — all auto-fixed

**Findings and Fixes Applied:**

🟡 MEDIUM-1 (FIXED): `WaiverLifecycleStatusShouldRejectUnknownJsonTokens` tested only one JSON synonym (`"done"`) via `JsonSerializer.Deserialize`. Story spec explicitly requires testing `"valid"`, `"invalid"`, `"cancelled"`, `"done"`. Expanded the test loop to cover all 4 synonyms and added `WaiverShouldRejectNullAffectedStoryIds` using a direct constructor call (the `BuildWaiver` helper coalesces null before the constructor sees it, so direct construction is required to reach this validation path). [`tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs`]

🟢 LOW-1 (FIXED): No test for null `AffectedStoryIds` list itself — only null elements within the list were tested. The code handles `null` correctly via `if (values is null ...)` → `ArgumentException`, but the path was unexercised. Added `WaiverShouldRejectNullAffectedStoryIds` test. [`tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs`]

**Post-fix validation:**
- Targeted: 43 contract tests passed, 0 failures (42 original + 1 new)
- Conformance: 50 passed, 0 failures — no regressions
- Full solution: 1174 total, 0 failures (1173 baseline + 1 new review-added test)

**Outcome: APPROVED — no Critical issues remain.**

## Change Log

- 2026-05-23: Story 5.4 implemented — `WaiverLifecycleStatus` (4 values), `ReleaseWaiverV1` (16 fields), `ReleaseWaiverValidator` (4 error tokens), `waiver.schema.json`, `release-waiver-v1-fixture.json`, conformance manifest extended to 4 entries, 42 contract tests + 7 conformance tests; solution total 1173 tests, 0 failures.
- 2026-05-23: Review — auto-fixed 1 medium issue (JSON synonym test coverage) and 1 low issue (null AffectedStoryIds path); added 1 test; solution total 1174 tests, 0 failures. Status → done.
