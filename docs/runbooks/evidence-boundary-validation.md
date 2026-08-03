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

## Known limitations

- This runbook does not install workflow gates or replace the verifier.
- It does not claim CI wiring, story completion, readiness, hold lift, or
  release approval.
- Shallow or partial history may prevent validation; that state is `BLOCKED`.
- Workflow upgrades require inventory, route, parity, and resolved-
  customization revalidation before prior evidence is current again.
