# Story 9.2 current-main scope review v3

The [v3 proposal](story-9.2-current-main-scope-proposal-v3.json) measures the one committed change from the previously authorized `ec5c9be52631b96793ab028febfbbfba4d362d48` to `fbe2f502eed26df45edc12e4a12e9917bf97b438`. It records all 17 changed paths, including six root gitlinks, with before and after Git object identities and SHA-256 hashes for blobs. The proposal excludes every uncommitted working-tree edit, including the separate `Directory.Packages.props` change to EventStore `3.119.0`.

| Binding | SHA-256 |
| --- | --- |
| v3 proposal file | `30cb277b1b549c13a7c06e380f5477f01843f9c665ce70855c21f28703c54811` |
| v3 canonical proposal material | `81b73f5642f6fff1f155d0f4bdce6b76dfad25a7a80e5b8991bdc3df53253745` |
| Existing v5 Quality approval file | `6a0218ea421ff448dee93e066b6bbc801498cecd28d95e7322a783a97ebbceb3` |

The commit adds the v2 scope authorization, v5 migration proposal and review, EventStore API receipts, successor preparation code and tests, and runbook/spec updates. It also changes these root gitlinks:

| Root submodule | Prior commit | Current commit |
| --- | --- | --- |
| Builds | `0d988cfc5ea123553f6961b6f3f04f375ff8ac3a` | `2cf00028bbe563d80d4d12b5fb2054914f14fcb6` |
| EventStore | `5e5d2305d870a03d6d63f52470ce9f7a1ec058da` | `37451b529ab21869fa4e2806968b5143ddeea14b` |
| Folders | `17b5a70d2b4b91bbbf73ea63c8a214a0ad426759` | `31909333eb763f73275c4d3df4367f5d06219c2f` |
| Memories | `858a828d7cdb648d18f07a4a568e8d8a52f92113` | `7b33e016a52907989181139298db18edef1a05d7` |
| Projects | `eaf195b1c0a654127ca85d22069b6e1129911c86` | `fff5a5dbb74ff79bf56cd10527260178776eb060` |
| Tenants | `42d1e30044c8aaa16627c87c197e6e1a17bedbe4` | `4b0cfa3440d2a0b623c4e9d4d1ba4b6aba9de100` |

The existing [v5 Quality approval](../../../docs/release-evidence/conformance-oracle-tiering-migration-approval-v5.json) is genuine and binds the `ec5c9be` source snapshot. It does not authorize this later commit or its six gitlink changes. The unchanged candidate-environment validator also rejects the resulting history with `AUTHORITY_BINDING_INVALID`; its original amendment permits exactly seven earlier net gitlinks and five owning promotion commits. Current-tree Release builds and structural verification pass, but they are not candidate-bound AC01–10 acceptance.

**Decision requested:** authorize successor preparation from the exact committed v3 proposal material above, while preserving the original baseline, frozen intent, historical approvals, and accepted records. After that decision, remeasure the successor at the newly authorized source, obtain a Quality decision for any changed binding, and run the full committed-candidate and final-record gates. This packet does not claim scope authorization, new Quality approval, or completion.
