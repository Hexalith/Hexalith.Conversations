# Sprint Change Proposal — Readiness-gate FAIL 2026-08-18 (AC-10 / AC-11)

- **Date:** 2026-08-18
- **Author:** Dev workflow (bmad-correct-course), for Jerome
- **Trigger:** Readiness gate **FAIL** at `29c56fa0b587636c00c72d44ebfc24b3cde35e34`
- **Amends:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18-e6-remediation-a3.md` §7 (AC-10, AC-11) and §8 (F-10)
- **Scope classification:** **Moderate** — amends acceptance criteria inside an already-APPROVED
  proposal (AC-11 count handling was one of its four release-owner-approved decisions), so it
  requires release-owner re-approval before Dev executes.
- **Implementation hold:** **ACTIVE — unchanged by this proposal**
- **Authorizes:** nothing beyond the AC amendments below and one bounded test-hermeticity task.
  Not IR-0, not A2 closure, not A3 closure, not Story 7.1, not release, not hold lift.
- **Lifecycle values changed:** **zero**. `sprint-status.yaml` is not modified by this proposal.
- **Mode:** Batch (selected by Jerome)
- **Status:** **APPROVED** by Jerome (release owner) on 2026-08-18 at HEAD `29c56fa`, working
  tree carrying only this proposal plus the disclosed `references/Hexalith.FrontComposer` drift.
  Approval covers CP-1, CP-2, CP-3 and CP-4, and the strengthen-not-relax decision on AC-10.
  **Approval does not lift the hold, close A2 or A3, or authorize anything in §6.**

---

## 1. Issue summary

The readiness gate failed with three blocking findings. Re-measurement at HEAD `29c56fa` shows the
three are of **three different kinds**, and only two are course-correction matters.

| # | Gate finding | Kind | Disposition |
|---|---|---|---|
| 1 | Global implementation hold ACTIVE; IR-0 not run; Epics 7–15 non-executable | **Working as designed** | No amendment. The gate is correctly reporting the intended state. |
| 2 | A2 "recorded count discrepancy" (28 passed vs a recorded 30) | **Record error in the A3 proposal** | Amend AC-11. The discrepancy does not exist. |
| 3 | A3 full lane produced 264 passed / 1 skipped, violating AC-10's zero-skip rule | **Acceptance criterion too weak, plus a test defect** | Strengthen AC-10 (here); fix the test (bmad-build). |

### 1.1 Finding 1 is not a defect

`epics.md:4312` defines `E6-REMEDIATION` as the exact predecessor of `IR-0`, with A1–A3 all required
before IR-0 may run. `sprint-status.yaml:41` and `v14-current-candidate-authority-v1.json`
(`"implementationHold": "ACTIVE"`, all four `completionEffect` flags false) record exactly that.
A1 is `done`; A2 and A3 are `open`. Epics 7–15 being non-executable is the designed consequence,
not a deviation. **No artifact changes. No amendment proposed.**

### 1.2 Finding 2 — the A2 discrepancy is a misattributed number

The A3 proposal, AC-11, states: *"The A2 spec text records 30/30."* It does not.

- `spec-e6-remediation-a2-restore-lifecycle-gates.md` is **byte-identical to its only commit**
  (`a232614`; `git diff a232614 --stat` returns empty) and contains **no test count at all** —
  its acceptance is qualitative: *"zero failed, skipped, or not-run selected tests."*
- The `30/30` strings in this repository belong to **unrelated records**: Story 6.7's
  checker/workflow suite (`6-7-…md:517`), Story 6.2's EventStore dispatcher (`6-2-…md:1704`), and
  the provenance ledger's copy of the same 6.2 line (`sprint-status-provenance-v1.md:45`).
  `git log -S "30/30"` finds no A2 origin.
- The A2 `-k` selection collects **exactly 28** tests, and **all nine `-k` terms resolve to real
  test functions** (no term matches zero tests, so no intended assertion is silently absent).
- The lane result is **28 collected / 28 passed / 0 failed / 0 skipped / 0 not-run / 129 deselected**.

A2's own acceptance criterion is therefore **already met**. The only thing blocking A2 closure was a
number that was never in A2.

### 1.3 Finding 3 — AC-10 is currently satisfiable by accident

The gate observed 264 passed / 1 skipped. **My run at the same HEAD produced 265 passed, 0 skipped.**

The suite did not change. The *repository* did:

```
 M references/Hexalith.FrontComposer      90954ac → 27c3a02   (unstaged gitlink move)
