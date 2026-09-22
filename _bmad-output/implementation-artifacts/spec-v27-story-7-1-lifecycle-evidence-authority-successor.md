---
title: 'Publish the V27 Story 7.1 lifecycle-evidence authority successor'
type: 'bugfix'
created: '2026-09-22'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: 'd02daf519adce39c25d43e7d92f362d58039bbd3'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-v26-story-7-1-committed-candidate-test-correction.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** V24 correctly blocks every marker-free descendant, so new lifecycle-evidence tooling cannot authorize itself from an untrusted candidate. The abandoned V27 design also claimed an invalid C1 `PASS`, learned most C1 bytes from the candidate, and could not externally pin C2's self-referential commit identity.

**Approach:** Prepare an exact eight-path C1 bootstrap for a one-time, externally recorded protected-branch exception, then publish a deterministic record-only C2. C1 remains `BLOCKED`; once the protected workflow proves C1 is its trusted baseline, exact C2 and untouched descendants may return a non-executable `PASS` without an exact C2 hash.

## Boundaries & Constraints

**Always:** Preserve V23–V26 byte-exact. Bind the approved predecessor; exact eight-path C1 scope, modes, tree content, `.gitmodules`, and ten raw gitlinks; record-only C2; protected-host provenance; full-history no-touch rules; truthful parent diffs; closed result semantics; and a nonempty ledger. Preserve publisher `FAIL` versus `BLOCKED`. Both hosts must independently authenticate V27 before import.

**Never:** Treat candidate content as bootstrap authority; make exact C1 pass; self-pin C2's commit hash; add a reusable bypass or signing protocol; reuse V23's execution-grant route; weaken V22/V24 contracts; follow symlink escapes; overwrite a concurrent record; use ambient Git; claim owner approval; set an authority flag true; lift `ACTIVE`; change product code, dependencies, sprint status, submodules, or gitlinks; push or perform the external branch exception from this build.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Candidate before external C1 landing | Protected host does not contain C1 | Existing V24 blocker | No fallback or combined C1+C2 landing |
| Exact C1 | Authorized C1 is the protected host; C2 absent | Nonempty `BLOCKED` / exit 2 | `V27_C2_PUBLICATION_MISSING` |
| Exact C2 | Direct record-only child of protected C1 | Non-executable `PASS` / exit 0 with the record as the parent diff | No lifecycle grant |
| Preserved descendant | C1/C2, `.gitmodules`, and gitlinks untouched in full history | `PASS`; report its actual immediate-parent diff | Unavailable history is `BLOCKED` |
| Governed drift | Any governed path was touched, even if restored | Stable `FAIL` / exit 1 before import | Preserve the specific failing ledger row |

</frozen-after-approval>

## Code Map

- `.github/workflows/planning-authority-preflight.yml:85-254` -- materialize the protected V27 schema, pass `trusted_host_commit`, and validate the closed V27 envelope; keep candidate code outside the trust host.
- `_bmad/scripts/resolve_current_planning_authority.py:218-253,1368-1453` -- reuse sanitized Git; require discovered C1 to be in protected-host ancestry; preserve child `FAIL`; route V27 before V24.
- `_bmad/scripts/verify_evidence_boundary.py:281-313,438-456,1694-1848` -- mirror authenticated sticky dispatch using the event baseline as protected-host provenance.
- `_bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py:249-318,568-636` -- reuse historical authentication, deterministic record, exact-scope, gitlink, and false-flag patterns; do not edit.
- `_bmad/schemas/v22-current-authority-recovery-v1.schema.json:478-744` -- historical exact-eight result contract; do not edit or relax.
- New V27 schema/publisher/test -- own the route-discriminated result definition, trusted Git, `.gitmodules` binding, full-history checks, and symlink-safe atomic no-replace publication.
- Existing resolver/verifier tests -- import shared V27 repository fixtures and cover top-level dispatch, truthful diffs, hostile environments, restored drift, and CLI exit codes.

## Tasks & Acceptance

