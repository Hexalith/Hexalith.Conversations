// <copyright file="ConversationAgentServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.DomainService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Explicit denied defaults; provider presence never establishes owner qualification.</summary>
public static class ConversationAgentServiceCollectionExtensions
{
    /// <summary>Registers current restricted source services and SDK admission.</summary>
    /// <param name="services">Host registrations.</param>
    /// <returns>The registrations.</returns>
    public static IServiceCollection AddConversationAgentServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IConversationAgentAuthority, UnavailableConversationAgentAuthority>();
        services.TryAddSingleton<IConversationDeletionApprovalVerifier, UnavailableConversationDeletionApprovalVerifier>();
        services.TryAddSingleton<IConversationDeletionReceiptVerifier, UnavailableConversationDeletionReceiptVerifier>();
        services.TryAddSingleton<IConversationCommandSourceVerifier, UnavailableConversationCommandSourceVerifier>();
        services.TryAddSingleton<IConversationTenantCatalogue, UnavailableConversationTenantCatalogue>();
        services.TryAddSingleton<IAuthoritativeEventStreamReader, UnavailableConversationAgentStreamReader>();
        services.TryAddScoped<ConversationAgentSourceReader>();
        services.TryAddScoped<ConversationAgentQueryService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IDomainServiceAdmissionStage, ConversationAgentAdmissionStage>());
        return services;
    }

    /// <summary>Selects an explicitly installed complete tenant namespace; independent namespace and stream providers remain mandatory.</summary>
    /// <param name="services">Host registrations.</param>
    /// <param name="registration">Exact installed namespace selection.</param>
    /// <returns>The registrations.</returns>
    public static IServiceCollection AddConversationTenantCatalogue(this IServiceCollection services,
        ConversationTenantCatalogueRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(registration.Scope);
        if (registration.Scope.Domain != "conversation")
        { throw new ArgumentException("Catalogue requires the Conversation source namespace.", nameof(registration)); }
        services.AddSingleton(registration);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<SourceNamespaceSnapshotReader>();
        services.Replace(ServiceDescriptor.Scoped<IConversationTenantCatalogue, EventStoreConversationTenantCatalogue>());
        return services;
    }
}
