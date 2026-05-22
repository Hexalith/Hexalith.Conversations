// <copyright file="ConversationGovernanceVerificationContracts.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Contracts.Versioning;
using static Hexalith.Conversations.Contracts.Governance.ConversationGovernanceVerificationVocabulary;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Defines the authorized verification boundary selected by the server.
/// </summary>
[JsonConverter(typeof(ConversationGovernanceVerificationScopeKindJsonConverter))]
public sealed record ConversationGovernanceVerificationScopeKind
{
    public static ConversationGovernanceVerificationScopeKind Conversation { get; } = new("conversation");

    public static ConversationGovernanceVerificationScopeKind Tenant { get; } = new("tenant");

    public static ConversationGovernanceVerificationScopeKind Suite { get; } = new("suite");

    public static ConversationGovernanceVerificationScopeKind TimeWindow { get; } = new("time-window");

    private static readonly IReadOnlyDictionary<string, ConversationGovernanceVerificationScopeKind> KnownValues = Known(
        Conversation,
        Tenant,
        Suite,
        TimeWindow);

    private ConversationGovernanceVerificationScopeKind(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static ConversationGovernanceVerificationScopeKind Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConversationGovernanceVerificationScopeKind));

    public override string ToString() => Value;
}

/// <summary>
/// Defines the closed set of v1 governance verification suites.
/// </summary>
[JsonConverter(typeof(ConversationGovernanceVerificationSuiteJsonConverter))]
public sealed record ConversationGovernanceVerificationSuite
{
    public static ConversationGovernanceVerificationSuite AuditPairing { get; } = new("audit-pairing");

    public static ConversationGovernanceVerificationSuite TenantIsolation { get; } = new("tenant-isolation");

    public static ConversationGovernanceVerificationSuite RedactionReplay { get; } = new("redaction-replay");

    public static ConversationGovernanceVerificationSuite ProjectionRebuild { get; } = new("projection-rebuild");

    public static ConversationGovernanceVerificationSuite ProviderPortability { get; } = new("provider-portability");

    public static ConversationGovernanceVerificationSuite SchemaCompatibility { get; } = new("schema-compatibility");

    private static readonly IReadOnlyDictionary<string, ConversationGovernanceVerificationSuite> KnownValues = Known(
        AuditPairing,
        TenantIsolation,
        RedactionReplay,
        ProjectionRebuild,
        ProviderPortability,
        SchemaCompatibility);

    private ConversationGovernanceVerificationSuite(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static IReadOnlyList<ConversationGovernanceVerificationSuite> All { get; } =
    [
        AuditPairing,
        TenantIsolation,
        RedactionReplay,
        ProjectionRebuild,
        ProviderPortability,
        SchemaCompatibility,
    ];

    public static ConversationGovernanceVerificationSuite Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConversationGovernanceVerificationSuite));

    public override string ToString() => Value;
}

/// <summary>
/// Defines bounded execution states for a verification run.
/// </summary>
[JsonConverter(typeof(ConversationGovernanceVerificationExecutionStatusJsonConverter))]
public sealed record ConversationGovernanceVerificationExecutionStatus
{
    public static ConversationGovernanceVerificationExecutionStatus Completed { get; } = new("completed");

    public static ConversationGovernanceVerificationExecutionStatus Blocked { get; } = new("blocked");

    public static ConversationGovernanceVerificationExecutionStatus Partial { get; } = new("partial");

    public static ConversationGovernanceVerificationExecutionStatus Failed { get; } = new("failed");

    public static ConversationGovernanceVerificationExecutionStatus NotApplicable { get; } = new("not-applicable");

    private static readonly IReadOnlyDictionary<string, ConversationGovernanceVerificationExecutionStatus> KnownValues = Known(
        Completed,
        Blocked,
        Partial,
        Failed,
        NotApplicable);

    private ConversationGovernanceVerificationExecutionStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static ConversationGovernanceVerificationExecutionStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConversationGovernanceVerificationExecutionStatus));

    public override string ToString() => Value;
}

