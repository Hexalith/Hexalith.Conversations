# Minimal-module platform compatibility

The isolated Release build qualifies the maintained minimal-module fixture on net11.0
using the package versions in the pinned Builds catalog. The overlay expects
EventStore 5.6.0 and rejects catalog drift before restore. NuGet uses the four
compatibility locks in `locks/package/`. Standalone fixture builds use their own
four locks. Both graphs use the fixture's internal project references and published
EventStore packages.

`Framework.props` selects the direct catalog and restores the active target framework
during SDK reference discovery. `RegisteredCommand.targets` selects the compatibility
locks and adds one registered-command assertion to the original six fixture tests.
The assertion exercises creation, replay in a fresh scope, and a no-op command using
the replayed state. The AppHost compiles without starting external resources.

Run the seven built assertions with `python3 tests/compatibility/MinimalModule/run_compatibility.py`.
Run the graph, lock selection and drift checks with
`python3 -m unittest discover -s tests/tooling -p test_minimal_module_compatibility_overlay.py`.

The historical SM-2 authoring-cost measurement refers to its recorded checkpoint.
The standalone fixture adopted the shared catalog in `b998a24c`. The entries below
describe earlier qualifications.

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
