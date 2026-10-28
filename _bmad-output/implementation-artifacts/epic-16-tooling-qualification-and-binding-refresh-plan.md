# Epic 16 tooling qualification and binding refresh plan

Prepared 2026-10-27 at Conversations
`26298b31eb4d362f88e008246ac3acb160128330`. The user selected planning only.
The [separate draft](spec-epic-16-tooling-qualification-and-binding-refresh.md)
controls any later implementation. Story 16.1 remains draft/backlog and held.

The user subsequently selected the normal AppHost baseline before code edits.
This resolves the proposed offline-exception question in favor of a real local
run and resource inspection. It is not implementation approval or runtime-story
entry, and it does not extend the earlier remediation's exception.

## Facts and preservation boundary

| Input | Inspected identity or disposition |
| --- | --- |
| Direct Builds source at planning entry | `5808d3b1a860d3afb75440b9726fca06703d0146`, clean |
| Direct Platform source at planning entry | `dc83a208930b95f1a0164229bebc8359024f2e48`, clean |
| Platform's recorded Builds | Same `5808d3b1a860d3afb75440b9726fca06703d0146` |
| Root and Platform accepted CLI | 4.35.2, roll-forward disabled |
| SDK | Both repositories pin 10.0.402/latestPatch |
| Current measured adoption | Six PASS, ten FAIL, identical human/JSON identities |
| Current six Story 16.1 commands | FAIL/exit 1/SOURCE_DRIFT on `.gitattributes` |
| Runtime trust | Entry null; independent trusted entries empty |

The [entry investigation](story-16-1-entry-investigation-2026-10-27.md) and its
[twelve command observations](../../module/validation/epic-16-entry-2026-10-27/results.json)
remain unchanged. Existing dirty root documentation and untracked observations
belong to that investigation; preserve them. No dependency work or fresh native
qualification was performed while preparing this plan.

The final preservation check observed external dependency checkout advances
while Conversations HEAD, its index and all 42 protected root file hashes
remained unchanged. The following checkout identities supersede the entry
snapshot for future qualification; none receives qualification credit here.

| Root-declared checkout | Entry HEAD | Later observed HEAD |
| --- | --- | --- |
| Builds | `5808d3b1a860d3afb75440b9726fca06703d0146` | `f914752cf8223b683799d4b25b4b0f742863297b` |
| EventStore | `4dc256c4db7a0ba88efdbbe0f6bcbcf047c09d34` | `b62a5f4b376fbd5994ef811f80d652e7c6e53f54` |
| Folders | `e8de71406a54030fd0048ec180f08936b74b6fe6` | `f48886763a1b57b30f7767deb9909f741dc812f3` |
| Memories | `ff0978d6c38c083ac410830dd2e2f9ffe7d6c4cc` | `3e7dcb504b3d816366427f53be51df187f837376` |
| Platform | `dc83a208930b95f1a0164229bebc8359024f2e48` | `9ead86f9061695bf3e30bcb0e020c0b830ddaf7e` |
| Projects | `3364aceb4e257b3bf60097dffee210476c68458d` | `c978a5e86c8134ba09a166e093a1a4d6b4db77a8` |
| Tenants | `2cf3a9660f71f6877923772cd145aa6856907945` | `ec2d21a93cc8591eabdba17b2004dfcc6482b6e3` |

These checkouts were individually clean, but differed from the root's recorded
gitlinks. Latest inspected Platform still records Builds `5808d3b1…`, and both
accepted pins remain 4.35.2. The Builds delta changes validation's environment
descriptor support, source-mapping materialization and CI/provenance handling;
the preparation/action paths still exist. Re-capture the actual workspace and
reassess affected tests before implementation. Preserve all external work; do
not update the index, gitlinks or checkouts to make the plan's initial tuple
current. Any resulting native checkout/index failures remain real failures.

The current Platform checkout has no locally available `v<SemVer>` release tag
pointing at its HEAD. `PlatformIdentityResolver` reports HXW011 when required
package-mode provenance is missing. This is an observed local identity gap,
not proof that no remote tag exists. Do not create a tag or fetch/update the
workspace to conceal it. Source mode may warn; package mode requires verified
release identity. Matching two current gitlinks alone does not qualify a tuple.

