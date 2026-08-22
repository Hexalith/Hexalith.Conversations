---
project: Conversations
date: 2026-08-19
trigger: "Implementation-readiness FAIL after V14 publication: A2/A3 closure, static anti-skip guard, IR-0, and hold decision remain outstanding"
mode: incremental
scope: minor
status: approved
implementation_hold: ACTIVE
---

# Sprint Change Proposal — Close E6 Remediation After V14 Publication

- **Author:** Dev workflow (`bmad-correct-course`), for Jerome
- **Trigger:** `_bmad-output/planning-artifacts/implementation-readiness.md`
- **Planning publication:** `fe3f6fae3640ae2a6dc7629ac13e0ce0daa31029`
- **Companion publication:** `62f27c4`
- **Scope classification:** **Minor** — one bounded static test plus formal A2/A3 evidence and lifecycle closure; no backlog reorganization
- **Status:** **APPROVED** by Jerome on 2026-08-20
- **Implementation hold:** **ACTIVE — unchanged**
- **Lifecycle effect now:** none; this proposal itself does not close A2/A3, run IR-0, or change the hold

## 1. Issue Summary

The V14 planning publication resolved two former implementation-readiness blockers:

1. F-10 now proves dirty-worktree rejection against a controlled temporary Git repository and is hermetic.
2. Epic 6 actions A4–A6 now map exactly to Epic 16 Stories 16.1–16.3.

The remaining readiness failure is narrower and entirely downstream of that publication:

| Blocker | Current state | Required disposition |
| --- | --- | --- |
| A2 | `open`; implementation spec remains `in-progress` | Apply the approved record-only closure note and close through `bmad-build` lifecycle gates. |
| A3 | `open` | Add the missing static anti-skip guard, complete fresh candidate-bound verification, then close after A2. |
| IR-0 | Not run | Run independently only after A1–A3 are closed at a committed candidate. |
| Implementation hold | `ACTIVE` | A release-owner `LIFTED` decision is valid only after candidate-matched IR-0 `READY`. |

### 1.1 Evidence

- `_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py` now creates controlled tracked dirt and asserts `E6_CURRENT_PROOF_WORKTREE_DIRTY`.
- A source scan of `_bmad/scripts/tests` finds no current `pytest.skip`, `skipif`, or ambient `verifier.worktree_dirt(ROOT)` execution, but it also confirms that no static regression guard enforces their continued absence.
- The approved Task-3 contract in `sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md` requires that guard separately from the hermetic F-10 proof.
- `sprint-status.yaml` still records Epic 6 remediation actions A2 and A3 as `open`.
- The current readiness report requires the sequence: finish the shared evidence repair, close A2, close A3, run independent IR-0, obtain the separate hold decision, and rerun readiness.

### 1.2 Sequence clarification

The older A3 proposal requires the A3 technical repair before A2 review can execute. The later approved readiness-gate proposal requires A2's formal transition before A3's formal transition. These statements are compatible and compose into one sequence:

1. Complete the remaining shared A3 technical evidence work, including the static guard.
2. Rerun the required lanes.
3. Close A2 formally.
4. Close A3 formally.
5. Run IR-0 independently.

No authority amendment is needed to establish this order.

## 2. Impact Analysis

### Epic and story impact

- No epic or story is added, removed, reordered, reopened, or amended.
- Epic 16 and its A4–A6 mappings remain unchanged.
- All Stories 7.1–16.3 remain non-executable while the implementation hold is active.
- A2 and A3 are non-story remediation action rows; their lifecycle closure does not constitute story execution or release authorization.

### Artifact impact

| Artifact | Impact |
| --- | --- |
| PRD | None. No requirement, MVP, success metric, or open-question change. |
| `epics.md` | None. V14 and Epic 16 remain current. |
| `architecture.md` | None. V14 ownership, graph, and hold rules remain current. |
| UX specification/map | None. UX remains `preserved-not-activated`. |
| Python tooling tests | Add one static anti-skip regression guard. |
| A2 implementation spec | Apply the already-approved record-only change-log note and transition to `done` only after gates pass. |
| `sprint-status.yaml` | Transition A2, then A3, from `open` to `done` only through their approved lifecycle gates. |
| Implementation-readiness report | Preserve the current report as the point-in-time FAIL result; rerun readiness after IR-0 and the hold decision. |

### Technical impact

The only code change is test-only. It inspects Python test source statically and changes no verifier behavior, runtime behavior, public contract, package, dependency, deployment configuration, submodule, or gitlink.

The lifecycle updates are evidence-bearing status transitions. They must consume fresh results from the closure candidate and must not infer success from the previously reported validation summary alone.

### Publication and Git boundary

- Preserve the two V14 publication commits exactly; do not amend, rebase, squash, or rewrite them.
- Preserve the intentionally uncommitted `implementation-readiness.md` point-in-time assessment.
- Any implementation and closure work is a descendant change with independently validated commit messages and scope.
- This proposal authorizes no push.

