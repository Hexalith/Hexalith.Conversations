# G-6 Conversations Toolkit Dapr pin candidate — 2026-09-27

**Status: pending exact G-6 packet owner acceptance.** This note records a narrow AppHost consumption fix at Conversations checkout `819e45b` and does not change a historical accepted record. Jérôme Piquot authorized the `CommunityToolkit.Aspire.Hosting.Dapr` `13.5.1-beta.767` candidate for the isolated G-6 rerun.

The shared Builds catalog selects `.767`, but the Conversations AppHost previously restored transitive `.757`. The AppHost now declares `<PackageReference Include="CommunityToolkit.Aspire.Hosting.Dapr" />` in `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`; central package management supplies the version. The changed project's SHA-256 is `f23b17b433072ef019e4fda56f5ca006c6477d2cc670395aa8c5c3c90ad2af80`. This confines the selection to the AppHost instead of enabling transitive pinning for every Conversations project.

| Check from the Conversations root | Result |
| --- | --- |
| `dotnet restore src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --force -p:NuGetAudit=false` | Exit `0`; `project.assets.json` resolves direct `CommunityToolkit.Aspire.Hosting.Dapr/13.5.1-beta.767` from the NuGet.org source. |
| `dotnet build src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --no-restore --configuration Release -warnaserror -p:NuGetAudit=false` | Exit `0`; zero warnings and errors. |
| `TMPDIR=/var/tmp dotnet test tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj --configuration Release -p:NuGetAudit=false` | Exit `0`; 8 passed, 1 intentional opt-in live-boundary skip. The skip is not runtime proof. |
| `git diff --check` | Exit `0`. |

The separate Builds-owned broad workspace check, run from `references/Hexalith.Builds` as `pwsh -NoProfile -File Tools/validate-package-version-exceptions.ps1 -InventoryPath Tools/package-version-exceptions.json -CatalogPath Props/Directory.Packages.props -WorkspaceRoot ../..`, exited `1` with 14 existing inventory drifts. They include five AppHost SDK `13.5.3` versus Aspire.Hosting `13.5.4` alignments, their allowlist mismatches, an unlisted Builds AppHost, EventStore admin CLI `3.82.0` versus allowlisted `3.48.0`, and absent Timesheets/ChatBot allowlist entries. The focused Builds-only inventory check without `-WorkspaceRoot` exited `0` with 15 allowlisted exceptions. Neither broad result is caused by the Conversations `.767` reference, and the broad gate remains nonpassing.

The accepted 2026-09-06 G-6 baseline and packet remain historical. The fresh Projects G-6 packet at `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260927/packet.json` has raw SHA-256 `ccc2cab78b24873bb92133d4a5bc753d7bf9b7a8ffe1ff418d847572db54745d`; its candidate validator passes after the two-sidecar persisted qualification. It binds this uncommitted Conversations AppHost change by exact file hash. Named acceptance of that packet and committed source closure or an explicit worktree-binding disposition are still required before G-6 can unblock P0 Stage 6.
