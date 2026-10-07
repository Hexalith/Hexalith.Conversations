---
title: 'Story 9.2 portable conformance tiers'
type: 'feature'
created: '2026-10-07'
status: 'in-progress'
baseline_commit: '51aa06b856bcb0aaf22013153cfe02a51b046156'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-9-context.md'
  - '_bmad-output/planning-artifacts/v9/story-contracts/9.2.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Portable assertions compile with Server; consumers lack a portable oracle and two-tier execution proof.

**Approach:** Split compilation under Story 9.1, prove dependencies/assertion preservation, run both tiers, and derive the final record.

## Boundaries & Constraints

**Always:** Preserve identities, tiers, strength, FR-20 membership, and telemetry/status suites. Bind Story 9.1. Separate frozen definitions, executed cases, and controls. Require nonzero passing execution without skips/omissions. Departures need versioned evidence and genuine Quality approval.

**Decisions (2026-10-07, user: "do recommended"):** Preserve the existing historical exclusions and all frozen definitions; compare active identities/cases against the 415-case floor, counting controls separately. Prepare additive current-surface evidence, explicit governance expectations, safe temporary snapshot output, and combined suite checks. Retain old strength hashes; derive and review successor hashes. Approach approval does not approve unseen public drift or final row digests.

**Never:** Change production APIs, dependencies, submodules, v1 bytes, accepted records; waive coverage, weaken assertions, or invent approvals. Roll back the split together, retaining Story 9.1.

## I/O & Edge-Case Matrix

| Input / State | Required behavior | Blocker |
|---|---|---|
| Nonportable evaluated dependency | Reject resolved compile surface | `PORTABLE_TIER_NONPORTABLE_REFERENCE` |
| Missing/renamed/duplicated or weakened assertion | Reject migration | `ASSERTION_INVENTORY_DRIFT`, `ASSERTION_STRENGTH_WEAKENED` |
| Missing declaration, skipped/empty/regressed execution | Reject completion | `TIER_NOT_DECLARED`, `ASSERTION_LEDGER_EMPTY`, `EXECUTED_COUNT_REGRESSION` |

</frozen-after-approval>

## Code Map

- `docs/release-evidence/conformance-oracle-tiering-disposition-v2.json` — 452 frozen methods: 376 portable/76 internal; seven portable validation additions. No recorded source/closure mixes tiers.
- `artifacts/v9/9.1/pre-split/` — verified hashes: 401 executed methods, 415 cases, 412 passes, three failures, 51 exclusions.
- `tests/Hexalith.Conversations.Conformance.Tests/` — link unchanged paths; retain collections. Seven historical controls inspect only their own assembly; add separate live controls.
- `_bmad/scripts/generate_conformance_tiering.py` — reuse `ProjectModel` strength derivation; evaluate both compilation sets.
- `_bmad/scripts/generate_story_record.py` — reuse successor parsing; `.slnx` supplies completion inventory.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/v9/story-9.2-execution-amendment-v1.json` — bind decisions/effective scenarios; preserve frozen contract.
- [x] `tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj` and existing conformance `.csproj` — link approved files, remove duplicate compilation; preserve namespaces/helpers and permitted dependencies.
- [x] `Hexalith.Conversations.slnx`, `.github/workflows/ci.yml` — declare/run both tiers, exclusions, and controls.
- [x] `tests/Hexalith.Conversations.Conformance.Portable.Tests/PortableCompileSurfaceValidationTest.cs` — implement AC02 against evaluated packability, transitive assets, and `ReferencePath`.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/Story92/ConformanceOracleTieringValidationTest.cs` — implement exact AC04/05 selectors across both assemblies.
- [x] `_bmad/scripts/verify_conformance_tiering.py` — verify identities/strengths, approvals, declarations, and execution.
- [x] `docs/release-evidence/conformance-oracle-tiering-migration-v3.json`; existing conformance `GovernanceAuditPairingSafetyNetConformanceTest.cs`, `PublicContractShapeSnapshotGenerationTest.cs`, `ReleaseBaselineValidationTest.cs` — implement authorized successors/safe snapshot output with retained identities and before/after proof.
- [x] `_bmad/scripts/tests/test_conformance_tiering.py` — add `structural_and_execution_faults`: nonportable reference, assertion deletion/duplication/rename/weakening, missing tier/project/declaration, skipped/not-run/empty execution, regression, and exact restoration.
- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — bind tier hashes, predecessor, inventories/strengths, approvals, faults, and retention.
- [ ] `docs/runbooks/story-final-record-generation.md`, `docs/release-evidence/story-9.2-final-record-v2.{json,md}` — document/generate/verify record and insertion; update spec/sprint status after passing gates.

