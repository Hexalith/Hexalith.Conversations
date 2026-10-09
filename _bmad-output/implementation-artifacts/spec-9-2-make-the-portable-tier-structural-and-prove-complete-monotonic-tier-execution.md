---
title: 'Story 9.2 portable conformance tiers'
type: 'feature'
created: '2026-10-07'
status: 'in-progress'
baseline_commit: '51aa06b856bcb0aaf22013153cfe02a51b046156'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-9-context.md'
  - '_bmad-output/planning-artifacts/v9/story-contracts/9.2.json'
  - '_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-authorization-v1.json'
  - '_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-proposal-v1.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Portable assertions compile with Server; consumers lack a portable oracle and two-tier execution proof.

**Approach:** Split compilation under Story 9.1, prove dependencies/assertion preservation, run both tiers, and derive the final record.

## Boundaries & Constraints

**Always:** Preserve identities, tiers, strength, FR-20 membership, and telemetry/status suites. Bind Story 9.1. Separate frozen definitions, executed cases, and controls. Require nonzero passing execution without skips/omissions. Departures need versioned evidence and genuine Quality approval.

**Decisions (2026-10-07, user: "do recommended"):** Preserve the existing historical exclusions and all frozen definitions; compare active identities/cases against the 415-case floor, counting controls separately. Prepare additive current-surface evidence, explicit governance expectations, safe temporary snapshot output, and combined suite checks. Retain old strength hashes; derive and review successor hashes. Approach approval does not approve unseen public drift or final row digests.

**Never:** Change production APIs, dependencies, submodules, v1 bytes, accepted records; waive coverage, weaken assertions, or invent approvals. Roll back the split together, retaining Story 9.1.

## I/O & Edge-Case Matrix

| Input / State | Required behavior | Blocker |
|---|---|---|
| Nonportable evaluated dependency | Reject resolved compile surface | `PORTABLE_TIER_NONPORTABLE_REFERENCE` |
| Missing/renamed/duplicated or weakened assertion | Reject migration | `ASSERTION_INVENTORY_DRIFT`, `ASSERTION_STRENGTH_WEAKENED` |
| Missing declaration, skipped/empty/regressed execution | Reject completion | `TIER_NOT_DECLARED`, `ASSERTION_LEDGER_EMPTY`, `EXECUTED_COUNT_REGRESSION` |

</frozen-after-approval>

## Code Map

- `docs/release-evidence/conformance-oracle-tiering-disposition-v2.json` — 452 frozen methods: 376 portable/76 internal; seven portable validation additions. No recorded source/closure mixes tiers.
- `artifacts/v9/9.1/pre-split/` — verified hashes: 401 executed methods, 415 cases, 412 passes, three failures, 51 exclusions.
- `tests/Hexalith.Conversations.Conformance.Tests/` — link unchanged paths; retain collections. Seven historical controls inspect only their own assembly; add separate live controls.
- `_bmad/scripts/generate_conformance_tiering.py` — reuse `ProjectModel` strength derivation; evaluate both compilation sets.
- `_bmad/scripts/generate_story_record.py` — reuse successor parsing; `.slnx` supplies completion inventory.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/v9/story-9.2-execution-amendment-v1.json` — bind decisions/effective scenarios; preserve frozen contract.
- [x] `tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` and existing conformance `.csproj` — link approved files, remove duplicate compilation; preserve namespaces/helpers and permitted dependencies.
- [x] `Hexalith.Conversations.slnx`, `.github/workflows/ci.yml` — declare/run both tiers, exclusions, and controls.
- [x] `tests/Hexalith.Conversations.Conformance.Portable.Tests/PortableCompileSurfaceValidationTest.cs` — implement AC02 against evaluated packability, transitive assets, and `ReferencePath`.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/Story92/ConformanceOracleTieringValidationTest.cs` — implement exact AC04/05 selectors across both assemblies.
- [x] `_bmad/scripts/verify_conformance_tiering.py` — verify identities/strengths, approvals, declarations, and execution.
- [x] `docs/release-evidence/conformance-oracle-tiering-migration-v3.json`; existing conformance `GovernanceAuditPairingSafetyNetConformanceTest.cs`, `PublicContractShapeSnapshotGenerationTest.cs`, `ReleaseBaselineValidationTest.cs` — implement authorized successors/safe snapshot output with retained identities and before/after proof.
- [x] `_bmad/scripts/tests/test_conformance_tiering.py` — add `structural_and_execution_faults`: nonportable reference, assertion deletion/duplication/rename/weakening, missing tier/project/declaration, skipped/not-run/empty execution, regression, and exact restoration.
- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — bind tier hashes, predecessor, inventories/strengths, approvals, faults, and retention.
- [x] `_bmad-output/planning-artifacts/v9/story-9.2-candidate-environment-amendment-v1.json`, `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json` — implement the authorized Story 9.2-only candidate-environment binding, preserving the original baseline and rejecting every unrelated protected-path change.
- [x] `_bmad/scripts/tests/test_generate_story_record.py` — verify the exact authorized seven-gitlink environment and owning promotion chain; reject missing/altered bindings and any additional promotion; restore every mutation byte-identically and preserve historical record compatibility.
- [x] `docs/runbooks/story-final-record-generation.md`, `docs/release-evidence/story-9.2-final-record-v2.{json,md}` — document/generate/verify record and insertion; update spec/sprint status after passing gates.

**Acceptance Criteria:**
- Given approved scenarios, when AC01–05 run, then builds/dependencies, exact declarations, and migration proof pass.
- Given tier results, when AC06–09 run, then complete passing execution preserves the floor, distinguishes additions, and detects/restores every fault.
- Given compatible passing evidence, when AC10 runs, then the derived record binds all required facts and reports `10/10/0/0/0/0`.

## Implementation Notes

- 2026-10-07: User approved both recommended approaches with "do recommended". Implement all reversible work and prepare concrete digest-bound approval evidence; do not invent the later Quality-owner decision. Preserve the original baseline commit.
- Structural implementation verified: both Debug/Release builds and AC02/05 pass; portable 326/326, internal 91/92 with only the approval control failing. All 415 retained cases pass; 21 fault fixtures and 11 source-model checks pass. Final acceptance remains pending.
- Resolve the remaining verifier concern: use resolved assembly identity, rather than only DLL filename, to reject a renamed nonportable reference. Keep the final-record integration specific to the hash-bound execution amendment; preserve historical parsing, schemas, and accepted records. Derive current facts through the verifier APIs and bind all measured fault receipts to the candidate. Genuine approval and committed-candidate final publication remain pending; prepare/test all tooling without fabricating either.
- Resolved assembly identity now uses MSBuild `FusionName`; the expanded 22-fault suite passes. Final-record integration and its 62 focused generator checks pass, including candidate/approval rejection, historical-pair compatibility, deterministic fixture derivation, and inserted-record drift. The publication/retention runbook is updated.
- Concrete decision packet: `docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md`. Proposal `f59dce5e7642c7ef588dcb0f3e4f7fe045598ee613c9638b3198a5e57f331eef` binds 14 changed rows and public drift digest `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1`. No Quality decision, final record, or completion status has been invented.

- Authorized amendment implementation: hash-pin and validate the closed environment document; verify its embedded proposal digest, original baseline, measured environment candidate ancestry, exact mode-160000 before/after gitlinks, and every owning promotion commit/parent/diff against Git. Accept only the approved seven existing promotions; continue rejecting any additional promotion or unrelated source/dependency/protected-record change. Bind the amendment digest, original environment candidate, proposal digest, and measured gitlinks/promotion chain in the Story 9.2-only final-record `conformanceExecution` shape. Preserve all other story parsing/schema behavior and add candidate-bound positive/negative coverage with exact restoration. Do not regenerate the approved migration or alter the execution amendment, assertions, production, dependencies, or submodules. Update the runbook and review packet to describe the actual authorization. The implementation handoff prepares/tests these changes and reports back; the root agent handles the required candidate commit, fresh acceptance, final-pair publication, and lifecycle after inspecting the implementation.

## Spec Change Log

- 2026-10-07: User requested "update to latest eventstore", authorizing the shared EventStore package update and its owning Builds changes despite the original dependency/submodule constraint. The original baseline and frozen intent remain preserved. This instruction supplies neither the Quality migration decision nor an amendment accepting the other candidate gitlink promotions.

- 2026-10-07: User replied "I authorize and approve" to the concrete current-candidate scope and independent Quality approval requests. This authorizes exactly candidate-environment proposal `a5c12ba32a82338ce8bcb791f8b64355e5a820d7433145e0648d107441bc3d20` and approves migration proposal `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`, all 14 reviewed successor row digests, and public drift `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1`. The actual conversation user is recorded as approver `user`; no personal name is invented. The [authorized environment amendment](../planning-artifacts/v9/story-9.2-candidate-environment-amendment-v1.json) embeds the reviewed proposal unchanged and the [Quality approval](../../docs/release-evidence/conformance-oracle-tiering-migration-approval-v3.json) binds its exact rows. Original frozen intent, baseline, accepted records, and all other gates remain preserved.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| Blind 1 — historical freeze tests in repository CI | false | reject | Directory collection honors `conftest.py`'s `collect_ignore`; the exact repository selection collects 860 tests and zero cases from `test_conformance_tiering.py`. Naming that retired file explicitly reproduces history rather than the configured directory lane (`artifacts/v9/9.2/review/directory-collection.log`). |
| Blind 2 — fault fixture lacks repository-job prerequisites | false | reject | The same directory exclusion prevents that fixture from running in the Python-only repository job. The conformance job initializes root dependencies and builds both Release projects before explicitly selecting the structural/execution faults. |
| Blind 3 — portable full-suite execution needs internal metadata | false | reject | The approved approach explicitly requires combined fourteen-suite checks. `ConformanceTierAssemblyInventory` reads PE metadata without loading internal runtime types; AC02's portable compile-surface control runs independently, and AC06 follows the required AC03 build. Running the combined baseline assertion without the second required binary correctly fails. |
| Blind 4 — ordinary verifier accepts synthetic approval markers | medium | patch | `approved_migration` checks nonempty identity strings but accepts the fixture markers; only final-record facts reject them. Reject those explicit markers on the ordinary path and make the disposable fixture's override explicit in test code. |
| Blind 5 — ordinary complete verifier accepts marker-only DLLs | medium | patch | `execution` checks a binary's existence/time and TRX codeBase but does not reject the literal synthetic marker file. Require real managed execution metadata on the ordinary path; final-record candidate stamping remains authoritative. |
| Blind 6 — resolved foreign nonpackable module references | medium | patch | `permitted` restricts only `Hexalith.Conversations`, while the project walk recognizes `Hexalith.` ownership. A nonpackable Hexalith binary outside the resolved shipped project/package surfaces can pass the surface-only control; apply consistent ownership checks. |
| Blind 7 — copied centralized catalog absent from source digest | low | reject | The catalog is omitted from the root-file digest, but the final record independently binds its owning Builds gitlink and the full promotion history, while dirty submodule state is rejected by candidate cleanliness. No acceptance bypass is shown; an additional submodule catalog receipt adds another binding mechanism for an uncommon fixture-only concern. |
| Blind 8 — orphan TRX results accepted | medium | patch | Result names/counters are verified without joining each result to its `testId` definition; the disposable fixture proves that one definition can accompany hundreds of result rows. Require valid tier/method definitions for every result without changing historical TRX parsing. |
| Blind 9 — output overwrites verification inputs | medium | patch | `write_json` protects containment but permits a retained artifact or tier TRX as destination. Reject collisions, including file aliases, before writing and preserve input bytes on failure. |
| Blind 10 — evaluated generated assertions omitted | medium | patch | `source_inventory` drops all `bin`/`obj` Compile items, so a custom compiled test there escapes the exact inventory. Exempt only known compiler-generated metadata and reject other generated Compile inputs. |
| Edge 1 — renamed nonpackable project accepted | medium | patch | The project walk exempts an `IsPackable=false` dependency whose AssemblyName lacks `Hexalith.`. Every traversed repository project is already known to be first-party; enforce its packability regardless of name. |
| Edge 2 — declaration check accepts echo instead of tier execution | medium | patch | `declarations` checks project/DLL substrings but does not require the actual invocation, so replacing the internal `dotnet` execution with `echo` retains PASS. Check the executable tier commands in the existing CI run block. |
| Edge 3 — output aliases retained evidence | medium | patch | The cited write path has no protected-input collision guard and can overwrite immutable v1 inputs. This shares Blind 9's root cause and will receive the same correction after individual classification. |
| Verification gap 1 — final-record caller's result paths mocked away | medium | patch | Trust the filed regression evidence: both current facts/pipeline tests mock the relevant call, and a nonexistent internal-result argument still passes. Add coverage through the actual verifier, including missing internal evidence, without introducing .NET/submodule prerequisites into the Python-only repository lane. |
| Verification gap 2 — fault fixture breaks fresh repository CI | false | reject | The reproduction explicitly names the retired file, bypassing `collect_ignore`; the configured repository directory lane collects none of it. The prepared conformance lane supplies the prerequisites, as confirmed by the passing candidate AC09 receipt. |
| Root follow-up — source replacement after record publication | medium | patch | The new full-history touched-path check also sees the valid record-only publication/retraction commits and would reject a documented replacement candidate. Recognize only independently verified record-only pair history; retain rejection of mixed pair/source commits, protected records, and extra promotions. |
| Root follow-up 2 — modified pair changes its retained candidate | medium | patch | A disposable Git probe publishes a pair, changes an allowed source path without retraction, then republishes a valid pair naming the new source candidate; the environment helper accepts it (`artifacts/v9/9.2/review/pair-replacement-probe.json`). Validate the parent pair before M/M refresh and require the same retained candidate; replacement must follow D/D retraction then A/A publication. |

All patch entries above are resolved in the replacement implementation. The current Python CI lane collects the hermetic regressions; the exact 22-case AC09 selector remains unchanged. Final focused generator/verifier checks pass 143/143, source-model checks pass 11/11, the approved migration reproduces unchanged, and the pre-candidate stable fault run passes 22/22. The intermediate source-digest failure remains separately retained; source bytes were frozen for its successful rerun. The first passing candidate and pair are archived under `artifacts/v9/9.2/retained/aa4595d52475ac2b00c54ecb4bf9de806a0450e4/`; publication commit `572328be4ccf783ece4381c744a256ed8898a00a` was retracted by pair-only commit `9c6ff6d3f5c1a3c5d1754c258cb5832b514ac495`. Fresh committed-candidate AC01–10, deterministic publication, insertion verification, and lifecycle completion remain required.

### Original accepted candidate review — 2026-10-08

