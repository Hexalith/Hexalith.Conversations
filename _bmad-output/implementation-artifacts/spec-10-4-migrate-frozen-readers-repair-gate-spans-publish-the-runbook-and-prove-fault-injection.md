---
title: 'Story 10.4: Adopt the shared evidence boundary'
type: 'refactor'
created: '2026-10-16'
status: 'in-progress'
baseline_commit: '1915a47123737859e3ac31872fa787fa8c0bd1e3'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-10-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Frozen readers duplicate unsafe evidence plumbing; adoption, fault coverage and promotion-span protection lack proof.

**Approach:** Adopt TestSupport, preserve assertion strength, publish guidance and derive acceptance evidence.

## Boundaries & Constraints

**Always:** Preserve 27 paths, assertion identities/expressions, fixtures, pins, tier ownership, original inventories and accepted 10.3 evidence. Require containment, recomputed hashes/payloads, exact diffs, raw gitlink modes, independent trust roots, nonempty ledgers and zero exemptions. Distinguish `PASS`/`FAIL`/`BLOCKED`/`not-applicable`; unavailable history cannot pass. Summary: `9/9/0/0/0/0`.

**Never:** Absorb projection-proof obligations, weaken conformance, change production APIs, restore retired gates, edit customization defaults or alter unrelated submodules.

**Owner decision, 2026-10-16:** Apply the recommended current-policy approach using baseline `1915a47123737859e3ac31872fa787fa8c0bd1e3`. Preserve its twelve gitlinks and reader assertions, all 27 inventory paths, nine AC identities and original provenance. Bind the ten surviving routes and replace AC-09's retired publisher with resolved-guidance verification.

**Owner amendment, 2026-10-17:** The user explicitly approved correcting only `ScaffoldSmokeTest.SubmodulePathsShouldLiveUnderReferences` from `submodulePaths.Length.ShouldBe(10)` to `submodulePaths.Length.ShouldBe(12)`, updating its migration proof, and regenerating final acceptance evidence. The approved baseline already contains twelve root gitlinks. This is the sole authorized exception to assertion-expression preservation: preserve every original entry blob and provenance, all 27 reader identities, all twelve gitlinks, and every other assertion. Represent the exact count correction explicitly in the migration material and update its resulting hashes, binding, and independent consuming-source pins; do not treat it as an unrecorded plumbing change.

**Owner gitlink amendment, 2026-10-17:** The user explicitly approved accepting the nine current root-submodule revisions introduced by `9cbc71b63be3fa1ce4d89d98ef1771c54c6603f5` while preserving the original baseline, inventories, assertions and evidence. This supersedes gitlink preservation only for the exact nine mode-`160000` transitions below. Retain the twelve baseline gitlinks as immutable provenance; independently pin and verify the complete approved transition set, both endpoint commits and modes, and the unchanged other three gitlinks. Record the amendment in the effective binding and closed final-record facts. Reject omissions, additions, endpoint drift and mode drift; do not add a generic gitlink exemption or edit submodule checkouts. Regenerate all source-bound acceptance evidence after implementation. No push.

| Root submodule | Original baseline commit | Approved current commit |
| --- | --- | --- |
| `references/Hexalith.Builds` | `abdfca89ed82f388b54664825a8a79fed73e7ccf` | `4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77` |
| `references/Hexalith.EventStore` | `6cc9d6114fd055dac7a1d86130208b52271e3740` | `77d9af18a54d92506e310fe5a45021a395e66f60` |
| `references/Hexalith.Folders` | `92e87359dd50dc9644e490a04de98adaaade9a86` | `42a2308113e1bdb9b1d07a778b4405dde797ebd3` |
| `references/Hexalith.FrontComposer` | `0129bd39095892283371f97836ade5768fef3809` | `b102b85b148f21fb26d5742e120c344109df7084` |
| `references/Hexalith.McpCli` | `0b96dfdf2ff6037be88f1fb952d6581a78930e67` | `b35b0975a234f1f97e2e35e9dad381d0a988f4af` |
| `references/Hexalith.Memories` | `f7c8ce84a8a765667beafc31154ee2d6981552f8` | `f6b60d5a2e9b81d84fddad789c7d9bed40d3a533` |
| `references/Hexalith.Parties` | `c0946ef17b3641fb23f6f17614913bfbab687373` | `90af3047b50046cb4f6f91e01f182f562110aab3` |
| `references/Hexalith.Platform` | `b2890d20575356239555df5b113e3100c1d594c1` | `b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b` |
| `references/Hexalith.Tenants` | `e44ac7f537e9f192605e24efc3e61a97d00d44f3` | `e667d5391dae306e631dec23acd191863e54130c` |

