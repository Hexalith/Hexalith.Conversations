// <copyright file="ConversationErrorCode.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Defines stable machine-readable Conversations error codes.
/// </summary>
[JsonConverter(typeof(ConversationErrorCodeJsonConverter))]
public sealed record ConversationErrorCode
{
    /// <summary>
    /// Gets the tenant binding missing code.
    /// </summary>
    public static ConversationErrorCode TenantBindingMissing { get; } = new("tenant_binding_missing");

    /// <summary>
    /// Gets the tenant isolation violation code.
    /// </summary>
    public static ConversationErrorCode TenantIsolationViolation { get; } = new("tenant_isolation_violation");

    /// <summary>
    /// Gets the tenant projection stale code.
    /// </summary>
    public static ConversationErrorCode TenantProjectionStale { get; } = new("tenant_projection_stale");

    /// <summary>
    /// Gets the audit sink unavailable code.
    /// </summary>
    public static ConversationErrorCode AuditSinkUnavailable { get; } = new("audit_sink_unavailable");

    /// <summary>
    /// Gets the audit pairing required code.
    /// </summary>
    public static ConversationErrorCode AuditPairingRequired { get; } = new("audit_pairing_required");

    /// <summary>
    /// Gets the idempotency conflict code.
    /// </summary>
    /// <remarks>
    /// P54 review fix (2026-05-20): a conflict is non-retryable. A caller who receives
    /// <c>idempotency_conflict</c> must generate a fresh idempotency key for the corrected request;
    /// the same key cannot be reused with a different payload until the conflict record expires per
    /// retention. Resubmitting the original equivalent payload remains safe and returns the stored
    /// logical outcome. See ADR-0001 §"Decision" for the rationale.
    /// </remarks>
    public static ConversationErrorCode IdempotencyConflict { get; } = new("idempotency_conflict");

    /// <summary>
    /// Gets the idempotency outcome unknown code.
    /// </summary>
    public static ConversationErrorCode IdempotencyOutcomeUnknown { get; } = new("idempotency_outcome_unknown");

    /// <summary>
    /// Gets the missing idempotency key code.
    /// </summary>
    public static ConversationErrorCode IdempotencyKeyMissing { get; } = new("idempotency_key_missing");

    /// <summary>
    /// Gets the hidden or unavailable aggregate code.
    /// </summary>
    public static ConversationErrorCode AggregateNotFound { get; } = new("aggregate_not_found");

    /// <summary>
    /// Gets the unsupported schema version code.
    /// </summary>
    public static ConversationErrorCode SchemaVersionUnsupported { get; } = new("schema_version_unsupported");

    /// <summary>
    /// Gets the command validation failed code.
    /// </summary>
    public static ConversationErrorCode CommandValidationFailed { get; } = new("command_validation_failed");

    /// <summary>
    /// Gets the duplicate participant membership code.
    /// </summary>
    public static ConversationErrorCode DuplicateParticipant { get; } = new("duplicate_participant");

    /// <summary>
    /// Gets the unsupported participant type or role code.
    /// </summary>
    public static ConversationErrorCode UnsupportedParticipant { get; } = new("unsupported_participant");

    /// <summary>
    /// Gets the participant validation unavailable code.
    /// </summary>
    public static ConversationErrorCode ParticipantValidationUnavailable { get; } = new("participant_validation_unavailable");

    /// <summary>
    /// Gets the tenant context mismatch code.
    /// </summary>
    public static ConversationErrorCode TenantContextMismatch { get; } = new("tenant_context_mismatch");

    /// <summary>
    /// Gets the provider-only identity forbidden code.
    /// </summary>
    public static ConversationErrorCode ProviderOnlyIdentityForbidden { get; } = new("provider_only_identity_forbidden");

    private static readonly IReadOnlyDictionary<string, ConversationErrorCode> KnownCodes =
        new[]
        {
            TenantBindingMissing,
            TenantIsolationViolation,
            TenantProjectionStale,
            AuditSinkUnavailable,
            AuditPairingRequired,
            IdempotencyConflict,
            IdempotencyOutcomeUnknown,
            IdempotencyKeyMissing,
            AggregateNotFound,
            SchemaVersionUnsupported,
            CommandValidationFailed,
            DuplicateParticipant,
            UnsupportedParticipant,
            ParticipantValidationUnavailable,
            TenantContextMismatch,
            ProviderOnlyIdentityForbidden,
        }.ToDictionary(code => code.Value, StringComparer.Ordinal);

    private ConversationErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the machine-readable code value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported error code.
    /// </summary>
    /// <param name="value">The machine-readable error code value.</param>
    /// <returns>The matching supported error code.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or unsupported.</exception>
    public static ConversationErrorCode Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownCodes.TryGetValue(value, out ConversationErrorCode? code)
            ? code
            : throw new ArgumentException($"Unsupported conversation error code '{value}'.", nameof(value));
    }

    /// <summary>
    /// Gets a value indicating whether retry is meaningful for the supplied error code.
    /// P23 review fix (2026-05-19): canonical retryable taxonomy lives on the error code itself so handlers
    /// and contract samples cannot drift apart. AuditSinkUnavailable is retryable per D2 decision.
    /// </summary>
    /// <param name="code">The error code to evaluate.</param>
    /// <returns><c>true</c> when retry is meaningful; otherwise <c>false</c>.</returns>
    public static bool IsRetryable(ConversationErrorCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return code == TenantProjectionStale
            || code == ParticipantValidationUnavailable
            || code == IdempotencyOutcomeUnknown
            || code == AuditSinkUnavailable;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