The user selected review of original accepted candidate `c44f6b7b116b17453aa213d39cd244a3e0cbe1dd`, against the original baseline, using completion claims from `e5b0a1ea2e76452e8a5a73e672ed33893172bfce`. Three independent reviewers returned the findings below. Each receives its own verdict before grouping. Source locations and observations refer to that isolated candidate; concurrent main edits are outside this review. The [review report](../../artifacts/v9/9.2/resume-2026-10-08/original-candidate-review.md) contains grouped corrections and receipts.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| Resume Blind 1 — unrelated managed test binaries | medium | bad_spec | Original `verify_conformance_tiering.py:616` accepts retained Contracts.dll copied into both tier DLL paths, with genuine TRX codeBase paths rebased, as complete passing execution of 415 cases. Both substituted DLLs already contain the accepted candidate revision; generic managed metadata and final revision stamping do not establish tier/test identity. |
| Resume Blind 2 — arbitrary bytes supply source revision | medium | defer | Original `generate_story_record.py:7227` consumes the pre-existing raw-byte `dotnet_source_revisions` helper at line 1229. Appending revision-shaped bytes to Microsoft.TestPlatform.Utilities.dll supplies the sole candidate revision while the managed validator passes; the identical regex exists at the baseline. No complete generated acceptance bypass was run. |
| Resume Blind 3 — recorded execution dates ignored | low | reject | Original `verify_conformance_tiering.py:614` accepts old or missing XML timestamps after rewriting otherwise genuine evidence. The probe establishes a weak mtime-only check, not acceptance of an authentic different-candidate run by the final generator; a new run-time digest/timestamp protocol adds complexity for an uncommon use. |
| Resume Blind 4 — failed run summary accepted | medium | bad_spec | Original `verify_conformance_tiering.py:600` returns complete passing execution after ResultSummary outcome becomes Failed while counters/results stay passing. The new complete-execution caller does not enforce a successful consistent run-level outcome. |
| Resume Blind 5 — failed nested result ignored | medium | bad_spec | Original `verify_conformance_tiering.py:621` ignores a failed InnerResults/UnitTestResult under a passing outer result. The Story 9.2 path must reject unsupported nested shape or validate its outcomes without double-counting the frozen ledger. |
| Resume Blind 6 — disabled conformance job accepted | medium | bad_spec | Original `verify_conformance_tiering.py:266` accepts declarations after adding `if: false` to the conformance job. Required run text remains present although the job is skipped; declaration proof omits execution conditions. |
| Resume Blind 7 — nonblocking tier step accepted | low | reject | Original `verify_conformance_tiering.py:266` ignores continue-on-error on tier execution. The subsequent combined verifier remains blocking and rejects failed/missing tier results; the reviewer confirms no overall green failed CI run was demonstrated. An additional guard has negligible practical effect in this reproduced configuration. |
| Resume Blind 8 — verifier and fault commands printed | medium | patch | Original `verify_conformance_tiering.py:292` accepts both required verifier/fault commands prefixed with echo. Their mere substrings satisfy declaration proof; apply executable-command validation already used for tier invocations. |
| Resume Blind 9 — Bash run body under Python shell | medium | patch | Original `verify_conformance_tiering.py:266` still accepts declaration proof when the tier step shell becomes Python. Require the Bash shell that interprets the existing mandatory run body. |
| Resume Blind 10 — root configuration output collision | medium | patch | Original `verify_conformance_tiering.py:697` overwrites pyproject.toml, uv.lock, AGENTS.md, .gitignore, .gitattributes, and package.json in scratch repositories. Protect these demonstrated root destinations while retaining evidence/alias guards; this differs from the earlier protected-evidence collision. |
| Resume Blind 11 — omitted Builds catalog digest | low | reject | carried from Blind 7: same omitted-catalog claim/location and unchanged binding code, so verdict/route are retained. The earlier dirty-submodule rejection rationale is not established by the root `--ignore-submodules=all` worktree check; committed gitlink binding does not establish consumed dirty bytes. No new end-to-end catalog-input acceptance bypass is claimed. |
| Resume Edge 1 — unrecognized direct ReferencePath | medium | bad_spec | Original `verify_conformance_tiering.py:199` accepts an extra modeled resolved assembly named RenamedInternal outside the approved project/package graph because non-Hexalith names are exempt. This is a direct binary reference, distinct from the previously fixed nonpackable ProjectReference; the probe is modeled MSBuild input, not complete publication. |
| Resume Edge 2 — unrelated managed DLL under tier filename | medium | bad_spec | Original `verify_conformance_tiering.py:616` accepts the same retained Contracts.dll substitution described by Resume Blind 1. Independently classified before grouping; assembly/test identity is not tied to the expected tier. |
| Resume Edge 3 — output replaces root build configuration | medium | patch | Original `verify_conformance_tiering.py:697` accepts .gitignore, .gitattributes, pyproject.toml, and uv.lock as output destinations and loses their original bytes. Independently classified before grouping with Resume Blind 10. |
| Resume Edge 4 — unknown API mode silently weakens checks | medium | patch | An actual original `verify(root, mode='compelte')` call at line 655 returns PASS without result inputs or execution output. Reject unsupported modes before dispatch; valid CLI modes already remain explicit. |
| Resume Verification gap 1 — managed-check regression survives tests | medium | patch | Pre-verified layer evidence is accepted as filed: an MZ-only validator mutation leaves all 116 focused Story 9.2 checks passing, yet invalid MZ bytes become complete passing execution. The existing caller-level negative uses non-MZ markers while richer fixtures mock metadata validation; add unmocked invalid-MZ and PE-without-CLI cases. |

Grouping yields five bad_spec entries (assembly identity, run summary, nested outcomes, disabled CI, and unrecognized resolved dependencies), five patch entries (printed commands, shell, root output collisions, unknown mode, and managed-check regression coverage), and one pre-existing deferred entry (raw source-revision parsing). Three low findings are rejected, including the carried catalog row. The selected historical review is complete; fixes remain unresolved. The user's review selection preserves the original accepted candidate and concurrent main work, so the workflow's source-reversion/re-derivation loop is not performed in this run. No loopback iteration, commit, pair retraction, replacement record, or new acceptance is claimed. The spec and sprint row remain `in-progress`.

### Authorized verifier patch follow-up — 2026-10-08

The five patch groups from the original accepted-candidate review are now resolved in the working tree. The measured committed boundary is `3c5699761c4944b6d68ddf585b26bb720f8792ca`; this run started at `1b97145c2c4581eb635e23c5dfbad09324bf357a` before an external gitlink promotion advanced HEAD. Changes are limited to `_bmad/scripts/verify_conformance_tiering.py`, its hermetic regressions in `_bmad/scripts/tests/test_generate_story_record.py`, explicit disposable-fixture compatibility in `_bmad/scripts/tests/test_conformance_tiering.py`, and this non-frozen status note. No frozen definition, assertion strength, approval, accepted pair, dependency, production source, or submodule has been changed by this follow-up.

| Review patch group | Implemented correction | Verification |
| --- | --- | --- |
| Resume Blind 8 — printed verifier/fault commands | Require exact executable verifier and fault commands, their actual inputs, selection, and result paths. | Hermetic echo/comment/missing-command/wrong-input mutations fail with `TIER_NOT_DECLARED`; each restores the workflow bytes and reruns the valid declaration check. |
| Resume Blind 9 — Bash body under Python shell | Require the existing tier run step's explicit Bash shell. | Python or absent shell fails with `TIER_NOT_DECLARED`; restored Bash passes. |
| Resume Blind 10 / Resume Edge 3 — root output collisions | Protect root agent instructions, build/dependency configuration, lockfiles, and Git configuration destinations, retaining evidence and alias guards. | All protected-path and hardlink/symlink cases reject with `OUTPUT_PATH_INVALID` while preserving the original bytes. |
| Resume Edge 4 — unknown API mode | Reject every unsupported mode before dispatch. | Misspelled, empty, case-altered, and unknown modes return `TIERING_INPUT_INVALID` without invoking an adapter. |
| Resume Verification gap 1 — weak managed-check regression coverage | Exercise malformed MZ and native PE-without-CLI bytes through the real unmocked execution validator. | All four malformed-binary cases reject with `TIER_EXECUTION_INCOMPLETE`; the separate tier-binary identity correction is recorded below. |

The [focused result](../../artifacts/v9/9.2/resume-2026-10-08/authorized-patches/generator-tests-corrected.xml) passes 176/176 with zero failures, errors, or skips. The [configured repository directory lane](../../artifacts/v9/9.2/resume-2026-10-08/authorized-patches/repository-lane.xml) passes 1014/1014 in 665.62 seconds, with zero failures, errors, or skips and 56 existing JUnit `record_property` compatibility warnings. Current-tree declarations, Python compilation, and whitespace checks pass. The initial focused run's one fixture `None`-handling failure is corrected; its failed XML remains separately retained. [Consolidated command receipts](../../artifacts/v9/9.2/resume-2026-10-08/authorized-patches/verification-receipts.json) bind the measured code and results.

The five groups historically classified `bad_spec` are implementation corrections to the existing frozen requirements for exact tier identity, complete passing execution, active declarations, and resolved dependencies. The user's Story 9.2 build request authorizes these reversible corrections; no frozen-intent renegotiation is required. The historical review observations and classifications above remain recorded, while the current implementation route is resolved by these additional tasks outside the frozen intent:

- [x] Derive managed Assembly, TypeDef, and MethodDef identities from contained CLI metadata; require the exact tier assembly and all 452 preserved compiled definitions plus three controls, including the 51 historical exclusions. Add unmocked wrong-assembly, swapped-tier, missing-active/historical-method/control, malformed-table, and appended-marker regressions. Keep the expected executed case identities unchanged.
- [x] Require exactly one consistent `Completed` run summary and one counter row. Reject failed summaries beside passing cases and unknown/missing/duplicate summaries. Retain truthful observation of consistent named control failures with `completePassingExecution=false`; ordinary acceptance still fails.
- [x] Reject nested or orphan TRX result shapes without changing the frozen flat case ledger. Exercise failed, passed, skipped, empty, and unexpected-wrapper nested mutations.
- [x] Require the existing main push/pull-request triggers and unconditional conformance job/build/tier/verifier/fault steps. Reject disabled conditions, spaced or quoted condition keys, YAML merges, dependency gates, duplicate jobs/steps/keys, failure masking, workflow shell overrides, manual-only workflows, and early exits. Preserve the exact ordered tier body without adding a YAML dependency.
- [x] Join every resolved reference's identity and physical path to evaluated packable project outputs, reached restored compile assets, or declared/evaluated framework targeting packs, regardless of assembly prefix. Exercise unrelated, substituted, unreached, and falsely labelled references.

The final [focused guard result](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/generator-tests-all-definitions-corrected.xml) passes 234/234 in 36.57 seconds with no failures, errors, or skips. The [combined fault/current-tree result](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/faults-and-current-tree-final.xml) passes 33/33 in 332.39 seconds: the exact original 22 fault cases plus 11 current-tree regressions. Fault mappings, selected test function/decorator, expected blockers, and byte-identical restoration remain unchanged. [Actual Portable surface evaluation](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/actual-portable-surface-final.json) and [current-tree declarations](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/current-tree-declarations-all-definitions.json) pass. [Read-only real DLL inventory](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/real-tier-inventory-final.json) finds all 376 Portable frozen definitions plus its control and all 76 internal definitions plus its two controls. Available assemblies retain their original bytes and mtimes; these are compatibility positives, not fresh execution acceptance.

The first guard run's malformed-table fixture passed a mutable bytearray into a bytes-only helper; its later historical-method fixture used an incorrect lane label. Both disposable fixtures are corrected and failed XML remains retained. The original AC09 clone setup lacked newly reached dependency projects; it now copies the evaluated root-declared projects and ordinary ancestor build configuration without submodule initialization/updates, package changes, or rebuilding assemblies. The superseded directory lane was stopped cleanly with SIGINT, exit `2`, after 636 passing cases in 431.97 seconds; its [interrupted XML](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/repository-lane-interrupted.xml) is retained and supplies no complete PASS. The final [configured directory lane](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/repository-lane-final.xml) passes 1072/1072 in 472.62 seconds, with zero failures, errors, or skips and 56 existing JUnit compatibility warnings. Code remained stable throughout the completed run. [Final verification receipts](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/verification-receipts-final.json) bind the exact final code, documentation, outputs, preserved artifacts, and separate failed/interrupted evidence.

The pre-existing raw source-revision parser remains deferred. The [concrete follow-up proposal](../../artifacts/v9/9.2/resume-2026-10-08/authorized-patches/follow-up-scope-proposal.md) records Decision A as implemented existing-requirement corrections. Decision B is an optional current-main successor route; it is not required to apply or verify these corrections and does not replace the user's historical review selection. An isolated successor from the original accepted source with only authorized corrections could preserve the original source/environment scope, subject to a clean committed candidate and exact evidence/approval rederivation. No such isolated successor, branch, checkout, commit, or acceptance was created in this run.

The [refreshed machine audit](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/follow-up-scope-proposal.json), SHA-256 `b99e34c00761c2070d47e4bc750ede30e673900ca3d56479d54f407e4b87a613`, measures 93 committed changed paths since the original accepted source candidate, including 17 production paths and nine root gitlinks across ten owning promotion commits. All nine protected contract/amendment/migration/approval/predecessor/final-pair artifacts match the original publication bytes. The final verifier/test hashes are bound separately from five excluded concurrent external source/configuration/test paths, including `eng/verify-ext-conv-ai-1.ps1`. The audit's documentation hash precedes this final status-note append. The earlier first-patch audit, SHA-256 `58f0294ed8b282871fcd66c9ba87906cf6202a39a350921b6bda7a16b4e06485`, remains retained separately.

Historical structure verification still exits `1` with `PUBLIC_CONTRACT_WIDENED` for later production source bytes; the actual current-candidate environment API still rejects the changed environment with `AUTHORITY_BINDING_INVALID`. These measurements supply no new exported-API or acceptance claim. The local Aspire baseline attempt ran after the Python edits and exits `2`: the existing deletion-publication delivery/projector/pump code has seven `CS0246` errors for missing publication interfaces/result types. No AppHost started; [startup](../../artifacts/v9/9.2/resume-2026-10-08/authorized-patches/aspire-run.log) and [state](../../artifacts/v9/9.2/resume-2026-10-08/authorized-patches/aspire-describe.log) receipts retain the exact check.

The optional current-main proposal distinguishes source/environment scope authorization from a later genuine Quality decision on exact changed successor proposal, row, and public-drift digests. An original-source successor must independently reproduce every existing approval binding; changed bindings also require a genuine decision. No unseen migration evidence has been approved, and no new committed candidate, pair publication/retraction, AC01–10 acceptance, or completion transition has been produced. The frozen intent, original baseline, original accepted evidence and embedded final record remain preserved; the spec and sprint lifecycle stay `in-progress`.

## Verification

Run effective Story 9.2 scenarios with receipts. Use individual Debug checks and separate Release acceptance. Rebuilds invalidate results. Verify determinism, insertion, frozen hashes, focused Python tooling, and `git diff --check`; unresolved questions/failures block completion.

Working-tree verification (2026-10-07): both Debug/Release builds pass with zero warnings/errors; AC02/05 pass; AC06 passes 326/326; AC07 reports 91/92 with only `TIER_APPROVAL_MISSING`; all 415 retained cases pass without skips or omissions. AC04/08 remain blocked by the actual Quality decision. AC09 passes 22/22; focused Story 9.1/9.2 generator tests pass 62/62; source-model checks pass 11/11; whitespace checks pass. Receipts are under `artifacts/v9/9.2/`. Final AC10 and lifecycle updates await approval and a fresh committed-candidate run.

Matrix audit: the four dependency fault variants cover nonportable references; deletion, duplication, rename, and weakening cover identity/strength failures; declaration removal, empty execution, skipped/not-run cases, and execution regression cover the final matrix row. Every covering fault executed, reported its exact blocker, and restored byte-identically in AC09.

Resumed verification (2026-10-07, current candidate `f962c5b1f81b995116c83f144c26707258319ba4`): the following current measurements supersede the earlier provisional build/execution status. Both individual Debug builds pass with zero warnings/errors; Debug AC02/05 pass. After separate Release restores, portable AC01/02 pass and AC06 passes 326/326 with no skips. The source verifier's `--declarations-only` check passes. AC09 passes 22/22 with exact blockers and byte-identical restoration; focused Story 9.1/9.2 generator checks pass 62/62 and source-model checks pass 11/11. These remain provisional checks, not final acceptance.

