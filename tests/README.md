# Hexalith.Conversations Test Framework

Hexalith.Conversations uses xUnit v3, Shouldly, and coverlet through central package management. Story 3.8A adds a narrow rendered Admin Web evidence host with a .NET Playwright responsive lane for the reopened investigation-workspace verification split. Story 3.8B adds an accessibility evidence lane in the same project (accessibility tree, heading outline, landmarks, keyboard focus order, and accessible-name safety) that runs alongside the responsive lane.

## Setup

Use the SDK pinned by `global.json` and restore from the repository root:

```powershell
dotnet restore Hexalith.Conversations.slnx
```

The test scaffold does not require Aspire runtime launch, Dapr sidecars, tenant seed data, production secrets, provider credentials, external cloud resources, or nested submodule initialization.

Install the browser binary before running the Admin Web responsive lane on a new machine:

```powershell
pwsh tests/install-playwright.ps1
```

## Running Tests

Run all tests:

```powershell
dotnet test Hexalith.Conversations.slnx
```

Run the fast domain lane:

```powershell
dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj
```

Run integration smoke checks:

```powershell
dotnet test tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj
```

Run the rendered Admin Web evidence lane (responsive + accessibility share one project):

```powershell
dotnet test tests/Hexalith.Conversations.Admin.Web.Tests/Hexalith.Conversations.Admin.Web.Tests.csproj
```

Run only the Story 3.8B accessibility evidence harness:

```powershell
dotnet test tests/Hexalith.Conversations.Admin.Web.Tests/Hexalith.Conversations.Admin.Web.Tests.csproj --filter "FullyQualifiedName~Accessibility"
```

Collect coverage:

```powershell
dotnet test Hexalith.Conversations.slnx --collect:"XPlat Code Coverage"
```

## VSTest Socket Fallback

Some restricted sandboxes block VSTest from creating local sockets even when the test assembly itself is valid. When that happens, build the affected test project and run the compiled xUnit v3 executable directly:

```powershell
dotnet build tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj -c Release --no-restore /nr:false /m:1
tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests
```

For focused documentation lanes, pass the xUnit class filter to the executable:

```powershell
tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests -class Hexalith.Conversations.Contracts.Tests.Documentation.MinimalModuleAuthoringCostBaselineValidationTest
```

Record both facts in story evidence: the VSTest socket failure and the direct xUnit executable result. If a test host needs to allocate a local port, classify socket permission failures as environment-limited unless another failure proves a product regression.

## Final-Record Completion Gate

Completion records are **generated from measured state, not authored**. A record may not contain a count, path, or commit that nobody measured. Commit every story-owned source path and require the remaining source tree clean outside record outputs and declared TRX artifacts; then run the tests and generate.

Emit TRX from the built xUnit v3 executable — `-trx <file>` on the executable itself:

```powershell
tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests `
  -noLogo -trx $PWD/TestResults/conformance.trx
```

`dotnet test --report-trx` is **rejected as an unknown option** on this lane; use the executable form above. See § VSTest Socket Fallback for building the executable.

Then invoke the generator once per completion, repeating `--test-results` with the full project name for every root-owned test project declared by the root `.slnx`:

```powershell
python3 _bmad/scripts/generate_story_record.py `
  --repository . `
  --story _bmad-output/implementation-artifacts/<story>.md `
  --baseline <story-baseline-commit> `
  --candidate HEAD `
  --test-results Hexalith.Conversations.Conformance.Tests=<path>.trx `
  --format bundle
