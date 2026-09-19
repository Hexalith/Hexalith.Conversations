---
title: 'Publish the pragmatic AR-15 V22 current-authority recovery'
type: 'bugfix'
created: '2026-09-20'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Architecture V16 adopts a repository-owner-reviewed AR-15 recovery, but the committed workflow still invokes the obsolete V21 ruleset route and no V22 current-authority resolver exists. This leaves current planning authority unresolved while Story 7.1 must remain locked.

**Approach:** Publish one direct-child V22 candidate over the selected `main` parent, changing exactly the eight Architecture V16 paths. Replace active V21 checks with a closed-schema V22 resolver that derives graph, path/mode, immutable-prefix, route, and root-gitlink facts from raw Git objects and reports distinct `PASS`, `FAIL`, or `BLOCKED` results with `implementationHold=ACTIVE`.

## Boundaries & Constraints

**Always:** Preserve parent `5cb0104315f5b12868fc7adb5fc8b4ec531d22df` as the candidate's sole parent; preserve the complete Architecture V1–V16 prefix byte-for-byte; append one complete V22 marker binding V16's 7,022 bytes and SHA-256 `7d438ccb69391805973ff827f02457a4441b1e540f1fff0249e4eee0bb26b2ea` plus the V22 recovery record digest; require all eight paths to be mode `100644`; compare exact ordinal root gitlink tuples and `.gitmodules` paths; retain nonempty assertion ledgers and `ACTIVE` for every result; leave authenticated approval outside candidate-authored evidence; integrate only by owner fast-forward after exact-tuple approval and exact-SHA ordinary CI.

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

- `.github/workflows/planning-authority-preflight.yml` -- remove both active V21 publisher gates and ruleset API use; invoke the committed V22 resolver in job `planning-authority`; run immutable V21 tests only from frozen historical tooling.
- `_bmad-output/planning-artifacts/architecture.md` -- immutable 215,037-byte V1–V16 parent blob followed by one V22 marker.
- `_bmad-output/planning-artifacts/v22-current-authority-recovery-v1.json` -- acyclic recovery policy, selected parent/tree, exact transaction, V16 and historical V21 pins, route-inventory digest, and ACTIVE-only nonclaims.
- `_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json` -- one active V22 resolver route; V21 `ci-trust` is historical-only and not invoked; forbidden mechanisms explicit.
- `_bmad/schemas/v22-current-authority-recovery-v1.schema.json` -- closed recovery-policy and resolver-result contract with conditional PASS/FAIL/BLOCKED requirements.
- `_bmad/schemas/v22-workflow-route-inventory-v1.schema.json` -- closed ordinal route inventory.
- `_bmad/scripts/resolve_current_planning_authority.py` -- isolated raw-Git resolver, duplicate-safe JSON/schema loading, marker checks, exact scope/mode/gitlink checks, and exit mapping.
- `_bmad/scripts/tests/test_resolve_current_planning_authority.py` -- object-level happy/fault fixtures, schema-envelope coverage, anti-vacuity, ACTIVE-only, workflow retirement, and historical-byte preservation.
- `_bmad/scripts/publish_story_7_1_successor_authorities.py` and its direct test -- frozen V21 historical verification; do not edit or import as current authority.

## Tasks & Acceptance

**Execution:**
- [ ] Create the two closed schemas and their matching acyclic V22 artifacts.
- [ ] Implement and fault-test the current-authority resolver using committed raw Git objects only.
- [ ] Append the V22 architecture successor and retire active V21 workflow invocations while preserving historical verification.
- [ ] Validate, review, create one Conventional Commit over the frozen parent, and prove the candidate from raw objects.
- [ ] Run the exact committed resolver locally; obtain ordinary-CI evidence for the same SHA; present the tuple for authenticated owner approval without inferring approval.

**Acceptance Criteria:**
- Given the candidate commit, when raw Git graph, diff, tree modes, architecture prefix, and root gitlinks are inspected, then it has exactly the selected parent, exactly eight mode-`100644` paths, unchanged V1–V16 bytes, and parent-equal ordinal gitlink tuples.
- Given any resolver outcome, when its output is validated, then it matches the closed result schema, has a nonempty ledger, distinguishes PASS/FAIL/BLOCKED, and preserves `implementationHold=ACTIVE`.
- Given the current workflow and inventory, when inspected and executed, then no active ruleset or V21 publisher route remains and the ordinary `planning-authority` job checks the exact V22 candidate.
- Given technical PASS and exact-SHA CI success, when evidence is presented, then owner approval remains a separate authenticated action and the next requested authority after post-merge PASS is AD-4 `EXECUTION_ALLOWED`, not Story 7.1 implementation.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Avoid digest cycles: route inventory and schemas are finalized first; the recovery record pins the inventory; the architecture marker pins the recovery record; runtime output supplies candidate commit/tree identities. The recovery record must not embed its own digest or the candidate SHA.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_resolve_current_planning_authority.py` -- expected: nonzero test count, no failures/errors/skips.
- `uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate <candidate> --check` -- expected: exit 0 and schema-valid PASS with ACTIVE hold.
- `npx --no-install commitlint --config commitlint.config.mjs --from <parent> --to <candidate> --verbose` -- expected: exact candidate message accepted.
- Raw `git rev-list`, `git diff-tree`, `git ls-tree`, `git cat-file`, and marker hash checks -- expected: exact graph, scope, modes, prefix, bindings, and gitlink equality.
