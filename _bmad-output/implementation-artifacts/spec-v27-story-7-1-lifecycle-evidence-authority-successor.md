---
title: 'Publish the V27 Story 7.1 lifecycle-evidence authority successor'
type: 'bugfix'
created: '2026-09-22'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 2
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

**Always:** Preserve V23–V26 byte-exact. Bind the approved predecessor; exact eight-path C1 scope, modes, tree content, `.gitmodules`, and ten raw gitlinks; record-only C2; protected-host provenance, defined as the event-supplied protected base of a trigger that is itself branch-filtered to the protected branch; full-history no-touch rules; truthful parent diffs; closed result semantics; and a nonempty ledger. Preserve publisher `FAIL` versus `BLOCKED`. Both hosts must independently authenticate V27 before import.

**Never:** Synthesize, default, or infer a trust anchor when the event supplies no protected base -- V27 must not select, and the existing V24 route stays authoritative; treat candidate content as bootstrap authority; make exact C1 pass; self-pin C2's commit hash; add a reusable bypass or signing protocol; reuse V23's execution-grant route; weaken V22/V24 contracts; follow symlink escapes; overwrite a concurrent record; use ambient Git; claim owner approval; set an authority flag true; lift `ACTIVE`; change product code, dependencies, sprint status, submodules, or gitlinks; push or perform the external branch exception from this build.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Candidate before external C1 landing | Protected host does not contain C1 | Existing V24 blocker | No fallback or combined C1+C2 landing |
| Event supplies no protected base | Manual or unfiltered trigger; empty or all-zero base | V27 does not select; existing V24 blocker | Never fall back to `HEAD^` or an empty anchor |
| Protected host predates `--trusted-host` | Materialized host is a pre-V27 blob | Governed V24 result, not a crash | Pass the flag only to a host that advertises it |
| Exact C1 | Authorized C1 is the protected host; C2 absent | Nonempty `BLOCKED` / exit 2 | `V27_C2_PUBLICATION_MISSING` |
| Exact C2 | Direct record-only child of protected C1 | Non-executable `PASS` / exit 0 with the record as the parent diff | No lifecycle grant |
| Preserved descendant | C1/C2, `.gitmodules`, and gitlinks untouched in full history | `PASS`; report its actual immediate-parent diff | Unavailable history is `BLOCKED` |
| Governed drift | Any governed path was touched, even if restored | Stable `FAIL` / exit 1 before import | Preserve the specific failing ledger row |

</frozen-after-approval>

## Code Map

- `.github/workflows/planning-authority-preflight.yml:54-126,177-292` -- derive `trusted_host_commit` only from `push`/`pull_request_target` (both already `branches: [main]`) with a nonempty, non-zero base; emit empty otherwise and drop the tautological self-comparison. Materialize the protected V27 schema, compare its digest to the pinned constant, pass the flag only to a host that advertises it, and discriminate the V27 envelope on both `V27.` and `V27_` first-ledger ids.
- `_bmad/scripts/resolve_current_planning_authority.py` -- treat an empty `--trusted-host` as absent provenance (return `None`, not a Git error); scope the `error_result` state pass-through to V27 codes so legacy `V16_MARKER_*` and `TRANSACTION_MODE_DRIFT` keep `BLOCKED`/2; recompute `mode`, `objectId` and `sha256` of `observed.changedPaths`, not paths alone; reject multi-parent candidates; run the history guard before deriving any fact.
- `_bmad/scripts/verify_evidence_boundary.py` -- mirror every resolver change above; normalize propagated blocker codes to the `EVIDENCE_V27_*` namespace; surface `effectiveHold`/`implementationHold` and the four authority flags on its own PASS document so AC3 holds at either host.
- `_bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py:249-318,568-636` -- reuse historical authentication, deterministic record, exact-scope, gitlink, and false-flag patterns; do not edit.
- `_bmad/schemas/v22-current-authority-recovery-v1.schema.json:478-744` -- historical exact-eight result contract; do not edit or relax.
- New V27 schema/publisher/test -- close the route discrimination (require a `V27.ROUTE.*` first ledger row and reject anything else, e.g. `oneOf` or a trailing `else: false`); validate the result envelope and the `fail()` envelope against the pinned schema before returning them; report unavailable tooling as `BLOCKED`, never `FAIL`; wrap publication `OSError` in a governed envelope; clean up quarantine files; cover partial (`--filter`) clones.
- `docs/runbooks/evidence-boundary-validation.md` -- declared context: add the V27 route, `--trusted-host`, the protected-base provenance rule, and the `V27_*`/`EVIDENCE_V27_*` code inventory.
- Existing resolver/verifier tests -- import shared V27 repository fixtures and cover top-level dispatch, truthful diffs, hostile environments, restored drift, and CLI exit codes.

