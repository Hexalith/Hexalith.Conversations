# Sprint Change Proposal - Mechanize Epic 5 Final-Record Verification

**Date:** 2026-07-14
**Project:** Conversations
**Requested by:** Jerome
**Workflow:** bmad-correct-course
**Mode:** Batch
**Status:** Approved; implementation in progress

## 1. Issue Summary

Epic 4 retrospective action A1 requires a mechanical final-record check for Epic 5 story completion. The check must verify final test counts, story File Lists, changed documentation and evidence, and public-contract-shape diff state from the final working tree.

Epic 5 delivered the required product and release evidence, but its three story reviews each corrected final-record drift:

- Story 5.1 review corrected timestamp consistency, File List coverage, and contract-diff validation strength. Its final conformance count was 365 / 365.
- Story 5.2 review corrected the exact removed-test path and count reconciliation. Its final conformance count was 374 / 374, while a modified `test-summary.md` was not represented consistently in the story File List.
- Story 5.3 review corrected source-hash, manifest-containment, signable-payload, changed-file, inventory-row, and accidental root-submodule gitlink issues. Its final conformance count was 384 / 384.

The existing validation tests prove the content of individual Epic 5 evidence artifacts. Story 5.3 also validates its own committed evidence boundary. No reusable gate currently derives all four required dimensions from the live final working tree and compares them with the completion record before review.

The repository currently has pre-existing working-tree changes at the `references/Hexalith.FrontComposer`, `references/Hexalith.Memories`, and `references/Hexalith.Tenants` root gitlinks. A correct gate therefore cannot equate completion with a globally clean tree. It must freeze the pre-existing path, status, and gitlink-object state before work, exclude only an unchanged frozen baseline, and fail on any new or altered out-of-scope state.

This is a completion-integrity limitation, not a product requirement or architecture change.

## 2. Impact Analysis

### Epic and Story Impact

- Epic 5 remains done. Its FR-20 deliverable and release-owner decision are not reopened by adding a workflow control after completion.
- Stories 5.1, 5.2, and 5.3 remain done. Their historical Dev Agent Records and accepted release evidence are not silently rewritten.
- The existing Epic 4 retrospective action remains the implementation tracker. No Story 5.4 or new epic is required.
- The check gains two explicit modes:
  - **Live completion mode:** derives observed state from the current final working tree and is mandatory before a future evidence story moves from review to done.
  - **Historical audit mode:** evaluates the committed Story 5.1-5.3 records at their recorded baseline/final commit boundaries without pretending that today's working tree is their former uncommitted final tree.
- If the historical audit finds a real mismatch, the mismatch is recorded as an amendment or separate corrective decision. Hash-bound or signed release evidence is not edited in place.

### Artifact Impact

- **PRD:** No change. Existing FR-20 and evidence-boundary requirements already require exact, auditable release records.
- **Architecture:** No change. The proposal strengthens the existing release-evidence and public-contract invariants without changing runtime structure.
- **UX:** No change. There is no user-facing surface or workflow impact.
- **Epics:** No scope or acceptance-criteria change. A short implementation-resolution reference may be appended to the Epic 5 retrospective; the historical retrospective result remains identifiable.
- **Sprint status:** Change the existing Epic 4 Dev-workflow action from `in-progress` to `done` only after the checker and Epic 5 audit pass, or after any detected discrepancy has an approved amendment.
- **Developer/test guidance:** Add the final-record command, ordering rule, baseline-dirt handling, and failure semantics to `tests/README.md`.
- **Implementation test tooling:** Add a reusable final-record checker and a non-mutating full public-contract-shape comparison.
- **Implementation evidence:** Add a machine-readable and human-readable Epic 5 final-record audit under `_bmad-output/implementation-artifacts/tests/`.
- **Release evidence:** Existing files under `docs/release-evidence/`, including the signed Story 5.3 evidence and release-owner decision, remain byte-identical unless the new check exposes a separately approved correction.
- **Submodules:** No gitlink or nested-submodule change is authorized.

### Technical Impact

The checker is repository workflow tooling. It does not change application runtime behavior, public contracts, package versions, AppHost topology, generated product output, or sibling submodule source.

The principal implementation risks are false confidence from parsing prose, circular test-count updates, and accidental classification of pre-existing working-tree state as story work. The proposed contract mitigates them by using a machine-readable record, binding a final test run to the executable/test inputs it exercised, deriving path inventories directly from Git, and comparing a regenerated public-contract shape without overwriting the immutable baseline.

## 3. Recommended Approach

