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
}
