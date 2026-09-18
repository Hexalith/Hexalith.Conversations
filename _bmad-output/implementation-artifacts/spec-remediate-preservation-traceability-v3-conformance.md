---
title: 'Remediate preservation traceability v3 Conformance failures'
type: 'bugfix'
created: '2026-09-17'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '2d2ae57db1fdcc164fe01ac4b1d99af15c31b324'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved preservation traceability contract records 473 tests and 969 obligations, but the Owner-review Conformance run has 17 failures caused by BMad 6.12 route drift, validators applying historical evidence to the current checkout, and current-state checks still targeting the stale v2 manifest. The PRD also fails to acknowledge the effective detached approval.

**Approach:** Repair the existing test identities and four terminal workflow controls, validate immutable evidence at its recorded Git-object time basis, and publish a distinct unapproved `3.0.0-rc.2` preservation candidate from genuinely changed sources. Keep approved `3.0.0-rc.1` immutable and keep all release and hold states fail-closed.

## Boundaries & Constraints

**Always:** Preserve exactly 473 fully qualified test identities, their order/digest, all seven categories, all 969 obligation identities and classifications, the 214/384 floors, 89 pending additions, 277 dispositions, zero legacy activation, and zero denominator shrinkage. Rebind four final-record controls one-for-one to `bmad-build` present/oneshot, `bmad-build-auto` review, and `bmad-code-review` present in both byte-identical skill trees. Record material actions in the PRD memlog.

**Never:** Rewrite v1/v2 evidence, `3.0.0-rc.1`, its approval/authority, projection proof v2, signed evidence, SM-C2 evidence, submodule content/gitlinks, public or production code, or package versions. Do not infer approval, waiver, current projection readiness, grant effectiveness, release authority, legacy activation, or hold lift. `rc.2` remains unapproved; FR-20/SM-C1 remain PENDING, SM-C2 FAILED, OQ-1 BLOCKED, and the hold ACTIVE.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Historical evidence | Later source/gitlink movement | Resolve bound blobs and mode-160000 gitlinks at the recorded candidate | Missing/wrong objects fail closed; current readiness stays blocked |
| Workflow completion | Any of four terminal routes | Candidate-bound final record is generated, inserted verbatim, and digest-verified before terminal status | Missing, displaced, gutted, stale, or uncommitted gates prevent completion |
| Preservation successor | Remediated 473-test assembly and fresh XML | Generate separate `rc.2` JSON/Markdown/schema/digest with 969 zero-orphan closures | Any identity, obligation, category, classification, or binding drift fails generation |
| Approval boundary | Existing detached `rc.1` approval | Verify against immutable predecessor bytes only | Never carry approval to `rc.2` |

</frozen-after-approval>

## Code Map

