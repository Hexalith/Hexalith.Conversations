# Good-Spine Rubric Review — Architecture V14

- **Target:** `_bmad-output/planning-artifacts/architecture.md`
- **Effective scope reviewed:** frozen V8 prefix; append-only V9–V14 overlays; V14-pinned bundle, graph, epic authority, and checkpoint sidecars; later repository authority records V15–V21 where they affect discovery, precedence, or a V14 invariant
- **Intent:** validation only; no target or authority artifact was changed
- **Date:** 2026-09-16
- **Lens:** the complete BMad good-spine checklist: divergence coverage at the level below, enforceability, Deferred/open-item safety, brownfield/source consistency, inherited/supersession consistency, spec/capability coverage, named-technology currency, and structural breadth including the operational/environmental envelope

## Methodology

1. Read the document in precedence order rather than treating its frozen frontmatter as current. V13 says the last complete overlay marker selects the architecture version and that the marker's `sidecar-head` selects checkpoint authority (`architecture.md:2415-2430`). V14 is the last complete marker (`architecture.md:2627-2698`).
2. Recomputed the live SHA-256 values of `architecture.md`, the V14 sidecar, and the execution graph and compared them with the V9 bundle rows. All three match the bundle exactly (`v9-authority-bundle-v1.json:159-164`, `:279-300`). The bundle itself identifies the V14 architecture/epic pair and a bound planning candidate (`v9-authority-bundle-v1.json:1-9`).
3. Walked every normative decision family and every explicit Deferred/open item, including the V13 DC-1…DC-11 closures and V14's Story 16 additions. Checked representative brownfield seams in the current code and solution: AppHost shipping flags, projection vocabulary, tenant-store capability, package pins, solution/test topology, and operational runbooks.
4. Reconciled the spine with its declared specification and PRD companions, then inspected the later V15–V21 authority chain because the repository now uses it to authorize Story 7.1.
5. Verified technology facts against the current tracked pins and primary vendor sources. Aspire's official package feed listed 13.5.4 on 2026-09-16, while the tracked Hexalith baseline and AppHost use 13.5.3; Dapr remains on the supported 1.18 line.

## Verdict

**FAIL — not safe as the single current build authority.** V14 is internally strong and its candidate-bound bundle/graph are byte-consistent, but its discovery contract now forks from the repository's executable V15–V21 authority chain. That fork has already moved the effective Story 7.1 hold to `LIFTED` without an appended architecture pointer, and the release-owner hold lifts occurred before V13's mandatory operational-envelope disposition. A builder following only the spine obtains `V14 + ACTIVE`; a builder following the later sidecars obtains `V21 + Story 7.1 LIFTED`. This is a live, level-below divergence, not merely historical clutter.

**Severity counts:** 2 critical · 2 high · 4 medium · 2 low.

## Findings

### F1 — CRITICAL — Authority discovery terminates at V14 while executable authority continues through V21

**Rubric dimensions:** divergence coverage; inherited/supersession consistency; enforceability; brownfield consistency.

V13 makes the discovery rule explicit: the last complete overlay marker names the current sidecar head, and a new `v<N>-*-authority` sidecar plus its appended pointer amendment must be published in the same commit; publishing only one is an authority-publication failure (`architecture.md:2415-2430`). The last marker is V14 and still pins `v14-current-candidate-authority-v1.json` with `hold=ACTIVE` (`architecture.md:2627-2640`, `:2679-2681`, `:2698`). The repository nevertheless contains a successor chain through V21. V21 points to V20 (`v21-story-7.1-authority-correction-v1.json:1-7`) and declares Story 7.1 `LIFTED` with full-story execution allowed (`v21-story-7.1-authority-correction-v1.json:118-130`). No V15–V21 pointer exists after the V14 END marker.

The V9 bundle and graph remain truthful point-in-time V14 artifacts (`v9-authority-bundle-v1.json:4-9`; `v9-execution-graph-v1.json:1-9`), but they cannot simultaneously be the current discovery endpoint and coexist with an undiscoverable executable V21 head. The current grammar also does not say whether V15–V21 are exempt mutable results or successor planning-authority sidecars; their names and effects make both readings plausible.

**Disposition: discuss.** Decide whether V15–V21 are inside V13's pointer-amendment scope. If yes, append a new architecture pointer overlay chaining V14 to the exact current head. If no, append an equally explicit second discovery namespace for mutable execution/hold authority, including precedence when its state differs from the V14 marker and bundle. Do not rewrite V1–V14.

### F2 — CRITICAL — The operational-envelope precondition was bypassed by two hold-lift decisions

**Rubric dimensions:** Deferred/open-item safety; inherited consistency; complete structural breadth.

