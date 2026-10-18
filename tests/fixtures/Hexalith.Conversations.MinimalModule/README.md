# Hexalith.Conversations.MinimalModule

Reproducible minimal-module fixture for Story 11.2 (Checkpoint B). It proves that a thin
module built from `docs/domain-module-authoring-template.md` restores, builds and passes
tests using only public Hexalith platform APIs.

The module owns contracts, domain behavior, registration and tests. The platform owns
ServiceDefaults, Dapr wiring, event subscription, projection and query runtime,
publication, health and telemetry. The single `AppHost` under `tests/` is a repository-only,
non-packable, non-publishable test host and counts in authoring cost.

```text
dotnet restore Hexalith.Conversations.MinimalModule.slnx --locked-mode
dotnet build Hexalith.Conversations.MinimalModule.slnx --configuration Release --no-restore
```
