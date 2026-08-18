# Sprint Change Proposal — E6-REMEDIATION A3 (additive)

- **Date:** 2026-08-18
- **Author:** Dev workflow (correct-course), for Jerome
- **Checkpoint:** `E6-REMEDIATION` action `A3` — *Architecture / Quality*
- **Scope classification:** **Moderate** (backlog + authority reorganization; no PRD/MVP change)
- **Implementation hold:** **ACTIVE — unchanged by this proposal**
- **Authorizes:** nothing beyond A3 itself. Not IR-0, not Story 7.1, not release, not hold lift.
- **Baseline tree:** `a232614e948e8b12d522f7441562116d598d948c` (= `origin/main`)
- **Frozen predecessor candidate:** `08d38fc021c8c76115f1192d2971c980f7e86ea9`
- **Status:** **APPROVED** by Jerome (release owner) on 2026-08-18, at baseline `a232614`,
  working tree clean apart from this proposal. Approval covers the plan below and its four
  recorded decisions (§4.2 ledger relocation, §4.4 V14 shape, §4.7 declaring-type scope,
  AC-11 count handling). **Approval does not lift the hold or authorize anything in §13.**

---

## 1. Issue summary

A2 restored the V12 lifecycle gate blocks across all twelve mirrored review/done routes, and its
focused lane is green. A2 nevertheless cannot close, because the evidence gate that A2 itself
requires fails before it can be consulted:

```
EVIDENCE_CONTEXT_WORKFLOW_INVALID: bmad-build/compile-epic-context.md
```

The failure is not in A2's work. BMAD 6.11 synchronization commit `4ba45a7` deleted the
authority-identity and historical-context safeguards from the four logical context-workflow files
(eight files across `.agents` and `.claude`). `verify_evidence_boundary.py:264-300` requires exactly
those safeguards, so the evidence gate fails closed on the first one it inspects.

The same commit also created a second, independent blockage. The frozen planning candidate
`08d38fc0` no longer matches **12 of the 92 protected paths**, so `publish_v9_planning_authority.py`
fails closed with `CANDIDATE_SOURCE_DRIFT`; nine of the ten current Python failures are that single
fault reported nine times.

Those 12 paths split into two groups requiring *opposite* treatment:

| Group | Files | Drift content | Correct treatment |
|---|---|---|---|
| Context-workflow | 8 | Safeguard deletion only. **Zero `uv run` occurrences at either the candidate or HEAD.** | Exact byte restoration to the candidate blob |
| Lifecycle | 4 | **Exactly six `python3` → `uv run` lines** and nothing else | Must be kept; requires a **new** candidate |

This is the crux: the eight context files must go *back* to the candidate, and the four lifecycle
files can never go back to it. Rebinding or rewriting the old candidate would therefore either
re-break the safeguards or revert intended BMAD 6.11 behavior. A **new committed candidate plus a
versioned additive successor authority** is the only treatment satisfying both.

### 1.1 State change during this analysis — read first

