---
baseline_commit: ceb7fbe958a6b89a3b1ad01a3c98252cc766b4fe
---

# Story 1.1: Pin the conformance oracle green on `main` and snapshot the public contract shape

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release owner,
I want the existing release-gate conformance suite pinned and proven green on unmodified `main` with a captured public-contract-shape snapshot,
so that every later refactor in the Boilerplate Reduction initiative has a trusted, unchanging behavioral baseline to diff against.

> **Initiative context (read first):** This is **Story 1.1 of a behavior-preservation refactor** (Conversations Boilerplate Reduction). This story moves **no production code**. Its entire job is to establish the *trusted oracle* — run the existing conformance suite, prove it green, freeze a baseline record and a public-contract-shape snapshot, and classify which suites are safe to refactor against. Everything downstream (Epics 2–5) diffs against the artifacts this story produces. Do **not** "fix", refactor, or improve any suite, contract, or production file here — if a suite is internally coupled, you *record and flag it* (AC3), you do not decouple it (that is Story 1.3).

## Acceptance Criteria

### AC1 — Suite is green on unmodified `main` and recorded as the named baseline

**Given** unmodified `main`
**When** the 14 `*ConformanceSuiteTest` classes under `tests/Hexalith.Conversations.Conformance.Tests` are run
**Then** all pass (100% green)
**And** the run is recorded as the named behavior-preservation baseline referenced by FR-20 (a committed artifact capturing: the commit SHA of `main`, the enumerated 14 suite class names, total test count, pass/fail tally = all pass, and the run date).

### AC2 — Public contract-shape snapshot captured and stored

**Given** the baseline run
**When** the public/adopter-facing contract surface is enumerated (the **exported public types of the `Hexalith.Conversations.Contracts` assembly** — public command / query / event / projection / error / governance shapes, including member names, types, and enum members)
**Then** a deterministic, diffable contract-shape snapshot artifact is captured and stored for later diffing (referenced by Story 5.1)
**And** it explicitly covers the public envelopes of all six release-gate behavior areas: **tenant isolation, governance/audit, idempotency, redaction, projection freshness, contract validation**.

### AC3 — Oracle survivability assessed; internally-coupled suites flagged into Story 1.3

**Given** the conformance suites and the `Hexalith.Conversations.Conformance.Tests` project itself
**When** their dependencies are inspected (project references + `using` directives + instantiated types)
**Then** each suite is classified **public-surface-only** (depends only on `Hexalith.Conversations.Contracts` / `.Client` / `.Testing` public types, HTTP, or `DomainResult`) **or internally-coupled** (depends on `Hexalith.Conversations.Server.*` plumbing types or any internal/reflection access)
**And** every suite found internally coupled is flagged into **Story 1.3** with its file path and the specific coupling, so the oracle can be made refactor-survivable before plumbing moves
**And** the known internal coupling of the **`Conformance.Tests` project reference to `Hexalith.Conversations.Server`** is recorded as a survivability risk for Story 1.3.

### AC4 — Artifacts are committed and self-describing

**Given** the baseline record and the contract-shape snapshot
**Then** both are committed under `docs/release-evidence/` (the existing release-evidence location) with a short header explaining what they are, how they were generated (exact command), and that they are the FR-20 / Story 5.1 baseline — **do not** leave them as throwaway console output.

## Tasks / Subtasks

- [x] **Task 1 — Verify and pin the suite green on unmodified `main` (AC1)**
  - [x] Confirm the working tree for `src/` and `tests/` is clean (no local edits) so the baseline truly reflects `main`. Capture the current commit SHA (`git rev-parse HEAD`).
  - [x] Restore + build the conformance test project (and its dependencies) and run it:
    `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`
  - [x] Confirm **all 14 `*ConformanceSuiteTest` classes pass, 100% green**. Record total test count and pass tally. If anything is red on unmodified `main`, STOP and report — a red oracle invalidates the entire baseline premise (do not "fix" tests to make them pass).
  - [x] Write the named baseline record (see Task 4) capturing commit SHA, the 14 suite names, test count, all-pass result, and run date.
