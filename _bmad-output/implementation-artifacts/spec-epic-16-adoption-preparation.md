---
title: 'Prepare Conversations Platform enrollment and Epic 16 successor adoption'
type: 'feature'
created: '2026-10-23'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'bb6ae6e6574377e8fc628266f16b51a301f9736e'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/references/Hexalith.Platform/docs/domain-module-enrollment.md'
  - '{project-root}/_bmad-output/planning-artifacts/epic-16-prerequisites-v1/epic.md'
---

<frozen-after-approval reason="user selected the recommended prerequisite implementation on 2026-10-23">

## Intent

Prepare a concrete, validated Conversations Platform enrollment and executable successor adoption package for owner review. The user selected this prerequisite work; Story 16.1 runtime implementation and its lifecycle remain held.

## Boundaries & Constraints

Use Platform's accepted tool pin and logical components/injected configuration. Retain immutable draft/history bytes and all current module runtime behavior. New additive artifacts must preserve all three Epic 16 stories, eighteen scenario IDs, requirements, dependency graph, rollback and final-record targets. Distinguish structural consistency PASS from entry BLOCKED and preserve all false authority flags. Never claim missing tests, owner acceptance, generator support, hosted qualification or SM-2 succeeded.

No commits, staging, pushes, dependency/gitlink updates, deployment, source-mapping migration, runtime implementation, host retirement or runtime execution. Do not load skills in references. Keep existing hosts frozen and disclose their consumers and retirement gates. Ordinary manifest/tool validation and tool restore are authorized. Preserve the original 16.1 draft and sprint status.

## I/O & Edge-Case Matrix

| Input | Expected behavior |
| --- | --- |
| Correct review package | Nonempty structural PASS ledger; runtime entry BLOCKED |
| Changed/missing/duplicate scenario, requirement, graph edge or command | FAIL; no command execution or authority grant |
| Missing prerequisite input or accepted tool | BLOCKED with stable code and exact diagnostic |
| Path escape, symlink escape or duplicate JSON keys | Reject safely; no outside reads/writes |
| Recomputed declared hashes after target weakening | Independent source bindings reject drift |
| Adoption scanner failures or missing runtime proofs | Report actual failures/pending facts; never count them as PASS |

</frozen-after-approval>

## Code Map

- Platform `templates/domain-module-enrollment/` and Parties `module/hexalith.module-manifest.v2.json`: reuse declaration patterns; actual Conversations app ID is `conversation`, resource `conversations`, readiness `/ready`, Tenants route `/tenants/events`. Inspect SDK auth routes/config before declaring callers. State role has `authoritative-restore` component classification, projection inventory remains derived.
- Direct Platform `.config/dotnet-tools.json`: merge only `hexalith.builds.module.cli` 4.35.1 with rollForward false. Existing root has no tool manifest.
- `src/Hexalith.Conversations.AppHost/` and fixture AppHost in `tests/fixtures/Hexalith.Conversations.MinimalModule/`: inventory actual tracked hosting projects and consumers, no retirement.
- `_bmad/scripts/verify_epic_16_prerequisites.py`: immutable draft checks, independent pins and exact inventory/graph relations. Reuse rather than weakening. New package must be additive outside `epic-16-prerequisites-v1/`.
- `_bmad/scripts/generate_story_record.py`: safe JSON/contained path/Git helpers; generator currently supports V9/V14, not draft wrappers. Reuse neutral helpers for hashes/facts, never imply compatibility until genuinely supported. This preparation must expose the complete adoption blockers; it does not publish final records.

## Tasks & Acceptance

