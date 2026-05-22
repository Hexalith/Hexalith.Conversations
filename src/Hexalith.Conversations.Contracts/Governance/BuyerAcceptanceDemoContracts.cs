// <copyright file="BuyerAcceptanceDemoContracts.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Contracts.Versioning;
using static Hexalith.Conversations.Contracts.Governance.BuyerAcceptanceDemoVocabulary;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Defines the bounded operations exercised by the buyer acceptance walkthrough.
/// </summary>
[JsonConverter(typeof(BuyerAcceptanceDemoStepKindJsonConverter))]
public sealed record BuyerAcceptanceDemoStepKind
{
    public static BuyerAcceptanceDemoStepKind Find { get; } = new("find");

    public static BuyerAcceptanceDemoStepKind ReadDetail { get; } = new("read-detail");

    public static BuyerAcceptanceDemoStepKind RedactionAudit { get; } = new("redaction-audit");

    public static BuyerAcceptanceDemoStepKind CitationCopy { get; } = new("citation-copy");

    public static BuyerAcceptanceDemoStepKind TemporalReconstruction { get; } = new("temporal-reconstruction");

    public static BuyerAcceptanceDemoStepKind CommandMetadata { get; } = new("command-metadata");

    public static BuyerAcceptanceDemoStepKind Verification { get; } = new("verification");

    public static BuyerAcceptanceDemoStepKind CrossTenantDenial { get; } = new("cross-tenant-denial");

    public static BuyerAcceptanceDemoStepKind EvidenceSummary { get; } = new("evidence-summary");

    private static readonly IReadOnlyDictionary<string, BuyerAcceptanceDemoStepKind> KnownValues = Known(
        Find,
        ReadDetail,
        RedactionAudit,
        CitationCopy,
        TemporalReconstruction,
        CommandMetadata,
        Verification,
        CrossTenantDenial,
        EvidenceSummary);

    private BuyerAcceptanceDemoStepKind(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static BuyerAcceptanceDemoStepKind Parse(string value)
        => ParseKnown(value, KnownValues, nameof(BuyerAcceptanceDemoStepKind));

    public override string ToString() => Value;
}

/// <summary>
/// Defines canonical synthetic fixture states for buyer acceptance.
/// </summary>
[JsonConverter(typeof(BuyerAcceptanceDemoFixtureKindJsonConverter))]
public sealed record BuyerAcceptanceDemoFixtureKind
{
    public static BuyerAcceptanceDemoFixtureKind FullTrust { get; } = new("full-trust");

    public static BuyerAcceptanceDemoFixtureKind Redacted { get; } = new("redacted");

    public static BuyerAcceptanceDemoFixtureKind Stale { get; } = new("stale");

    public static BuyerAcceptanceDemoFixtureKind MissingCitation { get; } = new("missing-citation");

    public static BuyerAcceptanceDemoFixtureKind UnresolvedParticipant { get; } = new("unresolved-participant");

    public static BuyerAcceptanceDemoFixtureKind BlockedCommand { get; } = new("blocked-command");

    public static BuyerAcceptanceDemoFixtureKind VerificationPass { get; } = new("verification-pass");

    public static BuyerAcceptanceDemoFixtureKind VerificationFailure { get; } = new("verification-failure");

    public static BuyerAcceptanceDemoFixtureKind CrossTenantPoison { get; } = new("cross-tenant-poison");

    private static readonly IReadOnlyDictionary<string, BuyerAcceptanceDemoFixtureKind> KnownValues = Known(
        FullTrust,
        Redacted,
        Stale,
        MissingCitation,
        UnresolvedParticipant,
        BlockedCommand,
        VerificationPass,
        VerificationFailure,
        CrossTenantPoison);