- [x] **Task 2 — Capture the public contract-shape snapshot (AC2)**
  - [x] Enumerate the exported public surface of the `Hexalith.Conversations.Contracts` assembly: every public type across the `Commands`, `Events`, `Queries`, `Projections`, `Errors`, `Governance`, `Identifiers`, `Participants`, `Results`, `Versioning`, and `Conformance` namespaces, including public members, property types, and enum member names. (Reflection over the built `Hexalith.Conversations.Contracts.dll` is the most robust approach; ordering must be deterministic — sort by namespace then type then member.)
  - [x] Emit a deterministic, diff-friendly artifact (stable ordering, normalized formatting) so a future run (Story 5.1) can diff byte-for-byte. Prefer a generated JSON or sorted text manifest, not hand-curation.
  - [x] Verify coverage of the six release-gate behavior areas' public envelopes; annotate in the artifact which namespaces/types back each area (tenant isolation, governance/audit, idempotency, redaction, projection freshness, contract validation).
  - [x] Prefer wiring this as a repeatable test/generator (mirroring the existing `ReleaseConformanceArtifactGenerationTest` pattern that writes into `docs/release-evidence/`) over a one-shot manual dump, so Story 5.1 can re-run it identically.
- [x] **Task 3 — Classify suites for refactor-survivability and flag couplings into Story 1.3 (AC3)**
  - [x] For each of the 14 suite classes, inspect `using` directives and instantiated types; classify public-surface-only vs internally-coupled (see Dev Notes for the pre-analyzed classification — verify it, do not trust it blindly).
  - [x] Record the confirmed internally-coupled suites — **`TelemetryCardinalityConformanceSuiteTest`** and **`TelemetryRedactionConformanceSuiteTest`** (both `using Hexalith.Conversations.Server.Diagnostics` / `Server.TenantAccess`) — into a Story 1.3 hand-off note with file paths and the exact coupling. **Plus a verified discrepancy:** `ConformanceStatusConformanceSuiteTest` (table said public-surface-only) is internally coupled through its engine/fixtures — also flagged.
  - [x] Record the project-level coupling: `Hexalith.Conversations.Conformance.Tests.csproj` references `Hexalith.Conversations.Server` — a survivability risk because the oracle compiles against the plumbing assembly being refactored. Flag for Story 1.3 triage.
  - [x] Do **not** decouple anything in this story. Classification + flagging only.
- [x] **Task 4 — Commit self-describing artifacts (AC4)**
  - [x] Write the baseline record + contract-shape snapshot under `docs/release-evidence/` with a header (what it is, generation command, that it is the FR-20 / Story 5.1 baseline).
  - [x] Write the Story 1.3 coupling hand-off note (lives in `release-baseline-v1.json` `oracleSurvivability` + `release-baseline-v1.md` AC3 section, and the Dev Agent Record below).
  - [x] Stage only intended files; respect the submodule rules (do not touch sibling submodules; do not recurse submodules).

## Dev Notes

### What this story is (and is NOT)

- **IS:** a gate-zero "freeze the baseline" story. Run the existing oracle, prove green, snapshot the public contract shape, classify survivability. Output = committed evidence artifacts + a Story 1.3 flag list.
- **IS NOT:** a refactor. No production source under `src/` changes. No conformance suite is rewritten, decoupled, strengthened, or deleted. No contract shape changes. If you feel the urge to "improve" something, that is a different story (1.2 backfills blind spots; 1.3 decouples coupled tests).
- The downstream contract: **Story 5.1** diffs the final public contract shape against *this story's* snapshot; **FR-20** references *this story's* named baseline run. If these artifacts are sloppy or non-deterministic, the whole behavior-preservation gate is undermined.

### The 14 conformance suites (the oracle) — pre-analyzed

