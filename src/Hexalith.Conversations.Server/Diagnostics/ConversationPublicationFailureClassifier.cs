// <copyright file="ConversationPublicationFailureClassifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Maps closed-vocabulary publication error codes to bounded failure signal classification enums.
/// </summary>
public static class ConversationPublicationFailureClassifier
{
    /// <summary>
    /// Maps a <see cref="ConversationErrorCode"/> to a <see cref="ConversationPublicationFailureClass"/>.
    /// </summary>
    /// <param name="code">The conversation error code.</param>
    /// <returns>The bounded publication failure class for telemetry signals.</returns>
    public static ConversationPublicationFailureClass Classify(ConversationErrorCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (code == ConversationErrorCode.SchemaVersionUnsupported)
        {
            return ConversationPublicationFailureClass.UnsupportedSchema;
        }

        if (code == ConversationErrorCode.TenantContextMismatch
            || code == ConversationErrorCode.TenantIsolationViolation
            || code == ConversationErrorCode.TenantBindingMissing)
        {
            return ConversationPublicationFailureClass.TenantViolation;
        }

        if (code == ConversationErrorCode.IdempotencyConflict)
        {
            return ConversationPublicationFailureClass.ReplayRequired;
        }

        return ConversationPublicationFailureClass.TransientFailure;
    }
}