```

That is a **concurrent session's Story 9.5 code-review work inside a sibling submodule** — unrelated
to Conversations, owned by another agent, and not captured by anything here.

`test_dirty_tracked_worktree_blocks_current_proof` (fault F-10) reads:

```python
dirt = verifier.worktree_dirt(ROOT)
if not dirt:
    pytest.skip("repository worktree is clean; the strict default is exercised by CI")
```

So the test's outcome is decided by **ambient repository state that nobody controls**. Two
consequences, both worse than the reported skip:

1. **AC-10 can pass for the wrong reason.** `0 skipped` was satisfied in my run only because a
   foreign submodule happened to be dirty. A "green" full lane is not evidence that F-10 holds.
2. **The skip's stated justification is false.** `.github/workflows/planning-authority-preflight.yml`
   checks out with `actions/checkout@v4` (`submodules: true`) and runs pytest before any build step.
   CI's tree is **clean**, so this test **always skips in CI**. The strict dirty-worktree default is
   therefore proven *nowhere* — not locally by design, not in CI at all.

This is a verification gap in a fail-closed evidence guard (`E6_CURRENT_PROOF_WORKTREE_DIRTY`,
`verify_epic_6_completion_supersession.py:785-791`) whose entire purpose is to stop uncommitted bytes
being attributed to a resolved commit. Relaxing AC-10 to tolerate the skip would bless that gap and
would additionally contradict `v14-current-candidate-authority-v1.json`
→ `resultSemantics.skipsAllowed: false`, forcing a V14 amendment on top.

**Decision (Jerome, 2026-08-18): strengthen AC-10; do not relax it.**

### 1.4 Evidence

| ID | Evidence | Source |
|---|---|---|
| E-1 | A2 selection collects exactly 28; all nine `-k` terms resolve to real tests | `pytest --collect-only` at `29c56fa` |
| E-2 | A2 focused lane: 28 passed, 0 failed, 0 skipped, 129 deselected | `pytest -q` at `29c56fa` |
| E-3 | A2 spec unmodified vs `a232614` and records no count | `git diff a232614 --stat`; full-text grep |
| E-4 | `30/30` exists only in Story 6.7 / 6.2 / provenance records | repo-wide grep; `git log -S` |
| E-5 | Full lane at `29c56fa`: **265 passed, 0 skipped** (gate saw 264 + 1 skip) | `pytest -q -rs` |
| E-6 | Cause of the delta: unstaged `references/Hexalith.FrontComposer` 90954ac→27c3a02 | `git status --porcelain`, `git diff --submodule=diff` |
| E-7 | The skip is conditioned on `verifier.worktree_dirt(ROOT)` | `test_verify_epic_6_completion_supersession.py:477-484` |
| E-8 | CI's tree is clean at pytest time → the test always skips in CI | `.github/workflows/planning-authority-preflight.yml:16-75` |
| E-9 | Exactly one test in the suite conditions on ambient state | grep for `pytest.skip` / `skipif` across `_bmad/scripts/tests` |

---

## 2. Impact analysis

### Epic impact

**None.** No epic scope, sequence, or acceptance changes. `E6-REMEDIATION` remains the closed A1–A3
inventory with exact predecessor `PC-PUBLICATION` and exact successor `IR-0`. No epic is invalidated,
added, deferred, or resequenced. Epics 7–15 remain `backlog` and non-executable.

### Story impact

**None.** `E6-REMEDIATION` has no sprint lifecycle key and produces no story final record. No story
file is created, modified, or reopened. Completed Story 6.2 / 6.7 records are untouched.

### Artifact conflicts

| Artifact | Conflict | Action |
|---|---|---|
| `sprint-change-proposal-2026-08-18-e6-remediation-a3.md` §7 AC-10 | Criterion is satisfiable by ambient state | **Amend** — CP-1 |
| `sprint-change-proposal-2026-08-18-e6-remediation-a3.md` §7 AC-11 | Asserts a count the A2 spec does not contain | **Amend** — CP-2 |
| `sprint-change-proposal-2026-08-18-e6-remediation-a3.md` §8 F-10 row | Does not say the dirt must be controlled | **Amend** — CP-3 |
| `spec-e6-remediation-a2-restore-lifecycle-gates.md` | No contract defect; its AC is met | **No contract change.** Record-only Spec Change Log entry at closure — CP-4 |
| `v14-current-candidate-authority-v1.json` | None — `skipsAllowed: false` is *reinforced* | **Unchanged.** Explicitly reaffirmed. |
| `sprint-status.yaml` | None | **Unchanged.** A2/A3 stay `open`; zero lifecycle or action-item values change. |
| PRD / `epics.md` / architecture / UX spec | None — no requirement, MVP, contract, or design change | **Unchanged.** |
| `.github/workflows/planning-authority-preflight.yml` | Runs the lane that must become deterministic | **Unchanged** — the fix belongs in the test, not the pipeline. |

### Technical impact

Bounded to one test function in one file: `test_dirty_tracked_worktree_blocks_current_proof` in
`_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py`, plus one new static guard test.
**No product code under `src/`. No packages. No submodules. No gitlinks. No verifier logic change** —
`verify_epic_6_completion_supersession.py:785-791` is already correct; only its *proof* is defective.

### Working-tree advisory (not part of this change)

`references/Hexalith.FrontComposer` carries an **unstaged** gitlink move (90954ac→27c3a02) from a
concurrent session. It is disclosed, not captured and not reverted — reverting would destroy another
session's committed work. Whoever commits A2/A3 must **declare it and must not stage it**; A3's AC-12
("all ten gitlinks remain at their `a232614` values") is satisfied as long as no commit captures it.
Note that this drift also currently *masks* the AC-10 defect by making the lane look 0-skipped.

---

## 3. Recommended approach

**Option 1 — Direct Adjustment. Selected.** Effort **Low**, risk **Low**.

Amend two acceptance criteria and one fault row in place; route one bounded test fix to `bmad-build`.
No new epic, no new story, no backlog restructuring.

**Option 2 — Rollback: not viable and not needed.** Nothing is wrong with the delivered A2 or A3
work. A2's gates are restored and green; A3's repairs are intact. Rolling back would discard correct
work to fix a record error and a test guard.

**Option 3 — PRD/MVP review: not applicable.** No requirement, MVP boundary, or product goal is
implicated. This is entirely inside the E6 remediation evidence contract.

### Rationale

The gate offered two exits for finding 3: make the test hermetic, or formally amend the criterion.
Measurement showed the honest answer is **neither alone** — the criterion is *too weak*, not too
strict. `0 skipped` was met accidentally at the same HEAD where the gate saw it violated. Amending
AC-10 to accept the skip would have converted a live verification gap into an approved one, and would
have required weakening `v14`'s `skipsAllowed: false` as collateral. Strengthening AC-10 closes the
gap and gives `bmad-build` an unambiguous target.

For finding 2, no contract needed changing at all: the blocking "discrepancy" was a number
misattributed from two unrelated story records. Correcting the record unblocks A2 without touching
its frozen intent.

---

## 4. Detailed change proposals

### CP-1 — Strengthen AC-10 (state-independence + controlled dirt)

**Artifact:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18-e6-remediation-a3.md`
**Section:** §7 Acceptance criteria, AC-10 (lines 483–484)

