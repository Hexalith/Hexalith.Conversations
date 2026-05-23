# Story 5.2: Generate Signed Release Conformance Artifact

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release owner,
I want each release to produce a signed conformance artifact,
so that release decisions have durable evidence rather than informal test claims.

## Acceptance Criteria

1. The release conformance artifact captures the required evidence fields for release gating
   - Given a release candidate is evaluated,
   - When the conformance artifact is generated,
   - Then it includes build hash, schema and event versions, contract package versions, test environment identity, dataset scale, tool versions, timestamped evidence links, pass/fail/waiver status, signer or runner identity, and release manifest reference,
   - And the artifact is machine-readable, deterministically structured, and content-safe.

2. Release-gated checks are classified in the artifact
   - Given release-gated checks complete,
   - When results are summarized,
   - Then tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, contract compatibility, and provider portability are each classified as `pass`, `fail`, `waived`, or `unknown-accepted`,
   - And automatic blockers remain blockers unless the named-waiver process explicitly applies.

3. Artifact validation rejects unsafe, incomplete, or unsigned evidence
   - Given artifact validation runs,
   - When required evidence is missing, unsigned (missing signer/runner identity), stale, contradictory, or content-unsafe,
   - Then validation fails with typed diagnostics,
   - And unsafe evidence does not get published as release-ready.

## Tasks / Subtasks

- [x] Confirm scope, evidence boundary, and existing conformance infrastructure before editing (AC: 1-3)
  - [x] Treat this as the second Epic 5 release-owner story: preserve release-owner framing, release decision evidence consequence, and FR82/FR86 traceability; do not reduce the story to a generic "write a JSON file" task. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`]
  - [x] Honor the Two-Level Evidence semantics gate: Story 5.2 creates the artifact type, generates local test evidence against the type, and produces a fixture artifact file; signed GA release artifacts are generated at release time using Story 5.2's tooling. Story 5.3 owns the versioned manifest with full test-to-requirement traceability; Story 5.4 owns named waivers; Story 5.10 consumes Story 4.5 evidence for the contract validation gate entry. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md#Two-level evidence semantics`; `_bmad-output/planning-artifacts/epics.md#Story 5.3`; `_bmad-output/planning-artifacts/epics.md#Story 5.4`; `_bmad-output/planning-artifacts/epics.md#Story 5.10`]
  - [x] Reuse the existing conformance vocabulary: `ConformanceRunResultV1`, `ConformanceCheckResultV1`, `ConformanceVocabulary` (`ConformanceCheck`, `ConformanceOutcome`, `ConformanceFailureClassification`), `ConformanceContractValidation`, and `AdopterConformanceSuite` already provide the adopter-layer conformance model. Story 5.2 adds the release-layer on top of this without forking or duplicating the adopter surface. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs`; `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuite.cs`]
  - [x] Do not invent new public error, compatibility status, freshness, trust, or conformance outcome values unless the existing contract vocabulary cannot represent the release-gate model; new vocabulary is an ADR-triggering public-contract change. [Source: `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`; `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]

