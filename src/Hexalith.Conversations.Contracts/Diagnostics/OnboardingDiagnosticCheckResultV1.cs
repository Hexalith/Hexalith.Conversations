// <copyright file="OnboardingDiagnosticCheckResultV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Diagnostics;

/// <summary>
/// Carries the content-safe machine-readable result of one CORE onboarding diagnostic check.
/// </summary>
/// <param name="SchemaVersion">The result schema version.</param>
/// <param name="Check">The closed-vocabulary diagnostic check.</param>
/// <param name="Status">The closed-vocabulary diagnostic status mapped to the shared trust/freshness language.</param>
/// <param name="SafeMessage">The bounded content-safe adopter message.</param>
/// <param name="RemediationGuidanceCode">The bounded machine-readable remediation guidance code.</param>
/// <param name="Documentation">The safe HTTPS documentation pointer.</param>
/// <param name="RequirementMappings">The acceptance-criteria requirement mappings.</param>
/// <param name="Error">The optional typed error for degraded, blocked, or unknown checks reusing the shared error catalog.</param>
/// <param name="AuditHandle">The optional safe audit handle when allowed by the underlying error descriptor.</param>
public sealed record OnboardingDiagnosticCheckResultV1(
    SchemaVersion SchemaVersion,
    OnboardingDiagnosticCheck Check,
    OnboardingDiagnosticStatus Status,
    string SafeMessage,
    string RemediationGuidanceCode,
    Uri Documentation,
    IReadOnlyList<string> RequirementMappings,
    ConversationError? Error = null,
    string? AuditHandle = null)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the closed-vocabulary diagnostic check.
    /// </summary>
    public OnboardingDiagnosticCheck Check { get; } = Check ?? throw new ArgumentNullException(nameof(Check));

    /// <summary>
    /// Gets the closed-vocabulary diagnostic status.
    /// </summary>
    public OnboardingDiagnosticStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    /// <summary>
    /// Gets the bounded content-safe adopter message.
    /// </summary>
    public string SafeMessage { get; } = DiagnosticContractValidation.RequiredSafeText(SafeMessage, nameof(SafeMessage));

    /// <summary>
    /// Gets the bounded machine-readable remediation guidance code.
    /// </summary>
    public string RemediationGuidanceCode { get; } =
        DiagnosticContractValidation.RequiredSafeToken(RemediationGuidanceCode, nameof(RemediationGuidanceCode));

    /// <summary>
    /// Gets the safe HTTPS documentation pointer.
    /// </summary>
    public Uri Documentation { get; } = DiagnosticContractValidation.RequiredDocumentationUri(Documentation, nameof(Documentation));

    /// <summary>
    /// Gets the acceptance-criteria requirement mappings.
    /// </summary>
    public IReadOnlyList<string> RequirementMappings { get; } =
        DiagnosticContractValidation.RequiredRequirementMappings(RequirementMappings, nameof(RequirementMappings));

    /// <summary>
    /// Gets the optional typed error reusing the shared error catalog.
    /// </summary>
    public ConversationError? Error { get; } = ValidateError(Status, Error);

    /// <summary>
    /// Gets the optional safe audit handle.
    /// </summary>
    public string? AuditHandle { get; } = AuditHandle is null
        ? null
        : DiagnosticContractValidation.RequiredSafeToken(AuditHandle, nameof(AuditHandle));

    private static ConversationError? ValidateError(OnboardingDiagnosticStatus status, ConversationError? error)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status == OnboardingDiagnosticStatus.Ready && error is not null)
        {
            throw new ArgumentException("Ready diagnostic checks must not carry a typed failure error.", nameof(error));
        }

        if (status != OnboardingDiagnosticStatus.Ready && error is null)
        {
            throw new ArgumentException("Non-ready diagnostic checks must carry a typed failure error.", nameof(error));
        }

        return error;
    }
}
