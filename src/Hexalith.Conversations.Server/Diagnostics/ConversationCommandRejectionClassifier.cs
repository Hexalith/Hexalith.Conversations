// <copyright file="ConversationCommandRejectionClassifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Maps closed-vocabulary error codes and denial reasons to bounded signal classification enums.
/// </summary>
public static class ConversationCommandRejectionClassifier
{
    /// <summary>
    /// Maps a <see cref="ConversationErrorCode"/> to a <see cref="ConversationCommandRejectionClass"/>.
    /// </summary>
    /// <param name="code">The conversation error code.</param>
    /// <returns>The bounded rejection class for telemetry signals.</returns>
    public static ConversationCommandRejectionClass Classify(ConversationErrorCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (code == ConversationErrorCode.TenantBindingMissing)
        {
            return ConversationCommandRejectionClass.TenantBinding;
        }

        if (code == ConversationErrorCode.TenantIsolationViolation
            || code == ConversationErrorCode.TenantContextMismatch
            || code == ConversationErrorCode.AggregateNotFound)
        {
            return ConversationCommandRejectionClass.TenantIsolation;
        }

        if (code == ConversationErrorCode.TenantProjectionStale)
        {
            return ConversationCommandRejectionClass.TenantProjectionUnavailable;
        }

        if (code == ConversationErrorCode.CommandValidationFailed
            || code == ConversationErrorCode.SchemaVersionUnsupported
            || code == ConversationErrorCode.DuplicateParticipant
            || code == ConversationErrorCode.UnsupportedParticipant
            || code == ConversationErrorCode.ProviderOnlyIdentityForbidden)
        {
            return ConversationCommandRejectionClass.Validation;
        }

        if (code == ConversationErrorCode.IdempotencyConflict
            || code == ConversationErrorCode.IdempotencyOutcomeUnknown
            || code == ConversationErrorCode.IdempotencyKeyMissing)
        {
            return ConversationCommandRejectionClass.Idempotency;
        }

        if (code == ConversationErrorCode.AuditSinkUnavailable
            || code == ConversationErrorCode.AuditPairingRequired)
        {
            return ConversationCommandRejectionClass.AuditUnavailable;
        }

        if (code == ConversationErrorCode.ParticipantValidationUnavailable)
        {
            return ConversationCommandRejectionClass.Infrastructure;
        }

        return ConversationCommandRejectionClass.None;
    }

    /// <summary>
    /// Maps a <see cref="ConversationTenantAccessDenialReason"/> to a <see cref="ConversationTenantDenialClass"/>.
    /// </summary>
    /// <param name="reason">The denial reason.</param>
    /// <returns>The bounded denial class for telemetry signals.</returns>
    public static ConversationTenantDenialClass Classify(ConversationTenantAccessDenialReason reason)
        => reason switch
        {
            ConversationTenantAccessDenialReason.MissingTenant
                or ConversationTenantAccessDenialReason.MalformedTenant
                or ConversationTenantAccessDenialReason.MissingCaller
                => ConversationTenantDenialClass.MissingContext,

            ConversationTenantAccessDenialReason.UnknownTenant
                or ConversationTenantAccessDenialReason.TenantDisabled
                => ConversationTenantDenialClass.UnknownOrDisabled,

            ConversationTenantAccessDenialReason.MissingMember
                or ConversationTenantAccessDenialReason.InsufficientRole
                or ConversationTenantAccessDenialReason.UnmappedRole
                or ConversationTenantAccessDenialReason.UnmappedStatus
                => ConversationTenantDenialClass.InsufficientAccess,

            ConversationTenantAccessDenialReason.TenantAccessUnavailable
                or ConversationTenantAccessDenialReason.TenantAccessStale
                or ConversationTenantAccessDenialReason.TenantAccessGapDetected
                or ConversationTenantAccessDenialReason.TenantAccessRolledBack
                => ConversationTenantDenialClass.ProjectionUnavailable,

            ConversationTenantAccessDenialReason.TenantMismatch
                or ConversationTenantAccessDenialReason.MalformedProjection
                or ConversationTenantAccessDenialReason.TenantProjectionPoisoned
                => ConversationTenantDenialClass.ContextMismatch,

            _ => ConversationTenantDenialClass.None,
        };
}
