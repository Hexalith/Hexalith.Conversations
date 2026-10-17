---
title: 'Story 10.4: Adopt the shared evidence boundary'
type: 'refactor'
created: '2026-10-16'
status: 'done'
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
- [x] `artifacts/v9/10.4/` and `docs/release-evidence/story-10.4-final-record-v2.{json,md}` — commit sources, measure immutable candidate, publish receipts/pair together, insert verbatim and verify. Later source changes require fresh evidence. No push.

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

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| Blind 1: fault/blocker identity | high | patch | The receipt parser accepts each declared expected code and checks only the final fault-ID set. A wrong stable code can therefore certify the required fault. Pin every required pairing independently. |
| Blind 2: JUnit identities | medium | patch | The parser checks counters and source properties but never the selected test class/name or duplicate cases. Unrelated cases can replace the contracted tests. |
| Blind 3: positive promotion proof | high | patch | Removing the exact-span case and adjusting the counter still satisfies the mutation-ID set through the displaced-span case. Require both contracted cases. |
| Blind 4: native revision stamp | medium | patch | The stamp regex permits arbitrary text before the candidate suffix. Parse the complete revision component and compare it exactly. |
| Blind 5: native helper envelope | high | patch | Embedded native results bypass schema/blocker/row consistency checks. A PASS label carrying blockers can be consumed as passing evidence. |
| Blind 6: AC-09 scope | high | patch | A schema-valid single unrelated passing row with empty manifest/changed paths passes the parser. Require the actual boundary, workflow and guidance facts. |
| Blind 7: undeclared working changes | medium | patch | The specialized generator checks only declared source files and omits the existing general route's dirty-tree guard. Reject unrelated source dirt while retaining explicit receipt/lifecycle allowances. |
| Blind 8: containment trust origin | high | patch | SmC2BaselineReconstructionValidationTest passes artifact-controlled fixtureOverlay.path through HashFile. ContainedFile derives its root from that same path, so a foreign repository marker can authorize an escaped read. Resolve the owning test repository independently of the evidence path. |
| Blind 9: migrated adapter coverage | false | reject | The reviewer withdrew the defect claim after finding no adapter mishandling. Direct GitFacts fixtures run alongside independently preserved adapter substitutions and assertions; no current failing outcome was identified. |
| Blind 10: governed chain table | false | reject | The reviewer found no current governed table and escaping mutation and withdrew the claim. Generic chain fixtures exercise the real neutral checker; restoring the retired ArchitecturePlanningAuthority route conflicts with current policy. |
| Blind 11: publication guidance | medium | patch | The runbook jumps from generation to inserted verification, which requires a committed atomic publication. Document source freeze, eight measurements, exact publication, insertion and verification, and align AC-09's workflow flag with its effective command. |
| Edge 1: fault/blocker identity | high | patch | Confirmed the same declared-code acceptance defect as Blind 1; retain this finding and fix the shared parser defect once. |
| Edge 2: malformed fault ledger | high | patch | The parser tests only truthiness after JSON decoding, so a nonempty string satisfies the ledger requirement. Require nonempty, valid evaluated assertion rows. |
| Edge 3: positive promotion proof | high | patch | Confirmed the same missing exact-span case as Blind 3; require the complete contracted case set. |
| Edge 4: mutable revision mode race | medium | patch | ReadRegularBlobAsync resolves the caller's revision separately in ReadTreeAsync and ReadBlobAsync. A moving HEAD can pass mode validation at one commit and return symlink bytes at another. Pin once before both operations. |
| Verification 1: neutral-helper guard test | medium | patch | The pre-verified gap shows that the mock returns a nonpackable helper graph for later shipped projects, allowing an unrelated failure after the helper restriction is removed. The five cases are also absent from the routine re-export. Use valid per-project graphs and a passing baseline before mutations, then collect the cases. |
| Verification 2: fault/blocker identity | high | patch | Confirmed the same declared-code acceptance defect as Blind 1 and Edge 1; independent required-code validation closes all three reports. |

Review corrections completed at source `7e5a4b765a4437f361fac7e7bb4f13e5b7cf3ec8`. All twelve patch root causes are fixed; two unsupported findings were rejected. Full verification passed: 1,088 native suite cases, five exact source-stamped native receipts, 1,838 current tooling cases, 51 amendment/correction cases, both Story 7.3 guards, committed insertion verification, and the scoped gate. The regenerated record is `9/9/0/0/0/0` with 29 restored faults and eight measured receipts. Atomic publication: `4bdab7d79fd4acb8238fefd5b64459f1b34fa922`; insertion: `3b21b99974c5fff2aefc35d7abfd1035bba9e8bc`. No deferred findings remain.

## Verification

2026-10-17 review correction checkpoint: all verified patch findings are implemented. Focused checks passed: 44 generator cases, six current importer/helper/runbook cases, two promotion-span cases, nine reader preservation cases, ten adoption-seam cases, and the real preservation fault-injection fact. Both affected native builds had zero warnings/errors. The independent containment origin retains the original four explicitly validated temporary-fixture reads; all 27 entry blobs and original provenance remain preserved. The provisional `718576e` measurements and atomic publication remain in Git and `/tmp/story104-pre-review-publication-718576e`; they are superseded for acceptance. Fresh full verification and candidate-bound publication are pending this source freeze.

2026-10-17 availability and output-safety correction: full amendment verification reproduced incorrect `FAIL` states for unavailable Git and baseline history; availability now precedes compiled pin validation. Unavailable Git, baseline and candidate history remain `BLOCKED`, while an available wrong baseline remains `FAIL`. The Story 10.4 output route now uses the resolved candidate and requires evaluated passing ownership of the candidate tree, HEAD, HEAD tree and index (including successful index read) before writing. Missing or unavailable ownership preserves existing bytes and the blocked state; proven ownership still permits a valid failing diagnostic receipt. Regression checks include a real tracked AC-09 receipt with Git unavailable, every required ownership fact, absent candidate-tree evaluation, canonical candidate ownership and the existing hardlink guard. The combined amendment/assertion/output selection passed 51 cases, and canonical candidate ownership passed separately; the current CI importer collects all 46 amendment cases. Exact results are retained in `/tmp/story104-gitlink-availability-output-combined.log`, `/tmp/story104-gitlink-output-canonical.log`, `/tmp/story104-gitlink-unavailable-{before,after}.log` and `/tmp/story104-gitlink-final-source-commitlint.log`.

The preceding source `b4652a2676a93ee167c7672bcf06617533deae46` completed its native lanes, all eight measured receipt commands, both Story 7.3 guards and scoped verifier. Its complete current tooling lane passed 1769 cases with 245 warnings in 1142.99 seconds. Those passing results remain preliminary because of the availability/output changes above; logs, command summaries, TRX/XML results and all eight receipts are preserved distinctly under `/tmp/story104-gitlink-pre-availability/`. Fresh committed-source measurements and final publication remain required.

2026-10-17 full-envelope correction: the first native internal conformance measurement at `63d379cfb5301d46a07951ba96295940251f0f12` executed 95 cases, with 90 passing and the five adoption cases failing `RESULT_SCHEMA_INVALID`: their underlying assertions passed, but the inherited v1 result schema requires `gitlinks` to be empty for `PASS`. Story 10.4 now treats that inherited field as prohibited raw transitions, clearing it only after all preceding ledger rows and the independently pinned metadata, complete raw transitions, original baseline roots and candidate roots pass. Invalid, missing, partial or drifted transitions retain the raw rows and blockers. All nine actual transitions remain explicit in changed paths, compiled and Python ledgers, and closed final-record amendment facts; the original result schema and Story 10.3 remain unchanged. A real committed-scope regression through complete verification and finalization passed, including the positive case, wrong-baseline rejection, metadata drift and byte restoration. It reconstructs the original source candidate after publication rather than following later lifecycle/source changes. The current CI importer collects all 29 amendment cases. The pre-correction native logs and TRX files remain distinct under `/tmp/story104-gitlink-pre-envelope-native/`; focused and full-path results are retained in `/tmp/story104-gitlink-result-focused.log` and `/tmp/story104-gitlink-complete-verifier-v2.log`. Fresh source-bound measurements are required after this correction.

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

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 10.4 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `10.4`
- Candidate: `7e5a4b765a4437f361fac7e7bb4f13e5b7cf3ec8`
- JSON content SHA-256 (all three digest fields zeroed): `0fe78ebc689b023bd2fc242e54b6cb133e0aec12c0d4be2584ab449cf4b27bba`

## Authority

| Field | Value |
| --- | --- |
| Epic | `epic-6-authority-2026-08-03-v10` |
| Architecture | `conversations-architecture-2026-08-03-v10` |
| Planning candidate | `1e9a61126d3b7a55b514b7c7c8942d5af03355e5` |
| Bundle digest | `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055` |

## Root gitlinks

| Path | Mode | Commit |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `160000` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `160000` | `4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77` |
| `references/Hexalith.Commons` | `160000` | `9e874b1a3c6e7b6ff07b6f40b81d36e4e1403798` |
| `references/Hexalith.EventStore` | `160000` | `77d9af18a54d92506e310fe5a45021a395e66f60` |
| `references/Hexalith.Folders` | `160000` | `42a2308113e1bdb9b1d07a778b4405dde797ebd3` |
| `references/Hexalith.FrontComposer` | `160000` | `b102b85b148f21fb26d5742e120c344109df7084` |
| `references/Hexalith.McpCli` | `160000` | `b35b0975a234f1f97e2e35e9dad381d0a988f4af` |
| `references/Hexalith.Memories` | `160000` | `f6b60d5a2e9b81d84fddad789c7d9bed40d3a533` |
| `references/Hexalith.Parties` | `160000` | `90af3047b50046cb4f6f91e01f182f562110aab3` |
| `references/Hexalith.Platform` | `160000` | `b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b` |
| `references/Hexalith.Projects` | `160000` | `a8059addf27261c0f2bddd23d2b266ebcfa00660` |
| `references/Hexalith.Tenants` | `160000` | `e667d5391dae306e631dec23acd191863e54130c` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-10.4-ENTRY-v1` | `fbcf160a9d4beb407c33da4aa5cbdc12040fc71591a9bd9720a89f591d90cc34` |

## Predecessors

- `10.3`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-10.4-01` | `0` | `PASS` | `none` | `334` | `artifacts/v9/10.4/AC-10.4-01.trx` | `b199aa15a64eb9cd47d3d9f9f687bb6d86c79267c8a62f7e3ac24bbb40571013` |
| `AC-10.4-02` | `0` | `PASS` | `none` | `334` | `artifacts/v9/10.4/AC-10.4-02.trx` | `af17c77e25d4c0b72b71d18a634f6a5500dcbb8eb9eccc78025d7b4849236700` |
| `AC-10.4-03` | `0` | `PASS` | `none` | `334` | `artifacts/v9/10.4/AC-10.4-03.trx` | `7fb1b312def906d22dc83e3b0698b5cbab083e3f9dcba8849520c659d274574e` |
| `AC-10.4-04` | `0` | `PASS` | `none` | `334` | `artifacts/v9/10.4/AC-10.4-04.trx` | `643ea1ac67846005d829269752c2e88896e93cdb34ea43fa30e8eeff633687d1` |
| `AC-10.4-05` | `0` | `PASS` | `none` | `236` | `artifacts/v9/10.4/AC-10.4-05.trx` | `71479e301db4b7d67e2e7e7e4f8eaa818b92fd3bce5b0a531528cef87285d15b` |
| `AC-10.4-06` | `0` | `PASS` | `none` | `28` | `artifacts/v9/10.4/AC-10.4-06.xml` | `94faa9000e8fdcbfc4a803d53777d061ff827837874bbda14557c388d1d3fb4a` |
| `AC-10.4-07` | `0` | `PASS` | `none` | `2` | `artifacts/v9/10.4/AC-10.4-07.xml` | `08c646dd589ff533267928dc4e3c6a4caa8f069fda503c87e936024235c072fd` |
| `AC-10.4-08` | `0` | `PASS` | `none` | `7` | none | none |
| `AC-10.4-09` | `0` | `PASS` | `none` | `522` | `artifacts/v9/10.4/AC-10.4-09.json` | `a5ecb193a31bc290d34d08bc23fc72e4c8de05bdbff2a36807f77be094affe77` |

