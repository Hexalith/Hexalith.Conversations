using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.MinimalModule.Contracts;

/// <summary>Command that creates a conversation.</summary>
/// <param name="ConversationId">The conversation aggregate identifier.</param>
/// <param name="Title">The conversation title.</param>
public sealed record CreateConversation(string ConversationId, string Title) : ICommandContract
{
    /// <inheritdoc/>
    public static string Domain => "conversation";

    /// <inheritdoc/>
    public static string CommandType => "create-conversation";

    /// <inheritdoc/>
    public string AggregateId => ConversationId;
}
