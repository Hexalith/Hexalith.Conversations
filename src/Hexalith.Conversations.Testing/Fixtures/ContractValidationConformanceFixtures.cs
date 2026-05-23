// <copyright file="ContractValidationConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Carries the deterministic synthetic scenario data for one contract validation conformance check.
/// </summary>
/// <param name="ScenarioToken">The bounded safe machine-readable scenario identifier (no real tenant IDs, Party IDs, or conversation IDs).</param>
/// <param name="ExpectedOutcome">The expected conformance outcome for this scenario.</param>
/// <param name="ExpectedClassification">The expected failure classification (conformant for all 10 scenarios).</param>
/// <param name="SafeMessage">The bounded content-safe scenario description (≤512 chars, no protected identifiers).</param>
/// <param name="ExpectedErrorCode">The expected typed error code for non-ready outcomes; null for ready outcomes.</param>
public sealed record ContractValidationScenarioData(
    string ScenarioToken,
    ConformanceOutcome ExpectedOutcome,
    ConformanceFailureClassification ExpectedClassification,
    string SafeMessage,
    ConversationErrorCode? ExpectedErrorCode = null);

/// <summary>
/// Provides the deterministic, synthetic, content-safe contract validation fixture exercised by the
/// contract validation conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Loading this fixture appends no events, mutates no
/// aggregate state, writes no projection store, and requires no nested submodule initialization.
/// The 10 scenarios cover all AC1-AC5-required contract validation surfaces. Classification is
/// conformant for all scenarios because the suite proves the system CORRECTLY handles each one.
/// </remarks>
public static class ContractValidationConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 10 deterministic synthetic contract validation scenario records.
    /// </summary>
    public static IReadOnlyList<ContractValidationScenarioData> Scenarios =>
    [
        new(
            "command-contract-shape",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Create-conversation and append-message command contracts match the published contract package shape and carry schema and version metadata.",
            null),

        new(
            "query-contract-shape",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Read-timeline and list-conversations query contracts match the published contract package and return freshness metadata as required.",
            null),

        new(
            "event-publication-shape",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Domain events carry schema and version metadata as required by the contract; no internal infrastructure terms are exposed in the adopter-facing event surface.",
            null),

        new(
            "typed-error-shape",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Typed error contract is content-safe with machine-readable code, category, retryability, and a documentation pointer; no protected identifiers included.",
            null),

        new(
            "version-discovery-shape",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Version-discovery returns active command, projection, event, and client package versions in the published contract shape without infrastructure internals.",
            null),

        new(
            "core-fixture-happy-path",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "The adopter-style CORE fixture exercises create, append, and read with Current freshness and stable participant and business-reference attribution.",
            null),

        new(
            "core-fixture-blocked-schema",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "An unsupported schema version in the CORE fixture is rejected fail-closed with a typed documented error; no silent compatibility is assumed.",
            ConversationErrorCode.SchemaVersionUnsupported),

        new(
            "core-fixture-probe-hidden",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "A cross-authorization probe in the CORE fixture is hidden as aggregate-not-found to prevent side-channel disclosure of protected record existence.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "redaction-consumer-contract",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Redaction command, event, and audit contracts remain stable for consumer-driven validation; no breaking change has been introduced in the contract surface.",
            null),

        new(
            "conformance-invariant-proof",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Project conformance invariants have traceable automated evidence: event log authority, fail-closed access, personal-data boundaries, and generated-first UI boundaries.",
            null),
    ];
}
