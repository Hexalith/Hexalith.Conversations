---
title: 'Measure the 4.38.3 candidate and refresh held Epic 16 validation'
type: 'feature'
created: '2026-10-30'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="separate prerequisite; user selected preparation only">

## Intent

**Problem:** Current root tool 4.38.2 differs from Platform's prepared 4.38.3. Accepted tooling bindings fail source verification; the existing collector couples candidate measurement to root adoption and parses an obsolete Platform acceptance phrase.

**Approach:** Deliver one current prerequisite handoff: independently measure the exact published 4.38.3 candidate, retain every adoption/Platform outcome, and add explicitly selected tooling v3 bound to the current tree with runtime still held.

**Decision:** On 2026-10-30 the user selected this separate qualification/binding-refresh plan. This authorizes preparation; implementation requires approval of this spec. Candidate measurement is separate from adopting the root pin or accepting Platform.

## Boundaries & Constraints

**Always:** Follow the [detailed plan](epic-16-4-38-3-qualification-and-binding-refresh-plan.md). Preserve native exits, exact inventories, provenance and distinct PASS/FAIL/BLOCKED/not-applicable states. Keep v3 entry null, independent entry trust empty, ACTIVE hold and all four authority flags false. Use isolated restoration, local Debug and producer-CI Release evidence.

**Never:** Change pins/gitlinks, initialize nested dependencies, rewrite accepted v1/v2 carriers/evidence, suppress scanner paths, amend accepted fixtures, retire hosts, dispatch CI, publish, deploy, send owner messages or implement 16.1–16.3. Root adoption, Platform correction/acceptance and operational/provider/human entry remain owner work.

## I/O & Edge-Case Matrix

| Input | Required outcome |
| --- | --- |
| Verified candidate differs from root pin | Measure artifact independently; retain adoption mismatch without changing the pin |
| Prepared tuple agrees; recorded source differs | Report measured identity failure; no acceptance inferred from README/version agreement |
| Missing archive, declared evidence or owner/native result | BLOCKED for that gate; no substituted success |
| Identity/control mismatch or empty execution | FAIL; retain nonempty diagnostics and restore inputs |
| Historical/new adoption rows | Exact ordered delta and owner disposition for every row; classification grants no waiver |
| Selected v3 runtime command | BLOCKED/2/ENTRY_QUALIFICATION_PENDING; zero runtime tests; outputs preserved |

</frozen-after-approval>

## Code Map

The plan and [input inventory](epic-16-4-38-3-qualification-inputs.json) bind exact sources and commands. Reuse `qualify_epic16_published_artifact.py` bounded collection, `PackagePolicy` in `epic16_tooling.py`, lazy selection and neutral contained-output helpers. Extend existing tests; preserve accepted `epic16_packages.py`/`epic16_trust_v2.py` bytes.

## Tasks & Acceptance

**Execution after approval, in dependency order:**

- [ ] `module/validation/epic-16-published-artifact-qualification/` — create a fresh attempt, capture protected inputs and inspect the normal mapped Debug AppHost before code edits; stop only the created instance. Unhealthy baseline stops edits.
- [ ] `_bmad/scripts/qualify_epic16_published_artifact.py`, `tests/tooling/test_epic16_published_artifact_qualification.py` — add explicit candidate-only/planning-input/predecessor/package selection; preserve legacy defaults and provenance-before-execution. Separate prepared identity, recorded identity and acceptance; verify available declared evidence carriers.
- [ ] Same attempt — collect exact 4.38.3 artifact/source/CI/schema/catalog identity, two positive and 36 malformed-key controls, both adoption formats and package/action observations; retain all failure deltas and owner gates.
- [ ] `_bmad-output/planning-artifacts/epic-16-tooling-v3/`, `_bmad/schemas/epic-16-tooling-v3.schema.json`, `_bmad/scripts/epic16_packages_v3.py`, `epic16_trust_v3.py` — add immutable held contracts/policy, predecessor pins and complete source binding; preserve every measurement obligation.
- [ ] `_bmad/scripts/epic16_tooling.py`, `generate_story_record.py`, `verify_story_16_{1,2,3}.py` — add exact lazy v3 routing and import/schema handling; retain legacy selection without fallback.
- [ ] `_bmad/scripts/tests/test_epic16_tooling_v3.py`, `test_epic16_tooling_v2.py`, `test_epic16_tooling.py` — cover the matrix, 15 native fixture routes, three deterministic final pairs, fault restoration and historical v1/v2 behavior. Actual current legacy commands retain SOURCE_DRIFT.
- [ ] `module/platform-handoff.md`, runtime 16.1 draft and fresh attempt — link measured qualification/held-command ledgers and exact next owner actions; retain draft/backlog and accepted evidence.

**Acceptance Criteria:**

- Given exact 4.38.3, when candidate collection finishes, then actual artifact identity and all 36 controls pass; any missing required artifact/control evidence blocks completion. External tuple/owner gates retain measured FAIL/BLOCKED without acceptance claims.
- Given v3 and preserved predecessors, when fixtures and held commands execute, then all 18 ACs, 32 requirements, graph 38/61, native/final rules and rollback remain unchanged; v3 stays held and legacy evidence remains immutable.
- Given completion of this bounded prerequisite, when the handoff is validated, then source/stream hashes, exact inventories, unchanged protected state and absent runtime outputs are proven. Neither root adoption nor runtime readiness follows.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Artifact measurement may pass while the overall qualification ledger fails. V3 refreshes source verification only; it binds the unchanged root pin and measured candidate as distinct facts. Future pin adoption needs its own qualification and source-binding decision.

## Verification

Run the plan's focused collector/tooling/generator regressions, actual held commands and Story 7.3 guards; require nonempty evaluated checks and deterministic fixture records. Rehash retained evidence and verify exact changed paths and input restoration. `git diff --check` must pass. Historical boundary replay and runtime publication are not-applicable to this prerequisite.
