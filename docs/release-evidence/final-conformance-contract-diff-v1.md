# Final Conformance and Contract Diff v1

**Story:** 5.1 - Final full-module conformance run + consolidated public-contract-shape diff
**Status:** pass
**Generated:** 2026-06-26T14:49:36Z
**Authoritative JSON:** `docs/release-evidence/final-conformance-contract-diff-v1.json`

## Summary

Story 5.1 confirms the behavior-preservation gate held across the initiative. It is not the first green run: Epic 3, Epic 4, and Story 4.2 already recorded standing conformance and contract-shape evidence. This artifact records the final confirmation input for Story 5.3.

## Conformance Run

- Build command: `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release --no-restore /nr:false /m:1`
- Build result: 0 warnings, 0 errors
- Preferred test command: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release --no-build /nr:false`
- Preferred test result: aborted before test execution because VSTest socket creation failed with `System.Net.Sockets.SocketException (13): Permission denied`
- Fallback command: `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests`
- Fallback result: 365 total, 365 passed, 0 errors, 0 failed, 0 skipped, 0 not run
- Suite classes: 14 `*ConformanceSuiteTest` classes

## Release-Gate Behaviors

The final conformance lane covers tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, and governance audit-pairing through the existing conformance suites and focused characterization tests.

## Contract Shape Diff

- Baseline: `docs/release-evidence/public-contract-shape-baseline-v1.json`
- Final snapshot method: temporary Story 5.1 reflection utility mirroring `PublicContractShapeSnapshotGenerationTest` ordering and serialization, writing outside the repository
- Comparison: byte-for-byte JSON comparison
- Baseline type count: 196
- Final type count: 196
- Diff status: empty
- Approval references: none required
- Baseline git diff: empty

## Baseline References

- `docs/release-evidence/release-baseline-v1.json`
- `docs/release-evidence/release-baseline-v1.md`
- `docs/release-evidence/public-contract-shape-baseline-v1.json`
- `_bmad-output/implementation-artifacts/1-1-pin-the-conformance-oracle-green-on-main-and-snapshot-the-public-contract-shape.md`

## Environment Limitations

VSTest could not create its local socket in this sandbox, so the Release test project was built and the compiled xUnit v3 executable was run directly. Root submodules were not initialized in this checkout; Story 5.1 recorded that state and did not initialize, reset, clean, or modify root or nested submodules.

Adjacent Epic 5 items remain outside this evidence story: OQ-2 target confirmation, removed-test ledger reconciliation, retained `Conformance.Tests` to `Server` reference reconciliation, and projection read-store population proof.
