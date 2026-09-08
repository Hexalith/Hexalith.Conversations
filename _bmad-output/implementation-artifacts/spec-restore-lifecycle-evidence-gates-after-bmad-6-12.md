---
title: 'Restore lifecycle evidence gates wiped by the BMAD 6.12.0 upgrade'
type: 'bugfix'
created: '2026-09-07'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '94dbb37694747e9dedee20591b84b5d6a69d19b3'
submodule_promotions: []
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Commit `1c36c45` ("fix: update BMAD 6.12.0") reinstalled the vendored skill trees and silently erased every project-owned lifecycle safeguard: the `V12 lifecycle evidence gates` section vanished from all 12 `ACTIVE_ROUTE_PATHS`, the `overlay_version`/`architecture_version` tokens vanished from all 4 `CONTEXT_WORKFLOW_PATHS`, and `bmad-dev-story` was deleted upstream so 2 of the 12 frozen route paths no longer exist. `verify_evidence_boundary.py` now returns `FAIL`/`EVIDENCE_GATE_NOT_USED`, 17 Python tests fail, and CI has been red on every push since. No review or done transition can be gated, so nothing in the repository can lawfully complete.

**Approach:** Restore the mechanical gate on every surviving lifecycle route and context workflow, re-freeze the route inventory against the routes BMAD 6.12.0 actually ships, and make a future upgrade fail loudly instead of silently disarming the gate. Preserve every frozen V9–V16 authority, IR-0 result, and the `ACTIVE` implementation hold exactly as they stand.

## Boundaries & Constraints

**Always:** Keep both skill trees byte-identical; place the gate strictly before each route's lifecycle status-write token; preserve `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` as distinct results; require nonempty assertion ledgers; keep V9–V16 authority bytes, the recorded IR-0 `READY`, and hold `ACTIVE` unchanged; keep every named fault fixture restoring byte-identically.

**Ask First:** Any change to a frozen authority artifact or its publisher; any change to `_bmad/scripts/publish_v9_planning_authority.py`; any package, submodule, gitlink, or product/runtime change; any push.

**Never:** Weaken or delete a gate assertion to make the suite green; treat a missing route as `not-applicable`; edit frozen V1–V16 authority bytes; reinterpret the IR-0 result; lift the implementation hold; start Story 7.1 or any successor; authorize release; push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Restored routes | Every active route carries exactly one gate marker before its lifecycle token | `verify_evidence_boundary.py` returns `PASS` with one `ROUTE-NN` row per route | Missing/duplicate marker is `EVIDENCE_GATE_NOT_USED` |
| Gate displaced | Marker moved after the lifecycle status-write token | `EVIDENCE_GATE_DISPLACED` with nonempty ledger | Fixture restores byte-identically |
| Gate gutted | Marker present but verifier/verdict tokens removed after it | `EVIDENCE_GATE_DECOY` | Fixture restores byte-identically |
| Mirror drift | `.agents/` copy differs from `.claude/` copy | `EVIDENCE_WORKFLOW_PARITY_DRIFT` | Reported per logical path |
| Upstream route removed | A frozen route path is absent from the installed tree | Visible `FAIL`/`BLOCKED` naming the path — never a silent pass | Absence is never `not-applicable` |
| Context workflow stripped | Required identity tokens missing from a context workflow | `EVIDENCE_CONTEXT_WORKFLOW_INVALID` | Fault test proves it turns red |

## Decisions

- **Route inventory shrinks to 10.** `bmad-dev-story` was deleted upstream and BMAD 6.12.0 ships no replacement (`bmad-agent-dev/SKILL.md` is a persona that writes no status). Drop both `bmad-dev-story/SKILL.md` paths and move the frozen "exactly twelve" constant and every test that hardcodes it to ten. The deletion is recorded deliberately, never inferred from absence. Do not remap onto `bmad-agent-dev`: gating a file with no lifecycle write is a vacuous assertion. `bmad-build/sync-sprint-status.md` is knowingly left ungated in this spec.
- **Restore the gate and add upgrade detection.** Re-inject the gate into the vendored route files, and add a preflight check that fails loudly the moment an installed route loses its gate, so the next BMAD upgrade cannot disarm the lifecycle gates silently. Do not relocate the gate's authoritative text into `_bmad/custom/*.toml`: the customization schema exposes no hook for injecting a blocking step at the pre-transition point.
- **`publish_v9_planning_authority.py` stays untouched.** V9 is a frozen root of trust; the historical lane keeps validating it from baseline `6400c09` where its bytes are correct. Its stale current-tree route constants are accepted, consistent with the existing `CANDIDATE_SOURCE_DRIFT` state V15 established by design. No V17 successor in this spec.
- **Full spec kept over the token guideline.** Routes and context workflows must land together or the verifier stays red, so this is one goal and is not split.

</frozen-after-approval>

## Code Map

