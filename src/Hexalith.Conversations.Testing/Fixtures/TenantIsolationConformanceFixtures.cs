// <copyright file="TenantIsolationConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Carries the deterministic synthetic scenario data for one tenant isolation conformance check.
/// </summary>
/// <param name="ScenarioToken">The bounded safe machine-readable scenario identifier (no real tenant IDs, Party IDs, or conversation IDs).</param>
/// <param name="ExpectedOutcome">The expected conformance outcome for this scenario.</param>
/// <param name="ExpectedClassification">The expected failure classification (conformant for all 12 scenarios).</param>
/// <param name="SafeMessage">The bounded content-safe scenario description (≤512 chars, no protected identifiers).</param>
/// <param name="ExpectedErrorCode">The expected typed error code for non-ready outcomes; null for ready outcomes.</param>
public sealed record TenantIsolationScenarioData(
    string ScenarioToken,
    ConformanceOutcome ExpectedOutcome,
    ConformanceFailureClassification ExpectedClassification,
    string SafeMessage,
    ConversationErrorCode? ExpectedErrorCode = null);

/// <summary>
/// Provides the deterministic, synthetic, content-safe tenant isolation fixture exercised by the
/// tenant isolation conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Loading this fixture appends no events, mutates no
/// aggregate state, writes no projection store, and requires no nested submodule initialization.
/// The 12 scenarios cover all AC1-required isolation cases. Classification is conformant for all
/// scenarios because the suite proves the system CORRECTLY handles each one.
/// </remarks>
public static class TenantIsolationConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 12 deterministic synthetic tenant isolation scenario records.
    /// </summary>
    public static IReadOnlyList<TenantIsolationScenarioData> Scenarios =>
    [
        new(
            "authorized-access",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Positive conformance: authorized access granted with current projection state.",
            null),

        new(
            "hidden-id-probe",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "Hidden side-channel shape: cross-scope probe collapses to not-found equivalent.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "stale-projection",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Fail-closed on projection staleness: system blocks when projection is outdated.",
            ConversationErrorCode.TenantProjectionStale),

        new(
            "unavailable-projection",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Fail-closed on projection unavailability: system blocks when projection is unreachable.",
            ConversationErrorCode.TenantProjectionStale),

        new(
            "disabled-tenant",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Fail-closed on disabled scope: system correctly denies access for disabled tenant.",
            ConversationErrorCode.TenantIsolationViolation),

        new(
            "deleted-tenant",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Fail-closed on deleted scope: system correctly denies access for deleted tenant.",
            ConversationErrorCode.TenantIsolationViolation),

        new(
            "mixed-scope-rebuild",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Isolation invariant: rebuild mixing scopes is blocked to prevent cross-scope data access.",
            ConversationErrorCode.TenantIsolationViolation),

        new(
            "poisoned-projection-event",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "Hidden side-channel shape: poisoned projection event never surfaces cross-scope content.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "malformed-binding",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Configuration error: malformed claim rejected at command boundary.",
            ConversationErrorCode.TenantBindingMissing),

        new(
            "query-enumeration",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "Hidden side-channel shape: enumeration attempt collapses to not-found equivalent.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "diagnostics-content-safety",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Positive safety conformance: diagnostics output is content-safe with no cross-scope data.",
            null),

        new(
            "admin-tool-access",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "Fail-closed on admin and tool paths: unauthorized access denied by same binding gates.",
            ConversationErrorCode.TenantIsolationViolation),
    ];
}