**Execution:**
- [ ] New V27 schema, publisher, and publisher tests -- implement the closed publication/result contracts, hardened Git and filesystem boundary, deterministic C1/C2 evidence, `.gitmodules`, full-history no-touch checks, and direct CLI `0/1/2` coverage.
- [ ] Resolver, verifier, and their tests -- authenticate V27 through protected-C1 provenance, preserve `FAIL`/`BLOCKED`, report real parent diffs, and prove top-level C1/C2/descendant/drift behavior independently.
- [ ] Protected workflow -- pass the trusted baseline identity and validate V27's complete non-executable result without weakening the legacy V22/V23 branches.
- [ ] Git sequence -- commit this approved spec as the predecessor, build an exact eight-path C1, then a record-only C2; produce the exact hashes and review packet for sequential external landing, but do not push or perform the exception.

**Acceptance Criteria:**
- Given protected-host provenance before C1, when C1 or combined C1+C2 is evaluated, then V24 remains authoritative and no V27 candidate code can authorize itself.
- Given protected C1 without C2, when either C1 host evaluates it, then it returns schema-valid nonempty `BLOCKED` / 2.
- Given record-only C2 or an unrelated descendant after protected C1, when either host evaluates it, then it returns nonempty-ledger `PASS` / 0, `ACTIVE`, and four false authority flags with the truthful parent diff.
- Given a governed path is modified and restored, `.gitmodules` or a gitlink drifts, Git is hostile, a parent is a symlink, or a concurrent record appears, when evaluated or published, then it fails closed without importing candidate code, escaping the repository, or replacing foreign bytes.

## Implementation Notes

The prior C1/C2 objects `18273007...` and `e68c542...` were reset from the branch after review and may be consulted only as abandoned implementation, never cherry-picked or cited as accepted evidence. The external protected-branch exception is a human-owned handoff: it must record actor, time, reason, and exact C1 hash; confirm protected `main == C1`; then land C2 separately. A red C1 push check is expected because its event baseline still runs V24.

The selected design uses protected-host ancestry as the one-time external authorization boundary. A new signing route was rejected because V23 signatures grant execution, and an exact C2 hash was rejected because C2 cannot contain its own commit identity. Direct parent, exact one-path scope, deterministic bytes, and protected-C1 provenance content-bind C2 without that circular requirement.

## Spec Change Log

- 2026-09-22: Published and verified the V27 seven-path C1 plus record-only C2 successor; retained Story 7.1 and V26 `in-progress` and changed no sprint, hold, release, push, submodule, or gitlink state.
- 2026-09-22: Review invalidated the seven-path self-authenticating design. Human renegotiation selected an externally authorized eight-path C1 that remains blocked plus a content-bound record-only C2 as the first V27 pass; the abandoned local C1/C2 commits were removed from branch history.

## Review Triage Log

