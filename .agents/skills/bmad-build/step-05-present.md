---
---

# Step 5: Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- NEVER auto-push.

## INSTRUCTIONS

### V12 lifecycle evidence gates

Before any lifecycle status write, re-read `{baseline_commit}` and `submodule_promotions` from `{spec_file}` frontmatter. Run `_bmad/scripts/verify_submodule_promotion.py` with the repository root, that baseline, committed `HEAD`, and the exact declared scope. Then run `python3 {project-root}/_bmad/scripts/verify_evidence_boundary.py --repository {project-root} --baseline {baseline_commit} --candidate HEAD`. Preserve `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` as distinct results. Continue only when the promotion gate exits `0` and the evidence result is `PASS` or `not-applicable` with a nonempty assertion ledger. Any other outcome leaves the spec and sprint lifecycle unchanged and HALTs with the stable diagnostics.

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

## On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}
