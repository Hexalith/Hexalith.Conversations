---
title: 'Story 6.1: Rebaseline Architecture and Planning Authority'
type: 'chore'
created: '2026-07-15'
status: 'done'
baseline_revision: 'f31aa5ada2e37e1ec5f3e4b8e907525b37da863f'
review_loop_iteration: 2
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** The architecture and epic plan still authorize Conversations-owned hosting and treat superseded feature-planning assumptions as current, so corrective implementation lacks one PRD-aligned ownership, evidence, and sequencing model.

**Approach:** Rebaseline architecture to the finalized initiative PRD/addendum and approved July 15 corrections, append Epic 6 authority without rewriting completed history, regenerate the derived Epic 6 context, and enforce the resulting planning contract with focused conformance tests.

## Boundaries & Constraints

**Always:** Treat `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md` plus `addendum.md` as initiative authority; distinguish its 20 initiative FRs from the preserved 104 Feature-FRs and 77 Feature-NFRs; preserve the accepted 13,289-LOC SM-1 baseline; use `READY FOR CORRECTIVE IMPLEMENTATION ONLY`; keep the 24 completed stories, Epics 1-5, retrospectives, `done` states, and signed v1 evidence immutable; append approved corrections only; include the later-approved Story 6.7 and the order `6.1 -> 6.7 -> 6.2`; never initialize or traverse nested submodules.

**Block If:** The approved PRD/addendum and July 15 proposals yield contradictory ownership, OQ, performance, or Story 6.7 decisions; a required FR landing zone cannot be verified on an existing public platform surface; or the historical epic prefix / signed v1 evidence cannot be preserved byte-for-byte.

**Never:** Modify the finalized PRD/addendum, historical Epics 1-5 text, retrospectives, signed v1 evidence, production/runtime source, solution membership, submodule contents or gitlinks; remove AppHost/ServiceDefaults projects (Story 6.2); change UX governance (6.4), the thin template (6.5), release evidence (6.6), or implement the promotion gate (6.7); activate FR-16 or preserved feature scope.

</intent-contract>

## Code Map

