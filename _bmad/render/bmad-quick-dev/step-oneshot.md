# Step One-Shot: Implement, Review, Present

## RULES

- **Language** — Speak in `English`. Write any file output in `English`.
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

#### Blind Hunter

Launch a subagent with no prior conversation context, with this prompt:

> Invoke the `bmad-review` skill with only the `adversarial` lens on the changed files.

If a layer's instruction requires subagents and none are available, generate one review prompt file per such layer in `/home/administrator/projects/hexalith/conversations/_bmad-output/implementation-artifacts` and HALT. Ask the human to run each in a separate session and paste back the findings.

### Classify

Deduplicate all review findings. Three categories only:

- **patch** — trivially fixable. Auto-fix immediately.
- **defer** — pre-existing issue not caused by this change. Append one new entry to `/home/administrator/projects/hexalith/conversations/_bmad-output/implementation-artifacts/deferred-work.md` using this format. Do not modify existing entries or look for duplicates.
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
4. **Verification** — append an empty `## Verification` heading. This is the replaceable first-run anchor for the generated record; do not author counts, paths, commits, or placeholder evidence beneath it.

Do not synchronize `review` yet.

### Commit Candidate

If version control is unavailable, change the trace to `in-progress`, synchronize only `in-progress`, record `GIT_UNAVAILABLE`, and HALT; a final record cannot derive a committed candidate without VCS even when promotion scope is empty. Otherwise, if the tree is dirty, create a local conventional commit derived from the intent. Stage only the scoped implementation files, `{spec_file}`, and exact declared root gitlinks; never use `git add -A` or `git commit -a`. Resolve committed `HEAD` once as immutable `candidate_revision` and pass that SHA to both gates.

### Promotion Completion Gate

When version control is available, invoke `python3 {project-root}/_bmad/scripts/verify_submodule_promotion.py --repository {project-root} --candidate {candidate_revision} --format json`, adding the trustworthy `--baseline <baseline_commit>`, one `--submodule <path>` per declaration, and `--require-remote <path>` when requested. Parse the JSON result. Gating is activated when the declaration or `changed_gitlinks` is non-empty; for activated work, `NO_VCS`, missing baseline, or `BASELINE_NOT_PROVIDED` is a blocker.

Any nonzero exit, any result other than `pass`, the activated missing-baseline condition, or a `SCOPE_NOT_EVALUATED` warning when version control is available fails the gate — `SCOPE_NOT_EVALUATED` means no submodule was evaluated, so the run proves nothing. Append the stable blocker codes and actionable diagnostics to the trace, set status `in-progress`, synchronize only `in-progress`, and HALT for remediation. Never write `done`, synchronize `review`, initialize/update/fetch submodules, or silently expand scope after failure.

### Final Record Generation Gate

When version control is available, first require the source tree clean outside `{spec_file}`, its sprint-status file, and declared TRX artifacts. After capturing `{candidate_revision}`, clean-rebuild it with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}` and rerun every root-owned test project into fresh TRX artifacts; never use `--no-build` output built before the candidate. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, adding the trustworthy `--baseline <baseline_commit>`, one `--test-results <full-project-name>=<artifact-path>` for every root-owned test project declared by the root `.slnx`, one `--submodule <path>` per declaration, and `--require-remote <path>` when requested. The generator must derive a candidate-bound test-binary manifest; `TEST_BUILD_NOT_BOUND` blocks completion. Parse the bundle JSON and its nested `document`. Every count, path, and commit in the trace comes from this one bundle; never author one yourself.

Any nonzero exit, or any nested `document.result` other than `pass`, fails the gate — a `RECORD_NOT_DERIVED` blocker means the run derived nothing, so it proves nothing. Append the stable blocker codes and actionable diagnostics to the trace, set status `in-progress`, synchronize only `in-progress`, and HALT for remediation. Never write `done`, and never hand-edit a count, path, or commit into agreement with the record.

Once it passes, insert bundle field `markdown` VERBATIM into the trace, replacing the existing block between the `<!-- STORY-FINAL-RECORD:BEGIN -->` and `<!-- STORY-FINAL-RECORD:END -->` markers when one is present, or appending it under `## Verification` when it is not. Set frontmatter `file_list_commit` to the revision the block was derived from. Then invoke the generator with `--repository {project-root} --story {spec_file} --verify-record-sha256 <markdown_sha256> --format json`; any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` returns the trace and sprint state to `in-progress` and HALTs.

### Complete Trace and Commit Completion Record

Only after both gates pass, change `{spec_file}` status to `done` and follow `./sync-sprint-status.md` with `target_status` = `review`. Create a second local conventional commit staging only `{spec_file}` and the sprint-tracking file changed by synchronization.

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
