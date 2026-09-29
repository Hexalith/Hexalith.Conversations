---
title: 'Prepare the AD-4 acceptance readiness contract and inspector'
type: 'feature'
created: '2026-09-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'aefe4003cc94f49021e942adb9cbb8ca9ccf86cd'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/story-7-1-ad4-approval-2026-09-27.md'
  - '{project-root}/_bmad-output/implementation-artifacts/story-7-1-terminal-acceptance-proposal-2026-09-27.md'
---

<frozen-after-approval reason="user approved AD-4 preparation in this conversation">

## Intent

**Problem:** The approved AD-4 proposal identifies missing terminal acceptance, but no repeatable non-authorizing readiness interface distinguishes committed inputs from unverified approval claims.

**Approach:** Add a read-only inspector with a closed result schema and a staged acceptance contract. Reuse the audit. This preparation is authorized by “I approve AD-4”; it does not implement or activate terminal authentication/publication.

## Boundaries & Constraints

**Always:** Require an explicit full candidate SHA. Inspect root committed objects only, with complete history and safe Git execution. Bind candidate/tree/parents, ordinal raw gitlinks, inspected paths/modes/blob identities/SHA-256 values, and a nonempty ledger. Inventory the exact fifteen absent prerequisite paths in the proposal. Distinguish absent, invalid and present-but-unverified inputs. Verify both preserved record pairs structurally and against their existing exact-byte digests without regeneration. Keep the actual accepted-main identity null. Every result retains `ACTIVE`, all four authority flags false, terminal verification/publication unsupported, and acceptance unestablished. A fully populated input set still blocks on unadopted terminal tooling/provenance; file presence never satisfies a gate.

**Never:** Publish an authority, append an architecture pointer, modify prior tooling/records/results/gitlinks/sprint status, select a trusted host or accepted-main revision, sign as the owner, fetch/update/traverse submodules, execute candidate-side tools, stage/commit/push, or infer acceptance from done/PASS/HEAD/history. No changes to existing historical resolver or boundary verifier. No product behavior changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Resume revision | Current committed inputs | Closed JSON, observed facts and explicit missing/unverified prerequisites; exit 2 | `BLOCKED`, no authorization |
| Populated files | Self-declared PASS/ACCEPTED or approval | Presence recorded; no trusted decision inferred | Unsupported terminal verification remains a blocker |
| Invalid input | Bad SHA/CLI, malformed or duplicate JSON, wrong mode, record drift, gitlink mismatch | Closed JSON with stable diagnostic and ledger | Exit 1 `FAIL` for invalid evidence |
| Unavailable evidence | Missing commit/blob, shallow/partial history, Git/environment failure | No fallback to worktree or HEAD | Exit 2 `BLOCKED` |
| Worktree decoy | Untracked or changed proof beside committed state | Only committed bytes inspected | Decoy cannot change result |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/publish_story_7_1_entry_authority.py` -- reuse host-side safe Git/path/JSON/raw-gitlink helpers and canonical prerequisite constants, without changing historical bytes. These helpers do not grant trust to this inspector.
- `_bmad/scripts/generate_story_record.py` -- reuse `v2_verify_pair` only; never invoke generator execution or outputs. Use host-side schemas for structural checks and report their bindings.
- `_bmad/scripts/publish_story_7_1_lifecycle_evidence_authority.py` -- reference complete-history/partial-clone guard semantics; do not run its publication routes.
- `docs/runbooks/current-change-validation.md` -- preserve the current routine policy and lack of a protected terminal route.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/schemas/story-7.1-ad4-readiness-result-v1.schema.json` -- close the result, failure and no-authority controls; permit only FAIL/BLOCKED overall.
- [x] `_bmad/scripts/inspect_story_7_1_acceptance.py` -- implement `--repository`, required `--candidate`, and `--check`; emit JSON to stdout only, stable blockers and exact committed bindings. Never accept trust/approval/output-write options.
- [x] `tests/tooling/test_ad4_acceptance_readiness.py` -- cover real Git/CLI paths, absence and fake completeness, malformed/duplicate JSON, wrong modes, record tamper, unsafe paths, shallow/partial history, missing objects, dirty decoys, and unchanged evidence bytes/timestamps. Keep fixtures outside archived artifact paths.
- [x] `docs/runbooks/story-7.1-ad4-acceptance.md` -- state the stage-1 command and semantics; specify the required future entry/integration/final-binding/terminal records, canonical digest rules, atomic pointer publication, protected-source trust adoption, owner decisions and negative acceptance cases. Separate implemented readiness from unimplemented terminal validation.
- [x] This spec and approval receipt -- retain review and verification evidence without changing the approved proposal.

