# Classification Change Procedure (v1)

**Artifact:** `classification-change-procedure-v1.json` (machine-readable; this `.md` is the human procedure)
**Status:** `accepted`
**Accepted date:** 2026-06-03
**Owning story:** 1.5 — *Establish classification dispute-resolution and reclassification escape-hatch*
**Governs:** `consume-promote-keep-inventory-v1.json` (the artifact **FR-2** governs, accepted by Story 1.4 at `baselineCommit: bf3d052`)
**Governing FR:** FR-2
**Single canonical log:** the inventory's `changeLog` array (`consume-promote-keep-inventory-v1.json#changeLog`)

## Role (what this artifact is)

This is the **escape hatch / amendability** half of the decision spine. Story 1.4 **built** the spine — the accepted `consume-promote-keep-inventory-v1.json` that classifies every `Hexalith.Conversations.*` source subtree as **Consume / Promote / Keep** and records the SM-1 plumbing-LOC baseline (**13,289**). This artifact makes the spine **amendable**: it defines the **append-only `changeLog`-entry schema** and the **documented procedure** an Epic 2/3 story follows to (a) challenge a classification call or (b) reclassify a misclassified area — **without ever silently editing the accepted baseline**, and re-asserting the FR-2 invariant after every change.

**This story moves no code.** Zero `src/` changes, zero `tests/` behavior changes. The inventory's real `changeLog` stays `[]` — this pilot challenges and reclassifies nothing real. The deliverable is the *mechanism + procedure + a read-only validator with teeth* so that when a future story genuinely hits a misclassification, the path to record it is defined, machine-validated, and silent-edit-proof.

## The two FR-2 flows, one canonical log

FR-2: *"any Consume/Promote/Keep call **can be challenged** and the resolution recorded with rationale; no area left unclassified or dual-classified at acceptance; **reclassifications after acceptance carry a logged rationale**."* Two distinct flows, both recorded as `changeLog` entries distinguished by `type`:

1. **Challenge** (review-time objection). Outcome is **upheld** (call stands; recorded with reasoning) **or** **reclassified** (becomes flow 2). An *upheld* challenge proves a call is *resolvable and recorded* — not necessarily a change.
2. **Reclassification** (discovery-time correction, typically an Epic 2/3 move story finding the SDK/Commons surface cannot satisfy a `Consume`, so it becomes a `Promote`). Append-only; flips only the `classification` label; never touches `approxLoc`/`paths`; recomputes `plumbingBaselineLoc` if the flip crosses the Keep↔plumbing line.

Keeping **one** canonical log (not prose scattered across story files) is what makes the no-silent-change invariant machine-checkable.

---

## The procedure — 5 steps

### Step 1 — How to challenge a call

Append a `challenge` entry to the inventory's `changeLog` with the required fields:

`entryId`, `type: "challenge"`, `areaId` (a **real** id in the inventory), `date`, `raisedBy`, `rationale` (the objection), `resolution ∈ {upheld, reclassified}`, `resolutionRationale`.

- **Upheld** → the area's `classification` is left **unchanged**; the entry records that the original call was examined and stands. No other field moves.
- **Reclassified** → proceed to Step 2 (the challenge is accompanied by, or upgraded into, a `reclassification` entry carrying `from`/`to`).

### Step 2 — How to reclassify on discovery

When a move story finds a `Consume` the SDK can't satisfy (or any other genuine misclassification), append a `reclassification` entry:

`entryId`, `type: "reclassification"`, `areaId`, `date`, `reclassifiedBy` (`<epic>.<story>`, e.g. `2.6`), `from`, `to` (∈ `{Consume, Promote, Keep}`, `from != to`), `rationale`, and `expectedPlumbingAfter` when the post-change set changes plumbing.

Then, in the inventory:
- set the target row's `classification` to `to`;
- **leave `approxLoc` and `paths` untouched** — a misclassification is a boundary/fate correction, not a re-measurement (LOC is fixed at `baselineCommit`);
- recompute `plumbingBaselineLoc` per Step 5 if the flip crosses Keep↔plumbing.

`from` must equal the area's **accepted** classification (a reclassification is applied off the Story-1.4 baseline). If the area is a `promoteLaterCandidate` (the two OQ-3 areas: `governance-evidence-vocabulary`, `hydration-reference-resolution`) and you are flipping it to `Promote`, the `rationale` **must** record that OQ-3's promote-later boundary was crossed deliberately — the hatch does not silently dissolve an open question.

### Step 3 — The no-silent-edit rule

- The `changeLog` is **append-only**. Never delete or rewrite an existing entry.
- The accepted row's **history is never rewritten** — a reclassification flips the live `classification` *and* leaves a matching log entry that justifies it.
- The `-v1` accepted inventory is **immutable**. A *structural* re-issue (new areas, changed measurement method, re-measured LOC) would be a new `-v2` artifact, not an edit of `-v1`.
- **No-silent-change invariant:** for **every** area whose live `classification` differs from the Story-1.4-accepted value there MUST exist a `reclassification` entry whose `to` equals the live value. No live classification may diverge from the accepted baseline without a matching logged entry.

### Step 4 — The invariant that must re-hold (FR-2)

After applying **any** sequence of `changeLog` reclassification entries, the FR-2 structural invariant must still hold on the **post-change** set:

