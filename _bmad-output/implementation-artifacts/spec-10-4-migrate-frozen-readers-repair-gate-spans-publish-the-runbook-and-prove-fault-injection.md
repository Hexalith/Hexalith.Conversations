---
title: 'Story 10.4: Adopt the shared evidence boundary'
type: 'refactor'
created: '2026-10-16'
status: 'in-progress'
baseline_commit: '1915a47123737859e3ac31872fa787fa8c0bd1e3'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-10-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Frozen readers duplicate unsafe evidence plumbing; adoption, fault coverage and promotion-span protection lack proof.

**Approach:** Adopt TestSupport, preserve assertion strength, publish guidance and derive acceptance evidence.

## Boundaries & Constraints

**Always:** Preserve 27 paths, assertion identities/expressions, fixtures, pins, tier ownership, original inventories and accepted 10.3 evidence. Require containment, recomputed hashes/payloads, exact diffs, raw gitlink modes, independent trust roots, nonempty ledgers and zero exemptions. Distinguish `PASS`/`FAIL`/`BLOCKED`/`not-applicable`; unavailable history cannot pass. Summary: `9/9/0/0/0/0`.

**Never:** Absorb projection-proof obligations, weaken conformance, change production APIs, restore retired gates, edit customization defaults or alter unrelated submodules.

**Owner decision, 2026-10-16:** Apply the recommended current-policy approach using baseline `1915a47123737859e3ac31872fa787fa8c0bd1e3`. Preserve its twelve gitlinks and reader assertions, all 27 inventory paths, nine AC identities and original provenance. Bind the ten surviving routes and replace AC-09's retired publisher with resolved-guidance verification.

## I/O & Edge-Case Matrix

