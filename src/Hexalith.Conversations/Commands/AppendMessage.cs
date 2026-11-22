using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Commands;

/// <summary>Appends a human-authored message using the occurrence assigned by the command boundary.</summary>
/// <param name="PublicCommand">The complete adopter request.</param>
/// <param name="PostedAt">The posting instant assigned to this execution.</param>
/// <param name="EventId">The immutable event identity.</param>
public sealed record AppendMessage(AppendMessageCommand PublicCommand, DateTimeOffset PostedAt, string EventId);
