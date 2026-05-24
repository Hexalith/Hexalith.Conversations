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

