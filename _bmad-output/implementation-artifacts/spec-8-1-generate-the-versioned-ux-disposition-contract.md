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