    private BuyerAcceptanceDemoFixtureKind(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static IReadOnlyList<BuyerAcceptanceDemoFixtureKind> Canonical { get; } =
    [
        FullTrust,
        Redacted,
        Stale,
        MissingCitation,
        UnresolvedParticipant,
        BlockedCommand,
        VerificationPass,
        VerificationFailure,
        CrossTenantPoison,
    ];

    public static BuyerAcceptanceDemoFixtureKind Parse(string value)
        => ParseKnown(value, KnownValues, nameof(BuyerAcceptanceDemoFixtureKind));

    public override string ToString() => Value;
}

/// <summary>
/// Defines the trust posture expected by a demo step or fixture.
/// </summary>
[JsonConverter(typeof(BuyerAcceptanceDemoTrustStateJsonConverter))]
public sealed record BuyerAcceptanceDemoTrustState
{
    public static BuyerAcceptanceDemoTrustState Current { get; } = new("current");

    public static BuyerAcceptanceDemoTrustState Redacted { get; } = new("redacted");

    public static BuyerAcceptanceDemoTrustState Stale { get; } = new("stale");

    public static BuyerAcceptanceDemoTrustState Incomplete { get; } = new("incomplete");

    public static BuyerAcceptanceDemoTrustState Unavailable { get; } = new("unavailable");

    public static BuyerAcceptanceDemoTrustState Hidden { get; } = new("hidden");

    public static BuyerAcceptanceDemoTrustState Failed { get; } = new("failed");

    private static readonly IReadOnlyDictionary<string, BuyerAcceptanceDemoTrustState> KnownValues = Known(
        Current,
        Redacted,
        Stale,
        Incomplete,
        Unavailable,
        Hidden,
        Failed);

    private BuyerAcceptanceDemoTrustState(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static BuyerAcceptanceDemoTrustState Parse(string value)
        => ParseKnown(value, KnownValues, nameof(BuyerAcceptanceDemoTrustState));

    public override string ToString() => Value;
}

/// <summary>
/// Defines the owner of evidence represented in the acceptance summary.
/// </summary>
[JsonConverter(typeof(BuyerAcceptanceEvidenceOwnershipJsonConverter))]
public sealed record BuyerAcceptanceEvidenceOwnership
{
    public static BuyerAcceptanceEvidenceOwnership Module { get; } = new("module");

    public static BuyerAcceptanceEvidenceOwnership InheritedPlatformControl { get; } = new("inherited-platform-control");

    public static BuyerAcceptanceEvidenceOwnership NotApplicable { get; } = new("not-applicable");

    public static BuyerAcceptanceEvidenceOwnership Waived { get; } = new("waived");

    private static readonly IReadOnlyDictionary<string, BuyerAcceptanceEvidenceOwnership> KnownValues = Known(
        Module,
        InheritedPlatformControl,
        NotApplicable,
        Waived);

    private BuyerAcceptanceEvidenceOwnership(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static BuyerAcceptanceEvidenceOwnership Parse(string value)
        => ParseKnown(value, KnownValues, nameof(BuyerAcceptanceEvidenceOwnership));

    public override string ToString() => Value;
}

/// <summary>
/// Defines bounded pass/fail states for demo execution.
/// </summary>
[JsonConverter(typeof(BuyerAcceptanceDemoExecutionStatusJsonConverter))]
public sealed record BuyerAcceptanceDemoExecutionStatus
{
    public static BuyerAcceptanceDemoExecutionStatus Passed { get; } = new("passed");

    public static BuyerAcceptanceDemoExecutionStatus Failed { get; } = new("failed");

    public static BuyerAcceptanceDemoExecutionStatus Partial { get; } = new("partial");

    public static BuyerAcceptanceDemoExecutionStatus Blocked { get; } = new("blocked");

    private static readonly IReadOnlyDictionary<string, BuyerAcceptanceDemoExecutionStatus> KnownValues = Known(
        Passed,
        Failed,
        Partial,
        Blocked);

    private BuyerAcceptanceDemoExecutionStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static BuyerAcceptanceDemoExecutionStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(BuyerAcceptanceDemoExecutionStatus));

