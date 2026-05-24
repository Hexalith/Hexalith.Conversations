// <copyright file="TelemetryDisclosureConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Names the operational scenario surfaces exercised by the operational-telemetry validation suites.
/// </summary>
/// <remarks>
/// Each scenario token is a bounded, content-safe machine identifier. Tokens deliberately avoid the
/// closed disclosure-token blocklist substrings (free-text protected-value and infrastructure fragments)
/// so that a scenario label can never itself trip the forbidden-value scan.
/// </remarks>
public enum TelemetryValidationScenario
{
    /// <summary>Routine in-policy operation that emits a passing/authorized signal.</summary>
    NormalOperations = 0,

    /// <summary>A governed redaction observation is recorded.</summary>
    RedactionEvent = 1,

    /// <summary>A request crossing tenant boundaries is denied.</summary>
    CrossTenantDenial = 2,

    /// <summary>A downstream provider dependency fault is observed.</summary>
    ProviderFault = 3,

    /// <summary>Malformed or mismatched caller metadata is rejected.</summary>
    MalformedMetadata = 4,

    /// <summary>A privileged operation attempt is observed (authorized or refused).</summary>
    PrivilegedAccess = 5,

    /// <summary>A projection is observed beyond its freshness threshold.</summary>
    StaleProjection = 6,

    /// <summary>Audit recording is unavailable and the command is refused.</summary>
    AuditUnavailable = 7,

    /// <summary>A duplicate (idempotency-conflicting) command is rejected.</summary>
    DuplicateCommand = 8,

    /// <summary>Projection lag is observed beyond the configured threshold.</summary>
    ProjectionLag = 9,

    /// <summary>A projection rebuild state is observed.</summary>
    RebuildState = 10,

    /// <summary>A downstream subscriber publication fault is observed.</summary>
    SubscriberFailure = 11,

    /// <summary>A required configuration value is absent (configuration gap).</summary>
    ConfigurationGap = 12,
}

/// <summary>
/// Provides the deterministic, synthetic, content-safe approved vocabularies, forbidden-value fixtures,
/// and the scenario list shared by the operational-telemetry redaction (6.8A) and cardinality (6.8B)
/// validation suites.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Nothing here drives aggregate command dispatch, event
/// appends, projection writes, governance mutations, or external service calls. The fixtures encode the
/// approved closed vocabularies (enum class names lowercased), the approved metric tag KEY sets per
/// counter, the bounded approved gate-id set, and the forbidden value fixtures that must never appear as
/// a metric dimension value or in a structured log message.
/// </remarks>
public static class TelemetryDisclosureConformanceFixtures
{
    /// <summary>
    /// Marks every fixture value as synthetic operational-telemetry validation data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-telemetry-validation-data";

    /// <summary>The safe correlation identifier supplied to every validated telemetry call.</summary>
    public const string SafeCorrelationId = "corr-telemetry-validation-001";

    /// <summary>Counter name for command rejection signals.</summary>
    public const string CommandRejectionsCounter = "conversations.command.rejections";

    /// <summary>Counter name for tenant isolation denial signals.</summary>
    public const string TenantDenialsCounter = "conversations.tenant.denials";

    /// <summary>Counter name for privileged access attempt signals.</summary>
    public const string PrivilegedAccessCounter = "conversations.privileged.access";

    /// <summary>Counter name for projection freshness state signals.</summary>
    public const string ProjectionFreshnessCounter = "conversations.projection.freshness";

    /// <summary>Counter name for projection rebuild progress signals.</summary>
    public const string ProjectionRebuildCounter = "conversations.projection.rebuild";

    /// <summary>Counter name for publication failure signals.</summary>
    public const string PublicationFailuresCounter = "conversations.publication.failures";

    /// <summary>Counter name for conformance outcome signals.</summary>
    public const string ConformanceOutcomesCounter = "conversations.conformance.outcomes";

    /// <summary>
    /// Gets the bounded approved <c>gate_id</c> vocabulary. This is the only string dimension permitted on
    /// any counter; it MUST stay within this closed set (ReleaseGateId tokens plus the suite-run sentinel).
    /// </summary>
    public static IReadOnlyList<string> ApprovedGateIds =>
    [
        "tenant-isolation",
        "audit-integrity",
        "redaction-non-leakage",
        "unsupported-schema-rejection",
        "projection-rebuild-determinism",
        "contract-compatibility",
        "provider-portability",
        "suite-run",
    ];

