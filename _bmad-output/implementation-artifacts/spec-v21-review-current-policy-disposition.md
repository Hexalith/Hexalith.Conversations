---
title: 'Reconcile the historical V21 review with current repository policy'
type: 'chore'
created: '2026-10-08'
status: 'in-progress'
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