## Tasks & Acceptance

**Execution:**
- [ ] New V27 schema, publisher, and publisher tests -- closed publication/result contracts with a mandatory `V27.ROUTE.*` discriminator, self-validated result and `fail()` envelopes, `BLOCKED` for unavailable tooling, governed `OSError` handling, quarantine cleanup, partial-clone coverage, and direct CLI `0/1/2` coverage.
- [ ] Resolver, verifier, and their tests -- empty provenance is absent provenance; legacy `BLOCKED`/2 preserved; full identity recomputation of `observed.changedPaths`; multi-parent rejection; single blocker namespace; hold and the four flags on the evidence document.
- [ ] Protected workflow and its tests -- anchor derived only from a main-filtered trigger with a real base, digest-compared V27 schema, flag passed only to a host that advertises it, both discriminator forms, and a test that drives a real V27 envelope and a pre-V27 host through the executing harness.
- [ ] Runbook -- record the V27 route, provenance rule, and code inventory in the declared-context runbook.
- [ ] Git sequence -- commit this approved spec as the predecessor, build an exact eight-path C1, then a record-only C2; produce the exact hashes and review packet for sequential external landing, but do not push or perform the exception.

**Acceptance Criteria:**
- Given protected-host provenance before C1, when C1 or combined C1+C2 is evaluated, then V24 remains authoritative and no V27 candidate code can authorize itself.
- Given protected C1 without C2, when either C1 host evaluates it, then it returns schema-valid nonempty `BLOCKED` / 2.
- Given record-only C2 or an unrelated descendant after protected C1, when either host evaluates it, then it returns nonempty-ledger `PASS` / 0, `ACTIVE`, and four false authority flags with the truthful parent diff.
- Given a governed path is modified and restored, `.gitmodules` or a gitlink drifts, Git is hostile, a parent is a symlink, or a concurrent record appears, when evaluated or published, then it fails closed without importing candidate code, escaping the repository, or replacing foreign bytes.
- Given a manual or otherwise unfiltered trigger that supplies no protected base, when either host evaluates any candidate, then V27 does not select and the existing V24 blocker is returned.
- Given an envelope whose first ledger row is not a `V27.ROUTE.*` id, when it is validated against the pinned schema, then it is rejected rather than left unconstrained.
- Given a legacy `V16_MARKER_CARDINALITY`, `V16_MARKER_INCOMPLETE`, or `TRANSACTION_MODE_DRIFT` fault, when the resolver reports it, then the result is `BLOCKED` / 2 exactly as before this change.
- Given the protected host is a pre-V27 blob, when the workflow invokes it, then it produces a governed V24 result rather than an unhandled parser crash.

## Implementation Notes

The prior C1/C2 objects `18273007...` and `e68c542...` were reset from the branch after review and may be consulted only as abandoned implementation, never cherry-picked or cited as accepted evidence. The external protected-branch exception is a human-owned handoff: it must record actor, time, reason, and exact C1 hash; confirm protected `main == C1`; then land C2 separately. A red C1 push check is expected because its event baseline still runs V24.

The published sequence is predecessor `377a5c6cf5bf044ec5619cc4dd2464fecbb08317` (tree
`bb4b2852698b092b875aaebce97857abc208d348`), bootstrap C1 `b8f61878ad2cdbf581c88dda15d62e989bdf15a7`
(tree `0f3e95ba5dcd9401854ae4c9fabc2ece65d7d14b`, exactly the eight declared mode-`100644` paths), and
record-only C2 `19f0a7506fc01daa145771de5eaf6a76739a9308` (tree `4632ac4468aa2676c9d0c59a998b545f5f3a94ca`,
blob `e3fa1f9fc86d007df5c52aaea959647d3edbf467`, record digest
`4c9da34de7e25362177afe00201588da5fe038ce3c3b08ff89d3f2644ba7612e`). The pinned roots of trust are schema
`1dffb78f2b550d228405c5d0e849ecfa92851f7676b743849e90af34fc735ae3`, publisher
`51d1099642ecdd12d793f3e6526fad8123a149eb0ddaecabf590fd396b7c17f6`, workflow
`e049096391c6b3088f8f3d13c14959715ff75da504401cac6ea2e1c4d5d5accb`, and bootstrap manifest
`1003ce34e377306ba488752401fa9a1d199c3b1316c5c1ab680c20443b129dd9`. Nothing was pushed and no branch
exception was performed from this build.