    public override string ToString() => Value;
}

/// <summary>
/// Describes one synthetic fixture available to the buyer acceptance walkthrough.
/// </summary>
public sealed record BuyerAcceptanceDemoFixtureV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    string FixtureId,
    BuyerAcceptanceDemoFixtureKind FixtureKind,
    BuyerAcceptanceDemoTrustState ExpectedTrustState,
    string SyntheticDataMarker,
    string SafeLabel,
    string SafeNextAction,
    ConversationId? ConversationId = null,
    IReadOnlyList<string>? RequirementMappings = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public string FixtureId { get; } = GovernanceContractValidation.RequiredSafeToken(FixtureId, nameof(FixtureId));

    public BuyerAcceptanceDemoFixtureKind FixtureKind { get; } =
        FixtureKind ?? throw new ArgumentNullException(nameof(FixtureKind));

    public BuyerAcceptanceDemoTrustState ExpectedTrustState { get; } =
        ExpectedTrustState ?? throw new ArgumentNullException(nameof(ExpectedTrustState));

    public string SyntheticDataMarker { get; } =
        GovernanceContractValidation.RequiredSafeToken(SyntheticDataMarker, nameof(SyntheticDataMarker));

    public string SafeLabel { get; } = GovernanceContractValidation.RequiredSafeText(SafeLabel, nameof(SafeLabel));

    public string SafeNextAction { get; } = GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));

    public ConversationId? ConversationId { get; } = ConversationId;

    public IReadOnlyList<string> RequirementMappings { get; } = ValidateRequirementMappings(RequirementMappings);
}

/// <summary>
/// Describes one deterministic step in the buyer acceptance walkthrough.
/// </summary>
public sealed record BuyerAcceptanceDemoStepV1(
    SchemaVersion SchemaVersion,
    string StepId,
    BuyerAcceptanceDemoStepKind StepKind,
    BuyerAcceptanceDemoFixtureKind FixtureKind,
    BuyerAcceptanceDemoTrustState ExpectedTrustState,
    string SafeLabel,
    string SafeNextAction,
    IReadOnlyList<string> RequirementMappings,
    ConversationId? ConversationId = null,
    BusinessReference? BusinessReference = null,
    string? EvidenceEntryId = null,
    AuditEvidenceHandle? AuditEvidenceHandle = null,
    string? TemporalCursor = null,
    IReadOnlyList<ConversationGovernanceVerificationEvidenceHandle>? EvidenceHandles = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public string StepId { get; } = GovernanceContractValidation.RequiredSafeToken(StepId, nameof(StepId));

    public BuyerAcceptanceDemoStepKind StepKind { get; } = StepKind ?? throw new ArgumentNullException(nameof(StepKind));

    public BuyerAcceptanceDemoFixtureKind FixtureKind { get; } =
        FixtureKind ?? throw new ArgumentNullException(nameof(FixtureKind));

    public BuyerAcceptanceDemoTrustState ExpectedTrustState { get; } =
        ExpectedTrustState ?? throw new ArgumentNullException(nameof(ExpectedTrustState));

    public string SafeLabel { get; } = GovernanceContractValidation.RequiredSafeText(SafeLabel, nameof(SafeLabel));

    public string SafeNextAction { get; } = GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));

    public IReadOnlyList<string> RequirementMappings { get; } = ValidateRequirementMappings(RequirementMappings);

    public ConversationId? ConversationId { get; } = ConversationId;

    public BusinessReference? BusinessReference { get; } = BusinessReference;

    public string? EvidenceEntryId { get; } =
        GovernanceContractValidation.OptionalSafeToken(EvidenceEntryId, nameof(EvidenceEntryId));

    public AuditEvidenceHandle? AuditEvidenceHandle { get; } = AuditEvidenceHandle;

    public string? TemporalCursor { get; } = ValidateOptionalTemporalCursor(TemporalCursor);

    public IReadOnlyList<ConversationGovernanceVerificationEvidenceHandle> EvidenceHandles { get; } =
        ValidateOptionalItems(EvidenceHandles);
}

