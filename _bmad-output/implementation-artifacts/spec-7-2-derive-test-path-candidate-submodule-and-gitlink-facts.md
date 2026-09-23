---
title: 'Derive test, path, candidate, submodule, and gitlink facts'
type: 'feature'
created: '2026-09-23'
status: 'ready-for-dev'
baseline_commit: 'c69334cb13a981c9112ad687427b1f43fafc2988'
allowed_skipped_tests: []
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/v9/story-contracts/7.2.json'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The v2 generator lacks Story 7.2's root test totals, exact changed paths, baseline, final candidate, and predecessor digest.

**Approach:** Extend the record and generator with measured facts from test artifacts and root Git objects; generate the pair after ten prerequisite scenarios pass.

## Boundaries & Constraints

**Always:** Follow all 11 commands in `v9/story-contracts/7.2.json`. Derive nonempty counts for root-owned `.slnx` test projects from current results and the skip policy. Compare the exact normalized baseline-to-candidate path set. Match raw mode-`160000` gitlinks to root `.gitmodules`. Recompute the Story 7.1 digest. Keep result states distinct and applicable ledgers nonempty.

**Never:** Accept caller facts; traverse or change submodules; count files beneath gitlinks as owned paths; rewrite historical evidence; change v1 or Story 7.1 output; implement Stories 7.3–7.4. Story 7.1's `done` row and `PASS` record do not establish terminal `ACCEPTED` authority.

## I/O & Edge-Case Matrix

| State | Result | Required blocker |
| --- | --- | --- |
| Eight current root project results, exact paths, ten gitlinks | Derived, schema-valid deterministic pair | None |
| Missing, stale, failed, unapproved-skipped, zero-run test | No passing record | `TEST_RESULTS_MISSING`, `TEST_RESULTS_STALE`, `TEST_FAILED`, `TEST_SKIPPED`, `TEST_NOT_RUN` |
| Unrelated dirt, divergent list, path below a gitlink | No passing record | `SOURCE_TREE_DIRTY`, `FILE_LIST_DRIFT`, `SUBMODULE_INTERNAL_PATH` |
| Missing/extra/moved gitlink, false `160000` filename, bad baseline, superseded candidate | No passing record | `GITLINK_SCOPE_MISMATCH`, `GITLINK_DRIFT`, `BASELINE_NOT_TRUSTWORTHY`, `CANDIDATE_NOT_FINAL` |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/generate_story_record.py:721,861,1040,1380,1476` -- adapt solution/TRX, path, and candidate derivation to v2. Its v2 route at `:3025,3215,3345,3829` handles gitlinks, JUnit ledgers, and output; retain strict CLI.
- `_bmad/schemas/story-final-record-v2.schema.json:7` -- closed record lacks per-project totals, path set, baseline, and predecessor digest; add a backward-compatible Story 7.2 measurement shape.
- `_bmad/scripts/tests/test_generate_story_record.py:2083,2164,2246` -- extend v2 hermetic fixtures and byte-identity snapshots for the ten exact Story 7.2 selectors; preserve Story 7.1 fixtures.
- `Hexalith.Conversations.slnx:21`, `_bmad/schemas/v9-acceptance-result-v1.schema.json`, and `_bmad-output/planning-artifacts/v9/story-contracts/7.2.json` -- eight-project inventory, closed result envelope, and frozen acceptance commands.
- `docs/runbooks/story-final-record-generation.md:258` and `docs/release-evidence/story-7.1-final-record-v2.json` -- operator rules and predecessor input. New outputs: `docs/release-evidence/story-7.2-final-record-v2.{json,md}`.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/schemas/story-final-record-v2.schema.json` -- add closed optional Story 7.2 measurements; keep the committed Story 7.1 record valid.
- [ ] `_bmad/scripts/generate_story_record.py` -- derive tests, paths, baseline/candidate, gitlinks, and predecessor binding; reject named faults; render and verify the new facts.
- [ ] `_bmad/scripts/tests/test_generate_story_record.py` -- implement `AC-7.2-01` through `AC-7.2-10` selectors, one-fault fixtures, acceptance-result validation, and byte-identical restoration.
- [ ] `docs/runbooks/story-final-record-generation.md` -- document Story 7.2 inputs, blocker codes, exit classes, and operator commands.
- [ ] `docs/release-evidence/story-7.2-final-record-v2.{json,md}` -- generate from a committed candidate after the prerequisite results pass and verify identical rerun bytes.

**Acceptance Criteria:**
- Given eight current root project results, when `AC-7.2-01` runs, then artifact-derived per-project and summed counts validate as acceptance-result v1.
- Given each named fault, when `AC-7.2-02` through `AC-7.2-10` run, then each proves its exact blocker and restores its fixture byte-identically.
- Given ten passing prerequisite scenarios and the verified Story 7.1 digest, when `AC-7.2-11` runs, then the pair binds every measured fact and reports `11/11/0/0/0/0`.
- Given existing v1 and Story 7.1 tests, when rerun, then their behavior and Story 7.1's committed pair remain valid.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Read each project's TRX at `artifacts/v9/7.2/test-results/<project>.trx`, deriving `<project>` from the committed `.slnx`. The direct Story 7.2 request authorizes implementation. Current planning authority lacks Story 7.1 terminal `ACCEPTED`; report that condition without claiming its lifecycle gate passed.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- v1, 7.1, and 7.2 pass.
- Run `AC-7.2-01` through `AC-7.2-10` exactly as declared in the frozen contract through pinned `uv` -- current nonempty JUnit results; each negative fixture proves its blocker.
- Run `AC-7.2-11` exactly through pinned `uv` after committing source -- schema-valid `PASS`, deterministic pair, ten gitlinks, `11/11/0/0/0/0`.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline c69334cb13a981c9112ad687427b1f43fafc2988 --candidate HEAD` -- record its actual state before review/done.
- `git diff --check` -- no whitespace errors.
