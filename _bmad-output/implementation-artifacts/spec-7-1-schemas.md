---
title: 'Add the three closed 7.1-SCHEMAS contracts'
type: 'feature'
created: '2026-09-08'
status: 'in-progress'
baseline_commit: '73bcee6f04479d4743d5a65ce929728e22687d7d'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 7.1 names four closed Draft 2020-12 contracts, but only the story-contract schema exists. The schema checkpoint cannot run, and later generator work has no machine-enforced shapes.

**Approach:** Add the three missing closed schemas and prove all four through `v2_schema_contract` only. Leave the generator, Story 7.1 final records, and sprint-status rows unchanged.

## Boundaries & Constraints

**Always:** Keep `_bmad/schemas/v9-story-contract-v1.schema.json` byte-identical. Use it as the closure, `$id`, `$defs`, commit, and digest precedent. Published required top-level fields are: acceptance-result `schemaVersion`, `storyId`, `scenarioId`, `command`, `exitCode`, `result`, `blockers`, `candidate`, `inputs`, `outputs`; frozen-inventory `schemaVersion`, `inventoryId`, `digestAlgorithm`, `canonicalization`, `items`, `sha256`; final-record v2 `schemaVersion`, `storyId`, `authority`, `candidate`, `predecessors`, `inventory`, `scenarios`, `faultInjection`, `outputs`, `rollback`, `summary`, `renderedMarkdownSha256`. Decision: acceptance-result lists a closed optional `assertionLedger` of `{id, subject, state}`; missing or empty is schema-valid, and a nonempty ledger remains a later generator PASS rule, not a schema requirement. Identities are `hexalith.conversations.acceptance-result.v1`, `hexalith.conversations.frozen-inventory.v1`, and `hexalith.conversations.story-final-record.v2`. Recursively close every object. Use lowercase 40-char commits and 64-char SHA-256. Paths are repository-relative, slash-separated, and reject `..` and backslash. Blocker arrays are unique and ordinally sorted. Inventory digest is SHA-256 over NFC UTF-8 obligation IDs, one ID plus LF, displayed order. Valid instances live only in memory. Before editing, confirm `implementation-hold-v1.json` is still `LIFTED` for `7.1-SCHEMAS` at PC `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`, bundle `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055`, and IR-0 `862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad`; drift is `BLOCKED`. Leave the three dirty `references/` gitlinks untouched.

**Ask First:** Halt if implementation finds another required nested field that still conflicts across v9 sources.

**Never:** Edit `_bmad/scripts/generate_story_record.py`, `.gitmodules`, `Directory.Packages.props`, `src/`, `tests/`, `references/`, `docs/release-evidence/`, `artifacts/v9/7.1/`, `_bmad-output/planning-artifacts/`, or sprint-status. Do not run `AC-7.1-02`–`AC-7.1-06`, write an acceptance-result or final-record file, mark Story 7.1 `done`, unlock 7.2, initialize or traverse submodules, or treat this checkpoint as a story final record.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Valid documents | In-memory complete instances for each new identity plus the existing story-contract schema | Draft 2020-12 metaschema and instance validation pass | No diagnostic |
| Missing or extra field | Required field removed or undeclared property inserted at any object depth | Instance rejected | Map to `OUTPUT_SCHEMA_INVALID`; restore bytes |
| Invalid binding | Uppercase or short commit, malformed digest, absolute or `..` path, duplicate constrained item, or wrong schema identity | Instance rejected | No coercion |
| Invalid schema | A schema is made internally inconsistent or permissive | Metaschema or closure test fails | No generated output treated as valid |
| Hold drift | PC, bundle, or IR-0 digest no longer matches the lift record | Do not implement | `BLOCKED`; treat hold as `ACTIVE` |

</frozen-after-approval>

## Code Map

