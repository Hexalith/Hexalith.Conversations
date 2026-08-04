---
title: 'Assess V11 independent implementation readiness'
type: 'chore'
created: '2026-08-04'
status: 'done'
baseline_commit: 'a28d14bcfeb5e46c0bcea958c7bcb02b3f74b75f'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The candidate-bound V11 planning publication requires an independent IR-0 verdict before the release owner may consider lifting the global implementation hold. The verdict must distinguish authority defects from unavailable validation and bind the exact planning candidate rather than descendant `HEAD`.

**Approach:** Recompute the candidate, authority bundle, Story 7.1 sidecar, execution graph, and hold prerequisites; run every required validation; then publish one machine-readable-frontmatter Markdown report whose actual result is `READY` or `BLOCKED` and whose evidence is exact and reproducible.

## Boundaries & Constraints

**Always:** Bind planning candidate `1e72e63cbf2b556b8dc6fe732428c66f51985ac7`, bundle digest `2c5e45b07b3d58dca1f1baca18dea6e98ba1ad37db682517a6f09ed15caaa3c4`, the V11 authority pair, and the candidate's ten gitlinks. Recompute hashes and graph relations independently. Preserve `PASS`, `FAIL`, `BLOCKED`, and not-applicable as distinct states; any unavailable mandatory command makes IR-0 `BLOCKED` even when a fallback passes.

**Ask First:** Any proposal to repair authority, install or persist dependencies, reinterpret a required command, use `HEAD` as `PC`, create a hold record, or alter the report contract after approval.

**Never:** Predetermine `READY`; lift or weaken the hold; edit planning authority, generated companions, sprint status, either Story 7.1 spec, schemas, generator/tests/results/final records, product/runtime/deployment files, or submodules; implement `7.1-SCHEMAS` or any Story 7.1 work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Complete matched evidence | Every mandatory check passes for the bound PC/bundle | Report `READY`; hold remains `ACTIVE` pending owner decision | No implied authorization |
| Mandatory lane unavailable | Exact command cannot execute or yields no non-vacuous ledger | Report `BLOCKED` with command, exit, stderr, and stable assessment blocker | Fallback is supplementary only |
| Candidate or artifact drift | PC, digest, authority, member hash, gitlink, sidecar, or graph differs | Report `BLOCKED` with observed and expected identities | Never rebind automatically |
| Missing hold decision | IR-0 evidence otherwise complete but no owner record exists | Report actual IR-0 result and effective hold `ACTIVE` | Missing post-IR-0 decision is not recast as an IR-0 defect |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` -- self-excluding 61-row manifest, PC, authority pair, ten gitlinks, and bundle digest.
- `_bmad/scripts/publish_v9_planning_authority.py:1615` -- canonical candidate resolver and exact `V9_PLANNING_AUTHORITY_OK` check output.
- `_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` -- closed checkpoint scope, `LIFTED` entry requirement, false completion effects, and one-way digest binding.
- `_bmad-output/planning-artifacts/v9-execution-graph-v1.json` -- 32-node/49-edge graph; checkpoint and all successor IR-0 ancestry.
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:4160` and `_bmad-output/planning-artifacts/architecture.md:2194` -- byte-pinned V11 authority and fail-closed hold semantics.
- `_bmad-output/implementation-artifacts/epic-6-retro-2026-08-03.md:34` -- rejected Epic 6 acceptance, candidate/finality defects, false historical green, and A1-A3 hold/remediation obligations.
- `_bmad-output/implementation-artifacts/epic-6-context.md` and `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:985` -- generated-context parity surface currently exercised by the root-pinned architecture suite.
- `_bmad-output/implementation-artifacts/sprint-status.yaml:41` -- publication-time `ACTIVE`, Story 6.2 done, Epic 7/Story 7.1 backlog.
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md` -- sole output; YAML frontmatter carries gate, result, identities, blockers, and command ledger.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` -- prove PC ancestry and recompute all member hashes, bundle digest, candidate gitlinks, containment, uniqueness, and self-exclusion.
- [x] `_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` and `_bmad-output/planning-artifacts/v9-execution-graph-v1.json` -- validate schemas, base/amendment digests, exact path sets, checkpoint semantics, acyclicity, edge parity, and IR-0 ancestry.
- [x] `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/implementation-artifacts/epic-6-retro-2026-08-03.md`, and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- execute mandatory checks, evaluate every declared hold prerequisite/open corrective obligation, and preserve effective `ACTIVE` without writing a decision.
- [x] `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md` -- publish the actual candidate-matched verdict with exact commands, exits, outputs/digests, blocker codes, and explicit non-authorization.

