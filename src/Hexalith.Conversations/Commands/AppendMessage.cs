using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Commands;

/// <summary>Appends a human-authored message no earlier than the time the API assigned.</summary>
/// <param name="PublicCommand">The complete adopter request.</param>
/// <param name="PostedAt">The API-assigned lower bound; the event commits at the later of this value and the conversation's LastEventAt.</param>
/// <param name="EventId">The immutable event identity.</param>
public sealed record AppendMessage(AppendMessageCommand PublicCommand, DateTimeOffset PostedAt, string EventId);
