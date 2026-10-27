---
title: 'Correct active Conversations enrollment consumers and retain a truthful qualification handoff'
type: 'refactor'
created: '2026-10-27'
status: 'done'
baseline_commit: 'dc5793db17fd6974200e80eb2b853187bc5f1669'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-enrollment-adoption-remediation-plan.md'
---

<frozen-after-approval reason="separate prerequisite scope; runtime entry remains held">

## Intent

**Problem:** Active build/configuration consumers violate Platform enrollment conventions. Accepted fixtures, frozen hosts and unqualified tooling also prevent adoption; automated results cannot supply owner acceptance.

**Approach:** Correct the fourteen eligible source findings and prepare a pending owner handoff. Preserve all ten outside-slice findings explicitly. The user selected this separate plan and then approved its implementation on 2026-10-27.

## Boundaries & Constraints

**Always:** Consume tool-selected roots/mode and declared configuration. Reuse Builds mappings and SDK persistence/hosting. Preserve public Contracts, tenant key bytes, CAS/recovery behavior, cursor purpose and workload authorization. Distinguish source tests from live qualification and FAIL from BLOCKED.

**Never:** Change accepted CLI pins/gitlinks, frozen host projects, accepted MinimalModule sources, retained captures or historical records. No scanner bypass, archive migration, dependency update, staging/commit/push, deployment, runtime launch, durable tenant-projection implementation or 16.1 lifecycle/hold lift. Entry stays null and trust empty. The user approved an explicit pre-edit AppHost exception: use mapped Debug builds and isolated composition tests, without a runtime launch. This exception applies only to this prerequisite slice and does not qualify live behavior.

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

## Code Map

[Detailed plan](epic-16-enrollment-adoption-remediation-plan.md) lists all 24 exact findings, consumer paths, protected files and verification steps. Builds supplies mapping materialization; validation alone does not resolve it. The current scanner includes archived sources and has no general archive exemption.

## Tasks & Acceptance

**Execution:**

- [x] `Directory.Build.props`, `Directory.Packages.props` — remove root/mode assignments and fallback imports; consume mapped roots with only the permitted Builds bootstrap.
- [x] Eight eligible project files and `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`, enumerated in the plan — consume mapped project/package pairs; keep frozen host/fixture files unchanged.
- [x] New `tests/Hexalith.Conversations.Contracts.Tests/PlatformEnrollmentMappingTest.cs` — exercise evaluated references using Builds' materializer, source/Debug and package-mode evaluation, missing mappings and competing siblings.
- [x] `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` and its seven production consumers in the plan — thread the declared read-model store binding through existing SDK calls, batch scopes and native proof registration. Preserve algorithms and public call compatibility.
- [x] Server `Program.cs`, `Queries/ConversationQueryServiceCollectionExtensions.cs`, `TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs` — bind enrolled Tenants options, SDK Data Protection and declarative subscriptions. Retain restart/single-replica disclosure.
- [x] The two flagged `IntegrationTests/Projections/` files, existing projection/tenant registration tests and new `Server.Tests/Projections/ConversationProjectionConfigurationTest.cs` — verify the matrix with non-default names and persisted test-store contents.
- [x] New `module/platform-handoff.md`, `module/README.md` and fresh validation observations — record every unresolved tool, frozen-source, hosting and human qualification gate. Leave acceptance pending.

**Acceptance Criteria:**

- Given the retained 24-finding inventory, when both native adoption formats run on the intended tracked candidate, then none of the fourteen eligible identities remains failing; all ten excluded identities and unexpected findings stay visible. Adoption may remain FAIL.
- Given configured composition, when exercising storage and delivery registration, then all consumers agree on the injected binding without changing key bytes, domain behavior or other-provider state.
- Given the completed source slice, when six Story 16.1 commands run, then each remains BLOCKED with no runtime receipt/final pair, and the handoff claims no owner qualification or runtime entry.

## Implementation Notes

2026-10-27: user approved the concrete plan and scoped offline baseline exception. The runtime hold, ten excluded findings, frozen artifacts and no-Git-mutation boundaries remain in force.

## Spec Change Log

## Review Triage Log

