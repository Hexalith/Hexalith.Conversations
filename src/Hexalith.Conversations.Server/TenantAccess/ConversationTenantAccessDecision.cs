// <copyright file="ConversationTenantAccessDecision.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Represents an internal tenant-access decision.
/// </summary>
/// <param name="IsAllowed">A value indicating whether the request may touch protected state.</param>
/// <param name="Requirement">The requested operation class.</param>
/// <param name="TenantId">The canonical tenant id when available.</param>
/// <param name="CallerPrincipalId">The caller principal id when available.</param>
/// <param name="DenialReason">The internal denial reason.</param>
/// <param name="IsRetryable">A value indicating whether retry may make sense internally.</param>
/// <param name="ProjectionVersion">The optional safe projection version.</param>
/// <param name="ProjectionWatermark">The optional safe projection watermark.</param>
public sealed record ConversationTenantAccessDecision(
    bool IsAllowed,
    ConversationTenantAccessRequirement Requirement,
    TenantId? TenantId,
    string? CallerPrincipalId,
    ConversationTenantAccessDenialReason DenialReason,
    bool IsRetryable,
    long? ProjectionVersion = null,
    string? ProjectionWatermark = null)
{
    private static readonly Uri ErrorDocumentation =
        new("https://docs.hexalith.local/conversations/errors/tenant-access");

    /// <summary>
    /// Creates an allowed access decision.
    /// </summary>
    /// <param name="requirement">The requested operation class.</param>
    /// <param name="tenantId">The canonical tenant id.</param>
    /// <param name="callerPrincipalId">The caller principal id.</param>
    /// <param name="projectionVersion">The optional safe projection version.</param>
    /// <param name="projectionWatermark">The optional safe projection watermark.</param>
    /// <returns>An allowed decision.</returns>
    public static ConversationTenantAccessDecision Allowed(
        ConversationTenantAccessRequirement requirement,
        TenantId tenantId,
        string callerPrincipalId,
        long? projectionVersion = null,
        string? projectionWatermark = null)
        => new(
            true,
            ValidateRequirement(requirement),
            tenantId ?? throw new ArgumentNullException(nameof(tenantId)),
            ValidateCaller(callerPrincipalId),
            ConversationTenantAccessDenialReason.None,
            false,
            projectionVersion,
            projectionWatermark);

    /// <summary>
    /// Creates a denied access decision.
    /// </summary>
    /// <param name="requirement">The requested operation class.</param>
    /// <param name="tenantId">The canonical tenant id when available.</param>
    /// <param name="callerPrincipalId">The caller principal id when available.</param>
    /// <param name="reason">The internal denial reason.</param>
    /// <param name="isRetryable">A value indicating whether retry may make sense internally.</param>
    /// <param name="projectionVersion">The optional safe projection version.</param>
    /// <param name="projectionWatermark">The optional safe projection watermark.</param>
    /// <returns>A denied decision.</returns>
    public static ConversationTenantAccessDecision Denied(
        ConversationTenantAccessRequirement requirement,
        TenantId? tenantId,
        string? callerPrincipalId,
        ConversationTenantAccessDenialReason reason,
        bool isRetryable = false,
        long? projectionVersion = null,
        string? projectionWatermark = null)
    {
        if (reason == ConversationTenantAccessDenialReason.None)
        {
            throw new ArgumentException("Denied tenant access decisions require a denial reason.", nameof(reason));
        }

        return new(
            false,
            ValidateRequirement(requirement),
            tenantId,
            callerPrincipalId,
            reason,
            isRetryable,
            projectionVersion,
            projectionWatermark);
    }

    /// <summary>
    /// Converts the decision to a durable content-safe command rejection.
    /// </summary>
    /// <param name="schemaVersion">The command schema version.</param>
    /// <param name="correlationId">The safe correlation id supplied by the boundary, never the caller-controlled command body.</param>
    /// <param name="causationId">The safe causation id supplied by the boundary, never the caller-controlled command body.</param>
    /// <returns>A content-safe rejection event.</returns>
    /// <remarks>
    /// Public <see cref="ConversationRejectedDomainEvent.ReasonCode"/> mirrors the Story 1.2 error-code token
    /// so that unauthorized, nonexistent, disabled-tenant, stale-projection, and cross-tenant outcomes are
    /// externally indistinguishable. Internal <see cref="DenialReason"/> stays on this in-memory decision
    /// for telemetry and audit-handle use only and never crosses the durable event boundary.
    /// </remarks>
    public ConversationRejectedDomainEvent ToRejection(
        SchemaVersion? schemaVersion,
        string? correlationId,
        string? causationId)
        => new(ToRejectionCode(), ToPublicReasonCode(ToRejectionCode()), schemaVersion, correlationId, causationId);

    /// <summary>
    /// Converts the decision to a non-disclosing public error result.
    /// </summary>
    /// <param name="schemaVersion">The public error schema version.</param>
    /// <param name="correlationId">The safe correlation id.</param>
    /// <returns>A content-safe error result.</returns>
    public ConversationErrorResult ToSafeErrorResult(SchemaVersion schemaVersion, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(schemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        ConversationErrorCode code = ToPublicErrorCode();
        ConversationErrorCategory category = code == ConversationErrorCode.TenantBindingMissing
            ? ConversationErrorCategory.Validation
            : ConversationErrorCategory.Authorization;

        ConversationError error = new(
            schemaVersion,
            code,
            category,
            IsRetryable: IsPublicRetryable(),
            correlationId,
            Documentation: ErrorDocumentation,
            DeveloperGuidance: "The requested operation was not accepted.");

        return new ConversationErrorResult([error]);
    }

    private static string ValidateCaller(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static ConversationTenantAccessRequirement ValidateRequirement(ConversationTenantAccessRequirement requirement)
    {
        if (!Enum.IsDefined(typeof(ConversationTenantAccessRequirement), requirement))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement,
                "The tenant access requirement value is outside the closed-world set.");
        }

        return requirement;
    }

    private ConversationErrorCode ToRejectionCode()
        => DenialReason switch
        {
            ConversationTenantAccessDenialReason.MissingTenant
                or ConversationTenantAccessDenialReason.MalformedTenant
                => ConversationErrorCode.TenantBindingMissing,
            ConversationTenantAccessDenialReason.TenantAccessUnavailable
                or ConversationTenantAccessDenialReason.TenantAccessStale
                or ConversationTenantAccessDenialReason.TenantAccessGapDetected
                or ConversationTenantAccessDenialReason.TenantAccessRolledBack
                => ConversationErrorCode.TenantProjectionStale,
            _ => ConversationErrorCode.TenantIsolationViolation,
        };

    private ConversationErrorCode ToPublicErrorCode()
        => DenialReason is ConversationTenantAccessDenialReason.MissingTenant
            or ConversationTenantAccessDenialReason.MalformedTenant
            ? ConversationErrorCode.TenantBindingMissing
            : ConversationErrorCode.TenantIsolationViolation;

    private bool IsPublicRetryable()
        => DenialReason is ConversationTenantAccessDenialReason.TenantAccessUnavailable
            or ConversationTenantAccessDenialReason.TenantAccessStale
            or ConversationTenantAccessDenialReason.TenantAccessGapDetected
            or ConversationTenantAccessDenialReason.TenantAccessRolledBack
                && IsRetryable;

    private static string ToPublicReasonCode(ConversationErrorCode code)
    {
        if (code == ConversationErrorCode.TenantBindingMissing)
        {
            return "tenant_binding_missing";
        }

        if (code == ConversationErrorCode.TenantProjectionStale)
        {
            return "tenant_projection_stale";
        }

        return "tenant_isolation_violation";
    }
}
