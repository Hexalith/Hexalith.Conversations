# Evidence-Boundary Validation

Use this runbook for changes to planning authority, manifests, generated
records, signed or release-facing evidence, evidence readers, or the tests and
workflows that govern those artifacts. It is reusable guidance; the mechanical
verifier and acceptance tests remain the enforcement boundary.

## Invariants

1. Recompute every declared source hash from the named source bytes.
2. Prove every evidence path is repository-relative, contained by the resolved
   repository root, and present at the candidate being evaluated.
3. Recompute the signable payload from canonical manifest rows; never trust a
   declared payload or payload digest.
4. Compare the final changed-path boundary using exact set equality. Report
   both missing and unexpected paths; containment is not sufficient.
5. Derive submodule gitlinks from raw Git mode `160000`, never from text
   matching or directory shape.
6. Require asserted inventory row identities to equal the frozen source
   inventory exactly, including duplicates, omissions, and additions.
7. Pin roots of trust in consuming test source so an artifact cannot redefine
   the authority used to validate itself.
8. Treat unavailable history as `BLOCKED` or a visible skip according to the
   governing contract, never as `PASS` or `not-applicable`.
9. Require a nonempty evaluated assertion ledger for every applicable change.
   Zero evaluated assertions cannot pass.

The mechanical result states are distinct: `PASS`, `FAIL`, `BLOCKED`, and
`not-applicable`. Only a valid, recorded `not-applicable` result may continue
without evaluated assertions. A missing result is not `not-applicable`.

## Authoring

- Use the neutral TestSupport helpers instead of local Git runners,
  repository-root walkers, hash helpers, manifest parsers, or assertion
  ledgers.
- Freeze path and identity inventories at story entry and record the canonical
  NFC UTF-8 LF list digest.
- Keep authority identities, candidates, schemas, exact commands, exit/result
  semantics, stable blocker codes, and output digests explicit.
- Generate projections from committed canonical blobs. Exclude a bundle from
  its own digest and keep mutable assessment/decision records outside that
  digest.
- Preserve accepted and signed evidence. Publish additive successors rather
  than rewriting history.

### Development workflow

- Use TestSupport helpers rather than local Git, root, or hash
  implementations.
- Run the verifier before every applicable review or done transition and before
  unattended finalization.
- Treat `BLOCKED` as blocked, never as pass or `not-applicable`.
- Keep roots of trust in consuming test source.
- Freeze inventories at story entry and compare exact sets.
- Record stable blocker codes. Never weaken an assertion merely to finish a
  story.

### Review workflow

Reviewers must answer all of these questions from independently inspected
source and evidence:

- Are declared hashes and the signable payload independently recomputed?
- Can every path be proven inside the repository?
- Is the changed-file boundary exact, including missing and unexpected paths?
- Are gitlinks derived from raw modes?
- Does the asserted inventory equal its frozen source inventory?
- Does unavailable history skip or block visibly?
- Did at least one applicable assertion execute?
- Would each guard turn red under its named fault fixture?

## Exemptions

Day-one exemptions are forbidden. A later exemption requires explicit owner,
scope, rationale, expiry, and stable `EXEMPTION_ACTIVE` warning. Expired or
malformed exemptions block with `EXEMPTION_EXPIRED`. An exemption cannot waive
root containment, signed-evidence preservation, exact inventory identity, or
anti-vacuity.

## Fault injection

Each applicable boundary must have a named fault that changes one condition at
a time: declared hash, path escape, generated-output drift, raw gitlink mode,
set equality, signed allowlist, unavailable Git history, workflow invocation,
marker span, alias route, guidance binding, customization resolution, and each
frozen chain-table relation. The expected stable code must occur, the assertion
ledger must be nonempty, and the fixture must restore byte-identically even
when validation fails.

## V27 lifecycle-evidence route

The current-authority resolver and this verifier both expose a V27 route that supersedes V24 for
candidates descended from an externally authorized bootstrap publication.

