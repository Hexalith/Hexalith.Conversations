---
title: 'Story 10.3: Evidence verifier and workflow enforcement'
type: 'feature'
created: '2026-10-14'
status: 'in-progress'
baseline_commit: '3319e062472bb44fda8389a2420af108da58da0a'
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

**Owner decision, 2026-10-15:** Use current policy: ten surviving workflow routes, additive effective binding/contract, and entry baseline `3319e062472bb44fda8389a2420af108da58da0a`. Keep absent legacy routes/aliases retired and historical bundle bytes as provenance. Gate explicitly required evidence work; preserve routine validation and restore the existing Story 7.3 completion guard.

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
- Applied the 2026-10-15 approved scope refresh at `3319e062472bb44fda8389a2420af108da58da0a`, retaining the reviewed source inventory at `10f395a690b3f581771891e5f69fc5858a5d1d12` and original provenance at `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. The binding independently pins all nine prior publication blobs and requires their exact source-candidate deletion. Current workflow commands and inventory hashes use the refreshed baseline; the schema retains the earlier record shape. Fresh verifier receipts can be written only to their two contracted, proven-withdrawn output paths; published inputs remain protected.
- Applied the approved 2026-10-15 scope refresh with separate source-inventory and historical-provenance trust anchors. The nine-file withdrawal is independently pinned to approved-entry blobs and must consist of exact raw deletions at the source candidate; only its two contracted verifier JSON receipts may be recreated before publication. Existing tracked publication outputs remain protected.

## Spec Change Log

- 2026-10-15 — The owner replied "I approve" to "Approve refreshing the baseline to `3319e062472bb44fda8389a2420af108da58da0a` and regenerating evidence while preserving current pins?" The exact approved revision is confirmed from Git and remains current HEAD. Replace the previous scope baseline `10f395a690b3f581771891e5f69fc5858a5d1d12` in the frozen decision and frontmatter only under this authorization. Preserve all twelve current root pointers, including the four differences documented below, the independently approved thirty-four source paths, and historical provenance at `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. Keep the source inventory pinned independently of the refreshed post-entry diff; the previous baseline's approved inventory remains its trust anchor. Adapt current bindings, schema, commands, route hashes and focused fixtures, then regenerate candidate-bound receipts and the Story 10.3 pair using the existing withdrawal/publication lifecycle. Earlier records remain preserved in Git. This approval supersedes the parked R01 and pending-refresh notes only as authorization to resume; fresh verification still gates review and completion.

- 2026-10-14 — The owner explicitly authorized “Use entry commit 7f24b72 as the approved baseline (recommended)”, identifying full revision `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. This replaces `0056c9f0335094e7f5b253aba7ec257bd68b0b4f`, whose diff already contained the EventStore and Folders gitlink updates in the specification commit. The frozen Owner decision and frontmatter baseline are amended only for this authorization. All pointers, accepted records, original contracts, and historical authority bytes are preserved.

- 2026-10-14 — The owner replied “I approve” to the request to refresh the frozen baseline and adapt the current binding/schema for the twelve committed root gitlinks. That request named `02672d4ddd3c9bc54eed3f9f14c155c41f3834a2`. While approval was pending, the workspace advanced to clean entry `10f395a690b3f581771891e5f69fc5858a5d1d12`, changing only four additional submodule pointers after the blocked-publication metadata commit. The approved refresh is applied to that current entry, preserving all committed pointers. The current binding will separately name all 34 reviewed source files and the exact post-entry changed-path set; historical provenance remains pinned to `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. The Story 10.3-only final-record candidate shape will admit its twelve-root snapshot; accepted historical shapes and records remain unchanged.

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
| R01 | high | approved refresh implemented, publication pending, 2026-10-15 | Code review 2026-10-14 (baseline `10f395a`..HEAD `3ab1c58`, chunk 1). Live `verify_evidence_boundary.py --verify-workflows --candidate HEAD` returns FAIL with EVIDENCE_BOUNDARY_GITLINK and CHANGED_PATH_SET_MISMATCH: `90ab316` (/pushall) moved EventStore `6394d05`->`210b90d`, FrontComposer `0e151a1`->`e0476b7`, Memories `e864054`->`379cb1a`; the scoped gate would block the done transition. Restore the pins or obtain an approved refresh. (blind-hunter+edge-case-hunter) The owner initially parked this finding and subsequently approved the exact 2026-10-15 baseline refresh recorded above, preserving all current pins. Fresh candidate-bound publication and final verification remain required. |
| R02 | medium | patched, 2026-10-15 | Four invalid binding variants exercise `main()` → `verify()` with an otherwise ordinary changed path: omitted source, unreviewed source, unreviewed diff, and duplicate diff. Each asserts FAIL/1, exact AUTHORITY_BINDING_INVALID, the failed `binding:source-paths` row, continued neutral evaluation, and no not-applicable warning. (verification-gap) |
| R03 | medium | patched, 2026-10-15 | Parametrized omitted-source and unreviewed-diff cases assert AUTHORITY_BINDING_INVALID during generation and the approved-entry inventory error from `v2_10_3_source_errors` against a committed invalid binding whose candidate and source digests remain internally consistent. (verification-gap) |
| R04 | low | rejected after audit, 2026-10-15 | Retained receipts rederive exactly the accepted scenario and mutation ledgers: 18 mutations, 18 restored, every expected blocker present, and identical Markdown projection. The ten bridge cases mutate request facts and leave evidence bytes unchanged; the four extra observed-code sets are the expected reader-strength/parity consequences of those faults. The separate 17-mutation synthetic rendering fixture does not supply the retained acceptance count. (blind-hunter) |
| R05 | false | rejected | Crash on malformed binding: `verify()` catches RuntimeError/KeyError/TypeError and maps to AUTHORITY_BINDING_INVALID/HELPER_UNAVAILABLE; generator paths hit the catch-all at ~9301/9380. Schema `if` is keyed on `storyId` const `10.3`. Sprint exception accepts any story-row status change, so `review`->`done` is allowed. |
| R06 | low | rejected | AC-01/AC-03 identical output (files byte-identical: docs/ paths make both applicable); hard-coded 34; baseline hash replicated in routes; no old-baseline schema test; test-fixture rewiring; valid_sprint without HEAD (still fails closed). Fixes add complexity or edit the spec; unlikely to hurt. Spec-prose, frozen-block, /tmp evidence and approval-commit notes rejected: fix is a spec edit. |

