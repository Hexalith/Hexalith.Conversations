---
title: 'Integrate generation into every blocking completion transition'
type: 'feature'
created: '2026-09-29'
status: 'in-progress'
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

### Review Findings

- [ ] [Review][Patch] Allow route-owned post-gate lifecycle edits during candidate retention [_bmad/scripts/generate_story_record.py:4283]
- [ ] [Review][Patch] Bind inserted-record verification to the designated story spec, retained candidate, and current contract [_bmad/scripts/generate_story_record.py:5184]
- [ ] [Review][Patch] Propagate every verifier stable blocker code through generator failures [_bmad/scripts/generate_story_record.py:2771]
- [ ] [Review][Patch] Validate every intervening retained-candidate commit instead of only the endpoint tree [_bmad/scripts/generate_story_record.py:4406]
- [ ] [Review][Patch] Validate acceptance-ledger row IDs before re-keying them [_bmad/scripts/generate_story_record.py:3554]
- [ ] [Review][Patch] Verify Story 7.2's predecessor link against the bound Story 7.1 record [_bmad/scripts/generate_story_record.py:4708]
- [ ] [Review][Patch] Correct mode-aware operator diagnostics [_bmad/scripts/generate_story_record.py:2969]
- [ ] [Review][Patch] Add a dirty working-tree Markdown verification case [_bmad/scripts/tests/test_generate_story_record.py:4563]
- [ ] [Review][Patch] Add a duplicate acceptance-ledger subject regression case [_bmad/scripts/tests/test_generate_story_record.py:5337]

#### Rejected

- `false` — Later contracts stopping with `SCENARIO_COMMAND_UNSUPPORTED` is the story's documented fail-closed behavior until each story adds its reader, not an accidental rollout gap.
- `false` — Candidate retention is intentionally implemented for Stories 7.2 and 7.3; later contracts cannot currently reach a passing committed pair because their unsupported scenarios fail first.
- `false` — No governed route creates a date-only sprint update; each lifecycle path changes the story row, so rejecting an isolated date churn does not break the reviewed workflow.
- `false` — Every Story 7.3 scenario forbids `not-applicable`; the cited generic behavior is unreachable for this story and future contracts using it remain unsupported by the current reader.
- `false` — Normal render inputs are bound by the candidate commit and unexpected working-tree changes are rejected; the proposed transient-render attack requires replacing the workflow's measured-artifact trust model.
- `false` — Acceptance-result files are deliberately treated as measured machine artifacts, as JUnit files are; cryptographic execution attestation is outside this story's contract.
- `false` — The reviewed routes contain no alternate lifecycle transition, and the verifier uses the owner-frozen transition anchors rather than attempting semantic interpretation of arbitrary prose.
- `low` — A post-replace readback I/O failure could leave a completed acceptance output, but the trigger is exceptional and transactional restoration would add disproportionate complexity.
- `medium (rejected: spec change)` — The Python behavior suite is absent from CI, but Story 7.3 explicitly requires the gate to state that no CI job or hook enforces it; changing that policy requires owner renegotiation rather than a review patch.

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

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 7.3 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate, measured JUnit results, and acceptance results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `7.3`
- Candidate: `450bd270a1fdb1a632cbda0cf271107de97fee69`
- JSON content SHA-256 (all three digest fields zeroed): `1141eb322ca4993d0144c6113420d7f8eeff60988c9d9f33490cce860e715c8e`

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
| `references/Hexalith.Builds` | `160000` | `85ca19bc99b137f0825d6a441199164653f537e9` |
| `references/Hexalith.Commons` | `160000` | `53f7961b517becde5b84ed4d20fe696b849b5cd9` |
| `references/Hexalith.EventStore` | `160000` | `771269ae9ba03357150b1deec8ee4b6e48f688f5` |
| `references/Hexalith.Folders` | `160000` | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` |
| `references/Hexalith.FrontComposer` | `160000` | `8e23128f08618c5a28b4e88b4acb011c1a9bed49` |
| `references/Hexalith.Memories` | `160000` | `289773387e0c6b665b669031616c1e84cc079297` |
| `references/Hexalith.Parties` | `160000` | `60b9836ea23151c5319dd06fd3deb80122f7abc3` |
| `references/Hexalith.Projects` | `160000` | `1152c8397f35fed1580e20915833dd958e8dbe07` |
| `references/Hexalith.Tenants` | `160000` | `e077e65ebfd5117bc9af85fd41a20cd8781f3b15` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-7.3-ENTRY-v1` | `ca106f6ad40f3a2ca580358d74a1565c34611821bc1d25e51694972d46ae8ca2` |

