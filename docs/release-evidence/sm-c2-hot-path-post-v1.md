# SM-C2 Hot-Path Post v1

**Result: fail — 1 of 4 rows pass.**

- Baseline: `docs/release-evidence/sm-c2-hot-path-baseline-v1.json`
- Source commit: `21b1fad0a7b97b89dc9c898e2cadee828b4a3b3f` with EventStore gitlink `e4618d9114c8824fd50fdfc8d135438aa261377c`
- Fixture SHA-256: `4838a5a174b247e3aa4cc6be0c181049a46d884612058df248af8892adaf3fff` (byte-identical to baseline)
- Project overlay SHA-256: `e88f942c2b8eb45d5a168bc832d77698a9073baac88033023fe1b6181ea68d1e` (byte-identical to baseline)
- Envelope: Release, .NET 10.0.10 / SDK 10.0.302, xUnit v3 3.2.2, one worker, five warmups, 30 repetitions, 2,000 operations per sample
- Rule: `post P95 <= 1.05 * baseline P95`

| Hot path | Baseline P95 | Post P95 | Maximum allowed | Change | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| HP-CREATE | 1.756600 | 0.875900 | 1.844430 | -50.136600% | pass |
| HP-APPEND | 18.868950 | 22.645300 | 19.812398 | +20.013600% | **fail** |
| HP-LIST | 746.914800 | 3210.647100 | 784.260540 | +329.854500% | **fail** |
| HP-OPEN | 22.066150 | 410.623550 | 23.169458 | +1760.875400% | **fail** |

The fixture now crosses the authorization guard and canonical create boundary for CREATE, and the authorization guard plus production idempotent executor for APPEND. LIST and OPEN retain the production query/read-store/hydration paths. LIST and OPEN exceed the frozen 5% threshold, so SM-C2 remains an open Story 6.2 release blocker. The JSON contains the evaluated project graph, canonical command paths, all raw samples, and hashes of the runner-generated xUnit artifacts.