- [x] `module/hexalith.module-manifest.v2.json` — customize Platform declaration for Conversations with direct dependencies, logical state/pubsub, injected keys, protected surfaces, Tenants feed/dead-letter and derived recovery inventory. Validate with accepted tool; do not expand runtime wiring.
- [x] `module/hosting-inventory.json`, `module/sm-2-checklist.md`, `module/README.md` — identify exact hosts/consumers, keep freeze/retirement gates pending, document declaration and actual validation results, remaining source/package/hosted work and owner review procedure.
- [x] `.config/dotnet-tools.json` — exact accepted tool entry; restore and run version, ordinary manifest validation and text/JSON adoption reports. Preserve FAIL/BLOCKED facts; no tracked scanner-evasion changes.
- [x] `_bmad-output/planning-artifacts/epic-16-adoption-v1/` — prepare additive candidate carriers for all three stories, inventories/graph and package bindings, preserving inherited assertions. Include exact future commands, explicit generator/verifier capability status, qualification/entry decisions pending, and documented scoped review boundary. Do not fabricate active authority or executed runtime receipts.
- [x] `_bmad/scripts/verify_epic_16_adoption.py`, `_bmad/schemas/epic-16-adoption-v1.schema.json` — implement read-only validation of new enrollment/candidate carriers and exact preserved semantics against independent pinned draft sources. Use strict schemas/contained reads and recomputed digests, distinguish malformed vs unavailable inputs, produce nonempty ledger and explicit entry blockers. Never shell out to manifest commands; no arbitrary output writer. Preserve structural result versus entry state.
- [x] `_bmad/scripts/tests/test_verify_epic_16_adoption.py` — hermetic meaningful tests for every matrix row, including rehashed assertion weakening, truncated/duplicate inventories, path confinement and no runtime command execution; unchanged prerequisite tests must pass.

**Acceptance Criteria:**

- Given this checkout, when the review verifier runs, then it evaluates enrollment and exact successor preservation without mutating sources, reports structural consistency separately from actual entry blockers and grants no runtime authority.
- Given the accepted tool, when validation runs, then exact declaration/adoption results are retained with a reviewable migration plan; an accepted-tool/SDK contract incompatibility is an explicit upstream adoption blocker, and missing environment/tooling is reported accurately.
- Given completed preparation, when an owner reviews the package, then pending qualification, SM-2, generator/verifier adoption and entry approval are concrete and the 16.1 sprint/runtime state and historical bytes are unchanged.

## Implementation Notes

2026-10-23: prepared enrollment and candidate carriers; accepted CLI ordinary validation PASS, adoption FAIL with fourteen retained HXA001 findings. Both checkers PASS (75 prerequisite / 90 adoption assertions); entry remains BLOCKED. Independently verified exact 22-file additive scope plus the parent-authored original draft updates; no staging, runtime or dependency changes. Generator capability metadata corrected against its actual constants.

## Spec Change Log

2026-10-23: correcting the SDK app-channel token exposed a pre-existing accepted-tool incompatibility: APP_API_TOKEN is required by EventStore but rejected by the accepted schema configurationKey regex. Clarified the non-frozen native-validation acceptance to retain actual FAIL results and an explicit upstream blocker, avoiding a false colon-key alias or unauthorized dependency upgrade. KEEP: correct SDK keys, exact CLI outputs, all preserved scenario/graph assertions, secure reads and false authority flags. Runtime scope is unchanged.

## Review Triage Log


| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| Blind 1: token injection key | medium / patch | SDK DaprAppChannelToken.ConfigurationKey is APP_API_TOKEN; candidate uses an unconsumed key twice. Correct the declaration. |
| Blind 2: ancestor replacement race | high / patch | contained_file and read_file_snapshot perform separate pathname operations; reproduced ancestor swap opens external bytes. Use confined descriptor reads via a neutral helper. |
| Blind 3: unconstrained schema root | medium / patch | The new schema only defines $defs; standard root validation accepts authority-granting objects. Add a root union and direct tests. |
| Blind 4: exponent overflow | low / patch | parse(1e999) produces infinity despite the non-finite rejection claim; reject non-finite decoded floats. |
| Blind 5: workload issuer egress | medium / patch | Dedicated WorkloadJwtBearer authority is declared without metadata/JWKS egress. Add both candidate egress rows. |
| Blind 6: unused Tenants inputs | medium / patch | AddHexalithTenants accepts an options delegate, not configuration binding; remove unsupported knobs and name required consumer binding work. |
| Blind 7: Data Protection registration | medium / patch | Canonical host uses AddDataProtection, not AddEventStoreDataProtection; document the missing shared-key registration as an entry blocker. |
| Blind 8: declarative subscription switch | medium / patch | SDK checks Dapr:DeclarativeSubscriptions; name the required true binding and route qualification explicitly. |
| Blind 9: omitted host consumers | medium / patch | DomainModuleAuthoringTemplateValidationTest reads both host boundaries; package-refresh checker reads the main project. Add these; label V18 Git-blob references historical. |
| Blind 10: observation provenance | low / reject | Root revision binds every tracked scanner source and gitlink, prepared declaration/tool/output bytes are pinned, and the only dirty tracked edit is Markdown outside scanner extensions. These are disclosed working-tree observations, not reproducible committed runtime evidence. Additional provenance is optional. |
| Edge 1: input replacement race | high / patch | Same demonstrated race as Blind 2; the neutral helper checks containment before a separate pathname open. |
| Edge 2: directory input classified unavailable | low / patch | contained_file rejects non-file objects, but caller converts its GateError to BLOCKED. Classify an observed directory as invalid input/FAIL. |
| Edge 3: omitted authoring-template consumer | medium / patch | Active authoring-template test directly reads host project; same inventory root cause as Blind 9. |
| Verification 1: self-referential authority expectations | medium / patch | Temporary mutation to AUTHORITY.executionAllowed=true leaves all adoption tests green; use literal false assertions independent of implementation constants. |
| Verification 2: untested FAIL exit | medium / patch | Temporary FAIL exit=0 mutation leaves suite green; run the real CLI against an invalid source and assert JSON FAIL with exit 1. |
| Verification 3: new tests require submodules | medium / patch | Normal repository CI checks out submodules:false; copying SOURCE_PINS from live references raises FileNotFoundError. Make the new fixtures independent of populated reference directories without skipping assertions. |

