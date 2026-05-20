// <copyright file="ConversationQueryServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Registers conversation query boundary services.
/// </summary>
public static class ConversationQueryServiceCollectionExtensions
{
    /// <summary>
    /// Adds conversation query handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationQueries(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ConversationQueryHandler>();
        return services;
    }
}
