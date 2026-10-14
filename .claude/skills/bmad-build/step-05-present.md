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
`python3 _bmad/scripts/verify_evidence_boundary.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --current-binding _bmad-output/planning-artifacts/story-10.3-current-binding-v1.json --baseline 7f24b72d57da65745cdecdea5b5e7c0da1313b4d --candidate HEAD --verify-workflows --format json`.
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
For a story with `_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json`,
the spec cannot opt out. For Story 10.3 use the additive effective contract
`_bmad-output/planning-artifacts/story-10.3-current-contract-v1.json`.
Read `finalRecord.paths` and `finalRecord.summary` from the effective contract.
Run `uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --format bundle --output-json <json> --output-markdown <md>`.
Require exit `0` and the exact `finalRecord.summary` before completing the story.
Retain the pair in a record-only commit. Insert its Markdown between
`<!-- STORY-FINAL-RECORD:BEGIN -->` and `<!-- STORY-FINAL-RECORD:END -->` in `{spec_file}`.
Run `uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --verify-inserted-record {spec_file}`.
On any failure return the spec to `in-progress`; never write `review` or `done`.
Report the exact command, its exit, and every stable blocker code, then HALT.
This is a workflow guard; no CI job or hook enforces it.
<!-- STORY-COMPLETION-GATE:END v1 -->

### Mark Spec Done

Change `{spec_file}` status to `done` in the frontmatter.

If `{story_key}` is not empty and `{{.implementation_artifacts}}/sprint-status.yaml` exists, read `[[bmad-snapshot:sync-sprint-status.md]]` with `{target_status}` = `review`.

### Commit and Complete

If version control is available and the tree is dirty, create a local commit with a conventional message derived from the spec title.

{workflow.open_spec}

### Display Summary

Display a very short completion summary — one or two sentences — including:

- What changed.
- The verification and review result, including whether anything was deferred.
- The commit hash, if one was created.

Do not list changed files, repeat details from the spec, or narrate the process unless the user asks.

Offer applicable next actions in one short line: when version control and a remote are available, create a pull request (and push first if needed); use `bmad-walkthrough`; or make another change.

Workflow complete.

After committing a governed done transition, rerun its `--verify-inserted-record` command at the new `HEAD` and require exit `0`. On any other exit, apply the gate's blocker branch, committing the return to `in-progress` as a new validated Conventional Commit.

## On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}
