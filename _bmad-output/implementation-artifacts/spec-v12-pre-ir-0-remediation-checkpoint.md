---
title: 'Authorize and execute the V12 pre-IR-0 remediation checkpoint'
type: 'chore'
created: '2026-08-04'
status: 'in-progress'
baseline_commit: '53cb3718ca2aeb72bbb4dc3785bc47f08a2cf3f5'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** V11 requires IR-0 before successor execution, but IR-0 is blocked by rejected Epic 6 evidence, missing current-route gates, fail-open planning checks, incompatible generated context, and an unavailable mandatory Python lane. V11 authorizes none of those repairs.

**Approach:** Publish an additive V12 checkpoint owning Epic 6 actions A1-A3, execute its repairs, independently decide the completion-supersession record, and rerun IR-0. Keep the hold `ACTIVE`; `LIFTED` remains separate.

## Boundaries & Constraints

**Always:** Preserve V1-V11 authority, completed records, signed evidence, and prior IR-0 evidence. Derive facts from Git objects or current results. Keep `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` distinct; require exact sets and nonempty ledgers. Keep user-protected submodules outside all writes.

**Ask First:** Any unavailable done-tree object, non-additive authority edit, dependency outside the pinned local environment, changed A1-A3 ownership, or need to alter product code, packages, submodule content, or gitlinks.

**Never:** Substitute current bytes for historical evidence; rewrite Story 6.2/6.7 records or statuses; traverse nested submodules; weaken gates; implement successors; create `implementation-hold-v1.json`; claim release approval.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Valid checkpoint | Committed V12 PC and closed authority | Companions reproduce; A1-A3 execute before IR-0 | Drift fails atomically |
| Epic 6 reconstruction | Recorded candidates and actual done commits | Exact trees, paths, ten gitlinks, builds, tests, promotion results | Unavailable evidence is `BLOCKED` |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/generate_story_record.py:1983` and `_bmad/scripts/verify_submodule_promotion.py:622` -- reusable history parsing plus the missing inspectability diagnostic.
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:985` -- frontmatter failure, mutable fallback, and vacuous signature check.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad/scripts/publish_v9_planning_authority.py`, `_bmad/scripts/tests/test_publish_v9_planning_authority.py`, and `_bmad/schemas/v12-pre-ir0-remediation-authority-v1.schema.json` -- author append-only `E6-REMEDIATION`, its inventory, downstream ownership, and `ACTIVE` rule.
- [ ] `pyproject.toml` and `uv.lock` -- pin pytest/jsonschema and make the frozen IR-0 command authoritative.
- [ ] `_bmad/scripts/verify_epic_6_completion_supersession.py`, `_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py`, `_bmad/schemas/epic-6-completion-supersession-v1.schema.json`, and `_bmad-output/planning-artifacts/epic-6-completion-supersession-contract-v1.json` -- derive exact Story 6.7/6.2 done-tree evidence without current-tree substitution.
- [ ] `_bmad/scripts/verify_evidence_boundary.py`, `_bmad/scripts/tests/{test_verify_evidence_boundary.py,test_verify_submodule_promotion.py}`, and `{.agents,.claude}/skills/{bmad-build,bmad-build-auto,bmad-dev-story,bmad-code-review}` completion files -- gate all twelve mirrored routes before lifecycle writes and remove retired-route assumptions.
- [ ] `_bmad-output/implementation-artifacts/epic-6-context.md`, `{.agents,.claude}/skills/{bmad-build,bmad-build-auto}/{compile-epic-context.md,step-01-clarify-and-route.md}`, `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs`, and `.github/workflows/planning-authority-preflight.yml` -- preserve frontmatter, fail closed on history/signature faults, and run preflight.
- [ ] `_bmad/scripts/publish_v9_planning_authority.py`, `docs/release-evidence/epic-6-completion-supersession-v1.{json,md}`, `_bmad-output/planning-artifacts/epic-6-completion-supersession-decision-v1.json`, and `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-04-ir-0-v12.md` -- bind the committed PC, publish companions, obtain the independent decision, and rerun IR-0.

**Acceptance Criteria:**
- Given the V12 PC, when publication is checked, then checkpoint scope, bundle rows, graph, active-route inventory, and ten gitlinks match exactly with a nonempty ledger.
- Given Story 6.7 candidate `aa2b6b7d05d277e1c083252462b9c8244914970e`/done `29def441408becfbbbdc5c59b9af14a7717cb21f` and Story 6.2 candidate `2971ab79efcf3ef11d4fba7b9139d7cae457a3f9`/done `e480c3f3176cdc3d911baf91eb3e7a8cd38874aa`, when reconstruction runs, then exact paths, raw gitlinks, rebuilt tests, promotion results, and the independent decision are bound without rewriting predecessors.
- Given any active route or authority boundary, when removal, displacement, decoy, parity, unavailable-history/submodule, malformed-context, wrong-signature, skipped-test, or empty-ledger faults run, then the transition fails visibly and fixtures restore byte-identically.
- Given accepted A1 evidence and green A2/A3 gates, when independent IR-0 reruns at the same PC/bundle, then it records `READY`, the hold remains `ACTIVE`, and Story 7.1 remains unstarted.

## Spec Change Log

## Verification

**Commands:**
- `uv sync --frozen && uv run --frozen python3 -m pytest -q _bmad/scripts/tests` -- expected: non-vacuous pass, zero failures/skips/not-run.
- `uv run --frozen python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` -- expected: exact V12 PC and bundle.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release -m:1` and direct V9/V8/architecture classes -- expected: zero failed/skipped/not-run.
- `git diff --check` plus exact changed-path verification -- expected: no whitespace, missing/unexpected path, submodule-content, or gitlink change.
