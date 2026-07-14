---
title: "Block incomplete submodule promotions from completion"
date: 2026-07-15
status: approved
change_scope: moderate
workflow: bmad-correct-course
approval: approved
approved_by: Jerome
approved_on: 2026-07-15
reconciles_with: sprint-change-proposal-2026-07-15.md
---

# Sprint Change Proposal: Block Incomplete Submodule Promotions from Completion

## 1. Issue Summary

Epic 3 requires every shared-capability promotion to land as a commit in the owning submodule plus a root-level gitlink bump. That requirement is documented in the epic and the promote/adopt runbook, but the workflows that assign `done` do not verify it mechanically.

This permits a story or spec to appear complete while promoted work remains dirty inside a submodule or while the umbrella repository still records an earlier submodule commit.

### Trigger and evidence

- Story 3.1 reached `done` while its completion record still described the root gitlink pointer bump as outstanding.
- Story 3.7 review found resolver tests outside the committed EventStore state and the root gitlink still pointing at the pre-review commit. Review had to commit the missing files and capture the corrected gitlink.
- The Epic 3 retrospective concluded that a runbook is not a gate and created action A2: add a blocking completion gate for submodule promotions.
- During analysis on 2026-07-14, FrontComposer, Memories, and Tenants demonstrated the state distinction the checker must handle: clean submodule worktrees whose checked-out commits differed from the umbrella repository's recorded gitlinks. Those changes belonged to separate work and were not absorbed by this correction.
- `bmad-code-review`, `bmad-quick-dev`, and `bmad-dev-auto` can currently write `done` without a committed-gitlink verification step. Quick-dev writes `done` before creating its local commit.

### Problem statement

The delivery process has a completion-control gap. For promotion-bearing work, story status is governed by review and task completion but not by the actual committed submodule and umbrella Git state. Prose evidence can therefore substitute for the state it claims to describe.

### Required invariant

Before any promotion-bearing story or spec reaches `done`, every affected root-declared submodule must be clean, its current commit must satisfy the declared availability policy, and the candidate umbrella commit must contain a mode-`160000` gitlink for that exact commit.

The affected set must be story-scoped. Unrelated concurrent submodule state is reported but cannot falsely block the scoped change.

### Non-goals

- Do not roll back or reimplement completed Epic 3 product work.
- Do not reopen or rewrite Epics 1–5; they remain immutable historical execution records under the approved major correction proposal.
- Do not change product behavior, public APIs, data models, deployment topology, PRD scope, or UX.
- Do not absorb or modify unrelated concurrent submodule work.
- Do not merge this correction with the separate final-story-record initiative covering test counts, File Lists, and contract-shape evidence.
- Do not initialize, update, or traverse nested submodules.

## 2. Impact Analysis

### Epic and story impact

Epic 3 remains the historical source of the trigger and action A2, but its text, stories, retrospective, and `done` status remain unchanged. The approved major correction proposal established Epic 6 as the only active corrective epic.

Add Story 6.7, **Mechanically block incomplete submodule promotions from completion**, to Epic 6. Sequence it after Story 6.1 establishes corrected architecture authority and before Story 6.2 or any later work that may promote submodule changes.

No new epic is required and no completed epic is invalidated or reopened.

### Artifact impact

| Artifact | Impact | Required change |
| --- | --- | --- |
| Product PRD | None | Preserve current MVP and requirements. |
| Epic plan | Direct | Append Story 6.7 to the active corrective Epic 6; preserve Epics 1–5. |
| Sprint status | Direct | Keep Epic 3 `done`, add Story 6.7 as `backlog`, and retain A2 as `open`. |
| Architecture | Coordinated | Add the completion invariant through Story 6.1's approved architecture rebaseline. |
| UX specification | None | No interface or interaction change. |
| Story/spec schemas | Direct | Add structured `submodule_promotions` scope. |
| Shared tooling | New | Add a read-only Python checker and Git-state fixture tests. |
| Completion workflows | Direct | Gate code-review, quick-dev, one-shot quick-dev, and dev-auto before `done`. |
| Dev-story | Direct | Add an earlier readiness gate before `review`. |
| Promotion runbook | Direct | Replace prose-only completion boxes with the canonical command and remediation rules. |
| Epic 3 retrospective | None | Preserve historical text; close A2 through sprint tracking after implementation. |

