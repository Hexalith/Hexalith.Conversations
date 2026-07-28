---
title: "Sprint Change Proposal — Evidence-Boundary Validation Pattern Consolidation"
project: "Conversations"
date: "2026-07-28"
status: "approved"
changeScope: "moderate"
mode: "incremental"
trigger: "Standing Epic 5 retrospective action A5, reaffirmed by the Story 6.1 review findings"
affectedAuthority: "epic-6-authority-2026-07-28-v5 and conversations-architecture-2026-07-28-v5"
proposedAuthority: "epic-6-authority-2026-07-28-v6 and conversations-architecture-2026-07-28-v6"
supersedesProposalScope: "none — additive to sprint-change-proposal-2026-07-28.md and sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md"
---

# Sprint Change Proposal — Evidence-Boundary Validation Pattern Consolidation

## 1. Issue Summary

### Problem statement

Story 5.3 produced a validation test that knows how to prove an evidence
artifact is trustworthy: recompute every declared hash rather than read it back,
keep every declared path inside the repository, assert the changed-file boundary
as set equality, exclude submodule gitlinks by parsing diff mode columns, and
skip visibly rather than pass silently when history cannot be resolved. That
knowledge lives in one 769-line file
(`tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs`)
and in nobody's workflow. Every subsequent evidence story has re-derived parts of
it by hand, unevenly, and in each case the gaps were found during *review* rather
than during development.

### How it was discovered

This is not a new finding. It is a standing Epic 5 retrospective action item in
`_bmad-output/implementation-artifacts/sprint-status.yaml` (epic 5, owner
`Dev workflow`, status `open`):

> "Promote the Story 5.3 evidence-boundary validation pattern into reusable
> dev/review guidance."

Its done-when clause is explicit about *when* the validation has to happen:

> "Future evidence stories validate source hashes, manifest containment, signable
> payload hash, changed-file set, submodule exclusion, and inventory row identity
> **before review**."

The item has been open since 2026-06-27. Two Epic 6 code reviews have since paid
for its absence.

### Evidence

**The same git subprocess exists three times, at three different strengths.**

| Concern | `SuccessMetricReportAndAttestation…` | `ProjectionReadStorePopulationProof…` | `SmC2BaselineReconstruction…` |
| --- | --- | --- | --- |
| Timeout | 60s, bounded | 120s | 60s |
| stderr | drained concurrently with stdout | sequential `ReadToEnd` after stdout | sequential `ReadToEnd` after stdout |
| Stream decoding | UTF-8 explicitly | ambient console codepage | ambient console codepage |
| Path quoting | `-c core.quotepath=false` | — | — |
| git absent | reported unavailable | throws | reported unavailable |
| History unresolvable | `Assert.Skip` | throws | `Assert.Skip` + executed-check counter |

The sequential `ReadToEnd` pair in two of the three is a deadlock shape: draining
stdout to completion while git is blocked writing a full stderr pipe hangs both
processes. The Story 5.3 implementation carries an in-source comment explaining
precisely that hazard; the two later copies do not, because the reasoning was
never anywhere a later author would read it.

**Only one of the three excludes submodule gitlinks, and only one pins a root of
trust.** `grep -ln "160000" tests/ --include=*.cs` returns three files;
reviewed supersession allowlists and source-pinned signed hashes exist in exactly
one.

**The copy-paste surface is large and growing.** A private repository-root walk is
defined in **31 test files**. Ad-hoc SHA-256 helpers appear in 6 conformance
files. `docs/release-evidence/` is read by **24 files across 3 test projects** —
17 in `Conformance.Tests`, 6 in `Contracts.Tests`, and 1 in `IntegrationTests`.

**The cost has already been paid twice, in review rather than in dev.** The Story
6.1 code review (pass 2, 2026-07-26) had to rebuild two of these invariants from
scratch after finding:

1. **19 of 19 non-evidence artifact bindings were tautological** — comparing a
   declared hash against the commit it was computed from, which can never fail.
   The fix was current-content equality plus a one-entry reviewed supersession
   allowlist. That is the Story 5.3
   `SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity`
   design, re-derived at review cost.
2. **Unresolvable git history produced zero-assertion green passes in two tests**,
   reproduced with `GIT_DIR=/nonexistent`, erasing the mode-`160000` exclusion
   guarantee. The fix was `Assert.Skip` plus a positive executed-path counter —
   again the Story 5.3 design, re-derived.

Both were caught. The point of the action item is that neither had to be
discoverable by an adversarial reviewer to be prevented.

### Category

Partial implementation of an earlier approved corrective action — not a new
requirement, not a technical limitation, not a strategic change. No product
behavior, public contract, package, or user-facing surface is involved.

### Rebase record

This proposal was drafted against `epic-6-authority-2026-07-28-v4` and claimed
story identifier 6.9. Between drafting and publication, a concurrent session
published `sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md`,
appended the v5 overlay amendment (`epics.md:1076-1216`), bumped architecture to
v5, and took identifier 6.9 for
`6-9-tier-the-conformance-oracle-and-make-the-portable-tier-structural`. Story
6.8 also moved from `backlog` to `in-progress` with its record committed.

