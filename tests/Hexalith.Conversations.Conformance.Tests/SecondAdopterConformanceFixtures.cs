// <copyright file="SecondAdopterConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Carries the deterministic synthetic scenario data for one second-adopter lifecycle conformance check.
/// </summary>
/// <param name="ScenarioId">The bounded safe machine-readable scenario identifier.</param>
/// <param name="Entry">The second adopter status entry under test.</param>
/// <param name="ExpectedValidationErrors">Empty = should pass validation; non-empty = validation must return these error tokens.</param>
/// <param name="SafeMessage">The content-safe scenario description.</param>
public sealed record SecondAdopterScenarioData(
    string ScenarioId,
    SecondAdopterStatusEntryV1 Entry,
    IReadOnlyList<string> ExpectedValidationErrors,
    string SafeMessage);

/// <summary>
/// Provides the deterministic, synthetic, content-safe second-adopter fixture exercised by the
/// second-adopter conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. The 10 scenarios cover all AC3-required second-adopter
/// lifecycle validation paths. Classification is conformant for all scenarios because the suite
/// proves the validator CORRECTLY flags or accepts each one.
/// </remarks>
public static class SecondAdopterConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    private static readonly DateTimeOffset FutureMilestone = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastMilestone = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureReviewDate = new(2027, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastReviewDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureWaiverExpiry = new(2027, 12, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastWaiverExpiry = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static SecondAdopterStatusEntryV1 Entry(
        string entryId,
        SecondAdopterStatus status,
        bool downgradeRuleTriggered = false,
        DateTimeOffset? milestoneDateUtc = null,
        DateTimeOffset? reviewDateUtc = null,
        string? waiverRef = null,
        DateTimeOffset? waiverExpiryDateUtc = null,
        string? statusChangeRationaleRef = null,
        string? conformanceArtifactRef = null)
        => new(
            entryId,
            status,
            "FR103",
            "release-engineer",
            milestoneDateUtc ?? FutureMilestone,
            downgradeRuleTriggered,
            "second-adopter-capability",
            waiverRef,
            waiverExpiryDateUtc,
            statusChangeRationaleRef,
            conformanceArtifactRef,
            reviewDateUtc ?? FutureReviewDate);

    /// <summary>
    /// Gets the 10 deterministic synthetic second-adopter scenario records.
    /// </summary>
    public static IReadOnlyList<SecondAdopterScenarioData> Scenarios =>
    [
        new(
            "adopter-identified-baseline",
            Entry("entry-identified-baseline", SecondAdopterStatus.Identified),
            [],
            "Second adopter identified with future milestone and review passes validator with no errors."),

        new(
            "adopter-qualified-trigger-set",
            Entry("entry-qualified-trigger", SecondAdopterStatus.Qualified, downgradeRuleTriggered: true),
            [],
            "Qualified second adopter with downgrade rule triggered passes validator with no errors."),

        new(
            "adopter-deferred-waiver-valid",
            Entry(
                "entry-deferred-waiver-valid",
                SecondAdopterStatus.Deferred,
                waiverRef: "deferred-scope-waiver",
                waiverExpiryDateUtc: FutureWaiverExpiry),
            [],
            "Deferred second adopter with valid waiver passes validator with no errors."),

        new(
            "adopter-disqualified-rationale",
            Entry(
                "entry-disqualified-rationale",
                SecondAdopterStatus.Disqualified,
                statusChangeRationaleRef: "revert-rationale-001"),
            [],
            "Disqualified second adopter with status change rationale passes validator with no errors."),

        new(
            "adopter-qualified-capability-link",
            Entry(
                "entry-qualified-capability",
                SecondAdopterStatus.Qualified,
                downgradeRuleTriggered: true,
                conformanceArtifactRef: "capability-review-artifact"),
            [],
            "Qualified second adopter with capability link and trigger passes validator with no errors."),

        new(
            "adopter-milestone-overdue",
            Entry("entry-milestone-overdue", SecondAdopterStatus.Identified, milestoneDateUtc: PastMilestone),
            ["milestone-overdue"],
            "Second adopter with past milestone date is flagged by validator."),

        new(
            "adopter-review-overdue",
            Entry(
                "entry-review-overdue",
                SecondAdopterStatus.Qualified,
                downgradeRuleTriggered: true,
                reviewDateUtc: PastReviewDate),
            ["review-overdue"],
            "Second adopter with past review date is flagged by validator."),

        new(
            "adopter-qualified-no-trigger",
            Entry("entry-qualified-no-trigger", SecondAdopterStatus.Qualified, downgradeRuleTriggered: false),
            ["qualified-no-downgrade-trigger"],
            "Qualified second adopter without downgrade rule trigger is flagged by validator."),

        new(
            "adopter-deferred-waiver-expired",
            Entry(
                "entry-deferred-waiver-expired",
                SecondAdopterStatus.Deferred,
                waiverRef: "expired-waiver-ref",
                waiverExpiryDateUtc: PastWaiverExpiry),
            ["waiver-expired"],
            "Deferred second adopter with expired waiver is flagged by validator."),

        new(
            "adopter-reverted-no-rationale",
            Entry("entry-reverted-no-rationale", SecondAdopterStatus.Disqualified, statusChangeRationaleRef: null),
            ["reverted-missing-rationale"],
            "Disqualified second adopter without status change rationale is flagged by validator."),
    ];
}
