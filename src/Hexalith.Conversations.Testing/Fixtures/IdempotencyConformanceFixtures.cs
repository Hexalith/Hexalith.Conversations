// <copyright file="IdempotencyConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Carries the deterministic synthetic scenario data for one idempotency conformance check.
/// </summary>
/// <param name="ScenarioToken">The bounded safe machine-readable scenario identifier (no real tenant IDs, Party IDs, or conversation IDs).</param>
/// <param name="ExpectedOutcome">The expected conformance outcome for this scenario.</param>
/// <param name="ExpectedClassification">The expected failure classification (conformant for all 8 scenarios).</param>
/// <param name="SafeMessage">The bounded content-safe scenario description (≤512 chars, no protected identifiers).</param>
/// <param name="ExpectedErrorCode">The expected typed error code for non-ready outcomes; null for ready outcomes.</param>
public sealed record IdempotencyScenarioData(
    string ScenarioToken,
    ConformanceOutcome ExpectedOutcome,
    ConformanceFailureClassification ExpectedClassification,
    string SafeMessage,
    ConversationErrorCode? ExpectedErrorCode = null);

/// <summary>
/// Provides the deterministic, synthetic, content-safe idempotency fixture exercised by the
/// idempotency conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Loading this fixture appends no events, mutates no
/// aggregate state, writes no projection store, and requires no nested submodule initialization.
/// The 8 scenarios cover all AC1-required idempotency cases. Classification is conformant for all
/// scenarios because the suite proves the system CORRECTLY handles each one.
/// </remarks>
public static class IdempotencyConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 8 deterministic synthetic idempotency scenario records.
    /// </summary>
    public static IReadOnlyList<IdempotencyScenarioData> Scenarios =>
    [
        new(
            "duplicate-equivalent-command",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Duplicate equivalent command produces stable idempotent outcome.",
            null),

        new(
            "duplicate-nonequivalent-command",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Duplicate non-equivalent command rejected with conflict error.",
            ConversationErrorCode.IdempotencyConflict),

        new(
            "reordered-delivery",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Reordered delivery produces stable idempotent outcome.",
            null),

        new(
            "unknown-outcome-retry",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Unknown outcome retry resolves to stable idempotent result.",
            null),

        new(
            "replayed-delivery",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Replayed delivery produces identical idempotent result.",
            null),

        new(
            "mismatched-key-reuse",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "Cross-scope key reuse hidden as aggregate-not-found to prevent side-channel disclosure.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "missing-idempotency-key",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Missing key rejected fail-closed to enforce idempotency discipline.",
            ConversationErrorCode.IdempotencyKeyMissing),

        new(
            "diagnostics-content-safety",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Diagnostic output is content-safe and contains no protected data fragments.",
            null),
    ];
}