- [x] Define the release gate classification vocabulary (AC: 2)
  - [x] Add `ReleaseGateStatus` as a new closed vocabulary record to `src/Hexalith.Conversations.Contracts/Conformance/` with values `pass`, `fail`, `waived`, and `unknown-accepted` following the exact pattern of `ConformanceOutcome` (sealed record, private constructor, `JsonConverter`, `Parse(string)`, `All`, `IsBlocking` property, `KnownValues` dict). No additional values should be added; waiver details belong to Story 5.4. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`; `_bmad-output/planning-artifacts/epics.md#Story 5.2 AC2`]
  - [x] Add `ReleaseGateId` as a closed vocabulary with the seven required gate identifiers: `tenant-isolation`, `audit-integrity`, `redaction-non-leakage`, `unsupported-schema-rejection`, `projection-rebuild-determinism`, `contract-compatibility`, and `provider-portability`. Follow the `ConformanceCheck` pattern (sealed record, private constructor, seven static properties, `All`, `Parse`). These identifiers must align with FR87-FR90 and FR86 gate classification requirements. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2 AC2`; `_bmad-output/planning-artifacts/prd.md#FR86`; `_bmad-output/planning-artifacts/prd.md#FR87-FR90`]
  - [x] Add `ReleaseGateResultV1` as a content-safe record with `ReleaseGateId GateId`, `ReleaseGateStatus Status`, `string SafeEvidenceSummary`, `string EvidenceHandle`, `string? WaiverReference` (nullable, used only when status is `waived` or `unknown-accepted`), `DateTimeOffset EvaluatedAtUtc`, and `string RequirementId`. Follow `ConformanceCheckResultV1` field validation patterns: `RequiredSafeToken`, `RequiredSafeText`, `RequiredUtcTimestamp`. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs`]

- [x] Define the signed release conformance artifact type (AC: 1, 3)
  - [x] Add `ReleaseConformanceArtifactV1` to `src/Hexalith.Conversations.Contracts/Conformance/` as a sealed record with these required fields (all validated at construction): `SchemaVersion SchemaVersion`, `string BuildHash`, `string SignerOrRunnerId`, `string TestEnvironmentId`, `string DatasetScale`, `string ToolVersions`, `string ReleaseManifestReference`, `DateTimeOffset GeneratedAtUtc`, `IReadOnlyList<SchemaVersion> EventSchemaVersions`, `IReadOnlyList<string> ContractPackageVersions`, `IReadOnlyList<string> EvidenceLinks`, `IReadOnlyList<ReleaseGateResultV1> GateResults`. Validate at construction using `ConformanceContractValidation` helpers; require non-empty lists; require at least one gate result per `ReleaseGateId` value; forbid null entries. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2 AC1`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs`]
  - [x] The artifact must be machine-readable (stable camelCase web JSON via `System.Text.Json`, additive-tolerant, round-trippable) and content-safe (all free-text fields pass the forbidden-surface scan rules from `ConformanceContractValidation`). [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`]
  - [x] Add a `ValidateArtifact` static helper to `ReleaseConformanceArtifactV1` that returns typed validation errors for: missing signer/runner identity, missing build hash, missing all seven gate entries, any gate entry with `fail` status and missing evidence handle, contradictory overall status (if a blocker gate is `fail` but other evidence implies pass), and unsafe free-text fields. Return a `IReadOnlyList<string>` of error reasons (content-safe tokens, not free text). [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2 AC3`]
  - [x] Add `OverallStatus` computed property: `pass` if all gate results are `pass`; `fail` if any gate result is `fail`; `waived` if no gate is `fail` and at least one is `waived`; `unknown-accepted` otherwise. This is a deterministic computation, not a settable field, so it cannot be forged. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2 AC2`]

