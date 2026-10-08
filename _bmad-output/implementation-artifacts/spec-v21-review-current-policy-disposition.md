---
title: 'Reconcile the historical V21 review with current repository policy'
type: 'chore'
created: '2026-10-08'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '5e4abc6f87692e634319c1d5612c91647ae6eae7'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The V21 publication spec still presents its halted loop-11 review as active work, although AR-15 froze its tooling and the current repository policy retired its workflow. Its unresolved findings lack a current disposition, so resuming that spec repeats obsolete authority gates.

**Approach:** Document every loop-11 finding's disposition under current policy, link that explanation from the current-change runbook, and append a dated pointer to the original spec. Preserve its existing content, status, frozen intent and triage as historical evidence. This is routine documentation work following the user's choice to re-scope for current main; it changes no publisher, test, schema, authority record, workflow, dependency, submodule, story status, or acceptance decision and creates no commit or push.

</frozen-after-approval>

## Implementation Notes

- Investigation found no active caller of the V21 publisher. Architecture V16/AR-15 and the V22 spec freeze its record, schema, publisher and direct tests; the current-change policy and test collection exclude its retired gates.
- Scope is a current-policy disposition, not Story 7.1 implementation or a new authority publication. No sprint story key or story-completion transition applies.
- Validation will check all loop-11 IDs against the disposition, relative links, preserved original-spec bytes, unchanged frozen tooling/evidence, root-submodule declarations and whitespace. Documentation-only work requires no new tests or product rebuild.
- Added `docs/runbooks/v21-review-disposition.md`, clarified V19–V21 retirement in the current-change runbook, and appended a dated pointer after the original publication spec's existing content.
- Read-only historical checks: the original V21 publication `d297f96dc39b04008f391b404021899431585b6d` exited `0` with `EFFECTIVE_HOLD=LIFTED`; the investigation baseline `5e4abc6f87692e634319c1d5612c91647ae6eae7` exited `1` with `V21_DESCENDANT_GITLINK_DRIFT` and `ACTIVE`. Both observations are documented without reinterpreting either result.
- Focused documentation validation passed: all 25 loop-11 IDs occur exactly once in the disposition; every local link resolves; the complete original-spec prefix is byte-identical to the baseline; V19/V20/V21 records and the V21 publisher, schema and direct tests remain byte-identical; new Markdown uses LF with no trailing whitespace.
- `python3 scripts/check-root-submodules.py --repository .` exited `0` with `root submodules: PASS`.
- Another workspace operation committed the initial spec and documentation while this run was active, including unrelated V17 changes. This workflow issued no staging, commit or push command and preserved that concurrent work.
- The one-shot Blind Hunter review returned four documentation findings, all verified and patched: pinned historical-suite reproduction, explicit integration-policy disposition, the later metadata commit and snapshot identity for the live root-submodule check. No findings were deferred and no frozen implementation was changed.
- Final focused validation passed after all review corrections, including baseline-scoped `git diff --check`. This routine documentation spec is complete; the original V21 review remains historical `in-review`, and no sprint or acceptance state was transitioned. Remaining task edits are left uncommitted by this workflow.

## Review Triage Log

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| DISPOSITION-01 | medium | patch | The frozen test fixtures read their checkout's workflow, which current main deleted. V22's original workflow runs the suite from tooling commit `239758d396d28372687b73f5dc128405892cb520`; the runbook now specifies a separate complete checkout at that revision and distinguishes suite reproduction from the observed CLI checks. |
| DISPOSITION-02 | low | patch | The integration row's unspecified later decisions obscured why those gates no longer apply. Added the 2026-09-27 direct-push policy's effect on topology/merge checks and linked the dated AD-4 preparation approval while retaining its unsupported terminal transition. |
| DISPOSITION-03 | low | patch | Git commit `c2486abc8232640e67be094bd1ca5e040b7697eb` recorded the review metadata and loop-11 triage on 2026-09-16. Added that later-history qualification without changing the original rejected verdict or rationale. |
| DISPOSITION-04 | low | patch | The root-submodule checker reads live declarations/index entries rather than a selected historical candidate. Reran it with unchanged before/after inputs at `b3a813b0ba0020a558a3779e4e88b65612850e82`, verified the index and declarations matched that commit, and documented its limited scope. |
