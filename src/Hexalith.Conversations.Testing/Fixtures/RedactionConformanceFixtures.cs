// <copyright file="RedactionConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Carries the deterministic synthetic scenario data for one redaction replay conformance check.
/// </summary>
/// <param name="ScenarioToken">The bounded safe machine-readable scenario identifier (no real tenant IDs, Party IDs, or conversation IDs).</param>
/// <param name="ExpectedOutcome">The expected conformance outcome for this scenario.</param>
/// <param name="ExpectedClassification">The expected failure classification (conformant for all 10 scenarios).</param>
/// <param name="SafeMessage">The bounded content-safe scenario description (≤512 chars, no protected identifiers).</param>
/// <param name="ExpectedErrorCode">The expected typed error code for non-ready outcomes; null for ready outcomes.</param>
public sealed record RedactionReplayScenarioData(
    string ScenarioToken,
    ConformanceOutcome ExpectedOutcome,
    ConformanceFailureClassification ExpectedClassification,
    string SafeMessage,
    ConversationErrorCode? ExpectedErrorCode = null);

/// <summary>
/// Provides the deterministic, synthetic, content-safe redaction replay fixture exercised by the
/// redaction conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Loading this fixture appends no events, mutates no
/// aggregate state, writes no projection store, and requires no nested submodule initialization.
/// The 10 scenarios cover all AC1-required redaction replay disclosure surfaces. Classification is
/// conformant for all scenarios because the suite proves the system CORRECTLY handles each one.
/// </remarks>
public static class RedactionConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 10 deterministic synthetic redaction replay scenario records.
    /// </summary>
    public static IReadOnlyList<RedactionReplayScenarioData> Scenarios =>
    [
        new(
            "projection-replay-content-safe",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Projection rebuild produces no protected values on any output surface.",
            null),

        new(
            "temporal-view-replay-hidden",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Temporal view suppresses redacted values and does not expose prior content.",
            null),

        new(
            "rebuild-replay-content-safe",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Full event replay produces no redacted value reappearance in derived outputs.",
            null),

        new(
            "audit-citation-without-exposure",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Audit evidence remains citeable without revealing redacted message content.",
            null),

        new(
            "log-trace-output-content-safe",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Logs and traces carry no redacted message fragments or protected content.",
            null),

        new(
            "error-response-content-safe",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Error responses contain no protected data fragments.",
            null),

        new(
            "stale-projection-blocked",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Stale projection blocked fail-closed to prevent protected values from reappearing.",
            ConversationErrorCode.TenantProjectionStale),

        new(
            "audit-sink-blocked",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Missing audit sink blocked fail-closed because redaction evidence is required.",
            ConversationErrorCode.AuditSinkUnavailable),

        new(
            "cross-scope-replay-hidden",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "Cross-scope replay hidden as aggregate-not-found to prevent side-channel disclosure.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "diagnostics-content-safety",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Diagnostic output is content-safe and contains no protected data fragments.",
            null),
    ];
}
