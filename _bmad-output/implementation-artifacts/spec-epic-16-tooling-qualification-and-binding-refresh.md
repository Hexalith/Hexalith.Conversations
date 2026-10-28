---
title: 'Qualify producer preparation and refresh held Epic 16 tooling bindings'
type: 'feature'
created: '2026-10-27'
status: 'in-progress'
baseline_commit: 'c71e6ba924f42e7f5da62116f9e3b4fd5d77227e'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-tooling-qualification-and-binding-refresh-plan.md'
---

<frozen-after-approval reason="separate prerequisite proposal; user selected planning only">

## Intent

**Problem:** Accepted CLI 4.35.2 cannot execute the implemented CI preparation command. The existing Epic 16 source binding rejects a legitimate committed attributes change before reaching the runtime-entry gate.

**Approach:** Measure the existing producer's bounded preparation capability, retain every unqualified release/owner gate, and introduce an explicitly selected v2 tooling package using shared consumers. This delivers a truthful prerequisite handoff and a working held route; it does not qualify runtime execution.

**Decision:** On 2026-10-27 the user selected preparation of this separate plan. Implementation is not yet approved. The earlier remediation's offline baseline exception does not automatically extend here.

**Baseline decision:** The user selected the normal AppHost baseline. Run and inspect the existing Conversations AppHost before code edits; an unsuccessful baseline blocks those edits. No offline exception is granted. This local baseline grants no runtime-story entry.

## Boundaries & Constraints

**Always:** Preserve accepted v1 carriers, schema, trust pins and evidence. Keep all 18 ACs, 32 requirements, 38 graph nodes, 61 edges, rollback and native measurement obligations. Pass immutable package policy explicitly. Preserve FAIL/BLOCKED distinctions and nonempty ledgers. Retain null entry, empty independent entry trust, ACTIVE hold and false authority flags.

**Never:** Silently repin v1, remove the attributes rule, fall back after v2 failure, nominate owner trust, change tool pins/gitlinks, publish, initialize nested dependencies, retire hosts, alter accepted fixtures, or implement runtime 16.1–16.3. Local compilation remains Debug. Published-package, platform-specific, operational and human acceptance remain separate measured gates.

## I/O & Edge-Case Matrix

| Input | Required behavior |
| --- | --- |
| Existing producer source tests | Report only their executed source scope |
| Missing published package, tag or platform evidence | Retain BLOCKED qualification; no acceptance inference |
| Existing v1 commands | Retain FAIL/SOURCE_DRIFT on `.gitattributes` |
| Explicit v2 contract with null entry | BLOCKED/ENTRY_QUALIFICATION_PENDING; no runtime output |
| Altered pin, inventory, selector or executable | Fail closed; no cross-version fallback |
| Synthetic fixture success | `runtimeQualified=false`; isolated fixture outputs only |

</frozen-after-approval>

## Code Map

[Detailed plan](epic-16-tooling-qualification-and-binding-refresh-plan.md) freezes the inspected revisions, review inventory, exact commands, proposed footprint and publication gates. Builds already owns `BuildPreparationService`, its CLI and action; reuse them. Existing consumers use global package paths, so adding a binding alone cannot select v2.

## Tasks & Acceptance

**Execution after approval:**

- [ ] `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` — run the existing mapped Debug topology, inspect resource readiness and retain the baseline before changing code; use the plan's exact CLI target and preserve other sessions.
- [ ] `module/validation/epic-16-tooling-qualification-v2/` — retain fresh bounded producer tests, source mapping, command streams, identities and complete qualification ledger. Keep unavailable artifact/platform/owner gates explicit; upstream corrections require their own scope.
- [ ] Planning `epic-16-tooling-v2/` — add README, three contracts and binding; preserve exact predecessor obligations and independent source inventory. Generate the binding last, excluding its own digest.
- [ ] `_bmad/schemas/epic-16-tooling-v2.schema.json`, `_bmad/scripts/epic16_trust_v2.py` — add closed successor schema and independent contract/schema/predecessor pins; preserve v1 files.
- [ ] `_bmad/scripts/epic16_packages.py`, `epic16_tooling.py` — introduce immutable explicit package selection across validation, source/Git binding, measurement and generation; retain shared neutral infrastructure and executable-closure checks.
- [ ] `_bmad/scripts/generate_story_record.py`, `verify_story_16_{1,2,3}.py` — allowlist exact v2 dispatch and declared imports while preserving standalone legacy routes and error classification.
- [ ] `_bmad/scripts/tests/test_epic16_tooling_v2.py` and existing affected suites — exercise the matrix, all fifteen native fixtures, three deterministic final records, independent fault guards and version isolation.
- [ ] `module/README.md`, `module/platform-handoff.md`, runtime draft — publish measured dispositions and pending release/source/catalog/owner requirements without lifecycle advancement.

**Acceptance Criteria:**

- Given exact producer inputs, when focused tests and source preparation execute, then their actual results and limits are retained, and no source or synthetic package result becomes published/Platform acceptance.
- Given preserved v1 and explicit v2 contracts, when all six Story 16.1 commands run for each, then v1 retains its measured FAIL and v2 reaches entry BLOCKED, with zero measured runtime tests and all seven runtime output paths absent.
- Given mutated selected-package inputs, when each named fault runs, then its guard rejects with a nonempty ledger; unchanged fixtures retain all native/final-record obligations and legacy dispatch behavior.

## Implementation Notes

2026-10-27: The user approved this prerequisite spec and chose to stop before execution. Resume with the required AppHost baseline. No implementation task has started; Story 16.1 remains held.

2026-10-27 execution attempt: [Baseline evidence](../../module/validation/epic-16-tooling-qualification-v2/attempt-20261027-apphost-baseline-01/README.md) records the isolated AppHost launch exiting 2 with seven missing-source-mapping build errors. Source preparation was BLOCKED because Builds and Platform checkouts differ from the recorded root gitlinks. No instance started, and no tooling or runtime code was changed. Required implementation/tests remain incomplete; the normal-baseline gate stops further edits. Stream/log hashes and preservation checks passed. Story 16.1 remains draft/backlog and held.

## Spec Change Log

## Review Triage Log

## Design Notes

Shared executable changes are explicitly proposed; accepted v1 data/trust/evidence bytes remain fixed. V1 therefore continues to expose drift. Finalize contracts/schema, then independent pins, then executable sources, then the v2 binding to avoid digest cycles. Reuse existing final-record destinations and native rules; package v2 does not redefine their wire contract.

## Verification

First capture the AppHost run and resource-state baseline. Then run the plan's bounded individual Debug/native and action commands, focused v2 plus affected v1/legacy Python suites, both Story 7.3 guards, and twelve actual held commands. Compare exact obligations, protected bytes, raw gitlinks/checkouts, output absence and whitespace. This slice cannot complete on failed required implementation tests; qualification exclusions remain separately BLOCKED rather than silently waived. Current historical boundary replay is not required.
