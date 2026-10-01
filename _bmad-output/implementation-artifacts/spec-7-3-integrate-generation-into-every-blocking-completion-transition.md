---
title: 'Integrate generation into every blocking completion transition'
type: 'feature'
created: '2026-09-29'
status: 'done'
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

- [x] [Review][Patch] Accept `followup_review_recommended` as the final frontmatter field [_bmad/scripts/generate_story_record.py:4386]
- [x] [Review][Patch] Update frontmatter status in the code-review route when present [.agents/skills/bmad-code-review/steps/step-04-present.md:131]
- [x] [Review][Patch] Pin the eight governed workflow bodies to LF for parity [.gitattributes]
- [x] [Review][Patch] Allow route-owned post-gate lifecycle edits during candidate retention [_bmad/scripts/generate_story_record.py:4283]
- [x] [Review][Patch] Bind inserted-record verification to the designated story spec, retained candidate, and current contract [_bmad/scripts/generate_story_record.py:5184]
- [x] [Review][Patch] Propagate every verifier stable blocker code through generator failures [_bmad/scripts/generate_story_record.py:2771]
- [x] [Review][Patch] Validate every intervening retained-candidate commit instead of only the endpoint tree [_bmad/scripts/generate_story_record.py:4406]
- [x] [Review][Patch] Validate acceptance-ledger row IDs before re-keying them [_bmad/scripts/generate_story_record.py:3554]
- [x] [Review][Patch] Verify Story 7.2's predecessor link against the bound Story 7.1 record [_bmad/scripts/generate_story_record.py:4708]
- [x] [Review][Patch] Correct mode-aware operator diagnostics [_bmad/scripts/generate_story_record.py:2969]
- [x] [Review][Patch] Add a dirty working-tree Markdown verification case [_bmad/scripts/tests/test_generate_story_record.py:4563]
- [x] [Review][Patch] Add a duplicate acceptance-ledger subject regression case [_bmad/scripts/tests/test_generate_story_record.py:5337]
- [x] [Review][Patch] Retract an existing pair before committing a replacement source candidate in all eight preparation sections
- [x] [Review][Patch] Document inserted-verification dirt, candidate-finality, and gitlink diagnostics and remediation
- [x] [Review][Patch] Reject committed source changes and their later revert in inserted-verification regression coverage

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

#### Generator chunk (2026-09-30)

- [x] [Review][Patch] Re-raise a git failure while reading an acceptance input instead of reporting the result stale [_bmad/scripts/generate_story_record.py:3683]
- [x] [Review][Patch] Require one gate heading and one follower before accepting block placement [_bmad/scripts/verify_story_completion_workflows.py:566]
- [x] [Review][Patch] Use each retained story's record-region flag during inserted-record verification [_bmad/scripts/generate_story_record.py:4429]
- [x] [Review][Patch] Assert the Story 7.3 Markdown contract lines, predecessor rows, and acceptance-results sentence [_bmad/scripts/tests/test_generate_story_record.py:5619]
- [x] [Review][Patch] Fault-test an acceptance result whose story id or scenario id does not match [_bmad/scripts/tests/test_generate_story_record.py:5625]
- [x] [Review][Patch] Fault-test a passing acceptance result that still carries blockers [_bmad/scripts/tests/test_generate_story_record.py:5639]
- [x] [Review][Patch] Pin Story 7.2 retention against a final-record region and a followup field [_bmad/scripts/tests/test_generate_story_record.py:4083]
- [x] [Review][Patch] Assert the three Story 7.3 self-ledger subject strings [_bmad/scripts/tests/test_generate_story_record.py:5617]

##### Rejected (generator chunk)

- `false` — An out-of-order acceptance ledger leaves `category` as `passed`, but any finding raises before categories are counted or outputs are written, so that category cannot become a passing summary.
- `false` — Every Story 7.3 scenario expects `PASS`. Any other state takes the failure branch and stops the run; a contract that expected `FAIL` is outside this story and would fail closed.
- `false` — Acceptance freshness is measured against the retained candidate whose blobs are checked. A later lifecycle commit does not change those blobs, and a result older than the retained candidate is still rejected.
- `false` — The generator writes workflow bodies and predecessors from the fixed Story 7.3 lists before schema validation, so the looser schema cannot publish a swapped or duplicated pair.
- `false` — Masking `## Review Triage Log` and `## Auto Run Result` through the next heading is the retention rule for those route-owned sections.
- `false` — The routes and the tests write unquoted `followup_review_recommended: true|false`. A quoted value fails closed instead of being treated as a lifecycle edit.
- `false` — A sprint commit may change only this story's row and `last_updated`. Another row changing is `CANDIDATE_NOT_FINAL` by design.
- `false` — Generation measures a clean candidate. The spec insertion is the later `--verify-inserted-record` step, so a dirty spec during generation is `WORKTREE_NOT_CLEAN`.
- `false` — The appended-marker check matches the route's instruction to append one blank line, the begin line, the Markdown, and the end line.
- `false` — The contract blob is validated from the same candidate before workflow integration runs, so the empty-hash fallback is not reachable.
- `false` — `workflowIntegration` is specified as the contract, the eight bodies, and the 7.1/7.2 records. The verifier script must be committed; it is not a record field.
- `low` — A missing Story 7.1 digest also emits the Story 7.1 binding finding, so the extra Story 7.2 wording does not hide the failed predecessor.
- `low` — The empty-result and unrecognized-command sentences still name JUnit. The run already stops with the scenario's own blocker, and the wording does not let a bad result pass.
- `low` — A CRLF spec fails marker parsing. Workflow bodies are pinned to LF, and CRLF normalization is more than a direct correction.
- `low` — A result written earlier in the commit's own second can look fresh. Treating that whole second as stale would also reject a legitimate result written later in the same second.
- `low` — A governed body that is a symlink is `SURFACE_UNREADABLE`. Closing the check-to-read race is extra hardening, not a defect the steady state reaches.
- `low` — Required clauses are full phrases. Rejecting a phrase that is only a prefix of a longer token would add a second matcher the current phrases do not need.
- `low` — The output writer resolves an existing ancestor inside the repository before creating parents. A symlink swap after that check is not an everyday write.
- `low` — A read-back mismatch after replace is exceptional, and restoring the previous bytes would add a second write path for a case that already reports `OUTPUT_WRITE_FAILED`.

#### Evidence chunk (2026-10-01)

- [x] [Review][Patch] State that a lifecycle-only successor commit is an accepted acceptance-result stamp [docs/runbooks/story-final-record-generation.md:339]

##### Rejected (evidence chunk)

- `rejected: spec change` — Implementation Notes still say the pair is not generated. The standing description is stale, and the correction edits this spec.
- `rejected: spec change` — The Verification section still logs the earlier `CANDIDATE_NOT_FINAL` run. Replacing that log edits this spec.
- `rejected: spec change` — The unticked final-record task understates the retention allowlist. The box stays open because ticking it is outside retention, and the correction edits this spec.
- `rejected: spec change` — The Spec Change Log is empty. Filling it edits this spec.
- `rejected: spec change` — The Code Map still describes the pre-7.3 generator. Correcting it edits this spec.
- `rejected: spec change` — The pass-1 outcome says 18 patches; that table has 16 patch rows. Correcting the count edits this spec.
- `false` — The known-limitations bullet states that `bmad-dev-auto` is retired and names `bmad-build-auto/step-04-review.md`. Neither skill tree contains that route. The older deferred-work sentence was already left in place on pass 1.
- `low` — The acceptance writer already retries exclusive no-follow temporary names and reports `OUTPUT_WRITE_FAILED`. The pair-writer symlink is already the deferred-work entry in this diff, and restating that internal procedure is more than a direct correction.
- `rejected: spec change` — Review-log line numbers have drifted. Updating them edits this spec.
- `low` — The four extra displacement shapes run on one route, and the shared placement checker is already faulted on every body. The ledger names those tests; it does not claim the shapes ran on every route. Expanding the matrix is more than a direct correction.
- `false` — Acceptance results stay under gitignored `artifacts/v9/7.3/`. The record binds their digests, and the scratch logs under `/tmp` are not story evidence.
- `false` — Pass-1 row 7 is rejected because its fix adds guards. That evidence column is the rejection reason.
- `rejected: spec change` — The Files note still says 113 new tests. Correcting that snapshot edits this spec.

#### Code review (2026-10-01)