- `_bmad-output/planning-artifacts/architecture.md` -- stale architecture authority, module/platform boundary, target tree, workflow, performance gate, and readiness conclusion.
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` -- historical Epic 1-5 plan requiring an EOF-only corrective authority amendment and Epic 6.
- `_bmad-output/implementation-artifacts/epic-6-context.md` -- derived developer context that must include Story 6.7 and corrected dependency order.
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` -- new executable contract for architecture/epic authority and append-only preservation.
- `tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` -- existing byte-integrity guard for frozen SM-2/v1 evidence.
- `src/Hexalith.Conversations.AppHost/` and `src/Hexalith.Conversations.ServiceDefaults/` -- current pre-6.2 drift to describe as migration input, not target ownership.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.{DomainService,ServiceDefaults,Aspire}/` and `references/Hexalith.Commons/src/libraries/Hexalith.Commons.{TenantAccess,Http,Serialization,Diagnostics}/` -- read-only public landing-zone evidence.

## Tasks & Acceptance

**Execution:**
- `_bmad-output/planning-artifacts/architecture.md` -- amend the existing document in place: rebaseline provenance, scope, starter, ownership, FR-10-FR-16 landing-zone register, OQ-1-OQ-5 states, SM-C2, target tree, verification/workflow, promotion-completion invariant, and readiness language; label local hosting projects as pre-6.2 drift rather than target architecture. Preserve all unaffected, still-binding domain/runtime decisions, including versioned events and mixed-stream replay/upcasting, EventStore precedence with quarantine/rebuild, fail-closed Parties writes and policy-defined read degradation, idempotency payload-mismatch/unknown-outcome handling, and approved legal-policy exceptions to immutable history. Prescribe the canonical `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()` pair; never teach direct `MapEventStoreDomainService()` use.
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` -- append the approved authority overlay, exact 24-row historical disposition table, corrective FR coverage, Epic 6 Stories 6.1-6.7, and dependency order at EOF without changing the existing prefix. Preserve the approved denominator literally: all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria; FR-16 is the only initiative non-activation. A delivered-to-inactive transition or compatible contract change requires named owner approval, rationale, and evidence.
- `_bmad-output/implementation-artifacts/epic-6-context.md` -- regenerate from the amended epic plan so Story 6.7, root-only submodule rules, and `6.1 -> 6.7 -> 6.2` are carried forward.
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` -- parse and assert canonical frontmatter authority, exact table keys and nonempty semantics, full FR surface lists, deferred FR-16, exactly one resolved row per OQ, a nonempty versioned hot-path inventory frozen before baseline with one-to-one post dispositions, reproducible `post P95 <= 1.05 x baseline` semantics, target ownership/tree, corrective-only readiness, append-only historical preservation, version-bound Epic 6 overlay/context correspondence, Story 6.7 sequencing, and the declared-promotion gitlink invariant. Verify named platform APIs are public through signature-aware source checks or compile-time/reflection evidence; raw substring presence is insufficient.
- `tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` -- replace current-file equality for superseded v1 inputs with a generic, shallow-clone-safe historical binding anchored to the immutable signed report/decision and its declared source identity; never substitute this workflow's baseline revision as v1 provenance. Current corrected artifacts remain governed by the new authority test.

**Acceptance Criteria:**
- Given the finalized initiative authority, when a maintainer reads the architecture, then it distinguishes initiative from preserved requirements, assigns FR-10-FR-15 to the approved platform surfaces, defers FR-16, resolves OQ-1-OQ-5, and contains no target-state Conversations AppHost/Aspire/ServiceDefaults ownership.
- Given the frozen pre-refactor benchmark envelope, when SM-C2 is evaluated, then every identified command/read hot path requires post-refactor P95 no greater than 1.05 times baseline under identical workload, data, concurrency, environment/runtime, tooling, warm/cold classification, repetitions, and commit-bound raw evidence.
- Given the historical epic plan and signed v1 record, when Story 6.1 completes, then the original epic bytes and v1 evidence remain unchanged while exactly one append-only authority overlay contains all 24 dispositions, corrective FR-3/10/13/17-20 coverage, and Stories 6.1-6.7.
- Given promotion-bearing corrective work, when planning dependencies are evaluated, then 6.1 precedes 6.7, 6.7 and the frozen benchmark precede 6.2 completion, 6.2 precedes 6.5, and 6.6 remains last.
- Given the amended planning artifacts, when the focused conformance executable runs, then it rejects stale provenance, local-host target ownership, missing/incorrect landing zones or OQs, weakened SM-C2 evidence, historical mutation, missing Story 6.7, or an incomplete promotion-completion invariant.

### Review Findings

Review pass 2 — 2026-07-26. Four adversarial layers (blind hunter, edge-case hunter, verification-gap, acceptance auditor). Independently reproduced: conformance build 0/0, focused 11/11 and 11/11, full conformance 401/401, `git diff --check` clean, signed commit `c6670fac` resolvable (clone not shallow).

- [x] [Review][Patch] Bump overlay/architecture authority to v2 and regenerate the derived context — RESOLVED 2026-07-26: bump to v2 and regenerate. Commit `d91c1cf` appended ADR-0003 obligations to the frozen overlay (`epics.md` Story 6.2 AC4-6, Story 6.6 AC4) and to `architecture.md:115-139` without bumping `overlay_version`/`authorityVersion`. Overlay contains 3 ADR-0003 references; `epic-6-context.md` contains 0, while still declaring `overlay_version: 'epic-6-authority-2026-07-15-v1'` and asserting that semantic drift is a conformance failure — so Story 6.2's dev agent would miss the mandatory production-path proof. Action: bump `overlay_version` and `authorityVersion` to the `-v2` identity, regenerate `epic-6-context.md` from the current overlay including the ADR-0003 obligations, and update `ArchitectureVersion`/`OverlayVersion` in the test.
- [x] [Review][Decision] Five uncommitted root gitlink drifts — RESOLVED 2026-07-26: leave untouched, exclude from the Story 6.1 commit. `references/Hexalith.Builds` 513b9bd→7ac2849, `.Commons` ea1fc45→427530e, `.EventStore`, `.Memories`, `.Tenants`. Builds and Commons moved during the review via an external `pull --tags origin main` at 2026-07-26 19:45 (reflog-confirmed; not issued by the review). No submodule is reverted or updated. When Story 6.1 is committed, stage only the declared File List paths so no gitlink moves are swept in — the failure that required review correction in Stories 2.2 and 3.3. Disclosure of this state is handled by the Boundary Confirmation patch below.
- [x] [Review][Decision] Exact row-count freezes versus documented growth — RESOLVED 2026-07-26: keep the hard freeze, add content pinning. `ArchitecturePlanningAuthorityValidationTest.cs:59` (`ShouldBe(7)`), `:81` (`ShouldBe(5)`), `:177` (`ShouldBe(24)`) stay as deliberate change-detectors, so an approved addition (an extra OQ-5 absolute gate, an FR-17 landing) requires a conscious test edit. The complementary axis — pinning load-bearing cell content so a row's meaning cannot be rewritten silently — is covered by the per-cell and disposition-pinning patches below.
- [x] [Review][Patch] Historical-binding fallback is tautological — byte-immutability dropped for 19 declared v1 artifacts [tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs:156-179] — Verified by hash analysis: all 19 non-release-evidence artifacts have `declared hash == blob hash at c6670fac`, so `historicalSha256.ShouldBe(sha256)` compares a constant against the commit it was computed from and can never fail; 0 artifacts exist where it could fail. Exactly 1 artifact actually drifts (`architecture.md`). The deleted `ComputeFileSha256(fullPath).ShouldBe(sha256)` was the only hash pin on the finalized PRD, 2 epic retrospectives, 15 completed story records, and `docs/domain-module-authoring-template.md` — all declared immutable by this story's own Never list. Restore current-content equality as the default and exempt only an enumerated, reviewed supersession list.
- [x] [Review][Patch] Unresolvable git history degrades two tests to zero-assertion green passes [tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs:170-173,418-421] — Reproduced with `GIT_DIR=/nonexistent`: 11/11 "passed" with every superseded-artifact check and the entire evidence-boundary test skipped, including `ShouldNotContain("160000")` — the exact gitlink-exclusion invariant Story 6.7 must mechanize. Triggered by `git clone --depth 1` or `actions/checkout` default `fetch-depth: 1`. Use `Assert.Skip` or fail, and add a positive assertion that the historical path actually executed.
- [x] [Review][Patch] Target-state host ownership and readiness verdict are only checked inside one extracted block each [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:123-125,131-133] — Two injections passed 11/11: target-state Conversations AppHost/ServiceDefaults ownership asserted at `architecture.md:51`, and `READY FOR IMPLEMENTATION ... FR-16 activation is authorized` at `architecture.md:1465`. Both directly negate AC1/AC5. Add whole-document negative scans permitting these names only inside spans labeled historical/superseded/pre-6.2.
- [x] [Review][Patch] SM-C2 freeze-before-capture is unasserted and the sole numeric guard is vacuous [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:93-110] — `"5% P95 regression"` is a substring of `"45% P95 regression"`, so OQ-5 can be inverted to 45% and stay green (`:89`). No assertion covers freeze-before-baseline ordering; the 8-item envelope loop matches nouns, so prohibitions rewritten as permissions pass. Anchor the percentage numerically and assert the polarity-bearing clauses verbatim.
- [x] [Review][Patch] Landing-zone register is decoupled from the API verification that should prove it [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:63-71,259-324] — Register rows are checked only for module-name substrings while the API list is hardcoded, so a row can name a nonexistent API or a Conversations facade OQ-1 forbids and still pass. `AddServiceDefaults` is named at `architecture.md:68` and never verified (real declaration is generic: `TBuilder AddServiceDefaults<TBuilder>`). Drive `AssertPublicStaticMethod` from the register's backticked identifiers.
- [x] [Review][Patch] Empty `parameterFragments` makes signature verification vacuous [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:340-341] — `All(...)` on an empty array returns true, so `AddEventStoreDomainModule` (`:287-290`) and `CreateWeb` (`:311-314`) verify name and return type only. Declared generic arity `AddTypedHttpClient<TClient,TImplementation,TOptions>` is unchecked because `(?:<[^>]+>)?` accepts any arity.
- [x] [Review][Patch] The overlay's binding dependency order is read by no test [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:209-241] — `epics.md` `### Binding Dependency Order` is unasserted; injecting `6.1 -> 6.2 -> 6.7`, "6.2 may complete before 6.7", and "6.5 may precede 6.2" passed 11/11. `6.2 precedes 6.5` is unenforced everywhere. The overlay is the artifact Story 6.2's dev agent reads, so it can authorize exactly the sequence AC4 forbids.
- [x] [Review][Patch] Load-bearing disposition cells can be inverted [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:177-180] — Rewriting rows 3.4/3.5 to "Retain the Conversations-owned ServiceDefaults/AppHost project as target architecture" passed. Those two rows carry the entire ownership correction; only row count, keys, arity and non-emptiness are checked.
- [x] [Review][Patch] Architecture's own denominator section has no assertion [_bmad-output/planning-artifacts/architecture.md:43-47] — No test reads `### Scope And Preservation Denominators`; rewriting it to "21 initiative requirements, 1 Feature-FR, 0 Feature-NFRs" and "The SM-1 baseline is renegotiable" passed. AC1's subject document is unguarded; the 20/104/77/52 and 13,289 literals are asserted only against the overlay and context.
- [x] [Review][Patch] Root of trust is unpinned inside the suite [tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs:120,507-518] — `sourceCommit` is read from JSON and validated only as 40 hex characters, so a fabricated commit disables the whole binding via the unavailable-history path. `oq-2-target-interpretation-decision-v1.json` is pinned nowhere in the dotnet suite. Pin the commit and the decision hash as source constants, as the epic prefix already is.
- [x] [Review][Patch] Row content assertions match the whole row, not the intended cell [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:64-71,87-89] — `rows.Single(...)` returns the full row string, so `deferred-non-activated`, `>=40%`, and owner names satisfy the check from any cell, including a Reopen-condition or responsibility cell. Assert per cell.
- [x] [Review][Patch] SM-C2 non-emptiness covers only cell 4 [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:102] — The other three tables use `Skip(1).All(nonempty)` (`:62`, `:84`, `:180`); an inventory row with empty classification and operation cells passes the only gate backing AC2.
- [x] [Review][Patch] Section-extraction boundary silently widened by an inserted section [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:140] — Confirmed order: `### Still-Binding Domain And Runtime Decisions` (architecture.md:107) → `### Projection Read-Store Population Decision` (:115) → `### Promotion Completion Invariant` (:141). The extracted region now includes the whole intervening ADR section, whose topics overlap (projection precedence, idempotent retry, replay), so the still-binding bullets could be deleted and the assertions satisfied by the inserted text.
- [x] [Review][Patch] Public-surface evidence is read from submodule working trees, not the recorded gitlinks [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:259-324] — Five submodules currently show `+` (checkout ≠ index), so the "verified public platform surface" is measured against commits the umbrella does not pin. This is also the first `references/`-reading test in Conformance.Tests, so the project now throws `DirectoryNotFoundException` on any clone without root submodules initialized, instead of failing with an actionable message.
- [x] [Review][Patch] "Exactly one row per OQ" is enforced only inside one extracted block [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:75-89] — `architecture.md:88` claims "exactly one authoritative row for each OQ-1 through OQ-5; prose elsewhere cannot reopen or contradict a row", but a duplicate contradictory row appended after the section is undetected. `ExtractBetween` (`:362-368`) also takes the first occurrence only, so a duplicate section heading hides contradictory rows.
- [x] [Review][Patch] Boundary Confirmation is factually wrong on scope and gitlinks [_bmad-output/implementation-artifacts/spec-6-1-rebaseline-architecture-and-planning-authority.md:131] — States "Changes are confined to the two conformance test files and this story record" while the File List declares 7 files and `sprint-status.yaml` is also modified; and claims no gitlink changed while five drift in the working tree. Correct the wording to match the File List and disclose the gitlink state.
- [x] [Review][Patch] Story-owned files silently carry ~31 lines from three unrelated commits [_bmad-output/planning-artifacts/architecture.md, .../epics.md, _bmad-output/implementation-artifacts/sprint-status.yaml] — Commit `d91c1cf` appended ADR-0003 acceptance criteria to the overlay and `architecture.md:115-139`/`:701`, and flipped Epic 5 retrospective action A3 from `open` to `done` (`sprint-status.yaml:217-219`); `e366de0`/`f52c1a5` rewrote SDK-pin prose (`architecture.md:408`, `:410`, `:643`). None is Story 6.1 work and none is disclosed in the record.
- [x] [Review][Patch] No line-ending policy, and phrase assertions are whitespace-exact [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:142-151,159,163-165,353] — `git check-attr` returns `eol: unspecified` for `architecture.md` and `epics.md`, and `.gitattributes` has no `* text=auto`. A default Windows clone (`core.autocrlf=true`) breaks the frozen prefix hash, `ShouldStartWith("---\n")`, and the overlay-boundary check, and reports every `docs/release-evidence/*.md` as signed-evidence tampering. Reflowing `architecture.md:109-113` to the ~90-char width used 6 lines later breaks ~50 multi-word assertions on semantically unchanged text.
- [x] [Review][Patch] Git process handling can hang or lose diagnostics [tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs:528-532,546,552-557] — stdout is drained to completion before stderr is read (classic pipe deadlock if git writes enough to stderr); `WaitForExit()` has no timeout; a missing `git` raises `Win32Exception` past the graceful-degradation path (the `?? throw` guard never fires for `Process.Start(ProcessStartInfo)`); output encoding and `core.quotepath` are unset, so a non-ASCII changed path mismatches; and git's stderr is now read and discarded rather than included in the failure message.
- [x] [Review][Patch] Signed-evidence strictness keys off a raw case-sensitive prefix [tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs:164-165] — A manifest entry spelled `./docs/release-evidence/x.json`, `docs/Release-Evidence/x.json`, or with backslashes fails `StartsWith("docs/release-evidence/", Ordinal)`, reclassifying signed evidence as a superseded planning input and tolerating its drift. Normalize the path before the prefix test.
- [x] [Review][Patch] Corrective FR-coverage table gets substring-only validation [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:200-205] — Unlike the disposition table, there is no row parsing, cell arity, or non-empty landing check; the table could collapse to a prose mention, and coverage rows for FR-11/12/14/15/16 can be deleted outright while green. Related: `:189-198` collects `FR-\d{1,2}` as a set union, so `FR-01` normalizes to 1 and a contradictory added sentence passes.
- [x] [Review][Patch] Current-presented performance and release-gate sections were never reconciled with SM-C2 [_bmad-output/planning-artifacts/architecture.md:163,696-706,722-731] — Release Gates still say only "Performance smoke baseline captured and compared for reference dataset", Performance Architecture Decisions never mentions SM-C2, the 1.05x rule, or the frozen inventory, and `P95 <= 500ms` is still presented as a current driver although OQ-5 says preserved absolute targets activate only through a current release decision.
- [x] [Review][Patch] Feature-FR namespace is presented as current PRD authority in unlabeled sections [_bmad-output/planning-artifacts/architecture.md:155,161,169,1266-1280] — "The PRD defines 104 functional requirements" / "77 non-functional requirements" attribute the preserved feature denominators to "the PRD", which the rebaseline redefines as the 20-FR initiative PRD; `:169` still lists "Aspire/AppHost integration" as an architectural component; `### Requirements to Structure Mapping` maps `FR95-FR99 Observability -> ServiceDefaults` using bare `FR1-FR104` labels that collide with `FR-1..FR-20`. Other historical sections were explicitly relabeled (`:331`, `:355`, `:635`, `:1131`, `:1461`); these were not.
- [x] [Review][Patch] Fault-injection evidence covers only the checks that cannot fail to notice [_bmad-output/implementation-artifacts/spec-6-1-rebaseline-architecture-and-planning-authority.md:118-127] — All 6 listed mutations target SHA-256 or byte-boundary comparisons. Zero target landing-zone/public-API verification, OQ resolution, SM-C2 semantics, target-ownership prose, disposition content, or Story 6.7 sequencing — and 12 mutations against those passed green during this review. Rerun injection with one mutation per acceptance criterion.
- [x] [Review][Patch] PRD and addendum are hash-pinned nowhere [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:45-46] — Only their paths are asserted as cited. The tests hard-code "20 initiative FRs", so a change to the PRD's requirement count leaves the plan asserting the old number and passing. `epics.md` is byte-pinned; its two authority siblings are not.
- [x] [Review][Patch] `AssertPublicMethod` accepts non-consumer-callable and commented-out text [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:332-342] — The declaring type's accessibility is never checked, so a `public` member of an `internal` class passes; the regex also matches inside `///` doc samples, `//` comments, and `#if false` regions. This is the check the spec added because "raw substring presence is insufficient".
- [x] [Review][Patch] Markdown/parse hardening bundle [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:381-414] — Alignment separators (`|:---|`) are counted as data rows and break the exact-count assertions; `Split('|')` has no escape handling so a cell containing `\|` or an inline-code pipe changes cell counts; `MarkdownDataRows` is not fence-aware; `-`, `TBD`, U+200B and U+FEFF pass `IsNullOrWhiteSpace`; `resolved-` is an open prefix accepting `resolved-pending`; `Regex.Escape("sealed class")` pins exactly one space so `public sealed partial class` fails; frontmatter is matched as text so a commented `# status:` line satisfies `:42`; context frontmatter is never delimited (`:215-217`); manifest hex case is not normalized; and `ShouldNotContain("160000")` is a substring test over the whole `--raw` output rather than the mode column.
- [x] [Review][Patch] Record metadata inconsistencies [_bmad-output/implementation-artifacts/epic-6-context.md:3, _bmad-output/planning-artifacts/architecture.md:1-14] — Context declares `generated: '2026-07-15'` while validation executed 2026-07-26; `architecture.md` retains `stepsCompleted: [1..8]`, `lastStep: 8`, `completedAt: '2026-05-14'` alongside `status: 'corrective-implementation-only'`, and `sessionResumedAt`/`sessionExitedAt` were deleted rather than appended to.
- [x] [Review][Defer] The planning gate has no automated execution path [.github/] — deferred, pre-existing. No `.github/workflows` directory or any pipeline definition outside `references/`. Every assertion in this story depends on an operator remembering to run the conformance executable; the guarded artifacts are markdown that later agents edit without a build step.
- [x] [Review][Defer] Nested submodule administrative metadata exists under a root submodule [.git/modules/references/Hexalith.FrontComposer/modules/] — deferred, pre-existing. `Hexalith.AI.Tools` and `Hexalith.Builds` module directories exist with mtimes 2026-07-14/15, predating this story; both working trees are empty (0 entries), so they are registered but not checked out. Not attributable to this pass.

