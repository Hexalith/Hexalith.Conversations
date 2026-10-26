# Epic 16 current adoption preparation for owner review

This additive refresh binds current sources at preparation HEAD
`8925c9fef46026d53d15478e2a744b3604c38a98`, accepted Platform CLI **4.35.2**,
and the exact immutable [adoption v1 predecessor](../epic-16-adoption-v1/package.json).
The prerequisite/adoption v1 packages, schemas, checkers, observations, signed
evidence, manifest and hosting inventory retain their original bytes.
The implementation hold stays **ACTIVE**. `executionAllowed`,
`ownerApprovalClaimed`, `releaseAuthorized` and `pushAuthorized` remain literal
false. Story 16.1 stays draft/backlog, with runtime entry pending.

From a fresh checkout, install the lockfile-pinned Python environment, then run
the read-only check and focused fault suites from the repository root:

```bash
uv sync --frozen
uv run --frozen --no-sync python3 _bmad/scripts/verify_epic_16_adoption_v2.py --repository .
uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py _bmad/scripts/tests/test_verify_epic_16_adoption.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py
```

Exit **0** means structural PASS, **1** means proven malformation/drift (FAIL),
and **2** means a named required input or secure-read facility is unavailable
(BLOCKED). The nonempty assertion ledger is distinct from `entryReadiness=BLOCKED`.
Scoped evidence is **not-applicable to runtime publication**: preparation
publishes no runtime receipts or final records. Historical authority publishers
and story lifecycle transitions are not invoked.

The verifier uses the neutral descriptor-relative no-follow reader, bounded
stable regular-file reads, strict JSON rejecting duplicate keys and nonfinite
numbers, independently pinned schema/source bytes, exact ordered inventories
and full predecessor comparisons. It never reads package-selected paths or
executes commands. It has no arbitrary output writer. Unsupported secure reads
block without a path-open fallback. Rehashing weakened wrappers, observations,
inventories, graph relations or authority claims cannot authorize them.

## Current native observations

The accepted direct Platform tool entry was merged exactly, preserving unrelated
tools and disabled roll-forward. Its restored version is
`4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`. Every command below was
measured read-only; new outputs exist only under
[`module/validation/epic-16-adoption-v2/`](../../../module/validation/epic-16-adoption-v2/results.json).

