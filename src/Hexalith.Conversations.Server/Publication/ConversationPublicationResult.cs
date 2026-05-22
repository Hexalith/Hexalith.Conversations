// <copyright file="ConversationPublicationResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Represents the result of mapping a persisted event to a public publication event.
/// </summary>
/// <param name="PublishedEvent">The public event when publication is allowed.</param>
/// <param name="Diagnostic">The safe diagnostic when publication is rejected.</param>
public sealed record ConversationPublicationResult(
    object? PublishedEvent,
    ConversationPublicationDiagnostic? Diagnostic)
{
    /// <summary>
    /// Gets a value indicating whether a public event is ready for publication.
    /// </summary>
    public bool IsPublished => PublishedEvent is not null;

    /// <summary>
    /// Creates a published result.
    /// </summary>
    /// <param name="publishedEvent">The public event.</param>
    /// <returns>The result.</returns>
    public static ConversationPublicationResult Published(object publishedEvent)
    {
        ArgumentNullException.ThrowIfNull(publishedEvent);
        return new(publishedEvent, null);
    }

    /// <summary>
    /// Creates a rejected result.
    /// </summary>
    /// <param name="diagnostic">The safe diagnostic.</param>
    /// <returns>The result.</returns>
    public static ConversationPublicationResult Rejected(ConversationPublicationDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new(null, diagnostic);
    }

    /// <summary>
    /// Gets the published event as the expected type.
    /// </summary>
    /// <typeparam name="T">The expected event type.</typeparam>
    /// <returns>The typed event.</returns>
    public T GetPublishedEvent<T>()
        where T : class
        => PublishedEvent as T ?? throw new InvalidOperationException($"Publication result does not contain {typeof(T).Name}.");
}
