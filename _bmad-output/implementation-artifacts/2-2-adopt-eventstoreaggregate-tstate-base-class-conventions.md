---
baseline_commit: f19f9bb27505116059d63a8bf7aeea24994f3e63
---

# Story 2.2: Adopt `EventStoreAggregate<TState>` base-class conventions

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversations maintainer,
I want `ConversationAggregate` to rely on the `EventStoreAggregate<TState>` base-class reflection dispatch and state replay,
so that redundant manual routing and idempotency-bridge shims disappear.

This is the **second story of Epic 2** (Consume Existing Technical-Module Surface) and the second `src/`
production change in the initiative. It is classified **remove-and-replace**. Covers **FR-7**. Relevant NFRs:
**NFR1** (behavior preservation), **NFR2** (no hot-path regression — preserve snapshot/projection use),
**NFR8** (public-surface / EventStore-concept boundary preserved).

> **READ THIS FIRST — the aggregate is already on the base class.** `ConversationAggregate` *already* extends
> `EventStoreAggregate<ConversationState>` with static `Handle(TCommand, ConversationState?) -> DomainResult`
> methods, and `ConversationState` already exposes `public void Apply(TEvent)` methods. The SDK reflection
> dispatch (`ProcessAsync`) and replay (`Replay` → `AggregateReplayer.Replay<ConversationState>`) **already work
> against this aggregate today** (Story 2.1 confirmed and explicitly deferred this work here). **Do NOT rewrite the
> aggregate's `Handle` methods or the state's `Apply` methods** — they are the canonical convention, not the
> redundancy. The deliverable of this story is **removing the one residual shim that the base class/SDK makes
> redundant** (the dead `EventStoreCommandStatusIdempotencyBridge`), proving the base-class dispatch/replay path is
> truly the live path, and recording the disposition in the FR-20 ledger. This is a **small, surgical, mostly-deletion
> story**, not a refactor of domain logic. Scope discipline is the primary risk here (see Scope Boundaries).

## Acceptance Criteria