## 3. Recommended Approach

**Selected path: Direct Adjustment.** Implement the already-approved static guard, rerun the complete evidence contract, and perform the two formal action transitions in the approved order.

- **Effort:** Low — one bounded test plus lifecycle evidence/record updates.
- **Product risk:** Low — no product or runtime code changes.
- **Governance/evidence risk:** Medium — a false or premature status transition would incorrectly make IR-0 appear eligible.
- **Timeline impact:** One bounded Developer and Architecture/Quality closure cycle before independent IR-0; no product-scope or backlog expansion.

**Rollback is not appropriate.** F-10, A2 implementation, A3 implementation, and the V14 publication are valid work. Reverting them would recreate resolved defects without closing the remaining evidence gap.

**MVP/PRD review is not appropriate.** No product goal, requirement, metric, or UX activation boundary changes.

## 4. Detailed Change Proposals

### CP-1 — Add the static anti-skip regression guard

**Artifact:** `_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py`

**OLD:**

```text
F-10 runs against controlled repository dirt, but nothing mechanically prevents
pytest.skip, skipif, or ambient verifier.worktree_dirt(ROOT) conditioning from
being reintroduced elsewhere under _bmad/scripts/tests.
```

**NEW:**

```text
Add an AST-based static test that recursively examines _bmad/scripts/tests/*.py
and fails with file-and-line diagnostics upon finding:

1. an actual pytest.skip(...) call;
2. a skipif marker, decorator, or call; or
3. an actual verifier.worktree_dirt(ROOT) call.

The guard itself runs on every invocation. The full lane has identical collected
and passed counts on clean and dirty trees, zero skips/not-run tests, and both
F-10 and the static guard are collected.
```

The guard must inspect AST behavior rather than raw explanatory strings or comments, so its own diagnostics do not self-trigger. It must not change `verify_epic_6_completion_supersession.py` or weaken another test.

**Rationale:** Implements the approved Task-3 contract and prevents the ambient-state skip class from silently returning.

### CP-2 — Close A2 through its lifecycle gates

**Artifacts:**

- `_bmad-output/implementation-artifacts/spec-e6-remediation-a2-restore-lifecycle-gates.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Precondition:** CP-1 is implemented and the required focused and full lanes pass with zero failed, skipped, or not-run tests.

**OLD:**

```yaml
status: 'in-progress'
```

```markdown
## Spec Change Log
```

```yaml
status: open
```

**NEW:**

```yaml
status: 'done'
```

Append the already-approved record-only note outside the frozen intent block:

```markdown
- 2026-08-18 — Closure note (record only; no acceptance change). The A2 focused lane measures
  28 collected / 28 passed / 0 failed / 0 skipped / 0 not-run / 129 deselected at `29c56fa`, and all
  nine `-k` terms resolve to real tests. The "30/30" count attributed to this spec in
  sprint-change-proposal-2026-08-18-e6-remediation-a3.md AC-11 was never present here; it was
  misattributed from the Story 6.7 and Story 6.2 records. Resolved by
  sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md CP-2. The frozen intent, boundaries,
  I/O matrix, and acceptance criteria are unchanged.
```

Then transition only the A2 sprint action:

```yaml
status: done
```

The historical 28/28 note is preserved exactly as approved. Fresh closure-candidate results are recorded separately by the lifecycle workflow and do not replace those historical facts.

**Rationale:** A2 implementation is present, but formal candidate-bound evidence and lifecycle closure remain outstanding.

### CP-3 — Close A3 after A2

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Preconditions:**

1. CP-1 passes.
2. The complete A3 acceptance and fault-injection contract passes against the closure candidate with zero failures, skips, or not-run tests.
3. A2 is formally `done`.

**OLD:**

```yaml
- id: "epic-6-retro-item-26-harden-planning-authority-verification-t"
  owner: "Architecture / Quality"
  status: open
```

**NEW:**

```yaml
- id: "epic-6-retro-item-26-harden-planning-authority-verification-t"
  owner: "Architecture / Quality"
  status: done