Retain accepted prerequisite/adoption packages, tooling v1 package/schema/trust,
historical records, raw observations, both frozen hosts, accepted MinimalModule
sources, tool pins, `.gitmodules`, all twelve root gitlinks and dependency
checkouts. Preserve the committed mapping-test attributes rule. Shared executable
changes listed below are proposed intentionally; their old v1 source hashes are
not rewritten. No Conversations, EventStore or Tenants runtime behavior changes.

## First implementation slice

The bounded deliverable is a truthful producer qualification report plus an
explicit v2 route that stops at the existing entry gate. It does not promise an
accepted published successor, all-platform qualification, owner approval or a
green deployment pipeline. Missing external gates remain recorded BLOCKED.

1. Re-read current owning guidance and capture the actual implementation-entry
   revisions, dirty paths and exact protected inventory. Preserve the prior
   investigation artifacts. Run and inspect the existing AppHost using the
   required baseline below before code edits. If the baseline cannot become
   healthy, retain its exact blocker and stop code edits; no offline exception
   or runtime configuration change is inferred.
2. Execute existing producer preparation/action and manifest/provenance tests.
   Record selected SDK, actual assembly/source identities, exact commands,
   bounded durations, exits, raw stdout/stderr, native reports and source-mode
   warnings. Use one fresh attempt directory under
   `module/validation/epic-16-tooling-qualification-v2/` with local byte-preserving
   attributes; do not change the root attributes merely for observations.
3. Build the new package policy, contracts and consumer route described below.
   Preserve v1 data/trust and all measurement obligations. No upstream runtime
   correction is included in this first slice; a reproduced producer defect
   becomes an exact owner handoff rather than an unreviewed lifecycle change.
4. Run focused faults and affected regressions, then all six v1 and six v2 Story
   16.1 commands. Retain failed attempts separately. Update current guidance with
   measured outcomes and external qualification requirements. Keep the runtime
   draft/backlog, null entry, empty trust and hold.

## Required AppHost baseline

Target only
`src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`.
The frozen MinimalModule AppHost is a different fixture. Use the retained local
harness without changing its source, topology, identity or frozen configuration.
The normal baseline is a Development/Debug observation, not AD-5/AD-6,
production parity or durable tenant authorization qualification.

Planning checked Aspire CLI `13.6.1+3751e615ad660621f2a7aa4c390ef48e08a0eed8`
and its `run`, `describe`, `wait`, `ps` and `stop` help without launching resources.
At implementation entry, verify the installed command shape again, inspect
existing sessions and inherit the complete producer-selected source/Debug
mapping environment. If bootstrapping that mapping is necessary, building the
existing producer and preparing temporary imports are allowed setup before code
edits; accepted tool pins and dependency roots remain unchanged. Do not use stale
`--no-build` output to bypass mapping or build failures.

From Conversations under that environment, capture these commands and results:

```bash
aspire ps --format Json --non-interactive
aspire run --detach --isolated --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json --non-interactive
aspire describe --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --include-hidden --format Json --non-interactive
aspire wait conversations --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --status healthy --timeout 120 --non-interactive
```

The detached `run` command satisfies the baseline's AppHost-run requirement
without blocking the terminal. Isolation protects concurrent sessions; missing
isolated credentials or prerequisites remain blockers, not permission to insert
test credentials or bypass authentication. Record the created process/run
identity, actual resources/endpoints and safe diagnostics. Inspect all required
resources, including the gateway and Admin Web, then capture final describe/log
state. A healthy Conversations process alone cannot establish a healthy topology.
Use the discovered resource names for additional bounded waits.

Only after a healthy baseline may this slice change tooling sources. On failure,
record the exact command/exit and resource/diagnostic result and stop before
those edits; focused diagnostics do not turn baseline failure into PASS. Do not
emit test commands/events to create durable tenant evidence as part of baseline
inspection. Preserve the runtime hold and single-replica disclosure.

Clean up only the baseline instance created by this slice after its work, using
the verified exact AppHost/session target. Recheck ownership if other instances
appear; an ambiguous target does not authorize stopping another session. No
`--all`, forced cleanup or volume deletion is part of this plan.

