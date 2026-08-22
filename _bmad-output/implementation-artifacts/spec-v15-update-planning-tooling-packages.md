---
title: 'Publish V15 authority for planning tooling package updates'
type: 'chore'
created: '2026-08-22'
status: 'in-review'
review_loop_iteration: 1
baseline_commit: '6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4'
submodule_promotions: []
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Conversations' planning verifier pins `jsonschema 4.25.0` and `pytest 8.4.1`, but both have compatible stable successors. Directly changing these canonical V9 inputs makes historical publication checks and the evidence boundary fail even though the new lock is valid.

**Approach:** Update the owned Python manifest and lock, preserve all V9–V14 history byte-identically, and publish an additive V15 tooling-environment authority that binds the new versions, exact enforcement files, and committed candidate.

## Boundaries & Constraints

**Always:** Preserve immutable authority and signed evidence; use a two-commit candidate/authority transaction to avoid self-reference; keep manifest/lock versions and PyPI hashes exact; retain nonempty `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` semantics; keep the implementation hold active; preserve and exclude unrelated worktree files.

**Ask First:** Changing any package beyond the two approved pins; widening the exact V15 path allowlist; changing existing V9–V14 identities or bytes; weakening failure semantics; reconciling, committing beyond the two-commit transaction, or pushing.

**Never:** Regenerate or rewrite V9/V13/V14 artifacts, overlays, or historical final-record evidence; treat unavailable validation as pass; allow gitlink changes; silently synchronize the environment during verification; claim hold lift, IR-0 authorization, successor activation, release approval, or push authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Current environment | Manifest and lock select approved versions | V15 validates version, hash, and path parity | Fail on mismatch, extra package, or unexpected path |
| Historical V9 check | Current package bytes differ from V9 candidate | V9 reproduces from committed historical candidate bytes | Fail if historical identity or digest changes |
| Candidate publication | C1 contains package and enforcement changes | C2 binds exact C1 SHA and changed-path set | Block on self-reference, missing C1, or dirty candidate |
| Fault fixture | One version, hash, path, or predecessor is altered | Named stable failure with nonempty ledger | Restore fixture byte-identically after failure |
| Dirty umbrella tree | Unrelated files exist | They remain untouched and outside C1/C2 | Stop on overlap or staged-boundary drift |

</frozen-after-approval>

## Code Map

- `pyproject.toml`, `uv.lock` -- canonical current tooling environment; update only jsonschema and pytest records.
- `_bmad-output/planning-artifacts/v9-authority-bundle-v1.json`, V12/V13/V14 sidecars, and E6/IR-0 artifacts -- immutable predecessor evidence; read-only roots of trust.
- `_bmad/scripts/publish_v15_planning_tooling_environment.py` -- new deterministic V15 publisher/checker.
- `_bmad/scripts/verify_evidence_boundary.py` -- validate immutable V14 plus current V15 exact scope.
- `_bmad/schemas/v15-planning-tooling-environment-authority-v1.schema.json` -- closed V15 artifact contract.
- `_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py`, `test_verify_evidence_boundary.py` -- successor, fault, and anti-vacuity coverage.
- `_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json` -- C2 candidate-bound authority output.
- `.github/workflows/planning-authority-preflight.yml` and `tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs` -- blocking Python/C# consumers with pinned trust anchors.

## Tasks & Acceptance

**Execution:**
- [x] `pyproject.toml`, `uv.lock` -- select jsonschema `4.26.0` and pytest `9.1.1`; preserve the remaining 13-package graph.
- [x] Current V9/E6/IR-0 authority -- preserve every byte, including the committed independent `READY` assessment with the global hold still `ACTIVE`, and validate the existing V9 checker from an isolated checkout of baseline `6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4`; do not weaken its live-byte drift checks.
- [x] V15 schema, publisher, tests, and generated authority -- bind baseline `6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4`, predecessor V9 file SHA-256 `8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3`, predecessor planning candidate `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`, predecessor bundle digest `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055`, C1 commit, exact versions/hashes/modes, the exact eleven-path C1/C2 boundary, zero gitlinks, active hold semantics, unchanged recorded IR-0 result, and a nonempty result ledger.
- [x] Evidence-boundary verifier/tests and planning preflight -- recognize and execute the additive V15 route without weakening V14.
- [x] C# V15 validation test -- independently pin trust anchors and reject version, digest, scope, predecessor, and anti-vacuity faults.
- [x] C1/C2 staged boundaries -- commit only declared files with commitlint-validated `build(deps)` messages; do not push.