The proposal was rebased in the same session rather than handed off stale:
identifier 6.9 → **6.10**, authority v5 → **v6**, marker family `-V5` → **-V6**,
append point line 1074 → **1216**, and the architecture readiness line 193 →
**245**. §4.2 AC1 was strengthened as a direct consequence (see the tier
interaction below). Two corrections were made honestly rather than absorbed:

- The v4-era claim that `architecture.md:1548-1550` still states a pre-v4
  sequence **does not reproduce**. Those lines are an architecture-validation
  checklist. The only readiness sequence string in the file is at line 245 and it
  is current for v5.
- `epic-6-context.md:8` `source_overlay_begin` still reads
  `'EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN'`, the **v3** marker family, after
  two subsequent regenerations. Confirmed stale. The v6 regeneration fixes it.

**Concurrency hazard, carried forward as an instruction.** Three correct-course
proposals landed on 2026-07-28 and identifier/version collisions are now the
normal case, not the exception. The v6 amendment must be published atomically
immediately before `create-story`, against whatever authority is live at that
moment, and its identifier re-confirmed against `sprint-status.yaml` at that
moment. This is also the fourth consecutive hand-extension of the same chain
assertion in roughly a day, which is the strongest available argument for the
§4.1.4 sub-change.

## 2. Impact Analysis

### Epic impact

Epic 6 remains viable and keeps every existing gate. One story is added; none is
removed, redefined, or resequenced away. Epics 1-5 are immutable history and are
untouched.

The non-obvious cost is the authority overlay, which is append-only and pinned
mechanically by
`tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs`
(now 1,510 lines). Adding Story 6.10 costs what adding 6.8 and 6.9 cost, plus one
more constant split:

- The test asserts the **chain** — the v2 overlay closes, the v3 amendment opens
  and closes, the v4 amendment opens and closes, the v5 amendment opens, and the
  document *ends* with the v5 `:END` marker (lines 520-557). A v6 block therefore
  needs its own marker family, `EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6`.
- The version constants are accreting inconsistently. They currently read
  `BaseOverlayVersion` (v2), `PreviousOverlayVersion` (v3), `V4OverlayVersion`
  (v4), `OverlayVersion` (v5), with `PreviousArchitectureVersion` (v3) and
  `V4ArchitectureVersion` (v4). Two naming schemes coexist and "previous" no
  longer means previous. A sixth version compounds it.

### Story impact

| Story | Impact |
| --- | --- |
| 6.1 | None. Completed historical authority; the v6 amendment appends, it does not rewrite. |
| 6.2 | None. **Its currently-open evidence state is not reopened, corrected, or absorbed by this proposal.** |
| 6.3 | No change beyond the v5 amendment. Its evidence validation is written against the shared helper. |
| 6.4 | No semantic change; its evidence validation is written against the shared helper. |
| 6.5 | No semantic change. Its SM-2 reproduction reads `Contracts.Tests` evidence, which is inside the gate's reach only because §4.2 puts the helper in a shared project. |
| 6.6 | No change beyond the v5 amendment. Its superseding attestation — the largest evidence artifact in the epic — is validated through the shared helper. |
| 6.7 | None. Its gate shape is the template this story's gate mirrors. |
| 6.8 | None. Record generation and evidence-boundary validation are independent gates over different facts. Its T9 gate-span coupling is inherited (see below). |
| 6.9 | None. Tier membership and evidence-boundary validation are orthogonal; AC1 is constrained so the portable tier's structural property is unaffected. |
| **6.10 (new)** | Consolidate the evidence-boundary validation pattern into one enforced helper. |

Proposed binding sequence, expressed as the v5 amendment expresses its own:

`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`, with `6.9 -> 6.3`,
`6.9 -> 6.6`, `6.9 -> 6.10`, `6.8 -> 6.10`, and `6.10` preceding the completion of
`6.3`, `6.5`, and `6.6`.

Story 6.10 follows 6.8 because both edit the same workflow bodies, and follows
6.9 because the tier decision must be settled before a new shared test project
enters the oracle. It precedes 6.3/6.5/6.6 because those are the remaining
evidence-bearing stories, and A5's done-when is about them.

### Tier interaction with Story 6.9

Story 6.9 declares a **portable tier** that binds `Contracts`, `Client`, and
`Testing` only and "references no non-packable module assembly", asserted by a
test over the resolved compile surface. A new non-packable `TestSupport` project
referenced by `Conformance.Tests` sits directly against that property.

Resolved by construction rather than by exemption: the evidence-boundary helper
needs **no Conversations assembly at all**. `System.Diagnostics.Process`,
`System.Security.Cryptography`, `System.Text.Json`, and `System.IO` are the whole
dependency set. AC1 therefore forbids any `src/` project reference from
`TestSupport` and proves it, so the project is tier-neutral under either reading
of 6.9's structural assertion and adds no forbidden edge to either tier.

### Inherited coupling from Story 6.8

