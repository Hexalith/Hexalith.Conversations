# Reality / Currentness Review — `architecture.md` through V14

- **Review date:** 2026-09-16
- **Target:** `_bmad-output/planning-artifacts/architecture.md` (2,698 lines; last
  changed by commit `fe3f6fa`, the V14 publication)
- **Repository state reviewed:** `main` at `fd3dd58`, initially clean and aligned
  with `origin/main`
- **Lens:** verify named technologies, versions, repository-shape claims, and
  material present-state assertions against the current tracked repository and
  current official primary sources; distinguish immutable history from effective
  current authority
- **Mutation boundary:** validation only. The target was not changed.

## Verdict

**FAIL AS A CURRENT STANDALONE AUTHORITY; V14 REMAINS A COHERENT HISTORICAL
ARCHITECTURE BASE.** The architecture's core technical choices still exist and
fit the codebase: .NET 10, Aspire, Dapr, the EventStore domain-service seams,
the projection contracts and key grammar, Central Package Management, `.slnx`,
and the named test stack are all present. The V14 graph claim also still matches
the tracked graph artifact at 38 nodes and 61 edges.

The failure is currentness. The document's binding discovery rule ends at the
V14 sidecar, while the committed authority chain has advanced through V21. The
V21 record carries a narrowly scoped Story 7.1 lift, but the current V21 checker
rejects `HEAD` with `V21_DESCENDANT_GITLINK_DRIFT` and computes the effective
hold as `ACTIVE`. Its current-version statements are also overtaken by V18 and
today's tree: SDK 10.0.401, Aspire 13.5.3, and Dapr 1.18.7. A builder may safely
inherit V14's technical invariants, but must not resolve current execution state,
workflow inventory, or package reality from this file alone.

**Severity count:** 1 critical · 3 high · 2 medium · 1 low.

## Methodology and evidence boundary

1. Read the complete target, the required repository baseline, `.gitmodules`,
   current solution/build files, relevant source contracts, and all committed
   V15–V21 authority sidecars.
2. Compared every explicit version statement with `global.json`, the imported
   `references/Hexalith.Builds/Props/Directory.Packages.props`, and the AppHost
   SDK line. Compared named internal surfaces with tracked source rather than
   inferring them from documentation.