**Acceptance Criteria:**
- Given the approved Python versions, when the frozen environment is installed, then manifest, lock, installed metadata, and PyPI hashes agree; the complete predecessor Python suite passes skip-free in an isolated baseline checkout, and the complete current Python suite excluding only the intentionally historical `test_publish_v9_planning_authority.py` module passes skip-free.
- Given immutable V9–V14 bytes and changed current tooling inputs, when historical and V15 checks run, then both pass without rewriting predecessor evidence.
- Given committed C1 and C2, when the evidence boundary evaluates baseline `6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4`, then its exact changed paths, raw gitlink set, hashes, predecessor, hold state, unchanged recorded IR-0 result, and nonempty assertion ledger pass.
- Given unrelated current-worktree files, when either commit is prepared, then no unrelated or gitlink path is staged, modified, or claimed.

## Spec Change Log

- 2026-08-22: Rebased non-frozen execution details from `36febdd` to clean candidate `5900d9f` after the separately authorized E6/IR-0 transaction completed; removed obsolete V9 publisher modifications and made V15 authority-neutral for IR-0.
- 2026-08-22: Rebased again to `6400c09` after the independent IR-0 `READY` report was committed; V15 preserves that result and the still-active global hold without granting any further authority.
- 2026-08-22: Clarified the non-frozen Python verification split: run the complete historical suite at baseline and the complete current suite excluding only the unchanged V9 live-byte drift module, whose rejection of current package bytes is an invariant rather than a regression.
- 2026-08-22: Adversarial review found descendant-head, PR/manual topology, lifecycle-dirty-state, closed-contract, failure-state, and negative-test gaps after V15 had already been pushed. Preserve the public V15 transaction and correct it additively through the approved V16 successor; keep the package pins, exact V15 C1/C2 boundary, V9-V15/IR-0 bytes, active hold, and zero-gitlink invariant.

## Design Notes

V15 is an additive current-environment authority, not a replacement for the V9 planning bundle, V12-V14 checkpoints, or the E6/IR-0 continuation. C1 contains the spec, canonical package files, schema, publisher, Python checks, evidence verifier, workflow, and C# validator. C2 contains only the generated artifact that names C1. This prevents an artifact from hashing a commit that contains itself. The exact combined boundary is eleven paths and contains no gitlink.

## Verification

**Commands:**
- `uv lock --check && uv sync --frozen` plus installed-version assertions -- expected: exact approved versions and a locked 13-package graph.
- In an isolated checkout of baseline `6400c09`, run the complete `_bmad/scripts/tests` suite with its frozen environment; in the current checkout run `_bmad/scripts/tests` with only `test_publish_v9_planning_authority.py` ignored -- expected: both lanes have zero failures, skips, or empty-ledger passes, while a direct live V9 check continues to reject package-byte drift.
- Run the V9 checker in an isolated checkout of baseline `6400c09`, then run live V13, V14, V15, and evidence-boundary `--check` commands against the committed successor -- expected: immutable predecessor authority and additive current authority all pass without teaching V9 to accept live package drift.
- Build and run `Hexalith.Conversations.Conformance.Tests` in Release -- expected: V15 trust-anchor and fault tests pass with zero warnings.
- `git diff --check`, staged exact-set inspection, and raw mode inspection -- expected: only declared C1/C2 paths, zero gitlinks, no unrelated files.