Story 6.8's preparation recorded task T9: inserting a section between the
promotion-gate heading and its follower marker widens Story 6.7's gate span in
`WORKFLOW_GATE_CONTRACTS`
(`_bmad/scripts/tests/test_verify_submodule_promotion.py:922`), which keeps the
positive test green while weakening its displacement guard. §4.3 inserts the
evidence gate at the same position and therefore hits the identical coupling. It
must be repaired in the same change, not rediscovered.

### Artifact conflicts

| Artifact | Impact |
| --- | --- |
| PRD / addendum | **None.** FR-1…FR-20, 104 Feature-FRs, 77 Feature-NFRs unchanged. Test-workflow mechanics, not product scope. |
| UX | **None.** No screen, flow, interaction, accessibility, or FrontComposer behavior. |
| Architecture | Version bump, one new amendment section, and binding-order text. No ownership, runtime, projection, or topology decision changes. |
| Epics overlay | Append-only v6 amendment adding Story 6.10 and the amended order. |
| Epic 6 context | Regenerated at v6 with the new story, sequence, an Evidence Boundary Invariant, and the stale `source_overlay_begin` corrected. |
| Conformance tests | `ArchitecturePlanningAuthorityValidationTest` constants/chain/rows/order updated; one new adoption-guard test; existing evidence tests migrated. |
| Solution | One new non-packable test project and its `.slnx` entry. |
| Dev/review workflows | Five bodies plus two render twins gain an evidence-boundary gate; one new code-review layer. |
| `sprint-status.yaml` | New story entry; action A5 flips to `done` only when 6.10 is `done`. |
| `tests/README.md` | New Evidence Boundary Validation section. |
| Runbooks | New `docs/runbooks/evidence-boundary-validation.md`, mirroring the promotion-gate runbook. |
| `deferred-work.md` | One shrunken residue entry; the CI-wiring entry amended from three gates to five. |

### Technical impact

No production source, contract, projection, topology, package, or signed evidence
changes. The change is confined to `tests/`, `_bmad/scripts/`, `.claude/skills/`,
`_bmad/render/`, planning artifacts, and documentation.

**The helper cannot ship.** `src/Hexalith.Conversations.Testing` is
`IsPackable=true` (its csproj sets it explicitly), so repository-introspection and
`git` subprocess helpers must not go there — they have no adopter value and would
put process execution inside a published package. Test projects inherit
`IsPackable=false` from `tests/Directory.Build.props:4`, which is where the new
shared project belongs.

**Adding a project is not free and must be verified, not assumed.** A new
`tests/Hexalith.Conversations.TestSupport/` project touches the solution file and
sits within reach of the scaffold and project-inventory guards
(`ContractPackageInventoryTest`, `ScaffoldSmokeTest`,
`ConversationsAppHostTopologyTest`) and of Story 6.9's tier-membership test.
Story 6.10 must run those guards and record the result rather than assume a
test-only project is invisible to them.

**`_bmad/render/` is no longer in the state Story 6.7 recorded.** Commit
`5ed5e20` modified five of those files. The two render twins in §4.3 must be
edited against their current content, and any divergence from their `.claude`
counterparts recorded rather than silently normalized.

**Honest limitation, stated rather than absorbed.** Nothing executes these gates
automatically. There is no `.github/workflows` outside `references/`, no git hook
invokes the checkers, and the pytest suite is manual-only and requires `uv`.
`deferred-work.md` already records this for the planning gate (Story 6.1) and the
promotion gate (Story 6.7); the July 28 proposal added the final-record gate.
Story 6.10 inherits the same limitation and must not claim otherwise. What it can
do is make the invocation non-deletable in the same way Story 6.7 did — a
conformance test asserting every gated workflow body still carries the call.
Wiring the gates into CI remains a single open deferred item.

**Suite state at the time of writing.** The last recorded full Release conformance
run was **418 total, 416 passed, 2 failed, 0 skipped**, both failures in
`ProjectionReadStorePopulationProofValidationTest` and belonging to Story 6.2's
in-progress evidence state. Stories 6.8 and 6.9 have since changed the suite, so
Story 6.10 must re-measure at its own baseline rather than inherit that figure.
It migrates the failing test onto the shared helper without resolving those two
failures — they are Story 6.2's to close.

## 3. Recommended Approach

**Direct Adjustment.** Add one story to the active corrective epic and publish a
v6 append amendment. No rollback, no MVP reduction.

Rollback is not warranted: nothing needs to be undone, and the Story 5.3 test is
the asset being generalized rather than waste. MVP review is not warranted: the
initiative's product scope is untouched, and the epic's own gates already depend
on trustworthy evidence — Story 6.6 cannot issue a superseding attestation
validated by a test that can pass having asserted nothing.

Three artifacts, no rule implemented twice:

- a **C# helper** in a shared non-packable test project — the reusable pattern;
- a **Python gate** alongside `verify_submodule_promotion.py` — what makes
  adoption non-optional *before review*, sharing one runtime, one JSON contract
  shape, one stable-code vocabulary, and one pytest suite with the promotion
  checker;
- a **conformance test** — non-deletability of the invocation plus the repo-wide
  invariant.