/// <summary>
/// Classifies verification outcomes without collapsing product failures into execution failures.
/// </summary>
[JsonConverter(typeof(ConversationGovernanceVerificationFailureClassificationJsonConverter))]
public sealed record ConversationGovernanceVerificationFailureClassification
{
    public static ConversationGovernanceVerificationFailureClassification Passed { get; } = new("passed");

    public static ConversationGovernanceVerificationFailureClassification GovernanceFailed { get; } = new("governance-failed");

    public static ConversationGovernanceVerificationFailureClassification InfrastructureFailed { get; } = new("infrastructure-failed");

    public static ConversationGovernanceVerificationFailureClassification DependencyUnavailable { get; } = new("dependency-unavailable");

    public static ConversationGovernanceVerificationFailureClassification DataUnavailable { get; } = new("data-unavailable");

    public static ConversationGovernanceVerificationFailureClassification StaleProjection { get; } = new("stale-projection");

    public static ConversationGovernanceVerificationFailureClassification UnsupportedVersion { get; } = new("unsupported-version");

    public static ConversationGovernanceVerificationFailureClassification UnauthorizedOrHidden { get; } = new("unauthorized-or-hidden");

    public static ConversationGovernanceVerificationFailureClassification ExecutionFailed { get; } = new("execution-failed");

    public static ConversationGovernanceVerificationFailureClassification NotApplicable { get; } = new("not-applicable");

    private static readonly IReadOnlyDictionary<string, ConversationGovernanceVerificationFailureClassification> KnownValues = Known(
        Passed,
        GovernanceFailed,
        InfrastructureFailed,
        DependencyUnavailable,
        DataUnavailable,
        StaleProjection,
        UnsupportedVersion,
        UnauthorizedOrHidden,
        ExecutionFailed,
        NotApplicable);

    private ConversationGovernanceVerificationFailureClassification(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static ConversationGovernanceVerificationFailureClassification Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConversationGovernanceVerificationFailureClassification));

    public override string ToString() => Value;
}

/// <summary>
/// Defines bounded operator remediation hints for verification outcomes.
/// </summary>
[JsonConverter(typeof(ConversationGovernanceVerificationRemediationJsonConverter))]
public sealed record ConversationGovernanceVerificationRemediation
{
    public static ConversationGovernanceVerificationRemediation None { get; } = new("none");

    public static ConversationGovernanceVerificationRemediation RetryLater { get; } = new("retry-later");

    public static ConversationGovernanceVerificationRemediation RequestAuthorization { get; } = new("request-authorization");

    public static ConversationGovernanceVerificationRemediation RefreshDerivedEvidence { get; } = new("refresh-derived-evidence");

    public static ConversationGovernanceVerificationRemediation MigrateSchema { get; } = new("migrate-schema");

    public static ConversationGovernanceVerificationRemediation InspectGovernanceEvidence { get; } = new("inspect-governance-evidence");

    public static ConversationGovernanceVerificationRemediation ProvideVerifyJustification { get; } = new("provide-verify-justification");

    private static readonly IReadOnlyDictionary<string, ConversationGovernanceVerificationRemediation> KnownValues = Known(
        None,
        RetryLater,
        RequestAuthorization,
        RefreshDerivedEvidence,
        MigrateSchema,
        InspectGovernanceEvidence,
        ProvideVerifyJustification);

    private ConversationGovernanceVerificationRemediation(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static ConversationGovernanceVerificationRemediation Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConversationGovernanceVerificationRemediation));

    public override string ToString() => Value;
}

/// <summary>
/// Carries a content-safe evidence reference for derived verification proof.
/// </summary>
public sealed record ConversationGovernanceVerificationEvidenceHandle(string Value)
{
    public string Value { get; } = GovernanceContractValidation.RequiredSafeToken(Value, nameof(Value));
}

