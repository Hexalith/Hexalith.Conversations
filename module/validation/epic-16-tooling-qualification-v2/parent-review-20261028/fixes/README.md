# Parent review fixes — 2026-10-28

The selected tooling-v2 inserted-record regression now generates five receipts
and a final pair through the actual CLIs, inserts the generated Markdown, verifies
PASS through the generator with the exact selected contract/schema bindings,
then corrupts the region and requires `INSERTED_RECORD_DRIFT` and a nonempty ledger.
The three isolated held-runtime cases invoke all five verifiers and the generator
for each story. Each starts with seven existing receipt/final files and requires
BLOCKED, zero measured cases, false authority and byte-identical existing evidence.
The seven real-root Story 16.1 outputs remain absent. The module README explicitly
compares its no-change statement with implementation-entry commit
`ef01d578a00efd527bae9226dca302f54cd012f0`.

Only the four new parametrized Python cases ran: **4 passed, 60 deselected**.
[JUnit](focused-review-regressions.xml), the exact
[command and bounds](focused-review-regressions.json), separate raw streams and
[pinned commitlint evidence](fixture-commitlint.json) are retained here.

The fresh producer qualification rebuilt the individual Module test project in
Debug and invoked its built test DLL directly for these same four classes:

| Class | Passed | Explicit bound | Process duration |
| --- | ---: | ---: | ---: |
| Workspace.BuildPreparationTests | 11 | 180 s | 5.811 s |
| PlatformManifestCommandTests | 75 | 180 s | 4.709 s |
| PlatformAdoptionValidationTests | 396 | 900 s | 213.511 s |
| Workspace.PlatformIdentityTests | 43 | 180 s | 19.030 s |

All **525** TRX cases passed with zero failures, errors or unexecuted cases.
Each class has separate raw stdout/stderr, TRX, exact command/bound metadata,
and persisted before/after SHA-256 inventories. See the
[derived native summary](producer-native-summary.json).

The clean producer repository is `references/Hexalith.Builds` at exact revision
`81ab049208678bd6616828824729c0ddac02f7d1` (tree
`9ee31a7735250dd2eb9627dbd8b0294bd67a39d9`).
[The Debug build](producer-debug-build.json) completed successfully under a
300-second bound. The actual invoked assembly is
`test/Hexalith.Builds.Module.Tests/bin/Debug/net10.0/Hexalith.Builds.Module.Tests.dll`
relative to that producer repository. The
[pre-execution inventory](native-execution-before.json) binds this DLL, its
`Hexalith.Builds.Module.Cli`, `Hexalith.Builds.Tooling` and
`Hexalith.Builds.Module.AppHost` dependencies, all other DLLs in its invocation
directory, and its deps/runtimeconfig files (92 files total).
[The final inventory](native-execution-after.json) and each class's before/after
inventory are identical. Producer revision, tree and clean source state also
match before and after execution.

These are fresh source/Debug observations. They make no retrospective
before-execution assembly-hash claim about attempt-03, and confer no published
package, runtime entry, release or owner qualification. The
[preservation report](preservation.json) confirms that attempt-03's complete
326-file inventory, including its artifact index, retains identical bytes and
that the seven real-root runtime outputs were absent before and after this work.
The [local artifact index](artifact-index.json) covers this fixes directory and
excludes itself.