A documentation-only promotion was considered and rejected on this repository's
own evidence. The July 28 proposal documented the outcome precisely: the Epic 5
`tests/Test-StoryFinalRecord.ps1` asset is real, well built, and referenced by
zero workflows, so Stories 6.1, 6.2, and 6.7 all completed without it. Guidance
that nothing invokes is inert.

- **Effort:** Moderate. Low for the authority chain (mechanical, precedent
  exists); moderate for the helper, the gate, the migration, and the workflow
  edits.
- **Risk:** Low-to-moderate. Additive and test-only, but it migrates tests that
  guard signed evidence, rewrites an append-only guard (§4.1.4), and repairs an
  inherited gate-span coupling. All three are fault-injected before the story
  closes.
- **Timeline:** Sits after 6.8 and 6.9, before the 6.3/6.5/6.6 group. It does not
  extend the epic's critical path, because those stories produce evidence
  artifacts that need this validation anyway.

## 4. Detailed Change Proposals

### 4.1 Authority chain

#### 4.1.1 `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

Append after the v5 block's `:END` marker at line 1216 (document is currently
1,216 lines / 88,922 bytes). Do not modify the v1, v2, v3, v4, or v5 blocks: the
frozen 55,536-byte prefix (`bd437b80…f0e8a8`) and the 14,843-byte v2 overlay
(`8825a7a2…63baa`) must recompute byte-identical after the append.

```
NEW (appended):

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:BEGIN version=epic-6-authority-2026-07-28-v6 supersedes=epic-6-authority-2026-07-28-v5 -->

## Appendix: 2026-07-28 Evidence-Boundary Consolidation Amendment

**Overlay version:** `epic-6-authority-2026-07-28-v6`
**Architecture authority:** `conversations-architecture-2026-07-28-v6`
**Supersedes:** `epic-6-authority-2026-07-28-v5` only by adding Story 6.10 and
amending the binding dependency order
**Status:** active corrective amendment; the v1, v2, v3, v4, and v5 overlays,
completed history, and signed evidence remain immutable historical records.

### Added Story

**6.10 Consolidate the evidence-boundary validation pattern into one enforced helper.**
[acceptance criteria as in §4.2]

### Superseding Story Dispositions

| Story | v6 disposition |
| --- | --- |
| 6.1 | No change. |
| 6.2 | No change. Its open evidence state is not reopened by this amendment. |
| 6.3 | No change beyond v5; its evidence validation uses the shared helper. |
| 6.4 | No semantic change; its evidence validation uses the shared helper. |
| 6.5 | No semantic change; its evidence validation uses the shared helper. |
| 6.6 | No change beyond v5; its superseding attestation is validated through the shared helper. |
| 6.7 | No change. |
| 6.8 | No change; record generation and evidence-boundary validation are independent gates. |
| 6.9 | No change; tier membership and evidence-boundary validation are orthogonal. |
| 6.10 | New. |

### Binding Dependency Order

`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`, with `6.9 -> 6.3`,
`6.9 -> 6.6`, `6.9 -> 6.10`, `6.8 -> 6.10`, and Story 6.10 preceding the
completion of Stories 6.3, 6.5, and 6.6.

Story 6.10 follows Story 6.8 because both amend the same completion workflow
bodies, and follows Story 6.9 because tier membership must be settled before a
shared test project enters the oracle. No story completing after Story 6.10 may
reach `done` with an evidence-validation test that re-implements the
evidence-boundary pattern instead of using the shared helper. The SM-C2 baseline
remains a pre-change gate for 6.2, and Story 6.6 remains last. This amendment
introduces one new story identifier and one sprint-status entry.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:END version=epic-6-authority-2026-07-28-v6 -->
```

**Rationale:** The overlay is append-only by construction. Adding the story any
other way would either rewrite frozen authority or leave the story ungoverned.
The new marker family is required because the conformance test asserts the
document *ends* with the active block's `:END`.

#### 4.1.2 `_bmad-output/planning-artifacts/architecture.md`

```
Line 7
OLD: authorityVersion: 'conversations-architecture-2026-07-28-v5'
NEW: authorityVersion: 'conversations-architecture-2026-07-28-v6'

Line 8 (supersededAuthorityVersions) — prepend:
NEW:   - 'conversations-architecture-2026-07-28-v5'

correctionAuthority — append:
NEW:   - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern.md'

New subsection after the Conformance Oracle Tiering amendment section:
NEW: ### 2026-07-28 Evidence-Boundary Consolidation Amendment
     Architecture version `conversations-architecture-2026-07-28-v6` supersedes
     v5 only by adding the evidence-boundary obligation and amending the binding
     story order to include Story 6.10. Evidence artifacts under
     `docs/release-evidence/` are validated through one shared helper: declared
     hashes are recomputed rather than read back, declared paths must resolve
     inside the repository, the changed-file boundary is set equality and
     excludes mode-`160000` gitlink entries, and an unresolvable-history run
     skips visibly instead of passing silently. Every ownership, runtime,
     projection, topology, promotion, performance, oracle-tiering, evidence, and
     readiness decision in v5 remains in force unchanged.

Line 245
OLD: **Overall Status: READY FOR CORRECTIVE IMPLEMENTATION ONLY.** Story order is
     `6.1 -> 6.7 -> 6.2 -> 6.8`; … and Story 6.6 is last. …
NEW: … the same sentence, extended so Story 6.10 follows 6.8 and 6.9 and
     precedes the completion of 6.3, 6.5, and 6.6, and adding: No evidence
     artifact reaches `done` on a validation test that re-implements the
     evidence-boundary pattern.
```

