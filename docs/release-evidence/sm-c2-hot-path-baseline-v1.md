# SM-C2 Hot-Path Baseline v1

- Inventory: `sm-c2-hot-path-inventory-v1`
- Source commit: `29def441408becfbbbdc5c59b9af14a7717cb21f`
- Fixture: `tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs`
- Fixture SHA-256: `fd2c61842a7c254f786f3165b9848b404473e114bcd7514a688f9d73807df26f`
- Classification: warm in-process module boundary
- Envelope: Release, .NET 10.0.10 / SDK 10.0.302, xUnit v3 3.2.2, one worker, five warmups, 30 repetitions, 2,000 operations per sample
- Host: Linux 6.6.87.2-microsoft-standard-WSL2 x86_64; AMD Ryzen 9 9950X3D; 24 logical processors available
- Processing: elapsed `Stopwatch` ticks converted to microseconds per operation; P95 is nearest-rank `ceil(0.95 * n)`.

| Hot path | Frozen workload | P95 (microseconds/op) |
| --- | --- | ---: |
| HP-CREATE | Tenant-bound v1 create through `ConversationAggregate.Handle`. | 0.436000 |
| HP-APPEND | Accepted append plus equivalent replay and payload-mismatch fingerprint mix. | 9.585550 |
| HP-LIST | Filter/order/page 100 warm tenant summary identities to 25 rows. | 3.273800 |
| HP-OPEN | Warm identity lookup and tenant/conversation/lifecycle/message-count read. | 0.030550 |

## Reconstruction provenance

The versioned fixture does **not** exist at source commit `29def441408becfbbbdc5c59b9af14a7717cb21f`; it was added alongside the Story 6.2 production edits. This baseline is therefore a reconstruction, which AC1 permits from the preserved source commit with the same versioned fixture. How it is anchored:

- **Method:** overlay the versioned fixture (`SHA-256 fd2c6184…`, byte-identical to the one the post run used) onto the preserved `29def44` production sources.
- **Measured production closure:** `src/Hexalith.Conversations` and `src/Hexalith.Conversations.Contracts`. The fixture exercises `ConversationAggregate.Handle`, `ConversationCommandFingerprint`, `ConversationState`, and the Contracts command/event/identifier/versioning types, and touches no Server, Client, Admin, or platform assembly.
- **Verification:** `git diff --name-only 29def44..<post-run-revision> -- src/Hexalith.Conversations src/Hexalith.Conversations.Contracts` reports **0 changed files**, so the overlaid closure at `29def44` and the closure at the post-run tree are byte-identical.
- **Why that makes the comparison meaningful:** both runs compile and measure the same production sources under one envelope, so the row-by-row P95 comparison reflects the envelope and the machine, not a source difference.
- **Residual limitation, stated plainly:** Story 6.2 changed no source inside the measured closure, so this gate confirms no regression rather than exercising the changed hosting and projection code. It is not a gate that could have failed for this story.

The companion JSON is authoritative for all raw samples, data shapes, repetitions, and processing. The post run must execute the byte-identical fixture envelope and compare every row against `post P95 <= 1.05 * baseline P95`.
