---
---

# Step 5: Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- Push only when the user's request authorizes it.

## INSTRUCTIONS

### Prepare Committed Candidate

When the story's v9 contract output pair is already committed and this run will commit any change that candidate retention does not mask, such as a source or gitlink change, a review finding or `deferred` frontmatter entry in `{spec_file}`, or a `deferred-work.md` entry, first remove both files named by the contract's `finalRecord.paths` and commit only those removals as a record-only retraction. Use the task's existing commit authorization. If that required commit is not authorized or fails, follow the completion gate's blocker branch and HALT before staging or committing the replacement candidate.

Only when the spec explicitly requires a generated final record or the story's v9 contract exists, stage the exact story-owned candidate paths and create a validated Conventional Commit if the user's request authorizes a commit. Resolve committed `HEAD` once into `{candidate_revision}`. Never stage unrelated paths or pass a moving `HEAD` token to a completion gate. If a required commit is not authorized, leave the work uncommitted and report its state without asking for repeat authorization.

### Current change validation

For new work under `docs/runbooks/current-change-validation.md`, run `python3 {project-root}/scripts/check-root-submodules.py --repository {project-root}` and focused tests for the change. Record failures honestly and do not mark a failing change complete. Run historical promotion or evidence-boundary verifiers only when the spec explicitly requires them.

### Final Record Generation Gate

When `_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json` exists for the story, where `<story-id>` is the story's dotted `<epic>.<story>` number (for example `7.3` for story key `7-3-…`), skip the legacy procedure below and run the story completion gate at the end of this section before any `review` or `done` transition; the spec cannot opt out. Otherwise, for new routine work, record the change and test results in the spec and skip this section unless the spec explicitly requires a generated final record. The legacy procedure below applies only to that explicit requirement.

Clean-rebuild the committed candidate with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}` and rerun every root-owned test project into fresh TRX artifacts. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, with the trustworthy baseline, all declared test-result artifacts, and the exact submodule scope. Require `TEST_BUILD_NOT_BOUND` and `RECORD_NOT_DERIVED` to block the gate. Any nonzero exit or nested result other than `pass` returns the spec and sprint lifecycle to `in-progress`; Never write `done`, and HALT with the stable diagnostics.

On success, insert bundle field `markdown` VERBATIM into the story's final-record region and retain `markdown_sha256`. Run the generator again with `--verify-record-sha256 <markdown_sha256> --format json`. Any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` returns lifecycle state to `in-progress` and HALTs. Only a candidate-bound, digest-verified final record permits the terminal transition below.

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

### Mark Spec Done

Change `{spec_file}` status to `done` in the frontmatter.

If `{story_key}` is not empty and `{{.implementation_artifacts}}/sprint-status.yaml` exists, read `[[bmad-snapshot:sync-sprint-status.md]]` with `{target_status}` = `review`.

### Commit and Complete

When the task authorizes a commit, stage only the identified task-owned paths, create a validated Conventional Commit, and verify that every intended path is committed. Any commit failure returns the spec and sprint lifecycle to `in-progress` and HALTs.

{workflow.open_spec}

### Display Summary

Display a very short completion summary — one or two sentences — including:

- What changed.
- The verification and review result, including whether anything was deferred.
- The commit hash, if one was created.

Do not list changed files, repeat details from the spec, or narrate the process unless the user asks.

Offer applicable next actions in one short line: check CI after an authorized direct push to `main`; use `bmad-walkthrough`; or make another change.

Workflow complete.

## On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}