| Finding | Verdict | Evidence | Route |
|---|---|---|---|
| VG-1 resolver result-schema coverage | high | Pre-verified: exact C1 returns `PASS` with seven `observed.changedPaths`, while the declared result schema requires exactly eight; the workflow checks only shallow envelope fields. | bad_spec |
| VG-2 evidence top-level dispatch coverage | medium | Pre-verified: V27 tests call `authority_route` and `validate_v27_scope` directly, so removal of the `verify()` dispatch could still leave the focused tests green. | patch |
| VG-3 publisher failure CLI coverage | medium | Pre-verified: negative tests stop below `main()`, leaving documented FAIL/1 and BLOCKED/2 exit and nonempty-ledger behavior untested. | patch |
| VG-O1 exact-C1 schema-invalid PASS | high | Independently reproduced: C1 resolves as `PASS` with seven paths and one Draft 2020-12 validation error; C2 has eight paths and validates. | bad_spec |
| BH-1 alternate seven-path C1 accepted | high | The hosts pin only the V27 schema and publisher blobs; the other five C1 blobs are learned from whichever direct seven-path child is discovered and then self-recorded. | intent_gap |
| BH-2 alternate record-only C2 accepted | high | C2 is located dynamically and constrained by parent, scope, and deterministic record bytes, but no external authority pins its commit identity; identical-tree commits with different metadata remain acceptable. | intent_gap |
| BH-3 modify-then-restore history accepted | high | Verification requires unique additions and compares only publication and evaluated-tip entries; an intervening modification followed by restoration is not inspected. | bad_spec |
| BH-4 publisher uses ambient Git boundary | high | The authenticated publisher invokes ambient `git` without the hosts' pinned executable, trusted environment, replacement-object suppression, or timeout, and its result is decisive. | bad_spec |
| BH-5 non-dictionary ledger rows bypass host predicate | false | The pinned publisher constructs the fixed dictionary ledger and validates committed records against the pinned schema plus deterministic equality; a non-dictionary ledger is unreachable without a publisher digest mismatch. | rejected |
| BH-6 resolver collapses FAIL into BLOCKED | medium | A publisher `SuccessorError` with state `FAIL` becomes `ResolutionError(FAIL)`, but `resolve_authority()` catches it and always calls `blocked_result()`. | bad_spec |
| BH-7 `observed.changedPaths` is not the parent diff | high | C2 and descendants report the eight V27 artifacts rather than their actual parent diff; this also produces the demonstrated schema-invalid seven-path exact-C1 envelope. | bad_spec |
| BH-8 `.gitmodules` descendant drift is unbound | high | V25-V27 predecessor checks bind raw mode-160000 entries only; a descendant can alter `.gitmodules` paths, URLs, or branch settings while retaining every gitlink. | bad_spec |
| BH-9 schema permits duplicate manifest rows | false | Schema permissiveness does not create the claimed accepted outcome: verification recomputes the ordered seven-row manifest and requires full document and canonical-byte equality. | rejected |
| BH-10 schema permits duplicate gitlink rows | false | Verification recomputes the ten raw gitlinks and requires full deterministic document equality, so duplicate or substituted rows cannot pass. | rejected |
| BH-11 schema permits duplicate assertion rows | false | `build_document()` supplies the fixed ten-row ledger and committed-record verification requires exact deterministic equality; schema-only duplicates are not accepted. | rejected |
| BH-12 record parent symlink escapes the repository | high | `write_document()` validates C1 inputs but follows `target.parent`; `mkstemp` and `os.replace` can therefore install the record outside the repository through a symlinked planning-artifacts directory. | bad_spec |
| EC-1 alternate C1 host/test blobs accepted | high | Same independently verified root cause as BH-1: exact path/mode plus two pinned blobs does not authenticate the other five C1 blobs or the C1 commit/tree. | intent_gap |
| EC-2 restored C1 blobs or gitlinks accepted | high | Same independently verified root cause as BH-3: only addition count and final tree state are checked, not intervening touches. | bad_spec |
| EC-3 replacement objects can influence publisher verification | high | The protected hosts sanitize their own Git calls, then execute publisher Git calls that omit `GIT_NO_REPLACE_OBJECTS`; a decisive second-stage history view can differ. | bad_spec |
| EC-4 symlinked record parent redirects publication | high | Same independently verified root cause as BH-12: the output parent components are not lstat/openat constrained beneath the repository. | bad_spec |
| EC-5 concurrent record creation is overwritten | medium | Existence is checked before generation and installation uses replacing `os.replace`; a record created during that window is silently replaced. | bad_spec |
| EC-6 publisher Git calls can hang indefinitely | medium | Every publisher Git subprocess omits a timeout even though both protected hosts depend on its completion. | bad_spec |

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_publish_story_7_1_lifecycle_evidence_authority.py _bmad/scripts/tests/test_resolve_current_planning_authority.py _bmad/scripts/tests/test_verify_evidence_boundary.py _bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py _bmad/scripts/tests/test_publish_story_7_1_committed_candidate_test_correction.py` -- expected: skip-free PASS.
- `python3 _bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py --root . --verify 119c75172b501213307fab9346aa671a22bb18d2` -- expected: V26 remains authenticated and unchanged.
- Run both V27 CLIs at C1, C2, and an unrelated descendant -- expected: C1 `BLOCKED` / 2; C2 and descendant non-executable `PASS` / 0; all envelopes schema-valid and nonvacuous.
- Run both hosts with protected-host provenance before C1 and at C1 -- expected: pre-C1 host rejects V27; C1 host accepts only C2 or preserved descendants.
- `git diff --check` plus raw parent/path/mode/blob/`.gitmodules`/gitlink/history inspection -- expected: exact eight-path C1, record-only C2, and no unexpected touch.