/// <summary>
/// Carries a deterministic buyer acceptance scenario manifest.
/// </summary>
public sealed record BuyerAcceptanceDemoScenarioV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    string ScenarioId,
    string SyntheticDataMarker,
    string SafeLabel,
    string CorrelationId,
    IReadOnlyList<BuyerAcceptanceDemoFixtureV1> Fixtures,
    IReadOnlyList<BuyerAcceptanceDemoStepV1> Steps,
    IReadOnlyList<string> RequirementMappings)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public string ScenarioId { get; } = GovernanceContractValidation.RequiredSafeToken(ScenarioId, nameof(ScenarioId));

    public string SyntheticDataMarker { get; } =
        GovernanceContractValidation.RequiredSafeToken(SyntheticDataMarker, nameof(SyntheticDataMarker));

    public string SafeLabel { get; } = GovernanceContractValidation.RequiredSafeText(SafeLabel, nameof(SafeLabel));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public IReadOnlyList<BuyerAcceptanceDemoFixtureV1> Fixtures { get; } = ValidateFixtures(Fixtures, TenantId);

    public IReadOnlyList<BuyerAcceptanceDemoStepV1> Steps { get; } = ValidateSteps(Steps, Fixtures);

    public IReadOnlyList<string> RequirementMappings { get; } = ValidateRequirementMappings(RequirementMappings);
}

/// <summary>
/// Captures the safe outcome of one buyer acceptance step.
/// </summary>
public sealed record BuyerAcceptanceEvidenceStepResultV1(
    SchemaVersion SchemaVersion,
    string StepId,
    BuyerAcceptanceDemoStepKind StepKind,
    BuyerAcceptanceDemoExecutionStatus Status,
    BuyerAcceptanceDemoTrustState TrustState,
    BuyerAcceptanceEvidenceOwnership EvidenceOwnership,
    string SafeSummary,
    string SafeNextAction,
    IReadOnlyList<string> RequirementMappings,
    IReadOnlyList<ConversationGovernanceVerificationEvidenceHandle>? EvidenceHandles = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public string StepId { get; } = GovernanceContractValidation.RequiredSafeToken(StepId, nameof(StepId));

    public BuyerAcceptanceDemoStepKind StepKind { get; } = StepKind ?? throw new ArgumentNullException(nameof(StepKind));

    public BuyerAcceptanceDemoExecutionStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    public BuyerAcceptanceDemoTrustState TrustState { get; } = TrustState ?? throw new ArgumentNullException(nameof(TrustState));

    public BuyerAcceptanceEvidenceOwnership EvidenceOwnership { get; } =
        EvidenceOwnership ?? throw new ArgumentNullException(nameof(EvidenceOwnership));

    public string SafeSummary { get; } = GovernanceContractValidation.RequiredSafeText(SafeSummary, nameof(SafeSummary));

    public string SafeNextAction { get; } = GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));

    public IReadOnlyList<string> RequirementMappings { get; } = ValidateRequirementMappings(RequirementMappings);

    public IReadOnlyList<ConversationGovernanceVerificationEvidenceHandle> EvidenceHandles { get; } =
        ValidateOptionalItems(EvidenceHandles);
}

/// <summary>
/// Carries selected, content-safe verification output for the acceptance summary.
/// </summary>
public sealed record BuyerAcceptanceVerificationSummaryV1(
    SchemaVersion SchemaVersion,
    ConversationGovernanceVerificationSuite Suite,
    ConversationGovernanceVerificationExecutionStatus Status,
    ConversationGovernanceVerificationFailureClassification Classification,
    string SafeDetail,
    ConversationGovernanceVerificationRemediation Remediation,
    IReadOnlyList<string> RequirementMappings)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ConversationGovernanceVerificationSuite Suite { get; } = Suite ?? throw new ArgumentNullException(nameof(Suite));

    public ConversationGovernanceVerificationExecutionStatus Status { get; } =
        Status ?? throw new ArgumentNullException(nameof(Status));

    public ConversationGovernanceVerificationFailureClassification Classification { get; } =
        Classification ?? throw new ArgumentNullException(nameof(Classification));

    public string SafeDetail { get; } = GovernanceContractValidation.RequiredSafeText(SafeDetail, nameof(SafeDetail));

    public ConversationGovernanceVerificationRemediation Remediation { get; } =
        Remediation ?? throw new ArgumentNullException(nameof(Remediation));

    public IReadOnlyList<string> RequirementMappings { get; } = ValidateRequirementMappings(RequirementMappings);
}

