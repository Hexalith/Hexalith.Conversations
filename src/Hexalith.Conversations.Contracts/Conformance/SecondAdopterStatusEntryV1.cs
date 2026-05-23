// <copyright file="SecondAdopterStatusEntryV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Conformance;

public sealed record SecondAdopterStatusEntryV1(
    string EntryId,
    SecondAdopterStatus Status,
    string AffectedRequirementsRef,
    string ReviewOwner,
    DateTimeOffset MilestoneDateUtc,
    bool DowngradeRuleTriggered,
    string CapabilityRef,
    string? WaiverRef,
    DateTimeOffset? WaiverExpiryDateUtc,
    string? StatusChangeRationaleRef,
    string? ConformanceArtifactRef,
    DateTimeOffset ReviewDateUtc)
{
    public string EntryId { get; } = ConformanceContractValidation.RequiredSafeToken(EntryId, nameof(EntryId));
    public SecondAdopterStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));
    public string AffectedRequirementsRef { get; } = ConformanceContractValidation.RequiredSafeToken(AffectedRequirementsRef, nameof(AffectedRequirementsRef));
    public string ReviewOwner { get; } = ConformanceContractValidation.RequiredSafeToken(ReviewOwner, nameof(ReviewOwner));
    public DateTimeOffset MilestoneDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(MilestoneDateUtc, nameof(MilestoneDateUtc));
    public bool DowngradeRuleTriggered { get; } = DowngradeRuleTriggered;
    public string CapabilityRef { get; } = ConformanceContractValidation.RequiredSafeToken(CapabilityRef, nameof(CapabilityRef));
    public string? WaiverRef { get; } = ConformanceContractValidation.OptionalSafeToken(WaiverRef, nameof(WaiverRef));
    public DateTimeOffset? WaiverExpiryDateUtc { get; } = WaiverExpiryDateUtc.HasValue
        ? ConformanceContractValidation.RequiredUtcTimestamp(WaiverExpiryDateUtc.Value, nameof(WaiverExpiryDateUtc))
        : (DateTimeOffset?)null;
    public string? StatusChangeRationaleRef { get; } = ConformanceContractValidation.OptionalSafeToken(StatusChangeRationaleRef, nameof(StatusChangeRationaleRef));
    public string? ConformanceArtifactRef { get; } = ConformanceContractValidation.OptionalSafeToken(ConformanceArtifactRef, nameof(ConformanceArtifactRef));
    public DateTimeOffset ReviewDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ReviewDateUtc, nameof(ReviewDateUtc));
}

public static class SecondAdopterStatusValidator
{
    public static IReadOnlyList<string> ValidateEntry(SecondAdopterStatusEntryV1 entry, DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(entry);

        List<string> errors = [];

        if (entry.MilestoneDateUtc < evaluatedAt)
            errors.Add("milestone-overdue");

        if (entry.ReviewDateUtc < evaluatedAt)
            errors.Add("review-overdue");

        if (entry.Status.Equals(SecondAdopterStatus.Qualified) && !entry.DowngradeRuleTriggered)
            errors.Add("qualified-no-downgrade-trigger");

        if (entry.WaiverRef is not null &&
            entry.WaiverExpiryDateUtc.HasValue &&
            entry.WaiverExpiryDateUtc.Value < evaluatedAt)
            errors.Add("waiver-expired");

        if (entry.Status.Equals(SecondAdopterStatus.Disqualified) && entry.StatusChangeRationaleRef is null)
            errors.Add("reverted-missing-rationale");

        return errors;
    }
}
