---
title: 'Epic 10 A1/A2: routine hardlink coverage and fault receipt integrity'
type: 'bugfix'
created: '2026-10-18'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context:
  - docs/runbooks/current-change-validation.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Retrospective actions A-1 and A-2 identify omitted routine hardlink coverage, assertion identities that incorrectly include the subject, and fault receipts that discard the verifier's observed state and exit. These gaps weaken future regression and malformed-receipt detection.

**Approach:** Harden the existing routine test importer, adoption fault producer, and receipt consumer as one bounded tooling follow-up. Given routine CI selection, when the adoption output-hardlink regression runs, then it must reject the write with `EVIDENCE_OUTPUT_FORBIDDEN`, preserve protected source bytes, and fail if the collision guard is removed. Given receipt ledgers, when IDs repeat even across different subjects, then consumption must reject them. Given newly measured adoption faults, when Git/history is unavailable, then retain and independently require the original `BLOCKED/2`; other adoption faults retain and require `FAIL/1`. Reject missing, malformed, mismatched, and misclassified observations. Keep the promotion-span guard's existing receipt contract because it records guard violations rather than a verifier envelope.

Follow current-change policy: use focused regression checks, preserve accepted records, receipts, bindings, schemas and original story specs, and do not replay or republish the old candidate boundary against current main. Historical readability may recognize only the exact accepted candidate and byte-pinned adoption fault receipt; it must not become an opt-in legacy mode or accept altered bytes. Regenerate local focused test receipts for this follow-up, independently review the change, and mark only A-1/A-2 done after verification. No product, dependency, gitlink, release or epic-lifecycle change is included.

</frozen-after-approval>

## Implementation Notes

- Planning found no unresolved intent choice or irreversible operation. The change uses the small oneshot route. This follow-up has no dotted story ID or v9 story contract; historical publication and evidence-boundary gates do not apply.
- Reuse `tests/tooling/test_evidence_boundary_current.py`'s existing importer and `test_adoption_output_hardlink_cannot_overwrite_a_source`; no CI job or framework change is needed.
- In `tests/tooling/test_evidence_boundary_adoption.py::observed_fault`, preserve the helper's original result and exercise the real adoption CLI exit mapping with that observation supplied at the verification seam. The bridge's transport exit is not the verifier exit. Record both measured result and exit in JUnit properties.
- In `_bmad/scripts/generate_story_record.py::v2_10_4_assertion_rows`, use ID-only uniqueness; in `v2_10_4_receipt_facts`, validate new adoption result/exit properties independently. Preserve only candidate `7e5a4b765a4437f361fac7e7bb4f13e5b7cf3ec8` and the original AC-10.4-06 receipt SHA-256 `94faa9000e8fdcbfc4a803d53777d061ff827837874bbda14557c388d1d3fb4a` as historical input.
- Extend `_bmad/scripts/tests/test_generate_story_record.py`'s synthetic receipt fixtures and parser negatives; add producer checks and exact legacy compatibility checks. Keep existing restoration, source stamps, case identity, blocker mapping and nonempty-ledger checks.
- Verify exact CI collection/execution of the hardlink regression, all adoption fault cases, focused generator receipt/identity/historical checks, and an isolated collision-guard mutation. Write local JUnit under `TestResults/epic-10-retro-a1-a2/`; do not replace `artifacts/v9/10.4/` evidence. Update the two action statuses in `sprint-status.yaml` and append dated closure evidence to the retrospective after verification.
- The broader lifecycle regression exposed three `guidance:resolved:*` IDs shared by adoption and workflow checks. `_bmad/scripts/evidence_boundary_adoption.py` now uses `guidance:adoption-resolved:*`. The exact accepted AC-09 envelope remains readable only at the accepted candidate and canonical SHA-256 `f6e7937ec00fef6eaebb355a68877dd264f5dfae7ca39acac5f9711fe66d395d`; changed envelopes and candidates still reject duplicate IDs. Original receipt rows and record projections remain unchanged.
- Retained `tests/tooling/check_adoption_hardlink_mutation.py` runs the unmodified regression first, then removes `and not collision` only from an in-memory function and requires a failing test. Explicit exit checks remain effective under `python -O`; errors, no collection, and a surviving mutant cannot report success.
- Retain fresh, source-commit-stamped adoption fault JUnit separately at `_bmad-output/implementation-artifacts/tests/epic-10-retro-a1-a2-faults.xml`, with an explicit LF rule in `.gitattributes`. This is focused follow-up evidence; the accepted Story 10.4 publication is preserved.
- Review recheck confirmed all five findings resolved, with no remaining concrete issues or deferrals. `python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests/test_generate_story_record.py -k 'story104 or story103_schema_keeps_accepted' -o junit_family=xunit1 --junitxml=TestResults/epic-10-retro-a1-a2/generator-focused.xml` passed **102 tests**, zero errors/skips. The producer-only current lane passed **11 tests**. Four accepted record pairs passed digest/schema checks; 139 evidence/binding/contract files remained byte-identical.

## Review Triage Log

- **medium, patched:** ID-only consumption rejected the accepted AC-09 raw envelope and exposed live composed guidance collisions. Distinct producer IDs and the exact candidate/canonical-envelope pin preserve historical consumption without accepting new duplicate IDs.
- **medium, patched:** The producer could emit `BLOCKED/2` with a failing ledger. Check the derived state against the untouched original observation; producer and consumer negative cases reject this contradiction.
- **medium, patched:** The producer could emit repeated IDs or an entirely skipped ledger. Enforce ID uniqueness and at least one evaluated row before emitting properties; negative cases cover both.
- **medium, patched:** The consumer accepted warning codes in blockers, although the transition adapter rejects them. Limit blockers to failure/availability codes and reject a warning-in-blockers receipt.
- **medium, patched:** Optimization removed the mutation checker's operational assertions, including its baseline invocation. Replace them with explicit failures; optimized probes reject pytest exits 0, 2, 3 and 5 where a killed mutant is required.