## Spec Change Log

### 2026-07-26 — Review pass 2 (adversarial, four layers)

- Trigger: the pass-1 gate was green and its reported numbers were truthful, but the gate itself was far weaker than claimed. Fourteen semantic mutations to the guarded artifacts passed green, and 4 of the 9 pass-1 `bad_spec` findings were not actually closed. The replacement historical binding was a tautology that removed the only working-tree hash pin from 19 declared v1 artifacts, including the finalized PRD and both epic retrospectives.
- Amendment: no change to intent or acceptance criteria. Verification is strengthened to be semantic rather than token-presence: prohibitions are scanned document-wide, polarity-bearing clauses are asserted verbatim with their inversions asserted absent, numeric bounds are anchored, API verification is driven from the register it is meant to prove, the root of trust is pinned in source, and unavailable history skips instead of passing.
- Authority versioning: commit `d91c1cf` amended the frozen overlay after its freeze without a version bump, leaving the derived context stale at a matching version string. Both authorities move to `-v2`, the overlay gains an amendment log, and the context is regenerated.
- Known-bad state avoided: a planning gate reporting 401/401 while the artifacts it guards can be rewritten to authorize module-owned hosting, a 45% performance regression, a reversed dependency order, and mutable retrospectives.
- KEEP: all pass-1 corrections remain in force, including the byte-based append-only assertion and the tamper-evident decision guard, with the historical binding narrowed to a one-entry reviewed allowlist.

