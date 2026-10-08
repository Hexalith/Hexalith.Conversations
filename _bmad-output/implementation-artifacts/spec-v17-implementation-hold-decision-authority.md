---
title: 'Publish the release-owner implementation-hold lift as V17 authority'
type: 'feature'
created: '2026-09-08'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '94dbb37694747e9dedee20591b84b5d6a69d19b3'
completion_scope: 'documentation-reconciliation'
publication_baseline_commit: '074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf'
implementation_commit: '82b91c10fe95f8ef7a7576d35199322481e359a7'
publication_commit: '8bd6789eae34a322e06156dd8e7e2fb0aded42d7'
reconciliation_baseline_commit: '5e4abc6f87692e634319c1d5612c91647ae6eae7'
submodule_promotions: []
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

## Reconciled Scope (2026-10-08)

The user authorized reconciliation with the existing V17 publication and current
validation policy. V17 was already published in the C1/C2 commits named above.
This run changes only this spec; it records that delivery rather than implementing
or republishing it. `status` tracks this documentation reconciliation. Completion
does not certify every original matrix fault, start a successor, satisfy the
readiness rerun, or grant current execution, release, or push authority.

The frozen section below remains byte-identical historical intent. Its missing-record
description and green-gate prerequisite describe the original development entry,
not a new implementation request. The reconciled tasks and acceptance below govern
this documentation correction. The original `baseline_commit` is retained as
planning provenance; the eight-path V17 transaction starts at
`publication_baseline_commit`, after the prerequisite work. Review the current
documentation diff from `reconciliation_baseline_commit`.

Use the [current-change policy](../../docs/runbooks/current-change-validation.md)
for this correction. The [evidence-boundary runbook](../../docs/runbooks/evidence-boundary-validation.md)
and verifier remain historical reproduction tools. A historical V17 `PASS` does
not turn a later `BLOCKED` result into a pass or change a current hold decision.
Preserve every authority, schema, publisher, test, and IR-0 record. Create no new
commits or publications, and do not push.

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

Already committed in C1, mirroring `publish_v16_planning_tooling_lifecycle.py` and its test:

- `_bmad/schemas/implementation-hold-v1.schema.json` -- closed record schema. `$id https://hexalith.com/schemas/implementation-hold-v1.schema.json`.
- `_bmad/schemas/v17-implementation-hold-decision-authority-v1.schema.json` -- closed successor-authority schema, same `$id` host form.
- `_bmad/scripts/publish_implementation_hold_decision.py` -- publisher. Error class `ImplementationHoldError(code, detail, state="FAIL")`, `HOLD_*` codes, success token `HOLD_DECISION_OK`.
- `_bmad/scripts/tests/test_publish_implementation_hold_decision.py` -- fault suite.

Already modified in C1:

