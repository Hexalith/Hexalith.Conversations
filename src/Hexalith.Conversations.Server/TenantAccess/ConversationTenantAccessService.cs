// <copyright file="ConversationTenantAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Checks Conversations tenant access from the local Tenants projection.
/// </summary>
/// <param name="projectionStore">The local tenant projection store.</param>
/// <param name="projectionSignal">The Conversations-owned projection signal for freshness, gap, rollback, and poisoning detection.</param>
/// <param name="logger">The service logger.</param>
public sealed class ConversationTenantAccessService(
    ITenantProjectionStore projectionStore,
    IConversationTenantProjectionSignal projectionSignal,
    ILogger<ConversationTenantAccessService> logger)
    : IConversationTenantAccessService
{
    private readonly ILogger<ConversationTenantAccessService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ITenantProjectionStore _projectionStore =
        projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    private readonly IConversationTenantProjectionSignal _projectionSignal =
        projectionSignal ?? throw new ArgumentNullException(nameof(projectionSignal));

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

        if (!Enum.IsDefined(typeof(ConversationTenantAccessRequirement), requirement))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement,
                "The tenant access requirement value is outside the closed-world set.");
        }

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

        if (!TryValidateCallerPrincipalId(callerPrincipalId))
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
            callerPrincipalId!,
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
            _logger.LogTrace(
                ex,
                "Tenant access projection lookup failure detail. Requirement={Requirement}",
                requirement);

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
            callerPrincipalId!,
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

        // Closed-world canonicalization: reject trim drift, control characters, common
        // delimiters that hint at prefixed identity, and unicode normalization variance.
        // Identifiers passing this check are byte-identical to their canonical NFC form,
        // contain only printable non-whitespace characters, and do not embed delimiter
        // characters reserved for upstream namespacing schemes.
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || !string.Equals(value, value.Normalize(NormalizationForm.FormC), StringComparison.Ordinal))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
            {
                return false;
            }

            if (c is ':' or '/' or '\\' or '|' or '#' or '?' or '&' or '%' or ',' or ';' or '<' or '>' or '"' or '\'')
            {
                return false;
            }

            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Format
                or UnicodeCategory.Surrogate
                or UnicodeCategory.PrivateUse
                or UnicodeCategory.OtherNotAssigned)
            {
                return false;
            }
        }

        canonical = value;
        return true;
    }

    private static bool TryValidateCallerPrincipalId(string? callerPrincipalId)
    {
        if (string.IsNullOrWhiteSpace(callerPrincipalId))
        {
            return false;
        }

        // The boundary contract (see IConversationTenantAccessService) requires the auth
        // middleware to hand a canonical caller principal id. Treat trim drift or embedded
        // control characters as MissingCaller (defense-in-depth) rather than normalizing
        // here, which would shadow upstream identity-provider drift bugs.
        if (!string.Equals(callerPrincipalId, callerPrincipalId.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        foreach (char c in callerPrincipalId)
        {
            if (char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }

    private async ValueTask<ConversationTenantAccessDecision?> CheckProjectionSignalAsync(
        ConversationTenantAccessRequirement requirement,
        TenantId tenantId,
        string canonicalTenantId,
        string callerPrincipalId,
        CancellationToken cancellationToken)
    {
        ConversationTenantProjectionHealth? health;
        try
        {
            health = await _projectionSignal
                .GetProjectionHealthAsync(canonicalTenantId, cancellationToken)
                .ConfigureAwait(false);
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
            _logger.LogTrace(
                ex,
                "Tenant access projection health lookup failure detail. Requirement={Requirement}",
                requirement);

            return Denied(
                requirement,
                tenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantAccessUnavailable,
                isRetryable: true);
        }

        if (health is null)
        {
            _logger.LogError(
                "Tenant access projection health signal returned a null record; failing closed. Requirement={Requirement}",
                requirement);

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

        // Snapshot the membership dictionary into a local Ordinal-keyed map so the
        // subsequent checks observe a consistent view and no broadened comparer (e.g.,
        // OrdinalIgnoreCase) from a non-default projection store can widen access.
        Dictionary<string, TenantRole> members;
        try
        {
            members = new Dictionary<string, TenantRole>(state.Members.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, TenantRole> entry in state.Members)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Key != entry.Key.Trim())
                {
                    return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.TenantProjectionPoisoned);
                }

                if (members.ContainsKey(entry.Key))
                {
                    return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.TenantProjectionPoisoned);
                }

                members.Add(entry.Key, entry.Value);
            }
        }
        catch (ArgumentException)
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

        if (!members.TryGetValue(callerPrincipalId, out TenantRole role))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.MissingMember);
        }

        // D2 hybrid: an unmapped role on the caller's own record denies only the caller.
        // Other members' unmapped roles do not deny this caller — closed-world denial scope
        // is narrowed so a partial Tenants SDK rollout cannot DoS valid members.
        if (!Enum.IsDefined(typeof(TenantRole), role))
        {
            return Denied(requirement, tenantId, callerPrincipalId, ConversationTenantAccessDenialReason.UnmappedRole);
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
                or ConversationTenantAccessRequirement.Admin
                or ConversationTenantAccessRequirement.Governance,
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