**Acceptance Criteria:**
- Given approved scenarios, when AC01–05 run, then builds/dependencies, exact declarations, and migration proof pass.
- Given tier results, when AC06–09 run, then complete passing execution preserves the floor, distinguishes additions, and detects/restores every fault.
- Given compatible passing evidence, when AC10 runs, then the derived record binds all required facts and reports `10/10/0/0/0/0`.

## Implementation Notes

- 2026-10-07: User approved both recommended approaches with "do recommended". Implement all reversible work and prepare concrete digest-bound approval evidence; do not invent the later Quality-owner decision. Preserve the original baseline commit.
- Structural implementation verified: both Debug/Release builds and AC02/05 pass; portable 326/326, internal 91/92 with only the approval control failing. All 415 retained cases pass; 21 fault fixtures and 11 source-model checks pass. Final acceptance remains pending.
- Resolve the remaining verifier concern: use resolved assembly identity, rather than only DLL filename, to reject a renamed nonportable reference. Keep the final-record integration specific to the hash-bound execution amendment; preserve historical parsing, schemas, and accepted records. Derive current facts through the verifier APIs and bind all measured fault receipts to the candidate. Genuine approval and committed-candidate final publication remain pending; prepare/test all tooling without fabricating either.
- Resolved assembly identity now uses MSBuild `FusionName`; the expanded 22-fault suite passes. Final-record integration and its 62 focused generator checks pass, including candidate/approval rejection, historical-pair compatibility, deterministic fixture derivation, and inserted-record drift. The publication/retention runbook is updated.
- Concrete decision packet: `docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md`. Proposal `f59dce5e7642c7ef588dcb0f3e4f7fe045598ee613c9638b3198a5e57f331eef` binds 14 changed rows and public drift digest `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1`. No Quality decision, final record, or completion status has been invented.

## Spec Change Log

- 2026-10-07: User requested "update to latest eventstore", authorizing the shared EventStore package update and its owning Builds changes despite the original dependency/submodule constraint. The original baseline and frozen intent remain preserved. This instruction supplies neither the Quality migration decision nor an amendment accepting the other candidate gitlink promotions.

## Review Triage Log

## Verification

Run effective Story 9.2 scenarios with receipts. Use individual Debug checks and separate Release acceptance. Rebuilds invalidate results. Verify determinism, insertion, frozen hashes, focused Python tooling, and `git diff --check`; unresolved questions/failures block completion.

Working-tree verification (2026-10-07): both Debug/Release builds pass with zero warnings/errors; AC02/05 pass; AC06 passes 326/326; AC07 reports 91/92 with only `TIER_APPROVAL_MISSING`; all 415 retained cases pass without skips or omissions. AC04/08 remain blocked by the actual Quality decision. AC09 passes 22/22; focused Story 9.1/9.2 generator tests pass 62/62; source-model checks pass 11/11; whitespace checks pass. Receipts are under `artifacts/v9/9.2/`. Final AC10 and lifecycle updates await approval and a fresh committed-candidate run.

Matrix audit: the four dependency fault variants cover nonportable references; deletion, duplication, rename, and weakening cover identity/strength failures; declaration removal, empty execution, skipped/not-run cases, and execution regression cover the final matrix row. Every covering fault executed, reported its exact blocker, and restored byte-identically in AC09.

Resumed verification (2026-10-07, current candidate `f962c5b1f81b995116c83f144c26707258319ba4`): the following current measurements supersede the earlier provisional build/execution status. Both individual Debug builds pass with zero warnings/errors; Debug AC02/05 pass. After separate Release restores, portable AC01/02 pass and AC06 passes 326/326 with no skips. The source verifier's `--declarations-only` check passes. AC09 passes 22/22 with exact blockers and byte-identical restoration; focused Story 9.1/9.2 generator checks pass 62/62 and source-model checks pass 11/11. These remain provisional checks, not final acceptance.