### 2026-07-15 — Review pass 1 re-derivation
- Trigger: the first implementation over-compressed the architecture, removed still-binding safety decisions, taught the lower-level mapper, weakened approved Epic 6 semantics, and added verification that could pass on token presence or the wrong historical provenance.
- Amendment: tasks now require in-place preservation of unaffected architecture constraints, the canonical domain-host pair, literal preservation denominators/approval rules, a frozen versioned hot-path inventory, semantic/version-bound authority checks, public-surface verification, and a shallow-clone-safe signed-v1 binding.
- Known-bad state avoided: a green planning gate that silently drops replay, projection, participant, idempotency, or legal safeguards; permits denominator drift; derives a host without default endpoints; or binds v1 evidence to the workflow baseline.
- KEEP: retain the corrected authority chain, exact FR landing-zone ownership, OQ decisions, SM-C2 formula/envelope, current-versus-target topology distinction, byte-identical Epic 1-5 prefix and signed v1 files, all 24 dispositions, Stories 6.1-6.7 with `6.1 -> 6.7 -> 6.2`, regenerated context, lifecycle synchronization, zero runtime/submodule changes, and the previously green focused/full conformance lanes.

## Review Triage Log

### 2026-07-26 — Review pass 2

- Layers: blind hunter, edge-case hunter, verification-gap, acceptance auditor (all four completed; none failed).
- intent_gap: 0
- decision_needed: 3 (all resolved: bump authority to v2 and regenerate; leave gitlinks untouched and exclude from the commit; keep the exact row-count freeze and add content pinning)
- patch: 29 applied (high 8, medium 15, low 6)
- defer: 2 (no CI execution path for the gate; pre-existing nested-submodule metadata under FrontComposer — both recorded in `deferred-work.md`)
- dismiss: 4 (`MapEventStoreDomainService` taught elsewhere — verified only the prohibition exists; the toothless `Hexalith.Conversations.Aspire` negative — harmless guard; raw exception messages in `LoadEvidenceArtifact` — cosmetic; "architecture pinned to a workflow baseline" — the spec requires `baselineRevision` in frontmatter, so this is by design)
- highest-severity findings:
  - `[high]` Historical binding was tautological; 19 declared v1 artifacts lost their only byte-immutability pin. Measured 19/19 tautological, 0 able to fail.
  - `[high]` Unresolvable git history produced zero-assertion green passes in two tests, erasing the `160000` gitlink-exclusion guarantee.
  - `[high]` Target-state host ownership and unqualified readiness were checked inside one block each; both injections passed pass-1.
  - `[high]` `"5% P95 regression"` is a substring of `"45% P95 regression"`, making the only numeric performance guard vacuous.
  - `[high]` Landing-zone register was decoupled from API verification; a row could name a nonexistent API or a forbidden facade.
  - `[high]` Overlay dependency order was unasserted, so the epic plan could authorize the sequence AC4 forbids.
  - `[high]` Load-bearing dispositions 3.4/3.5 could be reversed to reinstate module-owned hosting.
  - `[high]` The architecture's own denominator section had no assertion at all.
