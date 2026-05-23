// <copyright file="SecondAdopterStatusValidatorTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies that <see cref="SecondAdopterStatusValidator"/> returns the correct error tokens
/// for all second-adopter lifecycle validation paths (FR103, AC1, AC2, AC3).
/// </summary>
public sealed class SecondAdopterStatusValidatorTest
{
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureMilestone = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastMilestone = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureReviewDate = new(2027, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastReviewDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureWaiverExpiry = new(2027, 12, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastWaiverExpiry = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static SecondAdopterStatusEntryV1 BuildEntry(
        SecondAdopterStatus status,
        bool downgradeRuleTriggered = false,
        DateTimeOffset? milestoneDateUtc = null,
        DateTimeOffset? reviewDateUtc = null,
        string? waiverRef = null,
        DateTimeOffset? waiverExpiryDateUtc = null,
        string? statusChangeRationaleRef = null,
        string? conformanceArtifactRef = null)
        => new(
            "entry-001",
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

    [Fact]
    public void ValidateEntry_Identified_FutureMilestone_ReturnsNoErrors()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(SecondAdopterStatus.Identified);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_Qualified_WithTrigger_ReturnsNoErrors()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(SecondAdopterStatus.Qualified, downgradeRuleTriggered: true);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_Deferred_WithValidWaiver_ReturnsNoErrors()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(
            SecondAdopterStatus.Deferred,
            waiverRef: "deferred-scope-waiver",
            waiverExpiryDateUtc: FutureWaiverExpiry);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_Disqualified_WithRationale_ReturnsNoErrors()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(
            SecondAdopterStatus.Disqualified,
            statusChangeRationaleRef: "revert-rationale-001");
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_MilestoneOverdue_ReturnsMilestoneOverdue()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(SecondAdopterStatus.Identified, milestoneDateUtc: PastMilestone);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("milestone-overdue");
    }

    [Fact]
    public void ValidateEntry_ReviewOverdue_ReturnsReviewOverdue()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(
            SecondAdopterStatus.Qualified,
            downgradeRuleTriggered: true,
            reviewDateUtc: PastReviewDate);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("review-overdue");
    }

    [Fact]
    public void ValidateEntry_Qualified_NoTrigger_ReturnsQualifiedNoDowngradeTrigger()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(SecondAdopterStatus.Qualified, downgradeRuleTriggered: false);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("qualified-no-downgrade-trigger");
    }

    [Fact]
    public void ValidateEntry_WaiverExpired_ReturnsWaiverExpired()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(
            SecondAdopterStatus.Deferred,
            waiverRef: "expired-waiver-ref",
            waiverExpiryDateUtc: PastWaiverExpiry);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("waiver-expired");
    }

    [Fact]
    public void ValidateEntry_Disqualified_NoRationale_ReturnsRevertedMissingRationale()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(SecondAdopterStatus.Disqualified, statusChangeRationaleRef: null);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("reverted-missing-rationale");
    }

    [Fact]
    public void ValidateEntry_Deferred_NoWaiverRef_DoesNotTriggerWaiverExpired()
    {
        SecondAdopterStatusEntryV1 entry = BuildEntry(
            SecondAdopterStatus.Deferred,
            waiverRef: null,
            waiverExpiryDateUtc: PastWaiverExpiry);
        IReadOnlyList<string> errors = SecondAdopterStatusValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldNotContain("waiver-expired");
    }
}