## Predecessors

- `7.2`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-7.3-01` | `0` | `PASS` | `none` | `33` | `artifacts/v9/7.3/AC-7.3-01.json` | `b03cfb64208481273fa717470fd130d544b5eae12e7ac8ba6e014d961a815885` |
| `AC-7.3-02` | `0` | `PASS` | `none` | `22` | `artifacts/v9/7.3/AC-7.3-02.json` | `dca61d65ff7efad1ea738530a79ca7c12f8bf032967f7a279818453be9dc915d` |
| `AC-7.3-03` | `0` | `PASS` | `none` | `13` | `artifacts/v9/7.3/AC-7.3-03.xml` | `35ace92222838d500697fba7c1c84ae683d42dcc5c787e483ae670a0a1755367` |
| `AC-7.3-04` | `0` | `PASS` | `none` | `20` | `artifacts/v9/7.3/AC-7.3-04.xml` | `7e53b7700911bbcbf493d82b8598efe3f0b9bed8c44147b697687e35b2d50810` |
| `AC-7.3-05` | `0` | `PASS` | `none` | `15` | `artifacts/v9/7.3/AC-7.3-05.xml` | `fe20bb911d242e9bc674de6fdad7cf5bf505e34b9fcbcb586fe449bb766dc123` |
| `AC-7.3-06` | `0` | `PASS` | `none` | `14` | `artifacts/v9/7.3/AC-7.3-06.xml` | `089bfc873761b5d62296fc7760ccd11a4d314bf417c345a97aa4ef5d2a1be57a` |
| `AC-7.3-07` | `0` | `PASS` | `none` | `12` | none | none |

### `AC-7.3-01`

Command: `python3 _bmad/scripts/verify_story_completion_workflows.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --scenario AC-7.3-01 --output artifacts/v9/7.3/AC-7.3-01.json`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.3-01#0001` | `.agents/skills/bmad-build-auto/step-04-review.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0002` | `.agents/skills/bmad-build-auto/step-04-review.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0003` | `.agents/skills/bmad-build-auto/step-04-review.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0004` | `.agents/skills/bmad-build/step-05-present.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0005` | `.agents/skills/bmad-build/step-05-present.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0006` | `.agents/skills/bmad-build/step-05-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0007` | `.agents/skills/bmad-build/step-oneshot.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0008` | `.agents/skills/bmad-build/step-oneshot.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0009` | `.agents/skills/bmad-build/step-oneshot.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0010` | `.agents/skills/bmad-code-review/steps/step-04-present.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0011` | `.agents/skills/bmad-code-review/steps/step-04-present.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0012` | `.agents/skills/bmad-code-review/steps/step-04-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0013` | `.claude/skills/bmad-build-auto/step-04-review.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0014` | `.claude/skills/bmad-build-auto/step-04-review.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0015` | `.claude/skills/bmad-build-auto/step-04-review.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0016` | `.claude/skills/bmad-build/step-05-present.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0017` | `.claude/skills/bmad-build/step-05-present.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0018` | `.claude/skills/bmad-build/step-05-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0019` | `.claude/skills/bmad-build/step-oneshot.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0020` | `.claude/skills/bmad-build/step-oneshot.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0021` | `.claude/skills/bmad-build/step-oneshot.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0022` | `.claude/skills/bmad-code-review/steps/step-04-present.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0023` | `.claude/skills/bmad-code-review/steps/step-04-present.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0024` | `.claude/skills/bmad-code-review/steps/step-04-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0025` | `render:.claude/skills/bmad-build-auto/step-04-review.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0026` | `render:.claude/skills/bmad-build-auto/step-04-review.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0027` | `render:.claude/skills/bmad-build-auto/step-04-review.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0028` | `render:.claude/skills/bmad-build/step-05-present.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0029` | `render:.claude/skills/bmad-build/step-05-present.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0030` | `render:.claude/skills/bmad-build/step-05-present.md::block-in-gate-span-before-transition` | `PASS` |
| `AC-7.3-01#0031` | `render:.claude/skills/bmad-build/step-oneshot.md::completion-gate-block-present` | `PASS` |
| `AC-7.3-01#0032` | `render:.claude/skills/bmad-build/step-oneshot.md::generator-invocation-in-block` | `PASS` |
| `AC-7.3-01#0033` | `render:.claude/skills/bmad-build/step-oneshot.md::block-in-gate-span-before-transition` | `PASS` |