- [x] [Review][Decision] Dispose of the root gitlink moves inside Story 7.3 commits — `dbc7ed1` (five gitlinks) and `3824864` (four gitlinks) mix Story 7.3 source changes with moves of Builds, EventStore, FrontComposer, Projects, and Tenants, against the frozen "Never edit … gitlinks" rule. Triage row 62 attributes the moves only to `6183517`, and neither commit names the before and after IDs or the reason. Reverting those commits under the record's rollback boundary would also revert the bumps. Resolved by the owner (2026-10-01): owner-intended dependency bumps outside the story scope. Push CI run `36822427074` at `e935c27`, whose gitlinks equal `HEAD`'s, passed `build-and-test`, `aspire-tests`, `conformance`, and `repository`. Keep future gitlink bumps in separate `fix(references)` commits. (blind, acceptance, verification-gap Other)
- [x] [Review][Patch] Make code-review candidate preparation retract a committed pair before any findings, ledger, or source write [.agents/skills/bmad-code-review/steps/step-04-present.md:91] — owner decision (2026-10-01) for the retention-versus-review-writes finding: the route appends `### Review Findings` and `deferred-work.md` entries before its gate, retention masks neither, and the "replacing a source candidate" trigger does not clearly cover a findings-only write, so a findings-only commit returns `CANDIDATE_NOT_FINAL` and the story to `in-progress`. Fix the route wording in both trees and the runbook; leave the generator and the shared block unchanged. (blind, edge)
- [x] [Review][Patch] Add a retention regression for a lifecycle commit before the record-only commit [_bmad/scripts/generate_story_record.py:4563]
- [x] [Review][Patch] Cover a route-owned section that trails the inserted record at end of file [_bmad/scripts/generate_story_record.py:4369]
- [x] [Review][Patch] Fault-test an acceptance-result command whose script is not committed [_bmad/scripts/generate_story_record.py:5061]
- [x] [Review][Patch] Correct the known-limitation protection claim and require verifier reruns after BMAD upgrades [docs/runbooks/story-final-record-generation.md:997]
- [x] [Review][Patch] Document that each new contract needs retained-candidate generator entries before inserted verification [docs/runbooks/story-final-record-generation.md:1001]
- [x] [Review][Patch] File the Story 7.3 deferred entry under its own heading [_bmad-output/implementation-artifacts/deferred-work.md:406]
- [x] [Review][Patch] Remove the duplicated acceptance-code paragraph and rewrap the applicability line [docs/runbooks/story-final-record-generation.md:968]
- [x] [Review][Defer] The sprint-status V14 header still says the hold is active and IR-0 was not run [_bmad-output/implementation-artifacts/sprint-status.yaml:40] — deferred: pre-existing, and an unmasked sprint comment edit would break Story 7.3 candidate retention
- [x] [Review][Defer] The generator pair writer follows a predictable temporary symlink [_bmad/scripts/generate_story_record.py:4236] — deferred: pre-existing, already recorded in `deferred-work.md`

##### Rejected (code review 2026-10-01)

- `rejected: spec change` — Implementation Notes, the earlier `CANDIDATE_NOT_FINAL` log, the 113-test note, the 18-patch count, the empty Spec Change Log, and the unticked final-record task are stale. Correcting them edits this spec. (blind)
- `low` — `render_in_memory` copies `render_skill.render()` statement for statement up to `_publish`. Divergence needs a renderer change, which arrives with a BMAD upgrade that also rewrites the governed bodies and fails `AC-7.3-01`. A parity harness is more than a direct correction. (blind)
- `low` — Story 7.2 retention now shares the per-commit checks, the header-date mask, and the new `CANDIDATE_NOT_FINAL` message. Its real history (`262d954`, record-only `4011156`, lifecycle `55e76b1`) satisfies the stricter rules, and its bytes are unchanged. Only the spec's "unchanged" claim is false, and correcting it edits this spec. (blind, edge ×2, verification-gap Other, acceptance)
- `low` — Results under gitignored `artifacts/` are bound by digest, the same measured-artifact model as Stories 7.1 and 7.2 (carried row 91). The `/tmp` archive citations are spec text. (blind)
- `false` — The `v2_7_3_lifecycle` equality is a regression guard that fails if the generator ever writes lifecycle files on a `FAIL` or `BLOCKED` run. Prose refusal is covered by the clause checks (carried row 102). (blind)
- `false` — `passing = not findings and …` keeps an out-of-order ledger from `PASS`, and any finding stops the run before categories count (carried row 142). (blind)
- `low` — A route heading written directly after the END line with no blank line is not how the routes write Markdown; the blank-line layout passes. The fix adds a guard. (edge)
- `low` — Spec templates and routes do not produce a spec without a trailing LF. Supporting one adds a branch. (edge)
- `low` — A symlink loop under Python 3.11 still fails closed with exit `2` `INTERNAL_ERROR`. Naming it adds exception handling for a contrived input. (edge)
- `low` — No other contract declares the acceptance-result command shape. The 16.x commands use `--candidate HEAD` without `--contract` and stop at `INPUT_SCHEMA_INVALID` as v14 contracts, so an empty-input `PASS` is unreachable. The guard adds a branch. (edge)
- `low` — The acceptance auditor's rerun of `AC-7.3-07` at `c613f43` reproduced both outputs byte-identically. Recording it edits this spec. (acceptance)
- `low` — A `_bmad/scripts` lane rerun gives identical failure counts at the baseline and `HEAD`. Recording it edits this spec. (acceptance)
- `false` — Each cited fault test builds its own `tmp_path` repository through `build_v2_7_3_repository`, so no shared fixture or workspace file stays mutated. (acceptance)
- `false` — The record binds the story contract path and SHA-256 in `workflowIntegration`, plus the `V9-7.3-ENTRY-v1` inventory digest, as the accepted Story 7.2 record does. (acceptance)
- `false` — Push CI run `36822427074` at `e935c27`, whose root gitlinks equal `HEAD`'s, passed `ci / build-and-test` (Contracts, Client, core, Server, and Integration tests) and `ci / aspire-tests`, so the submodule-consuming test projects already ran against the moved gitlinks. (verification-gap)

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
- **Review patch follow-up.** Retained candidates now validate every intervening commit while allowing the lifecycle fields and sections written by the governed routes. Inserted-record verification is bound to the retained contract and its designated spec. Acceptance evidence preserves the verifier's complete stable-code set and validates producer ledger IDs and subjects before re-keying. Story 7.2 must bind the exact verified Story 7.1 digest. Mode-specific diagnostics and dirty-Markdown, reverted-intermediate-change, predecessor-link, ledger-ID, and duplicate-subject regressions cover the repaired boundaries.
- **Review pass 2 patch.** Record-pair commits are now isolated per commit, lifecycle rollback is accepted only through governed transitions, acceptance-result publication uses exclusive no-follow temporary files, and inserted-record verification rejects unrelated dirt or unmasked spec edits. The runbook now covers acceptance-result evidence and the v14 contract-schema prerequisite.
- **Resumed implementation audit.** Acceptance-result cleanup now removes only a temporary file successfully created by this invocation. Exhausting all exclusive-name retries leaves pre-existing regular files and symlinks intact; two regression cases cover that failure path.
- **Code review 2026-10-01 patches.** Both code-review trees now retract a committed pair before the route's first finding, `deferred-work.md`, or source write, and never stage those writes while the pair is committed; the generator and the shared block are unchanged. Three generator regressions are new: a lifecycle commit before the record-only commit is `CANDIDATE_NOT_FINAL`, a route-owned section trailing the inserted record at end of file is masked while an unowned one is rejected, and an acceptance command whose script is not committed is `SCENARIO_COMMAND_UNSUPPORTED`. Mutating each guarded line fails its test. The runbook corrects the protection claim, requires `AC-7.3-01`/`02` reruns after BMAD upgrades, documents per-contract retained-candidate entries and the code-review retraction, and drops the duplicate paragraph. The pair committed at `542b245` is superseded: `6fd9801` and these patches change non-lifecycle paths, so step 05 must retract it before committing the replacement candidate. The results it binds are archived in the gitignored `artifacts/v9/7.3-superseded-596d25e/`.
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

