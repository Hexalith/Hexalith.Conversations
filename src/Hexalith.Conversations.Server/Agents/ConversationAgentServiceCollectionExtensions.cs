// <copyright file="ConversationAgentServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Streams;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Server.Api;
using Hexalith.EventStore.Contracts.Streams;
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

    /// <summary>Composes the restricted source, worker and ordered delivery adapters for an explicitly installed runtime.</summary>
    /// <remarks>External authority, source, index, receiver and gateway providers must be registered independently.
    /// This method neither creates credentials nor establishes owner qualification.</remarks>
    /// <param name="services">Host registrations.</param>
    /// <param name="catalogue">Exact installed source namespace.</param>
    /// <param name="worker">Exact enrolled worker reference.</param>
    /// <returns>The registrations.</returns>
    public static IServiceCollection AddConversationDeletionRuntime(this IServiceCollection services,
        ConversationTenantCatalogueRegistration catalogue, ConversationDeletionWorkerRegistration worker)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(worker);
        if (worker.TenantId is null || catalogue.Scope is null || catalogue.Scope.Domain != "conversation"
            || catalogue.Scope.Tenant != worker.TenantId.Value || catalogue.Scope.FeedName != "approved-deletion-v1"
            || string.IsNullOrWhiteSpace(worker.AuthenticatedPrincipalId) || worker.ServicePartyId is null)
        {
            throw new ArgumentException("The exact Conversation namespace and enrolled worker must agree.");
        }
        Type[] requiredProviders = [typeof(IConversationAgentAuthority), typeof(IConversationDeletionApprovalVerifier),
            typeof(IConversationCommandSourceVerifier), typeof(IAuthoritativeEventStreamReader),
            typeof(ISourcePublicationNamespaceSource), typeof(ISourcePublicationIndexStore),
            typeof(IConversationDeletionReceiver), typeof(IConversationClient)];
        foreach (Type provider in requiredProviders)
        {
            ServiceDescriptor? selected = services.LastOrDefault(descriptor => descriptor.ServiceType == provider);
            if (selected is null || selected.ImplementationType is { } implementation && (
                implementation == typeof(UnavailableConversationAgentAuthority)
                || implementation == typeof(UnavailableConversationDeletionApprovalVerifier)
                || implementation == typeof(UnavailableConversationCommandSourceVerifier)
                || implementation == typeof(UnavailableConversationAgentStreamReader)))
            {
                throw new InvalidOperationException($"Authenticated Conversation runtime provider is missing: {provider.Name}.");
            }
        }
        services.AddConversationAgentServices();
        services.AddConversationTenantCatalogue(catalogue);
        services.AddSingleton(worker);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ConfiguredConversationDeletionWorker>();
        services.Replace(ServiceDescriptor.Scoped<IConversationDeletionReceiptVerifier, ConfiguredConversationDeletionReceiptVerifier>());
        services.AddScoped<ConversationDeletionDeliveryPump>();
        services.AddScoped<ISourcePublicationDelivery, ConversationDeletionPublicationDelivery>();
        services.AddScoped<ISourcePublicationProjector, ConversationDeletionPublicationProjector>();
        services.AddScoped<SourcePublicationFeed>();
        services.AddScoped<SourcePublicationDispatcher>();
        services.AddHostedService<ConversationDeletionDispatchWorker>();
        services.AddHttpContextAccessor();
        services.AddScoped<IConversationCommandApiHandler, ConversationAgentParticipantApiHandler>();
        return services;
    }
}
