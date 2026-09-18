---
---

# Step 5: Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- NEVER auto-push.

## INSTRUCTIONS

### Prepare Committed Candidate

Before any terminal gate, present the exact story-owned dirty path set and ask for explicit authorization to create the local candidate commit. Earlier permission to implement or apply patches did not authorize a commit. If authorization is absent or declined, preserve `in-progress`, leave the paths uncommitted, and HALT. Once authorized, stage only that exact path set, create a validated Conventional Commit, require every other source-tree path clean, and resolve committed `HEAD` exactly once into `{candidate_revision}`. Never pass a moving `HEAD` token to a completion gate.

### V12 lifecycle evidence gates

Before any lifecycle status write, re-read `{baseline_commit}` and `submodule_promotions` from `{spec_file}` frontmatter. Run `_bmad/scripts/verify_submodule_promotion.py` with the repository root, that baseline, `{candidate_revision}`, and the exact declared scope. Then run `python3 {project-root}/_bmad/scripts/verify_evidence_boundary.py --repository {project-root} --baseline {baseline_commit} --candidate {candidate_revision}`. Preserve `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` as distinct results. Continue only when the promotion gate exits `0` and the evidence result is `PASS` or `not-applicable` with a nonempty assertion ledger. Any other outcome leaves the spec and sprint lifecycle unchanged and HALTs with the stable diagnostics.

### Final Record Generation Gate

Clean-rebuild the committed candidate with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}` and rerun every root-owned test project into fresh TRX artifacts. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, with the trustworthy baseline, all declared test-result artifacts, and the exact submodule scope. Require `TEST_BUILD_NOT_BOUND` and `RECORD_NOT_DERIVED` to block the gate. Any nonzero exit or nested result other than `pass` returns the spec and sprint lifecycle to `in-progress`; Never write `done`, and HALT with the stable diagnostics.

On success, insert bundle field `markdown` VERBATIM into the story's final-record region and retain `markdown_sha256`. Run the generator again with `--verify-record-sha256 <markdown_sha256> --format json`. Any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` returns lifecycle state to `in-progress` and HALTs. Only a candidate-bound, digest-verified final record permits the terminal transition below.

### Authorize Completion Record and Lifecycle Mutation

Present the exact post-generation completion-record and lifecycle-status path set and request separate explicit authorization to commit that exact set. Candidate-commit authorization does not authorize the completion-record commit. If authorization is absent or declined, preserve the spec and sprint lifecycle as `in-progress`, do not write or synchronize `done`, leave the verified record paths uncommitted, and HALT. Once authorized, freeze that exact path set; no additional path may enter the completion commit.

### Mark Spec Done

Change `{spec_file}` status to `done` in the frontmatter.

If `{story_key}` is not empty and `{{.implementation_artifacts}}/sprint-status.yaml` exists, read `[[bmad-snapshot:sync-sprint-status.md]]` with `{target_status}` = `review`.

### Commit and Complete

Stage only the separately authorized completion-record and lifecycle-status path set, create a validated Conventional Commit, and verify that every authorized path is committed. Any commit failure returns the spec and sprint lifecycle to `in-progress`, never completes the workflow, and HALTs. Never infer completion-record commit authorization from approval of the implementation.

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
