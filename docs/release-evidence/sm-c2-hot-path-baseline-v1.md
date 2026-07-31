# SM-C2 Hot-Path Baseline v1

- Inventory: `sm-c2-hot-path-inventory-v1`
- Source commit: `29def441408becfbbbdc5c59b9af14a7717cb21f`
- Baseline EventStore commit: `b2d3402552fbadf529c220fcc739da9d06d285fe`
- Fixture SHA-256: `4838a5a174b247e3aa4cc6be0c181049a46d884612058df248af8892adaf3fff`
- Project overlay SHA-256: `e88f942c2b8eb45d5a168bc832d77698a9073baac88033023fe1b6181ea68d1e`
- Envelope: Release, .NET 10.0.10 / SDK 10.0.302, xUnit v3 3.2.2, one worker, five warmups, 30 repetitions, 2,000 operations per sample
- Host: Linux 6.6.87.2-microsoft-standard-WSL2 x86_64; AMD Ryzen 9 9950X3D; 24 logical processors available

| Hot path | Frozen production workload | P95 (microseconds/op) |
| --- | --- | ---: |
| HP-CREATE | Authorized tenant-bound v1 create through `CreateConversationBoundary.Dispatch` and `ConversationAggregate.Handle`. | 1.756600 |
| HP-APPEND | Authorized accepted execution, duplicate replay, and payload conflict through `IdempotentConversationCommandExecutor`. | 18.868950 |
| HP-LIST | Two cursor-linked 25-row pages through the production query and projection-read paths. | 746.914800 |
| HP-OPEN | Rich detail through the production query, consistency, and hydration paths. | 22.066150 |

## Reconstruction provenance

The production-path fixture does **not** exist at source commit `29def441408becfbbbdc5c59b9af14a7717cb21f`, so this is the AC1-permitted reconstruction:

- The versioned fixture and IntegrationTests project file were overlaid onto the preserved commit; both are byte-identical to the post run.
- Only root-declared submodules were initialized. EventStore resolved to the baseline gitlink `b2d3402552fbadf529c220fcc739da9d06d285fe`.
- The measured closure includes Conversations domain, contracts, Server query/projection code, and the EventStore Client/Testing read-model implementation.
- The same Release toolchain, worker count, data shape, warmups, repetitions, and operation count were used for baseline and post. The production sources intentionally differ; that is what the regression gate measures.
- The evaluated direct-project-reference graph and the four canonical command-path descriptions are recorded identically in both JSON artifacts; the raw baseline samples are bound to `sm-c2-hot-path-baseline-v1.xunit.xml`.

Residual limitation: this remains an in-process warm-path benchmark, not a sidecar/network latency test. The companion JSON is authoritative for raw samples and exact provenance.