- self-corrected during application: two bugs in the new scans were caught by re-injection — a section-exemption token (`"migration input"`) that exempted the target-ownership section, and suffix-only owner matching that accepted `Hexalith.Conversations.Http` for `Hexalith.Commons.Http`.

### 2026-07-15 — Review pass
- intent_gap: 0
- bad_spec: 9: (high 7, medium 2, low 0)
- patch: 0
- defer: 0
- reject: 8: (high 3, medium 4, low 1)
- addressed_findings:
  - `[high]` `[bad_spec]` Restore still-binding replay/upcasting, projection precedence/rebuild, Parties degradation, idempotency, and legal-policy safety decisions removed by the rewrite.
  - `[high]` `[bad_spec]` Require `AddEventStoreDomainService` plus `UseEventStoreDomainService` and prohibit the lower-level mapper as authoring guidance.
  - `[high]` `[bad_spec]` Preserve the approved manifest denominator and restrict non-activation so mandatory initiative requirements cannot disappear.
  - `[medium]` `[bad_spec]` Require named approval, rationale, and compatibility evidence for delivered-to-inactive or compatible-change dispositions.
  - `[high]` `[bad_spec]` Freeze a nonempty versioned hot-path inventory before baseline capture so SM-C2 cannot cherry-pick the denominator.
  - `[high]` `[bad_spec]` Bind Epic 6 overlay and generated context semantics/version rather than checking only identifiers and phrases.
  - `[high]` `[bad_spec]` Verify complete public landing-zone surfaces with signature-aware evidence instead of incomplete source substrings.
  - `[high]` `[bad_spec]` Anchor superseded v1 inputs to signed historical provenance without depending on this workflow baseline or shallow-clone object availability.
  - `[medium]` `[bad_spec]` Strengthen target-tree, provenance, OQ, and table validation so equivalent ownership or contradictory rows cannot pass outside one searched block.

