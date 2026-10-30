# Exact 4.38.3 candidate qualification and held binding refresh

This supports the [draft spec](spec-epic-16-4-38-3-qualification-and-binding-refresh.md).
The user selected preparation on 2026-10-30. Implementation is a separate approval.
There are no migrations, external state mutations or runtime API changes in this
prerequisite. Its footprint is root-owned collector/tooling/tests, an additive
package, local observations and handoff documentation.

## Current entry and independent inputs

The workspace advanced during investigation through the user's external
`/pushall` commit. Use current root
`f2b75d0dd45547d5b40c6b676fd1c17f695f917b`, tree
`217f8d6a1aa67cbb940f8b2ad7e0da758f10d69e`; earlier observations retain their
original candidates. The [input inventory](epic-16-4-38-3-qualification-inputs.json)
records all twelve direct gitlinks/checkouts and exact inspected source hashes.
All direct checkouts matched their recorded revisions and were clean.

| Subject | Observed identity |
| --- | --- |
| Root tool pin | 4.38.2, roll-forward false |
| Prepared candidate / Platform pin | 4.38.3, roll-forward false |
| Root Builds | `96dc22928af914ed22d44789090dd2f65c9e598e` |
| Root Platform | `2030542f8e7e25f97b2ee106e8074dedeefda9f5` |
| Platform's recorded Builds | `cc0db51b800be1d0d5badc3780395cfb5953d0f5` |
| Exact published Builds source, resolved from local `v4.38.3` | `d1931b1e803144048d927fff16cb8119afde541e` |
| Signed archive SHA-256 pinned by Platform | `6c074186a9bbfb66c558fe5d1090e1e35fdd58577c0396266074bd1cea8919fd` |
| Tooling DLL SHA-256 pinned by Platform | `4e307577ae4fb02104914951a9905a1c9dd453447a80735443d24fbc83496823` |

The hashes above are inspected producer declarations, not fresh package proof.
Independently retrieve and verify the actual bytes. No retained official 4.38.3
inventory was found in the inspected predecessor evidence. The available exact
release asset is named `g4-tool-package-inventory.json`; discover its asset ID
and digest through the release API rather than guessing a predecessor carrier.

Platform's `eng/prepare_builds_artifacts.py`, `eng/run_tuple_ci.py` and
`.github/workflows/tuple-ci.yml` require the recorded Builds source to equal
`d1931b1e803144048d927fff16cb8119afde541e`. Current recorded source differs.
Record this proven inequality as FAIL, separately from unavailable preflight
or owner acceptance as BLOCKED. Do not change Platform's nested gitlink.
The 82-test preflight does not grant candidate acceptance even when it passes;
generated/direct-host integration and authenticated owner decisions remain separate.
Read available owner results only; do not dispatch this workflow or reproduce
its Release execution locally.

The last retained root-tool adoption observation has 27 rows: six PASS and
twenty-one FAIL. Its exact predecessor is
`module/validation/epic-16-published-artifact-qualification/entry-20261030-story161-01/adoption-json.stdout.txt`.
Do not copy those counts onto 4.38.3. That observation predates the external
push; all additions, removals, duplicates and reason changes must be measured.

## Setup and collector changes after approval

1. Recapture actual implementation-entry root/dependency revisions, dirty paths,
   index digest and protected inputs. Preserve existing work and accepted
   bindings, manifests, archives, fixtures, hosts and observations. Refuse output
   aliases and existing observation paths. Inspect only direct dependencies;
   never initialize/update nested dependencies or repair checkouts.