### `AC-7.3-02`

Command: `python3 _bmad/scripts/verify_story_completion_workflows.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --scenario AC-7.3-02 --output artifacts/v9/7.3/AC-7.3-02.json`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.3-02#0001` | `.agents/skills/bmad-build-auto/step-04-review.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0002` | `.agents/skills/bmad-build-auto/step-04-review.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0003` | `.agents/skills/bmad-build/step-05-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0004` | `.agents/skills/bmad-build/step-05-present.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0005` | `.agents/skills/bmad-build/step-oneshot.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0006` | `.agents/skills/bmad-build/step-oneshot.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0007` | `.agents/skills/bmad-code-review/steps/step-04-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0008` | `.agents/skills/bmad-code-review/steps/step-04-present.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0009` | `.claude/skills/bmad-build-auto/step-04-review.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0010` | `.claude/skills/bmad-build-auto/step-04-review.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0011` | `.claude/skills/bmad-build/step-05-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0012` | `.claude/skills/bmad-build/step-05-present.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0013` | `.claude/skills/bmad-build/step-oneshot.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0014` | `.claude/skills/bmad-build/step-oneshot.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0015` | `.claude/skills/bmad-code-review/steps/step-04-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0016` | `.claude/skills/bmad-code-review/steps/step-04-present.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0017` | `render:.claude/skills/bmad-build-auto/step-04-review.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0018` | `render:.claude/skills/bmad-build-auto/step-04-review.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0019` | `render:.claude/skills/bmad-build/step-05-present.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0020` | `render:.claude/skills/bmad-build/step-05-present.md::block-states-gate-contract` | `PASS` |
| `AC-7.3-02#0021` | `render:.claude/skills/bmad-build/step-oneshot.md::block-bytes-identical-across-surfaces` | `PASS` |
| `AC-7.3-02#0022` | `render:.claude/skills/bmad-build/step-oneshot.md::block-states-gate-contract` | `PASS` |

### `AC-7.3-03`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_workflow_verifies_inserted_digest --junitxml=artifacts/v9/7.3/AC-7.3-03.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.3-03#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_matching_bytes_pass_and_altered_bytes_fail` | `PASS` |
| `AC-7.3-03#0002` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[no-marker-pair]` | `PASS` |
| `AC-7.3-03#0003` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[duplicated-marker-pair]` | `PASS` |
| `AC-7.3-03#0004` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[truncated-region]` | `PASS` |
| `AC-7.3-03#0005` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[trailing-byte-in-region]` | `PASS` |
| `AC-7.3-03#0006` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[marker-not-on-its-own-line]` | `PASS` |
| `AC-7.3-03#0007` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[end-marker-without-trailing-lf]` | `PASS` |
| `AC-7.3-03#0008` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[uncommitted-pair]` | `PASS` |
| `AC-7.3-03#0009` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[edited-working-tree-json]` | `PASS` |
| `AC-7.3-03#0010` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[inconsistent-committed-json]` | `PASS` |
| `AC-7.3-03#0011` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[foreign-story-committed-json]` | `PASS` |
| `AC-7.3-03#0012` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_and_reproduces_the_pair_after_lifecycle_commits` | `PASS` |
| `AC-7.3-03#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_in_a_preseeded_marker_pair` | `PASS` |

### `AC-7.3-04`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_fault_removed_workflow_invocation --junitxml=artifacts/v9/7.3/AC-7.3-04.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.3-04#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.agents/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-04#0002` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.agents/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-04#0003` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.agents/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-04#0004` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.agents/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-04#0005` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-04#0006` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-04#0007` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-04#0008` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation[.claude/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-04#0009` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.agents/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-04#0010` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.agents/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-04#0011` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.agents/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-04#0012` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.agents/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-04#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-04#0014` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-04#0015` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-04#0016` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_inside_the_block[.claude/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-04#0017` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-04#0018` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-04#0019` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-04#0020` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_blocks_the_record` | `PASS` |

### `AC-7.3-05`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_fault_displaced_workflow_invocation --junitxml=artifacts/v9/7.3/AC-7.3-05.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.3-05#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.agents/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-05#0002` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.agents/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-05#0003` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.agents/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-05#0004` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.agents/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-05#0005` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-05#0006` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-05#0007` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-05#0008` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation[.claude/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-05#0009` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-05#0010` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-05#0011` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-05#0012` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[before-the-gate-heading]` | `PASS` |
| `AC-7.3-05#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[gate-heading-removed]` | `PASS` |
| `AC-7.3-05#0014` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[transition-moved-into-the-span]` | `PASS` |
| `AC-7.3-05#0015` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[second-block-after-the-transition]` | `PASS` |