### 2026-09-30 — Review pass 2 (blind, edge-case, verification-gap)

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 33 | Retention permits the first pair commit to include lifecycle files (verification-gap) | medium | patch | Pre-verified with a combined pair/spec commit that passed both generation and inserted-record verification. Require the first commit carrying the pair to change exactly the two output paths. |
| 34 | A transition followed by blocker rollback to the candidate sprint value is rejected (edge) | medium | patch | `v2_sprint_status_only_change` compares every successor cumulatively to the candidate and requires a different final status, so a valid `in-progress → review → in-progress` sequence becomes `CANDIDATE_NOT_FINAL`. |
| 35 | An acceptance output can alias a tracked file (edge) | low | reject | carried: review-pass-1 row 30 rejected the same live behavior because contract commands are owner-frozen and the additional guard is disproportionate. |
| 36 | The verifier's predictable temporary path can be pre-created as a symlink (edge) | high | patch | `Path.open("wb")` follows an existing `.<name>.<pid>.tmp` symlink before `os.replace`, allowing an external target to be truncated. Use an exclusive no-follow temporary file in the destination directory. |
| 37 | The task note understates the lifecycle fields retention masks (edge claim) | low | reject | The implementation and runbook accurately name the route-owned fields and sections. The remaining mismatch is in this build's spec, and review findings whose fix edits this spec are rejected. |
| 38 | The existing committed pair is invalidated by later gitlink commits (blind) | false | reject | This resumed build deliberately leaves AC-7.3-07 and pair regeneration to step 05 after a new committed candidate; the old pair is not claimed as evidence for the uncommitted patch. |
| 39 | The existing final record predates the current review patches (blind) | false | reject | The spec explicitly labels the patch as an uncommitted candidate and leaves the final-record task unticked until step 05, so the old pair is expected to predate it. |
| 40 | Inserted-record verification ignores unrelated working-tree dirt (blind) | medium | patch | Verify mode checks only the pair copies and designated spec region. An uncommitted source or gitlink edit after the record-only commit can coexist with exit `0`; allow only the designated spec's expected lifecycle/record edit. |
| 41 | Inserted-record verification accepts arbitrary edits elsewhere in the spec (blind) | medium | patch | The working spec is not compared with the committed lifecycle-masked spec, so tasks or constraints can change while the inserted bytes still pass. Reuse the retention mask for the working spec. |
| 42 | Retention does not enforce a record-only first pair commit (blind) | medium | patch | Same reproduced root cause as row 33: cumulative allowed-path subtraction accepts a combined pair/lifecycle commit. |
| 43 | Successor commits can replace the pair with another self-consistent pair (blind) | low | reject | Lifecycle-path acceptance results may intentionally be rerun and rebound into a new deterministic pair. A hand-refinalized forged pair violates the workflow's measured-artifact trust model; proving execution provenance is outside this story and requires more than a direct fix. |
| 44 | Verify mode does not fully rederive Story 7.3 record semantics (blind) | low | reject | The frozen matrix scopes this mode to byte equality with the committed pair and `renderedMarkdownSha256`; the preceding generator invocation derives semantics. Full execution attestation is outside the accepted trust model and is not a direct correction. |
| 45 | Predecessor verification does not reconstruct all predecessor history (blind) | low | reject | The story requires committed schema-valid, digest-bound predecessor pairs and the exact 7.2→7.1 link, which the code checks. Re-running prior stories' full derivations is outside this story's trust boundary. |
| 46 | Acceptance-result `outputs` are not inspected (blind) | false | reject | Story 7.3 verifier results declare no measured outputs, and the generator neither copies nor consumes that generic field; it binds the result file, candidate inputs, and assertion ledger that this contract uses. |
| 47 | Acceptance result paths may alias tracked inputs (blind) | low | reject | carried: review-pass-1 row 30 rejected the same live behavior because the frozen contract owns the paths and the guard would add complexity for an unreachable normal route. |
| 48 | Uniform semantic negation can preserve parity substrings (blind) | low | reject | The verifier intentionally proves byte parity plus the owner-frozen required clauses, not arbitrary natural-language semantics. A canonical duplicate block would add a second source of truth for an unlikely malicious synchronized rewrite. |
| 49 | The route block omits a second generator invocation (blind) | false | reject | carried: review-pass-1 row 11 established that the frozen block has four required elements; the twice-run determinism check is Story 7.3's AC-07 completion evidence, not a clause required in every route. |
| 50 | Routes omit the Story 7.3 post-lifecycle rerun (blind) | false | reject | Runbook step 6 is the operator procedure for completing Story 7.3 itself. The governed route block must gate before transition and contains exactly the four frozen elements; post-transition evidence runs in step 05. |
| 51 | The generic v2 derivation and schema prose omits acceptance results (blind) | low | patch | The early runbook section still says all facts and dirt exceptions are JUnit-only and omits the separately loaded acceptance schema. Correct the documentation to include Story 7.3 acceptance evidence. |
| 52 | The backlog-contract limitation misstates v14 failures (blind) | low | patch | Contracts 12.1–16.3 carry `hexalith.conversations.v14-story-contract.v1`, while the generator first validates only the v9/v1 schema. Document that those contracts also need schema support before command readers. |

**Outcome.** No intent-gap or bad-spec entry. Five patch groups remain: record-only commit enforcement, rollback-compatible sprint retention, symlink-safe result publication, working-tree/spec validation in inserted-record mode, and two runbook corrections.


### 2026-09-30 — Resumed review (blind, edge-case, verification-gap)

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 53 | Final-field `followup_review_recommended` edits fail retention (blind) | medium | patch | The regex requires LF, but the frontmatter search bound excludes the last field's LF. Include that delimiter LF and cover the final-field lifecycle edit. |
| 54 | Code-review updates a body Status section that retention rejects (blind) | medium | patch | The route instructs a Status-section write while retained specs use frontmatter. Make both skill trees update frontmatter `status` when it exists, with the existing Status-section fallback for legacy stories. |
| 55 | Only Claude render twins are inspected (blind) | low | reject | The approved inventory names three canonical in-memory twins and eight mirrored bodies. Broken Agents render configuration stops workflow activation before completion; adding another render inventory and configuration guards exceeds a direct correction and is unlikely in the synchronized installation. |
| 56 | CRLF checkout breaks body/twin parity and committed-body digests (blind) | medium | patch | Body bytes preserve CRLF while renderer `read_text()` normalizes LF; the eight governed paths lack LF attributes. Pin those paths to LF and exercise an autocrlf checkout. |
| 57 | Inline marker mentions plus a valid pair fail record-region parsing (blind) | low | reject | The fixed governed spec carries a single marker occurrence for each marker, and strict rejection is fail-closed. Supporting additional inline occurrences needs expanded marker/mask grammar for an exceptional input rather than a direct correction. |
| 58 | A new Story 7.2 record insertion fails committed retention (blind) | low | reject | Confirmed by the two masks. Story 7.2 is already complete and its prior behavior and historical pair are explicitly preserved; reopening it through this gate would need additional retention support and its own record scope. That exceptional flow is not repaired by changing preserved 7.2 semantics here. |
| 59 | Generator pair writes still follow predictable temporary symlinks (blind) | high | defer | Reproduced with `v2_write_outputs`: a pre-created PID temporary symlink overwrote an unrelated temporary file. The pair/restoration writer is unchanged from the story baseline, so this is a pre-existing issue separate from the new verifier publisher. |
| 60 | Lifecycle-only retention ignores an executable-bit spec change (blind) | low | reject | Content masking does not compare modes, but an executable bit on this Markdown changes no reader or completion outcome. Adding mode guards for an exceptional cosmetic edit exceeds a direct correction. |
| 61 | The 7.3 schema permits duplicate logical inventory identities (blind) | low | reject | Generation constructs the fixed eight paths and exact 7.1/7.2 links from tooling constants and rejects mismatched inputs. The preceding generator is the semantic derivation gate; duplicating its inventory in schema guards adds a second source of truth for a forged-pair case outside the accepted measured-artifact model. |
| 62 | The review diff includes Builds/EventStore gitlink updates (blind) | false | reject | Those pointers were committed independently in `6183517f7d5983fd1bff25111d0f26f4926a0758` before this resumed run. The current story patch has no gitlink diff; preserving that user history is required. |
| 63 | Story 7.1 cannot use mandatory inserted-record verification (edge) | low | reject | A fixture confirms `ARGUMENT_INVALID`, but Story 7.1 is already complete and its pair remains historical. Reopening it would require new candidate retention and separately scoped record support rather than a direct correction; unsupported contracts fail closed. |
| 64 | Final-field `followup_review_recommended` edits fail retention (edge) | medium | patch | Same demonstrated boundary defect as row 53; retain this independent finding and fix the shared root cause once. |
| 65 | A new Story 7.2 insertion fails committed retention (edge) | low | reject | Same demonstrated exceptional reopening flow as row 58; preserve Story 7.2 behavior and its completed historical pair. Adding retention support is more than a direct correction. |
| 66 | The task note describes fewer lifecycle fields than retention permits (edge claim) | low | reject | carried: review-pass-2 row 37 rejected this exact spec-note mismatch. The implementation and runbook name the route-owned fields; the proposed fix edits this build's spec. |
| 67 | Story 7.1 cannot use mandatory inserted-record verification (verification-gap Other) | low | reject | The filed fixture reproduces the same exceptional reopening flow as row 63. Supporting it adds retention/record scope to an already completed preserved predecessor; it is not an everyday completion path. |
| 68 | Story 7.2 cannot retain a newly committed insertion (verification-gap Other) | low | reject | The filed fixture confirms row 58's mask discrepancy. This story intentionally preserves completed 7.2 behavior; reopening it needs separately scoped support and changes beyond a direct correction. |

