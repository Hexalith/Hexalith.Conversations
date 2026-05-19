// <copyright file="DefaultConversationTenantProjectionSignal.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Tenants.Client.Projections;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Default projection signal that delegates to the registered <see cref="ITenantProjectionStore"/>
/// when it implements <see cref="IConversationTenantProjectionSignal"/>, and otherwise reports
/// healthy state while logging a warning so operators see that freshness/poisoning checks are absent.
/// </summary>
/// <param name="projectionStore">The registered tenant projection store.</param>
/// <param name="logger">The signal logger.</param>
public sealed class DefaultConversationTenantProjectionSignal(
    ITenantProjectionStore projectionStore,
    ILogger<DefaultConversationTenantProjectionSignal> logger)
    : IConversationTenantProjectionSignal
{
    private readonly ILogger<DefaultConversationTenantProjectionSignal> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ITenantProjectionStore _projectionStore =
        projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    private int _warned;

    /// <inheritdoc />
    public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (_projectionStore is IConversationTenantProjectionSignal underlyingSignal)
        {
            return underlyingSignal.GetProjectionHealthAsync(tenantId, cancellationToken);
        }

        if (Interlocked.Exchange(ref _warned, 1) == 0)
        {
            // Production deployments must wire a tenant projection store that surfaces freshness,
            // gap, rollback, and poisoning signals (ADR-003). Until that work lands, we record a
            // single warning so the absence is visible in logs while the access service continues
            // to enforce the membership/state checks below.
            _logger.LogWarning(
                "Registered ITenantProjectionStore ({StoreType}) does not implement IConversationTenantProjectionSignal; "
                + "tenant access freshness, gap, rollback, and poisoning signals are not available. "
                + "Production deployments must register a signal-capable store before relying on tenant isolation guarantees.",
                _projectionStore.GetType().Name);
        }

        return ValueTask.FromResult(ConversationTenantProjectionHealth.Healthy);
    }
}
