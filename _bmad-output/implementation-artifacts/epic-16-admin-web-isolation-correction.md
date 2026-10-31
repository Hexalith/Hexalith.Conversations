# Admin Web isolation correction authorized 2026-10-31

This is a separately recorded Conversations hosting correction accompanying
`spec-epic-16-4-38-3-qualification-and-binding-refresh.md`. It does not amend
the existing frozen intent or authorize Story 16.1 implementation.

The repair target is the retained full baseline at
`module/validation/epic-16-published-artifact-qualification/attempt-20261031-4383-02/`.
Source preparation passed. Admin Web had no declared endpoint and exited 134
when its default `http://127.0.0.1:5000` was already occupied by another session;
its health wait exited 18. Preserve that evidence and the unrelated listener.

The user authorizes a narrow edit to
`src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` so Admin Web
receives an Aspire-managed HTTP endpoint compatible with `--isolated`.
Inspect the documented Aspire pattern and existing topology first. Do not pin
a port, disable authentication, alter dependency selection, or retire hosts.
Validate the correction with focused existing AppHost checks where relevant
and the complete fresh baseline. Extend an existing meaningful topology test
only if needed to verify endpoint allocation behavior.

Before editing, capture the current protected inputs, retained failed evidence,
raw mode-160000 gitlinks and actual checkouts, index bytes, frozen block bytes,
runtime output absence and unrelated session inventory in a new attempt.
The retained failure is the authorized pre-repair baseline. Do not repeat a
known port conflict merely to recreate it before making the scoped repair.

After the correction, rebuild the existing producer CLI in Debug if required,
rerun source preparation in a fresh workspace, inherit its full selected
environment, and run the exact complete mapped baseline:

```text
aspire run --detach --isolated --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj --format Json --non-interactive
```

Inspect every required resource, including Conversations, EventStore, Admin Web,
security, state store, pub/sub, their active Dapr resources, and native abstract
and rebuilder states. Retain exact commands, streams, exits, endpoint allocation
and health waits. Use native resource inspection to discover the endpoint;
verify Admin Web's `/health` without bypassing authentication. Stop only the
created instance after immediately verifying its process identity, and compare
unrelated sessions before/after. Never use force/all cleanup or volume deletion.

If this fresh complete baseline fails, stop collector/v3 implementation and
record the exact blocker. If it passes, resume every approved prerequisite task
in the parent spec and its detailed plan, preserving distinct owner gate
failures and candidate qualification outcomes. Any unavailable authentication
or required artifact/control evidence blocks the dependent qualification;
never bypass authentication or substitute local Release hosts.

Preserve the entry tool pins, gitlinks, checkout revisions, accepted fixtures
and evidence. Never initialize nested submodules. Keep Story 16.1 draft/backlog,
entry null, independent trust empty, hold ACTIVE and all four authority flags
false until every required entry qualification passes. Recompute retained
hashes, prove exact changed-path equality and input restoration, and retain a
nonempty evaluated ledger. Historical boundary replay and runtime publication
remain not-applicable.