- Internal Release build: `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore` exits `1` with 12 `CS0234`/`CS0246` errors for `Hexalith.EventStore.Client.Streams`, `AuthoritativeEventStream`, `IAuthoritativeEventStreamReader`, and `AuthoritativeStreamReadResult`. The narrow serialized fallback, `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`, also exits `1` with the same 12 errors. Logs: [AC03](../../artifacts/v9/9.2/AC-9.2-03.log) and [fallback](../../artifacts/v9/9.2/internal-release-fallback-build.log). Stale internal Release binaries were not used to claim fresh AC04/05/07 execution.
- Dependency environment: the original baseline and retained migration bind EventStore Client/Contracts `3.113.0`; the current Builds pin restores `3.110.0`, whose packages lack the required authoritative stream types. `python3 _bmad/scripts/verify_conformance_tiering.py --repository . --structure-only --output artifacts/v9/9.2/current-structure-check.json` exits `1` with `ASSERTION_STRENGTH_WEAKENED` because the recorded migration no longer reproduces. The [read-only dependency audit](../../artifacts/v9/9.2/dependency-environment-audit-v1.json) measures only `portableSurface` drift: both before/after strength-inventory digests remain unchanged. The original proposal and its review bindings are preserved; no dependency or assertion was changed to obtain a pass.
- Candidate scope: the unchanged final-record API's `v2_9_2_facts` probe exits `1` with `AUTHORITY_BINDING_INVALID` for seven changed root gitlinks: Builds, EventStore, Folders, Memories, Parties, Projects, and Tenants. The [decision packet](../../artifacts/v9/9.2/candidate-scope-review-v1.md) retains the exact reproduction command; its [machine audit](../../artifacts/v9/9.2/candidate-scope-audit-v1.json) binds the full candidate, promotion commits, and before/after gitlinks. Those existing submodule updates and the original baseline are preserved. No candidate-environment amendment has been authorized or implemented.
- Quality decision: `conformance-oracle-tiering-migration-approval-v3.json` remains absent. The concrete proposal/public-drift/changed-row bindings in the [Quality packet](../../docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md) still require an actual Quality-owner decision. No approval, accepted final record, AC10 `10/10/0/0/0/0` result, or completion transition is claimed.

Fresh complete Release execution, compatible candidate scope, genuine Quality approval, deterministic final-pair generation/insertion verification, and lifecycle updates remain incomplete. The spec and sprint row stay `in-progress`.

EventStore update verification (2026-10-07, root `54c8f7fb5b3557e29af3a7e7239c97362094fd89`, local Builds `af20682ac8fc420068a731ecb87cff84727a3d53`): the latest listed stable version shared by all 13 EventStore packages is `3.115.0`. The authoritative catalog pin was updated in local Builds commit `031b028d3f7b5b614585d4e7e61e6a1350da4096`; its targeted audit and shared-consumer regression fix are in the subsequent local Builds commit. Catalog, consumer authority, exception, whitespace, and root-submodule checks pass. All 115 audit-generator scenarios pass; the deterministic audit validates 304 packages and preserves all 145 other family decisions and 291 other package rows. No push was performed.

- Both separate Release restores/builds pass with zero warnings/errors, resolving the missing stream API blocker. Fresh [portable](../../artifacts/v9/9.2/eventstore-update/portable.trx) execution passes 326/326; fresh [internal](../../artifacts/v9/9.2/eventstore-update/internal.trx) execution passes 91/92. The sole failing case is the Quality approval control. The [verifier](../../artifacts/v9/9.2/eventstore-update/tiering-verification.json) observes all 415 retained cases across 401 methods and all three live controls, with zero skips or omissions; it exits `1` with `TIER_APPROVAL_MISSING`.
- The pending migration now reproduces at proposal `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`. Only its two EventStore `portableSurface` package versions and proposal digest change; all 14 successor row bindings, strength inventories, and public drift remain identical. The previous pending proposal/review packet and [environment comparison](../../artifacts/v9/9.2/eventstore-update/migration-environment-audit.json) are retained. The [Quality packet](../../docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md) is refreshed for the actual dependency environment, with no approval claimed.
- The current read-only [candidate probe](../../artifacts/v9/9.2/eventstore-update/candidate-scope-check.json) still exits `1` with `AUTHORITY_BINDING_INVALID` for the seven previously identified root gitlinks. Compatible candidate scope, genuine Quality approval, fresh committed-candidate acceptance, final-pair generation/insertion, and lifecycle updates remain pending. These measurements supersede the dependency build failure above; they do not establish complete Story 9.2 acceptance.

Current committed checkout verification (2026-10-07, `d54cba773290b555e1939e9d943ae84548e5eba3`): the [command receipts](../../artifacts/v9/9.2/current-candidate/verification-receipts.json) retain fresh measurements without overwriting earlier evidence. Both separate Debug and Release builds pass with zero warnings/errors; Debug and Release AC02/05 controls pass. Both Release assemblies are stamped by this exact committed checkout. AC01/02/03/05/06 pass. Portable execution passes 326/326; internal execution passes 91/92, with only the inventory approval control failing. [AC08](../../artifacts/v9/9.2/current-candidate/AC-9.2-08.json) observes all 415 retained cases passing across 401 active methods and all three live controls, with zero skips or omissions; AC04/07/08 remain blocked by `TIER_APPROVAL_MISSING`.

- [AC09](../../artifacts/v9/9.2/current-candidate/AC-9.2-09.xml) passes 22/22. The [receipt audit](../../artifacts/v9/9.2/current-candidate/fault-receipt-audit.json) verifies every exported candidate/source binding, exact blocker, baseline/restored PASS, and byte-identical restoration. The full frozen matrix remains covered. Fixture execution and approval remain explicitly synthetic and supply no actual acceptance decision.
- Focused Story 9.1/9.2 final-record tooling checks pass [62/62](../../artifacts/v9/9.2/current-candidate/generator-tests.xml); the exact source-model checks pass [11/11](../../artifacts/v9/9.2/current-candidate/source-model-tests-focused.xml). An initial broader source-model selector also attempted historical Story 9.1 freeze generation against the split checkout and reports five `CONFORMANCE_ASSERTION_UNKNOWN` failures. The [failed historical reproduction](../../artifacts/v9/9.2/current-candidate/source-model-tests-broad.log) is retained separately. `_bmad/scripts/tests/conftest.py` retires that suite from default collection; current CI explicitly invokes only its Story 9.2 structural/execution faults. Historical expectations and current acceptance gates are preserved.
- The unchanged final-record API still rejects exactly seven root gitlink changes with `AUTHORITY_BINDING_INVALID`. The current [scope packet](../../artifacts/v9/9.2/current-candidate/candidate-scope-review-v2.md) and [machine audit](../../artifacts/v9/9.2/current-candidate/candidate-scope-audit-v2.json) bind the original baseline, current checkout, seven exact before/after gitlinks, and five owning promotion commits. The [candidate-environment amendment proposal](../../artifacts/v9/9.2/current-candidate/candidate-environment-amendment-proposal-v1.json), digest `a5c12ba32a82338ce8bcb791f8b64355e5a820d7433145e0648d107441bc3d20`, awaits explicit user authorization. It would accept only those existing promotions as the Story 9.2 candidate environment while preserving the baseline and all assertion, public API, dependency, frozen-evidence, full-execution, fault, and independent Quality gates. No active validator or approval was changed.
- The pending migration reproduces at proposal `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`; all 14 successor rows, both strength inventories, and public-drift digest `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1` remain unchanged. The [Quality packet](../../docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md) now links the current measurements and the separate candidate-scope decision. No genuine approval file, final pair, insertion result, `10/10/0/0/0/0` result, or completion transition is claimed. The spec and sprint row remain `in-progress`.

Authorized-amendment implementation verification (2026-10-07): the [focused generator suite](../../artifacts/v9/9.2/environment-amendment-implementation/generator-tests.xml) passes 97/97 and the [source-model checks](../../artifacts/v9/9.2/environment-amendment-implementation/source-model-tests.xml) pass 11/11. Real Git-object fixtures verify the exact approved seven-gitlink/five-promotion environment and reject missing/malformed/uncommitted authorization, modified before/after bindings, altered owners/parents/diffs, absent ancestry, undeclared/removed/non-gitlink roots, extra and reverted promotions, and protected-path changes hidden by restoration. All fixture mutations restore byte-identically and rerun the valid baseline. Existing accepted Story 7.1–9.1 JSON/Markdown pairs remain compatible and unchanged. The [live structure check](../../artifacts/v9/9.2/environment-amendment-implementation/structure-check.json) passes against the unchanged approved migration and actual Quality decision. Schema, Python compilation, and whitespace checks pass. These are pre-candidate implementation checks; fresh committed-candidate AC01–10, deterministic publication, insertion, and lifecycle remain required.

### Resumption check — 2026-10-08

Initial checkout: `b3a813b0ba0020a558a3779e4e88b65612850e82`. Initial resumption checks found no new Story 9.2 implementation changes relative to accepted source candidate `c44f6b7b116b17453aa213d39cd244a3e0cbe1dd`. The later independent review found the guard gaps recorded above. The [fresh command receipts](../../artifacts/v9/9.2/resume-2026-10-08/implementation/current-command-receipts.json) describe initial current-main measurements separately from the preserved acceptance below; subsequent concurrent workspace edits are not reviewed here.

- Focused generator/verifier regressions pass 143/143; the exact source-model checks pass 11/11. Both individual Debug and Release tier builds pass with zero warnings/errors. Evaluated portability and declaration controls pass.
- Fresh portable execution passes 326/326; internal execution passes 91/92. The sole failure is `PostSplitAssertionInventoryShouldEqualApprovedDisposition`, reporting `PUBLIC_CONTRACT_WIDENED`. The [execution-only inventory](../../artifacts/v9/9.2/resume-2026-10-08/implementation/current-execution-inventory.json) independently observes all 415 retained cases across 401 methods, plus three controls, without skips or omissions; complete passing execution remains false. The live public-contract snapshot comparison passes. This blocker compares production source bytes and does not establish an exported API addition.
- `python3 _bmad/scripts/verify_conformance_tiering.py --repository . --structure-only --output artifacts/v9/9.2/resume-2026-10-08/implementation/structure-check.json` exits `1` with `PUBLIC_CONTRACT_WIDENED`. The complete verifier against the fresh relocated TRX inputs also exits `1`. The exact 22-case AC09 selector exits `1` with 22 setup errors at that same frozen-source guard, before any fault mutation; these errors supply no current fault-detection proof.
- The actual current-candidate environment API reports `AUTHORITY_BINDING_INVALID`. The [scope audit](../../artifacts/v9/9.2/resume-2026-10-08/current-scope-audit.json) measures two changed production source files and five changed root gitlinks after the accepted candidate. `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.2.json --verify-inserted-record _bmad-output/implementation-artifacts/spec-9-2-make-the-portable-tier-structural-and-prove-complete-monotonic-tier-execution.md` exits `1` with `CANDIDATE_NOT_FINAL` and `GITLINK_DRIFT`; its [failure document](../../artifacts/v9/9.2/resume-2026-10-08/implementation/inserted-record-current-check.log) identifies the later source/environment commits.
- Accepted-pair schema, cross-digests, retained-copy equality, and inserted Markdown preservation pass. All 694 files in the [retained archive](../../artifacts/v9/9.2/resume-2026-10-08/retained-archive-integrity.json) match their recorded hashes and sizes. Current builds changed generated binaries only; original candidate binaries remain retained. `git diff --check` passes.

The [decision packet](../../artifacts/v9/9.2/resume-2026-10-08/decision-packet.md) separates review of the original accepted candidate from preparation of a new current-main acceptance boundary. The user selected "Review the original accepted candidate (Recommended)". The review therefore evaluates baseline `51aa06b856bcb0aaf22013153cfe02a51b046156` through accepted source candidate `c44f6b7b116b17453aa213d39cd244a3e0cbe1dd` in an isolated local checkout, using the original completion claims and retained evidence. Frozen intent, baseline, approvals, accepted records, submodules, and unrelated user changes remain preserved. The spec and sprint row remain `in-progress`; current-main failures remain separate and no new current-main acceptance, authority amendment, or candidate commit is claimed.

Original-candidate reproduction: both individual Release builds pass; effective AC02/04/05 pass; portable AC06 passes 326/326 and internal AC07 passes 92/92. Complete AC08 passes with all 415 retained cases across 401 methods plus three controls, without skips. AC09 passes 22/22. Initial portable failures from missing EventStore/Builds Git history are retained separately; supplying exact accepted root dependency history through local sibling checkouts resolves them without source edits or nested-submodule initialization. [Successful rerun receipts](../../artifacts/v9/9.2/resume-2026-10-08/original-candidate-fresh/with-exact-history/command-receipts.json) and the [review report](../../artifacts/v9/9.2/resume-2026-10-08/original-candidate-review.md) document the boundary. These checks do not generate a replacement final record or change the historical 10/10 claim below.

## Auto Run Result

Completed on 2026-10-07 with reviewed source candidate `c44f6b7b116b17453aa213d39cd244a3e0cbe1dd`. The current result supersedes earlier provisional statuses in this spec. All 10 required acceptance checks pass, with zero failures, blocked checks, skips, or not-run checks. Fresh portable execution passes 326/326; internal execution passes 92/92; every retained assertion case and all live controls pass. The exact fault selector passes 22/22, reporting the required blockers and restoring every mutation byte-identically. [Required command receipts](../../artifacts/v9/9.2/review-candidate/uv-gate/verification-receipts.json) bind this committed candidate.

Independent review findings are resolved with nothing deferred. The focused generator/verifier suite passes 143/143, the source-model checks pass, and root-submodule and whitespace checks pass. Repeated final-record generation produces identical JSON and Markdown bytes. Pair-only publication `97d300d9bfda3ed771d35bea9f41a91356c907a9` retains the original source candidate; [inserted-record verification](../../artifacts/v9/9.2/review-candidate/inserted-record-before-lifecycle.json) passes with the exact required summary. Evidence uses immutable retention key `c44f6b7b116b17453aa213d39cd244a3e0cbe1dd`.