Review patches are implemented and independently checked. Focused verification passed 91 current CLI/reader tests, 44 generator/schema/route tests, and six affected historical neutral-helper tests. All 23 triage rows above are preserved.

The full Python lane at committed source `02672d4ddd3c9bc54eed3f9f14c155c41f3834a2` passed 1,682 tests and 55 subtests in an isolated checkout. Its two environment failures were `ReleaseToolingTests::test_msbuild_dependency_mode_matrix_is_evaluated` (missing root build configuration) and `ReleaseToolingTests::test_semantic_release_planning_in_ci_preserves_repository_refs` (missing pinned Node dependencies). Both pass on rerun in the configured workspace; all 1,684 tests are covered by those runs.

Historical blocked-publication finding: publication was blocked because another session's `/pushall` committed the reviewed fixes with root submodule updates in `02672d4ddd3c9bc54eed3f9f14c155c41f3834a2`. The scoped verifier command with approved baseline `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`, candidate `HEAD`, current binding, original authority bundle, `--verify-workflows`, and `--format json` exits `1` with `CHANGED_PATH_SET_MISMATCH` and `EVIDENCE_BOUNDARY_GITLINK`: 34 expected paths, 44 actual paths, and nine changed gitlinks. All external commits and pointers are preserved. The stale pair, seven receipts, and inserted region were withdrawn in `071d3e298e3ea13130c8b0d3124cb6cf4eb05558`.

Historical pending-approval note: owner approval was pending to use `02672d4ddd3c9bc54eed3f9f14c155c41f3834a2` as the frozen baseline and adapt the current binding/schema for its 12 root gitlinks. No replacement acceptance publication or done transition is claimed. The publication task remains open and status returned to `in-progress` while this approval is pending.

Final verification, 2026-10-14: the approved refresh is complete at evaluated source `5c315a24ca0187ba2b79642e8db081cbab11fb52`. The configured pinned Python lane passed 1,701 tests and 55 subtests with zero failures, errors, or skips; exact command/JUnit evidence is retained in `/tmp/story103-approved-full-5c315a24ca0187ba2b79642e8db081cbab11fb52/`. The first adapted-source lane found one fixture-only empty-commit error; the fixture now reuses an unchanged committed candidate, its focused and clean post-commit regressions pass, and the complete lane was rerun successfully. All eight effective acceptance commands exited `0`, producing summary `8/8/0/0/0/0`, seven candidate-bound receipts, 18 restored mutations, ten current routes, 34 source hashes, and twelve root gitlinks. The exact nine-file publication is `90b8f1c87207acc612d5b2b35096223b1eed3852`; its pair and inserted Markdown verify, and its full HEAD boundary returns PASS with zero gitlink changes. All 23 review findings are patched; nothing is deferred. The earlier pending-publication notes describe the retained pre-publication history. The spec is complete; the build workflow moves its sprint row to `review`.

