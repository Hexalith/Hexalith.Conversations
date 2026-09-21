---
title: 'Publish an additive V24 Story 7.1 tooling successor'
type: 'bugfix'
created: '2026-09-21'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 2
baseline_commit: '5a7234b922371b5d0a12085a444d93783263f278'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The accepted V23 request is an immutable exact-nine transaction, so correcting review-proven file-safety and verification gaps in a descendant produces `EVIDENCE_V23_REQUEST_DESCENDANT_SCOPE` or publisher-identity drift. Rewriting V23 would erase accepted evidence.

**Approach:** Publish one exact direct-child V24 tooling-correction transaction over V23. It authenticates the unchanged V23 publication first, then binds the corrected publisher and protected hosts through a new closed correction record without granting Story 7.1 execution.

## Boundaries & Constraints

**Always:** Preserve commit `5a7234b922371b5d0a12085a444d93783263f278`, its publisher digest, request, exact-nine manifest, V22 outcomes, and V23 BLOCKED/false semantics. Authenticate V24 before loading corrected Python; require one direct parent, exact mode-`100644` scope, self-excluding manifest and raw-gitlink equality, duplicate-safe JSON, a nonempty ledger, ACTIVE hold, and execution/release/push false. Reject ordinary V23 descendants unless they carry the exact V24 route.