### Independent review 1 — individual triage

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | The target accepts three stand-in flags while producer import properties are empty; require the actual generated validation imports and test the demonstrated partial environment. |
| B2 | medium | rejected: outside captured source-slice intent | Unmapped current CI really fails. The frozen approach explicitly delivers fourteen eligible source corrections plus a pending owner handoff, and its matrix requires absent mappings to fail. Producer integration is an explicit BLOCKED handoff gate; no CI readiness is claimed. Do not weaken the matrix or claim that integration is complete. |
| B3 | low | patch | The test creates competitors below the active workspace, whereas the removed fallback searched its parent. Correct the temporary fixture to contain an actual adjacent sibling without cross-test collisions. |
| B4 | medium | patch | Temporary MSBuild workspaces omit the pinned SDK policy, allowing a different installed SDK to win. Copy global.json and verify the selected SDK matches the repository policy. |
| B5 | medium | patch | The tracked-source snapshot intentionally omits the three new C# files and command records only name HEAD. Record a separate complete implementation-source digest snapshot, preserving native scanner scope and the no-staging boundary. |
| B6 | medium | patch | No existing attributes pattern protects this new observation directory. Autocrlf conversion can change raw streams and XML after their hashes were recorded; add a scoped byte-preservation rule. |
| B7 | false | rejected | Both executed non-default-component Integration population tests call production list queries and assert Current plus the exact conversation. QueryAsync reaches ListAsync and ValidatePageAsync, so the claimed absence of configured list/page coverage is disproved; additional conflicting-store assertions are optional beyond this claim. |
| B8 | medium | patch | The interruption test checks recovery completion but does not assert pre-recovery denial or protect populated alternate-component pending state. Add those bounded assertions to the existing configured recovery case. |
| B9 | medium | patch | Configured handler tests bypass the separately changed public PersistAsync/MarkPendingAsync invalidation paths and terminal reconciliation. Add explicit configured-component cases with alternate-component preservation. |
| B10 | medium | patch | Current Data Protection coverage inspects registration objects without exercising the configured key-ring/codec consumer. Add a bounded SDK-backed fake-Dapr operation test; do not claim live restart qualification. |
| B11 | low | patch | Current module guidance still describes the implemented Tenants delegate and source bindings as future work. Correct current-state guidance while preserving dated native observations. |
| E1 | medium | defer: pre-existing | Baseline ContractPackageInventoryTest already used unbounded Process.WaitForExit with no child termination. The Release branch retains that limitation; cancellation can leave packing blocked. This change did not introduce it. |
| V1 | medium | patch | Pre-verified gap: existing summaryless pending-sibling rebuild tests use only the default component, and configured tests contain no such sibling. Exercise a non-default binding, execute the rebuild batch and assert the persisted sibling dispatch reference survives. |

## Verification

2026-10-27 parent acceptance: all seven source-slice tasks are complete. Independently rehashed 28,496 protected files, compared twelve raw gitlinks/checkouts and 98 raw command streams, parsed all seven passing native reports and audited every matrix row. Full Server: 1,369 PASS; Contracts mapping/inventory: eight PASS; in-process population: two PASS. Fourteen targeted native identities cleared; exactly ten excluded failures remain. Both governance guards pass and six actual 16.1 commands remain BLOCKED. See [parent audit](../../module/validation/epic-16-enrollment-remediation-2026-10-27/parent-acceptance-audit.json).

The existing unmapped CI invocation fails the approved gate; producer mapping integration remains BLOCKED and is explicit in the handoff. This source-slice acceptance claims no CI, tracked-candidate, owner or live-runtime qualification.

Follow the plan's command sequence: mapped individual Debug builds and focused native xUnit classes, actual MSBuild package-reference evaluation, fresh ordinary/adoption reports in both formats, both Story 7.3 guards and six held Story 16.1 commands. Compare protected bytes, raw gitlinks, accepted pins and whitespace. No historical broad-lane rerun; runtime publication is not-applicable to this slice.


### Final source-slice verification — 2026-10-27

Review 1 is closed: B1, B3–B6, B8–B11 and V1 were corrected and verified; B7 was refuted by the executed configured list/page integration paths. B2 remains the deliberately pending producer-CI integration gate in the frozen source-only intent. E1 is one pre-existing package-process cancellation issue appended to the deferred-work ledger. No production state or recovery algorithm changed during the corrections.

Parent final verification built Contracts, Server and Integration projects individually in mapped Debug. All 1,387 distinct native cases passed with zero failures, skips or not-run cases: 10 mapping, 3 Contracts package inventory, 1,372 full Server, and 2 in-process population/replay. Eight actual package-mode evaluations passed without Release compilation. The complete 802-file source/policy snapshot matches; 28,496 protected files, twelve gitlinks/checkouts and the real index remain unchanged.

The first root-level observation-attributes rule caused a truthful SOURCE_DRIFT failure, retained in the earlier parent observation. Moving that rule to the observation directory's own `.gitattributes` restored exact accepted root bytes without changing any validator, binding or C# source. The final six actual Story 16.1 commands return BLOCKED/exit 2/ENTRY_QUALIFICATION_PENDING; both exact Story 7.3 guards pass. Native adoption remains FAIL/exit 1 with six PASS and exactly ten excluded FAIL rows; ordinary validation retains the accepted-tool token-key failure. All earlier failed attempts remain separate.

[Final parent verification](../../module/validation/epic-16-enrollment-remediation-2026-10-27/parent-final-entry/summary.json) binds the current source snapshot and the passing code-test reports. Runtime publication and a story-final-record transition are not-applicable to this separately approved prerequisite scope: it has no dotted story contract. This does not opt Story 16.1 out of its gate; its draft/backlog, null entry, empty trust and ACTIVE hold remain unchanged. Producer CI mapping and tracked-candidate/owner/live qualification remain pending. No staging, commit, push, dependency update or runtime launch was authorized or performed.