**Rationale:** The conformance test asserts semantic alignment between overlay
version and architecture version; drift between them is itself a conformance
failure per `epic-6-context.md:14`.

#### 4.1.3 `_bmad-output/implementation-artifacts/epic-6-context.md`

Regenerate at v6: frontmatter `overlay_version` / `architecture_version` /
`supersedes_overlay_version`; add a `### 6.10` entry; extend the Binding Sequence
bullets (currently lines 153-162) with Story 6.10's position; add an **Evidence
Boundary Invariant** section stating that declared hashes are recomputed and not
trusted, that the changed-file boundary is set equality excluding mode-`160000`,
that a skipped verification is never a pass, and that root-of-trust constants live
in test source rather than in the evidence under validation.

**Also fix, in the same regeneration:** line 8 `source_overlay_begin` still reads
`'EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN'` — the v3 marker family — after two
subsequent regenerations. It must name the active marker family.

#### 4.1.4 `tests/…/ArchitecturePlanningAuthorityValidationTest.cs`

```
Lines 28-42 — replace two coexisting naming schemes with version-keyed constants:
OLD: BaseOverlayVersion (v2) / PreviousOverlayVersion (v3) / V4OverlayVersion (v4)
     / OverlayVersion (v5); PreviousArchitectureVersion (v3) / V4ArchitectureVersion (v4)
     / ArchitectureVersion (v5)
NEW: V2OverlayVersion, V3OverlayVersion, V4OverlayVersion, V5OverlayVersion,
     OverlayVersion (= v6, active); V3ArchitectureVersion, V4ArchitectureVersion,
     V5ArchitectureVersion, ArchitectureVersion (= v6, active)

Lines 520-557  replaced by the declared amendment chain table (see below)
Lines ~581/613 active disposition table extracted from the V6 block, rows 6.1-6.10;
               the v5 table's 6.1-6.9 row set becomes immutable and is asserted
               from the v5 block
Lines ~620-624 new "### Story 6.10:" content assertions:
               "recomputed, never trusted", "set equality", "mode-160000",
               "skip is never a pass", "root of trust stays in test source";
               ShouldNotContain "declared hashes may be trusted"
Lines ~703-748 the v5 order becomes previous; the v6 order is asserted, including
               `6.9 -> 6.10` and `6.8 -> 6.10`;
               negatives: "Story 6.10 is optional", "6.10 may complete before 6.9"
Line 793   for (int story = 1; story <= 9; story++)  ->  story <= 10
Context semantics — add Story 6.10's sequence position and the Evidence Boundary
               Invariant strings.
```

**Chain-table sub-change (approved in Round 2, in scope).** The chain assertion has
now been hand-extended three times in roughly a day — v4, v5, and this proposal's
v6 — and each extension has been under-estimated or has forced a rebase. Replace
lines 520-557 with a declared table of `(markerFamily, version, supersedes)`
walked in order: each block must open only after the previous one closes, each
marker family must occur exactly twice, and the last entry must end the document.
A future v7 then costs one row.

Because this rewrites the assertion that *is* the append-only guard, it must be
fault-injected in the same story: splice a block inside an earlier one, duplicate
a marker family, and drop the final `:END` — each proving red before the story
closes.

**Rationale:** These constants and assertions are the mechanism that makes
planning authority non-advisory. Leaving them stale would turn a green suite into
proof of the wrong thing.

### 4.2 New Story 6.10

**Key:** `6-10-consolidate-the-evidence-boundary-validation-pattern`
**Title:** Consolidate the evidence-boundary validation pattern into one enforced helper

**AC1 — One helper, in a project that cannot ship and cannot change a tier.**
A new non-packable `tests/Hexalith.Conversations.TestSupport/` project provides
`RepositoryLocator`, `GitFacts`, `EvidenceManifest`, `BoundaryAssertions`, and
`AssertionLedger`, referenced by `Conformance.Tests`, `Contracts.Tests`, and
`IntegrationTests`. It inherits `IsPackable=false` from
`tests/Directory.Build.props`. It is never added to
`src/Hexalith.Conversations.Testing`, which is `IsPackable=true`.

`TestSupport` **references no Conversations assembly at all** — its dependency set
is the BCL plus the test framework — and a test asserts that over its resolved
compile surface. This keeps it neutral with respect to Story 6.9's portable and
module-internal tiers by construction rather than by exemption. The story
verifies, and records, that adding the project trips neither
`ContractPackageInventoryTest`, `ScaffoldSmokeTest`, the AppHost topology guards,
nor Story 6.9's tier-membership test.

