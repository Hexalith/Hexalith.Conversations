# Reality-Check Review — architecture.md (Conversations)

- **Review date:** 2026-08-18
- **Reviewer lens:** every committed decision web-researched or reality-checked, not asserted from training data
- **Target:** `_bmad-output/planning-artifacts/architecture.md` (2,359 lines, read in full; May 14 analysis + July 15 rebaseline amended to v8 + overlays V9–V12 through 2026-08-04)
- **Evidence base:** repo inspection (`global.json`, `Directory.Packages.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`, `Hexalith.Conversations.slnx`, `src/`, `tests/`, submodule trees under `references/` read-only) + live web checks (versionsof.net, aspire.dev/devblogs, xunit.net/NuGet) on 2026-08-18.

## Verdict

The document's structural and ownership decisions are strongly reality-grounded: every named platform surface it commits to (`Hexalith.EventStore.DomainService`/`ServiceDefaults`/`Aspire`, `Hexalith.Commons.TenantAccess`/`Http`/`Serialization`/`Diagnostics`, `AddEventStoreDomainService`/`UseEventStoreDomainService`, `AddTypedHttpClient`) exists in the tracked submodules, the SDK pin claim (10.0.302) matches `global.json` byte-for-byte, the AppHost test-only boundary (`IsPackable=false`, `IsPublishable=false`) is mechanically real in the csproj, `.slnx` + Central Package Management match the described build shape, and the ten-root-gitlink count in V12 matches root `.gitmodules`. However, the document's third-party **version notes were last refreshed selectively** (the SDK number was updated to 10.0.302 while the Aspire/Dapr numbers beside it were not), so still-binding text now asserts sibling pins of "Aspire 13.2.x" and "Dapr 1.17.7" when the shared `Hexalith.Builds` props — and this repo's own AppHost SDK line — pin **Aspire 13.4.6** and **Dapr 1.18.5**, and the "current public Aspire docs show 13.3" claim is a stale May-era web check (current stable is 13.4, 13.5 in development). One rebaseline sentence still describes `src/Hexalith.Conversations.ServiceDefaults/` as present drift although it has already been removed, and the authoritative target test tree has drifted from the actual `tests/` inventory in both directions. No finding invalidates a committed architectural decision — the drift is in the factual scaffolding around decisions, which this document's own conformance philosophy ("a source token alone is not evidence") says must be regenerated, not hand-carried. **Verdict: PASS WITH CORRECTIONS — no critical findings; two high-severity stale version claims in still-binding text should be corrected before the next authority publication.**

---

## Findings

### F1 — HIGH — Deferred decision premise stale: "sibling modules currently pin 13.2.x" (Aspire)

