---
title: 'Rebind the current E6 candidate and authorize independent IR-0'
type: 'governance'
created: '2026-08-22'
status: 'in-progress'
baseline_commit: 'bdd27b53e0e676f26bdcd093ef2bccefadcae285'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-19.md'
  - '{project-root}/_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json'
  - '{project-root}/_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json'
  - '{project-root}/_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json'
---

<frozen-after-approval reason="human-approved Option 1 continuation — do not modify">

## Intent

**Problem:** V12 historical reconstruction remains correctly `FAIL` / `REJECTED`, while V13 accepted the additive present-state proof and V14 preserved an `ACTIVE` hold. The current authority bundle predates the completed static anti-skip repair and post-review mutation guard, so candidate-source drift prevents A2/A3 closure and independent IR-0.

**Approach:** Preserve V1–V14 and the V12 rejection byte-for-byte. Rebind the existing generated authority bundle to one committed current candidate, close A2 and A3 only after their full mechanical gates pass, then authorize exactly the independent IR-0 already permitted by V12's completion effect. Do not add a generic waiver, parallel authority chain, or redundant schema.

## Boundaries & Constraints

**Always:** Bind exact Git objects and source bytes; use existing publication tooling; preserve distinct `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` results; require nonempty ledgers; keep the implementation hold `ACTIVE`; preserve unrelated blocked Epic 5 evidence.

**Ask First:** Any product, package, submodule, gitlink, signed-evidence, frozen V1–V14, or public-contract change; any need to authorize more than one independent IR-0 assessment.

**Never:** Rewrite V12/V13/V14; reinterpret the historical rejection; bypass A2/A3; manufacture `READY`; lift the hold; start Story 7.1 or another successor; authorize release or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Behavior | Failure Handling |
| --- | --- | --- | --- |
| Current candidate | Clean committed source candidate | Generated bundle binds exact bytes and all checks reproduce | Drift is `FAIL` |
| A2/A3 closure | Focused/full lanes and lifecycle gates pass | Both ledger rows become `done` | Any red, skipped, or empty result preserves `open` |
| IR-0 handoff | A1–A3 closed at the bound candidate | Independent assessment runs outcome-neutral | `FAIL`/`BLOCKED` never becomes `READY` |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/publish_v9_planning_authority.py` — existing atomic candidate rebind and deterministic companion generation.
- `_bmad/scripts/verify_evidence_boundary.py` — lifecycle gate over exact paths, raw gitlinks, active routes, context, and publication output.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — authoritative A2/A3 lifecycle rows and IR-0/hold state.
- `_bmad-output/implementation-artifacts/spec-e6-remediation-a2-restore-lifecycle-gates.md` — A2 implementation and verified mutation coverage.

## Tasks & Acceptance

**Execution:**
- [x] Commit this approved continuation spec as the source candidate without unrelated changes.
- [x] `_bmad/scripts/publish_v9_planning_authority.py` and its managed outputs — rebind the existing bundle to the exact committed source candidate; preserve pinned V12–V14 sidecars.
- [x] `_bmad/scripts/tests` — run the focused A2 matrix and complete Python lane with zero failed, skipped, or not-run tests.
- [x] `_bmad-output/implementation-artifacts/spec-e6-remediation-a2-restore-lifecycle-gates.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` — close A2, then A3, only after both lifecycle gates continue.
- [x] `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md` — run an independent, candidate-matched, outcome-neutral IR-0 and preserve its actual result.

**Acceptance Criteria:**
- Given the committed continuation candidate, when publication is regenerated and checked, then every managed companion and bundle row binds that candidate exactly while V12–V14 bytes remain unchanged.
- Given the A2/A3 fault and full-suite lanes, when verification runs, then all tests pass with zero skips and every named mutation restores byte-identically.
- Given passing lifecycle gates, when A2 and A3 close in order, then the exact sprint rows are `done`, IR-0 remains unrun until closure, and the hold remains `ACTIVE`.
- Given A1–A3 closed at one candidate, when independent IR-0 runs, then it records the evidence-derived result without lifting the hold, starting successors, or claiming release.

## Verification

- `uv run --frozen python3 -m pytest -q --tb=short _bmad/scripts/tests`
- `uv run --frozen python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check`
- `uv run --frozen python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline bdd27b53e0e676f26bdcd093ef2bccefadcae285 --candidate HEAD`
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release -m:1`
- `git diff --check`

## Spec Change Log

- 2026-08-22: Jerome selected Option 1 and authorized ownership of the concurrent worktree plus creation of the committed continuation candidate.
- 2026-08-22: Rebound planning candidate `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`, preserved V12–V14 byte-for-byte, closed A2/A3 after passing gates, and recorded independent IR-0 `READY` with the implementation hold still `ACTIVE`.
