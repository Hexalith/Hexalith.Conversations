---
title: 'Define the final-record schema and deterministic generator core'
type: 'feature'
created: '2026-09-12'
status: 'blocked'
baseline_revision: '64b050831eea694cb2065342cf23a150646eef57'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/v9/story-contracts/7.1.json'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
warnings:
  - 'oversized'
deferred: []
---

<intent-contract>

## Intent

**Problem:** The repository has the closed v2 schema files, but its final-record generator remains v1-only and cannot execute Story 7.1's six candidate-bound acceptance scenarios or emit the authoritative JSON and deterministic Markdown pair.

**Approach:** Consume, but do not create or publish, two committed additive successor authorities before any Story 7.1 implementation begins: V19 establishes trustworthy `7.1-SCHEMAS` completion while leaving the full-story hold active, then an independently human-authored V20 explicitly unlocks only full Story 7.1. After both validate, add an isolated contract-driven v2 route that reuses the hardened root, Git, path, snapshot, and rendering seams while preserving every v1 behavior. Do not edit or reinterpret V17, V18, the existing hold record, or the mixed checkpoint history.

## Boundaries & Constraints

**Always:** Preserve Story 7.1's V10 authority pair, planning candidate, authority-bundle digest, frozen inventory, rollback text, and six-scenario order. Derive candidate facts and all ten root gitlinks from raw committed Git objects; require current, nonempty machine results and a nonempty assertion ledger; validate emitted JSON against the closed schema; render and hash Markdown deterministically; preserve v1 compatibility; keep `sprint-status.yaml` read-only. Keep the four published evidence contracts distinct from the auxiliary operational failure schema. For v2, compute `outputs.json.sha256` from the exact deterministic JSON serialization with only that value replaced by 64 ASCII zeroes, and require `outputs.markdown.sha256` and `renderedMarkdownSha256` both to equal the SHA-256 of the exact emitted Markdown bytes. AC-7.1-06 executes AC-7.1-01 through AC-7.1-05 in frozen order and binds their measured results; it does not accept caller-supplied result paths or facts.

**Authority gate:** Require committed, schema-valid `PASS` records at `_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json` and `_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json`. V19 must bind a fresh V11-exact checkpoint candidate, record the historical mixed transaction as `NONCONFORMING`, set only `checkpointComplete: true`, and keep the implementation hold active. V20 must bind that exact V19 raw digest, an independently supplied release-owner `LIFTED` decision, `unlocks: [7.1]`, and `_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json`; it must lift only Story 7.1, resolve its candidate binding, and must not mark the story done or unlock Story 7.2, release, or push. The V9 planning candidate, V19 checkpoint candidate, V20 entry candidate, V20 publication baseline, and eventual `SC-7.1` are distinct and non-interchangeable. The V20 publication baseline is the unique single-parent child of the entry candidate whose exact changed path is the V20 authority record; `SC-7.1` is the later committed implementation candidate descended from that baseline. Missing, stale, invalid, non-`PASS`, or scope-widened V19/V20 evidence leaves the hold active and Story 7.1 unimplementable.

**Digest timing:** The V20 inventory freezes ordered path membership and these path-list digests: `V20-7.1-IMPLEMENTATION-PATHS-v1` = `4405332b49ec26ffff40c9b7424c858e7634e0d92313b2c0270011e29a132f4b`; `V20-7.1-RESULT-PATHS-v1` = `f54595279fc201056604e56971f2e22f5483c505ddb1a2488b69e513f08b0fef`; `V20-7.1-RECORD-OUTPUT-PATHS-v1` = `80b1c47320b8f4ebb98e0dfb40d6777cded1e4e08541c7734678991ab5598b64`; AC-7.1-01 = `78a2fc7f6a53273a4c85ecc2a476cfc88c7896a86f5a6a8fd69eacc44c2540c7`; AC-7.1-02, AC-7.1-03, and AC-7.1-04 each = `4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908`; AC-7.1-05 = `2ba87bf3475e3dc799fafbefb30a132c7950057abe98eb887a107f59f6016e26`; and AC-7.1-06 = `ab3e8bb068cf5fc8b1ade9225f004d6b8dcbbccc15eb59657b1af4bfcfbfb91c`. Before implementation, recompute and validate the committed raw digests of V19, V20, the V20 inventory and schema, and this amended semantic spec. Do not predeclare future raw digests for Story-owned files: derive their committed blob digests at `SC-7.1`, and derive each JUnit digest from the byte snapshot produced by its frozen command.