The external landing packet is sequential: record actor, time, reason, and exact C1
`b8f61878ad2cdbf581c88dda15d62e989bdf15a7`; land C1 alone under the one-time protected-branch exception
and expect a red C1 check because its own event baseline still runs V24; confirm protected `main == C1`;
then land C2 `19f0a7506fc01daa145771de5eaf6a76739a9308` normally, where the C1 baseline authorizes it. A
third commit `704edbdbfbc9ab9759ad179a58a710794ed4d6d9` is a preserved descendant, not part of the
exception: it repins the V23 entry-authority fixtures to the immutable V23 workflow blob, which the V27
workflow successor would otherwise redefine from the worktree. It should land with or immediately after C2
so the Python lane is green again.

Unavailable history is a distinct stable outcome. `V27_BOOTSTRAP_PARENT_DRIFT` reads as drift, so a
depth-limited clone, a bootstrap without an available parent, and any shallow repository now resolve to
`V27_HISTORY_UNAVAILABLE` (`EVIDENCE_V27_HISTORY_UNAVAILABLE` at the evidence host), matching the V22
`HISTORY_UNAVAILABLE` precedent and the runbook's required Git-history fault. The publisher refuses both
`--verify` and `--write` on partial history before deriving any fact from it; each host applies the same
guard only after V27 owns the route, so a protected host that predates C1 still leaves the existing V24
blocker in force.

The selected design uses protected-host ancestry as the one-time external authorization boundary. A new signing route was rejected because V23 signatures grant execution, and an exact C2 hash was rejected because C2 cannot contain its own commit identity. Direct parent, exact one-path scope, deterministic bytes, and protected-C1 provenance content-bind C2 without that circular requirement.

## Spec Change Log

- 2026-09-22 (review iteration 2): Review iteration 2 routed BH2-3 to `intent_gap` -- `workflow_dispatch`
  carries no branch filter and supplies no protected base, so the anchor fell back to `HEAD^` and a
  self-pushed C1/C2 pair could self-authorize on any branch while the record asserted
  `selfAuthorized: false`. The human selected the fail-closed resolution: derive the anchor only from a
  trigger already branch-filtered to the protected branch, and never synthesize one. Amended the frozen
  Boundaries and matrix accordingly, and folded in the `bad_spec` cluster: open schema route
  discrimination, the workflow's `V27.`-only discriminator, the legacy `V16_MARKER_*` /
  `TRANSACTION_MODE_DRIFT` regression from `BLOCKED`/2 to `FAIL`/1, the pre-V27-host parser crash,
  path-only `observed.changedPaths` comparison, merge-commit acceptance, shallow-only history detection,
  unpinned schema digests in consuming tests and the workflow, the unvalidated publisher and `fail()`
  envelopes, the missing hold/flags on the evidence document, and the untouched declared-context runbook.
  Known-bad state avoided: shipping an immutable C1 whose authorization anchor can be fabricated, and
  whose landing event crashes instead of blocking.

  KEEP -- these must survive re-derivation unchanged in substance: the exact eight-path C1 plus
  record-only C2 topology and the predecessor binding; protected-host ancestry as the authorization
  model with no self-pinned C2 hash; `require_complete_history` plus the no-available-parent branch and
  the distinct `V27_HISTORY_UNAVAILABLE` / `EVIDENCE_V27_HISTORY_UNAVAILABLE` code, with its eight tests;
  the pinned `/usr/bin/git` with `--no-replace-objects`, trusted environment and timeout; `openat` /
  `O_NOFOLLOW` / `O_EXCL` + `os.link` publication with no-replace semantics; the `.gitmodules` plus ten
  raw mode-160000 gitlink binding; the full-history no-touch check that catches modify-then-restore;
  truthful immediate-parent diffs; the deterministic canonical record and its self-excluded manifest
  digest; V23-V26 byte-exact preservation; and descendant `D1`'s repin of the V23 workflow root of trust
  in consuming test source.

