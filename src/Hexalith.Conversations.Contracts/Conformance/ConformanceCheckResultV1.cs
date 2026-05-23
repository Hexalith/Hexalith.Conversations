// <copyright file="ConformanceCheckResultV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Carries the content-safe machine-readable result of one adopter-facing conformance check.
/// </summary>
/// <remarks>
/// Each result maps a single <see cref="ConformanceCheck"/> to the relevant requirement (FR/NFR ids),
/// the relevant CORE precondition ids (from <c>ConversationCorePreconditionCatalog</c>), and the relevant
/// release-gate category so Story 5.10 can later aggregate without rework. Only structured, content-safe
/// data is carried; no tenant IDs, Party IDs, conversation IDs/existence hints, provider session/payload
/// values, business-reference values, raw exception text, local file paths, or production secrets are
/// present in any field. Typed failures embed the shared <see cref="ConversationError"/> rather than
/// re-serializing free text.
/// </remarks>
/// <param name="schemaVersion">The result schema version.</param>
/// <param name="check">The closed-vocabulary conformance check.</param>
/// <param name="scenario">The bounded machine-readable scenario identifier exercised (AC4 scenario matrix).</param>
/// <param name="outcome">The closed-vocabulary outcome aligned to the shared trust/freshness and readiness language.</param>
/// <param name="failureClassification">The closed-vocabulary failure classification distinguishing failure classes.</param>
/// <param name="requirementMappings">The relevant requirement (FR/NFR) identifiers.</param>
/// <param name="preconditionMappings">The relevant CORE precondition identifiers.</param>
/// <param name="releaseGateMappings">The relevant release-gate category identifiers (consumed by Story 5.10).</param>
/// <param name="safeMessage">The bounded content-safe adopter message.</param>
/// <param name="remediationGuidanceCode">The bounded machine-readable remediation guidance code.</param>
/// <param name="documentation">The safe HTTPS documentation pointer.</param>
/// <param name="correlationId">The safe correlation identifier.</param>
/// <param name="error">The optional embedded typed error for any failure classification.</param>
/// <param name="auditHandle">The optional safe audit handle when allowed.</param>
public sealed record ConformanceCheckResultV1(
    SchemaVersion SchemaVersion,
    ConformanceCheck Check,
    string Scenario,
    ConformanceOutcome Outcome,
    ConformanceFailureClassification FailureClassification,
    IReadOnlyList<string> RequirementMappings,
    IReadOnlyList<string> PreconditionMappings,
    IReadOnlyList<string> ReleaseGateMappings,
    string SafeMessage,
    string RemediationGuidanceCode,
    Uri Documentation,
    string CorrelationId,
    ConversationError? Error = null,
    string? AuditHandle = null)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the closed-vocabulary conformance check.
    /// </summary>
    public ConformanceCheck Check { get; } = Check ?? throw new ArgumentNullException(nameof(Check));

    /// <summary>
    /// Gets the bounded machine-readable scenario identifier exercised.
    /// </summary>
    public string Scenario { get; } = ConformanceContractValidation.RequiredSafeToken(Scenario, nameof(Scenario));

    /// <summary>
    /// Gets the closed-vocabulary outcome.
    /// </summary>
    public ConformanceOutcome Outcome { get; } = Outcome ?? throw new ArgumentNullException(nameof(Outcome));

    /// <summary>
    /// Gets the closed-vocabulary failure classification.
    /// </summary>
    public ConformanceFailureClassification FailureClassification { get; } =
        FailureClassification ?? throw new ArgumentNullException(nameof(FailureClassification));

    /// <summary>
    /// Gets the relevant requirement (FR/NFR) identifiers.
    /// </summary>
    public IReadOnlyList<string> RequirementMappings { get; } =
        ConformanceContractValidation.RequiredMappingTokens(RequirementMappings, nameof(RequirementMappings));

    /// <summary>
    /// Gets the relevant CORE precondition identifiers.
    /// </summary>
    public IReadOnlyList<string> PreconditionMappings { get; } =
        ConformanceContractValidation.RequiredMappingTokens(PreconditionMappings, nameof(PreconditionMappings));

    /// <summary>
    /// Gets the relevant release-gate category identifiers consumed by Story 5.10.
    /// </summary>
    public IReadOnlyList<string> ReleaseGateMappings { get; } =
        ConformanceContractValidation.RequiredMappingTokens(ReleaseGateMappings, nameof(ReleaseGateMappings));

    /// <summary>
    /// Gets the bounded content-safe adopter message.
    /// </summary>
    public string SafeMessage { get; } = ConformanceContractValidation.RequiredSafeText(SafeMessage, nameof(SafeMessage));

    /// <summary>
    /// Gets the bounded machine-readable remediation guidance code.
    /// </summary>
    public string RemediationGuidanceCode { get; } =
        ConformanceContractValidation.RequiredSafeToken(RemediationGuidanceCode, nameof(RemediationGuidanceCode));

    /// <summary>
    /// Gets the safe HTTPS documentation pointer.
    /// </summary>
    public Uri Documentation { get; } = ConformanceContractValidation.RequiredDocumentationUri(Documentation, nameof(Documentation));

    /// <summary>
    /// Gets the safe correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = ConformanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    /// <summary>
    /// Gets the optional embedded typed error reusing the shared error catalog.
    /// </summary>
    /// <remarks>
    /// The error invariant is outcome-based, mirroring the Story 4.4 onboarding readiness language: a
    /// trust-bearing <see cref="ConformanceOutcome.Ready"/> outcome must not carry a typed error, while any
    /// non-ready outcome (<c>degraded</c>, <c>blocked</c>, or <c>unknown</c>) must embed the observed typed
    /// error. A conformant check can therefore legitimately carry the expected typed error it observed (for
    /// example a conformant idempotency check that correctly surfaced a non-retryable conflict).
    /// </remarks>
    public ConversationError? Error { get; } = ValidateError(Outcome, Error);

    /// <summary>
    /// Gets the optional safe audit handle.
    /// </summary>
    public string? AuditHandle { get; } = ConformanceContractValidation.OptionalSafeToken(AuditHandle, nameof(AuditHandle));

    /// <summary>
    /// Gets a value indicating whether this result represents a conformant check (no failure).
    /// </summary>
    public bool IsConformant => FailureClassification.Equals(ConformanceFailureClassification.Conformant);

    private static ConversationError? ValidateError(ConformanceOutcome outcome, ConversationError? error)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.Equals(ConformanceOutcome.Ready) && error is not null)
        {
            throw new ArgumentException("Ready conformance checks must not carry a typed failure error.", nameof(error));
        }

        return !outcome.Equals(ConformanceOutcome.Ready) && error is null
            ? throw new ArgumentException("Non-ready conformance checks must carry the observed typed error.", nameof(error))
            : error;
    }
}
