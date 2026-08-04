# Epic 7 Context: Reliable Mechanical Completion Records

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Provide deterministic, candidate-bound completion records whose test counts, exact changed-path set, candidate identity, submodule condition, root gitlink state, and verdict are derived from machine results and Git objects rather than copied by a caller. This matters because a story must not reach review or done with vacuous scope, stale or incomplete tests, dirty or moved dependencies, displaced workflow integration, or rewritten historical evidence.

## Stories

- Story 7.1: Define the final-record schema and deterministic generator core
- Story 7.2: Derive test, path, candidate, submodule, and gitlink facts
- Story 7.3: Integrate generation into every blocking completion transition
- Story 7.4: Verify historical mode and required fault-injection blockers

## Requirements & Constraints

- Generate exactly one authoritative JSON final record and one digest-bound deterministic Markdown projection per story. Counts, commits, paths, gitlinks, and verdicts must never be accepted as caller-authored facts.
- Bind each story record to the planning candidate and authority-bundle digest, the story candidate root commit, baseline, all declared input/output hashes, predecessor final-record digests, frozen inventory, rollback boundary, scenario results, and every root-declared gitlink.
- Derive test totals from one current machine result for every required root-owned test project. Missing or stale artifacts, failures, unapproved skips, zero matching tests, non-run lanes, or an empty assertion ledger prevent `PASS`; environmental inability is `BLOCKED`, never success.
- Derive one exact committed path set and reject a second or divergent list, unrelated source dirt outside declared result/record allowances, or any path beneath a root submodule. Do not initialize, update, or traverse submodules.
- Resolve gitlinks from the story candidate's Git tree as raw mode `160000` entries and require exact equality with the ordinally sorted root `.gitmodules` inventory. Missing, extra, unresolved, non-gitlink, or moved bindings block completion; filenames that merely contain `160000` are irrelevant.
- After a story candidate is frozen, only its declared record outputs and machine-result inputs may differ. A later source commit, gitlink movement, untrustworthy baseline, candidate mismatch, stale input, or authority drift invalidates the record.
- Every governed review/done workflow and generated twin must invoke and verify the same generator before transition, with parity-checked commands, blocker handling, halt behavior, output paths, and insertion digest. `FAIL` or `BLOCKED` preserves the pre-review state and must not be presented as CI integration.
- Historical verification is read-only: validate committed blobs, modes, gates, run identities, records, and gitlinks without rewriting closed evidence. Explicitly state that former uncommitted worktree state cannot be reconstructed.
- Prove the guards through the frozen negative/fault matrix, including count, path, candidate, gitlink, test-result, assertion-ledger, workflow-placement, and Markdown-digest mutations. Every mutation must produce its expected blocker and restore fixtures byte-identically.
- Epic 7 remains subject to the global implementation hold. Work is executable only after current authority passes mechanical validation, independent readiness is `READY` for the same planning candidate, and the release owner explicitly records `LIFTED`.
- Before implementing the Story 7.1 generator, complete the V11 non-story `7.1-SCHEMAS` checkpoint. Its scope is limited to the three missing closed schemas, schema-contract tests, and the checkpoint result; it must not modify the generator, produce a Story 7.1 final record, execute later Story 7.1 criteria, begin Stories 7.2-7.4, change product/runtime/deployment/submodule surfaces, or mark a story `done`.

## Technical Decisions

- The closed contracts are story-contract v1, acceptance-result v1, frozen-inventory v1, and story-final-record v2 at their canonical `_bmad/schemas` paths. Unknown properties fail unless explicitly allowed. JSON is authoritative; Markdown is only a deterministic rendering whose SHA-256 is recorded in JSON.
- Paths are repository-relative, slash-separated, and ordinally sorted when order is not semantic. Frozen inventory hashes use the displayed obligation IDs encoded as NFC UTF-8, one ID plus LF per line, in declared order.
- Generator and verifier commands use stable machine semantics: exit `0` means `PASS`, `1` means `FAIL`, and `2` means `BLOCKED`. Direct pytest environment exits `2`-`4` map to `TEST_ENVIRONMENT_BLOCKED`; exit `5` maps to `TEST_NOT_RUN`.
- Blocker arrays are unique and ordinally sorted. Story-specific blockers supplement rather than replace applicable common authority, candidate, gitlink, digest, schema, test-environment, assertion-ledger, and final-record blockers.
- A passing final record requires every declared scenario to pass and summary totals of `required=passed`, with zero failed, blocked, skipped, or not-run scenarios. The generator must still emit schema-valid failure output for malformed input and invalid arguments, without tracebacks or payload leakage.
- The schema checkpoint validates all four contracts against Draft 2020-12 plus representative instances. Missing, extra, malformed, duplicate, non-normalized, or permissive mutations must fail with schema-invalid semantics, and checkpoint fixtures must be restored byte-identically.

## Cross-Story Dependencies

Completed Story 6.2 is the hard epic entry; superseded Story 6.8 and its partial implementation are unaccepted inputs only. The current execution graph first requires the `7.1-SCHEMAS` checkpoint after Story 6.2 and candidate-matched IR-0; the checkpoint can move Story 7.1 only to `in-progress`, creates no final record or lifecycle key, and cannot unlock Story 7.2. The remaining chain is strict: Story 7.1 completes the contracts and generator core, Story 7.2 adds measured fact extraction, Story 7.3 places the verified generator on every completion transition, and Story 7.4 closes the chain with read-only historical verification and the complete fault matrix. Epic 7 exits only when Stories 7.1-7.4 are done at compatible candidates, and its final-record chain is a hard input to successor preservation, conformance, evidence-boundary, authoring-proof, projection-proof, and release-attestation work.
