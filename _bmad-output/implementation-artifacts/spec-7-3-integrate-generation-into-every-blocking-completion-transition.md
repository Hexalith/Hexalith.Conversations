---
title: 'Integrate generation into every blocking completion transition'
type: 'feature'
created: '2026-09-29'
status: 'ready-for-dev'
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
- [ ] Both trees, four routes -- insert the identical block between `<!-- STORY-COMPLETION-GATE:BEGIN v1 -->` and `<!-- STORY-COMPLETION-GATE:END v1 -->`. It holds four things:
  - applicability: required whenever the story's v9 contract exists
  - `uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --format bundle --output-json <json> --output-markdown <md>`, with the paths from `finalRecord.paths`, requiring exit `0` and the exact summary
  - the record-only pair commit, then verbatim insertion between `STORY-FINAL-RECORD` markers and `--verify-inserted-record {spec_file}`
  - the blocker branch

  Also update the gate intros and remove the Story 7.2-only step-05 text.
- [ ] `_bmad/scripts/verify_story_completion_workflows.py` -- implement AC-01/02 per the matrix. Emit acceptance-result v1 with sorted `{path, sha256}` body inputs and a nonempty ledger.
- [ ] `generate_story_record.py` + v2 schema -- add four things. Keep every 7.1/7.2 test green.
  - the acceptance-result scenario reader: schema, IDs, command, `resultSemantics`, candidate, staleness
  - `--verify-inserted-record`
  - a 7.3 section binding body digests re-derived from the candidate, which must equal the AC inputs, plus the 7.1 and 7.2 record digests
  - codes and retention
- [ ] `test_generate_story_record.py` -- add the `v2_workflow_verifies_inserted_digest`, `v2_fault_removed_workflow_invocation`, `v2_fault_displaced_workflow_invocation`, and `v2_blocker_prevents_state_transition` selectors. The last one checks generator exits 1 and 2 on fixtures, plus each surface's blocker branch and no CI claim. Add verifier unit tests for every matrix row.
- [ ] `docs/runbooks/story-final-record-generation.md` -- cover the Story 7.3 surfaces, the rebinding, render twins, the block, the new codes, and the no-CI limitation.
- [ ] `docs/release-evidence/story-7.3-final-record-v2.{json,md}` -- generate at step 05 from the committed candidate, following the Story 7.2 completion pattern.

**Acceptance Criteria:**
- Given the integrated tree, when AC-7.3-01 through AC-7.3-06 run as declared, then each exits `0` with `PASS`.
- Given six passing results and the 7.1/7.2 records, when AC-7.3-07 runs twice, then both runs exit `0` with identical bytes, `7/7/0/0/0/0`, and bound body hashes. A rerun after the pair and lifecycle commits reproduces the pair.
- Given the change, when the Release Conformance project and the generator suite run, then both pass. The `_bmad/scripts` lane adds no failures over the baseline.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

This is one gate with one byte-identical span everywhere. Parity is byte equality of the spans, and placement is judged inside the existing C# gate span, so decoy text outside that span never counts. Twins are compared as rendered bytes, never written. Surfaces differ only outside the block, which is why the blocker branch is phrased to fit all four routes.

## Verification

**Commands:**
- `TMPDIR=/var/tmp uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- expected: all pass.
- AC-7.3-01…06 as declared, via `uv run --frozen --no-sync` -- expected: each exit `0`.
- `dotnet build Hexalith.Conversations.slnx -c Release -p:UseHexalithProjectReferences=true`, then run the Conformance test executable -- expected: zero failures.
- `python3 scripts/check-root-submodules.py --repository .` and `git diff --check` -- expected: clean.