/// <summary>
/// Summarizes buyer acceptance evidence without creating a durable authority or export artifact.
/// </summary>
public sealed record BuyerAcceptanceEvidenceSummaryV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    string ScenarioId,
    string SyntheticDataMarker,
    DateTimeOffset GeneratedAtUtc,
    string RunnerId,
    string CorrelationId,
    BuyerAcceptanceDemoExecutionStatus Status,
    IReadOnlyList<BuyerAcceptanceEvidenceStepResultV1> StepResults,
    IReadOnlyList<BuyerAcceptanceVerificationSummaryV1> VerificationOutput,
    IReadOnlyList<string> RequirementMappings,
    string SafeSummary,
    IReadOnlyList<BuyerAcceptanceEvidenceOwnership> EvidenceScope)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public string ScenarioId { get; } = GovernanceContractValidation.RequiredSafeToken(ScenarioId, nameof(ScenarioId));

    public string SyntheticDataMarker { get; } =
        GovernanceContractValidation.RequiredSafeToken(SyntheticDataMarker, nameof(SyntheticDataMarker));

    public DateTimeOffset GeneratedAtUtc { get; } =
        GovernanceContractValidation.RequiredUtcTimestamp(GeneratedAtUtc, nameof(GeneratedAtUtc));

    public string RunnerId { get; } = GovernanceContractValidation.RequiredSafeToken(RunnerId, nameof(RunnerId));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public BuyerAcceptanceDemoExecutionStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    public IReadOnlyList<BuyerAcceptanceEvidenceStepResultV1> StepResults { get; } = ValidateStepResults(StepResults);

    public IReadOnlyList<BuyerAcceptanceVerificationSummaryV1> VerificationOutput { get; } =
        ValidateOptionalItems(VerificationOutput);

    public IReadOnlyList<string> RequirementMappings { get; } = ValidateRequirementMappings(RequirementMappings);

    public string SafeSummary { get; } = GovernanceContractValidation.RequiredSafeText(SafeSummary, nameof(SafeSummary));

    public IReadOnlyList<BuyerAcceptanceEvidenceOwnership> EvidenceScope { get; } = ValidateEvidenceScope(EvidenceScope);
}

