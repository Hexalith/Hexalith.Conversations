// <copyright file="IConversationOnboardingDiagnosticSignals.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Hydration;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Reports the content-safe availability of the audit sink for onboarding diagnostics without exposing audit content.
/// </summary>
public interface IConversationAuditAvailabilitySignal
{
    /// <summary>
    /// Probes whether audit recording is currently available for the tenant.
    /// </summary>
    /// <param name="tenantId">The trusted tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The internal audit precondition status. <see cref="ConversationGovernanceAuditStatus.Succeeded"/> means available.</returns>
    ValueTask<ConversationGovernanceAuditStatus> GetAuditAvailabilityAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reports the content-safe availability of the Parties participant directory for onboarding diagnostics.
/// </summary>
public interface IParticipantDirectoryAvailabilitySignal
{
    /// <summary>
    /// Probes whether participant Party validation is currently available for the tenant.
    /// </summary>
    /// <param name="tenantId">The trusted tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The content-safe directory availability status.</returns>
    ValueTask<ParticipantDirectoryValidationStatus> GetDirectoryAvailabilityAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reports whether required provider configuration is present for onboarding diagnostics.
/// </summary>
/// <remarks>
/// The signal reports only a bounded boolean. Implementations must not surface provider payloads,
/// prompts, responses, session references, or secret values.
/// </remarks>
public interface IConversationProviderConfigurationSignal
{
    /// <summary>
    /// Probes whether required provider configuration is present for the tenant.
    /// </summary>
    /// <param name="tenantId">The trusted tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when required configuration is present; otherwise <c>false</c>.</returns>
    ValueTask<bool> IsProviderConfigurationPresentAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}