All under `tests/Hexalith.Conversations.Conformance.Tests/` (project: `Hexalith.Conversations.Conformance.Tests.csproj`). The actual xUnit test classes are the `*ConformanceSuiteTest.cs` files; the companion `*ConformanceSuite.cs` / `*Fixtures.cs` files are the shared scenario engines/fixtures they drive (not separate test classes — don't double-count them).

| # | Suite test class | Release-gate behavior | Survivability |
|---|------------------|-----------------------|---------------|
| 1 | `TenantIsolationConformanceSuiteTest` | Tenant isolation / fail-closed (NFR3) | public-surface-only |
| 2 | `IdempotencyConformanceSuiteTest` | Idempotency | public-surface-only |
| 3 | `ContractValidationConformanceSuiteTest` | Contract validation / compatibility | public-surface-only |
| 4 | `RedactionConformanceSuiteTest` | Redaction non-leakage | public-surface-only |
| 5 | `EventSchemaEvolutionConformanceSuiteTest` | Unsupported-schema rejection / schema evolution | public-surface-only |
| 6 | `ProviderPortabilityConformanceSuiteTest` | Provider portability | public-surface-only |
| 7 | `ConformanceStatusConformanceSuiteTest` | Conformance-status gate | public-surface-only |
| 8 | `PlatformEvidenceSeparationConformanceSuiteTest` | Platform/evidence separation | public-surface-only |
| 9 | `BuyerAcceptanceConformanceSuiteTest` | Buyer-acceptance governance precondition | public-surface-only |
| 10 | `ReleaseScopeConformanceSuiteTest` | Release-scope governance | public-surface-only |
| 11 | `SecondAdopterConformanceSuiteTest` | Second-adopter governance precondition | public-surface-only |
| 12 | `AdopterConformanceSuiteTest` | Adopter integration surface (tenant binding, idempotency, projection freshness, compatibility, error envelope) | public-surface-only |
| 13 | `TelemetryCardinalityConformanceSuiteTest` | Operational telemetry cardinality budgets | **internally coupled → flag to Story 1.3** |
| 14 | `TelemetryRedactionConformanceSuiteTest` | Operational telemetry redaction | **internally coupled → flag to Story 1.3** |

- **Suites 1–12** depend only on the public `Hexalith.Conversations.Contracts` surface (primarily the `Contracts.Conformance` types) plus `Hexalith.Conversations.Testing` fixtures — they survive the refactor.
- **Suites 13–14** directly `using Hexalith.Conversations.Server.Diagnostics` (and `Server.TenantAccess`) and instantiate internal server diagnostic types/enums (e.g. `ConversationCommandRejectionClass`, `ConversationTenantDenialClass`, the server-side `TelemetryCardinalityConformanceSuite` / `TelemetryRedactionConformanceSuite`). These bind to plumbing the refactor will move → **they are the AC3 "internally coupled" finds for Story 1.3.** Verify this by reading the two files' `using` blocks before reporting (don't trust this table alone).

### Project-level coupling (record for AC3 / Story 1.3)

`tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` references **four** projects:
`...Contracts`, `...Client`, `...Testing`, **and `...Server`**.

The `Server` project reference is the structural reason the two telemetry suites can compile against `Server.Diagnostics`. For the oracle to be fully refactor-survivable, the public-surface suites should not transitively depend on the plumbing assembly. **Record this project reference as a survivability risk for Story 1.3** — do not remove it here (removing it would break the two telemetry suites' compilation, which is exactly Story 1.3's job to resolve).

### Existing tooling to REUSE (do not reinvent)

The release-evidence machinery already exists — mirror it; don't hand-roll a parallel mechanism.

- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactBuilder.cs` — builds a deterministic `ReleaseConformanceArtifactV1` (round-trip-safe JSON).
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` — **the pattern to copy for the snapshot generator**: it finds the repository root, creates `docs/release-evidence/`, serializes indented JSON, writes a fixture file, then re-parses + re-validates it. The contract-shape snapshot generator should follow this same shape (deterministic write into `docs/release-evidence/`, validated on the way out).
- `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` — validates the committed `docs/release-evidence/conformance-manifest-v1-fixture.json`.
- Existing committed artifacts in `docs/release-evidence/`: `release-conformance-artifact-v1-fixture.json`, `conformance-manifest-v1-fixture.json`, `manifest.schema.json`, `contract-compatibility-policy.md`, waiver fixtures/schema. **Place the new baseline + contract-shape snapshot alongside these** (e.g. `release-baseline-v1.(json|md)` and `public-contract-shape-baseline-v1.json` — match the existing naming/casing convention). Do not overwrite the existing fixtures.

