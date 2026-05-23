// <copyright file="ConversationCorePreconditionCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Diagnostics;

/// <summary>
/// Provides the canonical contract-owned catalog of adopter-facing CORE preconditions and their safe-failure behavior.
/// </summary>
/// <remarks>
/// This is the single source of truth consumed by both adopter documentation and tests. Each precondition reuses the
/// shared <see cref="ConversationErrorCatalog"/> taxonomy for the typed failure emitted when unmet, and the shared
/// <see cref="ProjectionTrustState"/> vocabulary for the required state. No new public error or freshness vocabulary
/// is introduced.
/// </remarks>
public static class ConversationCorePreconditionCatalog
{
    private static readonly Uri PreconditionDocumentation =
        new("https://docs.hexalith.local/conversations/contracts/v1/preconditions", UriKind.Absolute);

    private static readonly CorePreconditionV1[] Preconditions =
    [
        Precondition(
            "projection-freshness",
            OnboardingDiagnosticCheck.TenantContext,
            ProjectionTrustState.Current,
            ConversationErrorCode.TenantProjectionStale,
            "When tenant access state is stale, dependent reads degrade to a non-trust-bearing state and writes fail closed; retry after the projection is current."),
        Precondition(
            "audit-sink-availability",
            OnboardingDiagnosticCheck.AuditAvailability,
            ProjectionTrustState.Current,
            ConversationErrorCode.AuditSinkUnavailable,
            "When audit recording is unavailable, governed mutations fail closed and are not silently weakened; retry after audit recording is available."),
        Precondition(
            "supported-schema-versions",
            OnboardingDiagnosticCheck.SchemaCompatibility,
            ProjectionTrustState.Current,
            ConversationErrorCode.SchemaVersionUnsupported,
            "When the requested schema version is unsupported, the request is rejected with a versioning error rather than processed under an incompatible contract."),
        Precondition(
            "contract-compatibility",
            OnboardingDiagnosticCheck.ContractVersion,
            ProjectionTrustState.Current,
            ConversationErrorCode.SchemaVersionUnsupported,
            "When the requested contract or package version is unsupported or invalid, compatibility evaluation returns a typed versioning error and bounded remediation."),
        Precondition(
            "participant-identity-validation",
            OnboardingDiagnosticCheck.PartiesIntegration,
            ProjectionTrustState.Current,
            ConversationErrorCode.ParticipantValidationUnavailable,
            "When participant identity validation is unavailable, writes fail closed and authorized reads may degrade display hydration to a safe unresolved state without disclosing personal data; retry when validation is available."),
        Precondition(
            "idempotency-key-behavior",
            OnboardingDiagnosticCheck.TenantContext,
            ProjectionTrustState.Current,
            ConversationErrorCode.IdempotencyKeyMissing,
            "When idempotency metadata is missing for a command, the command is rejected before processing so retries cannot duplicate or weaken accepted outcomes."),
        Precondition(
            "projection-subscription-health",
            OnboardingDiagnosticCheck.ProjectionSubscription,
            ProjectionTrustState.Current,
            ConversationErrorCode.TenantProjectionStale,
            "When the projection subscription is stale, rebuilding, or unavailable, reads degrade to a non-trust-bearing state and writes fail closed; retry after the subscription is current."),
        Precondition(
            "required-configuration",
            OnboardingDiagnosticCheck.ProviderConfiguration,
            ProjectionTrustState.Current,
            ConversationErrorCode.CommandValidationFailed,
            "When required configuration is missing, dependent operations report a bounded configuration-gap status and are not processed under partial configuration; no provider content or secret value is exposed."),
    ];

    private static readonly IReadOnlyDictionary<string, CorePreconditionV1> ByPreconditionId =
        Preconditions.ToDictionary(precondition => precondition.PreconditionId, StringComparer.Ordinal);

    /// <summary>
    /// Gets every documented CORE precondition.
    /// </summary>
    public static IReadOnlyList<CorePreconditionV1> All => Preconditions;

    /// <summary>
    /// Gets the precondition descriptor for a supported precondition identifier.
    /// </summary>
    /// <param name="preconditionId">The supported precondition identifier.</param>
    /// <returns>The precondition descriptor.</returns>
    public static CorePreconditionV1 Get(string preconditionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preconditionId);
        return ByPreconditionId.TryGetValue(preconditionId, out CorePreconditionV1? precondition)
            ? precondition
            : throw new ArgumentException($"Unsupported CORE precondition '{preconditionId}'.", nameof(preconditionId));
    }

    private static CorePreconditionV1 Precondition(
        string preconditionId,
        OnboardingDiagnosticCheck check,
        ProjectionTrustState requiredTrustState,
        ConversationErrorCode unmetErrorCode,
        string safeFailureBehavior)
        => new(
            SchemaVersion.Current,
            preconditionId,
            check,
            requiredTrustState,
            safeFailureBehavior,
            unmetErrorCode,
            PreconditionDocumentation);
}