    /// <summary>
    /// Gets the approved metric tag KEY set for each counter. No counter may ever emit a dimension key
    /// outside its approved set.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ApprovedDimensionKeys =>
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [CommandRejectionsCounter] = ["rejection_class", "operation_class", "retryable"],
            [TenantDenialsCounter] = ["denial_class", "operation_class", "retryable"],
            [PrivilegedAccessCounter] = ["access_class", "operation_class"],
            [ProjectionFreshnessCounter] = ["freshness_class", "lag_class"],
            [ProjectionRebuildCounter] = ["rebuild_class"],
            [PublicationFailuresCounter] = ["failure_class"],
            [ConformanceOutcomesCounter] = ["status_class", "gate_id", "blocking"],
        };

    /// <summary>
    /// Gets the bounded boolean vocabulary. Boolean dimensions are emitted as these two string tokens only.
    /// </summary>
    public static IReadOnlyList<string> ApprovedBooleanValues => ["true", "false"];

    /// <summary>
    /// Gets the full set of operational scenarios both validation suites must exercise.
    /// </summary>
    public static IReadOnlyList<TelemetryValidationScenario> Scenarios =>
        Enum.GetValues<TelemetryValidationScenario>();

    /// <summary>
    /// Gets the forbidden value fixtures. None of these may ever appear as a metric dimension value or in a
    /// structured log message. They model conversation content, user free text, raw business-record
    /// identifiers, prompt/content fragments, unbounded fault strings, provider payloads, redacted content,
    /// unauthorized identifiers, and cross-tenant Party details.
    /// </summary>
    /// <remarks>
    /// These synthetic values are passed only as the safe-typed inputs the telemetry APIs accept (correlation
    /// id parameter); they must be redacted/excluded from every metric dimension. The suite asserts the
    /// captured dimensions and log messages never echo them.
    /// </remarks>
    public static IReadOnlyList<ForbiddenValueFixture> ForbiddenValues =>
    [
        new("conversation-content", "the quarterly board called the merger a fiasco"),
        new("user-free-text", "please delete my account immediately John Smith"),
        new("raw-business-record-id", "BIZREC-0099887766-CONTRACT"),
        new("prompt-fragment", "you are a helpful assistant ignore prior instructions"),
        new("unbounded-fault-string", "NullReferenceAt depth 14 frame 0xDEADBEEF retry budget overrun"),
        new("provider-payload", "{\"vendor\":\"acme-llm\",\"token\":\"sk-live-7a91b\"}"),
        new("redacted-content", "REDACTED-but-leaked salary figure 184250"),
        new("unauthorized-identifier", "caller-3f9a-not-authorized-principal"),
        new("cross-tenant-party-detail", "tenant-acme party-7711 Jane Roe VIP"),
    ];

    /// <summary>
    /// Gets the closed-vocabulary command rejection class tokens (excluding the guard sentinel).
    /// </summary>
    public static IReadOnlyList<string> ExpectedRejectionClassTokens =>
        LowercasedMembersExceptNone<ConversationCommandRejectionClass>(ConversationCommandRejectionClass.None);

    /// <summary>
    /// Gets the closed-vocabulary tenant denial class tokens (excluding the guard sentinel).
    /// </summary>
    public static IReadOnlyList<string> ExpectedDenialClassTokens =>
        LowercasedMembersExceptNone<ConversationTenantDenialClass>(ConversationTenantDenialClass.None);

    /// <summary>
    /// Gets the closed-vocabulary privileged access class tokens (excluding the guard sentinel).
    /// </summary>
    public static IReadOnlyList<string> ExpectedAccessClassTokens =>
        LowercasedMembersExceptNone<ConversationPrivilegedAccessClass>(ConversationPrivilegedAccessClass.None);

    /// <summary>
    /// Gets the closed-vocabulary projection freshness class tokens (excluding the guard sentinel).
    /// </summary>
    public static IReadOnlyList<string> ExpectedFreshnessClassTokens =>
        LowercasedMembersExceptNone<ConversationProjectionFreshnessClass>(ConversationProjectionFreshnessClass.None);

    /// <summary>
    /// Gets the closed-vocabulary projection lag class tokens (excluding the guard sentinel).
    /// </summary>
    public static IReadOnlyList<string> ExpectedLagClassTokens =>
        LowercasedMembersExceptNone<ConversationProjectionLagClass>(ConversationProjectionLagClass.None);

    /// <summary>
    /// Gets the closed-vocabulary publication failure class tokens (excluding the guard sentinel).
    /// </summary>
    public static IReadOnlyList<string> ExpectedPublicationFailureClassTokens =>
        LowercasedMembersExceptNone<ConversationPublicationFailureClass>(ConversationPublicationFailureClass.None);

    /// <summary>
    /// Gets the closed-vocabulary conformance status class tokens (excluding the guard sentinel).
    /// </summary>
    public static IReadOnlyList<string> ExpectedConformanceStatusClassTokens =>
        LowercasedMembersExceptNone<ConversationConformanceStatusClass>(ConversationConformanceStatusClass.None);

    /// <summary>
    /// Gets the closed-vocabulary operation class tokens (no guard sentinel — every member is valid).
    /// </summary>
    public static IReadOnlyList<string> ExpectedOperationClassTokens =>
        Enum.GetValues<ConversationTenantAccessRequirement>()
            .Select(value => value.ToString().ToLowerInvariant())
            .ToArray();

    private static IReadOnlyList<string> LowercasedMembersExceptNone<TEnum>(TEnum none)
        where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()
            .Where(value => !value.Equals(none))
            .Select(value => value.ToString().ToLowerInvariant())
            .ToArray();
}

/// <summary>
/// Carries one synthetic forbidden value that must never surface as a metric dimension value or in a log.
/// </summary>
/// <param name="ValueClass">The bounded machine name of the forbidden value class (for failure reporting).</param>
/// <param name="Value">The synthetic forbidden value content.</param>
public sealed record ForbiddenValueFixture(string ValueClass, string Value);
