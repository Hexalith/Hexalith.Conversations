// <copyright file="ConversationClientContext.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Client;

/// <summary>
/// Carries caller context used to build supported v1 command metadata and read queries.
/// </summary>
public sealed record ConversationClientContext(
    TenantId TenantId,
    PartyId ActorPartyId,
    string CallerPrincipalId,
    string CorrelationId,
    string? CausationId = null,
    string? IdempotencyKey = null)
{
    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>
    /// Gets the stable caller Party identity for command metadata.
    /// </summary>
    public PartyId ActorPartyId { get; } = ActorPartyId ?? throw new ArgumentNullException(nameof(ActorPartyId));

    /// <summary>
    /// Gets the caller principal identity used by read authorization.
    /// </summary>
    public string CallerPrincipalId { get; } = ValidateRequired(CallerPrincipalId, nameof(CallerPrincipalId));

    /// <summary>
    /// Gets the caller correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId, nameof(CorrelationId));

    /// <summary>
    /// Creates v1 command metadata for supported write contracts.
    /// </summary>
    /// <returns>The command metadata.</returns>
    public ConversationCommandMetadata ToCommandMetadata()
        => new(SchemaVersion.Current, TenantId, ActorPartyId, CorrelationId, CausationId, IdempotencyKey);

    /// <summary>
    /// Creates a v1 get-conversation query for the supplied conversation identity.
    /// </summary>
    /// <param name="conversationId">The conversation identity.</param>
    /// <returns>The get-conversation query.</returns>
    public GetConversationQuery ToGetConversationQuery(ConversationId conversationId)
        => new(SchemaVersion.Current, TenantId, CallerPrincipalId, CorrelationId, conversationId);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