**Acceptance Criteria:**
- Given the detached PC and current bundle, when every declaration is independently recomputed, then the report records exact expected/observed identities and zero unexplained mismatches.
- Given the sidecar and graph, when parity is evaluated, then `7.1-SCHEMAS <- [6.2, IR-0]`, `7.1 <- [6.2, 7.1-SCHEMAS, IR-0]`, Story 7.2 remains behind complete Story 7.1, and all successor stories remain downstream of IR-0.
- Given all mandatory commands, when any command is unavailable or nonzero, then the verdict is `BLOCKED`; no passing fallback converts it to `READY`.
- Given the rejected Epic 6 acceptance and open corrective ledger, when predecessor sufficiency is assessed, then every unresolved blocker, dependency cycle, or missing additive supersession decision is named rather than inferred closed from lifecycle status.
- Given the published report, when machine readers parse its frontmatter, then one result, PC, bundle digest, authority pair, effective hold state, blocker list, and nonempty command ledger are present while Story 7.1 and the hold remain unchanged.

## Spec Change Log

## Design Notes

The IR-0 report is a mutable decision record outside the bundle digest. It binds the immutable PC/bundle in one direction. A `READY` result would satisfy only the readiness prerequisite; only a later release-owner record can make the effective hold `LIFTED`.

## Verification

**Commands:**
- `python3 -m pytest -q _bmad/scripts/tests/test_publish_v9_planning_authority.py` -- expected: exit 0, non-vacuous publisher/mutation suite.
- `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` -- expected: exact PC and bundle digest with exit 0.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV9ValidationTest` -- expected: 6/6, zero skipped/not-run.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV8ValidationTest` -- expected: 6/6, zero skipped/not-run.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.ArchitecturePlanningAuthorityValidationTest` -- expected: 17/17, zero skipped/not-run.
- `git diff --check` -- expected: no whitespace defects; final diff adds only this workflow spec and the IR-0 report.

## Suggested Review Order

**Verdict and authority boundary**

- Start with the fail-closed result and explicit hold non-authorization.
  [`implementation-readiness-report-2026-08-04-ir-0.md:313`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L313)

- Confirm machine-readable expected/observed authority and graph identities.
  [`implementation-readiness-report-2026-08-04-ir-0.md:66`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L66)

- Verify all ten candidate gitlinks remain bound to immutable PC.
  [`implementation-readiness-report-2026-08-04-ir-0.md:144`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L144)

**Blocking evidence**

- Review the six stable blockers and their evidence references.
  [`implementation-readiness-report-2026-08-04-ir-0.md:172`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L172)

- Inspect exact command outcomes, environment provenance, and supplementary lanes.
  [`implementation-readiness-report-2026-08-04-ir-0.md:228`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L228)

- Trace rejected predecessor obligations and unresolved supersession routing.
  [`implementation-readiness-report-2026-08-04-ir-0.md:416`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L416)

**Machine and scope verification**

- Reproduce closed-frontmatter, hash, ancestry, gitlink, and verdict checks.
  [`implementation-readiness-report-2026-08-04-ir-0.md:457`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L457)

- Reconfirm absent hold/checkpoint outputs and blocked Story 7.1 state.
  [`implementation-readiness-report-2026-08-04-ir-0.md:573`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L573)

- Finish at the explicit prohibition against implementation or hold lift.
  [`implementation-readiness-report-2026-08-04-ir-0.md:587`](../planning-artifacts/implementation-readiness-report-2026-08-04-ir-0.md#L587)
