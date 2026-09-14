---
title: 'Publish the Story 7.1 V20 release-owner authority'
type: 'feature'
created: '2026-09-14'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '589cdd711871d5a632b97df0b04375d1cf6e0864'
submodule_promotions: []
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-12.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 7.1 remains held because neither the fresh-checkpoint V19 authority nor the independent release-owner V20 authority has been published. The correction tooling is committed and valid, but the historical checkpoint cannot be reused.

**Approach:** Create one fresh exact-five-path schema checkpoint, publish and validate V19 as its additive child, select a distinct empty V20 entry commit, then publish and validate a human-authored V20 as the entry's exact-one-path child.

## Boundaries & Constraints

**Always:** Start from committed clean baseline `589cdd711871d5a632b97df0b04375d1cf6e0864`. Keep the correction record and semantic source at their frozen digests. Give the checkpoint exactly the four V11-owned tracked paths plus freshly command-produced JUnit XML; require a single parent, no gitlink changes, six-or-more passing tests, and a nonempty derived ledger. Publish V19 and V20 additively in separate exact-one-path commits. Record an explicit owner identity, UTC decision after entry and no later than V20 publication, and the frozen rationale binding `V19-STORY-7.1-CHECKPOINT-COMPLETION` plus `V20-STORY-7.1-INPUT-INVENTORY-v1`. Validate both authorities and require effective hold `LIFTED` only after V20.

**Never:** Reuse `b819a7c` or the stale ignored XML; fabricate result facts; edit V17, V18, the hold record, semantic Story 7.1 spec, dependencies, submodules, sprint status, loop state, or Story 7.1 implementation paths outside the V11 checkpoint; publish V19 and V20 together; mark Story 7.1 done; unlock 7.2/release/push; rewrite history; or push.

**Human decisions:** Record the V20 release-owner identity exactly as `Jerome Piquot <jpiquot@itaneo.com>`. Create the fresh checkpoint with exact descriptive authority-boundary annotations on all three schemas and a focused test that asserts them; do not alter schema validation semantics.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Fresh checkpoint | Clean corrected baseline and exact V11 command | Direct child changes exactly five paths; committed XML is a current nonempty pass | Any missing/extra path, gitlink, stale XML, failed/skipped/empty test, or non-substantive tracked change blocks |
| V19 publication | Valid committed checkpoint candidate | Exact-one-path V19 child records current `PASS`, historical `NONCONFORMING`, and hold `ACTIVE` | Missing or drifted correction, source, history, schema, path, mode, digest, or ledger blocks |
| V20 publication | Valid V19, distinct entry, explicit owner decision | Exact-one-path V20 child unlocks only `7.1`; effective hold becomes `LIFTED` | Missing, stale, pre-entry/post-publication, widened, or nonhuman decision keeps hold `ACTIVE` |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-12.md` -- authoritative V19-to-V20 order, candidate roles, and publication effects.
- `_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` -- exact checkpoint paths and frozen pytest/JUnit command; consume read-only.
- `_bmad/scripts/publish_story_7_1_successor_authorities.py` -- existing inventory, V19/V20 publish/check, chronology, and effective-hold routes; do not modify.
- `_bmad/schemas/v9-acceptance-result-v1.schema.json`, `_bmad/schemas/v9-frozen-inventory-v1.schema.json`, `_bmad/schemas/story-final-record-v2.schema.json` -- three checkpoint schemas requiring a fresh, approved substantive transaction.
- `_bmad/scripts/tests/test_generate_story_record.py` -- checkpoint test surface selected by `-k v2_schema_contract`.
- `artifacts/v9/schema-slice/v2-schema-contract.xml` -- ignored stale snapshot; regenerate with the exact command and force-add only the fresh bytes.

## Tasks & Acceptance

**Execution:**
- [ ] V11 checkpoint files -- apply the approved narrow schema/test clarification, run the exact frozen command, and commit only the exact five paths with a pinned-commitlint-valid message.
- [ ] `_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json` -- generate from the committed checkpoint, commit alone as its direct child, and validate at that publication.
- [ ] Git entry transaction -- create a distinct conventional allow-empty child of V19 before the owner decision and any Story 7.1 implementation.
- [ ] `_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json` -- generate from the entry using the approved human fields, commit alone as its direct child, and validate current V19/V20 plus effective hold.

**Acceptance Criteria:**
- Given the committed correction baseline, when the checkpoint and two authority publications complete, then Git history is a linear checkpoint -> V19 -> entry -> V20 chain with exact path boundaries and no submodule changes.
- Given committed V20, when V19, V20, inventory, lifecycle-boundary, and effective-hold checks run at `HEAD`, then all applicable ledgers are nonempty, both authorities report `PASS`, and only Story 7.1 implementation is lifted.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The entry is intentionally a separate allow-empty commit: it makes the human decision visibly later than V19 while preserving V20's exact-one-path publication rule. All five candidate commit messages must be revalidated with the pinned commitlint CLI immediately before use.

## Verification

**Commands:**
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml` -- expected: exit `0`, nonzero tests, no failures/errors/skips, fresh XML.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v19 --candidate HEAD --check` -- expected: V19 `PASS` with nonempty ledger and hold `ACTIVE`.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v20 --entry-candidate HEAD --check` -- expected: V20 `PASS`, `unlocks: [7.1]` only.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v20 --entry-candidate HEAD --effective-hold --check` -- expected: `PASS`, effective hold `LIFTED`, nonempty ledger.
- `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline 589cdd711871d5a632b97df0b04375d1cf6e0864 --candidate HEAD` and `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 589cdd711871d5a632b97df0b04375d1cf6e0864 --candidate HEAD` -- expected: promotion exit `0`, evidence `PASS` with nonempty ledger.
- `git diff --check` and exact-path audits for every transaction -- expected: no whitespace or scope drift.
