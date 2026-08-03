---
title: 'Publish the v10 evidence-boundary planning correction'
type: 'refactor'
created: '2026-08-03'
status: 'done'
review_loop_iteration: 0
baseline_commit: '93a86f29b82a7429aa80a31c14b4507cd7c58656'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The final v9 authority is still an unbound specification with no companion publication, and Story 10.3 names six obsolete workflow paths while omitting the current build/review routes. Epic 5 action A5 therefore remains open and the current plan cannot mechanically prove route-complete evidence-boundary guidance.

**Approach:** Append v10 amendments changing only Stories 10.3/10.4, then atomically generate the complete candidate-bound v9 companion set. Add team BMAD guidance and its runbook, without inserting workflow gates or implementing Epic 10 helpers, verification, reader migration, or runtime behavior.

## Boundaries & Constraints

**Always:** Use `epic-6-authority-2026-08-03-v10` and `conversations-architecture-2026-08-03-v10`; append after unchanged v8/v9 markers; retain Epic 10, Stories 10.1/10.2, AC-10.3-01..08, AC-10.4-01..08, and the 27-reader inventory; add only AC-10.4-09; bind one committed `PC`; keep hold=`ACTIVE` and A5=`open`; mechanically validate schemas, digests, topology, supersession, 52/28 UX parity, sprint projection, routes, tree parity, and resolved customization.

**Ask First:** Any need to change approved identities, frozen prefixes, protected artifacts, unrelated dirt, validator strength, or scope.