- [x] Add a minimal release conformance artifact generator helper (AC: 1)
  - [x] Add `ReleaseConformanceArtifactBuilder` to `src/Hexalith.Conversations.Conformance/` (under `Manifest/` directory matching the architecture map for FR82). The builder aggregates: the `ConformanceRunResultV1` from the adopter suite (Story 4.5 evidence), environment metadata (build hash, tool versions, test environment ID, dataset scale from constructor parameters), the signer/runner identity, and the release manifest reference. It derives per-gate results from the conformance run check results where directly mappable and emits `unknown-accepted` with an explicit evidence handle stub for gates whose evidence belongs to later stories (5.5-5.10). [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuite.cs`]
  - [x] The builder must be deterministic: the same inputs always produce the same `ReleaseConformanceArtifactV1` output. No random values, no `DateTime.Now` (inject `TimeProvider`). [Source: `_bmad-output/project-context.md#Testing Rules`]
  - [x] The builder must fail closed on invalid inputs: null signer, empty build hash, null conformance run result, null environment metadata. Return typed errors rather than producing a partial artifact. [Source: `_bmad-output/project-context.md#Critical Implementation Rules`]

- [x] Write contract and content-safety tests (AC: 1-3)
  - [x] Add `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` covering: `ReleaseGateStatus` closed-vocabulary completeness, JSON rejection of synonyms (`ok`, `pass-ish`, `green`, `red`, `skip`), `ReleaseGateId` closed-vocabulary completeness (all seven IDs present), JSON rejection of unknown gate IDs, `ReleaseGateResultV1` construction-time validation (null gate ID, null status, empty evidence handle, empty requirement ID, empty evidence summary, future timestamp, unsafe free-text rejection), `ReleaseConformanceArtifactV1` construction-time validation (empty build hash, missing signer, empty gate list, missing gate IDs, null schema version), `ValidateArtifact` returns errors for missing signer identity and missing required gate results, `OverallStatus` computed property matrix (all pass → pass; one fail → fail; all pass/waived → waived; all pass/unknown-accepted → unknown-accepted), stable camelCase web JSON, round-trip, and additive-JSON tolerance. [Source: `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceContractsTest.cs`; `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`]
  - [x] Register `ReleaseGateStatus`, `ReleaseGateId`, `ReleaseGateResultV1`, and `ReleaseConformanceArtifactV1` samples in `ContractSamples.cs` so existing serialization, forbidden-surface, and content-safety scans cover the new types automatically. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]
  - [x] Add `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` covering: `ReleaseConformanceArtifactBuilder` produces a valid `ReleaseConformanceArtifactV1` from the CORE fixture conformance run result; `OverallStatus` is deterministic; the artifact contains all seven required gate entries; gates mapped from the adopter suite carry `pass`/`fail`/`unknown-accepted` consistent with the adopter-suite outcome; gates not covered by the adopter suite emit `unknown-accepted` with a non-null stub evidence handle; artifact content-safety scan passes; null signer rejects; null conformance run result rejects; determinism (same inputs → same output with injected `TimeProvider`). [Source: `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuiteTest.cs`; `tests/Hexalith.Conversations.Conformance.Tests/CoreFixtureContentSafetyTest.cs`]

- [x] Generate and commit a local test evidence artifact file (AC: 1)
  - [x] After tests pass, invoke the builder from a test or script to write a deterministic fixture artifact to `docs/release-evidence/release-conformance-artifact-v1-fixture.json`. This proves the schema is real and machine-readable, not just a type definition. The file content must be synthetic and content-safe: use `"test-runner"` as signer, `"ci-build-test-fixture"` as build hash, `"test-env-local"` as environment, `"synthetic-dataset"` as dataset scale, `"1032 tests"` as tool versions (or similar safe bounded string), and use the CORE fixture run result as input. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2 AC1`; `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
  - [x] Add a doc validation test to `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` that: the fixture file exists at `docs/release-evidence/release-conformance-artifact-v1-fixture.json`, deserializes without error, validates via `ValidateArtifact` with zero errors, contains all seven gate IDs, and passes the content-safety scan. Follow the deterministic `AppContext.BaseDirectory`-based repo-root pattern from existing docs tests. [Source: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs`]

- [x] Update local evidence and run validation (AC: 1-3)
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 5.2 evidence: new type paths, new test paths, targeted test results, full solution results.
  - [x] Run targeted tests first:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ReleaseConformance|FullyQualifiedName~ReleaseGate|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"`
    - `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`
  - [x] Run full solution validation:
    - `dotnet build Hexalith.Conversations.slnx`
    - `dotnet test Hexalith.Conversations.slnx`
  - [x] Confirm no test, docs check, or setup step requires nested submodule initialization. [Source: `AGENTS.md`; `_bmad-output/project-context.md#Development Workflow Rules`]

- [x] Preserve story boundaries and stop conditions (AC: 1-3)
  - [x] Do not implement Story 5.3 versioned manifest schema, history, or manifest row generation; Story 5.4 named-waiver records or approvals; Story 5.5-5.9 per-domain conformance suites; Story 5.10 aggregated contract validation or CORE fixture manifest coverage; or Story 5.11 module/platform evidence separation.
  - [x] Do not change runtime tenant authorization, audit pairing, redaction replay, idempotency, projection freshness, event persistence, or client transport behavior.
  - [x] Do not add a new public package, CLI tool, background worker, durable artifact store, release-signing PKI infrastructure, database, export pipeline, globally runnable host, or admin UI surface for this story.
  - [x] Do not create a `src/Hexalith.Conversations.Conformance` source project unless it already exists; add builder code to the existing `tests/Hexalith.Conversations.Conformance.Tests` project or add a new `src/Hexalith.Conversations.Conformance/` project only if required — check first. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure`]
  - [x] Stop for ADR if implementation needs a new public error code, a new trust/freshness state, a new public conformance vocabulary term, a PKI or cryptographic signing requirement, a durable release evidence store, or a waiver-of-fail-closed/security/privacy rule.

## Dev Notes

### Epic and Business Context

- Epic 5 is the release-owner layer for compatibility, conformance, manifest traceability, waivers, release-gate proof, and module-versus-platform evidence. Story 5.2 creates the signed release conformance artifact type and minimal generation tooling; it does not aggregate a full manifest (5.3), manage waivers (5.4), or run per-domain proof suites (5.5-5.9). [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`]
- Story generation guardrail for Epic 5 is binding: keep the actor as release owner, keep the outcome as durable release evidence rather than informal test claims, and keep FR82/FR86 traceability visible. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`]
- FR82: The product can produce a signed conformance artifact for release gating. FR86: The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior. [Source: `_bmad-output/planning-artifacts/prd.md#FR82`; `_bmad-output/planning-artifacts/prd.md#FR86`]
- NFR63 is a nearby constraint: every release must produce a signed conformance artifact and versioned manifest mapping tests to FRs, NFRs, carry-forward commitments, pass criteria, waiver status, measurement method, and environment. Story 5.2 covers the artifact; Story 5.3 covers the versioned manifest rows. [Source: `_bmad-output/planning-artifacts/prd.md#NFR63`]

