---
deferred_work_file: '{implementation_artifacts}/deferred-work.md'
---

# Step 4: Present and Act

## RULES

- YOU MUST ALWAYS SPEAK OUTPUT in your Agent communication style with the config `{communication_language}`
- When `{spec_file}` is set, always write findings to the story file before offering action choices.
- `decision-needed` findings must be resolved before handling `patch` findings.

## INSTRUCTIONS

### 1. Clean review shortcut

If zero findings remain after triage (all dismissed or none raised): state that and proceed to section 6 (Sprint Status Update).

### 2. Write findings to the story file

If `{spec_file}` exists and contains a Tasks/Subtasks section, append a `### Review Findings` subsection. Write all findings in this order:

1. **`decision-needed`** findings (unchecked):
   `- [ ] [Review][Decision] <Title> — <Detail>`

2. **`patch`** findings (unchecked):
   `- [ ] [Review][Patch] <Title> [<file>:<line>]`

3. **`defer`** findings (checked off, marked deferred):
   `- [x] [Review][Defer] <Title> [<file>:<line>] — deferred, pre-existing`

Also append each `defer` finding to `{deferred_work_file}` under a heading `## Deferred from: code review ({date})`. If `{spec_file}` is set, include its basename in the heading (e.g., `code review of story-3.3 (2026-03-18)`). One bullet per finding with description.

### 3. Present summary

Announce what was written:

> **Code review complete.** <D> `decision-needed`, <P> `patch`, <W> `defer`, <R> dismissed as noise.

If `{spec_file}` is set, add: `Findings written to the review findings section in {spec_file}.`
Otherwise add: `Findings are listed above. No story file was provided, so nothing was persisted.`

### 4. Resolve decision-needed findings

If `decision_needed` findings exist, present each one with its detail and the options available. The user must decide — the correct fix is ambiguous without their input. Walk through each finding (or batch related ones) and get the user's call. Once resolved, each becomes a `patch`, `defer`, or is dismissed.

If the user chooses to defer, ask: Quick one-line reason for deferring this item? (helps future reviews): — then append that reason to both the story file bullet and the `{deferred_work_file}` entry.

**HALT** — I am waiting for your numbered choice. Reply with only the number. Do not proceed until you select an option.

### 5. Handle `patch` findings

If `patch` findings exist (including any resolved from step 4), HALT. Ask the user:

If `{spec_file}` is set, present all three options:

> **How would you like to handle the `<P>` `patch` findings?**
> 1. **Apply every patch** — fix all of them now, no per-finding confirmation. Defer and decision-needed items are not touched.
> 2. **Leave as action items** — they are already in the story file
> 3. **Walk through each patch** — show details for each before deciding

If `{spec_file}` is **not** set, present only options 1 and 2 (omit "Leave as action items" — findings were not written to a file):

> **How would you like to handle the `<P>` `patch` findings?**
> 1. **Apply every patch** — fix all of them now, no per-finding confirmation. Defer and decision-needed items are not touched.
> 2. **Walk through each patch** — show details for each before deciding

**HALT** — I am waiting for your numbered choice. Reply with only the number. Do not proceed until you select an option.

- **Apply every patch**: Apply every patch finding without per-finding confirmation. Do not modify defer or decision-needed items. After all patches are applied, present a summary of changes made. If `{spec_file}` is set, check off the patch items in the story file (leave defer items as-is).
- **Leave as action items** (only when `{spec_file}` is set): Done — findings are already written to the story.
- **Walk through each patch**: Present each finding with full detail, diff context, and suggested fix. After walkthrough, re-offer the applicable options above.

  **HALT** — I am waiting for your numbered choice. Do not proceed until you select an option.

**✅ Code review actions complete**

- Decision-needed resolved: <D>
- Patches handled: <P>
- Deferred: <W>
- Dismissed: <R>

### 6. Update story status and sync sprint tracking

Skip this section if `{spec_file}` is not set.

#### Prepare committed review candidate

1. If this review applied any patch outside `{spec_file}` and its sprint-status file, derive the exact patched path set from the findings applied in section 5 and compare it with the Git delta. Do not infer ownership for any additional dirty path and never stage a submodule pointer unless it was an explicitly approved patch target.
2. Present that exact path set and ask the user for explicit authorization to create the local review-patch commit. **HALT** until the user authorizes or declines the commit; choosing "Apply every patch" did not itself authorize a commit.
3. If authorization is declined, preserve story and sprint state as `in-progress`, leave the patches uncommitted, skip both completion gates, and hand off the exact path set for later finalization.
4. If authorization is granted, create a validated local Conventional Commit staging only the authorized review-patch paths. Never use `git add -A`, `git commit -a`, or include unrelated dirt. The story record and sprint-status file may remain uncommitted record outputs.
5. Require every other source-tree path clean, then resolve committed `HEAD` exactly once into `{candidate_revision}`. Pass this immutable SHA to both completion gates; do not re-resolve or pass the moving `HEAD` token afterwards. If version control is unavailable, preserve `in-progress`, record `GIT_UNAVAILABLE`, and HALT.

#### Promotion completion gate

Run this gate before determining `{new_status}`:

