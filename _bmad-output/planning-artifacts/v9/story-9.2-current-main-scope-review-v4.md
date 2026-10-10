# Story 9.2 current-main scope review v4

This packet measures the committed tree only. It requests a new scope decision and claims neither authorization, Quality approval, nor candidate acceptance.

- Prior authorized source: `fbe2f502eed26df45edc12e4a12e9917bf97b438`.
- Measured committed candidate: `353e9dbf47a1472b29fe3f43d485a8ae3a03b884`; tree `b3af5900eb22e356360d247adc99a0fe662415fd`.
- Proposal file SHA-256: `0939819cbe661a25abcb8746dd85a7cecf2fdfd1ed33a957d1265d42257930b1`; canonical material SHA-256: `f5dfa7f71b93bbb799559bbddef69464cd63d2a49dc8ffb127323c2e5e8f1c44`.
- Existing v6 Quality approval SHA-256: `dc66a8610bd6483ee6d8ad50e7f1d6dbe496c6f85e84ebf1b0ab1785cc75e57d`; it binds the prior `fbe2f502eed26df45edc12e4a12e9917bf97b438` source and does not cover this candidate.
- Scope: six linear commits, 28 net changed paths, seven changed root gitlinks, one owning promotion commit, and zero production `src/` path changes.
- The committed package environment uses EventStore `3.119.0` from the promoted Builds catalog. The earlier v6 API receipts cover `3.117.1`→`3.118.0` and do not review this package version.
- Commit `c3c7502` retracted the stale final JSON/Markdown pair in one pair-only commit; their historical blobs remain in Git. The current candidate has no published pair.

## Intervening commits

| Commit | Parent | Subject |
| --- | --- | --- |
| `98a7aa13aadf0afeab73efc8ce7fea6ca562beef` | `fbe2f502eed26df45edc12e4a12e9917bf97b438` | fix(conformance): bind story 9.2 successor approval and candidate gates |
| `c3c7502f7fd5e08377694a530f8ba3560f2c7e37` | `98a7aa13aadf0afeab73efc8ce7fea6ca562beef` | docs(conformance): retract stale story 9.2 final record pair |
| `8700f92fb6e4155b893c054b56e56c458bab8788` | `c3c7502f7fd5e08377694a530f8ba3560f2c7e37` | fix(conformance): allow pair-only story 9.2 record retraction |
| `d66ba43297119a6bb77eb30c20f05cf2900221bc` | `8700f92fb6e4155b893c054b56e56c458bab8788` | fix: update HexalithEventStoreVersion to 3.119.0 and adjust related comment |
| `8469c5c6891d12b43d2082136b1ca7ddfac52c3b` | `d66ba43297119a6bb77eb30c20f05cf2900221bc` | fix(conformance): use successor inputs in story 9.2 record |
| `353e9dbf47a1472b29fe3f43d485a8ae3a03b884` | `8469c5c6891d12b43d2082136b1ca7ddfac52c3b` | fix(conformance): add deferred work entries for Story 9.2 regression tests and verification gaps |

## Changed root gitlinks

| Path | Before | After |
| --- | --- | --- |
| `references/Hexalith.Builds` | `2cf00028bbe563d80d4d12b5fb2054914f14fcb6` | `6a002df5fa60f2f880f09ffd90b3bfaf819bcbf5` |
| `references/Hexalith.EventStore` | `37451b529ab21869fa4e2806968b5143ddeea14b` | `9b26956f9e825fd6771b62a79c7b81323ba856cf` |
| `references/Hexalith.Folders` | `31909333eb763f73275c4d3df4367f5d06219c2f` | `4d2d7d820431cb354804583ee5c217ec9ce11fb6` |
| `references/Hexalith.Memories` | `7b33e016a52907989181139298db18edef1a05d7` | `1295decba902954c3f783d2084f979b808ed7538` |
| `references/Hexalith.Parties` | `926faa207bab55eeeed4bc7d5d5a50b3e6af917f` | `56cb4401c4179d431385ad3d931fefa402e4fc5a` |
| `references/Hexalith.Projects` | `fff5a5dbb74ff79bf56cd10527260178776eb060` | `44188d8985789f7ecff7e8f9fde7b11a1ba61df0` |
| `references/Hexalith.Tenants` | `4b0cfa3440d2a0b623c4e9d4d1ba4b6aba9de100` | `cdd0c80c111109125f6bec63d7cf1fd7a3d64885` |

## Measured gate state

- Current-tree structural verification passes.
- Separate portable and internal Release restores and builds pass with zero warnings or errors against the committed `3.119.0` package catalog. These are current-tree build checks, not candidate-bound AC01–10.
- The authorized v3 route rejects the later root gitlinks with `SUCCESSOR_SCOPE_DRIFT`.
- Inserted-record verification rejects the absent final pair with `RECORD_CONTENT_DRIFT`.
- Candidate-bound AC01–10, new Quality material, and final publication have not been measured or claimed.

## Decision requested

Authorize successor preparation for exactly this measured six-commit chain, 28 net changed paths, seven root gitlinks, the effective EventStore 3.119.0 package catalog, and the pair-only historical record retraction.

If authorized, prepare a new exact source/package/migration and API review packet, seek a genuine Quality decision for its digest, then run all candidate-bound acceptance and final-record gates. The requested scope decision alone supplies no Quality approval or acceptance.
