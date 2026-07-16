---
baseline_commit: 581a93e
depends_on: 1-4-accept-the-canonical-consume-promote-keep-inventory-and-record-baseline-plumbing-loc
---

# Story 1.5: Establish classification dispute-resolution and reclassification escape hatch

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a reviewer,
I want any **Consume / Promote / Keep** call in the accepted inventory to be **challengeable with the resolution recorded**, and any **later reclassification** (when an Epic 2/3 story discovers a misclassification) to **carry a logged rationale** — applied through a single documented procedure that **never silently edits** the accepted baseline and that **preserves the FR-2 invariant** (no area unclassified, no area dual-classified) after every change,
so that the decision spine stays **honest** when reality disagrees mid-refactor, every downstream story has a defined way to feed a correction back, and SM-1's denominator (`plumbingBaselineLoc`) only ever moves through a recorded, auditable change.

> **Initiative context (read first):** This is **Story 1.5 of a behavior-preservation refactor** (Conversations Boilerplate Reduction), the **fifth and final gate-zero story** of Epic 1 and the **second of its two *decision-spine* stories**: Story 1.4 **built** the spine (the accepted `consume-promote-keep-inventory-v1.json` — every area once, single label, evidence + target capability + the recorded `plumbingBaselineLoc` = 13,289 SM-1 baseline); **this story makes the spine amendable** — it delivers the **dispute-resolution + reclassification escape-hatch procedure** over that artifact. Stories 1.1–1.3 built the **oracle** (pinned conformance suite + measured blind spots + decoupled at-risk tests). Like Story 1.4, this is a **documentation / governance-procedure / evidence story — zero `src/` and zero `tests/` production-behavior changes.** It does **not** challenge or reclassify any actual area in this pilot (the inventory is correct as accepted; its `changeLog` stays `[]`); it delivers the **mechanism + the documented procedure + a read-only validator with teeth** so that when an Epic 2/3 story *does* hit a misclassification, the path to record it is defined, machine-validated, and silent-edit-proof. The deliverable is a committed procedure artifact under `docs/release-evidence/` (`.md` + machine-readable `.json` with the changeLog-entry schema and worked examples) plus a structural validator mirroring `ConsumePromoteKeepInventoryValidationTest`.

## Acceptance Criteria

### AC1 — A reviewer can challenge any classification; the resolution (uphold or reclassify) is recorded with rationale — never silently

**Given** an accepted classification in `docs/release-evidence/consume-promote-keep-inventory-v1.json` (the artifact FR-2 governs, accepted by Story 1.4, `baselineCommit: bf3d052`)
**When** a reviewer challenges any area's `Consume / Promote / Keep` call
**Then** the **dispute-resolution mechanism** is defined as an **append-only entry** in the inventory's existing `changeLog` array (the append point Story 1.4 already provisioned), and each challenge entry records, at minimum: a stable `entryId`, `type: "challenge"`, the target `areaId`, the `date`, who raised it (`raisedBy`), the challenge `rationale`, the `resolution ∈ {upheld, reclassified}`, and the resolution `rationale`
**And** an **upheld** challenge leaves the area's `classification` **unchanged** (the entry records that the original call was examined and stands, with reasoning) — proving a challenge is *resolvable and recorded*, not necessarily a change
**And** a **reclassified** challenge is expressed per AC2 (it carries the `from`/`to` classification and re-asserts the invariant) — i.e. a challenge that flips a call IS a reclassification and obeys the same logging + invariant rules
**And** the recording is an **inventory `changeLog` entry** (the canonical decision log for this artifact), satisfying FR-2's "recorded with rationale in the decision log or an inventory note" — there is exactly one canonical place, not prose scattered across story files.

### AC2 — A later story that discovers a misclassification reclassifies through a logged, no-silent-change reclassification entry

**Given** a later Epic 2/3 story discovers a misclassification (the addendum's two named cases: a `Consume` item the EventStore SDK / Commons surface **cannot** satisfy becomes a `Promote` — e.g. the `generic-serialization-converters` row's Commons `TypeMapper` micro-promote per epics §Story 2.6 / §Story 3.6 forward-references "reclassification logged per Story 1.5")
**When** it reclassifies the item
**Then** it appends a `changeLog` entry of `type: "reclassification"` recording: `entryId`, the target `areaId`, `date`, the **reclassifying story** (`reclassifiedBy`, e.g. `"2.6"`), `from` and `to` ∈ `{Consume, Promote, Keep}` (`from` ≠ `to`), and the `rationale` (why reality diverged from the accepted call)
**And** the change is applied to the inventory **by appending the log entry — never by silently rewriting the accepted row's history**: the area's `classification` field is updated to `to` **and** the matching `changeLog` entry justifies it, so for **every** area whose live `classification` differs from what Story 1.4 accepted there exists a `reclassification` entry whose `to` equals the live value (no live classification may diverge from the accepted baseline without a matching logged entry — this is the *no-silent-change* invariant, and it is machine-checkable)
**And** reclassification **only flips the classification label**, never the area's `approxLoc` or `paths` (a misclassification is a *boundary/fate* correction, not a re-measurement; LOC is fixed at `baselineCommit`) — so `sourceTotalLoc` and per-area LOC reconciliation are **invariant** under reclassification, while `plumbingBaselineLoc` = Σ(Consume)+Σ(Promote) is **recomputed** from the post-change classification set (a Consume↔Promote flip leaves it unchanged; a Keep↔plumbing flip moves it, and the move is itself the audit trail SM-1/Story 5.3 reads).

