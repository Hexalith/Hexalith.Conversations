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

The platform projects are referenced from `references/Hexalith.EventStore` and share their `obj` state
with any other build of the umbrella, so always run the locked restore immediately before the build.

For Checkpoint B evidence, run these wrappers from the repository root. Each executes the corresponding
frozen contract command unchanged and records its exit code, console output and policy in
`artifacts/v9/11.2/AC-11.2-02.log` or `AC-11.2-03.log` before generating the fixture proof.

```text
python3 _bmad/scripts/run_minimal_module_command.py --repository . --scenario AC-11.2-02
python3 _bmad/scripts/run_minimal_module_command.py --repository . --scenario AC-11.2-03
```
