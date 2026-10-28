# Published CLI/Platform qualification — measured, runtime held

The final [collection-04 ledger](collection-04/qualification-ledger.json) is
**FAIL / exit 1** because adoption remains **six PASS / sixteen FAIL** and the available Platform
Builds gitlink disagrees with its documented accepted catalog/tag source. This
attempt supplies a measured 4.38.1 artifact handoff, with an unaccepted Platform
pair and pending owner gates. It grants no runtime, publication or deployment
authority. The implementation-entry source is Conversations
`c9c55d5a4033bfd7b22a4cf2298467b8e3549dac`; existing uncommitted planning work is
preserved. The separate prerequisite spec controls this observation.

## Actual artifact and producer identity

| Item | Measured identity/result |
| --- | --- |
| Exact candidate | `Hexalith.Builds.Module.Cli` 4.38.1; disabled root roll-forward |
| Source/tag | `v4.38.1` → `16ff70a2321ad576faaac3fe53088c2f1bdacae3` |
| Successful push CI / Release | [40589495278](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40589495278) / [40595436967](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40595436967) |
| Published producer inventory SHA-256 | `5c11240a48feb2811620da1a23b81a7f82aad6dab61daa88f8d8cd10f73512a6` |
| Signed feed archive SHA-256 / bytes | `f5710df04af88744a48e73f51155dbe41c1e91489ca7cac92d2bb7f53948dff3` / 37,561,135 |
| Qualified unsigned CI archive SHA-256 / bytes | `d263c1f77c187a6f9b6ec11af124b77976db18e3884036fba4bfdb107b2edcda` / 37,548,049 |
| Feed SHA-512/size and NuGet signature | PASS; native verification exit 0 |
| NuSpecs, CLI, packaged/loaded Tooling | PASS; exact version/source identities agree; raw NuSpec BOM difference retained |
| Three loose/embedded/source schemas | PASS; independent hashes agree |
| Embedded catalog / source Props | PASS; actual EventStore selection 3.128.3, FrontComposer 4.7.0; all selected fields compared |
| Ordinary human / JSON validation | PASS / PASS; native exits 0 / 0 |
| Malformed keys | PASS: all 36 exact-field HXP002 controls, native exits 1; 18 retained inputs remain unchanged |

[Identity](collection-04/identity.json), [native command arrays and stream hashes](collection-04/commands.json),
[unsigned reconstruction](collection-04/unsigned-reconstruction-proof.json) and
[complete payload entries](collection-04/published-payload-entries.json) keep signed
and unsigned ZIP identities separate. A temporary **Debug** helper uses the
selected SDK's public NuGet signature-removal API, leaves the signed original
unchanged, reproduces the **full** unsigned CI archive hash and size, and compares
every uncompressed payload entry. It performs no producer Release compilation or
publication. An independent earlier reconstruction is retained separately in
`independent-unsigned-proof/`.

The exact producer inventory selects 289 visible fixture files. The
[fixture proof](collection-04/fixture-scope.json) compares every source-bound
filtered hash/size and ordered manifest digest; it records the hidden
`package-consumer/.config/dotnet-tools.json` exclusion and all raw-Git versus
filtered-checkout differences. No claim of all-tracked-fixture coverage is made.
The complete producer Release log archive and
[native summary](collection-04/producer-native-summary.json) retain both the CI
trial and actual-version full lanes. Each reports Evidence 182/182 passed,
Module 2,239 total / 2,235 passed / 4 skipped, and Integration 13 total / 1 passed /
12 skipped. Every skip remains a skip. Linux execution grants no Windows/macOS
or opt-in live qualification.

## Adoption and remaining Platform/owner gates

The [exact ordered comparison and sixteen owner dispositions](collection-04/adoption-dispositions.json)
match the entry inventory with no additions, removals or reason changes. All
sixteen failures remain unaccepted: one tool-pin, twelve dependency-selector and
three Dapr rows. The four implementation-03 generated-import rows and every
predecessor failure are preserved. Every row includes its failure bytes, class,
owner scope, evidence, proposed correction, pending acceptance and next action.
Archive/fixture classification is not a waiver. No scanner exclusion or source,
fixture, host or pin repair is included.

