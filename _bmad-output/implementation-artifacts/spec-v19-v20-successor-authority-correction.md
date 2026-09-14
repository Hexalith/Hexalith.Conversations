---
title: 'Implement V19 and V20 successor-authority tooling'
type: 'feature'
created: '2026-09-12'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '64b050831eea694cb2065342cf23a150646eef57'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-12.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved forward-only correction has no dedicated schemas, frozen inventory, or publisher/validators for the separate V19 checkpoint-completion and V20 release-owner authorities. The first committed tooling candidate also bound pre-amendment semantic-source bytes that were never committed, so it cannot serve as a valid V20 entry baseline.

**Approach:** Add the successor-authority tooling surface defined by the approved proposal, classify `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` as a failed prepublication candidate, and bind a concise correction record plus the first committed human-amended semantic source. Make publication candidate-bound and fail-closed, but leave both authority records absent until their independent prerequisites exist.

## Boundaries & Constraints

**Always:** Recompute source hashes, canonical NFC UTF-8 LF inventory digests, exact path sets, committed blob modes/digests, single-parent relations, raw mode-`160000` gitlinks, JUnit counts and ordered nonempty ledgers. Preserve distinct `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` results. Preserve the human-amended semantic source at SHA-256 `eeee633e7045d9636d4babe62b6bca9744c8137b490da2304fcc9192f84fbaf5`; bind the exact prepublication correction record in V20 current inputs and entry bindings. V19 records `b819a7c` as `NONCONFORMING` and keeps the full-story hold active. V20 validates an explicitly supplied release-owner decision and distinguishes PC, checkpoint, entry, publication-baseline, and eventual `SC-7.1` roles. Missing or stale V20 evidence evaluates the effective Story 7.1 hold as `ACTIVE`.