## Design Notes

The architecture describes two states explicitly: current local AppHost/ServiceDefaults assets are pre-6.2 migration evidence, while the target domain module owns only contracts, behavior, handlers, projections, adapters, domain telemetry definitions, and optional domain UI. Missing generic capability is extended in its owning platform surface, never wrapped in a Conversations facade.

The promotion invariant is declarative here and implemented in Story 6.7: affected root-declared submodules must be clean, the declared commit must satisfy availability policy, and the committed umbrella revision must contain the exact mode-`160000` gitlink. Only declared promotions and changed gitlinks block; unrelated state warns.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release /nr:false /m:1` -- expected: zero warnings and errors.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.ArchitecturePlanningAuthorityValidationTest` -- expected: all authority and preservation checks pass.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.OqTwoTargetInterpretationDecisionValidationTest` -- expected: frozen SM-2 and signed v1 hashes remain valid.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests` -- expected: the full conformance suite passes.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.

## Validation Results

Executed 2026-07-26 on baseline `f31aa5ada2e37e1ec5f3e4b8e907525b37da863f`, after review pass 2 applied 31 patches.

- Release build of `tests/Hexalith.Conversations.Conformance.Tests`: 0 warnings, 0 errors.
- Release build of `Hexalith.Conversations.slnx`: 0 warnings, 0 errors.
- `ArchitecturePlanningAuthorityValidationTest`: 17 / 17 passed (was 11; +6 from split and added semantic checks).
- `SuccessMetricReportAndAttestationValidationTest`: 12 / 12 passed (was 11; +1 supersession-allowlist guard).
- Full conformance suite: 408 / 408 passed, 0 failed, 0 skipped (was 401).
- Full solution regression across all 9 test projects: 1,887 / 1,887 passed, 0 failed, 0 skipped (Admin.Web 14, AppHost 7, Client 29, Conformance 408, Contracts 618, IntegrationTests 9, Server 610, ServiceDefaults 7, Conversations 185). The +7 delta is exactly the conformance delta.
- `git diff --check`: clean.
- Frozen historical epic prefix re-verified after the overlay version bump: 55,536 bytes, sha256 `bd437b802513591c4af299ff0997bb694ced40304e1a178c3d53e95f88f0e8a8`, byte-identical.

