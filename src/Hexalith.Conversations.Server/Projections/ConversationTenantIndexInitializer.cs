// <copyright file="ConversationTenantIndexInitializer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Initializes a created tenant's Conversations index as version 2 <c>Ready(empty)</c> from the authoritative
/// <c>TenantCreated</c> event (AD-7), so an initialized tenant with no conversations reads as current-empty.
/// </summary>
/// <remarks>
/// <para>
/// The handler only asks <see cref="ConversationProjectionReadModelWriter"/> for a create-only initialization; an
/// existing index (a live delivery already created it, or a redelivery) is left unchanged. Store failures
/// propagate, so the delivery is retried rather than acknowledged.
/// </para>
/// <para>
/// It is registered before the Tenants projection handlers. The EventStore domain-event processor runs handlers
/// sequentially in registration order and stops at the first failure, so when <c>TenantCreated</c> is delivered in
/// order, the tenant becomes healthy (and its callers authorized) only after its index is <c>Ready</c>.
/// </para>
/// <para>
/// That ordering does not hold out of order. When a later tenant event arrives first, the checkpointed Tenants
/// projection records the gap and recovers <c>TenantCreated</c> from gateway history without running this handler,
/// so the tenant can become healthy with no index. The index then stays missing (<c>Unavailable</c>) until
/// <c>TenantCreated</c> itself is delivered here, or becomes <c>Rebuilding</c> if a conversation is delivered first,
/// until the deferred tenant-wide rebuild.
/// </para>
/// </remarks>
/// <param name="writer">The only author of tenant-index transitions.</param>
internal sealed class ConversationTenantIndexInitializer(ConversationProjectionReadModelWriter writer)
    : IEventStoreDomainEventHandler<TenantCreated>
{
    private readonly ConversationProjectionReadModelWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    /// <inheritdoc/>
    public async Task HandleAsync(TenantCreated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);

        // Only the tenants domain addresses a tenant; any other domain (and a payload naming a different tenant
        // than its stream) is acknowledged without a write, exactly as the Tenants projection ignores it.
        if (!string.Equals(context.Domain, TenantIdentity.Domain, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(context.AggregateId)
            || !string.Equals(@event.TenantId, context.AggregateId, StringComparison.Ordinal)
            || !TryCreateTenantId(context.AggregateId, out TenantId? tenantId))
        {
            return;
        }

        _ = await _writer.InitializeTenantIndexAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryCreateTenantId(string value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TenantId? tenantId)
    {
        try
        {
            tenantId = new TenantId(value);
            return true;
        }
        catch (ArgumentException)
        {
            tenantId = null;
            return false;
        }
    }
}
