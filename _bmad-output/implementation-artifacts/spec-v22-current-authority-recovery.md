---
title: 'Publish the pragmatic AR-15 V22 current-authority recovery'
type: 'bugfix'
created: '2026-09-20'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c610fbb8c5491fb0d7987c2f8294199887f4a44c'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Architecture V16 adopts a repository-owner-reviewed AR-15 recovery, but the committed workflow still invokes the obsolete V21 ruleset route and no V22 current-authority resolver exists. This leaves current planning authority unresolved while Story 7.1 must remain locked.

**Approach:** Publish one direct-child V22 candidate over the selected `main` parent, changing exactly the eight Architecture V16 paths. Replace active V21 checks with a closed-schema V22 resolver that derives graph, path/mode, immutable-prefix, route, and root-gitlink facts from raw Git objects and reports distinct `PASS`, `FAIL`, or `BLOCKED` results with `implementationHold=ACTIVE`.

## Boundaries & Constraints

**Always:** Preserve current local and remote `main` parent `c610fbb8c5491fb0d7987c2f8294199887f4a44c` as the candidate's sole parent; preserve the complete Architecture V1–V16 prefix byte-for-byte; append one complete V22 marker binding V16's 7,022 bytes and SHA-256 `7d438ccb69391805973ff827f02457a4441b1e540f1fff0249e4eee0bb26b2ea` plus the V22 recovery record digest; require all eight paths to be mode `100644`; compare exact ordinal root gitlink tuples and `.gitmodules` paths; retain nonempty assertion ledgers and `ACTIVE` for every result; leave authenticated approval outside candidate-authored evidence; integrate only by owner fast-forward after exact-tuple approval and exact-SHA ordinary CI.

**Never:** Modify the historical V21 record, schema, publisher, or tests; invoke or add GitHub rulesets, external validators, no-bypass/check-run mechanisms, or nonces; modify product code, dependencies, submodules, gitlinks, sprint status, or Story 7.1 artifacts; authorize release, push, owner approval, or `EXECUTION_ALLOWED`; use merge, squash, or rebase as AR-15 integration.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Candidate valid | Exact direct-child commit and eight-path transaction | Schema-valid `PASS`, exact observed commits/trees/manifests, all-PASS nonempty ledger, hold `ACTIVE`, exit 0 | No approval claim |
| Semantic drift | Wrong parent, path/mode, marker, digest, route, or gitlink tuple | Schema-valid `FAIL`, nonempty ledger and blockers, hold `ACTIVE`, exit 1 | Stable fault code |
| Evidence unavailable | Missing/malformed object, schema/output, history, or environmental dependency | Schema-valid `BLOCKED`, nonempty ledger and blockers, hold `ACTIVE`, exit 2 | Never coerce to PASS/FAIL |
| Committed main | `main` equals the approved candidate after fast-forward | Resolver reruns against the identical SHA; only `PASS` completes AR-15 | FAIL/BLOCKED keeps recovery incomplete |

</frozen-after-approval>

## Code Map

- `.github/workflows/planning-authority-preflight.yml` -- activate V22, preserve unrelated gates, and run frozen V21 tests only from `239758d396d28372687b73f5dc128405892cb520`.
- `_bmad-output/planning-artifacts/architecture.md` -- append V22 after the byte-exact prefix and bind the frozen V16 block.
- `_bmad-output/planning-artifacts/v22-*.json` and `_bmad/schemas/v22-*.schema.json` -- acyclic policy/route artifacts and closed policy, route, and result contracts.
- `_bmad/scripts/resolve_current_planning_authority.py` -- isolated raw-Git resolver using established safe loading, tree, marker, gitlink, ledger, and exit patterns.
- `_bmad/scripts/tests/test_resolve_current_planning_authority.py` -- object fixtures for the matrix, schema closure, stable faults, anti-vacuity, retirement, and restoration.
- `_bmad/scripts/publish_story_7_1_successor_authorities.py` and its direct test -- frozen V21 history; do not edit or invoke as current authority.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/schemas/v22-*.schema.json` and `_bmad-output/planning-artifacts/v22-*.json` -- create closed acyclic contracts and exact inventories.
- [x] `_bmad/scripts/resolve_current_planning_authority.py` and its direct test -- implement and fault-test schema-valid raw-object resolution.
- [x] `_bmad-output/planning-artifacts/architecture.md` and `.github/workflows/planning-authority-preflight.yml` -- append V22 and activate it while preserving historical V21 verification and unrelated gates.
- [x] Exact eight-path candidate -- validate, commit over the approved parent without pushing, and hand off local PASS plus the tuple; CI, approval, and fast-forward remain owner gates.

**Acceptance Criteria:**
- Given the current workflow and inventory, when inspected and executed, then no active ruleset or V21 publisher route remains and the ordinary `planning-authority` job checks the exact V22 candidate.
- Given technical PASS and exact-SHA CI success, when evidence is presented, then owner approval remains a separate authenticated action and the next requested authority after post-merge PASS is AD-4 `EXECUTION_ALLOWED`, not Story 7.1 implementation.

## Implementation Notes

- Published local candidate `cf82f8008d02b07d48338a545909d97faa302362` (tree `47643a4ebd7a066137fd4b6676f6ccde51b9e075`) as the sole child of the approved parent on `fix/v22-current-authority-recovery`; local and remote `main` remain unmoved.
- Independent task audit added the missing closed `implementationHold=ACTIVE` result field alongside `effectiveHold=ACTIVE`; the existing candidate was amended so the transaction remains one commit.
- Verification passed: 15 V22 matrix tests, 16 workflow/lifecycle tests, committed resolver PASS with 13 all-PASS assertions, evidence-boundary PASS with 23 assertions, actionlint, commitlint, and raw graph/scope/mode/prefix/gitlink checks. Ordinary exact-SHA CI, authenticated owner approval, owner fast-forward, and post-merge PASS remain external gates.
- Review entry is halted before the `in-review` transition: the required system-Python command `python3 _bmad/scripts/verify_evidence_boundary.py --repository /home/administrator/projects/hexalith/conversations --baseline c610fbb8c5491fb0d7987c2f8294199887f4a44c --candidate HEAD` returned `FAIL` / `TOOLING_INSTALLED_VERSION_MISMATCH` because system Python resolves `jsonschema 4.19.2`. The pinned `uv` environment resolves the required `4.26.0` and passes, but does not replace this mandatory gate.

## Spec Change Log

## Review Triage Log

## Design Notes

Finalize schemas and inventory first; the record pins the inventory, the marker pins the record, and runtime supplies commit/tree identities. Embed neither the record's digest nor candidate SHA. Keep both hold fields `ACTIVE`; do not move owner `main`.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_resolve_current_planning_authority.py` -- expected: nonzero test count, no failures/errors/skips.
- `uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate <candidate> --check` -- expected: exit 0 and schema-valid PASS with ACTIVE hold.
- `npx --no-install commitlint --config commitlint.config.mjs --from <parent> --to <candidate> --verbose` -- expected: exact candidate message accepted.
- Raw `git rev-list`, `git diff-tree`, `git ls-tree`, `git cat-file`, and marker hashes -- expected: exact graph, eight-path/mode scope, prefix, bindings, and ten-gitlink equality.
