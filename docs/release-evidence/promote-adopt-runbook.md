# Promote → Adopt Runbook (per-capability pipeline)

**Status:** Ratified by Story 3.1 (tracer-bullet). Reusable runbook for Stories 3.2–3.7.
**Source of truth for:** Epic 3 per-promote-story mechanics (FR-12 / FR-17 / NFR6 / FR-20).

This runbook captures the exact, ordered steps proven while promoting the first duplicated capability
(the generic typed-`HttpClient` DI registration) into a shared technical module and adopting it in
Conversations. Follow it for each subsequent capability promotion (Stories 3.2–3.7).

---

## 0. Resolve the landing zone (gating precondition — "don't promote into the dark")

- A promote story does **not** start until the landing zone for its capability is resolved (Epic 3 gate;
  OQ-1). Record the ratified zone here before writing code.
- **Ratified for Story 3.1:** a new library **`Hexalith.Commons.Http`** in the `Hexalith.Commons`
  submodule (`Hexalith.Commons/src/libraries/Hexalith.Commons.Http/`). Commons is the domain-agnostic
  infrastructure home and had no existing HTTP-client registration helper. EventStore was rejected as the
  wrong altitude (event-sourcing-specific).
- **Ratified for Story 3.2:** a new library **`Hexalith.Commons.TenantAccess`** in the
  `Hexalith.Commons` submodule (`Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/`).
  Commons is the domain-agnostic home for module tenant-access projection, fail-closed evaluation, and
  registration helpers; Conversations keeps its public tenant-access vocabulary as thin facades/adapters.

### Build-infrastructure caveat discovered in 3.1 (read before promoting into Commons again)

Commons libraries inherit `Hexalith.Commons/src/libraries/Directory.Build.props`, which **unconditionally**
imports the **nested** `Hexalith.Builds` submodule (`../../Hexalith.Builds/Hexalith.Package.props`). Umbrella
checkouts follow a **root-only submodule policy** and do not initialize Commons's nested `Hexalith.Builds`,
so any Commons library that inherits those shared props **fails to build from the umbrella**.

**Resolution adopted (lowest blast radius):** give the promoted library its **own self-contained**
`Directory.Build.props` in the library folder (MSBuild stops at the nearest one), declaring the load-bearing
settings inline (TargetFramework, Nullable, ImplicitUsings, warnings-as-errors, packaging) and **not**
importing the nested Builds — mirroring the self-contained pattern other promote-target modules (e.g.
EventStore) already use. Central package versions still resolve through the resilient
`Hexalith.Commons/Directory.Packages.props` fallback chain (which finds the umbrella-root `Hexalith.Builds`).
Test projects live under `Hexalith.Commons/test/` (whose `Directory.Build.props` already imports the parent
**conditionally**) and reference **only** the new library, forming a buildable island.

---

## 1. Promote — extract the shared, domain-agnostic helper **with its own tests**

- Create the helper in the ratified module, parameterized so a domain module supplies only the moving parts
  (for 3.1: client interface, implementation, options type, endpoint selector). Mirror the EventStore
  `AddEventStore<TAggregate>()` template-method style.
- Make it a **superset** of every existing shape — never weaken a caller. (3.1: selectable eager/lazy
  validation timing + first-class opt-in http/https scheme guard.)
- Keep the API **additive/backward-compatible** (NFR6): do not modify or break existing sibling signatures.
- Add unit tests **in the module** covering the capability's contract (3.1: missing endpoint, relative URI,
  non-http(s) scheme, valid endpoint, builder-returned-for-chaining).
- 3.2: module tests cover replay/deduplication, divergent duplicate replay conflict, out-of-order no-op,
  future/malformed evidence, configuration-key filtering/tombstoning, retryable persistence failures, and
  fail-closed evaluator states for missing/malformed tenant, missing/malformed caller, projection health,
  store exception, status/role mapping, poisoned member map, and role-to-requirement mapping.

## 2. Test-in-module — prove the helper green where it lives

- Build the new library and run its tests from the umbrella (the self-contained props make this possible
  without nested submodules). 3.1 result: library builds 0-warning; 8/8 helper tests pass.

## 3. Adopt — route the domain module through the shared helper

