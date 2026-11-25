using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Commands;

/// <summary>Appends a human-authored message.</summary>
/// <param name="PublicCommand">The complete adopter request.</param>
/// <param name="PostedAt">The timestamp supplied by the API for admission. Domain execution assigns the event occurrence.</param>
/// <param name="EventId">The immutable event identity.</param>
public sealed record AppendMessage(AppendMessageCommand PublicCommand, DateTimeOffset PostedAt, string EventId);
