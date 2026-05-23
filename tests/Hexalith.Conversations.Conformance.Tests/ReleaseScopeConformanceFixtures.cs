// <copyright file="ReleaseScopeConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Carries the deterministic synthetic scenario data for one release scope classification conformance check.
/// </summary>
/// <param name="ScenarioId">The bounded safe machine-readable scenario identifier.</param>
/// <param name="Entry">The capability release scope entry under test.</param>
/// <param name="ExpectedValidationErrors">Empty = should pass validation; non-empty = validation must return these error tokens.</param>
/// <param name="SafeMessage">The content-safe scenario description.</param>
public sealed record ReleaseScopeScenarioData(
    string ScenarioId,
    CapabilityReleaseScopeEntryV1 Entry,
    IReadOnlyList<string> ExpectedValidationErrors,
    string SafeMessage);

/// <summary>
/// Provides the deterministic, synthetic, content-safe release scope fixture exercised by the
/// release scope conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. The 10 scenarios cover all AC3-required scope validation
/// paths. Classification is conformant for all scenarios because the suite proves the validator
/// CORRECTLY flags or accepts each one.
/// </remarks>
public static class ReleaseScopeConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    private static readonly DateTimeOffset ReviewDate = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureExpiry = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastExpiry = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CapabilityReleaseScopeEntryV1 Entry(
        string capabilityId,
        CapabilityReleaseScope scope,
        IReadOnlyList<SubstrateConsequenceArea>? consequenceAreas = null,
        string? waiverRef = null,
        DateTimeOffset? conditionalExpiry = null)
        => new(
            capabilityId,
            scope,
            consequenceAreas ?? [],
            null,
            null,
            null,
            "release-engineer",
            ReviewDate,
            waiverRef,
            conditionalExpiry);

    /// <summary>
    /// Gets the 10 deterministic synthetic release scope scenario records.
    /// </summary>
    public static IReadOnlyList<ReleaseScopeScenarioData> Scenarios =>
    [
        new(
            "release-scope-v1-main",
            Entry("create-conversation", CapabilityReleaseScope.V1),
            [],
            "Capability v1 scope classification passes validator with no errors."),

        new(
            "release-scope-v1-1-planned",
            Entry("append-message", CapabilityReleaseScope.V1Point1),
            [],
            "Capability v1-1 scope classification passes validator with no errors."),

        new(
            "release-scope-vnext-future",
            Entry("add-participant", CapabilityReleaseScope.VNext),
            [],
            "Capability vnext scope classification passes validator with no errors."),

        new(
            "release-scope-conditional-valid",
            Entry("read-timeline", CapabilityReleaseScope.Conditional, conditionalExpiry: FutureExpiry),
            [],
            "Capability conditional scope with future expiry passes validator with no errors."),

        new(
            "release-scope-out-of-scope-boundary",
            Entry("close-archive", CapabilityReleaseScope.OutOfScope),
            [],
            "Capability out-of-scope boundary classification passes validator with no errors."),

        new(
            "release-scope-waived-approved",
            Entry("rebuild-projection", CapabilityReleaseScope.Waived, waiverRef: "approved-scope-waiver"),
            [],
            "Capability waived scope with approved waiver ref passes validator with no errors."),

        new(
            "release-scope-deferred-areas",
            Entry("update-metadata", CapabilityReleaseScope.Deferred, [SubstrateConsequenceArea.TenantIsolation, SubstrateConsequenceArea.AuditPairing]),
            [],
            "Capability deferred scope with substrate impact areas passes validator with no errors."),

        new(
            "release-scope-deferred-no-areas",
            Entry("deferred-cmd", CapabilityReleaseScope.Deferred),
            ["deferred-substrate-no-consequences"],
            "Capability deferred scope without substrate impact areas is flagged by validator."),

        new(
            "release-scope-waived-no-ref",
            Entry("waived-cmd", CapabilityReleaseScope.Waived),
            ["waived-no-reference"],
            "Capability waived scope without waiver ref is flagged by validator."),

        new(
            "release-scope-expired-cond",
            Entry("conditional-cmd", CapabilityReleaseScope.Conditional, conditionalExpiry: PastExpiry),
            ["expired-conditional-scope"],
            "Capability conditional scope with past expiry is flagged by validator."),
    ];
}
