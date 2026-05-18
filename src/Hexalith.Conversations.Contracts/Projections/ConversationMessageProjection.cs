// <copyright file="ConversationMessageProjection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Minimal adopter-facing conversation message read contract.
/// </summary>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="messageId">The stable message identity.</param>
/// <param name="authorPartyId">The stable Party reference for the author.</param>
/// <param name="text">The message text visible to the caller.</param>
/// <param name="createdAt">The public message creation timestamp.</param>
/// <param name="freshness">The freshness and trust state.</param>
public sealed record ConversationMessageProjection(
    TenantId TenantId,
    ConversationId ConversationId,
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    DateTimeOffset CreatedAt,
    ProjectionFreshness Freshness)
{
    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the tenant-scoped conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = RequireNonNull(ConversationId, nameof(ConversationId));

    /// <summary>
    /// Gets the stable message identity.
    /// </summary>
    public MessageId MessageId { get; } = RequireNonNull(MessageId, nameof(MessageId));

    /// <summary>
    /// Gets the stable Party reference for the author.
    /// </summary>
    public PartyId AuthorPartyId { get; } = RequireNonNull(AuthorPartyId, nameof(AuthorPartyId));

    /// <summary>
    /// Gets the public message creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = ValidateTimestamp(CreatedAt);

    /// <summary>
    /// Gets the freshness and trust state.
    /// </summary>
    public ProjectionFreshness Freshness { get; } = RequireNonNull(Freshness, nameof(Freshness));

    private static T RequireNonNull<T>(T value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset value)
    {
        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.");
        }

        if (value.Year < 2000 || value.Year > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must fall within the plausible business range (year 2000-9999).");
        }

        return value;
    }
}