- **Claim (line 738, `Core Architectural Decisions → Deferred Decisions`, still-binding):** "Aspire 13.3 upgrade should be evaluated separately because sibling modules currently pin 13.2.x."
- **Reality (repo):** `references/Hexalith.Builds/Props/Directory.Packages.props` pins `Aspire.Hosting` and all Aspire packages at **13.4.6** (plus `Aspire.Hosting.Keycloak`/`Kubernetes` at 13.4.6-preview); `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` uses `Sdk="Aspire.AppHost.Sdk/13.4.6"`.
- **Reality (web):** Aspire 13.4 is the current stable line — "Aspire 13.4 is here" (https://devblogs.microsoft.com/aspire/whats-new-aspire-13-4/), latest release 13.4.6 (https://github.com/microsoft/aspire/releases/tag/v13.4.0, releases page), with a 13.5 changelog in development (https://github.com/microsoft/aspire/wiki/13.5-Change-log).
- **Delta:** The deferred decision is overtaken twice over: the siblings did not stay on 13.2.x, and the evaluated upgrade target (13.3) was itself passed without the document recording the evaluation. A reader of still-binding text is told a pin state that is two minor versions behind the tree the document governs. The underlying rule ("align with sibling pins first; upgrades evaluated separately") is unaffected and the repo does comply with it — but the factual premise was never re-verified.

### F2 — HIGH — Version Position bullets stale: Aspire "13.2.x/13.3" and "Dapr client packages at 1.17.7"

- **Claim (lines 934–937, `Historical Infrastructure & Deployment Decision (Superseded For Production Ownership)` → Version Position):** "Current public Aspire docs show Aspire 13.3. Sibling modules pin Aspire mainly around 13.2.x and Dapr client packages at 1.17.7. Decision: align with sibling module pins first."
- **Reality (repo):** shared props pin Aspire packages at **13.4.6** and all `Dapr.*` packages (`Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Actors*`, `Dapr.AI*`, `Dapr.Workflow`) at **1.18.5**.
- **Reality (web):** current public Aspire documentation covers 13.4 (13.4.6, released 2026-06-19); 13.3 has been superseded since June 2026.
- **Delta:** The section heading is superseded only "For Production Ownership"; the Version Position list is written in the present tense and is the document's only statement of the Dapr pin anywhere. All three numbers (docs 13.3, siblings 13.2.x, Dapr 1.17.7) are stale against both the live web and the tracked submodule. Severity is high rather than medium because the same bullet list was demonstrably hand-refreshed for the SDK number ("current repository pin: 10.0.302") while the adjacent Aspire/Dapr numbers were left behind — a selective refresh that makes the stale values read as re-verified facts.

### F3 — MEDIUM — Still-binding rebaseline describes `src/Hexalith.Conversations.ServiceDefaults/` as present; it has been removed

- **Claim (line 335, `Target Ownership And Current Migration Input`, still-binding):** "`src/Hexalith.Conversations.ServiceDefaults/` remains pre-Story-6.2 drift and is removed when it has no independently justified domain-specific responsibility."
- **Reality (repo):** `src/` contains only `Hexalith.Conversations`, `.Admin.Web`, `.AppHost`, `.Client`, `.Contracts`, `.Server`, `.Testing` — no ServiceDefaults directory, and `Hexalith.Conversations.slnx` carries no entry for it. Story 6.2 is `done` per the v8 block; the removal the sentence anticipates has happened.
- **Delta:** Present-tense text asserts a project exists that no longer does. The decision (remove it) is honored by reality, so nothing is architecturally wrong — but a validator or agent following the sentence literally would go looking for a project to delete. One clause ("has been removed by Story 6.2") would restore accuracy. Medium because it is still-binding text making a false present-state claim, mitigated because the drift is in the completed direction.

### F4 — MEDIUM — Authoritative target test tree drifted from the actual `tests/` inventory in both directions

- **Claim (lines 1400–1424, `Corrected Target Directory Structure`, marked "authoritative"):** tests are `AppHost.Tests`, `Contracts.Tests`, `Tests`, `Server.Tests`, `IntegrationTests`, `Conformance.Tests` (portable tier), and `Conformance.Server.Tests` (module-internal tier).
- **Reality (repo, `tests/` and `.slnx`):** `Hexalith.Conversations.Conformance.Server.Tests` does **not** exist (only `Conformance.Tests`), while two existing, solution-tracked test projects — `Hexalith.Conversations.Client.Tests` and `Hexalith.Conversations.Admin.Web.Tests` — are **absent** from the authoritative tree.
- **Delta:** The missing module-internal tier project is explainable: its creation belongs to held successor work (v5 tiering amendment → Story 6.9 → Epic 9 under V9), so that half is plan-not-yet-built, low concern. The omission of `Client.Tests` and `Admin.Web.Tests` is not explainable the same way — they exist today, the v6 amendment makes "the root `.slnx` defines the required root-owned test projects" load-bearing for the record contract, and the May 14 historical tree (line 1513) even listed `Client.Tests`. The authoritative tree was evidently not re-checked against the live solution when corrected.

### F5 — MEDIUM — Document currency: authority-chain artifacts V13/V14 and two approved 2026-08-18 proposals postdate the document's last overlay

- **Claim (implicit; overlay chain ends at V12, lines 2248–2358, "Updated: 2026-08-04"):** the last complete overlay marker (`conversations-architecture-2026-08-04-v12`) is how machine readers determine current authority (rule stated at lines 1840–1843).
- **Reality (repo):** `_bmad-output/planning-artifacts/` now contains `v13-current-proof-authority-v1.json` and `v14-current-candidate-authority-v1.json`, and two approved 2026-08-18 sprint-change proposals (`sprint-change-proposal-2026-08-18-e6-remediation-a3.md` — approved by the release owner, freezing V13 as point-in-time and adding a V14 successor authority — and `sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md`). Recent commits (`29c56fa`, `151f965`) rebind the V14 candidate.
- **Delta:** These are candidate/proof sidecar authorities in the E6-REMEDIATION chain, not architecture overlays, so V12 plausibly remains the architecture head by design — but the architecture file gives a reader no pointer that the authority ecosystem has advanced fourteen days and two authority versions past its own last entry. Flagged at medium as a currency observation for the validation gate, not as a defect in a committed decision; whether a V13/V14-aware architecture overlay is required is a question for the authority owners, not this lens.

### F6 — LOW — Superseded Version Note: "current public Aspire documentation shows newer Aspire 13.3" / "Aspire templates locally expose 13.0"

- **Claim (line 699, `Historical Starter Template Evaluation (Superseded)` → Version Note; source link line 709 to aspire.dev 13.3 what's-new):** written against the May 2026 web state.
- **Reality (web):** current stable is Aspire 13.4 (13.4.6); 13.5 in development (sources as in F1).
- **Delta:** Same staleness as F1/F2 but inside a section explicitly headed "(Superseded)" and framed as the May 14 record — low per the severity discipline. Note, however, that this Version Note was partially rewritten after May (it names the 10.0.302 pin, which only landed 2026-07-14), so its "current public ... documentation shows" phrasing is misleading about when the web check was performed.

### F7 — LOW — SDK currency: 10.0.302 pin accurate but one patch behind; a new 10.0.4xx feature band exists as of 2026-08-11

- **Claim (lines 699, 701, 934):** "the repository now pins stable SDK `10.0.302`" with `rollForward=latestPatch` feature-band policy; deliberate deviation from sibling `10.0.103` historical pin.
- **Reality (repo):** `global.json` = `{"version": "10.0.302", "rollForward": "latestPatch"}` — exact match.
- **Reality (web, versionsof.net/core/10.0):** 10.0.302 released 2026-07-14; **10.0.303** and **10.0.400** both released 2026-08-11 (runtime 10.0.11). (https://versionsof.net/core/10.0/, https://support.microsoft.com/en-us/servicing/dotnet/net-10/2026/net-10-0-update-august-11-2026)
- **Delta:** The document (last amended 2026-08-04) was accurate when written and the `latestPatch` policy absorbs 10.0.303 without a pin change, so this is informational: at the next authority refresh the pin note should acknowledge the August servicing wave, and the 3xx-vs-new-400-band question falls under the module's stated feature-band policy.

### F8 — LOW — Testing stack names verified; xUnit v3 product line moved to a stable 4.0.0 three days ago

- **Claim (lines 731, 963–969):** testing stack is xUnit v3, Shouldly, NSubstitute, Testcontainers (+ bUnit/Playwright where UI is in scope). No versions are asserted in the document.
- **Reality (repo):** all pinned in `references/Hexalith.Builds/Props/Directory.Packages.props`: `xunit.v3` **3.2.2**, `xunit.runner.visualstudio` 3.1.5, `Shouldly` **4.3.0**, `NSubstitute` **6.0.0**, `Testcontainers` **4.13.0**. All four technologies exist and are actively used (nine root test projects in `.slnx`).
- **Reality (web):** xUnit v3 3.2.2 was the current stable at the doc's last amendment; **xunit.v3 4.0.0 stable shipped 2026-08-15** (https://www.nuget.org/packages/xunit.v3, https://xunit.net/releases/v3/3.2.2).
- **Delta:** No document claim is wrong — "xUnit v3" is the product line and remains correct. Flagged only so the gate knows an upgrade decision now exists: operational history in this workspace records that a submodule bump to xunit.v3 4.0.0-pre broke umbrella restore (NU1608 against the shared 3.2.2 pin), so any 4.0.0 adoption is an umbrella-coordinated change, consistent with the document's "version-aligned with sibling modules unless an ADR records divergence" rule (line 938).

### F9 — LOW — Fluent UI Blazor referenced without version; platform actually rides a 5.0 release candidate

- **Claim (lines 679, 1666):** admin UI comes through FrontComposer and "Fluent UI Blazor conventions"; coherence section lists FluentUI among assigned technologies. No version asserted.
- **Reality (repo):** shared props pin `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` at **5.0.0-rc.4-26180.1** — a prerelease.
- **Delta:** The technology exists and is used, so the claim holds; noted because a committed decision leaning on a prerelease dependency is exactly the kind of live-default the lens asks to surface, and the document nowhere records that the platform's Fluent UI line is an RC. UX remains `preserved-not-activated`, so exposure is currently confined to preserved/non-activated surfaces.

### F10 — VERIFIED (no finding) — Platform landing-zone surfaces all exist as claimed

- **Claims (lines 333–358, Landing-Zone Register; line 344 canonical host pair):** verified against tracked submodule sources, read-only, no submodule init/update performed:
  - `Hexalith.EventStore.DomainService`, `Hexalith.EventStore.ServiceDefaults`, `Hexalith.EventStore.Aspire` — all present under `references/Hexalith.EventStore/src/`.
  - `AddEventStoreDomainService` / `UseEventStoreDomainService` — defined in `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` (telemetry in `EventStoreDomainTelemetryExtensions.cs`).
  - `Hexalith.Commons.TenantAccess`, `Hexalith.Commons.Serialization`, `Hexalith.Commons.Diagnostics` — present under `references/Hexalith.Commons/src/libraries/` and included in the root `.slnx`.
  - `Hexalith.Commons.Http` `AddTypedHttpClient` — present in `references/Hexalith.Commons/src/libraries/Hexalith.Commons.Http/HttpClientRegistration.cs`.
  - `IAsyncDomainProjectionHandler` / legacy `IDomainProjectionHandler` (ADR 0003 section, line 407) — both interfaces present in `Hexalith.EventStore.DomainService`.

### F11 — VERIFIED (no finding) — AppHost test-only boundary is mechanically real

- **Claims (lines 335, 353, 1408, 1940–1945):** `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` contains `<IsPackable>false</IsPackable>` and `<IsPublishable>false</IsPublishable>`, uses `Aspire.AppHost.Sdk/13.4.6`, and references only Conversations surfaces plus `Hexalith.EventStore.Aspire`/EventStore platform dependencies — exactly the Story 6.2 boundary the v3 amendment and V9 invariants describe. `tests/Hexalith.Conversations.AppHost.Tests/` exists.

### F12 — VERIFIED (no finding) — Build-shape claims match: `.slnx`, CPM, shared version baseline, ten root gitlinks

- **Claims (lines 655, 683, 1645–1649, 2290):** `Hexalith.Conversations.slnx` is the solution entry point (present at root); `Directory.Packages.props` sets `ManagePackageVersionsCentrally=true` and imports `references/Hexalith.Builds/Props/Directory.Packages.props` exactly as line 1647 describes; root `references/` contains exactly ten root-declared submodules (AI.Tools, Builds, Commons, EventStore, Folders, FrontComposer, Memories, Parties, Projects, Tenants), matching V12's "ten root gitlink paths".

---

## Sources

- https://versionsof.net/core/10.0/ — .NET 10 SDK 10.0.302 (2026-07-14), 10.0.303 + 10.0.400 (2026-08-11), runtime 10.0.11
- https://support.microsoft.com/en-us/servicing/dotnet/net-10/2026/net-10-0-update-august-11-2026 — August 2026 .NET 10 servicing
- https://devblogs.microsoft.com/aspire/whats-new-aspire-13-4/ — Aspire 13.4 announcement (current stable line)
- https://github.com/microsoft/aspire/releases — Aspire 13.4.6 latest release
- https://github.com/microsoft/aspire/wiki/13.5-Change-log — Aspire 13.5 in development
- https://devblogs.microsoft.com/aspire/whats-new-aspire-13-3/ — the 13.3 line the document cites (now superseded)
- https://www.nuget.org/packages/xunit.v3 — xunit.v3 4.0.0 stable (2026-08-15)
- https://xunit.net/releases/v3/3.2.2 — xUnit v3 3.2.2 (2026-01-14), the pinned version
- Repo evidence: `/home/administrator/projects/hexalith/conversations/global.json`, `Directory.Packages.props`, `Hexalith.Conversations.slnx`, `src/`, `tests/`, `references/Hexalith.Builds/Props/Directory.Packages.props`, `references/Hexalith.EventStore/src/`, `references/Hexalith.Commons/src/libraries/`, `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`