The implementation spec is complete and the sprint entry is ready for human review, following the build workflow.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 9.2 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `9.2`
- Candidate: `c44f6b7b116b17453aa213d39cd244a3e0cbe1dd`
- JSON content SHA-256 (all three digest fields zeroed): `221fd0a523595b22c84a90b002888f40fe8ceeb576d814059cfe0651658ae0d8`

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
| `references/Hexalith.Builds` | `160000` | `af20682ac8fc420068a731ecb87cff84727a3d53` |
| `references/Hexalith.Commons` | `160000` | `116d26815eb81e35b3c161e1799e5ee12805fc0a` |
| `references/Hexalith.EventStore` | `160000` | `02e99bfa282ace7d56a1d7d7ad5c0321f63293e3` |
| `references/Hexalith.Folders` | `160000` | `b7f445becca270e176c54f7f6fe19c5d2940ff20` |
| `references/Hexalith.FrontComposer` | `160000` | `c561b3210f15206a90c39c82c58f2e5b1005cd60` |
| `references/Hexalith.Memories` | `160000` | `f4e7eb8626513c83f392a7cabf223b1a4673daa3` |
| `references/Hexalith.Parties` | `160000` | `d519cec885fe32e77452bfe66048e04db22ffd0e` |
| `references/Hexalith.Projects` | `160000` | `f8649509d798435af4021cec4440c425b80f6fd6` |
| `references/Hexalith.Tenants` | `160000` | `811447342e8f44b644a2074565e83f45519528fd` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-9.2-ENTRY-v1` | `647944fb901bd4b5c1ed6c96867a7a46420dfd0f8e56f71032e9f95474a8f3a9` |

## Predecessors

- `9.1`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-9.2-01` | `0` | `PASS` | `none` | `1` | `tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll` | `9e4ae1f90a154d7bc03dfe0cbeb2e15569bc98fea4fe3beefa66a2ccffeb1675` |
| `AC-9.2-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.2/AC-9.2-02.trx` | `60fce9a59eb3f8a32ed5caae57e8aa8745cf23fc21de537aafe3a4cdc71b8c89` |
| `AC-9.2-03` | `0` | `PASS` | `none` | `1` | `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `2c186bc0691900d63d9fb69d07b5aeb3a5aea040613ea7c539cee839463b3c82` |
| `AC-9.2-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.2/AC-9.2-04.trx` | `42bf93c52f31c9739f500c7d9f808281d2cc65fd5661e70eefa72cace1b2d2d9` |
| `AC-9.2-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.2/AC-9.2-05.trx` | `fffb9624c70a7410d5f773e5ad82e1a665e562014e2010a592edd72615dd0e38` |
| `AC-9.2-06` | `0` | `PASS` | `none` | `326` | `artifacts/v9/9.2/portable.trx` | `8cf2847a09b74a10552b03161e1b5ad810a5b265d21cd17ab47f8218c72ac9f8` |
| `AC-9.2-07` | `0` | `PASS` | `none` | `92` | `artifacts/v9/9.2/internal.trx` | `91e21b22c075b11b552306e93d863a4f00b912e5788c525fdfc1c99ab6f4ed83` |
| `AC-9.2-08` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.2/AC-9.2-08.json` | `d7a9329300f25de16391f04570ee4d78ee2f1ee60fbbf2bf5fe3f61ebe8f8df0` |
| `AC-9.2-09` | `0` | `PASS` | `none` | `22` | `artifacts/v9/9.2/AC-9.2-09.xml` | `972ac7ea59051d32e6cf966a8be0cec292ce34dc27a368d4b0a8fbd85417858d` |
| `AC-9.2-10` | `0` | `PASS` | `none` | `19` | none | none |

### `AC-9.2-01`

Command: `dotnet build tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj --configuration Release --no-restore`

| Bound output | SHA-256 |
| --- | --- |
| `tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll` | `9e4ae1f90a154d7bc03dfe0cbeb2e15569bc98fea4fe3beefa66a2ccffeb1675` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-01#0001` | `build::tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` | `PASS` |

### `AC-9.2-02`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Portable.Tests.PortableCompileSurfaceValidationTest.ResolvedSurfaceShouldContainNoNonPackableModuleReference -result-trx artifacts/v9/9.2/AC-9.2-02.trx -parallelMode none`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.2/AC-9.2-02.trx` | `60fce9a59eb3f8a32ed5caae57e8aa8745cf23fc21de537aafe3a4cdc71b8c89` |
| `tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll` | `9e4ae1f90a154d7bc03dfe0cbeb2e15569bc98fea4fe3beefa66a2ccffeb1675` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-02#0001` | `Hexalith.Conversations.Conformance.Portable.Tests.PortableCompileSurfaceValidationTest.ResolvedSurfaceShouldContainNoNonPackableModuleReference` | `PASS` |

### `AC-9.2-03`

Command: `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore`

| Bound output | SHA-256 |
| --- | --- |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `2c186bc0691900d63d9fb69d07b5aeb3a5aea040613ea7c539cee839463b3c82` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-03#0001` | `build::tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` | `PASS` |

### `AC-9.2-04`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.PostSplitAssertionInventoryShouldEqualApprovedDisposition -result-trx artifacts/v9/9.2/AC-9.2-04.trx -parallelMode none`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.2/AC-9.2-04.trx` | `42bf93c52f31c9739f500c7d9f808281d2cc65fd5661e70eefa72cace1b2d2d9` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `2c186bc0691900d63d9fb69d07b5aeb3a5aea040613ea7c539cee839463b3c82` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-04#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.PostSplitAssertionInventoryShouldEqualApprovedDisposition` | `PASS` |

### `AC-9.2-05`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.BothTiersShouldBeDeclaredEverywhere -result-trx artifacts/v9/9.2/AC-9.2-05.trx -parallelMode none`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.2/AC-9.2-05.trx` | `fffb9624c70a7410d5f773e5ad82e1a665e562014e2010a592edd72615dd0e38` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `2c186bc0691900d63d9fb69d07b5aeb3a5aea040613ea7c539cee839463b3c82` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-05#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.BothTiersShouldBeDeclaredEverywhere` | `PASS` |

