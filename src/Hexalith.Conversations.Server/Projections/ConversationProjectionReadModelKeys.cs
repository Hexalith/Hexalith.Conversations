// <copyright file="ConversationProjectionReadModelKeys.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;

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

    /// <summary>The diagnostic category for stable projection dispatch ledgers.</summary>
    internal const string DispatchLedgerKeyCategory = "conversation dispatch ledger";

    private const string ConversationKeyPrefix = "projection:conversations:";
    private const string TenantIndexKeyPrefix = "projection:conversations-index:";
    private const string DispatchLedgerKeyPrefix = "projection:conversations-dispatch:";

    /// <summary>
    /// Builds the per-conversation key holding the persisted summary/detail pair.
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="conversationId">The conversation identity.</param>
    /// <returns>The tenant-scoped conversation key.</returns>
    internal static string ConversationKey(TenantId tenantId, ConversationId conversationId)
        => $"{ConversationKeyPrefix}{RequireKeySegment(tenantId.Value)}:{RequireKeySegment(conversationId.Value)}";

    /// <summary>
    /// Builds the per-tenant summary-index key used to locate the candidate summary set.
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <returns>The tenant-scoped index key.</returns>
    internal static string TenantIndexKey(TenantId tenantId)
        => $"{TenantIndexKeyPrefix}{RequireKeySegment(tenantId.Value)}";

    /// <summary>
    /// Rejects an identifier that would make the composed key ambiguous.
    /// </summary>
    /// <param name="value">The identifier segment.</param>
    /// <returns>The validated segment.</returns>
    /// <remarks>
    /// <see cref="TenantId"/> and <see cref="ConversationId"/> validate only that a value is non-blank, so the
    /// separator is legal inside them. Tenant <c>a:b</c> with conversation <c>c</c> and tenant <c>a</c> with
    /// conversation <c>b:c</c> would otherwise compose the same key, and the write happens before the reader's
    /// tenant re-check can notice. Failing closed here keeps the "a different tenant resolves to a different
    /// key" property this module's cross-tenant isolation is built on.
    /// </remarks>
    private static string RequireKeySegment(string value)
    {
        if (value.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A projection key segment must not contain the key separator.",
                nameof(value));
        }

        return value;
    }

    /// <summary>Builds the bounded storage key for one stable projection dispatch identity.</summary>
    /// <param name="dispatchId">The stable platform dispatch identity.</param>
    /// <returns>A non-disclosing fixed-length dispatch-ledger key.</returns>
    internal static string DispatchLedgerKey(string dispatchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchId);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(dispatchId));
        return $"{DispatchLedgerKeyPrefix}{Convert.ToHexStringLower(digest)}";
    }
}
