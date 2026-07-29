# SM-C2 Hot-Path Post v1

**Result: fail — 2 of 4 rows pass.**

- Baseline: `docs/release-evidence/sm-c2-hot-path-baseline-v1.json`
- Source commit: `28e217ed0ac8c1028a4783f80ec47393ff1fbfbd` with EventStore gitlink `4c63f5d3e8089a85891cdbf8d87ce82ee445354a`
- Fixture SHA-256: `a01d182cf195cc1a4db3f50fcdd76051d97ccb9fe2d25fc109dfabfaac2de6bf` (byte-identical to baseline)
- Project overlay SHA-256: `e88f942c2b8eb45d5a168bc832d77698a9073baac88033023fe1b6181ea68d1e` (byte-identical to baseline)
- Envelope: Release, .NET 10.0.10 / SDK 10.0.302, xUnit v3 3.2.2, one worker, five warmups, 30 repetitions, 2,000 operations per sample
- Rule: `post P95 <= 1.05 * baseline P95`

| Hot path | Baseline P95 | Post P95 | Maximum allowed | Change | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| HP-CREATE | 0.450850 | 0.429550 | 0.473393 | -4.724409% | pass |
| HP-APPEND | 12.821500 | 12.871000 | 13.462575 | +0.386070% | pass |
| HP-LIST | 541.500000 | 2150.809150 | 568.575000 | +297.194672% | **fail** |
| HP-OPEN | 22.588900 | 276.978600 | 23.718345 | +1126.171261% | **fail** |

The previous toy fixture could not expose projection-read regressions. This production-path fixture does: LIST and OPEN exceed the frozen 5% threshold, so SM-C2 remains an open Story 6.2 release blocker. The JSON contains all raw samples and mechanical comparison values.