2. Before source edits, follow the established
   [normal mapped Debug baseline](epic-16-tooling-qualification-and-binding-refresh-plan.md#required-apphost-baseline).
   Verify current Aspire help, sessions and producer-selected source environment.
   Target the existing Conversations AppHost, run isolated/detached, inspect every
   required resource and retain bounded diagnostics. Stop only the created exact
   instance. A failed baseline stops code edits; no credentials, topology bypass,
   offline exception or tenant-event seeding is included. This is local readiness,
   not registered module run/debug or AD-5/AD-6 qualification.
3. Extend the existing collector with `--candidate-only`, `--planning-inputs`,
   `--adoption-predecessor` and `--tooling-contract`. Preserve legacy defaults.
   Candidate-only mode allows the declared candidate to differ from the active
   root pin, but records that difference as an unaccepted adoption fact. Its
   entry gate still verifies contained owning roots, clean/pinned direct
   dependencies, protected inputs and complete isolated artifact prerequisites.
   No candidate executable runs before provenance, signature, payload and archive
   verification succeed. Root pin mismatch cannot hide unrelated identity errors.
4. Replace the obsolete `catalog commit is` README parser with independent
   prepared-source/recorded-source/accepted-evidence observations. A prepared
   pin, range, README or artifact-helper receipt is not an acceptance record.
   Missing acceptance is BLOCKED; available contradictory identities are FAIL.
   Read actual qualificationEvidence carriers from the release assets/CI artifacts
   identified by their inventories. Require exact file/hash/size identity;
   missing declared bytes remain BLOCKED, regardless of broad native summaries.

Reuse `FreshOutput`, `Runner`, `ContainedReader` and existing output guards.
Retain the 1800-second overall deadline, 300-second network/restore limits,
60-second validation and 600-second diagnostic bounds, 8 MiB streams,
256 MiB archives, 2 GiB expansion and bounded traversal. Preserve cancellation,
same-group descendant cleanup, HTTP framing, finalization-time restoration,
signature classification and strict diagnostic multiplicity from prior fixes.

## Actual candidate collection

Discover exact release/source CI identities with bounded, read-only commands:

```bash
gh api repos/Hexalith/Hexalith.Builds/releases/tags/v4.38.3
gh api 'repos/Hexalith/Hexalith.Builds/actions/runs?head_sha=d1931b1e803144048d927fff16cb8119afde541e&per_page=100'
```

Retain the actual signed feed archive, signature verification, feed SHA-512,
size and SHA-256; producer inventory/run identities; NuSpec source; loaded and
packaged assemblies; loose/embedded/source schemas; embedded catalog versus
source Props; and exact signed/unsigned payload relation. Obtain the producer's
matched Release package evidence rather than compiling local Release hosts.
Retain fixture inventories, native results and all skips with their real scopes.

Restore the candidate into fresh explicit tool/NuGet/configuration/CLI-home
directories. The active installation and root manifest stay unchanged. Downloaded
assets may be retained under a fresh attempt below the existing qualification
root. Resolve the official inventory to `<attempt>/g4-tool-package-inventory.json`
only after asset identity/size/digest checks. Then the planned collector invocation
is the existing interface plus the new explicit selectors:

```text
python3 _bmad/scripts/qualify_epic16_published_artifact.py
  --conversations . --builds references/Hexalith.Builds --platform references/Hexalith.Platform
  --version 4.38.3 --release-sha d1931b1e803144048d927fff16cb8119afde541e
  --inventory <attempt>/g4-tool-package-inventory.json --output <fresh-collection>
  --candidate-only
  --planning-inputs _bmad-output/implementation-artifacts/epic-16-4-38-3-qualification-inputs.json
  --adoption-predecessor module/validation/epic-16-published-artifact-qualification/entry-20261030-story161-01/adoption-json.stdout.txt
  --tooling-contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.1.json
```

`<attempt>` and `<fresh-collection>` are newly allocated paths, not reusable
literal destinations. Initial collection protects the existing v2 package;
after v3 exists, a fresh final-code collection selects the exact v3 path instead.
Recapture implementation-entry identities and record differences from these
planning inputs; the planning manifest is historical context, not a demand that
legitimate approved source edits retain their old hashes.

Run both real-manifest positive formats and all 36 exact malformed-key controls
from the predecessor plan. Preserve exact-field HXP002, one diagnostic, exit 1
and unchanged input bytes. Run both actual adoption formats and compare ordered
rows with the named predecessor; retain each failure's class, owner, evidence,
next action and acceptance state. Historical/package configuration classifications
do not waive failure. Run package preparation/action export against this exact
candidate; missing tagged Platform identity or supported Release mapping remains
BLOCKED. Do not fabricate tags or substitute source mode.

Carry all thirteen retained producer review findings, ten groups and rejected
B8/B11 dispositions, matching current source/artifact evidence separately.
Native Windows/macOS, launch/cleanup, first-timeout attribution, process discovery,
forged listing, list cancellation, 67,001-byte claims, crash policy and later-batch
binding gates need their exact evidence. No upstream lifecycle fix is included.

## Additive v3 and execution order

Create the six files under `_bmad-output/planning-artifacts/epic-16-tooling-v3/`:
README, binding and the three story contracts, plus an immutable candidate
observation summary binding retained qualification-ledger/identity/stream digests.
It records measured failures and pending acceptance; it provides no entry authority.
Add `_bmad/schemas/epic-16-tooling-v3.schema.json`,
`_bmad/scripts/epic16_packages_v3.py`, `epic16_trust_v3.py` and
`_bmad/scripts/tests/test_epic16_tooling_v3.py`.

Each contract names its corresponding v2 predecessor. Preserve identical
`preservedContract` and `nativeProtocol`: 18 ACs, 32 requirements, graph 38/61,
predecessor order, rollback, nine product owners, five upstream API comparisons,
fault controls, native identities and final-record wire rules. Keep runtime/native
output paths and refuse tracked/existing accepted outputs.

Extend `PackagePolicy` selection in `epic16_tooling.py` through three exact v3
contract paths and lazy imports. Make binding-schema handling explicit for v3.
Extend successor routing and optional-import declarations in
`generate_story_record.py` and all three `verify_story_16_*.py` entry points.
No latest-version selection or fallback is permitted. Preserve the accepted
v2 policy/trust/schema/carrier bytes; shared consumer edits make its current-tree
source drift legitimate, while historical positive behavior is tested in
disposable candidate-bound fixtures.

Finalize contracts/schema, independent contract/schema/predecessor pins,
executable bytes and candidate summary, then source hashes. Freeze the complete
executing closure, selected policy/trust and summary sources. Exclude the binding
from its own rows; bind its full bytes separately during candidate evaluation.
Reject missing/additional/duplicate rows and self-inclusion. A final read-only
collector run can protect the finished package without being included in that
package's source digest; keep mutable handoff records outside the digest.

V3 binds the actual unchanged 4.38.2 root manifest separately from the isolated
4.38.3 candidate. It is a held verification successor, not pin adoption. Entry
stays null, entry trust empty, hold ACTIVE and all four authority flags false.

## Validation and bounded completion

Add caller-level faults for candidate/root separation, false prepared acceptance,
foreign source/archive/catalog, missing or substituted qualification evidence,
adoption delta multiplicity and all existing collector preservation/bounds.
For v3 cover exact selection, wrong story, cross-version mixing, every independent
pin, executing-module substitution, source-row drift, unsafe outputs, self-nominated
entry, absent versus internally broken optional imports and byte-exact restoration.

Planned focused commands from Conversations:

```bash
uv run --frozen --no-sync python3 -m pytest -q -ra -p no:cacheprovider tests/tooling/test_epic16_published_artifact_qualification.py _bmad/scripts/tests/test_epic16_tooling_v3.py
uv run --frozen --no-sync python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_epic16_tooling_v2.py _bmad/scripts/tests/test_generate_story_record.py _bmad/scripts/tests/test_verify_epic_16_adoption.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py
git diff --check
```

Use the actual fixture CLIs for all fifteen native routes, three deterministic
final pairs and inserted-record verification. Historical v1/v2 fixtures preserve
their historical strength; current-tree tests truthfully expect source drift.
Run all eighteen selected v3 runtime commands and the six Story 16.1 commands
for each preserved legacy version. Require v3 BLOCKED/2/ENTRY_QUALIFICATION_PENDING,
legacy FAIL/1/SOURCE_DRIFT, nonempty ledgers and zero runtime tests. Preflight all
output paths, preserve sentinels in isolated fault fixtures and retain all actual
runtime/final paths absent. Rehash every retained stream and the exact source,
carrier, adoption and changed-path inventories.

Before review/completion run the independent Story 7.3 guards:

```bash
python3 _bmad/scripts/verify_story_completion_workflows.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --scenario AC-7.3-01 --output artifacts/v9/7.3/AC-7.3-01.json
python3 _bmad/scripts/verify_story_completion_workflows.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --scenario AC-7.3-02 --output artifacts/v9/7.3/AC-7.3-02.json
```

The bounded deliverable completes only with passing implementation tests,
actual artifact identity and all 36 controls, truthful dispositions and passing
held-route/preservation checks. Missing artifact/control evidence blocks that
completion. The overall qualification may remain FAIL/BLOCKED because Platform,
adoption or external owners remain unaccepted; preserve those states explicitly.
Update `module/platform-handoff.md` and the runtime draft with that distinction.
No runtime story lifecycle transition or final-record publication occurs.

Root pin alignment, Platform's recorded-source correction/publication,
authenticated candidate acceptance, generated/direct-host qualification,
registration, AD-5/provider/recovery/operator/capacity, supported published helper
strategy, human SM-2 and trusted owner entry remain separate owner actions.
Future adoption changes need their own source-binding decision. Historical
evidence-boundary replay and runtime publication are not-applicable here.
