# Published CLI/Platform qualification and adoption-disposition plan

Prepared 2026-10-28 at Conversations
`c9c55d5a4033bfd7b22a4cf2298467b8e3549dac`. The user selected preparation
of this separate plan. Its [draft spec](spec-epic-16-published-artifact-platform-qualification.md)
controls later implementation; no implementation or runtime-entry approval is
inferred. This is one prerequisite handoff, with no public runtime/API change.

## Inspected inputs and preservation

[Planning input inventory](epic-16-published-artifact-platform-qualification-inputs.json)
records current full repository identities and source hashes. The prior
[post-tooling observation](story-16-1-entry-investigation-post-tooling-2026-10-28.md)
retains the fifteen measured commands and exact current inventory; no new tool,
package, native-host or AppHost qualification ran while preparing this plan.

| Input | Current observed boundary |
| --- | --- |
| Builds checkout/root gitlink | `81ab049208678bd6616828824729c0ddac02f7d1` |
| Platform checkout/root gitlink | `5889400b50e6e2cefb4ed6976ca53d086d3989e2` |
| Root / Platform exact tool pins | 4.38.1 / 4.35.2, both roll-forward disabled |
| Installed root informational identity | `4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3`; an observation, not independently qualified release provenance |
| Current declaration/adoption | Declaration PASS; adoption six PASS/sixteen FAIL |
| Tooling v2 | All 81 bound source hashes match; six commands BLOCKED/ENTRY_QUALIFICATION_PENDING |
| Runtime | Draft/backlog; entry null; independent trust empty; ACTIVE hold and four false flags |

Preserve all existing uncommitted planning work, all accepted predecessor
carriers, binding/schema/trust files, archives, fixtures, both frozen hosts,
tool manifests, `.gitmodules`, staged rows and all twelve direct checkout
tuples. Capture actual state again at execution entry. A changed input needs
an explicit measured delta, not a checkout/index repair. Never initialize nested
dependencies. New observations use fresh paths and local byte-preserving
attributes, leaving root attributes and source bindings unchanged.

The runtime draft now delegates this prerequisite; its prior planning history
is retained separately. No historical report or accepted evidence is rewritten.

## Candidate and producer qualification boundary

Investigate the root's exact **4.38.1**, with no ambient latest-version selection
or fallback to 4.38.0/4.35.2. Discover its actual published archive, source tag,
NuSpec repository commit, successful producer CI/release and exact inventory.
Resolve every revision through version control and retain complete identifiers.
Do not derive release authority solely from installed version text or today's
producer HEAD. An unavailable archive/tag/inventory is BLOCKED; conflicting
available identities are FAIL.

Builds' `docs/module-cli-configuration-key-released-artifact-handoff.md` records
historical 4.38.0 from `e171c5a788eab183b32683431b084198c9b8d373`.
Its artifact and successful controls do not qualify 4.38.1. Platform's README
names accepted 4.35.2 catalog/tag commit
`4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`; later-4.x range eligibility
does not establish the same catalog or an integration-qualified tuple.

Use only the actual producer-CI matched Release host/package inventory. The
existing producer lane is:

```text
Tools/test-g4-tool-package-contracts.ps1 -Version <exact-version> -RequireControls -RetainPackageDirectory
```

This is a description of required producer CI evidence, not a local Release
build command. Do not dispatch publication or substitute Debug packing,
old host binaries, `-SkipSourceValidation`, or a partial passing lane. Retain
every native result/skip and the exact fixture inventory, including any hidden
file exclusion and raw-Git versus filtered-checkout byte differences.

## Collector and command contract

Before collector code edits, run and inspect the existing mapped Development/Debug
baseline at `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`.
Follow the prior plan's [normal baseline procedure](epic-16-tooling-qualification-and-binding-refresh-plan.md#required-apphost-baseline):
verify current Aspire help and sessions, prepare the complete producer-selected
source environment without dependency updates, run isolated/detached, inspect
all required resources with bounded waits, and stop only this attempt's exact
instance. Missing mappings or unhealthy resources block code edits; no offline
exception, topology change, test credentials or tenant-event seeding is granted.
Retain safe diagnostics. This readiness observation does not qualify AD-5/AD-6,
ordinary registered module run/debug or tenant delivery.