## Producer capability and command plan

Existing implementation, relative to `references/Hexalith.Builds`:

- `src/libraries/Hexalith.Builds.Tooling/Workspace/BuildPreparationService.cs`
  validates the declaration, resolves a mapping and atomically owns a fresh
  output directory. It handles cancellation and removes its owned output.
- `src/libraries/Hexalith.Builds.Module.Cli/ModuleCommandApplication.cs`
  already exposes `prepare-build`; do not add another command or runner.
- `Github/prepare-module-build/prepare_module_build.py` verifies exact package
  mode, five contained generated imports, mapping hash and Release selection
  before exporting environment values. It never evaluates producer text as
  shell. Existing workflow placement is part of its tests.
- `test/Hexalith.Builds.Module.Tests/Workspace/BuildPreparationTests.cs`,
  `PlatformManifestCommandTests.cs`, `PlatformAdoptionValidationTests.cs` and
  `Workspace/PlatformIdentityTests.cs` cover the bounded source surface.

From Builds, build the individual test project in Debug:

```bash
dotnet build test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Debug --nologo -m:1
```

Invoke that built assembly with each class separately, writing each TRX to a
fresh absolute temporary path. The preparation command is:

```bash
dotnet test/Hexalith.Builds.Module.Tests/bin/Debug/net10.0/Hexalith.Builds.Module.Tests.dll -automated sync -failSkips -class Hexalith.Builds.ModuleTool.Tests.Workspace.BuildPreparationTests -trx /tmp/epic16-preparation-fresh.trx
python3 -m unittest discover -s Github/prepare-module-build/tests
```

Use the same native command with exact classes
`Hexalith.Builds.ModuleTool.Tests.PlatformManifestCommandTests`,
`Hexalith.Builds.ModuleTool.Tests.PlatformAdoptionValidationTests` and
`Hexalith.Builds.ModuleTool.Tests.Workspace.PlatformIdentityTests`, each with its
own fresh report. Record actual counts; do not copy prior counts. Preparation
tests include controlled source/package mapping evaluation, invalid declaration,
occupied/symlink output, provenance and cancellation. Controlled package fixture
tags are test inputs, not the real Platform's acceptance.

For actual source-mode preparation, invoke the built Debug CLI from the consumer
root against its declaration, with `--mode source`, `--output json` and a
nonexistent `--workspace` under a fresh existing temporary parent. Pass the
complete returned producer environment to a focused MSBuild evaluation of the
active consumer; verify selected Debug mode, import paths, mapping hash and
project identities. Never hand-set dependency roots or accept only selection
flags. Preflight that every required direct dependency is already initialized,
at its recorded commit and free of initialized nested dependencies. Otherwise
stop this actual-workspace probe; do not let the resolver initialize dependencies
as part of this slice. Compare workspace state afterward.

All process calls need explicit time/output bounds and cancellation/descendant
cleanup. Use focused first-failure fallback and existing environment-only pins
when justified; record exact changed command flags. Run no broad historical
39-minute lane. The user-selected AppHost baseline above is required; other
live topology tests remain separately scoped qualification and cannot be
inferred from that baseline.

## Retained producer release-review gates

Source of truth is Builds
`docs/module-cli-configuration-key-release-review.md` and its exact triage at
`evidence/g4/module-configuration-key-release-20261025/resumed-20261025T101341Z/review/triage.json`.
Keep all thirteen findings, ten deferred groups and the two rejected findings;
independently relate current evidence to each group. No blanket closed status is
permitted from passing preparation tests.

Paths in this table are relative to Builds; runtime implementations are under
`src/libraries/Hexalith.Builds.Tooling/Runtime/`, tests under
`test/Hexalith.Builds.Module.Tests/` unless the full CLI path is stated.