- `_bmad/scripts/verify_evidence_boundary.py` -- `GATE_MARKER` (L19), `ACTIVE_ROUTE_PATHS` (L61-74), `LOGICAL_ROUTE_PATHS` (L75), `CONTEXT_WORKFLOW_PATHS` (L76-81), `LIFECYCLE_TOKENS` (L82-89); `validate_active_routes` (L272) asserts exactly-12 (L276), one marker (L284), pre-transition placement (L286-289), decoy tokens (L290-292), mirror parity (L294-298); `validate_context_workflows` (L357) asserts parity plus `overlay_version`/`architecture_version`/`frontmatter` tokens. Both read the **working tree** (`root / path`, L267), not a candidate tree.
- `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- hardcodes `12` (L41-42) and `4` (L149); fault tests at L61, L78 inject via a custom `reader` and assert on-disk bytes are unchanged. No fault test exists for the context-workflow token/parity failures.
- `_bmad/scripts/tests/test_verify_submodule_promotion.py` -- 11 of the 17 current failures; parametrized over the same route inventory including `bmad-dev-story/SKILL.md`.
- `_bmad/scripts/tests/test_generate_story_record.py` -- 2 failures: gate-span and byte-identical-skill-tree assertions.
- `_bmad/scripts/publish_v9_planning_authority.py` -- L138-160 hold a duplicate frozen copy of the route inventory. **Read-only: do not edit.**
- Routes needing the gate restored (10 surviving, mirrored in `.agents/skills/` and `.claude/skills/`): `bmad-build/step-04-review.md` (lifecycle write L11), `bmad-build/step-05-present.md` (L15/L17), `bmad-build/step-oneshot.md` (L76/L79), `bmad-build-auto/step-04-review.md` (L11), `bmad-code-review/steps/step-04-present.md` (§6, L85-105).
- Context workflows needing identity tokens restored (4): `bmad-build/compile-epic-context.md`, `bmad-build/step-01-clarify-and-route.md`, and the `bmad-build-auto/` counterparts.
- `git show 63a1a2d:<path>` -- the last commit where every gate was intact; source of the exact prior gate and token wording.
- `.github/workflows/planning-authority-preflight.yml` -- L77 runs the verifier; L82-104 the skip-free Python lane. Do not weaken either.
- `_bmad/custom/bmad-build.toml`, `bmad-build-auto.toml`, `bmad-review.toml` -- project-owned customization that survived the upgrade; confirms which surfaces an upgrade does NOT overwrite.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/scripts/verify_evidence_boundary.py` -- re-freeze `ACTIVE_ROUTE_PATHS` (10 paths), `LOGICAL_ROUTE_PATHS` (5), and `LIFECYCLE_TOKENS` with the `bmad-dev-story` entries removed, and change the exactly-12 assertion to exactly-10, keeping absence of a declared route a visible `FAIL`/`BLOCKED` -- the inventory must describe routes that exist.
- [x] The 10 surviving route files in both trees -- restore exactly one `V12 lifecycle evidence gates` section immediately before each lifecycle status-write token, carrying both verifier commands and all four result states -- this is the gate itself.
- [x] The 4 context workflow files in both trees -- restore the `overlay_version`/`architecture_version` identity requirements -- `validate_context_workflows` fails closed without them.
- [x] `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- change the hardcoded `12` (L41-42) to `10`, keep `4` for context workflows, and add the missing fault test for context-workflow token/parity failures -- the runbook forbids an unproven guard.
- [x] `_bmad/scripts/tests/test_verify_submodule_promotion.py`, `test_generate_story_record.py` -- re-point the parametrized inventory at the 10-path route set without weakening any assertion.
- [x] Add the upgrade-detection check that fails loudly when an installed route has lost its gate marker, and wire it where an upgrade or CI run will hit it -- a silent wipe must never again be discovered only via red CI.
- [x] Verify both skill trees remain byte-identical and no gitlink, package, or authority artifact changed; validate the commit message with the pinned commitlint and do not push.

**Acceptance Criteria:**
- Given the restored tree, when `verify_evidence_boundary.py` runs against baseline `94dbb37` and `HEAD`, then the result is `PASS` with a nonempty ledger and zero blockers.
- Given the restored tree, when the CI Python lane runs, then it reports zero failed, zero errored, and zero skipped tests.
- Given each named fault fixture (missing marker, displaced marker, decoy gate, mirror drift, stripped context token, absent declared route), when injected one at a time, then the specific stable code is raised with a nonempty ledger and the fixture restores byte-identically.
- Given a simulated upgrade that strips the gate from one installed route, when the upgrade-detection check runs, then it fails and names the affected path.
- Given the completed change, when V13, V14, V15, and V16 `--check` run, then all pass and the recorded IR-0 `READY` and hold `ACTIVE` are unchanged.

## Implementation Notes

- Gate prose restored verbatim from `63a1a2d` rather than rewritten, so `EVIDENCE_GATE_DECOY`'s token expectations hold without relaxation.
- `LIFECYCLE_TOKENS["bmad-code-review/steps/step-04-present.md"]` re-frozen from ``set `{new_status}` = `done` `` to ``set `new_status` = `done` ``: BMAD 6.12.0 dropped the placeholder braces, so the old token named text that no longer exists and would have raised `EVIDENCE_GATE_DISPLACED` against a correctly placed gate. Verified the new token occurs in the current file.
- `validate_context_workflows` gained the optional `reader` hook `validate_active_routes` already had, so the new fault tests inject without writing to disk. No assertion weakened.
- `test_verify_submodule_promotion.py`'s step-oneshot gate span follower re-pointed from the deleted `### Generate Spec Trace` to `### Finalize Spec`. Verified the span stays tight (L72 to L76) and still precedes the `status: 'done'` write at L80 rather than running to EOF.
- New `_bmad/scripts/check_lifecycle_gate_preflight.py` is working-tree only and uses no Git, so it runs immediately after an upgrade. `LIFECYCLE_GATE_UNDECLARED_ROUTE` catches a renamed route, which the declared set alone would pass.
- `docs/runbooks/evidence-boundary-validation.md` deliberately untouched: its bytes are hashed into the frozen V9 authority bundle. New preflight codes are documented in the script docstring and tests instead.
- Not committed and not pushed. An unrelated untracked file from a concurrent session, `spec-v17-implementation-hold-decision-authority.md`, was left alone and must not be swept into this commit.

