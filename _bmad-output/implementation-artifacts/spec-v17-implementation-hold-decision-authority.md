---
title: 'Publish the release-owner implementation-hold lift as V17 authority'
type: 'feature'
created: '2026-09-08'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '94dbb37694747e9dedee20591b84b5d6a69d19b3'
submodule_promotions: []
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** IR-0 assessed `READY` but records `hold_decision.state: absent` and `effective_hold: ACTIVE`, and states that `READY` does not lift the hold. `v11-story-7.1-schema-slice-v1.json` requires `holdRequirement.effectiveState: LIFTED` at `_bmad-output/planning-artifacts/implementation-hold-v1.json`, a record that has never existed, so `7.1-SCHEMAS`, Story 7.1, Epic 16 and all 30 successor rows stay blocked.

**Approach:** Follow the V15/V16 publication pattern — a recursively closed Draft 2020-12 schema, a deterministic git-blob-sourced publisher with fault tests, and an atomically written record plus a successor authority. Publish the release-owner decision as `LIFTED` and V17 as the successor to V16, unlocking `7.1-SCHEMAS` only.

**Human decisions (2026-09-08):**
- Release owner is recorded as `{owner: "Release owner", name: "Jerome — release-owner hold lift 2026-09-08", mechanism: "Repository-recorded independent release-owner decision; no cryptographic-signature claim"}`, mirroring `epic-6-completion-supersession-current-proof-decision-v1.json`.
- The lift is **candidate-bound with no calendar expiry**: it binds the IR-0 sha256, the V9 `planningCandidate` and `bundleDigest`, and goes stale on any drift. `expiry.calendarExpiry` is `null`.
- The readiness rerun IR-0 requires is a **separate follow-up**, declared as an unmet obligation in `nonClaims`. This spec does not perform or satisfy it.
- **Implementation is gated.** At `94dbb37` the evidence lane is red (`verify_evidence_boundary.py` → `FAIL`/`EVIDENCE_GATE_NOT_USED`; 25 failed / 282 passed). Do not begin implementation until that lane is green, because V17 may not claim `PASS` on a red gate and because the concurrent lifecycle-gate restoration edits the same verifier file.

## Boundaries & Constraints

**Always:** Recompute every declared hash from source bytes via `git show <candidate>:<path>`; derive modes from `git ls-tree`, never `os.stat`; compare the changed-path boundary by exact set equality reporting both missing and unexpected paths; derive gitlinks from raw mode `160000`; keep `PASS`/`FAIL`/`BLOCKED`/`not-applicable` distinct with a nonempty ledger and `skipsAllowed: false`; keep V9–V16 and IR-0 bytes byte-identical; keep the V9 predecessor chain intact (`fileSha256 8af7ba3b…f953ef3`, `bundleDigest 159eec0c…98ff4f055`, `planningCandidate 1e9a6112…3355e5`); publish additively.

**Never:** Rewrite or reissue any V1–V16 authority; register the hold record in the V9 bundle or its path sets; touch `sprint-status.yaml` row states, `src/`, `tests/`, `references/`, or `.gitmodules`; initialize nested submodules; set `releaseAuthorized` or `pushAuthorized` true; claim the readiness rerun; treat `BLOCKED` or a skip as a pass; push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Publish | Clean candidate containing both new artifacts | Record + V17 written atomically; `result: PASS`; `HOLD_DECISION_OK` line; exit 0 | — |
| `--check` re-run | Published candidate, unchanged bytes | Recomputed document equals committed bytes; exit 0 | `HOLD_AUTHORITY_DRIFT` on any mismatch |
| Predecessor drift | Any of the 7 carried authorities differs in bytes or mode | `HOLD_PREDECESSOR_DRIFT`, `FAIL`, exit 1 | Nonempty ledger; nothing written |
| Stale binding | IR-0 sha256, planningCandidate or bundleDigest differs | `HOLD_BINDING_STALE`, `FAIL` | Record must not evaluate to `LIFTED` |
| Unavailable history | Shallow clone, missing blob, git failure, timeout | `BLOCKED`, exit 2 | Never collapsed to `FAIL` or `PASS` |
| Closed schema | Unknown field, weak hash/path pattern, empty ledger | Rejected by `Draft202012Validator` | `HOLD_SCHEMA_INVALID`; `HOLD_SCHEMA_UNAVAILABLE`/`BLOCKED` if jsonschema absent |
| Path escape | Absolute, `..`, backslash or non-normalized path | `HOLD_PATH_ESCAPE`, `BLOCKED` | — |

