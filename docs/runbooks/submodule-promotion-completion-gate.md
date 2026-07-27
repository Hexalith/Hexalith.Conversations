---
title: Submodule Promotion Completion Gate
version: 1
status: active
effective_date: 2026-07-27
---

# Submodule Promotion Completion Gate

This operational runbook governs work that promotes changes from a root-declared
`references/...` submodule into the umbrella repository. It is a live workflow
document. The historical signed-v1 evidence at
`docs/release-evidence/promote-adopt-runbook.md` remains byte-identical and is not
superseded by this runbook.

## 1. Declare the exact scope

Record `submodule_promotions` in story or spec frontmatter. Use an empty list for
non-promotion work. Promotion-bearing work lists every affected root-declared path
exactly once and states whether local remote-tracking availability is required:

```yaml
submodule_promotions:
  - path: references/Hexalith.EventStore
    require_remote: true
```

Set `require_remote: true` for every submodule shared with other clones — that is
the default for `references/...` paths, because an unpushed submodule commit leaves
the umbrella recording a gitlink no other clone can resolve. Use `false` only for a
submodule deliberately kept local to this workspace, and state why in the plan.

A missing declaration may be filled automatically only when it is an exact
transcription of already-approved scope. Ambiguous or expanded scope requires
Product Owner or user approval. The gate evaluates the union of the declaration
and root gitlinks changed between the baseline and candidate revisions.

An **absent** `submodule_promotions` field is not the same as an empty one: every
gated workflow treats a missing field as `INVALID_SCOPE` and refuses to complete.

## 2. Prepare each affected submodule

Commit each affected submodule separately. Before running the gate, each one must:

- be declared by the root `.gitmodules` file and initialized as its own worktree;
- be clean, including staged, unstaged, and untracked files;
- have a resolvable commit at `HEAD`;
- be contained by a locally known remote-tracking ref when `require_remote: true`.

Remote availability is a local, read-only check. Publish or fetch separately when
needed, then rerun the gate. The checker performs neither operation.

## 3. Commit the umbrella gitlinks

Commit the exact mode-`160000` gitlink for each affected submodule in the umbrella
repository. A staged pointer bump or a prose completion note is **not** gate
evidence: the candidate must be a committed revision whose recorded object ID
equals the affected submodule's current `HEAD`.

## 4. Run the gate

Use the story/spec baseline and the committed umbrella candidate. Repeat
`--submodule` for every declaration and repeat `--require-remote` for every path
whose policy requires local remote-tracking availability.

```bash
python3 _bmad/scripts/verify_submodule_promotion.py \
  --repository <root> \
  --baseline <story-baseline-commit> \
  --candidate <committed-umbrella-revision> \
  --submodule references/Hexalith.EventStore \
  --require-remote references/Hexalith.EventStore \
  --format json
```

## 5. Interpret the result

- Exit `0`: the gate passed. Review warnings before proceeding.
- Exit `1`: the invocation was valid, but completion blockers remain.
- Exit `2`: the invocation or repository state cannot support a trustworthy result.

Treat the emitted stable blocker codes as the authority for remediation:

| Blocker | Remediation |
| --- | --- |
| `PATH_NOT_ROOT_DECLARED` | Correct the declaration to an exact path from the root `.gitmodules`; do not expand scope silently. |
| `SUBMODULE_NOT_INITIALIZED` | Initialize only the affected root-declared submodule through the approved workspace procedure. |
| `SUBMODULE_DIRTY_TRACKED` | Commit or otherwise resolve the affected submodule's staged or unstaged tracked changes. |
| `SUBMODULE_DIRTY_UNTRACKED` | Commit or otherwise resolve the affected submodule's untracked files. |
| `SUBMODULE_HEAD_UNRESOLVED` | Restore the affected submodule to a resolvable commit. |
| `REMOTE_COMMIT_UNAVAILABLE` | Publish or fetch outside the checker until a local remote-tracking ref contains the commit. |
| `GITLINK_MISSING_IN_CANDIDATE` | Commit the affected root gitlink in the umbrella candidate. |
| `GITLINK_MODE_NOT_160000` | Restore the path as a root submodule and commit its mode-`160000` gitlink. |
| `GITLINK_COMMIT_MISMATCH` | Commit the umbrella gitlink that exactly records the affected submodule `HEAD`. |
| `UNCAPTURED_SUBMODULE_PROMOTION` | A root submodule outside the declared scope has a checkout strictly ahead of its recorded gitlink — real commits the umbrella never captured. Declare the path in `submodule_promotions` and commit the gitlink, or restore the checkout to the recorded commit. |