- Gate text was recovered verbatim from `63a1a2d` for all five logical routes and re-anchored to
  the section each route now writes lifecycle status in. `bmad-build/step-oneshot.md`'s follower
  is `### Finalize Spec` (upstream renamed `### Generate Spec Trace`), so
  `WORKFLOW_GATE_CONTRACTS` was re-pointed at the new heading rather than the gate span being
  allowed to run to end-of-file.
- `LIFECYCLE_TOKENS["bmad-code-review/steps/step-04-present.md"]` was re-frozen from
  ``set `{new_status}` = `done` `` to ``set `new_status` = `done` ``: BMAD 6.12.0 dropped the
  placeholder braces throughout that skill. The token must name text that exists, or
  `EVIDENCE_GATE_DISPLACED` would fire on a correctly placed gate.
  The restored gate prose in that file still writes `{new_status}` and `{spec_file}` in the
  older braced style; that was left verbatim so the diff reads as recovery, and no
  assertion depends on it.
- Context-workflow identity requirements were restored into the upgraded upstream prose (the
  surrounding sentences changed), not by reverting the files to `63a1a2d`.
- `validate_context_workflows` gained the same optional `reader` hook `validate_active_routes`
  already had, so the new context-workflow fault tests inject through the reader and the on-disk
  bytes are asserted unchanged. No assertion was relaxed.
- Upgrade detection is `_bmad/scripts/check_lifecycle_gate_preflight.py`: working-tree only, no
  Git, so it runs immediately after an upgrade. It is wired into `.githooks/pre-commit` (guarded
  to staged `.agents/skills`, `.claude/skills`, or verifier changes) and into
  `planning-authority-preflight.yml` ahead of the evidence gate. It also reports gates found
  outside the frozen inventory (`LIFECYCLE_GATE_UNDECLARED_ROUTE`), which is how a renamed route
  is caught — the declared set alone would still pass.
- `docs/runbooks/evidence-boundary-validation.md` was deliberately not edited: its bytes are hashed
  into the frozen V9 authority bundle.

## Spec Change Log

## Review Triage Log

## Design Notes

The gate is not new code — it is recovering a design that already existed and was proven, then erased by an upgrade. Prefer restoring the prior wording from `63a1a2d` verbatim over rewriting it, so the diff shows recovery rather than redesign, and so `EVIDENCE_GATE_DECOY`'s token expectations continue to hold without being relaxed to fit new prose.

Two CI failures are in play and only one is in scope. The evidence-gate failure (this spec) began at `1c36c45`. A separate Python-lane failure has been red since `63a1a2d` on 2026-08-22, when the evidence gate still passed; it is currently masked because the verifier now fails first. After this fix lands, re-run the lane and treat any residual failure as separate work rather than folding it in here.

## Verification

**Commands:**
- `uv run --frozen python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 94dbb37694747e9dedee20591b84b5d6a69d19b3 --candidate HEAD` -- expected: `"result": "PASS"`, nonempty ledger, `"blockers": []`.
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests --ignore=_bmad/scripts/tests/test_publish_v9_planning_authority.py` -- expected: zero failed, zero errored, zero skipped (currently 17 failed / 259 passed).
- `uv run --frozen python3 _bmad/scripts/publish_v16_planning_tooling_lifecycle.py --repository . --check` and the V13/V14/V15 equivalents -- expected: all `OK`, unchanged from today's passing state.
- `for p in <the 10 routes>; do diff .agents/skills/$p .claude/skills/$p; done` -- expected: no output.
- `git diff --check` and a raw-mode `160000` audit -- expected: no whitespace errors, zero gitlink changes.
