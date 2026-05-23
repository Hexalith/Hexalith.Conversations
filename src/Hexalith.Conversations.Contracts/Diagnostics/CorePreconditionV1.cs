// <copyright file="CorePreconditionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Diagnostics;

/// <summary>
/// Describes a single adopter-facing CORE precondition and its safe-failure behavior.
/// </summary>
/// <param name="SchemaVersion">The descriptor schema version.</param>
/// <param name="PreconditionId">The bounded machine-readable precondition identifier.</param>
/// <param name="Check">The diagnostic check that evaluates this precondition.</param>
/// <param name="RequiredTrustState">The trust/freshness state required for the precondition to be met (only <c>Current</c> is trust-bearing).</param>
/// <param name="SafeFailureBehavior">The bounded content-safe description of the safe failure behavior when unmet.</param>
/// <param name="UnmetErrorCode">The typed error code emitted when the precondition is unmet.</param>
/// <param name="Documentation">The safe HTTPS documentation pointer.</param>
public sealed record CorePreconditionV1(
    SchemaVersion SchemaVersion,
    string PreconditionId,
    OnboardingDiagnosticCheck Check,
    ProjectionTrustState RequiredTrustState,
    string SafeFailureBehavior,
    ConversationErrorCode UnmetErrorCode,
    Uri Documentation)
{
    /// <summary>
    /// Gets the descriptor schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the bounded machine-readable precondition identifier.
    /// </summary>
    public string PreconditionId { get; } = DiagnosticContractValidation.RequiredSafeToken(PreconditionId, nameof(PreconditionId));

    /// <summary>
    /// Gets the diagnostic check that evaluates this precondition.
    /// </summary>
    public OnboardingDiagnosticCheck Check { get; } = Check ?? throw new ArgumentNullException(nameof(Check));

    /// <summary>
    /// Gets the trust/freshness state required for the precondition to be met.
    /// </summary>
    public ProjectionTrustState RequiredTrustState { get; } = RequiredTrustState ?? throw new ArgumentNullException(nameof(RequiredTrustState));

    /// <summary>
    /// Gets the bounded content-safe description of the safe failure behavior when unmet.
    /// </summary>
    public string SafeFailureBehavior { get; } = DiagnosticContractValidation.RequiredSafeText(SafeFailureBehavior, nameof(SafeFailureBehavior));

    /// <summary>
    /// Gets the typed error code emitted when the precondition is unmet.
    /// </summary>
    public ConversationErrorCode UnmetErrorCode { get; } = UnmetErrorCode ?? throw new ArgumentNullException(nameof(UnmetErrorCode));

    /// <summary>
    /// Gets the safe HTTPS documentation pointer.
    /// </summary>
    public Uri Documentation { get; } = DiagnosticContractValidation.RequiredDocumentationUri(Documentation, nameof(Documentation));
}