### Technical impact

The change introduces one read-only repository-state checker. It has no runtime, infrastructure, package, or deployment effect.

The checker uses an explicit promotion declaration because Git state alone cannot reliably attribute dirty work to one story in a concurrent workspace. It also inspects root gitlinks changed between the story baseline and candidate revision so an omitted committed pointer change cannot silently pass.

For each affected path, the checker validates:

1. The path is declared by the root `.gitmodules` file.
2. The submodule worktree is initialized and clean, including untracked files.
3. `HEAD` resolves to a commit.
4. If requested, that commit is contained by a locally known remote-tracking ref.
5. The candidate root commit records mode `160000` and the same object ID.

The checker never initializes, updates, fetches, pushes, commits, or enters nested submodules.

## 3. Recommended Approach

Use a direct, append-only adjustment within the already-approved corrective Epic 6.

### Why this path

- It fixes the actual authority boundary instead of adding more narrative guidance.
- It preserves valid delivered work and avoids dependency rollback risk.
- It leaves MVP scope and product direction unchanged.
- A single checker prevents completion workflows from developing inconsistent Git-state rules.
- Explicit scope supports concurrent work without weakening detection of committed gitlink changes.

### Alternatives considered

| Option | Decision | Reason |
| --- | --- | --- |
| Direct adjustment | Selected | Contained workflow correction with durable enforcement. |
| Roll back completed promotion stories | Rejected | High effort and risk; does not repair the completion workflow. |
| Reduce or redefine MVP | Rejected | Product scope is unaffected and scope reduction would not close the control gap. |
| Rely on the runbook alone | Rejected | Existing evidence demonstrates that prose checks do not reliably control `done`. |

### Estimate and risk

- **Effort:** Medium.
- **Risk:** Low to medium.
- **Timeline impact:** One focused corrective story before the next submodule promotion completes.
- **Primary risk:** false blocking from unrelated concurrent submodule state.
- **Mitigation:** combine approved scope declarations with baseline-to-candidate gitlink detection; treat state outside both sets as warnings.

## 4. Detailed Change Proposals

### 4.1 Append-only Epic 6 Story 6.7

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

Preserve the completed Epic 3 text. Append the following story to Epic 6 when the approved Epic 6 plan is materialized in `epics.md`:

#### Story 6.7: Mechanically block incomplete submodule promotions from completion

As a Hexalith development-workflow maintainer,
I want promotion-bearing work to pass a mechanical submodule completion gate,
So that dirty submodules and uncaptured umbrella gitlinks cannot reach `done`.

**Acceptance Criteria:**

1. Promotion-bearing stories and specs declare exact root `references/...` paths and whether remote commit availability is required.
2. The gate evaluates the declared paths plus root gitlinks changed between the work baseline and candidate umbrella revision.
3. Each affected submodule is initialized, clean including untracked files, at a commit satisfying its availability policy, and exactly represented by a mode-`160000` gitlink in the candidate root commit.
4. Failures produce stable machine-readable blocker codes and actionable text, exit nonzero, and prevent completion workflows from writing or synchronizing `done`.
5. Unrelated state outside the declaration and changed-gitlink set is reported without blocking.
6. Discovery is limited to root `.gitmodules`; nested submodules are neither initialized nor traversed.
7. Automated Git fixtures prove success and all specified blocking and concurrency cases.

Add an Epic 6 sequencing note: Story 6.7 follows Story 6.1 and must complete before Story 6.2 or any later promotion-bearing work can reach `done`.

### 4.2 Sprint tracking

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

Preserve all Epic 1–5 entries, including:

```yaml
  epic-3: done
  3-7-promote-adopt-compile-time-command-event-contract-metadata: done
  epic-3-retrospective: done
```

Extend the approved Epic 6 backlog:

```yaml
  6-6-revalidate-and-issue-superseding-attestation: backlog
  6-7-mechanically-block-incomplete-submodule-promotions-from-completion: backlog
  epic-6-retrospective: optional
```

Add a dated reconciliation comment. Keep A2 `open` until Story 6.7 passes independent verification. After implementation, set Story 6.7 and A2 to `done`; Epic 6 remains governed by the status of all its corrective stories.