3. Verified time-sensitive external facts against primary sources as of the
   review date:
   [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0),
   [Aspire CLI/current docs](https://aspire.dev/get-started/install-cli/),
   [Aspire 13.5.3 API reference](https://aspire.dev/reference/api/csharp/),
   [Dapr.Client 1.18.7](https://www.nuget.org/packages/Dapr.Client/1.18.7),
   [xUnit v3 4.0.1](https://www.nuget.org/packages/xunit.v3/4.0.1),
   [Fluent UI Blazor package history](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components),
   and [WCAG 2.2](https://www.w3.org/TR/WCAG22/).
4. Ran the historical publication checker read-only:
   `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check`.
   It returned `CANDIDATE_SOURCE_DRIFT: pyproject.toml`. This is not evidence that
   V14's frozen bytes were corrupted; it is evidence that the V14 publisher no
   longer describes current `HEAD` after the later V15–V21/tooling work.
5. Ran the current successor checks read-only:
   `python3 _bmad/scripts/publish_story_7_1_successor_authorities.py v21 --check`
   and the same route with `--effective-hold`. Both exited 1 with
   `V21_DESCENDANT_GITLINK_DRIFT`; the latter returned `result: FAIL` and
   `effectiveHold: ACTIVE`. Also compared the V14 route/alias inventory with
   tracked `HEAD` using `git ls-tree` and the BMAD 6.12.0 update history.

## Findings

### F1 — CRITICAL — The resolver stops at V14; V21 is committed but currently fails closed

**Architecture claim.** The binding discovery rule says the last complete
architecture overlay marker names the current checkpoint sidecar, and that a new
sidecar must be published with a pointer amendment
(`_bmad-output/planning-artifacts/architecture.md:2415-2430`). Both V14 markers
still name `v14-current-candidate-authority-v1.json`, its V13-era SHA-256, and
`hold=ACTIVE` (`architecture.md:2627`, `architecture.md:2698`). The V14 body also
says `implementationHold` remains `ACTIVE` (`architecture.md:2679-2681`).

**Current tracked reality.** Seven later named authority files are committed:
`v15-planning-tooling-environment-authority-v1.json` through
`v21-story-7.1-authority-correction-v1.json` (all resolve as tracked mode-100644
files). V15 and V16 retain `ACTIVE`
(`v15-planning-tooling-environment-authority-v1.json:178`;
`v16-planning-tooling-lifecycle-authority-v1.json:229`); V17 lifts only
`7.1-SCHEMAS` (`v17-implementation-hold-decision-authority-v1.json:120-130`);
V18 returns to `ACTIVE` with Story 7.1 binding unresolved
(`v18-package-environment-authority-v1.json:235-240`); and V19 passes its
checkpoint but retains `ACTIVE` and disallows full Story 7.1 execution
(`v19-story-7.1-checkpoint-completion-authority-v1.json:255-263`). V20 records
the release owner's 2026-09-14 scoped decision to lift Story 7.1 only
(`_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json:9-24`),
and V21 preserves that as `implementationHold: LIFTED`, `unlocks: ["7.1"]`,
`global: false`, while still prohibiting Story 7.2, release, and push
(`_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json:118-130`).
V21 was published by committed change `d297f96`; it is not working-tree draft
state. However, at `HEAD` `fd3dd58`, both `v21 --check` and
`v21 --effective-hold` fail with `V21_DESCENDANT_GITLINK_DRIFT`; the latter
computes `effectiveHold: ACTIVE`. A direct `d297f96..HEAD` comparison confirms
post-V21 changes to the EventStore, Folders, and FrontComposer gitlinks.

**Why this matters.** V14 is still the technical architecture identity and its
invariants remain usable, but it is no longer the effective checkpoint/execution
head. A literal reader cannot discover V15–V21 or explain why the later scoped
authorization is currently fail-closed. Treating V21's recorded `LIFTED` value
as presently effective would be wrong; even before the current failure it was
scoped, never global.

**Disposition:** **DISCUSS, THEN UPDATE.** Preserve V14 bytes as immutable
history. Publish an additive pointer/currentness overlay (or an equally
mechanical delegated resolver) that reaches V21, invokes the effective-hold
computation, and reports the current `FAIL`/`ACTIVE` result. Resolve or
explicitly authorize the post-V21 gitlink policy before claiming the Story 7.1
lift. Do not rewrite V14 or broaden the authorization.

### F2 — HIGH — Every present-tense platform-version snapshot in the architecture is superseded by committed package authority and current repo pins

**Architecture claims.** Historical sections use present-tense wording for the
repository SDK pin (`10.0.302`) at `architecture.md:699-701` and
`architecture.md:934`. V13 correctly classifies the old Aspire 13.0/13.2/13.3
and Dapr 1.17.7 statements as historical, then records a dated 2026-08-18
snapshot of Aspire 13.4.6 and Dapr 1.18.5
(`architecture.md:2580-2585`).

**Current tracked reality.** The later committed V18 environment authority
records SDK `10.0.401` and Aspire `13.5.3`
(`_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json:199-211`).
The live tree agrees: `global.json:3-4` pins `10.0.401` with
`rollForward=latestPatch`; shared props pin mainline Aspire packages to 13.5.3
(`references/Hexalith.Builds/Props/Directory.Packages.props:109-121`) and Dapr
packages to 1.18.7 (`.../Directory.Packages.props:139-146`); the AppHost SDK is
`Aspire.AppHost.Sdk/13.5.3`
(`src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj:1`).
Microsoft lists 10.0.401 as the current .NET 10 SDK released 2026-09-08, Aspire's
current docs expose 13.5.3, and Dapr.Client 1.18.7 explicitly targets `net10.0`:
[.NET](https://dotnet.microsoft.com/en-us/download/dotnet/10.0),
[Aspire](https://aspire.dev/reference/api/csharp/),
[Dapr.Client](https://www.nuget.org/packages/Dapr.Client/1.18.7).

**Historical/current disposition.** The 2026-08-18 snapshot was accurate at its
stated date and should remain historical. The older 13.0/13.2/13.3 and 1.17.7
values are already explicitly superseded by V13. The false current impression
comes from the lack of a later architecture pointer/refresh, not from technology
non-existence. The durable decision to follow sibling central pins is satisfied
by the current tree.

**Disposition:** **UPDATE.** Add a dated current-environment pointer/refresh that
delegates exact pins to V18 plus the imported Builds catalog. Preserve all older
numbers as dated evidence; do not turn the architecture into a second package
catalog.

### F3 — HIGH — The V14 workflow inventory requires four forwarding aliases that tracked HEAD removed

**Architecture claim.** Deprecated `bmad-dev-auto` and `bmad-quick-dev` must be
single-hop aliases in both `.agents` and `.claude`, and missing paths block
publication (`architecture.md:2160-2171`, `architecture.md:2323-2332`). The V14
publisher encodes the same four paths and validates their routing and parity
(`_bmad/scripts/publish_v9_planning_authority.py:238-243`, `:1171-1190`).

**Current tracked reality.** All four alias `SKILL.md` files are absent from the
`HEAD` tree, while `bmad-build` and `bmad-build-auto` remain in both trees. Commit
`1c36c45` (`fix: update BMAD 6.12.0`) deliberately deleted the aliases. This is
a superseded workflow projection, not unexplained filesystem loss, but no later
architecture overlay updates V14's exact inventory or its fail-closed rule.

**Disposition:** **UPDATE THE AUTHORITY, NOT THE ALIASES AUTOMATICALLY.** Decide
whether BMAD 6.12.0's removal is the accepted route set, then publish a new exact
inventory/parity authority and checker binding. Until then, V14 route-currentness
claims do not pass against tracked reality.

### F4 — HIGH — The retained module AppHost exists, but its claimed fit still conflicts with the required Hexalith baseline

**Architecture decision.** The target retains a Conversations-owned AppHost as
a non-shipping test fixture (`architecture.md:61-70`, `architecture.md:333-335`,
`architecture.md:1396-1424`). DC-10 expressly calls this a "working
interpretation" and defers resolution to an Epic 12 hard-entry condition
(`architecture.md:2568-2575`).

**Repository reality.** The fixture exists, is solution-tracked, and is
mechanically non-packable/non-publishable:
`Hexalith.Conversations.slnx:39-46` and
`src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj:1-18`.
It consumes the platform's `Hexalith.EventStore.Aspire` surface, so it does not
duplicate a reusable Aspire library.

**Conflicting effective guidance.** The required baseline says a domain module
"must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project"
(`references/Hexalith.AI.Tools/hexalith-llm-instructions.md:121-134`) and says
the AppHost lives in the platform/host repository
(`references/Hexalith.AI.Tools/hexalith-llm-instructions.md:215-221`). The
architecture's narrower meaning of "ship" is not stated or accepted in that
baseline. The code proves the fixture's existence and packaging flags, not the
compatibility of the interpretation.

**Disposition:** **DISCUSS; DO NOT AUTOFIX.** Obtain the baseline owner's explicit
ruling before Epic 12. Either amend the baseline to exempt repository-local,
non-publishable test AppHosts or relocate/remove this fixture and amend the target
architecture additively. Until that ruling, do not describe the target as fully
baseline-conformant.

### F5 — MEDIUM — The current conformance project is labeled portable in the target but binds the non-packable Server assembly

**Architecture claim.** The two-tier rule says the portable tier binds only
Contracts, Client, and Testing, while the module-internal tier explicitly binds
Server (`architecture.md:119-139`). The authoritative target tree labels
`Hexalith.Conversations.Conformance.Tests` as portable and a separate
`Conformance.Server.Tests` as internal (`architecture.md:1410-1417`). V13's
dated factual refresh says the solution does not yet include the internal-tier
project (`architecture.md:2587-2590`).

**Current tracked reality.** The solution contains one conformance project
(`Hexalith.Conversations.slnx:48-56`). That project references Contracts,
Client, Testing **and Server**
(`tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj:5-10`).
No `Hexalith.Conversations.Conformance.Server.Tests` project exists. By the
architecture's own definition, the current project is module-internal, not the
portable project its target-tree comment declares. Story 9 remains backlog, so
this is unfinished migration rather than evidence of an accepted weakening.

**Disposition:** **DISCUSS/DEFER TO THE EPIC 9 OWNER.** Correct current-state
wording in the next additive refresh and make the Story 9 migration explicitly
produce a portable project/assembly surface without deleting or weakening the
current Server-bound assertions. The Quality owner remains the tier decider.

### F6 — MEDIUM — Fluent UI Blazor V5 exists and is the required line, but the repository is still on a release candidate

**Architecture commitment.** FrontComposer plus Fluent UI Blazor is the UI
foundation (`architecture.md:679`, `architecture.md:729`,
`architecture.md:903-914`). The required baseline sharpens this to Fluent UI
Blazor V5 (`references/Hexalith.AI.Tools/hexalith-llm-instructions.md:193-207`).

**Current reality and fit.** Shared props pin
`Microsoft.FluentUI.AspNetCore.Components` and `.Icons` to
`5.0.0-rc.5-26219.1`
(`references/Hexalith.Builds/Props/Directory.Packages.props:226-227`). The
package is real and current for the V5 preview line, but the official package
history still lists 4.14.4 as the latest stable and RC5 as prerelease, while the
project describes V5 as upcoming with stabilized APIs and regularly published
release candidates:
[NuGet package history](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components),
[official repository](https://github.com/microsoft/fluentui-blazor).
The current Conversations `Admin.Web` project does not yet reference
FrontComposer or Fluent UI (`src/Hexalith.Conversations.Admin.Web/Hexalith.Conversations.Admin.Web.csproj:1-8`),
consistent with preserved/non-activated UI scope rather than proof of production
fit.

**Disposition:** **DEFER WITH A REVISIT CONDITION.** Before UI activation, record
the exact V5 RC/stable policy, validate the chosen component APIs and migration
surface, and either move to V5 stable or explicitly accept the prerelease risk.
No architecture change is required merely because the technology exists as an
RC today.

### F7 — LOW — WCAG 2.1 AA remains valid, but it is not the current W3C-recommended target

**Architecture decision.** `architecture.md:920-924` fixes WCAG 2.1 AA as the
baseline.

**Current standard position.** WCAG 2.1 remains a valid W3C Recommendation and
is not deprecated. WCAG 2.2 is the newer Recommendation, is backward compatible
with 2.1, and W3C encourages use of the latest version:
[W3C WCAG overview](https://www.w3.org/WAI/standards-guidelines/wcag/) and
[WCAG 2.2 Recommendation](https://www.w3.org/TR/WCAG22/).

**Disposition:** **DEFER TO THE REQUIREMENTS/UX OWNER.** Keep 2.1 AA if it is a
deliberate contractual floor; otherwise promote 2.2 AA before UI activation.
This is a currency opportunity, not a factual error.

## Verified claims and technology-fit inventory

| Area | Current evidence | Result |
| --- | --- | --- |
| .NET / C# | `global.json:3-4` = SDK 10.0.401; `Directory.Build.props:18-24` = `net10.0`, nullable, implicit usings, warnings-as-errors, `LangVersion=latest`. Microsoft identifies .NET 10 as LTS and SDK 10.0.401 as current. | **Fits; architecture's exact old pin is historical (F2).** |
| Aspire | `AppHost.csproj:1` and shared props `:109-121` align at 13.5.3; [official Aspire docs](https://aspire.dev/get-started/install-cli/) also expose 13.5.3. | **Exists and fits local orchestration.** |
| Dapr | Shared props `:139-146` pin 1.18.7; [Dapr.Client 1.18.7](https://www.nuget.org/packages/Dapr.Client/1.18.7) includes `net10.0`. | **Exists and fits .NET 10.** |
| EventStore host/runtime seams | `src/Hexalith.Conversations.Server/Program.cs:14-44` uses `AddEventStoreDomainService` / `UseEventStoreDomainService`; referenced platform projects exist under `references/Hexalith.EventStore/src/`. | **Matches the platform-owned runtime decision.** |
| Projection vocabulary | `ProjectionTrustState` has exactly `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted` (`src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs:16-57`). | **Exact match to DC-2 (`architecture.md:2475-2485`).** |
| Temporal anchor | `ProjectionFreshnessV1` contains `ProjectionContractSchemaVersion`, `ProjectionCursor`, and `LastAppliedEventPosition` (`src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs:23-32`). | **Exact match to DC-5 (`architecture.md:2516-2528`).** |
| Derived-key grammar | State store, conversation/index prefixes, unpadded base64url encoding, and SHA-256 dispatch key match `ConversationProjectionReadModelKeys.cs:21-80`. | **Exact match to DC-7 (`architecture.md:2538-2555`).** |
| Tenant projection capability | `ITenantProjectionStore` exists (`references/Hexalith.Tenants/.../ITenantProjectionStore.cs:1-21`), but the shipped default remains explicitly in-memory/single-instance (`.../InMemoryTenantProjectionStore.cs:5-10`; `src/Hexalith.Conversations.Server/Program.cs:40-43`). | **V14 Story 16.1 is a real, still-unimplemented durability need, not a fabricated seam.** |
| Test stack | Shared props pin xUnit v3 4.0.1, Shouldly 4.3.0, NSubstitute 6.2.0, Testcontainers 4.15.0, bUnit 2.11.3, Playwright 1.62.0 (`references/Hexalith.Builds/Props/Directory.Packages.props:240`, `:259`, `:294`, `:311`, `:317-321`). Package sources confirm current .NET compatibility: [xUnit](https://www.nuget.org/packages/xunit.v3/4.0.1), [Shouldly](https://www.nuget.org/packages/Shouldly/4.3.0), [NSubstitute](https://www.nuget.org/packages/NSubstitute/6.2.0), [Testcontainers](https://www.nuget.org/packages/Testcontainers/4.15.0), [Playwright](https://www.nuget.org/packages/Microsoft.Playwright/1.62.0). | **All named technologies exist and fit; exact pins are centrally owned.** |
| OpenTelemetry | Shared props pin the OpenTelemetry family at 1.18.0 (`references/Hexalith.Builds/Props/Directory.Packages.props:266-275`); Server telemetry uses the ServiceDefaults-provided meter factory. | **Exists and fits the ServiceDefaults boundary.** |
| Solution/build shape | `Hexalith.Conversations.slnx` is the sole root solution; root `Directory.Packages.props:1-12` enables CPM and imports the Builds catalog; the Conversations-local ServiceDefaults project is absent. | **Matches V13's build-shape refresh.** |
| V14 graph | `v9-execution-graph-v1.json` contains 38 nodes and 61 edges, including Stories 16.1–16.3 and both current-proof checkpoints. | **Matches `architecture.md:2658-2677`.** |

## Current authority disposition summary

| Statement class | Status | Consumer rule |
| --- | --- | --- |
| V14 technical invariants and graph | Historical immutable authority, still technically consistent where verified | Inherit unless a later explicit authority changes the subject. |
| V14/V13 hold and sidecar-head statements | Superseded for current checkpoint execution by V15–V21, culminating in the scoped V21 Story 7.1 lift | Resolve through V21; do not infer a global lift. |
| Pre-V13 Aspire/Dapr version statements | Explicitly historical by `architecture.md:2582-2585` | Never use as package guidance. |
| V13 2026-08-18 version refresh | Accurate dated snapshot, no longer current | Use V18/current Builds catalog for exact pins. |
| SDK 10.0.302 "current" statements | Historical and superseded by committed V18 + `global.json` | Current pin is 10.0.401. |
| Central alignment rules | Still effective and observed | Continue consuming the imported Builds catalog; do not duplicate versions in architecture prose. |

## Gate conclusion

The architecture is **reality-grounded but not currentness-complete**. Its named
technology choices remain available and largely well fitted, and no evidence
supports replacing the core .NET/Aspire/Dapr/EventStore design. Handoff is
blocked only if this file is expected to be the sole current authority: it must
first gain an additive pointer to the V21 execution state and a current package
authority reference. The AppHost/baseline conflict remains an explicit human
decision, not an autofix.
