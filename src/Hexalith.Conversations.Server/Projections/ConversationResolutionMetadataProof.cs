namespace Hexalith.Conversations.Server.Projections;

/// <summary>Lasting generation state; retains the displaced completed generation for original generation recovery.</summary>
/// <param name="Barrier">A rejected or external writer observation without invented generation evidence.</param>
/// <param name="Current">The deterministically winning generation.</param>
/// <param name="PreviousCompleted">The displaced completed generation, if any.</param>
public sealed record ConversationResolutionMetadataProof(
    ConversationResolutionGeneration? Current, ConversationResolutionGeneration? PreviousCompleted = null,
    ConversationResolutionMetadataBarrier? Barrier = null);