Post-done verification, 2026-10-14: `uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/story-10.3-current-contract-v1.json --verify-inserted-record _bmad-output/implementation-artifacts/spec-10-3-provide-the-evidence-boundary-verifier-and-integrate-every-workflow-surface.md` exited `1` with `WORKTREE_NOT_CLEAN`, subject `sprint-status.yaml`. The generator rechecks all 34 sources but permits lifecycle-only working changes only for the spec, so its accepted committed sprint-row transition is incorrectly rejected. This supersedes the preceding completion claim: the spec and sprint row return to `in-progress`, the publication task is reopened, and the nine-file publication and inserted region will be withdrawn before the source fix.

Final accepted verification, 2026-10-14: source `e0dd827279439921316691af3bb2a2ccaac0243e` passes the complete configured pinned Python lane: 1704 tests and 55 subtests, with zero failures, errors, or skips. Exact command/log/JUnit evidence is retained in `/tmp/story103-approved-full-e0dd827279439921316691af3bb2a2ccaac0243e`. The committed sprint lifecycle rederivation fix is covered by real publication/completion regressions: the owned committed row and timestamps pass; unrelated row changes, uncommitted sprint changes, and first-generation dirt reject; receipt and sprint reads share one captured canonical consumer revision. All eight effective acceptance commands exited `0`; the closed generated record binds summary `8/8/0/0/0/0`, seven receipts, eighteen restored mutations, ten routes, thirty-four source hashes, and twelve root gitlinks. The exact nine-file publication is `3541cd0b91cc29b3bbec020a60abb87585d1f410`. Its generated pair and verbatim inserted Markdown verify. All twenty-three review findings are patched, nothing is deferred, historical evidence bytes and every entry root pointer are preserved. This completes and supersedes the earlier attempted-publication and blocker notes. The spec is done; the sprint row is `review` as required by the build workflow.

## Verification

- Run all eight effective contract commands, focused bridge/generator/schema/render tests, both Story 7.3 workflow scenarios, and the current Python lane.
- Entry probes: original AC-10.3-01 exits 2 (required `--baseline`); `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k stable_code_contract -p no:cacheprovider` exits 5 (139 deselected). Read-only Story 7.3 `evaluate` returns `WORKFLOW_INTEGRATION_MISSING` for all eleven surfaces. Story 10.2 predecessor pair verification passes.
- The first uninterrupted current Python lane measured 1627 passing tests and 55 passing subtests, with one failure in `test_python_tooling_lane_has_no_ambient_skip_constructs`: the pre-existing Windows symlink-privilege `pytest.skip` in `test_story101_artifact_linked_parent_is_rejected`. The symlink test now requires its containment assertion to execute, and the anti-skip guard is retained. A fresh lane on the final source candidate supplies the final pass evidence.
- Earlier focused runs exposed and corrected `test_v2_schema_contract_hold_metaschema_and_identities` (stale exact regex expectation, replaced with valid/invalid value checks), `test_v2_every_code_is_documented_in_the_runbook` (inherited codes missing from the runbook), and `test_every_gate_route_reruns_inserted_record_verification_after_its_done_commit` (missing post-done verification clauses). The canonical blocker branch required by `test_v2_blocker_prevents_state_transition_on_every_surface` is restored across all eleven completion surfaces.