**Acceptance Criteria:**
- Given the inspected main revision, when the inspector runs, then all fifteen canonical prerequisite absences are reported and both historical pairs retain their exact bytes; missing or invalid pairs visibly block/fail instead of being omitted.
- Given any claimed ACCEPTED evidence, when stage 1 evaluates it, then its schema-valid result cannot lift the hold, set an authority flag, claim terminal verification, or provide an accepted-main identity.
- Given a failed invocation, when its JSON is consumed, then its stable diagnostic, result, exit and nonempty ledger agree; malformed output never counts as readiness.
- Given the approved proposal, when the new contract is read, then the reader can identify what approval already authorizes and the exact integration/trust evidence still required without another generic approval request.

## Implementation Notes

This is standalone preparatory tooling, not Story 7.1/7.2 implementation or a sprint transition. The only pre-existing untracked file is the approved proposal produced in this conversation; preserving it is already authorized. No irreversible operations are planned. Integration target and provenance are deliberately required future inputs, not assumptions or blockers to implementing this non-authorizing inspector. The prior approval covers this bounded preparation; do not ask for checkpoint reapproval.

## Spec Change Log

- 2026-09-27: Implemented the approved additive stage-1 scope. The frozen intent, constraints and matrix are unchanged. Historical helper bytes remain unchanged; a private host module adapter hardens their Git execution for read-only inspection. Prospective terminal records are documented only, with no terminal command or authority publication.

- 2026-09-27: Independent review led to source-byte loading, Unicode error classification, pinned pair schema facts, stronger preservation/gitlink tests, and precise prospective baseline/canonical-byte rules. The frozen block and approved proposal remain unchanged.

## Review Triage Log

Three independent review layers completed: blind review (10 findings), edge-case review (no findings), and verification-gap review (2 findings). Each finding is classified below before grouping; the parent also identified a portable-fixture defect. These reviews assess preparation only, not terminal acceptance.

| ID | Verdict | Evidence and disposition |
| --- | --- | --- |
| B1 — cached helper bytes | medium | `load_host_module` calls the bytecode-aware loader before hashing source. Same-size/timestamp source replacement can execute stale bytecode while reporting the new source digest. Patch: read/bind source once and compile those bytes directly. |
| B2 — host symlinks | medium | Helper execution precedes `bind_host`'s leaf regular-file check; intermediate symlinks are unchecked. Patch with B1: reject symlinked host components and nonregular leaves before reading/executing the bound bytes. This establishes byte identity, not trusted-host authority. |
| B3 — invalid Unicode record | medium | `v2_verify_pair` renders with strict UTF-8; escaped lone surrogates raise `UnicodeEncodeError` outside the pair's `InspectionError` handler. The outer generic catch blocks and abandons the second pair. Patch: classify these demonstrated content failures inside the pair loop and continue. |
| B4 — runtime schema self-validation | low | Readiness schema bytes are bound but not parsed or used by the producer. Changing host schema/helpers can therefore break shape compatibility. Current source output is validated by focused tests and the documented consumer must reject malformed output. Reject additional producer/fallback machinery: arbitrary host-source corruption is not ordinary use, the host remains explicitly untrusted, and no schema-valid outcome grants readiness or authority. |
| B5 — pair schema constants | medium | Pair shape fixes story IDs but permits unrelated paths and expected hashes, even under VERIFIED. Patch: pin the two stories' canonical paths/expected hashes and VERIFIED actual hashes in the schema, with negative schema cases. |
| B6 — reusable consumer semantics | low | A manually forged diagnostic passes structural schema validation. `finish` always derives the diagnostic from the same blockers copied into the ledger, and CLI tests enforce the runbook's additional agreement rule. Reject a new consumer API: no current consumer accepts schema alone, and the described forged-output case needs new validation machinery without changing this producer's outcomes. |
| B7 — schema gitlink uniqueness/order | low | Structural `uniqueItems` permits same-path/different-ID forged rows. The actual producer uses `root_gitlinks`: raw tree traversal, sorted rows and exact equality with duplicate-rejecting `.gitmodules` paths. Reject a new semantic consumer for this manual-output case; verify complete actual tuples under G2 instead. |
| B8 — evidence size bounds | low | Subprocess capture and JSON parsing buffer content; extreme object inventories/blobs may exhaust process memory. No acceptance can follow missing output, as the runbook explicitly requires. Reject new streaming/size-limit policy for an uncommon resource-exhaustion case in this bounded local readiness tool; the existing timeout is not represented as a memory bound. |
| B9 — manifest comparison baseline | medium | The prospective contract asks for a complete diff without naming the before tree when multiple ordered parents exist. Patch the staged documentation with an explicitly owner-bound comparison baseline and merge/fast-forward comparison rule. This adds no live record schema or CLI surface. |
| B10 — canonical byte ambiguity | low | Prospective ordinal/escaping/integer rules leave serializer choices that could alter hashes. Directly clarify Unicode scalar ordering, escapes and zero representation, and supply concrete bytes/hash examples for future conformance; no terminal serializer is adopted. |
| G1 — full-file preservation guard coverage | medium | Independent mutation removed only the exact-byte guard and all 18 tests still passed. Patch a real temporary Git fixture whose changed pair is internally consistent; require FAIL / AD4_RECORD_BYTES_CHANGED / INVALID. No archived pair is regenerated. |
| G2 — exact gitlink tuple coverage | medium | Independent mutation replaced returned object IDs with zeros and all 18 tests still passed. Patch independently derived tuple equality and one changed-gitlink candidate, retaining an overall blocked result. |
| P1 — local archive count in regression suite | medium | The test requires exactly 45 local evidence files; only one `artifacts/v9` file is tracked. A clean full checkout has five tracked pair/artifact files and fails despite correct inspection. Patch snapshotting all available evidence plus mandatory pairs, while keeping the separate session-wide 45-file preservation comparison. |

