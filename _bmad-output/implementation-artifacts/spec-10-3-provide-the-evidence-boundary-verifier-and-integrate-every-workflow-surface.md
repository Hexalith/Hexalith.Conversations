---
title: 'Story 10.3: Evidence verifier and workflow enforcement'
type: 'feature'
created: '2026-10-14'
status: 'in-progress'
baseline_commit: '7f24b72d57da65745cdecdea5b5e7c0da1313b4d'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-10-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The historical verifier lacks Story 10.3's CLI, result shape, workflow enforcement, and final-record support.

**Approach:** Reuse neutral helpers for verification, integrate the owner-selected workflows, and generate acceptance evidence.

## Boundaries & Constraints

**Always:** Preserve distinct `PASS`/`FAIL`/`BLOCKED`/`not-applicable` states and exits `0`/`1`/`2`/`0`. Use the existing closed result schema, sorted unique code strings, and a nonempty evaluated ledger for applicable results. Recompute hashes, containment, exact changed-path equality, raw-mode gitlinks, and trust roots. Keep the contracted stable codes and zero initial exemptions. Bind the verified Story 10.2 pair, planning provenance, evaluated candidate, workflow/guidance inventories, resolved customization, and seven receipts; final summary is `8/8/0/0/0/0`.

**Never:** Filter gitlinks to manufacture a pass, rewrite historical authority/accepted records, migrate the 27 frozen readers reserved for 10.4, change production APIs or conformance membership, or edit installed customization defaults.

**Owner decision, 2026-10-14:** Use current policy: ten surviving workflow routes, additive effective binding/contract, and entry baseline `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. Keep absent legacy routes/aliases retired and historical bundle bytes as provenance. Gate explicitly required evidence work; preserve routine validation and restore the existing Story 7.3 completion guard.

## I/O & Edge-Case Matrix

| Scenario | Input | Result | Handling |
| --- | --- | --- | --- |
| Valid scope | Evaluated checks / no evidence changes | PASS / distinct not-applicable | Exit 0; non-applicable ledger empty |
| Unavailable | Missing baseline/history/authority | BLOCKED; SKIPPED row | Exit 2 |
| Fault | Integrity, helper, gate, route, guidance or parity defect | FAIL; exact code | Exit 1; block transition |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/verify_evidence_boundary.py` — preserve `verify`; use `_bmad/schemas/evidence-boundary-result-v1.schema.json` for the new route.
- `tests/Hexalith.Conversations.TestSupport/` — reuse `GitFacts`, `RepositoryLocator`, `EvidenceManifest`, `BoundaryAssertions`, and `AssertionLedger` through an internal non-shipping bridge; no Python equivalents.
- `_bmad/scripts/generate_story_record.py` — reuse `v2_verified_predecessor`, receipt parsing, pair verification, rendering, and rederivation.
- `_bmad/scripts/verify_story_completion_workflows.py` — reuse `ROUTES` and `render_in_memory`; restore its existing completion guard when editing these routes.
- `_bmad/scripts/{config_utils,resolve_customization,render_skill}.py` — merge/render APIs.
- `v9/story-contracts/10.3.json` and `v9/inventories/evidence-{workflows,guidance}-v2.json` under planning artifacts — frozen requirements; preserve original bytes.

## Tasks & Acceptance

**Execution:**

- [ ] `_bmad-output/planning-artifacts/story-10.3-current-{binding,contract}-v1.json` — record approved baseline, current inventories and effective commands, retaining all eight AC identities and original provenance.
- [ ] `tests/Hexalith.Conversations.TestSupport.Runner/` — add non-packable internal JSON bridge invoking shared helpers; register its project in `Hexalith.Conversations.slnx`.
- [ ] `_bmad/scripts/verify_evidence_boundary.py` and `_bmad/scripts/evidence_boundary_current.py` — add schema-valid scoped verifier and stable registry, inspecting changed/new evidence readers; preserve historical calls.
- [ ] `.agents/skills/` and `.claude/skills/` — mirror gates in build `step-04-review`, `step-05-present`, `step-oneshot`, build-auto `step-04-review`, and code-review `steps/step-04-present`. Place before governed transitions; preserve promotion checks where applicable and restore current Story 7.3 completion blocks.
- [ ] `_bmad/custom/bmad-{build,build-auto,review}.toml` and `docs/runbooks/current-change-validation.md` — bind approved scoped guidance and unchanged routine behavior.
- [ ] `_bmad/scripts/tests/test_verify_evidence_boundary.py` and `tests/tooling/test_evidence_boundary_current.py` — implement five contracted selectors and bridge/current regressions: stable codes, parity, anti-vacuity, transitions, gate/retired-route/guidance faults. Mutations restore bytes; historical suites stay outside routine CI.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and `_bmad/scripts/tests/test_generate_story_record.py` — add closed 10.3 facts, receipt validation and candidate-aware pair/inserted verification; prove unchanged 10.1/10.2 consumption.
- [ ] `docs/release-evidence/story-10.3-final-record-v2.{json,md}` — generate from committed sources and AC-01..07, verify insertion, and retain outputs.

