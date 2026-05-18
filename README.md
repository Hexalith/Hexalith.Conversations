# Hexalith.Conversations

Hexalith.Conversations is a tenant-scoped conversations module scaffold for the Hexalith ecosystem.

This initial scaffold is intentionally non-operative. It establishes project boundaries, central package management, smoke tests, and ADR tracking for future stories without implementing conversation persistence, tenant authorization, provider integrations, workers, read models, governance commands, or FrontComposer runtime behavior.

## Local Validation

Use the repository-pinned .NET SDK from `global.json`:

```powershell
dotnet restore Hexalith.Conversations.slnx
dotnet build Hexalith.Conversations.slnx
dotnet test Hexalith.Conversations.slnx
```

The scaffold smoke checks must not require Aspire runtime launch, Dapr sidecars, tenant seed data, production secrets, provider credentials, external cloud resources, or nested submodule initialization.

## Submodules

Root-level sibling modules are preserved through `.gitmodules`. Do not run recursive submodule initialization for this repository unless nested submodules are explicitly requested.