Add the root-owned offline collector named in the draft, reusing contained
reads and output ownership checks. Its interface accepts explicit `--builds`,
`--platform`, `--conversations`, `--version`, `--release-sha`, `--inventory` and
`--output`. Require contained repositories, pinned/clean dependency identities,
a new non-aliased output directory and an exact candidate; emit a nonempty
ledger with per-gate state. No command mutates manifests, dependency checkouts,
trust or lifecycle. Output cannot overwrite source/accepted evidence.

Keep native process exits separate from the collector's verdict: a proven
defect makes the overall result FAIL/exit 1; otherwise any unavailable required
qualification makes it BLOCKED/exit 2; only a fully supported set can PASS/exit 0.
Retain all individual states. Use stable codes for artifact unavailability,
identity mismatch, incomplete controls, adoption-inventory drift and pending
owner evidence; tests must prove each guard with a positive control. An internal
collector defect is not an unavailable external qualification.

The historical probe under Builds
`evidence/g4/module-configuration-key-release-20261025/resumed-20261025T101341Z/probe-published-artifact-03.py`
provides a procedure reference only. Preserve it unchanged. It hardcodes
predecessor adoption equality, adoption exit 1 and old field indexes; it also
has unbounded captures/download reads. A new collector must locate fields
semantically, compare the actual fresh inventory and retain native command
exits separately from qualification verdicts. Neither expected rejection nor
an old wrapper exit becomes a passing adoption result.

Use a 30-minute collection deadline, 300-second network/restore limits,
60-second validation limits, 600-second diagnostic test limits, 8 MiB per stream,
256 MiB per downloaded archive and 2 GiB total expanded package contents. Enforce
limits while streaming, with bounded cancellation/descendant cleanup. Record any
justified additive limit revision before a new attempt; never silently truncate.
Use explicit per-process and overall deadlines and capture the limits with results;
timeout, incomplete capture or unavailable safe inspection is BLOCKED. Test
identity/hash substitution, field relocation, missing/extra/duplicate controls
and adoption rows, output aliasing, timeout/cancellation and unchanged-input
restoration. No new generally reusable module runtime or proprietary CLI is
introduced by this offline evidence collector.

Restore into fresh explicit tool, NuGet package/HTTP cache and `DOTNET_CLI_HOME`
directories with a clear-only verified NuGet.org configuration. Never restore
over the active local tool installation. The underlying argument templates are:

```text
dotnet tool install hexalith.builds.module.cli --version <exact-version> --tool-path <fresh-tool-directory> --configfile <isolated-config> --no-cache
dotnet nuget verify <actual-signed-archive> --all
<isolated-tool>/hexalith-module --version
<isolated-tool>/hexalith-module validate --manifest <real-manifest>
<isolated-tool>/hexalith-module validate --manifest <real-manifest> --output json
<isolated-tool>/hexalith-module validate --manifest <real-manifest> --adoption
<isolated-tool>/hexalith-module validate --manifest <real-manifest> --adoption --output json
```

Resolve arguments in the collector from verified inputs, execute without shell
evaluation, and retain exact argument arrays/cwd/environment identities. Compare
archive feed SHA-512/size/signature, signed-archive SHA-256, qualified CI archive
and uncompressed payload, raw NuSpecs/repository commit, CLI and packaged/loaded
assembly identities, loose/embedded/source schemas and catalog/Props. Signed
and unsigned outer ZIP hashes remain distinct; record any BOM or checkout-filter
difference explicitly rather than assuming byte equality.

