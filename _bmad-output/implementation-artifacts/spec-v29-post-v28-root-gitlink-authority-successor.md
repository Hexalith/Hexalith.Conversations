---
title: 'Authorize the six post-V28 root gitlink transactions with an additive V29 successor'
type: 'feature'
created: '2026-09-26'
status: 'in-progress'
baseline_commit: 'c7eb9cdcd33be62f8dcb05948ff273f013596885'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-v28-five-root-gitlink-authority-successor.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-7-2-derive-test-path-candidate-submodule-and-gitlink-facts.md'
---

<frozen-after-approval reason="owner-approved exact historical retention; do not broaden without a new decision">

## Intent

**Problem:** Six commits after V28 touched seven frozen root gitlinks. V28 correctly rejects the history, blocking Story 7.2's evidence gate.

**Approach:** Publish an additive V29 authority binding only those transactions, with protected bootstrap C1 and record-only C2. The owner's 2026-09-26 approval retains the commits; the hold remains `ACTIVE`.

## Boundaries & Constraints

**Always:** Pin these six full commits in order: `83bc651a795173591ec7997a14e649b9bd424d5b`, `8442b0d360d93b7f543e80865bee109b632a1e37`, `dc30b020a1d4b21e511aaec12c575d400c62a24c`, `c6fc53bcfd1e94ea544d42109687a7792b8d2449`, `179b4844554dbbff49a173f54c5eabaa4141b843`, `819e45b2f8a493baf1fc92b62e59bdb8dd8696d6`. From V28 C2 `e91e9da4796038a3499b8d0a79735649974a1603` through the last touch, raw Git has 17 mode-`160000` transitions over `references/Hexalith.{Builds,Commons,EventStore,Folders,FrontComposer,Projects,Tenants}`. Canonical NFC UTF-8 LF sorted path-list SHA-256: `fe037611de7434e615d8392ce5416e9c600d84079b12635a4dc39713db8f1ca0`; chronological tab-separated `commit,path,before,after` row-list SHA-256: `32a9df6d61c31a0c2fad3cec23ec06a12da248f95a9533907ea3a08d699abcfe`. Recompute both from raw objects; bind every parent/tree, ten gitlinks, unchanged `.gitmodules`, complete history, exact sets, nonempty ledgers, and truthful immediate-parent diffs.

**Never:** Change V23–V28 or accepted evidence; trust caller facts or candidate content as provenance; accept later touches, restored bytes, or empty ledgers; traverse submodules; push; perform the human-owned protected-branch exception. Keep all four authority flags false.

## I/O & Edge-Case Matrix

| State | Expected result | Diagnostic |
| --- | --- | --- |
| Event-supplied protected base predates V29 C1 | Existing V28 verdict, no V29 self-selection | Existing V28 code |
| Protected base contains C1; candidate is C1 without C2 | `BLOCKED`, nonempty ledger | `V29_C2_PUBLICATION_MISSING` |
| Protected base contains C1; candidate is C2 or untouched descendant | Schema-valid `PASS`, hold `ACTIVE`, four false flags | None |
| Missing history, malformed or changed frozen transition | `BLOCKED` for unavailable facts; `FAIL` for proven drift | Stable `V29_` code |
| Later touch of a governed path, changed record, or changed root gitlink | `FAIL`, even if bytes are restored | Stable `V29_` code |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/publish_v28_five_root_gitlink_authority.py` and schema/tests -- reuse C1/C2 projection; preserve V28 bytes.
- `_bmad/scripts/resolve_current_planning_authority.py:2019` and `_bmad/scripts/verify_evidence_boundary.py:2193` -- independent V29 dispatch before V28.
- `.github/workflows/planning-authority-preflight.yml` -- event base, pinned schema, protected result validation.
- `_bmad/scripts/tests/test_resolve_current_planning_authority.py` and `test_verify_evidence_boundary.py` -- route and fault fixtures.
- `docs/runbooks/evidence-boundary-validation.md` -- publication and human landing procedure.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/schemas/v29-post-v28-root-gitlink-authority-v1.schema.json` and `_bmad/scripts/publish_v29_post_v28_root_gitlink_authority.py` -- closed deterministic C1/C2 record binding the ledger.
- [x] `_bmad/scripts/resolve_current_planning_authority.py` and `_bmad/scripts/verify_evidence_boundary.py` -- independent V29 authentication; retain V28 failure.
- [x] `.github/workflows/planning-authority-preflight.yml` -- event-base routing and closed V29 result validation.
- [x] `_bmad/scripts/tests/test_publish_v29_post_v28_root_gitlink_authority.py`, `_bmad/scripts/tests/test_resolve_current_planning_authority.py`, `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- named one-fault matrix fixtures.
- [x] `docs/runbooks/evidence-boundary-validation.md` -- V29 commands and human exception boundary.
- [ ] `_bmad-output/planning-artifacts/v29-post-v28-root-gitlink-authority-v1.json` -- record-only C2; byte-identical regeneration.

**Acceptance Criteria:**
- Given the six approved commits, when either protected host evaluates the frozen ledger, then its 17 raw transitions, seven-path digest, parent/tree chain, and ten final gitlinks match exactly.
- Given a valid event-supplied base at C1, when both hosts evaluate C2 and an untouched descendant, then each returns a nonempty schema-valid `PASS` with an `ACTIVE` hold and a truthful immediate-parent diff.
- Given absent provenance, unavailable history, one changed transition, or a later governed touch, when either host evaluates it, then it returns the matrix's distinct nonpassing result.
- Given V28's original historical candidates, when the focused suites rerun, then their existing results remain unchanged.

## Implementation Notes

- 2026-09-26: The V29 schema, publisher, two independent hosts, protected workflow, runbook, and one-fault fixtures are implemented as uncommitted source. The pinned schema, publisher, and workflow SHA-256 values match their current files. The isolated Git fixture published predecessor/C1/C2 and exercised C2 plus an untouched descendant with a nonempty `PASS` ledger; it is diagnostic, not protected publication.
- Focused `UV_CACHE_DIR=/tmp/v29-uv-cache uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true -k v29` over the publisher, resolver, and verifier test files passed `19`, with `236` deselected. The V28 selection over the resolver and verifier files passed `25`, with `222` deselected. Python compilation and `git diff --check` passed. A broader focused run reported `246` passes and excluded two V24 SSH signature fixtures whose Git temporary-file creation is blocked by this sandbox.
- Root publication is blocked: `git add -- _bmad-output/implementation-artifacts/spec-v29-post-v28-root-gitlink-authority-successor.md` exited `128` with `fatal: Unable to create '.git/index.lock': Read-only file system`. The predecessor, C1, C2, and root V29 record do not exist. No protected event supplied a V29 trust anchor; the hold remains `ACTIVE`.

## Spec Change Log

## Review Triage Log

## Design Notes

Publish this spec as predecessor, then C1 with only the nine tooling/test/runbook paths; C2 changes only its record. C1's pre-bootstrap check remains red. Landing requires separate human exception evidence: actor, time, reason, exact C1 hash, and the complete unpublished ancestry. Local diagnostics do not grant it.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_publish_v29_post_v28_root_gitlink_authority.py _bmad/scripts/tests/test_resolve_current_planning_authority.py _bmad/scripts/tests/test_verify_evidence_boundary.py` -- focused authority and fault fixtures pass.
- Protected `push` or `pull_request_target` workflow on `main`, after human C1 landing -- both hosts receive the event base; C2 must pass with hold `ACTIVE`.
- `git diff --check` -- no whitespace errors.
