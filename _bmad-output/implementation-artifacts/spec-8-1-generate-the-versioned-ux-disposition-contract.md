---
title: 'Generate the versioned UX disposition contract'
type: 'feature'
created: '2026-10-02'
status: 'in-progress'
baseline_commit: '92638b8a2d48f12626db13afcb1f45554cbf3683'
route: 'dispatch'
review_loop_iteration: 1
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-8-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No candidate-bound bundle exposes the canonical UX sources' 52 decisions and 28 acceptance criteria. The final-record gate rejects Story 8.1's Python and xUnit commands.

**Approach:** Extend the existing gate, generate a closed schema/JSON/Markdown bundle from the two UX sources, and prove inventory, provenance, and non-activation before recording Story 8.1.

## Boundaries & Constraints

**Always:** Use the exact `8.1.json` commands and frozen inventory. Bind `SC-8.1`, Story 7.4's digest, both source paths/versions/hashes, and ordered 52/28 IDs. Use schema `hexalith.conversations.ux-preservation-disposition.v1` with the PRD's required fields. Set every status to `preserved-not-activated`, label historical mappings non-current, and pass the current-change and insertion gates.

**Never:** Edit UX sources, historical mappings, production UI/runtime, gitlinks, accepted Story 7 records, published contracts, or the v1–v8 prefix. Do not activate UX, claim a historical hold lift or release authorization, or import Story 8.2's mutation matrix.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Expected behavior | Blocker |
| --- | --- | --- | --- |
| Valid | Bound sources, 52/28 rows, Story 7.4 | Digest-matched bundle; record `7/7/0/0/0/0` | None |
| Drift | Missing/changed source or ID | Reject | Source or inventory drift code |
| Activation | Missing banner, activated row, invalid current story | Reject | `UX_ACTIVATION_UNAUTHORIZED` / `UX_CURRENT_STORY_INVALID` |
| UI change | Production UI path in candidate | Reject | `UX_PRODUCTION_CHANGE_FORBIDDEN` |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v9/story-contracts/8.1.json`, `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:2550`: frozen contract and blockers.
- `_bmad-output/planning-artifacts/ux-requirement-map.md:25`, `_bmad-output/planning-artifacts/ux-design-specification.md:1323`: ordered inventory and source text; read only.
- `_bmad/scripts/publish_v9_planning_authority.py:187`, `_bmad/scripts/generate_preservation_traceability_manifest.py:432`: frozen IDs/parity and extraction patterns.
- `_bmad/scripts/generate_story_record.py:2799,5532,5830`: replace Story 7 retention and Python/xUnit rejection with contract-derived behavior; `_bmad/schemas/story-final-record-v2.schema.json` owns record shape.
- `.github/workflows/ci.yml:65` and governed routes contain the gate and post-`done` verification. Tooling lane passed 572 tests; A-2/A-3 tracking awaits proof.
- `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs:247` forbids future outputs; scope the assertion to its historical candidate.
- `docs/release-evidence/story-7.4-final-record-v2.json`: immutable predecessor.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — finish A-1: contract-derived retention/facts, Python/xUnit support, candidate/build and output binding, gitlink-only staleness fault. Execute AC-8.1-01's exact CLI before recording PASS; require explicit passing machine verdicts for other Python outputs, nonzero executed TRX counts, and a supported result route for declared solution builds. Retain the actual story status while masking retrospective rows.
- [x] `_bmad/scripts/generate_ux_preservation_disposition.py`, `docs/release-evidence/ux-preservation-disposition-v1.schema.json` — derive closed, deterministic, source-bound dispositions and blockers. Verify every authority field and the predecessor PASS record/pair; reject malformed authority shape with `UX_SCHEMA_INVALID`. Write the three files as one recoverable transaction. Use resolvable source anchors and render all required row fields to Markdown.
- [x] `_bmad/scripts/tests/test_generate_ux_preservation_disposition.py` — cover deterministic output, exact CLI exit and three output bytes, drift, activation, malformed authority, partial-write rollback, and fixture restoration.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs`, `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` — implement AC-02–06 and preserve historical assertions. Compare every decision and acceptance rationale and historical mapping with its canonical source row. Reject candidate paths outside the explicit Story 8.1 planning, tooling, tests, and evidence file set, including runtime configuration and workflow paths.
- [x] `docs/release-evidence/{ux-preservation-disposition-v1,story-8.1-final-record-v2}.{json,md}`, `_bmad-output/implementation-artifacts/spec-8-1-generate-the-versioned-ux-disposition-contract.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` — produce evidence, insert the record, and close A-1–A-3 tracking only on proof. The final gate compares candidate schema, every disposition row and authority field, and exact rendered Markdown with deterministic derivation from the canonical committed inputs.