**Selected path:** Direct Adjustment.
**Scope:** Minor course correction; no backlog or product-scope reorganization.
**Implementation effort:** Medium, estimated at one to two developer days including adversarial fixtures and the Epic 5 audit.
**Risk:** Low to product behavior; medium to evidence workflow until parsing and dirty-tree cases are covered.

Implement a reusable checker with live and historical modes, add a non-mutating full public-contract-shape comparator, document the completion sequence, and issue a consolidated Epic 5 audit report. Keep existing signed evidence immutable and close the sprint action only from a passing final run or an approved discrepancy amendment.

Rollback is not justified because the completed Epic 5 implementation is valid and no product change is being reversed. MVP/PRD review is not justified because the correction adds enforcement to requirements that already exist.

## 4. Detailed Change Proposals

### 4.1 Reusable Final-Record Checker

Proposed artifact: `tests/Test-StoryFinalRecord.ps1`

OLD:

```text
Epic 5 evidence validators check individual artifact contents. Story completion still relies on reviewers to reconcile the final test output, story File List, live Git changes, changed evidence, and public-contract-shape state across separate records.
```

NEW:

```text
A single command exits non-zero unless every required final-record dimension agrees.

Live completion mode accepts:
- the story/work-item record,
- a baseline commit,
- a frozen pre-existing working-tree-state file,
- the canonical final validation command or normalized result file,
- the immutable public-contract-shape baseline, and
- any explicitly approved contract-change reference.

Historical audit mode accepts:
- the story record,
- its recorded baseline and final commit boundaries, and
- its committed result/evidence artifacts.

Both modes emit a versioned JSON result. A Markdown report is rendered from that JSON and is not an independent source of truth.
```

The machine-readable result must contain at least:

- schema version, story/work-item identifier, mode, baseline commit, evaluated tree/commit, and result;
- exact test command/runner, passed, failed, skipped, total, exit code, and executable/test-input fingerprint;
- declared File List, observed changed paths, missing paths, unexpected paths, and path-status values;
- changed documentation/evidence paths, expected evidence paths, hashes, containment results, and JSON/Markdown pairing where required;
- frozen pre-existing dirty paths with status and gitlink object IDs, plus new or changed out-of-scope paths;
- public-contract baseline path/hash, regenerated shape type count/hash, diff state, differences, and approval reference when non-empty;
- actionable failure messages for every mismatch.

The checker must not make a global-clean-tree assumption. In live mode, the observed final inventory is derived mechanically from:

- tracked, staged, and committed changes relative to the work-item baseline;
- untracked non-ignored paths; and
- raw file modes so root gitlinks (`160000`) cannot be hidden by ordinary name-only output.

A pre-existing dirty entry is excluded only when its final path, status, and relevant object/working-tree identity exactly match the frozen start-state entry. Wildcard exclusions and path-only submodule exemptions are forbidden.

### 4.2 Final Test-Count Binding

OLD:

```text
Final counts are copied into story prose and evidence after a test run. Individual validators compare selected recorded values, but no common completion command proves that the final result and all count-bearing records describe the same run.
```

NEW:

```text
The final-record command invokes or consumes the canonical final validation run, normalizes its output, and compares passed/failed/skipped/total values with every count-bearing story, JSON, Markdown, and changed test-summary record.

The result is bound to a fingerprint of the executable, test, project, and dependency inputs exercised by the run. After capture, any change to those inputs makes the result stale. Story prose and final-record artifacts may then be finalized, but executable/test-input drift requires a new run.

The existing xUnit v3 executable fallback documented in tests/README.md remains valid when VSTest socket creation is blocked; both runners normalize into the same result schema.
```

This ordering prevents the checker or its tests from being added after the count recorded as "final."

### 4.3 File List and Changed Documentation/Evidence Closure

OLD:

```text
Story File Lists and evidence manifests are reviewed manually. A changed file can be absent from the File List, and a listed file can be absent from the actual final tree, until review identifies the drift.
```

NEW:

```text
The normalized repo-relative path set in the completion record must equal the mechanically observed work-item path set after exact frozen-baseline exclusions.

Every changed path under docs/ and _bmad-output/ that is part of the work item must be declared. Required evidence pairs, authoritative JSON sources, Markdown renderings, source hashes, manifest containment, and approval references are checked from the final bytes.

Missing, unexpected, duplicate, absolute, escaping, ignored-but-required, or case-mismatched paths fail the check. An out-of-scope changed path must be restored by its owner or carried as an exact frozen pre-existing entry; it cannot be omitted merely by labeling it unrelated.
```