| Group | Specific qualification target and remaining boundary |
| --- | --- |
| B1 | `ModuleCommandExecutionService.cs`; `LocalRunLifecycleTests.cs` and `LocalRunFollowupTests.cs`: owner-check/launch versus completed cleanup interleaving |
| B2 | Retained timeout-cause regression in lifecycle/review tests; prove first timeout survives later owner cancellation |
| B3/B4 | `CompositionRunResourceScanner.cs`, native discovery/lifecycle tests: inaccessible process metadata or incomplete Linux `/proc` must not become verified absence |
| B5 | `CompositionRunListing.cs`, `CompositionRunListingTests.cs`, `LocalRunReviewCorrectionTests.cs`: forged persisted identity/resource data must not leak in either format |
| B6 | CLI `src/libraries/Hexalith.Builds.Module.Cli/ModuleCommandApplication.cs` and `CompositionRunListing.cs`: public list cancellation lacks dedicated coverage/structured handling; preparation cancellation is a different path |
| B7 | Resource-claim admission versus read-size boundary; verify the retained 67,001-byte receipt case using current claim-store tests |
| B9 | `CompositionDocumentStore.cs`, `CompositionRunStateStore.cs`: owner must state the machine-crash durability contract; atomic replacement and round trips alone do not settle it |
| B10/G1 | `CompositionNativeProcessDiscoveryTests.NativeTaggedChildRemovalPreservesIndependentPeerAsync`: execute on Windows and macOS; Linux explicitly skips and earns no platform credit |
| E1 | `ModuleCommandExecutionService.cs`, `TestEnvironmentProvisioningReviewTests.cs`: later descriptor refusal must not reuse first-entry environment/catalog bindings |
| B8/B11 | Preserve the recorded rejected dispositions and rationale; do not reopen them without new evidence |

A needed correction must be scoped in Builds before modification, with its
own guidance, exact regression and source/artifact provenance. For B6, reuse
existing `ExecuteValidationAsync` cancellation semantics (`cancelled`,
HXC130, cancelled category/exit and a noncancelled output token); do not add a
generic exception mask. This plan identifies that route but does not authorize
unbounded native lifecycle repair. External platform tests and B9 owner policy
remain explicit qualification inputs.

## Additive tooling v2 design and footprint

Add the following root-owned files:

- `_bmad-output/planning-artifacts/epic-16-tooling-v2/README.md`,
  `binding.json`, and `story-contracts/{16.1,16.2,16.3}.json`.
- `_bmad/schemas/epic-16-tooling-v2.schema.json`.
- `_bmad/scripts/epic16_trust_v2.py`, `_bmad/scripts/epic16_packages.py`.
- `_bmad/scripts/tests/test_epic16_tooling_v2.py`.

Explicitly amend these shared executable entry points:

- `_bmad/scripts/epic16_tooling.py`.
- `_bmad/scripts/generate_story_record.py`.
- `_bmad/scripts/verify_story_16_1.py`, `verify_story_16_2.py`,
  `verify_story_16_3.py`.

Use an immutable package descriptor selected only by exact independently
allowlisted contract paths. Pass it through validation, schema lookup, source
inventory, executable verification, Git binding, measurement and generation.
Preserve default/explicit v1 behavior and the public helpers used by existing
tests. No production mutation of v1 globals, path-prefix guessing, ambient
latest-version selection or fallback after a selected-package failure.

Keep v1's declared source-row inventory unchanged. Load the new v2 policy/trust
modules lazily only for an exact v2 selection; otherwise adding imports or
inventory rows could replace the required old SOURCE_DRIFT result with an
earlier inventory/dependency failure. Each selected route must verify its own
complete executing closure. Cover this ordering with actual v1 CLI calls and
the existing positive v1 fixtures, not merely a mocked selector.

Each v2 contract names its exact corresponding tooling v1 contract as predecessor
and embeds the identical `preservedContract`. Pin preserved v1 carrier/schema
bytes independently in the new trust module; those historical bindings are
preserved bytes, not requests to validate old source hashes against today's
checkout. Preserve 18 ordered ACs, 32 requirement identities, graph 38/61,
predecessor order, rollback, native capture, nine-product ownership, five-upstream
API comparison, fault controls, test identities and unchanged final-record wire
rules. Commands select the new contract explicitly.

Select schemas and every import from the same descriptor throughout an invocation.
Capture/check the complete selected executing-source closure and actual loaded
locations, including both new modules. Extend declared-import handling narrowly;
an internal import bug must still surface rather than masquerade as unavailable
qualification. Legacy V9/V14 generation remains lazy and standalone.

