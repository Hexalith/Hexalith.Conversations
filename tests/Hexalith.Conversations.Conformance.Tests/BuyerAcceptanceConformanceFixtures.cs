// <copyright file="BuyerAcceptanceConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Carries the deterministic synthetic scenario data for one buyer partial acceptance conformance check.
/// </summary>
/// <param name="ScenarioId">The bounded safe machine-readable scenario identifier.</param>
/// <param name="Item">The buyer partial acceptance item under test.</param>
/// <param name="ExpectedValidationErrors">Empty = should pass validation; non-empty = validation must return these error tokens.</param>
/// <param name="SafeMessage">The content-safe scenario description.</param>
public sealed record BuyerAcceptanceScenarioData(
    string ScenarioId,
    BuyerPartialAcceptanceItemV1 Item,
    IReadOnlyList<string> ExpectedValidationErrors,
    string SafeMessage);

/// <summary>
/// Provides the deterministic, synthetic, content-safe buyer acceptance fixture exercised by the
/// buyer acceptance conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. The 10 scenarios cover all AC3-required buyer
/// acceptance validation paths. Classification is conformant for all scenarios because the suite
/// proves the validator CORRECTLY flags or accepts each one.
/// </remarks>
public static class BuyerAcceptanceConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    private static readonly DateTimeOffset FutureExpiry = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastExpiry = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureReviewDate = new(2027, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastReviewDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static BuyerPartialAcceptanceItemV1 Item(
        string itemId,
        BuyerAcceptanceItemStatus status,
        bool isBlocker = false,
        string? approver = null,
        string? buyerAcknowledgementRef = null,
        string? waiverRef = null,
        string? compensatingControl = null,
        DateTimeOffset? expiryDateUtc = null,
        DateTimeOffset? reviewDateUtc = null)
        => new(
            itemId,
            status,
            "test-capability",
            "release-engineer",
            approver,
            isBlocker,
            compensatingControl,
            expiryDateUtc ?? FutureExpiry,
            buyerAcknowledgementRef,
            waiverRef,
            null,
            null,
            null,
            null,
            reviewDateUtc ?? FutureReviewDate);

    /// <summary>
    /// Gets the 10 deterministic synthetic buyer acceptance scenario records.
    /// </summary>
    public static IReadOnlyList<BuyerAcceptanceScenarioData> Scenarios =>
    [
        new(
            "buyer-accept-main",
            Item("item-accept-main", BuyerAcceptanceItemStatus.Accepted, buyerAcknowledgementRef: "buyer-ack-main"),
            [],
            "Accepted capability with buyer acknowledgement passes validator with no errors."),

        new(
            "buyer-exclude-boundary",
            Item("item-exclude-boundary", BuyerAcceptanceItemStatus.Excluded, buyerAcknowledgementRef: null),
            [],
            "Excluded capability without acknowledgement passes validator with no errors."),

        new(
            "buyer-gap-accepted",
            Item("item-gap-accepted", BuyerAcceptanceItemStatus.UnknownAccepted, buyerAcknowledgementRef: "buyer-ack-gap"),
            [],
            "Gap-accepted capability with buyer acknowledgement passes validator with no errors."),

        new(
            "buyer-waived-with-link",
            Item("item-waived-with-link", BuyerAcceptanceItemStatus.Waived, waiverRef: "scope-waiver-001"),
            [],
            "Waived capability with waiver link passes validator with no errors."),

        new(
            "buyer-blocker-approved-control",
            Item(
                "item-blocker-approved",
                BuyerAcceptanceItemStatus.Accepted,
                isBlocker: true,
                approver: "approver-001",
                compensatingControl: "compensating-control-001",
                buyerAcknowledgementRef: "buyer-ack-blocker"),
            [],
            "Blocker capability with approver and compensating control passes validator with no errors."),

        new(
            "buyer-expired-item",
            Item("item-expired", BuyerAcceptanceItemStatus.Accepted, buyerAcknowledgementRef: "buyer-ack-expired", expiryDateUtc: PastExpiry),
            ["expired-acceptance-item"],
            "Accepted capability with past expiry is flagged by validator."),

        new(
            "buyer-missing-ack",
            Item("item-missing-ack", BuyerAcceptanceItemStatus.Accepted, buyerAcknowledgementRef: null),
            ["missing-buyer-acknowledgement"],
            "Accepted capability without buyer acknowledgement is flagged by validator."),

        new(
            "buyer-blocker-no-approver",
            Item("item-blocker-no-approver", BuyerAcceptanceItemStatus.Accepted, isBlocker: true, approver: null, buyerAcknowledgementRef: "buyer-ack-blk"),
            ["blocker-requires-approver"],
            "Blocker capability without approver is flagged by validator."),

        new(
            "buyer-review-due",
            Item("item-review-due", BuyerAcceptanceItemStatus.Accepted, buyerAcknowledgementRef: "buyer-ack-review", reviewDateUtc: PastReviewDate),
            ["review-due"],
            "Accepted capability with past review date is flagged by validator."),

        new(
            "buyer-waived-no-link",
            Item("item-waived-no-link", BuyerAcceptanceItemStatus.Waived, waiverRef: null),
            ["waived-missing-waiver-link"],
            "Waived capability without waiver link is flagged by validator."),
    ];
}