### `AC-10.4-01`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Debug/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.ReaderInventoryShouldMatchFrozenSet -trx artifacts/v9/10.4/AC-10.4-01.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-01.trx` | `b199aa15a64eb9cd47d3d9f9f687bb6d86c79267c8a62f7e3ac24bbb40571013` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-01#0001` | `Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.ReaderInventoryShouldMatchFrozenSet` | `PASS` |
| `AC-10.4-01#0002` | `initial:repository::repository` | `PASS` |
| `AC-10.4-01#0003` | `initial:candidate::candidate` | `PASS` |
| `AC-10.4-01#0004` | `initial:baseline::baseline` | `PASS` |
| `AC-10.4-01#0005` | `initial:diff::diff` | `PASS` |
| `AC-10.4-01#0006` | `initial:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-01#0007` | `initial:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-01#0008` | `initial:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-01#0009` | `initial:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0010` | `initial:containment:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-01#0011` | `binding:baseline::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0012` | `binding:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0013` | `roots:independent::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-01#0014` | `baseline:repository::repository` | `PASS` |
| `AC-10.4-01#0015` | `baseline:candidate::candidate` | `PASS` |
| `AC-10.4-01#0016` | `baseline:baseline::baseline` | `PASS` |
| `AC-10.4-01#0017` | `baseline:diff::diff` | `PASS` |
| `AC-10.4-01#0018` | `baseline:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-01#0019` | `baseline:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-01#0020` | `gitlinks:amendment::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0021` | `gitlinks:transitions::nine exact mode-160000 transitions` | `PASS` |
| `AC-10.4-01#0022` | `gitlinks:provenance::twelve baseline root gitlinks` | `PASS` |
| `AC-10.4-01#0023` | `gitlinks:candidate::nine approved and three unchanged roots` | `PASS` |
| `AC-10.4-01#0024` | `provenance:membership::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0025` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-01#0026` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-01#0027` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-01#0028` | `provenance:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-01#0029` | `provenance:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-01#0030` | `provenance:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-01#0031` | `provenance:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-01#0032` | `inventory:workflowInventory::workflowInventory` | `PASS` |
| `AC-10.4-01#0033` | `inventory:guidanceInventory::guidanceInventory` | `PASS` |
| `AC-10.4-01#0034` | `workflow:entry:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-01#0035` | `workflow:entry:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-01#0036` | `workflow:entry:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-01#0037` | `workflow:entry:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-01#0038` | `workflow:entry:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-01#0039` | `workflow:entry:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-01#0040` | `workflow:entry:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-01#0041` | `workflow:entry:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-01#0042` | `workflow:entry:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-01#0043` | `workflow:entry:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-01#0044` | `binding:exact-sources::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0045` | `boundary:repository::repository` | `PASS` |
| `AC-10.4-01#0046` | `boundary:candidate::candidate` | `PASS` |
| `AC-10.4-01#0047` | `boundary:baseline::baseline` | `PASS` |
| `AC-10.4-01#0048` | `boundary:diff::diff` | `PASS` |
| `AC-10.4-01#0049` | `boundary:changed-paths::changed-paths` | `PASS` |
| `AC-10.4-01#0050` | `boundary:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-01#0051` | `boundary:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-01#0052` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-01#0053` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-01#0054` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-01#0055` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-01#0056` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-01#0057` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-01#0058` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-01#0059` | `boundary:manifest:file:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-01#0060` | `boundary:manifest:file:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-01#0061` | `boundary:manifest:file:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-01#0062` | `boundary:manifest:file:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-01#0063` | `boundary:manifest:file:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-01#0064` | `boundary:manifest:file:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-01#0065` | `boundary:manifest:file:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-01#0066` | `boundary:manifest:file:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-01#0067` | `boundary:manifest:file:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-01#0068` | `boundary:manifest:file:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-01#0069` | `boundary:manifest:file:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-01#0070` | `boundary:manifest:file:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-01#0071` | `boundary:manifest:file:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-01#0072` | `boundary:manifest:file:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-01#0073` | `boundary:manifest:file:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-01#0074` | `boundary:manifest:file:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-01#0075` | `boundary:manifest:signable-payload::manifest` | `PASS` |
| `AC-10.4-01#0076` | `boundary:roots:compiled::roots:compiled` | `PASS` |
| `AC-10.4-01#0077` | `boundary:roots::roots` | `PASS` |
| `AC-10.4-01#0078` | `boundary:allowlist::allowlist` | `PASS` |
| `AC-10.4-01#0079` | `boundary:containment:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-01#0080` | `boundary:containment:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-01#0081` | `boundary:containment:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-01#0082` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0083` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-01#0084` | `boundary:containment:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-01#0085` | `boundary:containment:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-01#0086` | `boundary:containment:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-01#0087` | `boundary:containment:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-01#0088` | `boundary:containment:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-01#0089` | `boundary:containment:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-01#0090` | `boundary:containment:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-01#0091` | `boundary:containment:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-01#0092` | `boundary:containment:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-01#0093` | `boundary:containment:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-01#0094` | `boundary:containment:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-01#0095` | `boundary:containment:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-01#0096` | `boundary:containment:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-01#0097` | `boundary:containment:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-01#0098` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-01#0099` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0100` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-01#0101` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0102` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-01#0103` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0104` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-01#0105` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-01#0106` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0107` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0108` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-01#0109` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0110` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-01#0111` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0112` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0113` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0114` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-01#0115` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0116` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0117` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0118` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-01#0119` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-01#0120` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-01#0121` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-01#0122` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-01#0123` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-01#0124` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0125` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-01#0126` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-01#0127` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-01#0128` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-01#0129` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-01#0130` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-01#0131` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-01#0132` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-01#0133` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-01#0134` | `boundary:containment:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-01#0135` | `boundary:containment:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-01#0136` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-01#0137` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-01#0138` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-01#0139` | `candidate:blob:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-01#0140` | `candidate:blob:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-01#0141` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-01#0142` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-01#0143` | `candidate:blob:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-01#0144` | `candidate:blob:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-01#0145` | `candidate:blob:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-01#0146` | `candidate:blob:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-01#0147` | `candidate:blob:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-01#0148` | `candidate:blob:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-01#0149` | `candidate:blob:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-01#0150` | `candidate:blob:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-01#0151` | `candidate:blob:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-01#0152` | `candidate:blob:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-01#0153` | `candidate:blob:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-01#0154` | `candidate:blob:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-01#0155` | `candidate:blob:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-01#0156` | `candidate:blob:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-01#0157` | `candidate:blob:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-01#0158` | `candidate:blob:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-01#0159` | `candidate:blob:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-01#0160` | `candidate:blob:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-01#0161` | `candidate:blob:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-01#0162` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0163` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-01#0164` | `candidate:blob:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-01#0165` | `candidate:blob:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-01#0166` | `candidate:blob:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-01#0167` | `candidate:blob:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-01#0168` | `candidate:blob:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-01#0169` | `candidate:blob:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-01#0170` | `candidate:blob:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-01#0171` | `candidate:blob:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-01#0172` | `candidate:blob:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-01#0173` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-01#0174` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0175` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-01#0176` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0177` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-01#0178` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0179` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-01#0180` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-01#0181` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0182` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0183` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-01#0184` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0185` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-01#0186` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0187` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0188` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0189` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-01#0190` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0191` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0192` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0193` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-01#0194` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-01#0195` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-01#0196` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-01#0197` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-01#0198` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-01#0199` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0200` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-01#0201` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-01#0202` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-01#0203` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-01#0204` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-01#0205` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-01#0206` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-01#0207` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-01#0208` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-01#0209` | `candidate:blob:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-01#0210` | `candidate:blob:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-01#0211` | `readers:approved-correction::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-01#0212` | `readers:inventory::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-01#0213` | `readers:binding::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0214` | `entry:repository::repository` | `PASS` |
| `AC-10.4-01#0215` | `entry:candidate::candidate` | `PASS` |
| `AC-10.4-01#0216` | `entry:baseline::baseline` | `PASS` |
| `AC-10.4-01#0217` | `entry:diff::diff` | `PASS` |
| `AC-10.4-01#0218` | `entry:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-01#0219` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0220` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-01#0221` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0222` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-01#0223` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-01#0224` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-01#0225` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0226` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0227` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-01#0228` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0229` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-01#0230` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0231` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0232` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0233` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-01#0234` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-01#0235` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0236` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0237` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0238` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-01#0239` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-01#0240` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-01#0241` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-01#0242` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-01#0243` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-01#0244` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0245` | `entry:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-01#0246` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0247` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0248` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0249` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-01#0250` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-01#0251` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-01#0252` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0253` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0254` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0255` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-01#0256` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-01#0257` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-01#0258` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-01#0259` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-01#0260` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-01#0261` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-01#0262` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-01#0263` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-01#0264` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0265` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0266` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0267` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0268` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0269` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0270` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-01#0271` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-01#0272` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-01#0273` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0274` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0275` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-01#0276` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-01#0277` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-01#0278` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-01#0279` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0280` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0281` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0282` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0283` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0284` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0285` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0286` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0287` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-01#0288` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-01#0289` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-01#0290` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-01#0291` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-01#0292` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-01#0293` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-01#0294` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0295` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0296` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0297` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0298` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0299` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-01#0300` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0301` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0302` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-01#0303` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-01#0304` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-01#0305` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-01#0306` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-01#0307` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-01#0308` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-01#0309` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-01#0310` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-01#0311` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-01#0312` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-01#0313` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-01#0314` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-01#0315` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-01#0316` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-01#0317` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-01#0318` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-01#0319` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-01#0320` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-01#0321` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0322` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0323` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-01#0324` | `reader:entry:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-01#0325` | `reader:strength:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-01#0326` | `reader:helper:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-01#0327` | `reader:project:Conformance.Tests::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-01#0328` | `reader:project:Conformance.Portable.Tests::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-01#0329` | `reader:project:Contracts.Tests::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-01#0330` | `reader:project:IntegrationTests::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-01#0331` | `readers:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-01#0332` | `projection:scope::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-01#0333` | `authority:selection::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-01#0334` | `predecessor:10.3::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |

### `AC-10.4-02`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Debug/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.MigrationsShouldUseHelperAtEqualStrength -trx artifacts/v9/10.4/AC-10.4-02.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-02.trx` | `af17c77e25d4c0b72b71d18a634f6a5500dcbb8eb9eccc78025d7b4849236700` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-02#0001` | `Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.MigrationsShouldUseHelperAtEqualStrength` | `PASS` |
| `AC-10.4-02#0002` | `initial:repository::repository` | `PASS` |
| `AC-10.4-02#0003` | `initial:candidate::candidate` | `PASS` |
| `AC-10.4-02#0004` | `initial:baseline::baseline` | `PASS` |
| `AC-10.4-02#0005` | `initial:diff::diff` | `PASS` |
| `AC-10.4-02#0006` | `initial:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-02#0007` | `initial:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-02#0008` | `initial:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-02#0009` | `initial:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0010` | `initial:containment:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-02#0011` | `binding:baseline::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0012` | `binding:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0013` | `roots:independent::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-02#0014` | `baseline:repository::repository` | `PASS` |
| `AC-10.4-02#0015` | `baseline:candidate::candidate` | `PASS` |
| `AC-10.4-02#0016` | `baseline:baseline::baseline` | `PASS` |
| `AC-10.4-02#0017` | `baseline:diff::diff` | `PASS` |
| `AC-10.4-02#0018` | `baseline:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-02#0019` | `baseline:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-02#0020` | `gitlinks:amendment::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0021` | `gitlinks:transitions::nine exact mode-160000 transitions` | `PASS` |
| `AC-10.4-02#0022` | `gitlinks:provenance::twelve baseline root gitlinks` | `PASS` |
| `AC-10.4-02#0023` | `gitlinks:candidate::nine approved and three unchanged roots` | `PASS` |
| `AC-10.4-02#0024` | `provenance:membership::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0025` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-02#0026` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-02#0027` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-02#0028` | `provenance:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-02#0029` | `provenance:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-02#0030` | `provenance:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-02#0031` | `provenance:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-02#0032` | `inventory:workflowInventory::workflowInventory` | `PASS` |
| `AC-10.4-02#0033` | `inventory:guidanceInventory::guidanceInventory` | `PASS` |
| `AC-10.4-02#0034` | `workflow:entry:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-02#0035` | `workflow:entry:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-02#0036` | `workflow:entry:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-02#0037` | `workflow:entry:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-02#0038` | `workflow:entry:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-02#0039` | `workflow:entry:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-02#0040` | `workflow:entry:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-02#0041` | `workflow:entry:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-02#0042` | `workflow:entry:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-02#0043` | `workflow:entry:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-02#0044` | `binding:exact-sources::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0045` | `boundary:repository::repository` | `PASS` |
| `AC-10.4-02#0046` | `boundary:candidate::candidate` | `PASS` |
| `AC-10.4-02#0047` | `boundary:baseline::baseline` | `PASS` |
| `AC-10.4-02#0048` | `boundary:diff::diff` | `PASS` |
| `AC-10.4-02#0049` | `boundary:changed-paths::changed-paths` | `PASS` |
| `AC-10.4-02#0050` | `boundary:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-02#0051` | `boundary:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-02#0052` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-02#0053` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-02#0054` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-02#0055` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-02#0056` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-02#0057` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-02#0058` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-02#0059` | `boundary:manifest:file:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-02#0060` | `boundary:manifest:file:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-02#0061` | `boundary:manifest:file:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-02#0062` | `boundary:manifest:file:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-02#0063` | `boundary:manifest:file:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-02#0064` | `boundary:manifest:file:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-02#0065` | `boundary:manifest:file:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-02#0066` | `boundary:manifest:file:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-02#0067` | `boundary:manifest:file:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-02#0068` | `boundary:manifest:file:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-02#0069` | `boundary:manifest:file:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-02#0070` | `boundary:manifest:file:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-02#0071` | `boundary:manifest:file:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-02#0072` | `boundary:manifest:file:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-02#0073` | `boundary:manifest:file:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-02#0074` | `boundary:manifest:file:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-02#0075` | `boundary:manifest:signable-payload::manifest` | `PASS` |
| `AC-10.4-02#0076` | `boundary:roots:compiled::roots:compiled` | `PASS` |
| `AC-10.4-02#0077` | `boundary:roots::roots` | `PASS` |
| `AC-10.4-02#0078` | `boundary:allowlist::allowlist` | `PASS` |
| `AC-10.4-02#0079` | `boundary:containment:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-02#0080` | `boundary:containment:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-02#0081` | `boundary:containment:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-02#0082` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0083` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-02#0084` | `boundary:containment:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-02#0085` | `boundary:containment:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-02#0086` | `boundary:containment:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-02#0087` | `boundary:containment:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-02#0088` | `boundary:containment:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-02#0089` | `boundary:containment:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-02#0090` | `boundary:containment:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-02#0091` | `boundary:containment:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-02#0092` | `boundary:containment:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-02#0093` | `boundary:containment:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-02#0094` | `boundary:containment:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-02#0095` | `boundary:containment:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-02#0096` | `boundary:containment:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-02#0097` | `boundary:containment:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-02#0098` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-02#0099` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0100` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-02#0101` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0102` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-02#0103` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0104` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-02#0105` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-02#0106` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0107` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0108` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-02#0109` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0110` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-02#0111` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0112` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0113` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0114` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-02#0115` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0116` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0117` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0118` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-02#0119` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-02#0120` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-02#0121` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-02#0122` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-02#0123` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-02#0124` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0125` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-02#0126` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-02#0127` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-02#0128` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-02#0129` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-02#0130` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-02#0131` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-02#0132` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-02#0133` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-02#0134` | `boundary:containment:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-02#0135` | `boundary:containment:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-02#0136` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-02#0137` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-02#0138` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-02#0139` | `candidate:blob:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-02#0140` | `candidate:blob:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-02#0141` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-02#0142` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-02#0143` | `candidate:blob:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-02#0144` | `candidate:blob:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-02#0145` | `candidate:blob:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-02#0146` | `candidate:blob:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-02#0147` | `candidate:blob:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-02#0148` | `candidate:blob:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-02#0149` | `candidate:blob:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-02#0150` | `candidate:blob:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-02#0151` | `candidate:blob:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-02#0152` | `candidate:blob:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-02#0153` | `candidate:blob:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-02#0154` | `candidate:blob:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-02#0155` | `candidate:blob:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-02#0156` | `candidate:blob:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-02#0157` | `candidate:blob:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-02#0158` | `candidate:blob:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-02#0159` | `candidate:blob:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-02#0160` | `candidate:blob:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-02#0161` | `candidate:blob:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-02#0162` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0163` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-02#0164` | `candidate:blob:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-02#0165` | `candidate:blob:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-02#0166` | `candidate:blob:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-02#0167` | `candidate:blob:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-02#0168` | `candidate:blob:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-02#0169` | `candidate:blob:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-02#0170` | `candidate:blob:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-02#0171` | `candidate:blob:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-02#0172` | `candidate:blob:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-02#0173` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-02#0174` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0175` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-02#0176` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0177` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-02#0178` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0179` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-02#0180` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-02#0181` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0182` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0183` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-02#0184` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0185` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-02#0186` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0187` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0188` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0189` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-02#0190` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0191` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0192` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0193` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-02#0194` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-02#0195` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-02#0196` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-02#0197` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-02#0198` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-02#0199` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0200` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-02#0201` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-02#0202` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-02#0203` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-02#0204` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-02#0205` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-02#0206` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-02#0207` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-02#0208` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-02#0209` | `candidate:blob:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-02#0210` | `candidate:blob:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-02#0211` | `readers:approved-correction::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-02#0212` | `readers:inventory::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-02#0213` | `readers:binding::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0214` | `entry:repository::repository` | `PASS` |
| `AC-10.4-02#0215` | `entry:candidate::candidate` | `PASS` |
| `AC-10.4-02#0216` | `entry:baseline::baseline` | `PASS` |
| `AC-10.4-02#0217` | `entry:diff::diff` | `PASS` |
| `AC-10.4-02#0218` | `entry:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-02#0219` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0220` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-02#0221` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0222` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-02#0223` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-02#0224` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-02#0225` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0226` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0227` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-02#0228` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0229` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-02#0230` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0231` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0232` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0233` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-02#0234` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-02#0235` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0236` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0237` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0238` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-02#0239` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-02#0240` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-02#0241` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-02#0242` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-02#0243` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-02#0244` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0245` | `entry:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-02#0246` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0247` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0248` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0249` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-02#0250` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-02#0251` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-02#0252` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0253` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0254` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0255` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-02#0256` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-02#0257` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-02#0258` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-02#0259` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-02#0260` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-02#0261` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-02#0262` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-02#0263` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-02#0264` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0265` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0266` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0267` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0268` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0269` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0270` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-02#0271` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-02#0272` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-02#0273` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0274` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0275` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-02#0276` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-02#0277` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-02#0278` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-02#0279` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0280` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0281` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0282` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0283` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0284` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0285` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0286` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0287` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-02#0288` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-02#0289` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-02#0290` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-02#0291` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-02#0292` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-02#0293` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-02#0294` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0295` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0296` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0297` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0298` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0299` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-02#0300` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0301` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0302` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-02#0303` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-02#0304` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-02#0305` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-02#0306` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-02#0307` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-02#0308` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-02#0309` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-02#0310` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-02#0311` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-02#0312` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-02#0313` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-02#0314` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-02#0315` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-02#0316` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-02#0317` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-02#0318` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-02#0319` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-02#0320` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-02#0321` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0322` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0323` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-02#0324` | `reader:entry:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-02#0325` | `reader:strength:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-02#0326` | `reader:helper:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-02#0327` | `reader:project:Conformance.Tests::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-02#0328` | `reader:project:Conformance.Portable.Tests::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-02#0329` | `reader:project:Contracts.Tests::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-02#0330` | `reader:project:IntegrationTests::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-02#0331` | `readers:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-02#0332` | `projection:scope::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-02#0333` | `authority:selection::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-02#0334` | `predecessor:10.3::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |

### `AC-10.4-03`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Debug/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.DayOneExemptionInventoryShouldBeEmpty -trx artifacts/v9/10.4/AC-10.4-03.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-03.trx` | `7fb1b312def906d22dc83e3b0698b5cbab083e3f9dcba8849520c659d274574e` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-03#0001` | `Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.DayOneExemptionInventoryShouldBeEmpty` | `PASS` |
| `AC-10.4-03#0002` | `initial:repository::repository` | `PASS` |
| `AC-10.4-03#0003` | `initial:candidate::candidate` | `PASS` |
| `AC-10.4-03#0004` | `initial:baseline::baseline` | `PASS` |
| `AC-10.4-03#0005` | `initial:diff::diff` | `PASS` |
| `AC-10.4-03#0006` | `initial:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-03#0007` | `initial:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-03#0008` | `initial:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-03#0009` | `initial:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0010` | `initial:containment:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-03#0011` | `binding:baseline::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0012` | `binding:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0013` | `roots:independent::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-03#0014` | `baseline:repository::repository` | `PASS` |
| `AC-10.4-03#0015` | `baseline:candidate::candidate` | `PASS` |
| `AC-10.4-03#0016` | `baseline:baseline::baseline` | `PASS` |
| `AC-10.4-03#0017` | `baseline:diff::diff` | `PASS` |
| `AC-10.4-03#0018` | `baseline:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-03#0019` | `baseline:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-03#0020` | `gitlinks:amendment::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0021` | `gitlinks:transitions::nine exact mode-160000 transitions` | `PASS` |
| `AC-10.4-03#0022` | `gitlinks:provenance::twelve baseline root gitlinks` | `PASS` |
| `AC-10.4-03#0023` | `gitlinks:candidate::nine approved and three unchanged roots` | `PASS` |
| `AC-10.4-03#0024` | `provenance:membership::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0025` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-03#0026` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-03#0027` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-03#0028` | `provenance:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-03#0029` | `provenance:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-03#0030` | `provenance:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-03#0031` | `provenance:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-03#0032` | `inventory:workflowInventory::workflowInventory` | `PASS` |
| `AC-10.4-03#0033` | `inventory:guidanceInventory::guidanceInventory` | `PASS` |
| `AC-10.4-03#0034` | `workflow:entry:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-03#0035` | `workflow:entry:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-03#0036` | `workflow:entry:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-03#0037` | `workflow:entry:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-03#0038` | `workflow:entry:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-03#0039` | `workflow:entry:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-03#0040` | `workflow:entry:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-03#0041` | `workflow:entry:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-03#0042` | `workflow:entry:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-03#0043` | `workflow:entry:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-03#0044` | `binding:exact-sources::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0045` | `boundary:repository::repository` | `PASS` |
| `AC-10.4-03#0046` | `boundary:candidate::candidate` | `PASS` |
| `AC-10.4-03#0047` | `boundary:baseline::baseline` | `PASS` |
| `AC-10.4-03#0048` | `boundary:diff::diff` | `PASS` |
| `AC-10.4-03#0049` | `boundary:changed-paths::changed-paths` | `PASS` |
| `AC-10.4-03#0050` | `boundary:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-03#0051` | `boundary:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-03#0052` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-03#0053` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-03#0054` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-03#0055` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-03#0056` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-03#0057` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-03#0058` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-03#0059` | `boundary:manifest:file:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-03#0060` | `boundary:manifest:file:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-03#0061` | `boundary:manifest:file:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-03#0062` | `boundary:manifest:file:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-03#0063` | `boundary:manifest:file:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-03#0064` | `boundary:manifest:file:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-03#0065` | `boundary:manifest:file:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-03#0066` | `boundary:manifest:file:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-03#0067` | `boundary:manifest:file:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-03#0068` | `boundary:manifest:file:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-03#0069` | `boundary:manifest:file:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-03#0070` | `boundary:manifest:file:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-03#0071` | `boundary:manifest:file:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-03#0072` | `boundary:manifest:file:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-03#0073` | `boundary:manifest:file:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-03#0074` | `boundary:manifest:file:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-03#0075` | `boundary:manifest:signable-payload::manifest` | `PASS` |
| `AC-10.4-03#0076` | `boundary:roots:compiled::roots:compiled` | `PASS` |
| `AC-10.4-03#0077` | `boundary:roots::roots` | `PASS` |
| `AC-10.4-03#0078` | `boundary:allowlist::allowlist` | `PASS` |
| `AC-10.4-03#0079` | `boundary:containment:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-03#0080` | `boundary:containment:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-03#0081` | `boundary:containment:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-03#0082` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0083` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-03#0084` | `boundary:containment:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-03#0085` | `boundary:containment:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-03#0086` | `boundary:containment:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-03#0087` | `boundary:containment:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-03#0088` | `boundary:containment:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-03#0089` | `boundary:containment:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-03#0090` | `boundary:containment:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-03#0091` | `boundary:containment:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-03#0092` | `boundary:containment:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-03#0093` | `boundary:containment:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-03#0094` | `boundary:containment:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-03#0095` | `boundary:containment:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-03#0096` | `boundary:containment:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-03#0097` | `boundary:containment:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-03#0098` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-03#0099` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0100` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-03#0101` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0102` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-03#0103` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0104` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-03#0105` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-03#0106` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0107` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0108` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-03#0109` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0110` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-03#0111` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0112` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0113` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0114` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-03#0115` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0116` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0117` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0118` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-03#0119` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-03#0120` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-03#0121` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-03#0122` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-03#0123` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-03#0124` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0125` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-03#0126` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-03#0127` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-03#0128` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-03#0129` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-03#0130` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-03#0131` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-03#0132` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-03#0133` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-03#0134` | `boundary:containment:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-03#0135` | `boundary:containment:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-03#0136` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-03#0137` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-03#0138` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-03#0139` | `candidate:blob:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-03#0140` | `candidate:blob:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-03#0141` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-03#0142` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-03#0143` | `candidate:blob:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-03#0144` | `candidate:blob:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-03#0145` | `candidate:blob:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-03#0146` | `candidate:blob:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-03#0147` | `candidate:blob:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-03#0148` | `candidate:blob:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-03#0149` | `candidate:blob:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-03#0150` | `candidate:blob:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-03#0151` | `candidate:blob:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-03#0152` | `candidate:blob:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-03#0153` | `candidate:blob:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-03#0154` | `candidate:blob:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-03#0155` | `candidate:blob:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-03#0156` | `candidate:blob:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-03#0157` | `candidate:blob:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-03#0158` | `candidate:blob:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-03#0159` | `candidate:blob:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-03#0160` | `candidate:blob:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-03#0161` | `candidate:blob:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-03#0162` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0163` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-03#0164` | `candidate:blob:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-03#0165` | `candidate:blob:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-03#0166` | `candidate:blob:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-03#0167` | `candidate:blob:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-03#0168` | `candidate:blob:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-03#0169` | `candidate:blob:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-03#0170` | `candidate:blob:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-03#0171` | `candidate:blob:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-03#0172` | `candidate:blob:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-03#0173` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-03#0174` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0175` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-03#0176` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0177` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-03#0178` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0179` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-03#0180` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-03#0181` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0182` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0183` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-03#0184` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0185` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-03#0186` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0187` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0188` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0189` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-03#0190` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0191` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0192` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0193` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-03#0194` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-03#0195` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-03#0196` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-03#0197` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-03#0198` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-03#0199` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0200` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-03#0201` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-03#0202` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-03#0203` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-03#0204` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-03#0205` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-03#0206` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-03#0207` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-03#0208` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-03#0209` | `candidate:blob:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-03#0210` | `candidate:blob:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-03#0211` | `readers:approved-correction::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-03#0212` | `readers:inventory::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-03#0213` | `readers:binding::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0214` | `entry:repository::repository` | `PASS` |
| `AC-10.4-03#0215` | `entry:candidate::candidate` | `PASS` |
| `AC-10.4-03#0216` | `entry:baseline::baseline` | `PASS` |
| `AC-10.4-03#0217` | `entry:diff::diff` | `PASS` |
| `AC-10.4-03#0218` | `entry:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-03#0219` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0220` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-03#0221` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0222` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-03#0223` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-03#0224` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-03#0225` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0226` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0227` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-03#0228` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0229` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-03#0230` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0231` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0232` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0233` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-03#0234` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-03#0235` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0236` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0237` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0238` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-03#0239` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-03#0240` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-03#0241` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-03#0242` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-03#0243` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-03#0244` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0245` | `entry:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-03#0246` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0247` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0248` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0249` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-03#0250` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-03#0251` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-03#0252` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0253` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0254` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0255` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-03#0256` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-03#0257` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-03#0258` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-03#0259` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-03#0260` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-03#0261` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-03#0262` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-03#0263` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-03#0264` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0265` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0266` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0267` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0268` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0269` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0270` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-03#0271` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-03#0272` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-03#0273` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0274` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0275` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-03#0276` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-03#0277` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-03#0278` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-03#0279` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0280` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0281` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0282` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0283` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0284` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0285` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0286` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0287` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-03#0288` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-03#0289` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-03#0290` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-03#0291` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-03#0292` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-03#0293` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-03#0294` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0295` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0296` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0297` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0298` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0299` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-03#0300` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0301` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0302` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-03#0303` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-03#0304` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-03#0305` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-03#0306` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-03#0307` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-03#0308` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-03#0309` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-03#0310` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-03#0311` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-03#0312` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-03#0313` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-03#0314` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-03#0315` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-03#0316` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-03#0317` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-03#0318` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-03#0319` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-03#0320` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-03#0321` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0322` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0323` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-03#0324` | `reader:entry:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-03#0325` | `reader:strength:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-03#0326` | `reader:helper:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-03#0327` | `reader:project:Conformance.Tests::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-03#0328` | `reader:project:Conformance.Portable.Tests::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-03#0329` | `reader:project:Contracts.Tests::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-03#0330` | `reader:project:IntegrationTests::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-03#0331` | `readers:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-03#0332` | `projection:scope::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-03#0333` | `authority:selection::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-03#0334` | `predecessor:10.3::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |

### `AC-10.4-04`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Debug/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.KnownGitDefectsShouldBeClosedWithoutChangingProjectionProof -trx artifacts/v9/10.4/AC-10.4-04.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-04.trx` | `643ea1ac67846005d829269752c2e88896e93cdb34ea43fa30e8eeff633687d1` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-04#0001` | `Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.KnownGitDefectsShouldBeClosedWithoutChangingProjectionProof` | `PASS` |
| `AC-10.4-04#0002` | `initial:repository::repository` | `PASS` |
| `AC-10.4-04#0003` | `initial:candidate::candidate` | `PASS` |
| `AC-10.4-04#0004` | `initial:baseline::baseline` | `PASS` |
| `AC-10.4-04#0005` | `initial:diff::diff` | `PASS` |
| `AC-10.4-04#0006` | `initial:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-04#0007` | `initial:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-04#0008` | `initial:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-04#0009` | `initial:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0010` | `initial:containment:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-04#0011` | `binding:baseline::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0012` | `binding:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0013` | `roots:independent::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-04#0014` | `baseline:repository::repository` | `PASS` |
| `AC-10.4-04#0015` | `baseline:candidate::candidate` | `PASS` |
| `AC-10.4-04#0016` | `baseline:baseline::baseline` | `PASS` |
| `AC-10.4-04#0017` | `baseline:diff::diff` | `PASS` |
| `AC-10.4-04#0018` | `baseline:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-04#0019` | `baseline:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-04#0020` | `gitlinks:amendment::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0021` | `gitlinks:transitions::nine exact mode-160000 transitions` | `PASS` |
| `AC-10.4-04#0022` | `gitlinks:provenance::twelve baseline root gitlinks` | `PASS` |
| `AC-10.4-04#0023` | `gitlinks:candidate::nine approved and three unchanged roots` | `PASS` |
| `AC-10.4-04#0024` | `provenance:membership::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0025` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-04#0026` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-04#0027` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-04#0028` | `provenance:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-04#0029` | `provenance:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-04#0030` | `provenance:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-04#0031` | `provenance:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-04#0032` | `inventory:workflowInventory::workflowInventory` | `PASS` |
| `AC-10.4-04#0033` | `inventory:guidanceInventory::guidanceInventory` | `PASS` |
| `AC-10.4-04#0034` | `workflow:entry:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-04#0035` | `workflow:entry:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-04#0036` | `workflow:entry:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-04#0037` | `workflow:entry:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-04#0038` | `workflow:entry:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-04#0039` | `workflow:entry:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-04#0040` | `workflow:entry:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-04#0041` | `workflow:entry:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-04#0042` | `workflow:entry:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-04#0043` | `workflow:entry:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-04#0044` | `binding:exact-sources::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0045` | `boundary:repository::repository` | `PASS` |
| `AC-10.4-04#0046` | `boundary:candidate::candidate` | `PASS` |
| `AC-10.4-04#0047` | `boundary:baseline::baseline` | `PASS` |
| `AC-10.4-04#0048` | `boundary:diff::diff` | `PASS` |
| `AC-10.4-04#0049` | `boundary:changed-paths::changed-paths` | `PASS` |
| `AC-10.4-04#0050` | `boundary:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-04#0051` | `boundary:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-04#0052` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-04#0053` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-04#0054` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-04#0055` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-04#0056` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-04#0057` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-04#0058` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-04#0059` | `boundary:manifest:file:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-04#0060` | `boundary:manifest:file:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-04#0061` | `boundary:manifest:file:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-04#0062` | `boundary:manifest:file:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-04#0063` | `boundary:manifest:file:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-04#0064` | `boundary:manifest:file:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-04#0065` | `boundary:manifest:file:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-04#0066` | `boundary:manifest:file:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-04#0067` | `boundary:manifest:file:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-04#0068` | `boundary:manifest:file:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-04#0069` | `boundary:manifest:file:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-04#0070` | `boundary:manifest:file:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-04#0071` | `boundary:manifest:file:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-04#0072` | `boundary:manifest:file:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-04#0073` | `boundary:manifest:file:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-04#0074` | `boundary:manifest:file:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-04#0075` | `boundary:manifest:signable-payload::manifest` | `PASS` |
| `AC-10.4-04#0076` | `boundary:roots:compiled::roots:compiled` | `PASS` |
| `AC-10.4-04#0077` | `boundary:roots::roots` | `PASS` |
| `AC-10.4-04#0078` | `boundary:allowlist::allowlist` | `PASS` |
| `AC-10.4-04#0079` | `boundary:containment:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-04#0080` | `boundary:containment:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-04#0081` | `boundary:containment:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-04#0082` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0083` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-04#0084` | `boundary:containment:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-04#0085` | `boundary:containment:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-04#0086` | `boundary:containment:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-04#0087` | `boundary:containment:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-04#0088` | `boundary:containment:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-04#0089` | `boundary:containment:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-04#0090` | `boundary:containment:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-04#0091` | `boundary:containment:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-04#0092` | `boundary:containment:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-04#0093` | `boundary:containment:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-04#0094` | `boundary:containment:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-04#0095` | `boundary:containment:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-04#0096` | `boundary:containment:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-04#0097` | `boundary:containment:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-04#0098` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-04#0099` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0100` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-04#0101` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0102` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-04#0103` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0104` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-04#0105` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-04#0106` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0107` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0108` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-04#0109` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0110` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-04#0111` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0112` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0113` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0114` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-04#0115` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0116` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0117` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0118` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-04#0119` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-04#0120` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-04#0121` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-04#0122` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-04#0123` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-04#0124` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0125` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-04#0126` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-04#0127` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-04#0128` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-04#0129` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-04#0130` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-04#0131` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-04#0132` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-04#0133` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-04#0134` | `boundary:containment:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-04#0135` | `boundary:containment:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-04#0136` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-04#0137` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-04#0138` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-04#0139` | `candidate:blob:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-04#0140` | `candidate:blob:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-04#0141` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-04#0142` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-04#0143` | `candidate:blob:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-04#0144` | `candidate:blob:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-04#0145` | `candidate:blob:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-04#0146` | `candidate:blob:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-04#0147` | `candidate:blob:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-04#0148` | `candidate:blob:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-04#0149` | `candidate:blob:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-04#0150` | `candidate:blob:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-04#0151` | `candidate:blob:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-04#0152` | `candidate:blob:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-04#0153` | `candidate:blob:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-04#0154` | `candidate:blob:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-04#0155` | `candidate:blob:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-04#0156` | `candidate:blob:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-04#0157` | `candidate:blob:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-04#0158` | `candidate:blob:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-04#0159` | `candidate:blob:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-04#0160` | `candidate:blob:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-04#0161` | `candidate:blob:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-04#0162` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0163` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-04#0164` | `candidate:blob:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-04#0165` | `candidate:blob:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-04#0166` | `candidate:blob:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-04#0167` | `candidate:blob:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-04#0168` | `candidate:blob:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-04#0169` | `candidate:blob:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-04#0170` | `candidate:blob:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-04#0171` | `candidate:blob:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-04#0172` | `candidate:blob:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-04#0173` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-04#0174` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0175` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-04#0176` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0177` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-04#0178` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0179` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-04#0180` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-04#0181` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0182` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0183` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-04#0184` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0185` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-04#0186` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0187` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0188` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0189` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-04#0190` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0191` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0192` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0193` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-04#0194` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-04#0195` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-04#0196` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-04#0197` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-04#0198` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-04#0199` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0200` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-04#0201` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-04#0202` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-04#0203` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-04#0204` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-04#0205` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-04#0206` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-04#0207` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-04#0208` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-04#0209` | `candidate:blob:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-04#0210` | `candidate:blob:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-04#0211` | `readers:approved-correction::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-04#0212` | `readers:inventory::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-04#0213` | `readers:binding::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0214` | `entry:repository::repository` | `PASS` |
| `AC-10.4-04#0215` | `entry:candidate::candidate` | `PASS` |
| `AC-10.4-04#0216` | `entry:baseline::baseline` | `PASS` |
| `AC-10.4-04#0217` | `entry:diff::diff` | `PASS` |
| `AC-10.4-04#0218` | `entry:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-04#0219` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0220` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-04#0221` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0222` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-04#0223` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-04#0224` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-04#0225` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0226` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0227` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-04#0228` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0229` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-04#0230` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0231` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0232` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0233` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-04#0234` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-04#0235` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0236` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0237` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0238` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-04#0239` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-04#0240` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-04#0241` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-04#0242` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-04#0243` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-04#0244` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0245` | `entry:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-04#0246` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0247` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0248` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0249` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-04#0250` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-04#0251` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-04#0252` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0253` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0254` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0255` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-04#0256` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-04#0257` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-04#0258` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-04#0259` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-04#0260` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-04#0261` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-04#0262` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-04#0263` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-04#0264` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0265` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0266` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0267` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0268` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0269` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0270` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-04#0271` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-04#0272` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-04#0273` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0274` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0275` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-04#0276` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-04#0277` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-04#0278` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-04#0279` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0280` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0281` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0282` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0283` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0284` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0285` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0286` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0287` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-04#0288` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-04#0289` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-04#0290` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-04#0291` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-04#0292` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-04#0293` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-04#0294` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0295` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0296` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0297` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0298` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0299` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-04#0300` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0301` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0302` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-04#0303` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-04#0304` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-04#0305` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-04#0306` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-04#0307` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-04#0308` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-04#0309` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-04#0310` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-04#0311` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-04#0312` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-04#0313` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-04#0314` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-04#0315` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-04#0316` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-04#0317` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-04#0318` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-04#0319` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-04#0320` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-04#0321` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0322` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0323` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-04#0324` | `reader:entry:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-04#0325` | `reader:strength:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-04#0326` | `reader:helper:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-04#0327` | `reader:project:Conformance.Tests::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-04#0328` | `reader:project:Conformance.Portable.Tests::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-04#0329` | `reader:project:Contracts.Tests::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-04#0330` | `reader:project:IntegrationTests::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-04#0331` | `readers:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-04#0332` | `projection:scope::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-04#0333` | `authority:selection::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-04#0334` | `predecessor:10.3::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |

### `AC-10.4-05`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Debug/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.RunbookShouldDocumentTheExactContract -trx artifacts/v9/10.4/AC-10.4-05.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-05.trx` | `71479e301db4b7d67e2e7e7e4f8eaa818b92fd3bce5b0a531528cef87285d15b` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-05#0001` | `Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.RunbookShouldDocumentTheExactContract` | `PASS` |
| `AC-10.4-05#0002` | `initial:repository::repository` | `PASS` |
| `AC-10.4-05#0003` | `initial:candidate::candidate` | `PASS` |
| `AC-10.4-05#0004` | `initial:baseline::baseline` | `PASS` |
| `AC-10.4-05#0005` | `initial:diff::diff` | `PASS` |
| `AC-10.4-05#0006` | `initial:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-05#0007` | `initial:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-05#0008` | `initial:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-05#0009` | `initial:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0010` | `initial:containment:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-05#0011` | `binding:baseline::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0012` | `binding:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0013` | `roots:independent::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-05#0014` | `baseline:repository::repository` | `PASS` |
| `AC-10.4-05#0015` | `baseline:candidate::candidate` | `PASS` |
| `AC-10.4-05#0016` | `baseline:baseline::baseline` | `PASS` |
| `AC-10.4-05#0017` | `baseline:diff::diff` | `PASS` |
| `AC-10.4-05#0018` | `baseline:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-05#0019` | `baseline:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-05#0020` | `gitlinks:amendment::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0021` | `gitlinks:transitions::nine exact mode-160000 transitions` | `PASS` |
| `AC-10.4-05#0022` | `gitlinks:provenance::twelve baseline root gitlinks` | `PASS` |
| `AC-10.4-05#0023` | `gitlinks:candidate::nine approved and three unchanged roots` | `PASS` |
| `AC-10.4-05#0024` | `provenance:membership::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0025` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-05#0026` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-05#0027` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-05#0028` | `provenance:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-05#0029` | `provenance:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-05#0030` | `provenance:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-05#0031` | `provenance:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-05#0032` | `inventory:workflowInventory::workflowInventory` | `PASS` |
| `AC-10.4-05#0033` | `inventory:guidanceInventory::guidanceInventory` | `PASS` |
| `AC-10.4-05#0034` | `workflow:entry:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-05#0035` | `workflow:entry:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-05#0036` | `workflow:entry:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-05#0037` | `workflow:entry:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-05#0038` | `workflow:entry:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-05#0039` | `workflow:entry:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-05#0040` | `workflow:entry:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-05#0041` | `workflow:entry:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-05#0042` | `workflow:entry:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-05#0043` | `workflow:entry:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-05#0044` | `binding:exact-sources::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0045` | `boundary:repository::repository` | `PASS` |
| `AC-10.4-05#0046` | `boundary:candidate::candidate` | `PASS` |
| `AC-10.4-05#0047` | `boundary:baseline::baseline` | `PASS` |
| `AC-10.4-05#0048` | `boundary:diff::diff` | `PASS` |
| `AC-10.4-05#0049` | `boundary:changed-paths::changed-paths` | `PASS` |
| `AC-10.4-05#0050` | `boundary:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-05#0051` | `boundary:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-05#0052` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-05#0053` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-05#0054` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-05#0055` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-05#0056` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-05#0057` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-05#0058` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-05#0059` | `boundary:manifest:file:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-05#0060` | `boundary:manifest:file:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-05#0061` | `boundary:manifest:file:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-05#0062` | `boundary:manifest:file:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-05#0063` | `boundary:manifest:file:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-05#0064` | `boundary:manifest:file:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-05#0065` | `boundary:manifest:file:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-05#0066` | `boundary:manifest:file:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-05#0067` | `boundary:manifest:file:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-05#0068` | `boundary:manifest:file:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-05#0069` | `boundary:manifest:file:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-05#0070` | `boundary:manifest:file:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-05#0071` | `boundary:manifest:file:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-05#0072` | `boundary:manifest:file:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-05#0073` | `boundary:manifest:file:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-05#0074` | `boundary:manifest:file:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-05#0075` | `boundary:manifest:signable-payload::manifest` | `PASS` |
| `AC-10.4-05#0076` | `boundary:roots:compiled::roots:compiled` | `PASS` |
| `AC-10.4-05#0077` | `boundary:roots::roots` | `PASS` |
| `AC-10.4-05#0078` | `boundary:allowlist::allowlist` | `PASS` |
| `AC-10.4-05#0079` | `boundary:containment:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-05#0080` | `boundary:containment:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-05#0081` | `boundary:containment:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-05#0082` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0083` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-05#0084` | `boundary:containment:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-05#0085` | `boundary:containment:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-05#0086` | `boundary:containment:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-05#0087` | `boundary:containment:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-05#0088` | `boundary:containment:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-05#0089` | `boundary:containment:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-05#0090` | `boundary:containment:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-05#0091` | `boundary:containment:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-05#0092` | `boundary:containment:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-05#0093` | `boundary:containment:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-05#0094` | `boundary:containment:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-05#0095` | `boundary:containment:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-05#0096` | `boundary:containment:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-05#0097` | `boundary:containment:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-05#0098` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-05#0099` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0100` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-05#0101` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-05#0102` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-05#0103` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-05#0104` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-05#0105` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-05#0106` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-05#0107` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0108` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-05#0109` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-05#0110` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-05#0111` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0112` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-05#0113` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0114` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-05#0115` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-05#0116` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-05#0117` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-05#0118` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-05#0119` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-05#0120` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-05#0121` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-05#0122` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-05#0123` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-05#0124` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-05#0125` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-05#0126` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-05#0127` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-05#0128` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-05#0129` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-05#0130` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-05#0131` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-05#0132` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-05#0133` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-05#0134` | `boundary:containment:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-05#0135` | `boundary:containment:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-05#0136` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-05#0137` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-05#0138` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-05#0139` | `candidate:blob:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-05#0140` | `candidate:blob:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-05#0141` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-05#0142` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-05#0143` | `candidate:blob:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-05#0144` | `candidate:blob:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-05#0145` | `candidate:blob:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-05#0146` | `candidate:blob:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-05#0147` | `candidate:blob:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-05#0148` | `candidate:blob:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-05#0149` | `candidate:blob:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-05#0150` | `candidate:blob:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-05#0151` | `candidate:blob:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-05#0152` | `candidate:blob:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-05#0153` | `candidate:blob:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-05#0154` | `candidate:blob:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-05#0155` | `candidate:blob:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-05#0156` | `candidate:blob:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-05#0157` | `candidate:blob:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-05#0158` | `candidate:blob:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-05#0159` | `candidate:blob:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-05#0160` | `candidate:blob:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-05#0161` | `candidate:blob:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-05#0162` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-05#0163` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-05#0164` | `candidate:blob:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-05#0165` | `candidate:blob:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-05#0166` | `candidate:blob:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-05#0167` | `candidate:blob:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-05#0168` | `candidate:blob:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-05#0169` | `candidate:blob:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-05#0170` | `candidate:blob:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-05#0171` | `candidate:blob:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-05#0172` | `candidate:blob:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-05#0173` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-05#0174` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0175` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-05#0176` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-05#0177` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-05#0178` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-05#0179` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-05#0180` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-05#0181` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-05#0182` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0183` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-05#0184` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-05#0185` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-05#0186` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0187` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-05#0188` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-05#0189` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-05#0190` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-05#0191` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-05#0192` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-05#0193` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-05#0194` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-05#0195` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-05#0196` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-05#0197` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-05#0198` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-05#0199` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-05#0200` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-05#0201` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-05#0202` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-05#0203` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-05#0204` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-05#0205` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-05#0206` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-05#0207` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-05#0208` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-05#0209` | `candidate:blob:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-05#0210` | `candidate:blob:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-05#0211` | `guidance:present:bmad-build::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-05#0212` | `guidance:content:bmad-build::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-05#0213` | `guidance:resolved:bmad-build::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-05#0214` | `guidance:render:.agents/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-05#0215` | `guidance:render-used:.agents/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-05#0216` | `guidance:render:.claude/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-05#0217` | `guidance:render-used:.claude/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-05#0218` | `guidance:present:bmad-build-auto::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-05#0219` | `guidance:content:bmad-build-auto::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-05#0220` | `guidance:resolved:bmad-build-auto::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-05#0221` | `guidance:render:.agents/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-05#0222` | `guidance:render-used:.agents/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-05#0223` | `guidance:render:.claude/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-05#0224` | `guidance:render-used:.claude/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-05#0225` | `guidance:present:bmad-review::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-05#0226` | `guidance:content:bmad-review::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-05#0227` | `guidance:resolved:bmad-review::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-05#0228` | `runbook:sections::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-05#0229` | `runbook:contract::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-05#0230` | `guidance:hash:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-05#0231` | `guidance:hash:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-05#0232` | `guidance:hash:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-05#0233` | `guidance:hash:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-05#0234` | `guidance:hash:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-05#0235` | `authority:selection::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-05#0236` | `predecessor:10.3::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |

### `AC-10.4-06`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k complete_fault_matrix --junitxml=artifacts/v9/10.4/AC-10.4-06.xml`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-06.xml` | `94faa9000e8fdcbfc4a803d53777d061ff827837874bbda14557c388d1d3fb4a` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-06#0001` | `test_complete_fault_matrix_chain[CHAIN_CHANGED-EVIDENCE_CHAIN_TABLE_INVALID]` | `PASS` |
| `AC-10.4-06#0002` | `test_complete_fault_matrix_chain[CHAIN_MISSING-EVIDENCE_CHAIN_TABLE_INVALID]` | `PASS` |
| `AC-10.4-06#0003` | `test_complete_fault_matrix_chain[CHAIN_REORDERED-EVIDENCE_CHAIN_TABLE_INVALID]` | `PASS` |
| `AC-10.4-06#0004` | `test_complete_fault_matrix_chain[CHAIN_NESTED-EVIDENCE_CHAIN_MARKER_INVALID]` | `PASS` |
| `AC-10.4-06#0005` | `test_complete_fault_matrix_chain[CHAIN_DUPLICATE-EVIDENCE_CHAIN_MARKER_INVALID]` | `PASS` |
| `AC-10.4-06#0006` | `test_complete_fault_matrix_chain[CHAIN_UNTERMINATED-EVIDENCE_CHAIN_MARKER_INVALID]` | `PASS` |
| `AC-10.4-06#0007` | `test_complete_fault_matrix_chain[CHAIN_DUPLICATE_ROW-EVIDENCE_CHAIN_TABLE_INVALID]` | `PASS` |
| `AC-10.4-06#0008` | `test_complete_fault_matrix_guidance[GUIDANCE_REMOVED-EVIDENCE_GUIDANCE_NOT_USED]` | `PASS` |
| `AC-10.4-06#0009` | `test_complete_fault_matrix_guidance[GUIDANCE_EQUALITY-EVIDENCE_GUIDANCE_DRIFT]` | `PASS` |
| `AC-10.4-06#0010` | `test_complete_fault_matrix_guidance[GUIDANCE_DECLARED_HASH-EVIDENCE_GUIDANCE_DRIFT]` | `PASS` |
| `AC-10.4-06#0011` | `test_complete_fault_matrix_guidance[GUIDANCE_REDIRECT-EVIDENCE_GUIDANCE_DRIFT]` | `PASS` |
| `AC-10.4-06#0012` | `test_complete_fault_matrix_integrity[HASH-EVIDENCE_HASH_MISMATCH]` | `PASS` |
| `AC-10.4-06#0013` | `test_complete_fault_matrix_integrity[PATH_ESCAPE-EVIDENCE_PATH_ESCAPE]` | `PASS` |
| `AC-10.4-06#0014` | `test_complete_fault_matrix_integrity[GENERATED_OUTPUT-EVIDENCE_OUTPUT_FORBIDDEN]` | `PASS` |
| `AC-10.4-06#0015` | `test_complete_fault_matrix_integrity[SUBSET-CHANGED_PATH_SET_MISMATCH]` | `PASS` |
| `AC-10.4-06#0016` | `test_complete_fault_matrix_integrity[SIGNED_ALLOWLIST-SIGNED_EVIDENCE_ALLOWLISTED]` | `PASS` |
| `AC-10.4-06#0017` | `test_complete_fault_matrix_integrity[SIGNABLE-SIGNABLE_PAYLOAD_MISMATCH]` | `PASS` |
| `AC-10.4-06#0018` | `test_complete_fault_matrix_integrity[ROOT-ROOT_OF_TRUST_NOT_PINNED]` | `PASS` |
| `AC-10.4-06#0019` | `test_complete_fault_matrix_integrity[GIT_UNAVAILABLE-GIT_UNAVAILABLE]` | `PASS` |
| `AC-10.4-06#0020` | `test_complete_fault_matrix_integrity[HISTORY_UNAVAILABLE-REVISION_INVALID]` | `PASS` |
| `AC-10.4-06#0021` | `test_complete_fault_matrix_integrity[GITLINK-EVIDENCE_BOUNDARY_GITLINK]` | `PASS` |
| `AC-10.4-06#0022` | `test_complete_fault_matrix_readers[READER_STRENGTH-ASSERTION_STRENGTH_WEAKENED]` | `PASS` |
| `AC-10.4-06#0023` | `test_complete_fault_matrix_readers[READER_HELPER-EVIDENCE_HELPER_NOT_USED]` | `PASS` |
| `AC-10.4-06#0024` | `test_complete_fault_matrix_readers[EXEMPTION-DAY_ONE_EXEMPTION_FORBIDDEN]` | `PASS` |
| `AC-10.4-06#0025` | `test_complete_fault_matrix_readers[READER_INVENTORY-EVIDENCE_READER_INVENTORY_DRIFT]` | `PASS` |
| `AC-10.4-06#0026` | `test_complete_fault_matrix_workflow[WORKFLOW_REMOVED-EVIDENCE_GATE_NOT_USED]` | `PASS` |
| `AC-10.4-06#0027` | `test_complete_fault_matrix_workflow[MARKER_MALFORMED-EVIDENCE_GATE_NOT_USED]` | `PASS` |
| `AC-10.4-06#0028` | `test_complete_fault_matrix_workflow[SPAN_DISPLACED-EVIDENCE_GATE_DISPLACED]` | `PASS` |

### `AC-10.4-07`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_verify_submodule_promotion.py -k evidence_gate_span_coupling --junitxml=artifacts/v9/10.4/AC-10.4-07.xml`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-07.xml` | `08c646dd589ff533267928dc4e3c6a4caa8f069fda503c87e936024235c072fd` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-07#0001` | `test_evidence_gate_span_coupling[exact-span]` | `PASS` |
| `AC-10.4-07#0002` | `test_evidence_gate_span_coupling[displaced-span]` | `PASS` |

### `AC-10.4-08`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/story-10.4-current-contract-v1.json --format bundle --output-json docs/release-evidence/story-10.4-final-record-v2.json --output-markdown docs/release-evidence/story-10.4-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-08#0001` | `eight-measured-candidate-bound-receipts-including-AC-09` | `PASS` |
| `AC-10.4-08#0002` | `verified-10.3-predecessor` | `PASS` |
| `AC-10.4-08#0003` | `27-exact-reader-substitutions-and-zero-exemptions` | `PASS` |
| `AC-10.4-08#0004` | `29-real-faults-and-byte-identical-restoration` | `PASS` |
| `AC-10.4-08#0005` | `ten-workflow-spans-and-resolved-guidance` | `PASS` |
| `AC-10.4-08#0006` | `nine-approved-raw-gitlink-transitions-and-three-unchanged-roots` | `PASS` |
| `AC-10.4-08#0007` | `closed-schema-and-deterministic-pair` | `PASS` |

### `AC-10.4-09`