### Existing Surfaces to Reuse

- `ConformanceVocabulary.cs` already defines `ConformanceCheck` (11 CORE checks), `ConformanceOutcome` (`ready`/`degraded`/`blocked`/`unknown`), `ConformanceFailureClassification` (`conformant`/`product-invariant`/`infrastructure`/`configuration`/`unavailable-dependency`/`execution`). New `ReleaseGateStatus` and `ReleaseGateId` types must follow this exact pattern to stay consistent. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`]
- `ConformanceRunResultV1` and `ConformanceCheckResultV1` are the adopter-layer artifacts from Story 4.5. `ReleaseConformanceArtifactV1` wraps and extends these for the release-owner layer — it does not replace them. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs`; `src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs`]
- `ConformanceContractValidation.cs` already provides `RequiredSafeToken`, `RequiredSafeText`, `RequiredUtcTimestamp`, closed-token validator, and free-text leakage guards. All new record types must use these helpers at construction time. [Source: `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`]
- `AdopterConformanceSuite.cs` and `ConversationConformanceCoreFixtures.cs` (Story 4.5) are the input to the `ReleaseConformanceArtifactBuilder`. The builder maps per-check outcomes to release gate IDs where mappable. [Source: `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuite.cs`; `src/Hexalith.Conversations.Testing/Fixtures/ConversationConformanceCoreFixtures.cs`]
- `ContractSamples.cs` already participates in serialization, forbidden-surface, and content-safety scans. Register new conformance types there to get free scan coverage. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]
- `docs/release-evidence/` already exists and contains `contract-compatibility-policy.md`. The fixture artifact JSON file belongs there. [Source: `docs/release-evidence/contract-compatibility-policy.md`]
- `_bmad-output/implementation-artifacts/tests/test-summary.md` is updated each story; add Story 5.2 evidence there. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md`]

### Gate-to-Check Mapping

When the builder derives gate results from the `ConformanceRunResultV1`, use these conservative mappings. Gates not fully covered by the adopter suite must emit `unknown-accepted`:

| Release Gate ID | Mapped from Adopter Checks | Notes |
| --- | --- | --- |
| `tenant-isolation` | `tenant-binding` check outcome | Adopter suite covers CORE cross-tenant denial; full adversarial suite is Story 5.5 |
| `audit-integrity` | `governance-precondition` check outcome | Adopter suite covers audit-precondition path; full audit pairing suite is deferred |
| `redaction-non-leakage` | `governance-precondition` check outcome | Partial coverage; full redaction replay suite is Story 5.7 |
| `unsupported-schema-rejection` | `compatibility-discovery` check outcome | Adopter suite covers unsupported-schema rejection path |
| `projection-rebuild-determinism` | `projection-freshness` check outcome | Adopter suite covers freshness states; full rebuild suite is Story 5.9 context |
| `contract-compatibility` | `compatibility-discovery` + `error-envelope` check outcomes | Full manifest coverage is Story 5.10 |
| `provider-portability` | (no direct adopter check) → `unknown-accepted` | Full portability proof is Story 5.8 |

A gate status is `pass` when mapped check result outcome is `ready`; `fail` when mapped check outcome is `blocked` (unexpected, i.e. `ConformanceFailureClassification` is not `conformant`); `unknown-accepted` when no direct mapped check exists or the check evidence is partial. [Source: `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuiteTest.cs`]

### Files Likely to Touch

- New files:
  - `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs` — `ReleaseGateStatus` and `ReleaseGateId` closed vocabulary types and `ReleaseGateResultV1`.
  - `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs` — the release artifact type with `ValidateArtifact` and `OverallStatus`.
  - `src/Hexalith.Conversations.Conformance/Manifest/ReleaseConformanceArtifactBuilder.cs` (or inside `tests/Hexalith.Conversations.Conformance.Tests/` if no standalone Conformance source project exists — check `ls src/` first).
  - `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs`
  - `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs`
  - `docs/release-evidence/release-conformance-artifact-v1-fixture.json`

- Update files:
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` — register new types
  - `_bmad-output/implementation-artifacts/tests/test-summary.md` — Story 5.2 evidence entry