- **Provenance.** Both hosts accept a `--trusted-host REVISION` argument. Its only legitimate value
  is the protected base supplied by an event whose trigger is itself branch-filtered to the
  protected branch: `push` or `pull_request_target` on `main`. A manual or otherwise unfiltered
  trigger supplies no base, and the anchor is then **empty**. An empty or absent anchor is absent
  provenance, not an error: V27 does not select and the existing V24 route stays authoritative.
  Never synthesize an anchor from `HEAD^`, from the checked-out head, or from an all-zero base.
- **Selection.** V27 is selected only when the candidate's full history introduces exactly one
  bootstrap publication and that publication is already contained in the anchor's ancestry. The
  bootstrap is an exact eight-path, single-parent, mode-`100644` commit; its record-only child is a
  single-path direct child. Candidate content never authorizes the route.
- **History.** Complete history is required before any V27 fact is derived. Shallow clones, partial
  (`--filter`) clones, and a candidate or bootstrap with no available parent are `BLOCKED`, never
  `PASS`, `FAIL`, or `not-applicable`. Merge candidates are rejected rather than evaluated on a
  first-parent diff.
- **Results.** The V27 result envelope is closed and discriminated on its first assertion row, which
  must be one of two forms and nothing else. A routed envelope names exactly one of `V27.ROUTE.C1`,
  `V27.ROUTE.C2`, `V27.ROUTE.DESCENDANT`, `V27.ROUTE.DRIFT`, or `V27.ROUTE.BLOCKED`. A protected host
  that blocks before it reaches the routed publisher may instead lead with its own `V27_` blocker
  code, and that form is constrained to a failing, blocker-carrying result. Every host validates the
  envelope against the digest-pinned V27 schema before acting on it, and every result carries the
  `ACTIVE` hold with four false authority flags.
- **Exception procedure.** The bootstrap can never authorize itself, so landing it is a human-owned,
  one-time protected-branch exception performed outside this tooling. Record actor, time, reason and
  the exact bootstrap commit; land the bootstrap alone and expect its own check to be red, because
  its event baseline still runs the predecessor route; confirm that protected `main` equals the
  bootstrap; then land the record-only child normally, where the bootstrap baseline authorizes it.
  Never combine the two, never push either from an automated build, and never reuse the exception.

### Blocker code inventory

Publisher and resolver codes use the `V27_` prefix; this verifier normalizes everything it emits or
propagates into `EVIDENCE_V27_`. Filtering on one prefix at one host therefore never drops part of
the set. The stable codes a reader should recognize are:

- Provenance and selection: `V27_TRUSTED_HOST_REQUIRED`, `V27_TRUSTED_HOST_NOT_APPLICABLE`,
  `V27_PROTECTED_HOST_UNAVAILABLE`, `V27_BOOTSTRAP_NOT_PROTECTED`, `V27_BOOTSTRAP_NOT_ANCESTOR`,
  `V27_BOOTSTRAP_PUBLICATION_MISSING`, `V27_BOOTSTRAP_PUBLICATION_SPLIT`,
  `V27_DUPLICATE_BOOTSTRAP_PUBLICATION`.
- History availability: `V27_HISTORY_UNAVAILABLE`, `V27_HISTORY_INVALID`,
  `V27_CANDIDATE_PARENT_DRIFT`, `V27_LINEAGE_UNAVAILABLE`.
- Publication shape: `V27_BOOTSTRAP_SCOPE_DRIFT`, `V27_BOOTSTRAP_PARENT_DRIFT`,
  `V27_BOOTSTRAP_MODE_DRIFT`, `V27_COMBINED_PUBLICATION_REJECTED`, `V27_C2_PUBLICATION_MISSING`,
  `V27_DUPLICATE_RECORD_PUBLICATION`, `V27_RECORD_PARENT_DRIFT`, `V27_RECORD_SCOPE_DRIFT`,
  `V27_RECORD_MODE_DRIFT`.