For exit `2` (including `BASELINE_NOT_ANCESTOR` when the baseline is not an
ancestor of the candidate, and `INTERNAL_ERROR` for any unexpected failure),
correct the reported invocation, repository, baseline, candidate, or scope error
before relying on the result. Never reinterpret an error as a pass.

## 6. Workflow behavior on failure

- `bmad-code-review`, `bmad-quick-dev`, and `bmad-dev-story` keep or return story
  and sprint state to `in-progress` and cannot write `done`.
- `bmad-dev-auto` records the diagnostics and uses `blocked`; it cannot write a
  successful final revision or `done`.
- Preserve the stable codes and remediation text in the workflow record, resolve
  the named state, and rerun the same command.

## 7. Safety boundary

Discovery and evaluation are root-only. Never use recursive submodule commands.
Never initialize, update, fetch, enter, or traverse nested submodules to satisfy
this gate. The checker itself must not initialize, update, fetch, pull, push,
commit, add, checkout, reset, or mutate repository state.

Unrelated root-submodule dirt is a warning, not a blocker. A checkout that is
behind or diverged from its recorded gitlink warns as `UNRELATED_GITLINK_DRIFT`; a
checkout strictly **ahead** of it blocks as `UNCAPTURED_SUBMODULE_PROMOTION`,
because that is an uncaptured promotion rather than concurrent drift. A git failure
while inspecting an unrelated submodule warns (`UNRELATED_SUBMODULE_INSPECTION_FAILED`)
rather than aborting the run. Do not alter unrelated state as remediation for
scoped promotion work.

`SCOPE_NOT_EVALUATED` is emitted when nothing was declared **and** no usable
baseline was supplied (absent, or equal to the candidate), meaning no submodule was
evaluated at all. Every gated workflow treats it as a blocker when version control
is available: an exit-zero run that evaluated nothing proves nothing. The default
`--format text` output always names what was declared, what changed, and what was
evaluated, so a vacuous pass can never be mistaken for a verified one.

### Known limitations

- The gate reads the **committed** candidate. A promotion that is left uncommitted
  in the working tree is only visible through `UNCAPTURED_SUBMODULE_PROMOTION`
  (checkout ahead of the recorded gitlink); a declared path whose gitlink is not yet
  committed reports `GITLINK_COMMIT_MISMATCH`. Commit the scoped gitlink — never
  initialize, update, or fetch — to remediate.
- Nothing runs this gate automatically. There is no CI workflow or hook in this
  repository, so the gate is only as strong as the workflow prose that invokes it.
  Tracked in `_bmad-output/implementation-artifacts/deferred-work.md`.

## Ordered checklist (copy per story)

1. [ ] Every affected root-declared submodule identified; confirm the change belongs
      in the submodule's own repository, not the umbrella working tree.
2. [ ] Each affected path confirmed against the root `.gitmodules` file (no
      ad hoc or non-root paths).
3. [ ] Each affected submodule initialized without recursive or nested submodule
      commands.
4. [ ] The change made and committed inside each affected submodule's own repository.
5. [ ] Each submodule commit pushed and available via a local remote-tracking ref
      where the declared policy requires remote availability.
6. [ ] Each affected submodule worktree confirmed clean, including untracked files.
7. [ ] No nested submodule initialized, updated, or traversed while preparing the promotion.
8. [ ] Exact `submodule_promotions` scope recorded; remote requirements identified.
9. [ ] Each affected submodule committed separately, clean, and available remotely where required.
10. [ ] Root-only gitlinks committed in the umbrella repository and the mechanical completion gate passes.
