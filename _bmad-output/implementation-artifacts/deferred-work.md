# Deferred Work

Items deferred from completed code reviews. Each entry links to the source review and the rationale.

## Deferred from: code review of 1-1-set-up-initial-project-from-starter-template (2026-05-18)

- Strengthen boundary tests beyond `Assembly.GetReferencedAssemblies()` on empty marker assemblies. Today the compiler only retains references actually used by code; with marker-only assemblies, forbidden `<PackageReference>` entries would not show up in the runtime reference list. Revisit when stories 1.2+ add real content — at that point, parse `<PackageReference>` and `<ProjectReference>` from csproj files directly (or use a build-time analyzer) instead of relying on loaded-assembly reflection. Files: `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`, `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`, `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`, `tests/Hexalith.Conversations.Tests/DomainBoundaryTest.cs`.
- Add a `Hexalith.Conversations.slnx` vs disk parity test. Adding or removing a `.csproj` on disk without updating the slnx (or vice versa) is currently undetected. Coverage enhancement; defer until project list churn increases.
- Make `Directory.Build.props` sibling-module root probes fail loudly when sibling folders are absent. Today probes use `Exists(...)` and silently leave properties empty. Only material when sibling references are actually wired (likely Story 1.10/1.11). File: `Directory.Build.props:11-13`.