### Architecture Guardrails

- New conformance types must live in `src/Hexalith.Conversations.Contracts/Conformance/` to stay infrastructure-free and adopter-safe. The builder (which references the conformance suite runner) belongs in the conformance source project or tests project, not in Contracts. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Public contracts must not expose EventStore stream names, event positions, snapshots, projection topology, raw exception text, test framework namespaces, server routes, local paths, or unsafe free-text. All free-text fields must pass the existing forbidden-surface scan. [Source: `_bmad-output/planning-artifacts/architecture.md#API Pattern`]
- "Signed" in FR82 means a runner-attested artifact with bounded `SignerOrRunnerId` metadata — not a PKI/cryptographic signature. A cryptographic signing requirement would be an ADR-triggering infrastructure decision. Use `RequiredSafeToken` validation for `SignerOrRunnerId`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2 AC1`]
- Central Package Management is active. Do not add package versions directly to `.csproj` files. [Source: `Directory.Packages.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- SDK `10.0.300`, target `net10.0`, nullable enabled, implicit usings, warnings as errors. [Source: `global.json`; `Directory.Build.props`]

### Testing Requirements

- Primary test surface: contract-level tests for the new closed vocabularies, new record types, `ValidateArtifact` helper, `OverallStatus` computation, JSON shape, round-trip, additive tolerance, and content-safety. Secondary surface: generation tests proving the builder produces a structurally valid artifact from the CORE fixture conformance run.
- Use xUnit v3, Shouldly, deterministic synthetic fixtures, `AppContext.BaseDirectory`-based repo-root file reads, and `FakeTimeProvider` where timestamps are needed.
- Content-safety: register new types in `ContractSamples.cs`; do not introduce tenant IDs, Party IDs, provider payloads, conversation IDs, raw exception text, local paths, or business-record identifiers in test data.
- Targeted test filter for fast iteration: `FullyQualifiedName~ReleaseConformance|FullyQualifiedName~ReleaseGate`.
- Run `dotnet test Hexalith.Conversations.Conformance.Tests` in full after each change to catch regressions in the adopter-suite and core-fixture tests.

### Previous Story Intelligence