### `AC-9.2-06`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll -automated sync -failSkips -result-trx artifacts/v9/9.2/portable.trx -parallelMode none -class- Hexalith.Conversations.Conformance.Tests.ArchitecturePlanningAuthorityValidationTest -class- Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV8ValidationTest -class- Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV9ValidationTest -class- Hexalith.Conversations.Conformance.Tests.PlanningToolingEnvironmentAuthorityV15ValidationTest -class- Hexalith.Conversations.Conformance.Tests.PlanningToolingLifecycleAuthorityV16ValidationTest -class- Hexalith.Conversations.Conformance.Tests.PackageEnvironmentAuthorityV18ValidationTest -class- Hexalith.Conversations.Conformance.Tests.StoryFinalRecordGenerationValidationTest -class- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest -method- Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.BindingsClosuresAndFrozenV1BytesShouldValidateIndependently -method- Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.CurrentControlsAndTierPrerequisiteShouldStayTruthful -method- Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.PublicSurfacesAndConformanceAssertionsShouldHaveZeroGap -method- Hexalith.Conversations.Conformance.Tests.SmC2BaselineReconstructionValidationTest.BaselineShouldRecordAnAuditableReconstructionMethod`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.2/portable.trx` | `8cf2847a09b74a10552b03161e1b5ad810a5b265d21cd17ab47f8218c72ac9f8` |
| `tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll` | `9e4ae1f90a154d7bc03dfe0cbeb2e15569bc98fea4fe3beefa66a2ccffeb1675` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-06#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.SourcesShouldBindCanonicalPathsVersionsAndHashes` | `PASS` |
| `AC-9.2-06#0002` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.PreservedBundleShouldPassZeroGapVerification` | `PASS` |
| `AC-9.2-06#0003` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.AcceptanceCriteriaShouldProjectTheFrozenInventory` | `PASS` |
| `AC-9.2-06#0004` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.CandidateShouldContainNoProductionUiChange` | `PASS` |
| `AC-9.2-06#0005` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DispositionsShouldRemainPreservedAndHistorical` | `PASS` |
| `AC-9.2-06#0006` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DecisionsShouldProjectTheFrozenInventory` | `PASS` |
| `AC-9.2-06#0007` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0008` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0009` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0010` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.AllChecksShouldCarryFR88RequirementAndIdempotencyMappings` | `PASS` |
| `AC-9.2-06#0011` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-06#0012` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0013` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-06#0014` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.AllChecksShouldUseIdempotencyCheckId` | `PASS` |
| `AC-9.2-06#0015` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0016` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0017` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0018` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome` | `PASS` |
| `AC-9.2-06#0019` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `PASS` |
| `AC-9.2-06#0020` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0021` | `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.RunResultShouldHaveExactly8Checks` | `PASS` |
| `AC-9.2-06#0022` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0023` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-06#0024` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0025` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome` | `PASS` |
| `AC-9.2-06#0026` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-06#0027` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0028` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0029` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0030` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0031` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0032` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0033` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.AllChecksShouldUseEventPublicationCheckId` | `PASS` |
| `AC-9.2-06#0034` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `PASS` |
| `AC-9.2-06#0035` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.AllChecksShouldCarryFR90RequirementAndPortabilityGateMappings` | `PASS` |
| `AC-9.2-06#0036` | `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0037` | `Hexalith.Conversations.Conformance.Portable.Tests.PortableCompileSurfaceValidationTest.ResolvedSurfaceShouldContainNoNonPackableModuleReference` | `PASS` |
| `AC-9.2-06#0038` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails` | `PASS` |
| `AC-9.2-06#0039` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId` | `PASS` |
| `AC-9.2-06#0040` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllChecksShouldCarryFR103RequirementAndSecondAdopterMappings` | `PASS` |
| `AC-9.2-06#0041` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.PassScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0042` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllChecksShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0043` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0044` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0045` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0046` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0047` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0048` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0049` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllPassScenariosShouldProduceReadyOutcome` | `PASS` |
| `AC-9.2-06#0050` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RevertedNoRationaleShouldProduceConformantResult` | `PASS` |
| `AC-9.2-06#0051` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.MilestoneOverdueShouldProduceConformantResult` | `PASS` |
| `AC-9.2-06#0052` | `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0053` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.ProcedureRecordedBaselineShouldMatchTheGovernedInventory` | `PASS` |
| `AC-9.2-06#0054` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.InventoryChangeLogShouldBeAnArrayAndEveryRealEntryShouldConform` | `PASS` |
| `AC-9.2-06#0055` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.BothProcedureArtifactsAndTheGovernedInventoryShouldBeCommitted` | `PASS` |
| `AC-9.2-06#0056` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.CommittedProcedureShouldBeAcceptedAndDeclareGovernance` | `PASS` |
| `AC-9.2-06#0057` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.EveryEntryShouldTargetARealInventoryAreaId` | `PASS` |
| `AC-9.2-06#0058` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.EntryTemplateShouldProvideCopyPasteableShapesForBothTypes` | `PASS` |
| `AC-9.2-06#0059` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.EveryRealReclassificationEntryShouldBeAppliedToTheLiveClassification` | `PASS` |
| `AC-9.2-06#0060` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.CommittedProcedureShouldPassScopedContentSafetyScan` | `PASS` |
| `AC-9.2-06#0061` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.EveryWorkedExampleShouldConformToItsTypeSchema` | `PASS` |
| `AC-9.2-06#0062` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.GovernedInventoryShouldBackReferenceStoryOneFiveForBidirectionalDiscoverability` | `PASS` |
| `AC-9.2-06#0063` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.EveryReclassifiedChallengeShouldHaveAMatchingReclassificationEntry` | `PASS` |
| `AC-9.2-06#0064` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.WorkedExampleEntriesShouldNotLeakIntoTheRealInventoryChangeLog` | `PASS` |
| `AC-9.2-06#0065` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.ChangeLogEntrySchemaShouldDefineBothEntryTypes` | `PASS` |
| `AC-9.2-06#0066` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.WorkedExamplesShouldIncludeAnUpheldChallengeAndAReclassification` | `PASS` |
| `AC-9.2-06#0067` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.ReclassificationEntriesShouldNotOverrideApproxLocOrPaths` | `PASS` |
| `AC-9.2-06#0068` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.FoldingEachReclassificationExampleShouldPreserveFr2InvariantAndRecomputePlumbing` | `PASS` |
| `AC-9.2-06#0069` | `Hexalith.Conversations.Conformance.Tests.ClassificationChangeProcedureValidationTest.ProcedureMarkdownShouldDocumentTheFiveStepProcedureAndCanonicalLog` | `PASS` |
| `AC-9.2-06#0070` | `Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.FaultInjectedCandidatesShouldFailWithStableDiagnostics` | `PASS` |
| `AC-9.2-06#0071` | `Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.MarkdownProjectionShouldBeByteExact` | `PASS` |
| `AC-9.2-06#0072` | `Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.V2SchemaShouldBeSeparateClosedAndGoverned` | `PASS` |
| `AC-9.2-06#0073` | `Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.RequirementAndUxDenominatorsShouldMatchIndependentExtraction` | `PASS` |
| `AC-9.2-06#0074` | `Hexalith.Conversations.Conformance.Tests.SmC2BaselineReconstructionValidationTest.GitShouldConfirmTheFixtureIsAbsentAndTheBaselineSubmoduleIsPinned` | `PASS` |
| `AC-9.2-06#0075` | `Hexalith.Conversations.Conformance.Tests.SmC2BaselineReconstructionValidationTest.EvaluatedProjectGraphShouldMatchTheRecordedWorkloadManifest` | `PASS` |
| `AC-9.2-06#0076` | `Hexalith.Conversations.Conformance.Tests.SmC2BaselineReconstructionValidationTest.MarkdownShouldPresentTheReconstructionProvenance` | `PASS` |
| `AC-9.2-06#0077` | `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.GenerateAndSaveContractShapeSnapshotFile` | `PASS` |
| `AC-9.2-06#0078` | `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCaptureExportedPublicTypesDeterministically` | `PASS` |
| `AC-9.2-06#0079` | `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldBeContentSafe` | `PASS` |
| `AC-9.2-06#0080` | `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting` | `PASS` |
| `AC-9.2-06#0081` | `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCoverAllSixReleaseGateBehaviorAreas` | `PASS` |
| `AC-9.2-06#0082` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.EveryAtRiskRegisterEntryShouldBeAccountedFor` | `PASS` |
| `AC-9.2-06#0083` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.ConformanceSuiteContinuityShouldMatchCurrentTreeAndStoryFiveOneEvidence` | `PASS` |
| `AC-9.2-06#0084` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.InventoryChangeLogAndContractShapeImpactShouldBePreserved` | `PASS` |
| `AC-9.2-06#0085` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.SourceArtifactsShouldReferenceDurableRepositoryFiles` | `PASS` |
| `AC-9.2-06#0086` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.ProjectReferenceDispositionShouldMatchCurrentProjectAndServerUsingInventory` | `PASS` |
| `AC-9.2-06#0087` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.EveryStructuralDispositionSectionShouldBeAccountedFor` | `PASS` |
| `AC-9.2-06#0088` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.ActualRemovalsShouldBeDeadPlumbingAndNeverDeleteRowsShouldStillExist` | `PASS` |
| `AC-9.2-06#0089` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.EvidenceShouldNotUseBuildArtifactsGeneratedOutputOrLocalPathsAsSourceOfTruth` | `PASS` |
| `AC-9.2-06#0090` | `Hexalith.Conversations.Conformance.Tests.RemovedTestJustificationLedgerReconciliationValidationTest.JsonAndMarkdownArtifactsShouldExistAndExposeStoryFiveThreeFields` | `PASS` |
| `AC-9.2-06#0091` | `Hexalith.Conversations.Conformance.Tests.FinalConformanceContractDiffEvidenceValidationTest.JsonAndMarkdownArtifactsShouldExistAndBeInternallyConsistent` | `PASS` |
| `AC-9.2-06#0092` | `Hexalith.Conversations.Conformance.Tests.FinalConformanceContractDiffEvidenceValidationTest.ContractShapeDiffShouldBeEmptyOrCarryApprovalReferences` | `PASS` |
| `AC-9.2-06#0093` | `Hexalith.Conversations.Conformance.Tests.FinalConformanceContractDiffEvidenceValidationTest.EvidenceShouldNotUseBuildArtifactsGeneratedOutputOrLocalPathsAsSourceOfTruth` | `PASS` |
| `AC-9.2-06#0094` | `Hexalith.Conversations.Conformance.Tests.FinalConformanceContractDiffEvidenceValidationTest.JsonShouldReferenceStoryOneOneBaselinesAndExactFinalConformanceCounts` | `PASS` |
| `AC-9.2-06#0095` | `Hexalith.Conversations.Conformance.Tests.ReleaseWaiverValidationTest.FixtureWaiverShouldPassValidateWaiverWithZeroErrors` | `PASS` |
| `AC-9.2-06#0096` | `Hexalith.Conversations.Conformance.Tests.ReleaseWaiverValidationTest.FixtureWaiverShouldPassContentSafetyScan` | `PASS` |
| `AC-9.2-06#0097` | `Hexalith.Conversations.Conformance.Tests.ReleaseWaiverValidationTest.WaiverWithPastReviewDateShouldReturnStaleReviewDateError` | `PASS` |
| `AC-9.2-06#0098` | `Hexalith.Conversations.Conformance.Tests.ReleaseWaiverValidationTest.WaiverWithPastExpiryDateShouldReturnExpiredWaiverError` | `PASS` |
| `AC-9.2-06#0099` | `Hexalith.Conversations.Conformance.Tests.ReleaseWaiverValidationTest.WaiverShouldSerializeToStableCamelCaseJsonAndRoundTripDeterministically` | `PASS` |
| `AC-9.2-06#0100` | `Hexalith.Conversations.Conformance.Tests.ReleaseWaiverValidationTest.BlockerWithNullApproverShouldReturnBlockerRequiresApproverError` | `PASS` |
| `AC-9.2-06#0101` | `Hexalith.Conversations.Conformance.Tests.ReleaseWaiverValidationTest.WaiverLifecycleStatusAllShouldReturnExactlyFourValues` | `PASS` |
| `AC-9.2-06#0102` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.Ac5AndAc6ClaimsShouldBeBoundToPassingMachineReadableRuns` | `PASS` |
| `AC-9.2-06#0103` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.MarkdownShouldPresentTheAuthoritativeJsonBoundary` | `PASS` |
| `AC-9.2-06#0104` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.ProofShouldBindExactProductionRouteKeysAndBoundedOutcomes` | `PASS` |
| `AC-9.2-06#0105` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.ProofSourceAndSignedV1BindingsShouldRemainByteIdentical` | `PASS` |
| `AC-9.2-06#0106` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.SmC2PostShouldUseIdenticalEnvelopeAndRecordEveryMechanicalP95Result` | `PASS` |
| `AC-9.2-06#0107` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` | `PASS` |
| `AC-9.2-06#0108` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.AFailingProofResultMustBlockStoryCompletion` | `PASS` |
| `AC-9.2-06#0109` | `Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest.GatewayBoundaryEvidenceShouldCrossTheCoordinatorAndTheDaprStateStore` | `PASS` |
| `AC-9.2-06#0110` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.CommittedInventoryShouldPassScopedContentSafetyScan` | `PASS` |
| `AC-9.2-06#0111` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.PerAreaLocShouldReconcileToTheRecordedSourceTotal` | `PASS` |
| `AC-9.2-06#0112` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.ConsumePromoteCrossReferencesShouldUseWellFormedFrAndStoryIdentifiers` | `PASS` |
| `AC-9.2-06#0113` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.BothJsonAndMarkdownSiblingArtifactsShouldBeCommitted` | `PASS` |
| `AC-9.2-06#0114` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.EveryConsumeOrPromoteEntryShouldNameItsCapabilityFrAndOwningStory` | `PASS` |
| `AC-9.2-06#0115` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.PlumbingDerivationRowsShouldEnumerateExactlyTheConsumeAndPromoteAreas` | `PASS` |
| `AC-9.2-06#0116` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.PlumbingBaselineShouldEqualConsumePlusPromoteLoc` | `PASS` |
| `AC-9.2-06#0117` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.Story62DispositionShouldPreserveSignedV1AndReproduceTheConsumedPath` | `PASS` |
| `AC-9.2-06#0118` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.NoSourceFileShouldBeDoubleCountedAcrossAreas` | `PASS` |
| `AC-9.2-06#0119` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.RecordedPlumbingPercentageShouldMatchTheComputedRatio` | `PASS` |
| `AC-9.2-06#0120` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.EveryAreaShouldAppearExactlyOnceWithASingleValidClassification` | `PASS` |
| `AC-9.2-06#0121` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.ReconciliationPerClassificationSubtotalsShouldBeConsistent` | `PASS` |
| `AC-9.2-06#0122` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.AddendumFirstPassShouldBeExplicitlyConfirmedOrCorrected` | `PASS` |
| `AC-9.2-06#0123` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.InventoryShouldRecordVersioningConventionAndLeaveOpenQuestionsOpen` | `PASS` |
| `AC-9.2-06#0124` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.CommittedInventoryShouldBeAcceptedAndDeclareFr2Governance` | `PASS` |
| `AC-9.2-06#0125` | `Hexalith.Conversations.Conformance.Tests.ConsumePromoteKeepInventoryValidationTest.PromoteLaterCandidatesShouldBeKeptNowNotPromoted` | `PASS` |
| `AC-9.2-06#0126` | `Hexalith.Conversations.Conformance.Tests.ConformanceManifestValidationTest.FixtureManifestShouldPassValidateManifestWithZeroErrors` | `PASS` |
| `AC-9.2-06#0127` | `Hexalith.Conversations.Conformance.Tests.ConformanceManifestValidationTest.AllFixtureEntriesShouldPassContentSafetyScan` | `PASS` |
| `AC-9.2-06#0128` | `Hexalith.Conversations.Conformance.Tests.ConformanceManifestValidationTest.ManifestWithDuplicateTestIdShouldReturnDuplicateTestIdError` | `PASS` |
| `AC-9.2-06#0129` | `Hexalith.Conversations.Conformance.Tests.ConformanceManifestValidationTest.ManifestWithWaivedEntryMissingWaiverReferenceShouldReturnMissingWaiverReferenceError` | `PASS` |
| `AC-9.2-06#0130` | `Hexalith.Conversations.Conformance.Tests.ConformanceManifestValidationTest.ManifestShouldSerializeToStableCamelCaseJsonAndRoundTripDeterministically` | `PASS` |
| `AC-9.2-06#0131` | `Hexalith.Conversations.Conformance.Tests.ConformanceManifestValidationTest.LifecycleStageAllShouldReturnExactlySixStagesMatchingNfr1` | `PASS` |
| `AC-9.2-06#0132` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.GenerateAndSaveAtRiskTestRegisterFile` | `PASS` |
| `AC-9.2-06#0133` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory23StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0134` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.RegisterShouldBeContentSafe` | `PASS` |
| `AC-9.2-06#0135` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory33StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0136` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryReExpressionShouldBeGreenAndAnchored` | `PASS` |
| `AC-9.2-06#0137` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory27StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0138` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory21StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0139` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory24StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0140` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.RegisterShouldEnumerateEveryAtRiskTestDeterministically` | `PASS` |
| `AC-9.2-06#0141` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory22StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0142` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory25StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0143` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryStory26StructuralDispositionShouldBeAnchoredAndGreen` | `PASS` |
| `AC-9.2-06#0144` | `Hexalith.Conversations.Conformance.Tests.AtRiskTestRegisterGenerationTest.EveryRetireOrRetargetEntryShouldNameItsOwningStory` | `PASS` |
| `AC-9.2-06#0145` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SupersessionAllowlistShouldStayNarrowAndExcludeSignedEvidence` | `PASS` |
| `AC-9.2-06#0146` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity` | `PASS` |
| `AC-9.2-06#0147` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SmTwoFactsShouldMatchStoryFourTwoEvidenceWithoutClosingOqTwo` | `PASS` |
| `AC-9.2-06#0148` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.JsonAndMarkdownArtifactsShouldExistAndExposeRequiredFields` | `PASS` |
| `AC-9.2-06#0149` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SignedReleaseOwnerDecisionShouldStillBindTheImmutableV1ReportAndSourceIdentity` | `PASS` |
| `AC-9.2-06#0150` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SourceArtifactsShouldBeRepositoryRelativeExistingFilesWithHashes` | `PASS` |
| `AC-9.2-06#0151` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.ArchivedLegacyPrdRelocationShouldStayNarrowAndContentIdentical` | `PASS` |
| `AC-9.2-06#0152` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SmOneFactsShouldMatchAcceptedInventoryAndRowDispositionMath` | `PASS` |
| `AC-9.2-06#0153` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SignablePayloadHashShouldMatchSourceArtifactManifest` | `PASS` |
| `AC-9.2-06#0154` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.BehaviorPreservationAndRemovedTestFactsShouldMatchSourceArtifacts` | `PASS` |
| `AC-9.2-06#0155` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.FinalDiffShouldMatchIntendedEvidenceBoundaryAndExcludeSubmoduleGitlinks` | `PASS` |
| `AC-9.2-06#0156` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.SmOneRowEvidenceShouldBePartOfSignedSourceManifest` | `PASS` |
| `AC-9.2-06#0157` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.ResidualRisksAndAttestationShouldBeSignableButUnsigned` | `PASS` |
| `AC-9.2-06#0158` | `Hexalith.Conversations.Conformance.Tests.SuccessMetricReportAndAttestationValidationTest.EvidenceShouldNotUseBuildArtifactsGeneratedOutputOrLocalPathsAsSourceOfTruth` | `PASS` |
| `AC-9.2-06#0159` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineEnumeratedSuiteClassesShouldMatchTheActualSuiteClassesInTheAssembly` | `PASS` |
| `AC-9.2-06#0160` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotCapturedSurfaceShouldPassContentSafetyScan` | `PASS` |
| `AC-9.2-06#0161` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineCommitShouldBeAFullFortyCharacterHexShaOnMain` | `PASS` |
| `AC-9.2-06#0162` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface` | `PASS` |
| `AC-9.2-06#0163` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotShouldExistAndDeclareItsAssemblyAndTypeCount` | `PASS` |
| `AC-9.2-06#0164` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedBaselineRecordShouldExistAndDescribeAGreenAllPassOracle` | `PASS` |
| `AC-9.2-06#0165` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotTypeCountShouldMatchTheLiveExportedContractSurface` | `PASS` |
| `AC-9.2-06#0166` | `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineSurvivabilityClassificationShouldAccountForAllFourteenSuites` | `PASS` |
| `AC-9.2-06#0167` | `Hexalith.Conversations.Conformance.Tests.OracleBlindSpotAnalysisArtifactGenerationTest.BuiltArtifactShouldBeDeterministic` | `PASS` |
| `AC-9.2-06#0168` | `Hexalith.Conversations.Conformance.Tests.OracleBlindSpotAnalysisArtifactGenerationTest.GenerateAndSaveArtifactFile` | `PASS` |
| `AC-9.2-06#0169` | `Hexalith.Conversations.Conformance.Tests.OracleBlindSpotAnalysisArtifactGenerationTest.CommittedHeaderShouldDescribeTheArtifact` | `PASS` |
| `AC-9.2-06#0170` | `Hexalith.Conversations.Conformance.Tests.OracleBlindSpotAnalysisArtifactGenerationTest.ArtifactShouldPassContentSafetyScan` | `PASS` |
| `AC-9.2-06#0171` | `Hexalith.Conversations.Conformance.Tests.OracleBlindSpotAnalysisArtifactGenerationTest.BuiltArtifactShouldBeStructurallyComplete` | `PASS` |
| `AC-9.2-06#0172` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.AuditIntegrityGateShouldBePassWhenGovernancePreconditionIsReady` | `PASS` |
| `AC-9.2-06#0173` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.ArtifactShouldContainAllSevenRequiredGateEntries` | `PASS` |
| `AC-9.2-06#0174` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.AllGateStatusesShouldBelongToTheClosedVocabulary` | `PASS` |
| `AC-9.2-06#0175` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.ProviderPortabilityGateShouldAlwaysBeUnknownAcceptedWithNoAdopterMapping` | `PASS` |
| `AC-9.2-06#0176` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.BuilderShouldBeFullyDeterministicWithSameInputs` | `PASS` |
| `AC-9.2-06#0177` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.OverallStatusShouldBeDeterministicAcrossRuns` | `PASS` |
| `AC-9.2-06#0178` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.GenerateAndSaveFixtureArtifactFile` | `PASS` |
| `AC-9.2-06#0179` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.TenantIsolationGateShouldBeUnknownAcceptedBecauseTenantBindingUsesUnknownOutcome` | `PASS` |
| `AC-9.2-06#0180` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.BuilderShouldRejectNullConformanceRunResult` | `PASS` |
| `AC-9.2-06#0181` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.BuilderShouldProduceValidArtifactFromCoreFixture` | `PASS` |
| `AC-9.2-06#0182` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.ArtifactContentSafetyScanShouldPass` | `PASS` |
| `AC-9.2-06#0183` | `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.BuilderShouldRejectNullSignerOrRunnerId` | `PASS` |
| `AC-9.2-06#0184` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0185` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.AllChecksShouldCarryFR94RequirementAndPlatformEvidenceGateMappings` | `PASS` |
| `AC-9.2-06#0186` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0187` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0188` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0189` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome` | `PASS` |
| `AC-9.2-06#0190` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-06#0191` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `PASS` |
| `AC-9.2-06#0192` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0193` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0194` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0195` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-06#0196` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0197` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0198` | `Hexalith.Conversations.Conformance.Tests.PlatformEvidenceSeparationConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId` | `PASS` |
| `AC-9.2-06#0199` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.ConformanceRunResultShouldPreserveClosedTraceabilityTokensContainingTenantAndPartySegments` | `PASS` |
| `AC-9.2-06#0200` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.CrossTenantDenialShouldNotCarryClientActionThatDistinguishesExistence` | `PASS` |
| `AC-9.2-06#0201` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.ConformanceRunResultShouldNotLeakPoisonSentinels` | `PASS` |
| `AC-9.2-06#0202` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.EveryTypedFailureFixtureScenarioShouldExposeOnlySafeStructuredFields` | `PASS` |
| `AC-9.2-06#0203` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.TypedFailureCasesShouldRemainContentSafeOnTheWire` | `PASS` |
| `AC-9.2-06#0204` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.CrossTenantDenialShouldUseHiddenShapeAndNotRevealExistence` | `PASS` |
| `AC-9.2-06#0205` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.ConformanceRunResultShouldNotLeakProtectedIdentifiersOrInfrastructureTerms` | `PASS` |
| `AC-9.2-06#0206` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.FixtureShouldBeMarkedSyntheticAndDeterministic` | `PASS` |
| `AC-9.2-06#0207` | `Hexalith.Conversations.Conformance.Tests.CoreFixtureContentSafetyTest.PoisonProjectionShouldCarrySentinelsThatNeverAppearInAuthorizedSurfaces` | `PASS` |
| `AC-9.2-06#0208` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0209` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.RunResultShouldHaveExactly12Checks` | `PASS` |
| `AC-9.2-06#0210` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0211` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0212` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `PASS` |
| `AC-9.2-06#0213` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0214` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0215` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-06#0216` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-06#0217` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.AllChecksShouldCarryFR87RequirementAndTenantIsolationGateMappings` | `PASS` |
| `AC-9.2-06#0218` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome` | `PASS` |
| `AC-9.2-06#0219` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0220` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0221` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.AllChecksShouldUseTenantBindingCheckId` | `PASS` |
| `AC-9.2-06#0222` | `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0223` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.RunAggregationShouldBeDeterministicAndDegradedWhenAllChecksConformWithAStaleScenario` | `PASS` |
| `AC-9.2-06#0224` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.CompatibilityDiscoveryCheckShouldSurfaceUnsupportedAsBlockedTypedError` | `PASS` |
| `AC-9.2-06#0225` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.RunResultShouldRoundTripLosslesslyPreservingEveryCheckFieldForCi` | `PASS` |
| `AC-9.2-06#0226` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.SuiteShouldCoverEveryCoreConformanceCheck` | `PASS` |
| `AC-9.2-06#0227` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.NonReadyChecksMustCarryTypedErrorsAndReadyChecksMustNot` | `PASS` |
| `AC-9.2-06#0228` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.RunShouldExerciseTheReadyDegradedAndBlockedOutcomesAcrossChecks` | `PASS` |
| `AC-9.2-06#0229` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.SuiteShouldExerciseTheAc4ScenarioMatrix` | `PASS` |
| `AC-9.2-06#0230` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.EveryCheckShouldCarryTraceableRequirementPreconditionAndReleaseGateMappings` | `PASS` |
| `AC-9.2-06#0231` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.RunResultShouldSerializeToDeterministicWebJsonForCi` | `PASS` |
| `AC-9.2-06#0232` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.RunResultShouldRoundTripAndRemainAdditiveTolerantForCi` | `PASS` |
| `AC-9.2-06#0233` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.ProjectionFreshnessCheckShouldSurfaceStaleAsDegradedNonTrustBearing` | `PASS` |
| `AC-9.2-06#0234` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.IdempotencyCheckShouldSurfaceNonRetryableConflictAsBlocked` | `PASS` |
| `AC-9.2-06#0235` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.SuiteShouldPassEveryCheckAgainstTheSyntheticFixture` | `PASS` |
| `AC-9.2-06#0236` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.ErrorEnvelopeCheckShouldReuseSharedTypedErrorCatalog` | `PASS` |
| `AC-9.2-06#0237` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.TenantBindingCheckShouldExerciseCrossTenantHiddenSideChannelShape` | `PASS` |
| `AC-9.2-06#0238` | `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.EveryEmittedFailureClassificationMustBelongToTheClosedVocabulary` | `PASS` |
| `AC-9.2-06#0239` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0240` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome` | `PASS` |
| `AC-9.2-06#0241` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0242` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0243` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0244` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0245` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId` | `PASS` |
| `AC-9.2-06#0246` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.AllChecksShouldCarryFR89RequirementAndRedactionGateMappings` | `PASS` |
| `AC-9.2-06#0247` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0248` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0249` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-06#0250` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0251` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0252` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `PASS` |
| `AC-9.2-06#0253` | `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-06#0254` | `Hexalith.Conversations.Conformance.Tests.OqTwoTargetInterpretationDecisionValidationTest.SmOneTargetShouldRecomputeAndBeMetInclusively` | `PASS` |
| `AC-9.2-06#0255` | `Hexalith.Conversations.Conformance.Tests.OqTwoTargetInterpretationDecisionValidationTest.DecisionArtifactsShouldExistAndRecordApprovedResolution` | `PASS` |
| `AC-9.2-06#0256` | `Hexalith.Conversations.Conformance.Tests.OqTwoTargetInterpretationDecisionValidationTest.HistoricalEvidenceBindingsShouldMatchAndRemainPointInTimeEvidence` | `PASS` |
| `AC-9.2-06#0257` | `Hexalith.Conversations.Conformance.Tests.OqTwoTargetInterpretationDecisionValidationTest.SmTwoTargetShouldRecomputeAndRemainEstimateQualified` | `PASS` |
| `AC-9.2-06#0258` | `Hexalith.Conversations.Conformance.Tests.NoInModuleSharedFakeDuplicateConformanceTest.NoInModuleDomainResultAssertionsDuplicateShouldExist` | `PASS` |
| `AC-9.2-06#0259` | `Hexalith.Conversations.Conformance.Tests.NoInModuleSharedFakeDuplicateConformanceTest.NoInModuleEventStoreGatewayClientDuplicateShouldExist` | `PASS` |
| `AC-9.2-06#0260` | `Hexalith.Conversations.Conformance.Tests.NoInModuleSharedFakeDuplicateConformanceTest.NoInModuleActorStateManagerDuplicateShouldExist` | `PASS` |
| `AC-9.2-06#0261` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0262` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0263` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.AllChecksShouldUseEventPublicationCheckId` | `PASS` |
| `AC-9.2-06#0264` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.AllChecksShouldCarryFR91RequirementAndSchemaEvolutionGateMappings` | `PASS` |
| `AC-9.2-06#0265` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0266` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `PASS` |
| `AC-9.2-06#0267` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0268` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0269` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0270` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome` | `PASS` |
| `AC-9.2-06#0271` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0272` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-06#0273` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0274` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0275` | `Hexalith.Conversations.Conformance.Tests.EventSchemaEvolutionConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-06#0276` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0277` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.DeferredNoAreasShouldProduceConformantResult` | `PASS` |
| `AC-9.2-06#0278` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0279` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails` | `PASS` |
| `AC-9.2-06#0280` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.WaivedNoRefShouldProduceConformantResult` | `PASS` |
| `AC-9.2-06#0281` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllChecksShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0282` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllChecksShouldCarryFR100RequirementAndReleaseScopeMappings` | `PASS` |
| `AC-9.2-06#0283` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0284` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0285` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0286` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.PassScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0287` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0288` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0289` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId` | `PASS` |
| `AC-9.2-06#0290` | `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllPassScenariosShouldProduceReadyOutcome` | `PASS` |
| `AC-9.2-06#0291` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.FutureGovernanceVocabularyShouldNotAppearAsImplementedMutationPaths` | `PASS` |
| `AC-9.2-06#0292` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.EveryGovernanceMutationShouldPairItsEventWithAuditEvidence` | `PASS` |
| `AC-9.2-06#0293` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.GovernanceMutationWithMissingAuditEvidenceShouldFailClosed` | `PASS` |
| `AC-9.2-06#0294` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.NonGovernanceCommandsShouldEmitEventsWithoutAuditEvidenceDependency` | `PASS` |
| `AC-9.2-06#0295` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.GovernanceMutationWithMismatchedAuditEvidenceShouldFailClosed` | `PASS` |
| `AC-9.2-06#0296` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.AggregateGovernanceCommandSurfaceShouldMatchTheAuditPairedInventory` | `PASS` |
| `AC-9.2-06#0297` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllPassScenariosShouldProduceReadyOutcome` | `PASS` |
| `AC-9.2-06#0298` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0299` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0300` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.PassScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0301` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails` | `PASS` |
| `AC-9.2-06#0302` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId` | `PASS` |
| `AC-9.2-06#0303` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllChecksShouldCarryFR102RequirementAndBuyerAcceptanceMappings` | `PASS` |
| `AC-9.2-06#0304` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0305` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-06#0306` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.ExpiredItemShouldProduceConformantResult` | `PASS` |
| `AC-9.2-06#0307` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.MissingAckShouldProduceConformantResult` | `PASS` |
| `AC-9.2-06#0308` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllChecksShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0309` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0310` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0311` | `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0312` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-06#0313` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0314` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError` | `PASS` |
| `AC-9.2-06#0315` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-06#0316` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-06#0317` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-06#0318` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.AllChecksShouldCarryFR92RequirementAndContractCompatibilityGateMappings` | `PASS` |
| `AC-9.2-06#0319` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-06#0320` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.AllChecksShouldUseCompatibilityDiscoveryCheckId` | `PASS` |
| `AC-9.2-06#0321` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-06#0322` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-06#0323` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome` | `PASS` |
| `AC-9.2-06#0324` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-06#0325` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-06#0326` | `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |

### `AC-9.2-07`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -result-trx artifacts/v9/9.2/internal.trx -parallelMode none`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.2/internal.trx` | `91e21b22c075b11b552306e93d863a4f00b912e5788c525fdfc1c99ab6f4ed83` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `2c186bc0691900d63d9fb69d07b5aeb3a5aea040613ea7c539cee839463b3c82` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-07#0001` | `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyOracleCharacterizationTest.LiveExecutorShouldReplayDuplicateWithoutReinvokingMutation` | `PASS` |
| `AC-9.2-07#0002` | `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyOracleCharacterizationTest.LiveExecutorShouldInvokeMutationOnceForFirstSubmission` | `PASS` |
| `AC-9.2-07#0003` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoMeasurementDimensionShouldEverCarryAForbiddenValue` | `PASS` |
| `AC-9.2-07#0004` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.RunShouldEmitAtLeastOneMeasurementPerCounter` | `PASS` |
| `AC-9.2-07#0005` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.EveryMeasurementShouldCarryOnlyApprovedDimensionKeys` | `PASS` |
| `AC-9.2-07#0006` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoStructuredLogMessageShouldEverCarryAForbiddenValue` | `PASS` |
| `AC-9.2-07#0007` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoneSentinelGuardShouldPreventEmissionOfANoneDimensionValue` | `PASS` |
| `AC-9.2-07#0008` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.StructuredLogMessagesShouldNotCarryTenantOrPartyOrConversationIdShapes` | `PASS` |
| `AC-9.2-07#0009` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.BooleanDimensionsShouldOnlyCarryBoundedTrueOrFalseTokens` | `PASS` |
| `AC-9.2-07#0010` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoMeasurementDimensionShouldCarryRawIdentifierShapes` | `PASS` |
| `AC-9.2-07#0011` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.FixtureForbiddenValuesShouldCoverEveryRequiredDisclosureClass` | `PASS` |
| `AC-9.2-07#0012` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.GateIdShouldBeTheOnlyStringDimensionOutsideTheClassAndBooleanVocabularies` | `PASS` |
| `AC-9.2-07#0013` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.ClassDimensionsShouldOnlyCarryClosedVocabularyTokens` | `PASS` |
| `AC-9.2-07#0014` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.GateIdDimensionShouldOnlyCarryApprovedBoundedGateIds` | `PASS` |
| `AC-9.2-07#0015` | `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.EveryTelemetrySurfaceShouldRejectTheSentinelNoneValue` | `PASS` |
| `AC-9.2-07#0016` | `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.GovernanceEvidenceShouldBeAnchoredThroughPublicReadSurface` | `PASS` |
| `AC-9.2-07#0017` | `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.PoisonProjectionShouldNotExposeDetailThroughPublicReadSurface` | `PASS` |
| `AC-9.2-07#0018` | `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: "stale")` | `PASS` |
| `AC-9.2-07#0019` | `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: "rebuilding")` | `PASS` |
| `AC-9.2-07#0020` | `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: "gap")` | `PASS` |
| `AC-9.2-07#0021` | `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: "unavailable")` | `PASS` |
| `AC-9.2-07#0022` | `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.RedactedMessageShouldStaySuppressedThroughPublicReadSurface` | `PASS` |
| `AC-9.2-07#0023` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.DistinctDimensionValueCountPerKeyShouldNotExceedItsCardinalityBudget` | `PASS` |
| `AC-9.2-07#0024` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.ProjectionLagClassShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0025` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.TenantAccessRequirementShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0026` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.EveryApprovedCounterShouldBeExercisedUnderLoad` | `PASS` |
| `AC-9.2-07#0027` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.ApprovedGateIdVocabularyShouldBeBoundedAndSmall` | `PASS` |
| `AC-9.2-07#0028` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.ConformanceStatusClassShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0029` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.ProjectionFreshnessClassShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0030` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.DistinctDimensionValuesUnderLoadShouldStayWithinTheApprovedClosedVocabulary` | `PASS` |
| `AC-9.2-07#0031` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.CommandRejectionClassShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0032` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.PrivilegedAccessClassShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0033` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.TenantDenialClassShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0034` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.PublicationFailureClassShouldHaveFixedCardinalityBudget` | `PASS` |
| `AC-9.2-07#0035` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.CardinalityGateShouldRejectUnboundedOrRawGateIdValues` | `PASS` |
| `AC-9.2-07#0036` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.EveryEmittedGateIdUnderLoadShouldBeWithinTheBoundedApprovedSet` | `PASS` |
| `AC-9.2-07#0037` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.EveryClosedVocabularyEnumShouldStayWithinASmallBudgetCeiling` | `PASS` |
| `AC-9.2-07#0038` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.GateIdShouldBeTheOnlyDimensionCarryingValuesOutsideClassAndBooleanVocabularies` | `PASS` |
| `AC-9.2-07#0039` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.EachCounterShouldOnlyEverEmitItsApprovedDimensionKeySet` | `PASS` |
| `AC-9.2-07#0040` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.CardinalityGateShouldAcceptEveryApprovedGateId` | `PASS` |
| `AC-9.2-07#0041` | `Hexalith.Conversations.Conformance.Tests.TelemetryCardinalityConformanceSuiteTest.HighCardinalityLoadShouldProduceManyMeasurementsButFewDistinctTagValues` | `PASS` |
| `AC-9.2-07#0042` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence` | `PASS` |
| `AC-9.2-07#0043` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceMetadataWriteFailureAsUnavailable` | `PASS` |
| `AC-9.2-07#0044` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveFreshnessClassifierShouldNeverPromoteDegradedStatesToCurrent` | `PASS` |
| `AC-9.2-07#0045` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldReportCurrentProjectionAsTrustBearing` | `PASS` |
| `AC-9.2-07#0046` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceStaleProjectionAsNonTrustBearing` | `PASS` |
| `AC-9.2-07#0047` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceGapAsRebuildingNonTrustBearing` | `PASS` |
| `AC-9.2-07#0048` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceActiveRebuildAsNonTrustBearing` | `PASS` |
| `AC-9.2-07#0049` | `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction` | `PASS` |
| `AC-9.2-07#0050` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditSinkFailClosedConformanceTest.GovernedMutationShouldFailClosedWhenAuditSinkThrows` | `PASS` |
| `AC-9.2-07#0051` | `Hexalith.Conversations.Conformance.Tests.GovernanceAuditSinkFailClosedConformanceTest.GovernedMutationShouldEmitEventWhenAuditSinkHealthy` | `PASS` |
| `AC-9.2-07#0052` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.BothTiersShouldBeDeclaredEverywhere` | `PASS` |
| `AC-9.2-07#0053` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.PostSplitAssertionInventoryShouldEqualApprovedDisposition` | `PASS` |
| `AC-9.2-07#0054` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldDenyContradictoryTenantBindings` | `PASS` |
| `AC-9.2-07#0055` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "unknown", expectedReason: UnknownTenant)` | `PASS` |
| `AC-9.2-07#0056` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "disabled", expectedReason: TenantDisabled)` | `PASS` |
| `AC-9.2-07#0057` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "stale", expectedReason: TenantAccessStale)` | `PASS` |
| `AC-9.2-07#0058` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "ambiguous", expectedReason: TenantProjectionPoisoned)` | `PASS` |
| `AC-9.2-07#0059` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "insufficient", expectedReason: InsufficientRole)` | `PASS` |
| `AC-9.2-07#0060` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "unavailable", expectedReason: TenantAccessUnavailable)` | `PASS` |
| `AC-9.2-07#0061` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "gap", expectedReason: TenantAccessGapDetected)` | `PASS` |
| `AC-9.2-07#0062` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "rollback", expectedReason: TenantAccessRolledBack)` | `PASS` |
| `AC-9.2-07#0063` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "unmapped-role", expectedReason: UnmappedRole)` | `PASS` |
| `AC-9.2-07#0064` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "unmapped-status", expectedReason: UnmappedStatus)` | `PASS` |
| `AC-9.2-07#0065` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "malformed-projection", expectedReason: MalformedProjection)` | `PASS` |
| `AC-9.2-07#0066` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: "member-poisoned", expectedReason: TenantProjectionPoisoned)` | `PASS` |
| `AC-9.2-07#0067` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldDenyCrossTenantMemberLeakage` | `PASS` |
| `AC-9.2-07#0068` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenTenantBindingIsMissing` | `PASS` |
| `AC-9.2-07#0069` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveGuardShouldNotRunProtectedOperationWhenLiveServiceDenies` | `PASS` |
| `AC-9.2-07#0070` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenTenantIdIsMalformed` | `PASS` |
| `AC-9.2-07#0071` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldAllowAuthorizedOwner` | `PASS` |
| `AC-9.2-07#0072` | `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenCallerPrincipalIsMalformed` | `PASS` |
| `AC-9.2-07#0073` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId` | `PASS` |
| `AC-9.2-07#0074` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues` | `PASS` |
| `AC-9.2-07#0075` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.NullScenariosListShouldThrow` | `PASS` |
| `AC-9.2-07#0076` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.WaivedGateScenarioShouldProduceReadyOutcome` | `PASS` |
| `AC-9.2-07#0077` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.FailScenariosShouldHaveNonNullTypedError` | `PASS` |
| `AC-9.2-07#0078` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments` | `PASS` |
| `AC-9.2-07#0079` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.RunResultShouldHaveExactly10Checks` | `PASS` |
| `AC-9.2-07#0080` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant` | `PASS` |
| `AC-9.2-07#0081` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.AllChecksShouldCarryFR99RequirementAndConformanceStatusMappings` | `PASS` |
| `AC-9.2-07#0082` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.EmptyScenariosListShouldThrow` | `PASS` |
| `AC-9.2-07#0083` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip` | `PASS` |
| `AC-9.2-07#0084` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.PreconditionMappingsShouldNotBeEmpty` | `PASS` |
| `AC-9.2-07#0085` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.NullCorrelationIdShouldThrow` | `PASS` |
| `AC-9.2-07#0086` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.PassScenariosShouldHaveNullTypedError` | `PASS` |
| `AC-9.2-07#0087` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome` | `PASS` |
| `AC-9.2-07#0088` | `Hexalith.Conversations.Conformance.Tests.ConformanceStatusConformanceSuiteTest.OnlyProductInvariantFailScenarioShouldHaveBlockingTrue` | `PASS` |
| `AC-9.2-07#0089` | `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorReplayPayloadShouldExcludeCallerSuppliedSecrets` | `PASS` |
| `AC-9.2-07#0090` | `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorShouldReturnRetryableUncertaintyForPendingKeyWithoutMutation` | `PASS` |
| `AC-9.2-07#0091` | `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorShouldPreserveOriginalReasonCodeOnDuplicateRejectionReplay` | `PASS` |
| `AC-9.2-07#0092` | `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorShouldRejectConflictingKeyReuseWithoutMutation` | `PASS` |