### AC3 — The FR-2 invariant (no area unclassified, no area dual-classified, every area exactly once) is preserved after every change

**Given** the inventory at acceptance and after applying **any** sequence of `changeLog` reclassification entries
**Then** the FR-2 structural invariant still holds: **every area appears exactly once**, each carries **exactly one** classification ∈ `{Consume, Promote, Keep}`, **no area is unclassified**, **no area is dual-classified**, and **no source is double-counted** — i.e. a reclassification may change *which* of the three labels an area carries, but it can never leave an area with zero or two labels, drop an area, or duplicate one
**And** the `promoteLaterCandidate ⇒ classification == "Keep"` rule (the two OQ-3 areas: `governance-evidence-vocabulary`, `hydration-reference-resolution`) is **preserved unless an explicit reclassification entry flips the candidate to `Promote`** — in which case the entry's rationale must record that OQ-3's promote-later boundary was crossed deliberately (the escape hatch does not silently dissolve an open question)
**And** this preservation is **asserted by the validator** (AC5): it loads the accepted areas, folds the `changeLog` reclassification entries over them, and re-checks the full FR-2 invariant on the **post-change** set — not merely on the as-accepted set.

### AC4 — The escape-hatch procedure is documented so Epic 2/3 stories know how to feed reclassifications back into the inventory