Command: `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --current-binding _bmad-output/planning-artifacts/story-10.4-current-binding-v1.json --baseline 1915a47123737859e3ac31872fa787fa8c0bd1e3 --candidate HEAD --verify-workflows --format json --output artifacts/v9/10.4/AC-10.4-09.json`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.4/AC-10.4-09.json` | `a5ecb193a31bc290d34d08bc23fc72e4c8de05bdbff2a36807f77be094affe77` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.4-09#0001` | `initial:repository::repository` | `PASS` |
| `AC-10.4-09#0002` | `initial:candidate::candidate` | `PASS` |
| `AC-10.4-09#0003` | `initial:baseline::baseline` | `PASS` |
| `AC-10.4-09#0004` | `initial:diff::diff` | `PASS` |
| `AC-10.4-09#0005` | `initial:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-09#0006` | `initial:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-09#0007` | `initial:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-09#0008` | `initial:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0009` | `initial:containment:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-09#0010` | `binding:baseline::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0011` | `binding:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0012` | `roots:independent::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-09#0013` | `baseline:repository::repository` | `PASS` |
| `AC-10.4-09#0014` | `baseline:candidate::candidate` | `PASS` |
| `AC-10.4-09#0015` | `baseline:baseline::baseline` | `PASS` |
| `AC-10.4-09#0016` | `baseline:diff::diff` | `PASS` |
| `AC-10.4-09#0017` | `baseline:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-09#0018` | `baseline:roots:tree::roots:tree` | `PASS` |
| `AC-10.4-09#0019` | `gitlinks:amendment::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0020` | `gitlinks:transitions::nine exact mode-160000 transitions` | `PASS` |
| `AC-10.4-09#0021` | `gitlinks:provenance::twelve baseline root gitlinks` | `PASS` |
| `AC-10.4-09#0022` | `gitlinks:candidate::nine approved and three unchanged roots` | `PASS` |
| `AC-10.4-09#0023` | `provenance:membership::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0024` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-09#0025` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-09#0026` | `provenance:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-09#0027` | `provenance:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-09#0028` | `provenance:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-09#0029` | `provenance:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-09#0030` | `provenance:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-09#0031` | `inventory:workflowInventory::workflowInventory` | `PASS` |
| `AC-10.4-09#0032` | `inventory:guidanceInventory::guidanceInventory` | `PASS` |
| `AC-10.4-09#0033` | `workflow:entry:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0034` | `workflow:entry:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0035` | `workflow:entry:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0036` | `workflow:entry:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0037` | `workflow:entry:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0038` | `workflow:entry:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0039` | `workflow:entry:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0040` | `workflow:entry:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0041` | `workflow:entry:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0042` | `workflow:entry:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0043` | `binding:exact-sources::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0044` | `boundary:repository::repository` | `PASS` |
| `AC-10.4-09#0045` | `boundary:candidate::candidate` | `PASS` |
| `AC-10.4-09#0046` | `boundary:baseline::baseline` | `PASS` |
| `AC-10.4-09#0047` | `boundary:diff::diff` | `PASS` |
| `AC-10.4-09#0048` | `boundary:changed-paths::changed-paths` | `PASS` |
| `AC-10.4-09#0049` | `boundary:gitlinks:story104-pins::gitlinks:story104-pins` | `PASS` |
| `AC-10.4-09#0050` | `boundary:gitlinks:story104-transitions::gitlinks:story104-transitions` | `PASS` |
| `AC-10.4-09#0051` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-09#0052` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-09#0053` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-09#0054` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-09#0055` | `boundary:manifest:file:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-09#0056` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-09#0057` | `boundary:manifest:file:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-09#0058` | `boundary:manifest:file:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0059` | `boundary:manifest:file:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0060` | `boundary:manifest:file:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0061` | `boundary:manifest:file:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0062` | `boundary:manifest:file:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0063` | `boundary:manifest:file:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0064` | `boundary:manifest:file:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0065` | `boundary:manifest:file:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0066` | `boundary:manifest:file:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0067` | `boundary:manifest:file:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0068` | `boundary:manifest:file:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0069` | `boundary:manifest:file:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0070` | `boundary:manifest:file:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0071` | `boundary:manifest:file:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-09#0072` | `boundary:manifest:file:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-09#0073` | `boundary:manifest:file:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-09#0074` | `boundary:manifest:signable-payload::manifest` | `PASS` |
| `AC-10.4-09#0075` | `boundary:roots:compiled::roots:compiled` | `PASS` |
| `AC-10.4-09#0076` | `boundary:roots::roots` | `PASS` |
| `AC-10.4-09#0077` | `boundary:allowlist::allowlist` | `PASS` |
| `AC-10.4-09#0078` | `boundary:containment:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-09#0079` | `boundary:containment:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-09#0080` | `boundary:containment:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-09#0081` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0082` | `boundary:containment:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-09#0083` | `boundary:containment:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0084` | `boundary:containment:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0085` | `boundary:containment:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0086` | `boundary:containment:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-09#0087` | `boundary:containment:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-09#0088` | `boundary:containment:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-09#0089` | `boundary:containment:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-09#0090` | `boundary:containment:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-09#0091` | `boundary:containment:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-09#0092` | `boundary:containment:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-09#0093` | `boundary:containment:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-09#0094` | `boundary:containment:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-09#0095` | `boundary:containment:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-09#0096` | `boundary:containment:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-09#0097` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-09#0098` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0099` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-09#0100` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0101` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-09#0102` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0103` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-09#0104` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-09#0105` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0106` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0107` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-09#0108` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0109` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-09#0110` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0111` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0112` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0113` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-09#0114` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0115` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0116` | `boundary:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0117` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-09#0118` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-09#0119` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-09#0120` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-09#0121` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-09#0122` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-09#0123` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0124` | `boundary:containment:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-09#0125` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-09#0126` | `boundary:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-09#0127` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-09#0128` | `boundary:containment:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-09#0129` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-09#0130` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-09#0131` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-09#0132` | `boundary:containment:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-09#0133` | `boundary:containment:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-09#0134` | `boundary:containment:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-09#0135` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-09#0136` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json` | `PASS` |
| `AC-10.4-09#0137` | `candidate:blob:_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json::_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json` | `PASS` |
| `AC-10.4-09#0138` | `candidate:blob:_bmad-output/planning-artifacts/v9/story-contracts/10.4.json::_bmad-output/planning-artifacts/v9/story-contracts/10.4.json` | `PASS` |
| `AC-10.4-09#0139` | `candidate:blob:_bmad-output/planning-artifacts/v9-authority-bundle-v1.json::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-09#0140` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.json::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-09#0141` | `candidate:blob:docs/release-evidence/story-10.3-final-record-v2.md::docs/release-evidence/story-10.3-final-record-v2.md` | `PASS` |
| `AC-10.4-09#0142` | `candidate:blob:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0143` | `candidate:blob:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0144` | `candidate:blob:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0145` | `candidate:blob:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0146` | `candidate:blob:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0147` | `candidate:blob:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0148` | `candidate:blob:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0149` | `candidate:blob:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0150` | `candidate:blob:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0151` | `candidate:blob:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0152` | `candidate:blob:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0153` | `candidate:blob:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0154` | `candidate:blob:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0155` | `candidate:blob:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-09#0156` | `candidate:blob:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-09#0157` | `candidate:blob:artifacts/v9/10.4/reader-entry-v1.json::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-09#0158` | `candidate:blob:.gitattributes::.gitattributes` | `PASS` |
| `AC-10.4-09#0159` | `candidate:blob:_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md::_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md` | `PASS` |
| `AC-10.4-09#0160` | `candidate:blob:_bmad-output/implementation-artifacts/sprint-status.yaml::_bmad-output/implementation-artifacts/sprint-status.yaml` | `PASS` |
| `AC-10.4-09#0161` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0162` | `candidate:blob:_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json::_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json` | `PASS` |
| `AC-10.4-09#0163` | `candidate:blob:_bmad/schemas/story-final-record-v2.schema.json::_bmad/schemas/story-final-record-v2.schema.json` | `PASS` |
| `AC-10.4-09#0164` | `candidate:blob:_bmad/scripts/evidence_boundary_adoption.py::_bmad/scripts/evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-09#0165` | `candidate:blob:_bmad/scripts/evidence_boundary_current.py::_bmad/scripts/evidence_boundary_current.py` | `PASS` |
| `AC-10.4-09#0166` | `candidate:blob:_bmad/scripts/generate_story_record.py::_bmad/scripts/generate_story_record.py` | `PASS` |
| `AC-10.4-09#0167` | `candidate:blob:_bmad/scripts/tests/test_generate_story_record.py::_bmad/scripts/tests/test_generate_story_record.py` | `PASS` |
| `AC-10.4-09#0168` | `candidate:blob:_bmad/scripts/tests/test_verify_evidence_boundary.py::_bmad/scripts/tests/test_verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-09#0169` | `candidate:blob:_bmad/scripts/tests/test_verify_submodule_promotion.py::_bmad/scripts/tests/test_verify_submodule_promotion.py` | `PASS` |
| `AC-10.4-09#0170` | `candidate:blob:_bmad/scripts/verify_conformance_tiering.py::_bmad/scripts/verify_conformance_tiering.py` | `PASS` |
| `AC-10.4-09#0171` | `candidate:blob:_bmad/scripts/verify_evidence_boundary.py::_bmad/scripts/verify_evidence_boundary.py` | `PASS` |
| `AC-10.4-09#0172` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-09#0173` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0174` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-09#0175` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0176` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-09#0177` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0178` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-09#0179` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-09#0180` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0181` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0182` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-09#0183` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0184` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-09#0185` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0186` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0187` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0188` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-09#0189` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0190` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0191` | `candidate:blob:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0192` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-09#0193` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-09#0194` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-09#0195` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-09#0196` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-09#0197` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-09#0198` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0199` | `candidate:blob:tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-09#0200` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-09#0201` | `candidate:blob:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-09#0202` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Runner/Program.cs::tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` | `PASS` |
| `AC-10.4-09#0203` | `candidate:blob:tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs::tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs` | `PASS` |
| `AC-10.4-09#0204` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs::tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs` | `PASS` |
| `AC-10.4-09#0205` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs::tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` | `PASS` |
| `AC-10.4-09#0206` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/GitFacts.cs::tests/Hexalith.Conversations.TestSupport/GitFacts.cs` | `PASS` |
| `AC-10.4-09#0207` | `candidate:blob:tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs::tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs` | `PASS` |
| `AC-10.4-09#0208` | `candidate:blob:tests/tooling/test_evidence_boundary_adoption.py::tests/tooling/test_evidence_boundary_adoption.py` | `PASS` |
| `AC-10.4-09#0209` | `candidate:blob:tests/tooling/test_evidence_boundary_current.py::tests/tooling/test_evidence_boundary_current.py` | `PASS` |
| `AC-10.4-09#0210` | `readers:approved-correction::artifacts/v9/10.4/reader-entry-v1.json` | `PASS` |
| `AC-10.4-09#0211` | `readers:inventory::_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json` | `PASS` |
| `AC-10.4-09#0212` | `readers:binding::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0213` | `entry:repository::repository` | `PASS` |
| `AC-10.4-09#0214` | `entry:candidate::candidate` | `PASS` |
| `AC-10.4-09#0215` | `entry:baseline::baseline` | `PASS` |
| `AC-10.4-09#0216` | `entry:diff::diff` | `PASS` |
| `AC-10.4-09#0217` | `entry:gitlinks::gitlinks` | `PASS` |
| `AC-10.4-09#0218` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0219` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-09#0220` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0221` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-09#0222` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-09#0223` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-09#0224` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0225` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0226` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-09#0227` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0228` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-09#0229` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0230` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0231` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0232` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-09#0233` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-09#0234` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0235` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0236` | `entry:containment:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0237` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-09#0238` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-09#0239` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-09#0240` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-09#0241` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-09#0242` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-09#0243` | `entry:containment:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0244` | `entry:containment:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-09#0245` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0246` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0247` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0248` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-09#0249` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-09#0250` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` | `PASS` |
| `AC-10.4-09#0251` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0252` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0253` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0254` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-09#0255` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-09#0256` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` | `PASS` |
| `AC-10.4-09#0257` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-09#0258` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-09#0259` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs` | `PASS` |
| `AC-10.4-09#0260` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-09#0261` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-09#0262` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` | `PASS` |
| `AC-10.4-09#0263` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0264` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0265` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0266` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0267` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0268` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0269` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-09#0270` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-09#0271` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` | `PASS` |
| `AC-10.4-09#0272` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0273` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0274` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` | `PASS` |
| `AC-10.4-09#0275` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-09#0276` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-09#0277` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-09#0278` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0279` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0280` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0281` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0282` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0283` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0284` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0285` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0286` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` | `PASS` |
| `AC-10.4-09#0287` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-09#0288` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-09#0289` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs` | `PASS` |
| `AC-10.4-09#0290` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-09#0291` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-09#0292` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` | `PASS` |
| `AC-10.4-09#0293` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0294` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0295` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0296` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0297` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0298` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs` | `PASS` |
| `AC-10.4-09#0299` | `reader:entry:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0300` | `reader:strength:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0301` | `reader:helper:tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs::tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `PASS` |
| `AC-10.4-09#0302` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-09#0303` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-09#0304` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` | `PASS` |
| `AC-10.4-09#0305` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-09#0306` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-09#0307` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` | `PASS` |
| `AC-10.4-09#0308` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-09#0309` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-09#0310` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` | `PASS` |
| `AC-10.4-09#0311` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-09#0312` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-09#0313` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs::tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` | `PASS` |
| `AC-10.4-09#0314` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-09#0315` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-09#0316` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` | `PASS` |
| `AC-10.4-09#0317` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-09#0318` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-09#0319` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` | `PASS` |
| `AC-10.4-09#0320` | `reader:entry:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0321` | `reader:strength:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0322` | `reader:helper:tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs::tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` | `PASS` |
| `AC-10.4-09#0323` | `reader:entry:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-09#0324` | `reader:strength:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-09#0325` | `reader:helper:tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs::tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` | `PASS` |
| `AC-10.4-09#0326` | `reader:project:Conformance.Tests::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |
| `AC-10.4-09#0327` | `reader:project:Conformance.Portable.Tests::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |
| `AC-10.4-09#0328` | `reader:project:Contracts.Tests::tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | `PASS` |
| `AC-10.4-09#0329` | `reader:project:IntegrationTests::tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | `PASS` |
| `AC-10.4-09#0330` | `readers:exemptions::_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json` | `PASS` |
| `AC-10.4-09#0331` | `projection:scope::tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | `PASS` |
| `AC-10.4-09#0332` | `guidance:present:bmad-build::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0333` | `guidance:content:bmad-build::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0334` | `guidance:resolved:bmad-build::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0335` | `guidance:render:.agents/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-09#0336` | `guidance:render-used:.agents/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-09#0337` | `guidance:render:.claude/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-09#0338` | `guidance:render-used:.claude/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-09#0339` | `guidance:present:bmad-build-auto::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0340` | `guidance:content:bmad-build-auto::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0341` | `guidance:resolved:bmad-build-auto::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0342` | `guidance:render:.agents/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-09#0343` | `guidance:render-used:.agents/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-09#0344` | `guidance:render:.claude/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-09#0345` | `guidance:render-used:.claude/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-09#0346` | `guidance:present:bmad-review::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0347` | `guidance:content:bmad-review::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0348` | `guidance:resolved:bmad-review::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0349` | `runbook:sections::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-09#0350` | `runbook:contract::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-09#0351` | `guidance:hash:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0352` | `guidance:hash:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0353` | `guidance:hash:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0354` | `guidance:hash:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-09#0355` | `guidance:hash:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-09#0356` | `workflow:presence:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0357` | `workflow:span:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0358` | ``workflow:clause:.agents/skills/bmad-build/step-04-review.md:`FAIL`/`1`::.agents/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0359` | ``workflow:clause:.agents/skills/bmad-build/step-04-review.md:`BLOCKED`/`2`::.agents/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0360` | ``workflow:clause:.agents/skills/bmad-build/step-04-review.md:`not-applicable`/`0`::.agents/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0361` | `workflow:clause:.agents/skills/bmad-build/step-04-review.md:--verify-workflows::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0362` | `workflow:clause:.agents/skills/bmad-build/step-04-review.md:explicitly requires::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0363` | ``workflow:clause:.agents/skills/bmad-build/step-04-review.md:never write `review` or `done`::.agents/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0364` | `render:deterministic:.agents/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-09#0365` | `render:gate:.agents/skills/bmad-build/step-04-review.md::.agents/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0366` | `workflow:presence:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0367` | `workflow:span:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0368` | ``workflow:clause:.agents/skills/bmad-build/step-05-present.md:`FAIL`/`1`::.agents/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0369` | ``workflow:clause:.agents/skills/bmad-build/step-05-present.md:`BLOCKED`/`2`::.agents/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0370` | ``workflow:clause:.agents/skills/bmad-build/step-05-present.md:`not-applicable`/`0`::.agents/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0371` | `workflow:clause:.agents/skills/bmad-build/step-05-present.md:--verify-workflows::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0372` | `workflow:clause:.agents/skills/bmad-build/step-05-present.md:explicitly requires::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0373` | ``workflow:clause:.agents/skills/bmad-build/step-05-present.md:never write `review` or `done`::.agents/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0374` | `render:gate:.agents/skills/bmad-build/step-05-present.md::.agents/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0375` | `workflow:presence:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0376` | `workflow:span:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0377` | ``workflow:clause:.agents/skills/bmad-build/step-oneshot.md:`FAIL`/`1`::.agents/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0378` | ``workflow:clause:.agents/skills/bmad-build/step-oneshot.md:`BLOCKED`/`2`::.agents/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0379` | ``workflow:clause:.agents/skills/bmad-build/step-oneshot.md:`not-applicable`/`0`::.agents/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0380` | `workflow:clause:.agents/skills/bmad-build/step-oneshot.md:--verify-workflows::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0381` | `workflow:clause:.agents/skills/bmad-build/step-oneshot.md:explicitly requires::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0382` | ``workflow:clause:.agents/skills/bmad-build/step-oneshot.md:never write `review` or `done`::.agents/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0383` | `render:gate:.agents/skills/bmad-build/step-oneshot.md::.agents/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0384` | `workflow:presence:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0385` | `workflow:span:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0386` | ``workflow:clause:.agents/skills/bmad-build-auto/step-04-review.md:`FAIL`/`1`::.agents/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0387` | ``workflow:clause:.agents/skills/bmad-build-auto/step-04-review.md:`BLOCKED`/`2`::.agents/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0388` | ``workflow:clause:.agents/skills/bmad-build-auto/step-04-review.md:`not-applicable`/`0`::.agents/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0389` | `workflow:clause:.agents/skills/bmad-build-auto/step-04-review.md:--verify-workflows::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0390` | `workflow:clause:.agents/skills/bmad-build-auto/step-04-review.md:explicitly requires::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0391` | ``workflow:clause:.agents/skills/bmad-build-auto/step-04-review.md:never write `review` or `done`::.agents/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0392` | `render:deterministic:.agents/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-09#0393` | `render:gate:.agents/skills/bmad-build-auto/step-04-review.md::.agents/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0394` | `workflow:presence:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0395` | `workflow:span:.agents/skills/bmad-code-review/steps/step-04-present.md::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0396` | ``workflow:clause:.agents/skills/bmad-code-review/steps/step-04-present.md:`FAIL`/`1`::.agents/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0397` | ``workflow:clause:.agents/skills/bmad-code-review/steps/step-04-present.md:`BLOCKED`/`2`::.agents/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0398` | ``workflow:clause:.agents/skills/bmad-code-review/steps/step-04-present.md:`not-applicable`/`0`::.agents/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0399` | `workflow:clause:.agents/skills/bmad-code-review/steps/step-04-present.md:--verify-workflows::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0400` | `workflow:clause:.agents/skills/bmad-code-review/steps/step-04-present.md:explicitly requires::.agents/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0401` | ``workflow:clause:.agents/skills/bmad-code-review/steps/step-04-present.md:never write `review` or `done`::.agents/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0402` | `workflow:presence:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0403` | `workflow:span:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0404` | ``workflow:clause:.claude/skills/bmad-build/step-04-review.md:`FAIL`/`1`::.claude/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0405` | ``workflow:clause:.claude/skills/bmad-build/step-04-review.md:`BLOCKED`/`2`::.claude/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0406` | ``workflow:clause:.claude/skills/bmad-build/step-04-review.md:`not-applicable`/`0`::.claude/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0407` | `workflow:clause:.claude/skills/bmad-build/step-04-review.md:--verify-workflows::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0408` | `workflow:clause:.claude/skills/bmad-build/step-04-review.md:explicitly requires::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0409` | ``workflow:clause:.claude/skills/bmad-build/step-04-review.md:never write `review` or `done`::.claude/skills/bmad-build/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0410` | `render:deterministic:.claude/skills:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-09#0411` | `render:gate:.claude/skills/bmad-build/step-04-review.md::.claude/skills/bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0412` | `workflow:presence:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0413` | `workflow:span:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0414` | ``workflow:clause:.claude/skills/bmad-build/step-05-present.md:`FAIL`/`1`::.claude/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0415` | ``workflow:clause:.claude/skills/bmad-build/step-05-present.md:`BLOCKED`/`2`::.claude/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0416` | ``workflow:clause:.claude/skills/bmad-build/step-05-present.md:`not-applicable`/`0`::.claude/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0417` | `workflow:clause:.claude/skills/bmad-build/step-05-present.md:--verify-workflows::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0418` | `workflow:clause:.claude/skills/bmad-build/step-05-present.md:explicitly requires::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0419` | ``workflow:clause:.claude/skills/bmad-build/step-05-present.md:never write `review` or `done`::.claude/skills/bmad-build/step-05-present.md`` | `PASS` |
| `AC-10.4-09#0420` | `render:gate:.claude/skills/bmad-build/step-05-present.md::.claude/skills/bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0421` | `workflow:presence:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0422` | `workflow:span:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0423` | ``workflow:clause:.claude/skills/bmad-build/step-oneshot.md:`FAIL`/`1`::.claude/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0424` | ``workflow:clause:.claude/skills/bmad-build/step-oneshot.md:`BLOCKED`/`2`::.claude/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0425` | ``workflow:clause:.claude/skills/bmad-build/step-oneshot.md:`not-applicable`/`0`::.claude/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0426` | `workflow:clause:.claude/skills/bmad-build/step-oneshot.md:--verify-workflows::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0427` | `workflow:clause:.claude/skills/bmad-build/step-oneshot.md:explicitly requires::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0428` | ``workflow:clause:.claude/skills/bmad-build/step-oneshot.md:never write `review` or `done`::.claude/skills/bmad-build/step-oneshot.md`` | `PASS` |
| `AC-10.4-09#0429` | `render:gate:.claude/skills/bmad-build/step-oneshot.md::.claude/skills/bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0430` | `workflow:presence:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0431` | `workflow:span:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0432` | ``workflow:clause:.claude/skills/bmad-build-auto/step-04-review.md:`FAIL`/`1`::.claude/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0433` | ``workflow:clause:.claude/skills/bmad-build-auto/step-04-review.md:`BLOCKED`/`2`::.claude/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0434` | ``workflow:clause:.claude/skills/bmad-build-auto/step-04-review.md:`not-applicable`/`0`::.claude/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0435` | `workflow:clause:.claude/skills/bmad-build-auto/step-04-review.md:--verify-workflows::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0436` | `workflow:clause:.claude/skills/bmad-build-auto/step-04-review.md:explicitly requires::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0437` | ``workflow:clause:.claude/skills/bmad-build-auto/step-04-review.md:never write `review` or `done`::.claude/skills/bmad-build-auto/step-04-review.md`` | `PASS` |
| `AC-10.4-09#0438` | `render:deterministic:.claude/skills:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-09#0439` | `render:gate:.claude/skills/bmad-build-auto/step-04-review.md::.claude/skills/bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0440` | `workflow:presence:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0441` | `workflow:span:.claude/skills/bmad-code-review/steps/step-04-present.md::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0442` | ``workflow:clause:.claude/skills/bmad-code-review/steps/step-04-present.md:`FAIL`/`1`::.claude/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0443` | ``workflow:clause:.claude/skills/bmad-code-review/steps/step-04-present.md:`BLOCKED`/`2`::.claude/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0444` | ``workflow:clause:.claude/skills/bmad-code-review/steps/step-04-present.md:`not-applicable`/`0`::.claude/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0445` | `workflow:clause:.claude/skills/bmad-code-review/steps/step-04-present.md:--verify-workflows::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0446` | `workflow:clause:.claude/skills/bmad-code-review/steps/step-04-present.md:explicitly requires::.claude/skills/bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0447` | ``workflow:clause:.claude/skills/bmad-code-review/steps/step-04-present.md:never write `review` or `done`::.claude/skills/bmad-code-review/steps/step-04-present.md`` | `PASS` |
| `AC-10.4-09#0448` | `workflow:twins:bmad-build/step-04-review.md::bmad-build/step-04-review.md` | `PASS` |
| `AC-10.4-09#0449` | `workflow:twins:bmad-build/step-05-present.md::bmad-build/step-05-present.md` | `PASS` |
| `AC-10.4-09#0450` | `workflow:twins:bmad-build/step-oneshot.md::bmad-build/step-oneshot.md` | `PASS` |
| `AC-10.4-09#0451` | `workflow:twins:bmad-build-auto/step-04-review.md::bmad-build-auto/step-04-review.md` | `PASS` |
| `AC-10.4-09#0452` | `workflow:twins:bmad-code-review/steps/step-04-present.md::bmad-code-review/steps/step-04-present.md` | `PASS` |
| `AC-10.4-09#0453` | `route:retired:.agents/skills/bmad-dev-story::.agents/skills/bmad-dev-story` | `PASS` |
| `AC-10.4-09#0454` | `route:retired:.agents/skills/bmad-quick-dev::.agents/skills/bmad-quick-dev` | `PASS` |
| `AC-10.4-09#0455` | `route:retired:.claude/skills/bmad-dev-story::.claude/skills/bmad-dev-story` | `PASS` |
| `AC-10.4-09#0456` | `route:retired:.claude/skills/bmad-quick-dev::.claude/skills/bmad-quick-dev` | `PASS` |
| `AC-10.4-09#0457` | `guidance:resolved:bmad-build::bmad-build` | `PASS` |
| `AC-10.4-09#0458` | `guidance:resolved:bmad-build-auto::bmad-build-auto` | `PASS` |
| `AC-10.4-09#0459` | `guidance:resolved:bmad-review::bmad-review` | `PASS` |
| `AC-10.4-09#0460` | `guidance:file:_bmad/custom/bmad-build.toml::_bmad/custom/bmad-build.toml` | `PASS` |
| `AC-10.4-09#0461` | `guidance:file:_bmad/custom/bmad-build-auto.toml::_bmad/custom/bmad-build-auto.toml` | `PASS` |
| `AC-10.4-09#0462` | `guidance:file:_bmad/custom/bmad-review.toml::_bmad/custom/bmad-review.toml` | `PASS` |
| `AC-10.4-09#0463` | `guidance:file:docs/runbooks/current-change-validation.md::docs/runbooks/current-change-validation.md` | `PASS` |
| `AC-10.4-09#0464` | `guidance:file:docs/runbooks/evidence-boundary-validation.md::docs/runbooks/evidence-boundary-validation.md` | `PASS` |
| `AC-10.4-09#0465` | `completion:presence_checks:0::.agents/skills/bmad-build-auto/step-04-review.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0466` | `completion:presence_checks:1::.agents/skills/bmad-build-auto/step-04-review.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0467` | `completion:presence_checks:2::.agents/skills/bmad-build-auto/step-04-review.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0468` | `completion:presence_checks:3::.agents/skills/bmad-build/step-05-present.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0469` | `completion:presence_checks:4::.agents/skills/bmad-build/step-05-present.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0470` | `completion:presence_checks:5::.agents/skills/bmad-build/step-05-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0471` | `completion:presence_checks:6::.agents/skills/bmad-build/step-oneshot.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0472` | `completion:presence_checks:7::.agents/skills/bmad-build/step-oneshot.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0473` | `completion:presence_checks:8::.agents/skills/bmad-build/step-oneshot.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0474` | `completion:presence_checks:9::.agents/skills/bmad-code-review/steps/step-04-present.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0475` | `completion:presence_checks:10::.agents/skills/bmad-code-review/steps/step-04-present.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0476` | `completion:presence_checks:11::.agents/skills/bmad-code-review/steps/step-04-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0477` | `completion:presence_checks:12::.claude/skills/bmad-build-auto/step-04-review.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0478` | `completion:presence_checks:13::.claude/skills/bmad-build-auto/step-04-review.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0479` | `completion:presence_checks:14::.claude/skills/bmad-build-auto/step-04-review.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0480` | `completion:presence_checks:15::.claude/skills/bmad-build/step-05-present.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0481` | `completion:presence_checks:16::.claude/skills/bmad-build/step-05-present.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0482` | `completion:presence_checks:17::.claude/skills/bmad-build/step-05-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0483` | `completion:presence_checks:18::.claude/skills/bmad-build/step-oneshot.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0484` | `completion:presence_checks:19::.claude/skills/bmad-build/step-oneshot.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0485` | `completion:presence_checks:20::.claude/skills/bmad-build/step-oneshot.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0486` | `completion:presence_checks:21::.claude/skills/bmad-code-review/steps/step-04-present.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0487` | `completion:presence_checks:22::.claude/skills/bmad-code-review/steps/step-04-present.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0488` | `completion:presence_checks:23::.claude/skills/bmad-code-review/steps/step-04-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0489` | `completion:presence_checks:24::render:.claude/skills/bmad-build-auto/step-04-review.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0490` | `completion:presence_checks:25::render:.claude/skills/bmad-build-auto/step-04-review.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0491` | `completion:presence_checks:26::render:.claude/skills/bmad-build-auto/step-04-review.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0492` | `completion:presence_checks:27::render:.claude/skills/bmad-build/step-05-present.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0493` | `completion:presence_checks:28::render:.claude/skills/bmad-build/step-05-present.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0494` | `completion:presence_checks:29::render:.claude/skills/bmad-build/step-05-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0495` | `completion:presence_checks:30::render:.claude/skills/bmad-build/step-oneshot.md::completion-gate-block-present` | `PASS` |
| `AC-10.4-09#0496` | `completion:presence_checks:31::render:.claude/skills/bmad-build/step-oneshot.md::generator-invocation-in-block` | `PASS` |
| `AC-10.4-09#0497` | `completion:presence_checks:32::render:.claude/skills/bmad-build/step-oneshot.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-10.4-09#0498` | `completion:parity_checks:0::.agents/skills/bmad-build-auto/step-04-review.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0499` | `completion:parity_checks:1::.agents/skills/bmad-build-auto/step-04-review.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0500` | `completion:parity_checks:2::.agents/skills/bmad-build/step-05-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0501` | `completion:parity_checks:3::.agents/skills/bmad-build/step-05-present.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0502` | `completion:parity_checks:4::.agents/skills/bmad-build/step-oneshot.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0503` | `completion:parity_checks:5::.agents/skills/bmad-build/step-oneshot.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0504` | `completion:parity_checks:6::.agents/skills/bmad-code-review/steps/step-04-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0505` | `completion:parity_checks:7::.agents/skills/bmad-code-review/steps/step-04-present.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0506` | `completion:parity_checks:8::.claude/skills/bmad-build-auto/step-04-review.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0507` | `completion:parity_checks:9::.claude/skills/bmad-build-auto/step-04-review.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0508` | `completion:parity_checks:10::.claude/skills/bmad-build/step-05-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0509` | `completion:parity_checks:11::.claude/skills/bmad-build/step-05-present.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0510` | `completion:parity_checks:12::.claude/skills/bmad-build/step-oneshot.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0511` | `completion:parity_checks:13::.claude/skills/bmad-build/step-oneshot.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0512` | `completion:parity_checks:14::.claude/skills/bmad-code-review/steps/step-04-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0513` | `completion:parity_checks:15::.claude/skills/bmad-code-review/steps/step-04-present.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0514` | `completion:parity_checks:16::render:.claude/skills/bmad-build-auto/step-04-review.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0515` | `completion:parity_checks:17::render:.claude/skills/bmad-build-auto/step-04-review.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0516` | `completion:parity_checks:18::render:.claude/skills/bmad-build/step-05-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0517` | `completion:parity_checks:19::render:.claude/skills/bmad-build/step-05-present.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0518` | `completion:parity_checks:20::render:.claude/skills/bmad-build/step-oneshot.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-10.4-09#0519` | `completion:parity_checks:21::render:.claude/skills/bmad-build/step-oneshot.md::block-states-gate-contract` | `PASS` |
| `AC-10.4-09#0520` | `authority:selection::_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | `PASS` |
| `AC-10.4-09#0521` | `predecessor:10.3::docs/release-evidence/story-10.3-final-record-v2.json` | `PASS` |
| `AC-10.4-09#0522` | `output:contained::artifacts/v9/10.4/AC-10.4-09.json` | `PASS` |