**OLD:**
```
**AC-10 — Full lane.** `uv run --frozen python3 -m pytest -q _bmad/scripts/tests` reports **0 failed,
0 skipped, 0 not-run**, with passed ≥ 225 (215 current + the 10 repaired) plus new tests.
```

**NEW:**
```
**AC-10 — Full lane.** `uv run --frozen python3 -m pytest -q _bmad/scripts/tests` reports **0 failed,
0 skipped, 0 not-run**, with passed ≥ 225 (215 current + the 10 repaired) plus new tests.

The lane result MUST additionally be **independent of ambient worktree state**: the same collected
count, the same passed count, and 0 skipped on a **clean** tree and on a **dirty** tree alike. No
test may condition its execution on `verifier.worktree_dirt(ROOT)` or on any other live repository
state, and no test may call `pytest.skip`. Fault F-10 MUST be proven against **controlled** dirt
supplied by the test itself, never against ambient dirt. A skipped test is never a pass, whatever
the reason recorded in its skip message.

*Amended 2026-08-18 by sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md. Rationale: at
`29c56fa` the same lane produced 264 passed / 1 skipped and 265 passed / 0 skipped depending only on
whether an unrelated sibling submodule happened to be dirty. `0 skipped` was therefore satisfiable by
accident, and the skip's own justification ("exercised by CI") is false — CI checks out a clean tree,
so the strict default was proven nowhere.*
```

**Rationale:** Converts a criterion that ambient state could satisfy into one that only a hermetic
suite can satisfy. Consistent with `v14` `resultSemantics.skipsAllowed: false`, which needs no edit.

---

### CP-2 — Correct AC-11 (close the phantom A2 discrepancy)

