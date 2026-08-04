---
title: 'Define the final-record v2 schema contracts'
type: 'feature'
created: '2026-08-03'
status: 'blocked'
baseline_revision: '2a2387727323d96b6bc493e28ca6570488e64263'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/v9/story-contracts/7.1.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Published v9 authority names four closed final-record contracts, but only the story-contract schema exists. The acceptance-result, frozen-inventory, and final-record v2 documents therefore have no machine-enforced shape.

**Approach:** Add and test the three missing Draft 2020-12 schemas as a schema-only preparatory slice. Keep generator implementation deferred and do not claim Story 7.1 complete from this slice.

## Boundaries & Constraints

**Always:** Preserve `_bmad/schemas/v9-story-contract-v1.schema.json` byte-for-byte and use it as the closure/style precedent. Make every new object recursively closed with explicit required fields, stable schema identity, strict lowercase commit/SHA-256 patterns, normalized repository-relative paths, and deterministic array constraints. Keep the implementation hold effective while `ACTIVE`; execution also requires an approved authority amendment that separates this slice from the currently coupled Story 7.1 contract.

**Ask First:** Halt if the amended authority is absent or candidate-drifted at implementation time, a required field is ambiguous across v9 sources, or validation would require changing an authority command or introducing an unavailable runtime dependency.

**Never:** Modify `_bmad/scripts/generate_story_record.py`; create acceptance-result artifacts or Story 7.1 final-record outputs; implement Stories 7.2–7.4; rewrite Story 6.8, completed records, planning candidates, accepted evidence, production/public contracts, package versions, submodule content, or gitlinks; initialize, update, fetch, enter, or traverse submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Valid documents | Complete instances for each canonical schema identity | Draft 2020-12 metaschema and instance validation pass | No diagnostic |
| Missing or extra field | Required field removed or undeclared property inserted at any object depth | Instance is rejected | Stable test diagnosis maps the failure to `OUTPUT_SCHEMA_INVALID` |
| Invalid binding | Uppercase/short commit, malformed digest, absolute/backtracking path, duplicate constrained item, or wrong schema identity | Instance is rejected | No permissive coercion or fallback identity |
| Invalid schema | A schema is made internally inconsistent or permissive | Metaschema/closure test fails | No generated output is treated as valid |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:2349` -- canonical identities, field families, ordering, and closed-schema authority.
- `_bmad-output/planning-artifacts/v9/story-contracts/7.1.json` -- candidate-bound Story 7.1 source; consume read-only after the split amendment is published.
- `_bmad/schemas/v9-story-contract-v1.schema.json` -- existing closed Draft 2020-12 pattern and immutable first schema.
- `_bmad/schemas/v9-acceptance-result-v1.schema.json` -- add closed scenario-result, assertion-ledger, result-semantics, diagnostic, and binding shapes.
- `_bmad/schemas/v9-frozen-inventory-v1.schema.json` -- add ordered NFC UTF-8/LF inventory identity, items, and digest shape.
- `_bmad/schemas/story-final-record-v2.schema.json` -- add authoritative authority/candidate/gitlink, input/output digest, predecessor, inventory, rollback, scenario-summary, blocker, and rendered-Markdown digest shapes.
- `_bmad/scripts/tests/test_generate_story_record.py:49` -- reuse hermetic fixtures and add the exact `v2_schema_contract` selector with positive and negative schema mutations.
- `_bmad/scripts/tests/test_publish_v9_planning_authority.py:56` -- reuse `Draft202012Validator.check_schema`, instance rejection, and byte-restoration assertion patterns.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/schemas/v9-acceptance-result-v1.schema.json` -- define the recursively closed per-scenario result contract required by v9 result semantics.
- [ ] `_bmad/schemas/v9-frozen-inventory-v1.schema.json` -- define the recursively closed ordered inventory and canonical digest contract.
- [ ] `_bmad/schemas/story-final-record-v2.schema.json` -- define the recursively closed authoritative final-record v2 contract without embedding caller-authored facts.
- [ ] `_bmad/scripts/tests/test_generate_story_record.py` -- add `v2_schema_contract` fixtures proving metaschema validity, valid instances, required-field closure, nested extra-field rejection, identities, bindings, patterns, uniqueness, and fixture restoration.

**Acceptance Criteria:**
- Given the four canonical schema paths and amended schema-only authority, when `v2_schema_contract` runs, then every schema passes Draft 2020-12 metaschema validation and representative complete instances validate.
- Given each required or nested closure constraint, when one field is removed or one undeclared field is injected, then validation fails and the test records `OUTPUT_SCHEMA_INVALID` semantics.
- Given malformed identities, commits, digests, paths, ordering, or uniqueness, when negative fixtures validate, then none is accepted and every mutated fixture is restored byte-identically.
- Given the existing story-contract schema and protected planning bundle, when the slice completes, then their bytes and candidate-bound digests remain unchanged and no generator/result/final-record file is produced.

## Spec Change Log

## Design Notes

Use shared `$defs` inside each schema rather than cross-file `$ref` resolution so validation is deterministic from a single file. Keep the three identities distinct from the planning-publication `hexalith.conversations.v9-inventory.v1` schema.

## Verification

**Commands:**
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml` -- expected: `PASS`, with all positive and negative schema assertions executed.
- `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` -- expected: `V9_PLANNING_AUTHORITY_OK`, proving protected publication bytes remain unchanged.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: blocked
Blocking condition: The frozen implementation gate is unsatisfied: no approved schema-only authority amendment exists, the canonical Story 7.1 contract still couples schemas with generator and final-record outputs, the global implementation hold remains `ACTIVE`, and `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` exits 1 with `OUTPUT_DRIFT: _bmad-output/implementation-artifacts/sprint-status.yaml`.