**Outcome.** No intent-gap or bad-spec entry. Patch three root causes: final-field lifecycle masking, frontmatter status writes in code-review, and LF checkout policy for the governed bodies. Record the pre-existing generator pair-writer issue in the deferred-work ledger. The verification-gap layer reported no separate coverage gap. AC-7.3-07 remains incomplete pending authorized record retraction, a clean committed replacement candidate, and regenerated evidence.

### 2026-09-30 — Current resumed review (blind, edge-case, verification-gap)

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 69 | Universal inserted-record verification rejects Story 7.1 (blind) | low | reject | carried: rows 63 and 67 cover this preserved, already-completed predecessor. Supporting its exceptional reopening adds candidate-retention scope beyond a direct correction. |
| 70 | Story 7.2's committed insertion fails retention (blind) | low | reject | carried: rows 58, 65, and 68 cover the unchanged Story 7.2 mask. Its historical behavior and pair remain preserved; reopening it needs separately scoped support. |
| 71 | Candidate preparation commits source patches before explaining pair retraction (blind) | medium | patch | All four preparation sections commit the candidate before the gate's source-change/retraction rule. A resumed source patch with an existing pair therefore violates the instructed ordering. State the required record-only retraction before candidate preparation in both trees. |
| 72 | The included record binds older code-review body digests (blind) | false | reject | carried: rows 38 and 39 establish that this pair is superseded historical evidence, not evidence for the current candidate. Step 05 replaces it after review and a clean committed replacement candidate. |
| 73 | Inserted-verification diagnostics omit three reachable blockers (blind) | low | patch | The mode calls retained-candidate validation and checks outside-spec dirt, so it can emit WORKTREE_NOT_CLEAN, CANDIDATE_NOT_FINAL, and GITLINK_DRIFT. Its runbook paragraph omits those exits and their remedies; add them directly. |
| 74 | Bodies and render twins can be measured during concurrent editing (blind) | low | reject | The workflow requires a clean committed candidate, and generation checks dirt and committed input digests. Detecting transient edits between reads adds snapshot/race guards for an exceptional invocation outside that measured-artifact model. |
| 75 | Publication failure can leave installed or previous PASS bytes (blind) | low | reject | carried: row 26 rejects preserving a previously measured PASS as a defect; the invocation reports BLOCKED on publication failure. The post-replace readback failure is the separately recorded exceptional I/O case, whose transactional repair adds disproportionate complexity. |
| 76 | CI-claim heuristic also matches some negative wording (blind) | low | reject | carried: row 10 records the heuristic boundary. All governed blocks use the pinned no-enforcement clause that passes; supporting hypothetical alternative prose needs added parsing and guards rather than a direct correction. |
| 77 | The schema permits repeated logical inventory identities (blind) | low | reject | carried: row 61 establishes that generation constructs the fixed eight paths and exact predecessor links. Extra consumer/schema guards target forged pairs outside the accepted measured-artifact model. |
| 78 | Verbatim insertion conflicts with the prohibition on hand editing (blind) | false | reject | Step 2 explicitly instructs copying the generated Markdown bytes. Step 3 prohibits manually changing a pair, result, or region to force agreement; repeating the generated insertion procedure remains allowed. |
| 79 | Story 7.2's required committed insertion fails retention (edge) | low | reject | carried: rows 58, 65, and 68 cover the same preserved predecessor behavior; no new outcome or changed code was demonstrated. |
| 80 | The task note understates lifecycle fields accepted by retention (edge claim) | low | reject | carried: rows 37 and 66 reject this same spec-note mismatch. Implementation and runbook describe the route-owned fields; the proposed fix edits this build's spec. |
| 81 | Inserted-verification's committed-history guard lacks a regression test (verification-gap) | medium | patch | Pre-verified: bypassing only that mode's retained-candidate call leaves its 17 selected tests passing and accepts a valid inserted record after a committed source edit. Add a hermetic verification test that rejects the source commit and its later revert with CANDIDATE_NOT_FINAL. |

**Outcome.** No intent-gap or bad-spec entry. Patch candidate-preparation ordering, the inserted-verification diagnostic prose, and the missing committed-history regression. All thirteen findings retain individual verdicts; previously rejected predecessor and trust-model findings remain carried.

### 2026-10-01 — Review pass (blind, edge-case, verification-gap)

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 82 | Auto Run says the spec is `done` and the sprint row is `review`, while frontmatter is `in-review` and the row is `in-progress` (blind) | false | reject | carried: rows 2 and 50. `in-review` is this step-04 phase, Auto Run records the earlier completion, and runbook step 6 is the step-05 procedure. The correction edits this spec. |
| 83 | A `# last_updated` comment change is an unmasked sprint edit and retention reports `CANDIDATE_NOT_FINAL` (blind) | medium | patch | The candidate comment is `2026-09-25` and HEAD's is `2026-10-01`, while `last_updated:` stays `2026-09-30`. `v2_sprint_status_only_change` masks only `^last_updated:`, so that comment-only delta returns false. |
| 84 | The inserted record's gitlinks differ from the tree, so generation is `GITLINK_DRIFT` (blind) | false | reject | carried: rows 38, 62, and 72. The committed pair is the pre-regeneration record. Step 05 replaces it after a clean candidate; this patch does not claim those gitlinks are current. |
| 85 | Implementation Notes, the Spec Change Log, the unticked final-record task, and the 113-test Files note disagree with the inserted record (blind) | low | reject | The mismatch is in this spec. The correction edits it. |
| 86 | The code-review blocker never sets `record_gate_failed`, so a continued failure can set `done` (blind) | low | reject | The shared blocker already HALTs and forbids `review` or `done`. The flag is a safety net for continuing after HALT, which is not an everyday path, and adding that code-review variable to the byte-identical block is more than a direct correction. |
| 87 | build-auto success writes spec `done` and does not update the sprint row (blind) | false | reject | Baseline build-auto never synced sprint. Its Finalize still only writes spec `done`. The shared gate constrains the blocker, not that pre-existing success transition. |
| 88 | Code-review success writes sprint `done` while step-05 and oneshot write `review` (blind) | false | reject | Those targets pre-exist: code-review syncs `{new_status}`, and step-05 and oneshot sync `review`. This story did not change them. Runbook step 6 remains the Story 7.3 operator procedure. |
| 89 | Pair retraction removes the Story 7.2 keep-historical-pair instruction (blind) | false | reject | The frozen task deletes the Story 7.2-only step-05 text. The retraction sentence names the current story's `finalRecord.paths`. Deferred-work entries stay in place. |
| 90 | `workflowIntegration` does not pin the eight paths or the 7.1/7.2 identities (blind) | low | reject | carried: rows 61 and 77. Generation writes that fixed inventory. Extra schema guards target a forged pair outside the measured-artifact model. |
| 91 | The PASS record binds gitignored acceptance-result files that the diff does not add (blind) | false | reject | `artifacts/` is gitignored. The record binds those measured digests the way it binds JUnit; they are not committed story bytes. |
| 92 | The generator pair writer still follows a pre-created temporary symlink (blind) | high | defer | carried: row 59. `v2_write_outputs` still uses `Path.open("wb")`. The pair writer is the pre-existing deferred issue and is not patched or deferred again. |
| 93 | Review-log line citations have drifted (blind) | low | reject | Updating those citations edits this spec. |
| 94 | Deleting the record pair in a commit that also edits other paths returns HEAD and bypasses record-only retraction (edge) | false | reject | When both outputs are absent, `v2_retained_candidate` returns HEAD so the next run measures that tree. It does not keep the old record across the mixed deletion. |
| 95 | An acceptance output path under a root gitlink is written into the submodule (edge) | low | reject | Story 7.3 outputs stay under `artifacts/`. The frozen contract does not name a gitlink path, and a prefix guard adds a branch for a path this story does not declare. |
| 96 | No test checks that the workspace Story 7.3 record's gitlinks equal the tree (verification-gap) | false | reject | Pre-verified: no test compares them, and the five gitlink commits differ. That is the known pre-step-05 pair from rows 38 and 72. An equality assertion would fail the still-open final-record task, so the filed `patch` disposition is not taken. |

