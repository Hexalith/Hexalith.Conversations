// <copyright file="ConversationOnboardingDiagnosticsServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Registers CORE onboarding diagnostics services without adding an execution endpoint or write authority.
/// </summary>
public static class ConversationOnboardingDiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the read-only onboarding diagnostics service and its fail-closed default signals.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationOnboardingDiagnostics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Default signals fail closed so production deployments must wire trust-bearing signals
        // before relying on diagnostics; the absence is logged once per signal.
        services.TryAddScoped<IConversationAuditAvailabilitySignal, DefaultConversationAuditAvailabilitySignal>();
        services.TryAddScoped<IParticipantDirectoryAvailabilitySignal, DefaultParticipantDirectoryAvailabilitySignal>();
        services.TryAddScoped<IConversationProviderConfigurationSignal, DefaultConversationProviderConfigurationSignal>();
        services.AddScoped<ConversationOnboardingDiagnosticsService>();
        return services;
    }
}