**JUnit ledger mapping:** Accept exactly one `<testsuites>` root with exactly one direct `<testsuite>`, and visit that suite's direct `<testcase>` children in document order. For the one-based testcase ordinal `n`, emit `id` as `<scenarioId>#<n padded to four digits>`, `subject` as the exact parsed nonempty `classname` value plus `::` plus the exact parsed nonempty `name` value, and `state` as `PASS` only when the testcase has no direct `<failure>`, `<error>`, or `<skipped>` child; otherwise emit `FAIL`. Ignore timing, timestamp, hostname, file, and other incidental JUnit attributes. A malformed suite shape, missing or empty identity attribute, summary/detail inconsistency, or zero testcase cannot produce a passing ledger.

**Never:** Create or publish V19 or V20 as part of Story 7.1; start Story 7.1 implementation before both committed authorities validate; use a self-digest convention, failure shape, or scenario-result mapping other than the rules frozen here; weaken a closed schema or frozen authority; accept caller-authored counts, paths, commits, verdicts, digests, exit codes, or assertion ledgers; traverse or change submodules; rewrite accepted evidence; implement Stories 7.2-7.4; or treat an orchestrator status row as verification.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Deterministic bundle | Identical ordered contract, candidate, inventory, and five passing results | Byte-identical schema-valid JSON and Markdown; recorded Markdown digest matches | Drift yields `FAIL` and `RECORD_CONTENT_DRIFT` |
| Authority entry gate | Committed V19 checkpoint authority followed by committed independent V20 release-owner authority | Both validate as current `PASS`; V20 unlocks only `7.1`; implementation starts from the unique V20 publication baseline | Any missing, stale, invalid, non-`PASS`, candidate-mismatched, or widened authority keeps the hold active; do not implement |
| JUnit ledger | One pytest `<testsuites>` root, one direct suite, and nonempty direct testcase children | Rows use frozen document order, scenario-plus-ordinal IDs, exact `classname::name` subjects, and child-element-derived states | Malformed shape, missing identity, inconsistent counts, or zero testcases yields no passing ledger; failures/errors and skips remain nonpassing under their applicable blocker semantics |
| Caller facts | Counts, paths, commits, or verdict supplied as caller input | No passing record | Exit 1, `FAIL`, `CALLER_AUTHORED_FACT` |
| Empty derivation | No parsed result, candidate, path, or executed assertion | No passing record | Exit 1, `FAIL`, `RECORD_NOT_DERIVED` and `ASSERTION_LEDGER_EMPTY` |
| Invalid input | Malformed JSON, unknown schema identity, or invalid arguments before story/scenario/candidate identity is available | A `hexalith.conversations.story-record-generator-failure.v1` document is written to stdout; neither declared final-record output is created or overwritten | Exit 1 with exact `INPUT_SCHEMA_INVALID` or `ARGUMENT_INVALID`; no story, scenario, candidate, command, input, output, or path field and no message, payload, or traceback |

</intent-contract>

## Code Map

