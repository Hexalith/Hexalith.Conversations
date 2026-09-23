---
title: 'Acknowledge the five committed root gitlink changes with V28 authority'
type: 'feature'
created: '2026-09-23'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1b01599ce16df03e44e83ca2edecac086f965ba7'
---

<frozen-after-approval reason="owner-approved historical gitlink retention and successor boundary">

## Intent

The owner approves retaining the five root gitlink changes introduced by
`11e7e4dcfe8c7f2b57385225c0c79bfa81beadcf`. V27 correctly rejects that
historical touch: at `1b01599ce16df03e44e83ca2edecac086f965ba7`, the
current-authority resolver reports `V27_GOVERNED_PATH_TOUCHED` and the evidence
host reports `EVIDENCE_V27_GOVERNED_PATH_TOUCHED`, both `FAIL` / exit 1. Publish
an additive V28 successor that acknowledges only this exact prior transaction.
V28 remains non-executable; the implementation hold is `ACTIVE`, and
`executionAllowed`, `ownerApprovalClaimed`, `releaseAuthorized`, and
`pushAuthorized` remain false.

## Frozen historical change

The `11e7e4d` parent is `cc90e109f5903d5cde328d14e8264087e961e5ea`;
its tree is `387df36d58577893764695d48b9fc8af33fbede3`.
`11e7e4d` has tree `8d4639b43fc495fce6328b7fa3f20366eeb5418a`.
Pin the complete raw parent diff below. Each entry is mode `160000` before and
after; no other path changed in that commit.

| Root path | Before | After |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `4bc61d9a60fae13a65f23963c1cb731065222f2a` | `d0291d0919c58e805b875e42a0a84e62c1c6e3e5` |
| `references/Hexalith.Folders` | `b10971ae6e17d4fca2933908531f80a5f58b6e5a` | `19c29e00eb6679bbf7a0d20a8b4dbf30182243cf` |
| `references/Hexalith.FrontComposer` | `237b39e89cb50d8d37596f4da9ce3f6993260165` | `b6ea7834591c9352b1c0ec338fb64e8fedd5db11` |
| `references/Hexalith.Projects` | `ffa922d0f7a278da3bb02ee09769564df02d5f01` | `3f12e4c329a8b46a8e69397635ba290388923c75` |
| `references/Hexalith.Tenants` | `dfd84f4b9c93779f253672096fec5d717d6afc91` | `850b321c38421bbeec903e74b548b8293b30a610` |

Bind the other five root gitlinks and `.gitmodules` to their exact bytes at
`1b01599`; require ten raw mode-`160000` entries matching its root declaration.
Reject any missing, extra, moved, later touched, or restored gitlink, any
`.gitmodules` drift, and any changed historical diff or parent. Preserve V23–V27
versioned records, schemas, publishers, and their historical committed bytes.
Preserve
the Story 7.1 final pair byte-for-byte (JSON SHA-256
`0a4ede3074fca55853f2fd59f065677cacea3eb700491cbc87594e117557e2f9`,
Markdown SHA-256 `044ac80a72a2f58587dbd9a53463d75567a75ac99797028fa4cc9a413c3dab43`).
V28 does not claim Story 7.1 `ACCEPTED` or produce the Story 7.2 final pair.

## Proposed publication

Commit this spec as the predecessor after `1b01599`. Build a single-parent C1
bootstrap with exactly these eleven mode-`100644` changed paths:

1. `.github/workflows/planning-authority-preflight.yml`
2. `_bmad/schemas/v28-five-root-gitlink-authority-v1.schema.json`
3. `_bmad/scripts/publish_v28_five_root_gitlink_authority.py`
4. `_bmad/scripts/resolve_current_planning_authority.py`
5. `_bmad/scripts/tests/test_publish_v28_five_root_gitlink_authority.py`
6. `_bmad/scripts/tests/test_resolve_current_planning_authority.py`
7. `_bmad/scripts/tests/test_verify_evidence_boundary.py`
8. `_bmad/scripts/verify_evidence_boundary.py`
9. `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs`
10. `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs`
11. `docs/runbooks/evidence-boundary-validation.md`

