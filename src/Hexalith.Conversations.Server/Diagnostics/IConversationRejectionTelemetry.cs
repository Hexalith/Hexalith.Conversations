// <copyright file="IConversationRejectionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits content-safe, bounded-cardinality signals for command rejections, tenant denials, and privileged access attempts.
/// </summary>
public interface IConversationRejectionTelemetry
{
    /// <summary>
    /// Records a command rejection signal with bounded dimensions and no sensitive content.
    /// </summary>
    /// <param name="rejectionClass">The bounded rejection class.</param>
    /// <param name="operationClass">The bounded operation class.</param>
    /// <param name="isRetryable">A value indicating whether the operation is retryable.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    void RecordCommandRejection(
        ConversationCommandRejectionClass rejectionClass,
        ConversationTenantAccessRequirement operationClass,
        bool isRetryable,
        string correlationId);

    /// <summary>
    /// Records a tenant isolation denial signal with bounded dimensions and no sensitive content.
    /// </summary>
    /// <param name="denialClass">The bounded denial class.</param>
    /// <param name="operationClass">The bounded operation class.</param>
    /// <param name="isRetryable">A value indicating whether the operation is retryable.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    void RecordTenantDenial(
        ConversationTenantDenialClass denialClass,
        ConversationTenantAccessRequirement operationClass,
        bool isRetryable,
        string correlationId);

    /// <summary>
    /// Records a privileged access attempt signal with bounded dimensions and no sensitive content.
    /// </summary>
    /// <param name="accessClass">The bounded privileged access class.</param>
    /// <param name="operationClass">The bounded operation class.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    void RecordPrivilegedAccessAttempt(
        ConversationPrivilegedAccessClass accessClass,
        ConversationTenantAccessRequirement operationClass,
        string correlationId);
}
