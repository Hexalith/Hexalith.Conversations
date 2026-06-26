---
baseline_commit: bd00544b23a71acb8531d006a13e456f7fca168f
---

# Story 4.2: Measure and record the minimal-module authoring cost (SM-2 baseline)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release owner,
I want the authoring cost of a minimal valid domain module on the template measured and recorded,
so that SM-2 has a concrete baseline proving the authoring surface is thinner.

This is a documentation and release-evidence story. It must measure the minimal module boundary handed off by Story 4.1, record a machine-readable baseline for Epic 5, and keep OQ-2's numeric target status honest. It must not add product runtime behavior, new public Conversations contracts, optional UI/Admin surfaces, or sibling submodule changes.

## Acceptance Criteria

**AC-1 - Create a versioned SM-2 measurement artifact.**
Given the Story 4.1 template,
When the minimal do-nothing-but-valid module is measured,
Then create `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json` and `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md` recording file count, LOC, counting method, included/excluded categories, and traceability to the template.
[Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-42-Measure-and-record-the-minimal-module-authoring-cost-SM-2-baseline; docs/domain-module-authoring-template.md#Minimal-Project-Skeleton; docs/release-evidence/thin-authoring-template-validation-v1.md#Story-42-handoff]

**AC-2 - Measure only the accepted minimal boundary.**
Given the Story 4.1 handoff,
When files and LOC are counted,
Then include only hand-authored files in the accepted SM-2 baseline categories: `Contracts`, `Client`, domain/core, `Server`, `AppHost`, `ServiceDefaults` only if present, `Testing` only if present, and focused test projects.
And exclude `Admin.Web`, FrontComposer trust components, publication subscribers, governance workflows, generated output, local developer artifacts, shared platform libraries, and sibling submodule source.
[Source: docs/release-evidence/thin-authoring-template-validation-v1.md#Story-42-handoff]

**AC-3 - Make the measurement reproducible.**
Given the recorded measurement,
When a reviewer reads the artifact,
Then it contains the exact file list or manifest rows used for the count, category for each counted file, per-file LOC, totals, and commands or deterministic steps sufficient to re-run the count.
And the method ignores `bin/`, `obj/`, generated output, package caches, and local machine paths.
[Source: docs/release-evidence/consume-promote-keep-inventory-v1.md#Counting-method; _bmad-output/implementation-artifacts/1-4-accept-the-canonical-consume-promote-keep-inventory-and-record-baseline-plumbing-loc.md#Ground-truth-LOC-map]

**AC-4 - Compare against the pre-initiative equivalent without overstating certainty.**
Given SM-2 compares the template baseline against a pre-initiative equivalent,
When the comparison is recorded,
Then the artifact records the pre-initiative basis, file count, LOC, confidence level, and whether the value is measured or estimated.
And if no exact pre-template minimal skeleton can be reconstructed from accepted artifacts, the comparison explicitly marks that limitation and keeps OQ-2's numeric target unconfirmed rather than presenting an estimate as a fact.
[Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#SM-2-New-module-authoring-cost; _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-08.md#Major-Issues-go-forward-readiness]

**AC-5 - Record the baseline for Epic 5 consumption.**
Given Epic 5 assembles the success-metric report,
When Story 4.2 completes,
Then the JSON artifact exposes stable fields that Story 5.3 can read for SM-2: template minimal file count, template minimal LOC, comparison values, reduction percentages, target assumption, OQ-2 status, measurement date, and source artifact references.
[Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-53-Assemble-the-success-metric-report-and-signable-attestation]

**AC-6 - Add drift-prevention validation.**
Given the measurement becomes release evidence,
When tests run,
Then add a focused validation test under `tests/Hexalith.Conversations.Contracts.Tests/Documentation/` that asserts the SM-2 artifacts exist, reference the Story 4.1 template and validation note, contain the accepted included/excluded category sets, reconcile per-file totals to recorded totals, expose Story 5.3-readable fields, and do not cite `bin/` or `obj/` as source-of-truth paths.
[Source: tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs]

**AC-7 - Preserve behavior and contract stability.**
Given this story is measurement evidence only,
When implementation is complete,
Then no production source behavior, public contract shape, AppHost runtime topology, package versions, or sibling submodule source are changed.
And `git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json` remains empty.
[Source: docs/release-evidence/release-baseline-v1.md#AC2-Public-contract-shape-snapshot; _bmad-output/project-context.md#Development-Workflow-Rules]

## Tasks / Subtasks

- [x] **Task 0 - Confirm the measurement boundary before counting.** (AC: 1, 2, 4)
  - [x] Read `docs/domain-module-authoring-template.md` and `docs/release-evidence/thin-authoring-template-validation-v1.md`.
  - [x] Record the included categories exactly as Story 4.1 handed them off.
  - [x] Record excluded categories exactly: `Admin.Web`, FrontComposer trust components, publication subscribers, governance workflows, generated output, local developer artifacts, shared platform libraries, and sibling submodule source.
  - [x] Do not expand the SM-2 baseline to make the metric look better or more complete.

- [x] **Task 1 - Stand up or reconstruct the minimal do-nothing module for measurement.** (AC: 1, 2, 3)
  - [x] Use a temporary measurement workspace or explicit manifest; do not add a product module under `src/`.
  - [x] Include only hand-authored project/source/test/docs files owned by a new module author.
  - [x] Exclude generated files, restored packages, build output, local IDE artifacts, and all sibling submodule files.
  - [x] Prove the skeleton is "valid" with a build or document exactly why the measurement is manifest-only.

- [x] **Task 2 - Create the SM-2 baseline evidence artifacts.** (AC: 1, 3, 4, 5)
  - [x] Create `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json`.
  - [x] Create `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md`.
  - [x] Record the measurement date, template reference, boundary reference, counted file manifest, per-category totals, file count, LOC, counting command, and exclusions.
  - [x] Record pre-initiative equivalent basis and confidence. If it is estimated, label it estimated and explain the evidence basis.
  - [x] Compute file and LOC reduction percentages only from stated values; do not imply OQ-2 is resolved unless an approved decision exists.

- [x] **Task 3 - Add focused artifact validation.** (AC: 3, 5, 6)
  - [x] Add `tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` or extend the existing documentation test if that keeps the validation clearer.
  - [x] Assert JSON and MD artifacts exist and are internally consistent.
  - [x] Assert totals equal the sum of manifest rows and categories match the Story 4.1 boundary.
  - [x] Assert the artifact exposes stable Story 5.3 fields: `templateMinimal`, `preInitiativeEquivalent`, `comparison`, `oq2Status`, and source references.
  - [x] Assert the artifacts do not cite `bin/`, `obj/`, generated output, or absolute local machine paths as evidence.

- [x] **Task 4 - Verify and finalize.** (AC: 6, 7)
  - [x] Run the focused documentation test. If VSTest socket creation is blocked in this sandbox, build the test project and run the xUnit v3 executable directly with `-class`.
  - [x] Run `dotnet build tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj -c Release --no-restore /nr:false /m:1`.
  - [x] Run `git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json` and confirm it is empty.
  - [x] Update Dev Agent Record last with exact commands, results, measured values, and any OQ-2 limitation.

## Dev Notes

### Implementation scope

This story should produce release evidence and tests only. Expected file changes are:

- New `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json`
- New `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md`
- New or updated focused documentation validation test under `tests/Hexalith.Conversations.Contracts.Tests/Documentation/`
- This story file and `sprint-status.yaml`

Do not change product runtime source, public contracts, package versions, AppHost topology, shared submodules, or generated output. If a temporary measurement workspace is needed, keep it outside product `src/` and commit only the evidence artifacts and validation tests.

### Measurement boundary from Story 4.1

Story 4.1 explicitly handed this story the SM-2 scope. Count hand-authored files and LOC in these categories when present: `Contracts`, `Client`, domain/core, `Server`, `AppHost`, `ServiceDefaults`, `Testing`, and focused test projects. Exclude optional surfaces unless a later approved story brings them into scope: `Admin.Web`, FrontComposer trust components, publication subscribers, governance workflows, generated output, local developer artifacts, shared platform libraries, and sibling submodule source. [Source: docs/release-evidence/thin-authoring-template-validation-v1.md#Story-42-handoff]

The template states the same boundary and notes that Story 4.2 can measure the minimal authoring cost by counting those SM-2 baseline categories. Do not redefine the categories during implementation. [Source: docs/domain-module-authoring-template.md#Minimal-Project-Skeleton]

### Suggested artifact shape

Use a JSON structure that Story 5.3 can consume without prose parsing. Recommended top-level fields:

- `artifact`: `minimal-module-authoring-cost-sm2-baseline`
- `version`: `1`
- `status`: `accepted`
- `measurementDate`
- `templateReference`
- `boundaryReference`
- `countingMethod`
- `includedCategories`
- `excludedCategories`
- `templateMinimal`: file count, LOC, category totals, manifest rows
- `preInitiativeEquivalent`: file count, LOC, basis, confidence, measured-vs-estimated flag
- `comparison`: file reduction percentage, LOC reduction percentage, assumed target, target status
- `oq2Status`: expected value is still `unconfirmed` unless an approved OQ-2 decision exists
- `story5Reference`: `Story 5.3`

The paired `.md` should explain the method, list the totals, state the comparison honestly, and point Epic 5 to the JSON as the machine-readable source.

### Pre-initiative comparison guardrail

The PRD's SM-2 target is currently an assumption: `>=50% fewer files for a minimal module`; OQ-2 remains open. Story 4.2 must compare against the pre-initiative equivalent, but it must not turn an unverified reconstruction into a false fact. If the pre-template minimal equivalent is derived from accepted baseline artifacts rather than measured from an actual buildable skeleton, record the basis and confidence explicitly. [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#Success-Metrics; _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-08.md#Major-Issues-go-forward-readiness]

Story 1.4 is the model for measurement honesty: it corrected the first-pass `~18,000 LOC / ~50%` estimate down to `13,289 LOC / 37.15%` with a recorded rationale instead of rounding to flatter the metric. Use the same discipline for SM-2. [Source: docs/release-evidence/consume-promote-keep-inventory-v1.md#Plumbing-derivation]

### Existing validation patterns to reuse

`DomainModuleAuthoringTemplateValidationTest` already reads repository docs, checks required anchors, validates optional/excluded SM-2 scope fragments, and forbids `obj/`/`bin/` source-of-truth paths. Follow that style: xUnit v3, Shouldly, repository-root discovery from `AppContext.BaseDirectory`, and focused assertions over durable artifact invariants rather than entire prose. [Source: tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs]

For machine-readable release evidence, mirror the approach used by `ConsumePromoteKeepInventoryValidationTest`: validate internal consistency, category coverage, sums, and governance fields; do not make tests depend on fragile prose. [Source: tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs]

### Verification guidance

Story 4.1 found that `dotnet test` may be blocked by VSTest socket permission in this sandbox. If that happens again, build the test project and run the compiled xUnit v3 executable directly, for example:

```sh
dotnet build tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj -c Release --no-restore /nr:false /m:1
tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests -class Hexalith.Conversations.Contracts.Tests.Documentation.MinimalModuleAuthoringCostBaselineValidationTest
```

Also verify the public contract-shape baseline remains untouched:

```sh
git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json
```

### Previous story intelligence

Story 4.1 completed the template and validation evidence, added six focused documentation tests, and preserved public contract shape. Its senior review fixed stale test-count notes, so update Dev Agent Record last and use exact command results. [Source: _bmad-output/implementation-artifacts/4-1-document-the-thin-authoring-template-validated-against-post-refactor-conversations.md#Senior-Developer-Review-AI]

Epic 3 retrospective carry-forward specifically says Story 4.2 must execute against the accepted Story 4.1 measurement boundary without adding optional surfaces into SM-2. Sprint status repeats that action item. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#action_items]

Current working tree note at story creation time: `Hexalith.Commons` is already modified and `_bmad-output/story-automator/` is untracked. Treat those as existing out-of-scope state; do not reset or fold them into this story. [Source: git status --short]

### Project Structure Notes

- Keep release evidence under `docs/release-evidence/` and use a `-v1` suffix for accepted baseline artifacts.
- Keep documentation validation tests under `tests/Hexalith.Conversations.Contracts.Tests/Documentation/`.
- Do not add a new product module under `src/` for this measurement.
- Do not edit generated output or count generated output as authoring cost.
- Do not initialize nested submodules recursively; root-level submodule policy applies.

### Latest Technical Specifics

No external web research is required for this story. The implementation uses local repository-pinned conventions and documentation artifacts: .NET `10.0`, nullable/implicit usings/warnings-as-errors, central package management, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and the already validated Story 4.1 template/evidence. [Source: _bmad-output/project-context.md#Technology-Stack--Versions; _bmad-output/implementation-artifacts/4-1-document-the-thin-authoring-template-validated-against-post-refactor-conversations.md#Latest-Technical-Specifics]

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-42-Measure-and-record-the-minimal-module-authoring-cost-SM-2-baseline]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-19-New-module-authoring-cost-is-measured]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#SM-2-New-module-authoring-cost]
- [Source: docs/domain-module-authoring-template.md]
- [Source: docs/release-evidence/thin-authoring-template-validation-v1.md]
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.md]
- [Source: docs/release-evidence/release-baseline-v1.md]
- [Source: tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs]
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs]
- [Source: _bmad-output/implementation-artifacts/4-1-document-the-thin-authoring-template-validated-against-post-refactor-conversations.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: added `MinimalModuleAuthoringCostBaselineValidationTest`; initial direct xUnit run failed because the SM-2 JSON/Markdown artifacts did not exist.
- VSTest runner is blocked in this sandbox by socket permission; used compiled xUnit v3 executables directly per story guidance.
- Wider regression run exposed stale `ScaffoldSmokeTest` project-reference expectations for prior completed Commons/EventStore promotion references; updated the integration guard to match current project files.
- Admin Web xUnit lane still cannot initialize its local web host because socket creation is denied while selecting a free port; recorded as environment-limited, not a Story 4.2 product change.

### Completion Notes List

- Created versioned SM-2 evidence artifacts: `minimal-module-authoring-cost-sm2-baseline-v1.json` and `.md`.
- Recorded accepted Story 4.1 measurement boundary exactly: included `Contracts`, `Client`, domain/core, `Server`, `AppHost`, optional `ServiceDefaults`/`Testing` only if present, and focused test projects; excluded optional Admin/Web, FrontComposer trust, publication subscribers, governance workflows, generated/local artifacts, shared platform libraries, and sibling submodule source.
- Reconstructed the minimal module as an explicit manifest only; no product module was added under `src/`.
- Measured template minimal baseline: 29 files, 468 LOC.
- Recorded pre-initiative equivalent as estimated, low confidence: 58 files, 1,460 LOC.
- Computed directional reductions from stated values only: 50.00% files, 67.95% LOC.
- Kept OQ-2 status `unconfirmed`; the artifact does not claim the SM-2 target is resolved.
- Added focused documentation validation covering artifact existence, category boundary, manifest/totals reconciliation, Story 5.3-readable fields, and evidence-path safety.

### File List

- `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json`
- `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md`
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs`
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`
- `_bmad-output/implementation-artifacts/4-2-measure-and-record-the-minimal-module-authoring-cost-sm-2-baseline.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-26: Added SM-2 baseline release-evidence artifacts and focused validation.
- 2026-06-26: Updated stale scaffold project-reference expectations so the integration regression lane reflects the current completed Epic 3 promotion dependencies.
- 2026-06-26: Senior code review (AI) — corrected stale verification counts (focused validator 5/5 → 7/7, Contracts suite 616/616 → 618/618); no CRITICAL issues; status moved review → done.

### Verification Results

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter FullyQualifiedName~MinimalModuleAuthoringCostBaselineValidationTest --no-restore /nr:false /m:1` - aborted by VSTest socket permission.
- `dotnet build tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj -c Debug --no-restore /nr:false /m:1` - passed, 0 warnings.
- `tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0/Hexalith.Conversations.Contracts.Tests -class Hexalith.Conversations.Contracts.Tests.Documentation.MinimalModuleAuthoringCostBaselineValidationTest` - passed, 7/7.
- `dotnet build tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj -c Release --no-restore /nr:false /m:1` - passed, 0 warnings.
- `tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests -class Hexalith.Conversations.Contracts.Tests.Documentation.MinimalModuleAuthoringCostBaselineValidationTest` - passed, 7/7.
- `tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests` - passed, 618/618.
- `dotnet build tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj -c Release --no-restore /nr:false /m:1` - passed, 0 warnings.
- `tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests` - passed, 8/8.
- Release xUnit executables passed for non-Admin lanes: Conversations 185/185, Client 29/29, Contracts 618/618, ServiceDefaults 7/7, AppHost 7/7, Server 610/610, Conformance 361/361, Integration 8/8.
- `tests/Hexalith.Conversations.Admin.Web.Tests/bin/Release/net10.0/Hexalith.Conversations.Admin.Web.Tests` - environment-limited: 2 fixture failures from `System.Net.Sockets.SocketException (13): Permission denied` while allocating a local port; 12 tests not failing.
- `git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json` - empty.

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot — 2026-06-26
**Outcome:** Approved (status review → done). No CRITICAL issues found. All seven Acceptance Criteria are genuinely implemented and every task marked `[x]` was verified against the committed artifacts.

### Independently verified

- **Manifest reconciliation (AC-2, AC-3):** All 29 manifest rows sum to 468 LOC; per-category totals (Contracts 7/90, Client 4/57, domain/core 4/72, Server 7/117, AppHost 3/56, ServiceDefaults 0/0, Testing 0/0, focused tests 4/76) reconcile exactly to the recorded totals. The `incrementalLocalCapabilityEstimate` rows sum to +29 files / +992 LOC, which composes with the 29/468 baseline to the recorded pre-initiative 58/1,460.
- **Comparison math (AC-4):** File reduction `(58-29)/58 = 50.00%` and LOC reduction `(1460-468)/1460 = 67.95%` derive only from recorded values; `oq2Status` stays `unconfirmed` and `targetStatus` is `unconfirmed-estimate-only`. Pre-initiative basis is explicitly `estimated` / `low` confidence with a recorded limitation.
- **Boundary fidelity (AC-2):** `includedCategories` / `excludedCategories` match the Story 4.1 handoff exactly; the validation test enforces every manifest row stays inside the accepted boundary.
- **Validation test (AC-6):** Built clean (0 warnings) and ran the focused class directly via the xUnit v3 executable — **7/7 passed**; full `Hexalith.Conversations.Contracts.Tests` suite **618/618 passed**.
- **Behavior/contract stability (AC-7):** `git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json` is empty; no product runtime source, public contract, AppHost topology, or package versions changed.

### Findings and dispositions

- **[MEDIUM — FIXED] Stale verification counts.** The Dev Agent Record recorded the focused validator as `5/5` and the Contracts suite as `616/616`. The committed test class actually exposes 7 `[Fact]` methods (two QA-gap cases — comparison-math derivation and Markdown/JSON alignment — were added after the original numbers were recorded). Re-running confirmed **7/7** and **618/618**; the Verification Results and Change Log are corrected accordingly. This is the same stale-count failure mode flagged in the Story 4.1 carry-forward.
- **[MEDIUM — KEPT, justified] `ScaffoldSmokeTest.cs` edit is outside the evidence-only scope.** The change sits beyond the story's declared file set, but it is a correct and necessary repair of a pre-existing stale assertion: `AssertProjectReferences` is an exact-match check and the added Commons/EventStore references genuinely exist in the current `Contracts`, `Server`, and `AppHost` csproj files (Epic 3 promotion work). Integration lane verified **8/8 green**. Reverting would re-red the regression lane and falsify the recorded `Integration 8/8`. It is already documented in the File List, Change Log, and Debug Log, so it is retained as a justified, transparent deviation.
- **[LOW — disclosed] Manifest-only LOC are asserted, not derived from compiled files.** AC-3 reproducibility is satisfied from the manifest itself rather than a buildable throwaway module. This is within the story's explicitly permitted manifest-only approach and is transparently disclosed in `templateMinimal.validityProof` and `countingMethod`.
