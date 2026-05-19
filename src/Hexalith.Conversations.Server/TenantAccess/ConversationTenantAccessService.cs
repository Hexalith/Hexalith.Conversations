// <copyright file="ConversationTenantAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Checks Conversations tenant access from the local Tenants projection.
/// </summary>
/// <param name="projectionStore">The local tenant projection store.</param>
/// <param name="logger">The service logger.</param>
public sealed class ConversationTenantAccessService(
    ITenantProjectionStore projectionStore,
    ILogger<ConversationTenantAccessService> logger)
    : IConversationTenantAccessService
{
    private readonly ILogger<ConversationTenantAccessService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ITenantProjectionStore _projectionStore =
        projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    /// <inheritdoc />
    public async ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
        ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        TenantId? routeTenantId = null,
        TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null,
        TenantId? projectionTenantId = null,
        TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TenantResolution tenantResolution = ResolveTenant(
            trustedTenantId,
            routeTenantId,
            commandTenantId,
            aggregateTenantId,
            projectionTenantId,
            idempotencyTenantId);

        if (!tenantResolution.IsValid)
        {
            return Denied(requirement, trustedTenantId, callerPrincipalId, tenantResolution.DenialReason);
        }

        if (string.IsNullOrWhiteSpace(callerPrincipalId))
        {
            return Denied(
                requirement,
                tenantResolution.TenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.MissingCaller);
        }

        ConversationTenantAccessDecision? signalDenial = await CheckProjectionSignalAsync(
            requirement,
            tenantResolution.TenantId!,
            tenantResolution.CanonicalValue!,
            callerPrincipalId,
            cancellationToken).ConfigureAwait(false);

        if (signalDenial is not null)
        {
            return signalDenial;
        }

        TenantLocalState? state;
        try
        {
            state = await _projectionStore
                .GetAsync(tenantResolution.CanonicalValue!, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Tenant access projection lookup failed; failing closed. Requirement={Requirement}, FailureType={FailureType}",
                requirement,
                ex.GetType().Name);

            return Denied(
                requirement,
                tenantResolution.TenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantAccessUnavailable,
                isRetryable: true);
        }

        return DecideFromProjectionState(
            requirement,
            tenantResolution.TenantId!,
            tenantResolution.CanonicalValue!,
            callerPrincipalId,
            state);
    }

    private static TenantResolution ResolveTenant(params TenantId?[] tenantIds)
    {
        string? canonical = null;
        TenantId? canonicalTenantId = null;
        bool sawTenant = false;

        foreach (TenantId? tenantId in tenantIds)
        {
            if (tenantId is null)
            {
                continue;
            }

            sawTenant = true;
            if (!TryValidateTenantValue(tenantId.Value, out string? value))
            {
                return TenantResolution.Invalid(ConversationTenantAccessDenialReason.MalformedTenant);
            }

            if (canonical is null)
            {
                canonical = value;
                canonicalTenantId = tenantId;
                continue;
            }

            if (!string.Equals(canonical, value, StringComparison.Ordinal))
            {
                return TenantResolution.Invalid(ConversationTenantAccessDenialReason.TenantMismatch);
            }
        }

        return sawTenant
            ? TenantResolution.Valid(canonicalTenantId!, canonical!)
            : TenantResolution.Invalid(ConversationTenantAccessDenialReason.MissingTenant);
    }

    private static bool TryValidateTenantValue(string? value, out string? canonical)
    {
        canonical = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || value.Contains(':', StringComparison.Ordinal)
            || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        canonical = value;
        return true;
    }

    private async ValueTask<ConversationTenantAccessDecision?> CheckProjectionSignalAsync(
        ConversationTenantAccessRequirement requirement,
        TenantId tenantId,
        string canonicalTenantId,
        string callerPrincipalId,
        CancellationToken cancellationToken)
    {
        if (_projectionStore is not IConversationTenantProjectionSignal signal)
        {
            return null;
        }

        ConversationTenantProjectionHealth health;
        try
        {
            health = await signal.GetProjectionHealthAsync(canonicalTenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Tenant access projection health lookup failed; failing closed. Requirement={Requirement}, FailureType={FailureType}",
                requirement,
                ex.GetType().Name);

            return Denied(
                requirement,
                tenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantAccessUnavailable,
                isRetryable: true);
        }

        if (health.IsPoisoned)
        {
            return Denied(
                requirement,
                tenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantProjectionPoisoned,
                projectionVersion: health.Version,
                projectionWatermark: health.Watermark);
        }

        if (health.HasRollback)
        {
            return Denied(
                requirement,
                tenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantAccessRolledBack,
                isRetryable: true,
                projectionVersion: health.Version,
                projectionWatermark: health.Watermark);
        }

        if (health.HasGap)
        {
            return Denied(
                requirement,
                tenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantAccessGapDetected,
                isRetryable: true,
                projectionVersion: health.Version,
                projectionWatermark: health.Watermark);
        }

        return health.IsStale
            ? Denied(
                requirement,
                tenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantAccessStale,
                isRetryable: true,
                projectionVersion: health.Version,
                projectionWatermark: health.Watermark)
            : null;
    }

    private ConversationTenantAccessDecision DecideFromProjectionState(
        ConversationTenantAccessRequirement requirement,
        TenantId tenantId,
        string canonicalTenantId,
        string callerPrincipalId,
        TenantLocalState? state)
    {
        if (state is null)
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.UnknownTenant);
        }

        if (!TryValidateTenantValue(state.TenantId, out string? projectionTenantId))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.MalformedProjection);
        }

        if (!string.Equals(canonicalTenantId, projectionTenantId, StringComparison.Ordinal))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.TenantMismatch);
        }

        if (state.Members is null)
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.MalformedProjection);
        }

        if (state.Members.Keys.Any(memberId => string.IsNullOrWhiteSpace(memberId) || memberId != memberId.Trim()))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.TenantProjectionPoisoned);
        }

        if (!Enum.IsDefined(typeof(TenantStatus), state.Status))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.UnmappedStatus);
        }

        if (state.Status == TenantStatus.Disabled)
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.TenantDisabled);
        }

        if (state.Status != TenantStatus.Active)
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.UnmappedStatus);
        }

        if (state.Members.Values.Any(role => !Enum.IsDefined(typeof(TenantRole), role)))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.UnmappedRole);
        }

        if (!state.Members.TryGetValue(callerPrincipalId, out TenantRole role))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.MissingMember);
        }

        return HasPermission(role, requirement)
            ? ConversationTenantAccessDecision.Allowed(requirement, tenantId, callerPrincipalId)
            : Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.InsufficientRole);
    }

    private static bool HasPermission(TenantRole role, ConversationTenantAccessRequirement requirement)
        => role switch
        {
            TenantRole.TenantReader => requirement == ConversationTenantAccessRequirement.Read,
            TenantRole.TenantContributor => requirement is ConversationTenantAccessRequirement.Read or ConversationTenantAccessRequirement.Write,
            TenantRole.TenantOwner => requirement is ConversationTenantAccessRequirement.Read
                or ConversationTenantAccessRequirement.Write
                or ConversationTenantAccessRequirement.Admin,
            _ => false,
        };

    private ConversationTenantAccessDecision Denied(
        ConversationTenantAccessRequirement requirement,
        TenantId? tenantId,
        string? callerPrincipalId,
        ConversationTenantAccessDenialReason reason,
        bool isRetryable = false,
        long? projectionVersion = null,
        string? projectionWatermark = null)
    {
        _logger.LogInformation(
            "Tenant access denied. Requirement={Requirement}, Reason={Reason}",
            requirement,
            reason);

        return ConversationTenantAccessDecision.Denied(
            requirement,
            tenantId,
            callerPrincipalId,
            reason,
            isRetryable,
            projectionVersion,
            projectionWatermark);
    }

    private sealed record TenantResolution(
        bool IsValid,
        TenantId? TenantId,
        string? CanonicalValue,
        ConversationTenantAccessDenialReason DenialReason)
    {
        public static TenantResolution Valid(TenantId tenantId, string canonicalValue)
            => new(true, tenantId, canonicalValue, ConversationTenantAccessDenialReason.None);

        public static TenantResolution Invalid(ConversationTenantAccessDenialReason reason)
            => new(false, null, null, reason);
    }
}
