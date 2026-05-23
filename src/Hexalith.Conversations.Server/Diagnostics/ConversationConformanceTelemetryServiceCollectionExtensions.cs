// <copyright file="ConversationConformanceTelemetryServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Registers the conformance telemetry service.
/// </summary>
public static class ConversationConformanceTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IConversationConformanceTelemetry"/> as a singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationConformanceTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IConversationConformanceTelemetry, ConversationConformanceTelemetry>();
        return services;
    }
}
