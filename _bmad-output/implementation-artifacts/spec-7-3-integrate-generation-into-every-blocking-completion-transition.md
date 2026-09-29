---
title: 'Integrate generation into every blocking completion transition'
type: 'feature'
created: '2026-09-29'
status: 'in-review'
baseline_commit: '91bc1376bc433400186ed9404bd6541b86355f98'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/v9/story-contracts/7.3.json'
  - '{project-root}/docs/runbooks/story-final-record-generation.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The four live completion routes each carry their own legacy v1 final-record prose, with no v2 contract call and no parity guard. Only `bmad-build/step-05-present` knows the v2 route, and only for Story 7.2. Nothing proves that every route and its render twin gate `review`/`done` on the same generator.

**Approach:** Insert one byte-identical, marker-delimited v2 contract gate into every governed route, before its transition. Add `verify_story_completion_workflows.py` to prove presence, placement, and parity, including in-memory render twins. Extend the v2 generator to verify inserted Markdown and emit the Story 7.3 record.

## Boundaries & Constraints

**Always:** Follow all seven commands in `v9/story-contracts/7.3.json` exactly. Govern the four routes that the owner rebound one-for-one from the frozen inventory (PRD `.memlog.md:166-167`), in both `.agents/skills` and `.claude/skills`:
- `bmad-build/step-05-present` and `bmad-build/step-oneshot` replace `bmad-quick-dev` 05/oneshot.
- `bmad-build-auto/step-04-review` replaces `bmad-dev-story` step 9.
- `bmad-code-review/steps/step-04-present` is unchanged.

The render twins are the in-memory renders of the three `render_skill.py` routes. The block's blocker branch keeps or returns the spec and sprint row to `in-progress` and never writes `review` or `done`. It reports the exact command, exit, and blockers, then HALTs. Every fault fixture restores byte-identically. Exits are `0` PASS, `1` FAIL, `2` BLOCKED.

**Decision (owner, 2026-09-29):** A route must run the gate before `review` or `done` whenever `_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json` exists for the story; the spec cannot opt out. Routine work without a contract skips the gate under the current-change policy. The owner kept the full spec despite its ~2,350 tokens, because AC-7.3-07 needs every part in place.

**Never:**
- Edit `7.3.json`, the Story 7.1/7.2 pairs, `references/`, gitlinks, or the stale tracked `_bmad/render/bmad-quick-dev|bmad-dev-auto` files.
- Write into `_bmad/render/` to check the twins.
- Change the v1 route or Story 7.1/7.2 output bytes.
- Break `StoryFinalRecordGenerationValidationTest`, or change it.
- Claim CI enforcement.
- Implement Story 7.4.
- Require the historical V23–V29 boundary, the resolver, or Story 7.1 terminal `ACCEPTED` for this story. Per the current-change runbook and the owner's 2026-09-22/29 waivers, these are not gates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Integrated | 8 bodies + 3 twins, one in-span block each | AC-01/02 exit `0`, acceptance-result v1 with body digests | N/A |
| Removed | One body loses its block | Exit `1` `FAIL` | `WORKFLOW_INTEGRATION_MISSING` |
| Displaced | Block after the transition, decoy command elsewhere | Exit `1` `FAIL` | `WORKFLOW_INTEGRATION_DISPLACED` |
| Drift | Block bytes differ between bodies, trees, or twins | Exit `1` `FAIL` | `SURFACE_PARITY_DRIFT` |
| Inserted record | Spec region equals `.md` bytes and `renderedMarkdownSha256` | Verify exit `0` | Altered region: exit `1`, `RECORD_CONTENT_DRIFT` |
| Unreadable | Contract is not 7.3, or render/read fails | Exit `2` `BLOCKED` | Stable blocker, no partial PASS |

</frozen-after-approval>

## Code Map

- **Four routes × two trees.** Gate span → follower → transition, per the C# `Contracts` table (`StoryFinalRecordGenerationValidationTest.cs:41-127`):
  - `bmad-build/step-05-present.md:23,77`
  - `bmad-build/step-oneshot.md:80,96`
  - `bmad-build-auto/step-04-review.md:109,131`
  - `bmad-code-review/steps/step-04-present.md:97,111`

  Keep each legacy v1 paragraph and its ordered clauses as they are. Put the new block **after** them, still inside the span, so `ReplaceFirst`/order mutations keep hitting legacy text. In step-05, delete the Story 7.2 subsection, the `### Story 7.2 Terminal Gate`, and the 7.2 sentence at `:17`. Story 7.2 is done and the owner waived its gate (deferred-work `:404`).