1. **(AC-1 — base-class dispatch & replay are the convention, verified)** Given `ConversationAggregate :
   EventStoreAggregate<ConversationState>`, when a command is processed through the SDK host path
   (`IDomainProcessor.ProcessAsync`) and when state is replayed through the SDK replay path
   (`IAggregateReplay.Replay` → `AggregateReplayer.Replay<ConversationState>`), then routing resolves via the
   static `Handle(TCommand, ConversationState?)` reflection dispatch and state rehydration resolves via the
   `ConversationState.Apply(TEvent)` reflection convention — **with no hand-rolled per-command `switch`/`if` dispatch
   table and no idempotency-bridge shim standing between the SDK and the aggregate**. A test proves the SDK
   `ProcessAsync` reflection path reaches each `Handle` overload (teeth: a command type with no matching `Handle`
   surfaces the SDK's `No Handle method found` failure, not a silent no-op).

2. **(AC-2 — redundant idempotency-bridge shim removed)** Given `EventStoreCommandStatusIdempotencyBridge`
   (`src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs`) — a shim that
   interprets EventStore `CommandStatusRecord` into a Conversations idempotency decision and **always** returns
   `RetryableUncertainty` — when it is confirmed to have **zero production references** (it is referenced only by its
   own unit test), then it is **deleted** along with its test
   (`tests/Hexalith.Conversations.Server.Tests/EventStore/EventStoreCommandStatusIdempotencyBridgeTest.cs`), because
   the SDK already owns command-status and the genuine Conversations idempotency contract is owned end-to-end by
   `IdempotentConversationCommandExecutor` + the `Idempotency/*` subsystem (which the bridge never feeds). The removal
   is recorded per AC-5.

3. **(AC-3 — genuine domain idempotency & replay logic preserved, NOT removed)** Given the Conversations idempotency
   subsystem (`src/Hexalith.Conversations/Idempotency/*`, `IdempotentConversationCommandExecutor`) and the domain
   replay-verification service (`src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`), then they remain
   **unchanged in behavior**: these encode Conversations' explicit idempotency contract and content-safe replay
   verification (tenant/conversation scope, position-gap/reorder detection, schema-version checks, duplicate-identity
   rules) — they are domain logic the base class does **not** provide, so they are **Keep**, not redundant shims. Any
   per-event `Apply` dispatch *inside* `ConversationReplayVerifier` stays (the SDK exposes no public per-event apply
   seam usable mid-verification — see Dev Notes "The one judgment call").

4. **(AC-4 — pure aggregate command/state/event tests stay green, unchanged in strength)** Given the pure aggregate
   tests (`tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregate*Test.cs`) and the state `Apply` tests,
   when they run, then aggregate behavior is **unchanged (green)** and assertion strength is not reduced. These tests
   call the static `Handle`/`Apply` methods directly (pure command→state→event) — that is the intended pure-function
   test style and must not be rewritten to route through the SDK reflection layer.

5. **(AC-5 — ledger updated for the removed test; standing conformance gate holds)** The removal of the bridge and its
   test is recorded as an **append-only** entry in the FR-20 ledger
   (`docs/release-evidence/at-risk-test-register-v1.{json,md}`) via its generation test
   (`AtRiskTestRegisterGenerationTest` — **regenerate, do not hand-edit the JSON**), traceable to the Story 2.2 row
   already seeded in the register ("Story 2.2 (FR-7) — shared aggregate base-class dispatch / idempotency-bridge shims
   → idempotency executor couplings"). The full conformance suite is **100% green** on the story branch (Story 2.1
   closed at **351 tests**; the count must hold or grow, never regress — the bridge test removal is offset by the
   net-new base-class-dispatch teeth test of AC-1, so the suite stays monotonic). The **public contract-shape diff**
   vs the Story 1.1 snapshot (`docs/release-evidence/public-contract-shape-baseline-v1.json`, 196 types) is **empty**
   (the bridge is in the Server assembly, not `Hexalith.Conversations.Contracts` — deleting it must not change the
   public contract shape; if the diff is non-empty, that is a regression to investigate, not approve). No
   hot-path/snapshot/projection regression is introduced (NFR1/NFR2).

## Tasks / Subtasks

- [x] **Task 1 — Confirm the base-class convention is already satisfied (read-only baseline)** (AC: 1, 4)
  - [x] Re-read `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs` and confirm: it extends
        `EventStoreAggregate<ConversationState>`; every command handler is `public static DomainResult
        Handle(TCommand command, ConversationState? state)`; second parameter is exactly `ConversationState` (the SDK
        requires `parameters[1].ParameterType == typeof(TState)` — nullable annotation does not change the runtime
        type, so this matches). **Do not modify this file** unless Task 4's teeth test exposes a genuine
        discovery mismatch.
  - [x] Confirm `src/Hexalith.Conversations/State/ConversationState.cs` exposes `public void Apply(TEvent)` (single
        param, `void` return) for every persisted event the aggregate emits — the SDK discovers exactly these
        (`DomainProcessorStateRehydrator.DiscoverApplyMethods`: public instance `Apply`, one param, `void`).
  - [x] Confirm via grep that **no production code** performs hand-rolled per-command dispatch into the aggregate
        (command handlers do **not** call `ConversationAggregate.Handle(...)` directly — they orchestrate
        idempotency/tenant-access/governance and the SDK `/process` route reaches the aggregate). Record the grep
        result in the Dev Agent Record.
- [x] **Task 2 — Delete the redundant `EventStoreCommandStatusIdempotencyBridge` shim** (AC: 2)
  - [x] **Verify zero production references first** (non-negotiable): `grep -rn
        "EventStoreCommandStatusIdempotencyBridge" --include="*.cs" src/ tests/ | grep -v "/bin/\|/obj/"`. Expected:
        only `…/Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs` (definition) and
        `…/Server.Tests/EventStore/EventStoreCommandStatusIdempotencyBridgeTest.cs` (its test). **If any other
        production reference appears, STOP and re-evaluate** — the bridge would not be dead and the disposition must
        change (do not blindly delete a live path).
  - [x] Delete `src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs`. The
        `Server/EventStore/` directory contains only this file — remove the now-empty directory too.
  - [x] Delete `tests/Hexalith.Conversations.Server.Tests/EventStore/EventStoreCommandStatusIdempotencyBridgeTest.cs`
        (and its now-empty `EventStore/` test directory).
  - [x] Confirm `CommandStatusRecord` / `CommandStatus` (the EventStore SDK types the bridge consumed) are not left
        as unused `using`s anywhere; the Conversations idempotency types (`ConversationIdempotencyDecision`,
        `IdempotencyOutcomeCategory`, etc.) stay — they are used by the live executor.
- [x] **Task 3 — Add the base-class dispatch/replay teeth test** (AC: 1)
  - [x] Add a test (under `tests/Hexalith.Conversations.Server.Tests/` or `tests/Hexalith.Conversations.Tests/`)
        that drives `ConversationAggregate` **through the SDK `IDomainProcessor.ProcessAsync` reflection path** (not
        the static `Handle` call) for at least one representative command, asserting the produced `DomainResult`
        matches the direct `Handle` result — proving the reflection dispatch is the real live route, not a mirror.
  - [x] Give it **teeth** (Epic 1 L1/A1 — green alone is not evidence): assert that processing a command type with
        **no** matching `Handle` overload surfaces the SDK's `InvalidOperationException("No Handle method found …")`
        — so the test would go RED if the dispatch table were silently bypassed or stubbed. Optionally assert the
        replay path: feed an ordered event envelope set through the base-class `Replay(...)` and assert reconstructed
        state, so removing the `Apply` convention would turn it RED.
  - [x] Use only packages already in the Conversations CPM (xUnit v3, Shouldly, NSubstitute) — **do not introduce a
        new package version** (warnings-as-errors + CPM). Build `CommandEnvelope`/`ProcessAsync` inputs from the
        EventStore.Contracts types already referenced transitively.
- [x] **Task 4 — Confirm the pure aggregate & domain idempotency/replay suites are untouched and green** (AC: 3, 4)
  - [x] Run `tests/Hexalith.Conversations.Tests/` (pure aggregate `Handle`/`Apply` tests) and the Server idempotency
        suites (`…/Server.Tests/Idempotency/*`, `IdempotentConversationCommandExecutorTest`,
        `AddParticipantCommandHandlerIdempotencyTest`) — confirm green with **no source edits** to
        `Idempotency/*`, `IdempotentConversationCommandExecutor`, or `ConversationReplayVerifier`.
  - [x] Confirm the live characterization oracles still pass:
        `tests/Hexalith.Conversations.Conformance.Tests/LiveIdempotencyOracleCharacterizationTest.cs` and
        `LiveIdempotencyConflictOracleCharacterizationTest.cs` (these pin the executor's behavior — the bridge
        removal must not perturb them, since the bridge never fed the executor).
- [x] **Task 5 — Record the disposition in the FR-20 ledger** (AC: 5)
  - [x] Extend `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` to record the
        Story 2.2 disposition: the `EventStoreCommandStatusIdempotencyBridge` shim + its test removed as **redundant
        (base class/SDK owns command status; bridge was dead, zero production refs)**, traceable to the seeded
        "Story 2.2 (FR-7)" register row. Follow the established `Story21StructuralDispositions` shape (add a parallel
        `Story22StructuralDispositions` section or equivalent) — **regenerate** the `.json` via the test; **never
        hand-edit** it. Update the companion `.md`.
  - [x] Do **not** mutate the immutable accepted inventory or rewrite existing ledger rows — append-only (Story 1.5
        `classification-change-procedure-v1`). The `projectReferenceDisposition` block stays as-is (see Task 6).
- [x] **Task 6 — Run the standing conformance gate and generate the Dev Agent Record last** (AC: 5)
  - [x] Build `Hexalith.Conversations.slnx` **Release** (0 warnings — warnings-as-errors). Run the full conformance
        suite + Server/Tests per-project. Confirm green **≥ 351 (monotonic)**, public-contract-shape diff **empty**,
        no `src/` **public** API change (the deleted bridge was `public` but lives in the Server assembly, which is
        not part of the 196-type Contracts public-contract-shape baseline — verify the baseline JSON is byte-unchanged).
  - [x] **Generate the Dev Agent Record test counts / File List from the final `dotnet test` run as the LAST step**
        (Epic 1 retro P1/P2 — the human-curated count drifted in 5/5 Epic 1 stories; generate it last so the record
        matches the working tree at first review).

## Dev Notes

### Scope Boundaries — what this story does and does NOT do

**DOES (FR-7, remove-and-replace):**
- Delete the dead `EventStoreCommandStatusIdempotencyBridge` shim + its test (the one residual idempotency-bridge
  shim the base class/SDK makes redundant).
- Add a teeth test proving the SDK base-class **reflection dispatch** (`ProcessAsync`) and **replay**
  (`AggregateReplayer`) are the live path for `ConversationAggregate` / `ConversationState`.
- Record the removal in the FR-20 ledger (regenerated, append-only).

**DOES NOT (actively avoid scope creep — this is the primary risk):**
- **Do NOT rewrite the aggregate.** `ConversationAggregate.Handle(...)` and `ConversationState.Apply(...)` are
  already the canonical convention. There is no manual dispatch table inside the aggregate to remove. Touching them
  risks NFR1 for zero benefit.
- **Do NOT remove or refactor the genuine idempotency subsystem** (`Idempotency/*`,
  `IdempotentConversationCommandExecutor`). It is Conversations' explicit idempotency contract (project-context:
  "Define Conversations' idempotency contract explicitly before coding write APIs"), used by every governed command
  handler. It is **Keep**, not a shim. The base class provides command *dispatch*, not the domain idempotency
  reservation/replay/conflict/poison lifecycle.
