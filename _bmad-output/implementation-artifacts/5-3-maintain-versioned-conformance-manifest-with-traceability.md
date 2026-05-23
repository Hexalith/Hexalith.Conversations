# Story 5.3: Maintain Versioned Conformance Manifest with Traceability

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release owner,
I want a versioned release-specific conformance manifest,
so that every release-gate test maps to requirements and acceptance criteria.

## Acceptance Criteria

1. Each conformance test maps to full traceability metadata
   - Given a release manifest is created,
   - When conformance tests are registered,
   - Then each test maps to functional requirements, non-functional requirements, carry-forward commitments, release-gate status, pass criteria, waiver status, measurement method, environment, and evidence artifact,
   - And every FR and release-blocking NFR in scope has at least one traceable verification entry.

2. Manifest version history preserves changes and flags stale entries
   - Given a conformance test or entry changes,
   - When the manifest is updated,
   - Then version history preserves what changed, why, and which requirement or release gate is affected,
   - And stale mappings or orphan tests (referencing unknown gate IDs or missing requirement IDs) are flagged by validation.

3. Manifest validation fails with actionable diagnostics on structural errors
   - Given manifest validation runs,
   - When duplicate test IDs, missing FR mappings, missing pass criteria, missing waiver metadata where required, or untraceable evidence appears,
   - Then validation fails with content-safe actionable diagnostics,
   - And release evidence remains navigable by non-developer approvers.

4. Each manifest entry carries complete release decision traceability
   - Given a manifest entry represents a release gate or evidence obligation,
   - When the entry is authored or validated,
   - Then it includes requirement ID, gate status, evidence artifact handle, owner, lifecycle stage, release decision status, and waiver reference when applicable,
   - And decorative evidence without requirement traceability or release-decision meaning is rejected by validation.

## Tasks / Subtasks

- [x] Confirm scope and existing infrastructure before editing (AC: 1-4)
  - [x] Honor the Two-Level Evidence semantics gate: Story 5.3 defines the manifest schema, types, and validation logic, and seeds a synthetic fixture from existing story evidence; it does not generate signed GA release artifacts (5.2), manage named waivers (5.4), run per-domain proof suites (5.5-5.9), or aggregate all contract tests (5.10). [Source: `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`; `_bmad-output/implementation-artifacts/readiness-gates.md`]
  - [x] Reuse the existing conformance vocabulary: `ReleaseGateStatus`, `ReleaseGateId`, `ConformanceContractValidation`, and the sealed-record closed-vocabulary pattern from `ConformanceCheck` and `ConformanceOutcome` all already exist in `src/Hexalith.Conversations.Contracts/Conformance/`. New manifest types must follow the same pattern. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`]
  - [x] Run `ls src/` to check whether `src/Hexalith.Conversations.Conformance/` already exists. If it does NOT exist, place any new builder/seeder in `tests/Hexalith.Conversations.Conformance.Tests/` following the Story 5.2 precedent for `ReleaseConformanceArtifactBuilder.cs`. Contract types always go in `src/Hexalith.Conversations.Contracts/Conformance/` regardless. [Source: `_bmad-output/implementation-artifacts/5-2-generate-signed-release-conformance-artifact.md#Completion Notes`; `_bmad-output/planning-artifacts/architecture.md#Project Structure`]
  - [x] Do not introduce new public error codes, new public trust/freshness states, or new public conformance outcome values. Stop for an ADR if implementation requires any. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`; `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`]

- [x] Define the manifest lifecycle stage vocabulary (AC: 1, 4)
  - [x] Add `ConformanceManifestLifecycleStage` as a new closed vocabulary type to `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs` following the `ConformanceCheck` sealed-record pattern (sealed record, private constructor, six static properties, `All`, `Parse`, JSON converter). Values match NFR1 exactly: `design-review`, `automated-test`, `load-performance-test`, `operational-drill`, `release-evidence`, `accessibility-validation`. No additional values should be added. [Source: `_bmad-output/planning-artifacts/prd.md#NFR1`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`]
  - [x] Add `ConformanceManifestLifecycleStageJsonConverter` to `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` following the `ReleaseGateStatusJsonConverter` pattern using `ConversationStringValueJsonConverter<ConformanceManifestLifecycleStage>`. [Source: `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`]

