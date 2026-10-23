# Minimal-module platform compatibility

This CI-only compatibility suite compiles and runs the frozen Story 11.2 fixture against
the EventStore revision committed by Conversations. It runs the original six tests and
adds one registered-command assertion through the public in-process domain router.
The assertion resolves the keyed processor, checks the serialized creation event,
replays it through the public in-process router in a fresh service scope, and checks
the successful replay sequence and serialized state. It then supplies a detached JSON
state snapshot to a second registered command in another scope and requires no emitted
event. That no-op observes rehydration of the `Created` flag through the SDK; it does not
observe `Title` after rehydration or establish persistence. The serialized title is
checked at the replay output boundary.

The external `RegisteredCommand.targets` import adds the assertion only to the fixture
test project. The measured 13 paths, project graph, lock files, accepted authoring-cost
records and receipts remain unchanged. This compatibility build is not a new measured
authoring sample or a regeneration of historical proof. Keep it outside the root solution
so the fixture's self-contained xUnit/package configuration stays isolated.

The `ci / minimal-module platform compatibility` job initializes only root-declared
`references/Hexalith.Builds` and `references/Hexalith.EventStore` at their committed gitlinks.
It requires Git, Python 3.11, network access to NuGet, and the .NET SDK from `global.json`
(currently 10.0.402). The fixture pins Aspire.AppHost.Sdk 13.6.1 and the Dapr hosting
integration; restore downloads those build dependencies. No Docker daemon, Dapr CLI,
sidecar, running AppHost, or external state store is needed. The AppHost is compiled only.

This selected lane checks the frozen fixture's existing platform source references in
Release; the product's normal package-based CI remains separate. The lane uses locked
restore, forbids skipped tests, checks the exact seven discovered test names and counters,
and uploads logs/results even on failure. A platform/dependency change that invalidates
the locks must fail for investigation rather than refresh them automatically.

To reproduce, use a clean checkout with the two root dependencies at their gitlinks and
run the restore/build/test commands in `.github/workflows/ci.yml`. Pass the same absolute
`CustomAfterMicrosoftCommonTargets` path to restore and build. Run the built test DLL
directly with the xUnit flags shown there, then run `verify_results.py` on its XML result.
For local source debugging, use Debug for both restore and build and the Debug test DLL.
Do not run another build sharing platform `obj` directories between restore and build.