**AC2 — One hardened git runner.**
Bounded timeout; stderr drained concurrently with stdout; explicit UTF-8 stream
decoding; `-c core.quotepath=false` so non-ASCII paths compare correctly; a
missing `git` reported as *unavailable* rather than an escaping exception.
Exposes `TryResolveRevision`, `ChangedFiles(range)`, `RawDiffEntries(range)`, and
`TryReadBlobSha256(revision, path)`.

**AC3 — Manifest integrity recomputed, never trusted.**
For every declared source artifact: the path is repository-relative and not
rooted; it resolves inside the repository root; the file exists; the hash is 64
lowercase hex characters; the **recomputed** file hash equals the declared hash;
and the path never names `bin/`, `obj/`, or `/generated/`. The signable payload
hash is recomputed from the `(path, sha256, role)` manifest and compared. Drift is
permitted only through a narrow reviewed supersession allowlist that can never
contain a path under `docs/release-evidence/`, and a positive counter asserts that
every allowlisted entry was actually verified against the signed source commit —
a run that verified nothing must not look like a run that verified everything.

**AC4 — The changed-file boundary is exact and gitlink-free.**
Set *equality* against the recorded expected file list, never containment.
Mode-`160000` entries are rejected by parsing the `git diff --raw` mode columns,
never by substring-matching the raw diff, because a blob hash or a file name can
legitimately contain `160000`.

**AC5 — Skip is never a pass.**
When history is unresolvable — shallow clone, partial clone, non-repository
checkout, absent `git` — the check calls `Assert.Skip` with the reason and the
`AssertionLedger` records zero executed assertions. Any check that reports success
having executed none fails. Root-of-trust constants (signed artifact hashes,
signed source commits) stay pinned in the *consuming test's* source, never in the
helper and never read from the evidence under validation, so a coordinated edit of
an artifact and the record declaring its hash cannot satisfy the suite.

**AC6 — Adoption is gated before review, not recommended after it.**
`_bmad/scripts/verify_evidence_boundary.py` compares the baseline-to-candidate
diff against the repository and blocks when an evidence artifact changed without a
validation test that uses the helper, or when a validation test defines its own
git runner, repository-root walk, or ad-hoc file hash. A conformance test asserts
the repo-wide invariant and that all five gated workflow bodies still carry the
invocation — the Story 6.7 five-gate-body check that caught a gate body being
replaced with "the gate is optional".

Stable blocker codes: `EVIDENCE_HELPER_NOT_USED`, `ADHOC_GIT_RUNNER`,
`ADHOC_REPOSITORY_ROOT`, `ADHOC_HASH_HELPER`, `EVIDENCE_ARTIFACT_UNVALIDATED`,
`EXEMPTION_EXPIRED`, `SCOPE_NOT_EVALUATED`, `BASELINE_NOT_PROVIDED`.
Warning codes: `EXEMPTION_ACTIVE`, `EVIDENCE_TEST_OUTSIDE_CONFORMANCE`.

**AC7 — Anti-vacuity.**
When no evidence artifact and no validation test changed, the result is
`not-applicable` and is reported as that — never as `pass`. When evidence paths
did change but nothing was evaluated, `SCOPE_NOT_EVALUATED` warns and fails the
gate wherever version control is available. This is the exact hole Story 6.7
pass 2 found in its own gate, where a run with nothing declared and no usable
baseline returned exit 0 having evaluated nothing.

**AC8 — Migration preserves behavior and closes two real defects.**
`SuccessMetricReportAndAttestation…`, `ProjectionReadStorePopulationProof…`,
`SmC2BaselineReconstruction…`, `OqTwoTargetInterpretationDecision…`, and
`AtRiskTestRegisterGeneration…` migrate onto the helper, as do the six
`Contracts.Tests` evidence readers and the one in `IntegrationTests` — zero
day-one exemptions. Pinned constants and assertion semantics are preserved; test
counts before and after are recorded against a baseline measured at the story's
own start, not inherited from an earlier record. Two defects close: the sequential
stdout/stderr `ReadToEnd` deadlock shape, and the throw-on-missing-git path that
turns unavailable history into an error rather than a visible skip. The open
`ProjectionReadStorePopulationProofValidationTest` failures belong to Story 6.2
and are carried unchanged, not resolved here.

**AC9 — Guidance, and every guard proven able to fail.**
`docs/runbooks/evidence-boundary-validation.md` records the invariants, a
copy-per-story checklist, and an honest `### Known limitations` section mirroring
the promotion-gate runbook. One fault injection per guard, each restoring the
mutated artifact byte-identically: alter a declared hash; point a declared path
outside the repository; cite `obj/` as evidence; add a submodule gitlink to the
boundary; make the expected file list a subset; widen the allowlist to signed
evidence; make git unresolvable and verify **skip**, not pass; delete the helper
call from a workflow body. Plus the three chain-table injections from §4.1.4.

**AC10 — Repair the inherited gate-span coupling.**
Inserting the evidence gate between the promotion-gate heading and its follower
marker widens Story 6.7's gate span in `WORKFLOW_GATE_CONTRACTS`
(`_bmad/scripts/tests/test_verify_submodule_promotion.py:922`), keeping the
positive test green while weakening its displacement guard. Story 6.8 recorded
this as T9 for the same insertion point. Story 6.10 repairs it in the same change
and proves the displacement guard can still fail.