## I/O & Edge-Case Matrix

| Input | Result |
| --- | --- |
| Valid migrated scope | PASS with measured assertions |
| Integrity, adoption, guidance, chain or span defect | FAIL with exact code |
| Unavailable Git/history | Visible skip or BLOCKED; never green |
| Valid non-applicable scope | Explicit not-applicable |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Conversations.TestSupport/` — reuse its five neutral helpers; extend existing seams.
- Planning `v9/inventories/evidence-readers-v1.json` — frozen paths; nine differ at entry. Portable links 17; topology owns live tenant tests.
- `_bmad/scripts/evidence_boundary_current.py` — 10.3-specific pins; needs separate dispatch.
- `_bmad/scripts/generate_story_record.py` — reuse predecessor, receipt and insertion verification.
- `_bmad/scripts/tests/test_verify_submodule_promotion.py` — repair `promotion_gate_span()` follower coupling in hermetic fixtures; retain V12 history.

## Tasks & Acceptance

**Execution:**

- [x] `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` and the Story 10.4 migration proof — apply the approved exact 10-to-12 count correction, preserve original before-material, and independently verify that all other assertion bytes remain preserved.
- [x] `_bmad-output/planning-artifacts/story-10.4-current-{binding,contract}-v1.json` — bind approved scope, immutable before-material, provenance, predecessor, exact source/diff sets and nine scenarios.
- [x] `tests/Hexalith.Conversations.TestSupport/` and its test project — extend discovery/read seams and table-driven chain validation; test bounded streams, history and containment.
- [x] Frozen 27 paths and owning test `.csproj` files — replace path/hash/Git wrappers with equivalent shared checks. Collection, live-tenant test and `ContractSamples` need meaningful indirect adoption.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` — implement AC-01..05; independently pin entry snapshots and compare assertions/fixtures/constants through enumerated plumbing substitutions. Counts alone are insufficient. Prove unchanged projection strength and zero exemptions.
- [x] `_bmad/scripts/evidence_boundary_adoption.py`, verifier dispatch and `tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` — add 10.4 evaluation using shared helpers; preserve 10.3 consumption.
- [x] `_bmad/scripts/tests/test_verify_evidence_boundary.py`, promotion tests and `tests/tooling/test_evidence_boundary_current.py` — add `complete_fault_matrix`/`evidence_gate_span_coupling`: named faults, signable/root faults, nested/duplicate/unterminated chains, reader/strength drift, exemptions and span displacement. Retain exact codes, nonempty ledgers and restoration digests.
- [x] `docs/runbooks/evidence-boundary-validation.md` and `_bmad/custom/bmad-{build,build-auto,review}.toml` — bind current guidance, five sections and exact commands/codes; preserve history/routine policy. Test resolved renders and four guidance mutations.
- [x] Generator, `_bmad/schemas/story-final-record-v2.schema.json` and generator tests — add closed 10.4 facts, twelve-root shape, eight measured receipts including AC-09, source/predecessor/publication/insertion verification. Derive AC-08 last.
- [x] `_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json`, `_bmad/scripts/evidence_boundary_adoption.py`, `tests/Hexalith.Conversations.TestSupport.Runner/Program.cs`, generator/schema and their existing test files — bind the approved exact nine gitlink transitions independently, preserve baseline provenance, derive closed final-record facts, and prove rejection of missing/extra transitions, changed endpoints and raw-mode drift.
- [ ] `artifacts/v9/10.4/` and `docs/release-evidence/story-10.4-final-record-v2.{json,md}` — commit sources, measure immutable candidate, publish receipts/pair together, insert verbatim and verify. Later source changes require fresh evidence. No push.

