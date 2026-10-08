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

The commands above are the 2026-08-22 historical checks. The disposition below
does not re-run them. Current routine work uses the
[current-change policy](../../docs/runbooks/current-change-validation.md).

## Spec Change Log

- 2026-08-22: Jerome selected Option 1 and authorized ownership of the concurrent worktree plus creation of the committed continuation candidate.
- 2026-08-22: Rebound planning candidate `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`, preserved V12–V14 byte-for-byte, closed A2/A3 after passing gates, and recorded independent IR-0 `READY` with the implementation hold still `ACTIVE`.
- 2026-10-08: Appended the current-policy disposition below after verifying the 2026-08-22 record. The committed `in-progress` status remains historical metadata.
- 2026-10-08: Review corrected the sprint-banner reading, bundle and V12–V14 identities, historical verification scope, and runbook link.

## Current policy disposition (2026-10-08)

Recording commit `6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4` marked the five
task checkboxes in this spec and appended the 2026-08-22 completion entry.
Its diff contains only this spec and the IR-0 report. It left
`status: in-progress` and `review_loop_iteration: 0` unchanged. This
verification does not support rewriting that status to `done` or `in-review`.
There is no review triage, and the Verification commands remain the historical
August 22 checks. The frontmatter `context` list remains that same August 22
list, including the historical evidence-boundary runbook.

Read-only comparison on 2026-10-08 used `git rev-parse`, `git grep`,
`git diff-tree`, and `git show` against `HEAD`
`f6f8c4fea84c728568bfae4c4a5a58262a01c49d` before this disposition:

- Spec blob `88c21bc4627724bf8bee076d58e4aa34e7ee9c94` and IR-0 report blob
  `1abb099d5b2980eb01eecb911b61e3b6a76bf485` match the recording commit and
  that `HEAD`.
- The IR-0 report, assessed from publication
  `5900d9f8500af72183db9511db60b39ad7f74f29`, records `READY`, planning
  candidate `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`, and effective hold
  `ACTIVE`.
- `_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` blob
  `827c3a2b30ae3617b4f01863e92fcf1392f9dd70` matches at the recording commit
  and that `HEAD`. Its `planningCandidate` is
  `1e9a61126d3b7a55b514b7c7c8942d5af03355e5` and its `bundleDigest` is
  `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055`.
- These sidecar blobs match at baseline
  `bdd27b53e0e676f26bdcd093ef2bccefadcae285`, the planning candidate, the
  recording commit, and that `HEAD`:
  - `_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json`
    `f2b346d0f982ed2ab9db94da2d0580b73e3395db`
  - `_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json`
    `e8d1703122bf47fb7f308fd58bc8ad535d3919ec`
  - `_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json`
    `cf766b608b9c650354fede4d359d36a9f1cadca6`
- V1–V11 were not compared. This append does not edit them.

The sources disagree, and each one keeps its own meaning:

- Frozen V12 still lists A1–A3 as `open`. Preserving that sidecar left those
  rows unchanged.
- The IR-0 report is the August 22 assessment. It records A1–A3 `PASS` with
  recorded status `done`, result `READY`, and effective hold `ACTIVE`.
- `sprint-status.yaml` at publication
  `5900d9f8500af72183db9511db60b39ad7f74f29` and at `HEAD`
  `f6f8c4fea84c728568bfae4c4a5a58262a01c49d` still has one V14 banner: the
  global implementation hold remains `ACTIVE`, IR-0 was not run, A2 and A3
  are done, and A4–A6 remain open. That banner predates the IR-0 report. The
  recording commit did not change sprint tracking, so the checkbox on the
  A2/A3 task records this spec's acceptance of that already-written banner and
  of the later IR-0 report. It does not record a sprint-status edit.

The recorded completion is the checked spec, the completion changelog, and the
IR-0 report. It does not rewrite V12 or the sprint banner.

This append leaves the frozen approval block, the compared V12–V14 sidecars,
and the IR-0 report byte-for-byte unchanged. It does not rebind authority,
restore retired gates, run another IR-0, lift the implementation hold, start
a successor story, or authorize release. Later hold decisions stay separate
records. New routine work follows the
[current-change policy](../../docs/runbooks/current-change-validation.md).

Focused checks after the review patch, on 2026-10-08:
`python3 scripts/check-root-submodules.py --repository .` exited 0
(`root submodules: PASS`), and `git diff --check` on the edited spec and
runbook exited 0. The historical Verification commands were not re-run.

## Review Triage Log

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | medium | patch | The first disposition named the parent comment and omitted the same V14 banner at the compared `HEAD`. `git show HEAD:sprint-status.yaml` still has that one banner. The corrected disposition records it. |
| BH-02 | medium | patch | "A2 and A3 were done" and "IR-0 had not been run" are one banner, not two sources, and the first text gave them no precedence against V12 `open` and the IR-0 report. The corrected disposition states each source's meaning. |
| BH-03 | medium | patch | The first text omitted the publication commit, bundle blob, and `bundleDigest`, and its closing sentence covered V1–V14 without a V1–V11 comparison. The corrected text cites the compared blob ids and says V1–V11 were not compared. |
| BH-04 | medium | patch | "Current policy does not support" was not a sentence in the policy file, and the Verification section still read as a current command list. The text now says this verification does not support a status rewrite, and Verification is labeled historical. |
| BH-05 | medium | patch | The runbook link opened the spec at `status: in-progress`, and the frontmatter `context` list was unlabeled. The link now targets this section, and the disposition identifies `context` as the unchanged August 22 list. |
| BH-06 | false | reject | The same paragraph already stated that `review_loop_iteration: 0` was left unchanged. There is no separate permission to rewrite it. |
| BH-07 | low | reject | The 2026-10-08 changelog line names no person. This run has no named human actor to add, and the omission does not change the recorded commits or blobs. |
| BH-08 | medium | patch | "Checked every task" collided with "did not edit sprint tracking" because one task says to close A2/A3 in `sprint-status.yaml`. The recording commit's file list is only this spec and the IR-0 report; the checkbox is not a sprint edit. |