- 2026-09-22: Matrix Test Audit found row 4's `Unavailable history is BLOCKED` clause uncovered.
  Gave truncated history its own stable `V27_HISTORY_UNAVAILABLE` code instead of a parent-drift
  diagnosis, covered it at the publisher and both host boundaries, and rebuilt C1/C2 with the two
  descendants rebased onto the new record.
- 2026-09-22: Published the externally authorized eight-path C1 `b8f61878…` and record-only C2
  `19f0a750…`; exact C1 stays `BLOCKED`, C2 and the preserved descendant return non-executable
  `PASS`, and no sprint, hold, release, push, submodule, or gitlink state changed.
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

| **-- Review iteration 2 (eight-path design) --** | | | |
| BH2-1 / EC2-9 workflow discriminator misses `V27_` rows | high | Reproduced: `error_result` sets the first ledger id to the blocker code, and `"V27_HISTORY_UNAVAILABLE".startswith("V27.")` is False, so every resolver-boundary V27 failure skips the protected V27 schema validation. | bad_spec |
| BH2-2 / EC2-1 schema route discrimination is open | high | Reproduced against the committed schema: a `PASS`/0 envelope whose first ledger row is `V27.SUCCESSOR.01` or `V27_HISTORY_UNAVAILABLE` matches no `if/then` branch and validates with empty `changedPaths` and empty gitlinks. | bad_spec |
| BH2-3 `trusted_host_commit` is never proven to be the protected branch | high | Verified: `workflow_dispatch` carries no branch filter and supplies neither `pull_request.base.sha` nor `event.before`, so baseline falls back to `HEAD^` and a self-pushed C1/C2 pair self-authorizes on any branch, contradicting `selfAuthorized: false`. | intent_gap |
| BH2-4 / VG2-O1 local lifecycle gates can never reach a V27 PASS | medium | Confirmed by my own run: the gate without `--trusted-host` returns `EVIDENCE_V24_TOOLING_MANIFEST_DRIFT`; the same gate with `--trusted-host b8f6187` returns PASS/21 rows. Fix edits skill files (agent context). | defer |
| BH2-5 declared context runbook was never updated | medium | `docs/runbooks/evidence-boundary-validation.md` is declared spec context and is cited as justification for `V27_HISTORY_UNAVAILABLE`, but contains no V27 route, no `--trusted-host`, and no `V27_*` code inventory. | bad_spec |
| BH2-6 hosts compare only the paths of `observed.changedPaths` | medium | Verified by reading: both hosts build `declared`/`truthful` from `row.get("path")` alone, so fabricated `mode`/`objectId`/`sha256` pass; the untruthful-diff tests also mutate paths, so identity is never the trigger. | bad_spec |
| BH2-7 evidence-host blocker codes are split across two namespaces | low | Verified: `validate_v27_scope` re-raises the imported publisher blocker verbatim, so some codes are `EVIDENCE_V27_*` and others bare `V27_*`; a consumer filtering on the prefix drops the propagated set. | patch |
| BH2-8 / EC2-10 consuming tests and workflow do not pin the schema digest | medium | Verified: envelope helpers load `ROOT / SCHEMA_PATH` with no digest assertion, and the workflow prints `v27-result-schema=<sha>` without comparing it to the pinned constant. Violates runbook invariant 7. | bad_spec |
| BH2-9 the `fail()` envelope is never schema-validated | low | Verified: it is the only envelope emitted for pre-`verify_revision` errors, and the three CLI tests that reach it assert on `result`/`blockers` only, skipping `validate_envelope`. | patch |
| BH2-10 / EC2-5 merge commits pass as descendants on a first-parent diff | medium | Verified: `observation` takes `parents[0]`; the resolver accepts `len(parents) >= 2` and uses `parents[1]`; neither host rejects a multi-parent candidate and no fixture builds a merge. | bad_spec |
| BH2-11 / EC2-7 history guard covers shallow clones only and runs late | medium | Verified: the guard tests `--is-shallow-repository` only, so a `--filter=blob:none` partial clone reports `false`; and `v27_route_selected` calls it after discovery and both ancestry probes, so undiscoverable truncated history falls back to V24. | bad_spec |
| BH2-12 failed publication leaves quarantine dirt; `--write` accepts `--trusted-host` | low | Verified: the `finally` unlinks only the `.tmp.` file, leaving `.quarantine.<uuid>` in a governed directory; `parse_args` accepts `--trusted-host` with `--write`, where it is never read. | patch |
| BH2-13 spec record degrades its own reviewability | false | Rejected on rule: the fix is to edit this build's spec. | rejected |
| VG2-G1 workflow V27 envelope contract is never executed in tests | medium | Pre-verified: the only executing harness always supplies a `FIXTURE.WORKFLOW` ledger row, so the V27 branch is dead in tests; deleting it leaves the suite green once the digest constant is refreshed. | bad_spec |
| VG2-G2 workflow schema materialization only exercised on its `absent` path | medium | Pre-verified: the protected base contains four paths and never the V27 schema, so only the `else` branch runs; a rename turns every protected run red once C1 is the base. | bad_spec |
| VG2-G3 protected host predating `--trusted-host` crashes instead of blocking | high | Pre-verified by execution: the predecessor resolver exits 2 with `unrecognized arguments: --trusted-host`, writes nothing, and the workflow then `json.load`s a zero-byte file and dies under `set -e` — not the V24 verdict the spec's notes promise for the C1 push. | bad_spec |
| VG2-O2 `check_lifecycle_gate_preflight.py` does not require `--trusted-host` | low | Verified as filed; only matters once the gate texts change, which is itself deferred. | defer |
| VG2-O3 DESCENDANT branch does not constrain `changedPaths` in the workflow layer | low | Pre-verified: a DESCENDANT `PASS` with `changedPaths: []` passes the workflow checker; both hosts still recompute the truthful diff, so this is a defence-in-depth gap only. | patch |
| EC2-2 `safe_path` admits control characters the schema pattern rejects | maybe-false | Could not establish that a governed path containing TAB/newline/DEL is reachable in this repository; settling it needs a committed fixture with such a name. | defer |
| EC2-3 publisher result envelope is not schema-validated before return | medium | Verified: `verify_revision` returns `result_envelope(...)` without validating it, so the publisher CLI can print PASS/0 for an envelope both hosts would reject. | bad_spec |
| EC2-4 `write_document` OSError escapes as a raw traceback | low | Verified: `except BaseException` re-raises and `main()` catches only `SuccessorError`, so EACCES/ENOSPC/ENOTDIR exit 1 with a traceback instead of the governed JSON envelope. | patch |
| EC2-6 empty `--trusted-host` blocks instead of routing to V24 | low | Verified: `if trusted_host is None` does not catch `""`, so an unset workflow output reaches `resolve_commit("")` and blocks on a Git error rather than the documented V24 route. | patch |
| EC2-8 missing `jsonschema` is reported as FAIL, not BLOCKED | low | Verified independently before the review: the broad `except Exception` in `validate_schema` catches `ImportError` and raises `V27_DOCUMENT_SCHEMA_INVALID` with state FAIL, mislabelling an unavailable dependency as drift. | patch |
| EC2-11 / EC2-12 legacy V16 and V22 exit codes regressed | high | Reproduced: `ResolutionError` defaults to `state="FAIL"`, so replacing `blocked_result` with `error_result(..., error.state)` flips `V16_MARKER_CARDINALITY`, `V16_MARKER_INCOMPLETE` and `TRANSACTION_MODE_DRIFT` from BLOCKED/2 to FAIL/1. The frozen text asked only to preserve the *publisher's* FAIL vs BLOCKED. | bad_spec |
| EC2-13 evidence host returns no hold and no authority flags | medium | Verified: the evidence PASS document's top-level keys are `assertionLedger, baseline, blockers, candidate, changedPaths, repository, result, schemaVersion, worktreePaths` — no `effectiveHold` and none of the four flags, though AC3 requires them from *either* host. | bad_spec |

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_publish_story_7_1_lifecycle_evidence_authority.py _bmad/scripts/tests/test_resolve_current_planning_authority.py _bmad/scripts/tests/test_verify_evidence_boundary.py _bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py _bmad/scripts/tests/test_publish_story_7_1_committed_candidate_test_correction.py` -- expected: skip-free PASS.
- `python3 _bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py --root . --verify 119c75172b501213307fab9346aa671a22bb18d2` -- expected: V26 remains authenticated and unchanged.
- Run both V27 CLIs at C1, C2, and an unrelated descendant -- expected: C1 `BLOCKED` / 2; C2 and descendant non-executable `PASS` / 0; all envelopes schema-valid and nonvacuous.
- Run both hosts with protected-host provenance before C1 and at C1 -- expected: pre-C1 host rejects V27; C1 host accepts only C2 or preserved descendants.
- `git diff --check` plus raw parent/path/mode/blob/`.gitmodules`/gitlink/history inspection -- expected: exact eight-path C1, record-only C2, and no unexpected touch.

**Observed results:**
- `PASS` -- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true` over the five declared
  files: 269 passed, zero skipped, zero xfailed, zero xpassed.