### Review Pass 2 Corrections

Review pass 2 found the pass-1 gate substantially weaker than its own claims: 14 semantic mutations to the guarded artifacts passed green, and 4 of the 9 pass-1 `bad_spec` findings were not actually closed. The load-bearing corrections:

- **The historical binding was a tautology.** `SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity` compared the declared hash against the blob at the commit that hash was computed from, so the fallback could never fail. Measured: all **19** non-release-evidence artifacts satisfied `declared == blob@signed`, and **0** existed where the branch could fail; only `architecture.md` actually drifts. The single needed exemption had unpinned nineteen files, including the finalized PRD, both epic retrospectives, and 15 completed story records that this story's own Never list calls immutable. Current-content equality is restored as the default, with an explicit `SupersededByCorrectiveAuthority` allowlist of exactly one path and a new guard keeping that list narrow and free of signed evidence.
- **Silent no-op passes removed.** Unresolvable history produced zero-assertion green passes in two tests (reproduced with `GIT_DIR=/nonexistent`), erasing the `160000` gitlink exclusion that Story 6.7 exists to mechanize. Unavailable history now calls `Assert.Skip`, and a counter asserts the historical path actually executed.
- **Root of trust pinned in source.** The declared v1 source commit, the signed release-owner decision, and the OQ-2 decision are now pinned as test constants, so a coordinated multi-file edit can no longer satisfy the suite.
- **Prohibitions now scanned document-wide.** Target-state module-owned hosting and unqualified `READY FOR IMPLEMENTATION` were only checked inside one extracted block each; both are now scanned across every line, exempting only explicitly historical/superseded sections or qualified lines.
- **Verification coupled to the register.** Public-API checks are driven from the identifiers the landing-zone register actually names, including declared generic arity, so a row repointed at a nonexistent API or a Conversations facade fails. `AddServiceDefaults` is now verified (generic `TBuilder` form). Comments and disabled regions are stripped before signature matching, and the declaring type must be public.
- **Overlay amended post-freeze without a version bump.** Commit `d91c1cf` added the ADR-0003 obligations to the frozen overlay while `overlay_version` stayed `v1`, so the version-bound correspondence check compared two matching strings and passed while `epic-6-context.md` carried none of the new obligations. Both authorities are now `-v2`, the overlay carries an amendment log, the context is regenerated with the ADR-0003 obligations, and correspondence is asserted row-by-row against the register rather than by phrase presence.
- **Two of my own scan bugs, caught by re-injection.** The section-exemption list initially included `"migration input"`, which the heading *Target Ownership And Current **Migration Input*** matched, exempting the very section where target-state claims matter most; and the context owner check matched the suffix `Http`, which `Hexalith.Conversations.Http` also contains. Section exemptions are now limited to explicit history, and owner matching uses the last two module segments plus a blanket rejection of any `Hexalith.Conversations.*` landing zone.
- **A third self-correction in the line-ending policy.** The first `.gitattributes` form used directory-wide `_bmad-output/** text eol=lf`, which forces text treatment on the binary evidence screenshots under `implementation-artifacts/evidence/`; git reported it would rewrite CRLF byte sequences inside 39 PNGs, corrupting them. The policy is now scoped by text extension, with `*.png`/`*.jpg`/`*.pdf`/`*.zip` explicitly marked `binary`. Verified: PNGs resolve to `binary: set` / `text: unset`, the gated artifacts resolve to `eol: lf`, the intentionally-CRLF shared agent entry points remain `unspecified`, and no file renormalizes.

### Fault Injection — Review Pass 2 (each mutation reverted; working tree verified clean)

Pass 1 injected only SHA-256 and byte-boundary mutations, which are the checks that cannot fail to notice. This pass injects one mutation per acceptance criterion, targeting the semantic checks that are the actual deliverable.

