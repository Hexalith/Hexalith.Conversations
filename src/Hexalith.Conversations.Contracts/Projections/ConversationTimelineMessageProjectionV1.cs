// <copyright file="ConversationTimelineMessageProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Carries a projected conversation timeline message using stable IDs and visible text only.
/// </summary>
/// <param name="messageId">The stable message identity.</param>
/// <param name="authorPartyId">The stable Party reference for the author.</param>
/// <param name="text">The visible message text.</param>
/// <param name="createdAt">The public message creation timestamp.</param>
/// <param name="providerCorrelation">Optional safe provider correlation metadata.</param>
public sealed record ConversationTimelineMessageProjectionV1(
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    DateTimeOffset CreatedAt,
    ProviderCorrelationMetadata? ProviderCorrelation = null)
{
    /// <summary>
    /// Gets the stable message identity.
    /// </summary>
    public MessageId MessageId { get; } = MessageId ?? throw new ArgumentNullException(nameof(MessageId));

    /// <summary>
    /// Gets the stable Party reference for the author.
    /// </summary>
    public PartyId AuthorPartyId { get; } = AuthorPartyId ?? throw new ArgumentNullException(nameof(AuthorPartyId));

    /// <summary>
    /// Gets the visible message text.
    /// </summary>
    public string Text { get; } = ValidateText(Text);

    /// <summary>
    /// Gets the public message creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = ValidateTimestamp(CreatedAt);

    private static string ValidateText(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset value)
    {
        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.");
        }

        return value;
    }
}
