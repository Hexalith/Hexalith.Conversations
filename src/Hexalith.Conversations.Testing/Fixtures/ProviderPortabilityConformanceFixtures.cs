// <copyright file="ProviderPortabilityConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Carries the deterministic synthetic scenario data for one provider portability conformance check.
/// </summary>
/// <param name="ScenarioToken">The bounded safe machine-readable scenario identifier (no real tenant IDs, Party IDs, or conversation IDs).</param>
/// <param name="ExpectedOutcome">The expected conformance outcome for this scenario.</param>
/// <param name="ExpectedClassification">The expected failure classification (conformant for all 10 scenarios).</param>
/// <param name="SafeMessage">The bounded content-safe scenario description (≤512 chars, no protected identifiers).</param>
/// <param name="ExpectedErrorCode">The expected typed error code for non-ready outcomes; null for ready outcomes.</param>
public sealed record ProviderPortabilityScenarioData(
    string ScenarioToken,
    ConformanceOutcome ExpectedOutcome,
    ConformanceFailureClassification ExpectedClassification,
    string SafeMessage,
    ConversationErrorCode? ExpectedErrorCode = null);

/// <summary>
/// Provides the deterministic, synthetic, content-safe provider portability fixture exercised by the
/// provider portability conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Loading this fixture appends no events, mutates no
/// aggregate state, writes no projection store, and requires no nested submodule initialization.
/// The 10 scenarios cover all AC1-required provider portability surfaces. Classification is
/// conformant for all scenarios because the suite proves the system CORRECTLY handles each one.
/// </remarks>
public static class ProviderPortabilityConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 10 deterministic synthetic provider portability scenario records.
    /// </summary>
    public static IReadOnlyList<ProviderPortabilityScenarioData> Scenarios =>
    [
        new(
            "provider-id-stripped",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Conversation remains recoverable after provider correlation ID stripped; the event log uses stable Conversations IDs only.",
            null),

        new(
            "provider-id-changed",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Conversation remains recoverable after provider correlation ID changed; replay uses stable Conversations references rather than provider correlation.",
            null),

        new(
            "session-expiry-recoverable",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Conversation remains recoverable when the provider session expires; the event history source of truth is independent of session state.",
            null),

        new(
            "provider-id-migrated",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Conversation remains recoverable after provider migrates its ID format; stable Conversations IDs remain unchanged throughout.",
            null),

        new(
            "projection-rebuild-without-provider",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Projection rebuild from the event log succeeds without provider correlation authority; stable IDs drive the rebuild.",
            null),

        new(
            "replay-determinism-without-provider",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Aggregate replay is deterministic independent of provider correlation; provider IDs are kept as correlation metadata only.",
            null),

        new(
            "provider-only-identity-blocked",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Command using provider-owned ID as conversation identity rejected fail-closed; provider-only identity is forbidden.",
            ConversationErrorCode.ProviderOnlyIdentityForbidden),

        new(
            "session-authority-blocked",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Command requiring provider session as conversation authority blocked fail-closed; the event log is the sole durable source of truth.",
            ConversationErrorCode.ProviderOnlyIdentityForbidden),

        new(
            "cross-provider-correlation-hidden",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "Cross-provider correlation probe hidden as aggregate-not-found to prevent side-channel disclosure of provider boundaries.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "diagnostics-content-safety",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Diagnostic output is content-safe and contains no infrastructure terms or protected data fragments.",
            null),
    ];
}
