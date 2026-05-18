# Hexalith.Conversations Test Framework

Hexalith.Conversations uses xUnit v3, Shouldly, and coverlet through central package management. The current module is backend-only, so the primary test framework is .NET/xUnit rather than Playwright or Cypress.

## Setup

Use the SDK pinned by `global.json` and restore from the repository root:

```powershell
dotnet restore Hexalith.Conversations.slnx
```

The test scaffold does not require Aspire runtime launch, Dapr sidecars, tenant seed data, production secrets, provider credentials, external cloud resources, or nested submodule initialization.

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
- `Hexalith.Conversations.IntegrationTests` covers repository, package, and future topology smoke checks.

Shared fixtures and factories live in `src/Hexalith.Conversations.Testing`. Keep helpers pure first, then wrap them in xUnit fixtures only when repeated setup or teardown is real. Assertions should remain visible in test bodies.

## Practices

- Prefer unit tests for aggregate decisions, validators, redaction rules, and deterministic state transitions.
- Use integration tests for HTTP paths, EventStore command flow, Tenants projection wiring, and FrontComposer registration.
- Use generated, tenant-scoped test IDs from `ConversationTestIds` instead of hard-coded cross-test shared values.
- Keep tests deterministic: no sleeps, no random values without a stable factory, no conditional flow that hides failures.
- Keep command-time participant and tenant validation fail-closed in tests.
- Add browser/E2E tooling only when a Conversations UI surface exists; then prefer Playwright for multi-browser, API plus UI, and CI artifact support.

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

