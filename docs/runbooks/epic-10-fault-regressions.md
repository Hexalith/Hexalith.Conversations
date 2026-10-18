# Epic 10 fault regressions

Use the pinned .NET SDK in `global.json` and the locked Python environment:

```bash
uv sync --locked --python 3.11
```

For changes to adoption output protections, authority-chain validation, their
bridge routing or mutation harnesses, run both mutation proofs before completion:

```bash
uv run --frozen --no-sync python3 tests/tooling/check_adoption_hardlink_mutation.py
uv run --frozen --no-sync python3 tests/tooling/check_authority_chain_mutation.py
```

Each command must exit zero. The first requires one passing baseline regression,
then its guard-rejection assertion failing with both collision and link-count
protections disabled in memory. The second requires seven passing baseline
fault tests, then all seven guard-rejection assertions failing with the compiled
`AuthorityChain` method disabled in temporary sources. Collection, build, fixture,
restoration, unrelated assertion failures and surviving mutants cannot establish
success. Both commands preserve workspace sources and accepted evidence.

Run the routine CI selection for the affected behavior:

```bash
uv run --frozen --no-sync python3 -m pytest -q -ra -p no:cacheprovider _bmad/scripts/tests tests/tooling/test_evidence_boundary_current.py --ignore=_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py -k 'adoption_output_ or test_fault_receipt_ or frozen_authority_chain or runbook_acceptance_sequence or complete_fault_matrix or evidence_gate_span_coupling'
```

The seven chain cases mutate a disposable copy of the actual frozen authority
document through `authorityChains`, using the Runner's independently pinned
identities. They require the exact marker/table blocker, evaluated guard rows,
`FAIL/1` and byte-identical restoration. A separate regression requires the
production adoption seam to dispatch to the compiled guard and merge its ledger
and rejection, using disposable Git history without a historical publication replay.

The five native `EvidenceBoundaryAdoptionValidationTest` ACs replay the complete
frozen Story 10.4 source, publication and gitlink boundary. Routine conformance
excludes that class under [current-change policy](current-change-validation.md);
the current Python job retains live helper, reader, guidance and fault coverage.
The class remains unchanged and explicitly invocable against the accepted
checkpoint, as documented in the historical [evidence-boundary runbook](evidence-boundary-validation.md).
Later main can legitimately fail that historical replay. Preserve its result
and accepted receipts; never republish them to make a routine change pass.
