// <copyright file="EventSchemaEvolutionConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Carries the deterministic synthetic scenario data for one event schema evolution conformance check.
/// </summary>
/// <param name="ScenarioToken">The bounded safe machine-readable scenario identifier (no real tenant IDs, Party IDs, or conversation IDs).</param>
/// <param name="ExpectedOutcome">The expected conformance outcome for this scenario.</param>
/// <param name="ExpectedClassification">The expected failure classification (conformant for all 10 scenarios).</param>
/// <param name="SafeMessage">The bounded content-safe scenario description (≤512 chars, no protected identifiers).</param>
/// <param name="ExpectedErrorCode">The expected typed error code for non-ready outcomes; null for ready outcomes.</param>
public sealed record EventSchemaEvolutionScenarioData(
    string ScenarioToken,
    ConformanceOutcome ExpectedOutcome,
    ConformanceFailureClassification ExpectedClassification,
    string SafeMessage,
    ConversationErrorCode? ExpectedErrorCode = null);

/// <summary>
/// Provides the deterministic, synthetic, content-safe event schema evolution fixture exercised by the
/// event schema evolution conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Loading this fixture appends no events, mutates no
/// aggregate state, writes no projection store, and requires no nested submodule initialization.
/// The 10 scenarios cover all AC1-required event schema evolution surfaces. Classification is
/// conformant for all scenarios because the suite proves the system CORRECTLY handles each one.
/// </remarks>
public static class EventSchemaEvolutionConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 10 deterministic synthetic event schema evolution scenario records.
    /// </summary>
    public static IReadOnlyList<EventSchemaEvolutionScenarioData> Scenarios =>
    [
        new(
            "schema-v1-replay",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "v1 event records replay to correct aggregate state using stable version identifiers without relying on runtime schema inference.",
            null),

        new(
            "additive-field-replay",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "An additive new field in the event record replays correctly through the documented compatibility path; no version bump required.",
            null),

        new(
            "version-metadata-present",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Published event records carry schema version metadata as required; consumers can identify the version without parsing the full event structure.",
            null),

        new(
            "mixed-version-history-replay",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "A history with both v1 and additive-change event records replays deterministically to the same aggregate state.",
            null),

        new(
            "projection-rebuild-mixed-versions",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Projection rebuild from a mixed-version event history produces a functionally equivalent read model for the same recorded events.",
            null),

        new(
            "upcaster-boundary-deterministic",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "The compatibility or upcaster boundary produces deterministic output for the same input event version on sequential runs.",
            null),

        new(
            "diagnostics-content-safety",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Diagnostic output from schema version compatibility checks is content-safe and contains no infrastructure terms or protected data fragments.",
            null),

        new(
            "unsupported-version-blocked",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "An event record with an unsupported schema version is rejected fail-closed with a typed documented error; no silent compatibility is assumed.",
            ConversationErrorCode.SchemaVersionUnsupported),

        new(
            "unsupported-version-not-skipped",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "An unsupported schema version is not silently skipped during replay or projection rebuild; the system returns a typed rejection error.",
            ConversationErrorCode.SchemaVersionUnsupported),

        new(
            "version-schema-probe-hidden",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "A version-specific schema probe is hidden as aggregate-not-found to prevent side-channel disclosure of internal event version structure.",
            ConversationErrorCode.AggregateNotFound),
    ];
}