- `_bmad/schemas/v9-story-contract-v1.schema.json` -- immutable precedent: Draft 2020-12, `additionalProperties: false`, `$defs/commit` and `$defs/digest`. Do not change.
- `_bmad/schemas/v9-inventory-v1.schema.json` -- planning inventory only. Do not reuse its identity or `rows` shape.
- `_bmad/schemas/v9-acceptance-result-v1.schema.json` -- add. `$id` under `https://hexalith.io/schemas/conversations/`. Optional closed `assertionLedger`; do not require it.
- `_bmad/schemas/v9-frozen-inventory-v1.schema.json` -- add. `digestAlgorithm` const `sha256`; `canonicalization` const `nfc-utf8-lf-displayed-ids`; `items` unique nonempty obligation-ID strings.
- `_bmad/schemas/story-final-record-v2.schema.json` -- add. Nested `authority` binds epic/architecture/PC/bundle digest; `candidate` binds commit plus ten raw-mode `160000` gitlinks; `summary` uses `required`/`passed`/`failed`/`blocked`/`skipped`/`notRun`; `faultInjection.results` is a unique array of `{id, expectedBlocker}`.
- `_bmad/scripts/tests/test_generate_story_record.py` -- add `-k v2_schema_contract` only. Keep existing v1 generator tests. Do not import or call the generator for this selector.
- `_bmad/scripts/tests/test_publish_v9_planning_authority.py` -- reuse `Draft202012Validator.check_schema` / `.validate` and `path.read_bytes() == before` restore. Do not add the new schemas to `SCHEMA_PATHS`.
- `_bmad/scripts/generate_story_record.py` -- prohibited. Remains v1 (`SCHEMA = "story-final-record-v1"`).
- `_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` -- writable/prohibited paths and checkpoint command. Read-only.
- `_bmad-output/planning-artifacts/v9/story-contracts/7.1.json` -- full-story contract. Do not satisfy AC-7.1-02–06 here.
- `_bmad-output/planning-artifacts/implementation-hold-v1.json` -- lift binding. Read-only.
- `references/Hexalith.FrontComposer`, `references/Hexalith.Memories`, `references/Hexalith.Projects` -- pre-existing dirty gitlinks. Do not stage, commit, revert, or update.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/schemas/v9-acceptance-result-v1.schema.json` -- define the closed per-scenario result contract with optional closed `assertionLedger`.
- [ ] `_bmad/schemas/v9-frozen-inventory-v1.schema.json` -- define the closed ordered inventory and canonical digest contract.
- [ ] `_bmad/schemas/story-final-record-v2.schema.json` -- define the closed authoritative v2 record contract with no caller-authored counts, paths, commits, or verdicts as free text.
- [ ] `_bmad/scripts/tests/test_generate_story_record.py` -- add `v2_schema_contract` covering metaschema, valid in-memory instances, required-field and nested extra-field rejection, identities, bindings, patterns, uniqueness, `OUTPUT_SCHEMA_INVALID` mapping, and byte-identical fixture restore.
- [ ] `artifacts/v9/schema-slice/v2-schema-contract.xml` -- produced only by the checkpoint pytest command, never hand-authored.

**Acceptance Criteria:**
- Given the four canonical schema paths and a still-valid 7.1-SCHEMAS lift, when `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml` runs, then exit is `0` and every schema plus representative complete instances pass Draft 2020-12 validation.
- Given each required or nested closure constraint, when one required field is removed or one undeclared field is injected, then validation fails, the test records `OUTPUT_SCHEMA_INVALID`, and the mutated fixture is restored byte-identically.
- Given malformed identities, commits, digests, paths, ordering, or uniqueness, when negative fixtures validate, then none is accepted and every mutated fixture is restored byte-identically.
- Given the existing story-contract schema and protected planning bundle, when the slice completes, then their bytes stay unchanged, `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` still reports `V9_PLANNING_AUTHORITY_OK`, no generator or final-record file is produced, and Story 7.1 remains not `done`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Copy `$defs` into each new file; do not `$ref` another schema file. Keep frozen-inventory distinct from `hexalith.conversations.v9-inventory.v1`. Representative instances may use Story 7.1 inventory IDs `V8-6.8-AC1`, `V8-6.8-AC6-ANTI-VACUITY`, and `V8-6.8-PROHIBITIONS-SOURCE-BOUNDARY` and the ten root gitlink paths from `.gitmodules`. Prove one valid acceptance-result with no `assertionLedger` and one with a well-formed ledger; do not treat an empty or missing ledger as `OUTPUT_SCHEMA_INVALID`. `faultInjection` records named faults only; do not execute the Story 7.4 matrix. Add `jsonschema` to this test file's PEP 723 dependencies so `v2_schema_contract` can call `Draft202012Validator` without touching the generator.

## Verification

**Commands:**
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml` -- expected: exit `0`, nonempty JUnit at that path.
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py --junitxml=artifacts/v9/schema-slice/generator-regression.xml` -- expected: existing v1 generator tests still pass.
- `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` -- expected: `V9_PLANNING_AUTHORITY_OK`.
- `git diff --check` -- expected: no whitespace errors.
- `git diff -- references/Hexalith.FrontComposer references/Hexalith.Memories references/Hexalith.Projects` -- expected: unchanged from the pre-existing dirty pointers.

