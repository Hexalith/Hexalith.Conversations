using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.DomainService;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Explicit local source-proof enrollment; installation and native guard activation remain separately required.</summary>
public static class ConversationResolutionNativeProofRegistration
{
    private static readonly DomainProjectionOwnerProofRegistration Registration = new(
        ConversationProjectionHandler.ConversationDomain,
        ConversationAsyncProjectionHandler.ConversationReadModelProjectionType,
        new(1, 1, 1, "full-replay-v1", "projection-owner-proof/v1", null, null),
        new(ConversationProjectionReadModelKeys.StateStoreName, "resolution-native-selection")
        {
            SelectionKey = request => ConversationProjectionReadModelKeys.ResolutionSelectionKey(new TenantId(request.TenantId),
                new ConversationId(request.AggregateId)),
        })
    {
        // The legacy seam returns a gateway-owned opaque response and has no persisted owner writes.
        // Direct rich writers invalidate the outer resolution proof before writing. Named/rebuild/recovery
        // mutations remain under the same ConversationAsyncProjectionHandler and SDK batch protocols.
        LegacyCoexistence = new(typeof(ConversationAsyncProjectionHandler), typeof(ConversationProjectionHandler)),
    };

    /// <summary>Gets the registered SDK binding for producer-side selected-owner receipt verification.</summary>
    internal static ReadModelOwnerProofBinding OwnerProofBinding => Registration.Binding;

    /// <summary>Opts in to exact source handler coverage without activating or qualifying any native source guard.</summary>
    public static IServiceCollection AddConversationResolutionNativeProof(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!services.Any(descriptor => ReferenceEquals(descriptor.ImplementationInstance, Registration)))
            services.AddSingleton(Registration);
        return services;
    }
}