/// <summary>
/// Describes the tenant-safe verification scope selected by the server boundary.
/// </summary>
public sealed record ConversationGovernanceVerificationScopeV1(
    SchemaVersion SchemaVersion,
    ConversationGovernanceVerificationScopeKind ScopeKind,
    TenantId TenantId,
    ConversationId? ConversationId = null,
    DateTimeOffset? RequestedFromUtc = null,
    DateTimeOffset? RequestedToUtc = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ConversationGovernanceVerificationScopeKind ScopeKind { get; } =
        ScopeKind ?? throw new ArgumentNullException(nameof(ScopeKind));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public ConversationId? ConversationId { get; } = ConversationId;

    public DateTimeOffset? RequestedFromUtc { get; } = ValidateTimestamp(RequestedFromUtc, nameof(RequestedFromUtc));

    public DateTimeOffset? RequestedToUtc { get; } = ValidateToTimestamp(RequestedFromUtc, RequestedToUtc);

    private static DateTimeOffset? ValidateTimestamp(DateTimeOffset? value, string parameterName)
        => value is null
            ? null
            : GovernanceContractValidation.RequiredUtcTimestamp(value.Value, parameterName);

    private static DateTimeOffset? ValidateToTimestamp(DateTimeOffset? from, DateTimeOffset? to)
    {
        DateTimeOffset? safeTo = ValidateTimestamp(to, nameof(RequestedToUtc));
        DateTimeOffset? safeFrom = ValidateTimestamp(from, nameof(RequestedFromUtc));
        if (safeFrom is not null && safeTo is not null && safeTo < safeFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(RequestedToUtc), "Verification time windows must not end before they start.");
        }

        return safeTo;
    }
}

/// <summary>
/// Requests a verification run over an already trusted server-owned scope.
/// </summary>
public sealed record ConversationGovernanceVerificationRequestV1(
    SchemaVersion SchemaVersion,
    ConversationGovernanceVerificationScopeV1 Scope,
    IReadOnlyList<ConversationGovernanceVerificationSuite> SelectedSuites,
    string CorrelationId)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ConversationGovernanceVerificationScopeV1 Scope { get; } = Scope ?? throw new ArgumentNullException(nameof(Scope));

    public IReadOnlyList<ConversationGovernanceVerificationSuite> SelectedSuites { get; } = ValidateSuites(SelectedSuites);

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    private static IReadOnlyList<ConversationGovernanceVerificationSuite> ValidateSuites(
        IReadOnlyList<ConversationGovernanceVerificationSuite>? values)
    {
        if (values is null || values.Count == 0 || values.Any(value => value is null))
        {
            throw new ArgumentException("At least one verification suite is required.", nameof(values));
        }

        if (values.Select(value => value.Value).Distinct(StringComparer.Ordinal).Count() != values.Count)
        {
            throw new ArgumentException("Verification suites must be unique.", nameof(values));
        }

        return values.ToArray();
    }
}

/// <summary>
/// Carries the machine-readable result of one verification check.
/// </summary>
public sealed record ConversationGovernanceVerificationCheckResultV1(
    SchemaVersion SchemaVersion,
    ConversationGovernanceVerificationSuite Suite,
    string CheckName,
    IReadOnlyList<string> RequirementMappings,
    ConversationGovernanceVerificationExecutionStatus Status,
    ConversationGovernanceVerificationFailureClassification Classification,
    string SafeDetail,
    ConversationGovernanceVerificationRemediation Remediation,
    ConversationGovernanceVerificationEvidenceHandle? Evidence = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ConversationGovernanceVerificationSuite Suite { get; } = Suite ?? throw new ArgumentNullException(nameof(Suite));

    public string CheckName { get; } = ConversationGovernanceVerificationVocabulary.ValidateVocabularyValue(
        CheckName,
        nameof(CheckName));

    public IReadOnlyList<string> RequirementMappings { get; } = ValidateRequirementMappings(RequirementMappings);

    public ConversationGovernanceVerificationExecutionStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    public ConversationGovernanceVerificationFailureClassification Classification { get; } =
        Classification ?? throw new ArgumentNullException(nameof(Classification));

    public string SafeDetail { get; } = GovernanceContractValidation.RequiredSafeText(SafeDetail, nameof(SafeDetail));

    public ConversationGovernanceVerificationRemediation Remediation { get; } =
        Remediation ?? throw new ArgumentNullException(nameof(Remediation));

    public ConversationGovernanceVerificationEvidenceHandle? Evidence { get; } = Evidence;

    private static IReadOnlyList<string> ValidateRequirementMappings(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one requirement mapping is required.", nameof(values));
        }

        return values
            .Select(value => GovernanceContractValidation.RequiredSafeToken(value, nameof(values)))
            .ToArray();
    }
}