**Acceptance Criteria:**

- Given approved scope, when nine ACs execute, then the verified pair binds migration, predecessor, guidance, faults and span proof with `9/9/0/0/0/0`.
- Given required mutations, when real checkers execute, then exact blockers occur and bytes restore.
- Given migrated sources, when current lanes/guards execute, then they pass with assertions preserved.

## Implementation Notes

- Entry remains bound to `1915a47123737859e3ac31872fa787fa8c0bd1e3`; retain its twelve raw gitlinks as provenance and accept only the nine exact transitions in the owner gitlink amendment. Accepted 10.3 pair/receipts remain untouched.
- The independently pinned entry snapshot retains all 27 original blobs and exact enumerated plumbing substitutions. The separately named `approvedAssertionCorrections` binds exactly the owner-approved 10-to-12 correction in the snapshot, effective binding, closed final-record facts and independent consuming source. Original assertion strength material still includes `ShouldBe(10)`; the approved correction is never normalized as plumbing. Twenty-five readers now call shared discovery/read/hash/Git seams; the unchanged collection and live tenant characterization retain meaningful adoption through their serialized collection and migrated blind-spot evidence owner. ContractSamples supplies repository discovery to the three frozen contract readers without changing sample fixtures.
- The effective binding retains original inventory and contract provenance and names all nine ACs. AC-09 uses current resolved-guidance verification. The ten installed workflow bodies and installed defaults remain unchanged.
- Shared chain validation checks exact ordered nonempty rows and single ordered nonnested markers. Fault fixtures cover all ten named boundary faults, signable/root faults, seven chain variants, reader/helper/inventory/exemption faults, four guidance mutations and exact/displaced evidence and promotion spans. The lifecycle adapter retains successor blocker identities.
- The closed 10.4 final-record facts bind eight measured receipts including AC-09; AC-08 is derived last. Publication and insertion verification retain the committed source candidate and reject later source or receipt drift.

## Spec Change Log

- 2026-10-17: User approved the exact nine gitlink transitions in `9cbc71b63be3fa1ce4d89d98ef1771c54c6603f5` after the scoped verifier returned `FAIL` with `CHANGED_PATH_SET_MISMATCH` and `EVIDENCE_BOUNDARY_GITLINK` (516 assertions). Original baseline, inventories, assertions and evidence remain preserved; fresh acceptance measurements and final publication are required.

- 2026-10-17: User approved the narrow submodule-count amendment after the fresh Debug smoke-method failure confirmed expected 10 versus actual 12 at both baseline and current HEAD. Original entry bytes, inventories, predecessor evidence, and all other assertions remain preserved. Fresh source-bound acceptance measurements and final publication remain required.

## Review Triage Log

## Verification

2026-10-17 current-lane integration: `tests/tooling/test_evidence_boundary_current.py` imports the new `test_approved_gitlink_amendment*` cases and their module-scoped Git fixture alongside the existing complete fault matrix. `python3 -m pytest --collect-only -q tests/tooling/test_evidence_boundary_current.py -k approved_gitlink_amendment` collected all 28 amendment cases; the same command without `--collect-only` passed all 28. Collection, execution and exact commitlint output are retained under `/tmp/story104-gitlink-current-{collection,focused}.log` and `/tmp/story104-gitlink-ci-commitlint.log`. This integration is included in the source candidate for fresh acceptance measurements.

