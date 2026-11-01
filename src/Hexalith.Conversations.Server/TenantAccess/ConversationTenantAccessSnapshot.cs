// <copyright file="ConversationTenantAccessSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.TenantAccess;
using Hexalith.Tenants.Client.Projections;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Binds both shared tenant-access adapters to one durable projection snapshot for a single evaluation.
/// </summary>
/// <remarks>
/// The first adapter call reads the snapshot once; the health and the state the evaluator sees then come from
/// that same persisted record, so a concurrent delivery or recovery can never pair one record's health with
/// another record's members. Instances are per invocation and never shared.
/// </remarks>
/// <param name="snapshotStore">The durable Tenants snapshot store.</param>
internal sealed class ConversationTenantAccessSnapshot(ITenantProjectionSnapshotStore snapshotStore)
    : ITenantAccessStateStore, ITenantAccessProjectionHealthProvider
{
    private readonly ITenantProjectionSnapshotStore _snapshotStore =
        snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));

    private Task<TenantProjectionSnapshot?>? _read;
    private string? _tenantId;

    /// <summary>Gets the health observed for this evaluation, when the health adapter ran.</summary>
    internal TenantAccessProjectionHealth? ObservedHealth { get; private set; }

    /// <summary>Gets the number of durable snapshot reads this evaluation performed.</summary>
    internal int Reads { get; private set; }

    /// <inheritdoc />
    public async ValueTask<TenantAccessProjectionHealth?> GetProjectionHealthAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        TenantProjectionSnapshot? snapshot = await ReadOnceAsync(tenantId, cancellationToken).ConfigureAwait(false);
        ConversationTenantProjectionHealth health = ConversationTenantSnapshotHealth.From(snapshot, tenantId);
        return ObservedHealth = new TenantAccessProjectionHealth(
            health.Version,
            health.Watermark,
            health.IsStale,
            health.HasGap,
            health.HasRollback,
            health.IsPoisoned);
    }

    /// <inheritdoc />
    public async Task<TenantAccessState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        TenantProjectionSnapshot? snapshot = await ReadOnceAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (snapshot is not { IsHealthy: true } || !string.Equals(snapshot.TenantId, tenantId, StringComparison.Ordinal))
        {
            return null;
        }

        TenantLocalState state = snapshot.State!;
        return new TenantAccessState(
            state.TenantId,
            (int)state.Status,
            state.Members?.ToDictionary(
                static pair => pair.Key,
                static pair => (int)pair.Value,
                StringComparer.Ordinal));
    }

    private Task<TenantProjectionSnapshot?> ReadOnceAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (_read is null)
        {
            _tenantId = tenantId;
            Reads++;
            _read = _snapshotStore.GetSnapshotAsync(tenantId, cancellationToken);
        }
        else if (!string.Equals(_tenantId, tenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("One tenant-access evaluation reads exactly one tenant snapshot.");
        }

        return _read;
    }
}
