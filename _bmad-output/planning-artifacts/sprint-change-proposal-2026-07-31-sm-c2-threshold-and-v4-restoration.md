# Sprint Change Proposal — SM-C2 threshold amendment and v4 overlay restoration

**Date:** 2026-07-31
**Raised by:** Story 6.2 code-review pass 11 (`dev-story`)
**Approved by:** Jerome (release owner), 2026-07-31
**Applied as:** `epic-6-authority-2026-07-31-v6` / `conversations-architecture-2026-07-31-v6`

Two unrelated problems surfaced against the same authority surface and were decided together.
Both are recorded here so the v6 amendment has a proposal to point at, in the same shape as the
v1–v5 amendments.

---

## Change 1 — Story 6.2's AC1 SM-C2 gate cannot be met while AC6 stands

### Problem

AC1 freezes `sm-c2-hot-path-inventory-v1` and requires every one of its four rows to satisfy
`post P95 <= 1.05 x baseline P95`. Measured at Story 6.2's candidate, with the baseline
reconstructed at the preserved source commit `29def44` under the identical envelope and the
byte-identical fixture:

| Row | Baseline p95 (µs) | Post p95 (µs) | Max +5% | Change | Rule result |
| --- | ---: | ---: | ---: | ---: | --- |
| HP-CREATE | 1.7566 | 0.8759 | 1.8444 | −50.1% | pass |
| HP-APPEND | 18.8690 | 22.6453 | 19.8124 | +20.0% | fail |
| HP-LIST | 746.9148 | 3210.6471 | 784.2605 | +329.9% | fail |
| HP-OPEN | 22.0662 | 410.6236 | 23.1695 | +1760.9% | fail |

Three of four rows fail, and AC1 says an unmet gate blocks completion.

### Why the gate cannot be met

**HP-LIST and HP-OPEN are real and structural.** They are the price of the fail-closed cross-key
validation that Story 6.2's own review passes required in order to satisfy AC6, and that AC6's
never-list makes mandatory ("queries must expose cross-key generation inconsistency rather than
repair it on read"). At the baseline, a detail read was one store read. It is now the detail read
plus a full tenant-index read plus a dispatch-ledger read, and each list page additionally
bulk-reads one detail record and one ledger record per returned row and structurally compares each
summary against its index entry.

No optimisation closes a 5% gate against a single-read baseline. Even the best available redesign —
splitting the tenant index into a per-conversation entry key so validation stops deserializing the
whole index — is three small reads where the baseline did one. The gate compares a fast, incorrect
read path against a slower, correct one and reports the correctness as a regression.

**HP-CREATE and HP-APPEND carry no usable signal at ±5%.** Pass-10 decision D4 established this by
measurement, not argument: HP-APPEND spans 3.797750 → 26.665300 µs *within a single 30-sample run*,
a factor of 7, and nearest-rank p95 selects the transient peak. Across two rounds on byte-identical
code, HP-APPEND flipped `pass` → `fail` while HP-CREATE flipped the other way, and HP-CREATE's
baseline p95 alone moved 0.7646 → 1.7566 µs, a factor of 2.3. A ±5% threshold cannot adjudicate a
statistic whose own dispersion is two orders of magnitude wider.

### Decision — approved 2026-07-31 (Jerome)

Amend AC1 for Story 6.2 (option (a) of four presented) and raise a follow-up performance story.
The frozen inventory, the envelope, the paired-measurement requirement, and every raw sample stay
exactly as they are; only the pass rule changes, and only where it provably cannot decide.

Options considered and not taken: fixing the read path inside Story 6.2 (reopens the projection
store at pass 11 and still misses ±5%); deferring the whole story until a performance story lands
(blocks Epic 6 on work whose success is not assured); shipping with AC1 disclosed as unmet (makes
an acceptance criterion that says "blocks completion" not block completion, which is the pattern
this epic exists to stop).

### Amended rule

From the v6 amendment forward, for Story 6.2:

1. All four frozen rows still get **exactly one** baseline and one post result under **one identical
   envelope**, and every raw sample stays in the artifact. Unchanged from AC1 as published.
2. A row is gated at `post P95 <= 1.05 x baseline P95` only when **both** hold: its cost change is
   not attributable to an approved correctness change, **and** the row carries usable signal at that
   threshold.
3. **HP-LIST and HP-OPEN** fail the first test. For these two rows the ±5% rule is replaced by an
   **approved-cost ceiling**: the measured post p95 plus 10% headroom, recorded numerically in the
   artifact. The approved factor itself does not block; exceeding the ceiling does. A later change
   that makes these paths slower again therefore still goes red.
4. **HP-CREATE and HP-APPEND** fail the second test. They are recorded as measured, disclosed as
   carrying no usable signal at ±5%, and not gated in Story 6.2.
5. The disclosure is mandatory and must be stated in the artifact a reader relies on, not only in a
   test comment: which rows are gated at which rule, why, and — for the ungated rows — that a
   `pass` there may not be cited as evidence of no regression.
6. Story 6.6 re-measures under this same amended rule when it issues the superseding attestation.
   The ceiling values are Story 6.2's measurement, not a permanent budget.

### Follow-up story

**6.11 — Make cross-key projection validation cheap enough to re-gate SM-C2 at ±5%.** Owns the
HP-LIST and HP-OPEN paths: replace the full tenant-index deserialize in the detail path with a
per-conversation index-entry key family, remove the per-row detail+ledger fan-out from page
verification where the ledger's own record already proves the generation, and re-measure both rows.
Its done-when is a measured post p95 back under the ±5% rule, at which point the approved-cost
ceiling is retired. Added to the sprint plan as `backlog`.

---

## Change 2 — the published v4 overlay was rewritten in place

### Problem

Commit `1b7a06b` (2026-07-29 12:02, "fix: update submodule references and enhance test
documentation") rewrote the v4 amendment **in place**, one day after v5 was published declaring
"the v1, v2, v3, and v4 overlays … remain immutable historical records". It changed, with no v6
amendment and no disclosure:

- `epics.md` v4 — the derivation-sources paragraph ("unioned with the tracked working-tree delta" →
  "with source-tree dirt blocked outside record outputs and declared TRX inputs"), invariant 2 (the
  root `.slnx` sentence), invariant 4 (the record-output-paths qualifier), and invariant 5 (the
  bundle-and-digest wording).
- `architecture.md` — the same derivation-sources sentence in the v4 prose.

Story 6.8's "frozen authority, quoted verbatim" acceptance criteria consequently half-matched: AC4
had been synced to the new text, AC2 and AC5 had not, and sprint-status still claimed byte-for-byte
verification.

### Decision — approved 2026-07-31 (Jerome)

Restore the original v4 bytes and republish the edits as part of this v6 amendment, then re-sync
Story 6.8's quotes (option (a) of three presented). The rejected alternatives were accepting the
edited text with a documented exception — which would establish that a published immutable overlay
can be edited in place provided the edit is documented afterwards — and reverting the edits outright,
which discards four genuine improvements.

### Applied

The `1b7a06b` planning hunks are reverted, so v4 reads exactly as published. All four improvements
are republished in the v6 amendment and take effect from v6 forward. Story 6.8 is `in-progress`, so
its quotes are re-synced against v6 rather than being left to half-match.