Avoid digest cycles by finalizing contracts/schema first, then independent
contract/schema/predecessor pins, then executable bytes, then binding source
hashes. The new binding excludes itself from its source rows; full binding bytes
are separately bound during candidate evaluation. Do not pin the binding digest
inside executable source that the binding itself hashes. Freeze exact path/row
inventories and reject duplicates, omissions, additions and self-inclusion.

Keep v2 entry null, trusted entries empty, ACTIVE hold and all four flags false.
Keep existing native/runtime/final output paths, since contract digests isolate
their identities; tests for different packages use separate disposable fixtures.
Never overwrite a tracked or accepted output. A caller-supplied owner JSON or
fixture trust cannot activate either runtime route.

## Verification matrix for the implementation

From Conversations, first run the new focused suite, then affected existing
consumer/record/adoption suites. The following are planned future commands, not
test results from preparing this plan:

```bash
uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling_v2.py
uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_generate_story_record.py _bmad/scripts/tests/test_verify_epic_16_adoption.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py
git diff --check
```

Add independent faults for altered contract/schema/predecessor/source pins,
source-row set drift, unknown/wrong-story selectors, cross-version mixing,
executing-source substitution, self-nominated entry, unsafe/aliased outputs and
genuinely unavailable versus internally broken imports. Include positive
controls proving guards execute. Reuse native metadata/contained readers and
existing fault-restoration assertions; do not relax evidence strength.

Exercise all fifteen native fixture routes and all three deterministic final
records through actual entry points, always fixture-only and
`runtimeQualified=false`. Run existing affected legacy-dispatch tests. Execute
both exact Story 7.3 guards from their current contract, preserving their output
rules and existing observations.

Finally run the six exact commands in the preserved v1 Story 16.1 contract and
the six commands in the new v2 contract. Require v1 FAIL/exit 1/SOURCE_DRIFT on
`.gitattributes`, v2 BLOCKED/exit 2/ENTRY_QUALIFICATION_PENDING, nonempty ledgers,
zero measured runtime tests, and all five runtime receipts plus both final pair
paths absent. Do not substitute a fixture run for either actual held route.

Required implementation tests must pass before completing this slice. Package,
platform and owner qualification that could not be measured remains BLOCKED in
its separate ledger; it cannot be counted as passing tests or overall tool
acceptance. Runtime publication and lifecycle advancement do not apply to the
prerequisite slice. Its scoped source-integrity checks remain applicable.

## Published artifact and owner handoff after this slice

No successor version is inferred. Use only an actual exact producer-CI package
with matching source/release inventory. The producer's
`Tools/test-g4-tool-package-contracts.ps1 -Version <actual-version> -RequireControls -RetainPackageDirectory`
is a CI Release lane; local Debug packing cannot replace it. CLI packing
explicitly requires matched Release AppHost/EventStoreHost/UiHost outputs. Do
not reuse older host binaries or build local Release assets for this plan.

Before accepting any candidate, independently retain actual package/version,
feed archive/hash/size/signature, NuSpec repository revision, source/tag and CI
identity, packaged/loaded assembly identities, loose/embedded schema and catalog,
and the exact Platform release/source/Builds pairing. Restore only into isolated
tool/cache/configuration paths; accepted manifests stay unchanged. Apply ordinary
human/JSON validation, both adoption formats, all 36 malformed-key controls,
preparation/action export tests and the full retained review-gate inventory to
that exact artifact. Preserve any native failure and incompatible identity.

Retain all ten current adoption failure identities with explicit owner scope:
declaration/tool, six historical source captures, accepted MinimalModule fixture,
and two frozen host/direct-consumer projects. A different candidate can change
the observed set; report exact additions/removals and reasons, never fabricate
ten failures or suppress scanner scope. Archive migration, fixture amendment,
hosting decisions and selector fixes are separately reviewed work.

Actual owner acceptance must bind the release/source/catalog tuple, any supported
published testing-helper strategy, source/package/deployed parity, operational
envelope, recovery/operator/capacity disposition and human SM-2. Neither package
success nor this v2 route supplies those decisions. Pin alignment, publication,
remote Git operations and trusted runtime entry require their own concrete
reviewed actions. No deployment or messages to owners are part of this plan.