- every area appears **exactly once**;
- each carries **exactly one** classification ∈ `{Consume, Promote, Keep}`;
- **no area is unclassified**, **no area is dual-classified**, **none dropped or duplicated**;
- per-area LOC still reconciles to `sourceTotalLoc` (a relabel never adds, removes, or re-measures source — `sourceTotalLoc` is invariant).

A reclassification may change *which* of the three labels an area carries, but it can never leave an area with zero or two labels.

### Step 5 — Recompute `plumbingBaselineLoc` on a Keep↔plumbing flip

`plumbingBaselineLoc = Σ(Consume approxLoc) + Σ(Promote approxLoc)`; Keep is excluded.

- A **Consume↔Promote** flip leaves `plumbingBaselineLoc` **unchanged** (both count toward plumbing).
- A **Keep↔plumbing** flip **moves** it: Keep→{Consume,Promote} adds the row's `approxLoc`; {Consume,Promote}→Keep subtracts it.

Record the recomputed value as `expectedPlumbingAfter` on the entry. This is part of the procedure so **SM-1's denominator never drifts un-recorded** — the move is itself the audit trail Story 5.3 reads.

---

## The changeLog-entry schema (required fields per type)

| Type | Required fields | Constraints |
|---|---|---|
| `challenge` | `entryId`, `type`, `areaId`, `date`, `raisedBy`, `rationale`, `resolution`, `resolutionRationale` | `resolution ∈ {upheld, reclassified}` |
| `reclassification` | `entryId`, `type`, `areaId`, `date`, `reclassifiedBy`, `from`, `to`, `rationale` | `from != to`; both ∈ `{Consume, Promote, Keep}`; `reclassifiedBy` = `<epic>.<story>`; `approxLoc`/`paths` unchanged; `expectedPlumbingAfter` recorded when plumbing moves |

The machine-readable definitions, plus worked examples, live in `classification-change-procedure-v1.json` (`changeLogEntrySchema`, `workedExamples[]`). Every entry's `areaId` must reference a **real** area in the inventory (the validator rejects dangling references).

## Copy-pasteable entry templates

**Challenge:**
```jsonc
{
  "entryId": "CL-<areaId>-challenge-1",
  "type": "challenge",
  "areaId": "<real areaId>",
  "date": "<YYYY-MM-DD>",
  "raisedBy": "<who>",
  "rationale": "<why challenged>",
  "resolution": "upheld",            // or "reclassified" (then add a reclassification entry)
  "resolutionRationale": "<reasoning>"
}
```

**Reclassification:**
```jsonc
{
  "entryId": "CL-<areaId>-reclassify-1",
  "type": "reclassification",
  "areaId": "<real areaId>",
  "date": "<YYYY-MM-DD>",
  "reclassifiedBy": "2.6",           // <epic>.<story>
  "from": "Consume",
  "to": "Promote",                   // != from
  "rationale": "<why reality diverged; note OQ-3 boundary if a promoteLaterCandidate->Promote>",
  "expectedPlumbingAfter": 13289     // recomputed Σ(Consume+Promote)
}
```

## Worked examples (illustrative — NOT applied)

The JSON carries three validated worked examples (each `example: true`):

1. **Challenge / upheld** — `governance-evidence-vocabulary` Keep is challenged and **upheld** (OQ-3 stays open; classification unchanged; plumbing stays 13,289). Demonstrates *resolvable and recorded ≠ must change*.
2. **Reclassification / within-plumbing** — `generic-serialization-converters` `Consume → Promote` per the epics' §2.6/§3.6 Commons `TypeMapper`/`NameTypeMapper` forward-reference. A within-plumbing flip → `plumbingBaselineLoc` unchanged at **13,289**.
3. **Reclassification / Keep→plumbing** — `client-surface-dtos` `Keep → Promote`, an illustrative flip across the Keep↔plumbing line → `plumbingBaselineLoc` recomputes to **13,429** (+140). Exercises the validator's recompute-after-fold teeth on a non-trivial case.

These are **illustrative only**. The inventory's real `changeLog` is `[]`; nothing real is challenged or reclassified in this pilot.

## Discoverability

This procedure declares `governsArtifact: "consume-promote-keep-inventory-v1.json"` and `governingFr: "FR-2"`. The inventory's own `versioningConvention` already forward-references "Story 1.5" ("reclassifications APPEND a logged change entry to changeLog (Story 1.5)"), so a reader landing on either artifact finds the other. The accepted inventory is left **byte-for-byte unchanged** by this story — discoverability holds without editing it.

## Validation (teeth)

`tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` reads the committed artifacts and asserts: both committed + `status: accepted` + governance fields set; the schema defines required fields for both entry types; every worked example conforms to its type's schema; every entry targets a **real** `areaId`; **folding each reclassification worked example over a copy of the accepted areas preserves the FR-2 invariant** and the example's `expectedPlumbingAfter` equals the recomputed `Σ(Consume+Promote)`; the inventory's own `changeLog` is an array whose (currently zero) entries conform to the schema; and a scoped content-safety scan (same `ForbiddenFragments` as the inventory validator). The validator only **reads** the artifacts — it never re-derives curated LOC and never mutates anything.