- `.agents/skills/{bmad-build,bmad-build-auto,bmad-code-review}` and `.claude/skills/...` -- four current terminal routes; each pair must remain byte-identical.
- `tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs` -- preserve eight Fact identities while rebinding route contracts.
- `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs` -- validate deleted aliases from the frozen planning candidate, not the live tree.
- `tests/Hexalith.Conversations.Conformance.Tests/{ArchitecturePlanningAuthorityValidationTest,SuccessMetricReportAndAttestationValidationTest,ProjectionReadStorePopulationProofValidationTest}.cs` -- historical Git-object and archive-aware binding fixes.
- `tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` -- preserve method identities while validating the current successor contract and frozen predecessors.
- `_bmad/scripts/generate_preservation_traceability_manifest_v3.py` -- immutable rc.1 checker; reuse extraction logic without changing its outputs.
- `_bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py` and `docs/release-evidence/preservation-traceability-manifest-v3-rc2.*` -- additive unapproved successor.
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/{prd.md,addendum.md,.memlog.md}` -- truthful approval, result, and fail-closed status narrative.

## Tasks & Acceptance

**Execution:**
- [x] Restore candidate-bound final-record gates and explicit commit authorization on the four current terminal routes in both skill trees; rebind existing lifecycle tests without creating aliases or test identities.
- [x] Replace live-checkout validation of historical PRD, signed-v1, and projection-v2 bindings with contained, candidate-aware Git-object/archive validation; preserve every historical artifact byte.
- [x] Make current preservation assertions successor-aware; create deterministic rc.2 generation with immutable rc.1 lineage and exact identity/obligation equality guards.
- [x] Update PRD/addendum wording for effective rc.1 approval and unapproved rc.2 while retaining all mandated gate states; append every material result to the memlog.
- [x] Run focused lanes, the complete Conformance suite, byte/digest/schema checks, the evidence-boundary verifier, and an independent reviewer gate; regenerate only changed-source evidence.

**Acceptance Criteria:**
- Given the approved contract, when rc.2 is generated, then its ordered 473 IDs and 969 obligation identities/classifications exactly equal rc.1, with all floors retained, zero orphans, and no legacy activation.
- Given immutable historical evidence, when validators run after unrelated later changes, then they validate recorded objects without modifying history or claiming current projection readiness.
- Given the four current terminal routes, when gate mutation/parity checks run, then every control is present before completion and both skill trees remain byte-identical.
- Given the final candidate, when focused checks and full Conformance run, then all 473 pass with zero failed, skipped, or not-run tests; independent review finds no critical/high defect.
- Given green Conformance, when status is reconciled, then rc.2 remains unapproved and FR-20/SM-C1 remain PENDING until their remaining independent approval gate passes; SM-C2, OQ-1, and the hold remain unchanged.

## Implementation Notes

- Final source candidate: `sha256-path-mode-hash-size-overlay-v2:1ba2f3e23b56dc3c1caa05addb175461311e472487c5f2c4580317ee109d8840`.
- Candidate-owned restore, controlled build, exact-DLL Conformance, immutable manifest, and detached post-run evidence form a convergent chain. The controlled build completed with zero warnings/errors and the exact bound DLL passed 473/473 tests.
- The candidate inventory is frozen at 19 baseline-present and two explicitly new source paths. Python and C# independently enforce baseline presence, unique stage-0 mode `100644`, worktree mode, path containment, and byte identity.
- Historical PRD, signed-v1, and projection-v2 bindings resolve explicit Git objects and verify regular raw modes before reading blobs. Approved rc.1 and its detached approval remain byte-identical.
- The broad Python sweep retains 22 independently reproduced pre-existing planning-authority failures (V21: 1, V15: 5, V18: 8, V9: 8); the changed rc.2 Python lanes pass 110/110.

## Spec Change Log

- 2026-09-18: Implemented the remediation, generated unapproved rc.2 evidence, and verified the final candidate without staging, committing, pushing, or changing the frozen intent.

## Review Triage Log

- 2026-09-18 Final7 independent gate: PASS; zero actionable critical, high, medium, or low findings.
- Resolved review findings included bootstrap false-green behavior, evidence self-reference, stale toolchain/restore provenance, exact build causality, mode-blind and staged-removal candidate identity, historical raw-mode validation, detached-index drift, and executable filesystem/process fault coverage.
- Matrix verdicts: historical evidence PASS; workflow completion PASS; preservation successor PASS; approval boundary PASS.
- 2026-09-18 formal lifecycle gate: HALTED before the `in-review` status write. The required plain-Python evidence-boundary command exited `1` with `TOOLING_INSTALLED_VERSION_MISMATCH` (`jsonschema: 4.19.2`). The submodule-promotion command exited `0` with no declared promotions and a `SCOPE_NOT_EVALUATED` warning. Lifecycle status remains `in-progress` as required by the fail-closed workflow.

## Design Notes

The preservation manifest is an immutable authority chain, not a mutable “latest” file. `rc.1` proves the approved 473/969 traceability contract at its recorded source state; `rc.2` may refresh hashes and results only as an additive, explicitly unapproved successor. Historical projection proof validation is similarly separate from current readiness.

## Verification

**Commands:**
- `uv run --frozen python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests` -- expected: zero failed, errored, or skipped relevant checks.
- `candidate_digest=$(uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py --print-candidate-digest)` -- expected: the canonical digest of the frozen source-input inventory; spec/memlog assessment records are excluded.
- `uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py --capture-restore-evidence` -- expected: capture and bind fresh `dotnet --info`, then execute `dotnet restore tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --force-evaluate -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` through the SDK pinned by `global.json`, with exit status, log, and deterministic recursive `project.assets.json` inventory recorded before the `--no-restore` rebuild.
- `uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py --run-build` -- expected: after validating the exact current restore/toolchain receipt and 17-project dependency inventory, the generator executes the one exact Release Rebuild argv with `--no-restore`, `-m:1`, and `SourceRevisionId=$candidate_digest`; it captures the raw log and writes a receipt binding the restore-before-build sequence, exact command/exit/result, fresh toolchain and dependency hashes, clean counters, and post-build assembly.
- `uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py --run-conformance` -- expected: runner-owned assembly/XML receipt; final convergence is 473/473 with zero failed/skipped/not-run. If a changed source initially makes current generated evidence stale, retain the truthful failed receipt, regenerate rc.2 without `--require-green`, and repeat this command; never rewrite counters.
- `uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py`; repeat the runner; regenerate the now-green stable manifest once with `--require-green`; run the runner a final time against those exact manifest bytes; then run `uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py --finalize-detached-index` and `uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py --check --require-green` without regenerating the manifest -- expected: the final receipt hashes the exact final manifest it tested, the detached index binds manifest/receipt/XML/toolchain/restore/build evidence without a self-cycle, and Conformance remains 473/473 green.
- `uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3.py --check` -- expected: immutable rc.1 passes unchanged.
- `sha256sum -c docs/release-evidence/preservation-traceability-manifest-v3-owner-approval.sha256` -- expected: rc.1 approval bindings pass unchanged.
- `uv run --frozen python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 2d2ae57db1fdcc164fe01ac4b1d99af15c31b324 --candidate HEAD` -- expected: preserve PASS/FAIL/BLOCKED/not-applicable distinctly with a nonempty applicable ledger.