- `_bmad/scripts/generate_story_record.py:21` -- v1-only identities; add the v2 route without changing the legacy route's behavior.
- `_bmad/scripts/generate_story_record.py:225` -- reuse hardened Git execution, root-relative path containment, byte snapshots, commit resolution, root `.gitmodules`, and raw tree-entry helpers.
- `_bmad/scripts/generate_story_record.py:2659` -- current bundle writer emits stdout only; v2 must write the two contract-declared outputs atomically and deterministically.
- `_bmad/scripts/generate_story_record.py:2688` -- CLI lacks `--contract`, `--output-json`, and `--output-markdown`; route v2 before legacy argument handling.
- `_bmad/scripts/tests/test_generate_story_record.py:191` -- reuse the hermetic repository fixture and subprocess patterns.
- `_bmad/scripts/tests/test_generate_story_record.py:1462` -- representative v2 instances, schema identities, inventory digest, and exact ten-gitlink ordering already exist.
- `_bmad/schemas/story-final-record-v2.schema.json:7` -- closed authoritative output shape; its JSON output digest is presently self-referential.
- `_bmad/schemas/story-record-generator-failure-v1.schema.json` -- add the closed auxiliary pre-identity failure shape; it is operational output, not a fifth final/evidence contract.
- `_bmad-output/planning-artifacts/v9/story-contracts/7.1.json:25` -- six exact scenarios, commands, result semantics, output paths, and final `6/6/0/0/0/0` summary.
- `_bmad-output/planning-artifacts/implementation-hold-v1.json:10` -- current lift is non-global and unlocks only `7.1-SCHEMAS`.
- `_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json:213` -- current authority keeps the implementation hold active and Story 7.1 candidate binding unresolved.
- `_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json` -- required committed checkpoint-completion authority; consume read-only and require current `PASS` with checkpoint-only effect.
- `_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json` -- required committed independent release-owner authority; consume read-only and require current `PASS` that unlocks only `7.1`.
- `_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json` -- required committed ordered path inventory and digest-timing authority for Story 7.1 and each scenario.
- `_bmad-output/implementation-artifacts/spec-7-1-schemas.md:1` -- historical checkpoint record; its status and mixed transaction are not completion authority and must remain unchanged.
- `docs/runbooks/story-final-record-generation.md:1` -- document every new stable code and v2 invocation without weakening v1 history.

## Tasks & Acceptance

**Execution:**
- Additive successor authorities -- consume V19 and V20 read-only at the exact paths above; do not create or publish either in this story. Before changing any Story-owned implementation path, require V19 to validate the fresh checkpoint evidence and keep the full-story hold active, then require a separate independently human-authored V20 to validate that V19, bind the amended semantic spec and committed V20 inventory, unlock only `7.1`, and establish the unique publication baseline from which `SC-7.1` must descend.
- `_bmad-output/implementation-artifacts/spec-7-1-schemas.md` -- preserve it as historical provenance and do not treat its status or mixed checkpoint commit as completion. Require V19 to bind the V9 planning candidate and bundle, V11 authority, exact five-path checkpoint set, raw root gitlinks, committed machine-result digest, and nonempty assertion ledger, and to record the mixed transaction as `NONCONFORMING` without rewriting it.
- `_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json` -- require the committed schema-valid inventory to contain the exact implementation, result, output, and six scenario path-list identities and digests frozen above. Treat pre-entry authority/inventory/spec raw digests as committed V20 bindings, Story-owned raw digests as candidate-derived at `SC-7.1`, and JUnit raw digests as command-produced byte-snapshot facts.
- `_bmad/schemas/story-final-record-v2.schema.json` -- preserve the closed output shape while defining `outputs.json.sha256` as the zero-placeholder self-excluding digest. Tighten each embedded scenario row to carry the complete validated acceptance-result v1 object for AC-7.1-01 through AC-7.1-05.
- `_bmad/schemas/story-record-generator-failure-v1.schema.json` -- add a recursively closed schema with exactly required `schemaVersion`, `result`, and `blockers`: identity `hexalith.conversations.story-record-generator-failure.v1`, result const `FAIL`, and a nonempty unique blocker array restricted to `INPUT_SCHEMA_INVALID` and `ARGUMENT_INVALID`. The generator must emit blockers in ordinal order.
- `_bmad/scripts/generate_story_record.py` -- implement the additive deterministic v2 contract route and preserve all v1 semantics. Only an exact `--contract` or `--contract=...` option selects v2 parsing and its auxiliary failure document; legacy invocations retain their existing error shape and exit semantics. For AC-7.1-06, resolve one committed `SC-7.1`, execute the five frozen scenario commands in order, require exactly one ID-derived `--junitxml=artifacts/v9/7.1/<scenario-id>.xml` per command, capture the real exit code, snapshot and hash that XML, and derive a nonempty assertion ledger from its top-level test cases in document order. Build and schema-validate each acceptance-result object without accepting caller-authored evidence.
- `_bmad/scripts/tests/test_generate_story_record.py` -- cover AC-7.1-01 through AC-7.1-05, all named negative faults, non-vacuity, byte restoration, and v1 regression.
- `docs/runbooks/story-final-record-generation.md` -- publish the exact v2 command, output, digest, and blocker semantics.
- `docs/release-evidence/story-7.1-final-record-v2.json` and `docs/release-evidence/story-7.1-final-record-v2.md` -- generate, never hand-author, the candidate-bound AC-7.1-06 pair after AC-7.1-01 through AC-7.1-05 pass against the frozen candidate.