```

The gate fails on any nonzero exit or any nested `document.result` other than `pass`. It derives, rather than compares against hand-authored expectations:

- test counts parsed from `/TestRun/ResultSummary/Counters`, recomputed from the recorded results, and totalled by summation — required scope comes from the root solution; missing, empty, failed, or unapproved-skipped results block;
- the File List, from the committed baseline→candidate range with other source-tree dirt blocked — exactly one list, no submodule-internal path;
- root gitlink promotions with recorded commit and mode, in their own labelled section, bound to a candidate proven to be the revision that is actually final; and
- the Story 6.7 promotion-checker document, embedded verbatim.

The generated bundle's JSON document is authoritative; its Markdown is inserted verbatim and verified with `--verify-record-sha256 <markdown_sha256>`. A run that parsed no artifact, resolved no candidate, found no record section, or failed the insertion digest reports a blocker and can never be read as a pass. Historical mode (`--historical`) structurally parses already-closed generated records read-only, and never claims to reconstruct a former uncommitted working tree. A failure blocks `review -> done` until the record is corrected; never hand-edit a count, path, or commit into agreement, and never rewrite signed evidence to make the gate pass.

Full operating procedure, blocker→remediation table, exit codes, and safety boundary: `docs/runbooks/story-final-record-generation.md`.

### Epic 5 historical asset

`tests/Test-StoryFinalRecord.ps1` and `tests/Test-StoryFinalRecord.Tests.ps1` are the Epic 4 action A1 asset and the record of the Epic 5 final-record check. They verify a **hand-authored** manifest — the defect the generator above exists to remove — and are bound to Epic 5 artifacts. They are retained as the historical record and are not invoked by any workflow. Do not delete them, and do not re-mark Epic 4 action A1.

## Architecture

Test projects mirror production package boundaries:

- `Hexalith.Conversations.Contracts.Tests` validates public contract safety.
- `Hexalith.Conversations.Client.Tests` validates client boundary shape.
- `Hexalith.Conversations.Tests` is the pure domain/unit lane.
- `Hexalith.Conversations.Server.Tests` validates server boundary behavior.
- `Hexalith.Conversations.Admin.Web.Tests` validates the rendered investigation workspace host across two evidence lanes: the Story 3.8A responsive lane (responsive trust ordering, mobile safe triage, duplicate surface safety, poison-sentinel absence, viewport telemetry labels) and the Story 3.8B accessibility lane (single-h1 heading outline in trust order, banner/search/main landmarks and skip link, safe `aria-live` region, accessible blocked-command descriptions, keyboard focus order, accessible-name forbidden-sentinel scan, high-contrast/reduced-motion/200%-zoom survival), plus evidence artifact generation for both.
- `Hexalith.Conversations.IntegrationTests` covers repository, package, and future topology smoke checks.

Shared fixtures and factories live in `src/Hexalith.Conversations.Testing`. Keep helpers pure first, then wrap them in xUnit fixtures only when repeated setup or teardown is real. Assertions should remain visible in test bodies.

## Practices

- Prefer unit tests for aggregate decisions, validators, redaction rules, and deterministic state transitions.
- Use integration tests for HTTP paths, EventStore command flow, Tenants projection wiring, and FrontComposer registration.
- Use generated, tenant-scoped test IDs from `ConversationTestIds` instead of hard-coded cross-test shared values.
- Keep tests deterministic: no sleeps, no random values without a stable factory, no conditional flow that hides failures.
- Keep command-time participant and tenant validation fail-closed in tests.
- Browser/E2E tooling is scoped to the Story 3.8A and 3.8B rendered Admin Web evidence surface. Keep Playwright selectors based on accessible roles/labels (`Page.GetByRole`, `GetByLabel`) or stable `data-testid` contracts, capture the accessibility tree with `Locator.AriaSnapshotAsync()`, wait for observable state instead of fixed sleeps, and install browsers through `tests/install-playwright.ps1`.

## CI Notes

The blocking lane should start with:

```powershell
dotnet restore Hexalith.Conversations.slnx
dotnet build Hexalith.Conversations.slnx --configuration Release
dotnet test Hexalith.Conversations.slnx --configuration Release --collect:"XPlat Code Coverage"
```

Upload `TestResults/` as the coverage artifact when CI is configured.

## Knowledge References

This scaffold applies Murat knowledge fragments: `fixture-architecture`, `test-levels-framework`, `test-quality`, and `playwright-config` for future UI expansion.
