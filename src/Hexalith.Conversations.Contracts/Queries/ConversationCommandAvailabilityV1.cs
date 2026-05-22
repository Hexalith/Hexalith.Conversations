// <copyright file="ConversationCommandAvailabilityV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Server-owned command availability metadata for a governed read surface.
/// </summary>
public sealed record ConversationCommandAvailabilityV1(
    string ActionName,
    ProjectionTrustState AvailabilityState,
    string RequiredPermission,
    ProjectionTrustState PreconditionState,
    string RiskLevel,
    ProjectionTrustState FreshnessRequirementState,
    ConversationAuditReadinessState AuditRequirement,
    string BlockedReason,
    DateTimeOffset LastEvaluatedAt,
    string? ActionClassification = null,
    bool RequiresFreshServerRecheck = true)
{
    public const string ReadOnlyActionClassification = "read-only";

    public const string GovernanceChangingActionClassification = "governance-changing";

    private const int MaximumTokenLength = 96;
    private const int MaximumReasonLength = 180;

    private static readonly string[] ForbiddenVocabulary =
    [
        "eventstore",
        "event store",
        "stream",
        "providerpayload",
        "provider payload",
        "provider-payload",
        "browser-selected",
        "browser selected",
        "local storage",
        "route-secret",
        "route secret",
        "hidden-field",
        "hidden field",
        "client-state",
        "client state",
        "client-side",
        "client side",
        "tenant-evil",
        "raw exception",
        "raw-exception",
        "exception text",
        "persondetails",
        "person details",
        "party personal",
        "party-personal",
    ];

    private static readonly string[] NormalizedForbiddenVocabulary =
    [
        .. ForbiddenVocabulary.Select(NormalizeVocabulary),
    ];

    public string ActionName { get; } = RequireSafeToken(ActionName, nameof(ActionName));

    public ProjectionTrustState AvailabilityState { get; } =
        AvailabilityState ?? throw new ArgumentNullException(nameof(AvailabilityState));

    public string RequiredPermission { get; } = RequireSafeToken(RequiredPermission, nameof(RequiredPermission));

    public ProjectionTrustState PreconditionState { get; } =
        PreconditionState ?? throw new ArgumentNullException(nameof(PreconditionState));

    public string RiskLevel { get; } = RequireSafeToken(RiskLevel, nameof(RiskLevel));

    public ProjectionTrustState FreshnessRequirementState { get; } =
        FreshnessRequirementState ?? throw new ArgumentNullException(nameof(FreshnessRequirementState));

    public ConversationAuditReadinessState AuditRequirement { get; } =
        AuditRequirement ?? throw new ArgumentNullException(nameof(AuditRequirement));

    public string BlockedReason { get; } = RequireSafeText(BlockedReason, nameof(BlockedReason));

    public DateTimeOffset LastEvaluatedAt { get; } = ValidateTimestamp(LastEvaluatedAt);

    public string ActionClassification { get; } = ValidateActionClassification(
        ActionClassification ?? InferActionClassification(ActionName),
        nameof(ActionClassification));

    public bool RequiresFreshServerRecheck { get; } = ValidateExecutionGate(
        RequiresFreshServerRecheck,
        AvailabilityState,
        PreconditionState,
        FreshnessRequirementState,
        AuditRequirement,
        ActionClassification ?? InferActionClassification(ActionName));

    private static string RequireSafeText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > MaximumReasonLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Command availability text is too long.");
        }

        if (value.Any(char.IsControl))
        {
            throw new ArgumentException("Command availability text must not contain control characters.", parameterName);
        }

        if (ContainsForbiddenVocabulary(value))
        {
            throw new ArgumentException("Command availability text contains unsafe vocabulary.", parameterName);
        }

        return value;
    }

    private static string RequireSafeToken(string value, string parameterName)
    {
        value = RequireSafeText(value, parameterName);
        if (value.Length > MaximumTokenLength
            || value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '-')))
        {
            throw new ArgumentException("Command availability tokens must use safe ASCII token characters.", parameterName);
        }

        return value;
    }

    private static string ValidateActionClassification(string value, string parameterName)
    {
        value = RequireSafeToken(value, parameterName);
        return value is ReadOnlyActionClassification or GovernanceChangingActionClassification
            ? value
            : throw new ArgumentException("Unsupported command action classification.", parameterName);
    }

    private static string InferActionClassification(string actionName)
        => string.Equals(actionName, "read-governed-record", StringComparison.Ordinal)
            ? ReadOnlyActionClassification
            : GovernanceChangingActionClassification;

    private static bool ValidateExecutionGate(
        bool requiresFreshServerRecheck,
        ProjectionTrustState availabilityState,
        ProjectionTrustState preconditionState,
        ProjectionTrustState freshnessRequirementState,
        ConversationAuditReadinessState auditRequirement,
        string actionClassification)
    {
        ArgumentNullException.ThrowIfNull(availabilityState);
        ArgumentNullException.ThrowIfNull(preconditionState);
        ArgumentNullException.ThrowIfNull(freshnessRequirementState);
        ArgumentNullException.ThrowIfNull(auditRequirement);
        actionClassification = ValidateActionClassification(actionClassification, nameof(actionClassification));

        if (!requiresFreshServerRecheck)
        {
            throw new ArgumentException(
                "Command metadata must require a fresh server recheck.",
                nameof(requiresFreshServerRecheck));
        }

        if (availabilityState != ProjectionTrustState.Current)
        {
            return true;
        }

        if (preconditionState != ProjectionTrustState.Current || freshnessRequirementState != ProjectionTrustState.Current)
        {
            throw new ArgumentException(
                "Available command metadata requires current precondition and freshness states.",
                nameof(availabilityState));
        }

        if (actionClassification == GovernanceChangingActionClassification
            && auditRequirement != ConversationAuditReadinessState.Ready)
        {
            throw new ArgumentException(
                "Available governance-changing command metadata requires ready audit evidence.",
                nameof(auditRequirement));
        }

        return true;
    }

    private static bool ContainsForbiddenVocabulary(string value)
    {
        string normalized = NormalizeVocabulary(value);
        return ForbiddenVocabulary.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase))
            || NormalizedForbiddenVocabulary.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }

    private static string NormalizeVocabulary(string value)
        => string.Concat(value.Where(char.IsAsciiLetterOrDigit)).ToLowerInvariant();

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset value)
    {
        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.");
        }

        return value;
    }
}