**Never:** Create live V19/V20 authority JSON in this run; execute or certify `7.1-SCHEMAS`; author the release-owner decision; implement Story 7.1 or Stories 7.2-7.4; modify V17, V18, `implementation-hold-v1.json`, `b819a7c`, the approved semantic spec, checkpoint-owned files, sprint status, loop markers, dependencies, workflows, submodules, or other user changes; stage, commit, or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Inventory check | Proposed frozen path identities and current fixed inputs | Closed schema-valid inventory with every ordered path-list digest recomputed | Drift is `FAIL` with a stable inventory code |
| Prepublication correction | Failed candidate `596cee6` bound pre-amendment bytes that were never committed | Exact correction record and committed human semantic-source digest are both frozen V20 inputs | Missing, changed, or stale correction/source evidence is `FAIL` or `BLOCKED` |
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
- [x] `_bmad-output/planning-artifacts/v20-story-7.1-prepublication-correction-v1.md` and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-12.md` -- record the human-approved failed-candidate classification, preserve the first committed amended semantic source, and bind both exact records before V19/V20 publication.
- [x] `_bmad/scripts/publish_story_7_1_successor_authorities.py` -- implement explicit inventory, V19, and V20 publish/check routes plus effective-hold validation; derive all authority facts from committed objects and require explicit owner inputs for V20 writes.
- [x] `_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py` -- cover schemas, inventory, future transactions, exact paths, raw gitlinks, committed XML parsing, distinct roles/effects, failure-state semantics, every named proposal mutation, and restoration.

**Acceptance Criteria:**
- Given the six-file tooling candidate, when focused schema and inventory checks run, then all schemas are recursively closed and every proposal identity, ordered path list, digest, role, and effect is exact.
- Given the failed prepublication candidate, when V20 inputs are validated, then the correction record and committed `eeee633e...` semantic-source bytes are exact and the never-committed `90477eb2...` bytes are retained only as historical failure evidence.
- Given a hermetic valid checkpoint transaction, when V19 is rendered and checked, then committed evidence produces a nonempty `PASS` ledger while `b819a7c` remains `NONCONFORMING` and Story 7.1 remains held.
- Given valid committed V19/entry transactions and explicit owner data, when V20 is rendered and checked, then only Story 7.1 is lifted; absent or mutated evidence returns `ACTIVE` with the specified `FAIL` or `BLOCKED` result.
- Given the final correction worktree, when exact-path and protected-byte audits run, then only the proposal, correction record, V20 inventory/schema, V20 authority schema, publisher, tests, and this workflow spec changed; protected hashes and user changes are unchanged, and no authority record, checkpoint result, sprint status, or loop marker was written.

## Implementation Notes

- Added only the six approved successor-authority files. Live V19/V20 records remain absent; the current effective-hold check returns `BLOCKED`/`ACTIVE` with a nonempty ledger.
- V19 binds all five checkpoint blobs, independently confirms the historical result blob is absent from `b819a7c`, and records V17 by its actual authority ID.
- Focused verification passed 16 tests; inventory validation and the 14-test V18 regression lane also passed. Protected hashes, the modified Folders gitlink, sprint status, ignored checkpoint XML, and loop state remain unchanged.
- Review lifecycle gate: `python3 _bmad/scripts/verify_submodule_promotion.py --repository /home/administrator/projects/hexalith/conversations --baseline 64b050831eea694cb2065342cf23a150646eef57 --candidate HEAD` exited `1`/`BLOCKED` with `UNCAPTURED_SUBMODULE_PROMOTION` because the protected user checkout `references/Hexalith.Folders` is at `df0b636a6194b5529b566f0f579f1cebfa7f48c6` while the recorded gitlink remains `adb3831c17f70a7483f171877bf60086ddf91531`. The companion evidence-boundary command exited `0`/`PASS` with a nonempty ledger. Per the workflow, status remains `in-progress` and review/done transitions did not run.
- After the user committed the tooling candidate, real-checkout validation exposed a stale semantic-source binding: the declared `90477eb2...` bytes were never committed, while the first committed human-amended source hashes to `eeee633e...`. The user approved a controlled prepublication correction. The correction record is now a frozen V20 input and entry binding; focused verification passes 26 tests and the inventory route passes with 10 inventories. No live V19/V20 record was created.
- Final review hardened Git isolation, atomic writes, correction/entry chronology, current-evidence checks, and independent test oracles. The focused suite passes 42 tests, the 14-test V18 regression lane passes, both lifecycle evidence gates pass, and the absent V20 publication correctly leaves the effective hold `ACTIVE`/`BLOCKED`.

## Spec Change Log

- `2026-09-13` -- Human approved the controlled prepublication correction: preserve the committed amended semantic source, classify `596cee6` as a failed candidate, and bind the correction record before either authority publication.

## Review Triage Log

| ID | Reviewer | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| B01 | Blind Hunter | false | reject | The seven gitlink updates are committed user changes in `596cee6`, not correction-worktree changes: `git diff --raw HEAD -- references` is empty. The approved correction must preserve rather than revert them. |
| B02 | Blind Hunter | false | reject | V19 intentionally validates the committed JUnit snapshot; proposal handoff step 4 executes the checkpoint before step 5 publishes V19. The frozen command is authenticated through the exact V11 sidecar, while this publisher does not claim execution provenance. |
| B03 | Blind Hunter | false | reject | The implemented operational definition rejects byte-identical, line-ending-only, and Unicode-normalization-only rewrites. Neither the approved intent nor V11 defines semantic AST equivalence, so `$comment` and source-comment changes are not bypasses of the stated guard. |
| B04 | Blind Hunter | false | reject | V19 authenticates the exact V9 authority bundle by its externally frozen raw digest and independently recomputes its internal bundle digest. Revalidating every historical V9 artifact would cross the accepted point-in-time authority boundary. |
| B05 | Blind Hunter | high | patch | `render_v19` does not read the approved correction record or amended semantic source, so a five-path candidate descending from pre-correction history can pass. Require both exact regular blobs at the checkpoint candidate. |
| B06 | Blind Hunter | high | patch | `run_git` inherits redirecting Git variables and replacement-object/global-config behavior; `git -C` does not neutralize them. Use a sanitized Git environment and cover a poisoned caller environment. |
| B07 | Blind Hunter | false | reject | V20 binds all tooling bytes at the concrete entry candidate, and the independently supplied owner decision selects that candidate. Requiring a second external tooling manifest would duplicate the human-owned entry boundary rather than close a demonstrated self-reference. |
| B08 | Blind Hunter | medium | patch | Known implementation paths are content-bound through checkpoint or fixed inputs, except the future failure schema, which may be added before entry without detection. Require that path to remain absent at the V20 entry candidate; unrelated paths are not Story 7.1 work. |
| B09 | Blind Hunter | medium | patch | The year-only timestamp check accepts a decision predating V19/entry or postdating publication. Compare the supplied instant to committed V19/entry/publication chronology. |
| B10 | Blind Hunter | false | reject | The approved boundary is an explicit caller-supplied human decision, not cryptographic identity attestation. The publisher validates and binds the assertion and never invents it; signing infrastructure was not authorized. |
| B11 | Blind Hunter | medium | patch | `inventory_route(check=False)` replaces an existing noncanonical inventory even though authority writes are additive-only. Make an identical file idempotent and refuse differing existing bytes. |
| B12 | Blind Hunter | false | reject | The non-hex value is a frozen derivation token for future `SC-7.1`, paired with `candidateDerivedBindings`; no future digest exists at inventory-freeze time. Exact `const` validation prevents consumers from mistaking it for measured evidence. |
| B13 | Blind Hunter | false | reject | Scenario inventories include V19, V20, the frozen inventory, and its schema; V20's own entry bindings transitively bind both authority schemas, publisher/tests, correction, and semantic source. Duplicating all transitive dependencies in every scenario is unnecessary. |
| B14 | Blind Hunter | medium | defer | The contradictory Auto Run Result predates this correction and the frozen intent explicitly forbids editing the approved semantic source. Record it for separate cleanup without changing the source bytes. |
| B15 | Blind Hunter | false | reject | The cited `in-progress` statement is a dated lifecycle-gate note, followed by the later successful correction note; current frontmatter is correctly `in-review`. Its earlier file-count wording is historical and any fix would only edit this build spec. |
| E01 | Edge Case Hunter | high | patch | Confirmed independently from B06: redirecting Git variables reach every history read. Sanitize the subprocess environment and add a regression test. |
| E02 | Edge Case Hunter | medium | patch | `root_gitlinks` reads `.gitmodules` bytes without checking its raw mode, so a mode-`120000` blob can reach parsing. Require a committed mode-`100644` blob first. |
| E03 | Edge Case Hunter | false | reject | Same verified disposition as B02: command execution is the preceding checkpoint-transaction responsibility; V19 authenticates and parses its committed result without claiming runtime provenance. |
| E04 | Edge Case Hunter | high | patch | The predictable `.tmp` name can be a hard link, and `write_bytes` mutates its external inode before replacement. Create an exclusive random temporary file in the destination directory, then replace. |
| E05 | Edge Case Hunter | medium | patch | Confirmed independently from B09: a syntactically valid decision instant may precede V19. Bind it to Git chronology. |
| E06 | Edge Case Hunter | high | patch | Confirmed independently from B05: no V19 correction/source check currently exists. Validate their exact committed modes and hashes at the checkpoint candidate. |
| E07 | Edge Case Hunter | high | patch | V20 check revalidates V19 only at stored entry, so deleting or mutating V19 later can leave effective hold `LIFTED`. Require current evaluated V19 bytes to equal the published predecessor bytes. |
| E08 | Edge Case Hunter | false | reject | Same verified disposition as B01: no submodule path is modified in the correction worktree; the baseline diff contains preserved user-committed gitlink changes. |
| V01 | Verification Gap | medium | patch | The filed probe shows fabricated gitlink commit identities pass the current tests. Add an independent `git ls-tree` oracle for every emitted V19 gitlink row. |
| V02 | Verification Gap | medium | patch | The filed probe shows fabricated non-V19 entry-binding digests pass the current tests. Compare all nine emitted bindings with independent committed blob/mode reads. |
| V03 | Verification Gap | medium | patch | No test reaches `V19_NON_SUBSTANTIVE_CHANGE`; removing the guard leaves the suite green. Add a normalization-only exact-five-path checkpoint case. |
| V04 | Verification Gap | medium | patch | No test reaches `V19_RESULT_LEDGER_DUPLICATE`, and schema uniqueness does not reject distinct IDs with duplicate subjects. Add a duplicate-testcase mutation. |
| V05 | Verification Gap | medium | patch | V17/V18/hold drift guards have no mutation coverage. Add focused predecessor mutation/removal cases before an otherwise exact checkpoint. |
| V06 | Verification Gap | medium | patch | All current tests call internals, so CLI argument routing and exit semantics can regress unnoticed. Add focused route/exit/output tests without widening the production interface. |

## Design Notes

Use one dedicated CLI with explicit `inventory`, `v19`, and `v20` routes. Check routes are read-only and validate committed publication transactions; write routes use atomic replacement. V20 write requires caller-supplied release-owner identity, UTC instant, rationale, and concrete entry candidate, so the tooling never invents the decision.

## Verification

**Commands:**
- `uv run --frozen --no-cache pytest -q _bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py` -- expected: focused publisher/schema/mutation suite passes with nonzero tests.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . inventory --check` -- expected: inventory schema and every canonical path digest pass.
- `uv run --frozen --no-cache pytest -q _bmad/scripts/tests/test_publish_v18_package_environment_authority.py` -- expected: predecessor publisher regression suite passes.
- `git diff --check` -- expected: no whitespace errors.
- `git status --short` plus protected `sha256sum` and exact-path comparison -- expected: declared user changes preserved and no forbidden path changed.