### `AC-9.2-08`

Command: `python3 _bmad/scripts/verify_conformance_tiering.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.2.json --portable-result artifacts/v9/9.2/portable.trx --internal-result artifacts/v9/9.2/internal.trx --output artifacts/v9/9.2/AC-9.2-08.json`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.2/AC-9.2-08.json` | `d7a9329300f25de16391f04570ee4d78ee2f1ee60fbbf2bf5fe3f61ebe8f8df0` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-08#0001` | `python-output::artifacts/v9/9.2/AC-9.2-08.json` | `PASS` |

### `AC-9.2-09`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_conformance_tiering.py -k structural_and_execution_faults --junitxml=artifacts/v9/9.2/AC-9.2-09.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-09#0001` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[nonportable-reference]` | `PASS` |
| `AC-9.2-09#0002` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[transitive-nonportable-reference]` | `PASS` |
| `AC-9.2-09#0003` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[resolved-nonportable-reference]` | `PASS` |
| `AC-9.2-09#0004` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[renamed-nonportable-reference]` | `PASS` |
| `AC-9.2-09#0005` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[assertion-deleted]` | `PASS` |
| `AC-9.2-09#0006` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[assertion-duplicated]` | `PASS` |
| `AC-9.2-09#0007` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[assertion-renamed]` | `PASS` |
| `AC-9.2-09#0008` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[assertion-weakened]` | `PASS` |
| `AC-9.2-09#0009` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[tier-missing]` | `PASS` |
| `AC-9.2-09#0010` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[reason-missing]` | `PASS` |
| `AC-9.2-09#0011` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[approval-missing]` | `PASS` |
| `AC-9.2-09#0012` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[project-missing]` | `PASS` |
| `AC-9.2-09#0013` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[declaration-missing]` | `PASS` |
| `AC-9.2-09#0014` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[completion-declaration-missing]` | `PASS` |
| `AC-9.2-09#0015` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[denominator-drift]` | `PASS` |
| `AC-9.2-09#0016` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[public-widened]` | `PASS` |
| `AC-9.2-09#0017` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[v1-mutated]` | `PASS` |
| `AC-9.2-09#0018` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[predecessor-disposition-detached]` | `PASS` |
| `AC-9.2-09#0019` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[execution-skipped]` | `PASS` |
| `AC-9.2-09#0020` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[execution-not-run]` | `PASS` |
| `AC-9.2-09#0021` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[execution-empty]` | `PASS` |
| `AC-9.2-09#0022` | `_bmad.scripts.tests.test_conformance_tiering::test_structural_and_execution_faults[execution-regressed]` | `PASS` |

### `AC-9.2-10`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.2.json --format bundle --output-json docs/release-evidence/story-9.2-final-record-v2.json --output-markdown docs/release-evidence/story-9.2-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.2-10#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-9.2-10#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-9.2-10#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-9.2-10#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-9.2-10#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-9.2-10#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-9.2-10#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-9.2-10#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-9.2-10#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |
| `AC-9.2-10#0010` | `generator::hash-bound-execution-amendment-preserves-frozen-contract` | `PASS` |
| `AC-9.2-10#0011` | `generator::story-9.1-accepted-pair-and-disposition-compatible` | `PASS` |
| `AC-9.2-10#0012` | `generator::both-projects-assemblies-and-results-bound` | `PASS` |
| `AC-9.2-10#0013` | `generator::evaluated-portable-surface-has-no-nonportable-reference` | `PASS` |
| `AC-9.2-10#0014` | `generator::before-after-identities-and-strengths-bound` | `PASS` |
| `AC-9.2-10#0015` | `generator::genuine-quality-approval-binds-every-successor-and-public-drift` | `PASS` |
| `AC-9.2-10#0016` | `generator::415-frozen-cases-pass-and-controls-count-separately` | `PASS` |
| `AC-9.2-10#0017` | `generator::all-structural-execution-faults-measured-and-restored` | `PASS` |
| `AC-9.2-10#0018` | `generator::fault-receipts-bind-candidate-source-bytes` | `PASS` |
| `AC-9.2-10#0019` | `generator::protected-v1-bytes-and-fr20-membership-retained` | `PASS` |

## Story 9.2 conformance execution

- Candidate: `c44f6b7b116b17453aa213d39cd244a3e0cbe1dd`
- Story 9.1 record SHA-256: `c9d8ef947f1a43a702039d9c1c7c9cb1922f974e6ba207cba8b751fca93bd592`
- Frozen definitions / active methods / historical exclusions: 452 / 401 / 51
- Preserved cases before / after: 415 / 415; live controls: 3; historical controls: 7
- Migration proposal SHA-256: `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`
- Quality approval SHA-256: `8143718e4d1b1f9966fd90cb0ea64bd9cf101e6f114ca4001ba64e1046ef0edb`
- Candidate fault-source SHA-256: `d12c2ef3ac1e05b8d9974659638de03e9a8a1f24717cd6632ed4547a67afd947`

| Tier | Frozen methods | Frozen cases | Controls | Passed | Project SHA-256 | Assembly SHA-256 | Result SHA-256 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `portable` | `325` | `325` | `1` | `326` | `c20c87af2a4e808cb3d5d979dc92c23b9186396b7f5dce279fc36a2fad9eb0fc` | `9e4ae1f90a154d7bc03dfe0cbeb2e15569bc98fea4fe3beefa66a2ccffeb1675` | `8cf2847a09b74a10552b03161e1b5ad810a5b265d21cd17ab47f8218c72ac9f8` |
| `module-internal` | `76` | `90` | `2` | `92` | `07f95d6963e22f6363c292867d84723f268fb58dafa7a61f1d722c3f53a58931` | `2c186bc0691900d63d9fb69d07b5aeb3a5aea040613ea7c539cee839463b3c82` | `91e21b22c075b11b552306e93d863a4f00b912e5788c525fdfc1c99ab6f4ed83` |

| Inventory | SHA-256 |
| --- | --- |
| `beforeIdentitySha256` | `24003ef13a7794e6210363afca672352c6d2e7528fc002703551f8c2a60935cc` |
| `afterIdentitySha256` | `24003ef13a7794e6210363afca672352c6d2e7528fc002703551f8c2a60935cc` |
| `beforeStrengthInventorySha256` | `48532a7e404c89e50961034dc6b38718869bc7741d27e0059e199e3aa0a2af8f` |
| `afterStrengthInventorySha256` | `11c917696628f10118074ea2f1712152cf5c4679ab44938f2de10b9de05b915e` |

Fault fixtures use explicitly synthetic approval, execution, and assembly bytes. Their measured blockers and restoration bind candidate source inputs; passing acceptance comes from the two tier results above.

### Authorized candidate environment

- Amendment SHA-256: `ae7175a058a5a2a9006d715a96481f25b9a748e1c918a32d111ab0b92c15607a`
- Authorized environment proposal SHA-256: `a5c12ba32a82338ce8bcb791f8b64355e5a820d7433145e0648d107441bc3d20`
- Original measured environment candidate: `d54cba773290b555e1939e9d943ae84548e5eba3`

| Root gitlink | Before commit | After commit | Before / after mode |
| --- | --- | --- | --- |
| `references/Hexalith.Builds` | `ba4ca78c3868a4757cb92d912a54c8a237871b54` | `af20682ac8fc420068a731ecb87cff84727a3d53` | `160000 / 160000` |
| `references/Hexalith.EventStore` | `283b07a52c9c70e1c940164a7011ee8c3ad98b2d` | `02e99bfa282ace7d56a1d7d7ad5c0321f63293e3` | `160000 / 160000` |
| `references/Hexalith.Folders` | `12b3006819968bd52d595a12c9f163eac635478d` | `b7f445becca270e176c54f7f6fe19c5d2940ff20` | `160000 / 160000` |
| `references/Hexalith.Memories` | `cc754ab1487ddcec40340f9afa370aa3114851f1` | `f4e7eb8626513c83f392a7cabf223b1a4673daa3` | `160000 / 160000` |
| `references/Hexalith.Parties` | `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca` | `d519cec885fe32e77452bfe66048e04db22ffd0e` | `160000 / 160000` |
| `references/Hexalith.Projects` | `ada852fcc5ded20014be5d1f2427de33ccce6ebd` | `f8649509d798435af4021cec4440c425b80f6fd6` | `160000 / 160000` |
| `references/Hexalith.Tenants` | `5a519cd73018067d9b444dd777034dc91f9bd4b2` | `811447342e8f44b644a2074565e83f45519528fd` | `160000 / 160000` |

| Owning promotion commit | Parent | Changed gitlinks |
| --- | --- | --- |
| `591cb55182068d9a693f06406dd09f8c9805b51c` | `ae76ee7661683c1f957d446d80e4dcb1554764ec` | `references/Hexalith.EventStore, references/Hexalith.Folders, references/Hexalith.Memories, references/Hexalith.Parties, references/Hexalith.Tenants` |
| `32506cc046b0c351f6cd23ceea869b70a3964822` | `591cb55182068d9a693f06406dd09f8c9805b51c` | `references/Hexalith.EventStore, references/Hexalith.Memories, references/Hexalith.Parties, references/Hexalith.Projects` |
| `f962c5b1f81b995116c83f144c26707258319ba4` | `32506cc046b0c351f6cd23ceea869b70a3964822` | `references/Hexalith.Builds` |
| `54c8f7fb5b3557e29af3a7e7239c97362094fd89` | `f962c5b1f81b995116c83f144c26707258319ba4` | `references/Hexalith.EventStore, references/Hexalith.Memories, references/Hexalith.Parties` |
| `d54cba773290b555e1939e9d943ae84548e5eba3` | `54c8f7fb5b3557e29af3a7e7239c97362094fd89` | `references/Hexalith.Builds, references/Hexalith.Memories, references/Hexalith.Parties` |

## Fault injection

| Fault | Expected blocker | Observed exit | Observed blockers | Before SHA-256 | After SHA-256 |
| --- | --- | --- | --- | --- | --- |
| `nonportable-reference` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `1` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `transitive-nonportable-reference` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `1` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `resolved-nonportable-reference` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `1` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `renamed-nonportable-reference` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `1` | `PORTABLE_TIER_NONPORTABLE_REFERENCE` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `assertion-deleted` | `ASSERTION_INVENTORY_DRIFT` | `1` | `ASSERTION_INVENTORY_DRIFT` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `assertion-duplicated` | `ASSERTION_INVENTORY_DRIFT` | `1` | `ASSERTION_INVENTORY_DRIFT` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `assertion-renamed` | `ASSERTION_INVENTORY_DRIFT` | `1` | `ASSERTION_INVENTORY_DRIFT` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `assertion-weakened` | `ASSERTION_STRENGTH_WEAKENED` | `1` | `ASSERTION_STRENGTH_WEAKENED` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `tier-missing` | `TIER_UNASSIGNED` | `1` | `TIER_UNASSIGNED` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `reason-missing` | `TIER_REASON_MISSING` | `1` | `TIER_REASON_MISSING` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `approval-missing` | `TIER_APPROVAL_MISSING` | `1` | `TIER_APPROVAL_MISSING` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `project-missing` | `TIER_PROJECT_MISSING` | `1` | `TIER_PROJECT_MISSING` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `declaration-missing` | `TIER_NOT_DECLARED` | `1` | `TIER_NOT_DECLARED` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `completion-declaration-missing` | `TIER_NOT_DECLARED` | `1` | `TIER_NOT_DECLARED` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `denominator-drift` | `FR20_DENOMINATOR_DRIFT` | `1` | `FR20_DENOMINATOR_DRIFT` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `public-widened` | `PUBLIC_CONTRACT_WIDENED` | `1` | `PUBLIC_CONTRACT_WIDENED` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `v1-mutated` | `V1_ARTIFACT_DRIFT` | `1` | `V1_ARTIFACT_DRIFT` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `predecessor-disposition-detached` | `V1_ARTIFACT_DRIFT` | `1` | `V1_ARTIFACT_DRIFT` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `execution-skipped` | `TIER_EXECUTION_INCOMPLETE` | `1` | `TIER_EXECUTION_INCOMPLETE` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `execution-not-run` | `TIER_EXECUTION_INCOMPLETE` | `1` | `TIER_EXECUTION_INCOMPLETE` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `execution-empty` | `ASSERTION_LEDGER_EMPTY` | `1` | `ASSERTION_LEDGER_EMPTY` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |
| `execution-regressed` | `EXECUTED_COUNT_REGRESSION` | `1` | `EXECUTED_COUNT_REGRESSION` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` | `742dc0c8e9e78bc51194c2fb871895731dc083d69174043ca0ff126fac24536c` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-9.2-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-9.2-final-record-v2.md` |

## Rollback boundary

remove the portable project and restore every migrated assertion from the before-inventory as one unit; retain Story 9.1 and never change a public contract to simplify rollback.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `10` | `10` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->

### Original-scope successor completion — 2026-10-08

The ten grouped guard corrections are complete and reviewed. Fresh acceptance in the isolated original-source checkout passes AC01–09 and both deterministic AC10 generations at source candidate `1275e5cfb2ec8a77095ec6f097dcdfb62ce43650`: portable 326/326, internal 92/92, all 415 frozen cases plus three controls, and all 22 exact/restored faults. Pair-only publication is `7183038b367863ac769546206b951681042c11b3`; lifecycle commit `19d8f234d04b3ebc7a1fe8d37445803cba0bd403` marks the [isolated spec](../../../conversations-story92-guards-8y20scx2/checkout/_bmad-output/implementation-artifacts/spec-9-2-make-the-portable-tier-structural-and-prove-complete-monotonic-tier-execution.md) done and its own sprint row review. [Post-commit inserted-record verification](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/original-scope-final-insertion.json) passes with `10/10/0/0/0/0`; all 502 candidate-keyed archived files retain exact bytes and times. The [new record](../../../conversations-story92-guards-8y20scx2/checkout/docs/release-evidence/story-9.2-final-record-v2.json) binds the original genuine environment/migration approval unchanged.

