# SM-C2 Hot-Path Post v1

- Inventory: `sm-c2-hot-path-inventory-v1`
- Baseline: `docs/release-evidence/sm-c2-hot-path-baseline-v1.json`
- Fixture SHA-256: `fd2c61842a7c254f786f3165b9848b404473e114bcd7514a688f9d73807df26f` (byte-identical to baseline)
- Envelope: Release, .NET 10.0.10 / SDK 10.0.302, xUnit v3 3.2.2, one worker, five warmups, 30 repetitions, 2,000 operations per sample
- Host: Linux 6.6.87.2-microsoft-standard-WSL2 x86_64; AMD Ryzen 9 9950X3D; 24 logical processors available
- Rule: `post P95 <= 1.05 * baseline P95`

| Hot path | Baseline P95 | Post P95 | Maximum allowed | Result |
| --- | ---: | ---: | ---: | --- |
| HP-CREATE | 0.436000 | 0.448550 | 0.457800 | pass |
| HP-APPEND | 9.585550 | 9.511050 | 10.064828 | pass |
| HP-LIST | 3.273800 | 3.331050 | 3.437490 | pass |
| HP-OPEN | 0.030550 | 0.029950 | 0.032078 | pass |

The authoritative JSON contains all raw samples, the exact command, the frozen envelope, and the mechanical comparison values. All four inventory rows pass.