Historical Story 5 records are audited without rewriting them. If a final reviewed record and its final commit still disagree, the audit report names the discrepancy and the required amendment boundary.

### 4.4 Non-Mutating Public-Contract-Shape Diff

Proposed implementation surface: `tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` or a factored helper used by it.

OLD:

```text
The deterministic generator can overwrite public-contract-shape-baseline-v1.json, while the committed-baseline guard compares the live exported type count but does not independently expose a full non-mutating shape diff for a final-record command.
```

NEW:

```text
Expose a deterministic, non-mutating serialization of the live public contract shape. The final-record checker compares its full normalized bytes with the immutable Story 1.1 baseline.

The checker separately verifies that the baseline file itself has no unapproved working-tree change. It reports `empty` only when the regenerated live shape equals the baseline and the baseline artifact remains unchanged.

A non-empty diff fails Epic 5 verification unless every difference is recorded with an explicit approval reference. Epic 5's expected state is `empty`.
```

The comparison must not update or normalize the baseline as a side effect.

### 4.5 Epic 5 Audit Artifacts

Proposed artifacts:

- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-check.json`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-check.md`

The authoritative JSON contains one result per Story 5.1-5.3 plus the live verification of this workflow-tooling change. The Markdown file renders the same facts for review.

The audit must explicitly distinguish:

- the recorded historical counts of 365 / 365, 374 / 374, and 384 / 384;
- what is proven from committed artifacts and commit boundaries;
- what cannot be reconstructed about a former uncommitted working tree;
- the current live full-suite count after all test-impacting checker changes;
- unchanged frozen pre-existing root-gitlink dirt versus newly introduced gitlink changes; and
- pass, fail, or approved-amendment status for each required dimension.

No audit result may claim that today's working tree is the historical final working tree of Story 5.1, 5.2, or 5.3.

### 4.6 Developer and Review Guidance

Artifact: `tests/README.md`

OLD:

```text
The README documents canonical test execution and the xUnit v3 fallback, but not one mandatory final-record closure sequence.
```

NEW:

```text
Document this completion sequence:
1. Capture the work-item baseline commit and exact pre-existing dirty state before implementation.
2. Finish all product, test, documentation, and evidence changes.
3. Run the final validation through the final-record command.
4. Finalize the machine record, story Dev Agent Record, and File List without changing executable/test inputs.
5. Run final-record verification from the final working tree.
6. Move review to done only when the result passes or an explicit approved amendment resolves every failure.
```

Review guidance must require reviewers to read the machine result and inspect any exclusions or approval references rather than accepting a green summary alone.

### 4.7 Retrospective and Sprint-Status Resolution

Artifact: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-27.md`

OLD:

```text
Epic 4 A1: In progress. Story 5.3 has strong evidence-boundary validation, but Stories 5.1 and 5.2 still needed review corrections.
Epic 5 A5: Promote the Story 5.3 evidence-boundary validation pattern into reusable dev/review guidance.
```

NEW after implementation passes:

```text
Append a dated follow-up resolution that identifies the checker, guidance, and audit artifacts. Preserve the original retrospective-at-the-time result while recording that Epic 4 A1 and Epic 5 A5 were subsequently completed.
```

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- epic: 4
  action: "Add a mechanical final-record check for Epic 5 story completion that verifies test counts, file lists, changed docs/evidence, and public-contract-shape diff state from the final working tree."
  owner: "Dev workflow"
  status: in-progress
```

NEW after the final check passes or discrepancies have approved amendments:

```yaml
- epic: 4
  action: "Add a mechanical final-record check for Epic 5 story completion that verifies test counts, file lists, changed docs/evidence, and public-contract-shape diff state from the final working tree."
  owner: "Dev workflow"
  status: done
```

Epic 5 and Stories 5.1-5.3 remain `done`; no new backlog key is added.

## 5. Implementation Handoff

**Classification:** Minor course correction.
**Primary recipients:** Developer and Quality reviewer.
**Conditional recipient:** Release owner, only if the audit detects a discrepancy that affects an approved or hash-bound release claim.

### Responsibilities

- **Developer:** implement the checker, result schema, non-mutating contract comparator, and exact Git inventory logic.
- **Quality reviewer:** exercise success and failure fixtures for stale counts, missing/extra File List entries, untracked evidence, changed hashes, changed gitlinks, unchanged frozen dirt, contract drift, and approved-difference handling.
- **Technical writer/reviewer:** update `tests/README.md` and append the retrospective resolution without overstating historical reconstruction.
- **Release owner:** decide whether a detected signed-evidence discrepancy needs an amendment, superseding sidecar, or rejection. The implementation agent must not edit signed evidence to make the check pass.

