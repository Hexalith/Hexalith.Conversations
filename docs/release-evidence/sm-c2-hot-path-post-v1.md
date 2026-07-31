# SM-C2 Hot-Path Post v1

**Result: pass under the amended rule — 2 rows gated and passing, 2 rows recorded and not gated, 0 failing.**

- Baseline: `docs/release-evidence/sm-c2-hot-path-baseline-v1.json`
- Source commit: `856ee997cd35eb1d432fcb288a75a7b5bf3c5b58` with EventStore gitlink `e645901928eed9759e28e1086f23dc96875c3ac3`
- Fixture SHA-256: `4838a5a174b247e3aa4cc6be0c181049a46d884612058df248af8892adaf3fff` (byte-identical to baseline)
- Project overlay SHA-256: `e88f942c2b8eb45d5a168bc832d77698a9073baac88033023fe1b6181ea68d1e` (byte-identical to baseline)
- Envelope: Release, .NET 10.0.10 / SDK 10.0.302, xUnit v3 3.2.2, one worker, five warmups, 30 repetitions, 2,000 operations per sample
- Published rule: `post P95 <= 1.05 * baseline P95`
- Applicable rule: `epic-6-authority-2026-07-31-v6` (proposal `sprint-change-proposal-2026-07-31-sm-c2-threshold-and-v4-restoration.md`, approved by the release owner 2026-07-31)

## Amended pass rule

The frozen inventory, the identical envelope, the paired measurement, and every raw sample are
unchanged. A row is gated at `post P95 <= 1.05 * baseline P95` only when its cost change is **not**
attributable to an approved correctness change **and** the row carries usable signal at that
threshold.

| Hot path | Baseline P95 | Post P95 | Published rule | Applicable gate | Gate value | Result |
| --- | ---: | ---: | --- | --- | ---: | --- |
| HP-CREATE | 1.756600 | 0.875900 | pass (−50.14%) | recorded, not gated | — | **recorded** |
| HP-APPEND | 18.868950 | 22.645300 | fail (+20.01%) | recorded, not gated | — | **recorded** |
| HP-LIST | 746.914800 | 3210.647100 | fail (+329.85%) | approved-cost ceiling | 3531.711810 | **pass** |
| HP-OPEN | 22.066150 | 410.623550 | fail (+1760.88%) | approved-cost ceiling | 451.685905 | **pass** |

## Why HP-LIST and HP-OPEN are gated on a ceiling

Their cost is the fail-closed cross-key validation AC6 makes mandatory. At the baseline a detail
read was one store read. It is now that read plus a full tenant-index read plus a dispatch-ledger
read, and each list page additionally bulk-reads one detail record and one ledger record per
returned row and structurally compares each summary against its index entry. The published rule
therefore compares a fast, incorrect read path against a slower, correct one and reports the
correctness as a regression. No optimisation closes a 5% gate against a single-read baseline — even
splitting the tenant index into per-conversation entry keys is three small reads where the baseline
did one.

The ceiling is the measured post p95 plus 10% headroom, recorded numerically above. The approved
factor does not block; **exceeding the ceiling does**, so a later change that makes these paths
slower still goes red. The ceilings are Story 6.2's measurement, not a permanent budget: Story 6.6
re-measures under the same rule, and Story 6.11 retires them by re-gating both rows at ±5%.

## Why HP-CREATE and HP-APPEND are recorded rather than gated

**A `pass` on either row may not be cited as evidence of no regression.** HP-APPEND spans 3.797750
to 26.665300 µs *within a single 30-sample run*, a factor of 7, and nearest-rank p95 selects the
transient peak. Across two rounds on byte-identical code HP-APPEND flipped `pass` → `fail` while
HP-CREATE flipped the other way, and HP-CREATE's baseline p95 alone moved 0.7646 → 1.7566 µs, a
factor of 2.3. A ±5% threshold cannot adjudicate a statistic whose own dispersion is two orders of
magnitude wider. Both rows are measured, published with all raw samples, and left ungated rather
than read as signal they do not carry.

The fixture crosses the authorization guard and canonical create boundary for CREATE, and the
authorization guard plus production idempotent executor for APPEND. LIST and OPEN retain the
production query, read-store, and hydration paths. The JSON contains the evaluated project graph,
canonical command paths, all raw samples, and hashes of the runner-generated xUnit artifacts.