**Acceptance Criteria:**

- Given bound sources, when AC-8.1-01 runs twice, then the closed schema/JSON/Markdown bytes and digest match.
- Given frozen inventories, when AC-8.1-02–04 run, then source bindings and ordered 52/28 complete rows pass.
- Given preserved rows and `SC-8.1`, when AC-8.1-05–06 run, then mappings stay non-current and UI changes fail.
- Given six passing results and Story 7.4, when AC-8.1-07 runs, then the record binds all required inputs with `7/7/0/0/0/0`.

### Review Findings

Review 3 (2026-10-02; baseline `92638b8`; reviewed `92638b8..35121ca`, scoped to the generators, both pytest suites, both C# tests, both schemas, and the runbook; record pair retracted in `518df28`). Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; none failed.

- [x] [Review][Patch] Successor routes reject lifecycle-only descendants on retained reruns — resolved from decision (Option 1, 2026-10-03): pass `stamped` into `v2_successor_scenario_from_results` and accept `{candidate} ∪ stamped` for the xUnit assembly stamp, built-DLL stamp, and Python output `candidate`; leave Story 8.1's pinned-TRX route unchanged. A clean review reruns the gate at the retained candidate, so 9.2/10.1/11.2 builds and 10.3–16.3 `--candidate HEAD` outputs otherwise fail deterministically [_bmad/scripts/generate_story_record.py:5774,5859,5896] (A9, E6, E7, E16, B8)
- [x] [Review][Patch] Final gate reports `UX_SCHEMA_INVALID` instead of the owning blocker for an activated row, missing banner, 51-row inventory, or current mapping — the candidate-schema check runs before the activation/inventory checks, which the closed schema makes unreachable; the runbook's `UX_ACTIVATION_UNAUTHORIZED` row also promises the non-current case. Classify activation/inventory/ownership before generic schema failure, test through `v2_ux_facts` with committed mutations, and align the runbook row [_bmad/scripts/generate_story_record.py:6014] (A1, E28)
- [x] [Review][Patch] Production-path pytest is bound to live HEAD — it clones `HEAD`, so after Story 8.2's first commit the path guard fires without the injected file, and a later UX-source edit stops it at `UX_SOURCE_DRIFT`; there is no no-injection control. Check out the recorded Story 8.1 candidate and add a control run [_bmad/scripts/tests/test_generate_story_record.py:7134] (B13, A2)
- [x] [Review][Patch] Successor lifecycle retention is untested — no test covers `v2_retention_config` (0/1/2 specs), the task-checkbox mask (flip accepted, other Tasks edit rejected), or the 8-1 retro mask (open→done accepted, done→open rejected) [_bmad/scripts/generate_story_record.py:4739,4603,4690] (G1, B9)
- [x] [Review][Patch] The Story 8.1 record pair is never re-verified by a test — nothing asserts `v2_verify_pair == []`, record-schema validity, the AC-8.1-07 self-ledger, or the `storyId == 8.1 ⇔ uxDisposition` rule; Story 8.2's predecessor check depends on all four [_bmad/schemas/story-final-record-v2.schema.json:301] (G2)
- [x] [Review][Patch] `v2_ux_facts` assembly binding and derivation parity are never exercised — the only caller test stops at the path guard; add other-commit/older assembly (`TEST_RESULTS_STALE`) and edited committed row (parity code) cases [_bmad/scripts/generate_story_record.py:6056] (G3)
- [x] [Review][Patch] No test ties the committed disposition bundle to a fresh derivation — add `generate(ROOT, CONTRACT_PATH)` equals the three committed files; the parity test passes committed bytes as both inputs [_bmad/scripts/tests/test_generate_ux_preservation_disposition.py:70] (G4, B12)
- [x] [Review][Patch] Successor commands execute after the gate has already failed and before their own preconditions — the successor loop lacks the Story 8.1 `not findings` guard, and `subprocess.run` precedes the committed-script/project and `--scenario` checks, so a dirty tree still runs builds and uncommitted scripts [_bmad/scripts/generate_story_record.py:6516,5756] (B1)
- [x] [Review][Patch] `--verify-inserted-record` designated-pair check is tautological for stories ≥ 8 — `expected_outputs` is read from the same `finalRecord.paths` it is compared with; derive it from the `docs/release-evidence/story-<id>-final-record-v2.{json,md}` convention [_bmad/scripts/generate_story_record.py:6750] (B3, G6)
- [x] [Review][Patch] `v2_authority` binds a reconstructed contract path instead of the evaluated `--contract` bytes [_bmad/scripts/generate_story_record.py:3441] (B4, E1)
- [x] [Review][Patch] C# AC-8.1-06 diff uses rename detection — add `--no-renames` to match `committed_path_status` [tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs:210] (B6, A3, E24)
- [x] [Review][Patch] `permitted_early` accepts the generator at any position when the last scenario is `python_check`; require `position == len(scenarios) - 2` as its message states [_bmad/scripts/generate_story_record.py:6329] (B10, E17)
- [x] [Review][Patch] Task-checkbox mask misses `[X]` and indented sub-task checkboxes [_bmad/scripts/generate_story_record.py:4609] (E3, B9)
- [x] [Review][Patch] Runbook documents Story 8.1 blockers but not its procedure or the generic successor routes — add the candidate-stamped build prerequisite, pinned-TRX rerun behavior, `xunit`/`python`/`python_check`/`build`/`restore` routes, `outputFiles`, V14 contract acceptance, and the rerun rule (successors: rebuild and rerun every command at HEAD before a retained rerun; Story 8.1: no Conformance rebuild between the record commit and a clean-review rerun) [docs/runbooks/story-final-record-generation.md:1171] (B16)
- [x] [Review][Patch] Dead `subprocess.check_output` stub in `source_fixture` — `generate()` never calls it [_bmad/scripts/tests/test_generate_ux_preservation_disposition.py:38] (B12)
- [x] [Review][Patch] Locked no-op restore leaves `project.assets.json` older than the candidate, so Story 11.2's `AC-11.2-02` reports `TEST_RESULTS_STALE` after a successful exact restore; the zero-exit execution, not the asset mtime, should prove currency [_bmad/scripts/generate_story_record.py:5913] (E14)
- [x] [Review][Patch] `python_check` route and the story-≥8 early-generator exception are untested — add committed-script mismatch, nonzero exit, and misplaced-generator cases [_bmad/scripts/generate_story_record.py:5796] (G5)
- [x] [Review][Defer] xUnit `-method` selector rejects a `[Theory]` whose TRX `testName` carries an argument suffix [_bmad/scripts/generate_story_record.py:5789] — deferred: unverified medium; settle by checking whether any declared `-method` selector in contracts 8–16 targets a `[Theory]` once those tests exist
- [x] [Review][Defer] Successor Python verdict falls back to a domain `status` field and treats non-verdict values as failure [_bmad/scripts/generate_story_record.py:5835] — deferred: unverified medium; settle by checking whether any successor generator's declared JSON output carries a non-verdict `status` once those generators exist (E13, B14)
- [x] [Review][Defer] Solution build output paths assume forward-slash `Path`, default `AssemblyName`, and `net10.0` [_bmad/scripts/generate_story_record.py:5896] — deferred: unverified medium; settle against Story 11.2's fixture solution once it exists (E15)

Ledger transfer pending: `_bmad-output/implementation-artifacts/deferred-work.md` is outside the Story 8.1 candidate path set (`V2_8_1_ALLOWED_PATHS`, `AllowedCandidatePaths`). Committing it before the record fails AC-8.1-06 and the final gate with `UX_PRODUCTION_CHANGE_FORBIDDEN`, and committing it after the record breaks candidate retention. Append the three deferrals above to that ledger once Story 8.1 reaches `done`.

Rejected:

- `false` E5 — the spec template always carries both `Tasks & Acceptance` and `Implementation Notes` headings, so the fail-closed branch is unreachable for template specs.
- `false` E10 — the gate executes the exact declared `python3` command, and the gate itself is declared as `python3 …`, so both resolve the same interpreter.
- `false` E20, E21, E25 — both sources are byte-pinned to the Story 7.4 candidate before versions are read; their single, unquoted, non-empty version keys cannot change without `UX_SOURCE_DRIFT`.
- `false` B12 (mock) — `source_fixture` stubs only the `git show` call; the drift comparison still runs and the mutation tests reach `UX_SOURCE_DRIFT`.
- `false` B9 (retro and record region) — the spec requires Story 8.1 to close retro items 30–32, and successor records are inserted into their specs as Story 8.1's is.
- `low` B2, E9 — the gate leaves rewritten evidence after a failed rerun; for Story 8.1 this needs an interpreter mismatch after parity passed, successor outputs are declared dirt so the real `STALE` repeats, and snapshot/restore adds complexity.
- `low` B5, A4 — moving `baseline_commit` forward requires a deliberate frontmatter edit; pinning it adds a guard.
- `low` B6 (triplicated allowlist) — the three lists are frozen for this story; a sync test adds machinery.
- `low` A3 (C# negative test) — the Python gate proves both forbidden-path kinds; a C# fixture repository adds machinery.
- `low` B7, A5, E27 — CI uses `fetch-depth: 0`, sibling tests read the same commit through `ReadCandidateBytes`, and CI excludes the class.
- `low` B8, E8 — missing pinned TRX fails by spec design; no consumer regenerates the record from a fresh clone (CI does not run AC-8.1-07 and `--verify-inserted-record` verifies the pair only).
- `low` B9, E4 — unchecking a completed task after the candidate is unlikely; rejecting it adds state tracking.
- `low` B9, E2 — a second untracked `spec-<m>-<n>-*.md` draft is unlikely; listing the committed tree adds plumbing.
- `low` B11 — the schemas do not pin inventory content, but the gate's byte parity and the C# row checks do, Story 8.2 owns the zero-gap validator, and a schema change forces evidence regeneration.
- `low` A10 — Markdown renders only the first mapping, but derivation always emits exactly one and byte parity enforces it.
- `low` B15, E11, E19, E23 — tracebacks need unreadable files, missing `git`, or a missing Story 7.4 candidate object; the double module load costs only time; rollback swallowing was already rejected in Review 2.
- `low` A6 — authority-input and write-failure blocker labels and the unused exit `2` matter only when canonical inputs are missing or the disk fails.
- `low` A7 — the process-level AC-8.1-01 run is measured by the final gate; a subprocess test needs a real Git fixture.
- `low` A8 — the C# selectors fail generically only on failure, which the record maps to `TEST_FAILED`; adding codes touches every assertion.
- `low` E18 — the extra `RECORD_NOT_DERIVED` blocker is accurate, and the primary blocker is still reported.
- `low` E22 — output paths are fixed constants under an existing `docs/release-evidence`.
- `low` E26 — the Git commands used write little to stderr.

## Implementation Notes

**KEEP across review derivation:** Preserve the ordered 52/28 extraction from the two unchanged canonical UX files; the `preserved-not-activated` state and non-current historical mapping; Story 7.4's existing record bytes; exact xUnit selectors with candidate-stamped build/TRX evidence; root gitlink checks; the negative production-path fixture; and the deterministic JSON/Markdown digest pair. Do not change the frozen intent, historical UX mappings, product UI/runtime, or v1-v8 prefix.

**Story 8.1 candidate path set:** allow only the exact files named in the execution tasks plus the Epic 8 context file and `docs/runbooks/story-final-record-generation.md`. The whitelist applies to the baseline-to-candidate committed path set in both the C# AC-8.1-06 selector and final-record gate. Test a committed non-`src/` runtime or workflow path as well as a committed `src/` path; both must produce `UX_PRODUCTION_CHANGE_FORBIDDEN`.

**Disposition parity:** Derive the canonical schema, all 80 rows, authority fields, and Markdown from the committed contract, authority bundle, predecessor, and source bytes. The final gate compares candidate output bytes against that derivation before measuring AC-8.1-01. One valid-looking but wrong rationale, mapping, schema, authority digest, or Markdown row must fail with the owning drift/binding blocker. Record a passing AC-8.1-01 only after its exact command exits zero and reproduces all three committed output bytes.

## Spec Change Log

- Review iteration 1 (2026-10-02): Blind Hunter, Edge Case Hunter, and Verification Gap found that the prior implementation could report AC-8.1-01 without running its CLI, accept changed disposition text/schema/authority/Markdown, leave partial bundle writes, accept forbidden non-`src/` paths, and mishandle successor status/TRX/Python/solution-build states. Amended non-frozen execution tasks, implementation notes, and verification to require measured commands, full canonical parity, transactional output, exact path scope, and those successor cases. Avoid the known-bad state of a 7/7 record whose files exist but do not prove the declared behavior. KEEP the 52/28 source extraction, preservation state, immutable predecessor, candidate-stamped xUnit evidence, root gitlink binding, and deterministic digest pair.

## Review Triage Log

Review 1 (2026-10-02; baseline `92638b8`; candidate `5c9afe8`). Each reviewer finding is recorded separately before grouping. `B` is Blind Hunter, `G` Verification Gap, and `E` Edge Case Hunter.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 — AC-8.1-01 reports a fabricated pass | medium | bad_spec | `v2_ux_scenario_from_results` returns seven static PASS rows without executing the declared CLI; a broken `main()` can still yield a passing final record. |
| B2 — disposition lacks the candidate commit ID | false | reject | The approved canonical candidate rule intentionally binds the pre-candidate disposition by rule and the final record binds its digest to the committed candidate; embedding the later commit in its own input would be circular. |
| B3 — candidate schema can be weakened | medium | bad_spec | `v2_ux_facts` validates against the candidate's companion schema but never compares that schema to the canonical generated shape. |
| B4 — row obligations can be altered | high | bad_spec | The final gate checks IDs, status, owner and source hashes, but does not compare rationale or mappings against the source rows; a false UX obligation can pass. |
| B5 — Markdown omits required row fields | low | patch | `markdown()` renders only six columns; evidence/control, compatibility and disclosure-safety are absent from the reviewer-facing projection. |
| B6 — bundle writes can be partial | medium | bad_spec | `main()` writes schema, JSON and Markdown sequentially without rollback; a later write failure leaves mixed output bytes. |
| B7 — generator authority is only partly checked | medium | bad_spec | `generate()` compares planning candidate but copies other authority fields without independently verifying the bundle and inventory identities. |
| B8 — malformed authority shape raises a traceback | low | patch | `generate()` catches JSON parse errors but not missing nested keys or wrong object types before indexing `contract['authority']`. |
| B9 — candidate scope guard covers only `src/` | medium | bad_spec | Both AC-8.1-06 and `v2_ux_facts` accept a committed runtime configuration or workflow path outside `src/`, although the contract limits the candidate to Story 8.1 planning, tests and evidence. |
| B10 — evidence/control anchors do not resolve | low | patch | Generated fragments such as `#UX-Decision-Inventory:Design system foundation` are not headings or ID anchors in the canonical map. |
| B11 — final record has no fault-injection ledger | false | reject | Story 8.1 requires five negative fixtures and byte restoration, but its frozen AC-8.1-07 does not require a mutation ledger in the final record; the focused tests execute the required faults. |
| B12 — standalone generator trusts an incomplete predecessor | medium | bad_spec | `generate()` checks predecessor ID and candidate syntax but not its PASS status and cross-bound record pair before emitting a PASS bundle. |
| G1 — exact generator CLI has no passing test | medium | bad_spec | Pre-verified: tests call `generate()` and check command recognition, while the final gate can pass from committed files when `main()` fails. Same root as B1. |
| G2 — disposition text lacks source comparison | high | bad_spec | Pre-verified: Python and C# tests assert IDs, counts and hashes, but no row rationale or historical reference equality. Same root as B4. |
| G3 — authority fields are not fully bound | medium | bad_spec | Pre-verified: an all-zero bundle digest remains schema-valid and passes the current final-record checks. Same root as B7. |
| G4 — sprint status variable is overwritten | medium | patch | `without_status()` replaces the story-status match while scanning retro rows and returns the final retro status, so a valid status-only transition can be rejected. |
| G5 — declared solution build cannot pass | medium | bad_spec | The successor classifier accepts `.slnx` build commands such as AC-11.2-03, but the result handler rejects every build target that is not `.csproj`. |
| E1 — status transition rejected after retro closure | medium | patch | The retro loop overwrites the story match at `generate_story_record.py:4685`; same root as G4. |
| E2 — empty mappings pass a weakened schema | medium | bad_spec | Candidate schema validation has no canonical-schema comparison, and empty mappings evade the C# loop; same root as B3. |
| E3 — Markdown can contradict JSON | medium | bad_spec | The final gate checks the supplied Markdown digest but does not compare its bytes with a deterministic rendering of the authoritative JSON. |
| E4 — false authority metadata passes | medium | bad_spec | `v2_ux_facts` compares inventory and contract digests but not the other authority fields; same root as B7. |
| E5 — zero executed successor TRX can pass | medium | patch | `count_disagreements` checks executed only as a range, and the successor path accepts one passed row with `executed=0`. |
| E6 — successor Python output can omit verdict | medium | bad_spec | Non-`verify_` Python outputs may lack `result`, `status` and `exitCode`; the gate still records their existence as a PASS. |
| E7 — missing authority object causes traceback | low | patch | A parseable contract without `authority` reaches unchecked dictionary indexing; same root as B8. |
| E8 — six passing results claim is unmeasured | medium | bad_spec | The AC-8.1-01 bundle branch returns PASS from committed files without proving the declared command ran; same root as B1. |

Surviving bad-spec groups: exact scenario measurement (B1/G1/E8, E6), canonical disposition and schema parity (B3/B4/G2/E2/E3), full authority/predecessor binding (B7/B12/G3/E4), candidate path boundary (B9), coherent bundle writes (B6), and declared successor build support (G5). These precede patch groups under the review workflow.

Review 2 (2026-10-02; candidate `76a3c30`). All three layers reported before triage. `B2`, `G2`, and `E2` below name this review's reviewers, independent of Review 1 IDs.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B2-1 — Story 8.2 reruns a HEAD-based Story 8.1 scope check | medium | patch | AC-8.2-01 selects the whole class, and the current AC-8.1-06 method diffs baseline to HEAD; future Story 8.2 files would fail. Pin the check to Story 8.1's recorded candidate once available. |
| B2-2 — generic successor lacks predecessor record digests | medium | reject: future-story scope | The generic route records only contract predecessor IDs; Story 8.1's own 7.4 pair is verified and bound by `uxDisposition`. Story 8.2's separate final-record binding belongs to its explicit successor work, which the frozen Story 8.1 scope excludes. |
| B2-3 — generic pytest route does not inspect Story 8.2 mutation blockers | medium | reject: future-story scope | The route parses JUnit pass and nonempty assertions; the exact Story 8.2 mutation matrix is expressly excluded from this build's frozen intent and will need Story 8.2-specific result validation. |
| B2-4 — generic successor Python command is not executed | medium | patch | `v2_successor_scenario_from_results` accepts a stale PASS JSON without running the declared script; the new route should require its actual zero exit. |
| B2-5 — generic Python PASS ignores failing counts | medium | patch | The route checks top-level verdict but does not reject failed, skipped or not-run counts in a reported summary, allowing a contradictory PASS. |
| B2-6 — build can pass from an old Release DLL | medium | patch | The generic build branch reads `bin/Release` without executing the declared command or honoring configuration; a failed or Debug build can be mislabeled PASS. |
| B2-7 — restore can pass from old assets | medium | patch | The generic restore branch reads assets without executing the locked restore; an old asset file can mask exit 1. Same root as B2-6. |
| B2-8 — copied old TRX can look current | medium | patch | AC-8.1-02–06 rely on assembly identity and file times; rerunning each exact selector at record generation closes the copied-result case. |
| B2-9 — TRX and binary are untracked | false | reject | The final-record runbook explicitly allows declared TRX inputs outside Git and binds their hashes plus candidate-stamped assembly; a fresh checkout reproduces rather than inherits test artifacts. |
| B2-10 — rollback can also fail | low | reject | A persistent filesystem failure can prevent restoration after a replacement error; final-record parity rejects any mixed bundle, and adding another recovery path for persistent I/O failure is disproportionate for normal use. |
| B2-11 — generated files become mode 0600 | low | patch | `NamedTemporaryFile` is installed by `os.replace`, and the three local evidence files are currently mode 0600; set normal readable mode before replacement. |
| B2-12 — schema pattern accepts `../` | low | patch | The `path` regex admits parent traversal even though only normalized repository-relative paths are intended; tighten the schema directly. |
| G2-1 — failed build/restore can retain PASS | medium | patch | Pre-verified: fixture artifacts satisfy the generic gate without the exact command's exit result; same root as B2-6/7. |
| G2-2 — failed Python verifier can retain PASS | medium | patch | Pre-verified: the generic Python branch reads a prior PASS receipt without executing the declared verifier; same root as B2-4. |
| E2-1 — restoration errors are swallowed | low | reject | The `except OSError: pass` path can leave mixed local files under persistent I/O failure; same rare condition as B2-10, while the final gate still fails closed. |
| E2-2 — schema JSON has no verdict | medium | patch | Story 9.1's declared generator emits a `.schema.json` without result/exitCode, but the generic Python branch currently requires one; exempt schema documents while verifying the exact command and its substantive result. |
| E2-3 — failed successor Python command retains PASS | medium | patch | A prior JSON receipt is sufficient in the current generic branch; same root as B2-4. |
| E2-4 — failed successor build retains PASS | medium | patch | Prior candidate-stamped DLLs are sufficient in the current generic branch; same root as B2-6. |
| E2-5 — candidateBinding and sectionSha256 are unchecked | false | reject | Those descriptive fields are inside the immutable published contract, whose entire bytes must match its authority-bundle artifact hash; this Story 8.1 candidate cannot alter the contract path under its allowed-path guard. |

Patch groups: pin Story 8.1 conformance to its candidate (B2-1); execute exact successor Python/build/restore commands and inspect their outcomes (B2-4–7, G2-1/2, E2-2–4); rerun exact Story 8.1 xUnit selectors (B2-8); preserve generated file permissions and normalize schema paths (B2-11/12). No intent-gap or bad-spec entry survives this review.

Post-review verification (2026-10-02): rerunning AC-8.1-07 after generation produced new TRX bytes and changed both final-record files even though the candidate and test outcomes were unchanged. This is a medium patch to the B2-8 implementation: first generation executes the exact selectors, while a valid committed pair reuses only its matching TRX evidence for a stable record-only successor rerun; missing or changed pinned evidence fails. The generator and focused tests were patched before the final gate.

## Verification

**Commands:**

- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests` — current tooling lane passes without failed or errored tests.
- CI's `verify_story_completion_workflows.py` AC-7.3-01/02 commands — both pass; post-`done` route checks remain present.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release` — new selectors compile.
- The seven exact commands in `8.1.json` — each exits `0` with nonempty passing evidence; run the generator twice and compare output bytes.
- Negative fixtures must prove wrong row text/mapping, weakened schema, wrong authority digest, altered Markdown with recomputed digest, malformed authority, partial write failure, zero-executed TRX, missing Python verdict, and non-`src/` forbidden candidate path are rejected. CLI and all fixture restoration tests run and pass.
- `python3 scripts/check-root-submodules.py --repository .` — root declaration and gitlink invariants pass.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 8.1 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `8.1`
- Candidate: `b9859097d6bca9e9ed315313a4ffaa5e2cef157f`
- JSON content SHA-256 (all three digest fields zeroed): `d6b33c43ce6f1f4847ce256fbc2749e94226b346bbe270aa17ba1de4f831e43b`

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
| `references/Hexalith.Builds` | `160000` | `3734acbb14d30d06cea401bb26f40b0017702620` |
| `references/Hexalith.Commons` | `160000` | `c13dc6679aa91144b6d541078f3f20019d79c2eb` |
| `references/Hexalith.EventStore` | `160000` | `2c58ffda41759e895ace4b9625c9bd931a217672` |
| `references/Hexalith.Folders` | `160000` | `e88aca956ff24cd6371a5fe3446034c45c70e644` |
| `references/Hexalith.FrontComposer` | `160000` | `bad341fe2b02ed11f982b3dadfbf516d011870f3` |
| `references/Hexalith.Memories` | `160000` | `ece4edc4c9a37a62b34d3b7c8aa901fc363c038c` |
| `references/Hexalith.Parties` | `160000` | `937cb2a343aaa74963db9bb867a2c3a01ff48677` |
| `references/Hexalith.Projects` | `160000` | `10aba2537c097b6d60906f279775a533f2a72f3a` |
| `references/Hexalith.Tenants` | `160000` | `9bad98d93f3fff34351ed95fe07c7bc0eb7c54be` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-8.1-ENTRY-v1` | `6c61eb92078755496c73506419112026e3e9b7f63bb314b1028d4e9c7bb41ef9` |

## Predecessors

- `7.4`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-8.1-01` | `0` | `PASS` | `none` | `7` | `docs/release-evidence/ux-preservation-disposition-v1.json` | `e359a97e19d3f021792006a4c14ba3d1ff42249c05e5c76fba0e43302aa4461f` |
| `AC-8.1-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-02.trx` | `d2fe2825cf8faf718aac7d5cfe16a3c88831536f83674fc145d63fdebb722830` |
| `AC-8.1-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-03.trx` | `14be6f4b97974b20872075df9477bbc65bf51ff96c337d982a4ae3630884a309` |
| `AC-8.1-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-04.trx` | `2a3706e4261bb12a79f85eee1299520238e69508d693cd36852a5c36416764be` |
| `AC-8.1-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-05.trx` | `440a2195624953d8d4c7f6c38e72b736a7e394a54c634ed6a02bf9a39999d2df` |
| `AC-8.1-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-06.trx` | `bdb25f5bff086e2c65f92f275d023a7398cabc483bed4d5a27b1b97223bad4ac` |
| `AC-8.1-07` | `0` | `PASS` | `none` | `13` | none | none |

### `AC-8.1-01`

Command: `python3 _bmad/scripts/generate_ux_preservation_disposition.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.1.json --output-schema docs/release-evidence/ux-preservation-disposition-v1.schema.json --output-json docs/release-evidence/ux-preservation-disposition-v1.json --output-markdown docs/release-evidence/ux-preservation-disposition-v1.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-01#0001` | `schema::closed-valid` | `PASS` |
| `AC-8.1-01#0002` | `sources::path-version-hash` | `PASS` |
| `AC-8.1-01#0003` | `inventory::52-28` | `PASS` |
| `AC-8.1-01#0004` | `status::preserved` | `PASS` |
| `AC-8.1-01#0005` | `provenance::non-current` | `PASS` |
| `AC-8.1-01#0006` | `markdown::digest` | `PASS` |
| `AC-8.1-01#0007` | `predecessor::7.4` | `PASS` |

### `AC-8.1-02`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.SourcesShouldBindCanonicalPathsVersionsAndHashes -trx artifacts/v9/8.1/AC-8.1-02.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-02#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.SourcesShouldBindCanonicalPathsVersionsAndHashes` | `PASS` |

### `AC-8.1-03`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DecisionsShouldProjectTheFrozenInventory -trx artifacts/v9/8.1/AC-8.1-03.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-03#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DecisionsShouldProjectTheFrozenInventory` | `PASS` |

### `AC-8.1-04`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.AcceptanceCriteriaShouldProjectTheFrozenInventory -trx artifacts/v9/8.1/AC-8.1-04.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-04#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.AcceptanceCriteriaShouldProjectTheFrozenInventory` | `PASS` |

### `AC-8.1-05`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DispositionsShouldRemainPreservedAndHistorical -trx artifacts/v9/8.1/AC-8.1-05.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-05#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DispositionsShouldRemainPreservedAndHistorical` | `PASS` |

### `AC-8.1-06`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.CandidateShouldContainNoProductionUiChange -trx artifacts/v9/8.1/AC-8.1-06.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-06#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.CandidateShouldContainNoProductionUiChange` | `PASS` |

### `AC-8.1-07`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.1.json --format bundle --output-json docs/release-evidence/story-8.1-final-record-v2.json --output-markdown docs/release-evidence/story-8.1-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-07#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-8.1-07#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-8.1-07#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-8.1-07#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-8.1-07#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-8.1-07#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-8.1-07#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-8.1-07#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-8.1-07#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |
| `AC-8.1-07#0010` | `generator::ux-disposition-schema-sources-and-output-digests` | `PASS` |
| `AC-8.1-07#0011` | `generator::story-7.4-predecessor-pair-verified` | `PASS` |
| `AC-8.1-07#0012` | `generator::five-exact-xunit-selectors-passed` | `PASS` |
| `AC-8.1-07#0013` | `generator::candidate-build-and-production-scope-bound` | `PASS` |

## Story 8.1 UX disposition

- Contract: `_bmad-output/planning-artifacts/v9/story-contracts/8.1.json`
- Contract SHA-256: `51daf31859c8bf01c6bb7960b1ff3ba7c3e47eb121ed6da7966e59a285d3ed28`
- Story 7.4 record SHA-256: `1739a8daf93955fe31050b659151f1d45a8555ff58fe3a130b8a389c049d8290`
- Build SourceRevisionId: `b9859097d6bca9e9ed315313a4ffaa5e2cef157f`
- Test assembly SHA-256: `63bb2456912c1b69254f23038774d20e2c7af453c96df6e7b73253758d71f706`

| Bound input/output | Path | SHA-256 |
| --- | --- | --- |
| Source | `_bmad-output/planning-artifacts/ux-design-specification.md` | `948a5ac40a05fce510bffdd6818e3fcf3c871874b8779468954de57e452d8f18` |
| Source | `_bmad-output/planning-artifacts/ux-requirement-map.md` | `5965394e662a3b708896f5df85d2b981798bc590ea68feb3a974f66300c2751f` |
| `schema` | `docs/release-evidence/ux-preservation-disposition-v1.schema.json` | `d189f4dd1b1e701f683a4ff2ce7be54a1dba57290162555b9dd141bb832fe5dc` |
| `json` | `docs/release-evidence/ux-preservation-disposition-v1.json` | `e359a97e19d3f021792006a4c14ba3d1ff42249c05e5c76fba0e43302aa4461f` |
| `markdown` | `docs/release-evidence/ux-preservation-disposition-v1.md` | `330aff37565e46b3bbe5435c0c6c9ecabc814e54220f2102b7dc234de5abcbc1` |

## Fault injection

No fault-injection result is bound to this record.

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-8.1-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-8.1-final-record-v2.md` |

## Rollback boundary

remove only the Story 8.1 generator, schema, results, three disposition outputs, and final record; preserve both UX sources, historical mappings, product/UI code, and the v1-v8 prefix.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `7` | `7` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