### Implementation Order

1. Freeze the current pre-existing root working-tree state, including the three observed root gitlink entries, before any implementation change.
2. Define the versioned JSON contract and failure vocabulary.
3. Implement the non-mutating public-contract comparator and final-record checker.
4. Add adversarial fixtures and document the command/sequence.
5. Complete all test-impacting changes, then run the canonical final validation through the checker.
6. Finalize the implementation path list and audit artifacts.
7. Run live final-record verification from the final working tree.
8. Resolve any historical mismatch through a visible amendment boundary; do not rewrite signed evidence.
9. Append the retrospective resolution and mark the sprint action `done` only after the final result is passing or explicitly amended.

### Success Criteria

- One command fails mechanically on every required mismatch and succeeds only when test counts, exact paths, changed docs/evidence, and public-contract shape agree.
- Live verification includes tracked, staged, committed-since-baseline, and untracked non-ignored paths.
- Unchanged frozen pre-existing dirt is reported but not attributed to the work item; new or altered gitlink state fails.
- Test results are bound to the executable/test inputs they exercised and agree with every changed count-bearing record.
- The full regenerated public-contract shape equals the immutable baseline, with diff state `empty`, unless an explicit approval reference exists.
- Epic 5 historical limitations and results are reported honestly.
- Existing signed Story 5.3 release evidence remains byte-identical unless a separately approved correction is required.
- No product runtime, public API, package, AppHost, generated product output, or submodule source change is introduced.

## 6. Change Navigation Checklist

### 1. Understand Trigger and Context

- [x] 1.1 Trigger identified: Epic 4 retrospective A1, carried into Epic 5 and still `in-progress` in sprint status.
- [x] 1.2 Core problem defined: no reusable final-tree completion gate spans counts, exact paths, changed evidence, and contract shape.
- [x] 1.3 Evidence gathered: all three Epic 5 review correction sets, existing validators, final counts, retrospective status, signed-evidence boundaries, and current pre-existing gitlink dirt.

### 2. Epic Impact Assessment

- [x] 2.1 Epic 5 remains complete; this is a post-completion workflow correction.
- [N/A] 2.2 No existing epic scope modification is required.
- [x] 2.3 Future evidence stories benefit from the reusable live gate; no product dependency changes.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No epic resequencing or priority change is required.

### 3. Artifact Conflict and Impact Analysis

- [x] 3.1 PRD checked; existing FR-20/evidence requirements support the correction and need no edit.
- [x] 3.2 Architecture checked; the change enforces existing evidence and public-contract invariants and needs no edit.
- [N/A] 3.3 UX checked; there is no UI impact.
- [x] 3.4 Other artifacts identified: test tooling, test guidance, implementation audit, retrospective follow-up, and sprint action status.

### 4. Path Forward Evaluation

- [x] 4.1 Direct Adjustment is viable with medium implementation effort and bounded risk.
- [N/A] 4.2 Rollback is not justified because no completed product behavior needs reversal.
- [N/A] 4.3 MVP/PRD review is not justified because product scope is unchanged.
- [x] 4.4 Direct workflow adjustment selected; no Story 5.4 or epic reopening.

### 5. Proposal Components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic, story, artifact, and technical impacts documented.
- [x] 5.3 Recommended path and rejected alternatives documented.
- [x] 5.4 Product MVP remains unaffected; implementation and failure paths are bounded.
- [x] 5.5 Developer, Quality, technical-writing, and conditional release-owner handoffs defined.

### 6. Final Review and Handoff

- [x] 6.1 Applicable checklist items completed.
- [x] 6.2 Proposal checked against the current repository, Epic 5 records, and signed-evidence boundary.
- [x] 6.3 Jerome approved the proposal on 2026-07-14.
- [ ] 6.4 Sprint-status action update is pending successful implementation and verification.
- [x] 6.5 Handoff recipients, ordering, success criteria, and discrepancy escalation are defined.

## 7. Approval Request

Approval authorizes the bounded workflow-tooling, validation, documentation, audit, retrospective-resolution, and sprint-action changes described above. It does not authorize product/runtime changes, submodule updates, edits to signed release evidence, or acceptance of any discrepancy the checker may find.

**Decision requested:** Approve, reject, or request revisions to this Sprint Change Proposal.