- [x] Define the manifest row and change-log types (AC: 1, 2, 4)
  - [x] Add `ConformanceManifestRowV1` as a sealed record in `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs` with these validated fields:
    - `string TestId` — stable bounded machine-readable test identifier (`RequiredSafeToken`)
    - `string TestName` — bounded human-readable name (`RequiredSafeText`)
    - `string RequirementId` — FR or NFR identifier such as `"FR83"` or `"NFR63"` (`RequiredSafeToken`)
    - `string? CarryForwardCommitmentRef` — nullable bounded carry-forward reference; when non-null, validate with `RequiredSafeToken`
    - `ReleaseGateId? ReleaseGateId` — optional gate this test contributes to; null is valid when entry is not gate-specific
    - `string PassCriteria` — bounded pass criteria description (`RequiredSafeText`)
    - `ReleaseGateStatus ReleaseDecisionStatus` — current decision status (reuses `ReleaseGateStatus`; no new vocabulary needed)
    - `string? WaiverReference` — nullable bounded waiver reference; validate with `RequiredSafeToken` if non-null; `ValidateManifest` enforces presence when status is `waived`
    - `string MeasurementMethod` — bounded measurement method description (`RequiredSafeText`)
    - `string Environment` — bounded environment descriptor (`RequiredSafeToken`)
    - `string EvidenceArtifactHandle` — bounded evidence artifact handle (`RequiredSafeToken`)
    - `string Owner` — bounded owner identifier (`RequiredSafeToken`)
    - `ConformanceManifestLifecycleStage LifecycleStage` — required lifecycle stage
    - `DateTimeOffset RegisteredAtUtc` — UTC registration timestamp (`RequiredUtcTimestamp`)
  [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.3 AC1, AC4`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs`]
  - [x] Add `ConformanceManifestChangeV1` as a sealed record in the same file with: `string ChangeId` (`RequiredSafeToken`), `string ChangeSummary` (`RequiredSafeText`), `IReadOnlyList<string> AffectedRequirementIds` (non-empty list of safe tokens validated at construction), `DateTimeOffset ChangedAtUtc` (`RequiredUtcTimestamp`), `string ChangedBy` (`RequiredSafeToken`). This captures the version history (AC2). [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.3 AC2`]

- [x] Define the versioned manifest type (AC: 1, 2, 3)
  - [x] Add `ConformanceManifestV1` as a sealed record in `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs` with:
    - `SchemaVersion SchemaVersion` — manifest schema version (use `SchemaVersion.Current` in fixtures)
    - `string ManifestVersion` — bounded release-specific manifest version string such as `"v1-2026-05-23"` (`RequiredSafeToken`)
    - `string ReleaseReference` — bounded release reference (`RequiredSafeToken`)
    - `DateTimeOffset GeneratedAtUtc` — UTC generation timestamp (`RequiredUtcTimestamp`)
    - `IReadOnlyList<ConformanceManifestRowV1> Entries` — non-empty list; null entries forbidden; validated at construction
    - `IReadOnlyList<ConformanceManifestChangeV1> ChangeLog` — may be empty; represents version history; null list not allowed (use empty list)
  [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.3 AC1-AC3`; `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs`]

- [x] Add manifest validation helper (AC: 2, 3, 4)
  - [x] Add `ConformanceManifestValidator` as a static class in `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs` with a `ValidateManifest(ConformanceManifestV1 manifest)` method returning `IReadOnlyList<string>` of content-safe typed error tokens. Validate:
    - Duplicate test IDs: emit `"duplicate-test-id"` for each group of duplicates (one error token per duplicate test ID value)
    - `waived` status without waiver reference: emit `"missing-waiver-reference"` for each such row
    - Empty `RequirementId` (impossible at construction but defensively checked): emit `"missing-requirement-id"`
    - Empty `PassCriteria` (impossible at construction): emit `"missing-pass-criteria"`
    - Return empty list for a valid manifest
  - [x] All error tokens must be content-safe: never include caller-supplied free text or bounded data that could expose protected identifiers. Use the pattern `{token-name}` without appending row values that may be unsafe. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs#ValidateArtifact`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`]