2026-10-17 gitlink-amendment implementation: the effective binding, independently pinned Python and compiled C# consumers, closed schema and generator bind exactly the nine owner-approved mode-`160000` transitions from `9cbc71b63be3fa1ce4d89d98ef1771c54c6603f5`. The twelve baseline roots remain immutable provenance; candidate roots contain exactly nine approved endpoints and three unchanged roots. Exact changed-path equality includes the nine gitlink paths separately from source files. Missing/extra transitions, changed endpoints/modes, amendment metadata drift and changed unchanged roots fail; valid unrelated changes retain `not-applicable`. `python3 -m pytest -q tests/tooling/test_evidence_boundary_adoption.py _bmad/scripts/tests/test_generate_story_record.py -k 'approved_gitlink_amendment or story104 or approved_assertion_correction'` passed 53 cases. The original 28-case `complete_fault_matrix` selector also passed, `git diff --check` passed, and `python3 scripts/check-root-submodules.py --repository .` returned `root submodules: PASS`. Source-bound acceptance, broad current lanes and final publication must be freshly measured after this source freeze. Logs are retained as `/tmp/story104-gitlink-final-focused-v2.log`, `/tmp/story104-gitlink-matrix.log`, and `/tmp/story104-gitlink-commitlint.log`.

Runtime baseline for the gitlink amendment: `aspire start --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json`, `aspire wait conversations --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --timeout 30`, and `aspire describe --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json` succeeded before implementation. All eleven resources reached Running/Healthy after security readiness warmed up; `aspire stop --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` stopped this exact AppHost successfully. The pre-existing FrontComposer AppHost was preserved. Local observations are retained under `/tmp/story104-gitlink-runtime*`.

2026-10-17 amendment checks: `python3 -m pytest -q tests/tooling/test_evidence_boundary_adoption.py _bmad/scripts/tests/test_generate_story_record.py -k 'approved_assertion_correction or story104'` passed 15 cases. The explicit correction is independently constrained; unapproved count changes, another changed assertion, correction-metadata drift and closed-record drift turn red. Fresh committed-candidate measurements use a detached local shared clone at `/tmp/story104-approved-laacf8pj/checkout`, with twelve root-declared dependency clones checked out at the superproject's exact pinned commits. Concurrent user dependency checkout changes remain untouched. Isolation commands and source-bound results are retained under `/tmp/story104-approved-*`; no network or nested submodule initialization is used.

2026-10-17 approved-amendment runtime baseline: started the exact Conversations AppHost with `aspire start --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json`, waited using `aspire wait conversations --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --timeout 30`, and inspected `aspire describe --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json` before code changes. All eleven resources were Running/Healthy. The exact AppHost was stopped successfully with `aspire stop --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`. The initial readiness probe ran before background startup completed and reported no running AppHost; the retry passed. `aspire stop` rejected unsupported `--format Json`; the supported command above succeeded. Sanitized state retained at `/tmp/story104-approved-runtime-summary.json`.

Initial focused commands passed: `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k complete_fault_matrix` (28 cases), `python3 -m pytest -q _bmad/scripts/tests/test_verify_submodule_promotion.py -k evidence_gate_span_coupling` (2 cases), and `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k story104` (9 cases). The eight initial `AdoptionSeamTests` cases also passed and remain covered by the later unfiltered 40-case TestSupport run. These are preliminary checks; final acceptance measurements remain pending the owner amendment below.

Runtime baseline: `aspire start --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json`, `aspire describe --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json`, and `aspire wait conversations --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --timeout 30` passed. All eleven resources were Running/Healthy. The exact Conversations AppHost was stopped successfully after observation; the pre-existing FrontComposer AppHost was left running. Observation occurred after the initial test-support edits; production sources were unchanged.

Affected build command (each project individually): `dotnet build tests/Hexalith.Conversations.<Project>/Hexalith.Conversations.<Project>.csproj --configuration Debug --nologo -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`, for TestSupport.Tests, TestSupport.Runner, Conformance.Portable.Tests, Conformance.Tests, Contracts.Tests and IntegrationTests.

