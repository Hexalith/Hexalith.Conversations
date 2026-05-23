// <copyright file="ConversationProjectionTelemetryServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Registers the projection telemetry service.
/// </summary>
public static class ConversationProjectionTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IConversationProjectionTelemetry"/> as a singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationProjectionTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IConversationProjectionTelemetry, ConversationProjectionTelemetry>();
        return services;
    }
}
