---
title: 'Publish Epic 16 planning authority'
type: 'chore'
created: '2026-08-19'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '4e3828cfaa189604c11be34e8d67bc94520785d8'
context: ['{project-root}/docs/runbooks/evidence-boundary-validation.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved A4–A6 correction defines Epic 16, but the publisher still exposes 27 stories, omits V13 checkpoint nodes, and is candidate-drifted. Publishing unchanged would lose obligations or claim a false graph.

**Approach:** Append Epic/architecture V14 overlays and three V14 story contracts, then regenerate through a candidate-bound two-commit transaction. Clarify that V14 carries DC-9–DC-11, reuses the checkpoint-sidecar head, composes 38 nodes/61 edges, and keeps the release-owner hold outside the graph.

## Boundaries & Constraints

**Always:** Preserve V1–V12 epic bytes, V1–V13 architecture bytes, the frozen V9 schema, historical evidence, and all ten gitlinks. Keep hold=`ACTIVE`, no IR-0 result, A2–A6 `open`, and 16.x `backlog`. Legacy contracts retain V10 authority; 16.x binds V14. Approval authorizes two validated local commits (canonical C1, generated C2), not a push.

**Ask First:** Halt for a different authority, sidecar, mapping, graph, Story 16 command, lifecycle value, allowlist, or commit count.

**Never:** Reconstruct Epic V13; mint a sidecar; rewrite frozen data; run IR-0; lift the hold; start/close work; change product, dependencies, submodules/gitlinks, PRD, UX, or unrelated tracking.

## I/O & Edge-Case Matrix

| State | Expected behavior | Failure |
|---|---|---|
| Clean C1 | Atomic C2: 30 contracts, 38 nodes/61 edges, V14 companions | Gates `PASS`; nonempty ledgers |
| Protected bytes or candidate differ | No managed output replaced | Stable drift code; exit `1` |
| Baseline..C2 has an unlisted path or gitlink delta | Publication rejected | Exact offenders; `FAIL` |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/{sprint-change-proposal-2026-08-18.md,architecture.md,prds/prd-Conversations-2026-06-02/epics.md}` -- append-only authority; old markers read-only.
- `_bmad/scripts/publish_v9_planning_authority.py` and `_bmad/schemas/` -- parser, graph, renderer, atomic publisher, frozen V9 schema, and additive V14 schemas.
- `_bmad/scripts/verify_evidence_boundary.py` and `.github/workflows/planning-authority-preflight.yml` -- path/gitlink scope gate.
- `_bmad/scripts/tests/` and `tests/Hexalith.Conversations.Conformance.Tests/` -- independent publication proofs.
- `_bmad-output/planning-artifacts/v9/` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- generated; no hand edits.

## Tasks & Acceptance

**Execution:**
- [ ] Append the clarification and V14 overlays; carry DC-9–DC-11, exact A4→16.1/A5→16.2/A6→16.3 mappings, checkpoint nodes, and hold semantics.
- [ ] Give each Story 16 contract six non-vacuous scenarios and two final-record paths. AC-01–05 use the matching `verify_story_16_1.py`, `verify_story_16_2.py`, or `verify_story_16_3.py`; each command's `--scenario` and output basename equal its literal AC ID. AC-06 uses `generate_story_record.py`.
- [ ] Extend publisher/schema routing for 30 contracts and 38 nodes/61 edges; preserve historical authorities; regenerate all companions.
- [ ] Extend evidence validation and preflight to compare baseline..C2 with the declared source/generated allowlist and reject every mode-160000 delta.
- [ ] Add Python/C# positive and fault tests for mappings, frozen bytes, sidecars, graph, lifecycle neutrality, rollback, and non-vacuity.
- [ ] Validate C1, publish against C1, rebind the live companion, create C2, and run all gates against C2.

**Acceptance Criteria:**
- Given V13 and the approved proposal, when V14 publishes, then no Epic V13 exists, DC-9–DC-11 remain canonical, both V14 markers cross-point, and the existing sidecar head is pinned.
- Given the composed authority, when validated, then 30 stories and 38 nodes/61 edges exist with the exact approved mapping/predecessors.
- Given regenerated tracking, then hold=`ACTIVE`, IR-0 has no result, A2–A6 are `open`, and Epic 16 is entirely `backlog`.
- Given baseline/C1/C2, then changed paths equal the allowlist, gitlinks match, ledgers are nonempty, and no failed/skipped/not-run test passes.

## Spec Change Log

## Design Notes

Checkpoint-sidecar V14 and planning-overlay V14 are separate namespaces. Reusing `v14-current-candidate-authority-v1.json` avoids self-reference; it is pinned evidence, not a mutable input. Story 16.1 has graph predecessors `7.4` and `IR-0`, while the separate global hold still prohibits execution without a `LIFTED` decision.

## Verification

**Commands:**
- `uv run --frozen python3 -m pytest -q _bmad/scripts/tests` -- zero failures/skips.
- `uv run --frozen python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` -- V14 OK; no drift.
- `uv run --frozen python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 4e3828cfaa189604c11be34e8d67bc94520785d8 --candidate HEAD` -- `PASS`, exact scope, zero gitlink delta.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release` -- succeeds.
- `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -class Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV9ValidationTest -class Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV8ValidationTest -class Hexalith.Conversations.Conformance.Tests.ArchitecturePlanningAuthorityValidationTest` -- zero failed/skipped/not-run.
- `git diff --check` -- clean.
