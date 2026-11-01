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
/// Only a persisted <see cref="CheckpointedProjectionHealth.Healthy"/> record whose state names the requested
/// tenant reports healthy. A missing, malformed, unavailable, or untrusted snapshot reports stale without any
/// version or watermark, so those denials are indistinguishable and never disclose whether the tenant exists.
/// Gapped and corrupt records report a gap and poisoning respectively. Nothing here synthesizes health.
/// </remarks>
internal static class ConversationTenantSnapshotHealth
{
    /// <summary>The non-disclosing health reported for missing, malformed, and unavailable snapshots.</summary>
    internal static ConversationTenantProjectionHealth Unproven { get; } = new(IsStale: true);

    /// <summary>
    /// Maps a snapshot to projection health.
    /// </summary>
    /// <param name="snapshot">The snapshot read for this evaluation, or <see langword="null"/> when absent.</param>
    /// <param name="tenantId">The canonical tenant identifier the snapshot was read for.</param>
    /// <returns>The projection health.</returns>
    internal static ConversationTenantProjectionHealth From(TenantProjectionSnapshot? snapshot, string tenantId)
    {
        if (snapshot is null || !string.Equals(snapshot.TenantId, tenantId, StringComparison.Ordinal))
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