Actual published package preparation was attempted. Its
[native stdout](collection-04/package-preparation.stdout.txt) returns
**HXW011 / exit 2**: direct Platform
`5889400b50e6e2cefb4ed6976ca53d086d3989e2` has no local release tag. The
[preparation/action record](collection-04/package-preparation-action.json) therefore
keeps complete package/Release mapping and action export **BLOCKED**. No source
mapping or local Debug test substitutes for this gate. The
[Platform observation](collection-04/platform-pair.json) retains its exact 4.35.2
pin, range, recorded Builds gitlink and independently resolved documented 4.35.2
catalog/tag source. Range eligibility supplies no integration-qualified tuple or
registration acceptance.
The available Platform Builds gitlink `81ab049208678bd6616828824729c0ddac02f7d1`
disagrees with documented accepted catalog/tag source
`4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`; that independent measured gate is
**FAIL/PLATFORM_CATALOG_BINDING_MISMATCH**, separately from missing-tag BLOCKED.

[All thirteen producer findings](collection-04/producer-review-dispositions.json)
remain visible in ten deferred groups, with B8/B11 retaining their rejected
not-applicable dispositions. Candidate source hashes bind the real artifact
source. [Supplemental review byte boundaries](review-source-byte-boundaries.json)
separate raw Git bytes from checkout bytes: all 22 filtered candidate-source rows
match the earlier inspected checkout identities. The raw-versus-filtered hash
difference is not a source-code-change claim. Native launch/cleanup, first timeout,
Linux/non-Linux discovery uncertainty, forged listing, public list cancellation,
the 67,001-byte claim boundary, crash durability and batch stale bindings remain
exact owner/finding gates. Broad native or preparation success closes none.

Named tuple/registration acceptance, Windows/macOS hosts, supported published
testing helpers, source/package/deployed provider parity, AD-5, recovery/operator/
capacity and human SM-2 stay **BLOCKED**. Existing runbooks and the SM-2 checklist
are unchanged. No owner messages were sent.

## Baseline, preservation and verification

The [fresh normal source/Debug baseline](baseline-result.json) ran before collector
source edits, using the complete freshly prepared producer mapping. Conversations,
EventStore gateway, Admin Web, security and Dapr infrastructure became healthy.
Only the exact created isolated AppHost/session was stopped; final Aspire `ps`
was empty. Baseline readiness grants no runtime entry or tenant-delivery proof.
Generated local credentials were removed from safe diagnostic streams; original
native stream hashes and retained-stream hashes remain distinct in command
metadata and the [redaction record](baseline-safe-diagnostic-redactions.json).

`collection-01/` retains the first complete artifact/control observation.
`collection-02/` adds exact unsigned reconstruction, package preparation/action,
catalog/Props comparison and candidate-bound review dispositions. `collection-03/`
also reports the available Platform catalog/gitlink disagreement as a distinct
FAIL, separately from its missing-release-tag BLOCKED gate.
`collection-04/` binds the final network-worker implementation to fresh real-feed
transfers and repeats all exact artifact/control measurements. Blocking HTTP
open/read runs in an owned worker; the parent enforces the whole remaining
deadline, stream/body caps and cancellation, and records incomplete transfers as
BLOCKED. Real loopback tests prove successful transfer, continuous trickling
against both deadline kinds, byte-cap rejection and cancellation with worker
reaping. The completed earlier collections remain unchanged. Earlier empty
producer log responses, later successful raw log captures, exact collector source
snapshots and test attempts are retained separately. No observation was rewritten
to manufacture a successful qualification verdict.

The [held matrix](held-command-matrix-v2.json) reruns both six-command Story 16.1
routes: v1 is **FAIL/SOURCE_DRIFT/exit 1** and v2 is
**BLOCKED/ENTRY_QUALIFICATION_PENDING/exit 2**. It measures zero runtime tests and
confirms all seven runtime/final outputs remain absent. Entry stays null,
independent trust empty, Story 16.1 draft/backlog and the hold ACTIVE, with
`executionAllowed`, `ownerApprovalClaimed`, `releaseAuthorized` and
`pushAuthorized` all false. All twelve direct checkout tuples, raw index rows,
pins, dependencies, accepted bindings/fixtures and frozen hosts are preserved.
Historical authority publication/replay is not-applicable.

Run the focused collector faults with:

```text
uv run --frozen --no-sync python3 -m pytest -q tests/tooling/test_epic16_published_artifact_qualification.py
```

All 31 focused collector fault tests pass. Final integrity, changed-path and
focused-test records accompany this handoff.
