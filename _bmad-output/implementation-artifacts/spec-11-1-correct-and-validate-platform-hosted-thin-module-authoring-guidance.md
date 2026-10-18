---
title: 'Story 11.1: Correct and validate platform-hosted thin-module authoring guidance'
type: 'feature'
created: '2026-10-18'
status: 'in-progress'
baseline_commit: 'ed81a80ea0a8cb0c6e31f4755f082b8913389e6c'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `docs/domain-module-authoring-template.md` tells authors to create module-owned `ServiceDefaults`, Aspire/Dapr wiring, projection and telemetry pieces, and counts a shipping `AppHost`. It does not assign infrastructure to the platform or forbid a reusable module Aspire library, so SM-2 evidence cannot trust the recipe.

**Approach:** Correct the guidance source, generate a schema-validated deterministic guidance bundle (JSON + Markdown) from it, validate ownership/prohibition/AppHost rules with a Python generator, verifier, xUnit tests and a fault matrix, and record a reviewer decision bound to the bundle digests. Checkpoint A only: no fixture, no SM-2.

## Boundaries & Constraints

**Always:** Module-owned = contracts, domain behavior, module registration, module tests. Platform-owned = `ServiceDefaults`, Dapr implementation/wiring, event subscription, projection/query runtime, publication, health, telemetry. Exactly one repository-only test AppHost, `IsPackable=false`, `IsPublishable=false`, counted in authoring cost. Bundle fields: `schemaVersion` (`hexalith.conversations.thin-authoring-guidance.v2`), `candidate`, `source`, `moduleOwned`, `platformOwned`, `prohibited`, `testAppHost`, `validation`, `reviewDecision`, `digests`. Output is deterministic and byte-restored after every fault. Keep existing `DomainModuleAuthoringTemplateValidationTest` facts passing unless a correction truly supersedes them.

**Never:** Create the fixture, compute or decide SM-2, alter v1 SM-2 baseline or historical guidance evidence (`thin-authoring-template-validation-v1.md`), edit predecessor records, change submodules, or restore retired authority/evidence gates.

**Owner decision, 2026-10-18:** AC-11.1-01 does not build the absent `verify_story_candidate.py`. Substitute current-policy checks: candidate is committed `HEAD`, Story 7.4 and 10.4 final-record v2 digests are recomputed, and Story 6.2 is bound through its existing release evidence (no 6.2 v2 record). Record the substitution in the bundle `validation` and Implementation Notes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Valid guidance | Corrected source, approved review | PASS, deterministic JSON/MD | N/A |
| Ownership drift | Capability missing, extra or reassigned | FAIL | `THIN_GUIDANCE_OWNERSHIP_INVALID` |
| AppHost violation | 0 or 2 hosts, packable/publishable, cost rule omitted | FAIL | `APPHOST_BOUNDARY_INVALID` |
| Platform capability in module / reusable Aspire library | Prohibition absent | FAIL | `PROHIBITED_CAPABILITY_PRESENT` |
| Review missing | No approved decision | FAIL | `REVIEW_DECISION_MISSING` |
| Drift after generation | Source or MD/JSON edited | FAIL | `GUIDANCE_SOURCE_DRIFT` / `EVIDENCE_FORMAT_DRIFT` |

**Owner decision, 2026-10-18 (AC-07):** Add a closed Story 11.1 route to `generate_story_record.py`, its schema and tests, following the Story 10.4 pattern, so the final record binds `SC-11.1` and reaches `7/7/0/0/0/0`.

**Owner decision, 2026-10-18 (reviewer):** The reviewer is Jerome Piquot. The decision is `approved`, stamped with a UTC instant when the human approves the generated bundle at the end of this story, and bound to the bundle's signable-payload digest.

</frozen-after-approval>

## Code Map

