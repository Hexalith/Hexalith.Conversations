---
title: 'Story 10.4: Adopt the shared evidence boundary'
type: 'refactor'
created: '2026-10-16'
status: 'in-progress'
baseline_commit: '1915a47123737859e3ac31872fa787fa8c0bd1e3'
route: 'dispatch'
review_loop_iteration: 1
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
- [ ] `artifacts/v9/10.4/` and `docs/release-evidence/story-10.4-final-record-v2.{json,md}` — commit sources, measure immutable candidate, publish receipts/pair together, insert verbatim and verify. Later source changes require fresh evidence. No push.

**Acceptance Criteria:**

- Given approved scope, when nine ACs execute, then the verified pair binds migration, predecessor, guidance, faults and span proof with `9/9/0/0/0/0`.
- Given required mutations, when real checkers execute, then exact blockers occur and bytes restore.
- Given migrated sources, when current lanes/guards execute, then they pass with assertions preserved.

## Implementation Notes

- Entry is bound to `1915a47123737859e3ac31872fa787fa8c0bd1e3`; all twelve raw gitlinks and accepted 10.3 pair/receipts remain untouched.
- The independently pinned entry snapshot retains all 27 original blobs and exact enumerated plumbing substitutions. The separately named `approvedAssertionCorrections` binds exactly the owner-approved 10-to-12 correction in the snapshot, effective binding, closed final-record facts and independent consuming source. Original assertion strength material still includes `ShouldBe(10)`; the approved correction is never normalized as plumbing. Twenty-five readers now call shared discovery/read/hash/Git seams; the unchanged collection and live tenant characterization retain meaningful adoption through their serialized collection and migrated blind-spot evidence owner. ContractSamples supplies repository discovery to the three frozen contract readers without changing sample fixtures.
- The effective binding retains original inventory and contract provenance and names all nine ACs. AC-09 uses current resolved-guidance verification. The ten installed workflow bodies and installed defaults remain unchanged.
- Shared chain validation checks exact ordered nonempty rows and single ordered nonnested markers. Fault fixtures cover all ten named boundary faults, signable/root faults, seven chain variants, reader/helper/inventory/exemption faults, four guidance mutations and exact/displaced evidence and promotion spans. The lifecycle adapter retains successor blocker identities.
- The closed 10.4 final-record facts bind eight measured receipts including AC-09; AC-08 is derived last. Publication and insertion verification retain the committed source candidate and reject later source or receipt drift.

### Review loop 1 requirements

- Re-derive the chain seam and complete_fault_matrix chain fixtures from the actual frozen authority amendment chain described by the approved evidence-boundary proposal, with marker family, version and supersedes identities in order. Prove that each block opens after the preceding block closes, each family occurs exactly twice, and the final entry ends the governed document. Mutate that actual frozen material for changed/missing/reordered rows and nested/duplicate/unterminated/duplicate-row faults; check exact blocker codes, nonempty real-helper ledgers and byte-identical restoration. Do not replace or weaken any frozen reader assertion; evaluate the chain additively through the existing adoption/checker path. Keep independent expected chain identities in consuming source, not supplied by mutated evidence.
- Keep every Git operation bounded to the existing 30-second maximum. Give the native Python verifier orchestration a separate explicit bounded budget sufficient for its supported 120-second helper builds; avoid introducing another ad-hoc Git runner. Verify the orchestration budget behavior through the existing test-support seams and preserve cancellation and output bounds.
- Before writing AC-09, require a successful ownership/containment bridge result. Preserve BLOCKED/FAIL codes and exits without writing when facts are unavailable. Protect source and Git administration aliases, including worktree .git files and external gitdirs; rejecting multiply linked output is acceptable. Add real ownership-failure and gitfile-hardlink regression cases, and collect the existing hardlink regression through the normal CI adapter.
- In the real receipt parser require the exact AC-06 and AC-07 testcase sets, including positive exact-span. Independently bind all 29 required fault IDs to their expected exact blocker sets and compare each observed result; validate restoration digests and nonempty ledgers. Parse the complete informational-version source revision exactly. Schema-validate each native helper envelope, require the matching candidate, no blockers, positive evaluated count, and only passing ledger rows; apply consistent PASS semantics to AC-09. Add tamper cases using complete realistic receipts and the real parser, including stale AC-09.
- Anchor ContainedFile and read/hash consumers to the independently located owning repository, never a root inferred from the requested file. Preserve existing signatures when possible and prove sibling-checkout/traversal rejection without changing the frozen 27 reader bytes. Resolve historical mutable revisions once before both tree-mode and blob reads; prove the moving-HEAD race deterministically.
- Add a real-Git insertion-verification negative test that changes one inserted byte and produces RECORD_CONTENT_DRIFT. Preserve the valid positive case. Document a clean committed-source measurement checkout, exact command order, atomic receipts/pair commit, verbatim insertion, guard checks, and subsequent lifecycle commit; never instruct overwriting accepted evidence. Recompute the runbook guidance binding hash.
- Keep all implementation edits within the existing approved source path set; do not edit original authority or inventory files, installed workflow/default files, predecessor evidence or root gitlinks. All changed C# files retain CRLF. Run focused checks first, then commitlint-validated sources, fresh six Debug builds, exact five native receipts, broad native lanes, full current Python CI lane, AC-06/07/09, fresh deterministic pair publication and verified insertion. Prior attempt evidence is not fresh evidence.

