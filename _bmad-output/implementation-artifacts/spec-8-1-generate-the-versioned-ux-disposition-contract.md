---
title: 'Generate the versioned UX disposition contract'
type: 'feature'
created: '2026-10-02'
status: 'in-progress'
baseline_commit: '92638b8a2d48f12626db13afcb1f45554cbf3683'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-8-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No candidate-bound bundle exposes the canonical UX sources' 52 decisions and 28 acceptance criteria. The final-record gate rejects Story 8.1's Python and xUnit commands.

**Approach:** Extend the existing gate, generate a closed schema/JSON/Markdown bundle from the two UX sources, and prove inventory, provenance, and non-activation before recording Story 8.1.

## Boundaries & Constraints

**Always:** Use the exact `8.1.json` commands and frozen inventory. Bind `SC-8.1`, Story 7.4's digest, both source paths/versions/hashes, and ordered 52/28 IDs. Use schema `hexalith.conversations.ux-preservation-disposition.v1` with the PRD's required fields. Set every status to `preserved-not-activated`, label historical mappings non-current, and pass the current-change and insertion gates.

**Never:** Edit UX sources, historical mappings, production UI/runtime, gitlinks, accepted Story 7 records, published contracts, or the v1–v8 prefix. Do not activate UX, claim a historical hold lift or release authorization, or import Story 8.2's mutation matrix.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Expected behavior | Blocker |
| --- | --- | --- | --- |
| Valid | Bound sources, 52/28 rows, Story 7.4 | Digest-matched bundle; record `7/7/0/0/0/0` | None |
| Drift | Missing/changed source or ID | Reject | Source or inventory drift code |
| Activation | Missing banner, activated row, invalid current story | Reject | `UX_ACTIVATION_UNAUTHORIZED` / `UX_CURRENT_STORY_INVALID` |
| UI change | Production UI path in candidate | Reject | `UX_PRODUCTION_CHANGE_FORBIDDEN` |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v9/story-contracts/8.1.json`, `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:2550`: frozen contract and blockers.
- `_bmad-output/planning-artifacts/ux-requirement-map.md:25`, `_bmad-output/planning-artifacts/ux-design-specification.md:1323`: ordered inventory and source text; read only.
- `_bmad/scripts/publish_v9_planning_authority.py:187`, `_bmad/scripts/generate_preservation_traceability_manifest.py:432`: frozen IDs/parity and extraction patterns.
- `_bmad/scripts/generate_story_record.py:2799,5532,5830`: replace Story 7 retention and Python/xUnit rejection with contract-derived behavior; `_bmad/schemas/story-final-record-v2.schema.json` owns record shape.
- `.github/workflows/ci.yml:65` and governed routes contain the gate and post-`done` verification. Tooling lane passed 572 tests; A-2/A-3 tracking awaits proof.
- `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs:247` forbids future outputs; scope the assertion to its historical candidate.
- `docs/release-evidence/story-7.4-final-record-v2.json`: immutable predecessor.

## Tasks & Acceptance

**Execution:**

- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — finish A-1: contract-derived retention/facts, Python/xUnit support, candidate/build and output binding, gitlink-only staleness fault.
- [x] `_bmad/scripts/generate_ux_preservation_disposition.py`, `docs/release-evidence/ux-preservation-disposition-v1.schema.json` — derive closed, deterministic, source-bound dispositions and blockers.
- [x] `_bmad/scripts/tests/test_generate_ux_preservation_disposition.py` — cover deterministic output, drift, activation, and fixture restoration.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs`, `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` — implement AC-02–06 and preserve historical assertions.
- [ ] `docs/release-evidence/{ux-preservation-disposition-v1,story-8.1-final-record-v2}.{json,md}`, `_bmad-output/implementation-artifacts/spec-8-1-generate-the-versioned-ux-disposition-contract.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` — produce evidence, insert the record, and close A-1–A-3 tracking only on proof.

**Acceptance Criteria:**

- Given bound sources, when AC-8.1-01 runs twice, then the closed schema/JSON/Markdown bytes and digest match.
- Given frozen inventories, when AC-8.1-02–04 run, then source bindings and ordered 52/28 complete rows pass.
- Given preserved rows and `SC-8.1`, when AC-8.1-05–06 run, then mappings stay non-current and UI changes fail.
- Given six passing results and Story 7.4, when AC-8.1-07 runs, then the record binds all required inputs with `7/7/0/0/0/0`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**

- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests` — current tooling lane passes without failed or errored tests.
- CI's `verify_story_completion_workflows.py` AC-7.3-01/02 commands — both pass; post-`done` route checks remain present.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release` — new selectors compile.
- The seven exact commands in `8.1.json` — each exits `0` with nonempty passing evidence; run the generator twice and compare output bytes.
- `python3 scripts/check-root-submodules.py --repository .` — root declaration and gitlink invariants pass.
