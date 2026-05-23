// <copyright file="ConversationRejectionTelemetryServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Registers the rejection telemetry service.
/// </summary>
public static class ConversationRejectionTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IConversationRejectionTelemetry"/> as a singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationRejectionTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IConversationRejectionTelemetry, ConversationRejectionTelemetry>();
        return services;
    }
}