**Given** the need for Epic 2/3 move stories to feed corrections back (the epics file forward-references this twice: §Story 2.6/3.6 Commons `TypeMapper` / `NameTypeMapper` micro-promote — "reclassification logged per Story 1.5")
**Then** a committed **procedure document** spells out, in steps an implementing story can follow without re-deriving the rules: (1) **how to challenge** a call (append a `challenge` entry; upheld vs reclassified outcomes); (2) **how to reclassify** when a move story finds a Consume the SDK can't satisfy (append a `reclassification` entry with `from`/`to`/`reclassifiedBy`/`rationale`, flip the row's `classification`, leave `approxLoc`/`paths` untouched); (3) **the no-silent-edit rule** (append-only `changeLog`; the accepted row's history is never rewritten; `-v1` stays immutable, a *structural* re-issue would be `-v2`); (4) **the invariant that must re-hold** after the change (AC3); (5) **the changeLog-entry schema** (required fields per entry type) so an appended entry is machine-valid against the AC5 validator
**And** the procedure explicitly names the **single canonical log** (the inventory's `changeLog`) and that recomputing `plumbingBaselineLoc` after a Keep↔plumbing flip is part of the procedure (so SM-1's denominator never drifts un-recorded)
**And** the procedure is **discoverable from the inventory** — either the inventory references the procedure artifact, or (preferred, to keep Story 1.4's accepted JSON **byte-immutable**) the procedure artifact references the inventory by name as the artifact it governs, so a reader landing on either finds the other.

### AC5 — Procedure committed under `docs/release-evidence/`; machine-readable schema + worked examples; read-only validator with teeth; scope-clean

**Given** the assembled escape-hatch mechanism
**Then** it is committed as **`docs/release-evidence/classification-change-procedure-v1.md`** (human-readable procedure, AC4) **+ `docs/release-evidence/classification-change-procedure-v1.json`** (machine-readable: a top-level metadata block — `artifact`, `version`, `status: accepted`, `acceptedDate`, `governsArtifact: "consume-promote-keep-inventory-v1.json"`, `governingFr: "FR-2"`; a `changeLogEntrySchema` defining the **required fields per `type`** (`challenge` vs `reclassification`); and a `workedExamples[]` array carrying **at least one `challenge` (upheld)** and **at least one `reclassification`** example, each marked `example: true` and **illustrative only — NOT applied to the accepted inventory**), following the sibling release-evidence shape (`release-baseline-v1.*`, `at-risk-test-register-v1.*`, `consume-promote-keep-inventory-v1.*`) and the existing `*-fixture.json` precedent for non-binding example payloads
**And** Story 1.4's **accepted `consume-promote-keep-inventory-v1.json` is left byte-for-byte unchanged** (immutability of the `-v1` accepted baseline — its `changeLog` stays `[]`; this pilot challenges/reclassifies nothing real); the worked examples live in the **procedure** artifact, not in the inventory
**And** a structural **validator** `tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` is added, **mirroring `ConsumePromoteKeepInventoryValidationTest`** (repo-root discovery → re-read the committed JSON → assert **structural invariants**, never re-deriving curated LOC) and giving the procedure **teeth**: it asserts (a) both artifacts committed + `status: accepted` + `governsArtifact`/`governingFr` set; (b) the `changeLogEntrySchema` defines required fields for both entry types; (c) every `workedExample` conforms to the schema for its `type` (challenge → `resolution ∈ {upheld, reclassified}`; reclassification → `from`/`to` ∈ valid classifications, `from ≠ to`, `reclassifiedBy` well-formed `<epic>.<story>`); (d) every worked-example/`changeLog` entry targets a **real `areaId`** that exists in the accepted inventory (no dangling reclassification); (e) **teeth** — folding the worked-example reclassification(s) over a copy of the accepted areas **preserves the FR-2 invariant** (every area once, single label, no unclassified/dual, LOC reconciliation unchanged) and the example's stated `expectedPlumbingAfter` equals the recomputed Σ(Consume+Promote); (f) the inventory's **own** `changeLog` is an array and every **real** entry (currently zero) conforms to the same schema (guards future Epic 2/3 appends); (g) scoped content-safety (names SDK/Commons capabilities by design — forbids only secrets / drive paths / provider IDs, same `ForbiddenFragments` set as the inventory validator)
**And** **only intended files are staged** — `docs/release-evidence/classification-change-procedure-v1.{json,md}`, the validator test under `tests/Hexalith.Conversations.Conformance.Tests/`, this story file, and `sprint-status.yaml`; **zero production source under `src/` changes**; **no `tests/` behavior is altered** (the validator only *reads* the new + accepted artifacts); **the accepted inventory JSON/MD are not modified**; **no sibling submodule** (EventStore, Tenants, Parties, Commons, Folders, Projects, Memories, FrontComposer) is touched; **no submodule is recursed**.

## Tasks / Subtasks

- [x] **Task 1 — Re-confirm the governed baseline + the immutability constraint (AC1, AC5 precondition)**
  - [x] Confirm `src/`/`tests/` working tree clean; capture `git rev-parse --short HEAD` and record it as this story's `baseline_commit` (update frontmatter if it drifted from `581a93e`). — HEAD = `581a93e`, matches frontmatter; `src/`/`tests/` clean.
  - [x] Re-read `docs/release-evidence/consume-promote-keep-inventory-v1.json`: confirm `status: accepted`, `fr2Governed: true`, `changeLog: []`, and capture the full set of real `areaId`s + their accepted `classification` + `approxLoc` (the worked-example reclassification must target a real id; the validator cross-checks against this set). — 26 real areaIds captured; `status: accepted`, `fr2Governed: true`, `changeLog: []` confirmed.
  - [x] Confirm the inventory's `versioningConvention` already promises "reclassifications APPEND a logged change entry to changeLog (Story 1.5)" — this story fulfils that promise **without** editing the accepted JSON (the procedure artifact is a sibling; the inventory stays byte-immutable).

- [x] **Task 2 — Define the changeLog-entry schema + worked examples (AC1, AC2, AC3)**
  - [x] Define the `challenge` entry shape: `entryId`, `type:"challenge"`, `areaId`, `date`, `raisedBy`, `rationale` (the challenge), `resolution ∈ {upheld, reclassified}`, `resolutionRationale`. Write one **upheld** worked example against a real area (e.g. challenge `governance-evidence-vocabulary`'s Keep → **upheld**, reasoning: domain evidence vocab stays Keep per OQ-3). — `EX-challenge-governance-upheld`.
  - [x] Define the `reclassification` entry shape: `entryId`, `type:"reclassification"`, `areaId`, `date`, `reclassifiedBy` (`<epic>.<story>`), `from`, `to` (∈ valid classifications, `from ≠ to`), `rationale`, and — when the post-change set changes plumbing — `expectedPlumbingAfter`. Write one **reclassification** worked example aligned to the epics' own forward-reference (e.g. `generic-serialization-converters` `Consume → Promote` per §2.6/§3.6 "Commons `TypeMapper`/`NameTypeMapper` micro-promote — reclassification logged per Story 1.5"; a Consume→Promote flip leaves `plumbingBaselineLoc` unchanged at 13,289 — note that explicitly so the example also documents the "plumbing unchanged on a within-plumbing flip" case). — `EX-reclassify-serialization-consume-to-promote`, `expectedPlumbingAfter: 13289`.
  - [x] Mark **every** worked example `example: true` and add a top-level note that worked examples are **illustrative, not applied** to the accepted inventory (the inventory's real `changeLog` is empty in this pilot).
  - [x] (Optional, to also exercise the Keep↔plumbing teeth) add a second reclassification worked example that *does* move `plumbingBaselineLoc` and set its `expectedPlumbingAfter` to the recomputed Σ(Consume+Promote) so the validator's recompute-after-fold assertion has a non-trivial case. — `EX-reclassify-client-surface-keep-to-promote` (`client-surface-dtos` Keep→Promote), `expectedPlumbingAfter: 13429` (13289 + 140).

- [x] **Task 3 — Write the procedure document (AC4)**
  - [x] Author `docs/release-evidence/classification-change-procedure-v1.md`: the 5 documented steps (challenge → uphold/reclassify; reclassify-on-discovery; no-silent-edit/append-only/`-v1`-immutable; invariant-must-re-hold; entry schema), the single canonical log (inventory `changeLog`), the recompute-`plumbingBaselineLoc`-on-Keep↔plumbing-flip rule, and a copy-pasteable entry template for an Epic 2/3 story to follow.
  - [x] State the discoverability link: the procedure `governsArtifact: consume-promote-keep-inventory-v1.json` / `governingFr: FR-2`, and the inventory stays byte-immutable (no edit needed — the forward-reference already lives in its `versioningConvention`).

- [x] **Task 4 — Mark accepted + write the machine-readable artifact (AC5)**
  - [x] Write `docs/release-evidence/classification-change-procedure-v1.json` (metadata block + `changeLogEntrySchema` + `workedExamples[]`), `status: accepted`, `acceptedDate`, matching the sibling release-evidence shape.

- [x] **Task 5 — Add the validator with teeth; commit scope-clean (AC1–AC5)**
  - [x] Add `tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs`, mirroring `ConsumePromoteKeepInventoryValidationTest` (reuse the repo-root discovery + `ReleaseEvidenceDirectory()` + deterministic JSON-read + `.Clone()`-detached-areas pattern; do **not** invent a new harness). Assert AC5 (a)–(g): artifacts accepted + governance fields; schema defines both entry types; worked examples conform to their type's schema; every entry targets a real `areaId`; **fold-and-recheck FR-2 invariant** + `expectedPlumbingAfter` recompute; inventory `changeLog` array + real-entry conformance; scoped content-safety (same `ForbiddenFragments`). — 9 core AC5(a)–(g) facts + 8 `qa-generate-e2e-tests` gap-coverage facts = **17 facts**; reused all four helper patterns verbatim in spirit.
  - [x] Run the conformance project: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`. Story 1.4 post-review baseline = **331 passed**; expect 331 + the new validator methods, **0 failed / 0 skipped**. — Result: **348 passed, 0 failed, 0 skipped** (331 + 17 = 9 dev facts + 8 QA gap-coverage facts).
  - [x] `git status` / `git diff --stat`: **zero `src/` changes**; no `tests/` behavior altered (validator only *reads*); **`consume-promote-keep-inventory-v1.{json,md}` unchanged** (verify with `git diff --exit-code -- docs/release-evidence/consume-promote-keep-inventory-v1.json`); no sibling submodule touched; no submodule recursed. Only intended files staged. — `git diff --exit-code` on inventory clean; zero `src/` changes; no submodule touched.

- [x] **Task 6 — Update sprint status + finalize**
  - [x] Set this story's status to `review` in `sprint-status.yaml`; preserve all comments + STATUS DEFINITIONS; update `last_updated`.

## Dev Notes

### What this story IS (and is NOT)

- **IS:** the **escape-hatch / amendability** story for the decision spine. Produce the **documented dispute-resolution + reclassification procedure** over the accepted inventory (`classification-change-procedure-v1.{md,json}`, `accepted`), define the **append-only `changeLog`-entry schema** (challenge + reclassification), ship **worked examples** that the validator checks for real (schema-conformance + fold-and-recheck-invariant teeth), and guard the inventory's own (empty) `changeLog` for future Epic 2/3 appends. Output = the procedure artifact pair + a read-only validator mirroring `ConsumePromoteKeepInventoryValidationTest`.
- **IS NOT:** a refactor, a move, a deletion, a re-measurement, or an *actual* reclassification of any area in this pilot. **Zero `src/` production changes; zero `tests/` behavior changes.** The accepted inventory is correct as Story 1.4 left it — its real `changeLog` stays `[]`. This story builds the *mechanism + procedure + validator* so that a future story which genuinely hits a misclassification has a defined, machine-validated, silent-edit-proof way to record it. It does **not** resolve OQ-1 (landing zone), OQ-2 (SM-1 numeric target), or OQ-3 (governance/hydration promote-later boundary) — the upheld worked example *keeps* governance Keep precisely to demonstrate the hatch does not dissolve an open question.
- **Why a sibling artifact instead of editing the inventory:** Story 1.4 declared `-v1` **immutable once accepted** ("reclassifications APPEND a logged change entry to changeLog … never rewrite an accepted row"). The cleanest honoring of that is to leave the accepted JSON **byte-for-byte unchanged** and deliver the procedure + schema + worked examples as a **sibling** artifact that governs how its `changeLog` is appended. The inventory's `versioningConvention` already forward-references "Story 1.5", so discoverability holds without an edit.

### Closing the loop on Story 1.4's forward-references (the downstream contract)

Story 1.4 (Dev Notes "Downstream contract") promised: *"**Story 1.5** adds the dispute-resolution + reclassification escape-hatch **over this artifact** (so it must be a real, addressable, versioned artifact)."* It is — the inventory has stable `areaId`s, an empty append-only `changeLog`, and a recorded `versioningConvention`. This story consumes exactly those affordances:
- The **`changeLog: []`** array → the append point this story gives a schema + procedure for.
- The **stable `areaId`s** → what a `changeLog` entry's `areaId` references (validator asserts no dangling reference).
- The **`versioningConvention`** → the immutability rule this story's procedure formalizes (append, never rewrite; `-v1` immutable; structural re-issue = `-v2`).
- The epics file's **two forward-references** (lines 414, 579 — Commons `TypeMapper`/`NameTypeMapper` micro-promote "reclassification logged per Story 1.5") → the real-world reclassification case the worked example models (`generic-serialization-converters` `Consume → Promote`).

### The two FR-2 cases, made concrete (AC1 vs AC2)

FR-2 (epics L27): *"any Consume/Promote/Keep call **can be challenged** and the resolution recorded with rationale; no area left unclassified or dual-classified at acceptance; **reclassifications after acceptance carry a logged rationale**."* Two distinct flows, one canonical log:
1. **Challenge (AC1)** — a *review-time* objection to a call. Outcome is **upheld** (call stands, recorded) **or** **reclassified** (becomes case 2). The worked **upheld** example proves "resolvable and recorded" ≠ "must change".
2. **Reclassification (AC2)** — a *discovery-time* correction, typically from an Epic 2/3 move story finding the SDK can't satisfy a `Consume` (→ `Promote`). Append-only; flips `classification`; never touches `approxLoc`/`paths`; recomputes `plumbingBaselineLoc` if the flip crosses the Keep↔plumbing line.

Both are `changeLog` entries distinguished by `type`. Keeping **one** canonical log (not prose in story files) is what makes the no-silent-change invariant machine-checkable.

### The "no-silent-change" invariant — what gives the validator teeth (AC2/AC5)

The disaster this story prevents is a future story quietly flipping `query-cursor-orchestration` from Consume to Keep (shrinking SM-1's denominator to flatter the Epic 5 reduction metric) with no logged reason. The teeth:
- **Every live `classification` that differs from the Story-1.4-accepted value MUST have a matching `reclassification` entry whose `to` equals the live value.** (In this pilot, zero areas differ → zero required entries → vacuously satisfied, but the rule is in the validator the moment Epic 2/3 appends.)
- **Fold-and-recheck:** the validator folds the worked-example reclassifications over a *copy* of the accepted areas and re-asserts the **full** FR-2 invariant on the post-fold set (every area once, single label, no unclassified/dual, `Σ(area LOC)` unchanged = `sourceTotalLoc`), plus `expectedPlumbingAfter == Σ(Consume+Promote)` of the post-fold set. This is a real computation on real data, not a vacuous pass — it is why the worked examples must target **real** `areaId`s.

### Where it lives + the shape to mirror (AC5)

- Place both files under `docs/release-evidence/` beside the inventory + the other `-v1` evidence artifacts. Use the **metadata-block + arrays** JSON shape and a `.md` human header, exactly as the inventory does. The `*-fixture.json` files already in that directory (`conformance-manifest-v1-fixture.json`, `release-waiver-v1-fixture.json`, `release-conformance-artifact-v1-fixture.json`) are the **precedent for non-binding example payloads** — the `workedExamples[]` here are the same idea (illustrative, validated, not applied).
- The validator goes in `tests/Hexalith.Conversations.Conformance.Tests/` beside `ConsumePromoteKeepInventoryValidationTest.cs`. **Reuse its helpers verbatim in spirit:** `FindRepositoryRoot()` (walks up to the `.slnx`/`.git`), `ReleaseEvidenceDirectory()`, `LoadCommittedJson()`, `.Clone()`-detached area arrays (Story 1.4 hit an `ObjectDisposedException` returning `JsonElement`s from a disposed `JsonDocument` — clone to avoid it), and the scoped `ForbiddenFragments` content-safety set. Do **not** invent a new discovery/parse harness.

### Suggested `classification-change-procedure-v1.json` shape

```jsonc
{
  "artifact": "classification-change-procedure",
  "version": 1,
  "status": "accepted",
  "acceptedDate": "2026-06-03",
  "governsArtifact": "consume-promote-keep-inventory-v1.json",
  "governingFr": "FR-2",
  "appendOnlyLog": "consume-promote-keep-inventory-v1.json#changeLog",
  "versioningConvention": "The accepted inventory's '-v1' is immutable. Challenges and reclassifications APPEND an entry to its changeLog and flip only the target row's 'classification' (never approxLoc/paths, never history). A structurally new inventory would be '-v2'. This procedure artifact is itself '-v1'/append-via-'-v2'.",
  "changeLogEntrySchema": {
    "challenge": {
      "required": ["entryId", "type", "areaId", "date", "raisedBy", "rationale", "resolution", "resolutionRationale"],
      "resolutionEnum": ["upheld", "reclassified"],
      "note": "An 'upheld' challenge leaves classification unchanged. A 'reclassified' challenge is accompanied by a 'reclassification' entry (or carries from/to itself)."
    },
    "reclassification": {
      "required": ["entryId", "type", "areaId", "date", "reclassifiedBy", "from", "to", "rationale"],
      "classificationEnum": ["Consume", "Promote", "Keep"],
      "rules": ["from != to", "approxLoc and paths are NOT changed", "recompute plumbingBaselineLoc when the flip crosses the Keep<->plumbing line; record expectedPlumbingAfter"]
    }
  },
  "workedExamples": [
    {
      "example": true,
      "entryId": "EX-challenge-governance-upheld",
      "type": "challenge",
      "areaId": "governance-evidence-vocabulary",
      "date": "2026-06-03",
      "raisedBy": "reviewer (worked example)",
      "rationale": "Could the generic check->evidence->verify flow be Promoted now rather than Keep-now?",
      "resolution": "upheld",
      "resolutionRationale": "OQ-3 promote-later boundary is open; governance evidence vocabulary stays Keep-now (promoteLaterCandidate). The hatch records the question; it does not dissolve OQ-3."
    },
    {
      "example": true,
      "entryId": "EX-reclassify-serialization-consume-to-promote",
      "type": "reclassification",
      "areaId": "generic-serialization-converters",
      "date": "2026-06-03",
      "reclassifiedBy": "2.6",
      "from": "Consume",
      "to": "Promote",
      "rationale": "Worked example per epics Story 2.6/3.6: if Commons cannot satisfy the generic converters via the existing public TypeMapper.GetMap() surface, a NameTypeMapper micro-promote is opened additively and the row moves Consume->Promote. Within-plumbing flip: plumbingBaselineLoc is unchanged.",
      "expectedPlumbingAfter": 13289
    }
  ],
  "note": "workedExamples are ILLUSTRATIVE and are NOT applied to the accepted inventory. The inventory's real changeLog is [] in this pilot (nothing real is challenged or reclassified)."
}
```

### Critical guardrails (from project-context.md + the initiative)

- **This is gate-zero, no code moves.** Zero `src/` changes; zero `tests/` behavior changes. The validator only *reads* the procedure + the accepted inventory; it asserts structural/governance invariants, never production behavior.
- **The accepted inventory is immutable.** Do **not** edit `consume-promote-keep-inventory-v1.json` (verify `git diff --exit-code` on it). Its real `changeLog` stays `[]`. The worked examples live in the **procedure** artifact and are flagged `example: true`.
- **Single canonical decision log (FR-2).** One place — the inventory's `changeLog`. The procedure defines its schema; the validator enforces conformance. No challenge/reclassification recorded as loose prose in a story file counts.
- **No-silent-change (the core invariant).** Any live classification ≠ accepted value requires a matching logged `reclassification` entry. The validator folds worked examples over a copy and re-checks FR-2 + recomputes `plumbingBaselineLoc` — real teeth, not a vacuous schema check.
- **Reclassify flips the label only.** Never `approxLoc`/`paths` (LOC is fixed at `baselineCommit`; a misclassification is a fate correction, not a re-measurement). `sourceTotalLoc` + reconciliation are invariant; `plumbingBaselineLoc` recomputes only on Keep↔plumbing flips.
- **Do not pre-decide OQ-1/OQ-2/OQ-3.** The upheld worked example deliberately *keeps* governance Keep to show the hatch records an open question rather than resolving it. SM-1's numeric target stays OQ-2-open; only the *baseline mechanism* is fixed here.
- **Content-safety (scoped, same as the inventory validator).** The artifact **may** name SDK/Commons capabilities (`TypeMapper`, `NameTypeMapper`, EventStore) — that is its purpose. It must contain **no payload secrets, no drive paths, no provider IDs, no Parties personal data**. Reuse the inventory validator's `ForbiddenFragments` set; do **not** over-apply the public-contract forbidden-substrate-term scan.
- **Submodule rule (repo CLAUDE.md / project-context).** Conversations (`src/Hexalith.Conversations.*`, `tests/Hexalith.Conversations.*`) is at the **repository root**; the sibling `Hexalith.*` directories ARE submodules — **read** them only if confirming a capability name, **touch none**, **never recurse**. This story needs no submodule operation.

### How to run / verify (tech stack)

- .NET `10.0`, `net10.0`, SDK pinned `10.0.302`; nullable enabled, implicit usings, warnings-as-errors; Central Package Management via `Directory.Packages.props`. Test stack: **xUnit v3**, **Shouldly**, `Microsoft.NET.Test.Sdk`, `coverlet.collector`.
- Conformance oracle: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` — Story 1.4 post-review baseline **331 passed**; expect 331 + new validator methods, **0 failed / 0 skipped**.
- Immutability check: `git diff --exit-code -- docs/release-evidence/consume-promote-keep-inventory-v1.json docs/release-evidence/consume-promote-keep-inventory-v1.md` must report **no diff**.
- Solution: `Hexalith.Conversations.slnx` at repo root.

### Project Structure Notes

- The procedure belongs in `docs/release-evidence/` (NOT under `_bmad-output/` — that holds BMAD workflow artifacts; release-evidence holds the committed, code-adjacent decision/conformance artifacts the suites + later stories read). Matches the inventory's placement exactly.
- The validator goes in `tests/Hexalith.Conversations.Conformance.Tests/` beside `ConsumePromoteKeepInventoryValidationTest.cs`; reuse that file's repo-root-discovery + deterministic-JSON-read + `.Clone()`-detached-areas pattern.
- **Detected variance to record (not fix):** Story 1.4's senior-dev review flagged a pre-existing parallel test-isolation race — `ReleaseBaselineValidationTest.CommittedSnapshotTypeCountShouldMatch…` can transiently throw `JsonReaderException` ("end of data") when `PublicContractShapeSnapshotGenerationTest` regenerates `public-contract-shape-baseline-v1.json` mid-read. The new procedure validator reads a **static, never-regenerated** artifact, so it is not exposed to that race — but if the conformance run shows a flake on that *pre-existing* test, it is out of scope here (noted, not fixed).

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 1.5] — story statement + the four AC clauses (expanded above into AC1–AC5): challenge → resolution recorded with rationale; later misclassification → reclassify + log, no silent change; no area unclassified/dual at acceptance; escape-hatch procedure documented for Epic 2/3 feedback.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Requirements Inventory FR-2 (L27)] — FR-2 verbatim: challengeable calls, resolution recorded with rationale, no unclassified/dual at acceptance, reclassifications carry logged rationale.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md L414, L579] — the two forward-references: Commons `TypeMapper`/`NameTypeMapper` additive micro-promote "reclassification logged per Story 1.5" — the real-world reclassification case the worked example models.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md L85, L88, L340 (R5)] — PRD acceptance signals (challenge resolvable + recorded; reclassification logged in decision log or inventory note; R5 domain/plumbing boundary-dispute mitigation = FR-2 resolution rule + decision log).
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.json] — the artifact this story's procedure governs: `status: accepted`, `fr2Governed: true`, stable `areaId`s, append-only `changeLog: []`, `versioningConvention` forward-referencing Story 1.5. **Left byte-immutable by this story.**
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.md] — human header recording "Story 1.5's dispute-resolution + reclassification escape-hatch amends the rows here; logged in the JSON `changeLog`, never by silently editing an accepted row."
- [Source: _bmad-output/implementation-artifacts/1-4-accept-the-canonical-consume-promote-keep-inventory-and-record-baseline-plumbing-loc.md] — the decision-spine story this one makes amendable: its Dev-Notes "Downstream contract" promising Story 1.5 over a real/addressable/versioned artifact; its accepted shape + immutability convention; the `ObjectDisposedException`/`.Clone()` gotcha the validator must avoid.
- [Source: tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs] — the validator pattern to mirror: `FindRepositoryRoot()`, `ReleaseEvidenceDirectory()`, `LoadCommittedJson()`, `.Clone()`-detached areas, scoped `ForbiddenFragments`, structural-invariant (not curated-LOC) assertions.
- [Source: docs/release-evidence/*-fixture.json] — the existing non-binding example-payload precedent (`conformance-manifest-v1-fixture.json`, `release-waiver-v1-fixture.json`) the `workedExamples[]` follow.
- [Source: _bmad-output/project-context.md] — net10.0/CPM/warnings-as-errors; submodule rule (Conversations at repo root, siblings are submodules, never recurse); honesty-of-measurement (no silent SM-1 denominator drift); content-safety scope.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (1M context)

### Debug Log References

- `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...` → **348 passed, 0 failed, 0 skipped** (331 Story-1.4 baseline + 17 new procedure-validator facts: 9 dev AC5(a)–(g) + 8 `qa-generate-e2e-tests` gap-coverage). Project built clean under warnings-as-errors.
- `git diff --exit-code -- docs/release-evidence/consume-promote-keep-inventory-v1.{json,md}` → no diff (accepted inventory byte-immutable, AC5).
- `git status --short -- src/` → empty (zero `src/` production changes).
- `git submodule status` → all eight sibling modules remain uninitialized (`-` prefix); none touched, none recursed.

### Completion Notes List

- **AC1–AC2 (schema + canonical log):** Defined the append-only `changeLog`-entry schema in `classification-change-procedure-v1.json` (`changeLogEntrySchema.challenge` + `.reclassification`) with required-field lists, enums, and rules. One canonical log named throughout: the inventory's `changeLog`.
- **AC1 (challenge — upheld):** Worked example `EX-challenge-governance-upheld` challenges `governance-evidence-vocabulary`'s Keep and **upholds** it (OQ-3 stays open, classification unchanged) — proving a challenge is *resolvable and recorded* ≠ *must change*.
- **AC2 (reclassification):** `EX-reclassify-serialization-consume-to-promote` models the epics' §2.6/§3.6 Commons `TypeMapper`/`NameTypeMapper` forward-reference — `generic-serialization-converters` `Consume→Promote`, a within-plumbing flip leaving `plumbingBaselineLoc` at 13,289.
- **AC2/AC3 teeth (Keep↔plumbing):** `EX-reclassify-client-surface-keep-to-promote` flips `client-surface-dtos` `Keep→Promote`, moving `plumbingBaselineLoc` to 13,429 (+140) — a non-trivial recompute-after-fold case for the validator.
- **AC3 (invariant re-holds):** The validator folds each reclassification example over a detached copy of the accepted areas and re-checks the full FR-2 invariant (every area once, single valid label, LOC reconciles to `sourceTotalLoc`) on the post-fold set, plus `expectedPlumbingAfter == Σ(Consume+Promote)`. promoteLaterCandidate→Promote flips are required to record the OQ-3 boundary crossing.
- **AC4 (procedure doc):** `classification-change-procedure-v1.md` documents the 5 steps, the single canonical log, the recompute rule, copy-pasteable templates, and the discoverability link (`governsArtifact`/`governingFr`). The inventory's own `versioningConvention` already forward-references Story 1.5, so discoverability holds without editing the accepted JSON.
- **AC5 (committed + validator + scope-clean):** Both `-v1.{json,md}` committed under `docs/release-evidence/`, `status: accepted`; validator `ClassificationChangeProcedureValidationTest` (17 facts — 9 dev AC5(a)–(g) + 8 QA gap-coverage) mirrors `ConsumePromoteKeepInventoryValidationTest` (repo-root discovery, deterministic JSON read, `.Clone()`-detached areas, scoped `ForbiddenFragments`). Inventory left byte-for-byte unchanged; zero `src/` changes; no `tests/` behavior altered (validator only reads); no submodule touched.
- **No-silent-change invariant:** the validator carries the rule that every live `classification` differing from the accepted value requires a matching `reclassification` entry whose `to` equals the live value — vacuously satisfied now (inventory `changeLog` is `[]`), enforced the moment Epic 2/3 appends.
- **Out of scope (noted, not fixed):** the pre-existing parallel snapshot-regeneration test-isolation race on `ReleaseBaselineValidationTest` / `PublicContractShapeSnapshotGenerationTest`. The new validator reads a static, never-regenerated artifact and is not exposed to it; the run was clean.

### File List

- `docs/release-evidence/classification-change-procedure-v1.json` (new) — machine-readable procedure: metadata + governance block, `changeLogEntrySchema`, three worked examples, entry templates.
- `docs/release-evidence/classification-change-procedure-v1.md` (new) — human-readable 5-step procedure + schema table + templates + worked-example summary.
- `tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` (new) — read-only structural validator (9 facts) with fold-and-recheck teeth.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — story 1.5 `ready-for-dev → in-progress → review`.
- `_bmad-output/implementation-artifacts/1-5-establish-classification-dispute-resolution-and-reclassification-escape-hatch.md` (modified) — this story file: checkboxes, Dev Agent Record, File List, Change Log, Status.

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot (story-automator autonomous review, auto-fix) — 2026-06-03
**Outcome:** Approve (auto-fixed). 0 CRITICAL / 0 HIGH. 1 MEDIUM + 2 LOW found and fixed.

### What was validated
- **AC1 (challenge → recorded resolution):** `changeLogEntrySchema.challenge` defines the 8 required fields + `resolution ∈ {upheld, reclassified}`; the `EX-challenge-governance-upheld` worked example upholds `governance-evidence-vocabulary`'s Keep (classification unchanged) — proving *resolvable and recorded ≠ must change*. ✅
- **AC2 (logged reclassification, no silent change):** `changeLogEntrySchema.reclassification` (8 required fields, `from ≠ to`, `<epic>.<story>` `reclassifiedBy`). Two reclassification worked examples verified against the live inventory — `generic-serialization-converters` Consume→Promote (within-plumbing, `expectedPlumbingAfter` 13289) and `client-surface-dtos` Keep→Promote (Keep↔plumbing, 13289 + 140 = **13429**). Both `from` values match the Story-1.4-accepted classifications. ✅
- **AC3 (FR-2 invariant re-holds):** fold-and-recheck test folds each example over a detached copy of the accepted areas and re-asserts every-area-once / single-valid-label / LOC-reconciles-to-`sourceTotalLoc` (35769) plus the `expectedPlumbingAfter` recompute. Independently reproduced the math in Python — all correct. OQ-3 boundary-crossing rationale check present (latent; no example flips a `promoteLaterCandidate`). ✅
- **AC4 (procedure documented + discoverable):** `.md` documents the 5 steps, the single canonical `changeLog`, the recompute rule, copy-pasteable templates; `governsArtifact`/`governingFr` set; inventory's `versioningConvention` back-references "Story 1.5". ✅
- **AC5 (committed + validator with teeth + scope-clean):** both `-v1.{json,md}` committed `status: accepted`; `git diff --exit-code` on the inventory is clean (byte-immutable); `git status -- src/` empty; no submodule touched. ✅

### Findings & fixes (auto-applied)
1. **[MEDIUM] Stale Dev-Agent-Record counts.** Record claimed "9 facts / 340 passed"; the committed validator carries **17 facts** (9 dev AC5(a)–(g) + 8 `qa-generate-e2e-tests` gap-coverage added after the dev wrote the record) → **348 passed**. Corrected Task 5, Debug Log, Completion Notes, and Change Log to the actual numbers.
2. **[LOW] Content-safety scan covered only the `.json`.** The equally-committed `.md` was unscanned. Extended `CommittedProcedureShouldPassScopedContentSafetyScan` to scan both artifacts (same `ForbiddenFragments`).
3. **[LOW] Schema/validator drift on `reclassifiedBy`.** The schema declares `reclassifiedByPattern` but the validator hardcoded the regex literal. Now reads the schema-declared pattern (falls back to the canonical `<epic>.<story>` shape if absent).

### Verification after fixes
- `dotnet test …Conformance.Tests` → **348 passed / 0 failed / 0 skipped** (clean under warnings-as-errors).
- Inventory `git diff --exit-code` → no diff; zero `src/` changes; no `tests/` *behavior* altered (validator only reads).

## Change Log

| Date | Change |
|---|---|
| 2026-06-03 | Story 1.5 implemented: added `classification-change-procedure-v1.{json,md}` (FR-2 dispute-resolution + reclassification escape-hatch over the accepted inventory) and the read-only `ClassificationChangeProcedureValidationTest` (17 facts — 9 dev AC5(a)–(g) + 8 `qa-generate-e2e-tests` gap-coverage, fold-and-recheck teeth). Conformance suite 348 passed / 0 failed / 0 skipped. Accepted inventory left byte-immutable; zero `src/` changes; no submodule touched. Status → review. |
| 2026-06-03 | Senior Developer Review (AI, story-automator auto-fix): adversarial review, no HIGH/CRITICAL. Fixed (MEDIUM) stale Dev-Agent-Record counts — record said "9 facts / 340 passed", actual committed validator carries 17 facts → 348 passed (the QA gap-coverage pass added 8 facts after the dev wrote the record). Hardened the validator (2 LOW): scoped content-safety scan now covers the `.md` as well as the `.json`; `reclassifiedBy` validation now reads the schema-declared `reclassifiedByPattern` instead of a hardcoded literal (no schema/validator drift). Re-ran suite: **348 passed / 0 failed / 0 skipped**. Inventory still byte-immutable; zero `src/` changes. Status → done. |