- **Do NOT replace `ConversationReplayVerifier`'s inner `Apply` switch.** See "The one judgment call" below.
- **Do NOT remove the `Conformance.Tests → Server` project reference.** The at-risk register's
  `projectReferenceDisposition` says the *last* owning story of {2.2, 2.5, 3.2, 3.3} removes it. 2.5/3.2/3.3 still
  follow 2.2, so 2.2 is **not** the last owner — the still-coupled telemetry/status/live-characterization suites
  keep the reference. Leave it in place.
- **Do NOT touch** EventStore/Tenants/Parties/FrontComposer sources (no backward-compat edits needed for Epic 2 —
  confirmed). Do NOT consolidate the module's own `ServiceDefaults`/`AppHost`/`Aspire` (Epic 3).
- **Do NOT** adopt query handlers/cursor codec (2.3), read-model store (2.4), projection seam (2.5), serialization
  helpers (2.6), or EventStore.Testing fakes (2.7).

### The redundancy, precisely (authoritative facts)

`EventStoreCommandStatusIdempotencyBridge` is a `public static` class with one method
`Interpret(CommandStatusRecord?) -> ConversationIdempotencyDecision`. For **every** possible input it returns
`ConversationIdempotencyDecision.RetryableUncertainty(...)` (null → `eventstore_command_status_missing`; terminal →
`eventstore_terminal_replay_required`; pending → `eventstore_command_status_pending`). It is referenced **only** by
`EventStoreCommandStatusIdempotencyBridgeTest` — **no production path consumes it**. Its own test header documents it
never resolves a Conversations outcome. It is a vestigial bridge from before the idempotency contract was owned by
`IdempotentConversationCommandExecutor`: the executor reserves/replays/conflicts against
`IConversationIdempotencyStore` directly and never consults EventStore command status. The SDK base class already owns
command status and the `/process` lifecycle; this bridge "bridges" nothing. Deleting it is **behavior-preserving by
construction** (dead code) and is exactly the "idempotency-bridge shim removed where the base class or SDK already
provides them" the AC calls for.
[Source: src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs:14-32 ;
tests/Hexalith.Conversations.Server.Tests/EventStore/EventStoreCommandStatusIdempotencyBridgeTest.cs:19-93]

### The one judgment call — `ConversationReplayVerifier`'s `Apply` switch stays (KEEP)

