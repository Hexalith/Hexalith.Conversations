# Step One-Shot: Implement, Review, Present

You reach this step from step 2, or from step 1 when resuming a spec whose `route` is `oneshot`. `{spec_file}` already exists.

## RULES

- Speak in `{{.communication_language}}`. Write files in `{{.document_output_language}}`.
- Push only when the user's request authorizes it.
- Do not edit anything inside `<frozen-after-approval>` in `{spec_file}`.
- Review subagents must use the same model level as this session.
- Start all review subagents in this turn and wait for all of them to finish. Do not run them in the background or end your turn before they return.

## INSTRUCTIONS

### Implement

If `{story_key}` is not empty and `{{.implementation_artifacts}}/sprint-status.yaml` exists, read `[[bmad-snapshot:sync-sprint-status.md]]` with `{target_status}` = `in-progress`.

Build the change from `{spec_file}`. The Intent section is what you implement. As you work, add notes to `## Implementation Notes`: decisions you made, files you changed, surprises.

**When to stop and replan.** Stop coding if you learn something step 2 did not account for:

- the request left out something the user would notice in the result
- you need to do something you cannot undo
- the change is growing beyond what was planned

Write what triggered the stop in `## Implementation Notes`. Then update `{spec_file}`: add back `## Code Map` (filled in from what you learned while implementing) and `## Open Questions` (one question per gap), set `route: 'dispatch'` and `status: 'draft'`. Go back to `[[bmad-snapshot:step-02-plan.md]]` step 6.

### Review

Say which review layers you are skipping, then start every active layer before reading any results. Run them at the same time when you can. Fill in runtime placeholders first. When a layer tells you to launch a reviewer subagent, launch it with that prompt text. Do not read the reviewer's instruction file yourself. For any other customized instruction, do what it says:

{workflow.oneshot_review_layers}

If a layer needs subagents and you cannot launch them, write the full prompt for each layer under `{{.implementation_artifacts}}` (with placeholders filled in, not just file paths). Stop and ask the user to run each prompt in a separate session and paste back the findings.

### Classify

Wait until every review layer has reported. Then judge each finding. Ignore severity labels from reviewers — you decide.

For each finding:

- **Check the claim.** Go to the cited file and line. Does the problem the reviewer describes actually happen? Read surrounding code and callers until you can say yes or no. A nearby issue does not answer this one. Judge whether the bug is real, not whether the suggested fix sounds good. Code that fails loudly on a state you have not shown the program can reach is correct, not a bug.

- **Pick one verdict:**
  - `high` (intolerable), `medium` (tolerable), or `low` (cosmetic or negligible) — the problem is real. Rate it by harm to users or developers. For developer-only issues, say where it will hurt. Vague complaints like "this is messy" are not `high`/`medium`/`low` — use `false` or `maybe-false`. When unsure how bad, pick the higher grade.
  - `false` — you checked and the problem does not happen. Say what you found that disproves it.
  - `maybe-false` — you could not tell. Say what you would need to check. Use this only when the code and diff are not enough to decide.

- Write down every finding with its verdict and evidence. Do not drop any.

Reject `false` findings.

Reject `low` findings when users or developers would rarely hit the problem in normal use and the fix would add more than a simple correction or deletion.

Group what remains by root cause — two findings go together only if the same bug caused both. Same file or same fix is not enough. For each group, keep the worst verdict (`high` > `medium` > `low` > `maybe-false`). If a group has verified `high`, `medium`, or `low` members, route by the worst of those — not `defer` just because one member is `maybe-false`.

For each group:

- **patch** — This change caused or exposed the problem. The smallest fix is simple, adds no new public API, and does not guard code paths you did not show are reachable. Fix it now.
- **HALT** — Same as patch, but the smallest fix is not that simple. Stop and ask the user before continuing.
- **defer** — Everything else: old bugs not caused by this change, ideas for later, groups where every member is `maybe-false` and would be `medium` or `high` if true (record that severity marked unverified, and what would prove it; if it would only be `low`, reject it), or fixes that would edit CLAUDE.md, AGENTS.md, rules, or specs. Add one entry to `{{.implementation_artifacts}}/deferred-work.md`:

  ```markdown
  - source_spec: `{spec_file}`
    summary: <one sentence>
    evidence: <why this is real; for maybe-false, what would prove it>
  ```

  Do not edit old entries or check for duplicates.

### Prepare Committed Candidate

When the story's v9 contract output pair is already committed and this run will commit any change that candidate retention does not mask, such as a source or gitlink change, a review finding or `deferred` frontmatter entry in `{spec_file}`, or a `deferred-work.md` entry, first remove both files named by the contract's `finalRecord.paths` and commit only those removals as a record-only retraction. Use the task's existing commit authorization. If that required commit is not authorized or fails, follow the completion gate's blocker branch and HALT before staging or committing the replacement candidate.

Only when the spec explicitly requires a generated final record or the story's v9 contract exists, stage the exact story-owned candidate paths and create a validated Conventional Commit if the user's request authorizes a commit. Resolve committed `HEAD` once into `{candidate_revision}`. Never stage unrelated paths. If a required commit is not authorized, leave the work uncommitted and report its state without asking for repeat authorization.

### Current change validation

