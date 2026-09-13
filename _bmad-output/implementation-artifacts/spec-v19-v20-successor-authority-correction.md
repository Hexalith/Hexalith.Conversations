---
title: 'Implement V19 and V20 successor-authority tooling'
type: 'feature'
created: '2026-09-12'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '64b050831eea694cb2065342cf23a150646eef57'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-12.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved forward-only correction has no dedicated schemas, frozen inventory, or publisher/validators for the separate V19 checkpoint-completion and V20 release-owner authorities. Without them, neither future transaction can be validated without conflating historical checkpoint evidence, candidate roles, or hold effects.

**Approach:** Add the six-file successor-authority tooling surface defined by the approved proposal. Make publication candidate-bound and fail-closed, but leave both authority records absent until their independent prerequisites exist.

## Boundaries & Constraints

**Always:** Recompute source hashes, canonical NFC UTF-8 LF inventory digests, exact path sets, committed blob modes/digests, single-parent relations, raw mode-`160000` gitlinks, JUnit counts and ordered nonempty ledgers. Preserve distinct `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` results. V19 records `b819a7c` as `NONCONFORMING` and keeps the full-story hold active. V20 validates an explicitly supplied release-owner decision and distinguishes PC, checkpoint, entry, publication-baseline, and eventual `SC-7.1` roles. Missing or stale V20 evidence evaluates the effective Story 7.1 hold as `ACTIVE`.

**Never:** Create live V19/V20 authority JSON in this run; execute or certify `7.1-SCHEMAS`; author the release-owner decision; implement Story 7.1 or Stories 7.2-7.4; modify V17, V18, `implementation-hold-v1.json`, `b819a7c`, the approved semantic spec, checkpoint-owned files, sprint status, loop markers, dependencies, workflows, submodules, or other user changes; stage, commit, or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Inventory check | Proposed frozen path identities and current fixed inputs | Closed schema-valid inventory with every ordered path-list digest recomputed | Drift is `FAIL` with a stable inventory code |
| V19 future publication | Committed direct-child five-path checkpoint candidate with committed passing XML | Candidate-derived V19 document; exact historical nonconformance and checkpoint-only effect | Missing history is `BLOCKED`; path, gitlink, result, ledger, or candidate drift is `FAIL` |
| V20 future publication | Valid committed V19, committed entry inputs, and explicit independent owner fields | Separate exact-one-path V20 document unlocking only `7.1` | Missing/stale/invalid inputs block publication and leave effective hold `ACTIVE` |
| Mutation | One authority boundary is altered in a hermetic fixture | Named stable blocker, nonempty result ledger, byte-identical fixture restoration | No mutation may pass vacuously |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/publish_v18_package_environment_authority.py` -- reuse candidate-before-record, bounded Git, committed-blob, exact-scope, schema, atomic-write, and result-state patterns without modifying V18.
- `_bmad/scripts/publish_v9_planning_authority.py` -- reuse canonical inventory digest and independently recompute the V9 internal bundle digest; consume V9 read-only.
- `_bmad/scripts/verify_epic_6_completion_supersession.py` -- reuse the exact root `.gitmodules` inventory and raw mode-`160000` tree-entry model.
- `_bmad/scripts/tests/test_publish_v18_package_environment_authority.py` -- reuse hermetic transaction fixtures and stable-code mutation style.
- `_bmad/scripts/tests/test_generate_story_record.py` -- reuse recursive schema-closure and mutation walkers; do not change Story 7.1 tests.
- `_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` -- authoritative ordered checkpoint paths and command, read-only.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/schemas/v19-story-7.1-checkpoint-completion-authority-v1.schema.json` -- add a recursively closed Draft 2020-12 V19 contract with exact identities, preserved evidence, historical partitions, fresh candidate evidence, ledger, and checkpoint-only effects.
- [x] `_bmad/schemas/v20-story-7.1-release-owner-authority-v1.schema.json` -- add a recursively closed V20 contract requiring independent owner fields, committed predecessor/input bindings, non-interchangeable candidate roles, exact `unlocks: [7.1]`, and narrow effects.
- [x] `_bmad/schemas/v20-story-7.1-input-inventory-v1.schema.json` and `_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json` -- encode and validate every exact implementation/result/output/scenario inventory, canonical digest rule, fixed input binding, and candidate-derived binding rule from proposal sections 5.4-5.6.
- [x] `_bmad/scripts/publish_story_7_1_successor_authorities.py` -- implement explicit inventory, V19, and V20 publish/check routes plus effective-hold validation; derive all authority facts from committed objects and require explicit owner inputs for V20 writes.
- [x] `_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py` -- cover schemas, inventory, future transactions, exact paths, raw gitlinks, committed XML parsing, distinct roles/effects, failure-state semantics, every named proposal mutation, and restoration.