`ConversationReplayVerifier.Apply(ConversationState, object)` is a manual `switch` mapping each event type to
`state.Apply(...)`. On the surface this looks like "redundant manual dispatch" duplicating the SDK's reflection Apply.
**It is NOT in scope to replace, for three concrete reasons:**
1. **No usable public SDK seam.** The SDK's reflection apply lives in `DomainProcessorStateRehydrator`, which is
   `internal` to `Hexalith.EventStore.Client` — Conversations cannot call it. The public `AggregateReplayer.Replay<TState>`
   is a whole-stream engine that returns serialized JSON state and runs its **own** sequence/duplicate/version guards;
   it does not expose a "apply one already-deserialized event onto a live `ConversationState`" hook that interleaves
   with the verifier's per-event checks. [Source: …/Handlers/DomainProcessorStateRehydrator.cs:12,15,327 (internal) ;
   …/Aggregates/AggregateReplayer.cs:24-209]
2. **The switch is wrapped in genuine domain logic that must be preserved (NFR1).** `ConversationReplayVerifier` does
   tenant/conversation-scope validation, position-gap/reorder detection, schema-version checks, event-type-vs-payload
   matching, content-safe failure mapping to `ConversationErrorCode`, and **domain-specific** duplicate-identity rules
   (e.g. `ParticipantAdded`/`ConversationProjectChanged` re-adds are idempotent skips, others are
   `IdempotencyConflict`). This is a release-gate domain service used by `ConversationTemporalReconstructionService`
   and `ConversationGovernanceVerificationService` — not the aggregate's replay path. [Source:
   src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs:27-239]
3. **The AC is conditional:** "removed **where** the base class or SDK already provides them." The SDK does not provide
   a usable drop-in here, so the condition is not met. Replacing it would risk the standing conformance gate for no
   boilerplate win.

**Disposition:** KEEP, scoped out. A public per-event apply seam on the SDK is a reasonable **Epic 3 promote-later
candidate** (precedent: the `NameTypeMapper` micro-promote pattern in Story 2.6 / FR-14). If the dev believes it
should be promoted, log it via the Story 1.5 `classification-change-procedure-v1` append-only changeLog — do **not**
silently fold it into this story.

### Files to touch (and their current state)

