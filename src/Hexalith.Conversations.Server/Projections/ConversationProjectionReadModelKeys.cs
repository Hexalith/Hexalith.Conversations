// <copyright file="ConversationProjectionReadModelKeys.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Stable, tenant-scoped state-store keys shared by the conversation projection read store and writer.
/// </summary>
/// <remarks>
/// The SDK <see cref="Hexalith.EventStore.Client.Projections.IReadModelStore"/> has no built-in tenant
/// awareness, so the caller bakes the tenant into every key (mirroring the Tenants precedent). A different
/// tenant resolves to a different key, which keeps cross-tenant reads impossible by construction.
/// </remarks>
internal static class ConversationProjectionReadModelKeys
{
    /// <summary>The DAPR state-store component name (sibling convention).</summary>
    internal const string StateStoreName = "statestore";

    /// <summary>The diagnostic category for the per-conversation summary/detail read model.</summary>
    internal const string ConversationKeyCategory = "conversation read-model";

    /// <summary>The diagnostic category for the per-tenant conversation summary index.</summary>
    internal const string TenantIndexKeyCategory = "conversation index";

    private const string ConversationKeyPrefix = "projection:conversations:";
    private const string TenantIndexKeyPrefix = "projection:conversations-index:";

    /// <summary>
    /// Builds the per-conversation key holding the persisted summary/detail pair.
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="conversationId">The conversation identity.</param>
    /// <returns>The tenant-scoped conversation key.</returns>
    internal static string ConversationKey(TenantId tenantId, ConversationId conversationId)
        => $"{ConversationKeyPrefix}{tenantId.Value}:{conversationId.Value}";

    /// <summary>
    /// Builds the per-tenant summary-index key read as a single store entry by the list boundary (no N+1).
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <returns>The tenant-scoped index key.</returns>
    internal static string TenantIndexKey(TenantId tenantId)
        => $"{TenantIndexKeyPrefix}{tenantId.Value}";
}