**Artifact:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18-e6-remediation-a3.md`
**Section:** §7 Acceptance criteria, AC-11 (lines 486–488)

**OLD:**
```
**AC-11 — A2 focused lane.** The A2 spec's `-k` selection returns **0 failed, 0 skipped, 0 not-run**.
*Measured on rerun at this tree: **28 passed, 129 deselected**. The A2 spec text records 30/30; the
delta is surfaced here rather than silently reconciled, and must be resolved before A2 closes.*
```

**NEW:**
```
**AC-11 — A2 focused lane.** The A2 spec's `-k` selection returns **0 failed, 0 skipped, 0 not-run**.
*Measured at `29c56fa`: **28 collected / 28 passed / 0 failed / 0 skipped / 0 not-run / 129
deselected**. All nine `-k` terms resolve to real test functions, so no selected assertion is
silently absent.*

***RESOLVED 2026-08-18.*** *The earlier note asserting "the A2 spec text records 30/30" was
incorrect. The A2 spec is byte-identical to its only commit `a232614`, records **no** test count, and
states its acceptance qualitatively ("zero failed, skipped, or not-run selected tests"). The `30/30`
figure was misattributed from two unrelated records — Story 6.7's checker/workflow suite and Story
6.2's EventStore dispatcher. **No discrepancy exists, and none blocks A2 closure.***
```

**Rationale:** Removes a false blocker. A2's acceptance criterion is met as written; the recorded
conflict was a cross-record transcription error, not a coverage gap.

---

### CP-3 — Tighten the F-10 fault-injection row

**Artifact:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18-e6-remediation-a3.md`
**Section:** §8 Fault injections (line 516)

**OLD:**
```
| F-10 | Dirty worktree during a current-proof run | Blocks visibly; must not attribute worktree bytes to the resolved commit |
```

**NEW:**
```
| F-10 | **Controlled** dirty worktree during a current-proof run (supplied by the test, never ambient) | `BLOCKED` with `E6_CURRENT_PROOF_WORKTREE_DIRTY`; must not attribute worktree bytes to the resolved commit; must execute on a clean tree too |
```

**Rationale:** F-10 was the only fault in §8 whose proof depended on the runner's environment.
Naming the dirt as test-supplied makes the fault injectable in CI, where it currently never runs.

---

### CP-4 — A2 spec: record-only closure note (applied by `bmad-build`, not here)

**Artifact:** `_bmad-output/implementation-artifacts/spec-e6-remediation-a2-restore-lifecycle-gates.md`
**Section:** `## Spec Change Log` (currently empty — **outside** the `frozen-after-approval` block)

**OLD:** *(empty)*

**NEW:**
```
- 2026-08-18 — Closure note (record only; no acceptance change). The A2 focused lane measures
  28 collected / 28 passed / 0 failed / 0 skipped / 0 not-run / 129 deselected at `29c56fa`, and all
  nine `-k` terms resolve to real tests. The "30/30" count attributed to this spec in
  sprint-change-proposal-2026-08-18-e6-remediation-a3.md AC-11 was never present here; it was
  misattributed from the Story 6.7 and Story 6.2 records. Resolved by
  sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md CP-2. The frozen intent, boundaries,
  I/O matrix, and acceptance criteria are unchanged.
```

**Rationale:** Preserves provenance for the phantom discrepancy at the artifact a future reader will
consult. The `frozen-after-approval` block is not touched. Applied by `bmad-build` during A2 closure
so the lifecycle gates run over it, not by this proposal.

---

## 5. Implementation handoff

**Scope classification: Moderate.** The A3 proposal is APPROVED, and "AC-11 count handling" is named
as one of its four release-owner-approved decisions — so amending it requires Jerome's re-approval
before Dev executes. No PM/Architect replan is required: no PRD, epic, architecture, or UX change.

### Sequence

| # | Task | Owner | Tool |
|---|---|---|---|
| 1 | Approve this proposal | Jerome (release owner) | — |
| 2 | ~~Apply CP-1, CP-2, CP-3 to the A3 proposal~~ — **APPLIED 2026-08-18** by `bmad-correct-course` at Jerome's direction, uncommitted | Dev workflow | *(done in-session)* |
| 3 | Make F-10 hermetic (below) and add the static guard test | Dev workflow | `bmad-build` |
| 4 | Rerun the full lane; confirm 0 skipped with F-10 **executed** | Dev workflow | `bmad-build` |
| 5 | Apply CP-4 and close A2 (`open` → `done`) through the lifecycle gates | Dev workflow | `bmad-build` |
| 6 | Complete remaining A3 work; close A3 | Architecture / Quality | `bmad-build` |
| 7 | IR-0 — **only after A1, A2, and A3 are all closed** | Independent | out of scope here |

### Task-3 contract for `bmad-build`