| Command | Exit | Recorded outcome |
| --- | --- | --- |
| `dotnet tool restore` | 0 | Exact accepted pin restored |
| `dotnet tool run hexalith-module --version` | 0 | Full restored identity above |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json` | 1 | UsageOrManifest; two HXP002 token fields |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption` | 1 | All 24 readable rows retained |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption --output json` | 1 | Six PASS, eighteen FAIL |

The independently pinned current direct Builds schema accepts the unchanged
manifest and SDK `APP_API_TOKEN` key. **Published accepted CLI 4.35.2 still rejects
that key** at `modules[0].integration.secrets[1].configurationKey` and
`modules[0].runtime.extensions[0].configurationInputs[12].key`. Source-schema
compatibility has an upstream successor; published-tool compatibility remains
**BLOCKED**, and no native declaration/adoption success is claimed.

The eighteen failed rows comprise declaration failure, fourteen dependency-selector
findings and three Dapr findings. All exact paths, reasons and diagnostics are in
the [JSON output](../../../module/validation/epic-16-adoption-v2/adoption.json)
and [readable output](../../../module/validation/epic-16-adoption-v2/adoption-text.txt).
The three additional selector findings relative to v1 name `Directory.Packages.props`,
`tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`,
and `tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props`.
Each failing row is reported as a FAIL entry blocker independently of structural
consistency. No scanner finding is waived or converted to PASS.

[results.json](../../../module/validation/epic-16-adoption-v2/results.json)
binds exact commands, exits, stdout/stderr and their hashes, UTC start times,
restored version and provenance. The observation-time
[provenance](../../../module/validation/epic-16-adoption-v2/provenance.json)
records HEAD, working-tree status, twelve unchanged direct root gitlink/checkouts,
and the 1,047 tracked supported-source candidates with byte hashes and path-list
digest. The inventory contains sorted repository-relative paths for tracked
root-owned `.cs`, `.csproj`, `.props`, `.targets`, `.ps1`, `.sh` and `.py` files,
plus `appsettings*.json`, `appsettings*.yaml` and `appsettings*.yml` candidates.
`trackedSourcePathDigest` is SHA-256 of those path strings joined with LF and a
trailing LF, encoded as UTF-8. Each row's `sha256` hashes the file's exact bytes;
`size` is its byte length. These are scan candidates, subject to the native
scanner's supported file types, 1 MiB limit and frozen-host Dapr exclusion. The
candidate inventory therefore includes files a particular native audit may
filter out; it does not assert that every row was scanned. New files are untracked
because staging is unauthorized. The scan precedes the completed v2 additions;
it does not certify all new Python or unsupported/generated constructs. Owners
must rerun both modes on the intended reviewed/tracked candidate. Runtime was
not started and human SM-2 was not performed.

## Exact preserved obligations and remaining decisions

The three v2 carriers change only their wrapper schema identity and future
command paths. Their full inherited `preservedContract`, source bindings,
rollback/final-record paths and required **6/6/0/0/0/0** summary remain equal to
v1. All eighteen ordered scenarios and exact Given/When/Then/legacy commands,
thirteen legacy IDs, thirty-two requirements and coverage relations survive.
The graph retains all 38 nodes and 61 edges, including
`7.4, IR-0 -> 16.1 -> 16.2 -> 16.3` and four dependents of 16.3. AD-5/6/7/8 and
the AD-8 replacement for AC-16.2-02 remain unchanged; observation-time equality
is not restored.

All eighteen `futureCommands` are unavailable, unexecuted interface contracts.
The current generator still supports only V9/V14, and no successor dispatch,
fifteen measured scenario verifiers, runtime receipts or final records are added.
Planning Tooling must implement and qualify owner-approved successor consumption
and completion guards before execution. Story 16.3 retains its production
`project/v2/reconcile` and persisted end-state/query obligations.

All seven original pending owner decisions and questions remain unchanged.
Platform remains the selected infrastructure owner. Source/Debug,
package/Release and hosted parity, identities/providers, AD-5 operational and
recovery/operator envelopes, capacity/expiring waivers, AD-6 durable semantics
and human SM-2 remain unqualified. Only Release accepts applicable waivers.
Tenants still needs its `configureOptions` binding for injected pub/sub/topic;
Data Protection still needs SDK registration and restart/key-ring qualification;
`Dapr:DeclarativeSubscriptions=true` still needs protected delivery, dead-letter
and absent-discovery evidence. A separate owner-reviewed entry decision is required.

Both AppHosts remain frozen. Before Story 16.3 the baseline owner with
Architect/Runtime must decide exemption or relocation; retirement also requires
producer adoption, owner-reviewed source/package/deployed parity, migrated
consumers and applicable EventStore AD-22 exact-subject removal authority or an
explicit owner-reviewed not-applicable disposition. This package grants none.

## Historical reproduction and review scope

The real v1 checkers retain their pins and truthfully FAIL the current tree:
ten original pinned sources drifted before this refresh. Historical v1 claims
apply only to their original snapshots. The adoption tests now load exact
pinned historical root/tool bytes and the existing portable reference snapshot;
their original positive and fault assertions run without history or initialized
references. V2 tests use a separate current reference snapshot. Neither fixture
uses live references to populate its repository or weakens the real checker.

[package.json](package.json) excludes itself and mutable specs from artifact
fingerprints. Its 25 artifacts include the independently pinned root
`.gitattributes` policy, which preserves canonical LF inputs and raw native/CRLF
bytes across automatic checkout conversion. Its 30-path review boundary covers
that policy, the accepted tool entry,
new v2 package/schema/checker/tests/observations/current reference fixture,
historical root fixture and v1 fixture repair, enrollment README, runtime draft
pointer and this preparation spec. It contains no runtime, gitlink, dependency,
host or historical-evidence mutation. Pre-existing runtime draft edits are
preserved. Final review must independently inspect the actual Git diff and
untracked files; the package cannot prove working-tree ownership from its own
allowlist. No commit, staging, push or submodule update is authorized here.