- Governed no-touch: `V27_GOVERNED_PATH_TOUCHED`, `V27_GOVERNED_ARTIFACT_DRIFT`,
  `V27_GITMODULES_DRIFT`, `V27_ROOT_GITLINK_DRIFT`, `V27_SUBMODULE_BINDING_DRIFT`,
  `V27_RECORD_HISTORY_TOUCHED`, `V27_RECORD_DESCENDANT_DRIFT`, `V27_RECORD_REVERTED_OR_DELETED`.
- Record identity: `V27_RECORD_IDENTITY_MISMATCH`, `V27_RECORD_INVALID`,
  `V27_EMPTY_ASSERTION_LEDGER`, `V27_AUTHORITY_FLAG_DRIFT`, `V27_OBSERVED_DIFF_UNTRUTHFUL`,
  `V27_RESULT_INVALID`, `V27_RESULT_SCHEMA_INVALID`, `V27_RESULT_ENVELOPE_INVALID`.
- Publication safety: `V27_RECORD_ALREADY_EXISTS`, `V27_RECORD_CONCURRENT_PUBLICATION`,
  `V27_RECORD_PARENT_ALIAS`, `V27_PUBLICATION_FINAL_IDENTITY_DRIFT`, `V27_PUBLICATION_IO_FAILED`.
- Record identity, continued: `V27_MANIFEST_SCOPE_DRIFT`, `V27_RECORD_SELF_INCLUSION`,
  `V27_DOCUMENT_SCHEMA_INVALID`, `V27_RESULT_ENVELOPE_INVALID`.
- Loading and executing the pinned publisher: `V27_PUBLISHER_LOAD_FAILED`,
  `V27_PUBLISHER_INTERFACE_INVALID`, `V27_PUBLISHER_EXECUTION_FAILED`.
- Raw object and inventory reading: `V27_DIFF_MALFORMED`, `V27_DIFF_UNAVAILABLE`,
  `V27_CHANGED_PATH_DUPLICATE`, `V27_TREE_UNAVAILABLE`, `V27_GIT_OBJECT_UNAVAILABLE`,
  `V27_GITLINK_INVENTORY_UNAVAILABLE`, `V27_GITLINK_INVENTORY_INVALID`,
  `V27_GITMODULES_MALFORMED`, `V27_GITMODULES_DUPLICATE_PATH`, `V27_PATH_INVALID`,
  `V27_LINEAGE_UNAVAILABLE`, `V27_ANCESTRY_UNAVAILABLE`, `V27_BOOTSTRAP_NOT_ANCESTOR`,
  `V27_BOOTSTRAP_ARTIFACT_UNAVAILABLE`, `V27_GOVERNED_ARTIFACT_MISSING`,
  `V27_EVALUATED_CANDIDATE_UNAVAILABLE`, `V27_REVISION_UNAVAILABLE`, `V27_SCHEMA_INVALID`,
  `V27_RECORD_UNAVAILABLE`, `V27_WORKFLOW_IDENTITY_MISMATCH`.
- Generation and command line: `V27_GENERATION_BASELINE_UNAVAILABLE`,
  `V27_GENERATION_BASELINE_DRIFT`, `V27_TRUSTED_HOST_REQUIRED`, `V27_TRUSTED_HOST_NOT_APPLICABLE`,
  `V27_COMBINED_PUBLICATION_REJECTED`, `V27_BOOTSTRAP_MODE_DRIFT`.
- Tooling and environment: `V27_TOOLING_UNAVAILABLE`, `V27_SCHEMA_UNAVAILABLE`,
  `V27_SCHEMA_IDENTITY_MISMATCH`, `V27_PUBLISHER_IDENTITY_MISMATCH`, `V27_GIT_UNAVAILABLE`,
  `V27_REPOSITORY_UNAVAILABLE`, `V27_REPOSITORY_ROOT_MISMATCH`, `V27_PUBLICATION_IO_FAILED`.

