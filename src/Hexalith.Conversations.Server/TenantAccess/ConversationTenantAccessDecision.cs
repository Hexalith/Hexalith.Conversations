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
            requirement,
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
            requirement,
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
    /// <param name="correlationId">The command correlation id.</param>
    /// <param name="causationId">The command causation id.</param>
    /// <returns>A content-safe rejection event.</returns>
    public ConversationRejectedDomainEvent ToRejection(
        SchemaVersion? schemaVersion,
        string? correlationId,
        string? causationId)
        => new(ToRejectionCode(), ToReasonCode(DenialReason), schemaVersion, correlationId, causationId);

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
            IsRetryable: false,
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

    private static string ToReasonCode(ConversationTenantAccessDenialReason reason)
        => reason switch
        {
            ConversationTenantAccessDenialReason.None => "tenant_access_allowed",
            ConversationTenantAccessDenialReason.MissingTenant => "tenant_binding_missing",
            ConversationTenantAccessDenialReason.MalformedTenant => "tenant_binding_malformed",
            ConversationTenantAccessDenialReason.MissingCaller => "caller_missing",
            ConversationTenantAccessDenialReason.UnknownTenant => "tenant_unknown",
            ConversationTenantAccessDenialReason.TenantDisabled => "tenant_disabled",
            ConversationTenantAccessDenialReason.MissingMember => "tenant_member_missing",
            ConversationTenantAccessDenialReason.InsufficientRole => "tenant_role_insufficient",
            ConversationTenantAccessDenialReason.UnmappedRole => "tenant_role_unmapped",
            ConversationTenantAccessDenialReason.UnmappedStatus => "tenant_status_unmapped",
            ConversationTenantAccessDenialReason.TenantAccessUnavailable => "tenant_access_unavailable",
            ConversationTenantAccessDenialReason.TenantAccessStale => "tenant_access_stale",
            ConversationTenantAccessDenialReason.TenantAccessGapDetected => "tenant_access_gap_detected",
            ConversationTenantAccessDenialReason.TenantAccessRolledBack => "tenant_access_rolled_back",
            ConversationTenantAccessDenialReason.TenantMismatch => "tenant_mismatch",
            ConversationTenantAccessDenialReason.MalformedProjection => "tenant_projection_malformed",
            ConversationTenantAccessDenialReason.TenantProjectionPoisoned => "tenant_projection_poisoned",
            _ => "tenant_access_denied",
        };
}
