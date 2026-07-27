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

A missing declaration may be filled automatically only when it is an exact
transcription of already-approved scope. Ambiguous or expanded scope requires
Product Owner or user approval. The gate evaluates the union of the declaration
and root gitlinks changed between the baseline and candidate revisions.

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

For exit `2`, correct the reported invocation, repository, baseline, candidate, or
scope error before relying on the result. Never reinterpret an error as a pass.

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

Unrelated root-submodule dirt and gitlink drift are warnings, not blockers. Do not
alter unrelated state as remediation for scoped promotion work.

## Ordered checklist (copy per story)

1. [ ] Exact promotion/adoption boundary agreed before coding.
2. [ ] Candidate API drafted in the owning technical module.
3. [ ] Direct affected-project build and compiled xUnit v3 execution green.
4. [ ] Helper is domain-neutral and tenant-aware where applicable.
5. [ ] Conversations duplicate deleted or reduced to a thin facade.
6. [ ] Behavior preserved or strengthened; guard tests remain equal or stronger.
7. [ ] Dependent sibling projects compile green.
8. [ ] Exact `submodule_promotions` scope recorded; remote requirements identified.
9. [ ] Each affected submodule committed separately, clean, and available remotely where required.
10. [ ] Root-only gitlinks committed in the umbrella repository and the mechanical completion gate passes.
