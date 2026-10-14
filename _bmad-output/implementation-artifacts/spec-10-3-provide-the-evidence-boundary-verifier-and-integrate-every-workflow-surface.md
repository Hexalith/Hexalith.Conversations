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

- [x] `_bmad-output/planning-artifacts/story-10.3-current-{binding,contract}-v1.json` — record approved baseline, current inventories and effective commands, retaining all eight AC identities and original provenance.
- [x] `tests/Hexalith.Conversations.TestSupport.Runner/` — add non-packable internal JSON bridge invoking shared helpers; register its project in `Hexalith.Conversations.slnx`.
- [x] `_bmad/scripts/verify_evidence_boundary.py` and `_bmad/scripts/evidence_boundary_current.py` — add schema-valid scoped verifier and stable registry, inspecting changed/new evidence readers; preserve historical calls.
- [x] `.agents/skills/` and `.claude/skills/` — mirror gates in build `step-04-review`, `step-05-present`, `step-oneshot`, build-auto `step-04-review`, and code-review `steps/step-04-present`. Place before governed transitions; preserve promotion checks where applicable and restore current Story 7.3 completion blocks.
- [x] `_bmad/custom/bmad-{build,build-auto,review}.toml` and `docs/runbooks/current-change-validation.md` — bind approved scoped guidance and unchanged routine behavior.
- [x] `_bmad/scripts/tests/test_verify_evidence_boundary.py` and `tests/tooling/test_evidence_boundary_current.py` — implement five contracted selectors and bridge/current regressions: stable codes, parity, anti-vacuity, transitions, gate/retired-route/guidance faults. Mutations restore bytes; historical suites stay outside routine CI.
- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and `_bmad/scripts/tests/test_generate_story_record.py` — add closed 10.3 facts, receipt validation and candidate-aware pair/inserted verification; prove unchanged 10.1/10.2 consumption.
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

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B01 | high | patch | Bridge assertion counts permit replacing a meaningful BoundaryAssertions.True predicate and identity with unconditional success; preserve identities and reject vacuity. |
| B02 | high | patch | Only an exact forward-slash literal identifies readers; Windows separators and Path.Combine literals bypass inspection. |
| B03 | high | patch | The verifier passes candidate-authored rootConstants directly to the bridge; independently derive the permitted source and predecessor pins from approved provenance. |
| B04 | high | patch | Contained CLI outputs can collide with authority, binding, evidence, or tracked source inputs and still report success. |
| B05 | high | patch | Source consumption checks receipt hashes but does not rederive scenario or mutation ledgers, allowing fabricated record assertions with original receipts. |
| B06 | medium | patch | The unconditional numbered two-file publication instruction conflicts with the preceding Story 10.3 nine-file requirement; make publication and withdrawal instructions consistent. |
| B07 | medium | patch | Post-done verification appears after Stop/HALT or success reporting in installed completion routes; move it before terminal instructions. |
| B08 | medium | patch | Every failed dotnet build becomes HELPER_UNAVAILABLE, including compilation defects that require HELPER_FAILURE and FAIL/1. |
| B09 | medium | patch | Bridge envelope validation discards the registered HELPER_REQUEST_INVALID error when the runner returns a nonzero structured response. |
| B10 | medium | patch | Stable-code fixtures check membership rather than exact code sets; add exact-set assertions and missing registered outcome coverage. |
| E01 | high | patch | The shared declaration lexer makes verbatim/raw literals opaque, so release-evidence readers using those forms escape detection. |
| E02 | high | patch | Literal true in BoundaryAssertions.True preserves token counts and is accepted; same root cause as B01. |
| E03 | medium | patch | Unsuccessful reader blob reads become empty source without a SKIPPED unavailable-history assertion; confirmed additions/deletions need distinct handling. |
| E04 | high | patch | The bridge resolves HEAD then reuses the mutable revision expression for later diff/blob calls; pin canonical revisions through all dependent requests. |
| E05 | high | patch | CLI output containment permits protected-input collisions; same root cause as B04. |
| E06 | medium | patch | FileNotFoundError from missing dotnet is caught as AUTHORITY_UNAVAILABLE; translate missing tooling to HELPER_UNAVAILABLE inside bridge. |
| E07 | high | patch | Receipt hashes do not establish that record ledgers equal retained receipt contents; same root cause as B05. |
| E08 | medium | patch | A newline in a legal Git filename causes finalization to retry an invalid result and raise ValidationError; reproduced locally, requiring a closed structured failure. |
| V01 | medium | patch | Pre-verified regression gap: routine tests call current.main and cannot detect removal of the public wrapper dispatch; add a subprocess CLI check. |
| V02 | medium | patch | Pre-verified regression gap: generation tests stop before real JUnit parsing; add valid generation then a wrong-candidate stamp rejection. |
| V03 | high | patch | The verification reviewer independently confirmed vacuous shared helper assertions pass; same root cause as B01. |
| P01 | medium | patch | Typed new() is recognized but var x = new GitFacts() is not, because new precedes the type; add the ordinary construction form and a positive regression. |
| P02 | low | patch | The generated Markdown says no fault-injection result is bound although the Story 10.3 facts bind 17 mutations; render those facts accurately without changing prior story projections. |