- [x] Author the manifest schema document (AC: 1, 3)
  - [x] Create `docs/release-evidence/manifest.schema.json` as a structured human-navigable JSON specification document (not a formal JSON Schema dialect). Include sections: `schemaVersion`, `title`, `description`, `fields` (array of `{name, type, required, validation, description}` objects for each `ConformanceManifestRowV1` field), `validationRules` (array of `{ruleName, description, errorToken}` objects matching `ValidateManifest` checks), and `exampleRow` (a sample serialized `ConformanceManifestRowV1`). This document must be navigable by non-developer release approvers per NFR68. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure`; `_bmad-output/planning-artifacts/prd.md#NFR68`; `docs/release-evidence/contract-compatibility-policy.md`]

- [x] Create and commit the synthetic manifest fixture file (AC: 1, 3)
  - [x] Create `docs/release-evidence/conformance-manifest-v1-fixture.json` as a deterministic synthetic fixture manifest containing at least these rows:
    - `"story-5-1-compatibility-policy-publication"` → `requirementId: "FR81"`, `releaseGateId: null`, `lifecycleStage: "release-evidence"`, `releaseDecisionStatus: "pass"`, `evidenceArtifactHandle: "contract-compatibility-policy"`, `owner: "release-engineer"`, `environment: "local-ci"`, `passCriteria: "Policy document exists and validation tests pass"`, `measurementMethod: "automated-doc-validation-test"`, `waiverReference: null`
    - `"story-5-2-release-conformance-artifact"` → `requirementId: "FR82"`, `releaseGateId: null`, `lifecycleStage: "release-evidence"`, `releaseDecisionStatus: "unknown-accepted"`, `evidenceArtifactHandle: "release-conformance-artifact-v1-fixture"`, `owner: "release-engineer"`, `environment: "local-ci"`, `passCriteria: "Conformance artifact exists validates with zero errors and contains all 7 gate IDs"`, `measurementMethod: "automated-generation-test"`, `waiverReference: null`
    - `"story-5-3-conformance-manifest-schema"` → `requirementId: "FR83"`, `releaseGateId: null`, `lifecycleStage: "release-evidence"`, `releaseDecisionStatus: "pass"`, `evidenceArtifactHandle: "conformance-manifest-v1-fixture"`, `owner: "release-engineer"`, `environment: "local-ci"`, `passCriteria: "Manifest fixture exists validates with zero diagnostics and covers FR83 FR84 and NFR63"`, `measurementMethod: "automated-manifest-validation-test"`, `waiverReference: null`
  - [x] The fixture must be content-safe: no real tenant IDs, Party IDs, conversation IDs, local paths, or raw exceptions. Use bounded tokens like `"release-engineer"` and `"local-ci"`. Use `"test-runner"` as a safe signer if referenced. [Source: `_bmad-output/project-context.md#Testing Rules`; `docs/release-evidence/release-conformance-artifact-v1-fixture.json`]