V13 does not merely note the operational envelope as future work. It requires the release owner to decide it, defer it with a named owner, or delegate it to a named counterpart artifact **before any hold-lift decision** (`architecture.md:2612-2618`). The authoritative target tree contains only `docs/adrs/` and `docs/release-evidence/` (`architecture.md:1396-1421`); the repository's three live runbooks govern planning evidence and submodule/story-record mechanics, not production environment topology, runtime operations, provider strategy, recovery, or capacity waivers (`docs/runbooks/evidence-boundary-validation.md:1-6`, `docs/runbooks/story-final-record-generation.md:8-18`, `docs/runbooks/submodule-promotion-completion-gate.md:8-14`).

Despite that unmet precondition, V17 records a scoped `LIFTED` state for `7.1-SCHEMAS` (`v17-implementation-hold-decision-authority-v1.json:64-69`, `:120-136`), and V20/V21 lift Story 7.1 itself (`v20-story-7.1-release-owner-authority-v1.json:9-24`, `:168-177`; `v21-story-7.1-authority-correction-v1.json:118-130`). None names an operational-envelope decision, deferral owner/revisit condition, or counterpart artifact.

This weakens a binding V13 invariant without declaring supersession. The narrow scope of the lifts reduces blast radius but does not satisfy the literal “before any hold-lift decision” rule.

**Disposition: discuss.** The release owner must either (a) disposition the operational envelope now and state how that late decision cures the scoped lifts, or (b) append an architecture amendment explicitly narrowing the precondition to release/product-runtime work and explain why planning-only Story 7.1 is exempt. Silent reinterpretation is not safe.

### F3 — HIGH — The newly executable Story 7.1 authority has no defined terminal transition or ordinary integration topology

**Rubric dimensions:** divergence coverage at the level below; Deferred/open-item safety; enforceability.

The V9 successor-story contract requires every story to own a bounded outcome, rollback boundary, and separate final record (`architecture.md:2037-2070`). V21 now allows full Story 7.1 execution (`v21-story-7.1-authority-correction-v1.json:119-130`), but its own approved source records three unresolved high intent gaps: ordinary integration with accepted `main` history is rejected by the frozen lineage/gitlink rules, pull-request validation does not evaluate the merge candidate, and no terminal state consumes the final-record outputs and retires the temporary descendant-path restriction (`spec-publish-v20-story-7-1-release-owner-authority.md:329-330`, `:338`).

Two builders can therefore complete identical Story 7.1 outputs yet choose incompatible integration/retirement mechanisms, while a literal builder cannot transition out of the temporary authority at all. This is especially material because the hold is no longer preventing Story 7.1 work.

**Disposition: discuss.** Before accepting `SC-7.1`, publish owner-approved successor authority defining the terminal-state transition, the admissible integration topology with protected `main`, and the merge-candidate CI evidence. Keep Story 7.2 locked until that transition is mechanically proven.

### F4 — HIGH — The canonical SPEC contradicts the effective architecture and execution state

**Rubric dimensions:** spec/capability coverage; source consistency; supersession consistency.

`SPEC.md` declares itself and its companions the complete canonical contract (`SPEC.md:14-16`). Its only capability requires consumers to reach the “final V9 marker,” observe `PC=UNBOUND`, and see the global hold `ACTIVE` (`SPEC.md:20-26`); its constraints and non-goals repeat that binding the candidate or lifting the hold is forbidden/missing (`SPEC.md:28-45`). The V14 bundle instead has a concrete planning candidate and V14 authority pair (`v9-authority-bundle-v1.json:1-9`), and V21 permits Story 7.1 execution (`v21-story-7.1-authority-correction-v1.json:118-130`).

The architecture covers the original CAP-1 adoption intent historically, but the live spec package now directs a downstream planner to a different execution answer. Because the spec explicitly adopts `architecture.md` as a companion, “the architecture controls” does not remove the contradiction; the package promises a coherent complete contract.

**Disposition: discuss.** Refresh the spec through `bmad-spec` (or append a successor capability entry) so it discovers V14 and the separate current execution/hold chain while preserving stable capability identity. The fix belongs in the source package, not as an invented architecture override.

### F5 — MEDIUM — Initiative requirements coverage is asserted but not dispositioned for 13 of 20 FRs

**Rubric dimensions:** spec/PRD capability coverage; divergence coverage.

