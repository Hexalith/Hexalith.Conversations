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

#### Prepare committed review candidate

Present the exact review-patch path set and ask the user for explicit authorization to create its local commit. **HALT** until the user authorizes or declines; choosing "Apply every patch" did not itself authorize a commit. If declined, preserve story and sprint state as `in-progress`, leave the patches uncommitted, and skip all completion gates. If authorized, stage only those paths, create a validated Conventional Commit, require every other source path clean, and resolve committed `HEAD` exactly once into `{candidate_revision}`.

Authorization absent or declined is terminal for this run: HALT immediately after preserving `in-progress`. Do not fall through to status determination, sprint synchronization, the completion summary, next-step choices, or any `done` branch.

#### V12 lifecycle evidence gates

Before any lifecycle status write, read the baseline and exact `submodule_promotions` scope from `{spec_file}`. Run `_bmad/scripts/verify_submodule_promotion.py` against `{candidate_revision}`, then run `python3 {project-root}/_bmad/scripts/verify_evidence_boundary.py --repository {project-root} --baseline {baseline_commit} --candidate {candidate_revision}`. Preserve `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` as distinct results. Continue only for promotion exit `0` plus evidence `PASS` or `not-applicable` with a nonempty assertion ledger. Otherwise force `{new_status}` to `in-progress`, preserve diagnostics in the review record, never execute the `done` branch, and synchronize only `in-progress`.

#### Final record generation gate

Clean-rebuild the committed candidate with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}` and rerun every root-owned test project into fresh TRX artifacts. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, with the trustworthy baseline, all declared test-result artifacts, and the exact submodule scope. Require `TEST_BUILD_NOT_BOUND` and `RECORD_NOT_DERIVED` to block the gate. Any nonzero exit or nested result other than `pass` sets `record_gate_failed`, forces `{new_status}` = `in-progress`; never write or synchronize `done`, and HALT with the stable diagnostics.

On success, insert bundle field `markdown` VERBATIM into the story's final-record region and retain `markdown_sha256`. Run the generator again with `--verify-record-sha256 <markdown_sha256> --format json`. Any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` sets `record_gate_failed`, returns lifecycle state to `in-progress`, and HALTs. Only a candidate-bound, digest-verified final record permits status determination.

#### Authorize completion record and lifecycle mutation

Present the exact post-generation completion-record and lifecycle-status path set and request separate explicit authorization to commit that exact set. Review-candidate authorization does not authorize the completion-record commit. If authorization is absent or declined, preserve story and sprint state as `in-progress`, do not write or synchronize `done`, leave the verified record paths uncommitted, and HALT immediately without falling through to status determination, sprint synchronization, completion summary, or next-step choices. Once authorized, freeze that exact path set; no additional path may enter the completion commit.

#### Determine new status based on review outcome

- If `record_gate_failed` is not true, all `decision-needed` and `patch` findings were resolved (fixed or rejected), AND no unresolved `high`/`medium` findings remain: set `new_status` = `done`. Update the story file Status section to `done`.
- If `patch` findings were left as action items, or unresolved issues remain: set `new_status` = `in-progress`. Update the story file Status section to `in-progress`.
- If `record_gate_failed` is true, preserve `new_status` = `in-progress`; never write or synchronize `done`.

Save the story file.

#### Sync sprint-status.yaml

If `story_key` is not set, skip this subsection and note that sprint status was not synced because no story key was available.

If `{sprint_status}` file exists:

1. Load the FULL `{sprint_status}` file.
2. Find the `development_status` entry matching `{story_key}`.
3. If found: update `development_status[{story_key}]` to `{new_status}`. Update `last_updated` to current date. Save the file, preserving ALL comments and structure including STATUS DEFINITIONS.
4. If `{story_key}` not found in sprint status: warn the user that the story file was updated but sprint-status sync failed.

If `{sprint_status}` file does not exist, note that story status was updated in the story file only.

Stage and commit only the separately authorized completion-record and lifecycle-status path set with a validated Conventional Commit. Verify every authorized path is committed and no other path entered the commit. Any commit failure restores story and sprint state to `in-progress` and HALTs before completion.

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