- [x] Write contract and validation tests (AC: 1-4)
  - [x] Add `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` covering:
    - `ConformanceManifestLifecycleStage` closed-vocabulary completeness (6 values), JSON rejection of synonyms (`"test"`, `"testing"`, `"design"`, `"ops"`, `"review"`), `Parse` round-trips for all 6 values
    - `ConformanceManifestRowV1` construction-time validation: null/empty test ID, empty test name, empty requirement ID, empty pass criteria, empty evidence handle, empty owner, empty environment, null lifecycle stage, non-UTC timestamp, unsafe free-text rejection in `SafeText` fields; null `ReleaseGateId` is valid at construction; null `WaiverReference` is valid at construction even when status is `waived` (validator catches this, not constructor)
    - `ConformanceManifestChangeV1` construction-time validation: null change ID, empty change summary, empty affected requirement IDs, non-UTC timestamp, empty changed-by
    - `ConformanceManifestV1` construction-time validation: null schema version, empty manifest version, empty release reference, empty entries list, null entries in list, null change-log is invalid (use empty list)
    - `ConformanceManifestValidator.ValidateManifest` returns errors for: duplicate test IDs (`"duplicate-test-id"`), `waived` status with null waiver reference (`"missing-waiver-reference"`); returns empty list for a valid manifest
    - Stable camelCase web JSON shape, round-trip, additive-JSON tolerance for `ConformanceManifestRowV1` and `ConformanceManifestV1`
    - Fixture file at `docs/release-evidence/conformance-manifest-v1-fixture.json` exists, deserializes without error, passes `ValidateManifest` with zero diagnostics, contains at least 3 entries, and passes the content-safety scan
  [Source: `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs`; `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs`]
  - [x] Register `ConformanceManifestLifecycleStage`, `ConformanceManifestRowV1`, `ConformanceManifestChangeV1`, and `ConformanceManifestV1` samples in `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` so existing serialization, forbidden-surface, and content-safety scans cover the new types automatically. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]
  - [x] Add `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` covering:
    - The fixture manifest passes `ValidateManifest` with zero errors
    - A manifest with a duplicate test ID row returns `"duplicate-test-id"` error
    - A manifest with a `waived` entry missing a waiver reference returns `"missing-waiver-reference"` error
    - All entries in the fixture pass the content-safety scan (no unsafe fragment in any field)
    - Manifest serializes to stable camelCase JSON and round-trips deterministically using `FakeTimeProvider` for the `RegisteredAtUtc` and `GeneratedAtUtc` timestamps in test construction
    - `ConformanceManifestLifecycleStage.All` returns exactly 6 stages matching the NFR1-defined lifecycle vocabulary
  [Source: `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs`]

- [x] Update local evidence and run validation (AC: 1-4)
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 5.3 evidence: new type paths, new test paths, targeted test results, full solution results.
  - [x] Run targeted tests first:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConformanceManifest|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"` — 57 passed
    - `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` — 43 passed
  - [x] Run full solution validation:
    - `dotnet build Hexalith.Conversations.slnx` — succeeded, 0 warnings, 0 errors
    - `dotnet test Hexalith.Conversations.slnx` — 1121 total, 0 failures
  - [x] Confirm no test, docs check, or setup step requires nested submodule initialization. [Source: `AGENTS.md`; `_bmad-output/project-context.md#Development Workflow Rules`]

- [x] Preserve story boundaries and stop conditions (AC: 1-4)
  - [x] Do not implement Story 5.4 named-waiver records, waiver approval workflow, or waiver status aggregation from external sources.
  - [x] Do not implement Stories 5.5-5.9 per-domain conformance suites (tenant isolation, idempotency, redaction replay, provider portability, event schema evolution).
  - [x] Do not implement Story 5.10 aggregated contract validation manifest coverage or CORE fixture manifest rows.
  - [x] Do not implement Story 5.11 module-level versus platform-level evidence separation.
  - [x] Do not change runtime tenant authorization, audit pairing, redaction replay, idempotency, projection freshness, event persistence, or client transport behavior.
  - [x] Do not add a new public package, CLI tool, background worker, durable artifact store, release-signing PKI infrastructure, database, export pipeline, globally runnable host, or admin UI surface.
  - [x] Stop for ADR if implementation needs a new public trust/freshness/conformance vocabulary term, a PKI or cryptographic signing requirement, a durable manifest store, or a waiver of any fail-closed/security/privacy rule.

## Dev Notes

### Epic and Business Context

- Epic 5 is the release-owner layer for compatibility, conformance, manifest traceability, waivers, release-gate proof, and module-versus-platform evidence. Story 5.3 defines the manifest schema, types, and validation logic, and seeds a synthetic fixture from existing story evidence. It does not aggregate all story evidence or run per-domain suites. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`]
- FR83: The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability. FR84: The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies. [Source: `_bmad-output/planning-artifacts/prd.md#FR83`; `_bmad-output/planning-artifacts/prd.md#FR84`]
- NFR63 is the binding constraint: every release must produce a signed conformance artifact AND versioned manifest mapping tests to FRs, NFRs, carry-forward commitments, pass criteria, waiver status, measurement method, and environment. Story 5.2 covers the signed artifact; Story 5.3 covers the versioned manifest rows. [Source: `_bmad-output/planning-artifacts/prd.md#NFR63`]
- NFR8: Conformance evidence must include test environment identity, dataset scale, tool versions, build hash, schema/event versions, timestamped evidence links, and release manifest reference. Story 5.3's manifest rows are the target of Story 5.2's `ReleaseManifestReference` field — the fixture manifest's `ManifestVersion` + `ReleaseReference` is the reference string callers put in `ReleaseConformanceArtifactBuilder`. [Source: `_bmad-output/planning-artifacts/prd.md#NFR8`; `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactBuilder.cs`]
- NFR68: Release and conformance evidence must be navigable by non-developer approvers. The manifest schema document and the fixture JSON must together serve this navigability need without requiring log access. [Source: `_bmad-output/planning-artifacts/prd.md#NFR68`]

