// <copyright file="BuyerPartialAcceptanceItemV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Conformance;

public sealed record BuyerPartialAcceptanceItemV1(
    string ItemId,
    BuyerAcceptanceItemStatus Status,
    string CapabilityRef,
    string Owner,
    string? Approver,
    bool IsBlocker,
    string? CompensatingControl,
    DateTimeOffset ExpiryDateUtc,
    string? BuyerAcknowledgementRef,
    string? WaiverRef,
    string? ConformanceArtifactRef,
    string? ManifestRowRef,
    string? AffectedStoryRef,
    string? ReleaseScopeConsequenceRef,
    DateTimeOffset ReviewDateUtc)
{
    public string ItemId { get; } = ConformanceContractValidation.RequiredSafeToken(ItemId, nameof(ItemId));
    public BuyerAcceptanceItemStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));
    public string CapabilityRef { get; } = ConformanceContractValidation.RequiredSafeToken(CapabilityRef, nameof(CapabilityRef));
    public string Owner { get; } = ConformanceContractValidation.RequiredSafeToken(Owner, nameof(Owner));
    public string? Approver { get; } = ConformanceContractValidation.OptionalSafeToken(Approver, nameof(Approver));
    public bool IsBlocker { get; } = IsBlocker;
    public string? CompensatingControl { get; } = ConformanceContractValidation.OptionalSafeToken(CompensatingControl, nameof(CompensatingControl));
    public DateTimeOffset ExpiryDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ExpiryDateUtc, nameof(ExpiryDateUtc));
    public string? BuyerAcknowledgementRef { get; } = ConformanceContractValidation.OptionalSafeToken(BuyerAcknowledgementRef, nameof(BuyerAcknowledgementRef));
    public string? WaiverRef { get; } = ConformanceContractValidation.OptionalSafeToken(WaiverRef, nameof(WaiverRef));
    public string? ConformanceArtifactRef { get; } = ConformanceContractValidation.OptionalSafeToken(ConformanceArtifactRef, nameof(ConformanceArtifactRef));
    public string? ManifestRowRef { get; } = ConformanceContractValidation.OptionalSafeToken(ManifestRowRef, nameof(ManifestRowRef));
    public string? AffectedStoryRef { get; } = ConformanceContractValidation.OptionalSafeToken(AffectedStoryRef, nameof(AffectedStoryRef));
    public string? ReleaseScopeConsequenceRef { get; } = ConformanceContractValidation.OptionalSafeToken(ReleaseScopeConsequenceRef, nameof(ReleaseScopeConsequenceRef));
    public DateTimeOffset ReviewDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ReviewDateUtc, nameof(ReviewDateUtc));
}

public static class BuyerPartialAcceptanceItemValidator
{
    public static IReadOnlyList<string> ValidateItem(BuyerPartialAcceptanceItemV1 item, DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(item);

        List<string> errors = [];

        if (item.IsBlocker && item.Approver is null)
            errors.Add("blocker-requires-approver");

        if ((item.Status.Equals(BuyerAcceptanceItemStatus.Accepted) ||
             item.Status.Equals(BuyerAcceptanceItemStatus.UnknownAccepted)) &&
            item.BuyerAcknowledgementRef is null)
            errors.Add("missing-buyer-acknowledgement");

        if (item.ExpiryDateUtc < evaluatedAt)
            errors.Add("expired-acceptance-item");

        if (item.ReviewDateUtc < evaluatedAt)
            errors.Add("review-due");

        if (item.Status.Equals(BuyerAcceptanceItemStatus.Waived) && item.WaiverRef is null)
            errors.Add("waived-missing-waiver-link");

        return errors;
    }
}