- `PASS` -- V26 at `119c75172b501213307fab9346aa671a22bb18d2`: 9/9 assertions, hold `ACTIVE`, all authority
  flags false; V25 and V26 bytes unchanged.
- `BLOCKED` / 2 -- publisher, resolver, and evidence CLIs at exact C1 with protected host C1:
  `V27_C2_PUBLICATION_MISSING`, route `V27.ROUTE.C1`, seven-row ledger, eight truthful `observed.changedPaths`.
- `PASS` / 0 -- the same three hosts at C2 (route `V27.ROUTE.C2`, one-path record diff) and at descendant
  `704edbdb…` (route `V27.ROUTE.DESCENDANT`, its own one-path diff), each with an eleven-row ledger, `ACTIVE`
  hold, four false authority flags, and ten raw root gitlinks.
- `BLOCKED` / 2 -- both hosts with protected-host provenance before C1, and with no provenance at all: V24
  stays authoritative and returns `V24_TOOLING_MANIFEST_DRIFT` / `EVIDENCE_V24_TOOLING_MANIFEST_DRIFT`.
- `BLOCKED` / 2 -- unavailable history at every boundary: a depth-1 and a depth-2 `file://` clone and a
  bootstrap with no available parent each return `V27_HISTORY_UNAVAILABLE` /
  `EVIDENCE_V27_HISTORY_UNAVAILABLE` with a nonempty ledger from `verify_revision`, `--verify`, `--write`,
  `v27_route_selected`, `authority_route`, `validate_v27_scope`, `verify`, and both host command lines.
  No boundary returned `PASS`, `FAIL`, or `not-applicable`, and no record was written.