- Story 5.1 published `docs/release-evidence/contract-compatibility-policy.md` and a local policy summary with stable release-evidence classification IDs. Build on that pattern: small content-safe file, canonical README links (if any needed), deterministic docs validation, content-safety scan. [Source: `_bmad-output/implementation-artifacts/5-1-publish-contract-compatibility-and-deprecation-policy.md`]
- Story 4.5 established `ConformanceRunResultV1`, the adopter suite, and the CORE fixture. Story 5.2 builds on these directly — the builder takes a `ConformanceRunResultV1` as input. Do not re-implement the adopter-suite behavior or fork the conformance contract model. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md#Story 4.5`]
- Recurring lesson from docs-validation tests: use the `AppContext.BaseDirectory`-based approach to find repo files deterministically; do not hardcode absolute paths. [Source: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs`]
- Recurring content-safety lesson: closed machine identifiers like `tenant-isolation`, `contract-compatibility`, and `provider-portability` are valid safe tokens; scan for unsafe raw identifiers (real tenant IDs, conversation IDs, Party IDs, local paths, raw exceptions) not for gate name strings. [Source: `_bmad-output/implementation-artifacts/5-1-publish-contract-compatibility-and-deprecation-policy.md#Architecture and Safety Guardrails`]
- Recent git history: `feat(story-5.1): Publish contract compatibility and deprecation policy`, `fix(tests): Make privileged-justification tests time-independent`, `feat(story-4.7): Publish developer integration guide and API examples`. Follow the same focused-test and evidence-summary pattern and use `FakeTimeProvider` for deterministic timestamps. [Source: `git log --oneline -8`]

### Latest Technical Notes

- Solution currently has 1032 passing tests. New story should add conformance contract tests (est. 20-30 in `Contracts.Tests`) and generation tests (est. 8-12 in `Conformance.Tests`). Full solution pass count should increase by ~30-40.
- `dotnet test --filter` supports `FullyQualifiedName~...` selection; use targeted filters for fast docs/conformance validation before full solution runs. [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md#Story 5.1`]
- No external library upgrade is needed. The builder uses only types already in the Contracts project and the Testing/Conformance fixture layer.

### Out of Scope

- Versioned manifest schema, manifest row generation, manifest history, full test-to-requirement traceability tables (Story 5.3).
- Named-waiver records, waiver approval workflow, waiver status aggregation (Story 5.4).
- Full adversarial tenant isolation conformance suite (Story 5.5).
- Full idempotency conformance suite (Story 5.6).
- Full redaction replay conformance suite (Story 5.7).
- Provider portability proof (Story 5.8), event schema evolution proof (Story 5.9), aggregated contract test manifest rows (Story 5.10).
- Module-vs-platform evidence separation (Story 5.11).
- PKI or cryptographic signing infrastructure, release-signing tools, or hardware security module (HSM) integration.
- New durable artifact stores, database tables, background worker queues, export pipelines, CLI tools, or globally runnable hosts.
- New public compatibility vocabulary, new public freshness state, new public trust state, or any change to existing closed vocabularies.
- New runtime tenant authorization, audit pairing, redaction replay, idempotency, projection freshness, event persistence, or client transport behavior.

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 5: Conformance, Compatibility, and Release Evidence`
- `_bmad-output/planning-artifacts/epics.md#Story 5.2: Generate Signed Release Conformance Artifact`
- `_bmad-output/planning-artifacts/epics.md#Story 5.3: Maintain Versioned Conformance Manifest with Traceability`
- `_bmad-output/planning-artifacts/epics.md#Story 5.4: Support Named Waivers for Release-Gate Exceptions`
- `_bmad-output/planning-artifacts/prd.md#FR82`
- `_bmad-output/planning-artifacts/prd.md#FR86`
- `_bmad-output/planning-artifacts/prd.md#NFR63`
- `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- `_bmad-output/planning-artifacts/architecture.md#API Pattern`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/5-1-publish-contract-compatibility-and-deprecation-policy.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/project-context.md`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceRunResultV1.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`
- `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations.Testing/Fixtures/ConversationConformanceCoreFixtures.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuite.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuiteTest.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/CoreFixtureContentSafetyTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceContractsTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `docs/release-evidence/contract-compatibility-policy.md`
- `docs/release-evidence/release-conformance-artifact-v1-fixture.json` (new, created in this story)
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- All ACs implemented: `ReleaseGateStatus` (4 values), `ReleaseGateId` (7 gates), `ReleaseGateResultV1`, `ReleaseConformanceArtifactV1` with computed `OverallStatus` and static `ValidateArtifact`.
- Builder placed in `tests/Hexalith.Conversations.Conformance.Tests/` (no standalone Conformance source project exists).
- Gate-to-check mapping follows the conservative table from Dev Notes; `provider-portability` always emits `unknown-accepted`.
- Full solution: 1074 passing tests (up from 1032); 0 warnings, 0 errors.
- Code review (2026-05-23): 1 HIGH (empty File List — now fixed), 1 MEDIUM (misleading test name — now renamed to `ValidateArtifactShouldReturnNoErrorsForValidArtifact`), 2 LOW (dead validation branches — spec-required, left as-is; null `waiverReference` serialization — left as-is). No CRITICAL issues; story status remains `done`.

### File List

- `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs` (new) — `ReleaseGateStatus`, `ReleaseGateId`, and `ReleaseGateResultV1` closed-vocabulary types with content-safe field validation
- `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs` (new) — release artifact sealed record with all required evidence fields, computed `OverallStatus`, constructor-time gate-completeness validation, and static `ValidateArtifact`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (modified) — added `ReleaseGateStatusJsonConverter` and `ReleaseGateIdJsonConverter`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (modified) — registered `ReleaseGateStatus`, `ReleaseGateId`, `ReleaseGateResultV1`, and `ReleaseConformanceArtifactV1` samples for serialization, forbidden-surface, and content-safety scans
- `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` (new) — 31 contract tests covering closed-vocabulary completeness, JSON rejection, construction-time validation, `ValidateArtifact`, `OverallStatus` matrix, JSON shape, round-trip, additive tolerance, and fixture file validation
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactBuilder.cs` (new) — `ReleaseConformanceArtifactBuilder` class with deterministic gate-to-check mapping and injected `TimeProvider`
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` (new) — 10 generation tests covering builder output, gate coverage, determinism, content-safety, null rejection, and fixture artifact write
- `docs/release-evidence/release-conformance-artifact-v1-fixture.json` (new) — committed synthetic deterministic fixture artifact for schema validation and machine-readability proof
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified) — Story 5.2 evidence entry added
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — Story 5.2 status synced to `done`

## Story Context Validation

- Checklist reviewed: `.claude/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 5, Story 5.2 ACs, downstream Epic 5 story boundaries (5.3, 5.4, 5.5-5.11), and the Epic 5 story-generation guardrail.
  - Loaded `{prd_content}` from FR82, FR86, NFR63 in `_bmad-output/planning-artifacts/prd.md`.
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on FR82-FR94 structure mapping, conformance project boundaries, public API boundaries, shared vocabulary rule, ADR triggers, and `docs/release-evidence` placement.
  - Loaded persistent project context from `_bmad-output/project-context.md`, including .NET 10 / SDK 10.0.300, central package management, contract/client/test boundaries, fail-closed rules, content-safety rules, and root-level-only submodule policy.
  - Loaded previous story intelligence from `5-1-publish-contract-compatibility-and-deprecation-policy.md`, `test-summary.md` (Story 4.5 and 5.1 sections), `readiness-gates.md`, existing conformance vocabulary types, existing `ConformanceRunResultV1`, `AdopterConformanceSuite`, and `ConversationConformanceCoreFixtures`.
  - Loaded git history (8 recent commits).
- Checklist-driven guardrails applied:
  - Pointed dev agent at concrete existing conformance vocabulary and `ConformanceRunResultV1` so it extends rather than reinvents.
  - Made "signed" concrete as runner-attested (`SignerOrRunnerId` token) to avoid ADR-triggering PKI infrastructure.
  - Included gate-to-check mapping table so the builder has precise, unambiguous derivation logic.
  - Added explicit story boundary: 5.3/5.4 manifest/waiver work is out of scope; `unknown-accepted` is the correct status for gates without full adopter-suite coverage.
  - Added content-safety, ADR-stop-condition, and no-nested-submodule guardrails.
  - Added targeted and full validation commands plus test-summary evidence expectations.