## Approved root-gitlink amendment

- Owner decision: `2026-10-17`
- Approved revision: `9cbc71b63be3fa1ce4d89d98ef1771c54c6603f5`
- The twelve original baseline roots remain immutable provenance; three roots are unchanged.

| Root submodule | Original commit | Approved commit | Original / approved mode |
| --- | --- | --- | --- |
| `references/Hexalith.Builds` | `abdfca89ed82f388b54664825a8a79fed73e7ccf` | `4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77` | `160000 / 160000` |
| `references/Hexalith.EventStore` | `6cc9d6114fd055dac7a1d86130208b52271e3740` | `77d9af18a54d92506e310fe5a45021a395e66f60` | `160000 / 160000` |
| `references/Hexalith.Folders` | `92e87359dd50dc9644e490a04de98adaaade9a86` | `42a2308113e1bdb9b1d07a778b4405dde797ebd3` | `160000 / 160000` |
| `references/Hexalith.FrontComposer` | `0129bd39095892283371f97836ade5768fef3809` | `b102b85b148f21fb26d5742e120c344109df7084` | `160000 / 160000` |
| `references/Hexalith.McpCli` | `0b96dfdf2ff6037be88f1fb952d6581a78930e67` | `b35b0975a234f1f97e2e35e9dad381d0a988f4af` | `160000 / 160000` |
| `references/Hexalith.Memories` | `f7c8ce84a8a765667beafc31154ee2d6981552f8` | `f6b60d5a2e9b81d84fddad789c7d9bed40d3a533` | `160000 / 160000` |
| `references/Hexalith.Parties` | `c0946ef17b3641fb23f6f17614913bfbab687373` | `90af3047b50046cb4f6f91e01f182f562110aab3` | `160000 / 160000` |
| `references/Hexalith.Platform` | `b2890d20575356239555df5b113e3100c1d594c1` | `b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b` | `160000 / 160000` |
| `references/Hexalith.Tenants` | `e44ac7f537e9f192605e24efc3e61a97d00d44f3` | `e667d5391dae306e631dec23acd191863e54130c` | `160000 / 160000` |