| Input | Result |
| --- | --- |
| Valid migrated scope | PASS with measured assertions |
| Integrity, adoption, guidance, chain or span defect | FAIL with exact code |
| Unavailable Git/history | Visible skip or BLOCKED; never green |
| Valid non-applicable scope | Explicit not-applicable |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Conversations.TestSupport/` — reuse its five neutral helpers; extend existing seams.
- Planning `v9/inventories/evidence-readers-v1.json` — frozen paths; nine differ at entry. Portable links 17; topology owns live tenant tests.
- `_bmad/scripts/evidence_boundary_current.py` — 10.3-specific pins; needs separate dispatch.
- `_bmad/scripts/generate_story_record.py` — reuse predecessor, receipt and insertion verification.
- `_bmad/scripts/tests/test_verify_submodule_promotion.py` — repair `promotion_gate_span()` follower coupling in hermetic fixtures; retain V12 history.

## Tasks & Acceptance

**Execution:**

- [ ] `_bmad-output/planning-artifacts/story-10.4-current-{binding,contract}-v1.json` — bind approved scope, immutable before-material, provenance, predecessor, exact source/diff sets and nine scenarios.
- [ ] `tests/Hexalith.Conversations.TestSupport/` and its test project — extend discovery/read seams and table-driven chain validation; test bounded streams, history and containment.
- [ ] Frozen 27 paths and owning test `.csproj` files — replace path/hash/Git wrappers with equivalent shared checks. Collection, live-tenant test and `ContractSamples` need meaningful indirect adoption.
- [ ] `tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` — implement AC-01..05; independently pin entry snapshots and compare assertions/fixtures/constants through enumerated plumbing substitutions. Counts alone are insufficient. Prove unchanged projection strength and zero exemptions.
- [ ] `_bmad/scripts/evidence_boundary_adoption.py`, verifier dispatch and `tests/Hexalith.Conversations.TestSupport.Runner/Program.cs` — add 10.4 evaluation using shared helpers; preserve 10.3 consumption.
- [ ] `_bmad/scripts/tests/test_verify_evidence_boundary.py`, promotion tests and `tests/tooling/test_evidence_boundary_current.py` — add `complete_fault_matrix`/`evidence_gate_span_coupling`: named faults, signable/root faults, nested/duplicate/unterminated chains, reader/strength drift, exemptions and span displacement. Retain exact codes, nonempty ledgers and restoration digests.
- [ ] `docs/runbooks/evidence-boundary-validation.md` and `_bmad/custom/bmad-{build,build-auto,review}.toml` — bind current guidance, five sections and exact commands/codes; preserve history/routine policy. Test resolved renders and four guidance mutations.
- [ ] Generator, `_bmad/schemas/story-final-record-v2.schema.json` and generator tests — add closed 10.4 facts, twelve-root shape, eight measured receipts including AC-09, source/predecessor/publication/insertion verification. Derive AC-08 last.
- [ ] `artifacts/v9/10.4/` and `docs/release-evidence/story-10.4-final-record-v2.{json,md}` — commit sources, measure immutable candidate, publish receipts/pair together, insert verbatim and verify. Later source changes require fresh evidence. No push.

**Acceptance Criteria:**

- Given approved scope, when nine ACs execute, then the verified pair binds migration, predecessor, guidance, faults and span proof with `9/9/0/0/0/0`.
- Given required mutations, when real checkers execute, then exact blockers occur and bytes restore.
- Given migrated sources, when current lanes/guards execute, then they pass with assertions preserved.

## Implementation Notes

- Entry is bound to `1915a47123737859e3ac31872fa787fa8c0bd1e3`; all twelve raw gitlinks and accepted 10.3 pair/receipts remain untouched.
- The independently pinned entry snapshot retains all 27 original blobs and exact enumerated plumbing substitutions. Twenty-five readers now call shared discovery/read/hash/Git seams; the unchanged collection and live tenant characterization retain meaningful adoption through their serialized collection and migrated blind-spot evidence owner. ContractSamples supplies repository discovery to the three frozen contract readers without changing sample fixtures.
- The effective binding retains original inventory and contract provenance and names all nine ACs. AC-09 uses current resolved-guidance verification. The ten installed workflow bodies and installed defaults remain unchanged.
- Shared chain validation checks exact ordered nonempty rows and single ordered nonnested markers. Fault fixtures cover all ten named boundary faults, signable/root faults, seven chain variants, reader/helper/inventory/exemption faults, four guidance mutations and exact/displaced evidence and promotion spans. The lifecycle adapter retains successor blocker identities.
- The closed 10.4 final-record facts bind eight measured receipts including AC-09; AC-08 is derived last. Publication and insertion verification retain the committed source candidate and reject later source or receipt drift.

## Spec Change Log

## Review Triage Log

## Verification

Initial focused checks passed: the 28-case `complete_fault_matrix` selector, two `evidence_gate_span_coupling` cases, nine `story104` generator regressions and eight `AdoptionSeamTests` cases. Final source-candidate measurements follow below in the generated record.

Runtime baseline: `aspire start --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json`, `aspire describe --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json`, and `aspire wait conversations --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --timeout 30` passed. All eleven resources were Running/Healthy. The exact Conversations AppHost was stopped successfully after observation; the pre-existing FrontComposer AppHost was left running.

Affected build command (each project individually): `dotnet build tests/Hexalith.Conversations.<Project>/Hexalith.Conversations.<Project>.csproj --configuration Debug --nologo -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`, for TestSupport.Tests, TestSupport.Runner, Conformance.Portable.Tests, Conformance.Tests, Contracts.Tests and IntegrationTests.

- Build affected projects in Debug; use built xUnit `-method`/`-class` filters and approved acceptance commands.
- Run new selectors, current tooling CI lane, both conformance tiers, affected Contracts/Integration smoke tests and TestSupport tests.
- Current tooling CI command: `python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests tests/tooling/test_evidence_boundary_current.py --ignore=_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py --junitxml=/tmp/story104-current-ci-lane.xml`.
- Native current tiers use their Debug assemblies with `-automated sync -failSkips -parallel none -trx /tmp/story104-<tier>.trx`; the class/method exclusions are exactly those in `.github/workflows/ci.yml`, retained independently of this story. Contracts and TestSupport run unfiltered; Integration smoke uses `-class Hexalith.Conversations.IntegrationTests.ScaffoldSmokeTest`.
- Both Story 7.3 guard commands retain their contract, `--scenario AC-7.3-01` / `AC-7.3-02`, and contracted `artifacts/v9/7.3/<scenario>.json` outputs. The scoped 10.4 verifier initially passed with 406 evaluated assertions and no blockers at source commit `7b51671`; the final committed-candidate receipts below supersede that initial observation.
- Run both Story 7.3 guards and scoped verifier before review/completion; verify pair/insertion. Report broad blockers separately.