**Acceptance Criteria:**
- Given the current V17/V18/hold history, when Story 7.1 entry is evaluated, then implementation remains blocked until committed V19 and V20 records at the exact frozen paths both validate as current `PASS`, V19 proves only fresh checkpoint completion, V20 carries an independent human `LIFTED` decision for exactly `[7.1]`, and the work baseline is the unique exact-one-path V20 publication commit. The planning, checkpoint, entry, publication-baseline, and eventual `SC-7.1` candidates must not be substituted for one another.
- Given the four canonical closed schemas, when `v2_schema_contract` runs, then Draft 2020-12 validation, representative instances, closure, identities, patterns, ordering, uniqueness, and byte restoration all pass.
- Given identical ordered inputs and fixed time, when `v2_deterministic_bundle` runs twice, then JSON and Markdown bytes are identical. The JSON writer uses schema/member order, two-space indentation, UTF-8 without BOM, LF line endings, and exactly one terminal LF; `outputs.json.sha256` hashes those bytes with only its value set to 64 ASCII zeroes before final substitution. `outputs.markdown.sha256` and `renderedMarkdownSha256` both equal the raw SHA-256 of the emitted Markdown UTF-8/LF bytes, including exactly one terminal LF.
- Given attempted caller facts, when `v2_rejects_caller_authored_facts` runs, then the generator exits 1 with `FAIL` and `CALLER_AUTHORED_FACT` and emits no passing record.
- Given empty derivation, when `v2_rejects_empty_derivation` runs, then the generator exits 1 with `FAIL`, `RECORD_NOT_DERIVED`, and `ASSERTION_LEDGER_EMPTY`.
- Given each invalid-input fixture, when `v2_malformed_input_is_schema_valid_failure` runs, then the generator exits 1 and emits exactly one auxiliary failure document to stdout with the exact required blocker. It emits no story, scenario, candidate, command, input, output, or path field and no message, payload, or traceback, and it does not create or overwrite either final-record output. Malformed JSON and an unknown contract schema identity map to `INPUT_SCHEMA_INVALID`; invalid v2 CLI syntax or options map to `ARGUMENT_INVALID`.
- Given AC-7.1-01 through AC-7.1-05, when AC-7.1-06 executes their exact frozen commands, then each result binds the one resolved `SC-7.1`, exact command, captured exit code, result semantics, ordinal blockers, exact frozen input-path inventory with raw digests derived at the prescribed time, one ID-derived JUnit output path and byte-snapshot digest, and a unique nonempty testcase assertion ledger. The ledger must be derived only from the single direct pytest suite's direct testcase children in document order using `<scenarioId>#<four-digit one-based ordinal>`, exact parsed `classname::name`, and child-element-derived `PASS`/`FAIL`; incidental JUnit metadata must not affect it. Before development starts, the committed V20 inventory must carry the exact nine path-list identities (three global and six scenario-specific) with the seven digest values frozen in this spec.
- Given five current passing embedded acceptance results bound to frozen `SC-7.1`, when the canonical bundle command finalizes successfully, then it emits the schema-valid JSON/Markdown pair binding the planning candidate, bundle digest, candidate, ten raw gitlinks, inventory, rollback, and summary `6/6/0/0/0/0`. The `scenarios` array contains exactly AC-7.1-01 through AC-7.1-05 in contract order; AC-7.1-06 is counted as the sixth pass only after schema validation, atomic writes, and digest verification succeed.

## Spec Change Log

- `2026-09-13` -- Human selected the separate V19-to-V20 authority chain. Story 7.1 now consumes both committed authorities as prerequisites, freezes candidate roles and digest timing, and defines one deterministic JUnit testcase-to-ledger mapping.