The spine activates FR-1…FR-15 and FR-17…FR-20 (`architecture.md:323-329`), but the landing-zone register contains only FR-10…FR-16 (`architecture.md:346-358`). It later claims all 20 initiative FRs are governed by that rebaseline/register (`architecture.md:1676-1684`). The PRD describes FR-1…FR-9 and FR-17…FR-20 as in-scope inventory, consumption, adoption/template, measurement, and conformance outcomes (`prds/prd-Conversations-2026-06-02/prd.md:77-86`). Those requirements may be implemented and mechanically mapped in companion artifacts, but the spine neither names a current authoritative mapping nor says that they require no additional architecture decision.

**Disposition: defer.** Add a compact current disposition table or a single named, digest-bound counterpart map covering FR-1…FR-9 and FR-17…FR-20. Each row should say decided here, inherited/no architecture decision, or deferred with owner/revisit condition; do not restate full requirements.

### F6 — MEDIUM — A live public-API prohibition is impossible to enforce literally

**Rubric dimensions:** enforceability of live rules; divergence prevention.

The public-API rule forbids not only EventStore stream/event internals but also unqualified “route names, DTO property names, serialized payloads, error codes, … OpenAPI descriptions” (`architecture.md:1110-1118`). Other live rules require OpenAPI and stable contract compatibility (`architecture.md:886-889`) plus Problem Details carrying a stable error code (`architecture.md:1154-1162`). V13 narrows only the sequence-number/storage-offset part to EventStore internals (`architecture.md:2516-2528`), leaving the rest contradictory.

A literal conformance check is unpassable; a practical check must silently infer an unstated “EventStore-internal” qualifier. That fails the enforceability test.

**Disposition: autofix.** In the next append-only correction, scope route names, DTO properties, payloads, error codes, logs, and OpenAPI prohibitions to EventStore/internal implementation concepts; explicitly retain Conversations-owned public routes, DTO fields, serialized contracts, stable error codes, and OpenAPI.

### F7 — MEDIUM — The document cannot tell which pattern rules are actually enforced

**Rubric dimensions:** enforceability of every live rule.

The spine says every implementation pattern must have a conformance/analyzer/contract/architecture check and that a pattern without one is advisory (`architecture.md:1327-1330`), yet it supplies no pattern-to-check inventory. It simultaneously declares the patterns enforceable (`architecture.md:1668-1670`). Naming, folder placement, metadata-only classification, new-state ownership, error format, and disclosure rules therefore have an undiscoverable live/advisory status.

The V13/V14 authority machinery demonstrates that the repository can generate closed inventories, so leaving this state implicit is unnecessary. Two reviewers can disagree on whether a rule blocks completion while both follow the document.

**Disposition: defer.** Generate a non-amending pattern→enforcement ledger with stable rule IDs, owning test/analyzer/validator, current status, and successor story for uncovered rules. Treat uncovered entries explicitly as advisory until their named gate lands.

### F8 — MEDIUM — Current requirement-to-structure placement is missing; only the superseded May layout is mapped

**Rubric dimensions:** divergence coverage; brownfield consistency; capability coverage.

The authoritative target tree uses `Admin.Web`, one `Conformance.Tests` project, and no module `ServiceDefaults` (`architecture.md:1394-1424`). The only Feature-FR placement map explicitly uses the superseded May layout and points to `Admin/TrustComponents`, `Conformance/Suites/*`, `ServiceDefaults`, and `samples` (`architecture.md:1562-1586`). The current solution has `Admin.Web.Tests` and one `Conformance.Tests`, but no `Conformance.Server.Tests` (`Hexalith.Conversations.slnx:39-56`); V13 truthfully records the missing module-internal tier as Epic 9 work (`architecture.md:2587-2590`).

The missing project is safely story-gated, but the architecture still offers no current placement rule for Feature-FR work. Builders below this altitude must translate historical paths independently.

**Disposition: defer.** Publish a current requirements→owned-boundary map before the next feature/UX implementation tranche. Keep it structural and small; point to projects/boundaries, not a full file tree.

### F9 — LOW — Dated technology facts have drifted from the brownfield baseline

**Rubric dimensions:** named technology verified-current; brownfield consistency.

V13's dated refresh says the shared props pin Aspire 13.4.6 and Dapr 1.18.5 (`architecture.md:2580-2589`). The tracked AppHost now uses Aspire 13.5.3 (`src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj:1-6`), shared props use Aspire 13.5.3 and Dapr 1.18.7 (`references/Hexalith.Builds/Props/Directory.Packages.props:109-120`, `:136-146`), and the SDK pin is 10.0.401 (`global.json:1-5`). The official Aspire package feed lists 13.5.4 as current on 2026-09-16 ([NuGet](https://www.nuget.org/profiles/aspire)); official Dapr support remains on the 1.18 release line ([Dapr support policy](https://docs.dapr.io/operations/support/support-release-policy/)).