/// <summary>
/// Carries the machine-readable result of a governance verification run.
/// </summary>
public sealed record ConversationGovernanceVerificationRunResultV1(
    SchemaVersion SchemaVersion,
    ConversationGovernanceVerificationScopeV1 Scope,
    IReadOnlyList<ConversationGovernanceVerificationSuite> SelectedSuites,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId,
    ConversationGovernanceVerificationExecutionStatus Status,
    ConversationGovernanceVerificationFailureClassification Classification,
    string SafeSummary,
    IReadOnlyList<ConversationGovernanceVerificationCheckResultV1> Checks,
    GovernanceAuditEvidenceReference? AuditEvidence = null,
    string? AuditNotRecordedReason = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ConversationGovernanceVerificationScopeV1 Scope { get; } = Scope ?? throw new ArgumentNullException(nameof(Scope));

    public IReadOnlyList<ConversationGovernanceVerificationSuite> SelectedSuites { get; } = ValidateSuites(SelectedSuites);

    public DateTimeOffset GeneratedAtUtc { get; } =
        GovernanceContractValidation.RequiredUtcTimestamp(GeneratedAtUtc, nameof(GeneratedAtUtc));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public ConversationGovernanceVerificationExecutionStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    public ConversationGovernanceVerificationFailureClassification Classification { get; } =
        Classification ?? throw new ArgumentNullException(nameof(Classification));

    public string SafeSummary { get; } = GovernanceContractValidation.RequiredSafeText(SafeSummary, nameof(SafeSummary));

    public IReadOnlyList<ConversationGovernanceVerificationCheckResultV1> Checks { get; } = ValidateChecks(Checks);

    public GovernanceAuditEvidenceReference? AuditEvidence { get; } = AuditEvidence;

    public string? AuditNotRecordedReason { get; } = AuditNotRecordedReason is null
        ? null
        : GovernanceContractValidation.RequiredSafeText(AuditNotRecordedReason, nameof(AuditNotRecordedReason));

    private static IReadOnlyList<ConversationGovernanceVerificationCheckResultV1> ValidateChecks(
        IReadOnlyList<ConversationGovernanceVerificationCheckResultV1>? values)
        => values is null || values.Count == 0 || values.Any(value => value is null)
            ? throw new ArgumentException("At least one verification check result is required.", nameof(values))
            : values.ToArray();

    private static IReadOnlyList<ConversationGovernanceVerificationSuite> ValidateSuites(
        IReadOnlyList<ConversationGovernanceVerificationSuite>? values)
    {
        if (values is null || values.Count == 0 || values.Any(value => value is null))
        {
            throw new ArgumentException("At least one verification suite is required.", nameof(values));
        }

        if (values.Select(value => value.Value).Distinct(StringComparer.Ordinal).Count() != values.Count)
        {
            throw new ArgumentException("Verification suites must be unique.", nameof(values));
        }

        return values.ToArray();
    }
}

file static class ConversationGovernanceVerificationVocabulary
{
    internal static IReadOnlyDictionary<string, T> Known<T>(params T[] values)
        where T : notnull
        => values.ToDictionary(value => value.ToString() ?? string.Empty, StringComparer.Ordinal);

    internal static T ParseKnown<T>(string value, IReadOnlyDictionary<string, T> knownValues, string vocabularyName)
    {
        string safe = ValidateVocabularyValue(value, nameof(value));
        return knownValues.TryGetValue(safe, out T? known)
            ? known
            : throw new ArgumentException($"Unsupported {vocabularyName} value.", nameof(value));
    }

    internal static string ValidateVocabularyValue(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > 64 || value.Any(static c => !IsVocabularyCharacter(c)))
        {
            throw new ArgumentException("Value must be a bounded closed vocabulary token.", parameterName);
        }

        return value;
    }

    private static bool IsVocabularyCharacter(char value)
        => (value >= 'a' && value <= 'z') || char.IsAsciiDigit(value) || value is '-';
}
