// <copyright file="BuyerPartialAcceptanceValidatorTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies that <see cref="BuyerPartialAcceptanceItemValidator"/> returns the correct error tokens
/// for all buyer partial acceptance validation paths (FR102, AC1, AC2, AC3, AC4).
/// </summary>
public sealed class BuyerPartialAcceptanceValidatorTest
{
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureExpiry = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastExpiry = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureReviewDate = new(2027, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastReviewDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static BuyerPartialAcceptanceItemV1 BuildItem(
        BuyerAcceptanceItemStatus status,
        bool isBlocker = false,
        string? approver = null,
        string? buyerAcknowledgementRef = "buyer-ack-ref",
        string? waiverRef = null,
        DateTimeOffset? expiryDateUtc = null,
        DateTimeOffset? reviewDateUtc = null)
        => new(
            "item-001",
            status,
            "test-capability",
            "release-engineer",
            approver,
            isBlocker,
            null,
            expiryDateUtc ?? FutureExpiry,
            buyerAcknowledgementRef,
            waiverRef,
            null,
            null,
            null,
            null,
            reviewDateUtc ?? FutureReviewDate);

    [Fact]
    public void ValidateItem_Accepted_WithAck_ReturnsNoErrors()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(BuyerAcceptanceItemStatus.Accepted, buyerAcknowledgementRef: "buyer-ack-001");
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateItem_Excluded_NoAckRequired_ReturnsNoErrors()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(BuyerAcceptanceItemStatus.Excluded, buyerAcknowledgementRef: null);
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateItem_UnknownAccepted_WithAck_ReturnsNoErrors()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(BuyerAcceptanceItemStatus.UnknownAccepted, buyerAcknowledgementRef: "buyer-ack-gap");
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateItem_Waived_WithWaiverLink_ReturnsNoErrors()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(
            BuyerAcceptanceItemStatus.Waived,
            buyerAcknowledgementRef: null,
            waiverRef: "scope-waiver-001");
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateItem_Blocker_WithApprover_ReturnsNoErrors()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(
            BuyerAcceptanceItemStatus.Accepted,
            isBlocker: true,
            approver: "approver-001",
            buyerAcknowledgementRef: "buyer-ack-001");
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateItem_Accepted_MissingAck_ReturnsMissingBuyerAcknowledgement()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(BuyerAcceptanceItemStatus.Accepted, buyerAcknowledgementRef: null);
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldContain("missing-buyer-acknowledgement");
    }

    [Fact]
    public void ValidateItem_Blocker_MissingApprover_ReturnsBlockerRequiresApprover()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(
            BuyerAcceptanceItemStatus.Accepted,
            isBlocker: true,
            approver: null,
            buyerAcknowledgementRef: "buyer-ack-001");
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldContain("blocker-requires-approver");
    }

    [Fact]
    public void ValidateItem_ExpiredItem_ReturnsExpiredAcceptanceItem()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(
            BuyerAcceptanceItemStatus.Accepted,
            buyerAcknowledgementRef: "buyer-ack-001",
            expiryDateUtc: PastExpiry);
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldContain("expired-acceptance-item");
    }

    [Fact]
    public void ValidateItem_ReviewDue_ReturnsReviewDue()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(
            BuyerAcceptanceItemStatus.Accepted,
            buyerAcknowledgementRef: "buyer-ack-001",
            reviewDateUtc: PastReviewDate);
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldContain("review-due");
    }

    [Fact]
    public void ValidateItem_Waived_NoLink_ReturnsWaivedMissingWaiverLink()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(
            BuyerAcceptanceItemStatus.Waived,
            buyerAcknowledgementRef: null,
            waiverRef: null);
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldContain("waived-missing-waiver-link");
    }

    [Fact]
    public void ValidateItem_Excluded_DoesNotRequireBuyerAck()
    {
        BuyerPartialAcceptanceItemV1 item = BuildItem(BuyerAcceptanceItemStatus.Excluded, buyerAcknowledgementRef: null);
        IReadOnlyList<string> errors = BuyerPartialAcceptanceItemValidator.ValidateItem(item, EvaluatedAt);
        errors.ShouldNotContain("missing-buyer-acknowledgement");
    }
}