> Note: the existing `release-conformance-artifact-v1-fixture.json` is a *release-gate-result* artifact (per-gate pass/fail), **not** the public-contract-shape snapshot this story needs. The contract-shape snapshot (exported Contracts types/members) is a **new** artifact. Don't conflate them.

### Public contract surface to snapshot (AC2) — `Hexalith.Conversations.Contracts`

Project: `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj` (packable; `IsPackable=true`, PackageId `Hexalith.Conversations.Contracts`). Public namespaces and representative types to enumerate (snapshot the **actual exported types via reflection** — this list is a coverage check, not the source of truth):

- `…Contracts.Commands` — `CreateConversationCommand`, `AppendMessageCommand`, `AddParticipantCommand`, `CloseConversationCommand`, `ArchiveConversationCommand`, `AttachFileReferenceCommand`, `UpdateConversationMetadataCommand`, `ReassignConversationProjectCommand`, `ConversationCommandMetadata`, …
- `…Contracts.Events` — `ConversationCreated`, `ConversationClosed`, `ConversationArchived`, `ConversationLifecycleChanged`, `MessageAppended`, `MessageContentRedacted`, `ParticipantAdded`, `FileReferenceAttached`, `RetentionPolicySet`, `ConversationContentMarkedSensitive`, `ConversationEventMetadata`, `ConversationLifecycleStatus`, …
- `…Contracts.Queries` — `GetConversationQuery`, `ListConversationsQuery`, `GetConversationAuditRecordQuery`, `GetConversationAtPointInTimeQuery`, result DTOs `ConversationDetailsV1`, `ConversationSummaryV1`, `ConversationListResult`, …
- `…Contracts.Projections` — `ConversationSummaryProjectionV1`, `ConversationDetailProjectionV1`, `ProjectionFreshness` / `ProjectionFreshnessV1`, `ProjectionFreshnessReasonCode`, …
- `…Contracts.Errors` — `ConversationError`, `ConversationErrorCode` (closed vocabulary), `ConversationErrorCategory`, `ConversationErrorCatalog`, …
- `…Contracts.Governance` — sensitivity/redaction/retention commands + results, `GovernanceAuditEvidence`, `GovernanceVocabularies`, …
- `…Contracts.Identifiers` — `TenantId`, `ConversationId`, `PartyId`, `MessageId`, `FileId`, `FolderId`, `ProjectId`, …
- `…Contracts.Conformance` — `ConformanceRunResultV1`, `ReleaseConformanceArtifactV1`, `ReleaseGateId`, `ConformanceManifestV1`, … (the oracle's own contract types).
- Also: `…Contracts.Participants`, `…Contracts.Results`, `…Contracts.Versioning`, `…Contracts.TrustStates`, `…Contracts.Diagnostics`.

**Six release-gate areas → public-envelope mapping** (use to satisfy AC2's coverage clause):
tenant isolation → `Identifiers.TenantId` + tenant-scoped command/query fields + tenant error codes;
governance/audit → `Governance.*` commands/results + `GovernanceAuditEvidence`;
idempotency → command metadata / `Results.ConversationCommandAcceptedResult` + idempotency error codes;
redaction → `Events.MessageContentRedacted`, `Governance` redaction command/result, redaction projections;
projection freshness → `Projections.ProjectionFreshness*`, `TrustStates.ProjectionTrustState`;
contract validation → `Versioning.*`, `Conformance.ContractValidation*`, `Errors.ConversationErrorCode`.

### How to run / verify (Tech stack)

- .NET `10.0`, target `net10.0`, SDK pinned `10.0.302` (see `global.json` / `Directory.Build.props`); nullable enabled, implicit usings, warnings-as-errors; Central Package Management via `Directory.Packages.props`.
- Test stack: **xUnit v3** (`xunit.v3`), **Shouldly**, `Microsoft.NET.Test.Sdk`, `coverlet.collector`.
- Run the oracle: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`.
- The solution is `Hexalith.Conversations.slnx` at repo root.
- This module (`src/Hexalith.Conversations.*`, `tests/Hexalith.Conversations.*`) lives at the **repository root**, NOT inside a git submodule. The sibling `Hexalith.*` directories (EventStore, Tenants, Parties, Commons, Folders, Projects, Memories, FrontComposer, Builds, AI.Tools) ARE submodules — leave them untouched for this story.

### Project Structure Notes

- Artifacts belong in the existing `docs/release-evidence/` directory (already holds the release/manifest/waiver fixtures + schemas). Match existing file naming/casing conventions.
- Keep generated snapshot output deterministic (sorted, normalized) — non-determinism here produces false diffs in Story 5.1 and erodes trust in the oracle.
- If you add a generator test, it belongs in `tests/Hexalith.Conversations.Conformance.Tests/` next to `ReleaseConformanceArtifactGenerationTest.cs`, following its repo-root-discovery + write + re-validate shape.
- Detected variance to record (not fix): the `Conformance.Tests` project's `Server` project reference + the two telemetry suites' `Server.Diagnostics` coupling are out of line with "public-surface-only oracle" — flagged to Story 1.3, deliberately left as-is here.

### Critical guardrails (from project-context.md)

- **Behavior preservation is the dominant gate (NFR1).** Never make the suite pass by weakening or deleting a test. If `main` is red, report it — do not paper over it.
- Do **not** persist or expose Parties personal data, raw EventStore envelopes, snapshot mechanics, or tenant/auth context in any artifact you emit. The existing release-evidence content-safety scans forbid fragments like `EventStore`, `snapshot`, provider payloads, raw exceptions, and drive paths — keep the new snapshot clean of those too (it should contain only public Conversations contract type/member names).
- Fail-closed tenant isolation, governance audit-pairing, idempotency, redaction replay, and projection freshness are release-gate concerns — this story *pins* them, it does not modify them.
- Submodule rule (repo CLAUDE.md): never recurse into nested submodules; initialize/update only root-level submodules. This story should not need any submodule operation at all.
- Greenfield latitude (removing plumbing-only tests) does **not** apply in this story — nothing is removed here.

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 1.1] — story statement + the three AC blocks (reproduced/expanded above).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Epic 1] — gate-zero oracle role; FR-1/FR-2 plus the non-FR oracle work FR-20 depends on; relation to Stories 1.2 (blind spots) and 1.3 (decouple coupled tests).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR-20] — full release-gate conformance suite must pass; public contract shapes unchanged or explicitly approved; no conformance test silently dropped.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#A] — boilerplate inventory; areas the later epics will move (queries/cursor, projections, diagnostics/telemetry, tenant-access).
- [Source: _bmad-output/project-context.md#Testing Rules] — "Keep conformance-style tests explicit and named; tenant isolation, audit pairing, idempotency, and redaction replay are release-gate concerns"; xUnit v3 / Shouldly / NSubstitute / Testcontainers.
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs] — repo-root discovery + deterministic write-into-`docs/release-evidence/` + re-validate pattern to mirror for the snapshot generator.
- [Source: tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj] — project references (Contracts/Client/Testing/**Server**) backing the AC3 survivability finding.
- [Source: src/Hexalith.Conversations.Contracts/] — the public assembly whose exported types AC2 snapshots.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (Claude Opus 4.8, 1M context)

### Debug Log References

- `dotnet test …Conformance.Tests.csproj` on unmodified `main` (commit `ceb7fbe`): **248 passed, 0 failed, 0 skipped**.
- `--filter "FullyQualifiedName~ConformanceSuiteTest"` (the 14 suite classes only): **214 passed, 0 failed**.
- Full project after adding the two test-only Story 1.1 files: **260 passed, 0 failed, 0 skipped** (248 baseline + 4 `PublicContractShapeSnapshotGenerationTest` + 8 `ReleaseBaselineValidationTest`).
- `--filter "FullyQualifiedName~PublicContractShapeSnapshotGenerationTest"`: **4 passed** (determinism, six-area coverage, content-safety, generate+round-trip).
- `--filter "FullyQualifiedName~ReleaseBaselineValidationTest"`: **8 passed** (committed-artifact validation: green-oracle record, 40-char `main` SHA, suite-class enumeration vs assembly, survivability-classification completeness, committed-snapshot type-count vs live surface drift guard, committed content-safety scan, cross-artifact type-count agreement).

### Completion Notes List

- **AC1 — oracle pinned green.** All 14 `*ConformanceSuiteTest` classes pass 100% on unmodified `main` (`ceb7fbe`, clean `src/`+`tests/` tree). No suite was modified, weakened, or "fixed". Recorded as the named FR-20 baseline in `docs/release-evidence/release-baseline-v1.json` (+ `.md` header).
- **AC2 — contract-shape snapshot captured.** Deterministic reflection snapshot of the `Hexalith.Conversations.Contracts` assembly = **196 exported public types** across 14 namespaces, sorted namespace→type→member for byte-stable diffing. Wired as a repeatable generator test mirroring `ReleaseConformanceArtifactGenerationTest` (repo-root discovery → write → re-read + re-validate), so Story 5.1 can re-run it identically. All six release-gate areas' namespace coverage is asserted; captured surface passes a content-safety scan. **Discovery:** Conversations closed vocabularies (`ConversationErrorCode`, etc.) use the Hexalith smart-enum record pattern, not CLR enums — members are captured as static properties (the full closed vocabulary is present; no CLR `enum` types exist in Contracts).
- **AC3 — survivability classified + Story 1.3 hand-off.** 11 suites are public-surface-only. **Internally-coupled (flagged to Story 1.3):** `TelemetryCardinalityConformanceSuiteTest`, `TelemetryRedactionConformanceSuiteTest` (both `using Server.Diagnostics`/`Server.TenantAccess`), and — **a verified discrepancy from the story's pre-analysis table** — `ConformanceStatusConformanceSuiteTest`: its test class is clean, but its engine `ConformanceStatusConformanceSuite.cs` and `ConformanceStatusConformanceFixtures.cs` depend on `Server.Diagnostics` (`ConversationConformanceStatusClassifier`/`ConversationConformanceStatusClass`). Also noted: the non-suite shared fixture `TelemetryDisclosureConformanceFixtures.cs` has the same coupling. The project-level `Conformance.Tests → Hexalith.Conversations.Server` reference is recorded as the structural survivability risk. **Nothing was decoupled** (that is Story 1.3's job).
- **AC4 — artifacts committed (staged) self-describing.** `release-baseline-v1.json`, `release-baseline-v1.md`, and `public-contract-shape-baseline-v1.json` written under the existing `docs/release-evidence/` with headers (what they are, exact generation commands, FR-20 / Story 5.1 pointers). Only intended files staged; no sibling submodule touched.
- **Scope guardrail honored:** zero production source under `src/` changed. The only code added is **two test-only files** in the conformance test project — the snapshot generator (`PublicContractShapeSnapshotGenerationTest.cs`) and a committed-artifact validator (`ReleaseBaselineValidationTest.cs`) that guards the on-disk baseline/snapshot against drift (the generator overwrites its output every run and never reads the committed file back, so the validator is what actually fails fast if the committed Story 5.1 baseline goes stale).

### File List

- `tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` (new) — repeatable contract-shape snapshot generator + determinism/coverage/content-safety tests (4 tests).
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` (new) — validates the COMMITTED on-disk evidence: green-oracle baseline record, 40-char `main` SHA, suite-class enumeration vs the live assembly (FR-20: no suite silently added/dropped), survivability-classification completeness, committed-snapshot-vs-live-surface drift guard, committed content-safety scan, and cross-artifact type-count agreement (8 tests).
- `docs/release-evidence/public-contract-shape-baseline-v1.json` (new) — AC2 deterministic public-contract-shape snapshot (196 types); Story 5.1 diff baseline.
- `docs/release-evidence/release-baseline-v1.json` (new) — AC1 named FR-20 baseline run record + AC3 oracle-survivability classification.
- `docs/release-evidence/release-baseline-v1.md` (new) — AC4 self-describing header for the above + Story 1.3 coupling hand-off.
- `_bmad-output/implementation-artifacts/1-1-pin-the-conformance-oracle-green-on-main-and-snapshot-the-public-contract-shape.md` (modified) — `baseline_commit` frontmatter, task checkboxes, Dev Agent Record, File List, Change Log, Status → review.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — story 1-1 status `ready-for-dev` → `in-progress` → `review`.

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-06-02 · **Outcome:** Approve (auto-fix applied)

**Verification performed (claims validated against reality, not trusted):**

- **AC1 — green oracle.** Re-ran `dotnet test …Conformance.Tests.csproj` → **260 passed, 0 failed, 0 skipped**. Confirmed exactly 14 `*ConformanceSuiteTest` classes on disk, matching the baseline enumeration. HEAD = `ceb7fbe…b4fe` = the recorded `baselineCommit`. ✅
- **AC2 — contract-shape snapshot.** Committed `public-contract-shape-baseline-v1.json` has `typeCount: 196` and 196 type entries; the live `Hexalith.Conversations.Contracts` exported surface is 196 (asserted green by `CommittedSnapshotTypeCountShouldMatchTheLiveExportedContractSurface`). Six release-gate areas present; content-safety scan passes on the captured `types` payload. ✅
- **AC3 — survivability classification.** Independently verified the `using` directives: `TelemetryCardinalityConformanceSuiteTest` + `TelemetryRedactionConformanceSuiteTest` couple to `Server.Diagnostics`/`Server.TenantAccess`; the flagged **discrepancy** is real — `ConformanceStatusConformanceSuiteTest.cs` is clean but its engine `ConformanceStatusConformanceSuite.cs` and `ConformanceStatusConformanceFixtures.cs` both `using Hexalith.Conversations.Server.Diagnostics`. The `.csproj` does reference `…Server`. Classification is accurate. ✅
- **AC4 — committed self-describing artifacts.** All three evidence files present under `docs/release-evidence/` with headers, generation commands, and FR-20 / Story 5.1 pointers. ✅

**Findings (all MEDIUM — documentation/transparency; no code defects; auto-fixed):**

1. **File List incomplete** — `ReleaseBaselineValidationTest.cs` (8 tests, materially part of Story 1.1) was present in the working tree but absent from the File List. → Added to File List.
2. **Stale test counts** — Debug Log / Change Log recorded 252 project tests (248 + 4); actual is 260 (+8 from the validator). → Corrected to 260 with the per-filter breakdown; scope-guardrail note updated to "two test-only files".
3. **Staging inconsistency** — the validator was untracked while every other Story 1.1 artifact was staged (AC4: "stage only intended files"). → Staged alongside the others.

**0 CRITICAL / 0 HIGH.** Implementation is correct and the oracle is green; the work was under-documented, not under-built. Status → **done**.

## Change Log

| Date | Change |
|------|--------|
| 2026-06-02 | Story 1.1 implemented. Pinned the 14-suite conformance oracle green on unmodified `main` (`ceb7fbe`; 214 suite tests / 248 project tests, all pass) and recorded the FR-20 named baseline. Captured a deterministic 196-type public-contract-shape snapshot of `Hexalith.Conversations.Contracts` (Story 5.1 diff baseline) via a repeatable generator test. Classified all 14 suites for refactor-survivability; flagged 3 internally-coupled suites (incl. a verified discrepancy: `ConformanceStatusConformanceSuiteTest`) and the project-level `Server` reference to Story 1.3. No production code changed. Status → review. |
| 2026-06-02 | Automated review (story-automator). Re-ran the oracle: **260 passed, 0 failed, 0 skipped** — still green. All four ACs verified against reality (oracle green, 196-type live surface matches the committed snapshot, AC3 coupling classification confirmed accurate incl. the `ConformanceStatusConformanceSuiteTest` discrepancy). Fixed documentation/transparency gaps: the second test-only file `ReleaseBaselineValidationTest.cs` (8 tests) was present in the working tree but missing from the File List and unstaged, and the recorded project-test total was stale (252). Added it to the File List, corrected counts to 260, updated the scope-guardrail note (two test-only files), and staged it alongside the other Story 1.1 artifacts. 0 critical issues. Status → done. |