The V8/V9 checks must read their pinned historical sprint bytes for historical
assertions and check the current sprint projection separately. The two protected
hosts must independently verify protected-host ancestry, C1's exact path and
mode scope, and hard-pinned V28 schema and publisher digests before importing
V28 code. They must recompute the full C1 blob manifest and digest and compare
them with C2's record, and verify the exact historical gitlink diff. The
external landing packet pins C1's complete commit identity. V28 dispatch precedes
V27 only when the protected event base already contains C1. Missing or empty
provenance keeps the existing V27 failure; candidate content cannot self-authorize.
Reject partial history and merge candidates. The C1 result remains a nonempty
`BLOCKED` / exit 2 until its record is published.

Publish C2 as C1's direct single-parent child changing only
`_bmad-output/planning-artifacts/v28-five-root-gitlink-authority-v1.json` in
mode `100644`. Its deterministic record binds C1 commit/tree/parent, the exact
eleven-row path/object/SHA-256/size manifest and canonical manifest digest,
the ten gitlinks, `.gitmodules`, and the five approved historical transitions;
exclude the record from its own digest. C2 and untouched descendants may return
schema-valid, nonempty-ledger, non-executable `PASS` / exit 0, with their truthful
immediate-parent diff. No V28 PASS exists before protected C1 provenance.

## Verification and landing packet

- Run focused V28 publisher, resolver, and evidence-host pytest suites with
  `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true`.
- Run the V8/V9 Conformance classes from the built test assembly, the Release
  solution build, the full Conformance test project, and Story 7.2 selectors
  `AC-7.2-01` through `AC-7.2-10` from the frozen V9 contract. Record the
  exact command, exit, test count, and blocker for each gate; do not run
  `AC-7.2-11` or generate its final record.
- Verify C1 and C2 with both authority hosts against a protected host before
  C1 and one at C1; inspect exact parent/path/mode/blob/digest manifests,
  historical no-touch, `git diff --check`, and pinned commitlint results.

The landing packet must name exact C1/C2 hashes and trees, all manifest
digests, gate results, and the owner decisions still open. Landing C1 requires
a one-time human-owned protected-branch exception recording actor, time,
reason, and exact C1 hash. The C1 check is expected to remain red because its
event base predates C1. Confirm protected `main == C1` before landing C2
separately through the ordinary protected workflow. The packet must disclose
that the C1 fast-forward includes the local `1b01599` ancestry and this spec.
Do not push, perform the exception, lift the hold, or call Story 7.1 accepted.

</frozen-after-approval>

## Tasks & Acceptance

- [ ] Publish the pinned V28 schema, publisher, protected workflow, resolver, verifier, and focused tests in the exact C1 scope above.
- [ ] Repair the two Conformance test classes using historical Git sprint bytes and a separate current projection assertion.
- [ ] Generate the deterministic C2 record from committed C1 and commit C2 alone.
- [ ] Record the focused authority, Conformance, build, and Story 7.2 gate results and exact landing hashes.

- Given a protected base before C1, when C1 is evaluated, then V28 does not self-select and the existing V27 blocker remains.
- Given protected C1, when C1 is evaluated, then V28 reports `BLOCKED` with a nonempty ledger.
- Given protected C1, when C2 or an untouched descendant is evaluated, then V28 reports schema-valid `PASS`, `ACTIVE`, four false flags, and the true immediate-parent diff.
- Given any changed historical transition, path, mode, digest, parent, gitlink, or governed descendant, when evaluated, then V28 fails or blocks with a stable nonempty diagnostic.

## Implementation Notes

The final C0, C1, and C2 identities are recorded in the landing packet after local publication.
