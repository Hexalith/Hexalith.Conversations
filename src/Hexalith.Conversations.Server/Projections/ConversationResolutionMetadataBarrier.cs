namespace Hexalith.Conversations.Server.Projections;

/// <summary>A content-free native position observation that cannot assert currentness.</summary>
/// <param name="Position">The observed owner position.</param>
/// <param name="RequiresNewerGeneration">Whether an unfenced direct writer requires a strictly newer winner.</param>
/// <param name="DispatchId">The stable delivery identity.</param>
public sealed record ConversationResolutionMetadataBarrier(long Position, string DispatchId, bool RequiresNewerGeneration = false);