- `docs/domain-module-authoring-template.md` -- guidance source; correct the skeleton table (ServiceDefaults/Aspire/Dapr rows) and add ownership, prohibition and AppHost sections.
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` -- add `GuidanceShouldDefineOneNonShippingTestAppHost` and `GuidanceShouldProhibitPlatformCapabilityOwnership`; uses `RepositoryLocator`; CRLF.
- `_bmad-output/planning-artifacts/v9/story-contracts/11.1.json` -- frozen contract with AC commands; do not edit.
- `docs/release-evidence/thin-authoring-template-validation-v1.md`, `minimal-module-authoring-cost-sm2-baseline-v1.*` -- history; do not edit.
- `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json` -- final-record route (see Open Questions); pattern from 10.4.
- `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- pattern for restore-checked fault matrices.

## Tasks & Acceptance

**Execution:**
- [x] `docs/domain-module-authoring-template.md` -- correct ownership, add prohibition and test-AppHost sections -- source of truth
- [x] `docs/release-evidence/thin-authoring-guidance-v2.schema.json` -- closed schema with the ten required fields
- [x] `_bmad/scripts/generate_thin_authoring_guidance.py` -- emit deterministic JSON/MD with digests; exit 1 `THIN_GUIDANCE_OWNERSHIP_INVALID` on inventory mismatch
- [x] `_bmad/scripts/verify_thin_authoring_guidance.py` -- schema, source/evidence drift, format parity, review decision bound to signable-payload digest
- [ ] `docs/release-evidence/thin-authoring-guidance-v2.{json,md}` -- generated bundle with approved reviewer decision (named reviewer and UTC instant supplied by the human)
- [x] `tests/.../DomainModuleAuthoringTemplateValidationTest.cs` -- the two new methods
- [x] `_bmad/scripts/tests/test_thin_authoring_guidance.py` -- `checkpoint_a_fault_matrix`: reassign each platform capability, add reusable Aspire library, packable/publishable AppHost, omit cost rule, post-generation source edit, remove review; exact blockers; byte-identical restore
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` -- closed 11.1 route and facts; AC-01 substitute; AC-07 final record; receipts under `artifacts/v9/11.1/`

**Acceptance Criteria:**
- Given the corrected source, when the generator runs twice, then JSON and Markdown are byte-identical and ownership matches the inventory.
- Given each fault mutation, when the real checker runs, then it yields its exact blocker and bytes restore.
- Given the Contracts test assembly built in Release, when AC-03/AC-04 run with `-failSkips`, then both pass with zero skips.

## Implementation Notes

- AC-11.1-01 substitution (owner decision 2026-10-18): `verify_story_candidate.py` is not built. The bundle `validation.authoritySubstitution` and the final record's `thinGuidanceExecution.authoritySubstitution` record that the candidate is the committed `HEAD`, the Story 7.4 and 10.4 final-record v2 digests are recomputed (pair verified, digest equal to the bundle row), and Story 6.2 is bound through `docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json` (no 6.2 v2 record). The generator derives AC-11.1-01 itself; there is no `AC-11.1-01.json` receipt.
- Bundle `candidate` is the story contract plus planning candidate (not `HEAD`) so regeneration is byte-stable across commits; the committed-`HEAD` candidate is bound by the final record. The generator preserves a review decision still bound to the unchanged signable-payload digest, so AC-11.1-02 re-runs are idempotent.
- The reviewer decision is intentionally absent (`reviewDecision: null`) until the human approves. Approve with `generate_thin_authoring_guidance.py ... --approve-reviewer "Jerome Piquot" --decided-at-utc <UTC instant>`, then commit, run AC-11.1-03 through AC-11.1-06 against that commit, and run AC-11.1-07 (receipts under `artifacts/v9/11.1/` must be force-added like the other stories').
- The fault ids and exact blockers live once in `FAULT_EXPECTATIONS` (`generate_thin_authoring_guidance.py`); the fault matrix test, the AC-11.1-06 JUnit check and the final-record mutation ledger all derive from it.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| VG1 | medium | patch | `v2_generate` 11.1 dispatch, `V2Stop` mapping and the disk-receipt freshness/symlink guard are only reached with an injected `read_receipt`; add real-path tests. |
| VG2 | medium | patch | AC-03/04 TRX validation (wrong method, skipped/failed, total != 1) has no negative test. |
| VG3 | medium | patch | AC-11.1-01 substitute: predecessor digest/pair-verification mismatch has no negative test. |
| VG-o1 / E8 | low | patch | `AC-11.1-01.json` in `receipt_paths` tolerates an unvalidated dirt file; no receipt of that name exists. Delete the entry. |
| VG-o2 / B3 | low | patch | `candidate != HEAD` after defaulting to HEAD is tautological for the default path; compare only when `candidate` was supplied. |
| VG-o3 / B7 / E18 | low | defer | Story 10.4 fixtures skip live `.cs` files; narrows 10.4 fixture coverage but is required so adding 11.1 tests does not trip the frozen 10.4 reader check. |
| B1 / E19 | false | reject | Null `reviewDecision` is the specified pre-approval state; the owner decision stamps it at story end. |
| B2 | false | reject | Free-text approver is the owner-decided mechanism (Jerome approves via the CLI). |
| E1 | false | reject | Verification uses the separate `--verify-inserted` path (line ~10005); `v2_generate` is not the verify entry. |
| E3 | low | patch | Zero-result TRX raises IndexError instead of a ValueError; require exactly one result. |
| E15 / E4 | low | patch | Verifier: catch ImportError/UnicodeDecodeError as `Blocked`; guard ParseError on JUnit. |
| E13 | low | patch | `--decided-at-utc` must be a real date (strptime) and ASCII digits only; verifier uses fullmatch. |
| E17 | low | patch | Record schema: `receipts` need unique, exactly the four scenarioIds. |
| E10 / B11a | medium | patch | `skeleton_violation` fullmatch misses names like `Hexalith.X.ServiceDefaults` or "Reusable Aspire library"; broaden to a case-insensitive search excluding the single AppHost row. |
| B4, B5, B6, B8-B14, E2, E5-E7, E9, E11, E12, E14, E16 | low/maybe-false | defer | Hardening, design-duplication and doc-completeness concerns beyond the spec's contract (see deferred-work.md). |
| CR2-D1 | high | resolved: republish | Fix by withdrawing and republishing from a new candidate (option b). `--verify-inserted-record` failed at HEAD with `RECORD_CONTENT_DRIFT` (`publication boundary or later source changed`): pushed gitlink-only commit `24aa14a` follows the record publication, and the gate forbids any gitlink change after it. |
| CR2-D2 | medium | resolved: accepted | The blanket `.cs` skip stays. Narrowing it to the one 11.1 file was tried and failed `test_approved_gitlink_amendment_complete_verifier_preserves_result_schema`: the fixture copies live files over the Story 10.4 base, and 30 approved `.cs` files differ from it. |
| CR2-D3 | medium | resolved: deferred | Prohibition and ownership validation reads only the three marked tables; checklist, authoring sequence and description columns can reintroduce module-owned ServiceDefaults or Aspire guidance unnoticed. |
| CR2-P1 | medium | patched | `generate_thin_authoring_guidance.py` lines 217-236 exempt and count any skeleton category containing `AppHost`; a row such as `Reusable Aspire AppHost library` passes both checks. Match the single test-AppHost row exactly. |
| CR2-P2 | medium | patched | No tests reach `v2_11_1_verify_inserted`, `v2_11_1_descendant_errors`, the supplied-candidate guard or the `WORKTREE_NOT_CLEAN` guard; add real-git negative tests. |
| CR2-P3 | low | patched | `ROW` regex in `generate_thin_authoring_guidance.py` line 119 is unused; delete it. |
| CR2-W1 | medium | defer | Receipts are bound to the candidate by mtime and name only (PASS-only payload, TRX `endswith`, no cross-check of receipt digests in pair verification, mutation ledger `detected`/`restored` set True from constants); appended to deferred-work.md. |
| CR2 rejected | low/false | reject | Spec status vs sprint `review` (fix edits spec); unauthenticated approver (owner-decided, B2); 6.2 digest-only binding (as specified); missing-key traceback, path escape, non-atomic write, table-parse edges, csproj substring, symlinked parent dirs, stale template wording, Publication subscribers row, JUnit inventory-test optionality, coarse ledger entry, `review=False` test name (unlikely and fix adds guards); repo-csproj AppHost assertion is the AC-11.1-03 intent. |