The tree moved mid-analysis. Commit **`a232614`** ("Update submodule references and add remediation
spec for E6 lifecycle gates", Jerome, 2026-08-18 02:28) was authored **and pushed**; it is now
`origin/main`. It swept into a single commit:

- the twelve A2 lifecycle route files;
- the Epic 6 retrospective reconciliation;
- the A2 spec (new file);
- the `sprint-status.yaml` 91-comment deletion;
- **five** submodule gitlinks — EventStore, FrontComposer, Memories, Parties **and Tenants**.

**The A3 diagnosis is unchanged by this.** Re-measured at the new HEAD: still 12 of 92 protected
paths drifting, still `215 passed / 10 failed`, still the same ten failures. What changed is the
*baseline* and the *working-tree assumptions* in the original task framing:

| Task framing said | Actual state now |
|---|---|
| Baseline `1a7c08a` | Baseline `a232614` |
| Four dirty submodule worktrees to preserve | All committed; working tree **clean** except this proposal |
| sprint-status deletion is a protected working-tree edit | Now **committed** — it is baseline state, not a pending edit |
| A2 route restoration uncommitted | Committed |

Nothing is lost — the deletion and the route restoration both survive in history. But §4.2 and §6
below are written against the *actual* state, not the framing.

### 1.2 Two new findings this commit exposed — routed into A3

**N-1 — The commit-message contract has no enforcement layer in this repository.**
`a232614`'s subject fails the repo's own pinned commitlint with three errors (`type-empty`,
`subject-empty`, `header-format`). It was committed and pushed unimpeded. Verified cause:

| Enforcement layer | Baseline claims | Actual |
|---|---|---|
| commit-msg hook | "installed commit-message hook" | `.git/hooks/` contains **only samples**; `core.hooksPath` unset; no `.husky/` |
| CI commitlint gate | "blocking CI commitlint gate" | The only workflow is `planning-authority-preflight.yml`; no commitlint job anywhere |
| commitlint CLI | pinned | Present (`@commitlint/cli` 21.2.1) but `package.json` has **no scripts** and no wiring |

So the CLI is pinned and *nothing invokes it*. This is squarely A3's charter — "add mutation tests
plus an automatic preflight" — and it is why a planning-authority-bearing commit reached `origin/main`
with a non-conforming subject.

**N-2 — A fifth, undeclared gitlink moved.** The task framing declared four dirty submodules;
`a232614` carries five, adding `references/Hexalith.Tenants` (`5a2b90d` → `cff62ce`). An
undeclared gitlink movement inside a commit that also carries planning artifacts is precisely the
defect class the Epic 6 retrospective is about — Story 6.2's done commit moved seven gitlinks with no
candidate-bound evidence.

**Neither finding is rewritten away.** `a232614` is published history (`origin/main`); this proposal
does **not** amend, rebase or force-push it. Both are recorded as findings and fixed forward.

### 1.3 Evidence

| Claim | Verification |
|---|---|
| 12 of 92 protected paths drift | Enumerated `CANONICAL_PATHS + PROTECTED_CANDIDATE_PATHS` at `a232614`: 92 total, 0 missing, 12 drifting |
| The 8 context files contain no `uv run` | `grep -c "uv run"` = 0 at both `08d38fc0` and HEAD, all 8 files |
| The 4 lifecycle files drift only by `uv run` | Candidate blob vs current: 6 changed lines, all `python3 …resolve_customization.py` → `uv run …` |
| Restoring the 8 candidate blobs satisfies the verifier | All 4 logical paths: `.agents`/`.claude` parity `True`, base tokens `True`, extra tokens `True` |
| Full Python lane | `uv run --frozen python3 -m pytest -q _bmad/scripts/tests` → **215 passed / 10 failed** |
| 9 of 10 failures are one fault | All nine `test_publish_v9_planning_authority.py` failures fail-fast on `CANDIDATE_SOURCE_DRIFT: .agents/skills/bmad-build/compile-epic-context.md` |
| 10th failure | `test_verify_evidence_boundary.py::test_context_workflows_are_exact_mirrors_and_fail_closed` |
| sprint-status edit is value-neutral | 91 deleted lines, **100% comments**; 0 added lines; 0 non-comment deletions |
| Parity holds today | All 20 controlled files (10 logical paths × 2 trees) are pairwise byte-identical |
| N-1 | `git log -1 --format=%s a232614 \| npx commitlint` → 3 problems, exit 1 |

---

## 2. Impact analysis

### Epic impact

**None.** No epic is added, removed, resequenced or rescoped. A3 is an already-declared action in
`v12-pre-ir0-remediation-authority-v1.json:32-39` under checkpoint-owned authority `E6-REMEDIATION`.
Epic 6 remains `done` with verdict `rejected`; Epics 7–15 remain `backlog`.

### Story impact

**None.** No story record is created, edited, transitioned or completed. Story 7.1 remains
non-executable planning-only. The A2 spec stays `in-progress` and resumes review after A3.

### Artifact conflicts

| Artifact | Conflict | Action |
|---|---|---|
| PRD | None | No change |
| `epics.md` | None — A3 already authorized | No change |
| `architecture.md` | None — A4–A6 preserved as open | No change |
| UX specification / UX map | None | UX map regenerates byte-identically |
| V1–V13 authority, evidence, decisions | None if V13's publisher is pinned | Byte-frozen (§4.4) |
| `sprint-status.yaml` | Hold assertion red; provenance deleted | Resolved additively (§4.2) |
| 8 context-workflow files | Safeguards deleted | Exact restore (§4.1) |
| 4 lifecycle files | Legitimate 6.11 drift vs frozen candidate | New candidate (§4.3) |
| Preflight workflow | Never consumes V13 evidence or decision chain; no commit-message gate | Wired (§4.5, §4.9) |
| Conformance validator | Declaring-type guard is vacuous | Fixed (§4.7) |

### Technical impact

No product code under `src/`. No packages. No submodules. No gitlinks. The surface is planning
authority, workflow route text, verification tooling, and CI preflight.

---

## 3. Recommended approach

**Option 1 — Direct Adjustment. Viable. Selected.** Effort **Medium**, risk **Low-Medium**.

**Option 2 — Rollback: not viable.** Reverting `4ba45a7` would restore the eight safeguards but also
revert the `uv run` migration across the whole BMAD 6.11 surface, undoing intended behavior and
re-breaking the four lifecycle routes A2 just repaired. Reverting `a232614` is separately excluded:
it is published history.

**Option 3 — MVP review: not applicable.** No requirement, goal or MVP boundary is implicated.

### Selected shape

1. **Restore, don't revert.** Byte-restore the eight context files from the frozen candidate. Proven
   lossless: those files carry no 6.11 change to preserve.
2. **Supersede, don't rewrite.** Freeze V13 by pinning its publisher to its own recorded candidate,
   and add a **V14** current-candidate authority for the new candidate.
3. **Relocate, don't delete.** Move the 90 provenance comments into a committed ledger.
4. **Bind, don't assume.** Make the preflight actually consume V13 evidence and the decision chain —
   and actually invoke the commitlint CLI it already pins.

---

## 4. Detailed change proposals

### 4.1 Restore the eight context-workflow safeguards

**Method:** exact byte restoration from `08d38fc0` — not hand-editing.

```bash
for f in bmad-build/compile-epic-context.md bmad-build/step-01-clarify-and-route.md \
         bmad-build-auto/compile-epic-context.md bmad-build-auto/step-01-clarify-and-route.md; do
  git checkout 08d38fc021c8c76115f1192d2971c980f7e86ea9 -- ".agents/skills/$f" ".claude/skills/$f"
done
```

Restores, per `verify_evidence_boundary.py:279-298`: the `overlay_version` / `architecture_version`
frontmatter contract; the *Historical Epic 6 v8 exception* with its `### 6.1 ` through `### 6.12 `
requirement and `write nothing` blocked-report rule; and step-01's heading-only rejection,
historical-authority preservation and `filesystem mtime alone` clause.

**Preserves 6.11 because there is nothing to preserve in these files** — measured `uv run` count is
0 at both revisions. **Parity holds** because the candidate blobs are already pairwise byte-identical.

### 4.2 Resolve the 91 sprint-status provenance comments

The deletion is now **committed** in `a232614`, so this is a forward change from baseline, not an
edit to a pending working-tree change. Two mechanical facts constrain it:

1. `publish_v9_planning_authority.py:1497-1507` strips `^# V(10|11|12) PLANNING PUBLICATION:` and
   unconditionally re-adds the V12 notice. Line 1 of the 91 **is** that notice, so it returns in every
   option. `render_sprint` is idempotent, so the candidate must contain the rendered fixed point.
2. That notice carries the two strings `PlanningAuthorityV8ValidationTest.cs:257-258` asserts —
   `GLOBAL IMPLEMENTATION HOLD remains ACTIVE` and `IR-0 was not run`. Its absence is why that test is red.

**Resolution (approved):**

| Element | Disposition |
|---|---|
| Line 1 of 91 (V12 notice) | Returns mechanically via `render_sprint`. Re-greens the V8 hold assertion. |
| Lines 2–91 (90 provenance lines) | Relocated **byte-exact** into a new committed ledger |
| New ledger | `_bmad-output/implementation-artifacts/sprint-status-provenance-v1.md` |
| Pointer | One `# PROVENANCE LEDGER: …` comment placed **after** the V12 notice |
| Pin | Ledger `sha256` recorded in the V14 authority |
| Status / action values | **UNCHANGED — 0 non-comment lines touched** |

Recover the 90 lines from history, not by retyping:

```bash
git show 1a7c08a:_bmad-output/implementation-artifacts/sprint-status.yaml > /tmp/prev.yaml
diff /tmp/prev.yaml _bmad-output/implementation-artifacts/sprint-status.yaml  # the 91 deleted lines
```

The pointer survives republication: `render_sprint` strips only lines matching
`^# V(10|11|12) PLANNING PUBLICATION:`, so a `# PROVENANCE LEDGER:` line carries through unmodified,
keeping the fixed point stable.

`validate_managed_namespace` (`:1758-1791`) polices only `planning-artifacts/v9/**` and the
`v9-*.json` / `v11-*.json` / `v12-*.json` globs. A file under `implementation-artifacts/` is outside
that namespace and cannot raise `PUBLICATION_SCOPE_DRIFT`.

**Net effect vs baseline:** `+2` comment lines (machine notice + pointer), `0` value changes. Landing
at `−91` is not achievable; §4.2 states why rather than absorbing it silently.

### 4.3 New committed planning candidate

Required solely because the four lifecycle files must keep their six `uv run` lines. The established
two-phase pattern is followed exactly — verified against `08d38fc0` ("test(planning): fix
retrospective status-fault mutation syntax") and its rebind commit `55e3fd0` ("docs(planning): rebind
V12 candidate after retrospective test fix"):

- **Commit N — inputs.** Everything in §4.1, §4.2, §4.4–§4.9 plus this proposal. Becomes the new candidate.
- **Publish.** `publish_v9_planning_authority.py --repository . --candidate <N>` regenerates all 42
  managed companions atomically via `replace_managed_set` (`:1794+`), which stages the complete set
  and rolls back byte-identically on any failure.
- **Commit N+1 — companions.** The regenerated set.

Add this proposal to `CANONICAL_PATHS`, matching how the 2026-08-02/03/04 proposals are candidate-bound.

**Validated commit subjects** (pinned `@commitlint/cli` 21.2.1, `--config commitlint.config.mjs`, both PASS):

```
fix(planning): restore context safeguards and rebind A3 candidate
docs(planning): rebind V14 candidate after A3 remediation
```

### 4.4 V14 successor authority; V13 byte-frozen

`publish_v13_current_proof_authority.py:63-66` reads `bundle["planningCandidate"]` and `:92` writes it
into the V13 sidecar. Rebinding the bundle would therefore rewrite V13 — which requirement 1 forbids.

| Artifact | Disposition |
|---|---|
| `v13-current-proof-authority-v1.json` | **BYTE-FROZEN** |
| `publish_v13_current_proof_authority.py` | Candidate pinned to the literal `08d38fc0`; stops following the live bundle |
| `v9-authority-bundle-v1.json` + 42 companions | Rebound to candidate N — the established operation (V10, V11, V12 each rebound it; it is a managed output, not historical evidence) |
| `v14-current-candidate-authority-v1.json` | **NEW.** Binds candidate N, records V13 as point-in-time predecessor, pins the provenance-ledger sha256, keeps `implementationHold: ACTIVE` |
| `v14-current-candidate-authority-v1.schema.json` | **NEW** |
| `publish_v14_current_candidate_authority.py` | **NEW** |

V14 must carry `completionEffect` `{ir0RerunAllowed: false, holdLifted: false, successorStarted:
false, releaseAuthorized: false}` and inherit every V13 prohibition.

This *implements* the retrospective's own lesson — "a proof named current must be re-evaluated at the
candidate that consumes it" — by making V13 explicitly point-in-time and V14 the successor.

### 4.5 V13 evidence and decision-chain preflight consumption

Retro finding: the V13 validator "hardcodes A1 to `done` and checks equality with its own rendering
without consuming the current-proof evidence or independent decision" (`publish_v13_*.py:74-150`;
confirmed at `:111` — `"status": "done"` is a literal). The preflight
(`planning-authority-preflight.yml:48-68`) runs the evidence boundary, pytest, the V9 publisher check
and the C# validators — and **never** runs the V13 current-proof command or any decision-chain validator.

- **NEW** `_bmad/scripts/verify_decision_chain.py` — resolves authority → evidence → decision, requiring
  the decision's bound evidence HEAD to equal the evidence artifact's recorded HEAD, that the decision
  neither lifts the hold nor authorizes IR-0/release, and that A1's status is *derived* from the
  validated decision rather than asserted.
- **CHANGE** `publish_v13_current_proof_authority.py` — derive A1 status from the decision binding;
  fail closed with a stable code when evidence or decision is unavailable. Rendered bytes stay identical.
- **CHANGE** preflight — add a required step invoking the decision-chain validator and the V13/V14
  authority checks, before the C# validator job.

### 4.6 Current-proof evidence integrity (retro findings R2, R4, R5, R6)

| # | Finding | Change |
|---|---|---|
| R2 | Resolves a committed HEAD but loads contract/schema and runs tests from the mutable worktree (`verify_epic_6_completion_supersession.py:540-546,609-666`); no dirty-worktree fault exists | Materialize exact-HEAD bytes, or block visibly on applicable worktree dirt; add the missing fault |
| R4 | Post-done changed paths are an endpoint diff whose test oracle calls the same helper (`:595-598,638-646`) | Independent raw-Git fixture; omission and unexpected-path faults; or restrict wording to endpoint differences |
| R5 | Checks only the ten paths declared by a mutable contract; never derives the root mode-`160000` inventory from `.gitmodules`/HEAD nor proves each recorded object exists locally | Exact inventory equality plus per-object availability |
| R6 | Skip parser misses pytest's count-first `1 skipped` form (`:270-299`); freezes no test identities or count | Fix the parser both ways; freeze identities and count |

R1 (V13 is point-in-time, not evergreen) is discharged structurally by §4.4.

### 4.7 Declaring-type validation gap (retro finding R7)

`ArchitecturePlanningAuthorityValidationTest.cs:1223-1231` collects **every** type declaration in the
file and passes if *any* one has `public` in its modifiers. The matched method's position is never
used, so a public method inside an `internal` type passes whenever any unrelated public type exists
anywhere in the same file.

- **CHANGE** resolve the matched method's offset to its enclosing brace-balanced type span and assert
  *that* type is public. Handle nested types and file-scoped namespaces.
- **NEW** deferred-work entry for the compiled cross-assembly consumer fixture as an approved
  successor — recorded, not executed under the hold.

### 4.8 Retrospective and A2 spec

Both already carry the reconciliation and are committed. A3 appends only its own completion line to
the A3 row's evidence when A3 closes, plus N-1/N-2 as reconciliation findings. **No verdict, status,
or action-item value changes.**

### 4.9 Commit-message enforcement (new finding N-1)

The repository pins `@commitlint/cli` 21.2.1 and `commitlint.config.mjs`, and **nothing invokes them**.

- **NEW** `.github/workflows/commitlint.yml` — validate every commit in a push/PR range with the
  pinned CLI; blocking.
- **NEW** `package.json` scripts entry wiring the CLI, plus a `commit-msg` hook (`.husky/commit-msg`
  or a tracked `core.hooksPath` directory) so local commits are checked before they exist.
- **CHANGE** `planning-authority-preflight.yml` — for any commit touching planning authority, require
  the commit-message check to have passed.
- **RECORD** `a232614`'s non-conforming subject in the retrospective reconciliation as the observed
  instance. **Do not amend, rebase or force-push it** — it is published history on `origin/main`.

---

## 5. Changed-path inventory

### Restored to candidate bytes (8)

```
.agents/skills/bmad-build/compile-epic-context.md
.agents/skills/bmad-build/step-01-clarify-and-route.md
.agents/skills/bmad-build-auto/compile-epic-context.md
.agents/skills/bmad-build-auto/step-01-clarify-and-route.md
.claude/skills/bmad-build/compile-epic-context.md
.claude/skills/bmad-build/step-01-clarify-and-route.md
.claude/skills/bmad-build-auto/compile-epic-context.md
.claude/skills/bmad-build-auto/step-01-clarify-and-route.md
```

### Already committed at baseline — A3 modifies none of them (12)

The twelve mirrored lifecycle routes, as `a232614` committed them.

### New files (8)

```
_bmad-output/implementation-artifacts/sprint-status-provenance-v1.md
_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json
_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-18-e6-remediation-a3.md
_bmad/schemas/v14-current-candidate-authority-v1.schema.json
_bmad/scripts/publish_v14_current_candidate_authority.py
_bmad/scripts/verify_decision_chain.py
.github/workflows/commitlint.yml
.husky/commit-msg                                    # or tracked core.hooksPath equivalent
```

### Modified (9 + tests)

```
_bmad-output/implementation-artifacts/sprint-status.yaml   # +notice +pointer; 0 value changes
_bmad-output/implementation-artifacts/epic-6-retro-2026-08-03.md  # record N-1, N-2
_bmad-output/implementation-artifacts/deferred-work.md     # compiled-fixture successor entry
_bmad/scripts/publish_v9_planning_authority.py             # + proposal in CANONICAL_PATHS
_bmad/scripts/publish_v13_current_proof_authority.py       # pin 08d38fc0; derive A1 status
_bmad/scripts/verify_epic_6_completion_supersession.py     # R2, R4, R5, R6
tests/…/ArchitecturePlanningAuthorityValidationTest.cs     # R7 declaring-type
.github/workflows/planning-authority-preflight.yml         # decision-chain + V13/V14 + commit-msg
package.json                                               # commitlint wiring
_bmad/scripts/tests/test_*.py                              # fault injections (§8)
```

### Regenerated atomically (42)

`v9-authority-bundle-v1.json`, `v9-execution-graph-v1.json`, `v9-supersession-map-v1.json`,
`v11-story-7.1-schema-slice-v1.json`, `v12-pre-ir0-remediation-authority-v1.json`,
`epic-6-current-execution-view-v2.md`, `ux-requirement-map.md`, `sprint-status.yaml`,
4 inventories, 3 resolved-customization files, 27 story contracts.

### Byte-frozen — must not change (asserted in verification)

```
v13-current-proof-authority-v1.json
epic-6-completion-supersession-contract-v1.json
epic-6-completion-supersession-decision-v1.json
epic-6-completion-supersession-current-proof-contract-v1.json
epic-6-completion-supersession-current-proof-decision-v1.json
epic-6-current-execution-view-v1.md
epics.md, architecture.md
all completed story records (6-1, 6-2, 6-7, and every Epic 1–5 record)
commit a232614 and all published history
```

---

## 6. Candidate-commit boundary

**Included:** only the paths in §5.

**Excluded — absolutely: every gitlink and all submodule content.**

```
references/Hexalith.AI.Tools     references/Hexalith.Builds      references/Hexalith.Commons
references/Hexalith.EventStore   references/Hexalith.Folders     references/Hexalith.FrontComposer
references/Hexalith.Memories     references/Hexalith.Parties     references/Hexalith.Projects
references/Hexalith.Tenants
```

The four (now five) previously-dirty submodules are already committed at baseline; A3 must leave all
ten gitlinks at their `a232614` values. No submodule content, no gitlink, no `.gitmodules` change
enters either commit.

**Enforcement:**

- Stage by **explicit pathspec only**. Never `git add -A`, never `git commit -a` — this is exactly how
  `a232614` acquired an undeclared fifth gitlink (N-2).
- No `git submodule update`; no recursive or `--remote` operation; no nested submodule initialization.
- Declare `submodule_promotions: []` — nothing is promoted.
- **Gate:** the candidate must contain **zero** mode-`160000` entries:

```bash
git diff --raw --no-abbrev a232614e948e8b12d522f7441562116d598d948c <candidate> -- \
  | awk '$1 ~ /160000/ || $2 ~ /160000/' | wc -l   # expected: 0
```

This keeps `validate_gitlinks` (`verify_evidence_boundary.py:324-339`) trivially satisfied: with no
gitlink entries in the range, no gitlink evidence is asserted and none is required.

**Known hazard, now observed twice.** A concurrent session may commit shared files mid-run — it
happened during this analysis. Re-read `git rev-parse HEAD` and `sprint-status.yaml` immediately
before staging; never trust a status snapshot taken earlier in the session.

---

## 7. Acceptance criteria

**AC-1 — Context safeguards.** All eight files are byte-identical to their `08d38fc0` blobs; each
`.agents`/`.claude` pair is byte-identical; `validate_context_workflows` returns `PASS` for all four
logical paths with a nonempty ledger; `EVIDENCE_CONTEXT_WORKFLOW_INVALID` no longer occurs.

**AC-2 — 6.11 preserved.** The six `uv run` lines in the four lifecycle files are intact; no `uv run`
occurrence reverts to `python3`; the twelve lifecycle routes are byte-identical to `a232614`.

**AC-3 — Candidate binding.** `publish_v9_planning_authority.py --repository . --check` exits `0`.
All 92 protected paths match the new candidate. `CANDIDATE_SOURCE_DRIFT` is absent.

**AC-4 — V1–V13 unchanged.** Every artifact in §5's byte-frozen list has an unchanged sha256 measured
against `a232614`. `publish_v13_current_proof_authority.py --check` passes **with the pin**, and
V13's bytes are provably unmodified.

**AC-5 — V14 additive.** The V14 authority validates against its schema, binds candidate N, pins the
ledger sha256, records V13 as point-in-time predecessor, and asserts `implementationHold: ACTIVE`
with all four `completionEffect` flags false.

**AC-6 — Sprint status.** Exactly 0 non-comment lines differ from `a232614`; all lifecycle and
action-item values are identical; the V12 notice and pointer are present; the ledger contains the 90
deleted lines byte-exact and matches its pinned sha256.

**AC-7 — Decision chain.** The preflight runs the decision-chain validator; A1's status is derived
from the validated decision, not a literal; a decision whose bound evidence HEAD differs from the
evidence artifact's recorded HEAD fails closed.

**AC-8 — Evidence integrity.** R2, R4, R5 and R6 each have a passing positive test and a red fault
injection (§8).

**AC-9 — Declaring type.** A public method inside an `internal` type adjacent to an unrelated public
type **fails**; the current code passes this case, so the fault must be shown red before the fix.

**AC-10 — Full lane.** `uv run --frozen python3 -m pytest -q _bmad/scripts/tests` reports **0 failed,
0 skipped, 0 not-run**, with passed ≥ 225 (215 current + the 10 repaired) plus new tests.

**AC-11 — A2 focused lane.** The A2 spec's `-k` selection returns **0 failed, 0 skipped, 0 not-run**.
*Measured on rerun at this tree: **28 passed, 129 deselected**. The A2 spec text records 30/30; the
delta is surfaced here rather than silently reconciled, and must be resolved before A2 closes.*

**AC-12 — Boundary.** Zero mode-`160000` entries in either commit; all ten gitlinks remain at their
`a232614` values; no product code under `src/` is modified.

**AC-13 — Commit messages.** Both A3 commit subjects pass the pinned commitlint; the new gate
rejects a deliberately malformed subject; `a232614` is recorded, not rewritten.

**AC-14 — Hold.** The hold remains `ACTIVE` in every authority artifact. No IR-0 rerun, no Story 7.1
execution, no release authorization, no hold lift. `implementation-hold-v1.json` is **not** created.

---

## 8. Fault injections

Each must turn **red** with its stable code, then restore its fixture **byte-identically**.

| # | Fault | Expected |
|---|---|---|
| F-1 | Delete `overlay_version` from one context file | `EVIDENCE_CONTEXT_WORKFLOW_INVALID` |
| F-2 | Delete the *Historical Epic 6 v8 exception* heading | `EVIDENCE_CONTEXT_WORKFLOW_INVALID` |
| F-3 | Delete step-01's `filesystem mtime alone` clause | `EVIDENCE_CONTEXT_WORKFLOW_INVALID` |
| F-4 | Change one byte in `.claude` only | `EVIDENCE_WORKFLOW_PARITY_DRIFT` |
| F-5 | Revert one `uv run` line to `python3` | Candidate drift on that path |
| F-6 | Point V13's publisher back at the live bundle | Must fail — proves the pin cannot silently follow a rebind |
| F-7 | Mutate one byte of the V13 authority | V13 immutability assertion fails |
| F-8 | Decision bound to an evidence HEAD ≠ the evidence artifact's | Decision-chain validator fails closed |
| F-9 | Force A1 to `done` with no valid decision | Derived-status check fails |
| F-10 | Dirty worktree during a current-proof run | Blocks visibly; must not attribute worktree bytes to the resolved commit |
| F-11 | Omit one changed path from the post-done set | Independent raw-Git oracle fails |
| F-12 | Inject an unexpected path | Independent raw-Git oracle fails |
| F-13 | Remove one root gitlink from `.gitmodules` | Inventory equality fails |
| F-14 | Record a gitlink object absent from the local submodule | Object-availability check fails |
| F-15 | Emit `1 skipped` (count-first) | Skip parser detects it |
| F-16 | Deselect a frozen test identity | Frozen-identity check fails |
| F-17 | `internal class X { public void M() }` + unrelated `public class Y {}` | **Must fail** (passes today) |
| F-18 | Delete one provenance line from the ledger | Pinned sha256 fails |
| F-19 | Change one action-item status in sprint-status | Status-preservation assertion fails |
| F-20 | Add a gitlink to the candidate commit | Boundary gate reports non-zero |
| F-21 | Malformed commit subject (e.g. `a232614`'s) | New commit-message gate rejects it |

---

## 9. Verification commands

```bash
BASE=a232614e948e8b12d522f7441562116d598d948c
CAND=08d38fc021c8c76115f1192d2971c980f7e86ea9

# 0 — confirm HEAD has not moved under you (concurrent-session hazard)
git rev-parse HEAD

# 1 — context safeguards restored byte-exactly (expect: no output)
for f in bmad-build/compile-epic-context.md bmad-build/step-01-clarify-and-route.md \
         bmad-build-auto/compile-epic-context.md bmad-build-auto/step-01-clarify-and-route.md; do
  for t in .agents .claude; do
    diff <(git show "$CAND:$t/skills/$f") "$t/skills/$f" >/dev/null || echo "DRIFT $t/$f"
  done
done

# 2 — parity across the 10 controlled logical paths / 20 files (expect: no output)
for f in bmad-build/step-04-review.md bmad-build/step-05-present.md bmad-build/step-oneshot.md \
         bmad-build-auto/step-04-review.md bmad-dev-story/SKILL.md \
         bmad-code-review/steps/step-04-present.md bmad-build/compile-epic-context.md \
         bmad-build/step-01-clarify-and-route.md bmad-build-auto/compile-epic-context.md \
         bmad-build-auto/step-01-clarify-and-route.md; do
  diff -q ".agents/skills/$f" ".claude/skills/$f" >/dev/null || echo "PARITY DRIFT $f"
done

# 3 — 6.11 preserved (expect: 6)
git grep -c "uv run {project-root}/_bmad/scripts/resolve_customization.py" -- \
  '.agents/skills/bmad-dev-story/SKILL.md' '.claude/skills/bmad-dev-story/SKILL.md' \
  '.agents/skills/bmad-code-review/steps/step-04-present.md' \
  '.claude/skills/bmad-code-review/steps/step-04-present.md' | awk -F: '{s+=$2} END {print s+0}'

# 4 — evidence gate (expect: PASS or not-applicable, nonempty ledger)
uv run --frozen python3 _bmad/scripts/verify_evidence_boundary.py \
  --repository . --baseline "$BASE" --candidate HEAD

# 5 — candidate binding (expect: exit 0)
uv run --frozen python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check

# 6 — V13 frozen + V14 additive + decision chain
uv run --frozen python3 _bmad/scripts/publish_v13_current_proof_authority.py --check
uv run --frozen python3 _bmad/scripts/publish_v14_current_candidate_authority.py --check
uv run --frozen python3 _bmad/scripts/verify_decision_chain.py --repository .
git diff --quiet "$BASE" -- \
  _bmad-output/planning-artifacts/v13-current-proof-authority-v1.json && echo "V13 BYTE-FROZEN"

# 7 — sprint-status: zero non-comment change (expect: 0)
git diff -U0 "$BASE" -- _bmad-output/implementation-artifacts/sprint-status.yaml \
  | grep -E '^[+-]' | grep -vE '^[+-][+-]' | sed -E 's/^[+-]//' \
  | grep -vE '^\s*#' | grep -vE '^\s*$' | wc -l

# 8 — full Python lane (expect: 0 failed, 0 skipped)
uv run --frozen python3 -m pytest -q --tb=short _bmad/scripts/tests

# 9 — A2 focused lane (expect: 0 failed, 0 skipped; see AC-11 on the count)
uv run --frozen python3 -m pytest -q --tb=short \
  _bmad/scripts/tests/test_verify_evidence_boundary.py \
  _bmad/scripts/tests/test_verify_submodule_promotion.py \
  _bmad/scripts/tests/test_generate_story_record.py \
  -k 'active_route_inventory or route_gate_faults or displaced_gate_and_cross_tree_parity or completion_workflows_gate or workflow_contract_check or workflow_contract_rejects_enforcement_clause_outside_gate or both_skill_trees_stay_byte_identical or current_route_inventory or v12_gate_span'

# 10 — C# validators
dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj \
  --configuration Release -m:1
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj \
  --configuration Release --no-build \
  --filter "FullyQualifiedName~ArchitecturePlanningAuthorityValidationTest|FullyQualifiedName~PlanningAuthorityV9ValidationTest|FullyQualifiedName~PlanningAuthorityV8ValidationTest"

# 11 — boundary: zero gitlink entries in the candidate range (expect: 0)
git diff --raw --no-abbrev "$BASE" HEAD -- \
  | awk '$1 ~ /160000/ || $2 ~ /160000/' | wc -l

# 12 — all ten gitlinks unchanged vs baseline (expect: no output)
git diff "$BASE" HEAD -- references/ --stat

# 13 — commit messages (expect: both pass)
git log --format=%s "$BASE"..HEAD | while read -r s; do
  printf '%s\n' "$s" | npx --no-install commitlint --config commitlint.config.mjs || echo "REJECTED: $s"
done

# 14 — whitespace hygiene
git diff --check
```

> Per the recorded xUnit v3 constraint, use `-trx <file>` on compiled test executables when a
> machine-readable result artifact is needed. `--report-trx` is rejected. Counts must never be hand-typed.

---

## 10. Rollback boundary

| Layer | Rollback |
|---|---|
| Companion regeneration | `replace_managed_set` stages the full set and restores every prior byte on any failure — atomic by construction |
| Commit N+1 | `git revert` — restores the previous companion set; leaves inputs intact |
| Commit N | `git revert` — returns to `a232614`; forward-only, never a rewrite |
| Context restoration | Independently revertible via `git checkout a232614 -- <path>` |
| Provenance ledger | Delete the file, drop the pointer line; sprint-status values never moved, so nothing to unwind |
| V13 | Nothing to roll back — byte-frozen throughout |
| V14 | Delete authority + schema + publisher; V9–V13 unaffected |
| Commit-message gate | Remove the workflow/hook; no artifact depends on it |
| **Never rolled back** | Any submodule worktree, gitlink, completed story record, V1–V13 artifact, or published commit — none is written |

**Hard boundary:** rollback is **forward-only** (`git revert`). It never rewrites published history —
no amend, no rebase, no force-push — never touches `references/**`, and never alters the hold.

---

## 11. Sequence

1. **Complete A3** — §4.1 → §4.2 → §4.4–§4.9 → commit N → publish → commit N+1.
2. **Rerun evidence gates** — commands 4, 5, 6, 8, 10, 11, 13. All must be green.
3. **Resume A2 review** — with the evidence gate passing, A2's review can finally execute; reconcile
   the AC-11 count delta; only then may A2 transition.

**Ordering is mandatory.** A2 cannot close before A3 because A2's own gate depends on A3's repair.

---

## 12. Handoff

| Work | Owner |
|---|---|
| §4.1, §4.2, §4.3 — restoration, ledger, candidate | Dev workflow |
| §4.4, §4.5 — V14, V13 pin, decision chain | Architecture / Quality |
| §4.6, §4.7 — evidence integrity, declaring type | Architecture / Quality |
| §4.9 — commit-message enforcement | Dev workflow / Build owner |
| §9 verification, A2 review resume | Dev workflow |
| Approval of this proposal | Jerome (release owner) |

**Success criteria:** AC-1 through AC-14 all green, all 21 fault injections red-then-restored, hold
still `ACTIVE`.

---

## 13. Explicit non-authorizations

This proposal does **not**, and no part of its execution may be read to:

- lift or weaken the global implementation hold;
- authorize, run, or rerun IR-0;
- start, unblock, or execute Story 7.1 or any successor story;
- authorize release or claim release approval;
- create `implementation-hold-v1.json`;
- change any lifecycle or action-item status;
- alter the Epic 6 retrospective verdict (`rejected`);
- rewrite completed story records, V1–V13 evidence, or any published commit including `a232614`;
- modify product code, packages, submodules, or gitlinks;
- close A1, A2, A4, A5, or A6.

A3 closing makes `E6-REMEDIATION` eligible for its **next** step only. The hold stays `ACTIVE`.