1. Read `submodule_promotions` from `{spec_file}` without expanding its approved scope. If the field is absent entirely, record `INVALID_SCOPE`, fail the gate, and never write `done` — an undeclared scope is an untrustworthy input, not an empty one. Read the baseline from `baseline_commit`, falling back to `baseline_revision`; a missing value or `NO_VCS` is not trustworthy.
2. Invoke `python3 {project-root}/_bmad/scripts/verify_submodule_promotion.py --repository {project-root} --candidate {candidate_revision} --format json`, adding a `--submodule <path>` for every declared item, `--require-remote <path>` when its `require_remote` value is true, and `--baseline <value>` only when the baseline is trustworthy.
3. Parse the JSON result. Promotion gating is activated when the declaration is non-empty or `changed_gitlinks` is non-empty. When activated, a missing/untrustworthy baseline or `BASELINE_NOT_PROVIDED` is a blocker even if the checker otherwise exits zero. Independently of activation, a `SCOPE_NOT_EVALUATED` warning always fails the gate whenever version control is available: it reports that the checker evaluated no submodule at all, so an exit-zero run proves nothing about promotion completeness.
4. Any nonzero checker exit, any `result` other than `pass`, or the activated missing-baseline condition fails the gate. Preserve every `blockers[].code` in the review record; for the caller-promoted missing-baseline condition preserve `BASELINE_NOT_PROVIDED` as the blocker code with its diagnostic text.
5. On gate failure, set `promotion_gate_failed = true`, force `{new_status}` = `in-progress`, update the story Status section to `in-progress`, forbid the `done` branch below, and synchronize only `in-progress`; never write or synchronize `done`. Report the actionable checker diagnostics. Do not modify, initialize, update, fetch, commit, or silently expand submodule scope while remediating.

#### Final record generation gate

Run this gate after every patch is applied and before determining `{new_status}`. The review changes the tree, so the record must be regenerated from the tree that is actually final.

1. Require the source tree clean outside `{spec_file}`, its sprint-status file, and declared TRX artifacts. Clean-rebuild the committed candidate with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}`, then rerun every root-owned test project into fresh TRX artifacts; never use `--no-build` output built before the candidate. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, adding `--baseline <value>` only when the baseline is trustworthy, one `--test-results <full-project-name>=<artifact-path>` for every root-owned test project declared by the root `.slnx`, one `--submodule <path>` for every declared item, and `--require-remote <path>` when its `require_remote` value is true. Require the candidate-bound test-binary manifest; `TEST_BUILD_NOT_BOUND` blocks completion.
2. Parse the bundle JSON and its nested `document`. Any nonzero exit or any nested `document.result` other than `pass` fails the gate. A `RECORD_NOT_DERIVED` blocker means the run parsed no artifact, resolved no candidate, or found no record section to replace — it proves nothing, so it can never be read as a pass. Preserve every `blockers[].code` in the review record.
3. On gate failure, set `record_gate_failed = true`, force `{new_status}` = `in-progress`, update the story Status section to `in-progress`, forbid the `done` branch below, and synchronize only `in-progress`; never write or synchronize `done`. Never hand-edit a count, path, or commit into agreement with the record as remediation.
4. On success, insert bundle field `markdown` VERBATIM into `{spec_file}`, replacing the existing block between the `<!-- STORY-FINAL-RECORD:BEGIN -->` and `<!-- STORY-FINAL-RECORD:END -->` markers when one is present, or the region between `### File List` and `### Boundary Confirmation` when it is not. Set frontmatter `file_list_commit` to the revision the block was derived from and reference the generated record from sprint status without restating counts. Then invoke the generator with `--repository {project-root} --story {spec_file} --verify-record-sha256 <markdown_sha256> --format json`; any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` sets `record_gate_failed = true`, returns story/sprint state to `in-progress`, and HALTs.

#### Determine new status based on review outcome

- If `promotion_gate_failed` is not true, `record_gate_failed` is not true, all `decision-needed` and `patch` findings were resolved (fixed or dismissed), AND no unresolved `high`/`medium` findings remain: set `{new_status}` = `done`. Update the story file Status section to `done`.
- If `patch` findings were left as action items, or unresolved issues remain: set `{new_status}` = `in-progress`. Update the story file Status section to `in-progress`.
- If `promotion_gate_failed` or `record_gate_failed` is true: preserve `{new_status}` = `in-progress`; never write or synchronize `done` regardless of review outcome.

Save the story file.

#### Sync sprint-status.yaml

If `{story_key}` is not set, skip this subsection and note that sprint status was not synced because no story key was available.

If `{sprint_status}` file exists:

1. Load the FULL `{sprint_status}` file.
2. Find the `development_status` entry matching `{story_key}`.
3. If found: update `development_status[{story_key}]` to `{new_status}`. Update `last_updated` to current date. Save the file, preserving ALL comments and structure including STATUS DEFINITIONS.
4. If `{story_key}` not found in sprint status: warn the user that the story file was updated but sprint-status sync failed.

If `{sprint_status}` file does not exist, note that story status was updated in the story file only.

#### Completion summary

> **Review Complete!**
>
> **Story Status:** `{new_status}`
> **Issues Fixed:** <fixed_count>
> **Action Items Created:** <action_count>
> **Deferred:** <W>
> **Dismissed:** <R>

### 7. Next steps

Present the user with follow-up options:

> **What would you like to do next?**
> 1. **Start the next story** — run `dev-story` to pick up the next `ready-for-dev` story
> 2. **Re-run code review** — address findings and review again
> 3. **Done** — end the workflow

**HALT** — I am waiting for your choice. Do not proceed until the user selects an option.

## On Complete

Run: `python3 {project-root}/_bmad/scripts/resolve_customization.py --skill {skill-root} --key workflow.on_complete`

If the resolved `workflow.on_complete` is non-empty, follow it as the final terminal instruction before exiting.