</frozen-after-approval>

## Code Map

New, mirroring `publish_v16_planning_tooling_lifecycle.py` and its test:

- `_bmad/schemas/implementation-hold-v1.schema.json` -- closed record schema. `$id https://hexalith.com/schemas/implementation-hold-v1.schema.json`.
- `_bmad/schemas/v17-implementation-hold-decision-authority-v1.schema.json` -- closed successor-authority schema, same `$id` host form.
- `_bmad/scripts/publish_implementation_hold_decision.py` -- publisher. Error class `ImplementationHoldError(code, detail, state="FAIL")`, `HOLD_*` codes, success token `HOLD_DECISION_OK`.
- `_bmad/scripts/tests/test_publish_implementation_hold_decision.py` -- fault suite.

Modified:

- `_bmad/scripts/verify_evidence_boundary.py` -- add `V17_*` constants (~L21-57), a `v17` branch at the head of `authority_route` (~L247, most-recent-first), `validate_v17_scope()` beside `validate_v16_scope()` (~L605), the `verify()` dispatch branch, the `applicable` / `route in (...)` tuples, and the V17 publisher in `run_publication_check` (~L636).
- `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- V17 route and scope coverage.

Generated outputs (C2):

- `_bmad-output/planning-artifacts/implementation-hold-v1.json`
- `_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json`

Read-only roots of trust — never modified, only hashed: `v9-authority-bundle-v1.json`, `v12`/`v13`/`v14`/`v15`/`v16` authority JSONs, `implementation-readiness-report-2026-08-22-ir-0.md`, `v11-story-7.1-schema-slice-v1.json`.

Conventions to reuse verbatim from V16: `json_bytes` = `json.dumps(value, indent=2, ensure_ascii=False) + "\n"` UTF-8, **no `sort_keys`** (dict literal order is canonical); atomic `.tmp` + `os.replace`; `run_git` with `timeout=30` and `GIT_CONFIG_NOSYSTEM=1`; `--repository` / `--candidate` / `--publication` / `--check` flags; exit `0`/`1`/`2`; tests load the publisher by path with `importlib.util`, stage faults into `tmp_path` via `git clone --shared`, and never mutate repository bytes.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/schemas/implementation-hold-v1.schema.json` -- author the closed record schema: `additionalProperties: false` at every level including `$defs` and array items; exhaustive `required`; `$defs.sha1 ^[0-9a-f]{40}$`, `$defs.sha256 ^[0-9a-f]{64}$`, repo-relative path pattern `^(?!/)(?!.*(?:^|/)\.\.(?:/|$))(?!.*\\)[^\\u0000-\\u001f]+$`; `resultSemantics` as a `const` object; `effectiveState` `const "LIFTED"`; `assertionLedger` `minItems: 1`.
- [ ] `_bmad-output/planning-artifacts/implementation-hold-v1.json` -- generate the record: `decisionId`, `decisionDate 2026-09-08`, `decisionAuthority`, `scope` (`unlocks: ["7.1-SCHEMAS"]`, bound candidate/bundleDigest/IR-0 sha256, `staleOnDrift: true`, `global: false`), `expiry {kind: "candidate-bound", calendarExpiry: null}`, `ir0Assessment` (exact path + `862a880a…8b122ad`, `result READY`, `effectiveHoldAtAssessment ACTIVE`), `rationale`, `nonClaims`, `effectiveState LIFTED`.
- [ ] `_bmad/schemas/v17-implementation-hold-decision-authority-v1.schema.json` -- successor schema pinning `authorityId V17-IMPLEMENTATION-HOLD-DECISION`, `predecessorAuthorityId V16-PLANNING-TOOLING-LIFECYCLE`, `immutableAuthorities` as a 7-element `const`, and `authorityEffect` as a `const` object.
- [ ] `_bmad/scripts/publish_implementation_hold_decision.py` -- deterministic publisher producing both documents from committed blobs; builds the 7-row `immutableAuthorities` (V9, V12, V13, V14, V15, **V16**, IR-0 last) each `{path, sha256, mode}` with mode `100644`; asserts V13/V14 `nonClaims` bytes unchanged; emits a nonempty `V17-*` ledger.
- [ ] `_bmad/scripts/tests/test_publish_implementation_hold_decision.py` -- one named fault per matrix row, each changing a single condition, asserting the exact code and state, and leaving repository bytes byte-identical.
- [ ] `_bmad/scripts/verify_evidence_boundary.py` + its test -- route V17 ahead of V16 and preserve child `FAIL`/`BLOCKED` states.
- [ ] Commit C1 with only the six code/schema/test/spec paths and C2 with only the two generated artifacts; validate both messages with the pinned commitlint CLI; do not push.