## Spec Change Log

- 2026-10-17: User approved the narrow submodule-count amendment after the fresh Debug smoke-method failure confirmed expected 10 versus actual 12 at both baseline and current HEAD. Original entry bytes, inventories, predecessor evidence, and all other assertions remain preserved. Fresh source-bound acceptance measurements and final publication remain required.

- 2026-10-17 review loop 1: B7 exposed synthetic chain proof and B9 exposed the missing orchestration budget. Amend the non-frozen implementation requirements below and rederive those deficient seams in a fresh source checkout, discarding the prior publication only from this new attempt; preserve the reviewed attempt at 81ae0a0. KEEP every frozen entry blob/substitution, the sole approved count correction, all 27 reader assertions and identities, twelve baseline gitlinks, original inventories and accepted 10.3 evidence, deterministic closed pair, source/lifecycle equality, exact workflows and current guidance policy. Carry all individually logged validation and regression fixes into the re-derived implementation.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | high | patch | adoption output ownership: The output writer reads ownership lists without requiring a PASS bridge result; a BLOCKED history result can still write. Require successful ownership before any output write. |
| B2 | high | patch | adoption hardlink aliases: Protected paths omit the worktree gitfile and external Git administration directory. A receipt hardlinked to .git can overwrite repository administration; reject output aliases before writing. |
| B3 | high | patch | fault identity/code binding: The receipt parser trusts its own expected-code property; HASH relabeled with ROOT_OF_TRUST_NOT_PINNED is accepted. Bind each required fault identity independently to its exact blocker set. |
| B4 | high | patch | positive promotion receipt: The parser accepts AC-07 with exact-span removed and its count reduced to one. Require the exact positive and negative testcase identities. |
| B5 | high | patch | native source revision: Unanchored source revision matching accepts a different informational revision containing the candidate later on the line. Compare the complete parsed source revision exactly. |
| B6 | high | patch | native result envelope: A native PASS envelope with a blocker and FAIL ledger row is accepted because this branch skips schema and semantic validation. Require schema-valid PASS, no blockers, and entirely passing evaluated rows. |
| B7 | high | bad_spec | frozen authority chain: The required chain fault fixtures invent three rows and never evaluate the real frozen authority amendment chain. The non-frozen task omitted its markerFamily/version/supersedes relationships and final-document boundary; rederive that seam and fixtures from the frozen chain. |
| B8 | high | patch | contained read authority: ContainedFile chooses its trusted root after normalizing the requested path, so a sibling checkout can become a new root. Anchor reads to the owning repository independently of the requested file and prove sibling-checkout rejection. |
| B9 | medium | bad_spec | orchestration process budget: The native acceptance test runs the entire Python orchestration through a 30-second Git runner, although individual helper builds allow 120 seconds. The non-frozen execution task omitted a separate bounded orchestration budget; retain the 30-second Git limit and support the verifier deadline. |
| B10 | medium | patch | immutable measurement runbook: The listed commands overwrite tracked XML or reject AC-09 when followed in the published checkout. Document a separate immutable source measurement checkout and the ordered publication and insertion sequence. |
| E1 | high | patch | adoption output ownership: Independently confirmed duplicate of B1: unavailable ownership produces empty ownership lists and permits overwriting an existing allowed receipt. Require successful ownership and preserve unavailable result/exit semantics. |
| E2 | high | patch | positive promotion receipt: Independently confirmed duplicate of B4: deleting exact-span and adjusting the JUnit total passes the real parser. Require both exact testcase identities. |
| E3 | medium | patch | historical revision race: ReadRegularBlobAsync resolves a moving revision for the tree and again for the blob; a changed HEAD can return a symlink blob after a regular-mode check. Resolve once and use the immutable commit for both reads. |
| V1 | high | patch | CI hardlink coverage: Preverified: neither normal CI adapter imports the adoption hardlink test. Include that assertion in the normal lane so removing the collision guard turns CI red. |
| V2 | medium | patch | stale AC-09 regression: Preverified: receipt-parser tests do not reject a schema-valid passing AC-09 from another candidate; the lifecycle test replaces the real parser. Add complete-receipt real-parser coverage. |
| V3 | medium | patch | inserted record regression: Preverified: no test invokes the real Story 10.4 insertion route with modified inserted bytes. Add real-Git publication coverage that changes one byte and requires RECORD_CONTENT_DRIFT. |

