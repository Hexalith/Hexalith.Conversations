# 4.38.3 prerequisite resume: provenance passed, baseline failed

This fresh attempt is **BLOCKED**. Canonical source retrieval and preparation
passed; the required full mapped Debug AppHost baseline **FAILS** because Admin
Web could not bind its default port. No collector/v3 implementation or candidate
artifact/control qualification followed that failure.

The user authorized retrieving only the exact Platform-recorded Builds commit.
[Canonical-remote verification](canonical-remote.json) confirms agreement among
the root declaration, Platform declaration and local Builds origin. From the
owning root-declared Builds repository, the exact fetch was:

```text
git -c fetch.recurseSubmodules=false fetch --no-recurse-submodules --no-tags --no-write-fetch-head https://github.com/Hexalith/Hexalith.Builds.git cc0db51b800be1d0d5badc3780395cfb5953d0f5
```

[The fetch](exact-builds-fetch.json) exited **0**, and the subsequent object
lookup identifies a commit. Its `Props/Directory.Packages.props` is readable
with blob `52c61818bc0a0f62489cb8bf91de143d73a7d07e`. This resolves the earlier
local-object blocker; the revision is available from the canonical upstream.
The ancestry probe refers only to the unchanged, older local `origin/main` and
does not establish ancestry against current upstream main. No broad fetch or
ref update was used.

[A new producer CLI Debug build](producer-cli-debug-build.json) and
[fresh source preparation](source-prepare.json) both exited **0**. The complete
[producer-selected environment](source-environment.json) was inherited by the
AppHost commands; copies of the exact materialized mapping/import bytes are
retained in `prepared-source-mapping/`. Native diagnostics HXW009, HXW010 and
HXW011 remain warnings in source mode. In particular, availability of the
recorded revision does not reconcile it with Platform's prepared 4.38.3 source
`d1931b1e803144048d927fff16cb8119afde541e`, verify published artifact bytes, or
establish owner acceptance.

The required fresh baseline command was:

```text
aspire run --detach --isolated --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json --non-interactive
```

It exited **0**, creating AppHost PID **52891**, CLI PID **46168**. The two
required health waits for Conversations and EventStore exited **0**.
[Final resource inspection](resource-summary.json) shows both healthy, along
with security, state store, pub/sub and their active Dapr CLI resources. An
initial security TLS health error cleared before final inspection and is not
the terminal blocker. Rebuilders and abstract Dapr resources are retained with
their actual native states; no all-resources-healthy claim is made.

Admin Web finished with **process exit 134** and no declared endpoints. Its
[native logs](apphost-logs-conversations-admin-web.stdout.txt) report
`Failed to bind to address http://127.0.0.1:5000: address already in use.` The
[listener observation](port-5000-listener.stdout.txt) identifies an existing
`dcp` process; it was not stopped. This exact required health wait exited **18**:

```text
aspire wait conversations-admin-web --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --status healthy --timeout 120 --non-interactive
```

The [qualification ledger](qualification-ledger.json) separates passing
provenance/preparation, failing baseline/recorded-source equality, and blocked
implementation/qualification. The next action is separately reviewed
Conversations hosting work for Admin Web endpoint/isolation, or owner-managed
environment preparation, followed by another fresh complete mapped baseline.
Changing dependencies or stopping unrelated sessions is not a remedy included
in this prerequisite. The frozen host topology was preserved.

[Ownership verification](cleanup-target-verification.json) matched both created
process identities immediately before a targeted stop. That stop exited **0**;
the before/after existing session inventories are identical. No force, all,
volume deletion or nested initialization was used. [Preservation checks](preservation-check.json)
retain protected hashes, the complete previous attempt, index bytes, twelve
direct gitlinks/checkouts, frozen spec blocks and absent runtime output paths.
[Integrity checks](evidence-integrity-check.json) rehash retained streams and
files and compare the exact changed-path inventory, distinguishing the inherited
changes from this fresh attempt. Dashboard login tokens and generated dashboard
credential values are redacted with raw and retained digests recorded in
`safe-diagnostic-redactions.json`.

The root tool pin remains **4.38.3**, roll-forward false. Story 16.1 stays
**draft/backlog**, entry null, independent trust empty, hold ACTIVE and all four
authority flags false. No runtime tests were run; runtime/native/final outputs
remain absent. All implementation tasks are incomplete. Historical boundary
replay and runtime publication are **not-applicable** to this attempt.