- `PASS` -- `git diff --check`, exact parent/scope/mode inspection, and byte-identical `.gitmodules`
  (`b6eb7403…`) plus ten raw mode-`160000` gitlinks across the predecessor, C1, C2, and the descendant.
- `PASS` -- step-04 submodule-promotion gate at baseline `d02daf51…` and committed `HEAD`: exit 0, with no
  declared rows and no changed gitlinks.
- `PASS` -- `test_publish_story_7_1_entry_authority.py` and `test_static_anti_skip_guard.py` at the tip:
  148 passed, zero skipped, confirming descendant `704edbdb…` restores the lane that C1 and C2 leave red.
- `BLOCKED` -- step-04 lifecycle evidence-boundary gate at baseline `d02daf51…` and committed `HEAD`, run
  without protected-host provenance exactly as that gate specifies: `EVIDENCE_V24_TOOLING_MANIFEST_DRIFT`,
  exit 2, one-row ledger. This is the designed pre-exception state rather than a regression -- V24 remains
  authoritative until the protected host contains C1. The same gate with `--trusted-host b8f61878…` returns
  `PASS` with a twenty-one-row ledger including `V27-SCOPE-01`. Lifecycle status therefore stays
  `in-progress`, and the step-04 review layers were not run.
- `FAIL` -- pre-existing and out of V27 scope: twelve `test_publish_v18_package_environment_authority.py`
  rows blocked by `PACKAGE_NPM_LOCK_PARITY_DRIFT` from a later npm pin bump, and one
  `test_generate_preservation_traceability_manifest.py` row blocked by an RC2 restore-receipt digest
  mismatch. Both reproduce without any V27 path and neither touches a governed path.
- `BLOCKED` -- Story 7.1 completion. V27 grants no approval, execution, release, or push authority, and the
  one-time protected-branch exception remains a human-owned handoff that was not performed here.