## Review Triage Log

## Design Notes

Human resolution selected the separate additive V19-to-V20 successor-authority path and approved the following rules:

- Preserve V17, V18, the existing hold record, and the mixed checkpoint history as point-in-time evidence. V19 at `_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json` must record that the mixed checkpoint transaction is `NONCONFORMING`, bind fresh completion evidence to the exact V11 boundary, and leave the full-story hold active. Only afterward may an independently human-authored V20 at `_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json` bind V19 and the committed inventory, explicitly unlock only `7.1`, and establish the unique publication baseline. Story 7.1 consumes but never creates or publishes either authority.
- The V9 planning candidate, V19 checkpoint candidate, V20 entry candidate, V20 publication baseline, and eventual `SC-7.1` are non-interchangeable. `SC-7.1` is unknown at the V20 decision and must later descend from the unique exact-one-path V20 publication commit.
- `_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json` freezes ordered path membership and path-list digests. V20 freezes the committed pre-entry authority, inventory, schema, and amended semantic-spec raw digests; Story-owned blob digests are derived at committed `SC-7.1`; JUnit digests are derived from the exact command-produced snapshots. No future raw digest is fabricated in advance.
- `outputs.json.sha256` is a self-excluding digest over the exact prescribed JSON bytes with only that property value replaced by 64 ASCII zeroes. It is not the raw digest of the finished JSON file. Any downstream acceptance-result binding of the JSON file records its actual raw-file SHA-256.
- Pre-identity v2 input and argument failures use the auxiliary `hexalith.conversations.story-record-generator-failure.v1` schema and never fabricate story, scenario, candidate, command, input, output, or path fields.
- AC-7.1-06 is the evidence producer and consumer for AC-7.1-01 through AC-7.1-05: it executes their exact commands, derives their artifact paths from their IDs, captures their actual exits, hashes their byte snapshots, maps the single pytest suite's direct testcase children to ledger rows by the frozen scenario-plus-ordinal and `classname::name` rule, validates complete acceptance-result v1 objects, and embeds them in the final record. Raw-XML-only discovery and caller-produced result sidecars are not authoritative.

This resolves the authority ownership, candidate-role, digest-timing, pre-identity failure, and scenario-mapping ambiguities. This spec does not itself lift the hold or certify the checkpoint. Re-arm and implementation must wait until the separately produced committed V19 and independently human-authored committed V20 both validate; otherwise a new dev session must remain blocked rather than attempting either authority transaction.

## Verification

**Commands:**
- `python3 _bmad/scripts/publish_v18_package_environment_authority.py --repository . --candidate HEAD --check` -- expected: `V18_PACKAGE_ENVIRONMENT_AUTHORITY_OK` before relying on current authority.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 64b050831eea694cb2065342cf23a150646eef57 --candidate <committed-candidate>` -- expected: exit 0, `PASS`, and a nonempty assertion ledger before review or finalization.
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k '<canonical Story 7.1 selector>' --junitxml=artifacts/v9/7.1/<scenario>.xml` -- expected: nonzero test count with no failures, errors, or skips for each AC-7.1-01 through AC-7.1-05 command.
- `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.1.json --format bundle --output-json docs/release-evidence/story-7.1-final-record-v2.json --output-markdown docs/release-evidence/story-7.1-final-record-v2.md` -- expected: exit 0 and a generated `PASS` bundle only after all prerequisites and prior scenarios are current.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: blocked

Blocking condition: intent gap

Evidence: the current implementation-hold record is non-global and unlocks only `7.1-SCHEMAS`; V18 keeps the full-story hold active and `story71CandidateBindingResolved` false; the checkpoint record remains `in-progress` and its observed commit range crossed prohibited V11 paths; and the accepted output/error contracts do not define satisfiable self-digest or pre-identity failure semantics.

Unanswered questions: the five authority questions in Design Notes must be resolved without locally rewriting frozen planning evidence.

`awaiting-operator` does not apply: no Story 7.1 acceptance criterion requires an outside-repository human action, and the unresolved authority and output-contract semantics prevent a safe agent-implementable slice from starting.