**Outcome.** No intent-gap or bad-spec entry. Patch the sprint comment-date mask. Carried gitlink, schema, predecessor, and pair-writer findings stay rejected or deferred as already logged.

### 2026-10-01 — Completion review (blind, edge-case, verification-gap)

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 97 | A manually re-finalized failing predecessor pair passes integrity verification (blind) | low | reject | The normal v2 generator raises before output when any scenario fails or the summary differs from the contract. This reproduction hand-refinalizes a pair outside the accepted measured-artifact model; adding semantic guards to that exceptional forged-pair path exceeds a direct correction. |
| 98 | Uniform summary negation preserves the verifier's required substrings (blind) | low | reject | carried: row 48 records the unchanged byte-parity and frozen-clause boundary. Arbitrary natural-language interpretation or a second canonical block would add scope for the same synchronized rewrite. |
| 99 | Mandatory inserted verification rejects Story 7.1 (blind) | low | reject | carried: rows 63, 67, and 69 cover the same preserved completed predecessor and its exceptional reopening. No retained-candidate behavior changed. |
| 100 | The shared insertion instruction conflicts with Story 7.2's preserved mask (blind) | low | reject | carried: rows 58, 65, 68, and 70 cover this unchanged completed predecessor. Supporting its reopening requires separately scoped retention support. |
| 101 | Appending after malformed or partial markers still fails insertion verification (blind) | low | reject | Strict malformed-marker rejection is intentional and covered by the insertion faults. Expanding recovery grammar for an already corrupt spec adds guards beyond a direct correction; the fixed Story 7.3 spec carries one valid pair. |
| 102 | AC-06 does not execute rollback from review/done or agent recovery after a commit failure (blind) | false | reject | AC-06 permits preservation of pre-review state, which the FAIL/BLOCKED fixtures assert byte-for-byte. Every surface checks the explicit commit/verification-failure HALT clause. The separate rollback regression executes done/review to in-progress commits; execution attestation for prose is outside the documented workflow boundary. |
| 103 | Retention prose omits the newly permitted sprint header-date correction (blind) | low | patch | The mask accepts the header comment and an isolated correction when last_updated is unchanged. Add a direct sentence to the runbook naming that exact rule. |
| 104 | The final-record task note understates retained lifecycle fields (blind) | low | reject | carried: rows 37, 66, and 80 reject the same spec-note mismatch. The runbook and implementation name the route-owned fields; the proposed fix edits this build's spec. |
| 105 | Implementation Notes still say the pair is not generated yet (blind) | low | reject | carried: row 85 covers the historical spec-note mismatch. The current replacement remains a step-05 task; rewriting the note edits this build's spec. |
| 106 | The historical Code Map describes pre-change entry points (blind) | low | reject | The map captured the approved starting tree. Implementation Notes describe the resulting entry points; the proposed correction edits this build's spec. |
| 107 | Story 7.1's supported contract stops at inserted verification (edge) | low | reject | carried: rows 63, 67, and 69 cover the same preserved predecessor. The filed reproduction adds no changed boundary. |
| 108 | Story 7.2's required insertion fails its retention mask (edge) | low | reject | carried: rows 58, 65, 68, and 70 cover the same unchanged completed predecessor flow. Its bytes and behavior remain explicitly preserved. |
| 109 | The task claim permits fewer lifecycle fields than the mask (edge claim) | low | reject | carried: rows 37, 66, and 80 cover the same claim. Updating that note edits this build's spec. |

**Outcome.** All thirteen findings are individually triaged. One direct runbook correction is applied; no implementation or spec loopback is needed. The verification-gap reviewer found no gaps. The existing deferred pair-writer issue is unchanged and is not deferred again.

### 2026-10-01 — Resumed build review (blind, edge-case, verification-gap)

| # | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 110 | The diff ships a superseded `PASS` pair, and the code review wrote findings while that pair was committed (blind) | false | reject | carried: rows 38, 39, 72, and 84. The pair at `542b245` is superseded historical evidence; step 05 retracts it and generates the replacement. The 2026-10-01 review predates the retract-first rule it asked for. |
| 111 | Retraction leaves the spec's inserted region in place and ignores a half-committed pair (blind) | low | reject | Insertion replaces anything between the markers, and retention masks that region, so the stale region lasts only until regeneration. A half pair fails loudly with `RECORD_CONTENT_DRIFT` ("output pair is incomplete"), and the record-only commit stages both files. Clearing the region adds a step to nine sections. |
| 112 | Implementation Notes, Auto Run Result, and the "Story 7.2 unchanged" note contradict the current state (blind) | low | reject | carried: rows 82, 85, and 105. The correction edits this spec. |
| 113 | AC-7.3-01/02 do not check the gate intros or retraction sentences, so the upgrade remediation that reinserts only the block leaves the gate unreachable (blind) | medium | patch | The verifier reads only the block, its placement, and its clauses. An upgrade that wipes the skill files removes the intros, which then skip the gate. The runbook now says the verifier checks only the block and requires reapplying every Story 7.3 route edit after an upgrade. |
| 114 | "run every other scenario `command`" can be read as alternate scenarios (blind) | low | reject | The next sentence runs the generator, so "every other" reads as "all the rest", and the Story 7.3 runs followed it correctly. Rewording changes the byte-identical block on eleven surfaces, which the owner kept unchanged. |
| 115 | Gate reruns re-measure results, so the "skip when identical" commit branch rarely applies and each rerun adds a record-only commit (blind) | low | reject | Fresh results are by design (pass-1 outcome), and retention accepts a successive record-only commit. Reusing results needs new logic, not a direct correction. |
| 116 | The code-review RULES bullet "always write findings to the story file" contradicts the section-2 no-write HALT (blind) | low | patch | Both bullets apply when the retraction is not authorized, which is an everyday unauthorized review of a done contract-bound story. Add the exception to the RULES bullet in both trees. |
| 117 | Code-review candidate preparation does not name the findings and deferred-work entries as candidate paths (blind) | medium | patch | Left out, they stay as dirt (`WORKTREE_NOT_CLEAN`) or land in the post-pair lifecycle commit. Retention masks neither, so later reruns stop with `CANDIDATE_NOT_FINAL`. Name them in both trees. |
| 118 | The three new regressions match no AC selector, so the record omits them (blind) | false | reject | The record binds the owner-frozen selectors. Most of the suite, including the earlier retention regressions, is outside them by design, and the Verification section runs the full suite. |
| 119 | No test run is recorded for the current working-tree changes (blind) | low | reject | The record belongs in this spec, and step 05 writes it. This review reran the suite: 316 passed. |
| 120 | The new deferred entry uses an absolute path, says 7.3 is in `review`, and gives a stale reason (blind) | low | reject | Deferred-work entries are not edited in place (row 15). The entry already allows the fix "within a replacement candidate". |
| 121 | There is no rollback guidance for the gitlink moves inside Story 7.3 commits, and the notes say the gitlinks were preserved (blind) | low | reject | The owner settled the gitlink disposition on 2026-10-01 in the Decision item. Rolling back is not an everyday path, and correcting the notes edits this spec. |
| 122 | The v2 schema hard-codes `workflowIntegration` to Story 7.3 (blind) | low | reject | Later contracts fail closed until their story extends the generator (rows 5 and 121 of the generator chunk). A generic schema redesign is more than a direct correction. |
| 123 | No test covers the incomplete or out-of-order marker guard in `locate_block` (verification-gap) | medium | patch | Pre-verified by mutation: with the guard deleted, all 85 Story 7.3 tests pass, a stray END line passes, and a removed END line is `INTERNAL_ERROR`. Add a marker-structure fault test under the AC-7.3-04 selector. |
| 124 | The runbook's "Story 7.2 completion repair: current gates" still describes the deleted step-05 Story 7.2 text in the present tense (verification-gap Other) | low | patch | This story removed that route text. Mark the section historical and point to the shared gate. |
| 125 | Code-review sprint sync updates `last_updated` when the row already equals `{new_status}` (edge) | low | patch | The per-commit check (`generate_story_record.py:4559`, `require_transition=True`) rejects a date-only sprint commit, so a clean re-review of a done contract-bound story leaves later reruns at `CANDIDATE_NOT_FINAL`. Row 27's premise, that every lifecycle commit changes the row, does not hold for this route. Leave the file unchanged when the row already matches, as `bmad-build` sync does. |
| 126 | Retention does not mask `review_loop_iteration` or a frontmatter line ending in a YAML comment (edge) | low | reject | Only Stories 7.2 and 7.3 are retained. Neither spec has a commented status line, and both have `review_loop_iteration: 0`, so a follow-up reset writes the same bytes. Loopbacks re-derive source, which already requires retraction. |
| 127 | The `bmad-build-auto` HALT writes `blocked`, which retention does not mask (edge) | low | reject | That needs a halted follow-up run on a done retained story with a committed pair. Resuming re-derives the work, and retention fails closed. Accepting `blocked` adds a state no route has been shown to reach. |
| 128 | Twin renders read ignored `_bmad/custom/*.user.toml` overrides (edge) | low | reject | carried: row 8. |
| 129 | A blocked verifier leaves an earlier `PASS` result on disk (edge) | low | reject | carried: rows 26 and 75. |
| 130 | A marker quoted in prose breaks record-region parsing (edge) | low | reject | carried: rows 16 and 57. |
| 131 | An acceptance result with empty `inputs` binds vacuously (edge) | low | reject | For Story 7.3 the generator requires each result's inputs to equal the eight candidate body digests. No other contract declares this command shape (code-review rejected list, 2026-10-01). |
| 132 | `--verify-inserted-record` without `--contract` falls into the v1 parser (edge) | low | reject | The block's verify command always passes `--contract`, and the v1 parser rejects the call. |
| 133 | A verifier run with a different `--repository` binds another checkout (edge) | false | reject | The generator checks the exact command, the candidate, and every input digest against candidate blobs. Another checkout either reproduces the candidate's bytes or fails as stale or drifted. |
| 134 | Code-review retraction triggers only when both pair files are committed (edge) | false | reject | A half pair is unreachable through the record-only commit and fails loudly with `RECORD_CONTENT_DRIFT` (`v2_retained_candidate`). |
| 135 | Removing the Story 7.2 keep-historical-pair sentence lets a rerun on Story 7.2 retract its pair (edge) | false | reject | carried: row 89, with rows 58 and 65 on reopening Story 7.2. |
| 136 | The runbook's Story 7.2 retention paragraph omits the header-date comment (edge) | low | patch | The shared sprint mask accepts the `# last_updated` header date. Add it to that paragraph. |
| 137 | The final-record task note understates the retention mask (edge claim) | low | reject | carried: rows 37, 66, 80, 104, and 109. |
| 138 | "No governed route creates a date-only sprint update" is false (edge claim) | low | patch | Same root cause as row 125, fixed by the same code-review sync change. |
| 139 | Twin verdicts depend on unbound render inputs (edge claim) | low | reject | carried: row 8 and row 128. |