- Re-implement the domain registration to **delegate** to the shared helper.
- Wire the reference. The published package does not exist yet, so consume the promoted library **from
  source** using the established local-path convention: add a `Hexalith<Module>Root` property to the
  umbrella `Directory.Build.props` (with a `..\` sibling form and a same-dir form) and a guarded
  `ProjectReference` (`Condition="'$(Hexalith<Module>Root)' != ''"`) plus a relative fallback.

## 4. Delete — remove the hand-rolled implementation (FR-17), preserving consumers

- Delete the bespoke logic, **not necessarily the entrypoint symbol**.
- **Deletion-vs-facade decision (ratified for 3.1: thin facade).** When the entrypoint has a cross-submodule
  consumer, keep a one-line facade that delegates to the shared helper. This satisfies "adopt the shared
  helper" + "delete the hand-rolled implementation" while keeping dependent siblings and surface-pinning
  tests green with **zero sibling edits**. Choose literal full removal only if architecture mandates the
  symbol's removal — then migrate the consumer call site as a sanctioned cross-submodule promotion edit.
- 3.1: deleted the hand-rolled `ValidateEndpoint` + inline `AddHttpClient`; kept
  `AddHexalithConversationsClient(IServiceCollection, Action<ConversationClientOptions>)` (byte-identical
  public signature) as a thin facade.
- 3.2: deleted the hand-written decision mechanics from `ConversationTenantAccessService`; kept
  `ConversationTenantAccessDecision`, `ConversationTenantAccessRequirement`,
  `ConversationTenantAccessDenialReason`, `IConversationTenantAccessService`, and
  `ConversationTenantAccessGuard` as Conversations-owned vocabulary. The service now maps Tenants local
  projection state and Conversations health into the shared evaluator, then maps neutral denial kinds back
  into the existing Conversations-safe decision type.

## 5. Re-express / extend the guard tests (behavior preserved or strengthened — FR-20)

- Preserve **or strengthen, never weaken** behavior. Keep positive registration tests green; **add** any
  missing negative tests so the safety net survives the move. (3.1 added the three previously-missing
  Conversations rejection tests: missing / relative / non-http(s) endpoint.)
- Update the surface-pinning guards **only if the surface actually changed**:
  - `ContractPackageInventoryTest` `.cs` allowlist — thin facade keeps the file → **no change**.
  - `ClientBoundaryTest` Microsoft-transport allowlist — only changes if new **direct** `Microsoft.*`
    references appear in the assembly metadata (`GetReferencedAssemblies()` is the used-refs set, not the
    runtime closure). 3.1: the facade introduced no new direct Microsoft refs → **no change**.
  - Integration-guide example + doc tests — keep compiling against the surviving entrypoint; update in
    lockstep only if the entrypoint name changes. 3.1: name unchanged → **no change**.

## 6. Conformance green — release-gate suite ≥ prior count, contract-shape diff empty

- Run the full release-gate conformance suite. It must be **monotonic** (≥ the count at the prior story's
  close) and the public-contract-shape baseline diff must be **empty** (the baseline enumerates the
  **Contracts** assembly only; Client/helper changes must not alter it).
- 3.1 baseline: 357 at 2.7 close → **360 passed** at 3.1; contract-shape diff empty.
- 3.2 local evidence: Conformance project builds 0-warning and the public Contracts assembly is untouched.
  VSTest execution in this sandbox is blocked before discovery by `SocketException (13): Permission denied`
  when the test platform opens its local listener, so the pass count and generated contract-shape diff must
  be re-run in an environment that permits the VSTest socket.

## 7. Additive sibling-CI build — dependent modules compile green (NFR6)

- Build every dependent sibling against the promoted API.
- In the umbrella, point the consumer's module-root property at the in-tree source to build it directly,
  e.g. `dotnet build <consumer>.csproj -p:Hexalith<Module>Root=<umbrella-abs-path>`.
- 3.1: `Hexalith.Projects.Server` (consumer of `AddHexalithConversationsClient` at
  `ProjectsServerServiceCollectionExtensions.cs:146`) builds **0-warning** against the modified Conversations
  source; `Hexalith.Folders.Client` builds green transitively. Full umbrella Release build: 0 warnings.
- 3.2: full Conversations Release build is 0-warning. `Hexalith.Projects.Server` builds 0-warning against
  explicit root-level sibling paths. Full sibling `.slnx` builds are not a valid umbrella check here because
  they reference nested sibling paths under their own submodule directories; `Hexalith.Folders.Server`/core
  were additionally blocked by missing assets plus silent restore-graph failure in this sandbox.

## 8. Submodule commit + root pointer bump (root-only, never recursive)

- The promotion is a **separate technical-module submodule commit** (in the module that received the helper)
  **plus** a **root-level gitlink (pointer) bump** in the umbrella. Each promotion is its own commit + bump.
- **Never** `git submodule update --init --recursive`; never initialize/update nested submodules.

## 9. Verify gitlinks before building (recurring hazard from Stories 2.2–2.7)

- Before building, verify all **root** submodule gitlinks match recorded pointers; restore any out-of-scope
  working-tree drift to the recorded gitlink first. Out-of-scope drift broke the build in 2.2.

---

### Ordered checklist (copy per story)

1. [ ] Landing zone resolved + recorded (build-infra caveat handled).
2. [ ] Helper promoted with module tests; library builds + tests green.
3. [ ] Domain module adopts the helper; reference wired from source.
4. [ ] Hand-rolled logic deleted; facade-vs-removal decision recorded.
5. [ ] Behavior preserved/strengthened; guard tests re-expressed/extended only where the surface changed.
6. [ ] Conformance suite monotonic (≥ prior count) + contract-shape diff empty.
7. [ ] Dependent siblings compile green (NFR6).
8. [ ] Submodule commit + root gitlink pointer bump (root-only).
9. [ ] Root submodule gitlinks verified pre-build.
