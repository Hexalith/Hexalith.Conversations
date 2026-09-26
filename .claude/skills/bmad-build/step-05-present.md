---
---

# Step 5: Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- Push only when the user's request authorizes it.

## INSTRUCTIONS

### Prepare Committed Candidate

Only when the spec explicitly requires a generated final record, stage the exact story-owned candidate paths and create a validated Conventional Commit if the user's request authorizes a commit. Resolve committed `HEAD` once into `{candidate_revision}`. Never stage unrelated paths or pass a moving `HEAD` token to a completion gate. If a required commit is not authorized, leave the work uncommitted and report its state without asking for repeat authorization.

### Current change validation

For new work under `docs/runbooks/current-change-validation.md`, run `python3 {project-root}/scripts/check-root-submodules.py --repository {project-root}` and focused tests for the change. Record failures honestly and do not mark a failing change complete. Run historical promotion or evidence-boundary verifiers only when the spec explicitly requires them.

### Final Record Generation Gate

For new routine work, record the change and test results in the spec and skip this section unless the spec explicitly requires a generated final record. The procedure below applies only to that explicit requirement.

Clean-rebuild the committed candidate with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}` and rerun every root-owned test project into fresh TRX artifacts. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, with the trustworthy baseline, all declared test-result artifacts, and the exact submodule scope. Require `TEST_BUILD_NOT_BOUND` and `RECORD_NOT_DERIVED` to block the gate. Any nonzero exit or nested result other than `pass` returns the spec and sprint lifecycle to `in-progress`; Never write `done`, and HALT with the stable diagnostics.

On success, insert bundle field `markdown` VERBATIM into the story's final-record region and retain `markdown_sha256`. Run the generator again with `--verify-record-sha256 <markdown_sha256> --format json`. Any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` returns lifecycle state to `in-progress` and HALTs. Only a candidate-bound, digest-verified final record permits the terminal transition below.

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
