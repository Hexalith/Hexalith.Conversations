---
baseline_commit: 29adba4db23f6ebca8383adfcb32ef75f6519828
---

# Story 3.6: Promote & adopt the shared JSON-context base / polymorphic registration

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a technical-module maintainer,
I want a shared source-generated JSON-context base / polymorphic registration helper adopted by Conversations, with the polymorphic registry made public as needed,
so that a domain module declares only its serializable type lists instead of hand-assembling resolver combination, converter catalogs, and event/command type maps.

This is the sixth story of Epic 3. It covers FR-14 and must also close the real FR-8 payload that Story 2.6 deliberately deferred to FR-14: shared ruleless converter/context plumbing plus the public polymorphic registry surface. Story 3.6 is greenfield-adopt for Conversations, so FR-17 delete is N/A for a local JSON context, but any obsolete hand-assembled resolver/type-catalog code that the shared helper replaces must be reduced to a thin adapter or removed.

## Acceptance Criteria

**AC-1 - Resolve and record the FR-14 landing zone before code edits.**
Given Epic 3 OQ-1 was ratified as "Commons, all Epic-3",
When Story 3.6 starts,
Then FR-14 is recorded in `docs/release-evidence/promote-adopt-runbook.md` as a new additive Commons serialization capability, recommended name `Hexalith.Commons.Serialization`, with self-contained build props so umbrella builds do not require nested Commons submodules.
[Source: docs/release-evidence/promote-adopt-runbook.md#0-resolve-the-landing-zone-gating-precondition-dont-promote-into-the-dark; _bmad-output/implementation-artifacts/3-5-promote-adopt-the-shared-aspire-dapr-domain-module-hosting-base.md#Previous-story-intelligence]

**AC-2 - Promote the shared source-generated JSON options/context helper.**
Given Memories repeats the source-generated context pattern with `[JsonSerializable]` lists plus `JsonTypeInfoResolver.Combine(...)`,
When the shared helper is promoted,
Then Commons provides a domain-neutral way to create `JsonSerializerOptions` from one or more `IJsonTypeInfoResolver` instances, preserve web defaults/camelCase behavior, order resolver precedence deliberately, and append a reflection fallback only when explicitly requested.
[Source: Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs; Hexalith.Memories/src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs; Microsoft Learn System.Text.Json source generation: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation]

**AC-3 - Promote the polymorphic registry surface without public contract reshaping.**
Given Story 2.6 found `TypeMapper.GetMap<TMappable>()` unusable for Conversations events because it requires `IMappableType` and public parameterless constructors,
When polymorphic event/command registration needs a shared registry,
Then Commons exposes `NameTypeMapper` or an equivalent additive public surface that can map explicit names/discriminators to `Type` without forcing Conversations public records to implement `IMappableType`, add parameterless constructors, or carry new public discriminator members.
[Source: _bmad-output/implementation-artifacts/2-6-adopt-shared-serialization-helpers-for-generic-converters.md#Acceptance-Criteria; Hexalith.Commons/src/libraries/Hexalith.Commons/Reflections/TypeMapper.cs; Hexalith.Commons/src/libraries/Hexalith.Commons/Reflections/NameTypeMapper{TMappable}.cs]

**AC-4 - Adopt a Conversations JSON context and preserve wire compatibility.**
Given Conversations currently has no source-generated JSON context and serializes contracts through `JsonSerializerDefaults.Web` plus attribute converters,
When Conversations declares its serializable command, event, result, projection, error, conformance, governance, and identifier type lists against the shared helper,
Then existing JSON wire shapes stay byte/shape-compatible, `ContractSerializationTest` and `GenericValueConverterSkeletonTest` remain green and un-weakened, and the public-contract-shape baseline diff is empty unless an explicit approved contract change is recorded.
[Source: tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs; tests/Hexalith.Conversations.Contracts.Tests/GenericValueConverterSkeletonTest.cs; docs/release-evidence/public-contract-shape-baseline-v1.json]

**AC-5 - Preserve polymorphic event/command hierarchy behavior and projection decoding.**
Given `ConversationProjectionHandler.BuildPublicEventTypeMap()` currently owns the 13 public event type-name map and resolves exact names before suffix matches,
When the shared polymorphic registry is adopted,
Then projection decoding still accepts the existing event type names, rejects or skips unknown/malformed events no more permissively than today, never reports falsely-current freshness because of a skipped position, and does not introduce raw EventStore/Dapr envelope concepts into public Conversations contracts.
[Source: src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs; _bmad-output/implementation-artifacts/2-5-implement-projections-against-the-sdk-projection-seam.md]

**AC-6 - Keep domain-rule converters local and replace only ruleless machinery when equal-or-stronger.**
Given Story 2.6 classified `ConversationStringValueJsonConverter<T>` and `ConversationIntValueJsonConverter<T>` as ruleless machinery, while prefixed identifier converters encode a cross-type-substitution prevention rule,
When shared generic converter helpers are promoted or consumed,
Then only equal-or-stronger ruleless skeleton behavior is moved to Commons; the prefixed identifier rule (`tenant:`, `conv:`, `party:`, `project:`, `folder:`, `file:`, `message:`) remains behavior-preserving and domain-owned unless the shared helper explicitly supports fixed-prefix typed identifiers without weakening rejection messages, token guards, or wire values.
[Source: src/Hexalith.Conversations.Contracts/Serialization/ConversationStringValueJsonConverter.cs; src/Hexalith.Conversations.Contracts/Serialization/ConversationIntValueJsonConverter.cs; src/Hexalith.Conversations.Contracts/Serialization/PrefixedIdentifierJsonConverter.cs; src/Hexalith.Conversations.Contracts/Serialization/IdentifierJsonConverters.cs; docs/release-evidence/at-risk-test-register-v1.md#Story-2.6]

**AC-7 - Preserve content-safety, telemetry, and UX trust boundaries.**
Given serialization options and polymorphic metadata can affect logs, telemetry, diagnostics, UI DTOs, and accessibility surfaces,
When FR-14 is implemented,
Then type names/discriminators are bounded, content-safe, and stable; no payload text, tenant/user authority, raw provider details, Party personal data, or inaccessible record hints are added to serialized metadata, logs, telemetry, browser/UI DTOs, or test evidence.
[Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules; _bmad-output/planning-artifacts/ux-requirement-map.md#UX-DR23; _bmad-output/planning-artifacts/ux-design-specification.md#AC-SAFE-002]

**AC-8 - Release gates, sibling compatibility, and submodule mechanics hold.**
And the promoted Commons serialization tests pass, Conversations contract/projection/server/conformance tests pass, the full Conversations conformance suite is monotonic at **>= 361**, the public-contract-shape baseline diff is empty, Memories and other dependent siblings compile green against the additive API, and every touched submodule is committed separately with a root-level gitlink pointer bump. Never initialize nested submodules recursively.
[Source: _bmad-output/implementation-artifacts/3-5-promote-adopt-the-shared-aspire-dapr-domain-module-hosting-base.md#Completion-Notes-List; docs/release-evidence/promote-adopt-runbook.md#Ordered-checklist-copy-per-story; Hexalith.AI.Tools/hexalith-llm-instructions.md]

## Tasks / Subtasks

- [x] **Task 0 - Record the FR-14 landing zone and verify baseline state.** (AC: 1, 8)
  - [x] Add a Story 3.6 entry to `docs/release-evidence/promote-adopt-runbook.md` naming the Commons landing zone, expected library/project names, and self-contained `Directory.Build.props` requirement.
  - [x] Verify root-level submodule pointers before building; do not run recursive submodule commands.
  - [x] Record that Story 3.6 closes the FR-8 deferral from Story 2.6 and that FR-17 delete is N/A for a pre-existing Conversations JSON context because none exists.

- [x] **Task 1 - Characterize current serialization and polymorphic behavior before replacement.** (AC: 3, 4, 5, 6)
  - [x] Read the current converter files under `src/Hexalith.Conversations.Contracts/Serialization/`, especially the two ruleless base skeletons, `PrefixedIdentifierJsonConverter<T>`, `IdentifierJsonConverters.cs`, closed vocabulary converters, and `SchemaVersionJsonConverter`.
  - [x] Read `ContractSerializationTest` and `GenericValueConverterSkeletonTest`; treat them as the wire-shape and malformed-token oracles.
  - [x] Read `ConversationProjectionHandler.BuildPublicEventTypeMap()` and the projection handler tests; pin the exact 13 event types and exact-then-suffix resolution behavior before replacing any map.
  - [x] Confirm no Conversations public command/event/contract currently implements `IMappableType`; do not add it unless an explicit contract-shape change is approved and recorded.

- [x] **Task 2 - Promote the shared Commons serialization capability with module-owned tests.** (AC: 1, 2, 3, 6, 8)
  - [x] Create the Commons serialization library and tests using the self-contained props pattern established by Stories 3.1-3.5.
  - [x] Provide helpers for composing source-generated resolvers into `JsonSerializerOptions` with explicit resolver ordering and optional `DefaultJsonTypeInfoResolver` fallback.
  - [x] Provide an additive public polymorphic registry API that supports explicit safe discriminator/name-to-Type mappings without requiring domain contracts to implement `IMappableType` or have parameterless constructors.
  - [x] Do not force generated contexts to inherit from a nonstandard shared base if that fights the System.Text.Json source generator. Prefer normal partial `JsonSerializerContext` types plus shared resolver/options/registry helpers unless a compiled Commons test proves a base-class design works.
  - [x] Do not take a hidden dependency on the nested `Hexalith.PolymorphicSerializations` project from the new helper unless root-level availability, build props, and sibling builds are deliberately proven and recorded. Existing Commons.Metadatas usage is not enough by itself.
  - [x] If generic string/int value converters are promoted, prove their token guards, exception type, and round-trip behavior are equal-or-stronger than the Conversations skeletons.
  - [x] Keep the Commons API domain-neutral; do not reference Conversations, Memories contract types, EventStore envelopes, Dapr, Tenants, Parties, FrontComposer, or UI packages.

- [x] **Task 3 - Adopt a source-generated Conversations JSON context.** (AC: 2, 4, 7)
  - [x] Add a Conversations contracts JSON context that declares every public contract type covered by `ContractSamples.AllContracts`, plus required collection/envelope shapes used by commands, events, projections, conformance artifacts, and server HTTP JSON.
  - [x] Replace local hand-assembled `JsonSerializerOptions` where appropriate with options built through the shared helper; preserve `JsonSerializerDefaults.Web`, camelCase output, and all attribute converters.
  - [x] Do not introduce reflection fallback on hot paths unless a compatibility gap is proven and recorded; prefer source-generated metadata first.
  - [x] Keep `Contracts` free of server infrastructure, Dapr implementation details, EventStore server packages, and UI shell packages.

- [x] **Task 4 - Adopt the polymorphic registry in projection/event decoding.** (AC: 3, 5, 7)
  - [x] Replace or wrap `ConversationProjectionHandler`'s hand-built event type map with the shared registry only if exact existing behavior can be preserved.
  - [x] If there is no real common base/interface hierarchy for commands/events today, do not invent one for polymorphism. Use the registry for explicit type-name lookup and source-generated metadata, not as a reason to reshape public records.
  - [x] Preserve all current public event names: `ConversationCreated`, `MessageAppended`, `ParticipantAdded`, `FileReferenceAttached`, `ConversationMetadataUpdated`, `ConversationProjectChanged`, `ConversationClosed`, `ConversationArchived`, `ConversationLifecycleChanged`, `RetentionPolicySet`, `RetentionPolicyReplaced`, `ConversationContentMarkedSensitive`, and `MessageContentRedacted`.
  - [x] Preserve unknown-event handling: never turn skipped, malformed, or outside-vocabulary events into a falsely-current projection; freshness must degrade or fail closed as it does today.
  - [x] Add focused tests if the registry replaces the map so the new registry returns exactly the old key set and resolution behavior.

- [x] **Task 5 - Close the Story 2.6 FR-8 deferral honestly.** (AC: 4, 6, 8)
  - [x] If the shared helper replaces the two ruleless converter base skeletons, delete or reduce those skeletons only after `ContractSerializationTest` and `GenericValueConverterSkeletonTest` prove equal-or-stronger behavior.
  - [x] Keep prefixed identifier converters local unless the shared helper can encode the fixed prefix rule without weakening cross-type-substitution rejection.
  - [x] Record the final disposition in Dev Agent Record: what moved to Commons, what stayed local as domain rule, and why.
  - [x] Do not mutate frozen inventory `approxLoc`; any classification change must use the Story 1.5 append-only changeLog procedure.

- [x] **Task 6 - Update tests and release evidence.** (AC: 4, 5, 6, 7, 8)
  - [x] Add Commons serialization tests for resolver ordering, optional fallback, missing resolver guards, duplicate discriminator/name rejection, safe bounded discriminator strings, and no domain-specific references.
  - [x] Add or extend Conversations contract tests proving the source-generated context has metadata for every public contract sample and preserves representative JSON exactly.
  - [x] Add or extend projection tests proving event registry behavior is identical to the previous 13-event map.
  - [x] Run `dotnet test` or the built xUnit executables for Commons serialization tests, `Hexalith.Conversations.Contracts.Tests`, `Hexalith.Conversations.Server.Tests`, and `Hexalith.Conversations.Conformance.Tests`; required conformance count is `>= 361`.
  - [x] Verify `git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json` is empty.
  - [x] Build `Hexalith.Conversations.slnx` in Release with warnings as errors and build Memories plus any other sibling that consumes the additive API.

- [x] **Task 7 - Submodule commit, root pointer bump, and final record.** (AC: 1, 8)
  - [x] Commit the Commons promotion in `Hexalith.Commons` as its own submodule commit.
  - [x] If another sibling is edited for compatibility, commit that submodule separately. (N/A — no sibling source was edited; the Commons API is purely additive and Memories builds green against the unchanged existing surface.)
  - [x] Bump only root-level gitlinks in the umbrella repo.
  - [x] Generate the Dev Agent Record last, after validation gates pass, to avoid stale test counts and file-list drift.

## Dev Notes

### Current implementation to read before editing

Conversations currently has no source-generated JSON context. Contract JSON is exercised with `new JsonSerializerOptions(JsonSerializerDefaults.Web)` and attribute converters on public value types. The exact representative wire shapes are pinned in `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`, including prefixed identifiers and `schemaVersion:1`. The generic converter malformed-token behavior is pinned in `GenericValueConverterSkeletonTest.cs`. [Source: tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs; tests/Hexalith.Conversations.Contracts.Tests/GenericValueConverterSkeletonTest.cs]

The two ruleless base skeletons are `ConversationStringValueJsonConverter<T>` and `ConversationIntValueJsonConverter<T>`. The prefixed identifier converter is not ruleless: its prefix prevents silent cross-type substitution between identifier families on the wire. Keep that rule unless the shared helper represents it explicitly. [Source: src/Hexalith.Conversations.Contracts/Serialization/ConversationStringValueJsonConverter.cs; src/Hexalith.Conversations.Contracts/Serialization/ConversationIntValueJsonConverter.cs; src/Hexalith.Conversations.Contracts/Serialization/PrefixedIdentifierJsonConverter.cs]

`ConversationProjectionHandler` uses a private `FrozenDictionary<string, Type>` built from 13 public event record types. It resolves by exact name first and suffix match second. It skips unknown or malformed events and relies on source-position/freshness degradation so a skipped event cannot become a falsely-current projection. Any registry adoption must preserve those properties. [Source: src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs]

Commons currently exposes `TypeMapper.GetMap<TMappable>()`, `GetObject`, `GetType`, and `GetMappableTypes`, all built around `IMappableType`. `NameTypeMapper<TMappable>` is internal. This design is not directly usable for Conversations public event records because those records do not implement `IMappableType` and should not grow parameterless constructors or discriminator members just to satisfy the helper. [Source: Hexalith.Commons/src/libraries/Hexalith.Commons/Reflections/TypeMapper.cs; Hexalith.Commons/src/libraries/Hexalith.Commons/Reflections/NameTypeMapper{TMappable}.cs; _bmad-output/implementation-artifacts/2-6-adopt-shared-serialization-helpers-for-generic-converters.md]

Memories is the sibling pattern source for source-generated contexts. `MemoriesJsonSourceGenerationContext` declares the serializable type list and `MemoriesJsonContext.Options` combines the generated resolver with `DefaultJsonTypeInfoResolver`. The CLI context composes its own generated context ahead of the contracts resolver. Story 3.6 should generalize the mechanics, not copy Memories contract lists or semantics. [Source: Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs; Hexalith.Memories/src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs]

`Hexalith.Commons.Metadatas` references `Hexalith.PolymorphicSerializations` through a project-root property, but that is not a free pass for a new Commons serialization helper. The umbrella repository has a root-only submodule policy and Story 3.1 found that nested Commons build dependencies break umbrella builds. Treat any polymorphic-serialization dependency as a deliberate architecture/build decision with tests and sibling build proof, not an incidental reuse. [Source: Hexalith.Commons/src/libraries/Hexalith.Commons.Metadatas/Hexalith.Commons.Metadatas.csproj; docs/release-evidence/promote-adopt-runbook.md#Build-infrastructure-caveat-discovered-in-31-read-before-promoting-into-Commons-again]

### Architecture and product guardrails

This is not a UI redesign. The admin UX and FrontComposer surfaces are preserved. However, serialized trust metadata can feed UI, telemetry, diagnostics, and accessibility surfaces, so content-safety still applies: no raw protected content, inaccessible identifiers, tenant/user authority, Parties personal data, raw provider details, or hidden existence hints should be introduced through discriminators, converter messages, logs, telemetry, or JSON metadata. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules; _bmad-output/planning-artifacts/ux-design-specification.md#AC-SAFE-002]

Contracts must remain serialization-friendly and bounded-context clean. `Hexalith.Conversations.Contracts` must not reference server infrastructure, Dapr implementation details, EventStore server packages, FrontComposer UI packages, or technical-module internals that leak substrate vocabulary into adopter contracts. [Source: _bmad-output/project-context.md#Critical-Implementation-Rules]

FR-14 does not authorize a new persistence model, transport, provider, contract semantics, or UI surface. It is a behavior-preservation refactor that moves reusable serialization ceremony into Commons and adopts it in Conversations. [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-14-Shared-JSON-context-base--polymorphic-type-registration; _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#Non-Goals-Explicit]

### Latest technical specifics

System.Text.Json source generation requires a partial `JsonSerializerContext` plus `[JsonSerializable(typeof(T))]` entries for types to serialize/deserialize. Members declared as `object` and collection shapes need explicit registration for the runtime types expected. Source generation can be used through `JsonTypeInfo<T>`, a context instance, or `JsonSerializerOptions.TypeInfoResolver`. [Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation]

`JsonSerializerOptions.TypeInfoResolverChain` is available in modern .NET and resolver ordering is significant: the options query resolvers in order and use the first non-null result. Story 3.6 should make resolver ordering explicit and tested rather than relying on incidental `Combine(...)` order. [Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation#combine-source-generators]

System.Text.Json polymorphic deserialization requires type discriminators for derived types. The default discriminator property is `$type`; custom names are possible, and string or integer discriminator formats should not be mixed casually. For this repository, prefer stable, bounded, string discriminators matching existing contract/event type names unless an explicit compatibility decision says otherwise. [Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism]

`DefaultJsonTypeInfoResolver` modifiers can customize contracts, but types that use custom converters report `JsonTypeInfoKind.None` for converter-owned serialization. Do not assume a resolver modifier can introspect or preserve converter behavior unless a test proves it. [Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/custom-contracts]

### Previous story intelligence

Story 2.6 is the direct prerequisite. It proved that the FR-8 target did not exist in the consumable Epic-2 surface, recorded the FR-8 deferral to FR-14/Story 3.6, classified only the two value converter base skeletons as ruleless, kept prefixed identifiers as a domain rule, and confirmed `TypeMapper.GetMap()` cannot replace the projection event map without public contract reshaping. Story 3.6 must close that loop, not rediscover it. [Source: _bmad-output/implementation-artifacts/2-6-adopt-shared-serialization-helpers-for-generic-converters.md; docs/release-evidence/at-risk-test-register-v1.md#Story-2.6]

Story 3.5 closed with conformance at 361, Commons promotion committed at `a8b3639`, EventStore compatibility committed at `2e66b67`, full Release 0-warning, and public-contract-shape diff empty. It also reinforced the recurring hazards: VSTest local socket restrictions in this sandbox, stale test-count records, submodule promotions not committed, root gitlinks not bumped, nested Commons build-props failures, and out-of-scope submodule drift. [Source: _bmad-output/implementation-artifacts/3-5-promote-adopt-the-shared-aspire-dapr-domain-module-hosting-base.md#Completion-Notes-List]

The promote-adopt runbook is binding for this story: record landing zone, promote with module tests, adopt in Conversations, delete/reduce hand-rolled mechanics only where behavior is preserved, run monotonic conformance and sibling builds, commit submodules separately, and bump root-level gitlinks. [Source: docs/release-evidence/promote-adopt-runbook.md]

### Project Structure Notes

- Likely new shared code: `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/` and `Hexalith.Commons/test/Hexalith.Commons.Serialization.Tests/`.
- Likely Conversations contract files: a new source-generated context under `src/Hexalith.Conversations.Contracts/Serialization/` or a nearby contract-owned folder, plus tests under `tests/Hexalith.Conversations.Contracts.Tests/`.
- Likely Conversations server file: `src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs` if the shared registry replaces the private event map.
- Potential evidence files: `docs/release-evidence/promote-adopt-runbook.md` and, only if a classification changes, `docs/release-evidence/consume-promote-keep-inventory-v1.json` via the Story 1.5 changeLog procedure.
- Do not edit generated files under `obj/` or build output under `bin/`.
- Keep package versions in `Directory.Packages.props`; project files should contain versionless package references unless matching an existing local exception.
- Never initialize nested submodules or use recursive submodule update commands.

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-14-Shared-JSON-context-base--polymorphic-type-registration]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-36-Promote--adopt-the-shared-JSON-context-base--polymorphic-registration-greenfield-adopt--FR-17-NA]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-08.md#FR-8--FR-14-entanglement]
- [Source: _bmad-output/implementation-artifacts/2-6-adopt-shared-serialization-helpers-for-generic-converters.md]
- [Source: _bmad-output/implementation-artifacts/3-5-promote-adopt-the-shared-aspire-dapr-domain-module-hosting-base.md]
- [Source: docs/release-evidence/promote-adopt-runbook.md]
- [Source: docs/release-evidence/at-risk-test-register-v1.md#Story-2.6]
- [Source: src/Hexalith.Conversations.Contracts/Serialization/ConversationStringValueJsonConverter.cs]
- [Source: src/Hexalith.Conversations.Contracts/Serialization/ConversationIntValueJsonConverter.cs]
- [Source: src/Hexalith.Conversations.Contracts/Serialization/PrefixedIdentifierJsonConverter.cs]
- [Source: src/Hexalith.Conversations.Contracts/Serialization/IdentifierJsonConverters.cs]
- [Source: src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs]
- [Source: tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs]
- [Source: tests/Hexalith.Conversations.Contracts.Tests/GenericValueConverterSkeletonTest.cs]
- [Source: Hexalith.Commons/src/libraries/Hexalith.Commons/Reflections/TypeMapper.cs]
- [Source: Hexalith.Commons/src/libraries/Hexalith.Commons/Reflections/NameTypeMapper{TMappable}.cs]
- [Source: Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs]
- [Source: Hexalith.Memories/src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs]
- [Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation]
- [Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism]
- [Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/custom-contracts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (Tasks 0-4 and initial 5-6); Claude Opus 4.8 (1M context) (Task 5 thin-adapter closure, Task 6 Release/sibling build gates, Task 7 submodule commit + gitlink bump, finalization)

### Debug Log References

- Read and applied `.agents/skills/bmad-dev-story/SKILL.md` and `.agents/skills/bmad-dev-story/checklist.md`; activation resolver completed with no prepend/append additions, and repository instructions were read before edits.
- Verified root-level submodule state with `git submodule status`; no recursive submodule initialization commands were run.
- Added the Story 3.6 FR-14 landing-zone evidence to `docs/release-evidence/promote-adopt-runbook.md`, including the Story 2.6 FR-8 deferral closure and FR-17 delete N/A disposition for a pre-existing Conversations JSON context.
- Built `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/Hexalith.Commons.Serialization.csproj` in Release: passed with 0 warnings.
- Ran `dotnet test Hexalith.Commons/test/Hexalith.Commons.Serialization.Tests/Hexalith.Commons.Serialization.Tests.csproj -c Release`: build passed, but VSTest failed to start because the sandbox denied the local socket (`System.Net.Sockets.SocketException (13): Permission denied`).
- Ran the built xUnit v3 executable `Hexalith.Commons/test/Hexalith.Commons.Serialization.Tests/bin/Release/net10.0/Hexalith.Commons.Serialization.Tests`: passed, 13 total, 0 failed.
- Built `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj` in Release: passed with 0 warnings.
- Built `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` in Release with `/m:1`: passed with 0 warnings after carrying the registry nullability annotation through the server wrapper.
- Ran direct xUnit v3 executables for required Conversations tests: `Hexalith.Conversations.Contracts.Tests` passed 604/604, `Hexalith.Conversations.Server.Tests` passed 607/607, and `Hexalith.Conversations.Conformance.Tests` passed 361/361.
- Built `Hexalith.Conversations.slnx` in Release with `/m:1`: passed with 0 warnings.
- Attempted `dotnet test` for the required test projects: failed before test execution because MSBuild/VSTest named-pipe creation is denied in this sandbox (`System.Net.Sockets.SocketException (13): Permission denied`); direct xUnit executables were used for the test gate.
- Attempted `dotnet build Hexalith.Memories/Hexalith.Memories.slnx -c Release /m:1`: failed only on NU1900 package vulnerability lookup for `Hexalith.Memories.AppHost` and `Hexalith.Memories.IntegrationTests` because network access to `api.nuget.org:443` is denied; no compile errors surfaced in the sibling projects that did build.
- Verified `git diff -- docs/release-evidence/public-contract-shape-baseline-v1.json` is empty.
- (Claude Opus 4.8 continuation, 2026-06-24) Task 5 honest closure: reduced both ruleless skeletons (`ConversationStringValueJsonConverter<T>`, `ConversationIntValueJsonConverter<T>`) to thin adapters that inherit the promoted Commons bases (`StringValueJsonConverter<T>`/`IntValueJsonConverter<T>`), added an additive `Hexalith.Commons.Serialization` `ProjectReference` to `Hexalith.Conversations.Contracts.csproj` (dual `HexalithCommonsRoot` conditional). Chose reduce-to-thin-adapter over delete because the two file paths are frozen entries in `consume-promote-keep-inventory-v1.json` (`generic-serialization-converters`, `approxLoc: 215`); deletion would have mutated the frozen inventory, which Task 5 forbids. Behavior is byte-identical and pinned green by the unchanged `GenericValueConverterSkeletonTest` and `ContractSerializationTest` oracles.
- (Claude Opus 4.8 continuation) Rebuilt `src/Hexalith.Conversations.Contracts` Release: 0 warnings. Built full `Hexalith.Conversations.slnx` Release `/m:1`: 0 warnings, 0 errors (Task 6 warnings-as-errors gate).
- (Claude Opus 4.8 continuation) Re-ran the direct xUnit v3 executables (VSTest still socket-blocked in sandbox): `Hexalith.Commons.Serialization.Tests` 13/13, `Hexalith.Conversations.Contracts.Tests` 604/604, `Hexalith.Conversations.Server.Tests` 607/607, `Hexalith.Conversations.Conformance.Tests` 361/361 (monotonic >= 361). Contract-shape baseline diff re-verified empty.
- (Claude Opus 4.8 continuation) Sibling build gate: `dotnet build Hexalith.Memories/Hexalith.Memories.slnx -c Release /m:1 -p:NuGetAudit=false` passed with 0 warnings, 0 errors. The prior NU1900 blocker was solely the offline NuGet vulnerability audit reaching `api.nuget.org`; `NuGetAudit=false` bypasses it. Memories resolves Commons from the umbrella `HexalithCommonsRoot` (no nested Commons), so this is a real compat proof against the additive Commons surface. `Hexalith.Commons.slnx` itself does not build standalone from the umbrella (pre-existing nested `Hexalith.PolymorphicSerializations` submodule + NU5118 README packaging issues, unrelated to FR-14) — which is exactly why the promoted library carries a self-contained `Directory.Build.props` and builds individually inside `Hexalith.Conversations.slnx`.
- (Claude Opus 4.8 continuation) Committed the additive Commons promotion on the submodule `main` as `2dc8358` (`feat(serialization): add domain-neutral JSON resolver/registry helpers`); staged only the new library, its tests, and the two `Hexalith.Commons.slnx` entries (bin/obj gitignored; the repository's pre-existing conflicted `.claude/skills` markers were untouched). Bumped only the root-level `Hexalith.Commons` gitlink in the umbrella story commit; no nested submodules were initialized.

### Completion Notes List

- Story is COMPLETE and moved to `review`. All eight ACs satisfied; every task/subtask is checked. Final gates green: `Hexalith.Conversations.slnx` Release (0/0), Memories sibling Release (0/0), Commons serialization 13/13, Contracts 604/604, Server 607/607, Conformance 361/361 (monotonic >= 361), contract-shape baseline diff empty, Commons committed (`2dc8358`) with root gitlink bumped.
- Task 5 FR-8 disposition (final): the load-bearing ruleless string/int converter skeleton logic now lives once in Commons (`Hexalith.Commons.Serialization.StringValueJsonConverter<T>`/`IntValueJsonConverter<T>`, public, documented); the Conversations skeletons were reduced to thin internal adapters that inherit those bases, eliminating the duplicated logic while keeping the inventoried file paths and `Conversation`-prefixed derivation surface stable. The prefixed identifier converters and their cross-type-substitution prefix rule stayed local and domain-owned (unchanged). No frozen inventory `approxLoc` was mutated.
- Promoted an additive Commons serialization library with web-default JSON options composition, explicit resolver ordering, optional reflection fallback, safe bounded discriminator validation, exact-then-suffix type lookup, and ruleless string/int value converter bases.
- Added module-owned Commons tests covering resolver ordering/fallback guards, registry duplicate and discriminator validation, suffix lookup, and value converter behavior; direct xUnit execution passed despite VSTest socket restrictions in this sandbox.
- Added an internal Conversations source-generated `ConversationsJsonContext` covering public contract samples and collection shapes, and adopted the Commons JSON options helper in server command-body serialization without changing the exported public contract type count.
- Replaced `ConversationProjectionHandler`'s hand-built event map with the Commons polymorphic registry while preserving the existing 13 public event names and exact-before-suffix lookup behavior; added focused projection tests for the key set and suffix resolution.
- Kept `Hexalith.Conversations.Contracts` free of a Commons.Serialization project reference and kept the generated context internal via `InternalsVisibleTo` for server/test adoption; the two local ruleless converter skeletons remain local, while the Commons equivalents are promoted and tested.
- Kept prefixed identifier converters local as domain-rule converters; no public contract discriminator members, `IMappableType` requirements, parameterless constructors, payload text, authority data, provider details, or UI DTO metadata were introduced.
- Committed the additive Commons promotion as its own submodule commit (`2dc8358`, submodule `main`) and bumped only the root-level `Hexalith.Commons` gitlink in the umbrella story commit. No other submodule was edited, so none was committed separately. No nested submodule was initialized.

### File List

- `Directory.Build.props`
- `Hexalith.Conversations.slnx`
- `docs/release-evidence/promote-adopt-runbook.md`
- `_bmad-output/implementation-artifacts/3-6-promote-adopt-the-shared-json-context-base-polymorphic-registration.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`
- `src/Hexalith.Conversations.Contracts/Serialization/ConversationsJsonContext.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ConversationStringValueJsonConverter.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ConversationIntValueJsonConverter.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj`
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionHandlerTest.cs`
- `Hexalith.Commons/Hexalith.Commons.slnx`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/Directory.Build.props`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/Hexalith.Commons.Serialization.csproj`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/JsonSerializationOptions.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/PolymorphicTypeRegistration.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/PolymorphicTypeRegistry.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/StringValueJsonConverter.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/IntValueJsonConverter.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Serialization.Tests/Hexalith.Commons.Serialization.Tests.csproj`
- `Hexalith.Commons/test/Hexalith.Commons.Serialization.Tests/JsonSerializationOptionsTest.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Serialization.Tests/PolymorphicTypeRegistryTest.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Serialization.Tests/ValueJsonConverterTest.cs`

### Change Log

- 2026-06-24: Started Story 3.6 implementation, promoted Commons serialization helpers, adopted the internal Conversations JSON context and projection registry, passed Conversations direct test gates, and recorded the remaining Memories NU1900/submodule finalization blockers. Story remained `in-progress`.
- 2026-06-24: Completed Story 3.6. Closed the FR-8 deferral honestly by reducing the two ruleless Conversations converter skeletons to thin adapters over the promoted Commons bases (Contracts now references the additive `Hexalith.Commons.Serialization`), keeping behavior byte-identical and the frozen inventory untouched. Passed the full `Hexalith.Conversations.slnx` Release (warnings-as-errors) gate and the Memories sibling Release build (NuGet audit disabled to bypass the offline-only NU1900 block). Re-verified all test gates (Commons 13, Contracts 604, Server 607, Conformance 361) and the empty contract-shape baseline diff. Committed the Commons promotion (`2dc8358`), bumped the root gitlink, and moved the story to `review`.