- `_bmad/scripts/render_skill.py:232` -- `_render_sources` renders in memory. It resolves `{{…}}`, `{workflow.x}`, and `[[bmad-snapshot:…]]`, so the block must avoid those forms.
- **`generate_story_record.py`:**
  - v2 dispatch `4185–4561`. It accepts only pytest or self commands (`4280–4341`); add an acceptance-result v1 reader.
  - `V2_CODES` `2790`.
  - Retention `v2_story_7_2_candidate` `3936`: generalize to 7.3 without changing 7.2.
  - Predecessor digest `4162–4182`.
  - Pair check `v2_verify_pair` `3744`.
  - Markers `RECORD_BEGIN_MARKER` `31`.
- `_bmad/schemas/story-final-record-v2.schema.json:22-40` -- closed. Add a 7.3-only section. `v9-acceptance-result-v1.schema.json` is the result envelope; no producer exists yet.
- `check_lifecycle_gate_preflight.py:65-146` -- marker, order, and mirror style to copy.
- `_bmad/scripts/tests/test_generate_story_record.py` -- `build_v2_7_2_repository` `3590` and `v2_7_2_snapshot_and_fault` `3640` to reuse. Parity tests `1403–1450` must stay green.

## Tasks & Acceptance

**Execution:**
- [x] Both trees, four routes -- insert the identical block between `<!-- STORY-COMPLETION-GATE:BEGIN v1 -->` and `<!-- STORY-COMPLETION-GATE:END v1 -->`. It holds four things:
  - applicability: required whenever the story's v9 contract exists
  - `uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --format bundle --output-json <json> --output-markdown <md>`, with the paths from `finalRecord.paths`, requiring exit `0` and the exact summary
  - the record-only pair commit, then verbatim insertion between `STORY-FINAL-RECORD` markers and `--verify-inserted-record {spec_file}`
  - the blocker branch

  Also update the gate intros and remove the Story 7.2-only step-05 text.
- [x] `_bmad/scripts/verify_story_completion_workflows.py` -- implement AC-01/02 per the matrix. Emit acceptance-result v1 with sorted `{path, sha256}` body inputs and a nonempty ledger.
- [x] `generate_story_record.py` + v2 schema -- add four things. Keep every 7.1/7.2 test green.
  - the acceptance-result scenario reader: schema, IDs, command, `resultSemantics`, candidate, staleness
  - `--verify-inserted-record`
  - a 7.3 section binding body digests re-derived from the candidate, which must equal the AC inputs, plus the 7.1 and 7.2 record digests
  - codes and retention
- [x] `test_generate_story_record.py` -- add the `v2_workflow_verifies_inserted_digest`, `v2_fault_removed_workflow_invocation`, `v2_fault_displaced_workflow_invocation`, and `v2_blocker_prevents_state_transition` selectors. The last one checks generator exits 1 and 2 on fixtures, plus each surface's blocker branch and no CI claim. Add verifier unit tests for every matrix row.
- [x] `docs/runbooks/story-final-record-generation.md` -- cover the Story 7.3 surfaces, the rebinding, render twins, the block, the new codes, and the no-CI limitation.
- [ ] `docs/release-evidence/story-7.3-final-record-v2.{json,md}` -- generate at step 05 from the committed candidate, following the Story 7.2 completion pattern.
  This box stays unticked in the committed spec. After the candidate commit, the generator allows only the frontmatter `status` and the inserted record region to change, so the inserted record and the lifecycle commit carry this task's evidence.

**Acceptance Criteria:**
- Given the integrated tree, when AC-7.3-01 through AC-7.3-06 run as declared, then each exits `0` with `PASS`.
- Given six passing results and the 7.1/7.2 records, when AC-7.3-07 runs twice, then both runs exit `0` with identical bytes, `7/7/0/0/0/0`, and bound body hashes. A rerun after the pair and lifecycle commits reproduces the pair.
- Given the change, when the Release Conformance project and the generator suite run, then both pass. The `_bmad/scripts` lane adds no failures over the baseline.

## Implementation Notes

