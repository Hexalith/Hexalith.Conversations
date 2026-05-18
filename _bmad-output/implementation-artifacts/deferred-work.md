# Deferred Work

Items deferred from completed code reviews. Each entry links to the source review and the rationale.

## Deferred from: code review of 1-2-define-conversation-identity-command-event-and-error-contracts (2026-05-18)

- Bound extension/attribute/diagnostic dictionaries. `ProviderCorrelationMetadata.ExtensionData`, `UpdateConversationMetadataCommand.Attributes`, `ConversationMetadataUpdated.Attributes`, `ConversationError.SafeFieldDiagnostics` are all free-form `IReadOnlyDictionary<string,string>?` with no size cap, no allowed-key list, no forbidden-key enforcement. README/story claim they are "bounded" — the contract does not deliver. Owner: governance epic / Story 1.10 / Epic 2 retention policy.
- No `[JsonPolymorphic]` / discriminator on commands or events — defer to wire-transport story; document explicitly that wire dispatch is the adopter's responsibility for v1.
- README claim that "raw EventStore knowledge is not required" is overstated given the `{"value":...}` envelope wire shape. Revisit wording once the wire-shape decision (Story 1.2 Review Findings Decision #1) is resolved.
- `BusinessReference.System` name collides with `System` namespace; separator semantics between `System` and `Value` are not constrained. Renaming is a breaking contract change — bundle with the next contract-evolution batch.
- `ConversationError.Documentation : Uri?` accepts non-https, relative, and `javascript:` URIs. Restrict in the error-handling layer (Story 1.10 publication or governance audit emission).
- Cross-identifier-type silent rehydration: `JsonSerializer.Deserialize<ConversationId>(JsonSerializer.Serialize(new TenantId("x")))` succeeds because both wrap `{"value":"x"}`. Closed implicitly if Decision #1 introduces typed converters that project to primitives.
- Unicode / surrogate-pair / very-long ID inputs untested at the contract layer. Runtime story (Story 1.3 aggregate, Story 1.7 read models) needs to cap length and validate encoding.
- `ConversationSummaryProjection.ParticipantPartyIds` allows duplicate `PartyId` entries — producer hygiene concern; Story 1.7 projection logic should dedupe.
- `_bmad-output/process-notes/predev-preflight-latest.json` is committed with `"result": "fail"` next to a `Status: review` story. Process-artifact cleanup; revisit with the next preflight refactor.

## Deferred from: code review of 1-1-set-up-initial-project-from-starter-template (2026-05-18)

- Strengthen boundary tests beyond `Assembly.GetReferencedAssemblies()` on empty marker assemblies. Today the compiler only retains references actually used by code; with marker-only assemblies, forbidden `<PackageReference>` entries would not show up in the runtime reference list. Revisit when stories 1.2+ add real content — at that point, parse `<PackageReference>` and `<ProjectReference>` from csproj files directly (or use a build-time analyzer) instead of relying on loaded-assembly reflection. Files: `tests/Hexalith.Conversations.Contracts.Tests/ContractsAssemblyBoundaryTest.cs`, `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs`, `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`, `tests/Hexalith.Conversations.Tests/DomainBoundaryTest.cs`.
- Add a `Hexalith.Conversations.slnx` vs disk parity test. Adding or removing a `.csproj` on disk without updating the slnx (or vice versa) is currently undetected. Coverage enhancement; defer until project list churn increases.
- Make `Directory.Build.props` sibling-module root probes fail loudly when sibling folders are absent. Today probes use `Exists(...)` and silently leave properties empty. Only material when sibling references are actually wired (likely Story 1.10/1.11). File: `Directory.Build.props:11-13`.
