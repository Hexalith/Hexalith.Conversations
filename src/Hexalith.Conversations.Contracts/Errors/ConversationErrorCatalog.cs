// <copyright file="ConversationErrorCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Provides canonical safe descriptors and factories for Conversations errors.
/// </summary>
public static class ConversationErrorCatalog
{
    private static readonly Uri ErrorDocumentation = new("https://docs.hexalith.local/conversations/contracts/v1/errors", UriKind.Absolute);

    private static readonly ConversationErrorDescriptor[] Descriptors =
    [
        Descriptor(
            ConversationErrorCode.TenantBindingMissing,
            ConversationErrorCategory.Authorization,
            false,
            ConversationErrorClientAction.ProvideContext,
            "Provide authenticated tenant and caller context."),
        Descriptor(
            ConversationErrorCode.TenantIsolationViolation,
            ConversationErrorCategory.Authorization,
            false,
            ConversationErrorClientAction.CheckAccess,
            "The request cannot be completed with the supplied access context."),
        Descriptor(
            ConversationErrorCode.TenantProjectionStale,
            ConversationErrorCategory.Freshness,
            true,
            ConversationErrorClientAction.RetryLater,
            "Retry after tenant access state is current."),
        Descriptor(
            ConversationErrorCode.AuditSinkUnavailable,
            ConversationErrorCategory.Audit,
            true,
            ConversationErrorClientAction.RetryLater,
            "Retry after audit recording is available."),
        Descriptor(
            ConversationErrorCode.AuditPairingRequired,
            ConversationErrorCategory.Audit,
            false,
            ConversationErrorClientAction.ProvideAuditEvidence,
            "Provide required audit evidence before retrying.",
            allowsAuditHandle: true),
        Descriptor(
            ConversationErrorCode.IdempotencyConflict,
            ConversationErrorCategory.Conflict,
            false,
            ConversationErrorClientAction.UseNewIdempotencyKey,
            "Use a new idempotency key for a changed command payload."),
        Descriptor(
            ConversationErrorCode.IdempotencyOutcomeUnknown,
            ConversationErrorCategory.Uncertainty,
            true,
            ConversationErrorClientAction.RetrySameRequest,
            "Retry with the same idempotency metadata when the command outcome is unknown."),
        Descriptor(
            ConversationErrorCode.IdempotencyKeyMissing,
            ConversationErrorCategory.Validation,
            false,
            ConversationErrorClientAction.ProvideIdempotencyKey,
            "Provide idempotency metadata before sending the command."),
        Descriptor(
            ConversationErrorCode.AggregateNotFound,
            ConversationErrorCategory.Hidden,
            false,
            ConversationErrorClientAction.HideOrRefresh,
            "The requested conversation is not available."),
        Descriptor(
            ConversationErrorCode.SchemaVersionUnsupported,
            ConversationErrorCategory.Versioning,
            false,
            ConversationErrorClientAction.UseSupportedVersion,
            "Use supported Conversations contract and client versions."),
        Descriptor(
            ConversationErrorCode.CommandValidationFailed,
            ConversationErrorCategory.Validation,
            false,
            ConversationErrorClientAction.CorrectRequest,
            "Correct the request and retry."),
        Descriptor(
            ConversationErrorCode.DuplicateParticipant,
            ConversationErrorCategory.Conflict,
            false,
            ConversationErrorClientAction.CorrectRequest,
            "Correct participant membership and retry."),
        Descriptor(
            ConversationErrorCode.UnsupportedParticipant,
            ConversationErrorCategory.Validation,
            false,
            ConversationErrorClientAction.CorrectRequest,
            "Use a supported participant type and role."),
        Descriptor(
            ConversationErrorCode.ParticipantValidationUnavailable,
            ConversationErrorCategory.Validation,
            true,
            ConversationErrorClientAction.RetryLater,
            "Retry after participant validation is available."),
        Descriptor(
            ConversationErrorCode.TenantContextMismatch,
            ConversationErrorCategory.Authorization,
            false,
            ConversationErrorClientAction.AlignContext,
            "Align the request context with the authenticated context."),
        Descriptor(
            ConversationErrorCode.ProviderOnlyIdentityForbidden,
            ConversationErrorCategory.Validation,
            false,
            ConversationErrorClientAction.UsePartyIdentity,
            "Use a Conversations Party identity for participant attribution."),
    ];

    private static readonly IReadOnlyDictionary<ConversationErrorCode, ConversationErrorDescriptor> DescriptorByCode =
        Descriptors.ToDictionary(descriptor => descriptor.Code);

    /// <summary>
    /// Gets every supported descriptor.
    /// </summary>
    public static IReadOnlyList<ConversationErrorDescriptor> All => Descriptors;

    /// <summary>
    /// Gets the descriptor for a supported code.
    /// </summary>
    /// <param name="code">The supported error code.</param>
    /// <returns>The descriptor.</returns>
    public static ConversationErrorDescriptor Get(ConversationErrorCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return DescriptorByCode.TryGetValue(code, out ConversationErrorDescriptor? descriptor)
            ? descriptor
            : throw new ArgumentException($"Unsupported conversation error code '{code.Value}'.", nameof(code));
    }

    /// <summary>
    /// Creates a content-safe error from the canonical descriptor.
    /// </summary>
    /// <param name="code">The supported error code.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    /// <param name="auditHandle">The optional audit handle, included only when allowed by the descriptor.</param>
    /// <param name="safeFieldDiagnostics">Optional non-disclosing field diagnostics.</param>
    /// <param name="developerGuidance">Optional safe backward-compatible developer guidance.</param>
    /// <returns>The typed error.</returns>
    public static ConversationError CreateError(
        ConversationErrorCode code,
        string correlationId,
        string? auditHandle = null,
        IReadOnlyDictionary<string, string>? safeFieldDiagnostics = null,
        string? developerGuidance = null)
    {
        ConversationErrorDescriptor descriptor = Get(code);
        return new ConversationError(
            SchemaVersion.Current,
            descriptor.Code,
            descriptor.Category,
            descriptor.IsRetryable,
            correlationId,
            descriptor.AllowsAuditHandle ? auditHandle : null,
            descriptor.Documentation,
            safeFieldDiagnostics,
            developerGuidance,
            descriptor.ClientAction,
            descriptor.SafeMessage);
    }

    private static ConversationErrorDescriptor Descriptor(
        ConversationErrorCode code,
        ConversationErrorCategory category,
        bool isRetryable,
        ConversationErrorClientAction clientAction,
        string safeMessage,
        bool allowsAuditHandle = false)
        => new(code, category, isRetryable, clientAction, safeMessage, ErrorDocumentation, allowsAuditHandle);
}