- **Routes.** All eight bodies now carry one byte-identical block, inserted after the legacy v1 paragraphs and before the end of each C# gate span. The legacy clauses and their order are unchanged. Each gate intro now sends a contract-bound story to the block instead of the legacy procedure. Each candidate-preparation sentence now also triggers when the story's v9 contract exists. The code-review intro also clears `record_gate_failed` when the gate passes. In step-05, the Story 7.2 sentence, the Story 7.2 v2 subsection, the `Legacy final-record procedure` heading it introduced, and `### Story 7.2 Terminal Gate` are removed.
- **Block constraints.** The block uses lowercase "verbatim" and never contains `--verify-record-sha256`, a gate or follower heading, or a success clause. It therefore cannot absorb the C# `ReplaceFirst`, gutting, or reorder mutations. It avoids every `render_skill.py` token form, so the twins carry the source bytes. The applicability sentence and the blocker branch fit all four routes.
- **Verifier.** `_bmad/scripts/verify_story_completion_workflows.py` reads the working tree. It renders the three twins from `.claude/skills` in memory, reproducing `render_skill.render()` without its publication step, and never writes into `_bmad/render/`. `AC-7.3-01` checks presence, the generator command inside the block, and placement inside the gate span before every occurrence of the transition. `AC-7.3-02` checks byte parity across all eleven surfaces, fourteen required clauses plus the generator command, and the absence of any CI-enforcement claim. A blocked result binds no input and no ledger. Invocations that cannot form a contract-bound result print a separate failure document and write nothing.
- **Generator.** An acceptance-result reader recognizes `python3 SCRIPT --repository . --contract C --scenario ID --output PATH`. It checks the schema, IDs, exact command, `resultSemantics` consistency, candidate, mtime staleness, input digests against candidate blobs, and ledger. For 7.3 it adds the closed `workflowIntegration` section: the contract digest, the eight candidate body digests (which must equal each result's inputs), and the verified 7.1/7.2 JSON digests. It adds `--verify-inserted-record` and propagates `WORKFLOW_INTEGRATION_MISSING`, `WORKFLOW_INTEGRATION_DISPLACED`, and `SURFACE_PARITY_DRIFT`. Candidate retention is generalized from Story 7.2 to Story 7.3. The 7.3 rule also accepts the inserted record region, whether it fills a pre-existing marker pair or appends one pair after a blank line. Story 7.2 behavior, messages, and bytes are unchanged; the unused `v2_story_7_2_candidate` wrapper was removed.
- **Decision: acceptance schema loaded separately.** The acceptance schema is loaded only for contracts that declare acceptance-result scenarios. `test_v2_every_code_is_documented_in_the_runbook` pins the four-entry `V2_SCHEMA_FILES`, so adding the schema there would have required changing an existing 7.1 test.
- **Decision: self-ledger.** The self-invocation ledger and the Markdown intro sentence vary by story. Only 7.3 gets three extra generator assertions and the "acceptance results" wording, so the 7.1 and 7.2 bytes re-render identically. `test_v2_story_7_3_changes_leave_story_7_1_and_7_2_pairs_byte_identical` proves this.
- **Surprise.** `dataclasses` fails when a module is loaded through `spec_from_file_location` without registering it in `sys.modules`. The verifier therefore uses `NamedTuple`.
- **Files.** `.agents/skills` and `.claude/skills`: `bmad-build/step-05-present.md`, `bmad-build/step-oneshot.md`, `bmad-build-auto/step-04-review.md`, and `bmad-code-review/steps/step-04-present.md`. Also `_bmad/scripts/verify_story_completion_workflows.py` (new), `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` (113 new tests), and `docs/runbooks/story-final-record-generation.md`.
- **Review patch.** The block now defines `<story-id>` and makes the record-only commit conditional. It forbids only source and gitlink changes after that commit, with a retraction path, and counts an unauthorized required commit as a blocker. The generator accepts acceptance results rerun on the verified lifecycle-only path and requires an LF after the END marker. The build-auto Finalize sentence now names the v9-contract trigger. The runbook documents the backlog-contract fail-closed limitation.
- **Step 05 is not done here.** The Story 7.3 record pair is not generated yet. `AC-7.3-07` needs the committed candidate as `HEAD` and a clean tree, and every `artifacts/v9/7.3` result must be rerun after that commit. The results in the working tree bind the pre-commit `HEAD`, so the generator correctly rejects them as stale.

## Spec Change Log

## Review Triage Log

### 2026-09-29 — Review pass 1 (blind, edge-case, verification-gap)

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 1 | build-auto writes `in-review` before the gate (blind, edge) | false | reject | The gated `review` transition is the sprint-status sync to `review` (step-05:57, oneshot:119), which follows the gate. Spec `in-review` is the pre-review phase marker, and the blocker branch returns the spec to `in-progress`. |
| 2 | This spec is `in-review` while its sprint row is `in-progress` (blind) | false | reject | `in-review` is this build workflow's step-04 phase state, not the gated transition. The fix would edit this build's spec. |
| 3 | No verification evidence recorded in the spec (blind) | low | reject | The fix edits this build's spec. Step 05 records the evidence. |
| 4 | The block's "later commits may change only…" rule contradicts post-gate spec writes: build-auto Finalize (`## Auto Run Result`, `followup_review_recommended`) and oneshot (`## Review Triage Log`) (blind, edge, verification-gap) | medium | patch | Verified at build-auto:139-147 and oneshot:112-117. Reword the block to forbid only source and gitlink changes after the record-only commit. |
| 5 | Every backlog contract has scenario commands the generator cannot classify, so its gate blocks with `SCENARIO_COMMAND_UNSUPPORTED` (blind, edge) | medium | patch | Verified: 7.4 has 1 of 6, 8.1 has 6 of 7, and 16.3 has 5 of 6 unclassifiable, and so on. The owner-mandated fail-closed HALT is correct. Document the limitation and its remediation (extend the generator within the story) in the runbook. |
| 6 | `<story-id>` is undefined; routes carry `{story_key}` (`7-3-…`), so a mis-resolution skips the whole section (blind) | medium | patch | The intros decide skip versus gate before the block is read. Define `<story-id>` as the dotted number in the intros and the block. |
| 7 | Verify mode accepts any in-repo spec file and does not recheck candidate finality (blind, edge) | low | reject | Routes pass `{spec_file}`, and nothing commits between the record-only commit and verification. The generator rerun enforces finality for retained contracts. The fix adds guards. |
| 8 | Twin verdicts are not bound to render inputs such as `render_skill.py`, config TOML, and user overrides (blind, edge) | low | reject | The block has no render tokens, so overrides cannot change its bytes. Tracked inputs fall under the generator's clean-tree and mtime checks. Binding them adds complexity. |
| 9 | The real render path is untested: returning raw sources instead of `_render_sources` passes every test (blind, verification-gap) | medium | patch | Pre-verified gap. Add a uniform render-token parity case that must flag exactly the three twins. |
| 10 | The `CI_CLAIM` regex is narrow (blind) | low | reject | A heuristic. The required "no CI job or hook enforces it" clause already pins the limitation, and a uniform rewording of all eight blocks is unlikely. |
| 11 | The block omits the twice-run determinism check and schema validation (blind) | false | reject | The frozen Tasks list four block elements, and the block carries all four. Determinism is the generator's tested property and this story's AC-7.3-07 check. |
| 12 | An unauthorized commit is not a blocker, so code-review `record_gate_failed` can stay false (blind, edge) | medium | patch | The code-review intro clears `record_gate_failed` when the section is "skipped". Name a required commit that is not authorized as a blocker-branch trigger. |
| 13 | No recovery once the pair is committed: retained contracts return `CANDIDATE_NOT_FINAL` forever (blind, edge) | medium | patch | `v2_retained_candidate` returns `HEAD` when both outputs are absent. Document the retraction commit in the block and the runbook. |
| 14 | The schema does not pin the `workflowIntegration` identities (blind) | low | reject | The generator derives them from constants, and the tests assert them. Anyone able to commit a forged pair can recompute its digests. |
| 15 | `deferred-work.md:46` still lists `bmad-dev-auto` as a live bypass (blind) | low | reject | The deferred-work sweep resolves ledger entries; they are not edited in place. The runbook now records the retirement. |
| 16 | Record markers are matched as substrings (blind) | low | reject | Fails closed with `RECORD_CONTENT_DRIFT`, consistent with the v1 marker handling at `generate_story_record.py:773`. Line matching is more than a direct correction, and this spec quotes no full marker. |
| 17 | The runbook's 7.3 code table omits `SCENARIO_COMMAND_UNSUPPORTED`, and "any difference is drift" overstates verify mode (blind) | low | patch | A direct documentation correction. |
| 18 | `v2_story_7_2_candidate` is dead code (blind, edge) | low | patch | It has no caller in the generator or the tests; delete it. Renaming `V2_7_2_SPRINT_PATH` is rejected as churn in Story 7.2 code. |
| 19 | Tests read the live checkout (blind) | low | reject | The gate runs on a clean tree whose bytes equal the candidate. This matches the existing Story 7.2 test style. |
| 20 | Verify mode's committed-pair validity check is never exercised (verification-gap) | medium | patch | Pre-verified. Add committed tampered-JSON and other-story faults. |
| 21 | Only 1 of 14 required clauses is tested (verification-gap) | medium | patch | Pre-verified. Parametrize uniform clause loss over a literal list of the clauses. |
| 22 | A symlinked acceptance result is untested (verification-gap) | low | patch | Pre-verified. Mirror the JUnit symlink fault. |
| 23 | `OUTPUT_WRITE_FAILED` is untested (verification-gap) | low | patch | Pre-verified. Add an escaping-output case. |
| 24 | build-auto:153 still says a candidate commit exists only for an explicit final record (verification-gap) | low | patch | A direct correction: include the v9 contract. |
| 25 | Rerunning AC-7.3-01/02 after the record-only commit stamps the new `HEAD`, while the generator keeps the older candidate, so the result is `TEST_RESULTS_STALE` (edge) | medium | patch | Verified: the verifier stamps `HEAD` and the reader requires `candidate` equality. Accept results stamped on the verified lifecycle-only ancestry path. |
| 26 | A blocked verifier leaves an earlier PASS on disk (edge) | low | reject | That PASS was measured on the same candidate and inputs, and the generator rechecks candidate, mtime, and input digests. |
| 27 | A date-only sprint change is rejected (edge) | low | reject | Pre-existing Story 7.2 semantics. The lifecycle commit always changes the row value. |
| 28 | Rerunning the gate on an identical pair makes an empty commit, which counts as a commit failure (edge) | medium | patch | Skip the record-only commit when both outputs already equal their `HEAD` bytes. |
| 29 | An END marker at EOF without LF passes verify but fails retention (edge) | low | patch | A direct correction: require LF after the END line. |
| 30 | An acceptance output may name a tracked file (edge) | low | reject | Contract commands are owner-frozen. The guard adds complexity. |
| 31 | A lone surrogate in a ledger subject yields `INTERNAL_ERROR` (edge) | low | reject | The verifier emits path-derived subjects; a crafted result is needed. |
| 32 | A BLOCKED acceptance result is reported as FAIL with exit 1 (edge) | low | reject | The `TEST_RESULTS_FAILED` message carries the verifier's blocker codes, matching how the JUnit path treats non-passing scenarios. |

**Outcome.** No intent-gap or spec-defect finding, so no loopback. The implementation subagent applied all 18 patch findings. One requested assertion changed: rerunning AC-7.3-01/02 after the record-only commit rebinds those two result digests, so it yields a new pair that is itself reproducible, not the same bytes. The AC-7.3-07 rerun without new results still reproduces the pair.

## Design Notes

This is one gate with one byte-identical span everywhere. Parity is byte equality of the spans, and placement is judged inside the existing C# gate span, so decoy text outside that span never counts. Twins are compared as rendered bytes, never written. Surfaces differ only outside the block, which is why the blocker branch is phrased to fit all four routes.

## Verification

**Commands:**
- `TMPDIR=/var/tmp uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- expected: all pass.
- AC-7.3-01…06 as declared, via `uv run --frozen --no-sync` -- expected: each exit `0`.
- `dotnet build Hexalith.Conversations.slnx -c Release -p:UseHexalithProjectReferences=true`, then run the Conformance test executable -- expected: zero failures.
- `python3 scripts/check-root-submodules.py --repository .` and `git diff --check` -- expected: clean.

**Results (patched pre-candidate tree, 2026-09-29):**
- Generator suite: 284 passed, 0 failed, 0 skipped.
- AC-7.3-01 and AC-7.3-02 verifier: exit `0`, `PASS`. AC-7.3-03…06 selectors: 13, 20, 15, and 14 passed, each exit `0`.
- Release build: 0 warnings, 0 errors. Conformance executable: 473 total, 0 failed, 0 skipped.
- `_bmad/scripts` lane: 1305 tests, 265 failed plus 86 errors (351), the same count as the pre-change baseline. All are in historical-authority modules, from the missing `.github/workflows/planning-authority-preflight.yml`, gitlink or architecture drift, and stale sprint projections. None is in a Story 7.3 test.
- `check-root-submodules.py`: PASS. `git diff --check`: clean.
- AC-7.3-07 runs at step 05 on the committed candidate; its record is inserted below.