- **Target:** `_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py:477-484`.
- **Required:** F-10 must execute on every run. Supply the dirt under test control — e.g. monkeypatch
  `verifier.worktree_dirt` to return a synthetic tracked-path list, or drive `verifier.current_proof`
  against an isolated fixture repository — then assert `state == "BLOCKED"` and
  `code == "E6_CURRENT_PROOF_WORKTREE_DIRTY"`.
- **Also required:** a static guard test asserting `_bmad/scripts/tests` contains no `pytest.skip`,
  no `skipif`, and no test conditioned on `verifier.worktree_dirt(ROOT)`, so the class of defect
  cannot silently return.
- **Forbidden:** changing `verify_epic_6_completion_supersession.py:785-791`. The verifier is
  correct; only its proof is defective. Also forbidden: weakening any other assertion, touching
  `src/`, packages, submodules, gitlinks, or the FrontComposer drift.
- **Expected after the fix:** the full lane reports the same counts on a clean and a dirty tree, with
  F-10 among the passed.

### Success criteria

1. AC-10, AC-11, and the F-10 row read as amended above. **(Met — applied 2026-08-18, uncommitted.)**
2. `uv run --frozen python3 -m pytest -q _bmad/scripts/tests` → **0 failed, 0 skipped, 0 not-run**,
   with F-10 **executed**, and identical counts whether or not the tree is dirty.
3. The A2 focused lane still reports 28 passed / 0 skipped, and A2 closes through the gates.
4. `sprint-status.yaml` lifecycle values change only where a gate-verified closure authorizes it.
5. The FrontComposer gitlink is declared and **not** staged in any commit.

---

## 6. Explicit non-authorizations

This proposal does **not**:

- lift or weaken the global implementation hold (`ACTIVE`);
- authorize or rerun IR-0;
- close A2 or A3 (both remain `open` until `bmad-build` closes them through the lifecycle gates);
- start Story 7.1, any Epic 7–15 story, or `7.1-SCHEMAS`;
- authorize release, or create `implementation-hold-v1.json`;
- amend `v14-current-candidate-authority-v1.json`, V13, or any V1–V13 artifact;
- rewrite Story 6.2 or Story 6.7 records, or any published history;
- modify `sprint-status.yaml`, product code, packages, submodules, or gitlinks;
- capture or revert the concurrent `references/Hexalith.FrontComposer` drift.

---

## 7. Change navigation checklist record

| Item | Status | Note |
|---|---|---|
| 1.1 Triggering context | [x] Done | Readiness-gate FAIL at `29c56fa`; no story — checkpoint `E6-REMEDIATION` |
| 1.2 Core problem | [x] Done | Two categories: record error (AC-11) and too-weak acceptance criterion + test defect (AC-10) |
| 1.3 Evidence | [x] Done | E-1…E-9, §1.4 — all re-measured, not inherited from the gate report |
| 2.1 Current epic completable | [x] Done | Yes, unchanged |
| 2.2 Epic-level changes | [N/A] | None |
| 2.3 Remaining epics | [x] Done | Epics 7–15 unaffected; remain `backlog` behind IR-0 |
| 2.4 Epics invalidated / new epics | [N/A] | None |
| 2.5 Epic order / priority | [N/A] | Unchanged |
| 3.1 PRD conflicts | [N/A] | No requirement or MVP impact |
| 3.2 Architecture conflicts | [N/A] | No component, pattern, stack, schema, API, or integration change |
| 3.3 UI/UX conflicts | [N/A] | None |
| 3.4 Other artifacts | [!] Action-needed | A3 proposal §7/§8 (CP-1..CP-3); A2 spec change log (CP-4); test suite (task 3); CI pipeline unchanged |
| 4.1 Option 1 Direct Adjustment | [x] Viable | **Selected** — effort Low, risk Low |
| 4.2 Option 2 Rollback | [x] Not viable | Delivered A2/A3 work is correct; nothing to roll back |
| 4.3 Option 3 MVP review | [x] Not viable | No MVP dimension involved |
| 4.4 Recommended path | [x] Done | Option 1, hybrid: amend contract here, implement via `bmad-build` |
| 5.1–5.5 Proposal components | [x] Done | §1–§5 |
| 6.1 Checklist completion | [x] Done | This table |
| 6.2 Proposal accuracy | [x] Done | Every count and claim re-measured at `29c56fa` |
| 6.3 User approval | [x] Done | Approved by Jerome 2026-08-18; recorded in the header |
| 6.4 `sprint-status.yaml` update | [N/A] | No epic/story added, removed, or renumbered — zero edits |
| 6.5 Next steps / handoff | [x] Done | §5 |