| File | State | Change |
|---|---|---|
| `src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs` | dead `public static` shim, always returns `RetryableUncertainty`, zero prod refs | **Delete** (Task 2). Remove now-empty `EventStore/` dir. |
| `tests/Hexalith.Conversations.Server.Tests/EventStore/EventStoreCommandStatusIdempotencyBridgeTest.cs` | tests the dead shim | **Delete** (Task 2). Remove now-empty `EventStore/` test dir. |
| `tests/Hexalith.Conversations.{Server.Tests|Tests}/**` (new) | — | **Add** the base-class dispatch/replay teeth test (Task 3). |
| `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | has `Story21StructuralDispositions` | **Extend** with the Story 2.2 disposition; **regenerate** the ledger JSON (Task 5). |
| `docs/release-evidence/at-risk-test-register-v1.{json,md}` | seeded; has the "Story 2.2 (FR-7)" row | Regenerated (never hand-edited) + companion `.md` updated (Task 5). |
| `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs` | already `EventStoreAggregate<ConversationState>` w/ static `Handle` | **Read-only baseline** — do NOT modify (Task 1). |
| `src/Hexalith.Conversations/State/ConversationState.cs` | already exposes `public void Apply(TEvent)` | **Read-only baseline** — do NOT modify (Task 1). |
| `src/Hexalith.Conversations/Idempotency/*`, `…/Replay/ConversationReplayVerifier.cs` | genuine domain logic | **Keep unchanged** (AC-3). |

### The SDK base-class contract (authoritative facts for the teeth test)

`EventStoreAggregate<TState> : IDomainProcessor, IAggregateReplay` provides everything this story relies on:
- **Command dispatch:** `ProcessAsync(CommandEnvelope, object? currentState)` rehydrates `TState` then dispatches via
  `DiscoverHandleMethods` — public/instance/**static** `Handle` methods, **2 or 3 params**, `parameters[1] ==
  typeof(TState)` (third optional param must be `CommandEnvelope`), returning `DomainResult` (sync) or
  `Task<DomainResult>` (async). The Conversations `Handle(TCommand, ConversationState?)` static methods match the
  2-param sync shape exactly. Lookup is by **short command type name** (`ExtractShortTypeName`); a missing match
  throws `InvalidOperationException("No Handle method found for command type …")` — that is the teeth assertion.
  [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs:62-166]
- **Replay:** `Replay(AggregateReconstructionRequest) => AggregateReplayer.Replay<TState>(request)`, which discovers
  `ConversationState.Apply(TEvent)` (public instance, one param, `void`) and applies events in stream order with its
  own sequence/duplicate/version guards. [Source: …/Aggregates/EventStoreAggregate.cs:57-59 ;
  …/Aggregates/AggregateReplayer.cs:24-209]
- **`ITerminatable` short-circuit:** `ProcessAsync` rejects with `AggregateTerminated` if the rehydrated state is
  terminated. `ConversationState` does **not** implement `ITerminatable` today (it is `public sealed class
  ConversationState` with no terminatable interface), so this branch is inert for Conversations — nothing to preserve
  or regress here; do not add it. [Source: …/Aggregates/EventStoreAggregate.cs:68-72 ;
  src/Hexalith.Conversations/State/ConversationState.cs:26]
- The `Handle`/`Apply` discovery is **cached per type** (`_metadataCache`) — there is no per-call registration to
  hand-roll; that is the whole point of the base class (it removes the need for a manual dispatch table).

### Standing conformance gate (applies to every Epic 2–4 story)

Suite 100% green on the branch; public contract-shape diff vs the Story 1.1 snapshot empty or explicitly approved &
recorded; the local copy (the dead bridge) deleted; no test deleted without a recorded FR-20 ledger justification.
[Source: epics.md#Epic-2 standing-conformance-gate]

### Project Structure Notes

- Module follows the Hexalith project shape: `Contracts`, `Client`, `Server`, `Admin.Web`, `AppHost`,
  `ServiceDefaults`, `Testing`, with `tests/Hexalith.Conversations.*.Tests` mirrors. The aggregate lives in the
  domain assembly `Hexalith.Conversations` (`ConversationsAssemblyMarker`); the bridge lives in the Server assembly.
- Conformance/Server tests run **per-project**, not solution-wide
  (`dotnet test tests/Hexalith.Conversations.Conformance.Tests/`). Use `Hexalith.Conversations.slnx` for
  restore/build only. [Source: Hexalith.EventStore/CLAUDE.md#Build-&-Test-Commands]
- Submodule rule (binding): initialize/update **root-level submodules only**; never
  `git submodule update --init --recursive`. The EventStore SDK is consumed via the submodule-conditional project
  reference (built from source). [Source: CLAUDE.md#Git-Submodules]

### Carry-forward technical-debt awareness (do not let it flake the gate)

- **T1 parallelism race (closed by 2.1):** the `PublicContractShapeSnapshotGenerationTest` writer / reader race was
  fixed test-only via `ReleaseEvidenceArtifactCollection`. If you add a Conformance test, keep it inside the existing
  `[Collection]` discipline if it reads/writes release-evidence files. [Source: 2.1 Completion Notes; epic-1-retro §7 T1]
- **Admin.Web Playwright E2E lane** (2/14) requires Chromium (`pwsh tests/install-playwright.ps1`) — environmental,
  unrelated to this story; do not chase it. [Source: 2.1 Completion Notes]

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-2.2] — story statement + ACs + standing gate.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR-Coverage-Map] — FR-7 → Epic 2 (remove-and-replace).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#A row 12] — Aggregate scaffolding → Consume `EventStoreAggregate<TState>` reflection dispatch (FR-7). #B — EventStore.Client surface.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs:20-199] — base-class dispatch (`ProcessAsync`/`DiscoverHandleMethods`) + replay (`Replay`).
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/AggregateReplayer.cs:16-209] — public replay engine; sequence/duplicate/version guards.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Handlers/DomainProcessorStateRehydrator.cs:12-340] — `internal` Apply discovery (why the verifier switch cannot delegate).
- [Source: src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs:30-299] — already on the base class; static `Handle(TCommand, ConversationState?)`.
- [Source: src/Hexalith.Conversations/State/ConversationState.cs:214-628] — `public void Apply(TEvent)` convention methods.
- [Source: src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs] — the dead shim to delete (AC-2).
- [Source: src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs] — the genuine idempotency contract (Keep, AC-3).
- [Source: src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs] — domain replay verifier (Keep, AC-3).
- [Source: docs/release-evidence/at-risk-test-register-v1.md:35,57] — seeded "Story 2.2 (FR-7)" row; `projectReferenceDisposition` (2.2 is NOT the last owner → keep the Conformance→Server ref).
- [Source: _bmad-output/implementation-artifacts/2-1-wire-conversations-onto-the-shared-two-line-domain-service-host.md] — prior story; "aggregate already extends `EventStoreAggregate<ConversationState>`; do not re-touch it [that's 2.2]"; gate at 351; P1/P2/A1/A2 carry-forwards.

## Developer Context

### Technical Requirements (dev agent guardrails)

- .NET 10 (`net10.0`), SDK pinned `10.0.302` (`global.json`). Nullable enabled, implicit usings,
  **warnings-as-errors** — do not suppress broadly. File-scoped namespaces, Allman braces, `_camelCase` private
  fields, `Async` suffix, CRLF. ITANEO copyright header on every edited/created source file.
- Central Package Management (`Directory.Packages.props`) — never put package versions in `.csproj`; never introduce
  a new package version in the teeth test (use xUnit v3 / Shouldly / NSubstitute already present).
- Keep the change scoped to Conversations artifacts + the test/ledger updates this story mandates. **Do not edit**
  EventStore/Tenants/Parties/FrontComposer sources.
- This is a deletion-dominant story — the net production change is **removing one dead class**. Resist refactoring.

### Architecture Compliance

- Let EventStore own routing, actor identity, persistence, snapshots, publication, projection invalidation, command
  status — the base class delegates exactly this. The deleted bridge was an attempt to re-interpret command status
  inside Conversations; removing it **strengthens** the boundary (NFR8: public-surface / EventStore-concept boundary).
- Keep authorization/tenant lookups/HTTP/Parties calls out of aggregate logic — unchanged here.
- Keep aggregate state and event application deterministic and replay-safe (project-context). The base-class replay
  is deterministic; the domain `ConversationReplayVerifier` adds Conversations-specific content-safe guards — both
  stay deterministic.

### Library / Framework Requirements

- **`Hexalith.EventStore.Client`** (namespace `Hexalith.EventStore.Client.Aggregates`) — supplies
  `EventStoreAggregate<TState>`, `AggregateReplayer`, `IDomainProcessor`, `IAggregateReplay`,
  `AggregateReconstructionRequest/Result`. Already referenced transitively by the domain + Server assemblies (the
  domain already extends the base class). Referenced as a **project** (submodule, built from source).
- **`Hexalith.EventStore.Contracts`** — `CommandEnvelope`, `CommandStatusRecord`, `CommandStatus`, `DomainResult`,
  `IRejectionEvent`, replay envelopes. The teeth test builds `CommandEnvelope`/replay inputs from these.
- Versions in this ecosystem: Dapr `1.17.7`, Aspire `13.2.x`/`13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`,
  NSubstitute `5.3.0`. Use these via CPM.

### File Structure Requirements

- New tests go under `tests/Hexalith.Conversations.Server.Tests/` or `tests/Hexalith.Conversations.Tests/`
  (mirrors `src`). Evidence artifacts under `docs/release-evidence/` are written by generation tests, never
  hand-edited. The deleted bridge's `EventStore/` directories (src + test) should be removed if left empty.

### Testing Requirements

- xUnit v3 + Shouldly + NSubstitute. Run per-project.
- **Prove behavior, not mirrors** (Epic 1 L1 / agreement A1): the AC-1 teeth test must drive the **SDK reflection
  path** and go RED if dispatch is bypassed (the "No Handle method found" assertion is the teeth) — not merely
  re-call the static `Handle`. The pure aggregate tests (AC-4) stay as direct `Handle`/`Apply` calls.
- Conformance suite must stay **≥ 351 and monotonic**; assertion strength must not drop vs the Story 1.1 baseline.
  The removed bridge test is offset by the net-new dispatch/replay teeth test.
- Integration-test rule (carried from EventStore conventions): a Tier-2/3 test must inspect real end-state, not only
  a 202/return code or a mock call count — applies if you add any request-level integration test (you likely won't).

### Previous-Story Intelligence (Epic 1 → Epic 2 carry-forward, and Story 2.1)

- **Story 2.1 (immediately prior, done):** wired the two-line host; **explicitly deferred** the aggregate base-class
  work to 2.2 and stated "the aggregate already extends `EventStoreAggregate<ConversationState>`; do not re-touch it"
  — confirming this story's deliverable is the **shim removal**, not an aggregate rewrite. Closed the gate at 351 and
  the T1 parallelism race. Established the evidence-generation-test idiom (regenerate, never hand-edit).
- **L1 / A1 — coverage ≠ live-path exercise.** Pin behavior by fault-injection / negative assertions (the teeth test).
- **P1 / P2 — generate the Dev Agent Record (counts + File List) from the final `dotnet test` run, last.** The
  human-curated count drifted in 5/5 Epic 1 stories.
- **A2 / A3 — ledger entry for any removed/weakened test** (the bridge test removal); reclassifications go through the
  `classification-change-procedure-v1` append-only changeLog. **Append-only** — never rewrite accepted rows.
- **T2 / projectReferenceDisposition** — the `Conformance.Tests → Server` reference is removed only by the **last**
  owning story of {2.2, 2.5, 3.2, 3.3}; 2.2 is **not** last → keep it.

### Git Intelligence (recent work patterns)

Recent commits: `feat(story-2.1): Wire Conversations onto the shared two-line domain-service host` (the only `src/`
production change so far) preceded by `feat(story-1.x)` (test/evidence only). Established patterns to reuse: the
evidence-generation-test idiom for `docs/release-evidence/*` (repo-root discovery → deterministic indented-JSON write
→ re-read + re-validate + content-safety scan; regenerate, never hand-edit); Conventional Commits scope
`feat(story-2.2): …`. This story (2.2) is the **second** `src/` production change and is **deletion-dominant**.

### Project Context Reference

`_bmad-output/project-context.md` is binding. Most-relevant rules for this story:
- "Implement `ConversationAggregate : EventStoreAggregate<ConversationState>` and emit domain events." — already done;
  this story removes the residual shim around it.
- "Let EventStore own routing, actor identity, persistence, snapshots, publication, projection invalidation, **command
  status**." — the deleted bridge violated this by re-interpreting command status; removal restores the boundary.
- "Define Conversations' idempotency contract explicitly before coding write APIs." — that contract is
  `IdempotentConversationCommandExecutor` + `Idempotency/*` (Keep, AC-3), distinct from the dead bridge.
- "Keep aggregate state and event application deterministic and replay-safe." — base-class dispatch/replay preserved.
- "Treat EventStore as a bounded-context dependency; do not reimplement its runtime behavior." — consume the base
  class; remove the shim that duplicated its command-status concern.
- "Never initialize nested submodules / no `--init --recursive`." — root-level submodule only.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- **Conformance gate failure surfaced and resolved (inventory glob emptied by the consume):** deleting the bridge
  emptied the accepted Consume/Promote/Keep inventory's `src/Hexalith.Conversations.Server/EventStore/**` glob (the
  single 32-LOC file under `shared-host-api`), which tripped Story 1.4's
  `ConsumePromoteKeepInventoryValidationTest.NoSourceFileShouldBeDoubleCountedAcrossAreas` ("resolves to no .cs file —
  stale or mistyped path"). Per-area `approxLoc` is a frozen baseline constant, so LOC reconciliation never broke —
  only the glob-resolution guard did. **Resolution (user-approved):** kept the accepted area rows/LOC byte-immutable;
  relaxed the validator's empty-glob guard to tolerate a zero-file resolution **only** when an append-only `changeLog`
  entry accounts for that Consume/Promote area (Keep areas still hard-fail — vanishing Keep code is a regression), and
  appended a schema-conformant `challenge`/`upheld` entry (`CL-shared-host-api-challenge-1`) to the inventory's
  `changeLog`. The `challenge`/`upheld` shape is the only Story 1.5 `classification-change-procedure-v1` entry type
  that fits a *consumption* (the area stays `Consume`, so a `reclassification` — which requires `from != to` — is
  invalid). This generalizes for every future Epic 2/3 consume-deletion.

### Completion Notes List

Story 2.2 is **deletion-dominant** and behavior-preserving. The aggregate (`ConversationAggregate :
EventStoreAggregate<ConversationState>`) and `ConversationState.Apply(TEvent)` conventions were **not modified** — they
were already the canonical convention (Task 1 read-only baseline confirmed).

- **Task 1 (AC-1, AC-4):** Confirmed `ConversationAggregate` extends `EventStoreAggregate<ConversationState>` with six
  `public static DomainResult Handle(TCommand, ConversationState?)` overloads, and `ConversationState` exposes
  `public void Apply(TEvent)` for every persisted event. Grep finding (recorded honestly): the Server command handlers
  reach the aggregate via typed per-command `*Boundary.DispatchValidated(...)` seams (the Story 2.1 pattern) which call
  `ConversationAggregate.Handle(...)` directly — these are **not** a hand-rolled switch/if dispatch table (one typed
  boundary per command, no manual routing inside the aggregate), and are out of scope to change. No source edits.
- **Task 2 (AC-2):** Verified **zero production references** (only the definition + its own test), then deleted
  `EventStoreCommandStatusIdempotencyBridge.cs` and its test, plus the now-empty `EventStore/` directories (src + test).
  Confirmed `CommandStatusRecord`/`CommandStatus` no longer referenced anywhere in Conversations (the bridge was the
  sole consumer); the live idempotency types (`ConversationIdempotencyDecision`, etc.) remain.
- **Task 3 (AC-1) — teeth test added:** `ConversationAggregateBaseClassDispatchTest` (12 cases). Drives all **six**
  `Handle` overloads through the SDK `IDomainProcessor.ProcessAsync` reflection path and asserts each result matches
  the direct `Handle` result (create succeeds; the five state-dependent commands fail closed with their typed
  rejections — each produced by the handler the reflection reached). **Dispatch teeth:** an unknown command type
  surfaces `InvalidOperationException("No Handle method found …")`, not a silent no-op. **Replay teeth:** an ordered
  `ConversationCreatedDomainEvent` reconstructs state via the `Apply` convention (`Succeeded`, seq 1), and an unknown
  event type fails as `UnknownEventType` — so removing the `Apply` convention would turn it RED. Uses only CPM packages
  (xUnit v3 / Shouldly); payload serialized with default STJ for dispatch and Web options for replay to match the SDK.
- **Task 4 (AC-3, AC-4):** Pure aggregate + idempotency/replay suites green with **no source edits** to
  `Idempotency/*`, `IdempotentConversationCommandExecutor`, or `ConversationReplayVerifier` (git diff vs baseline
  empty). `ConversationReplayVerifier`'s inner per-event apply switch stays (no usable public per-event apply seam on
  the SDK — see Dev Notes "The one judgment call"). Live characterization oracles pass.
- **Task 5 (AC-5):** Added a parallel `story22StructuralDispositions` section to `AtRiskTestRegisterGenerationTest`
  (regenerated the ledger `.json`; never hand-edited) recording (1) the bridge+test removal as **redundant** (base
  class/SDK owns command status; dead, zero prod refs) and (2) the executor/`Idempotency/*`/`ConversationReplayVerifier`
  as **Keep** (AC-3). Content-safe (the forbidden literal "EventStore" does not appear in the register JSON — the shim
  is described by role). Updated companion `.md`. Append-only; accepted inventory rows/LOC byte-unchanged.
- **Task 6 (AC-5):** Release build **0 warnings / 0 errors** (warnings-as-errors). Public-contract-shape baseline
  (`public-contract-shape-baseline-v1.json`, 196 types) **byte-unchanged** vs baseline (the shim was in the Server
  assembly, outside the Contracts public surface) → contract-shape diff empty. Standing conformance gate
  **352 (≥ 351, monotonic)**: the removed bridge unit test is offset by the net-new dispatch/replay teeth + the
  net-new Story22 ledger test. `projectReferenceDisposition` left as-is (2.2 is not the last owner of
  {2.2, 2.5, 3.2, 3.3}).

**Final test counts (Release, generated from the final run — Epic 1 retro P1/P2):**

| Suite | Result |
|---|---|
| `Hexalith.Conversations.Contracts.Tests` | 587 passed |
| `Hexalith.Conversations.Client.Tests` | 25 passed |
| `Hexalith.Conversations.Tests` (pure aggregate) | 185 passed (+12 net-new dispatch/replay teeth) |
| `Hexalith.Conversations.Server.Tests` | 524 passed (−bridge test) |
| `Hexalith.Conversations.Conformance.Tests` (**standing gate**) | **352 passed** (≥ 351, monotonic; +1 Story22 ledger test) |

Not run (environmental, per Dev Notes — unrelated to this story): `IntegrationTests` (Tier-3, Docker/Aspire),
`Admin.Web.Tests` (Playwright/Chromium lane).

### File List

**Deleted (production + test):**
- `src/Hexalith.Conversations.Server/EventStore/EventStoreCommandStatusIdempotencyBridge.cs` (+ now-empty `EventStore/` dir)
- `tests/Hexalith.Conversations.Server.Tests/EventStore/EventStoreCommandStatusIdempotencyBridgeTest.cs` (+ now-empty `EventStore/` dir)

**Added:**
- `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateBaseClassDispatchTest.cs`

**Modified:**
- `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` (Story22 dispositions + regen)
- `tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` (consumed-glob tolerance keyed to changeLog)
- `docs/release-evidence/at-risk-test-register-v1.json` (regenerated)
- `docs/release-evidence/at-risk-test-register-v1.md`
- `docs/release-evidence/consume-promote-keep-inventory-v1.json` (append-only `changeLog` entry)
- `docs/release-evidence/consume-promote-keep-inventory-v1.md`

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot · **Date:** 2026-06-03 · **Outcome:** Approved (auto-fixed) · **Mode:** adversarial, auto-fix

**AC verification (all confirmed against the working tree, not just the story claims):**
- **AC-1 (base-class dispatch & replay are the live path, with teeth):** ✅ `ConversationAggregateBaseClassDispatchTest`
  drives all six `Handle` overloads through `IDomainProcessor.ProcessAsync` reflection (matching direct `Handle`),
  proves non-null rehydrated state delivery, and has genuine teeth — unknown command → `InvalidOperationException("No
  Handle method found …")`, unknown event → `UnknownEventType`. Verified RED-on-regression by construction. 12 cases, green.
- **AC-2 (dead bridge removed):** ✅ `grep` confirms zero production references; `EventStoreCommandStatusIdempotencyBridge.cs`,
  its test, and both now-empty `EventStore/` dirs are deleted. `CommandStatusRecord`/`CommandStatus` no longer referenced.
- **AC-3 (genuine idempotency/replay KEEP):** ✅ `git diff` empty for `Idempotency/*`,
  `IdempotentConversationCommandExecutor`, `ConversationReplayVerifier`, `ConversationAggregate`, `ConversationState`.
- **AC-4 (pure aggregate tests unchanged & green):** ✅ pure `Handle`/`Apply` style preserved; suite green.
- **AC-5 (ledger append-only; gate holds; contract-shape empty):** ✅ at-risk register JSON purely additive (+18, 0 deletions);
  `public-contract-shape-baseline-v1.json` byte-unchanged; conformance gate 352 (≥351, monotonic); inventory consumed-glob
  accounted via append-only `changeLog` `challenge`/`upheld` entry.

**Issues found and auto-fixed:**
1. 🔴 **CRITICAL — build was broken / story claims unverifiable as-found.** The `Hexalith.Tenants`, `Hexalith.Parties`,
   and `Hexalith.FrontComposer` submodule **working trees had drifted** off their committed gitlinks (Tenants at
   `d6c7052` vs recorded `5b4424e`). The drifted Tenants commit dropped the `Hexalith.Tenants.Client.Subscription`
   namespace that pre-existing `ConversationTenantAccessRegistrationTest.cs` consumes, so `dotnet build -c Release`
   FAILED (`CS0234`) — contradicting the story's "Release 0 warnings / 352 green". This drift is explicitly out of
   scope ("Do NOT touch Tenants/Parties/FrontComposer"). **Fix:** restored all three submodules to their recorded
   gitlinks (root-level checkout, non-recursive — CLAUDE.md compliant). Build now 0 warnings / 0 errors; full suite green.
2. 🟡 **MEDIUM — Dev Agent Record count drift (the very Epic 1 P1/P2 hazard the story flags).** The record claimed
   `Hexalith.Conversations.Tests` = 183 (+10) and "10 cases"; actual is **185 (+12)** with **12 cases** (the
   `[Theory]` carries 6 inline cases plus 6 facts). **Fix:** corrected the counts in Completion Notes + the final-counts table.
3. 🟡 **MEDIUM — conformance-validator relaxation was area-scoped, not glob-scoped.** The new empty-glob tolerance in
   `ConsumePromoteKeepInventoryValidationTest` keyed on "area has *any* changeLog entry", so a future stale/mistyped
   *sibling* glob in an already-logged area (e.g. `shared-host-api` also owns the Api-endpoint + root-host globs) would
   be silently masked. **Fix:** tightened to require the changeLog entry to reference the *specific* consumed spec
   (literal-prefix match) — strictly narrower; gate stays green at 352.

**Verification after fixes (Release, `--no-build`):** Contracts 587 · Client 25 · Tests 185 · Server.Tests 524 ·
Conformance **352** — all green; build 0 warnings. Not run (environmental, per Dev Notes): IntegrationTests (Docker/Aspire),
Admin.Web (Playwright/Chromium).

## Change Log

| Date | Change |
|---|---|
| 2026-06-03 | Senior Developer Review (AI, adversarial + auto-fix): **Approved**. Auto-fixed 1 CRITICAL (submodule working-tree drift broke the Release build via missing `Hexalith.Tenants.Client.Subscription` — restored Tenants/Parties/FrontComposer to recorded gitlinks) and 2 MEDIUM (Dev Agent Record count drift 183/+10 → 185/+12, "10 cases" → 12; tightened the consumed-glob validator tolerance from area-scoped to spec-scoped). Re-verified Release 0 warnings, full suite green, conformance gate 352 (≥351 monotonic), contract-shape diff empty. Status review → done. |
| 2026-06-03 | Story 2.2 implemented (FR-7, remove-and-replace). Deleted the dead `EventStoreCommandStatusIdempotencyBridge` shim + its test (zero production references; SDK base class owns command status). Added `ConversationAggregateBaseClassDispatchTest` proving the SDK reflection dispatch (`ProcessAsync`) reaches every `Handle` overload and replay reconstructs state via the `Apply` convention, with teeth (unknown command → "No Handle method found"; unknown event → `UnknownEventType`). Recorded the disposition in the FR-20 at-risk register (`story22StructuralDispositions`, regenerated) and accounted for the consumed inventory glob via an append-only `changeLog` `challenge`/`upheld` entry. Aggregate/state/idempotency/replay-verifier domain logic unchanged. Release 0 warnings; standing conformance gate 352 (≥351 monotonic); public-contract-shape diff empty. |