| Injected drift | Result | Caught by |
| --- | --- | --- |
| Target-state module-owned AppHost ownership in the live target-ownership section | FAIL | `NoTargetStateOwnershipOrUnqualifiedReadinessShouldSurviveAnywhereInTheDocument` |
| Unqualified `READY FOR IMPLEMENTATION` added outside a historical section | FAIL | `NoTargetStateOwnershipOrUnqualifiedReadinessShouldSurviveAnywhereInTheDocument` |
| OQ-5 inflated from 5% to 45% P95 (previously passed: `"5% P95"` is a substring of `"45% P95"`) | FAIL | `ArchitectureShouldResolveEveryOpenQuestionExactlyOnce` |
| Denominator rewritten from 20 to 21 initiative requirements | FAIL | `ArchitectureShouldStateScopeAndPreservationDenominators` |
| SM-C2 freeze inverted to "rows may be selected after measurement" | FAIL | `SmCTwoShouldFreezeNonemptyInventoryAndOneToOneComparablePostResults` |
| Overlay dependency order flipped to `6.1 -> 6.2 -> 6.7` | FAIL | `EpicOverlayShouldBindTheDependencyOrderItAuthorizes` |
| Disposition row 3.5 reversed to "Retain the Conversations-owned AppHost as target architecture" | FAIL | `EpicPlanShouldPreserveHistoricalPrefixAndContainExactDispositionRows` |
| Landing-zone register repointed at a `Hexalith.Conversations.Hosting` facade | FAIL (2) | `ArchitectureRegistersExactInitiativeLandingZonesAndDeferredFrSixteen` and the register-driven API check |
| Register names a nonexistent API (`AddConversationsServiceDefaultsFacade`) | FAIL | `NamedPlatformLandingZonesShouldExposeSignatureCompatiblePublicApis` |
| Declared generic arity reduced to `AddTypedHttpClient<TClient,TImplementation>` | FAIL | `NamedPlatformLandingZonesShouldExposeSignatureCompatiblePublicApis` |
| Derived context FR-12 owner changed to a module-owned facade | FAIL | `EpicOverlayAndGeneratedContextShouldBeVersionAndStoryEquivalent` |
| Byte appended to `epic-3-retro-2026-06-24.md`, a declared v1 source artifact (previously passed green) | FAIL | `SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity` ("not a reviewed supersession") |

### Boundary Confirmation

Story 6.1 changes planning authority, its conformance validation, the repository line-ending policy for the artifacts that gate hashes, and this story record. No production or runtime source, solution membership, submodule content, UX artifact, thin template, signed release evidence, retrospective document, historical epic prefix, or finalized PRD/addendum byte changed. The frozen 55,536-byte epic prefix is byte-identical and re-verified after the overlay version bump.

**Submodule gitlink state (disclosed, not authored by this story).** The working tree carries five uncommitted root gitlink moves: `references/Hexalith.Builds` `513b9bd`→`7ac2849`, `.Commons` `ea1fc45`→`427530e`, `.EventStore`, `.Memories`, and `.Tenants`. Builds and Commons fast-forwarded during review via an external `pull --tags origin main` at 2026-07-26 19:45 (reflog-confirmed); no review command issued a pull. Per the resolved review decision these are left untouched, and the Story 6.1 commit must stage only the declared File List paths so no gitlink move is swept in — the failure that required review correction in Stories 2.2 and 3.3. Under the promotion-completion invariant this story declares, that state would otherwise block completion.

**Unrelated content carried in story-owned files (disclosed).** Three of the declared files also contain roughly 31 lines from commits that are not Story 6.1 work, landed after the declared baseline: `d91c1cf` ("approve projection read-store proof ADR") appended the ADR-0003 acceptance criteria to the overlay and `architecture.md`, added a `sprint-status.yaml` header comment, and flipped Epic 5 retrospective action A3 from `open` to `done`; `e366de0` and `f52c1a5` rewrote SDK-pin prose in `architecture.md` and net to zero. The A3 flip is an `action_items` status entry, not a change to a retrospective document. Review pass 2 reconciles the ADR-0003 content into the versioned `-v2` authority rather than leaving it as an unversioned post-freeze amendment.

### File List

- `.gitattributes`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-6-context.md`
- `_bmad-output/implementation-artifacts/spec-6-1-rebaseline-architecture-and-planning-authority.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs`

### Superseded Pass-1 Validation Record

The pass-1 record claimed 11/11, 11/11, 401/401, and 1,880/1,880. All of those figures were independently reproduced and were truthful; they are superseded by the counts above only because the gate was strengthened. The pass-1 corrections it described (byte-based append-only assertion, the historical-binding replacement, and the added decision guard) remain in place, with the historical binding now narrowed to a reviewed allowlist.

### Pass-1 Defect History (retained for provenance)

These were the corrections review pass 1 made. They are retained because they remain in force; where pass 2 changed them, that is noted.

- `EpicPlanShouldPreserveHistoricalPrefixAndContainExactDispositionRows` compared a decoded character index against the frozen 55,536-byte prefix length. The historical prefix contains multi-byte characters, so the assertion failed (55,277 vs 55,537) even though the prefix was byte-identical. The append-only property is asserted on bytes: the region after the frozen prefix must begin with the overlay `BEGIN` marker declaring the same `prefix-bytes`/`prefix-sha256` boundary, must end with the matching `END` marker, and each marker must occur exactly once. **Pass 2 addition:** the frozen boundary is also verified against the epic plan at the work baseline commit, so a wrong freeze cannot stay self-consistently green.
- `SourceArtifactsShouldBeRepositoryRelativeExistingFilesWithHashes` required every signed v1 source artifact to equal current working-tree content, which this story's lawful amendment of `architecture.md` broke. **Pass 2 correction:** the replacement was a tautology that unpinned 19 files; current-content equality is restored as the default, with a one-entry reviewed supersession allowlist, and the unavailable-history path now skips instead of passing.
- Added `SignedReleaseOwnerDecisionShouldStillBindTheImmutableV1ReportAndSourceIdentity` so the declared manifest stays tamper-evident. **Pass 2 addition:** the decision file and the OQ-2 decision are pinned as source constants, terminating the trust chain in code rather than in data.
