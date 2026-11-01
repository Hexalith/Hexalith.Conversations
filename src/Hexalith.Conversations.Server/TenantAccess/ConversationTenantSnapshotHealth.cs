// <copyright file="ConversationTenantSnapshotHealth.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.Tenants.Client.Projections;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Maps one durable Tenants projection snapshot to the Conversations projection health signal.
/// </summary>
/// <remarks>
/// <para>
/// Only a persisted <see cref="CheckpointedProjectionHealth.Healthy"/> record whose state names the requested
/// tenant reports healthy, with its checkpoint as the version. Gapped and corrupt records report a gap and
/// poisoning; a malformed, unavailable, or untrusted record reports stale. None of them carries a version or
/// watermark, and none of them exposes state, so they always deny.
/// </para>
/// <para>
/// A missing record reports no projection fault and no version: the projection holds no tenant to vouch for, so
/// the shared evaluator finds no state and denies with <c>UnknownTenant</c> — non-retryable with the same public
/// code as a non-member of an existing tenant — and never discloses whether the tenant exists. This is an absence,
/// not synthesized health: it can never allow, because only a healthy record exposes state.
/// </para>
/// </remarks>
internal static class ConversationTenantSnapshotHealth
{
    /// <summary>The non-disclosing health reported for malformed, unavailable, and untrusted snapshots.</summary>
    internal static ConversationTenantProjectionHealth Unproven { get; } = new(IsStale: true);

    /// <summary>The health reported when no record exists, which leaves the denial to the absent state.</summary>
    internal static ConversationTenantProjectionHealth Absent { get; } = new();

    /// <summary>
    /// Maps a snapshot to projection health.
    /// </summary>
    /// <param name="snapshot">The snapshot read for this evaluation, or <see langword="null"/> when absent.</param>
    /// <param name="tenantId">The canonical tenant identifier the snapshot was read for.</param>
    /// <returns>The projection health.</returns>
    internal static ConversationTenantProjectionHealth From(TenantProjectionSnapshot? snapshot, string tenantId)
    {
        if (snapshot is null)
        {
            return Absent;
        }

        if (!string.Equals(snapshot.TenantId, tenantId, StringComparison.Ordinal))
        {
            return Unproven;
        }

        return snapshot.Health switch
        {
            CheckpointedProjectionHealth.Healthy when snapshot.IsHealthy => new(Version: snapshot.Checkpoint),
            CheckpointedProjectionHealth.Gapped => new(HasGap: true),
            CheckpointedProjectionHealth.Corrupt => new(IsPoisoned: true),
            _ => Unproven,
        };
    }
}