**Never:** Amend V23, relax its verifier branch, publish an authority/marker, claim approval, change sprint/product/dependency/submodule/gitlink state, push, or claim protected-main acceptance. The existing protected-base host cannot recognize V24; required-check migration remains an external owner gate, not a bypass here.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Exact V24 correction | Direct child of V23 with the closed eight-path transaction | Evidence `PASS` with a nonempty ledger; authority remains non-executable | No approval or hold-lift claim |
| Historical V23 | Evaluate the immutable V23 request commit | Original publisher digest and BLOCKED/false request remain valid | No V24 reinterpretation |
| Unrouted descendant | Marker-free V23 descendant without the V24 record | Existing `EVIDENCE_V23_REQUEST_DESCENDANT_SCOPE` | `BLOCKED`, never fallback |
| V24 drift | Wrong topology, manifest, blob, schema, or gitlink | Stable V24 FAIL/BLOCKED result | No corrected publisher execution |
| Protected landing | Existing protected-base workflow evaluates V24 | Visible bootstrap blocker until owner coordinates a successor-aware trusted host | Never weaken or bypass the check |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v24-story-7.1-entry-tooling-correction-v1.json` and `_bmad/schemas/v24-story-7.1-entry-tooling-correction-v1.schema.json` -- closed self-excluding contract generated last.
- `_bmad/scripts/publish_story_7_1_entry_authority.py` -- retain historical V23 validation; authenticate exact V24; distinguish the immutable request publication, correction publication, and permitted evidence-source baseline; bind correction generation to the exact V23 `HEAD`; carry the reviewed no-follow, rollback-close, visible-path, all-input stability, and descendant record-mode fixes.
- `_bmad/scripts/resolve_current_planning_authority.py` -- keep the original V23 pin; make any V24 occurrence in ancestry sticky so deletion cannot fall back to V23; authenticate the V24 publisher pin; and validate the complete closed request result, including exact typed nested rows, before any future V23 authority dispatch.
- `_bmad/scripts/verify_evidence_boundary.py` -- route V24 ancestry before the unchanged V23 request branch, independently verify both transactions, reject correction deletion/reversion and descendant record-mode drift, and preserve the V24 correction as the baseline for a later closed evidence/authority route.
- The three corresponding `_bmad/scripts/tests/test_*.py` modules -- prove review faults, historical parity, exact routing, host-owned result validation, correction-to-authority continuity in fixtures, protected-base bootstrap visibility, wrong-baseline generation, ninth-path scope drift, marker-free and deletion/reversion descendants, independent byte/inode drift, rollback after post-write failure, and descendant request/correction mode drift.
- `.github/workflows/planning-authority-preflight.yml` -- unchanged; protected-base materialization remains an external landing constraint.

## Tasks & Acceptance

**Execution:**
- [x] V24 schema/record and publisher -- implement closed correction generation/validation, preserve V23 pins, require exact V23 `HEAD` for generation, generate the record after the other seven blobs, retain a stable final snapshot of every named mode-`100644` single-link input plus the visible record, and quarantine a newly created request/correction after any post-write validation failure.
- [x] Resolver/verifier -- add pinned V24 topology, schema, publisher, all-eight mode, scope, result, and descendant checks without changing V23 rejection; make V24 ancestry sticky across record deletion/reversion; check evaluated request/correction modes; and host-validate exact typed nested rows in the closed non-executable request result before returning it.
- [x] Authority continuity -- keep the historical V23 request identity separate from the V24 correction/evidence scope anchor so an exact later evidence-source and authority fixture can use corrected tooling, while marker-free, deletion/reversion, or unrelated descendants block without legacy fallback.
- [x] Three test modules -- retain the reviewed safety regressions; cover ninth-path scope drift, marker-free and deletion/reversion descendants, independent byte/inode drift, wrong-baseline generation, post-write rollback, descendant request/correction mode drift, input hard links, writer replacement/stale-input races, raw duplicate JSON, hostile corrected-publisher identity, valid/invalid correction CLI paths, closed nested result rows, V23/V24 history, exact routing/tamper, a complete V24-to-authority fixture, and protected-base bootstrap visibility.
- [x] Exact transaction -- create one local direct-child commit containing only the eight declared mode-`100644` paths; exclude all spec, deferred-work, lifecycle, authority, marker, dependency, and gitlink changes.

**Acceptance Criteria:**
- Given immutable V23, when successor-aware hosts evaluate V24, then both lifecycle gates pass nonvacuously over exactly eight paths while execution remains false.
- Given V23 or any malformed/unrouted descendant, when evaluated, then its historical result or stable blocker is unchanged and corrected candidate code is never trusted first.
- Given a synthetic closed evidence-source and authority publication after V24, when successor-aware hosts evaluate the fixture, then scope starts at the V24 correction, the corrected publisher remains pinned, and the host validates the publisher result before accepting it.
- Given record-mode drift, a hostile corrected-publisher identity, a contradictory request result, a hard-linked input, or a writer pathname/input race, when the production route is exercised, then it emits the named stable blocker and never emits a success token or executable result.
- Given V24 appears anywhere in candidate ancestry, when a descendant deletes/reverts the correction route, changes the request/correction mode, or lacks a complete authority marker, then both hosts emit their stable V24 blocker and never dispatch through legacy V23.
- Given correction generation starts away from exact V23 `HEAD`, a ninth publication path appears, a nested result row is malformed, or post-write validation fails, then the command blocks; any file created by that invocation is quarantined and no success token is emitted.
- Given the local V24 candidate, when handed off, then protected landing and required-check migration are explicitly unresolved external gates rather than claimed success.

## Implementation Notes

**KEEP:** Preserve the exact V23 publication, original publisher digest, historical PASS/BLOCKED behavior, V24 direct-parent/exact-eight/self-excluding manifest contract, raw gitlink equality, duplicate-safe JSON, nonempty ledgers, ACTIVE hold, execution/release/push false, and visible protected-base bootstrap blocker. Preserve the exact-correction and synthetic authority success paths, stable topology/manifest/blob/schema/gitlink blockers, hostile-publisher rejection, descriptor-close quarantine behavior, request/correction visible-path revalidation, hard-link rejection, and all previously passing fault tests while re-deriving the V24 route.

## Spec Change Log

- Review loop 1: BH-01, BH-02, BH-08, and BH-12 showed that the first V24 implementation rejected every possible post-correction evidence source, measured authority evidence from immutable V23 instead of V24, and returned the corrected request result without host validation. The Code Map, tasks, acceptance criteria, and design notes now separate historical request identity from the V24 scope anchor, require a complete synthetic correction-to-authority route, require host-owned result validation, and bind the repeated writer/mode/test faults. Known-bad state avoided: an exact V24 correction that passes its own gates but cannot safely serve any later authority transaction. KEEP the immutable V23 semantics and all green V24 boundary/fault evidence listed in Implementation Notes.
- Review loop 2: the final edge-case review showed that V24 record deletion/reversion could select the legacy V23 route, evaluated request/correction mode drift was not closed, nested request-result rows were not host-closed, and the writer was not bound to exact V23 `HEAD`; related review gaps covered ninth-path scope, marker-free descendants, independent byte/inode validation, and post-write rollback. The Code Map, tasks, acceptance criteria, and KEEP set now make V24 ancestry sticky, require exact nested rows and evaluated record modes, bind generation to V23, require rollback of newly created failed publications, and name the missing production tests. Known-bad state avoided: a post-V24 authority executing through restored legacy tooling or a success token describing an invalid/stale publication. KEEP every green V23/V24 identity, safety, fault, and synthetic-authority behavior listed above.

## Review Triage Log

| Finding | Verdict / route | Evidence |
|---|---|---|
| BH-01 | high / bad_spec | `validate_current_request` accepts only the exact correction publication; any evidence-bearing child blocks before `render_authority`, so no post-V24 authority source is reachable. |
| BH-02 | high / bad_spec | Even the exact V24 source compares evidence changes from immutable V23, making all eight correction paths unexpected; V24 must be the evidence-scope anchor while V23 remains the request identity. |
| BH-03 | high / patch | The resolver binds only the seven manifest entries, then reads the record without checking its raw mode, so canonical record bytes at mode `100755` are accepted before corrected code loads. |
| BH-04 | high / patch | The independent evidence host repeats the record-mode omission and can return `PASS` for a mode-invalid eight-path transaction. |
| BH-05 | high / patch | `--write-correction` discards `atomic_write` identity and prints success without visible-path byte/inode revalidation, reproducing the request-writer replacement race. |
| BH-06 | medium / patch | The correction writer snapshots seven inputs once and can report success after an input changes, leaving a stale record that later verification rejects. |
| BH-07 | medium / patch | Worktree schema validation reopens the schema with `Path.read_bytes`, bypassing the no-follow/nonblocking/mode/link/inode checks already required for manifest inputs. |
| BH-08 | high / bad_spec | The resolver returns `request_check_result` directly; unlike the authority path and evidence host, it has no host-owned closed-result validation against contradictory execution claims. |
| BH-09 | false / reject | The claimed wrong authority-ledger digest is currently unreachable because BH-01/BH-02 block every V24-derived authority before the assertion is emitted. |
| BH-10 | medium / defer | Quarantine-path replacement after the descriptor opens can make the returned preserved path stale, but the same omission predates V24 and was not introduced by this transaction. |
| BH-11 | low / reject | Broad fallback can change a malformed V24 diagnostic into a legacy V23 blocker, but it still fails closed; the rare diagnostic-only harm does not justify a new dispatch guard by itself. |
| BH-12 | medium / bad_spec | The suite lacks the complete V24-to-evidence-to-authority route that would expose BH-01/BH-02, alongside the separately logged mode, writer, and result-validation gaps. |
| VG-01 | high / patch | Pre-verified: no correction-writer replacement test exists, and the branch reports success after an `atomic_write` pathname replacement. |
| VG-02 | medium / patch | Pre-verified: neither protected loader has a self-consistent hostile corrected-publisher fixture asserting its V24 identity-mismatch code before execution. |
| VG-03 | medium / patch | Pre-verified: `--verify-correction` has no valid or malformed CLI-level test, so its dispatch and success token can regress while direct validator tests stay green. |
| VG-04 | medium / patch | Pre-verified: the new `st_nlink == 1` input boundary has no regular hard-link test, despite hard-link rejection being an explicit story requirement. |
| VG-05 | high / patch | The record is outside the seven-item manifest and neither independent host separately checks its raw `100644` mode; canonical bytes can therefore pass at mode `100755`. |
| EC-01 | high / patch | Independent trace confirms the resolver omits the correction-record raw-mode check before corrected publisher execution. |
| EC-02 | high / patch | Independent trace confirms the evidence verifier omits the same record-mode check before returning boundary `PASS`. |
| EC-03 | high / patch | Independent trace confirms the correction writer can emit its success token after the visible pathname is replaced. |
| EC-04 | medium / patch | Independent trace confirms manifest inputs are not re-read before correction-write success, allowing a stale declared binding. |
| EC-05 | low / reject | Catch-all V24 lookup fallback changes diagnostics but still reaches a blocking legacy validation path; this unlikely low-impact case is rejected rather than adding dispatch complexity. |
| EC-06 | high / patch | The claim-aware trace independently reproduces the resolver record-mode gap already recorded as EC-01. |
| R2-BH-01 | false / reject | The protected V23 host is intentionally unchanged and its visible rejection is the specified external owner gate; the candidate and handoff make no protected-landing success claim. |
| R2-BH-02 | false / reject | Correction `PASS` means the closed non-executable transaction is internally valid; protected landing is a separate host result, so adding its external blocker to this record would conflate two authorities. |
| R2-BH-03 | high / bad_spec | `render_correction` authenticates V23 objects but never requires worktree `HEAD` to equal V23, so `--write-correction` can emit direct-parent/exact-scope PASS claims from a branch that cannot publish that transaction. |
| R2-BH-04 | medium / patch | A post-write manifest failure leaves a correction newly created by this invocation at the canonical no-clobber path because `_created` is discarded instead of driving ownership-aware quarantine. |
| R2-BH-05 | medium / bad_spec | Inputs are re-read one at a time and no final retained multi-input snapshot is checked, so an early input can become stale while later inputs are scanned and the writer can still emit success. |
| R2-BH-06 | medium / patch | The new request visible-path revalidation likewise discards `_created`; a detected post-write mutation leaves the failed publication at the authoritative path and blocks a safe retry. |
| R2-BH-07 | low / reject | carried from BH-11/EC-05: the same `128`-as-absent probe can blur diagnostics, but later object/history validation still fails closed; the rare diagnostic-only harm does not justify a new route branch. |
| R2-BH-08 | high / bad_spec | The resolver checks only selected `(id, subject, state)` and blocker fields, accepting extra keys and empty details despite the explicit closed host-result contract. |
| R2-BH-09 | medium / bad_spec | Scalar ledger/blocker rows reach `.get()` before type validation and are remapped to `V24_PUBLISHER_EXECUTION_FAILED`, violating the required stable host-owned invalid-result boundary even though execution remains blocked. |
| R2-BH-10 | low / reject | V24 failures retain a legacy `v23-entry-authority` subject, but the machine-readable code/state remain correct; making the cosmetic provenance route-specific requires extra parameterization. |
| R2-BH-11 | false / reject | The repository's required verification runs `--verify-correction` and the production evidence host against committed `HEAD`, so checked-in record or manifest drift is already caught outside the focused unit tests. |
| R2-BH-12 | low / reject | The positive fixture uses production generation, but independently pinned host digests plus hostile-publisher and per-boundary fault fixtures prevent producer agreement from becoming the sole trust evidence; another static fixture adds little. |
| R2-BH-13 | low / reject | Exact POSIX `0644` is stricter than Git's non-executable `100644` abstraction and may reject unusual checkouts, but relaxing this safety boundary is not a direct correction and the specified canonical environment is passing. |
| R2-BH-14 | false / reject | Independent publisher, resolver, and evidence validation is intentional: sharing candidate validation code would weaken the pre-execution trust boundary; concrete parity gaps are triaged separately rather than removing independence. |
| R2-VG-01 | medium / patch | Pre-verified: no negative test publishes a ninth path through all three production entry points, so any exact-eight scope check can regress while the current matrix stays green. |
| R2-VG-02 | high / patch | Pre-verified: the explicit marker-free V24 descendant guards have no production-route tests; removing either guard lets an arbitrary child masquerade as the authenticated correction. |
| R2-VG-03 | medium / patch | Pre-verified: existing replacement tests change bytes and inode together, so either half of `revalidate_owned_file` can regress independently without failing them. |
| R2-EC-01 | high / bad_spec | The resolver selects by current path presence; deleting V24 and restoring authenticated V23 tooling can dispatch a later authority through legacy executable code instead of preserving sticky V24 lineage. |
| R2-EC-02 | high / bad_spec | The evidence host has the same path-presence downgrade and can accept a post-V24 authority without authenticating the correction that exists in its ancestry. |
| R2-EC-03 | high / bad_spec | `validate_correction` checks all eight modes only at the publication and compares descendant bytes; mode-only drift of the evaluated correction/request can still verify successfully. |
| R2-EC-04 | medium / bad_spec | The sequential post-write scan leaves earlier inputs unprotected while later ones are checked, contradicting the claimed stable all-input success boundary. |
| R2-EC-05 | medium / patch | The correction writer detects post-write manifest drift but leaves its newly created canonical record instead of quarantining the owned inode, preventing safe no-clobber retry. |
| R2-EC-06 | high / bad_spec | Exact nested row keys, types, and nonempty details are not enforced by the resolver's V24 request-result validator, so a non-closed result is accepted. |
| R2-EC-07 | medium / bad_spec | The writer can emit success after an already-checked early input changes during the remaining scan; the claim that input races never receive success is therefore not established. |
| R2-EC-08 | high / bad_spec | The claim-aware trace independently confirms that nested result rows remain open despite the acceptance criterion requiring host-closed validation. |
| R2-EC-09 | high / bad_spec | V24 deletion plus restored V23 tooling bypasses corrected-publisher authentication and permits a signed authority to execute through the legacy route. |
| R2-EC-10 | high / bad_spec | The same deletion/reversion descendant violates the promised stable blocker for malformed or unrouted post-V24 history. |
| R2-EC-11 | high / bad_spec | Mode-only descendant drift of the correction/request records is not bound by the CLI's byte comparisons, reproducing the record-mode acceptance gap at the evaluated candidate. |
| R3-BH-01 | false / reject | V24 does not claim to create its own protected trust anchor: the external owner-controlled host migration selects the reviewed successor bytes, while the unchanged protected V23 host visibly rejects arbitrary V24 candidates. |
| R3-BH-02 | false / reject | Deterministic generation requires one canonical emitted representation, which `json_bytes` provides; validators accepting semantically identical closed JSON does not make the generator nondeterministic or executable. |
| R3-BH-03 | low / reject | A changed ledger detail can reach only the independently digest-pinned publisher, whose exact controls then reject it before authority resolution; pinning seven prose strings again in each host adds complexity without an execution outcome. |
| R3-BH-04 | false / reject | carried from R2-BH-01: protected-host rejection and owner-coordinated migration are explicit unresolved external gates, not success criteria this exact-eight transaction may bypass. |
| R3-BH-05 | medium / defer | `worktree_binding` hashes unfiltered worktree bytes and predates V24, so `core.autocrlf=true` can already make V23/V24 generated bindings differ from staged blobs; a repository-wide filtered-byte policy is outside this exact transaction. |
| R3-BH-06 | high / patch | A descriptor-close `OSError` escapes `read_regular_worktree_file` after request creation, bypassing the new domain-error rollback block and leaving the failed request visible. |
| R3-BH-07 | medium / patch | The retained V24 snapshot silently ignores descriptor-close failures and can print success without proving that its locked descriptors closed cleanly. |
| R3-BH-08 | low / reject | carried from R2-BH-13: exact POSIX `0644` is a deliberate stricter worktree safety precondition; relaxing it is not a direct correction to the required Git `100644` publication. |
| R3-BH-09 | high / patch | Repeated full checkouts in the new correction fixtures consumed the temp filesystem's inode budget during this run; sparse materialization is required so the mandated review loop remains runnable. |
| R3-BH-10 | medium / patch | The workflow FAIL checker test asserts only exit `1`, so a checker rejection and an intentionally propagated FAIL are indistinguishable and the intended branch can regress unnoticed. |
| R3-BH-11 | false / reject | Self-consistent manifest mutations are not a candidate-code trust proof; the externally selected protected host is the trust anchor, and hostile publisher/schema plus exact-transaction gates cover code loaded by that host. |
| R3-BH-12 | false / reject | Test blobs are intentionally part of this immutable exact-eight evidence transaction; later test changes require an authenticated successor rather than silently mutating the V24 lineage. |
| R3-BH-13 | false / reject | carried from R2-BH-02: record `PASS` denotes valid non-executable correction controls, while protected landing has its own visible BLOCKED result and must not be folded into this authority. |
| R3-VG-01 | medium / patch | Pre-verified: no test reaches successful `--write-correction`, so its zero exit, token, and exact visible bytes can regress while all current writer tests remain green. |
| R3-VG-02 | medium / patch | Pre-verified: all post-write failure tests use `created=True`; none proves that an idempotent retry preserves a preexisting exact correction when its later snapshot fails. |
| R3-VG-03 | high / patch | Pre-verified: the V24 marker-complete resolver branch is tested only with valid PASS, so bypassing its host-owned contradictory-authority validation would not fail the suite. |
| R3-VG-04 | high / patch | Pre-verified: no V24 marker-complete evidence test proves that authenticated FAIL/BLOCKED results preserve their state/code/detail instead of falling through to a PASS scope assertion. |
| R3-EC-01 | high / patch | Path-limited `git log` can simplify away side-branch add/delete/re-add history, letting the publisher miscount immutable correction publications; full-history traversal is required. |
| R3-EC-02 | high / patch | The resolver's sticky-lineage probe uses simplified history and can miss a TREESAME side-branch V24 occurrence, reopening legacy dispatch. |
| R3-EC-03 | high / patch | The resolver's publication search has the same simplification gap and can authenticate one visible addition while a second correction publication exists in reachable history. |
| R3-EC-04 | high / patch | The evidence host's sticky-lineage probe can likewise prune V24 side-branch history and misclassify the candidate as legacy. |
| R3-EC-05 | high / patch | The evidence host's V24 publication inventory also needs full-history traversal to reject hidden duplicate additions. |
| R3-EC-06 | high / patch | Request post-write rollback catches only `EntryAuthorityError`; close and other ordinary exceptions can leave a newly created request visible after a blocked result. |
| R3-EC-07 | high / patch | Correction post-write rollback has the same ordinary-exception gap, so `MemoryError` or another snapshot exception can strand an unvalidated canonical record. |
| R3-EC-08 | medium / defer | Initial unbounded regular-file reading predates V24 in `worktree_binding`; a repository-wide input-size policy is needed to settle a safe cap, so this pre-existing resource-hardening issue is deferred. |
| R3-EC-09 | high / patch | The new retained snapshot reads until EOF while holding every lock; a noncooperating continuously growing input can hang the writer, so reads must be bounded to the observed snapshot size and then fail drift. |
| R3-EC-10 | high / patch | The claim-aware trace independently confirms that simplified path history can bypass the sticky V24 route across TREESAME merges. |
| R3-EC-11 | high / patch | The claim-aware trace independently confirms that non-domain post-write exceptions bypass quarantine despite the story's every-failure rollback claim. |
| R3-VF-01 | medium / patch | Independent full verification reached `310 passed` but two existing sticky-route regressions failed because the restored sparse fixtures staged a deliberate out-of-sparse descendant without `git add --sparse`; the fixture must preserve sparse materialization while explicitly staging that test path. |

## Design Notes

V24 is a correction overlay, not a replacement request. Hosts prove V23 first, then the V24 direct-child manifest and corrected-publisher pin. Once the correction appears in ancestry, that lineage is sticky: deleting its record or restoring V23 tooling is drift, never a route downgrade. The immutable V23 publication remains the request identity; the authenticated V24 publication becomes the scope anchor for a later closed evidence source and exact authority publication. Exact V24 remains non-executable, arbitrary marker-free descendants remain invalid, and only the already-defined evidence/authority topology may continue past the correction. Closed result validation includes exact key sets and nonempty typed fields for every nested ledger and blocker row.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_resolve_current_planning_authority.py _bmad/scripts/tests/test_verify_evidence_boundary.py _bmad/scripts/tests/test_publish_story_7_1_entry_authority.py` -- expected: exit 0, skip-free PASS.
- `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline 5a7234b922371b5d0a12085a444d93783263f278 --candidate HEAD` -- expected: PASS.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 5a7234b922371b5d0a12085a444d93783263f278 --candidate HEAD` -- expected: PASS with exact eight paths and a nonempty ledger.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline e0b098fa1c056385e28ee8ac0efd0c55dfab324f --candidate 5a7234b922371b5d0a12085a444d93783263f278` -- expected: historical V23 request PASS with its original publisher digest.
- `git diff-tree --no-commit-id --name-only -r HEAD` and `git ls-tree -r HEAD -- <eight V24 paths>` -- expected: exact scope, mode `100644`, no gitlink.
- `npx --no-install commitlint --config commitlint.config.mjs --from 5a7234b922371b5d0a12085a444d93783263f278 --to HEAD --verbose` and `git diff --check` -- expected: accepted commit message and no whitespace errors.

**Final-record gate (2026-09-21):** BLOCKED with `TEST_RESULTS_FAILED`; lifecycle returned to `in-progress`. The candidate-bound rebuild succeeded with zero warnings/errors and seven root test projects passed without skips, but `Hexalith.Conversations.Conformance.Tests` remained 470/473 after all root-declared evidence submodules were initialized. `PreservationTraceabilityManifestValidationTest.BindingsClosuresAndFrozenV1BytesShouldValidateIndependently` rejects the V24 candidate's paths because its frozen overlay permits only the historical `2d2ae57...` path set, and `CurrentControlsAndTierPrerequisiteShouldStayTruthful` reports `BUILD_RECEIPT_ASSEMBLY_MISMATCH` because the final-record contract requires the test assembly to embed V24 candidate `20e2cdd...` while the frozen receipt requires digest `1ba2f3e...`. A third fault-injection test assumes `.git` is a directory and fails in the isolated linked worktree. Remediation requires a separately authorized preservation-evidence successor/tooling correction; widening this exact-eight V24 transaction would violate its approved scope. No final-record bundle or completion-record commit was created.
