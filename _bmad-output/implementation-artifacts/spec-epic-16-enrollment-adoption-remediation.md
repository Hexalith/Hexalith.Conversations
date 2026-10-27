---
title: 'Correct active Conversations enrollment consumers and retain a truthful qualification handoff'
type: 'refactor'
created: '2026-10-27'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-enrollment-adoption-remediation-plan.md'
---

<frozen-after-approval reason="separate prerequisite scope; runtime entry remains held">

## Intent

**Problem:** Active build/configuration consumers violate Platform enrollment conventions. Accepted fixtures, frozen hosts and unqualified tooling also prevent adoption; automated results cannot supply owner acceptance.

**Approach:** Correct the fourteen eligible source findings and prepare a pending owner handoff. Preserve all ten outside-slice findings explicitly. The user selected preparation of this separate plan on 2026-10-27; implementation is not yet approved.

## Boundaries & Constraints

**Always:** Consume tool-selected roots/mode and declared configuration. Reuse Builds mappings and SDK persistence/hosting. Preserve public Contracts, tenant key bytes, CAS/recovery behavior, cursor purpose and workload authorization. Distinguish source tests from live qualification and FAIL from BLOCKED.

**Never:** Change accepted CLI pins/gitlinks, frozen host projects, accepted MinimalModule sources, retained captures or historical records. No scanner bypass, archive migration, dependency update, staging/commit/push, deployment, runtime launch, durable tenant-projection implementation or 16.1 lifecycle/hold lift. Entry stays null and trust empty. Resolve the pre-edit AppHost baseline before code changes.

## I/O & Edge-Case Matrix

| Input | Required result |
| --- | --- |
| Tool mapping present | Exact mapped dependencies; no sibling discovery |
| Missing/mismatched dependency mapping | Explicit failure; no package fallback |
| Non-default enrolled component names | Same injected binding across reads, writes, batches and proofs |
| Missing/blank enrolled configuration | Fail before storage/delivery use |
| Two service providers | Independent bindings; no global mutation |
| Outside-slice finding or absent owner proof | Retain native failure/pending gate; no qualification claim |

</frozen-after-approval>

## Open Questions

1. Pre-edit baseline: approve an explicit exception using mapped Debug builds and isolated composition tests without an AppHost launch, or retain the pre-edit launch requirement until a separately authorized baseline run is available? The Hexalith baseline requires an AppHost run before code changes; runtime launch is currently held.

## Code Map

[Detailed plan](epic-16-enrollment-adoption-remediation-plan.md) lists all 24 exact findings, consumer paths, protected files and verification steps. Builds supplies mapping materialization; validation alone does not resolve it. The current scanner includes archived sources and has no general archive exemption.

## Tasks & Acceptance

**Execution:**

- [ ] `Directory.Build.props`, `Directory.Packages.props` — remove root/mode assignments and fallback imports; consume mapped roots with only the permitted Builds bootstrap.
- [ ] Eight eligible project files and `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`, enumerated in the plan — consume mapped project/package pairs; keep frozen host/fixture files unchanged.
- [ ] New `tests/Hexalith.Conversations.Contracts.Tests/PlatformEnrollmentMappingTest.cs` — exercise evaluated references using Builds' materializer, source/Debug and package-mode evaluation, missing mappings and competing siblings.
- [ ] `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` and its seven production consumers in the plan — thread the declared read-model store binding through existing SDK calls, batch scopes and native proof registration. Preserve algorithms and public call compatibility.
- [ ] Server `Program.cs`, `Queries/ConversationQueryServiceCollectionExtensions.cs`, `TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs` — bind enrolled Tenants options, SDK Data Protection and declarative subscriptions. Retain restart/single-replica disclosure.
- [ ] The two flagged `IntegrationTests/Projections/` files, existing projection/tenant registration tests and new `Server.Tests/Projections/ConversationProjectionConfigurationTest.cs` — verify the matrix with non-default names and persisted test-store contents.
- [ ] New `module/platform-handoff.md`, `module/README.md` and fresh validation observations — record every unresolved tool, frozen-source, hosting and human qualification gate. Leave acceptance pending.

**Acceptance Criteria:**

- Given the retained 24-finding inventory, when both native adoption formats run on the intended tracked candidate, then none of the fourteen eligible identities remains failing; all ten excluded identities and unexpected findings stay visible. Adoption may remain FAIL.
- Given configured composition, when exercising storage and delivery registration, then all consumers agree on the injected binding without changing key bytes, domain behavior or other-provider state.
- Given the completed source slice, when six Story 16.1 commands run, then each remains BLOCKED with no runtime receipt/final pair, and the handoff claims no owner qualification or runtime entry.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Follow the plan's command sequence: mapped individual Debug builds and focused native xUnit classes, actual MSBuild package-reference evaluation, fresh ordinary/adoption reports in both formats, both Story 7.3 guards and six held Story 16.1 commands. Compare protected bytes, raw gitlinks, accepted pins and whitespace. No historical broad-lane rerun; runtime publication is not-applicable to this slice.
