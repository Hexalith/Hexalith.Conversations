namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>The actual detail-query fields carried in the SDK query envelope payload.</summary>
/// <param name="ConversationId">The exact conversation identity, or the envelope identity when omitted.</param>
internal sealed record GetConversationPayload(string? ConversationId = null);
