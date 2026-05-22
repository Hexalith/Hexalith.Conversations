// <copyright file="ConversationBuyerAcceptanceDemoServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Registers the read-only buyer acceptance demo runner.
/// </summary>
public static class ConversationBuyerAcceptanceDemoServiceCollectionExtensions
{
    /// <summary>
    /// Adds the buyer acceptance demo runner without adding mutation endpoints or persistent evidence storage.
    /// </summary>
    public static IServiceCollection AddConversationBuyerAcceptanceDemo(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ConversationBuyerAcceptanceDemoService>();
        return services;
    }
}
