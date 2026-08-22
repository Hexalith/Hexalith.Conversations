---
title: 'Correct durable planning-tooling lifecycle authority'
type: 'bugfix'
created: '2026-08-22'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '08a4bdcc5a18067f8f93c777055d8097987a9da2'
submodule_promotions: []
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-approved additive repair of pushed V15 — do not modify unless human renegotiates">

## Intent

**Problem:** Pushed V15 proves its original package transaction, but its live gates assume `HEAD` is exactly C2, fail on ordinary descendants and PR/manual topology, reject required lifecycle metadata or unrelated dirty work, and lack several closed-contract and failure-path checks.

**Approach:** Preserve V15 and all historical authority, correct its consumers additively, and publish V16 as the durable successor. V16 validates the original V15/V16 transactions from committed Git objects while allowing descendant candidates, then records lifecycle completion in one separately authorized commit.

## Boundaries & Constraints

**Always:** Keep `jsonschema 4.26.0`, `pytest 9.1.1`, the 13-package lock graph, V9-V15/IR-0 bytes and identities, `READY` with hold `ACTIVE`, exact committed path sets, single-parent C1/C2 transactions, zero committed gitlink changes, nonempty ledgers, and distinct `PASS`/`FAIL`/`BLOCKED`/`not-applicable`. Report unrelated worktree paths truthfully but exclude them from committed candidate scope.

**Ask First:** Any package, historical authority, signed evidence, root gitlink, submodule checkout, path outside the declared code map, fourth commit, or push.

**Never:** Rewrite or force-push public V15; touch `references/Hexalith.Tenants`; initialize nested submodules; weaken historical V9 drift; accept skip/xfail/xpass as pass; lift the hold, alter IR-0 authority, activate successors, authorize release, or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Original or descendant candidate | V15/V16 publication is an ancestor of `HEAD` | Locate and validate its committed C1/C2 blobs; current authority bytes remain identical | Fail on alteration, non-ancestry, merge parent, or scope drift |
| PR, push, or manual run | Head and comparison baseline differ by event | Check the exact branch head and validate authority-owned baselines independently | `BLOCKED` on unavailable history |
| Dirty umbrella tree | Lifecycle metadata or unrelated Tenants checkout differs | Ignore live bytes for committed publication checks; ledger unrelated paths without claiming them | Fail if staged/committed into C1/C2 |
| Malformed input | Lock shapes, UTF-8 paths, installed metadata, schema, or Git history are invalid/unavailable | Stable nonempty `FAIL` or `BLOCKED` result | Never traceback or collapse `BLOCKED` to `FAIL` |
| Closed authority | Unknown fields, weak ledger rows, wrong URLs/hashes/versions, or vacuous assertions | Python and C# reject independently | Restore fault fixtures byte-identically |
| Current Python lane | Historical V9 module mixes drift-dependent and unaffected tests | Run unaffected V9 coverage on current packages; run complete V9 suite at baseline | Any skip, xfail, xpass, or failure blocks |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/publish_v15_planning_tooling_environment.py`, `_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py` -- validate V15 from its committed publication, exact lock normalization, stable malformed-input/installed faults, and single-parent history.
- `_bmad/scripts/publish_v16_planning_tooling_lifecycle.py`, `_bmad/schemas/v16-planning-tooling-lifecycle-authority-v1.schema.json`, `_bmad/scripts/tests/test_publish_v16_planning_tooling_lifecycle.py` -- new closed successor over corrective C1/C2 and immutable V15/package/predecessor identities.
- `_bmad/scripts/verify_evidence_boundary.py`, `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- separate committed scope from reported worktree state, route by authority in the candidate tree, locate publications, and preserve child `BLOCKED`.
- `.github/workflows/planning-authority-preflight.yml` -- checkout PR head, support manual/descendant runs, reject skip/xfail/xpass, run unaffected V9 tests current and complete V9 tests at baseline, then V16 gates.
- `tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs`, `PlanningToolingLifecycleAuthorityV16ValidationTest.cs` -- exact properties, hashes, URLs, ledgers, committed publication discovery, concurrent process drains, and timeouts.
- `_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md` -- record pushed V15 review outcome without changing frozen intent.
- `_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json` and all V9-V14/IR-0 artifacts -- read-only roots of trust.

## Tasks & Acceptance

**Execution:**
- [x] Harden V15 publisher/tests and Python/C# consumers for every matrix fault while preserving the pushed artifact.
- [x] Add closed V16 schema, publisher, Python/C# fault tests, and generated C2 authority.
- [x] Correct evidence routing/state propagation and CI event/current-versus-historical test lanes.
- [x] Commit C1 with only the twelve declared code/spec paths, C2 with only the V16 artifact, and leave the authorized final lifecycle-only commit to the parent; validate every message with pinned commitlint and do not push.

**Acceptance Criteria:**
- Given any descendant of V16 C2, all Python/C# authority gates validate the immutable original transactions without requiring a clean unrelated worktree.
- Given malformed, unavailable, unexpected, multi-parent, or non-passing test state, the named fault returns the correct nonempty result and CI blocks.
- Given baseline `08a4bdc`, C1/C2 contain exactly thirteen distinct paths combined, zero raw-mode `160000` changes, and preserve V9-V15/IR-0 hashes.
- Given current and historical environments, current applicable tests and the complete baseline suite both pass with zero skipped, xfailed, xpassed, failed, or not-run results.

## Spec Change Log

## Design Notes

V16 is a successor, not a rewrite. C1 carries twelve corrective code/spec paths; C2 carries only `_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json`; the later lifecycle-only commit may change only this V16 spec. Validators discover C2 from Git history, require it and C1 to have exactly one parent, compare authority bytes from C2, require C2 ancestry and unchanged authority bytes at the evaluated descendant, and validate C1 against its authority-owned baseline.

## Verification

**Commands:**
- `uv lock --check` and installed-version checks -- exact pins and unchanged 13-package graph.
- Focused V15/V16/evidence fault suites, then current applicable and isolated baseline Python lanes -- all pass with zero non-pass outcomes.
- V13/V14/V15/V16 publication and evidence checks at C2 and a descendant fixture -- `PASS` with nonempty ledgers; injected unavailable history remains `BLOCKED`.
- Release build plus direct V8/V9/V15/V16 planning validators -- zero warnings, failures, skips, or hangs.
- Commitlint, `git diff --check`, exact C1/C2/C3 path audit, immutable hashes, and raw-mode audit -- exact declared paths and zero gitlinks; no push.