**Explicit non-goals:** does not wire any gate into CI (open deferred item); does
not modify production source, contracts, package versions, AppHost topology,
generated output, or signed evidence; does not reopen Story 6.2's evidence state;
does not re-decide oracle tier membership; does not migrate repository-root copies
in tests that read no evidence.

### 4.3 Workflow surfaces

The promotion gate lives in exactly five bodies. The evidence gate goes
immediately after it in each, so the two share one failure discipline.

| Body | Insertion point |
| --- | --- |
| `.claude/skills/bmad-dev-story/SKILL.md` | after the promotion `<check>` block, before `<action>Update the story Status to: "review"</action>` |
| `.claude/skills/bmad-dev-auto/step-04-review.md` | after the promotion paragraph |
| `.claude/skills/bmad-quick-dev/step-05-present.md` | after `### Promotion Completion Gate` |
| `.claude/skills/bmad-quick-dev/step-oneshot.md` | after the promotion invocation |
| `.claude/skills/bmad-code-review/steps/step-04-present.md` | after the promotion invocation |
| `_bmad/render/bmad-quick-dev/step-05-present.md` | identical to its `.claude` twin |
| `_bmad/render/bmad-quick-dev/step-oneshot.md` | identical to its `.claude` twin |

Line numbers are deliberately omitted: Stories 6.8 and 6.9 are editing these same
files concurrently, and commit `5ed5e20` already moved five of the render copies.
Anchor on the marker text, not on line positions.

`bmad-dev-auto/step-04-review.md` **is** in scope here. Story 6.8 excluded it
under decision D5 because its frozen v4 text named four surfaces and disclosed the
omission as a known bypass to `done`. The v6 amendment freezes five, closing that
bypass for this gate.

```
### Evidence Boundary Gate

<action>Run `python3 {project-root}/_bmad/scripts/verify_evidence_boundary.py
  --repository {project-root} --baseline {{baseline_commit}} --candidate HEAD
  --format json`. Parse the JSON result.</action>
<check if="the checker exits nonzero, result is neither pass nor not-applicable,
  or the checker warned SCOPE_NOT_EVALUATED while version control is available">
  <action>Record every stable blocker code in Dev Agent Record → Debug Log References.</action>
  <action>Set story frontmatter status and Status section to `in-progress`; if sprint
    tracking exists, set development_status[{{story_key}}] to `in-progress`.</action>
  <action>HALT: "Evidence boundary gate failed; use the shared helper or record a
    dated, reasoned exemption — never weaken the assertion"</action>
</check>
```

Also amend the `bmad-dev-story` definition-of-done list and
`.claude/skills/bmad-dev-story/checklist.md` under *Testing & Quality Assurance*:

```
NEW: - Evidence artifacts are validated through the shared evidence-boundary helper
     - No ad-hoc git runner, repository-root walk, or file-hash helper was introduced
     - Any exemption is dated, reasoned, and recorded
```

### 4.4 New code-review layer

`_bmad/custom/bmad-code-review.toml` is a **new file**. The shipped
`customize.toml` is headed *"DO NOT EDIT -- overwritten on every update"*, so the
team-override file is the correct home. Arrays of tables keyed by `id` append, so
this adds a fifth layer without touching the four shipped ones.

```toml
[[workflow.review_layers]]
id = "evidence-boundary"
name = "Evidence Boundary Reviewer"
when = 'Only when the diff touches docs/release-evidence/ or any *ValidationTest.cs.'
instruction = """
Launch a subagent with no prior conversation context, with this prompt:

> You are an Evidence Boundary Reviewer. For each evidence artifact and validation
> test in this diff, check: are declared hashes recomputed or merely read back? Is
> the changed-file boundary set equality or containment? Are mode-160000 gitlink
> entries excluded by parsing raw-diff mode columns, or by substring match? Can any
> assertion report success having executed nothing — unresolvable history, empty
> collection, absent git? Is the root of trust pinned in test source, or read from
> the evidence under validation? Does any supersession allowlist reach signed
> evidence? Output findings as a Markdown list: one-line title, the invariant
> broken, and evidence from the diff.
>
> Diff:
> {diff_output}
"""
```

### 4.5 New assets and documentation

| Path | Purpose |
| --- | --- |
| `tests/Hexalith.Conversations.TestSupport/` | The shared non-packable, Conversations-assembly-free helper project. |
| `tests/…/TestSupport/{RepositoryLocator,GitFacts,EvidenceManifest,BoundaryAssertions,AssertionLedger}.cs` | The pattern itself. |
| `tests/…/Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs` | AC6 repo-wide invariant and workflow-body non-deletability guard. |
| `_bmad/scripts/verify_evidence_boundary.py` | The gate. |
| `_bmad/scripts/tests/test_verify_evidence_boundary.py` | pytest suite, including the AC9 fault injections. |
| `docs/runbooks/evidence-boundary-validation.md` | Operator/author runbook, mirroring the promotion-gate runbook. |
| `tests/README.md` §Evidence Boundary Validation | How to author an evidence validation test and run the gate. |

