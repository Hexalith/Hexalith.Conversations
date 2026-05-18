// <copyright file="ConversationErrorCode.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Defines stable machine-readable Conversations error codes.
/// </summary>
public static class ConversationErrorCode
{
    /// <summary>
    /// The tenant binding is missing.
    /// </summary>
    public const string TenantBindingMissing = "tenant_binding_missing";

    /// <summary>
    /// Tenant isolation prevented the operation.
    /// </summary>
    public const string TenantIsolationViolation = "tenant_isolation_violation";

    /// <summary>
    /// Tenant projection state is stale.
    /// </summary>
    public const string TenantProjectionStale = "tenant_projection_stale";

    /// <summary>
    /// The audit sink is unavailable.
    /// </summary>
    public const string AuditSinkUnavailable = "audit_sink_unavailable";

    /// <summary>
    /// Required audit pairing is missing.
    /// </summary>
    public const string AuditPairingRequired = "audit_pairing_required";

    /// <summary>
    /// The idempotency key conflicts with a prior request.
    /// </summary>
    public const string IdempotencyConflict = "idempotency_conflict";

    /// <summary>
    /// The aggregate is hidden or unavailable to the caller.
    /// </summary>
    public const string AggregateNotFound = "aggregate_not_found";

    /// <summary>
    /// The requested schema version is unsupported.
    /// </summary>
    public const string SchemaVersionUnsupported = "schema_version_unsupported";

    /// <summary>
    /// Command validation failed.
    /// </summary>
    public const string CommandValidationFailed = "command_validation_failed";
}
