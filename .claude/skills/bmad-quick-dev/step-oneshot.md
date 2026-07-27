# Step One-Shot: Implement, Review, Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- NEVER auto-push.
- All review subagents must run at the same model capability as the current session.
- Run subagents synchronously: launch them together, then wait for all results before continuing.

## INSTRUCTIONS

### Capture Baseline and Promotion Scope

Before implementation, capture `baseline_commit` as committed `HEAD`, or `NO_VCS` when version control is unavailable. Derive `submodule_promotions` from the clarified, already-approved intent: use `[]` for non-promotion work or list every affected root-declared `references/...` path exactly once with `require_remote`. Ambiguous or expanded scope requires human approval; HALT rather than inventing it.

### Implement

Follow `./sync-sprint-status.md` with `target_status` = `in-progress`.

Implement the clarified intent directly.

### Review

Execute these review layers in parallel wherever their execution methods allow, following each layer's instruction verbatim after substituting any runtime placeholders:

{workflow.oneshot_review_layers}

If a layer's instruction requires subagents and none are available, generate one review prompt file per such layer in `{{.implementation_artifacts}}` and HALT. Ask the human to run each in a separate session and paste back the findings.

### Classify

Deduplicate all review findings. Three categories only:

- **patch** — trivially fixable. Auto-fix immediately.
- **defer** — pre-existing issue not caused by this change. Append one new entry to `{{.deferred_work_file}}` using this format. Do not modify existing entries or look for duplicates.
  ```markdown
  - source_spec: `{spec_file}`
    summary: <one sentence>
    evidence: <why this is real>
  ```
- **reject** — noise. Drop silently.

If a finding is caused by this change but too significant for a trivial patch, HALT and present it to the human for decision before proceeding.

### Generate Spec Trace

Set `title` = a concise title derived from the clarified intent.

Write `{spec_file}` using `./spec-template.md`. Fill only these sections — delete all others:

1. **Frontmatter** — set `title: '{title}'`, `type`, `created`, `status: 'in-review'`, the captured `baseline_commit`, and `submodule_promotions`. Add `route: 'one-shot'`.
2. **Title and Intent** — `# {title}` heading and `## Intent` with **Problem** and **Approach** lines. Reuse the summary you already generated for the terminal.
3. **Suggested Review Order** — append after Intent. Build using the same convention as `./step-05-present.md` § "Generate Suggested Review Order" (spec-file-relative links, concern-based ordering, ultra-concise framing).

Do not synchronize `review` yet.

### Commit Candidate

If version control is available and the tree is dirty, create a local conventional commit derived from the intent. Stage only the scoped implementation files, `{spec_file}`, and exact declared root gitlinks; never use `git add -A` or `git commit -a`. Resolve committed `HEAD` as `candidate_revision`. If VCS is unavailable and `submodule_promotions` is non-empty, change the trace to `in-progress`, synchronize only `in-progress`, and HALT.

### Promotion Completion Gate

When version control is available, invoke `python3 {project-root}/_bmad/scripts/verify_submodule_promotion.py --repository {project-root} --candidate {candidate_revision} --format json`, adding the trustworthy `--baseline <baseline_commit>`, one `--submodule <path>` per declaration, and `--require-remote <path>` when requested. Parse the JSON result. Gating is activated when the declaration or `changed_gitlinks` is non-empty; for activated work, `NO_VCS`, missing baseline, or `BASELINE_NOT_PROVIDED` is a blocker.

Any nonzero exit, any result other than `pass`, the activated missing-baseline condition, or a `SCOPE_NOT_EVALUATED` warning when version control is available fails the gate — `SCOPE_NOT_EVALUATED` means no submodule was evaluated, so the run proves nothing. Append the stable blocker codes and actionable diagnostics to the trace, set status `in-progress`, synchronize only `in-progress`, and HALT for remediation. Never write `done`, synchronize `review`, initialize/update/fetch submodules, or silently expand scope after failure.

### Complete Trace and Commit Completion Record

Only after the gate passes (or non-promotion work has no VCS and preserves the prior behavior), change `{spec_file}` status to `done` and follow `./sync-sprint-status.md` with `target_status` = `review`. If version control is available, create a second local conventional commit staging only `{spec_file}` and the sprint-tracking file changed by synchronization.

### Present

1. Open the spec in the user's editor so they can click through the Suggested Review Order:
   - Resolve two absolute paths: (1) the repository root (`git rev-parse --show-toplevel` — returns the worktree root when in a worktree, project root otherwise; if this fails, fall back to the current working directory), (2) `{spec_file}`. Run `code -r "{absolute-root}" "{absolute-spec-file}"` — the root first so VS Code opens in the right context, then the spec file. Always double-quote paths to handle spaces and special characters.
   - If `code` is not available (command fails), skip gracefully and tell the user the spec file path instead.
2. Display a summary in conversation output, including:
   - The commit hash (if one was created).
   - List of files changed with one-line descriptions. Any file paths shown in conversation/terminal output must use CWD-relative format (no leading `/`) with `:line` notation (e.g., `src/path/file.ts:42`) for terminal clickability — this differs from spec-file links which use spec-file-relative paths.
   - Review findings breakdown: patches applied, items deferred, items rejected. If all findings were rejected, say so.
   - A note that the spec is open in their editor (or the file path if it couldn't be opened). Mention that `{spec_file}` now contains a Suggested Review Order.
   - **Navigation tip:** "Ctrl+click (Cmd+click on macOS) the links in the Suggested Review Order to jump to each stop."
3. Offer to push and/or create a pull request.

HALT and wait for human input.

Workflow complete.

## On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}
