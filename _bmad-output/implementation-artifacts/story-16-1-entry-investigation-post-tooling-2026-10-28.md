# Story 16.1 entry investigation after tooling completion — 2026-10-28

Investigated clean Conversations `main` at
`c9c55d5a4033bfd7b22a4cf2298467b8e3549dac`, tree
`56a572699adbcdc850e92a3ecfe626ecb09e87aa`. The separate
[tooling prerequisite](spec-epic-16-tooling-qualification-and-binding-refresh.md)
is done. The [earlier investigation](story-16-1-entry-investigation-2026-10-28.md)
predates that completion; its absent-v2 and twelve-failure observations remain
historical. This report refreshes planning facts without granting runtime entry.

## Verification

[Fresh captures](../../module/validation/epic-16-entry-2026-10-28-post-tooling-01/results.json)
retain all fifteen exact argument arrays, working directory, timestamps,
durations, exits and separate stdout/stderr sizes and SHA-256 hashes. Commands
were bounded to 60 seconds and monitored against an 8 MiB per-stream limit;
none reached either bound. Limits were checked by polling, not an atomic output
cap. Capture occurred outside the repository before additive retention here.

| Exact command family | Result |
| --- | --- |
| `dotnet tool run hexalith-module --version` | PASS/exit 0: `4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3` |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json` | PASS/exit 0: `validated: Success` |
| Same validation with `--adoption --output json` | FAIL/exit 1, HXA001; six passing and sixteen failing rows |
| All six exact `epic-16-tooling-v1/story-contracts/16.1.json` commands | FAIL/exit 1/SOURCE_DRIFT, first on `.config/dotnet-tools.json` |
| All six exact `epic-16-tooling-v2/story-contracts/16.1.json` commands | BLOCKED/exit 2/ENTRY_QUALIFICATION_PENDING |

All twelve verifier/generator results have nonempty assertion ledgers, zero
measured runtime tests, `runtimeQualified=false`, ACTIVE hold and four false
authority flags. Their result envelopes contain `candidate: null` because
rejection precedes runtime candidate binding; the capture records the complete
invoked root revision independently. Every command left the five runtime
receipt paths and both final-record paths absent. Runtime publication is
**not-applicable**, distinct from the **BLOCKED** entry result.

[Source recomputation](../../module/validation/epic-16-entry-2026-10-28-post-tooling-01/source-bindings.json)
used the existing contained reader. All **81 v2 rows match**. V1 has seven
mismatches among 71 rows: the tool manifest, root attributes and five shared
executables (`epic16_tooling.py`, `generate_story_record.py` and the three
`verify_story_16_*.py` entrypoints). The added executable mismatches follow the
completed additive tooling work; v1 remains preserved history, not a refresh
target. Both binding documents retain null entry and false authority.

[Adoption delta](../../module/validation/epic-16-entry-2026-10-28-post-tooling-01/adoption-delta.json)
compares exact failure identities and reasons with the earlier observation.
All twelve previous failures remain; four dependency-selector findings are
added, all below
`module/validation/epic-16-tooling-qualification-v2/attempt-20261028-implementation-03/`:

- `prepared-imports/source-mapping/SourceMapping.late.props`
- `prepared-imports/source-mapping/SourceMapping.values.props`
- `qualification-prepared-imports/source-mapping/SourceMapping.late.props`
- `qualification-prepared-imports/source-mapping/SourceMapping.values.props`

The complete failure inventory is one tool-pin, twelve dependency-selector and
three Dapr findings. These are retained producer-generated imports and source
captures, accepted fixtures and frozen host consumers. Removing archives,
excluding scanner paths, changing accepted fixtures or retiring hosts is not
authorized by this investigation. Root CLI remains 4.38.1 and direct Platform
remains 4.35.2; declaration success does not qualify or align that pair.

The prior mapped Debug readiness/cleanup and completed prerequisite's native,
action and Python tests remain separately scoped retained evidence. They were
not rerun here, and no AppHost was started. Resource readiness does not supply
durable authorization, a published package or operational acceptance.

## Entry requirements and next scope

`_bmad/scripts/epic16_tooling.py:entry_evidence` requires independently trusted
entry bytes, exact story identity, a full ancestor authorization revision,
explicit execution/owner authority and hashed predecessor files for `7.4` and
`IR-0`. Nine qualifications must each be PASS: `platformTool`, `adoption`,
`operationalEnvelope`, `providerParity`, `recoveryDrill`, `operatorCoverage`,
`capacityWaiver`, `sm2` and `ownerEntry`. The selected v2 binding's `entry` is
null and `_bmad/scripts/epic16_trust_v2.py:TRUSTED_ENTRIES` is empty. Another
approval message cannot manufacture those evidence facts or authorize caller
JSON as trust.

The pending handoff in `module/platform-handoff.md` and the prerequisite's
qualification ledger require the actual producer-CI archive/hash/size/signature,
NuSpec source/tag/CI identity, packaged/loaded assemblies, embedded/loose
schema/catalog and exact Platform release/source/Builds tuple. All 36 malformed-key
controls must run against that artifact. Native cleanup, launch, timeout,
process-discovery, persisted-listing, cancellation, claim-bound, crash durability,
Windows/macOS and batch-stale-binding qualification remain open. Local Debug
sources cannot substitute for the matched Release package lane or supported
published testing-helper strategy.

`production-operational-envelope-v1.md` remains an Architect/Runtime owner-review
draft; provider/environment parity, recovery/operator coverage and Release-owned
capacity/waiver disposition are pending. `module/sm-2-checklist.md` still requires
observed human active-checkout breakpoint/edit and named acceptance evidence.
AD-5 keeps runtime entry held until the envelope gate passes.

**Open scope choice:** prepare a separate published-artifact/Platform tuple
qualification and adoption-disposition plan, or validate existing qualified
owner records supplied by the user. The completed tooling prerequisite does
not need to be resumed or reapproved. Neither proposed next scope itself lifts
runtime entry. No deployment, destructive migration or external data mutation
is proposed. Eventual runtime implementation spans the owning EventStore,
Tenants and Conversations repositories; its detailed source map remains in
the earlier investigation and runtime draft.

## Preservation and planning checks

The capture compared complete staged mode/object/path rows and all twelve
direct checkout HEAD/status tuples before and after diagnostics, with exact
equality. It also compared HEAD/tree, clean status and all protected source
hashes. The retained entry state stores a digest/count of the full staged
inventory; raw index-byte equality is not claimed. No dependency was initialized,
updated or edited; no staging, commit, push or lifecycle transition occurred.

Only this report, a fresh observation directory and the runtime draft's unfrozen
planning sections are intended changes. Frozen intent, draft/backlog, accepted
evidence, entry trust, tool pins, runtime sources and root gitlinks remain
preserved. Planning validation recomputes every retained stream digest, checks
all twelve result ledgers, exact adoption delta, v2 source equality, output
absence, frozen-block equality and the final changed-path boundary. Native
product tests and historical evidence-boundary reproduction do not apply to
these planning edits. FAIL and BLOCKED halt runtime review/completion.
