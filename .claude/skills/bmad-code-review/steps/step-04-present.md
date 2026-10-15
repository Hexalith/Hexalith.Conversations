---
deferred_work_file: '{implementation_artifacts}/deferred-work.md'
---

# Step 4: Present and Act

## RULES

- YOU MUST ALWAYS SPEAK OUTPUT in your Agent communication style with the config `{communication_language}`
- When `spec_file` is set, always write findings to the story file before offering action choices.
- `decision-needed` findings must be resolved before handling `patch` findings.

## INSTRUCTIONS

### 1. Clean review shortcut

If zero findings remain after triage (all rejected or none raised): state that and proceed to section 6 (Sprint Status Update).

### 2. Write findings to the story file

If `{spec_file}` exists and contains a Tasks/Subtasks section, append a `### Review Findings` subsection. Write all findings in this order:

1. **`decision-needed`** findings (unchecked):
   `- [ ] [Review][Decision] <Title> — <Detail>`

2. **`patch`** findings (unchecked):
   `- [ ] [Review][Patch] <Title> [<file>:<line>]`

3. **`defer`** findings (checked off, marked deferred):
   `- [x] [Review][Defer] <Title> [<file>:<line>] — deferred: <pre-existing, or for maybe-false the evidence that would settle it>`

Also append each `defer` finding to `{deferred_work_file}` under a heading `## Deferred from: code review ({date})`. If `spec_file` is set, include its basename in the heading (e.g., `code review of story-3.3 (2026-03-18)`). One bullet per finding with description.

### 3. Present summary

Announce what was written:

> **Code review complete.** <D> `decision-needed`, <P> `patch`, <W> `defer`, <R> rejected.

The findings report ends with a `Rejected` appendix — one line per rejected finding: `false` with its refutation, `low` with why it was not worth fixing — in the story file's `### Review Findings` section when `spec_file` is set, at the tail of the chat listing otherwise.

If `spec_file` is set, add: `Findings written to the review findings section in {spec_file}.`
Otherwise add: `Findings are listed above. No story file was provided, so nothing was persisted.`

### 4. Resolve decision-needed findings

If `decision_needed` findings exist, present each one with its detail and the options available. The user must decide — the correct fix is ambiguous without their input. Walk through each finding (or batch related ones) and get the user's call. Once resolved, each becomes a `patch`, `defer`, or is rejected.

If the user chooses to defer, ask: Quick one-line reason for deferring this item? (helps future reviews): — then append that reason to both the story file bullet and the `{deferred_work_file}` entry.

**HALT** — I am waiting for your numbered choice. Reply with only the number. Do not proceed until you select an option.

### 5. Handle `patch` findings

If `patch` findings exist (including any resolved from step 4), HALT. Ask the user:

If `spec_file` is set, present all three options:

> **How would you like to handle the `<P>` `patch` findings?**
> 1. **Apply every patch** — fix all of them now, no per-finding confirmation. Defer and decision-needed items are not touched.
> 2. **Leave as action items** — they are already in the story file
> 3. **Walk through each patch** — show details for each before deciding

If `spec_file` is **not** set, present only options 1 and 2 (omit "Leave as action items" — findings were not written to a file):

> **How would you like to handle the `<P>` `patch` findings?**
> 1. **Apply every patch** — fix all of them now, no per-finding confirmation. Defer and decision-needed items are not touched.
> 2. **Walk through each patch** — show details for each before deciding

**HALT** — I am waiting for your numbered choice. Reply with only the number. Do not proceed until you select an option.

- **Apply every patch**: Apply every patch finding without per-finding confirmation. Do not modify defer or decision-needed items. After all patches are applied, present a summary of changes made. If `spec_file` is set, check off the patch items in the story file (leave defer items as-is).
- **Leave as action items** (only when `spec_file` is set): Done — findings are already written to the story.
- **Walk through each patch**: Present each finding with full detail, diff context, and suggested fix. After walkthrough, re-offer the applicable options above.

  **HALT** — I am waiting for your numbered choice. Do not proceed until you select an option.

**✅ Code review actions complete**

- Decision-needed resolved: <D>
- Patches handled: <P>
- Deferred: <W>
- Rejected: <R>

### 6. Update story status and sync sprint tracking

Skip this section if `spec_file` is not set.

<!-- EVIDENCE-BOUNDARY-GATE:BEGIN v1 -->
When the spec explicitly requires evidence-boundary verification, run its effective
contract command before this governed transition. Story 10.3 uses
`python3 _bmad/scripts/verify_evidence_boundary.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --current-binding _bmad-output/planning-artifacts/story-10.3-current-binding-v1.json --baseline 3319e062472bb44fda8389a2420af108da58da0a --candidate HEAD --verify-workflows --format json`.
Use `_bmad/scripts/evidence_boundary_current.py::transition_decision` to interpret the recorded result and exit.
Require the recorded result and exit to agree: `PASS`/`0` or explicit
`not-applicable`/`0` continues; record `not-applicable` distinctly.
`FAIL`/`1` and `BLOCKED`/`2` halt review, completion, and unattended finalization.
Report the exact command, exit, and sorted unique blocker codes; keep the spec
`in-progress` and never write `review` or `done` after failure.
For routine work without an explicit evidence requirement, follow
`docs/runbooks/current-change-validation.md` and its focused checks.
<!-- EVIDENCE-BOUNDARY-GATE:END v1 -->

#### Final record generation gate

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

#### Determine new status based on review outcome

- If all `decision-needed` and `patch` findings were resolved (fixed or rejected) AND no unresolved `high`/`medium` findings remain AND `record_gate_failed` is not true: set `new_status` = `done`. Update the story file Status section to `done`.
- If `patch` findings were left as action items, or unresolved issues remain: set `new_status` = `in-progress`. Update the story file Status section to `in-progress`.

Save the story file.

#### Sync sprint-status.yaml

If `story_key` is not set, skip this subsection and note that sprint status was not synced because no story key was available.

If `{sprint_status}` file exists:

1. Load the FULL `{sprint_status}` file.
2. Find the `development_status` entry matching `{story_key}`.
3. If found: update `development_status[{story_key}]` to `{new_status}`. Update `last_updated` to current date. Save the file, preserving ALL comments and structure including STATUS DEFINITIONS.
4. If `{story_key}` not found in sprint status: warn the user that the story file was updated but sprint-status sync failed.

If `{sprint_status}` file does not exist, note that story status was updated in the story file only.

Only when the spec requires a final record under the gate above, version control is available, and a governed done transition was written, commit that reviewed transition with a validated Conventional Commit. After committing a governed done transition, rerun its `--verify-inserted-record` command at the new `HEAD` and require exit `0`. On any other exit, apply the gate's blocker branch, committing the return to `in-progress` as a new validated Conventional Commit.


#### Completion summary

> **Review Complete!**
>
> **Story Status:** `{new_status}`
> **Issues Fixed:** <fixed_count>
> **Action Items Created:** <action_count>
> **Deferred:** <W>
> **Rejected:** <R>

### 7. Next steps

Present the user with follow-up options:

> **What would you like to do next?**
> 1. **Start the next story** — run `dev-story` to pick up the next `ready-for-dev` story
> 2. **Re-run code review** — address findings and review again
> 3. **Done** — end the workflow

**HALT** — I am waiting for your choice. Do not proceed until the user selects an option.

## On Complete

Run: `uv run {project-root}/_bmad/scripts/resolve_customization.py --skill {skill-root} --project-root {project-root} --key workflow.on_complete`

If the resolved `workflow.on_complete` is non-empty, follow it as the final terminal instruction before exiting.