**Acceptance Criteria:**

- Given approved bindings and a committed candidate, when effective AC-10.3-01..08 run, then all satisfy their contract and the generated summary is `8/8/0/0/0/0`.
- Given each required fault, when verification runs, then its exact code occurs and fixture bytes restore.
- Given FAIL/BLOCKED versus valid not-applicable, when a governed transition runs, then failure halts and non-applicability continues explicitly recorded.
- Given installed twins and in-memory renders, when parity and Story 7.3 checks run, then all pass; ordinary current-policy work remains valid.

## Implementation Notes

- Added a non-shipping JSON bridge over the neutral TestSupport helpers, including committed Git facts, exact diffs, raw gitlinks, manifest/hash/path checks, literal roots, assertion ledgers, and changed-reader declaration checks.
- Added the current-policy binding and effective eight-AC contract; mirrored the scoped gate across ten surviving routes, restored Story 7.3 completion and post-done verification guards, and bound current project-owned guidance without changing installed defaults.
- Added closed candidate-bound Story 10.3 generator facts, seven retained receipt paths, pair/inserted verification, and a checked publication scope that keeps actual HEAD diffs and gitlinks visible after record publication.
- Updated current CI to install the pinned SDK and exercise the package-free bridge without initializing submodules. Focused regressions cover helper faults, reader spoofing/strength loss, workflow faults/parity, non-vacuity, lifecycle result/exit mismatches, and post-publication source/gitlink drift.
- Pinned six historical provenance files independently to approved-baseline Git blobs, checked the baseline before non-applicability, and limited working-spec changes during generation/insertion verification to lifecycle-owned fields, task checkboxes, and the exact final-record region.

## Spec Change Log

- 2026-10-14 — The owner explicitly authorized “Use entry commit 7f24b72 as the approved baseline (recommended)”, identifying full revision `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. This replaces `0056c9f0335094e7f5b253aba7ec257bd68b0b4f`, whose diff already contained the EventStore and Folders gitlink updates in the specification commit. The frozen Owner decision and frontmatter baseline are amended only for this authorization. All pointers, accepted records, original contracts, and historical authority bytes are preserved.

## Review Triage Log

## Verification

- Run all eight effective contract commands, focused bridge/generator/schema/render tests, both Story 7.3 workflow scenarios, and the current Python lane.
- Entry probes: original AC-10.3-01 exits 2 (required `--baseline`); `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k stable_code_contract -p no:cacheprovider` exits 5 (139 deselected). Read-only Story 7.3 `evaluate` returns `WORKFLOW_INTEGRATION_MISSING` for all eleven surfaces. Story 10.2 predecessor pair verification passes.
- The first uninterrupted current Python lane measured 1627 passing tests and 55 passing subtests, with one failure in `test_python_tooling_lane_has_no_ambient_skip_constructs`: the pre-existing Windows symlink-privilege `pytest.skip` in `test_story101_artifact_linked_parent_is_rejected`. The symlink test now requires its containment assertion to execute, and the anti-skip guard is retained. A fresh lane on the final source candidate supplies the final pass evidence.
- Earlier focused runs exposed and corrected `test_v2_schema_contract_hold_metaschema_and_identities` (stale exact regex expectation, replaced with valid/invalid value checks), `test_v2_every_code_is_documented_in_the_runbook` (inherited codes missing from the runbook), and `test_every_gate_route_reruns_inserted_record_verification_after_its_done_commit` (missing post-done verification clauses). The canonical blocker branch required by `test_v2_blocker_prevents_state_transition_on_every_surface` is restored across all eleven completion surfaces.
