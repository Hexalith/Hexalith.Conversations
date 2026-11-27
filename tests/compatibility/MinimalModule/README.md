# Minimal-module platform compatibility

The fixture's 13 authored files and four original locks match checkpoint B
(`aeca17f120aa731d0d47b79b7d1b54fb63144bb8`). Those bytes define the historical
SM-2 authoring-cost measurement. The current Release graph uses the external
overlay and its four successor locks under `locks/package/`.
[Frozen proof](../../../docs/release-evidence/thin-module-fixture-proof-v1.json)

`Framework.props` selects net11.0 and the direct Builds catalog, including
during SDK reference discovery. `RegisteredCommand.targets` converts the
fixture's EventStore project references into centrally versioned package
references during evaluation, before NuGet walks the graph. It removes the
frozen inline package versions and keeps internal fixture project references.
The overlay expects EventStore 5.6.0 and verifies the direct and effective
catalog selections before restore. The Release build
compiles the AppHost, and the runner verifies the six original assertions and
one registered-command assertion.

## Regenerate and verify the successor locks

Use a disposable checkout with the direct Builds submodule and the SDK in
`global.json` (11.0.100). Run these commands from its root in a fresh Bash
session. The two NuGet restores use separate empty package and HTTP caches and
nuget.org as their sole source.

```bash
set -euo pipefail
scratch=$(mktemp -d)
trap 'rm -rf -- "$scratch"' EXIT
export TMPDIR="$scratch" TMP="$scratch" TEMP="$scratch"

overlay="$PWD/tests/compatibility/MinimalModule"
fixture="$PWD/tests/fixtures/Hexalith.Conversations.MinimalModule"
solution="$fixture/Hexalith.Conversations.MinimalModule.slnx"
config="$TMPDIR/NuGet.Config"
cat > "$config" <<'XML'
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <fallbackPackageFolders>
    <clear />
  </fallbackPackageFolders>
</configuration>
XML

props=(
    -p:Configuration=Release
    -p:MinimalModuleCompatibilityMode=package
    -p:MinimalModuleCompatibilityUseSuccessorLocks=true
    "-p:CustomAfterMicrosoftCommonProps=$overlay/Framework.props"
    "-p:CustomAfterMicrosoftCommonTargets=$overlay/RegisteredCommand.targets"
    -p:WarningsNotAsErrors=
    -p:NuGetAudit=true
    -p:NuGetAuditMode=all
    -p:RestoreFallbackFolders=
    -p:RestoreAdditionalProjectFallbackFolders=
)

unset NUGET_FALLBACK_PACKAGES
export NUGET_PACKAGES="$TMPDIR/packages-first" NUGET_HTTP_CACHE_PATH="$TMPDIR/http-first"
dotnet restore "$solution" --configfile "$config" --packages "$NUGET_PACKAGES" --force-evaluate "${props[@]}"
sha256sum "$overlay"/locks/package/*.json > "$TMPDIR/successor-locks.sha256"

export NUGET_PACKAGES="$TMPDIR/packages-locked" NUGET_HTTP_CACHE_PATH="$TMPDIR/http-locked"
dotnet restore "$solution" --configfile "$config" --packages "$NUGET_PACKAGES" --locked-mode --force "${props[@]}"
sha256sum -c "$TMPDIR/successor-locks.sha256"
dotnet build "$solution" --no-restore --configuration Release "-p:RestorePackagesPath=$NUGET_PACKAGES" "${props[@]}"
python3 tests/compatibility/MinimalModule/run_compatibility.py
python3 -m unittest discover -s tests/tooling -p test_minimal_module_compatibility_overlay.py
python3 -m unittest discover -s tests/tooling -p test_minimal_module_frozen_fixture.py
```

For routine qualification, compare the regenerated successor locks with the
checked-in bytes after both restores.

```bash
for lock in tests/compatibility/MinimalModule/locks/package/*.json; do
    git show "HEAD:$lock" | cmp - "$lock"
done
```

