# Epic 5 Final-Record Corrective Amendment

**Date:** 2026-08-22
**Marker:** `EPIC-5-FINAL-RECORD-CORRECTIVE-AMENDMENT-2026-08-22`
**Approval:** The user approved preserving the failed predecessor and publishing a dated corrective amendment plus successor audit.

## Preserved Predecessor

The original result remains byte-identical at commit `8f8f14fd6e842eeb19b7410554366a93f8a93ce5`:

- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-check.json` — SHA-256 `a6ec97c1fc3fb3e026d72ce5bd480561d71acf3c051f84ac73f9fd24671c65e1`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-check.md` — SHA-256 `0b8e1de3fcd132c2d0d226a38d9e7c94037a5b4db6c2448c5d418070f551a710`

Its authoritative JSON result is `fail`. The two preserved failures are:

1. `Live declared-vs-observed path inventory contains unexpected path '_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-14.md'.`
2. `Executable/test input fingerprint is stale: expected PENDING_FINAL_RUN, found 3c12175c7a32b4cabc0ad967c1dd9e10d2e4a6f1eb06c273f6092e7ca1cc6805.`

The July 14 retrospective follow-up and sprint action were therefore closed before their declared final-record gate had passed. This amendment records that discrepancy; it does not reinterpret the failed predecessor as a pass.

## Corrective Disposition

The successor audit:

- verifies the predecessor bytes, source commit, failed status, and exact failure inventory;
- reruns the hardened checker from a new, exact frozen working-tree boundary;
- retains the Story 5.1–5.3 historical count/path/evidence checks and Story 5.2 amendment;
- binds the current test result to the current executable/test-input fingerprint; and
- records a nonempty assertion ledger with distinct `PASS`, `FAIL`, `BLOCKED`, and `not-applicable` semantics.

This correction does not reconstruct the former uncommitted working tree. It provides a new live proof for the corrective work and an explicit approved disposition for the preserved July 14 failure.

No file under `docs/release-evidence/` is edited by this amendment. The Story 5.2 source record and the signed/hash-bound Story 5.3 evidence remain unchanged.
