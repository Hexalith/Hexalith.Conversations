// <copyright file="ReleaseWaiverV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Defines the closed waiver lifecycle status vocabulary for named release-gate waivers (FR85).
/// </summary>
/// <remarks>
/// Exactly four values — no more, no less. This vocabulary is entity-specific to <see cref="ReleaseWaiverV1"/>
/// and does not modify <see cref="ReleaseGateStatus"/>, <see cref="ConformanceOutcome"/>,
/// or <see cref="ConformanceCheck"/> vocabularies.
/// </remarks>
[JsonConverter(typeof(WaiverLifecycleStatusJsonConverter))]
public sealed record WaiverLifecycleStatus
{
    /// <summary>
    /// Gets the active status: waiver is currently valid and approved.
    /// </summary>
    public static WaiverLifecycleStatus Active { get; } = new("active");

    /// <summary>
    /// Gets the expired status: waiver has passed its ExpiryDateUtc and is treated as a finding.
    /// </summary>
    public static WaiverLifecycleStatus Expired { get; } = new("expired");

    /// <summary>
    /// Gets the rejected status: waiver request was explicitly denied by an approver.
    /// </summary>
    public static WaiverLifecycleStatus Rejected { get; } = new("rejected");

    /// <summary>
    /// Gets the superseded status: waiver was replaced by a newer named waiver.
    /// </summary>
    public static WaiverLifecycleStatus Superseded { get; } = new("superseded");

    private static readonly IReadOnlyDictionary<string, WaiverLifecycleStatus> KnownValues = Known(
        Active,
        Expired,
        Rejected,
        Superseded);

    private WaiverLifecycleStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported waiver lifecycle status in canonical order.
    /// </summary>
    public static IReadOnlyList<WaiverLifecycleStatus> All { get; } =
    [
        Active,
        Expired,
        Rejected,
        Superseded,
    ];

    /// <summary>
    /// Gets a value indicating whether this status means the waiver is currently valid and approved.
    /// </summary>
    public bool IsActive => Equals(Active);

    /// <summary>
    /// Gets a value indicating whether this status represents an outdated waiver that may affect gate status.
    /// </summary>
    /// <remarks>
    /// True for <see cref="Expired"/> and <see cref="Superseded"/>; stale waivers are treated as findings per AC2.
    /// <see cref="Rejected"/> is not stale — it was never accepted.
    /// </remarks>
    public bool IsStale => Equals(Expired) || Equals(Superseded);

    /// <summary>
    /// Resolves a supported waiver lifecycle status.
    /// </summary>
    /// <param name="value">The canonical status value.</param>
    /// <returns>The matching waiver lifecycle status.</returns>
    public static WaiverLifecycleStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(WaiverLifecycleStatus));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Carries all required governance fields for one named release-gate waiver (FR85, FR86, NFR62).