**Never:** Create an epic/story; edit PRD/addendum, UX semantics, evidence, completed records, packages, runtime code, the twelve installed route bodies, shipped defaults, gitlinks/submodules, or unrelated context files; close A5; lift the hold; run/bias IR-0; push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Atomic publication | Approved proposal and final v9 sources | v10 authorities and every companion bind one `PC` | Accept only after complete validation |
| Drift or omission | Route, alias, parity, digest, schema, customization, or coverage mismatch | Stable blocker; no partial acceptance | Preserve hold; do not weaken checks |
| Dirty worktree | Related proposal plus unrelated context changes | Scoped commits contain only publication paths; unrelated bytes remain untouched | Stop before staging any unexpected path |
| Independent assessment | Valid committed publication | Return neutral prompt naming `PC`, bundle digest, and authorities | Do not run it |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`, `_bmad-output/planning-artifacts/architecture.md` -- append-only v10 authorities; existing v9 blocks are immutable.
- `_bmad-output/planning-artifacts/v9/`, `v9-*-v1.json` -- absent schemas, 27 contracts, inventories, graph, map, and bundle already named by v9.
- `_bmad/scripts/generate_epic_6_current_execution_view.py` -- v8-only precedent; preserve v1 and add deterministic v9 publication tooling/v2 view.
- `_bmad-output/planning-artifacts/ux-requirement-map.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` -- current UX owner and backlog projections; retain denominators, hold, and A5.
- `_bmad/custom/{bmad-build,bmad-build-auto,bmad-review}.toml`, `docs/runbooks/evidence-boundary-validation.md` -- tracked guidance resolved by `resolve_customization.py`.
- `tests/Hexalith.Conversations.Conformance.Tests/*PlanningAuthorityValidationTest.cs` -- preserve prefix checks and add complete v10/companion validation.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`, `_bmad-output/planning-artifacts/architecture.md` -- append v10 authorities with v2 inventories, stable codes, amended ACs, rollback, and unchanged obligation ownership.
- [x] `_bmad/custom/*.toml`, `docs/runbooks/evidence-boundary-validation.md` -- add team guidance without editing installed routes/defaults.
- [x] `_bmad/scripts/publish_v9_planning_authority.py`, `_bmad/schemas/`, `_bmad-output/planning-artifacts/v9*`, `_bmad-output/planning-artifacts/ux-requirement-map.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` -- generate all contracts/projections and bundle from committed `PC`, excluding self/mutable records.
- [x] `_bmad/scripts/tests/test_publish_v9_planning_authority.py`, `tests/Hexalith.Conversations.Conformance.Tests/*PlanningAuthorityValidationTest.cs` -- validate all frozen boundaries, mappings, inventories, customization, denominators, hold, and A5.

**Acceptance Criteria:**
- Given publication runs, when it completes, then all companions atomically bind the same `PC`, v10 identities, inventories, and immutable predecessors.
- Given focused validation, when all lanes run, then failures, skips, not-run, drift, gaps, duplicates, and vacuity are zero.
- Given the final tree, when protected boundaries are compared, then all forbidden and unrelated bytes are unchanged.
- Given handoff, when work stops, then hold=`ACTIVE`, A5=`open`, no IR-0 artifact/push exists, and the unbiased prompt is provided.

## Spec Change Log

## Design Notes

Commit canonical sources as `PC`, then derive records from committed blobs to avoid self-reference. Any later canonical repair invalidates and regenerates the candidate.

## Verification

**Commands:**
- `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check`
- `uv run --no-cache --with pytest --with jsonschema pytest -q _bmad/scripts/tests/test_publish_v9_planning_authority.py`
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore`
- Run the compiled xUnit assembly with `-automated sync -failSkips -class` for V9, V8, and architecture validators; run `git diff --check` and scoped status/diff inspection. All lanes must be non-vacuous with zero failures/skips/not-run.

## Suggested Review Order

**Authority correction**

- Start here: append-only v10 authority narrows changes to Stories 10.3 and 10.4.
  [`epics.md:3997`](../planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#L3997)

- Matching architecture preserves v9 invariants while rebasing workflow and guidance ownership.
  [`architecture.md:2127`](../planning-artifacts/architecture.md#L2127)

**Publication transaction**

- Candidate-bound rendering validates routes, contracts, projections, and schemas before publication.
  [`publish_v9_planning_authority.py:1117`](../../_bmad/scripts/publish_v9_planning_authority.py#L1117)

- Set-level rollback restores every prior byte after mid-commit filesystem failure.
  [`publish_v9_planning_authority.py:1180`](../../_bmad/scripts/publish_v9_planning_authority.py#L1180)

- Full supersession parsing preserves all 156 obligations and frozen denominators.
  [`publish_v9_planning_authority.py:751`](../../_bmad/scripts/publish_v9_planning_authority.py#L751)

**Machine contracts and projections**

- Closed story contracts use the canonical identity and explicit result semantics.
  [`v9-story-contract-v1.schema.json:7`](../../_bmad/schemas/v9-story-contract-v1.schema.json#L7)

- Bundle pins one PC, 58 artifacts, ten gitlinks, ACTIVE hold, and open A5.
  [`v9-authority-bundle-v1.json:1`](../planning-artifacts/v9-authority-bundle-v1.json#L1)

- Graph projection keeps every successor downstream of unrun IR-0.
  [`v9-execution-graph-v1.json:1`](../planning-artifacts/v9-execution-graph-v1.json#L1)

- Supersession projects all obligations and 124/77/52/28 preservation denominators.
  [`v9-supersession-map-v1.json:1`](../planning-artifacts/v9-supersession-map-v1.json#L1)

**Reusable guidance**

- Canonical runbook centralizes evidence invariants, authoring, faults, and limitations.
  [`evidence-boundary-validation.md:8`](../../docs/runbooks/evidence-boundary-validation.md#L8)

- Project overrides bind build and unattended routes to canonical guidance.
  [`bmad-build.toml:3`](../../_bmad/custom/bmad-build.toml#L3)

- Review customization adds standing guidance and an evidence-boundary lens.
  [`bmad-review.toml:3`](../../_bmad/custom/bmad-review.toml#L3)

**Verification**

- Focused faults cover candidate drift, atomic rollback, guidance weakening, and IR-0 neutrality.
  [`test_publish_v9_planning_authority.py:36`](../../_bmad/scripts/tests/test_publish_v9_planning_authority.py#L36)

- Compiled validation checks immutable prefixes, bundle closure, graph topology, and projections.
  [`PlanningAuthorityV9ValidationTest.cs:34`](../../tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs#L34)