- Internal Release build: `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore` exits `1` with 12 `CS0234`/`CS0246` errors for `Hexalith.EventStore.Client.Streams`, `AuthoritativeEventStream`, `IAuthoritativeEventStreamReader`, and `AuthoritativeStreamReadResult`. The narrow serialized fallback, `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`, also exits `1` with the same 12 errors. Logs: [AC03](../../artifacts/v9/9.2/AC-9.2-03.log) and [fallback](../../artifacts/v9/9.2/internal-release-fallback-build.log). Stale internal Release binaries were not used to claim fresh AC04/05/07 execution.
- Dependency environment: the original baseline and retained migration bind EventStore Client/Contracts `3.113.0`; the current Builds pin restores `3.110.0`, whose packages lack the required authoritative stream types. `python3 _bmad/scripts/verify_conformance_tiering.py --repository . --structure-only --output artifacts/v9/9.2/current-structure-check.json` exits `1` with `ASSERTION_STRENGTH_WEAKENED` because the recorded migration no longer reproduces. The [read-only dependency audit](../../artifacts/v9/9.2/dependency-environment-audit-v1.json) measures only `portableSurface` drift: both before/after strength-inventory digests remain unchanged. The original proposal and its review bindings are preserved; no dependency or assertion was changed to obtain a pass.
- Candidate scope: the unchanged final-record API's `v2_9_2_facts` probe exits `1` with `AUTHORITY_BINDING_INVALID` for seven changed root gitlinks: Builds, EventStore, Folders, Memories, Parties, Projects, and Tenants. The [decision packet](../../artifacts/v9/9.2/candidate-scope-review-v1.md) retains the exact reproduction command; its [machine audit](../../artifacts/v9/9.2/candidate-scope-audit-v1.json) binds the full candidate, promotion commits, and before/after gitlinks. Those existing submodule updates and the original baseline are preserved. No candidate-environment amendment has been authorized or implemented.
- Quality decision: `conformance-oracle-tiering-migration-approval-v3.json` remains absent. The concrete proposal/public-drift/changed-row bindings in the [Quality packet](../../docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md) still require an actual Quality-owner decision. No approval, accepted final record, AC10 `10/10/0/0/0/0` result, or completion transition is claimed.

Fresh complete Release execution, compatible candidate scope, genuine Quality approval, deterministic final-pair generation/insertion verification, and lifecycle updates remain incomplete. The spec and sprint row stay `in-progress`.

EventStore update verification (2026-10-07, root `54c8f7fb5b3557e29af3a7e7239c97362094fd89`, local Builds `af20682ac8fc420068a731ecb87cff84727a3d53`): the latest listed stable version shared by all 13 EventStore packages is `3.115.0`. The authoritative catalog pin was updated in local Builds commit `031b028d3f7b5b614585d4e7e61e6a1350da4096`; its targeted audit and shared-consumer regression fix are in the subsequent local Builds commit. Catalog, consumer authority, exception, whitespace, and root-submodule checks pass. All 115 audit-generator scenarios pass; the deterministic audit validates 304 packages and preserves all 145 other family decisions and 291 other package rows. No push was performed.

- Both separate Release restores/builds pass with zero warnings/errors, resolving the missing stream API blocker. Fresh [portable](../../artifacts/v9/9.2/eventstore-update/portable.trx) execution passes 326/326; fresh [internal](../../artifacts/v9/9.2/eventstore-update/internal.trx) execution passes 91/92. The sole failing case is the Quality approval control. The [verifier](../../artifacts/v9/9.2/eventstore-update/tiering-verification.json) observes all 415 retained cases across 401 methods and all three live controls, with zero skips or omissions; it exits `1` with `TIER_APPROVAL_MISSING`.
- The pending migration now reproduces at proposal `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`. Only its two EventStore `portableSurface` package versions and proposal digest change; all 14 successor row bindings, strength inventories, and public drift remain identical. The previous pending proposal/review packet and [environment comparison](../../artifacts/v9/9.2/eventstore-update/migration-environment-audit.json) are retained. The [Quality packet](../../docs/release-evidence/conformance-oracle-tiering-migration-review-v3.md) is refreshed for the actual dependency environment, with no approval claimed.
- The current read-only [candidate probe](../../artifacts/v9/9.2/eventstore-update/candidate-scope-check.json) still exits `1` with `AUTHORITY_BINDING_INVALID` for the seven previously identified root gitlinks. Compatible candidate scope, genuine Quality approval, fresh committed-candidate acceptance, final-pair generation/insertion, and lifecycle updates remain pending. These measurements supersede the dependency build failure above; they do not establish complete Story 9.2 acceptance.
