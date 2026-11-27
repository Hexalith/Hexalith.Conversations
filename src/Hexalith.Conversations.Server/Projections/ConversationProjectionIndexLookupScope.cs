// <copyright file="ConversationProjectionIndexLookupScope.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Propagates the active target conversation identifier across asynchronous store calls so
/// <see cref="ConversationProjectionIndexLookupConverter"/> can extract only the target entry from the
/// tenant index without deserializing unrelated summaries.
/// </summary>
internal static class ConversationProjectionIndexLookupScope
{
    private static readonly AsyncLocal<ConversationProjectionIndexLookupTarget?> CurrentTarget = new();

    /// <summary>Gets the active lookup target for the current asynchronous execution context.</summary>
    internal static ConversationProjectionIndexLookupTarget? Current => CurrentTarget.Value;

    /// <summary>Sets the active lookup target and returns the prior target for restoration.</summary>
    /// <param name="conversationId">The target conversation identifier value.</param>
    /// <returns>The previous target, if any.</returns>
    internal static ConversationProjectionIndexLookupTarget? Begin(string conversationId)
    {
        ConversationProjectionIndexLookupTarget? previous = CurrentTarget.Value;
        CurrentTarget.Value = new ConversationProjectionIndexLookupTarget(conversationId);
        return previous;
    }

    /// <summary>Restores the previously active lookup target.</summary>
    /// <param name="previous">The prior target returned by <see cref="Begin"/>.</param>
    internal static void End(ConversationProjectionIndexLookupTarget? previous)
        => CurrentTarget.Value = previous;
}
