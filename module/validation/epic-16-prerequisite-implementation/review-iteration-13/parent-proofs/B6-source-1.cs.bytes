// <copyright file="TenantAccessEvaluator.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Hexalith.Commons.TenantAccess;

/// <summary>
/// Evaluates tenant access from a local Tenants projection without exposing module-specific vocabulary.
/// </summary>
public static class TenantAccessEvaluator
{
    /// <summary>
    /// Evaluates a tenant access request against local projection state.
    /// </summary>
    /// <typeparam name="TRequirement">The module requirement enum/type.</typeparam>
    /// <param name="requirement">The requested operation class.</param>
    /// <param name="tenantIds">All tenant-bearing inputs that must resolve to the same canonical value.</param>
    /// <param name="callerPrincipalId">The caller principal identifier.</param>
    /// <param name="stateStore">The neutral tenant access state store.</param>
    /// <param name="projectionHealth">The projection-health provider.</param>
    /// <param name="isRequirementDefined">Validates that <paramref name="requirement"/> is in the module closed-world set.</param>
    /// <param name="isStatusDefined">Validates that a status value is in the module closed-world set.</param>
    /// <param name="isStatusActive">Determines whether a status value is active.</param>
    /// <param name="isStatusDisabled">Determines whether a status value is disabled.</param>
    /// <param name="isRoleDefined">Validates that a role value is in the module closed-world set.</param>
    /// <param name="hasPermission">Maps a role value to the module requirement.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant access evaluation.</returns>
    public static async ValueTask<TenantAccessEvaluation<TRequirement>> EvaluateAsync<TRequirement>(
        TRequirement requirement,
        IEnumerable<string?> tenantIds,
        string? callerPrincipalId,
        ITenantAccessStateStore stateStore,
        ITenantAccessProjectionHealthProvider projectionHealth,
        Func<TRequirement, bool> isRequirementDefined,
        Func<int, bool> isStatusDefined,
        Func<int, bool> isStatusActive,
        Func<int, bool> isStatusDisabled,
        Func<int, bool> isRoleDefined,
        Func<int, TRequirement, bool> hasPermission,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(projectionHealth);
        ArgumentNullException.ThrowIfNull(isRequirementDefined);
        ArgumentNullException.ThrowIfNull(isStatusDefined);
        ArgumentNullException.ThrowIfNull(isStatusActive);
        ArgumentNullException.ThrowIfNull(isStatusDisabled);
        ArgumentNullException.ThrowIfNull(isRoleDefined);
        ArgumentNullException.ThrowIfNull(hasPermission);
        ArgumentNullException.ThrowIfNull(logger);

        TenantAccessEvaluationRules<TRequirement> rules = new(
            isRequirementDefined,
            isStatusDefined,
            isStatusActive,
            isStatusDisabled,
            isRoleDefined,
            hasPermission);

        cancellationToken.ThrowIfCancellationRequested();

        if (!rules.IsRequirementDefined(requirement))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement,
                "The tenant access requirement value is outside the closed-world set.");
        }

        TenantResolution tenantResolution = ResolveTenant(tenantIds);
        if (!tenantResolution.IsValid)
        {
            return Denied(requirement, null, callerPrincipalId, tenantResolution.DenialKind, logger);
        }

        if (!TryValidateCallerPrincipalId(callerPrincipalId))
        {
            return Denied(requirement, tenantResolution.CanonicalValue, callerPrincipalId, TenantAccessDenialKind.MissingCaller, logger);
        }

        TenantAccessEvaluation<TRequirement>? healthDenial = await CheckProjectionHealthAsync(
            requirement,
            tenantResolution.CanonicalValue!,
            callerPrincipalId!,
            projectionHealth,
            logger,
            cancellationToken).ConfigureAwait(false);

        if (healthDenial is not null)
        {
            return healthDenial;
        }

        TenantAccessState? state;
        try
        {
            state = await stateStore
                .GetAsync(tenantResolution.CanonicalValue!, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Tenant access projection lookup failed; failing closed. Requirement={Requirement}, FailureType={FailureType}",
                requirement,
                ex.GetType().Name);
            logger.LogTrace(ex, "Tenant access projection lookup failure detail. Requirement={Requirement}", requirement);

            return Denied(
                requirement,
                tenantResolution.CanonicalValue,
                callerPrincipalId,
                TenantAccessDenialKind.TenantAccessUnavailable,
                logger,
                isRetryable: true);
        }

        return DecideFromProjectionState(
            new TenantAccessEvaluationRequest<TRequirement>(
                requirement,
                tenantResolution.CanonicalValue!,
                callerPrincipalId!),
            state,
            rules,
            logger);
    }

    private static TenantResolution ResolveTenant(IEnumerable<string?> tenantIds)
    {
        string? canonical = null;
        bool sawTenant = false;

        foreach (string? tenantId in tenantIds)
        {
            if (tenantId is null)
            {
                continue;
            }

            sawTenant = true;
            if (!TryValidateTenantValue(tenantId, out string? value))
            {
                return TenantResolution.Invalid(TenantAccessDenialKind.MalformedTenant);
            }

            if (canonical is null)
            {
                canonical = value;
                continue;
            }

            if (!string.Equals(canonical, value, StringComparison.Ordinal))
            {
                return TenantResolution.Invalid(TenantAccessDenialKind.TenantMismatch);
            }
        }

        return sawTenant
            ? TenantResolution.Valid(canonical!)
            : TenantResolution.Invalid(TenantAccessDenialKind.MissingTenant);
    }

    private static bool TryValidateTenantValue(string? value, out string? canonical)
    {
        canonical = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

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

    private static async ValueTask<TenantAccessEvaluation<TRequirement>?> CheckProjectionHealthAsync<TRequirement>(
        TRequirement requirement,
        string canonicalTenantId,
        string callerPrincipalId,
        ITenantAccessProjectionHealthProvider projectionHealth,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        TenantAccessProjectionHealth? health;
        try
        {
            health = await projectionHealth.GetProjectionHealthAsync(canonicalTenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Tenant access projection health lookup failed; failing closed. Requirement={Requirement}, FailureType={FailureType}",
                requirement,
                ex.GetType().Name);
            logger.LogTrace(ex, "Tenant access projection health lookup failure detail. Requirement={Requirement}", requirement);

            return Denied(
                requirement,
                canonicalTenantId,
                callerPrincipalId,
                TenantAccessDenialKind.TenantAccessUnavailable,
                logger,
                isRetryable: true);
        }

        if (health is null)
        {
            logger.LogError(
                "Tenant access projection health signal returned a null record; failing closed. Requirement={Requirement}",
                requirement);

            return Denied(
                requirement,
                canonicalTenantId,
                callerPrincipalId,
                TenantAccessDenialKind.TenantAccessUnavailable,
                logger,
                isRetryable: true);
        }

        if (health.IsPoisoned)
        {
            return Denied(
                requirement,
                canonicalTenantId,
                callerPrincipalId,
                TenantAccessDenialKind.TenantProjectionPoisoned,
                logger,
                projection: ToCheckpoint(health));
        }

        if (health.HasRollback)
        {
            return Denied(
                requirement,
                canonicalTenantId,
                callerPrincipalId,
                TenantAccessDenialKind.TenantAccessRolledBack,
                logger,
                isRetryable: true,
                projection: ToCheckpoint(health));
        }

        if (health.HasGap)
        {
            return Denied(
                requirement,
                canonicalTenantId,
                callerPrincipalId,
                TenantAccessDenialKind.TenantAccessGapDetected,
                logger,
                isRetryable: true,
                projection: ToCheckpoint(health));
        }

        return health.IsStale
            ? Denied(
                requirement,
                canonicalTenantId,
                callerPrincipalId,
                TenantAccessDenialKind.TenantAccessStale,
                logger,
                isRetryable: true,
                projection: ToCheckpoint(health))
            : null;
    }

    private static TenantAccessEvaluation<TRequirement> DecideFromProjectionState<TRequirement>(
        TenantAccessEvaluationRequest<TRequirement> request,
        TenantAccessState? state,
        TenantAccessEvaluationRules<TRequirement> rules,
        ILogger logger)
    {
        TenantAccessDenialKind? projectionDenial = GetProjectionDenialKind(request.TenantId, state);
        if (projectionDenial is not null)
        {
            return Denied(request, projectionDenial.Value, logger);
        }

        if (!TryCreateMemberMap(state!.Members!, out Dictionary<string, int> members))
        {
            return Denied(request, TenantAccessDenialKind.TenantProjectionPoisoned, logger);
        }

        TenantAccessDenialKind? statusDenial = GetStatusDenialKind(state.Status, rules);
        if (statusDenial is not null)
        {
            return Denied(request, statusDenial.Value, logger);
        }

        if (!members.TryGetValue(request.CallerPrincipalId, out int role))
        {
            return Denied(request, TenantAccessDenialKind.MissingMember, logger);
        }

        if (!rules.IsRoleDefined(role))
        {
            return Denied(request, TenantAccessDenialKind.UnmappedRole, logger);
        }

        return rules.HasPermission(role, request.Requirement)
            ? Allowed(request)
            : Denied(request, TenantAccessDenialKind.InsufficientRole, logger);
    }

    private static TenantAccessDenialKind? GetProjectionDenialKind(
        string canonicalTenantId,
        TenantAccessState? state)
    {
        if (state is null)
        {
            return TenantAccessDenialKind.UnknownTenant;
        }

        if (!TryValidateTenantValue(state.TenantId, out string? projectionTenantId))
        {
            return TenantAccessDenialKind.MalformedProjection;
        }

        if (!string.Equals(canonicalTenantId, projectionTenantId, StringComparison.Ordinal))
        {
            return TenantAccessDenialKind.TenantMismatch;
        }

        return state.Members is null
            ? TenantAccessDenialKind.MalformedProjection
            : null;
    }

    private static bool TryCreateMemberMap(
        IReadOnlyDictionary<string, int> stateMembers,
        out Dictionary<string, int> members)
    {
        members = new Dictionary<string, int>(stateMembers.Count, StringComparer.Ordinal);

        try
        {
            foreach (KeyValuePair<string, int> entry in stateMembers)
            {
                if (IsUnsafeMemberKey(entry.Key) || members.ContainsKey(entry.Key))
                {
                    return false;
                }

                members.Add(entry.Key, entry.Value);
            }
        }
        catch (ArgumentException)
        {
            members.Clear();
            return false;
        }

        return true;
    }

    private static bool IsUnsafeMemberKey(string key)
        => string.IsNullOrWhiteSpace(key) || key != key.Trim();

    private static TenantAccessDenialKind? GetStatusDenialKind<TRequirement>(
        int status,
        TenantAccessEvaluationRules<TRequirement> rules)
    {
        if (!rules.IsStatusDefined(status))
        {
            return TenantAccessDenialKind.UnmappedStatus;
        }

        if (rules.IsStatusDisabled(status))
        {
            return TenantAccessDenialKind.TenantDisabled;
        }

        return rules.IsStatusActive(status)
            ? null
            : TenantAccessDenialKind.UnmappedStatus;
    }

    private static TenantAccessEvaluation<TRequirement> Allowed<TRequirement>(
        TenantAccessEvaluationRequest<TRequirement> request)
        => new(
            true,
            request.Requirement,
            request.TenantId,
            request.CallerPrincipalId,
            TenantAccessDenialKind.None,
            false);

    private static TenantAccessEvaluation<TRequirement> Denied<TRequirement>(
        TenantAccessEvaluationRequest<TRequirement> request,
        TenantAccessDenialKind reason,
        ILogger logger)
        => Denied(
            request.Requirement,
            request.TenantId,
            request.CallerPrincipalId,
            reason,
            logger);

    private static TenantAccessProjectionCheckpoint ToCheckpoint(TenantAccessProjectionHealth health)
        => new(health.Version, health.Watermark);

    private sealed record TenantAccessEvaluationRequest<TRequirement>(
        TRequirement Requirement,
        string TenantId,
        string CallerPrincipalId);

    private sealed record TenantAccessEvaluationRules<TRequirement>(
        Func<TRequirement, bool> IsRequirementDefined,
        Func<int, bool> IsStatusDefined,
        Func<int, bool> IsStatusActive,
        Func<int, bool> IsStatusDisabled,
        Func<int, bool> IsRoleDefined,
        Func<int, TRequirement, bool> HasPermission);

    private sealed record TenantAccessProjectionCheckpoint(long? Version, string? Watermark);

    private static TenantAccessEvaluation<TRequirement> Denied<TRequirement>(
        TRequirement requirement,
        string? tenantId,
        string? callerPrincipalId,
        TenantAccessDenialKind reason,
        ILogger logger,
        bool isRetryable = false,
        TenantAccessProjectionCheckpoint? projection = null)
    {
        logger.LogInformation(
            "Tenant access denied. Requirement={Requirement}, Reason={Reason}",
            requirement,
            reason);

        return new TenantAccessEvaluation<TRequirement>(
            false,
            requirement,
            tenantId,
            callerPrincipalId,
            reason,
            isRetryable,
            projection?.Version,
            projection?.Watermark);
    }

    private sealed record TenantResolution(bool IsValid, string? CanonicalValue, TenantAccessDenialKind DenialKind)
    {
        public static TenantResolution Valid(string canonicalValue)
            => new(true, canonicalValue, TenantAccessDenialKind.None);

        public static TenantResolution Invalid(TenantAccessDenialKind reason)
            => new(false, null, reason);
    }
}