### `AC-7.3-06`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocker_prevents_state_transition --junitxml=artifacts/v9/7.3/AC-7.3-06.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.3-06#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_when_generation_fails` | `PASS` |
| `AC-7.3-06#0002` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_when_generation_is_blocked[schemas-unavailable]` | `PASS` |
| `AC-7.3-06#0003` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_when_generation_is_blocked[git-fails]` | `PASS` |
| `AC-7.3-06#0004` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.agents/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-06#0005` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.agents/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-06#0006` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.agents/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-06#0007` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.agents/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-06#0008` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-06#0009` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-06#0010` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-06#0011` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[.claude/skills/bmad-code-review/steps/step-04-present.md]` | `PASS` |
| `AC-7.3-06#0012` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[render:.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-06#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[render:.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-06#0014` | `_bmad.scripts.tests.test_generate_story_record::test_v2_blocker_prevents_state_transition_on_every_surface[render:.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |

### `AC-7.3-07`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --format bundle --output-json docs/release-evidence/story-7.3-final-record-v2.json --output-markdown docs/release-evidence/story-7.3-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.3-07#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-7.3-07#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-7.3-07#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-7.3-07#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-7.3-07#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-7.3-07#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-7.3-07#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-7.3-07#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-7.3-07#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |
| `AC-7.3-07#0010` | `generator::acceptance-results-bound-to-candidate` | `PASS` |
| `AC-7.3-07#0011` | `generator::workflow-bodies-equal-acceptance-inputs` | `PASS` |
| `AC-7.3-07#0012` | `generator::predecessor-records-7.1-7.2-verified` | `PASS` |

## Story 7.3 workflow integration

- Story contract: `_bmad-output/planning-artifacts/v9/story-contracts/7.3.json`
- Story contract SHA-256: `82e27bd488dc3554b92282692127403843507de04e39b40cbe55ba9d5301d81e`

### Governed workflow bodies

| Path | SHA-256 |
| --- | --- |
| `.agents/skills/bmad-build-auto/step-04-review.md` | `aa37c2847a7b25c9f0181818a303f22b0c2738235d64f46530e893ee5613f892` |
| `.agents/skills/bmad-build/step-05-present.md` | `9f02e91b1f3d01998c5ec2fa7da7e82ffffa32c676865da2063234e27a2face6` |
| `.agents/skills/bmad-build/step-oneshot.md` | `e93a6cf69880e47df517643c50ff05574effd54d6c856c0fa857a9738707d73d` |
| `.agents/skills/bmad-code-review/steps/step-04-present.md` | `731613b8e42cd068a2974b80f4ac51ddf594c2b521c3883da9b86e00f1caa919` |
| `.claude/skills/bmad-build-auto/step-04-review.md` | `aa37c2847a7b25c9f0181818a303f22b0c2738235d64f46530e893ee5613f892` |
| `.claude/skills/bmad-build/step-05-present.md` | `9f02e91b1f3d01998c5ec2fa7da7e82ffffa32c676865da2063234e27a2face6` |
| `.claude/skills/bmad-build/step-oneshot.md` | `e93a6cf69880e47df517643c50ff05574effd54d6c856c0fa857a9738707d73d` |
| `.claude/skills/bmad-code-review/steps/step-04-present.md` | `731613b8e42cd068a2974b80f4ac51ddf594c2b521c3883da9b86e00f1caa919` |

### Predecessor records

| Story | Record | SHA-256 |
| --- | --- | --- |
| `7.1` | `docs/release-evidence/story-7.1-final-record-v2.json` | `0a4ede3074fca55853f2fd59f065677cacea3eb700491cbc87594e117557e2f9` |
| `7.2` | `docs/release-evidence/story-7.2-final-record-v2.json` | `063a71b65e69f73f34c69e50dc516794fab5dc5377b0d20f34c85ff274a3bd5f` |

## Fault injection

No fault-injection result is bound to this record.

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-7.3-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-7.3-final-record-v2.md` |

## Rollback boundary

remove only Story 7.3 workflow invocations, parity guard, fixtures, results, and records as one unit; retain Stories 7.1-7.2.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `7` | `7` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