- `_bmad/scripts/verify_evidence_boundary.py` -- contains the V17 constants, candidate-tree route ahead of V16, `validate_v17_scope()`, verification dispatch, and publisher check. Later successors take precedence when reproducing their candidates.
- `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- V17 route and scope coverage.

Already published outputs (C2):

- `_bmad-output/planning-artifacts/implementation-hold-v1.json`
- `_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json`

Read-only roots of trust — never modified, only hashed: `v9-authority-bundle-v1.json`, `v12`/`v13`/`v14`/`v15`/`v16` authority JSONs, `implementation-readiness-report-2026-08-22-ir-0.md`, `v11-story-7.1-schema-slice-v1.json`.

The existing publisher uses the V16 JSON, Git, CLI, and per-file atomic-write conventions. Its fault tests load it by path and stage fixtures in shared temporary clones. The current correction's only editable path is `_bmad-output/implementation-artifacts/spec-v17-implementation-hold-decision-authority.md`.

## Tasks & Acceptance

**Execution:**

- [ ] This spec -- record the actual C0/C1/C2 commit identities and compare C1's six paths and C2's two paths to the published inventories by exact set equality, including missing and unexpected paths; derive modes and gitlinks from Git.
- [ ] This spec -- record deterministic publisher and historical V17 verifier results; independently recompute all seven carried hashes and modes at the publication baseline, C2, and reconciliation baseline.
- [ ] This spec -- record the focused publisher and V17 routing test results and validate the two existing commit messages with the pinned commitlint CLI, retaining successful command evidence outside the repository.
- [ ] This spec -- distinguish the original planning baseline and its broader diff from the publication baseline; explain the current-change policy and retain the current historical-verifier blocker honestly.
- [ ] This spec -- verify the current documentation diff is exactly this file, the frozen section is unchanged, links resolve, and root-submodule inventory and whitespace checks pass. Leave historical artifacts, sprint states, and authorization flags untouched.

**Acceptance Criteria:**

- Given the existing C1/C2 and their actual C0, when their Git objects and publisher are checked, then the path sets are exactly six and two (eight combined), no raw mode `160000` changed, both documents reproduce, and all seven carried authorities retain their declared bytes and modes.
- Given the historical V17 publication, when the verifier evaluates C0 to C2 and focused tests run, then the verifier returns `PASS` with a nonempty ledger, all selected tests pass without skips, and the existing commit messages pass pinned commitlint.
- Given the reconciliation baseline and current policy, when the historical verifier returns `BLOCKED`, then this spec records its exact command, code, and exit state separately from V17's historical `PASS`, and makes no current hold-lift or readiness-rerun claim.
- Given the authorized documentation correction, when its diff is reviewed, then only this spec changes, the original baseline and frozen intent remain preserved, links and focused checks pass, and completion refers only to documentation reconciliation.

## Implementation Notes

- Original planning entry: `94dbb37694747e9dedee20591b84b5d6a69d19b3`.
- Publication C0: `074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf`.
- Implementation C1: `82b91c10fe95f8ef7a7576d35199322481e359a7`.
- Publication C2: `8bd6789eae34a322e06156dd8e7e2fb0aded42d7`.
- Documentation reconciliation baseline: `5e4abc6f87692e634319c1d5612c91647ae6eae7`.

C1 is C0's direct child and changes exactly the six code/schema/test paths in
V17's `publication.c1Paths`; the spec is outside that historical transaction.
C2 is C1's direct child and adds only the hold record and V17 authority. Their
combined changed-path set is the published eight-path inventory, with zero
changed gitlinks. The original planning-entry-to-C2 range instead contains
40 paths and six changed gitlinks from intervening work; it is not the V17
publication transaction. Preserve that distinction rather than rewriting history.

V17 records `implementationHold: LIFTED` for `7.1-SCHEMAS`, with release and push
authorization false and the readiness rerun unmet. That candidate-bound historical
decision is reproduced here. Later authority and effective-hold decisions retain
their own meaning; this correction grants no current permission to resume work.

## Spec Change Log

- 2026-10-08: The user authorized reconciling the stale spec with the already
  published V17 and current validation policy. Retained the original baseline
  and frozen intent, recorded the actual transaction separately, replaced the
  new-implementation tasks with a documentation audit, and scoped completion to
  that correction. Keep all historical publication, authority, and IR-0 bytes.

## Review Triage Log

## Design Notes

**Carried set is 7, not 16.** V1–V14 are not standalone files — they are byte-pinned overlay blocks inside `epics.md`/`architecture.md`, and V10 has no file at all. `immutableAuthorities` was introduced at V15; the growth rule is "append the immediate predecessor authority JSON, keep IR-0 pinned last". V1–V14 remain preserved transitively because the V9 bundle pins all 101 artifacts, including V11 at `14e95c44…a59da82d`.

**The record stays outside the bundle digest.** `publish_v9_planning_authority.py` raises `BUNDLE_INVENTORY_DRIFT` ("mutable gate or hold result") for any bundle inventory containing `implementation-hold-v1.json`. The existing V17 exclusion test verifies the hold record is absent from the V9 inventory and protected path sets. This correction changes none of those inventories.

**V13/V14 `nonClaims` are not a contradiction.** Both pin the literal string `"create implementation-hold-v1.json"` as something *they* do not do. V17 creating it is consistent; their bytes must remain untouched, asserted explicitly.

**Fail-closed default.** Per `sprint-change-proposal-2026-08-04.md`, effective authorization requires validator `PASS`, IR-0 `READY`, a valid `LIFTED` decision, and no later drift. Missing, stale or mismatched evidence evaluates to `ACTIVE`. There is deliberately no `ACTIVE_WITH_EXCEPTION` state.

## Verification

Use frozen Python dependencies and explicit historical test selection. The
current directory lane excludes retired evidence suites; a directory-level pass
would not establish their coverage. Run the existing publisher suite and the
V17-specific routing tests, and report their measured counts. This audit does
not claim exhaustive fault coverage of the archived matrix.

Record exact commands and measured results for the publisher, historical C0/C2
verifier, current historical-verifier blocker, Git path/mode/hash checks, existing
C1/C2 commitlint, root-submodule inventory, and this spec's diff/link/frozen checks.
