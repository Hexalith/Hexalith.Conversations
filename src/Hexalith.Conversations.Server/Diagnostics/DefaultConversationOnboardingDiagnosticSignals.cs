// <copyright file="DefaultConversationOnboardingDiagnosticSignals.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Hydration;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Default fail-closed audit availability signal that reports audit unavailable until a real signal is wired.
/// </summary>
/// <param name="logger">The signal logger.</param>
public sealed class DefaultConversationAuditAvailabilitySignal(
    ILogger<DefaultConversationAuditAvailabilitySignal> logger)
    : IConversationAuditAvailabilitySignal
{
    private readonly ILogger<DefaultConversationAuditAvailabilitySignal> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private int _warned;

    /// <inheritdoc />
    public ValueTask<ConversationGovernanceAuditStatus> GetAuditAvailabilityAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        WarnOnce();
        return ValueTask.FromResult(ConversationGovernanceAuditStatus.AuditUnavailable);
    }

    private void WarnOnce()
    {
        if (Interlocked.Exchange(ref _warned, 1) == 0)
        {
            _logger.LogWarning(
                "No audit availability signal is registered for onboarding diagnostics; reporting audit unavailable (fail-closed).");
        }
    }
}

/// <summary>
/// Default fail-closed participant directory availability signal that reports unavailable until a real signal is wired.
/// </summary>
/// <param name="logger">The signal logger.</param>
public sealed class DefaultParticipantDirectoryAvailabilitySignal(
    ILogger<DefaultParticipantDirectoryAvailabilitySignal> logger)
    : IParticipantDirectoryAvailabilitySignal
{
    private readonly ILogger<DefaultParticipantDirectoryAvailabilitySignal> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private int _warned;

    /// <inheritdoc />
    public ValueTask<ParticipantDirectoryValidationStatus> GetDirectoryAvailabilityAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        WarnOnce();
        return ValueTask.FromResult(ParticipantDirectoryValidationStatus.Unavailable);
    }

    private void WarnOnce()
    {
        if (Interlocked.Exchange(ref _warned, 1) == 0)
        {
            _logger.LogWarning(
                "No participant directory availability signal is registered for onboarding diagnostics; reporting unavailable (fail-closed).");
        }
    }
}

/// <summary>
/// Default fail-closed provider configuration signal that reports missing configuration until a real signal is wired.
/// </summary>
/// <param name="logger">The signal logger.</param>
public sealed class DefaultConversationProviderConfigurationSignal(
    ILogger<DefaultConversationProviderConfigurationSignal> logger)
    : IConversationProviderConfigurationSignal
{
    private readonly ILogger<DefaultConversationProviderConfigurationSignal> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private int _warned;

    /// <inheritdoc />
    public ValueTask<bool> IsProviderConfigurationPresentAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        WarnOnce();
        return ValueTask.FromResult(false);
    }

    private void WarnOnce()
    {
        if (Interlocked.Exchange(ref _warned, 1) == 0)
        {
            _logger.LogWarning(
                "No provider configuration signal is registered for onboarding diagnostics; reporting configuration missing (fail-closed).");
        }
    }
}