**Acceptance Criteria:**
- Given the six-file tooling candidate, when focused schema and inventory checks run, then all schemas are recursively closed and every proposal identity, ordered path list, digest, role, and effect is exact.
- Given a hermetic valid checkpoint transaction, when V19 is rendered and checked, then committed evidence produces a nonempty `PASS` ledger while `b819a7c` remains `NONCONFORMING` and Story 7.1 remains held.
- Given valid committed V19/entry transactions and explicit owner data, when V20 is rendered and checked, then only Story 7.1 is lifted; absent or mutated evidence returns `ACTIVE` with the specified `FAIL` or `BLOCKED` result.
- Given the final worktree, when exact-path and protected-byte audits run, then only the six authorized files plus this workflow spec were created, protected hashes and user changes are unchanged, and no authority record, checkpoint result, sprint status, or loop marker was written.

## Implementation Notes

- Added only the six approved successor-authority files. Live V19/V20 records remain absent; the current effective-hold check returns `BLOCKED`/`ACTIVE` with a nonempty ledger.
- V19 binds all five checkpoint blobs, independently confirms the historical result blob is absent from `b819a7c`, and records V17 by its actual authority ID.
- Focused verification passed 16 tests; inventory validation and the 14-test V18 regression lane also passed. Protected hashes, the modified Folders gitlink, sprint status, ignored checkpoint XML, and loop state remain unchanged.
- Review lifecycle gate: `python3 _bmad/scripts/verify_submodule_promotion.py --repository /home/administrator/projects/hexalith/conversations --baseline 64b050831eea694cb2065342cf23a150646eef57 --candidate HEAD` exited `1`/`BLOCKED` with `UNCAPTURED_SUBMODULE_PROMOTION` because the protected user checkout `references/Hexalith.Folders` is at `df0b636a6194b5529b566f0f579f1cebfa7f48c6` while the recorded gitlink remains `adb3831c17f70a7483f171877bf60086ddf91531`. The companion evidence-boundary command exited `0`/`PASS` with a nonempty ledger. Per the workflow, status remains `in-progress` and review/done transitions did not run.

## Spec Change Log

## Review Triage Log

## Design Notes

Use one dedicated CLI with explicit `inventory`, `v19`, and `v20` routes. Check routes are read-only and validate committed publication transactions; write routes use atomic replacement. V20 write requires caller-supplied release-owner identity, UTC instant, rationale, and concrete entry candidate, so the tooling never invents the decision.

## Verification

**Commands:**
- `uv run --frozen --no-cache pytest -q _bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py` -- expected: focused publisher/schema/mutation suite passes with nonzero tests.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . inventory --check` -- expected: inventory schema and every canonical path digest pass.
- `uv run --frozen --no-cache pytest -q _bmad/scripts/tests/test_publish_v18_package_environment_authority.py` -- expected: predecessor publisher regression suite passes.
- `git diff --check` -- expected: no whitespace errors.
- `git status --short` plus protected `sha256sum` and exact-path comparison -- expected: declared user changes preserved and no forbidden path changed.