**Outcome.** All thirty findings are individually triaged. No intent-gap or bad-spec entry, so no loopback. Seven patch groups (rows 113, 116, 117, 123, 124, 125/138, and 136) were applied by the implementation subagent: the upgrade remediation in the runbook, three code-review route corrections in both trees, a marker-structure fault test under the AC-7.3-04 selector, and two runbook corrections. No new deferred entry is needed, and the pair-writer issue that was already deferred is unchanged.

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

**Review-patch results (uncommitted candidate, 2026-09-30):**

- Focused review-patch regressions: 36 passed.
- Full generator suite: 290 passed, 0 failed, 0 skipped.
- AC-7.3-01 and AC-7.3-02: exit `0`, `PASS`. AC-7.3-03…06 selectors: 14, 20, 15, and 14 passed, each exit `0`.
- Release build: 0 warnings, 0 errors. Conformance executable: 473 total, 0 failed, 0 skipped.
- `check-root-submodules.py`: PASS. Python compilation and `git diff --check`: clean.
- AC-7.3-07 remains a post-candidate step; the existing pair and inserted record are not rewritten from an uncommitted tree.

**Review-pass-2 patch results (uncommitted candidate, 2026-09-30):**

- Full generator suite: 296 passed, 0 failed, 0 skipped.
- AC-7.3-01 and AC-7.3-02: exit `0`, `PASS`. AC-7.3-03…06 selectors: 14, 20, 15, and 14 passed, each exit `0`.
- Release build: 0 warnings, 0 errors. Conformance executable: 473 total, 0 failed, 0 skipped, 0 not run.
- `check-root-submodules.py`: PASS. Python compilation and `git diff --check`: clean.
- AC-7.3-07 and final-record regeneration remain blocked until a commit is authorized and a clean committed candidate exists.

**Resumed implementation validation (2026-09-30):**

- `TMPDIR=/var/tmp uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`: 298 passed, 0 failed, 0 skipped, exit `0`.
- AC-7.3-01 and AC-7.3-02: exit `0`, `PASS`, with 33 and 22 assertions. AC-7.3-03 through AC-7.3-06: exit `0`, with 14, 20, 15, and 14 passing tests. Every command ran exactly as declared through `uv run --frozen --no-sync` after the temporary-file cleanup fix.
- `dotnet build Hexalith.Conversations.slnx -c Release -p:UseHexalithProjectReferences=true`: exit `0`, 0 warnings, 0 errors.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -noLogo`: exit `0`, 473 total, 0 errors, 0 failed, 0 skipped, 0 not run.
- `python3 scripts/check-root-submodules.py --repository .`: `PASS`; Python compilation and `git diff --check`: exit `0`.
- Historical `_bmad/scripts` failures were not rerun; the baseline comparison above remains the evidence for that lane under the current-change policy.

**Current AC-7.3-07 result (2026-09-30):**

Exact command:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --format bundle --output-json docs/release-evidence/story-7.3-final-record-v2.json --output-markdown docs/release-evidence/story-7.3-final-record-v2.md
```

Exit `1`. Exact stdout:

```json
{
  "schemaVersion": "hexalith.conversations.story-record-generator-failure.v1",
  "result": "FAIL",
  "exitCode": 1,
  "storyId": "7.3",
  "blockers": [
    "CANDIDATE_NOT_FINAL",
    "GITLINK_DRIFT"
  ],
  "diagnostics": [
    {
      "code": "CANDIDATE_NOT_FINAL",
      "subject": "candidate.commit",
      "message": "commit 6183517f7d5983fd1bff25111d0f26f4926a0758 after the verified candidate changes source, gitlinks, or non-lifecycle state"
    },
    {
      "code": "GITLINK_DRIFT",
      "subject": "candidate.gitlinks",
      "message": "root gitlinks moved: references/Hexalith.Builds, references/Hexalith.EventStore"
    }
  ]
}
```

Both existing output files remained byte-identical. The committed pair pins candidate
`450bd270a1fdb1a632cbda0cf271107de97fee69`; committing the current patches alone
cannot rebaseline it. Recovery requires an authorized commit removing both Story 7.3
outputs before a clean replacement candidate can be measured. Preserve the superseded
pair and its evidence, rerun AC-7.3-01 through AC-7.3-06 at that candidate, run
AC-7.3-07 twice with identical bytes, commit exactly the new pair, insert and verify
the Markdown, then commit lifecycle bookkeeping and confirm reproduction. No commit
authorization is present in this session, so the existing pair and lifecycle state
remain unchanged and the final-record task remains incomplete.

**Resumed review patch verification (2026-09-30):**