All retained findings were patched and verified. B1/B2 share the host-loading cause: helpers now execute the exact descriptor-read source bytes, with no-follow directory/leaf checks and no bytecode loading. B3 keeps malformed Unicode local to its pair. B5 pins pair schema facts. G1/G2 and P1 now cover the previously untested facts without requiring local ignored artifacts. B9/B10 clarify prospective text and include four independently checked byte/hash examples. No intent change, lifecycle transition, or deferred-work entry was required. The parent reviewed the patch and independently ran the complete focused suite; all checks below passed. This spec is done only for the standalone preparation; Story 7.1 terminal authority remains unestablished and Story 7.2 remains in-progress. Rejected low findings remain recorded above rather than silently dropped.

## Verification

- `uv run --frozen --no-sync python3 -m pytest -q tests/tooling/test_ad4_acceptance_readiness.py` -- exit 0; **24 passed, 52 subtests passed** (8.33s), after independent review corrections.
- `uv run --frozen --no-sync python3 _bmad/scripts/inspect_story_7_1_acceptance.py --repository . --candidate aefe4003cc94f49021e942adb9cbb8ca9ccf86cd --check` -- exit 2; independently validated against the host readiness schema; `BLOCKED / AD4_PREREQUISITE_ABSENT`, fifteen ABSENT inputs, two VERIFIED preserved pairs, ten ordinal raw gitlinks, both holds ACTIVE, all four authority flags false, `acceptedMainCommit=null`. No stderr. Full inspection output retained at `/tmp/ad4-readiness-final.json` for this session.
- `python3 scripts/check-root-submodules.py --repository .` -- exit 0; `root submodules: PASS`.
- `git diff --check` -- exit 0. Direct byte/link checks of the four new implementation files -- exit 0; LF-only, final newline, no trailing whitespace, all runbook relative links resolve.
- `python3 /tmp/ad4-review-bwhiorjv/verify_preservation.py` against original `/tmp/story-7-1-acceptance-3x3zibco/preservation-before.json` -- exit 0; all 45 original evidence SHA-256 values and nanosecond mtimes unchanged. Approved proposal still hashes to `d05f8fb90fe3337f6d31b94f8268f966c1ebca1ea41ec9592b0f2a602612004d`. No tracked changes, stage/commit/push, gitlink changes, or sprint transition.
- Temporary test-repository commit-message validation: `node_modules/.bin/commitlint --config commitlint.config.mjs --edit /tmp/ad4-fixture-commit-message.txt --verbose` -- exit 0; pinned 21.2.2 reports 0 problems and 0 warnings. This is fixture-only validation; no root commit is created or proposed.

- Parent post-review inspection additionally validated every reported host-file hash and all four prospective canonical byte/hash vectors, plus the exact fixture-message bytes used by the tests. The final readiness ledger contains 20 rows. The fixture message was revalidated with pinned `@commitlint/cli@21.2.2`, 0 problems and 0 warnings. No root commit is created or proposed.
