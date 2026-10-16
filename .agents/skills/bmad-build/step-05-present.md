---
---

# Step 5: Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- NEVER auto-push.

## INSTRUCTIONS

<!-- EVIDENCE-BOUNDARY-GATE:BEGIN v1 -->
When the spec explicitly requires evidence-boundary verification, run its effective
contract command before this governed transition. Story 10.3 uses
`python3 _bmad/scripts/verify_evidence_boundary.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --current-binding _bmad-output/planning-artifacts/story-10.3-current-binding-v1.json --baseline f272a32e2a5fa334d0001da08caf1e55b6e142a7 --candidate HEAD --verify-workflows --format json`.
Use `_bmad/scripts/evidence_boundary_current.py::transition_decision` to interpret the recorded result and exit.
Require the recorded result and exit to agree: `PASS`/`0` or explicit
`not-applicable`/`0` continues; record `not-applicable` distinctly.
`FAIL`/`1` and `BLOCKED`/`2` halt review, completion, and unattended finalization.
Report the exact command, exit, and sorted unique blocker codes; keep the spec
`in-progress` and never write `review` or `done` after failure.
For routine work without an explicit evidence requirement, follow
`docs/runbooks/current-change-validation.md` and its focused checks.
<!-- EVIDENCE-BOUNDARY-GATE:END v1 -->

### Final Record Generation Gate

<!-- STORY-COMPLETION-GATE:BEGIN v1 -->
**Story completion gate (v9 contract).** Run this gate before any `review` or `done` transition whenever `_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json` exists for the story, where `<story-id>` is the story's dotted `<epic>.<story>` number (for example `7.3` for story key `7-3-…`); the spec cannot opt out, and the legacy procedure above does not replace it. Routine work without such a contract skips this gate under the current-change policy. This gate runs only when this workflow invokes it: no CI job or hook enforces it.

For Story 10.3, select `_bmad-output/planning-artifacts/story-10.3-current-contract-v1.json`
and its approved current binding as the effective contract. On first generation,
commit exactly its source paths before measuring; unrelated staged user changes
remain outside that source scope. Its record-only publication contains the pair
and the seven exact bound receipt paths together. For an already published pair,
retain its committed receipts and run inserted-record verification; do not rerun
acceptance or regenerate against the later `HEAD`. The scoped evidence gate still
checks the later `HEAD` and its complete raw diff, including every gitlink.

1. For first generation, make the committed story candidate `HEAD`, with no other dirt. From the repository root, run every other scenario `command` of the contract through `uv run --frozen --no-sync`. Then run the generator, with the contract path as `<contract>` and its two `finalRecord.paths` entries as `<json>` and `<md>`:

   ```bash
   uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --format bundle --output-json <json> --output-markdown <md>
   ```

   Require exit `0`, and require the `summary` of the record printed on stdout to equal the contract's `finalRecord.summary` exactly.
2. For Story 10.3, the publication set is exactly nine files: `<json>`, `<md>`, and every one of the seven `evidenceWorkflowExecution.receipts` paths. Unless all nine equal their committed `HEAD` bytes, commit exactly those nine files together as a record-only commit. For other stories, unless `<json>` and `<md>` both already equal their committed `HEAD` bytes, commit exactly those two files as a record-only commit. Insert the bytes of `<md>` verbatim between the `<!-- STORY-FINAL-RECORD:BEGIN -->` and `<!-- STORY-FINAL-RECORD:END -->` lines of `{spec_file}`, replacing anything already between them. When the spec has no such pair, append a blank line, the begin line, the Markdown, and the end line at its end. Then require exit `0` from:

   ```bash
   uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --verify-inserted-record {spec_file}
   ```

   After the record-only commit, commit no source or gitlink change; a source change requires first withdrawing the inserted record region and committing removal of the entire publication set (all nine files for Story 10.3; both `<json>` and `<md>` for other stories), then restarting this gate from the new candidate.
3. Blocker branch: on any nonzero exit, summary mismatch, required commit that is not authorized, commit failure, or verification failure, keep or return `{spec_file}` and the story's sprint-status row to `in-progress`, and never write `review` or `done`. Report the exact command, its exit, and every stable blocker code, then HALT. Remediate the named condition and rerun; never hand-edit the pair, a result file, or the inserted region into agreement.
<!-- STORY-COMPLETION-GATE:END v1 -->

### Mark Spec Done

Change `{spec_file}` status to `done` in the frontmatter.

If `{story_key}` is not empty and `{{.implementation_artifacts}}/sprint-status.yaml` exists, read `[[bmad-snapshot:sync-sprint-status.md]]` with `{target_status}` = `review`.

### Commit and Complete

If version control is available and the tree is dirty, create a local commit with a conventional message derived from the spec title.

After committing a governed done transition, rerun its `--verify-inserted-record` command at the new `HEAD` and require exit `0`. On any other exit, apply the gate's blocker branch, committing the return to `in-progress` as a new validated Conventional Commit.


{workflow.open_spec}

### Display Summary

Display a very short completion summary — one or two sentences — including:

- What changed.
- The verification and review result, including whether anything was deferred.
- The commit hash, if one was created.

Do not list changed files, repeat details from the spec, or narrate the process unless the user asks.

Offer applicable next actions in one short line: when version control and a remote are available, create a pull request (and push first if needed); use `bmad-walkthrough`; or make another change.

Workflow complete.

## On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}