The focused tooling checks compare all 17 fixture files with the frozen proof
and check the external graph, lock selection, and catalog-drift refusal. When
refreshing the locks deliberately, copy the four files under `locks/package/`
back from the disposable checkout after both restores and the build pass. Builds CI
provides the managed authority for the Release adapter. The
[current validation runbook](../../../docs/runbooks/current-change-validation.md#mapped-build-and-tests)
shows the local mapped Release workflow for the product solution.

## Earlier qualifications

The Story 11.2 proof derives its .NET SDK version from the Release build receipt.
New receipts capture the SDK version evaluated by MSBuild for each fixture project.
The accepted v1 receipt has the SDK version in its MSBuild diagnostics. Earlier
receipts without an unambiguous SDK observation cannot reproduce their original
proof. A fresh build from that checkout produces a new receipt and proof.

## Published-package hash refresh (2026-10-28)

The broad SDK update changed the frozen fixture AppHost by accident. Its exact
checkpoint-B bytes and SDK 13.6.1 are restored; the historical SM-2 evidence and
all original fixture lock files remain unchanged.

The four external compatibility locks were regenerated with the normal NuGet
`--force-evaluate` producer in an isolated copy, using an empty package/HTTP cache
and a cleared configuration containing only nuget.org. A second independent empty
cache passed `--locked-mode`; the downloaded EventStore 3.128.3 package metadata
identifies nuget.org as its source. Only the published EventStore content hashes
changed. No local staged package supplied these identities.

The locked package graph built in Release with zero warnings or errors, and the
native result verifier required all original six assertions plus the registered
command assertion: seven passed, with zero failures, skips, or errors. CI retains
locked mode and package content-hash validation.

## Dependency qualification (2026-11-01)

The external selection now qualifies EventStore 3.130.1 from the direct Builds
catalog. Its published Aspire package requires Dapr hosting 13.6.2-beta.959;
the overlay selects that version to avoid NU1605 without changing the frozen
AppHost's SDK 13.6.1 or any other measured fixture bytes.

All four external locks were regenerated with the explicit package overlay and
`--force-evaluate` in an isolated copy configured to use only nuget.org. An
independent empty package and HTTP cache then passed `--locked-mode` without
changing those locks. The five EventStore 3.130.1 package metadata records
identify nuget.org as their source.

The locked graph built in Release with zero warnings or errors. Native xUnit
execution and `verify_results.py` required and passed all seven assertions,
with zero failures, skips, or errors. All 17 measured fixture files, including
their four original locks, remain byte-for-byte unchanged.

## Current catalog maintenance (2026-11-01)

The active external overlay now selects EventStore 3.133.0 to match the updated
direct Builds catalog. Its four successor locks were regenerated using only
nuget.org in an isolated copy. A second restore with independent empty package
and HTTP caches passed `--locked-mode` and left all four locks unchanged.
The original fixture sources and original locks remain unchanged. This local
check restored the package graph without compiling Release assets or starting
resources; the CI compatibility job performs the Release build and all seven
native assertions for the committed source.

## EventStore 4 package qualification (2026-11-03)

The active overlay selects EventStore 4.0.4 from the updated direct Builds
catalog. This fixes the catalog-drift refusal that blocked both the compatibility
and release-tooling CI jobs. The drift guard and locked-mode requirements remain
in force.

The four external successor locks were regenerated with `--force-evaluate` in an
isolated fixture copy using only nuget.org and empty package and HTTP caches. A
second restore with independent empty caches passed `--locked-mode` without
changing the locks. The exact CI Release build passed with zero warnings or
errors, and native execution plus `verify_results.py` passed all seven required
assertions with zero failures, skips, or errors. All 17 measured fixture files,
including their four original locks, remain byte-for-byte unchanged.

## Current SDK and package qualification (2026-11-12)

The external overlay selects EventStore 4.2.0 from the current Builds catalog and
Aspire.Hosting/AppHost 13.6.5 to retain patched hosting packages while preserving
the frozen AppHost SDK source. The original 17 fixture files are unchanged.

The four successor locks were regenerated with `--force-evaluate` in an isolated
source copy using only nuget.org. A second empty package and HTTP cache passed
`--locked-mode` without changing the locks. The Release graph built with zero
warnings or errors, and native execution plus the result verifier passed all
seven assertions with zero failures or skips. These checks start no resources
and establish no composed runtime acceptance.

## EventStore 5 and .NET 11 qualification (2026-11-14)

The active graph selects EventStore 5.1.0, published during this maintenance run,
from the deliberately refreshed direct Builds catalog. It uses net11.0,
Microsoft.NET.Test.Sdk 18.12.0 and xUnit 4.0.3 while retaining Aspire hosting
13.6.5 and Dapr hosting 13.6.2-beta.959. These external selections leave the
original fixture's SDK, source defaults, package versions and four locks intact.

All four successor locks were regenerated in an isolated copy using only
nuget.org and empty package and HTTP caches. A second independent empty-cache
restore passed `--locked-mode` without changing those locks. The Release build
passed with zero warnings or errors, and native xUnit execution with
`-automated sync -failSkips` plus `verify_results.py` required and passed all
seven assertions with zero failures, skips or errors. SHA-256 checks confirmed
all 17 frozen fixture files remained unchanged. This package build acceptance
starts no resources and makes no runtime deployment acceptance claim.

## EventStore 5.5.1 qualification (2026-11-24)

The external compatibility graph tracks EventStore 5.5.1 from Builds v6.7.0.
NuGet regenerated the four external locks in a disposable fixture copy with
only nuget.org configured. Locked restore passed from a second empty package
and HTTP cache and preserved all four lock hashes. The isolated Release test
project and AppHost compiled with zero warnings, and the seven compatibility
assertions passed. The measured fixture files matched the committed copies.

## EventStore 5.6.0 qualification (2026-11-26)

The overlay is an explicit successor of the pinned Builds catalog: it selects
EventStore 5.6.0, the same identity Directory.Packages.props publishes as
`HexalithEventStoreVersion`. This is not a silent lock workaround; the
catalog-drift gate still refuses any later mismatch before restore.

NuGet regenerated the four external successor locks in a disposable fixture
copy with only nuget.org configured. Locked restore from a second empty
package and HTTP cache preserved all four lock hashes. The isolated Release
test project and AppHost compiled with zero C# warnings or errors, and the
seven compatibility assertions passed. The original fixture sources and
original locks remain unchanged.
