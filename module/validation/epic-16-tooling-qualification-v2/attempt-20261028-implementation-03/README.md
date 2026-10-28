# Bounded producer-source qualification and held tooling v2

This attempt implements the separately approved tooling prerequisite at root
`ef01d578a00efd527bae9226dca302f54cd012f0`, with Builds
`81ab049208678bd6616828824729c0ddac02f7d1` and Platform
`5889400b50e6e2cefb4ed6976ca53d086d3989e2`. Root CLI 4.38.1 and
Platform CLI 4.35.2 remain unchanged. No dependency checkout, root gitlink,
accepted package, frozen host, runtime code, entry trust or authority was changed.

## Measured scope

| Check | Result and limits |
| --- | --- |
| Fresh mapped AppHost baseline | PASS: Development/Debug Conversations, EventStore, Admin Web and security reached Running/Healthy; exact isolated instance stopped; final sessions empty. Readiness alone supplies no functional/runtime qualification. |
| Actual source preparation | PASS: complete seven-value producer environment, Debug/project-reference selection, four expected AppHost project references and mapping hash verified. HXW011 absent local Platform release tag and HXW010 bootstrap source-version range warnings remain. |
| Producer preparation native tests | PASS 11/11. Controlled package-mode fixtures are test inputs, not acceptance of a real Platform/package tuple. |
| Producer manifest native tests | PASS 75/75. |
| Producer adoption native tests | Initial 180-second timeout retained; fresh bounded retry PASS 396/396 in 413.607 seconds. |
| Producer identity native tests | PASS 43/43. |
| Producer action export tests | PASS 9/9. |
| Focused tooling v2 suite | Final PASS 60/60, including all fifteen native routes, three deterministic final records, independent v1 source guards, selected-package capture faults and scoped-schema autocrlf checkout. Earlier passing runs are retained separately. |
| Affected current tooling/record/adoption suites | PASS: tooling 2,885/2,885; current generator 1,516/1,516; adoption v1 131/131; adoption v2 162/162. Complete command streams and generator/adoption JUnit reports are retained. |
| Story 7.3 guards | Both exact commands PASS in an isolated local copy of the identical committed workflow inputs; pre-existing root observation files preserved. |
| Six actual v1 Story 16.1 commands | FAIL/exit 1/SOURCE_DRIFT first on `.config/dotnet-tools.json`; independent faults also prove rejection of `.gitattributes` drift. V1 data/schema/trust/binding bytes remain fixed. |
| Six actual v2 Story 16.1 commands | BLOCKED/exit 2/ENTRY_QUALIFICATION_PENDING. Every result has a nonempty ledger. |
| Runtime measurement and outputs | Zero runtime tests; all five runtime receipt paths and both final-record paths absent after every actual held command. |

The fifteen native fixtures and all three deterministic final records executed
through actual selected verifier/generator entrypoints in disposable repositories.
Fixture outputs remain isolated and `runtimeQualified=false`. Positive controls
and byte-identical restoration accompany faults for independent contract/schema/
predecessor/source pins, exact row inventories, unknown/wrong-story selectors,
cross-version evidence, executing bytes/loaded locations, owner nomination,
unsafe/aliased outputs and unavailable versus internally broken imports.
The first focused attempt's four failures are retained as a separate summary;
its original terminal stream was not separately captured. The fixes establish
the synthetic warning baseline and nonempty generator rejection ledgers. A later
58-test focused run passed the selected-policy/trust symlink and unavailable
capture controls. The final 60-test run passed, additionally binding scoped schema LF
attributes and verifying an autocrlf checkout; see its separate retained result.


The first explicit generator run failed with 14 failures, 64 setup errors and
1,455 passes. Current Story 10.3/10.4 helper failures came from the unmapped
Debug helper build; the identical build succeeds with the complete producer
source environment. The helper and its assertions were not changed. The final
directory-selected generator run applies the repository's unmodified conftest
retirement hook, selecting 1,516 cases and excluding exactly 17 live Story 11.3
cases. Its JUnit identities exactly equal the frozen selection and every row
passes. The 26 retained warnings concern the existing record_property/xunit2
combination. No passing subset was inferred from progress dots.