### 4.6 `sprint-status.yaml`

```
OLD:   6-9-tier-the-conformance-oracle-and-make-the-portable-tier-structural: backlog
       epic-6-retrospective: optional
NEW:   6-9-tier-the-conformance-oracle-and-make-the-portable-tier-structural: backlog
       6-10-consolidate-the-evidence-boundary-validation-pattern: backlog
       epic-6-retrospective: optional
```

Epic 5 action item A5 stays `open` and flips to `done` only when Story 6.10 itself
reaches `done` — the same discipline applied to action item A2 during the Story
6.7 review, where a premature flip was reverted.

### 4.7 `deferred-work.md`

New section `## Deferred from: sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern (2026-07-28)`:

- **Repository-root copies remain in tests that read no evidence.** Story 6.10
  migrates every evidence reader (24 files across three projects). A private
  repository-root walk is defined in 31 test files, so the residue sits in
  non-evidence tests such as `ServerBoundaryTest`, `ConversationsAppHostTopologyTest`,
  `ContractsAssemblyBoundaryTest`, and `RepositoryTestContextTest`. They may
  migrate opportunistically but are not gated, because the gate's subject is
  evidence, not repository-root discovery in general.

Amend the existing entry **"Nothing executes the promotion gate automatically"** so
it covers every gate rather than three: planning (6.1), promotion (6.7), final
record (6.8), oracle tiering (6.9), and evidence boundary (6.10). A single future
sweep wires them together.

## 5. Implementation Handoff

### Scope classification

**Moderate.** Binding authority and Epic 6 language change, one story is added,
and one new test project enters the solution, but no product, UX, contract, or
cross-repository baseline change is required.

### Responsibilities

- **Architect / planning owner** — publish the v6 amendment atomically against the
  live authority, bump architecture to v6, regenerate `epic-6-context.md`
  including the stale `source_overlay_begin` fix, and update the planning-authority
  assertions including the chain-table replacement.
- **Product owner** — add the 6-10 sprint-status entry; keep Epic 5 action A5
  `open` until 6.10 is `done`.
- **Conversations developer** — create the shared test project, implement the
  helper and the gate, migrate the 24 evidence readers, add the adoption
  conformance test and the runbook, edit the five workflow bodies and two render
  twins, repair the inherited gate-span coupling, and run every fault injection.
- **Test / release owner** — confirm the scaffold, project-inventory, and
  oracle-tier guards still pass with the new project, that migrated test counts are
  unchanged except for the two defect fixes, and that Stories 6.3-6.6 close through
  the gate.

### Sequence

1. Record this proposal and obtain approval.
2. **Re-confirm the live authority version and the next free story identifier**
   immediately before publishing. Three proposals landed on 2026-07-28; another
   may have landed since.
3. Publish the v6 authority amendment and regenerate the Epic 6 context.
4. Update the planning-authority assertions; confirm the suite is green at v6
   against a freshly measured baseline.
5. `create-story` for 6.10, then drive it through `dev-story` after 6.8 and 6.9.
6. Migrate the evidence readers and run the fault injections.
7. Close 6.3, 6.4, 6.5, and 6.6 through the gate.
8. Flip Epic 5 action A5 to `done` when 6.10 is `done`.

### Success criteria

- One implementation of the evidence-boundary pattern exists; no evidence test
  defines its own git runner, repository-root walk, or file-hash helper.
- A declared hash that no longer matches its file goes **red**, not stale.
- Unresolvable history produces a visible skip, never a silent pass, and a check
  that executed no assertions fails.
- A submodule gitlink cannot appear in a recorded evidence boundary.
- The gate cannot report `pass` having evaluated nothing.
- The invocation cannot be silently removed from any of the five workflow bodies.
- Adding the shared project trips no scaffold, project-inventory, or oracle-tier
  guard, proven by running them.
- Story 6.7's gate-span displacement guard still fails when displaced.
- The CI-wiring limitation is stated and left as the existing open deferred item
  rather than claimed as resolved.

## 6. Approval Record

Approved by Jerome on 2026-07-28.

Approval was given against the v5-based draft claiming story identifier 6.9. The
document was rebased to v6 / Story 6.10 in the same session after a concurrent
session took both, as recorded in §1 Rebase record. The rebase changed
identifiers, versions, line anchors, and strengthened §4.2 AC1; it changed no
approved decision, deliverable, or boundary.

### Applied in this session

| Artifact | Change |
| --- | --- |
| `…/sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern.md` | This proposal, rebased to the live authority. |
| `…/sprint-status.yaml` | `6-10-…: backlog` added with correct-course provenance. Epic 5 action A5 stays `open`. |

The v6 authority chain is **not** published in this session. Unlike the July 28
mechanical-final-record proposal, which published its v4 chain immediately, this
one hands off publication as sequence step 3, because two other sessions are
amending the same overlay concurrently and a third simultaneous append would
collide again. Publication must be atomic: `epics.md`, `architecture.md`,
`epic-6-context.md`, and the planning-authority assertions together — any one of
them alone leaves the suite red, since version drift between overlay and context
is itself a conformance failure.