/// </summary>
/// <param name="waiverId">The stable bounded machine-readable waiver identifier.</param>
/// <param name="owner">The bounded owner identifier.</param>
/// <param name="approver">The optional bounded approver identifier; null valid at construction but enforced by <see cref="ReleaseWaiverValidator"/> when <paramref name="isBlocker"/> is true.</param>
/// <param name="affectedRequirementId">The FR or NFR identifier such as FR87 or NFR62.</param>
/// <param name="affectedGateId">The optional release gate; null when the waiver applies to a non-gate requirement.</param>
/// <param name="affectedStoryIds">The non-empty list of bounded safe-token story key identifiers.</param>
/// <param name="isBlocker">True when this waiver covers an automatic release blocker (NFR62 categories).</param>
/// <param name="risk">The bounded risk description.</param>
/// <param name="compensatingControl">The bounded compensating control description.</param>
/// <param name="expiryDateUtc">The UTC expiry date; <see cref="ReleaseWaiverValidator"/> flags past expiry.</param>
/// <param name="buyerImpact">The bounded buyer impact description.</param>
/// <param name="buyerAcceptanceStatus">The optional bounded token; null means not customer-facing.</param>
/// <param name="evidenceLinks">The non-null list of bounded safe-token evidence artifact handles; empty list allowed.</param>
/// <param name="reviewDateUtc">The UTC review date; <see cref="ReleaseWaiverValidator"/> flags past review dates.</param>
/// <param name="lifecycleStatus">The required lifecycle status.</param>
/// <param name="createdAtUtc">The UTC creation timestamp.</param>
public sealed record ReleaseWaiverV1(
    string WaiverId,
    string Owner,
    string? Approver,
    string AffectedRequirementId,
    ReleaseGateId? AffectedGateId,
    IReadOnlyList<string> AffectedStoryIds,
    bool IsBlocker,
    string Risk,
    string CompensatingControl,
    DateTimeOffset ExpiryDateUtc,
    string BuyerImpact,
    string? BuyerAcceptanceStatus,
    IReadOnlyList<string> EvidenceLinks,
    DateTimeOffset ReviewDateUtc,
    WaiverLifecycleStatus LifecycleStatus,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>
    /// Gets the stable bounded machine-readable waiver identifier.
    /// </summary>
    public string WaiverId { get; } = ConformanceContractValidation.RequiredSafeToken(WaiverId, nameof(WaiverId));

    /// <summary>
    /// Gets the bounded owner identifier.
    /// </summary>
    public string Owner { get; } = ConformanceContractValidation.RequiredSafeToken(Owner, nameof(Owner));

    /// <summary>
    /// Gets the optional bounded approver identifier.
    /// </summary>
    public string? Approver { get; } = ConformanceContractValidation.OptionalSafeToken(Approver, nameof(Approver));

    /// <summary>
    /// Gets the FR or NFR identifier this waiver covers.
    /// </summary>
    public string AffectedRequirementId { get; } = ConformanceContractValidation.RequiredSafeToken(AffectedRequirementId, nameof(AffectedRequirementId));

    /// <summary>
    /// Gets the optional release gate; null when the waiver is not gate-specific.
    /// </summary>
    public ReleaseGateId? AffectedGateId { get; } = AffectedGateId;

    /// <summary>
    /// Gets the non-empty list of bounded safe-token story key identifiers.
    /// </summary>
    public IReadOnlyList<string> AffectedStoryIds { get; } = ValidateAffectedStoryIds(AffectedStoryIds, nameof(AffectedStoryIds));

    /// <summary>
    /// Gets a value indicating whether this waiver covers an automatic release blocker (NFR62 categories).
    /// </summary>
    public bool IsBlocker { get; } = IsBlocker;

    /// <summary>
    /// Gets the bounded risk description.
    /// </summary>
    public string Risk { get; } = ConformanceContractValidation.RequiredSafeText(Risk, nameof(Risk));

    /// <summary>
    /// Gets the bounded compensating control description.
    /// </summary>
    public string CompensatingControl { get; } = ConformanceContractValidation.RequiredSafeText(CompensatingControl, nameof(CompensatingControl));

    /// <summary>
    /// Gets the UTC expiry date.
    /// </summary>
    public DateTimeOffset ExpiryDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ExpiryDateUtc, nameof(ExpiryDateUtc));

    /// <summary>
    /// Gets the bounded buyer impact description.
    /// </summary>
    public string BuyerImpact { get; } = ConformanceContractValidation.RequiredSafeText(BuyerImpact, nameof(BuyerImpact));

    /// <summary>
    /// Gets the optional bounded buyer acceptance status token; null means not customer-facing.
    /// </summary>
    public string? BuyerAcceptanceStatus { get; } = ConformanceContractValidation.OptionalSafeToken(BuyerAcceptanceStatus, nameof(BuyerAcceptanceStatus));

    /// <summary>
    /// Gets the non-null list of bounded safe-token evidence artifact handles; empty list allowed.
    /// </summary>
    public IReadOnlyList<string> EvidenceLinks { get; } = ValidateEvidenceLinks(EvidenceLinks, nameof(EvidenceLinks));

    /// <summary>
    /// Gets the UTC review date.
    /// </summary>
    public DateTimeOffset ReviewDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ReviewDateUtc, nameof(ReviewDateUtc));

    /// <summary>
    /// Gets the required lifecycle status.
    /// </summary>
    public WaiverLifecycleStatus LifecycleStatus { get; } = LifecycleStatus ?? throw new ArgumentNullException(nameof(LifecycleStatus));

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(CreatedAtUtc, nameof(CreatedAtUtc));

    private static IReadOnlyList<string> ValidateAffectedStoryIds(IReadOnlyList<string>? values, string parameterName)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one affected story identifier is required.", parameterName);
        }

        // Story IDs are closed traceability tokens, not free text — do not run the disclosure blocklist.
        // The bounded character set already excludes prefixed protected identifiers and storage syntax.
        return values.Select(v => ValidateStoryId(v, parameterName)).ToArray();
    }

    private static string ValidateStoryId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        const int maxLength = 128;
        return value.Length > maxLength || value.Any(static c => !IsStoryIdCharacter(c))
            ? throw new ArgumentException("Story identifiers must be bounded machine-readable tokens.", parameterName)
            : value;
    }

    private static bool IsStoryIdCharacter(char c)
        => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.';

    private static IReadOnlyList<string> ValidateEvidenceLinks(IReadOnlyList<string>? values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        return values.Select(v => ConformanceContractValidation.RequiredSafeToken(v, parameterName)).ToArray();
    }
}

/// <summary>
/// Validates a <see cref="ReleaseWaiverV1"/> and returns content-safe typed error tokens (FR85, AC1, AC2, AC3).
/// </summary>
public static class ReleaseWaiverValidator
{
    /// <summary>
    /// Validates the waiver and returns typed diagnostic reasons for any governance violations.
    /// </summary>
    /// <param name="waiver">The waiver to validate.</param>
    /// <param name="evaluatedAt">The point-in-time at which to evaluate temporal conditions; enables deterministic testing.</param>
    /// <returns>A read-only list of content-safe token error reasons; empty when the waiver is valid.</returns>
    public static IReadOnlyList<string> ValidateWaiver(ReleaseWaiverV1 waiver, DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(waiver);

        List<string> errors = [];

        if (waiver.IsBlocker && waiver.Approver is null)
        {
            errors.Add("blocker-requires-approver");
        }

        if (waiver.ExpiryDateUtc < evaluatedAt)
        {
            errors.Add("expired-waiver");
        }

        if (waiver.ReviewDateUtc < evaluatedAt)
        {
            errors.Add("stale-review-date");
        }

        if (waiver.IsBlocker && waiver.AffectedGateId is not null && waiver.BuyerAcceptanceStatus is null)
        {
            errors.Add("buyer-facing-missing-acceptance");
        }

        return errors;
    }
}