**Acceptance Criteria:**
- Given the published candidate, `--check` reproduces both documents byte-for-byte and returns `PASS` with a nonempty ledger.
- Given a mutation of any carried authority's bytes or mode, publication fails closed with `HOLD_PREDECESSOR_DRIFT` and writes nothing.
- Given the published V17, `authorityEffect.implementationHold` is `LIFTED`, `successorActivated` is `true`, `releaseAuthorized` and `pushAuthorized` are `false`, and `nonClaims` states the readiness rerun is unmet.
- Given baseline `94dbb37`, C1 and C2 contain exactly eight distinct paths combined, zero raw-mode `160000` changes, and V9–V16 plus IR-0 hashes are unchanged.
- Given `v11-story-7.1-schema-slice-v1.json`, the record path and `holdRequirement.effectiveState LIFTED` match exactly, so `7.1-SCHEMAS` is unlocked and no other successor is started.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Carried set is 7, not 16.** V1–V14 are not standalone files — they are byte-pinned overlay blocks inside `epics.md`/`architecture.md`, and V10 has no file at all. `immutableAuthorities` was introduced at V15; the growth rule is "append the immediate predecessor authority JSON, keep IR-0 pinned last". V1–V14 remain preserved transitively because the V9 bundle pins all 101 artifacts, including V11 at `14e95c44…a59da82d`.

**The record stays outside the bundle digest.** `publish_v9_planning_authority.py:1874` raises `BUNDLE_INVENTORY_DRIFT` ("mutable gate or hold result") for any bundle inventory containing `implementation-hold-v1.json`. This is intentional: the mutable decision record must not enter the immutable digest. Do not add any new path to `CANONICAL_PATHS`, `PROTECTED_CANDIDATE_PATHS`, or `EXPECTED_OUTPUT_PATHS`; V15 and V16 are absent from them for the same reason. Add a test asserting this exclusion holds.

**V13/V14 `nonClaims` are not a contradiction.** Both pin the literal string `"create implementation-hold-v1.json"` as something *they* do not do. V17 creating it is consistent; their bytes must remain untouched, asserted explicitly.

**Fail-closed default.** Per `sprint-change-proposal-2026-08-04.md`, effective authorization requires validator `PASS`, IR-0 `READY`, a valid `LIFTED` decision, and no later drift. Missing, stale or mismatched evidence evaluates to `ACTIVE`. There is deliberately no `ACTIVE_WITH_EXCEPTION` state.

## Verification

**Commands:**
- `uv run --frozen python3 _bmad/scripts/publish_implementation_hold_decision.py --check --repository .` -- expected: exit 0, `HOLD_DECISION_OK`.
- `uv run --frozen python3 -m pytest -q _bmad/scripts/tests` -- expected: zero failed, skipped, xfailed, xpassed or not-run.
- `uv run --frozen python3 _bmad/scripts/verify_evidence_boundary.py --baseline <C0> --candidate <C2>` -- expected: `result: PASS`, nonempty ledger, exit 0.
- `git diff --name-only <C0>..<C2>` and `git diff --raw --no-abbrev <C0>..<C2>` -- expected: exactly the eight declared paths by set equality; zero `160000` modes.
- Pinned commitlint CLI on each exact commit message -- expected: pass, evidence preserved.

**Manual checks:**
- `sha256sum` each of the 7 carried authorities against the values in V17 and against V16's declarations — all unchanged.