### 4.3 Architecture invariant

**Coordinating artifacts:** the approved Story 6.1 architecture rebaseline and `_bmad-output/planning-artifacts/architecture.md`

Add this invariant while Story 6.1 rewrites `Development Workflow Integration → Build Process Structure`:

> Submodule promotion completion is a mechanically enforced repository invariant. Before promotion-bearing work reaches `done`, every affected root-declared submodule must have a clean worktree, satisfy its declared commit-availability requirement, and be represented by an exact mode-`160000` gitlink in the committed umbrella revision. The gate operates only on root `.gitmodules` declarations, never initializes or traverses nested submodules, and scopes blockers to the promotion declaration plus gitlinks changed since the work baseline.

Story 6.7 implements the invariant; Story 6.1 owns its placement in the corrected architecture. This proposal does not independently patch the superseded architecture before Story 6.1 runs.

### 4.4 Shared checker and tests

**New artifacts:**

- `_bmad/scripts/verify_submodule_promotion.py`
- `_bmad/scripts/tests/test_verify_submodule_promotion.py`

Canonical interface:

```bash
python3 _bmad/scripts/verify_submodule_promotion.py \
  --repository <root> \
  --baseline <story-baseline-commit> \
  --candidate <committed-umbrella-revision> \
  --submodule references/Hexalith.EventStore \
  --require-remote references/Hexalith.EventStore \
  --format json
```

`--submodule` and `--require-remote` are repeatable. Candidate defaults to `HEAD`; repository defaults to the project root.

Exit codes:

- `0`: gate passed.
- `1`: valid invocation with completion blockers.
- `2`: invalid invocation or repository state prevents a trustworthy decision.

Required fixtures cover clean/captured success; tracked and untracked dirt; old or mismatched root gitlinks; deterministic remote-availability failure and success; changed-but-undeclared gitlinks; unrelated dirty warnings; nested-submodule non-traversal; and invalid scope.

### 4.5 Promotion-scope declaration

**Artifacts:**

- `.agents/skills/bmad-create-story/template.md`
- `.agents/skills/bmad-create-story/SKILL.md`
- `.agents/skills/bmad-quick-dev/spec-template.md`
- `.agents/skills/bmad-quick-dev/step-02-plan.md`
- `.agents/skills/bmad-dev-auto/spec-template.md`
- `.agents/skills/bmad-dev-auto/step-02-plan.md`

Add the shared field:

```yaml
submodule_promotions: []
# Example:
# submodule_promotions:
#   - path: references/Hexalith.EventStore
#     require_remote: true
```

Non-promotion work uses an empty list. Promotion work lists every affected root-declared path exactly once. Existing work may receive a missing declaration automatically only when it is an exact transcription of already-approved scope; ambiguous or expanded scope requires Product Owner or user approval.

### 4.6 Code-review completion boundary

**Artifact:** `.agents/skills/bmad-code-review/steps/step-04-present.md`

Before the current status decision, run the checker with the story baseline, committed `HEAD`, and declared scope. If scope exists or a gitlink changed, missing baseline data is a blocker. Any nonzero result forces `in-progress`, prevents the `done` branch, preserves blocker codes in the review record, and synchronizes only `in-progress`.

The existing review decision applies only after the gate passes:

- gate passed and blocking findings resolved: `done`;
- gate passed but findings remain: `in-progress`;
- gate failed: `in-progress` regardless of finding disposition.

### 4.7 Quick-dev completion boundary

**Artifacts:**

- `.agents/skills/bmad-quick-dev/step-05-present.md`
- `.agents/skills/bmad-quick-dev/step-oneshot.md`

For activated promotion work, change the order from `mark done → commit` to:

```text
capture baseline → keep in-review → commit scoped implementation and gitlinks
→ run gate against committed candidate → mark done → commit completion record
```

The one-shot route must initially create its trace as `in-review` with baseline and promotion scope. On gate failure, it remains or returns to `in-progress` and never writes `done`. Non-promotion behavior remains unchanged.

### 4.8 Dev-auto completion boundary

**Artifact:** `.agents/skills/bmad-dev-auto/step-04-review.md`

