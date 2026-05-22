// <copyright file="ConversationFileReferenceProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Carries a projected stable file reference without file content.
/// </summary>
/// <param name="fileId">The stable file identity.</param>
/// <param name="folderId">An optional stable folder identity.</param>
/// <param name="messageId">An optional stable message identity associated with the file.</param>
/// <param name="occurredAt">The projected time the file reference was attached, when available.</param>
public sealed record ConversationFileReferenceProjectionV1(
    FileId FileId,
    FolderId? FolderId = null,
    MessageId? MessageId = null,
    DateTimeOffset? OccurredAt = null)
{
    /// <summary>
    /// Gets the stable file identity.
    /// </summary>
    public FileId FileId { get; } = FileId ?? throw new ArgumentNullException(nameof(FileId));

    /// <summary>
    /// Gets the projected time the file reference was attached, when available.
    /// </summary>
    public DateTimeOffset? OccurredAt { get; } = ValidateTimestamp(OccurredAt);

    private static DateTimeOffset? ValidateTimestamp(DateTimeOffset? value)
    {
        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.");
        }

        return value;
    }
}