Because the section is explicitly dated and the durable rule is sibling-pin alignment, this is not a version-policy contradiction. It is stale seed that readers can mistake for a current binding fact.

**Disposition: autofix.** Append a dated refresh or, preferably, state that exact package/SDK versions are code-owned seed and bind only the alignment/compatibility policy in the spine.

### F10 — LOW — Human discoverability remains poor despite strong machine integrity

**Rubric dimensions:** divergence prevention; inherited/supersession consistency.

The file is 2,698 lines of preserved and superseded prose with no stable `AD-n` / `Binds` / `Prevents` / `Rule` projection. The frozen frontmatter still reports V8 and `authority-correction-only-not-ready` (`architecture.md:1-15`), with the correction visible only after following V13's restatement (`architecture.md:2432-2440`) and then the V14 tail. The machine path is precise through V14; the human path requires recomputing many narrow supersessions and is now broken entirely for V15–V21 (F1).

**Disposition: defer.** Generate a short, digest-bound, non-amending current-rules spine projection. It should carry stable AD IDs and source-layer citations, with the append-only authority file remaining the provenance record.

## Checklist Scorecard

| Dimension | Rating | Basis |
| --- | --- | --- |
| Real divergence points at the level below | **Fail** | The V14/V21 authority fork and Story 7.1 terminal/integration gap permit incompatible execution decisions (F1, F3). Domain/runtime invariants themselves are generally strong. |
| Every live rule enforceable and divergence-preventing | **Partial** | Candidate/bundle/graph mechanics are excellent, but one API rule is internally impossible and pattern enforcement status is undiscoverable (F6, F7). |
| Deferred/open items are safe | **Fail** | FR-16, vocabulary completion, module-internal conformance, and Story 16 are gated safely. The operational envelope passed its own hold-lift deadline, and Story 7.1 has no terminal transition (F2, F3). |
| Named technology verified-current | **Partial** | Durable stack choices fit current Hexalith policy; exact dated version facts lag current pins and the official Aspire feed (F9). |
| Brownfield/source consistency | **Partial** | V14 correctly anticipates the current `ITenantProjectionStore` migration and ratifies shipped trust/freshness contracts. Current authority/spec state and version facts drift (F1, F4, F9). |
| Inherited/supersession consistency | **Fail** | V1–V14 are carefully scoped and byte-pinned, but later hold authorities bypass V13 discovery and an explicit inherited precondition (F1, F2). |
| Spec/capability coverage | **Fail** | Preserved denominators and FR-10…FR-16 are strong; the canonical SPEC is stale and 13 initiative FRs lack an in-spine disposition (F4, F5). |
| Complete structural breadth, including operations/environment | **Fail** | V13 identifies the missing operational envelope, but no decision/defer/delegation artifact exists and scoped hold lifts have already occurred (F2). |

## Verified Strengths

- The V14 architecture, V14 checkpoint sidecar, V14 epic authority, and 38-node/61-edge graph are candidate-bound and hash-consistent in the V9 bundle (`v9-authority-bundle-v1.json:1-9`, `:159-164`, `:270-300`; `architecture.md:2658-2677`).
- V13 closed the previous SM-C2, vocabulary, key-grammar, graph-composition, temporal-anchor, audit-degradation, ADR-namespace, tier-migration, AppHost, and ceiling-retirement divergences with concrete DC-1…DC-11 rules (`architecture.md:2457-2578`). The ratified vocabulary and temporal anchor match current code (`ProjectionTrustState.cs:12-57`; `ProjectionFreshnessV1.cs:11-32`).
- The major brownfield ownership decisions remain coherent: EventStore is authoritative, derived state is repairable, Tenants fail closed as the target invariant, the AppHost is non-packable/non-publishable, and platform modules own reusable runtime plumbing (`architecture.md:1921-1958`; `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj:1-18`).
- V14 handles the known durable tenant-projection gap as explicit successor work with exact predecessors and proof scenarios rather than pretending the current in-memory default is production-ready (`architecture.md:2642-2656`; `prds/prd-Conversations-2026-06-02/epics.md:4381-4413`; `src/Hexalith.Conversations.Server/Program.cs:35-44`).
- FR-16 is a good safe deferral: it is the sole non-activated initiative requirement, with a clear reopen condition and a prohibition on contract reshaping (`architecture.md:323-329`, `:346-370`).

## Gate Recommendation

Do not use `architecture.md` alone to authorize further work. Resolve F1 and F2 before another hold or successor decision, and resolve F3 before accepting Story 7.1's final candidate. F4 should be refreshed as an upstream spec correction. F5–F8 can be folded into the next append-only architecture update or named counterpart projections; F9–F10 are lower-risk hygiene improvements.
