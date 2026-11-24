# Conversations 1.5.0 release notes

## Tenant access denial

Conversations denies access when its durable tenant-access projection is
missing, gapped, corrupt, malformed, stale, rolled back, or unavailable. The
denial uses `tenant_isolation_violation` with `isRetryable: false`. It has the same public
shape as other tenant-access refusals and preserves the visibility rules for
the resource.

Update integrations that interpreted `tenant_projection_stale` from a tenant
access check as an automatic retry. That code remains available for retryable
Conversation source freshness failures. The `stale-projection` and
`unavailable-projection` cases in `Hexalith.Conversations.Testing` now expect
`tenant_isolation_violation`.

When valid callers remain denied, check the enrolled tenant-event delivery and
the durable projection's recovery status using the
[Platform handoff](../module/platform-handoff.md).