- Review patches: the followup-field mask now includes the delimiter LF; both code-review trees prefer frontmatter `status`; the eight governed bodies are pinned to LF. The temporary publisher also preserves pre-existing collision paths when all retries are exhausted.
- `TMPDIR=/var/tmp uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`: exit `0`, 302 passed, 0 failed or skipped, 134.24 seconds.
- AC-7.3-01/02 exact contract commands through pinned `uv`: exit `0`, `PASS`, 33/22 assertions. AC-7.3-03 through AC-7.3-06: exit `0`, 14/20/15/14 passing tests, with logs in `/tmp/bmad-7-3-final-checks-3ntfstx9`.
- Release Conformance rerun: exit `0`, 473 total, 0 errors, failures, skipped, or not run. The earlier Release solution build remains passing with 0 warnings/errors; these patches change Python, workflow prose, and checkout metadata only.
- `python3 scripts/check-root-submodules.py --repository .`: `PASS`. In-memory Python compilation and `git diff --check`: exit `0`.
- Matrix audit: all integrated, removed, displaced, parity-drift, inserted-record, and unreadable-input tests ran in the 302-test suite. The exact contract scenarios additionally reran the insertion, removed/displaced invocation, and blocker-transition selectors. No matrix row is untested or skipped.
- Final AC-7.3-07 retry uses the exact command recorded above and still exits `1`, `FAIL`, with `CANDIDATE_NOT_FINAL` and `GITLINK_DRIFT`. Both existing record outputs remain byte-identical. Archived pair/results and command/stdout evidence: `/tmp/bmad-7-3-before-completion-saib54tt`.
- All 16 resumed review findings are triaged above; three patch groups are fixed. The unchanged generator pair/restoration temporary-file vulnerability is recorded as pre-existing deferred work. No historical-authority lane was rerun under the current-change policy.
- Commit preparation: exact full messages for retraction, candidate, record-only publication, and lifecycle commits all passed the pinned CLI (`node_modules/.bin/commitlint --config commitlint.config.mjs --edit <message-file>`), with evidence in `/tmp/bmad-7-3-commit-plan-o_p2ix07/commitlint-validation.txt`. No files are staged and no commit is created.
- Completion remains `in-progress`: the required local commit sequence has not been authorized. First retract the superseded 7.3 pair in its own commit, then commit the nine reviewed patch paths, rerun the six scenarios on that clean candidate, generate AC-07 twice, commit only the replacement pair, insert/verify it, and commit the spec/sprint lifecycle update. The pending final-record task intentionally stays unticked before the candidate commit.

**Current resumed validation after review fixes (2026-09-30):**

- The three current review patch groups are complete. All eight completion-gate blocks remain byte-identical to their committed pre-patch bytes; candidate preparation now places record-only retraction before a replacement source-candidate commit.
- `TMPDIR=/var/tmp uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`: exit `0`, 303 passed, 0 failed or skipped, 157.76 seconds. The new test exercises inserted-record verification after both a forbidden source commit and its subsequent revert, preserves the pair bytes, and restores the fixture.
- Exact AC-7.3-01/02 contract commands through pinned `uv`: exit `0`, `PASS`, 33/22 assertions. AC-7.3-03 through AC-7.3-06: exit `0`, 15/20/15/14 passing tests. Command/stdout evidence is archived in `/tmp/bmad-7-3-completion-plan-_6ym3ov4/pre-candidate-checks`.
- `dotnet build Hexalith.Conversations.slnx -c Release -p:UseHexalithProjectReferences=true`: exit `0`, 0 warnings or errors. `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -noLogo`: exit `0`, 473 total, 0 errors, failures, skipped, or not run.
- `python3 scripts/check-root-submodules.py --repository .`: `PASS`; `git diff --check`: exit `0`. Every frozen matrix row ran and passed. Historical-authority lanes were not rerun under the current-change policy.
- All thirteen current review findings are individually triaged above. Three fixes were applied, no intent or spec loopback was needed, and the previously deferred pair-writer issue remains unchanged.
- The superseded pair, committed spec/sprint bytes, and measured results are archived in `/tmp/bmad-7-3-completion-plan-_6ym3ov4`. The required Story 7.3 local completion sequence retracts only that pair, commits this reviewed candidate, reruns all six prerequisites, generates AC-07 twice, publishes only the replacement pair, inserts/verifies it, and records the lifecycle transition. No root gitlink or predecessor record changes belong to this sequence.

**Current completion validation (2026-10-01):**

- The implementation was already present. This resumed run reviewed all three lenses and recorded thirteen individual findings; one direct runbook correction documents the sprint header-date mask. No new deferred work or implementation loopback was required.
- `TMPDIR=/var/tmp uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`: exit `0`, 311 passed, 0 failures or skips, 207.61 seconds after the runbook correction. All six matrix rows are covered by passing executed tests.
- `dotnet build Hexalith.Conversations.slnx -c Release -p:UseHexalithProjectReferences=true /nr:false /m:1`: exit `0`, 0 warnings or errors. The built Conformance executable with `-noLogo`: exit `0`, 473 total, 0 errors, failures, skips, or not run.
- `python3 scripts/check-root-submodules.py --repository .` and `git diff --check`: exit `0`. The focused runbook regression also passed.
- Initial AC-7.3-01 through AC-7.3-06 all passed, with 33/22 verifier assertions and 15/20/16/14 selected tests. The old pair correctly failed AC-7.3-07 with `CANDIDATE_NOT_FINAL` and `GITLINK_DRIFT`; those old outputs were preserved before the required record-only retraction.
- Every scenario must rerun on the clean replacement candidate. The final-record task remains intentionally unticked in that candidate; the generated region and lifecycle commit carry its completion evidence.
- Prior pairs, acceptance artifacts, exact-message commitlint evidence, and verification logs are archived in `/tmp/bmad-7-3-finalize-lgwj5xvq`. Root gitlinks and the Story 7.1/7.2 pairs are preserved.

**Code review patch validation (2026-10-01):**

- This run applied the eight open patches from the 2026-10-01 code review, then reviewed the result in three layers (rows 110–139) and applied seven more patch groups. The generator and the shared completion-gate block are unchanged.
- `TMPDIR=/var/tmp/... uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`: exit `0`, 340 passed, 0 failures or skips. The covering tests for all six matrix rows ran and passed.
- The `AC-7.3-01` and `AC-7.3-02` contract commands, run through `uv run --frozen --no-sync`, exit `0` with `PASS` (33 and 22 assertions). `AC-7.3-03` through `AC-7.3-06` exit `0` with 15, 44, 16, and 14 passing tests. `AC-7.3-04` grew from 20 because the new marker-structure fault test runs under its selector.
- The built Release Conformance executable (`-noLogo`) exits `0`: 473 total, 0 errors, failures, skips, or not run. An earlier run with `TMPDIR` exported had one `RUN_RECEIPT_RUNNER_MISMATCH`, because that historical receipt test builds its expected command from `Path.GetTempPath()`. With the default temporary directory the run is clean. The Release solution was not rebuilt: since its last build only Markdown and Python changed, and `HEAD` did not move.
- `python3 scripts/check-root-submodules.py --repository .` returns `PASS`, and `git diff --check` exits `0`.
- The committed pair at `542b245` is superseded. Step 05 retracts it, commits this candidate, reruns all six scenarios there, and runs `AC-7.3-07` twice. It then commits only the replacement pair, inserts and verifies it, and records the lifecycle transition.

## Auto Run Result