An unavailable dependency, an unavailable schema, an unreadable repository, a filesystem fault, a
refused or concurrent publication, and an aliased publication parent are all `BLOCKED`, never `FAIL`.
`FAIL` is reserved for proven governed drift: a touched governed path, a drifted `.gitmodules` or
gitlink, a record that is not its deterministic projection, and a publication whose installed bytes
do not match what was generated.

## V28 five-root-gitlink authority

V28 acknowledges the exact five root gitlink changes in `11e7e4dcfe8c7f2b57385225c0c79bfa81beadcf` while retaining V27's historical FAIL at the pre-V28 host. The protected resolver and evidence verifier select V28 before V27 only when the event-supplied protected base already contains V28 C1. An empty or pre-C1 base leaves V27 authoritative. Candidate content does not supply provenance.

V28 C1 is the direct child of the corrected spec predecessor and changes exactly eleven mode-`100644` files. Its protected-base check remains red; with C1 as the protected base, C1 returns a nonempty `BLOCKED` result until the record-only C2 is published. C2 is C1's direct single-parent child and changes only `_bmad-output/planning-artifacts/v28-five-root-gitlink-authority-v1.json`. Both hosts independently check the historical raw diff, the ten frozen root gitlinks, `.gitmodules`, C1 path and mode scope, and pinned schema and publisher bytes before importing the publisher. They recompute C1's complete blob manifest and canonical digest against C2. C2 and untouched single-parent descendants can return `PASS` with the true immediate-parent diff.

All V28 results keep the implementation hold `ACTIVE`; `executionAllowed`, `ownerApprovalClaimed`, `releaseAuthorized`, and `pushAuthorized` remain false. History loss and merge candidates block. A later touch of a governed file, `.gitmodules`, root gitlink, or the record fails even if its bytes are restored. The one-time C1 landing exception is human-owned and must record actor, time, reason, and exact C1 hash; confirm protected `main == C1` before landing C2 through the ordinary protected workflow. Neither host grants that exception or lifts the hold.

For local C2 publication, start at the committed C1 and verify the protected base is exactly that commit. Generate the record, validate and commit its single-path change, then check the committed C2 through both hosts:

```bash
c1="$(git rev-parse HEAD)"
test "$(git ls-remote origin refs/heads/main | cut -f1)" = "$c1"
uv run --frozen --no-sync python3 _bmad/scripts/publish_v28_five_root_gitlink_authority.py --root . --write
# Commit only _bmad-output/planning-artifacts/v28-five-root-gitlink-authority-v1.json after commitlint validation.
c2="$(git rev-parse HEAD)"
uv run --frozen --no-sync python3 _bmad/scripts/publish_v28_five_root_gitlink_authority.py --root . --verify "$c2" --trusted-host "$c1"
uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate "$c2" --trusted-host "$c1" --check
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline "$c1" --candidate "$c2" --trusted-host "$c1"
```

## Known limitations

- This runbook does not install workflow gates or replace the verifier.
- It does not claim CI wiring, story completion, readiness, hold lift, or
  release approval.
- Shallow or partial history may prevent validation; that state is `BLOCKED`.
- The V27 route is only as current as its pinned roots of trust. Changing the V27 schema, publisher,
  or protected workflow changes a digest that both hosts pin, so those pins and the bootstrap they
  authenticate must be republished together rather than edited in place.
- V27 evaluates single-parent candidates only. A merge candidate is rejected rather than evaluated
  on a first-parent diff, so a merge into the protected branch needs a fresh evaluation of the
  resulting commit before its evidence is current.
- The one-time protected-branch exception above is outside this runbook's enforcement. Nothing here
  verifies that it was recorded, and no tooling can grant it.
- Workflow upgrades require inventory, route, parity, and resolved-
  customization revalidation before prior evidence is current again.