## Verification

Run focused Python tests and both prerequisite/adoption checkers. Run accepted module CLI version, manifest validation and both adoption output modes; inspect exact output. Run `git diff --check` and verify only authorized root files changed. Record scoped evidence status as not-applicable to runtime publication: this change publishes no runtime receipts/final records.

Focused validation: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py _bmad/scripts/tests/test_verify_epic_16_adoption.py` — 139 passed. Matrix audit: positive read-only fixture covers correct package and observed failures; parameterized preservation, missing-input, JSON/path/symlink and rehash faults cover every remaining matrix row. Scoped runtime-publication evidence: not-applicable; no runtime acceptance or final record is claimed.

Review follow-up: the older prerequisite test suite independently reproduces a pre-existing submodule-free CI limitation (1 passed, 36 fixture errors). Recorded append-only in `deferred-work.md`; this workflow tracking entry is outside the additive implementation inventory, alongside the original draft updates. The new adoption suite must independently pass without initialized reference directories. No CI-wide PASS is claimed.

New upstream blocker verified independently: `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json` exited 1 with HXP002 at `modules[0].integration.secrets[1].configurationKey` and `modules[0].runtime.extensions[0].configurationInputs[12].key`. Accepted Builds schema configurationKey regex excludes underscores; EventStore DaprAppChannelToken.ConfigurationKey is APP_API_TOKEN. A correct candidate therefore requires an owner-approved tool/schema successor or an approved supported transport binding before adoption. Earlier ordinary-manifest PASS observations describe the superseded candidate, not the corrected one.

Final review resolution (2026-10-23): all retained patch findings were corrected. APP_API_TOKEN and dedicated workload egress now match source; unsupported Tenants knobs were removed, and exact Tenants delegate/Data Protection/declarative-subscription blockers are documented. Both host inventories include the missing active consumers and distinguish historical Git-blob references. A neutral descriptor-relative, no-follow, bounded reader closes ancestor/leaf replacement races; the unchanged prerequisite checker consumes captured snapshots through an isolated adapter. Root schema validation, exponent overflow, nonregular/unreadable inputs, literal authority expectations and CLI FAIL exits have direct regression coverage. Blind 10 remains rejected as logged. Authority-true and FAIL-exit-zero mutations were killed. New native schema incompatibility and older CI fixture limitation are recorded separately in deferred work; no runtime adoption is claimed.

Final verification: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py _bmad/scripts/tests/test_verify_epic_16_adoption.py` — 168 passed. Prerequisite checker PASS/exit 0/75 assertions; adoption checker PASS/exit 0/98 assertions. Both retain entry BLOCKED and four false authority flags. The new adoption suite independently passed 131 tests in `/tmp/epic16-portable-checkout-6tazkh4g` with references absent. Corrected native ordinary/adoption checks exit 1; adoption has 6 PASS and 15 FAIL rows, including the declaration/token-key incompatibility and fourteen source findings. Earlier ordinary PASS and 139-test notes describe superseded preparation revisions.

Completion applies only to this approved review-package preparation. Story 16.1 remains draft/backlog; its runtime final-record gate is not invoked. Scoped runtime-publication evidence is explicitly not-applicable. No commit, staging, remote, runtime or dependency operations are part of this completion.