```

Closure evidence records the fresh candidate, full Python lane, authority-validation lane, Release build, complete A3 fault matrix, publication/scope verification, and zero gitlink changes.

The existing A3 Sprint Change Proposal remains `APPROVED`; it is planning authority and is not rewritten as an implementation record.

**Rationale:** Closes the final E6 remediation action without manufacturing IR-0 or hold evidence.

## 5. Implementation Handoff

**Classification:** Minor.

| Sequence | Work | Owner / workflow | Completion condition |
| ---: | --- | --- | --- |
| 1 | Approve this Sprint Change Proposal | Jerome | Explicit approval recorded. |
| 2 | Implement CP-1 | Developer / `bmad-build` | Static guard is collected and passes; verifier behavior is unchanged. |
| 3 | Rerun focused and full evidence lanes | Developer / `bmad-build` | Clean and controlled-dirty executions collect and pass the same tests; zero failed/skipped/not-run. |
| 4 | Apply CP-2 and close A2 | Developer / `bmad-build` | A2 spec and sprint action transition through all lifecycle gates. |
| 5 | Apply CP-3 and close A3 | Architecture / Quality / `bmad-build` | Complete A3 contract is green at the same closure candidate; action becomes `done`. |
| 6 | Run IR-0 | Independent assessor | Full unchanged report binds the committed candidate and current authority; no target verdict is prescribed. |
| 7 | Record the hold decision | Jerome, release owner | Only candidate-matched IR-0 `READY` permits an explicit `LIFTED`; otherwise hold remains `ACTIVE`. |
| 8 | Rerun readiness | `bmad-sprint-planning` readiness intent | Proceed only if the gate returns `PASS`; tracking refresh occurs only then. |

### Success criteria

1. `_bmad/scripts/tests` is mechanically guarded against the three approved ambient-skip constructs.
2. F-10 and the guard execute on every full-lane run with identical clean/dirty collection and pass counts.
3. A2 closes through its lifecycle gates without modifying its frozen contract.
4. A3 closes only after its complete candidate-bound evidence and after A2 is `done`.
5. IR-0 remains independent and outcome-neutral.
6. The implementation hold remains `ACTIVE` until a valid release-owner decision explicitly changes it.
7. No product, dependency, submodule, gitlink, completed-record, V14 authority, PRD, architecture, or UX change occurs.

## 6. Explicit Non-Authorizations

This proposal does not:

- implement CP-1 merely by being approved;
- close A2 or A3 without their lifecycle gates;
- run, bias, or predetermine IR-0;
- lift or weaken the implementation hold;
- create a hold-decision artifact before candidate-matched IR-0 `READY`;
- start Story 7.1, `7.1-SCHEMAS`, Epic 16, or any other successor work;
- close A4–A6;
- authorize release or a push;
- amend, rebase, squash, or otherwise rewrite the two V14 publication commits;
- modify product code, packages, dependencies, submodules, nested submodules, or gitlinks;
- rewrite completed story records, accepted evidence, signed evidence, or V1–V14 authority history.

## 7. Change Navigation Checklist Record

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Trigger/context | [x] Done | Readiness remains FAIL after V14 resolved F-10 hermeticity and A4–A6 ownership. |
| 1.2 Core problem | [x] Done | Static guard and formal A2/A3 closure remain; IR-0 and hold decision are correctly downstream. |
| 1.3 Evidence | [x] Done | Current readiness report, approved A3/readiness proposals, A2 spec, tests, sprint ledger, V14 epics/architecture, PRD, and UX were reconciled. |
| 2.1 Current plan completable | [x] Done | Yes; no new epic or story is required. |
| 2.2 Epic-level changes | [N/A] | Epic 16 already resolves the prior ownership gap. |
| 2.3 Remaining epics | [N/A] | No successor definition changes. |
| 2.4 New epic needed | [N/A] | None. |
| 2.5 Order/priority | [x] Done | Shared repair → A2 closure → A3 closure → IR-0 → hold decision → readiness. |
| 3.1 PRD conflicts | [N/A] | None. |
| 3.2 Architecture conflicts | [N/A] | None. |
| 3.3 UI/UX conflicts | [N/A] | None. |
| 3.4 Other artifacts | [!] Action-needed | Static test, A2 spec record/status, and A2/A3 sprint action statuses. |
| 4.1 Direct adjustment | [x] Viable | Selected; bounded test and evidence closure. |
| 4.2 Rollback | [x] Not viable | Would recreate resolved defects. |
| 4.3 MVP review | [x] Not viable | No product-goal or scope change. |
| 4.4 Recommended path | [x] Done | Minor direct adjustment through `bmad-build`. |
| 5.1–5.5 Proposal components | [x] Done | Sections 1–5. |
| 6.1 Checklist completion | [x] Done | Applicable findings recorded. |
| 6.2 Proposal accuracy | [x] Done | Cross-checked against current local authority and source. |
| 6.3 User approval | [x] Done | Approved by Jerome on 2026-08-20. |
| 6.4 Sprint status update | [!] Pending | Only through CP-2/CP-3 lifecycle gates. |
| 6.5 Handoff | [x] Done | Section 5. |

## 8. Approval Record

**Decision:** **APPROVED** by Jerome on 2026-08-20.

Approval authorizes the bounded implementation and lifecycle handoff in CP-1–CP-3. It does not itself implement the guard, close either action, run IR-0, lift the hold, authorize successor execution, authorize release, or authorize a push.

**Handoff:** Developer owns CP-1 and CP-2 through `bmad-build`; Architecture / Quality owns CP-3 after A2 closes. Independent IR-0, the candidate-matched release-owner hold decision, and the subsequent readiness rerun remain conditional downstream steps.
