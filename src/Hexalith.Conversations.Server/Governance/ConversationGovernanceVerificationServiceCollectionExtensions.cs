// <copyright file="ConversationGovernanceVerificationServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Registers governance verification services without adding an execution endpoint or write authority.
/// </summary>
public static class ConversationGovernanceVerificationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the focused governance verification service and its local proof helpers.
    /// </summary>
    public static IServiceCollection AddConversationGovernanceVerification(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IConversationTemporalEventSource, UnavailableConversationTemporalEventSource>();
        services.TryAddSingleton<ConversationProjectionMaterializer>();
        services.TryAddSingleton<ConversationProjectionRebuildVerifier>();
        services.TryAddScoped<ConversationProjectionReadService>();
        services.AddScoped<ConversationGovernanceVerificationService>();
        return services;
    }
}