For new work under `docs/runbooks/current-change-validation.md`, run `python3 {project-root}/scripts/check-root-submodules.py --repository {project-root}` and focused tests for the change. Record failures honestly and do not mark a failing change complete. Run historical promotion or evidence-boundary verifiers only when the spec explicitly requires them.

### Final Record Generation Gate

When `_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json` exists for the story, where `<story-id>` is the story's dotted `<epic>.<story>` number (for example `7.3` for story key `7-3-…`), skip the legacy procedure below and run the story completion gate at the end of this section before any `review` or `done` transition; the spec cannot opt out. Otherwise, for new routine work, record the change and test results in the trace and skip this section unless the spec explicitly requires a generated final record. The legacy procedure below applies only to that explicit requirement.

Clean-rebuild the committed candidate with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}` and rerun every root-owned test project into fresh TRX artifacts. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, with the trustworthy baseline, all declared test-result artifacts, and the exact submodule scope. Require `TEST_BUILD_NOT_BOUND` and `RECORD_NOT_DERIVED` to block the gate. Any nonzero exit or nested result other than `pass` returns the trace and sprint lifecycle to `in-progress`; Never write `done`, and HALT with the stable diagnostics.

On success, insert bundle field `markdown` VERBATIM into the trace's final-record region and retain `markdown_sha256`. Run the generator again with `--verify-record-sha256 <markdown_sha256> --format json`. Any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` returns lifecycle state to `in-progress` and HALTs. Only a candidate-bound, digest-verified final record permits finalization.

<!-- STORY-COMPLETION-GATE:BEGIN v1 -->
**Story completion gate (v9 contract).** Run this gate before any `review` or `done` transition whenever `_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json` exists for the story, where `<story-id>` is the story's dotted `<epic>.<story>` number (for example `7.3` for story key `7-3-…`); the spec cannot opt out, and the legacy procedure above does not replace it. Routine work without such a contract skips this gate under the current-change policy. This gate runs only when this workflow invokes it: no CI job or hook enforces it.

1. Make the committed story candidate `HEAD`, with no other dirt. From the repository root, run every other scenario `command` of the contract through `uv run --frozen --no-sync`. Then run the generator, with the contract path as `<contract>` and its two `finalRecord.paths` entries as `<json>` and `<md>`:

   ```bash
   uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --format bundle --output-json <json> --output-markdown <md>
   ```

   Require exit `0`, and require the `summary` of the record printed on stdout to equal the contract's `finalRecord.summary` exactly.
2. Unless `<json>` and `<md>` both already equal their committed `HEAD` bytes, commit exactly those two files as a record-only commit. Insert the bytes of `<md>` verbatim between the `<!-- STORY-FINAL-RECORD:BEGIN -->` and `<!-- STORY-FINAL-RECORD:END -->` lines of `{spec_file}`, replacing anything already between them. When the spec has no such pair, append a blank line, the begin line, the Markdown, and the end line at its end. Then require exit `0` from:

   ```bash
   uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --verify-inserted-record {spec_file}
   ```

   After the record-only commit, commit no source or gitlink change; a source change requires first committing the removal of both `<json>` and `<md>`, then restarting this gate from the new candidate.
3. Blocker branch: on any nonzero exit, summary mismatch, required commit that is not authorized, commit failure, or verification failure, keep or return `{spec_file}` and the story's sprint-status row to `in-progress`, and never write `review` or `done`. Report the exact command, its exit, and every stable blocker code, then HALT. Remediate the named condition and rerun; never hand-edit the pair, a result file, or the inserted region into agreement.
<!-- STORY-COMPLETION-GATE:END v1 -->

### Completion Scope

Identify the exact task-owned source, completion-record, and lifecycle-status paths. Use the authorization already given for the task; do not request a separate per-commit approval. If the task does not authorize a commit, leave the paths uncommitted and report them. Keep unrelated paths out of the commit.

### Finalize Spec

Update `{spec_file}`:

1. Set `status: 'done'` in the frontmatter.
2. If review found anything, add `## Review Triage Log` with one line per finding: verdict and evidence. For `false`, the disproof. For `maybe-false`, what would settle it. For rejected `low`, why it was not worth fixing.

If `{story_key}` is not empty and `{{.implementation_artifacts}}/sprint-status.yaml` exists, read `[[bmad-snapshot:sync-sprint-status.md]]` with `{target_status}` = `review`.

### Commit

When the task authorizes a commit, stage only the identified task-owned paths, create a validated Conventional Commit, and verify that every intended path is committed. Any commit failure returns the spec and sprint lifecycle to `in-progress` and HALTs.

After that commit, when the story completion gate ran, rerun its `--verify-inserted-record` command at the new `HEAD` and require exit `0`. On any other exit, commit `{spec_file}` and the story's sprint-status row back to `in-progress`, report the exact command, its exit, and every stable blocker code, then HALT.

### Present

{workflow.open_spec}

Give the user a short summary — one or two sentences:

- What changed.
- Review result, including anything deferred.
- Commit hash, if you made one.

Do not list files, repeat the spec, or walk through what you did unless asked.

Offer next steps in one line: check CI after an authorized direct push to `main`; use `bmad-walkthrough`; or make another change.

Stop and wait for the user.

Workflow complete.

## On Complete

If anything appears below, do it before exiting. Otherwise exit.

{workflow.on_complete}
