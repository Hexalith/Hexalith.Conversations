// <copyright file="ConformanceStatusConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Server.Diagnostics;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Carries the deterministic synthetic scenario data for one conformance status classification check.
/// </summary>
/// <param name="ScenarioId">The bounded safe machine-readable scenario identifier.</param>
/// <param name="ExpectedOutcome">The conformance outcome input to the classifier; null for gate-path scenarios.</param>
/// <param name="ExpectedClassification">The failure classification input to the classifier; null for gate-path scenarios.</param>
/// <param name="GateStatus">The release gate status for gate-path scenarios; null for check-level scenarios.</param>
/// <param name="ExpectedStatusClass">The expected classifier output.</param>
/// <param name="IsBlocking">Whether this conformance outcome blocks the release gate.</param>
public sealed record ConformanceStatusScenarioData(
    string ScenarioId,
    ConformanceOutcome? ExpectedOutcome,
    ConformanceFailureClassification? ExpectedClassification,
    ReleaseGateStatus? GateStatus,
    ConversationConformanceStatusClass ExpectedStatusClass,
    bool IsBlocking);

/// <summary>
/// Provides the deterministic, synthetic, content-safe conformance status fixture exercised by the
/// conformance status conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. The 10 scenarios cover all classifier mapping paths
/// including check-level (Classify) and gate-level (ClassifyGate) paths. All scenarios produce
/// conformant results when the classifier is correct — the overall suite outcome is <c>ready</c>.
/// </remarks>
public static class ConformanceStatusConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 10 deterministic synthetic conformance status scenario records.
    /// </summary>
    public static IReadOnlyList<ConformanceStatusScenarioData> Scenarios =>
    [
        new(
            "conformance-pass-gate",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            null,
            ConversationConformanceStatusClass.Pass,
            false),

        new(
            "conformance-product-invariant-fail",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.ProductInvariant,
            null,
            ConversationConformanceStatusClass.Fail,
            true),

        new(
            "conformance-infrastructure-failure",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Infrastructure,
            null,
            ConversationConformanceStatusClass.InfrastructureFailure,
            false),

        new(
            "conformance-stale-evidence",
            ConformanceOutcome.Degraded,
            ConformanceFailureClassification.Conformant,
            null,
            ConversationConformanceStatusClass.StaleEvidence,
            false),

        new(
            "conformance-execution-failure",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Execution,
            null,
            ConversationConformanceStatusClass.ExecutionFailure,
            false),

        new(
            "conformance-waived-gate",
            null,
            null,
            ReleaseGateStatus.Waived,
            ConversationConformanceStatusClass.Waived,
            false),

        new(
            "conformance-unknown-accepted",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            null,
            ConversationConformanceStatusClass.UnknownAccepted,
            false),

        new(
            "conformance-unavailable-dep",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.UnavailableDependency,
            null,
            ConversationConformanceStatusClass.InfrastructureFailure,
            false),

        new(
            "conformance-conformant-blocked",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            null,
            ConversationConformanceStatusClass.Pass,
            false),

        new(
            "conformance-configuration-fail",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Configuration,
            null,
            ConversationConformanceStatusClass.ExecutionFailure,
            false),
    ];
}