All three review layers completed. The [individual triage](../../artifacts/v9/9.2/resume-2026-10-08/guard-corrections/original-scope-review-triage.json) records five pre-existing follow-ups in the deferred-work ledger; none is introduced by this guard successor. Main's 234 focused checks, 33 exact-fault/current-tree checks, and 1072 configured-directory checks remain passing for the frozen guard bytes. The isolated directory lane's single missing-code documentation failure is retained honestly; its minimal runbook correction passes the targeted check.

This main checkout keeps its current accepted pair, original baseline/frozen block, production/dependency bytes, and concurrent work unchanged. Its guard edits and deferred-work additions remain uncommitted. Its spec/sprint row stay in-progress because later main changes are outside the exact original approval; the isolated completion does not authorize or attest that later scope. No Git remote operation or push was performed.

### Current-main build resumption — 2026-10-08

The reviewed implementation and guard corrections are present at committed main `59e72b82cdc2ecc58971e34594c4e8b62896518b`; this resumption found no missing Story 9.2 implementation. Fresh focused tooling passes 207 Story 9.2 checks plus 27 Story 9.1 predecessor checks, all 11 source-model checks, and the sequential 33-case fault/current-tree run (22 exact/restored faults plus 11 current-tree regressions). Current-tree structure and declaration verification pass. Portable Debug and Release builds pass with zero warnings/errors, and their focused AC02 portability controls pass. [Consolidated receipts](../../artifacts/v9/9.2/resume-2026-10-08/current-main-59e72b8-g3c70fax/verification-receipts.json) bind commands, results, and preserved inputs.

Current product builds remain blocked by later main production code. `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Debug` exits `1` with `CS0246` for `SourceNamespaceSnapshotReader` in `EventStoreConversationTenantCatalogue.cs:17`. After a successful separate Release restore, `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore` exits `1` with nine `CS0246` errors for missing publication delivery/projector/entry/status/descriptor/scope types and the snapshot reader. The serialized fallback with `-m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` exits `1` with the same nine errors. [Exact build receipts](../../artifacts/v9/9.2/resume-2026-10-08/current-main-59e72b8-g3c70fax/dotnet-command-receipts.json) and logs retain these failures. Fresh complete tier execution is blocked by the internal build; no stale internal binary supplies acceptance.

Read-only [inserted-record verification of the completed original-scope successor](../../artifacts/v9/9.2/resume-2026-10-08/current-main-59e72b8-g3c70fax/original-successor-insertion.json) exits `0` and retains `10/10/0/0/0/0` at source candidate `1275e5cfb2ec8a77095ec6f097dcdfb62ce43650`, publication `7183038b367863ac769546206b951681042c11b3`, and lifecycle `19d8f234d04b3ebc7a1fe8d37445803cba0bd403`. The original accepted pair, frozen baseline/intent, amendments, migration, Quality approval, and protected predecessor/v1 evidence remain unchanged. Historical main verification still reports `PUBLIC_CONTRACT_WIDENED`, the actual candidate-environment API reports `AUTHORITY_BINDING_INVALID`, and main insertion verification reports `CANDIDATE_NOT_FINAL`/`GITLINK_DRIFT` for later source/environment commits.

The [current-main scope proposal](../../artifacts/v9/9.2/resume-2026-10-08/current-main-59e72b8-g3c70fax/current-main-scope-proposal.md), material digest `852db3f13f442300f71142cdf094b2e4feb54cb77f90ef0a48f4905e4c51c4df`, binds 97 committed changed paths, 20 production paths, nine root gitlinks, and ten owning promotion commits. It remains prepared and unapproved. Concurrent `eng/verify-ext-conv-ai-1.ps1` changes are preserved and excluded. No successor binding, new Quality decision, record publication, dependency/submodule/source correction, or lifecycle completion is claimed. Main's spec and sprint row remain `in-progress`.

### Authorized current-main successor preparation — 2026-10-08

The user replied `yes` to the concrete current-main scope request, authorizing proposal material `852db3f13f442300f71142cdf094b2e4feb54cb77f90ef0a48f4905e4c51c4df` and the exact committed source/environment at `59e72b82cdc2ecc58971e34594c4e8b62896518b`. The [authorization](../planning-artifacts/v9/story-9.2-current-main-scope-authorization-v1.json) binds a byte-identical tracked copy of the reviewed proposal. This supersedes the earlier pending scope status only. The original baseline, frozen block, original migration/Quality approval, accepted pairs, and completed isolated successor remain preserved. Current uncommitted production/test/verification-script edits are excluded. No new Quality decision or acceptance is supplied by this reply.

The authorized deliverable in this phase is a complete, reviewable, versioned successor proposal and the safe preparation tooling needed to reproduce it. Use existing read-only tier verifier APIs and `current_tree=True` to derive approved assertion semantics; bind the actual current production source snapshot separately and explicitly. A proposal is preparation evidence, never acceptance. The unchanged historical generator and its approved artifacts retain their original behavior. Do not widen their accepted scope or suppress source/build/approval blockers. Preparation must work from an isolated clean source snapshot of the exact approved commit rather than consume excluded shared-tree edits. Local detached checkouts and local copies of root-declared dependency sources may be used as needed; no network/remote operations or nested-submodule initialization. Do not edit production code, dependencies, submodule contents, original records, or excluded user work to obtain a passing build. Keep an exact blocked-build receipt if the approved source cannot compile; this cannot block preparing the complete proposal. Any changed migration proposal, row, or public-drift bindings require a later genuine Quality decision on concrete digests.

**Successor preparation tasks:**
- [x] `_bmad/scripts/prepare_story92_successor.py` — load the exact closed authorization/proposal; verify their pinned digests and original identities; remeasure all 97 source changes, nine root gitlinks, ten owning promotions, and protected evidence against Git. Reject wrong commits, missing/altered authorization, source/environment drift including reverted changes, or excluded production/test dirt. Use an explicit contained versioned output directory; reject aliases/collisions with all retained evidence and executable/configuration inputs before writing.
- [x] `_bmad/schemas/story-9.2-current-main-successor-proposal-v1.schema.json` — define a closed versioned preparation document binding approved scope, original accepted record/migration/approval, the actual current source snapshot and portable compile surface, exact retained assertion strengths and changed-row/public-drift digests, proposed successor material, required later Quality bindings, and truthful blocked/available build evidence. It must never claim Quality approval or passing acceptance.
- [x] `docs/release-evidence/conformance-oracle-tiering-migration-v4-proposal.json` and `docs/release-evidence/conformance-oracle-tiering-migration-review-v4.md` — generate a deterministic pending successor from the isolated approved source, retaining every original row and before-strength binding. Explain source/environment changes, whether row/public-drift digests changed, exact required Quality bindings, frozen 415-case floor and three controls, measured build blockers, and all preserved predecessors. Never replace any v3 or accepted record.
- [x] `_bmad/scripts/tests/test_prepare_story92_successor.py` — add hermetic meaningful tests for valid authorization and deterministic output, altered/missing/unapproved scope, wrong candidate and extra/reverted source/gitlink changes, protected artifact mutation, source-input dirt, failed build evidence, synthetic approval/acceptance rejection, and output collision/alias protection. Each mutation preserves/restores inputs; the repository directory lane must collect these tests without .NET or submodule prerequisites.
- [x] `docs/runbooks/story-final-record-generation.md` and this spec — document the actual authorized preparation command, exact output/review/Quality requirements, preserved historical behavior, and remaining acceptance/build blockers. Run the focused new tests and appropriate compatibility checks; retain command and output digests. Mark only completed preparation tasks, leaving main lifecycle in-progress until genuine Quality approval and complete fresh acceptance.

**Preparation acceptance:** Given the actual scope authorization and exact approved source checkout, when the preparation command runs twice, then the pending JSON/Markdown reproduce byte-identically and bind the approved scope plus all current measured inputs. Given any missing/altered authorization, unauthorized source/environment change, protected-input mutation, synthetic acceptance, or output alias, when preparation is attempted, then it rejects with a specific blocker and preserves input bytes. Given a failed build at the approved source, when the proposal is prepared, then the failure is retained as blocked acceptance and no passing execution is invented. Finish all reversible preparation, tests, and the concrete review packet before requesting the later Quality decision.


### Current-main preparation completed — 2026-10-08

All five authorized preparation tasks are complete. The [pending v4 proposal](../../docs/release-evidence/conformance-oracle-tiering-migration-v4-proposal.json) and [Quality review packet](../../docs/release-evidence/conformance-oracle-tiering-migration-review-v4.md) bind exact source `59e72b82cdc2ecc58971e34594c4e8b62896518b`, approved scope `852db3f13f442300f71142cdf094b2e4feb54cb77f90ef0a48f4905e4c51c4df`, successor material `4ac1299fb3ee707b94e6ea3b331ebc99e84e9ecfd869e69b89092c511f52d296`, and proposed migration `606a7e57dafb2084101e85c4003c93bf602643cf2a3f1265c1275424cb050fb4`. Independent schema/material checks preserve all 459 retained rows, original before-strength bindings, exact successor strengths, FR-20 membership, execution policy, all 14 changed-row hashes, the public-drift digest, and 20 protected original artifacts. The actual current production snapshot is bound separately from the historical freeze.

The final 62 hermetic preparation tests pass with zero failures/errors/skips. The configured repository tooling lane passes all 1,133 checks with zero failures/errors/skips; its 56 existing JUnit-property warnings remain visible. A final narrow cache-source guard correction is covered by the complete 62-case focused rerun. Two preparations and both tracked review copies are byte-identical: JSON SHA-256 `8aea6446a2f10d308284580cbc11ab4d90268ef2bc69e7e140f6d9300a4eb95d`, Markdown SHA-256 `57ee42ba8c45a4e09414d4f94ce13c68cf57413f884f738e585681caa19b6665`. [Consolidated preparation receipts](../../artifacts/v9/9.2/current-main-successor-v2/verification-receipts.json) bind commands, final tool/test bytes, output hashes, schema/material audit, preservation, and deterministic replay. The five disposable-fixture commit messages pass the owning pinned commitlint 21.2.3 policy; no owning-repository commit, staging, push, remote operation, dependency update, or nested-submodule initialization was performed.

The clean approved-source checkout is `/tmp/story92-approved-source-ofz1orw9/checkout`. Both local-cache Release restores and the portable Release build pass. `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore` and its serialized fallback each exit `1` with nine `CS0246` diagnostics for seven missing publication/snapshot types. The [measured build receipt](../../artifacts/v9/9.2/current-main-successor-v2/build-evidence.json) preserves those failures. The original provisional v1 rendering and measurement remain intact; [relocation provenance](../../artifacts/v9/9.2/current-main-successor-v2/build-evidence-provenance.json) records exact log copies and receipt-path rebinding for the corrected v2 rendering, with no fresh-build claim. No stale internal binary supplies execution acceptance.

Concurrent production/test/verification edits remain excluded and preserved. The original baseline, frozen block, historical generator behavior, original migration/Quality decision, accepted pairs, and completed isolated original-scope successor are unchanged. The new packet is pending evidence only. Main's spec and sprint row remain `in-progress`; genuine Quality approval of its exact complete binding, a compatible successor route, resolution of the source/build blocker, and fresh complete acceptance remain outstanding.

### EventStore repair authorization — 2026-10-09

The user selected **"Expand scope and repair EventStore"** in response to the concrete build blocker. This renegotiates the frozen production/dependency boundary only as needed to make the owning EventStore publication and authenticated namespace snapshot APIs available to Conversations and to refresh Story 9.2 successor evidence. Preserve the original baseline, frozen assertion definitions, historical migrations/approvals, accepted records, v4 pending proposal, and all unrelated source. This answer is a scope authorization, not a Quality approval of a new migration digest or permission to claim acceptance without fresh execution.

The current main Debug conformance build fails on missing `SourceNamespaceSnapshotReader`; the EventStore checkout contains no implementation of that type. After a Release-specific restore, the current Release build fails with 13 `CS0246` references to source publication and snapshot types because the pinned 3.117.1 package surface lags the checked-out source. The earlier approved-source receipt with nine errors remains historical; these are current-main measurements.

**Repair and successor tasks:**
- [x] `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Streams/SourceNamespaceSnapshotReader.cs` and a separate result type file — implement a bounded, cancellation-aware, fail-closed authenticated finite-cut fold over `ISourcePublicationNamespaceSource` and `IAuthoritativeEventStreamReader`. Verify scope, complete current cut, every exact source prefix, and an unchanged final cut before returning any values. Keep each C# type in its own file.
- [x] `references/Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests/Streams/SourceNamespaceSnapshotReaderTests.cs` — cover success, missing/incomplete/foreign/stale/changed cut, missing source prefix, fold failure, timeout and caller cancellation; run the focused tests.
- [x] Owning EventStore build/package route and Conversations Release resolution — verify the new API with local project references and determine the smallest valid package promotion path for Release. Do not substitute a Debug binary or stale package for a passing release.
- [x] This spec and versioned Story 9.2 successor evidence — record exact source/environment change, measured builds, changed row/public-drift digests, and a concrete later Quality review packet. Preserve the v4 proposal byte-for-byte and request a genuine Quality decision on the new exact material before final AC01–10.

**Repair acceptance:** Given a complete authorized finite namespace cut and exact source prefixes, when the reader folds, then it returns only values covered by the unchanged current cut. Given missing, foreign, stale, changed, over-bound, cancelled, or unreadable input, when it reads, then it returns no partial values or propagates the caller's cancellation. Given the repaired source and an available package path, when both conformance tiers build and execute, then Story 9.2 still requires all 415 frozen cases, three live controls, exact fault restoration, and candidate-bound final-record verification before `done`.

### EventStore repair preparation — 2026-10-09

The owning EventStore repair adds `SourceNamespaceSnapshotReader` and a separate result type, plus the `Pending` delivery status needed by the existing Conversations pump. Its 21 focused cases pass with zero failed/skipped/not-run. EventStore client and test projects build with zero warnings/errors. Conversations internal conformance builds with source references in Debug and Release, also with zero warnings/errors. A disposable local Release NuGet trial packs the exact four-package conformance graph (Contracts, Client, ServiceDefaults, DomainService) at `3.117.2-story92.2`; the package-reference Release build passes. This is a package-boundary proof, not an official package promotion or a fresh AC01–10 execution.

The [versioned repair proposal](../../docs/release-evidence/conformance-oracle-tiering-eventstore-repair-v5-proposal.json) and [Quality review packet](../../docs/release-evidence/conformance-oracle-tiering-eventstore-repair-review-v5.md) bind the four source files, local package hashes, commands/results, and unchanged 14 changed-row digests. The unchanged **Conversations public drift** digest `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1` excludes referenced EventStore packages. The EventStore.Client repair independently adds public `SourceNamespaceSnapshotReader`, `SourceNamespaceSnapshot<T>`, and `SourcePublicationDeliveryStatus.Pending = 3`; the packet binds their exact exported declarations (SHA-256 `df5445e94ae3e17e2b8ddb40a17ba0665e869f91987b58283816cccb94420078`) and the candidate assembly. Compared with published 3.117.1, the Client package changes 17 public type names, including publication APIs that predate this repair in the checkout; complete package API review remains required. The pending v4 proposal retains its exact bytes. The repair remains uncommitted; the root gitlink and central released package pin remain unchanged. The packet requests a later genuine Quality decision on both the clean promoted successor material and its EventStore API expansion. Story 9.2 remains `in-progress` pending official package promotion, clean committed successor measurement, Quality approval, complete tier execution, fault restoration, and candidate-bound final-record verification.