### Existing Surfaces to Reuse

- `ConformanceVocabulary.cs` — defines `ConformanceCheck` and `ConformanceOutcome` with the exact sealed-record, private-constructor, static-factory, `All`, `Parse`, `JsonConverter` pattern that `ConformanceManifestLifecycleStage` must follow exactly. Do not invent a different structure. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`]
- `ReleaseGateStatus.cs` — defines `ReleaseGateStatus` (pass/fail/waived/unknown-accepted with `IsBlocking`), `ReleaseGateId` (7 gate IDs), `ReleaseGateResultV1`. `ConformanceManifestRowV1.ReleaseDecisionStatus` reuses `ReleaseGateStatus` directly — no new status vocabulary needed. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs`]
- `ConformanceContractValidation.cs` — provides `RequiredSafeToken`, `RequiredSafeText`, `RequiredUtcTimestamp`, and closed-token validator. All new record types must use these helpers at construction time without exception. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`]
- `ReleaseConformanceArtifactV1.cs` — `ValidateArtifact` static method returning `IReadOnlyList<string>` of content-safe token errors is the exact pattern for `ConformanceManifestValidator.ValidateManifest`. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs`]
- `ClosedVocabularyJsonConverters.cs` — `ReleaseGateStatusJsonConverter` uses `ConversationStringValueJsonConverter<T>` as base; add `ConformanceManifestLifecycleStageJsonConverter` the same way. [Source: `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`]
- `ContractSamples.cs` — participates in serialization, forbidden-surface, and content-safety scans. Register all new manifest types to get free scan coverage without writing additional scan tests. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]
- `AppContext.BaseDirectory`-based repo-root path — the established pattern for deterministic test file reads; used in `ReleaseConformanceArtifactContractTest.cs` for the fixture JSON. Story 5.3 fixture test must use the same approach. [Source: `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs`]
- `docs/release-evidence/` — already contains `contract-compatibility-policy.md` and `release-conformance-artifact-v1-fixture.json`. The manifest schema document and manifest fixture JSON belong there. [Source: `docs/release-evidence/`]

### ConformanceManifestLifecycleStage Values

Exactly six values from NFR1 (no more, no less): `design-review`, `automated-test`, `load-performance-test`, `operational-drill`, `release-evidence`, `accessibility-validation`. JSON rejection test must prove synonyms like `"test"`, `"testing"`, `"ops"`, `"design"`, `"review"`, `"load-test"` are all invalid.

### manifest.schema.json Approach

Architecture calls for `docs/release-evidence/manifest.schema.json`. This must be a structured human-readable JSON specification (not a `$schema`-keyed JSON Schema draft), following the same spirit as `contract-compatibility-policy.md`:

```json
{
  "schemaVersion": "1",
  "title": "Hexalith.Conversations Conformance Manifest Schema",
  "description": "...",
  "fields": [
    { "name": "testId", "type": "string", "required": true, "validation": "safe-token", "description": "..." },
    ...
  ],
  "validationRules": [
    { "ruleName": "no-duplicate-test-ids", "description": "...", "errorToken": "duplicate-test-id" },
    { "ruleName": "waived-requires-waiver-reference", "description": "...", "errorToken": "missing-waiver-reference" }
  ],
  "exampleRow": { ... }
}
```

The `description`, field descriptions, and validation rule descriptions must be content-safe (no protected identifiers, no raw business data).

### Project Structure Decision