- The existing Story 7.3 implementation was verified without source edits. Thirteen review findings were individually triaged; one direct runbook correction documents the permitted sprint header-date correction. No new deferred entry was required; the pre-existing pair-writer issue remains unchanged.
- Validation: 311 generator tests and 473 Conformance tests passed, with zero failures or skips. The Release solution build passed with zero warnings or errors. Every matrix row ran and passed.
- Replacement candidate: `596d25e076819ec7e24388918464f60c7b505758`. AC-7.3-01 through AC-7.3-06 passed on that clean candidate with 33/22 verifier assertions and 15/20/16/14 selected tests.
- AC-7.3-07 passed twice with identical JSON and Markdown and summary `7/7/0/0/0/0`. The pair was committed separately, inserted verbatim, and verified with exit `0` before the lifecycle transition.
- Inserted Markdown SHA-256: `4cffd2b6def4755fad137b851a751c421ac7fe292986e2eb58cd9e221e6c8c70`. The intentionally unticked final-record task is evidenced by the generated region and lifecycle transition.
- Lifecycle: spec `done`, Story 7.3 sprint row `review`. Root gitlinks and Story 7.1/7.2 record bytes are preserved. Final evidence and exact commitlint validation remain archived in `/tmp/bmad-7-3-finalize-lgwj5xvq`.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 7.3 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate, measured JUnit results, and acceptance results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `7.3`
- Candidate: `854e8557d7fd8e66e0572014310450fd9353aa4e`
- JSON content SHA-256 (all three digest fields zeroed): `0e4856bfc06eb0f1ac70a592015eaeccf9410dda397ff28e7706ea6dce4888b4`

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
| `references/Hexalith.Builds` | `160000` | `212583e08c7b6db22c7ccee881ad11699e1f7522` |
| `references/Hexalith.Commons` | `160000` | `53f7961b517becde5b84ed4d20fe696b849b5cd9` |
| `references/Hexalith.EventStore` | `160000` | `6dededdecd62dd6dc6d1f15810108d860ec70c8f` |
| `references/Hexalith.Folders` | `160000` | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` |
| `references/Hexalith.FrontComposer` | `160000` | `48f7dfef920e8217e5c6221f364f0a1e0f61f387` |
| `references/Hexalith.Memories` | `160000` | `289773387e0c6b665b669031616c1e84cc079297` |
| `references/Hexalith.Parties` | `160000` | `60b9836ea23151c5319dd06fd3deb80122f7abc3` |
| `references/Hexalith.Projects` | `160000` | `4d8dcf65803792f7def3b10ed21227329536154b` |
| `references/Hexalith.Tenants` | `160000` | `54ceb3e1d50d3fc7bf1846e08cd2835828adfcdf` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-7.3-ENTRY-v1` | `ca106f6ad40f3a2ca580358d74a1565c34611821bc1d25e51694972d46ae8ca2` |

## Predecessors

- `7.2`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-7.3-01` | `0` | `PASS` | `none` | `33` | `artifacts/v9/7.3/AC-7.3-01.json` | `e3a9429e0ffc1c5abcaeade630e393af42ed70e264ebbc9f76cc4b6f1bf4ffb3` |
| `AC-7.3-02` | `0` | `PASS` | `none` | `22` | `artifacts/v9/7.3/AC-7.3-02.json` | `59c2d3187c2c87bbd3ff634c4bc5824aff7609fd0c45d43a48b95dec5ac694ed` |
| `AC-7.3-03` | `0` | `PASS` | `none` | `15` | `artifacts/v9/7.3/AC-7.3-03.xml` | `eb180d608d45dd9a52bf855081897cd2ec4f58adcd0f7c903f2bfafef8f260b1` |
| `AC-7.3-04` | `0` | `PASS` | `none` | `44` | `artifacts/v9/7.3/AC-7.3-04.xml` | `f426c21be6c61bfc0b753d1fa6718b6e97d89eee438f8d1cf55e03563b6639af` |
| `AC-7.3-05` | `0` | `PASS` | `none` | `16` | `artifacts/v9/7.3/AC-7.3-05.xml` | `b6d93d8b318ede7cf83d9bb72d7b8d8f8af67b19f65d301a770d4c814724b32d` |
| `AC-7.3-06` | `0` | `PASS` | `none` | `14` | `artifacts/v9/7.3/AC-7.3-06.xml` | `f1141e42d1a49a09ead4fda37e00a8b52ad28eec4c5347adde29a31d5a5307f6` |
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
| `AC-7.3-03#0010` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[edited-working-tree-markdown]` | `PASS` |
| `AC-7.3-03#0011` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[inconsistent-committed-json]` | `PASS` |
| `AC-7.3-03#0012` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_malformed_insertions[foreign-story-committed-json]` | `PASS` |
| `AC-7.3-03#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_and_reproduces_the_pair_after_lifecycle_commits` | `PASS` |
| `AC-7.3-03#0014` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_rejects_committed_source_changes` | `PASS` |
| `AC-7.3-03#0015` | `_bmad.scripts.tests.test_generate_story_record::test_v2_workflow_verifies_inserted_digest_in_a_preseeded_marker_pair` | `PASS` |

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
| `AC-7.3-04#0017` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build-auto/step-04-review.md-end-removed]` | `PASS` |
| `AC-7.3-04#0018` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build-auto/step-04-review.md-stray-end]` | `PASS` |
| `AC-7.3-04#0019` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build-auto/step-04-review.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0020` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build/step-05-present.md-end-removed]` | `PASS` |
| `AC-7.3-04#0021` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build/step-05-present.md-stray-end]` | `PASS` |
| `AC-7.3-04#0022` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build/step-05-present.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0023` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build/step-oneshot.md-end-removed]` | `PASS` |
| `AC-7.3-04#0024` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build/step-oneshot.md-stray-end]` | `PASS` |
| `AC-7.3-04#0025` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-build/step-oneshot.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0026` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-code-review/steps/step-04-present.md-end-removed]` | `PASS` |
| `AC-7.3-04#0027` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-code-review/steps/step-04-present.md-stray-end]` | `PASS` |
| `AC-7.3-04#0028` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.agents/skills/bmad-code-review/steps/step-04-present.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0029` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build-auto/step-04-review.md-end-removed]` | `PASS` |
| `AC-7.3-04#0030` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build-auto/step-04-review.md-stray-end]` | `PASS` |
| `AC-7.3-04#0031` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build-auto/step-04-review.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0032` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build/step-05-present.md-end-removed]` | `PASS` |
| `AC-7.3-04#0033` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build/step-05-present.md-stray-end]` | `PASS` |
| `AC-7.3-04#0034` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build/step-05-present.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0035` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build/step-oneshot.md-end-removed]` | `PASS` |
| `AC-7.3-04#0036` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build/step-oneshot.md-stray-end]` | `PASS` |
| `AC-7.3-04#0037` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-build/step-oneshot.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0038` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-code-review/steps/step-04-present.md-end-removed]` | `PASS` |
| `AC-7.3-04#0039` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-code-review/steps/step-04-present.md-stray-end]` | `PASS` |
| `AC-7.3-04#0040` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_marker_structure[.claude/skills/bmad-code-review/steps/step-04-present.md-end-before-begin]` | `PASS` |
| `AC-7.3-04#0041` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build-auto/step-04-review.md]` | `PASS` |
| `AC-7.3-04#0042` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build/step-05-present.md]` | `PASS` |
| `AC-7.3-04#0043` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_in_a_render_twin[render:.claude/skills/bmad-build/step-oneshot.md]` | `PASS` |
| `AC-7.3-04#0044` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_removed_workflow_invocation_blocks_the_record` | `PASS` |

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
| `AC-7.3-05#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[decoy-gate-heading]` | `PASS` |
| `AC-7.3-05#0014` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[gate-heading-removed]` | `PASS` |
| `AC-7.3-05#0015` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[transition-moved-into-the-span]` | `PASS` |
| `AC-7.3-05#0016` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_displaced_workflow_invocation_variants[second-block-after-the-transition]` | `PASS` |

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
| `.agents/skills/bmad-build-auto/step-04-review.md` | `1d955fc3dc2802794ecfe364e5bfdb7e8a0e4c8d1112ee449e5c2d12c447829e` |
| `.agents/skills/bmad-build/step-05-present.md` | `f7c639c622ea34ddea02fccce4076d8c0b4097140a85fb8977f392f61979e8bf` |
| `.agents/skills/bmad-build/step-oneshot.md` | `1d66bd73710fba380e65be00a0e469eec022b037368c9aea908d36a7c017cff7` |
| `.agents/skills/bmad-code-review/steps/step-04-present.md` | `6a4d9eea14b1655454112330158e2b484eefe9d993ad0cba924802b6f9db87a1` |
| `.claude/skills/bmad-build-auto/step-04-review.md` | `1d955fc3dc2802794ecfe364e5bfdb7e8a0e4c8d1112ee449e5c2d12c447829e` |
| `.claude/skills/bmad-build/step-05-present.md` | `f7c639c622ea34ddea02fccce4076d8c0b4097140a85fb8977f392f61979e8bf` |
| `.claude/skills/bmad-build/step-oneshot.md` | `1d66bd73710fba380e65be00a0e469eec022b037368c9aea908d36a7c017cff7` |
| `.claude/skills/bmad-code-review/steps/step-04-present.md` | `6a4d9eea14b1655454112330158e2b484eefe9d993ad0cba924802b6f9db87a1` |

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