- Build affected projects in Debug; use built xUnit `-method`/`-class` filters and approved acceptance commands.
- Run new selectors, current tooling CI lane, both conformance tiers, affected Contracts/Integration smoke tests and TestSupport tests.
- Current tooling CI command: `python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests tests/tooling/test_evidence_boundary_current.py --ignore=_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py --junitxml=/tmp/story104-current-ci-lane.xml`.
- Native current tiers use their Debug assemblies with `-automated sync -failSkips -parallel none -trx /tmp/story104-<tier>.trx`; the class/method exclusions are exactly those in `.github/workflows/ci.yml`, retained independently of this story. Contracts and TestSupport run unfiltered; Integration smoke uses `-class Hexalith.Conversations.IntegrationTests.ScaffoldSmokeTest`.
- Both Story 7.3 guard commands retain their contract, `--scenario AC-7.3-01` / `AC-7.3-02`, and contracted `artifacts/v9/7.3/<scenario>.json` outputs. The scoped 10.4 verifier passed with 515 evaluated assertions and no blockers at source commit `a02b218b8c9e37c57e0882cc0a0f54ab62b64c43`; this checkpoint is retained under `/tmp` and is not a final acceptance receipt.
- Run both Story 7.3 guards and scoped verifier before review/completion; verify pair/insertion. Report broad blockers separately.

### Checkpoint before the required owner amendment

