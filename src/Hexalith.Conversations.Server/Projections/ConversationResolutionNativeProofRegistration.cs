using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.DomainService;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Explicit source-proof descriptor for owner-controlled enrollment; coexisting legacy routes remain refused by the SDK.</summary>
public static class ConversationResolutionNativeProofRegistration
{
    /// <summary>Registers the exact named route and private selection key without activating or qualifying any source guard.</summary>
    public static IServiceCollection AddConversationResolutionNativeProof(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(new DomainProjectionOwnerProofRegistration(ConversationProjectionHandler.ConversationDomain,
            ConversationAsyncProjectionHandler.ConversationReadModelProjectionType,
            new(1, 1, 1, "full-replay-v1", "projection-owner-proof/v1", null, null),
            new(ConversationProjectionReadModelKeys.StateStoreName, "resolution-native-selection")
            { SelectionKey = request => ConversationProjectionReadModelKeys.ResolutionSelectionKey(new TenantId(request.TenantId),
                new ConversationId(request.AggregateId)) }));
        return services;
    }
}