Review patches are implemented and independently checked. Focused verification passed 91 current CLI/reader tests, 44 generator/schema/route tests, and six affected historical neutral-helper tests. All 23 triage rows above are preserved.

The full Python lane at committed source `02672d4ddd3c9bc54eed3f9f14c155c41f3834a2` passed 1,682 tests and 55 subtests in an isolated checkout. Its two environment failures were `ReleaseToolingTests::test_msbuild_dependency_mode_matrix_is_evaluated` (missing root build configuration) and `ReleaseToolingTests::test_semantic_release_planning_in_ci_preserves_repository_refs` (missing pinned Node dependencies). Both pass on rerun in the configured workspace; all 1,684 tests are covered by those runs.

Publication is blocked because another session's `/pushall` committed the reviewed fixes with root submodule updates in `02672d4ddd3c9bc54eed3f9f14c155c41f3834a2`. The scoped verifier command with approved baseline `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`, candidate `HEAD`, current binding, original authority bundle, `--verify-workflows`, and `--format json` exits `1` with `CHANGED_PATH_SET_MISMATCH` and `EVIDENCE_BOUNDARY_GITLINK`: 34 expected paths, 44 actual paths, and nine changed gitlinks. All external commits and pointers are preserved. The stale pair, seven receipts, and inserted region were withdrawn in `071d3e298e3ea13130c8b0d3124cb6cf4eb05558`.

Owner approval is pending to use `02672d4ddd3c9bc54eed3f9f14c155c41f3834a2` as the frozen baseline and adapt the current binding/schema for its 12 root gitlinks. No replacement acceptance publication or done transition is claimed. The publication task remains open and status returned to `in-progress` while this approval is pending.

## Verification

- Run all eight effective contract commands, focused bridge/generator/schema/render tests, both Story 7.3 workflow scenarios, and the current Python lane.
- Entry probes: original AC-10.3-01 exits 2 (required `--baseline`); `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k stable_code_contract -p no:cacheprovider` exits 5 (139 deselected). Read-only Story 7.3 `evaluate` returns `WORKFLOW_INTEGRATION_MISSING` for all eleven surfaces. Story 10.2 predecessor pair verification passes.
- The first uninterrupted current Python lane measured 1627 passing tests and 55 passing subtests, with one failure in `test_python_tooling_lane_has_no_ambient_skip_constructs`: the pre-existing Windows symlink-privilege `pytest.skip` in `test_story101_artifact_linked_parent_is_rejected`. The symlink test now requires its containment assertion to execute, and the anti-skip guard is retained. A fresh lane on the final source candidate supplies the final pass evidence.
- Earlier focused runs exposed and corrected `test_v2_schema_contract_hold_metaschema_and_identities` (stale exact regex expectation, replaced with valid/invalid value checks), `test_v2_every_code_is_documented_in_the_runbook` (inherited codes missing from the runbook), and `test_every_gate_route_reruns_inserted_record_verification_after_its_done_commit` (missing post-done verification clauses). The canonical blocker branch required by `test_v2_blocker_prevents_state_transition_on_every_surface` is restored across all eleven completion surfaces.