Before adding any builder/seeder code:
1. `ls src/` — check if `src/Hexalith.Conversations.Conformance/` exists
2. If **not** found: place any `ConformanceManifestSeeder` class in `tests/Hexalith.Conversations.Conformance.Tests/` (same as `ReleaseConformanceArtifactBuilder.cs` in Story 5.2)
3. If **found**: place the seeder in `src/Hexalith.Conversations.Conformance/Manifest/`
4. Contract types (`ConformanceManifestLifecycleStage`, `ConformanceManifestRowV1`, `ConformanceManifestChangeV1`, `ConformanceManifestV1`, `ConformanceManifestValidator`) always go in `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs` (one file for related manifest types, same as `ReleaseGateStatus.cs` bundles three related types)

If Story 5.3 is the natural home for creating `src/Hexalith.Conversations.Conformance/`, the architecture explicitly maps this project to `Manifest/`, `Suites/`, `Evidence/`, and `Verification/` subdirectories. Creating the project is permitted but not required if all code can stay in the tests project.

### Testing Requirements

- Primary test surface: contract-level tests for the lifecycle vocabulary, new record types, `ValidateManifest` helper, JSON shape, round-trip, additive tolerance, content-safety, and fixture file validation.
- Secondary surface: conformance-project validation tests proving the fixture manifest passes validation, duplicates are detected, and waiver-missing errors are correctly reported.
- Use xUnit v3, Shouldly, deterministic synthetic fixtures, `AppContext.BaseDirectory`-based repo-root file reads, and `FakeTimeProvider` for deterministic timestamps.
- Content-safety: register new types in `ContractSamples.cs`; do not introduce real tenant IDs, Party IDs, conversation IDs, raw exceptions, local paths, or business-record identifiers in test data or fixture files.
- Targeted test filter: `FullyQualifiedName~ConformanceManifest`.
- Run full `dotnet test tests/Hexalith.Conversations.Conformance.Tests` after changes to catch regressions in existing adopter-suite and Story 5.2 generation tests.

### Previous Story Intelligence

- Story 5.2 established `ReleaseConformanceArtifactV1`, `ReleaseGateStatus`, `ReleaseGateId`, `ReleaseConformanceArtifactBuilder`, and the `docs/release-evidence/release-conformance-artifact-v1-fixture.json` fixture pattern. Story 5.3 extends exactly the same approach: new contract types in `src/Hexalith.Conversations.Contracts/Conformance/`, builder/seeder in tests, fixture JSON in `docs/release-evidence/`. No deviation from this established pattern. [Source: `_bmad-output/implementation-artifacts/5-2-generate-signed-release-conformance-artifact.md`]
- Story 5.2's `ReleaseConformanceArtifactBuilder` takes a `releaseManifestReference` string. The fixture manifest created in Story 5.3 is the target of that reference. The fixture manifest's `ManifestVersion` = `"v1-fixture"` and `ReleaseReference` = `"local-test-release"` should inform what value Story 5.2 generation tests use for `releaseManifestReference`. There is no code dependency (the builder just stores a string), but the fixture manifest and the existing artifact fixture should be consistent. [Source: `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactBuilder.cs`]
- Story 5.1 used `AppContext.BaseDirectory`-based path for doc validation. Story 5.3 must use the same pattern for fixture JSON tests. [Source: `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs`]
- Content-safety lesson: closed machine identifiers like `"automated-test"`, `"release-evidence"`, `"FR83"`, `"design-review"`, `"local-ci"` are valid safe tokens. The content-safety scan rejects real tenant IDs, Party IDs, conversation IDs, local paths, and raw exceptions — not vocabulary name strings. [Source: `_bmad-output/implementation-artifacts/5-2-generate-signed-release-conformance-artifact.md#Previous Story Intelligence`]
- Recent git history (last 8 commits):
  - `feat(story-5.2): Generate signed release conformance artifact`
  - `feat(story-5.1): Publish contract compatibility and deprecation policy`
  - `fix(tests): Make privileged-justification tests time-independent`
  - `feat(story-4.7): Publish developer integration guide and API examples`
  Use the same focused-test and evidence-summary commit pattern.

### Latest Technical Notes

