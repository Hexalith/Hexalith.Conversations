using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>Current native evidence transported through the SDK; this interface conveys no cached grant.</summary>
public interface IConversationResolutionNativeEvidence
{
    /// <summary>Reads the exact delegated actor's current native Tenant Read decision.</summary>
    Task<TenantReadAuthorityResult?> ReadTenantAsync(QueryEnvelope query, CancellationToken cancellationToken);
    /// <summary>Inspects the original selected immutable generation through the existing native guard.</summary>
    Task<ProjectionCurrentProof?> ReadProjectionAsync(ConversationResolutionMetadataReadModel metadata, CancellationToken cancellationToken);
}