Run the checker after the existing local commit and before writing `final_revision` or `status: done`. For activated promotion work, unavailable VCS, missing trustworthy inputs, or any blocker halts with `status: blocked`, records diagnostics in `Auto Run Result`, and never writes a successful final revision or `done`.

### 4.9 Dev-story readiness gate

**Artifacts:**

- `.agents/skills/bmad-dev-story/SKILL.md`
- `.agents/skills/bmad-dev-story/checklist.md`

Before Step 9 changes a promotion-bearing story to `review`, run the checker using `baseline_commit` and committed `HEAD`. Failure leaves story and sprint state `in-progress`, records diagnostics, and halts for remediation. Dev-story reads the approved scope but cannot silently expand it. Code-review repeats the check as the final `done` authority.

### 4.10 Promotion runbook

**Artifact:** `docs/release-evidence/promote-adopt-runbook.md`

Replace the prose-only closing checks with:

```markdown
8. [ ] Exact `submodule_promotions` scope recorded; remote requirements identified.
9. [ ] Each affected submodule committed separately, clean, and available remotely where required.
10. [ ] Root-only gitlinks committed in the umbrella repository and the mechanical completion gate passes.
```

Document the canonical command, exit meanings, remediation, `in-progress`/`blocked` behavior, and the prohibition against recursive submodule commands. A staged pointer bump or prose completion note is not gate evidence.

## 5. Implementation Handoff

### Scope classification

**Moderate within the approved major correction.** Product scope is unchanged. This proposal appends one workflow story to active Epic 6 without changing completed historical backlog state.

### Responsibilities

| Role | Responsibility |
| --- | --- |
| Product Owner | Register Story 6.7 in Epic 6, preserve Epics 1–5, and maintain sprint state. |
| Developer | Implement the checker, fixtures, schemas, workflow hooks, and runbook changes. |
| Architect | Review the completion invariant and story-scoping semantics. |
| Independent reviewer | Exercise passing and blocking repository states before A2 closes. |

### Implementation sequence

1. Preserve the approved Epic 6 backlog and append Story 6.7 plus its sequencing note.
2. Complete Story 6.1's authority rebaseline, including the architecture invariant.
3. Create the Story 6.7 implementation artifact from the approved epic text.
4. Implement the read-only checker and its isolated temporary-Git fixtures.
5. Add the structured promotion-scope schema to story/spec planning.
6. Integrate code-review, quick-dev, one-shot, dev-auto, and dev-story.
7. Update the promotion runbook.
8. Run checker tests and independently prove blocking and success paths.
9. Set Story 6.7 and A2 to `done`; continue Epic 6 with Story 6.2.

### Completion evidence

Story 6.7 is complete only when evidence demonstrates:

- clean and captured promotion passes;
- tracked or untracked submodule dirt blocks;
- submodule `HEAD` not captured by the candidate root commit blocks;
- mismatched and undeclared root gitlinks block;
- the remote requirement blocks and passes deterministically;
- unrelated dirty submodules warn without blocking;
- nested submodules are not initialized or traversed;
- code-review, quick-dev, one-shot, and dev-auto cannot write `done` after a failed gate;
- dev-story cannot move failed promotion state to `review`;
- no unrelated worktree file or existing submodule-upgrade artifact is modified.

### Final success criterion

No completion-capable workflow can write `done` for promotion-bearing work unless every affected root-declared submodule is clean and the committed umbrella gitlink exactly matches its current commit. Unrelated concurrent state remains visible without becoming a false blocker.

## 6. Approval and Routing

Incremental edit review: complete; all ten original proposals were approved individually.

Reconciliation review: Jerome approved preserving Epics 1–5, moving the work to Story 6.7, sequencing it after Story 6.1 and before Story 6.2, and folding the architecture invariant into the Story 6.1 rebaseline.

Final proposal approval: approved by Jerome on 2026-07-15.

Finalized actions:

1. Proposal marked `approved` and reconciled with the approved major correction proposal.
2. Story 6.7 registered in the Epic 6 sprint backlog without changing Epics 1–5.
3. Implementation routed to Product Owner and Developer, with Architect ownership of the Story 6.1 invariant.

Checklist Section 6 is complete: proposal accuracy was revalidated after reconciliation, explicit final approval was received, sprint status was updated append-only, and the implementation handoff is defined.
