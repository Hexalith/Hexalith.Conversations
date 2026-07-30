# SM-C2 Hot-Path Post v1

**Result: fail — 2 of 4 rows pass.**

- Baseline: `docs/release-evidence/sm-c2-hot-path-baseline-v1.json`
- Source commit: `b261fe209c4ca6c966f4bd2a78a62a2d83ddde08` with EventStore gitlink `defb426f0bd9e3bd1247bc7149605b4bb6ef70d0`
- Fixture SHA-256: `4838a5a174b247e3aa4cc6be0c181049a46d884612058df248af8892adaf3fff` (byte-identical to baseline)
- Project overlay SHA-256: `e88f942c2b8eb45d5a168bc832d77698a9073baac88033023fe1b6181ea68d1e` (byte-identical to baseline)
- Envelope: Release, .NET 10.0.10 / SDK 10.0.302, xUnit v3 3.2.2, one worker, five warmups, 30 repetitions, 2,000 operations per sample
- Rule: `post P95 <= 1.05 * baseline P95`

| Hot path | Baseline P95 | Post P95 | Maximum allowed | Change | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| HP-CREATE | 0.764600 | 0.735000 | 0.802830 | -3.871305% | pass |
| HP-APPEND | 21.946550 | 15.878150 | 23.043878 | -27.650815% | pass |
| HP-LIST | 594.565050 | 2264.908850 | 624.293303 | +280.935417% | **fail** |
| HP-OPEN | 25.319550 | 296.873450 | 26.585528 | +1072.506818% | **fail** |

The fixture now crosses the authorization guard and canonical create boundary for CREATE, and the authorization guard plus production idempotent executor for APPEND. LIST and OPEN retain the production query/read-store/hydration paths. LIST and OPEN exceed the frozen 5% threshold, so SM-C2 remains an open Story 6.2 release blocker. The JSON contains the evaluated project graph, canonical command paths, all raw samples, and hashes of the runner-generated xUnit artifacts.
