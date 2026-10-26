# Epic 16 successor adoption candidate for owner review

This additive package is a preparation artifact. It preserves the immutable
[prerequisite draft](../epic-16-prerequisites-v1/epic.md) and accepted history.
The implementation hold stays **ACTIVE**; `executionAllowed`,
`ownerApprovalClaimed`, `releaseAuthorized` and `pushAuthorized` remain false.
Story 16.1's draft status and sprint/runtime state are untouched by this
preparation. Pre-existing edits to its draft content are preserved.

Run the read-only structural checks from the repository root:

```bash
python3 _bmad/scripts/verify_epic_16_prerequisites.py --repository .
python3 _bmad/scripts/verify_epic_16_adoption.py --repository .
uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py _bmad/scripts/tests/test_verify_epic_16_adoption.py
```

Both checkers return JSON to stdout. Structural exit 0 is PASS, 1 is proven
malformation/drift (FAIL), 2 is unavailable required input (BLOCKED). A structural
PASS is separate from `entryReadiness=BLOCKED`. Every result contains a nonempty
assertion ledger and fixed false authority flags. No manifest-supplied command
runs, and the adoption checker has no output-path writer. It reads only fixed,
contained repository inputs through descriptor-relative no-follow traversal,
bounded stable reads, duplicate-key/non-finite-number rejection, strict schemas,
recomputed hashes and independent source pins. It does not launch the accepted
CLI, runtime, historical authority tools or final-record generator. Unsupported secure-open environments report BLOCKED
without a path-open fallback. The unchanged prerequisite verifier consumes these
securely captured byte snapshots through an isolated read adapter.

## Preserved executable obligations

The three `story-contracts/*.json` carriers retain the draft's full contract in
`preservedContract`, including the eighteen ordered scenario IDs, exact
Given/When/Then assertions and legacy commands, thirteen legacy inventory IDs,
thirty-two complete requirement coverage relations, predecessor graph, unchanged
rollback and final-record paths/required `6/6/0/0/0/0` summary. The copied graph
preserves all 38 nodes and 61 edges, including `7.4, IR-0 -> 16.1 -> 16.2 -> 16.3`
and the four downstream dependents of 16.3. AD-5/6/7/8 assertions and the explicit
AD-8 replacement for AC-16.2-02 are retained exactly; whole-document observation
time equality is not restored.

`futureCommands` specifies all eighteen exact successor commands and intended
outputs. These are **future interface contracts, unavailable and unexecuted**.
The first five commands per story name a future `verify_story_16_N.py` interface
with `--contract`, exact scenario, candidate and receipt. The sixth names the
canonical generator with this successor carrier and unchanged final targets.
No legacy command is adopted implicitly. No test or runtime receipt is fabricated.

The current generator accepts only V9 and V14 contract schemas. It cannot consume
these candidate wrappers, just as it cannot consume the earlier draft wrappers.
The candidate schema/checker establish preparation consistency only; they do not
add generator dispatch, scenario verifiers, publication support or a completion
gate for these wrappers. Before execution, Planning Tooling must implement and
test an owner-approved successor contract/schema adapter with the generator and
Story 7.3 completion guard, and all fifteen measured scenario verifiers with
nonempty candidate-bound result/fault ledgers. Required final-record evidence
and denominators stay unchanged. Runtime proofs remain not-run.

## Enrollment and review boundary

[package.json](package.json) binds the candidate artifacts, enrollment inputs,
accepted CLI outputs, immutable draft sources, generator and prerequisite
checker. Its own bytes are excluded from its artifact inventory. The checker
independently pins source semantics, the schema, declaration, inventory and
observed CLI snapshots; rehashing a weakened candidate cannot authorize it.
The report compares exact inventories and relations rather than accepting a
subset. The package is a snapshot for this review, not a new routine-main gate
or an evergreen certification of later checkouts.

The exact additive review boundary is `package.json`'s `reviewBoundary`: the neutral
`contained_reader.py` helper and portable reference-input test snapshot, root tool
pin, `module/` declaration/inventory/checklist/documentation/static observations,
this package, its schema/checker/tests, and the mutable preparation spec. No
runtime, source-mapping, dependency, gitlink, host, historical-draft, sprint,
accepted-evidence or authority-publication edit is part of this implementation.
Pre-existing edits to the original Story 16.1 spec are outside this change's
review boundary and are preserved. The checker validates package
bindings; final review independently inspects the actual Git diff and untracked
files against this scope. It does not claim to derive working-tree ownership
from a self-authored allowlist.

The [enrollment README](../../../module/README.md) links exact accepted 4.35.1
ordinary/adoption commands and raw results. Ordinary manifest validation and
both adoption modes fail: accepted 4.35.1 rejects the SDK APP_API_TOKEN key with
two HXP002 field diagnostics. Adoption retains six PASS and fifteen FAIL rows,
including declaration failure and the fourteen existing HXA001 source findings.
The upstream schema/tool format fix is a separate BLOCKED adoption prerequisite.
No alias, accepted-pin change or successful native declaration validation is claimed. The tool's
bounded audit scans tracked source. New additive files are untracked during this
preparation because staging is unauthorized. Owner integration must rerun the
checks and inspect unsupported/generated constructs; no full post-integration
scan coverage is claimed.

## Entry decisions still required

All seven original decisions remain pending with their owners and questions
unchanged. Infrastructure selection is resolved in favor of Hexalith.Platform;
qualification of this enrollment and its injected bindings is pending. Platform
and Conversations must qualify source/Debug, package/Release and hosted parity,
identity, provider/configuration and recovery/operator envelopes; only Release
may accept scoped and expiring parity/capacity waivers. SM-2 requires actual
human breakpoint/edit and cleanup evidence, and remains unverified. Source
mapping and Dapr subscription/component migration are separate future work.
Tenants injected PubSubName/TopicName still require the SDK configureOptions
delegate; Data Protection settings still require AddEventStoreDataProtection
registration; Dapr:DeclarativeSubscriptions must resolve to true and its protected
route/dead-letter/discovery effects must be qualified. These remain explicit
BLOCKED entry requirements, without runtime edits.

Both inventory AppHosts remain frozen, with actual consumers and all producer,
source/package/deployed parity and EventStore AD-22 exact-subject removal gates
pending. The baseline owner with Architect/Runtime must decide exemption or
relocation before Story 16.3. The fixture cannot make that decision. Story 16.3
still requires the live production `project/v2/reconcile` route and persisted
end-state/query proof.

Release with Planning Tooling, Architecture, Quality, EventStore, Tenants and
Conversations owners must approve successor adoption, complete prerequisite
qualification and record a separately scoped entry decision. No approval, hold
lift, SM-2 completion, hosted qualification, runtime scenario PASS, generated
record or publication is claimed here. Scoped evidence is **not-applicable to
runtime publication**, because this change publishes no runtime receipts or
final records. It does not restart retired V19–V29 authority workflows.

The new fault suite uses a checked-in reference-input snapshot, verified against
independent source digests, so it runs without initialized submodules. It never
reads live reference checkouts to build fixtures. The older prerequisite test
suite retains its pre-existing live-reference fixture dependency; this preparation
does not modify or conceal that limitation.