## Verification

2026-10-17 review-loop rederivation: the new source attempt uses the actual preserved authority document and an independent consuming-source V2–V12/V14 `(markerFamily, version, supersedes)` inventory. The shared helper checks nonnested block order, exact family pairs and the final-document boundary; chain faults mutate those original bytes only in hermetic fixtures and restore them exactly. Native orchestration now shares bounded capture/cancellation with a separate explicit 20-minute maximum while every Git operation retains 30 seconds. The implementation also closes ownership/history writes, Git administration hardlink aliases, exact fault/testcase receipts, complete informational-version parsing, schema-valid native/AC-09 PASS semantics, independently anchored reads and the mutable-revision mode/blob race. Existing helper signatures and all frozen reader bytes remain preserved.

Preliminary rederivation evidence is retained under `/tmp/story104-reviewfix-r0jm5xtg/`: runtime start/wait/describe/stop passed with all eleven resources Running/Healthy; focused chain/output Python regressions passed 13 cases; native `AdoptionSeamTests` passed 12 cases, including a real 31-second orchestration, cancellation/output limits and deterministic moving HEAD; complete-receipt/closed-record parser regressions passed 23 cases; the real-Git atomic-publication/insertion regression passed, then rejected one changed inserted byte with `RECORD_CONTENT_DRIFT`. TestSupport and Conformance Debug builds passed with zero warnings/errors. The first insertion-fixture run correctly refused ignored receipt staging; explicit staging of only its contracted publication paths repaired the fixture. These preliminary checks do not substitute for the fresh committed-source measurements, complete current lane and final deterministic publication required below.

2026-10-17 lifecycle-source regression: the real generator rejected the owned sprint transition with `working source differs: _bmad-output/implementation-artifacts/sprint-status.yaml`, although descendant verification already allowed it. The generator and scoped checker now reuse one predicate that permits only exact source bytes, checked spec lifecycle bytes, or the owned sprint transition equal to the consumer commit. `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k story104_committed_lifecycle_sprint` passed all three real-Git cases: owned transition passes, committed unrelated-row drift fails, and uncommitted date drift fails. Original native and exact AC-01..05 measurements at `b626227` passed; their logs/receipts and the intentionally interrupted current tooling run (500 passed, exit 2) are archived distinctly at `/tmp/story104-b626227-measurements/`. Fresh builds, lanes and acceptance receipts are required at the final source commit. Publication and insertion remain local to `fix/story-10.4-evidence-boundary` in this isolated clone, preserving concurrent external main/gitlink updates.

2026-10-17 amendment checks: `python3 -m pytest -q tests/tooling/test_evidence_boundary_adoption.py _bmad/scripts/tests/test_generate_story_record.py -k 'approved_assertion_correction or story104'` passed 15 cases. The explicit correction is independently constrained; unapproved count changes, another changed assertion, correction-metadata drift and closed-record drift turn red. Fresh committed-candidate measurements use an isolated local shared clone at `/tmp/story104-approved-laacf8pj/checkout`, with twelve root-declared dependency clones checked out at the superproject's exact pinned commits. Concurrent user dependency checkout changes remain untouched. Isolation commands and source-bound results are retained under `/tmp/story104-approved-*`; no network or nested submodule initialization is used.

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