file static class BuyerAcceptanceDemoVocabulary
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

    internal static IReadOnlyList<T> ValidateOptionalItems<T>(IReadOnlyList<T>? values)
        where T : class
        => values is null ? [] : ValidateItems(values, nameof(values), requireNonEmpty: false);

    internal static IReadOnlyList<string> ValidateRequirementMappings(IReadOnlyList<string>? values)
    {
        string[] mapped = ValidateItems(values, nameof(values), requireNonEmpty: true)
            .Select(value => GovernanceContractValidation.RequiredSafeToken(value, nameof(values)))
            .ToArray();

        if (mapped.Distinct(StringComparer.Ordinal).Count() != mapped.Length)
        {
            throw new ArgumentException("Requirement mappings must be unique.", nameof(values));
        }

        return mapped;
    }

    internal static IReadOnlyList<BuyerAcceptanceDemoFixtureV1> ValidateFixtures(
        IReadOnlyList<BuyerAcceptanceDemoFixtureV1>? values,
        TenantId scenarioTenantId)
    {
        BuyerAcceptanceDemoFixtureV1[] fixtures = ValidateItems(values, nameof(values), requireNonEmpty: true);
        if (fixtures.Select(fixture => fixture.FixtureId).Distinct(StringComparer.Ordinal).Count() != fixtures.Length)
        {
            throw new ArgumentException("Fixture ids must be unique.", nameof(values));
        }

        if (fixtures.Any(fixture => fixture.TenantId != scenarioTenantId))
        {
            throw new ArgumentException("Demo fixtures must use the scenario tenant scope.", nameof(values));
        }

        return fixtures;
    }

    internal static IReadOnlyList<BuyerAcceptanceDemoStepV1> ValidateSteps(
        IReadOnlyList<BuyerAcceptanceDemoStepV1>? values,
        IReadOnlyList<BuyerAcceptanceDemoFixtureV1> fixtures)
    {
        BuyerAcceptanceDemoStepV1[] steps = ValidateItems(values, nameof(values), requireNonEmpty: true);
        if (steps.Select(step => step.StepId).Distinct(StringComparer.Ordinal).Count() != steps.Length)
        {
            throw new ArgumentException("Step ids must be unique.", nameof(values));
        }

        HashSet<string> declaredFixtureKinds = fixtures
            .Select(fixture => fixture.FixtureKind.Value)
            .ToHashSet(StringComparer.Ordinal);
        if (steps.Any(step => !declaredFixtureKinds.Contains(step.FixtureKind.Value)))
        {
            throw new ArgumentException("Demo steps must reference declared fixture kinds.", nameof(values));
        }

        return steps;
    }

    internal static string? ValidateOptionalTemporalCursor(string? value)
    {
        if (value is null)
        {
            return null;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        if (value.Length > 128 || value.Any(char.IsControl))
        {
            throw new ArgumentException("Temporal cursor must be a bounded content-safe cursor.", nameof(value));
        }

        string[] parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6
            || !string.Equals(parts[0], "temporal", StringComparison.Ordinal)
            || !string.Equals(parts[1], "v1", StringComparison.Ordinal)
            || !string.Equals(parts[2], "pos", StringComparison.Ordinal)
            || !long.TryParse(parts[3], out long position)
            || position < 1
            || !string.Equals(parts[4], "projection", StringComparison.Ordinal)
            || !long.TryParse(parts[5], out long projectionVersion)
            || projectionVersion < 1)
        {
            throw new ArgumentException("Temporal cursor must use the composite temporal v1 cursor shape.", nameof(value));
        }

        return value;
    }

    internal static IReadOnlyList<BuyerAcceptanceEvidenceStepResultV1> ValidateStepResults(
        IReadOnlyList<BuyerAcceptanceEvidenceStepResultV1>? values)
    {
        BuyerAcceptanceEvidenceStepResultV1[] results = ValidateItems(values, nameof(values), requireNonEmpty: true);
        if (results.Select(step => step.StepId).Distinct(StringComparer.Ordinal).Count() != results.Length)
        {
            throw new ArgumentException("Step result ids must be unique.", nameof(values));
        }

        return results;
    }

    internal static IReadOnlyList<BuyerAcceptanceEvidenceOwnership> ValidateEvidenceScope(
        IReadOnlyList<BuyerAcceptanceEvidenceOwnership>? values)
    {
        BuyerAcceptanceEvidenceOwnership[] scope = ValidateItems(values, nameof(values), requireNonEmpty: true);
        if (scope.Select(item => item.Value).Distinct(StringComparer.Ordinal).Count() != scope.Length)
        {
            throw new ArgumentException("Evidence scope entries must be unique.", nameof(values));
        }

        return scope;
    }

    private static T[] ValidateItems<T>(IReadOnlyList<T>? values, string parameterName, bool requireNonEmpty)
        where T : class
    {
        if (values is null || (requireNonEmpty && values.Count == 0) || values.Any(value => value is null))
        {
            throw new ArgumentException("Collection must contain valid items.", parameterName);
        }

        return values.ToArray();
    }

    private static bool IsVocabularyCharacter(char value)
        => (value >= 'a' && value <= 'z') || char.IsAsciiDigit(value) || value is '-';
}