The initial adoption runs failed because their fixtures copied live root files
that had already advanced beyond accepted pins. The additive
`epic16-adoption-root-sources.json` snapshot supplies three historic C# files and
two v2 artifacts from exact committed history; each decoded blob must match the
unchanged checker pins. Existing snapshots, carriers, root C# files and tool
manifest remain unchanged. [adoption-fixture-provenance.json](adoption-fixture-provenance.json)
independently recomputes the five historic blobs and recorded source revisions.

Raw index bytes changed during validation. Exact staged mode/object/path rows
and the initial HEAD tree remain unchanged, independently confirmed by the
parent audit. No initial raw index copy was retained, so the changed fields are
not identified and no cache-only or byte-identical-index claim is made. No index
was restored. [generator-selection-audit.json](generator-selection-audit.json),
[matrix-audit.json](matrix-audit.json), [implementation-summary.json](implementation-summary.json)
and [artifact-index.json](artifact-index.json) provide the final scoped audit.

## Retained qualification boundaries

[qualification-ledger.json](qualification-ledger.json) preserves all thirteen
original findings, ten deferred groups and the rejected B8/B11 dispositions,
with current source-file hashes and a distinct qualification boundary for each.
Its explicit `producerRepository` is `references/Hexalith.Builds`; all
`sourceReview.path` and `currentEvidence[].path` values resolve relative to that
producer repository, not the Conversations root.
Passing preparation does not exercise or close native cleanup/launch races,
timeout attribution, Linux/non-Linux process-discovery uncertainty, forged
persisted listings, public list cancellation, the 67,001-byte claim-bound issue,
machine-crash durability, Windows/macOS native-host coverage or later batch
stale bindings. No upstream correction was made in this slice.

Published producer-CI package identity, actual archive/hash/size/signature,
NuSpec source/tag/CI provenance, packaged and loaded assemblies, loose/embedded
schema/catalog and the exact Platform release/source/Builds pairing remain
BLOCKED. All 36 malformed-key controls must still run against that actual
artifact. Local Debug source tests cannot substitute for the producer's matched
Release host/package lane or a supported published testing-helper strategy.
Source/package/deployed provider parity, operational envelope, recovery/operator/
capacity decisions, human SM-2 and named owner entry also remain BLOCKED.

The earlier adoption observation's six PASS/twelve FAIL rows and exact twelve
failure identities remain undispositioned. This attempt neither reruns that
workspace scan with untracked new sources nor suppresses archived/generated
inputs. A later tracked candidate must report its exact actual scanner inventory.

## Identity, streams and preservation

[entry-state.json](entry-state.json) captures root HEAD/index/status and all twelve
clean direct checkout/index tuples, with no initialized nested dependencies.
[source-preparation-inventory.json](source-preparation-inventory.json) binds the
complete returned mapping and generated import bytes.
[qualified-source-inventory.json](qualified-source-inventory.json) binds a fresh
post-test preparation/evaluation probe and exact producer assembly hashes measured
before and after that probe, which remained unchanged.
[native-test-summary.json](native-test-summary.json) derives actual counts from
retained TRX reports. [command-index.json](command-index.json) identifies exact
commands, working-directory records, explicit time/output limits, exits and
separate retained stdout/stderr hashes. Timed-out process groups receive bounded
termination and descendant cleanup. The explicit generator-file invocation also opted into 17 already-retired live Story 11.3 cases; its FAIL remains a separate reproduction, not current acceptance.

[resource-readiness.json](resource-readiness.json) reports every observed resource;
final logs and describe snapshots retain bounded diagnostic limitations.
Dashboard/telemetry credential values were replaced in retained diagnostic
streams; raw capture hashes and the additional replacement ledger remain in
[safe-diagnostic-redactions.json](safe-diagnostic-redactions.json). Ownership
metadata retains the actual created process identities without the dashboard token.

[preservation-and-integrity.json](preservation-and-integrity.json) checks all 194
protected bytes, unchanged HEAD and exact staged mode/object/path inventory, all twelve direct checkout tuples,
stream/import integrity, exact selected mapping, cleanup, absent runtime outputs
and whitespace. Earlier observation files and the existing user draft/context
edits were preserved. The runtime draft was updated only in its unfrozen
prerequisite sections; its status remains draft and backlog is unchanged.

Tooling v2 keeps entry null, independent trusted entries empty, ACTIVE hold and
all four authority flags false. Runtime implementation, publication, deployment,
release, pin alignment and owner acceptance are outside this prerequisite result.