- Solution has 1074 passing tests after Story 5.2. New story should add ~20-30 contract tests and ~6-8 conformance validation tests; full solution count should increase to approximately 1100-1112.
- No external library upgrade needed. All new types use only types already present in the Contracts project: `System.Text.Json`, `SchemaVersion`, `ReleaseGateStatus`, `ReleaseGateId`, `ConformanceContractValidation`, and the existing JSON converter infrastructure.
- `SchemaVersion.Current` is the correct value for `ConformanceManifestV1.SchemaVersion` (existing pattern from `ReleaseConformanceArtifactV1`). [Source: `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactBuilder.cs`]
- For fixture timestamp values, use `DateTimeOffset.Parse("2026-05-23T00:00:00Z")` as a fixed safe timestamp (same date as Story 5.2 completion, which is today's date per system context).

### Files Likely to Touch

- New files:
  - `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs` — `ConformanceManifestLifecycleStage`, `ConformanceManifestChangeV1`, `ConformanceManifestRowV1`, `ConformanceManifestV1`, and `ConformanceManifestValidator`
  - `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` — contract and fixture validation tests
  - `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` — manifest validation tests
  - `docs/release-evidence/manifest.schema.json` — structured schema specification document
  - `docs/release-evidence/conformance-manifest-v1-fixture.json` — synthetic deterministic fixture manifest

- Update files:
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` — add `ConformanceManifestLifecycleStageJsonConverter`
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` — register new manifest types
  - `_bmad-output/implementation-artifacts/tests/test-summary.md` — Story 5.3 evidence entry

### Architecture Guardrails

- New manifest types belong in `src/Hexalith.Conversations.Contracts/Conformance/` to stay infrastructure-free and adopter-stable. No EventStore envelopes, Dapr actors, HTTP clients, server infrastructure, or UI shell references are permitted in Contracts. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Public contracts must not expose EventStore stream names, event positions, snapshots, projection topology, raw exception text, test framework namespaces, server routes, local paths, or unsafe free-text. All free-text fields must pass the forbidden-surface scan. [Source: `_bmad-output/planning-artifacts/architecture.md#API Pattern`]
- Central Package Management is active. Do not add package versions directly to `.csproj` files. [Source: `Directory.Packages.props`]
- SDK `10.0.300`, target `net10.0`, nullable enabled, implicit usings, warnings as errors. [Source: `global.json`; `Directory.Build.props`]

### Out of Scope

- Named-waiver records, waiver approval workflow, waiver status aggregation (Story 5.4)
- Full adversarial tenant isolation conformance suite (Story 5.5)
- Full idempotency conformance suite (Story 5.6)
- Full redaction replay conformance suite (Story 5.7)
- Provider portability proof (Story 5.8), event schema evolution proof (Story 5.9)
- Aggregated contract test manifest rows, CORE fixture manifest rows (Story 5.10)
- Module-vs-platform evidence separation (Story 5.11)
- PKI/cryptographic signing infrastructure, HSM integration
- New durable manifest stores, database tables, background workers, export pipelines, CLI tools, globally runnable hosts
- New public compatibility vocabulary, new public freshness state, new public trust state, or changes to existing closed vocabularies
- New runtime tenant authorization, audit pairing, redaction replay, idempotency, projection freshness, event persistence, or client transport behavior
- Admin UI surfaces for manifest navigation

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`
- `_bmad-output/planning-artifacts/epics.md#Story 5.3: Maintain Versioned Conformance Manifest with Traceability`
- `_bmad-output/planning-artifacts/epics.md#Story 5.4: Support Named Waivers for Release-Gate Exceptions`
- `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`
- `_bmad-output/planning-artifacts/prd.md#FR83`
- `_bmad-output/planning-artifacts/prd.md#FR84`
- `_bmad-output/planning-artifacts/prd.md#NFR63`
- `_bmad-output/planning-artifacts/prd.md#NFR8`
- `_bmad-output/planning-artifacts/prd.md#NFR68`
- `_bmad-output/planning-artifacts/prd.md#NFR1`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/architecture.md#API Pattern`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/5-2-generate-signed-release-conformance-artifact.md`
- `_bmad-output/implementation-artifacts/5-1-publish-contract-compatibility-and-deprecation-policy.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/project-context.md`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactBuilder.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs`
- `docs/release-evidence/contract-compatibility-policy.md`
- `docs/release-evidence/release-conformance-artifact-v1-fixture.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_No blocking issues. Three test failures on initial run (null-check tests where helper substituted null via `??`); fixed by calling constructors directly in those three tests._

### Completion Notes List

- Implemented `ConformanceManifestLifecycleStage` (6 NFR1 values), `ConformanceManifestRowV1` (14 validated fields), `ConformanceManifestChangeV1` (version history), `ConformanceManifestV1` (versioned manifest), and `ConformanceManifestValidator.ValidateManifest` returning content-safe typed error tokens — all in one file following the Story 5.2 pattern.
- Added `ConformanceManifestLifecycleStageJsonConverter` to `ClosedVocabularyJsonConverters.cs` following the `ReleaseGateStatusJsonConverter` pattern.
- Created `docs/release-evidence/manifest.schema.json` as a human-navigable JSON specification document with all 14 fields, 2 validation rules, and an example row.
- Created `docs/release-evidence/conformance-manifest-v1-fixture.json` with 3 content-safe entries (story-5-1, story-5-2, story-5-3) and empty change-log.
- Registered all 4 new types in `ContractSamples.cs` for automatic forbidden-surface and content-safety scan coverage.
- `src/Hexalith.Conversations.Conformance/` did NOT exist; builder/seeder follows Story 5.2 precedent and stays in the test project (no new seeder needed — fixture JSON is static).
- Solution test count increased from 1074 (Story 5.2) to 1121 (+47 tests: 41 contract + 6 conformance validation).
- Zero new public error codes, trust states, or conformance outcome values introduced.

### Senior Developer Review (AI)

**Reviewer:** AI Review Agent — 2026-05-23  
**Outcome:** Approved with auto-fixes applied

**Git vs Story Discrepancies:** 0

**Issues Fixed (5 Medium, 1 Low):**

- [AI-Review][MEDIUM] Added `ManifestRowShouldRejectEmptyMeasurementMethod` test — `MeasurementMethod` is a `RequiredSafeText` field with no dedicated empty-rejection test unlike `TestName` and `PassCriteria`. [ConformanceManifestContractTest.cs]
- [AI-Review][MEDIUM] Added `ManifestV1ShouldRejectNonUtcGeneratedAtUtc` test — `GeneratedAtUtc` validated via `RequiredUtcTimestamp` but had no test for the non-UTC path. [ConformanceManifestContractTest.cs]
- [AI-Review][MEDIUM] Added `ManifestChangeShouldRejectEmptyChangeId` test — `ChangeId` had a null test but no empty test, inconsistent with `ChangeSummary` pattern. [ConformanceManifestContractTest.cs]
- [AI-Review][MEDIUM] Enhanced `ManifestRowShouldRoundTripLosslessly` assertions — added `TestName`, `Owner`, `Environment`, `RegisteredAtUtc` checks (previously only 4 of 14 fields verified). [ConformanceManifestContractTest.cs]
- [AI-Review][MEDIUM] Enhanced `ManifestV1ShouldRoundTripLosslessly` assertions — added `ChangeLog.Count`, `GeneratedAtUtc`, `SchemaVersion` checks (previously only 3 of 6 top-level fields verified). [ConformanceManifestContractTest.cs]
- [AI-Review][LOW] Enhanced `ManifestShouldSerializeToStableCamelCaseJsonAndRoundTripDeterministically` — added deserialization assertion; test previously only proved serialization determinism without verifying the round-trip read. [ConformanceManifestValidationTest.cs]

**Post-fix solution test count:** 1124 total (1121 pre-review + 3 new tests), 0 failures.

**Intentional findings (no action):**
- `ValidateManifest` dead code for `missing-requirement-id`/`missing-pass-criteria` — intentional defensive checks per story spec.
- `manifest.schema.json` documents only `ConformanceManifestRowV1` fields — intentional per story spec (NFR68 approver navigability).

### File List

**New files:**
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs`
- `docs/release-evidence/manifest.schema.json`
- `docs/release-evidence/conformance-manifest-v1-fixture.json`

**Modified files:**
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` _(review fixes: +3 tests, enhanced round-trip assertions)_
- `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` _(review fix: added round-trip deserialization assertion)_
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/5-3-maintain-versioned-conformance-manifest-with-traceability.md`
