# Sprint change proposal · 20 November 2026

**Hexalith.Conversations · Applied to the sprint backlog · Moderate scope**

## Issue summary

The 20 November audit of main at `1bc8a18f25a7aa511246bbfbcc7d4dc03d90b143` found three risks that affect whether a consumer can use the module as documented. The minimal-module package check fails before compilation. The published client's ordinary conversation routes lack a demonstrated production host. A restarted service can report a populated tenant as Current and empty after index loss and event redelivery. The Release solution build and much of the domain and conformance coverage passed, making these specific package, integration and recovery boundaries the first places to spend engineering time. [Technical audit](https://chatgpt.com/space/page_a9c298c24f6c81918322141fa7947213) · [Failing CI job](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/44724234817/job/136247463908)

The trigger spans the package fixture in Story 11.2, the conformance tiers in 9.2 and the projection lifecycle in 16.2. Seven new backlog stories and two additions to existing acceptance criteria cover all ten findings. Use the current sprint supplement for the full story text. [Canonical epics and acceptance criteria](prds/prd-Conversations-2026-06-02/epics.md#current-sprint-supplement--20-november-2026) · [Sprint tracker](../implementation-artifacts/sprint-status.yaml)

## Impact on the product and plan

| Area | Current commitment | Applied change or dependent decision |
| --- | --- | --- |
| PRD §§10, 14 | Preserve the developer-facing contracts and user journeys while reducing module-specific plumbing. The historical §10 runtime example says net10.0. | Keep the MVP and documented user workflows. Story 11.4 aligns current onboarding examples with the active SDK and mapped restore. Story 9.3 identifies the supported consumer entry point. |
| Architecture, Core Architectural Decisions and Project Structure & Boundaries | EventStore owns generic hosting. Conversations owns its domain API and read semantics. ADR 0005 governs derived state and rollback. | The runtime owner documents the packaged-client-to-host route in 9.3. The projection owner documents the tenant replay cutoff, write conflict handling, completion and rollback in 16.4. Update the as-built deployment or data-flow diagram once these owners verify it. |
| UX, User Journey Flows and Responsive Design & Accessibility | A user can distinguish a trustworthy empty list from a degraded one. Reviewed responsive and accessibility evidence supports the existing experience. | Stories 9.4 and 16.4 test hidden and unavailable states. Story 8.3 changes where test evidence goes. Current screens, breakpoints and accessibility decisions cover the required behavior. |
| Release, CI and operations | Consumers need a reproducible package graph. Operators need to see read-model freshness. | Story 11.4 qualifies packages and commands. Story 16.5 exercises production meter registration and export. For shared workflow or tool changes, coordinate with CiCd or Builds owners. |
| Other epics | Epics 13–15 consume proof and release inputs. Epic 10 already owns evidence conventions. | Their scope and order stand. Use current, verified evidence from these stories when a later claim depends on it. Historical results and retrospectives retain their original scope. |

Use the current-change runbook for routine development. The sprint supplement sits after the archived V14 block. Existing story-specific acceptance and recorded waivers retain their original scope. [Current validation policy](../../docs/runbooks/current-change-validation.md) · [Derived-key lifecycle ADR](../../docs/adrs/0005-conversation-derived-key-lifecycle.md)

## Approach and sequence

**Direct adjustment** fits the evidence. The failing guard, unqualified HTTP boundary and unsafe index initialization have identifiable owners and regression tests. A rollback would reopen already-qualified shared hosting and would also restore the older empty-on-missing list behavior. Reducing the MVP would require consumers to abandon documented commands or trust weaker read semantics. Neither alternative resolves the package mismatch.

Estimates are engineering person-days, before external-owner response or CI queue time. Pick up the release fix first. The HTTP owner investigation and the projection containment test can start alongside it.

| Order | Work and owner | Estimate | Dependency or trade-off |
| --- | --- | --- | --- |
| First | 11.4 package qualification and maintainer commands · Release and Builds | 1–3 days | The fixture drift is reproduced. A clean package build supplies artifacts for 9.3 and 9.5. Preserve the historical measurement and its original locks. |
| Start immediately | 9.3 supported host · Conversations API and platform gateway owners | 2–5 days if an adapter exists | Identify the versioned owner before committing to a new adapter. Estimate a missing adapter after the ownership check. |
| Start immediately | 16.4 truthful tenant recovery · Projection and EventStore owners | 1–2 days for containment and regression coverage, then 3–7 days if the replay source is available | A temporary unavailable list preserves correctness. Recovery needs authoritative enumeration and an atomic completion decision. |
| Quick, independent | 9.4 typed failures and scopes, 8.3 isolated browser evidence · Client, API and UI test maintainers | 1–2 days each | These have local reproductions and can run alongside infrastructure work. |
| Near term | 9.5 discovery and 16.5 freshness telemetry · Package, service and observability owners | 1–2 days each | Use the qualified packages and supported host for final checks. Bound metric attributes before wiring export. |
| After the correctness baseline | Existing 12.3 and 12.4 list/open work · Query and performance owners | 2–4 days for baseline and verification, another 2–5 if a write-path change is justified | Begin measurement early. Settle recovery semantics before changing stored generation. Account for added write cost and legacy-reader compatibility. |

## Detailed backlog changes

Finding IDs refer to the audit. Each row records the previous evidence, the new acceptance boundary and the practical reason for the change. The current sprint supplement contains the executable test scenarios.

| Finding | Previous behavior or evidence | New sprint acceptance |
| --- | --- | --- |
| F1 · Release qualification | `tests/compatibility/MinimalModule/RegisteredCommand.targets:8` selects EventStore 5.3.1 while `references/Hexalith.Builds/Props/Directory.Packages.props:181` selects 5.4.0. The local guard and CI fail identically. | **11.4** qualifies the overlay and four successor locks in independent caches. All seven fixture assertions and five package consumer checks pass. Verify the 17 original fixture files against Git. |
| F2 · Packaged workflow | `src/Hexalith.Conversations.Server/Program.cs:69` maps the optional deletion host. Ordinary create/append are unavailable in `src/Hexalith.Conversations.Server/Api/ConversationAgentParticipantApiHandler.cs:100`. An external host for the client's routes is unverified. | **9.3** names a supported service address and proves packaged create, retry, append, read and identity isolation against the real composition. |
| F3 · Tenant recovery | `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs:313` initializes Ready on absence. `module/platform-handoff.md:575` and `:586` describe the legacy and restart consequences. | **16.4** covers the lost-index and redelivery sequence, interrupted recovery and competing writes. Only proven tenant-wide coverage permits Ready for existing data. |
| F4 · Typed HTTP errors | The code at `src/Hexalith.Conversations.Client/ConversationClient.cs:239` tries successive decoders. The forward-only HTTP 503 probe returned `idempotency_outcome_unknown` for `participant_validation_unavailable`. | **9.4** retains the exact structured error with one body read and exercises the fallback with malformed and empty responses. |
| F5 · Read scope | Detail checks conflicting optional headers at `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs:73`. List goes directly from claims to query construction at line 239. The audit established inconsistent behavior. | **9.4** tests omitted, matching, mismatched and multiple header values for list and detail. On mismatch, require the same hidden result and zero store calls. |
| F6 · Discovery | `src/Hexalith.Conversations.Contracts/Versioning/ContractCompatibilityMetadata.cs:368` advertises 1.0.0 and line 448 requires equality. The audited 2.1.0/wire-v1 probe returned unsupported. | **9.5** binds documented compatibility ranges to independently installed packages and tests supported, deprecated and unsupported outcomes. |
| F7 · List cost | `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:335` and line 556 process the full index for generation. Warm allocations for one generation were 107,224, 1,064,824 and 10,640,824 bytes at 100, 1,000 and 10,000 rows. Production latency is unknown. | **12.3** adds actual list/open size and paging measurements plus migration safeguards. **12.4** applies the existing universal four-row P95 rule in a matched environment. |
| F8 · Freshness metrics | The production registration was absent in the audit. The existing `AddConversationProjectionTelemetry` extension is in `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetryServiceCollectionExtensions.cs:20`. | **16.5** observes bounded measurements through the supported host and exporter for healthy, degraded and failed-publication paths. |
| F9 · Maintainer commands | `package.json:10` selects five missing test modules and its release-plan script points at a missing file. The pinned tool failed with HXW009. An earlier discovery run passed 14/14. With the current direct catalog, 12/14 pass and two fixture checks reproduce F1. | **11.4** validates current root commands and a clean, mapped SDK setup with the chosen pin. Record who owns any retired release check. |
| F10 · Browser evidence | Default directories in `tests/Hexalith.Conversations.Admin.Web.Tests/Responsive/ResponsiveEvidenceHarnessTest.cs:47` and `tests/Hexalith.Conversations.Admin.Web.Tests/Accessibility/AccessibilityEvidenceHarnessTest.cs:48` point into tracked evidence. All 14 tests passed and changed 39 tracked files. | **8.3** requires two concurrent, clean default runs with distinct outputs and an explicit reviewed operation for baseline updates. |

## Implementation handoff and open decisions

The Product Owner maintains the order above. The Release owner takes 11.4 first and attaches fixture results and clean consumer results. The API and platform owners identify the deployable route for 9.3. The projection and EventStore owners confirm the tenant history source and resume semantics in 16.4. The pinned SDK provides `IAsyncDomainSharedProjectionRebuildHandler`; the interface alone cannot establish that the deployed provider supplies complete history. QA verifies each story at its stated boundary. Production volumes, representative list sizes and the actual exporter are inputs from Operations.

Existing retrospective items 50, 54 and 55 cover broader package-mode CI, handoff corrections and live-test hardening respectively. Story evidence can support them individually. An Aspire DCP timeout and one dispatcher test that passed on isolated retry remain diagnostic follow-up. Reproduce them on a suitable runner before attributing either to product failure.

Completion means the release checks pass for the candidate, a packaged client completes its documented journey, and existing tenants cannot appear Current and empty solely because an index disappeared. Close each remaining audit finding against its own story evidence. This change records planning and ownership. Product implementation, package publication and release decisions are separate steps.

### Verification of this backlog change

- A duplicate-key-aware YAML and Markdown cross-check compared the tracker with Git HEAD. All 89 existing status entries and 60 action items match. The seven new keys match the seven story headings and all are in backlog. Both existing performance stories have supplemental acceptance. Each of F1–F10 resolves to at least one story. The historical canonical epic document is a byte-for-byte prefix of the updated file.
- `git diff --check` and `python3 scripts/check-root-submodules.py --repository .` passed. The proposal's local links, sprint anchor and source file locations resolved.
- `python3 -m unittest discover -s tests/tooling -v` ran with the installed .NET 11 SDK and current producer mapping. Twelve of fourteen tests passed. Both the overlay/lock selection test and the framework-discovery test stop in `tests/compatibility/MinimalModule/RegisteredCommand.targets:28` because 5.3.1 differs from 5.4.0. Direct MSBuild diagnostics confirmed the same cause in each. The earlier 14/14 result in the audit does not qualify the current catalog. A shell without the SDK on PATH and the environment's older source mapping also failed during setup.

### Workflow record

| Checklist | Disposition |
| --- | --- |
| 1.1–1.3 · Trigger and evidence | [x] Audit baseline, triggering stories and observed failures identified. |
| 2.1–2.5 · Epic impact | [x] Reused five existing epics. Seven new stories and two existing stories have explicit test boundaries. |
| 3.1–3.4 · Artifact impact | [x] Reviewed product, architecture, UX, CI, operational and evidence implications. |
| 4.1–4.4 · Options | [x] Compared direct adjustment, rollback and MVP reduction. Selected direct adjustment with effort and dependency estimates. |
| 5.1–5.5 · Proposal and ownership | [x] Recorded the issue, artifact deltas, ordering, role assignments and success criteria. |
| 6.1–6.2 · Review | [x] Reviewed against all ten findings and the canonical tracker. |
| 6.3–6.5 · Direction and handoff | [x] The request to apply the audit recommendations is the direction for this backlog update. Assigned implementation by role in the sprint artifacts. |
| Implementation decisions | [!] The deployed HTTP route owner, authoritative tenant enumeration and supported package ranges require owner evidence in 9.3, 16.4 and 9.5. |