- Debug builds of all six affected projects passed with zero warnings/errors. The added regular-mode test's xUnit cancellation-token analyzer finding was repaired; the rebuilt TestSupport suite passed 40/40.
- Current native lanes passed: Contracts 618/618, portable conformance 326/326 with the existing CI exclusions, and internal conformance 95/95 with its existing tier-freeze exclusion. Both Story 7.3 guards passed (33 presence and 22 parity assertions). The scoped verifier passed 515 assertions at committed source `a02b218b8c9e37c57e0882cc0a0f54ab62b64c43`.
- Integration smoke command: `dotnet tests/Hexalith.Conversations.IntegrationTests/bin/Debug/net10.0/Hexalith.Conversations.IntegrationTests.dll -automated sync -failSkips -parallel none -class Hexalith.Conversations.IntegrationTests.ScaffoldSmokeTest -trx /tmp/story104-IntegrationTests.trx`. Result: 7 passed, 1 failed, no skips. `SubmodulePathsShouldLiveUnderReferences` reports `submodulePaths.Length should be 10 but was 12`. The exact `ShouldBe(10)` expression exists at approved baseline `1915a47123737859e3ac31872fa787fa8c0bd1e3` and remains unchanged. Parent requested an owner decision on the narrow count amendment; it has not been applied.
- First complete current tooling CI command: `python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests tests/tooling/test_evidence_boundary_current.py --ignore=_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py --junitxml=/tmp/story104-current-ci-lane-final.xml`. Exit 1: 1712 passed, 17 failed, 0 errors/skips, 216 warnings, 1504.48 seconds. Retain `/tmp/story104-current-ci-lane-final.log` and XML as failing evidence. The earlier `/tmp/story104-current-ci-lane.log` was interrupted after 292 passes and is partial evidence only.
- One full-lane failure was a 30-second existing fixture Git timeout in `test_v2_done_commit_moving_a_gitlink_fails_inserted_record_verification[lifecycle-only-done]`, while running `git update-index --add --cacheinfo 160000,6975990db1dfbebd3e8ef74d05983c859c0a8729,references/Hexalith.Projects`; four isolated reruns passed. The other sixteen failures arose from historical fixtures following live bridge/guidance/sprint inputs or a changed diagnostic. Their inputs now use approved historical blobs and the original diagnostic is preserved.
- Post-repair focused commands passed: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k 'story103_committed_receipts or story103_source_rederivation_rejects_invalid_withdrawal or story103_inserted_rederivation or story92_portable_surface_guards_python_only'` (24 cases); `python3 -m pytest -q tests/tooling/test_evidence_boundary_current.py -k 'refreshed_source_accepts or current_ownership or edited_xml_withdrawal'` (13 cases); and `python3 -m pytest -q tests/tooling/test_evidence_boundary_adoption.py -k 'complete_fault_matrix_integrity or complete_fault_matrix_chain'` (17 cases with exact blocker equality). These focused results do not replace a fresh complete CI-lane run.
- Fresh complete current tooling CI retry at `a02b218b8c9e37c57e0882cc0a0f54ab62b64c43`: `python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests tests/tooling/test_evidence_boundary_current.py --ignore=_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py --junitxml=/tmp/story104-current-ci-rerun.xml`. Exit 0: 1729 passed, 0 failed/errors/skips, 216 warnings, 1532.30 seconds. Retain `/tmp/story104-current-ci-rerun.log`, XML and `.exit` as the complete passing evidence; the prior complete failing and interrupted runs remain distinct.
- All five exact effective-contract native filters passed at `a02b218b8c9e37c57e0882cc0a0f54ab62b64c43`, each reporting exactly one executed/passed case, zero failures/skips, and the full committed assembly revision. Only their output paths were redirected to `/tmp/story104-preflight-AC-10.4-0<n>.trx`. The new AC-01 method was corrected to the contracted `ReaderInventoryShouldMatchFrozenSet` identity; none of the frozen 27 reader assertions changed. The affected internal Debug rebuild passed with zero warnings/errors.
- `git check-attr text eol -- artifacts/v9/10.4/reader-entry-v1.json artifacts/v9/10.4/AC-10.4-01.trx artifacts/v9/10.4/AC-10.4-06.xml artifacts/v9/10.4/AC-10.4-09.json` reports `text: set` and `eol: lf` for all four representative paths. The snapshot and measured Python XML bytes contain LF only; native TRX bytes contain no CR/LF.
- Final 10.4 acceptance receipts, pair publication and insertion remain ungenerated pending the owner decision and fresh committed-candidate measurements. Keep this spec in-progress; no push was performed.

### Exact native checkpoint commands

These native broad-lane measurements use source `baf3a54e8d12451a1dffa92490a1476a3961ef0c`; the five internal contract filters were rebuilt and remeasured at `a02b218` as described above. The following are the exact expanded commands, including the existing CI exclusions.

```bash
dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Debug/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -parallel none -trx /tmp/story104-TestSupport.Tests.trx
dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -parallel none -trx /tmp/story104-Contracts.Tests.trx
dotnet tests/Hexalith.Conversations.IntegrationTests/bin/Debug/net10.0/Hexalith.Conversations.IntegrationTests.dll -automated sync -failSkips -parallel none -class Hexalith.Conversations.IntegrationTests.ScaffoldSmokeTest -trx /tmp/story104-IntegrationTests.trx
dotnet tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Debug/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll -automated sync -failSkips -parallel none \
  -class- Hexalith.Conversations.Conformance.Tests.ArchitecturePlanningAuthorityValidationTest \
  -class- Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV8ValidationTest \
  -class- Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV9ValidationTest \
  -class- Hexalith.Conversations.Conformance.Tests.PlanningToolingEnvironmentAuthorityV15ValidationTest \
  -class- Hexalith.Conversations.Conformance.Tests.PlanningToolingLifecycleAuthorityV16ValidationTest \
  -class- Hexalith.Conversations.Conformance.Tests.PackageEnvironmentAuthorityV18ValidationTest \
  -class- Hexalith.Conversations.Conformance.Tests.StoryFinalRecordGenerationValidationTest \
  -class- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest \
  -method- Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.BindingsClosuresAndFrozenV1BytesShouldValidateIndependently \
  -method- Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.CurrentControlsAndTierPrerequisiteShouldStayTruthful \
  -method- Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.PublicSurfacesAndConformanceAssertionsShouldHaveZeroGap \
  -method- Hexalith.Conversations.Conformance.Tests.SmC2BaselineReconstructionValidationTest.BaselineShouldRecordAnAuditableReconstructionMethod -trx /tmp/story104-Conformance.Portable.Tests.trx
dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Debug/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -parallel none \
  -class- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest -trx /tmp/story104-Conformance.Tests.trx
```