## Fault injection

29 observed mutation results are bound to the retained receipts.

| Fault | Expected blocker | Observed blockers | Before SHA-256 | After SHA-256 | Restored |
| --- | --- | --- | --- | --- | --- |
| `CHAIN_CHANGED` | `EVIDENCE_CHAIN_TABLE_INVALID` | `EVIDENCE_CHAIN_TABLE_INVALID` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `True` |
| `CHAIN_MISSING` | `EVIDENCE_CHAIN_TABLE_INVALID` | `EVIDENCE_CHAIN_TABLE_INVALID` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `True` |
| `CHAIN_REORDERED` | `EVIDENCE_CHAIN_TABLE_INVALID` | `EVIDENCE_CHAIN_TABLE_INVALID` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `True` |
| `CHAIN_NESTED` | `EVIDENCE_CHAIN_MARKER_INVALID` | `EVIDENCE_CHAIN_MARKER_INVALID` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `True` |
| `CHAIN_DUPLICATE` | `EVIDENCE_CHAIN_MARKER_INVALID` | `EVIDENCE_CHAIN_MARKER_INVALID` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `True` |
| `CHAIN_UNTERMINATED` | `EVIDENCE_CHAIN_MARKER_INVALID` | `EVIDENCE_CHAIN_MARKER_INVALID` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `True` |
| `CHAIN_DUPLICATE_ROW` | `EVIDENCE_CHAIN_TABLE_INVALID` | `EVIDENCE_CHAIN_TABLE_INVALID` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `6ba0fcfcdb61d2ae0b90e58a373d24bae763942781e70e92fd1fdae70da18784` | `True` |
| `GUIDANCE_REMOVED` | `EVIDENCE_GUIDANCE_NOT_USED` | `EVIDENCE_GUIDANCE_DRIFT, EVIDENCE_GUIDANCE_NOT_USED` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `True` |
| `GUIDANCE_EQUALITY` | `EVIDENCE_GUIDANCE_DRIFT` | `EVIDENCE_GUIDANCE_DRIFT, EVIDENCE_GUIDANCE_NOT_USED` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `True` |
| `GUIDANCE_DECLARED_HASH` | `EVIDENCE_GUIDANCE_DRIFT` | `EVIDENCE_GUIDANCE_DRIFT, EVIDENCE_GUIDANCE_NOT_USED` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `True` |
| `GUIDANCE_REDIRECT` | `EVIDENCE_GUIDANCE_DRIFT` | `EVIDENCE_GUIDANCE_DRIFT, EVIDENCE_GUIDANCE_NOT_USED` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `8a545b70b7f3a973c4f3a9e1178969dd9a6faafcf3d296e60c8b770ce5f17ee9` | `True` |
| `HASH` | `EVIDENCE_HASH_MISMATCH` | `EVIDENCE_HASH_MISMATCH` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `PATH_ESCAPE` | `EVIDENCE_PATH_ESCAPE` | `EVIDENCE_PATH_ESCAPE` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `GENERATED_OUTPUT` | `EVIDENCE_OUTPUT_FORBIDDEN` | `EVIDENCE_OUTPUT_FORBIDDEN` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `SUBSET` | `CHANGED_PATH_SET_MISMATCH` | `CHANGED_PATH_SET_MISMATCH` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `SIGNED_ALLOWLIST` | `SIGNED_EVIDENCE_ALLOWLISTED` | `SIGNED_EVIDENCE_ALLOWLISTED` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `SIGNABLE` | `SIGNABLE_PAYLOAD_MISMATCH` | `SIGNABLE_PAYLOAD_MISMATCH` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `ROOT` | `ROOT_OF_TRUST_NOT_PINNED` | `ROOT_OF_TRUST_NOT_PINNED` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `GIT_UNAVAILABLE` | `GIT_UNAVAILABLE` | `GIT_UNAVAILABLE` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `HISTORY_UNAVAILABLE` | `REVISION_INVALID` | `REVISION_INVALID` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `GITLINK` | `EVIDENCE_BOUNDARY_GITLINK` | `EVIDENCE_BOUNDARY_GITLINK` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `ebf096e3fe15e71fc15a91f4c73da8610fe6f0e1b9d7c436ff74e076f67a129d` | `True` |
| `READER_STRENGTH` | `ASSERTION_STRENGTH_WEAKENED` | `ASSERTION_STRENGTH_WEAKENED` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `True` |
| `READER_HELPER` | `EVIDENCE_HELPER_NOT_USED` | `ASSERTION_STRENGTH_WEAKENED, EVIDENCE_HELPER_NOT_USED` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `True` |
| `EXEMPTION` | `DAY_ONE_EXEMPTION_FORBIDDEN` | `DAY_ONE_EXEMPTION_FORBIDDEN` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `True` |
| `READER_INVENTORY` | `EVIDENCE_READER_INVENTORY_DRIFT` | `EVIDENCE_READER_INVENTORY_DRIFT` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `7aa5c8163240b3a453a11520d796c4ea09526e271d47117bc85c49a12d26a158` | `True` |
| `WORKFLOW_REMOVED` | `EVIDENCE_GATE_NOT_USED` | `EVIDENCE_GATE_NOT_USED, EVIDENCE_WORKFLOW_PARITY_DRIFT` | `d1ba73199d7a173f2d53e26cb05d03d5f50d86891624afda8f9d10567a2a0f6c` | `d1ba73199d7a173f2d53e26cb05d03d5f50d86891624afda8f9d10567a2a0f6c` | `True` |
| `MARKER_MALFORMED` | `EVIDENCE_GATE_NOT_USED` | `EVIDENCE_GATE_NOT_USED, EVIDENCE_WORKFLOW_PARITY_DRIFT` | `d1ba73199d7a173f2d53e26cb05d03d5f50d86891624afda8f9d10567a2a0f6c` | `d1ba73199d7a173f2d53e26cb05d03d5f50d86891624afda8f9d10567a2a0f6c` | `True` |
| `SPAN_DISPLACED` | `EVIDENCE_GATE_DISPLACED` | `EVIDENCE_GATE_DISPLACED, EVIDENCE_WORKFLOW_PARITY_DRIFT` | `d1ba73199d7a173f2d53e26cb05d03d5f50d86891624afda8f9d10567a2a0f6c` | `d1ba73199d7a173f2d53e26cb05d03d5f50d86891624afda8f9d10567a2a0f6c` | `True` |
| `PROMOTION_SPAN_DISPLACED` | `PROMOTION_GATE_SPAN_WEAKENED` | `PROMOTION_GATE_SPAN_WEAKENED` | `958f7fa86ac2bba893dbd2c63a03c42f0aa4e2b3869126d418892ad32071d9ea` | `958f7fa86ac2bba893dbd2c63a03c42f0aa4e2b3869126d418892ad32071d9ea` | `True` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-10.4-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-10.4-final-record-v2.md` |

## Rollback boundary

Remove the Story 10.4 migration, scoped binding/verifier, guidance changes, receipts and final pair together; retain the original inventories and accepted Story 10.3 evidence.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `9` | `9` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