The owner approved the baseline refresh and Story 10.3-only twelve-root schema adaptation, as recorded in the Spec Change Log. The applied current scope baseline is `10f395a690b3f581771891e5f69fc5858a5d1d12`; the 34 reviewed source paths are retained independently of the exact post-entry diff, and historical provenance remains pinned to `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. Status is `in-review` for resumed independent verification. The publication task remains unchecked; fresh acceptance execution, publication, and inserted verification are pending.

The attempted publication `90b8f1c87207acc612d5b2b35096223b1eed3852` and completion claim remain superseded by the post-done failure above. Withdrawal `297f9dfc294cb49eebf2f9258d5e448d01f6b30d` removed its exact nine files and inserted region before the source fix. Renewed verification starts from `in-review` with the final publication task unchecked. Story 10.3 inserted rederivation now permits only a committed consumer-HEAD sprint change limited to its owned story row and update dates; initial generation and uncommitted sprint changes remain strict. Fresh acceptance, publication, insertion and done verification are pending.

Resumed verification, 2026-10-15: R02 and R03 are patched; the focused command `uv run --frozen --no-sync python3 -m pytest -q -p no:cacheprovider tests/tooling/test_evidence_boundary_current.py _bmad/scripts/tests/test_generate_story_record.py -k 'binding_inventory_and_source_scope_faults_fail_closed or generation_and_source_rederivation_reject_invalid_binding_scope'` passes all ten selected cases. The R04 receipt audit rederives the retained record's scenario ledger, seven receipt hashes, eighteen mutation rows, and Markdown projection without differences. This evidence checks the retained candidate `e0dd827279439921316691af3bb2a2ccaac0243e`; it does not establish acceptance for the current candidate.

Affected validation completed: all 101 current-verifier cases pass. The combined current-verifier/Story 10.3 run initially reported six generator fixture failures because the fixtures copied an already-published live spec, appended a duplicate record region, and assumed live `in-review` status. Disposable source specs now explicitly begin unpublished and `in-review` with pending tasks; production guards and the real spec's frozen and inserted bytes remain intact. The final command `uv run --frozen --no-sync python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests/test_generate_story_record.py -k story103 --junitxml=/tmp/story103-generator-regressions.xml` passes all 40 Story 10.3 cases, with 1122 older-story cases deselected. An earlier full two-file run was stopped by request after 451 passes and is incomplete; it supplies no complete-lane claim. Logs and JUnit are retained under `/tmp/story103-{current,generator,resumed}-regressions.*`; receipt and publication audits are `/tmp/story103-retained-mutation-audit.json` and `/tmp/story103-publication-diagnostics.json`.

The entry scoped probe at `b45544b9d04285842fc668fc4d484ee0e32f9123` exits `1` with `AUTHORITY_BINDING_INVALID`, `CHANGED_PATH_SET_MISMATCH`, and `EVIDENCE_BOUNDARY_GITLINK`. The authority failure is the `publication:verified` assertion, subject `10.3`; both `binding:baseline` and `binding:source-paths` pass. Independent committed-record checks at consumer `3319e062472bb44fda8389a2420af108da58da0a` return no source errors and the descendant error `post-source history or raw gitlinks do not pass: EVIDENCE_BOUNDARY_GITLINK`. HEAD advanced externally during this resumed work, adding the Platform pointer change. The approved baseline remains `10f395a690b3f581771891e5f69fc5858a5d1d12`; all four changed root pointers are preserved:

| Root path | Approved-baseline commit | Current commit |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `6394d0500e6e277b8975cb397c9f0389f9a16929` | `210b90da172cc7ca946fbb06cfd44390bffa707b` |
| `references/Hexalith.FrontComposer` | `0e151a139d178c1f5d90df50ee3d066ce4d4f35a` | `e0476b71c7623840ba2ab0c276e43800a51fd44a` |
| `references/Hexalith.Memories` | `e864054d518021ba4a7870f42c07e9101178dd2f` | `379cb1a2f031f63c4a7148bbf32eb9d4d917dd6a` |
| `references/Hexalith.Platform` | `4b9cb5624159de0782dd3329cdcc3b2d66010509` | `55bee680e6901a45946dd8bf1e4a4060952bd7ea` |

Before renewed approval, the status remained `in-progress` and the final publication task was reopened. R01 remained parked under the owner's preserve-pins decision. A fresh passing acceptance record and review/done transition required an approved boundary refresh or pin decision; that earlier handoff claimed no baseline refresh, pin change, regenerated acceptance publication, or completion. The existing inserted record and accepted output pair were retained byte-for-byte.

Approved refresh implementation, 2026-10-15: the current binding and ten workflow routes now use scope baseline `3319e062472bb44fda8389a2420af108da58da0a`. Source membership remains pinned independently to `10f395a690b3f581771891e5f69fc5858a5d1d12`, and original provenance remains pinned to `7f24b72d57da65745cdecdea5b5e7c0da1313b4d`. The exact planned source boundary contains eighteen reviewed changed files and nine publication deletions, without filtering any raw path or gitlink. Each withdrawal digest is rederived from the new approved entry; partial deletion, edits, renames, mode changes, and forged withdrawal bindings reject. Committed-record consumption independently rechecks those deletions. The closed Story 10.3 schema additively admits the new baseline while retaining the previous baseline shape.

The affected current-verifier/Story 10.3 run passes 153 cases with 1122 older-story cases deselected (`/tmp/story103-refresh-regressions.{log,xml}`). The focused receipt-output and withdrawal-consumption run passes nineteen cases (`/tmp/story103-refresh-focused-output.{log,xml}`), including real disposable committed-source verification, receipt creation, forged withdrawal rejection, and protection of tracked receipts and pair outputs. The original nine publication files still equal their approved-entry Git blobs, and all twelve root pointers remain untouched. The parent workflow owns the authorized withdrawal, source commit, seven fresh receipts, nine-file publication, insertion, and final acceptance verification; the publication task and story remain open until those steps pass.