Bind the exact Platform release/source, its recorded Builds release/catalog,
range and pin against the candidate. Missing clean tagged Platform identity or
registered bindings remains BLOCKED; available incompatibility is FAIL. No
pin alignment is proposed until qualification and a separate owner decision.
Do not run ordinary module `run`/`debug`: Platform enrollment requires reviewed
registration and source/configuration/authentication/gateway/Dapr bindings first.

## Positive and malformed-key controls

Require ordinary real-manifest validation in human and JSON formats. Locate
both configuration-key fields semantically. Create temporary copies for these
nine exact values in each field and both formats, yielding **36 controls**:

```python
['', ' ', '1TOKEN', '_TOKEN', 'TOKEN_', 'APP__API_TOKEN',
 'App::Token', 'APP_API_TOKEN\n', 'A' * 257]
```

Each negative must exit 1 with one exact-field HXP002 and unchanged input bytes;
the real declaration stays unchanged. Retain all 18 mutated inputs, 36 argument
arrays/native streams and both positive controls. Missing or vacuous controls
cannot pass. Require the candidate's preparation/action export evidence, with
complete producer-selected mapping and exact package/Release mode; unavailable
supported package test infrastructure is BLOCKED rather than source-mode credit.

## Adoption and retained review dispositions

Freeze every current row from the prior observation: 22 total, six passing and
sixteen failing. Failures comprise one tool-pin, twelve dependency-selector and
three Dapr rows. Compare ordered human/JSON identities and exact additions,
removals and reason changes at the fresh candidate. Do not force the old count
onto a different candidate. Retain failure bytes, class (live consumer, generated
import, historical source or accepted fixture), owning repository/role, evidence,
proposed correction, acceptance state and next action for each identity.

Archive classification is not a waiver. Preserve the four newly scanned
`attempt-20261028-implementation-03` imports and the prior twelve failures.
No archive deletion, scanner exclusion, fixture amendment, frozen-host removal
or invented owner acceptance is included. Producer policy corrections, fixture
migration, host exemption/relocation and pin alignment become separately scoped
owner work with named regressions before implementation.

Carry all thirteen producer review findings, ten groups and rejected B8/B11
from Builds' `docs/module-cli-configuration-key-release-review.md` and the
completed prerequisite's `qualification-ledger.json`. Match evidence to the
candidate source/artifact for cleanup/launch races, first-timeout attribution,
Linux/non-Linux process discovery, forged listings, list cancellation, the
67,001-byte claim boundary, crash durability, Windows/macOS native discovery
and later batch stale bindings. Passing source preparation does not close them.

Available source diagnostic reruns use individual Debug builds and built test
assemblies with `-automated sync -failSkips -class <exact-class> -trx <fresh-path>`;
class names and paths remain in the completed predecessor plan. Linux skips
earn no Windows/macOS credit. New upstream fixes and the machine-crash policy
are outside this implementation scope.

## Completion and remaining owner gates

The collector tests, actual artifact identity and all 36 malformed controls
must pass before this prerequisite implementation completes. Missing artifact,
source/CI identity or actual control evidence blocks completion; preserve all
failed attempts separately. Unavailable external owner/native acceptance may
remain BLOCKED in the handoff without granting tuple acceptance. Proved
identity/control defects are FAIL and stop acceptance.

Retain only additive local observations and handoff links. Record actual
source/package/deployed provider parity, AD-5 envelope, recovery/operator/capacity
disposition, supported published testing-helper strategy, native hosts and human
SM-2 as separate owner inputs. Existing runbooks and the pending SM-2 checklist
remain unchanged. Do not nominate independent trust or reuse historical IR-0
as current authorization. No external owner messages are sent.

Before handoff, run collector faults, its exact command matrix and both six-command
Story 16.1 contract routes. V1 remains FAIL/SOURCE_DRIFT; unchanged v2 remains
BLOCKED/ENTRY_QUALIFICATION_PENDING, with zero runtime measurements and seven
absent outputs. Recompute all retained stream/source hashes, input restoration,
raw gitlinks, dependency tuples, index rows and exact changed-path set. Historical
authority publication/replay is not applicable. Runtime 16.1 remains draft/backlog.
