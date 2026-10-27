# Story 16.1 entry investigation — 2026-10-27

Investigated clean `main` at `26298b31eb4d362f88e008246ac3acb160128330`.
The checkout advanced externally during initial inspection, through
`cec9a1e1bbdf7e8efb2cb3aa299add7f52c2a6ef` and
`26298b31eb4d362f88e008246ac3acb160128330`; all fresh command observations below
use the latter candidate. No working-tree changes were present at measurement
entry. This report is planning material, not runtime proof or owner acceptance.

## Current source state

The separate [enrollment remediation](spec-epic-16-enrollment-adoption-remediation.md)
is done. Its source/composition tests remain retained prior results, not fresh
tests run here. Current `.github/workflows/ci.yml` passes `module-manifest` to
the reusable Builds workflow and invokes `Github/prepare-module-build` before
affected direct builds. Current root-declared Builds source also invokes its
workflow-owned preparation action in core, integration and Aspire lanes.

The existing producer implementation is
`references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Workspace/BuildPreparationService.cs`,
with CLI composition in
`references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/ModuleCommandApplication.cs`.
Reuse those implementations and their
`test/Hexalith.Builds.Module.Tests/Workspace/BuildPreparationTests.cs` and
`Github/prepare-module-build/tests/` coverage. They do not need duplicate
implementation in Conversations. The action prepares package-mode imports
without starting resources; source availability alone does not prove that an
accepted published artifact includes it or that CI passed.

Both root and direct Platform tool manifests still pin **4.35.2**, with
roll-forward disabled. The restored binary identifies itself as
`4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`. Its root help lists no
`prepare-build`; invoking that command returns HXC001/exit 1. The producer
action's README explicitly requires separately qualified publication, an exact
accepted tool pin and a compatible Platform/tag/catalog tuple. The older
isolated 4.38.0 observations do not establish inclusion of the newer preparation
implementation. No version, package or release is selected by this report.

## Verification

[results.json](../../module/validation/epic-16-entry-2026-10-27/results.json)
retains all twelve exact command invocations, exit codes, durations, separate
raw stdout/stderr files with recomputable SHA-256 hashes, source candidate/tree,
and all twelve raw root gitlinks with matching checkout revisions. The
`prepare-build` invocation used a disposable temporary workspace and launched
no resources. Its output is a usage failure, not a preparation receipt.

| Invocation | Observed result |
| --- | --- |
| `dotnet tool run hexalith-module --version` | Exit 0; accepted 4.35.2 identity above |
| `dotnet tool run hexalith-module -- prepare-build --help` | Exit 0; root help only, no preparation command listed |
| `dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode package --workspace <temporary-path> --output json` | Exit 1; Usage/HXC001 |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json` | Exit 1; UsageOrManifest/HXP002 on both existing token-key fields |
| Same validation with `--adoption --output json` | Exit 1; six PASS and ten FAIL rows |
| Same validation with `--adoption` | Exit 1; same six PASS and ten FAIL identities |
| Five verifier commands and final generator command from `epic-16-tooling-v1/story-contracts/16.1.json` | Each FAIL/exit 1/SOURCE_DRIFT on `.gitattributes`; one failing assertion, zero measured native tests |

The ten adoption failures remain exactly the declaration, six retained source
captures, accepted MinimalModule fixture, and two frozen host/direct-consumer
projects listed in [the owner handoff](../../module/platform-handoff.md).
Source remediation is not undone, and no failed identity is exempted.

The six story commands stop in `validate_package` before `entry_evidence`.
Recomputing all source rows in `epic-16-tooling-v1/binding.json` finds exactly
one mismatch:

| Source | Accepted SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `.gitattributes` | `d743202ab8c0bdf3dcd000a4a101acde352b6e6cf305a49b708b23c6e7029d7d` | `709f3cfe412d0b9f4ca8ccec9ff2c404568643fa4635fad9eb99124eed2508f0` |

Commit `cec9a1e1bbdf7e8efb2cb3aa299add7f52c2a6ef` added the mapping test's
`whitespace=cr-at-eol` attributes rule. Preserve that committed change and the
accepted predecessor; do not remove the rule or replace a frozen digest merely
to produce a different result. Additive source-binding refresh and its consumer
changes need separately reviewed scope. Previous BLOCKED/exit 2 observations
remain unchanged and do not describe this candidate's measured failure.

## Remaining qualifications and proposed next scope

The runtime binding still has `entry: null`; the consuming
`_bmad/scripts/epic16_tooling.py` still has `TRUSTED_ENTRIES = {}`. The nine owner
qualifications remain: `platformTool`, `adoption`, `operationalEnvelope`,
`providerParity`, `recoveryDrill`, `operatorCoverage`, `capacityWaiver`, `sm2`,
and `ownerEntry`. No draft approval or static test can supply them.

The next useful prerequisite plan would cover:

1. Qualify the existing producer preparation and action using their focused
   tests and an isolated exact candidate artifact. Retain source/package mapping,
   token-key, malformed-input, cancellation, discovery and export observations;
   identify which results still require a published release or live owner proof.
2. Design an additive successor source binding and consuming verifier/tests for
   the legitimate committed attributes change. Keep accepted carriers and exact
   obligations intact; retain null entry, empty trust and all false authority
   flags. The current FAIL remains in this observation directory.
3. Present the exact release/source/catalog alignment and all ten excluded
   finding dispositions for separate owner acceptance. Do not update a tool pin,
   dependency gitlink, frozen host, accepted fixture or retained capture by
   inference. A publication or alignment decision must name actual artifacts.

Alternatively, validate already supplied owner-qualified records against these
requirements. None were found in the current binding. Architect/Runtime and
Release must still complete the operational envelope and provider/recovery/
operator/capacity disposition. Human active-checkout breakpoint/edit evidence,
readiness/stop/cleanup and SM-2 acceptance remain pending.

After qualified entry, retain the [runtime owner-seam map](story-16-1-entry-investigation-2026-10-26.md#code-map-and-implementation-order-after-entry):
EventStore owns generic CAS/recovery/acknowledgement, Tenants owns coherent
durable transitions/checkpoints, and Conversations consumes one snapshot per
authorization. `IntegrationTests/Epic16/Story16_1EvidenceTests.cs` must measure
real loaded-product behavior and persisted end-state. Runtime work remains held.

## Scope and preservation

This investigation refreshes the runtime draft's unfrozen planning sections and
current module guidance, and adds this report plus fresh raw observations.
Frozen intent, draft/backlog, tool pins, root gitlinks/checkouts, runtime source,
accepted bindings, fixtures and prior observations remain unchanged. No runtime
receipt or final JSON/Markdown pair was created. No native runtime test,
AppHost launch, staging, commit, push, dependency update or release occurred.

Runtime evidence publication is **not-applicable** to this planning-only work.
The measured source-integrity and adoption **FAIL** results remain FAIL, and
runtime qualification remains **BLOCKED**. These are distinct dispositions; no
review/completion transition is made while the gates fail.

Planning verification passed: all 24 raw stream hashes recompute, both adoption
formats have exactly the same sixteen rows, and the ten failing identities equal
the handoff inventory. All six failure ledgers match the reported source and
code. All seven contracted runtime output paths remain absent. Frozen intent,
sprint bytes, every bound source, all twelve clean dependency checkouts and the
index equal the measured candidate. `git diff --check` passes. The runtime draft
is 1,533 tokens with `o200k_base`, within the workflow's proposed scope range.
