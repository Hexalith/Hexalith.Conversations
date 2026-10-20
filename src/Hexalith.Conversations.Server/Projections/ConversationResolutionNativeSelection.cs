using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Identity;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Content-free selected output metadata embedded in the original committed metadata batch.</summary>
/// <param name="ProjectionVersion">The unmodified owner freshness version.</param>
/// <param name="ProjectionOwner">The exact source identity, actual head message and immutable subset generation.</param>
public sealed record ConversationResolutionNativeSelection(string ProjectionVersion, ProjectionOwnerSelection ProjectionOwner)
{
    /// <summary>Builds evidence only when the named producer supplied an actual persisted event identity.</summary>
    internal static ConversationResolutionNativeSelection? From(ConversationResolutionMetadataReadModel model, string batchId,
        bool rebuild = false)
    {
        try { _ = new AggregateIdentity(model.TenantId.Value, ConversationProjectionHandler.ConversationDomain, model.ConversationId.Value); }
        catch (ArgumentException) { return null; }
        return model.SourceHeadSequence is > 0 && !string.IsNullOrWhiteSpace(model.SourceHeadMessageId)
            ? new(model.Freshness.ProjectionCursor, new(1, model.TenantId.Value, ConversationProjectionHandler.ConversationDomain,
                model.ConversationId.Value, ConversationAsyncProjectionHandler.ConversationReadModelProjectionType,
                model.ConversationId.Value, model.Digest(), model.SourceHeadSequence.Value, model.SourceHeadMessageId,
                batchId, model.Freshness.ProjectionCursor) { ReceiptProjectionName = rebuild ? "rebuild" : null }) : null;
    }
}
